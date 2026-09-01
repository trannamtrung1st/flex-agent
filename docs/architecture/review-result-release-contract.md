# Human review, Result, and Release contract

Approved detailed architecture contract for review-case selection, Human
revision, Review decision, Result construction, participant visibility, Release,
and correction in the MVP assessment workflow.

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Approved |
| **Owner** | Architecture Lead |
| **Approvers** | Product Lead, Architecture Lead, Security/Privacy reviewer |
| **Consulted perspectives** | Business analysis, architecture, UI/UX, security/privacy, documentation |
| **Version** | 0.1 |
| **Last reviewed** | 2026-09-01 |
| **Governs** | Review case/candidate, revision, decision, Result, Release, visibility, and correction realization |

This contract currently owns the Review/Result/Release realization split
extracted from ADR-009 (`REV-DEC-*`). ADR files remain until Phase 5.

This document does not change the
[Human review and Result Release specification](../requirements/features/review-result-release.md).
It is the current detailed MVP technical realization for this boundary.

## Purpose and audience

This contract defines the final architecture boundary in the MVP vertical slice:
from an immutable completed Evaluation to an explicitly released
Participant-facing Result. It gives backend, frontend, security, testing, and
operations contributors explicit rules for:

- review-case and assignment ownership;
- exact Evaluation candidate selection and stale-review handling;
- immutable Human revision and Review decision representation;
- deny-by-default Result construction and validation;
- separate Release authorization and atomic Participant visibility;
- correction/current-visible lineage; and
- idempotency, concurrency, audit, lifecycle, recovery, and verification.

It does not choose a framework, database product, OIDC provider, notification
provider, or detailed UI design.

## Governing sources

- [Concept model](../product/concept-model.md), especially the separation among
  Evaluation, Human revision, Review decision, Result, and Release.
- [MVP scope](../product/mvp-scope.md), especially mandatory human oversight and
  the explicit Participant-to-Result vertical slice.
- Approved [review and Release requirements](../requirements/features/review-result-release.md#business-rules)
  and [acceptance criteria](../requirements/features/review-result-release.md#acceptance-criteria).
- [Authorization and isolation](../requirements/features/auth-resource-isolation.md),
  [resolved Session configuration](../requirements/features/resolved-session-configuration.md),
  [assessment setup](../requirements/features/assessment-setup.md),
  [text Session lifecycle](../requirements/features/session-text-lifecycle.md),
  [Evidence and Evaluation](../requirements/features/evidence-evaluation.md), and
  [MVP operational defaults](../requirements/mvp-operational-defaults.md).
- [ADR-001](decisions/ADR-001-resolved-configuration-representation-and-integrity.md),
  [ADR-002](decisions/ADR-002-authorization-enforcement-and-delegation.md),
  [ADR-003](decisions/ADR-003-authorization-audit-persistence.md),
  [ADR-004](decisions/ADR-004-assessment-activation-baseline-and-atomicity.md),
  [ADR-005](decisions/ADR-005-atomic-attempt-start-and-submission-binding.md), and
  [ADR-006](decisions/ADR-006-mvp-architecture-baseline-and-evolution.md),
  [ADR-008](decisions/ADR-008-bounded-oss-component-set.md), and
  [ADR-009](decisions/ADR-009-mvp-session-evaluation-review-contracts.md).
- [MVP architecture](mvp-architecture.md) and the approved
  [Evaluation execution contract](evaluation-execution-contract.md).

## Scope

### In scope

- Review-case creation/refresh, assignment, claim, candidate selection, and
  stale-candidate reconciliation.
- Authorized inspection contracts for the selected Evaluation, Evidence,
  Submission, transcript, configuration, manifest, and fairness facts.
- Optional structured Human revision and classified reviewer content.
- Immutable `Approved`, `Rejected`, and `Escalated` Review decisions.
- Versioned Result construction from an approved decision, using an explicit
  Participant-facing allowlist.
- Individual, immediate, explicit Release of one exact Result to one permitted
  audience; authoritative Participant reads and asynchronous notification.
- Post-Release correction, current-visible Result selection, lifecycle,
  unavailability, export hooks, audit, and reconstruction.

### Out of scope

- Evaluation generation or replacement, rubric/policy authoring, Session or
  Evidence mutation, and general reviewer staffing or workload optimization.
- Bulk, scheduled, embargoed, or external-channel Release; Participant appeal;
  public links; certificates; downstream gradebook/HR/LMS integrations.
- Detailed reviewer and Participant interaction/visual specifications.

## Confirmed constraints

1. Evaluation, Human revision, Review decision, Result, and Release remain
   distinct logical and durable objects with immutable lineage.
2. A review candidate is one explicit immutable completed Evaluation; no
   `latest` alias or arrival order may select or replace it.
3. Reviewer assignment, content permission, workflow state, candidate integrity,
   and authorization are current-state checks, not properties of a URL or queue
   item.
4. Approval authority and Release authority are evaluated independently.
   Configured separation of duties is enforced at commit.
5. Result content is a deny-by-default projection; internal Evaluation,
   Evidence, confidence, uncertainty, provisional feedback, notes, identities,
   prompts, provider internals, and secrets do not flow implicitly.
6. Release is explicit and human-authorized. Evaluation completion, approval,
   Result construction, elapsed time, event delivery, or notification cannot
   release.
7. The primary relational store owns Review decision, Result, Release,
   participant visibility, idempotency, ordering, audit/outbox, and current
   visible lineage.
8. Historical records and Releases are never silently overwritten. Correction
   appends a new review-to-Release chain.

## Approved contract decisions

All decisions in this section were approved on 2026-08-06.

| ID | Approved decision | Rationale |
| --- | --- | --- |
| `REV-DEC-1` | Model the Review case as an expected-version state machine and represent assignment and candidate selection as immutable append-only events with current pointers. | Preserves history while supporting concurrency-safe queues and reassignment. |
| `REV-DEC-2` | A candidate selection captures one exact Evaluation plus its integrity state and lineage head observed at selection. Any later eligible replacement marks the case `candidate_stale` but never switches it automatically. | Prevents reviewers from deciding against a moving target. |
| `REV-DEC-3` | Represent Human revision as a versioned structured difference over one exact Evaluation, limited to policy-allowlisted JSON Pointer-like field paths and validated Evidence references. | Enables accountable changes without copying or overwriting the Evaluation. |
| `REV-DEC-4` | For `Approved`, construct and validate the immutable Result in the same primary-store transaction that commits the Review decision and required audit. `Rejected` and `Escalated` create no Result. | Ensures every approved decision has one exact validated Result while preserving Release as a separate action. |
| `REV-DEC-5` | Use a versioned Result envelope plus a policy-owned field allowlist. Absence from the allowlist means exclusion, and validation runs against the exact previewed payload digest. | Prevents internal-field leakage and preview/Release drift. |
| `REV-DEC-6` | Commit Release, exact Result binding, audience, current-visible pointer, Participant visibility state, and required audit/outbox in one transaction. | Implements `AR-DEC-11` and prevents early or contradictory visibility. |
| `REV-DEC-7` | Serve sensitive Participant Result reads from the authoritative write model or a transactionally equivalent read path. Projections may aid lists/status but never grant visibility. | Meets the five-second visibility objective without making projection lag an authorization boundary. |
| `REV-DEC-8` | A correction creates a new linked case, candidate, optional revision, decision, Result, and Release. The current-visible pointer changes only in the correction Release transaction. | Preserves released history and gives each read one unambiguous current Result. |
| `REV-DEC-9` | For the MVP, emit notification work only from the committed Release outbox. Notification contains availability status and a normal authenticated route, not Result content or bearer access. A later approved channel policy may permit a separately reviewed safe representation without changing historical Releases. | Separates delivery failure from visibility, minimizes disclosure, and preserves an explicit extension path. |

## Logical ownership and records

| Record | Authoritative owner | Required identity and mutation rule |
| --- | --- | --- |
| Review case | Review and Release | Organization through Session/Participant chain, frozen policy/lifecycle refs, state/version, Evaluation lineage, current candidate/assignment/result/release refs; expected-version transitions |
| Assignment event | Review and Release | Case, assignee/delegated group, capabilities/content scope, effective/expiry/revocation, actor, reason, UTC order; immutable |
| Candidate selection | Review and Release | Case, exact Evaluation and predecessor/successor lineage, observed lineage head, integrity state, selector, reason, UTC order; immutable |
| Reviewer content | Review and Release | Exact classification (`internal_note`, `participant_feedback`, `decision_reason`, `escalation_reason`), actor, protected content ref, visibility and time; immutable once submitted |
| Human revision | Review and Release | Exact Evaluation/candidate, schema, structured differences, Evidence refs, actor/assignment, reason, validation/integrity and UTC order; immutable |
| Review decision | Review and Release | Case/candidate, optional one Human revision, exact outcome, actor/assignment, policy/expected versions, reason/attestation, UTC order and audit; immutable |
| Result | Review and Release | Exact approved decision and reviewed content, versioned Participant payload, schema/locale/policy/audience, creator, digest/integrity, UTC order; immutable |
| Release | Review and Release | Exact Result, audience, prior/new visibility, actor/delegation, policy versions, idempotency, effective UTC order and audit; immutable |
| Participant visibility | Review and Release | Participant/Activity scope, current visible Release/Result, state/version and lawful availability; authoritative current pointer with immutable change events |
| Correction lineage | Review and Release | Prior/new case, decision, Result and Release, bounded reason, authorizing actor and Participant-facing update status; append-only |
| Notification work | Review and Release outbox, delivered by adapter | Release reference, audience reference, channel-policy version, state/retry; no Result payload or bearer authority |

Physical co-location is permitted, but authorization, mutation, lifecycle, and
lineage semantics remain distinct.

## Review state contract

```text
Awaiting evaluation
        | eligible completed Evaluation
        v
Ready -> Assigned/In review -> Approved + Result ready -> Released
                |                  |
                |                  +-- explicit Release only
                +-> Rejected
                +-> Escalated -> Ready or new linked case
                +-> Candidate stale -> explicit candidate resolution

Released -> correction request -> new linked review lifecycle -> corrected Release
```

- `Awaiting evaluation` may represent a terminal Session with no eligible
  Evaluation; it creates no fabricated outcome.
- Assignment and case state are separate. Revocation closes the assignment's
  authority without erasing prior work.
- `candidate_stale` blocks revision/decision commit until an authorized actor
  explicitly retains or changes the candidate under frozen policy.
- `Approved + Result ready` remains Participant-invisible.
- `Released` identifies one immutable Release; the Participant visibility record
  identifies which Release is currently visible.

## Versioned command contract

Every state-changing command contains a supported schema version, command and
idempotency identities, untrusted case/Result locator, expected case or
visibility version, client-observed candidate/Result digest where applicable,
and command-specific payload. The server derives actor, Organization, ownership,
assignment, policy, audience, and current state from authoritative records.

Initial commands are:

- `review.assignment.claim.v1`
- `review.assignment.change.v1`
- `review.candidate.select.v1`
- `review.revision.submit.v1`
- `review.decision.commit.v1`
- `result.release.v1`
- `result.correction.start.v1`
- `review.reconcile.v1`
- `result.reconcile.v1`

Equivalent retries return the existing authoritative result. Reuse with a
different trusted digest, candidate, Result, audience, policy context, or scope
fails without mutation. Responses return stable outcome/recovery categories and
non-disclosing denial behavior.

## Candidate, inspection, and Human revision contract

### Candidate selection

Candidate selection validates:

- exact immutable completed Evaluation and Evidence-set integrity;
- complete Organization, Activity, Participant, Attempt, Session, and manifest
  lineage;
- candidate eligibility under frozen and current narrowing policy;
- current actor/assignment authority and expected case version; and
- a bounded selection or replacement reason plus required durable audit.

The selection stores the observed Evaluation-lineage head. A replacement event
does not mutate it; it appends a stale marker/event for the case. Review input may
be preserved locally where safe, but no stale revision or decision may commit.

### Protected inspection

Queue queries constrain scope before materialization. Opening an Evaluation,
Evidence locator, Submission, transcript, configuration, manifest, revision,
Result preview, or audit reference performs a new ADR-002 decision over the
target's complete parent chain and current content permission. The Review case,
assignment, digest, signed artifact URL, or prior page view is not an access
token.

### Human revision schema

A `human-revision.v1` document contains:

- exact case, candidate Evaluation, revision-schema and frozen-policy versions;
- ordered structured operations, each with an allowlisted field path, prior
  value digest/reference, proposed value, bounded reason, and Evidence refs;
- explicitly classified Participant-facing feedback separate from internal
  notes;
- actor, active assignment, UTC order/time, and idempotency/correlation; and
- validation and integrity metadata.

Revision paths may address only policy-permitted criterion judgment,
applicability, score/decision, rationale, aggregate, or Participant-feedback
fields. They cannot address ownership, rubric identity, Evidence source content,
configuration, manifest, evaluator/model provenance, assignment, policy, Result,
or Release fields. Submission validates type, range, aggregation, Evidence,
content classification, unsafe markup, and protected-content rules before
creating the immutable artifact.

## Review decision and Result contract

### Decision commit

One primary-store transaction:

1. reauthenticates and reauthorizes the actor and validates the active assignment,
   complete ownership, expected case version, exact candidate, observed lineage,
   integrity, frozen/current policy, and optional submitted Human revision;
2. validates the requested `Approved`, `Rejected`, or `Escalated` outcome and
   required reason/attestation;
3. enforces idempotency and one decision for the expected case version;
4. inserts the immutable Review decision;
5. for `Approved`, deterministically constructs and validates exactly one
   immutable Result from the selected Evaluation plus optional revision;
6. appends case state/history and required audit/outbox; and
7. stores the idempotent response before exposing success.

For `Rejected` or `Escalated`, step 5 is omitted and no releasable Result exists.
External delivery is absent from this transaction.

### Result envelope and allowlist

A `participant-result.v1` envelope contains:

- stable Result/schema/locale identity;
- Organization, Activity, Participant, Attempt, and Session references required
  for protected ownership, not exposed as unrestricted public identifiers;
- exact Review decision, Evaluation, optional Human revision, result-policy, and
  lifecycle-policy references;
- an ordered Participant-facing payload containing only allowlisted fields;
- permitted audience and availability classification;
- payload digest/integrity procedure, creation service, and UTC order/time; and
- predecessor/successor correction references when applicable.

The policy allowlist names permitted fields, value types/ranges, criterion and
aggregate visibility, feedback/explanation classes, locale behavior, and safe
rendering constraints. Internal Evaluation confidence/uncertainty, provisional
feedback, Evidence selections, reviewer notes/identity, hidden rubric or
expected answers, configuration internals, prompts, model/provider internals,
credentials, and unrelated Participant data are excluded unless an approved
policy names a separately safe Participant representation.

The decision workflow may show an authorization-filtered preview generated by
the same versioned projector. The authoritative Result stores the exact validated
payload and digest; Release cannot edit or reconstruct it from current sources.

## Release and Participant visibility contract

### Release commit

One primary-store transaction:

1. authenticates and reauthorizes the Release actor separately from approval;
2. derives trusted ownership and verifies exact approved decision, Result,
   expected case/visibility versions, payload digest, integrity, current
   Participant relationship, frozen/current policy, audience, and any required
   separation of duties or recent authentication;
3. enforces idempotency and confirms the Result is not already released under a
   conflicting context;
4. inserts the immutable Release;
5. changes authoritative Participant visibility and the exact current-visible
   Result/Release pointer;
6. appends case/correction history and required durable audit; and
7. inserts a notification outbox item after, and transactionally coupled to,
   visibility.

No Result is visible unless all steps commit. A lost response reconciles by
trusted scope and idempotency. Notification failure does not roll back or repeat
Release.

### Participant read path

Every Result list, count, status, direct read, export, cache, or index path first
authorizes the current Participant relationship and visibility state against the
authoritative write model or transactionally equivalent source. Before Release,
the external state is neutral and reveals no score, review timing, candidate,
decision, or internal status. After Release, only the exact current permitted
Result payload and bounded Release/correction metadata are returned.

Projections may expose a `reconciling` status after commit but cannot grant early
visibility or revert a committed visible Result. Cache entries, if later used,
include Organization, Participant, visibility version, and Result version and
are never the sole authorization source.

### Correction

An authorized correction request creates a new case linked to the prior released
chain and records bounded reason and required audit. It does not reopen or edit
the prior case. The new workflow explicitly selects its Evaluation, optionally
creates a revision, decides, constructs a Result, and Releases it under current
authorization.

The correction Release transaction changes the current-visible pointer and
records Participant-facing `updated` status and effective time. Prior decisions,
Results, Releases, and internal lineage remain immutable and restricted by
policy. A Participant sees detailed prior content only when an approved Result
policy permits it.

## Authorization, security, and privacy contract

| Threat or harm | Required control | Verification |
| --- | --- | --- |
| Cross-scope queue/case/result disclosure | Scope queries before materialization; ADR-002 complete-chain authorization on every target | Wrong Organization through wrong Release list/count/read/export matrix |
| Stale assignment or candidate | Expected versions, observed lineage head, commit-time reauthorization and explicit stale state | Reassignment, revocation, replacement and concurrent reviewer tests |
| Internal content leaks into Result | Versioned deny-by-default allowlist, content classification, same projector for preview/Result, protected-content validator | Notes, confidence, Evidence, hidden prompt, reviewer identity and unsafe-markup tests |
| Unauthorized or implicit Release | Separate action/capability, human command, current policy/relationship, optional separation of duties and recent authentication | Evaluation/approval/event/notification implicit-release and revoked-authority tests |
| Pre-release disclosure | Atomic Release/visibility/audit, authoritative read path, scoped caches/projections | Direct ID, list/count, cache/index, export, notification and race tests |
| Replay or double Release | Scoped idempotency, trusted request digest, expected versions, Result/Release uniqueness | Equivalent, conflicting, lost-response, retry-storm and concurrent commands |
| Correction overwrite | Immutable chain and atomic current-visible pointer change | Replacement, access race, prior-history and current-version tests |
| Unsafe content/export | Inert rendering, URL policy, formula-injection neutralization, explicit export capability and audit | XSS, link, accessibility-name, CSV/formula, download and export tests |
| Operational or secondary disclosure | Protected refs and bounded telemetry; no Result in notifications; lifecycle and non-reuse controls | Log/trace/queue/screenshot/backup leakage and learning-reuse tests |

Signed URLs, object keys, digests, case/Evaluation/Result/Release identifiers,
notification routes, Organization membership, and role labels prove neither
authorization nor visibility.

## Failure and recovery contract

| Failure | Required outcome |
| --- | --- |
| No eligible completed Evaluation | Keep case unresolved; create no decision or Result |
| Replacement appears during review | Mark candidate stale; preserve local draft where safe; require explicit resolution |
| Assignment/permission changes | Deny new reads/actions immediately and narrow long-lived access within the approved bound |
| Revision/Result validation fails | Preserve recoverable input when safe; create no authoritative revision/decision/Result |
| Concurrent decision or Release | One expected-version/idempotent transaction wins; loser returns current safe state |
| Required audit fails | Roll back the coupled decision, Result, Release, correction, export, or visibility change |
| Release response is lost | Reconcile from idempotency and authoritative Result/visibility; do not repeat side effects |
| Projection or notification lags/fails | Keep authoritative visibility; expose reconciling status or retry notification separately |
| Result becomes lawfully unavailable | Preserve Release lineage and expose only the permitted unavailable state |
| Lifecycle policy is missing/widening | Fail the affected protected boundary; never invent retention or erase required lineage |

## Quality and observability

- Bounded queue/case/status reads and authoritative decision/Release
  acknowledgments retain the approved 2-second p95 objective. A committed
  Release becomes visible through the authoritative Participant path within the
  approved 5-second p95 objective outside declared platform-wide outages.
- Queries are scoped and bounded before materialization. Backpressure prevents
  one Organization, Activity, Evaluation, export, or notification failure from
  starving unrelated work.
- Operational signals include case/assignment/candidate state, stale candidate,
  revision validation, decision/Release idempotency and conflicts, result-policy
  validation, audit failure, authoritative visibility latency, projection and
  notification lag, correction, lifecycle, export, and security denials.
- Logs, metrics, traces, alerts, queues, notifications, errors, and screenshots
  contain no raw Evaluation, Evidence, revision, note, Result, Participant data,
  credentials, or unrestricted identifiers.

## Verification and traceability

| Contract surface | Requirements and acceptance criteria | Minimum repeatable evidence |
| --- | --- | --- |
| Case, assignment, candidate | `REQ-REV-1`–`REQ-REV-8`; `AC-REV-1`–`AC-REV-4` | Eligibility, no-Evaluation, exact candidate, list/count isolation, claim/reassign/revoke, stale replacement and concurrency tests |
| Inspection and Human revision | `REQ-REV-9`–`REQ-REV-18`; `AC-REV-5`–`AC-REV-9` | Target reauthorization, field/range/Evidence validation, immutability, note separation and unsafe-content tests |
| Review decision | `REQ-REV-19`–`REQ-REV-26`; `AC-REV-6`, `AC-REV-10`–`AC-REV-15` | Approve unchanged/revised, reject, escalate, stale permission, decision idempotency/concurrency and audit fault injection |
| Result construction | `REQ-REL-1`–`REQ-REL-5`; `AC-REL-1`–`AC-REL-2` | Allowlist, completeness, aggregate, locale, payload digest, internal-field leakage and safe-rendering tests |
| Release and visibility | `REQ-REL-6`–`REQ-REL-13`; `AC-REL-3`–`AC-REL-12` | Separate authority, pre-release matrix, atomic fault injection, equivalent/conflicting retries, lost response, projection/notification races |
| Correction and lifecycle | `REQ-REL-14`–`REQ-REL-16`; `AC-REL-13`–`AC-REL-15`, `AC-REV-20` | Replacement no-side-effect, correction Release/current pointer, prior lineage, unavailable state and lifecycle bounds tests |
| Authorization, audit, privacy, export | `REQ-REV-27`–`REQ-REV-35`; `AC-REV-16`, `AC-REV-18`–`AC-REV-20` | Full wrong-scope/identifier matrix, audit/redaction, export injection/isolation, non-reuse and reconstruction tests |
| Performance and UI state feed | `AC-REV-17`; review/Result accessibility criteria | Load/SLO evidence plus state-contract tests consumed by the later UI/UX specification |

Implementation acceptance requires transaction and process fault injection at
decision and Release boundaries, authoritative visibility tests under projection
lag, and the full configure-to-Release vertical-slice suite. Detailed keyboard,
screen-reader, focus, zoom/reflow, desktop, and narrow-layout evidence remains
owned by the downstream UI/UX specification and frontend implementation.

## Open questions

None. The approved specification resolves the product and policy questions.
Framework, library, and optional notification-delivery adapter choices remain
implementation details. Database, OIDC, secret, and gateway profiles and their
evidence gates are governed by current [operations](../operations/README.md) profiles. Selections must conform to this contract
without changing outcome semantics.

## Approval and downstream impact

Approval unblocks Review/Release implementation and requires conformance from:

- review-case, assignment, revision, decision, Result, Release, visibility,
  notification-outbox, correction, lifecycle, export, and audit modules;
- Participant and Reviewer query/API contracts;
- Reviewer UI implementation conforming to the approved [Evidence, Evaluation, and Human Review interaction specification](../ui-ux/evidence-evaluation-human-review.md), plus Release and Participant Result UI implementation conforming to the approved [Result and Release interaction specification](../ui-ux/result-release.md); and
- end-to-end isolation, failure, reconstruction, and Release-visibility tests.

## Related documents

- [MVP architecture](mvp-architecture.md)
- [Text Session runtime contract](session-runtime-contract.md)
- [Evidence and Evaluation execution contract](evaluation-execution-contract.md)
- [Architecture decisions](decisions/README.md)
