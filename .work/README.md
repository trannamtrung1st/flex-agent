# Implementation working state

`.work/` holds coding-agent execution records. It is tracked in Git so external reviewers and collaborators can inspect plans, progress, decisions, and verification evidence and maintainers can retain completed task history. It is **not** current product, architecture, or requirements authority.

**Snapshot-first:** completed, cancelled, blocked, and superseded tasks are not current authority. Git owns history. Durable decisions belong in approved documents under `docs/`, code, tests, and migrations.

Permanent truth lives in approved specs under `docs/`, current architecture documents, code, tests, migrations, and durable developer documentation such as `AGENTS.md` and `docs/contributing/development-harness.md`.

## Directory structure

```text
.work/
├── active/      # task execution state (one file per implementation task)
├── resources/   # non-authoritative source, proposal, or reference material used by tasks
└── templates/   # task templates
```

`.work/resources/` holds inputs such as planning proposals that a task consumes
before promotion. Those files **must not** become product, requirements, UI/UX,
or architecture authority. Promote durable decisions into approved artifacts
under `docs/` before implementation treats them as governing.

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
5. **Review** — mark the task completed and retain it during external review.
6. **Track** — keep the completed task file as implementation history; repository maintainers may clean up retained files when they choose.

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

Record concise implementation summaries only. Because files under `.work/` are
Git-visible, never persist secrets, credentials, sensitive participant data,
hidden chain-of-thought, or private reasoning.

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

Tracked for implementation collaboration and external review:

- `.work/README.md`
- `.work/templates/`
- `.work/active/`, including live and retained completed task files
- `.work/resources/`, non-authoritative source or proposal material (not
  product, requirements, UI/UX, or architecture authority)

Nothing under `.work/` is intentionally ignored. Before adding or updating any
tracked file under `.work/`, ensure it contains no secrets, credentials,
sensitive participant data, hidden chain-of-thought, private reasoning, or
unnecessary raw output.

Tracked plans remain non-authoritative execution records, not permanent product or architecture truth. Keep them after completion and external review for implementation tracking. Do not remove them as part of the implementation workflow; repository maintainers may clean them up when they choose. Promote durable decisions to their authoritative artifacts regardless of task-file retention.

## Completion

Completion requires evidence, not checklist theater:

- reconcile planned work with actual changes
- run applicable focused tests and proportionate integration or regression checks
- recheck governing specifications
- record remaining gaps or unverified behavior
- mark the task completed and make it safe and complete for external review
- retain it after completion and review for tracking; leave any cleanup to repository maintainers

Do not claim completion merely because checklist items were manually marked complete.
