---
id: enrollment-registry-page-size-cursor
status: completed
created: 2026-09-02
updated: 2026-09-02
---

# Goal

Stop the Participants registry from reusing a signed cursor after rows-per-page changes (same stale-cursor race already fixed on the Assign picker).

# Governing sources

- Review of `f732516` (outer registry page-size vs cursor)
- `docs/ui-ux/design-system/components/pagination.md` (signed-cursor tables, `UI-SUBM-DEC-13`)
- Assign picker reset in `ProductionEnrollmentPage.tsx`

# Scope

## In

- Immediate waiting + cursor/hasMore/stack invalidation when `enrollmentPageSize` changes
- Reload first page at the new limit
- Deferred-response test: 16 → 32, Next disabled, old cursor never sent with `limit=32`

## Out

- Numbered Activities paging
- Server cursor signing (page size remains unsigned)

# Plan

- [x] Red: enrollment page-size deferred-response test
- [x] Green: mirror Assign picker reset + generation on registry loads
- [x] Focused tests and proportionate browser check

# Current state

Registry page-size change now matches the Assign picker: render-time waiting, cursor/hasMore/stack cleared, first page reloaded, generation ignores stale Next responses.

# Decisions

- Reuse the candidate render-time reset and generation counter on the enrollment list.

# Findings / deviations

- Live UI fetch is too fast to observe the pending Next-disabled frame; the deferred unit test is the race evidence.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Red: Next still enabled after 16→32 with deferred first page | pass | vitest fail `toBeDisabled()` |
| Green: `ProductionEnrollmentPage` tests | pass | 17 passed |
| Browser: 16 then 32 first page, Next disabled when the new page is complete | pass | Local MCP screenshots (not committed). Durable race: `ProductionEnrollmentPage` “does not reuse the previous cursor while a new Participants page size is pending” |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
