---
id: assign-dialog-single-scroll
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Give the Assign Participant overlay one vertical wheel that actually receives the pointer.

# Governing sources

- `docs/ui-ux/design-system/components/modals.md` — live overlay head/foot seated; filling-table body scroll
- `docs/ui-ux/design-system/components/layouts.md` — no nested vertical wheel inside a clip-path overlay plate
- `docs/ui-ux/submission-attempt.md` — Assign picker

# Scope

## In

- One vertical scroller in the Assign overlay
- Spec/change-record note after the clip-path wheel regression
- Remaining review: ceremony-body selector parity; sticky overlay table toolbar

## Out

- Dialog Load more / next-options cursor
- Changing registry Load more
- Moving clip-path off `.dialog-plate`
- Sticky pagination (does not pin at first paint)
- Playwright MCP `mouse.wheel` as product proof

# Plan

- [x] First pass: clip body, fill table (nested `.datatable-scroll`)
- [x] Regression: wheel did not move nested table inside clip-path plate
- [x] Restore body as the overlay wheel; clip nested table vertical overflow
- [x] Docs + style contract + live evidence
- [x] Ceremony-body + sticky toolbar from remaining review

# Current state

Completed. Overlay filling tables scroll `.dialog-body` or `.ceremony-body`. Nested `.datatable-scroll` is not a vertical wheel. Overlay table toolbar is sticky.

# Decisions

- One scroll container: `.dialog-body` or `.ceremony-body` (direct child of the clip-path plate).
- Nested `.datatable-scroll` uses `overflow-y: clip` and `overscroll-behavior-y: auto`.
- Dialog head and Cancel/Assign foot stay seated.
- Overlay table toolbar is sticky so search stays seated while the body scrolls.
- Pagination stays in flow.

# Findings / deviations

- Nested `.datatable-scroll` inside `.dialog-plate` `clip-path` accepts `scrollTop` but does not receive the wheel. Operate filling tables do not sit in a clip-path plate, so that pattern does not copy here.
- Full `copied-styles` suite still fails on pre-existing `keys.css` digest drift. Overlay digest updated; that file is not the failure.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Style contract | passed | `style-entry.test.ts` `:is(.dialog-body, .ceremony-body)` + sticky toolbar |
| Focused vitest | passed | `style-entry.test.ts` 18 tests |
| Nested table not a vertical scroller | passed | rows `clientH === scrollH` (631) |
| Sticky toolbar | passed | `position: sticky`; toolbar Y unchanged at body `scrollTop` 218; first row Y negative; `.playwright-mcp/page-2026-08-30T11-50-27-624Z.png` |
| Body programmatic scroll | passed | desktop 526/745 `scrollTop` 218; narrow 581/849 |
| Narrow screenshot | passed | `.playwright-mcp/page-assign-sticky-narrow.png` |
| Desktop rest | passed | `.playwright-mcp/page-2026-08-30T11-51-26-550Z.png` |
| Detector | passed | `detect.mjs --json overlays.css` → `[]` |
| Playwright wheel | skipped | CDP/`mouse.wheel` on native dialog often `scrollTop` 0 |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
