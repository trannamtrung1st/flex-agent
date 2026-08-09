# ADR-011: Participant-visible Agent-response streaming in the MVP

## Status

Approved

## Decision metadata

| Field | Value |
| --- | --- |
| **Owner** | Architecture Lead |
| **Approvers** | Product Lead, Architecture Lead, UI/UX reviewer, Security/Privacy reviewer |
| **Consulted perspectives** | Business analysis, architecture, UI/UX, security/privacy, documentation |
| **Approved date** | 2026-08-09 |
| **Approval reference** | Product Lead approved `UI-SESS-DEC-1`–`UI-SESS-DEC-12` on 2026-08-09, with `UI-SESS-DEC-6` revised to require token-by-token streaming in the MVP as a foundation for future interaction behavior |
| **Governs** | Participant-visible incremental Agent-response publication, durability, identity, ordering, replay, partial failure, terminal cutoff, and backpressure |
| **Supersedes** | The complete-message-only Session publication boundary in [ADR-009](ADR-009-mvp-session-evaluation-review-contracts.md), `SESS-DEC-3`, and the original complete-message-only clause of `PROP-7` |

## Context

ADR-009 originally allowed provider-to-worker token streaming but required the
worker to buffer and validate one complete candidate before publishing it to the
Participant. That reduced durable-write volume and made partial visibility rare,
but it delayed useful response feedback and did not establish the incremental
interaction foundation required for later Flex Agent experiences.

The Product Lead has now made participant-visible token-by-token Agent-response
streaming an MVP requirement. A browser-only animation over an uncommitted
provider stream would conflict with approved Session guarantees:

- reconnect must reconstruct exactly what the Participant saw;
- audit and Evidence must distinguish visible from unpublished content;
- duplicate workers and retries must not interleave different answers;
- pause, expiry, completion, termination, and revocation must stop publication
  at one authoritative cutoff; and
- visible content cannot be silently replaced after partial failure.

The publication contract therefore needs a durable fragment model rather than a
transport-only stream.

## Decision drivers

- Deliver immediate incremental Agent responses in the MVP.
- Establish a reusable foundation for future text and richer real-time
  interaction without weakening Session authority.
- Preserve exact participant exposure, reconnect, historical reconstruction,
  Evidence location, and terminal-cutoff semantics.
- Prevent competing generation attempts, duplicate delivery, and stale clients
  from producing duplicated or interleaved text.
- Keep the primary relational store authoritative and avoid making an external
  broker mandatory.
- Bound write amplification, SSE fan-out, resource exhaustion, and incremental
  disclosure risk.
- Keep work traces and concise reasoning summaries distinct from streamed final
  answer content and raw chain-of-thought.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Retain complete-message-only publication | Simplest durability, final validation, and storage profile | Rejects the approved MVP experience and does not create the desired streaming foundation |
| Stream provider output directly to the browser, persist only the final message | Lowest latency and fewer writes | Cannot prove exact exposure, reconnect safely, enforce cutoff per fragment, or preserve partial failure honestly; browser/provider transport becomes de facto authority |
| Buffer small groups of tokens, commit each group before display | Reduces write/event volume while retaining durable-before-display semantics | Does not satisfy the explicit token-by-token interaction direction and introduces a separate batching policy |
| Commit every participant-visible provider delta before display | Exact exposure, deterministic order/replay, clear cutoff, and reusable streaming contract | Higher write/outbox/SSE volume, more complex validation and recovery, and already displayed fragments cannot be recalled |
| Add Kafka or Redis Streams as the authoritative token log | High fan-out potential | Adds an unnecessary authoritative store, dual-write/replay/retention boundaries, and operational complexity without evidence that PostgreSQL-backed publication is insufficient |

## Decision

### Require durable-before-display incremental streaming

The MVP streams Agent responses incrementally to the Participant. Every exact
provider delta selected for display must pass bounded incremental validation and
commit to the primary store before authorized SSE delivery or polling exposes
it. Provider delivery, worker memory, the outbox, SSE, and the browser are not
publication authority.

For this contract, **token-by-token** means the finest text-delta granularity
exposed by the selected provider interface, with no application-added batching.
A provider may emit a delta containing more than one model token when it does
not expose literal token boundaries; the platform records and displays that
delta as received and does not claim a finer boundary.

The versioned provider adapter must normalize provider events into ordered,
non-overlapping text deltas. If a provider emits cumulative snapshots, the
adapter publishes only a verified new suffix and fails safely if the prior
snapshot is not an exact prefix; metadata-only events do not become transcript
fragments.

The stable identity hierarchy is:

```text
session_id
  -> turn_id
    -> response_slot_id
      -> generation_attempt_id
        -> agent_message_id
          -> fragment_sequence
```

Each fragment also receives the Session's authoritative `session_sequence` and
contains or references the exact visible text delta, trusted scope,
configuration/model provenance, UTC publication time, integrity digest, and
idempotent commit identity.

### Claim one visible publisher

The first fragment transaction atomically claims the response slot, visible
generation attempt, and stable Agent message. Only that attempt may append
contiguous fragments to that message. Database uniqueness and expected-version
checks reject a competing attempt, duplicate fragment, fragment gap, digest
mismatch, changed scope, stale lease, pause, revocation, or terminal cutoff.

A final completion record covers the exact fragment range and assembled-content
digest. Transcript projections assemble the message only from those durable
fragments; they never replace them with a later provider full response.

### Preserve partial visibility honestly

Failure before the first fragment may retry the same response slot with a new
generation attempt. After any fragment becomes visible:

- the visible prefix remains immutable transcript content;
- the message ends `Incomplete` or `Cancelled` if the same stream cannot safely
  finish;
- another attempt cannot restart or replace the message in place; and
- when frozen workflow policy permits recovery, a new ordered continuation
  response slot may reference the incomplete message explicitly.

The UI must not concatenate a regenerated continuation in a way that makes it
appear to be the original uninterrupted answer.

### Preserve cutoff and replay authority

Every fragment commit reauthorizes trusted Session scope and competes through
the same Session version/sequence boundary as pause, completion, expiry,
termination, revocation, and abort. No fragment may display after the winning
cutoff unless it was already committed as transcript content at or before that
cutoff and current authorization permits its delivery or replay. No new
fragment may commit after the cutoff. A pre-cutoff prefix remains immutable
transcript content with its honest outcome.

Reconnect and multiple-device delivery query the primary store after a trusted
Session cursor, replay exact authorized fragments in Session order, and
deduplicate by stable fragment identity and digest. The client never supplies
authoritative assembled text or next-fragment position.

### Validate and bound incremental output

The worker applies schema, encoding, rendering, scope, and prohibited-content
checks to every delta with bounded rolling validation state. Content that fails
validation is never committed or echoed in errors, work traces, logs, or
fallback text. Because an earlier valid fragment may already be visible, a
later failure stops the stream and preserves the safe prefix as incomplete.

The frozen Session policy supplies positive bounds for:

- fragment size;
- fragment publication rate;
- fragment count;
- total assembled response size;
- concurrent streams per Session and Organization;
- buffered but uncommitted provider data;
- generation timeout and retry before visibility; and
- continuation eligibility after incomplete visibility.

Backpressure pauses provider consumption when supported or cancels the attempt
before the primary store, outbox, web runtime, SSE connection, or Participant
Session can grow without bound.

### Retain the existing topology

The primary relational store, transactional outbox, request/response commands,
SSE, and database-backed replay remain sufficient for correctness. Redis
Streams, Kafka, or another broker is not required. A later non-authoritative
delivery accelerator remains subject to ADR-009's outbox, deduplication,
authorization, retention, and database-fallback conditions.

## Security and privacy consequences

Incremental publication increases the chance that an early fragment becomes
visible before a risk detectable only from later context emerges. This decision
accepts that residual risk in exchange for the approved streaming experience
and requires:

- incremental plus completion-time validation;
- bounded rolling context capable of detecting prohibited content split across
  fragments;
- immediate stop without echo when a later delta fails;
- immutable preservation of already displayed safe fragments;
- exact current authorization before every fragment materialization and commit;
- no raw response fragments in logs, metrics, traces, errors, broker payloads,
  or browser/test artifacts; and
- negative tests for cross-Session delivery, prompt injection, hidden-prompt or
  rubric disclosure, split-fragment bypass, unsafe markup, external retrieval,
  revocation, and post-cutoff publication.

Documentation approval is not evidence that these controls work. Implementation
must pass executable negative, fault-injection, concurrency, load, and browser
verification before release.

## Consequences

- Version 0.2 of the Text Session lifecycle and runtime contract governs MVP
  publication.
- `SESS-DEC-3` remains historical and is superseded by `SESS-DEC-9` through
  `SESS-DEC-13`.
- The 2026-08-09 revision of `PROP-7`, `REQ-SESS-55`–`REQ-SESS-60`, and
  `AC-SESS-32` make durable participant-visible streaming observable behavior.
- The Text Session UI must expose growing Agent messages, incomplete outcomes,
  reconnect replay, and accessible rate-bounded announcements without confusing
  streamed answer text with Agent activity or hidden reasoning.
- Persistence, outbox, SSE, projection, manifest, Evidence-location, and terminal
  tests must include fragment identity and order.
- Write amplification and event volume increase materially. Implementation must
  load-test representative streaming Sessions and enforce positive backpressure
  bounds before release.
- A safe fragment already displayed cannot be recalled. Later validation failure
  ends the stream honestly instead of rewriting history.
- No external broker or new deployment component becomes mandatory.

## Verification

Minimum repeatable evidence includes:

- first-fragment response-slot claim under competing workers;
- provider delta and cumulative-snapshot normalization, prefix divergence,
  contiguous order, duplicate idempotency, digest conflict, and gap rejection;
- process death before and after first visibility;
- SSE loss, reconnect replay, multiple-device deduplication, and projection
  rebuild from authoritative fragments;
- pause, expiry, completion, termination, revocation, and abort races at every
  fragment boundary;
- provider timeout, invalid delta, total-size/rate exhaustion, backpressure, and
  incomplete/continuation behavior;
- prompt injection, unsafe markup, automatic-fetch attempts, hidden prompt,
  rubric/expected-answer, reviewer, secret, and cross-Participant disclosure;
- prohibited content split across consecutive fragments;
- exact Evidence and terminal transcript location against streamed content; and
- keyboard, focus, screen-reader announcement throttling, desktop, narrow, and
  400 percent zoom Playwright evidence.

## Related

- Requirements: [Text Session lifecycle](../../requirements/features/session-text-lifecycle.md)
- Runtime contract: [Text Session runtime contract](../session-runtime-contract.md)
- UI/UX: [Text Session interaction specification](../../ui-ux/text-session.md)
- Partially supersedes: [ADR-009](ADR-009-mvp-session-evaluation-review-contracts.md)
- Preserves: [ADR-001](ADR-001-resolved-configuration-representation-and-integrity.md), [ADR-002](ADR-002-authorization-enforcement-and-delegation.md), [ADR-003](ADR-003-authorization-audit-persistence.md), [ADR-005](ADR-005-atomic-attempt-start-and-submission-binding.md), and [ADR-006](ADR-006-mvp-architecture-baseline-and-evolution.md)
