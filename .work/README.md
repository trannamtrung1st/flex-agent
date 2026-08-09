# Implementation working state

`.work/` holds temporary coding-agent execution state. It is **not** authoritative product, architecture, or requirements documentation.

Permanent truth lives in approved specs under `docs/`, ADRs, code, tests, migrations, and durable developer documentation such as `AGENTS.md` and `docs/contributing/development-harness.md`.

## When a task file is required

Create `.work/active/<task-slug>.md` for substantive implementation work, such as:

- new features or multi-file changes
- migrations or meaningful refactors
- architecture-sensitive work
- UI journey implementation
- defects requiring investigation
- work with multiple meaningful steps, material uncertainty, or non-trivial verification

Load the `implementation-workflow` skill (`.agents/skills/` for Codex, `.cursor/skills/` for Cursor) before starting, alongside applicable role skills.

## When a task file is not required

Skip tracked planning for obvious one-step edits, such as a typo fix or tiny mechanical correction.

## Single task file rule

One implementation task normally has **one** state file:

```text
.work/active/<task-slug>.md
```

Do not split the same task across `PLAN.md`, `PROGRESS.md`, `STATE.md`, or `NOTES.md`.

Copy `.work/templates/implementation-plan.md` when creating a new task file.

## Task slug naming

Use a short, kebab-case slug that identifies the work:

- `participant-session-timeout`
- `fix-evaluation-export-auth`
- `harness-config-validation`

Prefer stable, descriptive names over ticket numbers alone.

## Task lifecycle

1. **Create** — copy the template, set goal, governing sources, scope, plan, and verification approach.
2. **Execute** — update the file continuously as steps start, complete, change, block, or verify.
3. **Reconcile** — compare planned work to actual changes before claiming completion.
4. **Promote** — move durable decisions or newly discovered requirements into authoritative artifacts when needed.
5. **Clean up** — delete the active task file after completion unless there is a clear reason to retain it.

Update the front-matter `status` and `updated` fields as work proceeds (`planned`, `in-progress`, `blocked`, `completed`).

## Progress markers

Use these checklist markers in the `# Plan` section:

| Marker | Meaning |
| --- | --- |
| `[x]` | completed |
| `[>]` | current |
| `[ ]` | pending |
| `[!]` | blocked |
| `[-]` | intentionally skipped |

Mark only one plan step as `[>]` at a time when possible.

## Handoff between Codex, Cursor, and humans

All coding agents and humans use the same file under `.work/active/`. Before substantial work:

1. Read the active task file if one exists for the current work.
2. Inspect governing sources listed there.
3. Continue from `# Current state` and the marked `[>]` step.

Keep `# Current state`, `# Findings / deviations`, `# Blockers`, and `# Verification` accurate enough that another agent can resume without re-deriving context.

Record concise implementation summaries only. Do not persist hidden chain-of-thought or private reasoning.

## What belongs elsewhere

| Content | Belongs in |
| --- | --- |
| Product meaning, scope, acceptance criteria | Approved specs under `docs/` |
| Architecture decisions | ADRs under `docs/architecture/` |
| UI/UX behavior | Approved UI/UX specs under `docs/ui-ux/` |
| Implemented behavior | Code and tests |
| Durable developer workflow | `AGENTS.md`, `.cursor/rules/`, `docs/contributing/` |

If a discovery changes product meaning, requirements, architecture, or another durable contract, update the appropriate authoritative artifact. Do not treat `.work/` files as permanent authority.

## Git policy

Tracked:

- `.work/README.md`
- `.work/templates/`
- `.work/active/.gitkeep`

Ignored:

- `.work/active/*.md` (live task files)

Active plans are temporary execution state, not permanent project history. If the team later needs cross-machine or branch-persisted planning state, this policy may be intentionally changed.

## Completion

Completion requires evidence, not checklist theater:

- reconcile planned work with actual changes
- run applicable focused tests and proportionate integration or regression checks
- recheck governing specifications
- record remaining gaps or unverified behavior
- remove the active task file when done

Do not claim completion merely because checklist items were manually marked complete.
