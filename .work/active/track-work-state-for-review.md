---
id: track-work-state-for-review
status: completed
created: 2026-08-09
updated: 2026-08-09
---

# Goal

Make `.work/` Git-visible and retain implementation task state long enough for external reviewers to inspect plans, progress, decisions, and verification evidence.

# Governing sources

- `AGENTS.md` implementation workflow
- `.dockerignore`
- `.agents/skills/implementation-workflow/SKILL.md`
- `.cursor/rules/06-implementation-workflow.mdc`
- `.cursor/skills/implementation-workflow/SKILL.md`
- `.work/README.md`
- `docs/contributing/development-harness.md`

# Scope

## In

- Remove the ignore rule for `.work/active/*.md`.
- Define tracked review-state retention consistently across Codex, Cursor, `.work/`, and contributor documentation.
- Keep task state non-authoritative and prohibit secrets, sensitive data, and hidden reasoning.
- Validate skill parity, ignore behavior, links, and documentation.

## Out

- Commit, stage, push, or open a pull request.
- Change product or application behavior.
- Alter unrelated implementation work already present in the worktree.

# Plan

- [x] Inspect existing tracking, lifecycle, and cleanup guidance.
- [x] Update Git policy and all workflow guidance consistently.
- [x] Validate the resulting policy and retain this completed file for external review.

# Current state

The ignore rule and all identified lifecycle guidance are updated and validated. This completed task file remains Git-visible for external review.

# Decisions

- Track all files under `.work/`, including active and retained completed task files.
- Keep completed task files through external review; remove or archive them only after review concludes or the owner directs otherwise.
- Retain this task file after completion as direct evidence that the new review-handoff policy works.
- Continue excluding `.work/` from container build inputs; Docker context exclusion is independent of Git tracking.

# Findings / deviations

- `.work/active/dotnet-react-workspace-scaffold.md` already exists and became Git-visible when the ignore rule was removed; it was not modified by this task.
- `.work/active/add-composite-skills.md` used the former immediate-cleanup wording; it was aligned with the new review-retention lifecycle before being exposed for review.
- The worktree contains unrelated user changes that remain untouched.
- This task file was removed from the working tree during validation and restored because the requested policy requires retaining it for review.
- TDD does not apply to this documentation/configuration-only change; structural and documentation validation provide the strongest relevant evidence.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Git ignore behavior | passed | `git check-ignore -q` reports every `.work` policy/task file is not ignored; `git ls-files .work` lists all current files. |
| Codex/Cursor skill parity and validation | passed | `cmp` reports matching workflow skills; `quick_validate.py` reports both skill folders valid. |
| Documentation consistency search | passed | No obsolete `.work` gitignore or immediate-cleanup guidance remains in repository workflow documentation. |
| Documentation validation | passed | `python3 scripts/check_docs.py` reports documentation validation passed. |
| Diff/whitespace validation | passed | `git diff --check` and `git diff --cached --check` both pass. |
| Sensitive-content review | passed | No common credential assignment or private-key patterns were found under `.work/`. |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
