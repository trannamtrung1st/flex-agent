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
- `developer` — coordinates full-stack work by composing the backend and frontend developer roles
- `backend-developer` — implements server behavior through TDD
- `frontend-developer` — implements accessible, resilient UI and verifies it in the live browser
- `reviewer` — coordinates independent backend, frontend, and security/privacy review into one evidence-backed result
- `backend-reviewer` — reviews correctness, contracts, data, concurrency, security, and tests
- `frontend-reviewer` — reviews code plus rendered UX, accessibility, responsiveness, and polish
- `tester` — runs risk-based functional, integration, regression, accessibility, and UI/UX testing
- `security-privacy-reviewer` — reviews the trust boundaries that are unusually important for participant data, memory, tools, uploads, evidence, and evaluations

For every UI role, the shared [design system](../ui-ux/design-system/README.md)
provides the status/authority boundary and
[implementation guide](../ui-ux/design-system/implementation-guide.md) used to
select relevant foundations, components, and product patterns. New production
UI clones a matching existing production page and Component Deck specimen.
The equivalent Cursor and Codex role skills point to the same source so visual
rules do not drift between harnesses.

Workflow skills cover delivery processes that compose roles:

- `git-workflow` — local git commits, branches, merges, and pull requests without defaulting to GitHub login
- `implementation-workflow` — plan, track, review, and complete substantive implementation work using shared task state under `.work/active/`

The security/privacy role is intentionally included beyond the requested minimum. This product handles sensitive participant content, consequential evaluations, external tools, dynamic memory, and multi-tenant boundaries; treating those concerns as a late generic review would be unsafe.

## Role and workflow separation

Role skills define capabilities, responsibilities, quality standards, and expected outputs. They are applicable independently and may compose one another when work crosses roles. `developer` composes `backend-developer` and `frontend-developer` for full-stack changes. `reviewer` composes `backend-reviewer`, `frontend-reviewer`, and `security-privacy-reviewer`; `tester` remains a separate QA role. `documentation-author` explicitly composes `business-analyst`, `architect`, and `ui-ux-designer` perspectives for authoritative documents under `docs/`, adding security/privacy review when relevant. Roles do not prescribe a project-specific sequence, handoff, approval path, or release process.

When the project adopts additional concrete processes—such as feature discovery, pull-request review, release readiness, or incident response—define each as a separate workflow skill that invokes the relevant roles. Installed workflow skills:

- `git-workflow` — version control
- `implementation-workflow` — substantive implementation planning and progress tracking

## Playwright MCP

`.cursor/mcp.json` and `.codex/config.toml` start the same pinned official `@playwright/mcp` server named `playwright`.

- Isolated browser contexts prevent state leaking between QA sessions.
- Screenshots, logs, and other artifacts are written to `.playwright-mcp/`.
- Omit custom screenshot filenames with the pinned MCP version so files cannot bypass the configured output directory.
- Only inspected PNG screenshots produced with synthetic accounts and data may be committed for external review.
- Accessibility snapshots, logs, traces, and browser state must remain untracked.
- Use synthetic accounts and data; browser artifacts must not contain real participant data or secrets.

For authenticated product journeys, Playwright must use the approved
Development/Testing browser profile in the
[Keycloak OIDC contract](../operations/provider-profiles/keycloak-oidc-contract.md#authenticated-browser-profile).
That profile exercises the real `/auth` and product API boundary, opaque
application sessions, application-owned authorization, and PostgreSQL state.

### Attach to a running local origin

Probe the origin the work needs, then attach when it is healthy. Do not start a
second listener on the same port. Do not run `pnpm compose:up` over a healthy
stack: that command regenerates secrets, reseeds data, and renews volumes.

A busy port is not proof the process is Flex Agent or the right profile.
On this workspace, Vite often listens on IPv6 `localhost` (`::1`) only, while
Compose publishes `:18080` on IPv4 `127.0.0.1`. Probe `http://localhost:<port>`
and `http://127.0.0.1:<port>` before concluding an origin is down or starting
another listener.

| Work | Origin | Attach probe |
| --- | --- | --- |
| Authenticated product (canonical SPA + API) | `http://localhost:18080` | `pnpm compose:status` — Compose services up and `session-endpoint:ok`. `/realms/flex-agent` also answers |
| Candidate UI against the live API | `http://localhost:5274` | HTTP success on that origin **and** Compose `:18080` healthy. Candidate OIDC also needs `RedirectUri` for `:5274` (`pnpm compose:status` prints `redirect-uri:`) and Vite `VITE_DEV_API_PROXY=http://127.0.0.1:18080` |
| Isolated design lab | `http://localhost:5275` | HTTP success on `/design-lab/` (also try `http://127.0.0.1:5275` if the process was started with `--host 127.0.0.1`) |

**Return to canonical after candidate work.** Candidate `RedirectUri` is
`http://localhost:5274/auth/callback`. Canonical sign-in on `:18080` then fails
with **Sign-in could not be completed** until the API matches the origin you
use. On a **healthy** stack, switch the API only (no reseed, no new secrets):

```bash
pnpm compose:api:canonical   # RedirectUri → http://localhost:18080/auth/callback
pnpm compose:api:candidate   # RedirectUri → http://localhost:5274/auth/callback
```

Do not run `pnpm compose:candidate` or `pnpm compose:up` over an attachable
stack: those regenerate secrets and reseed. Use `pnpm compose:reset` only when
the user asked for a fresh stack.

### Synthetic sign-in (Playwright MCP)

Authenticated product screenshots must pass the OIDC gate. Design lab (`:5275`)
does not. Match the browser origin to `redirect-uri` from `pnpm compose:status`.

1. Read usernames and the default demo password from
   `tests/Browser/FlexAgent.Oidc.Playwright/helpers/oidc.ts` (`syntheticUsers`,
   `FLEXAGENT_OIDC_*` overrides). Follow `signInThroughKeycloak`. Do not copy
   passwords into `.work/`, chat logs, screenshots, or commits.
2. Navigate the matching origin (`:18080` canonical SPA, or `:5274` candidate
   Vite). If the heading is **Sign in required**, click **Continue to sign in**.
3. On Keycloak, fill **Username or email** and **Password**, then **Sign In**.
   Do not screenshot the password form.
4. Wait until production chrome shows the operator menu (not the sign-in
   ceremony). Then take screenshots of the product states under test.

Actor choice: `demo.admin` for administrator Home / Activities / Setup /
Participants; `demo.participant` for My work when that seed assignment exists
(`FLEXAGENT_SEED_DEMO_WORK` default). Demo work IDs are deterministic in
`deploy/compose/authenticated-browser/seed-demo-work.sql`.

If **Continue to sign in** returns **Sign-in could not be completed**, the
`RedirectUri` does not match the origin — run `compose:api:canonical` or
`compose:api:candidate` as appropriate. Do not `compose:up`.

**Canonical OIDC Playwright origins.** Browser tests must use
`http://localhost:18080` so correlation cookies match the configured callback.
The Playwright `request` fixture uses `apiUrl()` (IPv4 `127.0.0.1`) because
Compose nginx publishes `:18080` on `127.0.0.1` only.

1. Choose the origin from the table. Record which origin screenshots used.
2. If the probe succeeds, navigate Playwright MCP to that origin. Do not start Compose or another Vite.
3. If the port is busy but the probe fails (wrong app, session down, overlay or proxy mismatch), report the blocker. Do not `compose:up`, `compose:reset`, or `compose:down` unless the user asked to reset the stack.
4. If nothing is listening, start only the documented command for that origin. Never pick a fallback port: candidate OIDC is bound to `5274`.
5. Prefer Vite `:5274` for source-level UI evidence when the Compose SPA image may lag `web/` source. Prefer `:18080` for canonical gateway and OIDC evidence.
6. `pnpm verify:oidc` always starts its own Compose lifecycle and tears it down. Do not reuse a live stack for that gate, and do not run it while you intend to keep `:18080` for interactive work.

When `:18080` is down and authenticated product evidence is required, start the
profile with:

```bash
pnpm compose:up
```

The `compose:*` scripts in root `package.json` delegate to
`build/scripts/authenticated-browser-profile.sh`. See [workspace
development](workspace.md#oidc-authenticated-browser) for the full lifecycle.

The required local/CI OIDC gate is `pnpm verify:oidc`. It fails when Docker or
the Playwright browser is missing and always tears down Compose plus generated
secret material. Canonical Playwright uses the shipped `web/` SPA image at
`http://localhost:18080`. The named non-Production overlay uses
`compose:candidate` and Vite at `http://localhost:5274`.

Use its canonical `http://localhost:18080` browser origin and exact
`http://localhost:18080/auth/callback` redirect so Playwright, the SPA, API,
and Keycloak exercise one documented gateway contract.
The synthetic `/browser` adapter remains a bounded presentation/test harness
and cannot substitute for authenticated product evidence.

After cloning, open the repository in Cursor or trust it in Codex, then enable the project MCP server if prompted. Codex only loads project `.codex/config.toml` settings for trusted repositories. `npx` downloads the pinned Playwright MCP package on first use. Upgrade both MCP pins together and run a browser smoke test. If a browser binary is missing, follow the MCP server’s install prompt before testing.

## Implementation workflow

Codex and Cursor use equivalent `implementation-workflow` skills (`.agents/skills/` and `.cursor/skills/`).

- **Cursor:** always-on rule `.cursor/rules/06-implementation-workflow.mdc`
- **Codex:** matching section in `AGENTS.md`
- **Both:** load `implementation-workflow` for substantive implementation work

Shared live state lives in `.work/active/<task-slug>.md`. Copy `.work/templates/implementation-plan.md` when starting a new task. See `.work/README.md` for naming, lifecycle, progress markers, review handoff, and retirement.

Executable workspace commands and gate coverage for the current scaffold live in [`workspace.md`](workspace.md).

`.work/` is Git-tracked, non-authoritative execution state so external reviewers can inspect implementation plans and evidence and maintainers can retain completed task history. Governing specs, ADRs, code, and tests remain authoritative. The workflow composes role skills and specification-driven TDD; it does not replace them. Trivial one-step edits do not require a task file. Any tracked file under `.work/`, including `active/`, `resources/`, and `templates/`, must not contain secrets, credentials, sensitive participant data, hidden chain-of-thought, private reasoning, or unnecessary raw output.

All files under `.work/`, including live and retained completed task files, are Git-visible. Keep completed files after completion and external review for implementation tracking. Do not remove them as part of the implementation workflow; repository maintainers may clean them up when they choose.

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

UI/UX designers, frontend developers, frontend reviewers, and testers must
attach to the matching local origin when it is healthy, or start it only when
that origin is down. For every changed journey they should:

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
python3 scripts/check_docs.py
```

The script validates internal links and heading fragments (including the repository root `README.md`), deprecated terms, duplicate requirement IDs, Mermaid fence balance, the current P0 feature catalog, `docs/current-state.md`, snapshot-first `.work` hygiene, and rejects stale historical-authority patterns. Placeholder specs, ADR files, and UI retirement ledgers are not required catalog members.

GitHub Actions runs the same checks on pull requests and pushes to `main` via [`.github/workflows/docs.yml`](../../.github/workflows/docs.yml). Markdown lint covers `docs/`, `AGENTS.md`, `.cursor/rules/`, `.cursor/skills/`, `.agents/skills/`, `.work/README.md`, and `.work/templates/`.

Anything absent or ambiguous in the approved spec must be handled as one of:

- an open question that blocks or informs a material decision, always with an **interim default** and brief rationale;
- a clearly labeled best-practice proposal (`PROP-*` / `Proposed`) for approval; or
- a reversible implementation detail that does not change observable product behavior.

Do not silently interpret an illustrative product-document example as an MVP commitment.

## Recommended roles to add later

- **Platform/SRE reviewer** before production, to cover SLOs, capacity, disaster recovery, deployment safety, and incident readiness.
- **AI evaluation specialist** when model/harness evaluation datasets and quality gates are defined.
- **Voice/device QA specialist** before voice release, because microphones, speakers, network jitter, echo cancellation, and real interruption timing require device-level testing beyond browser mocks.
