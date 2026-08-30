---
id: seed-numbered-demo-accounts
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Add extra local synthetic login identities next to the existing usernames:
`demo.admin1`–`demo.admin5` and `demo.participant1`–`demo.participant30`, each
bound in IdentityAccess like the original `demo.admin` and `demo.participant`.

# Governing sources

- `deploy/compose/keycloak/flex-agent-realm.json`
- `deploy/compose/authenticated-browser/seed.sql`
- `docs/contributing/workspace.md`
- ADR-010 authenticated-browser profile (synthetic identities)

# Scope

## In

- Keycloak realm template users (same shared local password as existing demo users)
- PostgreSQL identity seed: actors, issuer/subject bindings, org grants, participant display labels
- Compose contract tests and workspace username documentation

## Out

- Changing original `demo.admin` / `demo.participant` ids used by OIDC Playwright
- Enrolling numbered participants into demo-work campaigns
- Production identity provisioning

# Plan

- [x] Create task file
- [x] Add failing profile tests for numbered usernames and seed bindings
- [x] Add Keycloak users and seed.sql identity rows
- [x] Update workspace docs (usernames only)
- [x] Run focused tests and compose validator

# Current state

Reviewed and applied to the live stack without compose:reset: profile `seed`
plus Keycloak `partialImport` (SKIP existing). Password-grant probes confirm
numbered usernames, subjects, and names.

# Decisions

- Keep original `demo.admin` and `demo.participant` unchanged for existing tests.
- Numbered extras use `demo.admin{n}` / `demo.participant{n}` (n with no separator).
- Stable UUID prefixes: admin extras `d2000000-...` / `a2000000-...` / `b2000000-...`;
  participant extras `e2000000-...` / `a3000000-...` / `b3000000-...`.

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| AuthenticatedBrowserProfileTests | passed | 11/11 after green |
| validate-authenticated-browser-compose.py (realm) | passed | `authenticated-browser compose contract ok` |
| Live SQL apply (`profile seed`) | passed | 5 admin actors/bindings, 105 admin grants (5×21), 30 participant actors/bindings, 60 receive/discover grants, 30 display labels |
| Live Keycloak partialImport | passed | before=5 users, added=35, after=40; `demo.admin1`/`demo.participant30` ids match seed subjects |
| Password-grant token probe | passed | `demo.admin1`, `demo.admin5`, `demo.participant1`, `demo.participant30`, originals: HTTP 200, `sub` matches Keycloak id |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
