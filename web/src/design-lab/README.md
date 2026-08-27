# Design lab

Isolated Shipboard Terminal specimens and synthetic journeys. This tree is the
local visual-composition donor for production migration. It is not a production
router, production bundle, authentication flow, or API client.

Route namespace: `/design-lab/*` (no `/prototypes` alias). Dev server:
`pnpm --filter @flex-agent/web dev:design-lab --host 127.0.0.1` at
`http://127.0.0.1:5275/`.

## Module disposition

| Disposition | Meaning | Location |
| --- | --- | --- |
| **promote** | Production-safe shared foundation, component, or pattern | `web/src/lib/`, `web/src/styles/`, `web/src/design-system/` |
| **surface donor** | Lab visual composition that Phase 8 may copy/adapt into a feature | `app/`, `routes/`, `features/` (except gallery), `pages/` if present |
| **lab-only** | Fixtures, demo controls, gallery, synthetic behavior, future/reference | `data/`, `lib/useDemoParam.ts`, `lib/useSurface.ts`, gallery, remaining `components/` |

Promoted modules are imported from `web/src/design-system/` and `web/src/lib/`.
Do not import this folder from production entry, features, or
`web/src/design-system/`.

See `components/README.md` for family-level notes.
