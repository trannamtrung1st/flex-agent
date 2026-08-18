# ADR-016: Worker workload identity and bounded Invocation delegation

## Status

Approved. Product, Architecture, Operations, and Security/Privacy approval of
the complete decision package was recorded on 2026-08-18.

The Worker reference-path implementation of this ADR was independently reviewed
and approved on 2026-08-18. Approval covers the implemented authorization
slice; it does not authorize a Production or Staging Worker lane until
deployment-profile, live-issuer, and live-provider verification gates pass.
[ADR-015](ADR-015-session-timer-lane-service-delegation.md) was approved as
part of the same Worker authorization package.

## Decision metadata

| Field | Value |
| --- | --- |
| **Owner** | Architecture Lead |
| **Required approvers** | Product Lead, Architecture Lead, Operations owner, Security/Privacy reviewer |
| **Consulted perspectives** | Product analysis, architecture, backend, operations, security/privacy |
| **Proposed date** | 2026-08-18 |
| **Approved date** | 2026-08-18 |
| **Approval reference** | Product, Architecture, Operations, and Security/Privacy approval of all recorded dispositions on 2026-08-18 |
| **Governs** | Worker workload authentication, external-principal-to-service-actor binding, identity freshness, lane composition, and bounded `session.invocation.execute` delegation |
| **Upstream sources** | [ADR-002](ADR-002-authorization-enforcement-and-delegation.md), [ADR-003](ADR-003-authorization-audit-persistence.md), [auth-resource-isolation](../../requirements/features/auth-resource-isolation.md), [resolved Session configuration](../../requirements/features/resolved-session-configuration.md), and [text Session lifecycle](../../requirements/features/session-text-lifecycle.md) |
| **Extends** | ADR-002 delegated service execution; ADR-008 provider-neutral identity and mounted-file `SecretSource` boundaries |
| **Integrates** | Approved ADR-015 timer-lane delegation without merging timer-fire and Invocation-execution authority |
| **Preserves** | Frozen Session configuration, Session isolation, durable-before-display publication, one timer lane, audit coupling, additive migration history through `0024`, and default-off protected Worker capabilities |

## Context

ADR-002 requires a background worker to authenticate as its own service
identity, load a trusted bounded delegation and resource scope, revalidate
current authorization before protected work, and reauthorize before sensitive
disclosure or commit. The current Worker accepts an explicitly configured
`Sessions:WorkerServiceActorId`, but that identifier and its process, container,
network, or database location are not authentication.

The timer path already has an implemented per-Session
`session.timer_lane.fire` delegation realization governed by approved ADR-015.
The Invocation path has durable scoped work and lease recovery, but its work
envelope has no delegation reference and its claim, protected-read/model,
publication, Decision/effect, and work-state boundaries do not yet enforce one
bounded `session.invocation.execute` delegation. Both protected lanes therefore
remain refused in Production and Staging.

This ADR selects a portable workload-authentication contract and a self-hostable
reference profile. It also defines how authenticated Worker identity combines
with resource- and action-bounded Invocation delegation. It does not select or
qualify a live model provider, implement human OIDC application sessions, or
approve any new participant-visible behavior.

## Decision drivers

- Satisfy `REQ-AUTH-1`, `REQ-AUTH-2`, `REQ-AUTH-11`, and `REQ-AUTH-15`
  without treating configuration, network location, or database access as
  identity or permission.
- Keep authentication provider-neutral and self-hostable while allowing later
  managed-identity or mTLS adapters behind the same application contract.
- Bound stolen-credential and stale-authorization exposure through short-lived
  proof, current durable binding, explicit delegation, and commit-time checks.
- Preserve exact Organization, Activity, Participant, Attempt, Session,
  Invocation, action, purpose, lease, and idempotency scope across retries.
- Keep timer-fire authority and Invocation-execution authority independently
  enabled, delegated, revoked, and observable.
- Fail closed without leaking credentials, tokens, protected identifiers, or
  participant content through product state or diagnostics.
- Support equivalent concurrent Worker instances, graceful shutdown, and
  recoverable credential refresh without reopening a process that is stopping.
- Preserve applied migration history and never fabricate authority for
  historical work.

## Options considered

| Option | Benefits | Costs and risks | Disposition |
| --- | --- | --- | --- |
| Configured actor id, process/container identity, network location, or database credential | No new identity integration | Not cryptographic application authentication; broad infrastructure access becomes product permission; weak revocation and audit meaning | Rejected |
| Static shared application secret presented directly to the Worker | Simple and portable | Long-lived replay value, difficult instance attribution and rotation, and authentication still needs a custom protocol | Rejected as the reference profile |
| Mutual TLS workload certificate | Strong channel-bound identity and mature PKI semantics | Requires certificate issuance, trust distribution, rotation/reload, and a receiving network boundary that the in-process Worker does not otherwise need | Permitted as a future adapter after equivalent contract and operational evidence |
| Signed JWT access token obtained with OAuth 2.0 client credentials from an OIDC-capable issuer | Short-lived issuer-attested proof, standard validation fields, portable self-hosted implementation, and future managed-secret/identity adapters | Token endpoint and verification-key availability become dependencies; bearer replay and client-credential protection require controls | Selected reference profile |
| SPIFFE/SPIRE or cloud-specific managed identity | Strong platform workload identity and rotation on compatible platforms | Adds an orchestrator or cloud dependency and cannot be the only self-hostable MVP path | Permitted later behind the same workload-identity contract |

## Decision

### 1. Separate credential acquisition, authentication, binding, and authorization

The Worker uses four distinct boundaries:

1. A credential source supplies only the material needed to obtain workload
   proof. Secrets remain behind the ADR-008 mounted-file `SecretSource` for the
   reference profile.
2. A provider adapter obtains and cryptographically validates short-lived
   workload proof under a pinned validation profile.
3. IdentityAccess resolves the validated stable external principal to exactly
   one current, active, versioned Flex Agent service actor binding.
4. ADR-002 authorizes that actor for an exact action and protected resource
   through current authoritative state and a durable delegation.

Success at an earlier boundary never implies success at a later boundary. A
configured actor id remains an expected binding check, not proof of identity.

### 2. Use a provider-neutral authenticated-workload contract

The application contract supplies a non-secret authenticated workload context
containing at minimum:

- authentication profile identifier and method;
- normalized issuer and stable external subject, plus client identity when the
  selected profile exposes one separately;
- validated audience;
- proof issue, not-before, expiry, and local validation times when applicable;
- the resolved Flex Agent service actor id and durable binding id/version; and
- a bounded correlation reference suitable for diagnostics.

The context does not contain reusable credentials or a raw access token. It is
valid only while both the cryptographic proof and its current durable binding
remain valid. Provider-specific credential acquisition, token/certificate
parsing, key discovery, and rotation stay outside the authorization kernel.

### 3. Use OAuth 2.0 client credentials with signed JWT access tokens as the reference self-hostable profile

The reference profile obtains a short-lived signed JWT access token using OAuth
2.0 client credentials from the deployment's approved OIDC-capable issuer. It
must:

- use a confidential client credential supplied by `SecretSource`, never from
  product tables, command payloads, work envelopes, logs, or images;
- require a signed JWT access token. Opaque tokens and token introspection are
  outside this reference profile and cannot silently replace local validation;
- pin issuer, audience, permitted signature algorithms, and the stable
  subject/client identity expected for the deployment;
- validate signature, key trust, issuer, audience, not-before, expiry, and the
  profile's positive bounded clock skew;
- reject unsigned tokens, algorithm substitution, unknown or ambiguous keys,
  wrong issuer/audience/subject/client identity, expired or not-yet-valid proof,
  inconsistent or future `iat`, overlong `nbf`/`exp` windows, and claims that
  attempt to supply Organization, role, scope, actor id, or product permission;
- obtain discovery and verification keys only through the approved protected
  issuer endpoint and accept cached keys only within the profile's approved
  positive cache lifetime; and
- refresh before expiry with bounded backoff and jitter, while closing the
  recoverable new-work gate whenever no currently valid proof exists.

Hosts may cache cryptographic proof. They must not treat a still-unexpired
cached JWT as current durable authorization. Sensitive admission, disclosure,
and commit re-read the current principal binding in the same authorization
transaction.

Production and Staging OAuth token and JWKS endpoints must be absolute `https`
URIs. Development and Testing may use `http` for local issuer doubles.

The exact token maximum lifetime, clock skew, refresh margin, issuer-metadata
cache lifetime, retry bounds, and timeouts are deployment-profile parameters.
They must be positive, bounded, documented, tested, and approved by Operations
and Security/Privacy before a Production or Staging profile can be enabled.
They are not inferred from an issuer default.

Bearer replay remains possible within the proof lifetime. TLS, least-privilege
secret access, short proof lifetime, no token persistence, issuer-side client
revocation, and current durable binding revocation are required controls. This
reference profile does not extend the human application-session decision to
workloads or make provider roles application permissions.

### 4. Bind the external principal to one service actor through durable state

IdentityAccess owns an authoritative, versioned workload-principal binding. Its
logical record contains a stable binding id, authentication profile/method,
issuer, external subject and applicable client identity, expected audience,
service actor id, service purpose/type, effective and revoked state, monotonic
version, and append-only issuance/revocation/replacement provenance. It stores
no credential or token.

At most one active binding may match the same normalized principal and profile
for the same deployment audience. The bound actor must exist. The binding's
constrained service purpose/type establishes how this workload may authenticate;
it must not present the workload principal as a human identity. Replacement
creates an inspectable successor; it does not rewrite historical identity.
Provision, revoke, or replace is an explicitly authorized, idempotent operator
command with mutation-coupled durable audit. Credential rotation does not
replace the binding when the stable principal is unchanged.

Every protected admission, disclosure, and commit validates the context's
binding id/version against current durable state and verifies that it still
maps the authenticated principal to the expected actor. Missing, unavailable,
ambiguous, stale, not-yet-effective, revoked, or mismatched state denies.

### 5. Use one bounded per-Session Invocation-execution delegation

IdentityAccess uses the existing durable service-delegation model with the
single action `session.invocation.execute`. One delegation is issued for the
Worker service actor and one exact Organization/Activity/Participant/Attempt/
Session ownership chain. It carries system purpose, initiating authority,
effective time, required expiry, revocation, and monotonic version.

The delegation lifetime is derived from the authoritative Session execution
cutoff plus an explicitly approved positive bounded recovery allowance and is
also capped by the approved deployment maximum. It never silently follows a
later Session extension, exceeds either bound, or renews itself. Renewal is out
of scope until an authorized renewal command and owning behavior are approved.

The trusted Session execution authority record carries this delegation
reference. Every new `execute_invocation` durable work item links the same
reference atomically with the Invocation and complete trusted ownership. The
work payload cannot supply or replace it. Retries retain the original
delegation, work identity, scope, and idempotency context.

Issuance and revocation use the existing `service_delegation.issue` and
`service_delegation.revoke` authorization boundary: the initiating actor needs
a current Organization/action grant, the mutation retains initiator, reason,
source, correlation, prior/new state, and exact authorization reference, and
commit-time reauthorization plus required durable audit gate the transaction.

Per-Invocation delegation was considered but is not selected: a per-Session
single-action delegation provides the same authoritative Session boundary for
all permitted triggers and retries while avoiding authority-record churn. The
Invocation and work identities still narrow each execution inside that Session.

### 6. Authorize claim, disclosure, publication, effect, and work state

The Worker must authenticate and validate its current actor binding before it
can claim protected work. The Invocation claim transaction selects only work
whose trusted envelope, ownership, action, and current delegation are eligible
for that actor, then authorizes before lease mutation. Ineligible historical or
revoked work cannot block unrelated eligible Organization/Session partitions.
A poller skipping an ineligible row is not fabricated as a kernel decision.

The current workload binding, Invocation delegation, complete ownership,
Invocation/work identity, frozen Session policy, lifecycle/cutoff, expected
version, lease, and idempotency boundary are then checked:

- before loading a protected Session snapshot or disclosing provider context to
  a model port, including a dedicated model-disclosure admission immediately
  before the provider `ExecuteAsync` or stream start;
- before each durable response-fragment publication;
- before response seal and message completion;
- before recording an Invocation outcome or Agent Decision and effecting each
  independently permitted output or requested action;
- before recording unpublished failure; and
- before completion, retry/release, or other protected work-state mutation.

The Invocation claim transaction reauthorizes the work envelope's execute
delegation with a commit lock as its last authorization step so a concurrent
revocation cannot persist a lease. Commit-time authorization is the last
meaningful authorization operation in each sensitive transaction. If
authorization loses a race, the transaction, including staged success audit and
outbox writes, rolls back before any bounded denial audit is attempted
separately. Authorization or required-audit unavailability cannot leave a
protected partial mutation committable.

### 7. Preserve independent timer and Invocation capabilities

Both protected lanes require the same fresh authenticated workload binding, but
each keeps its own default-off host capability and exact action delegation:

| Lane | Authority required for its action |
| --- | --- |
| Timer polling and timer fire | Current workload binding plus `session.timer_lane.fire` delegation under approved ADR-015 |
| Invocation claim and execution | Current workload binding plus `session.invocation.execute` delegation under this ADR |

Neither delegation authorizes the other action. A deployment may enable either
or both lanes only when that lane's complete dependencies and verification
evidence are present.

A timer fire that atomically creates Invocation work has one additional
downstream-envelope precondition: the target Session must already carry a
current `session.invocation.execute` delegation, and the new work must link it.
That execution delegation does not authorize the timer fire; the timer
delegation does. If the downstream delegation is absent or invalid, the fire
transaction creates no Invocation or work and leaves the pending timer safely
retryable under its owning lifecycle rules.

### 8. Treat identity loss as recoverable authorization loss, not shutdown

The Worker has two independent gates:

- a monotonic shutdown gate that never reopens after termination begins; and
- a recoverable authenticated-authority gate that is open only while current
  proof maps to the expected active actor binding.

Expiry, issuer/key ambiguity, refresh failure, or binding revocation closes new
claims immediately and reports the protected lane as not ready while liveness
remains honest. An already-started provider call is cancellation-requested when
supported, but cancellation is not the security boundary: every later
protected commit still denies. Recovery may reopen only the authenticated-
authority gate after new proof and current binding validate, and never the
shutdown gate.

Equivalent Worker instances authenticate independently, but may map to the same
service actor when the approved deployment profile intentionally defines one
workload principal. Durable claim leases, binding/delegation versions, and
commit-time checks remain authoritative across instances.

### 9. Keep historical unverifiable work fail-closed

Applied migrations through `0024` remain immutable. Implementation uses the
next available additive migration after rechecking repository state. Existing
Invocation work or Sessions without provable delegation lineage remain
unclaimable. Migration, identifiers, or operator configuration must not
backfill or infer authority, and this ADR does not invent a quarantine,
completion, cancellation, or deletion state.

Operators receive bounded counts and reason categories without protected stable
identifiers. Any later cleanup or reconciliation command requires its own
approved authorization and audit contract. A claimed row whose authority is
lost is not mutated through a cleanup bypass; its lease may expire while the
row remains ineligible.

### 10. Make readiness and diagnostics bounded and non-disclosing

Readiness distinguishes disabled, authenticating, ready, refresh-degraded,
identity-denied, dependency-unavailable, and stopping states without revealing
tokens, secrets, issuer payloads, stable Organization/Activity/Participant/
Session/Invocation/delegation identifiers, or protected content. The Worker
ready check reports the last observation on the recoverable authorization
gate; it does not mint tokens, query JWKS, or open a binding transaction.
A still-valid cryptographic proof may be re-checked against the current
durable principal binding without requesting a new access token. Transactional
authorization remains the authoritative binding check at claim, disclosure,
and commit. Metrics and logs use allowlisted reason categories, lane, profile,
process-instance correlation, and bounded timing data. Raw authentication
material is never stored in product state, audit, telemetry, test artifacts,
or generated fixtures.

## Authorization and data ownership

| Concern | Authoritative owner | Required invariant |
| --- | --- | --- |
| Credential material | Deployment secret facility through `SecretSource` | Never product data; least privilege and rotation/reload evidence required |
| Cryptographic workload proof | Workload-authentication adapter | Validated against one pinned profile; raw proof is memory-bounded and non-persistent |
| External-principal binding | IdentityAccess | Versioned, current, one actor, auditable, and independently revocable |
| Service delegation | IdentityAccess | One actor, action, purpose, complete resource scope, bounded time, and immutable history |
| Session/Invocation/work ownership | Sessions authoritative store | Derived from trusted Session relationships; payload cannot widen or redirect it |
| Frozen capability and lifecycle state | Sessions | Lower scope may narrow but never widen; cutoff and current state gate every effect |
| Claim lease and idempotency | Sessions durable work | Same scoped work identity across retry; no delegation substitution |
| Audit | AuthorizationAudit under ADR-003 | Mutation-coupled where required, append-only, non-secret, and exact authorization reference |

## Failure and recovery behavior

| Failure | Required outcome |
| --- | --- |
| Token endpoint, discovery, or key refresh unavailable | Use only still-valid proof and previously validated keys inside approved profile bounds; otherwise close new-work gate and deny |
| Unknown key, invalid signature/algorithm, issuer/audience/principal mismatch, `nbf`, or expiry failure | Deny authentication; no actor context, claim, protected read, model disclosure, or mutation |
| Binding unavailable, stale, ambiguous, or revoked | Deny both protected lanes for that context; no configured-actor fallback |
| Lane delegation missing, expired, revoked, wrong action, or cross-scope | Deny that lane/action; the other lane gains no authority |
| Identity/delegation lost after claim | Request provider cancellation when possible; deny and roll back every later protected commit |
| Commit-time reauthorization or required audit unavailable | Roll back; never commit protected partial state |
| Worker restarts or multiple instances race | Reauthenticate; use authoritative leases, versions, delegation, and idempotency to reconcile |
| Historical work lacks delegation lineage | Leave unclaimable and report bounded operator evidence; do not fabricate authority |

## Security and privacy controls

- Threat model bearer replay, client-secret theft, issuer/key/algorithm
  confusion, stale binding, cross-scope envelope substitution, startup-only
  authorization, multi-instance races, and diagnostic leakage.
- Minimize provider disclosure and perform the last current identity/delegation
  check before the model boundary. A model provider never receives workload
  credentials or authorization records.
- Use TLS and the approved issuer trust configuration for token acquisition and
  metadata/key retrieval. No automatic fallback issuer, audience, algorithm,
  key, credential, actor, delegation, or model profile is allowed.
- Keep issuer administration, client lifecycle, secret-file permissions,
  binding provisioning/revocation, and application authorization as separate
  privileged boundaries with least privilege and auditable procedures.
- Add positive and negative tests for wrong Organization, Activity,
  Participant, Attempt, Session, Invocation, actor, principal, action,
  delegation, work identity, lease, version, and lifecycle state.
- Scan logs, metrics, errors, readiness, repository content, generated fixtures,
  and retained test artifacts for raw credentials, tokens, and protected data.

## Verification gates

Approval alone does not enable a protected Worker lane. Implementation must
provide, at minimum:

- profile contract tests for credential acquisition, signed-JWT enforcement,
  opaque-token rejection, signature/trust, algorithm, issuer, audience, stable
  principal, `nbf`/`exp`, bounded skew, key/discovery refresh/cache, expiry,
  refresh, and secret rotation;
- binding provision/revoke/replace, version-race, multi-instance, audit-failure,
  and no-secret-persistence tests;
- additive migration and populated-`0024` upgrade evidence without historical
  authority backfill;
- Invocation delegation issue/link and complete cross-scope negative tests;
- claim, pre-model disclosure, fragment, seal, Decision/effect, failure,
  completion, retry/release, and timer-created-work authorization race tests;
- shutdown-versus-refresh, readiness, bounded diagnostics, restart,
  crash/reclaim, fair-claim, and dependency-outage tests;
- locked regression, architecture-boundary, documentation, whitespace,
  supply-chain/OCI, and secret/artifact checks applicable to changed inputs; and
- independent backend and security/privacy review of the implemented path.

Production or Staging enablement additionally requires an approved deployment
profile recording its issuer, audience, stable principal rule, algorithms,
proof lifetime maximum, skew, refresh/cache/retry/timeout bounds, secret
delivery and rotation procedure, binding provisioning/revocation procedure,
and evidence references. Live provider qualification remains a separate gate.

## Approved decision disposition

Approval on 2026-08-18 adopted every disposition below. Deployment-specific
numeric bounds remain mandatory profile evidence rather than open architecture
questions; they must be approved before that profile enables Production or
Staging work.

| Decision topic | Approved disposition and rationale | Implementation/profile evidence owner |
| --- | --- | --- |
| Reference workload authentication profile | A signed JWT access token obtained with OAuth 2.0 client credentials, as specified above. It provides portable short-lived proof using the existing OIDC-capable and mounted-secret families without making the identity provider the authorization owner. | Architecture, Operations, Security/Privacy |
| Cached issuer verification keys during an outage | Only previously validated keys inside the approved positive profile cache lifetime, while independently enforcing proof expiry and current durable binding. Unknown keys or expired/ambiguous cache close new work. This preserves bounded availability without accepting unverifiable proof. | Architecture, Operations, Security/Privacy |
| Delegation granularity | One per Session, with exact Invocation/work checks inside it. This preserves retry and mixed trusted-trigger behavior with less authority-record churn. | Architecture, Security/Privacy |
| Invocation-delegation lifetime and renewal | Derive expiry from Session cutoff plus an approved positive recovery allowance, cap it by an approved deployment maximum, and do not renew in this slice. This avoids copying the timer lane's seven-day cap without product evidence. | Product, Architecture, Operations |
| Workload-principal binding provisioning | A non-interactive idempotent, separately authorized operator command with durable audit; no seed-data authority or general administration UI. This supports repeatable deployment without turning migrations or configuration into permission. | Architecture, Operations, Security/Privacy |
| In-flight provider call after authority loss | Request cancellation when supported, discard/deny all later protected commits, and emit only bounded outcome categories. Cancellation reduces disclosure and cost but is not the correctness boundary. | Architecture, Security/Privacy |
| Claimed work after authority loss | Do not use a cleanup bypass; allow the lease to expire and keep the row ineligible. Any reconciliation command needs a separately approved contract. | Architecture, Operations, Security/Privacy |

## Consequences

- Production-capable Worker lanes gain cryptographic workload identity without
  coupling the authorization kernel to Keycloak, a cloud, Kubernetes, or mTLS.
- A stolen reference-profile client credential remains security-sensitive; the
  short-lived token does not remove secret rotation, TLS, least privilege, and
  issuer administration obligations.
- Identity provider and verification-key availability enter the Worker's
  recoverable identity observation loop. Readiness reports that gate only; it
  does not mint tokens or query JWKS. While a cryptographic proof remains valid,
  OAuth re-checks the current durable principal binding without requesting a new
  access token. Transactional authorization remains the authoritative binding
  check at claim, disclosure, and commit.
- Every Invocation work item carries inspectable authority lineage, and retries
  cannot widen Session or action scope.
- Timer and Invocation lanes can be operated independently, while timer-created
  Invocation work cannot be committed without a valid downstream execution
  envelope.
- Historical unverifiable work may require operator reconciliation and remains
  unavailable rather than receiving fabricated authority.
- More current-state checks occur around external disclosure and commits; this
  cost is required by ADR-002 freshness and revocation semantics.
- Approval and implementation do not qualify a live model provider, human OIDC,
  hosted Session start, backup/restore, load, recovery, or production pilot.

## Related

- Requirements: `REQ-AUTH-1`, `REQ-AUTH-2`, `REQ-AUTH-11`, `REQ-AUTH-15`,
  `REQ-AUTH-17`–`REQ-AUTH-22`, `REQ-AUTH-25`–`REQ-AUTH-31`, `AC-AUTH-16`,
  `AC-AUTH-17`, `REQ-SESS-55`–`REQ-SESS-70`, `REQ-SESS-75`, and
  `REQ-SESS-78`–`REQ-SESS-85`
- Architecture: [MVP architecture](../mvp-architecture.md),
  [Session runtime contract](../session-runtime-contract.md),
  [ADR-008](ADR-008-bounded-oss-component-set.md), and
  [ADR-015](ADR-015-session-timer-lane-service-delegation.md)
- Tracked implementation preparation:
  `.work/active/session-runtime-worker-identity-invocation-delegation.md`
