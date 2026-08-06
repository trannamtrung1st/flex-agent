# ADR-005: Atomic attempt start and submission binding

## Status

Approved

## Owners and approvers

- Owner: Architecture Lead
- Approvers: Product Lead, Architecture Lead, UI/UX reviewer, Security/Privacy reviewer
- Approved date: 2026-08-06

## Context

The approved [Submission and attempts specification](../../requirements/features/submission-attempts.md) requires a successful attempt start to bind the exact immutable Submission versions used by the Session. The same start boundary freezes the resolved session configuration, creates the initial execution manifest, binds the Session, transitions the Attempt to `Active`, consumes one entitlement, and accepts its required durable audit event.

These records must not diverge. A Session without an exact Submission binding creates a moving evidence target. A consumed Attempt without a usable Session unfairly penalizes the Participant. A Session exposed before its configuration, manifest, or audit event is authoritative permits partial execution. Duplicate or concurrent starts must not create competing Sessions or consume more than one entitlement.

ADR-001 governs resolved-configuration and manifest representation. ADR-002 governs authorization enforcement and delegation. ADR-003 requires mutation-coupled durable audit in the owning consistency boundary. This decision extends that existing atomic start boundary to the Attempt transition and exact Submission-version binding required by approved decision `PROP-2`.

## Decision drivers

- Exactly one authoritative Session and exact Submission-version set per started Attempt.
- No entitlement consumption before the complete start boundary commits.
- No participant interaction, model access, evidence collection, or reviewer use from partial state.
- Safe idempotent retries, concurrent requests, process failure, and uncertain client responses.
- Commit-time authorization and complete organization/activity/cohort/enrollment/participant/task ownership validation.
- Immutable historical binding even when later Submission versions are accepted.
- Compatibility with ADR-001, ADR-002, and ADR-003 without introducing a distributed transaction or premature service topology.
- Sensitive-content minimization: bind protected references and integrity metadata, not raw Submission payloads.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Commit all start records in one authoritative transactional boundary | Strong atomicity, straightforward retry semantics, no compensating entitlement repair, and direct compatibility with ADR-003 | Requires start-owned records to share the MVP consistency boundary and constrains premature physical separation |
| Create an immutable Submission binding first, then atomically reference it during start | Can isolate intake ownership while preserving a stable precondition | Requires lifecycle and cleanup rules for unused bindings and still needs atomic validation that the binding remains eligible |
| Use a distributed saga with compensating actions | Allows independently deployed stores | Exposes temporary partial states, makes entitlement restoration and audit truth complex, and is not justified by MVP scale |
| Bind a mutable `latest` Submission alias | Minimal start-time coordination | Breaks reconstructability, fairness, evidence integrity, and reviewer consistency |

## Decision

### Authoritative MVP consistency boundary

Use one authoritative transaction in the primary transactional platform store to commit all of the following or none:

- the idempotent start-command success outcome;
- one Attempt transition to `Active` and one entitlement consumption;
- one immutable exact Submission binding for the Attempt and Session;
- one immutable resolved session configuration and its digest metadata;
- one initial resolved execution manifest;
- one Session binding to the Attempt, configuration, manifest, and Submission binding;
- the Session readiness state that permits execution to begin; and
- the immutable authoritative audit event or audit-outbox event required by ADR-003.

Logical ownership remains distinct even when records share the transaction. Submission intake continues to own accepted Submission versions and protected payloads. Attempt control owns entitlement and Attempt state. Session resolution owns the resolved configuration and manifest. Session lifecycle owns execution after the committed readiness boundary. Physical co-location for the MVP does not merge these domain responsibilities.

The transaction stores stable protected Submission-version and item references, ownership references, accepted-state/version facts, material-category identifiers, integrity metadata references, and resolved capability-compatibility outcomes needed for reconstruction. It does not copy raw Submission text or attachment bytes into the Attempt, Session, configuration, manifest, or audit event.

### Commit-time validation

Before and again inside the authoritative commit boundary, the start operation must:

- authenticate the human or service actor and authorize the start action through ADR-002;
- derive the organization, activity, cohort, baseline, enrollment, participant, task, Attempt, and accepted Submission ownership chain from trusted state;
- validate current enrollment/cohort availability, effective timing, remaining entitlement, and absence of a competing non-terminal Session;
- load exact accepted Submission versions that satisfy the frozen Submission requirement without using `latest`, `current`, a filename, or a client-selected owner as authority;
- verify that every required item remains accepted, immutable, integrity-valid, and eligible under the frozen requirement and current narrowing organization policy;
- resolve compatible Submission-reading capabilities from trusted frozen configuration, never from participant content or metadata;
- block start when required agent inspection lacks a compatible permitted capability;
- revalidate every unfrozen authorization, timing, entitlement, and eligibility assumption; and
- accept the required durable audit/outbox record in the same transaction.

A readiness check, upload confirmation, cached authorization allow, client-provided version list, possession of an object key, or prior page access is advisory only and cannot replace commit-time validation.

### Exact immutable Submission binding

The binding records:

- a stable binding identifier;
- organization, activity, cohort, enrollment, participant, task, Attempt, and Session references;
- the frozen Submission-requirement revision;
- an explicitly ordered set of exact accepted Submission-version and material-item references;
- protected integrity and validation-policy references for those items;
- the resolved capability-compatibility outcome for each item;
- the binding reason/stage and UTC commit time; and
- the start command and audit correlation references.

The owning store enforces immutability and uniqueness for the authoritative Attempt/Session start binding. Later accepted Submission versions create new version history and do not update the binding. If a frozen requirement permits later in-session material, the owning Session workflow appends a separately identified ordered binding under the same exact-version, authorization, capability, provenance, and audit rules; it never edits the start binding.

### Idempotency, concurrency, and uncertain outcomes

Each start request uses a trusted idempotency key scoped to the organization, enrollment, intended Attempt, and frozen start inputs. The authoritative store enforces uniqueness sufficient to prevent more than one non-terminal Session per Attempt and more than one entitlement consumption for the committed start.

- An equivalent retry after commit returns the existing Attempt, Session, configuration, manifest, and Submission-binding result.
- Reuse of the key with a different trusted command digest reports an idempotency conflict and changes no protected state.
- Concurrent equivalent requests produce one winning commit and reconcile to the same result.
- A competing non-equivalent request fails without creating another Session or consuming entitlement.
- A timeout or lost response is reconciled by idempotency key and authoritative bindings before another start is offered.
- A failure before commit leaves the Session unstarted and entitlement unconsumed.
- A failure after commit is a response/projection recovery problem; it does not reverse consumption, create another start, or rewrite the binding.

Participant interaction and model execution may begin only from the committed Session readiness state. Asynchronous projectors, notifications, indexes, and read models may run after commit but are never the authority for whether start succeeded.

### Failure and recovery

The start fails closed when authorization, ownership, timing, entitlement, accepted-version eligibility, capability compatibility, configuration resolution, manifest creation, persistence, or required audit acceptance fails. The response uses bounded non-sensitive reason categories and exposes a safe correction or reconciliation path where one exists.

An Attempt that aborts after the transaction commits remains consumed and retains its Session, configuration, manifest, Submission binding, and terminal provenance under `PROP-1`. Another try requires remaining baseline allowance or a separately authorized retry entitlement. Compensation must not delete or reset the committed Attempt merely because execution later failed.

### Verification

Implementation acceptance requires automated coverage for:

- exact binding of direct text and multiple attachment items;
- later Submission versions leaving the start binding unchanged;
- missing or non-accepted versions and mutable-alias substitution;
- wrong organization, activity, cohort, enrollment, participant, task, Attempt, Session, or object reference;
- missing required Submission-reading capability and optional not-inspected material;
- duplicate and concurrent starts, mismatched idempotency reuse, and multiple-device races;
- failure injection before each transaction write and required-audit acceptance;
- lost-response and post-commit reconciliation;
- uniqueness enforcement for entitlement consumption and non-terminal Session binding;
- absence of participant interaction or model access before committed readiness; and
- audit, logs, metrics, and error responses containing references and bounded categories rather than raw Submission content or access secrets.

## Approved decision disposition

| Question | Approved disposition |
| --- | --- |
| `Q-ADR5-1` | Use one authoritative transaction in the primary transactional platform store for the MVP; do not use a saga or distributed transaction. |
| `Q-ADR5-2` | Commit Attempt activation, entitlement consumption, exact Submission binding, resolved configuration, initial manifest, Session readiness/binding, and required audit/outbox acceptance together or not at all. |
| `Q-ADR5-3` | Keep raw Submission payloads in their protected owning store and bind only exact immutable references plus required integrity, policy, capability, and provenance metadata. |
| `Q-ADR5-4` | Permit execution only after committed Session readiness; reconcile uncertain responses from the authoritative idempotency and binding records. |

## Consequences

- Attempt consumption and Session usability cannot diverge under an ordinary partial write.
- Every started Session has an immutable, inspectable Submission target that later versions cannot change.
- Required capability incompatibility blocks start before entitlement consumption.
- Durable audit remains coupled to the protected mutation without a second authoritative write path.
- The MVP favors one transactional platform boundary over premature independent service stores.
- Submission, Attempt, resolution, and Session modules retain logical ownership but must coordinate through the shared start transaction contract.
- Future physical extraction requires a superseding ADR that preserves equivalent atomic visibility, idempotency, authorization, and audit semantics.
- Implementation must provide database constraints, transactional fault-injection tests, idempotency reconciliation, and projection monitoring.

## Related

- Requirements: [`REQ-SUBM-15`–`REQ-SUBM-22`, `REQ-SUBM-31`, `REQ-SUBM-46`–`REQ-SUBM-48`](../../requirements/features/submission-attempts.md#business-rules)
- Acceptance criteria: [`AC-SUBM-5`–`AC-SUBM-10`, `AC-SUBM-16`, `AC-SUBM-31`, `AC-SUBM-32`](../../requirements/features/submission-attempts.md#acceptance-criteria)
- Approved `PROP-2` disposition: [Submission and attempts](../../requirements/features/submission-attempts.md#approved-decision-dispositions)
- Resolved-configuration integrity: [ADR-001](ADR-001-resolved-configuration-representation-and-integrity.md)
- Authorization enforcement: [ADR-002](ADR-002-authorization-enforcement-and-delegation.md)
- Audit ownership and persistence: [ADR-003](ADR-003-authorization-audit-persistence.md)
- Assessment activation atomicity: [ADR-004](ADR-004-assessment-activation-baseline-and-atomicity.md)
