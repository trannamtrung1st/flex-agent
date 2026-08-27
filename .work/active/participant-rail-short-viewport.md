---
id: participant-rail-short-viewport
status: completed
created: 2026-08-27
updated: 2026-08-27
---

# Goal

Make design-lab Assignment Station and Examination Console shells fit genuinely short desktop viewports so rail and work-bay instruments stay reachable without a 620px floor clipping past `body { overflow: hidden }`.

# Governing sources

- `docs/ui-ux/design-system/foundation/layout.md` (short desktop scrolls inside the rail; important actions remain reachable)
- `docs/ui-ux/design-system/components/sidebars.md`
- Review of `6e11ac9` (P2 short-height clipping; P3 breakpoint wording; P3 natural-layout E2E)

# Scope

## In

- Desktop `.station` / `.console` height contract
- E2E coverage at real short viewports (1440×500, 1000×500) plus a natural-layout scroller assertion
- Breakpoint wording in `.work/active/participant-rail-full-height.md`
- Design-system wording for the removed 620px floor

## Out

- Production Phase 8 migration
- Rail IA / sticky-brand architecture changes
- Examiner-plate density redesign (chrono already scrolls internally)

# Plan

- [x] Red: unit + E2E that fail while `min-height: 620px` plus hidden body overflow clips short viewports
- [x] Green: drop the 620px floor so desktop shells stay `100dvh`
- [x] Update stylesheet digests and design-system short-height wording
- [x] Fix full-height task breakpoint wording
- [x] Playwright MCP screenshots at short desktop heights
- [x] Focused design-lab unit + e2e

# Current state

Desktop participant shells are `height: 100dvh` with no 620px floor. Short desktops scroll rail (and assignment well / session ledger / chrono) inside the hull. Stacked assignment still uses page scroll at ≤1080px.

# Decisions

- Remove the 620px min-height rather than enable page scroll on short desktops. Layout.md already requires internal rail scroll on short desktops; a floor taller than the viewport contradicted that while `body` overflow is hidden. Stacked assignment (≤1080px) already uses page scroll.

# Findings / deviations

- Examiner **Submit Session** sits in `.chrono` (overflow-y auto). At 1440×500 it is below the first paint and reachable by scrolling that plate, same pattern as Protocol in the left rail.
- Overlay scrollbars may stay visually quiet until hover; overflow is still allocated (`phase-rail-scroll` overflow 378px at 1440×500).

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Design-lab unit | pass | `pnpm test:design-lab` — 34 tests |
| Design-lab e2e | pass | `pnpm test:e2e:design-lab -- e2e/design-lab/surfaces.spec.ts` — 9 passed |
| Impeccable adapters | pass | `python3 scripts/impeccable_context.py generate` then `check`; unittest `test_impeccable_context.py` |
| Playwright MCP assignment 1440×500 | pass | station 500×500; Start Attempt in viewport; rail overflow 378. `.playwright-mcp/page-2026-08-27T10-01-33-811Z.png` |
| Playwright MCP session 1440×500 | pass | console 500; Transmit in viewport; rail overflow 278; chrono overflow 148. `.playwright-mcp/page-2026-08-27T10-02-06-727Z.png` |
| Playwright MCP session 1000×500 | pass | console 500×1000; min-height 0; Leave session + Transmit in viewport. `.playwright-mcp/page-2026-08-27T10-03-07-173Z.png` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
