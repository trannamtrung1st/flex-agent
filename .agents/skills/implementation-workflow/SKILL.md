---
name: implementation-workflow
description: Plan, track, review, and complete substantive implementation work using shared Git-tracked task state under .work/active/. Use for multi-step features, refactors, migrations, investigations, external review handoffs, and other non-trivial coding tasks.
---

# Implementation Workflow

Follow the implementation workflow section in `AGENTS.md` and the always-on rule `.cursor/rules/06-implementation-workflow.mdc`. Operational details live in `.work/README.md`.

This workflow composes role skills; it does not replace them. Load applicable roles (`backend-developer`, `frontend-developer`, `architect`, `business-analyst`, `tester`, reviewers, and others) for the work at hand.

`git-workflow` is separate. Commits, pushes, and pull requests happen only when explicitly requested.

## When to use

Use this workflow for substantive implementation work:

- new features or multi-file changes
- migrations or meaningful refactors
- architecture-sensitive work
- UI journey implementation
- defects requiring investigation
- work with multiple steps, material uncertainty, or non-trivial verification

Skip tracked planning for obvious one-step edits such as typos or tiny mechanical corrections.

## Workflow

### 1. Inspect before implementing

- Read repository state relevant to the task.
- Read governing sources: product docs, feature specs, UI/UX specs, architecture documents, and applicable repository guidance.
- Check `.work/active/` for an existing task file for this work.

### 2. Decide whether to track

If the task is substantive, create or resume:

```text
.work/active/<task-slug>.md
```

Copy from `.work/templates/implementation-plan.md`. One task, one file. Do not create separate `PLAN.md`, `PROGRESS.md`, or similar for the same task.

Task files are tracked for collaboration and external review. Never record secrets, sensitive participant data, credentials, hidden chain-of-thought, or private reasoning in any tracked file under `.work/`, including `resources/`.

### 3. Plan before substantial implementation

Before large edits, define in the task file:

- goal and scope (in/out)
- governing sources and requirement or acceptance IDs where applicable
- ordered plan steps with verification approach
- known risks, assumptions, or open questions (with interim defaults when material)

Do not invent product behavior when specifications are ambiguous. Record open questions with interim defaults and promote consequential items to `PROP-*` when needed.

### 4. Execute incrementally

- Load applicable role skills for each kind of work.
- Follow specification-driven TDD when behavior changes (red, green, refactor).
- For new production UI, classify first; clone and adapt a matching accepted production page plus Component Deck specimen; use a Lab journey only when the family lacks a production donor; use explicit `$impeccable shape` only for a documented gap.
- Mark plan progress using the markers documented in `.work/README.md`:
  - `[x]` completed, `[>]` current, `[ ]` pending, `[!]` blocked, `[-]` intentionally skipped

### 5. Keep the task file current

Update the task file when:

- a step starts or completes
- the approach changes materially
- new repository facts are discovered
- a blocker appears or clears
- a step is intentionally skipped
- verification succeeds or fails
- remaining gaps become known

Use concise implementation summaries. Do not persist hidden chain-of-thought or private reasoning.

Update front-matter `status` and `updated` as work proceeds.

### 6. Verify with evidence

- Run focused tests, then proportionate integration or regression checks.
- Record commands, results, and artifact paths in the `# Verification` table.
- For UI work, attach to the matching local origin first (`docs/contributing/development-harness.md`, Attach to a running local origin), then follow Playwright MCP verification.
- Do not claim completion without evidence.

### 7. Reconcile and promote

Before declaring completion:

- reconcile planned work against actual changes
- recheck governing specifications
- move durable decisions or newly discovered requirements into authoritative artifacts (`docs/`, ADRs, specs) when required
- record remaining gaps or unverified behavior

### 8. Prepare review handoff

Mark the task completed and keep `.work/active/<task-slug>.md` after completion and external review as retained implementation history. Do not remove completed task files as part of the implementation workflow; repository maintainers may clean them up when they choose. Promote durable decisions to authoritative artifacts because task files remain non-authoritative working records.

Completion is not achieved by manually checking boxes alone. Verification must support the claim.
