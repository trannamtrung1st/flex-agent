---
id: collapsible-nav-groups
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Make collapsible sectioned-nav group headers a real control (hand pointer, keyboard disclosure) on the Component Deck index, expose that variant in the gallery, and let Gangway opt into the same per-group collapse.

# Governing sources

- `docs/ui-ux/design-system/components/sidebars.md` (index rail, gangway)
- `docs/ui-ux/design-system/components/accordion.md` (native details/summary; one-open on narrow catalog rails)
- `docs/ui-ux/design-system/foundation/interaction-states.md` (pointer, hover, focus)

# Scope

## In

- Shared `SectionedNavigation` collapsible groups (index default on; gangway configurable)
- Pointer cursor on collapsible group summaries at all widths
- Gallery specimen for the grouped/collapsible menu variant
- Gangway `collapsibleGroups` plus per-group `collapsible` override; keep items visible when the gangway width-collapses

## Out

- Changing production ManagementLayout to collapse groups by default
- A generic Accordion primitive

# Plan

- [x] Red: tests for index collapse, pointer-eligible summaries, gangway opt-in
- [x] Green: SectionedNavigation / Gangway / IndexRail / CSS / gallery
- [x] Docs: sidebars.md and accordion.md gallery pointer
- [x] Verify: Vitest + design-lab Playwright screenshots

# Current state

Review pass completed. Index reveals the group that owns the current item when current moves. ManagementLayout forwards `collapsibleGroups` to gangway and drawer. Design-lab admin enables group collapse. Production shells stay static unless they opt in.

# Decisions

- Index groups stay independently collapsible on desktop (all start open). Narrow catalog (≤900px) keeps one-open accordion.
- Gangway group collapse is opt-in (`collapsibleGroups` or per-group `collapsible`). Width-collapsed gangway force-expands groups so channel codes remain reachable.
- Summary click uses `preventDefault` plus React `open` so jsdom and Chromium stay in sync.

# Findings / deviations

- Impeccable detector still reports pre-existing `.gangway { transition: width }` (`layout-transition`). Not changed.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `vitest` navigation | passed | 4 files / 12 tests |
| `vitest.design-lab` gallery | passed | 35 tests including cursor CSS contract |
| `tsc -b --noEmit` | passed | |
| Design-lab gallery | passed | Origin `http://localhost:5275`; computed `cursor: pointer` on index and specimen summaries; a11y `[cursor=pointer]` |
| Cross-surface review | passed | Gallery index reopen Data after Colors→Datatable; admin gangway fold + width-collapse restores ACC; layout specimen group labels are `span` / `cursor: auto`; production `:18080` not authenticated (session loading) |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
