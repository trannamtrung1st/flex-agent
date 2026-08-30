---
id: ceremony-unavailable-auth-commit
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Route the production sign-in gate (idle and denied) and access-changed recovery through `CeremonyUnavailable`, with an explicit transmit recovery for **Continue to sign in**.

# Governing sources

- `docs/ui-ux/design-system/components/layouts.md` (ceremony / unavailable)
- `docs/ui-ux/design-system/components/cards.md`
- `docs/ui-ux/design-system/components/buttons.md`
- `docs/ui-ux/design-system/product/empty-loading.md`
- `docs/ui-ux/activity-campaign-journey.md` (sign-in fail-closed)

# Scope

## In

- `CeremonyRecovery.variant` `quiet` (default) or `transmit` (large auth commit)
- Production auth gate and `AccessChangedScreen`
- Deck ceremony specimen + DS copy so the exception is documented

## Out

- Quiet Return/Reload recovery
- Signing-out wait/retry plane
- Visual redesign of Shipboard ceremony

# Plan

- [x] Red: `CeremonyUnavailable` transmit recovery test
- [x] Green: helper + auth/access-changed consumers
- [x] Docs + gallery specimen
- [x] Verify: Vitest + candidate Vite screenshots

# Current state

Completed. Auth and access-changed planes use `CeremonyUnavailable` with `recovery.variant="transmit"`. Return/Reload stays quiet.

# Decisions

- Transmit recovery is the only commit skin on this helper; `open` stays off ceremony recovery.
- Transmit recovery uses `size="large"` to match the existing auth gate.

# Findings / deviations

- Signing-out error still assembles `CeremonyArea` + `CeremonyEmpty` + quiet retry (loading plane, out of scope).
- Keyboard focus screenshot skipped after Playwright tab switched to the Deck; structure verified by snapshot.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Vitest ceremony + App | passed | `CeremonyArea.test.tsx` + `App.test.tsx` (12 tests) |
| Vitest gallery-deck | passed | 26 tests including transmit specimen |
| `tsc -b --noEmit` | passed | |
| Impeccable detect | passed | `[]` on changed UI files |
| Playwright denied desktop/narrow | passed | Origin `http://localhost:5274`; `.playwright-mcp/page-2026-08-30T07-46-19-512Z.png`, `page-2026-08-30T07-47-00-149Z.png`; heading danger, alert copy, `key--transmit key--large` |
| Playwright idle gate | passed | `page-2026-08-30T07-47-31-383Z.png` teal title, same transmit key |
| Deck ceremony specimen | passed | `http://localhost:5275/design-lab/shared/gallery#layout-management-ceremony` `page-2026-08-30T07-48-05-729Z.png` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
