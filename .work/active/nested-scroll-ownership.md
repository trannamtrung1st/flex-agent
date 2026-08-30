---
id: nested-scroll-ownership
status: in-progress
created: 2026-08-29
updated: 2026-08-30
---

# Goal

Make each production surface have one primary vertical wheel target (plus documented parallel columns and overlay widgets), by clipping or removing nested `overflow: auto` ancestors that currently compete.

# Governing sources

- `docs/ui-ux/design-system/foundation/layout.md` — hull `100dvh`, body overflow hidden, inner work scroll
- `docs/ui-ux/design-system/components/layouts.md` — management clip, operate-scroll, ceremony main overflow
- `docs/ui-ux/design-system/components/cards.md` — operate-scroll vs frame-scroll vs inner instruments
- `docs/ui-ux/design-system/change-record.md` — Management scroll ownership (2026-08-29), Nested scroll ownership (2026-08-30)
- `docs/ui-ux/design-system/foundation/accessibility.md` — avoid two-dimensional scrolling for ordinary text

# Scope

## In

- Clip Status Bays `.operate-scroll` so `.bay-plates` owns column scroll
- Stop stacked `WorkWell` bodies from scrolling inside Enrollment (and other management record planes)
- Keep guided-task `.work-well__body` as the assignment-station scroller
- Same clip/fill for filling tables on lab walls, review queue, and production registries

## Out

- Changing overlay/popover/listbox widget scroll
- Flattening live-session or reviewer parallel columns
- Gallery specimen inner-scroll except where it traps the deck page — follow-on `.work/active/gallery-document-scroll.md`
- A generic `scroll` prop on shared plates

# Plan

- [x] Scan CSS and layout composition for nested scrollports
- [x] Record keep vs clip decisions for owner review
- [x] Implement Status Bays clip and WorkWell overflow ownership
- [x] Consistency pass: filling tables, guided-task well descendants, Status Bays narrow, lab-wall column fill

# Current state

One vertical wheel target per column. Layout CSS owns scroll; plates grow unless a named shell opts in.

- Status Bays: clip `.operate-scroll` at desktop; `.bay-plates` per column. At ≤1080px operate is visible again; a top-level rule sets `.bay-plates { overflow-y: visible }` so nested `html[data-surface]` media does not leave column scroll.
- Stacked management wells: `.work-well__body` overflow visible; `.operate-scroll` is the scroller. Guided-task wells still inner-scroll (descendant selector, so `contain`/`Inset` still match).
- Filling tables: `.operate-scroll:has(.datatable-scroll)` is a flex column clipped so the table can fill and scroll, including lab `.campaigns-wall` / `.wall` that are not `.workspace-area`. Hug registries restore operate-scroll.
- Setup/create: clip operate; `.create-ceremony__scroll` is the inner scroller. 52rem column width is setup-only, not all `record-plane`.
- Split ledger / live session: clipped operate; parallel column rails stay.

# Decisions

- One vertical wheel target per viewport column.
- No generic `scroll` boolean on shared components.
- Status Bays: clip `.operate-scroll` at desktop; `.bay-plates` scrolls per column.
- Stacked wells on management records: `.work-well__body` overflows visible.
- Guided-task assignment: `.work-well__body` remains the work scroller.
- Filling tables: layout opts in via `:has(.datatable-scroll)`, not a table prop.

# Findings / deviations

- Nested `html[data-surface="participant-home"]` media previously lost to `.bay-plates { overflow-y: auto }` at 390px; un-nested override is in place.
- Lab campaign/enrollment walls are not `.workspace-area`, so they did not get operate-scroll flex/`min-height: 0`. Clip-without-fill cut rows (`overflow: hidden` on a block that still sized to content). Filling-table operate panes now share the flex column.
- `.frame-scroll { overflow-x: hidden; overflow-y: visible }` computes as `overflow-y: auto` (CSS overflow pairing). Switched to `overflow-x: clip` so the etched payload is not a dormant nested scroller beside `.datatable-scroll`.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Source overflow inventory | done | CSS grep plus OperateArea / WorkWell / DataTable / shell layouts |
| Focused vitest (style-entry, Enrollment detail, WorkWell) | passed | 22 tests |
| detect.mjs on changed CSS | passed | `[]` |
| Status Bays desktop column scroll | passed | operate `hidden`, crowded columns scroll |
| Lab Campaign registry filling table | passed | `.playwright-mcp/page-2026-08-30T00-49-27-749Z.png`; operate `hidden` equal client/scroll; `.datatable-scroll` 539/628 |
| Lab Enrollment Manifest filling table | passed | operate `hidden`; table scrolls |
| Review queue clip | passed | operate `hidden`; table auto |
| Split ledger parallel columns | passed | operate `hidden`; rail/ledger/marginalia auto |
| Production setup ceremony | passed | operate `hidden`; `.create-ceremony__scroll` auto; `.playwright-mcp/page-2026-08-30T00-49-00-449Z.png` |
| Production Enrollment stacked wells | passed | operate `auto`; three `.work-well__body` `visible`; `.playwright-mcp/page-2026-08-30T00-50-44-274Z.png` |
| Guided-task well still inner-scrolls | passed | Journey: well `auto`, rail `auto` (parallel) |
| Gallery WorkWell not a nested scroller | passed | `#pane .work-well__body` `overflow: visible` (earlier pass) |
| Production Activities filling table | passed | operate `hidden`; table scrolls; `.frame-scroll` `clip`/`visible`; `.playwright-mcp/page-2026-08-30T00-53-47-075Z.png` |
| Production Enrollment index filling table | passed | operate `hidden`; table scrolls; frame `clip`/`visible` |
| Production Home plate grid | passed | operate `auto`; destination bays `visible`; `.playwright-mcp/page-2026-08-30T00-55-26-610Z.png` |
| Live session parallel columns | passed | ledger `auto` (scrolls); chrono/rail `auto` idle; no operate-scroll |
| Config dialog ceremony-body | source | `DialogPlateBody className="ceremony-body"` replaces `dialog-body` — one overlay scroller |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
