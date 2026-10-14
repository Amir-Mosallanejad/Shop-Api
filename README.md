# ShopApi

A shop backend built with **.NET 8**, **Clean Architecture**, and **CQRS** (MediatR). Provides authentication, catalog, cart, and order management over a documented REST API, backed by SQL Server (EF Core) and runnable locally or via Docker Compose.

## Tech Stack

| Concern | Technology |
|---|---|
| Runtime | .NET 8 (SDK 8.0.4xx, see `global.json`) |
| Architecture | Clean Architecture (Domain / Application / Infrastructure / API) |
| CQRS | MediatR 12.4.1 |
| Validation | FluentValidation 11.12.0 (request pipeline behavior) |
| Mapping | AutoMapper 13.0.1 (entity → DTO) |
| Database | SQL Server 2022 + EF Core 8.0.32 (code-first, migrations in `src/ShopApi.Infrastructure/Migrations`) |
| Auth | JWT Bearer (HMAC-SHA256, 24h default), PBKDF2-SHA512 password hashing |
| API docs | Swagger (Development) + [`API_DOCUMENTATION.md`](API_DOCUMENTATION.md) |
| Containers | Docker + Docker Compose (`api` + `mssql`) |

## Project Structure

```
src/
  ShopApi.Domain/          # Entities, Money value object, enums, BaseEntity, DomainException (zero dependencies)
  ShopApi.Application/     # CQRS requests/handlers/validators, DTOs, Result<T>/Error, repository + service contracts
  ShopApi.Infrastructure/  # AppDbContext + Fluent-API configs, repository/UnitOfWork implementations,
                           # JwtService, PasswordHasher, CurrentUserService, seed data, AddInfrastructure()
  ShopApi.API/             # Controllers, middleware (global exceptions, request logging), Program.cs composition root
docs/
  prompts/                 # Phase prompt files (project build history)
  plans/<phase>/           # Implementation plans + validation reports per phase
docker-compose.yml         # api (5125→8080) + mssql (1433), shared network, persistent volume
API_DOCUMENTATION.md       # Full endpoint reference with samples and example flows
```

Dependency rule: `API → Infrastructure → Application → Domain` (one way, no cycles).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (`dotnet --version` → 8.x)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the Compose path)
- `dotnet-ef` tool for migrations: `dotnet tool install -g dotnet-ef`

## Quickstart

### Option A — Docker Compose (recommended)

```bash
cp .env.example .env
# Edit .env: set a strong SA_PASSWORD (min 8 chars, upper/lower/digit/symbol)
docker compose up --build -d
```

This boots SQL Server, waits for it to become healthy, then the API auto-migrates, seeds demo data (3 categories, 4 products), and serves at:

- API: `http://localhost:5125`
- Swagger UI: `http://localhost:5125/swagger`
- Health: `http://localhost:5125/health`

```bash
docker compose logs -f api   # watch bootstrap
docker compose down          # stop (data preserved in the mssql-data volume)
docker compose down -v       # stop AND wipe the database
```

### Option B — Local .NET run (needs SQL Server on `localhost,1433`)

```bash
cp .env.example .env
# Edit .env: DB_CONNECTION password, JWT_SECRET (min 32 chars)
dotnet ef database update --project src/ShopApi.Infrastructure --startup-project src/ShopApi.API
dotnet run --project src/ShopApi.API --launch-profile http
```

Swagger UI opens at `http://localhost:5125/swagger`.

## Configuration

All secrets come from the environment (`.env` locally, never committed). See `.env.example` for the template.

| Variable | Used by | Description |
|---|---|---|
| `DB_CONNECTION` | Host runs | Full SQL Server connection string (`localhost` host) |
| `JWT_SECRET` | Everywhere | HMAC signing key, min 32 chars (`openssl rand -base64 48`) |
| `SA_PASSWORD` | Compose | SQL Server `sa` password (complexity rules apply) |
| `DB_NAME` | Compose | Database name (default `ShopApiDb`) |
| `ASPNETCORE_ENVIRONMENT` | Everywhere | `Development` enables Swagger + auto-migrate/seed |

Inside Compose, `DB_CONNECTION` is built from `SA_PASSWORD`/`DB_NAME` with the `mssql` host and overrides the `.env` value.

Non-sensitive settings (`Jwt:ExpiryHours`, `Cors:AllowedOrigins`) live in `src/ShopApi.API/appsettings.json`.

## API Overview

19 routes across 5 controllers — see [`API_DOCUMENTATION.md`](API_DOCUMENTATION.md) for the full reference with request/response samples.

| Controller | Scope | Highlights |
|---|---|---|
| `POST /api/auth/*` | Public, rate-limited (10/min/IP) | Register, login → JWT + user |
| `/api/categories` | Read public, writes Admin | Full CRUD, slug auto-generation |
| `/api/products` | Read public, writes Admin | Filter/sort/pagination, image URLs |
| `/api/cart` | Authenticated user | Get, add/increase, remove; stock-checked |
| `/api/orders` | Owner (+ Admin status) | Place from cart (atomic), list, status machine |

Auth: `Authorization: Bearer <token>`. Failures return ProblemDetails with a stable `code` (`Validation`, `Unauthorized`, `Forbidden`, `NotFound`, `Conflict`, `InternalError`).

## Example Flow

```bash
TOKEN=$(curl -s -X POST http://localhost:5125/api/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"vera@shop.test","password":"P@ssw0rd123","firstName":"Vera","lastName":"Fy"}' \
  | python3 -c 'import json,sys; print(json.load(sys.stdin)["token"])')

curl 'http://localhost:5125/api/products?pageSize=5'
curl -X POST http://localhost:5125/api/cart/items \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"productId":"<id-from-above>","quantity":2}'
```

## Development

```bash
dotnet build                          # Debug build (0 errors expected)
dotnet build -c Release               # Release build
dotnet ef migrations add <Name> --project src/ShopApi.Infrastructure --startup-project src/ShopApi.API
dotnet ef database update --project src/ShopApi.Infrastructure --startup-project src/ShopApi.API
```

Conventions: conventional commits (`type(scope): summary`, e.g. `feat(api): ...`); atomic commits, staged explicitly; plans live in `docs/plans/<phase>/`; validation reports gate each phase. See `AGENTS.md`.

## Roadmap

Phases 01–06 (solution → domain → application → infrastructure → API → Docker) are implemented and validation-passed. Remaining: final review / production hardening (Phase 08: `Production` compose overlay, CI migration job, secret vaulting, rate-limit tuning).
