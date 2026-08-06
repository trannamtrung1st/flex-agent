# ADR-004: Assessment activation baseline and atomicity

## Status

Approved

## Owners and approvers

- Owner: Architecture Lead
- Approvers: Product Lead, Architecture Lead, UI/UX reviewer, Security/Privacy reviewer
- Approved date: 2026-08-06

## Context

The approved [assessment setup specification](../../requirements/features/assessment-setup.md) requires every activated assessment cohort to bind to one immutable activation baseline before participant enrollment or session start. The baseline freezes fairness-governed configuration, must be independently identifiable and content-verifiable, and is consumed by the approved [resolved session configuration specification](../../requirements/features/resolved-session-configuration.md).

Activation also changes authoritative cohort state. The baseline, cohort binding, `Activated` transition, and required audit event must not diverge under partial failure, duplicate requests, concurrent administrators, process restarts, or an uncertain client response. Readiness is only advisory; authorization and all source assumptions must be revalidated at the activation commit boundary.

ADR-001 already defines canonical representation and integrity for the resolved session configuration and execution manifest. Reusing its procedure identifier for a cohort activation baseline would conflate artifacts with different schemas, owners, lifecycles, and digest coverage. A compatible but distinct procedure is required.

## Decision drivers

- One authoritative immutable baseline per activated cohort.
- Deterministic content verification for equivalent fairness-governed inputs.
- Clear separation among activity revisions, cohort identity, activation attempts, activation baselines, resolved session configurations, and execution manifests.
- Atomic mutation and required durable audit without a distributed dual write.
- Safe retries, duplicate commands, concurrent activation, process failure, and uncertain client outcomes.
- Trusted organization/activity/cohort ownership and commit-time authorization.
- Compatibility with ADR-001 normalization and digest conventions without artifact conflation.
- Minimized duplication of sensitive or large content.
- Historical verification across schema and procedure upgrades.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Reuse ADR-001's resolved-configuration procedure identifier | Minimal new naming and shared implementation | Conflates cohort and session artifacts, makes field coverage ambiguous, and risks historical reinterpretation |
| Use a distinct canonical JSON/SHA-256 baseline procedure compatible with ADR-001 | Deterministic, interoperable, separately versioned, and easy to verify with shared normalization concepts | Requires explicit baseline schema, coverage rules, and conformance fixtures |
| Store only mutable activity and cohort references | Low initial storage and implementation effort | Later edits can change historical meaning and session fairness cannot be verified |
| Copy every source payload into the baseline | Maximum local availability | Duplicates sensitive content and expands authorization, retention, deletion, and consistency risk |
| Create the baseline, cohort transition, and audit event in independent systems | Independent scaling and storage | Introduces distributed dual-write ambiguity and failure modes before scale requires them |
| Use one owning consistency boundary with an immutable audit/outbox append | Atomic authoritative state, retry safety, and compatibility with ADR-003 | Requires explicit idempotency, uniqueness, and recovery rules |

## Decision

### Logical ownership and artifacts

Maintain these distinct logical records:

1. **Activity revision** — the saved editable input state from which a cohort candidate is prepared.
2. **Cohort** — the administrative group and authoritative activation state.
3. **Activation attempt** — one idempotent request outcome, including validation or commit failure.
4. **Cohort activation baseline** — the immutable fairness-governed content and source provenance accepted for an activated cohort.
5. **Audit event** — the immutable security and governance history for the activation request and outcome.

Each record has its own stable identity and authorization scope. Physical co-location is permitted when the distinct immutability, lifecycle, and authorization semantics remain enforceable.

The activation baseline contains normalized execution-relevant values and immutable source references or verified content digests. Large or sensitive prompts, knowledge content, rubric content, participant data, credentials, tokens, and secrets remain in their protected owning stores. The baseline contains stable protected references and digests only when required.

### Canonical content-digest procedure

The initial baseline procedure is `activation-baseline-jcs-sha256-v1`.

The implementation must:

- Normalize the fairness-governed effective values, immutable source references/digests, resolution classifications, decisions, and approved-exception references into the versioned baseline digest document.
- Represent included timestamps as UTC RFC 3339 strings with schema-defined precision.
- Convert semantic sets into arrays sorted by their schema-defined stable key while preserving order for behaviorally ordered arrays.
- Reject non-finite numbers and values outside the approved schema.
- Serialize the digest document as UTF-8 JSON using the [JSON Canonicalization Scheme (RFC 8785)](https://www.rfc-editor.org/rfc/rfc8785).
- Compute SHA-256 over the canonical bytes and encode the digest as lowercase hexadecimal.
- Store the procedure identifier, schema version, canonicalization version, and digest with the baseline.

The digest document covers the fairness-governed source set and effective cohort rules, including agent, harness, activity revision, task/submission requirements, model, knowledge, capabilities, workflow, rubric/evaluation procedure, Stable-memory state, adaptive-follow-up policy, cohort timing/deadline/attempt rules and bounds, review/release requirements, resolution decisions, and approved-exception references.

The digest excludes the generated baseline identifier, activation-attempt identifier, cohort identifier, actor, activation timestamp, persistence metadata, and other schema-declared binding metadata. This permits separately owned cohorts with equivalent fairness-governed content to have the same content digest while retaining distinct baseline identities and bindings. The organization/activity/cohort ownership chain is validated and atomically bound separately; a matching digest never grants access or permits cross-scope substitution.

Any future baseline procedure uses a new identifier and explicit migration/verification behavior. A newer procedure must not reinterpret a historical digest as if it originally applied.

### Atomic activation boundary

Use one authoritative consistency boundary to commit all of the following or none:

- activation-attempt success state;
- immutable baseline identity, digest, content, and provenance;
- unique cohort-to-baseline binding;
- cohort transition to `Activated`; and
- an immutable authoritative audit event or immutable outbox event accepted under ADR-003.

Cohort activation is classified as `required_durable`. The operation must not report success or expose the cohort as activated unless the audit record or immutable audit outbox append is durably accepted in the owning consistency boundary.

For the MVP, use the primary transactional platform store selected by ADR-003 for this mutation-coupled boundary. This decision does not require a distributed transaction, remote audit service, queue, or workflow engine. An idempotent projector may populate audit and read projections after the authoritative commit.

### Authorization and validation

At command admission and again inside the authoritative commit boundary, the activation service must:

- authenticate its human or service actor;
- derive organization, activity, cohort, and source ownership from trusted state;
- authorize the current action and delegated resource scope through ADR-002's authorization kernel;
- load the exact expected activity revision;
- validate required immutable source identities, digests, compatibility, and organization ownership;
- enforce upper-scope narrowing and Stable-memory/MVP capability restrictions;
- validate any separately approved exception and additional approver; and
- reject stale, revoked, cross-scope, incomplete, conflicting, or unverifiable state.

A readiness result, client-provided identifier or digest, cached positive decision, role label, or prior page access is never sufficient commit-time evidence.

### Idempotency, concurrency, and uncertain outcomes

Each activation command carries a trusted idempotency key scoped to organization, activity, cohort, and expected draft revision. The owning store enforces at most one authoritative baseline binding for a cohort.

- An equivalent retry after success returns the existing activation result.
- A repeated key with a mismatched trusted command digest reports an idempotency conflict.
- A stale expected revision or competing non-equivalent request fails without changing the winning baseline.
- A timeout or lost client response is reconciled by idempotency key and authoritative cohort binding before retry.
- A failure before commit leaves the cohort unactivated and records the non-sensitive failed attempt/audit outcome required by policy.
- A failure after the authoritative commit is recovered through idempotent projection or response reconciliation; it does not create a second baseline.

### Verification and conformance

Before implementation is accepted, architecture must publish conformance fixtures covering:

- equivalent input objects with different key order;
- Unicode content and normalization-sensitive strings;
- numeric boundaries and rejected non-finite values;
- UTC timestamp normalization where timestamps are included;
- semantic-set ordering and behaviorally ordered arrays;
- excluded identity/binding metadata;
- one-field changes across every fairness-governed domain;
- altered or missing source digests;
- equivalent content across separately identified cohorts;
- idempotent retry, mismatched-key conflict, concurrent activation, audit failure, and uncertain-response reconciliation; and
- cross-organization, cross-activity, and cross-cohort substitution attempts.

Verification recomputes the content digest using the recorded procedure and schema version. It validates ownership and binding independently. An unverifiable historical source produces an honest degraded or failed verification status under the governing lifecycle policy; it must not be replaced by a current source.

## Approved decision disposition

| Question | Approved disposition |
| --- | --- |
| `Q-ADR4-1` | Use the distinct `activation-baseline-jcs-sha256-v1` RFC 8785/SHA-256 procedure. Do not reuse ADR-001's resolved-configuration procedure identifier. |
| `Q-ADR4-2` | Exclude generated identity and binding metadata from the content digest so equivalent cohort content may share a digest; protect organization/activity/cohort binding through trusted authorization and the atomic consistency boundary. |
| `Q-ADR4-3` | Commit baseline creation, unique cohort binding, `Activated` transition, and immutable audit/outbox acceptance in the primary transactional platform store for the MVP. |
| `Q-ADR4-4` | Classify activation as `required_durable`; no activated state is visible when durable audit acceptance fails. |

## Consequences

- Activation and session-resolution artifacts remain distinct and independently verifiable.
- Equivalent fairness-governed cohort content produces the same digest even when cohort and baseline identities differ.
- Ownership and binding integrity depend on both the content digest and the protected atomic binding; a digest alone is never authorization or tenant evidence.
- Audited activation avoids a state/audit dual write and supports safe retry and recovery.
- Implementations must maintain a versioned baseline schema, canonicalization procedure, conformance fixtures, unique binding constraint, idempotency contract, and recovery path.
- Source content stays minimized, but historical verification still depends on the governing retention and archival policy for referenced sources.
- A future topology may extract storage or audit projection only if it preserves the same authoritative consistency and idempotency semantics.

## Related

- Requirements: [`REQ-ACT-9`–`REQ-ACT-24`, `REQ-ACT-35`–`REQ-ACT-42`](../../requirements/features/assessment-setup.md#business-rules)
- Acceptance criteria: [`AC-ACT-6`–`AC-ACT-8`, `AC-ACT-13`–`AC-ACT-18`, `AC-ACT-25`](../../requirements/features/assessment-setup.md#acceptance-criteria)
- Approved question/proposal disposition: [Assessment setup](../../requirements/features/assessment-setup.md#approved-decision-disposition)
- Resolved-configuration integrity: [ADR-001](ADR-001-resolved-configuration-representation-and-integrity.md)
- Authorization enforcement: [ADR-002](ADR-002-authorization-enforcement-and-delegation.md)
- Audit ownership and persistence: [ADR-003](ADR-003-authorization-audit-persistence.md)
