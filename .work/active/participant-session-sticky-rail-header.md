---
id: participant-session-sticky-rail-header
status: completed
created: 2026-08-27
updated: 2026-08-27
---

# Goal

Keep the Examination Console left-rail identity (`RailBrand`) seated while only instrument content scrolls, matching Assignment Station and gangway shells.

# Governing sources

- `docs/ui-ux/design-system/foundation/layout.md` — live-session instrument rail; gangway/bulkhead scroll split
- `docs/ui-ux/design-system/components/sidebars.md` — gangway head / body
- `docs/ui-ux/text-session.md` — bounded content scroller; sticky regions must not cover focused controls
- Assignment Station already: `.rail-brand` outside `.phase-rail-scroll`

# Scope

## In

- Design-lab Examination Console left rail markup and CSS
- Tests (unit CSS, component structure, e2e sticky geometry)

## Out

- IA, copy, session phase behavior
- Admin gangway
- Assignment rail (already split)

# Plan

- [x] Spatial thesis and incumbent scan
- [x] Red: CSS + DOM + e2e sticky-header tests
- [x] Green: `.rail-scroll` split; header shrink-0; body overflow
- [x] Update `copied-styles` digest
- [x] Playwright MCP screenshots (desktop + narrow)
- [x] Impeccable layout detector on changed files

# Current state

Complete. Examination Console `RailBrand` is a shrink-0 sibling of `.rail-scroll`. Desktop overflow-y lives on the body only. At ≤1180px the wrapper unwraps (`display: contents`) like Assignment.

# Decisions

- Mirror Assignment: `RailBrand` is a sibling of `.rail-scroll`, not `position: sticky` inside a single scroller.
- At ≤1180px railband, unwrap with `display: contents` like `.phase-rail-scroll`.
- Protocol plate stays in the scroll body (`margin-top: auto` when content is short).
- `.rail > *:not(.rail-scroll)` keeps shrink-0 so the scroller can flex-shrink (specificity vs Assignment’s `.phase-rail > *`).

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Design-lab unit CSS/DOM | pass | `pnpm test:design-lab -- src/design-lab/participant-rail-layout.test.ts src/design-lab/pc-surfaces.test.tsx src/design-lab/copied-styles.test.ts` — 32 passed |
| Design-lab e2e sticky header | pass | `pnpm test:e2e:design-lab -- e2e/design-lab/surfaces.spec.ts` — 5 passed |
| copied-styles digest | pass | `surfaces/participant-session.css` `8d72dd2d…` |
| Playwright MCP desktop/narrow | pass | `.playwright-mcp/page-2026-08-27T09-18-54-164Z.png` (1440×900 seated brand); `.playwright-mcp/page-2026-08-27T09-20-01-044Z.png` (1440×720 scrolled body, brand Y=18); `.playwright-mcp/page-2026-08-27T09-21-20-787Z.png` (390×844 railband unwrap) |
| Impeccable detect --scope layout | pass | `[]` |

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

- Assignment `.phase-rail > * { flex-shrink: 0 }` still wins over `.phase-rail-scroll { flex: 1 1 auto }`; not changed in this task.
- Narrow session railband is not viewport-sticky (page stacks); matches Assignment drawer behavior.
