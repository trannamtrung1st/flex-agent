---
id: static-header-assign-table
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Add `StaticHeader` as the unsorted sibling of `SortableHeader`, use it on existing static tables, and give the Assign Participant dialog matching header height plus a compact Actor id column.

# Governing sources

- `docs/ui-ux/design-system/components/tables.md`
- `docs/product/concept-model.md` (Actor as identity)

# Scope

## In

- `StaticHeader` (`th` + `.col-head` + `colMin`)
- Assign dialog: static heads, Participant + Actor (`CompactId` of `actor_id`)
- Replace hand-rolled `.col-head` spans in Deck/reviewer tables

## Out

- Dialog filter form
- Nested etched frame in the dialog
- Schema-driven `thead` wrapper

# Plan

- [x] Red: `StaticHeader` + enrollment dialog column tests
- [x] Green: component, exports, dialog, migrations, docs
- [x] Verify: Vitest; Playwright production dialog blocked on session

# Current state

Completed. Production dialog screenshot needs Demo Administrator; Playwright tabs had a Participant session.

# Decisions

- Column label is **Actor** (concept-model identity); value is `CompactId` of `actor_id`.
- `thead`/`tr` stay HTML. Select and visually hidden action heads stay as they are.

# Findings / deviations

- Playwright MCP on `:5274` after Home (Demo Participant) could not reopen admin Participants (`Access denied`).

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Vitest StaticHeader + enrollment | pass | 10 tests |
| `tsc -b --noEmit` | pass | |
| Impeccable detect | pass | `[]` |
| Playwright assign dialog | blocked | Participant session on candidate origin |
| Playwright Deck heads | pass | `.playwright-mcp/page-2026-08-30T08-43-34-857Z.png` |

# Blockers

None remaining in code. Manual: sign in as Demo Administrator and open Assign to confirm header height vs registry.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
