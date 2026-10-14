# Validation Report: phase-02-domain

## Summary

- **Status**: ✅ PASSED
- **Build Status**: ✅ (`dotnet build`: 0 errors; `ShopApi.Domain` alone: 0 warnings, 0 errors)
- **Test Status**: N/A (no test projects exist — expected; tests arrive in a later phase)

## Files Checked

| # | Planned File | Exists | Matches Plan | Notes |
|---|---|---|---|---|
| 1 | `src/ShopApi.Domain/Common/BaseEntity.cs` | ✅ | ✅ | `abstract`, Guid+UTC stamps, `Touch()`; exact spec |
| 2 | `src/ShopApi.Domain/Exceptions/DomainException.cs` | ✅ | ✅ | Both ctors present |
| 3 | `src/ShopApi.Domain/Enums/UserRole.cs` | ✅ | ✅ | `Customer = 0`, `Admin = 1` |
| 4 | `src/ShopApi.Domain/Enums/OrderStatus.cs` | ✅ | ✅ | All 5 members with explicit values in flow order |
| 5 | `src/ShopApi.Domain/ValueObjects/Money.cs` | ✅ | ✅ | `sealed record`, `Zero`/`From`, `+`/`*` with guards, `ToString`, 2-decimal truncation; see Warnings §2 (record shape) |
| 6 | `src/ShopApi.Domain/Entities/User.cs` | ✅ | ✅ | Normalized email, hash-only password, `UpdateName`/`ChangePasswordHash`/`PromoteToAdmin`, `FullName`, read-only navigations |
| 7 | `src/ShopApi.Domain/Entities/Address.cs` | ✅ | ✅ | Entity (not VO) per plan decision; `Update()`; length rules as specified |
| 8 | `src/ShopApi.Domain/Entities/Category.cs` | ✅ | ✅ | Nullable `ParentId`, self-parent guard, `Rename` regenerates slug, Phase-04 TODO present |
| 9 | `src/ShopApi.Domain/Entities/Product.cs` | ✅ | ✅ | `Money Price`, `UpdateDetails`/`ChangePrice`/`AdjustStock`/`SetCategory`/`AddImage`/`SetMainImage`; empty description → `string.Empty` |
| 10 | `src/ShopApi.Domain/Entities/ProductImage.cs` | ✅ | ✅ | http(s) scheme check, `Mark/UnmarkAsMain` |
| 11 | `src/ShopApi.Domain/Entities/CartItem.cs` | ✅ | ✅ | 1–999 bounds, `ChangeQuantity`/`Increase`/`Decrease`, no zero-quantity state |
| 12 | `src/ShopApi.Domain/Entities/Order.cs` | ✅ | ✅ | `Create` factory, `ORD-yyyyMMdd-XXXXXX` numbers, exact transition table, total recomputed from items, `RecalculateTotal` private per plan default |
| 13 | `src/ShopApi.Domain/Entities/OrderItem.cs` | ✅ | ✅ | Immutable (no mutators), price snapshot, computed `LineTotal`, `internal AttachToOrder` two-step as planned |
| 14 | 5× `.gitkeep` deletions | ✅ | ✅ | `git status` shows exactly 13 additions + 5 deletions; csproj byte-identical (untouched) |

## ✅ Passed

- All 13 files exist with exact namespaces (`ShopApi.Domain.{Common,Entities,Enums,Exceptions,ValueObjects}`), class names, signatures, and access modifiers from the plan.
- Every constructor validates required state; every mutator calls `Touch()` (spot-verified across all entities); collections exposed as `IReadOnlyCollection<T>`.
- `Order.ChangeStatus` implements the Pending→Paid→Shipped→Delivered / Cancelled-from-Pending-or-Paid table with idempotent same-status no-op and terminal-state lockout.
- `Order.Create` enforces non-empty lines, 1–999 quantities, non-null prices, single currency; total recomputed from snapshots.
- `Money` enforces non-negativity, 3-letter ISO currency, same-currency arithmetic.
- Single-main-image invariant managed by `Product.AddImage`/`SetMainImage`; slug generators reject empty results; both carry Phase-04 unique-index TODOs.
- Zero EF leakage: no `EntityFramework`/`DataAnnotations`/`virtual` usages found in Domain sources; navigation properties nullable; no DataAnnotations attributes.
- `ShopApi.Domain.csproj` unmodified — dependency-free status proven, not just claimed.
- `dotnet build` full solution: 0 errors. Domain project alone: 0 warnings, 0 errors.

## ❌ Failed / Missing

- None.

## ⚠️ Warnings

1. **Pre-existing NU1903 (AutoMapper 13.0.1 advisory) still present** — 2× warnings at solution level, Application-scoped. Unrelated to this phase; carried over from Phase 01. Revisit when a patched AutoMapper ships.
2. **Informational — `Money` record shape**: plan described a "positional record with custom constructor logic"; implementation uses an equivalent `sealed record` with explicit `init` properties plus a validating constructor. Behavior matches the spec exactly (factories, operators, equality, rounding); the chosen shape is arguably cleaner since positional records complicate ctor validation. No action.
3. **Informational — slug generator accepts Unicode letters/digits** (`char.IsLetterOrDigit`): non-Latin product/category names produce non-empty (non-ASCII) slugs rather than throwing. Sensible for an international shop; no spec conflict (plan only mandates rejecting empty results).
4. **Carried over from Phase 01**: Infrastructure still lacks its direct `ShopApi.Domain` reference (blocked on a fix outside this phase's scope; required before Phase 04 entity mapping).

## 📋 Clean Code Checklist

- [x] SOLID principles followed (SRP per aggregate; OCP via behavior methods not conditionals in callers; aggregate-root encapsulation)
- [x] No code duplication (DRY) — only the plan-sanctioned 5-line slug-generator duplication between `Category`/`Product` (documented, avoids cross-aggregate coupling)
- [x] Simple solutions (KISS) — single exception type, no premature abstractions, immutable order lines
- [x] Meaningful names (`ChangeX`/`Adjust`/`EnsureX`/`AttachToOrder` conventions as planned)
- [x] No magic numbers/strings (named `private const` bounds; `OrderNumberChars` charset constant)
- [x] Proper error handling — `DomainException` as the single failure type per plan (`Result<T>`/ProblemDetails correctly deferred to Phase 03)
- [x] All planned files exist
- [x] Namespaces match plan
- [x] Build succeeds with no errors
- [x] No compiler warnings (Domain scope; solution-level NU1903 is pre-existing and out of scope)

## 🔧 Recommended Fixes

1. None required for this phase. Optional follow-ups (all non-blocking):
   - Restore the missing direct `Infrastructure → Domain` reference before Phase 04 (`dotnet add src/ShopApi.Infrastructure reference src/ShopApi.Domain`).
   - Track the AutoMapper GHSA advisory for a future upgrade.
   - Per AGENTS.md Commit Rules: commit this phase atomically (e.g., `feat(domain): add shop entities and value objects`), staging the 13 additions + 5 deletions explicitly, showing the diff and awaiting user confirmation first.
