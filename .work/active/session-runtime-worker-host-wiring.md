---
id: session-runtime-worker-host-wiring
status: planned
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

- [ ] Inventory current Worker composition (`IdleDurableInvocationWorkProcessor`,
      sampling store, missing timer processor, ready copy)
- [ ] Red: hosted content-phase complete without fragment persist must fail
- [ ] Green: persist-through-coordinator then allow real processor registration
- [ ] Due-timer poll with poison-row ACK for permanent lifecycle ineligible
- [ ] Ready-copy and composition tests; focused then proportionate regression

# Current state

Foundation is complete in `structured-agent-runtime-sync` (`e966390`
**approved**, 0 P0 / 0 P1 / 0 P2). This file is the future Worker host
boundary; do not start until explicitly prioritized. Worker host still
registers `IdleDurableInvocationWorkProcessor`.
`DurableInvocationWorkProcessor.PublishDeltaAsync` mutates in-memory
`SessionRuntime` only. `DurableTimerFireProcessor` is test-composed, not
hosted. `/health/ready` says the Worker is accepting work claims when the
claim gate is open. 30-second claim lease vs long Execute/stream needs a
heartbeat design before live claiming.

# Decisions

- Keep Idle until publication persist is in the hosted content loop (review
  High residual from the foundation pass).

# Findings / deviations

- None yet.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Host stays Idle until persist is wired | pending | |
| Fragment/seal persist before work complete | pending | |
| Timer `lifecycle_ineligible` is not infinite retry | pending | |
| Ready copy matches actual claiming | pending | |

# Blockers

None. Do not start until this task is explicitly prioritized.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [ ] Governing specifications were rechecked
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
