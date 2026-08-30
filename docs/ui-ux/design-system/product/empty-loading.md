# Empty, Loading, Streaming & Pending States

## Empty States

A useful empty state is an **instrument plate** (`EmptyPlate`): dim node,
Michroma label, mono note (max about 44ch), optional recovery key. Inset /
separated modifiers (`empty-plate--inset`, `empty-plate--separated`) sit a
dashed hairline empty note after seated content. Unoccupied bays use dashed
absence, not a second clipped card. `OperateArea` `empty` renders this plate
inside the etched frame. Page-level unknown, denied, and missing-resource
planes use `CeremonyUnavailable`: hug ceremony + inset empty well + **quiet**
recovery centered in the well (Return, Reload). Do not use a commit/amber
`open` key on those actions. Auth commit stays `transmit` (`Continue to sign
in`).

## Loading

Loading is a Shipboard instrument family, never a spinner. Gallery: `wait`,
`wait-panel`.

- **Wait-mark:** square hairline bezel with pulsing teal node and scan line
- **Scan-track:** 3px teal determinate or indeterminate fill
- **Skel-stack:** dashed dim lines with optional teal sweep
- **Wait-plate:** `WaitPlate` (`.wait-plate`) is empty-plate anatomy plus
  wait-mark and scan-track. Inset (`.wait-plate--inset`) sits inside an etched
  well.   Auto hug ceremony wait and empty inset wells use the 36rem column cap so a
  short status line does not shrink the well. `CeremonyWait` is the page helper for
  protected ceremony loading; signing out uses the same wait plate until a logout
  error requires the empty well and retry key.
- Occupied keys use the waiting state in [interaction states](../foundation/interaction-states.md)
- **Wait panel:** `WaitPanel` (`.loading-panel`) is the inline protected-loading
  composition: wait-mark plus polite `role="status"` / `aria-busy` copy.
  `announceOnly` keeps the label for assistive technology only. There is no
  `ProtectedLoading` alias. Default copy is generic; product strings come from
  the governing specification. Do not place `WaitPanel` in a ceremony etched
  frame; that well uses `CeremonyWait`.

Teal is the wait voice. Amber stays on the current stage bar or other rationed
attention object only. Reduced motion holds geometry still.

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
