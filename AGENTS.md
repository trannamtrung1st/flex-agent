# Flex Agent Repository Guidance

## Product foundation

Read `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
`docs/product/overview.md` before making product or architecture decisions.

Documents govern by concern. A document overrides another only within its area
of authority; see `docs/README.md#authority-by-concern`.

- Product meaning and scope: approved product documents or product decisions
- Observable system behavior: approved feature specifications
- User interaction: approved UI/UX specifications
- Technical realization: approved ADRs
- Implemented behavior: code and tests, traceable to the above

A conflicting ADR triggers product or requirements review; it does not override
product semantics. Do not turn an idea, example, or future capability into an
MVP requirement. Ask when a material ambiguity remains. Every open question
must include an **interim default** with a brief rationale. The default is
working guidance, not approved behavior. Record consequential defaults as
`Proposed` (`PROP-*` in feature specifications).

Keep these concepts separate:

- Organization: tenant boundary and non-bypassable policy limits
- Agent: reusable identity, knowledge defaults, capabilities, and communication behavior
- Harness: workflow and policy constraints, allowed capability subset, and evaluation procedure
- Activity: execution context; a campaign is one managed multi-participant form
- Session: isolated execution with frozen resolved configuration, events, evidence, and outcome
- Evaluation, human revision, review decision, result, and release: distinct objects in the outcome chain

Non-negotiable invariants:

- Enforce organization, activity-scope, participant, and session isolation.
- Record a resolved execution manifest and resolved session configuration for every session.
- Never silently overwrite audit-relevant history; keep prior versions inspectable.
- Distinguish generated, sent, played, interrupted, cancelled, and playback-confirmed voice content.
- Link evaluations and human revisions to stable evidence; preserve original outputs.
- Never allow uncontrolled memory learning, harness self-modification, or result release.
- Lower configuration scopes may narrow but not widen delegated upper-scope capabilities.
- Do not reuse participant data for agent learning without explicit permission.
- Give recorded times unambiguous ordering and timezone interpretation.
- Require explicit authorization at every sensitive boundary.

## Role routing

Load the matching repository skill from `.agents/skills/` before substantive work. Role and workflow skills live in that directory.

- Requirements, scope, acceptance criteria, or spec decomposition: `business-analyst`
- Cross-cutting design, boundaries, quality attributes, technology decisions, or ADRs: `architect`
- Journeys, information architecture, interaction states, accessibility, visual design, or design systems: `ui-ux-designer`
- Authoritative product, UI/UX, architecture, or technical docs under `docs/`: `documentation-author`
- APIs, domain logic, persistence, auth, jobs, or integrations: `backend-developer`
- Web UI, client state, components, styling, accessibility implementation, or frontend defects: `frontend-developer`
- Backend review: `backend-reviewer`
- Frontend review or UI polish review: `frontend-reviewer`
- QA, acceptance, regression, functional, accessibility, or UX testing: `tester`
- Threat modeling, privacy, isolation, memory, uploads, tools, or audit review: `security-privacy-reviewer`
- Commits, branches, merges, or pull requests: `git-workflow`

Load multiple skills when a task crosses roles. Keep implementation and review perspectives distinct: a reviewer reports findings and evidence and does not edit unless fixes were also requested. Role skills define reusable responsibilities and quality standards. Workflow skills such as `git-workflow` cover delivery processes that compose roles; load them when the task is primarily about that process.

## Specification-driven TDD

For each behavior change:

1. Read the governing spec and identify stable requirement or acceptance ID(s).
2. Resolve material ambiguity or record an open question with an interim default, rationale, and a `Proposed`/`PROP-*` item when consequential.
3. Map each in-scope criterion to an implementation surface and verification.
4. Red: add the smallest useful test and run it to confirm the intended failure.
5. Green: implement the minimum behavior that makes the test pass.
6. Refactor while keeping the suite green.
7. Run focused tests, then proportionate integration and regression checks.
8. Report coverage, evidence, assumptions, and anything not verified.

For defects, reproduce with a failing regression test before fixing when feasible. Use domain/unit tests for rules and transitions, integration/contract tests for persistence and boundaries, and browser tests for critical journeys. Playwright exploration is evidence, not a substitute for repeatable tests. Do not claim a red phase unless the failing test ran. For docs, configuration, migrations, or time-boxed spikes where test-first is not meaningful, state the exception and use the strongest available validation. Never weaken or delete a valid test just to obtain green.

## Playwright MCP verification

For every UI-affecting change when the app can run, use the project `playwright` MCP server configured in `.codex/config.toml`:

1. Reach each changed state through real interactions.
2. Use accessibility snapshots to inspect controls, names, structure, and focus.
3. Take screenshots before judging visual quality.
4. Evaluate hierarchy, copy, spacing, alignment, overflow, feedback, focus, contrast clues, responsiveness, and polish.
5. Fix defects and repeat the browser check.

Cover applicable loading, empty, populated, success, validation, error/retry, pending, disabled, dialog, destructive confirmation, keyboard-focus, permission, session lifecycle, release, and voice states at desktop and narrow viewports. Store artifacts only in `.playwright-mcp/`. Omit custom screenshot filenames so the pinned server honors its output directory. Never put real credentials, secrets, or participant data in artifacts. If the server or runnable app is unavailable, report the exact blocker and manual checks; do not claim visual verification from source alone.

## Security and privacy defaults

Treat participant submissions, transcripts, voice, evaluations, memory, and audit records as sensitive.

- Deny by default; authenticate, then authorize actor and resource scope on the server.
- Never trust client-supplied organization, activity, participant, session, role, or ownership identifiers.
- Prevent cross-tenant, activity, and session access in queries, caches, events, tools, logs, and memory retrieval.
- Minimize collection, retention, export, and model/tool disclosure; require an explicit policy for learning reuse.
- Keep secrets and raw sensitive content out of source, browser artifacts, logs, metrics, and error responses.
- Validate uploads by type, size, content, ownership, and malware policy; retain immutable versions used for evaluation.
- Apply least privilege, allowlists, timeouts, and auditable approval to tools and integrations.
- Make retryable sensitive mutations idempotent and preserve actor, reason, prior state, and timestamp.
- Use platform-managed encryption in transit and at rest.
- Threat-model new trust boundaries and add negative authorization/isolation tests.

Do not invent cryptography, authentication, authorization, retention, consent, or compliance behavior. Raise an open question with an interim default and rationale, plus a `Proposed`/`PROP-*` item when consequential.

## Git workflow

Use local `git` for routine version-control work. Do not offer Connect GitHub, ConnectScm, or other GitHub authentication flows unless the user explicitly asks for GitHub-hosted operations.

- Inspect and change local state with `git status`, `git diff`, `git log`, `git add`, `git commit`, `git branch`, `git merge`, and read-only `git rebase` inspection.
- Use `gh` or GitHub APIs only when the user explicitly requests pull requests, issues, checks, releases, or other GitHub-hosted actions.
- If GitHub access is unavailable, continue with local git and report what the user can do manually.
- Commit only when explicitly requested. Push only when explicitly requested.
- Never update `git config`.
- Never run destructive or irreversible git commands (for example `push --force`, `reset --hard`) unless the user explicitly requests them.
- Never skip hooks (`--no-verify`, `--no-gpg-sign`, and similar) unless the user explicitly requests it.
- Warn before any force-push to `main` or `master`.

When the user asks for a commit:

1. Run `git status`, `git diff`, and `git log` in parallel.
2. Stage only relevant files. Do not commit likely secrets (`.env`, credentials files, and similar).
3. Draft a concise 1–2 sentence message focused on why, matching repository style.
4. Commit with a HEREDOC message.
5. Run `git status` after the commit to verify success.

If a pre-commit hook fails, fix the issue and create a new commit. Use `git commit --amend` only when the user explicitly requested amend, the failed commit was created in this session and not pushed, or a successful commit was auto-modified by hooks and needs inclusion.

Create or update pull requests only when explicitly requested. Before using `gh`, confirm branch state with local git (`git status`, `git log`, and `git diff` against the base branch). Do not push to the remote unless the user explicitly asks.
