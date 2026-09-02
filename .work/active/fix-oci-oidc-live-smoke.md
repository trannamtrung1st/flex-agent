---
id: fix-oci-oidc-live-smoke
status: in-progress
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
- Compose daemon HTTP timeouts and digest-pinned infra pre-pull so cold GHA runners can start Keycloak/Postgres
- Keycloak readiness wait and secret-safe failure diagnostics before migrate/app start
- Focused tests for the smoke contract

## Out

- Full local `pnpm verify:oidc` duration/coverage
- Node 20 deprecation warnings on third-party actions
- Keycloak image, hostname, or realm-import changes until diagnostics show the exit cause

# Plan

- [x] Inspect failed run #455 and smoke scripts
- [x] Red: tests for pull_policy / no registry pull / CI Playwright deps
- [x] Green: overlay, profile, workflow, verify-oidc-ci
- [x] Focused tests + static OIDC gate
- [x] Red: tests for Compose HTTP timeout, digest pre-pull, infra failure logs
- [x] Green: timeouts, Hub-mirror fallback pull, diagnostics
- [x] Focused tests + static OIDC gate
- [x] Red: tests for Keycloak wait and secret-safe diagnostics
- [x] Green: wait_keycloak_healthy + dump_oidc_diagnostics
- [x] Focused tests + static OIDC gate

# Current state

Diagnostic-only change is local-green: `up_smoke` waits for Keycloak health after infra and dumps secret-safe inspect + logs (including Keycloak) before cleanup. Fail-fast only on `exited`/`dead`; Compose ps uses `--all --quiet` so stopped Keycloak is still inspectable. No Keycloak runtime/config change. Job display name is `oci-oidc-smoke`.

# Decisions

- Keep digest-pinned pulls for Postgres/Keycloak/SDK; never-pull only API/SPA CI tags.
- Load OCI images with the Docker driver (no `platforms:` on load); assert `linux/amd64` after load.
- Raise Compose/Docker client timeouts to 300s and pre-pull digest-pinned images (Docker Hub via `mirror.gcr.io` fallback) before `up`.
- Do not change Keycloak image, hostname, or realm import until CI logs show the exit cause.

# Findings / deviations

- Live GHA `170e898` pulled infra images and ran migrations through `0062`; Keycloak then exited 1. Prior timeout/pre-pull work did not address that.
- App-tier failure logs omitted Keycloak, and cleanup deleted the container before evidence survived.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Live GHA `oci` on `170e898` | fail | Keycloak exit 1 after successful pulls/migrate/seed |
| AuthenticatedBrowserProfileTests | pass | 19/19, including Keycloak wait/diagnostics |
| `python3.12 scripts/test_authenticated_browser_compose.py` | pass | validator negatives ok |
| `FLEXAGENT_OIDC_SKIP_LIVE=1 bash build/scripts/verify-oidc-ci.sh` | pass | static complete |
| Live GHA Keycloak logs after this change | pending | needs push |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
