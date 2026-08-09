# Result and Release interaction specification

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Approved |
| **Owner** | Product Lead |
| **Approvers** | Product Lead, UI/UX reviewer, Architecture Lead, Security/Privacy reviewer |
| **Version** | 0.1 |
| **Prepared date** | 2026-08-09 |
| **Approved date** | 2026-08-09 |
| **Approval reference** | Product Lead approved `PROP-UI-REL-1`–`PROP-UI-REL-12` on 2026-08-09 after UI/UX, architecture, security/privacy, traceability, and repository-consistency review |
| **Audience** | Product, design, frontend, backend, security/privacy, QA, and implementation reviewers |
| **Governs** | Release work, exact Result preview, explicit Release confirmation and reconciliation, Participant pre-release and released Result views, correction visibility, notification handoff, accessibility, responsive behavior, and protected-content interaction for the P0 assessment Campaign |
| **Journey** | [`JRN-MVP-7`](activity-campaign-journey.md#jrn-mvp-7-release-and-view-result) |

This approved UI/UX contract is authoritative for the governed interaction
concerns. Product meaning, observable behavior, and technical realization
remain governed by the approved product documents, feature specifications,
operational defaults, and ADRs in their respective areas of concern.

## Purpose and intended outcome

This experience begins after one immutable `Approved` Review decision has
created one validated Result that is **Result ready · Not released**. It gives a
separately authorized actor a protected place to verify the exact Result,
audience, integrity, policy, and current visibility state before deliberately
releasing it. It also gives a Participant a neutral pre-release Results area and,
after Release, a clear view of only their own permitted participant-facing
outcome.

The experience is successful when:

- Release work shows only exact Results the actor may currently inspect or
  release, without treating approval, assignment, a URL, or a queue item as
  Release authority;
- the immutable Result preview consumed from Review work is visibly the same
  payload being released and cannot be edited, reconstructed, or silently
  switched on this surface;
- the actor can distinguish `Result ready · Not released`, `Release blocked`,
  `Releasing Result`, `Checking Release status`, `Released`, `Corrected`, and
  `Result unavailable` without relying on color or timing guesses;
- the Release confirmation names the exact Result, permitted Participant
  audience, policy consequences, separation-of-duties state, and effective
  action without exposing unnecessary protected identifiers;
- duplicate, concurrent, stale, revoked, audit-failed, or uncertain Release
  attempts reconcile to authoritative state and never imply or repeat Release;
- before Release, the Participant sees only the policy-permitted neutral state
  and no Evaluation, Review decision, score, reviewer activity, or predicted
  timing;
- after Release, the Participant sees only the exact allowlisted Result fields,
  authoritative Release time, current correction state, and permitted next
  action;
- a corrected Result is identified as updated with its new effective time, while
  prior Results and Releases are not overwritten or exposed unless policy
  permits them;
- notification failure never changes authoritative Result visibility, and
  notification content does not contain the Result or bearer authority; and
- desktop, narrow, keyboard, screen-reader, reduced-motion, and 400 percent zoom
  experiences preserve the same authority, content hierarchy, state meaning,
  and recovery path.

Observable Result and Release behavior remains governed by the approved
[Human review and Result Release](../requirements/features/review-result-release.md)
feature specification.

## Authority and upstream sources

| Concern | Governing source |
| --- | --- |
| Evaluation, Human revision, Review decision, Result, Release, isolation, and immutable-history meaning | [Concept model](../product/concept-model.md) |
| MVP Human review, explicit Release, Participant Result, and deferred capabilities | [MVP scope](../product/mvp-scope.md#mvp-validation-slice) |
| Result construction, Release authorization, visibility, correction, privacy, accessibility, performance, and acceptance criteria | [Human review and Result Release](../requirements/features/review-result-release.md) |
| Authentication, complete-chain authorization, revocation, denial, and sensitive access | [Authorization and resource isolation](../requirements/features/auth-resource-isolation.md) |
| Frozen Result/release policy, configuration provenance, and historical reconstruction | [Resolved Session configuration](../requirements/features/resolved-session-configuration.md) |
| Protected-data lifecycle and application-session defaults | [MVP operational defaults](../requirements/mvp-operational-defaults.md) |
| Platform journey, information architecture, shared states, terminology, accessibility, and responsive baseline | [Activity journey and Campaign information architecture](activity-campaign-journey.md) |
| Immutable decision, preview, and **Result ready · Not released** handoff | [Evidence, Evaluation, and Human Review interaction specification](evidence-evaluation-human-review.md#approved-handoff) |
| Result envelope, Release transaction, authoritative Participant reads, notification, correction, lifecycle, and recovery | [Human review, Result, and Release contract](../architecture/review-result-release-contract.md) |
| SPA/server authority, authorization audit, and detailed MVP contracts | [MVP architecture](../architecture/mvp-architecture.md), [ADR-002](../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md), [ADR-003](../architecture/decisions/ADR-003-authorization-audit-persistence.md), and [ADR-009](../architecture/decisions/ADR-009-mvp-session-evaluation-review-contracts.md) |

## Scope and boundaries

### In scope

- Show only currently authorized individual Release work, bounded status counts,
  ordering, filters, and permitted actions.
- Consume the exact immutable `Approved` Review decision and validated Result
  created by Review work; show human-readable Activity, Participant, task,
  Attempt, and Result context only as currently authorized.
- Present the exact participant-facing Result preview, Result version and digest
  state, Release eligibility, integrity, visibility audience, frozen/current
  narrowing policy, lifecycle state, and separation-of-duties requirement.
- Confirm one explicit, immediate Release of one exact Result to one
  server-derived permitted audience.
- Present pending, duplicate, uncertain, conflict, stale, authorization,
  integrity, audit, projection-reconciliation, and notification states without
  weakening authoritative visibility.
- Present immutable Release and correction history to authorized actors through
  progressive disclosure and bounded human-readable metadata.
- Present a neutral Participant Results area before Release and the exact
  current permitted Result after Release.
- Present correction update status, current effective time, and lawful
  unavailability without silently overwriting or denying historical Release.
- Define availability-only notification feedback, keyboard, focus,
  screen-reader, reflow, content, failure/recovery, and security/privacy behavior
  for these surfaces.

### Out of scope

- Creating, editing, retrying, replacing, selecting, or reviewing an Evaluation;
  preparing or submitting a Human revision; or recording a Review decision.
- Editing the immutable Result, Result schema, participant-facing field
  allowlist, rubric, Activity, frozen policy, audience, lifecycle policy,
  Submission, transcript, Evidence, configuration, or manifest.
- Adding `Release Result` to the upstream Evidence, Evaluation, and Human Review
  surface or combining approval and Release into one action.
- Bulk, cohort-wide, scheduled, embargoed, automatic, or external-channel
  Release; public links; audience selection; certificates; badges; downloadable
  reports; gradebook/HR/LMS integration; or third-party sharing.
- General Release staffing, workload balancing, organization-wide Result search,
  or unrestricted participant/outcome browsing.
- Built-in Participant appeal or review-request intake. A configured support
  route may be shown as information but does not create an appeal.
- Authoring or adjudicating a correction. This specification presents a
  server-permitted correction-start handoff, Release of a new correction Result,
  and corrected Participant state; the correction follows a new linked review
  lifecycle.
- Showing internal Evaluation confidence or uncertainty, provisional feedback,
  Evidence selection, reviewer identity or notes, hidden rubric content,
  prompts, model/provider internals, credentials, or unrestricted lineage to the
  Participant.
- In-product print, copy, download, or export unless a separately approved
  capability and interaction specification enables it.
- Shared visual tokens and implementation mechanics owned by the future design-
  system foundation and frontend architecture.

## Actors and visible capability boundaries

| Actor or service | Permitted interaction | Boundary shown in the interface |
| --- | --- | --- |
| Release-authorized Reviewer or Activity administrator | Within current delegation, inspect an exact approved decision and Result, satisfy Release requirements, and explicitly Release when permitted | Approval or Activity administration alone is not Release authority; cannot edit Result content or choose a wider audience |
| Approving Reviewer without Release authority | See **Result ready · Not released** and a safe handoff only when separately permitted | Does not see or activate a Release control; the interface does not reveal who can Release or when they will act |
| Actor subject to separation of duties | Inspect only the metadata and preview their content permission allows | Cannot Release a Result they approved when frozen/current policy requires another actor |
| Activity administrator or correction-authorized actor | Within separate capability, inspect bounded Release state/history and start the server-permitted correction workflow | Cannot overwrite a released Result, edit history, or obtain raw review content through operational authority |
| Participant | See a neutral Results state before Release and, after Release, only their own current permitted Result and bounded Release/correction metadata | Cannot see internal Evaluation/review state, other Participants, unreleased/prior content, or protected lineage unless Result policy explicitly permits it |
| Review/release service | Return authoritative Result eligibility, permitted actions, visibility, Release, correction, lifecycle, and reconciliation state | Client state, role labels, routes, notification links, identifiers, and previews do not establish authority or visibility |
| Notification service | Deliver availability-only work emitted after committed Release under an approved channel policy | Notification does not contain Result content, score, protected reason, or bearer access and cannot grant visibility |
| Audit/compliance reviewer | Within explicit delegated scope, inspect minimized Release, correction, access, and export history | Audit access does not imply Participant Result, raw Evidence, reviewer-note, or unrestricted export access |

## Approved interaction decisions

The following interaction decisions were approved on 2026-08-09. Stable
`PROP-*` identifiers are retained for traceability and future supersession.

| ID | Approved decision | Rationale and consequence |
| --- | --- | --- |
| `PROP-UI-REL-1` | Use a capability-scoped **Release work** destination separate from **Review work** and from the Participant **Results** area. | Preserves independent approval/Release authority and prevents administrative browsing from becoming Result access. |
| `PROP-UI-REL-2` | Keep six independent visible state tracks: access, Result eligibility/integrity, Release command, Participant visibility, correction/lifecycle, and notification delivery. | Prevents one generic status from collapsing authority, immutable content, visibility, history, and asynchronous delivery. |
| `PROP-UI-REL-3` | Use one stable Release hierarchy: case header, urgent status, immutable Result preview, audience/policy, decision/provenance, confirmation, then Release/correction history. | Keeps the exact content and consequence ahead of secondary lineage while preserving a consistent reading order across viewports. |
| `PROP-UI-REL-4` | Consume the exact immutable Result payload/digest from the approved handoff and provide no edit or audience-selection control on Release work. | Prevents preview/Release drift and keeps Result construction and audience policy server-owned. |
| `PROP-UI-REL-5` | Use one action labeled **Release Result** with a deliberate confirmation naming the exact Result, Participant audience, visibility consequence, and applicable separation-of-duties or recent-authentication requirement. | Makes the sensitive transition explicit without inventing bulk or scheduled behavior. |
| `PROP-UI-REL-6` | After an uncertain response, show **Checking Release status**, reconcile authoritative Release/visibility by idempotency context, and do not offer or perform automatic resubmission. | Avoids duplicate Release and false failure/success claims. |
| `PROP-UI-REL-7` | Before Release, show Participants one policy-permitted neutral state: **Result not available** with only a safe next action; do not expose review stage, score, reviewer activity, or predicted timing. | Prevents indirect pre-release disclosure and misleading timing promises. |
| `PROP-UI-REL-8` | Use a Participant Result hierarchy of availability/update status, Activity and task context, outcome summary, permitted criterion outcomes, Participant-facing feedback, Release metadata, and permitted support route. | Prioritizes the outcome and meaning while excluding internal lineage and operational detail. |
| `PROP-UI-REL-9` | Identify a corrected current Result with **Result updated**, the new effective Release time, and a concise policy-owned update explanation; do not show prior Result content by default. | Communicates changed truth without silently overwriting history or widening Participant access. |
| `PROP-UI-REL-10` | Represent lifecycle or relationship restrictions as **Result unavailable** while preserving a bounded statement that availability changed; never imply the historical Review decision or Release did not exist. | Preserves historical truth and non-disclosing access behavior. |
| `PROP-UI-REL-11` | Treat notification as availability-only. Release success is complete independently of notification, and a notification link opens the normal authenticated Results route. | Prevents delivery failure from changing visibility and avoids leaking content or bearer authority. |
| `PROP-UI-REL-12` | Omit appeal, download, print, copy, export, public sharing, prior-Result detail, and external-channel controls unless a separately approved policy and interaction specification enables them. | Keeps the MVP bounded and avoids implying authorization or workflows that have not been specified. |

## Information architecture

### Entry points

Release-authorized actors may enter Release work from:

- the capability-resolved **Open Release work** handoff on an approved Review
  case;
- the **Assigned Review and Release work** priority band on Home;
- an authorized Activity or Participant operational context; or
- an authorized deep link that resolves current scope before protected content
  renders.

Participants enter **Results** from their Participant navigation, Activity/task
context, or a normal authenticated availability notification. A notification,
route, Result identifier, prior page view, cached status, or deep link is only a
locator. The destination reauthorizes the complete ownership and visibility
chain before showing protected content.

### Release work hierarchy

```text
Release work
├── Authorized individual Results
│   ├── Status and permitted next action
│   ├── Activity, Participant, task, and ready time
│   └── Bounded filters and ordering
└── Approved decision / exact Result
    ├── Case header and urgent authoritative status
    ├── Immutable Participant-facing Result preview
    ├── Audience, policy, integrity, and separation of duties
    ├── Approved decision and bounded provenance
    ├── Release confirmation or reconciliation
    └── Release and correction history
```

Release work is not a general Result repository. Queries, counts, filters, and
ordering are scoped before materialization. Each row names the owning state,
for example **Result ready · Not released**, **Release blocked**, **Released**,
or **Result updated**, and shows only the next currently permitted action.
MVP ordering follows the server-authoritative deadline when one exists,
otherwise oldest release-ready time first, with a stable server tie-breaker.

The detail page preserves authorized human-readable Activity, Participant,
task, Attempt, and Result labels. Opaque identifiers, payload digests, policy
versions, and complete lineage belong in a subordinate provenance/history
disclosure when the actor is permitted to inspect them.

### Participant Results hierarchy

```text
Results
└── Activity or task
    ├── Availability or update status
    ├── Outcome summary
    ├── Permitted criterion outcomes
    ├── Participant-facing feedback
    ├── Release and correction metadata
    └── Permitted support route
```

The Results area may list only the current Participant's own authorized Activity
or task relationships. An empty or pre-release state must not reveal inaccessible
Result counts, exact internal workflow stage, or whether another Participant has
received a Result. A direct inaccessible and a nonexistent target use the same
non-disclosing safe behavior.

## State model

### Independent state tracks

The interface keeps these tracks separate and names the owning object:

| Track | Representative states | Interaction consequence |
| --- | --- | --- |
| Access | Resolving, permitted preview, permitted Release, reauthentication required, separation-of-duties blocked, denied/revoked | Controls and protected content follow current server-returned capability; visibility never derives from a rendered page |
| Result eligibility and integrity | Result ready, validation blocked, integrity warning, stale expected version, lawfully unavailable | Only one exact valid Result may proceed; no client edit, reconstruction, or silent substitution |
| Release command | Not started, confirming, recording, checking status, conflict, failed safely, released | Duplicate actions are disabled while authority is uncertain; success follows authoritative reconciliation only |
| Participant visibility | Neutral/not available, reconciling, visible, unavailable | Participant content renders only from the authoritative visibility path; projection state cannot grant or revoke access |
| Correction and lifecycle | Original current, correction in review, corrected Result ready, Result updated/current, superseded internal lineage, unavailable under policy | Current content changes only after a new correction Release; historical artifacts remain immutable |
| Notification | Not applicable, queued, delivery delayed, delivered, failed/retrying | Delivery is secondary feedback and never changes Release or Participant visibility |

Do not compress these tracks into generic `Pending`, `Complete`, `Available`, or
`Error`. For example, a Result may be `Released` while notification delivery is
delayed, and a correction may be in review while the original released Result
remains the current visible Result.

### Release transition

```text
Result ready · Not released
        |
        | current Release authority and valid exact Result
        v
Confirm Release Result
        |
        | authoritative commit in progress
        v
Releasing Result
        |
        +-- uncertain response --> Checking Release status --+
        |                                                   |
        +-- denied/stale/audit/integrity failure -----------+--> Not released / blocked
        |
        +-- committed ------------------------------------------> Released
                                                                  |
                                                                  +--> notification delivery
```

The UI never advances to `Released` from elapsed time, a client callback, a
notification event, or optimistic state. It uses the authoritative Release and
Participant visibility response. An equivalent duplicate resolves to the
existing Release; a conflicting command shows the current safe state and no
protected details from the competing payload.

### Participant visibility and correction transition

```text
Session complete or later internal state
        |
        v
Result not available
        |
        | exact Release commits
        v
Result available
        |
        | correction review begins
        +------------------------------> current Result remains visible
        |
        | corrected Result Release commits
        v
Result updated
        |
        | lifecycle or relationship restricts current access
        v
Result unavailable
```

No pre-release state exposes whether review is queued, active, rejected,
escalated, approved, or waiting for another Release actor. A correction in
progress does not change the current visible Result. `Result unavailable` does
not erase or contradict the historical Release.

## Release work list and status

### Initial, loading, and empty states

- Resolve identity, Organization context, Release capability, and list scope
  before protected names, counts, Result states, or preview fragments render.
- Loading reserves the expected list/detail structure and uses **Resolving
  Release work** as the programmatic status when waiting is material.
- The empty state is **No Result currently needs your Release action**. It does
  not state that no other Results exist and does not show inaccessible totals.
- Filters may cover only policy-permitted bounded categories such as Activity,
  release readiness, blocking state, and correction status. Client filtering of
  a broader unscoped dataset is prohibited.

### List item content and actions

Each authorized item shows:

- current owning state and any blocking reason category;
- permitted Activity, Participant, and task labels;
- exact Result version label or correction label;
- release-ready time or policy deadline in the user's locale with unambiguous
  timezone disclosure;
- separation-of-duties or reauthentication requirement when applicable; and
- one next action such as **Review Result for Release**, **Reauthenticate to
  continue**, **Open released Result**, or **Open correction status**.

Do not show scores, feedback excerpts, reviewer identity, internal reasons,
Evidence, or notification details in list rows. A list row does not contain a
one-click Release action.

## Exact Result inspection

### Case header and authoritative status

The detail header keeps the permitted Activity, Participant, task, Attempt, and
current Result/correction context visible. Directly beneath it, an urgent status
region names:

- **Result ready · Not released**;
- **Release blocked** plus the bounded actionable category;
- **Releasing Result**;
- **Checking Release status**;
- **Released** plus authoritative effective time;
- **Result updated** plus current correction effective time; or
- **Result unavailable** under current authorization or lifecycle policy.

The status region states whether the Participant can currently see the Result.
It does not infer notification delivery or human receipt from visibility.

### Immutable Participant-facing Result preview

Before Release, the primary content region is labeled:

> **Participant-facing Result · Exact content to be released**

After Release, the equivalent read-only region is labeled:

> **Participant-facing Result · Exact content released**

It renders the immutable payload created with the approved Review decision and
the same field order and safe renderer used by the Participant view. It may
contain only policy-permitted outcome summary, criterion results, aggregate,
feedback, explanation, locale, and other allowlisted fields. Sections absent by
policy remain absent; the interface does not expose excluded internal field
names as a way to infer protected content.

The preview:

- is read-only and has no inline edit, content regeneration, field selector, or
  audience selector;
- shows a clear validation/integrity status and Result version;
- renders links and rich content as inert, policy-safe data;
- distinguishes trusted interface notices from untrusted Result content
  visually and programmatically; and
- cannot navigate to Evaluation, Evidence, Review work, another Participant, or
  an unrestricted source merely because content contains an identifier or link.

If payload digest, integrity, schema, protected-content, localization, or
current-policy validation is blocked, Release is unavailable. The page names the
bounded category and server-returned recovery route; it does not repair or
reconstruct the immutable Result locally.

### Audience, policy, and provenance

The audience summary states **This Participant only** or another exact
policy-owned human-readable audience label. It is read-only. The UI does not
derive or accept authoritative Organization, Participant, audience, Result,
decision, or policy scope from client input.

The policy summary shows only what the actor needs to confirm:

- Release is individual, immediate, and explicit;
- the Participant gains access only after successful Release;
- approval and Release authority are separate;
- whether separation of duties or recent authentication applies;
- whether a reason or attestation is required; and
- the applicable lifecycle/availability summary in bounded plain language.

The bounded provenance disclosure may identify the exact immutable Review
decision, Result version, original or correction status, policy versions, and
creation/effective times. It does not reproduce Evaluation, Evidence, reviewer
notes, hidden prompts, or unnecessary actor identifiers.

## Release confirmation and commit

### Eligibility and disabled action

**Release Result** is available only when the server returns current Release
capability and the exact Result is eligible. A disabled action has a visible and
programmatic reason, such as:

- another authorized actor must Release this Result;
- reauthentication is required;
- the Result failed validation or integrity checks;
- the Participant relationship or policy no longer permits Release;
- the expected version is stale and must be reloaded; or
- Release service or required audit is temporarily unavailable.

The client must not infer eligibility from an `Approved` label or hide a server
denial behind an apparently enabled optimistic control.

### Confirm Release Result

The confirmation identifies:

- permitted assessment/Activity, Participant, and task labels;
- exact Result version and whether it is an original or correction Result;
- immutable preview validation state;
- exact audience label;
- separation-of-duties, recent-authentication, attestation, or reason
  requirements returned by policy;
- consequence: the exact Result becomes visible to the Participant immediately
  after authoritative commit; and
- correction consequence, when applicable: the corrected Result becomes the
  current visible Result and the prior Release remains in protected history.

The final action is **Release Result**. Cancel/Escape returns to the preview
without changing state. The dialog contains no editable Result fields, audience
selection, schedule, notification content, or combined approval action.

### Pending, uncertain, and authoritative outcomes

During commit, show **Releasing Result** and disable conflicting Release or
correction actions. Navigation that does not risk ambiguous mutation may remain
available.

If the response is lost, times out, or becomes uncertain, show:

> **Checking Release status. Do not release this Result again.**

Reconcile the exact Result, expected visibility version, idempotency context,
Release, and authoritative Participant visibility before restoring actions.
Never automatically resubmit.

Successful outcomes are exact:

- **Result released · Visible to Participant** with authoritative Release time;
  or
- **Corrected Result released · Current Result updated** with the new effective
  time.

An authorization, separation-of-duties, stale-version, integrity, audit, or
policy failure must preserve **Not released** and must not show a success toast.
A duplicate equivalent request returns the existing success. A conflict states
that Release status changed and offers **Reload Release status** without
revealing the other command or actor.

### Notification feedback

After authoritative Release, notification delivery appears only as subordinate
operational feedback when the actor may inspect it:

- **Availability notification queued**;
- **Availability notification delivered**; or
- **Availability notification delayed**.

Notification failure does not change **Released**, trigger another Release, or
hide the Result from the authoritative Participant path. This surface offers no
editing of notification content or external delivery destination in the MVP.

## Participant Results interaction

### Neutral pre-release state

Before authoritative Release, the Participant sees:

> **Result not available**
>
> Your result is not available in Flex Agent. Return to your activity or use the
> provided support route if you need help.

Production copy may adapt to frozen policy but must remain neutral. It must not
state or imply that an Evaluation exists, review is pending, a decision has been
made, the Participant passed or failed, a Reviewer is late, Release is
scheduled, or another Participant has received a Result. No score-shaped
placeholder, locked Result card, hidden criterion count, progress step, or
predicted availability time is shown.

If policy distinguishes **No Result will be available** from **Result not yet
available**, the server may return that exact Participant-safe state. The client
does not infer it from internal rejection, escalation, or lifecycle status.

### Released Result

After authoritative visibility resolves, show:

1. **Result available** and authoritative Release date/time;
2. Activity and task context using permitted labels;
3. configured overall outcome or aggregate, only when present in the Result;
4. ordered criterion outcomes or scores, only when present;
5. Participant-facing feedback or explanation;
6. **Original Result** or **Result updated** status and current effective time;
   and
7. a policy-approved informational support route, when configured.

Do not invent missing aggregates, normalize unfamiliar policy-owned scales, or
turn absent criterion details into zero values. Labels, ranges, units, and
applicability come from the Result schema. Internal confidence, uncertainty,
Evidence links, provisional feedback, reviewer identity/notes, decision reason,
hidden rubric content, and configuration/model detail remain absent.

The Participant Result payload is inert untrusted data. Links do not
automatically fetch or embed external resources. Any permitted safe link is
visibly distinguished from trusted product actions and follows the approved URL
policy.

### Corrected Result

When a correction Release commits, the current view leads with:

> **Result updated**

It shows the new authoritative effective time and a concise policy-owned
statement that the current Result replaces the previously visible outcome. The
new Result content follows the same hierarchy as any released Result.

The view does not show a redline, prior score, prior feedback, correction reason,
Reviewer identity, or full prior Result by default. If a future approved Result
policy permits Participant history, that history requires its own explicit
access and content rules; it cannot be inferred from internal correction
lineage.

### Lawful unavailability and authorization change

When lifecycle, legal restriction, Participant relationship, or current
authorization prevents disclosure, remove protected Result content and show a
non-disclosing **Result unavailable** state with the safe server-returned next
action. When policy permits the distinction, copy may state that a previously
available Result is no longer available; it must not claim that no Review
decision or Release ever occurred.

Reauthentication may restore access only after the complete relationship and
visibility chain resolves again. Cached Result content must not render while
authorization is unresolved or after current access is lost.

### Results list and empty state

The Participant Results list contains only currently permitted own Activity/task
entries. Each visible released item names **Result available** or **Result
updated**, the Activity/task, and effective time. Pre-release entries appear only
when policy permits a neutral placeholder; their ordering, counts, and copy do
not reveal internal workflow timing.

An empty state says **No Results are available here** and offers the normal
Participant navigation or configured support route. It does not imply that no
internal Evaluation, review, or Result artifact exists.

## Correction and history interaction for authorized actors

- The released detail remains read-only and shows the exact current Release,
  current-visible status, and bounded correction lineage.
- **Start correction review** appears only when returned as a currently
  permitted server action. Its confirmation states that it creates a new linked
  review lifecycle and does not edit or hide the current Result or Release.
- While correction review is active, the authorized view shows **Correction in
  review · Current Participant Result unchanged**.
- A corrected Result ready for Release uses the same inspection and confirmation
  contract as the original, with the prior current Release named in bounded
  provenance.
- After correction Release, history identifies the prior Release as
  **Superseded by correction** for authorized actors and the new Release as
  **Current Participant Result**. Both remain immutable.
- Missing or lawfully unavailable historical sources are labeled honestly and
  are never replaced by current data, logs, cache, or another version.

This interaction does not define correction intake, evidence, reviewer
independence, or adjudication policy. Those actions remain absent unless the
server returns a separately approved correction capability.

## Shared content and feedback

### Required terms and labels

- Use **Release work**, **Participant-facing Result**, **Result ready**, **Not
  released**, **Release Result**, **Releasing Result**, **Checking Release
  status**, **Released**, **Visible to Participant**, **Result not available**,
  **Result available**, **Result updated**, **Current Participant Result**,
  **Correction in review**, and **Result unavailable**.
- Use **This Participant only** or the exact policy-owned audience label; do not
  use generic **Public**, **Published**, or **Everyone** for the MVP audience.
- Use **Availability notification** rather than **Result sent**. Notification
  delivery does not prove Result visibility or human receipt.
- Avoid generic **Approved**, **Complete**, **Done**, **Available**, **Sent**,
  **Published**, and **Failed** without naming the owning object and consequence.
- Never call a Result released before the Release commit. Never call a
  notification a Release. Never call a corrected Result an edit of the prior
  Result.

### Example Release-actor copy

| Situation | Copy pattern |
| --- | --- |
| Ready | **Result ready · Not released. The Participant cannot see this Result.** |
| Exact preview | **Participant-facing Result · Exact content to be released.** |
| Separate authority | **Review approval does not grant Release authority.** |
| Separation of duties | **Another authorized actor must Release this Result under the current policy.** |
| Confirmation | **Release makes this exact Result visible to this Participant immediately after the authoritative commit.** |
| Pending | **Releasing Result. Visibility has not been confirmed yet.** |
| Uncertain | **Checking Release status. Do not release this Result again.** |
| Success | **Result released · Visible to Participant.** |
| Correction success | **Corrected Result released · Current Result updated.** |
| Audit failure | **Result not released. Required audit could not be recorded. Try again after service recovery.** |
| Stale/conflict | **Release status changed. Reload the authoritative state before taking another action.** |
| Notification delayed | **Result remains released. The availability notification is delayed.** |

### Example Participant copy

| Situation | Copy pattern |
| --- | --- |
| Neutral pre-release | **Result not available. Return to your activity or use the provided support route if you need help.** |
| Released | **Result available. Released {localized date and time}.** |
| Corrected | **Result updated. This is your current Result, effective {localized date and time}.** |
| Unavailable | **Result unavailable. Use the provided support route if you need help.** |
| Reauthentication | **Sign in again to check Result availability.** |

Production labels, times, versions, reasons, actions, and support routes come
from current authorized server state. Copy omits raw identifiers, payload
digests, credentials, reviewer identity, internal reasons, provider names,
private endpoints, another Participant, and unrestricted diagnostics.

## Accessibility contract

WCAG 2.2 AA is the contractual target inherited from the approved platform
journey and review/Release requirements.

### Structure and reading order

- Release work uses landmarks and headings for list or case header, urgent
  status, exact Result preview, audience/policy, decision/provenance,
  confirmation, notification, and history.
- Participant Results use headings for availability/update status, Activity and
  task, outcome summary, criteria, feedback, Release metadata, and support.
- Semantic order remains status, core Result content, consequence/policy,
  action, then history even when a wide layout places regions side by side.
- Outcome labels, scores, units, criterion applicability, Release, correction,
  integrity, availability, and notification state do not depend on color, icon,
  shape, position, motion, hover, or sound alone.
- Trusted system status/actions and untrusted Result content use distinct
  semantic containers so Result text cannot spoof a notice or control.
- Times expose a human-readable localized value and an unambiguous timezone;
  machine ordering detail is available only where useful and authorized.

### Keyboard and focus

- Release lists, filters, detail navigation, disclosures, preview content,
  confirmation, reauthentication, reconciliation, history, Participant Result,
  and support routes work without drag, hover, sound, or pointer precision.
- Opening a Release item moves focus to its page heading; returning restores the
  originating list item when it remains authorized.
- Opening confirmation moves focus into the named dialog. Cancel/Escape closes
  it and restores **Release Result**. Confirmed Release moves focus to the new
  authoritative status heading.
- Validation or eligibility summaries link to the affected bounded category or
  policy region without exposing prohibited content.
- Permission loss, a concurrent Release, correction becoming current, or lawful
  unavailability moves focus to the new authoritative status because the prior
  action/content is no longer current.
- Background notification updates, status polling, projection reconciliation,
  and list refreshes do not steal focus.

### Announcements

- Use polite announcements for Release-work readiness, reauthentication
  completion, recovered connection, notification delivery, and Result content
  becoming ready after navigation.
- Use assertive announcements once for Release success, correction success,
  permission loss, blocked integrity, failed required audit, authoritative
  conflict, and Participant Result becoming unavailable while open.
- During uncertain response, announce **Checking Release status** once; do not
  announce every poll or retry interval.
- Announcements name the owning state and consequence without automatically
  reading Result content, score, feedback, Participant identity, or internal
  reason.

### Dialogs, status, and protected content

- Confirmation exposes an accessible name, consequence description, exact
  Result/correction label, audience, requirements, safe cancel, and final action.
- Disabled controls expose their reason in adjacent text and programmatic
  description; disabled appearance alone is insufficient.
- Status messages use appropriate `status` or alert semantics without nesting
  every dynamic region in a live region.
- Result content, identifiers, scores, feedback, and Participant labels do not
  enter browser titles, operating-system notifications, analytics labels, or
  accessibility names outside the protected page context.
- Error summaries and denial states do not reveal inaccessible Result existence,
  another actor, another Participant, or competing command payload.

## Responsive behavior

- At narrow widths and 400 percent zoom, preserve current Activity/Result
  context, urgent status, exact Result preview, audience/consequence, and next
  permitted action before secondary provenance, notification, and history.
- A wide Release list/detail layout becomes one document flow. Filters collapse
  into a labeled disclosure or selector without changing scope, ordering, or
  current-location indication.
- Wide preview/policy panes stack in semantic order. No Result field,
  correction/update status, Release consequence, error, or disabled reason is
  omitted solely because the viewport is narrow.
- Participant criterion tables become labeled stacked records. Ordinary Result
  text requires no page-level two-dimensional scrolling.
- Long feedback, code, links, versions, and unbroken strings wrap or use bounded
  content scrolling without forcing the whole page horizontally.
- A sticky status or **Release Result** region is permitted only when it does not
  cover focused content, errors, browser zoom controls, dialogs, or the software
  keyboard.
- Touch targets remain operable at the approved accessibility size; Release and
  correction actions remain visually separated from navigation and support
  links.
- Respect reduced motion. No Release, visibility, correction, integrity, or
  notification meaning depends on animation.

## Security and privacy UX controls

- Authenticate and authorize every entry, list, count, filter, case read, Result
  preview, decision/provenance disclosure, Release, correction handoff,
  Participant status/read, history, notification status, export, and
  reconnection on the server.
- Scope Release work and Participant Results before materialization. Client
  filters, hidden controls, route guards, cached status, Result identifiers,
  digests, signed URLs, Release references, notification links, Organization
  membership, or prior access do not authorize content.
- Do not render cached protected names, preview fields, scores, feedback, or
  status before current access and authoritative visibility resolve. Remove
  prohibited content and controls when authorization, Participant relationship,
  visibility, lifecycle, or actor context changes.
- Render Evaluation-derived, reviewer-authored, Participant-facing, link,
  Markdown, filename, and Result content as inert untrusted data. It cannot
  execute, fetch external resources, spoof trusted status/actions, change
  policy/audience, submit Release, authorize tools or memory, or navigate across
  scope.
- Do not automatically fetch Result links, images, embeds, or external
  resources. A future permitted external link or download remains deliberate,
  policy-bound, reauthorized, and separately specified.
- Keep raw Result content, scores, feedback, Participant identity, Evaluation,
  Evidence, Human revision, Review decision reasons, reviewer identity/notes,
  prompts, expected answers, credentials, provider payloads, private endpoints,
  and unrestricted identifiers out of URLs, titles, notifications, analytics,
  logs, metrics, traces, queues, errors, screenshots, and test artifacts.
- Keep the immutable Result preview and Participant view deny-by-default. No
  wording, Markdown, hidden field, CSS, client state, or notification payload may
  opt internal content into Participant visibility.
- Evaluation, Human revision, Review decision, Result, Release, correction, and
  Participant interaction content must not be offered for memory, learning,
  calibration, analytics training, cross-Participant reuse, unrelated
  Activities, or Harness improvement in the MVP.
- Denials, loading placeholders, list counts, empty states, failures, correction
  labels, notification states, and timing do not disclose inaccessible Result
  existence, another Participant, internal review state, score, or protected
  history.
- In-product copy, print, download, export, public sharing, and appeal controls
  are absent unless separately authorized and specified. Hiding controls does
  not prevent operating-system or browser capture; deployments requiring
  stronger endpoint controls must govern them outside this specification.

## Failure and recovery matrix

| Condition | Visible state | Preserved state | Prohibited claim or action | Recovery |
| --- | --- | --- | --- | --- |
| Initial Release authorization unresolved | Resolving Release work | No protected UI content | No cached names, counts, Result preview, or scores | Resolve current identity, scope, capability, and Result visibility |
| Approved decision has no valid release-eligible Result | Release blocked | Immutable decision and bounded failure category | No local Result reconstruction, partial preview, or Release | Follow server-returned review/policy recovery route |
| Result integrity, schema, or protected-content validation fails | Result not release-eligible | Exact Result reference and safe status | No edit, substitution, Release, or Participant preview | Authorized correction or operational path |
| Actor approved but lacks Release authority | Result ready · Not released | Result and decision under permitted read scope | No enabled Release or disclosure of another actor | Return to work or follow current server-permitted route |
| Separation of duties blocks actor | Another authorized actor must Release | Result and policy summary | No bypass, self-assignment inference, or actor disclosure | Authorized handoff outside this mutation |
| Reauthentication required | Reauthenticate to Release | Exact Result remains not released | No credential capture in Result form or optimistic Release | Complete approved authentication flow, then reauthorize |
| Participant relationship or policy changes before commit | Release denied · Not released | Prior authoritative state | No stale Release, audience change, or existence disclosure | Reload current state; follow authorized operational path |
| Required Release audit unavailable | Result not released | Exact Result and safe actor input | No Release, visibility, notification, or success claim | Retry after recovery with current authorization and idempotency |
| Release response lost or times out | Checking Release status | Command/idempotency context | No automatic or manual duplicate while uncertain | Reconcile authoritative Release and visibility |
| Equivalent duplicate Release | Result released | Existing exact Release and visibility | No second Release, notification, or audit interpretation | Return existing authoritative outcome |
| Conflicting or concurrent Release | Release status changed | One authoritative state; no competing payload | No overwrite, audience switch, or competing actor detail | Reload authoritative Release status |
| Participant projection lags | Result visibility reconciling | Committed Release and authoritative path | No early projection grant or reversion to unreleased | Read authoritative visibility; retry projection separately |
| Availability notification fails | Result released · Notification delayed | Release and Participant visibility | No rollback, repeated Release, or Result content in retry | Retry bounded notification work separately |
| Participant opens before Release | Result not available | Neutral Activity/task context only when permitted | No score, criteria, Review state, actor, or predicted time | Return to Activity or configured support route |
| Participant guesses another Result or changes identifier | Non-disclosing unavailable/denied state | Current own context only | No existence, owner, score, or release-state disclosure | Return to own Results or reauthenticate |
| Correction review starts | Correction in review · Current Result unchanged | Prior current Release and linked correction status | No mutation, early replacement, or Participant preview of correction | Complete new review and explicit correction Release |
| Correction Release commits | Result updated | Both immutable Releases; new current pointer | No silent overwrite or prior content by default | Show new Result and effective time |
| Lifecycle or relationship removes current access | Result unavailable | Historical Release under policy | No cached content or claim Release never occurred | Reauthorize or follow policy-approved support route |
| Connection lost while viewing | Reconnecting; visibility not confirmed | Last content only under safe same-actor policy | No current authority, Release, or updated-state claim | Reauthenticate and reconcile before rendering/actions |
| Unsupported Result, policy, or command version | Release/Result unavailable | Stable protected references | No reinterpretation, weaker fallback, or guessed fields | Upgrade supported adapter or authorized operational path |
| Result content attempts script, link, notice, or control spoofing | Inert literal/sanitized content or Release blocked | Exact authorized source meaning | No execution, fetch, trusted styling, state change, or scope escape | Safe rendering, validation, and security event when applicable |

## Traceability matrix

| Interaction or state | Approved acceptance criteria | Implementation surface | Verification expected after implementation |
| --- | --- | --- | --- |
| Release work list, exact approved handoff, eligibility, integrity, and scoped access | `AC-REL-1`–`AC-REL-4`; `AC-REV-4`, `AC-REV-9`, `AC-REV-12`, `AC-REV-18`; `AC-AUTH-1`, `AC-AUTH-4`–`AC-AUTH-8`, `AC-AUTH-11`–`AC-AUTH-13`, `AC-AUTH-18`, `AC-AUTH-20`, `AC-AUTH-22`, `AC-AUTH-23` | Scoped Release-work queries, handoff resolver, Result preview, policy/provenance summary, permitted actions | Authorized and wrong-scope list/count/filter/deep-link; exact Result/digest; no edit/audience control; internal-field leakage; separation-of-duties and revocation tests |
| Explicit Release confirmation and authoritative commit | `AC-REL-3`–`AC-REL-8`; `AC-REL-11`; `AC-REL-12`; `AC-REV-15`, `AC-REV-17` | Confirmation, expected-version/idempotency command, authoritative status, focus/announcement handling | Happy path, recent-auth/attestation, duplicate/conflicting/concurrent Release, stale Result, lost response, audit failure, no implicit Release; keyboard/dialog evidence |
| Participant neutral pre-release and exact released Result | `AC-REL-9`–`AC-REL-12`; `AC-REV-17`; `AC-AUTH-1`–`AC-AUTH-3`, `AC-AUTH-7`, `AC-AUTH-8`, `AC-AUTH-11`–`AC-AUTH-13`, `AC-AUTH-18`, `AC-AUTH-20`–`AC-AUTH-23` | Authoritative Participant Results list/status/read, safe renderer, support route | Every internal pre-release state produces neutral output; own/wrong Participant direct/list/count/cache/index/link tests; allowlist, inert content, narrow/zoom and screen-reader evidence |
| Correction, current-visible Result, history, and lawful unavailability | `AC-REL-13`–`AC-REL-15`; `AC-REV-18`, `AC-REV-20`; `AC-AUTH-1`, `AC-AUTH-8`, `AC-AUTH-11`–`AC-AUTH-13`, `AC-AUTH-18`, `AC-AUTH-20`, `AC-AUTH-22`–`AC-AUTH-24`; `AC-RSC-18`–`AC-RSC-21` | Correction handoff/status, exact correction Release, current pointer, authorized history, lifecycle/unavailable view | Replacement no-side-effect, current Result during review, correction race/Release, update notice/time, prior restriction, lifecycle/relationship loss and reconstruction tests |
| Notification delivery separation | `REQ-REL-13`; `AC-REL-3`, `AC-REL-5`, `AC-REL-6`, `AC-REL-9` | Committed Release outbox status, authenticated Results route | No pre-release work, availability-only payload, failure/delay independent of visibility, duplicate delivery and no bearer/content leakage tests |
| Authorization, privacy, inert rendering, lifecycle, non-reuse, and historical truth | `AC-REL-8`–`AC-REL-15`; `AC-REV-16`, `AC-REV-18`–`AC-REV-20`; `AC-AUTH-1`–`AC-AUTH-24`; `AC-RSC-18`–`AC-RSC-21` | Every surface, renderer, cache policy, history, support, optional export hooks | Full wrong-scope/identifier/version matrix, loading/cache leakage, content/control injection, URL/log/notification/screenshot/export leakage, non-reuse and lifecycle-policy tests |
| Accessibility, responsive behavior, state feedback, and performance | `AC-REL-10`–`AC-REL-12`; `AC-REV-17`; approved WCAG baseline | Release list/detail, preview, confirmation, reconciliation, Participant neutral/released/corrected/unavailable views | Accessibility snapshots; keyboard-only path; screen-reader state/focus tests; reduced motion; 400 percent zoom; desktop/narrow screenshots; 2-second acknowledgment and 5-second authoritative visibility evidence |

## Verification notes

This is a documentation-only change, so test-first implementation and
Playwright visual verification do not apply yet. Approval of this specification
does not constitute implementation evidence.

Before implementation is considered complete, verification must include:

- repeatable positive and negative tests mapped to every applicable row above;
- authorization tests for wrong Organization, Activity, Participant,
  Enrollment, Attempt, Session, decision, Result, Release, visibility version,
  actor capability, and correction lineage;
- state-contract tests for all six independent tracks and every prohibited
  transition or ambiguous combined state;
- idempotency, concurrent-action, lost-response, required-audit failure,
  notification failure, projection lag, correction race, lifecycle, and
  permission-revocation tests;
- allowlist, inert-rendering, unsafe-link, trusted-notice spoofing, and protected-
  content leakage tests across UI, URLs, caches, errors, notifications,
  telemetry, exports, screenshots, and accessibility names;
- accessibility snapshots and keyboard/focus evidence for Release list, exact
  preview, policy/provenance disclosure, confirmation, reconciliation, denial,
  terminal Release, Participant neutral/released/corrected/unavailable states,
  and support navigation;
- Playwright screenshots at desktop and narrow viewports, plus 400 percent zoom
  checks, for initial, loading, empty, Result-ready, blocked, separation-of-
  duties, reauthentication, confirmation, pending, uncertain, conflict, audit-
  failed, released, notification-delayed, neutral pre-release, released Result,
  corrected Result, lawfully unavailable, permission-lost, and recovery states;
  and
- visual review of hierarchy, copy, spacing, alignment, overflow, focus,
  contrast clues, status distinction, protected-content boundaries, dangerous-
  action separation, and polish.

Store browser artifacts only in `.playwright-mcp/` and use synthetic data with
no real Participant, Result, or credential content.

## Open questions

None. `PROP-UI-REL-1`–`PROP-UI-REL-12` were approved on 2026-08-09.

## Downstream gaps and review needed

- The design-system foundation must define reusable protected Result, exact-
  preview, status, confirmation, error summary, audience/policy summary,
  updated-Result, history, and responsive stacked-record patterns identified
  here.
- Frontend and backend contracts must expose the six independent state tracks,
  exact Result/digest and expected versions, server-derived audience and
  permitted actions, authoritative Participant visibility, correction lineage,
  notification state, and bounded recovery categories without making the
  browser authoritative.
- QA must turn the traceability and verification sections into repeatable suites
  and Playwright evidence after a runnable implementation exists.
- Implementation, automated verification, accessibility evidence, and
  Playwright visual evidence remain delivery gaps; approval of this
  specification does not constitute implementation evidence.

## Approval record

| Perspective | Status | Confirmed concern |
| --- | --- | --- |
| Product Lead | Approved | Scope, Release and Participant outcomes, and `PROP-UI-REL-1`–`PROP-UI-REL-12` |
| UI/UX reviewer | Approved | Release-work hierarchy, exact preview and confirmation, Participant Results, correction states, content, responsive behavior, and WCAG 2.2 AA contract |
| Architecture Lead | Approved | Immutable Result/digest handoff, server-derived audience, expected-version/idempotency recovery, authoritative visibility, notification separation, and correction lineage |
| Security/Privacy reviewer | Approved | Release authorization, pre-release non-disclosure, deny-by-default Result content, inert rendering, notification minimization, lifecycle truth, and non-reuse |

- Business-analysis review bounded Release actor, approving Reviewer,
  Participant, administrator, service, notification, and audit responsibilities;
  mapped happy, alternate, failure, concurrency, correction, and terminal states
  to approved criteria without adding MVP scope.
- UI/UX review confirmed the Release work and Participant Results information
  architecture, six state tracks, exact preview and confirmation, neutral pre-
  release behavior, corrected Result presentation, content, focus,
  accessibility, and responsive behavior.
- Architecture review preserved the immutable Result/digest handoff, separate
  Release authority, expected-version/idempotency reconciliation, atomic
  authoritative visibility, notification separation, current-visible correction
  pointer, and server authority.
- Security/privacy review preserved complete-chain authorization, pre-release
  non-disclosure, deny-by-default Participant content, inert rendering,
  notification minimization, correction/lifecycle truth, no learning reuse, and
  safe denial.
- Traceability and repository-consistency review found no conflict with the
  approved product model, P0 requirements, operational defaults, architecture
  contracts, or adjacent UI/UX specifications. Implementation and verification
  evidence remain open.

## Related documents

- [UI/UX documentation](README.md)
- [Activity journey and Campaign information architecture](activity-campaign-journey.md)
- [Evidence, Evaluation, and Human Review interaction specification](evidence-evaluation-human-review.md)
- [Human review and Result Release](../requirements/features/review-result-release.md)
- [Human review, Result, and Release contract](../architecture/review-result-release-contract.md)
