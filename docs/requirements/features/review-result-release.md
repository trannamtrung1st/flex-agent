# Feature: Human review and result release

## Status and source

- Status: Approved
- Owner: Product Lead
- Approvers: Product Lead, Architecture Lead, UI/UX reviewer, Security/Privacy reviewer
- Approved date: 2026-08-06
- Source: [Evaluation, review decision, result, and release](../../product/concept-model.md#evaluation-review-decision-result-and-release), [Resolved execution manifest](../../product/concept-model.md#resolved-execution-manifest), [Session state and events](../../product/concept-model.md#session-state-and-events), [Product invariants](../../product/concept-model.md#product-invariants), [MVP validation slice](../../product/mvp-scope.md#mvp-validation-slice), [MVP executable workflow](../../product/mvp-scope.md#mvp-executable-workflow), and [Reviewer capabilities](../../product/mvp-scope.md#reviewer-capabilities-mvp)
- Catalog entry: P0 #7 — [P0 authoring order](../README.md#p0-authoring-order)
- Related requirements: Consumes authorization and isolation from [`auth-resource-isolation.md`](auth-resource-isolation.md), resolved configuration and manifest provenance from [`resolved-session-configuration.md`](resolved-session-configuration.md), the frozen review/release requirements and fairness baseline from [`assessment-setup.md`](assessment-setup.md), exact Attempt and Submission-version bindings from [`submission-attempts.md`](submission-attempts.md), the terminal Session record and transcript cutoff from [`session-text-lifecycle.md`](session-text-lifecycle.md), and immutable completed Evaluation, Evidence, integrity, and replacement lineage from [`evidence-evaluation.md`](evidence-evaluation.md).
- Related decisions: Approved defaults `PROP-1`–`PROP-10` in this specification. [ADR-001](../../architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md) governs resolved-configuration and manifest integrity. [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md) governs human and service authorization. [ADR-003](../../architecture/decisions/ADR-003-authorization-audit-persistence.md) governs durable audit. [ADR-004](../../architecture/decisions/ADR-004-assessment-activation-baseline-and-atomicity.md) governs the frozen cohort baseline. [ADR-005](../../architecture/decisions/ADR-005-atomic-attempt-start-and-submission-binding.md) governs the exact Attempt, Session, and Submission binding. [ADR-006](../../architecture/decisions/ADR-006-mvp-architecture-baseline-and-evolution.md) governs the lifecycle resolver direction, atomic Release/Result/participant-visibility boundary, and asynchronous-notification separation. [ADR-009](../../architecture/decisions/ADR-009-mvp-session-evaluation-review-contracts.md) approves the detailed [Human review, Result, and Release contract](../../architecture/review-result-release-contract.md), including exact candidate selection, decision/Result atomicity, Release visibility, correction, and availability-only MVP notifications. The [MVP operational defaults](../mvp-operational-defaults.md#protected-data-lifecycle-defaults) govern default outcome-record retention.
- Decision approval: `PROP-1`–`PROP-10` were approved on 2026-08-06. Built-in appeal remains deferred; retention is configurable through an approved versioned lifecycle policy rather than a hard-coded feature duration.

This approved specification is authoritative for observable Human revision, Review decision, Result, and Release behavior in the assessment MVP. Architecture, UI/UX, and implementation must preserve its stable requirements, acceptance criteria, and approved decision dispositions.

## Problem and measurable outcome

An internal Evaluation is not a participant-facing outcome. Before a Result becomes visible, an authorized reviewer needs to inspect the exact Evaluation and Evidence, understand uncertainty and configuration provenance, optionally make a bounded Human revision without overwriting the original, and record an explicit Review decision. A separately authorized Release must then publish only the approved participant-facing Result.

Without a controlled boundary, a replacement Evaluation can race with review, an internal note can leak to the participant, a stale reviewer can release after reassignment, a retry can publish twice, or a later correction can silently replace history. Treating approval, Result construction, and Release as one implicit flag also makes it impossible to explain who judged the outcome, what the participant was shown, and when visibility changed.

The measurable outcome is:

- Every review case is bound to one organization, activity, participant, Enrollment, Attempt, Session, active assignment, frozen review/release policy, and one explicitly selected immutable completed Evaluation version.
- Every Human revision is optional, structured, attributable, reasoned, evidence-linked, immutable once submitted, and connected to the untouched original Evaluation.
- Every Review decision records `Approved`, `Rejected`, or `Escalated` against the exact Evaluation and optional Human revision it considered.
- Every Result is a distinct immutable participant-facing artifact constructed only from fields permitted by the frozen result policy and an approved Review decision.
- Every Release is an explicit, currently authorized, idempotent, durably audited transition that makes exactly one Result version visible to its permitted audience.
- Evaluation replacement, concurrent review, reassignment, revocation, audit failure, integrity failure, stale commands, and uncertain responses cannot publish an unauthorized or ambiguous Result.
- A post-release correction creates new linked review, decision, Result, and Release records; it never overwrites the Evaluation, Human revision, prior decision, prior Result, or prior Release.
- Participants can access only their own released Result and permitted release metadata; internal Evaluation content, confidence, provisional feedback, reviewer notes, assignments, and hidden configuration remain protected.
- Automated verification covers the happy path, unchanged approval, Human revision, rejection, escalation, exact-version selection, authorization, isolation, idempotency, concurrency, durable audit, participant visibility, correction lineage, privacy, accessibility, and recovery.

## Actors and permissions

All protected operations follow [`auth-resource-isolation.md`](auth-resource-isolation.md). A role label, queue item, assignment identifier, Evaluation identifier, Result URL, Release event, notification, cached page, or possession of source content is not authorization evidence.

| Actor | Permitted actions and scope | Explicit restrictions |
| --- | --- | --- |
| Assigned reviewer | Within an active assignment and permitted workflow state, inspect the exact selected Evaluation, Evidence, Submission versions, terminal transcript, configuration/manifest summary, and fairness-relevant facts; prepare and submit a permitted Human revision; record an authorized Review decision | Cannot review an unassigned case, mutate an Evaluation, change frozen rubric or release policy, inspect another participant, select an ineligible Evaluation, expose internal content, or release unless separately authorized |
| Release-authorized reviewer or activity administrator | Within current delegated activity, participant, workflow, and release scope, inspect the approved Review decision and Result preview and explicitly Release the Result | Approval authority does not imply Release authority; cannot edit the approved Result during Release, bypass a required separation-of-duties policy, release a rejected/escalated case, or release in bulk unless separately approved |
| Activity administrator | Within delegated activity scope, manage bounded review assignments and queues, monitor case status, resolve permitted escalations, and inspect minimized operational and audit information | Cannot use activity administration as general access to raw Evidence, internal Evaluation content, reviewer notes, or participant Results without the corresponding capability and resource relationship |
| Organization administrator | Within separately delegated policy, audit, or operational scope, manage upper-scope review/release policy and inspect minimized audit or operational records | Cannot bypass activity, assignment, participant, Session, workflow, or sensitive-content authorization; organization membership alone is not review or Release authority |
| Participant | Within the participant's own active relationship and current visibility policy, view the exact released Result and permitted release/correction status; submit a review request only if a separately approved policy enables it | Cannot view an unreleased Result, Evaluation, Evidence selection, confidence, provisional feedback, reviewer identity or notes, assignment, hidden rubric content, another participant's Result, or prior internal versions not permitted by policy |
| Review/release service | Under explicit service identity and bounded delegation, create review projections, validate selected lineage, construct an allowed Result, commit a Review decision or Release transition, and publish scoped visibility | Cannot infer authority from an event payload, choose a different Evaluation or participant, widen result fields, release automatically from Evaluation completion, or reuse a human credential |
| Audit or compliance reviewer | Within explicit delegated scope, inspect decision, revision, Result, Release, correction, access, and export history with required protected references | Cannot use audit access to obtain unrestricted raw participant content, hidden prompts, reviewer notes, credentials, or unrelated records |
| System operator or support actor | Inspect non-sensitive queue health, latency, projection lag, and bounded failure categories when separately authorized | Cannot inspect protected review or Result content through logs, traces, metrics, queues, screenshots, or support tooling without explicit content authorization |

## Scope

### In scope

- Assignment-scoped review queues and case claiming sufficient for the assessment MVP.
- Review eligibility validation against an immutable completed Evaluation, its integrity state, exact Evidence lineage, Session/Attempt binding, frozen review/release policy, and active authorization.
- Explicit selection and pinning of one review-eligible Evaluation version when replacement lineage exists.
- Reviewer inspection of exact permitted Evidence, Evaluation, configuration/manifest, Submission, transcript, and fairness-relevant context.
- Optional structured Human revision of fields the frozen review policy permits, with rationale, Evidence references, actor, reason, and immutable lineage.
- Reviewer comments separated into internal notes and participant-facing feedback.
- `Approved`, `Rejected`, and `Escalated` Review decisions.
- Construction and validation of one immutable participant-facing Result from an approved Review decision.
- Explicit idempotent Release to the participant-visible audience permitted by current authorization and frozen policy.
- Result visibility, safe status, and post-release correction lineage for the participant.
- Authorization, isolation, concurrency, failure/recovery, audit, export, privacy, lifecycle, accessibility, responsive behavior, and operational observability for review and Release.

### Out of scope

- Creating or editing rubrics, evaluation procedures, agents, harnesses, activities, cohort baselines, review/release policy definitions, or Result schemas.
- Generating, retrying, or replacing an Evaluation; this feature only selects an eligible immutable Evaluation supplied by [`evidence-evaluation.md`](evidence-evaluation.md).
- Editing Submissions, transcript content, Evidence sources, resolved configuration, execution manifests, or Session terminal state.
- Autonomous approval or Release, implicit Release on Evaluation completion, and release without human authorization.
- General reviewer staffing, scheduling, workload balancing, organization-wide case search, or unrestricted content repositories.
- Bulk or cohort-wide Release, scheduled Release, embargo management, anonymous double review, multi-reviewer consensus, and calibration workflows unless later approved.
- External email/SMS delivery, public links, third-party result sharing, certificates, transcripts, badges, or downloadable participant reports unless separately specified.
- Gradebook, learning-management, human-resources, admissions, or other downstream-system integration.
- Hard-coded product-wide retention values, legal-hold rules, deletion procedures, consent text, or compliance certification; this feature requires configurable approved lifecycle policy but does not choose its values.
- Built-in participant appeal adjudication in the MVP under approved default `PROP-7`; a later enabled appeal remains owned by this feature boundary and must preserve the released lineage.
- Hidden model chain-of-thought, raw reviewer scratch work that is not submitted as an authoritative artifact, or unrestricted audit export.

### Boundary terms

- **Review case** — the assignment-scoped workflow record connecting one participant's Attempt and Session to eligible Evaluation lineage and review/release status.
- **Review candidate** — one explicit immutable completed Evaluation version selected as the subject of review; it is never a mutable `latest` alias.
- **Human revision** — an optional immutable reviewer-authored adjustment to permitted Evaluation fields, preserving the original Evaluation and citing stable Evidence or an explicit authorized rationale.
- **Reviewer note** — internal content for authorized review participants; it is never participant-facing merely because a case is approved.
- **Participant-facing feedback** — content intentionally included in the Result under the frozen result policy.
- **Review decision** — one immutable `Approved`, `Rejected`, or `Escalated` outcome referencing the exact review candidate and optional Human revision.
- **Result** — one immutable, validated participant-facing artifact derived from an approved Review decision. It is not visible merely because it exists.
- **Release** — the auditable visibility transition that publishes one exact Result version to the permitted audience.
- **Correction lineage** — immutable predecessor/successor links among post-release review cases, decisions, Results, and Releases.

## User journeys and state transitions

### Review, decision, and release lifecycle

```text
Awaiting evaluation
        │ eligible completed Evaluation selected
        ▼
Ready for review ── assign/claim ──> In review
        ▲                              │
        │                              ├── submit optional Human revision
        │                              │
        │                              ├── reject ─────> Rejected
        │                              ├── escalate ───> Escalated ── resolve/reassign ──> Ready for review
        │                              └── approve ────> Approved decision
        │                                                    │ construct + validate
replacement selected                                      ▼
with preserved lineage                              Result ready
                                                         │ explicit authorized Release
                                                         ▼
                                                      Released
                                                         │ later correction
                                                         ▼
                                              New linked review lifecycle
```

`Rejected` and `Escalated` do not create a releasable Result. `Approved decision`, `Result ready`, and `Released` are separate authoritative states. A submitted Human revision is an artifact, not an implicit decision. A later Evaluation or correction creates new lineage and never reopens or mutates an earlier decision.

### Assigned reviewer reviews and approves unchanged

1. The reviewer opens an assigned case and is authorized against the exact organization, activity, participant, Attempt, Session, assignment, workflow state, and sensitive-content scope.
2. The system identifies the exact selected Evaluation version, its replacement lineage, integrity state, frozen rubric/evaluation and review/release policy, and required source availability.
3. The reviewer inspects criterion judgments, rationale, confidence, uncertainty, Evidence, exact permitted Submission and transcript locations, configuration provenance, and fairness-relevant exceptions.
4. The interface distinguishes internal/provisional content from participant-facing Result content and presents unavailable or integrity-affected sources honestly.
5. The reviewer chooses `Approve unchanged`, supplies any policy-required reason or attestation, and previews the participant-facing Result.
6. At decision commit, the system reauthorizes the reviewer, revalidates assignment and candidate integrity/version, records an immutable `Approved` Review decision, constructs and validates the Result, and accepts required durable audit.
7. The Result remains unreleased until an explicit actor with current Release authority completes Release.

### Reviewer submits a Human revision

1. The reviewer starts from the exact selected Evaluation and sees which fields the frozen review policy permits them to adjust.
2. The reviewer changes only permitted judgment, score/decision, rationale, applicability, or participant-feedback fields; required reasons and Evidence references are entered separately from internal notes.
3. The system validates types, ranges, aggregation, completeness, Evidence ownership, protected-content rules, and differences from the original Evaluation.
4. Submission creates one immutable Human revision linked to the untouched Evaluation, actor, assignment, reason, Evidence, time, and content digest or equivalent integrity reference.
5. The reviewer may then approve, reject, or escalate based on that revision. Saving or submitting the revision alone does not make a Review decision or create a Result.

### Reviewer rejects or escalates

1. The reviewer selects `Reject evaluation` or `Escalate`, sees the consequence, and supplies a bounded required reason.
2. At commit, the system reauthorizes the action and validates current assignment, selected Evaluation, and workflow state.
3. `Rejected` closes that review candidate without a Result or Release. A later Evaluation requires a new linked review candidate.
4. `Escalated` records the reason and authorized destination or operational state without revealing protected content in notifications or queue metadata.
5. Resolution creates or reassigns a permitted review step; it does not mutate the prior decision or silently promote an Evaluation.

### Authorized actor releases a Result

1. The actor opens the unreleased Result through the assigned or delegated release workflow.
2. The view shows the exact Review decision, Result version, participant-visible preview, current visibility audience, release-policy requirements, integrity state, and any separation-of-duties constraint.
3. The actor explicitly confirms Release. The client supplies idempotency and expected-version context but not authoritative organization, participant, decision, Result, or audience scope.
4. Inside the authoritative commit boundary, the service reauthenticates and authorizes the actor, derives the complete ownership chain, confirms that the decision is `Approved`, verifies the exact immutable Result and current policy, rejects stale or conflicting state, and accepts the required durable audit event.
5. The service records one Release and makes exactly that Result version visible to the participant through scoped reads or an architecture-approved equivalent that cannot expose partial visibility.
6. Duplicate equivalent commands return the existing Release. Conflicting reuse changes nothing and surfaces a safe conflict.

### Participant views a released Result

1. The participant opens the result area through their current authenticated Enrollment relationship.
2. Before Release, the participant sees only a neutral pending/not-yet-available state permitted by policy, not an Evaluation, score, reviewer state, or timing inference that would expose protected workflow details.
3. After Release, the participant sees the exact Result fields permitted by the frozen policy, the authoritative release time, and any correction status or support route.
4. The participant cannot navigate from a Result field or identifier to internal Evaluation, Evidence, reviewer notes, another participant, or an unreleased version.
5. If authorization, lifecycle, or source availability later prevents access, the system reports a safe unavailable state without rewriting the historical Release.

### Evaluation replacement or correction changes the eligible lineage

1. A replacement Evaluation or post-release correction request is created through an authorized upstream process with immutable lineage.
2. An in-progress reviewer sees that the selected candidate is no longer the current eligible candidate and cannot commit a decision until the policy-authorized version is explicitly resolved.
3. The system never silently switches the reviewer to a replacement Evaluation or copies a Human revision to it.
4. A post-release correction follows a new linked review, decision, Result, and Release path. The participant-visible current Result changes only after the new Release commits.
5. Prior internal artifacts and Releases remain inspectable to authorized actors; participant-visible correction history follows the approved result policy.

## Business rules

### Review eligibility, assignment, and exact-version selection

- `REQ-REV-1` — A review case must derive organization, activity, cohort, participant, Enrollment, Attempt, Session, Evaluation lineage, and frozen review/release policy from trusted authoritative records; client, queue, event, notification, or identifier input must not establish ownership or scope.
- `REQ-REV-2` — A Review decision may consider only an immutable completed Evaluation whose Session/Attempt/Evidence/configuration lineage is complete, integrity-valid, and eligible under the frozen policy and current narrowing organization policy.
- `REQ-REV-3` — When Evaluation replacement lineage contains more than one completed version, the review case must store one explicit review-candidate identifier and selection reason. `Latest`, `current`, display order, or arrival time must not select the candidate.
- `REQ-REV-4` — Selecting or changing the review candidate must require current authorization, a bounded reason, durable audit, and preservation of the prior selection history; it must not mutate or hide either Evaluation.
- `REQ-REV-5` — An assigned reviewer may read or act only while the assignment, organization/activity relationship, content permission, and workflow state are current. Reassignment or revocation must affect new operations immediately and cached or long-lived access within the approved authorization propagation bound.
- `REQ-REV-6` — Queue list, count, filter, claim, assignment, and workload metadata must be constrained before materialization to the actor's permitted organization, activity, assignment, and content scope; post-filtering an unscoped result is prohibited.
- `REQ-REV-7` — Claim, reassign, relinquish, and escalation transitions must be idempotent, concurrency-safe, and preserve actor, prior assignment, reason, UTC time, authoritative order, and audit correlation.
- `REQ-REV-8` — A review case must surface terminal Sessions without an Evaluation as operationally unresolved under the approved upstream policy; it must not fabricate an Evaluation, decision, or Result.

### Inspection, Human revision, and reviewer content

- `REQ-REV-9` — The reviewer must be able to inspect the original selected Evaluation unchanged, its criterion-level judgments, rationale, confidence, uncertainty, Evidence references, integrity/availability state, replacement lineage, and the authorized configuration/manifest and fairness summary needed to understand it.
- `REQ-REV-10` — Opening Evidence, Submission, transcript, configuration, manifest, revision, or audit content must independently reauthorize the complete target ownership chain and current content permission; a review case or Evaluation reference is not an access token.
- `REQ-REV-11` — A reviewer must be able to approve the selected Evaluation unchanged. Human revision must remain optional unless a frozen policy requires a correction to a particular invalid state.
- `REQ-REV-12` — A Human revision must reference exactly one immutable Evaluation, preserve the original output, identify every changed field and prior/new value or equivalent structured difference, and record actor, assignment, bounded reason, Evidence references, UTC time/order, and integrity/version metadata.
- `REQ-REV-13` — The frozen review policy must define which criterion judgment, applicability, score/decision, rationale, aggregation, and participant-feedback fields a reviewer may revise. A lower scope, reviewer, or client must not widen that field set or range.
- `REQ-REV-14` — A submitted Human revision must pass the frozen schema's completeness, type, range, aggregation, citation, evidence-sufficiency, protected-content, and policy validation. Invalid or partial revision content must not become an authoritative revision.
- `REQ-REV-15` — A submitted Human revision must be immutable. A correction creates a new linked revision or review lineage; it must not edit, delete, detach, or hide an earlier revision.
- `REQ-REV-16` — Internal reviewer notes, participant-facing feedback, required decision reasons, and audit metadata must be separate fields with separate visibility policy. Internal notes must not enter a Result implicitly.
- `REQ-REV-17` — Reviewer-authored and model-authored content must be treated as untrusted display data and must not change authorization, policy, rubric meaning, workflow state, tools, memory, Evidence scope, or release fields.
- `REQ-REV-18` — The system must not request, store, or expose hidden model chain-of-thought as a review justification. Reviewers use the inspectable Evaluation rationale, Evidence, uncertainty, and their own concise submitted reason.

### Review decision

- `REQ-REV-19` — A Review decision must have exactly one outcome: `Approved`, `Rejected`, or `Escalated`, and must reference the exact selected Evaluation plus zero or one submitted Human revision used for that decision.
- `REQ-REV-20` — A decision commit must reauthenticate and reauthorize the actor and revalidate the assignment, ownership chain, selected Evaluation, integrity state, frozen policy, and expected case version inside the authoritative commit boundary.
- `REQ-REV-21` — `Approved` means the exact reviewed content is eligible for Result construction; it does not itself make a Result visible.
- `REQ-REV-22` — `Rejected` must not construct a releasable Result. A later Evaluation or review requires new linked lineage and must preserve the rejected decision.
- `REQ-REV-23` — `Escalated` must not construct a releasable Result. It must record a bounded reason and approved destination/state without placing protected content in queue, notification, log, or audit metadata.
- `REQ-REV-24` — A Review decision must be immutable. Withdrawal, correction, supersession, or reconsideration must append a new authorized artifact and explicit lineage rather than change the original decision.
- `REQ-REV-25` — Concurrent or duplicate decision commands must produce at most one authoritative decision for the same expected case version. Equivalent retries return the existing outcome; conflicts fail without side effects.
- `REQ-REV-26` — Committing a Review decision, its optional Human revision binding, and required audit event must be atomic or use an architecture-approved equivalent that cannot expose a decision without its exact lineage and durable audit.

### Result construction and Release

- `REQ-REL-1` — A Result must be a distinct immutable artifact derived from exactly one `Approved` Review decision and the exact Evaluation/Human revision content that decision references.
- `REQ-REL-2` — The frozen result policy must define the participant-facing schema, required fields, permitted criterion outcomes or scores, aggregation, feedback, explanation, locale, and visibility audience. Result construction must fail closed when that contract is missing or invalid.
- `REQ-REL-3` — Result construction must include only explicitly permitted participant-facing fields. Internal Evaluation confidence, uncertainty, provisional feedback, reviewer notes or identity, hidden rubric/expected-answer content, Evidence selections, configuration internals, prompts, model/provider internals, credentials, and unrelated participant data must be excluded unless a narrower approved field policy explicitly permits a safe representation.
- `REQ-REL-4` — A Result must preserve stable references to its Review decision, selected Evaluation, optional Human revision, Session/Attempt, frozen result-policy version, creation actor/service, UTC time/order, schema version, and integrity metadata without copying unnecessary protected source content.
- `REQ-REL-5` — A Result must pass schema, range, aggregation, completeness, participant-content, localization, unsafe-markup/link, and protected-content validation before it becomes release-eligible.
- `REQ-REL-6` — Approval authority and Release authority must be evaluated separately. The same actor may perform both only when the frozen policy and current delegation permit it; the system must enforce any required separation of duties.
- `REQ-REL-7` — Release must be an explicit human-authorized command against one exact unreleased Result version. Evaluation completion, approval, Result construction, notification processing, elapsed time, or a client-rendered confirmation must not trigger implicit Release.
- `REQ-REL-8` — At Release commit, the service must derive the complete trusted ownership chain; reauthorize the actor and action; verify the exact `Approved` decision, Result integrity and expected version, frozen/current narrowing policy, participant relationship, and permitted audience; and accept required durable audit.
- `REQ-REL-9` — The authoritative Release transition, participant visibility grant/state, exact Result binding, and required durable audit event must commit atomically or through an architecture-approved equivalent that cannot expose the Result before all are authoritative.
- `REQ-REL-10` — Release must be idempotent. An equivalent retry returns the existing Release and participant-visible Result; conflicting idempotency reuse, Result version, audience, or policy context must fail without publishing or changing visibility.
- `REQ-REL-11` — A successful Release must record Release identifier, Result version, participant/audience scope, actor/service, authorizing relationship and policy versions, idempotency outcome, UTC time/order, prior visibility state, and audit correlation.
- `REQ-REL-12` — Before Release, participant reads, lists, counts, caches, indexes, exports, notifications, and indirect status channels must not disclose the Result or internal review state. After Release, they must expose only the exact permitted Result to the permitted participant relationship.
- `REQ-REL-13` — A notification may announce that a Result is available only after authoritative Release and under an approved channel policy. Notification content must not contain the Result, score, sensitive reason, protected identifier, or access-grant material unless separately approved.
- `REQ-REL-14` — A later Evaluation replacement, policy edit, assignment change, or Result reconstruction must not alter an existing Result or Release. A correction requires new linked review, decision, Result, and Release artifacts.
- `REQ-REL-15` — When a correction is released, the participant-facing view must identify that the current Result was updated and its effective Release time without silently presenting the replacement as the original outcome. Prior internal lineage remains available only to authorized actors; participant history follows the approved result policy.
- `REQ-REL-16` — A Release whose client response is lost or times out must be reconcilable by idempotency key and trusted resource scope. Retrying must not duplicate Release, visibility, or notification side effects.

### Authorization, audit, privacy, export, and lifecycle

- `REQ-REV-27` — Every review/release read, list, count, claim, assignment, Evidence navigation, revision, decision, Result preview, Release, participant view, correction, export, cache, index, job, event, projection, and notification must enforce server-side organization, action, complete resource-chain, assignment/delegation, sensitive-content, workflow-state, and visibility authorization.
- `REQ-REV-28` — Inaccessible and nonexistent cases, Evaluations, Results, and Releases must use the approved non-disclosing external behavior while preserving distinct internal diagnostics and security-relevant audit.
- `REQ-REV-29` — Review assignment and candidate selection, submitted Human revision, Review decision, Result creation/validation failure, Release attempt/outcome, participant Result access, correction, sensitive download/export, and security-relevant denial must produce audit or operational events under the approved durability class.
- `REQ-REV-30` — Review decision, Result creation when coupled to approval, Release, correction, and any visibility-expanding transition must be `required_durable` under ADR-003. If required audit cannot be accepted, the protected mutation or disclosure must fail without its side effect.
- `REQ-REV-31` — Audit, logs, metrics, traces, queues, indexes, notifications, error responses, and screenshots must use stable protected references and bounded categories; they must not contain credentials, raw Submissions or transcripts, Evidence excerpts, full Evaluations, Human revision text, reviewer notes, Result content, hidden prompts, or unrestricted participant identifiers.
- `REQ-REV-32` — Protected exports must require explicit export capability, complete resource authorization, approved purpose/scope, bounded record selection, safe format handling, and durable audit. An export must not become a route to unrestricted review content or another participant's Result.
- `REQ-REV-33` — Evaluation, Human revision, decision, Result, Release, correction, assignment, access, export, and audit records must bind to an applicable approved, versioned, configurable lifecycle policy. Organization policy sets non-bypassable permitted bounds; an Activity policy may select or narrow values within those bounds but must not widen them. The resolved policy must define the applicable retention duration or approved disposition per record class plus deletion, legal-hold, consent, export, and evidence-preservation behavior. Missing, invalid, or widening lifecycle configuration must fail closed, and lawful unavailability must be reported without rewriting history.
- `REQ-REV-34` — Review and Result content must not be reused for Dynamic memory, cross-participant learning, calibration, analytics training, unrelated activities, harness improvement, or agent self-modification in the assessment MVP.
- `REQ-REV-35` — Access revocation, participant relationship changes, lawful restriction, or approved lifecycle action may prevent future disclosure but must not be represented as if a historical decision or Release never occurred.

## Data, evidence, and audit

### Logical records

| Record | Required properties and lineage | Mutation rule |
| --- | --- | --- |
| Review case | Stable identity; organization/activity/cohort/participant/Enrollment/Attempt/Session; frozen review/release and lifecycle-policy references; status; eligible Evaluation lineage; current explicit candidate; assignment references; expected version | Status transitions append ordered history; candidate and assignment changes preserve prior values and reasons |
| Review assignment | Case; assignee or delegated group; capabilities; content scope; effective/expiry/revocation state; assigner; reason; UTC time/order | Never silently repointed; reassignment/revocation appends history |
| Candidate selection | Case; exact Evaluation; predecessor/successor Evaluation lineage; selector; bounded reason; integrity state; UTC time/order | Immutable selection event; later selection appends a new event |
| Human revision | Exact Evaluation; structured changes; Evidence references; author/assignment; reason; schema/integrity metadata; UTC time/order | Immutable once submitted; correction creates linked successor |
| Reviewer content | Explicit classification as internal note, participant-facing feedback, decision reason, or escalation reason; author; visibility; UTC time/order | Submitted authoritative content preserves history and classification |
| Review decision | Exact candidate; optional Human revision; `Approved`/`Rejected`/`Escalated`; actor/assignment; reason/attestation; policy and expected versions; UTC time/order; integrity/audit correlation | Immutable; later action creates superseding linked record |
| Result | Exact approved decision and reviewed content; participant-facing payload; schema/locale/review/release/lifecycle-policy versions; audience; creator; integrity metadata; UTC time/order | Immutable; correction creates linked Result |
| Release | Exact Result; prior/new visibility; audience; actor/delegation; review/release/lifecycle-policy versions; idempotency; UTC time/order; audit correlation | Immutable, idempotent transition; correction creates linked Release |
| Correction lineage | Prior/current case, decision, Result, and Release references; reason category; authorizing actor; participant-visible correction status | Append-only lineage; never overwrites prior artifacts |

Physical co-location is permitted, but these remain distinct logical objects with separate authorization, immutability, lifecycle, and state semantics.

### Result content boundary

The Result payload is an allowlisted projection owned by the frozen result-policy schema. Depending on that policy, it may contain:

- Assessment/task identity suitable for the participant.
- Overall outcome or configured aggregate.
- Permitted criterion-level outcomes or scores.
- Participant-facing rationale or feedback intentionally approved for disclosure.
- Explicit insufficiency, limitation, or not-applicable statements when participant-visible.
- Release time, Result version, correction status, and a permitted support or review-request route.

The Result payload does not inherit every field from the Evaluation or Human revision. Absence from the allowlist means exclusion. Reviewer preview and participant rendering must use the same validated payload version, subject only to actor-specific surrounding controls and current lawful availability.

### Required audit and correlation events

At minimum, record or correlate:

- Review case created, found ineligible, or linked to a terminal Session without Evaluation.
- Review assignment claimed, assigned, reassigned, relinquished, expired, or revoked.
- Candidate Evaluation selected, changed, rejected as stale, or blocked by integrity/availability state.
- Sensitive Evaluation, Evidence, Submission, transcript, configuration, or manifest content accessed or denied when policy requires it.
- Human revision submitted, rejected by validation, or superseded.
- Review decision requested, committed, deduplicated, conflicted, rejected, or escalated.
- Result constructed, validation failed, became release-eligible, or was superseded.
- Release requested, authorized, denied, committed, deduplicated, conflicted, or reconciled after an uncertain response.
- Participant Result access permitted, denied, or made lawfully unavailable when policy requires it.
- Correction initiated, approved, released, or failed.
- Sensitive download/export attempted, completed, or denied.
- Cross-organization, wrong participant, wrong assignment, guessed identifier, stale permission, forged lineage, pre-release access, unsafe content, or idempotency conflict denied when security-relevant.

Events use UTC plus an authoritative sequence or equivalent ordering. They contain stable protected references and bounded metadata, not copied review or Result content. An authorized investigator must be able to reconstruct who saw and changed what, which exact Evaluation and optional Human revision informed the decision, which Result was approved, who released it, what audience became eligible, when visibility changed, and how a correction relates to prior history.

## Quality requirements

### UX and accessibility

- The reviewer workspace must distinguish awaiting-evaluation, ready, assigned, in-review, revision-draft, revision-submitted, stale-candidate, integrity-warning, source-unavailable, rejected, escalated, approved, Result-ready, release-pending, released, corrected, permission-denied, and dependency-failure states in text and structure rather than color alone.
- The exact Evaluation version and replacement status must remain visible while reviewing. A candidate change must interrupt decision submission with an explanation and explicit recovery; the interface must not silently refresh to different evidence.
- Internal Evaluation content, reviewer notes, participant-facing feedback, Result preview, and released Result must be visually and programmatically distinguishable.
- `Approve unchanged`, `Submit revision`, `Reject`, `Escalate`, and `Release` must state their consequence. Release and other irreversible actions require deliberate confirmation that identifies the Result and audience without exposing protected identifiers unnecessarily.
- Forms must preserve entered revision/reason content on recoverable validation, conflict, or network failure. They must identify field-level and summary errors, move focus appropriately, and not claim success until authoritative status is reconciled.
- Evidence navigation must focus the exact permitted source location or an explicit whole-artifact/unavailable explanation and provide a reliable return path to the originating criterion.
- Participant pre-release states must not imply a score or expose reviewer timing. Released and corrected states must clearly identify availability, effective time, current version status, and permitted next action.
- Review queues, criterion navigation, revision forms, dialogs, Result previews, and participant views must be fully keyboard operable with programmatic names, headings, landmarks, status announcements, logical focus, and no keyboard trap.
- Scores, decisions, warnings, uncertainty, stale state, correction, and Release status must not rely on color, icon, hover, animation, position, or sound alone.
- At narrow widths and 400 percent zoom, reviewers must be able to inspect one criterion and its Evidence, revise, preview, decide, and Release sequentially without hidden actions or two-dimensional scrolling for ordinary text. Participant Results must reflow without losing labels or status.
- WCAG 2.2 AA is the approved requirements baseline under `PROP-9`. Review-side interaction is governed by the approved [Evidence, Evaluation, and Human Review interaction specification](../../ui-ux/evidence-evaluation-human-review.md); Release and Participant-facing interaction is governed by the approved [Result and Release interaction specification](../../ui-ux/result-release.md).

### Performance and reliability

- Queue, case, candidate, Result, and Release status queries must be bounded and scoped before materialization. One organization, activity, large Evaluation, or slow protected-source adapter must not starve unrelated authorized review work.
- Claim, candidate selection, revision submission, decision, Result creation, and Release commands must require positive timeouts, idempotency where applicable, expected versions, and bounded retries. Retrying after a lost response must reconcile authoritative state before presenting failure or resubmitting.
- A process restart, duplicate delivery, delayed projection, notification failure, or participant read race must not duplicate a decision/Release, expose an unreleased Result, lose immutable history, or revert visibility.
- Required protected records and audit acceptance must be authoritative before success is shown. Read projections and notifications may lag only within a measured bound and must never broaden source-of-truth visibility.
- Under approved default `PROP-8`, 95 percent of bounded queue/case/status reads and authoritative decision/Release acknowledgments must complete within 2 seconds, and a committed Release must become visible through the authoritative participant read path within 5 seconds at the 95th percentile, excluding declared platform-wide outages. Delayed projections must show a reconciling state rather than a contradictory outcome.
- Release-state invariants, idempotency, concurrency, and visibility isolation must be verified under process termination, transaction failure, audit failure, projection lag, retry storms, and uncertain client response before production rollout.

### Security and privacy

- Review and Release are sensitive trust boundaries. All identifiers, assignments, queue entries, lineage, fields, roles, policy references, and visibility audiences supplied by clients, jobs, events, notifications, or models must be rederived or verified against authoritative state.
- Review content must be minimized by actor and task. Sensitive Evidence and full Evaluation detail use progressive disclosure; Result construction copies only allowlisted participant-facing fields.
- Rich text, Markdown, links, filenames, reviewer input, Evaluation output, and Result content must be rendered as untrusted data without script execution, unsafe external retrieval, state-changing links, formula injection in exports, markup spoofing of trusted notices, or capability escalation.
- Caches, search indexes, object storage, download capabilities, browser history, telemetry, analytics, queues, backups, notifications, and exports must preserve organization, activity, participant, Session, Evaluation, review-case, Result, Release, assignment, and visibility isolation.
- Signed URLs, object keys, digests, Evaluation/Result identifiers, notification links, and Release references do not prove authorization and must not be reusable across actors or scopes.
- Hidden prompts, expected answers, internal confidence, reviewer identity/notes, security controls, secrets, private endpoints, model/provider internals, unrelated participant data, and raw protected content must not leak through Results, errors, logs, audit, metrics, screenshots, accessibility names, URLs, or notifications.
- Negative tests must cover wrong organization/activity/cohort, wrong participant/Enrollment/Attempt/Session, wrong or expired assignment, guessed Evaluation/revision/decision/Result/Release identifiers, forged parent or lineage, stale candidate, revoked Release authority, separation-of-duties violation, pre-release reads, cache/index leakage, unsafe markup/link, export injection, duplicate/conflicting command, audit failure, and correction race.

## Acceptance criteria

### `AC-REV-1` — Eligible completed Evaluation enters review safely

- **Given** a completed immutable Evaluation has valid Session/Attempt/Evidence/configuration lineage and current eligibility
- **When** the review service creates or opens its case
- **Then** the case binds the trusted organization-through-Session ownership chain, frozen review/release policy, active assignment state, and one explicit Evaluation version
- **And** no mutable alias or client-supplied owner selects the candidate.

### `AC-REV-2` — Terminal Session without Evaluation stays unresolved

- **Given** a Session is `Terminated` or `Aborted`, Evaluation processing failed, or no eligible completed Evaluation exists
- **When** the case is listed or opened
- **Then** it is shown as operationally unresolved with a safe permitted next action
- **And** no Evaluation, decision, Result, or Release is fabricated.

### `AC-REV-3` — Replacement lineage requires explicit candidate selection

- **Given** two completed Evaluations exist in authorized replacement lineage
- **When** review begins or the replacement arrives during review
- **Then** exactly one version is explicitly selected under current policy with reason and audit
- **And** the system neither uses `latest` nor silently changes the reviewer's candidate.

### `AC-REV-4` — Assigned reviewer sees only the permitted case

- **Given** a reviewer has one active assignment
- **When** the reviewer lists, counts, searches, or opens review work
- **Then** only records inside that assignment and current content scope are materialized
- **And** guessed identifiers or changed filters cannot reveal another participant or unassigned totals.

### `AC-REV-5` — Source navigation reauthorizes every target

- **Given** an assigned reviewer can inspect a criterion
- **When** the reviewer opens an Evidence, Submission, transcript, configuration, manifest, revision, or audit reference
- **Then** the target's complete ownership chain and current content permission are reauthorized
- **And** denied or unavailable content is explained without leaking it.

### `AC-REV-6` — Reviewer approves Evaluation unchanged

- **Given** the exact selected Evaluation is eligible and no Human revision is required
- **When** an authorized reviewer chooses `Approve unchanged` and satisfies required attestation
- **Then** an immutable `Approved` Review decision references the original Evaluation and no Human revision
- **And** a distinct validated Result is created without becoming participant-visible.

### `AC-REV-7` — Human revision preserves the original

- **Given** the reviewer is permitted to adjust configured fields
- **When** a valid Human revision is submitted
- **Then** it records structured differences, reason, exact Evidence references, actor/assignment, time/order, and lineage to the untouched Evaluation
- **And** submitting it alone does not decide or release the case.

### `AC-REV-8` — Invalid revision cannot become authoritative

- **Given** a proposed revision changes a prohibited field, violates a range or aggregation rule, omits a required reason, cites unauthorized Evidence, or contains prohibited disclosure
- **When** it is submitted
- **Then** validation fails with accessible bounded guidance
- **And** no Human revision, decision, Result, Release, or partial authoritative content is created.

### `AC-REV-9` — Internal notes cannot leak into Result

- **Given** a case contains internal notes and separately classified participant-facing feedback
- **When** a Result is constructed and previewed
- **Then** only allowed participant-facing content appears
- **And** internal notes, reviewer identity, and audit metadata are absent from the payload and participant view.

### `AC-REV-10` — Reviewer rejects an Evaluation

- **Given** the selected Evaluation is eligible for decision and the reviewer has rejection authority
- **When** the reviewer submits a valid bounded rejection reason
- **Then** one immutable `Rejected` decision is recorded with exact lineage and durable audit
- **And** no releasable Result is created.

### `AC-REV-11` — Reviewer escalates safely

- **Given** the reviewer encounters a policy-defined uncertainty, conflict, or integrity concern
- **When** the reviewer escalates with a valid reason and permitted destination
- **Then** one immutable `Escalated` decision and next operational state are recorded
- **And** protected content is absent from queue and notification metadata
- **And** no releasable Result is created.

### `AC-REV-12` — Stale assignment or permission blocks decision

- **Given** the reviewer loaded a valid case but the assignment or permission is revoked before commit
- **When** the reviewer submits a revision or decision
- **Then** commit-time authorization denies the action without changing authoritative state
- **And** cached and long-lived access narrows within the approved revocation target.

### `AC-REV-13` — Concurrent decisions have one authority

- **Given** two actors or browser tabs submit decisions for the same expected case version
- **When** the commands race
- **Then** at most one authoritative decision commits
- **And** the other receives a non-destructive stale/conflict response with a path to reload.

### `AC-REV-14` — Duplicate decision is idempotent

- **Given** a decision committed but the client did not receive the response
- **When** the same authorized request is retried with matching idempotency and trusted payload digest
- **Then** the existing decision is returned
- **And** no duplicate decision, Result, or audit interpretation is created.

### `AC-REV-15` — Required audit gates Review decision

- **Given** a Review decision otherwise passes validation
- **When** its required durable audit event or immutable outbox cannot be accepted
- **Then** the decision and coupled Result creation fail without authoritative side effects
- **And** the reviewer sees a recoverable unavailable state.

### `AC-REL-1` — Result uses only the participant-facing schema

- **Given** an approved decision references an Evaluation and optional Human revision containing internal and participant-facing fields
- **When** the Result is constructed
- **Then** it includes every required and only permitted field from the frozen result policy
- **And** it preserves exact lineage without copying prohibited internal content.

### `AC-REL-2` — Invalid Result cannot become release-eligible

- **Given** Result content is incomplete, out of range, inconsistent with aggregation, unsafe to render, or contains prohibited protected content
- **When** construction or validation runs
- **Then** the Result is not release-eligible
- **And** the case preserves a bounded failure state without exposing partial content to the participant.

### `AC-REL-3` — Approval does not Release

- **Given** a Review decision is `Approved` and the Result is valid
- **When** no authorized Release command has committed
- **Then** participant reads, lists, counts, caches, indexes, exports, and notifications do not disclose the Result
- **And** internal views label it `Unreleased`.

### `AC-REL-4` — Release authority is independent

- **Given** an actor may approve but lacks current Release authority, or policy requires a different releasing actor
- **When** that actor attempts Release
- **Then** the command is denied without changing visibility
- **And** the same actor may Release only when current policy and delegation explicitly allow it.

### `AC-REL-5` — Authorized Release publishes one exact Result

- **Given** an exact immutable Result has an `Approved` decision, valid integrity, current participant relationship, and satisfied release policy
- **When** an authorized actor explicitly confirms Release
- **Then** one Release, exact Result binding, participant visibility transition, and required durable audit commit atomically or equivalently
- **And** only that Result becomes visible to the permitted participant.

### `AC-REL-6` — Duplicate Release is idempotent

- **Given** Release committed but the client response was lost
- **When** the equivalent request is retried with matching idempotency context
- **Then** the existing Release and visibility status are returned
- **And** no duplicate Release, notification, or visibility transition occurs.

### `AC-REL-7` — Conflicting Release changes nothing

- **Given** an idempotency key, expected version, Result identity, audience, or policy context conflicts with an existing request
- **When** Release is attempted
- **Then** the command fails safely without publishing or replacing content
- **And** the conflict is auditable without exposing the other payload.

### `AC-REL-8` — Required audit failure blocks Release

- **Given** Release otherwise passes authorization and validation
- **When** required durable audit cannot be accepted
- **Then** no Release or participant visibility side effect commits
- **And** retry can reconcile safely after recovery.

### `AC-REL-9` — Participant sees only own released Result

- **Given** one participant has a released Result and another has an unreleased or different Result
- **When** either participant reads, lists, changes an identifier, follows a notification link, or requests an export
- **Then** each sees only their own currently permitted released Result
- **And** inaccessible and nonexistent targets follow non-disclosing behavior.

### `AC-REL-10` — Pre-release state is neutral

- **Given** a participant's Session is complete but review is queued, active, rejected, escalated, approved, or Result-ready without Release
- **When** the participant opens the result area
- **Then** the view discloses only the policy-permitted neutral availability state and safe next action
- **And** it does not reveal Evaluation content, score, reviewer identity, assignment, or protected workflow timing.

### `AC-REL-11` — Released Result is accessible and responsive

- **Given** a participant or release-authorized actor uses keyboard-only input, assistive technology, a narrow viewport, or 400 percent zoom
- **When** they inspect Result status, content, correction state, preview, confirmation, validation, or error states
- **Then** controls and content retain programmatic names, reading order, focus, reflow, non-color status, and operability consistent with the approved accessibility baseline.

### `AC-REL-12` — Unsafe content is inert

- **Given** Evaluation, reviewer, or participant-facing content contains scripts, active markup, misleading system-notice markup, unsafe links, or spreadsheet formulas
- **When** it appears in review, Result, notification, or export surfaces
- **Then** it is blocked, neutralized, or safely represented according to output context
- **And** it cannot execute, retrieve external content automatically, spoof trusted actions, or escape its authorized scope.

### `AC-REL-13` — Replacement Evaluation cannot alter released history

- **Given** a Result was released from one Evaluation and a later authorized replacement Evaluation completes
- **When** review consumers process the replacement
- **Then** the prior Evaluation, decision, Result, Release, and participant-visible current state remain unchanged
- **And** any correction begins a new explicit linked review lifecycle.

### `AC-REL-14` — Correction Release is explicit and traceable

- **Given** a correction completes new authorized review and produces a valid replacement Result
- **When** the replacement Result is released
- **Then** it becomes the participant-visible current Result with an update notice and new effective time
- **And** prior internal lineage remains immutable and reconstructable.

### `AC-REL-15` — Lawful unavailability preserves historical truth

- **Given** lifecycle, legal restriction, participant relationship change, or source unavailability prevents current disclosure
- **When** an authorized actor or participant requests a prior record
- **Then** the system reports the permitted unavailable state honestly
- **And** it does not rewrite audit history to imply the decision or Release never occurred.

### `AC-REV-16` — Review and Result data are not reused for learning

- **Given** the assessment MVP contains Evaluation, reviewer, Result, or Release content
- **When** memory, calibration, analytics-training, harness-improvement, or unrelated-activity processing is attempted
- **Then** reuse is denied and, when security-relevant, audited
- **And** content cannot opt itself into reuse.

### `AC-REV-17` — Review/Release performance is measurable

- **Given** inputs are within approved platform bounds and no declared platform-wide outage exists
- **When** representative review and Release load tests run
- **Then** the approved `PROP-8` status/acknowledgment and participant-visibility objectives are measured separately
- **And** saturation, projection lag, and failure recovery do not weaken authorization or visibility isolation.

### `AC-REV-18` — Historical outcome is reconstructable

- **Given** an authorized investigator inspects an original or corrected outcome
- **When** reconstruction is requested
- **Then** the system identifies the exact Session/Attempt, Evaluation and Evidence lineage, optional Human revision, Review decision, Result payload/version, Release/audience, actors/delegations, policy versions, UTC order, and audit correlations
- **And** missing or lawfully unavailable sources are reported rather than silently substituted.

### `AC-REV-19` — Negative review/release coverage gates rollout

- **Given** the feature is considered for production rollout
- **When** the automated authorization and invariant matrix is evaluated
- **Then** it covers every applicable wrong-scope, stale-state, lineage, concurrency, idempotency, audit, unsafe-content, pre-release disclosure, correction, and export case listed in this specification
- **And** rollout is blocked while an applicable negative case is missing or failing.

### `AC-REV-20` — Lifecycle configuration is bounded and reconstructable

- **Given** organization policy defines permitted lifecycle bounds and an Activity selects or narrows a lifecycle policy for review and Result records
- **When** the cohort baseline is activated or a Review decision, Result, Release, correction, export, or lifecycle action is committed
- **Then** the system binds and enforces the exact approved policy version and applicable record-class disposition within the upper-scope bounds
- **And** missing, invalid, expired, unverifiable, or widening configuration fails closed without silently extending retention or deleting required history.

## Edge and failure cases

| Case | Required behavior |
| --- | --- |
| Review case created before Evaluation completion | Remain awaiting/unresolved; do not expose partial Evaluation or permit decision |
| Terminal Session is `Terminated` or `Aborted` | Follow upstream no-automatic-Evaluation policy and route operationally without fabricated outcome |
| Multiple replacement Evaluations exist | Require one explicit eligible candidate and preserve selection history; never use `latest` |
| Replacement arrives while reviewer edits | Preserve input locally where safe, mark candidate stale, block commit, and require explicit reconciliation |
| Assignment expires while content is open | Deny the next protected read/action and narrow long-lived access within the approved revocation bound |
| Evidence becomes unavailable during review | Show exact affected state; block decision if frozen policy requires the source; never substitute content |
| Human revision has an invalid citation or score | Reject submission, preserve recoverable input, and create no authoritative revision |
| Reviewer notes contain participant-facing-looking text | Keep classification authoritative; never infer visibility from wording or placement |
| Two reviewers decide concurrently | One expected-version transition wins; the other receives a safe conflict |
| Approver lacks Release authority | Preserve approved/unreleased state and deny Release |
| Release permission is revoked between confirmation and commit | Commit-time authorization denies with no visibility change |
| Required audit is unavailable | Fail the coupled decision, Result, Release, correction, export, or disclosure without protected side effect |
| Release response is lost | Reconcile by trusted scope and idempotency; do not repeat visibility or notification |
| Notification fails after Release | Keep authoritative Release; retry bounded notification separately without including Result content |
| Read projection lags after Release | Show reconciling status from authoritative state; never show unreleased content early or revert committed visibility |
| Result contains unsafe markup or protected content | Block release eligibility and surface bounded validation guidance |
| Participant guesses another Result ID | Return non-disclosing denial and record security telemetry/audit as policy requires |
| Correction races with prior Result access | Serve one authoritative visible version per read and explicit correction status; preserve both Releases |
| Result becomes lawfully unavailable | Preserve historical Release and show only the permitted unavailable state |
| Export formula or markup injection | Neutralize according to the export format and preserve literal content safely |
| Lifecycle policy is missing, invalid, or widens an upper-scope bound | Fail activation or the affected protected transition; do not invent a duration, silently extend retention, or delete required history |
| Lifecycle policy changes after records exist | Apply only through its approved effective-time and migration/disposition rules; preserve the historical policy version and do not reinterpret prior decisions or Releases silently |

## Dependencies and rollout

### Dependencies

- Approved and implemented authorization, complete resource-chain isolation, non-disclosing denial, service delegation, and revocation behavior from [`auth-resource-isolation.md`](auth-resource-isolation.md) and ADR-002.
- Immutable resolved configuration/manifest, cohort baseline, exact Attempt/Session/Submission binding, terminal Session handoff, and completed Evaluation/Evidence lineage from P0 #2–#6.
- Versioned frozen review/release policy, participant-facing Result schema, and applicable configurable lifecycle policy available before cohort activation.
- Approved [Human review, Result, and Release contract](../../architecture/review-result-release-contract.md) and [ADR-009](../../architecture/decisions/ADR-009-mvp-session-evaluation-review-contracts.md) for candidate/decision/Result records, lifecycle enforcement, concurrency/idempotency, atomic Release/visibility/audit, correction, and MVP notifications.
- Approved [Evidence, Evaluation, and Human Review interaction specification](../../ui-ux/evidence-evaluation-human-review.md) for review queues, criterion/Evidence inspection, Human revision, Review decision, pre-decision preview, conflict, and Reviewer accessibility states, plus the approved [Result and Release interaction specification](../../ui-ux/result-release.md) for Release confirmation, Participant Result, correction, and their accessibility states.
- Approved versioned lifecycle policy plus export, notification-channel, and rendering/sanitization policies where those capabilities are enabled.

### Rollout

- Keep Release disabled until every upstream P0 contract and the complete participant-to-Result integration path pass positive and negative tests.
- Version the review-case, candidate-selection, Human-revision, decision, Result, Release, and correction-lineage contracts. Unknown versions fail closed rather than being reinterpreted.
- Quarantine prototype or migrated records whose organization/participant/Session/Evaluation/decision/Result/Release ownership, integrity, visibility, or lineage cannot be verified.
- Begin with individual explicit Release under approved `PROP-5`; bulk, scheduled, and external-channel release remain disabled.
- Use staged internal test cohorts with synthetic data, then authorized pilot cohorts, before general availability. Rollback must prevent new Release without hiding previously committed history.
- Do not mark implemented or release-ready until automated invariant tests, security isolation tests, accessibility verification, and required Playwright desktop/narrow evidence exist.

### Observability

Track bounded metadata without raw review or Result content:

- Review cases awaiting Evaluation, unassigned, assigned, in review, stale-candidate, escalated, approved/unreleased, released, corrected, and blocked by integrity or policy.
- Assignment claim/reassignment conflicts and revocation-propagation outcomes.
- Human revision validation failures by bounded category.
- Decision and Release authorization denial, stale-version conflict, idempotent replay, conflicting replay, audit failure, and uncertain-response reconciliation.
- Time from eligible Evaluation to review start, Review decision, Result readiness, and Release; report queue wait separately from active review time.
- Result validation failures and protected-content suppression categories.
- Authoritative Release-to-participant-visibility latency, projection lag, and notification lag/failure separately.
- Cross-scope, guessed-ID, pre-release access, unsafe-content, and unauthorized export attempts.
- Records with missing/ambiguous Evaluation-to-decision-to-Result-to-Release lineage; rollout target is zero.
- Records missing an applicable lifecycle-policy version, outside configured bounds, or past due for an authorized lifecycle action.
- Released Results visible before authoritative Release or to the wrong participant; release target is zero.

## Open questions

None. `Q-1`–`Q-10` were resolved on 2026-08-06 as recorded below.

## Approved decision disposition

| Prior IDs | Approved disposition | Rationale / consequence |
| --- | --- | --- |
| `Q-1`, `PROP-1` | Pin one exact completed Evaluation as the review candidate; changing it requires current authorization, a bounded reason, durable audit, and explicit stale-review handling. | Prevents mutable aliases or replacement races from silently changing the basis of review. |
| `Q-2`, `PROP-2` | Represent a submitted Human revision as an immutable structured difference over one exact Evaluation, limited by the frozen review policy and linked to stable Evidence. | Allows accountable human adjustment without overwriting the original or widening the rubric/policy. |
| `Q-3`, `PROP-3` | Construct Result content through a versioned deny-by-default participant-facing field allowlist. | Keeps Result distinct from internal Evaluation and reviewer content and reduces accidental disclosure. |
| `Q-4`, `PROP-4` | Authorize approval and Release separately; the same actor may perform both only when the frozen policy permits, and configured separation of duties must be enforced. | Preserves explicit control without imposing universal two-person operation on every MVP activity. |
| `Q-5`, `PROP-5` | MVP Release is individual, explicit, immediate, and bound to one exact Result. Bulk, scheduled, embargoed, and external-channel Release are deferred. | Completes the vertical slice while limiting high-impact concurrency and disclosure risk. |
| `Q-6`, `PROP-6` | A correction creates new linked review, decision, Result, and Release artifacts. Participants see the current corrected Result, update notice, and effective time; detailed prior content is restricted unless policy permits it. | Prevents silent overwrite while communicating that the visible outcome changed. |
| `Q-7`, `PROP-7` | Built-in participant appeal/review-request intake is deferred beyond the MVP. A configured support route may be informational; any later appeal must reference the exact released Result and preserve history. | Appeal standing, window, routing, reviewer independence, Evidence intake, and adjudication require a later approved specification. |
| `Q-8`, `PROP-8` | Apply p95 two-second bounded status/acknowledgment and p95 five-second committed-Release-to-authoritative-participant-visibility objectives under the stated exclusions. | Makes system responsiveness measurable without imposing a target on human review duration. |
| `Q-9`, `PROP-9` | Use WCAG 2.2 AA as the requirements baseline across desktop and narrow layouts. | Provides a testable inclusive baseline while detailed interactions are governed by the approved review-side and Result/Release UI/UX specifications. |
| `Q-10`, `PROP-10` | Retention is configurable through an approved versioned lifecycle policy. Organization policy defines non-bypassable permitted bounds; Activity policy may select or narrow within them. Exact values remain policy configuration rather than hard-coded feature behavior. | Supports different legitimate lifecycle needs without permitting lower scopes to extend upper-scope retention or leaving records without an enforceable disposition. |

## Approved defaults

- `PROP-1` — Pin one exact review-eligible Evaluation version; changing it requires authorization, reason, durable audit, and explicit stale-review handling.
- `PROP-2` — Represent a submitted Human revision as an immutable structured difference over one exact Evaluation, limited by the frozen review policy and linked to stable Evidence.
- `PROP-3` — Construct the Result through a deny-by-default, versioned participant-facing field allowlist; never copy internal Evaluation or reviewer content implicitly.
- `PROP-4` — Authorize approval and Release separately; allow the same actor only when policy permits and enforce separation of duties when configured.
- `PROP-5` — Limit MVP Release to one explicit immediate command for one exact Result; defer bulk, scheduled, embargoed, and external-channel Release.
- `PROP-6` — Correct a released outcome through new linked artifacts and show participants the current Result plus update status and effective time without exposing detailed prior content by default.
- `PROP-7` — Defer built-in appeal/review-request intake beyond the MVP; any configured support route is informational and any later appeal preserves exact released lineage.
- `PROP-8` — Apply p95 2-second bounded review/status/acknowledgment and p95 5-second committed-Release-to-authoritative-visibility objectives under the stated exclusions.
- `PROP-9` — Use WCAG 2.2 AA as the review and Result requirements baseline; detailed interaction design remains subject to the applicable approved UI/UX specification.
- `PROP-10` — Require an approved versioned configurable lifecycle policy: Organization policy sets non-bypassable bounds, Activity policy may select or narrow within them, exact record-class dispositions are reconstructable, and missing or widening configuration fails closed.

## Traceability

| Requirement/AC | Automated verification | Playwright/manual evidence |
| --- | --- | --- |
| `REQ-REV-1`–`REQ-REV-8`, `AC-REV-1`–`AC-REV-4`, `PROP-1` | Eligibility, no-Evaluation, exact-version, replacement, stale candidate, wrong-scope list/count, claim/reassign/revoke concurrency tests | Awaiting, ready, assigned, stale, unresolved, denied states |
| `REQ-REV-9`–`REQ-REV-18`, `AC-REV-5`–`AC-REV-9`, `PROP-2` | Source reauthorization, field allowlist/range, Evidence link, immutability, note leakage, unsafe-content, chain-of-thought exclusion tests | Evaluation/Evidence inspection, revision form, validation, focus return, internal/participant content separation |
| `REQ-REV-19`–`REQ-REV-26`, `AC-REV-6`, `AC-REV-10`–`AC-REV-15` | Approve unchanged, revised approve, reject, escalate, stale permission, concurrent/duplicate/conflicting decision, audit failure tests | Decision confirmations, conflict/recovery, approved/rejected/escalated states |
| `REQ-REL-1`–`REQ-REL-5`, `AC-REL-1`, `AC-REL-2`, `PROP-3` | Allowlist/completeness/range/aggregation, internal-field leakage, locale, markup/link, integrity tests | Result preview, validation, unavailable and unsafe-content states |
| `REQ-REL-6`–`REQ-REL-13`, `AC-REL-3`–`AC-REL-12`, `PROP-4`, `PROP-5` | Separate authority, pre-release denial, exact participant, duplicate/conflicting Release, audit failure, lost response, projection/notification race tests | Release preview/confirmation/pending/success/error; participant pending/released/denied states |
| `REQ-REL-14`–`REQ-REL-16`, `AC-REL-13`–`AC-REL-15`, `PROP-6` | Replacement no-side-effect, correction race, new Release, prior lineage, lawful unavailability tests | Corrected Result, update notice, historical/unavailable states |
| `REQ-REV-27`–`REQ-REV-35`, `AC-REV-12`, `AC-REV-15`, `AC-REV-16`, `AC-REV-18`–`AC-REV-20`, `PROP-10` | Full wrong-scope/identifier matrix, revocation, audit durability/redaction, export injection/isolation, configured duration/disposition, upper-bound narrowing, missing/widening policy, effective-time/migration, non-reuse and reconstruction tests | Permission denied, redacted, export, lifecycle status, unavailable and audit/provenance views |
| UX requirements, `AC-REL-10`–`AC-REL-12`, `PROP-9` | Component accessibility and end-to-end keyboard/screen-reader tests TBD | Required Playwright accessibility snapshots and desktop/narrow screenshots across applicable states |
| Performance requirements, `AC-REV-17`, `PROP-8` | Representative p95 reads/acknowledgments/visibility, saturation, outage, projection lag, retry-storm and recovery tests | Pending, reconciling, delayed and recovery messaging |
| Upstream/downstream end-to-end boundary | Configure-to-release happy path plus isolation, integrity, failure, correction, and no-implicit-release integration tests | Complete reviewer and participant journey at desktop and narrow widths |
