# Result and Release

This pattern implements shared presentation from the approved
[Result and Release interaction specification](../../result-release.md). That
specification governs behavior; this module does not authorize Release or
redefine Result construction, visibility, correction, or notification.

## Distinct state tracks

Keep Evaluation completion, Review decision, Result readiness, Release, and
Participant visibility visually and textually distinct. `Approved`,
`Completed`, or `Result ready` never implies that a Participant can see a
Result.

Use explicit labels from the governing specification, including
`Result ready · Not released`, `Releasing`, `Released`, `Corrected`, and the
owning unavailable state. Status uses text plus structural/non-color cues from
[status](../foundation/status.md).

## Release work item

An authorized Release item may expose:

- Participant and Activity/Campaign context permitted for the actor
- exact immutable Result candidate/version
- Review decision and Result-ready status
- audience and visibility consequence
- integrity, policy, or stale-state blocker
- Release action and history when authorized

Use workspace density for the queue and calm reading density for the exact
preview. The preview is protected content and follows
[protected-content](protected-content.md).

## Confirmation and pending

- The confirmation title and body name the concrete Result, audience, and
  irreversible visibility consequence without exposing unauthorized detail.
- Initial focus, containment, cancel/Escape behavior, and restoration follow
  [modals](../components/modals.md) and the governing interaction specification.
- **Release Result** is the deliberate primary command only inside the
  authorized confirmation context. It is not a generic success button.
- After confirmation, preserve the exact target and show pending/reconciliation
  status until authoritative Release succeeds, fails, or returns a conflict.
- Duplicate, stale, permission-loss, integrity, and lost-response outcomes use
  the owning recovery state; never claim success from a client-only response.

## Participant Result

- Before Release, use the approved neutral pre-release state without revealing
  Result existence, score, Review content, or timing beyond permitted copy.
- After Release, present the Participant-facing Result content and release time
  without exposing internal Evaluation, reviewer notes, or hidden policy.
- Corrected Results identify the current visible Result and permitted history;
  do not silently overwrite the previously released version.
- Notification indicates availability only and is not proof that Release or
  visibility succeeded.

## Responsive record pattern

At narrow widths, convert queue rows and comparison-heavy metadata into labeled
stacked records while preserving the same reading order, status, exact version,
consequence, and action hierarchy. Never hide Release state or the destructive
visibility consequence in horizontal overflow or a tooltip.
