# ADR-021: Production frontend reset and single-SPA topology

## Status

**Approved — 2026-08-28; amended 2026-08-31** for design-lab outbound
imports of production-safe domain composition after generic design-system
demotion.

## Owners and approvers

- Owner: Architecture Lead
- Required approvers: Architecture Lead, Frontend/UI owner, Security/Privacy
  reviewer
- Proposed date: 2026-08-28
- Approved date: 2026-08-28
- Approval reference: Repository-owner activation of task
  `shipboard-production-ux-reset`

## Context

[ADR-020](ADR-020-frontend-rebuild-transition-and-design-lab-isolation.md)
authorized a dual-build transition: `@flex-agent/web-legacy` served production
until a later cutover, while `@flex-agent/web` held a Shipboard candidate plus
an isolated design lab. The confirmed product/UI reset invalidates that
strategy. Production page composition in both trees must be removed and rebuilt
against replacement UI/UX authority. Serving `web-legacy/`, publishing the
design lab, or shipping an empty shell during the rebuild is forbidden.

ADR-019 state/library boundaries and ADR-020 design-lab isolation lessons
remain in force. This ADR supersedes ADR-020 only where dual-build, production
pointer, cutover, and mandatory-legacy-as-runtime language conflict with the
reset.

## Decision drivers

- One production SPA, one isolated design-lab entry, no parallel production
  tree.
- Design-lab routes, fixtures, stores, and environment adapters must never
  enter the production entry graph, bundle, OCI image, or authenticated E2E.
- During any interval with no rebuilt production UI, publication must fail
  closed with an operator-facing message.
- Rollback is Git and immutable build provenance, not a hidden legacy runtime.
- Client-supplied organization, activity, participant, session, role, or
  ownership identifiers are never authorization.

## Options considered

| Option | Pros | Cons |
| --- | --- | --- |
| Keep ADR-020 dual-build until cutover | Preserves current production SPA | Conflicts with the authority reset; keeps a second production composition |
| Publish design lab as a temporary production shell | Visual continuity | Forbidden: synthetic fixtures and lab routes are not product |
| Ship an empty or placeholder production bundle | Unblocks OCI jobs | Ambiguous user-facing product; forbidden |
| Fail closed, then restore one `web/` SPA | Honest operator failure; single end-state topology | Temporary loss of a deployable production frontend |
| Runtime flag choosing lab vs production | Easy local comparison | Two production truths; rejected |

## Decision

Use one production SPA in `web/` (`@flex-agent/web`) and one isolated
design-lab Vite entry in that package. Delete `web-legacy/`. Do not recreate a
second production or candidate tree.

### `FE-RESET-1` — Target topology

| Entry | Package | Role |
| --- | --- | --- |
| `web/index.html` → `web/src/main.tsx` | `@flex-agent/web` | Sole production SPA |
| `web/design-lab.html` → `web/src/design-lab/main.tsx` | `@flex-agent/web` | Isolated design lab; never a production pointer |

Production commands, production Playwright, `deploy/docker/spa.Dockerfile`,
SPA SBOM, and authenticated-browser canonical journeys point **explicitly** at
the production `web/` entry. Design-lab verification uses separately named
commands (`dev:design-lab`, `build:design-lab`, `verify:design-lab`,
`test:e2e:design-lab`).

### `FE-RESET-2` — Compile-time isolation (preserved from ADR-020)

Production modules must not import `src/design-lab`, `.work/resources`, or any
retired `web-legacy` path. Design-lab modules may import `design-system`,
`lib`, shared styles, lab modules, and production-safe domain composition used
to clone production pages (`web/src/components/work/`, `web/src/content/`, and
named assessment readouts such as `SetupTrackReadout`). They must not import
the production entry graph, pages, API clients, auth/query hooks, or other
`features/` modules. `design-system` must never import the design lab.
Production may use layout families `management`, `guided-task`, and
`live-session`. `reference` remains design-lab only. The lab
`LiveSessionLayout` implementation may own `data-layout="live-session"` until
that family is promoted into `design-system/patterns/layouts/`.

ADR-019 Query, form, Lucide, native `fetch`, typed/domain clients, CSRF,
generation guards, one in-memory QueryClient per tree, protected cache purge,
and authorization-context epoch remain in force. Realtime Session UI stays
outside Query (`FE-DEC-11`).

### `FE-RESET-3` — Fail-closed publication interval

Until the rebuilt production SPA is restored as deployable:

| Command / artifact | Required behavior |
| --- | --- |
| Root `pnpm build` | Exit non-zero with a message that production frontend publication is disabled for the Shipboard UX reset; point operators to `pnpm build:design-lab` for the isolated lab |
| `deploy/docker/spa.Dockerfile` | Must not COPY or build `web-legacy/`; must not use `design-lab.html` as the image entry; during the interval it must fail the image build rather than publish a placeholder |
| `build/scripts/generate-spa-sbom.sh` | Must not treat `web-legacy/package.json` as the production graph during or after the reset |
| `build/scripts/serve-e2e-spa.sh` | Must not serve `web-legacy/dist` |

Design-lab lint, typecheck, unit tests, lab build, and lab Playwright remain
independently runnable throughout.

After restore, root `pnpm build` and `spa.Dockerfile` build the production
`web/` entry only. Negative checks must still reject design-lab HTML, lab
routes, lab fixtures, and synthetic adapters in the production artifact.

### `FE-RESET-4` — Recovery and rollback

Recovery is a Git revision of this repository plus rebuilding OCI images from
that revision. Do not retain `web-legacy/` or a dual-SPA switch as rollback
infrastructure. Immutable SBOM and image digests identify what was published.

### `FE-RESET-5` — Authority order for page implementation

Replacement production pages may be implemented only after:

1. The Product/UI/UX retirement decision in
   [`docs/ui-ux/retired-authority.md`](../../ui-ux/retired-authority.md) is
   Approved and applied; and
2. Replacement P0 UI/UX specifications are Approved.

This ADR does not approve journeys, copy, or visual language.

### `FE-RESET-6` — No compatibility bridge

Do not maintain redirects from deleted `web-legacy` routes, a runtime dual-SPA
bridge, or a feature flag that selects lab fixtures in production.

## Consequences

Positive: one production truth; lab isolation remains testable; publication
cannot silently ship the wrong entry.

Negative: an intentional interval with no deployable production SPA; OIDC and
OCI jobs that assumed `web-legacy` must be retargeted or fail closed until
restore.

Neutral: ADR-010 client-rendered React/Vite SPA is unchanged. ADR-019 is
unchanged. Design-system v1.0 remains visual authority.

## Related

- UI/UX: [retirement ledger](../../ui-ux/retired-authority.md),
  [UI/UX index](../../ui-ux/README.md),
  [design system v1.0](../../ui-ux/design-system/README.md)
- Supersedes: ADR-020 dual-build production pointer, cutover-from-legacy, and
  `web-legacy/` as a live runtime. Design-lab isolation is restated here as
  `FE-RESET-2` (amended 2026-08-31 for production-safe domain composition).
  Historical ADR-020 `FE-TRANS-3` / `FE-TRANS-4` sentences that forbid every
  lab import from `web/src/components/` or `features/` yield to `FE-RESET-2`.
- Does not supersede: ADR-019
