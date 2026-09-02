---
id: server-numbered-pagination-selection
status: completed
created: 2026-09-02
updated: 2026-09-02
---

# Goal

Add an opt-in server-numbered mode to the production Activities list, move its
search/order/page/count work into one authorized server query, and make shared
DataTable header selection explicitly page-only or matching-scope capable so a
server page is never mistaken for the complete matching set.

# Governing sources

- `docs/requirements/features/assessment-setup.md` — `REQ-ACT-43`–`REQ-ACT-46`,
  `AC-ACT-23`, `AC-ACT-28`, `AC-ACT-29`
- `docs/ui-ux/flows/assessment-campaign-setup.md` — `UI-ACT-DEC-7`
- `docs/ui-ux/flows/submission-attempt.md` — `UI-SUBM-DEC-9`,
  `UI-SUBM-DEC-13`, `UI-SUBM-DEC-15`
- `docs/ui-ux/design-system/README.md` — v1.1, `DS-DEC-12`, `DS-DEC-13`
- `docs/ui-ux/design-system/components/pagination.md`
- `docs/ui-ux/design-system/components/tables.md`
- `docs/architecture/backend-module-architecture.md` — bounded list-query and
  selection contracts
- `docs/architecture/frontend-architecture.md` — server-backed table and
  selection ownership
- `.work/active/participants-registry-cursor-pager.md` — completed precursor;
  committed at `f17a40b`; do not undo signed-cursor paging or its browser
  evidence

The Activities surface is an existing production registry. Reuse
`AssessmentActivitiesPage`, `OperateArea bay="registry"`, and the approved
DataTable/Component Deck specimen. No new layout family and no `$impeccable
shape` gap are authorized.

# Scope

## In

- Opt-in `paging=numbered` on `GET /v1/assessment/activities`; omitted `paging`
  temporarily preserves the existing complete-list v1 response
- One-based page, default `1`; page-size default `16`, maximum `50`
- Trimmed, case-insensitive title/Activity-ID search up to 200 characters
  with wildcard characters treated literally
- Ordered, unique sort entries drawn from `title`, `activation`, `updated`, and
  `revision`, each `asc`/`desc`, maximum four; default `title:asc`; final
  ascending Activity-ID tie-breaker
- Same-observation authorized rows, `total_items`, and `total_pages`
- Empty out-of-range page with current metadata and deliberate client recovery
- Eliminate the PostgreSQL Activity-list N+1 revision lookup while adding the
  bounded query
- Parameterized TanStack Query keys for all Activity pages and prefix
  invalidation after Activity creation
- Existing numbered DataTable footer driven by returned server metadata,
  including pending, retry, search-empty, and page-drift states
- Shared table-selection capability with `page` and `matching` modes;
  matching scope may omit a total and uses non-numeric copy when it does
- Page-only Assign picker header under P0; current server-page IDs are not
  passed as the complete matching set
- Focused backend, HTTP, PostgreSQL, frontend, accessibility, responsive, and
  security/isolation verification

## Out

- Bulk Participant assignment or any multi-record mutation
- A server selection mutation endpoint; the shared matching descriptor is
  presentation/domain-client groundwork only until a feature approves a
  consuming command
- Adding exact totals or random page jumps to existing cursor endpoints
- Replacing signed Enrollment cursors with offsets
- Retiring the legacy unpaged v1 Activity-list response
- A repository-wide generic pagination BuildingBlock before a second module
  demonstrates the same stable application contract
- Unrelated Assessment setup, Attempt start, or design-lab behavior

# Plan

- [x] Promote approved requirements, UX, design-system v1.1, and architecture
  decisions; generate adapters and validate documentation before code work.
- [x] Red — add Assessment application/store tests for numbered defaults,
  accepted page/search/ordered-sort combinations, deterministic ties,
  out-of-range pages, invalid bounds/specifications, and no store access after
  invalid input.
- [x] Green — introduce Assessment-owned numbered list request/result contracts;
  keep the application use case responsible for validation and current actor /
  Organization authorization before invoking the store.
- [x] Red/green PostgreSQL — replace the unbounded Activity plus per-row revision
  lookup with one scoped joined projection and a count/page operation from one
  consistent observation; prove parity in the in-memory adapter and add
  wrong-Organization/count-leakage integration tests.
- [x] Red/green HTTP — parse the explicit `paging=numbered` query, preserve the
  omitted-mode legacy v1 shape, return additive `pagination` metadata for the
  numbered response, and cover authentication, authorization, validation,
  encoding, defaults, and `Cache-Control: no-store` behavior.
- [x] Red/green frontend API and Query — add canonical Activity query/response
  types, query serialization, `activitiesRoot()` plus parameterized
  `activities(query)` keys, request cancellation, and prefix invalidation for
  every cached page after create.
- [x] Red/green shared selection — replace the implicit `matchingIds` capability
  with an explicit page/matching scope contract; make matching `total`
  optional, keep local complete-set consumers compatible, prevent client-side
  action helpers from resolving an unknown server-wide set, and cover labels,
  transitions, exclusions, query changes, and empty pages.
- [x] Red/green production UI — drive `AssessmentActivitiesPage` from the
  server-numbered Query, remove local page/search/sort ownership, retain the
  last authorized page only as busy placeholder state, retry the exact failed
  query, recover an out-of-range page deliberately, and configure the Assign
  picker header as page-only without changing its one-row commit rule.
- [x] Run focused backend, PostgreSQL, Runtime HTTP, API-client, Query,
  DataTable selection/pagination, Activities-page, and Enrollment-page tests;
  then run `pnpm verify:web:production` and proportionate .NET regression
  suites.
- [x] Attach to a healthy candidate origin (`:5274` with canonical API
  `:18080`), sign in as the synthetic administrator, and inspect first/middle/
  final, search-empty, pending, retry, out-of-range recovery, page-only Assign
  selection, desktop, narrow, keyboard, and accessibility-snapshot states.
- [x] Recheck governing docs, run `python3 scripts/check_docs.py`, reconcile
  implementation and verification evidence here, and prepare independent
  backend, frontend, and security/privacy review handoff.

# Current state

Implementation finished. Independent review found and fixed a pending-page
footer mismatch and hug/total inconsistency. Signed-cursor Participant paging
at `f17a40b` remains in place. Out-of-range page recovery uses React
render-time state adjustment. Footer range uses returned pagination metadata so
placeholder rows are not relabeled as the requested page.

# Decisions

- Server-numbered is data ownership, not a third `DataTablePagination` visual
  mode.
- The Activity v1 transition is opt-in through `paging=numbered`; silent
  truncation of calls that omit paging is prohibited.
- The numbered response retains `activities` and `permitted_actions` and adds
  `pagination: { mode, page, page_size, total_items, total_pages }`.
- Activity ordering accepts at most four unique ordered field/direction entries
  and always ends with ascending Activity ID.
- Exact numbered totals and page rows share one authorized data observation.
- Cursor totals are optional. Matching selection can operate without a total;
  the total changes copy only and never authorizes a mutation.
- A browser `queryKey` is presentation identity. Any future bulk server command
  requires a versioned allowlisted selection descriptor and commit-time
  reauthorization.
- The P0 Assign picker is page-only and still cannot commit multiple rows.

# Findings / deviations

- The current PostgreSQL Activity list is unbounded and performs one current-
  revision query per Activity after its initial list query. The numbered store
  change should remove that N+1 behavior rather than page the outer query while
  keeping per-row lookups.
- The current Activities registry performs search, multi-sort, and page slicing
  with `useTableController`; all three must move together. Server paging with
  local search or sort would misrepresent the matching total.
- The current shared `TableSelection` stores a required matching total and
  resolves matching mode from an exhaustive `matchingIds` array. That contract
  cannot honestly represent a cursor-backed matching scope and must be split
  before enabling generic cross-page matching behavior.
- `.work/active/participants-registry-cursor-pager.md` is completed and its
  implementation is committed at `f17a40b`, but the task remains temporarily
  for review. Its selection-related files overlap this follow-on and must be
  evolved without reverting the signed-cursor behavior.
- Seeded Compose data currently yields two Activities pages (25 items, size
  16), so a distinct middle page is not present; first and last pages were
  inspected instead.
- Live pending/retry and live out-of-range recovery were not reproduced in the
  browser: pending/retry are covered by Activities page tests; out-of-range
  recovery is covered by the page test that requests page 2 then lands on page
  1. The numbered page-jump control only lists valid pages, so drift requires a
  shrinking total between requests.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Governing document consistency | pass | `python3 scripts/check_docs.py` (Documentation validation passed); earlier impeccable-context generate/test from docs promotion |
| Assessment application tests | pass | `dotnet test tests/AssessmentConfiguration/FlexAgent.AssessmentConfiguration.Tests` — NumberedActivityListTests (7) plus suite green |
| PostgreSQL numbered-page/isolation tests | pass | `AssessmentNumberedListPersistenceTests` (2) — org isolation, literal `%`, tie-break, out-of-range |
| HTTP contract/negative tests | pass | `AssessmentHttpNegativeContractTests` numbered facts — invalid paging/bounds/duplicate sort, encoded `q`, omitted paging legacy shape, create then page + `Cache-Control: no-store` |
| Frontend focused tests | pass | Activities, Enrollment, Create, table selection, API, queryKeys; later `pnpm verify:web:production` 617 tests |
| Production frontend gate | pass | `pnpm verify:web:production` lint, typecheck, 617 tests, isolation, candidate bundle |
| Candidate Playwright inspection | pass with gaps | Origin `http://localhost:5274`, synthetic `demo.admin`. Assign page-only header: `.playwright-mcp/page-2026-09-02T12-00-12-388Z.png` (THIS PAGE 16 PARTICIPANTS; a11y “Select all visible participants”). Activities first: `.playwright-mcp/page-2026-09-02T12-01-55-656Z.png` (`01–16 OF 25`, PAGE 01). Last: `.playwright-mcp/page-2026-09-02T12-02-19-554Z.png` (`17–25 OF 25`, PAGE 02, Next disabled). Search-empty: `.playwright-mcp/page-2026-09-02T12-02-44-116Z.png` (`00 OF 00`). Narrow 390×844: `.playwright-mcp/page-2026-09-02T12-03-15-908Z.png`. Keyboard Next focus: `.playwright-mcp/page-2026-09-02T12-05-07-959Z.png`. Cursor registry unchanged: `.playwright-mcp/page-2026-09-02T12-03-53-779Z.png` (`01–16`, no page jump). Snapshots under `.playwright-mcp/*.yml`. Gaps: live pending/retry/out-of-range not exercised in browser. |
| Independent review | pass with residual gaps | 2026-09-02 re-review: backend contracts/authz/isolation hold; footer now uses returned pagination during pending; hug uses visible rows; Assign remains page-only; cursor Participants unchanged. Residual: live pending/retry/drift not reproduced in browser; unbounded page offset; no bulk selection mutation. |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
