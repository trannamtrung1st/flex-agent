---
id: assignment-station-guided-task
status: completed
created: 2026-08-29
updated: 2026-08-29
---

# Goal

Rebuild production `/my-work/:enrollmentId` as a Shipboard assignment station:
`guided-task` hull, instrument rail, 2-node spine, one work well, persistent
footer actions. Keep existing Submission intake contracts and honest Attempt
unavailable. Do not copy lab Status Bays, Examination/Result phases, or
fixtures.

# Governing sources

- Confirmed shape brief (2026-08-29): assignment station, not Status Bays
- `docs/ui-ux/activity-campaign-journey.md` — `/my-work/:enrollmentId` is `guided-task`; `JRN-MVP-3`
- `docs/ui-ux/submission-attempt.md` — assignment reading order, independent tracks
- `docs/ui-ux/design-system/components/layouts.md` — `guided-task` slots
- `docs/ui-ux/design-system/components/sidebars.md` — assignment instrument rail
- ADR-019, ADR-021 `FE-RESET-2` / `PC-14`
- Design-lab `JourneyPage` as visual donor only

# Scope

## In

- Route layout assignment `guided-task` for `/my-work/:enrollmentId`
- Production shell does not wrap that route in `ManagementLayout`
- Assignment station chrome: rail brand, My work return, operator profile
- Identity/timing instruments + Submission / Attempt spine
- One well at a time; Attempt inspectable and unavailable
- Footer owns Begin intake / Submit version / Cancel intake
- Loading in the station hull; unavailable is a ceremony well + return

## Out

- My work index, Home, Status Bays
- Session / Review / Result / Release
- Lab fixtures, demo plate, protocol plate, briefing ack
- Start Attempt control

# Plan

- [x] Red: layout assignment, shell, spine, page tests
- [x] Green: shell branch, station layout, page rebuild, station CSS
- [x] Focused tests
- [x] Playwright MCP: populated assignment desktop + narrow, Attempt view, intake, confirm

# Current state

Completed. Production assignment locator is a guided-task station. My work
index remains management.

# Decisions

- Spine view is local UI state, default Submission
- Attempt history omitted until the host returns it
- Wordmark returns to Home; rail link returns to My work
- Denied deep-link on this locator uses the station hull (parent layout is guided-task)
- Assignment `h1` is the Task title; campaign is the meta line (not repeated in both)
- Heading Phase names status and the permitted next action in words; footer owns the key

# Findings / deviations

- Live demo assignment opened with intake already receiving and eligibility
  `too_early`. Footer showed Cancel intake until a direct-text item was added,
  then Submit version appeared. That is host permission, not a station bug.
- Browser verification opened the confirm dialog and cancelled it. Finalize
  to an accepted version was not executed live; unit tests cover the mutation.
- Loading wait in the well was too fast to capture as a screenshot.
- Begin intake was not live-verified because the demo enrollment already had an
  open intake.

- Polish pass (2026-08-29): human copy for eligibility/accommodation/byte
  limits; CompactId on Enrollment; received intake items listed; attachment
  hint no longer looks like a filled filename; unavailable well no longer
  duplicates the heading; Attempt spine short reads “Not available here”.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused unit tests (`src/pages`, `src/components/shell`, `src/components/work`, `src/router`, `src/App.test.tsx`) | pass | 79 passed |
| Impeccable detector | pass | `[]` |
| Playwright MCP polish desktop Submission | pass | `.playwright-mcp/page-2026-08-29T08-24-52-827Z.png` |
| Playwright MCP polish desktop Attempt | pass | `.playwright-mcp/page-2026-08-29T08-26-56-751Z.png` |
| Playwright MCP polish narrow | pass | `.playwright-mcp/page-2026-08-29T08-25-57-194Z.png` |
| Playwright MCP polish unavailable | pass | `.playwright-mcp/page-2026-08-29T08-24-26-996Z.png` |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
