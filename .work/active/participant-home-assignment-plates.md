---
id: participant-home-assignment-plates
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Polish Participant Home so production `/` lists current assignments (not a My work door), Status Bays reuse `AssignmentPlate`, copy matches the Participant job, and design-system docs record the interim.

# Governing sources

- `docs/ui-ux/activity-campaign-journey.md` (`IA-MVP-1`, `PROP-UX-4`, Home vs `/my-work`)
- `docs/ui-ux/design-system/components/cards.md` (`AssignmentPlate`, Status Bays hull)
- `docs/ui-ux/design-system/components/layout-primitives.md` (Status Bays are not `Grid`)
- `PC-09`, `PC-11`, `PC-14`

# Scope

## In

- Production Home: when `my-work` is available, load `listMyWork` and render assignment plates; omit the My work destination plate; Participant copy
- Shared assignment plate + My work list hook
- Design-lab Status Bays: `AssignmentPlate`; drop local `.plate` CSS twin; quieter bay-head
- Docs: IA interim, cards, layout, change-record, regenerate `DESIGN.md`

## Out

- Full `IA-MVP-1` bands 1/3–6 Home-work feed
- Production Status Bays 4-column hull
- Live Session continuity on Home without a continuity API

# Plan

- [x] Red: Home participant roster tests; lab Status Bays `AssignmentPlate`
- [x] Green: hook, plates, CSS, lab HomePage, docs
- [x] Tests, typecheck, live lab + production participant Home
- [x] Impeccable detect on changed UI

# Current state

Completed. Participant Home lists My work assignments. Status Bays use `AssignmentPlate`. Docs record the IA-MVP-1 interim.

# Decisions

- Do not invent a Home-work API. Reuse `listMyWork` when `my-work` is available.
- Keep Status Bays as domain 4-column CSS; only the enrollment plates promote to `AssignmentPlate`.
- Production still does not clone Status Bays (same as My work).
- Lab deadlines use `InstantReadout` plus a visually hidden IANA zone (`PC-11`).

# Findings / deviations

- `IA-MVP-1` bands 1 and 3–6 remain unimplemented.
- Superseded for production Home roster: `.work/active/unify-participant-home-my-work.md` redirects `/` to `/my-work`. Status Bays `AssignmentPlate` reuse still stands.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused vitest Home, My work, style-entry | pass | 24 tests |
| Design-lab gallery + pc-surfaces | pass | 30 tests |
| Typecheck + typecheck:design-lab | pass | tsc |
| Live lab Status Bays | pass | `.playwright-mcp/page-2026-08-30T08-29-11-744Z.png` desktop; `.playwright-mcp/page-2026-08-30T08-29-40-114Z.png` narrow; empty `.playwright-mcp/page-2026-08-30T08-30-45-420Z.png` |
| Live production Participant Home | pass | `.playwright-mcp/page-2026-08-30T08-32-19-096Z.png` desktop; `.playwright-mcp/page-2026-08-30T08-32-44-386Z.png` narrow — current assignments, no My work door plate |
| `detect.mjs` | pass | `[]` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
