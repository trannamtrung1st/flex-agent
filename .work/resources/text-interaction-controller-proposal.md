# Proposed text Interaction Controller for MVP

## Status and authority

- **Status:** Proposed, non-authoritative working input
- **Created:** 2026-08-15
- **Source:** Product discussion about Participant interruption and proactive
  interval interaction during text Sessions
- **Intended consumer:**
  [`.work/active/text-interaction-controller-contract.md`](../active/text-interaction-controller-contract.md)
- **Dependency:** The active
  [`structured-agent-runtime-sync`](../active/structured-agent-runtime-sync.md)
  task must complete before this proposal is promoted into authoritative
  product, requirements, UI/UX, or architecture documents.

This document preserves a candidate direction. It does not change the approved
MVP, enable an Interaction Controller, or alter the acceptance target of the
active structured-runtime work.

## Proposed outcome

Promote a narrow **text Interaction Controller** into the assessment MVP so an
active Session can react coherently to:

1. a Participant message accepted while an Agent response is still streaming;
   and
2. the existing bounded Session timer signal used to consider proactive Agent
   outreach.

The controller detects and normalizes permitted interaction signals and owns
interaction mechanics. The resolved Agent makes the semantic recommendation.
The Session runtime remains authoritative for trusted-trigger and Invocation
admission, authorization, ordering, validation, cancellation, message
publication, timing, and every resulting effect.

The target is event-aware text interaction, not voice behavior and not a
general multi-agent system.

## Problem

The approved text Session can stream an Agent response and can use one bounded
timer lane, but it does not yet define one coherent arbitration contract for a
Participant message or timer signal that arrives while an Agent response is in
progress.

Without that contract, implementations may diverge on whether they reject,
queue, merge, or use the new input to cancel an active response. They may also
allow a timer-triggered proactive response to compete with an existing
Participant-facing response. Those differences affect transcript honesty,
assessment fairness, provider cost, latency, recovery, and audit
reconstructability.

## Proposed conceptual model

### One Session, one Agent, bounded overlapping Invocations

- There remains exactly one isolated Session and one frozen resolved Agent
  revision for the Participant interaction.
- When an active response requires semantic arbitration, a Session may have one
  content-producing **response Invocation** and one purpose-bound
  **interaction-decision Invocation** overlapping briefly.
- These are not separate Sessions, Agent identities, transcript branches, or
  memory contexts.
- An Agent Invocation remains one bounded semantic decision opportunity. A
  provider call is an execution detail below that boundary.
- The interaction-decision Invocation receives only authorized, minimized,
  committed facts. It never observes hidden chain-of-thought or unpublished
  provider output.

### Two logical lanes, one visible publisher

| Lane | Responsibility | Allowed Participant-visible effect |
| --- | --- | --- |
| Response lane | Produce one ordered Agent message for an admitted Turn | At most one active streaming Agent message |
| Interaction-decision lane | Recommend how the runtime should react to an admitted interaction signal | No concurrent message publication; only validated control recommendations |

The lanes are logical runtime responsibilities, not separate services or a
requirement for separate model deployments.

### Event-driven, not continuously thinking

The controller should evaluate only permitted signals. It must not keep a model
request permanently open or inspect every generated token as a new signal. The
runtime applies frozen admission policy before it creates any Agent Invocation.
Candidate MVP signal sources are:

- one accepted Participant interaction input while an Agent response is
  active; and
- one due event from the already-approved single Session timer lane.

Equivalent retries are deduplicated. Distinct Participant messages are accepted
individually under the frozen concurrent-pending-turn policy or rejected
honestly; they are never merged, content-coalesced, or silently dropped. Timer
and eligible operational signals may be coalesced under frozen positive bounds.
Raw browser activity, typing, focus, presence, provider callbacks, worker polls,
and telemetry do not automatically become Agent Invocations.

## Proposed decision vocabulary

The exact schema remains an architecture decision. The minimum semantic
vocabulary should distinguish:

| Recommendation | Meaning if independently validated by the runtime |
| --- | --- |
| `continue_current_response` | Keep the active Agent stream; the accepted Participant message keeps its one stable pending Turn and response slot, which become eligible after the active response reaches its governed outcome |
| `interrupt_current_response` | Stop future publication for the runtime-bound active response at an authoritative cutoff, preserve its exact visible prefix, then make the accepted Participant Turn's existing response slot eligible |
| `no_action` | Resolve the control-purpose Invocation with no control effect; do not terminalize, replace, or relink the accepted Participant Turn, and apply its frozen pending-turn fallback |
| `next_timer_request` | Recommend the next bounded relative timer delay through the existing one-lane replacement contract |

The runtime may reject any stale, unauthorized, prohibited, over-budget, or
invalid recommendation without fabricating another recommendation.

For the first version, a control-purpose interaction-decision Invocation should
not directly publish a Participant-visible Agent message. It may only affect
eligibility of the already-created pending response slot after runtime
validation. This adds a bounded decision step during contention but prevents
two simultaneous Agent speakers.

This restriction does not replace the approved idle timer path: when the
response lane is available, a trusted timer-triggered Agent Invocation may use
the existing Agent-initiated Turn and directly recommend one permitted message,
intentional no-action, and an optional next-timer request.

## Candidate runtime flows

### Participant input during an active Agent response

```text
Agent response fragments are committing and streaming
  -> Participant submits new text
  -> runtime authorizes, validates, and durably accepts one Participant message
     with one stable pending Turn and response slot
  -> runtime admits one interaction-decision Invocation
  -> resolved Agent recommends continue, interrupt, or no action
  -> runtime validates against its bound expected Session/output state
     -> continue: finish active response, then activate the pending response slot
     -> interrupt: close active response honestly, then activate that same slot
     -> no action: preserve the pending Turn and apply frozen fallback policy
```

Already published Agent fragments are immutable. Interruption can stop only
future fragments. The next response receives the accepted Participant input
and the exact prior Agent prefix that became authoritative transcript content.

### Interval signal and proactive outreach

```text
existing Session timer event becomes due
  -> runtime reauthorizes and validates state, revision, cooldown, and budgets
     -> response/control lane occupied: coalesce or expire the signal under
        frozen policy without admitting competing Agent work
     -> response lane idle: preserve the existing timer-triggered Invocation
        and Agent-initiated Turn path
  -> resolved Agent directly recommends one permitted message, intentional
     no-action, and/or an optional next-timer change
  -> runtime independently validates the output and timer recommendation
```

The Agent does not schedule or wake itself. The controller may detect or
normalize the signal; the Session runtime owns the timer, trusted trigger, and
Invocation admission. The Agent recommends semantic behavior.

## Proposed defaults requiring approval

- **`PROP-TIC-1` — One Session and one resolved Agent.** Interaction control
  uses purpose-bound Invocations within the existing Session; it never creates
  a parallel Session or independent Agent identity. This preserves isolation,
  configuration fairness, and transcript authority.
- **`PROP-TIC-2` — At most one visible response publisher.** A control-purpose
  Invocation may overlap a response Invocation, but it cannot publish a second
  Agent message while the response lane is occupied. This prevents competing
  Agent speech and ambiguous order.
- **`PROP-TIC-3` — Event-driven controller.** Only admitted mid-response
  Participant input requires MVP control-purpose reasoning. The existing
  one-lane timer path remains a normal timer-triggered Agent Invocation when the
  response lane is available. This bounds cost and avoids continuous
  surveillance or feedback loops.
- **`PROP-TIC-4` — Runtime-bound expected-state effects.** The runtime binds the
  observed Session revision/sequence and active output to the control-purpose
  Invocation. The Agent chooses permitted semantics but cannot author or
  substitute scope, target identity, or expected-state authority. A stale
  recommendation has no effect and is recorded as stale rather than applied to
  newer work.
- **`PROP-TIC-5` — Conservative failure behavior.** If the controller times
  out, fails, or returns invalid output, do not cancel a valid active response.
  Preserve the Participant input and process it after the response when still
  permitted. This avoids destructive behavior from controller failure.
- **`PROP-TIC-6` — Published text remains continuity.** Every durably published
  Agent fragment remains transcript content after interruption. No controller
  may retract, rewrite, or claim it was not published. Publication remains
  distinct from proof of human perception.
- **`PROP-TIC-7` — Reuse the existing timer lane.** MVP interaction control
  does not introduce an additional timer, arbitrary schedules, or parallel
  cadence. This contains scheduler and loop complexity.
- **`PROP-TIC-8` — Frozen positive bounds.** The resolved Session policy fixes
  controller concurrency, timeout, cooldown, signal coalescing, queued-input,
  interruption, proactive-message, Invocation-chain, and per-Session budgets.
  Lower scopes may narrow but never widen them. Distinct accepted Participant
  messages remain separate immutable messages and Turns; only eligible
  non-content signals may be coalesced.
- **`PROP-TIC-9` — Separate durable facts.** Interaction signals, Invocations,
  Decisions, requested actions, response publication, transcript messages,
  audit, Evidence, and telemetry remain distinct records with protected
  references rather than duplicated raw content.
- **`PROP-TIC-10` — Preserve text completion-state compatibility.** An
  intentionally stopped text stream uses the existing `Cancelled` Agent-message
  outcome with a bounded Participant-input-preemption reason; a failure that
  cannot prove a clean cancellation remains `Incomplete`. Do not reuse the
  voice-specific `Interrupted` meaning without a separately approved contract.

## Participant experience targets

The eventual approved UI/UX specification should make each state perceivable
without exposing controller internals:

- The Participant can deliberately send text while the Agent is responding.
- The accepted text and its one stable Turn appear in a stable order and are
  labeled as waiting while the controller/runtime resolves whether the current
  answer continues.
- The UI distinguishes **Agent is responding**, **Checking your message**,
  **Response stopping**, **Message waiting**, and the final complete,
  cancelled/stopped, or incomplete response outcome.
- A stopped or cancelled Agent message retains its exact prefix and an honest
  visible outcome. The UI does not visually concatenate a later response as
  though it were the same uninterrupted message.
- A proactive Agent message appears as Agent-initiated work without fabricating
  a Participant message or exposing raw timer/controller data.
- Reconnect reconstructs the same accepted input, active-output outcome,
  controller resolution, and next permitted action from authoritative state.

Exact copy, focus behavior, announcements, responsive layout, and whether the
Participant may explicitly override automatic routing remain UI/UX decisions.

## Ordering and ownership constraints

- The primary Session store remains the authority for `session_sequence` and
  every material state transition.
- A stable transcript-item order must remain distinguishable from fragment
  event order: later fragments may append to an earlier Agent message after a
  following Participant input has been accepted.
- The contract-authoring task must decide how current message/fragment range
  semantics represent that interleaving without reordering event history.
- Cancellation competes atomically with fragment publication. The winning
  authoritative order determines the last included fragment.
- The runtime binds a controller recommendation to the exact active output and
  expected Session state it may affect. The Agent cannot choose or substitute a
  target identifier or cancel “whatever is currently running.”
- Only the runtime can make an existing pending response slot eligible, admit
  an Agent-initiated Turn, allocate output identity, derive audience, stop
  publication, or change the timer schedule.

## Security, privacy, and fairness constraints

- Reauthorize Organization, Activity, Participant, Attempt, Session, lifecycle,
  and frozen capability scope at signal admission and material effect commit.
- Never trust client-authored timer, presence, active-response, author,
  audience, ownership, or interruption state.
- Treat Participant content as untrusted semantic input. It cannot establish a
  trigger, controller action, expected-state target, policy, scope, or authority
  merely by instructing the Agent to continue, interrupt, or reach out.
- Minimize controller context to the exact purpose. Do not copy transcript or
  Participant content into generic audit, telemetry, errors, or the execution
  manifest.
- Freeze materially behavioral controller policy at cohort activation so
  equivalent assessment Sessions do not receive drifting interruption or
  proactive-outreach behavior.
- Bound model calls and signal chains so model-authored timer requests,
  proactive messages, Participant replies, and controller Invocations cannot
  form an uncontrolled loop.
- Treat controller failure as an interaction failure, not permission to widen
  capabilities, release results, learn from the Participant, or modify the
  Harness.

## Out of scope for the proposed MVP addition

- Voice, speech detection, floor management, TTS, playback acknowledgement, or
  voice interruption semantics.
- Continuous model observation, token-by-token controller re-evaluation, or
  hidden-reasoning inspection.
- Multiple concurrent visible Agent messages or multiple response lanes.
- Multiple Participants in one real-time Session.
- Arbitrary or parallel timers, external event streams, tools, Dynamic memory,
  or general workflow-event orchestration.
- A separate autonomous controller Agent with independent identity, memory,
  authority, or Session.
- Unrestricted self-waking behavior or Agent-authored effects that bypass
  runtime validation.

## Open questions and interim defaults

Interim defaults are working guidance only until explicitly approved.

1. **Should the interaction-decision Invocation use the same model deployment
   as response generation?**
   - **Interim default:** Use the same frozen Agent revision and resolved
     provider/model profile, with a purpose-specific controller instruction and
     output schema recorded in the resolved Session configuration.
   - **Rationale:** Avoids an ungoverned second behavioral profile during MVP;
     a separately qualified controller model can be evaluated later.
2. **When does Participant input become an ordered transcript item while an
   earlier Agent message is still growing?**
   - **Interim default:** Durably accept it immediately with a stable transcript
     position, one stable Turn, and one response slot after the active Agent
     message identity, while recording all fragment and controller events in
     true `session_sequence` order. Its ordinary response Invocation remains
     pending until controller resolution and response-lane eligibility.
   - **Rationale:** Preserves deliberate send and immediate acceptance without
     pretending later fragments occurred before the input event.
3. **What happens if controller reasoning fails or exceeds its deadline?**
   - **Interim default:** Continue the active response and retain the input for
     the next permitted Turn; never infer interruption.
   - **Rationale:** This is the least destructive recoverable outcome.
4. **What happens when a timer event becomes due during active response or
   controller work?**
   - **Interim default:** Coalesce at most one pending timer signal and apply
     expiry and cooldown bounds without admitting parallel Agent work. When the
     lane becomes available, preserve the existing direct timer-triggered
     Invocation and Agent-initiated Turn path.
   - **Rationale:** Preserves proactive opportunity without creating a signal
     backlog or competing decisions.
5. **May Participants explicitly choose interruption instead of relying solely
   on Agent judgment?**
   - **Interim default:** Preserve this as an approved-UI decision. The runtime
     contract should support an explicit stop-and-send request, but automatic
     semantic routing is the initial proposal.
   - **Rationale:** Explicit control is a valuable safety/recovery path, but its
     prominence and assessment consequences require interaction review.
6. **Should an idle timer-triggered proactive turn require a separate control
   model call before message generation?**
   - **Interim default:** No. Preserve the existing timer-triggered Invocation,
     which may directly recommend one permitted message, intentional no-action,
     and an optional next-timer request through the single response lane.
   - **Rationale:** Retains approved semantics and avoids unnecessary latency
     and cost; an extra control-purpose Invocation is reserved for actual
     mid-response arbitration.
7. **Does automatic proactive outreach affect assessment fairness or scoring
   evidence?**
   - **Interim default:** Freeze the same controller policy and budgets for the
     cohort, preserve proactive messages in the transcript, and treat them as
     Evidence only through the existing approved Evidence-selection rules.
   - **Rationale:** Maintains cohort consistency without conflating transcript
     presence with evaluation relevance.
8. **Does text preemption require a new `Interrupted` message state?**
   - **Interim default:** No. Use the existing `Cancelled` outcome with a
     bounded Participant-input-preemption reason when cancellation is proven;
     otherwise preserve the visible prefix as `Incomplete`.
   - **Rationale:** Preserves current text streaming compatibility and avoids
     conflating text cancellation with the deferred voice contract, where
     `Interrupted` has playback-specific meaning.

## Expected authority changes after the dependency clears

The contract task should assess and, only after explicit approval, update:

- `docs/product/mvp-scope.md` and possibly `concept-model.md` for promoted MVP
  meaning and Interaction Controller boundaries;
- `docs/requirements/features/session-text-lifecycle.md` and
  `resolved-session-configuration.md` for observable behavior, frozen policy,
  failure, ordering, and acceptance criteria;
- `docs/ui-ux/text-session.md` for Participant states and controls;
- `docs/architecture/session-runtime-contract.md` plus a new or superseding ADR
  for lanes, concurrency, expected-state effects, persistence, and recovery;
- applicable operational defaults, architecture traceability, design-system
  modules, and feature catalogs where the approved decision requires them.

The authoring task must preserve history, version/supersession links, stable
IDs, and the distinction between Proposed and Approved content.

## Verification themes for later implementation

- Participant input during a response: continue, interrupt, stale decision,
  controller failure, equivalent retry, distinct concurrent inputs, one stable
  Message/Turn/response-slot identity, and reconnect.
- Exact race between fragment commit and interruption cutoff.
- Timer event while idle, responding, paused, reconnecting, completing,
  terminal, revoked, over budget, and already under controller evaluation.
- Exactly one visible Agent publisher and no fabricated Participant message for
  proactive turns.
- Controller retry/idempotency, crash recovery, late provider result, signal
  coalescing, and loop suppression.
- Cross-Organization, Activity, Participant, Attempt, and Session negative
  authorization/isolation tests.
- Frozen cohort policy and lower-scope narrowing tests.
- Participant UI accessibility, announcements, focus, narrow viewport,
  incomplete/cancelled messages, waiting input, and authoritative reconnect.
- Audit reconstruction without raw sensitive-content duplication or hidden
  reasoning exposure.
