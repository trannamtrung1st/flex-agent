---
id: breadcrumb-destination-trail
status: completed
created: 2026-08-29
updated: 2026-08-29
---

# Goal

Map production breadcrumbs to real destinations only, so nested Activity locators no longer invent collection/item crumbs without pages.

# Governing sources

- `docs/ui-ux/activity-campaign-journey.md` — canonical production routes (`IA-MVP-2`, `IA-MVP-3`)
- `docs/ui-ux/assessment-campaign-setup.md` — campaign hierarchy vs reachable pages
- `docs/ui-ux/design-system/components/sidebars.md` — breadcrumbs
- `web/src/router/production-routes.tsx` — real locators
- `web/src/components/shell/Breadcrumbs.tsx`

# Scope

## In

- Production crumb mapping: destination trail, not URL-segment walk
- Tests and design-system note that crumbs are reachable pages
- Component Deck nested specimen that matches the destination trail
- Browser evidence on the Participants page
- Review-fix: no Home-only trail on prefix-known unknown leaves; Deck setup specimen without phantom Activity

## Out

- Removing Home from `BreadcrumbNav` (spec still requires it)
- Changing routes, BackKey, or gangway
- Campaign/cohort display names in crumbs (locators stay opaque; names remain in-page)

# Plan

- [x] Red: destination-trail assertions for setup, enrollments, enrollment detail
- [x] Green: replace segment walker with destination mapper
- [x] Spec + Deck alignment
- [x] Review: unmatched prefix locators hide the trail; Deck setup crumbs match destinations
- [x] Focused tests, detector, Playwright MCP click-through

# Current state

Production crumbs list reachable destinations. Unmapped prefix-known leaves render no trail. Deck setup specimens match Activities / Setup and readiness.

# Decisions

- Breadcrumbs list reachable destinations only. Locator segments stay in the URL and page context (`IA-MVP-2`).
- Participants roster current crumb is **Participants**. Enrollment detail current crumb is **Enrollment**.
- Setup remains the Activity parent destination. A bare `/activities/:id` locator uses the setup trail to match the redirect.
- An unmatched leaf under a known prefix (for example `/activities/:id/not-a-leaf`) renders no trail, not Home alone.

# Findings / deviations

- Review found a Home-only trail when `isKnownProductionLocator` matched a prefix but the mapper had no destination. Fixed by returning null.
- Deck `layout-management-setup` still showed a non-link Activity crumb. Aligned to the destination trail.
- BackKey compact label **Setup** vs crumb **Setup and readiness** is intentional (action vs destination name).

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused Vitest | pass | Breadcrumbs 9/9 |
| Design-lab gallery-deck | pass | 20/20 including setup trails without Activity |
| Browser Activities index | pass | Home / Activities. `.playwright-mcp/page-2026-08-29T12-45-16-833Z.png` |
| Browser Create + Activities crumb | pass | Home / Activities / Create assessment Campaign; crumb to `/activities`. `.playwright-mcp/page-2026-08-29T12-47-56-945Z.png` |
| Browser Setup | pass | Home / Activities / Setup and readiness. `.playwright-mcp/page-2026-08-29T12-49-31-424Z.png` |
| Browser Participants / Enrollment | pass | Prior pass `.playwright-mcp/page-2026-08-29T12-39-04-577Z.png`, `.playwright-mcp/page-2026-08-29T12-40-59-430Z.png` |
| Browser unknown nested leaf | pass | No breadcrumb nav. `.playwright-mcp/page-2026-08-29T12-50-37-991Z.png` |
| Impeccable detect.mjs | pass | `[]` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe for external review
