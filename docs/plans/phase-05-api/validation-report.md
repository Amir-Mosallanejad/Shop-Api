# Validation Report: phase-05-api

## Summary

- **Status**: ✅ PASSED
- **Build Status**: ✅ (`dotnet build` Debug + Release: 0 errors, no new warnings)
- **Test Status**: N/A (no test projects; no-DB smoke run in build phase: boot OK, `/health` 503-Unhealthy, `/swagger/v1/swagger.json` 200, `POST /api/auth/register {}` 400)

## Files Checked

| # | Planned File | Exists | Matches Plan | Notes |
|---|---|---|---|---|
| 1 | `ShopApi.API.csproj` (Modify) | ✅ | ✅ | +JwtBearer 8.0.32 +HealthChecks.EF 8.0.32, nothing else |
| 2 | `Controllers/BaseApiController.cs` | ✅ | ✅ | `[ApiController]` abstract base, exact 5-code table + 500 default, `extensions.code` |
| 3 | `Controllers/AuthController.cs` | ✅ | ✅ | Anonymous, both actions rate-limited |
| 4 | `Controllers/CategoriesController.cs` | ✅ | ✅ | Open reads, admin writes, 201 with Location |
| 5 | `Controllers/ProductsController.cs` | ✅ | ✅ | Query-bound paging/filter, admin writes, 201 |
| 6 | `Controllers/CartController.cs` | ✅ | ✅ | `[Authorize]`, get/add/remove shapes |
| 7 | `Controllers/OrdersController.cs` | ✅ | ✅ | Owner reads, **admin-gated** status PATCH, 201 on create |
| 8 | `Middleware/GlobalExceptionHandler.cs` | ✅ | ✅ | Domain→409, FV-escape→400, SQL 2601/2627/547→409, dev-only 500 detail, Error/Warning logging |
| 9 | `Middleware/RequestLoggingMiddleware.cs` | ✅ | ✅ | Method/path/status/elapsed, no query-string logging, extension in-file |
| 10 | `Program.cs` (Modify) | ✅ | ✅ | DotNetEnv-first; MediatR repointed to `IUnitOfWork` assembly; validators+behavior+AutoMapper+Infrastructure; auth before AuthZ; CORS/rate-limit/health/handler wired; Dev migrate+seed; `partial Program` kept |
| 11 | `appsettings.json` (Modify) | ✅ | ✅ | Only +`Jwt:ExpiryHours` +`Cors:AllowedOrigins`; no secrets |
| 12 | `.env.example` (Modify) | ✅ | ✅ | +`JWT_SECRET` template with rotation hint |
| 13–14 | 2× `.gitkeep` deletions | ✅ | ✅ | `Filters/.gitkeep` retained |

## ✅ Passed

- All 8 files exist with exact namespaces (`ShopApi.API.Controllers`, `ShopApi.API.Middleware`), type names, routes, verbs, and auth attributes (full attribute sweep across 5 controllers).
- `ToActionResult` implements the specified table; unknown codes fail safe to 500; `Result<bool>` deletes return uniform 200 bodies.
- Creates return 201 with `Location` (3× `CreatedAtAction` verified); reads return 200; empty cart is 200 (never 404).
- Pipeline order verified textually: ExceptionHandler → RequestLogging → Swagger(dev) → Https → CORS → RateLimiter → Authentication (line 121) → Authorization (122) → Controllers + `/health`.
- Both Phase 01 TODOs resolved (`typeof(IUnitOfWork).Assembly`; `UseAuthentication` present); no `typeof(Program).Assembly` in MediatR registration (grep-verified).
- No `new AppDbContext`, no hardcoded secrets/keys (grep-verified); CORS closed-by-default (empty-origins fallback); Swagger Bearer definition present.
- `git status`: 8 additions + 4 modifications + 2 deletions, 0 other changes.

## ❌ Failed / Missing

- None.

## ⚠️ Warnings

1. **Pre-existing NU1903 (AutoMapper advisory)** — carried, out of scope.
2. **Two defects found and fixed during BUILD (not plan deviations, recorded for traceability)**: (a) duplicate route NAME `GetById` across 3 controllers crashed startup — route names removed (`CreatedAtAction` resolves by action name); (b) unconditional Development `MigrateAsync` crashed boot without a database — Dev bootstrap is now warn-and-continue, so the app serves with `/health` Unhealthy (matches the plan's "probes, not crashes" intent). Both fixes verified by the passing smoke run.
3. **Phase 06 bindings created**: docker-compose must provide MSSQL + `DB_CONNECTION`/`JWT_SECRET`; initial migration still ungenerated (`dotnet ef migrations add InitialCreate --project src/ShopApi.Infrastructure --startup-project src/ShopApi.API`); full DB-backed auth→cart→order flow untested until then.
4. **Rate-limit tuning**: 10 req/min/IP on auth is a starting point; revisit after observing login UX and brute-force telemetry in staging.

## 📋 Clean Code Checklist

- [x] SOLID principles followed (ISender-only controllers; handler/middleware single responsibilities; `IExceptionHandler` abstraction)
- [x] No code duplication (DRY) — one mapping method, one exception mapper, no per-controller status logic
- [x] Simple solutions (KISS) — no custom policies/versioning/envelopes; records bound directly; conventional middleware
- [x] Meaningful names (route templates, `ToActionResult`, `MapHealthChecks("/health")`)
- [x] No magic numbers/strings (permit/window/queue, skew, expiry, policy/route names as literals only where framework requires)
- [x] Proper error handling (Result→ProblemDetails contract complete; exception handler covers Domain/validation/SQL/500; no token/query material in logs)
- [x] All planned files exist
- [x] Namespaces match plan
- [x] Build succeeds with no errors
- [x] No compiler warnings (except pre-existing NU1903)

## 🔧 Recommended Fixes

1. None required for this phase. Next: Phase 06 (Docker + live MSSQL) to run the first DB-backed end-to-end pass and generate the initial migration.
2. Per AGENTS.md Commit Rules: commit atomically (e.g., `feat(api): add controllers and HTTP pipeline` + `docs(plans): add phase-05 plan and report`), staging explicitly, showing diffs and awaiting user confirmation first.
