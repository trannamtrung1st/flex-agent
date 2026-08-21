# ADR-017: Assessment source authority and activation-transaction participation

## Status

Approved — 2026-08-21

## Owners and approvers

- Owner: Architecture Lead
- Required approvers: Product Lead, Architecture Lead, Security/Privacy reviewer
- Proposed date: 2026-08-21
- Approved date: 2026-08-21
- Approval reference: repository owner approval acting for the Product Lead,
  Architecture Lead, and Security/Privacy reviewer after review of the source
  authority, fail-closed Production boundary, IdentityAccess ownership, and
  local Keycloak verification topology

This record is the approved architecture decision for Assessment source
authority and activation-transaction participation. Implementation must fail
closed wherever a required owner cannot provide the approved exact,
transaction-aware validation capability.

## Context

[ADR-004](ADR-004-assessment-activation-baseline-and-atomicity.md) requires
activation to revalidate exact immutable source identities, digests,
compatibility, and organization ownership inside one PostgreSQL consistency
boundary. [Backend module architecture](../backend-module-architecture.md)
requires an explicitly named coordinator and a transaction capability approved
by every state-owning module; ambient transactions, shared connections, and
cross-module SQL are forbidden.

The Assessment setup slice must consume pre-provisioned source families, but
several owning capabilities do not yet exist as production modules:

- Configuration persists Organization-scoped source identity and content digest
  only, with a synthetic source kind and no trusted compatibility or
  effective-value descriptor.
- No Agent, Harness, rubric, workflow, knowledge, memory, or governance-policy
  authoring module exists.
- Sessions owns operator-installed model-deployment profiles, qualification,
  adapter configuration, and credential catalogs in file-loaded in-memory
  registries. Those records cannot serialize eligibility or revocation with the
  PostgreSQL activation transaction.
- Credential binding, secret resolution, and credential revocation are
  downstream Session concerns under `REQ-RSC-30` and `REQ-RSC-46`. They are not
  activation-baseline content.
- No exact OpenAI-compatible live profile is qualified or Production-enabled.

## Decision drivers

- Preserve ADR-004 atomicity and fail-closed fairness.
- Keep one owner per durable record and forbid Assessment-owned SQL against
  another module's tables.
- Distinguish Development/Testing synthetic fixtures from Production authority.
- Prevent Sessions file registries or cached readiness from becoming commit
  evidence.
- Keep credential secrets out of the activation baseline and transaction.
- Allow Assessment domain, readiness, and activation work to proceed without
  inventing Agent/Harness authoring or a live provider qualification.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Treat Sessions file/in-memory registries as activation-transaction authority | Reuses existing operator-state records | Cannot serialize revocation with PostgreSQL; stale eligibility can freeze into a fairness baseline |
| Copy live profile rows into Assessment tables at readiness time | Local SQL simplicity | Dual write, stale copies, and hidden Sessions-ownership bypass |
| Block all Assessment implementation until every source owner is transactional | Strictest consistency | Stalls a required P0 slice whose empty-Cohort activation does not need live providers |
| Configuration-owned PostgreSQL source versions plus readiness descriptors as the only activation-transaction source authority; Production fail-closed for owners that cannot revalidate in-transaction | Unblocks Assessment with an honest Production blocker; keeps Sessions ownership intact | Requires synthetic Development/Testing fixtures and a later Sessions transaction port |

## Decision

### Source-authority matrix

Assessment consumes exact source versions through the owning module's
application port. The activation transaction may read a source only when that
owner can revalidate the exact version inside the shared PostgreSQL
consistency boundary.

| Category | Owning module | Immutable identity | Lifecycle and revocation | Effective-value validator | Selector projection | Activation-transaction participation | Development/Testing fixture |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Organization policy/bounds | Configuration | Organization-scoped source id + version id + digest | Configuration source lifecycle; revoked/unavailable versions fail closed | Assessment fairness/narrowing against the trusted descriptor | Configuration read port | Yes, via Configuration transaction-aware port | Configuration-seeded synthetic descriptor |
| Agent revision | Configuration until an Agent authoring module exists | Same | Same | Assessment compatibility and capability subset | Configuration read port | Yes, only for Configuration-registered versions | Synthetic Agent revision |
| Harness revision | Configuration until a Harness authoring module exists | Same | Same | Assessment compatibility and capability subset | Configuration read port | Yes, only for Configuration-registered versions | Synthetic Harness revision |
| Task and Submission-requirement revision | Assessment Configuration | Activity revision + single Task binding identity | Assessment revision immutability | Assessment required-field and bound validator | Assessment setup projection | Yes, Assessment-owned rows | Created with the draft revision |
| Workflow / adaptive-follow-up policy | Configuration | Configuration source version | Configuration lifecycle | Assessment unconstrained-adaptation rejection | Configuration read port | Yes | Synthetic policy revisions |
| Rubric / evaluation procedure | Configuration | Configuration source version | Configuration lifecycle | Assessment reference-only; no raw rubric copy | Configuration read port | Yes | Synthetic rubric reference |
| Model deployment profile | Sessions owns live operator-installed profiles; Configuration owns synthetic activation-eligible descriptors | Live: Sessions profile identity. Synthetic: Configuration source version | Live: Sessions qualification/eligibility files. Synthetic: Configuration lifecycle | Assessment eligibility against the trusted descriptor; never secrets | Configuration selector for transactional versions; Sessions catalog is not commit authority | Synthetic Configuration versions: Yes. Sessions file/in-memory profiles: No | Configuration-seeded synthetic model-deployment version |
| Knowledge references | Configuration | Configuration source version | Configuration lifecycle | Assessment minimized protected reference | Configuration read port | Yes | Synthetic knowledge references |
| Stable-memory snapshot | Configuration when a snapshot is selected; otherwise Assessment default `no-read` | Snapshot source version + digest, or explicit disabled state | Configuration lifecycle for snapshots | Assessment Stable/no-read and exact-snapshot rules | Configuration snapshot selector | Yes when a snapshot is selected; default no-read needs no external source | Optional synthetic snapshot |
| Capability profile | Configuration | Configuration source version | Configuration lifecycle | Assessment most-restrictive narrowing; P0 disablements | Configuration read port | Yes | Synthetic capability profile |
| Review / Release requirements | Configuration | Configuration source version | Configuration lifecycle | Assessment required-field validator | Configuration read port | Yes | Synthetic review/release requirements |
| Approved exception reference | No Assessment-owned exception workflow | Exact separately approved exception id when present | Owner of the exception record | Assessment fail-closed exception validator | Not a setup authoring selector | Only when an approved transactional exception port exists | None; absence is the no-exception path |
| Credential binding / secrets | Sessions (downstream) | Not an activation identity | Session binding and execution | Out of Assessment scope | Not an Assessment selector | No | Not seeded for activation |

### Configuration readiness descriptor

Configuration must expose a narrow application-facing read and
transaction-aware validation port for each registered source version:

- Organization scope, source id, version id, source kind, procedure, schema
  version, and content digest
- lifecycle/availability (`available`, `revoked`, `unavailable`,
  `mutable_alias`)
- compatibility and capability descriptors
- domain-validated effective-value document
- environment eligibility (`development`, `testing`, `production`)

Assessment stores only normalized effective values plus stable protected
references and digests. It must not query Configuration tables through
Assessment-owned SQL.

A `mutable_alias` or display-name reference is never activatable. Readiness
and commit validation must resolve to an exact immutable version.

### Named activation transaction coordinator

The Assessment application owns
`IAssessmentActivationCoordinator`. The coordinator is the only writer that
may commit an activation attempt, immutable baseline, unique Cohort binding,
`Activated` transition, and required-durable audit/outbox acceptance.

State-owning modules that must approve the shared PostgreSQL transaction
capability for this slice:

- Assessment Configuration — Activity revisions, Task binding, Cohort state,
  activation attempts, baselines, and bindings
- Configuration — exact source version, descriptor, and availability
- IdentityAccess — admission and in-transaction reauthorization
- FlexAgent.Postgres primitives — UTC time, append-only audit, and outbox
  acceptance used by ADR-003/ADR-004

Sessions is not an activation-transaction participant in this slice.

The coordinator must:

- begin one primary-store transaction;
- reauthorize the current actor, action, relationship, and authentication
  strength inside that transaction;
- reload exact expected Activity and Cohort revisions;
- call each participating owner's transaction-aware validation port with the
  same transaction;
- reject stale, revoked, mutable, wrong-scope, digest-mismatched,
  incompatible, widening, or unverifiable sources;
- canonicalize and persist the baseline only after domain-valid
  `effective_value` documents pass Assessment-owned
  `CanonicalJsonLimits`;
- commit all ADR-004 writes or none.

An independent read connection, ambient transaction, cached readiness result,
client digest, or Sessions in-memory registry lookup cannot satisfy commit
validation.

### Production versus Development/Testing

- Development and Testing may seed bounded Configuration-owned synthetic
  source versions for every required category, including a synthetic
  model-deployment profile. Those versions are real PostgreSQL authority for
  the activation transaction.
- Production must fail closed with a bounded, non-sensitive readiness blocker
  for any required category whose owner cannot supply an exact
  transactionally revalidated version.
- Production model-deployment selection against a Sessions file-loaded profile
  is readiness-blocked until a later approved ADR gives Sessions a
  PostgreSQL-serializable validation port. No exact live profile is currently
  qualified.
- Credential binding remains a Session start concern and must not appear in
  the baseline, readiness DTO, or activation command.

### Assessment-owned canonicalization limits

Before hashing or persisting a baseline, Assessment must apply explicit
positive limits and versioned fairness-domain validators. The digest schema's
`additionalProperties: true` and caller-supplied `CanonicalJsonLimits` are not
production defaults.

Interim Assessment-owned limits for `activation-baseline-jcs-sha256-v1`:

- max UTF-8 bytes: 262144
- max nesting depth: 8
- max object properties: 64
- max array elements: 64

Unknown, oversized, over-deep, or domain-invalid `effective_value` members are
rejected before canonicalization.

## Approved decision disposition

The following dispositions and `PROP-7` in the Assessment setup specification
were approved with this ADR on 2026-08-21.

| Question | Approved disposition |
| --- | --- |
| `Q-ADR17-1` | Configuration-owned PostgreSQL source versions and readiness descriptors are the only source authority that may participate in the Assessment activation transaction in this slice. |
| `Q-ADR17-2` | Sessions file/in-memory model-profile, qualification, and credential-catalog records are not activation-transaction participants and are not baseline content. |
| `Q-ADR17-3` | Production activation fails closed when any required category lacks a transactional owner or an exact permitted version. |
| `Q-ADR17-4` | `IAssessmentActivationCoordinator` plus owner-approved transaction ports are the only legal cross-module consistency mechanism. |

## Consequences

- Assessment setup can be implemented and verified with synthetic
  Configuration fixtures without claiming live provider qualification.
- Production readiness remains honest: missing transactional owners are
  blockers, not placeholders.
- A later Sessions PostgreSQL port can join the coordinator without changing
  the baseline schema or Assessment SQL.
- Agent and Harness authoring can later replace Configuration-registered
  synthetic revisions without rewriting Cohort history.
- Tests must encode the fail-closed Production path and must not use a Sessions
  file/in-memory registry as activation-transaction evidence.

## Related

- Requirements: `REQ-ACT-3`, `REQ-ACT-9`–`REQ-ACT-16`, `REQ-ACT-24`,
  `REQ-RSC-9`–`REQ-RSC-14`, `REQ-RSC-30`, `REQ-RSC-46`
- Architecture: ADR-002, ADR-003, ADR-004, ADR-006, backend module
  architecture
- Approved feature decision: `PROP-7` in
  [assessment-setup.md](../../requirements/features/assessment-setup.md)
