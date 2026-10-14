# ShopApi — API Documentation

A .NET 8 shop backend built with Clean Architecture and CQRS (MediatR). All responses are standardized: success returns the DTO body, failures return RFC-7807 ProblemDetails with a stable machine-readable `code` extension.

- [1. Base URLs & Environments](#1-base-urls--environments)
- [2. Authentication](#2-authentication)
- [3. Auth Endpoints](#3-auth-endpoints)
- [4. Categories Endpoints](#4-categories-endpoints)
- [5. Products Endpoints](#5-products-endpoints)
- [6. Cart Endpoints](#6-cart-endpoints)
- [7. Orders Endpoints](#7-orders-endpoints)
- [8. Common Errors](#8-common-errors)
- [9. Full Example Flow](#9-full-example-flow)
- [10. Notes & Limits](#10-notes--limits)

## 1. Base URLs & Environments

| Environment | Base URL | Swagger UI | Notes |
|---|---|---|---|
| Local (`dotnet run --launch-profile http`) | `http://localhost:5125` | `/swagger` | Development only |
| Local HTTPS profile | `https://localhost:7177` | `/swagger` | Development only |
| Docker Compose | `http://localhost:5125` | `/swagger` | Development in compose; auto-migrates + seeds |
| Health (all) | `/health` | — | `200` Healthy / `503` Unhealthy, includes a `database` entry |

The OpenAPI JSON document is served at `/swagger/v1/swagger.json` (Development only — Production returns `404` for Swagger).

Conventions used throughout this document:

- JSON keys are **camelCase** (`orderNumber`, `totalCount`, `isMain`).
- Money is a JSON number plus a `currency` string: `"price": 1299.99, "currency": "USD"` (always 2 decimals).
- Dates are UTC ISO-8601, e.g. `"2026-10-14T12:02:08.63Z"`.
- Error responses always carry `extensions.code` — switch on `code`, never on human-readable `title`/`detail`:
```json
{
  "title": "NotFound",
  "status": 404,
  "detail": "Product not found.",
  "code": "NotFound",
  "traceId": "0HNP9TC8R59N4:00000002"
}
```

## 2. Authentication

**Register** via `POST /api/auth/register`, then **log in** via `POST /api/auth/login`. Both return:

```json
{
  "token": "eyJhbGciOi...",
  "user": {
    "id": "3fa85f64-5717-4562-b3c3-2c963f66afa6",
    "email": "vera@shop.test",
    "firstName": "Vera",
    "lastName": "Fy",
    "fullName": "Vera Fy",
    "role": "Customer"
  }
}
```

Use the token on protected routes:

```http
Authorization: Bearer eyJhbGciOi...
```

In Swagger UI, click the **Authorize** button (Bearer scheme) and paste the token.

Token facts:

- HMAC-SHA256 JWT. Claims: `sub` = user id, `email`, `role` (`Customer` | `Admin`), `jti`.
- Default lifetime 24 hours (`Jwt:ExpiryHours`). No issuer/audience validation in local/dev.
- Registration never accepts a role — every new account is `Customer`. Admin status is assigned server-side only.
- Auth endpoints are rate limited: **10 requests/minute per IP** → `429 Too Many Requests` on breach.
- Login failures always return the identical message `Invalid email or password.` (no user-enumeration oracle).
- Expired or tampered tokens are rejected with `401` by the authentication middleware before any handler runs.

Endpoint auth levels used below: `Public` (no token), `User` (any valid token), `Admin` (token with `Admin` role).

## 3. Auth Endpoints

### `POST /api/auth/register` — Public

Register a new customer account.

Request:

```json
{
  "email": "vera@shop.test",
  "password": "P@ssw0rd123",
  "firstName": "Vera",
  "lastName": "Fy"
}
```

Rules: valid email (max 256), password 6–100 chars, names 1–100 chars.

- `200` — `AuthResponseDto` (see [Section 2](#2-authentication)).
- `400` — `Validation` (bad shape).
- `409` — `Conflict` (`Email is already registered.`).
- `429` — rate limit breached.

### `POST /api/auth/login` — Public

Log in with email + password. Same response shape as register.

Request:

```json
{
  "email": "vera@shop.test",
  "password": "P@ssw0rd123"
}
```

- `200` — `AuthResponseDto`.
- `400` — `Validation`.
- `401` — `Unauthorized` (`Invalid email or password.` — identical for unknown email and wrong password).
- `429` — rate limit breached.

## 4. Categories Endpoints

Category shape (`CategoryDto`):

```json
{
  "id": "3fa85f64-5717-4562-b3c3-2c963f66afa6",
  "name": "Electronics",
  "slug": "electronics",
  "parentId": null
}
```

### `GET /api/categories` — Public

List all categories ordered by name.

- `200` — array of `CategoryDto`.

### `GET /api/categories/{id}` — Public

Get one category by id.

- `200` — `CategoryDto`.
- `404` — `NotFound` (`Category not found.`).

### `POST /api/categories` — Admin

Create a category. Slug is generated from the name.

Request:

```json
{
  "name": "Headphones",
  "parentId": null
}
```

- `201` — `CategoryDto` + `Location` header.
- `400` — `Validation` (name 2–100 chars).
- `404` — `NotFound` (`Parent category not found.`).
- `409` — `Conflict` (ungeneratable slug).

### `PUT /api/categories/{id}` — Admin

Rename / re-parent a category (slug regenerates from the new name).

Request:

```json
{
  "name": "Audio",
  "parentId": "3fa85f64-5717-4562-b3c3-2c963f66afa6"
}
```

- `200` — `CategoryDto`.
- `404` — `NotFound` (category or parent missing).
- `409` — `Conflict` (self-parenting).

### `DELETE /api/categories/{id}` — Admin

Delete a category. Blocked while products or subcategories reference it.

- `200` — `true`.
- `404` — `NotFound`.
- `409` — `Conflict` (`Category contains products.` / `Category has subcategories.`).

## 5. Products Endpoints

Product shape (`ProductDto`):

```json
{
  "id": "5ab08b63-ece9-47c2-aa17-7e03d68466e0",
  "name": "Laptop Pro",
  "slug": "laptop-pro",
  "description": "High-performance laptop for developers.",
  "price": 1299.99,
  "currency": "USD",
  "stock": 25,
  "categoryId": "3ab8f891-d8bd-4f3d-8d24-6c9ea7ffec13",
  "categoryName": "Electronics",
  "images": [
    {
      "id": "96015dd3-a09a-40fb-84e1-effc5600c5ee",
      "url": "https://example.com/images/laptop-pro.jpg",
      "isMain": true
    }
  ],
  "createdAt": "2026-10-14T12:02:08.63Z"
}
```

### `GET /api/products` — Public

Filtered, sorted, paginated product list.

Query parameters:

| Parameter | Type | Default | Description |
|---|---|---|---|
| `categoryId` | guid | — | Filter by category |
| `search` | string | — | Name substring match |
| `minPrice` | decimal | — | Minimum price |
| `maxPrice` | decimal | — | Maximum price |
| `sortBy` | string | `created` | `name` \| `price` \| `created` (unknown values fall back to `created`) |
| `sortDescending` | bool | `false` | Sort direction |
| `page` | int | `1` | 1-based page (floored at 1) |
| `pageSize` | int | `20` | Clamped to 1–100 |

- `200` — `{ "items": [...], "page": 1, "pageSize": 20, "totalCount": 4, "totalPages": 1 }`.

### `GET /api/products/{id}` — Public

Get one product with images and category name.

- `200` — `ProductDto`.
- `404` — `NotFound` (`Product not found.`).

### `POST /api/products` — Admin

Create a product (images inline; slug generated from the name).

Request:

```json
{
  "name": "Wireless Mouse",
  "description": "Ergonomic wireless mouse.",
  "price": 29.99,
  "currency": "USD",
  "stock": 150,
  "categoryId": "3ab8f891-d8bd-4f3d-8d24-6c9ea7ffec13",
  "images": [
    { "url": "https://example.com/images/wireless-mouse.jpg", "isMain": true }
  ]
}
```

Rules: name 2–200, description ≤ 2000, price > 0, 3-letter currency, stock ≥ 0, image URLs must start with `http://`/`https://` (max 10 images).

- `201` — `ProductDto` + `Location`.
- `400` — `Validation`.
- `404` — `NotFound` (`Category not found.`).
- `409` — `Conflict`.

### `PUT /api/products/{id}` — Admin

Update name, description, price, stock, and category (images are not managed here).

Request: same body as create minus `images`.

- `200` — `ProductDto`.
- `404` — `NotFound` (product or category missing).
- `409` — `Conflict`.

### `DELETE /api/products/{id}` — Admin

Delete a product.

- `200` — `true`.
- `404` — `NotFound`.
- `409` — `Conflict` (product appears in placed order lines — history is preserved).

## 6. Cart Endpoints

All cart endpoints require a `User` token. The cart shape (`CartDto`):

```json
{
  "items": [
    {
      "productId": "5ab08b63-ece9-47c2-aa17-7e03d68466e0",
      "productName": "Laptop Pro",
      "unitPrice": 1299.99,
      "currency": "USD",
      "quantity": 2,
      "lineTotal": 2599.98,
      "imageUrl": "https://example.com/images/laptop-pro.jpg"
    }
  ],
  "totalAmount": 2599.98,
  "currency": "USD",
  "totalItems": 2
}
```

### `GET /api/cart` — User

Get the current user's cart. An empty cart returns zeroed totals with `200` — never `404`.

- `200` — `CartDto`.
- `401` — `Unauthorized`.

### `POST /api/cart/items` — User

Add a product (or increase its quantity).

Request:

```json
{
  "productId": "5ab08b63-ece9-47c2-aa17-7e03d68466e0",
  "quantity": 2
}
```

Rules: quantity 1–999; requested total cannot exceed stock.

- `200` — updated `CartDto`.
- `401` — `Unauthorized`.
- `404` — `NotFound` (`Product not found.`).
- `409` — `Conflict` (`Insufficient stock.`).

### `DELETE /api/cart/items/{productId}` — User

Remove a product line entirely (to reduce quantity, re-add with the desired amount via `POST`).

- `200` — updated `CartDto`.
- `401` — `Unauthorized`.
- `404` — `NotFound` (`Product is not in the cart.`).

## 7. Orders Endpoints

Order shape (`OrderDto`):

```json
{
  "id": "7c9e6679-7425-40de-944b-e29f16aeb6b6",
  "orderNumber": "ORD-20261014-A1B2C3",
  "status": "Pending",
  "totalAmount": 2599.98,
  "currency": "USD",
  "address": {
    "id": "d2c8a9e1-0b3a-4b5c-8d6e-7f8a9b0c1d2e",
    "province": "Tehran",
    "city": "Tehran",
    "postalCode": "12345",
    "fullAddress": "Some street 1"
  },
  "items": [
    {
      "productId": "5ab08b63-ece9-47c2-aa17-7e03d68466e0",
      "productName": "Laptop Pro",
      "quantity": 2,
      "unitPrice": 1299.99,
      "currency": "USD",
      "lineTotal": 2599.98
    }
  ],
  "createdAt": "2026-10-14T12:10:00.00Z"
}
```

Prices are snapshots at order time — later product price changes never rewrite placed orders.

### `POST /api/orders` — User

Place an order from the current cart (atomically decrements stock and clears the cart).

Request:

```json
{
  "addressId": "d2c8a9e1-0b3a-4b5c-8d6e-7f8a9b0c1d2e"
}
```

The address must belong to the caller.

- `201` — `OrderDto` + `Location`.
- `401` — `Unauthorized`.
- `404` — `NotFound` (`Address not found.`).
- `409` — `Conflict` (`Cart is empty.` / `Insufficient stock.`).

### `GET /api/orders?page=1&pageSize=20` — User

List the caller's own orders, newest first.

- `200` — paged `OrderDto` list (`items`, `page`, `pageSize`, `totalCount`, `totalPages`).
- `401` — `Unauthorized`.

### `GET /api/orders/{id}` — User

Get one order. Owners and admins only.

- `200` — `OrderDto`.
- `401` — `Unauthorized`.
- `403` — `Forbidden` (`Access denied.`).
- `404` — `NotFound` (`Order not found.`).

### `PATCH /api/orders/{id}/status` — Admin

Change order status. Legal transitions:

| From | To |
|---|---|
| `Pending` | `Paid`, `Cancelled` |
| `Paid` | `Shipped`, `Cancelled` |
| `Shipped` | `Delivered` |
| `Delivered`, `Cancelled` | (terminal — no transitions) |

Request:

```json
{
  "status": "Paid"
}
```

- `200` — `OrderDto`.
- `404` — `NotFound`.
- `409` — `Conflict` (illegal transition).

## 8. Common Errors

Clients must switch on the `code` field — never on human-readable `title`/`detail` strings.

| `code` | HTTP | Meaning | Example trigger |
|---|---|---|---|
| `Validation` | 400 | Shape/format failure (FluentValidation) | empty register body |
| `Unauthorized` | 401 | Missing/invalid credentials or token | bad login, no bearer header |
| `Forbidden` | 403 | Authenticated but not allowed | reading another user's order |
| `NotFound` | 404 | Resource missing (or another user's address) | unknown product id |
| `Conflict` | 409 | Rule/state clash, incl. domain guards and DB unique/FK violations | duplicate email, bad status jump, deleting a referenced product |
| `InternalError` | 500 | Unexpected failure (detail hidden outside Development) | — |
| (framework) | 429 | Auth rate limit breached | 11th login attempt within a minute |

## 9. Full Example Flow

Base URL `http://localhost:5125`. Requires a user-owned `addressId` for step 5 (there is no address-management endpoint in this version — use a row seeded or inserted directly; see [Notes](#10-notes--limits)).

```bash
# 1. Register (200) — capture .token
curl -X POST http://localhost:5125/api/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"email":"vera@shop.test","password":"P@ssw0rd123","firstName":"Vera","lastName":"Fy"}'

TOKEN=eyJhbGciOi...

# 2. Login (200) — same shape as register
curl -X POST http://localhost:5125/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"vera@shop.test","password":"P@ssw0rd123"}'

# 3. Browse the seeded catalog (200) — pick the Laptop id
curl 'http://localhost:5125/api/products?pageSize=20'

LAPTOP=5ab08b63-ece9-47c2-aa17-7e03d68466e0

# 4. Add 2 Laptops to the cart (200) — totalItems 2, totalAmount 2599.98
curl -X POST http://localhost:5125/api/cart/items \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"productId\":\"$LAPTOP\",\"quantity\":2}"

# 5. Confirm the cart (200) — identical totals
curl http://localhost:5125/api/cart -H "Authorization: Bearer $TOKEN"

# 6. Place the order (201) — stock decremented, cart cleared
curl -X POST http://localhost:5125/api/orders \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"addressId":"<your-address-id>"}'

# 7. Read the order back (200) — status Pending, snapshot unitPrice 1299.99
curl http://localhost:5125/api/orders/<order-id> -H "Authorization: Bearer $TOKEN"
```

## 10. Notes & Limits

- **No address-management endpoints exist** in this version: orders reference existing `Addresses` rows (seed/demo data or direct inserts). Using another user's address returns `404` (by design, to avoid id-existence oracles).
- **No image upload**: product images are URL references only (`http://`/`https://`, max 10 per product on create).
- **No admin promotion endpoint**: `PromoteToAdmin` is domain behavior without an HTTP surface; admin users are provisioned server-side.
- **Single-currency enforcement**: an order rejects mixed-currency lines; the catalog ships in USD.
- **Slugs** are generated from names and enforced unique by the database; category slugs regenerate on rename.
- **Tunables**: auth rate limit (10/min/IP) and JWT lifetime (`Jwt:ExpiryHours`, default 24) live in `Program.cs`/`appsettings.json`; CORS origins in `Cors:AllowedOrigins`.
- **Environments**: Docker Compose runs `Development` (Swagger on, auto-migrate + seed). Production serves no Swagger UI and expects a pre-migrated database.
