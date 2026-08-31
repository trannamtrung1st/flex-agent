# Production app composition

`web/src/components/` holds production UI that wires routing, authentication,
API state, and shell behavior. Shell, `ErrorBoundary`, and API-backed
composition must not be imported from the design lab (ADR-021 `FE-RESET-2`).
`work/` is production-safe domain chrome: production pages and design-lab
Deck clones may share it. Do not put Query, API clients, or auth hooks here.

| Area | Path | Role |
| --- | --- | --- |
| Shell | `shell/` | Auth gate layouts, session loading/denied/sign-out screens, route-derived breadcrumbs, theme hook wrapper |
| Work | `work/` | Assignment station layout, spine, intake lists, submission version lists, assignment plates, setup ceremony hosts, briefing acknowledgement |
| Resilience | `ErrorBoundary.tsx` | App-level React error boundary and fatal fallback |

Reusable Shipboard Terminal primitives live in
[`web/src/design-system/`](../design-system/README.md) (`components/`, `patterns/`).
Pages and features should import generic UI from the design-system barrel and
reserve this tree for app-specific composition. Ceremony plates (`CeremonyArea`,
`CeremonyUnavailable`, `CeremonyWait`) belong in the design-system barrel;
`SessionChrome` owns auth-gate shell layouts only.
