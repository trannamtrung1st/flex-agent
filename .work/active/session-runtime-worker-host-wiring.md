---
id: session-runtime-worker-host-wiring
status: completed
created: 2026-08-17
updated: 2026-08-17
predecessor: structured-agent-runtime-sync
---

# Goal

Wire the production Worker host to the already-tested Sessions claim, Invocation,
Decision, ADR-011 publication, and one-lane timer-fire application so durable
work is processed without fabricating Decisions or completing claims before
fragments are in PostgreSQL.

Do not enable the live processor until content-phase persist uses
`PostgresPublishAgentResponseCoordinator` (or an equivalent persist port) per
fragment and seal.

# Governing sources

- `.work/active/structured-agent-runtime-sync.md` — completed foundation;
  this task owns the host composition that was deferred from that file
- `docs/requirements/features/session-text-lifecycle.md` — `REQ-SESS-55`–
  `REQ-SESS-60`, `REQ-SESS-63`, `REQ-SESS-69`, `REQ-SESS-71`–`REQ-SESS-77`,
  `AC-SESS-32`, `AC-SESS-38`–`AC-SESS-41` (hosted paths still Partial)
- `docs/architecture/decisions/ADR-011-participant-visible-agent-response-streaming.md`
- `docs/architecture/decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md`
- `docs/architecture/decisions/ADR-013-agent-requested-next-timer-replacement.md`
- `docs/architecture/session-runtime-contract.md` — `SESS-DEC-9`, `SESS-DEC-13`,
  `SESS-DEC-16`, `SESS-DEC-21`, `SESS-DEC-26`

# Scope

## In

- Compose `DurableInvocationWorkProcessor` on the Worker only together with
  PostgreSQL publication persist (`PostgresPublishAgentResponseCoordinator` /
  `TrySaveAgentResponsePublicationAsync`) for every displayable delta and seal
- Keep Idle until that persist path is in the same host graph
- Bounded claim polling of `invocation.execute` with existing lease/SKIP LOCKED
  fairness; do not steal long Execute/stream without a heartbeat design
- Wire `DurableTimerFireProcessor` / due-claim; ACK or terminalize
  `timer_fire.lifecycle_ineligible` instead of default `retry_later`
- Make `/health/ready` describe the actual gate (loop running / not shutting
  down), not “accepting work claims” while Idle or before persist is wired
- Frozen-policy / credential-binding rehydration required before live model
  calls; fail closed; no credentials in Session records or logs
- Focused tests: host composition, persist-before-complete, cancel/lease,
  poison-row ACK, ready-copy honesty

## Out

- Production HTTP `/sessions/{id}/events` (see
  `session-runtime-production-http-sse`)
- Live OpenAI/Azure providers and `GATE-STACK-PROVIDERS`
- OIDC, backup/restore/export labs, OTLP Collector, timer-storm load lab
- Rewriting frozen migrations `0005`–`0019`
- Text Session UI polish unless a host-wiring defect appears

# Plan

- [x] Inventory current Worker composition (`IdleDurableInvocationWorkProcessor`,
      sampling store, missing timer processor, ready copy)
- [x] Red: hosted content-phase complete without fragment persist must fail
- [x] Green: persist-through-coordinator then allow real processor registration
- [x] Due-timer poll with poison-row ACK for permanent lifecycle ineligible
- [x] Ready-copy and composition tests; focused then proportionate regression

# Current state

Worker host registers `IdleDurableInvocationWorkProcessor` and
`IdleDurableTimerFireProcessor` when `ConnectionStrings:Sessions` is absent.
When that connection string is present, the same composition graph registers
`DurableInvocationWorkProcessor` together with
`PostgresPublishAgentResponseCoordinator` as `IAgentResponsePublicationPersistPort`,
`PostgresDurableInvocationWorkStore`, `FailClosedModelExecutionPort`, and
`FailClosedTrustedSessionBindingSource`. Hosted due-timer polling stays
`IdleDurableTimerFireProcessor` until a real binding source exists.
Content-phase complete is blocked until fragment and seal persist succeed.
`/health/ready` reports loop running and whether durable claiming is enabled.

# Decisions

- Register the live invocation processor only in the same host graph as
  `PostgresPublishAgentResponseCoordinator` persist.
- Heartbeat: renew the 30-second `invocation.execute` lease after each
  persisted fragment (`TryRenewClaimLeaseAsync`). A failed renew does not
  `MarkCompleted`.
- Hosted model port is fail-closed (`provider_unavailable`); live OpenAI/Azure
  remain out of scope. Credential binding is resolved from Worker configuration
  opaque refs only, never from Session rows or logs.
- `timer_fire.lifecycle_ineligible` is acknowledged after a loaded Session
  returns that outcome and the due revision is persist-cancelled. Missing
  binding does not cancel the schedule. Hosted due-timer polling stays Idle
  while `FailClosedTrustedSessionBindingSource` is the Worker binding source,
  so fail-closed rehydration cannot HOL-block or destroy due timers.

# Findings / deviations

- Frozen-policy rehydration from configuration source payloads is still
  fail-closed: `ITrustedSessionBindingSource` on the Worker returns null, so
  `LoadAsync` cannot reconstruct `TrustedSessionBinding.Policy` from PostgreSQL
  identity rows alone. Invocation claims then `retry_later`. Hosted due-timer
  polling stays Idle until a real binding source exists. Missing binding on a
  test-composed due-claim rolls back as `stale_revision` and leaves the
  schedule pending. True `timer_fire.lifecycle_ineligible` after a loaded
  Session still persist-cancels the due revision.
- Lease heartbeat covers content-phase fragment persist, not a long control
  `ExecuteAsync` call. Fail-closed Execute returns immediately; a future live
  provider still needs Execute-duration heartbeat or a longer lease.
- In-memory unit tests use `PassThroughAgentResponsePublicationPersistPort`;
  PostgreSQL proof uses the coordinator. The processor still applies domain
  handlers locally, then persists through the port.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Host stays Idle until persist is wired | passed | No Sessions CS: `IdleDurableInvocationWorkProcessor` + idle timer. With CS: live processor and `PostgresPublishAgentResponseCoordinator` as persist (`WorkerRuntimeTests` 11/11 including composition). |
| Fragment/seal persist before work complete | passed | Red: `Content_phase_does_not_complete_the_claim_when_fragment_persist_fails` (RetryLater, claim not completed). Green: Postgres `Processor_persists_fragments_through_the_publication_coordinator_before_completing_work` (Published, fragment+seal in PostgreSQL, work completed). Sessions processor tests 27/27; claim tests included in 23/23 with timer class. |
| Timer `lifecycle_ineligible` is not infinite retry | passed | Processor ACK for loaded-session `lifecycle_ineligible`. Missing binding no longer cancels: `Missing_binding_does_not_cancel_the_due_schedule`. Hosted due-timer processor stays Idle with fail-closed bindings. |
| Ready copy matches actual claiming | passed | Idle: "Worker loop is running. Durable work claiming is not enabled." Live graph: "...claiming is enabled." Shutdown: "Worker is shutting down." No "accepting work claims". |
| Focused Sessions | passed | `FlexAgent.Sessions.Tests` 413/413 |
| Architecture | passed | `FlexAgent.Architecture.Tests` 30/30 |
| Runtime | passed | `FlexAgent.Runtime.Tests` 74/74 |
| Crash/recovery claim | passed | `DurableInvocationWorkCrashRecoveryTests` 8/8 (with claim+timer classes: 31/31 after High remediations rebuild) |
| Whitespace | passed | `git diff --check` clean |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
