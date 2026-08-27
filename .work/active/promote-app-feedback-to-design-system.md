---
id: promote-app-feedback-to-design-system
status: completed
created: 2026-08-28
updated: 2026-08-28
---

# Goal

Promote reusable feedback and navigation primitives from `web/src/components` into
`web/src/design-system`, keep production shell/auth composition in `components/`,
and document the boundary.

# Governing sources

- ADR-020 `FE-TRANS-3`
- `docs/architecture/frontend-architecture.md`
- `web/src/design-system/README.md`
- `docs/ui-ux/design-system/components/alerts.md`
- `docs/ui-ux/design-system/components/error-summary.md`
- `docs/ui-ux/design-system/product/empty-loading.md`
- `docs/ui-ux/design-system/components/layouts.md`

# Scope

## In

- `feedback/`: Alert, ErrorSummary, WaitPanel
- Presentational ThemeToggle in design-system; hook wrapper in shell
- BreadcrumbNav in navigation; route mapper stays in shell Breadcrumbs
- CSS split, gallery specimens, tests, boundary README

## Out

- ProductionAppShell, SessionChrome, ErrorBoundary
- SignOutRetryKey
- ProtectedLoading alias

# Plan

- [x] Task file and `web/src/components/README.md`
- [x] Feedback trio + CSS + gallery + tests; delete `components/ui/`
- [x] ThemeToggle split
- [x] BreadcrumbNav + layout CSS retarget
- [x] Verification and task reconciliation

# Current state

Implementing feedback promotion and shell splits.

# Decisions

- `WaitPanel` replaces `ProtectedLoading` with no compatibility alias.
- `BreadcrumbNav` uses `react-router-dom` `Link` like CommandStrip.
- Shell `ThemeToggle` keeps optional `useTheme` when props omitted.

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused unit tests | passed | 28 tests across feedback, ThemeToggle, BreadcrumbNav, shell wrappers, Activities page, gallerySections |
| Architecture isolation | passed | `FrontendRebuildIsolationTests` 17/17 |
| Playwright gallery | passed | Alert/error-summary, wait-panel, and breadcrumb specimens exercised at desktop and narrow widths with accessibility snapshots. Historical auto-named PNGs under `.playwright-mcp/` were pruned 2026-08-28. |
| Production activities | blocked | `/activities` requires sign-in; WaitPanel covered by unit tests and gallery specimen |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
