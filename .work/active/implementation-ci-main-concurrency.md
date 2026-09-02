---
id: implementation-ci-main-concurrency
status: completed
created: 2026-09-02
updated: 2026-09-02
---

# Goal

Every `main` push runs the full Implementation job set. Pull requests may still skip when the PR diff has no implementation paths. Retire the approved Enrollment pagination task.

# Governing sources

- Review after `1eb4bc5` Implementation went green
- `.github/workflows/implementation.yml`
- `build/scripts/detect-implementation-changes.sh`

# Scope

## In

- Push (non-PR) events always `implementation=true`
- PR events keep path-based skipping
- Delete `.work/active/enrollment-registry-page-size-cursor.md`

## Out

- Architecture-certification concurrency
- Walking back to last verified implementation SHA (unnecessary if main always runs full gates)

# Plan

- [x] Red: empty-diff `push` must not skip gates
- [x] Green: skip path filter unless `pull_request`
- [x] Document in `workspace.md`; focused script + docs checks

# Current state

Enrollment pagination task retired. Detector emits `true` for every non-PR event, including docs-only `main` pushes. Path skipping remains PR-only. Main still does not cancel in-progress Implementation runs.

# Decisions

- Full gates on every `main` push. Path skipping stays PR-only.

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Empty-diff `EVENT_NAME=push` → true | pass | `detect-implementation-changes.test.sh` event policy |
| Empty-diff `EVENT_NAME=pull_request` → false | pass | same |
| `check_docs.py` | pass | Documentation validation passed |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
