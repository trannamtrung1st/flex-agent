# Feature: Evidence and evaluation

## Status and source

- Status: Approved
- Owner: Product Lead
- Approvers: Product Lead, Architecture Lead, UI/UX reviewer, Security/Privacy reviewer
- Approved date: 2026-08-06
- Source: [Evidence](../../product/concept-model.md#evidence), [Evaluation, review decision, result, and release](../../product/concept-model.md#evaluation-review-decision-result-and-release), [Resolved execution manifest](../../product/concept-model.md#resolved-execution-manifest), [Assessment fairness constraints](../../product/concept-model.md#assessment-fairness-constraints), [Product invariants](../../product/concept-model.md#product-invariants), [MVP validation slice](../../product/mvp-scope.md#mvp-validation-slice), [MVP executable workflow](../../product/mvp-scope.md#mvp-executable-workflow), and [Reviewer capabilities](../../product/mvp-scope.md#reviewer-capabilities-mvp)
- Catalog entry: P0 #6 — [P0 authoring order](../README.md#p0-authoring-order)
- Related requirements: Consumes authorization and isolation from [`auth-resource-isolation.md`](auth-resource-isolation.md), the frozen rubric/evaluation procedure and manifest from [`resolved-session-configuration.md`](resolved-session-configuration.md), the activated fairness baseline from [`assessment-setup.md`](assessment-setup.md), the exact accepted Submission-version binding from [`submission-attempts.md`](submission-attempts.md), and the immutable terminal transcript cutoff from [`session-text-lifecycle.md`](session-text-lifecycle.md). Supplies a protected internal Evaluation and its Evidence lineage to [`review-result-release.md`](review-result-release.md).
- Related decisions: Approved defaults `PROP-1`–`PROP-8` in this specification. [ADR-001](../../architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md) governs resolved-configuration and manifest integrity. [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md) governs human and service authorization. [ADR-003](../../architecture/decisions/ADR-003-authorization-audit-persistence.md) governs durable audit. [ADR-005](../../architecture/decisions/ADR-005-atomic-attempt-start-and-submission-binding.md) governs the exact Submission binding. Evaluation-completion atomicity, Evidence locator representation, evaluator provenance, deterministic-execution isolation, and replacement lineage still require architecture review.
- Decision approval: `PROP-1`–`PROP-8` and the deterministic/agent-assisted/agent-judgment evaluation direction were approved on 2026-08-06.

This approved specification is authoritative for observable Evidence and internal Evaluation behavior in the assessment MVP. Architecture, UI/UX, implementation, and downstream review/release specifications must preserve its stable requirements, acceptance criteria, and approved decision dispositions.

## Problem and measurable outcome

After a Session reaches its authoritative terminal handoff, an assigned reviewer needs an internal judgment that is structured by the frozen rubric and traceable to the exact material that was available for that Attempt. A score or recommendation without stable Evidence references cannot be inspected. A mutable transcript or `latest` Submission reference can change the apparent basis of a judgment. A model-generated explanation can also be misleading if it cites unavailable content, hides missing Evidence, treats participant instructions as policy, or exposes chain-of-thought instead of an inspectable rationale.

The Evaluation is an internal recommendation, not a Result. It may combine deterministic evaluators with Agent judgment according to the frozen evaluation procedure and should not ask the Agent to reproduce facts or calculations that an approved deterministic evaluator can establish. Deterministic output is still protected, versioned Evidence rather than unquestionable truth: its inputs, evaluator identity, execution outcome, and limits must remain inspectable. The Evaluation must remain distinct from an optional Human revision, the Review decision, Release, and the participant-facing Result. A reviewer must later be able to approve the original Evaluation unchanged, revise it without overwriting it, reject it, or escalate it through the owning review feature.

The measurable outcome is:

- Every Evaluation consumes one authorized terminal Session handoff, one frozen rubric/evaluation procedure, one resolved configuration and manifest, one exact Submission-version binding, and one immutable transcript cutoff.
- Every cited Evidence item identifies an exact protected source version and stable source location, or explicitly records that only whole-artifact precision is available.
- Every required rubric criterion produces a structured judgment or an explicit `Insufficient evidence` outcome; the system never fabricates a score to fill a gap.
- Every criterion declares a frozen evaluator mode: `deterministic`, `agent_assisted`, or `agent_judgment`. Objective rules use an approved deterministic evaluator when the procedure supplies one, and Agent output cannot override its verified facts or deterministic aggregation.
- Every completed Evaluation preserves criterion-level rationale, Evidence references, configured score or decision fields, confidence, uncertainty, and provisional feedback without exposing hidden chain-of-thought.
- A completed Evaluation cannot be silently edited or overwritten. Retried or authorized replacement processing preserves the original invocation, output, lineage, actor or service, reason, and time.
- Cross-organization, cross-activity, cross-participant, cross-Attempt, cross-Session, mutable-alias, forged-locator, prompt-injection, and stale-configuration paths cannot influence or disclose another Evaluation.
- Participants cannot access internal Evidence selections, rubric judgments, confidence, provisional feedback, or an unreleased outcome through this feature.
- Automated verification covers eligibility, exact-source binding, citation integrity, deterministic evaluator isolation and provenance, criterion completeness, insufficiency, aggregation, Agent/deterministic conflict, idempotency, concurrency, provider failure, authorization, isolation, audit, lifecycle, and accessible reviewer inspection.

## Actors and permissions

All protected operations follow [`auth-resource-isolation.md`](auth-resource-isolation.md). A role label, Session identifier, Evaluation identifier, queue item, model request, citation locator, or possession of source content is not authorization evidence.

| Actor | Permitted actions and scope | Explicit restrictions |
| --- | --- | --- |
| Evaluation service | Under an explicit service identity and delegated organization/Session scope, validate an eligible handoff; read only the frozen procedure and exact protected sources; invoke versioned permitted deterministic evaluators and the permitted model; create Evidence items; validate output; and commit an internal Evaluation | Cannot select a different participant, Session, Submission version, transcript range, rubric, procedure, evaluator, model, knowledge source, or memory state; cannot widen capabilities; cannot execute participant-provided code or call an external evaluator without a separately approved contract; cannot create a Human revision, Review decision, Release, Result, memory, calibration item, or harness change |
| Assigned reviewer | Within an active review assignment and current workflow state, inspect the original Evaluation, criterion judgments, confidence and uncertainty, Evidence references, exact permitted source locations, configuration summary, and integrity status | Cannot inspect unassigned records, mutate the original Evaluation, treat provisional feedback as released, access hidden chain-of-thought, or release a Result through this feature |
| Activity administrator | Within current delegated activity and sensitive-content scope, inspect bounded Evaluation status, failures, fairness/configuration provenance, and authorized operational summaries | Organization membership or a role label alone does not grant raw Evidence, transcript, Submission, rubric-internal, or Evaluation access; cannot silently trigger a replacement Evaluation or alter an output |
| Organization administrator | Within separately delegated policy, audit, or operational scope, inspect minimized status and audit records and manage applicable upper-scope policy | Cannot bypass activity, assignment, participant, Session, or sensitive-content boundaries; cannot use administrative access as general review assignment |
| Participant | No direct Evidence-selection or Evaluation access in this feature | Cannot view internal criteria, scores, judgments, confidence, uncertainty, provisional feedback, reviewer assignments, or unreleased outcomes; later participant visibility is owned by `review-result-release.md` |
| Review/release service | Under explicit delegated scope, consume an immutable completed Evaluation and its lineage for the human-review workflow | Cannot alter the Evaluation or infer release authorization from Evaluation completion |
| System operator or support actor | Inspect non-sensitive health, queue, latency, and bounded failure information when separately authorized | Cannot inspect protected content through logs, traces, metrics, queues, or support tools without an explicit content-access capability and owning-resource authorization |

## Scope

### In scope

- Eligibility validation for the P0 assessment Evaluation after a terminal Session handoff.
- Evidence-source binding to the exact accepted Submission versions, terminal transcript cutoff, resolved configuration, execution manifest, and activated baseline when relevant.
- Creation of protected Evidence items with stable source locations, version and integrity provenance, source type, and scope.
- Criterion-level Evaluation using the exact frozen rubric and evaluation procedure.
- Criterion routing to a frozen `deterministic`, `agent_assisted`, or `agent_judgment` evaluator mode, with versioned evaluator identity, exact inputs, bounded output, and execution provenance.
- Built-in allowlisted deterministic evaluation components for objective rules, including schema and citation validation, stable parsing, permitted calculations, exact comparisons, rubric aggregation, and other bounded functions defined by the frozen procedure.
- Structured scores or decisions only when defined by the frozen rubric, plus rationale, Evidence references, confidence, uncertainty, provisional feedback, and explicit insufficiency.
- Validation that every citation resolves to an authorized exact source inside the eligible Session boundary.
- Idempotent evaluation requests, bounded retry, provider failure handling, concurrent completion safety, and immutable rerun/supersession lineage.
- Internal reviewer inspection of the original Evaluation and its Evidence without permitting revision or release.
- Audit, manifest provenance, authorization, isolation, sensitive-data minimization, and lifecycle behavior for Evidence and Evaluation records.

### Out of scope

- Human revision, reviewer comments, Review decision, escalation workflow, Result construction, Release, participant result visibility, and appeal. These belong to [`review-result-release.md`](review-result-release.md).
- Creating or editing rubrics, evaluation procedures, tasks, agents, harnesses, workflows, models, knowledge sources, memory snapshots, activities, or cohort baselines.
- Live-session questioning, message publication, terminal cutoff creation, or changing a terminal Session.
- Submission intake, parsing-policy authoring, malware handling, version acceptance, or changing the exact Submission binding.
- Participant-session tool-result and voice/playback Evidence in the MVP; tools and voice are deferred capabilities, although the Evidence contract must remain extensible to them without changing historical records. Internal deterministic Evaluation components are not participant-session tools and confer no Session capability.
- Participant-provided code execution, unrestricted scripts, network-enabled evaluators, or external evaluation systems without a separately approved sandbox, authorization, egress, timeout, data-disclosure, provenance, failure, and audit contract.
- Dynamic memory, cross-participant learning, calibration-dataset creation, analytics, harness improvement, agent self-modification, or autonomous policy change.
- Hidden model chain-of-thought, raw internal reasoning traces, or claims of exact model-output reproducibility.
- A universal scoring scale, pass threshold, confidence scale, or aggregation formula independent of the frozen rubric/evaluation procedure.
- General-purpose Evidence search, cross-assignment browsing, bulk content export, or public Evaluation APIs.
- Product-wide retention periods, consent wording, legal holds, or deletion schedules beyond enforcing the approved policy that applies.

### Boundary terms

- **Evidence source** — an exact immutable or terminally bounded protected record that may support an Evaluation, such as an accepted Submission version, a transcript item within the terminal cutoff, or a resolved-configuration/manifest fact.
- **Evidence item** — a protected, scoped reference to an exact source and stable source location, with provenance and integrity state. It does not duplicate the source's ownership.
- **Evidence set** — the immutable collection of Evidence items cited by one completed Evaluation.
- **Evaluation invocation** — one bounded processing attempt using a trusted handoff, frozen procedure, exact evaluator bindings, any applicable permitted model, and exact candidate sources.
- **Evaluator mode** — the frozen per-criterion execution type: `deterministic`, `agent_assisted`, or `agent_judgment`.
- **Deterministic evaluator** — a versioned, allowlisted internal component that produces the same defined result for the same canonical inputs and configuration, subject to its recorded runtime and dependency contract.
- **Agent-assisted evaluator** — a criterion flow in which verified deterministic facts become protected Evidence inputs to an Agent judgment.
- **Evaluation** — the immutable completed internal judgment. A failed invocation is not an Evaluation.
- **Rationale** — a concise, reviewer-inspectable explanation of how cited Evidence supports a criterion judgment. It is not hidden chain-of-thought.

## User journeys and state transitions

### Evaluation processing lifecycle

```text
Ineligible
    │ authoritative eligible handoff
    ▼
Queued ── claim ──> Running ── validate + durable commit ──> Completed
  │                    │
  │                    ├── retryable failure ──> Failed (retryable)
  │                    └── terminal failure ───> Failed (review required)
  └── stale/duplicate request ──> existing authoritative outcome

Completed ── authorized new invocation ──> new Completed Evaluation
     └──────────────────────────── preserves immutable lineage ─────┘
```

`Completed` means the Evaluation, criterion judgments, Evidence set, required manifest provenance, and required audit acceptance are authoritative. A later annotation, integrity finding, lawful source unavailability, or replacement Evaluation is appended separately and never rewrites the completed artifact.

### Evaluation service processes an eligible Session

1. The service receives or claims a request under an explicit service identity, trusted organization/Session scope, and idempotency context.
2. The service revalidates that the Session handoff is eligible under the approved terminal-state policy and that the terminal record, transcript cutoff, Attempt mapping, configuration, manifest, and exact Submission binding are authoritative and integrity-valid.
3. It loads the exact frozen rubric/evaluation procedure, per-criterion evaluator modes, deterministic evaluator versions, model binding, Evidence policy, and candidate source references from the resolved Session binding; it does not resolve `current`, `latest`, or another mutable alias.
4. It reads only the exact protected source content needed and permitted for the Evaluation.
5. For a `deterministic` or `agent_assisted` criterion, it runs only the exact allowlisted evaluator version against canonical authorized inputs and records its bounded output, integrity, timing, and provenance as protected Evidence.
6. For an `agent_assisted` criterion, it supplies the verified deterministic facts and other exact permitted Evidence to the Agent. For an `agent_judgment` criterion, it supplies only the exact permitted Evidence defined by the procedure.
7. It treats participant, Submission, transcript, Agent, knowledge, deterministic-output, and model content as untrusted data rather than instructions, authorization, rubric changes, or tool approvals.
8. It produces and validates criterion judgments, stable Evidence locators, score/decision fields, confidence, uncertainty, and provisional feedback against the frozen schema. An Agent conclusion that conflicts with a verified deterministic fact or aggregation rule is rejected or surfaced as a procedure-defined conflict; it does not override the fact.
9. It rejects or marks insufficient any criterion whose required Evidence is absent, unauthorized, invalid, outside the terminal cutoff, or not represented truthfully.
10. It commits one immutable completed Evaluation and Evidence set with evaluator lineage, manifest provenance, and required audit acceptance.
11. It makes the completed internal artifact available only to authorized review consumers; it does not create or release a Result.

### Assigned reviewer inspects an Evaluation

1. The reviewer opens an assigned review item and is reauthorized for the exact organization, activity, participant, Attempt, Session, Evaluation, and current workflow state.
2. The view presents the original Evaluation status and integrity state, rubric/procedure version, model/configuration summary, and overall provisional judgment where the rubric permits one.
3. The reviewer examines each criterion's judgment, rationale, confidence, uncertainty, and Evidence references.
4. Activating an Evidence reference navigates to the exact permitted source location or explains that only whole-artifact precision is available.
5. Missing, unavailable, invalid, redacted, or lawfully deleted source content is shown honestly and does not silently disappear from history.
6. Any revision, approval, rejection, escalation, or release action transitions to the owning review feature and leaves the original Evaluation unchanged.

### Evaluation fails and recovers

1. A bounded failure records the invocation, exact frozen inputs, provider/request reference when available, attempt number, outcome, safe reason category, and timestamps.
2. No partial or schema-invalid output is presented as a completed Evaluation.
3. A retryable failure may be retried within the frozen policy using the same idempotency lineage and input digest.
4. Equivalent concurrent retries reconcile to one authoritative completion or the same failure state.
5. Exhausted, non-retryable, integrity, authorization, or configuration failures create an internal review-required state with a safe next action; they do not fabricate a score or silently select newer inputs.

### Source integrity changes after completion

1. A later verifier detects that a protected source is unavailable, fails integrity verification, is under lawful restriction, or no longer may be disclosed.
2. The original Evidence item and Evaluation remain immutable.
3. The system appends an integrity or availability annotation with actor/service, reason category, time, affected references, and audit correlation.
4. Review consumers see the current limitation and cannot treat the affected citation as verified Evidence.
5. Any replacement Evaluation follows the authorized rerun policy and creates new lineage; it does not repair the old artifact in place.

### Prohibited transitions and actions

- `Ineligible` directly to `Running` or `Completed` without an authoritative handoff and current service authorization.
- A mutable Submission alias, live transcript tail, client-selected range, model-supplied identifier, or cross-scope locator to trusted Evidence.
- Failed, cancelled, timed-out, partial, or schema-invalid invocation output to `Completed`.
- A criterion with missing required Evidence to a fabricated score or unqualified confident judgment.
- A completed Evaluation to edited criterion content, replaced Evidence, changed rubric, changed model/configuration provenance, or erased uncertainty.
- Evaluation completion to Human revision, Review decision, Release, or participant-visible Result without the separate authorized review workflow.
- Participant, Submission, transcript, Agent, knowledge, or model content to policy authority, capability widening, tool execution, memory write, or cross-participant learning.
- An Agent judgment to replacement of a verified deterministic fact, deterministic aggregation, or evaluator outcome without a separately recorded validation conflict and approved resolution path.
- An internal deterministic evaluator to participant-session capability, unrestricted code execution, network egress, mutable dependency resolution, or cross-scope source access.
- An assigned review read to general Evidence search, another participant's content, or another Session's Evaluation.

## Business rules

### Eligibility and trusted inputs

- `REQ-EVAL-1` — Evaluation processing must begin only from the immutable handoff defined by `REQ-SESS-40`, with one trusted organization, activity/cohort baseline, enrollment, participant, Attempt, Session, terminal record, transcript cutoff, resolved configuration, execution manifest, and exact Submission-version binding.
- `REQ-EVAL-2` — The Evaluation service must authenticate and authorize its service identity and delegated resource scope before reading protected inputs and again at the protected commit boundary; delayed work, retries, events, queue fields, or model output must not redirect processing to another scope.
- `REQ-EVAL-3` — Every Evaluation invocation must use the exact rubric, evaluation procedure, per-criterion evaluator bindings, Evidence policy, knowledge/memory state, and approved exception references frozen in the Session configuration and manifest, plus the exact model deployment and generation parameters for any mode that invokes the Agent.
- `REQ-EVAL-4` — An invocation must not resolve a newer or mutable `current`, `latest`, display-name, filename, model alias, rubric alias, transcript tail, Submission alias, knowledge source, or memory state.
- `REQ-EVAL-5` — Evaluation eligibility must follow the approved terminal-state policy. An ineligible, unsealed, integrity-invalid, cross-scope, or incomplete handoff must fail closed without a model call or completed Evaluation.
- `REQ-EVAL-6` — The service must verify exact source ownership, terminal-boundary inclusion, permitted status, version identity, integrity state, and current processing authorization before protected content is disclosed to a parser, model, reviewer, export, cache, or downstream consumer.
- `REQ-EVAL-7` — The Evaluation request must have a trusted idempotency identity scoped to the Session handoff and frozen input digest. Equivalent retries must reconcile to the same authoritative outcome; conflicting reuse must fail without altering prior state.

### Evidence formation and integrity

- `REQ-EVAL-8` — Every Evidence item must belong to exactly one organization and one Evaluation context through an unambiguous activity, participant, Attempt, Session, and source-ownership chain.
- `REQ-EVAL-9` — An Evidence item must identify an exact source type, stable source reference and version, integrity reference or digest when available, locator schema/version, source-native location, creator service or actor, UTC creation time, and current verification state.
- `REQ-EVAL-10` — A transcript Evidence locator must resolve only to accepted participant messages, published Agent messages, published work-trace updates, or participant-visible system notices at or before the authoritative terminal cutoff; failed generations, local drafts, unpublished outputs, hidden prompts, and post-cutoff content are not transcript Evidence.
- `REQ-EVAL-11` — A Submission Evidence locator must resolve only to an exact accepted version and material item in the Session's immutable Submission binding. Failed, quarantined, rejected, later unbound, or mutable-alias material is not Evaluation Evidence.
- `REQ-EVAL-12` — A configuration or manifest Evidence locator may identify a frozen fact when that fact is material to the judgment or fairness review, but it must not disclose secrets, credentials, raw hidden prompts, unrestricted knowledge content, or unrelated protected fields.
- `REQ-EVAL-13` — A locator must identify the narrowest trustworthy source location available under the approved locator contract. When fine-grained location cannot be verified, the item must cite the whole exact artifact and expose its lower precision rather than inventing a range or quote.
- `REQ-EVAL-14` — Evidence references and permitted previews must point to content in the owning protected source; audit, manifest, queue, metrics, and Evaluation summary records must not copy unnecessary raw Submission or transcript content.
- `REQ-EVAL-15` — Every cited Evidence reference must resolve, authorize, verify, and fall within the invocation's exact candidate-source boundary before an Evaluation can complete. A model-generated citation is untrusted until independently validated.
- `REQ-EVAL-16` — The Evidence set for a completed Evaluation must be immutable and content-addressed or equivalently integrity-verifiable. Later annotations, verification findings, or lawful unavailability must be appended separately.
- `REQ-EVAL-17` — Evidence from one participant, Attempt, Session, activity, or organization must not be used to evaluate another unless a future approved specification explicitly permits and scopes that use; the assessment MVP permits no such cross-participant use.

### Structured Evaluation output

- `REQ-EVAL-18` — A completed Evaluation must identify its organization, activity/cohort, participant, Attempt, Session, Evaluation invocation, frozen rubric and procedure versions, configuration and manifest references, Evidence-set reference, applicable deterministic evaluator and model/runtime provenance, schema version, creation service, and authoritative UTC completion time/order.
- `REQ-EVAL-19` — Every rubric criterion required by the frozen procedure must have exactly one criterion judgment keyed to the exact criterion identifier and version; the Evaluation must not add, omit, merge, or reinterpret criteria outside the frozen procedure.
- `REQ-EVAL-20` — Each criterion judgment must contain the procedure-defined status, score or decision fields when applicable, concise rationale, zero or more validated Evidence references, confidence representation, uncertainty or limitation information, and provisional feedback permitted by the schema.
- `REQ-EVAL-21` — When required Evidence is absent, contradictory, inaccessible, integrity-invalid, or insufficient under the frozen procedure, the criterion must record `Insufficient evidence` or the procedure's equivalent explicit non-judgment state and must not fabricate a score, pass/fail decision, citation, or confidence.
- `REQ-EVAL-22` — `Not applicable` may be used only when the frozen rubric/procedure explicitly permits it and defines its effect. It must not be used as a substitute for missing Evidence or processing failure.
- `REQ-EVAL-23` — Overall scores, decisions, classifications, or recommendations may be produced only by the frozen aggregation rule. The system must not invent a universal scale, change weights, discard required criteria, or compute an overall outcome when the rule's completeness preconditions are not met.
- `REQ-EVAL-24` — Rationale must be a concise evidence-backed explanation suitable for reviewer inspection and must distinguish observed source facts from inference. It must not store, request, expose, or claim to reproduce hidden chain-of-thought.
- `REQ-EVAL-25` — Confidence and uncertainty must use the frozen procedure's bounded representation and must be visible per criterion. The system must support uncertainty and must not convert missing Evidence, provider reliability, or invalid citations into false precision.
- `REQ-EVAL-26` — Provisional feedback must remain internal and explicitly labeled as provisional until the review/release workflow authorizes participant visibility. It must not reveal hidden prompts, rubric internals prohibited by policy, expected-answer keys, secrets, security controls, or unrelated participant data.
- `REQ-EVAL-27` — Model output is untrusted input. A completed Evaluation must pass schema, type, criterion-set, range, aggregation, citation, protected-content, and policy validation independently of the model response.
- `REQ-EVAL-28` — Evaluation completion alone must not create a Human revision, Review decision, Result, Release, participant notification, memory candidate, approved memory, calibration example, or harness change proposal.

### Idempotency, lineage, failure, and recovery

- `REQ-EVAL-29` — Each invocation must preserve its state, attempt number, input digest, configuration/procedure/evaluator and applicable model references, runner or provider request reference when available, start/end times, outcome, bounded failure category, and correlation lineage without logging unnecessary protected content.
- `REQ-EVAL-30` — A failed, cancelled, timed-out, partial, or schema-invalid invocation must not create a completed Evaluation or make partial criterion judgments available as authoritative review content.
- `REQ-EVAL-31` — Retry policy must be frozen or constrained by the Evaluation procedure and must define positive attempt, timeout, and backoff bounds. Retries must use the same exact inputs and must preserve each invocation attempt and provider outcome.
- `REQ-EVAL-32` — Concurrent or duplicate workers must not create competing authoritative completions for the same invocation identity. Equivalent completion attempts must reconcile idempotently; conflicting output must fail and surface an integrity event.
- `REQ-EVAL-33` — A completed Evaluation must be immutable. An authorized later evaluation must create a new invocation and completed artifact linked to the prior Evaluation with a bounded reason, actor/service, time, and disposition; no earlier output, Evidence set, or provenance may be overwritten or hidden.
- `REQ-EVAL-34` — An automated retry after a completed Evaluation must return the existing authoritative outcome. A replacement Evaluation requires a separately authorized and audited reason permitted by the approved rerun policy.
- `REQ-EVAL-35` — Provider, parser, storage, authorization, integrity, manifest, or audit dependency failure must fail closed for completion, preserve an honest recoverable or review-required state, and never fall back to a newer rubric, source, model, or weaker validation path.
- `REQ-EVAL-36` — A later integrity or availability finding must append an annotation and current disposition. It must not rewrite the historical fact that a prior Evaluation completed under its then-recorded verification state.

### Authorization, audit, privacy, and lifecycle

- `REQ-EVAL-37` — Every request, list, count, queue claim, model disclosure, Evidence read, locator resolution, Evaluation read, annotation, retry, rerun, export, event, cache, index, job, and projection must enforce server-side organization, action, complete resource-chain, assignment or delegation, sensitive-content, and workflow-state authorization.
- `REQ-EVAL-38` — An assigned reviewer may access only the Evaluation and exact Evidence sources covered by the active review assignment and current content permissions. Citation navigation must reauthorize the target source and must not make the Evaluation record an access token.
- `REQ-EVAL-39` — A participant must not access internal Evidence selections, rubric judgments, scores, decisions, confidence, uncertainty, provisional feedback, model provenance, reviewer-only configuration, or Evaluation status through this feature; participant outcome visibility is release-gated by the owning review feature.
- `REQ-EVAL-40` — Participant, Submission, transcript, Agent, knowledge, and model content must be treated as untrusted data and must not change policy, authorization, rubric meaning, system instructions, capability access, tool approval, memory state, or Evidence-validation rules.
- `REQ-EVAL-41` — Protected content disclosed to a model or parser must be limited to the exact authorized sources and fields needed by the frozen procedure. The manifest must preserve provider/model and protected input/output references needed for reconstruction without copying raw content into operational logs or audit events.
- `REQ-EVAL-42` — Evaluation request/claim, source-access decision, Evidence-set seal, invocation completion/failure, completed-artifact publication, retry exhaustion, replacement authorization, integrity annotation, sensitive Evidence access/export, and unauthorized or cross-scope attempt must produce audit or operational events according to the approved durability class.
- `REQ-EVAL-43` — Publishing a completed Evaluation for review and creating or replacing its authoritative lineage must be mutation-coupled `required_durable` operations under ADR-003. The protected transition must fail if its required audit event or approved immutable outbox cannot be durably accepted.
- `REQ-EVAL-44` — Audit, logs, metrics, traces, queue payloads, notifications, and errors must use stable protected references and bounded categories; they must not contain credentials, raw prompts, raw Submission or transcript content, full model outputs, hidden reasoning, unnecessary participant data, or unrestricted Evidence excerpts.
- `REQ-EVAL-45` — Evidence, invocation, Evaluation, annotation, audit, provider-reference, and protected-payload records must follow applicable approved retention, deletion, legal-hold, consent, export, and evidence-preservation policy. This feature defines no independent duration and must report lawful unavailability honestly.
- `REQ-EVAL-46` — In the assessment MVP, Evaluation inputs and outputs must not be reused for Dynamic memory, cross-participant learning, calibration, analytics training, unrelated activities, harness improvement, or agent self-modification. Participant or model content cannot enable that reuse.

### Deterministic and Agent evaluator composition

- `REQ-EVAL-47` — The frozen evaluation procedure must assign every criterion exactly one evaluator mode: `deterministic`, `agent_assisted`, or `agent_judgment`. A runtime worker, model response, participant artifact, or reviewer interface must not change that mode for an existing Session.
- `REQ-EVAL-48` — A deterministic evaluator must be explicitly allowlisted and bound by exact evaluator identifier, immutable version or verified digest, input schema, canonicalization procedure, configuration, output schema, resource limits, dependency contract, and failure behavior before cohort activation and Session resolution.
- `REQ-EVAL-49` — Objective calculation, exact comparison, schema validation, citation validation, or rubric aggregation must use the frozen deterministic evaluator when the approved procedure defines one. An Agent must not recompute, replace, widen, or override a verified deterministic fact or aggregation result.
- `REQ-EVAL-50` — An `agent_assisted` criterion must provide the Agent with stable protected references to verified deterministic facts and other exact permitted Evidence. The Agent may interpret those facts under the rubric but must not present a conflicting claim as if the deterministic output did not exist.
- `REQ-EVAL-51` — A deterministic evaluator invocation and output must preserve the exact organization/Evaluation scope, evaluator version, canonical input digest and protected references, configuration digest, start/end times, outcome, bounded error category, output digest or protected reference, and manifest/audit correlation needed for reconstruction.
- `REQ-EVAL-52` — Failure, timeout, resource-limit exhaustion, schema-invalid output, integrity mismatch, unavailable dependency, or Agent/deterministic conflict must follow the frozen procedure's explicit failure behavior. The system must not silently fall back to Agent judgment, a newer evaluator, weaker validation, or an invented value.
- `REQ-EVAL-53` — Internal deterministic evaluators do not grant participant-session tool capability. In the MVP they must not execute participant-provided code, run unrestricted scripts, use unapproved network egress, or call an external evaluation system. Any such capability requires a later approved sandbox or integration contract with authorization, isolation, input validation, resource limits, egress policy, sensitive-data disclosure, provenance, retry, failure, and audit requirements.

## Data, evidence, and audit

### Logical records

Architecture may choose physical storage only if it preserves logical ownership, authorization, exact-source linkage, integrity, immutability, idempotency, lineage, audit, and lifecycle semantics.

| Record | Purpose | Minimum content |
| --- | --- | --- |
| Evaluation handoff | Bind eligibility to the terminal Session boundary | Handoff ID; organization/activity/cohort/enrollment/participant/Attempt/Session; terminal record and transcript cutoff; configuration/manifest/Submission-binding references; eligibility state; trusted digest; UTC order/time |
| Evaluation request | Deduplicate and authorize work | Request/idempotency ID; trusted handoff/input digest; service delegation; requested action/reason; status; correlation; timestamps |
| Evaluation invocation | Preserve one processing attempt | Invocation ID/attempt; exact handoff, procedure, evaluator-mode, deterministic evaluator, applicable model, configuration and source references; input digest; runner/provider reference; state/outcome; failure category; start/end times; lineage |
| Deterministic evaluator invocation | Preserve one bounded objective computation | Invocation ID; Evaluation/criterion scope; evaluator identifier/version/digest; canonical input digest and protected references; configuration/dependency digest; limits; outcome/error; protected output reference/digest; UTC times; manifest/audit correlation |
| Evidence item | Link a criterion to an exact protected source location | Evidence ID; ownership chain; source type/reference/version; locator schema and location; integrity reference/state; precision; creator; UTC time; annotation links |
| Evidence set | Freeze the Evidence used by one completed Evaluation | Evidence-set ID; Evaluation/invocation; ordered item references; schema; digest/integrity state; seal service/time; annotations |
| Criterion judgment | Preserve one structured rubric judgment | Criterion ID/version; status; configured score/decision fields; rationale; Evidence references; confidence; uncertainty/limitations; provisional feedback; validation outcome |
| Evaluation | Preserve the immutable internal judgment | Evaluation ID/version; ownership chain; invocation; rubric/procedure; evaluator and applicable model provenance; configuration/manifest; Evidence set; criterion judgments; permitted aggregate; schema; service; completion time/order; lineage/disposition references |
| Evaluation annotation | Add integrity, availability, or lifecycle context without mutation | Annotation ID/type; affected Evaluation/Evidence/source; actor/service; bounded reason; protected note reference when permitted; UTC time/order; audit correlation |
| Audit event | Preserve security and governance history | Event/schema ID; actor/service; organization; action; protected resource references; outcome; bounded reason; UTC time/order; correlation; assignment/delegation; durability class |

### Evidence source and locator contract

The P0 candidate-source boundary begins with only sources already bound to the Session:

- Exact accepted Submission versions and material items in the immutable Submission binding.
- Accepted participant messages, published Agent messages, published work-trace updates, and participant-visible system notices through the terminal transcript cutoff.
- Frozen resolved-configuration, activation-baseline, and execution-manifest facts when material to a criterion or fairness explanation.

A frozen deterministic evaluator may derive a protected fact from those exact sources. That fact becomes an Evidence item only with the evaluator, canonical-input, output-integrity, criterion, and manifest provenance required by `REQ-EVAL-48`–`REQ-EVAL-52`; it does not expand the candidate-source boundary.

Each Evidence locator records:

- Source type and exact protected source/version reference.
- Organization, activity, participant, Attempt, and Session ownership chain.
- Locator schema and version.
- A stable source-native location such as a message identifier plus permitted character span, a direct-text item range, an attachment page/region or parser-defined segment, or a specific configuration/manifest field reference.
- Integrity reference or digest when the owning source provides one.
- Precision (`exact range`, `stable segment`, or `whole artifact`) and current verification state.
- The criterion judgments that cite it.

A locator is not a copy of Evidence content and does not grant access. The owning protected source remains authoritative. A reviewer preview is produced only after current authorization and must preserve source identity, location, and any redaction or unavailability state.

### Evaluation output contract

The frozen rubric/evaluation procedure owns criterion identifiers, permitted judgment states, score or decision fields, value ranges, aggregation, Evidence sufficiency rules, confidence representation, uncertainty categories, and feedback constraints. The platform-level Evaluation envelope guarantees:

- Exact criterion/version identity and completeness against the frozen procedure.
- Exact per-criterion evaluator mode and deterministic/Agent provenance.
- Explicit `Insufficient evidence` rather than invented values.
- Validated Evidence references and source precision.
- Concise rationale that separates observation from inference.
- Per-criterion confidence and uncertainty.
- Provisional labeling and no release side effect.
- Immutable original output and visible replacement lineage.

### Required audit and manifest events

At minimum, record or correlate:

- Evaluation request accepted, deduplicated, rejected, or found ineligible.
- Service delegation and exact handoff/input digest used.
- Protected source access permitted or denied when policy requires it.
- Invocation started, retried, timed out, failed, cancelled, or completed.
- Deterministic evaluator invoked, completed, failed, exceeded a bound, or conflicted with an Agent judgment.
- Model/provider operation appended to the execution manifest with protected references and bounded outcome.
- Citation or source-integrity validation failed.
- Evidence set sealed and completed Evaluation published internally.
- Retry policy exhausted and human review required.
- Replacement Evaluation requested, authorized, rejected, completed, or linked as successor.
- Evidence or Evaluation integrity/availability annotation appended.
- Assigned reviewer accessed sensitive Evidence, downloaded a protected artifact, or exported an authorized record when required by policy.
- Cross-organization, cross-participant, cross-Session, wrong-assignment, forged-locator, prompt-injection, or identifier-enumeration attempt denied when security-relevant.

Events use UTC plus authoritative sequence or equivalent ordering. They contain protected references and bounded metadata rather than copied content. Evaluation completion must be reconstructable across the terminal handoff, invocation, manifest operation, sealed Evidence set, criterion judgments, completed artifact, and audit event.

## Quality requirements

### UX and accessibility

- The assigned-review experience must distinguish queued, running, completed, failed/retryable, review-required, superseded, integrity-warning, unavailable-source, and permission-denied states in text and structure rather than color alone.
- The Evaluation summary must identify that it is internal and provisional, show the exact rubric/procedure version and integrity status, and keep the primary reviewer task clear without implying that a Result has been approved or released.
- Criterion navigation must expose criterion name, evaluator mode, judgment status, configured score/decision, confidence, uncertainty, rationale, Evidence count, and missing-Evidence or evaluator-failure state in a consistent reading order.
- Deterministic facts and Agent interpretations must be distinguishable without implying that an evaluator is infallible. A conflict, failed evaluator, stale version, or bounded limitation must be labeled in text with the safe next action.
- Activating an Evidence reference must move focus to the exact permitted source location or a clear whole-artifact/unavailable explanation, identify source type and version, and provide a reliable way back to the originating criterion.
- Loading, empty, failure, stale-assignment, permission-denied, citation-invalid, retry-pending, and source-unavailable states must explain the safe next action without exposing protected identifiers or content.
- Scores, confidence, uncertainty, insufficiency, warnings, and supersession must not rely on color, icons, hover, animation, or position alone.
- Criterion and Evidence views must be fully keyboard operable, use programmatic headings and landmarks, preserve logical focus, expose status announcements, and avoid inaccessible custom tables or split panes.
- At narrow widths and 400 percent zoom, the reviewer must be able to inspect one criterion and its Evidence sequentially without hidden actions, two-dimensional scrolling for ordinary text, or loss of source/version context.
- Exact source content, provisional feedback, hidden rubric detail, and model/configuration provenance must be progressively disclosed according to current authorization and reviewer need.
- WCAG 2.2 AA is the proposed baseline pending an approved UI/UX specification. Detailed review and release interactions remain a downstream UI/UX traceability gap.

### Performance and reliability

- Queue claim, status reconciliation, and reviewer status reads must be idempotent and use bounded organization/Session-scoped queries.
- Each model or deterministic evaluator invocation must have positive configured time, input, output, retry, concurrency, memory, and compute bounds applicable to its mode and accepted by upper-scope policy. Exhaustion must fail safely and remain visible for review.
- A provider timeout, process restart, lost response, duplicate delivery, or concurrent worker must not duplicate an authoritative Evaluation, lose a completed artifact, or select newer inputs.
- Partial output may be retained only as a protected invocation artifact when policy permits; it must never appear as a completed Evaluation or criterion judgment.
- Evidence locators must be verified before completion and reauthorized when opened. A failed later verification must surface an annotation rather than silently removing the citation.
- Evaluation completion must commit the immutable artifact, criterion judgments, deterministic and Agent evaluator provenance, Evidence-set seal, lineage, required manifest provenance, and required durable audit atomically or through an architecture-approved equivalent consistency boundary that cannot expose partial completion.
- The service must apply bounded backpressure per organization and must not allow one activity, participant, oversized artifact, or provider failure to starve other authorized work.
- Under `PROP-6`, an eligible request must receive an authoritative queued/running/existing status within 2 seconds at the 95th percentile, and 95 percent of Evaluations whose inputs are within frozen platform limits must complete within 120 seconds, excluding declared provider-wide outages. Load and failure-injection tests must verify both objectives before production rollout.

### Security and privacy

- The system must derive all ownership, assignment, source, criterion, and configuration relationships from trusted records and reject client-, queue-, event-, model-, locator-, or filename-supplied values that conflict with them.
- Model, parser, and deterministic evaluator calls are trust boundaries. They receive only the minimum exact protected content required by the frozen procedure, under explicit service authorization and approved provider or evaluator policy.
- Prompt injection in Submissions, transcript messages, Agent content, filenames, document metadata, knowledge content, or model output must not alter policy, rubric meaning, system instructions, source scope, citation validation, tool access, memory, or workflow state.
- Deterministic evaluator allowlists must prevent participant-controlled evaluator selection, executable-path or argument injection, mutable dependency substitution, unapproved network access, unsafe temporary-file sharing, and resource-exhaustion escape from the Evaluation scope.
- Evidence locators, previews, caches, search indexes, queues, object stores, model batches, analytics, logs, traces, exports, and backups must preserve organization, participant, Attempt, Session, Evaluation, and assignment isolation.
- A digest, citation, provider request identifier, signed URL, object key, or Evaluation identifier proves neither authorization nor permission to disclose the source.
- Hidden prompts, expected-answer keys, raw chain-of-thought, credentials, tokens, private endpoints, unrelated knowledge content, and unnecessary participant data must not appear in reviewer-facing rationale, provisional feedback, audit, logs, metrics, traces, errors, screenshots, or notifications.
- Model output, criterion rationale, Evidence previews, and uploaded content must be rendered as untrusted content without script execution, unsafe external retrieval, state-changing links, or hidden capability escalation.
- Negative tests must cover wrong organization, wrong activity/cohort, wrong participant, wrong Attempt/Session, wrong review assignment, guessed Evaluation/Evidence ID, forged parent or locator, mutable alias, post-cutoff transcript range, unbound Submission version, revoked delegation, prompt injection, schema abuse, oversized input, provider retry, audit failure, race, and unauthorized export.

## Acceptance criteria

### `AC-EVAL-1` — Eligible terminal handoff starts Evaluation safely

- **Given** a Session has an approved eligible terminal state, immutable terminal record and transcript cutoff, sealed manifest state, resolved configuration, exact Submission binding, and current service delegation
- **When** Evaluation processing is requested
- **Then** one request is bound to the exact trusted handoff and frozen input digest
- **And** no mutable source is resolved.

### `AC-EVAL-2` — Ineligible or incomplete handoff fails closed

- **Given** a Session is non-terminal, ineligible under the terminal-state policy, missing its cutoff or manifest seal, or has an inconsistent Attempt or Submission binding
- **When** Evaluation processing is requested
- **Then** no protected content is disclosed to the model
- **And** no completed Evaluation is created
- **And** a bounded safe failure state and audit/operational event are recorded.

### `AC-EVAL-3` — Duplicate requests are idempotent

- **Given** equivalent requests use the same trusted Session handoff and frozen input digest
- **When** they arrive sequentially or concurrently
- **Then** they reconcile to one authoritative invocation/completion lineage
- **And** no duplicate Evaluation or model disclosure occurs beyond the approved retry policy.

### `AC-EVAL-4` — Conflicting idempotency reuse changes nothing

- **Given** an Evaluation request key already identifies one trusted input digest
- **When** the key is reused with different scope or frozen inputs
- **Then** the request fails as a conflict
- **And** the original request, Evidence, invocation, and Evaluation remain unchanged.

### `AC-EVAL-5` — Exact Submission and transcript sources are enforced

- **Given** the Session binds exact accepted Submission versions and a terminal transcript cutoff
- **When** the service assembles candidate Evidence
- **Then** only bound Submission items and eligible transcript items at or before the cutoff can be cited
- **And** later Submission versions, mutable aliases, unpublished output, and post-cutoff content are excluded.

### `AC-EVAL-6` — Evidence locator identifies a stable source location

- **Given** a permitted source supports a trustworthy fine-grained location
- **When** it becomes Evidence
- **Then** the Evidence item records the exact source/version, locator schema, stable location, integrity state, and ownership chain
- **And** an authorized reviewer can navigate to the same location.

### `AC-EVAL-7` — Whole-artifact Evidence is honest about precision

- **Given** an exact source is permitted but a finer stable location cannot be verified
- **When** the source is cited
- **Then** the Evidence item identifies the whole exact artifact and lower precision
- **And** it does not fabricate a quote, page, range, or segment.

### `AC-EVAL-8` — Forged or invalid citation blocks completion

- **Given** model output cites an absent, cross-scope, post-cutoff, unbound, unauthorized, or integrity-invalid source or range
- **When** independent citation validation runs
- **Then** the citation is rejected
- **And** the affected criterion becomes insufficient or the invocation fails according to the frozen procedure
- **And** no invalid citation appears in a completed Evaluation.

### `AC-EVAL-9` — Every required criterion has one structured judgment

- **Given** an eligible invocation uses a frozen rubric with required criteria
- **When** the Evaluation completes
- **Then** every exact criterion/version has one and only one judgment
- **And** each judgment satisfies the frozen schema for status, score/decision, rationale, Evidence, confidence, uncertainty, and provisional feedback.

### `AC-EVAL-10` — Missing Evidence produces explicit insufficiency

- **Given** required Evidence for a criterion is absent, contradictory, inaccessible, invalid, or insufficient
- **When** the criterion is evaluated
- **Then** it records `Insufficient evidence` or the frozen procedure's equivalent
- **And** no fabricated score, decision, citation, or false confidence is emitted.

### `AC-EVAL-11` — Not-applicable is procedure-controlled

- **Given** a criterion is claimed to be not applicable
- **When** the frozen procedure does not permit that state or its preconditions are unmet
- **Then** completion fails validation or the criterion is treated as insufficient
- **And** required criteria are not silently removed from aggregation.

### `AC-EVAL-12` — Aggregation follows only the frozen rule

- **Given** criterion judgments are available
- **When** an overall score, decision, or recommendation is derived
- **Then** the frozen weights, ranges, completeness rules, and formula are applied exactly
- **And** no overall value is produced when its required preconditions are unmet.

### `AC-EVAL-13` — Rationale is inspectable without chain-of-thought

- **Given** a completed criterion judgment
- **When** an assigned reviewer inspects its rationale
- **Then** the rationale concisely connects observed Evidence to the judgment and distinguishes inference
- **And** raw chain-of-thought, hidden prompts, expected-answer keys, secrets, and unrelated participant data are absent.

### `AC-EVAL-14` — Confidence and uncertainty remain visible

- **Given** a criterion has a valid judgment
- **When** the Evaluation completes
- **Then** the configured confidence representation and applicable uncertainty/limitation information are preserved per criterion
- **And** missing Evidence is not converted into numeric or categorical certainty.

### `AC-EVAL-15` — Later source or configuration changes do not alter history

- **Given** a completed Evaluation cites exact sources and frozen configuration
- **When** a Submission, transcript projection, rubric, model alias, knowledge source, policy, or activity draft later changes
- **Then** the completed Evaluation, Evidence set, input digest, and historical interpretation remain unchanged.

### `AC-EVAL-16` — Retry preserves exact inputs and attempt history

- **Given** a retryable provider or processing failure
- **When** the invocation retries within its approved bounds
- **Then** it uses the same exact handoff, sources, rubric/procedure, configuration, and input digest
- **And** every attempt and outcome remains inspectable.

### `AC-EVAL-17` — Partial or invalid output is not completed

- **Given** a provider times out or returns partial, malformed, out-of-range, criterion-incomplete, policy-violating, or schema-invalid output
- **When** validation runs
- **Then** no completed Evaluation is published
- **And** authorized users receive an honest retryable or review-required state.

### `AC-EVAL-18` — Concurrent completion has one authority

- **Given** two workers attempt to complete the same invocation
- **When** their commits race
- **Then** only one authoritative immutable completion succeeds
- **And** an equivalent loser reconciles to it while conflicting output raises an integrity failure.

### `AC-EVAL-19` — Replacement Evaluation preserves lineage

- **Given** a completed Evaluation exists
- **When** a currently authorized actor or service requests a replacement for an approved reason
- **Then** a new invocation and Evaluation are created with predecessor, reason, actor/service, time, and disposition links
- **And** the original Evaluation and Evidence remain inspectable and unchanged.

### `AC-EVAL-20` — Completed Evaluation does not release a Result

- **Given** an Evaluation completes successfully
- **When** its internal artifact becomes available for review
- **Then** no Human revision, Review decision, Result, Release, participant notification, or participant visibility is created by this feature.

### `AC-EVAL-21` — Wrong-scope processing and reads are denied

- **Given** a human or service actor has the wrong organization, activity, participant, Attempt, Session, assignment, delegation, action, or workflow state
- **When** the actor requests processing, Evidence, Evaluation, locator preview, retry, rerun, list, count, or export
- **Then** access is denied without protected disclosure or mutation
- **And** security-relevant attempts are audited with bounded metadata.

### `AC-EVAL-22` — Citation navigation reauthorizes the source

- **Given** an assigned reviewer can read an Evaluation
- **When** the reviewer opens an Evidence reference
- **Then** the owning source and exact location are independently authorized and verified
- **And** Evaluation access alone does not grant source access.

### `AC-EVAL-23` — Participant cannot inspect internal Evaluation data

- **Given** an authenticated participant owns the Session
- **When** the participant requests the Evaluation, Evidence selections, criteria, scores, rationale, confidence, provisional feedback, or status
- **Then** the request is denied or the resource is omitted according to the non-disclosure contract
- **And** no unreleased outcome or internal identifier is exposed.

### `AC-EVAL-24` — Prompt injection cannot change authority

- **Given** a Submission, transcript, filename, metadata field, Agent message, knowledge item, or model output contains instructions to change the rubric, disclose another source, execute a tool, write memory, or bypass citation checks
- **When** Evaluation processing runs
- **Then** the content is treated only as untrusted data
- **And** the frozen policy, source scope, capabilities, memory state, and validation rules remain unchanged.

### `AC-EVAL-25` — Required audit gates completion and replacement

- **Given** a completed Evaluation or replacement lineage would become authoritative
- **When** required durable audit or immutable-outbox acceptance is unavailable
- **Then** the protected transition does not complete
- **And** no partial Evaluation is exposed as authoritative
- **And** recovery can reconcile without duplication.

### `AC-EVAL-26` — Sensitive data is minimized across operational surfaces

- **Given** Evaluation processing produces logs, metrics, traces, queue messages, errors, notifications, audit, or manifest entries
- **When** those records are inspected
- **Then** they contain bounded categories and protected references only
- **And** raw Submission/transcript content, full model output, hidden prompts/reasoning, credentials, and unnecessary participant data are absent.

### `AC-EVAL-27` — Integrity or lawful unavailability is reported honestly

- **Given** a cited source later fails verification or is lawfully unavailable
- **When** an authorized reviewer inspects the historical Evaluation
- **Then** the original citation remains in history with a current limitation annotation
- **And** the source is not falsely shown as verified, silently removed, or restored from an unauthorized copy.

### `AC-EVAL-28` — Reviewer inspection is accessible and responsive

- **Given** an assigned reviewer uses keyboard-only input, assistive technology, 400 percent zoom, or a narrow viewport
- **When** the reviewer moves among Evaluation summary, criteria, Evidence, failure, insufficiency, and integrity states
- **Then** headings, names, states, focus, announcements, source/version context, and return navigation remain operable and understandable
- **And** meaning does not depend on color, hover, sound, motion, or a wide split-pane layout.

### `AC-EVAL-29` — Evaluation service objectives are measurable

- **Given** authorized representative requests whose inputs fit frozen platform limits and no declared provider-wide outage applies
- **When** status and completion latency are measured under `PROP-6`
- **Then** at least 95 percent receive queued/running/existing status within 2 seconds
- **And** at least 95 percent complete within 120 seconds
- **And** timeout, retry, queue, provider, and review-required outcomes remain separately observable without protected content.

### `AC-EVAL-30` — Evaluation data is not reused for learning

- **Given** Evidence, invocation output, rationale, confidence, provisional feedback, or a completed Evaluation exists
- **When** a memory, calibration, analytics-training, harness-improvement, or unrelated-activity consumer requests it in the MVP
- **Then** the request is denied or receives no eligible records
- **And** participant or model content cannot opt the record into reuse.

### `AC-EVAL-31` — Historical Evaluation is reconstructable

- **Given** an authorized reviewer or auditor inspects a completed Evaluation
- **When** the applicable sources remain lawfully available
- **Then** the system can identify the exact Session handoff, Submission binding, transcript cutoff, rubric/procedure, configuration/manifest, deterministic evaluator and model invocations, Evidence set, criterion judgments, completion event, and lineage
- **And** any unavailable element is identified honestly rather than fabricated.

### `AC-EVAL-32` — Frozen procedure selects one evaluator mode per criterion

- **Given** an activated rubric/evaluation procedure contains deterministic, Agent-assisted, and Agent-judgment criteria
- **When** a Session resolves and Evaluation processing begins
- **Then** every criterion uses exactly its frozen evaluator mode and exact evaluator/model bindings
- **And** no worker, model response, participant content, or interface can substitute another mode.

### `AC-EVAL-33` — Objective criterion uses its deterministic evaluator

- **Given** a criterion defines an approved deterministic evaluator for an exact calculation, comparison, validation, or aggregation
- **When** the criterion is processed with valid canonical inputs
- **Then** the exact allowlisted evaluator version produces the procedure-defined bounded output
- **And** the Agent is not used to replace or recompute that authoritative deterministic step.

### `AC-EVAL-34` — Agent-assisted criterion consumes verified deterministic facts

- **Given** a criterion uses `agent_assisted` mode and its deterministic evaluator succeeds
- **When** the Agent evaluates the qualitative portion
- **Then** it receives the verified deterministic facts through protected Evidence references plus only the other exact permitted sources
- **And** its rationale distinguishes those facts from its interpretation.

### `AC-EVAL-35` — Agent cannot override deterministic output

- **Given** an Agent judgment conflicts with a verified deterministic fact or aggregation result
- **When** independent output validation runs
- **Then** the Agent claim does not replace the deterministic output
- **And** the conflict follows the frozen procedure's explicit rejection, insufficiency, or review-required behavior
- **And** both relevant provenance records remain inspectable.

### `AC-EVAL-36` — Deterministic evaluator provenance is reconstructable

- **Given** a deterministic or Agent-assisted criterion completes
- **When** an authorized reviewer or auditor reconstructs it
- **Then** the exact evaluator identifier/version, canonical input digest and protected references, configuration/dependency digest, resource bounds, outcome, output digest/reference, UTC timing, and manifest/audit correlation are available
- **And** raw protected inputs are not duplicated into operational records.

### `AC-EVAL-37` — Deterministic evaluator failure does not weaken evaluation

- **Given** a deterministic evaluator times out, exceeds a resource bound, returns invalid output, fails integrity, or loses a required dependency
- **When** the criterion is processed
- **Then** the procedure-defined failure state is recorded
- **And** the system does not silently fall back to Agent judgment, a newer evaluator, weaker validation, or an invented value.

### `AC-EVAL-38` — Internal evaluators do not create Session tool capability

- **Given** an Evaluation procedure requests participant-provided code execution, an unrestricted script, unapproved network egress, or an external evaluator without an approved contract
- **When** the Evaluation service validates or runs the procedure
- **Then** the operation is denied before execution or disclosure
- **And** no participant-session capability, cross-scope access, side effect, or unaudited external call occurs.

## Edge and failure cases

| Case | Required behavior |
| --- | --- |
| Session completion and Evaluation request race | Evaluation remains ineligible until the complete terminal handoff and manifest state are authoritative; no early model disclosure |
| Terminal Session is `Terminated` or `Aborted` | Apply the approved eligibility policy; do not infer comparability or fabricate an Evaluation |
| Submission source is accepted but not in the Session binding | Preserve it in Submission history but exclude it from the Evaluation |
| Transcript item is after cutoff or unpublished | Exclude and record a bounded validation outcome; do not move the cutoff |
| Evidence locator resolves to another source version | Reject the locator; never fall back to a same-name or latest item |
| Fine-grained parser location is unstable | Cite the whole exact artifact with lower precision or mark the criterion insufficient |
| Model invents a criterion or omits a required one | Fail schema validation; do not publish a completed Evaluation |
| Model emits a valid score with invalid Evidence | Reject completion or record insufficiency according to the frozen procedure |
| Agent output conflicts with verified deterministic fact | Preserve the fact and conflict provenance; apply the frozen rejection, insufficiency, or review-required behavior |
| Deterministic evaluator fails or exceeds a bound | Record the exact failure and do not fall back silently to Agent judgment, a newer evaluator, or weaker validation |
| Participant content attempts evaluator or command injection | Treat it as data; preserve the frozen evaluator binding, arguments, source scope, resource bounds, and egress denial |
| Procedure requests participant-code or unapproved external evaluation | Block before execution or disclosure and record a bounded configuration/policy failure |
| Required Evidence is contradictory | Preserve the conflicting references, uncertainty, and procedure-defined non-judgment or judgment; do not silently discard inconvenient Evidence |
| Provider times out after accepting a request | Reconcile by invocation/provider/idempotency references before retry; keep exact inputs |
| Provider returns after local timeout | Accept at most one authorized completion under the invocation concurrency rule; late output cannot overwrite authority |
| Worker crashes during completion | Recover from the architecture-approved consistency boundary; expose neither a false failure nor partial completion |
| Required audit is unavailable | Do not publish the completed or replacement Evaluation; preserve an honest recoverable state |
| Reviewer assignment is revoked while viewing | Reauthorize the next read/navigation and stop disclosure within the approved revocation target |
| Source becomes unavailable after completion | Append a limitation annotation; do not delete the citation or copy content from logs/backups into the view |
| Oversized or unsupported protected source | Enforce frozen limits, identify affected criteria safely, and route to retry/review rather than truncating silently |
| Malicious content asks the model to expose hidden prompts or another participant | Treat it as untrusted Evidence content, suppress prohibited output, and preserve isolation |
| Replacement request races with review | Preserve both workflow order and immutable lineage; the review feature must define which Evaluation version is decision-eligible |

## Dependencies and rollout

### Dependencies

- Approved [`auth-resource-isolation.md`](auth-resource-isolation.md), [`resolved-session-configuration.md`](resolved-session-configuration.md), [`assessment-setup.md`](assessment-setup.md), [`submission-attempts.md`](submission-attempts.md), and [`session-text-lifecycle.md`](session-text-lifecycle.md).
- Versioned rubric/evaluation-procedure schemas with criterion identifiers, sufficiency rules, confidence/uncertainty representation, aggregation, feedback constraints, and output validation.
- Versioned allowlisted deterministic evaluator registry and runner with canonical input contracts, immutable dependency/configuration identities, resource isolation, protected output references, and manifest/audit adapters.
- Protected exact-source readers for accepted Submission versions, terminal transcript items, resolved configuration, activation baseline, and execution manifest.
- Provider/model invocation boundary with explicit service delegation, bounded policy, manifest provenance, timeout/retry controls, and protected payload references.
- ADR-001, ADR-002, ADR-003, and ADR-005 implementations or equivalent verified contracts.
- Architecture decision for Evidence locators, Evaluation completion consistency, deterministic evaluator isolation/provenance, and replacement lineage before implementation.
- Approved `review-result-release.md` consumer contract before any Human revision, Review decision, Result, or Release behavior is implemented.
- Approved UI/UX interaction specification for reviewer inspection before visual behavior is treated as final.

### Rollout

- Keep Evaluation processing disabled until all five upstream P0 specs are implemented and the terminal handoff/configuration/Submission-binding contracts pass integration tests.
- Start with the assessment MVP's text transcript, exact accepted Submission versions, frozen configuration/manifest sources, and built-in allowlisted deterministic evaluators only. Do not enable voice, participant-session tools, participant-code execution, unapproved external evaluators, memory-learning, calibration, or shared-session Evidence through configuration flags.
- Version the Evaluation envelope, criterion schema, evaluator-mode contract, deterministic evaluator/dependency identity, Evidence locator schema, and integrity procedure. Unknown versions fail closed rather than being reinterpreted.
- Quarantine prototype or migrated Evaluations whose organization, participant, Session handoff, exact sources, rubric/procedure, configuration/manifest, or lineage cannot be verified; do not expose them as completed review inputs.
- Gate downstream review/release consumption on immutable completed status, integrity state, active assignment, and an approved Evaluation-version selection contract.
- Require automated negative authorization/isolation, prompt-injection, citation, retry/concurrency, failure-injection, and sensitive-data tests before rollout.
- UI/UX behavior remains a traceability gap until an approved interaction specification and Playwright evidence cover desktop and narrow reviewer states.

### Observability

Track bounded, non-sensitive metrics for:

- Eligible, queued, running, completed, retryable-failed, terminal-failed, review-required, and replacement invocation counts.
- Queue wait, provider latency, validation latency, completion latency, timeout rate, retry count, and retry exhaustion by approved bounded category.
- Evidence items by source type and precision; citation-validation, integrity, unavailable-source, insufficiency, and aggregation-block rates.
- Criterion counts by evaluator mode; deterministic evaluator latency, failure, bound-exhaustion, invalid-output, Agent-conflict, and prohibited-execution denial rates by bounded evaluator/version category.
- Duplicate/conflicting request, concurrent completion, stale handoff, mutable-alias, post-cutoff, and cross-scope rejection counts.
- Required-audit acceptance failure, manifest append failure, completion recovery, integrity annotation, and replacement-lineage conflict counts.
- Assigned-reviewer source-open latency and authorization-denial categories without raw content or unrestricted identifiers.

Alerts should cover sustained queue backlog, provider or deterministic-evaluator failure, resource-bound exhaustion, prohibited execution or egress attempts, Agent/deterministic conflict spikes, completion-recovery backlog, audit or manifest rejection, citation-integrity spikes, cross-scope attempts, prompt-injection policy violations, and unexpected participant-facing access attempts. Metrics and traces must not contain raw protected content or high-cardinality participant identifiers.

## Open questions

None. `Q-1`–`Q-8` were resolved on 2026-08-06 as recorded below. Deployment-specific evaluator limits and lifecycle durations remain required configuration under approved upper-scope policy; they are not unresolved product semantics.

## Approved decision disposition

| Prior IDs | Approved disposition | Rationale / consequence |
| --- | --- | --- |
| `Q-1`, `PROP-1` | The frozen rubric/evaluation procedure owns exact criteria, configured score or decision fields, completeness, explicit insufficiency, permitted applicability, per-criterion rationale/Evidence/confidence/uncertainty, and aggregation preconditions. | Avoids a universal scoring model while preserving one inspectable platform envelope. Rubric schemas and validators must expose the contract before activation. |
| `Q-2`, `PROP-2` | Every Evidence item uses exact source/version identity plus a versioned source-native locator, integrity state, and explicit precision. Whole-artifact citation is permitted only when finer stable location cannot be verified. | Provides durable navigation without false precision or unnecessary protected-content duplication. Attachment readers and parsers need versioned locator adapters. |
| `Q-3`, `PROP-3` | Automatically evaluate only `Completed` Sessions in the MVP. Route `Terminated` and `Aborted` Sessions to human operational handling without a generated Evaluation. | Incomplete, administratively stopped, or integrity-failed Sessions must not silently appear comparable to ordinary completed assessments. |
| `Q-4`, `PROP-4` | Equivalent retries return the existing completed Evaluation. A replacement requires current authorization, an approved bounded reason, a new invocation/artifact, immutable predecessor/successor lineage, durable audit, and explicit review-eligible version selection downstream. | Enables controlled correction without overwrite, hidden history, or score shopping. |
| `Q-5`, `PROP-5` | Use procedure-defined bounded qualitative confidence per criterion with explicit uncertainty/limitations. Percentage probabilities require a separately approved and validated procedure. | Supports honest uncertainty and avoids uncalibrated numerical precision. |
| `Q-6`, `PROP-6` | Require p95 authoritative status acknowledgment within 2 seconds and 95 percent Evaluation completion within 120 seconds for bounded inputs outside declared provider-wide outages. | Gives reviewers a measurable expectation while keeping queue, provider, timeout, and review-required outcomes separately observable. |
| `Q-7`, `PROP-7` | Define no feature-specific retention duration. Exact Evidence and immutable Evaluation history follow the applicable approved lifecycle while raw content remains in owning protected stores and lawful unavailability is reported honestly. | Avoids inventing privacy or legal policy and minimizes duplicated sensitive content. A separate lifecycle decision still owns concrete durations. |
| `Q-8`, `PROP-8` | The frozen procedure assigns each criterion `deterministic`, `agent_assisted`, or `agent_judgment` mode. Use versioned allowlisted deterministic evaluators for objective rules when available; Agent output cannot override verified facts, authorization, rubric rules, or deterministic aggregation. Internal evaluators confer no participant-session tool capability. | Reduces avoidable model variability while retaining Agent judgment for qualitative criteria. Evaluator inputs, versions, outputs, limits, and failures become reconstructable protected provenance. Participant-code execution and unapproved external evaluators remain disabled pending a separate approved contract. |

## Approved defaults

These defaults are approved with this specification and govern MVP Evidence and internal Evaluation behavior. Stable `PROP-*` IDs are retained for traceability.

- `PROP-1` — Use the rubric-owned structured Evaluation envelope with explicit insufficiency and aggregation only under the frozen rule.
- `PROP-2` — Use exact, versioned, integrity-verifiable Evidence locators and disclose whole-artifact precision when finer location is unavailable.
- `PROP-3` — Automatically evaluate only `Completed` Sessions; route `Terminated` and `Aborted` Sessions to human operational handling without generated Evaluation.
- `PROP-4` — Preserve immutable replacement lineage, require authorized bounded reasons, and make the downstream review-eligible version explicit.
- `PROP-5` — Use bounded qualitative confidence and explicit uncertainty/limitations unless a separately approved procedure validates numeric probabilities.
- `PROP-6` — Apply the approved 2-second p95 status and 120-second 95-percent completion objectives under the stated bounds and exclusions.
- `PROP-7` — Apply the governing lifecycle without a feature-specific duration and keep raw source content in owning protected stores.
- `PROP-8` — Route each criterion through frozen deterministic, Agent-assisted, or Agent-judgment mode; use deterministic evaluation for objective rules when available, preserve evaluator provenance, and prohibit Agent override or implicit Session-tool capability.

## Traceability

| Requirement/AC | Implementation | Automated verification | Playwright/manual evidence | Status |
| --- | --- | --- | --- | --- |
| `REQ-EVAL-1`–`REQ-EVAL-7`, `AC-EVAL-1`–`AC-EVAL-5` | Handoff eligibility, trusted binding, request/idempotency service — architecture and implementation TBD | Eligible/ineligible handoff, mutable alias, stale input, duplicate/concurrent/conflicting request tests | Queued, running, existing, incomplete-handoff, conflict states | Gap |
| `REQ-EVAL-8`–`REQ-EVAL-17`, `AC-EVAL-5`–`AC-EVAL-8`, `PROP-2` | Evidence-source adapters, locator schema, integrity verifier, Evidence-set seal — architecture and implementation TBD | Exact Submission/transcript/config locators; post-cutoff, unbound, forged, cross-scope, integrity, precision tests | Criterion-to-source navigation, whole-artifact, invalid/unavailable citation states | Gap |
| `REQ-EVAL-18`–`REQ-EVAL-28`, `AC-EVAL-9`–`AC-EVAL-14`, `AC-EVAL-20`, `PROP-1`, `PROP-5` | Rubric schema, model-output validator, aggregation and protected-content policy — implementation TBD | Criterion completeness, insufficiency, applicability, ranges, aggregation, citation, rationale and prohibited-content tests | Structured criterion, uncertainty, insufficiency, provisional labeling evidence | Gap |
| `REQ-EVAL-47`–`REQ-EVAL-53`, `AC-EVAL-32`–`AC-EVAL-38`, `PROP-8` | Evaluator-mode resolver, allowlisted deterministic runner, canonical inputs, protected outputs, resource/egress isolation, manifest provenance — ADR and implementation TBD | Mode-freeze, reproducibility, exact-version, Agent-assisted, conflict, failure, injection, resource-limit, prohibited-code/network/external-call tests | Evaluator mode, deterministic fact, Agent interpretation, conflict and failure provenance states | Gap |
| `REQ-EVAL-29`–`REQ-EVAL-36`, `AC-EVAL-15`–`AC-EVAL-19`, `AC-EVAL-27`, `PROP-4` | Invocation state machine, retry/concurrency, completion boundary, immutable lineage/annotations — ADR and implementation TBD | Provider timeout/lost response, partial output, concurrent completion, retry exhaustion, replacement and source-unavailability tests | Failed, retrying, review-required, superseded, integrity-warning states | Gap |
| `REQ-EVAL-37`–`REQ-EVAL-46`, `AC-EVAL-21`–`AC-EVAL-27`, `AC-EVAL-30` | ADR-002 authorization adapters, model trust boundary, ADR-003 audit/outbox, lifecycle policy hooks — implementation TBD | Full wrong-scope/assignment matrix, prompt injection, audit failure, log leakage, unauthorized export/reuse tests | Permission denied, redacted/minimized content, revoked assignment, source authorization states | Gap |
| UX requirements, `AC-EVAL-22`, `AC-EVAL-28` | Reviewer Evidence/Evaluation interaction specification and frontend — UI/UX spec and implementation TBD | Accessibility component and end-to-end tests TBD | Required Playwright accessibility snapshots and desktop/narrow screenshots | Gap |
| Performance requirements, `AC-EVAL-29`, `PROP-6` | Queue, worker, provider and backpressure implementation — architecture TBD | Representative p95 status/completion, bounded input, outage, saturation and recovery tests | Pending, delayed, timeout and recovery messaging | Gap |
| `AC-EVAL-31`, `AC-EVAL-36` | Historical reconstruction verifier and authorized inspection — architecture and implementation TBD | Complete reconstruction, evaluator/model provenance, lawful unavailability, corrupted locator/manifest and lineage tests | Reviewer/auditor provenance view | Gap |
| Downstream boundary, `REQ-EVAL-28`, `AC-EVAL-20`, `PROP-3`, `PROP-4` | Approved [`review-result-release.md`](review-result-release.md) contract; implementation TBD | No-release side-effect, terminal-state routing and explicit review-version selection integration tests | Completed versus unevaluated terminal case handoff | Gap |
