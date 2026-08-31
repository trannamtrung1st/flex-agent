---
id: workspace-toast-advisory
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Mount production toasts and apply the design-system feedback rungs consistently: toast for transient mutation receipts, OperateArea advisory for standing page conditions, Alert for blocking or still-true work outcomes.

# Governing sources

- `docs/ui-ux/design-system/components/alerts.md`
- `docs/ui-ux/design-system/components/layouts.md`
- Lab donor: `web/src/design-lab/routes/AdminPage.tsx` (`ToastHost`)

# Scope

## In

- Production `ToastHost` on `ProductionAppShell` (management and guided-task); default dock `top-center` (was `bottom-center`; see toast default change Aug 2026)
- Enrollment assign receipt → toast; assign/update errors stay Alert in the work body
- Activities missing sources → OperateArea `advisory`
- Setup draft save receipt → toast; cohort activated / provenance Alerts unchanged
- Enrollment lifecycle and accommodation success → toast
- Submission begin-intake and submit-version success → toast
- Docs rule clarification in alerts/layouts

## Out

- Replacing danger Alerts or Setup **Cohort activated** with toasts
- Session `no_action` / Result-Release success toasts
- Design-lab AdminPage now mounts `ToastHost` (same host as production). Component Deck still uses local `useToasts` for specimens.

# Plan

- [x] Inspect production feedback call sites
- [x] Red: ToastHost + enrollment/activities/setup/my-work assertions
- [x] Green: host, page wiring, copy helpers
- [x] Docs
- [x] Focused tests + Playwright evidence

# Current state

Completed. Production and design-lab Admin shells mount `ToastHost`. Receipts use toast; standing registry/source conditions use OperateArea advisory; blocking errors stay in-body Alert.

Specs aligned 2026-08-30: `UI-SUBM-DEC-13`/`14`, `JRN-MVP-2`, Setup save-draft success copy, alerts/implementation-guide, gallery notes.

# Decisions

- `ToastHost` lives in the design-system overlay module so lab and production share one dock owner.
- `usePushToast` is a no-op outside the host so page tests that do not assert receipts stay valid; tests that assert toasts wrap `ToastHost`.

# Findings / deviations

- Enrollment “more pages remain” is a standing condition, so it uses OperateArea advisory rather than Alert-in-context.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused Vitest (8 files, 61 tests) + `tsc -b --noEmit` | passed | ToastHost, enrollment, activities, setup, my-work, shell, enrollment detail |
| Impeccable detect.mjs | passed | `[]` on changed TSX |
| Spec/doc alignment (DEC-13/14, journeys, setup save, alerts, Admin `ToastHost`) | passed | Focused Vitest 7 files / 60 tests + gallery 28 tests |
| Playwright `:5274` | passed (earlier) | Toast-dock on My work; Begin intake `.playwright-mcp/page-2026-08-30T11-06-08-205Z.png`. Compose `:18080` SPA not recaptured. Admin assign/advisory need administrator session. |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
