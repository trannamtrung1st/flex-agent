# Feature: Assessment setup

## Status and source

- Status: Approved
- Owner: Product Lead
- Approvers: Product Lead, Architecture Lead, UI/UX reviewer, Security/Privacy reviewer
- Approved date: 2026-08-06
- Source: [Activity](../../product/concept-model.md#activity), [Effective configuration resolution](../../product/concept-model.md#effective-configuration-resolution), [Assessment fairness constraints](../../product/concept-model.md#assessment-fairness-constraints), [Group and cohort semantics](../../product/concept-model.md#group-and-cohort-semantics), [MVP validation slice](../../product/mvp-scope.md#mvp-validation-slice), and [MVP executable workflow](../../product/mvp-scope.md#mvp-executable-workflow)
- Catalog entry: P0 #3 — [P0 authoring order](../README.md#p0-authoring-order)
- Related requirements: Consumes the approved authorization and isolation contract in [`auth-resource-isolation.md`](auth-resource-isolation.md) and produces the cohort activation baseline consumed by [`resolved-session-configuration.md`](resolved-session-configuration.md).
- Related decisions: Approved defaults `PROP-1`–`PROP-7` in this specification. Proposed `PROP-8` records the interim empty-knowledge warning. [ADR-001](../../architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md) governs compatible downstream resolved-configuration representation and integrity. [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md) and [ADR-003](../../architecture/decisions/ADR-003-authorization-audit-persistence.md) govern authorization enforcement and durable audit. [ADR-004](../../architecture/decisions/ADR-004-assessment-activation-baseline-and-atomicity.md) governs activation-baseline representation, content digests, binding, idempotency, and atomic activation. Approved [ADR-017](../../architecture/decisions/ADR-017-assessment-source-authority-and-activation-transaction.md) governs source ownership and activation-transaction participation.
- Decision approval: `PROP-7` and ADR-017 were approved on 2026-08-21 by the Product Lead, Architecture Lead, and Security/Privacy reviewer. Production remains fail-closed for any required category without exact transaction-aware authority.

This approved specification is authoritative for assessment activity setup and
cohort activation behavior. Implementation is partial: domain, PostgreSQL,
HTTP, and React evidence exists, while authenticated browser, performance,
aggregate release-gate, and final independent-review evidence remain open.

## Problem and measurable outcome

The assessment MVP needs an administrator to configure a campaign activity before participants are enrolled or sessions begin. The setup must bind an existing agent and harness to assessment-specific parameters without turning the activity into a second agent or harness authoring surface.

Assessment setup is also the fairness boundary. An editable draft may refer to mutable or incomplete sources while it is being prepared, but an activated cohort must use one immutable baseline that identifies the exact agent, harness, task, rubric, model, knowledge, workflow, capability, memory, timing, attempt, and review configuration intended for comparable participant sessions. A display name, `current` alias, or mutable reference is not an adequate activation record.

Without this boundary, participants in the same cohort could receive materially different instructions, model behavior, knowledge, memory, follow-up rules, or evaluation criteria. A later edit could also change an active assessment silently or make the resulting sessions impossible to explain.

The measurable outcome is:

- An authorized administrator can create and save an assessment activity draft using existing organization-owned sources and assessment-required parameters.
- Every successful activation validates 100 percent of the required source categories; readiness reports every safely disclosable blocking category without exposing secrets.
- Every activated cohort has exactly one immutable activation baseline with a stable identifier, versioned schema, content digest, source provenance, actor, and UTC activation time.
- No cohort becomes available for enrollment or session start until the baseline and its required audit event commit successfully.
- Every baseline records Stable memory with approved-memory reads disabled or pinned to one immutable memory snapshot.
- Every material post-activation change creates a new activity revision and activation baseline for a new cohort; it never changes an activated cohort silently.
- Duplicate or concurrent activation requests create one authoritative baseline and do not produce conflicting cohort state.
- Downstream session resolution can verify the baseline identifier, digest, ownership, and frozen source references required by `REQ-RSC-9`–`REQ-RSC-14`.
- Automated verification covers successful setup, incomplete and invalid configuration, cross-organization substitution, capability widening, stale sources, concurrent activation, audit failure, immutability, and accessible recovery.

## Actors and permissions

All permissions are action- and resource-scoped under [`auth-resource-isolation.md`](auth-resource-isolation.md). A role label, organization membership, page access, or possession of an activity, cohort, source, or baseline identifier is not proof of permission.

| Actor | Permitted actions and scope | Explicit restrictions |
| --- | --- | --- |
| Activity administrator | Within delegated activity scope, create an assessment activity draft; select permitted existing sources; set assessment-required parameters; run readiness checks; inspect differences; and activate a cohort | Cannot create or edit reusable agent, harness, rubric, model, knowledge, or memory sources; cross organizations; widen upper-scope capabilities; or mutate an activated baseline |
| Organization administrator | Within delegated organization and action scope, manage permitted defaults, inspect source eligibility and activation history, and perform separately authorized setup actions | Organization membership alone does not grant sensitive-content access, unrestricted source selection, or an activation bypass |
| Reviewer | Within an active assignment, inspect the activation-baseline summary and provenance needed to review fairness and a resulting session | Cannot configure or activate a cohort solely through reviewer assignment; cannot inspect unrelated cohorts, hidden secrets, or another organization's sources |
| Participant | No assessment-setup mutation permission; later receives only participant-visible instructions and operational facts through the owning enrollment and session features | Cannot list drafts, select sources, inspect hidden prompts or rubrics, activate cohorts, or access another participant's or cohort's configuration |
| Assessment activation service | Under explicit service identity and bounded delegation, validate authoritative sources, freeze the baseline, bind it to the cohort, and record audit events | Cannot trust client-supplied ownership or authorization values, combine cross-scope sources, silently choose newer sources, or activate after a failed validation or audit boundary |
| Audit or compliance reviewer | Within explicitly delegated scope, inspect activation attempts, changes, source references, digests, exceptions, and audit history | Cannot use audit access as a route to raw participant content, secrets, or unrestricted configuration |

## Scope

### In scope

- Create a campaign-form assessment activity draft within one organization.
- Select an existing agent revision and harness revision or immutable harness snapshot, or apply pre-provisioned assessment defaults that resolve to exact revisions before activation.
- Define exactly one versioned assessment task for the MVP and bind existing versioned submission requirements, rubric/evaluation procedure, text workflow, adaptive-follow-up policy, and human-review/release requirements.
- Configure assessment timing, campaign start/end boundaries, deadlines, per-attempt duration where applicable, attempt limit, and cohort rules.
- Resolve inherited model, knowledge, capability, policy, and evaluation inputs for readiness display without editing their reusable sources.
- Configure Stable memory with either approved-memory reads disabled or one immutable memory snapshot.
- Validate organization ownership, authorization, compatibility, completeness, immutable identity, content digests, upper-scope constraints, and MVP capability restrictions.
- Activate a cohort by atomically creating and binding one immutable activation baseline and recording the required audit event.
- Preserve draft revisions, activation attempts, successful baselines, failures, approved exceptions, and later superseding relationships without silently overwriting history.
- Permit participant enrollment after activation under the unchanged cohort baseline, consistent with the MVP executable workflow.
- Provide authorized readiness, confirmation, success, failure, history, and baseline-inspection states.

### Out of scope

- Creating, editing, publishing, comparing, restoring, or generally administering reusable agents or harnesses; those concerns belong to P1 or P2 specifications.
- Authoring general-purpose workflows, tools, model deployments, knowledge libraries, rubrics, evaluation procedures, or memory snapshots.
- Participant identity management, enrollment state, invitation delivery, attempt authorization or consumption, participant-specific deadline enforcement, and submission versioning; these belong to [`submission-attempts.md`](submission-attempts.md).
- Session start, text conversation, timers during a session, pause/resume, completion, failure recovery, or administrative session termination; these belong to [`session-text-lifecycle.md`](session-text-lifecycle.md).
- Resolved session configuration creation and runtime manifest recording, which belong to [`resolved-session-configuration.md`](resolved-session-configuration.md).
- Evidence collection, evaluation generation, human revision, review decision, result construction, or release.
- Dynamic memory, learning from assessment interactions, memory-candidate approval, or cross-participant learning.
- Voice interaction, tool execution, shared multi-participant sessions, direct activities, embedded activities, and API-triggered activities.
- General organization management, billing, proctoring, cheating detection, identity verification, or complex scheduling.
- Product-wide retention durations, deletion schedules, legal holds, consent wording, or accommodation policy.
- Selecting a database, transaction mechanism, canonicalization library, digest procedure, queue, policy engine, or UI framework.

## User journeys and state transitions

### Assessment activity and cohort setup lifecycle

The activity draft and cohort activation baseline are distinct. An activity draft may be revised; an activated cohort baseline may not.

```text
Activity absent
    │ authorized create
    ▼
Draft activity revision
    │ save revisions / correct validation issues
    ├──────────────────────────────────────────────┐
    │                                              │
    │ create cohort                                │
    ▼                                              │
Draft cohort ── authorized activation request ──► Validating
                                                     ├── failure ──► Activation failed
                                                     │                  │ correction creates or selects
                                                     │                  │ a new draft revision
                                                     │                  └──────────────► Draft cohort
                                                     │
                                                     └── atomic baseline + audit commit
                                                                          ▼
                                                                       Activated
                                                                          │ material change requested
                                                                          ▼
                                                           New activity revision and cohort
```

`Activated` is terminal for that cohort baseline. A later administrative status may prevent new enrollments or starts under an owning feature, but it does not unfreeze, replace, or rewrite the baseline.

### Activity administrator creates a draft

1. The administrator starts a campaign assessment within delegated organization scope.
2. The system creates an organization-owned draft activity revision from permitted explicit selections or pre-provisioned defaults.
3. The administrator selects an exact agent revision and harness revision or immutable snapshot and supplies the assessment-required parameters.
4. The system validates fields and source eligibility as the draft is saved without treating client-provided scope as authoritative.
5. Each successful save creates or records an inspectable revision when audit-relevant configuration changed.
6. The administrator may leave and resume the latest permitted draft without losing successfully saved values.

### Administrator reviews readiness

1. The administrator opens the cohort readiness view.
2. The service resolves the candidate baseline from authoritative organization, agent, harness, activity, and cohort sources.
3. The view shows a readable summary of the candidate task, timing, attempts, agent/harness, model, knowledge, text workflow, rubric, review gate, memory state, disabled capabilities, and source-version status.
4. Blocking errors and warnings are separated and grouped by actionable category.
5. The administrator corrects the activity draft or owning reusable source through its authorized workflow.
6. Passing readiness is informative; activation repeats authoritative validation at the commit boundary.

### Administrator activates a cohort

1. The administrator requests activation and receives a summary of the configuration and consequences.
2. The administrator confirms activation through an accessible deliberate action.
3. The service reauthorizes the actor and reloads the authoritative draft revision and every required source.
4. The service verifies source ownership, immutable identity, content digests, compatibility, Stable memory behavior, MVP-disabled capabilities, and upper-scope constraints.
5. The service creates the activation baseline identifier, digest, provenance, and audit event and binds the baseline to the cohort in one atomic boundary.
6. The cohort becomes `Activated` only after the commit succeeds.
7. The administrator receives the activated summary and the next permitted action, such as assigning participants.

### Activation fails safely

1. Validation detects an incomplete field, stale draft, missing revision, digest mismatch, cross-scope source, incompatible binding, capability widening, invalid memory state, authorization change, timeout, or persistence/audit failure.
2. No baseline is marked active and no enrollment or session may use the failed candidate.
3. The prior draft remains available when authorization and privacy permit.
4. The administrator receives an error summary with affected categories and recovery actions but no secrets or unrelated source details.
5. A non-sensitive failed-attempt record and required audit event remain inspectable.
6. A retry reauthorizes and revalidates current authoritative state rather than reusing a stale readiness result.

### Participant is assigned after activation

1. A cohort may be activated before it contains participants, matching the MVP sequence of setup followed by assignment.
2. The enrollment feature adds an authorized participant under the activated cohort and its eligibility rules.
3. Adding or removing a participant does not modify the baseline or expose other cohort members.
4. Every later session start verifies and consumes the same baseline through [`resolved-session-configuration.md`](resolved-session-configuration.md).

### Material change is requested after activation

1. An administrator opens an activated cohort and requests a change to a fairness-sensitive value.
2. The system explains that the activated baseline is immutable and identifies the affected category.
3. The system offers a permitted path to create a new activity revision and cohort candidate without changing the original.
4. The administrator reviews the differences and separately activates the new cohort.
5. Existing enrollments, sessions, evidence, and outcomes remain linked to their original cohort and baseline.

### Duplicate or concurrent activation requests

1. Equivalent or conflicting requests target the same draft cohort.
2. The activation boundary uses trusted draft revision and idempotency/uniqueness state.
3. Exactly one authoritative baseline may bind to the cohort.
4. Equivalent retries return that result; stale or conflicting requests receive a conflict state and current summary.
5. No request silently replaces the winning baseline or combines values from different draft revisions.

### Prohibited transitions

- Activity absent directly to an activated cohort without an organization-owned activity revision and required source validation.
- Draft or `Activation failed` to participant enrollment availability or session start without successful activation.
- `Validating` to `Activated` after authorization, source verification, baseline persistence, cohort binding, or required audit acceptance fails.
- `Activated` to a changed baseline, mutable source reference, or different digest.
- An activity or cohort value to widen a capability or weaken a boundary established by organization, agent, or harness scope.
- A Stable assessment to Dynamic memory writes or unpinned changing approved-memory reads.
- A selected source from another organization or unrelated activity to become part of the baseline.
- A participant, reviewer-only assignment, or unbounded service identity to configure or activate a cohort.
- A stale readiness result or stale browser form to overwrite a newer draft revision or activated state.
- Cohort membership to shared transcript, shared working context, or participant-to-participant data visibility.

## Business rules

### Draft creation and assessment parameters

- `REQ-ACT-1` — An assessment activity must be created as a campaign-form activity owned by exactly one organization and must remain unavailable for enrollment or session start until the applicable cohort is activated.
- `REQ-ACT-2` — Creating, reading, listing, updating, validating, or activating an activity or cohort must enforce the actor's current delegated organization, action, and resource scope through the approved authorization contract.
- `REQ-ACT-3` — The setup surface must select existing organization-owned source revisions or permitted pre-provisioned defaults; it must not edit reusable agent, harness, rubric, workflow, model, knowledge, or memory-source content.
- `REQ-ACT-4` — A selected agent and harness must be compatible under their approved capability, instruction, workflow, evaluation, memory, and organization-policy boundaries before activation.
- `REQ-ACT-5` — A saved assessment draft must identify its activity revision and contain exactly one versioned task binding for the MVP plus submission requirements, rubric/evaluation-procedure binding, cohort-level timing rules and deadlines, attempt limit, cohort rules, text workflow, adaptive-follow-up policy, human-review/release requirements, and memory-read choice.
- `REQ-ACT-6` — Assessment parameters may narrow or supply values within an upper-layer schema but must not widen any organization, agent, or harness capability, permission, workflow, memory, tool, privacy, retention, evaluation, or review boundary.
- `REQ-ACT-7` — Audit-relevant saved changes must create inspectable revision history or equivalent append-only change history with actor, previous revision, new revision, reason when required, and UTC time; a save must not silently rewrite a previously activated or otherwise referenced revision.
- `REQ-ACT-8` — The system must detect a stale draft revision at save or activation and must not silently overwrite newer saved work or an activated cohort.

### Readiness and source validation

- `REQ-ACT-9` — Readiness validation must use authoritative server-side ownership and relationship data and must not trust client-supplied organization, activity, cohort, role, source-version, digest, or authorization values.
- `REQ-ACT-10` — Before activation, the system must validate every required source for presence, organization ownership, permitted status, immutable version identity, verified digest, compatibility, and availability under the selected activity and cohort.
- `REQ-ACT-11` — A mutable-only `current`, `latest`, display-name, or alias reference must be resolved to and confirmed as an exact immutable version or verified content digest before activation.
- `REQ-ACT-12` — Readiness results must distinguish blocking errors from warnings and must identify the affected configuration category and safe recovery action without exposing secrets, hidden prompts, raw protected content, or cross-scope identifiers.
- `REQ-ACT-13` — A passing readiness check must not reserve authorization or source state; activation must reauthorize the actor and revalidate every required source and constraint at the authoritative commit boundary.

### Cohort activation and fairness baseline

- `REQ-ACT-14` — Each activated assessment cohort must bind to exactly one immutable activation baseline with a stable baseline identifier and digest; two cohorts may have equivalent digests but must retain distinct ownership and activation identities.
- `REQ-ACT-15` — Activation is a `required_durable` audited mutation and must atomically create the baseline, bind it to the cohort, transition the cohort to `Activated`, and durably accept the required audit event, or leave the cohort unactivated.
- `REQ-ACT-16` — The baseline must record the exact organization-policy revision, agent revision, harness revision or snapshot, activity revision, task/submission-requirement revision, model deployment identity, knowledge-source versions or hashes, capability set, text workflow/policy version, rubric/evaluation-procedure version, memory policy, adaptive-follow-up policy, cohort-level timing/deadline/attempt rules and bounds, human-review/release requirements, and approved exception references that govern the cohort.
- `REQ-ACT-17` — The baseline must classify each recorded value as inherited, activity-supplied, cohort-supplied, derived, most-restrictive resolution, or approved exception so an authorized reviewer can explain its provenance.
- `REQ-ACT-18` — The baseline must record its organization, activity, cohort, schema version, the `activation-baseline-jcs-sha256-v1` procedure identifier, source-reference set, effective candidate values, resolution decisions, actor, UTC activation timestamp, and content digest in accordance with ADR-004.
- `REQ-ACT-19` — Cohort activation must be idempotent under equivalent retries and safe under concurrent requests; only one authoritative baseline may bind to a cohort.
- `REQ-ACT-20` — A failed activation must not make the cohort available for enrollment or session start and must preserve a non-sensitive attempt record with stable reason category, correlation reference, authoritative draft revision, and unambiguous timestamps.
- `REQ-ACT-21` — An activated baseline must be immutable. An annotation, later verification finding, closure state, or superseding relationship must be appended separately and must not rewrite the original baseline or activation history.
- `REQ-ACT-22` — Later changes to an organization policy, agent, harness, model alias, knowledge source, workflow, rubric, memory source, or activity draft must not change the identifier, digest, content, or historical interpretation of an activated baseline.
- `REQ-ACT-23` — A post-activation change to any fairness-governed cohort rule or bound listed in `REQ-ACT-16` is material and must create a new activity revision and cohort activation baseline. Participant identity, enrollment status, attempt number, actual permitted session timing window, and an authorized participant-specific accommodation are handled through their owning rules rather than silently reclassifying the cohort baseline. Existing participants, sessions, evidence, evaluations, and results must remain linked to their original cohort and baseline.
- `REQ-ACT-24` — An activated baseline must be consumable and verifiable by the session resolver using its identifier, digest, ownership chain, immutable source references, and the fairness classifications required by `REQ-RSC-9`–`REQ-RSC-14`.
- `REQ-ACT-25` — A cohort may be activated without participants, and later authorized enrollment changes must not mutate its baseline; participant identity, enrollment status, attempt number, participant-specific accommodation, and actual permitted session timing window are session-bound or enrollment-owned values rather than fairness-baseline content.

### Stable memory and MVP capability restrictions

- `REQ-ACT-26` — Every assessment baseline must set memory mode to Stable and must disable new persistent learning and cross-participant learning from assessment interactions.
- `REQ-ACT-27` — A new assessment draft must default to Stable memory with `approved_memory_reads = disabled`. An authorized administrator may explicitly select one immutable organization-owned memory-snapshot identifier and digest before activation; the activated baseline must represent exactly one of those two states.
- `REQ-ACT-28` — When approved-memory reads use a snapshot, the baseline must record its eligible retrieval scope and must prohibit retrieval from sources outside the snapshot.
- `REQ-ACT-29` — The MVP baseline must explicitly enable text interaction and disable voice interaction, tool execution with an empty permitted tool set, Dynamic memory writes, shared-session behavior, and direct, embedded, or API deployment behavior.
- `REQ-ACT-30` — Adaptive follow-up must bind to a versioned fairness policy that constrains permitted variation; the setup must not enable unconstrained participant-specific adaptation.

### Timing, attempts, and cohort administration

- `REQ-ACT-31` — Activity and cohort start/end times, deadlines, permitted session-window rules, and other recorded wall-clock boundaries must use UTC for persistence and must include the named timezone used for administrator and participant interpretation. The baseline freezes the cohort rules and bounds, not a participant's later calculated or accommodated window.
- `REQ-ACT-32` — The attempt limit and any configured per-attempt duration must be positive bounded values accepted by the applicable upper-scope policy. Attempt authorization, consumption, retry entitlement, actual permitted session timing, and participant-specific accommodations belong to [`submission-attempts.md`](submission-attempts.md). An accommodation must be explicitly authorized, reason-coded, bounded by organization policy, linked immutably to the enrollment and original cohort baseline, and visible as a fairness-relevant difference to authorized reviewers; assessment setup must not provide a hidden ad hoc override.
- `REQ-ACT-33` — Adding, removing, or reassigning a participant must follow the enrollment feature and must not expose one participant to another participant's identity or protected records solely through cohort membership.
- `REQ-ACT-34` — A setup view may display aggregate cohort administration facts only when the actor is authorized for them; participant lists, counts, search, and exports must use the same scoped-query rules as direct reads.

### Audit, inspection, and lifecycle

- `REQ-ACT-35` — Activity creation, audit-relevant draft changes, activation request, activation success or failure, approved exception use, stale/conflicting request, baseline verification failure, and superseding-baseline creation must produce audit events appropriate to their sensitivity.
- `REQ-ACT-36` — Cohort activation is classified as `required_durable`. Activation and any exception or other mutation classified by approved policy as requiring durable audit must fail without the protected state transition when the audit event cannot be durably accepted, consistent with `REQ-AUTH-31` and ADR-003.
- `REQ-ACT-37` — Activation and audit records must use stable protected references and bounded metadata rather than copying raw participant data, hidden prompts, knowledge content, rubric content, credentials, secrets, or unrestricted source payloads.
- `REQ-ACT-38` — Authorized administrators and assigned reviewers must be able to inspect a readable baseline summary, source versions, digest status, memory state, disabled capabilities, resolution decisions, exceptions, and superseding relationship without receiving secrets or unrelated data.
- `REQ-ACT-39` — Participants must not receive activity drafts, hidden setup values, activation internals, rubric/evaluation internals, source digests, or baseline history; participant-visible instructions and operational facts are governed by the enrollment and session features.
- `REQ-ACT-40` — Activity, cohort, baseline, and activation-attempt records must follow the applicable approved retention, deletion, legal-hold, and export policy; this feature must not invent an independent duration or fabricate verification after a source is lawfully unavailable.
- `REQ-ACT-41` — One currently authorized Activity administrator may activate a ready cohort after an explicit deliberate confirmation; routine activation does not require a second-person approval.
- `REQ-ACT-42` — An exception to an upper-scope or fairness rule may be used only when a separately approved rule permits it and the exception has a current authorized actor, explicit reason, bounded scope, additional authorized approval, UTC timestamp, immutable reference, and required durable audit. An exception must never widen a non-bypassable organization boundary.

## Data, evidence, and audit

### Logical records

The following are logical product records. Architecture may choose physical storage only if it preserves their ownership, immutability, authorization, audit, and lifecycle semantics.

| Record | Purpose | Minimum content |
| --- | --- | --- |
| Assessment activity | Own the campaign-form assessment and its organization boundary | Activity ID, organization, type/profile, status, current permitted draft revision, creation actor/time |
| Activity revision | Preserve one saved setup state | Revision ID, parent activity, selected source references, assessment parameters, prior revision, author, UTC save time, change reason when required |
| Cohort | Own the administrative group and activation state | Cohort ID, organization/activity ownership, draft/validating/activated state, rules, bound baseline ID when activated |
| Activation attempt | Preserve each activation outcome | Attempt/idempotency ID, actor or service, trusted activity/cohort/draft scope, start/end times, outcome, stable errors, correlation ID |
| Cohort activation baseline | Freeze the fairness-governed candidate configuration | Baseline ID/digest, schema/procedure versions, ownership, activity revision, exact source references/digests, effective candidate values, classifications, decisions, exception references, actor, activation time |
| Baseline annotation or superseding link | Add later explanation without mutation | Author, reason, time, baseline reference, annotation type, optional new activity revision/cohort/baseline reference |
| Audit event | Preserve security and governance history | Event ID/schema, actor/service, organization, action, resource and baseline references, decision/result, reason, UTC time/ordering, correlation, delegation/exception reference |

### Required baseline content

The activation baseline contains, when applicable:

- Organization-policy revision and digest.
- Agent identifier, exact revision, and digest.
- Harness identifier plus exact revision or immutable snapshot and digest.
- Activity identifier and revision; campaign and cohort identifiers.
- The single MVP task revision and its submission-requirement revision.
- Activity/cohort timing rules and bounds, deadlines, attempt limit, and cohort rules; not participant-specific calculated timing windows.
- Text workflow, adaptive-follow-up policy, and completion-policy revisions.
- Rubric, evaluation procedure, evidence requirements, and human-review/release requirements.
- Model provider, model identifier, deployment version or approved immutable fingerprint, adapter version, and relevant generation parameters inherited or permitted for the assessment.
- Knowledge-source identifiers, versions, and content hashes.
- Enabled and disabled capability sets, including the explicit empty MVP tool set.
- Stable memory mode, read/write policy, no-read state or memory-snapshot identifier/digest, and eligible retrieval scope.
- Resolution classifications, most-restrictive decisions, and approved exception references.
- Baseline schema, normalization/canonicalization, and digest-procedure versions.
- Activation actor, service delegation when applicable, UTC timestamp, correlation reference, stable baseline identifier, and digest.

Participant rosters, submissions, transcripts, evidence, evaluations, results, raw prompts, raw knowledge content, and secret values are not copied into the baseline. They remain in their owning protected records.

### Required audit events

At minimum, record:

- Assessment activity created.
- Audit-relevant activity revision saved.
- Source selection or assessment parameter changed when policy requires audit.
- Readiness validation requested and blocking validation failure detected.
- Cohort activation requested, succeeded, failed, deduplicated, or rejected as stale/conflicting.
- Cross-organization source substitution, unauthorized capability widening, mutable-only source, digest mismatch, or invalid memory state rejected.
- Approved exception applied or rejected.
- Cohort bound to its activation baseline.
- Baseline integrity verification succeeded or failed when verification is performed.
- New revision/cohort/baseline created because of a post-activation material change.
- Authorized baseline inspection or export when required by policy.

Events preserve an unambiguous UTC order and correlation across the activity revision, activation attempt, baseline creation, cohort transition, and downstream session resolution.

### Evidence and fairness inspection

The baseline is evidence about the conditions offered to a cohort. An authorized fairness or outcome review should be able to answer:

- Which exact activity revision and cohort were activated?
- Which agent, harness, model, knowledge, workflow, rubric, memory, timing, and attempt sources were frozen?
- Which values were inherited, supplied, narrowed, derived, or approved as exceptions?
- Were approved-memory reads disabled or pinned to an immutable snapshot?
- Were voice, tools, Dynamic writes, and shared sessions explicitly disabled?
- Did activation revalidate source ownership, hashes, authorization, and compatibility?
- Which actor or service activated the cohort, when, and under which delegation?
- Does the baseline digest still verify?
- Was a later cohort created for a material change while the original remained unchanged?

## Quality requirements

### UX and accessibility

- The setup experience must distinguish activity draft status, cohort activation status, readiness result, blocking errors, warnings, and activated state in text and structure rather than color alone.
- The primary next action must be clear for empty, incomplete, ready, validating, failed, stale, permission-denied, and activated states.
- Source selectors must show the permitted item name, exact revision or snapshot, status, and organization context needed to avoid accidental selection; unavailable and empty states must explain the safe next action.
- The activation confirmation must summarize material frozen values and state that future material changes require a new revision and cohort.
- Activating, saving, validation, success, and error states must expose accessible status announcements and must not trap keyboard focus.
- Validation summaries must move focus appropriately, link to the affected field or category, preserve recoverable draft input, and avoid exposing unauthorized details.
- A stale-edit or concurrent-activation conflict must preserve recoverable local input and show the current authoritative state before another action is attempted.
- Baseline summaries and comparison views must provide meaningful headings, linear reading order, selectable/copyable permitted identifiers, and a compact layout at narrow widths.
- Every control must have an accessible name, instructions and errors must be programmatically associated, and activation must be operable without pointer, hover, drag, sound, or motion.
- The interface must support reflow at 400 percent zoom and narrow viewports without hiding the activation consequence, error recovery, or current status; WCAG 2.2 AA is the contractual target under the approved [assessment Campaign setup interaction specification](../../ui-ux/assessment-campaign-setup.md).
- Loading states must not render stale or unauthorized source details, participant data, hidden prompts, or secrets.

### Performance and reliability

- Draft saves, readiness checks, and activation commands must be idempotent where retries are possible and must use bounded reads within one trusted organization/activity/cohort chain.
- Readiness may cache immutable source metadata only with organization-, version-, and digest-aware keys; activation must revalidate authoritative state at commit.
- Partial persistence must not expose a cohort as activated. Recovery must identify and safely complete or reject an incomplete pre-activation operation without producing a second baseline.
- Activation must fail closed when authorization, policy, source-version, digest, audit, or persistence dependencies are missing, unavailable, inconsistent, or timed out.
- Concurrency controls must prevent lost draft updates and competing authoritative baselines.
- Activation failures must preserve the last successfully saved authorized draft and provide a safe retry after correction.
- With required immutable source metadata available inside the platform boundary, readiness and activation must each complete in no more than 2 seconds at the 95th percentile, excluding authentication-provider redirects and end-user network latency. Representative load testing must verify the objective.

### Security and privacy

- Every draft, source, cohort, activation attempt, baseline, annotation, list, count, export, and audit operation must enforce server-side organization, action, resource, and relationship authorization.
- The service must derive organization and parent ownership from trusted state and reject forged or mismatched activity, cohort, source, revision, digest, role, or actor fields.
- Source selection and activation must prevent cross-organization reference substitution, confused-deputy service execution, stale delegation, capability widening, mutable-alias substitution, digest confusion, and duplicate-activation races.
- A model, knowledge source, rubric, workflow, or memory snapshot must not be selectable merely because its identifier is known; ownership, status, scope, and compatibility must be authorized and verified.
- Secrets, credentials, tokens, private endpoints, raw hidden prompts, raw knowledge content, and unnecessary participant data must not appear in drafts, baselines, audit events, logs, metrics, traces, screenshots, or error responses.
- Secret use must be represented by an authorized binding or reference resolved only at its permitted runtime boundary.
- A matching baseline digest proves neither authorization nor permission to disclose, retain, restore, or reuse the content.
- Metrics and traces must use bounded labels and stable non-sensitive reason categories without raw protected content or unrestricted identifiers.
- Negative tests must cover wrong organization, wrong activity/cohort, unauthorized role/action, guessed ID, forged parent, stale revision, revoked delegation, mutable alias, cross-scope snapshot, upper-scope widening, Dynamic memory, unpinned reads, audit failure, retry, and concurrency.

## Acceptance criteria

### `AC-ACT-1` — Authorized administrator creates an assessment draft

- **Given** an authenticated administrator has active delegated assessment-creation scope in an organization
- **When** the administrator creates a campaign assessment
- **Then** the system creates an organization-owned draft activity and initial revision
- **And** the cohort is not available for enrollment or session start before activation
- **And** the creation is audited as required.

### `AC-ACT-2` — Setup uses exact permitted source revisions

- **Given** permitted agent, harness, rubric, workflow, model, knowledge, and memory sources exist
- **When** the administrator selects or applies them to a draft
- **Then** the draft records exact revisions or immutable snapshot references where available
- **And** any mutable alias is resolved to an exact version and shown before activation
- **And** no reusable source content is edited through assessment setup.

### `AC-ACT-3` — Cross-organization or forged source is rejected

- **Given** a selected source belongs to another organization or its client-supplied ownership conflicts with authoritative parent state
- **When** the draft is saved, validated, or activated
- **Then** the source is rejected without revealing protected details or existence outside the actor's scope
- **And** no cross-scope reference is stored in an activated baseline
- **And** no protected state outside the authorized organization changes.

### `AC-ACT-4` — Required assessment parameters are validated

- **Given** a draft does not contain exactly one versioned task or lacks a required submission requirement, rubric/evaluation procedure, timing/deadline rule, attempt limit, cohort rule, text workflow, adaptive-follow-up policy, review/release requirement, or memory choice
- **When** readiness or activation validation runs
- **Then** activation is blocked
- **And** each missing or invalid category is identified with an actionable accessible error
- **And** the saved draft remains recoverable.

### `AC-ACT-5` — Lower-scope widening is blocked

- **Given** organization, agent, or harness policy prohibits a capability or sets a non-bypassable limit
- **When** the activity or cohort requests a broader capability or less restrictive limit
- **Then** the broader value does not become effective
- **And** validation applies an approved most-restrictive rule or blocks activation
- **And** the sources and decision category are recorded.

### `AC-ACT-6` — Readiness does not replace commit-time validation

- **Given** readiness passes for a draft
- **And** authorization or a required source changes before activation commits
- **When** activation runs
- **Then** the service reauthorizes and reloads authoritative state
- **And** blocks activation if the current state is no longer valid
- **And** does not rely on the stale readiness result.

### `AC-ACT-7` — Activation creates one immutable baseline atomically

- **Given** one currently authorized Activity administrator has a draft cohort with complete valid immutable sources
- **When** the administrator confirms activation
- **Then** exactly one baseline, cohort binding, activated-state transition, and required audit event commit successfully
- **And** routine activation does not require a second-person approval
- **And** the baseline has a stable ID, digest, schema/procedure versions, actor, UTC activation time, and source provenance
- **And** the cohort is unavailable if any part of the boundary fails.

### `AC-ACT-8` — Baseline contains the downstream fairness contract

- **Given** cohort activation succeeds
- **When** an authorized administrator or session resolver inspects the baseline
- **Then** all fields required by `REQ-ACT-16`–`REQ-ACT-18` are present
- **And** behavior-affecting sources use immutable versions or verified digests
- **And** inherited, supplied, narrowed, derived, and exception values are distinguishable
- **And** the resolver can verify the identifier, digest, and ownership chain.

### `AC-ACT-9` — Approved-memory reads are disabled safely

- **Given** the cohort selects no approved-memory reads
- **When** activation succeeds
- **Then** Stable memory and `approved_memory_reads = disabled` are explicit in the baseline
- **And** Dynamic writes and cross-participant learning are disabled
- **And** no memory snapshot or mutable memory source is treated as eligible retrieval content.

### `AC-ACT-10` — Approved-memory reads use one immutable snapshot

- **Given** the cohort is permitted to read approved memory
- **When** activation succeeds
- **Then** the baseline records one organization-owned immutable memory-snapshot identifier and digest plus eligible retrieval scope
- **And** Stable memory and disabled Dynamic writes are explicit
- **And** a missing, mutable, cross-scope, or unverifiable snapshot blocks activation.

### `AC-ACT-11` — Deferred capabilities are explicitly disabled

- **Given** an MVP cohort is ready for activation
- **When** its candidate and final baselines are inspected
- **Then** text interaction is enabled
- **And** voice, tool execution, Dynamic memory writes, shared-session behavior, and non-campaign deployment forms are explicitly disabled
- **And** the empty tool set cannot be interpreted as permission to execute undeclared tools.

### `AC-ACT-12` — Empty cohort may activate before assignment

- **Given** an assessment draft and cohort are otherwise ready but contain no participants
- **When** an authorized administrator activates the cohort
- **Then** activation may succeed with the same fairness and audit checks
- **And** a later authorized enrollment does not change the baseline ID or digest
- **And** participant assignment remains governed by `submission-attempts.md`.

### `AC-ACT-13` — Material change creates a new cohort baseline

- **Given** a cohort has an activated baseline
- **When** an administrator requests a change to a fairness-sensitive value
- **Then** the original baseline and cohort remain unchanged
- **And** the system directs the administrator to a new activity revision and cohort candidate
- **And** any later activation produces a new baseline identity
- **And** the superseding relationship and differences are inspectable.

### `AC-ACT-14` — Later source changes do not alter activation history

- **Given** a cohort is activated
- **When** an organization policy, agent, harness, model alias, knowledge source, workflow, rubric, memory source, or editable activity draft later changes
- **Then** the baseline retains its original content, source references, and digest
- **And** existing enrollments and sessions remain linked to the original cohort
- **And** a later cohort resolves independently.

### `AC-ACT-15` — Duplicate and concurrent activation are safe

- **Given** equivalent or conflicting activation requests target the same draft cohort concurrently
- **When** they reach the activation boundary
- **Then** at most one authoritative baseline binds to the cohort
- **And** equivalent retries return the existing result
- **And** stale or conflicting requests receive the current state without overwriting it
- **And** no source values from competing drafts are combined.

### `AC-ACT-16` — Activation failure preserves a recoverable draft

- **Given** validation, authorization, source retrieval, digest verification, persistence, or required audit acceptance fails
- **When** activation terminates
- **Then** the cohort remains unactivated
- **And** no enrollment/session path may consume the failed candidate
- **And** the last successfully saved authorized draft remains available
- **And** the attempt records a stable non-sensitive failure category and correlation reference.

### `AC-ACT-17` — Required audit acceptance gates activation

- **Given** activation is classified as requiring durable audit
- **When** the audit event cannot be durably accepted in the approved boundary
- **Then** the baseline, cohort binding, and activated-state transition do not commit
- **And** the operation reports a retryable or administrator-action state without claiming activation
- **And** an operational alert is produced without raw protected content.

### `AC-ACT-18` — Stale draft save does not overwrite newer work

- **Given** one actor has an older activity revision while another actor saves or activates a newer revision
- **When** the older actor saves or activates
- **Then** the request is rejected as stale or requires an explicit merge/new revision workflow
- **And** the current authoritative state is not overwritten
- **And** recoverable local input and a readable difference summary remain available when privacy permits.

### `AC-ACT-19` — Timing is unambiguous

- **Given** an administrator configures campaign dates, deadlines, permitted session-window rules, or attempt duration
- **When** the values are saved and later displayed
- **Then** persisted wall-clock instants have UTC interpretation and the named display timezone is recorded
- **And** the baseline freezes cohort rules and bounds while the enrollment/session contracts own the participant's actual permitted window
- **And** invalid ordering, nonexistent local times, ambiguous local times, or non-positive duration/attempt values block activation with field-specific errors.

### `AC-ACT-20` — Authorized actors receive a readable baseline summary

- **Given** baselines exist across organizations and cohorts
- **When** an authorized administrator or assigned reviewer opens one
- **Then** the actor sees only baselines within current scope
- **And** receives a readable summary before technical provenance
- **And** can inspect permitted source versions, digest status, memory state, disabled capabilities, decisions, and superseding links
- **And** raw secrets, hidden protected content, and unrelated participant data are absent.

### `AC-ACT-21` — Participant cannot access setup internals

- **Given** a participant is or will be enrolled in an activated cohort
- **When** the participant requests the draft, hidden baseline fields, rubric internals, source digests, or activation history
- **Then** the request is denied without revealing whether an inaccessible identifier exists
- **And** only participant-visible instructions and operational facts from the owning later features may be returned.

### `AC-ACT-22` — Setup states are accessible and responsive

- **Given** a setup view is empty, loading, incomplete, ready, validating, failed, stale, permission-denied, or activated
- **When** an administrator uses keyboard navigation, assistive technology, 400 percent zoom, or a narrow viewport
- **Then** status and errors are announced and do not rely on color alone
- **And** focus moves to the error summary, confirmation, or next safe action when appropriate
- **And** controls, labels, consequences, and recovery remain operable and readable without hidden content or horizontal task loss.

### `AC-ACT-23` — Lists and counts remain scope-safe

- **Given** activities, cohorts, and selectable sources exist inside and outside an actor's scope
- **When** the actor lists, searches, filters, autocompletes, counts, or pages through setup resources
- **Then** only authorized resources contribute rows, suggestions, totals, and page metadata
- **And** loading and empty states do not expose inaccessible resource existence.

### `AC-ACT-24` — Negative setup coverage gates release

- **Given** assessment setup is considered for release
- **When** its verification suite runs
- **Then** tests cover unauthorized action, wrong organization/activity/cohort, forged parent, stale draft, mutable source, digest mismatch, capability widening, invalid memory choice, unapproved or stale exception, audit failure, retry, concurrent activation, and post-activation mutation
- **And** the feature is not release-ready while an applicable negative case is missing or failing.

### `AC-ACT-25` — Exceptions require separate bounded approval

- **Given** a separately approved rule permits an exception to a fairness or upper-scope value
- **When** an administrator requests that exception for activation
- **Then** the system requires a current separately authorized approver, explicit reason, bounded resource scope, UTC timestamp, and immutable exception reference
- **And** the exception receives required durable audit
- **And** it does not widen a non-bypassable organization boundary
- **And** an absent, stale, unbounded, or unauthorized exception blocks activation.

### `AC-ACT-26` — New drafts default to no approved-memory reads

- **Given** an authorized administrator creates a new assessment draft
- **When** the memory configuration is initialized
- **Then** the draft defaults to Stable memory with `approved_memory_reads = disabled`
- **And** no memory snapshot is selected implicitly
- **And** changing to snapshot-backed reads requires an explicit authorized selection that must pass `AC-ACT-10`.

### `AC-ACT-27` — Readiness and activation meet the approved objective

- **Given** required immutable source metadata is available inside the platform boundary under representative load
- **When** readiness and activation latency are measured separately
- **Then** each operation completes in no more than 2 seconds at the 95th percentile
- **And** authentication-provider redirects and end-user network latency are excluded consistently
- **And** a missed objective is observable without logging raw protected content.

## Edge and failure cases

| Case | Required outcome |
| --- | --- |
| No permitted agent, harness, rubric, workflow, model, or memory snapshot is available | Show an authorized empty state and owning administrative next action; do not offer an invalid placeholder as activatable configuration |
| Selected source becomes unavailable or revoked between readiness and activation | Fail activation, preserve the draft, identify the source category, and audit the failure |
| Source digest differs from its selected immutable identity | Treat as integrity failure; do not substitute a current version or recompute the candidate silently |
| Organization policy narrows after a draft save | Apply current policy at activation; block or narrow under an approved deterministic rule and show the change |
| Organization policy narrows after activation | Preserve the baseline; the owning authorization/session contracts decide whether new starts remain permitted, and history must reflect the policy/baseline distinction |
| Activation times out with uncertain commit status | Reconcile by trusted idempotency key and authoritative cohort binding before offering retry; never create a second baseline blindly |
| Administrator loses permission during confirmation | Reauthorize at commit, fail closed, preserve no unauthorized side effect, and show a non-disclosing access-changed state |
| Audit ingestion or transactional persistence is unavailable | Leave the cohort unactivated and alert according to the approved durability policy |
| Draft is opened in two browser tabs | Detect revision conflict; preserve recoverable input and prevent last-write-wins loss of audit-relevant changes |
| Participant is enrolled after activation | Link enrollment to the existing cohort without changing the baseline; enforce eligibility in the enrollment feature |
| Participant-specific accommodation is requested | Apply `REQ-ACT-32`; do not use assessment setup for a hidden exception or rewrite the cohort baseline |
| Activated source is later lawfully deleted or becomes unverifiable | Preserve honest degraded/unverifiable status under governing policy; do not substitute a newer source or claim full verification |

## Dependencies and rollout

### Dependencies

- Approved authorization, resource isolation, scoped-query, commit-time reauthorization, and audit-durability behavior from [`auth-resource-isolation.md`](auth-resource-isolation.md), [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md), and [ADR-003](../../architecture/decisions/ADR-003-authorization-audit-persistence.md).
- Versioned organization policy, agent, harness, task/submission requirement, workflow, adaptive-follow-up policy, rubric/evaluation procedure, model deployment, knowledge source, memory snapshot, and review/release requirement records.
- An append-only or equivalently tamper-evident audit facility with UTC timestamps, unambiguous ordering, correlation, and idempotent acceptance.
- Approved [ADR-004](../../architecture/decisions/ADR-004-assessment-activation-baseline-and-atomicity.md) for the versioned activation-baseline schema, canonical representation/digest procedure, idempotency, and atomic activation boundary compatible with downstream [ADR-001](../../architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md) semantics.
- Enrollment and attempt behavior from [`submission-attempts.md`](submission-attempts.md).
- Session resolution consumption and drift checks from [`resolved-session-configuration.md`](resolved-session-configuration.md).
- Participant-visible instructions and active-session timing behavior from [`session-text-lifecycle.md`](session-text-lifecycle.md).

### Rollout

- Assessment setup and cohort activation are mandatory MVP foundations, not optional customer-facing feature flags.
- No production enrollment or session may use a cohort that lacks a successfully committed baseline with verifiable ownership and required source identities.
- Implement the draft revision, readiness, baseline, activation, authorization, and audit contracts before enabling downstream participant assignment.
- Seeded or migrated sources without unambiguous organization ownership, immutable identity, or verified digest must be quarantined from activation rather than treated as global defaults.
- Roll out source families behind automated compatibility and negative-isolation tests, but keep activation disabled until every required MVP category is supported.
- A diagnostic or shadow readiness evaluator may compare outcomes, but only the authoritative activation validator may permit activation.
- The [assessment Campaign setup interaction specification](../../ui-ux/assessment-campaign-setup.md) is approved. UI implementation and Playwright evidence for desktop and narrow states remain traceability gaps.

### Observability

Track at minimum:

- Draft creation/save success, validation failure, and revision-conflict counts.
- Readiness and activation latency by bounded stable category.
- Activation successes, failures, retries, deduplications, and uncertain-commit reconciliations.
- Missing immutable sources, mutable aliases, digest mismatches, cross-scope selections, and compatibility failures.
- Upper-scope conflict and capability-widening rejection counts.
- Stable-memory no-read versus snapshot selection counts without identifying snapshot content.
- Required-audit acceptance failure, backlog, and projection lag.
- Baseline verification success, degraded, and failed status.
- Post-activation material-change attempts and new-cohort creation.
- Count of activated cohorts lacking required baseline fields; the release target is zero.

Metrics, logs, and traces must use bounded labels and must not contain raw participant data, prompts, rubric or knowledge content, credentials, secrets, or unrestricted source identifiers.

## Open questions

Questions `Q-1`–`Q-6` and the additional approval confirmations for audit durability, empty-cohort activation, and timing ownership were decided on 2026-08-06 as recorded below.

- `Q-7` — **Resolved 2026-08-21 by `PROP-7` and ADR-017.**
  Configuration-owned PostgreSQL source versions plus readiness descriptors are
  the only source authority that may participate in the Assessment activation
  transaction in this slice. Sessions file/in-memory profile, qualification,
  and credential-catalog records are not commit participants and are not
  baseline content. Production fails closed for any required category that
  cannot be revalidated in-transaction. Credential binding remains a
  downstream Session concern.
- `Q-8` — **Proposed as `PROP-8`.** What readiness conditions are warnings rather
  than blockers? Interim default: empty knowledge is a warning and does not
  block activation. Other unspecified warning categories remain blockers until
  approved.

## Approved decision disposition

The following table preserves question and proposal history while linking each approved decision to its authoritative location. The cited requirements, acceptance criteria, and ADR govern behavior.

| Prior IDs or confirmation | Approved disposition | Authoritative location |
| --- | --- | --- |
| `Q-1`, `PROP-1` | Limit the assessment MVP to exactly one versioned task per activity. | `REQ-ACT-5`, `AC-ACT-4` |
| `Q-2`, `PROP-2` | Default new drafts to Stable memory with approved-memory reads disabled; permit explicit selection of one verified immutable snapshot. | `REQ-ACT-26`–`REQ-ACT-28`, `AC-ACT-9`, `AC-ACT-10`, `AC-ACT-26` |
| `Q-3`, `PROP-3` | Keep participant-specific accommodations out of setup; P0 #4 owns explicit authorization, reason, bounded policy, immutable linkage, actual timing, and fairness-visible provenance. | `REQ-ACT-23`, `REQ-ACT-25`, `REQ-ACT-31`, `REQ-ACT-32`, `AC-ACT-19` |
| `Q-4`, `PROP-4` | Set readiness and activation objectives to no more than 2 seconds at the 95th percentile under the stated preconditions. | [Performance and reliability](#performance-and-reliability), `AC-ACT-27` |
| `Q-5`, `PROP-5` | Use the distinct versioned `activation-baseline-jcs-sha256-v1` procedure and an atomic activation boundary compatible with, but not conflated with, ADR-001. | `REQ-ACT-15`, `REQ-ACT-18`, [ADR-004](../../architecture/decisions/ADR-004-assessment-activation-baseline-and-atomicity.md) |
| `Q-6`, `PROP-6` | Permit one authorized Activity administrator to perform routine activation after deliberate confirmation; require additional authorized approval only for a separately permitted exception. | `REQ-ACT-41`, `REQ-ACT-42`, `AC-ACT-7`, `AC-ACT-25` |
| `Q-7`, `PROP-7` | Use Configuration-owned PostgreSQL source versions and readiness descriptors as the only activation-transaction source authority in this slice; exclude Sessions file registries and credential binding; fail closed in Production when a required owner cannot revalidate an exact version in-transaction. | [ADR-017](../../architecture/decisions/ADR-017-assessment-source-authority-and-activation-transaction.md), `REQ-ACT-9`–`REQ-ACT-16`, `REQ-ACT-24`, `AC-ACT-6`–`AC-ACT-8`, `AC-ACT-27` |
| `Q-8`, `PROP-8` | When every required category is ready and no knowledge references are selected, readiness is `warning` and activation remains permitted. | `REQ-ACT-12`, `REQ-ACT-16`, `AC-ACT-4` |
| Audit durability confirmation | Classify cohort activation and approved exceptions as `required_durable`; fail the protected transition when durable audit acceptance fails. | `REQ-ACT-15`, `REQ-ACT-36`, `AC-ACT-17`, `AC-ACT-25`, ADR-003, ADR-004 |
| Empty-cohort confirmation | Permit activation before participant assignment; later enrollment must not change the baseline. | `REQ-ACT-25`, `AC-ACT-12` |
| Timing-boundary confirmation | Freeze cohort timing rules and bounds in setup; let P0 #4 own participant-specific accommodations and actual permitted timing, which session resolution records as session-bound values. | `REQ-ACT-16`, `REQ-ACT-23`, `REQ-ACT-25`, `REQ-ACT-31`, `REQ-ACT-32`, `AC-ACT-19` |

## Approved defaults

- `PROP-1` — Limit the assessment MVP to one versioned task binding per activity.
- `PROP-2` — Default new assessment drafts to Stable memory with approved-memory reads disabled; allow explicit selection of one verified immutable snapshot.
- `PROP-3` — Defer participant-specific accommodations and actual permitted timing to `submission-attempts.md`; prohibit hidden ad hoc setup overrides.
- `PROP-4` — Require readiness and activation to complete in no more than 2 seconds at the 95th percentile under the approved preconditions.
- `PROP-5` — Use ADR-004's distinct versioned RFC 8785/SHA-256 activation-baseline procedure and atomic activation boundary; do not reuse the resolved-configuration procedure identifier.
- `PROP-6` — Permit single-admin routine activation after deliberate confirmation; require additional approval for separately permitted exceptions.
- `PROP-7` — Use Configuration-owned PostgreSQL source versions and readiness descriptors as the only activation-transaction source authority in this slice; fail closed in Production when a required owner cannot revalidate in-transaction; exclude Sessions file registries and credential binding from activation authority. See resolved `Q-7` and approved ADR-017.

## Proposed defaults

- `PROP-8` — When required categories are ready and no knowledge references are selected, readiness overall severity is `warning` with `assessment.knowledge_unselected`. Activation remains permitted. Other warning categories stay unspecified and fail closed as blockers.

## Traceability

| Requirement/AC | Implementation | Automated verification | Playwright/manual evidence | Status |
| --- | --- | --- | --- | --- |
| `REQ-ACT-1`–`REQ-ACT-8`, `AC-ACT-1`–`AC-ACT-5`, `AC-ACT-18` | Assessment Configuration domain/application handlers, PostgreSQL revisions and scoped selectors, production HTTP endpoints, and React draft/setup surfaces implement the approved optimistic-concurrency boundary. Draft save retargets an unactivated Cohort bound revision. | Focused Assessment, HTTP-negative, React, and PostgreSQL save-then-retarget store tests cover draft lifecycle, required fields, source selection, cross-scope denial, stale revision, and protected-state clearing. | Authenticated create, save, two-tab stale, and unsaved-leave screenshots exist. Create selectors show source kind, exact version, and development/available status. | Implemented |
| `REQ-ACT-9`–`REQ-ACT-13`, `AC-ACT-4`, `AC-ACT-6` | Trusted readiness evaluation uses exact Configuration descriptors and approved [ADR-017](../../architecture/decisions/ADR-017-assessment-source-authority-and-activation-transaction.md) transaction-aware validation; Production fails closed for non-participating source owners. Proposed `PROP-8` treats empty knowledge as a warning. | Focused tests cover missing, stale, mutable, revoked, wrong-scope, digest, compatibility, empty-knowledge warning, and commit-time source/permission changes. | Authenticated warning, blocked, ready, and out-of-date screenshots exist; other warning categories stay unspecified. | Partial |
| `REQ-ACT-14`–`REQ-ACT-24`, `REQ-ACT-41`, `REQ-ACT-42`, `AC-ACT-7`, `AC-ACT-8`, `AC-ACT-13`–`AC-ACT-17`, `AC-ACT-25` | Assessment owns the baseline schema/digest producer, atomic PostgreSQL coordinator, attempts, binding, idempotency/reconciliation, immutable history, required audit/outbox, and thin HTTP commands under approved ADR-004 and ADR-017. Empty knowledge is recorded as a derived fairness domain `selected=none`. GET recomputes the stored baseline digest and reports `verified` or `degraded`. An activated Activity with a missing Cohort, missing bound baseline, missing persisted document, failed digest recompute, or revision/binding inconsistency is `degraded`, never `verified`. | Canonicalization, fault/race, retry, authorization-revocation, lost-response, redaction, audit-failure, migration-upgrade, digest-mismatch, missing-digest-check, and hosted GET recompute tests pass in focused suites. | Authenticated confirmation with compact Task/Agent/Harness/timing/Attempts/memory/capabilities/rubric/review summary, warning activation, success, and new-cohort-omitted screenshots exist. Dedicated reconciling PNG is still missing. | Partial |
| `REQ-ACT-25`, `REQ-ACT-31`–`REQ-ACT-34`, `AC-ACT-12`, `AC-ACT-19`, `AC-ACT-23` | Cohort lifecycle, empty-cohort activation, time bounds, and the explicit Enrollment handoff boundary are implemented without creating Enrollment or assignment authority. | Focused and PostgreSQL tests cover empty-cohort activation, bounds, timezone identity, and scoped list/count behavior. | Empty-cohort activation, confirmation, and activated-success screenshots exist. Later Enrollment handoff evidence remains open. | Partial |
| `REQ-ACT-26`–`REQ-ACT-30`, `AC-ACT-9`–`AC-ACT-11`, `AC-ACT-26` | Stable-memory default/no-read, exact immutable snapshot validation, and the P0 capability profile are enforced in readiness and baseline construction. | Focused tests cover no-read, exact snapshot, cross-scope memory, and prohibited Dynamic/tool/voice/shared-session capability paths. | Confirmation compact summary shows Stable no-read memory and disabled P0 capabilities. Snapshot-selector live UI remains unseeded. | Partial |
| `REQ-ACT-35`–`REQ-ACT-40`, `AC-ACT-17`, `AC-ACT-20`, `AC-ACT-21` | PostgreSQL audit/outbox coupling, authorized baseline/history projections, redaction, and degraded historical verification are implemented for the current slice. | Focused, PostgreSQL, and HTTP-negative tests cover durability failure, role/scope denial, access revocation, and baseline non-disclosure. | Component admin/degraded states exist; authenticated Administrator/Reviewer browser evidence remains open. | Partial |
| UX/accessibility requirements, `AC-ACT-22` | The production Assessment provider and Activity/setup pages implement approved component states and keep `/browser` non-authoritative. Confirmation uses the specified freeze copy, compact frozen-configuration map, and warning list; leave uses the specified three actions; access-changed offers Back to Activities. | React tests cover accessible names, validation, navigation confirmation, compact confirmation sections, readiness, reconciliation-after-GET, access loss including denied save, and degraded summaries. | Authenticated warning, confirm compact summary, leave, activated, degraded, access-changed, keyboard, reduced-motion, and 400-percent chrome screenshots exist. 400-percent still clips chrome; both-theme matrix is incomplete; dedicated reconciling PNG is missing. | Partial |
| Performance/reliability requirements, `AC-ACT-15`–`AC-ACT-18`, `AC-ACT-27`, `PROP-4` | Idempotency, transaction consistency, reconciliation, and bounded telemetry surfaces are implemented under ADR-004 and ADR-017. | Fault, timeout-adjacent, retry, uncertain-commit, and race tests pass. Local same-origin CSRF readiness p95 was 6 ms / 20 POSTs; activation p95 was 17.5 ms / 12 POSTs (OIDC excluded). | Pending/retry/conflict components and authenticated recovery screenshots exist. Multi-tenant load and OIDC-inclusive p95 were not measured. | Partial |
| Security/privacy requirements, `AC-ACT-3`, `AC-ACT-5`, `AC-ACT-21`, `AC-ACT-23`–`AC-ACT-25` | IdentityAccess application sessions, server-derived Organization/relationship/action context, commit-time authorization, exact source validation, and non-disclosing HTTP projections protect the implemented surface. | Focused, HTTP-negative, and project gitleaks cover current MFA, CSRF, guessed identifiers, wrong scope, revoke-after-read, and sensitive baseline redaction cases. | Denied/access-loss components and an authenticated access-changed screenshot exist. Live OTP MFA is fixture-mapped; Reviewer browser evidence remains open. | Partial |
