# Phase 6: Dockerize

1. Optimized multi-stage Dockerfile for ShopApi.API

   - Base image: mcr.microsoft.com/dotnet/aspnet:8.0
   - SDK image: mcr.microsoft.com/dotnet/sdk:8.0
   - Non-root user for security
   - Expose port 8080

2. docker-compose.yml including:

   - api service
   - mssql service (image: mcr.microsoft.com/mssql/server:2022-latest)
   - volume for database persistence
   - shared network
   - healthcheck
   - env_file: .env

3. Proper .dockerignore

4. Full explanation of how to run (docker compose up --build)

5. How to run migrations inside docker

6. Entrypoint script if needed
