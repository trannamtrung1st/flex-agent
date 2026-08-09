# Cursor and Codex Development Harness

This repository provides equivalent project-scoped harnesses for Cursor and Codex. Both support specification analysis, red-green-refactor TDD, independent review, and browser-backed QA.

For product and engineering documentation structure, start at [`docs/README.md`](../README.md).

## What is installed

Persistent Cursor rules under `.cursor/rules/` and Codex guidance in `AGENTS.md` enforce:

- Flex Agent vocabulary and architectural invariants
- Specification-first red-green-refactor development
- Reusable role selection
- Mandatory Playwright MCP screenshot evaluation for UI work
- Security, privacy, participant isolation, and audit defaults
- Local git workflow defaults (`git` for commits and inspection; GitHub tooling only on explicit request)
- Shared implementation planning and progress tracking under `.work/active/` for substantive coding tasks

Equivalent project skills under `.cursor/skills/` (Cursor) and `.agents/skills/` (Codex) provide these roles:

- `business-analyst` — turns product intent into bounded, testable specs
- `architect` — governs system boundaries, quality attributes, technical decisions, and ADRs
- `ui-ux-designer` — designs accessible journeys, interaction states, responsive behavior, and design-system guidance
- `documentation-author` — composes product, architecture, and UI/UX perspectives into authoritative documents under `docs/`
- `backend-developer` — implements server behavior through TDD
- `frontend-developer` — implements accessible, resilient UI and verifies it in the live browser
- `backend-reviewer` — reviews correctness, contracts, data, concurrency, security, and tests
- `frontend-reviewer` — reviews code plus rendered UX, accessibility, responsiveness, and polish
- `tester` — runs risk-based functional, integration, regression, accessibility, and UI/UX testing
- `security-privacy-reviewer` — reviews the trust boundaries that are unusually important for participant data, memory, tools, uploads, evidence, and evaluations

For every UI role, the shared [design system](../ui-ux/design-system/README.md)
provides the status/authority boundary and
[implementation guide](../ui-ux/design-system/implementation-guide.md) used to
select relevant foundations, components, and product patterns. The equivalent
Cursor and Codex role skills point to the same source so visual rules do not
drift between harnesses.

Workflow skills cover delivery processes that compose roles:

- `git-workflow` — local git commits, branches, merges, and pull requests without defaulting to GitHub login
- `implementation-workflow` — plan, track, verify, and complete substantive implementation work using shared task state under `.work/active/`

The security/privacy role is intentionally included beyond the requested minimum. This product handles sensitive participant content, consequential evaluations, external tools, dynamic memory, and multi-tenant boundaries; treating those concerns as a late generic review would be unsafe.

## Role and workflow separation

Role skills define capabilities, responsibilities, quality standards, and expected outputs. They are applicable independently and may compose one another when work crosses roles. `documentation-author` explicitly composes `business-analyst`, `architect`, and `ui-ux-designer` perspectives for authoritative documents under `docs/`, adding security/privacy review when relevant. Roles do not prescribe a project-specific sequence, handoff, approval path, or release process.

When the project adopts additional concrete processes—such as feature discovery, pull-request review, release readiness, or incident response—define each as a separate workflow skill that invokes the relevant roles. Installed workflow skills:

- `git-workflow` — version control
- `implementation-workflow` — substantive implementation planning and progress tracking

## Playwright MCP

`.cursor/mcp.json` and `.codex/config.toml` start the same pinned official `@playwright/mcp` server named `playwright`.

- Isolated browser contexts prevent state leaking between QA sessions.
- Screenshots, logs, and other artifacts are written to `.playwright-mcp/`.
- Omit custom screenshot filenames with the pinned MCP version so files cannot bypass the configured output directory.
- `.playwright-mcp/` is gitignored and must never be committed.
- Use synthetic accounts and data; browser artifacts must not contain real participant data or secrets.

After cloning, open the repository in Cursor or trust it in Codex, then enable the project MCP server if prompted. Codex only loads project `.codex/config.toml` settings for trusted repositories. `npx` downloads the pinned Playwright MCP package on first use. Upgrade both MCP pins together and run a browser smoke test. If a browser binary is missing, follow the MCP server’s install prompt before testing.

## Implementation workflow

Codex and Cursor use equivalent `implementation-workflow` skills (`.agents/skills/` and `.cursor/skills/`).

- **Cursor:** always-on rule `.cursor/rules/06-implementation-workflow.mdc`
- **Codex:** matching section in `AGENTS.md`
- **Both:** load `implementation-workflow` for substantive implementation work

Shared live state lives in `.work/active/<task-slug>.md`. Copy `.work/templates/implementation-plan.md` when starting a new task. See `.work/README.md` for naming, lifecycle, progress markers, and cleanup.

Executable workspace commands and gate coverage for the current scaffold live in [`workspace.md`](workspace.md).

`.work/` is temporary, non-authoritative execution state. Governing specs, ADRs, code, and tests remain authoritative. The workflow composes role skills and specification-driven TDD; it does not replace them. Trivial one-step edits do not require a task file.

Active task files (`.work/active/*.md`) are gitignored. Templates and `.work/README.md` are tracked.

## Git workflow

Agents use local `git` for routine version-control work.

- **Cursor:** always-on rule `.cursor/rules/05-git-workflow.mdc`
- **Codex:** matching section in `AGENTS.md`
- **Both:** load the `git-workflow` skill from `.cursor/skills/` or `.agents/skills/` when the task is primarily about commits, branches, merges, or pull requests

Defaults:

- Use `git status`, `git diff`, `git log`, `git add`, and `git commit` for inspection and commits.
- Do not offer Connect GitHub, ConnectScm, or GitHub login flows unless the user explicitly asks for GitHub-hosted operations.
- Use `gh` only for explicit pull-request, issue, check, or release requests.
- Commit and push only when the user explicitly asks.
- Never update `git config`, skip hooks, or run destructive git commands unless explicitly requested.

## UI evidence standard

UI/UX designers, frontend developers, frontend reviewers, and testers must use the live app when it is runnable. For every changed journey they should:

1. Reach each applicable state through real interactions.
2. Use accessibility snapshots to inspect names, roles, focus order, and structure.
3. Take desktop and narrow screenshots.
4. Evaluate hierarchy, copy, affordances, spacing, alignment, clipping,
   feedback, focus, contrast clues, applicable design-system conformance, and
   polish.
5. Repeat after fixes and cite `.playwright-mcp/` evidence.

Typical coverage includes loading, empty, populated, validation, error/retry, pending, disabled, dialogs, destructive confirmation, permission denied, keyboard focus, time warnings, expiry, completion, release, and applicable voice states.

## Specification policy

Approved product documents and approved feature specifications outrank illustrative product examples. Author specs from [`docs/templates/feature-spec.md`](../templates/feature-spec.md). A spec should include stable requirement and acceptance-criterion IDs, actors and permissions, in/out of scope, journeys and state transitions, business rules, data/audit needs, non-functional requirements, failure behavior, dependencies, open questions, and traceability.

Every open question (`Q-*`) must include an **interim default** plus brief rationale. That interim default is working guidance so work can continue without inventing silent requirements; it is not approved behavior until decided. Record consequential interim defaults as `PROP-*` when they need formal approval (a `PROP-*` may promote or refine a `Q-*` interim default).

Run documentation validation before pushing doc changes:

```bash
python scripts/check_docs.py
```

The script validates internal links and heading fragments (including the repository root `README.md`), deprecated terms, duplicate requirement IDs, Mermaid fence balance, all 19 feature-spec file presence, catalog membership and tier order in both requirements hubs, and tier counts.

GitHub Actions runs the same checks on pull requests and pushes to `main` via [`.github/workflows/docs.yml`](../../.github/workflows/docs.yml). Markdown lint covers `docs/`, `AGENTS.md`, `.cursor/rules/`, `.cursor/skills/`, and `.agents/skills/`.

Anything absent or ambiguous in the approved spec must be handled as one of:

- an open question that blocks or informs a material decision, always with an **interim default** and brief rationale;
- a clearly labeled best-practice proposal (`PROP-*` / `Proposed`) for approval; or
- a reversible implementation detail that does not change observable product behavior.

Do not silently interpret an illustrative product-document example as an MVP commitment.

## Recommended roles to add later

- **Platform/SRE reviewer** before production, to cover SLOs, capacity, disaster recovery, deployment safety, and incident readiness.
- **AI evaluation specialist** when model/harness evaluation datasets and quality gates are defined.
- **Voice/device QA specialist** before voice release, because microphones, speakers, network jitter, echo cancellation, and real interruption timing require device-level testing beyond browser mocks.
