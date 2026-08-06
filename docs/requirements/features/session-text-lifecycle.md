# Feature: Text session lifecycle

## Status and source

- Status: Approved
- Owner: Product Lead
- Approvers: Product Lead, Architecture Lead, UI/UX reviewer, Security/Privacy reviewer
- Approved date: 2026-08-06
- Source: [Session](../../product/concept-model.md#session), [Workflow model](../../product/concept-model.md#workflow-model), [Session state and events](../../product/concept-model.md#session-state-and-events), [Inspectable justification boundary](../../product/concept-model.md#evaluation-review-decision-result-and-release), [Product invariants](../../product/concept-model.md#product-invariants), [MVP validation slice](../../product/mvp-scope.md#mvp-validation-slice), [MVP executable workflow](../../product/mvp-scope.md#mvp-executable-workflow), and [Participant capabilities](../../product/mvp-scope.md#participant-capabilities-mvp)
- Catalog entry: P0 #5 — [P0 authoring order](../README.md#p0-authoring-order)
- Related requirements: Consumes authorization and isolation from [`auth-resource-isolation.md`](auth-resource-isolation.md), the frozen configuration and manifest from [`resolved-session-configuration.md`](resolved-session-configuration.md), the activated text workflow from [`assessment-setup.md`](assessment-setup.md), and the active Attempt plus exact Submission binding from [`submission-attempts.md`](submission-attempts.md). Supplies a terminal, ordered transcript and lifecycle record to [`evidence-evaluation.md`](evidence-evaluation.md).
- Related decisions: Approved defaults `PROP-1`–`PROP-8` in this specification. [ADR-001](../../architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md) governs manifest integrity and terminal sealing. [ADR-002](../../architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md) governs interactive, real-time, and service authorization. [ADR-003](../../architecture/decisions/ADR-003-authorization-audit-persistence.md) governs durable audit. [ADR-005](../../architecture/decisions/ADR-005-atomic-attempt-start-and-submission-binding.md) governs the committed readiness boundary from which this lifecycle begins. [ADR-006](../../architecture/decisions/ADR-006-mvp-architecture-baseline-and-evolution.md) governs the SPA/API/gateway, request/response plus SSE, worker, persistence, and recovery baseline. The [MVP operational defaults](../mvp-operational-defaults.md#protected-data-lifecycle-defaults) govern the default transcript, configuration, manifest, and working-context lifecycle. Detailed Session turn ordering, timer accounting, participant-visible work-trace publication, and terminal-transition realization still require architecture review.
- Decision approval: `PROP-1`–`PROP-8` and the participant-visible work-trace direction were approved on 2026-08-06.

This approved specification is authoritative for observable text Session lifecycle behavior in the MVP. Architecture, UI/UX, implementation, and downstream specifications must preserve its stable requirements, acceptance criteria, and approved decision dispositions.

## Problem and measurable outcome

After an Attempt crosses the atomic start boundary, the participant needs a fair, recoverable, isolated text examination rather than a generic chat. The system must preserve which instructions were acknowledged, which messages were accepted and shown, how time was accounted for, why the Session paused or ended, and which exact terminal transcript may be evaluated.

The lifecycle is sensitive to races and partial failure. Duplicate sends must not create duplicate turns. A reconnect must not create a new Session or restart its timer. A late message must not cross a terminal cutoff. An administrator must not pause or terminate a Session without current delegated authority. Model, audit, or persistence failure must not produce a transcript that claims content was accepted or shown when it was not.

The measurable outcome is:

- Every started assessment Session begins only from the committed Attempt, resolved configuration, manifest, exact Submission binding, and readiness state defined by ADR-005.
- Every required instruction or consent acknowledgment is versioned, actor-bound, and verified before the start commit.
- Every accepted participant message, participant-visible Agent message, and participant-visible Agent work-trace update has one stable identity, one unambiguous Session-local order, and an immutable historical representation.
- Duplicate, concurrent, retried, stale, or cross-Session commands cannot create duplicate or mis-scoped transcript entries or state transitions.
- The participant can reconnect to the same authoritative Session state without losing accepted messages, silently extending time, or creating another Attempt.
- Active-time, paused-time, warnings, and terminal cutoff are derived from server-authoritative state and remain reconstructable in UTC order.
- Pause, resume, completion, termination, and abort transitions preserve actor, reason, prior state, new state, timing effect, and audit provenance.
- No new message or model operation begins after the authoritative terminal cutoff.
- A terminal Session exposes one sealed transcript cutoff and lifecycle summary to evidence/evaluation consumers without automatically releasing a Result.
- Automated verification covers authorization, isolation, ordering, idempotency, timing boundaries, reconnect, failure recovery, terminal races, audit failure, accessibility, and protected-content handling.

## Actors and permissions

All actions are governed by [`auth-resource-isolation.md`](auth-resource-isolation.md). A role label, cohort membership, browser connection, access link, Session identifier, or possession of transcript content is not proof of permission.

| Actor | Permitted actions and scope | Explicit restrictions |
| --- | --- | --- |
| Participant | Within the participant's own active authorized Session, review participant-visible instructions and timing; record required acknowledgments; send text; receive Agent messages and enabled work-trace updates; view status and time; reconnect; and request completion | Cannot select another participant, Session, Attempt, configuration, Submission binding, message author, sequence, timer value, pause interval, or terminal reason; cannot edit or delete an accepted message; cannot resume, widen, or reopen a terminal Session |
| Activity administrator | Within current delegated activity and Session-control scope, monitor bounded operational state; pause, resume, or terminate when the governing policy permits; inspect transcript content only with an explicit sensitive-content capability | Cannot infer control authority from organization membership or a role label, silently change timing/configuration, impersonate the participant, author participant messages, rewrite transcript history, reopen a terminal Session, or inspect raw content without permission |
| Organization administrator | Within explicitly delegated organization and action scope, inspect bounded operational and audit state and manage applicable upper-scope policy | Organization membership alone does not grant Session control, transcript access, or a bypass of activity, participant, or fairness boundaries |
| Assigned reviewer | Within an active review assignment and applicable workflow state, inspect the terminal transcript, lifecycle summary, timing facts, and protected references made available for review | Cannot join the participant conversation, send or edit messages, pause/resume the Session, inspect an unassigned Session, or release a Result through this feature |
| Session execution service | Under explicit service identity and bounded delegation, enforce state and timing, accept ordered commands, use the frozen configuration, request Agent generation, publish permitted Agent messages and work-trace updates, append manifest records, and commit lifecycle transitions | Cannot trust client scope, use mutable configuration, call disabled tools, learn from participant data, cross Session boundaries, expose prohibited internal reasoning, or continue after terminal cutoff |
| Model provider adapter | Under the frozen model binding and bounded service delegation, process the exact permitted turn context and return generation outcomes | Cannot grant authority, change workflow state, access another Session, use capabilities absent from the resolved configuration, or make a generated state instruction authoritative |
| Audit or compliance reviewer | Within explicitly delegated scope, inspect lifecycle, message-order, timing, recovery, transition, and access history | Cannot use audit access to obtain unrestricted raw transcript content, hidden prompts, credentials, unrelated participant records, or unrestricted exports |

## Scope

### In scope

- Participant-visible pre-start instructions, rules, consent notices, and required acknowledgment records used by the Session start boundary.
- Live execution after ADR-005 commits the Session readiness state.
- One participant in one isolated text Session.
- Ordered participant and Agent text messages plus participant-visible Agent work status and concise reasoning summaries under the frozen workflow and adaptive-follow-up policy.
- Message admission, idempotency, ordering, pending state, Agent generation, publication, retry, cancellation, and safe failure.
- Server-authoritative Session duration, hard timing bounds, remaining-time display, warning events, pause intervals, and terminal cutoff.
- Authorized administrative pause, resume, and termination.
- Participant completion, workflow-permitted completion, time-expiry completion, and unrecoverable abort.
- Connection loss, reconnect, stale client, multiple-tab/device, provider failure, persistence failure, manifest-append failure, and audit failure behavior.
- Append-only transcript and lifecycle history with exact visible-content references, authorship, state, order, and timestamps.
- Terminal transcript cutoff, lifecycle summary, Attempt terminal mapping, manifest terminal state/seal handoff, and downstream evaluation readiness.
- Participant, administrator, and reviewer states for instructions, ready, starting, active, thinking, waiting, reconnecting, paused, expiring, completing, completed, terminated, aborted, permission-changed, and unavailable conditions.

### Out of scope

- Authentication, role design, general authorization policy, invitation delivery, enrollment, attempt entitlement, retry entitlement, or start-window calculation.
- Activity/cohort activation, text-workflow authoring, adaptive-follow-up policy authoring, rubric authoring, configuration resolution, or the atomic Attempt/session-start transaction.
- Creating or replacing Submission versions. If the frozen requirement permits later in-Session material, [`submission-attempts.md`](submission-attempts.md) and ADR-005 govern exact-version intake and binding; this feature only gates use by current Session state.
- Defining Evidence semantics, generating or revising an Evaluation, making a Review decision, constructing a Result, or releasing a Result.
- Voice, playback, interruption, tool execution, Dynamic memory, memory candidates, cross-participant learning, shared real-time Sessions, video, proctoring, cheating detection, or biometric verification.
- Participant message editing or deletion after acceptance, private direct messages to administrators or reviewers, reviewer participation in the examination, or administrator impersonation.
- Offline authoring that can be submitted without reauthorization, multi-device collaborative composition, or continuing a Session while the service is unreachable.
- Product-wide retention durations, deletion schedules, legal-hold rules, consent wording, model-provider selection, transport protocol, queue topology, or database technology.

### Boundary terms

- An **accepted participant message** is participant-authored text durably committed to the authoritative Session transcript. A local draft, optimistic placeholder, rejected command, or transport retry is not accepted transcript content.
- A **published Agent message** is Agent-authored text durably committed and authorized for participant display. An internal prompt, partial generation, failed candidate, retry candidate, or hidden evaluation instruction is not published transcript content.
- A **published Agent work-trace update** is a durably recorded participant-visible status label or concise authored reasoning summary about the current turn. It is a product-facing explanation, not raw model chain-of-thought, and it must remain distinguishable from the final Agent message and trusted system status.
- A **turn** links one accepted participant message to zero or one published Agent response plus the generation attempts and terminal outcome needed to explain processing. The frozen workflow may define Agent-initiated opening or closing messages separately.
- The **terminal transcript cutoff** is the last committed Session-local sequence included in the completed, terminated, or aborted transcript. Later commands cannot change it.
- Connection state is not Session state. Closing a tab or losing a socket does not by itself create, complete, pause, terminate, or reset a Session.

## User journeys and state transitions

### Session lifecycle

The pre-start instruction and acknowledgment state is associated with the authorized enrollment/Attempt request. The `Active` Session exists only after the atomic start boundary commits.

```text
Instructions required
  ├── missing/declined/expired acknowledgment ──► Start blocked
  └── requirements satisfied
                   ▼
                 Ready
                   │ ADR-005 atomic start commit
                   ▼
                 Active ◄──────── authorized resume ──────── Paused
                   │  │                                          ▲
                   │  └── authorized/system safety pause ────────┘
                   │
                   ├── participant/workflow completion request ──┐
                   ├── time expires ──────────────────────────────┤
                   ├── authorized administrative termination ─────┤
                   └── unrecoverable execution failure ───────────┤
                                                                  ▼
                                                              Completing
                                                                  │ transcript cutoff + lifecycle/manifest commit
                                      ┌───────────────────────────┼───────────────────────────┐
                                      ▼                           ▼                           ▼
                                  Completed                   Terminated                    Aborted
```

`Completed`, `Terminated`, and `Aborted` are terminal. The terminal record and transcript cutoff are immutable. Corrections, annotations, later verification failures, or lifecycle-policy actions are appended separately and never reopen or rewrite the Session.

Connection states such as `Connecting`, `Connected`, `Reconnecting`, and `Offline` are projections over the lifecycle. They do not independently stop time or change the authoritative state.

### Participant reviews instructions and acknowledges requirements

1. The participant opens the authorized assignment and sees the participant-visible task instructions, Session format, effective timing and timezone facts, Attempt consequence, completion behavior, data-use notice, and any acknowledgment or consent requirements frozen for the cohort and permitted for display.
2. The interface distinguishes information from a required acknowledgment and identifies the exact version being acknowledged without exposing hidden configuration.
3. The participant deliberately records each required acknowledgment. Silence, continued browsing, a preselected control, or an inaccessible Session link is not acknowledgment.
4. The service reauthorizes the actor, verifies the exact instruction/notice version and scope, and records actor, version, outcome, and UTC time.
5. The start boundary revalidates every required acknowledgment. A missing, declined, withdrawn, stale, cross-scope, or superseded record blocks start without consuming an Attempt.

### Participant conducts the text examination

1. After the atomic start commit, the participant enters the `Active` Session and sees the authoritative transcript, status, effective end conditions, remaining time, and exact bound Submission summary permitted for display.
2. The participant composes a local draft. Draft text is not sent, audited as transcript content, or available to the Agent until the participant deliberately sends it.
3. The send command carries an idempotency key and Session locator; the service derives and validates the participant, Attempt, state, timer, configuration, and ownership from trusted data.
4. A permitted message is committed once with a stable identifier and Session-local order. The UI replaces any optimistic placeholder with the authoritative message state.
5. The execution service builds the next turn only from the frozen configuration and exact authoritative Session context, including permitted Submission material and prior published transcript content.
6. When enabled by the frozen participant-visible work-trace policy, the Session may publish predefined work-status labels and concise authored reasoning summaries while the turn runs. Each update is committed before display and remains distinct from hidden reasoning and the final answer.
7. The Agent response is validated against the enabled text-only capability and frozen workflow, then committed as one published Agent message before it is represented as authoritative transcript content.
8. The participant may send the next permitted message or request completion. The workflow, not model-authored text, controls allowed transitions.

### A turn fails and recovers

1. The participant message commits, but provider generation, validation, manifest append, or Agent-message publication fails or times out.
2. The accepted participant message remains visible and immutable; the system does not ask the participant to retype it or silently remove it.
3. The turn enters a bounded failed/retryable or paused state with a safe explanation.
4. A retry uses the same accepted participant message and a stable turn identity. It records distinct generation-attempt provenance without publishing duplicate Agent messages.
5. At most one Agent response becomes the published response for that turn unless the frozen workflow explicitly permits multiple ordered Agent messages.
6. If safe recovery cannot preserve ordering, authorization, or manifest/audit integrity, the Session pauses or transitions through `Completing` to `Aborted` according to the governing policy.

### Participant loses connection and reconnects

1. The client loses network or real-time connectivity while the Session remains non-terminal.
2. The interface enters `Reconnecting` without claiming that the Session, timer, pending message, or Agent turn paused.
3. On reconnection, the service reauthenticates and reauthorizes the participant, then returns the authoritative lifecycle state, timer facts, transcript sequence, pending turn status, and terminal state if one occurred.
4. The client reconciles local idempotency keys and discards or marks stale optimistic projections; it does not resend a command blindly.
5. If access was revoked or the Session became terminal, protected live content and controls are removed and the participant receives the applicable non-disclosing next action.

### Administrator pauses and resumes a Session

1. An Activity administrator with current delegated Session-control authority requests pause with a bounded reason category and optional protected note.
2. The service reauthorizes at commit time and orders the transition against pending messages, generation, timing, and terminal commands.
3. On successful pause, new participant sends and new Agent generations are blocked. An in-flight turn reaches the configured cancellation or commit boundary and its outcome is recorded explicitly.
4. The participant receives an accessible paused state, who may resolve it in non-sensitive terms, and whether the timer continues under the approved timing policy.
5. Resume reauthorizes the administrator and participant relationship, validates remaining permitted time and non-terminal state, records the pause interval, and returns to `Active` only if the Session may still continue.

### Session completes, terminates, or aborts

1. A participant completion request, workflow-permitted completion, time expiry, authorized administrative termination, or unrecoverable failure initiates `Completing`.
2. The service establishes one server-authoritative terminal command/order that rejects later message admission, then resolves, cancels, or records any already accepted pending turn according to the frozen completion policy.
3. After pending work reaches its governed outcome, the terminal commit finalizes the immutable transcript cutoff and records prior/new state, terminal category and reason, actor/service, timer summary, last transcript sequence, exact Session/configuration/manifest/Attempt/Submission bindings, and UTC ordering.
4. Under approved decision `PROP-5`, normal participant/workflow completion and configured time expiry map to terminal `Completed`; authorized early administrative termination maps to `Terminated`; unrecoverable platform or integrity failure maps to `Aborted`.
5. `Completed` maps the owning Attempt to `Completed`. `Terminated` and `Aborted` map it to `Aborted`; none restores consumed entitlement. Another try follows the remaining-limit or retry-entitlement rules in [`submission-attempts.md`](submission-attempts.md).
6. The manifest records the terminal state and seal required by `REQ-RSC-36` and ADR-001. Evidence/evaluation handoff remains blocked until the authoritative terminal record and required manifest state are available.
7. The participant receives a completion confirmation or safe terminal explanation. No evaluation, review, result, score, or release status is implied.

### Prohibited transitions and actions

- `Ready` to participant interaction without the ADR-005 atomic start commit.
- `Active` to a changed frozen configuration, Submission start binding, participant, Attempt, or ownership chain.
- `Paused` to participant send or new Agent generation.
- `Completed`, `Terminated`, or `Aborted` to `Active`, `Paused`, or a changed terminal cutoff.
- A client clock, local countdown, socket state, browser focus, or model output to authoritative timing or lifecycle state.
- A generated Agent statement such as “the Session is complete” to a state transition without the frozen workflow and authoritative commit.
- A participant, model, administrator, retry, event, or stale client to author or alter another actor's message, sequence, timer, pause, or terminal reason.
- A duplicate idempotency key with different content or scope to reuse the earlier successful outcome.
- A failed, cancelled, or unpublished generation to appear as participant-visible transcript content.
- A later Submission version or mutable `latest` alias to replace the exact bound material used by the Session.
- A Session transcript, draft, or working context to become memory, calibration material, or cross-participant learning input in the MVP.

## Business rules

### Entry, instructions, and authoritative state

- `REQ-SESS-1` — Live Session execution must begin only from the committed readiness state created by ADR-005, with exactly one bound Attempt, participant, enrollment, activity/cohort baseline, resolved configuration, execution manifest, and exact Submission-version binding.
- `REQ-SESS-2` — Before start, the participant must be shown the currently applicable participant-visible instructions, rules, effective timing facts, Attempt consequence, completion behavior, and approved data-use or consent notice required by the frozen policy.
- `REQ-SESS-3` — Every required acknowledgment must be deliberate and must record a stable acknowledgment identifier, participant, enrollment/Attempt scope, exact notice or instruction version, outcome, UTC time, and authoritative order; silence, a preselected control, or mere Session access must not count.
- `REQ-SESS-4` — The atomic start boundary must reauthorize the participant and verify that every required acknowledgment remains current, affirmative where required, and applicable to the exact enrollment/Attempt; missing, declined, withdrawn, stale, or cross-scope acknowledgment must block start without consuming entitlement.
- `REQ-SESS-5` — The system must maintain one canonical Session state and ordered event history used by text execution, timers, pause/resume, terminal transitions, manifest provenance, evidence handoff, and audit.
- `REQ-SESS-6` — Client-supplied participant, organization, activity, cohort, enrollment, Attempt, Session, configuration, manifest, Submission, message-author, order, state, timer, or terminal values are untrusted locators or requests and must not become authoritative without server-side validation.
- `REQ-SESS-7` — Every Session command must authorize the human or service actor for the exact action and complete ownership chain at admission and again at the protected commit boundary when state, timing, disclosure, or fairness may have changed.

### Text messages and turns

- `REQ-SESS-8` — The frozen text workflow/policy must define positive bounded participant-message size, Session turn/message limits where applicable, permitted Agent-opening/closing behavior, concurrent-pending-turn policy, generation timeout/retry bounds, completion handling for a pending turn, adaptive-follow-up constraints, participant-visible work-trace policy, and configurable warning schedule before the Session starts.
- `REQ-SESS-9` — A participant message may be accepted only while the Session is `Active`, current authorization permits it, the server-authoritative terminal cutoff has not occurred, and all frozen message and rate limits are satisfied.
- `REQ-SESS-10` — Each accepted participant message must have a stable identifier, Session and participant ownership, author type, immutable protected content or content reference, Session-local order, accepted UTC time, idempotency/correlation reference, and turn linkage.
- `REQ-SESS-11` — Message admission must be idempotent and concurrency-safe. Equivalent retries must return the same authoritative message; reuse of an idempotency key with different trusted content or scope must fail without altering the original.
- `REQ-SESS-12` — Accepted participant messages must not be edited, deleted, reordered, reassigned, or silently replaced. A participant correction is a new ordered message.
- `REQ-SESS-13` — Local drafts and optimistic UI placeholders must remain distinguishable from accepted transcript content and must not be exposed to the Agent, reviewer, audit transcript, evidence, or another device before authoritative acceptance.
- `REQ-SESS-14` — Agent generation must use the trusted Session binding, frozen configuration, permitted exact Submission material, approved memory-read state, and authoritative published transcript; it must not re-resolve mutable sources or accept participant content as policy, authorization, tool approval, or workflow authority.
- `REQ-SESS-15` — Each generation attempt must record stable turn/configuration/model references, ordered timing, outcome, bounded failure category, and protected input/output references required by the manifest without copying unnecessary raw content into audit or operational logs.
- `REQ-SESS-16` — Agent text becomes authoritative participant-visible transcript content only after it is durably committed as a published Agent message with stable identity, Session-local order, exact visible content or protected reference, model/generation provenance, publication time, and turn linkage.
- `REQ-SESS-17` — Failed, cancelled, superseded-before-publication, or partial generation must not be represented as a published Agent message. If any partial content was exposed to the participant, the system must preserve exactly what was exposed as a distinct published record or terminate the turn with an explicit visibility record; it must not claim the content was never shown.
- `REQ-SESS-18` — Retrying a failed turn must reuse the accepted participant message and stable turn identity, preserve each generation-attempt outcome, and prevent multiple Agent responses from being published accidentally for the same response slot.
- `REQ-SESS-19` — Text, links, markup, code, and metadata from participants, Submissions, models, or knowledge sources must be treated as untrusted content and rendered without enabling script execution, hidden capability escalation, external retrieval, tool execution, or a state transition.
- `REQ-SESS-51` — The frozen participant-visible work-trace policy must define whether work status and concise reasoning summaries are displayed, which predefined status labels and summary fields are permitted, update-rate and length bounds, accessibility behavior, and whether a final “Why this response or question?” explanation accompanies the published Agent message.
- `REQ-SESS-52` — A participant-visible work-trace update must be durably committed before display with a stable identifier, Session/turn ownership, type, exact displayed content, Session-local order, UTC publication time, and generation/configuration provenance. It must remain distinguishable from the final Agent message, trusted system status, and unpublished internal processing.
- `REQ-SESS-53` — A participant-visible work-trace update must be a concise Agent-authored explanation for the participant and must not expose or claim to reproduce raw chain-of-thought, hidden prompts, rubric internals, expected-answer criteria, secrets, security controls, unrelated participant data, or protected reviewer-only content.
- `REQ-SESS-54` — If a proposed work-trace update violates the frozen visibility policy or cannot be safely recorded, the system must suppress that update and continue with a permitted generic status or fail the affected publication safely; it must not leak the prohibited content through errors, logs, analytics, or fallback text.

### Timing, warnings, pause, and connection recovery

- `REQ-SESS-20` — Session timing must derive from the authoritative start commit, frozen timing policy, participant's permitted Session window, approved accommodations, authoritative service time, and ordered pause intervals; client clocks and countdowns are display aids only.
- `REQ-SESS-21` — Persisted start, message, pause, resume, warning, expiry, completion, termination, abort, and reconnect-reconciliation times must use UTC and retain the named timezone needed to explain applicable wall-clock boundaries.
- `REQ-SESS-22` — The system must track elapsed active time, elapsed paused time, remaining duration, and any absolute hard endpoint as distinct facts and must not extend an absolute activity/enrollment deadline through a client delay, reconnect, or unauthorized pause.
- `REQ-SESS-23` — Remaining time and status must be recalculated from authoritative state after reload, reconnect, device change, pause/resume, or stale-client reconciliation; the client must not persist or restore a more favorable timer value.
- `REQ-SESS-24` — Time-warning thresholds and participant-facing warning content must be configurable within organization, Agent, Harness, and Activity policy bounds and frozen in the Session's resolved workflow. A timed Session must have a valid non-empty schedule whose thresholds are positive, resolvable against the effective duration, and non-duplicative after resolution. Each configured threshold must be emitted at most once, and the platform must not substitute universal fixed thresholds. Warning history must remain reconstructable, and late or missed delivery must not change the terminal boundary.
- `REQ-SESS-25` — Closing a tab, losing focus, changing device, or losing transport must not by itself create another Session, restart timing, pause, complete, terminate, or abort the Session.
- `REQ-SESS-26` — Reconnect must reauthenticate and reauthorize the participant, return state and transcript changes after a trusted last-seen sequence, reconcile pending idempotency keys, and prevent stale controls from committing against newer state.
- `REQ-SESS-27` — A pause transition must record current and new state, authorized actor/service, bounded reason, UTC interval boundary, timer effect, in-flight-turn disposition, and audit correlation. While paused, new participant messages and Agent generations must be blocked.
- `REQ-SESS-28` — Resume must reauthorize the control actor and participant relationship, validate the Session is paused and non-terminal with permitted time remaining, close exactly one pause interval, and return to `Active` without changing the frozen configuration or transcript history.
- `REQ-SESS-29` — Confirmed enrollment suspension or other recoverable authorization loss must immediately block new Session commands and must pause or keep paused the Session within the approved revocation-propagation target. Confirmed enrollment revocation, ownership loss, or non-recoverable authorization loss must terminate the live Session under the approved terminal policy without deleting history.
- `REQ-SESS-30` — Required identity, authorization, Session-state, timer, persistence, manifest, or audit dependency failure must fail closed for the affected operation. It must preserve accepted content, expose a safe retry/reconnect/paused state when possible, and must not fabricate success or continue uncontrolled execution.

### Completion, termination, and downstream handoff

- `REQ-SESS-31` — Participant-requested completion must require a deliberate action, reauthorize at commit time, explain that no further messages will be accepted, and be idempotent under retries.
- `REQ-SESS-32` — A model output may recommend or signal completion only when the frozen workflow permits it; the execution service must independently validate and commit the transition. Model text alone must never be authoritative state.
- `REQ-SESS-33` — Time expiry must use authoritative service time and the approved boundary rule. At the terminal boundary the service must order the expiry against concurrent message/turn commands and must not accept a command whose authoritative commit order falls after the cutoff.
- `REQ-SESS-34` — Authorized administrative termination must require current delegated Session-control permission, a deliberate confirmation, and a bounded reason; it must not rewrite prior messages, timer facts, configuration, Submission binding, or Attempt consumption.
- `REQ-SESS-35` — Entering `Completing` must establish one authoritative terminal command/order and prevent new message admission and new Agent generation while pending turns are resolved, cancelled, or recorded according to the frozen completion policy.
- `REQ-SESS-36` — The terminal commit must atomically or through an architecture-approved equivalent consistency boundary record the terminal Session state and reason, actor/service, authoritative terminal time/order, last transcript sequence, timing summary, Attempt terminal mapping, and required lifecycle/audit event; failure must leave an honest recoverable `Completing` or fail-closed state rather than a false terminal success.
- `REQ-SESS-37` — `Completed`, `Terminated`, and `Aborted` records and their transcript cutoffs must be immutable. Later annotations, verification findings, lawful content unavailability, or corrections must be appended separately.
- `REQ-SESS-38` — Under approved terminal mapping `PROP-5`, participant/workflow completion and time expiry result in Session `Completed` and Attempt `Completed`; authorized early termination results in Session `Terminated` and Attempt `Aborted`; unrecoverable execution or integrity failure results in Session and Attempt `Aborted`.
- `REQ-SESS-39` — A terminal transition must not restore consumed entitlement, delete the Attempt, or authorize another try. A later try requires remaining baseline allowance or a separately authorized retry entitlement under [`submission-attempts.md`](submission-attempts.md).
- `REQ-SESS-40` — Evidence/evaluation handoff may begin only after the terminal Session record, transcript cutoff, Attempt mapping, required manifest terminal state/seal, and protected references are authoritative; the handoff must not itself create, approve, revise, or release a Result.
- `REQ-SESS-41` — No participant message, Agent message or work-trace publication, model call, Submission-use binding, or evidence-producing turn may be appended as live Session activity after the terminal cutoff. Late retries must return the existing terminal outcome without mutation.

### Transcript, audit, privacy, and lifecycle

- `REQ-SESS-42` — The authoritative transcript must contain only accepted participant messages, published Agent messages, published Agent work-trace updates, and explicitly participant-visible system notices, each with stable authorship, Session-local order, state, UTC time, and protected provenance sufficient to distinguish what was submitted, generated, explained, and shown.
- `REQ-SESS-43` — Lifecycle and transcript history must be append-only or equivalently tamper-evident. Corrections must preserve the original record, and presentation projections must not silently omit an accepted or published item from authorized historical inspection.
- `REQ-SESS-44` — Instruction acknowledgment, pause/resume, administrative termination, terminal commit, terminal-cutoff creation, Attempt terminal mapping, and any other Session mutation classified by approved policy as fairness- or outcome-relevant must produce mutation-coupled `required_durable` audit under `PROP-1`; the protected transition must fail when its required event cannot be durably accepted.
- `REQ-SESS-45` — Routine message reads and connection events may use the bufferable audit class only when approved policy permits it and bounded durable buffering accepts the event; message/transcript content must not be duplicated into audit events.
- `REQ-SESS-46` — Authorized structured history must make it possible to determine which instructions were acknowledged, when execution began, which exact messages were accepted and published in what order, which retries or partial failures occurred, how time and pause were accounted for, who ended the Session and why, and what terminal cutoff was supplied downstream.
- `REQ-SESS-47` — Participant-visible Session history, authorized administrator monitoring, assigned-review access, exports, caches, events, real-time channels, logs, indexes, background work, and transcript projections must preserve organization, activity, participant, Attempt, and Session isolation and current visibility policy.
- `REQ-SESS-48` — In the MVP, transcript content and Session working context must not be reused for Dynamic memory, cross-participant learning, calibration, unrelated activities, or harness self-modification. Participant or model content cannot enable such reuse.
- `REQ-SESS-49` — Session, acknowledgment, message, turn, transcript, timer, pause, terminal, connection, audit, and protected payload records must follow applicable approved retention, deletion, legal-hold, consent, export, and evidence-preservation policy; this feature defines no independent duration and must report lawful unavailability honestly.
- `REQ-SESS-50` — Participant, reviewer, administrator, and audit views must reveal only the content and metadata necessary for the actor's current authorized task; hidden prompts, chain-of-thought, credentials, provider internals, other participants, and unreleased evaluation/result content must remain unavailable.

## Data, evidence, and audit

### Logical records

Architecture may choose physical storage only if it preserves ownership, authorization, ordering, immutability, idempotency, timer, audit, and terminal-cutoff semantics.

| Record | Purpose | Minimum content |
| --- | --- | --- |
| Instruction/notice version | Identify participant-visible pre-start content | Stable version, organization/activity/cohort scope, content or protected reference, effective status, required acknowledgment type, provenance |
| Acknowledgment | Preserve a deliberate participant decision | Acknowledgment ID, participant/enrollment/Attempt scope, exact notice version, outcome, actor, UTC time/order, idempotency/correlation, withdrawal/supersession reference when policy permits |
| Session | Own live and terminal execution state | Session ID, organization/activity/cohort/enrollment/participant/Attempt references, configuration/manifest/Submission-binding references, state, state revision, start/terminal order and times |
| Session lifecycle event | Preserve every transition without overwriting | Event ID/order, prior/new state, actor/service, reason category, protected note reference when permitted, timing effect, correlation, UTC time |
| Message | Preserve one authoritative visible transcript item | Message ID, Session/turn, author type and actor reference where applicable, immutable content/protected reference, state, Session-local sequence, accepted/published time, idempotency/correlation |
| Agent work-trace update | Preserve exactly what participant-visible “thinking” showed without storing hidden reasoning | Update ID, Session/turn, status-or-summary type, exact displayed content/protected reference, policy/configuration/generation provenance, Session-local sequence, publication time |
| Turn | Connect participant input to Agent processing | Turn ID/order, triggering message or Agent-initiated type, frozen configuration reference, status, generation-attempt references, published-response reference, terminal outcome |
| Generation attempt | Preserve runtime provenance without conflating it with visible transcript | Attempt ID/order, turn/configuration/model references, start/end times, outcome, bounded failure/cancellation category, protected input/output references, manifest sequence |
| Timer state/interval | Reconstruct effective time | Start instant, duration/hard endpoint source, warning schedule/version, active/paused intervals, authoritative remaining time at transitions, timezone interpretation |
| Connection/reconciliation record | Explain recovery without making transport authoritative | Connection/session reference, actor/device-session pseudonymous reference when policy permits, last-seen and returned sequence, outcome, bounded reason, UTC times |
| Terminal record | Freeze the evaluation boundary | Terminal state/reason, actor/service, terminal command/order/time, transcript cutoff sequence, timing summary, Attempt mapping, configuration/manifest/Submission references, seal/handoff status |
| Audit event | Preserve security and governance history | Event/schema ID, actor/service, organization, action, protected resource references, outcome, bounded reason, UTC time/order, correlation, assignment/delegation reference |

### Transcript and evidence boundary

The transcript is an ordered Session record and a source that later Evidence may reference. This specification defines transcript integrity and terminal cutoff; [`evidence-evaluation.md`](evidence-evaluation.md) will define which transcript locations become Evidence and how evaluations cite them.

An authorized downstream consumer must be able to distinguish:

- accepted participant text from local drafts, rejected sends, and duplicated retries;
- published Agent text from hidden prompts, partial/failed candidates, and unpublished generations;
- published Agent work-status and concise reasoning summaries from raw chain-of-thought, hidden prompts, rubric internals, expected-answer criteria, and trusted system notices;
- exact bound Submission material the Agent could inspect from preserved material it did not inspect;
- messages included before the terminal cutoff from late or rejected commands;
- active, paused, disconnected, completing, completed, terminated, and aborted intervals;
- normal completion, time expiry, administrative termination, and platform abort;
- full transcript availability from honest degraded or unavailable-under-policy state.

### Required audit events

At minimum, record according to approved sensitivity and durability classification:

- Instruction/notice displayed when policy requires, acknowledgment accepted, declined, withdrawn, stale, or rejected.
- Session readiness consumed and execution entered `Active`.
- Participant message accepted, deduplicated, rejected by state/timing/policy, or involved in an idempotency conflict, without copying raw content into the audit payload.
- Agent generation started when required, failed, timed out, cancelled, retried, and published.
- Participant-visible Agent work-trace update published, suppressed by visibility policy, deduplicated, or involved in a publication failure, without copying hidden reasoning into audit.
- Connection authorized, revalidated, denied, expired, reconciled, or terminated when security- or recovery-relevant.
- Time warning emitted, expiry boundary reached, and timer discrepancy detected.
- Pause/resume requested, committed, rejected, or blocked by stale authorization.
- Participant/workflow completion requested; administrative termination requested; terminal transition committed, deduplicated, or failed.
- Attempt terminal mapping, transcript cutoff, manifest terminal append/seal outcome, and evidence/evaluation handoff readiness.
- Cross-organization, cross-participant, cross-Session, guessed-ID, forged-parent, stale-client, replay, rate-limit bypass, post-terminal write, or unauthorized control denial when security-relevant.
- Required audit acceptance failure and any protected Session operation blocked by it.

Audit and manifest records use protected message, content, and transcript references plus bounded metadata. They must not contain raw drafts, raw transcript content when a protected reference suffices, hidden prompts, chain-of-thought, credentials, tokens, unrestricted identifiers, or another participant's data.

## Quality requirements

### UX and accessibility

- The pre-start view must separate instructions, timing, Attempt consequence, completion behavior, data-use notice, and required acknowledgments. Required controls must be explicit, unchecked by default, labeled with their consequence, and operable before start.
- The live view must keep the primary task clear and expose Agent identity, Session status, remaining time, transcript, message composer, send state, and completion action without exposing hidden configuration or evaluation details.
- When enabled, participant-visible Agent work status and concise reasoning summaries must appear in a clearly labeled “Thinking” or equivalent region that remains distinguishable from the final Agent message and trusted system notices. The label must not claim that the content is complete internal reasoning.
- Work-status updates must use the frozen predefined labels. Reasoning summaries and any final “Why this response or question?” explanation must be concise, participant-oriented, and suppress hidden rubric, expected-answer, prompt, reviewer-only, security, and unrelated participant information.
- Loading, connecting, active, sending, waiting for Agent, retryable turn failure, reconnecting, offline, paused, expiring, completing, completed, terminated, aborted, access-expired, and unavailable states must be distinct in text and structure and must not rely on color, motion, sound, or spinner state alone.
- The composer must distinguish local draft, sending, accepted, failed-before-acceptance, and accepted-but-response-failed states. Recoverable local text should remain available when safe, but stale or unauthorized drafts must never be sent automatically.
- Transcript items must expose author, order, content, and status with semantic structure. Screen-reader reading order must match authoritative order; new messages and status updates must use non-disruptive announcements.
- Frequent work-trace updates must be rate-bounded and announced without overwhelming assistive-technology users. Replaced visual status text must remain available through the authoritative ordered history when policy exposes it.
- Remaining time must be available as text, update without excessive announcements, and announce each configured warning and terminal expiry. Color or animation alone must not communicate urgency.
- Pause, permission change, reconnect, and terminal states must move focus to the status or next safe action when appropriate while preserving readable transcript context permitted by policy.
- Completion and administrative termination must require deliberate confirmation that states the consequence. Destructive controls must not be adjacent to routine send controls without sufficient distinction.
- Keyboard focus must remain logical when messages arrive, must not be stolen by ordinary Agent output, and must return predictably after retry, reconnect, confirmation, or error dismissal.
- The experience must support keyboard-only use, screen readers, reduced motion, 400 percent zoom, reflow, and narrow viewports without hiding status, time, composer state, completion consequence, or recovery action.
- Raw model formatting must not break semantic structure, overflow the viewport, obscure controls, spoof system notices, or execute active content. Agent and participant content must remain visually and programmatically distinguishable from trusted system status.
- WCAG 2.2 AA is the proposed accessibility baseline pending an approved UI/UX specification.

### Performance and reliability

- Session commands, message admission, pause/resume, and terminal transitions must use bounded trusted ownership/state queries and idempotency where retries are possible.
- Under `PROP-4`, a participant message must receive an authoritative accepted, rejected, or reconciliation-required outcome within 2 seconds at the 95th percentile when authorization, Session state, and primary persistence are available inside the platform boundary; end-user network latency and Agent generation are excluded and measured separately.
- Under `PROP-4`, reconnect state synchronization after successful authentication and transport restoration must return authoritative lifecycle, timer, and transcript delta state within 2 seconds at the 95th percentile for a bounded MVP transcript; larger historical transcript loading may paginate after the current state and recent context are safe to display.
- Agent generation must use a positive bounded timeout and retry budget from the frozen policy. Provider latency, timeout, cancellation, publication, and participant-perceived wait must be measured separately.
- Message and lifecycle ordering must remain correct under duplicate delivery, concurrent tabs/devices, delayed events, process restart, model retry, terminal races, and projection lag.
- Acknowledgment, message, Agent publication, pause/resume, and terminal success must not be acknowledged before their required authoritative records are durably associated.
- Failure after participant-message acceptance must preserve that message and expose the turn's authoritative pending/retry/failed state; it must not require blind resubmission.
- Backpressure, positive message/turn limits, generation concurrency limits, and rate limits must prevent one Session from exhausting shared capacity or delaying unrelated Sessions without bound.
- Timer evaluation and terminal cutoff must continue from authoritative server state during client disconnection and process restart. Recovery must reconcile persisted intervals and must not trust client elapsed time.
- Manifest/audit/persistence failure must pause or fail the affected operation safely, retain observable bounded diagnostics, and support idempotent recovery without duplicating transcript content.

### Security and privacy

- Every message, transcript read, real-time connection, reconnect, pause/resume, completion, termination, timer query, terminal handoff, export, event, job, and projection must enforce server-side action and complete resource-chain authorization.
- Real-time connections must authorize establishment and each privileged subscription or command and must terminate or narrow stale access within the approved 60-second revocation-propagation target.
- Session identifiers, last-seen sequences, message IDs, idempotency keys, author fields, timer values, and state versions are untrusted input and must not permit cross-scope substitution, replay, ordering manipulation, or state rollback.
- Participant, Submission, knowledge, and model content must be treated as untrusted at model and rendering boundaries. Prompt injection cannot grant capabilities, alter policy, enable tools/memory, reveal hidden prompts, or authorize state transitions.
- Participant-visible work-trace generation must use a dedicated constrained output contract. It must not expose raw chain-of-thought, hidden prompts, rubric internals, expected answers, secrets, security controls, reviewer-only material, unrelated participant content, or content outside the current Session's authorized context.
- The MVP resolved configuration's empty tool set, disabled voice, disabled Dynamic memory, and no shared-Session behavior must be enforced throughout execution rather than treated as UI-only settings.
- Rendering must prevent script execution, unsafe URL behavior, markup spoofing of trusted system notices, data exfiltration, and leakage through previews or embedded content.
- Queries, caches, transcript projections, queues, events, provider requests, temporary data, analytics, logs, traces, and browser/test artifacts must preserve organization/activity/participant/Session scope.
- Raw drafts, unnecessary transcript content, hidden prompts, chain-of-thought, participant attributes, credentials, tokens, provider payloads, and unrestricted identifiers must not appear in logs, metrics, traces, error responses, analytics, or test artifacts.
- Message size, rate, pending-turn, reconnect, generation, and transcript-retrieval limits must be enforced from approved policy using non-disclosing errors and abuse signals.
- Negative tests must cover wrong organization/activity/cohort/participant/Session, guessed message/turn, forged author/order/state, stale authorization, replayed send, mismatched idempotency reuse, multiple-device race, cross-Session cache/event leakage, prompt injection, XSS/unsafe markup, prohibited work-trace disclosure, rate exhaustion, pause/terminal bypass, and post-terminal write.

## Acceptance criteria

### `AC-SESS-1` — Required instructions and acknowledgments gate start

- **Given** an authorized participant has an otherwise eligible Attempt
- **When** a required instruction or consent notice has not been affirmatively acknowledged in its current exact version, or the acknowledgment is declined, withdrawn, stale, or cross-scope
- **Then** the Session does not start and no entitlement is consumed
- **And** the participant sees the missing or declined requirement and safe next action without hidden policy details
- **And** a valid deliberate acknowledgment records the participant, exact version, outcome, scope, and UTC time.

### `AC-SESS-2` — Execution begins only from committed readiness

- **Given** acknowledgments and all ADR-005 start preconditions are satisfied
- **When** the atomic start boundary commits
- **Then** one Session enters `Active` bound to the exact Attempt, participant, configuration, manifest, and Submission version set
- **And** the authoritative timer begins at the committed start instant
- **And** no participant interaction or model execution occurs before the commit.

### `AC-SESS-3` — Active participant sends one ordered message

- **Given** the participant is authorized and the Session is `Active` before its terminal cutoff
- **When** the participant sends text within frozen policy limits
- **Then** one immutable participant message is committed with stable identity, authorship, Session-local order, accepted UTC time, and turn linkage
- **And** the Agent processes only the committed authoritative content
- **And** another Session cannot observe the message or its metadata.

### `AC-SESS-4` — Duplicate and conflicting sends are safe

- **Given** equivalent send commands are retried or delivered concurrently
- **When** the service processes their idempotency key and trusted content digest
- **Then** equivalent commands return one authoritative message and turn
- **And** mismatched reuse reports a conflict without changing the original
- **And** no duplicate Agent response is published accidentally.

### `AC-SESS-5` — Agent response becomes visible only after publication

- **Given** an accepted participant message has an eligible turn
- **When** Agent generation succeeds under the frozen configuration and text workflow
- **Then** one published Agent message is durably committed with exact visible content, stable order, turn/model provenance, and UTC publication time before it is authoritative in the transcript
- **And** participant content does not enable tools, memory writes, external retrieval, or a state transition
- **And** hidden prompts and unpublished candidates remain absent from the participant transcript.

### `AC-SESS-6` — Turn failure preserves accepted input and recovers safely

- **Given** a participant message was accepted and Agent generation, validation, manifest append, or publication later fails
- **When** the turn reaches its bounded failure or timeout
- **Then** the participant message remains immutable and visible
- **And** the turn shows a retryable, paused, or terminal outcome without requiring blind resubmission
- **And** a retry uses the same turn, preserves prior attempt provenance, and publishes at most one response for the response slot.

### `AC-SESS-7` — Partial Agent visibility is recorded honestly

- **Given** Agent content is generated or streamed but publication does not complete normally
- **When** no content reached the participant
- **Then** it is not represented as a published transcript message
- **But given** any partial content did reach the participant
- **Then** the exact exposed portion and visibility outcome are preserved distinctly and are not silently discarded or expanded.

### `AC-SESS-8` — Concurrent Sessions remain isolated

- **Given** multiple participants use Sessions under the same activity, cohort, Agent, or Harness
- **When** they send, reconnect, load transcript deltas, or receive Agent output concurrently
- **Then** every message, timer, turn, output, working context, cache entry, event, and projection remains bound to the correct participant and Session
- **And** no row, count, sequence gap, error, or content reveals another participant.

### `AC-SESS-9` — Reconnect restores authoritative state

- **Given** a participant loses connectivity while the Session is non-terminal
- **When** transport returns and the participant reauthenticates and remains authorized
- **Then** the same Session returns its authoritative state, timer facts, transcript delta after the trusted last-seen sequence, and pending turn outcome
- **And** local optimistic state is reconciled by idempotency key
- **And** the Attempt, start time, and accepted messages are not duplicated or reset.

### `AC-SESS-10` — Disconnection does not silently change timing

- **Given** the client closes, loses focus, changes devices, or remains disconnected
- **When** authoritative time advances
- **Then** the Session follows the approved frozen timing and pause policy rather than the client connection state
- **And** reconnect displays the recalculated remaining time and any warnings or terminal transition that occurred
- **And** the client cannot restore a more favorable timer.

### `AC-SESS-11` — Timer warnings and expiry use authoritative boundaries

- **Given** an authorized assessment configuration selects a valid warning schedule within its upper-scope bounds
- **And** the schedule is frozen with the timed Session workflow
- **When** it crosses each threshold and the terminal boundary
- **Then** only the configured warnings are recorded and presented at most once in accessible text
- **And** a zero, negative, out-of-duration, unresolvable, or duplicate-effective threshold is rejected before Session start
- **And** another valid Activity or cohort may freeze different warning thresholds without changing this Session
- **And** the platform does not add universal fixed thresholds that were not configured
- **And** missed or delayed warning delivery does not extend the Session
- **And** a command ordered after the terminal cutoff is rejected without transcript mutation.

### `AC-SESS-12` — Authorized pause and resume preserve fairness history

- **Given** an Activity administrator has current delegated Session-control authority
- **When** the administrator pauses and later resumes a non-terminal Session
- **Then** each transition is commit-time authorized, deliberately confirmed where applicable, and durably recorded with actor, reason, UTC boundaries, timer effect, and in-flight-turn disposition
- **And** no new participant message or Agent generation begins while paused
- **And** resume preserves the configuration, transcript, Attempt consumption, and applicable hard deadline.

### `AC-SESS-13` — Unauthorized or stale control is denied

- **Given** an administrator lacks Session-control scope, has stale/revoked authority, targets another organization/activity, or acts after terminal cutoff
- **When** pause, resume, or terminate is requested
- **Then** no lifecycle, timer, transcript, or Attempt state changes
- **And** the response is non-disclosing
- **And** the denial is audited when security-relevant.

### `AC-SESS-14` — Authorization loss stops live participation

- **Given** a participant's enrollment is suspended, revoked, expired, or otherwise no longer authorizes the live Session
- **When** a new command or long-lived connection is revalidated
- **Then** new participant and model operations are blocked immediately at their commit boundaries
- **And** stale real-time access terminates or narrows within 60 seconds
- **And** recoverable suspension leaves the Session paused while confirmed non-recoverable loss follows the authorized terminal policy without deleting history.

### `AC-SESS-15` — Participant completes deliberately and idempotently

- **Given** the Session is `Active` or in another frozen workflow state that permits participant completion
- **When** the authorized participant confirms completion
- **Then** one terminal command enters `Completing`, establishes the authoritative command fence, and blocks new sends
- **And** the immutable transcript cutoff is finalized only after already accepted pending work reaches its governed completion or cancellation outcome
- **And** retries return the same completion outcome
- **And** the participant receives completion confirmation without an implied score, Evaluation, Result, or release.

### `AC-SESS-16` — Time expiry completes at one ordered cutoff

- **Given** the authoritative Session time reaches its terminal boundary while a send or Agent turn may be concurrent
- **When** commands are ordered at the boundary
- **Then** only content committed before the cutoff is eligible for the terminal transcript
- **And** the frozen pending-turn policy records whether an already accepted turn completed or was cancelled
- **And** the Session follows the approved time-expiry terminal mapping once.

### `AC-SESS-17` — Administrative termination preserves the consumed Attempt

- **Given** an authorized Activity administrator deliberately terminates a non-terminal Session with a bounded reason
- **When** the terminal transition commits
- **Then** the Session becomes `Terminated`, no later live command is accepted, and the prior transcript/configuration/Submission binding remain unchanged
- **And** the Attempt remains consumed and maps according to the approved terminal policy
- **And** another try requires remaining allowance or an authorized retry entitlement.

### `AC-SESS-18` — Unrecoverable failure aborts honestly

- **Given** execution cannot safely preserve authorization, ordering, timer, persistence, manifest, or audit integrity after start
- **When** bounded recovery is exhausted
- **Then** the Session transitions through `Completing` to `Aborted` with a stable non-sensitive reason
- **And** the Attempt, frozen configuration, manifest, transcript cutoff, and accepted messages remain inspectable
- **And** no false completion or automatic entitlement restoration occurs.

### `AC-SESS-19` — Terminal commit gates evaluation handoff

- **Given** a Session is completing
- **When** terminal state, reason, cutoff, timing summary, Attempt mapping, required audit, and manifest terminal/seal state become authoritative
- **Then** the Session exposes one immutable downstream handoff
- **And** evidence/evaluation processing cannot consume later live content or a mutable transcript alias
- **And** failure leaves an honest completing/recovery state rather than a false terminal success.

### `AC-SESS-20` — Post-terminal commands are side-effect free

- **Given** a Session is `Completed`, `Terminated`, or `Aborted`
- **When** a stale client, retry, model callback, event, administrator, or participant attempts a message, generation, pause, resume, completion, or termination command
- **Then** the existing terminal state and cutoff are returned or a non-disclosing denial is issued
- **And** no transcript, timer, Attempt, manifest, or evidence state changes.

### `AC-SESS-21` — Transcript history is exact and immutable

- **Given** accepted participant messages, published Agent messages, published Agent work-trace updates, failed generations, retries, and lifecycle notices exist
- **When** an authorized actor inspects structured history
- **Then** visible transcript content is distinguishable from drafts and unpublished generations
- **And** final Agent messages, participant-visible work status or reasoning summaries, hidden processing, and trusted system notices are distinguishable
- **And** authorship, Session order, UTC times, turn outcomes, exact displayed content, and terminal inclusion are reconstructable
- **And** corrections or lawful unavailability preserve the original reference and honest status rather than silently replacing content.

### `AC-SESS-22` — Later configuration or Submission changes do not alter the Session

- **Given** a Session has started with frozen configuration and exact Submission bindings
- **When** an Agent, Harness, workflow, model alias, policy, knowledge source, or Submission later changes
- **Then** the Session continues with its original permitted versions and exact bindings
- **And** a later accepted Submission version is excluded unless the frozen policy permits it and a separate exact ordered in-Session binding commits
- **And** no mutable `current` or `latest` alias changes historical meaning.

### `AC-SESS-23` — Disabled capabilities and learning remain disabled

- **Given** the MVP Session enables text and disables voice, tools, Dynamic memory, and shared-Session behavior
- **When** participant content, model output, a retry, or a service requests one of those disabled capabilities or attempts learning reuse
- **Then** the request is blocked
- **And** no transcript or working context is written to cross-participant memory, calibration, or harness self-modification
- **And** the denial is observable without raw participant content.

### `AC-SESS-24` — Active Session interaction is accessible and responsive

- **Given** the participant uses keyboard navigation, a screen reader, reduced motion, 400 percent zoom, or a narrow viewport
- **When** the Session receives messages, participant-visible work-trace updates, warnings, pending states, errors, or status changes
- **Then** Agent/participant authorship, authoritative order, work status, concise reasoning summary, remaining time, composer state, completion action, and recovery remain perceivable and operable
- **And** ordinary incoming messages do not steal focus
- **And** frequent work-trace changes are rate-bounded and do not overwhelm announcements
- **And** the experience does not rely on color, sound, hover, pointer, drag, or motion alone.

### `AC-SESS-25` — Failure, pause, and terminal states are accessible

- **Given** the Session is reconnecting, offline, response-failed, paused, expiring, completing, completed, terminated, aborted, permission-changed, or unavailable
- **When** the state is presented
- **Then** the status, consequence, timer behavior, preserved content, and next safe action are stated in text and programmatically available
- **And** focus moves to the status or action when needed without hiding permitted transcript context
- **And** protected technical or cross-scope details are absent.

### `AC-SESS-26` — Untrusted content cannot spoof or execute trusted UI

- **Given** participant, Agent-message, or Agent work-trace content includes markup, code, links, instructions, or text resembling a system notice
- **When** it is rendered or processed
- **Then** active content does not execute, unsafe retrieval does not occur, and trusted controls/status remain distinguishable
- **And** the content cannot authorize a tool, memory write, lifecycle transition, or data access
- **And** layout, focus, and narrow-screen controls remain usable.

### `AC-SESS-27` — Approved platform service objectives are measurable

- **Given** authorization, Session state, and primary persistence are available inside the platform boundary under representative load
- **When** message admission and reconnect synchronization are measured under `PROP-4`
- **Then** each returns its authoritative outcome within 2 seconds at the 95th percentile for the stated bounded workload
- **And** end-user network and Agent/provider generation latency are measured separately
- **And** misses are observable without raw transcript content.

### `AC-SESS-28` — Required durable audit gates fairness-relevant transitions

- **Given** acknowledgment, pause/resume, administrative termination, or terminal commit is classified as `required_durable` under `PROP-1`
- **When** its audit event cannot be durably accepted
- **Then** the protected transition does not report or expose false success
- **And** the Session remains in its prior or honest recoverable state
- **And** an operational signal is emitted without transcript content or secrets.

### `AC-SESS-29` — Negative lifecycle coverage gates release

- **Given** the text Session lifecycle is considered for release
- **When** its verification suite runs
- **Then** tests cover wrong organization/activity/cohort/participant/Session, forged parent/author/order/state/timer, guessed message/turn, stale authorization, replay and mismatched idempotency, duplicate/concurrent sends, cross-Session cache/event leakage, provider and manifest failure, reconnect, multiple devices, exact timer boundaries, configurable warning schedules, pause/terminal races, audit failure, prompt injection, unsafe markup, prohibited work-trace disclosure, rate exhaustion, and post-terminal callbacks
- **And** the feature is not release-ready while an applicable negative case is missing or failing.

### `AC-SESS-30` — Historical access remains scoped after completion

- **Given** a terminal transcript remains available
- **When** the participant, assigned reviewer, administrator, service, or auditor requests it
- **Then** access follows the actor's current relationship, delegated capability, workflow/visibility state, and lifecycle policy
- **And** completion or organization membership alone grants no access
- **And** participant access does not expose hidden prompts, evaluation internals, another participant, or an unreleased Result.

### `AC-SESS-31` — Participant-visible thinking is safe and reconstructable

- **Given** the frozen work-trace policy enables participant-visible Agent work status and concise reasoning summaries
- **When** a turn is processed
- **Then** every displayed update uses a permitted predefined status or bounded participant-facing summary and is committed with exact displayed content, order, time, Session/turn scope, and provenance before display
- **And** the work-trace region remains distinguishable from the final Agent message and trusted system notices
- **And** raw chain-of-thought, hidden prompts, rubric internals, expected-answer criteria, secrets, security controls, reviewer-only content, and unrelated participant data are absent
- **And** a prohibited or unrecordable update is suppressed in favor of a permitted generic status or safe failure without leaking its content
- **And** the final Agent message may include the configured concise “Why this response or question?” explanation without claiming to reproduce complete internal reasoning.

## Edge and failure cases

| Case | Required outcome |
| --- | --- |
| Required notice changes after acknowledgment but before start | Treat the old acknowledgment as stale for the new exact version; block start without consumption and request a new deliberate decision |
| Participant opens two tabs before start | Reconcile to one Attempt/Session start; only the winning committed readiness state may execute |
| Two tabs send the same command | Deduplicate by trusted idempotency scope and return one message/turn |
| Two tabs send different messages concurrently | Apply the frozen concurrent-pending-turn policy and authoritative ordering; never infer order from client clocks |
| Participant loses connection after send but before response | Reconcile whether the message committed; preserve accepted input and return the authoritative turn state |
| Provider returns after timeout or terminal cutoff | Record the late provider outcome as permitted provenance; do not publish it into the terminal transcript |
| Agent output is partially displayed before disconnect | Preserve exactly the exposed portion and visibility outcome; do not replace it silently with a longer candidate |
| Session pauses during an in-flight turn | Apply the frozen cancellation/commit boundary and record the outcome; do not leave ambiguous visible content |
| Client countdown reaches zero early or late | Use authoritative service time and state; reconcile the UI without changing the terminal boundary |
| Warning cannot be delivered | Preserve the warning/threshold outcome when required; do not extend time or claim delivery |
| Participant is disconnected until after expiry | Complete at the authoritative boundary and show the terminal state on authorized reconnect |
| Enrollment is suspended during the Session | Block new commands and pause/keep paused within the revocation target; preserve transcript and timing history |
| Enrollment is revoked during the Session | Block access and terminate under the approved policy; do not delete Session history or restore entitlement |
| Administrator pause and participant completion race | Order commands at the authoritative commit boundary; exactly one valid transition wins and the other returns current state |
| Message commit and time expiry race | Include the message only if its authoritative order is before the terminal cutoff |
| Required audit is unavailable during completion | Remain honestly `Completing` or in the prior safe state; do not expose a false terminal handoff |
| Manifest terminal append/seal fails | Block evaluation readiness, preserve terminal intent and transcript cutoff candidates, and recover idempotently without rewriting history |
| Later Submission is accepted | Keep the start binding unchanged; use it only if the frozen rule permits and a new exact ordered binding commits |
| Terminal transcript content is lawfully deleted | Preserve minimized metadata and an honest unavailable/degraded status under policy; do not substitute later content |
| Model output requests a tool or reveals hidden instructions | Treat output as untrusted; block the disabled action and avoid exposing protected instructions |
| Participant-visible work-trace summary contains hidden rubric, expected-answer, prompt, security, or reviewer-only content | Suppress the update, record a bounded policy outcome, and show only a permitted generic status or safe failure |

## Dependencies and rollout

### Dependencies

- Approved authentication, authorization, resource-isolation, real-time revocation, scoped-query, commit-time reauthorization, and audit-durability contracts from [`auth-resource-isolation.md`](auth-resource-isolation.md), ADR-002, and ADR-003.
- Activated cohort baseline with exact text workflow/policy, adaptive-follow-up policy, timing rules, warning schedule, pause/completion rules, and participant-visible instruction/notice references from [`assessment-setup.md`](assessment-setup.md).
- Trusted enrollment, Attempt, permitted timing, accommodation, exact Submission versions, and entitlement state from [`submission-attempts.md`](submission-attempts.md).
- Atomic committed Session readiness, resolved configuration, initial manifest, and exact Submission binding from [`resolved-session-configuration.md`](resolved-session-configuration.md) and ADR-005.
- Append-only manifest records and terminal seal behavior from ADR-001.
- A model-provider adapter capable of bounded timeout, cancellation outcome, stable correlation, and the version identity required by the resolved manifest.
- Protected transcript/payload storage and rendering capable of exact Session scoping, immutable accepted/published records, ordered retrieval, safe markup, and lifecycle-policy enforcement.
- [`evidence-evaluation.md`](evidence-evaluation.md) consumer contract for the terminal transcript cutoff and protected evidence references.
- [`review-result-release.md`](review-result-release.md) for assigned review access, review decisions, participant-visible Result, and Release.
- Architecture decision or design covering authoritative Session/message/work-trace ordering, timer interval accounting, terminal consistency, publication, constrained work-trace generation, and recovery under the approved defaults.
- UI/UX interaction specification covering the Session state model, participant-visible work-trace region, content, focus, configurable warning behavior, responsive layout, and accessibility before UI implementation is considered complete.

### Rollout

- The text Session lifecycle is a mandatory MVP workflow boundary, not an optional customer-facing feature flag.
- Do not enable live Sessions until ADR-005 start readiness, current authorization, exact Submission binding, canonical state, ordered transcript, timer, idempotency, required audit, and manifest append work together.
- Enable one-participant text interaction only. Keep voice, tools, Dynamic memory, shared Sessions, and alternate activity deployment forms explicitly disabled.
- Do not enable a workflow unless required message/turn/generation limits, participant-visible work-trace policy, configurable timer/warning behavior, pause/completion policy, instruction/notice versions, and terminal mapping are complete and validated.
- Roll out with seeded non-sensitive test cohorts and failure injection for duplicate/concurrent sends, lost responses, provider timeout/late callback, persistence and audit failure, reconnect, revocation, pause, expiry, terminal races, and manifest sealing.
- Quarantine migrated or prototype Sessions whose participant/Attempt/configuration/manifest/Submission ownership, transcript order, timer state, or terminal cutoff cannot be verified; do not use them for Evaluation.
- UI rollout requires automated component checks plus Playwright accessibility snapshots and desktop/narrow screenshots for every applicable state in `AC-SESS-24`–`AC-SESS-26` and `AC-SESS-31`.

### Observability

Track at minimum:

- Sessions started, active, paused, resumed, completed, terminated, aborted, and stuck in completing by bounded reason.
- Instruction/acknowledgment blocked starts by version-safe category.
- Participant messages accepted, rejected, deduplicated, conflicted, and blocked after pause/terminal state.
- Agent generations started, timed out, cancelled, retried, late, failed, and published; exposed-partial-answer cases should have a release target of zero when non-streaming is selected.
- Participant-visible work-trace updates proposed, published, deduplicated, suppressed by bounded policy category, failed before publication, and rejected for prohibited-content risk.
- Message-admission latency, provider latency, publication latency, and participant-perceived turn latency separately.
- Reconnects, reconciliation conflicts, stale clients, transcript-delta size, and revocation-propagation lag.
- Timer discrepancies, duplicate/missed warnings, pause intervals, expiry transitions, and post-cutoff command attempts.
- Required-audit acceptance failures, manifest append/seal failures, terminal-transition recovery, and evidence-handoff delay.
- Cross-organization, cross-participant, cross-Session, guessed-ID, forged-order, replay, unsafe-markup, prompt-injection, and rate-limit denials.
- Sessions with zero or multiple authoritative terminal records, duplicate Session-local message sequences, transcript items after cutoff, or terminal Attempt-mapping mismatch; the release target is zero.

Metrics, logs, traces, and alerts must use bounded labels and protected references and must not contain raw drafts, transcript content, prompts, model outputs, participant attributes, credentials, tokens, unrestricted identifiers, or provider payloads.

## Open questions

None. `Q-1`–`Q-7` were resolved on 2026-08-06 as recorded below.

## Approved decision disposition

| Prior IDs | Approved disposition | Rationale / consequence |
| --- | --- | --- |
| `Q-1`, `PROP-1` | Classify acknowledgment, pause/resume, administrative termination, terminal commit/cutoff, and Attempt terminal mapping as mutation-coupled `required_durable` audit events. Routine transcript reads and connection telemetry may use only an approved bufferable class. | These mutations affect consent evidence, fairness, time, or the evaluation boundary and must not diverge from audit history. |
| `Q-2`, `PROP-2` | Authoritative pause intervals stop per-Attempt active-duration accounting but do not extend an absolute activity/enrollment deadline without a separately authorized accommodation or fairness exception. | Participants are not charged for an approved interruption, while cohort deadlines and upper-scope limits remain enforceable. |
| `Q-3`, `PROP-3` | Browser closure, focus loss, device change, or transport disconnection does not pause Session timing. Only an authorized administrative or system-safety transition may pause. | Participant-controlled or unreliable connectivity cannot become a timing or fairness bypass. |
| `PROP-4` | Set initial platform objectives of no more than 2 seconds at the 95th percentile for authoritative message-admission outcome and bounded reconnect synchronization, excluding end-user network latency and Agent/provider generation. | The platform boundary receives a measurable target without conflating model latency or participant network conditions. |
| `Q-5`, `PROP-5` | Map participant/workflow completion and time expiry to Session/Attempt `Completed`; administrative early termination to Session `Terminated` and Attempt `Aborted`; unrecoverable execution/integrity failure to Session/Attempt `Aborted`. | Normal completion, operator intervention, and platform failure remain distinguishable while preserving the approved Attempt terminal model. |
| `Q-4`, `PROP-6` | Make the versioned warning schedule configurable within upper-scope policy bounds and freeze the selected non-empty schedule for each timed Session. Emit only configured thresholds once each; do not impose universal fixed thresholds. | Organizations and Activities can choose appropriate warnings without changing an in-progress Session or relying on hard-coded product timing. |
| `Q-6`, `PROP-7` | Support participant-visible Agent work status and concise authored reasoning summaries under a frozen policy, with exact displayed-content recording. Publish final Agent answers as complete durable messages by default; raw chain-of-thought and protected internals are prohibited. | Participants receive transparent progress and a useful explanation without exposing hidden prompts, rubrics, expected answers, secrets, or unreliable internal reasoning. |
| `Q-7`, `PROP-8` | Permit read-only participant access to the participant-visible terminal transcript before Result release only while current authorization, relationship, Session visibility, and lifecycle policy permit it; keep hidden and outcome content release-gated. | Participants may revisit content they saw without gaining access to Evaluation, review, hidden instructions, or unreleased Results. |

## Approved defaults

These defaults are approved with this specification and govern MVP text Session lifecycle behavior. Stable `PROP-*` IDs are retained for traceability.

- `PROP-1` — Require mutation-coupled durable audit for acknowledgment, pause/resume, administrative termination, terminal commit/cutoff, and Attempt terminal mapping.
- `PROP-2` — Stop active-duration accounting during an authoritative pause without extending an absolute deadline unless separately authorized.
- `PROP-3` — Do not pause timing merely because the client disconnects, closes, loses focus, or changes devices.
- `PROP-4` — Apply the 2-second p95 platform objectives for message-admission outcome and bounded reconnect synchronization under the stated exclusions.
- `PROP-5` — Apply the approved Session-to-Attempt terminal mapping for completion, termination, and abort.
- `PROP-6` — Use only the configurable, versioned, non-empty warning schedule frozen for the timed Session; impose no universal warning thresholds.
- `PROP-7` — Display frozen-policy Agent work status and concise authored reasoning summaries, record exactly what was shown, publish final answers as complete durable messages by default, and never expose raw chain-of-thought or protected internals.
- `PROP-8` — Permit scoped read-only access to the participant-visible terminal transcript before Result release while keeping hidden and outcome content gated.

## Traceability

| Requirement/AC | Implementation | Automated verification | Playwright/manual evidence | Status |
| --- | --- | --- | --- | --- |
| `REQ-SESS-1`–`REQ-SESS-7`, `AC-SESS-1`, `AC-SESS-2` | Instruction/notice versioning, acknowledgment command, ADR-005 readiness consumer, canonical Session state — architecture and implementation TBD | Current/stale/declined acknowledgment; cross-scope; pre-commit failure; duplicate start tests | Pre-start instructions, required acknowledgment, blocked, starting, and active states | Gap |
| `REQ-SESS-8`–`REQ-SESS-19`, `REQ-SESS-51`–`REQ-SESS-54`, `AC-SESS-3`–`AC-SESS-8`, `AC-SESS-31`, `PROP-7` | Ordered message/turn/work-trace model, generation adapter, constrained explanation contract, publication boundary, safe renderer — architecture and implementation TBD | Idempotency, concurrency, ordering, retry, partial visibility, work-trace policy/leakage, cross-Session, prompt-injection, unsafe-markup tests | Draft/sending/accepted/thinking/waiting/retry/published states at desktop and narrow widths | Gap |
| `REQ-SESS-20`–`REQ-SESS-30`, `AC-SESS-9`–`AC-SESS-14`, `PROP-2`, `PROP-3`, `PROP-6` | Authoritative timer, pause intervals, warning scheduler, reconnect/revocation protocol — architecture and implementation TBD | Exact boundary, pause accounting, disconnect, reconnect, stale client, revocation, multiple-device tests | Timer/warnings, reconnecting, offline, paused, resumed, permission-changed states | Gap |
| `REQ-SESS-31`–`REQ-SESS-41`, `AC-SESS-15`–`AC-SESS-20`, `PROP-5` | Terminal command/order, transcript cutoff, Attempt mapping, manifest seal and handoff boundary — architecture and implementation TBD | Completion/expiry/termination/abort, message-terminal race, audit/seal failure, post-terminal callback tests | Confirmation, completing, completed, terminated, aborted, recovery states | Gap |
| `REQ-SESS-42`–`REQ-SESS-50`, `AC-SESS-21`–`AC-SESS-23`, `AC-SESS-28`–`AC-SESS-30`, `PROP-1`, `PROP-8` | Transcript history, authorization/audit adapters, lifecycle enforcement, scoped historical views — architecture and implementation TBD | Immutability, correction, audit durability/redaction, retention/unavailability, access-scope, disabled-learning tests | Participant transcript, assigned review, administrator monitoring, denied/unavailable states | Gap |
| UX/accessibility requirements, `AC-SESS-24`–`AC-SESS-26`, `AC-SESS-31` | Session interaction specification, participant-visible work-trace region, and accessible components — UI/UX spec TBD | Keyboard, focus, live-region rate, semantic-order, zoom/reflow, reduced-motion, work-trace labeling, safe-rendering component tests | Playwright accessibility snapshots and desktop/narrow screenshots for every applicable state | Gap |
| Performance/reliability requirements, `AC-SESS-6`, `AC-SESS-9`–`AC-SESS-11`, `AC-SESS-27`, `PROP-4` | Backpressure, SLO telemetry, provider timeout/cancellation, state reconciliation — architecture TBD | Load, bounded transcript, process restart, delayed event, timeout, projection-lag, recovery tests | Pending, delayed, retry, reconnect, timer reconciliation, degraded states | Gap |
| Security/privacy requirements, `AC-SESS-8`, `AC-SESS-13`, `AC-SESS-14`, `AC-SESS-23`, `AC-SESS-26`, `AC-SESS-29` | ADR-002 enforcement adapters, scoped transport/cache/events, content security controls — implementation TBD | Full negative authorization/isolation, replay, injection, XSS, rate/resource exhaustion, data-leakage suite | Non-disclosing denial, safe content, access-expired, post-terminal states | Gap |
