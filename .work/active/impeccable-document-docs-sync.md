---
id: impeccable-document-docs-sync
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Refresh official visual documentation so it matches the current Shipboard
Terminal implementation, then regenerate the non-authoritative Impeccable
adapters. Do not replace root `DESIGN.md` with a Stitch token sheet.

# Governing sources

- `docs/ui-ux/design-system/README.md` — Approved v1.0
- `docs/ui-ux/design-system/implementation-guide.md`
- `docs/ui-ux/activity-campaign-journey.md` — Approved v1.0
- `.agents/skills/impeccable/reference/document.md` (scan mode, adapted)
- `.agents/skills/documentation-author/SKILL.md`
- `scripts/impeccable_context.py`

# Scope

## In

- Canonical design-system visual-evidence and catalog drift vs current `web/`
- Adapter source list and projection language
- Regenerated `PRODUCT.md` / `DESIGN.md`
- Cross-doc consistency of clone rule and layout-family authority
- Change-record note

## Out

- Stitch-format overwrite of `DESIGN.md`
- Product/requirements/ADR consolidation
- New visual identity, tokens, or UI code
- Historical ADR deletion
- Changing production route-layout assignment

# Plan

- [x] Load Impeccable context (`web`) and document playbook
- [x] Refresh canonical modules for visual-authority and catalog drift
- [x] Expand adapter source list to current composition modules
- [x] Regenerate adapters and run check/unit tests
- [x] Consistency review across IA, architecture, skills, lab README
- [x] Record remaining gaps

# Current state

Completed. Consistency pass reconciled approved layout families with current
production `management` assignment for contract-unavailable locators, added
missing IA locators, and aligned clone language across docs and harness files.

# Decisions

- Command: `document` scan mode against canonical `docs/ui-ux/design-system/`,
  then `python3 scripts/impeccable_context.py generate`. Refresh, not overwrite.
- Preserve Shipboard Terminal north star and named color/type rules already in
  the modules. No new metaphor.
- Target app: `web`.
- Did not write a Stitch `DESIGN.md` or `.impeccable/design.json` sidecar;
  those would compete with the generated adapter and `docs/` authority.
- Approved IA families remain the contract target. Current production may keep
  unavailable locators on `management` until the host contract exists.

# Findings / deviations

- Token values in `tokens.css` already matched `foundation/colors.md` and
  related modules; no token rewrite.
- Product docs (`overview` v0.4, concept-model v0.5, mvp-scope v0.4) were
  already current; `PRODUCT.md` fingerprint unchanged except when design
  sources change.
- Consistency review found IA vs production family conflict (Session/Review
  Release) and missing `/activities/new` and `/results` locators. Docs now
  distinguish approved target vs current unavailable assignment.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `python3 scripts/impeccable_context.py check` | passed | adapters current |
| `python3 scripts/check_docs.py` | passed | Documentation validation passed after layouts link fix |
| `python3 -m unittest discover -s scripts -p 'test_impeccable_context.py'` | passed | 15 tests |
| Playwright MCP | skipped | docs-only; no UI change |
| Token re-extract | skipped | scan found no primitive drift |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
