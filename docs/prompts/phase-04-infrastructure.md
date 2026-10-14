# Phase 4: Infrastructure Layer

1. AppDbContext with DbSets and Fluent API Configurations for all entities

   - Relations, Indexes, Constraints, Soft Delete if needed

2. Implement Repositories and UnitOfWork

3. Implement JwtService, PasswordHasher, CurrentUserService

4. Initial Migration (provide CLI command, do not run it yourself)

5. Optional Seed Data for Category and Product (for testing)

6. DependencyInjection extension to register this layer's services

Note: Connection String must be read from .env, not hardcoded.
