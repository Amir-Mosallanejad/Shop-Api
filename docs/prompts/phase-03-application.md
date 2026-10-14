# Phase 3: Application Layer

I want you to implement the Application layer using CQRS:

1. Repository Interfaces:

   - IGenericRepository<T>
   - IProductRepository, IOrderRepository, IUserRepository, ICartRepository
   - IUnitOfWork

2. Service Interfaces:

   - IJwtService (token generation and validation)
   - IPasswordHasher
   - ICurrentUserService

3. DTOs (Request/Response) for all entities

4. Commands and Queries with MediatR for:

   - Auth: Register, Login
   - Category: Create, Update, Delete, GetAll, GetById
   - Product: Create, Update, Delete, GetAll (with filter/pagination), GetById
   - Cart: AddToCart, RemoveFromCart, GetCart
   - Order: CreateOrder, GetMyOrders, GetOrderById, UpdateStatus

5. Validators with FluentValidation for all Commands

6. Mapping Profile (AutoMapper)

7. Result Pattern (Result<T> and Error classes)

8. MediatR Pipeline Behavior for Validation

Only the Application layer. Do not provide Infrastructure code.
