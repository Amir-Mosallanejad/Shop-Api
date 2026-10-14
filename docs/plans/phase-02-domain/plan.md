# Plan — phase-02-domain: Domain Layer

## 1. Overview

### What this phase accomplishes
Builds the entire Domain layer — the heart of the shop model — as pure, persistence-ignorant C# with zero external dependencies. By the end of this phase:
- `ShopApi.Domain` contains 8 entities (`User`, `Category`, `Product`, `ProductImage`, `CartItem`, `Order`, `OrderItem`, `Address`), 1 value object (`Money`), 2 enums (`UserRole`, `OrderStatus`), 1 base entity (`BaseEntity`), and 1 domain exception (`DomainException`).
- All state changes go through constructors and behavior methods that enforce invariants (price/stock/quantity guards, order-status transition guard, main-image invariant).
- All IDs are `Guid`; all timestamps are UTC `DateTime`.
- `dotnet build` succeeds; Domain still has zero `<PackageReference>` and zero `<ProjectReference>`.

### Clean Architecture layers affected
| Layer | Project | State after this phase |
|---|---|---|
| Domain | `src/ShopApi.Domain` | **Fully implemented.** 13 new `.cs` files; 5 obsolete `.gitkeep` files deleted. |
| Application | `src/ShopApi.Application` | Untouched (consumes these types in Phase 03: DTOs map from entities, handlers call behavior methods, `Result<T>` wraps `DomainException`). |
| Infrastructure | `src/ShopApi.Infrastructure` | Untouched (EF configurations for these entities come in Phase 04). |
| API | `src/ShopApi.API` | Untouched. |

### Dependencies on previous phases
- Requires Phase 01 output: `src/ShopApi.Domain/ShopApi.Domain.csproj` (`net8.0`, Nullable+ImplicitUsings enabled) and the 5 folder skeletons. Verified present.
- Known pre-existing deviation (from `phase-01-solution/validation-report.md`): Infrastructure is missing its direct `ShopApi.Domain` project reference. **Not a blocker** for this phase (Domain references nothing), but it must be restored before Phase 04, when Infrastructure maps these entities.
- Forward compatibility: Phase 03 needs repository interfaces over these entities plus an `IJwtService`/`IPasswordHasher` that consume `User` — so `User` must expose `Email`, `PasswordHash`, `Role` with public getters, and `Order` must expose a status type (`OrderStatus`) that `UpdateStatus` commands can set through `ChangeStatus()`.

---

## 2. File-by-File Breakdown

| # | File Path | Action | Layer | Purpose |
|---|---|---|---|---|
| 1 | `src/ShopApi.Domain/Common/BaseEntity.cs` | Create | Domain | Abstract base: `Guid Id`, UTC `CreatedAt`/`UpdatedAt` |
| 2 | `src/ShopApi.Domain/Exceptions/DomainException.cs` | Create | Domain | Base domain exception for invariant violations |
| 3 | `src/ShopApi.Domain/Enums/UserRole.cs` | Create | Domain | `Customer`, `Admin` roles |
| 4 | `src/ShopApi.Domain/Enums/OrderStatus.cs` | Create | Domain | `Pending → Paid → Shipped → Delivered`, plus `Cancelled` |
| 5 | `src/ShopApi.Domain/ValueObjects/Money.cs` | Create | Domain | Non-negative amount + ISO currency; arithmetic operators |
| 6 | `src/ShopApi.Domain/Entities/User.cs` | Create | Domain | Shop user with email, password hash, name, role |
| 7 | `src/ShopApi.Domain/Entities/Address.cs` | Create | Domain | User address-book entry (entity, not value object — see §3) |
| 8 | `src/ShopApi.Domain/Entities/Category.cs` | Create | Domain | Product category tree with slug + optional parent |
| 9 | `src/ShopApi.Domain/Entities/Product.cs` | Create | Domain | Sellable product with `Money` price, stock, images |
| 10 | `src/ShopApi.Domain/Entities/ProductImage.cs` | Create | Domain | Product image URL + main-image flag |
| 11 | `src/ShopApi.Domain/Entities/CartItem.cs` | Create | Domain | User's cart line (product + quantity) |
| 12 | `src/ShopApi.Domain/Entities/Order.cs` | Create | Domain | Customer order aggregate root with items + status machine |
| 13 | `src/ShopApi.Domain/Entities/OrderItem.cs` | Create | Domain | Immutable-after-creation order line with unit-price snapshot |
| 14 | `src/ShopApi.Domain/{Common,Entities,Enums,Exceptions,ValueObjects}/.gitkeep` | Delete | Domain | Remove 5 placeholders superseded by real files |

**Explicit non-goals:** no DTOs, no MediatR handlers, no FluentValidation validators, no AutoMapper profiles, no `Result<T>` (all Phase 03); no `DbContext`/EF attributes/fluent config (Phase 04); no controllers (Phase 05).

---

## 3. Detailed File Specifications

### File: `src/ShopApi.Domain/Common/BaseEntity.cs`
- **Namespace**: `ShopApi.Domain.Common`
- **Type**: `public abstract class BaseEntity`
- **Properties**:
  - `Guid Id { get; protected set; }`
  - `DateTime CreatedAt { get; protected set; }`
  - `DateTime? UpdatedAt { get; protected set; }`
- **Constructors**:
  - `protected BaseEntity()` — sets `Id = Guid.NewGuid()`, `CreatedAt = DateTime.UtcNow`, `UpdatedAt = null`.
- **Methods**:
  - `protected void Touch()` — sets `UpdatedAt = DateTime.UtcNow`. Called by every mutating behavior method in derived entities.
- **Dependencies**: None (BCL only).
- **Validation rules**: `Id` never `Guid.Empty` after construction; `CreatedAt.Kind == DateTimeKind.Utc`.
- **Notes**: No `==`/`GetHashCode` overrides here (identity equality is reference equality; value-object equality lives in `Money`).

### File: `src/ShopApi.Domain/Exceptions/DomainException.cs`
- **Namespace**: `ShopApi.Domain.Exceptions`
- **Type**: `public class DomainException : Exception`
- **Constructors**:
  - `public DomainException(string message)` → `: base(message)`
  - `public DomainException(string message, Exception innerException)` → `: base(message, innerException)`
- **Dependencies**: None.
- **Validation rules**: N/A (transport for invariant violations).
- **Notes**: All guard clauses in entities/value objects throw this (never `ArgumentException`/`InvalidOperationException` directly) so Phase 03's exception filter can map one type to `ProblemDetails`. Do NOT create per-entity exception subclasses in this phase (KISS; revisit only if filtering needs it).

### File: `src/ShopApi.Domain/Enums/UserRole.cs`
- **Namespace**: `ShopApi.Domain.Enums`
- **Type**: `public enum UserRole`
- **Members** (explicit values):
  - `Customer = 0` (default; every registered user starts here)
  - `Admin = 1`
- **Dependencies**: None.
- **Notes**: Explicit values required so EF (Phase 04) stores stable ints even if members are reordered later.

### File: `src/ShopApi.Domain/Enums/OrderStatus.cs`
- **Namespace**: `ShopApi.Domain.Enums`
- **Type**: `public enum OrderStatus`
- **Members** (explicit values, forward-flow order):
  - `Pending = 0`, `Paid = 1`, `Shipped = 2`, `Delivered = 3`, `Cancelled = 4`
- **Dependencies**: None.
- **Notes**: Legal transitions are enforced by `Order.ChangeStatus()` (§Order), not by the enum itself. `Cancelled` is terminal from `Pending`/`Paid` only.

### File: `src/ShopApi.Domain/ValueObjects/Money.cs`
- **Namespace**: `ShopApi.Domain.ValueObjects`
- **Type**: `public sealed record Money` (positional record with custom constructor logic)
- **Properties**:
  - `decimal Amount { get; init; }` — always rounded to 2 decimals, `>= 0`.
  - `string Currency { get; init; }` — 3-letter ISO code, stored uppercase; default `"USD"` via factory.
- **Constructors / factories**:
  - `public Money(decimal amount, string currency)` — validates, rounds (`Math.Round(amount, 2, MidpointRounding.ToZero)` — truncation toward zero, documented), uppercases currency.
  - `public static Money Zero(string currency = "USD")` — factory for `0`.
  - `public static Money From(decimal amount, string currency = "USD")` — convenience factory.
- **Methods / operators**:
  - `Money operator +(Money left, Money right)` — throws `DomainException` on currency mismatch.
  - `Money operator *(Money money, int multiplier)` — throws `DomainException` on negative multiplier.
  - `override string ToString()` → `"{Amount:F2} {Currency}"`.
  - Record value-equality (`==`, `GetHashCode`) is compiler-provided — do NOT hand-roll.
- **Dependencies**: `DomainException`.
- **Validation rules**: amount `>= 0` (negative → `DomainException`); currency non-null, exactly 3 letters (A–Z after uppercasing → else `DomainException`); empty/whitespace → `DomainException`.
- **Notes**: Immutable (`init` only). Single-currency-shop assumption is NOT hardcoded — every operation checks currency equality so multi-currency stays possible.

### File: `src/ShopApi.Domain/Entities/User.cs`
- **Namespace**: `ShopApi.Domain.Entities`
- **Type**: `public sealed class User : BaseEntity`
- **Properties** (all `public get`, `private set` unless noted):
  - `string Email { get; private set; }` — normalized lowercase, trimmed.
  - `string PasswordHash { get; private set; }` — never plaintext; set only via constructor/`ChangePasswordHash`.
  - `string FirstName { get; private set; }`
  - `string LastName { get; private set; }`
  - `UserRole Role { get; private set; }` — default `Customer`.
  - `IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly()` (backed by `private readonly List<Address> _addresses = new()`; navigation only, no EF attributes).
  - `IReadOnlyCollection<Order> Orders => _orders.AsReadOnly()` (same backing pattern).
  - `string FullName => $"{FirstName} {LastName}"` (computed, expression-bodied).
- **Constructors**:
  - `public User(string email, string passwordHash, string firstName, string lastName, UserRole role = UserRole.Customer)` — validates all, normalizes email.
- **Methods**:
  - `void UpdateName(string firstName, string lastName)` (+ `Touch()`).
  - `void ChangePasswordHash(string newPasswordHash)` (+ `Touch()`).
  - `void PromoteToAdmin()` — sets `Role = Admin` (+ `Touch()`); no-op if already admin.
- **Dependencies**: `BaseEntity`, `UserRole`, `DomainException`.
- **Validation rules**: email required, must contain `@` and `.` after `@` (simple format check; full RFC validation is Phase 03 FluentValidation's job); max 256 chars; passwordHash required, min 10 chars (hashes are long — rejects accidental plaintext of typical password length only as sanity check, NOT as security); first/last name required, 1–100 chars.
- **Notes**: No password hashing logic here (that's `IPasswordHasher` in Phase 03). Email uniqueness is a DB constraint (Phase 04), not a domain invariant.

### File: `src/ShopApi.Domain/Entities/Address.cs`
- **Namespace**: `ShopApi.Domain.Entities`
- **Type**: `public sealed class Address : BaseEntity`
- **Design decision**: Entity, NOT value object — the phase prompt lists `Address (Id, UserId, …)` with identity and a `UserId` FK (address book entries are individually referenced by `Order`), so it needs `BaseEntity` identity. No separate `Address` value object in this phase.
- **Properties**:
  - `Guid UserId { get; private set; }` (+ `User? User { get; private set; }` navigation, nullable to allow FK-only construction).
  - `string Province { get; private set; }` (1–100 chars, required).
  - `string City { get; private set; }` (1–100 chars, required).
  - `string PostalCode { get; private set; }` (3–20 chars, required; format check deferred to Phase 03 validators).
  - `string FullAddress { get; private set; }` (5–500 chars, required).
- **Constructors**:
  - `public Address(Guid userId, string province, string city, string postalCode, string fullAddress)`.
- **Methods**:
  - `void Update(string province, string city, string postalCode, string fullAddress)` (+ `Touch()`).
- **Dependencies**: `BaseEntity`, `DomainException`.
- **Validation rules**: `userId != Guid.Empty`; all string rules above; inputs trimmed before length checks.
- **Notes**: Orders snapshot the shipping address at creation (see `Order`) so later edits here never rewrite history.

### File: `src/ShopApi.Domain/Entities/Category.cs`
- **Namespace**: `ShopApi.Domain.Entities`
- **Type**: `public sealed class Category : BaseEntity`
- **Properties**:
  - `string Name { get; private set; }` (2–100 chars, required).
  - `string Slug { get; private set; }` — URL-safe, lowercase; auto-generated from `Name` when not supplied.
  - `Guid? ParentId { get; private set; }` (null = root) + `Category? Parent { get; private set; }`.
  - `IReadOnlyCollection<Category> Children` + `IReadOnlyCollection<Product> Products` (backing `List<T>`, read-only exposure).
- **Constructors**:
  - `public Category(string name, Guid? parentId = null, string? slug = null)` — generates slug via `private static string GenerateSlug(string name)` (lowercase, spaces→`-`, strip non-`[a-z0-9-]`; empty result → `DomainException`).
- **Methods**:
  - `void Rename(string name)` — updates `Name` + regenerates `Slug` (+ `Touch()`).
  - `void SetParent(Guid? parentId)` — rejects self-parenting (`parentId == Id` → `DomainException`) (+ `Touch()`). Cycle detection across levels is an Application concern (needs repository) — document, don't implement.
- **Dependencies**: `BaseEntity`, `DomainException`.
- **Validation rules**: as above; slug 2–120 chars after generation.
- **Notes**: Slug uniqueness is a DB unique index (Phase 04).

### File: `src/ShopApi.Domain/Entities/Product.cs`
- **Namespace**: `ShopApi.Domain.Entities`
- **Type**: `public sealed class Product : BaseEntity`
- **Properties**:
  - `string Name { get; private set; }` (2–200 chars).
  - `string Slug { get; private set; }` (same generation rules as `Category`; own private static generator — small duplication accepted over cross-entity coupling, or share via `internal static` helper on `Category`? Decision: duplicate the 5-line generator; DRY-at-all-costs would couple aggregates).
  - `string Description { get; private set; }` (max 2000 chars; empty allowed → stored as `string.Empty`, never null).
  - `Money Price { get; private set; }` (required; negativity enforced by `Money` itself).
  - `int Stock { get; private set; }` (`>= 0`).
  - `Guid CategoryId { get; private set; }` + `Category? Category { get; private set; }`.
  - `IReadOnlyCollection<ProductImage> Images` (backing list).
- **Constructors**:
  - `public Product(string name, string description, Money price, int stock, Guid categoryId, string? slug = null)`.
- **Methods**:
  - `void UpdateDetails(string name, string description)` — regenerates slug (+ `Touch()`).
  - `void ChangePrice(Money newPrice)` — null → `DomainException` (+ `Touch()`).
  - `void AdjustStock(int delta)` — resulting stock `< 0` → `DomainException` (+ `Touch()`).
  - `void SetCategory(Guid categoryId)` — `Guid.Empty` → `DomainException` (+ `Touch()`).
  - `ProductImage AddImage(string url, bool isMain)` — creates the `ProductImage` (which enforces single-main, see below), appends to backing list (+ `Touch()`); returns the created image.
- **Dependencies**: `BaseEntity`, `Money`, `DomainException`.
- **Validation rules**: `price` non-null; `stock >= 0` at construction; `categoryId != Guid.Empty`; name/slug rules as `Category`.
- **Notes**: Stock decrement on order placement is orchestrated by Application handlers calling `AdjustStock(-qty)` — Product exposes the capability, not the workflow.

### File: `src/ShopApi.Domain/Entities/ProductImage.cs`
- **Namespace**: `ShopApi.Domain.Entities`
- **Type**: `public sealed class ProductImage : BaseEntity`
- **Properties**:
  - `Guid ProductId { get; private set; }` + `Product? Product { get; private set; }`.
  - `string Url { get; private set; }` (required, must start with `http://` or `https://`, max 2000 chars).
  - `bool IsMain { get; private set; }`.
- **Constructors**:
  - `public ProductImage(Guid productId, string url, bool isMain)` — internal? Decision: `public` (Application tests/seed code construct images directly) with full validation.
- **Methods**:
  - `void MarkAsMain()` / `void UnmarkAsMain()` — flag flips (+ `Touch()`). The single-main invariant across a product's images is enforced by `Product.AddImage` / a `Product.SetMainImage(ProductImage)` helper: when an image is added or marked with `isMain: true`, all siblings are unmarked first. Specify `void SetMainImage(Guid imageId)` on `Product` (unknown id → `DomainException`).
- **Dependencies**: `BaseEntity`, `DomainException`.
- **Validation rules**: `productId != Guid.Empty`; URL scheme check; no remote fetching/validation (no I/O in Domain).
- **Notes**: Deleting images is a repository concern (Phase 03/04); Domain only manages the flag.

### File: `src/ShopApi.Domain/Entities/CartItem.cs`
- **Namespace**: `ShopApi.Domain.Entities`
- **Type**: `public sealed class CartItem : BaseEntity`
- **Properties**:
  - `Guid UserId { get; private set; }` + `User? User { get; private set; }`.
  - `Guid ProductId { get; private set; }` + `Product? Product { get; private set; }`.
  - `int Quantity { get; private set; }` (`>= 1`).
- **Constructors**:
  - `public CartItem(Guid userId, Guid productId, int quantity)`.
- **Methods**:
  - `void ChangeQuantity(int quantity)` — `< 1` → `DomainException` (+ `Touch()`).
  - `void Increase(int amount)` / `void Decrease(int amount)` — positive amounts only; decrease below 1 → `DomainException` (removal itself is a repository `Delete`, not a zero-quantity state) (+ `Touch()`).
- **Dependencies**: `BaseEntity`, `DomainException`.
- **Validation rules**: both FKs non-empty; quantity 1–999 (upper bound prevents absurd lines; constant as `private const int MaxQuantity = 999`, not magic).
- **Notes**: One-line-per-(user,product) uniqueness is a DB unique index (Phase 04); stock-availability check is Application-level (needs `Product.Stock`).

### File: `src/ShopApi.Domain/Entities/Order.cs`
- **Namespace**: `ShopApi.Domain.Entities`
- **Type**: `public sealed class Order : BaseEntity` — **aggregate root** for the order cluster (`OrderItem` children are only mutated via `Order`).
- **Properties**:
  - `string OrderNumber { get; private set; }` — generated, format `ORD-yyyyMMdd-XXXXXX` (date + 6-char uppercase alphanumeric random suffix); TODO comment noting DB unique index in Phase 04.
  - `Guid UserId { get; private set; }` + `User? User { get; private set; }`.
  - `Money TotalAmount { get; private set; }` — always recomputed from items, never set directly (private setter, no public mutator).
  - `OrderStatus Status { get; private set; }` — starts `Pending`.
  - `Guid AddressId { get; private set; }` + `Address? Address { get; private set; }` — shipping address **snapshot semantics**: handlers must copy address values at order time (Phase 03); document that mutating the `Address` entity later does not rewrite the order.
  - `IReadOnlyCollection<OrderItem> Items` (backed by `private readonly List<OrderItem> _items`).
- **Constructors**:
  - `private Order(...)` + `public static Order Create(Guid userId, Guid addressId, IEnumerable<(Guid productId, int quantity, Money unitPrice)> lines)` — factory validates (non-empty lines, each qty 1–999, non-null prices with identical currency), creates `OrderItem`s, computes total, generates number. Factory (not constructor) because item creation + total computation is multi-step orchestration.
- **Methods**:
  - `void ChangeStatus(OrderStatus newStatus)` — enforces transition table (+ `Touch()`):
    - `Pending → Paid | Cancelled`; `Paid → Shipped | Cancelled`; `Shipped → Delivered`; `Delivered → (terminal)`; `Cancelled → (terminal)`; same-status is a no-op; anything else → `DomainException`.
  - `Money RecalculateTotal()` — sums `UnitPrice * Quantity` per item (same-currency guaranteed by factory), assigns `TotalAmount`, returns it. Called by factory; `internal` re-exposure only if Phase 03 needs it (default: private, document decision).
- **Dependencies**: `BaseEntity`, `Money`, `OrderStatus`, `OrderItem`, `DomainException`.
- **Validation rules**: `userId`/`addressId` non-empty; ≥1 item; single currency across items; total `>= 0`.
- **Notes**: No `AddItem` post-creation (orders are immutable once placed — changes happen via new orders or cancellation; keeps the aggregate simple and audit-friendly).

### File: `src/ShopApi.Domain/Entities/OrderItem.cs`
- **Namespace**: `ShopApi.Domain.Entities`
- **Type**: `public sealed class OrderItem : BaseEntity`
- **Properties**:
  - `Guid OrderId { get; private set; }` + `Order? Order { get; private set; }` (set post-construction by EF/parent; constructor accepts `orderId` defaulting to `Guid.Empty` with documentation that the aggregate root assigns it — OR constructor takes parent `Order`? Decision: constructor `public OrderItem(Guid orderId, Guid productId, int quantity, Money unitPrice)`; `Order.Create` passes `Guid.Empty` then assigns via `internal void AttachToOrder(Guid orderId)`. Document this two-step clearly for Phase 04 mapping).
  - `Guid ProductId { get; private set; }` + `Product? Product { get; private set; }`.
  - `int Quantity { get; private set; }` (1–999).
  - `Money UnitPrice { get; private set; }` — **snapshot** of `Product.Price` at order time (never a live reference).
  - `Money LineTotal => UnitPrice * Quantity` (computed, no setter).
- **Methods**: none (immutable after creation — no mutators by design).
- **Dependencies**: `BaseEntity`, `Money`, `DomainException`.
- **Validation rules**: `productId != Guid.Empty`; quantity range; `unitPrice` non-null.
- **Notes**: Immutability is the audit trail: price changes on `Product` never alter placed order lines.

---

## 4. Implementation Order

1. `Common/BaseEntity.cs` — everything inherits it; no dependencies.
2. `Exceptions/DomainException.cs` — thrown by everything below.
3. `Enums/UserRole.cs` + `Enums/OrderStatus.cs` — dependency-free; needed by `User`/`Order`.
4. `ValueObjects/Money.cs` — needed by `Product`, `Order`, `OrderItem`.
5. `Entities/User.cs` — no entity dependencies (navigation targets resolved later).
6. `Entities/Address.cs` — depends only on `User`'s id shape (`Guid`).
7. `Entities/Category.cs` — self-referencing only.
8. `Entities/Product.cs` (without image wiring first) + `Entities/ProductImage.cs`, then add `Product.AddImage`/`SetMainImage` referencing `ProductImage`.
9. `Entities/CartItem.cs` — leaf entity.
10. `Entities/OrderItem.cs` — leaf of the order cluster.
11. `Entities/Order.cs` — aggregate root + `Create` factory + status machine (depends on `OrderItem`, `Money`, `OrderStatus`).
12. Delete the 5 now-redundant `.gitkeep` files (`Common`, `Entities`, `Enums`, `Exceptions`, `ValueObjects`).
13. `dotnet build src/ShopApi.Domain` → fix warnings → full `dotnet build`.
14. `git status` review: only the 13 `.cs` files added and 5 `.gitkeep` files deleted; csproj untouched (proves zero new dependencies).

## 5. Code Patterns & Standards

- **DDD building blocks**: Entities (identity + lifecycle + behavior), one Value Object (`Money`: immutable, structural equality, side-effect-free operators), Enums for fixed sets, Aggregate Root (`Order` guards its `OrderItem` collection — external code never holds a mutable reference to `_items`).
- **Encapsulation**: all setters `private`/`protected`; every mutation flows through a named behavior method ending in `Touch()`. No anemic models. Collections exposed as `IReadOnlyCollection<T>` over `private readonly List<T>`.
- **Constructors over object initializers**: required state is constructor-mandatory; invalid instances are unrepresentable. Multi-step creation uses `static Create` factories (`Order`).
- **Guard-clause validation**: `private static` validation helpers per entity (e.g., `EnsureEmail`, `EnsureQuantity`) throwing `DomainException` with messages naming the property and rule. No DataAnnotations (`[Required]`, `[MaxLength]`) — those are presentation/persistence concerns and would couple Domain to `System.ComponentModel.DataAnnotations`.
- **No EF leakage**: no `virtual` for lazy loading, no navigation-configuration attributes, no `DbSet`/`IEntityTypeConfiguration` references, no `Microsoft.EntityFrameworkCore` using anywhere (CI-checkable via grep).
- **Naming**: PascalCase types/members; `ChangeX` for mutations, `Adjust` for deltas, `EnsureX` for guards; `IsMain`, `LineTotal`, `FullName` for computed members; `MaxQuantity = 999`-style `private const` instead of magic numbers; env/config keys untouched in this phase.
- **Error handling**: `DomainException` only (see §3). No `Result<T>` here — Result lives in Application (Phase 03) and maps `DomainException` → failure. No try/catch inside Domain (guards throw; callers handle).
- **Nullability**: `#nullable enable` already on — navigations that may be unloaded are declared nullable (`Category?`, `User?`); required scalars are non-nullable with constructor guarantees. No null-forgiving `!` except backing-field initialization where the compiler can't see the constructor assignment (minimize; prefer inline `= new()`).
- **KISS/DRY/SOLID**: one small exception type; slug generator duplicated once between `Category`/`Product` (conscious, documented); no repository/service interfaces (Phase 03); no generic base beyond `BaseEntity`.

## 6. Edge Cases & Validation

- **Negative/zero money**: `Money` ctor rejects `< 0`; `Zero()` covers freebies. `Product.ChangePrice(null)` → `DomainException` (non-nullable + explicit guard for callers bypassing NRT).
- **Stock underflow**: `AdjustStock` delta taking stock `< 0` → `DomainException`; Application must pre-check and translate to user-facing error.
- **Quantity bounds**: `CartItem`/`OrderItem` enforce 1–999; `Decrease` below 1 throws (caller deletes the line instead — no zero-quantity rows).
- **Order-status abuse**: terminal states (`Delivered`, `Cancelled`) reject all transitions; backward moves (`Paid → Pending`) rejected; same-status is idempotent no-op (safe for retried webhooks in later phases).
- **Multi-currency totals**: `Money.+` and `Order.Create` reject mixed currencies — prevents silent FX bugs; shop is single-currency by data, not by assumption.
- **Self-parent category**: `SetParent(Id)` rejected; deeper cycles documented as Application-level check (needs DB read — impossible in pure Domain).
- **Slug collisions/emptiness**: names producing empty slugs (e.g., `"!!!"`) → `DomainException`; uniqueness enforced by DB index in Phase 04 (note as TODO comment on both slug properties).
- **Orphan FKs**: every `Guid` FK setter rejects `Guid.Empty`; navigation properties nullable so FK-only construction works for EF materialization later.
- **Order snapshot integrity**: `UnitPrice` copied at creation; `Address` documented as snapshot-at-order-time; post-order `Product` price/name edits never mutate history.
- **UTC discipline**: `BaseEntity` stamps `DateTime.UtcNow` only; any `DateTime` parameter entering Domain (none in this phase's API surface) must be normalized — note for Phase 03 DTO mapping (`DateTime.SpecifyKind`).
- **Security**: plaintext passwords can never be stored — `User` accepts only `passwordHash` (naming is the guardrail; length sanity-check rejects obvious plaintext); email normalized to prevent case-variant duplicates; no secrets in code or `.gitkeep` removals.

## 7. Verification Checklist

- [ ] All 13 `.cs` files created under `src/ShopApi.Domain/` with namespaces `ShopApi.Domain.{Common,Entities,Enums,Exceptions,ValueObjects}`
- [ ] 5 obsolete `.gitkeep` files deleted; no other files touched (csproj byte-identical → dependency-free proven)
- [ ] `BaseEntity` stamps non-empty `Guid` + UTC `CreatedAt`; `Touch()` updates `UpdatedAt`
- [ ] `Money` rejects negative amounts and malformed currencies; `+`/`*` enforce same-currency/positive-multiplier; 2-decimal rounding applied
- [ ] Every entity constructor validates required state; every mutator calls `Touch()`; collections exposed read-only
- [ ] `Order.ChangeStatus` enforces the Pending→Paid→Shipped→Delivered / Cancelled-from-Pending-or-Paid table; terminal states immutable
- [ ] `Order.Create` computes `TotalAmount` from items; `OrderItem.UnitPrice` is a snapshot; order lines immutable post-creation
- [ ] `Product` single-main-image invariant managed via `AddImage`/`SetMainImage`; slug generators reject empty results
- [ ] No `using Microsoft.EntityFrameworkCore`, no DataAnnotations, no `virtual` navigations (grep-verified)
- [ ] `dotnet build` (Debug + Release) succeeds with 0 warnings, 0 errors (NU1903 AutoMapper notice is Application-scoped, unaffected)
- [ ] `git status` shows exactly 13 additions + 5 deletions, no modifications
- [ ] Clean Code: DDD building blocks respected, no anemic models, no magic numbers (named consts), `DomainException` as the single failure type
