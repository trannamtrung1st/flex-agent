---
id: create-assessment-campaign-commission
status: completed
created: 2026-08-29
updated: 2026-08-29
---

# Goal

Rebuild the admin Create assessment Campaign page to the locked commissioning-plate composition: nameplate title, Agent and Harness as primary berths, the other eight as an etched source strip, reserved Create, product-language labels.

# Governing sources

- `docs/ui-ux/assessment-campaign-setup.md` — create entry, form/type, source selection, missing category, access-loss
- `docs/ui-ux/activity-campaign-journey.md` — `JRN-MVP-1`
- `docs/ui-ux/design-system/` — OperateArea, WorkWell, FormField, keys, plates
- Confirmed Impeccable shape brief: ceremony plate + berth rack + reserved Create

# Scope

## In

- `/activities/new` create page composition, labels, source option captions
- Shared category/option presentation used by the create surface
- Activities missing-category copy when it names a source category

## Out

- Setup and readiness layout
- Create contract changes (all ten categories still required)
- Source authoring

# Plan

- [x] Shape brief locked (fused commissioning plate)
- [x] Red: presentation helpers and create-page structure tests
- [x] Green: page, labels, berth layout, reserved Create, widened record
- [x] Focused tests and typecheck of this surface
- [x] Playwright MCP desktop, validation, and narrow on candidate origin
- [x] Impeccable detector on changed targets
- [x] Align create plate with Setup: DS DropdownSelect, single frame inset, hugging PlateFoot Create
- [x] Review: reserved Create foot so validation does not push the commit key off-screen

# Current state

Create matches Setup’s operate plate: one etched-frame inset, DS fields and DropdownSelect, hugging trailing Create. Repo-wide `tsc -b` still reports unrelated existing errors in OperateHead and Setup tests.

# Decisions

- Native `<select>` stays inside berth chrome so options remain in the document and popovers are not clipped by the scrolling well.
- UUID `version_id` values use an 8-character caption; `assessment.` / `.vN` stripped from source kind.
- Eligibility mark only for development-only bindings, not on every available berth.
- Nested `WorkWell` inside `EtchedFrame` was double-padding; Create now uses the same OperateArea + `PlateFoot` recipe as Setup.
- Full-width Create was a record-plane override, not the key spec; Create now hugs on the trailing plate foot.
- Fields scroll in `.create-ceremony__scroll`; Create stays in `PlateFoot` so the commit key remains in the frame.

# Findings / deviations

- None vs the locked brief. Setup page was not redesigned.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused unit tests | passed | 32 tests: create page, Activities, presentation, schema |
| Playwright desktop | passed | `.playwright-mcp/page-2026-08-29T13-29-44-759Z.png` |
| Playwright validation | passed | `.playwright-mcp/page-2026-08-29T13-26-02-225Z.png` (pre-foot pin); reserved foot in code + tests |
| Playwright create → setup | passed | earlier this session `/activities/01a04daf-7caa-7c6a-a447-56ae6762188c/setup` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
