---
id: activities-registry-polish
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Polish the administrator Activities registry: omit the index breadcrumb that
duplicates gangway + OperateHead, remove leftover create-well CSS, and align
the management-index Deck specimen.

# Governing sources

- `docs/ui-ux/design-system/components/sidebars.md` — breadcrumbs are not a second gangway
- `docs/ui-ux/design-system/components/layouts.md` — console index / registry
- Neighbor: Home already omits crumbs; nested setup/create keep trails

# Scope

## In

- Production `/activities` and `/my-work` indexes (same gangway-index rule)
- Deck management index / empty specimens
- Dead `.registry-create` CSS

## Out

- Nested trails (create, setup, enrollment, assignment)
- Design-lab Admin Campaigns wall
- Changing Create placement or empty-plate actions

# Plan

- [x] Red: index trails must not render; Deck index has no breadcrumb nav
- [x] Green: hide crumbs; drop dead CSS; update modules
- [x] Focused tests
- [x] Playwright MCP desktop, empty-search, narrow
- [x] Detector on changed UI targets

# Current state

Completed. Nested Create still shows Home / Activities / Create assessment Campaign.

# Decisions

- Hide crumbs on exact `/activities` and `/my-work`. Nested locators keep Home plus reachable ancestors. Review/release/results already hide via ceremony locator.

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `vitest` Breadcrumbs, production-navigation, AssessmentActivities | passed | 28 tests |
| `test:design-lab` gallery-deck | passed | 26 tests |
| detect.mjs | passed | `[]` |
| Playwright index desktop | passed | `.playwright-mcp/page-2026-08-30T07-53-20-495Z.png` |
| Playwright empty search desktop/narrow | passed | `.playwright-mcp/page-2026-08-30T07-53-48-026Z.png`, `.playwright-mcp/page-2026-08-30T07-54-12-990Z.png` |
| Playwright nested Create trail | passed | `.playwright-mcp/page-2026-08-30T07-54-46-980Z.png` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
