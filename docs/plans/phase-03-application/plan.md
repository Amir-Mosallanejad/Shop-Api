# Plan — phase-03-application: Application Layer (CQRS)

## 1. Overview

### What this phase accomplishes

Implements the full Application layer with CQRS + MediatR on top of the Phase 02 Domain model. By the end of this phase:

- `Result` / `Result<T>` / `Error` / `PagedResult<T>` shared kernel types exist.
- Repository + UnitOfWork + service contracts (`IJwtService`, `IPasswordHasher`, `ICurrentUserService`) are declared (
  implementations come in Phase 04).
- Shared response DTOs + one AutoMapper `MappingProfile` exist.
- A `ValidationBehavior` MediatR pipeline runs FluentValidation validators for every command (and id-queries).
- 21 operations are implemented as MediatR request + handler pairs: Auth (2), Categories (5), Products (5), Cart (3),
  Orders (4).
- `dotnet build` succeeds; Application references only Domain (+ its 4 NuGet packages).

### Clean Architecture layers affected

| Layer          | Project                      | State after this phase                                                                                                                             |
|----------------|------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|
| Application    | `src/ShopApi.Application`    | **Fully implemented.** ~54 new `.cs` files; 5 obsolete `.gitkeep` files deleted (1 kept).                                                          |
| Domain         | `src/ShopApi.Domain`         | Untouched (consumed: entities, `Money`, enums, `DomainException`, `BaseEntity`).                                                                   |
| Infrastructure | `src/ShopApi.Infrastructure` | Untouched, but its Phase 04 work is fully prescribed by the interfaces declared here.                                                              |
| API            | `src/ShopApi.API`            | Untouched (Phase 05 wires controllers to these MediatR requests; `Program.cs` MediatR registration must be repointed to the Application assembly). |

### Dependencies on previous phases

- Phase 01: project, NuGet set (MediatR 12.4.1, FluentValidation 11.12.0 ×2, AutoMapper 13.0.1), folder skeleton.
  Verified present.
- Phase 02: all 8 entities + `Money` + enums + `DomainException` with exact member shapes from
  `phase-02-domain/plan.md` (handlers call `User(...)`, `Order.Create(...)`, `ChangeStatus`, `AdjustStock`, `AddImage`,
  cart mutators — all must exist with the planned signatures).
- Forward contracts: Phase 04 implements every interface in `Interfaces/`; Phase 05 maps HTTP to the requests in
  `Features/` and maps `Result` → HTTP responses. Request/response/DTO shapes defined here are frozen API for those
  phases.

---

## 2. File-by-File Breakdown

| #     | File Path                                                                       | Action | Layer       | Purpose                                                                                                                                                                          |
|-------|---------------------------------------------------------------------------------|--------|-------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1     | `src/ShopApi.Application/Common/Wrappers/Error.cs`                              | Create | Application | `Error` record + well-known codes/factories                                                                                                                                      |
| 2     | `src/ShopApi.Application/Common/Wrappers/Result.cs`                             | Create | Application | Non-generic `Result` base + generic `Result<T>`                                                                                                                                  |
| 3     | `src/ShopApi.Application/Common/Wrappers/PagedResult.cs`                        | Create | Application | Paginated list envelope                                                                                                                                                          |
| 4     | `src/ShopApi.Application/Common/Behaviors/ValidationBehavior.cs`                | Create | Application | MediatR pipeline running FluentValidation                                                                                                                                        |
| 5     | `src/ShopApi.Application/Interfaces/IGenericRepository.cs`                      | Create | Application | Generic CRUD/query contract                                                                                                                                                      |
| 6     | `src/ShopApi.Application/Interfaces/IProductRepository.cs`                      | Create | Application | Product paging/filter/slug contract                                                                                                                                              |
| 7     | `src/ShopApi.Application/Interfaces/IOrderRepository.cs`                        | Create | Application | Order user-scoped query contract                                                                                                                                                 |
| 8     | `src/ShopApi.Application/Interfaces/IUserRepository.cs`                         | Create | Application | User email-lookup contract                                                                                                                                                       |
| 9     | `src/ShopApi.Application/Interfaces/ICartRepository.cs`                         | Create | Application | Cart line query/clear contract                                                                                                                                                   |
| 10    | `src/ShopApi.Application/Interfaces/IUnitOfWork.cs`                             | Create | Application | Repository aggregator + atomic save                                                                                                                                              |
| 11    | `src/ShopApi.Application/Interfaces/IJwtService.cs`                             | Create | Application | Token generation/validation contract                                                                                                                                             |
| 12    | `src/ShopApi.Application/Interfaces/IPasswordHasher.cs`                         | Create | Application | Password hash/verify contract                                                                                                                                                    |
| 13    | `src/ShopApi.Application/Interfaces/ICurrentUserService.cs`                     | Create | Application | Current caller identity contract                                                                                                                                                 |
| 14    | `src/ShopApi.Application/Common/Dtos/UserDtos.cs`                               | Create | Application | `UserDto`, `AuthResponseDto`                                                                                                                                                     |
| 15    | `src/ShopApi.Application/Common/Dtos/CategoryDtos.cs`                           | Create | Application | `CategoryDto`                                                                                                                                                                    |
| 16    | `src/ShopApi.Application/Common/Dtos/ProductDtos.cs`                            | Create | Application | `ProductDto`, `ProductImageDto`                                                                                                                                                  |
| 17    | `src/ShopApi.Application/Common/Dtos/CartDtos.cs`                               | Create | Application | `CartDto`, `CartItemDto`                                                                                                                                                         |
| 18    | `src/ShopApi.Application/Common/Dtos/OrderDtos.cs`                              | Create | Application | `OrderDto`, `OrderItemDto`                                                                                                                                                       |
| 19    | `src/ShopApi.Application/Common/Dtos/AddressDtos.cs`                            | Create | Application | `AddressDto`                                                                                                                                                                     |
| 20    | `src/ShopApi.Application/Mappings/MappingProfile.cs`                            | Create | Application | AutoMapper entity→DTO profile                                                                                                                                                    |
| 21    | `src/ShopApi.Application/Features/Auth/RegisterCommand.cs`                      | Create | Application | Register request + handler                                                                                                                                                       |
| 22    | `src/ShopApi.Application/Features/Auth/LoginQuery.cs`                           | Create | Application | Login request + handler                                                                                                                                                          |
| 23    | `src/ShopApi.Application/Features/Auth/RegisterCommandValidator.cs`             | Create | Application | Register rules                                                                                                                                                                   |
| 24    | `src/ShopApi.Application/Features/Auth/LoginQueryValidator.cs`                  | Create | Application | Login rules                                                                                                                                                                      |
| 25    | `src/ShopApi.Application/Features/Categories/CreateCategoryCommand.cs`          | Create | Application | Create category + handler                                                                                                                                                        |
| 26    | `src/ShopApi.Application/Features/Categories/UpdateCategoryCommand.cs`          | Create | Application | Update category + handler                                                                                                                                                        |
| 27    | `src/ShopApi.Application/Features/Categories/DeleteCategoryCommand.cs`          | Create | Application | Delete category + handler                                                                                                                                                        |
| 28    | `src/ShopApi.Application/Features/Categories/GetAllCategoriesQuery.cs`          | Create | Application | List categories + handler                                                                                                                                                        |
| 29    | `src/ShopApi.Application/Features/Categories/GetCategoryByIdQuery.cs`           | Create | Application | Category by id + handler                                                                                                                                                         |
| 30    | `src/ShopApi.Application/Features/Categories/CreateCategoryCommandValidator.cs` | Create | Application | Create rules                                                                                                                                                                     |
| 31    | `src/ShopApi.Application/Features/Categories/UpdateCategoryCommandValidator.cs` | Create | Application | Update rules                                                                                                                                                                     |
| 32    | `src/ShopApi.Application/Features/Categories/DeleteCategoryCommandValidator.cs` | Create | Application | Delete rules                                                                                                                                                                     |
| 33    | `src/ShopApi.Application/Features/Categories/GetCategoryByIdQueryValidator.cs`  | Create | Application | Id rules                                                                                                                                                                         |
| 34    | `src/ShopApi.Application/Features/Products/CreateProductCommand.cs`             | Create | Application | Create product + handler                                                                                                                                                         |
| 35    | `src/ShopApi.Application/Features/Products/UpdateProductCommand.cs`             | Create | Application | Update product + handler                                                                                                                                                         |
| 36    | `src/ShopApi.Application/Features/Products/DeleteProductCommand.cs`             | Create | Application | Delete product + handler                                                                                                                                                         |
| 37    | `src/ShopApi.Application/Features/Products/GetProductByIdQuery.cs`              | Create | Application | Product by id + handler                                                                                                                                                          |
| 38    | `src/ShopApi.Application/Features/Products/GetProductsQuery.cs`                 | Create | Application | Filtered/paged products + handler (+ `ProductFilter` record)                                                                                                                     |
| 39    | `src/ShopApi.Application/Features/Products/CreateProductCommandValidator.cs`    | Create | Application | Create rules                                                                                                                                                                     |
| 40    | `src/ShopApi.Application/Features/Products/UpdateProductCommandValidator.cs`    | Create | Application | Update rules                                                                                                                                                                     |
| 41    | `src/ShopApi.Application/Features/Products/DeleteProductCommandValidator.cs`    | Create | Application | Delete rules                                                                                                                                                                     |
| 42    | `src/ShopApi.Application/Features/Products/GetProductByIdQueryValidator.cs`     | Create | Application | Id rules                                                                                                                                                                         |
| 43    | `src/ShopApi.Application/Features/Cart/AddToCartCommand.cs`                     | Create | Application | Add/increase cart line + handler                                                                                                                                                 |
| 44    | `src/ShopApi.Application/Features/Cart/RemoveFromCartCommand.cs`                | Create | Application | Remove cart line + handler                                                                                                                                                       |
| 45    | `src/ShopApi.Application/Features/Cart/GetCartQuery.cs`                         | Create | Application | Current cart + handler                                                                                                                                                           |
| 46    | `src/ShopApi.Application/Features/Cart/AddToCartCommandValidator.cs`            | Create | Application | Add rules                                                                                                                                                                        |
| 47    | `src/ShopApi.Application/Features/Cart/RemoveFromCartCommandValidator.cs`       | Create | Application | Remove rules                                                                                                                                                                     |
| 48    | `src/ShopApi.Application/Features/Orders/CreateOrderCommand.cs`                 | Create | Application | Place order from cart + handler                                                                                                                                                  |
| 49    | `src/ShopApi.Application/Features/Orders/GetMyOrdersQuery.cs`                   | Create | Application | Paged own orders + handler                                                                                                                                                       |
| 50    | `src/ShopApi.Application/Features/Orders/GetOrderByIdQuery.cs`                  | Create | Application | Order by id (owner/admin) + handler                                                                                                                                              |
| 51    | `src/ShopApi.Application/Features/Orders/UpdateOrderStatusCommand.cs`           | Create | Application | Change order status + handler                                                                                                                                                    |
| 52    | `src/ShopApi.Application/Features/Orders/CreateOrderCommandValidator.cs`        | Create | Application | Create rules                                                                                                                                                                     |
| 53    | `src/ShopApi.Application/Features/Orders/UpdateOrderStatusCommandValidator.cs`  | Create | Application | Status rules                                                                                                                                                                     |
| 54    | `src/ShopApi.Application/Features/Orders/GetOrderByIdQueryValidator.cs`         | Create | Application | Id rules                                                                                                                                                                         |
| 55–59 | 5× `.gitkeep` deletions                                                         | Delete | Application | `Interfaces`, `Mappings`, `Features`, `Common/Behaviors`, `Common/Wrappers` placeholders (KEEP `Common/Exceptions/.gitkeep` — reserved, no exception types needed in this phase) |

**Explicit non-goals:** no repository/UnitOfWork/service implementations, no `DbContext`, no migrations (Phase 04); no
controllers/middleware/`Program.cs` changes (Phase 05); no `Result → HTTP` mapping (Phase 05).

---

## 3. Detailed File Specifications

### Shared kernel

#### File: `Common/Wrappers/Error.cs`

- **Namespace**: `ShopApi.Application.Common.Wrappers`
- **Type**: `public sealed record Error(string Code, string Message)`
- **Static factories**: `NotFound(string message)`, `Validation(string message)`, `Conflict(string message)`,
  `Unauthorized(string message)`, `Forbidden(string message)` — each stamps its fixed `Code` (`"NotFound"`,
  `"Validation"`, `"Conflict"`, `"Unauthorized"`, `"Forbidden"`).
- **Dependencies**: None.
- **Notes**: Codes are plain strings (not enums) so Phase 05 can serialize them directly into ProblemDetails
  `extensions.code`.

#### File: `Common/Wrappers/Result.cs`

- **Namespace**: `ShopApi.Application.Common.Wrappers`
- **Types**:
    - `public abstract record Result` (non-generic base): `bool IsSuccess { get; }`, `bool IsFailure => !IsSuccess`,
      `Error? Error { get; }`; `protected Result(bool isSuccess, Error? error)`; static `Success()` + generic
      `Success<T>(T value)` / `Failure<T>(Error error)` factories living on the base so the pipeline behavior can
      construct typed failures.
    - `public sealed record Result<T> : Result`: `T? Value { get; }`;
      `private Result(T? value, bool isSuccess, Error? error)`; implicit conversion NOT provided (explicit `Success`/
      `Failure` only — keeps failure paths visible).
- **Dependencies**: `Error`.
- **Notes**: Records (not classes) for structural equality in handler unit tests later. `Value` nullable-annotated;
  handlers guarantee non-null on success by construction.

#### File: `Common/Wrappers/PagedResult.cs`

- **Namespace**: `ShopApi.Application.Common.Wrappers`
- **Type**: `public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)`
- **Computed**: `int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize)` (guard `PageSize <= 0` → treat as
  validation failure upstream, never construct invalid here; still, `TotalPages` must not throw on `PageSize == 0` —
  specify `PageSize <= 0 ? 0 : ...`).
- **Dependencies**: None.

#### File: `Common/Behaviors/ValidationBehavior.cs`

- **Namespace**: `ShopApi.Application.Common.Behaviors`
- **Type**:
  `public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull where TResponse : Result`
- **Method**: `Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)` —
  gathers all `IValidator<TRequest>` from DI; zero validators → `next()`; else runs all, takes first failure, returns
  typed failure via `Result.Failure<TValue>(Error.Validation(...))` constructed through `typeof(TResponse)`
  generic-argument reflection (the ONE sanctioned reflection spot; comment must explain why: C# cannot express
  `where TResponse : Result<TValue>`).
- **Dependencies**: MediatR, FluentValidation, `Result`/`Error`.
- **Notes**: Registration (`AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))`) happens in Phase
  05 `Program.cs`, not here. Behavior never throws on validation failure — it returns `Failure`.

### Repository & service contracts

#### File: `Interfaces/IGenericRepository.cs`

- **Namespace**: `ShopApi.Application.Interfaces`
- **Type**: `public interface IGenericRepository<T> where T : BaseEntity`
- **Methods** (all `CancellationToken ct = default`):
    - `Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)`
    - `Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct = default)`
    - `Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)`
    - `Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)`
    - `Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)`
    - `Task AddAsync(T entity, CancellationToken ct = default)`; `void Update(T entity)`; `void Remove(T entity)`
- **Dependencies**: `BaseEntity`, `System.Linq.Expressions` (BCL — allowed).
- **Notes**: `Update`/`Remove` are sync (they only change tracking state). No `SaveChanges` here — that belongs to
  `IUnitOfWork` so multi-repo operations stay atomic.

#### File: `Interfaces/IProductRepository.cs`

- **Type**: `public interface IProductRepository : IGenericRepository<Product>`
- **Methods**:
    - `Task<Product?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)` (images + category loaded —
      loading strategy is Phase 04's choice; contract only promises populated navigations).
    - `Task<Product?> GetBySlugAsync(string slug, CancellationToken ct = default)`
    - `Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)`
    - `Task<PagedResult<Product>> GetPagedAsync(ProductFilter filter, CancellationToken ct = default)` — `ProductFilter`
      record lives in `Features/Products/GetProductsQuery.cs` (same types, no cycle: Interfaces must NOT reference
      Features — therefore `ProductFilter` is defined in `Interfaces/IProductRepository.cs`? Decision: define
      `public sealed record ProductFilter(...)` in the **Interfaces** file's namespace (
      `ShopApi.Application.Interfaces`) so the contract is self-contained; the query maps its own parameters onto it.
      Document this explicitly.)
    - `Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken ct = default)`
- **`ProductFilter` record fields**: `Guid? CategoryId`, `string? Search`, `decimal? MinPrice`, `decimal? MaxPrice`,
  `string? SortBy` (`"name" | "price" | "created"` — anything else → handler rejects via validator),
  `bool SortDescending`, `int Page` (≥1), `int PageSize` (1–100).
- **Dependencies**: `Product`, `PagedResult<Product>`.

#### File: `Interfaces/IOrderRepository.cs`

- **Type**: `public interface IOrderRepository : IGenericRepository<Order>`
- **Methods**:
    - `Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)`
    - `Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)`
    -
  `Task<PagedResult<Order>> GetByUserPagedAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)`
- **Dependencies**: `Order`, `PagedResult<Order>`.

#### File: `Interfaces/IUserRepository.cs`

- **Type**: `public interface IUserRepository : IGenericRepository<User>`
- **Methods**:
    - `Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)` (implementations normalize identically
      to `User`: trim + lowercase — state the rule so Phase 04 matches).
    - `Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)`
- **Dependencies**: `User`.

#### File: `Interfaces/ICartRepository.cs`

- **Type**: `public interface ICartRepository : IGenericRepository<CartItem>`
- **Methods**:
    - `Task<IReadOnlyList<CartItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)` (products populated
      for price/name mapping).
    - `Task<CartItem?> GetLineAsync(Guid userId, Guid productId, CancellationToken ct = default)`
    - `Task ClearUserCartAsync(Guid userId, CancellationToken ct = default)`
- **Dependencies**: `CartItem`.

#### File: `Interfaces/IUnitOfWork.cs`

- **Type**: `public interface IUnitOfWork : IDisposable`
- **Properties**: `IProductRepository Products { get; }`, `IOrderRepository Orders { get; }`,
  `IUserRepository Users { get; }`, `ICartRepository Cart { get; }`,
  `IGenericRepository<Category> Categories { get; }` (no dedicated category repo per phase scope — generic covers it;
  document).
- **Methods**: `Task<int> SaveChangesAsync(CancellationToken ct = default)`
- **Notes**: One `SaveChangesAsync` per handler execution = atomic multi-entity writes (order placement + stock + cart
  clear).

#### File: `Interfaces/IJwtService.cs`

- **Namespace**: `ShopApi.Application.Interfaces`
- **Type**: `public interface IJwtService`
- **Methods**: `string GenerateToken(User user)`; `ClaimsPrincipal? ValidateToken(string token)` (null = invalid; never
  throws on malformed input — Phase 04 must honor this).
- **Dependencies**: `User`, `System.Security.Claims` (BCL).

#### File: `Interfaces/IPasswordHasher.cs`

- **Type**: `public interface IPasswordHasher`
- **Methods**: `string Hash(string password)`; `bool Verify(string password, string passwordHash)`
- **Notes**: Implementations (PBKDF2/BCrypt) are Phase 04's choice; contract stays algorithm-agnostic.

#### File: `Interfaces/ICurrentUserService.cs`

- **Type**: `public interface ICurrentUserService`
- **Properties**: `Guid? UserId { get; }` (null = anonymous), `bool IsAuthenticated { get; }`, `bool IsAdmin { get; }`
- **Notes**: `IsAdmin` lets `GetOrderById` authorize admin bypass without referencing HTTP claims here.

### DTOs (all in `ShopApi.Application.Common.Dtos`; all `sealed record`, all response-shaped, no behavior)

#### File: `Common/Dtos/UserDtos.cs`

- `UserDto(Guid Id, string Email, string FirstName, string LastName, string FullName, string Role)` (`Role` =
  `UserRole.ToString()` via profile).
- `AuthResponseDto(string Token, UserDto User)`.

#### File: `Common/Dtos/CategoryDtos.cs`

- `CategoryDto(Guid Id, string Name, string Slug, Guid? ParentId)`.

#### File: `Common/Dtos/ProductDtos.cs`

-

`ProductDto(Guid Id, string Name, string Slug, string Description, decimal Price, string Currency, int Stock, Guid CategoryId, string? CategoryName, IReadOnlyList<ProductImageDto> Images, DateTime CreatedAt)`.

- `ProductImageDto(Guid Id, string Url, bool IsMain)`.
- `CreateProductImageRequest(string Url, bool IsMain)` (nested input for create/update commands — lives here so both
  product commands share it).

#### File: `Common/Dtos/CartDtos.cs`

-

`CartItemDto(Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity, decimal LineTotal, string? ImageUrl)` (
main-image URL or null).

- `CartDto(IReadOnlyList<CartItemDto> Items, decimal TotalAmount, string Currency, int TotalItems)` (single-currency
  cart guaranteed by catalog data; empty cart → `TotalAmount 0`, `Currency "USD"`).

#### File: `Common/Dtos/OrderDtos.cs`

-

`OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, string Currency, decimal LineTotal)`.
-
`OrderDto(Guid Id, string OrderNumber, string Status, decimal TotalAmount, string Currency, AddressDto Address, IReadOnlyList<OrderItemDto> Items, DateTime CreatedAt)`.

#### File: `Common/Dtos/AddressDtos.cs`

- `AddressDto(Guid Id, string Province, string City, string PostalCode, string FullAddress)`.

### Mapping

#### File: `Mappings/MappingProfile.cs`

- **Namespace**: `ShopApi.Application.Mappings`
- **Type**: `public sealed class MappingProfile : Profile`
- **Maps** (entity→DTO only; NO `ReverseMap` — commands construct entities via constructors/behavior methods, keeping
  Domain encapsulation intact):
    - `User → UserDto` (`Role` ← `src.Role.ToString()`; `FullName` mapped directly).
    - `Category → CategoryDto`; `Address → AddressDto`; `ProductImage → ProductImageDto`.
    - `Product → ProductDto` (`Price` ← `Amount`, `Currency` ← `Currency`, `CategoryName` ←
      `Category != null ? Name : null`, `Images` collection).
    - `CartItem → CartItemDto` (`ProductName` ← `Product.Name`, `UnitPrice` ← `Product.Price.Amount`, `Currency` ←
      `Product.Price.Currency`, `LineTotal` ← `Amount * Quantity`, `ImageUrl` ← first main image URL or null — null-safe
      chain).
    - `Order → OrderDto` (`Status` ← `ToString()`, totals from `TotalAmount`, `Address` nested, `Items` collection);
      `OrderItem → OrderItemDto` (same flattening; `ProductName` ← `Product != null ? Name : "Unknown"` —
      deleted-product tolerance).
- **Dependencies**: AutoMapper, Domain entities, DTOs.
- **Notes**: Null-navigations must never throw (repositories may return partial graphs in edge cases) — every member
  chain is null-guarded.

### Features — Auth (`ShopApi.Application.Features.Auth`)

#### File: `Features/Auth/RegisterCommand.cs`

-

`public sealed record RegisterCommand(string Email, string Password, string FirstName, string LastName) : IRequest<Result<AuthResponseDto>>`

- Handler `RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>`, deps `IUnitOfWork`,
  `IPasswordHasher`, `IJwtService`, `IMapper`.
- Flow: `EmailExistsAsync` → `Conflict("Email is already registered.")`; `Hash(password)`;
  `new User(email, hash, first, last)`; `Users.AddAsync` + `SaveChangesAsync`; `GenerateToken(user)`; map; `Success`.
  `Role` always `Customer` (no role input — privilege-escalation impossible by construction).

#### File: `Features/Auth/LoginQuery.cs`

- `public sealed record LoginQuery(string Email, string Password) : IRequest<Result<AuthResponseDto>>`
- Handler deps same as Register. Flow: `GetByEmailAsync` null OR `!Verify(...)` → identical
  `Unauthorized("Invalid email or password.")` (no user-enumeration oracle); else token + map + `Success`.

#### File: `Features/Auth/RegisterCommandValidator.cs`

- `AbstractValidator<RegisterCommand>`: `Email` NotEmpty + `EmailAddress` + max 256; `Password` min 6 max 100 (plaintext
  rules live ONLY here, never in Domain); `FirstName`/`LastName` 1–100.

#### File: `Features/Auth/LoginQueryValidator.cs`

- `Email` NotEmpty + `EmailAddress`; `Password` NotEmpty (no length rules — login must accept any stored-credential
  shape).

### Features — Categories (`ShopApi.Application.Features.Categories`)

#### File: `Features/Categories/CreateCategoryCommand.cs`

- `record CreateCategoryCommand(string Name, Guid? ParentId) : IRequest<Result<CategoryDto>>`. Handler deps
  `IUnitOfWork`, `IMapper`: parent (if any) exists → else `NotFound("Parent category not found.")`;
  `new Category(name, parentId)`; `Categories.AddAsync` + save; map. Domain slug-validation failures surface as
  `DomainException` → caught → `Conflict` (see handler-error pattern in §5).

#### File: `Features/Categories/UpdateCategoryCommand.cs`

- `record UpdateCategoryCommand(Guid Id, string Name, Guid? ParentId) : IRequest<Result<CategoryDto>>`. Handler: load →
  `NotFound`; parent checks (exists + not self — self-check duplicates Domain guard; keep both: cheap + explicit error);
  `Rename` + `SetParent`; `Update` + save; map.

#### File: `Features/Categories/DeleteCategoryCommand.cs`

- `record DeleteCategoryCommand(Guid Id) : IRequest<Result<bool>>`. Handler: load → `NotFound`;
  `CountByCategoryAsync > 0` → `Conflict("Category contains products.")`; `Categories.FindAsync(c => c.ParentId == id)`
  non-empty → `Conflict("Category has subcategories.")`; `Remove` + save; `Success(true)`.

#### File: `Features/Categories/GetAllCategoriesQuery.cs`

- `record GetAllCategoriesQuery() : IRequest<Result<IReadOnlyList<CategoryDto>>>`. Handler: `ListAllAsync`, order by
  `Name` in handler (in-memory, small set), map.

#### File: `Features/Categories/GetCategoryByIdQuery.cs`

- `record GetCategoryByIdQuery(Guid Id) : IRequest<Result<CategoryDto>>`. Handler: load →
  `NotFound("Category not found.")`; map.

#### Validators (30–33)

- `CreateCategoryCommandValidator`: `Name` 2–100.
- `UpdateCategoryCommandValidator`: `Id` non-empty; `Name` 2–100.
- `DeleteCategoryCommandValidator` / `GetCategoryByIdQueryValidator`: `Id` `NotEmpty()`.

### Features — Products (`ShopApi.Application.Features.Products`)

#### File: `Features/Products/CreateProductCommand.cs`

-

`record CreateProductCommand(string Name, string Description, decimal Price, string Currency, int Stock, Guid CategoryId, IReadOnlyList<CreateProductImageRequest>? Images) : IRequest<Result<ProductDto>>`

- Handler deps `IUnitOfWork`, `IMapper`: category exists → else `NotFound`; `SlugExistsAsync(generated?)` — slug is
  generated inside `Product`, so pre-check via a locally generated slug? Decision: attempt creation, rely on DB unique
  constraint in Phase 04 for the race; ALSO pre-check with slug generated by the same rule duplicated? NO — instead:
  create entity, `AddAsync`, `SaveChangesAsync` inside try/catch for unique-violation? Infrastructure exception types
  unknown here. Decision (document): pre-check skipped; duplicate slugs rejected by Phase 04 DB constraint surfacing as
  failure via middleware. Handler: `new Product(name, desc, new Money(price, currency), stock, categoryId)`; images via
  `AddImage` each; save; reload `GetByIdWithDetailsAsync` for populated graph; map.

#### File: `Features/Products/UpdateProductCommand.cs`

-

`record UpdateProductCommand(Guid Id, string Name, string Description, decimal Price, string Currency, int Stock, Guid CategoryId) : IRequest<Result<ProductDto>>` (
images managed separately — out of Phase 03 scope; document). Handler: load → `NotFound`; category exists → else
`NotFound`; `UpdateDetails` + `ChangePrice(new Money(...))` + stock sync via `AdjustStock(target - current)` +
`SetCategory`; save; reload details; map.

#### File: `Features/Products/DeleteProductCommand.cs`

- `record DeleteProductCommand(Guid Id) : IRequest<Result<bool>>`. Handler: load → `NotFound`; `Remove` + save;
  `Success(true)`. (FK references from cart/order lines are Phase 04 mapping decisions — restrict vs. history
  preservation; handler stays simple; document.)

#### File: `Features/Products/GetProductByIdQuery.cs`

- `record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDto>>`. Handler: `GetByIdWithDetailsAsync` → `NotFound`;
  map.

#### File: `Features/Products/GetProductsQuery.cs`

-

`record GetProductsQuery(Guid? CategoryId, string? Search, decimal? MinPrice, decimal? MaxPrice, string? SortBy, bool SortDescending, int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<ProductDto>>>`

- Handler deps `IUnitOfWork`, `IMapper`: build `ProductFilter` from parameters; `GetPagedAsync`; map `Items`, preserve
  paging metadata.

#### Validators (39–42)

- Create/Update: `Name` 2–200; `Description` max 2000; `Price` `> 0`; `Currency` length 3; `Stock` `>= 0`; `CategoryId`
  non-empty; each image `Url` absolute http(s) + `Images` max 10 entries (anti-abuse cap).
- Delete/GetById: `Id` non-empty. GetProducts has no validator (all parameters optional with safe defaults; `Page`/
  `PageSize` clamped in handler: `Page < 1 → 1`, `PageSize` clamped 1–100 — document).

### Features — Cart (`ShopApi.Application.Features.Cart`)

- All handlers start: `currentUserId = _currentUser.UserId` null → `Unauthorized("Authentication required.")`.
- Shared private mapping helper duplicated per handler? Decision:
  `private static CartDto ToCartDto(IEnumerable<CartItem> lines)` static helper inside EACH handler file is
  duplication — instead specify one `internal static class CartMapping` in `Features/Cart/CartMapping.cs`? That adds
  file #55. Decision: YES, add `Features/Cart/CartMapping.cs` (
  `internal static CartDto ToCart(IEnumerable<CartItem> lines)`) — totals computed from `Product.Price * Quantity`,
  `Currency` from first line or `"USD"` when empty, `TotalItems` = sum of quantities, `ImageUrl` = first main image. (
  Table row added below as #55.)

#### File: `Features/Cart/AddToCartCommand.cs`

- `record AddToCartCommand(Guid ProductId, int Quantity) : IRequest<Result<CartDto>>`. Handler deps `IUnitOfWork`,
  `ICurrentUserService`: product exists → `NotFound`; `product.Stock < existing + requested` →
  `Conflict("Insufficient stock.")`; existing line → `Increase` else `new CartItem(user, product, qty)` + `AddAsync`;
  save; reload `GetByUserIdAsync`; `CartMapping.ToCart`.

#### File: `Features/Cart/RemoveFromCartCommand.cs`

- `record RemoveFromCartCommand(Guid ProductId) : IRequest<Result<CartDto>>`. Handler: `GetLineAsync` null →
  `NotFound("Product is not in the cart.")`; `Remove` + save; reload; map.

#### File: `Features/Cart/GetCartQuery.cs`

- `record GetCartQuery() : IRequest<Result<CartDto>>`. Handler: `GetByUserIdAsync`; map (empty list → empty cart DTO,
  still `Success`).

#### Validators (46–47)

- Add: `ProductId` non-empty; `Quantity` 1–999. Remove: `ProductId` non-empty.

### Features — Orders (`ShopApi.Application.Features.Orders`)

- All handlers resolve user via `ICurrentUserService` (null → `Unauthorized`); admin bypass via `IsAdmin`.
- **DomainException pattern**: `Order.Create` / `ChangeStatus` / `AdjustStock` throw `DomainException` on rule
  violations the handler pre-checked (race window) — handlers wrap the orchestration core in try/catch (
  `DomainException` → `Result.Failure(Error.Conflict(ex.Message))`). This is the specified translation of domain
  invariants to application failures.

#### File: `Features/Orders/CreateOrderCommand.cs`

- `record CreateOrderCommand(Guid AddressId) : IRequest<Result<OrderDto>>`
- Handler deps `IUnitOfWork`, `ICurrentUserService`, `IMapper`. Flow: cart lines = `GetByUserIdAsync` → empty →
  `Conflict("Cart is empty.")`; address = `FindAsync(a => a.Id == addressId && a.UserId == user)` (generic repo via a
  new `IGenericRepository<Address> Addresses` on `IUnitOfWork`? Decision: YES — add `Addresses` generic property to the
  `IUnitOfWork` spec in §3-file-10; reuse for ownership check) → missing → `NotFound("Address not found.")`; per-line
  stock check (`product.Stock < qty` → `Conflict`) using populated `Product` navigations; build
  `(productId, qty, product.Price)` tuples; `Order.Create(...)`; stock decrement `AdjustStock(-qty)` per product +
  `Products.Update`; `Orders.AddAsync(order)` (items cascade — Phase 04 mapping note); `ClearUserCartAsync`; ONE
  `SaveChangesAsync`; reload `GetByIdWithItemsAsync`; map.

#### File: `Features/Orders/GetMyOrdersQuery.cs`

- `record GetMyOrdersQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<OrderDto>>>` (clamp like
  products). Handler: `GetByUserPagedAsync`; map preserving metadata.

#### File: `Features/Orders/GetOrderByIdQuery.cs`

- `record GetOrderByIdQuery(Guid Id) : IRequest<Result<OrderDto>>`. Handler: `GetByIdWithItemsAsync` → `NotFound`;
  `order.UserId != current && !IsAdmin` → `Forbidden("Access denied.")`; map.

#### File: `Features/Orders/UpdateOrderStatusCommand.cs`

- `record UpdateOrderStatusCommand(Guid Id, OrderStatus Status) : IRequest<Result<OrderDto>>`. Handler: load with
  items → `NotFound`; `ChangeStatus(Status)` (DomainException → Conflict via pattern); `Update` + save; map. NOTE: no
  role check here — admin-only enforcement is Phase 05 authorization policy; document.

#### Validators (52–54)

- Create: `AddressId` non-empty. UpdateStatus: `Id` non-empty + `Status IsInEnum()`. GetById: `Id` non-empty.

#### Extra file

- #55 `src/ShopApi.Application/Features/Cart/CartMapping.cs` — `internal static class CartMapping` with
  `internal static CartDto ToCart(IEnumerable<CartItem> lines)` (see Cart section).

---

## 4. Implementation Order

1. `Common/Wrappers/Error.cs` → `Result.cs` → `PagedResult.cs` (nothing depends on them; everything needs them).
2. `Interfaces/IGenericRepository.cs` → specific repos (`IProduct` (+`ProductFilter`), `IOrder`, `IUser`, `ICart`) →
   `IUnitOfWork` (add `Categories` + `Addresses` generic properties per §3 decisions) → service contracts (
   `IJwtService`, `IPasswordHasher`, `ICurrentUserService`).
3. `Common/Dtos/` — all 6 files (depend only on BCL + enums-as-strings; no entity refs needed since DTOs are flat).
4. `Mappings/MappingProfile.cs` (needs entities + DTOs).
5. `Common/Behaviors/ValidationBehavior.cs` (needs `Result`/`Error` only).
6. `Features/Auth/` — 2 operations + 2 validators (vertical slice; proves the kernel end-to-end).
7. `Features/Categories/` — 5 operations + 4 validators.
8. `Features/Products/` — 5 operations + 4 validators.
9. `Features/Cart/` — `CartMapping.cs` first, then 3 operations + 2 validators.
10. `Features/Orders/` — 4 operations + 3 validators (most complex; last).
11. Delete the 5 obsolete `.gitkeep` files; KEEP `Common/Exceptions/.gitkeep`.
12. `dotnet build src/ShopApi.Application` → fix → full `dotnet build`.
13. Grep-verification: no `Microsoft.EntityFrameworkCore`, no `ShopApi.Infrastructure`, no `ShopApi.API` references in
    `src/ShopApi.Application/**/*.cs` (except nothing); csproj diff must show zero changes.
14. `git status` review: 55 additions + 5 deletions, 0 modifications.

## 5. Code Patterns & Standards

- **CQRS**: one `IRequest<Result<T>>` per use case; handlers are thin orchestrators (load → guard → mutate via Domain
  behavior methods → save → map). Zero business rules in handlers that belong in entities (price/stock/status logic
  stays in Domain; handlers only translate failures).
- **Vertical slices**: each `Features/<Area>/` folder owns its requests, handlers, validators (shared DTOs stay in
  `Common/Dtos` to avoid cross-slice imports).
- **Request/handler co-location**: one file per operation containing both the record and its handler (keeps 21
  operations navigable); validators in separate `*Validator.cs` files (discoverable by `AddValidatorsFromAssembly`
  conventions in Phase 05).
- **Failure model**: `Result.Failure(Error.*)` for expected failures (
  NotFound/Conflict/Unauthorized/Forbidden/Validation); `DomainException` → `Conflict` translation at handler
  boundaries (Orders + any `Create` path); unexpected exceptions bubble to Phase 05 middleware. No throwing for control
  flow inside handlers.
- **Validation layering**: FluentValidation = input shape/format (required, lengths, ranges, email format, enum
  membership, URL shape); Domain guards = invariants (negativity, transitions, stock). Validators never duplicate
  invariant logic — they fail fast on shape so Domain rarely throws.
- **Mapping**: AutoMapper entity→DTO only, null-safe member chains, no `ReverseMap`. `Money` flattened to
  `(decimal, string)` pairs at the boundary; rehydrated via `new Money(...)` on the way in.
- **AuthN/Z split**: handlers authenticate identity (`ICurrentUserService.UserId`) and apply ownership checks;
  role-based access (admin-only status changes) is Phase 05 policy — handlers expose `IsAdmin` escape hatch only for
  read-own-or-admin (`GetOrderById`).
- **Async discipline**: every I/O call awaited with `CancellationToken` threaded from handler signature; no `.Result`/
  `.Wait()`; `AddAsync`/`SaveChangesAsync` always paired (no silentyj unit-of-work bypass).
- **Naming**: `{Verb}{Entity}{Command|Query}` requests; `{X}Handler`; `{X}Validator`; `...Dto` records;
  `I{...}Repository`; interface methods `...Async` suffix; error messages `"Entity reason."` with identifying values
  where safe (ids) — never passwords/tokens/hash material.
- **KISS/DRY/SOLID**: one `CartMapping` helper (not three copies); `ProductFilter` single definition; DIP everywhere (
  handlers depend on `IUnitOfWork`/service abstractions, never statics except `Money` factories); no service
  implementations (Phase 04).

## 6. Edge Cases & Validation

- **Duplicate email registration**: `EmailExistsAsync` pre-check + DB unique index (Phase 04) for the race; identical
  `Unauthorized` message on login for unknown email vs. wrong password (no enumeration oracle).
- **Privilege escalation**: `RegisterCommand` has no role field; `PromoteToAdmin` unreachable from any Phase 03
  operation (admin seeding is Phase 04/06 concern — note).
- **Anonymous cart/order access**: every Cart/Order handler rejects null `UserId` with `Unauthorized` before any
  repository call.
- **Empty-cart order**: `Conflict`, never a zero-item `Order` (Domain would throw anyway — defense in depth).
- **Stock races**: pre-check + `AdjustStock` guard; true serialization is a Phase 04 concurrency-token concern —
  document on `CreateOrderCommand`.
- **Cross-user address use**: address lookup scoped by `(Id, UserId)` — using another user's address yields `NotFound` (
  not `Forbidden`, to avoid id-existence oracle).
- **Order visibility**: owner-only + admin bypass; wrong user gets `Forbidden`.
- **Illegal status jump**: `ChangeStatus` throws → handler maps to `Conflict` with the Domain message (safe: contains
  only enum names).
- **Currency mixing**: rejected at `Order.Create`; `CartDto` derives display currency from lines (empty → `"USD"`).
- **Paging abuse**: `PageSize` clamped 1–100, `Page` floored at 1 in handlers; unknown `SortBy` rejected by validator (
  Create/Update paths) — `GetProductsQuery` has no validator so the handler defensively defaults unknown sorts to
  `created` (document).
- **Slug collision on product create**: pre-check impossible (slug generated inside entity) — Phase 04 unique constraint
  is the backstop; surface via middleware as Conflict (Phase 05 note).
- **Category delete with dependents**: products/children checks → `Conflict` before removal (no orphaned FKs, no cascade
  surprises).
- **Token/hash material**: never logged, never placed in `Error.Message`, never returned in DTOs (`UserDto` carries no
  `PasswordHash` — profile must not map it; assert in Phase 05 tests).
- **Validator coverage**: every command + every id-bearing query has a validator so `ValidationBehavior` actually
  executes on all Phase 05 endpoints; parameterless `GetAllCategoriesQuery`/`GetCartQuery` intentionally validator-free.

## 7. Verification Checklist

- [ ] All 55 `.cs` files created under `src/ShopApi.Application/` with namespaces
  `ShopApi.Application.{Common.Wrappers,Common.Behaviors,Common.Dtos,Interfaces,Mappings,Features.*}`
- [ ] 5 obsolete `.gitkeep` files deleted; `Common/Exceptions/.gitkeep` retained; csproj byte-identical
- [ ] `Error` codes limited to the 5 well-known strings; `Result<T>` success/failure construction explicit (no implicit
  operators)
- [ ] `IUnitOfWork` exposes Products/Orders/Users/Cart + generic Categories/Addresses and a single `SaveChangesAsync`
- [ ] Every command and every id-bearing query has a validator; parameterless reads documented as intentionally
  uncovered
- [ ] `MappingProfile` maps all 8 entities (+nested) with null-safe chains and no `ReverseMap`
- [ ] Handlers return `Result<T>` on all paths (no throws for expected failures); `DomainException` translated to
  `Conflict` at order/status/create boundaries
- [ ] Auth: no role input on register; identical login-failure message; `UserDto` carries no password hash
- [ ] `dotnet build` (Debug + Release) succeeds with 0 errors and no NEW warnings (pre-existing AutoMapper NU1903
  excepted)
- [ ] Grep-verified: no `EntityFramework`, `Infrastructure`, or `API` references inside Application sources
- [ ] `git status` shows exactly 55 additions + 5 deletions, 0 modifications
- [ ] Clean Code: thin handlers, DDD invariants untouched, named constants, `CancellationToken` threaded everywhere, no
  `.Result`/`.Wait()`
