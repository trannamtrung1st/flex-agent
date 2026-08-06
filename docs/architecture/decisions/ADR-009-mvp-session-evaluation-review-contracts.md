# ADR-009: MVP Session, Evaluation, and Review/Release contracts

## Status

Approved

## Decision metadata

| Field | Value |
| --- | --- |
| **Owner** | Architecture Lead |
| **Approvers** | Product Lead, Architecture Lead, Security/Privacy reviewer |
| **Consulted perspectives** | Business analysis, architecture, UI/UX, security/privacy, documentation |
| **Approved date** | 2026-08-06 |
| **Approval reference** | Contract review approved `SESS-DEC-1`–`SESS-DEC-8`, `EVAL-DEC-1`–`EVAL-DEC-8`, and `REV-DEC-1`–`REV-DEC-9`; provider-streaming, broker, and notification scope confirmed in follow-up review |
| **Governs** | Detailed MVP Session runtime, Evidence/Evaluation execution, and Human review/Result/Release realization |

## Context

[ADR-006](ADR-006-mvp-architecture-baseline-and-evolution.md) approved the MVP
system shape but deliberately required detailed contracts before text Session,
Evaluation, and Review/Release implementation. The approved feature
specifications already govern observable behavior; architecture still needed to
fix authoritative ordering, timing, publication, Evidence location and sealing,
evaluator execution, completion atomicity, review candidate selection, Result
construction, Release visibility, correction, and recovery semantics.

The contract review also clarified two infrastructure questions:

- A model provider may stream candidate tokens directly to a worker, but the MVP
  does not publish final Agent answers token by token to the Participant.
- Redis Streams, Kafka, or another broker is not needed for MVP correctness.
  The primary relational store and transactional outbox remain authoritative;
  a broker may later accelerate delivery only under the approved non-authority
  and measured-need constraints.

Without one approving ADR, the detailed contracts could be marked approved while
remaining ambiguous under the repository's authority-by-concern rule, which
assigns technical realization to approved architecture decisions.

## Decision drivers

- Preserve the approved Organization, Activity, Participant, Attempt, and
  Session isolation and outcome-chain invariants.
- Give implementation one deterministic ordering, timing, idempotency,
  completion, lineage, and recovery contract.
- Keep provider output, browser state, event delivery, caches, and optional
  brokers outside workflow and authorization authority.
- Avoid per-token durable writes and broker operations before an approved
  Participant-visible streaming feature or measured throughput need exists.
- Preserve exact Evidence and Result reconstructability without duplicating
  unnecessary protected content.
- Maintain separate authorization and atomicity for Review decision, Result, and
  Release.
- Keep contract versions explicit so later changes can supersede rather than
  silently reinterpret historical execution.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Leave the detailed contracts `Proposed` | No additional commitment | Blocks feature implementation and leaves known runtime races unresolved |
| Mark contracts approved without an ADR | Fewer documents | Conflicts with authority-by-concern and obscures the approving decision |
| Approve each contract through a separate ADR | Fine-grained decision records | Repeats shared context and makes the coordinated vertical-slice boundary harder to review |
| Approve the three versioned contracts through one umbrella ADR | One explicit authority boundary with contract-specific stable IDs and future supersession paths | Requires coordinated review when a change spans more than one contract |
| Require Kafka/Redis Streams and Participant-visible token streaming now | Early high-throughput streaming infrastructure | Adds broker, chunk durability, fan-out, sensitive retention, and recovery boundaries without an MVP requirement or evidence |

## Decision

### Adopt the detailed contracts

Approve version 0.1 of:

- [Text Session runtime contract](../session-runtime-contract.md), including
  `SESS-DEC-1` through `SESS-DEC-8`.
- [Evidence and Evaluation execution contract](../evaluation-execution-contract.md),
  including `EVAL-DEC-1` through `EVAL-DEC-8`.
- [Human review, Result, and Release contract](../review-result-release-contract.md),
  including `REV-DEC-1` through `REV-DEC-9`.

The feature specifications continue to govern observable behavior. These
contracts and this ADR govern their detailed MVP technical realization.

### Provider token-streaming boundary

The worker may receive a bounded token stream from the model-provider adapter as
internal, untrusted candidate data. The worker buffers and validates the
candidate, then commits one complete durable Agent message through the approved
response-slot and Session-sequence transaction. Partial provider tokens are not
Participant-visible transcript content and are not individually durable by
default.

If a worker fails before publication, its lease may expire and a new
`generation_attempt_id` may retry the same stable `response_slot_id`. At most one
complete final message may win publication.

### SSE and optional broker boundary

Request/response commands and SSE remain the MVP client contract. The primary
relational store owns Session events, `session_sequence`, replay, reconnect,
idempotency, and transcript authority. The committed transactional outbox or
bounded polling wakes delivery.

Redis Streams, Kafka, or another broker is not required. A later measured need
may introduce one as an at-least-once, non-authoritative delivery accelerator
only when:

- publication originates from the committed outbox rather than an uncoordinated
  database-plus-broker dual write;
- consumers deduplicate by authoritative `session_sequence`;
- reconnect and historical replay query the primary store;
- broker failure has a bounded database-backed fallback;
- event payloads use scoped protected references and do not establish audience
  authorization; and
- retention or trimming cannot remove authoritative history.

Making a broker mandatory or authoritative requires an architecture update or
superseding ADR. Participant-visible token streaming is a separate future
feature and must define durable-before-display chunks, ordering, exact exposed
content, terminal cutoff, authorization, retention, fan-out, and recovery. Its
stable identity hierarchy begins with `session_id`, `turn_id`,
`response_slot_id`, `generation_attempt_id`, and `chunk_sequence`.

### Notification boundary

For the MVP, Release notifications announce availability and route the actor to
the normal authenticated application. They contain no Result content or bearer
access. A later approved channel policy may permit a separately reviewed safe
representation without changing historical Releases or weakening Release
authorization.

## Consequences

- Text Session, Evaluation, and Review/Release backend implementation may begin
  against the approved contracts using specification-driven TDD.
- Final Agent answers favor durable complete-message publication over immediate
  Participant-visible token latency in the MVP.
- A worker restart may repeat model generation, but response-slot uniqueness and
  immutable attempt provenance prevent duplicate final publication.
- PostgreSQL-backed work and outbox behavior remains sufficient for correctness;
  Redis or Kafka does not become a hidden deployment prerequisite.
- Evidence locators, Evidence-set seals, Human revisions, Results, Releases, and
  correction lineage now have approved versioned technical boundaries.
- Machine-readable schemas, conformance fixtures, implementation, UI/UX
  specifications, negative tests, failure injection, load evidence, and
  production operations remain required; approval does not mark them complete.
- A meaning-changing contract revision requires a new version and an updating or
  superseding ADR. Historical records retain the procedure and schema versions
  under which they were created.

## Related

- [MVP architecture](../mvp-architecture.md)
- [ADR-001: resolved configuration and manifest integrity](ADR-001-resolved-configuration-representation-and-integrity.md)
- [ADR-002: authorization enforcement and delegation](ADR-002-authorization-enforcement-and-delegation.md)
- [ADR-003: authorization audit persistence](ADR-003-authorization-audit-persistence.md)
- [ADR-005: Attempt start and Submission binding](ADR-005-atomic-attempt-start-and-submission-binding.md)
- [ADR-006: MVP architecture baseline](ADR-006-mvp-architecture-baseline-and-evolution.md)
- [ADR-008: bounded OSS component set and provider/deployment defaults](ADR-008-bounded-oss-component-set.md)
- [Text Session lifecycle](../../requirements/features/session-text-lifecycle.md)
- [Evidence and Evaluation](../../requirements/features/evidence-evaluation.md)
- [Human review and Result Release](../../requirements/features/review-result-release.md)
