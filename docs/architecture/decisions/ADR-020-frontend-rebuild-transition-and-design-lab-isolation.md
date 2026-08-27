# ADR-020: Frontend rebuild transition and design-lab isolation

## Status

Approved — 2026-08-27.

This record was approved by the repository owner through task
`impeccable-frontend-rebuild`, with detailed realization delegated to
architecture, frontend, and security/privacy roles. Completeness review on
2026-08-27 found no new high-risk conflict with `AR-DEC-12`, ADR-019, or
approved P0 behavior. It does not change product meaning, Assessment
requirements, Session runtime contracts, or Result/Release authority.

## Owners and approvers

- Owner: Architecture Lead
- Required approvers: Architecture Lead, Frontend/UI owner, Security/Privacy
  reviewer
- Proposed date: 2026-08-27
- Approved date: 2026-08-27

## Context

The approved visual language is design-system v1.0 Shipboard Terminal. The
current production SPA lives in `web/` and still implements v0.1 presentation
plus the frozen production API-mode route set. A one-way rebuild must:

- keep serving unchanged production behavior until an explicit cutover;
- build a new SPA on the same ADR-010/ADR-019 stack and v1.0 tokens;
- isolate prototype/design-lab surfaces from production routing, bundles, OCI
  images, authentication, and E2E;
- remove `web-legacy/` and the raw prototype snapshot after acceptance.

## Decision drivers

- Production must never serve two SPAs or choose between them at runtime.
- `web-legacy/` is transitional evidence only, not product authority.
- ADR-019 Query, form, Lucide, native `fetch`, and Session-outside-Query
  decisions remain in force.
- Design-lab fixtures, prototype routes, and `.work/resources` must not enter
  the production entry graph (`PC-14`).
- Cutover rollback is a repository/deployment revision rollback, not a
  maintained dual-runtime switch.
- Legacy retirement after gates pass is mandatory.

## Options considered

| Option | Pros | Cons |
| --- | --- | --- |
| In-place restyle of current `web/` | Smaller directory churn | Mixes legacy tests/styles with v1.0; hard to prove isolation; blocks a clean design-lab split |
| Dual packages `web-legacy` + new `web`, one production pointer | History-preserving rename; explicit production owner; isolated candidate | Temporary workspace/CI complexity |
| Runtime feature flag choosing old vs new SPA | Easy local comparison | Two production truths; cache/auth risk; forbidden by this task |
| Keep prototype repo as a workspace package | Convenient visual source | Supply-chain and authority leak; rejected in Phase 2 |

## Decision

Use one transitional dual-build, then one cutover, then mandatory legacy
removal.

### `FE-TRANS-1` — Directory ownership

Rename current `web/` to `web-legacy/` with history-preserving Git. Create a
new `web/` for the candidate SPA and separately built design lab. Until Phase 9
cutover, production commands, production E2E, `deploy/docker/spa.Dockerfile`,
and SPA SBOM/OCI continue to point **explicitly** at `web-legacy/`. Do not
infer production source from the `@flex-agent/web` package name.

### `FE-TRANS-2` — Package identities and commands

| Package | Role |
| --- | --- |
| `@flex-agent/web` | New candidate SPA and design-lab build graphs |
| `@flex-agent/web-legacy` | Frozen production SPA until cutover |

Root/CI commands must name the target: `verify:web:legacy`, `verify:web:new`,
combined `verify:web`, isolated design-lab verify/preview, and an explicit
production-build selector owned by CI/Docker.

### `FE-TRANS-3` — Compile-time import boundary

Production entry modules may import app, design-system, features, api, and
lib code in new `web/src`. Design-lab entry modules may also import synthetic
fixtures under `web/src/design-lab`. Production code must never import from
`src/design-lab`, `.work/resources`, or `web-legacy/`. Legacy code must never
import the new production tree. Architecture tests enforce both directions.

### `FE-TRANS-4` — Design-lab isolation

The design lab is a separate HTML/Vite entry, router root `/prototypes/*`,
bundle, and Playwright config. It uses synthetic data only. It is not an input
to the production SPA image, production E2E suite, authentication flow, or API
traffic.

### `FE-TRANS-5` — ADR-019 preserved

TanStack Query, React Hook Form, Zod, Lucide named imports, native `fetch`,
typed/domain clients, CSRF, generation guards, one in-memory QueryClient per
tree, protected cache purge, authorization-context epoch, and realtime Session
outside Query remain as in ADR-019. This ADR supersedes only path/layout and
styling-entry details required by v1.0 and the dual-build.

### `FE-TRANS-6` — Toolchain and fonts

New `web/` uses repository-pinned Node, pnpm, React, Vite, TypeScript, lint,
Vitest, Testing Library, Playwright, Query, RHF, Zod, and Lucide versions from
`build/toolchain.json` and the workspace lockfile. Fonts are self-hosted
`@fontsource/michroma` and `@fontsource/sometype-mono` (OFL-1.1) at exact
pinned versions. Do not copy prototype caret ranges or pnpm 11.

### `FE-TRANS-7` — Cutover and rollback

Phase 9 switches production pointers from `web-legacy/` to `web/` in one
reviewed change set. If a cutover gate fails, revert the deployment and
repository revision. Do not add a maintained code fallback, route switch, or
compatibility bridge that survives the task.

### `FE-TRANS-8` — Mandatory retirement

After cutover gates pass, delete `web-legacy/`, transitional compatibility
commands, and `.work/resources/impeccable-prototype-snapshot/`. Git history,
the design-system change record, and this ADR remain the recovery path.

## Consequences

Positive: production stays stable during migration; isolation is testable;
v1.0 can land without restyling the live SPA in place.

Negative: two frontend packages and CI paths exist until Phase 10.

Neutral: ADR-010 `STACK-DEC-4` client-rendered React/Vite SPA is unchanged;
only the directory that currently holds production assets changes during
transition.

## Related

- Requirements: frozen production API-mode parity in
  `.work/active/impeccable-frontend-rebuild.md`; P0 UI/UX specs unchanged
- UI/UX: [design system v1.0](../../ui-ux/design-system/README.md)
- Supersedes: none. Narrows ADR-010 workspace path language for the transition
  window. Does not supersede ADR-019 state ownership.
