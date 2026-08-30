# Design lab

Isolated Shipboard Terminal specimens and synthetic journeys. Production UI
clones a matching existing production page and Component Deck specimen. This
tree remains a composition donor only for shells whose approved family is not
yet production-backed. It is not a production router, production bundle,
authentication flow, or API client.

Route namespace: `/design-lab/*` (no `/prototypes` alias). Dev server:
`pnpm --filter @flex-agent/web dev:design-lab --host 127.0.0.1` at
`http://127.0.0.1:5275/`.

## Module disposition

| Disposition | Meaning | Location |
| --- | --- | --- |
| **promote** | Production-safe shared foundation, component, or pattern | `web/src/lib/`, `web/src/styles/`, `web/src/design-system/` |
| **surface donor** | Lab visual composition for shells not yet production-backed | `app/`, `routes/`, `features/` (except gallery), `pages/` if present |
| **lab-only** | Fixtures, demo controls, gallery, synthetic behavior, future/reference | `data/`, `lib/useDemoParam.ts`, `lib/useSurface.ts`, gallery, remaining `components/` |

Promoted modules are imported from `web/src/design-system/` and `web/src/lib/`.
Lab routes must use the same shared layout families as production
(`ManagementLayout`, `GuidedTaskLayout`, `LiveSessionLayout`). Import
`ReferenceLayout` from `web/src/design-system/lab.ts`. Do not hand-build outer
chrome in a route module. Router assignment wraps each lab route in
`LayoutAssignment` from the route-layout manifest.
Shared Shipboard CSS is `web/src/styles/shared.css`. Lab-only demo and surface
sheets are composed by `web/src/styles/design-lab.css` and must not enter the
production bundle.

Do not import this folder from production entry, features, or
`web/src/design-system/`.

Verify with `pnpm verify:design-lab` (unit, typecheck, lint, bundle, Playwright).

See `components/README.md` for family-level notes.
