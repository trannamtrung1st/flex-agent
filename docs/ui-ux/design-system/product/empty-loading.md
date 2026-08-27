# Empty, Loading, Streaming & Pending States

## Empty States

A useful empty state is an **instrument plate**: dim node, Michroma label, mono
note (max about 44ch), optional primary key. Unoccupied bays use a dashed
hairline empty note (dashed absence), not a second clipped card.

## Loading

Loading is a Shipboard instrument family, never a spinner:

- **Wait-mark:** square hairline bezel with pulsing teal node and scan line
- **Scan-track:** 3px teal determinate or indeterminate fill
- **Skel-stack:** dashed dim lines with optional teal sweep
- **Wait-plate:** empty-plate anatomy plus wait-mark and scan-track
- Occupied keys use the waiting state in [interaction states](../foundation/interaction-states.md)

Teal is the wait voice. Amber stays on the current stage bar or other rationed
attention object only. `role="status"` / `aria-busy` / polite live regions as
appropriate. Reduced motion holds geometry still.

## Streaming

- show content incrementally when appropriate
- use a current-generation marker at the boundary only
- do not animate previously generated content
- keep Agent Core processing/speaking synchronized with real semantics

## Pending

Use explicit labels such as `Waiting`, `Queued`, or `Processing`. Avoid
ambiguous pulsing dots without text when state matters. Do not use
Participant-facing **Pending release** copy (`PC-03`).

## Product Composition Rules

### Live Interaction — Session console

Instrument rail, ledger, examiner plate, composer. Strongest Shipboard
expression. Later-release voice beacons stay out of MVP text Session.

### Activities / Campaigns & Enrollment — Operations

Tables, gangway, Campaign Context instrument, ceremony dialogs. Save draft /
Check readiness / server Activate (`PC-05`). Invalid Campaign id shows
unavailable, never silent substitution (`PC-06`).

### Evaluation & Review — Analysis bay

Split ledger and marginalia. Separate Review decision from Release (`PC-01`).
Human revision is preview-and-submit, not local mutation (`PC-02`).

### Result / Release

Neutral unpublished Participant state; server-projected released fields only
(`PC-03`, `PC-04`).

### Marketing / Onboarding

May be more cinematic: larger Core, hull ground, restrained traces. The
application remains instrument-like.
