# Validation Report: phase-06-docker

## Summary

- **Status**: ✅ PASSED
- **Build Status**: ✅ (`dotnet build`: 0 errors; image `shopapi:phase06` 238MB builds clean)
- **Test Status**: N/A (no test projects; live stack verification in build phase — all green, see Passed §)

## Files Checked

| # | Planned File | Exists | Matches Plan | Notes |
|---|---|---|---|---|
| 1 | `src/ShopApi.API/Dockerfile` | ✅ | ✅ | Official-template stages, `USER app`, per-csproj layer caching, `UseAppHost=false` |
| 2 | `docker-compose.yml` | ✅ | ✅ | `api`+`mssql:2022-latest`, `shopnet`, `mssql-data`, tools18 healthcheck, `depends_on: healthy`, fail-fast `:?` guards — plus one justified addition (see Warnings §2) |
| 3 | `.dockerignore` | ✅ | ✅ | Exact 15 entries; double barrier for `.env` |
| 4 | `.env.example` (Modify) | ✅ | ✅ | Append-only `SA_PASSWORD`/`DB_NAME` block with complexity rule + host-note |
| + | `Migrations/` 3 files (phase-exempted) | ✅ | ✅ | 8 tables, all FKs/indexes/checks (verified in build) |
| + | `Mappings/MappingProfile.cs` (fix) | ✅ | ✅ | User-approved `ConstructUsing` fix; 12/12 harness checks incl. strict config validation |

## ✅ Passed

- `docker compose config` renders with `mssql` host in API `DB_CONNECTION` (verified with scratch `.env`).
- Image runs as non-root `app` (`whoami` check); 238MB final size.
- Live stack: `mssql` reached `healthy`; API migrated + seeded; `/health` 200 Healthy; `/swagger` 200; `GET /api/products` returned the 4 seeded products with correct flattened prices.
- Full round trip: register (Customer) → login → add-to-cart → get-cart coherent (2 × 1299.99 = 2599.98).
- Persistence: `down` → `up -d` preserved the registered user (volume `shopapi_mssql-data` intact, still present).
- Teardown as planned: stack `down`, volume preserved, scratch `.env` deleted and never committed (absent from disk; gitignored).
- `dotnet build` host: 0 errors with migration files included.

## ❌ Failed / Missing

- None.

## ⚠️ Warnings

1. **Pre-existing NU1903 (AutoMapper advisory)** — carried, out of scope.
2. **Justified plan deviation**: `ASPNETCORE_URLS: "http://+:8080"` added to the `api` service environment. Without it, the `.env` value (`http://localhost:5125`) binds loopback-only inside the container and the mapped port serves nothing — found by reasoning during build, would have failed verification. Explicit `environment:` wins over `env_file:` per the plan's own precedence chain.
3. **Approved out-of-phase fix** (build phase, user-confirmed): AutoMapper 13 cannot constructor-bind nested-path flattenings (`Money.Amount`, `Product.Name`) — first live run returned 500 on `/api/products`. Fixed via explicit `ConstructUsing` + `ForAllMembers(Ignore)` in the 4 affected maps; DTO shapes untouched. Recommend committing as its own `fix(application): ...` for traceability.
4. **Transient registry flake observed**: `mcr.microsoft.com` SDK manifest fetch EOF'd twice, succeeded on retry minutes later (`aspnet` unaffected). No action — noted for CI retry-policy awareness.
5. **ARM hosts**: `mssql/server:2022-latest` is amd64-only (plan §6 note stands; validated on x64 Windows).

## 📋 Clean Code Checklist

- [x] SOLID principles followed — N/A (no C# architecture added; container files declarative)
- [x] No code duplication (DRY) — compose interpolates `SA_PASSWORD`/`DB_NAME` from single definitions; Dockerfile reuses stages
- [x] Simple solutions (KISS) — no entrypoint script, no API healthcheck stanza, no prod hardening (all consciously deferred per plan)
- [x] Meaningful names (`shopapi`/`shopnet`/`mssql-data`, `auth` policy untouched, stable ports)
- [x] No magic numbers/strings (healthcheck intervals, ports, column-compatible versions documented; secrets never in files)
- [x] Proper error handling (fail-fast `:?` guards; SQL weak-password surfaces via `logs mssql`; API serves Unhealthy rather than crash-looping)
- [x] All planned files exist
- [x] Namespaces match plan — N/A (no C# namespaces added)
- [x] Build succeeds with no errors
- [x] No compiler warnings (except pre-existing NU1903)

## 🔧 Recommended Fixes

1. None required for this phase. Next: Phase 07 (documentation) then Phase 08 (final review + prod hardening: `Production` compose overlay, CI migration job, secret vaulting).
2. Per AGENTS.md Commit Rules, suggested atomic split of the pending delta: `feat(api): add controllers and HTTP pipeline` + `feat(docker): containerize API with MSSQL compose stack` + `feat(infrastructure): add initial migration` + `fix(application): use explicit AutoMapper construction` + `docs(plans): ...` reports — staging explicitly, showing diffs, awaiting user confirmation per commit.
