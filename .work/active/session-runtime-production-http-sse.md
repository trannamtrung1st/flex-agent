---
id: session-runtime-production-http-sse
status: planned
created: 2026-08-17
updated: 2026-08-17
predecessor: structured-agent-runtime-sync
---

# Goal

Host production HTTP Session event subscription so authorized reconnect and
multi-device replay use `ReplayAuthorizedSessionEventsCommand` over PostgreSQL,
not the synthetic `/browser` adapter. Complete `REQ-SESS-59` on the API host
with ADR-002 kernel enforcement and 60-second revocation revalidation.

# Governing sources

- `.work/active/structured-agent-runtime-sync.md` — completed foundation;
  replay command and Repeatable Read coordinator exist and are tested
- `docs/requirements/features/session-text-lifecycle.md` — `REQ-SESS-59`,
  `AC-SESS-32` (Partial until this host path exists)
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`
- `docs/architecture/decisions/ADR-011-participant-visible-agent-response-streaming.md`
- `docs/architecture/session-runtime-contract.md` — `SESS-DEC-7`, `SESS-DEC-10`,
  `SESS-DEC-13`
- `docs/ui-ux/text-session.md` — Participant reconnect copy; do not expose
  Decision/timer internals

# Scope

## In

- API inbound adapter: `GET /sessions/{id}/events` (or the approved contract
  path) mapping trusted actor + ownership + untrusted `Last-Event-ID` to
  `ReplayAuthorizedSessionEventsCommand`
- Deny by default; never treat `Last-Event-ID` as identity or authorization
- ADR-002 kernel at subscribe and at least every 60 seconds while held
- Held connection completes on revoke; no cross-session leak
- Compose Sessions on the API host without promoting SyntheticBrowser to
  domain authority
- Negative tests: guessed Session ID, stolen cursor without cookie/actor,
  wrong ownership, reviewer vs participant, malformed/future cursor
- When the Participant UI is pointed at this host path, Playwright evidence
  for reconnect; until then keep synthetic as the non-authoritative harness

## Out

- Worker claim/content-loop wiring (see `session-runtime-worker-host-wiring`)
- Live providers, OIDC product login if still deferred (use trusted test
  actors until identity work is in scope)
- Backup/restore/export labs
- Rewriting frozen migrations `0005`–`0019`
- Treating synthetic `/browser/.../events` as `REQ-SESS-59` complete

# Plan

- [ ] Confirm replay command/coordinator contracts and SSE event catalog
- [ ] Red: API has no production Session events route / kernel
- [ ] Green: authorized subscribe + replay + 60s revalidation + revoke
- [ ] Isolation and cursor tests; Playwright if UI is switched to this path

# Current state

`FlexAgent.Api` maps synthetic browser endpoints only. Sessions replay exists
in application + PostgreSQL tests. Feature spec marks `REQ-SESS-59` Partial.

# Decisions

- Synthetic `/browser` remains a harness. Production SSE is a distinct host
  adapter over the existing replay command.

# Findings / deviations

- None yet.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Production route uses replay command + PostgreSQL | pending | |
| Last-Event-ID is not authorization | pending | |
| 60s revocation revalidation | pending | |
| Cross-session leak tests | pending | |

# Blockers

None. Do not start until this task is explicitly prioritized. Prefer completing
or sequencing with worker-host wiring so hosted fragments exist to replay.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [ ] Governing specifications were rechecked (`REQ-SESS-59` status truthful)
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
