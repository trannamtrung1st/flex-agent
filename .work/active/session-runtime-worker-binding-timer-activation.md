---
id: session-runtime-worker-binding-timer-activation
status: completed
created: 2026-08-18
updated: 2026-08-18
predecessors:
  - session-runtime-worker-host-wiring
  - session-runtime-subject-binding-rehydration
---

# Goal

Make the production Worker capable of rehydrating the immutable trusted Session
binding from PostgreSQL and processing the one approved due-timer lane only
under a current, bounded service delegation. The hosted path must revalidate
the complete Organization/Activity/Participant/Attempt/Session scope, frozen
policy, timer revision, lifecycle, cutoff, and delegation inside the
authoritative commit boundary before it admits one timer-triggered Invocation.

This task prepares and verifies the production timer-polling path without
enabling a live model provider or claiming that the overall Text Session or
production pilot is complete.

# Governing sources

- `AGENTS.md` — isolation, frozen configuration, authorization, audit,
  specification-driven TDD, and implementation-workflow invariants
- `docs/product/concept-model.md` — Session isolation, frozen configuration,
  governed Agent triggers, and the runtime-owned one-lane timer
- `docs/product/mvp-scope.md` and `docs/product/overview.md` — P0 text scope and
  remaining Worker/provider production gates
- `docs/requirements/features/auth-resource-isolation.md` — deny-by-default
  background execution, service identity, bounded delegation, revocation, and
  commit-time authorization
- `docs/requirements/features/resolved-session-configuration.md` —
  `REQ-RSC-15`–`REQ-RSC-20`, `REQ-RSC-24`, `REQ-RSC-28`,
  `REQ-RSC-47`–`REQ-RSC-55`, and `AC-RSC-26`–`AC-RSC-28`
- `docs/requirements/features/session-text-lifecycle.md` — `REQ-SESS-61`–
  `REQ-SESS-77` and `AC-SESS-33`–`AC-SESS-41`, especially `REQ-SESS-75`
- `docs/architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md`
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`
- `docs/architecture/decisions/ADR-003-authorization-audit-persistence.md`
- `docs/architecture/decisions/ADR-011-participant-visible-agent-response-streaming.md`
- `docs/architecture/decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md`
- `docs/architecture/decisions/ADR-013-agent-requested-next-timer-replacement.md`
- `docs/architecture/decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md`
- `docs/architecture/session-runtime-contract.md` — `SESS-DEC-14`–
  `SESS-DEC-21` and `SESS-DEC-24`–`SESS-DEC-28`
- `.work/active/session-runtime-worker-host-wiring.md` — completed hosted
  invocation/publication boundary and intentional idle timer processor
- `.work/active/session-runtime-subject-binding-rehydration.md` — immutable
  PostgreSQL policy snapshot and API binding rehydration
- `.work/active/structured-agent-runtime-traceability.md` — completed foundation
  snapshot and explicit Worker binding/timer residual

# Scope

## In

- Register `PostgresTrustedSessionBindingSource` in the Worker when Sessions
  persistence is configured; do not reconstruct policy from mutable
  configuration sources or client/work payloads.
- Prove exact immutable policy, configuration digest, manifest identity, and
  protected-reference rehydration for Worker invocation and timer paths.
- Add or extend the smallest durable service-delegation representation required
  by ADR-002 for `session.timer_lane.fire`, including a stable delegation
  reference, service identity, Organization and Session scope, allowed action,
  system purpose or initiating authority, effective/expiry facts, revocation,
  and monotonic version/freshness.
- Carry the trusted delegation reference with the authoritative timer schedule
  or equivalent durable work envelope; never infer it from a Session id,
  process location, or static actor label.
- Reauthorize the Worker service, delegation, resource ownership, Session
  relationship/policy, lifecycle, cutoff, and expected timer revision inside
  the same transaction that commits the timer fire, Invocation, audit, and
  outbox records.
- Compose `PostgresFireDueTimerCoordinator` and `DurableTimerFireProcessor` in
  the Worker behind an explicit runtime capability gate.
- Keep timer polling disabled when Sessions persistence, trusted binding,
  delegation enforcement, or the explicit timer-polling capability is absent.
- Preserve current single-lane concurrency, idempotency, pause/cutoff,
  budget-exhaustion, lifecycle-ineligible, and poison-row behavior.
- Make readiness and bounded telemetry distinguish invocation claiming from
  timer polling without exposing Organization, Session, participant,
  delegation, configuration, or credential identifiers.
- Reconcile authoritative requirement status and production-gate documentation
  after executable evidence exists.

## Out

- Live OpenAI/Azure or other model-provider adapters, provider qualification,
  `SecretSource` credential resolution, Organization BYOK, and provider
  capability/compatibility evidence
- Enabling a real-use execution profile while `FailClosedModelExecutionPort`
  remains the only model port
- Long-provider-call lease heartbeat; it remains owned by the provider-adapter
  successor task
- OIDC/application sessions, production Participant UI migration, or new HTTP
  Session commands
- Changes to the frozen P0 timer policy, timer cadence values, Decision
  envelope, output/action profile, or Participant-visible behavior
- Voice, Interaction Controller, tools, arbitrary/parallel timers, Dynamic
  memory, or richer workflow triggers
- Timer-storm/load, OTLP Collector, backup/restore, lifecycle/export, or full
  production-pilot certification
- Rewriting frozen migrations `0001`–`0021`; any persistence change is additive
  and begins at `0022`
- Commits, pushes, pull requests, deployments, or releases unless separately
  requested

# Plan

- [x] Confirm the authorization/delegation realization before behavior edits:
      map ADR-002 and `REQ-SESS-75` to a stable service principal, durable
      per-Session delegation, timer-schedule reference, commit-time kernel
      check, audit facts, and revocation behavior; promote a consequential
      technical choice to an ADR amendment if the approved decisions do not
      already determine it.
- [x] Red — add focused host composition tests proving that a Sessions
      connection string alone no longer implies trusted timer authority: the
      Worker must use PostgreSQL binding rehydration, retain the fail-closed
      model port, and keep timer polling disabled until the explicit capability
      and delegation dependencies are present.
- [x] Red — add PostgreSQL authorization tests for missing, expired, revoked,
      wrong-service, wrong-action, wrong-Organization, wrong-Session, and stale
      delegation; prove denial leaves the due schedule, Session version,
      Invocation set, audit, and outbox unchanged.
- [x] Red — add a commit-race test in which delegation revocation or narrowing
      wins after selection but before mutation; the timer fire must deny or
      reconcile without admitting an Invocation.
- [x] Green — implement the minimum additive delegation persistence and scoped
      repository/kernel support, including migration and upgrade tests when
      required. New Sessions receive the reference transactionally; historical
      rows without a trustworthy reference remain fail-closed and are not
      guessed or silently backfilled.
- [x] Red/green — cover `PostgresTrustedSessionBindingSource` through the real
      Worker path: exact snapshot round-trip succeeds; missing snapshot,
      ownership mismatch, configuration/policy digest mismatch, malformed
      payload, or cross-Session/cross-Organization substitution returns no
      binding and causes no mutation.
- [x] Green — make `PostgresFireDueTimerCoordinator` authorize the service and
      delegation at admission and reauthorize under the timer-fire transaction;
      preserve the existing expected-revision and authoritative-database-clock
      boundary.
- [x] Green — compose `IDueTimerFirePort`, `DurableTimerFireSettings`, and
      `DurableTimerFireProcessor` in the Worker only when all runtime gates are
      satisfied; otherwise retain `IdleDurableTimerFireProcessor`. Source the
      service principal and capability from trusted host configuration, not
      claim payloads or Participant/Session input.
- [x] Verify the real PostgreSQL end-to-end timer path with the production
      binding source and delegation store: one due event admits one Invocation,
      equivalent/concurrent workers reconcile, restart/reclaim remains safe,
      missing binding/delegation preserves pending work, and
      budget/lifecycle terminal outcomes do not head-of-line block another
      Session.
- [x] Verify pause, resume, revocation, completion, expiry, termination, abort,
      stale revision, wrong scope, and cutoff races continue to suppress late
      timer-triggered work and never rearm a prohibited lane.
- [x] Update readiness/telemetry and their tests so operators can distinguish
      `invocation_claiming_enabled` from `timer_polling_enabled` using bounded
      labels and honest degraded/disabled states.
- [x] Run focused tests, the locked .NET regression suite, migration/upgrade
      checks, architecture boundaries, docs validation, and whitespace checks;
      record observed red and green evidence below.
- [x] Reconcile `session-text-lifecycle.md`, `resolved-session-configuration.md`,
      product/architecture production-gate wording, this task, and actual code.
      Do not rewrite the completed traceability snapshot or mark live-provider,
      OIDC, Participant UI, load/OTLP, or production-pilot gates complete.
- [x] Obtain independent backend and security/privacy review of the completed
      slice; resolve blocking findings and repeat affected verification before
      marking this task completed.

# Review remediations (`9ab00a1`)

- [x] P1 — Issue/revoke through a transaction-bound coordinator with
      mutation-coupled append-only audit and previous/new transition facts
      (`REQ-AUTH-27`, `REQ-AUTH-31`, ADR-003). Remove autocommit repository
      mutation APIs.
- [x] P1 — Make `ReauthorizeInTransactionAsync` the last meaningful SQL before
      each timer-fire `CommitAsync`; add an expiry-after-persist race test.
- [x] P2 — Persist `authorization_reference_type` + `authorization_reference_id`
      on timer-fire audit events with the exact `delegation_id`.
- [x] P2 — Require `expires_at` for `session.timer_lane.fire` with a 7-day max
      lifetime (`PROP-WBT-5`); fail closed on missing or over-long expiry.

# Review remediations (`d175099`)

- [x] P1 — Authorize `service_delegation.issue` / `service_delegation.revoke`
      through the kernel; mutate `service_delegation` as the resource; record
      the parent organization grant as `authorization_reference`; propagate
      initiator, correlation, source, and reason from the calling command.
- [x] P1 — Remove `NarrowAllowedAction` (capability substitution). Prove
      wrong-action with test-only SQL; use revoke for the commit race.
- [x] P1 — Make `0023` refuse populated unbounded `0022` timer-lane rows with
      an explicit operator-facing guard; do not fabricate expiry. Add `0024`
      `grant_id` so mutation audit can name the authorizing grant.

# Review remediations (`58f2595`)

- [x] P1 — Abort the caller transaction on commit-time (and post-write)
      authorization denial so a later `COMMIT` cannot persist the mutation.
- [x] P1 — `0023` preflight and CHECK ignore revoked timer-lane rows so the
      documented revoke-then-upgrade repair works; add revoked+unbounded
      upgrade evidence.

# Review remediations (`9da4af5`)

- [x] P1 — Make authorization-denial rollback use a non-cancelable cleanup
      token so a canceled request cannot leave a committable transaction.

# Review remediations (`90a96f6`)

- [x] P1 — Remove the public pre-abort production hook; reproduce the
      canceled-token race with a test kernel wrapper so rollback cannot be
      skipped.

# Post-completion remediation (`fabe346`)

- [x] Red — prove that configuring Sessions persistence alone keeps durable
      Invocation processing idle, and that timer polling remains independently
      gated.
- [x] Green — require an explicit default-off
      `Sessions:InvocationProcessing:Enabled` capability before registering the
      live Invocation processor and its mutation dependencies.
- [x] Verify focused host composition/readiness, locked regression, docs, and
      whitespace; then reconcile this retained task record.

# Current state

Post-completion remediation is complete. Sessions persistence rehydrates the
immutable binding and can sample backlog, but it no longer registers the live
Invocation processor, publication persist port, or model port. Those mutation
dependencies require `Sessions:InvocationProcessing:Enabled`. Timer polling
remains independently gated by `Sessions:TimerPolling:Enabled`. Both
capabilities default off.

Live model providers, OIDC, Participant UI, hosted HTTP Session-create, and
production-pilot certification remain out of scope. ADR-015 remains Proposed
until product and architecture approve it.

# Decisions

- Reuse the Session's immutable `session_frozen_policy_snapshots` record via
  `PostgresTrustedSessionBindingSource`. The Worker must not re-resolve mutable
  Agent, Harness, Activity, or Organization configuration at execution time.
- Preserve `FailClosedModelExecutionPort` in this task. Binding and timer
  readiness must not be misreported as provider readiness.
- Keep timer polling behind a distinct explicit capability, default disabled.
  Keep Invocation processing behind a separate explicit capability, default
  disabled (`PROP-WBT-8`). Tests may enable either capability with synthetic
  provider-independent Session data; a real-use profile must wait for the
  separately qualified provider and credential boundary.
- Missing or invalid binding/delegation is retryable only while authoritative
  state could become available; it must not cancel a valid pending timer,
  fabricate success, or create a second schedule.
- `LifecycleIneligible` and `BudgetExhausted` remain durable terminal timer-row
  outcomes; stale/missing authority remains a non-mutating fail-closed outcome.
- Due-claim selects only currently valid, ownership-matched timer-lane
  envelopes for the Worker service principal so unproven or revoked schedules
  cannot occupy the poller.

## Proposed implementation defaults

Recorded as proposed [ADR-015](../../docs/architecture/decisions/ADR-015-session-timer-lane-service-delegation.md).
Security/privacy accepted the implemented MVP path at `f5e06e9`. Product and
architecture approval remain required before the ADR is Approved.

- `PROP-WBT-1` — Use one durable per-Session service-delegation reference for
  `session.timer_lane.fire`, linked from the timer schedule. It names the
  trusted Worker service principal, Organization, Session, action, system
  purpose, effective/expiry bounds, revocation state, and monotonic version.
- `PROP-WBT-2` — Do not backfill a delegation for an existing active Session
  from identifiers alone.
- `PROP-WBT-3` — Require an explicit `Sessions:TimerPolling:Enabled` host
  capability, default `false`, in addition to the Sessions connection string.
- `PROP-WBT-8` — Require an explicit `Sessions:InvocationProcessing:Enabled`
  host capability, default `false`, before registering the live Invocation
  processor or its mutation ports. Sessions persistence and timer polling
  remain independently gated.
- `PROP-WBT-4` — Use the primary database clock and transaction for delegation
  freshness, timer due state, Session lifecycle/cutoff, expected revision,
  Invocation admission, audit, and outbox commit.
- `PROP-WBT-5` — `session.timer_lane.fire` delegations require `expires_at`, and
  `expires_at - effective_at` cannot exceed seven days. Renewal is a later
  authorized command.
- `PROP-WBT-6` — Service-delegation issue and revoke are kernel-authorized
  against a current actor-organization grant. Audit records the mutated
  delegation as the resource and the grant as `authorization_reference`.
  Callers propagate initiator, correlation, source, and reason.
- `PROP-WBT-7` — Do not overwrite `allowed_action`. Replace a capability by
  revoking the old delegation and issuing a new authorized one.

# Findings / deviations

- ADR-002 already required durable delayed-work delegation; ADR-015 records the
  timer-lane realization as Proposed rather than silently amending ADR-002.
- Malformed frozen-policy payloads cannot be injected against append-only
  snapshots; unit `FrozenRuntimePolicySnapshotTests` plus digest/ownership/
  missing/cross-scope PostgreSQL tests cover fail-closed rehydration.
- Wrong-session negative cases currently substitute a delegation from another
  Organization/Session pair; kernel checks both organization and session id.
- Production Session insert still has no hosted HTTP command. `InsertActiveAsync`
  issues a timer-lane delegation only when callers supply
  `AuthorizedServiceDelegationIssue` plus the commit kernel.
- The configured Worker service actor must already exist in `actors`; this task
  does not provision that row.
- Implementer backend and security/privacy review found that skipping only
  null envelopes left revoked/expired/mismatched due rows able to occupy the
  poller, and that admission used a second connection while those rows were
  locked. Due-claim now joins a currently valid envelope; admission and commit
  authorization share the fire transaction.
- Independent review of `f5e06e9` (2026-08-18) found no remaining blockers and
  accepted this timer-lane delegation/security remediation for the MVP. ADR-015
  stays Proposed until product and architecture approve it. GitHub has no
  attached commit status checks for that SHA.
- A later repository-status review found that binding registration also
  activated Invocation claiming. That path is now separately gated
  (`PROP-WBT-8`). The live Invocation processor still lacks its own bounded
  service-delegation realization; enabling the host capability remains a
  test/synthetic profile, not production authorization.

# Threats and required controls

| Threat | Required control | Verification |
| --- | --- | --- |
| Static process identity is treated as permission | Current durable delegation plus service principal and action/resource match | Unknown/wrong service and invalid envelopes are not selected |
| Cross-Organization or guessed Session substitution | Composite trusted ownership loaded from the selected due row and binding; delegation bound to the same scope | Wrong-org/session negative matrix |
| Revocation races timer commit | Reauthorize delegation under the same transaction/lock boundary as mutation | Revoke-between-admission-and-commit races |
| Invalid or revoked authority occupies the poller | Due-claim joins currently valid, ownership-matched envelopes | Revoked due row does not HOL-block another Session |
| Mutable or tampered policy widens timer behavior | Rehydrate immutable snapshot and verify configuration/policy digest | Missing/digest-mismatch/cross-scope tests |
| Retry or concurrency admits parallel work | Expected schedule revision, one-lane uniqueness, idempotent admission, `SKIP LOCKED` | Concurrent Worker tests |
| Invalid authority destroys valid pending work | Skip or roll back without cancelling or advancing the schedule | Repeated missing binding/delegation tests |
| Audit or telemetry leaks protected identifiers | Bounded reason/metric categories; protected references only in authoritative records | Ready-copy and telemetry allowlist tests |
| Timer polling creates provider-failure loops before qualification | Separate default-off capability and honest readiness | Connection-string-only host remains timer-idle |
| Sessions persistence is treated as Invocation execution authority | Separate default-off `Sessions:InvocationProcessing:Enabled` plus idle processor/mutation ports | Connection-string-only host keeps `IdleDurableInvocationWorkProcessor` |

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Baseline repository state | passed | Planned from `e8793e5` |
| Host composition red/green | passed | `WorkerRuntimeTests` 15/15: connection-string-only idle Invocation + idle timer; explicit `Sessions:InvocationProcessing:Enabled` live processor; explicit `Sessions:TimerPolling:Enabled` live timer with idle Invocation |
| Timer processor/domain focused tests | passed | `DurableTimerFireProcessorTests` 9/9 including `timer_fire.authority_denied` → `retry_later` |
| PostgreSQL binding/delegation/timer integration | passed | `SessionTimerLaneDelegationTests` including HOL skip of revoked due rows, `PostgresTrustedSessionBindingSourceTests`, `SessionTimerSchedulePersistenceTests` against PostgreSQL 18 |
| Migration and upgrade safety | passed | Additive `0022`–`0024`; Grate expected one-time count 24; populated unbounded `0022` timer-lane rows fail closed on `0023`; bounded `0022` rows apply |
| Architecture/module boundaries | passed | Architecture 31/31 including Worker Dockerfile COPY of IdentityAccess |
| Locked .NET regression | passed | Invocation-capability remediation: `bash build/scripts/verify-dotnet.sh` **942/942** |
| Documentation | passed | `python3 scripts/check_docs.py` |
| Whitespace | passed | `git diff --check` clean |
| External review remediations (`9ab00a1`) | passed | P1 audit/transaction coordinator; P1 final reauth-before-commit + expiry race; P2 audit delegation reference; P2 required 7-day timer-lane expiry |
| External review remediations (`d175099`) | passed | P1 kernel-authorized issue/revoke with grant `authorization_reference` and caller mutation context; P1 removed `NarrowAllowedAction`; P1 explicit `0022`→`0023` populated fail-closed plus `0024` `grant_id` |
| External review remediations (`58f2595`) | passed | P1 abort caller tx on final auth denial + commit-after-deny test; P1 revoked unbounded `0022` rows upgrade |
| External review remediations (`9da4af5`) | passed | P1 non-cancelable denial rollback + canceled-token commit-after-deny test |
| External review remediations (`90a96f6`) | passed | P1 removed public pre-abort hook; cancel race via test kernel wrapper |
| Independent review (`f5e06e9`) | passed | No blockers. Slice accepted for MVP. Local `940/940` not independently verifiable from GitHub status checks |
| Post-completion Invocation capability remediation | passed | Red: 4 `WorkerRuntimeTests` failed because CS registered live processor/claiming. Green: `Sessions:InvocationProcessing:Enabled` default-off. Runtime 92/92; Architecture 31/31; `check_docs.py`; `git diff --check`; locked **942/942** |

# Blockers

None. The live Invocation path remains intentionally disabled by default while
its separate service-delegation and provider-qualification work is outstanding.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Worker binding rehydration uses the immutable PostgreSQL Session snapshot
      and fails closed on tamper, missing data, or scope mismatch
- [x] Timer work uses current bounded service delegation and commit-time
      authorization; static actor identity alone never grants permission
- [x] Hosted timer polling remains disabled unless all explicit capability,
      persistence, binding, delegation, and authorization gates are satisfied
- [x] At most one due schedule admits at most one timer-triggered Invocation
      across retries, concurrency, restart, pause, revocation, and cutoff races
- [x] Negative Organization/Activity/Participant/Attempt/Session isolation and
      service-delegation tests pass
- [x] Audit, outbox, readiness, and telemetry remain bounded, honest, and free
      of protected payloads or identifiers
- [x] Applicable focused tests pass with observed red and green evidence
- [x] Applicable integration/regression and migration/upgrade checks pass
- [x] Governing specifications and production-gate status are rechecked and
      reconciled without overstating provider, OIDC, UI, load, or pilot readiness
- [x] Remaining gaps or unverified behavior are recorded
- [x] Independent backend and security/privacy review findings are resolved
- [x] Task state is safe and complete for external review
- [x] Sessions persistence alone cannot activate protected Invocation work
- [x] Post-completion remediation is verified and reconciled
