---
id: design-lab-admin-console-no-home
status: completed
created: 2026-08-27
updated: 2026-08-27
---

# Goal

Remove the redundant **Home** token from the design-lab administrator CommandStrip so area navigation lives only in the gangway (desktop) and drawer Menu (narrow).

# Governing sources

- `docs/ui-ux/design-system/foundation/layout.md` — management shell: command strip + optional gangway
- `docs/ui-ux/design-system/components/sidebars.md` — gangway as persistent area navigation
- `docs/ui-ux/activity-campaign-journey.md` — product Home remains a participant/operator work destination; this change does not implement or remove that surface
- ADR-020 — design-lab isolation

# Scope

## In

- Design-lab admin console chrome: stop rendering CommandStrip `Home`
- Tests and Playwright evidence for desktop and drawer widths

## Out

- Participant Home / Status Bays
- Production app shell destinations
- Product IA Home dashboard for administrators (not present as a plate in this console)

# Plan

- [x] Red: assert admin console has no Primary `Home` nav and still exposes gangway/drawer areas
- [x] Green: omit CommandStrip `nav` on `AdminPage`; remove unused `administratorHome`
- [x] Browser verify desktop and narrow; detector once

# Current state

Complete. Admin command strip is brand + ident only; gangway/drawer owns area navigation.

# Decisions

- **Interim default:** administrator area navigation is the gangway/bulkhead only. CommandStrip keeps brand (channel index), Admin suffix, and operator ident. Index `/admin-console` still redirects to Enrollments. Product IA Home (`IA-MVP-1`) is not a separate admin plate in this design-lab console.

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| focused design-lab unit test | pass | `pnpm test:design-lab -- src/design-lab/pc-surfaces.test.tsx` — 28 passed |
| Playwright MCP screenshots | pass | `.playwright-mcp/page-2026-08-27T09-07-53-199Z.png` desktop; `.playwright-mcp/page-2026-08-27T09-08-18-061Z.png` narrow; `.playwright-mcp/page-2026-08-27T09-08-43-508Z.png` drawer. Drawer Campaigns navigation reached `/admin-console/campaigns`. |
| impeccable detect.mjs | pass | empty findings `[]` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
