# Plan — phase-06-docker: Dockerize (API + MSSQL)

## 1. Overview

### What this phase accomplishes
Makes the whole system runnable with one command (`docker compose up --build`) by containerizing the API and provisioning SQL Server. By the end of this phase:
- A multi-stage `Dockerfile` builds `ShopApi.API` (SDK build → ASP.NET runtime, non-root user, port 8080).
- `docker-compose.yml` runs `api` + `mssql` (2022) on a shared network with a persistent volume, an `mssql` healthcheck, `depends_on: healthy`, and `env_file: .env`.
- `.dockerignore` keeps secrets and build noise out of the image context.
- `.env.example` gains the compose-owned variables (`SA_PASSWORD`, `DB_NAME`).
- `docker compose up --build` boots to a healthy stack: SQL passes healthcheck → API auto-migrates + seeds (Development) → `/health` returns 200 with `database: Healthy`.
- The runbook (how to run, how to migrate) is documented in this plan and verified live.

### Clean Architecture layers affected
| Layer | Project | State after this phase |
|---|---|---|
| None (code) | — | **No C# changes.** This phase adds containerization only: 2 new files, 1 modified template file. All validation is operational (build images, boot stack, probe endpoints). |
| Cross-cutting | repo root | `Dockerfile`, `docker-compose.yml`, `.dockerignore` new; `.env.example` extended. |

### Dependencies on previous phases
- Phase 01: `global.json` SDK 8.0.x matches container images (`aspnet:8.0` / `sdk:8.0`).
- Phase 04: `EnableRetryOnFailure` (survives SQL cold starts); Dev `MigrateAsync` + `SeedData` (zero-step first boot); unique indexes/constraints travel with the migration.
- Phase 05: `/health` DB check (readiness signal); warn-and-continue Dev bootstrap (app serves even if SQL lags); `DB_CONNECTION`/`JWT_SECRET` env-first resolution (compose supplies both).
- Phase 07 (forward): the runbook section here seeds `API_DOCUMENTATION.md`/README Docker chapters — do not write those files in this phase.

---

## 2. File-by-File Breakdown

| # | File Path | Action | Layer | Purpose |
|---|---|---|---|---|
| 1 | `src/ShopApi.API/Dockerfile` | Create | Cross-cutting | Multi-stage API image (sdk build → aspnet runtime, non-root, 8080) |
| 2 | `docker-compose.yml` | Create | Cross-cutting | `api` + `mssql` stack: network, volume, healthcheck, env wiring |
| 3 | `.dockerignore` | Create | Cross-cutting | Slim context, never bake `.env`/build output |
| 4 | `.env.example` | Modify | Cross-cutting | Add `SA_PASSWORD`, `DB_NAME` templates (append-only) |

**Explicit non-goals:** no C# changes; no migration `.cs` files (generated post-phase, §4 step 8); no `API_DOCUMENTATION.md`/README (Phase 07); no entrypoint script (rejected — see §5); no API-container healthcheck (rejected — image has no curl; `/health` serves orchestrators instead).

---

## 3. Detailed File Specifications

### File: `src/ShopApi.API/Dockerfile` (Create; build context = repo root)
- **Base images** (exact): `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime), `mcr.microsoft.com/dotnet/sdk:8.0` (build) — matches `global.json` 8.0.x line.
- **Stage 1 `base`**: `FROM .../aspnet:8.0 AS base`; `WORKDIR /app`; `EXPOSE 8080`; `USER app` (.NET 8 images ship the non-root `app` user; ASP.NET defaults to port 8080 via `ASPNETCORE_HTTP_PORTS`, so no `ENV ASPNETCORE_URLS` needed).
- **Stage 2 `build`**: `FROM .../sdk:8.0 AS build`; `ARG BUILD_CONFIGURATION=Release`; `WORKDIR /src`; `COPY` each of the 4 `.csproj` files individually (sln optional — include `ShopApi.sln` copy for solution restore fidelity? Decision: copy the 4 csproj files only, restore the API project — fewer moving parts); `RUN dotnet restore "src/ShopApi.API/ShopApi.API.csproj"` (own layer → dependency caching); `COPY . .`; `WORKDIR "/src/src/ShopApi.API"`; `RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build`.
- **Stage 3 `publish`**: `FROM build AS publish`; `RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false`.
- **Stage 4 `final`**: `FROM base AS final`; `WORKDIR /app`; `COPY --from=publish /app/publish .`; `ENTRYPOINT ["dotnet", "ShopApi.API.dll"]`.
- **Validation**: `docker build -f src/ShopApi.API/Dockerfile -t shopapi:phase06 .` succeeds; `docker images` shows a single tagged image; no secrets in layers (nothing under `/app` references `.env`).

### File: `docker-compose.yml` (Create; Compose v2 syntax, no `version:` key)
- **Top level**: `name: shopapi` (stable container names for the runbook).
- **Service `api`**: `build: { context: ., dockerfile: src/ShopApi.API/Dockerfile }`; `ports: ["5125:8080"]` (host 5125 matches local `launchSettings`/`.env` habit); `env_file: [.env]` (phase mandate — secrets come from the user's real `.env`); `environment:` — `ASPNETCORE_ENVIRONMENT: Development` (local/demo only; enables auto-migrate + seed + Swagger — Production override documented in §6), `DB_CONNECTION: "Server=mssql,1433;Database=${DB_NAME:-ShopApiDb};User Id=sa;Password=${SA_PASSWORD:?SA_PASSWORD is required};TrustServerCertificate=True;"` (explicit `environment:` wins over `env_file`, so the `.env` localhost value never breaks containers; `:?` fails fast with a clear message when `SA_PASSWORD` is unset); `depends_on: mssql: { condition: service_healthy }`; `networks: [shopnet]`; `restart: unless-stopped`.
- **Service `mssql`**: `image: mcr.microsoft.com/mssql/server:2022-latest`; `environment:` — `ACCEPT_EULA: Y`, `SA_PASSWORD: ${SA_PASSWORD:?SA_PASSWORD is required}`, `MSSQL_PID: ${MSSQL_PID:-Developer}`; `ports: ["1433:1433"]` (host-side `dotnet ef` + SSMS access); `volumes: [mssql-data:/var/opt/mssql]`; `networks: [shopnet]`; `healthcheck: { test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$$SA_PASSWORD\" -Q \"SELECT 1\" -C || exit 1"], interval: 10s, timeout: 5s, retries: 10, start_period: 30s }` (`$$` compose-escapes to `$` in-container; `-C` trusts the self-signed cert; tools18 path is correct for the 2022 image).
- **Top-level `volumes:`**: `mssql-data: { driver: local }` (survives `docker compose down`; destroyed only by `down -v` — runbook calls this out).
- **Top-level `networks:`**: `shopnet: { driver: bridge }` (the mandated shared network; `mssql` hostname resolves from `api`).
- **Validation**: `docker compose config` renders without errors/warnings (proves interpolation + `$$` escaping).

### File: `.dockerignore` (Create)
- **Exact entries** (one per line): `bin/`, `obj/`, `TestResults/`, `.git/`, `.vs/`, `.idea/`, `.vscode/`, `.env`, `.env.*`, `!.env.example`, `docs/`, `*.md`, `docker-compose*.yml`, `.dockerignore`, `.gitignore`.
- **Rationale per entry**: build output + VCS/IDE noise slim the context; `.env`/`.env.*` NEVER baked (negation keeps the template visible, harmless); `docs/` + `*.md` unneeded at runtime; compose file + ignores are host-only.
- **Validation**: `docker build` context transfer size is small (BUILDKIT `transferring context` line shows KB, not tens of MB).

### File: `.env.example` (Modify — append only, existing 5 lines untouched)
- **Appended lines**:
  - Blank line + `# --- Docker Compose (Phase 06) ---`
  - `SA_PASSWORD=CHANGE_ME_STRONG_8_PLUS_CHARS` (SQL Server requires ≥8 chars with complexity — uppercase/lowercase/digit/symbol; comment states the rule).
  - `DB_NAME=ShopApiDb`
  - Comment: `# Compose builds the container DB_CONNECTION from SA_PASSWORD/DB_NAME (host mssql); the localhost DB_CONNECTION above is for host-side dotnet/run only.`
- **Validation**: copying to `.env` + filling the two secrets is sufficient for `docker compose up --build`; no other file edits needed.

---

## 4. Implementation Order

1. Append `SA_PASSWORD`/`DB_NAME` block to `.env.example` (no other edits).
2. Write `.dockerignore` per §3.
3. Write `src/ShopApi.API/Dockerfile` per §3 (context = repo root).
4. Write `docker-compose.yml` per §3.
5. `docker compose config` — must render clean (catches interpolation/`$$` mistakes before any download).
6. `docker build -f src/ShopApi.API/Dockerfile -t shopapi:phase06 .` — image builds; note context size + final size.
7. Prepare runtime `.env`: `copy .env.example .env` to a SCRATCH location? Decision: copy in place (`cp`), fill `SA_PASSWORD` with a strong test value + keep others — `.env` is gitignored (verified Phase 01); after verification, restore `.env` to non-existence (delete scratch) so no secret lingers on disk. Record in commit body that `.env` was never committed.
8. Generate the initial migration (first time, needs only the SDK — no DB): `dotnet ef migrations add InitialCreate --project src/ShopApi.Infrastructure --startup-project src/ShopApi.API` (requires `dotnet-ef` tool on host; install hint `dotnet tool install -g dotnet-ef` if missing). Migration files ARE created here (unlike Phases 01–05: this is the first phase where the startup project is fully wired). Verify `Migrations/` contains `*_InitialCreate.cs` + snapshot + designer.
9. `docker compose up --build -d` — wait for `mssql` healthy (`docker compose ps`, `docker compose logs mssql` shows `SQL Server is now ready`), then API migrates + seeds via the Development bootstrap.
10. Verify live (all against `http://localhost:5125`): `GET /health` → 200 with `database: Healthy`; `GET /swagger/v1/swagger.json` → 200; `GET /api/products` → 200 with the 4 seeded products; `POST /api/auth/register` (new user) → 200 + token; `POST /api/auth/login` → 200; authenticated `POST /api/cart/items` → 200; `GET /api/cart` totals match.
11. Verify persistence: `docker compose down` (no `-v`) → `up -d` → previously registered user still logs in (volume survived).
12. Teardown decision: leave stack DOWN but volumes intact (`docker compose down`) for Phase 07; delete scratch `.env`; `git status` review (4 files: 3 adds + 1 modify; migration files are Phase-06-exempted extras? Decision: migration `.cs` files commit WITH this phase as `feat(infrastructure): add initial migration` — they are build outputs of a documented step, small, and required for reviewers to run compose without the SDK tool. Record this explicitly.)
13. `dotnet build` sanity (host build unaffected by new files).

## 5. Code Patterns & Standards

- **Official-template Dockerfile**: stage order/naming (`base/build/publish/final`), `USER app`, `UseAppHost=false`, per-csproj `COPY` for layer caching — deviations from the Microsoft template need justification in review.
- **Fail-fast compose**: `:?SA_PASSWORD is required` on both services (identical message — one mental model); SQL complexity rule documented next to the template value.
- **Env precedence made explicit**: explicit `environment:` > `env_file:` > image `ENV` > code defaults — the runbook states this chain so the localhost-vs-`mssql` host question never recurs.
- **No entrypoint script** (conscious rejection of phase item 6): startup ordering is covered by `depends_on: healthy` + EF `EnableRetryOnFailure` + warn-and-continue bootstrap; a custom script would add shell-maintenance burden for zero gain. Revisit only if multi-database orchestration appears.
- **No API healthcheck stanza** (conscious rejection): `aspnet:8.0` ships no curl/wget; installing one bloats the image. `/health` (with DB check) serves Docker/K8s probes; Compose readiness is proven by the smoke tests in §4 step 10.
- **Production posture noted, not implemented**: compose pins `Development` for the zero-step demo; production flips `ASPNETCORE_ENVIRONMENT=Production`, pre-migrates via CI (`dotnet ef database update` with a privileged connection), and mounts secrets from a vault — flagged for Phase 08 hardening, not done here.
- **KISS**: one shared bridge network, one named volume, latest-pinned 2022 SQL tag, no resource limits (documented as intentionally absent for local dev).

## 6. Edge Cases & Validation

- **Missing `.env`**: compose aborts early on `:?` with `SA_PASSWORD is required` (before pulling images — fast feedback, no half-built stack).
- **Weak `SA_PASSWORD`**: SQL container exits (`Password validation failed`); runbook states the complexity rule + `docker compose logs mssql` as the diagnostic; API keeps serving with `/health` Unhealthy (never crash-loops on someone else's misconfig — warn-and-continue).
- **Port collisions (5125/1433 in use)**: change the HOST side only (`"5126:8080"`, `"1434:1433"`); container sides stay fixed; host-side `dotnet ef` then targets the remapped port.
- **First-boot SQL slowness**: `start_period: 30s` + 10 retries + API retry policy + bootstrap catch — three nested safety nets; observed cold-start behavior recorded during verification.
- **Volume reuse across schema changes**: stale `mssql-data` from an older migration set causes migrate/seed surprises — runbook documents `docker compose down -v` as the reset escape hatch (data loss called out explicitly).
- **Seed duplication**: `SeedData` early-returns on existing categories — re-`up` never duplicates; wiping the volume re-seeds cleanly.
- **Secret leakage**: `.env` gitignored + `.dockerignore`d (double barrier); `docker compose config` output redacts nothing — reviewers must not paste rendered output with real secrets; `.env.example` values are placeholders only.
- **Apple Silicon / ARM hosts**: `mssql/server:2022-latest` is amd64-only — ARM Docker Desktops emulate (slow) or fail; runbook notes `--platform linux/amd64` override and the performance caveat (validates on the current x64 Windows host; ARM called out, not solved).
- **Linux line endings**: `Dockerfile`/compose/`dockerignore` must be LF-safe (no CRLF-sensitive instructions used — no shell scripts, so safe by construction).

## 7. Verification Checklist

- [ ] `src/ShopApi.API/Dockerfile` builds cleanly (`docker build -f ... -t shopapi:phase06 .`), small context transfer, non-root `app` user effective (`docker run --rm --entrypoint whoami` → `app`)
- [ ] `docker compose config` renders with `mssql` host in API `DB_CONNECTION` and no warnings
- [ ] `InitialCreate` migration generated (3 files under `src/ShopApi.Infrastructure/Migrations/`); `dotnet build` still green
- [ ] `docker compose up --build -d` reaches `mssql` healthy + `api` running; API logs show migration + seed (not the unreachable-DB warning)
- [ ] `GET /health` → 200 `Healthy` (database entry Healthy); `/swagger/v1/swagger.json` → 200
- [ ] `GET /api/products` lists the 4 seeded products; register→login→add-to-cart→get-cart round trip returns 200s with coherent totals
- [ ] Persistence: `down` → `up -d` preserves the registered user (volume intact)
- [ ] Teardown leaves `down` state with volume intact; scratch `.env` deleted; `git status` shows only the 4 planned files + migration set
- [ ] `.env` never committed (`git check-ignore`, `git log --name-only` inspection); `.dockerignore` excludes `.env`
- [ ] Runbook (§4 steps 5–12 + §6 diagnostics) executed verbatim during verification
