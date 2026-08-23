---
id: p0-enrollment-shared-request-quota
status: planned
created: 2026-08-23
updated: 2026-08-23
---

# Goal

Own replica-independent per-actor/Organization Enrollment request limits after
`PROP-8` / Proposed ADR-018 is decided. If those records are approved as
written, the first Enrollment slice no longer owns this contract and this task
is the implementation home. If owners instead require the quota on the first
slice, cancel this task and implement there. If they reject shared quota
entirely, record that approved disposition here and close without a store.

# Governing sources

- `docs/requirements/features/submission-attempts.md` — `Q-8` / `PROP-8`
  (Proposed; not approved)
- `docs/architecture/decisions/ADR-006-mvp-architecture-baseline-and-evolution.md`
  — coarse gateway limits; optional cache may later hold bounded rate counters
- `docs/architecture/decisions/ADR-018-enrollment-request-limit-scope.md`
  — Proposed Enrollment request-limit scope
- `.work/active/p0-enrollment-assignment-discovery.md` — replica-local limiter
  already shipped as defense in depth

# Scope

## In

- After an approved decision: a replica-independent admission control for
  authenticated Enrollment reads and mutations keyed without protected labels,
  or the documented approved rejection of that work.
- Negative tests that two API processes cannot silently double the approved
  permit budget when a shared quota is required.

## Out

- Changing Enrollment assignment, lifecycle, or My work product behavior
- Putting actor, Organization, Enrollment, or Participant identifiers in
  metrics or gateway labels
- Selecting Redis or another store before ADR-018 / `PROP-8` is decided

# Plan

- [ ] Wait for owner acceptance or rejection of `PROP-8` / Proposed ADR-018
- [ ] If approved as written, design the shared store or gateway surface here
- [ ] If rejected because the first slice must own the quota, cancel this task
- [ ] If rejected because no shared quota is wanted, record that disposition
      and close without a store

# Current state

Planned and blocked on the `PROP-8` / ADR-018 decision. The first Enrollment
slice remains **in-progress** until that decision and the broader independent
review are resolved. Do not treat this file as an approved product contract.

# Decisions

None approved. Interim default: keep the replica-local limiter until decided.

# Findings / deviations

- Review of `2ae4cb7` required either implementing shared/gateway quota or
  an explicit spec/ADR move. This task is that move's implementation home.
- Review of `08269b6` approved that bookkeeping. `PROP-8` / ADR-018 are
  still not an authorized disposition.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `PROP-8` / ADR-018 decided | pending | |

# Blockers

- `PROP-8` and Proposed ADR-018 are not approved.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [ ] Governing specifications were rechecked
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
