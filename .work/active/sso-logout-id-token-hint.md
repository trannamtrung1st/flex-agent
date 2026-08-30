---
id: sso-logout-id-token-hint
status: completed
created: 2026-08-29
updated: 2026-08-29
---

# Goal

Skip Keycloak's RP-initiated logout confirmation by including `id_token_hint`
on end-session URLs while keeping provider tokens out of APIs and logs.

# Governing sources

- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md` — `STACK-DEC-21`, `STACK-DEC-22`
- `docs/operations/provider-profiles/keycloak-oidc-contract.md`

# Plan

- [x] Red/green: end-session and logout tests expect `id_token_hint`
- [x] Green: encrypt provider ID token at login; return hint on logout
- [x] Migration `0061` for session ciphertext column
- [x] Update ADR, Keycloak contract, and frontend architecture docs
- [x] Run focused tests and live compose verification

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Runtime tests | passed | `dotnet test --project tests/Runtime/FlexAgent.Runtime.Tests` — 287/287 |
| Postgres integration | passed | `dotnet test --project tests/Integration/FlexAgent.Postgres.Integration.Tests` — 357/357 (includes migration `0061` and encrypted ciphertext persistence) |
| Live compose logout (no `#kc-logout`) | passed | Rebuilt API + migrated; Sign out → anonymous gate `.playwright-mcp/page-2026-08-29T11-28-17-696Z.png`; next **Continue to sign in** shows Keycloak login form (SSO ended) |
| End-session unit tests | passed | `id_token_hint` in `TryBrowserEndSessionUrl` |
| Coordinator logout/rotation hint tests | passed | `Logout_returns_the_encrypted_provider_id_token_hint`, `Rotation_preserves_provider_id_token_hint_for_logout` |
| Playwright OIDC sign-out selectors | passed | `signOutThroughProductionChrome` helper; canonical + candidate specs use operator `menuitem` |
| Re-review pass (2026-08-29) | passed | Stale `.work/` notes updated; `apiUrl()` for IPv4 API requests; `resolveRepoRoot()` for helper scripts. Runtime 287/287; Postgres 357/357 |
| Live Playwright canonical (post-reset) | passed | `FLEXAGENT_OIDC_ORIGIN=http://localhost:18080` — **7/7** including `OIDC-E2E-03` local logout (no `#kc-logout`) |
| Live Playwright candidate | passed | `OIDC-CANDIDATE-01` with candidate overlay + Vite `:5274` (sign-out via operator menuitem, frictionless logout) |
| Harness docs (candidate footgun) | passed | `development-harness.md`, `workspace.md`, Keycloak contract, `AGENTS.md`, Playwright MCP rule |
| `pnpm verify:oidc` full matrix | not run | Harness rule: disposable stack only; individual gates exercised separately |

# Completion

- [x] Planned work reconciled with changes
- [x] Focused and integration tests pass
- [x] Governing docs updated (`STACK-DEC-21`, Keycloak contract, frontend architecture)
- [x] Live compose verification recorded
