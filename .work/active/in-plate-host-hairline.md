---
id: in-plate-host-hairline
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Close remaining plate-foot consistency gaps: lab fused readout+foot wells use the same full-bleed recipe as Setup/Create, and guard the overlay viewport measure against a duplicate binding that broke candidate HMR.

# Governing sources

- `docs/ui-ux/design-system/components/cards.md` — in-plate `PlateFoot` rule full-bleed to the bezel

# Scope

## In

- Shared `.in-plate-host` with existing `.setup-ceremony` bleed CSS
- Lab `CampaignsArea` and `PoliciesArea` hosts
- Overlay source contract: one `visualViewport` binding in `useFloatingPlacement`

## Out

- Compact guided-task stacking
- Horizon / FormSection / ReadoutGrid row hairlines
- Completing OIDC on `:5274` after Compose restart (Sign in required is expected)

# Plan

- [x] Red: CSS + overlay source contracts; lab host class assertion
- [x] Green: plates.css, lab stacks, overlay rename
- [x] Verify: Vitest + Playwright lab campaign record + candidate compile

# Current state

Completed.

# Decisions

- Generic host class `.in-plate-host` shares the Setup/Create bleed recipe so lab fused plates do not get a one-off hatch.
- Renamed `visual` to `visualViewport` in `useFloatingPlacement` so HMR cannot collide on a short binding, with a source contract test.

# Findings / deviations

- Disk already had a single viewport binding; the prior candidate overlay was a stale transform. After rename, `:5274` serves the app (Sign in required), not `PARSE_ERROR`.
- Assignment-station hull foot was not re-opened: Compose restart dropped the participant session.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused Vitest | pass | `EtchedFrame.test.tsx`, `overlayPortalRoot.test.ts`; design-lab `pc-surfaces` 22 |
| Playwright lab campaign | pass | Foot vs `.frame-in` width/left delta 0; keys 24px desktop / 16px compact. `.playwright-mcp/page-2026-08-30T10-38-23-940Z.png`, `.playwright-mcp/page-2026-08-30T10-38-55-729Z.png` |
| Candidate Vite overlay | pass | `parseError: false` on `/my-work`; Sign in required. `.playwright-mcp/page-2026-08-30T10-41-30-684Z.png` |
| Detector | pass | empty |

# Blockers

None remaining for in-scope work.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
