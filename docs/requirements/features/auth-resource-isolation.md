# Feature: Authorization and isolation

## Status and source

- Status: Approved
- Owner: Product Lead
- Approvers: Product Lead, Architecture Lead, Security/Privacy reviewer
- Approved date: 2026-08-06 (authorization/audit decision update; original feature approval 2026-08-05)
- Source: [Organization](../../product/concept-model.md#organization), [Session](../../product/concept-model.md#session), [Effective configuration resolution](../../product/concept-model.md#effective-configuration-resolution), [Product invariants](../../product/concept-model.md#product-invariants), [MVP validation slice](../../product/mvp-scope.md#mvp-validation-slice)
- Catalog entry: P0 #1 — [P0 authoring order](../README.md#p0-authoring-order)
- Related decisions: Approved defaults `PROP-1`–`PROP-10` in this specification. [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md) governs the policy-decision, enforcement, delegation, and freshness boundaries. [ADR-003](../../architecture/decisions/ADR-003-authorization-audit-persistence.md) governs authorization audit-event ownership and MVP persistence. Authentication mechanism, vendor policy engine, invitation credential format, and product-wide lifecycle policy remain deferred decisions.

## Problem and measurable outcome

Flex Agent operates protected resources across organizations, reusable agents and harnesses, activities, sessions, human and system actors, workflow stages, and derived records. These resources may include configuration, knowledge bindings, messages, attachments, submissions, evidence, evaluations, decisions, results, memory artifacts, operational data, and audit history.

Authentication establishes an identity; it does not establish permission to perform every action or access every resource. Every protected operation must therefore be authorized using trusted identity, organization ownership, delegated capabilities, resource relationships, and applicable workflow or visibility state.

Without one consistent authorization and isolation contract, an actor or service could access another organization, activity, resource subject, or session through direct identifiers, list and search endpoints, downloads, real-time channels, background work, caches, events, projections, exports, or administrative functions.

This feature establishes the generic, cross-cutting authorization and isolation contract for the Flex Agent platform. It applies regardless of whether an activity is an assessment, interview, coaching engagement, project review, support interaction, requirements-discovery session, or another structured conversational workflow.

The assessment MVP is an initial authorization profile built on this contract. In that profile, the primary resource subject is a participant, reviewers inspect assigned assessment records, and participant-facing results become visible only after an authorized release. These assessment-specific mappings do not limit the platform authorization model.

This specification does not select an authentication provider, prescribe a role-based-access-control implementation, or require a specific policy engine.

The measurable outcome is:

- Every protected operation evaluates authorization from trusted server-side identity, capability, scope, relationship, and state data.
- Cross-organization, cross-activity, cross-subject, and cross-session access is denied by default.
- List, search, count, export, download, background, event-driven, and real-time paths enforce the same scope as direct resource access.
- Human actors can access only the protected resources and actions covered by active membership, ownership, assignment, enrollment, or delegation.
- System services perform protected work only through explicit service identity and bounded delegation.
- Permission and assignment changes, sensitive mutations, exports, and security-relevant denials produce inspectable audit records.
- Automated positive and negative authorization tests cover every protected resource type before release.
- The assessment MVP additionally prevents cross-participant access and exposes participant-facing results only after authorized release.

## Actors and permissions

Permissions are capability-, action-, and resource-scoped. Named roles are administrative bundles for a particular activity profile; role names do not replace authorization checks.

### Platform actor categories

| Actor category                    | Permitted scope                                                              | Representative permitted actions                                                                    | Explicit restrictions                                                                                                          |
| --------------------------------- | ---------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| Unauthenticated visitor           | Public or authentication-entry surfaces only                                 | Begin authentication; view explicitly public content, if any                                        | Cannot read, list, create, update, delete, transition, export, or download protected resources                                 |
| Authenticated organization member | Active membership and explicitly delegated organization/resource scope       | Use protected organization features covered by active capabilities and relationships                | Membership alone does not grant unrestricted access to all organization resources or sensitive session content                 |
| Resource subject                  | Resources owned by, assigned to, or explicitly visible to that actor         | Participate in an activity; create or update permitted actor-owned content; view permitted outcomes | Cannot access another subject's private resources solely because both belong to the same activity, group, or organization      |
| Operator or administrator         | Own organization and delegated administrative scope                          | Configure or operate permitted agents, harnesses, activities, assignments, sessions, and records    | Cannot cross organizations, widen own permissions, or infer unrestricted sensitive-content access from an administrative title |
| Reviewer or decision-maker        | Assigned organization, activity, group, session, case, or record scope       | Inspect permitted records and perform delegated review or decision actions                          | Cannot access unassigned resources or perform actions outside the current workflow state                                       |
| System service                    | Explicit service identity and narrowly delegated organization/resource scope | Perform authorized background, scheduled, event-driven, or internal operations                      | Has no implicit global bypass and cannot infer scope solely from client-supplied identifiers or untrusted event payloads       |

A resource subject is the person or entity whose private activity or session data is being processed. The assessment MVP uses `Participant` as the resource-subject role. Future activity profiles may use terms such as candidate, interviewee, coachee, customer, requester, or project owner without changing the platform rules.

### Assessment MVP authorization profile

| MVP actor                  | Platform category          | MVP scope and permissions                                                                                                                                 |
| -------------------------- | -------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Participant                | Resource subject           | Own active enrollment, attempts, submissions, sessions, and participant-visible released results                                                          |
| Reviewer                   | Reviewer or decision-maker | Assigned activity, cohort, or session review scope; permitted submissions, transcripts, evidence, resolved configuration, evaluations, and review actions |
| Activity administrator     | Operator or administrator  | Delegated assessment setup, enrollment, cohort, session-operation, and review-assignment scope                                                            |
| Organization administrator | Operator or administrator  | Organization-level management plus only the sensitive-content capabilities explicitly delegated by policy                                                 |
| Assessment service         | System service             | Bounded assessment jobs, events, notifications, and processing under explicit organization and resource scope                                             |

A cohort is an administrative grouping. Cohort membership does not permit participants to access one another's sessions, submissions, transcripts, evidence, evaluations, or results.

## Scope

### Platform-wide scope

#### Platform-wide behavior in scope

- Authorization for protected create, read, list, search, count, aggregate, update, delete, transition, export, download, and administrative operations.
- Organization-level tenant isolation for all protected resources.
- Activity-, actor-, subject-, assignment-, group-, session-, configuration-, workflow-, derived-record-, and audit-resource scoping.
- Resource-subject self-access and prevention of cross-subject access.
- Reviewer, decision-maker, operator, and administrator assignment or delegation checks.
- Service-to-service and background-operation authorization.
- Enforcement across APIs, server-rendered routes, real-time connections, file access, jobs, events, caches, search indexes, projections, notifications, and exports.
- Permission grant, assignment, revocation, expiry, and propagation behavior.
- Authorization failure behavior and resource-existence confidentiality.
- Security-relevant authorization audit events and minimum audit fields.
- Positive and negative authorization and isolation verification requirements.
- Enforcement of workflow and visibility states supplied by the feature that owns the protected action.

### Assessment MVP profile

#### Assessment behavior in scope

- Participant enrollment, attempt, submission, and session ownership.
- Cross-participant and cross-session isolation, including within one cohort.
- Reviewer assignments at activity, cohort, or session scope.
- Administrative operation of assessment activities within delegated scope.
- Protection of submissions, transcripts, evidence, evaluations, review artifacts, release records, and participant-facing results.
- Enforcement of participant result visibility supplied by the review-and-release workflow.

### Out of scope

- Authentication-provider selection, account registration, password policy, MFA, SSO, identity recovery, and session-cookie or token format.
- General organization-management UX beyond the minimum membership, role-bundle, grant, and assignment data required by the MVP.
- Billing, licensing, and commercial entitlement rules.
- Biometric identity verification, human proctoring, and advanced cheating detection.
- Cross-organization sharing, federation, guest collaboration, and public protected-resource sharing.
- Shared multi-participant real-time sessions.
- Tool-specific permissions, which are defined by [`tool-execution-permissions.md`](tool-execution-permissions.md) in P2.
- Business rules for a particular review decision or result release, which are defined by [`review-result-release.md`](review-result-release.md) for the assessment MVP.
- Data-retention periods, consent wording, legal holds, and deletion policy, except that authorization must enforce whichever approved policies apply.
- Emergency access, support impersonation, or break-glass operation (excluded from MVP per approved default `PROP-6`; add only through a later approved requirement and ADR).
- A specific authorization library, role model, policy language, database row-security mechanism, cache design, or network topology.

## User journeys and state transitions

### Authorization context states

```text
Unauthenticated
    │ successful authentication
    ▼
Authenticated identity
    │ active organization membership or approved external relationship
    ▼
Organization-scoped actor
    │ active ownership, role bundle, assignment, enrollment, or delegated grant
    ▼
Resource-scoped actor
    │ action + resource + workflow-state authorization succeeds
    ▼
Authorized operation
```

A failure at any step results in `Denied`. Authorization is evaluated for each protected operation. UI visibility, possession of an identifier, group membership, prior access, or a previously opened page does not constitute continuing permission.

A grant or assignment has this lifecycle:

```text
Pending or absent → Active → Revoked or expired
                         │
                         └── may be narrowed by a more restrictive upper-scope policy
```

Revoked or expired access cannot transition back to active without a new authorized grant or assignment.

### Resource subject performs an activity action

1. The actor authenticates.
2. The system resolves the actor's active organization relationship and activity relationship.
3. The system verifies that the requested resource belongs to, is assigned to, or is explicitly visible to that actor.
4. The system verifies that the requested action is allowed in the current workflow and visibility state.
5. The actor receives only the permitted resource fields and actions.
6. Requests for another subject's private resource are denied without exposing whether that resource exists.

For the assessment MVP, the resource subject is the participant and the activity relationship is an enrollment.

### Reviewer or decision-maker inspects an assigned record

1. The reviewer authenticates.
2. The system resolves the reviewer's active organization membership and review assignment.
3. The system verifies that the requested activity, group, session, case, or record is within that assignment.
4. The system permits only the review or decision actions allowed by the current workflow state.
5. Unassigned resources and resources in other organizations are denied.

For the assessment MVP, assigned records may include submissions, transcripts, evidence, resolved configuration, evaluations, review artifacts, and release-ready results.

### Operator or administrator performs a protected operation

1. The operator authenticates.
2. The system resolves the operator's organization and delegated administrative scope.
3. The system verifies the target resource and all affected child resources belong to that organization and fall within the delegated scope.
4. The system authorizes the specific action rather than relying on page access or an administrative role label.
5. Sensitive mutations and exports are audited.

### System service performs delegated work

1. A trusted request, schedule, or event creates work under an explicit service identity and organization/resource scope.
2. The service validates the delegation and current authorization state before protected work begins.
3. The service processes only resources within that scope.
4. Retries, delayed execution, and untrusted event fields cannot redirect the work to another organization, subject, or session.
5. The execution and any sensitive side effects remain correlated to the initiating context.

### Access is revoked while an actor or service is active

1. An authorized administrator revokes a membership, role bundle, grant, assignment, enrollment, or delegation.
2. The change is recorded with actor, reason, and time.
3. New protected operations stop authorizing the revoked scope within the approved propagation target.
4. Existing real-time connections, cached authorization state, delayed work, and active service delegations are revalidated or terminated within that target.
5. A human actor is shown a non-disclosing access-expired message; unsaved protected content is not committed after authorization fails.

### Assessment MVP examples

#### Participant accesses an assigned assessment

1. The participant authenticates.
2. The system resolves the participant's active organization membership and enrollment.
3. The system verifies that the requested activity, attempt, submission, session, or released result belongs to that participant and is currently visible.
4. The participant receives only the permitted assessment resource and actions.
5. Requests for another participant's resource are denied without exposing whether that resource exists.

#### Reviewer inspects an assigned assessment session

1. The reviewer authenticates.
2. The system resolves the reviewer's active organization membership and review assignment.
3. The system verifies the requested session and linked resources are within that assignment.
4. The system permits only actions allowed by the review workflow and current state.
5. Unassigned sessions and resources in other organizations are denied.

### Prohibited transitions

- Unauthenticated to authorized operation without successful authentication.
- Organization-scoped actor to cross-organization resource access.
- Resource-subject relationship to access another subject's private resources.
- Administrative-group membership to shared private-resource access.
- Reviewer assignment to resources outside the assignment.
- Activity or session configuration to capabilities prohibited by organization policy.
- Revoked or expired grant to authorized operation without a new active grant.
- Client-provided organization, actor, subject, role, ownership, assignment, or authorization identifiers to trusted authorization context without server-side verification.
- System service to unrestricted or cross-organization execution without explicit delegation.

## Business rules

### Platform-wide rules

- `REQ-AUTH-1` — Every protected operation must authorize an authenticated human or system actor for a specific action on a specific resource using trusted server-side identity, organization scope, resource relationships, delegated capabilities, and current workflow or visibility state.
- `REQ-AUTH-2` — Access must be denied unless an active rule, ownership relationship, assignment, enrollment, or grant explicitly permits the actor, action, and resource combination.
- `REQ-AUTH-3` — Every protected resource must belong to exactly one organization either directly or through an unambiguous parent-resource chain.
- `REQ-AUTH-4` — An actor must not access a protected resource belonging to another organization, including through administration, review, background processing, caches, search, events, exports, or direct identifiers.
- `REQ-AUTH-5` — Lower scopes may narrow permissions or supply permitted parameters but must not widen permissions beyond organization policy or another explicitly delegated upper-scope boundary.
- `REQ-AUTH-6` — Organization membership or an administrative role label alone must not grant every action or unrestricted access to protected subject or session content; the requested action and delegated resource scope must also be authorized.
- `REQ-AUTH-11` — A system service must act through an explicit service identity or auditable delegation with no implicit unrestricted or cross-organization bypass.
- `REQ-AUTH-12` — List, search, count, aggregate, autocomplete, notification, export, and reporting operations must apply authorization before returning resource data or totals.
- `REQ-AUTH-13` — Authorization of a nested or linked resource must validate its complete trusted ownership chain, including organization and applicable activity, subject, assignment, group, session, or parent-record relationships.
- `REQ-AUTH-14` — File previews, downloads, generated links, and exports must be authorized for the requested artifact and must not permit access to another artifact by changing an identifier or reusing an access mechanism outside its approved scope.
- `REQ-AUTH-15` — The same authorization contract must apply to synchronous requests, real-time connections, background jobs, scheduled work, event consumers, retries, and administrative scripts.
- `REQ-AUTH-16` — Client-supplied organization, role, ownership, subject, assignment, or authorization values must be treated as untrusted input and verified against authoritative server-side data.
- `REQ-AUTH-17` — Creation of a protected resource must authorize the parent scope and assign organization and ownership relationships from trusted context rather than accepting them as authoritative client input.
- `REQ-AUTH-18` — A mutation or workflow transition must re-evaluate authorization at commit time so a stale page, token claim, cached decision, or earlier read does not authorize an action after permission or state has changed.
- `REQ-AUTH-19` — Revoked or expired memberships, grants, assignments, enrollments, and service delegations must stop authorizing new operations within the approved propagation target.
- `REQ-AUTH-20` — If required identity, membership, policy, assignment, ownership, delegation, or workflow-state data is unavailable or inconsistent, the protected operation must fail closed and must not partially mutate protected state.
- `REQ-AUTH-21` — An authorization denial must not reveal protected resource content, sensitive metadata, another actor's identity, or whether an inaccessible identifier exists.
- `REQ-AUTH-22` — Authorization failures must not create, update, delete, transition, release, export, or otherwise mutate the protected resource.
- `REQ-AUTH-24` — Session isolation must cover conversational state, subject data, messages, attachments, working context, evidence, decisions, outputs, derived records, and audit history associated with the session.
- `REQ-AUTH-25` — Caches, search indexes, queues, event payloads, temporary files, logs, and derived projections must preserve organization and resource scope and must not leak data across actors, subjects, activities, or sessions.
- `REQ-AUTH-26` — Access-control changes, reviewer or operator assignments, subject-activity relationships, sensitive mutations, outcome releases, exports, and security-relevant authorization denials must produce auditable events.
- `REQ-AUTH-27` — Authorization audit records must identify the actor or service, organization, action, resource type and stable reference, decision, reason code, timestamp, correlation reference, and the grant, relationship, assignment, or delegation used when applicable.
- `REQ-AUTH-28` — Authorization audit history must be append-only or equivalently tamper-evident; corrections must preserve the original event.
- `REQ-AUTH-29` — Audit and operational records must not contain credentials, authentication secrets, raw tokens, or unnecessary protected content.
- `REQ-AUTH-30` — Every protected resource type and operation must have automated positive and negative authorization coverage, including cross-organization, cross-subject, cross-session, and wrong-assignment cases where applicable.
- `REQ-AUTH-31` — An operation that policy classifies as requiring durable audit must not complete unless its audit event is durably accepted. This class must include access-control mutations, service delegations, protected-content exports and downloads, access to unreleased outcomes, outcome releases, and any other operation explicitly designated by approved policy. Routine authorized reads and security-relevant denials may complete through bounded durable buffering only when policy classifies them as bufferable; buffer exhaustion or inability to accept the event must fail the operation without silent loss.
- `REQ-AUTH-32` — Completing a session or activity must not by itself grant or remove access. A resource subject, reviewer, decision-maker, operator, or administrator may continue to access an available completed-session resource only while an active authorized relationship, delegated capability, applicable workflow state, and visibility policy permit that access. Organization membership or an administrative role alone must not grant continued access to sensitive session content, and participant outcome access must remain gated by release rules.
- `REQ-AUTH-33` — Authorization audit records must follow the applicable approved retention, deletion, legal-hold, and export policy. Until a more specific approved lifecycle policy applies, the system must preserve the minimum restricted audit metadata needed for investigation and reconstructability, must not duplicate raw protected content, and must not hard-code a product-wide retention duration.

### Assessment MVP business rules

- `REQ-AUTH-7` — A participant may access only the participant's own active enrollments, attempts, submissions, sessions, participant-visible resources, and released results.
- `REQ-AUTH-8` — Cohort membership must not grant a participant access to another cohort member's identity, enrollment, attempt, submission, session, transcript, evidence, evaluation, review, or result.
- `REQ-AUTH-9` — A reviewer may access only assessment resources covered by an active review assignment and may perform only review actions delegated for the current workflow state.
- `REQ-AUTH-10` — An assessment administrator may access only resources in the administrator's organization and within the administrator's active delegated activity or administrative scope.
- `REQ-AUTH-23` — Assessment result access must enforce the participant-visibility and release state supplied by the review-and-release workflow; this feature does not define the release transition itself.

## Data, evidence, and audit

### Authoritative authorization data

Authorization decisions depend on authoritative platform relationships, including:

- Human or service actor identity and active authentication context.
- Organization membership or approved organization relationship and its status.
- Delegated operator or administrative capabilities and resource scope.
- Reviewer or decision-maker assignment and assignment status.
- Resource-subject ownership, assignment, or participation relationship.
- Activity, group, session, case, record, and parent-resource ownership.
- Current workflow and visibility state supplied by the feature that owns the protected action.
- Service identity and delegated execution context for background or internal work.
- Grant, assignment, relationship, delegation, and policy effective and expiry times where applicable.

For the assessment MVP, these relationships include participant enrollment, participant identity, cohort membership, attempt and session ownership, reviewer assignment, result release state, and parent relationships among organization, activity, task, enrollment, attempt, session, submission, evidence, evaluation, review, release, result, and audit records.

Derived claims may accelerate a decision but must not override authoritative current state when a sensitive operation is committed.

### Access-control history

Memberships, role bundles, grants, assignments, subject-activity relationships, enrollments, service delegations, and revocations must preserve enough history to determine:

- Who created, changed, or revoked access.
- The organization and resource scope affected.
- The previous and new access state.
- The reason supplied for the change.
- When the change became effective.
- Which active sessions, connections, jobs, delegations, or cached decisions were affected when that information is available.

### Required audit events

At minimum, the system records:

- Organization membership activation, change, expiry, and revocation.
- Operator or administrative permission grant, change, and revocation.
- Reviewer or decision-maker assignment, reassignment, expiry, and revocation.
- Resource-subject activity relationship or enrollment activation, change, and revocation.
- Authorization-policy or organization-policy changes.
- Sensitive resource mutation and destructive action.
- Session pause or termination performed by an authorized operator.
- Access to or export of sensitive protected records according to the approved audit policy.
- Protected artifact download and bulk export.
- Review decision and outcome release, linked to the owning workflow event.
- Service delegation used for protected background work.
- Repeated or security-relevant cross-organization, cross-subject, cross-session, or identifier-enumeration denials.
- Fail-closed authorization errors that block a protected operation.

For the assessment MVP, protected audit targets include participant submissions, transcripts, evidence, evaluations, review artifacts, result release, and participant-result access where required by policy.

### Audit record minimum fields

Each authorization-related audit event contains:

- Event identifier.
- UTC timestamp with unambiguous ordering.
- Correlation or request identifier.
- Actor type and stable actor or service reference.
- Organization reference.
- Action.
- Resource type and stable resource reference.
- Activity, subject, group, or session reference when relevant.
- Authorization decision and stable reason code.
- Grant, role bundle, assignment, relationship, enrollment, or delegation reference used when applicable.
- Previous and new state for access-control changes.
- Supplied reason for an authorized administrative change.
- Source channel, such as interactive request, real-time connection, background job, event consumer, or administrative script.

Audit records reference protected content rather than copying messages, attachments, submissions, transcripts, evidence, evaluations, decisions, credentials, or other sensitive payloads into the event.

## Quality requirements

### UX and accessibility

- Navigation, lists, actions, and controls must expose only resources and operations the current actor may use; hidden UI is not a substitute for server-side authorization.
- An unauthenticated actor must receive a clear path to authenticate without seeing protected content.
- An authenticated but unauthorized actor must receive a clear, non-disclosing message that the resource or action is unavailable.
- Access-expired and permission-changed messages must explain the next safe action, such as returning to the assigned activity or contacting an administrator, without revealing restricted details.
- When authorization expires while a form or session control is open, the interface must prevent the protected commit and explain that access changed. Recoverable local input should not be silently discarded unless retaining it would create a privacy risk.
- Denial and access-expired states must be keyboard reachable, place focus on the message or next action, expose an accessible name and description, and not rely on color alone.
- Loading states must not briefly render protected data before authorization is resolved.
- Responsive and narrow-screen interfaces must preserve the same denial message, next action, and absence of protected content.

### Performance and reliability

- Authorization must be evaluated within the end-to-end response and interaction budgets of the owning feature.
- Authorization processing inside the service boundary must meet the approved objective of no more than 50 ms at the 95th percentile (`PROP-8`), excluding authentication-provider redirects and end-user network latency.
- Authorization queries and filters must be bounded and must not scan unrelated organizations or unbounded participant populations.
- Batch operations must authorize the complete selected resource set before mutation; a partial authorization failure must not silently process unauthorized items.
- Pagination, totals, and aggregate results must be calculated from the authorized resource set.
- Authorization infrastructure and policy-data failures must fail closed.
- Grant, assignment, and revocation operations must be idempotent where retries are possible.
- Cached authorization decisions must be scoped by trusted actor, organization, action, resource, and relevant policy or grant version.
- Permission changes must invalidate or supersede stale authorization decisions within the approved propagation target.
- Concurrent permission or workflow-state changes must be resolved against current authoritative state before a sensitive mutation commits.
- Authorization denials, policy-data failures, and revocation lag must be observable without logging sensitive payloads.

### Security and privacy

- Server-side authorization is required at every sensitive boundary regardless of client checks.
- Object-level and function-level authorization must prevent insecure direct-object reference and privilege-escalation paths.
- Identifiers must be treated as locators, not proof of access.
- Resource existence must not be disclosed to actors outside the permitted scope.
- Queries, caches, search indexes, events, object storage, file delivery, queues, and background work must include trusted organization and resource scope.
- Replayed requests, stale real-time connections, old browser tabs, retries, and delayed jobs must not retain authorization after the applicable grant expires or is revoked beyond the approved propagation target.
- Service accounts and internal jobs must use least privilege and explicit delegation; a trusted network location is not authorization.
- Authorization data and audit metadata must be minimized to what is needed for enforcement, investigation, and approved retention.
- Error responses, metrics, traces, and logs must not expose tokens, credentials, protected content or cross-scope identifiers.
- Repeated enumeration or cross-scope attempts must be detectable and subject to approved abuse controls.
- Negative tests must cover identifier substitution, parent-child mismatch, forged organization fields, unassigned reviewer access, participant-to-participant access, stale grants, cache isolation, background processing, downloads, exports, and list/count leakage.

## Acceptance criteria

### Platform-wide acceptance criteria

### `AC-AUTH-1` — Protected operations require authentication

- **Given** an actor is not authenticated
- **When** the actor requests any protected resource or operation
- **Then** the operation is denied
- **And** no protected content or sensitive resource metadata is returned
- **And** no protected state is changed.

### `AC-AUTH-4` — Cross-organization access is denied

- **Given** an authenticated participant, reviewer, administrator, or service identity belongs to organization A
- **When** it requests a protected resource owned by organization B without an explicitly approved cross-organization capability
- **Then** the operation is denied across direct reads, lists, counts, downloads, exports, events, jobs, and mutations
- **And** no data from organization B is returned or processed for organization A.

### `AC-AUTH-5` — Administrator access is delegated and action-specific

- **Given** an authenticated administrator belongs to the resource's organization
- **When** the administrator requests an operation
- **Then** the system verifies the administrator's active delegated resource scope and the specific action
- **And** organization membership by itself does not authorize an undelegated sensitive action.

### `AC-AUTH-7` — Lists, search, counts, and aggregates are scoped

- **Given** protected resources exist both inside and outside an actor's permitted scope
- **When** the actor lists, searches, filters, autocompletes, counts, aggregates, or pages through resources
- **Then** only authorized resources contribute rows, identifiers, totals, facets, page metadata, and suggestions
- **And** unauthorized-resource existence cannot be inferred from the response.

### `AC-AUTH-8` — Nested resources, files, and exports validate ownership chains

- **Given** an actor is authorized for one activity or session
- **When** the actor requests a nested resource, file, preview, download, or export using an identifier linked to a different activity, participant, session, or organization
- **Then** the complete trusted ownership chain is validated
- **And** the request is denied without returning the artifact or an externally usable access mechanism.

### `AC-AUTH-9` — Client-supplied scope cannot create cross-scope resources

- **Given** an actor may create a protected child resource under an authorized parent
- **When** the request supplies an organization, participant, owner, role, activity, or session identifier that conflicts with trusted parent context
- **Then** the conflicting value is rejected or ignored according to the contract
- **And** the resource cannot be created in or linked to an unauthorized scope
- **And** organization and ownership relationships are assigned from trusted context.

### `AC-AUTH-10` — Lower scopes cannot widen permissions

- **Given** organization policy or an upper-scope grant prohibits an action
- **When** an agent, harness, activity, session parameter, assignment, or request attempts to enable that action
- **Then** the configuration or operation is rejected
- **And** any authorized override path requires an actor permitted to override, a reason, and an audit event.

### `AC-AUTH-11` — Revocation stops new access

- **Given** an actor has an active membership, grant, assignment, or enrollment
- **When** an authorized administrator revokes or expires it
- **Then** new HTTP protected operations stop authorizing the revoked scope immediately after the authoritative change
- **And** stale caches, real-time connections, retries, and delayed jobs do not continue access beyond the approved propagation target of 60 seconds (`PROP-4`)
- **And** delayed jobs and service operations validate current scope before protected work begins
- **And** the revocation is audited.

### `AC-AUTH-12` — Authorization dependencies fail closed

- **Given** required identity, membership, assignment, policy, or ownership data is unavailable, times out, or is internally inconsistent
- **When** a protected operation is attempted
- **Then** the operation is denied or safely retried without granting access
- **And** no partial protected mutation is committed
- **And** an operationally actionable error is recorded without sensitive payloads.

### `AC-AUTH-13` — Denials are non-disclosing and side-effect free

- **Given** an authenticated actor lacks permission for a requested protected resource or action
- **When** authorization is evaluated
- **Then** the actor receives a generic unavailable or access-denied result appropriate to the interaction
- **And** the response contains no protected content, cross-scope identity, ownership detail, or existence confirmation
- **And** the resource, workflow, and audit-relevant business state are not mutated except for the authorization-denial audit event itself.

### `AC-AUTH-14` — Access-control changes are auditable

- **Given** an authorized administrator creates, changes, expires, or revokes a membership, grant, administrative scope, reviewer assignment, or enrollment
- **When** the change is committed
- **Then** an append-only audit event records the actor, organization, affected scope, previous state, new state, reason, time, and correlation reference
- **And** the original event remains inspectable if a later correction is made.

### `AC-AUTH-15` — Sensitive access and export are auditable

- **Given** the approved audit policy marks an operation as sensitive
- **When** an authorized actor accesses, downloads, or exports the protected resource
- **Then** the system records the actor, organization, action, resource reference, grant or assignment used, timestamp, source channel, and correlation reference
- **And** the audit event does not copy raw participant content or credentials.

### `AC-AUTH-16` — Background and service operations preserve actor and scope

- **Given** an authorized interactive request schedules a job or emits an event that will access protected data
- **When** the job or consumer executes
- **Then** it uses an explicit service identity and trusted organization/resource scope
- **And** it re-evaluates authorization or validates an auditable delegation appropriate to the operation
- **And** changing an untrusted event field cannot redirect processing to another organization or participant.

### `AC-AUTH-17` — Batch and concurrent operations do not partially bypass authorization

- **Given** a batch contains both authorized and unauthorized resources, or a grant changes while a mutation is pending
- **When** the batch or mutation reaches its authorization and commit boundary
- **Then** every affected resource is validated against current authoritative state
- **And** unauthorized items are not processed
- **And** the contract returns an explicit all-or-nothing failure or an itemized result without silently treating unauthorized items as successful.

### `AC-AUTH-19` — Session data remains isolated during concurrent use

- **Given** multiple sessions run concurrently under the same organization, activity, agent, harness, group, or workflow
- **When** their actors send messages, attach content, resume sessions, or retrieve session state
- **Then** each response and side effect is associated only with the authorized session and resource subject
- **And** messages, attachments, working context, evidence, decisions, outputs, and cached or real-time state do not cross sessions
- **And** for the assessment MVP, one participant's submission, transcript, evaluation, or result never appears in another participant's session.

### `AC-AUTH-20` — Access-denied interfaces are accessible

- **Given** an actor reaches an unavailable, revoked, expired, or unauthorized state through the UI
- **When** the denial is displayed
- **Then** focus moves to the denial message or safe next action
- **And** the message and next action have accessible names and descriptions
- **And** the state is operable by keyboard, does not rely on color alone, works at narrow viewport widths, and does not render protected content during loading.

### `AC-AUTH-21` — Protected resource types have negative authorization coverage

- **Given** a protected resource type or operation is introduced or changed
- **When** its verification suite runs
- **Then** tests cover authorized access, unauthenticated denial, wrong-organization denial, wrong-participant or wrong-assignment denial where applicable, forged identifiers, list/count leakage, and side-effect-free failure
- **And** the feature is not considered release-ready while an applicable negative case is missing or failing.

### `AC-AUTH-22` — Required audit acceptance gates sensitive operations

- **Given** an operation is classified by approved policy as requiring durable audit
- **When** its audit event cannot be durably accepted in the owning transaction or approved durable ingestion boundary
- **Then** the operation fails without committing the access-control change, delegation, mutation, disclosure, download, export, unreleased-outcome access, or release
- **And** a routine read or security-relevant denial completes asynchronously only when policy classifies the event as bufferable and a bounded durable buffer accepts it
- **And** buffer exhaustion, backpressure failure, or inability to accept the event fails the operation and produces an operational alert without silently losing or downgrading the event.

### `AC-AUTH-23` — Completed-resource access remains relationship- and state-scoped

- **Given** an activity or session is complete and its protected records remain available
- **When** a resource subject, reviewer, decision-maker, operator, or administrator requests one of those records
- **Then** access succeeds only while the actor retains an active authorized relationship and delegated capability and the resource's workflow and visibility state permit access
- **And** completion, organization membership, or an administrative role label alone does not authorize access
- **And** participant outcome access remains denied until the owning release workflow permits it.

### `AC-AUTH-24` — Audit lifecycle follows approved policy without content duplication

- **Given** authorization audit records contain the restricted metadata required for investigation and reconstructability
- **When** retention, deletion, legal hold, or export is evaluated
- **Then** the currently applicable approved lifecycle policy is enforced
- **And** when no more specific approved duration applies, the minimum required metadata remains restricted and available without a hard-coded product-wide duration
- **And** raw protected content is not duplicated into the audit records.

### Assessment MVP acceptance criteria

### `AC-AUTH-2` — Participant accesses an owned authorized resource

- **Given** an authenticated participant has active organization membership and an active enrollment
- **And** the requested attempt, submission, or session belongs to that participant
- **And** the action is permitted in the current workflow state
- **When** the participant performs the action
- **Then** the operation succeeds
- **And** the response contains only data within that participant's authorized scope.

### `AC-AUTH-3` — Participant cannot access another participant

- **Given** two participants belong to the same organization, activity, or cohort
- **When** one participant requests the other participant's enrollment, attempt, submission, session, transcript, evidence, evaluation, review artifact, or result by direct identifier or modified request
- **Then** the request is denied
- **And** the response does not reveal whether the identifier exists
- **And** no protected state is changed.

### `AC-AUTH-6` — Reviewer access requires an active assignment

- **Given** an authenticated reviewer belongs to the resource's organization
- **When** the reviewer requests a session or linked submission, transcript, evidence, resolved configuration, or evaluation
- **Then** access succeeds only when an active assignment covers that resource and action
- **And** an expired, revoked, or unrelated assignment is denied.

### `AC-AUTH-18` — Result visibility follows release state

- **Given** a participant owns the assessment session
- **When** the participant requests the result before the result-release workflow marks it visible to that participant
- **Then** the result, internal evaluation, reviewer notes, and release metadata are not returned
- **And** after an authorized release, the participant can access only the participant-facing result permitted by the release.

## Dependencies and rollout

### Platform dependencies

- A trusted authentication layer that supplies a stable human or service identity.
- Authoritative organization membership and access-control data.
- Resource models with unambiguous organization and parent ownership.
- Explicit activity, subject, assignment, group, session, and workflow-state relationships as required by each owning feature.
- An append-only or equivalently tamper-evident audit facility with UTC timestamps and correlation references.
- Approved [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md) for enforcement boundaries, policy representation, service delegation, freshness, file delivery, and event scope; approved [ADR-003](../../architecture/decisions/ADR-003-authorization-audit-persistence.md) for audit-event ownership and MVP persistence.

### Assessment MVP dependencies

- Participant enrollment and attempt ownership from [`submission-attempts.md`](submission-attempts.md).
- Session ownership and state from [`session-text-lifecycle.md`](session-text-lifecycle.md).
- Resolved configuration and organization-policy references from [`resolved-session-configuration.md`](resolved-session-configuration.md).
- Review assignments, review permissions, and participant-result visibility from [`review-result-release.md`](review-result-release.md).

### Rollout

- Authorization and organization ownership are mandatory foundations, not an optional customer-facing feature flag.
- Before protected production data is accepted, every protected resource type must have an organization owner and all required subject, activity, assignment, group, session, or parent relationships.
- Existing or seeded records without unambiguous ownership must be quarantined from normal access until repaired; they must not default to globally visible.
- A pre-release shadow or diagnostic mode may compare intended and actual policy decisions, but it must not permit an operation that the enforcing decision denies.
- Rollout proceeds resource family by resource family only when its positive and negative authorization matrix is automated and passing.
- Assessment-specific resources are added to the platform matrix rather than receiving a separate weaker authorization path.
- No bypass, support-impersonation, or emergency-access path is enabled in the MVP (`PROP-6`).

### Observability

Track at minimum:

- Authorization decisions by stable reason category, action, actor category, activity profile, and resource type.
- Denial rates and changes from baseline.
- Cross-organization, cross-subject, cross-session, and identifier-enumeration attempts.
- Policy-data lookup errors and fail-closed operations.
- Grant, assignment, enrollment, delegation, and revocation propagation lag.
- Active connections or delayed jobs terminated because permission changed.
- Sensitive exports and downloads.
- Audit-write failures and backlog.

Metrics and traces must use bounded labels and must not include raw protected content, credentials, tokens, or unrestricted resource identifiers.

## Approved decision disposition

The following decisions were approved on 2026-08-06. The cited requirements and acceptance criteria govern behavior; the original question and proposal IDs are retained for traceability.

| Question/proposal | Approved disposition | Authoritative location |
| --- | --- | --- |
| `Q-1` | Completion does not automatically grant or remove access. Continued access requires a current authorized relationship, delegated capability, applicable workflow state, visibility, and resource availability; participant outcomes remain release-gated. | `REQ-AUTH-32`, `AC-AUTH-23` |
| `Q-2` | Apply the approved lifecycle policy. Until a more specific policy applies, preserve only the minimum restricted audit metadata needed for investigation and reconstructability, do not duplicate raw protected content, and do not hard-code a product-wide duration. | `REQ-AUTH-33`, `AC-AUTH-24`, [ADR-003](../../architecture/decisions/ADR-003-authorization-audit-persistence.md) |
| `Q-3`, `PROP-10` | Fail closed when an operation requiring durable audit cannot have its event accepted; allow bounded durable buffering only for policy-classified routine reads or denials, with backpressure, alerting, retry, and no silent loss. | `REQ-AUTH-31`, `AC-AUTH-22`, [ADR-003](../../architecture/decisions/ADR-003-authorization-audit-persistence.md) |

The authorization-local questions are closed. A product-wide lifecycle policy must still define any specific retention periods, deletion schedules, legal-hold behavior, and organization export rules before those behaviors are implemented; this specification intentionally does not invent them.

## Approved defaults

These defaults are approved with this specification and govern MVP authorization behavior. Stable `PROP-*` IDs are retained for traceability.

- `PROP-1` — Keep the platform authorization model based on explicit capabilities, actions, relationships, and resource scope. For the assessment MVP UI, use four human role labels: `Organization administrator`, `Activity administrator`, `Reviewer`, and `Participant`. Role labels are permission bundles and must never be the sole authorization input.
- `PROP-2` — Give organization administrators authority to manage organization activities and assignments, but require an explicit activity-scoped sensitive-content capability to inspect raw subject/session content. For the assessment MVP, this includes submissions, transcripts, evidence, evaluations, and reviewer notes.
- `PROP-3` — Allow reviewer or decision-maker assignments at activity, group, or session scope. For the assessment MVP, default reviewer assignment to activity scope with optional cohort or session narrowing. A narrower assignment constrains access and never creates cross-organization permission.
- `PROP-4` — Require revocation to stop new HTTP operations immediately after the authoritative change and to terminate or revalidate cached decisions and real-time access within 60 seconds. Delayed jobs and service operations must validate current scope before protected work begins.
- `PROP-5` — Audit every access-control mutation, service delegation, protected-content download, bulk export, access to unreleased decisions or outcomes, and security-relevant denial. Record routine authorized reads through bounded access logs or approved sampling unless a later policy requires per-read audit.
- `PROP-6` — Do not provide support impersonation, emergency access, or a break-glass role in the MVP. Add one only through an approved requirement and architecture decision with least privilege, time limits, reason capture, elevated audit, approval, and organization notification.
- `PROP-7` — Return the same non-disclosing external response for an inaccessible protected identifier and a nonexistent identifier, while retaining distinct internal reason codes for operations and security investigation.
- `PROP-8` — Set an initial authorization-processing objective of no more than 50 ms at the 95th percentile inside the service boundary, excluding authentication-provider redirects and end-user network latency. Confirm or revise this target after representative load testing.
- `PROP-9` — Permit invitation or entry links only as expiring, revocable, single-purpose references that reveal no protected resource details before authentication. Possession of the link does not itself authorize protected access; the authenticated actor and intended relationship must still be verified.
- `PROP-10` — Fail closed when a policy-required audit record for a sensitive mutation or disclosure cannot be durably accepted; permit bounded durable buffering only for routine reads or denials that approved audit policy classifies as bufferable. Normative behavior is defined by `REQ-AUTH-31` and `AC-AUTH-22`.

## Traceability

The traceability matrix covers the generic platform contract. Rows that reference participants, enrollments, evaluations, or result release are assessment MVP profile mappings.

| Requirement/AC | Implementation | Automated verification | Playwright/manual evidence | Status |
| --- | --- | --- | --- | --- |
| `REQ-AUTH-1`, `REQ-AUTH-2`, `AC-AUTH-1`, `AC-AUTH-13` | Trusted identity and authorization boundary — approved [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md) | Protected-route contract tests; side-effect assertions | Unauthenticated and denied UI states | Gap |
| `REQ-AUTH-3`–`REQ-AUTH-6`, `AC-AUTH-4`, `AC-AUTH-5`, `AC-AUTH-10` | Organization ownership and delegated-scope enforcement — architecture TBD | Cross-organization matrix; upper/lower-scope conflict tests | Administrator scope and denial flows | Gap |
| `REQ-AUTH-7`, `REQ-AUTH-8`, `REQ-AUTH-24`, `AC-AUTH-2`, `AC-AUTH-3`, `AC-AUTH-19` | Enrollment, participant ownership, and session isolation — architecture TBD | Cross-participant and concurrent-session integration tests | Participant own-session and denied-resource journeys | Gap |
| `REQ-AUTH-9`, `REQ-AUTH-10`, `AC-AUTH-5`, `AC-AUTH-6` | Reviewer and administrator assignments — architecture TBD | Active, expired, revoked, and unrelated assignment tests | Reviewer assigned/unassigned states | Gap |
| `REQ-AUTH-11`, `REQ-AUTH-15`, `AC-AUTH-16` | Service identity and delegated background execution — approved [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md) | Job/event scope tampering and stale-delegation tests | Operational evidence only | Gap |
| `REQ-AUTH-12`–`REQ-AUTH-14`, `AC-AUTH-7`, `AC-AUTH-8` | Scoped queries, linked-resource validation, and artifact delivery — architecture TBD | List/count leakage; parent mismatch; download/export tests | Scoped tables, search, empty states, and denied downloads | Gap |
| `REQ-AUTH-16`, `REQ-AUTH-17`, `AC-AUTH-9` | Trusted parent-derived ownership — architecture TBD | Forged organization/owner field contract tests | Validation and denial messages | Gap |
| `REQ-AUTH-18`–`REQ-AUTH-20`, `AC-AUTH-11`, `AC-AUTH-12`, `AC-AUTH-17` | Commit-time reauthorization, revocation, and fail-closed behavior — approved [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md) | Concurrency, cache invalidation, timeout, retry, and batch tests | Permission-changed and access-expired states | Gap |
| `REQ-AUTH-21`, `REQ-AUTH-22`, `AC-AUTH-3`, `AC-AUTH-13` | Non-disclosing denial contract — architecture TBD | Existence-oracle and no-side-effect tests | Generic unavailable states | Gap |
| `REQ-AUTH-23`, `AC-AUTH-18` | Result visibility enforcement supplied by release workflow — architecture TBD | Released/unreleased participant-access tests | Participant result visibility states | Gap |
| `REQ-AUTH-25`, `AC-AUTH-4`, `AC-AUTH-7`, `AC-AUTH-8`, `AC-AUTH-16`, `AC-AUTH-19` | Scope-safe caches, search, queues, events, and projections — approved [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md) | Cache-key, index, event, job, and concurrent-session isolation tests | Operational evidence; no direct UI requirement | Gap |
| `REQ-AUTH-26`–`REQ-AUTH-29`, `REQ-AUTH-31`, `REQ-AUTH-33`, `AC-AUTH-14`, `AC-AUTH-15`, `AC-AUTH-22`, `AC-AUTH-24` | Audit event ownership, append-only persistence, durability gating, and lifecycle boundary — approved [ADR-003](../../architecture/decisions/ADR-003-authorization-audit-persistence.md) | Audit schema, append-only history, redaction, durability-class, buffer-failure, and lifecycle-policy tests | Audit-history and export evidence | Gap |
| `REQ-AUTH-32`, `AC-AUTH-23` | Completed-resource relationship, workflow, visibility, and release enforcement — owning resource and release architectures TBD | Completed-resource active/revoked relationship and released/unreleased visibility tests | Completed-session participant and reviewer access states | Gap |
| `REQ-AUTH-30`, `AC-AUTH-21` | Authorization verification matrix in CI — test design TBD | Resource/action positive and negative suite | Review checklist and test report | Gap |
| UX and accessibility requirements, `AC-AUTH-20` | UI access-state patterns — UI/UX spec TBD | Component accessibility tests where applicable | Keyboard, focus, narrow viewport, loading, and denial screenshots | Gap |
