---
id: p0-participant-timing-accommodations
status: in-progress
created: 2026-08-23
updated: 2026-08-24
predecessors:
  - p0-assessment-setup-cohort-activation
  - p0-enrollment-assignment-discovery
---

# Goal

Implement the next bounded P0 Submission/Attempt slice: derive each enrolled
Participant's server-authoritative Submission and Attempt-start windows from
the verified frozen Cohort baseline, authoritative Enrollment state, current
server time, and any currently effective permitted accommodation.

Add immutable, policy-bounded, authorization-scoped, durably audited
accommodation records and the approved fairness-exception approval interaction
without changing the activated Cohort baseline. Show exact effective timing and
the named Campaign timezone to authorized administrators and the owning
Participant through the production Enrollment and **My work** surfaces.

This task stops before Submission intake, Attempt entitlement/consumption,
retry entitlements, ADR-005 Session start, or live Session interaction. It
establishes the timing and accommodation authority those successor slices must
consume.

# Governing sources

- `AGENTS.md`, `.work/README.md`, and the `implementation-workflow`,
  `business-analyst`, `developer`, backend/frontend, UI/UX, and
  security/privacy repository skills.
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — Organization precedence, immutable Cohort
  fairness, P0 workflow order, and hosted Session prerequisites.
- `docs/requirements/features/submission-attempts.md` — primary behavior,
  including the approved 2026-08-23 Accommodation amendment:
  `REQ-SUBM-9`–`REQ-SUBM-14`, accommodation portions of
  `REQ-SUBM-23`, `REQ-SUBM-36`–`REQ-SUBM-42`, `AC-SUBM-11`,
  `AC-SUBM-12`, `AC-SUBM-22`, applicable parts of
  `AC-SUBM-23`–`AC-SUBM-25`, `AC-SUBM-27`–`AC-SUBM-28`,
  `REQ-SUBM-50`–`REQ-SUBM-56`, `AC-SUBM-33`–`AC-SUBM-39`, and
  approved `PROP-3`, `PROP-5`, `PROP-7`, and `PROP-9`–`PROP-15`.
  `PROP-5` governs the
  Attempt-eligibility/effective-timing read, not accommodation mutation
  latency.
- `docs/requirements/features/assessment-setup.md` — immutable timing source
  and fairness boundary: `REQ-ACT-16`, `REQ-ACT-23`, `REQ-ACT-25`,
  `REQ-ACT-31`, `REQ-ACT-32`, `REQ-ACT-42`, `AC-ACT-19`, and
  `AC-ACT-25`.
- `docs/requirements/features/auth-resource-isolation.md` and
  `docs/requirements/mvp-operational-defaults.md` — current
  action/resource authorization, application-session freshness, commit-time
  reauthorization, non-disclosure, audit durability, lifecycle policy, and
  latency expectations.
- `docs/ui-ux/submission-attempt.md` — approved effective-rules,
  accommodation, fairness-exception, **My work**, timing, protected-state,
  keyboard, announcement, and responsive behavior; especially
  `UI-SUBM-DEC-1`, `UI-SUBM-DEC-6`, and `UI-SUBM-DEC-10`.
- `docs/ui-ux/design-system/README.md` and
  `docs/ui-ux/design-system/implementation-guide.md`, using the applicable
  accessibility, color, typography, layout, density, interaction, motion,
  status, button, input, selection, alert, badge, panel, list, modal, error
  summary, responsive content, empty/loading, protected-content, technical
  metadata, and timeline modules.
- `docs/architecture/mvp-architecture.md`,
  `docs/architecture/backend-module-architecture.md`, ADR-002, ADR-003,
  ADR-004, and ADR-017 — modular-monolith ownership,
  Assessment/Submissions/Configuration collaboration, trusted timing, exact
  authorization, current policy resolution, durable audit/outbox, immutable
  baseline behavior, and Configuration-owned policy source authority.
- Predecessor task files
  `.work/active/p0-assessment-setup-cohort-activation.md` (completed) and
  `.work/active/p0-enrollment-assignment-discovery.md` (implementation and
  verification present; closeout review still in progress).

# Scope

## In

- Reconcile the migration head, canonical catalog, activated-Cohort reader,
  Enrollment ports, API composition, production React routes, tests, and
  predecessor evidence before behavior changes.
- Define a transport-independent effective-timing model that keeps immutable
  baseline timing, current non-widening Organization policy, effective
  Submission window, effective Attempt-start window, exclusive cutoffs,
  per-Attempt duration, authoritative evaluation time, current eligibility
  reason, and accommodation provenance distinct.
- Derive timing only from a verified activated-Cohort binding, exact frozen
  baseline, active Enrollment, current Organization policy, server time, and
  current eligible accommodation. Current policy may narrow a previously
  permitted value without rewriting the baseline or history. Missing,
  degraded, inconsistent, cross-scope, stale, or unsupported policy fails
  closed.
- Add the approved versioned normalized accommodation-policy contract. It must
  include exact current policy
  identity, frozen baseline policy identity, supported dimensions, bounds,
  reason categories, approval/separation rule, and effective/expiry
  constraints. Resolve the accommodation record class through the independent
  versioned lifecycle-policy boundary rather than embedding a retention choice
  in the accommodation rule.
- Add Submissions-owned accommodation behavior for request/grant,
  approval-required, approve, reject, revoke, supersede, derived expiration,
  current-policy invalidation, and current-effect selection while preserving
  prior facts and the Cohort baseline. Do not create an expiry mutation or
  background job unless the approved contract requires materialized expiry;
  server-time selection must exclude an expired record immediately.
- Keep a policy-bounded accommodation distinct from a fairness exception. An
  exception does not affect timing until current additional authorization,
  required approval, unchanged request/policy revalidation, and durable audit
  acceptance all commit.
- Require exact action/resource authorization at admission and current actor,
  application-session, grant, Enrollment, baseline, policy, and approval-state
  revalidation immediately before protected commit or disclosure.
- Make retryable accommodation and decision commands idempotent and
  concurrency-safe. Mismatched keys, stale revisions, lost responses,
  competing decisions, expired requests, or policy changes reconcile without
  duplicate or widened effect.
- Add an additive PostgreSQL migration after the implementation-time migration
  head for Organization-scoped accommodation state, append-only facts/decisions,
  idempotency outcomes, immutable parent/policy bindings, UTC times, and
  database-enforced scope and uniqueness.
- Add canonical Draft 2020-12 effective-timing and accommodation schemas and
  fixtures, then apply an explicit compatibility rollout for enclosing
  Enrollment and **My work** projections. Update the catalog, OpenAPI, C#, and
  TypeScript projections together. Do not silently reinterpret existing v1
  baseline timing fields or add unknown properties to a strict v1 schema.
- Extend administrator Enrollment detail with baseline/effective timing,
  current accommodation, bounded request/confirmation, approval-required,
  separately authorized approve/reject, revoke, stale, uncertain/reconciling,
  audit-failure, and history states.
- Extend Participant **My work** list/detail with the exact effective boundary
  in the named Campaign timezone and a minimized accommodation consequence.
  Participants cannot request, select, edit, or infer policy bounds.
- Preserve protected-state clearing on logout, actor/Organization change,
  permission or Enrollment visibility loss, degraded baseline, and stale
  responses. The browser never calculates authority from its clock.
- Add bounded telemetry using allowlisted operation/outcome labels only.
  Exclude protected identifiers, reasons, and values from metric dimensions and
  ordinary logs.
- Update authoritative implementation-status/traceability rows only after
  repeatable implementation and review evidence exists.
- Run independent backend, frontend, security/privacy, and QA review after
  implementation.

## Out

- Submission intake, upload/quarantine/scanning, immutable Submission versions,
  preview/download, object storage, or material-category capability checks.
- Attempt ordinal/entitlement calculation, retry entitlement, Attempt start or
  consumption, resolved Session configuration, execution manifest, exact
  Submission binding, ADR-005 composition, or Session handoff.
- Session duration enforcement, Session timers, conversation, voice, tools,
  memory, Evaluation, review/Release, or external notifications.
- Editing an Activity, Cohort, activated baseline, Task, timing rule, or
  Organization policy through the accommodation surface.
- Participant self-service accommodation changes, bulk operations, a general
  governance queue, or a general Organization-policy editor.
- Any positive dimension, bound, reason, or approval rule invented from test
  data, UI convenience, or the current opaque development policy fixture.
- Cross-module SQL, a new network service, queue-first consistency, cache
  authority, or a new ADR unless a genuinely new durable decision is found.
- Commits, pushes, pull requests, deployment, or release unless separately
  requested.

# Acceptance and traceability matrix

| Governing behavior | Implementation surfaces | Required evidence |
| --- | --- | --- |
| Effective timing comes from the exact verified baseline, active Enrollment, current non-widening Organization policy, server time, and current permitted accommodation; the Cohort baseline never changes (`REQ-SUBM-9`, `REQ-SUBM-13`, `REQ-ACT-23`, `REQ-ACT-25`, `AC-ACT-19`, timing-policy narrowing edge case) | Submissions timing domain/service, Assessment activated-binding port, Configuration current-policy port, Enrollment decision, clock boundary, scoped query projections | Baseline-only/accommodated/too-early/expired/unavailable domain cases; current-policy narrowing after grant; PostgreSQL wrong-parent/degraded-baseline/stale-policy/clock-boundary tests; no baseline update |
| UTC persistence and named-timezone interpretation remain explicit; start and Submission cutoffs are exclusive (`REQ-SUBM-10`, `AC-SUBM-22`, `PROP-3`, `UI-SUBM-DEC-6`) | UTC value objects, database constraints, canonical DTO, resilient browser formatter, participant/admin projections | Exact instant, before/at/after cutoff, DST gap/fold, unsupported browser timezone, client-clock substitution, and desktop/narrow display evidence; no false local conversion |
| Accommodation is policy-enabled, bounded, reason-coded, immutable in parent/provenance, and current only while effective under both recorded and current policy (`REQ-SUBM-11`–`REQ-SUBM-14`, `AC-SUBM-11`, `AC-SUBM-12`) | Normalized policy ports, accommodation aggregate/state machine, coordinator, PostgreSQL records/events/operations | Supported/unknown dimension, inside/outside bounds, frozen/current-policy mismatch, current narrowing, effective/expiry, revoke/supersede, wrong baseline, duplicate/concurrent, and immutable-history tests |
| Fairness exception requires a different current authorized approver and cannot self-activate, alter the requested decision, or widen a non-bypassable boundary (`REQ-ACT-42`, `AC-ACT-25`, `REQ-SUBM-54`, `UI-SUBM-DEC-10`) | Request/decision commands, approval action, requester/approver rule, expected revision, reconciliation, Enrollment UI | Wrong actor/scope, self-approval, edit/widen attempt, stale/expired request, decision race, lost response, and absent-approver tests |
| Mutations and protected reads enforce current authorization and required durable audit (`REQ-SUBM-36`–`REQ-SUBM-42`, `PROP-7`, applicable `AC-SUBM-19`, `AC-SUBM-24`, `AC-SUBM-25`) | IdentityAccess session/authorization ports, Submissions transaction, audit/outbox, scoped repositories, thin HTTP mapping | Wrong Organization/activity/cohort/Enrollment/Participant, guessed ID, forged parent/action, stale grant/session, commit-time revocation, list/count/history, audit rollback, append-only, and redaction tests |
| Administrator and Participant views show only current permitted timing, actions, and fairness consequence (`AC-SUBM-23` and approved UI contract) | Enrollment detail, My work list/detail, versioned browser DTOs, accessible form/dialog/status/history | Loading, baseline-only, active accommodation, approval required, approved/rejected/revoked/expired, stale, uncertain, denied, degraded, and access-loss component/browser evidence |
| Timing, authorization, and commands remain observable without protected labels (`AC-SUBM-27`, `AC-SUBM-28`, authorization `PROP-8`) | Allowlisted metrics/logs, performance fixtures, security scan | Attempt-eligibility/effective-timing and authorization p95 under `PROP-5`; accommodation mutation latency recorded as diagnostic evidence without inventing an SLO; label allowlist, leakage assertions, shared rate/abuse regression, and gitleaks |

This task closes only the timing/accommodation implementation row.
`AC-SUBM-4` remains Partial until future Submission-intake and Attempt-start
consumers independently enforce current Enrollment denial. Attempt, retry, and
intake/version criteria remain unimplemented.

# Architecture and data plan

## Ownership

- `FlexAgent.Submissions` owns effective Participant timing, accommodation
  identity/lifecycle, timing decisions for this slice, commands/queries,
  idempotency outcomes, and browser-safe results.
- `FlexAgent.Submissions.Infrastructure` owns Submissions SQL, migrations,
  transaction/repository adapters, and audit/outbox integration. Core code must
  not reference Npgsql, Dapper, HTTP, or another module's infrastructure.
- Assessment Configuration keeps ownership of Activity/Cohort state, immutable
  baseline, and frozen source verification. Extend its application-owned
  activated-Cohort reader with the missing attempt limit, optional duration,
  exact frozen Organization-policy reference, and normalized frozen
  fairness-domain values needed for this slice. Assessment does not define or
  normalize Submissions accommodation semantics.
- Configuration remains the ADR-017 owner of exact Organization-policy source
  versions and current availability/effective values. Add a narrow read and
  transaction-aware owner port so Submissions can resolve current policy and
  reject a stale, revoked, missing, widening, or cross-scope version without
  querying Configuration tables. If Configuration cannot expose the current
  applicable policy selection and transaction participation, the implementation
  must fail closed rather than use a fallback policy.
- The versioned lifecycle resolver is independent of the accommodation-rule
  policy. Do not reuse the current Enrollment constant as proof that the
  accommodation record class satisfies `REQ-OPS-18`.
- IdentityAccess keeps ownership of actors, application sessions,
  Organization action grants, authentication strength, and current
  authorization evidence.
- `FlexAgent.Api` remains a thin transport/composition boundary.
- Canonical schemas under `contracts/` remain wire authority; projections are
  not domain entities or authorization facts.
- No ADR is expected. Escalate before code if the existing modular-monolith,
  ADR-002, ADR-003, or ADR-004 decisions cannot preserve the required boundary.

## Effective timing

- Preserve baseline `starts_at_utc`, `ends_at_utc`, `deadline_utc`,
  `time_zone_id`, `attempt_limit`, and optional
  `per_attempt_duration_seconds` separately from effective values.
- The approved mapping is Submission
  `[starts_at_utc, deadline_utc)` and Attempt start
  `[starts_at_utc, ends_at_utc)`; per-Attempt duration is a distinct downstream
  Session limit and does not silently move the Attempt-start cutoff.
- Return explicit effective Submission and Attempt-start windows, exclusive
  cutoff labels, `evaluated_at_utc`, current state, and bounded reason.
- Treat complete-payload receipt and Attempt-start atomic commit as the future
  authoritative event times under `PROP-3`; this slice evaluates timing but
  does not accept payloads or start Attempts.
- Re-resolve the current Organization policy for each authoritative eligibility
  decision. Apply the most restrictive intersection of frozen baseline bounds,
  current Organization bounds, and an eligible accommodation; a current
  narrowing can make a historical accommodation out-of-scope without editing
  it.
- Degraded baseline, inactive Enrollment, or missing/incompatible current
  policy returns unavailable/denied and grants no downstream authority. A
  baseline-only descriptive projection may remain visible only when it is
  explicitly labeled non-authoritative and does not claim current eligibility.
- Preserve the named timezone identifier exactly. Browser conversion catches
  unsupported identifiers and falls back to exact UTC plus the named zone;
  it must not substitute the browser timezone or display a fabricated local
  conversion.
- Participant output contains only exact effective time, named timezone,
  state, and plain-language consequence—not policy bounds, approval internals,
  source digests, actor IDs, or audit references.

## Persistence

- Recheck migration head before implementation. At planning time it is
  `0043`. The shared-quota task is expected to consume `0044` if implemented
  first, so this task must use the next available additive migration rather
  than assume `0044`.
- Persist stable accommodation ID; Organization/Enrollment/activity/cohort/
  baseline/Participant binding; dimension and normalized value; exact policy
  identity/version/digest at request and decision; frozen baseline policy
  identity; independently resolved lifecycle policy; requester and optional
  approver; bounded reason category; effective/expiry UTC times;
  status/revision; decision and revocation times; correlation; and
  authorization/audit references.
- Protect request, parent, baseline, policy, actor, reason, and proposed value
  through constraints/triggers. Status changes append ordered facts and never
  rewrite prior decisions.
- Add scoped idempotency/reconciliation for request/bounded grant, approve,
  reject, revoke, and explicit supersession. Equivalent retry returns the same
  result; mismatched key changes nothing.
- Enforce at most one current effect for each Enrollment and recognized
  dimension. A later approved record supersedes rather than edits its
  predecessor; values are normalized replacements, never composed deltas.
- Use database UTC time inside commits and an explicit authoritative order.
- Couple every protected mutation and required audit/outbox fact in one
  transaction. Required-audit failure rolls back all protected state.
- Resolve the approved Accommodation lifecycle class from the independent
  versioned lifecycle-policy boundary: retain history for 365 days after
  Activity closure, audit for 730 days, and idempotency outcomes for 90 days,
  subject to legal hold. Business expiry affects eligibility only.

## HTTP/contracts

- Add exact Enrollment-scoped command/query routes under the parallel
  `/v2/assessment` gateway after canonical v2 schemas exist. Preserve existing
  `/v1/assessment` routes and strict v1 payloads unchanged.
- Keep request/grant, exception decision, revocation, timing query, and
  reconciliation distinct. A GET or client action list is not mutation
  authority.
- Define bounded outcomes for invalid field, denied/unavailable, unsupported
  policy/dimension, outside bounds, approval required, stale revision,
  idempotency conflict, expired/revoked/superseded request, audit unavailable,
  uncertain/reconciling, and rate limited.
- Preserve `no-store`, CSRF, request-size/rate-limit,
  authentication-strength, and non-disclosing behavior.
- Route new reads through the existing Enrollment `read` request-limit surface
  and new commands through its `mutation` surface so alternate paths cannot
  create an unbounded quota. Replica-independent gateway enforcement remains
  assigned to a separate task under approved `PROP-8` and ADR-018 and is not a
  completion claim for this task.
- Do not reinterpret strict `my-work-assignment.v1` baseline timing as
  accommodated timing or append fields that its `additionalProperties: false`
  contract rejects. Add strict v2 Enrollment and **My work** projections, serve
  v1 and v2 in parallel, and migrate the production SPA to v2 without retiring
  v1 in this task. Update schema catalog, fixtures, OpenAPI, C#, TypeScript,
  endpoint, and client coherently.

## UI

- Use workspace density for administrator effective rules, accommodation form,
  exception decision, and history; interaction density for Participant timing.
- Administrator order: Enrollment state; immutable baseline timing; current
  effective timing; current/pending/history; permitted request/revoke;
  separately authorized decision.
- Show only currently permitted dimensions, bounds, reasons, and
  effective/expiry controls. Confirmation states exact target, baseline value,
  requested effect, expiry, approval consequence, and preserved history.
- Keep approval required, checking, approved, rejected, revoked, expired,
  superseded, audit-failure, stale, and permission-loss states distinct.
- Participant detail leads with exact effective boundary and named Campaign
  timezone. Relative/local conversion is supplementary and non-authoritative.
- If the browser cannot interpret the server-supplied named zone, show exact
  UTC and the unmodified named zone with bounded unavailable-conversion copy;
  never silently format in the browser's zone.
- Use semantic controls, linked error summaries, accessible dialogs,
  contained/restored focus, visible status, logical headings, and focus
  recovery after access loss.
- At narrow width and 400% zoom, keep status, exact boundary, consequence, and
  recovery before history. Use labeled stacked records where needed.
- Verify light/dark, reduced motion, keyboard, names/announcements, desktop,
  narrow, and 400% reflow through the authenticated app with synthetic data.

# Security and privacy plan

- Treat Enrollment, Participant identity, timing, accommodation, exception
  decisions, and audit history as sensitive.
- Deny by default and derive every scope/parent from trusted server state.
- Require separate exact actions for protected read, bounded grant, fairness
  decision, and revocation; revalidate session, strength, grant, parentage,
  policy, and requester/approver rule immediately before commit.
- Add the new actions to the explicit authentication-strength and audit-class
  maps. The fairness decision must use its own current grant and relationship;
  an administrator role label or the requester's grant cannot imply approval.
- Prevent cross-Organization/activity/cohort/Enrollment/Participant leakage
  through reads, writes, lists, counts, cursors, caches, events, logs, and
  retries.
- Do not reveal inaccessible bounds, approval route, actor identity, internal
  reason, source digest, or record existence.
- Require idempotency key, trusted command digest, and expected revision.
- Accept bounded reason categories only; no free-form protected narrative
  unless a later approved policy governs it.
- Keep protected values out of URLs, analytics, logs, metrics, traces, browser
  storage, screenshots, fixtures, and errors. Browser evidence uses synthetic
  actors and timing only.
- Do not copy accommodation data into the baseline, Session, memory, model
  context, Evaluation, or review records in this slice.

# Plan

- [x] Promote the seven implementation-readiness decisions into the owning
  authoritative documents. Completed 2026-08-23 through the approved concept
  amendment, `PROP-9`–`PROP-15`, `REQ-SUBM-50`–`REQ-SUBM-56`,
  `AC-SUBM-33`–`AC-SUBM-39`, operational defaults v0.4, UI specification v0.2,
  and `AR-DEC-26`–`AR-DEC-27`.
- [x] Reconcile the implementation-time migration and contract heads, then
  freeze the requirement-to-domain/API/contract/UI/test mapping and the
  normalized frozen/current policy, timing, authorization, reason, lifecycle,
  and strict v2 compatibility contracts before the accommodation Red phase.
- [x] Red — run failing domain tests for baseline-only timing, exclusive
  boundaries, UTC/timezone validation, current-policy narrowing, bounded
  accommodation lifecycle, exception approval, derived
  expiry/revocation/supersession, and unchanged baseline.
- [x] Green/refactor — implement minimum Submissions timing/accommodation
  domain/application behavior with explicit clock and trusted policy/baseline
  ports.
- [ ] Red — run failing PostgreSQL migration-upgrade, scope, immutability,
  concurrency, idempotency, decision-race, session/grant revocation,
  current-policy revalidation, authoritative-clock, audit-failure, and
  append-only-history tests.
- [>] Green/refactor — add migration, repositories, named coordinator,
  owner-port extensions, IdentityAccess adapters, audit/outbox, and telemetry.
- [ ] Red — run failing schema/fixture/catalog/OpenAPI/C#/TypeScript parity,
  HTTP positive/negative, CSRF, no-store, limit, redaction, and reconciliation
  tests.
- [ ] Green/refactor — implement thin HTTP routes and coordinated versioned
  contracts without changing existing v1 meaning.
- [ ] Red — run failing React/API tests for administrator timing, bounded
  request/confirmation, approval-required, separate decision, revoke,
  stale/uncertain/audit failure/access loss, and Participant timing consequence.
- [ ] Green/refactor — implement approved Enrollment and **My work** UI states
  with protected loading/clearing, accessible focus/announcements, and
  responsive records.
- [ ] Run focused domain, contract, HTTP, PostgreSQL, React, accessibility,
  performance, authorization, concurrency, audit, and security tests.
- [ ] Run authenticated Playwright MCP through administrator, separate approver
  when applicable, and Participant journeys; inspect accessibility snapshots
  and desktop/narrow/400%/themes/focus/dialog/error screenshots in
  `.playwright-mcp/`.
- [ ] Run proportionate full regression, docs, whitespace, secret,
  supply-chain, and OCI gates and record exact evidence.
- [ ] Run independent backend, frontend, security/privacy, and QA review;
  remediate blocking findings, reconcile changes/specs, and update truthful
  traceability rows.

# Current state

Review remediations 2026-08-24 (third pass after 57ffafa). Timing projection
sets `policy_available` from effective (identity-bound) policy and omits
request/approve/reject client actions when that policy is unavailable; revoke
remains advertiseable. Expiry is truncated to UTC microseconds before digest
and store so .NET ticks match PostgreSQL `timestamptz`. The identity-unchecked
two-argument `EffectiveBounds` overload was removed. Focused domain/application
tests: 73 combined Submissions classes green; PostgreSQL microsecond round-trip
test added on `EnrollmentPersistenceTests` (not run here — Docker unavailable).

Production remains fail-closed without an exact current Organization policy and
without a persisted frozen snapshot from Assessment/Configuration. Development
uses the synthetic policy fixture. Remaining closeout: broader PostgreSQL
isolation/concurrency/audit suites against `0046`, persist frozen bounds on
activation, canonical catalog/OpenAPI fixtures, broader HTTP/React state
coverage, Playwright MCP evidence, independent reviews, and spec
traceability.

The Assessment port supplies verified baseline start, end, deadline, and
timezone. Attempt limit, optional per-Attempt duration, and frozen
Organization-policy identity still use adapter defaults when the binding
omits them. Accommodation records, coordinators, additive `0046` persistence,
v2 HTTP, and SPA timing UI now exist. **My work** shows exact effective
cutoff and does not expose intake or Attempt start. Production positive
accommodations remain fail closed until Configuration supplies an exact
current Organization policy; development uses the synthetic fixture.

# Decisions

- Keep timing/accommodations in Submissions; create no Timing, Fairness, or
  Accommodation service/module.
- Consume the frozen baseline through an Assessment-owned application port and
  current Organization policy through a Configuration-owned application port;
  shared PostgreSQL does not authorize cross-module SQL.
- Keep baseline immutable; link accommodation history to exact Enrollment and
  baseline without changing the document, digest, Activity, or Cohort.
- Keep baseline timing, effective timing, eligibility, and accommodation state
  distinct in domain and wire contracts.
- Map Submission to `[starts_at_utc, deadline_utc)` and Attempt start to
  `[starts_at_utc, ends_at_utc)`; keep per-Attempt duration separate. Browser
  formatting never decides eligibility.
- Support exactly `submission_deadline_utc`,
  `attempt_start_not_before_utc`, `attempt_start_before_utc`, and
  `per_attempt_duration_seconds`; permit one current normalized replacement per
  Enrollment/dimension and preserve superseded history. Normalize absolute or
  relative policy-source bounds against the verified baseline before
  evaluation.
- Keep bounded accommodation and fairness exception distinct; pending,
  rejected, or failed exceptions never change Participant timing. Every
  fairness exception requires a different current authorized approver, and the
  approval can accept or reject only the exact request.
- Accept only current-policy allowlisted reason categories, with no free text or
  diagnosis. The synthetic fixture category is development-only.
- Resolve lifecycle independently: accommodation history 365 days after
  Activity closure, audit 730 days, idempotency outcomes 90 days, all subject
  to legal hold; business expiry affects eligibility only.
- Preserve strict v1 contracts and routes. Add parallel strict v2 Enrollment
  and **My work** projections under `/v2/assessment`, migrate the SPA, and do
  not retire v1 in this task.
- Preserve existing architecture and canonical-schema patterns. Add no ADR
  unless they cannot satisfy a newly discovered durable decision.
- Do not claim Attempt, retry, Submission intake/versioning, Session readiness,
  or end-to-end `AC-SUBM-4` from this slice.

# Approved readiness decisions

The seven former open questions were approved by the product owner on
2026-08-23 and promoted to the authoritative documents listed under
**Governing sources**. There are no remaining product, requirements, UI/UX,
architecture, lifecycle, or compatibility questions blocking implementation.
Any implementation discovery that would change these decisions must return to
the owning authority rather than introduce a code-level exception.

# Findings / deviations

- **Sequencing risk — Enrollment predecessor closeout:**
  `.work/active/p0-enrollment-assignment-discovery.md` is completed after broad
  independent review; its request-quota disposition is resolved by approved
  `PROP-8` / ADR-018.
  Its implemented surfaces and recorded verification are sufficient to start
  this slice, whose first step rechecks them. This task must not absorb or claim
  the separate shared-quota behavior governed by approved `PROP-8`/ADR-018.
- **Implementation surface — current policy:** Configuration does not yet expose
  the approved accommodation-policy vocabulary or a transaction-aware current-
  policy port. Implement that owner port; production positive behavior fails
  closed until an exact current policy is available.
- **Implementation surface — lifecycle:** replace the inappropriate fixed
  Enrollment lifecycle reference with independent resolution of the approved
  Accommodation lifecycle class. Do not derive retention from business expiry.
- **Implementation surface — Assessment handoff:** the verified activated
  binding port can be extended safely, but today it omits attempt limit,
  optional duration, frozen Organization-policy reference, and frozen policy
  values. Assessment should expose those frozen facts; it must not become the
  owner of current Organization policy or Submissions accommodation semantics.
- **Implementation surface — contracts:** `my-work-assignment.v1` has
  `additionalProperties: false` and baseline timing semantics. Preserve it and
  implement the approved parallel strict v2 projections and SPA migration.
- **Implementation surface — timezone fallback:** the
  server accepts platform-recognized timezone IDs, while browser formatting may
  not support every ID. The client must show exact UTC plus the named zone when
  conversion is unsupported instead of substituting its local timezone.
- **Implementation surface — derived expiry:**
  server-time selection can derive expired state immediately. A materialized
  expiry transition or worker would add consistency and audit obligations and
  is not part of the approved contract.
- Enrollment detail is history-only and Participant detail renders raw UTC
  strings; approved UI behavior remains a material implementation gap.
- Retry entitlement and Attempt start have no owner yet. Keep them out of this
  task even though the UI spec discusses them nearby.
- **Fourth review pass (2026-08-24):** v1 Enrollment mutation/list/detail now
  uses `EnrollmentProjection.AdministratorActions` (lifecycle vocabulary only).
  v2 timing and accommodation mutations use `TimingAdministratorActions`.
  History `expires_at_utc` uses microsecond `FormatCanonicalInstant`.
  `permitted_dimensions` / `permitted_reason_categories` require
  `policy_available`. New invalid v1 fixture rejects leaked accommodation
  actions. Confirmed: Submissions 76, Contract catalog/mapping 103, Enrollment
  SPA eslint + 2 vitest; then commit/push.
- **Fifth review pass (2026-08-24):** revoke mutation outcomes no longer advertise
  request/approve/reject. `Success` takes actual `accommodationPolicyAvailable`;
  grant/decide pass `true` after policy validation; revoke passes `false`
  because it does not resolve current policy. Confirmed: Submissions 77;
  then commit/push.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Governing sources reconciled | passed for implementation | Approved concept, feature, operational, UI/UX, and architecture documents agree on timing mapping, frozen/current policy, dimensions, reasons, separation, lifecycle, and strict v2 rollout. |
| Current code/migration/contract/UI seam inventory | passed for planning | Assessment exposes only baseline start/end/deadline/timezone; Submissions injects a fixed Enrollment lifecycle reference; planning-time head is `0043`; strict v1 contracts/routes/UI exist; accommodations and current-policy resolver do not. Enrollment implementation and its separate closeout task are completed. |
| Planning document validation | passed | `python3 scripts/check_docs.py`; `git diff --check`; no-index whitespace check for this new task file produced no diagnostics (status `1` is the expected difference result). |
| Timing semantics readiness | passed | `REQ-SUBM-50`, `AC-SUBM-33`, and `PROP-9` approve both half-open windows and separate duration. |
| Accommodation-policy readiness | passed | `REQ-SUBM-51`–`REQ-SUBM-54`, `AC-SUBM-34`–`AC-SUBM-36`, and `PROP-10`–`PROP-13` define frozen/current validation, dimensions, normalized replacement, reasons, and approval separation. Production positive behavior remains fail closed until exact policy configuration exists. |
| Lifecycle-policy readiness | passed | Operational defaults v0.4 `REQ-OPS-30`/`AC-OPS-7` and `PROP-14` define retention, audit, idempotency, hold, and business expiry. |
| Contract compatibility | passed | `REQ-SUBM-56`, `AC-SUBM-39`, `PROP-15`, and `AR-DEC-27` preserve strict v1 while adding parallel strict v2 and migrating the SPA. |
| Domain red/green | passed for focused coordinators after fifth review pass | Accommodation/timing/enrollment domain classes → 77 passed (revoke outcome omits policy-dependent actions when policy was not revalidated). PostgreSQL microsecond round-trip test added but not executed here (Docker unavailable). |
| PostgreSQL migration/isolation/concurrency/fault tests | pending | Include populated-`0043` upgrade and real PostgreSQL negatives. |
| Schema/OpenAPI/C#/TypeScript parity | partial | Contract catalog 101 passed including `invalid-accommodation-action`; mapping parity rejects leaked v1 accommodation actions. OpenAPI/TS suite not re-run this pass. |
| API authorization/HTTP negatives | partial | CSRF grant rejection before session authentication passed. Broader grant/decide/revoke HTTP coverage still pending. |
| React/accessibility | partial | Focused vitest for enrollment detail (including revoke revision), My work timing, timezone formatter, and enrollment client: 8 passed. Playwright MCP still pending. |
| Authenticated Playwright MCP | pending | Real synthetic journeys; accessibility snapshots and desktop/narrow/400%/themes/focus/error screenshots. |
| Regression/performance/security/supply-chain/OCI/docs | pending | Record commands, counts, skips, environment, timings, and residuals. |
| Independent review | pending | Backend, frontend, security/privacy, and QA review required. |

# Planned verification command set

- Focused core: `dotnet test --project
  tests/Submissions/FlexAgent.Submissions.Tests/FlexAgent.Submissions.Tests.csproj
  -c Release`.
- PostgreSQL: focused filters, then the full
  `tests/Integration/FlexAgent.Postgres.Integration.Tests` Release project.
- Runtime/API: focused filters, then the full
  `tests/Runtime/FlexAgent.Runtime.Tests` Release project.
- Architecture/contracts: architecture and contract .NET projects plus
  `pnpm --filter @flex-agent/contracts test`.
- Web: focused Enrollment/**My work** tests, then web test, lint, typecheck,
  and build.
- Browser: authenticated Docker profile plus configured Playwright MCP;
  synthetic screenshots only under `.playwright-mcp/`.
- Regression: locked restore, full solution Release tests, docs check,
  `git diff --check`, gitleaks, supply-chain, and OCI verification.

# Blockers

None for starting implementation.

Production positive-accommodation enablement remains fail closed until an exact
current Organization policy is configured and resolved through the new
Configuration owner port. Development and repeatable tests use the explicitly
synthetic development fixture category. No external credential, paid provider,
or deployment is required to implement and verify this task. Before final
integration/closeout, recheck the completed Enrollment predecessor and the
independently tracked shared-quota implementation; do not silently absorb or
claim the quota task's behavior.

# Completion

- [ ] Plan reconciles with actual changes and current migration/contract head
- [x] Consequential timing, policy, lifecycle, and compatibility questions are approved
- [ ] Domain red/green/refactor evidence is recorded
- [ ] Migration, isolation, concurrency, idempotency, clock, history, and audit evidence passes
- [ ] Schema, OpenAPI, C#, TypeScript, endpoint, and client remain compatible and traceable
- [ ] Administrator and Participant UI passes component, accessibility, responsive, and Playwright verification
- [ ] Focused, integration, performance, security, full regression, supply-chain, OCI, docs, and whitespace gates pass
- [ ] Governing specs and implementation-status rows remain truthful
- [ ] Independent backend/frontend/security/privacy/QA findings are resolved or accepted by an authorized owner
- [ ] Remaining gaps are recorded without claiming intake, Attempt, retry, or Session readiness
- [ ] Task state is safe and complete for external review and retained
