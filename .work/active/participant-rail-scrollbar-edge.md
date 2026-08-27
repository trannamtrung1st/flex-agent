---
id: participant-rail-scrollbar-edge
status: completed
created: 2026-08-27
updated: 2026-08-27
---

# Goal

Dock Assignment Station and Examination Console left-rail scrollbars to the rail hairline so they do not sit in the content gutter.

# Governing sources

- `docs/ui-ux/design-system/foundation/layout.md` — hull hairlines; instrument rail
- Follow-up to sticky rail header: scroll body must not inset the thumb into phase/instrument content

# Scope

## In

- Desktop `.phase-rail-scroll` and `.rail-scroll` geometry
- Tests and visual evidence

## Out

- Narrow stacked railband (no local rail scroller)
- Admin gangway

# Plan

- [x] Red: CSS padding + e2e scroller flush to rail edge
- [x] Green: zero rail padding-inline-end; 18px padding on scroller/brand
- [x] Update stylesheet digests
- [x] Playwright MCP screenshots with overflow

# Current state

Complete. Both rails use `padding: 18px 0 16px 26px`. Scrollports sit 1px inside the hairline (the border). Overlay thumbs reserve a stable gutter on that edge.

# Decisions

- Scrollport is flush to the rail hairline; `padding-right: 18px` on the scroller (and brand) restores the original content inset so overlay thumbs sit in that gutter, not on keys.

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Unit CSS | pass | `participant-rail-layout.test.ts` + copied-styles — 34 passed |
| e2e flush scroller | pass | `surfaces.spec.ts` — 7 passed; railRight − scrollRight ≤ 2 |
| copied-styles | pass | journey `f3ee5bda…`; session `3b24d15e…` |
| Playwright MCP | pass | Assignment overflow: railRight 260 / scrollRight 259. Session: 232 / 231. Artifacts `.playwright-mcp/page-2026-08-27T09-27-10-480Z.png`, `.playwright-mcp/page-2026-08-27T09-28-18-515Z.png` |
| detect --scope layout | pass | `[]` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review

# Remaining gaps

- Transcript well scroll geometry was not in this defect; only the left instrument rails.
