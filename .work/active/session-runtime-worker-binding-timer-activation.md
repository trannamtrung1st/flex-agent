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

# Current state

Implemented. When `ConnectionStrings:Sessions` is set, the Worker registers
`PostgresTrustedSessionBindingSource` and keeps `FailClosedModelExecutionPort`.
Timer polling stays `IdleDurableTimerFireProcessor` unless
`Sessions:TimerPolling:Enabled` is true, in which case it composes
`PostgresFireDueTimerCoordinator` plus `DurableTimerFireProcessor` and the
ADR-002 kernel. Additive `0022` stores per-Session `service_delegations` and
links `session_timer_schedules.timer_lane_delegation_id`. New inserts may issue
that reference in the same transaction. Due-claim joins a currently valid,
ownership-matched `session.timer_lane.fire` envelope for the configured service
principal so revoked, expired, mismatched, or historical-null rows stay pending
without HOL-blocking another Session. Admission and commit authorization run on
the same fire transaction; commit uses `FOR SHARE` so revocation or narrowing
after selection still denies.

# Decisions

- Reuse the Session's immutable `session_frozen_policy_snapshots` record via
  `PostgresTrustedSessionBindingSource`. The Worker must not re-resolve mutable
  Agent, Harness, Activity, or Organization configuration at execution time.
- Preserve `FailClosedModelExecutionPort` in this task. Binding and timer
  readiness must not be misreported as provider readiness.
- Keep timer polling behind a distinct explicit capability, default disabled.
  Tests may enable the capability with synthetic provider-independent Session
  data; a real-use profile must wait for the separately qualified provider and
  credential boundary.
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
They remain working guidance until product, architecture, and security/privacy
approve that ADR.

- `PROP-WBT-1` — Use one durable per-Session service-delegation reference for
  `session.timer_lane.fire`, linked from the timer schedule. It names the
  trusted Worker service principal, Organization, Session, action, system
  purpose, effective/expiry bounds, revocation state, and monotonic version.
- `PROP-WBT-2` — Do not backfill a delegation for an existing active Session
  from identifiers alone.
- `PROP-WBT-3` — Require an explicit `Sessions:TimerPolling:Enabled` host
  capability, default `false`, in addition to the Sessions connection string.
- `PROP-WBT-4` — Use the primary database clock and transaction for delegation
  freshness, timer due state, Session lifecycle/cutoff, expected revision,
  Invocation admission, audit, and outbox commit.

# Findings / deviations

- ADR-002 already required durable delayed-work delegation; ADR-015 records the
  timer-lane realization as Proposed rather than silently amending ADR-002.
- Malformed frozen-policy payloads cannot be injected against append-only
  snapshots; unit `FrozenRuntimePolicySnapshotTests` plus digest/ownership/
  missing/cross-scope PostgreSQL tests cover fail-closed rehydration.
- Wrong-session negative cases currently substitute a delegation from another
  Organization/Session pair; kernel checks both organization and session id.
- Production Session insert still has no hosted HTTP command. `InsertActiveAsync`
  issues a timer-lane delegation only when callers supply `ServiceDelegationIssue`.
- The configured Worker service actor must already exist in `actors`; this task
  does not provision that row.
- Implementer backend and security/privacy review found that skipping only
  null envelopes left revoked/expired/mismatched due rows able to occupy the
  poller, and that admission used a second connection while those rows were
  locked. Due-claim now joins a currently valid envelope; admission and commit
  authorization share the fire transaction. This is not a second-agent review.

# Threats and required controls

| Threat | Required control | Verification |
| --- | --- | --- |
| Static process identity is treated as permission | Current durable delegation plus service principal and action/resource match | Unknown/wrong service and invalid envelopes are not selected |
| Cross-Organization or guessed Session substitution | Composite trusted ownership loaded from the selected due row and binding; delegation bound to the same scope | Wrong-org/session negative matrix |
| Revocation races timer commit | Reauthorize delegation under the same transaction/lock boundary as mutation | Revoke/narrow-between-admission-and-commit races |
| Invalid or revoked authority occupies the poller | Due-claim joins currently valid, ownership-matched envelopes | Revoked due row does not HOL-block another Session |
| Mutable or tampered policy widens timer behavior | Rehydrate immutable snapshot and verify configuration/policy digest | Missing/digest-mismatch/cross-scope tests |
| Retry or concurrency admits parallel work | Expected schedule revision, one-lane uniqueness, idempotent admission, `SKIP LOCKED` | Concurrent Worker tests |
| Invalid authority destroys valid pending work | Skip or roll back without cancelling or advancing the schedule | Repeated missing binding/delegation tests |
| Audit or telemetry leaks protected identifiers | Bounded reason/metric categories; protected references only in authoritative records | Ready-copy and telemetry allowlist tests |
| Timer polling creates provider-failure loops before qualification | Separate default-off capability and honest readiness | Connection-string-only host remains timer-idle |

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Baseline repository state | passed | Planned from `e8793e5` |
| Host composition red/green | passed | `WorkerRuntimeTests` 13/13 including connection-string-only idle timer + explicit `Sessions:TimerPolling:Enabled` live processor |
| Timer processor/domain focused tests | passed | `DurableTimerFireProcessorTests` 9/9 including `timer_fire.authority_denied` → `retry_later` |
| PostgreSQL binding/delegation/timer integration | passed | `SessionTimerLaneDelegationTests` including HOL skip of revoked due rows, `PostgresTrustedSessionBindingSourceTests`, `SessionTimerSchedulePersistenceTests` against PostgreSQL 18 |
| Migration and upgrade safety | passed | Additive `0022`; Grate expected one-time count 22; `Upgrade_from_frozen_0020_applies_0021_subject_binding_tables` includes `service_delegations` |
| Architecture/module boundaries | passed | Architecture 31/31 including Worker Dockerfile COPY of IdentityAccess |
| Locked .NET regression | passed | Re-run after admission-in-transaction fix: `bash build/scripts/verify-dotnet.sh` **930/930** |
| Documentation | passed | `python3 scripts/check_docs.py` |
| Whitespace | passed | `git diff --check` clean |
| Implementer backend and security/privacy review | passed | HOL skip via current-envelope join; admission/commit share the fire transaction; ADR-015 remains Proposed; external second-agent review still valuable |

# Blockers

None.

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

