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
- `docs/contributing/development-harness.md` (authenticated-browser Compose profile)
- Authenticated-browser Compose contract (`build/scripts/validate-authenticated-browser-compose.py`)

# Scope

## In

- Prebuilt-image Compose overlay and `up-smoke` so local CI tags are never pulled from a registry
- OCI workflow image build/load so loaded images are runnable on the daemon
- CI Playwright browser install with OS libraries
- Compose daemon HTTP timeouts and digest-pinned infra pre-pull so cold GHA runners can start Keycloak/Postgres
- Keycloak readiness wait and secret-safe failure diagnostics before migrate/app start
- Generated bind-mount permission model so non-root Keycloak and API can read realm/secret files
- Focused tests for the smoke contract

## Out

- Full local `pnpm verify:oidc` duration/coverage
- Node 20 deprecation warnings on third-party actions
- Idempotent Compose `down` when `.generated/keycloak.env` is already deleted (P3 teardown noise)
- Keycloak image, hostname, realm-import semantics, Buildx, pull policy, or application behavior unless new evidence requires it

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
- [x] Red: tests for generated bind-mount permission contract
- [x] Green: realm 0644, secrets dir 0755, secret 0644, .generated 0700, keycloak.env 0600
- [x] Focused tests + static OIDC gate
- [x] Confirmation pass: scope `umask 077` to a subshell; lock Compose `:ro` mounts in tests
- [x] Live GHA `oci-oidc-smoke` on `f06b1df` (run `33591149691`)

# Current state

Resolved. Head `f06b1df`. Implementation run `33591149691` is green, including live `oci-oidc-smoke`: Keycloak and API healthy, Playwright `OIDC-E2E-01` PKCE login 1 passed (3.3s), `verify:oidc:ci complete`. Review approved `fa78c2e` and `f06b1df`. Durable truth is in the profile script, renderer, Compose `:ro` mounts, and tests. This task file is retired after review.

# Decisions

- Keep digest-pinned pulls for Postgres/Keycloak/SDK; never-pull only API/SPA CI tags.
- Load OCI images with the Docker driver (no `platforms:` on load); assert `linux/amd64` after load.
- Raise Compose/Docker client timeouts to 300s and pre-pull digest-pinned images (Docker Hub via `mirror.gcr.io` fallback) before `up`.
- Keep Keycloak readiness diagnostics from `cb45f6c` / `fbd2e86`.
- Host confidentiality stays on `.generated/` (`0700`) and `keycloak.env` (`0600`); bind-mounted children are container-readable.
- Do not chmod 777, run Keycloak/API as root, or dump container env in diagnostics.
- Do not make further OCI/OIDC runtime changes for this incident. Teardown `keycloak.env not found` is leftover trap-then-workflow `down` noise; leave it unless cleanup code is touched later.

# Findings / deviations

- Live GHA `170e898` pulled infra images and ran migrations through `0062`; Keycloak then exited 1. Prior timeout/pre-pull work did not address that.
- App-tier failure logs omitted Keycloak, and cleanup deleted the container before evidence survived.
- Run `33589718112` confirmed Keycloak reached realm import and failed on `Permission denied` for the `0600` bind-mounted realm. API `appuser` (UID 10001) would hit the same class of failure on `.generated/secrets`.
- Confirmation pass: process-wide `umask 077` after creating `keycloak.env` could leak onto later files in the same shell; chmod 644 still corrected realm/secret, but the umask is now scoped to a subshell.
- Successful live path on `33591149691` is not a skipped-test green: Keycloak healthy, API healthy, `OIDC-E2E-01` passed.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Live GHA `oci-oidc-smoke` `33589718112` | fail | Keycloak realm import Permission denied |
| AuthenticatedBrowserProfileTests bind-mount contract | pass | 21/21 including umask subshell and Compose `:ro` |
| `python3.12 scripts/test_authenticated_browser_compose.py` | pass | includes umask subshell and `:ro` checks |
| `FLEXAGENT_OIDC_SKIP_LIVE=1 bash build/scripts/verify-oidc-ci.sh` | pass | static complete |
| Documentation run `33591149693` | pass | Success |
| Implementation run `33591149691` | pass | Success; `dotnet`, `web`, `oidc`, `supply-chain`, OCI builds |
| Live GHA `oci-oidc-smoke` `33591149691` on `f06b1df` | pass | Keycloak healthy; API healthy; Playwright `OIDC-E2E-01` 1 passed (3.3s); `verify:oidc:ci complete`; Compose teardown ok |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
