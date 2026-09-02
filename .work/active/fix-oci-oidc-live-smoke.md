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
- Focused tests for the smoke contract

## Out

- Full local `pnpm verify:oidc` duration/coverage
- Node 20 deprecation warnings on third-party actions

# Plan

- [x] Inspect failed run #455 and smoke scripts
- [x] Red: tests for pull_policy / no registry pull / CI Playwright deps
- [x] Green: overlay, profile, workflow, verify-oidc-ci
- [x] Focused tests + static OIDC gate
- [x] Red: tests for Compose HTTP timeout, digest pre-pull, infra failure logs
- [x] Green: timeouts, Hub-mirror fallback pull, diagnostics
- [x] Focused tests + static OIDC gate

# Current state

Confirmation pass complete locally. Unsigned CI tags are skipped; digest-pinned infra images pre-pull; Compose/Docker timeouts are 300s with 120s per pull attempt. Ready to push for live GHA `oci` smoke.

# Decisions

- Keep digest-pinned pulls for Postgres/Keycloak/SDK; never-pull only API/SPA CI tags.
- Load OCI images with the Docker driver (no `platforms:` on load); assert `linux/amd64` after load.
- Raise Compose/Docker client timeouts to 300s and pre-pull digest-pinned images (Docker Hub via `mirror.gcr.io` fallback) before `up`.

# Findings / deviations

- GitHub job logs require sign-in; diagnosis is from workflow/scripts plus the public annotation (step 12, OIDC live smoke, exit 1).
- Runs #455 (60s) and #457 (58s) match `COMPOSE_HTTP_TIMEOUT` default 60s; #456 (34s) failed earlier from `--pull never` on nginx.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Live GHA `oci` OIDC smoke (#457) | fail | 58s, no preflight `::error::`; Compose HTTP timeout / Hub pull |
| AuthenticatedBrowserProfileTests | pass | 18/18, including timeout/pre-pull assertions |
| `python3.12 scripts/test_authenticated_browser_compose.py` | pass | validator negatives ok |
| `FLEXAGENT_OIDC_SKIP_LIVE=1 bash build/scripts/verify-oidc-ci.sh` | pass | static complete |
| Live GHA `oci` after this change | pending | commit/push next |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
