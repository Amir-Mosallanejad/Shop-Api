# Phase 5: API Layer

1. Controllers:

   - AuthController
   - CategoriesController
   - ProductsController
   - CartController
   - OrdersController

2. Middleware:

   - GlobalExceptionHandler
   - RequestLogging (optional)

3. Configuration:

   - Swagger with JWT Bearer
   - CORS for frontend
   - Simple Rate Limiting on Auth
   - Health Check endpoint

4. Final Program.cs with all DI registrations

5. appsettings.json only for non-sensitive values

All endpoints must return standardized responses (Result → HTTP Response).
