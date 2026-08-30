---
id: assign-table-pattern-consistency
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Make the Assign Participant table match the shared select/identifier/CompactId grammar, and make CompactId plaques work whenever the full value is not fully visible.

# Governing sources

- `docs/ui-ux/design-system/components/tables.md`

# Scope

## In

- Assign dialog: `HeaderSelectionControl`, `.datatable-id` toggles select, commit iff exactly one row
- `CompactId`: value plaque when logically truncated or CSS-clipped
- Tests for CompactId and the dialog
- Export `resolveSelectedIds` from the design-system patterns barrel
- Cross-surface check of picker vs record vs index tables

## Out

- Bulk assign API loop
- Changing record tables so identifier click selects instead of opening the record
- Review queue select column (no bulk on that surface)

# Plan

- [x] CompactId plaque + tests
- [x] Assign dialog pattern + tests
- [x] Verify Vitest
- [x] Cross-surface Playwright check

# Current state

Completed.

# Decisions

- Record/index tables keep identifier → open/navigate. Picker tables use identifier → select. Lab Campaign registry and EnrollmentTable keep identifier → open while header/row select is for bulk, matching the Deck datatable specimen.
- Assign commit stays one Participant until bulk exists; header select-all disables commit when more than one id is selected. A cohort with a single assignable row still enables commit after select-all (exactly one).

# Findings / deviations

- Review queue and Activities/Participants registries correctly omit header select (not pickers).
- Lab EnrollmentTable `Session state` head stays a special `col-state` cell (not this pass).
- Live Assign dialog on seeded Accessibility Standards Review listed one already-enrolled candidate; select-all therefore stayed a single selection.
- Playwright MCP tabs do not share a signed-in actor; later tabs can still be Demo Participant. Assign verification used Demo Administrator on candidate `:5274`.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Vitest CompactId + ProductionEnrollmentPage | passed | 16 tests, `pnpm exec vitest run` |
| `tsc -b --noEmit` | passed | web package |
| Impeccable detect on CompactId, enrollment page, patterns index | passed | exit 0 |
| Registry CompactId plaque | passed | `.playwright-mcp/page-2026-08-30T08-50-46-256Z.png` |
| Assign dialog rest (header select, id toggle, commit disabled) | passed | `.playwright-mcp/page-2026-08-30T08-51-16-139Z.png` |
| Assign dialog one selected, actor plaque in a11y tree | passed | `.playwright-mcp/page-2026-08-30T08-51-45-763Z.png`; tooltip in snapshot |
| Deck datatable (select + identifier opens) | passed | `.playwright-mcp/page-2026-08-30T08-52-45-886Z.png` |
| Lab EnrollmentTable | passed | `.playwright-mcp/page-2026-08-30T08-53-23-426Z.png` |
| Lab Campaign registry | passed | `.playwright-mcp/page-2026-08-30T08-53-55-112Z.png` |
| Lab review queue (no select) | passed | `.playwright-mcp/page-2026-08-30T08-54-29-502Z.png` |
| Narrow viewport | not run | Seeded assign list was a single row; desktop dialog covered the picker grammar |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
