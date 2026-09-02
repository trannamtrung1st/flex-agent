---
id: fix-oci-oidc-live-smoke
status: completed
created: 2026-09-02
updated: 2026-09-02
---

# Goal

Make the Implementation `oci` job OIDC live smoke succeed on GitHub Actions after images are loaded into the runner daemon.

# Governing sources

- `docs/contributing/workspace.md` (CI linux/amd64 OCI builds, `pnpm verify:oidc` / CI smoke)
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md` (`linux/amd64` blocking CI)
- Authenticated-browser Compose contract (`build/scripts/validate-authenticated-browser-compose.py`)

# Scope

## In

- Prebuilt-image Compose overlay and `up-smoke` so local CI tags are never pulled from a registry
- OCI workflow image build/load so loaded images are runnable on the daemon
- CI Playwright browser install with OS libraries
- Focused tests for the smoke contract

## Out

- Full local `pnpm verify:oidc` duration/coverage
- Node 20 deprecation warnings on third-party actions

# Plan

- [x] Inspect failed run #455 and smoke scripts
- [x] Red: tests for pull_policy / no registry pull / CI Playwright deps
- [x] Green: overlay, profile, workflow, verify-oidc-ci
- [x] Focused tests + static OIDC gate

# Current state

Confirmed locally. Ready for required review of the snapshot; live GHA `oci` smoke remains the unverified path until CI runs after push.

# Decisions

- Keep digest-pinned pulls for Postgres/Keycloak/SDK; never-pull only API/SPA CI tags.
- Load OCI images with the Docker driver (no `platforms:` on load); assert `linux/amd64` after load.

# Findings / deviations

- GitHub job logs require sign-in; diagnosis is from workflow/scripts plus the public annotation (step 12, OIDC live smoke, exit 1).

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| AuthenticatedBrowserProfileTests | pass | 17/17 after overlay, profile, workflow, and Playwright-deps assertions |
| `python3.12 scripts/test_authenticated_browser_compose.py` | pass | validator negatives ok |
| `FLEXAGENT_OIDC_SKIP_LIVE=1 bash build/scripts/verify-oidc-ci.sh` | pass | static complete (~3.6s) |
| Live GHA `oci` OIDC smoke | pending | not runnable here; needs push to Implementation |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
