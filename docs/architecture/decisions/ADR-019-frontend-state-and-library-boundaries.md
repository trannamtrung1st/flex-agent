# ADR-019: Frontend state and library boundaries

## Status

Approved — 2026-08-26; amended 2026-08-26 to require protected-subtree remount on identity replacement, fresh permission observation before dependent reads, and exact no-refetch Activities invalidation.

This record was approved by the repository owner on 2026-08-26, with detailed
technical realization delegated to the implementing architecture and frontend
roles. It does not change product meaning, Assessment requirements, Session
runtime contracts, or the SPA/API authority boundary in
[ADR-006](ADR-006-mvp-architecture-baseline-and-evolution.md)
`AR-DEC-12`.

## Owners and approvers

- Owner: Architecture Lead
- Required approvers: Architecture Lead, Frontend/UI owner, Security/Privacy
  reviewer
- Proposed date: 2026-08-26
- Approved date: 2026-08-26

## Context

[ADR-010](ADR-010-dotnet-implementation-stack-and-workspace.md) `STACK-DEC-4`
approves React, Vite, and a client-rendered SPA. It does not select libraries
for HTTP-backed page resources, non-trivial forms, runtime shape validation,
or general-purpose icons.

The SPA currently copies remote Assessment data into page-local state, owns
Campaign-create form values manually, and has no shared icon library. Native
`fetch`, typed/domain API clients, CSRF, generation-based stale-response
protection, and design-system CSS already exist and must remain authoritative
for transport, domain outcomes, and presentation tokens.

Realtime Session UI is a reviewed, distinct surface: authoritative projections,
SSE/`EventSource`, command identity, reconciliation, and isolation. Folding
that surface into ordinary CRUD query/cache semantics would weaken
[session-runtime-contract.md](../session-runtime-contract.md) and `AR-DEC-4`.

## Decision drivers

- Keep server authorization, isolation, business validation, and workflow
  transitions out of the browser (`AR-DEC-12`, ADR-002).
- Give ordinary HTTP-backed pages one owner for fetch/cache/load/error/
  invalidation without inventing a second domain layer.
- Give non-trivial forms one owner for field values, client UX validation, and
  accessible error association without duplicating server rules.
- Preserve native `fetch`, typed/domain clients, CSRF, and generation guards.
- Preserve semantic CSS tokens and components; do not introduce Tailwind,
  CSS-in-JS, Axios, persisted Query cache, or an application-global entity
  store.
- Make authentication-context cache clearing explicit so Query data cannot
  cross actors, Organizations, or application sessions.
- Leave realtime Session behavior unmigrated until a dedicated task can
  preserve its reviewed race and isolation protections.

## Options considered

| Option | Pros | Cons |
| --- | --- | --- |
| Continue page-local `useState`/`useEffect` for all HTTP resources and forms | No new dependencies | Duplicate remote copies, inconsistent cache/auth lifecycle, and growing form/error duplication |
| TanStack Query for HTTP-backed resources; React Hook Form + Zod for non-trivial forms; Lucide for icons; native `fetch` retained | Clear state ownership; tree-shakable icons; existing transport stays | Adds locked frontend packages and a migration discipline |
| Axios as the HTTP client | Familiar interceptors | Duplicates CSRF/`fetchJson`/generation behavior already owned by API providers |
| Zustand or another global store for server entities | Convenient shared objects | Creates a second authority for server data and weakens isolation/clearing |
| Tailwind or CSS-in-JS | Rapid utility styling | Conflicts with the approved semantic token/component CSS system |
| Migrate SessionPage into Query | One client-state story | Would reinterpret SSE, projection commit, command identity, and reconciliation |

## Decision

Use TanStack Query for HTTP-backed page resources, React Hook Form with Zod
only where runtime form shape has value, Lucide React for general-purpose
icons, and existing native `fetch` plus typed/domain API clients for transport
and domain meaning. Do not add Axios, Tailwind, or Zustand.

The first production-backed proof is the Activities list, source-option reads,
and Campaign-create form. Other HTTP pages migrate later under the same
ownership rules. Realtime Session UI remains a documented exception.

### Stable frontend decisions

| ID | Decision | Verified by |
| --- | --- | --- |
| `FE-DEC-1` | Mount one in-memory TanStack Query client per App/test tree above both production and synthetic API-mode branches. Do not use a process-global singleton or persist the cache. | `WI-FE-02` |
| `FE-DEC-2` | Query owns fetch, cache, load, error, refetch, cancellation, and invalidation for migrated HTTP resources. The server remains authoritative. Query data is the only client copy of those resources. | `WI-FE-04` |
| `FE-DEC-3` | Query defaults start with no automatic retry, no window-focus refetch, no persistence, and no optimistic mutations. Feature hooks may later opt into bounded transient retries only after safe error classification and tests. | `WI-FE-05`, `WI-FE-15` |
| `FE-DEC-4` | Treat every Query/mutation cache entry as protected until an explicit reviewed public/protected split exists. Every transition out of API `ready`, plus trusted actor or Organization replacement, must cancel in-flight work, clear the complete QueryClient (including mutation variables and results), and remount the protected React subtree keyed by trusted actor and Organization identity. Query-cache clearing alone does not discard RHF, refs, or other local protected UI state. | `WI-FE-03`, `WI-FE-14` |
| `FE-DEC-5` | Query keys hold stable resource identities and a contract/projection version when parallel wire meanings can coexist. Keys are never authorization evidence and must not contain titles, source content, credentials, CSRF tokens, actor claims, or Organization identifiers used as scope. | `WI-FE-14` |
| `FE-DEC-6` | Feature query hooks compose typed/domain API clients and pass Query `AbortSignal` through those clients. Query must not call `fetch` directly or wrap `ProductionApiError`/domain outcomes in a new generic error model. | `WI-FE-06` |
| `FE-DEC-7` | Mutations are not optimistic for audited or authority-sensitive commands. After authoritative success, invalidate the documented keys with an exact filter and `refetchType: "none"` when navigation will unobserve the query, then navigate or refresh from server state. Expected revision, idempotency, 403/409, and uncertain outcomes remain defined by the typed/domain API layer. | `WI-FE-05`, `WI-FE-08` |
| `FE-DEC-8` | React Hook Form owns non-trivial form values, dirty/touched state, client errors, and submit coordination. Zod validates only client UX shape and basic constraints. Server authorization, eligibility, scope, revision, transitions, memory policy, and security rules remain server-authoritative. | `WI-FE-07`, `WI-FE-08` |
| `FE-DEC-9` | Lucide React is the standard general-purpose icon set via direct named imports. Icons that accompany visible text are decorative (`aria-hidden="true"`). Do not add an icon wrapper until repeated sizing, semantic, or accessibility behavior cannot stay consistent through direct use. | `WI-FE-09` |
| `FE-DEC-10` | Retain `tokens.css`, `components.css`, and `app.css`. Do not add Tailwind, CSS-in-JS, Axios, or Zustand. Simple ephemeral UI stays in component state; locally complex UI uses a focused reducer/hook before any state library. | `WI-FE-10`, `WI-FE-11`, `WI-FE-12` |
| `FE-DEC-11` | Realtime Session projection, SSE/`EventSource` lifecycle, pending command identity, reconciliation, reconnect, and Session isolation remain outside this Query/form migration. They may be refactored only in a dedicated task that preserves the reviewed contracts. | `WI-FE-13` |
| `FE-DEC-12` | Shell authentication, CSRF, logout, generation-based stale-response protection, actor/navigation bootstrap, and protected-state clearing remain in the existing API providers. Query cache clearing is a dependent lifecycle, not a replacement identity layer. Identity replacement remounts the protected subtree (`ProtectedAuthSubtree`) without remounting the API provider. | `WI-FE-03`, `WI-FE-06` |

## Consequences

- Positive: ordinary CRUD pages gain a shared, testable state and form
  boundary without changing product or Session contracts.
- Positive: authentication-context purge and generation guards have an
  explicit frontend realization for cached HTTP data.
- Negative: the repository takes on additional locked frontend packages and
  must keep supply-chain, license, and SBOM evidence current.
- Negative: unmigrated pages temporarily keep effect-based remote copies until
  later slices apply the same rules.
- Neutral: design-system visual meaning is unchanged; Lucide supplies shapes
  that must use existing icon sizes and semantic foreground roles.
- Neutral: ADR-010 continues to govern the React/Vite stack, pinning, and
  upgrade policy. This ADR governs narrower frontend state and library
  ownership.

## Implementation

Apply
[frontend architecture](../frontend-architecture.md)
for layering, provider placement, query keys, Activities coordination, form
initialization, and unmigrated surfaces. The first slice is documented there
and must not silently expand into Session, Assessment setup, Enrollment, My
work, Submission, Review, or Result/Release.

## Related

- Architecture: `AR-DEC-4`, `AR-DEC-12`, `AR-DEC-14` in
  [mvp-architecture.md](../mvp-architecture.md)
- Stack: [ADR-010](ADR-010-dotnet-implementation-stack-and-workspace.md)
  `STACK-DEC-4`, `STACK-DEC-16`
- Authorization: [ADR-002](ADR-002-authorization-enforcement-and-delegation.md)
- Session: [session-runtime-contract.md](../session-runtime-contract.md)
- Requirements: `REQ-ACT-1`–`REQ-ACT-13`, `REQ-ACT-35`–`REQ-ACT-42`,
  `AC-ACT-1`–`AC-ACT-7`, `AC-ACT-18`, `AC-ACT-22`–`AC-ACT-24`, `AC-ACT-27` in
  [assessment-setup.md](../../requirements/features/assessment-setup.md)
- UI/UX: [assessment-campaign-setup.md](../../ui-ux/assessment-campaign-setup.md)
  `UI-ACT-DEC-1`–`UI-ACT-DEC-6`
- Icons: [icon-shapes.md](../../ui-ux/design-system/components/icon-shapes.md)
- Does not supersede ADR-006, ADR-010, or ADR-011
