# Feature: Resolved session configuration

## Status and source

- Status: Approved
- Version: 0.4
- Owner: Product Lead
- Approvers: Product Lead, Architecture Lead, Security/Privacy reviewer
- Approved date: 2026-08-14
- Approval history: Baseline approved 2026-08-06; v0.2 approved 2026-08-11 to freeze behaviorally material Agent Invocation/Decision policy; v0.3 approved 2026-08-11 to freeze optional Agent-requested next-timer replacement policy; v0.4 approved 2026-08-14 to freeze the P0-compatible Decision-output profile
- Source: [Agent Invocation, Invocation Trigger, and Agent Decision](../../product/concept-model.md#agent-invocation-invocation-trigger-and-agent-decision), [Effective configuration resolution](../../product/concept-model.md#effective-configuration-resolution), [Configuration precedence stack](../../product/concept-model.md#configuration-precedence-stack), [Assessment fairness constraints](../../product/concept-model.md#assessment-fairness-constraints), [Resolved execution manifest](../../product/concept-model.md#resolved-execution-manifest), [Session state and events](../../product/concept-model.md#session-state-and-events), [Product invariants](../../product/concept-model.md#product-invariants), [MVP validation slice](../../product/mvp-scope.md#mvp-validation-slice)
- Catalog entry: P0 #2 — [P0 authoring order](../README.md#p0-authoring-order)
- Related decisions: Consumes the approved authorization and isolation contract in [`auth-resource-isolation.md`](auth-resource-isolation.md). Open questions `Q-1`–`Q-7` and proposals `PROP-1`–`PROP-10` were approved on 2026-08-06 and incorporated into the normative sections identified in [Approved decision disposition](#approved-decision-disposition). Technical representation and integrity choices are governed by [ADR-001](../../architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md); [ADR-006](../../architecture/decisions/ADR-006-mvp-architecture-baseline-and-evolution.md) governs the relational primary and modular runtime topology; [ADR-008](../../architecture/decisions/ADR-008-bounded-oss-component-set.md) governs selected infrastructure, provider defaults, and scoped credential/BYOK bindings; [ADR-012](../../architecture/decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md) governs the Agent Invocation/Decision fields and provenance; and [ADR-013](../../architecture/decisions/ADR-013-agent-requested-next-timer-replacement.md) governs the optional next-timer replacement policy; [ADR-014](../../architecture/decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md) governs the P0-compatible Decision-output envelope. Detailed schema, append, reconstruction, and transaction implementation remain architecture and implementation work.

Version 0.4 is **approved** and supersedes version 0.3 while preserving its
previously approved configuration, manifest, Invocation/Decision, and next-timer
behavior.

## Problem and measurable outcome

A Flex Agent session combines configuration from organization policy, an agent revision, a harness revision or snapshot, an activity revision, a cohort fairness baseline, and session-linked records such as the authorized participant, attempt, task, rubric, model deployment, knowledge sources, and memory policy.

Those sources can overlap, change independently, or contain incompatible values. A mutable identifier such as “current harness,” “latest model,” or an unhashed knowledge source is not enough to explain what a session actually used. Resolving configuration lazily during conversation also creates fairness, authorization, reliability, and audit risk: two participants in the same activated cohort could receive materially different behavior, and later edits could silently change the historical meaning of an existing session.

This feature establishes the session-boundary contract that:

1. resolves permitted configuration through the approved precedence stack;
2. verifies the result against organization, authorization, and cohort-fairness boundaries;
3. freezes the exact effective configuration before execution starts;
4. creates a versioned resolved execution manifest with immutable references or content digests;
5. binds every runtime and evaluation operation to that frozen record; and
6. preserves append-only provenance so reviewers can reconstruct and explain the session later.

The feature does not promise byte-for-byte reproduction of nondeterministic model output. It promises configuration reconstructability, evidence traceability, historical explainability, versioned behavior, and auditable runtime provenance.

The measurable outcome is:

- Every started session has exactly one immutable resolved session configuration and one linked resolved execution manifest.
- No participant-facing interaction, model call, evidence collection, or evaluation begins before the configuration freeze commits successfully.
- One hundred percent of required configuration inputs are represented by immutable content, immutable references, or versioned references with verified content digests.
- Lower-scope configuration never widens an upper-scope capability or policy boundary.
- Cohort-frozen assessment inputs cannot drift silently at session start.
- Resolution conflicts, missing required versions, hash mismatches, unauthorized overrides, and unavailable mandatory dependencies block session start without partial execution.
- Later edits to organization policy, agents, harnesses, activities, knowledge, memory, models, workflows, or rubrics do not mutate or reinterpret an existing session record.
- Authorized reviewers and administrators can inspect a human-readable configuration summary and the underlying version/hash provenance without receiving secrets or unrelated participant data.
- Automated tests verify deterministic resolution, immutability, conflict handling, concurrency, authorization, audit production, and reconstruction for every supported session profile.

## Actors and permissions

Permissions are action- and resource-scoped under the approved authorization contract. Possession of a session, activity, configuration, or manifest identifier is not proof of access.

| Actor | Permitted actions and scope | Explicit restrictions |
| --- | --- | --- |
| Participant | Request the start of an authorized attempt/session; receive participant-relevant instructions, timing, enabled interaction modes, and a non-sensitive configuration summary when product policy exposes one | Cannot select revisions at session start, override policy, inspect internal prompts, hidden evaluation instructions, reviewer-only configuration, secrets, another participant's configuration, or raw manifest internals |
| Activity administrator | Within delegated activity scope, inspect resolution readiness, start-blocking errors, source versions, cohort-baseline linkage, approved override references, and non-secret resolved values | Cannot introduce an ad hoc session-only policy widening, replace a cohort baseline, access another organization, or bypass required revisions/hashes |
| Organization administrator | Within delegated organization and sensitive-configuration scope, inspect policy provenance, approve only the override actions separately authorized by policy, and investigate resolution failures | Organization membership alone does not grant raw participant/session content or unrestricted secret/configuration access |
| Reviewer | For assigned sessions, inspect the resolved configuration, execution manifest, rubric/model/knowledge provenance, fairness baseline, and runtime records needed to review an outcome | Cannot inspect unassigned sessions, secrets, unrelated tenant configuration, or mutate the frozen record |
| Session configuration resolver | Under an explicit service identity and bounded delegation, read authoritative source revisions, resolve effective values, validate constraints, compute digests, persist the frozen record, and emit audit events | Has no implicit cross-organization access; cannot trust client-supplied ownership or revision claims; cannot start execution after failed resolution |
| Session execution service | Read the frozen configuration by trusted session binding; use only enabled capabilities and pinned references; append permitted runtime provenance to the manifest | Cannot re-resolve from mutable “current” definitions, change frozen values, or use capabilities absent from the resolved configuration |
| Evaluation service | Read the frozen rubric, evidence policy, model/runtime provenance, and session records needed to generate an internal evaluation | Cannot substitute a newer rubric, workflow, model configuration, or knowledge source after the session starts |
| Audit or compliance reviewer | Within explicitly delegated scope, inspect resolution decisions, overrides, hashes, failures, and manifest history | Cannot use audit access as a general route to raw protected content; access itself is audited where policy requires |

## Scope

### Platform-wide scope

#### Platform-wide behavior in scope

- Resolution of organization, agent, harness, activity, and session-bound inputs through the approved precedence stack.
- Validation that lower scopes narrow or supply permitted parameters without widening upper-scope capabilities.
- Consumption of an immutable activity/cohort activation baseline when the activity profile requires fairness freezing.
- Binding of participant, enrollment, attempt, task, cohort, rubric, workflow, model, knowledge, memory, and policy references applicable to the session.
- Deterministic conflict handling: reject, select the most restrictive valid value, or apply an explicitly authorized and audited override.
- Materialization of a canonical resolved session configuration with schema version, resolver version, source provenance, effective values, decisions, and content digest.
- Creation of a linked resolved execution manifest before session execution begins.
- Immutable references or content hashes for agent, harness, activity, workflow, rubric, knowledge, memory, model deployment, and other behavior-affecting inputs.
- Atomic session binding so execution cannot observe a partially written or uncommitted configuration.
- Idempotent resolution and start behavior under client retries, service retries, and concurrent start requests.
- Detection of stale, missing, unauthorized, internally inconsistent, or hash-mismatched source material.
- Append-only runtime provenance associated with the manifest, including model calls and future permitted tool execution.
- Sealing and verification of manifest history when the session reaches a terminal state.
- Authorized inspection, export, audit, and reconstruction of the configuration and manifest.
- Safe failure, retry, and preservation of failed or aborted resolution history.

### Assessment MVP profile

#### Assessment behavior in scope

- One participant per isolated session.
- Campaign activity as the MVP deployment form.
- Consumption of the cohort configuration frozen by `assessment-setup.md`.
- Stable memory with either approved-memory reads disabled or an immutable memory snapshot pinned at cohort activation.
- Pinned task instructions, submission requirements, rubric, text workflow, timing and attempt parameters, model deployment, knowledge sources, evaluation procedure, and release-review requirements.
- An explicit empty or disabled tool-capability set because tool execution is deferred from the MVP.
- Verification that sessions in the same activated cohort use the same fairness-governed baseline for all material comparison dimensions.
- Blocking a session whose required baseline or source versions no longer match their recorded hashes.

### Out of scope

- Creating, editing, approving, or publishing organization policies, reusable agents, reusable harnesses, knowledge libraries, model deployments, rubrics, or workflows.
- General agent and harness library management, covered by P1 specifications.
- Activity creation and cohort activation, including deciding which configuration is frozen for a cohort, covered by [`assessment-setup.md`](assessment-setup.md).
- Enrollment, attempt authorization, attempt consumption, and submission versioning, covered by [`submission-attempts.md`](submission-attempts.md).
- Live conversation stages, pause, resume, completion, timing behavior, and participant examination, covered by [`session-text-lifecycle.md`](session-text-lifecycle.md).
- Evidence semantics and evaluation generation, covered by [`evidence-evaluation.md`](evidence-evaluation.md).
- Human review decisions and result release, covered by [`review-result-release.md`](review-result-release.md).
- Tool execution behavior and tool-specific authorization, deferred to [`tool-execution-permissions.md`](tool-execution-permissions.md).
- Dynamic memory and learning behavior.
- Harness snapshot comparison, restoration, or rollout.
- Shared multi-participant real-time sessions.
- Exact reproduction of nondeterministic model output or external-provider behavior.
- Selecting a database, event store, canonical JSON library, digest library, transaction mechanism, or policy engine.
- Product-wide retention durations, deletion schedules, legal holds, or archival policy.

## User journeys and state transitions

### Configuration lifecycle

This state machine describes the configuration boundary, not the full participant session lifecycle.

```text
Unresolved
    │ authorized start/resolution request
    ▼
Resolving
    ├── validation/conflict/dependency failure ──► Resolution failed
    │                                                │ safe retry after correction
    │                                                └──────────────► Resolving
    │
    └── atomic freeze + session binding
                         ▼
                       Frozen
                         │ execution begins using frozen ID/digest
                         ▼
                       In use
                         ├── session terminates or start fails after freeze
                         │                      ▼
                         │                    Aborted
                         │
                         └── session completes
                                                ▼
                                              Sealed
```

`Frozen`, `Aborted`, and `Sealed` records are immutable. An administrative annotation or later verification result may be appended, but the effective configuration is never rewritten.

### Authorized participant starts a session

1. The participant requests start for an authorized enrollment and attempt.
2. The start boundary resolves trusted organization, activity, cohort, participant, enrollment, attempt, task, and session ownership.
3. The resolver loads the exact cohort baseline and source revisions named by that baseline.
4. The resolver verifies source availability, organization ownership, active authorization, revision status, content digests, and required compatibility.
5. The resolver applies the precedence and conflict rules to produce one canonical effective configuration.
6. The resolver validates that all behavior-affecting references are immutable or content-addressed and that no lower scope widens an upper boundary.
7. The resolver creates the resolved session configuration, initial execution manifest, configuration digest, and audit event in one atomic start boundary.
8. The session is bound to the frozen configuration and manifest identifiers.
9. Only after the commit succeeds may the execution service send instructions, call a model, accept participant interaction, or collect evidence.
10. Downstream services use the trusted session binding and do not independently select “current” configuration.

### Activity administrator inspects readiness

1. The administrator opens an activity or cohort readiness view within delegated scope.
2. The system reports whether all required versioned inputs are available and hash-verifiable.
3. The view identifies blocking categories such as missing model deployment version, mutable knowledge reference, invalid override, incompatible schema, or baseline drift.
4. The administrator can follow the owning workflow to correct the source configuration; the readiness view does not mutate the frozen cohort baseline or create a session-only widening.
5. A later start request performs authoritative validation again at the commit boundary.

### Reviewer inspects historical configuration

1. The reviewer opens an assigned session.
2. The system authorizes access to the session and linked configuration/manifest.
3. The reviewer sees a readable summary of the effective agent, harness, activity/cohort baseline, model, knowledge, memory, workflow, rubric, generation parameters, and approved overrides.
4. The reviewer can inspect stable identifiers, versions, digest status, and runtime provenance needed to evaluate fairness and explainability.
5. Secrets, credentials, hidden provider tokens, unrelated organization data, and configuration outside the assignment remain unavailable.
6. The reviewer can verify that the configuration digest and manifest chain remain valid.

### Resolution fails before start

1. A required source is missing, stale, unauthorized, hash-mismatched, mutable-only, or incompatible.
2. The resolver records a stable failure category and operational correlation reference.
3. No resolved configuration is marked frozen, no participant interaction begins, and no partial execution side effect is committed.
4. The participant receives a non-technical unavailable/retry message.
5. An authorized administrator receives actionable, non-secret diagnostics.
6. A corrected retry either resolves successfully or produces a new failed resolution attempt; prior failure history remains inspectable.

### Duplicate or concurrent start requests

1. Two or more requests attempt to start the same authorized attempt/session.
2. The system uses a trusted idempotency or uniqueness boundary for the session start.
3. Exactly one frozen configuration and one initial manifest become authoritative.
4. Equivalent retries return the same authoritative identifiers and status.
5. A conflicting request cannot create a second active session, overwrite the first configuration, or consume a different set of mutable source values.

### Source changes after freeze

1. An authorized administrator changes an agent, harness, activity, policy, model binding, rubric, knowledge source, memory source, or workflow after a session is frozen.
2. The changed source receives a new revision or digest.
3. The existing session continues to reference the exact prior revision/content.
4. The existing resolved configuration and manifest remain unchanged.
5. A new session resolves under the applicable current or newly activated baseline.
6. A material cohort-fairness change requires the activity/cohort workflow to create a new valid baseline rather than silently affecting in-flight participants.

### Execution appends runtime provenance

1. A runtime component reads the trusted manifest and confirms the permitted operation.
2. The component performs the model call or future permitted tool action using the frozen configuration.
3. It appends a sequenced runtime record containing the operation type, stable configuration references, provider/request identifiers when available, input/output or evidence references, timing, status, and error category.
4. Runtime records reference protected payloads rather than duplicating unnecessary raw content.
5. An append failure that would break required auditability blocks or safely pauses the owning operation according to the session lifecycle contract.

### Session terminates or fails after freeze

1. A session may terminate before meaningful interaction because of infrastructure failure, participant abandonment, authorization change, or administrative termination.
2. The frozen configuration and manifest are preserved.
3. The manifest records the terminal or aborted state and the reason category.
4. Whether the attempt is consumed is decided by `submission-attempts.md`; this feature does not delete or repurpose the configuration.
5. A retry that requires a new session receives a new configuration/manifest identity even when the effective digest is identical.

### Prohibited transitions

- `Unresolved` directly to participant interaction, model execution, evidence collection, or evaluation.
- `Resolution failed` to active execution without a new successful resolution.
- `Frozen`, `Aborted`, or `Sealed` to a changed effective configuration.
- A mutable “current,” “latest,” or alias-only reference to a frozen behavior-affecting input without an immutable version or verified digest.
- A session request to replace an activity/cohort fairness baseline.
- A lower-scope value to enable a capability prohibited by organization, agent, harness, or activity policy.
- An unapproved override to a resolved value.
- A downstream service to resolve its own newer agent, harness, model, knowledge, rubric, workflow, or memory version.
- A failed or partial persistence operation to mark a session started.
- One session's configuration or runtime records to become linked to another organization, participant, attempt, or session.

## Business rules

### Resolution boundary and precedence

- `REQ-RSC-1` — Every session must be bound to exactly one resolved session configuration and one resolved execution manifest before any participant-facing interaction, model execution, evidence collection, or evaluation begins.
- `REQ-RSC-2` — Resolution must use trusted server-side ownership and relationship data to identify the applicable organization policy, agent revision, harness revision or snapshot, activity revision, cohort baseline when applicable, enrollment, attempt, task, workflow, rubric, model deployment, knowledge sources, memory policy, and session parameters.
- `REQ-RSC-3` — Resolution must apply the precedence order `Organization → Agent → Harness → Activity → Session binding`, where the session records the result and does not define new policy.
- `REQ-RSC-4` — A lower layer may narrow a permitted capability or supply a parameter within an upper-layer schema, but it must not widen a capability, permission, retention boundary, memory permission, tool permission, workflow permission, evaluation permission, or other non-bypassable upper-layer constraint.
- `REQ-RSC-5` — When valid values conflict, the resolver must reject the configuration, select the most restrictive compatible value under an approved deterministic rule, or apply an explicitly authorized override with stable actor, reason, scope, and timestamp references.
- `REQ-RSC-6` — The resolver must record the source and decision rule for every value whose effective result differs from at least one contributing layer or required an override.
- `REQ-RSC-7` — The assessment MVP must not permit participant-, request-, or session-authored overrides that widen the activated cohort baseline; a material change must be handled through a new approved activity/cohort baseline.
- `REQ-RSC-8` — Resolution must validate the complete organization, activity, cohort, enrollment, attempt, participant, task, and session ownership chain before binding the configuration.

### Fairness baseline and drift

- `REQ-RSC-9` — When an activity profile requires configuration freezing at activity or cohort activation, each session must reference and validate the exact immutable activation-baseline identifier and digest.
- `REQ-RSC-10` — Material fairness dimensions must resolve from the activated baseline, including agent revision, harness revision or snapshot, model deployment, knowledge sources, enabled capabilities, workflow, rubric/evaluation procedure, memory read policy, memory snapshot or no-read state, and applicable adaptive-follow-up policy.
- `REQ-RSC-11` — A session start must fail if a source named by the activation baseline is missing, has a different digest, belongs to a different organization/activity/cohort, or cannot be verified as the approved source.
- `REQ-RSC-12` — Non-material session-bound values such as participant identity, authorized attempt number, session identifier, and permitted timing window may differ per session when the baseline schema explicitly allows them.
- `REQ-RSC-13` — The resolved record must distinguish baseline-frozen values, session-bound values, derived values, and approved override values so fairness review can determine why sessions legitimately differ.
- `REQ-RSC-14` — Stable memory mode alone is insufficient for cohort fairness; the assessment MVP must record either `approved_memory_reads = disabled` or an immutable memory-snapshot identifier and digest frozen at activation.

### Versioning, immutability, and identity

- `REQ-RSC-15` — Every behavior-affecting input must be represented by immutable content, an immutable reference, or a versioned reference accompanied by a verified cryptographic content digest; a mutable reference without a verified immutable identity is invalid.
- `REQ-RSC-16` — The resolved configuration must include a stable identifier, organization/activity/session ownership, schema version, resolver version, source-reference set, effective configuration, resolution decisions, approved override references, UTC freeze timestamp, and configuration digest.
- `REQ-RSC-17` — The configuration digest must be computed from a deterministic canonical representation of the effective configuration and required provenance fields using a versioned digest procedure.
- `REQ-RSC-18` — Equivalent source inputs processed by the same resolver version and canonicalization procedure must produce the same effective configuration and configuration digest, excluding identifiers and timestamps explicitly defined as non-digest metadata.
- `REQ-RSC-19` — The frozen configuration must be immutable. Corrections, annotations, verification results, or administrative notes must be appended as separate records and must not rewrite the effective configuration or original provenance.
- `REQ-RSC-20` — Later changes to any source revision, alias, current pointer, or provider configuration must not alter the resolved values, digest, or historical interpretation of an existing session.
- `REQ-RSC-21` — A new session must receive a new resolved-configuration and manifest identity even when its effective configuration digest matches another session.
- `REQ-RSC-22` — Repeated or concurrent resolution requests for the same session start boundary must be idempotent and must not create competing authoritative configurations or manifests.

### Atomic start and failure behavior

- `REQ-RSC-23` — Freezing the resolved configuration, creating the initial execution manifest, binding both to the session, and recording the start-boundary audit event must succeed atomically or leave the session unstarted.
- `REQ-RSC-24` — A resolver failure, timeout, authorization denial, stale source, incompatible schema, digest mismatch, missing required version, or audit-write failure must fail closed and must not produce participant interaction or downstream execution.
- `REQ-RSC-25` — Resolution retries must use a trusted idempotency boundary and must revalidate current authorization and all unfrozen source assumptions at commit time.
- `REQ-RSC-26` — A resolution attempt that fails before freeze must preserve a non-sensitive failure record with stable reason code, correlation reference, source categories checked, and timestamps.
- `REQ-RSC-27` — A session that aborts after freeze must preserve its resolved configuration and manifest; neither record may be deleted or reassigned merely because no participant interaction completed.
- `REQ-RSC-28` — Downstream services must receive configuration and manifest identifiers through trusted session state and must not accept client-supplied substitutes as authoritative.

### Resolved execution manifest

- `REQ-RSC-29` — Every execution manifest must reference the resolved configuration identifier and digest and must identify the organization, activity, cohort when applicable, enrollment/attempt, participant/resource subject, and session.
- `REQ-RSC-30` — The initial manifest must record at minimum the agent revision, harness revision or snapshot, activity revision, activation baseline, model provider, model identifier, deployment version, credential mode and stable non-secret credential-binding reference/version, knowledge-source versions or hashes, tool-definition set and versions, policy/workflow version, rubric version, memory read/write policy, memory snapshot when enabled, relevant generation parameters, and approved override references.
- `REQ-RSC-31` — In the assessment MVP, the manifest must explicitly record text interaction as enabled, voice as disabled, tool execution as disabled with an empty permitted tool set, Dynamic memory writes as disabled, and approved-memory reads as disabled or pinned to the recorded memory snapshot.
- `REQ-RSC-32` — If a configured model provider cannot supply the immutable deployment/version identity required by the approved product model or an architecture-approved equivalent fingerprint, the assessment session must not start.
- `REQ-RSC-33` — Runtime model calls and future permitted tool operations must append sequenced manifest records containing the configuration reference, operation type, provider or tool identity/version, timing, status, correlation references, and stable input/output or evidence references required for audit and reconstruction.
- `REQ-RSC-34` — Retrieved approved-memory references or content hashes actually used during a session must be appended to the manifest when approved-memory reads are enabled; references outside the pinned snapshot are prohibited.
- `REQ-RSC-35` — Manifest runtime records must be append-only, ordered unambiguously, and protected from silent deletion, replacement, or reordering.
- `REQ-RSC-36` — When a session reaches a terminal state, the system must record a manifest terminal state and verification/seal value covering the frozen configuration reference and required runtime record sequence.
- `REQ-RSC-37` — A seal or digest verifies recorded content integrity but must not be treated as authorization to access that content.

### Inspection, evidence, and audit

- `REQ-RSC-38` — Authorized reviewers and administrators must be able to inspect a human-readable summary of effective values, source layers, versions, digest status, fairness-baseline linkage, resolution decisions, and approved overrides.
- `REQ-RSC-39` — Participant-facing configuration information must be limited to product-approved instructions and non-sensitive operational facts and must not expose hidden prompts, evaluation internals, secrets, provider credentials, reviewer-only configuration, or another actor's data.
- `REQ-RSC-40` — Configuration and manifest reads, lists, exports, and verification operations must enforce the same organization, assignment, participant, and session authorization rules as the owning session.
- `REQ-RSC-41` — Resolution start, success, failure, approved override use, drift detection, digest verification failure, manifest append failure, seal creation, seal verification failure, and authorized export must produce audit events appropriate to their sensitivity.
- `REQ-RSC-42` — Audit and manifest records must reference protected payloads rather than copying unnecessary submissions, transcript content, model prompts, model outputs, evidence, credentials, or secrets.
- `REQ-RSC-43` — Recorded timestamps and sequence values must provide unambiguous ordering and timezone interpretation for resolution, start, runtime operations, terminal state, and verification.
- `REQ-RSC-44` — The system must support automated reconstruction verification that reloads the recorded immutable inputs, re-applies the recorded resolver/canonicalization version where supported, and confirms the stored effective digest without mutating the historical record.
- `REQ-RSC-45` — A reconstruction check that cannot load or verify a required source must report the manifest as unverifiable or degraded with a stable reason; it must not silently substitute a newer source.

### Provider credential resolution

- `REQ-RSC-46` — Provider credential selection must derive from trusted deployment and Organization policy plus an authorized Organization-scoped or deployment-default `SecretSource` binding. Participant, Activity, request, and Session input must not select or widen credential ownership. A missing, revoked, wrong-Organization, or provider-mismatched binding must fail closed before model work and must not silently fall back to another credential, payer, or provider.
- `REQ-RSC-47` — The resolved configuration must include or reference the
  versioned Agent Invocation contract, permitted typed trigger subset, permitted
  Agent Decision types, output kinds, requested-action kinds, and validation
  schemas, Agent-initiated communication
  policy, intentional no-action policy, and positive invocation retry/chaining
  bounds whenever those values can change Session behavior, fairness, permitted
  capability, or reconstruction.
- `REQ-RSC-48` — For an activated assessment cohort, behaviorally material
  Invocation/Decision policy must be part of the frozen cohort baseline or an
  immutable referenced policy with verified digest. A mutable runtime setting
  must not add a trigger, decision capability, output kind, requested-action
  kind, Agent-initiated communication
  path, or looser chain bound for an in-flight cohort or Session.
- `REQ-RSC-49` — The P0 assessment profile must explicitly permit only the
  Invocation Trigger, Agent Decision, output-kind, and requested-action subset
  required by approved text Session behavior. Voice/Interaction Controller
  signals, Participant Session tools,
  silence-driven triggers, arbitrary or parallel timer lanes, and richer
  configurable workflow triggers must remain disabled even when their types are
  representable by the contract. One optional system timer lane and bounded
  next-timer replacement may be enabled only under `REQ-RSC-51`–`REQ-RSC-53`.
- `REQ-RSC-50` — Manifest runtime provenance for admitted Agent Invocations must
  link the trusted trigger, configuration, bounded execution attempts, resulting
  Agent Decision or execution failure, validation outcome, and authoritative
  domain effect or explicit no-domain-effect outcome. The latter must still
  preserve the lifecycle and bookkeeping records needed to terminalize the
  Invocation and any linked Turn or response slot. Provenance must use minimized
  facts or stable protected references and must not copy complete transcript,
  Submission, hidden prompt, credential, raw controller telemetry, or chain-of-
  thought content by default.
- `REQ-RSC-51` — When the P0 text Session enables Agent timer behavior, the
  resolved configuration must freeze one Session timer lane with an enabled
  flag, positive default relative delay, positive minimum and maximum
  Agent-requested delays, active-time clock basis, permitted lifecycle stages,
  permitted timer-triggered Decision capabilities, cooldown, concurrency,
  duplicate-suppression, total replacement, and timer-triggered Invocation
  bounds. When disabled, neither a default timer nor an Agent-requested timer
  effect may be scheduled.
- `REQ-RSC-52` — Timer-lane policy must be part of the immutable cohort baseline
  or a verified immutable referenced policy. Lower scopes may disable the lane
  or narrow its delays, stages, capabilities, and budgets but must not enable or
  widen it for an active cohort or Session.
- `REQ-RSC-53` — Manifest provenance must record or reference the timer-lane
  policy version, default schedule, accepted/rejected/superseded replacement
  outcomes, stable lane and schedule revision, due/fired/cancelled/expired
  state, driving Decision, resulting trusted timer trigger and Invocation, and
  bounded reason categories without copying model content or hidden reasoning.
- `REQ-RSC-54` — The resolved configuration must include or reference the
  supported Agent Decision envelope/schema version, the permitted P0 output
  kinds, the permitted requested-action kinds, and the rule that lower scopes
  may narrow but not widen those sets. Historical v1 reconstruction policy must
  remain part of the frozen contract identity.
- `REQ-RSC-55` — The P0 assessment profile must freeze at most one Participant
  `message` output, zero `voice` outputs, no reviewer/administrator/runtime-only
  presentation outputs, and no requested action other than the optional
  next-timer replacement. Representable later kinds remain disabled for an
  in-flight cohort or Session.

## Data, evidence, and audit

### Logical records

The following are logical product records. Architecture may store them as separate documents, relational records, event projections, or another design that preserves the contract.

| Record | Purpose | Minimum content |
| --- | --- | --- |
| Resolution attempt | Preserve each start-boundary resolution outcome | Attempt ID/idempotency key, trusted session/attempt scope, resolver version, started/finished timestamps, outcome, stable error categories, correlation ID |
| Resolved session configuration | Frozen effective values used by the session | Configuration ID, ownership chain, source references, baseline reference, effective values including provider credential mode and opaque binding reference/version, decisions, override references, schema/resolver/canonicalization versions, freeze time, digest |
| Resolved execution manifest | Connect frozen configuration to runtime provenance | Manifest ID, configuration ID/digest, required model and non-secret credential-binding provenance plus knowledge/tool/policy/workflow/rubric/memory/generation fields, runtime sequence, terminal state, seal/verification fields |
| Manifest runtime record | Append-only operation provenance | Sequence, type, service actor, configuration reference, provider/tool identity and version, timestamps, status, request/correlation references, protected payload/evidence references |
| Agent Invocation/Decision provenance | Reconstruct why Agent reasoning ran, what was recommended, and what domain effect was permitted or explicitly omitted | Invocation/Decision contract versions, typed trusted-trigger reference, configuration/policy references, attempt and validation outcomes, domain-effect/no-domain-effect reference, minimized protected provenance |
| Agent timer schedule provenance | Reconstruct the single runtime-owned timer lane without treating model output as schedule authority | Lane-policy version, default and accepted relative delay category, schedule revision, active-time due facts, replacement/validation outcome, driving Decision reference, lifecycle state, trusted timer-trigger and resulting Invocation references |
| Configuration annotation | Preserve later explanation without rewriting history | Author, scope, timestamp, reason, linked configuration/manifest, annotation type, optional verification finding |
| Audit event | Security and governance history | Actor/service, organization, action, resource, decision/result, reason code, timestamp, correlation, relevant assignment/delegation/override reference |

### Minimum source provenance

The source-reference set contains, when applicable:

- Organization-policy revision and digest.
- Agent identifier, agent revision, and digest.
- Harness identifier plus harness revision or immutable snapshot identifier and digest.
- Activity identifier and activity revision.
- Campaign and cohort identifiers for the MVP.
- Activity/cohort activation-baseline identifier, schema version, and digest.
- Task revision and submission-requirement revision.
- Workflow and policy revision.
- Rubric/evaluation-procedure revision.
- Model provider, model identifier, deployment version/fingerprint, and adapter version.
- Knowledge-source identifiers, versions, and content hashes.
- Enabled/disabled capability set and tool-definition set.
- Memory mode, read/write permissions, snapshot/no-read state, snapshot digest, and eligible retrieval scope.
- Generation parameter set and parameter-schema version.
- Approved override identifiers with actor, scope, reason, and time references.
- Resolver, canonicalization, and manifest schema versions.

References must be sufficient to retrieve or verify the historical source under approved retention and archival policy. A display name or mutable alias alone is insufficient.

### Canonical effective configuration

The effective configuration must be normalized into stable domains so consumers do not reinterpret layer precedence independently:

- Identity and communication behavior.
- Instructions and participant-visible guidance.
- Allowed interaction surfaces.
- Capability and tool permissions.
- Workflow/stage policy.
- Task and submission requirements.
- Timing, deadline, and attempt-linked parameters.
- Knowledge bindings.
- Memory read/write and reuse policy.
- Model and generation configuration.
- Evidence requirements.
- Evaluation rubric and procedure.
- Human-review and release requirements.
- Safety, authorization, privacy, and retention constraints.
- Fairness/adaptive-follow-up policy.
- Output schema and completion requirements.
- Agent Invocation/Decision contract versions, permitted trigger/decision
  subset, permitted P0 output and requested-action kinds, Agent-initiated
  communication and no-action policy, validation policy, historical v1
  reconstruction identity, and positive retry/chaining limits when
  behaviorally material.

Each domain records its effective value plus source/decision provenance where needed. Secrets are represented by approved secret references or capability bindings, never copied into the configuration.

### Runtime provenance

The manifest may grow during execution, but the frozen configuration section never changes. Runtime records include only information required for operation, evidence linkage, audit, and reconstruction. Large or sensitive payloads remain in their owning protected stores and are linked by stable references and digests when appropriate.

For the assessment MVP, runtime provenance primarily covers:

- Model request and response correlation.
- Agent Invocation identity, typed trusted-trigger provenance, execution and
  decision outcome, validation result, and domain-effect/no-domain-effect
  correlation.
- Model/deployment identity actually invoked.
- Generation parameters actually applied.
- Transcript and evidence references.
- Workflow and timing events linked by session/event identifiers.
- Evaluation invocation reference and rubric/configuration digest.
- Errors, retries, cancellations, and terminal status.

Tool inputs and results remain empty/disabled in the MVP. The manifest schema reserves a compatible location for later tool-execution records without enabling the capability.

### Required audit events

At minimum, record:

- Resolution requested.
- Resolution succeeded and configuration frozen.
- Resolution failed, with stable non-sensitive category.
- Cohort-baseline drift or source digest mismatch detected.
- Approved override applied or rejected.
- Duplicate/concurrent start deduplicated or denied.
- Session bound to configuration and manifest.
- Manifest runtime append failed or recovered.
- Session marked aborted or manifest sealed.
- Configuration or manifest verification succeeded or failed.
- Authorized configuration/manifest export.
- Administrative annotation added.

Audit events contain references and bounded metadata rather than raw prompts, submissions, transcripts, model outputs, credentials, or hidden evaluation content.

### Evidence and reconstruction

The resolved configuration and execution manifest are evidence about the conditions under which a session ran. Evaluation evidence may cite them when model version, rubric, workflow, knowledge, memory, timing, or fairness configuration is material to a conclusion or review.

A reconstruction operation should be able to answer:

- Which exact revisions and hashes contributed?
- Which values won each overlap or conflict?
- Which upper boundaries constrained lower values?
- Which approved override, if any, was used?
- Which cohort baseline governed the session?
- Which model deployment and generation parameters were actually invoked?
- Which knowledge and approved-memory content was eligible and actually retrieved?
- Did runtime services use the frozen configuration?
- Does the stored digest/seal still verify?
- Which parts are fully reconstructable, externally dependent, or currently unverifiable?

## Quality requirements

### UX and accessibility

- Participant start screens must communicate only actionable states: ready, starting, temporarily unavailable, configuration changed and administrator action required, or access expired.
- Participant errors must not expose internal source identifiers, policy names, hashes, provider details, hidden prompts, or another actor's data.
- Administrator readiness and failure views must group problems by actionable category and distinguish blocking errors from warnings.
- Reviewer/admin manifest views must provide a readable summary before technical details, including effective model, agent/harness/activity versions, cohort baseline, memory state, workflow/rubric, digest verification, overrides, and terminal state.
- Technical identifiers and digests must be copyable, selectable, and labeled; truncated display must provide an accessible way to obtain the complete permitted value.
- Differences between baseline-frozen, session-bound, derived, and override values must be conveyed with text and structure, not color alone.
- Resolution progress, success, and failure states must expose accessible status updates and must not trap keyboard focus.
- Error summaries must move focus appropriately, link to affected fields or source categories, and remain usable at narrow viewport widths.
- Tables and provenance trees must have meaningful headings, linear reading order, keyboard access, and an alternate compact layout on small screens.
- Loading states must not briefly render stale or unauthorized configuration details.

### Performance and reliability

- With pre-versioned source records available in the platform boundary, configuration resolution and atomic freeze must complete in no more than 2 seconds at the 95th percentile, excluding participant network latency and external authentication redirects.
- Resolution must use bounded source reads scoped to one organization/activity/session chain and must not scan unrelated tenants or unbounded participant populations.
- Frequently reused immutable source metadata may be cached only with version/digest-aware keys and organization scope.
- A cached value must be revalidated where required at the freeze commit boundary; cache freshness must not override a recorded baseline or authoritative revocation.
- Resolution and start operations must be idempotent and safe under retries, duplicate client submissions, process restarts, and concurrent requests.
- Partial persistence must not expose a session as started. Recovery must identify and safely complete or roll back an incomplete pre-freeze operation.
- The resolver must fail closed when mandatory authorization, policy, revision, digest, audit, or persistence dependencies are unavailable or inconsistent.
- Manifest runtime appends must preserve sequence integrity under concurrency and retries.
- Verification jobs must be resumable and must report degraded/unverifiable sources rather than silently substituting current versions.
- Resolution latency, failure categories, hash mismatches, drift detection, deduplication, append failures, and verification failures must be observable.

### Security and privacy

- Every source read, resolution operation, manifest read, append, seal, verification, and export must enforce trusted organization and resource scope.
- Client-provided revision IDs, baseline IDs, digests, ownership fields, model IDs, and configuration values are untrusted until validated against authoritative server-side records.
- The resolver must validate complete parent ownership and must not combine sources from different organizations, activities, cohorts, participants, attempts, or sessions.
- Secrets, credentials, API keys, access tokens, private endpoints, and provider authentication material must not be copied into the configuration, manifest, audit events, logs, metrics, traces, or UI.
- Secret usage is represented by an authorized secret binding/reference whose value is resolved only by the permitted runtime boundary.
- Organization BYOK is operator-provisioned through that secret boundary. The
  product stores only an opaque binding reference and bounded non-secret status;
  it does not accept or retain a raw provider key through product UI or API.
- Cryptographic digests and seals must use an approved versioned procedure; weak or deprecated procedures must not be introduced silently.
- A matching digest does not bypass authorization, retention, deletion, or privacy controls.
- Manifest and audit logs must minimize duplicated participant content and use stable protected references where possible.
- Exports must be scoped, authorized, audited, and sanitized for the requesting actor.
- Error responses and observability data must use stable categories and bounded labels without raw protected configuration or unrestricted identifiers.
- Configuration poisoning, cross-tenant reference substitution, mutable-alias substitution, stale-cache use, hash-confusion, duplicate-start races, and manifest-sequence tampering must have negative test coverage.
- If a required source becomes subject to deletion or legal hold, the platform must follow the approved policy while preserving an honest verification status; it must not fabricate reconstructability.

## Acceptance criteria

### `AC-RSC-1` — Successful session start freezes one configuration

- **Given** an authorized participant has an authorized attempt
- **And** all required source revisions and hashes are available and valid
- **When** the participant starts the session
- **Then** exactly one resolved session configuration and one linked execution manifest are committed
- **And** the session is bound to their identifiers and configuration digest
- **And** participant interaction or model execution begins only after that commit succeeds.

### `AC-RSC-2` — Precedence prevents capability widening

- **Given** an upper layer prohibits a capability or sets a non-bypassable limit
- **And** a lower layer requests a broader capability or less restrictive limit
- **When** configuration is resolved
- **Then** the lower value does not become effective
- **And** the system rejects the conflict or applies the approved most-restrictive rule
- **And** the decision and contributing sources are recorded.

### `AC-RSC-3` — Compatible restrictions resolve deterministically

- **Given** multiple layers provide valid but different restrictive values covered by an approved deterministic rule
- **When** the same inputs are resolved with the same resolver version
- **Then** the same effective value and decision provenance are produced
- **And** the same canonical effective configuration digest is produced.

### `AC-RSC-4` — Unauthorized override is rejected

- **Given** a configuration conflict requires an override
- **When** the request has no active authorized override with the required scope and reason
- **Then** session start is blocked
- **And** no widened value is applied
- **And** the rejected override attempt is audited without exposing secrets.

### `AC-RSC-5` — Authorized override is traceable

- **Given** an approved product rule permits an override
- **And** an authorized actor records the override before the applicable freeze boundary
- **When** a session resolves under that approved scope
- **Then** the effective value follows the approved override
- **And** the configuration records the override identifier, actor reference, scope, reason, and timestamp
- **And** the override does not affect resources outside its scope.

### `AC-RSC-6` — Cohort baseline drift blocks start

- **Given** an assessment cohort was activated with a baseline referencing a specific harness, model, knowledge source, rubric, workflow, or memory snapshot digest
- **When** a session start observes a missing source or a different digest
- **Then** the session does not start
- **And** the participant receives a non-technical unavailable state
- **And** an authorized administrator receives a drift category and affected source
- **And** the failure is audited.

### `AC-RSC-7` — Mutable-only references are invalid

- **Given** a behavior-affecting source is referenced only as `current`, `latest`, an alias, or another mutable locator
- **And** no immutable version or verified content digest is available
- **When** resolution runs
- **Then** the source is rejected
- **And** no frozen configuration is created
- **And** the error identifies the source category without exposing protected content.

### `AC-RSC-8` — Stable memory fairness is explicit

- **Given** an assessment cohort uses Stable memory
- **When** a session configuration is resolved
- **Then** the configuration and manifest record either that approved-memory reads are disabled or the exact immutable memory snapshot identifier and digest
- **And** Dynamic writes are disabled
- **And** retrieval outside the pinned snapshot is prohibited.

### `AC-RSC-9` — Required model deployment identity is recorded

- **Given** an assessment session is configured to use a model
- **When** resolution runs
- **Then** the manifest records the provider, model identifier, deployment version or approved immutable fingerprint, adapter version, and relevant generation parameters
- **And** if the required deployment identity cannot be obtained, session start is blocked.

### `AC-RSC-10` — Atomic failure leaves the session unstarted

- **Given** configuration persistence, manifest creation, session binding, or required audit persistence fails
- **When** the start boundary attempts to commit
- **Then** the session is not exposed as started
- **And** no participant interaction, model request, evidence, or evaluation side effect occurs
- **And** the failure can be safely retried.

### `AC-RSC-11` — Duplicate starts are idempotent

- **Given** equivalent duplicate or concurrent start requests target the same authorized attempt/session
- **When** the requests are processed
- **Then** one authoritative configuration and manifest are created
- **And** equivalent successful retries return the same identifiers and status
- **And** no second active session or competing frozen configuration is created.

### `AC-RSC-12` — Source changes do not mutate a frozen session

- **Given** a session has a frozen resolved configuration
- **When** an agent, harness, activity, model alias, knowledge source, rubric, workflow, memory source, or policy later changes
- **Then** the session continues to reference the original immutable versions/content
- **And** its configuration and digest remain unchanged
- **And** a later session resolves independently.

### `AC-RSC-13` — Downstream execution uses the trusted binding

- **Given** a session is bound to a resolved configuration and manifest
- **When** an execution or evaluation service processes the session
- **Then** it loads configuration through the trusted session binding
- **And** it rejects a client-supplied substitute configuration or manifest identifier
- **And** it does not read newer mutable source definitions.

### `AC-RSC-14` — Initial manifest contains the required provenance

- **Given** a session start succeeds
- **When** an authorized reviewer inspects the initial manifest
- **Then** the required agent, harness, activity, cohort baseline, model, knowledge, capability/tool, workflow/policy, rubric, memory, generation, resolver, schema, and override provenance is present
- **And** every applicable behavior-affecting source has an immutable version or verified digest.

### `AC-RSC-15` — Runtime provenance is append-only and sequenced

- **Given** the session performs model calls or another permitted runtime operation
- **When** provenance is recorded
- **Then** each record receives an unambiguous sequence and timestamp
- **And** references the frozen configuration
- **And** retries do not create ambiguous competing sequence entries
- **And** prior runtime records cannot be silently overwritten, deleted, or reordered.

### `AC-RSC-16` — Aborted sessions preserve provenance

- **Given** a session freezes successfully but terminates before normal completion
- **When** the terminal state is recorded
- **Then** the frozen configuration and manifest remain inspectable
- **And** the manifest records an aborted terminal state and reason category
- **And** neither record is reassigned to another session or silently deleted.

### `AC-RSC-17` — Terminal manifest verifies

- **Given** a session reaches a terminal state
- **When** the manifest is sealed and later verified
- **Then** the seal covers the frozen configuration reference and required runtime sequence
- **And** an unchanged record verifies successfully
- **And** altered, missing, or reordered required content produces a verification failure and audit event.

### `AC-RSC-18` — Historical reconstruction confirms the effective digest

- **Given** all required historical sources remain available under policy
- **When** an authorized reconstruction check runs
- **Then** it loads the recorded sources and resolver/canonicalization versions
- **And** reproduces the stored effective configuration digest
- **And** reports source-by-source verification status without mutating the session.

### `AC-RSC-19` — Missing historical source is reported honestly

- **Given** a historical source cannot be retrieved or verified because of approved deletion, provider limitations, corruption, or another failure
- **When** reconstruction runs
- **Then** the check reports a stable degraded or unverifiable status and affected source
- **And** it does not substitute a current source or claim full reconstructability
- **And** the finding is auditable.

### `AC-RSC-20` — Configuration access follows assignment and scope

- **Given** configurations and manifests exist across organizations, participants, cohorts, and sessions
- **When** a participant, reviewer, administrator, service, or auditor reads, lists, verifies, or exports them
- **Then** only records within the actor's current authorized scope are returned or processed
- **And** inaccessible record existence cannot be inferred
- **And** access or export is audited when required.

### `AC-RSC-21` — Sensitive values are not exposed

- **Given** a resolved configuration uses provider credentials, secret bindings, private endpoints, hidden prompts, or reviewer-only instructions
- **When** the configuration is stored, logged, displayed, exported, or included in metrics/traces
- **Then** raw secrets and unauthorized hidden content are absent
- **And** only permitted references or redacted summaries are shown
- **And** runtime components resolve secrets only through their authorized boundary.

### `AC-RSC-22` — Resolution failure states are accessible

- **Given** configuration resolution is in progress, blocked, failed, or ready
- **When** a participant or authorized administrator uses the interface with keyboard navigation or a narrow viewport
- **Then** status changes are announced accessibly
- **And** focus moves to the error summary or safe next action when appropriate
- **And** the state does not rely on color alone
- **And** protected technical details are shown only to authorized actors.

### `AC-RSC-23` — Concurrent manifest appends preserve ordering

- **Given** two authorized runtime components attempt to append provenance concurrently
- **When** the records are committed
- **Then** both receive a deterministic unambiguous order or one is safely retried
- **And** no required record is lost or overwritten
- **And** the terminal seal covers the committed order.

### `AC-RSC-24` — Assessment MVP records deferred capabilities as disabled

- **Given** an MVP text assessment session resolves successfully
- **When** the manifest is inspected
- **Then** text interaction is enabled
- **And** voice interaction, tool execution, Dynamic memory writes, and shared-session behavior are explicitly disabled
- **And** an empty tool set cannot be interpreted as permission to use undeclared tools.

### `AC-RSC-25` — Provider credentials remain scoped and fail closed

- **Given** an Organization policy selects an approved external model provider
  and either an Organization BYOK or deployment-default credential binding
- **When** a Session configuration resolves or the runtime prepares a model call
- **Then** the runtime validates the binding's Organization/deployment scope,
  provider match, version, and active status through the trusted secret boundary
- **And** the frozen configuration and manifest contain only the non-secret
  credential mode and opaque binding reference/version
- **And** Participant, Activity, request, and Session input cannot substitute a
  different credential owner
- **And** a missing, revoked, wrong-scope, or mismatched binding blocks model work
  without falling back to another credential, payer, or provider
- **And** raw credential material is absent from product storage, UI, API
  payloads, audit, logs, telemetry, and exports.

### `AC-RSC-26` — Assessment Invocation/Decision policy is frozen without enabling deferred capabilities

- **Given** an MVP text assessment Session resolves from an activated cohort
- **When** the resolved configuration and initial manifest are inspected
- **Then** they identify the supported Agent Invocation contract and the exact
  permitted P0 trigger, decision, output-kind, and requested-action subset,
  including Agent-initiated and no-action policy where applicable
- **And** behaviorally material values match the immutable cohort baseline or
  its verified referenced policy
- **And** voice/Interaction Controller triggers, Participant Session tools,
  silence-driven behavior, arbitrary or parallel timers, richer
  configurable workflow triggers, and non-P0 output or requested-action kinds
  remain explicitly disabled
- **And** a later mutable policy change cannot alter the existing Session.

### `AC-RSC-27` — Optional timer replacement policy is frozen and bounded

- **Given** an MVP text assessment profile enables the Session Agent timer lane
- **When** the cohort baseline, resolved configuration, and initial manifest are
  inspected
- **Then** one timer lane identifies its positive default delay, permitted
  relative-delay bounds, active-time basis, stages, Decision capabilities,
  cooldown, concurrency, duplicate, replacement, and Invocation budgets
- **And** lower scopes cannot add a lane, widen a bound, or switch the clock
  basis
- **And** a profile that disables the lane schedules neither a default nor an
  Agent-requested timer event.

### `AC-RSC-28` — P0 output and requested-action kinds are frozen

- **Given** an MVP text assessment Session resolves from an activated cohort
- **When** the resolved configuration and initial manifest are inspected
- **Then** they identify the Decision envelope/schema version and permit at most
  one Participant `message` output, zero `voice` outputs, no
  reviewer/administrator/runtime-only presentation outputs, and no requested
  action other than optional next-timer replacement
- **And** lower scopes cannot add an output kind, audience, or action
- **And** historical v1 reconstruction remains part of the frozen contract
  identity.

## Dependencies and rollout

### Dependencies

- The approved product semantics, this version 0.4 revision, text Session
  lifecycle v0.5, [ADR-012](../../architecture/decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md),
  [ADR-013](../../architecture/decisions/ADR-013-agent-requested-next-timer-replacement.md),
  and [ADR-014](../../architecture/decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md)
  govern Invocation/Decision, next-timer, and P0 output-profile configuration
  and manifest implementation.

- Approved authorization and resource-isolation behavior from [`auth-resource-isolation.md`](auth-resource-isolation.md).
- Versioned organization policy, agent, harness, activity, task, workflow, rubric, model, knowledge, and memory source records.
- Activity/cohort activation baseline and fairness rules from [`assessment-setup.md`](assessment-setup.md).
- Trusted enrollment, attempt, participant, and session ownership from [`submission-attempts.md`](submission-attempts.md).
- Session lifecycle start/active/terminal contracts from [`session-text-lifecycle.md`](session-text-lifecycle.md).
- Evidence and evaluation consumers that accept the trusted configuration/manifest binding.
- Append-only or equivalently tamper-evident audit/event storage with unambiguous sequence and UTC time.
- A model-provider adapter contract capable of returning the required deployment/version identity.
- [ADR-001](../../architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md) for canonical representation, digest/seal procedure, logical artifact separation, and source materialization; further architecture decisions remain required for storage layout, transaction/idempotency boundaries, runtime append sequencing, archival, and reconstruction.

### Rollout

- Resolved configuration is a mandatory session foundation, not an optional customer-facing feature flag.
- No assessment production session may start through a legacy path that lacks the frozen configuration and manifest binding.
- Implement the logical schema and resolver contract before live session execution.
- Add source families incrementally behind automated contract tests, but keep session start disabled until every required MVP source family is supported.
- Run a diagnostic readiness check on configured activities/cohorts before activation and perform authoritative validation again at session start.
- Permit shadow comparison between an old prototype resolver and the authoritative resolver only if the authoritative resolver remains the enforcing decision.
- Quarantine seeded or migrated source records that lack unambiguous ownership, version identity, or digest; do not treat them as globally valid.
- Require downstream model, session, evidence, evaluation, review, and export services to consume the trusted manifest/configuration binding before release.
- Enable reconstruction verification and terminal seal checks before relying on the system for auditable assessment outcomes.

### Observability

Track at minimum:

- Resolution requests, successes, and failures by stable category.
- Resolution latency and source-read latency.
- Cohort-baseline drift and digest mismatches.
- Missing immutable versions or provider deployment fingerprints.
- Precedence conflicts and approved override use.
- Duplicate/concurrent start deduplication.
- Atomic-start rollback or recovery events.
- Manifest append latency, retry, conflict, and failure.
- Aborted-after-freeze sessions.
- Seal creation and verification failures.
- Reconstruction success, degraded, and unverifiable status.
- Configuration/manifest read and export denials.
- Count of sessions lacking required manifest fields; the release target is zero.

Metrics and traces must use bounded labels and must not contain raw participant content, prompts, outputs, credentials, secrets, or unrestricted identifiers.

## Open questions

None. Questions `Q-1`–`Q-7` were decided on 2026-08-06 as recorded below. Product-wide retention duration remains outside this feature's scope and must be governed by the applicable approved organization, legal, privacy, deletion, and result-lifecycle policies.

## Approved decision disposition

The following table preserves the proposal and question history while linking each approved decision to its authoritative location. The cited requirements and sections govern behavior; [ADR-001](../../architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md) governs technical representation.

| Prior IDs | Approved disposition | Authoritative location |
| --- | --- | --- |
| `Q-1`, `PROP-6` | Use a versioned canonical JSON procedure and SHA-256 for configuration digests and terminal manifest seals. | [ADR-001](../../architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md) |
| `Q-2`, `PROP-8` | Block a fairness-sensitive assessment when the model deployment has neither an immutable version nor an architecture-approved equivalent fingerprint. | `REQ-RSC-32`, `AC-RSC-9` |
| `Q-3`, `PROP-1` | Maintain linked but independently identifiable logical configuration and manifest artifacts; physical co-location is permitted when their semantics and controls remain distinct. | [Logical records](#logical-records), `REQ-RSC-1`, `REQ-RSC-29`, [ADR-001](../../architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md) |
| `Q-4`, `PROP-10` | Preserve an aborted-after-freeze configuration and manifest; the attempt-owning specification decides consumption and retry entitlement. | `REQ-RSC-27`, `AC-RSC-16`, [Dependencies](#dependencies) |
| `Q-5`, `PROP-9` | Give reviewers and administrators an authorized readable summary; give participants only published rules and non-sensitive operational facts. | `REQ-RSC-38`–`REQ-RSC-40`, `AC-RSC-20`–`AC-RSC-22`, [UX and accessibility](#ux-and-accessibility) |
| `Q-6` | Apply the governing retention, deletion, legal-hold, privacy, and result-lifecycle policies; preserve honest degraded or unverifiable status when required historical material is unavailable. This feature defines no independent duration. | `REQ-RSC-44`, `REQ-RSC-45`, `AC-RSC-18`, `AC-RSC-19`, [Security and privacy](#security-and-privacy) |
| `Q-7` | Store normalized execution-effective values and immutable source references/digests; copy source content only when required for execution or reconstruction and permitted by data-minimization policy. | [Canonical effective configuration](#canonical-effective-configuration), [Runtime provenance](#runtime-provenance), [ADR-001](../../architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md) |
| `PROP-2` | Reject mutable-only references for behavior-affecting inputs. | `REQ-RSC-15`, `AC-RSC-7` |
| `PROP-3` | Treat configuration freeze, initial manifest creation, trusted session binding, and required audit persistence as one atomic start boundary. | `REQ-RSC-23`–`REQ-RSC-25`, `AC-RSC-10` |
| `PROP-4` | Prohibit lower-scope widening and ad hoc participant/request/session overrides of the assessment baseline. | `REQ-RSC-4`–`REQ-RSC-8`, `AC-RSC-2`, `AC-RSC-4`, `AC-RSC-5` |
| `PROP-5` | Preserve an append-only ordered runtime manifest and create a terminal integrity seal. | `REQ-RSC-33`–`REQ-RSC-37`, `AC-RSC-15`, `AC-RSC-17`, `AC-RSC-23` |
| `PROP-7` | Set the initial resolution and atomic-freeze service objective to at most 2 seconds at the 95th percentile under the stated preconditions. | [Performance and reliability](#performance-and-reliability) |

## Traceability

| Requirement/AC | Automated verification | Playwright/manual evidence |
| --- | --- | --- |
| `REQ-RSC-1`–`REQ-RSC-8`, `AC-RSC-1`–`AC-RSC-5` | Unit/property tests for precedence; cross-layer conflict matrix; unauthorized/authorized override tests | Administrator conflict and override states; reviewer provenance summary |
| `REQ-RSC-9`–`REQ-RSC-14`, `AC-RSC-6`, `AC-RSC-8`, `AC-RSC-24` | Baseline drift, cross-cohort substitution, memory snapshot/no-read, deferred-capability tests | Cohort readiness and start-blocked flows |
| `REQ-RSC-15`–`REQ-RSC-22`, `AC-RSC-3`, `AC-RSC-7`, `AC-RSC-11`, `AC-RSC-12` | Determinism/property tests; mutable-alias rejection; source-change and concurrency tests | Technical provenance and immutable-history inspection |
| `REQ-RSC-23`–`REQ-RSC-28`, `AC-RSC-10`, `AC-RSC-11`, `AC-RSC-13`, `AC-RSC-16` | Fault injection across persistence/audit; duplicate start; stale authorization; aborted-after-freeze tests | Participant unavailable/retry states; admin diagnostics |
| `REQ-RSC-29`–`REQ-RSC-37`, `AC-RSC-9`, `AC-RSC-14`, `AC-RSC-15`, `AC-RSC-17`, `AC-RSC-23` | `ManifestTerminalizationTests`; Postgres `0017`/`0018` seal and handoff fault injection | No reviewer manifest UI |
| `REQ-RSC-46`, `AC-RSC-25` | Domain/port no-fallback tests; frozen-authority processor tests; fake-transport adapter tests; secret traversal/symlink/size tests; destination-policy and adapter-configuration digest tests; successor exact-profile qualification | No Participant credential UI; binding status is not a browser field |
| `REQ-RSC-47`–`REQ-RSC-55`, `AC-RSC-26`–`AC-RSC-28` | Frozen-policy resolver tests; contract v1/v2 dual-read; Sessions Decision tests | Participant UI hides envelope, output-id, audience, and timer internals |
| `REQ-RSC-38`–`REQ-RSC-45`, `AC-RSC-18`–`AC-RSC-22` | Cross-scope access matrix; redaction tests; reconstruction/degraded-source tests; audit assertions | Participant/admin/reviewer access states; keyboard, focus, responsive evidence |
| Quality and observability requirements, `AC-RSC-22` | Accessibility component tests; latency/load tests; bounded-label checks | Narrow viewport, keyboard, screen-reader status, failure screenshots |
