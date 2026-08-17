---
id: session-runtime-production-http-sse
status: completed
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
  `AC-SESS-32` (combined row remains Partial: UI still synthetic, OIDC deferred)
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`
- `docs/architecture/decisions/ADR-011-participant-visible-agent-response-streaming.md`
- `docs/architecture/session-runtime-contract.md` — `SESS-DEC-7`, `SESS-DEC-10`,
  `SESS-DEC-13`
- `docs/ui-ux/text-session.md` — Participant reconnect copy; do not expose
  Decision/timer internals

# Scope

## In

- API inbound adapter: `GET /sessions/{id}/events` mapping trusted actor +
  server-side ownership + untrusted `Last-Event-ID` to
  `ReplayAuthorizedSessionEventsCommand`
- Deny by default; never treat `Last-Event-ID` as identity or authorization
- ADR-002 kernel at subscribe and at least every 60 seconds while held
- Held connection completes on revoke; no cross-session leak
- Compose Sessions on the API host without promoting SyntheticBrowser to
  domain authority
- Negative tests: guessed Session ID, stolen cursor without actor, wrong
  ownership, reviewer vs participant, malformed/future cursor
- Keep synthetic `/browser` as the Participant UI harness (no UI switch)

## Out

- Worker claim/content-loop wiring (see `session-runtime-worker-host-wiring`)
- Live providers, OIDC product login (trusted test actors only)
- Backup/restore/export labs
- Rewriting frozen migrations `0005`–`0019`
- Treating synthetic `/browser/.../events` as `REQ-SESS-59` complete
- Pointing `SessionPage` at production SSE (Playwright reconnect evidence)

# Plan

- [x] Confirm replay command/coordinator contracts and SSE event catalog
- [x] Red: API has no production Session events route / kernel
- [x] Green: authorized subscribe + replay + 60s revalidation + revoke
- [x] Isolation and cursor tests; Playwright skipped (UI not switched)

# Current state

`GET /sessions/{id}/events` is hosted on `FlexAgent.Api`. When
`ConnectionStrings:Sessions` is set, the host composes PostgreSQL replay,
`PostgresAuthorizationKernel` (`session.events.subscribe`), and
`SubscribeAuthorizedSessionEventsHandler`. Without that connection string the
route exists and fails closed (401 without a trusted actor, 404 when the
actor is known but subscription is not permitted).

Trusted test-actor header `X-Flex-Test-Actor-Id` maps through a server-side
directory (organization, relationship, participant id). `Last-Event-ID` is a
cursor only. Participant UI remains on `/browser`. Combined spec rows stay
Partial because OIDC and production UI wiring are still later.

# Decisions

- Synthetic `/browser` remains a harness. Production SSE is a distinct host
  adapter over the existing replay command.
- Until OIDC, identity is a server-registered test actor, not client-supplied
  organization or ownership fields.
- Hosted binding lookup uses `ITrustedSessionBindingSource` keyed by trusted
  organization + untrusted session id. An empty memory source fails closed
  until frozen-policy rehydration exists.

# Findings / deviations

- Red was compile-fail for missing `SubscribeAuthorizedSessionEventsCommand`
  (`dotnet test` Sessions subscribe class, 2026-08-17).
- Self-review: `ReplayAsync` also calls the kernel so a poll cannot disclose
  after revoke even if the 60s timer has not fired; HTTP maps deny vs
  reconcile to `: access-revoked` vs `: reconcile`.
- Self-review: actor directory uses `ConcurrentDictionary`.
- Second review (2026-08-17): held-loop poll now drains `HasMore` and closes
  on reconcile/deny, matching the initial replay path. Tests send
  `SessionEventEndpointExtensions.TestActorHeaderName`.
- Confirmation pass (2026-08-18): Runtime tests csproj ItemGroup was repaired
  after a concatenated close-tag; subscribe 7/7, production SSE+API 12/12,
  composition/Dockerfile 6/6, `git diff --check` and `check_docs.py` passed.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Production route uses replay command + PostgreSQL | passed | `SessionRuntimeProductionSubscribeTests` 2/2; API composition when `ConnectionStrings:Sessions` is set |
| Last-Event-ID is not authorization | passed | Stolen cursor without actor → 401; malformed cursor reconciles with no fragment text |
| 60s revocation revalidation | passed | Held SSE completes after kernel deny; Postgres grant revoke then `AuthorizeAsync` denies |
| Cross-session leak tests | passed | Guessed session / reviewer / wrong participant leak no events |
| Locked .NET regression | passed | `bash build/scripts/verify-dotnet.sh` **890/890** |
| Docs | passed | `python3 scripts/check_docs.py`; `git diff --check` clean |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked (`REQ-SESS-59` API path hosted;
      combined Session row remains Partial)
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
