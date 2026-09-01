# Flex Agent Repository Guidance

## Product foundation

Read `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
`docs/product/overview.md` before making product or architecture decisions.

Documents govern by concern. A document overrides another only within its area
of authority; see `docs/README.md#authority-by-concern`.

- Product meaning and scope: approved product documents or product decisions
- Observable system behavior: approved feature specifications
- User interaction: approved UI/UX specifications
- Technical realization: approved architecture documents under `docs/architecture/`
- Implemented behavior: code and tests, traceable to the above
- Cross-concern status: derived `docs/current-state.md` (non-normative)

A conflicting architecture document triggers product or requirements review; it does not override
product semantics. Historical ADR files are recoverable from Git and are not the
current architecture catalog. Do not turn an idea, example, or future capability into an
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
- Cross-cutting design, boundaries, quality attributes, technology decisions, or current architecture documents: `architect`
- Journeys, information architecture, interaction states, accessibility, visual design, or design systems: `ui-ux-designer`
- Authoritative product, UI/UX, architecture, or technical docs under `docs/`: `documentation-author`
- Full-stack implementation spanning backend and frontend: `developer`
- APIs, domain logic, persistence, auth, jobs, or integrations: `backend-developer`
- Web UI, client state, components, styling, accessibility implementation, or frontend defects: `frontend-developer`
- Cross-cutting review spanning backend, frontend, and security/privacy: `reviewer`
- Backend review: `backend-reviewer`
- Frontend review or UI polish review: `frontend-reviewer`
- QA, acceptance, regression, functional, accessibility, or UX testing: `tester`
- Threat modeling, privacy, isolation, memory, uploads, tools, or audit review: `security-privacy-reviewer`
- Visual design craft, critique, extraction, hardening, polish, or audit of UI: `impeccable` (explicit `$impeccable`; never implicit)
- Commits, branches, merges, or pull requests: `git-workflow`
- Substantive implementation planning, execution tracking, and completion: `implementation-workflow`

Use `developer` and `reviewer` for their defined cross-cutting compositions; load specialist skills directly for narrower work. Keep implementation and review perspectives distinct: a reviewer reports findings and evidence and does not edit unless fixes were also requested. Role skills define reusable responsibilities and quality standards. Workflow skills cover delivery processes that compose roles: load `git-workflow` when the task is primarily about version control; load `implementation-workflow` for substantive implementation work alongside applicable role skills.

Impeccable is an explicit composition layer for UI craft. Approved Flex Agent
documents remain authority for product, requirements, journeys, accessibility,
security, and architecture. Bounded commands: `shape`/`critique` for UI/UX
design, `extract`/`document` for design-system adoption, `harden`/`adapt`/`polish`
for implementation, and `audit` for review. Do not run open-ended polish loops,
hooks, or live mode unless a later approved proposal enables them.

## Implementation workflow

For substantive implementation work, load `implementation-workflow` from `.agents/skills/`.

- Shared mutable task state: `.work/active/<task-slug>.md` (template: `.work/templates/implementation-plan.md`)
- Operational guidance: `.work/README.md`
- Trivial one-step edits do not require a task file.

Keep the active task file current during execution: steps, discoveries, blockers, verification evidence, and next actions. One task normally has one state file; do not split the same work across separate plan or progress files.

`.work/` is tracked, snapshot-first, non-authoritative working state so external reviewers can inspect live implementation plans and evidence. `planned` / `in-progress` stay in `.work/active`. A completed task may remain temporarily through its required review, then is deleted once durable truth has been promoted. Completed, cancelled, blocked, and superseded tasks are **not current authority**; Git owns history. Never put secrets, sensitive data, or hidden reasoning in it. If a discovery changes product meaning, requirements, architecture, or another durable contract, move it into the appropriate authoritative artifact.

Completion requires reconciling planned work with actual changes, proportionate verification with evidence, and rechecking governing specifications. Do not claim completion from checklist marks alone. After required review and promotion, delete the task file. Git is the implementation history.

For any UI design, implementation, review, or testing task, read
`docs/ui-ux/design-system/README.md` and its status/authority rules, then load
the applicable modules through
`docs/ui-ux/design-system/implementation-guide.md`. A narrower approved UI/UX
specification governs feature-specific behavior. A design-system module does
not authorize a deferred capability.

Before new production UI: classify the surface against approved UX and
`docs/current-state.md`. Clone and adapt a matching accepted production page and
Component Deck specimen. Use a Design Lab journey only when the approved layout
family has no production donor. Invoke explicit `$impeccable shape` only for a
documented gap. Establish any reusable addition in the design system before
production use. Attach to a healthy local origin before starting Compose or
Vite; see Playwright MCP verification below.

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

Use the project `playwright` MCP server configured in `.codex/config.toml`.
Artifacts must stay under `.playwright-mcp/`.

Attach before starting anything. Pick the origin the work needs, probe it, and
reuse it when healthy. Do not start a second listener on the same port. Do not
run `pnpm compose:up` over a healthy stack (`compose:up` regenerates secrets
and reseeds). A busy port is not proof it is Flex Agent or the right profile.
Probe both `http://localhost:<port>` and `http://127.0.0.1:<port>` (Vite often
binds `::1` only; Compose `:18080` is IPv4 `127.0.0.1`).
Full table: `docs/contributing/development-harness.md` (Attach to a running
local origin).

| Work | Origin | Attach probe |
| --- | --- | --- |
| Canonical product | `http://localhost:18080` | `pnpm compose:status` with `session-endpoint:ok`; `/realms/flex-agent` answers |
| Candidate UI | `http://localhost:5274` | HTTP success **and** `:18080` healthy; candidate overlay + `VITE_DEV_API_PROXY=http://127.0.0.1:18080` for OIDC |
| Design lab | `http://localhost:5275` | HTTP success on `/design-lab/` (also try `127.0.0.1` if bound there) |

After candidate overlay work, return the API to the canonical `RedirectUri`
with `pnpm compose:api:canonical` (API only; no reseed). Use
`pnpm compose:reset` only when a fresh stack is acceptable. See
`docs/contributing/development-harness.md` (synthetic sign-in).

If the probe succeeds, navigate there. If the port is busy but the probe fails,
report the blocker; do not tear down the user's stack. If nothing is listening,
start only the documented command for that origin (never a fallback port).
Prefer `:5274` when Compose SPA may lag `web/` source; prefer `:18080` for
canonical OIDC. Never reuse a live stack for `pnpm verify:oidc`.

For authenticated product UI, sign in with the synthetic Compose users. Do not
stop at **Sign in required**. Read
`docs/contributing/development-harness.md` (Synthetic sign-in) and
`tests/Browser/FlexAgent.Oidc.Playwright/helpers/oidc.ts`. Match
`pnpm compose:status` `redirect-uri` to `:18080` or `:5274`. Switch a healthy
stack with `pnpm compose:api:canonical` / `pnpm compose:api:candidate` — never
`compose:up` / `compose:candidate` over a live profile. Do not screenshot the
Keycloak password form or record passwords in `.work/`.

For every UI-affecting change when that origin can run:

1. Reach each changed state through real interactions.
2. Use accessibility snapshots to inspect controls, names, structure, and focus.
3. Take screenshots before judging visual quality.
4. Evaluate hierarchy, copy, spacing, alignment, overflow, feedback, focus,
   contrast clues, responsiveness, polish, and conformance to the applicable
   design-system modules.
5. Fix defects and repeat the browser check.

Cover applicable loading, empty, populated, success, validation, error/retry, pending, disabled, dialog, destructive confirmation, keyboard-focus, permission, session lifecycle, release, and voice states at desktop and narrow viewports. Store artifacts only in `.playwright-mcp/`. Omit custom screenshot filenames so the pinned server honors its output directory. Only inspected PNG screenshots produced with synthetic accounts and data may be committed for external review; keep accessibility snapshots, logs, traces, and browser state untracked. Never put real credentials, secrets, or participant data in artifacts. If the server or runnable app is unavailable, report the exact blocker and manual checks; do not claim visual verification from source alone.

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
