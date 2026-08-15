---
id: document-backend-module-architecture
status: completed
created: 2026-08-15
updated: 2026-08-15
---

# Goal

Make the approved backend architecture and module-structure rules easy to
apply consistently during future implementation.

# Governing sources

- `docs/product/concept-model.md`
- `docs/product/mvp-scope.md`
- `docs/product/overview.md`
- `docs/architecture/mvp-architecture.md`
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`
- `docs/architecture/decisions/ADR-006-mvp-architecture-baseline-and-evolution.md`
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
- `AGENTS.md`

# Scope

## In

- State the backend architectural identity precisely.
- Define module responsibilities, dependency direction, project-splitting
  thresholds, cross-module collaboration, persistence ownership, and
  verification expectations.
- Link the guidance from architecture and contributor entry points.

## Out

- Product, requirement, runtime, or UI behavior changes.
- Source-code or project-layout refactoring.
- Retrofitting existing modules as part of this documentation task.

# Plan

- [x] Inspect governing architecture, current project structure, and executable
  architecture tests.
- [x] Add authoritative implementation guidance derived from ADR-010 and link
  it from existing documentation routes.
- [x] Validate documentation, review the scoped diff, and reconcile the task.

# Current state

The backend module guide now consolidates the approved architectural identity,
module ownership and dependencies, project-splitting policy, adapter placement,
cross-module collaboration, persistence/isolation rules, and verification
checklist. Architecture and contributor routes link to the guide. Explicit
owner approval was recorded on 2026-08-15, so the guide is authoritative for
future backend implementation under ADR-006 and ADR-010.

# Decisions

- Name the architecture "domain-oriented modular monolith with ports and
  adapters"; treat Clean Architecture as a dependency rule rather than a
  required project template.
- Keep one core assembly per module as the default and introduce separate
  adapter assemblies only when they create an enforceable dependency boundary.
- Preserve existing approved ADR-010 decisions; the new guide explains their
  application and does not create new product scope.
- Record the 2026-08-15 explicit owner approval in the guide and architecture
  index; no further architecture approval action remains for this item.

# Findings / deviations

- The worktree contains unrelated Session runtime changes. They are excluded
  from this task and must not be staged or committed.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `python3 scripts/check_docs.py` | passed | Documentation validation passed. |
| Scoped `git diff --check` | passed | No whitespace errors in task-owned files. |
| Scoped diff review | passed | Only the task record and five documentation files are in scope. |
| Approval record follow-up | passed | Guide and architecture index record explicit owner approval dated 2026-08-15. |
| Application/integration tests | skipped | Documentation-only change; no implemented behavior changed. |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
