---
id: signin-fail-closed-recovery
status: completed
created: 2026-08-29
updated: 2026-08-29
---

# Goal

Fail-closed human login (unbound, zero/ambiguous Organization, and other
callback failures) returns the operator to the production sign-in gate with
a non-disclosing recovery state and ends the provider SSO session when login
was refused after a validated identity, so they can choose another account.

# Governing sources

- `docs/requirements/mvp-operational-defaults.md` — `REQ-OPS-15`, `REQ-OPS-27`, `REQ-OPS-28`, `AC-OPS-4`
- `docs/requirements/features/auth-resource-isolation.md` — `REQ-AUTH-13`, `REQ-AUTH-21`, `AC-AUTH-13`, `AC-AUTH-20`
- `docs/ui-ux/activity-campaign-journey.md` — `PROP-UX-6`, authorization-denied recovery
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md` — `STACK-DEC-19`, `STACK-DEC-21`
- `docs/operations/provider-profiles/keycloak-oidc-contract.md`

# Scope

## In

- `/auth/callback` document failures redirect to the SPA (`/?signin=denied`) instead of raw JSON.
- Identity/Organization fail-closed after a validated ID token also starts RP-initiated provider logout with a matching post-logout URI.
- Auth gate shows a ceremony denial with a safe next action; successful login return paths drop the recovery query.
- Keycloak post-logout URI registration, focused tests, and OIDC-E2E-05A/05B recovery assertions.

## Out

- Auto-provisioning unbound identities
- Client Organization selection
- Distinguishing unknown vs disabled vs ambiguous reasons in the browser
- Changing successful-login or ordinary Sign out contracts beyond post-logout URI registration

# Plan

- [x] Red/green callback redirect and provider end-session after identity denial
- [x] SPA denied gate, return-path stripping, and Vitest coverage
- [x] Realm/post-logout registration and OIDC-E2E-05A/05B recovery
- [x] Promote recovery copy into the activity journey and Keycloak contract
- [x] Verify focused tests, Playwright MCP, and record evidence

# Current state

Completed. Callback failures redirect into the SPA; identity denials also
start provider logout. Candidate API was rebuilt and live Keycloak
post-logout URIs were updated.

# Decisions

- Coarse query `signin=denied` only; reason codes stay in audit.
- Provider logout after validated identity denial only; protocol/validation failures redirect to the SPA without ending SSO.
- Recovery copy is one non-disclosing sentence for unknown, disabled, zero-Organization, and ambiguous-Organization denials.

# Findings / deviations

- Authenticated sessions ignore `?signin=denied` and stay on Home (gate is idle-only). That is correct.
- Zero-Organization and ambiguous Organization use the same coordinator-denial redirect as unknown subject.
- Identity-specific “not set up” copy was too narrow for zero-Organization; copy is now generic.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Runtime human-auth | passed | 38 tests including unknown, zero-org, ambiguous, and disabled HTTP callbacks |
| Vitest auth gate | passed | `App.test.tsx` + `signin-completion.test.ts` 8 passed |
| Playwright MCP unbound | passed | Callback → Keycloak logout `/?signin=denied`; SPA `.playwright-mcp/page-2026-08-29T05-30-41-886Z.png`; narrow `05-30-56-278Z.png` |
| Playwright MCP admin after denial | passed | Home as administrator `.playwright-mcp/page-2026-08-29T05-32-58-009Z.yml` heading Home |
| `pnpm verify:oidc` | not run | Canonical Playwright job not re-executed |

# Blockers

None.

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
