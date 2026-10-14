# Plan — phase-05-api: API Layer (Controllers + Pipeline)

## 1. Overview

### What this phase accomplishes
Exposes the Application layer over HTTP and completes the vertical wiring of the whole system. By the end of this phase:
- 5 controllers (Auth, Categories, Products, Cart, Orders) map HTTP to the 21 MediatR operations; every response is a `Result` translated to a standardized HTTP response (DTO on success, ProblemDetails with `extensions.code` on failure).
- `GlobalExceptionHandler` (`IExceptionHandler`) converts `DomainException` → 409, FluentValidation leftovers → 400, SQL 2601/2627 + FK-547 `DbUpdateException` → 409, everything else → 500 without leaking internals.
- JWT Bearer authentication, admin-role authorization on catalog writes + order-status changes, CORS for the frontend, fixed-window rate limiting on auth, and a `/health` endpoint (DB check) are all wired.
- Final `Program.cs` registers MediatR (Application assembly), validators, pipeline behavior, AutoMapper, `AddInfrastructure`, auth, Swagger-with-Bearer; Development boots migrate + seed.
- `appsettings.json` holds only non-sensitive values; `JWT_SECRET` template lands in `.env.example`.
- `dotnet build` succeeds; Swagger UI drives the whole API with an Authorize button.

### Clean Architecture layers affected
| Layer | Project | State after this phase |
|---|---|---|
| API | `src/ShopApi.API` | **Fully implemented.** 8 new `.cs` files; `Program.cs` + `appsettings.json` + `.env.example` + csproj modified; 2 `.gitkeep` files deleted (1 kept). |
| Domain / Application / Infrastructure | other `src/*` | Untouched (consumed; the 2 Domain private ctors from Phase 04 stay as-is). |

### Dependencies on previous phases
- Phase 01: project, Swagger baseline, DotNetEnv-first `Program.cs` (rewritten here, order preserved).
- Phase 02: entities (never referenced directly by controllers — only via MediatR request shapes).
- Phase 03: all 21 requests + `Result`/`Error` codes + validators + `ValidationBehavior` (registered here); `ICurrentUserService` fed by JWT claims (binding: `NameIdentifier` + `Admin` role string).
- Phase 04: `AddInfrastructure`, `SeedData`, and its binding requirements on THIS phase: SQL-conflict → 409 mapping, `AddInfrastructure` + Dev seed wiring, `JWT_SECRET` template line, unknown-sort default already in repo (no API work needed).

---

## 2. File-by-File Breakdown

| # | File Path | Action | Layer | Purpose |
|---|---|---|---|---|
| 1 | `src/ShopApi.API/ShopApi.API.csproj` | Modify | API | Add JwtBearer + HealthChecks.EF packages |
| 2 | `src/ShopApi.API/Controllers/BaseApiController.cs` | Create | API | `Result` → HTTP mapping helper |
| 3 | `src/ShopApi.API/Controllers/AuthController.cs` | Create | API | Register + login (anonymous, rate-limited) |
| 4 | `src/ShopApi.API/Controllers/CategoriesController.cs` | Create | API | Category CRUD (reads open, writes admin) |
| 5 | `src/ShopApi.API/Controllers/ProductsController.cs` | Create | API | Product CRUD + filtered/paged list |
| 6 | `src/ShopApi.API/Controllers/CartController.cs` | Create | API | Cart (authenticated users) |
| 7 | `src/ShopApi.API/Controllers/OrdersController.cs` | Create | API | Orders (owner reads, admin status) |
| 8 | `src/ShopApi.API/Middleware/GlobalExceptionHandler.cs` | Create | API | `IExceptionHandler`: Domain/DB/500 mapping |
| 9 | `src/ShopApi.API/Middleware/RequestLoggingMiddleware.cs` | Create | API | Method/path/status/elapsed logging + `UseRequestLogging` ext |
| 10 | `src/ShopApi.API/Program.cs` | Modify | API | Final composition root (full DI + pipeline + dev seed) |
| 11 | `src/ShopApi.API/appsettings.json` | Modify | API | Add `Jwt:ExpiryHours`, `Cors:AllowedOrigins` (non-sensitive only) |
| 12 | `.env.example` | Modify | Cross-cutting | Add `JWT_SECRET` template line |
| 13–14 | 2× `.gitkeep` deletions | Delete | API | `Controllers`, `Middleware` (KEEP `Filters/.gitkeep` — reserved; no filter needed with `IExceptionHandler`) |

**Explicit non-goals:** no new DTO/handler/service code (requests bound directly); no migration files (generated post-phase via the Phase 04 command, now runnable since API wires `AddInfrastructure`); no frontend code; no Docker (Phase 06).

---

## 3. Detailed File Specifications

### Project file

#### File: `ShopApi.API.csproj` (Modify)
- **Changes** (and nothing else):
  1. `Microsoft.AspNetCore.Authentication.JwtBearer` — latest stable 8.x at implementation time (pinned, recorded in commit body).
  2. `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` — latest stable 8.x (for `AddDbContextCheck<AppDbContext>`).
- **Notes**: Rate limiting needs no package (framework `System.Threading.RateLimiting` via Web SDK). `Swashbuckle.AspNetCore` 6.6.2 stays.

### Shared controller base

#### File: `Controllers/BaseApiController.cs`
- **Namespace**: `ShopApi.API.Controllers`
- **Type**: `public abstract class BaseApiController : ControllerBase` with `[ApiController]` on the abstract class (inherited by children so each controller only declares `[Route]`).
- **Method**: `protected ActionResult<T> ToActionResult<T>(Result<T> result)` — mapping table (exact):
  - `IsSuccess` → `Ok(result.Value)` (callers needing 201 use `CreatedAtAction` directly, not this helper).
  - `NotFound` → `NotFound(Problem(...404...))`; `Validation` → `BadRequest(...400...)`; `Conflict` → `Conflict(...409...)`; `Unauthorized` → `Unauthorized(...401...)`; `Forbidden` → `ObjectResult(...403...)` (no `Forbid()` — it triggers auth schemes; a plain 403 ProblemDetails is correct for an authenticated-but-denied caller); unknown code → `ObjectResult(...500...)`.
  - ProblemDetails built via `Problem(detail:, statusCode:, title: Error.Code)` + `extensions["code"] = Error.Code` (so clients switch on a stable machine code, humans read `detail`).
- **Dependencies**: `Result<T>`/`Error` codes only (string-switched; new codes default to 500 — safe).
- **Notes**: `Result<bool>` deletes return `Ok(true)` (uniform envelope over 204 — clients always parse a body on success).

### Controllers (all extend `BaseApiController`; all ctor-inject `ISender` only — never repositories/services; all actions take `CancellationToken`)

#### File: `Controllers/AuthController.cs`
- Route `api/auth`. `[AllowAnonymous]` (whole controller; global default stays open — authorization declared per controller, not globally, to keep Swagger/health frictionless).
- `POST api/auth/register` — `[FromBody] RegisterCommand`, `[EnableRateLimiting("auth")]` → `ToActionResult(await _sender.Send(cmd, ct))` (200 + `AuthResponseDto`). No 201 (register returns a token, not a locatable resource).
- `POST api/auth/login` — `[FromBody] LoginQuery`, same rate limit + mapping.
- **Notes**: Rate limiting is per-IP fixed window (defined in `Program.cs`); validators run in-pipeline before handlers.

#### File: `Controllers/CategoriesController.cs`
- Route `api/categories`.
- `GET api/categories` → `GetAllCategoriesQuery` → 200 list. `GET api/categories/{id:guid}` → `GetCategoryByIdQuery` → 200/404 (used as `CreatedAtAction` target).
- `POST` (`[Authorize(Roles = "Admin")]`, `[FromBody] CreateCategoryCommand`) → `CreatedAtAction(nameof(GetById), new { id }, dto)` (201).
- `PUT {id:guid}` (admin, body `UpdateCategoryRequest(name, parentId)` — small nested record in this file mapping to `UpdateCategoryCommand(id, ...)`) → 200/404/409.
- `DELETE {id:guid}` (admin) → `DeleteCategoryCommand` → 200 `true`/404/409.
- **Decision note**: reads anonymous, writes admin-only (catalog integrity; documented for Phase 08 review).

#### File: `Controllers/ProductsController.cs`
- Route `api/products`.
- `GET api/products` — `[FromQuery]` params (`categoryId?`, `search?`, `minPrice?`, `maxPrice?`, `sortBy?`, `sortDescending=false`, `page=1`, `pageSize=20`) → `GetProductsQuery` → 200 `PagedResult<ProductDto>`.
- `GET api/products/{id:guid}` → details → 200/404 (`CreatedAtAction` target).
- `POST` (admin, `[FromBody] CreateProductCommand` — record bound directly, images included) → 201 via `CreatedAtAction`.
- `PUT {id:guid}` (admin, body `UpdateProductRequest` nested record → `UpdateProductCommand`) → 200/404/409.
- `DELETE {id:guid}` (admin) → 200 `true`/404 (FK-blocked deletes surface as 409 via the exception handler — Phase 04 binding).

#### File: `Controllers/CartController.cs`
- `[Authorize]`, route `api/cart`.
- `GET api/cart` → `GetCartQuery` → 200 (empty cart is 200 with zero totals, never 404).
- `POST api/cart/items` (`[FromBody] AddToCartCommand`) → 200 updated cart / 404 (no product) / 409 (stock) / 401.
- `DELETE api/cart/items/{productId:guid}` → `RemoveFromCartCommand` → 200 updated cart / 404 (line missing).

#### File: `Controllers/OrdersController.cs`
- `[Authorize]`, route `api/orders`.
- `POST api/orders` (`[FromBody] CreateOrderCommand`) → `CreatedAtAction(nameof(GetById), ...)` 201 / 401 / 404 (address) / 409 (empty cart, stock).
- `GET api/orders` (`[FromQuery] page=1, pageSize=20`) → `GetMyOrdersQuery` → 200 paged.
- `GET api/orders/{id:guid}` (`GetOrderByIdQuery`, named `GetById` for `CreatedAtAction`) → 200 / 404 / 403.
- `PATCH api/orders/{id:guid}/status` (`[Authorize(Roles = "Admin")]`, body `UpdateStatusRequest(OrderStatus status)` nested record → `UpdateOrderStatusCommand`) — **admin-only per Phase 03 W4**; 200 / 404 / 409 (illegal transition).

### Middleware

#### File: `Middleware/GlobalExceptionHandler.cs`
- **Namespace**: `ShopApi.API.Middleware`
- **Type**: `public sealed class GlobalExceptionHandler : IExceptionHandler`
- **Method**: `async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)` — always returns true (handled), writes ProblemDetails JSON:
  - `DomainException` → 409, title `Conflict`, detail = message (safe: enum names/ids only).
  - `ValidationException` (FluentValidation escape hatch — should be unreachable behind the behavior) → 400 `Validation`.
  - `DbUpdateException` → inspect inner `SqlException`: numbers 2601/2627 (unique) or 547 (FK) → 409 `Conflict` with generic detail (never raw SQL text); else → 500.
  - `InvalidOperationException` from DI/config fail-fasts at startup → 500 (startup crashes never reach here in practice).
  - default → 500 `InternalError`, detail = exception message ONLY in Development else `"An unexpected error occurred."` (no stack traces to clients; server log carries them).
- **Dependencies**: `IHostEnvironment` (ctor) for the dev-detail switch; `ILogger<GlobalExceptionHandler>` (logs unhandled as Error with TraceId; expected 409s as Warning).
- **Notes**: Registered via `AddExceptionHandler<GlobalExceptionHandler>()` + `app.UseExceptionHandler()` (no `UseExceptionHandler("/error")` controller — the modern minimal path).

#### File: `Middleware/RequestLoggingMiddleware.cs`
- **Type**: `public sealed class RequestLoggingMiddleware` (conventional `InvokeAsync(HttpContext, RequestDelegate)` + `ILogger`): logs `Information` on completion — `{Method} {Path} → {StatusCode} in {ElapsedMs}ms`; query strings excluded (tokens could appear in future query flows — never log `QueryString`).
- **Extension**: `public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) => app.UseMiddleware<RequestLoggingMiddleware>()` in the same file.
- **Notes**: "Optional" per phase — included in minimal form; registered unconditionally (single log line per request is cheap and pays off in Phase 06 docker log inspection).

### Composition root

#### File: `Program.cs` (Modify — full rewrite, preserving DotNetEnv-first order)
- **Ordered specification**:
  1. `using` block: `DotNetEnv`, `MediatR`, `FluentValidation`, `AutoMapper`, `ShopApi.Application.*` (Interfaces for assembly marker, Behaviors, Mappings), `ShopApi.Infrastructure`, `ShopApi.API.Middleware`, JWT/auth namespaces.
  2. `Env.TraversePath().Load();` FIRST (unchanged).
  3. `builder.Configuration.AddEnvironmentVariables();` + keep the `_ = DB_CONNECTION ?? GetConnectionString` probe (documents env-wins; `AddInfrastructure` re-resolves and throws).
  4. Services in order: `AddControllers()`; `AddEndpointsApiExplorer()`; `AddSwaggerGen` with `SwaggerDoc("v1", ShopApi/v1)` (keep) + Bearer security definition (`http`, `bearer`, JWT) + global security requirement (Authorize button in UI); `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IUnitOfWork).Assembly))` (**repointed** — resolves the Phase 01 TODO; `IUnitOfWork` is the stable Application marker); `AddValidatorsFromAssembly(same)`; `AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))` (Phase 03 W2 binding); `AddAutoMapper(typeof(MappingProfile).Assembly)`; `AddInfrastructure(builder.Configuration)`; JWT auth — `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new() { ValidateIssuer=false, ValidateAudience=false, ValidateLifetime=true, ValidateIssuerSigningKey=true, IssuerSigningKey=new SymmetricSecurityKey(keyBytes), ClockSkew=2min })` where `keyBytes` re-resolves `JWT_SECRET`-first exactly like `JwtService` (duplication acknowledged in comment — single source would couple API to service internals); `AddAuthorization()` (role checks via `Roles=` attributes, no custom policies — KISS); CORS policy `"frontend"` — origins from `Cors:AllowedOrigins` (empty array default → CORS effectively closed until configured); `AddRateLimiter` with `"auth"` fixed-window policy (10 req/min per IP, `QueueLimit=0`, 429 auto); `AddHealthChecks().AddDbContextCheck<AppDbContext>("database")`; `AddExceptionHandler<GlobalExceptionHandler>()`; `AddProblemDetails()`.
  5. `var app = builder.Build();`
  6. Pipeline order: `UseExceptionHandler()`; `UseRequestLogging()`; `if (IsDevelopment()) { UseSwagger(); UseSwaggerUI(); }`; `UseHttpsRedirection()`; `UseCors("frontend")`; `UseRateLimiter()`; **`UseAuthentication()`** (resolves the Phase 01 TODO — MUST precede authorization); `UseAuthorization()`; `MapControllers()`; `MapHealthChecks("/health")` (anonymous; JSON with `database` entry status).
  7. Development bootstrap AFTER pipeline mapping, BEFORE `Run`: `if (IsDevelopment()) { using scope → await db.Database.MigrateAsync(ct); await SeedData.SeedAsync(db, ct); }` (Development-only auto-migrate + seed; prod stays manual — Phase 04 decision honored).
  8. `app.Run();` + `public partial class Program { }` retained.
- **Line budget**: ~120 lines (composition only, no business logic).

### Configuration files

#### File: `appsettings.json` (Modify — additive only)
- Add (keep everything existing): `"Jwt": { "ExpiryHours": 24 }` (non-sensitive knob matching `JwtService` override key), `"Cors": { "AllowedOrigins": ["http://localhost:3000"] }` (sensible frontend default; empty array also valid). NO secrets, NO connection-string changes.

#### File: `.env.example` (Modify — append)
- `JWT_SECRET=CHANGE_ME_AT_LEAST_32_CHARS_LONG_SECRET` (placeholder fails the 32-char gate only if used verbatim? It passes length but must be replaced — comment `# Replace with a strong random secret (min 32 chars). Never commit .env.`; generation hint: `openssl rand -base64 48` in comment).

---

## 4. Implementation Order

1. Modify `ShopApi.API.csproj` (2 packages) → `dotnet restore`.
2. `Controllers/BaseApiController.cs` (mapping contract everything uses).
3. `Middleware/GlobalExceptionHandler.cs` (failure contract for the whole API).
4. `Controllers/AuthController.cs` (proves auth + rate-limit + mapping vertical).
5. `Controllers/CategoriesController.cs` → `ProductsController.cs` (catalog pair).
6. `Controllers/CartController.cs` → `OrdersController.cs` (authenticated pair; status endpoint admin-gated).
7. `Middleware/RequestLoggingMiddleware.cs` (+ extension, same file).
8. Rewrite `Program.cs` per §3 order (resolving both Phase 01 TODOs; deleting the obsolete MediatR-Program-assembly comment).
9. Modify `appsettings.json` (+2 sections) and `.env.example` (+`JWT_SECRET`).
10. Delete `Controllers/.gitkeep` + `Middleware/.gitkeep` (KEEP `Filters/.gitkeep`).
11. `dotnet build` → fix → `dotnet build -c Release`.
12. Smoke-run WITHOUT database: boot with `DB_CONNECTION` pointing nowhere should still start (no startup DB touch outside Development block); `GET /health` returns 503 `Unhealthy` (DB down) but the app serves — proves pipeline order; `GET /swagger` 200 in Development; `POST /api/auth/register` with `{}` → 400 Validation (proves behavior+validators wired); then stop. Full DB-backed run deferred to Phase 06 (needs MSSQL).
13. Grep-verification: no `new AppDbContext`, no hardcoded secrets/JWT key, `UseAuthentication` precedes `UseAuthorization` textually, `typeof(IUnitOfWork).Assembly` present, no `typeof(Program).Assembly` in MediatR line.
14. `git status` review: 8 additions + 4 modifications + 2 deletions, 0 other changes.

## 5. Code Patterns & Standards

- **Thin controllers**: `ISender`-only injection; actions construct requests from route/body/query and return `ToActionResult`/`CreatedAtAction` — zero branching beyond that (handlers own all decisions).
- **Records as wire contracts**: MediatR request records bound directly (`[FromBody]`/`[FromQuery]`/route) — no parallel request-DTO layer (single source of truth; only `UpdateXxxRequest` adapters where route-id + body must merge).
- **Centralized HTTP semantics**: status table lives in ONE method (`ToActionResult`); `Error.Code` strings drive it (new codes fail safe to 500); ProblemDetails `extensions.code` gives clients a stable contract.
- **Defense in depth**: validators (shape) → handlers (rules) → Domain (invariants) → exception handler (511/409/500 translation) — each layer assumes the previous can be bypassed.
- **AuthN before AuthZ, always**: pipeline order + `[Authorize]` on user data + `Roles="Admin"` on catalog writes/status changes; login/register explicitly anonymous + rate-limited (credential-stuffing resistance).
- **Fail-closed configuration**: empty CORS origins = closed; missing DB/secret = startup crash (from `AddInfrastructure`/`JwtService`); weak secret rejected; 500s hide internals outside Development.
- **KISS**: no custom auth policies, no versioning, no response-envelope wrapper (status code + DTO/ProblemDetails IS the envelope), no AutoMapper in controllers (handlers return ready DTOs), conventional middleware over `IMiddleware` activation (no per-request DI needed).

## 6. Edge Cases & Validation

- **Validation failures**: empty-body register → 400 with `code: Validation` (proves behavior wired; response must list the message, never stack).
- **Duplicate email/slug races**: handler pre-checks + unique indexes → `DbUpdateException` 2601/2627 → 409 via handler (NOT 500, NOT raw SQL text).
- **Deleting referenced products**: FK-547 → 409 with generic message; cart/order history intact.
- **Anonymous cart/order access**: 401 via handler (`ICurrentUserService` null) AND via `[Authorize]` (belt + suspenders; attribute fires first).
- **Cross-user order read**: 403 (not 404 — existence already confirmed to an authenticated caller is acceptable; Phase 03 decision kept).
- **Illegal status transition**: 409 with Domain message (enum names only — safe).
- **Expired/tampered tokens**: 401 from JwtBearer middleware before any handler runs; `ValidateToken` null-path is defense-in-depth for any manual use.
- **Rate-limit breach**: 429 on 11th auth attempt/minute/IP (legit users unaffected; document tuning knob location).
- **CORS misconfiguration**: unknown origins get no `Access-Control-Allow-Origin` (browser blocks; server logs normally) — secure default, no wildcard+credentials footgun.
- **DB down**: `/health` 503 with `database: Unhealthy`, app stays up (probes, not crashes); Development auto-migrate only runs when a server answers.
- **Swagger in Production**: UI + JSON stay Development-gated (existing behavior preserved); Bearer definition ships in the doc regardless (harmless metadata).
- **Token in logs**: request logger excludes query strings; auth endpoints log like any other (bodies never logged); JWT lifetimes bounded 24h default.

## 7. Verification Checklist

- [ ] All 8 `.cs` files created with namespaces `ShopApi.API.{Controllers,Middleware}`; `BaseApiController` carries `[ApiController]`
- [ ] 5 controllers expose exactly the planned routes/verbs/auth attributes; status endpoint admin-gated; creates return 201 with `Location`
- [ ] `ToActionResult` maps all 5 `Error` codes per table; unknown codes → 500; `extensions.code` present on every failure
- [ ] `GlobalExceptionHandler` maps Domain→409, FV-escape→400, SQL 2601/2627/547→409, default→500 with dev-only detail; logs Error/Warning appropriately
- [ ] `Program.cs`: DotNetEnv first; MediatR repointed to Application assembly; validators + behavior + AutoMapper + `AddInfrastructure` registered; `UseAuthentication` before `UseAuthorization`; rate limiter + CORS + health + exception handler wired; Dev migrate+seed block; `partial Program` retained
- [ ] `appsettings.json` gains only `Jwt:ExpiryHours` + `Cors:AllowedOrigins`; `.env.example` gains `JWT_SECRET` template; no secrets committed
- [ ] csproj gains exactly the 2 planned packages (pinned 8.x); no other diffs
- [ ] 2 `.gitkeep` files deleted; `Filters/.gitkeep` retained
- [ ] `dotnet build` Debug + Release: 0 errors, no NEW warnings
- [ ] Smoke-run (no DB): boot OK; `/health` 503-Unhealthy; `/swagger` 200 (Dev); `POST register {}` → 400 Validation
- [ ] Grep-verified: no `new AppDbContext`, no hardcoded secrets, auth-before-authorization order, no `typeof(Program).Assembly` in MediatR registration
- [ ] `git status`: 8 additions + 4 modifications + 2 deletions, 0 other changes
