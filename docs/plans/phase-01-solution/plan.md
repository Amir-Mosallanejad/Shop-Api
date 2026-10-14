# Plan — phase-01-solution: Solution Structure & Projects

## 1. Overview

### What this phase accomplishes
Scaffolds the entire `ShopApi` .NET 8 solution from zero to a buildable, runnable skeleton that enforces Clean Architecture boundaries before any business logic exists. By the end of this phase:
- A `ShopApi.sln` solution exists at repo root.
- Four projects exist under `src/` with correct SDK types and target framework.
- Project-to-project references form a strict one-way dependency graph (no cycles).
- Each project has its prescribed internal folder structure (empty folders + placeholder intent, no domain logic yet).
- `ShopApi.API` boots with minimal `Program.cs`: Swagger (Swashbuckle), DotNetEnv `.env` loading, DI placeholders for later phases, EF Core DbContext registration stub (connection string from env, no entities yet).
- `.env.example` documents required environment variables without secrets.
- `dotnet build` and `dotnet run --project src/ShopApi.API` both succeed; Swagger UI reachable in Development.

### Clean Architecture layers affected
| Layer | Project | State after this phase |
|---|---|---|
| Domain | `src/ShopApi.Domain` | Created. Pure class library, zero external NuGet deps. Folder structure only. No entities yet (Phase 02). |
| Application | `src/ShopApi.Application` | Created. Class library with CQRS/validation/mapping packages installed but unused. Folder structure only. No handlers yet (Phase 03+). |
| Infrastructure | `src/ShopApi.Infrastructure` | Created. Class library with EF Core + SQL Server packages installed but no DbContext implementation yet (stub registration only in API). Folder structure only. |
| API | `src/ShopApi.API` | Created. ASP.NET Core Web API. Only project with executable output. Contains `Program.cs`, `appsettings*.json`, `Properties/launchSettings.json`. Swagger + DI + env loading wired. No controllers yet. |

### Dependencies on previous phases
- **None.** This is Phase 01, the root phase.
- Assumes host has .NET 8 SDK (`8.0.406` verified on this machine). No prior `src/` code exists (verified: repo contains only `.git/`, `.opencode/`, `docs/`, `AGENTS.md`).
- All later phases (02-domain, 03-application, etc.) depend on this phase's project GUIDs, references, and folder conventions remaining stable. Do NOT rename projects or change target frameworks after this phase without updating all downstream plans.

---

## 2. File-by-File Breakdown

| # | File Path | Action | Layer | Purpose |
|---|---|---|---|---|
| 1 | `ShopApi.sln` | Create | Solution | Solution container linking all 4 projects |
| 2 | `src/ShopApi.Domain/ShopApi.Domain.csproj` | Create | Domain | Domain class-library project file, net8.0, no external packages |
| 3 | `src/ShopApi.Application/ShopApi.Application.csproj` | Create | Application | Application class-library project file with MediatR, FluentValidation, AutoMapper |
| 4 | `src/ShopApi.Infrastructure/ShopApi.Infrastructure.csproj` | Create | Infrastructure | Infrastructure class-library project file with EF Core + SqlServer |
| 5 | `src/ShopApi.API/ShopApi.API.csproj` | Create | API | WebAPI project file with Swashbuckle + DotNetEnv, project refs |
| 6 | `src/ShopApi.API/Program.cs` | Create | API | Composition root: env loading, services, middleware pipeline |
| 7 | `src/ShopApi.API/appsettings.json` | Create | API | Base configuration (Logging, AllowedHosts, ConnectionStrings placeholder) |
| 8 | `src/ShopApi.API/appsettings.Development.json` | Create | API | Dev overrides (detailed errors, Swagger enabled flag) |
| 9 | `src/ShopApi.API/Properties/launchSettings.json` | Create | API | Local launch profiles (http/https ports, env vars) |
| 10 | `.env.example` | Create | Cross-cutting | Template for required env vars, no real secrets |
| 11 | `.gitignore` | Create | Cross-cutting | Standard Visual Studio + .NET + `.env` ignore rules |
| 12 | `global.json` | Create | Cross-cutting | Pin .NET SDK to 8.x line for reproducible builds |
| 13 | `src/ShopApi.Domain/` folder tree | Create | Domain | Empty folder skeleton (Entities, ValueObjects, Enums, Exceptions, Common) |
| 14 | `src/ShopApi.Application/` folder tree | Create | Application | Empty folder skeleton (Common, Interfaces, Behaviors, Mappings, Features placeholder) |
| 15 | `src/ShopApi.Infrastructure/` folder tree | Create | Infrastructure | Empty folder skeleton (Persistence, Repositories, UnitOfWork placeholder, Migrations placeholder) |
| 16 | `src/ShopApi.API/` folder tree | Create | API | Folder skeleton (Controllers placeholder, Middleware placeholder, Filters placeholder) |

**Explicit non-goals (do NOT create in this phase):** `Dockerfile`, `docker-compose.yml`, any `Entity`, `DbContext`, `Controller`, `Handler`, `Validator`, `Profile`, `Repository`, migration files. Docker wiring is deferred to a later infrastructure phase per `AGENTS.md` workflow.

### Dependency graph (must be enforced)
```
ShopApi.Domain ──► (no project refs)
ShopApi.Application ──► ShopApi.Domain
ShopApi.Infrastructure ──► ShopApi.Domain, ShopApi.Application
ShopApi.API ──► ShopApi.Application, ShopApi.Infrastructure (transitively Domain; do NOT add direct Domain ref unless needed — prefer transitive)
```
No reverse or circular references allowed.

---

## 3. Detailed File Specifications

### File: `ShopApi.sln`
- **Type**: Visual Studio solution file (generated by CLI, not hand-written)
- **Contents spec**: Must list exactly 4 projects with matching relative paths `src/<Project>/<Project>.csproj`. Solution configuration platforms: `Debug|Any CPU` and `Release|Any CPU`.
- **Dependencies**: None (container only)
- **Validation rules**: `dotnet sln list` shows 4 entries; opening in VS/Rider shows all projects loaded, zero unloadable.
- **Notes**: Generate via `dotnet new sln -n ShopApi`, then `dotnet sln add` per project. Never hand-edit GUIDs.

### File: `src/ShopApi.Domain/ShopApi.Domain.csproj`
- **SDK**: `Microsoft.NET.Sdk`
- **Target framework**: `net8.0` (singular; no multi-targeting)
- **C# language version**: `latest` (or `12`; must match `global.json` SDK capabilities)
- **Nullable**: `enable`
- **ImplicitUsings**: `enable`
- **TreatWarningsAsErrors**: `false` in this phase (switch to `true` only after validation baseline passes, to avoid blocking scaffold)
- **NuGet packages**: NONE. Domain must remain dependency-free. Explicitly forbid: EF Core, MediatR, AutoMapper, Newtonsoft.
- **ProjectReferences**: NONE.
- **Validation rules**: `dotnet build src/ShopApi.Domain` succeeds with 0 warnings attributable to config.
- **Notes**: DDD purity gate — any future `using Microsoft.EntityFrameworkCore` inside Domain is a violation.

### File: `src/ShopApi.Application/ShopApi.Application.csproj`
- **SDK**: `Microsoft.NET.Sdk`
- **Target framework**: `net8.0`
- **Nullable / ImplicitUsings**: `enable` / `enable`
- **NuGet packages (exact minimal set, .NET 8 compatible)**:
  - `MediatR` — v12.x (CQRS dispatcher; no `MediatR.Extensions.Microsoft.DependencyInjection` — that package is deprecated; use `MediatR` + manual `AddMediatR` from same package)
  - `FluentValidation` — v11.x
  - `FluentValidation.DependencyInjectionExtensions` — v11.x (for `AddValidatorsFromAssembly`)
  - `AutoMapper` — v12.x or v13.x
- **ProjectReferences**: `../ShopApi.Domain/ShopApi.Domain.csproj` (single ref)
- **Validation rules**: `dotnet restore` resolves without version conflicts; no EF Core packages here.
- **Notes**: `MediatR` registration call itself lives in API `Program.cs` (composition root), not in Application. Application only declares handlers/validators later.

### File: `src/ShopApi.Infrastructure/ShopApi.Infrastructure.csproj`
- **SDK**: `Microsoft.NET.Sdk`
- **Target framework**: `net8.0`
- **Nullable / ImplicitUsings**: `enable` / `enable`
- **NuGet packages (exact minimal set)**:
  - `Microsoft.EntityFrameworkCore` — v8.x (must match runtime major 8)
  - `Microsoft.EntityFrameworkCore.SqlServer` — v8.x (same version as core)
  - `Microsoft.EntityFrameworkCore.Tools` — v8.x, `PrivateAssets="all"` (migrations tooling only)
  - `Microsoft.EntityFrameworkCore.Design` — v8.x, `PrivateAssets="all"` (design-time `dotnet ef` support)
- **ProjectReferences**:
  - `../ShopApi.Domain/ShopApi.Domain.csproj`
  - `../ShopApi.Application/ShopApi.Application.csproj`
- **Validation rules**: All EF packages share identical major.minor (e.g., all `8.0.x`); `dotnet restore` clean.
- **Notes**: Infrastructure references Application only for interfaces (to be defined in later phase). If strict Clean Architecture is desired, a later refactor may drop the Application ref and keep only Domain — flag as decision point, default to keeping both refs for UnitOfWork + repository interface access.

### File: `src/ShopApi.API/ShopApi.API.csproj`
- **SDK**: `Microsoft.NET.Sdk.Web`
- **Target framework**: `net8.0`
- **Nullable / ImplicitUsings**: `enable` / `enable`
- **NuGet packages (exact minimal set)**:
  - `Swashbuckle.AspNetCore` — v6.x (Swagger gen + UI; .NET 8 compatible)
  - `DotNetEnv` — v3.x (`.env` file loading; method `Env.Load()` / `Env.TraversePath()`)
  - `Microsoft.EntityFrameworkCore.Design` — v8.x only if `dotnet ef` migrations are run with API as startup project; otherwise omit to avoid duplication (decision: include with `PrivateAssets="all"` if Infrastructure is not set as startup-capable; document choice)
- **ProjectReferences**:
  - `../ShopApi.Application/ShopApi.Application.csproj`
  - `../ShopApi.Infrastructure/ShopApi.Infrastructure.csproj`
- **Validation rules**: `dotnet run --project src/ShopApi.API` launches Kestrel; no `Class Library` output type override.
- **Notes**: API must NOT reference Domain directly (use transitive via Application). If a direct `using ShopApi.Domain` becomes necessary later, add explicit ref then — not now.

### File: `src/ShopApi.API/Program.cs`
- **Namespace**: Implicit (top-level statements, no namespace declaration)
- **Type**: Composition root (top-level `WebApplicationBuilder` → `WebApplication` pipeline)
- **Ordered responsibilities (exact sequence)**:
  1. `DotNetEnv.Env.Load()` (or `Env.TraversePath().Load()`) as the FIRST statement before `CreateBuilder`, so `Environment.GetEnvironmentVariable` sees `.env` values. Must tolerate missing `.env` file (local dev only; CI uses real env vars) — no throw if file absent.
  2. `var builder = WebApplication.CreateBuilder(args);`
  3. Configuration: `builder.Configuration.AddEnvironmentVariables()` (default already includes it; ensure `.env` values flow through). Connection string resolved as `Environment.GetEnvironmentVariable("DB_CONNECTION") ?? builder.Configuration.GetConnectionString("DefaultConnection")`.
  4. Service registrations in order:
     - `builder.Services.AddControllers();`
     - `builder.Services.AddEndpointsApiExplorer();`
     - `builder.Services.AddSwaggerGen(...)` with doc definition `v1 / ShopApi` (title + version only in this phase; JWT/security scheme deferred).
     - `builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(<Application assembly marker>).Assembly))` — assembly marker type to be created in later phase; in THIS phase register with `typeof(Program).Assembly` + TODO comment pointing to future marker, or guard with comment `// TODO(phase-03): point to Application assembly`.
     - `// TODO(phase-03+): AddValidatorsFromAssembly, AddAutoMapper, AddDbContext<AppDbContext>, AddScoped<IUnitOfWork,...>, AddScoped<generics>` — stubs as comments only, no dead code that fails compilation.
  5. `var app = builder.Build();`
  6. Middleware pipeline in order:
     - `if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }`
     - `app.UseHttpsRedirection();`
     - `app.UseAuthorization();` (`UseAuthentication` omitted until auth phase — add TODO comment, do NOT add no-op call)
     - `app.MapControllers();` (no controllers exist yet — must still map so 404-with-routing works, not 500)
  7. `app.Run();` + `public partial class Program { }` marker at file end for future integration tests (`WebApplicationFactory<Program>`).
- **Dependencies**: `DotNetEnv`, `Swashbuckle.AspNetCore`, `MediatR` (registration only)
- **Validation rules**: Starts in Development and Release; `/swagger` returns 200 in Development; `/swagger/v1/swagger.json` returns valid OpenAPI JSON; missing `.env` does not crash startup.
- **Notes**: Keep file under ~60 lines. No business logic, no hardcoded connection strings, no `UseDeveloperExceptionPage` (already implied by framework). Follow minimal-API top-level style per .NET 8 `webapi` template.

### File: `src/ShopApi.API/appsettings.json`
- **Type**: JSON configuration, UTF-8, no BOM
- **Required keys spec**:
  - `Logging:LogLevel:Default` = `Information`, `Microsoft.AspNetCore` = `Warning`
  - `AllowedHosts` = `*`
  - `ConnectionStrings:DefaultConnection` = placeholder referencing env, e.g. value sourced from `DB_CONNECTION` at runtime (leave a non-secret localdb placeholder string; real value NEVER committed)
  - `Swagger:Title` = `ShopApi`, `Swagger:Version` = `v1` (optional structured section if Program.cs reads it; otherwise rely on inline constants)
- **Validation rules**: Valid JSON; `builder.Configuration.GetConnectionString("DefaultConnection")` returns non-null (even if placeholder).
- **Notes**: Real connection string lives in `.env` / user-secrets / CI variables, never here.

### File: `src/ShopApi.API/appsettings.Development.json`
- **Type**: JSON environment override
- **Required keys spec**: `Logging:LogLevel:Default` = `Debug`; `DetailedErrors` = `true` (informational); no connection string override unless local dev needs distinct DB (if present, must still be placeholder, not credential).
- **Validation rules**: Merges cleanly over base; app still starts with `ASPNETCORE_ENVIRONMENT=Development`.
- **Notes**: Committed to git (placeholders only). Real dev secrets go in `.env` (gitignored) or `dotnet user-secrets`.

### File: `src/ShopApi.API/Properties/launchSettings.json`
- **Type**: JSON launch profiles (generated by `dotnet new webapi`, then verified)
- **Required profiles spec**:
  - `http`: `commandName: Project`, `dotnetRunMessages: true`, `launchBrowser: true`, `launchUrl: swagger`, `applicationUrl: http://localhost:5000`, `environmentVariables: ASPNETCORE_ENVIRONMENT=Development`
  - `https`: same + `https://localhost:5001` first in `applicationUrl`, matching template default ports
- **Validation rules**: `dotnet run --launch-profile http` and `--launch-profile https` both boot; Swagger auto-opens only when `launchBrowser` true.
- **Notes**: Do not invent custom ports in this phase; keep template defaults to minimize onboarding friction.

### File: `.env.example`
- **Type**: DotEnv template, committed to git
- **Required variable keys spec (values are placeholders/empty, never secrets)**:
  - `DB_CONNECTION=` — full SQL Server connection string template, e.g. `Server=localhost,1433;Database=ShopApiDb;User Id=sa;Password=CHANGE_ME;TrustServerCertificate=True;`
  - `ASPNETCORE_ENVIRONMENT=Development`
  - `ASPNETCORE_URLS=http://localhost:5000` (optional, documents Kestrel binding)
- **Dependencies**: Consumed by `DotNetEnv.Env.Load()` in `Program.cs`
- **Validation rules**: Copying to `.env` (`cp .env.example .env`) + `dotnet run` works after filling `CHANGE_ME`; `.env` itself is gitignored.
- **Notes**: Add one comment line at top: `# Copy to .env and fill real values. Never commit .env.`

### File: `.gitignore`
- **Type**: Git ignore rules
- **Required entries spec**: `.env` (exact) + `.env.*` except `.env.example` via negation `!.env.example`; standard `bin/`, `obj/`; `TestResults/`; `.vs/`, `.vscode/` (optional but recommended); `*.user`.
- **Validation rules**: `git status --ignored` shows `.env` ignored and `.env.example` tracked after `git add`.
- **Notes**: Generate via `dotnet new gitignore`, then append DotEnv lines. Verify negation ordering (negation AFTER wildcard).

### File: `global.json`
- **Type**: JSON SDK pin
- **Required keys spec**: `sdk:version` = `8.0.xxx` matching installed SDK floor (e.g. `8.0.400` allows roll-forward), `rollForward: latestMinor`, `allowPrerelease: false`.
- **Validation rules**: `dotnet --version` inside repo reports 8.x; SDK-not-found error must not occur on machines with any 8.0.4xx SDK.
- **Notes**: Prevents accidental .NET 9/10 SDK pickup in CI.

### Folder trees (description only, no code files yet)
- **`src/ShopApi.Domain/`**: `Entities/` (future aggregates + `BaseEntity`), `ValueObjects/` (future `Money`, etc.), `Enums/`, `Exceptions/` (domain exceptions), `Common/` (base types, interfaces like `IAuditable`). Each folder gets a `.gitkeep` (or is created implicitly by later phase files) so the structure is visible in git. No `.cs` files in this phase.
- **`src/ShopApi.Application/`**: `Common/` (`Behaviors/`, `Exceptions/`, `Wrappers/Result.cs` placeholder dir), `Interfaces/` (`IRepository.cs`, `IUnitOfWork.cs` placeholder dir), `Mappings/` (AutoMapper profiles dir), `Features/` (per-feature CQRS folders, empty). No handlers yet.
- **`src/ShopApi.Infrastructure/`**: `Persistence/` (`AppDbContext.cs` + `Configurations/` dirs, empty), `Repositories/`, `UnitOfWork/`, `Migrations/` (empty, `dotnet ef` output target). No implementations yet.
- **`src/ShopApi.API/`**: `Controllers/` (empty; `WeatherForecastController` from template MUST be deleted), `Middleware/` (empty placeholder), `Filters/` (empty placeholder for future `ApiExceptionFilter`). Template `WeatherForecast.cs` MUST be deleted.

---

## 4. Implementation Order

1. Verify prerequisite: `dotnet --version` reports 8.x. If not, install .NET 8 SDK and stop.
2. `dotnet new sln -n ShopApi` at repo root → produces `ShopApi.sln`.
3. `dotnet new classlib -n ShopApi.Domain -o src/ShopApi.Domain -f net8.0` → then set `Nullable enable`, `ImplicitUsings enable` in csproj.
4. `dotnet new classlib -n ShopApi.Application -o src/ShopApi.Application -f net8.0` → same property edits.
5. `dotnet new classlib -n ShopApi.Infrastructure -o src/ShopApi.Infrastructure -f net8.0` → same property edits.
6. `dotnet new webapi -n ShopApi.API -o src/ShopApi.API -f net8.0` (uses controllers by default; do NOT pass `--no-https` — keep HTTPS profile).
7. Delete template noise: `src/ShopApi.API/Controllers/WeatherForecastController.cs`, `src/ShopApi.API/WeatherForecast.cs`.
8. `dotnet sln add` all four csproj files in order: Domain → Application → Infrastructure → API.
9. `dotnet add src/ShopApi.Application reference src/ShopApi.Domain` (Application → Domain).
10. `dotnet add src/ShopApi.Infrastructure reference src/ShopApi.Domain` + `dotnet add src/ShopApi.Infrastructure reference src/ShopApi.Application`.
11. `dotnet add src/ShopApi.API reference src/ShopApi.Application` + `dotnet add src/ShopApi.API reference src/ShopApi.Infrastructure`.
12. Add NuGet packages per csproj spec (§3): Application (MediatR, FluentValidation ×2, AutoMapper); Infrastructure (EF Core ×4); API (Swashbuckle.AspNetCore, DotNetEnv, optionally EFCore.Design). Pin all to latest stable 8-compatible minor at execution time and record exact versions in commit message.
13. Create folder trees (§3, items 13–16) with `.gitkeep` where empty.
14. Write `.env.example`, `.gitignore` (`dotnet new gitignore` + append), `global.json`.
15. Write `appsettings.json`, `appsettings.Development.json`, verify `Properties/launchSettings.json` ports.
16. Write `src/ShopApi.API/Program.cs` per §3 ordered responsibilities (env-first, Swagger, MediatR stub, pipeline order, `partial Program` marker).
17. `dotnet restore` → `dotnet build` (Debug) → fix any warnings/errors → `dotnet build -c Release`.
18. Smoke-run: `cp .env.example .env` (temp, then revert password to placeholder), `dotnet run --project src/ShopApi.API --launch-profile http`, assert `/swagger` 200 and `/swagger/v1/swagger.json` valid JSON, then stop and ensure scratch `.env` is NOT committed.
19. `git status` review: `.env` ignored, `.env.example` tracked, no `bin/`/`obj/` staged.

---

## 5. Code Patterns & Standards

- **Architecture**: Clean Architecture strict layers + one-way dependencies (§2 graph). Composition root ONLY in API. No `new AppDbContext` outside Infrastructure; no EF `using` outside Infrastructure/API registration.
- **CLI-first scaffolding**: All projects/references via `dotnet` CLI (commands in §4), never manual `.sln` GUID edits. Ensures reproducible onboarding docs.
- **Naming conventions**:
  - Projects/assemblies/root namespaces: `ShopApi.<Layer>` (e.g., `ShopApi.Domain`, `ShopApi.Application`).
  - Folders map 1:1 to namespace segments (e.g., `src/ShopApi.Application/Common/Behaviors` → `ShopApi.Application.Common.Behaviors`).
  - Env vars: `UPPER_SNAKE_CASE` (`DB_CONNECTION`). Config keys: PascalCase hierarchies.
  - Placeholders/TODOs: `// TODO(<phase>): <what>` — e.g., `// TODO(phase-03): register Application assembly`.
- **Configuration pattern**: Env wins over `appsettings.json` (`DB_CONNECTION` env > `ConnectionStrings:DefaultConnection`). No hardcoded secrets anywhere; `.env` + user-secrets locally, provider secret store in CI/prod.
- **Error handling (this phase)**: Rely on framework defaults (`ProblemDetails` via `UseExceptionHandler` only from next phase; do NOT add custom middleware now). Ensure `appsettings.Development.json` enables detailed errors for debuggability. Full Result Pattern + `ApiExceptionFilter` + `ProblemDetails` mapping are deferred to Application/API phases — leave TODO comments, not dead code.
- **DI pattern**: Constructor injection only; no service locator. Registrations live in `Program.cs` now, to be extracted to `Infrastructure/DependencyInjection.cs` extension method in a later phase (note as planned refactor, do not pre-create).
- **Swagger pattern**: `AddEndpointsApiExplorer + AddSwaggerGen` in services; `UseSwagger + UseSwaggerUI` gated on Development. No security definitions yet.
- **KISS/DRY/SOLID**: No abstractions without a second consumer in this phase (no generic repository yet, no `Result<T>` yet). Single `Program.cs` composition root; no `Startup.cs` split (obsolete pattern).

---

## 6. Edge Cases & Validation

- **Missing `.env` file**: `Env.Load()` must no-op (DotNetEnv default), app falls back to `appsettings.json` + real env vars. Verify by temporarily renaming `.env` → boot still succeeds. Log a one-line `Console.WriteLine` warning only in Development (optional).
- **Port collision (5000/5001 in use)**: Document fallback `dotnet run --urls http://localhost:5010` and `ASPNETCORE_URLS` override in `.env.example`. Do not hardcode alternate ports in `launchSettings.json`.
- **SDK mismatch (user has .NET 6/7/9 only)**: `global.json` produces clear SDK-not-found error; remediation is installing .NET 8 SDK, not retargeting. Verify `dotnet --version` step (§4.1) catches this first.
- **EF package version drift (e.g., Core 8.x + SqlServer 9.x)**: `dotnet restore` warns `NU1605` or runtime `MissingMethodException` later. Mitigation: pin all four EF packages to identical version in one commit; verification checklist requires version-equality check.
- **Template leftovers**: `WeatherForecastController`/`WeatherForecast.cs` if not deleted cause Swagger to show demo endpoints and confuse Phase 02 validation. Checklist requires their absence.
- **Secret leak**: `.env` committed, or real password in `appsettings.json` / `.env.example`. Mitigations: `.gitignore` negation test, `git diff --cached --name-only` must not list `.env`, `.env.example` password field must literally read `CHANGE_ME`.
- **Swagger in Production**: `UseSwaggerUI` must be Development-gated. Verify Release run (`ASPNETCORE_ENVIRONMENT=Production dotnet run --no-launch-profile`) returns 404 on `/swagger`.
- **HTTPS dev-cert missing (Linux/Docker hosts)**: `UseHttpsRedirection` can cause `ERR_CERT` loops. Note remediation `dotnet dev-certs https --trust`; do not remove redirection in code.
- **Case-sensitive filesystems**: Paths `src/ShopApi.API` vs `src/shopapi.api` diverge on Linux CI. Enforce exact PascalCase everywhere; verify `dotnet sln list` paths match disk.
- **Security**: No `User Secrets` IDs committed with values; CORS not added yet (deferred — adding permissive `AllowAll` now is a vulnerability; leave TODO for restrictive policy in API hardening phase).

---

## 7. Verification Checklist

- [ ] `ShopApi.sln` exists at root; `dotnet sln list` shows exactly 4 projects under `src/`
- [ ] Each csproj targets `net8.0`, has `Nullable enable` + `ImplicitUsings enable`
- [ ] `ShopApi.Domain` has zero `<PackageReference>` entries and zero `<ProjectReference>` entries
- [ ] `ShopApi.Application` references Domain only; has MediatR + FluentValidation (+DI ext) + AutoMapper and nothing else
- [ ] `ShopApi.Infrastructure` references Domain + Application; all EF Core packages share one identical version; Tools/Design marked `PrivateAssets="all"`
- [ ] `ShopApi.API` uses `Microsoft.NET.Sdk.Web`, references Application + Infrastructure, has Swashbuckle.AspNetCore + DotNetEnv
- [ ] Dependency graph verified: `dotnet list src/ShopApi.API reference`, `...Infrastructure reference`, `...Application reference` show §2 graph, no cycles
- [ ] `WeatherForecastController.cs` and `WeatherForecast.cs` deleted; `Controllers/` folder exists but empty
- [ ] Domain/Application/Infrastructure folder trees exist per §3 (with `.gitkeep` where empty)
- [ ] `.env.example` contains `DB_CONNECTION` with `CHANGE_ME`, `ASPNETCORE_ENVIRONMENT`, optional `ASPNETCORE_URLS`; top comment present; no real secret
- [ ] `.env` is gitignored (`git check-ignore -v .env` succeeds); `.env.example` is tracked; `git status --ignored` confirms
- [ ] `global.json` pins SDK 8.x with `rollForward latestMinor`; `dotnet --version` inside repo prints 8.x
- [ ] `Program.cs` loads DotNetEnv FIRST, registers Controllers + Explorer + SwaggerGen + MediatR stub, pipelines Swagger(dev-only) → HttpsRedirection → Authorization → MapControllers → Run, ends with `public partial class Program`
- [ ] `appsettings.json` valid JSON with Logging/AllowedHosts/ConnectionStrings placeholder; `appsettings.Development.json` valid JSON merge
- [ ] `Properties/launchSettings.json` has `http` (port 5000) + `https` (5000+5001) profiles with `launchUrl: swagger`
- [ ] `dotnet restore` clean (0 errors); `dotnet build` Debug + Release succeed with 0 errors and 0 project-config warnings
- [ ] Smoke test: `dotnet run --project src/ShopApi.API --launch-profile http` boots; `GET /swagger` 200 in Development; `GET /swagger/v1/swagger.json` valid OpenAPI; missing-`.env` boot also succeeds
- [ ] Production-gate: with `ASPNETCORE_ENVIRONMENT=Production`, `/swagger` does NOT serve UI
- [ ] `git status` shows no `bin/`, `obj/`, `.env`, or `TestResults/` staged; commit message records exact NuGet versions installed
- [ ] Clean Code: no hardcoded secrets, no dead commented-out business code beyond marked TODO(phase-xx) stubs, file/namespace/folder naming follows §5
