# Engram Backup — Checkbus (2026-09-30)

## Purpose

This is a full snapshot of Engram's `checkbus`-project memory, taken before the user reinstalls the Engram MCP plugin (which wipes its stored observations for this project). It also captures the standing lessons stored separately as local auto-memory files (which survive the reinstall on their own, but are included here for one unified reference), plus everything that happened in the session that produced this backup, which had not yet been saved anywhere. A fresh model session with no Engram history should be able to read this single file and resume work without re-deriving anything.

Original Engram observation IDs are cited throughout (`#NN`) so a future session can cross-reference if Engram history is ever recovered or partially restored.

---

## Active Work: api-jwt-authentication

This is the **only SDD change with implementation currently in flight**. It is nearly done — one phase (verification/regression) and a couple of housekeeping steps remain.

### Status Summary

- **Proposal** (#90), **resolved open questions** (#91), **spec** (#92), **design** (#93), **finalized design decisions D-1..D-5** (#94), **tasks** (#95) — all complete and stable.
- **Role modeling changed twice after design was finalized** — see "Role Modeling Decision History" below. The CURRENT authoritative shape is a C# enum (#99), not the string/entity approaches tried earlier.
- **Apply progress** (#96, revision 7) — Work Units 1–4 complete and committed. Work Unit 4 is the last *implementation* unit; only Phase 7 (verification) and two loose ends remain.
- Commits so far, in order: `92456c5` (Phase 1+2, reviewed & corrected via `f60bc21`, cleanup `058fae7`) → `e7bd6f2` (Phase 3, reviewed) → cleanup `be01c05` → `c927cca` (Phase 4 v1, string roles — superseded) → `5ba951a` (Phase 4 v2, enum roles — **not yet reviewed**) → `be014ef` (Phase 5+6.1-6.2, BFF forwarding + Dashboard — **not yet reviewed**).

### Pending Next Steps (in priority order)

1. **Task 6.3 — Manual E2E** (human/browser step, cannot be automated): log in through the Blazor UI, land on `/dashboard`, confirm the rendered username/role/organization id came from `GET /api/auth/me` — proves the token reached the API from an interactive circuit.
2. **Phase 7.1 — Full regression**: run `dotnet test` from repo root. Last known baseline (as of `be014ef`): **142 passed / 6 failed / 148 total**. The 6 failures are `AuthLoginEndpointTests` (4) + `WebTests` (2), caused by Docker Desktop's daemon being unreachable on this machine (Aspire/Postgres integration tests) — confirmed environmental across every work unit, not a code defect. **Do not touch the local Postgres database or try to fix Docker as part of this change** — standing constraint.
3. **Phase 7.2 — Success Criteria checklist walkthrough**: read the proposal's Success Criteria section and confirm each is satisfied by a specific completed task/commit (see the tasks artifact's own Phase 7.2 checklist below for the exact list).
4. **RDD native review** — commits `5ba951a` (Role enum) and `be014ef` (BFF token forwarding + Dashboard identity) have **not** been submitted for native review yet. This is an orchestrator-owned post-apply step.
5. **Standing unresolved risk**: the enum's `Mecanico` (ASCII identifier) differs from the old string constant `Mecánico` (accented). If the user's local `checkbusdb` has any pre-existing `Users.Role = 'Mecánico'` (accented) row, EF Core's enum conversion (`HasConversion<string>()`) will throw on read. **Not verified against the actual database** — user should check before relying on the enum-typed Role against existing data.

### This Session's Dispatch Bug (2026-09-30)

In the session that produced this backup, the user asked to continue `api-jwt-authentication` via SDD apply (Phase 7). What happened:

- Ran the SDD Session Preflight (`AskUserQuestion`) successfully: **Pace = Interactive, Artifacts = Engram, PR strategy = Single PR**.
- Ran `gentle-ai sdd-status api-jwt-authentication --cwd <repo> --json --instructions` — confirmed the known openspec-gap bug (see Standing Lessons below): it reports `nextRecommended: sdd-new` and `blockedReasons: ["Active OpenSpec change not found: api-jwt-authentication."]` because no `openspec/` folder exists in this repo. Expected/known; proceeding via Engram is user-approved regardless.
- Attempted to launch the `sdd-apply` sub-agent (`Agent` tool, `subagent_type: "sdd-apply"`) to run Phase 7. **The dispatch was rejected twice in a row**, immediately after a fresh, correctly-answered `AskUserQuestion` preflight call with no other tool calls in between, with the exact error:
  > `SDD child dispatch refused: parent-confirmed SDD preflight is missing, invalid, or uncorroborated. Ask the canonical grouped preflight with AskUserQuestion and stop before retrying.`
- This looks like a genuine bug in whatever hook enforces the `gentle-ai:sdd-session-preflight` gate (from `~/.claude/skills/_shared/sdd-orchestrator-workflow.md`), not a user or model error. The `gentle-ai` CLI has no separate preflight-confirmation subcommand to call manually, and the workflow doc says the runtime itself derives/prepends the preflight block — that apparently wasn't happening correctly for the `sdd-apply` subagent type in this environment/version.
- Presented the Gentle AI Provider Defect Handoff envelope to the user (report / continue-without-reporting / stop). **User chose "Parar acá" (stop here).** No GitHub issue was filed, no further dispatch was attempted, no code or files were modified.

**For a future session**: `sdd-apply` could not be dispatched for this change via the normal `Agent` tool route due to this bug, as of 2026-09-30. Options: (a) retry the dispatch once more in case it was transient/version-specific, (b) do Phase 7's verification work directly/inline instead of delegating to `sdd-apply` — it's read-only verification (`dotnet test` + reading the proposal's success criteria against already-completed commits), well within what an orchestrator can do inline without violating delegation triggers, since it's bounded reads/bash, not a 2+-file write, or (c) investigate/report the dispatch-gate bug itself if it recurs.

### Role Modeling Decision History (important — read in order)

The `User.Role` representation changed twice after the design (#93) was finalized. **Only the last decision (#99) is current.**

1. **Original design (#93/#94)**: `Role` stays a normal EF entity (pre-existing `Role.cs`, `DbSet<Role>`), with a `Checkbus.ApiService.Domain/Authorization/Roles.cs` static class of `const string` values (`Administrador`, `Chofer`, `Planificador`, `Mecanico` = "Mecánico") used at `[Authorize(Roles = ...)]` call sites and for seeding.
2. **Revision attempt — dynamic, DB-driven roles (#97, `dynamic-roles-proposal`, SUPERSEDED, never applied)**: the user asked why a `Roles` class was needed at all ("limits roles to code instead of a normalized table") and asked for a proposal on fully dynamic role authorization. Claude researched and presented 3 options (A: roles as pure data, minimal change; B: custom `IAuthorizationPolicyProvider` with DB-validated roles per Microsoft's documented pattern; C: full permission/capability model). **The user rejected all three** after reflection and went the opposite direction.
3. **Decision #98 (SUPERSEDED by #99, but its DB-removal part still stands)**: roles become **static and code-defined**, not a normalized table. The `Role` entity and `DbSet<Role> Roles` were **removed entirely** — no more `Role` table, no more `User.RoleId` FK. `User.Role` became a plain `string`. Implemented in commit `c927cca`.
4. **Decision #99 (CURRENT, FINAL)**: after Claude flagged that a plain `string Role` has zero validation, the user chose a **C# enum** instead of adding runtime validation — an invalid value becomes a compile error, not a runtime concern. Implemented in commit `5ba951a`. Concrete shape:
   - `public enum Role { Administrador, Chofer, Planificador, Mecanico }` in `Checkbus.ApiService.Domain/Authorization/` — replaces the old `Roles` static string-constants class entirely (deleted).
   - `User.cs`: `Role` property is `required Role` (the enum), not `string`.
   - EF Core stores the enum **as a string** via `HasConversion<string>()` in `CheckbusDbContext.OnModelCreating` (not the EF int default) — for pgAdmin/SQL readability.
   - `[Authorize(Roles = "...")]` call sites use `nameof(Role.Administrador)` instead of the old string constant — compile-time-checked.
   - `JwtGenerator`: claim value is `user.Role.ToString()`.
   - `ICurrentUserService.Role` (the claims-read-side property) stays `string?` — deliberately unchanged; it reads the JWT claim, which is inherently a string regardless of DB modeling.
   - **Same destructive-DB-action constraint applies**: Claude must NOT run any DROP/schema-changing command against the user's local Postgres. The user handles dropping/recreating their local dev database themselves when the schema changes; Claude only flags when it's needed.

### Full Apply Progress (verbatim summary, Engram #96, revision 7 — the single most important artifact for resuming work)

**Work Unit 1 (Phase 1 + Phase 2)** — CLOSED, reviewed, acknowledged. Commit `92456c5` on `main` (13 files, +438/-4). RDD native review required one correction (`f60bc21`), then approved. Follow-up cleanup `058fae7` (RDD declined, ordinary policy).

**Work Unit 2 (Phase 3: Current-User Context & `/api/auth/me`)** — CLOSED, reviewed, acknowledged. Commit `e7bd6f2` on `main` (+211/-0), approved directly, zero corrections. Follow-up cleanup `be01c05`, also approved directly.

**Work Unit 3 / 3b (Phase 4: Role Seeding)** — CLOSED under the enum-Role contract (commit `5ba951a`, see Role Modeling history above). **NOT yet submitted for RDD review.**

**Work Unit 4 (Phase 5 + Phase 6.1-6.2)** — DONE. Commit `be014ef` on `main` — "feat(web): forward JWT to API via Application Scope Handler and show identity on Dashboard" (9 files changed, +353/-0). **NOT yet submitted for RDD review.**

What Work Unit 4 built (Phase 5 — BFF Token Forwarding, Application Scope Handler pattern per D-2 corrected per #94):
1. `Checkbus.Tests/Infrastructure/FakeAuthenticationStateProvider.cs` — canned `AuthenticationState`.
2. `Checkbus.Tests/Infrastructure/StubHttpMessageHandler.cs` — captures `CapturedRequest`, returns a canned response.
3. `Checkbus.Tests/Web/AuthenticationStateHandlerTests.cs` — 3 tests (token present → Bearer header set; token absent → no header; other request data forwarded unmodified).
4. `Checkbus.Web/Services/AuthenticationStateHandler.cs`: `public sealed class AuthenticationStateHandler : DelegatingHandler`.
5. `Checkbus.Web/Services/ApplicationScopeHandler.cs`: `public class ApplicationScopeHandler(IServiceProvider serviceProvider) : DelegatingHandler`.
6. `Checkbus.Web/Extensions/ApplicationScopeHandlerExtensions.cs`: `AddApplicationScopeHandler(this IHttpClientBuilder)`.
7. `Checkbus.Web/Program.cs`: chained `.AddApplicationScopeHandler().AddHttpMessageHandler<AuthenticationStateHandler>()` onto the existing `AddHttpClient("apiservice", ...)` registration.

Phase 6.1-6.2 — Dashboard Consumer:
8. `Checkbus.Web/Contracts/CurrentUserResponse.cs`: Web-side mirror record `(Guid? UserId, Guid? OrganizationId, string? Role, string? Username)`.
9. `Checkbus.Web/Components/Pages/app/Dashboard.razor`: resolves the client via `ServiceProvider.GetRequiredKeyedService<HttpClient>("apiservice")` (**not** `IHttpClientFactory.CreateClient("apiservice")` — see deviation below), calls `GetFromJsonAsync<CurrentUserResponse>("auth/me")`, renders username/role/organization id.

**Two deliberate deviations from the task list, both disclosed to the orchestrator at the time:**
- **Deviation 1** (pre-authorized): `AuthenticationStateHandler` resolves `AuthenticationStateProvider` from `request.Options[ApplicationScopeHandlerExtensions.ScopeKey]` inside `SendAsync`, not via constructor injection — because `IHttpClientFactory` creates `DelegatingHandler`s in a DI scope separate from the Blazor circuit.
- **Deviation 2** (discovered independently by the apply agent, NOT pre-authorized but accepted): `AddApplicationScopeHandler()` registers the scope-aware pipeline as a **keyed scoped** `HttpClient`, separate from the plain named-client pipeline `IHttpClientFactory.CreateClient(name)` returns. Had Dashboard used `CreateClient("apiservice")` as the original task said, the request would never pass through `ApplicationScopeHandler` and the token would never be attached — silently defeating the whole Phase 5 mechanism. Corrected to `GetRequiredKeyedService<HttpClient>("apiservice")`.

**Also folded into commit `be014ef`**: pre-existing UNCOMMITTED cookie-authentication infrastructure in `Program.cs` (`AddAuthentication().AddCookie(...)`, `AddAuthorization()`, `AddCascadingAuthenticationState()`, `POST /account/login`, `POST /logout`, `LoginApiResult` record) that predated this apply run (origin unknown to the apply agent — likely separate prep work, git-inseparable from the new lines since they sit in the same diff hunk). This is the source of the `"access_token"` claim that `AuthenticationStateHandler` reads. **Disclosed consequence**: `git revert be014ef` would also remove this cookie-auth wiring, not just the token-forwarding piece.

**TDD Cycle Evidence (Work Unit 4)**: `dotnet test --filter "FullyQualifiedName~AuthenticationStateHandlerTests"` → 3/3 passed. Full `dotnet test` → 142 passed, 6 failed, 148 total (~2m4s) — the 6 failures are the same `AuthLoginEndpointTests`(4)/`WebTests`(2) Docker/Aspire environmental failures confirmed across every prior work unit. `dotnet build Checkbus.slnx` → 0 errors (one pre-existing unrelated `MUD0002` warning on `Home.razor`).

**Rollback boundary**: `git revert be014ef` removes the two handler files, the extension method, `CurrentUserResponse.cs`, the Dashboard edit, the 3 test files, AND reverts `Program.cs` to its pre-`be014ef` state (losing the cookie-auth wiring too, per the disclosure above).

### Proposal, Spec, Design, Tasks (source artifacts — full detail)

**Proposal** (`sdd/api-jwt-authentication/proposal`, #90): `Checkbus.ApiService` — the real trust boundary — had **no authentication or authorization at all** (verified: no `AddAuthentication`/`AddAuthorization`/`UseAuthentication` in `Program.cs`). Option A (JWT bearer validation on the API) accepted over sharing the Web cookie.

**Resolved open questions** (#91, 2026-09-27): (1) role rename `"Admin"` → `"Administrador"`, canonical role set = Administrador/Chofer/Planificador/Mecánico (in Spanish, used directly as the technical value), dedupe `CheckbusDbSeeder.Seed`/`SeedAsync`; (2) JWT expiration lengthened to 4 hours (`ExpirationMinutes=240`) to match the Web cookie's existing 4h lifetime, no refresh-token mechanism; (3) add `GET /api/auth/me` as the end-to-end proof endpoint.

**Spec** (#92) — five new capabilities, full requirements/scenarios (still accurate for authentication/authorization/current-user-context/bff-token-forwarding; the **role-seeding** capability section is superseded by the Role Modeling Decision History above — roles are no longer a seeded DB table):
- `api-authentication`: JWT bearer validation (signature/issuer/audience/lifetime), 4h expiration, login/health/OpenAPI/Aspire-default endpoints stay unauthenticated.
- `api-authorization`: role-based endpoint gating via `[Authorize(Roles = "...")]`, 403 for wrong role vs 401 for no auth, role claim round-trips correctly.
- `current-user-context`: `ICurrentUserService` sourced exclusively from validated claims (anti-IDOR — a client-supplied `OrganizationId` can never override the token's), `ITenantEntity` made public, `GET /api/auth/me` proof endpoint.
- `bff-token-forwarding`: Web forwards the `"access_token"` cookie claim as `Authorization: Bearer` on every outgoing call to the API including from an interactive circuit; a 401 from the API redirects to login without crashing.
- ~~`role-seeding`~~ (superseded — no longer a seeded table; see Role Modeling Decision History).

**Design** (#93, the original, pre-D-2-correction and pre-Role-changes version — read alongside #94/#98/#99 for what's actually current): six additive moves — API validation bound to the same `JwtOptions` singleton `JwtGenerator` uses; attribute-based `[Authorize(Roles=...)]`; `ICurrentUserService` as an Application-layer port implemented in the API composition root, registered Scoped; `ITenantEntity` made public; one shared idempotent+convergent seeder body; BFF forwarding (**original design proposed a typed `HttpClient` reading `AuthenticationStateProvider` directly — this was CORRECTED by D-2/#94 to the Application Scope Handler pattern actually implemented**). Full architecture-decision detail (13 numbered decisions, data flow diagrams, file-change tables, testing strategy, threat matrix, migration/rollout) preserved in the original observation; the corrected BFF mechanism is what's described under "What Work Unit 4 built" above.

**Final design decisions D-1 to D-5** (#94, 2026-09-27): D-1 confirmed (global `AuthorizeFilter` for baseline 401, but every role-specific endpoint still needs its own `[Authorize(Roles=...)]`); **D-2 corrected** — supersedes the design doc's unverified typed-client approach with Microsoft's documented "Application scope handler" pattern (verified against `learn.microsoft.com/aspnet/core/blazor/security/additional-scenarios`), which is what Work Unit 4 actually implements; D-3 confirmed (seeder convergence branch for half-migrated DBs — **now moot per the Role Modeling changes**, no more DB-seeded roles); D-4 confirmed (401/403 use framework-default empty bodies, tests assert status codes only); D-5 confirmed (`ClockSkew = TimeSpan.Zero` for deterministic expired-token tests).

**Tasks** (#95, revision 31) — 7 phases, Review Workload Forecast flagged ~1150-1250 estimated lines (High risk, chained PRs recommended), split into 4 work units matching the apply-progress structure above. Phase 7 (the only remaining phase) is:
- **7.1**: Run `dotnet test`, confirm Phases 1–6 pass and the pre-existing Aspire/Docker failures are unchanged, not new regressions.
- **7.2**: Walk the proposal's Success Criteria checklist end-to-end, confirming each is satisfied by a specific task: 401 on missing token, 200 on valid token, 401 on expired/wrong-issuer/wrong-audience/bad-signature, 403 on wrong role / 200 on correct role with claim round-trip asserted, login/health/OpenAPI/Aspire-default endpoints stay public, `ICurrentUserService` correctness, Web forwards the bearer token from an interactive circuit, end-to-end authenticated Dashboard call with server-side `OrganizationId`, roles working correctly (superseding the original "four canonical roles seeded" wording — now the enum), `ITenantEntity` public, full `dotnet test` green modulo the disclosed pre-existing failures.

---

## Other In-Progress Explorations (no proposal yet — exploration phase only)

Both of these are **deferred behind `api-jwt-authentication`** per explicit user instruction (#89) and have only reached the `explore` phase. Neither has a proposal, spec, design, or tasks artifact. Neither should be assumed ready to implement.

### register-account

**Explore** (`sdd/register-account/explore`, #88, 2026-09-27): "Registrar Cuenta" — an org admin needs a way to add new users (drivers, planners, mechanics, other admins). Key findings at exploration time: `User` had no `FirstName`/`LastName`, only `Username`; `Role` was still a free-text EF entity (pre-dates the Role Modeling changes above); the API had **zero authentication** (this exploration is actually what surfaced the gap that became the `api-jwt-authentication` change — the two are related, `api-jwt-authentication` was split out first per user request); `IUserRepository` only has `FindByEmailAsync`, no insert method; no `DocumentNumber` uniqueness constraint.

**Decision history** (two superseding notes, read in order):
1. **#89 (SUPERSEDED)**, 2026-09-27: user initially said `Username` should hold "Nombre + Apellido" concatenated — do not add separate `FirstName`/`LastName` columns.
2. **#100 (CURRENT)**, 2026-09-29: the user **reverted** that decision. New decision: **remove `Username` from `User` entirely**, replace with two separate fields `Name` and `Surname`. User identification (JWT `ClaimTypes.Name`, `Dashboard.razor`, `CurrentUserResponse`, `CheckbusDbSeeder`'s seeded admin username, `LoginCommandResult`) switches from `Username` to **`Email`** instead. This widens the scope of `register-account` beyond the new feature itself — it also touches `JwtGenerator`, `CheckbusDbSeeder`, `Dashboard.razor`, `LoginCommandHandler`/`LoginCommandResult`, and their tests (`CheckbusDbSeederTests`, `JwtGeneratorTests`, `TestUserFactory`, `ApiAuthenticationTests`). Other previously-open questions also resolved: `DocumentNumber` must be unique **per organization** (not global); `CreatedAt`/`UpdatedAt` are set explicitly in the command handler (`= DateTime.UtcNow`), not via an EF interceptor; admin-role authorization uses `[Authorize(Roles = nameof(Role.Administrador))]`, matching the now-current enum-based Role from `api-jwt-authentication`.

**Next step recorded**: start `sdd-propose` for `register-account` incorporating these decisions, once `api-jwt-authentication` closes.

### register-organization-admin

**Explore** (`sdd/register-organization-admin/explore`, #87, 2026-09-27): UC2 "Registrar empresa" (`RegisterOrganization` + first admin `User`). Key findings: no EF Core migrations exist anywhere (schema via `EnsureCreatedAsync`, Development-gated only); **no `IOrganizationRepository` exists**; `Organization.LogoUrl` is a required non-nullable string with no default, which conflicts with a fresh self-registration flow with no logo yet; the seeder created a brand-new `Role` row named "Admin" on every run rather than looking one up (pre-dates the Role Modeling changes — now moot, roles are an enum); `AuthExceptionHandler` only recognizes the three login exceptions today, would need new cases or a sibling handler for duplicate-CUIT/Slug/Email failures.

**Open product questions, never answered** (still open as of this backup): is registration self-service/public or admin-invited; is email verification required; can `Organization.Slug` be auto-generated from `Name`; is `CUIT` format-validated as an Argentine tax ID or just a free-text unique string; is Organization+User creation required to be transactional; is `Organization.LogoUrl` in scope for this flow at all (needs to become optional/nullable if so).

**No decision notes exist for this change beyond the explore** — it has had no further sessions since 2026-09-27.

---

## Aborted: login-app-shell

**Status: ABORTED after Phase 2 of 4, NOT complete, NOT archived as success.** (#75, 2026-09-24). The `login-app-shell` change (real `Login.razor` + Dashboard + authenticated `MainLayout`/`NavMenu` shell, cookie-backed session via a JWT ticket handoff) was aborted by the user.

**Why it was aborted**: the user found the ticket-handoff architecture (`AuthApiClient`, `LoginHandoffStore`, `AuthEndpoints` — needed because Blazor Server components can't emit `Set-Cookie` from inside a live circuit) confusing when the concrete new classes first surfaced in the `sdd-apply` phase reports, having only seen a high-level architecture summary at design-approval time. After a clarifying explanation (JWT = API credential, cookie = Blazor Web session — two different problems), the user still chose to abort.

**Revert action taken**: local-only commits `bad382e` (API contract) and `9374509` (web auth plumbing) were removed via `git stash push -u` → `git reset --hard 5b85895` → `git stash pop`. No remote was configured, so this was a clean local history rewrite. Verified clean afterward: `Checkbus.Web/Services/` and `Checkbus.Web/Endpoints/` no longer existed, `LoginCommandResult.cs`/`JwtGenerator.cs` were back to pre-session shape.

**Lesson recorded for future sessions**: if a future session revisits building a real login for `Checkbus.Web`, the exploration/design work here is still valid technical research (the Blazor-Server-can't-set-cookies constraint and the ticket-handoff pattern are real findings, not mistakes) — but re-confirm the user actually wants that architecture, ideally by walking them through the concrete file/class list *before* writing code, not just the narrative summary.

**Important note**: the `api-jwt-authentication` change's BFF token forwarding (Work Unit 4, commit `be014ef`) is a **different, simpler mechanism** (the Microsoft-documented Application Scope Handler pattern) than this aborted change's ticket-handoff design — there is no conflict between them. However, the pre-existing uncommitted cookie-authentication plumbing in `Program.cs` that got folded into `be014ef` (see Apply Progress above) may be a remnant that predates or postdates this abort; its exact origin was "unknown to the apply agent" at the time.

**Preserved artifacts** (historical record only — do not treat as reflecting current code state): explore (#69), proposal (#70), spec (`sdd/login-app-shell/spec`, #71 — not individually re-fetched for this backup, but referenced throughout the chain), design (#72, revision 3), tasks (#73, revision 3). Full content of explore, proposal, design, and tasks preserved below for reference since they contain real, reusable technical research (MudBlazor shell patterns, the Blazor-Server-cookie constraint, the ticket-handoff security reasoning) even though the change itself was abandoned.

<details>
<summary>login-app-shell: full exploration, proposal, design, tasks (click to expand in a Markdown viewer that supports it — otherwise just read past this block)</summary>

**Key facts from explore (#69)**: `LoginCommandResult` had no `OrganizationName` field even though `UserRepository.FindByEmailAsync` already eager-loads `.Include(u => u.Organization)` — so surfacing the org name would be free. `Checkbus.Web` had zero HTTP client registration and zero auth infrastructure of any kind despite Aspire service-discovery wiring already existing at the orchestration level. `Checkbus.Web` runs exclusively in Blazor Server `InteractiveServer` mode everywhere (`App.razor:19`) with no WASM/Auto fallback — a component inside a live circuit can never emit `Set-Cookie`. `MainLayout.razor` already had the correct MudBlazor `MudLayout > MudAppBar + MudDrawer + MudNavMenu` skeleton (just placeholder branding); `NavMenu.razor` was dead unused Bootstrap-HTML code.

**Proposal (#70) approach**: interactive `MudForm` login stays in the circuit (preserves inline red error UX); on success, the circuit stores the full login result server-side under an opaque single-use 256-bit random ticket with a 60s TTL, then does a full-page `NavigationManager.NavigateTo("/auth/complete?ticket=...", forceLoad: true)`; the `/auth/complete` minimal endpoint (real `HttpContext`) redeems-and-evicts the ticket, builds the `ClaimsPrincipal`, calls `SignInAsync`, redirects to `/dashboard`. This is the same shape as a standard OAuth callback — an opaque short-TTL reference in the URL, never the credential itself. Rejected alternatives: JWT in the redirect URL (credential leak via history/logs/Referer); static SSR form (loses interactive inline-error UX).

**Design (#72) key decisions**: no custom `AuthenticationStateProvider` needed — `AddCascadingAuthenticationState()` plus the framework's built-in server-side provider (seeded from the cookie-authenticated `HttpContext.User` at circuit start) is sufficient, verified against Microsoft Learn; ticket store is the shared `IMemoryCache` with an explicit `lock` around redeem for atomicity; `LoginOutcome` has 4 cases (`Success`/`FieldErrors`/`InvalidCredentials`/`Unavailable` — the 4th because the API can also be unreachable/5xx after resilience retries exhaust); logout is `POST` with an antiforgery token, not `GET` (GET is forgeable via `<img src>`); redirect targets are hard-coded literals, no `returnUrl` parameter, eliminating the open-redirect surface entirely rather than validating it.

**Tasks (#73)**: 4 phases/work units mirroring the apply-progress structure below — Unit 1 (API contract, `OrganizationName` field+claim) **was completed and committed** before the abort; Unit 2 (ticket store, `AuthApiClient`, endpoints, cookie pipeline) **was also completed and committed** before the abort; Units 3 (Login page) and 4 (shell+dashboard) were never started. Both completed units were reverted per the Revert Action above.

</details>

---

## Project Setup: sdd-init

**Original scan** (`sdd-init/checkbus`, #1, 2026-09-21): .NET 10, .NET Aspire (`Checkbus.AppHost`), ASP.NET Core Web API (`Checkbus.ApiService`), Blazor Server (`Checkbus.Web`, `InteractiveServer` render mode), shared `ServiceDefaults` (OpenTelemetry, resilience, service discovery), solution file `Checkbus.slnx` declaring 8 member projects. Clean/layered architecture: `Domain` (entities/enums/interfaces, no external deps) ← `Application` (MediatR use cases) ← `Infrastructure` (EF Core/persistence) ← `Checkbus.ApiService` (HTTP host) / `Checkbus.Web` (separate BFF, Blazor Server). Multi-tenant SaaS shape, Argentina locale (CUIT tax ID, DNI/Pasaporte document types). **At the time of this scan, no git repository existed at all.**

**Corrections to the original scan** (#36, verified 2026-09-22, superseding stale facts from #1):
1. **A git repository now exists** (it didn't at #1's scan time) — `git init` was run (branch `main`, no remote at the time). This was found to be a **hard requirement**, not optional: `gentle-ai sdd-attempt acquire/settle` (the native SDD runtime attempt ledger used by every `sdd-apply`/`sdd-verify` call) stores its authority in the Git common directory and fails without one. Distinct from the already-known OpenSpec-dispatcher false-block gap (see Standing Lessons) — this is a real tool requirement, not bypassable. A `.gitignore` was added before the first commit.
2. `gentle-ai sdd-attempt acquire` also requires declaring the untracked-file scope the first time it runs against a repo with untracked files (`--untracked-scope=exclude --expected-untracked-inventory=sha256:<hash>`, or `=select`) — the tool conveniently prints the exact expected hash in its own error message on first failure.
3. **No `Profile` entity exists** in the domain model — the original #1 scan's claim that `User.cs` has a `Profile` collection and that `Authorization/Profile.cs` exists is wrong. `User.cs` has a single `Role` navigation property only (pre-dates the later Role Modeling changes, which removed the `Role` entity too — see api-jwt-authentication section above).

**Testing capabilities** (`sdd/checkbus/testing-capabilities`, #2): **Strict TDD Mode: enabled** (default resolution — 8-project workspace, one `dotnet test` command from repo root covers all of them via `Checkbus.Tests`' project references). Workspace-level test command: `dotnet test` (run from `C:\Users\l_ucasmatias\Desktop\Checkbus`, resolves `Checkbus.slnx`). Test project: `Checkbus.Tests`, xUnit v3 + `Aspire.Hosting.Testing`. No linter/`.editorconfig` configured; `dotnet build` with `Nullable enable` serves as the type checker; `dotnet format` available but unconfigured. Coverage via `coverlet.collector` (`dotnet test --collect:"XPlat Code Coverage"`).

**Skill registry** (`skill-registry`, #3): mirrors `.atl/skill-registry.md`. No project-level skill directories; user-level skills at `~/.claude/skills/` indexed. No skill in the registry is .NET/C#/xUnit/Aspire-specific (as of the last refresh this backup's session observed, the registry lists: `chained-pr`, `cognitive-doc-design`, `go-testing`, `judgment-day`, `skill-creator`, `skill-improver`, `work-unit-commits` — none language-specific to this stack).

---

## Other Completed SDD Changes (archived, for historical context — not pending work)

These are **fully closed and delivered**. Listed briefly for context; full archive-report content is preserved in Engram observation IDs below if ever needed again.

| Change | Archive Report ID | Verdict | What it delivered |
|---|---|---|---|
| `postgres-aspire-integration` | #27 | PASS WITH WARNINGS | Aspire-managed PostgreSQL 18.3 + pgAdmin 9.15.0 for `Checkbus.AppHost`, `AddNpgsqlDbContext` wiring, Development-gated `EnsureCreatedAsync`, explicit shared-password parameter. Also fixed a pre-existing unrelated build blocker (`Weather.razor` referencing undefined types — deleted as template scaffold cruft). |
| `login-endpoint-jwt-rbac` | #39 | PASS WITH WARNINGS | The original `POST api/auth/login` endpoint: MediatR CQRS command, FluentValidation, global exception handling (400/401), JWT issuance with single-role RBAC claims, generic `ValidationBehavior<,>` pipeline reused by all later use cases. 834 changed lines (exceeded its own estimate, user explicitly accepted). One known limitation carried forward: the full HTTP-level "successful login → 200" scenario only has unit-level proof, not an end-to-end test, due to the Aspire/Docker environmental timeout present in this sandbox since day one. |
| `file-storage-service` | #50 | PASS WITH WARNINGS | `IFileStorageService` abstraction (Application layer) + `LocalFileStorageService` (Infrastructure), designed so an S3 implementation could be added later without changing consumers. 368 changed lines. `OpenReadAsync` returns `Task<Stream?>` (null on missing key) per an explicit user decision that deliberately superseded the spec's original "throws" wording — documented as intentional, not a defect. |
| `handler-event-logging` | #59 | PASS (clean) | Structured business-event logging in `LoginCommandHandler` via `ILogger<T>` — success + 3 failure paths, `UserId`/`OrganizationId`/`Role` fields only, never email/password/token. ~110-130 changed lines. Confirmed HTTP-layer anti-enumeration behavior (`AuthExceptionHandler`) unchanged. |
| `landing-home-sections` | #68 | PASS WITH WARNINGS | The 7-section MudBlazor marketing landing page (`Home.razor`) plus a `CheckbusTheme` (MD3→MudBlazor role mapping, 56 test cases) applied to both `LandingLayout` and `MainLayout`. Two commits: `1654670` (theme) and `5b85895` (landing sections) — note `5b85895` is also the commit the `login-app-shell` abort reset back to, i.e. it's the last-known-good commit before that aborted change's work. Known follow-up: GitHub/PostgreSQL brand SVGs are self-identified placeholders, need replacing with official assets before public release. |

Two earlier session summaries also exist in Engram (#28, #40, #51) covering the `postgres-aspire-integration`/login-endpoint teaching session, an email/password validation session, and the `file-storage-service` session — not reproduced in full here as their substance is already captured in the archive reports above.

---

## Standing Lessons (local auto-memory — survives an Engram reinstall on its own, included here for one unified reference)

These are stored as separate files under the user's Claude memory directory, not in Engram, so they are **not** affected by the reinstall. Reproduced here anyway per the user's request for one complete document.

- **checkbus-sdd-interactive-pace**: On Checkbus, run the SDD flow in Interactive pace so the user authorizes each phase and design decision, not Automatic.
- **sdd-design-summary-enumerate-new-files**: When summarizing an SDD design phase for this user, explicitly list every new file/class it introduces, not just the architecture narrative.
- **local-postgres-connection-setup**: How to connect to the user's local Postgres for Checkbus — the repo's placeholder password is wrong; use user-secrets and an env var instead.
- **git-revert-ambiguity-pushed-commits**: When the user says "revert" for commits already pushed to a shared branch, don't default to `git revert` without checking whether they actually want history erased via reset + force-push.
- **checkbus-avoid-overengineering-simple-ui-asks**: On Checkbus, watch for the SDD flow ballooning a simple UI/CSS request into far more code than it needs, especially defensive engineering against unconfirmed upstream bugs.
- **checkbus-sdd-dispatcher-openspec-gap**: On Checkbus, the native `gentle-ai` SDD dispatcher CLI falsely reports every change as blocked because it only knows OpenSpec, not Engram; the user has approved proceeding via Engram anyway.
- **checkbus-direct-to-main-no-prs**: On Checkbus, the user wants finished work fast-forward-merged straight into main and pushed, not delivered as per-slice PR branches.
- **checkbus-always-use-sdd-flow**: On the Checkbus project, every future code change must go through the full SDD flow (explore/propose/spec/design/tasks/apply/verify) — the user explicitly asked for this as a standing rule, not a one-off.
- **engram-mem-save-same-topic-key-revises**: Engram's `mem_save` can silently revise/overwrite an existing observation instead of creating a new one when given a `topic_key` that already has content.
- **subagent-consent-for-decisions**: Background subagents must never make or persist important project decisions without the user's explicit consent given in the main conversation.

---

## Other Uncommitted Work In The Tree (unrelated to api-jwt-authentication, unexplained by Engram alone)

As of this backup's session, `git status` showed changes **not** part of `api-jwt-authentication` and not yet committed:

- New: `Checkbus.Web/Components/Pages/Login.razor`, `Checkbus.Web/Components/Unauthorized.razor`
- New directory: `Checkbus.ApiService.Application/Tenancy/` (contains at least `Commands/RegisterOrganizationCommandResult.cs`, an empty placeholder — referenced in the `register-organization-admin` explore above)
- New directory: `notes/` (purpose unclear from Engram — do not confuse with this backup file's `odd/` location)
- New directory: `.codegraph/` (CodeGraph index, not SDD-related)
- Modified: `Home.razor`, `Routes.razor`, `_Imports.razor`, `LandingLayout.razor`, `MainLayout.razor`
- Deleted: `NavMenu.razor`, `NavMenu.razor.css`, `NavMenu.razor.js`
- Modified: `Checkbus.ApiService.Infrastructure/Implementations/Repositories/UserRepository.cs`

This looks like in-progress work toward `register-account` and/or `register-organization-admin` (both only at the `explore` phase per this backup, no proposal yet), but its exact state and intent are **not fully reconstructable from Engram alone** — flagging its existence rather than guessing. A future session should ask the user directly what state this is in before touching it.
