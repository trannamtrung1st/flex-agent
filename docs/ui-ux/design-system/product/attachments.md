# Attachments & Submissions

Files are versioned Submission or Session artifacts and must remain traceable to
the Participant, turn, Task, Submission version, Activity, or Session context
that produced and consumed them.

## File Row

- icon/preview: 32–40px when useful
- filename: 13–14px, fg-strong, truncate only when full name remains available
- metadata: size/type/time/status in 12–13px fg-muted; mono only for technical identifiers
- row boundary: divider-first; use a card only for a standalone submission object
- actions: preview/open, download/export when permitted, remove before submission when permitted

## Local preparation and intake states

For the assessment MVP, use the state tracks and exact language from the
approved [Submission and Attempt specification](../../submission-attempt.md):

- local preparation: empty, editing, attachments selected, local issue, unsent
  changes
- intake: ready to submit, receiving, validating, cancelling, cancelled,
  rejected, failed, reconciling, accepted

Do not use generic `Uploaded`, `Submitted`, `Ready`, `Failed`, or `Complete`
without the owning object and consequence. A later upload-capable feature may
define another state grammar in its approved specification.

Use explicit text plus progress when real progress is available. Retry must be a
clear action on confirmed failure. An uncertain final response reconciles the
authoritative intake outcome before another finalization is offered.

## Drop Zone

- border: 1px dashed border-strong
- radius: md
- background: surface-inset
- drag-active: surface-selected + border-selected
- always provide a keyboard-operable file picker; drag-and-drop is never the only input method

## Rules

- Show accepted file types/limits before selection when constraints matter.
- Do not imply that local selection, transfer receipt, validation, or progress
  equals an accepted immutable Submission version.
- Preserve provenance in review/audit contexts.
