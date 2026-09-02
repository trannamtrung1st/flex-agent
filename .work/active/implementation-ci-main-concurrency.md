---
id: implementation-ci-main-concurrency
status: completed
created: 2026-09-02
updated: 2026-09-02
---

# Goal

Stop a docs-only `main` push from cancelling an in-flight Implementation run and then reporting green with every gate skipped. Reconcile screenshot-evidence citations with Git.

# Governing sources

- Review of `ba9fad7` / `21c3bb5` CI cancellation/skip interaction
- `.github/workflows/implementation.yml`
- `build/scripts/detect-implementation-changes.sh`
- Playwright MCP evidence rules in `AGENTS.md` and `docs/contributing/development-harness.md`

# Scope

## In

- `cancel-in-progress` only for pull requests
- Document the policy
- Stop `.work/` from citing deleted PNG paths; clarify optional Git retention of screenshots

## Out

- Change-detector walk-back to last verified implementation SHA
- Architecture-certification concurrency (scheduled, not this hole)

# Plan

- [x] Fix Implementation concurrency and document it
- [x] Align screenshot-evidence wording and enrollment task citations
- [x] Run path-classifier and docs checks; push so full Implementation runs on HEAD

# Current state

`cancel-in-progress` is PR-only. Enrollment task cites the durable race test instead of deleted PNGs. This commit changes `implementation.yml`, so GitHub should run full Implementation gates on HEAD (pagination code plus this fix).

# Decisions

- Main pushes never cancel an earlier Implementation run on the same ref. PRs still cancel obsolete runs.
- Screenshots remain required during UI work; committing them stays optional. Tracked task files cite only Git-present evidence.

# Findings / deviations

- Did not re-queue cancelled run `33635602551` on `ba9fad7`: that SHA still has `cancel-in-progress: true`. Full gates on this HEAD cover the same pagination code.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Path classifier includes `implementation.yml` | pass | `bash build/scripts/detect-implementation-changes.test.sh` |
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
