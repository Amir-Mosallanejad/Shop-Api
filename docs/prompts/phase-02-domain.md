# Phase 2: Domain Layer

I want you to build the core shop entities in the Domain layer:

Entities:

- User (Id, Email, PasswordHash, FirstName, LastName, Role, CreatedAt)
- Category (Id, Name, Slug, ParentId)
- Product (Id, Name, Slug, Description, Price, Stock, CategoryId, Images)
- ProductImage (Id, ProductId, Url, IsMain)
- CartItem (Id, UserId, ProductId, Quantity)
- Order (Id, OrderNumber, UserId, TotalAmount, Status, CreatedAt, Address)
- OrderItem (Id, OrderId, ProductId, Quantity, UnitPrice)
- Address (Id, UserId, Province, City, PostalCode, FullAddress)

Rules:

- Use Value Objects for Money and Address (if it makes sense)
- Enums for OrderStatus and UserRole
- BaseEntity with Id, CreatedAt, UpdatedAt
- Domain-level validation (e.g. Price cannot be negative)
- No dependencies on EF or external frameworks
- Use Guid for IDs

Only provide the Domain layer code.
