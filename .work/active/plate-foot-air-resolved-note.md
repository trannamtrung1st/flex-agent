---
id: plate-foot-air-resolved-note
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Give `PlateFoot` equal air above the hairline, show resolved provenance once as a sentence-case helper, and stop repeating it on every frozen Setup field.

# Governing sources

- `docs/ui-ux/design-system/components/cards.md` — plate foot
- `docs/ui-ux/design-system/foundation/layout.md` — bay gap
- `docs/ui-ux/design-system/foundation/typography.md` — helper vs microlabel
- `docs/ui-ux/assessment-campaign-setup.md` — Resolved from … label

# Scope

## In

- Shared air above `.plate-foot` hairline
- Assignment plate uses the same foot pad token
- One station-level resolved note; frozen fields have no hint
- `.field-hint` sentence-case helper voice

## Out

- Changing frozen etch or save-title-only API
- Per-section duplicate notes

# Plan

- [x] Red: CSS contracts + Setup note once
- [x] Green: plates, fields, station, gallery, docs
- [x] Live screenshots

# Current state

Completed. Ceremony docked feet use `--plate-foot-pad-block` margin above the rule (outside the inner scroller). Assignment readouts pad the same token. One sentence-case resolved note.

# Decisions

- Ceremony: foot `margin-block-start`, not scroller padding (padding sat below the last field inside overflow).
- Assignment: predecessor padding-end; dialog/work-well excluded (already inset).
- Copy: `Resolved from this Activity revision.` Later seated on `Alert` Note (`setup-resolved-note-alert`); this task’s field-hint placement is superseded.

# Findings / deviations

- Candidate `:5274` Setup was Access denied during this pass; Deck specimen used for live evidence.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused tests | pass | EtchedFrame, FormField, Setup, style-entry, gallery-deck |
| Detect | pass | `[]` |
| Playwright | pass | gap 16px/16px; one note `text-transform: none`; `.playwright-mcp/page-2026-08-30T08-40-30-514Z.png` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
