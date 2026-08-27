---
id: sync-design-system-docs-from-lab
status: completed
created: 2026-08-28
updated: 2026-08-28
---

# Goal

Refresh approved design-system modules from the implemented design-system library and design-lab Component Deck / journey donors. Keep root `DESIGN.md` as a generated, non-authoritative adapter.

# Governing sources

- `docs/ui-ux/design-system/README.md` (Approved v1.0)
- `docs/ui-ux/design-system/implementation-guide.md`
- `web/src/design-system/` and `web/src/design-lab/`
- Impeccable `document` command; repository adapter policy in `scripts/impeccable_context.py`

# Scope

## In

- Official modules under `docs/ui-ux/design-system/`
- Visual-authority note: design lab is the specimen source; production pages are not
- Component Deck catalog map and recently promoted primitives (Alert, WaitPanel, BreadcrumbNav, KeyGroup, OperateArea)
- Regenerated `DESIGN.md` adapter after module edits

## Out

- Stitch-format rewrite of `DESIGN.md` / `.impeccable/design.json` sidecar
- Token or visual redesign
- Production page polish or restyle
- Product/behavior spec changes

# Plan

- [x] Load Impeccable context, document reference, and documentation-author / UI-UX skills
- [x] Scan design-lab gallery, design-system barrels, and official modules for drift
- [x] Patch official modules and change record; expand adapter source list
- [x] Generate adapters and run docs/adapter checks

# Current state

Review pass completed. Catalog mappings and several primitive descriptions now match implementation.

# Decisions

- Keep Approved v1.0; this is documentation of already-shipped presentation, not a token-meaning bump.
- Do not replace the generated `DESIGN.md` adapter with Impeccable’s Stitch DESIGN.md format (same as `impeccable-document-refresh`).

# Findings / deviations

Review pass (2026-08-28) fixed:

- `Alert` `warning`/`success` share the teal Note skin; only `danger` is distinct
- Toast linger is 4200ms, not ~4s
- `select-mark` is table/selection, not instrument badges
- Gallery `nav-rail` is `.nav-rail` grammar; `IndexRail` is the reference catalog
- `OperateArea` `headArrangement` vs assembling `OperateHead`
- `/surfaces` opts out of reference catalog contain
- Accordion is a pattern with no promoted primitive
- Candidate CSS is `shared.css` (ADR-019 still names the legacy split)

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Adapter unit tests | pass | `python3 -m unittest discover -s scripts -p 'test_impeccable_context.py'` — 15 tests |
| Adapter generate/check | pass | `python3 scripts/impeccable_context.py generate` then `check` |
| Docs validation | pass | `python3 scripts/check_docs.py` |
| Playwright MCP | skipped | Documentation only |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [-] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review


# Decisions

- Keep Approved v1.0; this is documentation of already-shipped presentation, not a token-meaning bump.
- Do not replace the generated `DESIGN.md` adapter with Impeccable’s Stitch DESIGN.md format (same as `impeccable-document-refresh`).

# Findings / deviations

- `alerts.md` and `empty-loading.md` omitted `Alert` / `WaitPanel` after the 2026-08-28 feedback promotion.
- `button-group.md` described a shared-bezel strip; implemented `KeyGroup` is wrapping `Inline` with gap `2.5`.
- BreadcrumbNav, TooltipHost, and lab route-layout assignment were implemented but thin in the modules.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Adapter unit tests | pass | `python3 -m unittest discover -s scripts -p 'test_impeccable_context.py'` — 15 tests |
| Adapter generate/check | pass | `python3 scripts/impeccable_context.py generate` then `check` |
| Docs validation | pass | `python3 scripts/check_docs.py` |
| Whitespace | pass | `git diff --check` on changed docs/scripts |
| Playwright MCP | skipped | Documentation only; no visual UI change |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [-] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
