---
id: setup-readiness-ceremony-record
status: completed
created: 2026-08-29
updated: 2026-08-29
---

# Goal

Rebuild production Setup and readiness as the locked Ceremony-on-record station: Operate head with a monument next-action sentence, a wide ceremony plate filling the management record, four-track chase, campaign title, and a split foot that omits unarmed keys.

# Governing sources

- Confirmed shape brief (2026-08-29): Ceremony on record
- Approved comp: `.impeccable/mocks/decision/assigned.webp`
- `docs/ui-ux/assessment-campaign-setup.md` — `UI-ACT-DEC-1`–`6`, four state tracks
- `docs/ui-ux/design-system/components/layouts.md` — management nested record
- `docs/ui-ux/design-system/components/cards.md` — OperateArea, PlateFoot
- `docs/ui-ux/design-system/components/buttons.md` / `button-group.md`
- `docs/ui-ux/design-system/foundation/status.md` — instrument marks; Stable memory copy

# Scope

## In

- `AssessmentSetupPage` composition and copy
- Four-track chase (local / draft / readiness / cohort)
- Next-action sentence, key drop-off, confirmation dialog, activated handoff
- Production CSS for the setup ceremony plate
- Component Deck `layout-management-setup` specimen (design-system only; no production-module import)

## Out

- Seven spec setup sections and new assessment APIs
- Guided-task / assignment-station hull
- Dialog-as-page
- Design-lab CampaignConfigDialog fixture mutations (keys/`disabledReason` specimen)

# Plan

- [x] Shape brief locked (Ceremony on record)
- [x] Red: station presentation + page tests
- [x] Green: page, chase, foot, CSS
- [x] Focused tests
- [x] Playwright MCP desktop + narrow + confirm + activated
- [x] Polish: design-system ReadoutGrid/context, frozen fields, Component Deck specimen

# Current state

Polished. Production station uses Operate `context` ReadoutGrid (same nested-record grammar as Enrollment), frozen title fields, and a Component Deck Management setup specimen. Campaign Configuration dialog is unchanged.

# Decisions

- Keep `management` + `OperateArea` `record-plane--setup`; ceremony plate fills one etched well at the 52rem form column. Stacked nested records (Enrollment) are unframed and fill the landmark — see `etched-frame-clip-rule`.
- Activation confirmation stays a `CeremonyDialog`.
- Unavailable keys omit rather than sit disabled.
- Memory `disabled` displays as Stable with supporting copy (`status.md`).
- Activate is shown only when `activate_cohort` is permitted, readiness has a current no-blocker result, and the title is not dirty (`UI-ACT-DEC-3`).
- After activation, the readiness track stays Ready even when the payload omits `issues`.
- Four tracks live in Operate `context` as `ReadoutGrid` columns={4}, matching Enrollment identity instruments and the Component Deck readout grid.
- Activated campaign title uses FieldInput `frozen`, not `disabled`.
- Design-lab Management setup duplicates the composition with design-system modules only (frontend isolation). Campaign Configuration dialog stays the keys/`disabledReason` specimen.

# Findings / deviations

- Production still exposes only campaign title plus save / check / activate. The seven setup sections in the UI spec remain out of scope (no new APIs).
- `UI-ACT-DEC-2` unsaved-leave guard implemented: `useBlocker` + `useBeforeUnload`, `SetupUnsavedLeaveDialog` with spec copy and three actions; title field shows revision hint when saved.
- Review follow-up: `allowNavigationRef` + `flushSync` + `queueMicrotask` on `blocker.proceed()` prevent double `beforeunload` after discard/save-and-leave; **Leave without saving** uses `key--danger` (quiet destructive).

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused vitest | pass | `pnpm test src/pages/AssessmentSetupPage.test.tsx src/features/assessment/setupStation.test.ts` — 15 passed (includes leave-guard cases) |
| Design-lab vitest | pass | `pnpm test:design-lab …gallery-deck.test.tsx …gallerySections.test.ts` — 27 passed |
| Frontend isolation | pass | `node build/scripts/check-frontend-isolation.mjs` |
| Impeccable detect | pass | `detect.mjs --json` on setup page, station, CSS, LayoutSections — `[]` |
| Playwright ready desktop | pass | `.playwright-mcp/page-setup-ready-desktop-confirm.png` — ReadoutGrid in context, amber Activate in well |
| Playwright confirm | pass | `.playwright-mcp/page-setup-confirm.png` |
| Playwright ready narrow | pass | `.playwright-mcp/page-setup-ready-narrow.png` — readout-grid stacks to one column below 46rem |
| Playwright activated | pass | `.playwright-mcp/page-setup-activated-desktop.png` — frozen title, Ready+Activated |
| Playwright lab specimen | pass | `.playwright-mcp/page-setup-lab-gallery.png` — `#layout-management-setup` four-track draft specimen |
| Playwright leave guard (post-fix) | pass | `.playwright-mcp/setup-unsaved-leave-dialog-final.png` — custom dialog on Activities + gangway Home; discard navigates without native `beforeunload`; `key--danger` on leave action |
| Light theme | not run | Out of this polish pass |

Origin: candidate UI `http://localhost:5274` with Compose `:18080` healthy (`session-endpoint:ok`). Synthetic `demo.admin`. Campaign `a1000000-0000-4000-8000-000000000007` (draft) and `...000025` (activated).

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
