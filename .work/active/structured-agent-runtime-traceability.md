---
id: structured-agent-runtime-traceability
parent_task: structured-agent-runtime-sync
status: completed
created: 2026-08-11
updated: 2026-08-18
---

# Structured Agent runtime — executable traceability and threat model

Companion matrix for `.work/active/structured-agent-runtime-sync.md`. Maps each
in-scope obligation to authoritative aggregates, transaction boundaries,
authorization, idempotency keys, browser-safe projections, tests, and security
controls. Implementation surfaces below exist unless a residual is named.
Verification cells are not a completion claim: they name the current evidence
and must stay aligned with outstanding labs. Authoritative feature-spec Status
remains `Partial` or `Gap` where host, provider, or production-UI gates are
open.

This companion was reconciled on 2026-08-18 through the approved Worker host,
production HTTP SSE adapter, and Session-scoped subject/binding slices ending
at `eea597a`. It is complete as a traceability snapshot of the implemented
Session runtime foundation; the residual risks below remain later production
gates and do not become completed behavior through this record.

## Multi-channel output decision dependency

Cleared on 2026-08-14. ADR-014
(`docs/architecture/decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md`)
approves a versioned Decision envelope with a P0 compatibility profile.
`agent-decision.v2` is the implemented successor schema and `agent-decision.v1`
remains immutable historical evidence, dual-read as `respond`+one `message` or
`no_action`+zero outputs. Runtime allocates `agent_output_id`. Effective
audience is derived, not model-authored. P0 delivery for the single message
output remains ADR-011. Voice, rich rendering, and extra audiences stay
disabled.

## Duration encoding (interim default, documented)

Relative timer delays use **ISO 8601 duration** strings restricted to positive
`PnDTnHnMnS` forms without year/month/week components or fractional seconds.
Examples: `PT30S`, `PT5M`, `PT1H`, `PT24H`. Wire shape is validated by
`iso8601_positive_duration`; semantic bounds (`PT1S`..`PT24H`) are enforced by
`DurationBoundarySemanticsTests` and duration boundary fixtures.

## Protected assets

| Asset | Owner | Browser exposure |
| --- | --- | --- |
| Trusted trigger facts | Sessions runtime | None |
| Invocation context and provider I/O | Sessions runtime / protected store | None |
| Raw Agent Decision and timer request | Sessions runtime | None |
| Validation/effect provenance | Sessions runtime | Bounded safe category only |
| Agent message fragments | Sessions runtime | `text_delta` via SSE only after commit |
| Credential binding identity | Configuration / SecretSource | None |
| Session ownership chain | PostgreSQL authoritative records | Opaque session locator only |

## Threat model summary (STRIDE + privacy)

| Threat | Entry | Control | Verification |
| --- | --- | --- | --- |
| Spoofing (forged trigger/scope) | API, worker poll, synthetic adapter | Trusted adapter admission; server-derived ownership; Session-scoped current relationship and commit-time reauthorization | Negative admission matrix; cross-org/session isolation; guessed-Session and relationship-revoke tests |
| Tampering (stale order, duplicate effect) | Retries, concurrent workers | `session_sequence`, expected version, scoped idempotency, at-most-one effect constraints | Concurrency/restart PostgreSQL tests |
| Repudiation | Sensitive mutations | Durable audit + manifest append with correlation | Audit failure injection |
| Information disclosure | SSE, logs, metrics, exports | Browser-safe projections; bounded labels; no credentials in records | Log/telemetry snapshot tests; DOM/storage inspection |
| Denial of service | Timer storms, fragment floods | Positive budgets, backpressure, fair claiming | Partial — bounded backpressure/fair-claim tests; timer-storm/load lab pending |
| Elevation (prohibited Decision) | Model output | Independent validation; frozen capability matrix | Policy rejection tests |
| Privacy misuse (cross-tenant retrieval) | Guessed IDs and stolen SSE cursors | Composite scope on every query; `Last-Event-ID` never establishes identity or authority | Isolation matrix; cross-Session relationship and cursor tests |

## Requirement traceability matrix

| Obligation ID | Authoritative aggregate / record | Transaction boundary | Actor / service auth | Idempotency / order key | Browser-safe projection | Positive test | Negative / failure test |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `REQ-RSC-46`, `AC-RSC-25` | Resolved deployment + opaque credential binding ref | Pre-provider preflight (read-only) | Service delegation at worker claim | N/A (binding frozen in Session config) | None | Binding resolves for trusted org | Missing/revoked/wrong-org/provider mismatch fails closed |
| `REQ-RSC-47`–`55`, `AC-RSC-26`–`28` | Frozen runtime policy snapshot on Session | `InsertActiveAsync` writes runtime, starting participant relationship, and immutable policy snapshot atomically | Trusted configuration resolver; API rehydrates via `PostgresTrustedSessionBindingSource`; Worker remains fail-closed | Config digest + cohort version | None | Snapshot round-trip and hosted API binding rehydration; P0 output-kind freeze | Digest mismatch, lower-scope widen, drift, extra output kind, and unsupported populated `0020`→`0021` fail closed |
| `REQ-SESS-61`, `62`, `67`, `68`, `SESS-DEC-14`–`15`, `20` | `TrustedTriggerV1` → `AgentInvocation` admission | Single admission TX | Participant/service at boundary | `(session_id, trigger_family, trigger_id, purpose, policy_digest)` | None | Participant/opening/closing/timer admitted | Unknown/forged/prohibited/stale/cutoff denied |
| `REQ-SESS-63`, `69`, `SESS-DEC-16`, `21` | Invocation + execution attempts | Claim → provider (ext) → outcome TX | Worker service identity | Invocation id + attempt ordinal | None | One Decision on success | Timeout/malformed → outcome, no Decision |
| `REQ-SESS-64`–`67`, `70`, `78`–`85`, `AC-SESS-42`–`48`, `SESS-DEC-17`–`19`, `23`, `29`–`35` | Successor Decision envelope + validation/effect; v1 dual-read | Decision commit; independent per-item output vs action validation; separate effect TX | Reauth at effect | Decision id + output id + effect idempotency | `session.agent.work.v1` resolved state | `respond`+one message / `no_action`+zero outputs; timer action independent; mixed valid message + rejected voice | Schema-invalid → execution outcome, no Decision; schema-valid `respond` + zero valid outputs → Decision rejection, not `no_action`; typed `voice` parses then fails P0 profile; extra message/audience/id rejected per item without voiding siblings |
| `REQ-SESS-55`–`60`, `AC-SESS-32`, ADR-011 | Fragments + completion linked to output id; Session-scoped actor relationship and frozen policy snapshot for replay | Per-fragment TX; completion TX; replay hydration Repeatable Read; Worker persists each fragment/seal through `PostgresPublishAgentResponseCoordinator` before claim completion | Service at publish; hosted HTTP SSE resolves `(actor, untrustedSessionId)` and reauthorizes through ADR-002 on subscribe, replay, and held connection at least every 60 seconds; OIDC remains open | `(agent_output_id, agent_message_id, fragment_sequence)`; reconnect `Last-Event-ID` only if it matches a persisted fragment or seal sequence (not delivery acknowledgement); monotonic relationship version | `session.agent.fragment.v1`, `session.agent.complete.v1` | Ordered PostgreSQL replay; persisted Worker publication; Session-scoped subscribe/replay/hold; relationship and grant revocation | Gap/duplicate/cutoff; stolen cursor; guessed Session; cross-Session role mismatch; stale set/revoke; revoke-before-first-set; persist/lease/crash failure; fragment bounds and prefix divergence |
| `REQ-SESS-71`–`77`, `AC-SESS-38`–`41`, `SESS-DEC-24`–`28` | `TimerScheduleRevision` on `SessionRuntime`; PostgreSQL `session_timer_schedules` via `0016` | Domain replacement/fire plus `PostgresFireDueTimerCoordinator` / lifecycle persist | Scheduler service (Worker due-claim polling remains idle) | `expected_schedule_revision` | None (UI-SESS-DEC-14) | `OneLaneTimerSchedulerTests` and persist/due-claim tests | Two pending / double fire / stale revision / pause fire / cutoff rearm; `BudgetExhausted` ACK |
| `UI-SESS-DEC-13` | Turn + work projection | Effect commit couples turn terminalization | N/A | Turn/slot id | Work SSE + optional neutral status | No-action resolves without Agent message | No raw `no_action` in DOM |
| `UI-SESS-DEC-14` | Timer lane internal | Schedule TX | Scheduler | Revision + session order | Agent work states only when visible work | Timer-triggered message path | No synthetic Participant message |
| `UI-SESS-DEC-15` | Browser-safe projection | Effect/SSE only | N/A | Session cursor | Existing message/no-action states | Envelope internals hidden | No voice/shared-workspace/reviewer UI |
| ADR-005 handoff | Session + manifest | Terminal seal TX | Service | Terminal intent idempotency | Terminal SSE | E2E readiness → Active → terminal | Seal fault → honest Completing |
| Security isolation | All scoped tables | Every TX | ADR-002 kernel | Composite FK + query scope | Safe errors only | Own-scope CRUD | Cross-scope deny + non-disclosing |

## Module ownership (implemented foundation)

| Component | Project | Forbidden imports |
| --- | --- | --- |
| Domain rules | `FlexAgent.Sessions` | HTTP, Npgsql, provider SDKs |
| Application | `FlexAgent.Sessions` | Direct table access outside repos |
| PostgreSQL repos | `FlexAgent.Sessions.Infrastructure` | Domain logic in SQL |
| Provider port | `FlexAgent.Sessions` abstractions | Provider types in contracts |
| Worker handler | `FlexAgent.Worker` + Sessions app | Unscoped repositories |
| Synthetic adapter | `FlexAgent.SyntheticBrowser` | Domain authority / DB writes |
| Browser contracts | `FlexAgent.Contracts` | Authorization secrets |

## Runtime contracts

| Schema | Category | Browser-safe |
| --- | --- | --- |
| `trusted-trigger.v1` | Admission input | No |
| `trusted-trigger-provenance.v1` | Invocation-embedded trigger facts (scope-free) | No |
| `agent-invocation.v1` | Runtime record (single authoritative `ownership`) | No |
| `agent-invocation-execution-outcome.v1` | Invocation terminal outcome | No |
| `agent-invocation-execution-attempt.v1` | Runtime record | No |
| `agent-decision.v1` | Historical structured control (immutable) | No |
| `agent-decision.v2` | P0 output/action envelope | No |
| `decision-validation-effect.v1` | Runtime record | No |
| `timer-schedule-revision.v1` | Scheduler record | No |
| `sse-event.v1` (extended) | Transport | Yes — adds `complete`, `work` |

Protected runtime TypeScript mirrors live in
`web/src/contracts/internal-runtime.v1.ts` and
`web/src/contracts/internal-runtime.v2.ts`. Participant-safe browser
projections remain in `web/src/contracts/v1.ts`.

## Residual risks (tracked)

- Production OIDC and live provider adapters remain out of scope; deterministic
  fake adapter proves the model-execution boundary only.
- Numeric timer policy values come from frozen configuration fixtures in tests,
  not from code constants.
- With a Sessions connection string, the Worker hosts
  `DurableInvocationWorkProcessor` with PostgreSQL claim and publication
  persistence. Its model port and trusted binding source remain fail-closed,
  so live provider execution is not enabled. The lease heartbeat covers
  persisted content fragments, not a future long provider `ExecuteAsync` call.
- Hosted due-timer polling remains Idle until a real trusted Worker binding
  source exists. Test-composed due processing ACKs true
  `lifecycle_ineligible`; missing binding leaves the schedule pending.
- Production HTTP `/sessions/{id}/events`, ADR-002 enforcement,
  Session-scoped relationship resolution, frozen-policy rehydration, and
  60-second held revalidation are implemented. `REQ-SESS-59` remains Partial
  because production OIDC and the Participant UI switch are still open.
- Organization-wide in-flight stream caps, buffered-uncommitted provider byte
  caps beyond assembled size, and generation-timeout content cancellation remain
  later.
- Host OTLP/Collector export and a concurrent timer-storm load lab remain later.
- Backup/restore and authorized reconstruction labs have not been run.
- Voice, Interaction Controller, TTS, and rich-content UI remain P2.

## Reconciliation evidence

| Slice | Status | Evidence |
| --- | --- | --- |
| Structured runtime foundation | completed and frozen | `.work/active/structured-agent-runtime-sync.md`; approved `e966390` |
| Worker host publication boundary | completed and approved | `.work/active/session-runtime-worker-host-wiring.md`; `DurableInvocationWorkCrashRecoveryTests` 13/13; approval recorded in `bc3f3e0` |
| Production HTTP SSE adapter | completed and approved | `.work/active/session-runtime-production-http-sse.md`; authorized PostgreSQL replay and held revalidation; freeze `735c203` |
| Session-scoped relationship and policy rehydration | completed and approved | `.work/active/session-runtime-subject-binding-rehydration.md`; local locked .NET regression 910/910; approval record `eea597a` |
| Authoritative requirement status | remains honest | `REQ-SESS-59` remains Partial for OIDC and production Participant UI; live providers, hosted timer polling, load/OTLP, and backup/restore remain open |

No blockers remain for this traceability reconciliation. Future production
gates should update their own task records and the authoritative specification
status rows; this completed snapshot must not be used to imply those gates
already passed.
