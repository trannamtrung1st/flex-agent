---
id: admin-activities-create-route
status: completed
created: 2026-08-29
updated: 2026-08-29
---

# Goal

Make the administrator Activities registry operable: empty state fully visible,
primary create in the registry toolbar (empty and populated), Campaign create
on a dedicated locator, and the create form reachable by scrolling.

# Governing sources

- `docs/ui-ux/activity-campaign-journey.md` — `JRN-MVP-1`
- `docs/requirements/features/assessment-setup.md`
- `docs/ui-ux/design-system/` — Operate, DataTable toolbar, EmptyPlate
- Neighbor: `ProductionEnrollmentPage` toolbar Assign keys on empty lists

# Scope

## In

- `/activities` registry only
- Toolbar and empty-plate Create when permitted
- `/activities/new` create page; success still goes to setup
- Breadcrumb label for `new`

## Out

- Lab bulk delete / row selection
- Creating a live Campaign in the demo org
- Owner visual acceptance of the broader production reset

# Plan

- [x] Red: list tests require toolbar/empty Create links and no in-page form; create-page tests own the form
- [x] Green: routes, pages, empty-state CSS, create-plane scroll
- [x] Focused unit tests and typecheck
- [x] Playwright MCP screenshots of empty Activities and create page (desktop and narrow)
- [x] Impeccable detector on changed targets

# Current state

Consistency pass applied: both admin registries share OperateArea flush
`registry-frame`, fill empty wells, toolbar + empty-plate primary actions, and
no `registry-wall--empty` hug. Create and Setup stay `record-plane--setup`
ceremonies in one etched well. Enrollment detail is an unframed stacked
record (`framed={false}`); it still uses `record-plane` for chrome pinning,
not an etched clip — see `etched-frame-clip-rule`.

Demo org still has zero campaigns, so populated registry and live Enrollment /
Setup were not browser-verified this pass.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Activities, create, Enrollment, Setup, Enrollment detail unit tests | passed | 26 tests |
| detect.mjs | passed | `[]` |
| Playwright empty Activities desktop | passed | `.playwright-mcp/page-2026-08-29T04-54-23-561Z.png` |
| Playwright create record-plane scroll + submit | passed | `.playwright-mcp/page-2026-08-29T04-53-48-360Z.png` |
| Live Enrollment / Setup | unverified | no Campaign in demo org |

# Decisions

- Locator `/activities/new` (interim default). `JRN-MVP-1` does not require an
  in-page form. Static leaf ranks above `activities/:activityId`. Promote into
  the UI/UX locator table if that spec is live authority after the reset.

# Findings / deviations

- Prior Phase 7 polish put create in a WorkWell below the registry. That fought
  `layout-management__main { overflow: hidden }` and hid toolbar actions when
  the list was empty.

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
