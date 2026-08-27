# Design-lab component disposition

Shared production-safe implementations live in `web/src/design-system/`. This
folder keeps lab adapters, demo controls, and domain tables that still depend
on synthetic fixtures.

## Promote (owned by `web/src/design-system/`)

| Family | Path | Notes |
| --- | --- | --- |
| Foundations | `web/src/lib/`, `web/src/styles/` | `cx`, breakpoints, announcer, format helpers, Shipboard CSS |
| Glyphs, keys, fields, feedback, plates, overlays, state, navigation, readouts, select, menu, temporal, datatable, chrome | `design-system/components/` | Generic typed props; chrome `homeTo` is required; no fixture identities |
| Table actions / selection | `design-system/patterns/` | Descriptor-driven table/bulk/row actions |

## Surface donor

| Module | Path | Notes |
| --- | --- | --- |
| Enrollment table | `EnrollmentTable.tsx`, `tableLogic.ts` | Enrollment-row rendering over shared datatable |
| Participant/admin/reviewer/session/journey routes | `../routes/`, `../features/` | Copy/adapt in Phase 8; strip fixtures |

## Lab-only

| Module | Path | Notes |
| --- | --- | --- |
| Catalog identities and sign-out | `chrome/operator.ts`, `usePrototypeSignOut.ts`, `overlays/SignOutCeremony.tsx` | Synthetic catalog home and demo sign-out |
| Demo plate | `plates/DemoPlate.tsx` | Specimen state switcher |
| Lab chrome wrappers | `chrome/Brand.tsx`, `chrome/CommandStrip.tsx` | Default `homeTo` to the channel catalog |
| Gallery | `../features/gallery/` | Component Deck specimens |
| Fixtures | `../data/` | Synthetic only |

Surfaces should import promoted modules from `web/src/design-system/` (the
`components/index.ts` barrel here re-exports them plus lab-only adapters).
