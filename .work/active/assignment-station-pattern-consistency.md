---
id: assignment-station-pattern-consistency
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Polish the Assignment Station (`/my-work/:enrollmentId`) to the Shipboard guided-task grammar. CompactId viewport clamping is already in `TooltipHost` / `useFloatingPlacement` — not reopened.

# Governing sources

- `docs/ui-ux/activity-campaign-journey.md` (`guided-task`, `IA-MVP-4`)
- `docs/ui-ux/submission-attempt.md` (`UI-SUBM-DEC-1`, `UI-SUBM-DEC-3`)
- `docs/ui-ux/design-system/components/buttons.md` (`disabledReason`)
- `docs/ui-ux/design-system/components/layouts.md`

# Scope

## In

- AssignmentSpine: `--current` only while that node is viewed
- Empty/unpermitted intake: keep **Submit version** visible, disabled with `disabledReason`
- Eligibility copy: name the closed window (`too_early`)
- Record labels: `enrollmentStatusCopy`
- Narrow guided-task foot: fixed to the viewport floor (body is the scroller at ≤1080px; sticky cannot pin a last-child foot)

## Out

- CompactId / TooltipHost placement (fixed externally)
- Start Attempt API
- Home assignment plates (still show raw `active` on registry plates)

# Plan

- [x] Red tests (spine, submit reason, eligibility, fixed CSS)
- [x] Green implementation
- [x] Playwright Assignment station (desktop + 390, Attempt, disabled Submit plaque)

# Current state

Completed. CompactId left-clip remains owned by shared floating placement.

# Decisions

- Empty intake cannot finalize in the UI even if the server lists `finalize_intake` (**interim default**: require at least one received item). Rationale: `UI-SUBM-DEC-3` is a material commit, not an empty receipt.
- `too_early` with an open intake, or with Begin intake still permitted, means Attempt start has not opened. `too_early` with neither means the Submission window has not opened. Rationale: live seed permits Begin intake while eligibility is `too_early`; “Submission window has not opened” would contradict the primary key.

# Findings / deviations

- First sticky attempt failed: at ≤1080 the page scroller is `body`, so a last-child `position: sticky` never docks. Switched to `position: fixed` plus bay padding so the well can scroll under a hull foot.
- Review pass: list plates use `enrollmentStatusCopy`; empty intake phase is **Intake receiving** until an item exists; Submit version keeps accessible name `Submit version` with the block in `disabledReason`.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Vitest | pass | Station, list, spine, eligibility, layouts |
| Playwright review | pass | List Active `.playwright-mcp/page-2026-08-30T10-58-20-524Z.png`. Station CompactId `.playwright-mcp/page-2026-08-30T10-57-32-661Z.png`. Narrow Begin foot `.playwright-mcp/page-2026-08-30T10-58-51-425Z.png`. Intake/confirm/cancel exercised on Classification exercise; CompactId plaque in viewport. |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
