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

- General provider token retention outside amended `STACK-DEC-21` (see
  `sso-logout-id-token-hint` for the bounded encrypted ID-token ciphertext used
  only as logout `id_token_hint`)
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
URL with `id_token_hint` when available; the SPA follows it, SSO ends without
`#kc-logout` confirm, and a second login can bind a different synthetic actor.
(See `sso-logout-id-token-hint` for the 2026-08-29 amendment.)

# Decisions

- HTTP end-session is allowed only when HTTPS is not required and the
  endpoint host is loopback (`localhost`, `127.0.0.1`, `::1`).
- `post_logout_redirect_uri` is derived from the configured `RedirectUri`
  origin (`/`), never from the browser.
- Keycloak 26.7.0 rejects first-class `postLogoutRedirectUris`; the realm
  uses the client attribute `post.logout.redirect.uris` with `##`.
- **Superseded 2026-08-29** (`sso-logout-id-token-hint`): logout confirmation
  without `id_token_hint` is no longer accepted behavior. The API now stores an
  encrypted provider ID token for RP-initiated logout and includes
  `id_token_hint` on end-session URLs per amended `STACK-DEC-21`.

# Findings / deviations

- Keycloak 26.7.0 import failed on `postLogoutRedirectUris`; attribute-only
  configuration is required.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| HostOptions / logout HTTP tests | passed | 27 tests across EndSession, Runtime, Profile |
| SPA logout next-location tests | passed | `vitest` 16/16 (`production-logout` + `production-routes`) |
| `python3 scripts/check_docs.py` | passed | Documentation validation passed |
| Live Sign out (no SSO rebind) | passed (2026-08-28; confirm superseded 2026-08-29) | Administrator Sign out → login form → participant Home. Pre-`id_token_hint` flow used Keycloak confirm. Post-`id_token_hint`: direct **Sign in required** — see `sso-logout-id-token-hint` |
| Confirmation pass 2026-08-28 | superseded 2026-08-29 | `#kc-logout` was expected before `id_token_hint`; frictionless logout is now required per `STACK-DEC-21` amendment |
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
