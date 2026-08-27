---
id: impeccable-document-refresh
status: completed
created: 2026-08-27
updated: 2026-08-27
---

# Goal

Keep root `DESIGN.md` as a generated, non-authoritative pointer at `docs/ui-ux/design-system/`. Sync those official modules with the implemented Shipboard Terminal tokens, type delivery, and rail geometry.

# Governing sources

- `docs/ui-ux/design-system/README.md` (Approved v1.0) — visual authority
- `docs/ui-ux/design-system/implementation-guide.md`
- Implemented tokens and surfaces under `web/src/`

# Scope

## In

- Sync `docs/ui-ux/design-system/` with implemented CSS names, font pins, and participant rail bulkheads
- Keep `DESIGN.md` as a generated referral (no Stitch frontmatter, no sidecar expansion)

## Out

- Canonical Impeccable DESIGN.md rewrite
- `.impeccable/design.json` sidecar
- Visual redesign or token invention
- Product/behavior spec changes

# Plan

- [x] Load Impeccable context and document reference
- [x] Scan tokens, DESIGN.md, design-system modules, and doctor findings
- [x] Sync official design-system modules and thin the DESIGN.md adapter to a referral
- [x] Verify adapter tests and `impeccable_context.py check`

# Current state

Review pass completed. Adapter, official modules, and CSS names now agree. Unused 2px focus aliases aligned to the live 1px/3px ring. `tokens.css` no longer claims DESIGN.md frontmatter.

# Findings / deviations

- Light `--teal-glow` is emission-focus, not light `brand-softer`. Documented in `colors.md`.
- No `.impeccable/design.json` sidecar (explicitly out of scope).

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Adapter unit tests | pass | `python3 -m unittest discover -s scripts -p 'test_impeccable_context.py'` — 15 tests |
| Adapter check | pass | `python3 scripts/impeccable_context.py generate` then `check` |
| Docs validation | pass | `python3 scripts/check_docs.py` |
| Whitespace | pass | `git diff --check` |
| Design-lab unit tests | pass | `pnpm --filter @flex-agent/web test:design-lab` — 7 files, 34 tests |
| Playwright MCP | skipped | Docs/token-comment/alias-only; no visual change |

# Decisions

- User chose not to expand `DESIGN.md` into Impeccable's canonical visual spec. Official modules stay visual authority.
- Record implemented CSS custom properties (`--ground`, `--notch`, `--gangway-w`) instead of the unused `--fa-*` examples.
- Record `@fontsource/michroma@5.3.0` and `@fontsource/sometype-mono@5.3.0` as the completed `DS-PROP-2` pin.
- Document participant instrument rails as desktop hull bulkheads (assignment 260px / ≤1080 stacked; session 232px / ≤1180 stacked).

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [-] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
