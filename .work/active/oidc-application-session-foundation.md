---
id: oidc-application-session-foundation
status: in-progress
created: 2026-08-20
updated: 2026-08-20
---

# Goal

Implement the production human-authentication foundation for Flex Agent:
Keycloak-backed OIDC Authorization Code flow with PKCE and server-side exchange,
a PostgreSQL-backed opaque application session, and production request/SSE
identity derived from that current application session rather than test headers
or provider roles.

This task is the first independently executable foundation in the hosted
Participant path. It advances but cannot by itself close full `AC-OPS-4`,
because production human-grant mutation and most protected feature endpoints do
not yet exist. It does **not** create or start a product Session. Hosted Session
start still depends on the unimplemented Activity/Cohort, Enrollment,
Submission, Attempt, acknowledgment, resolved-configuration, manifest, and
atomic start contracts and must be planned as a separate successor slice.

# Why this is a new task

No existing active task owns production human OIDC or application sessions.
The completed PostgreSQL authorization foundation, synthetic frontend journey,
production HTTP SSE, and Session subject-rehydration tasks each explicitly
defer this boundary. Reopening those retained completed tasks would blur their
reviewed scopes and evidence.

# Governing sources

- `AGENTS.md` — isolation, trusted-scope, sensitive-data, TDD, UI verification,
  and implementation-workflow rules
- `docs/README.md#authority-by-concern`
- `docs/product/concept-model.md` — Organization and Session isolation and
  authorization invariants
- `docs/product/mvp-scope.md` and `docs/product/overview.md` — P0 text-only
  assessment scope and remaining production gates
- `docs/requirements/mvp-operational-defaults.md`
  - `REQ-OPS-9`–`REQ-OPS-17` and `REQ-OPS-27`–`REQ-OPS-29`: PKCE/server exchange, browser token exclusion,
    opaque-cookie attributes and rotation, inactivity/absolute expiry,
    revocation, concurrent sessions, logout, MFA, and one-issuer portability
  - `AC-OPS-4`: login, privilege change, expiry, revocation, logout, protected
    requests, SSE freshness, and browser-storage evidence; this task owns the
    authentication/application-session foundation and existing protected SSE
    subset, while later grant/feature mutations must adopt its rotation contract
- `docs/requirements/features/auth-resource-isolation.md`
  - `REQ-AUTH-1`, `REQ-AUTH-2`, `REQ-AUTH-4`, `REQ-AUTH-7`,
    `REQ-AUTH-13`, `REQ-AUTH-15`, `REQ-AUTH-16`, `REQ-AUTH-19`–`REQ-AUTH-21`,
    `REQ-AUTH-24`–`REQ-AUTH-30`
  - `AC-AUTH-1`–`AC-AUTH-4`, `AC-AUTH-11`–`AC-AUTH-14`,
    `AC-AUTH-19`–`AC-AUTH-21`
- `docs/requirements/features/session-text-lifecycle.md`
  - `REQ-SESS-55`–`REQ-SESS-60`, `AC-SESS-9`, `AC-SESS-13`,
    `AC-SESS-14`, and `AC-SESS-32`: authorized replay/reconnect and cutoff
  - the current implementation-status rows that leave OIDC identity open
- `docs/ui-ux/activity-campaign-journey.md` — one authenticated Organization
  context per application session and no client-selected persona/scope
- `docs/ui-ux/text-session.md` — sign-in-to-continue, safe draft retention,
  reauthorization/reconciliation, and non-disclosing access-loss behavior
- `docs/ui-ux/design-system/README.md` and selected modules from
  `implementation-guide.md`: accessibility, layout, interaction states, status,
  buttons, alerts, protected content, and empty/loading
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`
  — application-owned authorization after authentication
- `docs/architecture/decisions/ADR-003-authorization-audit-persistence.md`
  — minimized append-only authentication/security audit
- `docs/architecture/decisions/ADR-006-mvp-architecture-baseline-and-evolution.md`
  — stable `(issuer, subject)` binding and API-owned application session
- `docs/architecture/decisions/ADR-008-bounded-oss-component-set.md`
  — Keycloak `26.7.x`, identity/MFA/logout/revocation/key-rotation/outage gates
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
  — ASP.NET Core OIDC, PostgreSQL-backed opaque session, thin endpoints,
  `GATE-STACK-HTTP`, `GATE-STACK-SESSION`, and multi-instance evidence
- Completed predecessors:
  - `.work/active/postgres-authorization-configuration-foundation.md`
  - `.work/active/p0-activity-journey-frontend-realization.md`
  - `.work/active/session-runtime-production-http-sse.md`
  - `.work/active/session-runtime-subject-binding-rehydration.md`

# Current implementation inventory

- `IdentityAccess` owns the ADR-002 PostgreSQL authorization kernel and service
  workload identity, but has no human OIDC identity or application-session
  model.
- Migration head is `0029`; `actors` and action-specific
  `actor_organization_grants` exist, but no stable human `(issuer, subject)`
  binding or application-session table exists.
- `GET /sessions/{sessionId}/events` has production PostgreSQL authorization,
  replay, and at-most-60-second relationship/grant revalidation. Its human
  identity adapter is disabled outside a Development/Testing test-header mode.
- The synthetic `/browser` adapter has an in-memory test-only cookie. It is not
  a reusable production application-session authority.
- The SPA uses only the synthetic `/browser` API. The repository has no
  production Activity, Enrollment, Submission, Attempt, Session-start, actor
  context, navigation, or Session-detail API.
- The repository has no Keycloak/NGINX OIDC callback reference composition or
  live human-identity contract evidence.

# Scope

## In

- Define a provider-neutral stable human identity binding from the exact
  configured issuer and exact provider subject to one pre-existing internal
  actor. Issuer and subject remain case-sensitive trusted identifiers; do not
  apply lossy URI, case, Unicode, or whitespace normalization after provider
  validation.
- Bind every application session to exactly one server-derived Organization
  context in addition to its stable human identity and actor. Zero or ambiguous
  eligible Organization contexts fail closed; the browser cannot select or
  supply Organization scope.
- Add an additive migration after `0029` for immutable human identity bindings,
  Organization-bound opaque application-session state, rotation lineage,
  individual revocation, bounded authentication strength, UTC lifecycle facts,
  terminal credential invalidation, and minimized audit references. Store no
  provider token or raw browser session secret.
- Generate a high-entropy opaque browser credential, store only its one-way
  lookup digest, and issue it in an `HttpOnly`, `Secure`, `SameSite=Lax`,
  path-bounded cookie with an explicit production name.
- Enforce 30 minutes of inactivity and at most 12 hours absolute lifetime, with
  configuration allowed to shorten but not widen either bound.
- Support concurrent application sessions with stable identities and individual
  revocation; rotate on login and through an explicit server-owned privilege
  change/sensitive-reauthentication boundary. Expose and test that narrow
  rotation contract without inventing the absent human-grant administration API.
- Add thin ASP.NET endpoints/composition for login, callback, current
  authentication state, logout, and a bounded same-origin return path. The
  current-session response is a minimal non-sensitive state projection and does
  not expose stable actor, identity, Organization, grant, or provider-claim
  identifiers merely to prove login.
- Use Authorization Code with PKCE, server-side code exchange, exact issuer and
  audience/client validation, bounded clock skew, nonce/correlation/state
  validation, and server-only client credentials.
- Load client credentials only through an approved server-side secret boundary;
  keep issuer/discovery/token/JWKS/end-session endpoints server-configured,
  exact-HTTPS, bounded, and unavailable to browser/request substitution. Do not
  follow redirects to an unapproved origin or rely on ambient proxy headers.
- Protect logout and every cookie-authenticated state-changing endpoint with an
  approved antiforgery/same-origin contract in addition to `SameSite=Lax`;
  validate forwarded scheme/host only through explicitly trusted gateway
  configuration.
- Replace the production SSE test identity with application-session identity.
  Revalidate both the application session and the current ADR-002
  Session-scoped authorization on held connections within 60 seconds.
- Fail closed on unknown subject, disabled local identity, zero/ambiguous
  Organization context, expired/revoked/rotated session, missing MFA strength
  where the current application-authorized action requires it, invalid provider
  response, unavailable required session/configuration state, or inconsistent
  session/actor/Organization state. A provider outage blocks new login and
  reauthentication; it does not silently revoke or prolong an already-valid
  database-authoritative application session.
- Keep authentication and authorization distinct: OIDC/provider roles never
  become Organization, Participant, Reviewer, Administrator, resource, or
  workflow authority.
- Add bounded telemetry and required security audit facts without cookie
  values, digests usable as bearer material, raw tokens, claims, credentials,
  Participant content, or protected resource data.
- Add the ADR-008-pinned Keycloak `26.7.0` local/CI contract profile and NGINX
  routing needed to exercise callback, logout, revocation, key rotation, MFA,
  account disablement, issuer/audience mismatch, clock skew, and provider outage
  with synthetic data.
- Update truthful implementation/readiness documentation after executable
  evidence exists.

## Out

- Account registration, invitation, password, recovery, identity merge, or
  automatic actor/Organization provisioning.
- A browser-selectable Organization, actor, Participant, relationship, role,
  grant, or authorization scope.
- Provider roles or groups as Flex Agent authorization.
- General Organization/member/grant administration UI or API.
- End-to-end privilege-change adoption by absent human-grant mutation surfaces;
  those future mutations must invoke the application-session rotation contract
  before full `AC-OPS-4` can be claimed.
- Activity/Cohort setup, Enrollment, Submission intake, Attempt entitlement,
  participant acknowledgment, resolved Session configuration, execution
  manifest creation, or atomic Session start.
- Production actor-context, navigation, My work, Session-detail, message,
  completion, review, release, or other feature APIs.
- Switching the synthetic SPA product journey to production data merely to
  demonstrate login. Product UI adoption belongs to the successor hosted
  workflow task and must use real authorized feature projections.
- Multi-issuer selection, social login, custom password flow, passkeys, or
  provider-specific public contracts.
- Production deployment, real Participant data, commits, pushes, pull requests,
  releases, or external credential creation.

# Acceptance and verification mapping

| Obligation | Planned implementation surface | Planned executable evidence |
| --- | --- | --- |
| PKCE and server-side exchange (`REQ-OPS-9`, `REQ-OPS-10`) | ASP.NET OIDC adapter and thin `/auth` endpoints | Code/state/nonce/correlation, wrong issuer/audience/key/nonce/state, callback replay, return-path injection, provider outage, and browser-storage tests |
| Stable provider-neutral identity and one Organization context (`REQ-OPS-17`, `REQ-OPS-27`, `REQ-OPS-28`, `REQ-AUTH-1`, `REQ-AUTH-4`, `PROP-UX-6`) | Exact issuer + exact subject identity binding plus server-derived actor and Organization-bound application session | Known/unknown/disabled/rebound subject, issuer substitution, case/Unicode variants, zero/one/multiple Organization contexts, client-supplied Organization rejection, duplicate binding, two-actor and two-Organization isolation tests |
| Opaque session and rotation (`REQ-OPS-11`, `REQ-OPS-14`) | PostgreSQL application-session coordinator, narrow privilege-change/reauth rotation contract, and secure cookie adapter | Fixation, login rotation, old-token rejection, direct rotation-contract tests, concurrent sessions, individual revocation, atomic/monotonic activity updates, terminal digest invalidation, multi-instance tests; future human-grant mutations remain adoption gaps |
| Expiry/revocation/logout (`REQ-OPS-12`–`REQ-OPS-15`, `REQ-OPS-29`) | Database-authoritative created/last-seen/expiry/revoked/rotated state, logout coordinator, and trusted provider-lifecycle bridge | Exact inactivity/absolute boundaries, shorter policy, clock skew, logout, provider-logout safe fallback, new-login and existing-session account disablement, forced logout within 60 seconds, provider outage, revocation race, and database failure |
| MFA (`REQ-OPS-16`) | Stored bounded authentication-strength evidence evaluated after application-owned resource relationship/action resolution | Participant policy, administrator/reviewer positive and negative MFA matrix, stale/forged claim denial, provider-role non-authority, sensitive-reauth rotation |
| Application authorization remains authoritative (`REQ-AUTH-1`–`REQ-AUTH-21`) | Trusted actor adapter feeds ADR-002 kernel; no provider-role mapper | Wrong org/participant/session/relationship, revoked grant, guessed ID, absent dependency, non-disclosing and side-effect-free denial |
| Protected SSE freshness (`REQ-OPS-13`, `REQ-AUTH-15`, `REQ-SESS-59`, `REQ-SESS-60`) | Existing Session event endpoint plus application-session revalidation | Session-cookie expiry/revoke/rotation while connected, grant/relationship revoke, reconnect with rotated cookie, stolen cursor/session ID, multi-instance replay |
| Audit/privacy (`REQ-AUTH-26`–`REQ-AUTH-29`) | Minimized authentication/session audit and bounded telemetry | Login/session/logout/deny facts; scans prove no token, cookie, secret, raw claims, provider payload, or protected content in DB audit, logs, errors, traces, or artifacts |
| Reference component gate (ADR-008) | Keycloak 26.7.0 + PostgreSQL 18 + NGINX test profile | PKCE, MFA, logout, revocation, key rotation, clock skew, new-login and existing-session account-disablement behavior, outage, restricted admin/health routes, locked/supply-chain/OCI checks |
| Cookie mutation and gateway trust | Antiforgery/same-origin enforcement, trusted forwarded headers, exact redirect URI and return-path handling | Cross-site logout/mutation, forged Origin/Host/Forwarded headers, downgrade, alternate callback, encoded/open redirect, and untrusted proxy tests |
| OIDC egress and secret isolation | Installed exact issuer/endpoints, bounded HTTP client, approved mounted/protected secret source | Endpoint/redirect substitution, alternate scheme/host/port/path, proxy injection, timeout/oversize, secret permission/rotation, and error/log leakage tests |

# Security and privacy threat model

| Threat or privacy harm | Planned control | Planned negative evidence |
| --- | --- | --- |
| Session fixation or raw-token theft | Rotate after login/privilege change/reauth; high-entropy opaque cookie; digest-only storage; secure attributes | Fixed pre-login cookie, replay of rotated/revoked cookie, database read artifact and browser-storage scan |
| Login CSRF, callback replay, or open redirect | PKCE, state, nonce, correlation cookie, single-use callback, same-origin path allowlist | Missing/mismatched/replayed state/nonce/code and external/protocol-relative/encoded return paths |
| Cookie-authenticated request CSRF or gateway spoofing | Antiforgery/same-origin checks and explicit trusted-proxy configuration | Cross-site logout/mutation, forged Origin/Host/Forwarded headers, HTTPS-downgrade and callback-host substitution |
| Provider claim becomes application authority | Map only exact issuer/subject to a pre-provisioned actor; invoke ADR-002 for every resource action | Forged provider roles/groups/org IDs, wrong Organization grants, guessed resources |
| Cross-tenant identity collision or ambiguous tenant context | Composite exact issuer/subject uniqueness, actor binding, and one server-derived Organization per application session; no subject-only authorization or client Organization selector | Same subject under another issuer, case/Unicode variants, rebound actor, zero/multiple Organization contexts, duplicate concurrent binding |
| Stale or revoked application session remains live | Database authority on every protected request and within 60 seconds on SSE | Revoke/expire/rotate while a request or stream is held across two API instances; account disablement only through the resolved trusted propagation mechanism |
| MFA bypass | Store only validated bounded authentication strength and require it by application action | Missing, stale, malformed, or provider-role-only MFA evidence for Administrator/Reviewer access |
| Token/claim/content leakage | No provider token in browser storage; no raw session secret in DB; bounded logs/audit/errors | Canary credential/token/claim/content scans across response, DB, logs, telemetry, screenshots, traces, and support output |
| Session exhaustion or lookup abuse | Positive size/count/rate bounds, indexed digest lookup, bounded DB/network timeouts | Oversized cookie/state, login/callback flood, random-token storm, provider slowness/outage |
| OIDC metadata/token egress escape or client-secret disclosure | Server-installed exact HTTPS endpoints, redirect/origin checks, approved secret source, bounded response and timeout, redacted diagnostics | Alternate host/scheme/port/path, redirect, proxy/header substitution, oversized metadata/token payload, canary secret scans |
| Ambiguous or excessive authentication retention | Immediate bearer/digest invalidation at terminal state; minimized metadata separated from Organization authorization audit and governed by approved lifecycle policy | Expired/revoked/rotated lookup denial, cleanup/minimization, backup/log/artifact scans, retained-lineage verification without usable credentials |

# Resolved decisions

All former `OQ-AUTHN-*` items were resolved on 2026-08-20. Product-owned
behavior was promoted to MVP operational defaults v0.3 and Activity journey
v0.3; delivery sequencing was promoted to Product overview v0.4; delegated
technical realization was promoted to `STACK-DEC-19` through `STACK-DEC-25` in
ADR-010. The retained identifiers preserve planning traceability.

- **OQ-AUTHN-1 — Human identity provisioning.** Resolution: require an
  operator-pre-provisioned exact `(issuer, subject) -> actor` binding and deny
  unknown subjects without creating an actor, Organization membership, or
  grant. Rationale: registration/invitation/account recovery are explicitly out
  of scope and authentication must not create authorization.
- **OQ-AUTHN-2 — Exact Keycloak MFA evidence.** Resolution: accept only an
  operator-configured allowlist of explicit `acr`/`amr` evidence qualified by
  the pinned Keycloak profile; deny Administrator/Reviewer access when the
  required evidence is absent or unrecognized. The exact allowlist is a pinned
  deployment-profile value qualified by contract tests, not a public product
  contract. Rationale: this denies ambiguous claims while allowing the selected
  Keycloak profile to evolve through reviewed configuration.
- **OQ-AUTHN-3 — Provider-token retention.** Resolution: discard provider
  access/ID tokens after validated login and retain no refresh token in this
  slice. Rationale: the application session owns browser continuity for at most
  12 hours, and avoiding refresh credentials minimizes sensitive state.
- **OQ-AUTHN-4 — Provider logout failure.** Resolution: revoke the Flex
  Agent application session atomically first, clear the cookie, then attempt a
  bounded provider logout redirect when the qualified profile supports it; a
  provider outage cannot restore or prolong the local session. Rationale:
  application revocation is authoritative and must fail safe.
- **OQ-AUTHN-5 — Product browser adoption.** Resolution: do not route the
  existing synthetic SPA through production authentication until real scoped
  actor-context and feature APIs exist. Use API/integration/browser contract
  evidence for this foundation. Rationale: a login-only shell backed by
  synthetic product data would misrepresent hosted readiness.
- **OQ-AUTHN-6 — Organization context at login.** Resolution: after exact
  identity mapping, resolve exactly one currently eligible Organization context
  from trusted server records and bind it immutably to the new application
  session; deny login completion when there are zero or multiple eligible
  contexts. Rationale: approved `PROP-UX-6` permits one Organization context per
  application session and no MVP switcher, while client-selected scope would
  violate `REQ-AUTH-16`.
- **OQ-AUTHN-7 — Multi-instance OIDC transaction protection.** Resolution:
  persist the shared ASP.NET Core Data Protection key ring in PostgreSQL,
  encrypt it at rest with an operator-managed protection key, and use bounded,
  expiring, atomically consumed PostgreSQL OIDC transaction records. Callback
  affinity and instance-local correlation/PKCE authority are prohibited.
  Rationale: callback, state, nonce, and PKCE verification must survive instance
  changes and reject replay.
- **OQ-AUTHN-8 — Provider account disablement and upstream logout.** Resolution:
  use validated Keycloak back-channel logout plus a restricted authenticated
  Keycloak lifecycle event-listener bridge for account disablement. The bridge
  revokes matching local sessions within 60 seconds; provider outage blocks new
  login/reauthentication but does not revoke or prolong a valid local session.
  Rationale: positive trusted propagation meets the approved bound without
  retaining provider tokens or polling provider availability on every request.
- **OQ-AUTHN-9 — Authentication audit ownership before Organization context.**
  Resolution: IdentityAccess owns a separate deployment-scoped,
  append-only/minimized authentication security event stream for pre-context
  login/callback facts; Organization-scoped authorization events continue to
  use ADR-003 after context resolution. Rationale: the existing `audit_events`
  schema requires an Organization and must not be populated with a fabricated
  tenant. Retain minimized authentication-security metadata for the approved
  730-day lifecycle.
- **OQ-AUTHN-10 — Inactivity activity source.** Resolution: a successfully
  authenticated browser request, command, or client-initiated SSE connect or
  reconnect may advance activity atomically using database time; server-only
  heartbeats, polling, and revalidation do not. Rationale: background server
  traffic must not keep an abandoned browser session alive. Conservative write
  coalescing is permitted only when it cannot extend the inactivity bound.
- **OQ-AUTHN-11 — Terminal application-session retention.** Resolution:
  expiry, revocation, rotation, and logout make the lookup digest unusable
  immediately; retain only the minimized lineage/security metadata required by
  the approved 730-day authentication-security metadata lifecycle. Rationale: a
  terminal credential has no execution purpose, while audit-relevant history
  must remain inspectable.

# Readiness gates before behavior implementation

- Phase 0 inventory, baseline verification, schema TDD, and OIDC host TDD may
  begin. All product and architecture prerequisites for implementation have
  been approved and promoted.
- No Production/Staging claim is allowed until the exact
  Keycloak/NGINX live matrix, multi-instance callback/session behavior,
  leakage scans, and independent security/privacy review pass. Full `AC-OPS-4`
  additionally remains open until future production privilege-change and
  protected feature surfaces adopt and verify the rotation/session contract.

# Plan

- [x] Reconcile the promoted authentication/session contract with the current
      host, IdentityAccess, PostgreSQL, NGINX, API-test, and migration seams;
      freeze the test matrix and record any implementation-only discoveries
      without reopening the approved product or architecture decisions.
- [x] Domain and application coordinators for exact identity, one Organization
      context, opaque digest-only sessions, rotation, logout, and MFA strength.
- [x] Additive migration `0030_human_identity_and_oidc_application_state.sql`
      plus Postgres and in-memory stores.
- [x] Thin `/auth` login, callback, current-session, logout, back-channel, and
      restricted lifecycle endpoints with PKCE, cookie, and antiforgery.
- [x] Production SSE identity uses the application session; Development/Testing
      harness remains an explicit fallback when no session cookie is present.
      Held streams re-authenticate without advancing activity.
- [x] Pinned Keycloak `26.7.0` / PostgreSQL 18 / NGINX `1.30.4` compose profile
      and contract documentation.
- [x] External review of `74ef167`: PostgreSQL advisory-lock `(integer, integer)`,
      tombstone on initial login insert, and bounded unknown-`kid` JWKS refresh.
- [x] External review of `6f5f555`: identity logout watermark vs late `sub`-only
      callback, and JWKS request-local RSA snapshots.
- [x] External review of `44a697b`: scope `sid+sub` logout to one provider
      session, and fail closed on malformed JWKS RSA parameters.
- [x] External review of `5c0b539`: approve the code change; dispose the
      failed RSA in `TryFromParameters`; add a PostgreSQL sibling-session
      logout case; record approval in task and readiness docs.
- [>] Remaining evidence: Docker-backed PostgreSQL/migration/live Keycloak
      matrix, leakage scans, and independent security review. Docker is not
      running in this execution environment.
- [ ] Run and record the clean focused baseline: IdentityAccess/Runtime,
      PostgreSQL integration, Session subscription/replay, architecture,
      migration-history, locked restore, docs, and whitespace checks.
- [x] Red — add domain/application and migration tests for stable human identity,
      digest-only opaque sessions, login/rotation lineage, concurrent sessions,
      inactivity/absolute expiry, individual revocation, MFA strength, audit
      minimization, immutable history, and populated `0029 -> next` upgrade.
- [x] Green — add the minimum additive schema and IdentityAccess coordinators;
      keep cryptographic/session material behind narrow interfaces, database
      time authoritative, queries bounded, and failures non-disclosing.
- [x] Red — add HTTP/OIDC contract tests for PKCE, server exchange, issuer,
      audience, nonce/state/correlation, callback replay, safe return paths,
      cookie attributes, antiforgery, trusted proxy/forwarded headers, login
      fixation, logout, exact-endpoint egress, client-secret isolation,
      provider failure, and Production/Staging fail-closed composition.
- [x] Green — compose the thin ASP.NET login/callback/current-session/logout
      endpoints and provider-neutral trusted-actor adapter without exposing
      provider tokens or claims to the SPA or authorization kernel.
- [x] Red — extend protected-request and held-SSE tests so expired, revoked,
      rotated, disabled, wrong-issuer, insufficient-MFA, and cross-instance
      application sessions lose authority immediately for new requests and
      within 60 seconds for streams while current Session/grant checks remain.
- [x] Green — replace production SSE test identity with application-session
      identity and revalidate both authentication and ADR-002 authorization at
      the bounded freshness point; preserve Development/Testing-only test
      identity as an explicit separate harness profile.
- [!] Build the pinned Keycloak/NGINX local/CI contract profile and run the live
      synthetic identity matrix: PKCE, MFA, logout, revocation, key rotation,
      clock skew, account disablement, upstream logout propagation, provider
      outage, multi-instance login callback/API/session behavior, and restricted
      infrastructure routes. Use no real credentials or user data and report
      new-login versus existing-session behavior separately.
- [>] Run focused then aggregate regression, PostgreSQL migration/concurrency/
      fault tests, architecture, locked dependency, supply-chain, OCI, docs,
      whitespace, leakage scans, and applicable browser-flow evidence. Record
      exact commands, counts, artifacts, and unavailable gates.
- [ ] Reconcile actual changes against governing sources, update truthful
      implementation/readiness rows, run independent backend/architecture and
      security/privacy review, resolve blockers, and retain this task for the
      successor hosted workflow handoff.

# Current state

External review of `5c0b539` **approves the code change**. The remaining P3
failed-RSA disposal leak is fixed, and a PostgreSQL sibling-session
`sid+sub` logout case is added. Docker still blocks execution of
`0033`/that integration test and the live Keycloak matrix. Full `AC-OPS-4`
stays Partial. The foundation is not marked complete.

The next successor after this task is **not** a direct Session-row creation
endpoint. It must implement or depend on approved Activity/Cohort activation,
Enrollment, Submission/Attempt entitlement, acknowledgment, resolved
configuration, manifest, exact Submission-version binding, and the ADR-005
atomic Session-start boundary before exposing hosted Participant start.

# Decisions

- External review of `5c0b539` approved the human-authentication code change
  on 2026-08-20. Remaining work is evidence, not a merge-blocking defect:
  dispose the failed RSA in `TryFromParameters` (done), run Docker-backed
  PostgreSQL/`0033` including the sibling-session logout case, and run the
  live Keycloak/NGINX matrix before claiming foundation completion.

# Findings / deviations

- External review of `5c0b539` (approved): `sid+sub` logout scoping and
  malformed-JWKS fail-closed handling cleared the prior P1/P2 blockers. The
  P3 uncommitted-RSA dispose leak is fixed. The PostgreSQL sibling-session
  case is added but unexecuted here because Docker is unavailable.
- External review of `be6ad7f` (fixed): login `state` is bound to a short-lived
  `HttpOnly`/`Secure`/`SameSite=Lax` correlation cookie required on callback
  and cleared on success and failure.
- External review of `be6ad7f` (fixed): logout `jti` claim and session
  revocation commit in one transaction; a successful replay is idempotent 204
  so the OP can retransmit after a recoverable error.
- External review of `be6ad7f` (fixed): provider logout tombstones the
  provider-session digest and shares advisory/identity locks with rotation so
  a successor cannot remain live after logout wins.
- External review of `44a697b` (fixed): a logout token with both `sid` and
  `sub` revokes only the intersecting provider session and does not watermark
  the identity. Malformed JWKS RSA parameters fail closed without throwing.
- External review of `6f5f555` (fixed): `sub`-only logout writes an
  `(issuer, subject)` watermark from logout `iat`; login insert rejects an
  ID-token `iat` at or before that watermark. JWKS cache stores parameters and
  returns request-owned RSA snapshots.
- External review of `74ef167` (fixed): `pg_advisory_xact_lock` now uses two
  `integer` keys; login insert shares the provider lock and tombstone;
  unknown-`kid` JWKS refresh is single-flighted with a per-URI cooldown.
- External review of `be6ad7f` (fixed): unknown JWKS `kid` refreshes the cache
  once before validation fails closed.
- External review of `2a6ca65` (fixed): ID tokens require `iat` and measure
  lifetime as `exp - iat`; `nbf` is validated only when present.
- External review of `2a6ca65` (fixed): rotation is one transactional CAS
  (exactly one live predecessor row, successor insert, unique predecessor).
- External review of `2a6ca65` (fixed): differing `ConnectionStrings:Identity`
  and `ConnectionStrings:Sessions` fail closed; split databases are unsupported.
- External review of `2a6ca65` (fixed): logout tokens require `iat`, `exp`,
  `jti`, and the back-channel `events` member, reject `nonce`, accept `sub`
  and/or `sid`, and consume `jti` once. `sub`-only logout revokes identity
  sessions without disabling the actor.
- Follow-up review (fixed): unsigned back-channel logout tokens no longer
  revoke sessions; logout tokens are signature-validated without a login nonce.
- Follow-up review (fixed): privilege-change rotation keeps the provider-session
  digest so forced logout still matches the rotated session.
- Follow-up review (fixed): SSE denies when the application-session Organization
  does not match the Session subject Organization; interactive humans use
  `human.interactive`.
- Follow-up review (fixed): configured inactivity, absolute lifetime, and clock
  skew are bounded; Production/Staging complete composition requires mounted
  lookup/transaction secrets.
- Self-review: MFA on SSE applies only when an application session is present.
  The Development/Testing harness is not treated as MFA evidence.
- Self-review: login CSRF/open-redirect, PKCE, cookie attributes, callback
  replay, and client-supplied Organization rejection are covered by runtime
  tests. Clock-skew, key-rotation, multi-instance callback, and leakage scans
  still need Docker-backed evidence.
- Rate-limit/flood bounds from the threat model are not implemented in this
  slice; rely on gateway policy until a dedicated limiter is added.
- The previous high-level next-item description grouped authentication and
  hosted Session start too closely. Repository inspection shows that OIDC is
  independently executable, while authoritative Session start is blocked by
  several unimplemented P0 domain workflows.
- Existing production SSE authorization is reusable, but its identity adapter
  authenticates only at initial connection. Application-session revocation
  requires reauthentication at the existing at-most-60-second revalidation
  point, not only repeated resource authorization for a cached actor.
- The synthetic `/browser` cookie demonstrates presentation behavior only. It
  is in-memory, test-only, `SameSite=Strict`, and intentionally not the approved
  PostgreSQL-backed production session.
- The repository has no current Keycloak reference composition. Component-live
  qualification is therefore part of this task rather than assumed evidence.
- Review finding (high): the initial plan mapped identity only to an actor and
  omitted the one-Organization application-session context required by approved
  `PROP-UX-6`. The plan now requires a server-derived, immutable Organization
  binding and fails closed on zero or ambiguous contexts.
- Review finding (high): the initial plan required multi-instance evidence but
  did not define shared protection for OIDC state/nonce/correlation/PKCE across
  callback instances. `STACK-DEC-20` now governs a protected shared key ring and
  durable single-use OIDC transactions; instance affinity is not accepted as
  authority.
- Review finding (high): the initial plan treated provider outage/account
  disablement as if it automatically revoked current application sessions. The
  plan now distinguishes new authentication from existing database-authoritative
  sessions; `STACK-DEC-22` governs the trusted back-channel logout and
  account-disablement event bridge required before claiming propagation.
- Review finding (medium): cookie-authenticated mutation CSRF, forwarded-header
  trust, exact OIDC egress, client-secret isolation, pre-Organization audit
  ownership, inactivity touch semantics, and terminal credential lifecycle were
  incomplete. They are now explicit scope, threats, tests, and readiness gates.
- Review finding (medium): the initial completion criteria claimed all of
  `AC-OPS-4` even though human-grant mutations are out of scope and absent from
  the repository. The task now claims only the authentication/application-
  session foundation and existing protected SSE subset, supplies a narrow
  rotation contract, and retains full privilege-change adoption as a later gap.
- Frontend visual review is not applicable to this plan revision because the
  task explicitly avoids product UI changes. Any later UI adoption must load
  the frontend workflow and produce live Playwright accessibility/screenshot
  evidence against real authorized feature APIs.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Existing-task and implementation inventory | passed | No active task owns production OIDC/application sessions; completed predecessors explicitly defer it |
| Governing source review | passed for planning | Product, requirements, UI/UX, design-system, ADR, task-state, host, identity, migration, SSE, and SPA seams inspected on 2026-08-20 |
| Independent plan consistency review | passed with updates | Backend, frontend-boundary, architecture, and security/privacy review identified and incorporated Organization-context, multi-instance OIDC-state, provider-propagation, CSRF/egress/secret, audit, inactivity, and lifecycle gaps |
| Decision promotion | passed | Approved business behavior promoted to operational defaults v0.3 and Activity journey v0.3; sequencing promoted to Product overview v0.4; delegated technical decisions promoted to ADR-010 `STACK-DEC-19`–`STACK-DEC-25` |
| `python3 scripts/check_docs.py` | passed | Internal links/fragments, requirement IDs, terminology, Mermaid fences, feature catalogs, and tier checks passed on 2026-08-20 |
| `git diff --check` | passed | No whitespace errors after promotion |
| Baseline tests | not run | Planning-only update; first execution step owns observed baseline evidence |
| Live Keycloak/NGINX flow | not run | No reference composition exists yet |
| UI/Playwright | not applicable to this plan revision | No product UI change is planned in this foundation; browser OIDC contract evidence remains required during implementation |
| Review-fix confirmation pass | passed | Re-read validator/CAS rotate/logout/connection-string paths; focused runtime **35** and architecture **35** passed again on 2026-08-20. Docker still unavailable |
| `be6ad7f` follow-up runtime | passed | `FlexAgent.Runtime.Tests` **167** passed, including login CSRF correlation, atomic/idempotent logout, rotate-vs-logout, and JWKS unknown-`kid` refresh |
| `be6ad7f` follow-up confirmation pass | passed | Re-read correlation cookie, atomic JTI+revoke, rotate/logout locks+tombstone, and unknown-`kid` JWKS refresh; focused runtime **20** and architecture **35** passed again on 2026-08-20. Docker still unavailable |
| Architecture module/session ownership | passed | 35 tests in `FlexAgent.Architecture.Tests` after renaming `0031` so it is not treated as a Sessions script |
| `python3 scripts/check_docs.py` | passed | After Keycloak back-channel URL documentation |
| `git diff --check` | passed | No whitespace errors in the review-fix diff |
| PostgreSQL integration / migration `0031` | blocked | Docker socket unavailable; `HumanAuthenticationPersistenceTests` and `MigrationUpgradeTests` compiled but were not executed |
| Live Keycloak 26.7.0 back-channel logout | blocked | Same Docker unavailability; realm now sets `backchannel.logout.url`/`adminUrl`; `KeycloakBackChannelLogoutTests` compiled |
| Leakage scans / supply-chain / OCI | not run | Not executed in this environment |
| Independent security/privacy review | approved for the `5c0b539` code change | External review approved `sid+sub` logout scoping and JWKS fail-closed handling; Docker-backed PostgreSQL/`0033` and live Keycloak remain required before foundation completion |
| `74ef167` follow-up runtime | passed | Focused human-auth/OIDC/JWKS/advisory runtime **41** passed on 2026-08-20, including login-after-logout tombstone and unknown-`kid` cooldown |
| `74ef167` follow-up architecture | passed | `FlexAgent.Architecture.Tests` **35** passed on 2026-08-20 |
| `74ef167` follow-up hosts | passed | `FlexAgent.Api` and `FlexAgent.Worker` built with 0 warnings |
| `git diff --check` | passed | No whitespace errors after the `74ef167` follow-up |
| `74ef167` confirmation pass | passed | Re-read advisory-key types, TryInsertLiveSession tombstone+locks, and JWKS single-flight/cooldown/dispose; focused runtime **41** and architecture **35** passed again on 2026-08-20 |
| `6f5f555` follow-up runtime | passed | Focused human-auth/OIDC/JWKS runtime **43** passed on 2026-08-20, including stale `sub`-only remint denial and JWKS snapshot verify-after-refresh |
| `6f5f555` follow-up architecture | passed | `FlexAgent.Architecture.Tests` **35** passed on 2026-08-20 |
| `6f5f555` follow-up hosts | passed | `FlexAgent.Api`, `FlexAgent.Worker`, and Postgres integration tests compiled |
| PostgreSQL integration / migration `0033` | blocked | Docker still unavailable; `identity_logout_watermarks` is additive and unexecuted here |
| `6f5f555` confirmation pass | passed | Re-read identity watermark + `iat` comparison, request-local JWKS snapshots, and in-flight parameter sharing; focused runtime **43** and architecture **35** passed again on 2026-08-20 |
| `44a697b` follow-up runtime | passed | Focused human-auth/JWKS/workload runtime **62** passed on 2026-08-20, including `sid+sub` sibling-session survival and malformed JWKS fail-closed |
| `44a697b` follow-up architecture | passed | `FlexAgent.Architecture.Tests` **35** passed on 2026-08-20 |
| `44a697b` confirmation pass | passed | Re-read sid-only / sub-only / sid+sub logout branches and JWKS import fail-closed; focused runtime **62** and architecture **35** passed again on 2026-08-20 |
| `5c0b539` approval cleanup | passed | Failed-RSA dispose and focused JWKS/logout runtime tests; docs readiness rows updated. PostgreSQL sibling-session case compiled, not executed |

# Blockers

Docker is not running in this execution environment, so Testcontainers
PostgreSQL, migration-upgrade proof, and the live Keycloak/NGINX matrix cannot
be observed here. Start Docker and re-run
`FlexAgent.Postgres.Integration.Tests` plus
`deploy/compose/keycloak-contract.compose.yaml` before claiming those gates.
No product or architecture decision is blocking the remaining evidence.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] The applicable `REQ-OPS-9`–`REQ-OPS-17`, `REQ-OPS-27`–`REQ-OPS-29`, and `AC-OPS-4` authentication/application-session plus protected-SSE subset has executable evidence without overstating absent privilege-change or feature APIs
- [ ] Stable identity and application-session persistence are isolated, append-only where audit-relevant, migration-safe, and multi-instance capable
- [ ] Every application session has one trusted server-derived Organization context; zero/ambiguous/client-supplied contexts fail closed
- [ ] Provider tokens/roles never become browser storage or application authorization
- [ ] Login, rotation, expiry, revocation, logout, MFA, key rotation, account disablement, upstream logout, outage, and concurrent-session evidence passes with new-login/existing-session semantics stated accurately
- [ ] OIDC transient transaction protection works across API instances without instance affinity or an unshared Data Protection key ring becoming authority
- [ ] Cookie mutations, forwarded headers, return paths, OIDC endpoints, client credentials, and provider HTTP behavior fail closed under the approved CSRF, gateway, egress, secret, timeout, and size contracts
- [ ] New protected requests and held SSE connections enforce current application-session and ADR-002 authority within the approved bounds
- [ ] Negative cross-Organization/actor/session, fixation, replay, return-path, leakage, and failure matrices pass
- [ ] Applicable focused, integration, concurrency, migration, architecture, locked, supply-chain, OCI, documentation, and whitespace checks pass
- [ ] Governing implementation-status rows are truthful and successor hosted-workflow dependencies are explicit
- [ ] Full `AC-OPS-4` remains explicitly Partial until production human-grant mutations and later protected feature endpoints adopt the rotation/session contract
- [ ] Independent backend/architecture and security/privacy findings are resolved
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
