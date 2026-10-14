# Plan — phase-07-documentation: API Documentation

## 1. Overview

### What this phase accomplishes
Produces a single, accurate, human-readable `API_DOCUMENTATION.md` at the repo root that lets a frontend developer integrate without reading C#. By the end of this phase:
- Base URL, environment matrix (host `dotnet run` vs. compose), and Swagger locations documented.
- Authentication flow documented (register → token → `Authorization: Bearer` usage, roles, expiry, rate limits).
- All 19 routes across 5 controllers documented with method, route, auth level, request/response JSON samples, query parameters, and status codes.
- A common-errors table (all 5 `Error` codes + `InternalError` + framework 401/403/429) and a full worked example flow (Register → Login → Add to Cart → Order) verified against live behavior from Phase 06.
- Every sample matches the implementation exactly (camelCase JSON, real field names, real status codes from `BaseApiController` + `GlobalExceptionHandler`).

### Clean Architecture layers affected
| Layer | Project | State after this phase |
|---|---|---|
| None (code) | — | **Zero C# changes.** One Markdown file created at repo root. Validation is documentary accuracy (every claim traceable to code + live smoke results). |

### Dependencies on previous phases
- Phase 03: DTO shapes (all sample bodies must use exact DTO member names), `Error` codes, validator rules (documented as 400 triggers).
- Phase 05: routes/verbs/auth attributes (source of truth — verified by attribute sweep below), status table, rate limiting (10/min/IP on auth), `/health`, Swagger Bearer setup.
- Phase 06: live-verified behaviors to cite (seeded catalog, register→cart totals, 503-Unhealthy without DB, seeded product names/prices for realistic samples).
- Phase 08 (forward): this document becomes review input; prod hardening may change auth lifetimes/CORS — doc must mark environment-dependent values as such.

---

## 2. File-by-File Breakdown

| # | File Path | Action | Layer | Purpose |
|---|---|---|---|---|
| 1 | `API_DOCUMENTATION.md` | Create | Cross-cutting | Complete API reference + guide (spec §3) |

**Explicit non-goals:** no README changes, no Swagger XML comments, no Postman collection, no code changes of any kind.

---

## 3. Detailed File Specifications

### File: `API_DOCUMENTATION.md` (Create, repo root, UTF-8, LF, ~450–550 lines)

**Global conventions for the whole document** (must be stated once, then obeyed):
- JSON samples use **camelCase** (ASP.NET Core default serialization — verified live: `orderNumber`, `totalCount`, `isMain`). Never PascalCase.
- Monetary values are JSON numbers (`1299.99`) with a sibling `currency` string (`"USD"`); amounts always have 2 decimals.
- GUIDs shown as `"3fa85f64-5717-4562-b3c3-2c963f66afa6"` placeholders except where a real seeded value aids copy-paste (none seeded with fixed IDs — all generated, so placeholders throughout).
- Dates are UTC ISO-8601 (`"2026-10-14T12:02:08.63Z"` style).
- Error responses always follow the `extensions.code` contract: `{ "title": "<Code>", "status": <n>, "detail": "<message>", "code": "<Code>", "traceId": "<id>" }`.

**Section 1 — Title + overview**: `# ShopApi — API Documentation`; 3–5 sentences (Clean Architecture .NET 8 shop backend; CQRS; JWT; SQL Server); link table of contents (anchor list of all 9 sections).

**Section 2 — Base URLs & environments** (table):
| Environment | Base URL | Swagger UI | Notes |
|---|---|---|---|
| Local (`dotnet run --launch-profile http`) | `http://localhost:5125` | `/swagger` | Dev only |
| Local HTTPS profile | `https://localhost:7177` | `/swagger` | Dev only |
| Docker Compose | `http://localhost:5125` | `/swagger` | `Development` in compose; auto-migrate + seed |
| Health (all) | `/health` | — | 200 Healthy / 503 Unhealthy, includes `database` entry |
State: Swagger JSON at `/swagger/v1/swagger.json`; Swagger serves Development only (Production returns 404).

**Section 3 — Authentication**:
- How to register (`POST /api/auth/register` + minimal body) and log in; response contains `token` + `user` (role included).
- Usage: `Authorization: Bearer <token>` header on all `[Authorize]` routes; Swagger Authorize button (Bearer scheme) with the same header.
- Token facts: HMAC-SHA256 JWT, claims (`sub` = user id, `email`, `role` = `Customer|Admin`, `jti`), default 24h expiry (`Jwt:ExpiryHours`), no issuer/audience in local/dev.
- Roles: `Customer` (default at registration — no role input exists), `Admin` (server-side only); which endpoints need Admin (list: category/product writes, order status).
- Rate limiting: auth endpoints 10 req/min/IP → `429 Too Many Requests` on breach; login failures return uniform `401` (`Invalid email or password.`) with no user-enumeration oracle.
- Token expiry/tampering → `401` from the JWT middleware before any handler runs.

**Sections 4–8 — Endpoints grouped by controller** (one subsection per route; each with: Method + Route line, 1-line description, `Auth:` badge line (`Public` / `User` / `Admin`), request body sample or query-parameter table, success response sample with status, failure cases with statuses). Exact inventory (verified by attribute sweep — 19 routes):

*AuthController (`api/auth`, Public):*
1. `POST /api/auth/register` — body `{email, password (min 6), firstName, lastName}` → 200 `{token, user{...}}`; 400 Validation; 409 Conflict (duplicate email); 429 on flood.
2. `POST /api/auth/login` — body `{email, password}` → 200 same shape; 400; 401 uniform message.

*CategoriesController (`api/categories`):*
3. `GET /api/categories` (Public) → 200 array of `{id, name, slug, parentId}`.
4. `GET /api/categories/{id}` (Public) → 200 object; 404.
5. `POST /api/categories` (Admin) — body `{name, parentId?}` → 201 with `Location`; 400 (name 2–100); 404 (parent missing); 409 (bad slug).
6. `PUT /api/categories/{id}` (Admin) — body `{name, parentId?}` → 200; 404; 409 (self-parent).
7. `DELETE /api/categories/{id}` (Admin) → 200 `true`; 404; 409 (has products / has subcategories).

*ProductsController (`api/products`):*
8. `GET /api/products` (Public) — query table: `categoryId?` (guid), `search?` (name substring), `minPrice?`, `maxPrice?` (decimals), `sortBy?` (`name|price|created`, default `created`), `sortDescending?` (bool, default false), `page?` (default 1), `pageSize?` (default 20, clamped 1–100) → 200 `{items[], page, pageSize, totalCount, totalPages}` with full product sample (include `categoryName`, `images[{id,url,isMain}]`).
9. `GET /api/products/{id}` (Public) → 200 product; 404.
10. `POST /api/products` (Admin) — body `{name, description, price (>0), currency (3-letter), stock (≥0), categoryId, images?[{url (http(s)), isMain}] (max 10)}` → 201; 400; 404 (category); 409.
11. `PUT /api/products/{id}` (Admin) — same body minus images → 200; 404; 409.
12. `DELETE /api/products/{id}` (Admin) → 200 `true`; 404; 409 (ever-ordered product — FK history guard).

*CartController (`api/cart`, User):*
13. `GET /api/cart` → 200 `{items[{productId, productName, unitPrice, currency, quantity, lineTotal, imageUrl?}], totalAmount, currency, totalItems}` (empty cart = zeroed 200, never 404).
14. `POST /api/cart/items` — body `{productId, quantity (1–999)}` → 200 updated cart; 401; 404 (no product); 409 (insufficient stock).
15. `DELETE /api/cart/items/{productId}` → 200 updated cart; 404 (line missing).

*OrdersController (`api/orders`, User):*
16. `POST /api/orders` — body `{addressId}` (must belong to caller) → 201 with `Location` (`GET /api/orders/{id}`); 401; 404 (address); 409 (empty cart / stock).
17. `GET /api/orders?page=&pageSize=` → 200 paged `OrderDto` (`{id, orderNumber (ORD-yyyyMMdd-XXXXXX), status, totalAmount, currency, address{...}, items[{productId, productName, quantity, unitPrice, currency, lineTotal}], createdAt}`).
18. `GET /api/orders/{id}` → 200; 401; 404; 403 (not owner, non-admin).
19. `PATCH /api/orders/{id}/status` (Admin) — body `{status: "Paid"|"Shipped"|"Delivered"|"Cancelled"}` → 200; 404; 409 (illegal transition; legal flow table: Pending→Paid|Cancelled, Paid→Shipped|Cancelled, Shipped→Delivered, terminal otherwise).

**Section 9 — Common errors table** (exact `code` → HTTP → meaning → typical trigger):
| `code` | HTTP | Meaning | Example trigger |
|---|---|---|---|
| `Validation` | 400 | Shape/format failure (FluentValidation) | empty register body |
| `Unauthorized` | 401 | Missing/invalid credentials or token | bad login, no bearer |
| `Forbidden` | 403 | Authenticated but not allowed | reading another user's order |
| `NotFound` | 404 | Resource missing (or cross-user address) | unknown product id |
| `Conflict` | 409 | Rule/state clash (incl. Domain + SQL unique/FK) | duplicate email, bad status jump, deleting referenced product |
| `InternalError` | 500 | Unexpected (detail hidden outside Development) | — |
| (framework) | 429 | Auth rate limit breached | 11th login attempt in a minute |

**Section 10 — Full example flow** (copy-pasteable curl sequence, verified live in Phase 06 with these exact semantics):
1. Register → capture `token` (sample truncated token shown as `eyJhbGciOi...`).
2. Login → confirm same shape.
3. List products → pick first `id` (cite seeded `Laptop Pro` 1299.99 as the walkthrough item).
4. Add 2 × Laptop to cart → show `totalItems: 2, totalAmount: 2599.98`.
5. Get cart → confirm identical totals.
6. Place order (note: needs a user-owned `addressId` — document that address creation has no endpoint in this version and the seed/demo path; do NOT invent an address endpoint).
7. Get order by id → show `Pending` status and snapshot `unitPrice`.
Annotate each step with expected status codes andpointer to the relevant section above.

**Section 11 — Notes & limits** (honesty section): no address-management endpoints (orders need pre-existing rows); no image upload (URLs only); single-currency enforcement; slug uniqueness DB-backed; rate-limit/auth-expiry tunables and where they live (`Program.cs`, `Jwt:ExpiryHours`); compose runs Development (Swagger on) — Production differs.

---

## 4. Implementation Order

1. Re-verify the route inventory (`Select-String` attribute sweep — already executed during planning; re-run at build to catch drift).
2. Write Sections 1–3 (title, URLs, auth).
3. Write Sections 4–8 controller by controller (Auth → Categories → Products → Cart → Orders), one controller per edit-append pass to keep the file reviewable.
4. Write Section 9 (errors) + Section 10 (flow) + Section 11 (limits).
5. Accuracy pass: every route string, verb, auth badge, field name, and status code cross-checked against `src/ShopApi.API/Controllers/*.cs`, `Common/Dtos/*.cs`, `BaseApiController.StatusFor`, and `GlobalExceptionHandler.Map`.
6. Markdown lint pass: headings hierarchy (`#` once, `##` sections, `###` endpoints), fenced `http`/`json`/`bash` blocks, tables render (pipe-escape check), TOC anchors match headings.
7. `git status` review: exactly 1 addition (`API_DOCUMENTATION.md`), 0 modifications.

## 5. Code Patterns & Standards

- **Docs-as-contract**: every claim traceable to code (route → controller file:line; status → `StatusFor`/handler; field → DTO member). No aspirational endpoints.
- **Audience-first**: frontend developer with no C# knowledge — no type names like `PagedResult<T>` (render as plain JSON shapes), no namespace mentions, framework concepts translated (`[Authorize(Roles="Admin")]` → `Admin` badge).
- **Naming**: routes in `code` spans with method prefix (`GET /api/products`); JSON keys exactly as serialized (camelCase); error `code` values in code spans; file named exactly `API_DOCUMENTATION.md` (system prompt rule 12).
- **Error handling documentation**: the `extensions.code` table is normative — clients must switch on `code`, not on human `detail`/`title` strings (state this rule explicitly in Section 9).
- **KISS**: one file, no diagrams, no generated Swagger dumps pasted inline (link to `/swagger` instead); curl over SDK-specific clients.

## 6. Edge Cases & Validation

- **Stale-route risk**: controllers changed twice during build (route-name removal, bootstrap fix) — the attribute sweep is the single source of truth; any route added after planning invalidates the doc (re-sweep at build).
- **camelCase discipline**: C# DTO members are PascalCase — every sample must be mentally lowercased-first-letter; nested objects (`user`, `address`, `items[]`) included. Spot-check against live Phase 06 payloads (`totalCount`, `isMain`, `orderNumber` all observed camelCase).
- **Auth-badge accuracy**: anonymous reads vs. Admin writes vs. owner-scoped reads — each badge re-checked against `[AllowAnonymous]`/`[Authorize]`/`Roles` attributes (sweep output archived in §3 inventory).
- **Status-code accuracy**: 201-only-on-create (register/login return 200 with token — a common doc error to avoid: do NOT document 201 for auth); deletes return 200 `true` (not 204); empty cart 200 (not 404).
- **Security in docs**: sample tokens truncated (`eyJhbGciOi...`); sample passwords clearly fake (`P@ssw0rd123` style with a "use a strong password" note); never include real `.env` values; warn that Swagger in compose exposes the API on localhost without TLS (dev-only posture).
- **No invented surface**: address creation, image upload, admin promotion, and order cancellation endpoints do NOT exist — the doc must say so where the flow needs them (Section 10 step 6, Section 11) rather than implying them.

## 7. Verification Checklist

- [ ] `API_DOCUMENTATION.md` exists at repo root; ~450–550 lines; `#` title once
- [ ] All 19 routes documented with correct method + path + Auth badge (matches attribute sweep 1:1, no extras, no omissions)
- [ ] Every JSON sample uses camelCase keys matching DTO members exactly (spot-check ≥10 field names against `Common/Dtos/*.cs`)
- [ ] Every status code matches `BaseApiController.StatusFor` + handler failure paths + `CreatedAtAction` usages (201 × 3 creates: category, product, order)
- [ ] Error table covers all 5 `Error` codes + `InternalError` + 429 with correct HTTP mappings
- [ ] Example flow steps executable in order against a compose stack (register→login→products→cart→cart-get→order→order-get), with noted address prerequisite
- [ ] No documentation of non-existent endpoints; limits section present and honest
- [ ] Markdown renders (headings, tables, fenced blocks, TOC anchors); no pasted secrets/tokens
- [ ] `git status` shows exactly 1 addition, 0 modifications; `dotnet build` unaffected (no code touched)
