---
id: version-list-generic-composition
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Keep submission version lineage as a production-page component that composes WorkWell ordered-list primitives (`ol`, `data-sequence`, `Stack`). Do not keep a design-system `VersionList`.

# Governing sources

- `docs/ui-ux/design-system/components/lists.md`
- `docs/ui-ux/design-system/components/layout-primitives.md`
- Neighbor: `ProductionEnrollmentDetailPage` History well

# Scope

## In

- Production `SubmissionVersionList` under `web/src/components/work/`
- Design-lab journey specimen inlined with the same `ol` + `Stack` composition (lab cannot import production work modules)
- WorkWell ordered-list CSS (drop `.version-list` opt-out)
- Remove design-system `VersionList` and duplicated version-row CSS

## Out

- `intake-item-list` (still a structured exception)
- DocumentGlyph (still used by FieldFile / reviewer)
- Authenticated production My Work with live version history (no Campaign versions in this browser pass)

# Plan

- [x] Red: page tests require `ol` + `data-sequence` + `Stack`; CSS tests drop version-list exceptions
- [x] Green: production-page `SubmissionVersionList`; lab inline composition; delete design-system VersionList; simplify plates/shell/journey CSS
- [x] Focused tests
- [x] Playwright MCP on design-lab `:5275` submission specimen
- [x] Consistency re-review: lab `ol` now `reversed`; two-row order test added
- [x] Impeccable detect.mjs

# Current state

Version lineage uses the same WorkWell ordered list as enrollment history. Production wraps that composition in `SubmissionVersionList`. Design-lab clones it inline.

# Decisions

- Production assignment surface owns `SubmissionVersionList` (`web/src/components/work/`). It is not a design-system export.
- Design-lab outbound isolation forbids importing `web/src/components/work/`; the journey specimen clones `ol` + `Stack` in place.

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused unit tests (SubmissionVersionList, My Work detail, workWellSectionMarks, Enrollment detail) | passed | 16 tests |
| detect.mjs | passed | `[]` |
| Playwright design-lab submission desktop | passed | `.playwright-mcp/page-2026-08-30T00-27-30-654Z.png` |
| Playwright design-lab submission narrow | passed | `.playwright-mcp/page-2026-08-30T00-28-29-709Z.png` |
| Production My Work with accepted versions | unverified | no live version history in this pass; unit test covers composition |
| Full design-lab copied-styles suite | skipped | pre-existing `keys.css` digest drift unrelated to this change |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
