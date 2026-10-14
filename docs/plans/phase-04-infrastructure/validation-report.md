# Validation Report: phase-04-infrastructure

## Summary

- **Status**: ✅ PASSED
- **Build Status**: ✅ (`dotnet build` Debug + Release: 0 errors, no new warnings)
- **Test Status**: N/A (no test projects; runtime behavior verified in build phase via throwaway harness — 14/14 checks passed — harness removed afterwards)

## Files Checked

| # | Planned File | Exists | Matches Plan | Notes |
|---|---|---|---|---|
| 1 | `ShopApi.Infrastructure.csproj` (Modify) | ✅ | ✅ | Domain ref restored; Jwt 8.23.0; AspNetCore framework ref; EF still 8.0.32 |
| 2 | `Persistence/AppDbContext.cs` | ✅ | ✅ | 8 DbSets, assembly scan, no audit override, no `OnConfiguring` |
| 3 | `Persistence/AppDbContextFactory.cs` | ✅ | ✅ | Env-only connection, clear missing-var error |
| 4 | `Configurations/UserConfiguration.cs` | ✅ | ✅ | Unique email, cascade addresses/cart, restrict orders |
| 5 | `Configurations/AddressConfiguration.cs` | ✅ | ✅ | Lengths + UserId index |
| 6 | `Configurations/CategoryConfiguration.cs` | ✅ | ✅ | Unique slug, self-FK restrict |
| 7 | `Configurations/ProductConfiguration.cs` | ✅ | ✅ | Owned Money (`Price`/`Currency`), unique slug, stock check, category restrict, images cascade |
| 8 | `Configurations/ProductImageConfiguration.cs` | ✅ | ✅ | URL cap, `IsMain` default false |
| 9 | `Configurations/CartItemConfiguration.cs` | ✅ | ✅ | Unique (User,Product), quantity check, product cascade |
| 10 | `Configurations/OrderConfiguration.cs` | ✅ | ✅ | Owned total, unique number, user/address restrict, items cascade |
| 11 | `Configurations/OrderItemConfiguration.cs` | ✅ | ✅ | Owned unit price, quantity check, product restrict |
| 12 | `Repositories/GenericRepository.cs` | ✅ | ✅ | No-tracking reads, sync attach-on-write |
| 13 | `Repositories/ProductRepository.cs` | ✅ | ✅ | Escaped LIKE search, `Price.Amount` range, sort switch defaulting to `CreatedAt`, defensive clamps |
| 14 | `Repositories/OrderRepository.cs` | ✅ | ✅ | Items+Product+Address includes, user-paged desc |
| 15 | `Repositories/UserRepository.cs` | ✅ | ✅ | Trim+lowercase parity with lockstep comment |
| 16 | `Repositories/CartRepository.cs` | ✅ | ✅ | Product+Images include, `ExecuteDeleteAsync` clear |
| 17 | `UnitOfWork/UnitOfWork.cs` | ✅ | ✅ | Single construction point, atomic save, disposes context |
| 18 | `Services/PasswordHasher.cs` | ✅ | ✅ | PBKDF2-SHA512/210k, self-describing format, fixed-time compare, false-on-garbage |
| 19 | `Services/JwtService.cs` | ✅ | ✅ | Fail-fast ≥32-char secret, sub/email/role/jti claims, 24h default, null-on-garbage validate |
| 20 | `Services/CurrentUserService.cs` | ✅ | ✅ | Null-safe claims mapping, `Admin` coupling documented |
| 21 | `DependencyInjection.cs` | ✅ | ✅ | Env-first connection, retry policy, all scoped registrations, no `Migrate()` |
| 22 | `Persistence/SeedData.cs` | ✅ | ✅ | Idempotent guard, 3 categories + 4 products via Domain ctors |
| 23–25 | 3× `.gitkeep` deletions | ✅ | ✅ | `Migrations/.gitkeep` retained; zero migration `.cs` files generated |

## ✅ Passed

- All 21 files exist with exact namespaces (6/6), type names, and signatures (21/21 declaration sweep).
- Delete-behavior matrix exactly as specified (11 `OnDelete` sites verified against the cascade-vs-Restrict plan).
- 17 check/index/owned mapping hits = 3 checks + 5 unique indexes + 3 `OwnsOne` + 6 column names — exact count match.
- Phase 03 bindings honored: unknown-`SortBy` → `CreatedAt` ordering, email normalization parity, populated navigations, set-based cart clear.
- `AddInfrastructure` fail-fast messages name the variables; secrets read-only (grep shows only env/config reads + runtime key derivation, no hardcoded values); no `ShopApi.API` references.
- `dotnet build` Debug + Release: 0 errors. No migration files generated per phase instruction.
- The Phase 01 deviation (missing direct Domain ref) is FIXED by this phase's csproj change.

## ❌ Failed / Missing

- None.

## ⚠️ Warnings

1. **Pre-existing NU1903 (AutoMapper advisory)** — carried, out of scope.
2. **Approved out-of-phase change**: `private Product()` + `private OrderItem()` parameterless ctors added to 2 Phase-02 Domain files. Required — EF cannot bind owned-type (`Money`) constructor parameters, and the model threw `InvalidOperationException` at build time without them (caught by the build-phase model check, user-approved before applying). Invariants preserved (private, null-forgiving initializers overwritten by EF materialization). Phase-02 validation report stays historical; this report records the amendment.
3. **Plan prose undercount**: overview/checklist say "20 new `.cs` files" but the file table specifies 21 creates (includes `AppDbContextFactory`) — all 21 present; table authoritative, no action.
4. **Phase 05 bindings created by this phase**: middleware MUST map SQL 2601/2627 + FK-violation `DbUpdateException` to 409 (not 500); `AddInfrastructure` + `SeedData` (Development only) must be wired in `Program.cs`; `JWT_SECRET` template line belongs in `.env.example` (API-owned file); initial migration still to be generated via the documented command after Phase 05 wiring.

## 📋 Clean Code Checklist

- [x] SOLID principles followed (DIP: handlers see interfaces; ISP: focused repos; OCP: assembly-scanned configs)
- [x] No code duplication (DRY) — generic base + specific query shapes; normalization rule in one method with lockstep comment
- [x] Simple solutions (KISS) — no spec pattern, no TransactionScope, no audit override, minimal seed
- [x] Meaningful names (IX_/CK_ conventions, `AddInfrastructure`, behavior-revealing method names)
- [x] No magic numbers/strings (210k iterations, 32-char minimum, 24h default, paging clamps, LIKE escape as named consts)
- [x] Proper error handling (fail-fast config with variable names; total-function `ValidateToken`; `Verify` false-on-garbage; `Result` mapping stays in Application)
- [x] All planned files exist
- [x] Namespaces match plan
- [x] Build succeeds with no errors
- [x] No compiler warnings (except pre-existing NU1903)

## 🔧 Recommended Fixes

1. None required for this phase. Binding follow-ups recorded in Warnings §4 for Phase 05.
2. Per AGENTS.md Commit Rules: commit atomically (e.g., `feat(infrastructure): add EF Core persistence and services` + `fix(domain): add EF materialization constructors` + `docs(plans): add phase-04 plan and report` — the Domain touch-up deserves its own commit for traceability), staging explicitly, showing diffs and awaiting user confirmation first.
