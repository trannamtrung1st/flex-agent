# ADR-013: Agent-requested next-timer replacement

## Status

Approved

## Decision metadata

| Field | Value |
| --- | --- |
| **Owner** | Architecture Lead |
| **Required approvers** | Product Lead, Architecture Lead, Security/Privacy reviewer |
| **Consulted perspectives** | Business analysis, architecture, UI/UX, security/privacy, documentation |
| **Approved date** | 2026-08-11 |
| **Governs** | Optional Agent recommendation to replace the next system timer event for an enabled Session timer lane |
| **Upstream sources** | Approved [concept model v0.3](../../product/concept-model.md), [MVP scope v0.3](../../product/mvp-scope.md), [resolved Session configuration v0.3](../../requirements/features/resolved-session-configuration.md), and [text Session lifecycle v0.4](../../requirements/features/session-text-lifecycle.md) |
| **Extends** | [ADR-012](ADR-012-structured-agent-invocation-and-decision-boundary.md) |
| **Further extended by** | [ADR-014](ADR-014-agent-output-envelope-and-p0-compatibility.md) treats the next-timer request as the only P0 requested action on the Decision envelope |
| **Preserves** | [ADR-002](ADR-002-authorization-enforcement-and-delegation.md), [ADR-003](ADR-003-authorization-audit-persistence.md), [ADR-005](ADR-005-atomic-attempt-start-and-submission-binding.md), [ADR-011](ADR-011-participant-visible-agent-response-streaming.md), and Session terminal-cutoff authority |

## Context

A Session may use one frozen system timer cadence, such as a periodic check for
whether the Agent should act. A fixed cadence alone can run too early or too
late for the Agent's current semantic context. The Agent therefore needs an
optional way to recommend that the next timer occur after another bounded
relative delay.

The Agent must not become a scheduler or self-waking authority. A recommendation
must not create parallel timers, bypass Session state, widen frozen policy, or
form an unbounded event loop. The scheduling control must also coexist with an
otherwise valid `emit_message`, `no_action`, or other permitted Agent Decision.

## Decision drivers

- Let Agent judgment adapt the next check without granting scheduling authority.
- Preserve exactly one pending next event for the enabled Session timer lane.
- Keep the system default cadence as the fallback.
- Make timer replacement ordered, idempotent, reconstructable, and cutoff-safe.
- Bound frequency, total reschedules, and Agent-trigger feedback loops.
- Avoid participant-facing countdown or internal-control disclosure unless a
  later approved workflow specifically requires it.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Fixed system cadence only | Simple and predictable | Cannot adapt the next check to current semantic context |
| Add every Agent-requested timer beside the default cadence | Preserves the base schedule | Creates overlapping Invocations, timer accumulation, races, and loop risk |
| Let the Agent directly schedule arbitrary jobs | Flexible | Makes model output authoritative and bypasses frozen policy and lifecycle control |
| Replace one next timer through independently validated optional Decision control | Adaptive, bounded, and reconstructable | Requires a versioned timer-lane state and replacement transaction |

## Decision

### Use optional next-timer control on a successful Decision

A successful Agent Decision may contain one optional semantic
`next_timer_request`. It recommends a positive relative delay from the
authoritative scheduling-effect commit boundary. It is not a separate
Invocation, provider call, trusted trigger, or authoritative schedule.

The scheduling recommendation is validated independently from the Decision's
primary behavior. A policy rejection of a structurally valid timer request does
not by itself reject an otherwise permitted `emit_message`, `no_action`, or
other Decision behavior. Malformed control that makes the complete Decision
schema invalid remains an Invocation execution/validation failure under
ADR-012.

Exact wire names and duration encoding belong to versioned schemas. Provider-
native delayed-job, tool-call, or scheduling formats are not domain authority.

### Maintain one Session timer lane

The feature permits at most one logical Agent timer lane per Session; when
enabled, that is the Session's sole lane unless a later approved specification
defines additional lanes. Frozen policy supplies:

- whether the lane is enabled;
- its default relative delay;
- positive minimum and maximum Agent-requested delays;
- active-time clock basis for P0;
- maximum accepted replacements and timer-triggered Invocations per Session;
- cooldown, concurrency, and duplicate-suppression limits; and
- permitted trigger stages and Agent Decision capabilities.

An accepted request atomically or equivalently replaces the lane's one pending
next event. When the driving Decision belongs to the timer Invocation whose
event has already fired, the accepted request instead installs the lane's sole
successor event in place of the default successor that would otherwise be
armed. It never appends a parallel event. The schedule retains stable lane
identity, a monotonically increasing schedule revision, due-time facts,
driving Decision provenance, and state such as `Pending`, `Claimed`, `Fired`,
`Cancelled`, `Superseded`, or `Expired`.

An equivalent retry reconciles to the same schedule revision. A mismatched,
stale, late, out-of-bounds, prohibited, or post-cutoff request causes no
scheduling effect and does not erase a still-valid existing next event.

### Resume the default cadence

When the enabled lane's Session enters `Active`, the system arms its first event
using the frozen default delay. After a timer event fires and its Invocation
reaches a terminal outcome, the runtime arms the next default event unless the
successful Decision supplies an accepted replacement. Omission or rejection of
a request does not disturb a still-valid pending event.

This establishes replacement rather than accumulation:

```text
one pending default next event
  -> optional Agent recommendation
  -> independent schedule validation
  -> same lane revision replaced, or existing/default schedule retained
  -> one trusted timer event fires
  -> one new Agent Invocation may be admitted
  -> default cadence resumes unless replaced again
```

If another Invocation attempts to replace the lane concurrently, authoritative
Session order and expected schedule revision select one winner; the loser
reconciles or fails without creating another pending event.

### Preserve lifecycle and cutoff authority

P0 relative delay uses active Session time. Pause suspends the delay and no
Agent timer fires while the Session is not `Active`. Resume recomputes the due
instant from authoritative remaining active delay. Revocation, completion,
expiry, termination, abort, or another terminal cutoff cancels or expires the
pending event and prevents late claims or Agent results from rearming it.

When due, the scheduler first reauthorizes its service delegation and
revalidates Session ownership, lifecycle, frozen policy, schedule revision,
budget, and cutoff. Only then may it commit one typed trusted timer trigger and
admit or idempotently reconcile one new Agent Invocation. The model-authored
request is provenance for the schedule; it is never the trusted trigger itself.

### Keep the participant experience bounded

The pending timer, requested delay, rejection reason, and internal schedule
revision are not participant transcript content and are not shown by default.
If a timer-triggered Invocation later produces a permitted Agent Message, it
uses the existing Agent-initiated Turn and ADR-011 publication behavior. If it
produces `no_action`, the existing intentional-no-action UI behavior applies.

## Security and privacy consequences

| Risk | Required control | Minimum verification |
| --- | --- | --- |
| Rapid self-trigger loop or resource exhaustion | Positive delay, replacement-count, Invocation-count, cooldown, and concurrency bounds | Minimum/maximum delay, repeated replacement, long-running Invocation, and exhaustion tests |
| Parallel or duplicate timers | One lane identity, expected revision, uniqueness, idempotency, and authoritative order | Equivalent retry, mismatched reuse, concurrent replacement, worker race, and process-restart tests |
| Cross-Session scheduling | Trusted Session binding and full resource-chain validation at request and fire | Substituted Organization/Activity/Participant/Attempt/Session and guessed schedule tests |
| Post-pause or post-cutoff action | Active-time clock, no firing outside `Active`, reauthorization and cutoff validation | Pause/resume, revocation, completion, expiry, termination, and abort races |
| Prompt injection chooses unsafe timing | Frozen min/max and eligibility policy; independent validation | Negative, zero, oversized, malformed, prohibited-stage, and policy-widening requests |
| Sensitive content copied into scheduler records | Bounded control fields and stable Decision/Invocation references | Log, event, audit, export, and telemetry leakage tests |

## Consequences

- The Agent can adapt one next timer while the runtime remains scheduler and
  trigger authority.
- P0 gains an optional frozen-policy timer cadence and bounded replacement seam;
  silence-driven behavior, arbitrary timers, parallel timer lanes, voice,
  tools, and configurable workflow stages remain deferred.
- Resolved configuration, manifest, Session state, scheduler contracts, and
  verification require new fields and records before implementation acceptance.
- No new participant-facing countdown or control is required.

## Verification required before implementation acceptance

- Versioned Decision-control and timer-trigger schema fixtures.
- Default cadence, accepted replacement, rejected replacement, and default-
  resumption tests.
- Exactly-one pending event and exactly-one Invocation under duplicate,
  concurrent, restart, and lost-response conditions.
- Active-time pause/resume tests and every terminal-cutoff race.
- Bounds, cooldown, total-budget, and feedback-loop exhaustion tests.
- Independent primary-Decision versus timer-request validation tests.
- Wrong-scope, stale delegation, revocation, and protected-data leakage tests.

## Related

- [ADR-012: Structured Agent Invocation and Decision runtime boundary](ADR-012-structured-agent-invocation-and-decision-boundary.md)
- [ADR-014: Agent Decision output envelope and P0 compatibility](ADR-014-agent-output-envelope-and-p0-compatibility.md)
- [Text Session runtime contract](../session-runtime-contract.md)
- [Resolved Session configuration](../../requirements/features/resolved-session-configuration.md)
- [Text Session lifecycle](../../requirements/features/session-text-lifecycle.md)
- [Text Session interaction specification](../../ui-ux/text-session.md)
