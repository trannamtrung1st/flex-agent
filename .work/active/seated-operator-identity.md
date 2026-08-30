---
id: seated-operator-identity
status: completed
created: 2026-08-29
updated: 2026-08-29
---

# Goal

Project the seated operator’s given and family name into production chrome, replacing the `ORG` / `Organization` stub. Organization stays omitted from this control until a real org display name exists.

# Governing sources

- Owner-confirmed chrome brief (this task): glyph + given/family name + role; later glyph-only density; no photo avatars
- `docs/ui-ux/design-system/components/avatars.md`
- `docs/requirements/features/auth-resource-isolation.md`
- `docs/operations/provider-profiles/keycloak-oidc-contract.md`
- ADR-010 `STACK-DEC-26`
- `PC-10`

# Scope

## In

- Compose seated display name from validated ID-token `given_name` + `family_name`, else `preferred_username`
- Request OIDC `profile` so those claims are present
- Persist name on the application session; project `display_name` on `GET /v1/assessment/shell`
- Production `ProfileMenu` shows name + role; omit Organization
- Empty fallback: username, then role only

## Out

- Org switcher / org display name
- Photographic avatars / glyph-only density mode
- Profile editing
- Lab callsign identities (`CND-8842` etc.)

# Plan

- [x] Domain composer + OIDC claim capture (TDD)
- [x] Persist seated display name on application sessions (`0062`) and return it from Authenticate
- [x] Shell `display_name` contract
- [x] Production chrome mapping + ProfileMenu layout/CSS
- [x] Docs: avatars module + Keycloak profile scope
- [x] Focused tests and live-browser verification

# Current state

Completed. Live Compose API rebuilt with migration `0062`. Candidate overlay is on so Vite `:5274` OIDC still matches this stack.

# Decisions

- IdentityAccess projects the name; the SPA does not read Keycloak.
- Capture at login from the validated ID token; store on the session (not the actor).
- Existing sessions without a name stay role-only until re-login.

# Findings / deviations

- Local API was recreated with the candidate RedirectUri so `:5274` sign-in could be verified. Canonical `:18080` callback is not the current API setting.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Domain/OIDC/coordinator/runtime shell | passed | 59 runtime tests (`SeatedOperatorDisplayNameTests`, `OidcIdTokenValidatorTests`, `HumanAuthenticationCoordinatorTests`, `HumanAuthenticationRuntimeTests`, `AssessmentHttpNegativeContractTests`) |
| Frontend unit | passed | `production-operator.test.ts`, `ProductionAppShell.test.tsx` (5 tests) |
| Impeccable detect | passed | `[]` on ProfileMenu, chrome.css, ProductionAppShell, AssignmentStationLayout |
| Playwright MCP chrome | passed | Closed strip `.playwright-mcp/page-2026-08-29T14-08-09-528Z.png`; open menu `.playwright-mcp/page-2026-08-29T14-10-35-817Z.png`; narrow `.playwright-mcp/page-2026-08-29T14-11-10-931Z.png`. Menu text: `ADMINISTRATOR` / `Demo Administrator` / theme / sign out. No `ORG`. |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
