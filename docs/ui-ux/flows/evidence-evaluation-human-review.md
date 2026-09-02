# Evidence, Evaluation, and Human Review interaction specification

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Approved |
| **Owner** | Product Lead |
| **Approvers** | Product Lead, UI/UX Lead, Architecture Lead, Security/Privacy reviewer |
| **Version** | 1.0 |
| **Prepared date** | 2026-08-28 |
| **Approved date** | 2026-08-28 |
| **Approval reference** | Reconstructed and re-approved after the Shipboard production UX reset. Successor of the retired specification at Git `eb9c398`. `PROP-UI-REV-*` dispositions remain in force. |
| **Audience** | Product, design, frontend, backend, security/privacy, QA, and implementation reviewers |
| **Governs** | Assigned Review work, Evaluation processing status, exact candidate and lineage presentation, criterion and Evidence inspection, optional Human revision, Review decision, and the Result-ready/not-released handoff for the P0 assessment Campaign |
| **Journeys** | [`JRN-MVP-5`](activity-campaign-journey.md#jrn-mvp-5-produce-and-inspect-evaluation) and [`JRN-MVP-6`](activity-campaign-journey.md#jrn-mvp-6-review-and-decide) |

This approved UI/UX contract is authoritative for the governed interaction
concerns. Product meaning, observable behavior, and technical realization
remain governed by the approved product documents, feature specifications,
operational defaults, and ADRs in their respective areas of concern.

## Purpose and intended outcome

This experience begins when a terminal Session needs an Evaluation or an
immutable completed Evaluation becomes eligible for Human review. It gives an
assigned Reviewer one protected case context in which to understand the exact
Evaluation, verify its Evidence, optionally submit a bounded Human revision,
and record one explicit Review decision without changing the original
Evaluation or implying that a Result has been released.

The experience is successful when:

- an assigned Reviewer can distinguish `Awaiting evaluation`, `Queued`,
  `Running`, `Failed — retryable`, `Review required`, and
  `Evaluation completed` without seeing partial output as authoritative;
- the selected Evaluation version, replacement lineage, integrity,
  availability, rubric/procedure version, and internal/provisional status remain
  visible throughout review;
- every criterion presents its configured judgment, evaluator mode, rationale,
  confidence, uncertainty, and exact Evidence references in a consistent
  reading order;
- opening Evidence reauthorizes and resolves the exact permitted source
  location, labels lower precision or unavailability honestly, and provides a
  reliable return to the originating criterion;
- the Reviewer can approve the Evaluation unchanged or submit only a
  policy-permitted, structured Human revision that preserves the original;
- internal notes, required reasons, Participant-facing feedback, and the
  participant-facing preview remain visibly and programmatically distinct;
- rejection and escalation state their consequences and cannot create a
  releasable Result;
- stale candidates, concurrent decisions, permission loss, integrity failure,
  audit failure, and uncertain responses reconcile to authoritative state
  without overwriting history or losing recoverable local work;
- an approved decision shows the exact resulting state as **Result ready · Not
  released**, with no Release action on this surface; and
- desktop, narrow, keyboard, screen-reader, reduced-motion, and 400 percent
  zoom experiences preserve the same authority, context, and recovery meaning.

Observable Evaluation and review behavior remains governed by the approved
[Evidence and Evaluation](../../requirements/features/evidence-evaluation.md) and
[Human review and Result Release](../../requirements/features/review-result-release.md)
feature specifications.

## Authority and upstream sources

| Concern | Governing source |
| --- | --- |
| Evidence, Evaluation, Human revision, Review decision, Result, Release, isolation, and immutable-history meaning | [Concept model](../../product/concept-model.md) |
| MVP text-assessment, evidence-backed Evaluation, Human review, and explicit Release scope | [MVP scope](../../product/mvp-scope.md#mvp-validation-slice) |
| Evidence formation, processing states, criterion output, lineage, inspection, failure, privacy, and acceptance criteria | [Evidence and Evaluation](../../requirements/features/evidence-evaluation.md) |
| Review case, assignment, candidate selection, Human revision, Review decision, Result construction, privacy, and acceptance criteria | [Human review and Result Release](../../requirements/features/review-result-release.md) |
| Authentication, assignment-scoped access, complete-chain authorization, revocation, denial, and sensitive access | [Authorization and resource isolation](../../requirements/features/auth-resource-isolation.md) |
| Frozen configuration/manifest provenance, reconstruction, unavailable sources, and redaction | [Resolved Session configuration](../../requirements/features/resolved-session-configuration.md) |
| Protected-data lifecycle and application-session defaults | [MVP operational defaults](../../requirements/mvp-operational-defaults.md) |
| Platform journey, information architecture, terminology, accessibility, responsive, and protected-content baseline | [Activity journey and Campaign information architecture](activity-campaign-journey.md) |
| Evaluation locators, processing states, exact-source resolution, immutable completion, and review handoff | [Evidence and Evaluation execution contract](../../architecture/evaluation-execution-contract.md) |
| Review-case expected versions, candidate staleness, revision schema, decision/Result transaction, and separate Release boundary | [Human review, Result, and Release contract](../../architecture/review-result-release-contract.md) |
| SPA/server authority, service boundaries, and sensitive-content controls | [MVP architecture](../../architecture/mvp-architecture.md), [ADR-002](../../architecture/backend-module-architecture.md), [ADR-003](../../architecture/backend-module-architecture.md), and [ADR-009](../../architecture/mvp-architecture.md) |

## Scope and boundaries

### In scope

- Show only currently authorized Review work, bounded status counts, filters,
  ordering, assignment state, and next permitted action.
- Present terminal Sessions awaiting an eligible Evaluation and automatic
  Evaluation states through immutable completion or review-required failure.
- Present one assigned case with exact Organization/Activity/Participant,
  Enrollment, Attempt, Session, candidate Evaluation, and assignment context as
  authorized human-readable labels rather than unrestricted identifiers.
- Present candidate Evaluation version, selected state, observed lineage,
  replacement availability, integrity, source availability, frozen
  rubric/procedure, evaluator/model/configuration summary, and fairness-relevant
  facts through progressive disclosure.
- Present overall Evaluation judgment only when the frozen procedure permits it,
  plus criterion-level status, evaluator mode, configured score or decision,
  rationale, confidence, uncertainty, provisional feedback, and Evidence.
- Navigate to exact accepted Submission, terminal transcript, configuration,
  manifest, or deterministic-fact Evidence under current authorization.
- Present whole-item precision, redaction, invalid locator, integrity warning,
  lawful unavailability, and deterministic/Agent conflict honestly.
- Permit an authorized actor to explicitly resolve a stale or multi-version
  candidate under the frozen policy, with reason and preserved selection
  history.
- Prepare, validate, preserve, and submit an optional Human revision limited to
  permitted structured fields and stable Evidence references.
- Keep internal notes, Participant-facing feedback, decision reason, and
  escalation reason as separately labeled content classes.
- Preview the Participant-facing projection used for a proposed approval,
  clearly labeled as a preview that is neither approved nor released.
- Confirm and reconcile `Approve unchanged`, `Approve with Human revision`,
  `Reject Evaluation`, and `Escalate review` commands.
- Present immutable terminal Review decisions and the approved **Result ready ·
  Not released** handoff without exposing a Release control.
- Define keyboard, focus, screen-reader, reflow, content, failure/recovery,
  security/privacy, and verification behavior for these surfaces.

### Out of scope

- Creating, changing, retrying, or replacing an Evaluation. The interface may
  show only the operational next actions returned for the current actor and
  state; Evaluation rerun policy and execution remain upstream concerns.
- Creating or editing rubrics, Evaluation procedures, evaluator bindings,
  models, Agents, Harnesses, Activities, cohort baselines, Submissions,
  transcripts, Evidence sources, resolved configurations, or manifests.
- General reviewer staffing, scheduling, organization-wide case search,
  unrestricted content browsing, workload analytics, or calibration.
- Editing the original Evaluation, Evidence set, submitted Human revision, or
  committed Review decision.
- `Release Result`, audience selection, separation-of-duties confirmation,
  participant pre-release/Result views, Release reconciliation, correction
  Release, notifications, or appeal. These belong to the downstream Result and
  Release interaction specification.
- Bulk or scheduled decision/Release, anonymous or multi-reviewer consensus,
  external sharing, public links, downloadable reports, or downstream-system
  integration.
- Voice, participant-session tools, participant code, unrestricted evaluator
  execution, Dynamic memory, learning, calibration reuse, or shared Sessions.
- A universal scoring scale, confidence scale, decision threshold, or Result
  schema outside the frozen procedure and policy.
- Hidden model chain-of-thought, raw internal reasoning traces, or Reviewer
  scratch work that has not been deliberately submitted as an authoritative
  artifact.
- Shared visual tokens and implementation mechanics owned by the design-system
  foundation and frontend architecture.

## Actors and visible capability boundaries

| Actor or service | Permitted interaction | Boundary shown in the interface |
| --- | --- | --- |
| Assigned Reviewer | Open the exact active assignment; inspect the selected Evaluation and permitted sources; prepare and submit a Human revision; approve, reject, or escalate when currently authorized | Cannot browse unrelated cases, mutate original artifacts, change frozen policy, silently switch candidates, or infer Release authority from approval |
| Review-work manager or Activity administrator | Within separately delegated scope, inspect bounded case status, assign/claim/reassign where permitted, resolve operationally unresolved work, and select an eligible candidate | Activity or Organization administration alone does not expose raw Evidence, Evaluation detail, notes, or another Participant; content access and decision actions remain separate |
| Release-authorized actor | May see a safe handoff or link to separately authorized Release work after approval | This surface does not expose `Release Result`; Release authority, preview confirmation, and visibility are governed separately |
| Evaluation service | Return authoritative processing, completion, lineage, integrity, and bounded failure states | Provider progress, browser timers, partial output, queue guesses, or client polling frequency are not Evaluation authority |
| Review/release service | Return authoritative assignment, candidate, revision, decision, preview, expected version, and permitted actions | The client cannot choose ownership, audience, policy, decision authority, or participant visibility |
| Audit/compliance reviewer | Within explicit delegated scope, inspect minimized lineage, provenance, access, and decision history | Audit access is not unrestricted raw Evidence, transcript, hidden-prompt, reviewer-note, Result, or export access |
| Participant | No access to this interaction surface | Must not see Evaluation existence/status, criteria, Evidence selection, score, Reviewer identity, Review decision, preview, or timing through direct or indirect channels |

## Approved interaction decisions

The following interaction decisions were approved on 2026-08-09. Stable
`PROP-*` identifiers are retained for traceability and future supersession.

| ID | Approved decision | Rationale and consequence |
| --- | --- | --- |
| `PROP-UI-REV-1` | Use one capability-scoped **Review work** destination with separate assigned-case and authorized management views; do not create a general Evaluation or Evidence repository. | Preserves assignment isolation and the approved platform IA while supporting bounded queue management. |
| `PROP-UI-REV-2` | Keep six independent visible state tracks: access, Evaluation processing, assignment, candidate/integrity, Human revision, and Review decision/Result handoff. | Prevents a generic status from collapsing processing, authorization, lineage, local draft, decision, and Release meaning. |
| `PROP-UI-REV-3` | Use a stable case hierarchy of case header, urgent status, candidate/provenance summary, criterion navigation, active criterion, Evidence, Human revision, decision, and history. | Keeps the exact review basis and next action visible in one semantic order across viewports. |
| `PROP-UI-REV-4` | Make criterion inspection the primary review unit. Show a compact criterion list and one active criterion detail; open each Evidence source deliberately in a subordinate viewer or route with an exact return target. | Reduces cognitive load without hiding completeness, source context, or keyboard access. |
| `PROP-UI-REV-5` | Label every Evaluation and its provisional feedback **Internal Evaluation · Not a released Result** until the separate Release commits. | Makes the outcome-chain boundary explicit and reduces accidental disclosure or reviewer overstatement. |
| `PROP-UI-REV-6` | Present evaluator mode as **Rule-based**, **Agent-assisted**, or **Agent judgment** with the canonical machine value available in provenance details. | Gives Reviewers understandable provenance without implying that deterministic output is infallible or hiding the frozen mode. |
| `PROP-UI-REV-7` | Treat candidate replacement as an interrupting `Candidate changed` state. Preserve safe local work, block revision/decision submission, and require explicit retain-or-select resolution with reason. | Prevents silent review against a moving target and preserves immutable selection history. |
| `PROP-UI-REV-8` | Keep Human revision as a structured compare experience with separate changed fields, required reason, Evidence references, internal notes, and Participant-facing feedback. Submission creates a read-only artifact and does not decide the case. | Preserves the original Evaluation and prevents content-classification leakage. |
| `PROP-UI-REV-9` | Use separate deliberate actions: **Approve unchanged**, **Approve with Human revision**, **Reject Evaluation**, and **Escalate review**. Each confirmation identifies its consequence and exact candidate/revision in human-readable terms. | Keeps decision outcomes distinct and prevents an ambiguous generic submit action. |
| `PROP-UI-REV-10` | Generate the proposed Participant-facing preview with the same versioned projection used for Result construction and label it **Preview · Not approved · Not released**. | Lets the Reviewer check disclosure before approval while preventing preview/Result drift or a false visibility claim. |
| `PROP-UI-REV-11` | Reconcile lost or uncertain revision/decision responses before offering another mutation. Show **Checking review status** and never resubmit automatically. | Supports idempotency and prevents duplicate or conflicting immutable artifacts. |
| `PROP-UI-REV-12` | After `Approved`, show the immutable Review decision and exact preview as **Result ready · Not released**, then offer only a capability-resolved navigation handoff to **Release work**. | Preserves separate Release authority and keeps this surface from becoming an implicit Release path. |

## Information architecture

### Entry points

An authorized actor enters from:

- **Home** → assigned Review item requiring attention;
- **Review work** → scoped current or recent work;
- **Activities** → assessment Campaign → bounded **Review status** for an
  authorized manager;
- a terminal Session or Evaluation handoff that resolves to the permitted case;
  or
- an authorized deep link that authenticates, authorizes, and resolves the
  current assignment and candidate before protected content renders.

The browser must not render cached Participant, Evaluation, Evidence, or note
content while entry authorization is unresolved. A deep link, case identifier,
Evaluation identifier, Evidence locator, or assignment label is a locator, not
proof of access.

### Review work hierarchy

```text
Review work
├── Assigned to me
│   ├── Needs attention
│   ├── Ready for review
│   ├── In review
│   └── Recent decisions
└── Review management (only when separately authorized)
    ├── Awaiting Evaluation or assignment
    ├── Assignment and candidate exceptions
    └── Bounded operational status

Assigned case
├── Case header and current status
├── Evaluation candidate and provenance
├── Criteria
│   └── Active criterion
│       ├── Judgment, confidence, and uncertainty
│       ├── Rationale and provisional feedback
│       └── Evidence references
├── Evidence source viewer
├── Human revision
├── Participant-facing preview
├── Review decision
└── Review and lineage history
```

**Needs attention** may include candidate stale, integrity warning, source
unavailable, revision validation failure, Evaluation review required,
assignment expiry, or a recoverable uncertain command. It must not infer
urgency from inaccessible records or client time.

### Review work list

Each row or stacked record shows only metadata allowed for the current actor:

- permitted assessment Campaign and task labels;
- permitted Participant reference;
- Session terminal category without transcript content;
- Evaluation state and, only after completion, selected candidate version;
- assignment state and policy-owned review deadline when present;
- integrity/attention status in text;
- last authoritative workflow update time with named timezone; and
- one server-returned next action such as **Open review**, **Continue review**,
  **Resolve candidate**, or an authorized assignment action.

List counts, filters, ordering, facets, empty states, and pagination include
only records materialized inside the current scope. The interface must not show
the existence, count, status, Participant, or timing of inaccessible work.

While the list resolves, reserve only generic structure and label it **Loading
Review work**. An authorized empty state says **No Review work is assigned to
you** and offers only a server-permitted next action; it does not show hidden
totals, sample protected cases, or a general search control.

### Assigned case hierarchy

The persistent case header identifies:

- permitted Campaign, task, Participant, Attempt, and Session labels;
- current assignment and case state;
- **Internal Evaluation · Not a released Result**;
- exact selected Evaluation version and completion time;
- candidate lineage state: selected, replacement available, stale, superseded,
  or explicit selection required;
- integrity and Evidence availability summary;
- frozen rubric/procedure label and version; and
- current server-returned next action.

Opaque identifiers, hashes, provider request references, and raw configuration
details remain in authorized provenance views only when needed. They must not
be the primary way a Reviewer distinguishes cases.

## State model

### Independent state tracks

| Track | States shown by this surface | Governing principle |
| --- | --- | --- |
| Access | `Resolving`, `Authorized`, `Reauthentication required`, `Permission changed`, `Unavailable` | Protected content appears only after current authorization; permission loss removes content and actions |
| Evaluation processing | `Awaiting eligible Evaluation`, `Queued`, `Running`, `Evaluation completed`, `Failed — retryable`, `Review required` | Partial or invalid output never appears as a completed Evaluation; internal validating/completing stages remain under `Running` unless a bounded user-facing distinction is approved later |
| Assignment | `Unassigned`, `Assigned to you`, `Assigned to another authorized actor`, `Claim pending`, `Reassigned`, `Expired`, `Revoked` | A label or prior assignment never substitutes for current action/content authorization |
| Candidate and integrity | `No eligible candidate`, `Selected`, `Selection required`, `Replacement available`, `Candidate stale`, `Integrity warning`, `Source unavailable`, `Lawfully unavailable` | The selected immutable Evaluation never switches automatically; limitations remain visible |
| Human revision | `None`, `Local draft`, `Validation errors`, `Submitting`, `Checking submission status`, `Submitted`, `Stale draft`, `Unavailable` | Local work is not authoritative; a submitted revision is immutable and is not a decision |
| Review decision and handoff | `Not eligible`, `Ready to decide`, `Confirmation`, `Submitting`, `Checking review status`, `Approved`, `Rejected`, `Escalated`, `Result validation failed`, `Result ready · Not released` | A decision is authoritative only after commit; approval and Result construction do not imply Release |

No single spinner, banner, route, or color may collapse these tracks. For
example, a case can be `Evaluation completed`, `Assigned to you`, `Candidate
stale`, `Local draft`, and `Not eligible` for decision at the same time.

### Evaluation-to-review transition

```text
Awaiting eligible Evaluation
          │ automatic request accepted
          ▼
       Queued ──> Running ──> Evaluation completed
                      │                  │
                      ├──> Failed — retryable
                      └──> Review required
                                         │ explicit candidate selection
                                         ▼
                                 Ready for review
```

- `Queued` and `Running` describe server-owned state, not percent progress.
  Internal validating or completing stages remain represented as `Running` in
  this surface. The interface must not estimate completion time from polling or
  provider behavior.
- `Evaluation completed` appears only after the immutable Evaluation, criterion
  judgments, Evidence set, lineage, manifest provenance, review handoff, and
  required audit are authoritative.
- `Failed — retryable` and `Review required` show only a bounded reason category
  and current permitted next action. A Reviewer is not offered a retry or
  replacement action unless the server authorizes that exact action.
- A `Terminated` or `Aborted` Session remains **No automatic Evaluation** and
  follows an authorized operational path; the UI must not fabricate criteria,
  a score, or a Review decision.

### Candidate and decision transition

```text
Ready for review
      │ open exact selected candidate
      ▼
  In review ────────────────┬───────────────┐
      │                     │               │
      │ optional revision   │ reject        │ escalate
      ▼                     ▼               ▼
Revision draft         Reject confirm   Escalate confirm
      │                     │               │
Submit Human revision       ▼               ▼
      │                  Rejected        Escalated
      ▼
Revision submitted
      │
      ├── Approve with Human revision ──┐
      └── choose original only if policy permits

In review ── Approve unchanged ─────────┤
                                        ▼
                                  Decision commit
                                        │
                         construct and validate Result
                                        ▼
                              Result ready · Not released

Any editable state ── replacement/permission/concurrency change ──>
Candidate stale / Permission changed / Current decision
```

- Starting or saving a local Human revision does not change authoritative case
  state. Submitting it creates one immutable artifact but not a decision.
- `Rejected` and `Escalated` are terminal for that decision and create no
  releasable Result.
- Approval constructs and validates the Result inside the authoritative
  decision boundary. A Result validation or required-audit failure means the
  approval did not commit.
- A replacement Evaluation appends lineage and marks the case stale. It never
  changes the active criterion, Evidence, revision, preview, or candidate
  silently.

## Evaluation processing and operational states

### Awaiting, queued, and running

Before completion, show a compact status region containing:

- owning state in text;
- last authoritative update time;
- whether the user should wait or take an authorized operational action;
- a safe navigation option; and
- no criterion placeholders that resemble real judgments, scores, Evidence
  counts, or provisional feedback.

Skeletons may reserve layout only after authorization and must not contain
cached names, source lengths, rubric values, or guessed criterion counts.

For processing beyond the approved 120-second objective, use **Evaluation is
taking longer than expected** and continue to expose the authoritative state.
Do not name a provider, promise a completion time, or treat the objective as a
deadline.

### Failure and review-required states

Failure content contains:

- **Evaluation not completed** or **Evaluation needs operational review**;
- a bounded category such as source unavailable, integrity check failed,
  evaluator failed, output invalid, required audit unavailable, or processing
  unavailable;
- the consequence: no completed Evaluation and no decision can be recorded;
- the next action returned for the current actor; and
- a correlation reference only when policy permits and support needs it.

Partial provider output, invalid criteria, failed deterministic output, hidden
diagnostics, and protected excerpts must not appear as reviewable Evaluation
content.

### Completed status

Completion moves focus only when it changes the current task from waiting to
reviewable and the Reviewer has not moved elsewhere. Announce **Evaluation
completed. Review is ready.** Do not announce a score, recommendation, or
Participant content in a global notification.

## Candidate and provenance interaction

### Candidate summary

The candidate summary remains available while inspecting, revising, previewing,
or deciding. It shows:

- selected Evaluation version and authoritative completion time;
- whether it is the original or an authorized replacement;
- predecessor/successor availability and current review eligibility;
- Evaluation and Evidence-set integrity state;
- rubric/procedure version and criterion count;
- per-mode criterion counts and permitted model/evaluator summary;
- frozen configuration and manifest verification status;
- Session terminal state and transcript cutoff status;
- exact bound Submission version summary; and
- approved fairness exceptions or degraded reconstruction status when material
  and authorized.

The primary view uses understandable labels. **Technical provenance** is a
secondary disclosure with exact supported schema/version, evaluator/runtime,
digest, sequence, and verification details for authorized reviewers or
auditors. Secrets, raw prompts, private endpoints, credentials, unrestricted
knowledge content, and unrelated configuration fields are never shown.

### Replacement and stale candidate

When an eligible replacement appears:

1. keep the current Evaluation and local draft visible only while access
   remains authorized;
2. show an assertive **Candidate changed** status naming the consequence;
3. disable Human revision submission, preview approval, and all Review decision
   actions;
4. preserve the local revision and bounded reason in the current same-actor
   browser context when safe, labeling it **Draft for the prior candidate**;
5. offer **Review candidate versions** only when the actor may resolve the
   selection; and
6. require explicit selection or policy-permitted retention with a bounded
   reason before review continues.

The resolution view compares safe metadata and lineage, not two full protected
Evaluations by default. Opening either version requires current content
authorization. Selecting a different candidate resets the active criterion and
preview; it never applies the prior draft automatically. The Reviewer may copy
or adapt content only through a deliberate policy-permitted action that remains
local until resubmitted.

## Criterion and Evaluation inspection

### Evaluation summary

The summary begins with **Internal Evaluation · Not a released Result** and
presents, when defined by the frozen procedure:

- overall status, score, decision, classification, or recommendation;
- completeness and aggregation state;
- number of criteria by judgment status;
- insufficiency, not-applicable, integrity, and conflict counts;
- bounded Evaluation-level uncertainty or limitation summary; and
- provisional Participant-facing feedback, explicitly labeled **Provisional —
  visible only if approved for the Result and later released**.

Do not invent an overall value when aggregation preconditions are unmet. Do not
present qualitative confidence as a probability or convert missing Evidence
into precision.

### Criterion navigation and detail

Each criterion entry has a programmatic name containing:

- criterion label and sequence;
- judgment status;
- configured score/decision when present;
- evaluator mode;
- Evidence count;
- confidence and uncertainty summary; and
- warning, conflict, insufficiency, or unavailable-source state.

The active criterion detail uses this order:

1. criterion name, version, and configured purpose;
2. judgment status and configured score/decision;
3. evaluator mode and provenance summary;
4. confidence and uncertainty/limitations;
5. concise rationale, with observed facts and inference distinguishable;
6. Evidence references and source precision;
7. provisional feedback; and
8. available Human revision controls.

`Insufficient evidence` and permitted `Not applicable` are named states, not
empty values. Deterministic facts and Agent interpretation are visually and
programmatically distinct. A conflict shows both protected references and the
procedure-defined consequence without asking the Reviewer to treat either as
hidden policy authority. The rationale is an inspectable explanation, not
hidden chain-of-thought; raw reasoning traces and hidden prompts are never
requested or shown.

### Large criterion sets

Use semantic lists, headings, and ordinary links or buttons. Pagination or
windowing may be used only if it retains criterion position, total count inside
authorized scope, keyboard navigation, accessible names, selected state, and
return context. It must not remove content currently focused by assistive
technology without an explicit navigation outcome.

## Evidence source interaction

### Evidence reference

Each Evidence reference identifies:

- source category: Submission, transcript, deterministic fact, configuration,
  or manifest;
- exact human-readable source/version label;
- precision: exact range, stable segment, or whole item;
- current verification, integrity, redaction, and availability state; and
- the observation the rationale attributes to that source, without duplicating
  unnecessary raw content in the criterion card.

An Evidence identifier, digest, signed URL, locator, or source label does not
grant access. **Open Evidence** performs a new authorization and verification
decision for the exact target and location.

### Exact source viewer

After authorization, the source viewer:

- names source type and exact immutable version;
- identifies the cited location in source-native terms, such as lines,
  transcript item, verified text range, safe configuration field, or whole
  item;
- moves focus to the cited location or the viewer heading when only whole-item
  precision exists;
- highlights the range visually when possible but also labels it in text;
- keeps surrounding context bounded and offers deliberate expansion only when
  permitted;
- renders source content as inert untrusted text or safely sanitized content;
- exposes authorized preview/download as separate deliberate actions; and
- provides **Back to criterion** that restores the exact case, candidate,
  criterion, Evidence reference, and prior reading position.

For line and byte-range locators, the user-facing view should prefer validated
line or excerpt context and keep raw byte coordinates in provenance details.
Markdown is Evidence source text in the MVP; rendering must not change the
meaning or locator authority.

### Lower precision, invalid, and unavailable Evidence

| State | Required presentation | Decision effect |
| --- | --- | --- |
| Whole item | **Whole item cited — a finer verified location is unavailable** | Reviewer may inspect the exact item; the frozen policy determines whether decision remains eligible |
| Redacted | Name that some content is withheld under current permission or policy without exposing it | Reauthorize if capability changes; never reconstruct from another surface |
| Locator invalid | **Evidence reference could not be verified** with affected criterion | Block or limit decision according to authoritative state; do not substitute another range |
| Integrity warning | Identify the historical completion state and current appended limitation | Do not show the source as verified; preserve original citation history |
| Lawfully unavailable | **Source unavailable under the applicable lifecycle or legal restriction** | Preserve stable reference and current limitation; do not restore from logs, caches, or backups |
| Permission denied | Generic unavailable state with safe return | Remove protected content; Evaluation access alone does not widen source access |

## Human revision interaction

### Start a revision

**Create Human revision** is available only when the selected candidate,
assignment, policy, integrity, and workflow state permit it. Starting a
revision:

- freezes the displayed base candidate/version for the local draft;
- exposes only policy-allowlisted fields and ranges;
- shows the original value beside or immediately before the proposed value;
- keeps reason, Evidence references, internal notes, and Participant-facing
  feedback in separate labeled controls; and
- states **Draft only — not submitted and not a Review decision**.

The Reviewer cannot edit ownership, rubric identity, Evidence source content,
configuration, manifest, evaluator/model provenance, assignment, policy,
Result, or Release fields.

### Revision form behavior

- Each changed field identifies criterion, field label, original value,
  proposed value, and applicable range or format.
- A required reason is entered once for the revision and, when policy requires,
  per changed field. Reasons must remain separate from internal notes.
- **Add Evidence reference** offers only Evidence already authorized for the
  exact candidate and independently revalidates it on submission.
- Aggregates are read-only when deterministically derived. If policy permits an
  aggregate adjustment, all dependency and completeness rules remain visible
  and server-validated.
- Participant-facing feedback has an explicit disclosure label and a link to
  **Preview Participant view**. Internal notes appear in a separate region with
  **Internal — never included in the Result automatically**.
- Recoverable validation, connection, or dependency failure preserves safe
  local input and associates each error with the affected field and summary.
- Browser persistence beyond the current same-actor context is not assumed.
  Protected draft text must not appear in URLs, titles, notifications,
  analytics, or another actor's restored session.

### Submit and immutable state

**Submit Human revision** opens a confirmation summarizing:

- exact Evaluation candidate;
- number and categories of changes;
- required reason and Evidence-reference completeness;
- Participant-facing feedback included for possible Result projection; and
- consequence: the revision becomes immutable but does not approve, reject,
  escalate, create a visible Result, or Release anything.

While submission is pending, disable only conflicting revision and decision
actions. If the response is lost, show **Checking revision status** and
reconcile by authoritative state. A successful submission replaces the form
with a read-only structured difference and offers only currently permitted next
actions. An invalid, stale, unauthorized, or audit-failed submission creates no
authoritative revision.

## Participant-facing preview

The preview is part of review validation, not Participant visibility. Before an
approval decision it is labeled:

> **Preview · Not approved · Not released**

The preview uses the exact selected Evaluation plus the explicitly selected
submitted Human revision, if any, and the same versioned participant-facing
projector and field allowlist used for Result construction. It shows only fields
that policy permits and separately lists excluded categories in generic terms,
such as internal confidence, Evidence selection, and Reviewer notes, without
revealing their content.

If the preview is incomplete, inconsistent, unsafe, stale, or fails protected-
content validation, approval is disabled and the interface shows the affected
field category and safe correction path. The Reviewer cannot edit the preview
directly; changes occur through the Evaluation selection or Human revision
workflow.

## Review decision interaction

### Eligibility and actions

The decision region repeats the exact candidate, selected submitted Human
revision or **No Human revision**, preview status, integrity state, assignment,
and current permitted actions.

- **Approve unchanged** references the original Evaluation and no Human
  revision.
- **Approve with Human revision** references exactly one submitted Human
  revision.
- **Reject Evaluation** requires the bounded policy-owned rejection reason and
  states that no releasable Result will be created.
- **Escalate review** requires the bounded reason and permitted destination or
  state and states that no releasable Result will be created.

If a Human revision has been submitted, the interface must not silently choose
it for approval or silently discard it. The Reviewer explicitly chooses
**Approve with Human revision** or, only when policy permits, **Approve
unchanged** with the consequence stated.

### Confirm approval

The approval confirmation identifies:

- assessment, Participant, and task using permitted labels;
- exact Evaluation version;
- exact submitted Human revision or none;
- participant-facing preview version and current validation state;
- attestation or reason required by policy; and
- consequence: one immutable `Approved` Review decision and one validated
  Result will be created, but the Result will remain **Not released**.

The final action repeats **Approve unchanged** or **Approve with Human
revision**. Confirmation content must not imply Release authority or include a
`Release Result` control.

### Confirm rejection or escalation

The rejection and escalation confirmations name the exact candidate, selected
reason, consequence, and next operational state. Their final actions are
**Reject Evaluation** and **Escalate review**. Neither dialog contains a Result
preview as if one will be created.

### Pending, uncertain, and authoritative outcomes

During commit, show **Recording Review decision**. If the response becomes
uncertain, show **Checking review status** and do not offer another decision.
Reconcile the expected case version, candidate, revision, decision, and Result
state before restoring actions.

Successful outcomes are exact:

- **Review decision approved · Result ready · Not released**;
- **Review decision rejected · No Result created**; or
- **Review escalated · No Result created**.

An audit, authorization, candidate, integrity, concurrency, or Result-validation
failure must not show approval. Preserve safe local reason/input where allowed,
show current authoritative state, and direct focus to the status and next safe
action.

### Approved handoff

After approval, the case is read-only for that decision. Show:

- exact immutable Review decision and time;
- original Evaluation and optional Human revision used;
- exact validated Result preview;
- **Not released** status and explanation that the Participant cannot see it;
  and
- **Open Release work** only when the current actor separately has that
  capability.

The link enters the downstream surface and must reauthorize there. Its presence
does not prove Release authority, and its absence must not expose who may
release or when Release might occur.

## Assignment and management interaction

### Claim, assign, reassign, and relinquish

Authorized management controls show current assignee or delegated group,
content scope, capability scope, effective/expiry state, and current case
version. Each change states the consequence, requires the policy-owned reason,
and reconciles uncertain responses.

Assignment does not automatically load protected Evaluation content. A manager
without content permission sees only bounded operational metadata. Reassignment
or revocation removes decision controls immediately and protected content on the
next read; long-lived access narrows within the approved 60-second bound.

### Assignment loss while reviewing

When assignment expires or is revoked:

- stop new protected reads and mutations;
- close or replace active source viewers without retaining protected excerpts;
- remove decision and revision controls;
- retain local draft only if approved policy permits secure same-actor recovery,
  otherwise clear it safely;
- move focus to **Review access changed**; and
- offer only a safe return to **Review work**, reauthentication, or configured
  support route.

The denial does not confirm whether the case remains assigned to another actor
or reveal its current state.

## Shared content and feedback

### Required terms and labels

- Use **Review work**, **Assigned case**, **Internal Evaluation**, **Evaluation
  completed**, **Review required**, **Evaluation candidate**, **Candidate
  changed**, **Human revision**, **Review decision**, **Participant-facing
  preview**, **Result ready**, and **Not released**.
- Use **Rule-based** (`deterministic`), **Agent-assisted**
  (`agent_assisted`), and **Agent judgment** (`agent_judgment`) for evaluator
  modes.
- Use **Insufficient evidence**, **Not applicable**, **Whole item cited**,
  **Integrity warning**, and **Source unavailable** as explicit states.
- Use **Open Evidence**, **Back to criterion**, **Create Human revision**,
  **Submit Human revision**, **Approve unchanged**, **Approve with Human
  revision**, **Reject Evaluation**, **Escalate review**, **Review candidate
  versions**, and **Open Release work** for deliberate actions.
- Avoid generic **Complete**, **Approved**, **Submitted**, **Published**,
  **Available**, and **Released** without naming the owning object and
  consequence.
- Never call an Evaluation or preview a Result. Never call a submitted Human
  revision a decision. Never call an approved Result released.

### Example Reviewer copy

| Situation | Copy pattern |
| --- | --- |
| Evaluation processing | **Evaluation running. Criterion judgments are not available until completion.** |
| Evaluation delayed | **Evaluation is taking longer than expected. Its status remains authoritative; no Result has been created.** |
| Evaluation failure | **Evaluation not completed. No Review decision can be recorded.** |
| Internal Evaluation | **Internal Evaluation · Not a released Result.** |
| Whole-item Evidence | **Whole item cited. A finer verified location is unavailable.** |
| Source unavailable | **This Evidence source is unavailable under the current authorization or lifecycle policy. It has not been replaced.** |
| Candidate stale | **Candidate changed. Review the eligible versions before submitting a revision or decision.** |
| Local revision | **Draft only. This Human revision has not been submitted and does not change the Evaluation.** |
| Revision submitted | **Human revision submitted. No Review decision has been recorded.** |
| Approval confirmation | **Approval creates an immutable Review decision and Result. The Result remains unavailable to the Participant until a separate Release.** |
| Approval success | **Review decision approved. Result ready · Not released.** |
| Rejection success | **Review decision rejected. No Result was created.** |
| Escalation success | **Review escalated. No Result was created.** |
| Uncertain response | **Checking review status. Do not submit another decision.** |
| Permission loss | **Review access changed. Return to Review work or use the provided support route.** |

Production labels, times, versions, reasons, actions, and support routes come
from current authorized server state. Copy omits raw identifiers, provider
names, hashes, credentials, hidden prompts, expected answers, private endpoints,
another Participant, and unrestricted diagnostics.

## Accessibility contract

WCAG 2.2 AA is the contractual target inherited from the approved platform
journey and review requirements.

### Structure and reading order

- Use landmarks and headings for case header, urgent status, Evaluation
  summary, criteria, active criterion, Evidence, Human revision, preview,
  Review decision, and history.
- Semantic order remains case/status, candidate/provenance, criterion
  navigation, criterion detail, Evidence, revision, preview, decision, history
  even when a wide layout places regions side by side.
- Criterion navigation is a semantic list or ordinary navigation structure, not
  a grid that requires two-dimensional keyboard movement for plain text.
- Status names include the owning object. Score, confidence, uncertainty,
  Evidence precision, integrity, selection, and decision do not depend on
  color, icon, shape, position, or visual comparison alone.
- Original and proposed values in Human revision are labeled in text and
  associated with the same criterion/field context.
- Trusted system status and untrusted Evaluation/Evidence/reviewer content have
  distinct programmatic containers so content cannot spoof a notice or action.

### Keyboard and focus

- Queue filters, case navigation, criterion selection, disclosures, Evidence
  open/return, source expansion, revision fields, preview, confirmations,
  decision actions, and safe return work without drag, hover, sound, or pointer
  precision.
- Opening Evidence moves focus to the exact cited location or viewer heading;
  **Back to criterion** restores focus to the originating Evidence reference.
- Changing criteria moves focus to the active criterion heading only after a
  deliberate selection, not during background status refresh.
- Validation summaries link to the affected revision field, Evidence reference,
  reason, or preview category.
- Confirmation dialogs have accessible names/descriptions, modal semantics,
  contained focus, safe cancel/Escape behavior, and trigger-focus restoration.
- Candidate stale, permission loss, and a concurrent terminal decision move
  focus to the new authoritative status because the focused action is no longer
  available.
- Ordinary processing updates, Evidence annotations, and queue refreshes do not
  steal focus.

### Announcements

- Use polite announcements for Evaluation queued/running milestones, Evaluation
  completion, revision submission, source-view readiness, and recovered
  connection.
- Use assertive announcements for candidate stale, permission loss, integrity
  failure that blocks decision, validation summary after submit, and an
  authoritative Review decision.
- Do not announce every processing poll, queue-position change, criterion
  count, or source excerpt. Announce only a material state transition once.
- Decision announcements name the exact outcome and Result consequence without
  reading Participant content, scores, rationale, or feedback automatically.

### Forms and protected content

- Every revision control exposes criterion, field, original value label,
  allowed format/range, required state, error, and proposed value relationship.
- Disabled controls have visible and programmatic reasons; disabled styling
  alone is insufficient.
- Error summaries and inline errors do not expose inaccessible source content
  or unrestricted identifiers.
- Protected draft, rationale, Evidence, feedback, and Result preview content
  must not enter browser titles, system notifications, or accessibility labels
  outside the protected page context.

## Responsive behavior

- At narrow widths and 400 percent zoom, preserve case identity, current status,
  candidate version/integrity, one active criterion, Evidence action, revision
  status, preview status, and next decision action before secondary provenance
  and history.
- A wide list/detail layout becomes one document flow. Criterion navigation may
  collapse into a labeled selector plus previous/next controls, but criterion
  count, selected state, warnings, and direct access remain equivalent.
- Evidence opens as a full-width subordinate view or route at narrow widths;
  **Back to criterion** remains persistent without covering content or focus.
- Wide source/context panes collapse into disclosures in semantic order. No
  Evidence, rationale, original/proposed value, preview field, or decision
  consequence is omitted solely because the viewport is narrow.
- Tables for criterion summaries, structured differences, provenance, or
  history become labeled stacked records. Ordinary text requires no page-level
  horizontal scrolling.
- Long source text, code, links, hashes, and unbroken strings wrap or use bounded
  content scrolling without forcing the whole page to scroll horizontally.
- A sticky case status or decision region is permitted only when it does not
  cover focused content, errors, dialogs, browser zoom controls, or the software
  keyboard.
- Touch targets remain operable at the approved accessibility size; destructive
  or terminal actions remain visually separated from navigation.
- Respect reduced motion. No processing, integrity, selection, comparison, or
  decision meaning depends on animation.

## Security and privacy UX controls

- Authenticate and authorize every entry, list, count, filter, claim,
  assignment, case read, Evaluation read, criterion read, locator resolution,
  source preview/download, configuration/manifest disclosure, revision,
  decision, preview, history, export, and reconnection on the server.
- Scope Review work before materialization. Client filtering, hidden controls,
  route guards, cached assignments, case identifiers, signed URLs, Evidence
  digests, or prior page access do not authorize content.
- Do not render cached protected content before current access resolves. On
  revocation, actor/context change, or reassignment, remove prohibited content,
  source viewers, controls, and unsafe local drafts within the approved bound.
- Render Submission, transcript, Agent, deterministic output, rationale,
  provisional feedback, Reviewer content, and preview content as inert
  untrusted data. It cannot execute, fetch external resources, spoof trusted
  status/actions, change policy, select a candidate, authorize a tool or memory
  write, or navigate across scope.
- Do not automatically fetch submitted links, images, embeds, or external
  resources. Authorized exact-source preview/download remains deliberate and
  bound to current actor/action/artifact version.
- Keep raw Submissions, transcript, Evidence excerpts, full Evaluations, Human
  revision text, notes, Result content, prompts, expected answers, credentials,
  provider payloads, private endpoints, and unrestricted identifiers out of
  URLs, titles, notifications, analytics, logs, metrics, traces, queue metadata,
  errors, screenshots, and test artifacts.
- Visually and programmatically separate Participant-facing feedback from
  internal notes. The Result preview is deny-by-default and cannot inherit an
  internal field because of wording, placement, Markdown, or client state.
- Evidence, Evaluation, Human revision, Review decision, and preview content
  must not be offered for memory, learning, calibration, analytics training,
  cross-Participant reuse, unrelated Activities, or Harness improvement in the
  MVP.
- Denials, loading placeholders, list counts, empty states, failures, and
  notifications do not disclose inaccessible case existence, Participant
  identity, Evaluation state, criterion count, decision, score, or timing.
- In-product copy, download, print, and export controls are absent unless
  separately authorized and specified. This UI does not claim that hiding a
  control prevents operating-system or browser capture; deployments that need
  stronger endpoint controls must govern them outside this interaction spec.

## Failure and recovery matrix

| Condition | Visible state | Preserved state | Prohibited claim or action | Recovery |
| --- | --- | --- | --- | --- |
| Initial authorization unresolved | Resolving Review work | No protected UI content | No cached names, counts, criteria, or source excerpts | Resolve current identity, assignment, and scope |
| Terminal Session has no eligible Evaluation | Awaiting eligible Evaluation or No automatic Evaluation | Terminal Session reference and bounded operational state | No fabricated score, criteria, decision, or Result | Wait or follow server-returned operational action |
| Evaluation queued/running beyond objective | Evaluation taking longer than expected | Authoritative request/state | No percent, provider detail, or promised time | Continue bounded status reconciliation |
| Evaluation output partial or invalid | Evaluation not completed | Invocation history in protected service | No partial criterion presented as authoritative | Retry/review-required path outside this surface |
| Required Evaluation audit/completion fails | Evaluation not completed or Review required | Prior authoritative state | No completed Evaluation or review readiness | Service recovery and authoritative reconciliation |
| Candidate replacement arrives | Candidate changed | Current candidate, immutable lineage, safe local draft labeled for prior candidate | No silent switch, submit, preview approval, or decision | Explicit candidate resolution with reason |
| Candidate integrity warning | Review blocked or limited by policy | Historical candidate and appended finding | No verified claim or source substitution | Authorized integrity/replacement path |
| Evidence locator fails | Evidence reference could not be verified | Criterion, original locator, bounded annotation | No fallback to latest/same-name source | Apply policy-owned insufficiency or review-required path |
| Evidence source permission denied | Source unavailable | Case context only if still authorized | No excerpt, ownership detail, or reusable URL | Return to criterion or request authorized access route |
| Evidence lawfully unavailable | Source unavailable under policy | Stable reference and limitation annotation | No recovery from logs/cache/backups or erased history | Continue only if frozen policy permits |
| Revision validation fails | Human revision not submitted | Safe local field values and Evidence selections | No partial revision or implicit decision | Correct linked fields and resubmit deliberately |
| Revision response lost | Checking revision status | Draft and idempotent command context | No duplicate submission | Reconcile authoritative revision state |
| Assignment revoked while open | Review access changed | History under policy; local draft only if safe | No new source reads, revision, decision, or existence disclosure | Return to Review work, reauthenticate, or support |
| Two tabs/actors submit decisions | Current Review decision or stale conflict | One authoritative decision and local unsent input where safe | No overwrite or second decision | Reload authoritative case and follow permitted next action |
| Decision response lost | Checking review status | Command/idempotency context and prior confirmed case | No second decision | Reconcile decision and Result state |
| Required decision audit unavailable | Review decision not recorded | Prior case, safe local reason/input | No approval, rejection, escalation, or Result claim | Retry after recovery with current authorization |
| Result validation fails during approval | Result validation failed; approval not recorded | Evaluation, submitted revision, local attestation where safe | No Result-ready or approved claim | Correct through permitted revision/policy path |
| Connection lost while reading | Reconnecting; content may remain only under safe same-actor policy | Last authorized case context and local draft when safe | No current authorization, candidate, or decision claim | Reauthenticate and reconcile before actions |
| Unsupported schema or provenance version | Review unavailable | Stable protected references | No reinterpretation or weaker fallback | Upgrade supported adapter or authorized operational path |
| Content attempts script, link, notice, or control spoofing | Inert literal/sanitized content | Exact authorized source meaning | No execution, fetch, trusted-status styling, or state change | Safe rendering and security event when applicable |

## Traceability matrix

| Interaction or state | Approved acceptance criteria | Implementation surface | Verification expected after implementation |
| --- | --- | --- | --- |
| Review work list, assignment, eligibility, no-Evaluation, and exact candidate | `AC-REV-1`–`AC-REV-4`; `AC-EVAL-1`–`AC-EVAL-5`; `AC-AUTH-4`–`AC-AUTH-8`, `AC-AUTH-11`–`AC-AUTH-13` | Scoped Review work queries, status records, assignment/candidate controls, case resolver | Authorized and wrong-scope list/count/filter/deep-link; awaiting/queued/running/completed/no-Evaluation; claim/reassign/revoke; mutable alias and replacement tests |
| Evaluation criteria, rationale, confidence, insufficiency, aggregation, and provenance | `AC-EVAL-9`–`AC-EVAL-15`, `AC-EVAL-31`–`AC-EVAL-37`; `AC-RSC-18`–`AC-RSC-21` | Evaluation summary, criterion navigation/detail, provenance disclosures | Criterion completeness, mode labels, deterministic fact/Agent interpretation, conflict, insufficiency, no invented aggregate, degraded reconstruction, redaction; keyboard and narrow evidence |
| Evidence references and exact source navigation | `AC-EVAL-5`–`AC-EVAL-8`, `AC-EVAL-21`–`AC-EVAL-22`, `AC-EVAL-27`–`AC-EVAL-28`; `AC-REV-5`; `AC-AUTH-8`, `AC-AUTH-15`, `AC-AUTH-23` | Evidence list, locator resolver, source viewer, return navigation, authorized preview/download | Exact range/whole item, forged locator, wrong source/version, unavailable/redacted/integrity state, revoked assignment, focus return, 400 percent zoom, desktop/narrow evidence |
| Processing failure, retry/concurrency, completion, replacement, and stale candidate | `AC-EVAL-3`–`AC-EVAL-4`, `AC-EVAL-16`–`AC-EVAL-19`, `AC-EVAL-25`, `AC-EVAL-29`; `AC-REV-3`, `AC-REV-12`–`AC-REV-15` | Processing status, candidate banner/resolution, reconciliation, failure states | Duplicate/conflicting request, timeout, partial output, audit failure, concurrent completion, replacement arrival, local-draft preservation, explicit resolution and announcement tests |
| Human revision, content classification, and Participant-facing preview | `AC-REV-6`–`AC-REV-9`; `AC-EVAL-20`; `AC-REL-1`–`AC-REL-2` | Structured compare form, Evidence selector, internal/Participant fields, preview projector, validation and submission | Allowed/prohibited field, range/aggregation, invalid citation, note leakage, unsafe content, stale/unauthorized submit, immutable submission, no decision/Release side effect; keyboard/focus evidence |
| Approve unchanged/with revision, reject, escalate, and decision recovery | `AC-REV-6`, `AC-REV-10`–`AC-REV-15`; `AC-REL-3`–`AC-REL-4`; `AC-AUTH-18`, `AC-AUTH-20`, `AC-AUTH-22` | Decision region, confirmations, expected-version/idempotency reconciliation, terminal decision view | Happy paths, required reasons, revoked permission, concurrent/duplicate/conflicting decision, audit failure, Result validation failure, no implicit Release, dialog/focus/announcement evidence |
| Authorization, privacy, inert rendering, lifecycle, non-reuse, and reconstruction | `AC-EVAL-21`–`AC-EVAL-27`, `AC-EVAL-30`–`AC-EVAL-31`, `AC-EVAL-38`; `AC-REV-16`, `AC-REV-18`–`AC-REV-20`; `AC-REL-12`; `AC-AUTH-20`–`AC-AUTH-24` | Every surface, local draft policy, content renderer, lifecycle/unavailable states, audit/provenance views | Full wrong-scope/assignment/identifier matrix, loading leakage, prompt/control injection, log/screenshot/export leakage, lifecycle-policy failure, non-reuse and historical reconstruction tests |
| Accessibility, responsive behavior, and performance feedback | `AC-EVAL-28`–`AC-EVAL-29`; `AC-REV-17`; `AC-AUTH-20` | Queue, case workspace, criterion/Evidence flow, revision, preview, dialogs, decision status | Accessibility snapshots; keyboard-only path; screen-reader state/focus tests; reduced motion; 400 percent zoom; desktop/narrow screenshots; p95 status/acknowledgment evidence under approved bounds |

## Verification notes

This is a documentation-only change, so test-first implementation and
Playwright visual verification do not apply yet. Approval of this specification
does not constitute implementation evidence.

Before implementation is considered complete, verification must include:

- repeatable positive and negative tests mapped to every applicable row above;
- authorization tests for wrong Organization, Activity, Participant, Attempt,
  Session, assignment, candidate, source, and decision version;
- state-contract tests for every independent track and prohibited transition;
- idempotency, concurrent-action, lost-response, audit-failure, replacement,
  and permission-revocation tests;
- inert-rendering and protected-content leakage tests across UI, URLs, errors,
  notifications, telemetry, exports, screenshots, and accessibility names;
- accessibility snapshots and keyboard/focus evidence for list, criterion,
  Evidence, revision, preview, confirmation, denial, and terminal states;
- Playwright screenshots at desktop and narrow viewports, plus 400 percent zoom
  checks, for initial, loading, empty, processing, completed, populated,
  insufficiency, whole-item, unavailable, integrity, candidate-stale, revision,
  validation, confirmation, pending, approved, rejected, escalated, conflict,
  audit-failed, permission-denied, and recovery states; and
- visual review of hierarchy, copy, spacing, alignment, overflow, focus,
  contrast clues, status distinction, protected-content boundaries, and polish.

Store browser artifacts only in `.playwright-mcp/` and use synthetic data with
no real Participant or credential content.

## Open questions

None. `PROP-UI-REV-1`–`PROP-UI-REV-12` were approved on 2026-08-09.

## Downstream gaps and review needed

- The approved [Result and Release interaction specification](result-release.md)
  consumes the exact **Result ready · Not released** handoff without adding a
  Release action here or redefining the preview payload.
- The [design-system foundation](../design-system/README.md) (Approved v1.1)
  defines reusable status, criterion list, source viewer, structured difference,
  confirmation, error-summary, protected-content, and responsive
  stacked-record patterns in conformance with this specification. Visual
  presentation follows Shipboard Terminal; Review remains separate from
  Release (`PC-01`) and Human revision remains an immutable server submit
  (`PC-02`).
- Frontend and backend contracts must expose the six independent state tracks,
  exact candidate/version, source precision, permitted actions, expected
  versions, and bounded recovery categories without making the browser
  authoritative.
- QA must turn the traceability and verification sections into repeatable
  suites and Playwright evidence after a runnable implementation exists.

## Approval record

| Perspective | Status | Confirmed concern |
| --- | --- | --- |
| Product Lead | Approved | Scope, Reviewer outcome, `PROP-UI-REV-1`–`PROP-UI-REV-12`, and the no-Release boundary |
| UI/UX reviewer | Approved | Information hierarchy, criterion/Evidence flow, Human revision, content, responsive behavior, and WCAG 2.2 AA contract |
| Architecture Lead | Approved | State-track feed, candidate/version semantics, preview/Result handoff, expected-version/idempotency recovery, and server-authority boundary |
| Security/Privacy reviewer | Approved | Assignment/source authorization, protected-content handling, content classification, local-draft behavior, non-disclosure, and lifecycle states |

- Business-analysis review bounded Reviewer, management, service, Release-
  authorized actor, auditor, and Participant responsibilities; mapped happy,
  alternate, failure, concurrency, and terminal states to approved `AC-*`
  criteria; and introduced no new MVP capability.
- UI/UX review confirmed the Review work hierarchy, six independent state
  tracks, criterion-first inspection, Evidence return path, structured Human
  revision, deliberate decisions, content, accessibility, and responsive
  behavior.
- Architecture review preserved exact Evaluation candidate selection,
  immutable lineage, server-owned state, expected-version/idempotency recovery,
  Result projection, and the separate Release boundary approved by ADR-009.
- Security/privacy review preserved current assignment and complete-chain
  authorization, exact-source reauthorization, inert rendering, protected
  content classification, safe local-draft limits, lifecycle truth, and
  disabled learning/reuse.
- Traceability review covers `AC-EVAL-1`–`AC-EVAL-38`, `AC-REV-1`–`AC-REV-20`,
  and the applicable `AC-REL-*`, `AC-AUTH-*`, and `AC-RSC-*` criteria.
  Implementation and verification evidence remain open.

## Related documents

- [UI/UX documentation](../README.md)
- [Activity journey and Campaign information architecture](activity-campaign-journey.md)
- [Text Session interaction specification](text-session.md)
- [Evidence and Evaluation](../../requirements/features/evidence-evaluation.md)
- [Human review and Result Release](../../requirements/features/review-result-release.md)
- [Evidence and Evaluation execution contract](../../architecture/evaluation-execution-contract.md)
- [Human review, Result, and Release contract](../../architecture/review-result-release-contract.md)
