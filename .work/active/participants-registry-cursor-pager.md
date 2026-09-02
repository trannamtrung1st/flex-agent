---
id: participants-registry-cursor-pager
status: completed
created: 2026-09-02
updated: 2026-09-02
---

# Goal

Amend `UI-SUBM-DEC-13` so the admin Participants registry and Assign picker use
one server-backed DataTable pager (signed cursor + `limit`) instead of Load more
plus client paging, then implement that interaction.

# Governing sources

- `docs/ui-ux/flows/submission-attempt.md` — `UI-SUBM-DEC-13`
- `docs/ui-ux/design-system/components/tables.md`
- `docs/ui-ux/design-system/components/pagination.md`
- `docs/ui-ux/design-system/components/lists.md`
- `docs/requirements/features/submission-attempts.md` (cursor lists; no
  exclude-after-load-all)

Before new production UI: classify as existing production registry
(`ProductionEnrollmentPage` + Deck datatable). No new family. No `$impeccable
shape` gap.

# Scope

## In

- Amend `UI-SUBM-DEC-13` and align pagination/list/table design-system copy
- Cursor-mode DataTable pagination (prev/next + rows-per-page; no fake total)
- Production Participants registry and Assign picker fetch `limit`/`cursor`
  (picker `q` prefix)
- Unit tests for the changed pager and page
- Live candidate-origin check (`:5274` with healthy `:18080`)

## Out

- Enrollment-list server search/sort (API has no `q`/order today)
- Lab ItemList Load more (named lists keep that primitive)
- P0 Attempt-start remaining work
- Pinning the Assign-dialog table pager above ceremony foot (existing overlay
  rule: dialog body is the vertical wheel)

# Plan

- [x] Inspect API (`limit`, `cursor`, `has_more`, candidate `q`)
- [x] Amend UX + design-system docs
- [x] Red: enrollment page tests for Next cursor instead of Load more
- [x] Green: pagination component + page + client
- [x] Focused tests + Playwright on healthy candidate origin

# Current state

Independent review 2026-09-02 (second pass): no blockers. Live candidate
origin rechecked. Task file remains until merge/reviewer promotion.

# Decisions

- Cursor pager does not invent a total or page-jump list.
- Registry search/sort stay off until Enrollment list accepts query params.
  Picker search uses existing `q`.
- Prev uses a client cursor stack; it does not synthesize offsets.
- Empty cursor range reuses `00 OF 00` (same empty token as numbered mode).

# Findings / deviations

- Overlay CSS already documents that a filling table in a dialog scrolls the
  plate body; pagination is in flow, not sticky. First paint of a 16-row
  Assign picker hides Next until the body is scrolled. Same geometry as the
  previous numbered footer. Not changed in this task.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| UX spec `UI-SUBM-DEC-13` | done | `docs/ui-ux/flows/submission-attempt.md` 2026-09-02 |
| Design-system pagination / lists / tables | done | cursor footer vs ItemList Load more |
| Focused web tests | pass | vitest 22 tests (`ProductionEnrollmentPage`, `DataTablePagination`, enrollment client) |
| `python3 scripts/check_docs.py` | pass | after `impeccable_context.py generate` |
| Playwright MCP (final confirm) | pass | `:5274` 2026-09-02: first page `01–16`, Next → `17–24` |
| Impeccable detect | pass | `[]` on page + pager |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass (docs check + live registry/picker; no new HTTP API)
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
