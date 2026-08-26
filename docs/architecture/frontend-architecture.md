# Frontend architecture

Implementation guidance for structuring the Flex Agent browser SPA consistently
with approved SPA/API authority, locked React/Vite policy, and frontend state
ownership.

## Status and authority

**Approved — 2026-08-26.** This guide applies
[ADR-019](decisions/ADR-019-frontend-state-and-library-boundaries.md). It does
not introduce product behavior or replace feature or UI/UX specifications. If
this guide conflicts with an approved ADR, the ADR governs technical
realization. If it conflicts with approved product, requirements, or UI/UX
authority, stop and record the conflict; do not reinterpret those sources.

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

## Layering

```text
App composition root (main.tsx)
  QueryClientProvider          # one in-memory client per mounted tree
    ProductionApiProvider | BrowserApiProvider
      Router
        Feature pages
          feature query/mutation hooks
            typed/domain API clients (web/src/api/)
              fetchJson / native fetch
```

- `web/src/api/` remains React-free except existing provider components. It
  owns CSRF, credentials, generation guards, `ProductionApiError`, command
  execution, and domain outcome interpretation.
- Feature Query keys and hooks live under `web/src/features/<capability>/`.
  Hooks compose API clients; they do not call `fetch` and do not infer
  authorization from keys or cached payloads.
- Pages render Query and form state. They must not copy successful query data
  or RHF field values into parallel `useState` authority.

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
- trusted production shell actor or Organization identity changes
- API state leaves `ready` for any equivalent protected-state reset

A successful rebootstrap compares trusted current and incoming shell
actor/Organization identities and purges Query cache before replacement when
either changes. The protected React subtree (shell outlet, forms, refs, and
ephemeral UI) remounts under a key derived from that trusted identity so local
state cannot survive the replacement. Query-cache clearing alone is not
sufficient. Generation-based stale-response protection in
`ProductionApiProvider` must still reject older responses after reset.

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
| `assessmentKeys.activities()` | `["assessment", "v1", "activities", "list"]` |
| `assessmentKeys.sourceOptions()` | `["assessment", "v1", "activities", "source-options"]` |

Reserve `assessmentKeys.activity(activityId)` for later detail adoption. Do
not create unused hooks merely to populate a hierarchy.

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
success, invalidate exactly `assessmentKeys.activities()` with `exact: true`
and `refetchType: "none"`, then immediately run the existing
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
size, semantic foreground, and accessibility. Retain `tokens.css`,
`components.css`, and `app.css`. Add only the smallest semantic alignment rule
when existing button gap/layout is insufficient.

## Intentionally unmigrated surfaces

These remain outside the first Query/form slice and keep their current owners
until a later task applies ADR-019 without weakening their contracts:

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
