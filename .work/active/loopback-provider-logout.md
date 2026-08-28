---
id: loopback-provider-logout
status: completed
created: 2026-08-28
updated: 2026-08-28
---

# Goal

Make production Sign out end the Keycloak SSO session on the local
authenticated-browser profile so the next login can choose a different
synthetic actor. Production HTTPS-only end-session rules stay fail-closed.

# Governing sources

- `docs/requirements/mvp-operational-defaults.md` — `REQ-OPS-15`
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
  — `STACK-DEC-21`, `STACK-DEC-22`, `STACK-DEC-27`
- `docs/operations/provider-profiles/keycloak-oidc-contract.md`
- `docs/architecture/frontend-architecture.md` — logout cache/session teardown

# Scope

## In

- Return a browser-safe `end_session_url` for HTTPS and for HTTP loopback
  when `RequireHttpsEndpoints` is false (Development/Testing).
- SPA follows that URL, including HTTP loopback; reject other HTTP and
  non-https schemes.
- Register Keycloak `post.logout.redirect.uris` for the canonical and
  candidate origins so RP-initiated logout can return to the SPA sign-in
  gate.
- Prove Sign out no longer SSO-rebinds the previous actor.

## Out

- Retaining provider ID tokens (`STACK-DEC-21`)
- Client-selected role or Organization switching
- Production HTTP end-session

# Plan

- [x] Red/green: HostOptions, logout HTTP, SPA next-location, realm attribute
- [x] Green: allow loopback HTTP end-session and post-logout redirect
- [x] Update OIDC-E2E-03 / OIDC-CANDIDATE-01 to complete RP-initiated logout
- [x] Focused tests and live Sign out on candidate Vite
- [x] Record evidence and remaining gaps

# Current state

Completed. Local Sign out returns the Keycloak HTTP loopback end-session
URL, the SPA follows it, Keycloak confirmation ends SSO, and a second login
can bind a different synthetic actor.

# Decisions

- HTTP end-session is allowed only when HTTPS is not required and the
  endpoint host is loopback (`localhost`, `127.0.0.1`, `::1`).
- `post_logout_redirect_uri` is derived from the configured `RedirectUri`
  origin (`/`), never from the browser.
- Keycloak 26.7.0 rejects first-class `postLogoutRedirectUris`; the realm
  uses the client attribute `post.logout.redirect.uris` with `##`.
- Keycloak logout confirmation without `id_token_hint` is accepted.

# Findings / deviations

- Keycloak 26.7.0 import failed on `postLogoutRedirectUris`; attribute-only
  configuration is required.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| HostOptions / logout HTTP tests | passed | 27 tests across EndSession, Runtime, Profile |
| SPA logout next-location tests | passed | `vitest` 16/16 (`production-logout` + `production-routes`) |
| `python3 scripts/check_docs.py` | passed | Documentation validation passed |
| Live Sign out (no SSO rebind) | passed | Administrator Sign out → Keycloak confirm → login form → participant Home. Screenshots `.playwright-mcp/page-2026-08-28T16-21-30-628Z.png`, `16-21-51-659Z.png`, `16-22-11-938Z.png`, `16-22-33-793Z.png` |
| Confirmation pass 2026-08-28 | passed | Session remains anonymous on `http://localhost:5274/` (`Continue to sign in`). Narrow Sign out still present. `#kc-logout` confirmed. Validator pins `post.logout.redirect.uris`. Focused tests 27 .NET + 16 Vitest + compose python. |
| `pnpm verify:oidc` full matrix | not run this task | OIDC-E2E-03/CANDIDATE-01 updated; live MCP proved the candidate path |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass (live candidate logout)
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
