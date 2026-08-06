# ADR-001: Resolved configuration representation and integrity

## Status

Approved

## Owners and approvers

- Owner: Architecture Lead
- Approvers: Product Lead, Architecture Lead, Security/Privacy reviewer
- Approved date: 2026-08-06

## Context

The approved [resolved session configuration specification](../../requirements/features/resolved-session-configuration.md) requires deterministic configuration digests, immutable behavior-affecting inputs, an append-only runtime manifest, and a terminal integrity seal. It also distinguishes the frozen effective configuration from runtime provenance while allowing architecture to choose their physical storage.

Without one versioned representation and integrity procedure, equivalent inputs could produce different digests, consumers could disagree about which fields are covered, and historical verification could silently change after a serializer or schema upgrade.

## Decision drivers

- Deterministic results for equivalent normalized inputs.
- Historical verification across schema and implementation upgrades.
- Clear separation between immutable effective configuration and append-only runtime provenance.
- Data minimization and avoidance of unnecessary sensitive-content duplication.
- Cryptographic agility without reinterpretation of historical records.
- Authorization and isolation independent of integrity verification.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Versioned canonical JSON plus SHA-256 | Interoperable, deterministic, widely supported, inspectable, and easy to test with shared fixtures | Requires explicit domain normalization and versioned coverage rules |
| Implementation-native serializer plus SHA-256 | Low initial effort | Serializer changes and cross-language differences can change digests silently |
| Store only immutable source references | Minimizes duplication | Runtime remains dependent on source availability and cannot directly consume the effective values |
| Copy every contributing source into the resolved record | Maximizes local availability | Excessive duplication expands sensitive-data, retention, access-control, and consistency risk |
| One mutable physical manifest containing configuration and runtime records | Simple physical shape | Blurs the freeze boundary and increases accidental mutation risk |

## Decision

### Logical artifacts

The system must maintain two linked logical artifacts with stable, independent identifiers:

1. an immutable resolved session configuration containing normalized execution-effective values and source/decision provenance; and
2. an append-only resolved execution manifest that references the configuration identifier and digest and records ordered runtime provenance.

The artifacts may share a physical store or transaction boundary. Physical co-location must not weaken their distinct immutability, append, authorization, retention, or verification semantics.

### Source materialization

The resolved configuration must contain the normalized effective values needed for execution and immutable source identifiers plus verified digests needed for provenance. Source content may be copied only when it is required to guarantee execution or reconstruction and the applicable authorization, privacy, retention, and data-minimization policies permit the copy.

Large or sensitive submissions, transcripts, prompts, outputs, evidence, credentials, and secrets remain in their protected owning stores. The configuration and manifest contain stable protected references and digests where required, never raw credentials or secret values.

### Canonical representation and configuration digest

The initial procedure is `rsc-jcs-sha256-v1`:

- Normalize the effective configuration and required provenance into the schema-defined digest document before serialization.
- Represent timestamps included in the digest document as UTC RFC 3339 strings with a schema-defined precision.
- Convert semantic sets into arrays sorted by their schema-defined stable comparison key; preserve array order where order is behaviorally meaningful.
- Reject non-finite numbers and values that cannot be represented by the approved schema.
- Serialize the digest document as UTF-8 JSON using the [JSON Canonicalization Scheme (RFC 8785)](https://www.rfc-editor.org/rfc/rfc8785).
- Compute the SHA-256 digest over the resulting bytes and encode it using lowercase hexadecimal.
- Exclude only fields that the versioned schema explicitly classifies as non-digest metadata, such as generated record identifiers and persistence timestamps.

The stored configuration records the procedure identifier, schema version, canonicalization version, and digest. Any future procedure uses a new identifier; it must not reinterpret or recompute a historical digest as if the new procedure had originally applied.

### Terminal manifest seal

The initial procedure is `manifest-jcs-sha256-v1`. At a terminal state, the system constructs a seal document containing:

- the manifest schema and seal-procedure identifiers;
- the resolved configuration identifier and digest;
- the ordered required runtime records, including their sequence values and protected payload/evidence references;
- the terminal state and reason category; and
- the organization, activity, participant/resource-subject, attempt, and session ownership identifiers required by the manifest contract.

The system applies the same domain normalization and RFC 8785 UTF-8 serialization rules, then computes a lowercase hexadecimal SHA-256 digest. The terminal digest must be committed through an authorized append-only or equivalently tamper-evident boundary that prevents covered history and its recorded digest from being silently replaced together. Verification must fail when covered content is altered, missing, duplicated, or reordered. Corrections and verification findings are appended as separate records and never rewrite covered history.

A recomputable digest is not tamper evidence by itself; integrity depends on the protected boundary that records and verifies it. A digest or seal does not authenticate an actor, grant access, override deletion or retention policy, or prove that external model behavior was deterministic.

### Conformance

Before implementation is accepted, architecture must publish cross-language conformance fixtures covering equivalent objects with different input key order, Unicode content, numeric boundaries, timestamp normalization, semantic-set ordering, behaviorally ordered arrays, excluded metadata, altered content, missing runtime records, and reordered runtime records.

## Consequences

- Consumers share one versioned digest and seal contract.
- Configuration and runtime history remain distinguishable even if stored together.
- Historical records retain the procedure needed for later verification.
- Implementations must normalize domain values before canonical serialization and maintain conformance fixtures.
- SHA-256 replacement requires a new procedure version and migration strategy; historical records retain their original procedure.
- Retention and source-copy choices remain constrained by approved privacy and lifecycle policy.

## Related

- Requirements: [`REQ-RSC-15`–`REQ-RSC-20`, `REQ-RSC-29`–`REQ-RSC-37`](../../requirements/features/resolved-session-configuration.md#business-rules)
- Acceptance criteria: [`AC-RSC-3`, `AC-RSC-7`, `AC-RSC-14`, `AC-RSC-17`–`AC-RSC-19`, `AC-RSC-23`](../../requirements/features/resolved-session-configuration.md#acceptance-criteria)
- Approved question/proposal disposition: [Resolved session configuration](../../requirements/features/resolved-session-configuration.md#approved-decision-disposition)
- Authorization boundary: [Authorization and isolation](../../requirements/features/auth-resource-isolation.md)
