---
id: gallery-document-scroll
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Make the Component Deck catalog column a document scroller so review wheels the page. Nested family specimens hug content. Production hulls and lab Operate routes keep inner-scroll ownership.

# Governing sources

- `docs/ui-ux/design-system/foundation/layout.md` — reference shell; catalog vs hull
- `docs/ui-ux/design-system/components/layouts.md` — Component Deck nested family specimens
- `docs/ui-ux/design-system/foundation/accessibility.md` — avoid competing two-dimensional scroll for ordinary review
- Neighbor: `.work/active/nested-scroll-ownership.md` (production hulls; gallery trap carved out)

# Scope

## In

- Gallery-scoped CSS under `html[data-surface="gallery"]` only
- Hug `.layout-spec` shells; neutralize inner vertical scrollports in specimens
- Hug catalog datatables (`overflow-y: clip` with horizontal auto)
- Catalog contract note in layouts.md, layout.md, and change-record.md
- Gallery CSS source tests and Playwright on `:5275`

## Out

- Shared `layouts.css` / `app-shell.css` / `plates.css` / `datatable.css`
- Lab Operate routes and production overflow
- Deck index rail scroll; overlay widget scroll
- Browse-vs-inspect toggle

# Plan

- [x] Task file and nested-scroll-ownership pointer
- [x] Red: gallery CSS source tests
- [x] Green: gallery.css hug overrides
- [x] Docs
- [x] Verify: vitest, Playwright gallery, Operate spot-check, detect.mjs

# Current state

Component Deck specimens hug. Catalog `body` is the vertical wheel target. Inner hull scrollports in `.layout-spec` use `overflow: clip`. Deck index rail still inner-scrolls. Assignment lab route still uses `overflow-y: auto` on the rail and well.

# Decisions

- Isolation: specimen wrappers only (`.layout-spec`, `.datatable-demo`), never gallery-root `[data-layout]` or bare `.operate-scroll`.
- Neutralized regions use `overflow-x`/`overflow-y: clip`, not `visible` with a non-visible other axis (that computes to `auto`).
- Datatable: `overflow-x: auto; overflow-y: clip`.

# Findings / deviations

- First pass authored `overflow-y: visible` with `overflow-x: hidden` on `.workspace-area`; computed `overflow-y` was `auto`. Switched to `clip` on both axes.
- Consistency pass: hug leftover `min-height: 0` on nested family slot stretch rules collapsed guided-task/live-session slots to ~47px. Restored slot floors (`8rem` rail/examiner, `4.5rem` bay). Also clip `.layout-spec .gangway-body`.
- Full `copied-styles` byte-identity suite still fails on pre-existing `components/keys.css` digest drift (unrelated). Focused gallery assertions pass.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused vitest (gallery CSS + gallery-deck) | passed | 5 filtered + 26 gallery-deck tests |
| Playwright Component Deck desktop | passed | operate `clip`; slots restored 128/72px; `.playwright-mcp/page-2026-08-30T01-04-37-981Z.png` |
| Playwright Component Deck narrow | passed | `.playwright-mcp/page-2026-08-30T01-01-33-181Z.png` |
| Operate lab assignment still inner-scrolls | passed | `participant-journey`: hull `hidden` 100dvh; `.phase-rail-scroll` and `.work-well__body` `overflow-y: auto` |
| detect.mjs | passed | `[]` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
