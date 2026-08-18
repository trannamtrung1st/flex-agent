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

Make the hosted Worker capable of rehydrating the immutable trusted Session
binding from PostgreSQL and processing the one approved due-timer lane only
under a current, bounded service delegation. The hosted path must revalidate
the complete Organization/Activity/Participant/Attempt/Session scope, frozen
policy, timer revision, lifecycle, cutoff, and delegation inside the
authoritative commit boundary before it admits one timer-triggered Invocation.

This task prepares and verifies the hosted timer-polling path without
enabling a live model provider, workload authentication, or claiming that
the overall Text Session or production pilot is complete. Until Worker
workload authentication exists, Production and Staging refuse
`Sessions:TimerPolling:Enabled`; the host flag and configured actor GUID
are not authentication.

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
      Invocation set, success audit, and outbox unchanged. A security-relevant
      kernel denial persists a separate denial audit after rolling back
      protected work.
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

# Review remediations (`f4eff9b`)

- [x] P1 — Refuse Worker startup when `Sessions:InvocationProcessing:Enabled`
      is set outside Development/Testing. The host flag is not authorization
      and must not persist fail-closed execution failures in production.
- [x] P2 — Persistence-only (and timer-only) hosts must not register the live
      invocation work store or issue an unscoped claimable-work aggregate.
      Document that this count is not an authorization-exempt operator read.
- [x] Verify focused host tests, locked regression, docs, and whitespace.

# Review remediations (`6811012` / remaining `f4eff9b` findings)

- [x] P1 — Persist security-relevant timer-lane authorization denials after
      rolling back protected mutations; do not audit ineligible poller skips.
- [x] P1 — Require explicit `Sessions:WorkerServiceActorId` when timer polling
      (or synthetic Invocation processing) is enabled; do not silently use the
      compiled default. Document deferred workload identity and expiry/renewal.
- [x] P2 — Add an executable requirement-to-test mapping table; fix ADR-015
      `PROP-WBT-1`–`8` (and later) references. Keep Invocation processing
      Development/Testing-only until a successor delegation task.
- [x] Verify focused authorization/timer/Worker host tests, locked regression,
      docs, and whitespace.

# Review remediations (`6fb9de5`)

- [x] P2 — Correct executable `REQ-AUTH-18`–`REQ-AUTH-22` mappings so
      commit-time reauthorization, revocation/expiry, fail-closed missing
      data, non-disclosure, and no-mutation denials map to the approved
      requirement semantics.
- [x] P2 — Add dedicated denial-audit fault injection for `REQ-AUTH-31` on
      the commit-time deny path.
- [x] Verify focused authorization/timer tests, locked regression, docs, and
      whitespace.

# Review remediations (`50c894e` production-boundary follow-up)

- [x] Reconcile the stale plan statement that authorization denial leaves
      `audit` unchanged; protected work rolls back, then a separate denial
      audit is persisted.
- [x] Refuse `Sessions:TimerPolling:Enabled` outside Development/Testing until
      Worker workload authentication exists; host flag and actor GUID are not
      authentication.
- [x] Expand executable traceability against governing sources without
      manufacturing tests.
- [x] Reconcile `# Blockers` (implementation vs production-enablement vs
      governance vs deferred successors). Keep ADR-015 Proposed.
- [x] Verify focused Worker/timer/authorization tests, locked regression,
      docs, and whitespace. Leave independent review unchecked.

# Review remediations (`9650da9` traceability evidence)

- [x] P2 — Replace generic predecessor evidence phrases with exact
      `TestClass.TestMethod` names, or an explicit unenumerated evidence gap.
      Do not add tests merely to fill rows.

# Current state

Independent review of `c8dc740` against `9650da9` found no remaining
implementation, security, or closure-evidence issue. The timer-lane
Worker/delegation slice is `completed`. Completion does not enable production
timer polling. Production/Staging remain fail-closed until Worker workload
authentication exists. Production Invocation processing remains blocked until
bounded Invocation delegation exists. ADR-015 stays Proposed pending Product
Lead and Architecture Lead approval. GitHub has no attached commit status
checks for `c8dc740`. Next MVP work is a successor slice, not further
timer-lane remediation.

Live model providers, OIDC, Participant UI, hosted HTTP Session-create, and
production-pilot certification remain out of scope.

# Decisions

- Reuse the Session's immutable `session_frozen_policy_snapshots` record via
  `PostgresTrustedSessionBindingSource`. The Worker must not re-resolve mutable
  Agent, Harness, Activity, or Organization configuration at execution time.
- Preserve `FailClosedModelExecutionPort` in this task. Binding and timer
  readiness must not be misreported as provider readiness.
- Keep timer polling behind a distinct explicit capability, default disabled.
  Until Worker workload authentication exists, enabling
  `Sessions:TimerPolling:Enabled` outside Development/Testing refuses startup
  (`PROP-WBT-3`). The host flag and `Sessions:WorkerServiceActorId` are not
  authentication. Keep Invocation processing behind a separate explicit
  capability, default disabled (`PROP-WBT-8`). Until invocation service
  delegation exists, the Invocation flag is a Development/Testing host
  profile only and refuses startup elsewhere. Tests may enable timer polling,
  or Invocation processing, in Development/Testing with synthetic
  provider-independent Session data.
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
  Until Worker workload authentication exists, enabling the flag outside
  Development/Testing refuses Worker startup. The flag is not authentication.
- `PROP-WBT-8` — Require an explicit `Sessions:InvocationProcessing:Enabled`
  host capability, default `false`, before registering the live Invocation
  processor or its mutation ports. Until invocation service delegation exists,
  enabling the flag outside Development/Testing refuses Worker startup. The
  flag is not authorization.
- `PROP-WBT-9` — Unscoped claimable-work aggregates are not an
  authorization-exempt operator read (`REQ-AUTH-12`). Persistence-only and
  timer-only hosts keep `UnknownDurableInvocationWorkStore` and do not sample
  live invocation backlog.
- `PROP-WBT-10` — `session.timer_lane.fire` delegations fail closed after
  `expires_at`. Authorized renewal is deferred. Remaining Session duration
  after expiry cannot re-arm the lane; never fabricate or silently extend
  authority.
- `PROP-WBT-11` — Enabling timer polling or synthetic Invocation processing
  requires an explicit non-empty `Sessions:WorkerServiceActorId`. The compiled
  default is not used. The actor must already exist in IdentityAccess. Real
  workload authentication and provisioning remain a successor task; this slice
  does not claim the timer lane is production-authenticated.
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
  (`PROP-WBT-8`). Review of `f4eff9b` required a production startup refuse and
  removal of the unscoped persistence-only backlog read. The live Invocation
  processor still lacks bounded service delegation; Development/Testing may
  compose it, and that remains a synthetic profile rather than production
  authorization.
- Independent review of `50c894e` (2026-08-18) against `6fb9de5` accepted the
  P2 mapping and denial-audit fault-injection remediations, then a follow-up
  review revised the close verdict: missing workload authentication is a
  production-enablement blocker, the plan still claimed denial left `audit`
  unchanged, the executable table omitted governing-source rows, and
  `# Blockers — None` was misleading. GitHub has no attached commit status
  checks for that SHA.
- Independent review of `c8dc740` (2026-08-18) against `9650da9` found no
  remaining implementation, security, or closure-evidence issue. Exact
  `TestClass.TestMethod` evidence was spot-checked against the repository.
  UI `AC-SESS` rows correctly record an unenumerated evidence gap. Completing
  this slice does not enable production timer polling. GitHub has no attached
  commit status checks for that SHA.

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
| Host flag is treated as production authorization | Refuse Invocation processing and timer polling outside Development/Testing until their production gates exist | Production/Staging `InvalidOperationException`; Testing/Development still compose; CS-only stays idle |
| Persistence-only mode issues an unscoped protected aggregate | Unknown invocation store unless the synthetic Invocation profile is composed | CS-only and timer-only use `UnknownDurableInvocationWorkStore` |

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Baseline repository state | passed | Planned from `e8793e5` |
| Host composition red/green | passed | `WorkerRuntimeTests` 19/19 including Production/Staging refuse of Invocation processing, Testing compose, CS-only/timer-only `UnknownDurableInvocationWorkStore` |
| Timer processor/domain focused tests | passed | `DurableTimerFireProcessorTests` 9/9 including `timer_fire.authority_denied` → `retry_later` |
| PostgreSQL binding/delegation/timer integration | passed | `SessionTimerLaneDelegationTests` including HOL skip of revoked due rows, `PostgresTrustedSessionBindingSourceTests`, `SessionTimerSchedulePersistenceTests` against PostgreSQL 18 |
| Migration and upgrade safety | passed | Additive `0022`–`0024`; Grate expected one-time count 24; populated unbounded `0022` timer-lane rows fail closed on `0023`; bounded `0022` rows apply |
| Architecture/module boundaries | passed | Architecture 31/31 including Worker Dockerfile COPY of IdentityAccess |
| Locked .NET regression | passed | `f4eff9b` remediations: `bash build/scripts/verify-dotnet.sh` **946/946** |
| Documentation | passed | `python3 scripts/check_docs.py` |
| Whitespace | passed | `git diff --check` clean |
| External review remediations (`9ab00a1`) | passed | P1 audit/transaction coordinator; P1 final reauth-before-commit + expiry race; P2 audit delegation reference; P2 required 7-day timer-lane expiry |
| External review remediations (`d175099`) | passed | P1 kernel-authorized issue/revoke with grant `authorization_reference` and caller mutation context; P1 removed `NarrowAllowedAction`; P1 explicit `0022`→`0023` populated fail-closed plus `0024` `grant_id` |
| External review remediations (`58f2595`) | passed | P1 abort caller tx on final auth denial + commit-after-deny test; P1 revoked unbounded `0022` rows upgrade |
| External review remediations (`9da4af5`) | passed | P1 non-cancelable denial rollback + canceled-token commit-after-deny test |
| External review remediations (`90a96f6`) | passed | P1 removed public pre-abort hook; cancel race via test kernel wrapper |
| Independent review (`f5e06e9`) | passed | No blockers. Slice accepted for MVP. Local `940/940` not independently verifiable from GitHub status checks |
| Post-completion Invocation capability remediation | passed | Red: 4 `WorkerRuntimeTests` failed because CS registered live processor/claiming. Green: `Sessions:InvocationProcessing:Enabled` default-off. Runtime 92/92; Architecture 31/31; `check_docs.py`; `git diff --check`; locked **942/942** |
| Review remediations (`f4eff9b`) | passed | P1 Production/Staging startup refuse; P2 unknown store for persistence-only/timer-only. Red 5 failed; green `WorkerRuntimeTests` 19/19; Architecture 31/31; `check_docs.py`; `git diff --check`; locked **946/946** |
| Remaining `f4eff9b` remediations (`6811012` follow-up) | passed | Denial audit after rollback; explicit WorkerServiceActorId; traceability `PROP-WBT-1`–`11`. `SessionTimerLaneDelegationTests` 21/21; `WorkerRuntimeTests` 20/20; Architecture 31/31; `check_docs.py`; `git diff --check`; locked **949/949** |
| Review remediations (`6fb9de5`) | passed | P2 corrected `REQ-AUTH-18`–`REQ-AUTH-22` mappings; P2 commit-time denial-audit fault injection. `SessionTimerLaneDelegationTests` 22/22; `WorkerRuntimeTests` 20/20; Architecture 31/31; `check_docs.py`; `git diff --check`; locked **950/950** |
| Independent review (`50c894e`) | passed (later revised) | P2 remediations accepted, then close verdict revised for production timer-polling gate, traceability coverage, and blockers. Local `950/950` not independently verifiable from GitHub status checks |
| Review remediations (`50c894e` follow-up) | passed | P1 Production/Staging refuse `Sessions:TimerPolling:Enabled`; stale deny-audit plan wording; expanded governing-source table; blockers classified. Red: 2 `WorkerRuntimeTests` (Production/Staging timer polling). Green: `WorkerRuntimeTests` 23/23; `SessionTimerLaneDelegationTests` 22/22; Architecture 31/31; `check_docs.py`; `git diff --check`; locked **953/953** |
| Review remediations (`9650da9` P2) | passed | Documentation-only exact `TestClass.TestMethod` evidence (or explicit unenumerated UI gap). `python3 scripts/check_docs.py`; `git diff --check` |
| Independent review (`c8dc740`) | passed | No blocker. Exact-evidence table accepted. Slice complete; production timer polling remains disabled. Local regression not independently verifiable from GitHub status checks |

# Executable traceability

| Requirement/control | Production symbol/contract | Exact automated evidence or gap | Status |
| --- | --- | --- | --- |
| `REQ-AUTH-1` | Kernel `AuthorizeInTransactionAsync` / `ReauthorizeInTransactionAsync` for `session.timer_lane.fire` on a configured Worker actor; Production/Staging refuse timer polling until workload authentication | `SessionTimerLaneDelegationTests.Due_timer_with_production_binding_admits_one_invocation`; `WorkerRuntimeTests.Worker_refuses_to_start_when_timer_polling_is_enabled_outside_development_and_testing` | Enforced here for synthetic timer fire. Production workload authentication deferred |
| `REQ-AUTH-11` | Explicit `Sessions:WorkerServiceActorId` plus current `session.timer_lane.fire` delegation; compiled default unused; host flag is not authentication | `WorkerRuntimeTests.Worker_refuses_to_start_timer_polling_without_an_explicit_worker_service_actor_id`; `WorkerRuntimeTests.Worker_refuses_to_start_when_timer_polling_is_enabled_outside_development_and_testing`; `SessionTimerLaneDelegationTests.Invalid_delegation_denies_without_mutating_due_work` | Enforced here for timer-lane identity selection. Real workload authentication/provisioning deferred |
| `REQ-AUTH-15` | Same kernel/delegation contract for scheduled timer fire as other protected mutations | `SessionTimerLaneDelegationTests.Admission_authorization_denial_rolls_back_work_and_persists_denial_audit`; `SessionTimerLaneDelegationTests.Commit_reauthorization_denial_rolls_back_success_audit_and_persists_denial_audit` | Enforced here for the timer lane |
| `REQ-AUTH-17` | Service-delegation issue assigns org/session ownership from trusted insert context, not client-supplied locators | `SessionTimerLaneDelegationTests.Issue_records_mutation_coupled_audit_against_the_authorizing_grant`; `SessionTimerLaneDelegationTests.Invalid_delegation_denies_without_mutating_due_work` | Enforced here for timer-lane delegation issue. Hosted HTTP Session-create remains out of scope |
| `REQ-AUTH-18` | Commit-time `ReauthorizeInTransactionAsync` is the last SQL before fire `COMMIT` | `SessionTimerLaneDelegationTests.Revocation_after_admission_and_before_commit_denies_without_invocation`; `SessionTimerLaneDelegationTests.Expiry_after_persistence_and_before_commit_denies_without_invocation`; `SessionTimerLaneDelegationTests.Commit_reauthorization_denial_rolls_back_success_audit_and_persists_denial_audit` | Enforced here |
| `REQ-AUTH-19` | Revoked or expired timer-lane delegation cannot authorize due work | `SessionTimerLaneDelegationTests.Invalid_delegation_denies_without_mutating_due_work` (revoked/expired); `SessionTimerLaneDelegationTests.Revocation_after_admission_and_before_commit_denies_without_invocation`; `SessionTimerLaneDelegationTests.Expiry_after_persistence_and_before_commit_denies_without_invocation` | Enforced here |
| `REQ-AUTH-20` | Missing, unavailable, or inconsistent authorization/binding data fails closed without partial mutation | `SessionTimerLaneDelegationTests.Invalid_delegation_denies_without_mutating_due_work` (missing); `SessionTimerLaneDelegationTests.Historical_schedule_without_delegation_stays_pending`; `PostgresTrustedSessionBindingSourceTests.Missing_snapshot_returns_no_binding`; `PostgresTrustedSessionBindingSourceTests.Ownership_mismatch_returns_no_binding`; `PostgresTrustedSessionBindingSourceTests.Digest_mismatch_returns_no_binding` | Enforced here. Dedicated authorization-kernel unavailability injection is not in this slice |
| `REQ-AUTH-21` | Denial does not disclose internals; reason codes and host-guard copy stay bounded | `SessionTimerLaneDelegationTests.Admission_authorization_denial_rolls_back_work_and_persists_denial_audit`; `WorkerRuntimeTests.Worker_refuses_to_start_when_timer_polling_is_enabled_outside_development_and_testing` | Enforced here for timer-lane deny audit and host-guard copy |
| `REQ-AUTH-22` | Authorization denial does not mutate protected Session/Invocation/schedule/outbox/success-audit state; a separate denial audit may persist after rollback | `SessionTimerLaneDelegationTests.Admission_authorization_denial_rolls_back_work_and_persists_denial_audit`; `SessionTimerLaneDelegationTests.Commit_reauthorization_denial_rolls_back_success_audit_and_persists_denial_audit` | Enforced here |
| `REQ-AUTH-26` | Kernel admission/commit denials produce durable deny audit; poller skips do not | `SessionTimerLaneDelegationTests.Admission_authorization_denial_rolls_back_work_and_persists_denial_audit`; `SessionTimerLaneDelegationTests.Invalid_delegation_denies_without_mutating_due_work` | Enforced here |
| `REQ-AUTH-27` | Deny audit records actor, org, `session.timer_lane.fire`, Session id, deny, reason, correlation/source, delegation when evaluated | `SessionTimerLaneDelegationTests.Admission_authorization_denial_rolls_back_work_and_persists_denial_audit` | Enforced here |
| `REQ-AUTH-28` | Audit history is append-only; denial audit is an insert after rollback, not a rewrite of a success event | `SessionTimerLaneDelegationTests.Commit_reauthorization_denial_rolls_back_success_audit_and_persists_denial_audit`; `AuditAppendOnlyTests.Audit_events_reject_update_and_delete` | Enforced here for timer-lane deny-after-rollback. Schema append-only is satisfied by predecessor `AuditAppendOnlyTests.Audit_events_reject_update_and_delete` |
| `REQ-AUTH-29` | Audit/readiness/errors omit credentials, connection secrets, and protected payloads | `WorkerRuntimeTests.Worker_refuses_to_start_when_timer_polling_is_enabled_outside_development_and_testing`; `WorkerRuntimeTests.Worker_ready_copy_names_timer_polling_when_the_capability_is_enabled` | Enforced here for Worker host-guard and readiness copy |
| `REQ-AUTH-30` | Positive and negative timer-lane authorization coverage including wrong org/session/service/action | `SessionTimerLaneDelegationTests.Due_timer_with_production_binding_admits_one_invocation`; `SessionTimerLaneDelegationTests.Invalid_delegation_denies_without_mutating_due_work` | Enforced here for the timer lane only |
| `REQ-AUTH-31` | Success audit/outbox share the fire transaction and roll back on deny; deny audit is a later audit-only commit; denial-audit insert failure throws after rollback | `SessionTimerLaneDelegationTests.Commit_reauthorization_denial_rolls_back_success_audit_and_persists_denial_audit`; `SessionTimerLaneDelegationTests.Denial_audit_failure_after_commit_reauthorization_deny_does_not_mutate_or_leave_audit`; `AuditOutboxFaultInjectionTests.Audit_failure_rolls_back_version_and_outbox` | Enforced here |
| `AC-AUTH-16` | Background timer fire uses explicit service actor, trusted ownership, and current delegation | `SessionTimerLaneDelegationTests.Due_timer_with_production_binding_admits_one_invocation`; `SessionTimerLaneDelegationTests.Invalid_delegation_denies_without_mutating_due_work` | Enforced here for synthetic timer fire. Production workload authentication deferred |
| `AC-AUTH-17` | Concurrent/race authorization does not admit unauthorized timer work; revoked rows do not HOL-block others | `SessionTimerLaneDelegationTests.Revocation_after_admission_and_before_commit_denies_without_invocation`; `SessionTimerLaneDelegationTests.Revoked_due_row_does_not_head_of_line_block_another_session` | Enforced here |
| `REQ-RSC-15`–`REQ-RSC-20` | Immutable frozen policy snapshot, digest, and no rewrite of resolved configuration | `FrozenRuntimePolicySnapshotTests.Canonical_snapshot_round_trips_enabled_timer_policy`; `FrozenRuntimePolicySnapshotTests.Snapshot_parse_rejects_digest_mismatch`; `FrozenRuntimePolicyResolverTests.Resolved_policy_is_immutable_after_source_collection_mutation`; `FrozenRuntimePolicyResolverTests.Policy_digest_is_stable_for_identical_effective_values`; `PostgresTrustedSessionBindingSourceTests.Rehydrates_exact_immutable_policy_and_protected_refs`; `PostgresTrustedSessionBindingSourceTests.Digest_mismatch_returns_no_binding` | Satisfied by predecessor freeze/rehydration; Worker timer path consumes that snapshot |
| `REQ-RSC-24` | Missing/mismatched binding fails closed and does not admit timer work | `PostgresTrustedSessionBindingSourceTests.Missing_snapshot_returns_no_binding`; `PostgresTrustedSessionBindingSourceTests.Digest_mismatch_returns_no_binding`; `SessionTimerSchedulePersistenceTests.Missing_binding_does_not_cancel_the_due_schedule` | Enforced here on the Worker timer path |
| `REQ-RSC-28` | Worker loads binding from trusted PostgreSQL Session state, not work-payload substitutes | `PostgresTrustedSessionBindingSourceTests.Rehydrates_exact_immutable_policy_and_protected_refs`; `PostgresTrustedSessionBindingSourceTests.Cross_organization_session_id_returns_no_binding`; `WorkerRuntimeTests.Worker_registers_timer_processor_only_when_timer_polling_is_explicitly_enabled` | Enforced here |
| `REQ-RSC-47`–`REQ-RSC-55` | Frozen P0 Invocation/Decision/timer-lane/output policy in the Session snapshot | `FrozenRuntimePolicyResolverTests.Resolve_produces_frozen_policy_matching_baseline_with_enabled_timer_lane`; `FrozenRuntimePolicyResolverTests.Resolve_fails_closed_for_deferred_trigger_not_supported_by_p0_kernel`; `P0TextSessionRuntimeCapabilityPolicyTests.P0_kernel_does_not_support_deferred_capabilities`; `PostgresTrustedSessionBindingSourceTests.Rehydrates_exact_immutable_policy_and_protected_refs` | Satisfied by predecessor. This slice does not widen frozen policy |
| `AC-RSC-26`–`AC-RSC-28` | Frozen assessment/timer/output kinds without enabling deferred capabilities | `P0TextSessionRuntimeCapabilityPolicyTests.P0_kernel_exposes_required_explicitly_disabled_capabilities_for_mvp_profile`; `P0TextSessionRuntimeCapabilityPolicyTests.P0_kernel_supports_optional_single_timer_lane_without_modeling_operational_enablement`; `FrozenRuntimePolicyResolverTests.Resolve_fails_closed_for_deferred_decision_type` | Satisfied by predecessor |
| `REQ-SESS-61`–`REQ-SESS-70` | Structured Invocation/Decision identity, XOR outcome, trusted context, bounds | `TrustedTriggerAdmissionTests.Accepting_participant_message_on_active_session_admits_stable_invocation`; `TrustedTriggerAdmissionTests.Unknown_or_prohibited_triggers_fail_closed`; `InvocationExecutionTests.Successful_completion_records_exactly_one_decision_and_no_execution_outcome`; `InvocationExecutionTests.Infrastructure_failure_records_an_execution_outcome_and_never_a_decision`; `SessionTimerLaneDelegationTests.Due_timer_with_production_binding_admits_one_invocation` | Satisfied by predecessor. This slice admits at most one timer-triggered Invocation |
| `REQ-SESS-71`–`REQ-SESS-74` | Next-timer replacement, validation, atomic persist, authoritative delay | `OneLaneTimerSchedulerTests.Accepted_no_action_replacement_supersedes_the_pending_default`; `OneLaneTimerSchedulerTests.Out_of_bounds_or_malformed_delay_is_rejected_without_schedule_effect`; `SessionTimerSchedulePersistenceTests.Accepted_replacement_persists_superseded_as_replaced_and_one_pending_successor` | Satisfied by predecessor |
| `REQ-SESS-75` | Due timer fire authorizes current bounded delegation inside the commit boundary | `PostgresFireDueTimerCoordinator.TryFireNextDueAsync`; `SessionTimerLaneDelegationTests.Due_timer_with_production_binding_admits_one_invocation` | Enforced here in Development/Testing only until workload authentication exists |
| `REQ-SESS-76` | Enabled timer lane arms when Session enters `Active` | `OneLaneTimerSchedulerTests.Create_active_with_enabled_lane_arms_one_pending_default`; `SessionTimerSchedulePersistenceTests.Insert_active_persists_the_default_pending_schedule` | Satisfied by predecessor |
| `REQ-SESS-77` | History distinguishes requested vs accepted vs fired timer facts | `OneLaneTimerSchedulerTests.Omitted_or_rejected_timer_keeps_the_pending_default`; `OneLaneTimerSchedulerTests.Accepted_timer_decision_installs_the_sole_successor_instead_of_default`; `OneLaneTimerSchedulerTests.Fire_due_admits_one_timer_invocation_and_retries_reconcile` | Satisfied by predecessor |
| `AC-SESS-33`–`AC-SESS-37` | Invocation/Decision participant-visible states | Exact TestClass.TestMethod evidence is an unenumerated gap in this Worker-slice table (UI/synthetic Participant surface). Not applicable to hosted timer polling | Not applicable to this Worker slice |
| `AC-SESS-38`–`AC-SESS-40` | Accepted next-timer replacement UX/domain | Domain: `OneLaneTimerSchedulerTests.Accepted_no_action_replacement_supersedes_the_pending_default`; `SessionTimerSchedulePersistenceTests.Accepted_replacement_persists_superseded_as_replaced_and_one_pending_successor`. Exact UI TestClass.TestMethod evidence is an unenumerated gap in this table | Domain methods named at left; no UI change in this slice |
| `AC-SESS-41` | Pause and cutoff suppress the timer safely | `OneLaneTimerSchedulerTests.Pause_suspends_remaining_delay_and_resume_recomputes_due_at`; `OneLaneTimerSchedulerTests.Cutoff_cancels_the_pending_event_and_prevents_fire_or_rearm`; `SessionTimerSchedulePersistenceTests.Paused_session_is_not_claimed_even_when_fire_at_is_past` | Satisfied by predecessor; preserved here |
| `PROP-WBT-1` | Per-Session `service_delegations` linked from `session_timer_schedules.timer_lane_delegation_id` | `SessionTimerLaneDelegationTests.Due_timer_with_production_binding_admits_one_invocation` | Enforced here |
| `PROP-WBT-2` | No identifier-only backfill | `SessionTimerLaneDelegationTests.Invalid_delegation_denies_without_mutating_due_work` (`missing`); `SessionTimerLaneDelegationTests.Historical_schedule_without_delegation_stays_pending` | Enforced here |
| `PROP-WBT-3` | `Sessions:TimerPolling:Enabled` default false; Production/Staging refuse until workload authentication | `WorkerRuntimeTests.Worker_keeps_invocation_processing_idle_when_only_a_sessions_connection_string_is_set`; `WorkerRuntimeTests.Worker_refuses_to_start_when_timer_polling_is_enabled_outside_development_and_testing`; `WorkerRuntimeTests.Worker_registers_timer_processor_when_timer_polling_is_enabled_in_testing` | Enforced here |
| `PROP-WBT-4` | Primary DB clock/transaction for due, lifecycle, delegation, commit | `SessionTimerLaneDelegationTests.Expiry_after_persistence_and_before_commit_denies_without_invocation` | Enforced here |
| `PROP-WBT-5` | Required 7-day max `expires_at` | `SessionTimerLaneDelegationTests.Timer_lane_fire_issue_requires_bounded_expiry` | Enforced here |
| `PROP-WBT-6` | Issue/revoke kernel-authorized against org grant | `SessionTimerLaneDelegationTests.Issue_records_mutation_coupled_audit_against_the_authorizing_grant`; `SessionTimerLaneDelegationTests.Revoke_records_mutation_coupled_audit_against_the_authorizing_grant` | Enforced here |
| `PROP-WBT-7` | No in-place `allowed_action` rewrite | Production API has no `NarrowAllowedAction`; wrong-action case in `SessionTimerLaneDelegationTests.Invalid_delegation_denies_without_mutating_due_work` | Enforced here |
| `PROP-WBT-8` | Invocation processing default-off; Production/Staging refuse | `WorkerRuntimeTests.Worker_refuses_to_start_when_invocation_processing_is_enabled_outside_development_and_testing` | Enforced here. Full Invocation delegation is successor work |
| `PROP-WBT-9` | Persistence-only/timer-only use `UnknownDurableInvocationWorkStore` | `WorkerRuntimeTests.Worker_keeps_invocation_processing_idle_when_only_a_sessions_connection_string_is_set`; `WorkerRuntimeTests.Worker_registers_timer_processor_only_when_timer_polling_is_explicitly_enabled` | Enforced here |
| `PROP-WBT-10` | Expiry fail-closed; renewal deferred | `SessionTimerLaneDelegationTests.Expiry_after_persistence_and_before_commit_denies_without_invocation` | Enforced here; no renewal command in this slice |
| `PROP-WBT-11` | Explicit Worker actor id for synthetic protected lanes | `WorkerRuntimeTests.Worker_refuses_to_start_timer_polling_without_an_explicit_worker_service_actor_id` | Enforced here. Workload authentication/provisioning deferred |

# Blockers

### Implementation

None for the current Development/Testing timer-lane path.

### Production enablement

- Worker workload authentication and provisioning are absent. ADR-002 requires the Worker to authenticate as its service identity. `Sessions:WorkerServiceActorId` is trusted deployment configuration only. Production and Staging refuse `Sessions:TimerPolling:Enabled` until a successor implements workload authentication.
- Live Invocation processing still lacks bounded service delegation and remains Development/Testing-only.

### Governance

- ADR-015 remains Proposed pending Product Lead and Architecture Lead approval. Independent implementation review is not ADR approval.

### Intentionally deferred successor work

- OIDC / cloud workload identity
- Authorized timer-lane delegation renewal
- Live model providers, hosted HTTP Session-create, Participant UI, production-pilot certification

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
- [x] Production and other non-test environments refuse
      `Sessions:InvocationProcessing:Enabled` until invocation delegation exists
- [x] Persistence-only and timer-only hosts do not issue unscoped invocation
      claimable-work aggregates
- [x] Production and other non-test environments refuse
      `Sessions:TimerPolling:Enabled` until Worker workload authentication exists
- [x] Independent backend and security/privacy review of the `c8dc740`
      exact-evidence traceability cleanup (against `9650da9`)
