---
id: home-plate-grid-promotion
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Promote Home/My work assignment plates and plate-grid fill onto the design system, drop destination-bay one-off CSS, hide unavailable Home destinations, and catalog the new recipes on the Component Deck.

# Governing sources

- `docs/ui-ux/design-system/components/cards.md`
- `docs/ui-ux/design-system/components/layout-primitives.md`
- `docs/ui-ux/design-system/foundation/layout.md`
- `docs/ui-ux/activity-campaign-journey.md` (`IA-MVP-1`, destination absence)
- `PC-09`

# Scope

## In

- `Grid` `fit="fit" | "fill"`
- Promoted `AssignmentPlate` + horizon readout
- Home: available destinations only
- My work: same `Grid` fill recipe
- Component Deck specimens
- Design-system module updates

## Out

- Full `IA-MVP-1` Home work-item feed
- Post-login landing route

# Plan

- [x] Red: Grid fill, Home available-only, AssignmentPlate DS contract, gallery registry
- [x] Green: primitives, CSS, pages, gallery, docs
- [x] Focused tests and live Home / Deck verification

# Current state

Completed. Home uses `Grid fit="fill"` and `AssignmentPlate`. Unavailable destinations are omitted.

# Decisions

- `Grid` `fit="fill"` is `auto-fill`; default remains `auto-fit`.
- Compact (≤720px) fill grids stay one column.
- Home omits unavailable destinations; empty Home stays `CeremonyUnavailable`.
- Assignment plate reuses `frame-cut` / `--cut: var(--notch)` without etched ticks.

# Findings / deviations

- `IA-MVP-1` priority-band Home feed is still not implemented; Home remains an available-destination catalog.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `pnpm typecheck` + `typecheck:design-lab` | pass | tsc |
| Focused vitest (Home, My work, Grid, AssignmentPlate, style-entry) | pass | 36 tests |
| `pnpm test:design-lab` gallery-deck + gallerySections | pass | 36 tests |
| Live Home `/` administrator | pass | `.playwright-mcp/page-2026-08-30T08-05-55-328Z.png`, `.playwright-mcp/page-2026-08-30T08-06-21-111Z.png` — Activities only, no My work dead plate |
| Live Deck `#assignment-plate` | pass | `.playwright-mcp/page-2026-08-30T08-05-06-364Z.png` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
