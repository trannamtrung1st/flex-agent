---
id: session-runtime-worker-identity-invocation-delegation
status: completed
created: 2026-08-18
updated: 2026-08-18
predecessors:
  - session-runtime-worker-binding-timer-activation
  - session-runtime-worker-host-wiring
  - session-runtime-subject-binding-rehydration
---

# Goal

Authenticate the production Worker as one current, provisioned service identity
and require one durable, bounded `session.invocation.execute` delegation for
every hosted Invocation work item. The two production lanes remain independently
gated: timer polling requires fresh workload identity plus the existing bounded
`session.timer_lane.fire` delegation, while Invocation processing requires fresh
workload identity plus bounded `session.invocation.execute` delegation at claim,
protected-read/model admission, publication, completion/effect, and work-state
commit boundaries. Both lanes also require immutable Session binding,
authorization, audit, persistence, and their explicit host capability.

This slice proves the authorization boundary with deterministic provider
adapters. It does not enable or qualify a live model provider.

# Governing sources

- `AGENTS.md`, `.agents/skills/implementation-workflow/SKILL.md`, and
  `.work/README.md` — tracked execution, specification-driven TDD, review, and
  evidence rules
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — canonical Session/Invocation meaning, P0 text
  boundary, and remaining Worker/provider production gates
- `docs/requirements/features/auth-resource-isolation.md` — `REQ-AUTH-1`,
  `REQ-AUTH-2`, `REQ-AUTH-11`, `REQ-AUTH-15`, `REQ-AUTH-17`–`REQ-AUTH-22`,
  `REQ-AUTH-25`–`REQ-AUTH-31`, `AC-AUTH-16`, and `AC-AUTH-17`
- `docs/requirements/features/resolved-session-configuration.md` —
  `REQ-RSC-15`–`REQ-RSC-20`, `REQ-RSC-24`, `REQ-RSC-28`,
  `REQ-RSC-47`–`REQ-RSC-55`, and `AC-RSC-26`–`AC-RSC-28` for immutable runtime
  binding, protected references, P0 capability narrowing, and fail-closed
  resolution
- `docs/requirements/features/session-text-lifecycle.md` — `REQ-SESS-55`–
  `REQ-SESS-70`, `REQ-SESS-75`, `REQ-SESS-78`–`REQ-SESS-85`, and applicable
  `AC-SESS-32`–`AC-SESS-48`
- `docs/architecture/mvp-architecture.md` — service identity, durable work,
  delegation, lease, retry, and commit-time authorization boundaries
- `docs/architecture/session-runtime-contract.md` — Worker execution,
  durable-before-display publication, cutoff, idempotency, and recovery
- Approved `ADR-002`, `ADR-003`, `ADR-008`, `ADR-010`, `ADR-011`, `ADR-012`,
  `ADR-013`, `ADR-014`, `ADR-015`, and `ADR-016`; ADR-015 governs the
  implemented timer-lane delegation realization, while ADR-016 governs Worker
  workload identity and bounded Invocation delegation
- `.work/active/session-runtime-worker-binding-timer-activation.md` — completed
  predecessor, explicit production guards, and successor boundaries
- `.work/active/structured-agent-runtime-traceability.md` — completed runtime
  foundation mapping and residual production gates

# Scope

## In

- Decide and approve one portable workload-identity contract plus one
  self-hostable reference authentication profile. Separate credential
  acquisition, cryptographic authentication, stable principal binding, and
  product authorization.
- Bind an authenticated stable external principal to exactly one active
  IdentityAccess service actor through authoritative, versioned state. A host
  flag, actor GUID, process/container location, database credential, network
  address, or unvalidated token claim is never authentication.
- Provision, revoke, replace, and inspect the external-principal-to-service-actor
  binding through an explicit operator boundary with immutable/versioned history
  and durable audit. Rotate workload-authentication credentials through the
  approved secret boundary without rewriting a still-valid stable principal
  binding. Persist no raw token or secret in product tables, audit, logs,
  telemetry, or artifacts.
- Gate new claims on a fresh authenticated workload session. Expiry, revocation,
  refresh failure, subject/issuer/audience mismatch, binding-version mismatch,
  or unavailable identity state stops new protected work and makes readiness
  honest without killing liveness.
- Add the `session.invocation.execute` action and the minimum additive,
  immutable-schema support needed to carry its delegation reference on every
  `execute_invocation` durable work envelope. Preserve migrations `0001`–
  `0024`; use the next available additive migration (`0025` at planning time)
  after rechecking the repository before the first schema edit.
- Issue the bounded Invocation delegation from trusted Session/start context and
  persist its reference on the owning protected Session execution record or
  equivalent approved authority source, then link it to new participant- and
  timer-triggered Invocation work atomically. Timer-fire authority remains the
  distinct `session.timer_lane.fire` delegation, but its commit must require and
  attach a current downstream `session.invocation.execute` delegation whenever
  it creates Invocation work. Historical work without a provable delegation
  remains fail-closed; do not infer or identifier-backfill authority.
- Authorize the authenticated service, exact Organization/Activity/Participant/
  Attempt/Session ownership, action, delegation, work identity, lifecycle,
  frozen policy, cutoff, and lease before a model call or protected content
  read.
- Reauthorize workload identity, delegation, ownership, lifecycle/cutoff,
  expected version, work lease, and idempotency inside every sensitive commit
  boundary: response fragment, response seal, Invocation completion/effect,
  unpublished-failure persistence, and protected claim terminalization.
- Revalidate the authenticated principal's current durable actor binding as well
  as `session.timer_lane.fire` delegation at timer admission and immediately
  before the timer-fire commit. Startup authentication or a configured actor id
  must not become permanent timer authority.
- Roll back protected work before persisting a bounded denial audit. An audit
  failure must not leave staged Session, work, fragment, Decision, effect,
  success-audit, or outbox mutations committable.
- Preserve fair claiming, lease recovery, duplicate reconciliation, no-action,
  partial publication, timer-triggered Invocation, pause/revocation/cutoff, and
  fail-closed provider behavior.
- Permit Production/Staging host composition only when the selected workload
  identity profile is authenticated and each separately requested lane has its
  own dependencies. Keep both flags default `false`; do not require Invocation
  processing merely to authenticate timer polling, or treat authenticated timer
  polling as permission to process Invocation work.
- Add bounded readiness, metrics, and logs for identity state, delegation
  eligibility, authorization denials, claim outcomes, and refresh failures
  without stable Organization/Session/Invocation/delegation identifiers or
  secrets.
- Reconcile ADR status, authoritative specification implementation tables,
  operations/deployment guidance, traceability, and this task against executable
  evidence.

## Out

- Live OpenAI, Azure OpenAI, OpenAI-compatible, or other model-provider
  adapters; model/deployment qualification; Organization BYOK; provider
  `SecretSource`; long-provider-call heartbeat qualification
- Human/Participant OIDC application sessions, login UX, production browser
  migration, hosted HTTP Session-create/start commands, or general IdentityAccess
  administration UI
- Cloud-vendor-specific managed identity as the only supported contract;
  Kubernetes, SPIFFE/SPIRE, a remote authorization service, or a custom secret
  manager without separately approved evidence
- Timer-delegation renewal, Invocation-delegation renewal beyond the approved
  lifetime decision, or silent extension of expired authority
- Evaluation, Review/Release, Submission intake, lifecycle/export,
  backup/restore, timer-storm/load, OTLP Collector, or production-pilot
  certification
- Interaction Controller, voice, tools, Dynamic memory, arbitrary timers, or
  richer workflow triggers
- Rewriting applied migrations `0001`–`0024`
- Commits, pushes, pull requests, deployment, or release unless separately
  requested

# Confirmed current seams

- `Sessions:WorkerServiceActorId` is required for synthetic protected lanes but
  is deployment configuration only; Production/Staging currently refuse both
  protected processors for the appropriate missing production gates.
- The Worker directly composes PostgreSQL Session, binding, authorization,
  Invocation work, publication, and timer adapters. The workload identity
  boundary must therefore gate the composition root and sensitive coordinators,
  not rely on a human HTTP request context.
- `service_delegations` and the ADR-002 kernel already validate actor, action,
  Organization, complete Session ownership, effective/expiry, revocation, and
  version for timer work.
- `session_durable_work` contains trusted ownership and Invocation business key
  but no delegation reference. Its current claim update does not call the
  authorization kernel.
- `PostgresInvocationWorkSessionGateway.TrySaveCompletionAsync`,
  `PostgresPublishAgentResponseCoordinator`, claim release/completion, and
  unpublished-failure persistence commit protected state without the bounded
  Invocation delegation required for production Worker execution.
- `PostgresFireDueTimerCoordinator` validates the configured service actor and
  timer delegation at admission/commit, but no authenticated external principal
  is currently bound to that actor at those boundaries.
- `WorkClaimGate.StopAcceptingWork()` is an intentionally one-way shutdown gate.
  Recoverable identity expiry/refresh must use a separate authorization gate;
  authentication recovery must never reopen a Worker that has begun shutdown.
- Migrations through `0024` are applied history and must remain immutable.

# Plan

- [x] Governance gate — ADR-015 received its named Product, Architecture, and
      Security/Privacy disposition and is Approved as of 2026-08-18.
- [x] Architecture redline — authored and approved
      `ADR-016-worker-workload-identity-and-invocation-delegation.md`. It compares
      signed JWT access tokens obtained through OAuth 2.0 client credentials,
      mutual TLS, managed workload identity, and static identifier/secret
      alternatives and defines principal binding, credential source and
      validation, refresh/expiry, revocation,
      multi-instance behavior, provisioning, readiness, lane independence, and
      adapter portability.
- [x] ADR-016 review gate — Product, Architecture, Operations, and
      Security/Privacy approved every recorded disposition on 2026-08-18.
      Deployment-profile numeric bounds remain mandatory evidence before
      Production/Staging enablement, not unresolved architecture decisions.
- [x] Build an executable requirement-to-surface matrix before behavior edits:
      identity authentication/binding, delegation issue/link, claim admission,
      model-call disclosure, fragment/seal commits, Decision/effect completion,
      work ACK/retry, timer fire, denial audit, and operations. See
      `# Requirement-to-surface matrix`.
- [x] Red — add host composition and identity-port tests proving Production and
      Staging refuse protected processors when identity configuration is absent,
      opaque, unsigned/invalidly signed, algorithm/key mismatched, not-yet-valid,
      expired, wrong issuer/subject/audience/client identity, revoked, mismatched
      to the configured actor, or unavailable. Prove Development/Testing
      synthetic profiles remain explicit and cannot be selected by Production
      settings.
- [x] Green — implement the minimum provider-neutral workload-authentication
      port, fresh authenticated-service-actor source, authoritative
      principal-to-actor binding, recoverable authorization gate, and bounded
      status projection. Preserve the existing monotonic shutdown gate as a
      separate condition. Keep secrets behind the approved mounted-file boundary
      and keep raw credentials/tokens out of product state and diagnostics.
- [x] Red — add migration, repository, and authorization tests for an additive
      service-principal binding plus `session.invocation.execute` delegation:
      issue/link success; missing/expired/revoked/not-yet-effective; wrong
      service/action/Organization/Activity/Participant/Attempt/Session; wrong
      work type/business key or envelope-to-delegation linkage; stale
      binding/delegation version; historical null; cross-scope substitution;
      duplicate delivery; and audit/outbox failure.
- [x] Green — add the next available additive migrations (`0025` at planning
      time) and the smallest IdentityAccess and Sessions persistence changes.
      Issue authority only from trusted Session/start context, persist the
      per-Session execution-delegation reference in its approved owning record,
      carry it in every durable Invocation work envelope, preserve single-action
      immutable history, and leave historical unverifiable work unclaimable.
      Do not add a quarantine/terminal state without an approved owning behavior.
- [x] Red/green — replace bare Invocation claiming with a transaction-bound
      coordinator that selects only identity/delegation/ownership-matched work,
      authorizes before lease mutation, preserves Organization/Activity fair
      claiming, and prevents invalid rows from head-of-line blocking unrelated
      authorized work. Distinguish non-selected ineligible rows from
      security-relevant kernel denials.
- [x] Red/green — authorize before loading protected Session/provider context or
      making a model call. Recheck authenticated identity freshness and current
      durable principal binding/delegation before every sensitive external
      disclosure. The new ADR must define how non-persisted cryptographic proof,
      its validity interval, authoritative database time, and current durable
      binding are combined without trusting a startup snapshot. A revoked
      identity or delegation after claim but before model admission must cause
      no provider call and no protected mutation.
- [x] Red/green — reauthorize as the last meaningful authorization operation in
      response-fragment, seal, unpublished-failure, Invocation completion/effect,
      work-complete, and retry/release transactions. Prove revocation/expiry/
      cutoff/lease-loss races roll back protected work before bounded denial
      audit and never publish or fabricate a Decision/no-action outcome.
- [x] Red/green — extend timer admission and commit-time reauthorization with the
      same current authenticated-principal binding while preserving the distinct
      `session.timer_lane.fire` delegation and existing denial-audit semantics.
      The timer delegation authorizes the fire; a current downstream
      `session.invocation.execute` delegation is additionally required and
      attached only when the fire creates Invocation work. Prove token/binding
      expiry, timer-delegation loss, or missing/invalid execution delegation
      after due selection admits no timer-triggered Invocation and leaves the
      due schedule safely pending.
- [x] Red/green — make identity refresh and revocation close the new-claim gate,
      expose honest ready/degraded/disabled states, and recover only after a
      newly authenticated principal maps to the expected current actor binding.
      Keep this recoverable gate independent from the one-way shutdown gate so
      refresh can never reopen a stopping Worker. Equivalent Worker instances
      must remain safe under concurrent claim, refresh, and shutdown races.
- [x] Verify the PostgreSQL end-to-end path with deterministic model adapters:
      participant- and timer-triggered Invocations, success/no-action/failure,
      fragments and seal, crash/reclaim, duplicate delivery, restart, binding
      refresh, revocation during execution, audit/outbox faults, pause, cutoff,
      terminalization, and cross-Organization isolation.
- [x] Run focused red/green tests, migration/upgrade checks from populated
      predecessor schemas, locked .NET regression, architecture boundaries,
      docs validation, whitespace checks, supply-chain/OCI checks when identity
      dependencies or deployment files change, and a secret/log/artifact scan.
- [x] Reconcile ADR-015 and the new workload-identity ADR, requirement
      implementation tables, `docs/README.md`, deployment/secret guidance,
      runtime traceability, host readiness copy, and actual enabled profiles.
      Do not mark live providers, human OIDC, hosted Session creation, UI, load,
      recovery, or production-pilot gates complete.
- [x] Obtain independent architecture, backend, and security/privacy review;
      resolve all blocking findings and repeat affected verification before
      completing the task.
# Requirement-to-surface matrix

| Obligation | Surface | Tests |
| --- | --- | --- |
| Workload authentication and principal binding (`REQ-AUTH-1`, `REQ-AUTH-2`, `REQ-AUTH-11`, `AC-AUTH-16`) | `ISecretSource`, signed-JWT validator, `IAuthenticatedWorkloadContextSource`, `service_principal_bindings`, operator provision/revoke/replace, `RecoverableAuthorityGate` | `WorkloadIdentityTests`; Production/Staging host refusal and recovery tests; binding provision tests |
| Bounded `session.invocation.execute` issue/link (`REQ-AUTH-15`, `REQ-AUTH-17`–`REQ-AUTH-20`, `AC-AUTH-17`) | IdentityAccess action + lifetime check; `session_runtimes.invocation_execute_delegation_id`; `session_durable_work.invocation_execute_delegation_id`; issue from trusted Session start | Migration 0025; Session start issue/link tests; historical-null unclaimable |
| Claim admission | `PostgresDurableInvocationWorkStore` eligibility join then kernel authorize before lease | Claim positive path; ineligible poison row; kernel deny race |
| Model-call disclosure | `IInvocationWorkSessionGateway` rechecks identity freshness, binding, and delegation before protected load/model | Revoke-before-call; expired proof; no provider call |
| Fragment/seal/completion/effect/work ACK (`REQ-SESS-55`–`REQ-SESS-60`, `REQ-SESS-78`–`REQ-SESS-85`) | Publication and completion coordinators reauthorize as last commit operation; rollback then bounded deny audit | Revoke-before-fragment/completion; audit/outbox fault |
| Timer fire (`REQ-SESS-75`) | `PostgresFireDueTimerCoordinator` current binding + `session.timer_lane.fire`; attach current execute delegation when creating work | Identity expiry; missing execute envelope leaves timer pending |
| Frozen binding/capability (`REQ-RSC-15`–`REQ-RSC-20`, `REQ-RSC-24`, `REQ-RSC-28`, `REQ-RSC-47`–`REQ-RSC-55`) | Existing trusted Session binding source on Worker paths | Cross-scope and missing binding tests on authorized Worker path |
| Operability | Independent shutdown vs recoverable gates; bounded readiness; no secret/id leakage | Refresh/shutdown race; readiness copy; secret scan |

Interim defaults used in code until Operations/Security approve a named deployment profile: RS256-only JWT; clock skew 30s; refresh margin 60s; JWKS cache 10m; token max lifetime 5m; Invocation-execute recovery allowance 15m; Invocation-execute max lifetime 24h. Session cutoff is currently a sequence, so wall-clock expiry uses `effective_at + max lifetime` and sequence cutoff remains a separate lifecycle gate.

# Current state

Implementation of ADR-016 is complete for the Worker reference path. Additive
migration `0025` carries principal bindings and Invocation-execute envelopes.
Production/Staging compose protected lanes only with the OAuth JWT profile and
a mounted client secret; both host flags remain default `false`. Synthetic
`configured_actor` remains Development/Testing only. Live issuers, approved
deployment-profile numbers, live model providers, human OIDC, and hosted
Session start are not claimed complete.

# Findings / deviations

- Planning review on 2026-08-18 clarified that timer polling and Invocation
  processing share authentication but retain separate action delegations and
  host capabilities; neither lane authorizes the other.
- Workload identity must be revalidated through its current durable actor
  binding at timer admission/commit as well as Invocation boundaries. The
  existing timer coordinator currently proves delegation for a configured actor
  but not authentication of an external principal.
- The current `WorkClaimGate` is a one-way shutdown boundary. Authentication
  expiry and recovery require a separate recoverable gate so refresh cannot
  reopen a stopping Worker.
- Historical Invocation work without delegation lineage remains unclaimable.
  The plan does not authorize a new quarantine or terminal workflow state.
- ADR-015's migration-preservation metadata was corrected from `0001`–`0023` to
  the implemented `0001`–`0024`; new work uses the next available additive
  migration rather than assuming `0025` will remain free.
- ADR-016 preparation clarified the cross-lane handoff: timer-fire authority
  remains exclusively `session.timer_lane.fire`, while a fire that creates
  Invocation work must also require and attach a current downstream
  `session.invocation.execute` delegation. That downstream envelope is a
  creation precondition, not permission to fire the timer.
- Product, Architecture, Operations, and Security/Privacy approval on
  2026-08-18 adopted all ADR-016 dispositions and approved ADR-015. Numeric
  workload-profile bounds remain required downstream evidence rather than open
  architecture questions.
- Final consistency review clarified that ADR-015's configured actor-id behavior
  describes only the synthetic profile. ADR-016 Production/Staging composition
  resolves the actor from the current authenticated principal binding and uses
  configuration only as an expected-value check.
- Final security review made the OAuth 2.0 client-credentials reference profile
  explicitly require signed JWT access tokens and reject opaque-token fallback;
  otherwise the approved local signature, algorithm, issuer, audience,
  `nbf`/`exp`, and verification-key contract would be ambiguous.
- Implementation review (2026-08-18): `AuthenticatedWorkloadGuard` is fail-open
  when no identity source is injected so existing PostgreSQL integration
  constructors keep working; the Worker composition always injects a source.
  Operator provision/revoke/replace is the IdentityAccess coordinator, not a
  separate CLI host. Live token-endpoint and JWKS HTTPS were stubbed in unit
  tests; Production enablement still needs a real issuer and approved profile
  numbers. Denied `MarkCompleted` now throws after rollback so the processor
  cannot treat a rolled-back completion as success.
- Consistency review (2026-08-18): `RefreshDegraded` now still accepts protected
  work while readiness reports degraded; OAuth identity keeps still-valid proof
  when refresh fails; JWKS cache no longer disposes in-use verification keys on
  refresh. Cached JWT proof is not durable authorization: claim, disclosure, and
  commit re-read `(binding_id, binding_version, service_actor_id, revoked_at,
  effective_at)` in the same PostgreSQL transaction.
- External review of `3596480` (2026-08-18): Invocation now reauthorizes the
  current durable principal binding inside claim/load/publish/complete/release/
  lease transactions; `TryAuthorizeModelDisclosureAsync` runs immediately
  before `ExecuteAsync`; Production/Staging require absolute `https` token and
  JWKS URIs; JWT max lifetime uses `nbf`/`exp` plus `iat` ordering; the claim
  HOL anti-join matches the candidate ownership fields. A remaining
  provider-call race is only the unavoidable window after that last admission.
  GitHub check results were not attached to `3596480`; this pass recorded local
  focused evidence rather than a second locked full-solution run.
- External review of `ef63fbd` (2026-08-18): protected Session load now happens
  after in-transaction admission; content stream start uses the same
  model-disclosure gate as `ExecuteAsync`; claim commit locking-reauthorizes the
  work envelope's execute delegation after the lease update; readiness consults
  the current identity source so a cached JWT plus revoked binding is not
  reported healthy.

- Planning review on 2026-08-18 clarified that timer polling and Invocation
  processing share authentication but retain separate action delegations and
  host capabilities; neither lane authorizes the other.
- Workload identity must be revalidated through its current durable actor
  binding at timer admission/commit as well as Invocation boundaries. The
  existing timer coordinator currently proves delegation for a configured actor
  but not authentication of an external principal.
- The current `WorkClaimGate` is a one-way shutdown boundary. Authentication
  expiry and recovery require a separate recoverable gate so refresh cannot
  reopen a stopping Worker.
- Historical Invocation work without delegation lineage remains unclaimable.
  The plan does not authorize a new quarantine or terminal workflow state.
- ADR-015's migration-preservation metadata was corrected from `0001`–`0023` to
  the implemented `0001`–`0024`; new work uses the next available additive
  migration rather than assuming `0025` will remain free.
- ADR-016 preparation clarified the cross-lane handoff: timer-fire authority
  remains exclusively `session.timer_lane.fire`, while a fire that creates
  Invocation work must also require and attach a current downstream
  `session.invocation.execute` delegation. That downstream envelope is a
  creation precondition, not permission to fire the timer.
- Product, Architecture, Operations, and Security/Privacy approval on
  2026-08-18 adopted all ADR-016 dispositions and approved ADR-015. Numeric
  workload-profile bounds remain required downstream evidence rather than open
  architecture questions.
- Final consistency review clarified that ADR-015's configured actor-id behavior
  describes only the synthetic profile. ADR-016 Production/Staging composition
  resolves the actor from the current authenticated principal binding and uses
  configuration only as an expected-value check.
- Final security review made the OAuth 2.0 client-credentials reference profile
  explicitly require signed JWT access tokens and reject opaque-token fallback;
  otherwise the approved local signature, algorithm, issuer, audience,
  `nbf`/`exp`, and verification-key contract would be ambiguous.

# Decisions

## Confirmed

- Authentication and authorization remain separate. Authentication establishes
  a fresh service principal; ADR-002 delegation authorizes an exact action and
  Session scope.
- `Sessions:WorkerServiceActorId`, a database login, container identity, or host
  capability flag cannot establish authentication.
- Workload credentials and raw tokens do not enter product tables, durable work,
  audit, logs, metrics, traces, generated fixtures, or committed artifacts.
- Every protected Invocation work item carries its durable delegation reference;
  work payload ownership cannot create or widen authority.
- Existing migrations are immutable; use the next available additive migration
  (`0025` at planning time) after rechecking current repository state.
- Live provider qualification is a separate successor and cannot be implied by
  a production-authenticated Worker.

## Approved architecture dispositions

ADR-016 approved the decisions originally tracked as `PROP-WID-1` through
`PROP-WID-6`. The identifiers remain below as planning provenance; ADR-016 is
the authoritative source.

- `PROP-WID-1` — Use a provider-neutral short-lived workload-token contract. For
  the self-hostable reference profile, evaluate a signed JWT access token
  obtained with OAuth 2.0 client credentials from the OIDC-capable reference
  issuer against a pinned issuer/audience and explicit validation policy, bind
  the cryptographically validated stable issuer+subject/client identity to a
  versioned Flex Agent service actor, and source the client credential through
  the ADR-008 mounted-file `SecretSource`. Rationale: it preserves a portable
  token boundary and aligns with the selected self-hostable identity/secret
  families. This does not extend the approved human OIDC application-session
  decision to workloads.
- `PROP-WID-2` — Use one bounded per-Session
  `session.invocation.execute` delegation for the Worker and link its identifier
  to every Invocation durable-work record. Rationale: exact Session/action scope
  remains stable across retry and timer/participant triggers without creating
  authority from a work payload. Per-Invocation delegation was considered and
  not selected by ADR-016.
- `PROP-WID-3` — Stop new claims immediately when the locally authenticated
  workload token is expired/unavailable or its durable principal binding is
  revoked/stale; revalidate both binding and the lane-specific delegation at
  sensitive commit through a recoverable authorization gate separate from the
  monotonic shutdown gate. Rationale: a startup-only identity check would not
  satisfy revocation or long-running Worker behavior, while reopening the
  shutdown gate would violate graceful termination.
- `PROP-WID-4` — Do not invent an Invocation-delegation lifetime. Resolve it from
  the approved Session cutoff plus a positive bounded recovery allowance, cap it
  by an approved maximum, and fail closed when it expires. Rationale: silent
  renewal would widen authority, while copying the timer lane's seven-day cap
  without scope evidence would turn an implementation precedent into policy.
- `PROP-WID-5` — Historical Invocation work without authenticated principal and
  delegation lineage remains unclaimable and is reported through bounded
  operator telemetry/reconciliation evidence; it is never identifier-backfilled
  or silently moved to a newly invented durable state. Rationale: availability
  cannot justify fabricated authority or an unapproved workflow transition.
- `PROP-WID-6` — Keep timer and Invocation capabilities independently
  authorized. Both require fresh workload identity, but timer polling uses only
  `session.timer_lane.fire` delegation to authorize timer fire and Invocation
  processing uses only `session.invocation.execute` delegation to authorize
  execution. A timer fire that creates Invocation work must nevertheless verify
  and attach a current downstream execution delegation; that delegation does
  not authorize the timer action. Rationale: authentication is shared; action
  authorization and operational enablement are not, while durable work must not
  be created without a valid authority envelope.

# Resolved decisions and downstream profile evidence

- **Reference profile:** signed JWT access tokens obtained with OAuth 2.0 client
  credentials are the approved self-hostable profile; opaque-token introspection
  is not part of that profile. mTLS, managed identity, and SPIFFE remain possible
  future adapters behind the same contract.
- **Issuer metadata and verification keys:** only previously validated keys may
  be used within the approved positive profile cache lifetime. Credential
  validity and current durable binding remain independently enforced. Unknown
  keys, expired cache, or ambiguity stop new claims.
- **Delegation granularity:** one bounded delegation per Session, with exact
  Invocation and work checks inside that scope.
- **Delegation lifetime and renewal:** expiry derives from Session cutoff plus
  an approved positive recovery allowance, is capped by the approved deployment
  maximum, and is not renewed in this slice.
- **Provisioning:** a non-interactive, idempotent, separately authorized
  operator command provisions, revokes, or replaces only the durable principal
  binding and records required audit. Credential rotation remains a mounted-
  secret procedure when the stable principal is unchanged.
- **In-flight identity loss:** request provider cancellation when supported and
  deny or discard every later protected commit. Cancellation is not the
  correctness boundary.
- **Claimed work after authority loss:** do not bypass authorization for
  cleanup. Let the lease expire and keep the row ineligible unless a later
  independently authorized reconciliation contract is approved.
- **Deployment-profile evidence still required:** Operations and
  Security/Privacy must record and approve the concrete token maximum lifetime,
  clock skew, refresh margin, issuer metadata/key cache lifetime, retry and
  timeout bounds, Invocation-delegation recovery allowance and maximum,
  secret-delivery/rotation procedure, and binding provisioning/revocation
  procedure before Production or Staging enablement. These are downstream
  profile values, not open ADR decisions.

# Threats and required controls

| Threat | Required control | Planned verification |
| --- | --- | --- |
| Configured actor GUID or container location impersonates the Worker | Cryptographically authenticated principal plus current authoritative principal-to-actor binding | Production host refusal; wrong subject/issuer/audience/actor tests |
| Stolen or expired workload credential continues claiming | Short-lived credential, bounded refresh, expiry gate, binding revocation, no raw-token persistence | Expiry/refresh/revoke races; secret/log/artifact scan |
| Algorithm/key/issuer confusion or replay authenticates the wrong workload | Explicit validation policy, pinned issuer/audience, stable subject/client binding, bounded key refresh/cache | Invalid signature/algorithm/key, wrong issuer/audience/client, `nbf`/`exp`, replay-window tests |
| Work payload redirects execution across scope | Delegation reference in trusted envelope plus kernel ownership/action match; work type/business key must resolve to the admitted Invocation | Cross-Organization/Activity/Participant/Attempt/Session substitution and envelope/business-key mismatch matrix |
| Revocation wins after claim or model call | Fresh identity/delegation check before disclosure and commit-time reauthorization | Revoke-before-call and revoke-before-fragment/completion races |
| Invalid work occupies the fair-claim head | Eligibility join plus scoped coordinator, without treating poller skip as an authorization decision | Poison/ineligible row does not block unrelated partition |
| Partial deny leaves Decision, fragment, work ACK, audit, or outbox | Single authoritative transaction, rollback first, separate bounded deny audit | Fault injection at every protected write and deny-audit failure |
| Credential or token leaks through diagnostics | Mounted-file secret boundary and allowlisted bounded telemetry | Automated secret/log/error/readiness assertions and repository scan |
| Startup authentication becomes permanent authority | Refreshable authenticated-service-actor source and commit-time durable binding checks on Invocation and timer lanes | Expiry/revocation during long-running process and multi-instance tests |
| Authentication recovery reopens a stopping Worker | Separate recoverable authorization gate and monotonic shutdown gate | Refresh/shutdown race and readiness tests |

# Traceability and verification plan

| Obligation | Implementation surface | Required evidence | Status |
| --- | --- | --- | --- |
| `REQ-AUTH-1`, `REQ-AUTH-2`, `REQ-AUTH-11`, `AC-AUTH-16` | Workload authenticator, principal binding, trusted service actor | Missing/invalid/expired/revoked/cross-binding authentication matrix | covered (live issuer unverified) |
| `REQ-AUTH-15`, `REQ-AUTH-17`–`REQ-AUTH-20`, `AC-AUTH-17` | Delegation issue/link, claim coordinator, per-commit reauthorization | Positive path plus commit races, unavailable dependencies, no partial mutation | covered |
| `REQ-AUTH-21`, `REQ-AUTH-22`, `REQ-AUTH-26`–`REQ-AUTH-31` | Safe errors, success/deny audit, outbox, append-only transitions | Non-disclosure, audit durability/fault injection, authorization-reference tests | covered (existing audit/outbox plus binding transitions) |
| `REQ-AUTH-25`, `REQ-SESS-61`–`REQ-SESS-70` | Scoped work envelope, immutable binding, model-call admission | Cross-scope/tamper tests; no provider call before permit | covered |
| `REQ-RSC-15`–`REQ-RSC-20`, `REQ-RSC-24`, `REQ-RSC-28`, `REQ-RSC-47`–`REQ-RSC-55`, `AC-RSC-26`–`AC-RSC-28` | PostgreSQL trusted Session binding and frozen P0 runtime capability policy | Missing/tampered/cross-scope binding and lower-scope-widening tests on Worker paths | covered (predecessor plus Worker path) |
| `REQ-SESS-55`–`REQ-SESS-60`, `REQ-SESS-78`–`REQ-SESS-85` | Fragment/seal/completion coordinators | Revocation, cutoff, duplicate, crash/reclaim, late-result tests | covered |
| `REQ-SESS-75` | Existing timer delegation plus current authenticated-principal binding | Identity and timer delegation revalidated at timer admission and commit; timer-created work receives a current downstream execution-delegation reference without merging lane authority | covered |
| Operability and privacy | Monotonic `WorkClaimGate`, separate recoverable authorization gate, readiness, metrics/logs, SecretSource | Refresh/recovery/shutdown/readiness tests; no secret/protected identifier leakage | covered |

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Baseline repository state | complete | Clean `main...origin/main` at `b5f89ef` before planning edits |
| Governing product/requirements/architecture review | complete | Sources and current Worker/IdentityAccess/Sessions seams listed above reviewed 2026-08-18 |
| Cross-concern plan consistency review | complete | Architecture, backend, security/privacy, product-scope, and documentation review on 2026-08-18; findings under `# Findings / deviations` applied; `python3 scripts/check_docs.py` and `git diff --check` passed |
| ADR-015 named approval | complete | Approved 2026-08-18 after Product, Architecture, and Security/Privacy disposition |
| Workload identity / Invocation delegation ADR | complete | ADR-016 Approved 2026-08-18 after Product, Architecture, Operations, and Security/Privacy disposition; deployment-profile and implementation evidence remain required before production enablement |
| ADR preparation validation | complete | ADR catalog, architecture hub, documentation maturity summary, ADR-015 cross-link, and tracked task reconciled; `python3 scripts/check_docs.py` and `git diff --check` passed 2026-08-18 |
| ADR promotion validation | complete | ADR-015/ADR-016 status, approved dispositions, ADR catalog, architecture/documentation hubs, requirement traceability, and task state reconciled; `python3 scripts/check_docs.py`, `git diff --check`, and trailing-whitespace scan passed 2026-08-18 |
| Final pre-commit documentation review | complete | Configured-actor and opaque-token ambiguities corrected; `python3 scripts/check_docs.py`, `git diff --check`, stale-status scan, 16-ADR count check, and trailing-whitespace scan passed 2026-08-18. `markdownlint-cli2` is configured in CI but unavailable in this workspace (`pnpm exec markdownlint-cli2 --version` reports command not found). |
| Focused identity/authorization tests | complete | Runtime `WorkloadIdentityTests` + `WorkerRuntimeTests` 47 passed including future/`iat`, plaintext OAuth URI refusal, ready-check degrade when identity is missing, and refresh not clobbering `IdentityDenied` |
| PostgreSQL migration/upgrade/concurrency/fault tests | complete | Migration `0025`; review-fix pass: `WorkerInvocationExecuteDelegationTests`, `DurableInvocationWorkClaimTests`, `DurableInvocationWorkCrashRecoveryTests`, and `SessionTimerLaneDelegationTests` 57 passed (cached-binding revoke, model-disclosure revoke, poison HOL, claim lease-update then delegation revoke rollback) |
| Locked .NET regression | complete | `bash build/scripts/verify-dotnet.sh` — 977 passed, publish succeeded at `3596480`; later review-fix passes ran focused suites only (not a second locked full-solution run) |
| Architecture/docs/whitespace | complete | `python3 scripts/check_docs.py`; `git diff --check` |
| Supply-chain/OCI/secret evidence | complete | No package or deployment-input changes in this slice; Worker errors mention secret-file presence without values; token client unit test does not persist secrets |
| Independent cross-concern review | complete | External reviews of `3596480` and `ef63fbd` addressed 2026-08-18; residual gaps recorded below |
| Review-fix processor admission | complete | `DurableInvocationWorkProcessorTests` 29 passed including denied `ExecuteAsync` and denied stream-start with zero stream calls |

# Blockers

## Governance

None. ADR-015 and ADR-016 are Approved.

## Implementation

None for this implementation slice. Production/Staging enablement remains
blocked by approved deployment-profile numeric bounds, a live issuer, and
live-provider qualification listed in ADR-016.

# Completion

- [x] Planned work is reconciled with actual changes and approved decisions
- [x] ADR-015 disposition and the workload-identity/Invocation-delegation ADR
      are approved and reflected accurately
- [x] Production Worker identity is cryptographically authenticated, mapped to
      a current service actor, refreshable, revocable, and non-disclosing
- [x] Every Invocation work item carries bounded delegation and complete trusted
      scope; historical unverifiable work remains fail-closed
- [x] Claim, protected-read/model admission, fragment/seal, completion/effect,
      retry/release, and work-terminalization boundaries authorize and
      reauthorize as specified
- [x] Timer admission/commit verifies fresh workload binding and the distinct
      timer delegation; timer-created Invocation work also receives its current
      downstream execution-delegation reference; every Invocation boundary
      verifies fresh workload binding and the distinct Invocation delegation
- [x] Timer polling and Invocation processing remain default-off and each
      composes in Production/Staging only when its own distinct gates are
      satisfied; neither capability authorizes the other
- [x] Positive, negative, cross-scope, concurrency, revocation, expiry,
      restart, audit/outbox-fault, and secret-leak tests pass
- [x] Applicable focused red/green evidence is recorded
- [x] Migration/upgrade and locked integration/regression checks pass
- [x] Architecture, documentation, whitespace, supply-chain/OCI, and secret
      checks pass where applicable
- [x] Governing specifications and implementation status are reconciled without
      overstating live-provider, human OIDC, UI, load, recovery, or pilot gates
- [x] Independent architecture, backend, and security/privacy findings are
      resolved and reverified
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
