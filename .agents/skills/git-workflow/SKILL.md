---
name: git-workflow
description: Local git workflow for commits, branches, merges, and pull requests. Use git commands directly for version control; reserve GitHub tooling for explicit PR, issue, check, or release requests.
---

# Git Workflow

Follow the git workflow section in `AGENTS.md`. In Cursor, the always-on rule `.cursor/rules/05-git-workflow.mdc` carries the same policy. This skill adds task-specific detail for version-control work.

## When to use

- Creating or amending commits
- Inspecting branch state, history, or diffs
- Preparing branches for review
- Creating pull requests when explicitly requested

## Local git first

- Use `git` for all routine inspection and commits.
- Do not offer Connect GitHub, ConnectScm, or GitHub authentication for local work.
- Use `gh` only when the user explicitly asks for GitHub-hosted operations.

## Commit workflow

1. Run `git status`, `git diff`, and `git log` in parallel.
2. Stage only relevant files; exclude secrets and unrelated changes.
3. Match recent commit message style; focus on why, not just what.
4. Commit with a HEREDOC message, for example:

```bash
git commit -m "$(cat <<'EOF'
Why the change matters.

EOF
)"
```

5. Verify with `git status`.

## Pull request workflow

Only when explicitly requested:

1. Confirm branch state with local git against the base branch.
2. Push only if the user explicitly asks.
3. Use `gh pr create` or equivalent only when GitHub access is available and requested.
4. If GitHub is unavailable, report the branch name, base branch, and manual PR steps.

## Safety

- Never update `git config`.
- Never force-push to `main` or `master` without warning.
- Never skip hooks or run destructive git commands unless explicitly requested.
- If a pre-commit hook fails, fix the issue and create a new commit.
- Use `git commit --amend` only when the user explicitly requested amend, the failed commit was created in this session and not pushed, or a successful commit was auto-modified by hooks and needs inclusion.
