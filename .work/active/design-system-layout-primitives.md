---
id: design-system-layout-primitives
status: completed
created: 2026-08-27
updated: 2026-08-27
---

# Goal

Add a production-safe inner composition layer (`Stack`, `Inline`, `Grid`, `Container`, `Inset`) so feature UI can reuse tokenized layout instead of one-off flex/grid/spacing CSS, without changing the closed application-shell set.

# Governing sources

- `docs/ui-ux/design-system/README.md` (Approved v1.0)
- `docs/ui-ux/design-system/foundation/layout.md`
- `docs/ui-ux/design-system/components/layouts.md`
- `docs/ui-ux/design-system/components/layout-primitives.md`
- `docs/architecture/frontend-architecture.md`
- ADR-020 `FE-TRANS-9` (not amended; outer-shell contract unchanged)
- Rebuild task `.work/active/impeccable-frontend-rebuild.md` Wave 8.1b remains complete

# Scope

## In

- Documented public API and tokens
- Five composition primitives + CSS + tests
- Design-lab gallery visual showcase (no code samples)
- Representative production and design-lab adoption

## Out

- Fifth application shell
- `sx` / Box / Spacer / runtime breakpoints
- Reviewer `record-grid`, live-session ledger/examiner geometry
- Repo-wide CSS conversion
- Journey action-row promotion (kept domain CSS; nested Inline risked hull-height measurement)
- Participant home Status Bays 4-column grid (intrinsic Grid would change hierarchy)

# Plan

- [x] Track and publish contract
- [x] Red tests
- [x] Implement primitives
- [x] Gallery visual showcase (no code samples)
- [x] Representative migration
- [x] Broader slot-content adoption
- [x] Verification evidence

# Current state

Completed after a defect rescan. Slot content that is a simple vertical or wrapping group uses `Stack`/`Inline`/`Container`. Domain geometry stayed in CSS: Status Bays columns, reviewer record-grid, live-session ledger/readout row reflow, journey action-row, channel-index 3-track rows, EmptyPlate inset grid, OperateHead/ceremony/form-row pairs.

Defect pass (fixed then rescanned clean):

- Released-result well-head: `gap="none"` so `.well-seal` 14px margin is not stacked on Stack gap.
- Activities sections: outer `Stack gap="none"` so dashed `.workspace-section` padding is the only separator.
- `data-flow-*` custom properties scoped to `.composition-*` so Container align cannot leak.
- Wrapping `Inline` children `flex-shrink: 0` (nowrap still shrinks). Long wrapping demo key uses `truncate`.
- Empty-plate wide+center: plate left matches `(specWidth - plateWidth) / 2` (delta 0 at 1440).

# Decisions

- Inner primitives live under `web/src/design-system/components/layout/`, not `patterns/layouts/`.
- Selectors use `.composition-*` and `data-flow-*`. `.layout-*` remains reserved for shells.
- Participant home Status Bays stay a domain 4-column grid.
- Journey action footer stays `.action-row` rather than nested `Inline`.
- Component Deck composition sections are live specimens only. API and usage live in `layout-primitives.md` and source.
- Wide gallery specs use `Stack` `align="stretch"` (`data-flow-align="stretch"`) so width-constrained specimens fill the deck column.
- Channel-index groups, page head, and roster lists use `Stack`; each channel row stays a three-track domain grid (code, copy, Open).
- Journey well and well-head use `Stack`; `.layout-guided__main > .well` keeps flex fill, not inner column CSS.
- `ReadoutList` stays CSS `.readout-stack` because live-session media queries switch it to a row.

# Findings / deviations

- Guided-task action `Inline` was reverted after a hull-height e2e flake while investigating; the shell already owns `.action-row` wrapping. Follow-up candidate, not unfinished primitive work.
- `--space-3`/`--space-4`/`--space-8` moved from rem aliases in `semantic-aliases.css` onto the documented px ladder in `tokens.css`.
- Review found Container width comparison collapsed to content (~136px) because `.spec` uses `align-items: flex-start`. Stretching `.spec--wide` restored prose 592 / form 832 / content 1152 / full 1158 at 1440px.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| focused layout tests | pass | layout.test.tsx |
| production slot tests | pass | AssessmentActivitiesPage, ErrorSummary, ProductionHomePage, OperateArea |
| gallery tests + lint | pass | `pnpm --filter @flex-agent/web test:design-lab` 74 passed; lint + isolation clean |
| Playwright surfaces | pass | Empty-plate center delta 0; wrap keys 390 wrap-as-units no page overflow; recipes KeyGroup fits; policies frozen-line unclipped; released seal→title 14px / title→ident 8px; channel-index gap 32px desktop / 22px ≤760px; Status Bays plate gap 16px. Historical PNGs (`wrap-keys-390-el.png`, `recipes-390-el.png`, `policies-1440.png`) were pruned with the 2026-08-28 `.playwright-mcp/` cleanup. |
| Playwright hull e2e | pass | `surfaces.spec.ts` 11 passed including left rails |
| Production Activities | blocked | `/activities` on 5274 shows Sign in required (API proxy down); form/list Stack/Inline covered by unit tests |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
