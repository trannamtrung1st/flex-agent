---
id: gallery-seated-dialog-scroll
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Stop seated Component Deck dialog recipes (Record accommodation) from inner-scrolling. The catalog column remains the only vertical wheel target, matching nested family specimens.

# Governing sources

- `docs/ui-ux/design-system/foundation/layout.md` — reference deck is a document scroller
- `docs/ui-ux/design-system/components/layouts.md` — nested specimens hug
- `docs/ui-ux/design-system/components/modals.md` — live overlay bodies still inner-scroll
- Neighbor: `.work/active/gallery-document-scroll.md` (layout-spec hug; overlays left unchanged)

# Scope

## In

- Gallery CSS for seated in-flow `DialogPlate` recipes
- Catalog contract in layout, layouts, modals, inputs, change-record
- Source tests and Playwright on `:5275`

## Out

- Shared `overlays.css` overlay scroll (native `<dialog>` still inner-scrolls)
- Production hulls and lab Operate routes
- Deck index rail scroll

# Plan

- [x] Red: gallery CSS source test for seated dialog-body hug
- [x] Green: clip seated `.dialog-body` / drop contain overscroll
- [x] Docs
- [x] Verify: vitest, Playwright form-recipes, detect.mjs

# Current state

Review pass 2026-08-30: seated Record accommodation and all four Form recipe OperateAreas hug. `.dialog-body` and `.operate-scroll` both use `overflow` clip + `overscroll-behavior: auto`. Catalog `body` is the document scroller. Leftover `overflow: auto` in the section is only native `textarea` widgets. Live overlay CSS still inner-scrolls `.dialog-body`.

# Decisions

- Isolation: seated catalog plates only (`#form-recipes`), never shared overlay CSS.
- Neutralize with `overflow-x`/`overflow-y: clip` and `overscroll-behavior: auto`, matching `.layout-spec`. `max-height: none` alone is not enough because overlay `.dialog-body` still creates a scrollport with `overscroll-behavior: contain`.
- Form recipe OperateArea `.operate-scroll` uses the same clip hug (not `overflow: visible` + leftover `contain`).
- Live `<dialog>` DemoDialog specimens keep inner body scroll. Filling-table overlays clip the body so the table owns rows.

# Findings / deviations

- Prior gallery-document-scroll pass covered `.layout-spec` only. The accommodation recipe is an in-flow overlay plate, not a nested family hull.
- Consistency review: commission/instrument/ledger recipes still had `overflow: visible` and inherited `overscroll-behavior: contain` on `.operate-scroll`. Aligned to clip + `auto`.
- `copied-styles` byte-identity still reports pre-existing `components/keys.css` digest drift (unrelated). Focused seated-dialog assertions pass.
- Gallery `ItemListSpecimen is not defined` remains a separate lab error; not caused by this change.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused vitest seated dialog hug | passed | `copied-styles.test.ts` seated catalog dialog (body + operate-scroll clip) |
| Playwright desktop metrics | passed | dialog-body clip, not nested; four operate-scroll clip; leftover auto = textareas only |
| Playwright element screenshot | passed | `.playwright-mcp/element-2026-08-30T11-19-48-743Z.png` |
| detect.mjs | passed | `[]` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
