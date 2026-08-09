---
id: add-composite-skills
status: completed
created: 2026-08-09
updated: 2026-08-09
---

# Goal

Add repository-local `developer` and `reviewer` skills that compose the existing specialist implementation and review roles for Codex and Cursor.

# Governing sources

- `AGENTS.md` role routing and implementation workflow
- `.cursor/rules/02-role-routing.mdc`
- `docs/contributing/development-harness.md`
- Existing specialist skills under `.agents/skills/` and `.cursor/skills/`
- System `skill-creator` guidance

# Scope

## In

- Create `developer` as a coordinator for `backend-developer` and `frontend-developer`.
- Create `reviewer` as a coordinator for all installed `*-reviewer` roles.
- Keep Codex and Cursor skill catalogs and role-routing documentation aligned.
- Validate skill structure, links, and Markdown.

## Out

- Change specialist role responsibilities.
- Change product behavior or application code.
- Commit or push changes.

# Plan

- [x] Initialize and author the two composite skills.
- [x] Mirror the skills and update role-routing documentation.
- [x] Run structural and repository documentation validation.
- [x] Reconcile changes and prepare this task file for external review.

# Current state

The composite skills and routing documentation are implemented and validated. This completed task file is retained for external review under the current `.work/` tracking policy.

# Decisions

- `developer` composes only `backend-developer` and `frontend-developer`, matching the user request.
- `reviewer` composes the installed reviewer roles: `backend-reviewer`, `frontend-reviewer`, and `security-privacy-reviewer`; `tester` remains a separate QA role.
- Composite skills coordinate specialists and preserve their individual requirements rather than duplicating their full checklists.

# Findings / deviations

- The worktree contains unrelated user changes; they will not be modified.
- TDD is not meaningful for this documentation/configuration-only change; skill and documentation validators provide the strongest applicable checks.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Skill initialization and quick validation | passed | `quick_validate.py` reports all four skill folders valid. |
| Codex/Cursor skill parity | passed | `cmp` reports both pairs of `SKILL.md` files match. |
| Documentation validation | passed | `python3 scripts/check_docs.py` reports documentation validation passed. |
| Markdown lint or focused equivalent | partial | `git diff --check` passed; `markdownlint-cli2` is not installed or cached locally, so CI lint was not run. |
| OpenAI metadata validation | passed | Both `agents/openai.yaml` files parse and satisfy required interface constraints. |
| Composite dependency check | passed | Every referenced specialist skill exists in both catalogs. |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
