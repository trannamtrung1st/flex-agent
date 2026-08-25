# Feature: Submission and attempts

## Status and source

- Status: Approved
- Owner: Product Lead
- Approvers: Product Lead, Architecture Lead, UI/UX reviewer, Security/Privacy reviewer
- Approved date: 2026-08-06
- Last amended: 2026-08-23 — approved `PROP-8`, `REQ-SUBM-57`–`REQ-SUBM-58`,
  and `AC-SUBM-40`–`AC-SUBM-41` for replica-independent Enrollment request
  limits, plus `PROP-9`–`PROP-15` timing and Accommodation readiness contracts;
  PostgreSQL-backed shared admission for `PROP-8` is implemented, then hardened
  so the deployed window cannot change and cleanup is expiry-indexed; `0045`
  keeps a valid longer `0044` window and waits until mismatched counters no
  longer overlap the aligned deployed-policy bucket.
- Source: [Enrollment / participation](../../product/concept-model.md#enrollment-participation), [Attempt](../../product/concept-model.md#attempt), [Submission](../../product/concept-model.md#submission), [Assessment fairness constraints](../../product/concept-model.md#assessment-fairness-constraints), [MVP validation slice](../../product/mvp-scope.md#mvp-validation-slice), and [MVP executable workflow](../../product/mvp-scope.md#mvp-executable-workflow)
- Catalog entry: P0 #4 — [P0 authoring order](../README.md#p0-authoring-order)
- Related requirements: Consumes the activated cohort and frozen attempt/submission rules from [`assessment-setup.md`](assessment-setup.md), the authorization contract from [`auth-resource-isolation.md`](auth-resource-isolation.md), and supplies trusted enrollment, participant, attempt, submission-version, accommodation, and permitted-timing data to [`resolved-session-configuration.md`](resolved-session-configuration.md) and [`session-text-lifecycle.md`](session-text-lifecycle.md).
- Related UI/UX: The approved [Submission and Attempt interaction specification](../../ui-ux/submission-attempt.md) governs administrator Enrollment and exception-approval interaction plus Participant Submission, accepted-version, Attempt-readiness, start, reconciliation, accessibility, responsive, and protected-content behavior.
- Related decisions: [ADR-001](../../architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md) governs compatible protected references from the resolved configuration and manifest. [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md) and [ADR-003](../../architecture/decisions/ADR-003-authorization-audit-persistence.md) govern authorization enforcement and durable audit. [ADR-005](../../architecture/decisions/ADR-005-atomic-attempt-start-and-submission-binding.md) governs the atomic attempt/session-start and exact Submission-binding boundary required by `PROP-2`. [ADR-006](../../architecture/decisions/ADR-006-mvp-architecture-baseline-and-evolution.md) governs quarantine and immutable artifact storage. [ADR-008](../../architecture/decisions/ADR-008-bounded-oss-component-set.md) selects the object-store default and configurable `ArtifactSafetyScanner` boundary, while the [MVP operational defaults](../mvp-operational-defaults.md) govern intake, authentication-session, lifecycle, and recovery defaults. `PROP-1`–`PROP-7` were approved on 2026-08-06; `PROP-8`–`PROP-15` and [ADR-018](../../architecture/decisions/ADR-018-enrollment-request-limit-scope.md) were approved on 2026-08-23 and are recorded under [Approved decision dispositions](#approved-decision-dispositions).
- Decision approval: `PROP-1`–`PROP-7`, the text-plus-attachment and capability-gated access direction, and the complete feature specification were approved on 2026-08-06. `PROP-8` was approved on 2026-08-23 to keep replica-local request limits in the first Enrollment slice and move replica-independent quota to a separate task; the same-day readiness review approved `REQ-SUBM-57`–`REQ-SUBM-58` and `AC-SUBM-40`–`AC-SUBM-41` as that task's observable contract. `PROP-9`–`PROP-15` were approved on 2026-08-23 to resolve accommodation timing, policy precedence, dimensions, reasons, approval separation, lifecycle, and v2 projection compatibility.

This approved specification is authoritative for observable Submission, enrollment, and Attempt behavior in the MVP. Architecture and implementation must preserve its stable requirements, acceptance criteria, and approved decision dispositions.

## Problem and measurable outcome

An activated assessment cohort needs a controlled bridge from administrative setup to an individual participant session. The system must assign the right participant to the right activated cohort, derive that participant's permitted timing without changing the cohort baseline, authorize only the permitted number of attempts, and preserve every accepted submission version used before or during an attempt.

This boundary is fairness- and audit-sensitive. Counting an attempt too early can penalize a participant for a platform failure; counting it too late can permit extra attempts. Replacing a submission silently can make an evaluation impossible to explain. Trusting client-supplied participant, activity, attempt, deadline, or file references can expose another participant's records or bypass activity rules.

The measurable outcome is:

- Every active enrollment links exactly one participant to one activated cohort in one organization, without modifying the cohort activation baseline.
- Every attempt start authorizes the current enrollment, effective timing window, remaining entitlement, cohort state, and complete ownership chain at the commit boundary.
- Duplicate or concurrent start requests cannot create more than one active session or consume more than one entitlement for the same attempt.
- A failed start before the approved consumption boundary does not consume an attempt; a failure after that boundary preserves the attempt and its session provenance.
- Every accepted submission version is immutable, independently identifiable, integrity-verifiable, and linked to its participant, enrollment, task, and applicable attempt/session context.
- Later versions never silently replace or alter a version already bound to a session, evidence record, evaluation, or review.
- Participant-specific accommodations and retry entitlements are authorized, bounded, reason-coded, immutable in history, and visible to authorized fairness review without mutating the cohort baseline.
- Automated verification covers attempt limits, timing boundaries, duplicate/concurrent starts, platform-failure recovery, upload validation, version preservation, cross-participant access, revocation, audit behavior, and accessible recovery.

## Actors and permissions

All actions are governed by [`auth-resource-isolation.md`](auth-resource-isolation.md). A role label, organization membership, cohort membership, possession of a link, or possession of an identifier is not proof of permission.

| Actor | Permitted actions and scope | Explicit restrictions |
| --- | --- | --- |
| Participant | Within an active enrollment, view participant-visible assignment and timing facts; upload or enter permitted submission material; inspect the participant's accepted versions; request an eligible attempt; and retry recoverable intake failures | Cannot select a different participant, activity, cohort, baseline, task, attempt ordinal, timing window, accommodation, submission owner, or session binding; cannot access another participant's identity or records |
| Activity administrator | Within delegated activity scope, create, inspect, suspend, close, or revoke enrollments; inspect attempt status; record accommodations within pre-approved policy bounds; and request or grant retry entitlement only through its approved authorization path | Cannot enroll into an unactivated cohort, widen organization or baseline limits, decide a fairness exception requested by the same actor, rewrite consumed attempts or accepted submission versions, inspect raw participant content without a sensitive-content capability, or use an administrative role label as sufficient authority |
| Organization administrator | Within explicitly delegated organization and action scope, manage applicable enrollment or upload policy and inspect bounded operational history | Organization membership alone does not grant raw submission access, cross-activity mutation, or an exception to non-bypassable policy |
| Assigned reviewer | Within active review assignment, inspect the exact submission versions and attempt facts made available to the review workflow | Cannot modify submissions, grant attempts or accommodations, inspect unassigned records, or treat a mutable latest-version alias as evidence |
| Enrollment and attempt service | Under explicit service identity and bounded delegation, derive trusted ownership, evaluate eligibility, reserve or consume entitlement, reconcile retries, and emit protected events | Cannot trust event or client scope, create cross-organization links, grant an unapproved exception, or reuse one participant's attempt/session for another participant |
| Submission intake service | Under explicit service identity and bounded delegation, receive, validate, quarantine when policy requires, finalize, version, and link permitted material | Cannot make rejected or unverified content available as an accepted submission, execute untrusted content, redirect storage by client identifiers, or expose unrestricted download mechanisms |
| Audit or compliance reviewer | Within explicitly delegated scope, inspect enrollment, attempt, accommodation, retry, submission-version, integrity, and access history | Cannot use audit access to obtain raw submission payloads, secrets, unrelated participant records, or unrestricted exports |

## Scope

### In scope

- Assign an existing participant identity to exactly one activated assessment cohort through an organization-owned enrollment.
- Expose the assignment in-product and preserve invitation or access-notification state when a permitted delivery mechanism is used.
- Activate, suspend, close, and revoke enrollment eligibility without deleting historical attempts, submissions, sessions, or audit records.
- Derive the participant's actual permitted attempt-start window from frozen cohort rules plus current, explicitly authorized participant-specific accommodations.
- Evaluate attempt eligibility, remaining entitlement, deadline/window rules, required submission readiness, concurrent starts, and current authorization.
- Create and preserve attempt ordinals and statuses and bind a successfully started attempt to exactly one isolated session.
- Reconcile duplicate, retried, concurrent, and uncertain start outcomes without double consumption or competing active sessions.
- Grant a bounded retry entitlement after a permitted failure without deleting or renumbering the original attempt.
- Accept direct participant text and permitted file attachments for the single MVP task according to its frozen submission-requirement revision and current non-widening organization policy.
- Provide agent-friendly text attachment categories first and a configurable, extensible material-category contract for later formats without weakening existing policy or historical meaning.
- Validate file count, declared and detected type, size, content, archive/parser safety, and malware state when those checks apply under approved policy.
- Preserve immutable accepted submission versions, integrity metadata, prior versions, and exact attempt/session/evidence bindings.
- Make exact bound material available to an agent only when the resolved session configuration includes a compatible permitted reading capability, and make exact permitted versions available to an assigned reviewer without creating unrestricted repository-style browsing.
- Provide participant, administrator, and reviewer states for enrollment, eligibility, limits, deadlines, intake progress, validation, rejection, retry, accepted versions, conflict, permission change, and unavailable dependencies.
- Record protected audit and operational events using stable references and bounded non-sensitive reason categories.

### Out of scope

- Creating identities, authenticating users, or selecting an identity provider.
- Creating or activating activities and cohorts or editing frozen timing, attempt, task, or submission rules; those belong to [`assessment-setup.md`](assessment-setup.md).
- Resolving or freezing the session configuration and execution manifest; those belong to [`resolved-session-configuration.md`](resolved-session-configuration.md).
- Live text examination, session timers after start, pause/resume, completion, administrative session termination, reconnect behavior, or transcript handling; those belong to [`session-text-lifecycle.md`](session-text-lifecycle.md).
- Defining evidence, generating an evaluation, revising a judgment, deciding a review, constructing a result, or releasing an outcome.
- General-purpose file management, collaborative editing, source-control behavior, repository cloning or browsing, plagiarism detection, cheating detection, biometric verification, or automated proctoring.
- Participant self-service changes to identity, cohort, attempt limits, deadlines, accommodations, or retry entitlement.
- Dynamic memory, using submissions for agent learning, memory candidates, general external tool execution, voice, or shared multi-participant sessions. Controlled reading of an accepted, exact-bound attachment is a capability-gated submission operation under this specification; it does not authorize arbitrary tools, external retrieval, code execution, or repository access.
- Product-wide retention durations, deletion schedules, legal-hold rules, consent wording, malware vendor selection, storage technology, digest algorithm selection, queue topology, or notification-provider selection.

### Terminology and capability boundary

- A **Submission** is the logical, versioned participant work linked to a task, activity, or session; it is not synonymous with a file.
- An **Accommodation** is an immutable Enrollment-scoped record that replaces
  one policy-adjustable timing dimension for one Participant while eligible. It
  is not a Cohort edit, retry entitlement, running-Session extension, or
  Participant-controlled preference.
- A **fairness exception** is a requested replacement value outside the
  pre-approved accommodation bounds. It remains non-effective until a distinct
  currently authorized approver approves the unchanged request; it can never
  widen a non-bypassable Organization boundary.
- **Direct text** is participant text entered into the product as Submission material.
- An **attachment** is a file item contained in an accepted Submission version.
- Acceptance preserves material; it does not by itself authorize an agent, reviewer, model, parser, or tool to read it.
- Agent access requires an exact accepted-version binding plus a compatible capability in the resolved session configuration. Reviewer access requires an active review assignment and sensitive-content authorization; it does not depend on the agent's capabilities.
- A URL or repository reference inside direct text or an attachment remains untrusted inert content unless a separately approved and resolved external capability authorizes retrieval. The MVP does not dereference, clone, browse, or execute such references.

## User journeys and state transitions

### Enrollment lifecycle

```text
Absent
  │ authorized assignment to an activated cohort
  ▼
Active ── authorized temporary restriction ──► Suspended
  │                                             │ authorized restore
  │                                             └──────────────► Active
  ├── authorized revocation ──► Revoked
  └── activity/enrollment end ─► Closed
```

`Revoked` and `Closed` are terminal for new starts under that enrollment. They do not erase accepted submissions, consumed attempts, frozen configurations, sessions, evidence, or audit history. Reassigning the participant requires a new enrollment relationship and must not repurpose the old one.

### Attempt lifecycle

```text
Eligible
  │ authorized start request
  ▼
Starting
  ├── fails before atomic session-start commit ──► Start failed
  │                                                  │ safe eligible retry
  │                                                  └────────────► Eligible
  │
  └── resolved configuration + manifest + session/submission binding + audit commit
                                                        ▼
                                                      Active
                                                        ├── normal terminal flow ─► Completed
                                                        └── abnormal terminal flow ► Aborted
```

`Completed` and `Aborted` attempts are historical terminal records. Under approved decision `PROP-1`, the entitlement is consumed only when the atomic session-start boundary commits. A later retry is represented by a separately authorized entitlement and a new attempt record; it never rewrites or renumbers the original.

### Submission-version intake lifecycle

```text
Direct text or local attachment
  │ participant submits through an authorized intake
  ▼
Uploading / Receiving
  ├── cancelled or transport failure ──► Intake failed
  │                                        │ retry creates/reuses only approved intake state
  │                                        └──────────────────────────────────────────────► Uploading / Receiving
  ▼
Validating
  ├── policy, integrity, or safety rejection ──► Rejected
  └── protected payload + metadata commit ─────► Accepted version
                                                  │ later accepted material
                                                  ▼
                                             New accepted version
```

An `Accepted version` is immutable. `Rejected` or incomplete intake does not become an accepted Submission version and must not be made available to session, evidence, evaluation, preview, or download consumers. Its minimal operational history follows the governing upload and retention policy.

### Administrator assigns a participant

1. The administrator selects an activated cohort within delegated activity scope and a permitted participant identity within the same organization.
2. The service reauthorizes the actor and derives organization, activity, cohort, baseline, participant, and task relationships from trusted state.
3. The service rejects inactive cohorts, incompatible or duplicate relationships, cross-organization identities, and any assignment that would violate current policy.
4. A successful operation creates one active enrollment linked to the unchanged cohort baseline and records its timing/attempt rule sources.
5. The participant can discover the assignment in-product; any external notification is a delivery side effect and not authorization.
6. Equivalent retries return the existing enrollment; a conflicting request reports the current non-sensitive state.

### Participant prepares and submits work

1. The participant opens the assigned task through an active enrollment.
2. The system shows the participant-visible submission requirements, accepted material categories, limits, cutoff interpretation, current accepted versions, whether each category can be inspected by the configured agent, and whether a new version may still be submitted.
3. The participant enters direct text or selects permitted attachments. The initial agent-friendly attachment categories are validated UTF-8 plain text (`.txt`) and Markdown (`.md`); client-side checks may provide early feedback but do not replace server-side validation or detected-content checks.
4. Intake reports receiving and validating states, preserves recoverable local selection when safe, and permits cancellation before final acceptance.
5. The service verifies the trusted enrollment/task ownership, effective submission requirement, applicable timing, file policy, payload integrity, and safety state.
6. On success, the service commits a new immutable version and returns its identifier, accepted UTC time, permitted filename/type/size facts, integrity status, and relationship summary.
7. On failure, no accepted version is created; the participant receives a safe reason category and recovery action without internal scanner, parser, storage, or cross-scope details.

### Participant starts an attempt

1. The participant requests a start for the active enrollment.
2. The service derives the authoritative cohort baseline, participant, task, attempt history, accommodation records, current enrollment/cohort state, and server time.
3. The service evaluates the actual permitted timing window, remaining entitlement, required accepted submission set, compatible resolved agent-reading capabilities, conflicting starts, and current authorization.
4. The start boundary creates or resolves one attempt record and delegates the frozen session start to [`resolved-session-configuration.md`](resolved-session-configuration.md).
5. Only a successful atomic configuration, manifest, session binding, exact submission-version binding under approved decision `PROP-2`, and required audit commit transitions the attempt to `Active` and consumes entitlement under approved decision `PROP-1`.
6. The participant receives the current status, attempt number, remaining entitlement, and timing facts permitted by product policy.

### Start fails or the outcome is uncertain

1. A validation, authorization, timing, submission-readiness, resolution, persistence, audit, or dependency failure occurs.
2. If the atomic start boundary did not commit, the attempt remains unconsumed and the participant receives a safe retry or correction path.
3. If the client response is lost or times out, the service reconciles the idempotency key, attempt record, session binding, resolved configuration, and audit state before offering another start.
4. If the start committed and the session later aborts, the original attempt, configuration, manifest, and reason category remain inspectable.
5. A permitted additional try is granted through an authorized retry entitlement with reason, scope, actor, UTC time, and link to the original attempt; history is never rewritten.

### Administrator applies an accommodation or retry entitlement

1. The administrator opens the participant's enrollment within delegated activity scope.
2. The system shows the immutable cohort value, current effective participant
   timing/attempt facts, and existing exceptions without exposing unrelated
   participants.
3. The administrator selects one currently permitted dimension, one normalized
   replacement value within the server-returned policy bounds, and one bounded
   reason category, then confirms the exact consequence and effective/expiry
   facts.
4. The service resolves both the frozen baseline accommodation bounds and the
   exact current Organization policy. It distinguishes a policy-bounded
   participant-specific value from an exception outside the pre-approved
   bounds.
5. A fairness exception remains non-effective until a different current actor
   with the exact approval action approves the unchanged request. The decision
   cannot edit or widen the value and no absent or stale approval path is
   inferred.
6. The service reauthorizes and revalidates at commit, verifies that the value
   does not widen a non-bypassable Organization boundary, resolves the approved
   accommodation lifecycle policy, and creates an immutable record linked to
   the Enrollment and original Cohort baseline.
7. The effective participant window or retry entitlement is derived from that
   record and supplied to session resolution; the Cohort baseline remains
   unchanged.
8. Authorized reviewers can see the fairness-relevant difference and
   provenance when reviewing the affected Attempt.

### Prohibited transitions

- Absent enrollment to attempt start or accepted submission.
- Enrollment into an unactivated cohort or across an organization boundary.
- Suspended, Revoked, or Closed enrollment to a new accepted submission or attempt start unless an approved rule explicitly permits the action and current authorization confirms it.
- Client-supplied participant, cohort, deadline, accommodation, remaining-attempt, attempt ordinal, submission-owner, object key, or session identifier to authoritative state without trusted verification.
- `Starting` to more than one `Active` session or consumption of more than one entitlement for equivalent retries.
- `Start failed` before atomic commit to a consumed entitlement.
- `Active`, `Completed`, or `Aborted` attempt to renumbered, deleted, or reassigned history.
- Incomplete, failed, quarantined, rejected, or unverified intake to an accepted submission or downstream evidence input.
- An accepted submission version to mutated content, replaced integrity metadata, a different owner/task, or a mutable latest-version evidence reference.
- A later submission version to silently replace a version already bound to an attempt, session, evidence item, evaluation, or review.
- Participant self-service to accommodation, retry entitlement, deadline, attempt-limit, cohort, or ownership changes.
- Cohort membership to visibility of another participant's enrollment, attempt, submission, identity, count contribution, or status.

## Business rules

### Enrollment and assignment

- `REQ-SUBM-1` — An enrollment must link exactly one participant to one activity and one activated cohort within exactly one organization and must reference the unchanged cohort activation baseline.
- `REQ-SUBM-2` — Enrollment creation, read, list, mutation, suspension, closure, revocation, notification, and export must enforce current action- and resource-scoped authorization through the trusted ownership chain.
- `REQ-SUBM-3` — A participant must not be enrolled into a draft, validating, failed, superseded-for-new-assignment, or otherwise unavailable cohort.
- `REQ-SUBM-4` — Assignment must derive organization, activity, cohort, baseline, task, and participant relationships from authoritative state and reject cross-organization or inconsistent parentage without disclosing protected resource existence.
- `REQ-SUBM-5` — Equivalent enrollment commands must be idempotent; a conflicting active enrollment must not be overwritten, duplicated, or silently reassigned.
- `REQ-SUBM-6` — Suspending, closing, or revoking an enrollment must stop authorizing new starts and new submission intake within the approved propagation target while preserving historical records and not independently altering a session already governed by the session lifecycle.
- `REQ-SUBM-7` — Cohort membership, assignment notification, or access-link possession must not expose another participant or grant authority beyond the active enrollment.
- `REQ-SUBM-8` — External delivery status may inform the administrator but must not be treated as proof that the intended participant authenticated, received authorization, or completed any action.

### Timing and accommodations

- `REQ-SUBM-9` — The service must derive each participant's actual permitted attempt-start and submission window from the frozen cohort timing rules, authoritative enrollment state, current server time, and active permitted accommodation records.
- `REQ-SUBM-10` — Persisted deadlines, effective windows, accommodation instants, and acceptance times must use UTC and retain the named timezone needed to display the governing rule unambiguously.
- `REQ-SUBM-11` — A participant-specific accommodation must be explicitly authorized, reason-coded, bounded by organization policy, immutably linked to the enrollment and original cohort baseline, and visible as a fairness-relevant difference to authorized reviewers.
- `REQ-SUBM-12` — An accommodation may narrow or extend only dimensions that an approved policy marks as adjustable and must never mutate the cohort baseline or widen a non-bypassable organization boundary. A requested value outside the pre-approved bounds is a fairness exception and must satisfy `REQ-ACT-42`, including approval by a different currently authorized actor, or be rejected.
- `REQ-SUBM-13` — Eligibility and cutoff decisions must use authoritative service time and one documented boundary rule; client clocks, display timezone, upload progress, and request construction time are not authoritative.
- `REQ-SUBM-14` — Expired, revoked, superseded, exhausted, or out-of-scope accommodations must not affect new eligibility decisions, and their historical effect must remain inspectable.

### Attempt authorization and consumption

- `REQ-SUBM-15` — An attempt start must validate current authentication, enrollment status, organization/activity/cohort/participant/task ownership, cohort availability for starts, actual permitted timing, required accepted submission readiness, remaining entitlement, and absence of a conflicting active start.
- `REQ-SUBM-16` — Attempt ordinal and remaining entitlement must be calculated from trusted historical attempts and active authorized retry-entitlement records; the client must not select or increase them.
- `REQ-SUBM-17` — Attempt start must be idempotent and concurrency-safe so equivalent requests resolve to one attempt/session and conflicting requests cannot oversubscribe the limit.
- `REQ-SUBM-18` — Under approved decision `PROP-1`, an attempt consumes entitlement only when the approved atomic session-start boundary commits the resolved configuration, initial manifest, session binding, exact submission-version binding required by approved decision `PROP-2`, attempt transition, and required audit state.
- `REQ-SUBM-19` — A failure before that atomic commit must not consume entitlement; a timeout or uncertain response must be reconciled from authoritative state before another attempt is authorized.
- `REQ-SUBM-20` — A session that aborts after the start boundary commits must retain a consumed `Aborted` attempt, frozen configuration, manifest, and reason category; it must not be deleted, reset, or reused.
- `REQ-SUBM-21` — A permitted retry after consumption must be represented by a separate authorized entitlement with actor, reason, bounded scope, UTC time, original-attempt reference, approval provenance, and required audit; it must produce a new attempt record rather than changing the original ordinal or status. When the entitlement permits an attempt beyond the frozen baseline limit, it is a fairness exception and must satisfy `REQ-ACT-42`, including approval by a different currently authorized actor.
- `REQ-SUBM-22` — At most one non-terminal session may be bound to an attempt, and an attempt/session binding must never be reassigned to another enrollment or participant.
- `REQ-SUBM-23` — Attempt status, entitlement, accommodation, and failure information shown to a participant must be limited to actionable participant-facing facts and must not expose internal policy, other participants, hidden configuration, or security controls.

### Submission intake and version preservation

- `REQ-SUBM-24` — Submission intake must authorize the current participant and trusted enrollment/task relationship and must enforce the frozen submission-requirement revision plus any current organization policy that narrows, but does not widen, the activated baseline.
- `REQ-SUBM-25` — The service must validate every applicable material item by permitted category, count, declared and detected type, size, content structure, integrity, archive/parser safety, and malware state before accepting it under the governing upload policy.
- `REQ-SUBM-26` — If a required upload or safety policy is absent, unavailable, inconsistent, or times out, the service must fail closed for the affected material and must not expose it as accepted, previewable, downloadable, or available downstream.
- `REQ-SUBM-27` — A successfully accepted Submission version must have a stable version identifier, organization/activity/task/enrollment/participant ownership, monotonically ordered version lineage, accepted UTC time, submitting actor, protected payload reference, integrity metadata, permitted descriptive metadata, validation-policy references, and applicable attempt/session bindings.
- `REQ-SUBM-28` — Accepted submission content and integrity metadata must be immutable; correction or replacement creates a new accepted version linked to prior history.
- `REQ-SUBM-29` — Failed, cancelled, incomplete, quarantined, or rejected intake must not create an accepted version or become available to the agent, session, evidence, evaluation, preview, download, or reviewer flow.
- `REQ-SUBM-30` — The service must preserve every accepted version required by applicable evidence, evaluation, review, audit, legal-hold, and lifecycle policy; a later version must not silently overwrite or detach an earlier version.
- `REQ-SUBM-31` — Under approved decision `PROP-2`, the exact accepted submission-version set required for an attempt must be committed in the same authoritative consistency boundary as the attempt transition and session start, or session start must fail without consuming entitlement. Mutable aliases such as `latest` or `current` must not be used by session, evidence, evaluation, or review consumers.
- `REQ-SUBM-32` — A new version after an attempt binding must not change that binding. If the governing submission requirement explicitly permits in-session material, each later accepted version must receive an explicit ordered session binding before downstream use.
- `REQ-SUBM-33` — Participant-visible version history must distinguish receiving, validating, rejected, accepted, bound, superseded-for-future-use, and unavailable-under-policy states without implying that historical content was overwritten.
- `REQ-SUBM-34` — Downloads, previews, and generated access mechanisms must authorize the exact version at use time, be bounded to the intended artifact and actor, and not allow identifier substitution or reuse outside approved scope.
- `REQ-SUBM-35` — Submission content is untrusted participant data. Intake, preview, parsing, indexing, model, and evaluation consumers must not treat embedded instructions as platform authority, tool approval, configuration, or authorization evidence.

### Audit, privacy, and lifecycle

- `REQ-SUBM-36` — Enrollment access-control mutations, accommodations, retry entitlements, attempt start/consumption outcomes, accepted submission versions, version bindings, integrity/safety rejections, sensitive reads/downloads/exports, and security-relevant denials must produce audit events appropriate to their sensitivity.
- `REQ-SUBM-37` — Enrollment access-control mutations and the atomic attempt-start boundary are `required_durable` under the approved authorization and session-start contracts. Under approved decision `PROP-7`, accommodation and retry-entitlement mutations are also `required_durable`; the protected transition must fail if its required audit event cannot be durably accepted.
- `REQ-SUBM-38` — Audit and operational records must use stable protected references and bounded reason categories rather than copying raw submissions, filenames when unnecessary, participant attributes, credentials, tokens, scanner details, or parser output.
- `REQ-SUBM-39` — Enrollment, attempt, submission, accommodation, retry, intake, audit, and payload records must follow applicable approved retention, deletion, legal-hold, consent, export, and evidence-preservation policy; this feature defines no independent duration.
- `REQ-SUBM-40` — In the MVP, participant submission material must not be reused for agent learning, cross-participant memory, calibration, or an unrelated activity. Any later-release reuse requires a separately approved governing specification, explicit policy and permission, provenance, scope, and lifecycle controls; Dynamic memory and learning remain disabled in the MVP.
- `REQ-SUBM-41` — Authorized history must make it possible to determine who assigned the participant, which baseline and rules applied, which attempts were authorized and consumed, which exceptions applied, which exact submission versions were accepted and bound, and what changed without reconstructing from raw logs.
- `REQ-SUBM-42` — List, search, count, notification, storage, cache, event, background, preview, download, and export paths must preserve organization, activity, participant, enrollment, attempt, session, and version isolation where applicable.
- `REQ-SUBM-43` — Under approved decision `PROP-6`, every active enrollment must be discoverable to its participant through an authorized in-product list or detail view. The view must reflect the current state and next permitted action while participant visibility remains authorized; after suspension, revocation, closure, or another visibility change it must stop showing stale Active state and must not reveal inaccessible assignments or other participants through rows, totals, filters, or empty states.
- `REQ-SUBM-44` — Material categories must be governed by a configurable, versioned, extensible policy contract. Each enabled attachment category must define compatible detected content types and encodings, permitted filename extensions as non-authoritative hints, positive per-item and total count/size bounds, validation and integrity rules, malware and parser behavior, preview/download behavior, agent-reading capability requirements, and lifecycle-policy references. Organization policy sets non-bypassable bounds; frozen task/activity requirements may select a supported subset or narrow those bounds but must not enable an unapproved category or widen them.
- `REQ-SUBM-45` — The initial MVP attachment categories must be agent-friendly text: validated UTF-8 plain text (`text/plain`, `.txt`) and Markdown (`text/markdown`, `.md`). Direct text entry remains a separate permitted material category. A configured category is unavailable unless all fields required by `REQ-SUBM-44` have approved values and active controls; archive, document, image, audio, code-execution, and repository categories are not implicitly enabled by this baseline.
- `REQ-SUBM-46` — An agent, model, parser, or tool may receive Submission material only when current authorization permits the action, the exact accepted version is bound to the session, and the resolved session configuration contains a compatible capability permitted by organization, Agent, Harness, and Activity constraints. Access must remain limited to the exact bound material and must preserve its version and provenance for evidence use.
- `REQ-SUBM-47` — If a frozen requirement requires agent inspection of material and no compatible permitted capability can access every required item, the attempt must not start and must not consume entitlement. If agent inspection is not required, inaccessible optional material may remain preserved, but the participant and assigned reviewer must be told that the agent did not inspect it and it must not be represented as agent-consumed evidence.
- `REQ-SUBM-48` — Submission links and repository references must remain inert untrusted content unless a separately approved external capability is present in the resolved session configuration and its governing release/specification permits retrieval. This feature does not authorize repository cloning, browsing, code execution, network fetching, or escalation from embedded content.
- `REQ-SUBM-49` — The human-review workflow may expose exact accepted versions only through an active review assignment and current sensitive-content authorization. The MVP handoff supports an assigned review queue or workspace; it must not create a general-purpose submission repository, cross-assignment browsing, or authority derived from search visibility.
- `REQ-SUBM-50` — Under approved decision `PROP-9`, the frozen timing fields map
  to a Submission window `[starts_at_utc, deadline_utc)` and an Attempt-start
  window `[starts_at_utc, ends_at_utc)`. `per_attempt_duration_seconds` remains
  a separate Session duration limit and must not silently move the latest
  Attempt-start instant.
- `REQ-SUBM-51` — Under approved decision `PROP-10`, every new timing or
  eligibility decision must evaluate the frozen Cohort timing and
  accommodation bounds together with the exact current Organization policy.
  A later current-policy narrowing may make a historical accommodation
  ineligible without rewriting the Cohort baseline, accommodation, or prior
  decisions. Missing, mutable, revoked, stale, incompatible, unavailable, or
  cross-scope policy must fail closed.
- `REQ-SUBM-52` — Under approved decision `PROP-11`, the initial timing-
  accommodation dimensions are `submission_deadline_utc`,
  `attempt_start_not_before_utc`, `attempt_start_before_utc`, and
  `per_attempt_duration_seconds`. Each record stores one normalized replacement
  value, not a delta. Organization policy must explicitly enable the dimension
  and supply a positive bounded permitted result range. A policy source may
  express absolute or relative bounds, but its owner port must normalize them
  against the verified baseline into absolute UTC-instant or positive-seconds
  result ranges before evaluation. At most one record per Enrollment and
  dimension may affect a decision; a later approved record supersedes rather
  than edits or adds to the prior value.
- `REQ-SUBM-53` — Under approved decision `PROP-12`, every accommodation or
  fairness-exception request must use a bounded reason category supplied by the
  exact policy. Free-text explanations, diagnoses, and detailed personal
  circumstances must not be collected by this feature. Development/Testing
  fixtures may use a clearly synthetic category, but no Production reason
  vocabulary or positive bound is implied by fixture data.
- `REQ-SUBM-54` — Under approved decision `PROP-13`, every fairness exception
  requires a different currently authorized requester and approver. Approval
  and rejection must use a separate exact action, must not edit or widen the
  request, and must reauthorize and revalidate the unchanged request, current
  policy, non-bypassable bounds, Enrollment, and baseline immediately before
  the required-durable decision commits. No current approver means no effect.
- `REQ-SUBM-55` — Under approved decision `PROP-14`, accommodation records and
  their decision history must resolve the approved lifecycle class in the
  [MVP operational defaults](../mvp-operational-defaults.md#protected-data-lifecycle-defaults).
  Business `expires_at` controls eligibility only and is not a deletion or
  retention clock. Expired state is derived from authoritative service time;
  implementations must not require a materialized expiry transition for
  eligibility correctness. Related idempotency outcomes follow the separate
  90-day operational-default row, and audit metadata follows its 730-day row.
- `REQ-SUBM-56` — Under approved decision `PROP-15`, existing strict v1
  Enrollment and **My work** projections retain their current baseline-field
  meaning. Effective timing must be introduced through strict v2 projections
  that distinguish baseline timing, effective timing, authoritative evaluation
  time, eligibility state, and minimized accommodation consequence. V1 and v2
  remain available in parallel during migration; this feature does not retire
  or reinterpret v1.
- `REQ-SUBM-57` — Under approved decision `PROP-8`, every authenticated
  Enrollment or **My work** read and Enrollment mutation must acquire one
  deployment-wide permit after the current application session resolves and
  before protected query or mutation work begins. The permit partition is the
  trusted `(Organization, actor, surface)` tuple, where `surface` is exactly
  `read` or `mutation`. All API replicas share one fixed-window budget. The
  approved defaults and maximums are 60 reads and 20 mutations per 10-second
  window; a deployment may only lower a permit limit or lengthen the window
  through one versioned deployment-wide policy. Unauthenticated requests do
  not consume this actor-scoped quota and remain subject to coarse ingress
  controls.
- `REQ-SUBM-58` — Shared Enrollment admission must use an authoritative service
  clock and an atomic bounded increment so concurrency, retry, process
  restart, or replica routing cannot exceed the deployment-wide budget. Proven
  exhaustion returns `429`, `enrollment.rate_limited`, `Retry-After`, and
  `Cache-Control: no-store`. Missing, unavailable, timed-out, or
  configuration-mismatched shared admission state fails closed as
  `503`/`enrollment.unavailable` with `no-store`; it must not fall back to an
  independent local permit decision. Replica-local limiting may remain only as
  defense in depth. Counter state is protected, operational, short-lived, and
  excluded from business audit; telemetry uses only bounded surface and
  outcome labels without Organization, actor, Participant, or Enrollment
  identifiers.

## Data, evidence, and audit

### Logical records

Architecture may choose physical storage only if it preserves these ownership, authorization, immutability, ordering, audit, and lifecycle semantics.

| Record | Purpose | Minimum content |
| --- | --- | --- |
| Enrollment | Authorize one participant's relationship to one activated cohort | Enrollment ID, organization/activity/cohort/baseline/task/participant references, status, rule-source references, created actor/time, current revision |
| Enrollment revision/event | Preserve status and assignment history | Enrollment reference, prior/new state, actor/service, reason, UTC time/order, correlation, authorization/delegation reference |
| Accommodation | Record one bounded participant-specific timing difference without changing the baseline | Accommodation ID, Organization/enrollment/participant/activity/cohort/baseline references, one approved dimension and normalized replacement value, frozen and decision-time current policy references, bounded reason category, requester and optional distinct approver, effective/expiry times, lifecycle-policy reference, UTC creation/decision/revocation times, status/revision, supersession reference |
| Retry entitlement | Permit one separately authorized additional try | Entitlement ID, enrollment, original attempt, reason category, actor, policy/approval reference, created/expiry times, consumption link |
| Attempt | Preserve one controlled execution try | Attempt ID, enrollment/participant/task/baseline references, ordinal, entitlement source, status, requested/start/terminal times, session/configuration/manifest bindings, terminal reason |
| Start command/outcome | Reconcile retries and uncertain responses | Idempotency key, trusted command digest, attempt/enrollment scope, start-boundary status, correlation, stable outcome/reason, timestamps |
| Submission | Group versioned participant material for one task requirement | Submission ID, organization/activity/task/enrollment/participant ownership, requirement reference, current permitted display state |
| Submission version | Preserve one accepted immutable material set | Version ID/order, submission and ownership references, actor, accepted UTC time, protected payload references, integrity and policy metadata, prior-version link |
| Intake attempt | Track receiving and validation without treating it as accepted evidence | Intake ID/idempotency key, trusted scope, item metadata, status, bounded validation outcomes, correlation, timestamps, accepted-version reference when successful |
| Submission binding | Freeze exact material used by an attempt/session/evidence consumer | Binding ID, exact version references, attempt/session reference, binding reason/stage, actor/service, UTC time/order |
| Material-category policy | Define an enabled, bounded, extensible intake and reading contract | Stable category/version, detected content types/encodings, extension hints, count/size bounds, validation/integrity rules, malware/parser/preview/download behavior, compatible capability identifiers, lifecycle references, status |
| Material access outcome | Preserve whether exact bound material was available to an agent or reviewer | Exact version/item, session or review assignment, actor/service, resolved capability or authorization reference, permitted/blocked/not-inspected outcome, bounded reason, UTC time/order |
| Audit event | Preserve security and governance history | Event/schema ID, actor/service, organization, action, protected resource references, decision/outcome, stable reason, UTC time/order, correlation, delegation/exception reference |

### Required audit events

At minimum, record:

- Enrollment created, deduplicated, suspended, restored, closed, revoked, or rejected.
- Assignment notification requested and bounded delivery outcome when applicable.
- Accommodation requested, granted, rejected, revoked, superseded, or applied;
  and an expired, stale, or current-policy-ineligible record rejected when it
  is presented for eligibility or mutation. Expiry may remain a derived state
  and does not require a standalone expiry mutation.
- Attempt eligibility checked when policy requires, start requested, blocked, deduplicated, committed, reconciled, completed, or aborted.
- Retry entitlement granted, rejected, expired, revoked, or consumed.
- Submission intake started when policy requires, cancelled, failed, rejected, accepted, or deduplicated.
- Accepted submission version created and exact attempt/session binding created.
- Agent material access permitted, blocked, or omitted because of the resolved capability set when required for evidence or investigation.
- Reviewer sensitive-content access through an assigned review workflow when required by policy.
- Integrity, malware, archive, parser, policy, or deadline rejection using a bounded non-sensitive category.
- Authorized preview, download, sensitive read, export, deletion, legal-hold, or lifecycle transition when policy requires.
- Cross-organization, cross-participant, parent-mismatch, guessed-ID, stale-authorization, limit-bypass, and repeated enumeration denials when security-relevant.
- Required audit acceptance failure and any protected transition blocked by it.

Events must preserve unambiguous UTC ordering across enrollment revisions, accommodations, attempt eligibility, atomic session start, submission intake/version acceptance, version binding, session terminal state, and downstream evidence use.

### Evidence integrity and historical inspection

An authorized reviewer or investigator must be able to answer:

- Which participant and activated cohort did the enrollment bind, and under which baseline?
- What cohort timing and attempt rules applied, and which participant-specific accommodation changed the actual permitted window?
- Which attempt entitlement was used, when was it consumed, which session/configuration/manifest did it bind, and why did it terminate?
- Was a later attempt permitted by the baseline limit or a separately authorized retry entitlement?
- Which exact submission requirement and accepted version set applied to the attempt?
- Which exact items could the resolved agent capabilities inspect, which were not inspected, and was any required item blocked before start?
- What prior and later accepted versions exist, and did any consumer use them?
- Did the accepted payload pass the required integrity and safety policy at that time?
- Were assignment, intake, start, download, and export actions authorized within the same ownership chain?

The inspection surface must answer these questions through protected structured records and references; it must not require raw log access or duplicate submission content into audit events.

## Quality requirements

### UX and accessibility

- The participant experience must show assignment status, submission requirements, deadline and timezone, effective accommodation when participant-visible, attempts used/remaining, current attempt state, accepted material categories, whether the configured agent can inspect each required category, and the next permitted action in plain language.
- Empty, loading, receiving, validating, accepted, rejected, cancelled, offline/reconnecting, retrying, limit-exhausted, too-early, expired, permission-changed, unavailable, and conflict states must be distinct in text and structure and must not rely on color, motion, sound, filename, or progress animation alone.
- File controls must have accessible names and instructions; validation errors must identify the affected item and rule category and be programmatically associated with the control.
- Intake progress must expose a text status and must not announce excessively frequent updates. Cancellation, retry, and removal actions must remain keyboard operable.
- Recoverable text and file selections should remain available after a validation, network, stale-state, or authorization-independent failure when retaining them does not create a privacy or security risk.
- When authorization, enrollment state, timing, or policy changes before commit, the interface must block acceptance/start, announce the changed state, preserve safe local work where appropriate, and point to the next safe action.
- Accepted-version history must use readable order and timestamps, identify which version is bound to an attempt when permitted, and require deliberate confirmation before creating a replacement version when the consequence may be misunderstood.
- Before start, required material that lacks a compatible agent-reading capability must be identified with a plain-language blocking explanation. Optional preserved material that the agent will not inspect must be labeled as not available to the agent and must not appear as agent-consumed evidence.
- Start confirmation must show the attempt ordinal, effective time allowance/window, submission version summary, and whether the action consumes entitlement under the approved policy.
- Error summaries must receive focus when appropriate, link to the affected control or item, and avoid exposing internal policy, scanner, parser, storage, or inaccessible resource details.
- All actions must be operable without pointer, hover, drag, sound, or motion and must support logical keyboard focus, screen-reader status announcements, 400 percent zoom, and narrow viewports without hiding deadline, entitlement, version, consequence, or recovery information.
- WCAG 2.2 AA is the contractual target under the approved
  [Submission and Attempt interaction specification](../../ui-ux/submission-attempt.md).

### Performance and reliability

- Enrollment mutations, accommodation/retry grants, attempt starts, and submission finalization must be idempotent where retries are possible and must use bounded trusted ownership queries.
- Attempt eligibility and consumption must be concurrency-safe and must prevent oversubscription under multiple devices, tabs, retries, delayed events, or process restarts.
- Submission intake must support bounded backpressure, cancellation, timeouts, and safe cleanup or quarantine of incomplete material according to policy.
- An uncertain start or finalization response must reconcile authoritative state before retry and must not create a second attempt, accepted version, or binding blindly.
- A finalized accepted version must not be acknowledged until its protected payload and required metadata/integrity state are durably associated; partial failure must remain non-accepted and recoverable or safely cleanable.
- Eligibility, assignment, and metadata-finalization objectives are approved in decision `PROP-5`; byte transfer, end-user network latency, malware scanning, and external delivery latency are measured separately.
- Dependency failure in authorization, policy, ownership, time, storage, validation, audit, or session resolution must fail closed at its protected boundary and remain observable without raw participant content.

### Security and privacy

- Every enrollment, accommodation, retry, attempt, submission, intake, version, binding, preview, download, list, count, event, job, notification, and export operation must enforce server-side action and complete resource-chain authorization.
- Organization, participant, cohort, task, enrollment, attempt, session, submission, object-storage, and version identifiers supplied by a client or event must be treated as untrusted locators and checked against authoritative ownership.
- Upload mechanisms and temporary access credentials must be least-privileged, short-bounded, exact-object and exact-operation scoped, and unusable to read, overwrite, enumerate, or finalize another object or participant's material.
- Intake must defend against type confusion, polyglot content, malicious archives, decompression bombs, parser exploits, active content, malware, path manipulation, duplicate replay, resource exhaustion, and unsafe preview behavior according to approved policy.
- Participant material must be treated as untrusted data at model and tool boundaries; embedded prompts, links, metadata, or instructions cannot grant authority, change configuration, approve tools, or escape the participant/session scope.
- Capability identifiers and material-category configuration must be resolved from trusted frozen state; participant content, filenames, extensions, MIME declarations, URLs, or repository references must not enable a parser, tool, external request, or broader capability.
- Raw submissions, unnecessary filenames, participant attributes, access tokens, storage paths, scanner output, parser output, and signed access mechanisms must not appear in logs, metrics, traces, error responses, analytics, or browser/test artifacts.
- Queries, caches, events, queues, indexes, temporary storage, object storage, previews, content delivery, and background validation must preserve the same organization/activity/participant/session isolation as direct reads.
- Rate, size, count, concurrency, and resource limits must be enforced from approved policy without revealing other participants or enabling limit bypass through retries or multipart uploads.
- Negative tests must cover wrong organization, wrong activity/cohort, participant substitution, forged parent, guessed attempt/version, stale or revoked enrollment, expired accommodation, replayed entitlement, concurrent start, duplicate finalization, object-key substitution, signed-link reuse, malicious content, archive/parser limits, and list/count leakage.

## Acceptance criteria

### `AC-SUBM-1` — Authorized assignment creates one enrollment

- **Given** an administrator has current delegated enrollment permission for an activated cohort
- **And** the participant identity is eligible in the same organization
- **When** the administrator assigns the participant
- **Then** exactly one active enrollment is created and linked to the cohort baseline and task
- **And** the cohort baseline remains unchanged
- **And** the access-control mutation is durably audited.

### `AC-SUBM-2` — Unavailable or cross-scope assignment is rejected

- **Given** the cohort is not activated or available for assignment, the participant belongs to another organization, or the trusted parent chain is inconsistent
- **When** assignment is requested
- **Then** no enrollment is created or modified
- **And** the response does not reveal protected cross-scope existence or content
- **And** a stable non-sensitive rejection category is recorded when required.

### `AC-SUBM-3` — Duplicate and conflicting assignment are safe

- **Given** duplicate or concurrent assignment commands target the same participant and cohort
- **When** they are processed
- **Then** equivalent requests return one enrollment
- **And** conflicting requests do not overwrite or reassign the existing relationship
- **And** no duplicate notification grants additional authorization.

### `AC-SUBM-4` — Suspension or revocation blocks new participant actions

- **Given** an enrollment is active and an authorized administrator suspends or revokes it
- **When** the participant later requests new submission intake or a new attempt start
- **Then** the request is denied within the approved propagation target
- **And** historical accepted versions and attempts remain intact
- **And** any active session is handled by the owning session-lifecycle policy rather than silently deleted.

### `AC-SUBM-5` — Eligible participant can request an attempt

- **Given** the participant has an active enrollment, is within the actual permitted start window, has remaining entitlement, satisfies required submission readiness, and has no conflicting start
- **When** the participant requests the next attempt
- **Then** the service derives the next ordinal and trusted scope
- **And** delegates one idempotent atomic start to the session resolver
- **And** does not accept a client-selected ordinal, owner, timing window, or session binding.

### `AC-SUBM-6` — Limit, timing, or readiness blocks start

- **Given** the attempt limit is exhausted, the start is too early or late, a required accepted submission is absent, the enrollment/cohort is unavailable, or a conflicting session is active
- **When** the participant requests start
- **Then** no new active attempt or session is created
- **And** no entitlement is consumed
- **And** the participant receives a specific safe next action when one exists.

### `AC-SUBM-7` — Atomic start consumes one entitlement

- **Given** the participant is eligible under `AC-SUBM-5`
- **When** the approved atomic start boundary commits successfully
- **Then** one attempt transitions to `Active`, binds to one session/configuration/manifest and the exact required submission-version set under approved decision `PROP-2`, and consumes one entitlement under approved decision `PROP-1`
- **And** the consumption and binding are durably audited
- **And** participant interaction begins only after the commit.

### `AC-SUBM-8` — Pre-commit failure does not consume an attempt

- **Given** an eligible start fails before the atomic session-start commit because of validation, authorization, configuration resolution, persistence, or required audit failure
- **When** the outcome is reconciled
- **Then** no entitlement is consumed and no session is exposed as started
- **And** the participant may safely retry after the blocking condition is corrected
- **And** failure history remains inspectable through a bounded reason category.

### `AC-SUBM-9` — Duplicate or uncertain starts do not double-consume

- **Given** equivalent start requests arrive concurrently or the participant retries after an uncertain response
- **When** the service reconciles the idempotency key and authoritative attempt/session binding
- **Then** at most one attempt becomes active and at most one entitlement is consumed
- **And** equivalent retries return the same status and identifiers
- **And** a conflicting request cannot create a competing session.

### `AC-SUBM-10` — Post-start abort preserves history and needs explicit retry entitlement

- **Given** the atomic start committed and the session later aborted
- **When** the terminal state is recorded
- **Then** the consumed attempt, configuration, manifest, and reason category remain unchanged and inspectable
- **And** another attempt is unavailable unless the baseline limit permits it or an authorized retry entitlement is granted
- **And** a retry entitlement that exceeds the frozen baseline limit requires
  approval by a different currently authorized actor and the durable audit
  mandated by `REQ-ACT-42`
- **And** the original attempt is not reset, deleted, or renumbered.

### `AC-SUBM-11` — Accommodation changes participant timing without changing the baseline

- **Given** an approved policy permits a bounded participant-specific timing accommodation
- **When** an authorized administrator records it with reason and scope
- **Then** the accommodation links immutably to the enrollment and original cohort baseline
- **And** the actual permitted window reflects the approved difference
- **And** the baseline remains unchanged
- **And** a value outside pre-approved policy bounds is rejected unless it
  remains inside non-bypassable bounds and receives approval by a different
  currently authorized actor under a separately permitted fairness-exception
  rule
- **And** authorized fairness review can identify the difference and provenance.

### `AC-SUBM-12` — Unauthorized or stale exception is rejected

- **Given** an accommodation or retry entitlement is absent, expired, revoked,
  unbounded, cross-scope, client-authored, not permitted by policy, or missing
  required approval by a different currently authorized actor
- **When** it is used for eligibility
- **Then** it has no effect
- **And** no non-bypassable boundary is widened
- **And** the rejection is audited without exposing unrelated policy or participant data.

### `AC-SUBM-13` — Permitted material creates an immutable accepted version

- **Given** the participant has an active enrollment and submits material permitted by the frozen requirement and current narrowing policy within the approved window
- **When** receiving and all required validation complete successfully
- **Then** one immutable accepted version is committed with stable identity, ownership, lineage, UTC acceptance time, protected payload reference, and integrity/policy metadata
- **And** the participant receives an accessible confirmation
- **And** downstream consumers can use only the exact accepted version reference.

### `AC-SUBM-14` — Invalid or unsafe material is not accepted

- **Given** material exceeds a permitted count or size, has a disallowed or mismatched type, fails integrity or structural validation, violates archive/parser limits, is unsafe under malware policy, or cannot be validated fail-closed
- **When** intake validation runs
- **Then** no accepted version is created
- **And** the material is unavailable to preview, download, session, model, evidence, evaluation, and review consumers
- **And** the participant receives a bounded reason and safe recovery action.

### `AC-SUBM-15` — Later version preserves earlier history

- **Given** an accepted submission version exists and policy still permits a new version
- **When** the participant submits corrected material
- **Then** a new immutable accepted version is created in ordered lineage
- **And** the prior version and every prior binding remain unchanged
- **And** the history shows which exact version each attempt or evidence consumer used.

### `AC-SUBM-16` — Attempt binds an exact submission-version set

- **Given** all submission requirements for an attempt are satisfied
- **When** the authoritative attempt/session-start boundary commits under approved decision `PROP-2`
- **Then** the attempt records the exact accepted version identifiers
- **And** failure to commit that binding leaves the session unstarted and the entitlement unconsumed
- **And** a later version does not change the bound set
- **And** no consumer resolves the evidence through a mutable `latest` or `current` alias.

### `AC-SUBM-17` — Concurrent finalization is idempotent

- **Given** equivalent or conflicting finalization requests target one intake concurrently
- **When** they are processed
- **Then** equivalent requests return at most one accepted version
- **And** mismatched reuse of the idempotency key reports a conflict
- **And** no partial or duplicate version becomes visible.

### `AC-SUBM-18` — Authorization loss before finalization blocks acceptance

- **Given** a participant begins intake while authorized
- **And** the enrollment is suspended/revoked or another required authorization is lost before final acceptance
- **When** finalization reauthorizes and checks current state
- **Then** acceptance is blocked
- **And** no unauthorized version becomes available downstream
- **And** safe cleanup or quarantine follows policy.

### `AC-SUBM-19` — Participant isolation applies to every access path

- **Given** submissions, attempts, and enrollments exist for multiple participants and organizations
- **When** a participant substitutes an identifier, object key, version, parent reference, access link, filter, page cursor, or event field
- **Then** only that participant's currently authorized records are returned or changed
- **And** inaccessible existence, metadata, totals, and content are not disclosed
- **And** no protected state outside the authorized scope changes.

### `AC-SUBM-20` — Assigned reviewer sees only exact permitted versions

- **Given** a reviewer has an active assignment covering an attempt
- **When** the reviewer opens the case from an assigned review queue or workspace
- **Then** the reviewer sees only the exact versions made available by that workflow and assignment
- **And** the binding and integrity state are distinguishable
- **And** raw storage paths, access secrets, unrelated versions, unassigned participants, and general repository-style browsing are absent.

### `AC-SUBM-21` — Preview and download are artifact-scoped

- **Given** an actor is authorized to preview or download one accepted version
- **When** an access mechanism is issued and used
- **Then** it is limited to the exact artifact, actor/action scope, and approved lifetime
- **And** changing an identifier or reusing the mechanism for another artifact fails
- **And** the access is audited when required by policy.

### `AC-SUBM-22` — Deadline interpretation is unambiguous

- **Given** a submission or attempt-start cutoff is configured and displayed in a named timezone
- **When** requests arrive before, exactly at, and after the boundary
- **Then** the service applies the approved authoritative-time rule consistently
- **And** stored decisions use UTC with unambiguous ordering
- **And** ambiguous/nonexistent local times, client-clock differences, or display formatting do not change the decision.

### `AC-SUBM-23` — Participant states are accessible and responsive

- **Given** the assignment, attempt, or submission view is empty, loading, receiving, validating, accepted, rejected, cancelled, retrying, expired, exhausted, conflicted, permission-changed, or unavailable
- **When** a participant uses keyboard navigation, assistive technology, 400 percent zoom, or a narrow viewport
- **Then** status, deadline, entitlement, version, consequence, error, and recovery remain perceivable and operable
- **And** focus and announcements follow the current task without relying on color, sound, hover, drag, or motion alone
- **And** protected content is not exposed during loading or error transitions.

### `AC-SUBM-24` — Required durable audit gates sensitive transitions

- **Given** an enrollment mutation, accommodation, retry entitlement, or attempt-start commit is classified as `required_durable`
- **When** its audit event cannot be durably accepted
- **Then** the protected transition does not commit
- **And** the user receives a retryable or administrator-action state without a false success
- **And** an operational signal is emitted without raw submission or participant content.

### `AC-SUBM-25` — Historical inspection is complete but minimized

- **Given** an authorized reviewer or investigator inspects an enrollment and attempt
- **When** structured history is loaded
- **Then** the baseline, rule sources, accommodations, entitlements, attempts, session bindings, exact accepted submission versions, changes, actors, reasons, and UTC order are reconstructable
- **And** original records are not overwritten
- **And** audit views use protected references rather than duplicating raw payloads.

### `AC-SUBM-26` — Learning reuse is disabled in the MVP

- **Given** participant submission material is accepted
- **When** memory, agent-learning, calibration, or cross-participant reuse is attempted
- **Then** the material is not written or reused for those purposes
- **And** it remains available only to the authorized assessment/evidence lifecycle
- **And** a client, embedded instruction, or service retry cannot enable Dynamic learning.

### `AC-SUBM-27` — Approved service objectives are measurable

- **Given** authoritative policy and ownership data are available inside the platform boundary under representative load
- **When** enrollment mutation, attempt-eligibility, and metadata-finalization latency are measured under approved decision `PROP-5`
- **Then** each synchronous operation meets the approved 2-second 95th-percentile objective
- **And** byte transfer, end-user network latency, malware scanning, and external delivery are measured separately
- **And** missed objectives are observable without raw protected content.

### `AC-SUBM-28` — Negative submission and attempt coverage gates release

- **Given** submission and attempt behavior is considered for release
- **When** its verification suite runs
- **Then** tests cover wrong organization/activity/cohort/participant, forged parent, guessed ID, revoked enrollment, stale/expired/current-policy-ineligible accommodation, unknown dimension, unbounded value, implicit composition, free-text or unknown reason, requester self-approval, missing lifecycle policy, v1/v2 semantic drift, unsupported browser timezone, replayed retry entitlement, limit exhaustion, exact time boundaries, pre/post-commit failure, duplicate/concurrent start, duplicate finalization, object-key substitution, signed-access reuse, file-policy failures, malicious/archive content, version immutability, list/count leakage, and audit failure
- **And** the feature is not release-ready while an applicable negative case is missing or failing.

### `AC-SUBM-29` — Participant discovers only currently authorized assignments

- **Given** active and inaccessible enrollments exist and an enrollment may become suspended, revoked, or closed
- **When** a participant opens the in-product assignment list or detail view under approved decision `PROP-6`
- **Then** every active enrollment currently visible to the participant is shown with its current state and next permitted action
- **And** stale Active state is not shown after suspension, revocation, or closure propagates
- **And** inaccessible assignments and other participants do not contribute rows, totals, filters, or empty-state details.

### `AC-SUBM-30` — Agent-friendly text categories are configurable and fail closed

- **Given** the MVP enables direct text, UTF-8 plain-text attachments, and Markdown attachments
- **When** the frozen requirement and current organization policy are resolved
- **Then** only categories with approved positive count/size limits and active validation, integrity, safety, preview/download, capability, and lifecycle controls are offered or accepted
- **And** detected content and encoding must match the category even when the filename extension or declared type claims otherwise
- **And** an administrator may narrow the supported set or limits without widening organization policy
- **And** adding a later category creates a new versioned policy contract without changing prior accepted versions or enabling that category for existing frozen requirements.

### `AC-SUBM-31` — Agent reads only exact material allowed by resolved capabilities

- **Given** an attempt has exact accepted Submission versions and the resolved session configuration includes capabilities for only some permitted material categories
- **When** Submission material is prepared for agent or model use
- **Then** the agent receives only exact bound items compatible with the resolved permitted capabilities
- **And** the access retains version and provenance references for evidence
- **And** embedded content, filenames, links, or repository references do not enable another capability or external request
- **And** blocked or omitted items remain isolated and are not represented as inspected evidence.

### `AC-SUBM-32` — Missing required reading capability blocks start safely

- **Given** the frozen task requires agent inspection of a submitted attachment
- **And** the resolved session configuration lacks a compatible permitted reading capability
- **When** the participant requests an attempt start
- **Then** no session starts and no entitlement is consumed
- **And** the participant receives a plain-language explanation and safe correction path
- **And** an assigned reviewer can distinguish preserved material from material the agent inspected
- **And** optional material may remain preserved only when the frozen requirement does not require agent inspection.

### `AC-SUBM-33` — Frozen timing fields have one exact window interpretation

- **Given** a verified baseline contains start, end, deadline, timezone, and an
  optional per-Attempt duration
- **When** Submission and Attempt-start eligibility are evaluated before,
  exactly at, and after each boundary
- **Then** Submission uses `[starts_at_utc, deadline_utc)`
- **And** Attempt start uses `[starts_at_utc, ends_at_utc)`
- **And** per-Attempt duration remains a separately displayed and resolved
  Session limit rather than moving the Attempt-start cutoff
- **And** client time or timezone formatting cannot change the outcome.

### `AC-SUBM-34` — Current policy narrowing invalidates effect without rewriting history

- **Given** an accommodation was permitted by its frozen baseline policy and a
  later exact current Organization policy narrows the applicable bounds
- **When** a new eligibility decision evaluates the accommodation
- **Then** an out-of-current-policy replacement value has no effect
- **And** the original baseline, accommodation, decision, and prior historical
  effect remain unchanged and inspectable
- **And** missing, stale, revoked, unavailable, incompatible, or cross-scope
  current policy fails closed with a bounded non-disclosing state.

### `AC-SUBM-35` — Accommodation dimensions and supersession are deterministic

- **Given** the exact policy enables one of the four approved timing dimensions
  and supplies a bounded result range
- **When** an authorized administrator requests one normalized replacement
  value
- **Then** only a value within both frozen and current policy bounds may become
  effective
- **And** at most one record for that Enrollment and dimension affects the
  decision
- **And** a later approved record supersedes rather than edits or adds to the
  prior value
- **And** relative source bounds are normalized against the verified baseline
  before comparison with an absolute requested result
- **And** an unknown dimension, delta, implicit composition, zero or negative
  duration, or unbounded value is rejected without changing timing.

### `AC-SUBM-36` — Accommodation reasons are bounded and minimized

- **Given** an authorized actor requests an accommodation or fairness exception
- **When** the request is validated
- **Then** it must use one reason category from the exact current policy
- **And** free text, a diagnosis, detailed circumstances, an unknown category,
  or a synthetic-only category in Production is rejected
- **And** Participant projections expose only the approved plain-language
  consequence, not the internal category or protected circumstances.

### `AC-SUBM-37` — Every fairness exception uses a distinct approver

- **Given** a requested value is outside the pre-approved accommodation bounds
  but a separately approved rule permits a fairness exception
- **When** approval or rejection is attempted
- **Then** the requester cannot approve or reject the request even when holding
  another administrative grant
- **And** a different current actor must hold the exact decision action and
  required authentication strength
- **And** the unchanged request, Enrollment, baseline, current policy, bounds,
  and authorization are revalidated at the required-durable commit
- **And** no approver, stale authority, attempted edit, audit failure, or
  uncertain response leaves the exception effective.

### `AC-SUBM-38` — Business expiry and record lifecycle remain distinct

- **Given** an accommodation has `expires_at` and an exact approved lifecycle
  policy
- **When** authoritative service time reaches `expires_at`
- **Then** the record immediately stops affecting new eligibility without
  requiring a materialized expiry transition
- **And** its history remains available until the approved Activity-closure
  lifecycle disposition, subject to hold and dependency-safe preservation
- **And** expiry alone never authorizes deletion or shortening of audit
  retention.

### `AC-SUBM-39` — Effective timing rolls out without changing v1 meaning

- **Given** an existing strict v1 Enrollment or **My work** consumer and a new
  v2 consumer
- **When** effective timing is introduced
- **Then** v1 retains its existing fields and baseline meaning
- **And** v2 exposes separate baseline and effective timing, authoritative
  evaluation time, eligibility state, and minimized accommodation consequence
- **And** both versions remain available while the production SPA migrates
- **And** an unsupported version fails explicitly rather than being silently
  interpreted with another schema.

### `AC-SUBM-40` — Enrollment request limits aggregate across API replicas

- **Given** two API replicas use the same approved deployment-wide quota
  policy and one authenticated actor in one Organization sends requests for
  the same Enrollment surface through both replicas
- **When** the combined requests consume the configured fixed-window budget
- **Then** at most that one shared budget is permitted across both replicas
- **And** the next request returns `429`, `enrollment.rate_limited`, an accurate
  positive `Retry-After`, and `Cache-Control: no-store`
- **And** another actor, Organization, or surface retains an independent
  budget without exposing either partition in telemetry or error content.

### `AC-SUBM-41` — Shared-admission uncertainty fails closed without becoming exhaustion

- **Given** an authenticated Enrollment request and unavailable, timed-out, or
  policy-mismatched shared admission state
- **When** the service cannot prove that a global permit was acquired
- **Then** no protected Enrollment query or mutation begins
- **And** the response is `503`/`enrollment.unavailable` with
  `Cache-Control: no-store`, not `429`
- **And** no replica-local fallback can grant the request
- **And** recovery uses the same authoritative database clock and policy so a
  retry cannot create a second window or widen the configured budget.

## Edge and failure cases

| Case | Required outcome |
| --- | --- |
| Cohort is activated with no participants | Permit later authorized enrollment without changing the baseline |
| Same participant is assigned twice to the same cohort | Return the existing relationship for an equivalent command; do not duplicate entitlement |
| Participant is reassigned to another cohort | Create a separately authorized enrollment; preserve the old baseline, attempts, submissions, and history |
| Attempt limit or timing policy narrows after enrollment | Evaluate new starts against current permitted policy without rewriting the cohort baseline or prior attempts; surface conflicts for authorized review |
| Two devices start simultaneously | Commit at most one active attempt/session and one entitlement consumption |
| Start response times out | Reconcile idempotency and authoritative session binding before showing retry |
| Resolver freezes configuration and the session immediately aborts | Preserve a consumed Aborted attempt under approved decision `PROP-1`; require baseline allowance or explicit retry entitlement for another try |
| Upload loses connectivity | Keep the intake non-accepted; provide safe resume/retry only when the approved protocol verifies scope and bytes |
| Complete payload receives a server receipt before the cutoff but validation finishes afterward | Apply the receipt-time rule in approved decision `PROP-3`; accept only if validation passes and authorization remains current |
| Complete payload receives its server receipt at or after the exclusive cutoff | Reject it as late under approved decision `PROP-3`; validation or retry does not change the receipt time |
| Enrollment is suspended/revoked before finalization | Reauthorize and block acceptance; do not accept through stale authorization |
| File extension and detected type disagree | Reject or quarantine under policy; do not trust the extension or client content type |
| Required attachment category has no compatible resolved agent capability | Block start without consuming entitlement; identify the unsupported requirement and safe correction path |
| Optional attachment is preserved but not agent-readable | Label it as not inspected, exclude it from agent-consumed evidence, and preserve exact-version reviewer access when authorized |
| Submission contains a repository URL or embedded retrieval instruction | Preserve it as untrusted inert content; do not fetch, clone, browse, execute, or enable a capability from the content |
| Archive expands beyond policy or contains unsafe paths | Stop processing, keep it unavailable downstream, and record a bounded rejection |
| Scanner, parser, storage, policy, or audit dependency is unavailable | Fail closed at the applicable acceptance/start boundary and preserve safe retry/reconciliation state |
| New version is accepted after a session binding | Preserve both versions; do not alter the bound set, and apply the explicit in-session rule if one exists |
| Historical payload is lawfully deleted | Preserve honest unavailable/degraded metadata and required references under policy; do not substitute a later version |
| Access link is forwarded | Reauthorize the receiving actor and exact artifact/action; link possession alone grants nothing |

## Dependencies and rollout

### Dependencies

- Approved authentication, authorization, resource-isolation, scoped-query, revocation, commit-time reauthorization, and audit-durability behavior from [`auth-resource-isolation.md`](auth-resource-isolation.md), [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md), and [ADR-003](../../architecture/decisions/ADR-003-authorization-audit-persistence.md).
- Activated cohort baseline, versioned task/submission requirement, attempt limit, timing/deadline rules, participant-accommodation bounds, and immutable source references from [`assessment-setup.md`](assessment-setup.md) and [ADR-004](../../architecture/decisions/ADR-004-assessment-activation-baseline-and-atomicity.md).
- Trusted participant identities and organization relationships supplied by an approved identity/membership boundary.
- Approved organization material-category, file-admission, malware, parser/archive, rate/size/count, retention, deletion, legal-hold, consent, and export policies. The initial direct-text, UTF-8 plain-text, and Markdown categories follow approved decision `PROP-4` and `REQ-SUBM-44`–`REQ-SUBM-45`; each deployment still requires explicit bounded configured values before enablement.
- Protected payload storage and access delivery that can enforce exact organization/participant/artifact scope, durable association, integrity verification, quarantine, and lifecycle policy.
- Atomic resolved-configuration, initial-manifest, Attempt transition, entitlement consumption, exact Submission-version binding, Session readiness/binding, and required-audit start contract from [`resolved-session-configuration.md`](resolved-session-configuration.md), ADR-001, ADR-003, and approved [ADR-005](../../architecture/decisions/ADR-005-atomic-attempt-start-and-submission-binding.md).
- Versioned capability identifiers and compatibility resolution that can prove whether the frozen session may read each required material category without enabling capabilities from participant content.
- Active-session and terminal-state behavior from [`session-text-lifecycle.md`](session-text-lifecycle.md).
- Evidence and evaluation consumers that accept exact protected submission-version references rather than mutable aliases.
- Human review and release behavior from [`review-result-release.md`](review-result-release.md), including assigned review queue/workspace scope and review/release authorization.
- Notification delivery integration only if a later external channel receives a separately approved delivery and privacy contract under the disposition of `Q-6`.

### Rollout

- Enrollment, attempt control, and versioned submission preservation are mandatory MVP workflow boundaries, not optional customer-facing feature flags.
- Do not enable participant assignment until cohort activation/baseline validation and authorization enforcement are available.
- Do not enable session start until attempt idempotency, consumption, uncertain-outcome reconciliation, resolved configuration, manifest, session binding, exact submission-version binding, and durable-audit boundaries work together.
- Enable agent-friendly formats first: direct text, validated UTF-8 plain-text attachments, and validated Markdown attachments. Do not enable any attachment category until its approved count/size/type/content/malware/parser policy, compatible agent-reading capability, quarantine behavior, protected storage, preview/download controls, lifecycle references, and negative tests are active.
- Add later material categories through a new versioned category-policy contract and compatible capability mapping; do not reinterpret prior frozen requirements, accepted versions, bindings, or evidence.
- Seeded or migrated enrollments, attempts, and submission versions without unambiguous ownership, baseline linkage, immutable version identity, or integrity state must be quarantined from new starts and evidence use.
- Apply the approved [Submission and Attempt interaction specification](../../ui-ux/submission-attempt.md). Frontend implementation, automated accessibility checks, and Playwright evidence across desktop and narrow states remain delivery gaps.
- Rollout must include failure injection for audit, storage, validation, session-resolution, duplicate, concurrency, timeout, and revocation paths before participant use.

### Observability

Track at minimum:

- Enrollment creation, duplication, rejection, suspension, revocation, and notification outcomes by bounded category.
- Attempt eligibility, started, blocked, consumed, aborted, deduplicated, reconciled, and retry-entitlement counts.
- Limit-exhausted, timing-window, submission-readiness, stale-authorization, and conflicting-start blocks.
- Intake started, cancelled, failed, rejected, accepted, deduplicated, and abandoned counts by bounded material/policy category.
- Receiving, validation, and metadata-finalization latency separately from end-user network transfer.
- Quarantine backlog/age, scanner/parser dependency health, cleanup failure, and protected-storage association failure.
- Cross-organization, cross-participant, parent-mismatch, object-key substitution, signed-access reuse, and enumeration denials.
- Required-audit acceptance failures and protected transitions blocked by them.
- Accepted versions without required ownership, integrity, or lineage fields; the release target is zero.
- Attempts with zero or multiple active session bindings; the release target is zero.

Metrics, logs, traces, and alerts must use bounded labels and must not contain raw submissions, unnecessary filenames, participant attributes, credentials, access URLs, object keys, scanner/parser output, or unrestricted identifiers.

## Open questions

`Q-1`–`Q-7` were resolved on 2026-08-06. `Q-8` was resolved as approved
`PROP-8`, and the accommodation-readiness questions were resolved as approved
`PROP-9`–`PROP-15`, on 2026-08-23.
Deployment-specific count, size, timing-bound, and production reason values
remain required exact policy configuration; they are not unresolved product
semantics and are not inferred from synthetic fixtures.

## Proposed defaults requiring approval

None.

## Approved decision dispositions

| Decision | Approved disposition | Rationale / consequence |
| --- | --- | --- |
| `Q-1` / `PROP-1` | Consume entitlement only when the atomic session-start boundary commits; a post-commit abort remains consumed and another try requires baseline allowance or a separately authorized retry entitlement. | Avoids penalizing pre-start failures while preserving auditable executions and preventing silent resets. |
| `Q-2` / `PROP-2` | Bind the exact accepted version set in the authoritative attempt/session-start consistency boundary; fail start without consumption if binding cannot commit. Later in-session versions require the frozen rule and explicit ordered bindings. | Prevents a moving evidence target. [ADR-005](../../architecture/decisions/ADR-005-atomic-attempt-start-and-submission-binding.md) governs the implementation boundary. |
| `Q-3` / `PROP-3` | Use an exclusive server-authoritative cutoff: atomic commit time for attempt start and complete-payload receipt time for attachments, with no implicit grace period. | Produces testable ordering independent of client clocks. |
| `Q-4` / `PROP-4` | Support direct text and agent-friendly attachments first: validated UTF-8 plain text (`.txt`) and Markdown (`.md`). Govern all categories through a configurable, versioned, extensible, fail-closed policy; no category is enabled without complete approved bounds and controls. | Meets the text-plus-attachment MVP while keeping formats safe, capability-aware, and extensible. Archives and other categories are not implicitly enabled. |
| `Q-5` / `PROP-5` | Require a 2-second 95th-percentile objective for synchronous enrollment mutation, attempt eligibility, and accepted-version metadata finalization under the documented preconditions and exclusions. | Aligns participant-facing readiness operations while measuring transfers and external dependencies separately. |
| `Q-6` / `PROP-6` | Require in-product assignment discovery; defer email, SMS, calendar, and other external channels until their delivery and privacy contracts are approved. | Preserves a complete authorized MVP path without inventing provider behavior. |
| `Q-7` / `PROP-7` | Treat accommodation and retry-entitlement grants, changes, revocations, and consumption as `required_durable` mutation-coupled audit events under ADR-003. | Preserves fairness-relevant exception history and blocks unaudited mutation. |
| `Q-8` / `PROP-8` | Keep the first Enrollment slice's implemented request-limit contract replica-local and move replica-independent/shared/gateway-enforced per-actor/Organization Enrollment quota to the separately tracked follow-on task. ADR-006 coarse gateway limits are not that actor-scoped product quota. | Closes the first slice against its implemented contract without inventing a shared store, while retaining an explicit owner and multi-replica verification requirement for the residual. |
| `PROP-9` | Map Submission to `[starts_at_utc, deadline_utc)` and Attempt start to `[starts_at_utc, ends_at_utc)`; keep per-Attempt duration independent of the latest start instant. | Makes the frozen timing model and exclusive `PROP-3` boundary directly implementable without importing Session-duration behavior. |
| `PROP-10` | Evaluate new timing decisions against the frozen baseline accommodation bounds and exact current Organization policy; current policy may narrow future effect without rewriting history. | Preserves Cohort fairness while enforcing current non-bypassable limits and failing closed on stale or unavailable policy. |
| `PROP-11` | Support `submission_deadline_utc`, `attempt_start_not_before_utc`, `attempt_start_before_utc`, and `per_attempt_duration_seconds` as one-value replacement dimensions; normalize absolute or relative policy-source bounds against the verified baseline before evaluation; permit at most one current effect per Enrollment/dimension and supersede rather than compose or edit. | Provides deterministic calculation, concurrency constraints, and audit reconstruction without implicit delta addition. |
| `PROP-12` | Require an exact policy allowlist of bounded reason categories; collect no free text, diagnosis, or detailed circumstances, and treat synthetic fixture categories as non-Production. | Minimizes sensitive data and prevents an example vocabulary from becoming policy. |
| `PROP-13` | Require a different currently authorized requester and approver for every fairness exception; approval/rejection cannot edit the request and must revalidate and durably audit at commit. | Removes implicit self-approval and makes absence or staleness of the approval route fail closed. |
| `PROP-14` | Retain accommodation and decision history for 365 days after Activity closure under the approved lifecycle matrix; keep audit metadata under its 730-day rule and related idempotency outcomes under their 90-day rule; treat business expiry as eligibility only. | Preserves fairness reconstruction and holds without turning `expires_at` into deletion authority. |
| `PROP-15` | Preserve strict v1 Enrollment/**My work** baseline meaning; introduce parallel strict v2 projections and routes for separate baseline/effective timing and migrate the SPA without retiring v1 in this slice. | Avoids breaking strict or mixed-version consumers and prevents silent semantic reinterpretation. |
| Discussion decision | Keep Submission broader than attachment; support direct text plus permitted attachments; gate agent access by exact binding and resolved capabilities; use assigned review queues/workspaces rather than a general review repository; keep external repository access deferred. | Separates preservation, agent access, and reviewer authorization while retaining the approved MVP tool boundary. |

## Traceability

| Requirement/AC | Implementation | Automated verification | Playwright/manual evidence | Status |
| --- | --- | --- | --- | --- |
| `REQ-SUBM-1`–`REQ-SUBM-8`, `REQ-SUBM-43`, `AC-SUBM-1`–`AC-SUBM-4`, `AC-SUBM-29`, `PROP-6` | Production Enrollment aggregate, activated-Cohort port, assignment/lifecycle HTTP and React, and in-product **My work** discovery. `AC-SUBM-4` is implemented at the Enrollment decision boundary and as Submission intake denial for non-active Enrollment; Attempt start remains unimplemented. External delivery remains deferred under `PROP-6`. | Domain, PostgreSQL assignment/close/reassign, contract catalog, HMAC-scoped cursors, and HTTP CSRF/unknown-member/concealed-detail/rate-limit tests | Component empty/active/suspended/conflict/confirm states. Authenticated Playwright desktop/narrow/both-theme evidence exists; 400% zoom remains a recorded gap | Partial |
| `REQ-SUBM-9`–`REQ-SUBM-14`, `REQ-SUBM-50`–`REQ-SUBM-56`, `AC-SUBM-11`, `AC-SUBM-12`, `AC-SUBM-22`, `AC-SUBM-33`–`AC-SUBM-39`, `PROP-3`, `PROP-9`–`PROP-15` | Submissions timing/accommodation domain, `0046` records plus additive `0047` complete Enrollment parent FK, parallel v2 Enrollment/**My work** projections with `current_accommodations[]`, and SPA timing UI. Production positive grants remain fail-closed until Configuration supplies an exact current Organization policy; Assessment frozen policy snapshots are still adapter-defaulted when omitted. Attempt start and retry remain out of this slice. | Domain coordinators; PostgreSQL `0046`/`0047` upgrade, isolation, parent-scope negative inserts, concurrency, idempotency, session-revocation, audit-rollback, and append-only tests; contract catalog/OpenAPI `$ref`/C#/TypeScript v2 fixtures including decide/revoke; HTTP CSRF/unknown-member/unauthenticated timing negatives; Enrollment and **My work** vitest | Component timing/accommodation states exist. Authenticated Playwright MCP desktop/narrow/400%/theme evidence is not yet captured for this slice | Partial |
| `REQ-SUBM-15`–`REQ-SUBM-23`, `AC-SUBM-5`–`AC-SUBM-10`, `PROP-1` | Attempt entitlement/ordinal model, idempotency, concurrency, fairness-exception approval, and atomic start/reconciliation contract — approved [ADR-005](../../architecture/decisions/ADR-005-atomic-attempt-start-and-submission-binding.md), [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md), [MVP architecture](../../architecture/mvp-architecture.md), and [Submission and Attempt interaction specification](../../ui-ux/submission-attempt.md); implementation TBD | Limit, readiness, pre/post-commit fault injection, separately authorized exception approval/rejection, mandatory requester/approver separation, retry, timeout, and multiple-device/concurrent start tests | Eligible, confirmation, approval-required, exception decision/reconciliation, starting, active, exhausted, failure, uncertain/reconciled, and aborted states | Gap |
| `REQ-SUBM-24`–`REQ-SUBM-35`, `REQ-SUBM-44`–`REQ-SUBM-48`, `AC-SUBM-13`–`AC-SUBM-18`, `AC-SUBM-30`–`AC-SUBM-32`, `PROP-2`, `PROP-4` | Production Submission intake and immutable accepted-version lineage are implemented on **My work**: frozen Task plus environment-scoped Organization material policy (Production/Staging fail closed until Configuration publishes a versioned source), private artifact store, receiving/validating/cancel/finalize, PostgreSQL-authoritative acceptance, catalogued v2 version-detail/preview contracts, and inert Participant preview/download. Accepted payload cleanup deletes the exact stored object version only after `DeleteAsync` succeeds, then appends a disposition; missing exact versions fail closed as terminal work with `exact_artifact_version_unavailable`. Shipped `0055` backfills already-queued cleanup work (and historically deletes unbackfillable pending/leased jobs). Additive `0057` reconstructs recoverable terminal provenance from remaining unversioned items, uniquely indexes dispositions, and the processor completes duplicate claimed work as a no-op when a disposition already exists. `0056` makes accepted-cleanup scan-cursor advancement replica-safe so held/open pages cannot starve later eligible artifacts across API replicas. Retention still uses the approved 365-day OPS default (`IndependentlyResolvedFromOwner=false`); Configuration/Organization-narrowed versioned lifecycle policy is not yet consumed. Assessment does not yet persist Activity closure, so Production enqueue remains fail-closed on that clock. Attempt/Session binding, Agent-reading, and assigned-review remain unimplemented. | Domain policy/intake/capability/cleanup including Activity-closure eligibility, delete-failure/exact-version, missing-version fail-closed, duplicate-claim no-op, scan-cursor CAS, and held-page scan-cursor regressions; PostgreSQL `0048`–`0057` parent-scope, immutability, concurrent finalize, durable work exact artifact version backfill, unbackfillable reconstruction, unique dispositions, lifecycle hold/disposition, and `cleanup_accepted` work kind; SeaweedFS conditional/presign/scope/restore/expiry/exact-version delete; HTTP CSRF/admission/unauthenticated `no-store` including version detail; React **My work** intake/history/permission-loss/multi-item preview/reconciling-refresh tests; Dapper accepted-version list/find materialization | Component empty/editing/submit/cancel/permission-loss/history-when-unavailable/multi-item/reconciling states. Authenticated Playwright on rebuilt compose: local prepare, confirm, accept, later version, inert preview, session download, 360px, CSS `zoom: 4` (sticky chrome dominates; not a WCAG reflow pass). Live cancel and live receiving/validating remain gaps | Partial |
| `REQ-SUBM-36`–`REQ-SUBM-42`, `REQ-SUBM-49`, `AC-SUBM-19`–`AC-SUBM-21`, `AC-SUBM-24`–`AC-SUBM-26`, `PROP-7` | Participant exact preview/download uses session use-time authorization, a five-minute actor/enrollment/version/item capability, required durable audit before disclosure, `no-store`, and catalogued v2 version-detail plus preview schemas. Assigned-review, administrator raw-content browsing, Evidence, and Release remain unimplemented. | Preview audit-before-disclosure and audit-unavailable fail-closed tests; unauthenticated preview/download/version-detail `no-store`; SeaweedFS download-presign expiry and scope isolation; catalog/OpenAPI/C#/TypeScript parity for `AcceptedVersionDetailV2` and `ProtectedItemPreviewV2` | Permission-loss focus and inert viewer component evidence exists; rebuilt-compose inert preview of Version 2 captured 2026-08-25. Assigned-review workspace and administrator scoped history remain Gap | Partial |
| UX/accessibility requirements, `AC-SUBM-23` | Production **My work** Submission section: local preparation, Submit-version confirmation, cancel, version history, inert preview, session download, drop zone plus file picker, reconciling copy with **Refresh assignment**, per-item preview when a version has more than one item | Component keyboard/focus/dialog/error-summary/multi-item/reconciling tests | Authenticated Playwright desktop, dark theme, 360px, 320px, and CSS `zoom: 4` screenshots exist under `.playwright-mcp/` (2026-08-25). Sticky chrome at 400% zoom is not a reflow pass. Live cancel and live receiving/validating remain gaps | Partial |
| Performance/reliability requirements, `REQ-SUBM-57`–`REQ-SUBM-58`, `AC-SUBM-9`, `AC-SUBM-17`, `AC-SUBM-27`, `AC-SUBM-40`–`AC-SUBM-41`, `PROP-5`, `PROP-8` | Enrollment mutation idempotency, coordinator consistency, bounded mutation/request-limit telemetry, replica-local defense in depth, and PostgreSQL-backed replica-independent request admission are implemented. Policy `window_seconds` is frozen at the deployed value (minimum 10 seconds) so a duration change cannot reset the shared budget; a valid longer `0044` window is preserved. Counters store indexed `expires_at` for bounded `SKIP LOCKED` cleanup. Attempt eligibility, metadata finalization, hosted load labs, and a true two-OS-process lab remain TBD. | Two-port in-process aggregate-budget, database-clock window/cleanup, frozen-window rejection, expiry-index cleanup, mutated-`0044` longer-window freeze-in-place, live old-window refusal, overlap-horizon refusal until the aligned deployed-policy bucket ends (including non-divisible `12 → 20`), policy-mismatch, lock-timeout fail-closed, restart, recovery, and representative admission-latency tests plus HTTP 429/503 contract tests | Existing recoverable 429 UI remains applicable; shared-admission 503 uses the existing protected unavailable state and adds no new interaction contract | Partial |
| Security/privacy requirements, `AC-SUBM-14`, `AC-SUBM-18`–`AC-SUBM-21`, `AC-SUBM-26`, `AC-SUBM-28` | Scoped intake/query, complete parent-scope FKs, S3 organization-key isolation, inert UTF-8 validation, no transferable storage URLs in the browser, allowlisted intake telemetry bands | Isolation, capability mismatch/expiry, malware-scanner fail-closed, HTTP negatives, artifact scope tests | Permission-denied and unavailable preview component evidence; full malicious-content Playwright suite remains outstanding | Partial |
