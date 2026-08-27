# Production app composition

`web/src/components/` holds production-only UI that wires routing, authentication,
API state, and shell behavior. It must not be imported from the design lab
(ADR-020 `FE-TRANS-3`).

| Area | Path | Role |
| --- | --- | --- |
| Shell | `shell/` | `ProductionAppShell`, session chrome, route-derived breadcrumbs, theme hook wrapper |
| Resilience | `ErrorBoundary.tsx` | App-level React error boundary and fatal fallback |

Reusable Shipboard Terminal primitives live in
[`web/src/design-system/`](../design-system/README.md) (`components/`, `patterns/`).
Pages and features should import generic UI from the design-system barrel and
reserve this tree for app-specific composition.
