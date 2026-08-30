---
id: item-list-load-more
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Add a shared design-system `ItemList` with generic row content via `renderItem` and optional Load more, with two triggers: a trailing key (`button`) and auto-request when the nested scrollport reaches its end (`end`).

# Governing sources

- `docs/ui-ux/design-system/components/lists.md`
- `docs/ui-ux/design-system/components/pagination.md`
- `docs/ui-ux/design-system/implementation-guide.md` (Component Deck catalog)
- `docs/ui-ux/design-system/foundation/interaction-states.md` (waiting keys)
- Neighbor: `DataTableShell` named scroll region; enrollment Load more key (table footer, not this primitive)

# Scope

## In

- Design-system `ItemList<T>` (`items` + `itemKey` + `renderItem`, optional `loadMore`, optional named nested scroll)
- Shared list CSS (hairline rows, load-more item, themed nested scrollport)
- Component Deck `item-list` specimen: nested overflow inside the gallery, interactive Load more
- Design-system docs and Deck catalog mapping
- Row gutter polish: flush frame, `--space-4` gutters, full-width Load more key

## Out

- Migrating production datatable Load more (enrollment registry stays a table footer)
- WorkWell prose/ordered lists and `SubmissionVersionList`
- Infinite auto-fetch without an explicit `loadMore` contract (end trigger still requires `onLoadMore` and stops when `loadMore` is omitted)

# Plan

- [x] Red: `ItemList` unit tests and gallery section/deck tests
- [x] Green: component, CSS, gallery specimen, exports
- [x] Docs: `lists.md`, implementation-guide catalog, pagination cross-link
- [x] Dual trigger: `button` key and `end` IntersectionObserver sentinel
- [x] Row gutter + flush frame + full-width Load more
- [x] Deck demo: more pages, 800ms delay, `waiting` via occupied key / `WaitPanel`
- [x] Focused tests (web + design-lab)
- [x] Playwright MCP on design-lab `:5275` item-list (both triggers)
- [x] Impeccable detect.mjs

# Current state

Completed. End-trigger Deck specimen no longer dumps the last page instantly. Both specimens page 8 of 40 campaigns with an 800ms delay and `loadMore.waiting`, so the button trigger shows an occupied key and the end trigger shows `WaitPanel` (`Loading more`).

# Decisions

- `renderItem` returns row content, not the `<li>`. Callers pass custom elements (keys, inlines, readouts) without fighting list chrome.
- Load more is a quiet compact `Key` that stretches the trailing `<li>`, caption centered. Not a table footer and not a fake data row.
- Deck specimens use `EtchedFrame inset="flush"`; the list owns `--space-4` gutters so hairlines meet the bezel.
- Nested `scroll` makes a labelled `region` with `overscroll-behavior: contain`, cloning `DataTableShell` naming (`"{label}, scrollable"`).
- Deck specimens page 8 of 40 with an 800ms delay so occupied Load more and end-trigger `WaitPanel` are inspectable.
- `loadMore.trigger="end"` uses a 1px sentinel and `IntersectionObserver` rooted on the nested scrollport. Requests are duplicate-locked until `waiting` clears or items change. Occupied end-trigger announces via `WaitPanel`, not a key.

# Findings / deviations

- `copied-styles` `keys.css` digest is still stale from unrelated working-tree drift (same skip as prior gallery work).

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| ItemList unit tests | passed | 8 tests |
| Gallery deck tests | passed | 29 tests |
| Playwright end trigger waiting | passed | scroll to foot → 8 items + `WaitPanel` "Loading more" → 16 items |
| Playwright end WaitPanel | passed | `.playwright-mcp/element-end-wait.png` |
| Playwright keyed occupied Load more | passed | `.playwright-mcp/element-key-wait.png` |
| copied-styles keys.css digest | skipped | pre-existing drift |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
