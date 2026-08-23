---
id: p0-enrollment-assignment-discovery
status: in-progress
created: 2026-08-22
updated: 2026-08-23
---

# Goal

Implement the first production Enrollment slice for an activated assessment
Campaign: an authorized Activity administrator can assign one eligible existing
Participant to one activated Cohort, inspect and change that Enrollment through
its permitted lifecycle, and the Participant can discover the currently
authorized Assignment through production **My work** list and detail views.

The slice must preserve the immutable activation baseline, derive every trusted
relationship on the server, make equivalent assignment commands idempotent,
fail closed on authorization or required-audit loss, prevent list/count/detail
disclosure, and expose no submission-intake, Attempt-start, Session, or external
delivery authority that has not yet been implemented.

# Governing sources

- `AGENTS.md` and `.work/README.md` — repository invariants, specification-driven
  TDD, UI verification, security defaults, and tracked execution state.
- `docs/README.md#authority-by-concern` — authority ordering for product,
  observable behavior, UI/UX, architecture, and implementation.
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — Organization, Activity, Cohort, Enrollment,
  Participant, Session, and MVP boundaries.
- `docs/requirements/features/submission-attempts.md` — primary behavior:
  `REQ-SUBM-1`–`REQ-SUBM-8`, `REQ-SUBM-43`, `AC-SUBM-1`–`AC-SUBM-4`,
  `AC-SUBM-29`, and approved `PROP-6`. Apply the Enrollment-specific portions
  of `REQ-SUBM-36`–`REQ-SUBM-43`, `AC-SUBM-19`, `AC-SUBM-23`–`AC-SUBM-25`,
  `AC-SUBM-27`–`AC-SUBM-28` without claiming the later Submission/Attempt
  portions are implemented.
- `docs/requirements/features/assessment-setup.md` — activated-Cohort handoff,
  especially `REQ-ACT-23`–`REQ-ACT-25`, `REQ-ACT-31`–`REQ-ACT-34`,
  `AC-ACT-12`, `AC-ACT-19`, and the rule that Enrollment never mutates the
  activation baseline.
- `docs/requirements/features/auth-resource-isolation.md` and
  `docs/requirements/mvp-operational-defaults.md` — current application-session,
  Organization/action/resource/relationship authorization, MFA, CSRF,
  revocation, non-disclosure, scoped-query, audit, and latency defaults. Apply
  `REQ-AUTH-1`–`REQ-AUTH-7`, `REQ-AUTH-10`, `REQ-AUTH-12`–`REQ-AUTH-13`,
  `REQ-AUTH-16`–`REQ-AUTH-22`, `REQ-AUTH-26`–`REQ-AUTH-31`, and their applicable
  acceptance criteria.
- `docs/ui-ux/activity-campaign-journey.md` — `JRN-MVP-2`, production navigation,
  and the activated-Cohort-to-Enrollment handoff.
- `docs/ui-ux/submission-attempt.md` — approved administrator Assignment,
  Enrollment lifecycle, participant discovery, access-change, content, and
  state behavior. Only the Enrollment/discovery subset is in scope.
- `docs/ui-ux/design-system/README.md` and
  `docs/ui-ux/design-system/implementation-guide.md`, with applicable foundation
  accessibility, color, typography, layout, density, interaction-state, status,
  and motion modules; button, input, error-summary, alert, badge, card, list,
  modal, table, pagination, and content components; and empty/loading,
  protected-content, technical-metadata, and timeline product patterns.
- `docs/architecture/mvp-architecture.md` and
  `docs/architecture/backend-module-architecture.md` — modular-monolith
  ownership, protected persistence, scoped ports, thin host adapters,
  cross-module coordination, and isolation gates.
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`,
  `ADR-003-authorization-audit-persistence.md`,
  `ADR-004-assessment-activation-baseline-and-atomicity.md`,
  `ADR-010-dotnet-implementation-stack-and-workspace.md`, and
  `ADR-017-assessment-source-authority-and-activation-transaction.md` — trusted
  authorization, required-durable audit, immutable baseline ownership,
  .NET/React/PostgreSQL workspace direction, and named transaction composition.
- `.work/active/p0-assessment-setup-cohort-activation.md` — completed predecessor
  that owns the activated baseline and intentionally omitted **Assign
  Participants** until this production capability exists.

# Scope

## In

- Add the first governed behavior under `src/Modules/Submissions/`, with a core
  assembly for Enrollment domain/application contracts and a separate
  infrastructure assembly for PostgreSQL and other protected adapters because
  isolation and transaction rules merit a compile-time boundary.
- Add an additive migration sequence after `0042` for Submissions-owned Enrollment,
  append-only lifecycle/history, and command-idempotency state plus the minimal
  IdentityAccess-owned display profile needed to name pre-provisioned eligible
  human actors. Preserve existing migrations unchanged.
- Assign one existing eligible Participant at a time to one activated Cohort.
  Treat the Participant identifier supplied by the UI as a locator only; derive
  Organization membership/eligibility, Activity/Cohort/baseline/task parentage,
  and current administrator authority from trusted server state.
- Support `Active`, `Suspended`, restored `Active`, `Closed`, and `Revoked`
  Enrollment state with optimistic revision checks, append-only history,
  bounded reason codes, exact actor/time/correlation, and preserved terminal
  records.
- Make exact duplicate and concurrent same-Cohort assignment resolve to one
  live Enrollment. Permit a new Enrollment identity after the old relationship
  is `Closed` or `Revoked`, including the same Cohort, without repurposing the
  terminal record. Treat a live Enrollment in another Cohort of the same
  Activity as a safe conflict in this slice; a future separately authorized
  reassignment command may terminalize the old Enrollment and create another in
  one governed operation.
- Compose assignment and lifecycle commands through one explicitly named
  Submissions-owned transaction coordinator. Revalidate administrator action,
  Participant eligibility, activated-Cohort/baseline/task facts, expected
  revision, and conflicting Enrollment state in the commit transaction; commit
  protected state, append-only history, audit, and outbox atomically.
- Add narrow owner-controlled collaboration ports:
  - IdentityAccess returns only same-Organization eligible human actors and a
    permitted display identity for the scoped selector, and revalidates actor,
    application-session, authentication strength, grant, and eligibility.
  - Assessment Configuration returns and transactionally revalidates only the
    activated Cohort's trusted Organization/Activity/Cohort/baseline/task
    binding and participant-visible summary. Submissions never queries
    Assessment tables directly.
- Add production administrator HTTP and React surfaces for scoped participant
  choices, Enrollment list/detail, assignment, suspend, restore, close, and
  revoke. Replace the predecessor's omitted action with an authorized
  **Assign Participants** destination only when both the activated Cohort and
  current action projection permit it.
- Add production Participant HTTP and React **My work** list/detail surfaces.
  Show only current Assignment state, approved Participant-visible task/timing
  summary, and the next implemented action (**Open assignment** or a bounded
  support/return action). Do not invent Submission readiness, Attempt
  entitlement, or Session state.
- Extend production Home, breadcrumbs, login return paths, and route guards so
  an authenticated Participant can reach and return to **My work** without
  receiving an administrator Activity route or losing the originally requested
  safe same-origin path.
- Keep Suspended Assignments visible to their own Participant with a clear
  current state and no new protected action. Remove Closed/Revoked Assignments
  from current **My work** and make protected deep-link content unavailable;
  never show stale `Active` state. Administrative history remains authorized
  and inspectable.
- Extend the production shell/navigation projection so **Activities** and **My
  work** are independently server-authorized. A Participant's discovery action
  must not accidentally expose the administrator destination merely because
  the actor has some granted action.
- Extend the authenticated Development/Testing browser profile with a fixed
  synthetic Participant actor/binding, permitted display profile, exact
  eligibility/discovery grants, and production API/UI data. Keep all evidence
  synthetic and keep `/browser` non-authoritative.
- Add minimized bounded observability for create/deduplicate/conflict,
  lifecycle mutation, denial, discovery, audit failure, and latency. Exclude
  Participant attributes, display names, raw identifiers, reason text, and
  protected resource content from metrics/log labels.
- Update authoritative implementation traceability and developer docs only for
  behavior actually implemented and verified; leave later Submission/Attempt
  rows as gaps.

## Out

- Creating, inviting, importing, editing, or globally searching Participant
  identities; a general user/Participant directory; bulk assignment; and
  cross-Organization identity administration.
- A general reassignment workflow. This slice returns a conflict for another
  live Cohort relationship and does not silently close or replace it.
- Accommodations, fairness-exception approval, retry entitlements, calculated
  Participant-specific timing windows, Submission intake/uploads/versions,
  Attempt eligibility/start/consumption, ADR-005 composition, or Session
  creation/control.
- External email, SMS, calendar, webhook, or other delivery channels and their
  provider, consent, privacy, retry, or delivery-status contracts. In-product
  discovery is the only P0 channel and is not separate authorization evidence.
- Reviewer raw-Submission access, Evaluation, review/release, Result, export,
  learning/memory reuse, or retention-policy invention.
- Reusing synthetic `/browser` contracts, pages, state, or endpoints as
  production authority. They may remain a bounded Development/Testing fixture.
- Editing the activated Cohort baseline, changing its digest, copying
  Participant roster data into it, or treating a matching digest as
  authorization.
- A new service boundary, queue-first consistency model, or ADR unless
  implementation discovers a durable ownership/atomicity decision not already
  governed by the approved modular-monolith and coordinator rules.

# Acceptance and traceability matrix

| Governing behavior | Implementation surface | Required evidence |
| --- | --- | --- |
| One authorized Enrollment binds one Participant to the trusted Organization/Activity/activated Cohort/baseline/task without baseline mutation (`REQ-SUBM-1`–`REQ-SUBM-4`, `AC-SUBM-1`–`AC-SUBM-2`, `REQ-ACT-25`, `AC-ACT-12`) | Enrollment aggregate/handler, Assessment activated-Cohort port, IdentityAccess eligibility port, composite database constraints, assignment API/UI | Domain and PostgreSQL tests for exact linkage and unchanged baseline; inactive/missing/degraded/wrong-parent/wrong-Organization/disabled-actor cases; authenticated assignment screenshot |
| Equivalent retry and concurrency create one Enrollment; conflicts never overwrite or silently reassign (`REQ-SUBM-5`, `AC-SUBM-3`) | Trusted command digest, scoped idempotency record, uniqueness/locking, conflict outcome and reconciliation UI | Same-key/same-command, same-key/different-command, distinct-key concurrent duplicate, lost-response retry, and live-other-Cohort conflict tests |
| Enrollment access changes are current, reasoned, durable, and historical (`REQ-SUBM-6`, `REQ-SUBM-36`–`REQ-SUBM-41`, `AC-SUBM-4`, `AC-SUBM-24`–`AC-SUBM-25`) | State machine, expected revision, append-only events, required-durable audit/outbox, lifecycle endpoints and confirmations | Allowed/forbidden transition matrix; stale/concurrent command; audit/outbox fault injection; update/delete rejection for history; exact target/consequence/reason and success/failure UI states |
| Cohort membership, URLs, lists, counts, filters, caches, and projections do not grant or leak access (`REQ-SUBM-2`, `REQ-SUBM-7`, `REQ-SUBM-42`, `AC-SUBM-19`) | Server-derived scope, cursor queries without unscoped totals, concealed errors, relationship-aware own-detail query, safe caches | Wrong Organization/activity/cohort/Participant, actor substitution, guessed ID, tampered parent, list/count/filter/pagination, stale grant, forwarded deep-link, and cache-key tests |
| Every currently authorized Assignment is discoverable in product and stale Active state disappears (`REQ-SUBM-8`, `REQ-SUBM-43`, `AC-SUBM-29`, `PROP-6`) | Minimized initial availability event, `My work` navigation, scoped authoritative list/detail projections, current-state read, no external delivery action | Empty/active/suspended/revoked/closed/access-changed list/detail tests; event-is-not-authority/delivery-proof test; participant A/B and Organization A/B isolation; authenticated Participant screenshots |
| Assignment/Enrollment UI is accessible, responsive, and honest about unavailable downstream capabilities (`AC-SUBM-23`, approved UI spec) | Production routes/pages, shared design-system components, focus/status/error contracts, desktop/narrow layouts | Component keyboard/focus/announcement tests; accessibility snapshots and Playwright screenshots for applicable loading, empty, active, pending, success, duplicate, conflict, validation, stale, denied, suspended, terminal confirmation, audit-failure, and access-change states in both themes and narrow/400% conditions |
| Authorization and operational objectives remain measurable without protected labels (`AC-SUBM-27`, auth `PROP-8`) | Server timing/decision metrics and bounded logs; no Participant labels/IDs in dimensions | Representative synchronous Enrollment mutation p95 at or below 2 seconds and in-service authorization p95 at or below 50 ms under documented preconditions; high-cardinality/leakage assertions, project gitleaks, and documented results |

`AC-SUBM-4` is complete for this slice only at the Enrollment authority
boundary: Suspended/Closed/Revoked must immediately return no new-intake or
new-start authority from the owner-controlled decision contract and remove such
actions from current projections. End-to-end denial by the later Submission
intake and Attempt-start consumers remains an explicit downstream gap and must
not be marked implemented by this task.

# Architecture and data design

## Ownership and dependency boundaries

- `FlexAgent.Submissions` owns Enrollment identity, lifecycle rules, command and
  query use cases, idempotent outcomes, Participant relationship checks, and
  transport-neutral application results needed by this slice.
- `FlexAgent.Submissions.Infrastructure` owns Submissions SQL, PostgreSQL
  transaction/repository adapters, and required audit/outbox integration. It
  may implement application ports but the core assembly must not reference
  Npgsql, Dapper, host, or another module's infrastructure assembly.
- IdentityAccess continues to own actors, human identity bindings, application
  sessions, Organization action grants, and the new minimal display profile.
  Its scoped candidate port must require trusted Organization and action scope,
  return no general directory/count, and expose only an opaque actor locator and
  permitted display label.
- Assessment Configuration continues to own Activity, Cohort, activation
  baseline, and task-revision source facts. Its new read/revalidation port must
  verify `Activated`, exact parentage/binding, and baseline integrity state; it
  returns a minimized immutable snapshot and never transfers table ownership.
- `FlexAgent.Api` remains a composition root with thin request validation,
  current application-session/CSRF establishment, use-case invocation, and
  outcome mapping. No Enrollment domain policy belongs in endpoints.
- Canonical schemas and browser-safe derived DTOs belong in `contracts/` and
  the existing `FlexAgent.Contracts`/web contract surfaces; they contain no
  live permission decision or backend ownership internals. React consumes those
  versioned browser-safe DTOs only. It never treats route IDs,
  hidden fields, role labels, cached `permitted_actions`, or selector results as
  authority.

## Persistence contract

- Add Submissions-owned tables with explicit Organization scope:
  - `submissions_enrollments`: stable Enrollment ID; Organization, Activity,
    Cohort, baseline, exact task source/version/digest, applicable lifecycle
    policy reference, and Participant actor references; current status;
    monotonic revision; assignment actor/time; updated time.
  - `submissions_enrollment_events`: append-only ordered lifecycle facts with
    prior/new state, bounded reason code, actor, UTC time, correlation,
    authorization reference, and Enrollment revision.
  - `submissions_enrollment_operations`: scoped idempotency key, trusted command
    digest, operation kind, authoritative outcome/Enrollment reference, and
    timestamps sufficient for retry reconciliation without protected payloads.
- Add an IdentityAccess-owned Organization-scoped human display profile rather
  than storing names in Submissions. Require a bounded, sanitized display label
  and active actor/binding/eligibility; do not expose provider claims or raw
  profile attributes through Enrollment contracts.
- Treat that profile as pre-provisioned IdentityAccess data, not a new runtime
  account/profile-management API. Missing, disabled, ambiguous, or cross-scope
  profiles do not appear as candidates and cannot be assigned by direct actor
  locator. Display-label changes affect future permitted presentation only;
  stable actor/Enrollment IDs preserve historical identity without snapshotting
  the label into Enrollment or audit records.
- Enforce composite Organization/parent foreign keys and explicit status,
  length, timestamp, revision, and digest constraints. Add an
  Assessment-owned unique key for the exact
  `(organization, activity, cohort, baseline)` binding if required as the target
  of the Submissions foreign key; do not approximate the binding with two
  unrelated foreign keys. Enforce at most one live (`Active` or `Suspended`)
  Enrollment per Participant/Activity. Do not add a permanent
  Participant/Cohort uniqueness rule: terminal records remain and a later
  authorized assignment creates a new identity instead of rewriting history.
- Lifecycle transitions are exactly: absent to `Active`; `Active` to
  `Suspended`, `Closed`, or `Revoked`; `Suspended` to `Active`, `Closed`, or
  `Revoked`; `Closed` and `Revoked` terminal. Equivalent commands reconcile;
  stale or incompatible transitions do not overwrite current state.
- A retry with the original idempotency key always reconciles to that original
  authorized outcome, even after its Enrollment becomes terminal. Creating a
  later Enrollment requires a fresh key and a newly authorized command; key
  expiry follows policy and never turns a retry into implicit reassignment.
- Baseline ID, Cohort ID, exact task binding, Participant, Organization, and
  assignment identity are immutable on an Enrollment. Current state may change
  only with a matching expected revision and an appended event.
- Resolve and store the exact applicable lifecycle-policy reference before
  Enrollment creation. Follow `REQ-SUBM-39` and `REQ-OPS-18`–`REQ-OPS-23`:
  preserve restricted Enrollment/history metadata when no more-specific
  disposal duration exists, apply the approved 90-day idempotency-record default
  without breaking authoritative duplicate lookup, and do not invent an
  Enrollment retention duration or destructive cleanup path in this slice.
- Use the existing `audit_events` and `outbox_items` contracts. Assignment and
  every access-control mutation are `required_durable`: no protected transition
  commits when audit/outbox acceptance fails.
- Migration `0043` must be additive, empty-database and `0042`-upgrade safe, and
  immutable after first shared execution. If implementation requires more than
  one additive migration, continue monotonically instead of editing an executed
  script.

## State, visibility, and action contract

| Enrollment state | Administrator state actions | Participant current list/detail | Enrollment decision for future new intake/start | Reassignment consequence |
| --- | --- | --- | --- | --- |
| Absent | Assign when Cohort and Participant are currently eligible | No row/detail | Deny | Creates a new `Active` Enrollment |
| `Active` | Suspend, Close, or Revoke only when each exact action is returned | Visible with **Open assignment** | Permit only the Enrollment relationship; later consumers must still enforce their own rules | Equivalent same-Cohort command returns the same Enrollment; another live Cohort conflicts |
| `Suspended` | Restore, Close, or Revoke only when each exact action is returned | Visible to the owner with `Suspended`, no stale Active state, and only a safe support/return action | Deny immediately | Conflicts until restored or made terminal |
| `Closed` | Read minimized authorized history; no state mutation | No current-list row; a direct Participant locator returns the common protected-unavailable state with no Task content | Deny | A later authorized assignment creates a new Enrollment identity |
| `Revoked` | Read minimized authorized history; no state mutation | No current-list row; a direct Participant locator returns the same protected-unavailable state as other inaccessible IDs | Deny | A later authorized assignment creates a new Enrollment identity |

- Enrollment lifecycle and Participant visibility remain separate decisions even
  when this initial policy maps them as above. The API returns both current
  state and current visibility/actions; clients never infer visibility from a
  status string alone.
- Participant-visible timing is a server-supplied descriptive projection of the
  frozen Cohort schedule and named timezone. It is not a calculated
  Participant-specific window and does not authorize Submission intake or an
  Attempt. If current baseline/task verification is degraded after assignment,
  keep the authorized Assignment relationship visible with a generic unavailable
  state and no downstream action; do not disclose digests or setup internals.
- Use bounded transition reason categories that restate the approved lifecycle:
  `temporary_restriction`, `restriction_removed`, `activity_or_enrollment_end`,
  and `access_revoked`. The applicable category is required, included in the
  trusted command digest, validated for the requested transition, and shown in
  terminal confirmation. Do not accept or log free-form reason text in this
  slice.
- Reconciliation of an existing operation or duplicate relationship must
  reauthorize the caller before returning protected Enrollment details. A
  committed outcome may remain durable while the retry response becomes a safe
  denial after grant, session, or resource-scope loss.

## Production HTTP and browser contracts

- Define committed Draft 2020-12 schemas before endpoint implementation for the
  assignment and lifecycle commands, candidate/Enrollment pages, administrator
  detail, Participant Assignment list/detail, and safe outcomes. Validate them
  with `JsonSchema.Net`, project OpenAPI 3.1, and map reviewed C# and browser
  DTOs with parity tests. Reject unknown request members, coercion, excessive
  depth/size, invalid UUIDs, overlong query/cursor/idempotency/reason values, and
  response fields outside the deny-by-default schemas.
- Keep stable bounded outcome codes, server-derived `permitted_actions`, current
  revision, visibility, and authoritative state in responses; do not serialize
  canonical domain objects. Version the schema independently from internal
  aggregate and database representations.
- Administrator routes under the existing production Assessment gateway:
  - `GET /v1/assessment/activities/{activityId}/cohorts/{cohortId}/participant-options`
    with bounded query/cursor and no unscoped total.
  - `GET|POST /v1/assessment/activities/{activityId}/cohorts/{cohortId}/enrollments`.
  - `GET /v1/assessment/activities/{activityId}/cohorts/{cohortId}/enrollments/{enrollmentId}`.
  - idempotent `POST` commands ending in `/suspend`, `/restore`, `/close`, and
    `/revoke`, each carrying expected revision, idempotency key, and the bounded
    reason code required for that lifecycle transition.
- Participant routes:
  - `GET /v1/assessment/my-work` with bounded cursor and no inaccessible totals.
  - `GET /v1/assessment/my-work/{enrollmentId}` with own-current-relationship
  authorization; the ID is only a locator.
- Candidate and list queries use a positive default/maximum page bound frozen in
  the transport schema, deterministic ordering, and a cursor bound to the
  current actor, Organization, query, and resource scope. Cursor tampering,
  over-limit requests, and page traversal after access change fail safely. The
  gateway applies bounded per-actor/Organization request limits without using
  high-cardinality protected labels. This slice ships a replica-local API
  limiter as defense in depth; a shared gateway-enforced quota remains an
  explicit residual.
- Cookie-authenticated mutations require the established CSRF token. Assign and
  administrator Enrollment reads/mutations require current administrator MFA;
  Participant discovery uses the current Organization authentication policy
  and exact own-Enrollment relationship rather than assuming an administrator
  MFA rule.
- Centralize the initial exact action identifiers in the Submissions domain
  contract: `assessment.enrollment.candidate.read`,
  `assessment.enrollment.receive`, `assessment.enrollment.list`,
  `assessment.enrollment.read`, `assessment.enrollment.assign`,
  `assessment.enrollment.suspend`, `assessment.enrollment.restore`,
  `assessment.enrollment.close`, `assessment.enrollment.revoke`, and
  `assessment.assignment.discover`. Candidate eligibility and discovery are
  distinct; neither grants administrator mutation authority.
- Do not reuse the current Assessment relationship heuristic, which classifies
  any non-administrator `assessment.*` grant as Reviewer. Shell bootstrap and
  Participant discovery must support a Participant action without Reviewer MFA,
  while administrator Enrollment actions still require current administrator
  MFA. Authorize the exact operation and resource relationship so an actor with
  both administrator and Participant capabilities receives only the authority
  applicable to the requested action.
- Normalize unauthenticated, denied, concealed-not-found, validation, stale,
  duplicate, conflict, audit-unavailable, and dependency-unavailable outcomes
  consistently with existing production API conventions. Wrong-scope and
  guessed-resource responses must not disclose which parent or identity exists.
- Add production routes:
  `/activities/:activityId/cohorts/:cohortId/participants`, nested Enrollment
  detail, `/my-work`, and `/my-work/:enrollmentId`. The setup success action
  navigates to the exact server-returned Cohort route only when permitted.
- Add `my-work` to the production shell destination map and derive each
  navigation item independently. Shell bootstrap requires a valid current
  application session and Organization context, not the administrator-only
  `assessment.activity.read` policy. Activities availability requires an
  applicable current administrator action and MFA; My work availability
  requires `assessment.assignment.discover` and the Organization's Participant
  authentication policy, even when the list is empty.
- Protected JSON responses use `Cache-Control: no-store` and applicable browser
  security headers. Protected loading renders no stale content; logout, actor
  change, Organization change, 401/403, or visibility loss aborts/invalidate
  in-flight requests and clears protected state before any late response can
  repopulate it. Login uses the existing safe-return-path validator rather than
  the current hard-coded `/activities` return.

## Authorization and disclosure matrix

| Surface | Caller authority | Resource relationship and strength | Disclosure rule |
| --- | --- | --- | --- |
| Production shell/Home/navigation | Current application session and one server-bound Organization | Evaluate each destination independently; Administrator destinations require MFA, Participant destination follows Organization policy | Destination availability only; no Enrollment, candidate, or count data |
| Candidate page | `assessment.enrollment.candidate.read` | Current administrator MFA and delegated Activity/Cohort scope | Only currently eligible same-Organization actor locator plus permitted display label; no total or unavailable identities |
| Assign command | `assessment.enrollment.assign` on the target Activity/Cohort | Current administrator MFA; target independently has `assessment.enrollment.receive`; Assessment binding and target eligibility revalidated at commit | Created/Already assigned or minimized safe conflict; wrong scope/target is concealed |
| Administrator list/detail | `assessment.enrollment.list` or `.read` | Current administrator MFA and exact Activity/Cohort scope | Only scoped Enrollment summaries; no raw Task/baseline internals, other Cohorts, or unscoped totals |
| Suspend/restore/close/revoke | The exact matching mutation action | Current administrator MFA, exact Enrollment parent chain, expected revision, valid reason, commit-time reauthorization | Current state/revision after durable commit; audit failure never projects success |
| My work list/detail | `assessment.assignment.discover` | Current application session, Organization Participant policy, and `participant_actor_id == current actor`; current visibility must permit the read | Own minimized Assignment only; another actor, closed/revoked, guessed ID, or inaccessible parent returns the common unavailable behavior |
| Eligibility marker | `assessment.enrollment.receive` on the target actor | IdentityAccess-owned active actor/binding/Organization relationship | Used only by candidate/assignment validation; does not grant a UI destination or access by itself |

- Freeze the initial per-route ADR-003 audit classification in the
  transport/application contract: Enrollment assignment and lifecycle mutations
  are `required_durable`; candidate/admin Enrollment reads and repeated or
  security-relevant enumeration denials are `bufferable`; shell plus a
  Participant's minimized own **My work** reads are `operational_sample`.
  Bufferable events require bounded durable acceptance before the response and
  may not silently downgrade. Tests cover exhaustion/backpressure, while
  ordinary own-list telemetry stays minimized and does not become an
  authoritative audit stream accidentally.

# Security and privacy review matrix

- Protected assets/data classes: Participant actor locator and permitted display
  label; Enrollment ownership/state/history; Activity/Cohort/baseline/task and
  lifecycle-policy references; authorization/audit/idempotency metadata. No raw
  Submission, Session, Evaluation, credential, or provider token is in scope.
- Actors: authorized Activity administrator, owning Participant, dual-capability
  human actor, unauthorized same-Organization actor, cross-Organization actor,
  and internal audit/outbox adapters. The browser and route/cursor values are
  untrusted entry points.
- Trust boundaries: Keycloak establishes external authentication only; the
  opaque application session resolves the platform actor/Organization;
  IdentityAccess owns grants/eligibility/display; Assessment owns the activated
  binding; Submissions owns Enrollment decisions; PostgreSQL is the shared
  physical transaction boundary with logical module ownership. No external
  notification or other new network system participates.

| Threat or failure | Required control | Required negative evidence |
| --- | --- | --- |
| Cross-Organization, Activity, Cohort, Participant, or Enrollment substitution/IDOR | Trusted application session; scoped owner ports and SQL; composite constraints; server-derived parentage; locator-only URLs | Every wrong-scope permutation across command, detail, list, candidate, navigation, cache, and pagination paths returns a non-disclosing denial and changes nothing |
| Candidate selector becomes a Participant directory or enumeration oracle | Exact admin action; Organization-scoped eligibility grant; bounded prefix/cursor; minimized label; no total or unavailable-identity hints | Query, empty, count, cursor, guessed actor, disabled binding, removed grant, and cross-Organization cases |
| Stale administrator grant, Participant eligibility, application session, or Enrollment status authorizes a commit | Commit-time IdentityAccess and Assessment revalidation in the same named transaction; expected revision; current relationship query | Revoke/disable/change-after-read races and two-administrator concurrent mutations |
| Duplicate/replayed command or key collision changes another outcome | Operation key scoped to Organization/actor/use case/resource plus trusted canonical command digest; same-key mismatch rejected | Equivalent retry, altered Participant/parent/reason/revision, concurrent distinct keys, and lost-response reconciliation |
| A formerly authorized caller replays an idempotency key to recover protected outcome data | Keep committed outcome durable but reauthorize current action/resource scope before disclosure | Commit then revoke grant/session/Enrollment visibility; retry returns safe denial without changing or exposing the committed record |
| Cross-module SQL or a confused-deputy port bypasses owner authority | Narrow application-facing contracts, trusted typed scope, no infrastructure references, architecture dependency tests | Architecture tests plus forged/mismatched owner-port contract cases |
| Audit/outbox failure leaves unaudited access state | State/event/audit/outbox in one transaction; `required_durable` fail closed | Fault injection at every insert/commit boundary proves no Enrollment/status change and safe operational signal |
| Projection/cache lag shows stale Active or leaks totals | Authoritative scoped reads for this slice; actor/Organization/resource cache keys; immediate invalidation or no protected cache | Suspend/revoke/close then list/detail; Participant A/B; Organization switch; old deep link; pagination/count cases |
| Display identity, reason, or protected IDs leak through logs, metrics, URLs, errors, or screenshots | Minimized DTOs, bounded outcome/reason categories, allowlisted telemetry, synthetic evidence only | Serialization/log/metric/error/storage inspection, gitleaks, and artifact review |
| Browser role/action state or hidden fields grant authority | Server-side authorization for every query/command; independently derived navigation; current response replaces stale state | Forged `permitted_actions`, parent IDs, expected revision, CSRF omission, direct route, back/forward cache, and access-change tests |
| A late protected response repopulates UI after logout, 401/403, actor/Organization change, or route replacement | `no-store`; abort/sequence in-flight requests; clear per-resource state on identity/scope generation change | Delayed candidate/list/detail responses after logout/access loss/route change never render or remain in browser state |
| Enrollment/history/idempotency outlives approved purpose or is deleted inconsistently | Exact lifecycle-policy reference, dependency-aware preservation, approved idempotency default, no ad hoc deletion | Missing/unknown lifecycle policy fails creation; terminal history remains reconstructable; idempotency disposal cannot permit duplicate live Enrollment |

# Plan

- [x] Reconcile the approved product, requirements, UI/UX, architecture, design
  system, predecessor task, current production routes, IdentityAccess seed, and
  database seams; freeze this task to Enrollment assignment/lifecycle plus
  in-product discovery.
- [x] Record the actual implementation baseline before edits: `git status`,
  relevant solution/web tests, migration head `0042`, authenticated-browser
  profile health, and current activated-Cohort production handoff. Add exact
  commands/results to this file without copying credentials or protected data.
- [x] Red/green — Submissions core/infrastructure projects, architecture
  boundary tests, and host composition. Canonical Draft 2020-12 Enrollment
  schemas, C#/browser mapping, and OpenAPI parity remain an explicit gap.
- [x] Red/green — Enrollment domain tests (19) for creation, lifecycle,
  revision, terminal history, retry/conflict, digest mismatch, new identity
  after terminal, reason validation, visibility, and intake denial.
- [x] Green — Enrollment aggregate, coordinator, query service, and in-memory
  fakes without Submission/Attempt/Session placeholders.
- [x] Green — migration `0043`, repositories, named unit of work, required
  audit/outbox adapter, and parameterized scoped SQL. PostgreSQL evidence
  covers assignment without baseline mutation, new identity after close, and
  append-only history. Full concurrency/fault-injection/upgrade matrix is not
  yet complete.
- [x] Green — owner-controlled Assessment activated-Cohort reader, IdentityAccess
  display-profile directory, and synthetic participant seed. Dedicated
  disabled-identity and change-after-read collaboration tests remain thin.
- [x] Green — production Enrollment and My work endpoints, independent shell
  destinations, 403 protected-state clearing, and safe login return-path
  client. Full HTTP-negative/runtime matrix is not yet complete.
- [x] Green — production React routes/pages and focused component tests for
  assignment success, My work active list, setup handoff, and 403 clearing.
  Lifecycle confirmation, empty/suspended/unavailable, and delayed-response
  races are only partially covered.
- [x] Review remediation: serialize assignment by `(organization, activity,
  participant)` before the live-row read; lock Enrollment rows for lifecycle
  updates and translate zero-row updates to `enrollment.stale_revision`;
  return Assessment's authoritative verification state and refuse assignment
  when degraded; write successful assignment audit against the created
  Enrollment; retain one client idempotency key for a pending mutation.
- [x] Review remediation 2: revalidate Assessment draft/source/baseline and
  the administrator application session inside the Enrollment transaction;
  lock Participant eligibility rows; insert live Enrollment without aborting
  on uniqueness; bind browser retry keys to command identity; authorize
  lifecycle mutations as Enrollment resources.
- [x] Review remediation 3: move session revalidation behind an IdentityAccess
  owner port; lock the display-profile row; fail closed on a mismatched
  commit-transaction handle.
- [x] Review remediation 4: confirm application-session validity against the
  database clock immediately before Enrollment mutation or replay disclosure.
- [x] Review remediation 5: confirm replay liveness after the locked Enrollment
  read, immediately before disclosure.
- [x] Review remediation 6: evaluate shell destinations independently so a
  dual-capability actor can reach My work without administrator MFA, and
  confirm session liveness immediately before Enrollment commit.
- [x] Review remediation 7: roll back in-memory Enrollment/operation/audit
  mutations when the pre-commit session confirmation fails, and require a
  non-null session port on both unit-of-work implementations.
- [x] Review remediation 8: require the Enrollment actor on
  `IEnrollmentUnitOfWork.ExecuteAsync` and confirm session liveness from that
  argument so callers cannot skip the pre-commit check.
- [x] Remaining verification progressed: live administrator create/activate/
  assign/suspend/restore Playwright; participant populated My work list/detail
  after login return to `/my-work`; tampered-cursor fail-closed tests; one
  PostgreSQL assignment latency bound; focused architecture/domain/HTTP/web
  suites. Full solution/OCI gates, per-actor rate limits, 50 ms authorization
  p95, 400% zoom, and independent review remain open.
- [x] Review remediation: bind Enrollment/My work cursors to actor,
  Organization, query, and resource scope with an HMAC; reject forged,
  cross-scope, overlong, and one-bit-tampered cursors; reject out-of-range
  and unparsable limits with 400/`no-store`. The 2 s mutation p95 gate stays
  open; the PostgreSQL assignment check is a single-sample smoke only.
- [x] Review remediation: load a shared current/previous Enrollment cursor
  signing key from deployment-managed secret or configuration so replicas
  can verify each other's tokens; bind participant-options cursors the same
  way and pass only `afterActorId` to IdentityAccess.
- [x] Review remediation: bind a SHA-256 digest of the normalized candidate
  prefix so issued cursors stay within `MaximumCursorLength`; reject cursor
  signing secrets that are not ≥32 decoded bytes.
- [x] External review of `888cd66` approved the cursor/security remediations
  with no blocking code findings. The task stays **in-progress** for already
  recorded residuals.
- [x] Close remaining in-scope residuals that this slice can meet without a
  shared gateway store: replica-local per-actor/Organization Enrollment
  request limits as defense in depth, multi-sample mutation p95, in-service
  authorization p95, and bounded mutation/limit telemetry without protected
  labels. Shared/gateway-enforced bounded quota, authenticated 400%
  Playwright, full `FlexAgent.slnx`/OCI, and independent slice review remain
  recorded residuals.
- [x] Continue Docker-backed verification: sign in through the authenticated
  browser profile and capture 400%/keyboard My work evidence.
- [x] Add dedicated Enrollment/My work 429 recovery copy, then continue
  remaining solution/OCI verification.
- [x] Review of `1fec6b3`: keep the replica-local limiter as MVP defense
  in depth, fail closed when configuration raises the frozen ceiling or
  shortens the window, and leave a shared gateway quota as an explicit
  residual.
- [x] External review of `d71ba10` approved the rate-limit closeout with
  no blocking code finding. Shared/gateway-enforced quota, GitHub CI, and
  broader independent slice review remain residuals.
- [x] Fix the `d71ba10` Implementation web job (typed catch variables and
  no synchronous setState in the My work effect), hide raw
  Organization/Enrollment locators in production chrome, and add
  in-shell Sign out. Live Playwright of the new chrome remains a residual.
- [x] Fix Implementation supply-chain on `626ea8d`: allowlist documented
  Enrollment synthetic `idempotency_key` fixture values so gitleaks no
  longer treats them as generic API keys.
- [x] Review of `626ea8d`: complete Sign out only after a successful
  local revoke, return a browser-safe `end_session_url`, and do not
  present CSRF/transport failure as logged out.
- [x] Review of `c7a96fa`: clear protected chrome when Sign out starts,
  keep it cleared on an unconfirmed logout, and re-bootstrap only after
  a known pre-revoke 400.

# Planned verification command set

- Focused core: `dotnet test --project
  tests/Submissions/FlexAgent.Submissions.Tests/FlexAgent.Submissions.Tests.csproj
  -c Release`.
- Boundaries/contracts: `dotnet test --project
  tests/Architecture/FlexAgent.Architecture.Tests/FlexAgent.Architecture.Tests.csproj
  -c Release`, `dotnet test --project
  tests/Contract/FlexAgent.Contract.Tests/FlexAgent.Contract.Tests.csproj -c
  Release`, and `pnpm --filter @flex-agent/contracts test`.
- PostgreSQL/runtime: run the Enrollment-focused filters in
  `tests/Integration/FlexAgent.Postgres.Integration.Tests` and
  `tests/Runtime/FlexAgent.Runtime.Tests`, then both complete projects in
  Release configuration. Record Docker/Testcontainers prerequisites and exact
  test counts.
- Web: `pnpm --filter @flex-agent/web test`, `pnpm --filter @flex-agent/web
  lint`, `pnpm --filter @flex-agent/web typecheck`, and `pnpm --filter
  @flex-agent/web build`.
- Authenticated product browser: `bash
  build/scripts/authenticated-browser-profile.sh`, followed by Playwright MCP
  interaction/accessibility/screenshot verification at
  `http://localhost:18080`; do not substitute the synthetic adapter or source
  inspection.
- Regression/release: `dotnet restore FlexAgent.slnx --locked-mode`, `dotnet
  test --solution FlexAgent.slnx -c Release`, `python3 scripts/check_docs.py`,
  `git diff --check`, `gitleaks detect --source . --config gitleaks.toml
  --no-banner --redact`, and applicable `pnpm verify:supply-chain`/`pnpm
  verify:oci` gates.

# Current state

This pass added replica-local Enrollment request limiting as defense in
depth. It does **not** close the gateway-wide bounded-quota contract:

- Authenticated Enrollment reads and mutations acquire a process-local
  fixed-window quota keyed by `(organization, actor, surface)`. Interim
  defaults are 60 reads and 20 mutations per 10-second window. Testing may
  lower those values. Configuration above the frozen ceilings or a window
  shorter than 10 seconds fails closed. Saturation of 10,000 live
  partitions fail-closes. Metrics/logs use only `read`/`mutation` and
  `permitted`/`limited`.
- HTTP 429/`enrollment.rate_limited`/`no-store` plus `Retry-After` is
  covered after two allowed My work reads when the test read quota is 2.
- Assignment mutation p95 is a 20-sample PostgreSQL gate against the
  approved 2-second `PROP-5` bound, after one activated Campaign seed.
- In-service Enrollment authorization p95 is a warmed 20-sample PostgreSQL
  grant lookup against the 50 ms `PROP-8` bound.
- Mutation telemetry records only allowlisted operation/outcome labels.

Confirmation pass 2026-08-23 before the 429-recovery commit: Enrollment
and My work web tests 12 passed; `pnpm --filter @flex-agent/web
typecheck` passed; `check_docs` and `git diff --check` passed. The task
stays **in-progress** for the gateway-wide quota residual, independent
slice review, and GitHub CI.

Follow-up 2026-08-23 after Docker 400% evidence: Enrollment and My work
now show recoverable **Too many requests** copy for `429` /
`enrollment.rate_limited` and keep the selected Participant on assign.
`dotnet test --solution FlexAgent.slnx -c Release` passed (1438 / 2
skipped). `bash build/scripts/verify-oci.sh` completed.

External review of `d71ba10` (2026-08-23): **approved**. No blocking
code finding. The `1fec6b3` findings are closed. Shared/gateway quota
remains an explicit residual. Implementation
[run 32590813013](https://github.com/trannamtrung1st/flex-agent/actions/runs/32590813013)
failed the web job: Enrollment `catch` callbacks needed `: unknown`,
and My work `setPending` ran synchronously inside `useEffect`. Those
lint failures are fixed. Chrome residuals continue: hide raw locators
and add in-shell Sign out. The task stays **in-progress**.

Review remediation 2026-08-23 for `1fec6b3`: constructor and
`ValidateOnStart` reject raised 60/20 ceilings or a window shorter than
10 seconds. Denied admissions return `Retry-After`. My work’s standalone
rate-limit panel now has **Try again**. The shared/gateway-enforced
quota is left open. Focused green: Enrollment HTTP + limiter 16;
Enrollment/My work web 12; `pnpm --filter @flex-agent/web typecheck`;
`check_docs`; `git diff --check`. Live 429/Try again was not recaptured:
the running authenticated profile predates this change, and hitting the
frozen 60-read quota would lock the synthetic Participant.

Docker follow-up 2026-08-23: `authenticated-browser-profile.sh status`
showed the loopback profile healthy (API, SPA, Keycloak, Postgres,
NGINX). Synthetic Participant login through the gateway completed PKCE
and returned to populated `/my-work`. 320×640 (400% reflow) list and
assignment detail were captured in light and dark, plus a Tab pass that
reached **Return to My work**. Raw Organization and Enrollment locators
remain visible in the shell/breadcrumb. The separate `verify-oci.sh`
image rebuild was not run.

Confirmation pass 2026-08-23 before commit: domain 43, architecture 41,
Enrollment HTTP plus limiter 14, Enrollment PostgreSQL 19, `check_docs`,
and `git diff --check` are green. Mutation p95 and warmed authorization
p95 remain asserted in `EnrollmentPersistenceTests`. The task stays
**in-progress** for authenticated 400% evidence, full solution/OCI gates,
GitHub CI on this SHA, and independent backend/frontend/security/QA
review of the broader slice. Cursor/security review of `888cd66` remains
approved.

External review of `d95f740` found an IdentityAccess ownership leak (Submissions
SQL against `application_sessions`), a display-profile lock gap, and silent
degradation on a mismatched `commitTransaction` handle. Those are remediated
in this pass. The task stays **in-progress**.

Review of `073e4f1` found no new material findings. The Enrollment
session/transaction remediation sequence is closed: owner-port
revalidation, assignment/lifecycle concurrency, revocation serialization,
database-clock expiry, replay-after-locked-read, pre-commit confirmation
on a required actor, in-memory rollback, required session port, and
independent shell destinations. Do not make further architectural changes
to this subsystem unless a new test exposes a concrete defect. The task
stays **in-progress** for remaining verification only.

Confirmed again on 2026-08-23 before the unsigned-cursor commit: domain 31,
Enrollment HTTP 6, and ProductionEnrollmentPage 4 still pass; live `/my-work`
still shows the assigned Enrollment. Focused green remains architecture 41
and Enrollment PostgreSQL 18 from the prior pass.
An overflow list cursor (`long.MaxValue` ticks) is now rejected as
`enrollment.invalid_field` instead of throwing. Live Participant My work
still shows the assigned Enrollment after reload.
In-memory concurrent snapshot isolation remains a known fake-only P3 and
is not treated as an MVP blocker. Shell bootstrap no longer applies
administrator MFA globally.
Home needs a valid current application session. Activities needs a qualifying
administrator grant and administrator MFA. My work needs
`assessment.assignment.discover` and the Participant authentication policy.
A dual-capability actor without administrator MFA receives HTTP 200 with
`activities.is_available = false` and `my-work.is_available = true`.
Administrator and reviewer list/read/mutate routes still return 403 without
MFA. Enrollment unit-of-work implementations confirm session liveness
immediately before commit after writes succeed; replay still confirms after
the locked Enrollment read.

Remediation now in tree:

- Assignment acquires an advisory lock keyed by
  `(organization, activity, participant)` before the live-row read. Unique
  index races translate to Deduplicated/Conflict instead of HTTP 500.
- Transactional Enrollment reads use `FOR UPDATE`. Zero-row optimistic
  updates throw `EnrollmentStaleRevisionException` and map to
  `enrollment.stale_revision`.
- `PostgresActivatedCohortBindingReader` recomputes Assessment
  `BaselineVerification` and assignment fails closed when degraded.
- Successful assignment required-durable audit uses the created Enrollment
  ID; pre-creation assignment denials still name the Cohort.
- The production client retains one idempotency key for a pending assign or
  lifecycle command. Lost-response retry reuses that key. Stale revision
  reloads the list and starts a new logical command.

What exists now: Submissions core/infrastructure, migration `0043`, owner
ports, production HTTP under `/v1/assessment`, Draft 2020-12 Enrollment
schemas plus OpenAPI/C#/TS projections, independent Home/Activities/My work
destinations, and a synthetic Participant seed. Session commit revalidation
is owned by IdentityAccess (`IApplicationSessionCommitPort`); Submissions
only adapts that port. Eligibility revalidation locks the display-profile
row. Owner ports fail closed on a non-null, non-`NpgsqlTransaction` handle.
The early session lock remains; IdentityAccess then confirms liveness with
`clock_timestamp()` immediately before Enrollment insert/update, again
immediately before unit-of-work commit, and after the locked Enrollment
read for replay disclosure.

Live evidence now covers the rebuilt profile at `http://localhost:18080`:
participant **My work** empty state at desktop light, dark, and narrow 390px.
A null-cursor PostgreSQL type error that returned HTTP 500 on first My work
read was fixed (`AfterTime`/`AfterId` now have explicit `DbType`s). Denied
Activities now stays inside the shell so Home/My work remain reachable.

The Playwright blocker is cleared without seeding Assessment tables: an
administrator created and activated a Campaign in the live profile, then
assigned, suspended, and restored Synthetic Participant. After application
and Keycloak logout, a Participant login from `/my-work` returned to
populated My work list/detail. Independent shell destinations held:
administrator saw Home/Activities; Participant saw Home/My work.

Unsigned Base64 `updatedAt:enrollmentId` cursors were still forgeable
after the first fail-closed pass. Review of that closeout required
scope-bound HMAC tokens. List tokens are now
`v1.{base64url(payload)}.{hmac}` where the payload binds query kind,
Organization, actor, Activity, Cohort, ticks, and Enrollment ID.
`EnrollmentQueryService` opens the token against the current actor/scope
before the store sees `afterTime`/`afterId`. Empty cursor remains first
page. Forged restart tuples, stolen cross-actor tokens, wrong query kind,
one-bit HMAC flips, overflow ticks, and overlong tokens fail as
`enrollment.invalid_field`. HTTP rejects `limit=0`, `limit=999999`,
unparsable limit, and overlong cursor with 400/`no-store` instead of
coercing to the default page size. Candidate `q` over
`MaximumQueryPrefixLength` is also 400.

Cursor HMAC keys are shared across API replicas via
`enrollment-cursor-{keyId}` or `Enrollment:CursorSigning` current/previous
materials. Production/Staging fail closed without a key. SQL filters
remain the disclosure boundary.

A single PostgreSQL assignment smoke used `Stopwatch` and finished under
2 s. That does **not** close representative synchronous Enrollment
mutation p95 ≤ 2 seconds.

What remains: representative mutation p95 (multi-sample), per-actor/
Organization rate limits, measured 50 ms authorization p95, 400%
zoom/keyboard matrix, full `FlexAgent.slnx` and OCI/SBOM gates,
independently reproduced CI on this SHA, and independent
backend/frontend/security review.

Confirmation pass 2026-08-23 before commit: HMAC tokens bind query
kind/org/actor/activity/cohort; stores consume `afterTime`/`afterId` only;
HTTP rejects out-of-range, unparsable, and overlong list query values
with 400/`no-store`; assignment timing remains a single-sample smoke.
Local green: domain 35, Enrollment HTTP 10, architecture 41, Enrollment
PostgreSQL 18, `git diff --check` clean. No GitHub CI on this SHA.

Review of `5c89aec` required a shared replica key and candidate cursor
binding. Tokens are now `v1.{keyId}.{payload}.{hmac}`. The host loads
current and optional previous materials from `enrollment-cursor-{id}` or
`Enrollment:CursorSigning:Keys:{id}`; Production/Staging fail closed
without them. Same-key replica verify and different-key rejection are
tested; the previous key still opens tokens after rotation. Participant
options use `queryKind=participant-options` plus normalized prefix;
IdentityAccess/PostgreSQL receive only `afterActorId`. Malformed or raw
UUID candidate cursors fail as `enrollment.invalid_field`. Local green
after this pass: domain 39, Enrollment HTTP 10, architecture 41,
Enrollment PostgreSQL 18. No GitHub CI on this working tree.

Confirmation pass 2026-08-23 before commit: no per-process random signer;
replicas with the same configured key verify each other's tokens; a
different key and a retired previous-only signer fail closed; candidate
lists open HMAC scope and pass only `afterActorId` to IdentityAccess.
Local green: domain 39, Enrollment HTTP 10, architecture 41, Enrollment
PostgreSQL 18, `git diff --check` clean. No GitHub CI on this SHA.

Review of `a4aadf3` found that a 64-character Unicode prefix could issue a
cursor longer than 512 characters, and that short secrets were hashed to
32 bytes instead of rejected. The payload now binds `SHA-256(normalized
prefix)` so a maximum-length `é` prefix still pages. Cursor secrets must
decode from Base64/Base64URL to at least 32 bytes.

Confirmation pass 2026-08-23 before commit: a 64-character `é` candidate
prefix issues a cursor at or below 512 characters and the next page opens
it; `"password"` is rejected as signing material; replica and rotation
tests use decoded 32-byte fixtures. Local green: domain 41, Enrollment
HTTP 10, architecture 41, Enrollment PostgreSQL 18, `git diff --check`
clean. No GitHub CI on this SHA.

External review of `888cd66` (2026-08-23): **approved**. No blocking code
finding. Prefix digest size, ≥32-byte decoded signing secrets, current/
previous replica keys, and candidate `afterActorId` binding close the
cursor/security thread. Compatibility note accepted without a format bump
or dual-read window because `a4aadf3`–`888cd66` are an unreleased
remediation sequence; a released mixed-version rollout would need a new
token version or temporary dual accept. Focused green remains local
(domain 41, Enrollment HTTP 10, architecture 41, Enrollment PostgreSQL
18). No GitHub CI on that SHA. The Enrollment task stays open for
mutation p95, authorization p95, per-actor rate limits, 400%
accessibility, full CI/OCI, and remaining independent review.

# Decisions

- Use the architecture-approved `Submissions` module name even though this
  first slice implements only Enrollment. Do not create empty Submission or
  Attempt layers to fill the future namespace.
- Split core and infrastructure from the first behavior because protected
  persistence, cross-module transaction participation, and architecture tests
  satisfy the repository's split conditions.
- Keep IdentityAccess and Assessment state behind owner-controlled ports. A
  database foreign key may reinforce an invariant, but it does not authorize
  Submissions repositories to query another module's tables.
- Use a named Submissions-owned coordinator for the Enrollment transaction,
  following the approved modular-monolith transaction pattern. No new ADR is
  expected unless implementation requires ownership or failure semantics beyond
  those already approved.
- Use a minimal Organization-scoped display profile and eligibility action for
  pre-provisioned human actors. This is not a general Participant directory,
  identity creation workflow, or authorization derived from an OIDC display
  claim.
- Allow one live Enrollment per Participant/Activity in this slice. Exact
  same-Cohort assignment is idempotent while live; another live Cohort is a
  conflict. After `Closed` or `Revoked`, a later authorized assignment creates a
  new Enrollment identity even for the same Cohort. Terminal history is never
  repurposed.
- Keep Suspended visible to the owning Participant with no new protected action
  so the UI can state the current restriction without showing stale `Active`.
  Closed and Revoked are absent from current **My work** and protected detail;
  authorized administrators retain minimized history.
- Return state and Participant visibility separately so later policy can narrow
  visibility without redefining Enrollment status. The initial mapping is fixed
  in the state/visibility table and verified on both server and client.
- Use only transition-specific bounded reason categories in this slice; reject
  free-form reason text to keep audit, logs, and error paths minimized.
- Apply the route-level ADR-003 audit classes in the authorization matrix:
  mutation=`required_durable`, candidate/admin sensitive read and security denial
  =`bufferable`, minimized own/shell read=`operational_sample`. Do not let a
  browser request select or downgrade the class.
- Use in-product discovery only. Do not add a placeholder external notification
  provider or imply delivery/authentication from an outbox event.
- Treat the initial `Active` Enrollment event/outbox fact as the minimized
  in-product availability record required by traceability. Authoritative **My
  work** reads still derive from current Enrollment state and authorization; the
  event is not delivery proof, a grant, or an eventually consistent read source.
- Keep public routes under the current `/v1/assessment` production gateway for
  journey continuity while domain ownership remains in Submissions.
- Replace action-prefix relationship inference for new Enrollment/Assignment
  operations with exact operation-specific authorization and authentication
  strength. A Participant grant must never be treated as Reviewer authority or
  trigger Reviewer MFA accidentally, and a dual-capability actor receives no
  role shortcut.
- Use the existing Organization-scoped exact action grants as the initial
  delegated administrative scope permitted by `REQ-AUTH-10`; always narrow them
  through the trusted Activity/Cohort/Enrollment parent chain. Do not invent an
  Activity-specific grant store in this slice, and do not treat Organization
  membership without the exact action as authority.
- Commit canonical JSON Schemas as wire authority and treat C#, OpenAPI, and
  browser types as tested projections, consistent with ADR-010.
- Use authoritative reads rather than an eventually consistent protected
  projection for this first slice. If a cache is later justified, its scope and
  revocation behavior require explicit tests before admission.
- Expose an owner-controlled current Enrollment decision contract for future
  Submission/Attempt consumers, but do not claim `AC-SUBM-4` end-to-end until
  those consumers enforce it. This task verifies immediate denial at the
  Enrollment boundary and participant/admin projections only.
- Enrollment request limits are an interim operational default, not a new
  approved `PROP-*`. Authenticated reads allow 60 permits and mutations
  allow 20 permits per 10-second window per `(organization, actor)`.
  Surfaces are independent. Unauthenticated requests return 401 before a
  quota is consumed. The process-local limiter is MVP defense in depth,
  not the gateway-wide contract: each API replica has independent
  in-memory partitions, so effective capacity scales with process count.
  A shared/gateway-enforced quota remains an explicit residual. Labels
  never include actor, Organization, Enrollment, or Participant
  identifiers. Deployments may only lower the frozen 60/20 ceilings or
  lengthen the 10-second window. Values above those ceilings or a shorter
  window fail closed at startup.
- Authenticate Enrollment, My work, and participant-options list cursors
  with HMAC-SHA256 over a scope-bound payload (query kind, Organization,
  actor, Activity, Cohort, SHA-256 digest of the normalized prefix, ticks
  or after-actor). Tokens include a key ID. Replicas share
  `Enrollment:CursorSigning` current and optional previous materials from
  mounted secrets (`enrollment-cursor-{id}`) or configuration. Secrets must
  be Base64/Base64URL of at least 32 random bytes; shorter values are
  rejected. Production/Staging fail closed without a key. Development and
  Testing may derive a deterministic 32-byte non-production key so local
  replicas stay consistent. Rotation accepts the previous key until it is
  removed. Do not dual-read the pre-`888cd66` `v1` payload or plain-text
  secrets: those remediations were unreleased. If that format is later found
  in a deployed environment, bump the token version or add a temporary
  dual-accept window before mixed-version rollout. This is not a new ADR.

# Findings / deviations

- Review of `626ea8d`: request changes. P1 — Sign out ignored
  `/auth/logout` status and swallowed transport errors, then cleared
  chrome and navigated home. Provider `EndSessionEndpoint` 302s were
  not followed as top-level navigation. Remediation: successful logout
  returns JSON `{ logged_out, end_session_url }`; the client navigates
  only after `logged_out: true`, using an `https` end-session URL when
  present. CSRF 400 and transport failure keep the session chrome and
  announce **Sign out could not be completed.**
- Review of `c7a96fa`: P1 closed (confirmed `logged_out: true` and
  top-level provider navigation). P2 follow-up — a dropped logout
  response could leave Assignment/Enrollment chrome visible after the
  server had already revoked. Sign out now clears protected state
  immediately (`signing-out`), navigates only after a confirmed revoke,
  keeps a cleared **Sign out status could not be confirmed. Try again.**
  panel when the result is ambiguous, and re-bootstraps `/auth/session`
  plus shell after a known HTTP 400. `ProductionApiError` lives in
  `production-api-error.ts` to break the logout/API import cycle.
- Review of `1f698a5`: approved. Narrow gitleaks allowlist only.
- Implementation run [32610425519](https://github.com/trannamtrung1st/flex-agent/actions/runs/32610425519)
  on `626ea8d`: web, dotnet, and OCI passed; supply-chain failed at
  Secret scan. Gitleaks flagged historical Enrollment fixture keys
  `enr-assign-synthetic-0001` and `enr-suspend-synthetic-0001` as
  `generic-api-key`. Those values are documented synthetic contract
  fixtures, not credentials. The project allowlist now includes them
  beside `idem-synthetic-\d{4}`. Local `gitleaks detect --source . --config
  gitleaks.toml --no-banner --redact` reports no leaks.
- Review of `d71ba10`: approved. No blocking code finding. The two
  findings against `1fec6b3` are closed. Shared/gateway-wide per-actor
  quota stays an explicit product/operations residual. Immediate **Try
  again** without a `Retry-After` countdown is a non-blocking UX note.
  Implementation run [32590813013](https://github.com/trannamtrung1st/flex-agent/actions/runs/32590813013)
  failed the web job on ESLint (`catch` without `: unknown`, and
  `setPending` inside the My work effect).
- Review of `1fec6b3`: request changes. P1 — do not mark the
  gateway/per-actor quota closed while the limiter is replica-local
  in-memory state. P2 — `Enrollment:RequestLimits` accepted values above
  the frozen 60/20/≥10s ceiling. Remediation keeps the local limiter,
  fail-closes raised ceilings or a shorter window, returns `Retry-After`
  on 429, and adds **Try again** on the My work rate-limit panel. The
  shared/gateway-enforced quota stays an explicit residual.
- Follow-up 2026-08-23: 429 recovery copy, full `FlexAgent.slnx`, and
  local OCI verification are closed. Raw Organization/Enrollment
  locators, shared/gateway rate-limit quota, and independent slice review
  remain.
- Review of `888cd66`: approved. No blocking defect. Cursor/security
  remediations from the prior reviews are closed. Compatibility of the
  changed `v1` payload and Base64 secret encoding is a non-blocking note
  for this unreleased sequence. Residuals remain operational/verification
  only.
- Review of `a4aadf3`: embedding the Base64 prefix made a valid 64-character
  Unicode search issue an overlong `next_cursor`, and `Materialize`
  accepted `"password"` by hashing it. Prefix digest binding and
  decoded-length ≥32 enforcement close those contracts. Remaining
  residuals: mutation p95, per-actor rate limit, 50 ms authorization p95,
  400% zoom, no GitHub CI on this SHA, and independent review.
- Review of `5c89aec`: a per-process random HMAC key would reject valid
  `next_cursor` values across horizontally scaled API replicas, and
  participant-options still forwarded raw UUID cursors. Shared
  current/previous keys and candidate HMAC binding close those contracts.
  Remaining residuals: mutation p95, per-actor rate limit, 50 ms
  authorization p95, 400% zoom, no GitHub CI on this SHA, and independent
  review.
- Review of the unsigned-cursor closeout: a syntactically valid
  `updatedAt:enrollmentId` Base64 token was still accepted, so a caller
  could restart pagination or replay another actor/cohort cursor. HMAC
  scope binding plus same-scope/cross-scope/one-bit tests close that
  contract. HTTP now returns 400/`no-store` for out-of-range and
  unparsable limits and overlong cursors instead of coercing to the
  default page size. A one-sample assignment timing check must not close
  p95. Candidate-list IdentityAccess Guid cursors still fail as an empty
  page rather than `enrollment.invalid_field`. No GitHub CI status is
  attached to this working tree. Remaining residuals: mutation p95,
  per-actor rate limit, 50 ms authorization p95, 400% zoom, raw UUID
  breadcrumbs, no in-shell sign-out, and independent review.
- Consistency review 2026-08-23: focused suites remain green. Overflow
  cursor ticks are rejected. Candidate-list tamper still returns an empty
  page (IdentityAccess Guid cursor) rather than `enrollment.invalid_field`.
  The running SPA image still has the pre-fix suspend sentence until
  rebuild. Remaining residuals: raw UUID breadcrumbs, no in-shell sign-out,
  no per-actor rate limit, no 50 ms authorization p95, no 400% zoom, and
  no independent review.
- Self-review 2026-08-23: tampered list cursors previously treated parse
  failure as a first page. Fixed at the query boundary. Suspend confirmation
  previously claimed terminal My work removal; copy now distinguishes
  suspend/restore from close/revoke. Remaining residuals: raw UUID
  breadcrumbs, no in-shell sign-out, cramped desktop assign control row,
  no per-actor rate limit, no 50 ms authorization p95, no 400% zoom
  evidence, and no independent review.
- Review of `073e4f1`: no new material findings. Session/transaction
  architecture is complete for this slice. Remaining work is Playwright,
  HTTP-negative, latency, full solution/OCI, and independent review.
- Review of `5e76cc8`: in-memory rollback and required session port are
  correct, but `CommitSessionActor` remained nullable and publicly
  settable, so a future `ExecuteAsync` caller could skip pre-commit
  confirmation. Remediation 8 makes the actor a required unit-of-work
  argument and removes the mutable transaction property. Concurrent
  in-memory snapshot clobber is a known fake-only P3; production
  PostgreSQL plus deny-by-default in-memory host composition keep it
  out of MVP scope.
- Review of `f753b7e`: production PostgreSQL pre-commit and independent
  shell destinations are correct. The in-memory unit of work reported
  denied but left Enrollment/event/operation/audit mutations in place, and
  both unit-of-work constructors accepted a null session port. Remediation
  7 snapshots those fakes and restores them on pre-commit failure, and
  requires `IEnrollmentSessionPort`.
- Review of the `cd9a799 → e1b8954` delta: replay confirmation after the
  locked Enrollment read is correct. The CI follow-up that 403'd the entire
  shell on administrator MFA was inconsistent with independent Home /
  Activities / My work evaluation. Remediation 6 removes that global gate
  and adds a unit-of-work pre-commit `ConfirmLiveAsync` so time-based
  expiry cannot commit after later audit/outbox/idempotency writes.
- Review of `f1a6b44`: concurrent assignment could 500 on the live unique
  index; concurrent lifecycle could 500 on a zero-row revision update;
  production binding snapshots hardcoded `VerificationDegraded = false`;
  successful assignment audit used the Cohort ID under the Enrollment
  resource type; the browser generated a new idempotency key on every
  retry. Fixed in this pass.
- First live **My work** list returned HTTP 500: PostgreSQL could not type
  null `@AfterTime`/`@AfterId` cursor parameters. Fixed with explicit
  `DbType.DateTimeOffset` and `DbType.Guid`. Empty list now renders.
- Participant destination denial previously replaced the entire shell, so a
  Participant who landed on `/activities` could not reach **My work**. Denied
  destinations now render inside the shell with a Home recovery link.
- Login from `/my-work` returned to `/my-work` for the Participant. Login from
  `/` as an administrator still lands on Activities when that destination is
  available; that is the existing administrator default, not a Participant leak.
- The current production setup deliberately says **Assign Participants** is
  omitted; the approved UI journey requires that action once a separately
  authorized production Enrollment destination exists.
- `src/Modules/Submissions/` and production Enrollment/**My work** endpoints do
  not exist. Synthetic pages under `/browser` include broader fake behavior and
  are not suitable implementation authority.
- IdentityAccess currently owns actors/bindings/grants but no permitted display
  profile for a scoped Participant selector. This plan adds the smallest
  owner-controlled profile surface rather than inventing a general directory.
- The authenticated browser seed has an administrator actor but needs a stable
  application-side Participant actor/binding/profile/grant set before the
  production Participant journey can be verified.
- The production shell currently derives both Home and Activities availability
  from any permitted Assessment action. That must be corrected before adding a
  Participant-only discovery grant, or Participant navigation could expose an
  unrelated administrator destination.
- The same shell evaluates `assessment.activity.read` and the Assessment
  relationship resolver classifies every non-administrator `assessment.*`
  grant as Reviewer. Reusing it for `assessment.assignment.discover` would deny
  ordinary Participants or impose Reviewer MFA. The plan now requires
  operation-specific Participant authorization and an administrator-independent
  shell bootstrap.
- The first plan draft proposed permanent Participant/Cohort uniqueness while
  also promising a new relationship after terminal state. The persistence
  contract now uses live-only Participant/Activity uniqueness so reassignment
  cannot rewrite history and is not blocked after `Closed`/`Revoked`.
- Existing Assessment tables prove Activity/Cohort and Activity/baseline
  separately; an Enrollment foreign key must target an exact owner-approved
  Cohort/baseline binding, not infer that the two references belong together.
- Existing protected production APIs do not explicitly emit `Cache-Control:
  no-store`, and the production client clears all protected shell state on 401
  but not on 403 or delayed request completion. The plan now includes response
  caching and late-response invalidation tests before Participant data is added.
- Enrollment/history lifecycle has no independent approved duration. Creation
  must bind an applicable lifecycle-policy reference and preserve minimized
  history rather than inventing a cleanup duration; idempotency records use the
  approved 90-day default without weakening duplicate protection.
- No unresolved product question blocks this slice. Any newly discovered
  semantic conflict must be promoted to the applicable authoritative spec with
  an interim default/`Proposed` item before implementation proceeds.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Governing product/requirements/UI/architecture/design-system sources reconciled | passed | Scope and traceability above bind the first implementation row in `submission-attempts.md` and the `JRN-MVP-2` handoff without pulling in later Submission/Attempt behavior. |
| Current repository seam review | passed | Assessment provides activated Cohort/baseline/task facts; IdentityAccess provides actors/sessions/grants; migration head is `0042`; production router has Activities/setup only; `/browser` has synthetic Enrollment/My-work pages. |
| Independent backend/frontend/security plan review | passed | Second review corrected terminal-reassignment uniqueness, exact Cohort/baseline binding, shell/Participant MFA classification, protected response races/caching, wire-schema authority, and lifecycle-policy linkage before implementation. |
| Business-analysis and architecture readiness review | passed | Confirmed the bounded first traceability slice, explicit downstream AC-SUBM-4 gap, new-after-terminal semantics, module/transaction ownership, authoritative schema projections, exact action/resource matrix, latency objectives, and no new external system. |
| `python3 scripts/check_docs.py` | passed | Documentation validation passed on 2026-08-22. |
| whitespace/diff validation | passed | `git diff --check` passed; direct `git diff --no-index --check` on the untracked task file produced no whitespace diagnostics (its status `1` is the expected no-index difference result). |
| Secret scan | passed | `gitleaks detect --source . --config gitleaks.toml --no-banner --redact` found no leaks during the readiness review. |
| Focused Submissions domain tests | passed | `dotnet test --project tests/Submissions/FlexAgent.Submissions.Tests/FlexAgent.Submissions.Tests.csproj -c Release` — 43 passed, including percentile rank and allowlisted mutation telemetry. |
| Architecture/contract tests | passed | Architecture 41 passed. This pass did not change schemas. |
| PostgreSQL migration/isolation/concurrency/fault tests | passed | Included in full `FlexAgent.slnx` Release: 1438 passed, 2 skipped. Enrollment p95 cases remain in that suite. |
| Runtime/API authorization and HTTP-negative tests | passed | Enrollment HTTP + limiter 16. Covers CSRF, missing session, malformed/overlong/unparsable list query values, guessed detail, unknown member, oversized body, 429 + `Retry-After` after a lowered read quota, and fail-closed raised ceilings / shortened window. |
| React component/accessibility tests | passed | Enrollment/My work 429 recovery: client maps `enrollment.rate_limited`; My work heading **Too many requests** plus **Try again** reloads the list; assign keeps the selected Participant. `pnpm --filter @flex-agent/web typecheck` passed. |
| Authenticated Playwright MCP desktop/narrow/both-theme evidence | passed | Docker profile at `http://localhost:18080`. Participant login returned populated My work. Desktop list: `.playwright-mcp/page-2026-08-22T18-07-50-907Z.png`. 320×640 list: `.playwright-mcp/page-2026-08-22T18-08-02-305Z.png`. 320 assignment detail: `.playwright-mcp/page-2026-08-22T18-08-23-633Z.png`. 320 focus on Return to My work: `.playwright-mcp/page-2026-08-22T18-08-52-320Z.png`. 320 dark detail: `.playwright-mcp/page-2026-08-22T18-09-13-410Z.png`. Desktop dark detail: `.playwright-mcp/page-2026-08-22T18-09-30-514Z.png`. Visual: hierarchy and next action are clear; no overflow of primary copy at 320; shell still shows raw Organization/Enrollment locators. |
| Full regression, security, supply-chain, and performance gates | passed | `dotnet test --solution FlexAgent.slnx -c Release` passed (1438 / 2 skipped). `bash build/scripts/verify-oci.sh` completed locally, including health probes and SBOM/vulnerability scan. |
| Cursor/security remediations review (`888cd66`) | passed | External review approved with no blocking code finding. Prefix digest, ≥32-byte decoded keys, replica/rotation, and candidate `afterActorId` binding accepted. GitHub CI still absent for that SHA. |
| Rate-limit closeout review (`d71ba10`) | passed | External review approved with no blocking code finding. Shared/gateway quota remains a residual. Immediate Try-again countdown is a non-blocking UX note. |
| GitHub Implementation (`d71ba10`, run 32590813013) | failed locally remediated | Web job failed ESLint: implicit `catch` types and `setState` in the My work effect. Confirmation pass: `pnpm lint` 0 errors, focused web tests 20, typecheck, `check_docs`, `git diff --check`. Live Sign out / locator-free chrome not recaptured. |
| GitHub Implementation (`626ea8d`, run 32610425519) | failed locally remediated | Web, dotnet, and OCI passed. Supply-chain Secret scan failed on Enrollment fixture idempotency keys. `gitleaks.toml` now allowlists `enr-assign-synthetic-` / `enr-suspend-synthetic-` `\d{4}` under `contracts/fixtures/`. Local gitleaks: no leaks. |
| Sign-out completion (`626ea8d` P1) | passed locally | Auth HTTP 10: antiforgery 400 leaves the session; success returns `logged_out` / nullable `end_session_url` / `no-store` without a 302. Web logout tests cover local `/`, HTTPS provider URL, CSRF 400, transport failure, and rejected `javascript:` next locations. Live Playwright of Sign out not recaptured. |
| Sign-out ambiguous response (`c7a96fa` P2) | passed locally | Focused web: `production-logout.test.ts`, `production-routes.test.tsx`, `production-api.test.tsx` — 16 passed. Typecheck and `pnpm lint` (0 errors). Transport-lost logout clears Assignment content, stays on a Signing-out panel with unconfirmed copy + **Try again**, and does not `location.assign`. Known 400 re-bootstraps and restores Sign out without the unconfirmed alert. Live Playwright of Sign out still not recaptured (Docker SPA images not rebuilt for this chrome). |

# Blockers

- Shared/gateway-enforced per-actor/Organization Enrollment request
  limits remain open. The API-process limiter is defense in depth only.
- Independent backend/frontend/security/QA review of the broader
  Enrollment slice is still required before this task can be marked
  completed. GitHub CI is not attached to this working tree.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [ ] Independent backend, frontend, security/privacy, and QA review findings are resolved or explicitly accepted by the authorized owner
- [ ] Task state is safe and complete for external review
