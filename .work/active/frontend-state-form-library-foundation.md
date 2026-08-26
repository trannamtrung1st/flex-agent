---
id: frontend-state-form-library-foundation
status: in-progress
created: 2026-08-26
updated: 2026-08-26
---

# Goal

Establish an approved, explicit frontend architecture for HTTP-backed server
state, forms, runtime validation, icons, HTTP transport, client-only state, and
realtime Session state, then prove that direction through one bounded
production-backed migration.

The work updates authoritative documentation first, adds the smallest shared
infrastructure, and migrates the production Activities surface: TanStack Query
owns Activity/source reads and Campaign-create mutation lifecycle; React Hook
Form plus Zod own the non-trivial Campaign-create form; Lucide supplies one
representative general-purpose icon. Existing typed/domain API clients and
native `fetch` remain the transport path, the semantic token/component CSS
system remains the styling path, and realtime `SessionPage` behavior remains
unchanged.

# Approval and decision authority

- On 2026-08-26, the repository owner approved this implementation-workflow
  item and delegated the detailed technical decision to the implementing
  architecture/frontend role.
- The selected direction is therefore approved for documentation as the next
  available ADR (ADR-019 at planning time), subject to repository-consistency
  review while authoring it. Phase 1 must recheck the ADR index before creating
  the file and update all task references if concurrent work has claimed that
  number. A discovered
  conflict with approved product, requirements, UI/UX, or earlier architecture
  authority must stop runtime work and be recorded here; implementation must
  not silently reinterpret the governing source.
- This task file is execution state, not architecture authority. Phase 1 must
  publish the durable decision under `docs/` before dependencies or runtime
  code change.

# Governing sources

- `AGENTS.md` — authority by concern, product/session/isolation invariants,
  specification-driven TDD, UI verification, security/privacy, and tracked
  implementation workflow
- `.agents/skills/implementation-workflow/SKILL.md`, `.work/README.md`, and
  `.cursor/rules/06-implementation-workflow.mdc` — single task state,
  incremental execution, evidence, reconciliation, and retention
- `.agents/skills/architect/SKILL.md`, `frontend-developer`,
  `business-analyst`, `ui-ux-designer`, and `documentation-author` — technical
  boundaries, testable scope, accessible interaction, and authoritative docs
- `docs/README.md#authority-by-concern`
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md`
- `docs/requirements/features/assessment-setup.md` — especially
  `REQ-ACT-1`-`REQ-ACT-13`, `REQ-ACT-35`-`REQ-ACT-42`,
  `AC-ACT-1`-`AC-ACT-7`, `AC-ACT-18`, `AC-ACT-22`-`AC-ACT-24`, and
  `AC-ACT-27`
- `docs/requirements/features/auth-resource-isolation.md` — current trusted
  scope, server authorization, protected reads, denial, and access-change
  behavior
- `docs/ui-ux/activity-campaign-journey.md`
- `docs/ui-ux/assessment-campaign-setup.md` — especially `UI-ACT-DEC-1`-
  `UI-ACT-DEC-6`, explicit save/validation, recoverable input, protected
  content, focus, and responsive behavior
- `docs/ui-ux/design-system/README.md` and
  `docs/ui-ux/design-system/implementation-guide.md`
- Applicable design-system modules: accessibility, colors, typography, layout,
  density, interaction states, status, inputs, selection controls, buttons,
  alerts, error summary, icon shapes, cards/panels, and empty/loading
- `docs/architecture/mvp-architecture.md` — especially browser/API authority,
  `AR-DEC-4`, `AR-DEC-12`, `AR-DEC-14`, and the Browser SPA responsibility
  boundary
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`
- `docs/architecture/decisions/ADR-006-mvp-architecture-baseline-and-evolution.md`
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
  — React/Vite, locked dependency, browser, supply-chain, and SPA/API gates
- `docs/architecture/session-runtime-contract.md` and
  `docs/ui-ux/text-session.md` — authoritative projection, SSE, command,
  reconciliation, stale-state, and isolation contracts that this task must not
  weaken or migrate
- `.work/active/p0-activity-journey-frontend-realization.md` and
  `.work/active/p0-assessment-setup-cohort-activation.md` — completed frontend
  and production Assessment predecessors
- `.work/active/structured-agent-runtime-sync.md` — completed and reviewed
  realtime Session behavior; do not reopen this slice

# Current repository baseline

- `web/package.json` has only React, React DOM, and React Router as runtime
  dependencies. TanStack Query, React Hook Form, Zod, Hook Form resolvers,
  Lucide, Axios, Tailwind, and Zustand are absent.
- `App.tsx` selects production or synthetic API providers and routers. There is
  no Query client/provider.
- `BrowserApiProvider` owns synthetic actor/navigation bootstrap, command
  execution, reconciliation, `fetchJson`, and coarse load/error state.
- `ProductionApiProvider` owns application-session bootstrap, CSRF, native
  `fetch`, `ProductionApiError`, generation-based stale-response rejection,
  logout, and protected-state clearing.
- `AssessmentActivitiesPage` manually loads Activity and source-option data,
  copies remote data into local state, and manually owns a Campaign-create form
  with title plus ten required exact-source selectors.
- `AssessmentSetupPage` has a complex workflow but only one editable title
  field in its current form. It is not the best first non-trivial RHF proof.
- `ProductionMyWorkPage` is a safe later Query candidate but does not prove a
  mutation or a non-trivial form in one coherent slice.
- `SessionPage` is 879 lines and combines authoritative projections,
  EventSource lifecycle, streamed/transient runtime state, command identity,
  reconciliation, stale-response protection, reconnect/error behavior, and
  Session isolation. `sessionRuntimeView` and their focused suites are mature
  reviewed behavior and are excluded from this migration.
- The design system already governs semantic tokens, CSS components,
  accessibility, inputs, error summaries, and icon sizing. It does not yet
  select a general-purpose icon library.
- All earlier frontend/Assessment task files are completed retained history.
  The only other non-completed pre-existing task observed during planning is
  the planned documentation-only text-interaction-controller contract, whose
  scope explicitly excludes frontend behavior.
- The worktree contains unrelated backend edits and a separate backend task.
  They belong to the user/other work and must not be modified, staged, or
  included by this task.

# Scope

## In

- Publish and index the approved frontend-state ADR (provisionally ADR-019) and
  a concise frontend architecture guide.
- Update the existing design-system icon-shape contract with Lucide usage and
  accessibility conventions without changing design-system visual meaning.
- Add locked compatible versions of `@tanstack/react-query`,
  `react-hook-form`, `zod`, `@hookform/resolvers`, and `lucide-react`.
- Add one Query provider/client factory above both production and synthetic
  API-mode branches, with isolated test clients and no persisted cache.
- Tie Query-cache lifecycle to application/synthetic authentication and
  protected-state lifecycle so data cannot cross actors, Organizations, or
  sessions.
- Add feature-owned Assessment query keys and hooks that compose existing
  typed/domain API clients rather than calling `fetch` directly.
- Migrate `AssessmentActivitiesPage` Activity/source reads and create mutation
  to TanStack Query with no duplicate page-local copy of remote data.
- Migrate the Campaign-create title and all required exact-source selectors to
  React Hook Form with a Zod resolver.
- Demonstrate the Lucide convention in `ThemeToggle` while retaining visible
  text and accessible naming.
- Preserve current loading, empty, populated, pending, validation, recoverable
  failure, permission-loss, and navigation behavior.
- Add focused tests first, then run full web, documentation, build,
  supply-chain, accessibility-tree, and live screenshot verification.
- Reconcile documentation, code, tests, lockfile, and this task file before
  closeout.

## Out

- Migrating `AssessmentSetupPage`, Enrollment, My work, Submission, Review,
  Result/Release, or every effect-based page in this work item.
- Rewriting `SessionPage`, `sessionRuntimeView`, EventSource/SSE lifecycle,
  streamed message state, projection commit rules, command identity,
  reconciliation, reconnect behavior, or Session isolation.
- Moving shell authentication, actor/navigation bootstrap, CSRF, logout, safe
  error mapping, expected revision, idempotency, reconciliation, or domain
  outcomes into TanStack Query.
- Query cache persistence, offline-first behavior, SSR, server actions, Query
  Devtools, normalized entity stores, or optimistic updates for audited or
  authority-sensitive mutations.
- Zustand or another global client store. A future proposal may use scoped
  ephemeral state only after concrete evidence; it may never become an
  application-global server-entity store.
- Tailwind, CSS-in-JS, styling redesign, token renaming, or broad component
  restyling.
- Axios or any replacement of native `fetch` and the current typed/domain API
  abstraction.
- A shared icon wrapper, dynamic icon registry, or broad icon retrofit without
  repeated behavior demonstrating a need.
- New product behavior, business validation, permissions, or server contracts.
- Committing, pushing, or opening a pull request unless separately requested.

# Technical decisions

## State ownership

| State kind | Owner in this task | Boundary |
| --- | --- | --- |
| HTTP-backed page resources | TanStack Query | Fetch/cache/load/error/refetch/invalidation only; server remains authoritative |
| Form values and client validation | React Hook Form; Zod only where runtime shape has value | Client UX only; server authorization and business validation remain authoritative |
| Simple ephemeral UI | Component-local React state | Dialog visibility, non-server presentation state, and similar local concerns |
| Locally complex ephemeral UI | `useReducer` by default | Prefer a focused reducer/hook before adding a state library |
| Shell/authentication/CSRF | Existing API providers | Never infer identity or authorization from Query keys or cached data |
| Domain commands and reconciliation | Existing typed/domain API layer | Preserve expected revisions, idempotency keys, 403/409 outcomes, safe errors, and uncertain-result behavior |
| Realtime Session state | Existing focused code and reducers/helpers | Authoritative projection plus SSE/transient state; intentionally unmigrated |

## Query composition and lifecycle

- `QueryClientProvider` belongs in `web/src/App.tsx`, above both API-provider
  and router branches. `main.tsx` remains the DOM/StrictMode composition root.
- Use a `createFlexQueryClient()` factory. Each mounted App/test tree gets one
  client created once through a lazy state initializer or an equivalent stable
  provider component; do not construct it during every render and do not use a
  process-global singleton that leaks between tests or authentication contexts.
- Initial defaults preserve current observable behavior: no automatic retry,
  no automatic refetch on window focus, no cache persistence, and no global
  optimistic mutation behavior. Feature hooks may later opt into bounded
  transient retries only after safe error classification and tests.
- A 401, 403, logout, unauthenticated/failed bootstrap, synthetic actor
  replacement, production shell actor/Organization replacement, or equivalent
  protected-state reset cancels in-flight protected queries and clears cached
  protected queries before a later actor can render. Every transition out of
  `ready` must use the same purge path rather than only changing `apiState`.
  Generation-based stale-response protection remains in
  `ProductionApiProvider` and must prevent an older response from repopulating
  UI state after the reset. A successful rebootstrap compares the trusted
  current and incoming shell actor/Organization and purges before replacement
  when either identity changes.
- The initial application treats every Query/mutation cache entry as protected.
  The purge cancels in-flight work and clears the complete QueryClient,
  including mutation variables/results, rather than trying to maintain a
  fragile prefix allowlist. Introducing cacheable public data later requires an
  explicit reviewed separation; it does not weaken this default.
- Query cancellation passes the provided `AbortSignal` through the existing
  typed/domain client to `fetchJson`; Query code does not bypass the API layer.
- Query errors remain `ProductionApiError` or the existing safe/domain error
  type. Query does not wrap them into a new generic model.
- Feature UI classifies access loss through typed status/outcome helpers rather
  than new regular-expression checks against presentation copy. If the current
  Assessment helper is private, expose or relocate the narrow helper without
  changing the API error contract.
- Query data is the single client copy of each migrated remote resource. Do not
  mirror successful query data into `useState`.

## Query key and hook placement

- Add feature-oriented files under `web/src/features/assessment/`, initially
  `queryKeys.ts` and `queries.ts`. The existing `web/src/api/` directory stays
  React-free transport/domain client code.
- Use readonly key factories, initially:
  - `assessmentKeys.all` -> `["assessment"]`
  - `assessmentKeys.v1` -> `["assessment", "v1"]`
  - `assessmentKeys.activities()` ->
    `["assessment", "v1", "activities", "list"]`
  - `assessmentKeys.sourceOptions()` ->
    `["assessment", "v1", "activities", "source-options"]`
  - reserve `assessmentKeys.activity(activityId)` for later detail adoption;
    do not create unused hooks merely to populate a hierarchy.
- Keys contain stable resource identities only. They never contain titles,
  source content, credentials, CSRF tokens, actor claims, or authorization
  evidence. Cache clearing, not user-supplied Organization identifiers, defines
  the authentication-context boundary.
- Include a contract/projection version whenever parallel wire meanings can
  coexist. Do not use the Query key to reinterpret or upgrade a response.

## Activities query coordination

- The Activities list is the prerequisite query because its server-returned
  `permitted_actions` decides whether the create surface is available.
- Enable the source-option query only after the Activity list succeeds and
  includes `create_assessment`. This is an intentional, authorization-informed
  dependency, not an avoidable request waterfall; actors without the action do
  not request unused source options.
- While creation is permitted, preserve the current stable initial layout by
  keeping the protected loading state until the first source-option request has
  settled. A source-option failure degrades only the create section to the
  existing safe missing-source/unavailable state; it does not discard an
  authorized Activity list. An access-loss error still follows the provider
  purge/gate path and must not render cached protected content.

## Mutation and invalidation

- `createProductionAssessmentClient` remains responsible for request shape,
  endpoint, response interpretation, and safe/domain outcomes.
- Campaign creation uses `useMutation`. It is not optimistic.
- On authoritative success, schedule an exact invalidation of
  `assessmentKeys.activities()` and immediately execute the existing
  `onCreated(activityId)` navigation. Do not await an active-list refetch before
  navigation and do not invent a local Activity summary from submitted fields.
  The invalidated list refetches when it is next observed.
- A client-invalid form sends no request. A recoverable server failure retains
  safe form values. Access loss relies on the provider to clear protected state
  and Query cache before protected controls disappear.
- Future expected-revision, idempotent, 403-domain, 409-conflict, and uncertain
  mutations follow the same rule: Query coordinates lifecycle; the typed/domain
  API layer defines meaning and reconciliation.

## Forms and validation

- `AssessmentActivitiesPage` is the first RHF form because the current form has
  a title and all ten `REQUIRED_SOURCE_CATEGORIES`; this is a meaningful runtime
  object. `AssessmentSetupPage` currently has only one editable title and stays
  unchanged.
- Define one Zod schema for `{ title, sources }`. Title remains required with
  the existing 200-character maximum. Each required category must have a
  non-empty exact option identity.
- Do not trim, normalize, coerce, or otherwise change submitted values unless
  the current approved specification and server contract already require it.
- Zod never validates authorization, source eligibility, Organization scope,
  revision freshness, readiness, activation eligibility, memory policy, or
  security rules. Those remain server-authoritative.
- Use `zodResolver`. RHF owns field values, dirty/touched state, client errors,
  and submit coordination; TanStack mutation owns request pending/error/success.
- Source options arrive asynchronously. Initialize their first permitted
  selections once after the first successful source-option result and only
  while the form is pristine. A refetch or cache update must never silently
  reset a title or source selection the administrator has touched.
- Immediately before mutation, resolve every selected identity against the
  latest source-option query data and require complete category coverage. If an
  option disappeared or changed, send no request, preserve the entered values,
  and show a safe actionable stale-options error. This client guard prevents a
  malformed request but does not establish source eligibility or authority.
- Keep field errors programmatically associated. Extend `ErrorSummary`
  backward-compatibly, or use an equivalent page-local structure, so each
  actionable entry links to the exact field. After submitted validation or a
  correctable server failure, focus the summary heading once; activating an
  entry focuses its field. Preserve safe input on recoverable failure and do
  not announce duplicate error messages.

## Icons and styling

- `lucide-react` is the standard general-purpose icon set. Use direct named
  imports so unused icons tree-shake.
- Demonstrate the convention with `Sun`/`Moon` in `ThemeToggle`. Preserve its
  visible text and accessible label; the icon is decorative and
  `aria-hidden="true"`.
- Use the design-system icon sizes and semantic foreground roles. Do not add
  raw colors or place every icon in a colored container.
- Do not create a wrapper in this slice. Add one later only if repeated sizing,
  semantic, or accessibility behavior cannot remain consistent through direct
  use.
- Retain `tokens.css`, `components.css`, and `app.css`; add only the smallest
  semantic alignment rule if the existing button gap/layout is insufficient.

## Client-only and realtime state

- Do not add Zustand. Simple UI stays local; locally complex UI should first be
  extracted into focused hooks/reducers.
- Do not put server-backed Activities, Sessions, Enrollments, Submissions, or
  other entities in an application-wide store.
- `SessionPage` and `sessionRuntimeView` are a deliberate architecture
  exception to ordinary CRUD migration sequencing, not technical debt to fold
  into this slice. A future contained refactor may separate projection,
  connection, command, and presentation hooks/reducers while preserving the
  reviewed authority and race protections; it requires its own task.

# Acceptance criteria

- `WI-FE-01` — Approved architecture documentation is published and indexed
  before dependency or runtime changes begin.
- `WI-FE-02` — TanStack Query infrastructure is mounted once for both API modes
  and each application/test tree has an isolated client.
- `WI-FE-03` — Authentication/access-loss boundaries clear protected cached
  data on every transition out of ready and on trusted actor/Organization
  replacement; stale responses cannot repopulate data for a later actor or
  scope.
- `WI-FE-04` — Activity and source-option reads are Query-owned with no
  duplicate page-local remote-data authority.
- `WI-FE-05` — Campaign creation uses a non-optimistic Query mutation and
  invalidates only the documented Activities-list key after authoritative
  success.
- `WI-FE-06` — Native `fetch`, CSRF, credentials, generation guards,
  `ProductionApiError`, typed contracts, domain outcomes, and existing API
  clients retain their responsibilities.
- `WI-FE-07` — The Campaign-create form uses RHF plus a Zod resolver for title
  and all required exact-source selections, sends no invalid client payload,
  does not reset touched values when options refetch, rejects stale option
  identities before mutation, and preserves safe input on recoverable server
  failure.
- `WI-FE-08` — Server validation remains authoritative for authorization,
  scope, revision/source validity, transitions, eligibility, permissions,
  memory constraints, and security rules.
- `WI-FE-09` — Lucide conventions are documented and demonstrated without an
  unnecessary wrapper or inaccessible icon-only meaning.
- `WI-FE-10` — The existing semantic CSS token/component architecture is
  retained; Tailwind is not added.
- `WI-FE-11` — Axios is not added and HTTP transport remains native `fetch`
  through existing abstractions.
- `WI-FE-12` — Zustand is not added and no application-global store holds
  server-backed entities.
- `WI-FE-13` — `SessionPage`, `sessionRuntimeView`, SSE/EventSource lifecycle,
  pending command identity, reconciliation, projection ordering, reconnect
  behavior, and Session isolation remain functionally unchanged.
- `WI-FE-14` — Query keys/caches are not authorization evidence and
  Organization, Activity, Participant, and Session isolation remain intact.
- `WI-FE-15` — Focused API/provider/Activities/form/shell tests pass, including
  cache clearing, stale response, typed access-loss classification,
  cancellation, validation, input preservation, duplicate-submit prevention,
  versioned keys, and exact invalidation.
- `WI-FE-16` — Accessibility-tree and live screenshot evidence covers the
  changed Activities/form states at desktop and narrow viewports in both
  themes, or the exact runnable-environment blocker is recorded without a
  visual-completion claim.
- `WI-FE-17` — Lint passes.
- `WI-FE-18` — Typecheck passes.
- `WI-FE-19` — Full web tests, production build, documentation validation, and
  supply-chain checks pass.
- `WI-FE-20` — Final documentation names all intentionally unmigrated pages and
  the realtime Session boundary; plan and actual changes are reconciled.

# Plan

- [x] **Phase 1 — publish the architecture before runtime changes.** Recheck the
  ADR index, use ADR-019 if it remains available (otherwise the next available
  number and update this task), then create and approve the frontend-state and
  library-boundaries ADR; create a concise
  `frontend-architecture.md`; update architecture/ADR indexes and the existing
  design-system icon-shapes module; document the state-ownership, layering,
  provider/cache lifecycle, query-key, mutation, error, form/validation,
  styling, Zustand, and realtime exclusions above. Give the ADR stable
  frontend-decision IDs and map them to `WI-FE-*` verification. This
  documentation-only phase is the recorded TDD exception; validate with
  `python3 scripts/check_docs.py`, link/status review, and consistency review
  against ADR-006/ADR-010/MVP architecture and the approved design system.
  **Completion:** the durable docs are approved, internally consistent, and
  describe the exact intended code boundary. **Dependency:** every later phase
  waits for this phase.
- [x] **Phase 2 red — specify Query infrastructure and isolation behavior.** Add
  the smallest failing tests for one Query provider across both API modes,
  stable one-time client construction, per-render test-client isolation,
  protected-query cancellation/cache clearing on production 401/403/logout,
  failed/unauthenticated bootstrap, trusted shell actor/Organization change,
  synthetic actor/access reset, and stale response non-repopulation. Run them
  and record the intended failures. **Invariants:**
  existing auth, CSRF, safe-error, and generation semantics remain in the API
  providers; tests use synthetic data only. **Completion:** failures prove the
  missing infrastructure rather than an unrelated harness error.
- [x] **Phase 2 green/refactor — add locked dependencies and Query provider.**
  Add TanStack Query, RHF, Zod, resolver, and Lucide with reviewed exact pins
  and lockfile changes; implement `createFlexQueryClient`, mount the provider in
  `App.tsx`, add isolated test helpers, and connect cache clearing to protected
  state changes. Migrate all direct `ProductionApiProvider`/
  `BrowserApiProvider` test consumers to the isolated Query test wrapper rather
  than adding an optional production fallback; nine test files use those
  providers at planning time, so re-run `rg` and update the complete set. Keep
  retry, focus refetch, persistence, and optimistic defaults disabled initially.
  Run focused provider/App tests and keep the task evidence current.
  **Completion:** Phase 2 red tests pass and existing provider/App tests remain
  green.
- [x] **Phase 3 red — specify the representative Query slice.** Add failing
  Activities tests for initial loading, Activity success/empty, independent
  source-option success/failure, no source-option request without the
  server-returned create action, intentional list-then-options coordination,
  access loss, cancellation/unmount, create pending/duplicate prevention,
  recoverable create failure, authoritative success/immediate navigation, and
  background exact list-key invalidation. **Invariants:** no
  optimistic Activity, no generic error replacement, and no client
  authorization inference. **Completion:** the tests fail because the page
  still owns manual server lifecycle.
- [x] **Phase 3 green/refactor — migrate Activities server state.** Add
  feature-owned Assessment key factories/query hooks, pass Query cancellation
  through the typed client, replace effect/local remote-data copies with
  queries, gate source options on the server-returned create action, and
  implement Campaign creation as a mutation. Retain independent source-option
  degradation, safe error copy, permitted actions, and navigation without
  waiting for an active-list refetch.
  Run focused production-assessment and Activities suites. **Completion:** Query
  is the only client owner of migrated remote data; successful create
  invalidates the list and all Phase 3 tests pass.
- [x] **Phase 4 red — specify RHF/Zod form behavior.** Add failing accessible
  tests for missing/overlong title, every missing required source category,
  linked error-summary entries and one-time summary focus, no request on
  client-invalid input, exact valid payload, asynchronous first-option
  initialization, no touched-value reset after option refetch, stale/removed
  option rejection before mutation, pending duplicate prevention, recoverable
  server error with all input retained, and access-loss protected-content
  removal.
  **Invariants:** test through roles/names; do not encode server-only business
  validation. **Completion:** failures identify the current manual form-state
  boundary.
- [x] **Phase 4 green/refactor — migrate the Campaign-create form.** Introduce
  the Zod runtime schema and resolver, register title/source fields with RHF,
  remove their parallel local state, map client/server errors into the approved
  accessible summary/field behavior, and coordinate submit state with the
  Query mutation. Run focused Activities tests after each red/green/refactor
  step. **Completion:** RHF owns form state, Zod adds bounded UX validation,
  the server remains authoritative, and Phase 4 tests pass.
- [x] **Phase 5 red/green — establish Lucide convention.** Add/adjust the shell
  test first to protect accessible theme-toggle naming and prevent duplicate
  icon announcements; then add direct `Sun`/`Moon` imports with decorative
  semantics and only minimal existing-CSS alignment if needed. Do not create an
  icon wrapper. **Completion:** both theme states retain visible text,
  accessible names, focus behavior, and design-system sizing.
- [x] **Phase 6 — focused and aggregate automated verification.** Run focused
  tests throughout, then `pnpm lint`, `pnpm typecheck`, `pnpm test`,
  `pnpm build`, `python3 scripts/check_docs.py`, and
  `bash build/scripts/verify-supply-chain.sh`. Inspect dependency licenses,
  lockfile/SBOM/audit results, `git diff --check`, and relevant Session-focused
  tests to prove no realtime regression. Record commands and exact outcomes in
  `# Verification`. **Completion:** all applicable automated gates pass or an
  exact blocker/gap is recorded without a completion claim.
- [x] **Phase 7 — live browser and visual verification.** Use the repository
  Playwright MCP against the authenticated synthetic profile when runnable.
  Reach changed Activities states through real interactions; inspect the
  accessibility tree and screenshots for loading/empty/populated, valid and
  invalid create, pending/disabled, recoverable error, permission loss, focus,
  desktop, narrow, light/dark, reduced motion, and 400-percent reflow as
  applicable. Store evidence only in `.playwright-mcp/`, with synthetic data
  and default screenshot names. Fix findings and repeat. **Completion:** live
  evidence supports the changed UI claim, or the exact environment blocker and
  manual checks are recorded.
- [x] **Phase 8 — reconcile, review, and close.** Compare actual files and
  behavior with `WI-FE-01`-`WI-FE-20`; recheck governing specifications;
  update exact dependency/contributor documentation if required; record all
  intentionally unmigrated flows and residual risks; perform distinct
  architecture, frontend, security/privacy, and QA review; address findings;
  rerun affected verification; set this file to `completed` only with evidence
  and retain it for external review. **Completion:** documentation, code,
  tests, lockfile, screenshots, and task state are consistent and reviewable.

# Phase dependencies

```text
Phase 1 approved documentation
  -> Phase 2 Query/dependency foundation
    -> Phase 3 Activities server-state migration
      -> Phase 4 RHF/Zod form migration
        -> Phase 5 Lucide convention
          -> Phase 6 aggregate automation
            -> Phase 7 live browser evidence
              -> Phase 8 reconciliation and review
```

Phases 3 and 4 remain separate red/green cycles even though they affect the
same surface. This prevents simultaneous server-state and form-state rewrites
from obscuring regressions. Phase 5 may be implemented after Phase 2 but stays
ordered after the representative slice to keep one current step and a simple
review history.

# Files / areas likely affected

- `.work/active/frontend-state-form-library-foundation.md`
- `docs/architecture/decisions/ADR-019-frontend-state-and-library-boundaries.md`
  at planning time; use the rechecked next available ADR number
- `docs/architecture/frontend-architecture.md`
- `docs/architecture/README.md`
- `docs/architecture/decisions/README.md`
- `docs/ui-ux/design-system/components/icon-shapes.md`
- `docs/contributing/workspace.md` if exact frontend dependency guidance belongs
  there after installation
- `web/package.json`, `pnpm-lock.yaml`
- `web/src/App.tsx`
- `web/src/api/query-client.ts` or one equivalently named provider module
- `web/src/api/browser-api.tsx`, `web/src/api/production-api.tsx`
- `web/src/api/production-assessment.ts`
- `web/src/features/assessment/queryKeys.ts`
- `web/src/features/assessment/queries.ts`
- `web/src/pages/ProductionActivitiesPage.tsx`
- `web/src/pages/AssessmentActivitiesPage.tsx` and focused tests
- `web/src/components/ui/ErrorSummary.tsx` and a focused backward-compatibility
  test if structured linked errors are implemented in the shared component
- `web/src/components/shell/ThemeToggle.tsx` and shell tests
- A shared Query test-render helper only if repeated setup justifies it
- Existing semantic CSS only if icon alignment requires a minimal rule
- `.playwright-mcp/` inspected PNG evidence only, following repository policy
- `build/scripts/generate-spa-sbom.sh` — scoped CycloneDX `group`/`name` matching for SPA runtime packages

# Risks and controls

| Risk | Control | Required evidence |
| --- | --- | --- |
| Cached protected data crosses actor/access boundary | In-memory per-App client; clear on 401/403/logout/actor reset; no persistence | Provider/cache isolation and stale-response tests; permission-loss browser inspection |
| Query becomes a second domain/authorization layer | Hooks call typed clients; errors/outcomes pass through; keys are not authority | API/query boundary review and focused 403/409/error tests |
| Two sources of truth remain | Remove local copies of migrated query data and RHF field values | Source review plus state-transition tests |
| Automatic retry changes rate/command behavior | Disable global retries/focus refetch initially; explicit user retry only | Request-count and retry tests |
| Optimistic mutation fabricates audited state | No optimistic create; invalidate/read after authoritative success | Mutation test proves no pre-success list insertion |
| Cache clear races with an older response | Preserve provider generation guard and test post-reset late response | Stale-response regression |
| Zod duplicates or contradicts server rules | Validate only form shape/basic constraints; server remains authoritative | Client/server failure tests and architecture review |
| Async source options overwrite administrator input | Initialize once only while pristine; never reset touched fields on refetch | Delayed-query and refetch preservation tests |
| A selected source disappears between load and submit | Resolve against the latest query data; block malformed client request; server still revalidates | Stale-option no-request test plus server rejection coverage |
| RHF migration loses recoverable input or focus | Error-summary/field association, focus, pending, and preservation tests | Component tests plus accessibility snapshot/screenshots |
| Icon library weakens accessibility or bundle | Direct imports; decorative icon; visible text; no wrapper | Accessible-name test, build/SBOM review |
| Realtime Session behavior is accidentally generalized | Explicit exclusion; no Session code edits; focused regression check | Diff audit and Session tests |
| Dependency supply-chain regression | Exact locks, license/SBOM/audit checks, controlled install | Supply-chain command evidence |
| Unrelated dirty backend work is overwritten or included | Touch only listed frontend/docs/task surfaces; inspect diffs narrowly | `git status`, scoped diff, final changed-file reconciliation |

# Current state

Owner review of `c3a40bd` command-401 access-loss gap is fixed.

- [x] Propagate synthetic authentication loss from executeCommand/fetchJson/reconcile
- [x] Regression: command 401 unmounts Session stand-in and shows sign-in gate
- [x] Do not treat command 403/409 domain outcomes as workspace access loss

# Decisions

- Use one new frontend-state ADR rather than silently amending ADR-010.
  ADR-010 continues to govern the React/Vite stack and dependency/build policy;
  the new ADR (provisionally ADR-019) owns the narrower cross-cutting frontend
  state/library decision and links back to it.
- Create a concise frontend architecture guide because the repository has a
  backend implementation guide but no durable home for frontend layering,
  state ownership, query/form placement, and realtime exclusions. The ADR
  records why; the guide records how to apply it.
- Use the production Activities/Campaign-create surface as the one
  representative slice. It proves two ordinary queries, one mutation and
  invalidation, and a genuinely non-trivial form without entering Session
  realtime behavior.
- Introduce Zod/resolvers immediately for that form because the required-source
  record is a meaningful runtime shape. Keep Zod out of server authority and do
  not require it for trivial fields elsewhere.
- Keep Zustand entirely out of this task. No repository evidence currently
  justifies it.
- Use direct Lucide imports and no wrapper in the first slice.
- Identity replacement remounts `ProtectedAuthSubtree` inside the existing API
  provider rather than remounting the provider. That destroys RHF/refs while
  keeping CSRF, generation, and QueryClient ownership intact.
- Every successful trusted-context replacement starts a new authorization-context
  epoch. Same actor/Organization with a narrowed relationship or permission set
  still purges Query cache and remounts protected UI. Isolation is not limited
  to actor + Organization identity.
- `reloadTrustedContext` is test-only (`reloadTrustedContextForTests`) and is
  not part of the production `useProductionApi` surface.
- Dependent source-option reads require `isFetchedAfterMount` on the Activity
  list so cached `permitted_actions` cannot launch them.
- Activities create invalidation uses `exact: true` and `refetchType: "none"`
  so navigation does not race a still-mounted list refetch.
- Synthetic projection refresh is not trusted-context replacement. Ordinary
  `executeCommand` follow-up refresh must not increment the authorization
  epoch or remount `ProtectedBrowserAuthSubtree`. Bootstrap, actor/org
  identity change, capability/`actor_stage` narrowing, and explicit
  `replaceAuthorizationContext` still replace.
- Leaving synthetic API `ready` for idle/denied/error purges Query cache,
  clears actor/navigation, advances the epoch, and renders a workspace gate
  instead of protected routes.
- Synthetic HTTP 401 from command, reconcile, or `fetchJson` is authentication
  loss and uses the same ready-exit. Command HTTP 403/409 domain outcomes are
  returned to the caller.

# Findings / deviations

- The initial suggestion to consider `AssessmentSetupPage` as the first RHF
  migration was revised based on repository evidence: its current editable
  form contains only one title field, while `AssessmentActivitiesPage` owns the
  title plus ten exact-source selectors and is the stronger representative
  non-trivial form.
- The initial suggestion to consider an ordinary `BrowserApiProvider` read was
  narrowed to the production Activities client. `BrowserApiProvider` currently
  carries synthetic actor/navigation and command/reconciliation semantics;
  retaining it avoids mixing a proof migration with synthetic harness
  restructuring.
- No conflicting active frontend implementation task was found. Existing
  dirty backend files are unrelated and must remain untouched.
- `@hookform/resolvers` `5.9.1` listed optional peers that broke `pnpm
  licenses` / cyclonedx `npm ls`. Pinned `5.2.2` (react-hook-form peer only).
- SPA SBOM validation compared unscoped CycloneDX `name` values, so
  `@tanstack/react-query` and `@hookform/resolvers` looked missing.
  `build/scripts/generate-spa-sbom.sh` now matches `group`/`name`.
- Live Activities form and ThemeToggle could not be exercised: synthetic Vite
  proxy has no `/browser` API, and production `:18080/activities` is
  sign-in gated. Form/query/theme behavior is covered by component tests.
  This is an accepted non-blocking verification gap; it does not reopen the
  three review defects.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Repository/skill/governing-source inspection | passed for planning | Root guidance, implementation workflow, architecture/frontend/BA/UI roles, approved design system, ADR-006/010, Assessment requirements/UI, current API/pages/tests/styles, Session state, dependencies, and active work inspected on 2026-08-26 |
| Plan path and active-work conflict check | passed | `.work/active/frontend-state-form-library-foundation.md` was absent; no non-completed frontend implementation task found |
| Cross-cutting plan readiness review | passed with updates | Architecture, frontend, backend-contract, security/privacy, and QA review completed 2026-08-26; provider/cache replacement, query coordination/versioning, invalidation ordering, async form initialization, stale options, and error-summary evidence incorporated; no unresolved blocker |
| Plan artifact integrity | passed | `python3 scripts/check_docs.py` passed; all 20 `WI-FE-*` IDs are present; governing/current paths exist; `git diff --no-index --check /dev/null .work/active/frontend-state-form-library-foundation.md` emitted no whitespace diagnostics (exit 1 is the expected new-file difference) |
| Phase 1 documentation validation | passed | `python3 scripts/check_docs.py` exit 0 on 2026-08-26 after ADR-019, frontend architecture guide, index, icon-shape, and workspace pointer updates |
| Query provider/cache isolation focused tests | passed | `pnpm --filter @flex-agent/web test src/api/query-client.test.tsx src/api/production-api.test.tsx src/App.test.tsx` — 15 passed |
| Activities Query/mutation focused tests | passed | `pnpm --filter @flex-agent/web test src/pages/AssessmentActivitiesPage.test.tsx src/api/production-assessment.test.ts src/features/assessment/queryKeys.test.ts` |
| RHF/Zod focused tests | passed | Activities form tests plus `src/features/assessment/campaignCreateSchema.test.ts` and `src/components/ui/ErrorSummary.test.tsx` |
| Lucide/theme-toggle focused tests | passed | `pnpm --filter @flex-agent/web test src/components/shell/ThemeToggle.test.tsx` — 1 passed |
| Lint | passed | `pnpm --filter @flex-agent/web lint` exit 0 (existing react-refresh warnings remain; RHF `watch` incompatible-library warning on Activities) |
| Typecheck | passed | `pnpm --filter @flex-agent/web typecheck` |
| Full web tests | passed | `pnpm --filter @flex-agent/web test` — 24 files, 171 passed |
| Production web build | passed | `pnpm --filter @flex-agent/web build` |
| Supply-chain/license/SBOM | passed | `bash build/scripts/verify-supply-chain.sh` exit 0 after resolver pin and SPA SBOM group/name matching; `pnpm audit --audit-level=high` reports 2 moderate only |
| Whitespace/diff integrity | passed | `git diff --check`; changes limited to docs, web, lockfile, supply-chain SBOM matcher, and this task file |
| Session non-regression | passed | Full web suite includes `SessionPage`/`sessionRuntimeView` tests; `SessionPage.tsx` and `sessionRuntimeView.ts` were not edited |
| Playwright accessibility/screenshots | blocked for migrated form/theme | Vite `http://localhost:5173` `/browser/actor-context` 404 (API `:8080` not running). Production `http://localhost:18080/activities` shows sign-in required. Inspected PNGs: `.playwright-mcp/page-2026-08-26T00-11-27-954Z.png` (desktop gate), `.playwright-mcp/page-2026-08-26T00-11-55-342Z.png` (narrow gate). Do not claim Campaign-create or ThemeToggle visual completion |
| Independent architecture/frontend/security/QA review | passed with residual live gap | Implementer review: ADR-019/`FE-DEC-*` match code; Query hooks compose typed clients; cache purge on ready-exit and identity change; keys are not auth; no Axios/Tailwind/Zustand; Session files untouched. Residual: live form/theme screenshots require authenticated production session or synthetic API |
| Post-review confirmation (2026-08-26) | passed with same live gap | Re-ran `python3 scripts/check_docs.py`, `git diff --check`, `pnpm --filter @flex-agent/web test` (24 files, 171 passed), and typecheck. Pins, keys, purge paths, Activities mutation/form contract, and Session non-edit rechecked. Production `:18080/activities` still sign-in gated; Vite `:5173` still Access denied without synthetic API. No new defects found |
| Review follow-up (identity remount, fresh permission, exact invalidation) | passed | Red then green: in-place org replacement clears typed Campaign title without remounting `ProductionApiProvider`; cached `create_assessment` does not call source-options; create invalidates with `exact: true` and `refetchType: "none"`. Full web tests 24 files, 172 passed; typecheck; lint 0 errors; `python3 scripts/check_docs.py` |
| Follow-up confirmation (2026-08-26) | passed | Re-ran docs check, `git diff --check`, web tests (24 files, 172 passed), and typecheck before commit |
| Authorization-context epoch follow-up | passed | Red then green: same actor/Organization with administrator→reviewer shell replacement clears Query cache and Campaign local state, and renders the narrowed shell. `reloadTrustedContext` removed from `useProductionApi`; tests use `reloadTrustedContextForTests`. Full web tests 24 files, 173 passed; typecheck; lint 0 errors; docs check |
| Authorization-context confirmation (2026-08-26) | passed | Re-ran docs check, `git diff --check`, web tests (24 files, 173 passed), and typecheck before commit |
| Session remount follow-up (command refresh ≠ epoch) | passed | Under real `ProtectedBrowserAuthSubtree`: ordinary `executeCommand` follow-up refresh keeps Session stand-in mount id; actor switch and explicit same-actor `replaceAuthorizationContext` remount. `SessionPage.test.tsx` harness was not wrapped (bootstrap remount vs EventSource). Full web tests 24 files, 176 passed; typecheck; lint 0 errors; `python3 scripts/check_docs.py` |
| Session remount confirmation (2026-08-26) | passed | Rechecked `executeCommand` uses `replaceAuthorizationContext: false`; epoch still advances on bootstrap, identity change, and explicit replace. Production epoch path unchanged. Re-ran docs check, `git diff --check`, typecheck, and web tests (24 files, 176 passed). |
| Ready-exit teardown follow-up | passed | Red: command success then follow-up actor-context 403 left `probe-actor` populated. Green: `leaveReady` clears actor/navigation, purges Query, bumps epoch; `BrowserWorkspaceGate` renders denied/error instead of routes. Same-actor capability narrowing remounts. App bootstrap 403 hides navigation. Full web tests 24 files, 179 passed; typecheck; lint 0 errors; docs check |
| Ready-exit confirmation (2026-08-26) | passed | Rechecked `executeCommand` still uses `replaceAuthorizationContext: false`; catch paths all call `leaveReady`; `BrowserWorkspaceGate` handles loading/idle/denied/error before Routes. Re-ran docs check, `git diff --check`, typecheck, and web tests (24 files, 179 passed). |
| Command/resource 401 access-loss follow-up | passed | Red: command and `fetchJson` 401 left actor `ready` and Session stand-in mounted. Green: shared `withAuthenticationLoss` wrapper calls `leaveReady("idle")` then rethrows; sign-in gate shown. Command 403/409 still return domain outcomes. Full web tests 24 files, 181 passed; typecheck; lint 0 errors; docs check |
| Command/resource 401 confirmation (2026-08-26) | passed | Rechecked `executeCommand`, `reconcileCommand`, and `fetchJson` all use `withAuthenticationLoss`; command 401 still skips follow-up refresh; 403/409 remain returned bodies. Re-ran docs check, `git diff --check`, typecheck, and web tests (24 files, 181 passed). |

# Blockers

None. Live Campaign-create and ThemeToggle Playwright remains an accepted
non-blocking verification gap (no authenticated production session and no
synthetic API on `:8080`). It is not a code defect and does not block the
review follow-up.

# Completion

- [x] `WI-FE-01`-`WI-FE-20` are reconciled against actual behavior and files
- [x] Planned work is reconciled with actual changes
- [x] Applicable focused red/green/refactor tests pass with recorded evidence
- [x] Applicable integration/regression checks pass
- [x] Lint and typecheck pass
- [x] Documentation, build, and supply-chain gates pass
- [x] Live accessibility/screenshot verification passes or an exact blocker is recorded without a completion claim
- [x] Governing specifications and the approved frontend-state ADR are rechecked
- [x] Realtime Session behavior and isolation remain unchanged
- [x] Remaining gaps and intentionally unmigrated flows are recorded
- [x] Independent review findings are resolved or explicitly accepted by the owner
- [x] Task state is safe and complete for external review and retained afterward
