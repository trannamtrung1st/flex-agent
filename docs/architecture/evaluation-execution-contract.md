# Evidence and Evaluation execution contract

Approved detailed execution contract for Evidence formation and internal
Evaluation in the MVP assessment workflow.

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Approved |
| **Owner** | Architecture Lead |
| **Approvers** | Product Lead, Architecture Lead, Security/Privacy reviewer |
| **Consulted perspectives** | Business analysis, architecture, UI/UX, security/privacy, documentation |
| **Version** | 0.1 |
| **Last reviewed** | 2026-09-01 |
| **Governs** | Evaluation request, Evidence locator/seal, evaluator execution, completion, replacement, and review-handoff realization |

This contract currently owns the Evidence and Evaluation realization split
extracted from ADR-009 (`EVAL-DEC-*`). ADR files remain until Phase 5.

This document does not change the
[Evidence and Evaluation specification](../requirements/features/evidence-evaluation.md).
It is the current detailed MVP technical realization for this boundary.

## Purpose and audience

This contract defines the technical boundary from an eligible completed text
Session handoff through an immutable internal Evaluation ready for human review.
It gives backend, security, testing, and UI/UX consumers explicit rules for:

- exact source and Evidence locator representation;
- Evidence-set integrity and sealing;
- deterministic, Agent-assisted, and Agent-judgment execution;
- durable work, retry, idempotency, completion, and replacement lineage;
- service authorization, model/parser/evaluator trust boundaries; and
- reconstruction and repeatable verification.

It does not choose a model, framework, parser product, programming language, or
container runtime.

## Governing sources

- [Concept model](../product/concept-model.md), especially Evidence, the outcome
  chain, assessment fairness, and the resolved execution manifest.
- [MVP scope](../product/mvp-scope.md), especially text-only assessment, stable
  memory, human review, and deferred tools/learning.
- Approved [Evidence and Evaluation requirements](../requirements/features/evidence-evaluation.md#business-rules)
  and [acceptance criteria](../requirements/features/evidence-evaluation.md#acceptance-criteria).
- [Authorization and isolation](../requirements/features/auth-resource-isolation.md),
  [resolved Session configuration](../requirements/features/resolved-session-configuration.md),
  [Submission and Attempts](../requirements/features/submission-attempts.md),
  [text Session lifecycle](../requirements/features/session-text-lifecycle.md), and
  [MVP operational defaults](../requirements/mvp-operational-defaults.md).
- [ADR-001](decisions/ADR-001-resolved-configuration-representation-and-integrity.md),
  [ADR-002](decisions/ADR-002-authorization-enforcement-and-delegation.md),
  [ADR-003](decisions/ADR-003-authorization-audit-persistence.md),
  [ADR-005](decisions/ADR-005-atomic-attempt-start-and-submission-binding.md), and
  [ADR-006](decisions/ADR-006-mvp-architecture-baseline-and-evolution.md),
  [ADR-008](decisions/ADR-008-bounded-oss-component-set.md), and
  [ADR-009](decisions/ADR-009-mvp-session-evaluation-review-contracts.md).
- [MVP architecture](mvp-architecture.md) and the approved
  [text Session runtime contract](session-runtime-contract.md).

## Scope

### In scope

- Eligibility and exact input binding for automatically evaluating a
  `Completed` Session.
- Versioned Evidence locators for exact bound direct text, `.txt` and `.md`
  material, terminal transcript items, frozen configuration/manifest facts,
  and verified deterministic facts.
- Evidence-set creation and integrity sealing.
- Frozen per-criterion evaluator modes, deterministic runner isolation,
  Agent-assisted composition, Agent-judgment validation, and aggregation.
- Durable invocation, provider/evaluator attempt, completion, failure,
  annotation, replacement, and review-eligible-version lineage.
- Assigned-review inspection state needed by downstream review without Human
  revision, Review decision, Result, or Release side effects.

### Out of scope

- Creating or changing a rubric, Evaluation procedure, model binding, Session
  transcript, Submission binding, or terminal cutoff.
- Human revision, Review decision, Result construction, Release, correction, or
  Participant outcome visibility.
- Voice or participant-session tool Evidence, participant code, unrestricted
  scripts, network-enabled evaluators, or unapproved external evaluation.
- Parser or model product selection and detailed reviewer interaction design.

## Confirmed constraints

1. Only an authoritative eligible handoff from a `Completed` Session may create
   an automatic Evaluation. `Terminated` and `Aborted` route to human operations.
2. Exact frozen references and versions are mandatory; `latest`, filenames,
   display order, provider aliases, or model output never select sources.
3. The Evaluation service authenticates as itself, loads a durable delegation,
   and reauthorizes before protected reads and the completion commit.
4. External model calls and deterministic execution occur outside the
   authoritative completion transaction.
5. Raw source content remains in its owning protected store. Evidence locators
   and operational records do not become copies or access tokens.
6. Model, parser, evaluator, Submission, and transcript content are untrusted and
   cannot change policy, evaluator mode, scope, rubric, tool, memory, or Release.
7. A completed Evaluation, its criterion judgments, Evidence set, and provenance
   are immutable. Replacement creates explicit successor lineage.
8. Evaluation completion alone has no Human revision, decision, Result, Release,
   notification, memory, calibration, or harness-change side effect.

## Approved contract decisions

All decisions in this section were approved on 2026-08-06.

| ID | Approved decision | Rationale |
| --- | --- | --- |
| `EVAL-DEC-1` | Use one versioned `evidence-locator.v1` tagged union whose source identity, location type, coordinate system, precision, integrity, and adapter/procedure version are explicit. | Prevents consumers from interpreting opaque or mutable locator strings differently. |
| `EVAL-DEC-2` | Seal the ordered Evidence set with `evidence-set-jcs-sha256-v1`: schema normalization, RFC 8785 canonical JSON, and lowercase SHA-256, while keeping authorization independent of the digest. | Provides cross-language integrity compatible with ADR-001 without copying source content. |
| `EVAL-DEC-3` | Give every request an immutable input identity (terminal handoff plus frozen input digest) and a request kind. The initial request is unique for that input; a replacement request additionally binds one predecessor and authorized reason. Each processing attempt is separate under its request. | Distinguishes equivalent retry from an authorized replacement while preserving every provider/evaluator outcome. |
| `EVAL-DEC-4` | Complete an Evaluation in one primary-store transaction that publishes the immutable artifact, criterion judgments, Evidence items/set seal, evaluator/model provenance references, lineage, manifest append, review-case handoff, and required audit/outbox acceptance. | Prevents partial or unaudited internal authority from reaching review. |
| `EVAL-DEC-5` | Run deterministic evaluators through an allowlisted restricted worker adapter with no network egress by default, no Participant-controlled executable selection, immutable evaluator/dependency identity, and positive resource bounds. | Implements `PROP-8` without creating a participant-session tool or code-execution capability. |
| `EVAL-DEC-6` | Treat deterministic output as verified protected Evidence, not policy or infallible truth. Agent-assisted criteria receive the protected fact and may interpret it, but cannot overwrite it. | Preserves provenance and explicit conflict handling. |
| `EVAL-DEC-7` | Support direct-text and text-attachment locations at whole-item, line-range, and UTF-8 byte-range precision; fall back honestly to whole item when finer verified precision is unavailable. | Matches MVP material categories while keeping coordinates stable and testable. |
| `EVAL-DEC-8` | Select the downstream review candidate explicitly. A replacement Evaluation is not review-eligible merely because it is newer. | Prevents silent candidate switching and score-shopping races. |

## Logical ownership and records

| Record | Authoritative owner | Required identity and mutation rule |
| --- | --- | --- |
| Evaluation handoff | Session execution, consumed by Evaluation | Exact Organization through Session chain, terminal record/cutoff, configuration, manifest, Submission binding, eligibility, trusted digest; immutable |
| Evaluation request | Evaluation | Handoff, frozen input digest, `initial` or `replacement` kind, predecessor/reason when replacing, idempotency scope/key, delegation, state, current authoritative outcome; state-transitioned |
| Invocation attempt | Evaluation | Request, attempt ordinal, exact inputs, model/evaluator references, work/lease, timing, outcome/failure, protected request/response references; append-only after terminal |
| Deterministic invocation | Evaluation | Criterion, evaluator/dependency identity, canonical input digest/references, limits, output digest/reference, outcome and timing; append-only |
| Evidence item | Evaluation | Exact source/version, locator, ownership, precision, integrity/verification, creator and cited criterion references; immutable after completion |
| Evidence set | Evaluation | Ordered Evidence-item identities, schema, seal procedure/digest, invocation and Evaluation identity; immutable after seal |
| Criterion judgment | Evaluation | Exact criterion/version/mode, status, configured fields, rationale, Evidence references, confidence, uncertainty, provisional feedback, validation; immutable after completion |
| Evaluation | Evaluation | Exact ownership and upstream lineage, schema/procedure, judgments, aggregate, Evidence set, model/evaluator/configuration/manifest provenance, completion order/time; immutable |
| Evaluation lineage | Evaluation | Predecessor/successor, bounded authorized reason, actor/service, creation time, eligibility disposition; append-only |
| Evaluation annotation | Evaluation | Integrity, availability, lifecycle, or verification finding; affected records, bounded reason, actor/service and time; append-only |
| Review handoff | Review and Release | Evaluation lineage, explicit candidate eligibility, integrity and policy references; created from completed Evaluation without Release side effect |

## Evidence locator contract

### Common envelope

Every `evidence-locator.v1` contains:

| Field | Contract |
| --- | --- |
| `locator_schema` | Exact supported locator schema and major version |
| `source_type` | Tagged source type from the allowlist below |
| `source_ref` | Stable protected source identity plus exact immutable version or terminal boundary |
| `ownership_ref` | Trusted Organization, Activity, Participant, Attempt, Session, and Evaluation-context references required for verification |
| `location` | Source-specific tagged location; never an arbitrary executable path or query |
| `precision` | `exact_range`, `stable_segment`, or `whole_item` |
| `integrity` | Source digest/reference, locator-adapter/procedure version, and verification state |
| `created_by` | Evaluation service and invocation reference |

Ownership fields are persisted for lineage and constraint enforcement but are
derived from trusted records. A caller cannot authorize or redirect resolution
by supplying them.

### MVP source types and locations

| Source type | Allowed location |
| --- | --- |
| `submission.direct_text` | Exact Submission version and material item; whole item, one-based inclusive line range, or half-open UTF-8 byte range |
| `submission.text_attachment` | Exact accepted `.txt`/`.md` version and item; whole item, one-based inclusive line range, or half-open UTF-8 byte range over the validated immutable bytes |
| `session.transcript_item` | Exact accepted/published transcript item at or before cutoff; an Agent item is reconstructed only from its ordered durable response fragments and complete/incomplete outcome; locator uses the whole item or a half-open UTF-8 byte range over exact displayed content |
| `session.work_trace` | Exact published work-trace item at or before cutoff; whole item or half-open UTF-8 byte range |
| `configuration.fact` | Exact resolved-configuration or activation-baseline digest plus an allowlisted JSON Pointer into a safe fairness/configuration projection |
| `manifest.fact` | Exact manifest identity/sequence/seal plus an allowlisted JSON Pointer into a safe provenance projection |
| `deterministic.fact` | Exact deterministic invocation and protected output digest plus an allowlisted JSON Pointer into the versioned evaluator output schema |

For text ranges, offsets count bytes in the exact validated UTF-8 source and use
`start_inclusive`/`end_exclusive`. The locator stores an excerpt digest, never a
copied excerpt, so resolution can reject drift or incorrect coordinates. A line
range stores the line-splitting procedure version. Markdown is cited as source
text in MVP; rendered DOM positions are not stable Evidence locations.

A locator adapter must verify source type, exact version, ownership, cutoff or
binding membership, location bounds, integrity, and current authorization.
Unsupported adapter or major versions fail closed. If a fine location cannot be
verified, the Evaluation may cite the exact whole item only when the frozen
procedure permits that precision.

### Evidence-set seal

The `evidence-set-jcs-sha256-v1` seal document contains:

- Evidence-set and Evaluation-invocation identities;
- schema and seal-procedure versions;
- trusted Organization, Activity, Participant, Attempt, and Session references;
- the terminal handoff and frozen input digest;
- each unique Evidence item identity and locator digest, sorted by the
  schema-defined stable key `(source_type, source_ref, location_digest,
  evidence_id)`; and
- source integrity/verification states used at completion.

The document is normalized, serialized as UTF-8 using RFC 8785, and hashed with
SHA-256 encoded as lowercase hexadecimal. Altered, missing, duplicated, or
reordered covered entries fail verification. The seal proves neither actor
identity nor authorization and does not override lifecycle policy.

## Evaluation and invocation state contracts

```text
Ineligible
    | eligible terminal handoff
    v
Queued -> Running -> Validating -> Completing -> Completed
             |            |             |
             +------------+-------------+-> Failed retryable
                                          -> Failed review-required
                                          -> Cancelled

Completed --authorized replacement request--> new request and Evaluation
```

- `Queued` means the authoritative request and work item committed.
- `Running` and `Validating` are invocation-attempt states; partial outputs are
  protected attempt artifacts, never criterion judgments available for review.
- `Completing` means all external work ended and the service is attempting the
  authoritative completion boundary.
- `Completed` means the immutable Evaluation, Evidence-set seal, lineage,
  manifest provenance, review handoff, and required audit are authoritative.
- Failure states preserve bounded reason and attempt history. They do not create
  a completed Evaluation.

## Versioned work and provider contract

An `evaluation.work.v1` record contains a stable work/request/attempt identity,
request kind and predecessor reference when replacing, trusted Organization and
Session references, input digest, delegation reference, procedure and
evaluator-registry versions, available-at time, attempt and lease state,
timeout/retry bounds, and bounded failure category. It contains no human
credential or unnecessary source content.

The model-adapter request is constructed only from:

- fixed system and Evaluation instructions identified by frozen version;
- exact permitted criterion/rubric fields;
- validated Evidence content resolved from the exact candidate boundary;
- verified deterministic facts for `agent_assisted` criteria;
- configured model/deployment and generation parameters;
- the non-secret provider credential binding frozen by the trusted resolved
  configuration and resolved only through the approved `SecretSource`; and
- a strict versioned output schema.

Raw credentials never enter evaluation work, prompts, invocation provenance, or
provider responses stored by the product. A missing, revoked, wrong-scope, or
provider-mismatched binding blocks the invocation without fallback to another
credential, payer, or provider, as required by `REQ-RSC-46` and `AC-RSC-25`.

Provider responses are untrusted. Independent validation covers criterion
identity/completeness, type/range, citations, confidence/uncertainty, aggregation,
protected content, Agent/deterministic conflicts, and policy before completion.

## Deterministic evaluator contract

The evaluator registry entry binds:

- exact evaluator identifier and immutable version or image/module digest;
- supported criterion operation and input/output schema versions;
- canonical input procedure and immutable dependency/configuration digest;
- positive CPU, memory, elapsed-time, input, output, and concurrency limits;
- no-egress default and any prohibited filesystem, process, or system access;
- deterministic failure and conflict behavior; and
- manifest, audit, and lifecycle classification.

Workers may invoke only a registry-bound evaluator declared by the frozen
procedure. Participant content cannot select a path, executable, argument,
dependency, network endpoint, secret, or evaluator version. The MVP runner:

- executes no Participant-provided code or unrestricted script;
- has no network egress by default;
- receives canonical bounded inputs through protected references;
- writes only bounded protected output and temporary data subject to cleanup;
- records timeout, bound exhaustion, invalid output, dependency failure, and
  integrity failure without falling back to Agent judgment; and
- keeps each invocation scoped to one Organization, Evaluation, and criterion.

## Critical consistency flows

### Request admission and claim

One primary-store transaction validates the terminal handoff, exact frozen
inputs, request kind and any predecessor/reason, authorization/delegation,
eligibility, integrity, and idempotency; then commits the Evaluation request,
input digest, durable work, and required audit/outbox state. No protected source
or model disclosure occurs before the worker revalidates the request at claim
time.

Equivalent retries return the authoritative request/Evaluation. Reusing a key
with a different trusted input digest fails without mutation. Duplicate workers
may claim through leases, but completion uniqueness is enforced independently.

### Processing and validation

1. The worker authenticates, loads delegation and trusted ownership, and
   revalidates current processing authorization.
2. It verifies terminal state/seal, exact configuration, manifest, rubric,
   procedure, evaluator registry, model binding, Submission binding, and source
   integrity without mutable aliases.
3. It invokes deterministic evaluators first where the frozen mode requires
   them and persists protected invocation provenance.
4. It forms and independently validates Evidence locators.
5. It invokes the Agent only for `agent_assisted` or `agent_judgment` criteria,
   using minimum exact content and bounded provider controls.
6. It validates every criterion and aggregate, seals the Evidence set, and
   prepares immutable completion records.

External calls never remain inside an open database transaction.

### Authoritative completion

One primary-store transaction:

1. reauthorizes the service and revalidates delegation, expected request state,
   terminal handoff, frozen input digest, source integrity, and current policy;
2. verifies every required criterion, locator, evaluator outcome, conflict rule,
   confidence/uncertainty value, and aggregate;
3. inserts immutable Evidence items, the sealed Evidence set, criterion
   judgments, Evaluation, and model/evaluator provenance references;
4. enforces uniqueness for one authoritative completion of the request identity;
5. appends manifest provenance and immutable Evaluation lineage;
6. creates or refreshes a non-releasing review handoff with no implicit
   candidate switch; and
7. accepts required durable audit/outbox state before exposing `Completed`.

Any failure exposes no partial Evaluation. A worker that loses the completion
response reconciles from the request identity and stored outcome.

### Replacement and annotation

An authorized replacement uses a new request and invocation with one exact
predecessor, bounded approved reason, current authorization, and required audit.
It never edits the predecessor or silently becomes the review candidate. The
review contract explicitly selects eligibility and handles stale in-progress
review.

Later integrity, lifecycle, or source-availability findings append annotations
and a current disposition. They do not alter the historical verification state
recorded at completion.

## Security and privacy contract

| Threat or harm | Required control | Verification |
| --- | --- | --- |
| Wrong-scope work or source access | ADR-002 service delegation, trusted ownership, reauthorization at claim/read/commit | Wrong Organization through wrong Evaluation and forged work/locator matrix |
| Mutable-source substitution | Exact version/cutoff/binding/digest checks; no aliases | Later Submission, post-cutoff transcript, model/rubric alias and filename tests |
| Prompt or evaluator injection | Fixed policy channels, evaluator allowlist, strict schemas, no Participant executable selection or egress | Malicious Submission/transcript/metadata/model output and argument/path tests |
| Citation forgery | Independent locator resolution, bounds, integrity, authorization and Evidence-set seal | Forged ID, range, adapter version, digest, source and precision tests |
| Agent override of deterministic facts | Mode resolver and independent conflict validator | Conflicting claim, aggregation, retry and fallback tests |
| Cross-review disclosure | Assignment-scoped reads and source reauthorization; locators are not capabilities | Wrong assignment, guessed ID, signed/object reference and revoked assignment tests |
| Operational leakage or secondary use | Protected references, minimized provider input, bounded telemetry, lifecycle enforcement, learning disabled | Log/queue/error/screenshot/export leakage and non-reuse tests |
| Partial or unaudited completion | Single completion transaction with required audit/outbox | Fault injection at each completion write and audit acceptance |

## Failure and recovery contract

| Failure | Required outcome |
| --- | --- |
| Incomplete/unsealed/ineligible handoff | Reject before parser, evaluator, or model disclosure |
| Parser or locator adapter cannot prove precision | Use permitted whole-item precision or explicit insufficiency; never invent a range |
| Deterministic evaluator fails or exceeds bounds | Record exact failure; follow frozen review-required/insufficient behavior; never weaken mode |
| Model timeout, invalid output, or late response | Preserve attempt; retry exact input within bounds; publish at most one authoritative completion |
| Worker crash or lost lease | Redeliver safely from durable work; reconcile by request/input identity |
| Completion transaction or audit fails | Remain recoverable/review-required; expose no completed artifact or partial judgments |
| Source later unavailable or integrity-invalid | Append annotation and honest unavailable/degraded state; do not substitute content |
| Review candidate changes during processing | Completion preserves lineage; downstream review selection remains explicit |

## Quality and observability

- Eligible request status retains the approved 2-second p95 objective; 95 percent
  of bounded Evaluations retain the 120-second completion objective outside
  declared provider-wide outages.
- Work claiming applies Organization-aware fairness, positive concurrency,
  timeout, retry, input, output, and source-size bounds.
- Operational signals include handoff eligibility, queue age, invocation state,
  source/locator verification, evaluator mode and failure, provider latency,
  Agent/deterministic conflict, completion/lineage conflict, audit/manifest
  failure, review-required state, and replacement/annotation outcome.
- Metrics, logs, traces, alerts, work records, and errors contain no raw Evidence,
  Submission, transcript, prompt, provider output, criterion rationale, reviewer
  content, credential, or unrestricted Participant identifier.

## Verification and traceability

| Contract surface | Requirements and acceptance criteria | Minimum repeatable evidence |
| --- | --- | --- |
| Eligibility and idempotency | `REQ-EVAL-1`–`REQ-EVAL-7`; `AC-EVAL-1`–`AC-EVAL-5` | Eligible/ineligible handoff, exact input, duplicate/conflict, delegation, and no-early-disclosure tests |
| Locators and Evidence seal | `REQ-EVAL-8`–`REQ-EVAL-17`; `AC-EVAL-5`–`AC-EVAL-8` | Locator conformance fixtures, source/cutoff/binding, range/digest, cross-scope, precision, and seal-tamper tests |
| Structured output | `REQ-EVAL-18`–`REQ-EVAL-28`; `AC-EVAL-9`–`AC-EVAL-14`, `AC-EVAL-20` | Criterion completeness, insufficiency, applicability, rationale, confidence, aggregation, and no-release tests |
| Invocation and lineage | `REQ-EVAL-29`–`REQ-EVAL-36`; `AC-EVAL-15`–`AC-EVAL-19`, `AC-EVAL-27` | Timeout, retry, lost response, concurrent completion, immutable replacement and annotation tests |
| Authorization, privacy, lifecycle | `REQ-EVAL-37`–`REQ-EVAL-46`; `AC-EVAL-21`–`AC-EVAL-27`, `AC-EVAL-30`–`AC-EVAL-31` | Full scope/assignment matrix, prompt injection, audit failure, lifecycle, leakage, non-reuse and reconstruction tests |
| Evaluator composition | `REQ-EVAL-47`–`REQ-EVAL-53`; `AC-EVAL-32`–`AC-EVAL-38` | Mode freeze, evaluator identity, deterministic fact, conflict, bounds, no-egress, no-code and no-fallback tests |
| Performance and UI state feed | `AC-EVAL-28`–`AC-EVAL-29` | Load/SLO evidence plus state-contract tests consumed by the later UI/UX specification |

Implementation acceptance requires cross-language locator and Evidence-set-seal
fixtures; transaction, process-kill, retry, and provider fault injection; and an
end-to-end test from terminal Session handoff through explicit downstream review
candidate selection. UI screenshots and interaction approval remain separate.

## Open questions

None. The approved feature specification resolves product and policy questions.
Framework, evaluator-library, and parser details remain implementation choices;
applicable storage and model-provider profiles and evidence gates are governed
by ADR-008. ADR-008 intentionally selects no normative model. Every deployment,
Organization-provided, or self-hosted provider profile must pass this contract
and its applicable evidence gates rather than change Evaluation semantics.

## Approval and downstream impact

Approval unblocks Evaluation implementation and requires updates or conformance
from:

- Evaluation persistence, work, evaluator registry/runner, model adapter,
  locator adapters, seal fixtures, authorization adapters, and tests;
- the Review candidate-selection and source-navigation implementation;
- Reviewer state and Evidence-navigation implementation conforming to the approved [Evidence, Evaluation, and Human Review interaction specification](../ui-ux/evidence-evaluation-human-review.md); and
- lifecycle, reconciliation, audit, and operational runbooks.

## Related documents

- [MVP architecture](mvp-architecture.md)
- [Text Session runtime contract](session-runtime-contract.md)
- [Human review, Result, and Release contract](review-result-release-contract.md)
- [Architecture decisions](decisions/README.md)
