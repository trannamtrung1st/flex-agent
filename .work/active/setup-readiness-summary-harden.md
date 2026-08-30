---
id: setup-readiness-summary-harden
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Harden Setup summaries: one ErrorSummary, focus after failed save and blocked readiness, blocked Deck specimen.

# Governing sources

- `docs/ui-ux/assessment-campaign-setup.md` — one error or readiness summary; focus on readiness-result heading when blocked
- `docs/ui-ux/design-system/components/error-summary.md`

# Scope

## In

- Merge save + blocker items into one ErrorSummary
- Focus summary heading after failed save, failed check, failed activation, and after readiness returns blockers
- Gallery blocked Setup composition

## Out

- Production OIDC visual sign-off (`:5274` / `:18080` Setup)
- Inventing a Ready-result heading when check succeeds

# Plan

- [x] Red tests
- [x] Green
- [x] Deck evidence + detector

# Current state

Completed. Review pass 2026-08-30: save-only title aligned to **Correct the following**; gallery field ids `task-submission` and `cohort-state`.

# Decisions

- Blockers win the title (**Readiness blocked**); a save error is an extra unlinked item in the same summary.
- Successful readiness without blockers keeps focus on **Check readiness** (no extra heading).

# Findings / deviations

- Deck hash routes (`#layout-management-setup`) can swallow in-page `#field` jumps for the blocked specimen; production Setup is a path route, so field `href`s remain the contract.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| AssessmentSetupPage + setupStation | pass | Vitest 21 tests |
| GalleryDeck | pass | `pnpm exec vitest run --config vitest.design-lab.config.ts src/design-lab/features/gallery/gallery-deck.test.tsx` |
| Detector | pass | `detect.mjs --json` empty |
| Playwright | pass | `.playwright-mcp/page-2026-08-30T09-26-14-439Z.png` (1280) · `.playwright-mcp/page-2026-08-30T09-26-50-926Z.png` (390) |
| Production Setup OIDC | not verified | Sign-in / access on `:5274` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
