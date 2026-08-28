---
id: demo-seed-accounts
status: completed
created: 2026-08-28
updated: 2026-08-28
---

# Goal

Rename local Keycloak seed identities from `synthetic.*` to `demo.*` with a shared
local password. Application DB bindings remain on stable Keycloak user UUIDs; no
PostgreSQL schema migration.

# Governing sources

- Approved plan: demo seed account rename
- `deploy/compose/keycloak/flex-agent-realm.json`
- `deploy/compose/authenticated-browser/seed.sql`
- `docs/contributing/workspace.md`

# Scope

## In

- Keycloak realm template usernames, emails, credentials
- Compose validator, runtime contract tests, OIDC Playwright helpers, Keycloak logout test
- Workspace documentation (username only; credentials live in realm file)

## Out

- Session payload fixture `synthetic.participant.message`
- SyntheticBrowser stage id `actor.synthetic.participant`
- PostgreSQL migrations (bindings use stable `sub` UUIDs)

# Plan

- [x] Create task file
- [x] Update realm JSON and seed.sql comments
- [x] Update validator, tests, docs, oidc helpers
- [x] Run gitleaks; add path-scoped allowlist if flagged (no leaks; allowlist not needed)
- [x] Run focused tests, compose:reset, and login/OIDC checks

# Target identities

| Role | Username | Email | Keycloak user id |
| --- | --- | --- | --- |
| Administrator | `demo.admin` | `demo.admin@example.test` | `dddddddd-dddd-4ddd-8ddd-dddddddddddd` |
| Participant | `demo.participant` | `demo.participant@example.test` | `eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee` |
| Unbound | `demo.unbound` | `demo.unbound@example.test` | `ffffffff-ffff-4fff-8fff-ffffffffffff` |
| Zero-org | `demo.zeroorg` | `demo.zeroorg@example.test` | `11111111-1111-4111-8111-111111111111` |
| Ambiguous org | `demo.ambiguous` | `demo.ambiguous@example.test` | `22222222-2222-4222-8222-222222222222` |

Shared password: stored only in `deploy/compose/keycloak/flex-agent-realm.json` and
test default constants (not recorded here).

# Current state

Implementing realm and consumer updates.

# Decisions

- Keep Keycloak user UUIDs and `human_identity_bindings.subject` unchanged.
- Username is short form (`demo.admin`); email uses `@example.test` domain.

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `AuthenticatedBrowserProfileTests` | passed | 8/8 via `--filter-class FlexAgent.Runtime.Tests.AuthenticatedBrowserProfileTests` |
| `validate-authenticated-browser-compose.py` | passed | `authenticated-browser compose contract ok` |
| `gitleaks detect` | passed | No leaks found |
| `check_docs.py` | passed | Documentation validation passed |
| `compose:up` + browser login | passed | `demo.admin` and `demo.participant` OIDC sign-in at `http://localhost:18080` after fresh `compose:down`/`compose:up` |

# Blockers

None.

- Removed invalid `postLogoutRedirectUris` from realm template (Keycloak 26.7 import rejects it; post-logout URIs remain in `attributes.post.logout.redirect.uris`).

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
