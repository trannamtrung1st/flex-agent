# Frontend architecture

Implementation guidance for structuring the Flex Agent browser SPA consistently
with SPA/API authority, locked React/Vite policy, and frontend state
ownership.

## Status and authority

**Approved — 2026-09-02.** This guide currently owns SPA Query, form, icon,
and transport ownership and single-SPA production topology with
isolated design lab, server-backed table query/selection ownership, and no `web-legacy/` runtime. Dual-build
`web-legacy` topology is historical only and must not be restored.
This guide does not introduce product behavior or replace feature or UI/UX
specifications. Design-lab specimens and shipped production composition do
not authorize routes, permissions, lifecycle, or release scope. If this
guide conflicts with product, requirements, or UI/UX, stop and record the
conflict.

## Architectural identity

The Flex Agent frontend is a **client-rendered React SPA** that presents
server-authoritative state.

- **Presentation and transient UI** belong in the browser (`AR-DEC-12`).
- **Authorization, Organization/resource scope, timers, ordering, acceptance,
  Evaluation, Release, and reconciliation truth** remain server-side.
- **HTTP-backed page resources** use TanStack Query for fetch, cache, load,
  error, cancellation, and invalidation only (`FE-DEC-1`–`FE-DEC-7`).
- **Non-trivial forms** use React Hook Form; Zod validates client UX shape
  only (`FE-DEC-8`).
- **Transport and domain meaning** stay in typed/domain API clients over
  native `fetch` (`FE-DEC-6`, `FE-DEC-12`).
- **Realtime Session UI** is an explicit exception (`FE-DEC-11`).

## Production topology

The production SPA source is `web/` (`@flex-agent/web`). The design lab is a
separately built entry in the same package. There is no `web-legacy/` runtime.
Production never serves the lab.

Production modules must not import `src/design-lab` or `.work/resources`.
Shared visual implementations are owned by
`web/src/design-system/` plus `web/src/lib/` and `web/src/styles/shared.css`.
Design-lab modules may import that shared tree, production-safe domain
composition (`web/src/components/work/`, `web/src/content/`, named assessment
readouts used by Deck clones), `web/src/styles/design-lab.css`, and synthetic
fixtures only inside the design-lab entry graph. They must not import
production pages, API clients, or auth/query hooks. See
`FE-RESET-1`–`FE-RESET-6`. The lab route namespace is `/design-lab/*`.

Production SPA HTTP contract readiness (host `FlexAgent.Api`, not the synthetic
browser harness):

| Surface | Current host contract | SPA posture |
| --- | --- | --- |
| Auth session, Assessment shell, setup, Enrollment, My Work, Submission intake | Exposed under `/auth`, `/v1/assessment`, `/v2/assessment` | Implemented in `web/src/api/` and production pages |
| Attempt start | Mapped under `/v2/assessment/my-work/{enrollmentId}/attempt` | Implemented; Continue uses the committed Session locator |
| Text Session snapshot, commands, and hosted events | `GET/POST /v1/sessions/{sessionId}` and `GET /v1/sessions/{sessionId}/events`; compatibility SSE remains `GET /sessions/{sessionId}/events` | `/sessions/:sessionId` is the Participant live-session route; `/operations` and `/transcript` are separate management records |
| Review, Result, Release | Architecture contract exists; no production host HTTP group | Destinations stay contract-unavailable |

Local Vite proxies `/auth`, `/v1`, `/v2`, `/sessions/{id}/events`, and `/browser`
to the API. Document loads of `/sessions/:sessionId` stay on the SPA.
Do not treat `/browser/*` synthetic harness routes as production truth.
Human login is a document navigation through `/auth/login` and
`/auth/callback`. Callback failures must redirect back into the SPA recovery
gate rather than rendering API JSON as a document. Identity/Organization
fail-closed also starts provider logout with `id_token_hint` so the next
**Continue to sign in** is not bound to the refused account and Keycloak does
not require a separate confirmation click.

Styling follows design-system v1.1: primitive values in
`web/src/styles/tokens.css`, semantic aliases in `semantic-aliases.css`, light
remaps in `adaptations.css`. Do not treat v0.1 Deep-Space names as visual
authority. New production UI clones a matching existing production page and
Component Deck specimen; the design-system module wins if they disagree.
Isolated design-lab journeys remain composition donors only for shells whose
approved family is not yet production-backed. Lucide remains the general icon
library (`DS-DEC-10`).

## Layering

```text
App composition root (main.tsx)
  QueryClientProvider          # one in-memory client per mounted tree
    ProductionApiProvider | BrowserApiProvider
      Router
        Shared layout (router-assigned family)
          Composition primitives (Stack, Inline, Grid, Container, Inset, SplitBay)
            Feature pages (slot content only)
            feature query/mutation hooks
              typed/domain API clients (web/src/api/)
                browser-safe wire types (web/src/contracts/v1.ts, v2.ts)
                fetchJson / native fetch
```

Route composition is **router layout assignment → shared layout → inner
composition primitives → feature page content**. Pages under `web/src/pages/`
and design-lab routes must not import outer-chrome primitives (`CommandStrip`,
`Gangway`, `Bulkhead`, `ConsoleFoot`, `RailBrand`, `IndexRail`,
`AreaGroupList`) to assemble a shell. Layouts live in
`web/src/design-system/patterns/layouts/`. Inner flow, wrap, intrinsic grid,
width, and padding live in `web/src/design-system/components/layout/` and may
be imported from the production design-system barrel. Production may use
`management`, `guided-task`, and `live-session`. `reference` is design-lab
only (`web/src/design-system/lab.ts`) and must not enter the production barrel.
Router hosts resolve the family from the route-layout manifest before rendering
the matching layout.

- `web/src/api/` remains React-free except existing provider components. It
  owns CSRF, credentials, generation guards, `ProductionApiError`, command
  execution, and domain outcome interpretation. Wire shapes come from
  `web/src/contracts/` (browser-safe `v1.ts` / `v2.ts` only).
- Feature Query keys and hooks live under `web/src/features/<capability>/`.
  Hooks compose API clients; they do not call `fetch` and do not infer
  authorization from keys or cached payloads.
- Pages render Query and form state. They must not copy successful query data
  or RHF field values into parallel `useState` authority.
- Reusable Shipboard primitives (`keys`, `fields`, `feedback`, `navigation`,
  layout primitives, and closed layout families) live in `web/src/design-system/`.
  Production app composition (auth shell, route-derived breadcrumbs, API wiring)
  lives in `web/src/components/` except `work/`, which is shared domain chrome
  the design lab may import. Shell, pages, API clients, and auth/query hooks
  remain lab-forbidden (`FE-RESET-2`).

## State ownership

| State kind | Owner | Must not |
| --- | --- | --- |
| HTTP-backed page resources | TanStack Query | Mirror into page `useState`; persist cache; treat cache as authorization |
| Form values and client validation | React Hook Form; Zod for meaningful runtime shape | Encode server authorization, eligibility, revision, or security rules |
| Simple ephemeral UI | Component-local React state | Promote dialog/presentation flags into Query or a global store |
| Locally complex ephemeral UI | Focused `useReducer`/hook | Add Zustand or an application-global store without a later approved decision |
| Shell/authentication/CSRF | API providers | Infer identity from Query keys |
| Domain commands and reconciliation | Typed/domain API layer | Move expected revision, idempotency, or 403/409 meaning into Query |
| Realtime Session | Existing Session page, helpers, and reducers | Migrate SSE/projection/command identity into Query in this architecture |

## Query client and cache lifecycle

Create the client with `createFlexQueryClient()`. Each mounted App or test
tree gets one client through a lazy initializer or equivalent stable provider.
Do not construct a client during every render. Do not use a process-global
singleton.

Initial defaults:

- retry disabled
- refetch on window focus disabled
- no persistence
- no global optimistic updates

Every Query/mutation cache entry is protected until a reviewed public split
exists. Purge by cancelling in-flight work and clearing the complete
QueryClient, including mutation variables and results, when:

- production 401 or 403 handling clears protected state
- logout succeeds
- bootstrap is unauthenticated or failed
- synthetic actor/access is replaced
- trusted production shell actor, Organization, or authorization context is replaced
- API state leaves `ready` for any equivalent protected-state reset

A successful trusted-context replacement always starts a new authorization-context
epoch. Same actor and Organization with a narrowed relationship, navigation, or
permission set still purges Query/mutation cache, bumps generation-based
stale-response protection, and remounts the protected React subtree under
`actorId:organizationId:epoch`. Do not fingerprint `permitted_actions` as the
isolation key; a later server-issued authorization/session version may replace
the client epoch. Query-cache clearing alone is not sufficient.
Generation-based stale-response protection in `ProductionApiProvider` must
still reject older responses after reset.

A synthetic actor/navigation reload after an ordinary `executeCommand` is a
projection refresh, not trusted-context replacement. It must not increment the
authorization-context epoch, purge the Query cache, set API state back to
`loading`, or remount `ProtectedBrowserAuthSubtree`. Actor or Organization
replacement, first bootstrap, explicit `replaceAuthorizationContext`, and
same-actor `capabilities` or `actor_stage` narrowing remain replacements.

Synthetic authentication loss (`unauthenticated` / HTTP 401) discovered by
`refresh`, `executeCommand`, `reconcileCommand`, or `fetchJson` must leave
`ready` through the same teardown. Ignore a 401 whose request started under a
previous authorization-context epoch so it cannot sign out a newer trusted
context. Command HTTP 403/409 domain outcomes are returned to the caller and
are not treated as complete workspace access loss.

Leaving synthetic API `ready` for unauthenticated, denied, or error must purge
the Query cache, clear actor and navigation, advance the authorization-context
epoch, and render `denied`/`error`/`idle` instead of protected routes. Do not
keep a previous actor identity in the protected subtree key after access loss.

Query cancellation passes the provided `AbortSignal` through the typed client
to `fetchJson`. Feature UI classifies access loss with typed status/outcome
helpers, not regular expressions against presentation copy.

## Query keys

Use readonly factories owned by the feature. Include a contract/projection
version whenever parallel wire meanings can coexist. Do not use a key to
reinterpret or upgrade a response.

Initial Assessment keys:

| Factory | Key |
| --- | --- |
| `assessmentKeys.all` | `["assessment"]` |
| `assessmentKeys.v1` | `["assessment", "v1"]` |
| `assessmentKeys.activitiesRoot()` | `["assessment", "v1", "activities", "list"]` |
| `assessmentKeys.activities(query)` | `["assessment", "v1", "activities", "list", canonicalQuery]` |
| `assessmentKeys.sourceOptions()` | `["assessment", "v1", "activities", "source-options"]` |

Reserve `assessmentKeys.activity(activityId)` for later detail adoption. Do
not create unused hooks merely to populate a hierarchy.

Server-numbered Activity queries extend the root list key with one canonical object
containing `paging`, `page`, `pageSize`, normalized search, and the ordered
field/direction sort specifications. Semantically equivalent requests must have equal keys. A prefix
invalidation at `assessmentKeys.activitiesRoot()` invalidates all
cached Activity pages; a mutation must not invalidate only the page that was
visible when the list changed.

## Server-backed tables and selection ownership

`DataTablePagination` is controlled presentation. Numbered props have the same
shape whether rows were sliced locally or returned by the server; do not add a
`server-numbered` visual variant. Feature Query owns server request, pending,
error, retry, and returned page metadata. A server-paged page must not run local
search or ordering and then label that subset as all matches.

While a same-context replacement page is pending, Query may retain the last
authorized page as placeholder presentation. The table remains busy, paging is
disabled, and the retained page metadata is not relabeled as the requested
page. Authorization-context replacement or access loss still purges it through
the existing protected-cache lifecycle.

Table-header selection declares its capability explicitly:

- `page` mode selects or clears only IDs present on the current page;
- `matching` mode is enabled only with a stable complete-local or server query
  scope and stores that scope plus explicit exclusions; an exact total is
  optional and affects copy only; and
- client-side action helpers must not materialize a matching selection from
  current-page records. A server-backed bulk action consumes a typed selection
  descriptor through its domain API instead.

Changing a query value that contributes to matching scope invalidates the old
matching selection. A current-page cursor result is never passed as the full
matching identifier set. The P0 Assign Participant picker uses page mode and
still permits only one committed selection under `UI-SUBM-DEC-9` and
`UI-SUBM-DEC-15`; the generic matching mode does not authorize bulk assignment.

## Activities coordination (first migrated slice)

The Activities list is the prerequisite query because server-returned
`permitted_actions` decides whether the create surface is available.

- Enable the source-option query only after a **fresh** Activity-list
  observation (`isFetchedAfterMount`) succeeds and includes
  `create_assessment`. Cached list payloads must not enable that dependent
  protected read. Actors without that action must not request unused source
  options.
- While creation is permitted, keep the protected loading state until the
  first source-option request has settled, preserving the current stable
  initial layout.
- A source-option failure degrades only the create section to the existing
  safe missing-source/unavailable state. It must not discard an authorized
  Activity list.
- Access-loss errors follow the provider purge/gate path and must not render
  cached protected content.

Campaign creation uses a non-optimistic `useMutation`. On authoritative
success, invalidate the Activity-list key prefix with `refetchType: "none"` so
every cached numbered page becomes stale, then immediately run the existing
`onCreated(activityId)` navigation. Do not refetch the still-mounted list
during navigation, do not await a list refetch, and do not invent a local
Activity summary from submitted fields. The invalidated list refetches when it
is next observed.

## Forms and validation

Use React Hook Form with `zodResolver` for the Campaign-create form because
title plus required exact-source selectors are a meaningful runtime object.

- Title is required with the existing 200-character maximum.
- Each required source category must have a non-empty exact option identity.
- Do not trim, normalize, or coerce submitted values unless an approved
  specification and server contract already require it.
- Initialize first permitted source selections once after the first successful
  source-option result and only while the form is pristine. A refetch must
  never silently reset touched title or source values.
- Immediately before mutation, resolve every selected identity against the
  latest source-option query data and require complete category coverage. If an
  option disappeared or changed, send no request, preserve entered values, and
  show a safe stale-options error. This is a malformed-request guard, not
  eligibility authority.
- Keep field errors programmatically associated. After submitted validation or
  a correctable server failure, focus the error-summary heading once;
  activating an entry focuses its field. Preserve safe input on recoverable
  failure.

`AssessmentSetupPage` currently has one editable title and is not part of this
first form migration.

## Icons and styling

Use `lucide-react` with direct named imports so unused icons tree-shake.
Follow [icon shapes](../ui-ux/design-system/components/icon-shapes.md) for
size, semantic foreground, and accessibility. The candidate package loads
`web/src/styles/shared.css` (tokens, base, production-safe families). Lab-only
sheets stay in `design-lab.css`. Do not restore the former
`tokens.css` / `components.css` / `app.css` split. Do not add
Tailwind, CSS-in-JS, Axios, or Zustand.

## Intentionally unmigrated surfaces

These remain outside the **first Query/form slice**. They are not a
claim that production UX is missing: Home, Activities, setup, Enrollment, and
My Work are rebuilt Shipboard pages. Keep current state owners until a later
task applies this guide without weakening their contracts:

- `AssessmentSetupPage` and Assessment setup workflow beyond Activities create
- Enrollment pages
- My work pages
- Submission, Review, Result, and Release pages
- Remaining effect-based HTTP pages not listed as migrated
- `SessionPage`, `sessionRuntimeView`, SSE/`EventSource` lifecycle, streamed
  or transient runtime state, pending command identity, reconciliation,
  reconnect behavior, and Session isolation

- `AssessmentSetupPage` and Assessment setup workflow beyond Activities create
- Enrollment pages
- My work pages
- Submission, Review, Result, and Release pages
- Remaining effect-based HTTP pages not listed as migrated
- `SessionPage`, `sessionRuntimeView`, SSE/`EventSource` lifecycle, streamed
  or transient runtime state, pending command identity, reconciliation,
  reconnect behavior, and Session isolation

A future contained Session refactor may separate projection, connection,
command, and presentation hooks while preserving reviewed authority and race
protections. That work requires its own task.

## Verification

Frontend changes under this guide must prove:

- isolated Query clients per App/test tree
- protected cache clearing and stale-response non-repopulation
- typed access-loss classification
- cancellation through the API client
- Activities query coordination and exact list-key invalidation
- RHF/Zod client validation without duplicating server rules
- input preservation, duplicate-submit prevention, and accessible error focus
- Lucide decorative-icon naming
- no Axios, Tailwind, or Zustand
- unchanged Session-focused tests unless a dedicated Session task says otherwise
