# Harness, Snapshots & Configuration History

The Harness defines how an Agent operates: instructions, workflows, policies,
allowed capability subset, rubrics, validation rules, memory controls, and
Evaluation procedure. Harness UI must make mutation controlled, reviewable,
reproducible, and auditable.

This is a later-release shared pattern unless an approved feature specification
enables the relevant Harness-management capability. It does not add Harness
editing, snapshots, restoration, or improvement to the assessment MVP.

## Current working Harness

Use workspace density with stable sections/inspectors for the editable Harness.
Clearly expose:

- current working version/state
- whether there are unapplied or unsaved modifications
- important validation problems
- proposed changes when an approved later feature permits suggestions
- the explicit approval state and authorized actor required by the owning
  feature specification

Do not imply that an Agent can silently self-modify a Harness. Automatic
application of high-risk learning proposals and uncontrolled Harness
modification are out of scope; any later bounded behavior requires an approved
specification and auditable authorization.

## Assignment Policy

An Agent or Activity/Campaign assignment may use:

- **Current** — resolve the current approved Harness revision when a new Session
  is instantiated
- **Pinned** — use a specific immutable Harness snapshot

Show the effective assignment and its source when inheritance/overrides exist.
Do not display `Current` as though an already-running Session can mutate
underneath itself.

## Session Resolution

Every instantiated Session records the exact resolved Harness state/snapshot it
uses. Session detail and audit surfaces should expose that resolved
snapshot/version even if the parent assignment was `Current`.

The resolved Session Harness is immutable historical context for that Session
record; later Harness edits do not rewrite what the Session used.

## Snapshots / Backups

Represent snapshots as versioned objects, not statuses. A snapshot row/detail should expose, when available:

- identifier/version
- created time and actor/source
- concise change summary
- validation/review state when applicable
- usage/pinning information
- compare/diff action
- restore action when permitted

`Restored` is a history event. Restoring creates/establishes a new current harness state according to product behavior; it must not make historical session records appear to have used a different harness.

## Compare / Diff

Prefer a structured diff or split comparison showing changed instructions, tools, policies, workflow/rubric/validation configuration, and other affected harness areas. Use blue for current selection/context, not to imply that every addition is semantically positive. Additions/removals should remain understandable without color.

## Audit / Safety Rules

- Record material harness changes in history/audit surfaces.
- Preserve actor/source and timestamps when available.
- Surface validation failures before actions that would create an invalid runnable configuration.
- Destructive removal or restore actions require clear consequence text.
- Never imply uncontrolled or unaudited self-modification.
