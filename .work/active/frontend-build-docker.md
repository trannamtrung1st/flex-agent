---
id: frontend-build-docker
status: completed
created: 2026-08-29
updated: 2026-08-29
---

# Goal

Make the production frontend TypeScript/Vite build and SPA Docker image succeed, and fix any findings that currently fail those gates.

# Governing sources

- `web/package.json` (`build` = `tsc -b && vite build`)
- `deploy/docker/spa.Dockerfile`
- `build/scripts/build-oci-images.sh`
- `build/scripts/verify-web.sh` / `pnpm verify:web:production`
- `docs/contributing/workspace.md` (local Docker build commands)
- `.agents/skills/frontend-developer/SKILL.md`
- `.agents/skills/impeccable/` (harden / production-ready; no visual redesign)

# Scope

## In

- Production web compile and Vite build
- SPA OCI image (`deploy/docker/spa.Dockerfile`)
- Lint/typecheck/test findings that fail `pnpm verify:web:production`

## Out

- Visual redesign or journey changes unless a compile-safe edit
- API/worker Docker unless they block SPA verification
- Commits or pushes
- Vite 500 kB chunk warning (does not fail the build)

# Plan

- [x] Reproduce production `pnpm --filter @flex-agent/web build` failure
- [x] Fix compile/bundle/lint findings
- [x] Reproduce `docker build -f deploy/docker/spa.Dockerfile`
- [x] Fix Docker/OCI findings
- [x] Re-run production build and SPA image; record evidence

# Current state

Completed. Review pass (2026-08-29) aligned leave-prompt `returnValue` assignment and fetch URL parsing with existing production tests; focused tests and lint green.

# Decisions

- Treat this as a production-build/harden task, not a visual redesign.
- Remove unused deprecated `compactEnrollmentId` / `formatNamedCampaignInstant` instead of disabling lint.
- Tooltip exclusive-open ownership uses a stable instance token so the plaque close callback does not self-reference during render.

# Findings / deviations

- `pnpm --filter @flex-agent/web build` and SPA Docker already compiled; CI production verify failed ESLint (12 errors).
- Vite still warns that `index-*.js` is ~573 kB (>500 kB). Warning only; not treated as a gate failure in this task.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `pnpm --filter @flex-agent/web build` | passed | Vite production build, `dist/assets/index-oAQhQVO7.js` |
| `pnpm verify:web:production` | passed | lint, typecheck, 383 tests, isolation, candidate bundle |
| `docker build -f deploy/docker/spa.Dockerfile -t flex-agent-spa:local .` | passed | image `flex-agent-spa:local`, in-image `tsc -b && vite build` |
| Impeccable detect (changed UI) | passed | `[]` |
| Review pass: focused tests + lint + typecheck | passed | 57 tests in 6 files; production lint/typecheck |
| Live Compose `:18080` | healthy, stale SPA | `session-endpoint:ok`; SPA container started before this source change |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
