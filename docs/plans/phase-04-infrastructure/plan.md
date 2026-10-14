# Plan — phase-04-infrastructure: Infrastructure Layer (EF Core + Services)

## 1. Overview

### What this phase accomplishes
Implements everything the Application layer depends on but cannot provide itself. By the end of this phase:
- `AppDbContext` maps all 8 Domain entities via 8 `IEntityTypeConfiguration<T>` classes (tables, keys, owned `Money`, indexes, check constraints, delete behaviors).
- `GenericRepository<T>` + 4 specific repositories + `UnitOfWork` implement every Application contract, honoring the binding requirements from the Phase 03 validation report (unknown-`SortBy` default, email normalization, populated navigations).
- `PasswordHasher` (PBKDF2, BCL-only), `JwtService` (HMAC-SHA256, fail-fast secret), `CurrentUserService` (claims-based) implement the service contracts.
- `AddInfrastructure(IServiceCollection, IConfiguration)` registers the whole layer; connection string resolved env-first per the phase mandate.
- `SeedData` provides idempotent dev categories/products; `AppDbContextFactory` enables design-time migrations without API wiring.
- The exact `dotnet ef migrations add` CLI command is documented; migration files are NOT generated (per phase instruction).
- The missing direct `Infrastructure → Domain` reference (Phase 01 deviation) is restored, plus one new package and one framework reference.
- `dotnet build` succeeds.

### Clean Architecture layers affected
| Layer | Project | State after this phase |
|---|---|---|
| Infrastructure | `src/ShopApi.Infrastructure` | **Fully implemented.** 20 new `.cs` files; csproj modified (Domain ref + 1 package + 1 framework ref); 3 `.gitkeep` files deleted (1 kept). |
| Domain | `src/ShopApi.Domain` | Untouched (mapped + constructed, never modified). |
| Application | `src/ShopApi.Application` | Untouched (its interfaces are now implemented; its binding requirements are honored — see §3 per-file notes). |
| API | `src/ShopApi.API` | Untouched (`AddInfrastructure` is CALLED in Phase 05; `Program.cs` keeps its TODOs). |

### Dependencies on previous phases
- Phase 01: project + EF Core 8.0.32 packages + folder skeleton. Verified present. **Fixes the known deviation**: restores the direct Domain `ProjectReference`.
- Phase 02: exact entity shapes (private setters, `Money`, `Order.Create` two-step with `AttachToOrder`, `BaseEntity` timestamps). Configurations must map private-setter properties and the owned `Money` type.
- Phase 03: all 9 contracts + binding requirements from `phase-03-application/validation-report.md` (W3/W4): unknown `SortBy` → creation-date ordering; email trim+lowercase parity with `User`; `ValidateToken` returns null (never throws); order items cascade on order insert; deleted-product name tolerance relies on Restrict (see §6).
- Phase 05 (forward): calls `AddInfrastructure`, runs `SeedData` in Development, maps `DbUpdateException` FK violations to 409 (see §6), enforces admin policy on update-status.

---

## 2. File-by-File Breakdown

| # | File Path | Action | Layer | Purpose |
|---|---|---|---|---|
| 1 | `src/ShopApi.Infrastructure/ShopApi.Infrastructure.csproj` | Modify | Infrastructure | Restore Domain ref; add Jwt package + AspNetCore framework ref |
| 2 | `src/ShopApi.Infrastructure/Persistence/AppDbContext.cs` | Create | Infrastructure | DbContext: 8 DbSets, `ApplyConfigurationsFromAssembly` |
| 3 | `src/ShopApi.Infrastructure/Persistence/AppDbContextFactory.cs` | Create | Infrastructure | Design-time factory for `dotnet ef` without API wiring |
| 4 | `src/ShopApi.Infrastructure/Persistence/Configurations/UserConfiguration.cs` | Create | Infrastructure | Users table, unique email, relations |
| 5 | `src/ShopApi.Infrastructure/Persistence/Configurations/AddressConfiguration.cs` | Create | Infrastructure | Addresses table + FK |
| 6 | `src/ShopApi.Infrastructure/Persistence/Configurations/CategoryConfiguration.cs` | Create | Infrastructure | Categories table, unique slug, self-FK |
| 7 | `src/ShopApi.Infrastructure/Persistence/Configurations/ProductConfiguration.cs` | Create | Infrastructure | Products table, owned Money, unique slug, stock check |
| 8 | `src/ShopApi.Infrastructure/Persistence/Configurations/ProductImageConfiguration.cs` | Create | Infrastructure | ProductImages table + FK |
| 9 | `src/ShopApi.Infrastructure/Persistence/Configurations/CartItemConfiguration.cs` | Create | Infrastructure | CartItems table, unique (User,Product), quantity check |
| 10 | `src/ShopApi.Infrastructure/Persistence/Configurations/OrderConfiguration.cs` | Create | Infrastructure | Orders table, owned total, unique number |
| 11 | `src/ShopApi.Infrastructure/Persistence/Configurations/OrderItemConfiguration.cs` | Create | Infrastructure | OrderItems table, owned unit price, quantity check |
| 12 | `src/ShopApi.Infrastructure/Repositories/GenericRepository.cs` | Create | Infrastructure | Generic CRUD implementation (no-tracking reads) |
| 13 | `src/ShopApi.Infrastructure/Repositories/ProductRepository.cs` | Create | Infrastructure | Paging/filter/sort implementation |
| 14 | `src/ShopApi.Infrastructure/Repositories/OrderRepository.cs` | Create | Infrastructure | User-scoped order queries |
| 15 | `src/ShopApi.Infrastructure/Repositories/UserRepository.cs` | Create | Infrastructure | Email lookup with Domain-parity normalization |
| 16 | `src/ShopApi.Infrastructure/Repositories/CartRepository.cs` | Create | Infrastructure | Cart lines with products + set-based clear |
| 17 | `src/ShopApi.Infrastructure/UnitOfWork/UnitOfWork.cs` | Create | Infrastructure | Repository aggregator + atomic save |
| 18 | `src/ShopApi.Infrastructure/Services/PasswordHasher.cs` | Create | Infrastructure | PBKDF2 hash/verify (BCL only) |
| 19 | `src/ShopApi.Infrastructure/Services/JwtService.cs` | Create | Infrastructure | HMAC-SHA256 token service |
| 20 | `src/ShopApi.Infrastructure/Services/CurrentUserService.cs` | Create | Infrastructure | Claims-based caller identity |
| 21 | `src/ShopApi.Infrastructure/DependencyInjection.cs` | Create | Infrastructure | `AddInfrastructure` registration extension |
| 22 | `src/ShopApi.Infrastructure/Persistence/SeedData.cs` | Create | Infrastructure | Idempotent dev seed (categories + products) |
| 23–25 | 3× `.gitkeep` deletions | Delete | Infrastructure | `Persistence/Configurations`, `Repositories`, `UnitOfWork` (KEEP `Migrations/.gitkeep` — no migration files are generated in this phase) |

**Explicit non-goals:** no migration `.cs` files (command documented only, §4 step 12); no `Program.cs`/controller/middleware changes (Phase 05); no DTO/handler changes (frozen); no soft-delete columns (rejected — see §5).

---

## 3. Detailed File Specifications

### Project file

#### File: `ShopApi.Infrastructure.csproj` (Modify)
- **Changes** (and nothing else):
  1. Add `<ProjectReference Include="..\ShopApi.Domain\ShopApi.Domain.csproj" />` (restores Phase 01 deviation; required for entity/configuration compilation).
  2. Add `<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.x" />` (latest stable 8.x at implementation time — pinned and recorded in commit body; needed ONLY by `JwtService`).
  3. Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (provides `IHttpContextAccessor` for `CurrentUserService` without a legacy Abstractions package).
- **Validation**: `dotnet restore` clean; no other package/ref diffs.

### DbContext

#### File: `Persistence/AppDbContext.cs`
- **Namespace**: `ShopApi.Infrastructure.Persistence`
- **Type**: `public sealed class AppDbContext : DbContext`
- **Constructors**: `public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)`
- **DbSets** (expression-bodied getters, exact names): `Users`, `Addresses`, `Categories`, `Products`, `ProductImages`, `CartItems`, `Orders`, `OrderItems` — all `DbSet<T>`.
- **Methods**: `protected override void OnModelCreating(ModelBuilder modelBuilder)` — single statement `modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)` (configurations discovered, never hand-listed — new entities can't be forgotten).
- **Notes**: NO `SaveChangesAsync` audit override — `BaseEntity.UpdatedAt` is written exclusively by Domain `Touch()` (single-writer rule; stamping here would mark freshly Added rows as Updated). NO `OnConfiguring` (connection comes from DI; design-time path uses the factory).

#### File: `Persistence/AppDbContextFactory.cs`
- **Namespace**: `ShopApi.Infrastructure.Persistence`
- **Type**: `public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>`
- **Method**: `AppDbContext CreateDbContext(string[] args)` — resolves `DB_CONNECTION` env var (missing → throw `InvalidOperationException` naming the variable), builds `DbContextOptionsBuilder<AppDbContext>.UseSqlServer(...)`, returns context.
- **Notes**: Exists solely so the §4 migration command runs before Phase 05 wires `AddInfrastructure`. Never used at runtime.

### Entity configurations (all `public sealed class XConfiguration : IEntityTypeConfiguration<X>`, namespace `ShopApi.Infrastructure.Persistence.Configurations`)

Conventions applying to ALL: table names plural (see below); `Id` = key, `ValueGeneratedNever` (Domain generates Guids — database must NOT override); `CreatedAt` required, `UpdatedAt` optional; no soft-delete columns (rejected, §5).

#### File: `Configurations/UserConfiguration.cs`
- Table `Users`. `Email` required, max 256, unique index `IX_Users_Email`. `PasswordHash` required, max 500. `FirstName`/`LastName` required, max 100. `Role` stored as int (EF default enum mapping — no converter needed; explicit enum values from Phase 02 keep stored ints stable).
- Relations: `Addresses` one-to-many (`UserId`, **cascade** — address book dies with user); `Orders` one-to-many (`UserId`, **Restrict** — order history survives; user with orders cannot be deleted); `CartItems` one-to-many (`UserId`, **cascade** — transient state).

#### File: `Configurations/AddressConfiguration.cs`
- Table `Addresses`. `Province`/`City` required max 100; `PostalCode` required max 20; `FullAddress` required max 500. Index on `UserId`.

#### File: `Configurations/CategoryConfiguration.cs`
- Table `Categories`. `Name` required max 100. `Slug` required max 120, unique index `IX_Categories_Slug`. Self-reference `Parent`/`Children` via `ParentId`, delete behavior **Restrict** (SQL Server forbids cascade cycles; matches handler's subcategory guard).

#### File: `Configurations/ProductConfiguration.cs`
- Table `Products`. `Name` required max 200. `Slug` required max 220, unique index `IX_Products_Slug` (the duplicate-slug backstop required by Phase 03 handlers). `Description` required (empty string allowed) max 2000.
- `Money Price` mapped as owned type: `OwnsOne(p => p.Price, b => { b.Property(m => m.Amount).HasColumnName("Price").HasColumnType("decimal(18,2)").IsRequired(); b.Property(m => m.Currency).HasColumnName("Currency").HasColumnType("char(3)").IsRequired().HasMaxLength(3); })`.
- `Stock` required + check constraint `CK_Products_Stock_NonNegative` (`Stock >= 0`, defense in depth behind the Domain guard).
- Relations: `Category` many-to-one (`CategoryId`, index, **Restrict** — matches handler's has-products guard); `Images` one-to-many (**cascade**).

#### File: `Configurations/ProductImageConfiguration.cs`
- Table `ProductImages`. `Url` required max 2000. `IsMain` required, default `false` (`HasDefaultValue(false)`). Index on `ProductId`; cascade from `Product`.

#### File: `Configurations/CartItemConfiguration.cs`
- Table `CartItems`. Unique composite index `IX_CartItems_User_Product` on (`UserId`, `ProductId`) (the one-line-per-pair invariant). `Quantity` required + check constraint `CK_CartItems_Quantity_Range` (`Quantity BETWEEN 1 AND 999`). FKs: `User` **cascade**, `Product` **cascade** (cart lines are transient — product deletion cleans them; order history is unaffected since orders snapshot prices).

#### File: `Configurations/OrderConfiguration.cs`
- Table `Orders`. `OrderNumber` required max 32, unique index `IX_Orders_OrderNumber`. `Status` stored as int. `Money TotalAmount` owned: columns `TotalAmount decimal(18,2)` + `Currency char(3)`.
- Relations: `User` many-to-one (`UserId`, index, **Restrict** — history preservation); `Address` many-to-one (`AddressId`, **Restrict** — address edits/deletes never rewrite orders); `Items` one-to-many (**cascade** — lines are created with the order via `Orders.AddAsync` and die with it).

#### File: `Configurations/OrderItemConfiguration.cs`
- Table `OrderItems`. `Quantity` required + check `CK_OrderItems_Quantity_Range` (1–999). `Money UnitPrice` owned: columns `UnitPrice decimal(18,2)` + `Currency char(3)`.
- Relations: `Order` many-to-one (**cascade**, see above); `Product` many-to-one (`ProductId`, index, **Restrict** — placed lines survive product deletion; enables the mapping's `"Unknown"` tolerance path).

### Repositories (namespace `ShopApi.Infrastructure.Repositories`)

#### File: `Repositories/GenericRepository.cs`
- **Type**: `public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity` (non-sealed — inherited by specific repos).
- **State**: `protected readonly AppDbContext Context`; `protected DbSet<T> Set => Context.Set<T>()`; ctor `GenericRepository(AppDbContext context)`.
- **Methods**: `GetByIdAsync` → `Set.FindAsync(new object[] { id }, ct)`; `ListAllAsync` → `Set.AsNoTracking().ToListAsync(ct)`; `FindAsync` → `Set.AsNoTracking().Where(predicate).ToListAsync(ct)`; `ExistsAsync` → `AnyAsync`; `CountAsync` → predicate null ? `CountAsync()` : `CountAsync(predicate)`; `AddAsync` → `Set.AddAsync`; `Update` → `Set.Update`; `Remove` → `Set.Remove`.
- **Notes**: Reads are `AsNoTracking` (handlers reattach via `Update`); tracking-load variants live ONLY in specific repos where navigations are needed.

#### File: `Repositories/ProductRepository.cs`
- **Type**: `public sealed class ProductRepository : GenericRepository<Product>, IProductRepository`
- **Methods**:
  - `GetByIdWithDetailsAsync` — `Set.Include(p => p.Images).Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id, ct)` (tracking load: handler mutates then `Update`).
  - `GetBySlugAsync` — same includes, predicate on `Slug`.
  - `SlugExistsAsync` — `AnyAsync(p => p.Slug == slug && (!excludeId.HasValue || p.Id != excludeId.Value), ct)`.
  - `CountByCategoryAsync` — `CountAsync(p => p.CategoryId == categoryId, ct)`.
  - `GetPagedAsync(ProductFilter filter, ct)` — binding requirement honoring Phase 03 W3: base query with `Include Images + Category`; filters: `CategoryId` equality; `Search` → `EF.Functions.Like(p.Name, %term%)` with `%`/`_`/`[` escaped (specify escape helper); `MinPrice`/`MaxPrice` on `p.Price.Amount`; sort switch on `filter.SortBy?.ToLowerInvariant()`: `"price"` → `Price.Amount`, `"name"` → `Name`, default (null/empty/unknown incl. `"created"`) → `CreatedAt`; `SortDescending` flips each; `TotalCount = await CountAsync()` on filtered query; `Skip((page-1)*size).Take(size)` with defensive re-clamp (`page < 1 → 1`, size clamp 1–100); returns `new PagedResult<Product>(items, page, size, total)`.
- **Dependencies**: EF Core, `ProductFilter`, `PagedResult<Product>`.

#### File: `Repositories/OrderRepository.cs`
- **Type**: `public sealed class OrderRepository : GenericRepository<Order>, IOrderRepository`
- **Methods**:
  - `GetByIdWithItemsAsync` — `Include(o => o.Items).ThenInclude(i => i.Product)`, `Include(o => o.Address)`, by id.
  - `GetByOrderNumberAsync` — same includes, predicate on `OrderNumber`.
  - `GetByUserPagedAsync(userId, page, size, ct)` — `Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt)`, same clamp + `PagedResult<Order>` pattern (include `Items` for total display; `Product` not needed for list view — document).

#### File: `Repositories/UserRepository.cs`
- **Type**: `public sealed class UserRepository : GenericRepository<User>, IUserRepository`
- **Methods**: `GetByEmailAsync` / `EmailExistsAsync` — normalize input EXACTLY as `User` does (`Trim().ToLowerInvariant()`) before comparing (binding requirement, Phase 03 W3); document that any future change to `User` normalization must change this file in lockstep.

#### File: `Repositories/CartRepository.cs`
- **Type**: `public sealed class CartRepository : GenericRepository<CartItem>, ICartRepository`
- **Methods**:
  - `GetByUserIdAsync` — `Include(l => l.Product).ThenInclude(p => p.Images)`, `Where(l => l.UserId == userId)`.
  - `GetLineAsync` — `FirstOrDefaultAsync(l => l.UserId == userId && l.ProductId == productId, ct)`.
  - `ClearUserCartAsync` — `await Set.Where(l => l.UserId == userId).ExecuteDeleteAsync(ct)` (set-based; bypasses tracker — safe: lines are leaf rows with no in-memory dependents).

#### File: `UnitOfWork/UnitOfWork.cs`
- **Namespace**: `ShopApi.Infrastructure.UnitOfWork`
- **Type**: `public sealed class UnitOfWork : IUnitOfWork`
- **State**: `private readonly AppDbContext _context`; repo properties constructed ONCE in ctor from `_context` (`Products = new ProductRepository(_context)` etc.; `Categories = new GenericRepository<Category>(_context)`; `Addresses = new GenericRepository<Address>(_context)`).
- **Methods**: `SaveChangesAsync(ct)` → `_context.SaveChangesAsync(ct)`; `Dispose()` → `_context.Dispose()` (+ full `Dispose(bool)` pattern? Decision: simple `Dispose() => _context.Dispose()` — context is the only resource; document).
- **Notes**: Single `SaveChangesAsync` per handler = the atomicity the Phase 03 order flow relies on.

### Services (namespace `ShopApi.Infrastructure.Services`)

#### File: `Services/PasswordHasher.cs`
- **Type**: `public sealed class PasswordHasher : IPasswordHasher`
- **Algorithm**: PBKDF2 (`Rfc2898DeriveBytes` with SHA-512, BCL-only — no new package). Consts: `Iterations = 210_000`, `SaltSize = 16`, `HashSize = 64`.
- **Methods**: `Hash(password)` — empty → `ArgumentException` (programmer error, not domain failure); random salt via `RandomNumberGenerator`; format `$PBKDF2-SHA512${Iterations}${Base64(salt)}${Base64(hash)}` (self-describing for future iteration upgrades). `Verify(password, passwordHash)` — parse failures / shape mismatch → `false` (never throw on attacker input); compare with `CryptographicOperations.FixedTimeEquals`.
- **Notes**: Work factor (~200ms/log-in) is acceptable for auth endpoints (Phase 05 rate-limits auth anyway).

#### File: `Services/JwtService.cs`
- **Type**: `public sealed class JwtService : IJwtService`
- **State**: `private readonly byte[] _key`; `private readonly TimeSpan _lifetime` (default 24h; overridable via `Jwt:ExpiryHours` config); ctor `(IConfiguration configuration)`: secret = `Environment.GetEnvironmentVariable("JWT_SECRET") ?? configuration["Jwt:Secret"]`; missing/whitespace or `< 32` chars → `InvalidOperationException` naming `JWT_SECRET` (fail fast at startup, never a weak default).
- **Methods**: `GenerateToken(User user)` — claims `sub=user.Id`, `email`, `role=user.Role.ToString()`, `jti=new Guid`; HMAC-SHA256; expiry UtcNow + lifetime. `ValidateToken(string token)` — validates signature + lifetime (2-min clock skew), no issuer/audience enforcement in this phase (documented stub for Phase 06 hardening); null/whitespace input → null; ANY exception → null (binding requirement — never throws).
- **Dependencies**: `System.IdentityModel.Tokens.Jwt` (new package), `IConfiguration`, `User`.

#### File: `Services/CurrentUserService.cs`
- **Type**: `public sealed class CurrentUserService : ICurrentUserService`
- **State**: ctor `(IHttpContextAccessor httpContextAccessor)`.
- **Properties**: `UserId` → `Guid.TryParse(FindFirst(ClaimTypes.NameIdentifier)?.Value)` else null; `IsAuthenticated` → `HttpContext?.User.Identity?.IsAuthenticated is true`; `IsAdmin` → `IsInRole("Admin")` (role claim string must equal `UserRole.Admin.ToString()` — the exact string `JwtService` writes; document the coupling).
- **Notes**: Null `HttpContext` (background tasks) → all-false/null, never throws.

### Composition

#### File: `DependencyInjection.cs`
- **Namespace**: `ShopApi.Infrastructure`
- **Type**: `public static class DependencyInjection`
- **Method**: `public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)`
  1. `var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION") ?? configuration.GetConnectionString("DefaultConnection")`; null/whitespace → `InvalidOperationException("Database connection string is not configured. Set the DB_CONNECTION environment variable.")`.
  2. `services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()))` (retry resists docker-compose startup races in Phase 06).
  3. `AddScoped<IUnitOfWork, UnitOfWork>()`, `AddScoped<IPasswordHasher, PasswordHasher>()`, `AddScoped<IJwtService, JwtService>()`, `AddScoped<ICurrentUserService, CurrentUserService>()`.
  4. `services.AddHttpContextAccessor()`; `return services`.
- **Notes**: Called by Phase 05 `Program.cs`, not here. No MediatR/FluentValidation/AutoMapper registrations (Application wiring is also Phase 05).

#### File: `Persistence/SeedData.cs`
- **Namespace**: `ShopApi.Infrastructure.Persistence`
- **Type**: `public static class SeedData`
- **Method**: `public static async Task SeedAsync(AppDbContext context, CancellationToken ct = default)` — idempotent: early-return unless `!await context.Categories.AnyAsync(ct)`; seeds 3 categories (`Electronics`/`electronics`, `Books`/`books`, `Clothing`/`clothing`) + 4 products (Laptop Pro 1299.99, Wireless Mouse 29.99, C# in Depth 49.99, Cotton T-Shirt 19.99 — USD, positive stock, 1–2 images each with `https://example.com/…` URLs and exactly one main). Uses Domain constructors only (guards run on seed too).
- **Notes**: Invoked from Phase 05 `Program.cs` in Development only; never from migrations. Prices are illustrative test data, not secrets.

---

## 4. Implementation Order

1. Modify `ShopApi.Infrastructure.csproj` (Domain ref + Jwt package + FrameworkReference) → `dotnet restore`.
2. `Persistence/AppDbContext.cs` (DbSets + assembly scan).
3. `Persistence/Configurations/` — `User`, `Address`, `Category`, `Product`, `ProductImage`, `CartItem`, `Order`, `OrderItem` (any order; each self-contained).
4. `Repositories/GenericRepository.cs` → `ProductRepository` → `OrderRepository` → `UserRepository` → `CartRepository`.
5. `UnitOfWork/UnitOfWork.cs`.
6. `Services/PasswordHasher.cs` → `Services/JwtService.cs` → `Services/CurrentUserService.cs`.
7. `DependencyInjection.cs`.
8. `Persistence/SeedData.cs` + `Persistence/AppDbContextFactory.cs`.
9. Delete 3 obsolete `.gitkeep` files (KEEP `Migrations/.gitkeep`).
10. `dotnet build` (Debug) → fix → `dotnet build -c Release`.
11. Verify `AddInfrastructure` compiles into a scratch host? Decision: NO scratch code in repo (would be an extra file) — verification is `dotnet build` + Phase 05 wiring. Instead run a design-time check: `dotnet ef --version` presence + document the migration command (step 12).
12. Document (in commit body / Phase 05 handoff — NOT as a file) the initial-migration command: `dotnet ef migrations add InitialCreate --project src/ShopApi.Infrastructure --startup-project src/ShopApi.API --output-dir Migrations`. NOTE: runnable only after Phase 05 calls `AddInfrastructure` (API is the startup project) OR via the factory — state both paths. Do NOT run it (phase instruction).
13. Grep-verification: no `ShopApi.API` references, no hardcoded connection strings/secrets, `DB_CONNECTION`/`JWT_SECRET` referenced but never assigned in code.
14. `git status` review: 20 additions + 1 modification + 3 deletions, 0 other changes.

## 5. Code Patterns & Standards

- **Repository + UoW**: generic base for CRUD, specific repos for query shapes, `UnitOfWork` as the single construction point; one save per use case (atomicity without explicit transactions — EF's `SaveChanges` transaction suffices; `TransactionScope` deferred until a cross-context need appears).
- **Persistence ignorance (one-way)**: Infrastructure references Domain + Application; configurations use Fluent API ONLY (zero attributes on entities — Domain stays clean); owned types map `Money` without the value object knowing EF exists.
- **No-tracking reads, explicit attach-on-write**: `GenericRepository` reads `AsNoTracking`; tracking loads only where handlers mutate (`GetByIdWithDetailsAsync`, cart lines). Rationale documented in code comments (stale-write awareness for Phase 05 reviewers).
- **Fail-fast configuration**: missing `DB_CONNECTION` / weak `JWT_SECRET` throw `InvalidOperationException` at startup with the variable name — never silent defaults, never weak fallbacks.
- **Security**: PBKDF2-SHA512/210k + random salt + fixed-time compare; JWT HMAC-SHA256 with ≥256-bit secret; `ValidateToken` total-function (null, never throw); role strings coupled exactly (`Admin`); password/token/hash material never logged (no logging statements in services at all).
- **No soft delete** (conscious rejection): phase says "if needed" — order history already preserves facts via Restrict + snapshots; deleted users/products are blocked by Restrict where history exists and cascaded where state is transient. Documented so Phase 08 reviewers don't re-litigate.
- **Naming**: `XxxConfiguration`, `XxxRepository`, `...Service` impls matching interface names; tables/indexes/constraints in `IX_`/`CK_` conventions; `AddInfrastructure` extension naming matches .NET conventions.
- **KISS**: no specification pattern, no generic UoW magic, no retry-policy tuning beyond `EnableRetryOnFailure`, seed data minimal and idempotent.

## 6. Edge Cases & Validation

- **Duplicate slug/email races**: app-level pre-checks (Phase 03) + DB unique indexes (`IX_Products_Slug`, `IX_Categories_Slug`, `IX_Users_Email`, `IX_Orders_OrderNumber`) as backstops; unique-violation `DbUpdateException` surfaces to Phase 05 middleware, which MUST map SQL error 2601/2627 to 409 Conflict (binding requirement on Phase 05).
- **Deleting referenced products**: `OrderItems.Product` Restrict preserves history — deleting an ever-ordered product fails at `SaveChanges`; Phase 05 must surface as 409 (same mapping as above), NOT 500. Cart lines cascade (transient).
- **Deleting users with history**: `Orders.User` Restrict blocks; addresses/cart cascade. Admin UI (future) must explain the block — handler returns success only when the DB accepts.
- **Self-referencing categories**: self-FK Restrict + handler guard; deep cycles remain possible (documented Phase 03 limitation — needs recursive CTE check, out of scope).
- **Unknown sort fields**: repository defaults to `CreatedAt DESC` (binding requirement, Phase 03 W3) — never throws on novel input.
- **Email parity drift**: `UserRepository` normalization duplicates `User`'s rule; any Domain change must update both (comment cross-reference in both files).
- **Empty-cart / empty-order edge**: enforced in Application; DB allows zero-item orders structurally (no constraint) — acceptable, creation path is the only writer.
- **Connection resilience**: `EnableRetryOnFailure` for docker-compose cold starts; migrations verità: initial migration runs against a live MSSQL (Phase 06 provides it) — `dotnet ef database update` stays manual/CI-owned, never app-startup behavior (no `Migrate()` call in `AddInfrastructure` — deliberate, avoids privilege surprises in prod).
- **Secret handling**: `DB_CONNECTION`/`JWT_SECRET` read, never written/logged/defaulted; `.env.example` update (JWT_SECRET template line) is a Phase 05 task alongside Swagger/CORS — flagged, not done here (API-owned file).
- **Clock skew**: 2-minute tolerance in token validation; order numbers use UTC date (multi-day-boundary collisions impossible due to random suffix + unique index).

## 7. Verification Checklist

- [ ] All 20 `.cs` files created with namespaces `ShopApi.Infrastructure.{Persistence,Persistence.Configurations,Repositories,UnitOfWork,Services}` (+ root `DependencyInjection`)
- [ ] csproj modified ONLY with Domain ref + Jwt package + FrameworkReference; all EF packages still identical 8.0.32
- [ ] 3 obsolete `.gitkeep` files deleted; `Migrations/.gitkeep` retained; no migration files generated
- [ ] All 8 configurations applied via assembly scan; table/column/index/check names exactly as specified
- [ ] `Money` owned in 3 places with exact column names (`Price`/`Currency`, `TotalAmount`/`Currency`, `UnitPrice`/`Currency`); amounts `decimal(18,2)`
- [ ] Delete behaviors exactly: cascade (Addresses, CartItems, Images, OrderItems-from-Order, cart-from-user/product) vs. Restrict (Orders-from-User/Address, Products-from-Category, OrderItems-from-Product, self-Category)
- [ ] Repositories honor Phase 03 bindings: sort default, email parity, populated navigations, set-based cart clear
- [ ] `PasswordHasher` verifies its own hashes (round-trip); `JwtService` generate→validate round-trips; `ValidateToken` nulls (never throws) on garbage
- [ ] `AddInfrastructure` throws with variable names on missing config; no `Migrate()` call; no hardcoded secrets
- [ ] `SeedData` idempotent (double-run safe); uses Domain constructors only
- [ ] `dotnet build` Debug + Release: 0 errors, no NEW warnings (NU1903 carried)
- [ ] Grep-verified: no `ShopApi.API` refs, no hardcoded connection strings, `DB_CONNECTION`/`JWT_SECRET` read-only
- [ ] `git status`: 20 additions + 1 modification + 3 deletions, 0 other changes
- [ ] Migration CLI command documented for Phase 05+ execution; NOT executed (phase instruction)
