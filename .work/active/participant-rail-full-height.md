---
id: participant-rail-full-height
status: completed
created: 2026-08-27
updated: 2026-08-27
---

# Goal

Make participant Assignment Station and Examination Console left rails read as full-height hull bulkheads on desktop, without changing IA, copy, or phase behavior.

# Governing sources

- `docs/ui-ux/design-system/foundation/layout.md` (guided-task / live-session shells; context rail width)
- `docs/ui-ux/design-system/components/sidebars.md`
- Interim visual default: desktop flush bulkhead; stacked header at drawer widths

# Scope

## In

- Assignment `.phase-rail` and session `.rail` desktop geometry
- Frame traces confined to the work bay
- Session left rail twin treatment; examiner plate remains an inset work plane

## Out

- Product/IA/navigation changes
- Admin gangway
- Narrow stacked layout becoming viewport-sticky

# Plan

- [x] Record spatial thesis and interim default
- [x] Red: desktop e2e that the assignment and session left rails meet the hull
- [x] Green: CSS bulkhead geometry on both surfaces
- [x] Update stylesheet digests
- [x] Playwright MCP screenshots (desktop + narrow)
- [x] Focused design-lab tests

# Current state

Desktop rails are flush hull bulkheads. Narrow/drawer widths keep the stacked instrument band.

# Decisions

- Desktop (≥1080 assignment / ≥1180 session): left rail flush top/bottom/left; work bay keeps 18px block inset.
- Traces are clipped to the work-bay column, not the viewport.
- Session right examiner plate stays an inset plate.
- Session brand stays outside `.rail-scroll` (compatible with the pinned-brand scroller).

# Findings / deviations

- Review pass: assignment rail now excludes `.phase-rail-scroll` from `flex-shrink: 0`, matching the session rail so short desktops scroll instruments instead of clipping.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Design-lab rail layout + copied-styles + pc-surfaces | pass | `pnpm test:design-lab` — 32 tests |
| Design-lab e2e hull geometry | pass | `pnpm test:e2e:design-lab -- e2e/design-lab/surfaces.spec.ts` — 5 passed |
| Playwright MCP desktop assignment | pass | rail box `0,0,260,900` at 1440×900; `.playwright-mcp/page-2026-08-27T09-13-28-302Z.png` |
| Playwright MCP desktop session | pass | rail box `0,0,232,900`; `.playwright-mcp/page-2026-08-27T09-14-48-713Z.png` |
| Playwright MCP stacked / narrow | pass | assignment 1000px stacked header; `.playwright-mcp/page-2026-08-27T09-16-05-847Z.png`; session 390px stacked; `.playwright-mcp/page-2026-08-27T09-17-40-041Z.png` |
| Home from assignment rail | pass | navigated to `/design-lab/participant-home` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
