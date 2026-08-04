# Cursor Development Harness

This repository uses project-scoped Cursor rules and reusable role skills to support specification analysis, red-green-refactor TDD, independent review, and browser-backed QA.

For product and engineering documentation structure, start at [`docs/README.md`](README.md).

## What is installed

Persistent rules under `.cursor/rules/` enforce:

- Flex Agent vocabulary and architectural invariants
- Specification-first red-green-refactor development
- Reusable role selection
- Mandatory Playwright MCP screenshot evaluation for UI work
- Security, privacy, participant isolation, and audit defaults

Project skills under `.cursor/skills/` provide these roles:

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

The security/privacy role is intentionally included beyond the requested minimum. This product handles sensitive participant content, consequential evaluations, external tools, dynamic memory, and multi-tenant boundaries; treating those concerns as a late generic review would be unsafe.

## Role and workflow separation

Role skills define capabilities, responsibilities, quality standards, and expected outputs. They are applicable independently and may compose one another when work crosses roles. `documentation-author` explicitly composes `business-analyst`, `architect`, and `ui-ux-designer` perspectives for source-of-truth documents, adding security/privacy review when relevant. Roles do not prescribe a project-specific sequence, handoff, approval path, or release process.

When the project adopts a concrete process—such as feature discovery, implementation, pull-request review, release readiness, or incident response—define that process as a separate workflow skill that invokes the relevant roles. No end-to-end workflow skill is installed yet.

## Playwright MCP

`.cursor/mcp.json` starts the official `@playwright/mcp` server named `playwright`.

- Isolated browser contexts prevent state leaking between QA sessions.
- Screenshots, logs, and other artifacts are written to `.playwright-mcp/`.
- Omit custom screenshot filenames with the pinned MCP version so files cannot bypass the configured output directory.
- `.playwright-mcp/` is gitignored and must never be committed.
- Use synthetic accounts and data; browser artifacts must not contain real participant data or secrets.

After cloning, open the repository in Cursor and enable the project MCP server when prompted. `npx` downloads the pinned Playwright MCP package on first use. Upgrade the pin deliberately after a browser smoke test. If a browser binary is missing, follow the MCP server’s install prompt before testing.

## UI evidence standard

UI/UX designers, frontend developers, frontend reviewers, and testers must use the live app when it is runnable. For every changed journey they should:

1. Reach each applicable state through real interactions.
2. Use accessibility snapshots to inspect names, roles, focus order, and structure.
3. Take desktop and narrow screenshots.
4. Evaluate hierarchy, copy, affordances, spacing, alignment, clipping, feedback, focus, contrast clues, and polish.
5. Repeat after fixes and cite `.playwright-mcp/` evidence.

Typical coverage includes loading, empty, populated, validation, error/retry, pending, disabled, dialogs, destructive confirmation, permission denied, keyboard focus, time warnings, expiry, completion, release, and applicable voice states.

## Specification policy

Narrow approved feature specs outrank the overview. A spec should include stable requirement and acceptance-criterion IDs, actors and permissions, in/out of scope, journeys and state transitions, business rules, data/audit needs, non-functional requirements, failure behavior, dependencies, open questions, and traceability.

Anything absent or ambiguous in the approved spec must be handled as one of:

- a question that blocks a material decision;
- a clearly labeled best-practice proposal for approval; or
- a reversible implementation detail that does not change observable product behavior.

Do not silently interpret an illustrative overview example as an MVP commitment.

## Recommended roles to add later

- **Platform/SRE reviewer** before production, to cover SLOs, capacity, disaster recovery, deployment safety, and incident readiness.
- **AI evaluation specialist** when model/harness evaluation datasets and quality gates are defined.
- **Voice/device QA specialist** before voice release, because microphones, speakers, network jitter, echo cancellation, and real interruption timing require device-level testing beyond browser mocks.
