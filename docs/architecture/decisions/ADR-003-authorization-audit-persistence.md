# ADR-003: Authorization audit ownership and persistence

## Status

Proposed

## Owners and approvers

- Owner: Architecture Lead
- Required approvers: Product Lead, Architecture Lead, Security/Privacy reviewer

## Context

The approved [authorization and isolation specification](../../requirements/features/auth-resource-isolation.md) requires auditable access-control changes, assignments, delegations, sensitive mutations and disclosures, outcome releases, exports, and security-relevant denials. Audit records must be append-only or equivalently tamper-evident, retain unambiguous UTC ordering and correlation, preserve corrections without overwriting history, and avoid credentials or unnecessary protected content.

The specification does not yet decide the failure behavior when a required audit record cannot be durably accepted (`Q-3`/`PROP-10`), nor does it define product-wide retention (`Q-2`). Architecture can still define logical ownership, event contracts, idempotency, and persistence boundaries without selecting a physical database or inventing retention policy.

## Decision drivers

- Preserve security and governance history without dual-write ambiguity.
- Correlate each audit event to the actor/service, organization, action, resource, decision, and authorizing relationship.
- Prevent silent overwrite, deletion, cross-tenant linkage, or duplicate interpretation.
- Keep raw participant content, credentials, and unrestricted identifiers out of audit payloads.
- Support retries, partial failure, investigation, export, correction, and later retention/deletion policy.
- Avoid claiming tamper evidence from a recomputable hash or application convention alone.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Application logs as the audit source of truth | Minimal implementation | Weak schema, retention, isolation, idempotency, access control, and correction semantics |
| Synchronous write to an independent remote audit service | Strong separation | Creates a distributed dual-write and availability dependency for every audited operation |
| Authoritative append in the owning consistency boundary plus idempotent projection | Preserves mutation/audit atomicity, supports retries, and allows physical evolution | Requires append-only constraints, projection monitoring, and explicit handling for reads/denials without an owning mutation |
| Best-effort asynchronous audit for every event | Low request latency | Can silently lose events required for accountability or release safety |

## Proposed decision

### Logical ownership

Create one logical authorization audit stream owned by the platform audit boundary. Each event has a globally unique event identifier, schema version, organization, UTC occurrence time, unambiguous sequence within its authoritative stream, correlation reference, actor/service, action, resource reference, decision/reason, source channel, and the relevant grant, assignment, enrollment, relationship, or delegation reference.

Payloads contain stable references and bounded metadata, not raw tokens, credentials, prompts, submissions, transcripts, evidence, evaluations, reviewer notes, or unrestricted protected content. Audit access and export use the same organization and resource authorization contract as the owning resource.

### Mutation-coupled audit events

When an access-control change or protected mutation requires an audit event, record the authoritative audit event or an immutable outbox event in the same durable consistency boundary as the owning state transition. Project it idempotently into the audit read store by event identifier. The projection may lag, but it must not become a second authoritative history or permit silent loss, mutation, or cross-organization reassignment.

Corrections append a new event that references the original. Normal application paths expose no update or delete operation for an existing audit event. Physical storage must enforce append-only semantics or provide equivalent independently verifiable protection; restricted APIs or hashes alone are not sufficient evidence against an actor able to replace both history and verification data.

### Read, download, export, and denial events

Operations without an owning mutation write directly to a durable audit-ingestion boundary using an idempotency key derived from the trusted operation/correlation context. Policy classifies each event as:

- `required_durable` — the operation may complete only according to the eventual approved disposition of `Q-3`; the current interim default is working guidance only;
- `bufferable` — a bounded durable local buffer may accept the event before asynchronous delivery; or
- `operational_sample` — bounded access telemetry governed by the approved audit policy rather than the authoritative security-audit stream.

No event may be silently downgraded by a client, delivery adapter, or failing audit dependency. Buffer overflow, prolonged delivery failure, sequence conflict, rejected cross-scope event, and projection lag produce bounded alerts and operational metrics without raw protected content.

### Ordering, retries, and verification

Use UTC event times plus an authoritative per-stream sequence or equivalent ordering key; timestamps alone do not establish order. Producers and projectors are idempotent by event identifier. Duplicate delivery returns the existing event status when the trusted payload digest matches and reports a conflict when it does not.

Audit verification must detect missing, altered, duplicated, reordered, or cross-organization-linked events according to the selected physical protection. The physical storage ADR must define the trust anchor, backup/restore verification, privileged maintenance path, and how authorized retention or deletion remains distinguishable from tampering.

## Open questions

- `Q-ADR3-1` — Which physical store and protection mechanism should back the authoritative audit stream and read projection?
  - **Interim default:** For the MVP, use an append-only logical table or event collection in the primary transactional platform store with separate write/read capabilities, database constraints that reject mutation of existing events, immutable backups, and monitored idempotent projection. Revisit a separately administered store when the threat model, scale, or operational separation requires it.
  - **Rationale:** This preserves a single transaction for audited mutations and avoids a premature distributed dual write while keeping the logical boundary extractable.
- `Q-ADR3-2` — How long should authorization audit events and their protected references remain available?
  - **Interim default:** Follow `Q-2` in the authorization specification: preserve the minimum restricted metadata needed for investigation and reconstructability until an approved retention, deletion, legal-hold, and export policy applies.
  - **Rationale:** Architecture must not invent retention or compliance behavior; the event model should support policy once approved.

## Consequences

- Audited mutations avoid an ambiguous state/audit dual write.
- Read and denial auditing gain explicit durability classes instead of implicit best effort.
- Append-only history and correction semantics are testable independently of the physical store.
- Audit projection lag, backlog, and failure require monitoring and recovery procedures.
- `Q-3`/`PROP-10`, physical protection, and retention remain unapproved and block final production audit design.

## Related

- Requirements: [`REQ-AUTH-26`–`REQ-AUTH-29`](../../requirements/features/auth-resource-isolation.md#business-rules)
- Acceptance criteria: [`AC-AUTH-14`, `AC-AUTH-15`](../../requirements/features/auth-resource-isolation.md#acceptance-criteria)
- Open questions: [`Q-2`, `Q-3`](../../requirements/features/auth-resource-isolation.md#open-questions)
- Proposed failure default: [`PROP-10`](../../requirements/features/auth-resource-isolation.md#proposed-default-requiring-approval)
- Authorization enforcement and delegation: [ADR-002](ADR-002-authorization-enforcement-and-delegation.md)
