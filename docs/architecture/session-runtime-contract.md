# Text Session runtime contract

Approved detailed runtime contract for the MVP text Session lifecycle,
including participant-visible incremental Agent-response streaming.

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Approved |
| **Owner** | Architecture Lead |
| **Approvers** | Product Lead, Architecture Lead, Security/Privacy reviewer |
| **Consulted perspectives** | Business analysis, architecture, UI/UX, security/privacy, documentation |
| **Version** | 0.2 |
| **Approved date** | 2026-08-06; version 0.2 approved 2026-08-09 |
| **Approval reference** | [ADR-009](decisions/ADR-009-mvp-session-evaluation-review-contracts.md) approved `SESS-DEC-1`–`SESS-DEC-8`; [ADR-011](decisions/ADR-011-participant-visible-agent-response-streaming.md) supersedes `SESS-DEC-3` and approves `SESS-DEC-9`–`SESS-DEC-13` plus version 0.2 |
| **Governs** | Session command, ordering, timing, publication, reconnect, terminalization, and recovery realization |

This document does not alter observable behavior in the approved
[text Session lifecycle specification](../requirements/features/session-text-lifecycle.md).
It is authoritative for the detailed MVP technical realization within the
approved product, requirements, and ADR boundaries.

## Purpose and audience

This contract gives backend, frontend, security, and testing contributors one
authoritative runtime boundary after the atomic Attempt/Session start in
[ADR-005](decisions/ADR-005-atomic-attempt-start-and-submission-binding.md) and
before the Evaluation handoff. It defines:

- one Session state and ordering authority;
- message, turn, work-trace, timing, and terminal record ownership;
- synchronous command and asynchronous generation boundaries;
- reconnect and SSE cursor behavior;
- exact transaction, idempotency, audit, and manifest coupling; and
- failure, isolation, and verification rules.

It intentionally does not select a programming language, framework, database
product, model provider, or SSE library.

## Governing sources

- [Concept model](../product/concept-model.md), especially Session isolation,
  configuration precedence, assessment fairness, and the resolved execution
  manifest.
- [MVP scope](../product/mvp-scope.md), especially one Participant per text
  Session and the deferred voice, tools, Dynamic memory, and shared-Session
  capabilities.
- Approved [text Session lifecycle requirements](../requirements/features/session-text-lifecycle.md#business-rules)
  and [acceptance criteria](../requirements/features/session-text-lifecycle.md#acceptance-criteria).
- [Authorization and isolation](../requirements/features/auth-resource-isolation.md),
  [resolved Session configuration](../requirements/features/resolved-session-configuration.md),
  [Submission and Attempts](../requirements/features/submission-attempts.md), and
  [MVP operational defaults](../requirements/mvp-operational-defaults.md).
- [ADR-001](decisions/ADR-001-resolved-configuration-representation-and-integrity.md),
  [ADR-002](decisions/ADR-002-authorization-enforcement-and-delegation.md),
  [ADR-003](decisions/ADR-003-authorization-audit-persistence.md),
  [ADR-005](decisions/ADR-005-atomic-attempt-start-and-submission-binding.md), and
  [ADR-006](decisions/ADR-006-mvp-architecture-baseline-and-evolution.md),
  [ADR-008](decisions/ADR-008-bounded-oss-component-set.md), and
  [ADR-009](decisions/ADR-009-mvp-session-evaluation-review-contracts.md).
- Approved [MVP architecture](mvp-architecture.md), especially `AR-DEC-3`,
  `AR-DEC-4`, `AR-DEC-7`, and its ordering and durable-work rules.

## Scope

### In scope

- Runtime behavior from committed Session readiness through immutable terminal
  state and eligible Evaluation handoff.
- Participant message admission, turns, response slots, generation attempts,
  durable participant-visible Agent-response fragments, work-trace updates,
  complete/incomplete Agent-message outcomes, and replay.
- Active-duration and absolute-deadline accounting, warnings, pause/resume,
  expiry, completion, termination, and abort.
- Request/response commands, SSE delivery, bounded polling fallback, reconnect,
  stale-client reconciliation, revocation, and multiple-device concurrency.
- Append-only transcript/lifecycle history, manifest provenance, audit coupling,
  lifecycle policy, and operational verification.

### Out of scope

- Attempt entitlement, Submission acceptance, exact start binding, configuration
  resolution, and initial manifest creation before ADR-005 commits.
- Evaluation, Evidence selection, Human revision, Review decision, Result, and
  Release behavior after the terminal handoff.
- Detailed SPA information architecture, content, component design, and visual
  behavior. Those remain owned by an approved UI/UX interaction specification.
- Voice, participant-session tools, Dynamic memory, shared Sessions, offline
  submission, and participant-provided code execution.

## Confirmed constraints

1. The primary relational store owns authoritative Session state, ordering,
   idempotency, transcript metadata, timers, work admission, manifest records,
   and required audit/outbox acceptance.
2. The browser, SSE connection, client clock, model provider, work payload, and
   cache are never authoritative for Session state, identity, timing, or order.
3. Every protected operation derives the complete Organization, Activity,
   Participant, Attempt, and Session chain from trusted records and uses
   [ADR-002](decisions/ADR-002-authorization-enforcement-and-delegation.md).
4. External model calls occur outside database transactions and only through
   bounded durable work under a service identity and durable delegation.
5. The Session uses its immutable resolved configuration and exact bound
   Submission versions; mutable aliases and later versions are prohibited.
6. Accepted Participant messages and published Agent content are immutable.
   Corrections append new records.
7. Persisted time is UTC. Authoritative sequence and commit order, not client or
   provider timestamps, resolve races.
8. Required identity, authorization, ownership, integrity, manifest, or audit
   state fails closed.

## Approved contract decisions

`SESS-DEC-1`–`SESS-DEC-8` were approved on 2026-08-06.
`SESS-DEC-9`–`SESS-DEC-13` were approved on 2026-08-09 through ADR-011.

| ID | Approved decision | Rationale |
| --- | --- | --- |
| `SESS-DEC-1` | Use one monotonically increasing `session_sequence` allocated by the primary store for every authoritative lifecycle, transcript, work-trace, warning, timer, terminal-intent, and manifest-relevant Session record. | One order resolves device, timer, model, and terminal races without trusting wall clocks. |
| `SESS-DEC-2` | Represent a turn with one stable `turn_id` and one or more policy-declared response slots; the MVP default participant turn has exactly one Agent response slot. Enforce at most one published Agent message per slot. | Separates accepted input, generation attempts, and participant-visible publication while preventing duplicate answers. |
| `SESS-DEC-3` | **Superseded by `SESS-DEC-9` through ADR-011.** Original decision: publish final Agent answers only as complete durable messages while provider tokens remain Participant-invisible. | Retained as historical rationale; it no longer governs MVP publication. |
| `SESS-DEC-4` | Persist timer facts as an authoritative start, active-duration budget, optional absolute endpoint, closed pause intervals, current open pause, and emitted warning occurrences. Compute remaining time from database-authoritative UTC at each command. | Makes timing reconstructable and prevents connection or process state from controlling fairness. |
| `SESS-DEC-5` | Entering `Completing` commits a terminal intent and transcript cutoff sequence through the same Session version/sequence boundary that publishes messages. The winner of the race determines inclusion; no later publication may cross the cutoff. | Gives completion, expiry, termination, and late provider callbacks one deterministic boundary. |
| `SESS-DEC-6` | Complete terminalization in one primary-store transaction that records the immutable terminal record, timing summary, Attempt mapping, manifest terminal append/seal, Evaluation-handoff eligibility, and required audit/outbox acceptance. | Prevents false terminal success and partial Evaluation readiness. |
| `SESS-DEC-7` | Use request/response for commands and SSE for committed state events. Every event carries a Session-scoped cursor equal to `session_sequence`; reconnect reads authoritative deltas after a trusted cursor from the primary store. Redis Streams, Kafka, or another broker is not required for MVP correctness and may later act only as a non-authoritative delivery accelerator unless a superseding ADR establishes a stronger role. | Preserves recoverability without making the connection or optional broker authoritative. |
| `SESS-DEC-8` | Store idempotency by trusted command scope, key, schema version, request digest, state, and result reference. Equivalent retries reconcile; mismatched reuse returns conflict without mutation. | Covers lost responses and multi-device retries without duplicate side effects. |
| `SESS-DEC-9` | Stream Agent responses token by token to the Participant in the MVP: publish every provider-emitted text delta at the finest granularity the provider interface exposes, without application-added batching. Every exact delta selected for display must be validated and committed as an authoritative response fragment before SSE or polling may expose it. | Establishes streaming as a reusable product foundation without making transport buffers or the browser authoritative; it also avoids promising literal token boundaries when a provider exposes only multi-token deltas. |
| `SESS-DEC-10` | Use the stable hierarchy `session_id -> turn_id -> response_slot_id -> generation_attempt_id -> agent_message_id -> fragment_sequence`. Allocate one `session_sequence` for every fragment commit and require a positive contiguous `fragment_sequence` within the message. | Gives reconnect, deduplication, evidence, and terminal races one exact identity and order model. |
| `SESS-DEC-11` | The first fragment commit atomically claims the response slot, visible generation attempt, and Agent message. Only that attempt may append contiguous fragments. A final record seals the fragment range and assembled-content digest as `Complete`; stopped visible streams end explicitly as `Incomplete` or `Cancelled`. | Prevents two retries from interleaving or a later full candidate from replacing content already shown. |
| `SESS-DEC-12` | A failure before the first visible fragment may retry the same response slot with a new generation attempt. After any visible fragment, an in-place restart is prohibited; the system may finish the same valid stream or, when frozen policy permits, create a new ordered continuation response slot linked to the incomplete message. | Preserves exact exposure while retaining an explicit recovery path. |
| `SESS-DEC-13` | Enforce positive per-fragment, fragment-rate, fragment-count, total-response, in-flight, and Session stream bounds. Use rolling incremental validation, transactional outbox delivery, database-backed replay, and backpressure; no external broker is required for correctness. | Contains write amplification, resource exhaustion, incremental disclosure, and delivery failure while preserving the existing OSS-first topology. |

## Logical ownership and records

| Record | Authoritative owner | Required identity and mutation rule |
| --- | --- | --- |
| Session | Session execution | Stable Session and ownership references, state, expected version, latest sequence, resolved-configuration/manifest/start-binding references; expected-version transitions only |
| Session event | Session execution | Session, sequence, event type/schema, actor/service, UTC commit time, correlation, protected payload reference; append-only |
| Message | Session execution | Session, author type, turn/slot, immutable Participant content or rebuildable Agent assembled-content projection, publication state, first/last Session sequence, UTC times, provenance, completion outcome; Participant content is immutable after acceptance, while Agent content and outcome derive only from append-only fragments and the final outcome record and are never replaced by a later candidate |
| Agent response fragment | Session execution | Session, turn, response slot, visible generation attempt, Agent message, contiguous fragment sequence, authoritative Session sequence, exact text delta/protected reference, digest, UTC publication time, validation/configuration/model provenance; append-only |
| Turn | Session execution | Trigger message or Agent-initiated type, response-slot policy, state, exact configuration, attempt references, published response; state-transitioned with history |
| Response slot | Session execution | Turn, ordinal/type, state, winning visible-generation and Agent-message references; first-fragment uniqueness prevents competing publication |
| Generation attempt | Session execution | Turn/slot, work/delegation/model/configuration references, attempt order, timing, bounded outcome, protected input/output references; append-only |
| Work-trace update | Session execution | Turn, sequence, allowed type, exact displayed content reference, policy/generation provenance, publication time; append-only |
| Timer state | Session execution | Start, active budget, optional absolute endpoint, warning-schedule version, open-pause reference, revision; authoritative summary |
| Pause interval | Session execution | Start/end sequences and UTC instants, actor/service, bounded reason, timer effect; append-only after close |
| Warning occurrence | Session execution | Frozen threshold identity, sequence, due and committed UTC time, delivery status; unique per Session and threshold |
| Terminal intent | Session execution | Requested terminal outcome/reason, actor/service, cutoff sequence, expected version, UTC time, idempotency; immutable |
| Terminal record | Session execution | Final state/reason, cutoff, last transcript item, timing summary, Attempt mapping, manifest seal/handoff, actor/service and UTC order; immutable |
| Reconciliation record | Session execution | Actor application session, trusted last-seen sequence, returned range/current state, outcome, UTC time; bounded and lifecycle-governed |

Large or sensitive content remains in its protected owning store. Session and
operational records use protected references and integrity metadata; audit,
work, SSE, logs, metrics, and errors do not duplicate transcript content.

## Session and turn state contracts

### Session lifecycle

```text
Ready --ADR-005 commit--> Active <----authorized resume---- Paused
                            |                                ^
                            +----authorized/safety pause-----+
                            |
                            +----terminal intent----> Completing
                                                       |
                                    +------------------+------------------+
                                    v                  v                  v
                                Completed          Terminated           Aborted
```

- Only `Active` admits Participant messages or new generation work.
- `Paused` admits no Participant message or new generation. The frozen policy
  determines whether an already claimed attempt may finish recording provenance;
  publication still competes at the authoritative sequence boundary.
- `Completing` admits no new message, work-trace update, response fragment, or
  Agent-message completion publication. In-flight work is cancelled or recorded
  as late/cancelled according to policy.
- `Completed`, `Terminated`, and `Aborted` are immutable terminal states.

Connection states (`connecting`, `connected`, `reconnecting`, `offline`) are
client projections and never Session lifecycle values.

### Turn lifecycle

```text
Accepted input -> Work queued -> Generating -> Streaming -> Complete
                       |             |           |          |
                       +-------------+-----------+----------+-> Retryable failure before visibility
                                               |            -> Incomplete after visibility
                                               |            -> Cancelled or late
```

An accepted input remains in the transcript even if no Agent response publishes.
Before visibility, each retry appends a generation attempt to the same response
slot. The first-fragment transaction claims one visible attempt and Agent
message. Uniqueness on the response-slot publication claim and on
`(agent_message_id, fragment_sequence)` prevents competing or duplicate visible
text. After visibility, failure produces an immutable incomplete message or a
separately linked continuation; it never restarts the same message.

## Versioned command contract

Every command envelope contains:

| Field | Meaning |
| --- | --- |
| `schema_version` | Supported command schema; unknown major versions fail closed |
| `command_id` | Correlation identity, not idempotency authority by itself |
| `idempotency_key` | Opaque caller key scoped and digested by the server |
| `session_locator` | Untrusted locator used to load authoritative ownership |
| `expected_session_version` | Required for state-changing commands where a stale action is unsafe |
| `client_last_seen_sequence` | Reconciliation hint only |
| `payload` | Command-specific content; no actor, owner, author, timer, or policy field becomes trusted scope |

The server derives actor/application session, Organization, resource chain,
current Session state, timer, policy versions, and permitted actions. Every
response returns a stable outcome category, authoritative Session version and
sequence when disclosure is permitted, correlation reference, currently
permitted recovery action, and no protected existence detail on denial.

Initial command types are:

- `session.message.send.v1`
- `session.pause.v1`
- `session.resume.v1`
- `session.complete.v1`
- `session.terminate.v1`
- `session.reconcile.v1`

System expiry and unrecoverable-abort commands use the same internal command
contract with service identity, durable delegation or system purpose, expected
version, idempotency, and required audit classification.

## Provider streaming and broker boundary

The Agent is a reusable identity and frozen configuration, not an independent
MVP service that writes platform state. A delegated worker executes one response
slot and calls the model-provider adapter. The approved data path is:

```text
committed Participant message and durable work
  -> worker claim and authorization
  -> bounded provider request
  -> provider-to-worker incremental response stream
  -> rolling validation of next exact delta
  -> authoritative fragment transaction
  -> committed outbox wake-up or bounded polling
  -> authorized SSE fragment delivery
  -> repeat within bounds
  -> authoritative Agent-message completion or incomplete outcome
```

- Provider-to-worker streaming is the source of incremental untrusted candidate
  events. The versioned provider adapter normalizes them into ordered,
  non-overlapping text deltas at the finest granularity the provider exposes.
  For a cumulative-snapshot provider, it emits only the verified new suffix and
  fails the attempt on prefix divergence; metadata-only events do not become
  transcript fragments. The worker maintains bounded rolling validation state
  and commits each exact normalized delta selected for Participant display
  before delivery. A delta may be buffered before validation, but it is not
  authoritative or visible until its fragment transaction commits.
- The model provider, Agent configuration, and provider callback never write the
  primary store directly. The worker is the trusted validation and commit
  boundary.
- The worker resolves only the credential binding frozen by the trusted Session
  configuration through the approved `SecretSource`. Client input and work
  payloads never carry raw provider credentials or select another credential
  owner, and resolution failure does not fall back to another payer or provider.
- A worker failure before the first committed fragment discards the unpublished
  candidate and permits a new `generation_attempt_id` for the same response
  slot. After the first fragment claims publication, lease transfer may resume
  only when the same visible attempt's provider continuation and next fragment
  order can be proven safe; otherwise the message ends `Incomplete` and an
  optional continuation uses a new linked response slot.
- The primary store owns durable work, `session_sequence`, replay, reconnect,
  idempotency, and transcript authority. The committed outbox is the only source
  from which an optional delivery adapter may publish notifications.
- Redis Streams, Kafka, or another broker is not required in the MVP. If measured
  multi-instance fan-out or throughput later justifies one, it may carry bounded
  event references as an at-least-once, non-authoritative accelerator. Consumers
  deduplicate by `session_sequence`, reconnect from the primary store, and fall
  back to bounded polling when the accelerator is unavailable.
- An optional broker must not contain unnecessary raw transcript or Evaluation
  content, establish audience authorization, replace the Session cursor, or make
  stream retention part of historical recovery.
- Making a broker authoritative or required for correctness requires an
  architecture update or superseding ADR covering transaction/outbox coupling,
  partitioning, replay, retention, isolation, recovery, and failure behavior.

Participant-visible incremental streaming is required in the MVP under
ADR-011. A broker alone does not provide publication durability, audience
authorization, exact fragment reconstruction, or terminal-cutoff safety; those
remain primary-store and application responsibilities.

## Critical consistency flows

### Participant message admission

Within one primary-store transaction, the API:

1. authenticates the application session and derives the complete trusted
   resource chain;
2. authorizes `session.message.send`, loads current policy/relationship versions,
   and validates `Active`, time, rate, size, pending-turn, and workflow limits;
3. checks the scoped idempotency record and trusted request digest;
4. increments `session_sequence` and commits one accepted Participant message;
5. creates the stable turn, response slot, and durable generation work record;
6. appends required manifest/audit/outbox records under their approved durability
   classes; and
7. stores the idempotent result reference and updated Session version.

No model request starts before this commit. A pre-commit failure changes nothing;
a lost post-commit response reconciles to the existing message and turn.

### Work-trace, Agent-fragment, and message-completion publication

The worker authenticates as a service, loads the durable delegation, rederives
scope, and performs external model work outside a transaction. For each
participant-visible delta, one primary-store transaction:

1. reauthorizes the service and revalidates Session, turn, slot, configuration,
   cutoff, and lease/idempotency state;
2. validates the next exact delta against output schema, rolling content state,
   size/rate/count/total bounds, rendering policy, and prohibited-content
   controls;
3. on the first fragment, atomically claims the response slot, visible
   generation attempt, and stable Agent message;
4. verifies the next contiguous fragment sequence and trusted digest;
5. allocates `session_sequence` and appends exactly one permitted work-trace or
   Agent-response fragment;
6. appends manifest and required audit/outbox state; and
7. updates the turn/stream state and idempotent fragment outcome.

After the provider finishes, a final transaction validates the full assembled
content state, fragment range, response bounds, current Session/cutoff, and
visible attempt; records the assembled-content digest and `Complete` outcome;
appends manifest/audit/outbox state; and closes the turn. It does not replace the
durable fragments with a later provider candidate.

If terminal intent, pause, revocation, validation failure, or a bound wins before
the next fragment, no later delta commits or displays. Zero-fragment attempts
may retry within policy. A visible prefix remains immutable and receives an
`Incomplete` or `Cancelled` outcome with bounded provenance; it is never
silently removed or completed by another attempt.

### Timing, warning, pause, and expiry

For an effective instant `now`, active elapsed time is the sum of intervals in
which the Session was `Active`, excluding authoritative pause intervals. The
remaining active budget is the frozen duration minus that sum. When an absolute
endpoint exists, the effective remaining time is the smaller permitted bound.
Only an authorized accommodation or fairness exception may change a frozen
upper boundary.

- Commands compare `now` using the primary-store or equivalently authoritative
  server time inside the commit boundary.
- Pause commits one open interval and blocks admission/publication. Resume
  closes exactly that interval after current authorization and time validation.
- Each warning is a durable work item with uniqueness on
  `(session_id, warning_threshold_id)`. Late delivery is recorded but never
  changes expiry.
- Expiry is an idempotent service command. A Participant message and expiry
  serialize through the same Session expected-version and sequence boundary.

### Terminalization and Evaluation handoff

1. Participant completion, workflow completion, expiry, authorized termination,
   or unrecoverable abort requests one terminal intent with expected version and
   idempotency.
2. The winning transaction changes the Session to `Completing`, allocates the
   cutoff sequence, blocks new admission/publication, and accepts the required
   audit record.
3. In-flight attempts are cancelled or recorded according to the frozen policy.
   Provider output cannot extend the cutoff.
4. A terminalization coordinator idempotently verifies the complete ordered
   transcript, timer facts, Attempt mapping, required manifest records, and
   terminal seal.
5. One transaction commits the immutable terminal record, terminal Session and
   Attempt states, manifest seal, eligible Evaluation handoff/work item only for
   `Completed`, and required audit/outbox acceptance.
6. If sealing or required audit fails, the Session remains honestly
   `Completing`; it does not expose Evaluation readiness or terminal success.

## SSE, reconnect, and authorization freshness

- SSE emits only committed state events. Each event contains event schema,
  Session reference, `session_sequence`, bounded type/status fields, and
  protected content only when the current actor is authorized for that view.
- An Agent-fragment event also contains the stable Agent-message identity,
  visible generation-attempt identity, contiguous fragment sequence, exact
  authorized text delta or protected reference, and integrity digest. A client
  deduplicates only after verifying the authoritative identity/order returned by
  the scoped projection.
- Establishment and subscription authorize the exact Participant and Session.
  The stream revalidates on relevant invalidation and at least within the
  approved 60-second revocation bound.
- `Last-Event-ID` or an explicit last-seen sequence is an untrusted cursor. The
  API verifies it belongs to the loaded Session, returns current state plus the
  authorized delta, and paginates older history when needed.
- Sequence gaps, an unknown cursor, projection lag, or uncertain message state
  trigger authoritative reconciliation rather than optimistic replay.
- Bounded polling returns the same query projection and cursor semantics; it is
  not a separate workflow authority.

## Security and privacy contract

| Threat or harm | Required control | Verification |
| --- | --- | --- |
| Cross-Organization/Participant/Session access | Trusted parent-chain loading and ADR-002 enforcement before materialization, subscription, work, and commit | Wrong-scope read/write/list/event/work matrix |
| Replay or multi-device race | Scoped idempotency, request digest, expected version, sequence and uniqueness constraints | Equivalent, conflicting, concurrent, and lost-response tests |
| Prompt injection or confused deputy | Frozen system/policy channels, no MVP tools or memory writes, constrained output validators | Attempts to alter scope, timing, rubric, tools, memory, or terminal state |
| Hidden-reasoning or protected-data disclosure | Separate constrained work-trace schema; deny hidden prompts, rubric internals, expected answers, secrets, reviewer data, and raw chain-of-thought | Prohibited-content and log/telemetry leakage tests |
| Incremental disclosure before full-response validation | Validate every delta with bounded rolling state before commit; stop on a prohibited delta; preserve already visible safe fragments as incomplete rather than rewriting them | Split-across-fragment prohibited-content, late validation failure, incomplete-prefix, and no-error-echo tests |
| Unsafe rendering or external retrieval | Treat all Participant/model text and links as inert untrusted content; no automatic fetch | XSS, unsafe URL, markup spoofing, preview, and exfiltration tests |
| Competing, duplicate, gapped, or late fragment publication | First-fragment slot claim, fragment identity/digest uniqueness, contiguous order, and terminal cutoff at every commit | Concurrent attempt, duplicate, gap, digest mismatch, timeout, pause, expiry, termination, and late-callback races |
| Timer manipulation | Authoritative server time, frozen policy, persisted pause intervals | Client-clock, disconnect, restart, boundary, and warning tests |
| Audit or manifest divergence | Mutation-coupled required audit and terminal seal | Failure injection at every coupled write |

Session working context is non-authoritative and follows the approved lifecycle.
It is never reused for memory, calibration, analytics training, another Activity,
or another Participant in the MVP.

## Failure and recovery contract

| Failure | Required outcome |
| --- | --- |
| Authorization or ownership unavailable | Deny or stop the protected operation; do not use cached scope as authority |
| Primary-store failure before commit | Expose no acceptance or state transition |
| Lost response after commit | Reconcile by trusted scope and idempotency; do not repeat mutation |
| Model timeout, cancellation, or invalid output before visibility | Preserve accepted input and attempt outcome; retry the response slot within frozen bounds or expose safe failure |
| Model timeout, cancellation, or invalid output after visibility | Preserve the exact durable prefix, append `Incomplete`/`Cancelled`, and never restart or replace it in place |
| Worker crash or lost lease | Before visibility, permit safe redelivery; after visibility, resume only the proven same attempt/order or end incomplete; never permit competing publication |
| Duplicate, cumulative, or divergent provider event; duplicate outbox/SSE delivery | Normalize to one non-overlapping delta, require cumulative-prefix continuity where applicable, and reconcile by message/attempt/fragment identity and digest; never append or display text twice |
| Fragment gap or digest conflict | Stop publication/display and reconcile from primary storage; fail the stream incomplete if contiguous reconstruction cannot be proven |
| SSE disconnect or gap | Keep Session, fragments, and timer authoritative; reconnect from current state/cursor and replay the exact authorized delta |
| Audit/manifest append failure | Block the coupled operation; retain honest recoverable state |
| Terminal seal failure | Remain `Completing`; block Evaluation handoff and post-cutoff publication |
| Projection/cache failure | Query authoritative state or expose a bounded degraded state; never broaden access |
| Lifecycle disposition of content | Preserve minimized lineage and report unavailable content honestly; never substitute later content |

## Quality and observability

- Authoritative message admission and bounded reconnect synchronization retain
  the approved 2-second p95 objective under the specification's exclusions.
- Backpressure and fair durable-work claiming are partitioned by Organization
  and Activity, with positive message, transcript, generation, retry,
  fragment-size/rate/count, total-response, in-flight-stream, and per-Session
  bounds.
- Operational signals include command admission/commit latency, sequence
  conflicts, idempotent replay/conflict, generation state, time to first durable
  fragment, fragment commit-to-display latency, fragment duplicate/gap/conflict,
  incomplete-stream rate, stream backpressure, completion latency, warning
  drift, pause duration, reconnect delta, revocation lag, terminalization age,
  audit/manifest failure, work backlog, and post-cutoff attempts.
- Logs, metrics, traces, alerts, work records, and error responses contain
  bounded categories and protected references, never raw transcript, prompt,
  output, draft, credentials, unrestricted identifiers, or Participant data.

## Verification and traceability

| Contract surface | Requirements and acceptance criteria | Minimum repeatable evidence |
| --- | --- | --- |
| Entry and command authority | `REQ-SESS-1`–`REQ-SESS-7`; `AC-SESS-1`–`AC-SESS-2` | Committed-readiness, stale acknowledgment, wrong-scope, and pre-commit failure tests |
| Messages, turns, fragments, work traces | `REQ-SESS-8`–`REQ-SESS-19`, `REQ-SESS-51`–`REQ-SESS-60`; `AC-SESS-3`–`AC-SESS-8`, `AC-SESS-31`, `AC-SESS-32` | Idempotency, concurrent publisher, provider delta/cumulative-snapshot normalization, first-fragment claim, contiguous order, digest conflict, duplicate/gap/divergence, retry/continuation, reconnect replay, cutoff, injection, unsafe rendering, and prohibited-disclosure tests |
| Timer, pause, warning, reconnect | `REQ-SESS-20`–`REQ-SESS-30`; `AC-SESS-9`–`AC-SESS-14` | Exact-boundary, disconnect, restart, revocation, warning uniqueness, and pause accounting tests |
| Terminal and handoff | `REQ-SESS-31`–`REQ-SESS-41`; `AC-SESS-15`–`AC-SESS-20` | Message/expiry/termination races, seal/audit fault injection, mapping, post-cutoff callback, and handoff tests |
| History, privacy, lifecycle | `REQ-SESS-42`–`REQ-SESS-50`; `AC-SESS-21`–`AC-SESS-23`, `AC-SESS-28`–`AC-SESS-30` | Immutability, current authorization, lawful unavailability, non-reuse, audit and leakage tests |
| Performance and UI state feed | `AC-SESS-24`–`AC-SESS-27`, `AC-SESS-31`, `AC-SESS-32` | Streaming load/backpressure and SLO evidence plus state-contract tests consumed by the approved UI/UX specification |

Implementation acceptance also requires ADR-001 conformance fixtures, database
constraint tests, process-kill and transaction fault injection, and an
end-to-end test from ADR-005 readiness through eligible Evaluation handoff.
Provider-path verification must also exercise the scoped credential and
fail-closed no-fallback behavior required by `REQ-RSC-46` and `AC-RSC-25`.
Playwright visual evidence remains owned by the downstream UI/UX implementation
and is not satisfied by this architecture document.

## Open questions

None. The approved feature specification resolves the relevant product and
policy questions. Framework and library choices remain implementation details;
component/provider profiles and their evidence gates are governed by ADR-008.
ADR-008 intentionally selects no normative model. Every enabled deployment or
Organization provider profile must satisfy its gates and does not change this
contract's semantics.

## Approval and downstream impact

Version 0.2 approval through ADR-011 unblocks detailed streaming text Session
implementation and makes the following downstream artifacts responsible for
conforming to this contract:

- backend persistence schemas, Session domain modules, work records, APIs, SSE,
  authorization adapters, model adapters, and tests;
- the Evaluation handoff consumer;
- the approved Session UI/UX interaction specification and frontend state
  model; and
- operational dashboards, reconciliation procedures, and lifecycle jobs.

## Related documents

- [MVP architecture](mvp-architecture.md)
- [Evidence and Evaluation execution contract](evaluation-execution-contract.md)
- [Human review, Result, and Release contract](review-result-release-contract.md)
- [Architecture decisions](decisions/README.md)
- [ADR-011: Participant-visible Agent-response streaming](decisions/ADR-011-participant-visible-agent-response-streaming.md)
