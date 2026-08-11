---
id: structured-agent-runtime-traceability
parent_task: structured-agent-runtime-sync
status: active
created: 2026-08-11
updated: 2026-08-11
---

# Structured Agent runtime — executable traceability and threat model

Companion matrix for `.work/active/structured-agent-runtime-sync.md`. Maps each
in-scope obligation to authoritative aggregates, transaction boundaries,
authorization, idempotency keys, browser-safe projections, tests, and security
controls. Implementation surfaces are **planned targets** until linked evidence
exists in this task's verification table.

## Duration encoding (interim default, documented)

Relative timer delays use **ISO 8601 duration** strings restricted to positive
`PnDTnHnMnS` forms without negative components, year/month/week components, or
fractional seconds. Examples: `PT30S`, `PT5M`, `PT1H`. Wire bound: 1 second
through 24 hours (`PT1S` … `PT24H`). Overflow, negative, and ambiguous numeric
encodings are rejected at schema validation.

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
| Spoofing (forged trigger/scope) | API, worker poll, synthetic adapter | Trusted adapter admission; server-derived ownership; commit-time reauthorization | Negative admission matrix; cross-org/session isolation tests |
| Tampering (stale order, duplicate effect) | Retries, concurrent workers | `session_sequence`, expected version, scoped idempotency, at-most-one effect constraints | Concurrency/restart PostgreSQL tests |
| Repudiation | Sensitive mutations | Durable audit + manifest append with correlation | Audit failure injection |
| Information disclosure | SSE, logs, metrics, exports | Browser-safe projections; bounded labels; no credentials in records | Log/telemetry snapshot tests; DOM/storage inspection |
| Denial of service | Timer storms, fragment floods | Positive budgets, backpressure, fair claiming | Load/backpressure harness |
| Elevation (prohibited Decision) | Model output | Independent validation; frozen capability matrix | Policy rejection tests |
| Privacy misuse (cross-tenant retrieval) | Guessed IDs | Composite scope on every query | Isolation matrix |

## Requirement traceability matrix

| Obligation ID | Authoritative aggregate / record | Transaction boundary | Actor / service auth | Idempotency / order key | Browser-safe projection | Positive test | Negative / failure test |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `REQ-RSC-46`, `AC-RSC-25` | Resolved deployment + opaque credential binding ref | Pre-provider preflight (read-only) | Service delegation at worker claim | N/A (binding frozen in Session config) | None | Binding resolves for trusted org | Missing/revoked/wrong-org/provider mismatch fails closed |
| `REQ-RSC-47`–`53`, `AC-RSC-26`–`27` | Frozen runtime policy snapshot on Session | Activation / Session bind commit | Trusted configuration resolver | Config digest + cohort version | None | Policy reconstruction exact | Lower-scope widen / drift rejected |
| `REQ-SESS-61`, `62`, `67`, `68`, `SESS-DEC-14`–`15`, `20` | `TrustedTriggerV1` → `AgentInvocation` admission | Single admission TX | Participant/service at boundary | `(session_id, trigger_family, trigger_id, purpose, policy_digest)` | None | Participant/opening/closing/timer admitted | Unknown/forged/prohibited/stale/cutoff denied |
| `REQ-SESS-63`, `69`, `SESS-DEC-16`, `21` | Invocation + execution attempts | Claim → provider (ext) → outcome TX | Worker service identity | Invocation id + attempt ordinal | None | One Decision on success | Timeout/malformed → outcome, no Decision |
| `REQ-SESS-64`–`67`, `70`, `SESS-DEC-17`–`19`, `23` | `AgentDecision` + validation/effect | Decision commit; separate effect TX | Reauth at effect | Decision id + effect idempotency | `session.agent.work.v1` resolved state | `emit_message` / `no_action` paths | Schema-invalid → execution outcome; policy reject → no effect |
| `REQ-SESS-55`–`60`, `AC-SESS-32`, ADR-011 | Fragments + completion | Per-fragment TX; completion TX | Service at publish | `(agent_message_id, fragment_sequence)` | `session.agent.fragment.v1`, `session.agent.complete.v1` | Ordered replay | Gap/duplicate/cutoff |
| `REQ-SESS-71`–`77`, `AC-SESS-38`–`41`, `SESS-DEC-24`–`28` | `TimerScheduleRevision` | Replacement / fire TX | Scheduler service | `expected_schedule_revision` | None (UI-SESS-DEC-14) | Default arm, replace, fire once | Two pending / double fire impossible |
| `UI-SESS-DEC-13` | Turn + work projection | Effect commit couples turn terminalization | N/A | Turn/slot id | Work SSE + optional neutral status | No-action resolves without Agent message | No raw `no_action` in DOM |
| `UI-SESS-DEC-14` | Timer lane internal | Schedule TX | Scheduler | Revision + session order | Agent work states only when visible work | Timer-triggered message path | No synthetic Participant message |
| ADR-005 handoff | Session + manifest | Terminal seal TX | Service | Terminal intent idempotency | Terminal SSE | E2E readiness → Active → terminal | Seal fault → honest Completing |
| Security isolation | All scoped tables | Every TX | ADR-002 kernel | Composite FK + query scope | Safe errors only | Own-scope CRUD | Cross-scope deny + non-disclosing |

## Module ownership (target)

| Component | Project | Forbidden imports |
| --- | --- | --- |
| Domain rules | `FlexAgent.Sessions` (new) | HTTP, Npgsql, provider SDKs |
| Application | `FlexAgent.Sessions` | Direct table access outside repos |
| PostgreSQL repos | `FlexAgent.Sessions.Infrastructure` | Domain logic in SQL |
| Provider port | `FlexAgent.Sessions` abstractions | Provider types in contracts |
| Worker handler | `FlexAgent.Worker` + Sessions app | Unscoped repositories |
| Synthetic adapter | `FlexAgent.SyntheticBrowser` | Domain authority / DB writes |
| Browser contracts | `FlexAgent.Contracts` | Authorization secrets |

## Contract tranche (this execution step)

| Schema | Category | Browser-safe |
| --- | --- | --- |
| `trusted-trigger.v1` | Admission input | No |
| `agent-invocation.v1` | Runtime record | No |
| `agent-invocation-execution-attempt.v1` | Runtime record | No |
| `agent-decision.v1` | Structured control | No |
| `decision-validation-effect.v1` | Runtime record | No |
| `timer-schedule-revision.v1` | Scheduler record | No |
| `sse-event.v1` (extended) | Transport | Yes — adds `complete`, `work` |

## Residual risks (tracked)

- Production OIDC and live provider adapters remain out of scope; deterministic
  fake adapter proves boundary only.
- Numeric timer policy values come from frozen configuration fixtures in tests,
  not from code constants.
- Full PostgreSQL runtime schema not yet migrated; contract tranche precedes
  persistence implementation.
