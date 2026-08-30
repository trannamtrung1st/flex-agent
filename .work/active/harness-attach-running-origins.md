---
id: harness-attach-running-origins
status: completed
created: 2026-08-29
updated: 2026-08-29
---

# Goal

Tell Cursor and Codex agents to probe and attach to a healthy local origin before starting Compose or Vite, without changing `compose:up` freshness or `verify:oidc` teardown.

# Governing sources

- `docs/contributing/development-harness.md`
- `docs/contributing/workspace.md` (OIDC origins and commands)
- `.cursor/rules/03-playwright-mcp.mdc` and `AGENTS.md` Playwright section
- STACK-DEC-27 canonical origin `http://localhost:18080`

# Scope

## In

- Canonical attach-first procedure in contributing docs
- Always-on Playwright MCP rule and Codex `AGENTS.md` equivalent
- Role skills that start or screenshot the live UI (mirrored `.agents/` and `.cursor/`)

## Out

- New `compose:ensure` command
- Changing `compose:up` to reuse volumes/secrets
- Changing `verify:oidc` lifecycle
- Impeccable live-mode internals

# Plan

- [x] Write attach-first procedure in development-harness.md
- [x] Mirror into Playwright rule, AGENTS.md, workspace.md, UI role skills
- [x] Re-review: OIDC contract attach, IPv4/IPv6 probe, live attach evidence

# Current state

Completed. Agents attach via `compose:status` / origin HTTP probes; `compose:up` remains a fresh start.

# Decisions

- One authoritative procedure in `docs/contributing/development-harness.md`; always-on Playwright surfaces restate a condensed table so agents see it without opening the doc.
- `compose:up` remains a destructive fresh start.
- No `compose:ensure` command in this cut.

# Findings / deviations

- `python3 scripts/check_docs.py` still fails on pre-existing `DESIGN.md` adapter drift unrelated to this task.
- Review pass 2026-08-29: Vite on this Mac listens on `::1` only. Probing only `127.0.0.1:5275` reported down while `localhost:5275` was serving the design lab. Guidance now probes both loopback forms.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Heading fragments | passed | `attach-to-a-running-local-origin` and `oidc-authenticated-browser` resolve |
| `pnpm compose:status` | passed | 2026-08-29: services healthy, `session-endpoint:ok`; `http://localhost:18080/auth/session` HTTP 200 |
| Candidate Vite `:5274` | passed | HTTP 200 (already running; not started by this task) |
| `localhost:5275` vs `127.0.0.1:5275` | passed | `::1:5275` listen; `localhost` HTTP 200; `127.0.0.1` connection refused |
| Playwright attach `:18080` | passed | Existing Compose origin; anonymous SIGN IN REQUIRED; `.playwright-mcp/page-2026-08-29T08-49-21-982Z.png` |
| OIDC contract attach link | passed | `#attach-to-a-running-local-origin` resolves |
| `.agents` / `.cursor` skill pairs | passed | `diff -q` equal for updated skills |
| `python3 scripts/check_docs.py` | skipped (pre-existing) | Fails on `DESIGN.md` drift; not introduced by this change |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
