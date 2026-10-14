# Plan — phase-08-final-review: Final Review (Clean Code + Sample Test + Runbook)

## 1. Overview

### What this phase accomplishes
Closes the project with verification, not new features. By the end of this phase:
- The entire codebase is swept against Clean Code rules (naming, SRP/short methods, duplication, magic values) and every finding is dispositioned (fix now if trivially safe, otherwise recorded with rationale).
- A compliance checklist is delivered so the user can confirm standard-compliance item by item.
- ONE sample unit test project exists proving handlers are testable: `tests/ShopApi.Application.Tests` with `RegisterCommandHandlerTests` (success + duplicate-email paths), all green via `dotnet test`.
- Final run commands (local + Docker) are consolidated in one place.
- `dotnet build` (Debug + Release) and `dotnet test` both succeed.

### Clean Architecture layers affected
| Layer | Project | State after this phase |
|---|---|---|
| All (review) | `src/*` | Read-only sweep; tiny cleanups ONLY if zero-risk (otherwise findings recorded, code untouched). |
| Tests (new) | `tests/ShopApi.Application.Tests` | **Created**: xunit + Moq project with one handler test class (2 tests). Added to `ShopApi.sln` via CLI. |

### Dependencies on previous phases
- Phases 01–07: the full system (reviewed code, live-verified behavior, docs). This phase re-reads all layers with fresh eyes.
- Known items feeding the sweep (from prior validation reports): slug-generator duplication (sanctioned), SQL error numbers 2601/2627/547 as literals, `Money` truncation mode, `ForAllMembers(Ignore)` pairing, Dev-only migrate/seed, admin-policy coverage, untested `ValidationBehavior` reflection path (a test here would be ideal — but scope says ONE handler sample; note the behavior test as recommended follow-up, do not expand scope).
- Phase 06/07 docs stay authoritative; this phase adds no new product docs except the test project itself.

---

## 2. File-by-File Breakdown

| # | File Path | Action | Layer | Purpose |
|---|---|---|---|---|
| 1 | `tests/ShopApi.Application.Tests/ShopApi.Application.Tests.csproj` | Create | Test | xunit + Moq test project, refs Application |
| 2 | `tests/ShopApi.Application.Tests/Features/Auth/RegisterCommandHandlerTests.cs` | Create | Test | Sample handler tests (success + conflict) |
| 3 | `ShopApi.sln` | Modify | Solution | Add test project via `dotnet sln add` (CLI only, no hand edits) |

**Explicit non-goals:** no product-code refactors beyond zero-risk cleanups (each cleanup, if any, committed separately as `refactor:` with before/after justification); no second test class; no prod compose overlay/CI/vaulting (my own forward notes, NOT in the phase prompt — proposed as follow-ups, not built); no README/API-doc rewrites (typo fixes only if found during review).

---

## 3. Detailed File Specifications

### Clean-code sweep procedure (no file output — findings go to the validation report)
For every `.cs` file under `src/` (excluding `obj`/`bin`/`Migrations/` generated code), check and record:
1. **Naming**: types PascalCase; methods verb-led (`ChangeStatus`, `EnsureEmail`, `ToActionResult`); booleans predicate-named (`IsMain`, `IsSuccess`, `IsAdmin`); no abbreviations except industry-standard (`DTO`, `Id`, `Db`); route templates kebab-free and consistent (`api/<plural>`).
2. **SRP/short methods**: handlers do load→guard→mutate→save→map only; no method over ~40 lines except `MappingProfile` ctor (configuration DSL, exempt) and `Program.cs` (composition root, exempt); cyclomatic hotspots flagged (`ProductRepository.GetPagedAsync` switch, `GlobalExceptionHandler.Map` — both justified by their dispatch nature, record as accepted).
3. **Duplication**: known sanctioned dups (slug generators ×2, paging-clamp lines in 2 query handlers, `Unauthorized("Authentication required.")` ×6) re-confirmed as cheapest-correct; any NEW duplication found must be extracted or justified in the report.
4. **Magic values**: all numeric/string literals must be named consts, config, or framework-required (route templates, claim types, policy names `"frontend"`/`"auth"`, SQL error numbers — the latter get a named-const or explanatory-comment disposition during review: EITHER extract to `SqlErrorNumbers` consts (zero-risk cleanup, allowed) OR record rationale (self-describing with the `2601 or 2627 or 547` pattern + comment). Decision at review time, documented either way.
5. **Error handling**: every handler returns `Result` on all paths (no throws for expected failures); `DomainException` translated at documented boundaries; controllers never try/catch; 500s hide internals outside Development.

### Test project

#### File: `tests/ShopApi.Application.Tests/ShopApi.Application.Tests.csproj`
- **SDK**: `Microsoft.NET.Sdk`; `net8.0`; `Nullable`/`ImplicitUsings` enable (match product projects).
- **Packages** (latest stable at implementation time, pinned + recorded): `xunit` (2.9.x), `xunit.runner.visualstudio` (2.8.x line, must pair with xunit), `Microsoft.NET.Test.Sdk` (17.x), `Moq` (4.2x.x).
- **ProjectReferences**: `..\..\src\ShopApi.Application\ShopApi.Application.csproj` ONLY (tests target Application; Domain arrives transitively — assert no Infrastructure/API refs).
- **Notes**: `IsPackable=false` (test projects never pack).

#### File: `tests/ShopApi.Application.Tests/Features/Auth/RegisterCommandHandlerTests.cs`
- **Namespace**: `ShopApi.Application.Tests.Features.Auth`
- **Type**: `public sealed class RegisterCommandHandlerTests`
- **Fixture wiring** (private helpers, not shared base class — KISS for a sample):
  - `Mock<IUserRepository> _users`; `Mock<IUnitOfWork>` returning `_users.Object` for `Users`; `IMapper` = REAL mapper (`new MapperConfiguration(c => c.AddProfile<MappingProfile>()).CreateMapper()` — exercises the production profile, catching mapping regressions); `IPasswordHasher` stub (hand-rolled 5-line private sealed class returning `"HASHED:" + password` / ordinal compare — no Moq needed for trivial interfaces; `IJwtService` stub returning `"test-token"`).
  - SUT: `new RegisterCommandHandler(_unitOfWork.Object, hasher, jwt, mapper)`.
- **Test 1 `Handle_valid_request_returns_success_with_token_and_normalized_email`**: `EmailExistsAsync` → false; `AddAsync` captures the `User`; `SaveChangesAsync` → 1; act with `("Vera@Shop.Test", "P@ssw0rd123", "Vera", "Fy")`; assert `IsSuccess`, `Token == "test-token"`, `User.Email == "vera@shop.test"` (Domain normalization proven through the handler), captured user's `Role == Customer`, `PasswordHash` starts with `"HASHED:"`.
- **Test 2 `Handle_duplicate_email_returns_conflict_without_creating_user`**: `EmailExistsAsync` → true; act; assert `IsFailure`, `Error!.Code == "Conflict"`; `Verify(AddAsync(...), Times.Never)`; `Verify(SaveChangesAsync(...), Times.Never)`.
- **Conventions demonstrated** (comment header in file): Arrange (mocks + stubs), Act (single `Handle` call with `CancellationToken.None`), Assert (Result shape, no exception assertions for expected failures).
- **Dependencies**: xunit, Moq, Application (handler, DTOs, `Result`), Domain (entity assertions only).

### Solution modification
- `dotnet sln add tests/ShopApi.Application.Tests/ShopApi.Application.Tests.csproj` (CLI-generated GUIDs; verify `dotnet sln list` shows 5 projects).

### Final run commands (delivered in the validation report §🔧/appendix AND verified live during build)
- **Local**: `dotnet restore && dotnet build -c Release && dotnet test`; `dotnet ef database update --project src/ShopApi.Infrastructure --startup-project src/ShopApi.API` (with `DB_CONNECTION`); `dotnet run --project src/ShopApi.API --launch-profile http`.
- **Docker**: `cp .env.example .env` (+ fill `SA_PASSWORD`); `docker compose up --build -d`; `docker compose logs -f api`; probes from §4 step 5; `docker compose down` (volume kept).

---

## 4. Implementation Order

1. Run the Clean-Code sweep (§3 procedure) across `src/`; write findings to a scratch list (do NOT edit code yet).
2. Disposition each finding: zero-risk cleanup (typo, named-const extraction, comment) → apply immediately in `src/`; anything behavioral → record as recommendation, code untouched.
3. `dotnet new xunit -n ShopApi.Application.Tests -o tests/ShopApi.Application.Tests -f net8.0` (or `classlib` + hand-added packages — decision: `xunit` template for correct defaults, then delete template noise `UnitTest1.cs`), set Nullable/ImplicitUsings, `IsPackable=false`.
4. Add package refs (xunit, runner, Test.Sdk, Moq — pinned versions) + Application `ProjectReference`; `dotnet sln add` the test project.
5. Write `RegisterCommandHandlerTests.cs` per §3 (stubs + 2 tests).
6. `dotnet test` → green (2/2 passed); on failure, fix test FIRST (product code is live-verified — suspect the test), escalate to user only if product behavior is genuinely wrong.
7. `dotnet build -c Release` full solution (5 projects) → 0 errors.
8. Live final-command rehearsal: `docker compose down -v` (fresh volume) → `up --build -d` → `/health` 200 → register→login→cart→order full flow incl. **order placement + admin status change** (the two flows never executed live before — use a JWT_SECRET scratch `.env`, promote-via-DB? Admin promotion has no endpoint: create order as customer, then update status requires Admin token — obtain by `UPDATE Users SET Role=1` via sqlcmd against the container, documented as demo-only seed hack; record exact sqlcmd line). Then `docker compose down`, delete scratch `.env`.
9. `git status` review: 2 additions (csproj + test class) + 1 sln modification + any zero-risk cleanups (each listed); 0 other changes.

## 5. Code Patterns & Standards

- **Tests mirror production patterns**: Result-shape assertions (never exception-chasing for expected failures); Moq only at architectural seams (`IUnitOfWork`/repos); REAL mapper (profile under test); hand stubs for trivial pure interfaces (clearer than mock setups for 1-method fakes).
- **Review standards**: findings classified `Must-fix` (bugs, lies in docs, security) / `Should-fix` (readability, const extraction) / `Accepted` (sanctioned dups, dispatch switches, exempt DSL/composition files); nothing behavioral changes without a failing test or live repro.
- **Naming**: test project `ShopApi.Application.Tests` (mirrors SUT); test class `{Handler}Tests`; test methods `Handle_state_returns_expectation` (underscored BDD phrasing, xunit convention).
- **KISS**: one test class, two tests, no fixture base, no Bogus/AutoFixture (sample must be readable by juniors in one sitting).

## 6. Edge Cases & Validation

- **xunit/Moq version pairing**: runner must match xunit minor; Test.Sdk 17.x for `dotnet test` discovery; verify versions resolve without NU1605/NU1903-class warnings beyond the known AutoMapper one.
- **Flaky-live-test avoidance**: no live HTTP tests in this phase (compose stack is manual-verified, not CI-asserted —_no testcontainers dependency introduced_).
- **Admin-flow demo hack**: `UPDATE Users SET Role = 1 WHERE Email = ...` via container sqlcmd is demo-only and MUST be labeled as such (never a documented product flow; API_DOCUMENTATION.md already states no promotion endpoint).
- **Secret hygiene**: scratch `.env` for rehearsal deleted afterwards; `git status` must never show `.env`.
- **Scope guard**: if the sweep finds a REAL bug, stop and ask the user (new behavior change needs its own plan/mini-phase — do not smuggle fixes into "final review").

## 7. Verification Checklist

- [ ] Sweep covers 100% of hand-written `src/**/*.cs` ( owns generated `Migrations/*` and `obj/` excluded); findings ledger complete with Must/Should/Accepted dispositions
- [ ] Zero-risk cleanups (if any) each build green individually; no behavioral change without user sign-off
- [ ] Test project builds; `dotnet test` → 2/2 passed; `dotnet sln list` shows 5 projects
- [ ] Test asserts production mapper + Domain normalization through the handler (not mocks of everything)
- [ ] Live rehearsal: fresh-volume compose boot → health 200 → full customer flow → admin status change → teardown; all commands in §3 recorded verbatim as executed
- [ ] `dotnet build -c Release` 0 errors, no NEW warnings across all 5 projects
- [ ] `git status`: 2 additions + 1 sln modification (+ listed cleanups), 0 other changes; `.env` absent
- [ ] Validation report delivers the user-facing compliance checklist + final commands (phase items 2 & 4)
