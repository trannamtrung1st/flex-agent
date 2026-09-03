# Design-lab component disposition

Shared production-safe implementations live in `web/src/design-system/`. This
folder keeps lab adapters, demo controls, and domain tables that still depend
on synthetic fixtures.

## Folder layout

| Path | Role |
| --- | --- |
| `chrome/` | Lab chrome adapters (catalog home defaults, synthetic operator identities) |
| `enrollment/` | Fixture-backed enrollment manifest table and sort/filter helpers |
| `operate/` | Lab operate hosts. Walls, home board, and reviewer queue/ledger wrap `OperateAreaHost` (`CampaignsOperateArea`, `EnrollmentWallOperateArea`, `SampleWallOperateArea`, `HomeBoardOperateArea`, `ReviewerQueueOperateArea`, `ReviewerLedgerOperateArea`). Deck form recipes use `FormRecipeOperateArea` (`OperateArea` plus owned `form-recipe`). Lab registry hug class: `registryHugClassName.ts`. |
| `layouts/` | Thin lab adapter over production `LiveSessionLayout` (catalog default `homeTo`) |
| `overlays/` | Lab-only overlay ceremonies (`CampaignCeremonyDialog`, `CampaignCeremonyPlate`, `FormRecipeDialog`, `SignOutCeremony`) |
| `plates/` | Lab plate adapters (`DemoPlate`, `StatusBays`, `ProtocolPlate`, `FrozenLine`, `InPlateHost`, `WorkWellReleasedSeal`) plus re-exports of assignment chrome from `web/src/components/work/` |
| `state/` | Lab-only marks (`ActivationMark`, `RecordSeal`, `recordResultMark`); `StageBars` re-exported from `web/src/components/work/SessionMarks.tsx` |
| `datatable/` | Expand-row shell and enrollment/Deck detail interiors (not on the production barrel) |
| `index.ts` | Re-exports design-system primitives plus lab adapters for routes |

Import domain modules from their folder barrels (`../components/enrollment`,
`../components/operate`, …), not from loose files at this level.

## Promote (owned by `web/src/design-system/`)

| Family | Path | Notes |
| --- | --- | --- |
| Foundations | `web/src/lib/`, `web/src/styles/` | `cx`, breakpoints, announcer, format helpers, Shipboard CSS |
| Glyphs, keys, fields, feedback, plates, overlays, state, navigation, readouts, select, menu, temporal, datatable, chrome | `design-system/components/` | Generic typed props; chrome `homeTo` is required; no fixture identities. `CompactId` is the shared truncated-identifier readout. Datatable expand chrome is not in this tree — it lives in `design-lab/components/datatable/`. Assignment heading/bays/plates/record marks, `SetupOperateArea`, setup ceremony shells, and `AcknowledgmentGate` are not in this tree — they live in `web/src/components/work/`. `OperateAreaHost` is wrapper-only (not on the production barrel). |
| Table actions / selection | `design-system/patterns/` | Descriptor-driven table/bulk/row actions |

## Surface donor

| Module | Path | Notes |
| --- | --- | --- |
| Enrollment table | `enrollment/EnrollmentTable.tsx`, `enrollment/tableLogic.ts` | Enrollment-row rendering over shared datatable; export name `DataTable` is lab-local |
| Participant/admin/reviewer/session/journey routes | `../routes/`, `../features/` | Copy/adapt in Phase 8; strip fixtures |

## Lab-only

| Module | Path | Notes |
| --- | --- | --- |
| Catalog identities and sign-out | `chrome/operator.ts`, `usePrototypeSignOut.ts`, `overlays/SignOutCeremony.tsx` | Synthetic catalog home and demo sign-out |
| Demo plate | `plates/DemoPlate.tsx` | Specimen state switcher |
| Status Bays / protocol ident / frozen line / in-plate host / form-recipe dialog / campaign ceremony overlay / activation mark / session marks / enrollment result mark / form pair / datatable expand | `plates/StatusBays.tsx`, `plates/ProtocolPlate.tsx`, `plates/FrozenLine.tsx`, `plates/InPlateHost.tsx`, `plates/WorkWellReleasedSeal.tsx`, `overlays/FormRecipeDialog.tsx`, `overlays/CampaignCeremonyDialog.tsx`, `overlays/CampaignCeremonyPlate.tsx` (`CampaignCeremonyConfigGrid`), `state/ActivationMark.tsx`, `state/SessionMarks.tsx` (`RecordSeal`; `StageBars` from `components/work`), `state/recordResultMark.tsx`, `fields/FormPair.tsx` (`FormPair`, `FormPairField`), `datatable/` (`DatatableDetailShell`, `DatatableDetailContent`, gutter hook) | Lab-only class owners except shared `StageBars`. Briefing acknowledgement is `web/src/components/work/AcknowledgmentGate.tsx`. Product field copy is `web/src/content/fieldCopy.ts`, not the design-system barrel. |
| Lab chrome wrappers | `chrome/Brand.tsx`, `chrome/CommandStrip.tsx` | Default `homeTo` to the channel catalog |
| Gallery | `../features/gallery/` | Component Deck specimens |
| Fixtures | `../data/` | Synthetic only |

Surfaces import generic primitives from `web/src/design-system/`. Assignment
chrome imports from `web/src/components/work/`. Lab hosts and lab-only plates
import from the folder barrels (`../components/operate`, `../components/plates`,
`../components/overlays`). The `components/index.ts` barrel re-exports both.
