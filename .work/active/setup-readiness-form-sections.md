---
id: setup-readiness-form-sections
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Seat assessment Setup and readiness on the specified FormSection hierarchy, replace the memory paragraph with a field, and align Create/activate commit keys with ceremony large size.

# Governing sources

- `docs/ui-ux/assessment-campaign-setup.md` — setup page hierarchy, `UI-ACT-DEC-1`
- `docs/requirements/features/assessment-setup.md` — inherited vs editable; save still title-only
- `docs/ui-ux/design-system/components/inputs.md` — FormSection, frozen fields
- `docs/ui-ux/design-system/components/buttons.md` — ceremony large keys

# Scope

## In

- Setup FormSections for the seven specified clusters using Activity detail already on `AssessmentSetupView`
- Frozen resolved source/timing/memory/cohort fields (not disabled, not new authoring)
- Memory as FormField inside Memory and capabilities
- Create and Activate ceremony keys `size="large"`; drop local activate min-width
- Assign Participants (activated primary) also `size="large"`
- Component Deck management-setup specimen matches the same sections

## Out

- Saving source, timing, or memory from Setup (API remains title-only)
- Campaign Configuration dialog as a production station
- Source display names on Setup (API still exposes ids; captions use truncated revision ids)

# Plan

- [x] Red: setup section groups, frozen memory field, Create large
- [x] Green: station sections, create/activate size, CSS cleanup, gallery specimen
- [x] Tests + detector + live evidence

# Current state

Completed. Production Setup on candidate `:5274` shows seven FormSections, frozen Memory copy, and 44px ceremony primaries.

# Decisions

- Sources, timing, memory, and cohort are frozen `FieldInput`s. Shared provenance is one **Resolved from this Activity revision** Note (`Alert` info); superseded per-field hints in `.work/active/setup-resolved-note-alert.md`. Editable source selection stays on Create until save supports it.
- Unbound values use **Not bound** (sources) or **Not seated** (timing/cohort facts).
- Activated-cohort primary **Assign Participants** uses the same large ceremony size as Activate.

# Findings / deviations

- Repeating per-field resolved hint was the original landing; later replaced by one station Note (`setup-resolved-note-alert`).
- Bound source fields show truncated source/version ids (`sourceRevisionCaption`), not human titles, because Setup view maps those ids.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused Vitest | pass | `AssessmentSetupPage.test.tsx` (11), create + setupStation + keys earlier; gallery-deck 27 |
| Detect | pass | impeccable `detect.mjs` on SetupCeremonyStation `[]` |
| Playwright desktop Setup | pass | `.playwright-mcp/page-2026-08-30T08-09-38-817Z.png` (sections), `08-12-11-287Z.png` (Memory + 44px Assign) |
| Playwright Deck setup | pass | `.playwright-mcp/page-2026-08-30T08-08-31-081Z.png` |
| Playwright Create | pass | Create `key--transmit key--large` 44px; live Create page at `/activities/new` |
| Playwright narrow Setup | unverified | Viewport resize kept switching Playwright to other tabs; desktop + tests cover stacked FormSections |

# Blockers

None for the in-scope Setup form. Canonical `:18080` SPA may still lag `web/` if RedirectUri was pointed at the candidate overlay.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass (focused Vitest + live candidate Setup/Create)
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
