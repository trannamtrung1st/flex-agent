---
id: session-runtime-live-provider-qualification
status: in-progress
created: 2026-08-18
updated: 2026-08-21
predecessors:
  - session-runtime-worker-host-wiring
  - session-runtime-worker-identity-invocation-delegation
---

# Goal

Migrate the legacy Direct OpenAI implementation to the approved vendor-neutral
OpenAI-compatible adapter (`openai_compatible` /
`sessions.openai_compatible.v1`) behind the existing provider-neutral Sessions
execution port. Complete the migration through deterministic contract,
isolation, recovery, operability, and destination-policy evidence without
requiring a live endpoint that has not yet been selected.

Resolve only a frozen, digest-bound deployment profile, versioned adapter
configuration, and opaque deployment-default or Organization-scoped credential
binding through trusted operator state and the mounted-file secret boundary.
Preserve durable-before-display publication, current Worker authorization,
append-only provenance, and fail-closed behavior without changing payer,
provider, endpoint, model, capability, or network destination implicitly.

Migration completion requires the executable adapter, isolation, recovery,
operability, deterministic public/private destination-policy evidence, and a
bounded qualification harness that is ready to bind an exact profile. Running
that harness against a live endpoint is a deferred successor gate. Until one
exact profile passes it, the adapter remains default-off and cannot close the
OpenAI-compatible profile subset of `GATE-STACK-PROVIDERS`. The distinct
synthetic OpenRouter track remains separately gated. This task does not make a
provider/model a product default, enable Organization self-service endpoint
entry, qualify real use, or certify a production pilot.

# Governing sources

- `AGENTS.md`, `.work/README.md`, and
  `.agents/skills/implementation-workflow/SKILL.md` — tracked implementation,
  specification-driven TDD, verification, security/privacy, and completion
  rules
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — provider-neutral product meaning, MVP boundary,
  and live-provider qualification priority
- `docs/requirements/features/resolved-session-configuration.md` —
  `REQ-RSC-28`–`REQ-RSC-33`, `REQ-RSC-42`, `REQ-RSC-46`–`REQ-RSC-50`,
  `AC-RSC-21`, `AC-RSC-25`, `AC-RSC-26`, and `AC-RSC-28`
- `docs/requirements/features/session-text-lifecycle.md` — `REQ-SESS-55`–
  `REQ-SESS-70`, `REQ-SESS-78`–`REQ-SESS-85`, `AC-SESS-31`–`AC-SESS-37`,
  and `AC-SESS-42`–`AC-SESS-48`
- `docs/requirements/features/auth-resource-isolation.md` — service identity,
  delegated background execution, current authorization, sensitive-data
  minimization, and fail-closed audit requirements
- `docs/architecture/mvp-architecture.md` and
  `docs/architecture/session-runtime-contract.md` — provider trust boundary,
  execution ordering, durable fragment publication, timeout/cancellation,
  recovery, manifest provenance, and production gates
- `docs/architecture/backend-module-architecture.md` — Sessions-owned port,
  external-provider adapter assembly, composition-root, and architecture-test
  boundaries
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`,
  `ADR-003-authorization-audit-persistence.md`,
  `ADR-008-bounded-oss-component-set.md`,
  `ADR-010-dotnet-implementation-stack-and-workspace.md`,
  `ADR-011-participant-visible-agent-response-streaming.md`,
  `ADR-012-structured-agent-invocation-and-decision-boundary.md`, and
  `ADR-016-worker-workload-identity-and-invocation-delegation.md`
- `.work/active/session-runtime-worker-host-wiring.md` and
  `.work/active/session-runtime-worker-identity-invocation-delegation.md` —
  completed predecessor seams and explicitly deferred provider work

# Scope

## In

- Reconcile the exact post-predecessor Worker, Sessions, IdentityAccess,
  PostgreSQL, migration, package, and test baseline before behavior changes.
- Keep `IModelExecutionPort` and all domain/application contracts
  provider-neutral. Rename the provider assembly and namespace to
  `FlexAgent.Sessions.OpenAiCompatible`; keep any OpenAI SDK dependency internal
  to that adapter and retain it only if custom-endpoint contract tests prove the
  required base-path, streaming, cancellation, and structured-output behavior.
- Add a trusted deployment-profile registry/resolver for installed adapters.
  The resolved profile must positively bind the adapter/contract version,
  approved endpoint or deployment, requested model, immutable resolved model
  version or approved fingerprint, capability profile, credential mode,
  generation bounds, timeouts, retry policy, and safe correlation policy.
- Consume the exact profile reference/version/digest and non-secret credential
  binding from the trusted frozen Session binding. Mutable Worker-global
  `Sessions:ModelDeployment:*` settings may bootstrap synthetic tests or locate
  an installed registry, but they must not select or replace the behaviorally
  material provider/model/endpoint/credential for an already-frozen real
  Session.
- Support the approved `deployment_default` and `organization_byok` credential
  modes using opaque reference/version values already frozen for the Session.
  Resolve the raw credential only through an authorized mounted-file secret
  boundary immediately before model work.
- Validate binding owner scope, provider, endpoint/deployment, binding version,
  active/revoked state, and profile compatibility before any prompt or protected
  content crosses the provider boundary. Missing or mismatched state must not
  fall back to another binding, payer, endpoint, model, OpenRouter route, or
  provider.
- Implement the OpenAI-compatible adapter for the current two-phase execution
  contract: bounded structured Agent Decision control followed, only when
  accepted, by non-overlapping Participant-visible content events. Normalize
  SDK/provider details, streaming deltas, completion, usage, request
  correlation, cancellation, timeouts, rate limits, transient failures,
  malformed control, and terminal provider failures into application-owned
  outcomes.
- Make the two provider phases restart-safe before using a live adapter. Both
  phases must receive the same trusted frozen profile, credential-binding
  reference, minimized Invocation context, attempt identity, and bounds through
  an explicit application-owned contract or a durable provider-attempt
  reference. The adapter must not depend on process-local cached control-call
  state to start content after a Worker restart.
- Define one effective retry budget across durable Invocation attempts and
  lower-level SDK/provider requests. Disable or bound SDK-implicit retries under
  that budget, record each provider request attempt, and never restart or
  replace a stream after a Participant-visible fragment commits.
- Bind the operator-approved HTTPS origin to a versioned adapter configuration
  whose digest covers the exact API base path and destination-policy identity.
  Public destinations fail closed on non-public/reserved results. Private
  destinations require an explicit operator allowlist and DNS/rebinding-safe
  connection policy; loopback, link-local, metadata, unspecified, multicast,
  mixed allowed/disallowed answers, redirects, and unapproved proxies remain
  denied. No Organization, Activity, Session, Participant, prompt, or provider
  content may supply endpoint/proxy/redirect authority.
- Preserve the runtime's independent Decision/item validation and prevent
  provider output or metadata from establishing authorization, trusted scope,
  trigger provenance, output identity/audience, timer authority, workflow
  state, or any other domain effect.
- Add bounded claim-lease renewal for the control call and content stream so a
  live provider call cannot lose its lease silently, publish through a reclaimed
  attempt, or complete work after current authorization, cutoff, cancellation,
  or shutdown wins.
- Record the required append-only execution-attempt and manifest provenance:
  adapter/contract version, requested and resolved model identity/fingerprint,
  permitted generation parameters, timing, bounded outcome, usage, and
  protected provider correlation. Store stable protected references rather
  than prompts, outputs, credentials, tokens, or unrestricted provider payloads.
- Use only additive schema/contract changes if current records cannot preserve
  required provenance. Recheck the migration head before editing and never
  rewrite applied migrations `0001`–`0033`.
- Gate Worker composition and readiness on an explicit installed and qualified
  provider profile in addition to the existing workload-identity, delegation,
  lane, persistence, and frozen-binding gates. All protected lanes and provider
  selection remain default-off and fail closed.
- Produce repeatable fake-transport provider contract tests plus a bounded,
  opt-in qualification harness that can later consume an exact installed
  profile and synthetic data. Migration verification covers the harness gates,
  redaction, budget, and fail-closed behavior without fabricating live
  capability, latency, capacity, cost, privacy/data-policy, or immutable-model
  evidence. A successor qualification task records those facts when an owner
  selects an exact endpoint. OpenRouter evidence remains a separate gate.
- Reconcile authoritative implementation-status tables, workspace/operator
  guidance, package locks, qualification evidence, and this task after tests
  pass.

## Out

- Selecting a preferred or product-default model/provider, using mutable model
  aliases as immutable provenance, or promising compatibility beyond the exact
  qualified deployment profile
- OpenRouter for real assessment data, automatic provider/model routing,
  dynamic/free fallback, provider-specific Azure semantics, or a second
  external adapter
- Organization-supplied arbitrary endpoint URLs, Organization-installed code,
  browser/API entry of raw provider keys, or a self-service provider/plugin UI
- Human OIDC/application sessions, hosted Session creation/start, Assessment or
  Submission implementation, Evaluation, Review/Release, lifecycle/export,
  backup/restore, or production-pilot certification
- Full start-time enforcement of `REQ-RSC-32`/`AC-RSC-9` for newly created
  Sessions. This task must consume and revalidate frozen model identity and fail
  closed before model disclosure, but the owning configuration/start slice must
  still block Session start when immutable identity cannot be frozen.
- Interaction Controller, voice, tools, Dynamic memory, richer triggers, or any
  expansion of the approved P0 Decision/output/action profile
- Provider calls inside database transactions, provider-native durable
  contracts, raw provider payload persistence, or provider metadata as product
  authority
- Real participant data during qualification until the concrete provider data,
  retention, residency, and privacy policy has explicit approval
- Running live OpenAI-compatible qualification, applying a qualification label,
  or enabling any compatible endpoint before an owner supplies and approves an
  exact profile, credential, destination policy, and bounded synthetic run
- Commits, pushes, pull requests, deployments, releases, or enabling production
  traffic unless separately requested

# Historical pre-Phase-A seam inventory (retained)

- `IModelExecutionPort` owns provider-neutral control execution and optional
  Participant-visible content events. The deterministic fake and fail-closed
  implementations are the only current adapters.
- `ModelDeploymentCredentialBindingResolver` already chooses an
  Organization-scoped binding before a deployment default and rejects missing,
  incomplete, revoked, wrong-Organization, and provider-mismatched candidates;
  it does not resolve a raw provider credential.
- `MountedFileSecretSource` rejects ordinary filename traversal and reads a
  named file under a configured root. Provider use still needs explicit tests
  for symlink escape, file type/permissions, size bounds, atomic rotation, and
  safe failure while a credential is replaced.
- The Worker currently reads an opaque provider/binding configuration, but its
  production-shaped Sessions composition still supplies
  `FailClosedModelExecutionPort`; no provider SDK is pinned in the solution.
- `DurableInvocationWorkSettings` currently derives provider and binding values
  from mutable Worker-global `Sessions:ModelDeployment:*` configuration.
  `TrustedSessionBinding` and its PostgreSQL rehydration currently carry frozen
  policy and permitted content references, but not the complete frozen
  provider profile or credential binding. That host-global seam is insufficient
  authority for a live multi-Organization Session.
- Durable Invocation claiming, workload authentication, per-Session
  `session.invocation.execute` delegation, model-disclosure admission,
  fragment/seal persistence, and post-call commit reauthorization exist in the
  predecessor slices.
- Fragment persistence renews the claim lease, but the control `ExecuteAsync`
  phase can exceed the lease before the first fragment. Live provider work must
  close this reclaim/concurrency gap.
- Current execution requests carry provider and opaque binding identifiers but
  do not yet carry the complete trusted deployment profile or all manifest
  provenance required to qualify a live provider.
- `ModelContentStreamRequest` currently carries only ownership, Invocation ID,
  and generation-attempt ID. After the Decision commit, a restarted Worker can
  enter content publication without the original context, frozen provider
  profile, or credential binding. A live adapter cannot rely on an in-memory
  lookup to bridge this gap.
- ADR-016 readiness/remediation changes were committed as `94c1412` while this
  task was being planned. The implementation baseline is clean at that commit;
  the current-source solution baseline passed during readiness review.

# 2026-08-20 current seam audit

- `IModelExecutionPort`, frozen profile/binding resolution, mounted provider
  secrets, provider-request reservation, lease/auth fencing, append-only
  provenance, and OpenRouter separation are reusable without changing domain
  product semantics.
- The legacy implementation is concentrated in
  `FlexAgent.Sessions.OpenAi`, `DirectOpenAiModelExecutionAdapter`,
  `ModelDeploymentAdapterKinds.DirectOpenAi`, Worker composition/readiness,
  solution/Docker/project references, four adapter test files, architecture
  tests, and legacy fixtures. The repository audit found 48 direct-identity
  occurrences across 10 C# test files plus the named project/runtime surfaces.
- `ApprovedHttpsOrigin` intentionally canonicalizes only an HTTPS origin and
  rejects paths. The new compatible base path therefore belongs in a separate
  versioned adapter configuration whose digest participates in the installed
  profile identity; it must not be overloaded into the origin field.
- The legacy `ApprovedOriginHandler` checks scheme, host, port, literal private
  addresses, and redirects, but it does not resolve DNS or pin a validated
  address. A hostname that resolves or rebinds to a private destination is not
  covered. Private endpoint support requires a new injectable
  resolver/destination/connector boundary rather than renaming the handler.
- Installed profiles already carry an optional `AdapterConfigurationDigest`,
  and OpenRouter demonstrates a separate configuration registry. Reuse that
  pattern, but require the digest for `openai_compatible`; legacy profile digest
  calculation must remain reproducible for historical inspection.
- PostgreSQL stores adapter kind and contract version as append-only text and
  has no enum constraint. No data rewrite is needed for the identity change.
  Historical `direct_openai` rows must remain byte-for-byte attributable, while
  new reservations/provenance use only the new identity.
- The current official SDK is isolated correctly but its custom base-path and
  compatible-runtime behavior are not proven. Deterministic request-URI tests
  decide whether to retain it.
- No SPA contract, user journey, or visual state changes. Frontend and
  Playwright work are not applicable unless a later operator UI is separately
  approved.

# Requirement-to-surface matrix

| Concern | Implementation surface | Required verification |
| --- | --- | --- |
| Frozen provider/model identity (`REQ-RSC-28`–`REQ-RSC-33`, `REQ-RSC-46`) | Trusted Session binding plus installed profile resolver; frozen non-secret profile and binding references; execution/manifest append | Worker-config mutation after freeze, mutable alias, wrong profile, wrong endpoint/model, missing immutable identity, and client/session substitution fail closed; full start-time enforcement remains a separately recorded gap |
| Credential isolation (`REQ-RSC-42`, `REQ-RSC-46`, `AC-RSC-21`, `AC-RSC-25`) | Opaque binding resolver plus authorized mounted-file secret adapter | Deployment default and Organization BYOK success; wrong Organization/provider/version, revoke/rotate race, missing secret, traversal/symlink/file-permission/size failures, and every silent-fallback case |
| Provider-neutral execution (`REQ-SESS-61`–`REQ-SESS-70`, `AC-SESS-33`–`AC-SESS-37`) | Sessions-owned OpenAI-compatible adapter implementing `IModelExecutionPort` | Structured Decision/no-action, malformed control, timeout, cancellation, retry, outage, rate limit, and late/cutoff outcomes without fabricated Decisions |
| Restart-safe provider phases (`REQ-SESS-16`–`REQ-SESS-18`, `REQ-SESS-63`, `REQ-SESS-66`, `REQ-SESS-67`) | Application-owned request/attempt contract available independently to control and content phases | Process restart and adapter replacement after control commit; retry before visibility; no process-local cache authority; no restart after visible fragment |
| Durable streaming (`REQ-SESS-55`–`REQ-SESS-60`, `REQ-SESS-66`, `REQ-SESS-84`, `AC-SESS-31`, `AC-SESS-32`) | SDK stream normalization into exact non-overlapping content events; existing publication coordinator | Delta order, duplicate/cumulative/divergent events, Unicode and size bounds, persist-before-display, partial-stream failure, and reconnect reconstruction |
| Decision authority (`REQ-SESS-64`, `REQ-SESS-78`–`REQ-SESS-85`, `AC-SESS-42`–`AC-SESS-48`) | Existing schema/profile validators and independent runtime effect boundary | Prompt/provider attempts to widen audience, identifiers, output/action kinds, scope, tools, workflow, timer, or Release cause no prohibited effect |
| Lease, auth, and lifecycle races (`REQ-SESS-67`, `ADR-002`, `ADR-016`) | Durable work processor, lease renewal, cancellation, disclosure admission, and commit reauthorization | Blocking control call and stream; competing claimant; shutdown, revoke, pause, cutoff, lease loss, and refresh failure before disclosure and each commit |
| Provenance and minimization (`REQ-RSC-33`, `REQ-RSC-50`, `REQ-SESS-70`, `REQ-SESS-85`) | Append-only attempt/manifest records and allowlisted telemetry | Exact adapter/model/profile/outcome/usage reconstruction; no prompt, output, credential, token, protected IDs, or raw provider body in generic records or diagnostics |
| Module and supply-chain gates (`ADR-008`, `ADR-010`) | Separate adapter assembly, central package pin, lock files, solution/OCI/SBOM inputs, architecture rules | Official SDK confined to adapter; negative architecture control; locked restore, license/SBOM/vulnerability/secret checks, OCI build |
| Network/egress boundary (`ADR-008` model-provider gate) | Digest-bound OpenAI-compatible adapter configuration plus bounded HTTP/SDK transport | Exact HTTPS origin and base path; public-only and explicit private-allowlist policies; redirect escape, loopback, link-local, metadata, unspecified/multicast/reserved address, mixed DNS answer, rebinding, proxy/header substitution, DNS/connection timeout, and endpoint mismatch fail closed |
| Qualification readiness and deferred exact-profile evidence (`ADR-008`, OpenAI-compatible subset of `GATE-STACK-PROVIDERS`) | Opt-in synthetic qualification harness plus a successor exact-profile run | Deterministic harness gates, budgets, redaction, no-fallback, and failure behavior pass during migration; a later live run must add latency/capacity/cost, returned model identity plus operator artifact/deployment fingerprint, data policy, correlation, credential isolation, and network-policy evidence before enablement; OpenRouter remains a distinct track |

# Security and privacy threat model

| Threat / privacy harm | Preventive boundary | Executable evidence |
| --- | --- | --- |
| Cross-Organization credential or payer substitution | Trusted frozen profile plus owner/provider/version binding; no fallback | Wrong-Organization, wrong-provider, stale/revoked/rotated binding, concurrent rotation, and fallback-denial tests |
| Secret disclosure through paths, symlinks, diagnostics, headers, or artifacts | Named mounted-file lookup with containment, file/permission/size checks, bounded lifetime, and telemetry/log redaction | Traversal and symlink escape, non-regular/oversized/unreadable file, exception/log/header capture, SBOM/support-artifact secret scans |
| SSRF or egress to attacker-controlled/internal destinations | Installed origin plus digest-bound base path and destination policy; DNS answers validated and connection-pinned under policy; redirects/proxies denied by default; no untrusted endpoint input | Loopback, link-local, metadata, unspecified/multicast/reserved ranges, unapproved private ranges, mixed DNS answers, rebinding, alternate scheme/port/origin/base path, redirect, proxy/header, and resolution-change tests |
| Prompt/provider confused deputy | Trusted instructions and scope remain separate from untrusted Participant/model content; existing Decision/effect validators remain authoritative | Malicious prompt/control/metadata attempts to change scope, audience, tools, memory, workflow, Evaluation, Review, Result, or Release cause no effect |
| Retry amplification, duplicate billing, or resource exhaustion | One frozen retry/timeout/token/request budget across SDK and durable attempts; rate/backpressure limits | SDK retry plus work-redelivery matrix, rate-limit/outage storm, cancellation, timeout, byte/token/event bounds, and quota attribution |
| Restart, lease loss, revoke, pause, or cutoff races disclose late content | Restart-safe attempt contract, periodic lease renewal, disclosure authorization, per-fragment commit reauthorization | Crash between phases, competing claim, adapter replacement, lease-renew failure, revoke/pause/cutoff/shutdown before call and before each commit |
| Excessive external disclosure or secondary use | Minimized frozen Invocation context, approved synthetic-only qualification, concrete provider data-policy gate | Request-shape allowlist, no unrelated Session/Organization data, retention/training policy evidence, and no real participant data before approval |
| Sensitive telemetry or provenance over-collection | Protected correlation/reference plus bounded outcome/usage fields; raw payload exclusion | Logs, metrics, traces, audit, manifest, errors, fixtures, and qualification artifacts scanned for prompts, outputs, tokens, credentials, and stable participant identifiers |

# Plan

- [x] Confirm the clean `94c1412` predecessor baseline, recheck migration and
      package heads, run focused Worker identity/authorization/Sessions tests,
      and update this task if executable seams differ from the inventory above.
- [x] Build an executable requirement-to-code/test inventory for profile
      resolution, secrets, adapter calls, lease renewal, publication,
      provenance, host composition, readiness, telemetry, and qualification.
- [x] Red — prove a frozen Session cannot obtain its provider, model, endpoint,
      capability, or credential binding from mutable Worker-global settings.
      Cover two Organizations, Worker configuration changed after Session
      freeze, wrong profile digest/version, and missing frozen profile/binding.
- [x] Green — extend the trusted Session binding and PostgreSQL rehydration with
      the minimum immutable non-secret provider-profile and credential-binding
      references. Resolve those references against the installed registry and
      secret boundary at execution time; retain host configuration only as
      non-authoritative adapter/registry bootstrap.
- [x] Red — reproduce process restart or adapter replacement after the
      structured Decision commits but before the first content event. Prove the
      current content request cannot safely reconstruct the frozen profile,
      credential binding, minimized context, provider-attempt identity, and
      request bounds without process-local state.
- [x] Green — extend the application-owned provider request/attempt contract or
      persist the minimum protected provider-attempt reference so control and
      content phases are independently reconstructable. Preserve the approved
      one-or-multiple-provider-phase option, use a new provider request attempt
      when retrying before visibility, and prohibit restart after visibility.
- [x] Red — add architecture and composition tests proving the OpenAI SDK can
      exist only in a Sessions-owned adapter assembly; Sessions core, API,
      Worker policy, browser contracts, and unrelated modules remain free of
      provider packages and provider-native types.
- [x] Green — add the smallest adapter project/test boundary, pin the official
      OpenAI .NET SDK centrally with locked transitive dependencies, wire it
      through the provider-neutral port, and retain fail-closed/default-off host
      behavior.
- [x] Red — add trusted-profile and credential tests for exact installed
      adapter/profile resolution, immutable model identity, capability and
      endpoint matching, deployment-default and Organization BYOK selection,
      missing/incomplete/revoked/rotated/wrong-scope bindings, secret-file
      traversal/symlink/type/permissions/size/read/rotation failures, and every
      prohibited fallback.
- [x] Green — implement the minimal server-owned profile and provider-secret
      resolution boundary. Resolve raw secrets only at the adapter edge, keep
      them out of object display/logging/persistence, and settle shared
      `SecretSource` ownership according to the backend module admission rules.
- [x] Red — drive the official SDK through a fake HTTP transport and prove
      bounded structured control, content deltas, usage/provenance,
      cancellation, timeout, rate limit, retryable/non-retryable errors,
      malformed/oversized output, provider outage, and safe errors without a
      network dependency.
- [x] Green — implement Direct OpenAI request/response mapping, streaming
      normalization, single-owner bounded retry and timeout behavior, failure
      categories, cancellation propagation, usage/provenance capture, and
      protected request correlation without leaking SDK types into
      application/domain contracts.
- [x] Red/green — enforce the installed profile's Direct OpenAI HTTPS egress
      allowlist and transport bounds. Deny alternate schemes/origins/ports,
      redirect escape, loopback/link-local/metadata/private destinations,
      untrusted proxy/header substitution, and endpoint mismatch without
      disclosing the rejected destination or credential.
- [x] Red — reproduce lease expiry during a blocked control call and long
      content stream, including competing claim, revoke, pause, cutoff,
      shutdown, and renewal failure races.
- [x] Green — add bounded claim renewal/cancellation across both provider phases
      so lease or authority loss stops further disclosure/publication and cannot
      ACK, retry, or complete a reclaimed attempt incorrectly.
- [x] Red/green — append the minimum required execution/manifest provenance and,
      only if needed, add the next immutable migration and contract version.
      Prove ordering, idempotency, crash recovery, seal/reconstruction, and that
      sensitive provider material never enters audit, outbox, manifest,
      telemetry, errors, or test artifacts.
- [x] Red/green — compose the qualified adapter in the Worker only when the
      exact profile, mounted secret, current workload identity, Invocation
      delegation, Sessions persistence, lane flag, and numeric bounds are all
      valid. Prove missing/stale/mismatched configuration keeps readiness honest
      and provider work disabled without affecting liveness or unrelated lanes.
- [-] Historical Direct OpenAI live qualification was intentionally skipped
      after the vendor-neutral target superseded it. Do not spend live calls or
      apply a qualification label to the legacy identity; retain its
      deterministic evidence only as migration input.
- [x] Run focused adapter/Sessions/Runtime/PostgreSQL/architecture tests, then
      locked solution, package/supply-chain, OCI, documentation, whitespace,
      and applicable failure/recovery checks. Record exact commands and counts.
- [x] Resolve independent review blockers: serialize permitted InvocationContext
      into provider messages; execute and validate frozen ResolvedModelVersion;
      cancel provider work when lease renewal throws; give each external request
      its own provenance identity and phase; canonicalize origin-only endpoints
      into the profile digest; enforce adapter contract version.
- [x] Close remaining Phase A blockers from `dc727f2` review: persist participant
      exact text through accept; make `0027` safe for populated `0026` rows;
      treat claim-authority cancellation as retry through the Direct OpenAI
      adapter; record failed provider requests and enforce the request budget.
- [x] Close remaining Phase A blockers from `e17b546` review: reserve each
      provider request durably before network I/O; require participant exact
      text at admission; record the `0027` checksum/history assertion; refresh
      the verification table.
- [x] Close remaining Phase A blockers from `353a242` review: atomically
      reserve a provider request under the current claim fence, reject a stale
      worker after lease reclaim, and add an overlapping-lease test.
- [x] Close remaining Phase A blockers from `eb432f2` review: reauthorize
      workload/delegation in the same transaction as claim-fenced reservation,
      require the admission port on every executable processor, and state the
      post-reservation dispatch residual accurately.
- [x] Close remaining P2 hardening from `4373f70` review: require
      authorization dependencies on PostgreSQL admission, and cover
      service-principal-binding revoke at reservation.
- [x] Record independent review of `4a6e314`: no P0/P1; leftover invocation-id
      coupling was recorded as hardening backlog; deterministic Phase A may
      proceed to Phase B migration.
- [x] Close the leftover P2 from `4a6e314`: `TryReserveAsync` fails closed
      unless the supplied invocation id equals `claimedWork.AgentInvocationId`,
      and both admission implementations budget/insert only that claimed id.
- [x] Review the 2026-08-20 vendor-neutral documentation amendment against
      product scope, ADR authority, current code, durable identifiers, and the
      endpoint trust boundary; correct stale status, scope ambiguity, and the
      legacy DNS-control overclaim.
- [ ] Red — pin the target adapter kind `openai_compatible`, contract
      `sessions.openai_compatible.v1`, project/namespace/class names, and
      example-profile digest. Prove `direct_openai` configuration cannot enable
      execution and legacy stored provenance remains readable without becoming
      executable authority.
- [ ] Green — rename the adapter and test projects/namespaces/classes; update
      solution, Worker project references/composition/readiness, Docker copy
      inputs, architecture boundaries, package locks, and qualification opt-in
      naming. Keep OpenRouter independent and keep the provider SDK isolated.
- [ ] Red/green — add a versioned OpenAI-compatible adapter-configuration file
      and registry. Require its digest in every `openai_compatible` installed
      profile and bind the exact API base path plus destination-policy identity
      into profile resolution and provenance; reject missing, extra,
      mismatched, duplicate, or cross-profile configuration.
- [ ] Red/green — replace the partial origin guard with an injectable endpoint
      destination policy and resolver/connector. Cover public-only and explicit
      private-CIDR allowlists, all DNS answers, mixed answers, rebinding/TOCTOU,
      IPv4/IPv6 loopback/link-local/metadata/unspecified/multicast/reserved
      ranges, redirect and proxy denial, TLS hostname/SNI preservation, system
      trust, exact port, and base-path confinement.
- [ ] Red/green — requalify the two-phase Chat Completions subset against fake
      transports at multiple compatible base paths. Pin request shape,
      structured control, streaming deltas, model identity, usage, timeouts,
      cancellation, retry ownership, failure normalization, and provenance. If
      the current SDK cannot meet the contract, replace it only inside the
      adapter boundary.
- [ ] Red/green — update Worker composition so only an installed
      `openai_compatible` profile plus matching adapter configuration and an
      accepted exact-profile qualification record can enable the port. Prove
      deterministic test fixtures cannot create real enablement authority.
      Preserve default-off behavior, frozen Session authority, mounted-secret
      isolation, claim fencing, current authorization, no fallback, and
      truthful readiness diagnostics.
- [ ] Preserve history without reinterpretation: do not rewrite migrations
      `0001`–`0033` or append-only `direct_openai` provenance. Add upgrade and
      reconstruction tests showing legacy rows remain inspectable, legacy
      active bindings fail closed, and new attempts record only the new adapter
      kind/contract/profile digest. Add a new migration only if newly required
      provenance cannot be represented by existing append-only fields.
- [ ] Run focused domain, adapter, Runtime, Architecture, and PostgreSQL tests,
      then the locked full solution, OCI build, SBOM/license/vulnerability and
      secret checks, documentation validation, JSON/example consistency, and
      whitespace checks. Obtain independent backend and security/privacy review.
- [-] Defer the bounded synthetic-only live qualification run to a successor
      task created when an owner supplies one exact approved compatible
      deployment profile, credential, destination policy, and run budget. Do
      not claim qualification or enablement from deterministic migration
      evidence.
- [ ] Reconcile implementation status, operator guidance, qualification
      readiness, the deferred exact-profile gate, remaining OpenRouter gaps,
      and final migration completion.

# Current state

Independent review of `4a6e314` approved the deterministic legacy Phase A
provider-execution/admission slice: no P0/P1 findings. Its leftover P2
(admission must bind reservation to `claimedWork.AgentInvocationId`) remains
closed. On 2026-08-20, ADR-008 and ADR-010 superseded the vendor-specific target
with `openai_compatible` / `sessions.openai_compatible.v1` and made an exact
operator-installed on-premises endpoint a first-class compatibility target.

The current code is intentionally divergent: it still exposes
`direct_openai`, `sessions.openai.v1`, `FlexAgent.Sessions.OpenAi`, an origin-only
profile, and a partial literal-IP/same-origin guard. The next implementation
step is the red contract-identity and fail-closed legacy test slice described
above. No exact OpenAI-compatible live profile is selected or qualified, but
that is not a blocker to deterministic migration. All compatible live
composition remains default-off.

On 2026-08-19 the Product Lead separately approved the OpenRouter
synthetic-development profile and its implementation task. Its historical live
evidence and the later Phase 28 `qualified_for: synthetic_development` label are
not OpenAI-compatible qualification evidence, and the OpenRouter track remains
independently gated. It cannot authorize production/Participant data or
substitute for this migration or its deferred exact-profile successor.

# Delivery phases

- **Historical Phase A — legacy deterministic implementation:** restart-safe provider-phase
  contract, adapter/module boundary, frozen profile and secret resolution,
  fake-transport SDK contract tests, egress controls, retry/lease behavior,
  provenance, host composition, and regression/supply-chain evidence. This
  evidence remains useful but does not qualify the new adapter identity or
  private-endpoint boundary.
- **Phase B — OpenAI-compatible migration:** vendor-neutral identity and module
  rename, digest-bound base-path/destination configuration, DNS/rebinding-safe
  public/private endpoint enforcement, fail-closed legacy handling, and full
  deterministic regression evidence.
- **Deferred successor — exact-profile qualification:** after migration, run
  bounded synthetic calls against one owner-selected compatible profile and
  retain privacy/data-policy, network-policy, capacity, cost,
  immutable-identity, operational, and credential-isolation evidence. This
  successor remains mandatory before enablement but is not a migration
  completion criterion. Deterministic evidence must never be represented as
  live qualification.

# Decisions

- The supported external adapter identity is `openai_compatible` with contract
  `sessions.openai_compatible.v1`; `direct_openai` is historical only and must
  not be accepted as an enablement alias.
- The official OpenAI .NET SDK is conditional internal transport. Retain it only
  if deterministic custom-endpoint tests prove the approved contract; otherwise
  replace it without changing the Sessions port or durable domain contracts.
- The adapter belongs to Sessions but must be packaged separately from the
  Sessions core and composition roots because an external-provider SDK meets
  the approved project-splitting condition.
- OpenAI-compatible installed profiles require a versioned adapter-configuration
  digest binding the exact base path and destination-policy identity. The
  approved origin alone is insufficient.
- Historical `direct_openai` profile digests and append-only provider-attempt
  rows are never rewritten or relabeled. A legacy frozen active binding fails
  closed; it is not silently translated into the new contract.
- Operator-installed on-premises endpoints are in scope. Organization
  self-service endpoint entry, untrusted adapter code, arbitrary URLs, and
  automatic routing remain out of scope.
- Qualification is attached to one exact provider deployment profile. Runtime
  configuration may select only installed, operator-approved profiles; it
  cannot construct arbitrary endpoints or silently substitute another profile.
- Deterministic adapter migration and exact-profile live qualification are
  separate delivery gates. Migration may complete without a selected live
  endpoint, but no compatible endpoint may be enabled until its successor
  qualification passes.
- A real Session's frozen trusted binding, not Worker-global configuration,
  selects its behaviorally material provider profile and opaque credential
  binding. Host configuration may install/locate adapters and registries only.
- Live qualification uses bounded synthetic content until a concrete provider
  data/retention policy is approved for real participant data.
- Provider integration does not weaken current authorization, frozen policy,
  Decision validation, durable-before-display, or commit reauthorization
  boundaries.

# Open questions

Interim defaults are working guidance only and do not approve a deployment.

- **Which exact OpenAI-compatible deployment/model profile is qualified first?**
  Interim default: prefer the intended operator-managed on-premises runtime
  when its owner supplies an exact endpoint, artifact/deployment fingerprint,
  synthetic-use credential, and data-policy determination; otherwise select no
  normative model and leave qualification blocked. Rationale: this validates
  the motivating deployment shape without fabricating evidence or making it a
  product default.
- **Which authentication modes does v1 support?** Interim default: one
  mounted-secret API-key/Bearer-header mode; anonymous endpoints, per-profile
  custom headers, mTLS identity, and workload-identity exchange remain disabled
  until separately designed and reviewed. Rationale: preserve the existing
  credential-isolation contract and minimize the first compatibility surface.
- **How is private TLS trust established?** Interim default: use the
  host/container platform trust store and require operators to install the
  approved private CA there; never add per-profile certificate-validation
  bypass. Rationale: keeps trust operationally reviewable and avoids an unsafe
  compatibility escape hatch.
- **How is an immutable on-premises model identified when the API returns an
  alias?** Interim default: the response model must match the frozen returned
  identity, while the digest-bound adapter configuration and retained
  qualification evidence map that identity to an operator-attested immutable
  artifact/deployment fingerprint. Rationale: the adapter cannot infer an
  artifact digest that the compatible API does not expose.
- **Where should the generic mounted-file secret contract live once both Worker
  identity and Sessions provider credentials use it?** Interim default: first
  prove the dependency boundary with architecture tests, then extract only the
  narrow read-only contract/adapter if it satisfies the building-block admission
  rule; otherwise keep a Sessions-owned provider-secret port. Rationale: avoid
  coupling provider execution to IdentityAccess infrastructure or creating a
  generic utility layer prematurely.
- **Does required provider provenance need a new database migration?** Interim
  default: reuse existing append-only attempt/manifest fields when they can
  preserve every required fact without reinterpretation; otherwise add the next
  immutable additive migration after rechecking the head. Rationale: preserve
  applied history while meeting reconstruction requirements.
- **Where is non-secret profile qualification evidence retained?** Interim
  default: add a versioned, machine-readable profile lock and sanitized evidence
  under an operator-reviewable repository artifact location selected during the
  implementation inventory, and summarize truthful implementation status in
  authoritative docs. Do not store secrets, raw provider responses, prompts,
  participant data, or account-specific identifiers in `.work/` or committed
  evidence. Rationale: qualification must be reviewable and reproducible, while
  `.work/` remains non-authoritative execution state.
- **How are the control and content provider phases reconstructed after a
  restart?** Interim default: do not use an adapter-local cache. Extend the
  application-owned request with the same trusted frozen profile, opaque
  binding, minimized context, attempt identity, and bounds for both phases, or
  persist a minimal protected provider-attempt reference if provider semantics
  require it. Rationale: the current content request is insufficient after a
  process restart, and process memory cannot become execution authority.
- **Who owns lower-level provider retries?** Interim default: the Sessions
  runtime owns the total frozen budget; any SDK retry is explicitly configured,
  counted as a provider request attempt, limited to safe pre-visibility
  failures, and disabled when it cannot be observed or bounded. Rationale:
  stacked hidden retries can exceed time/cost budgets and duplicate work.
- **Can live qualification run in this development environment?** Interim
  default: do not run it until an owner supplies an exact approved profile,
  credential, destination policy, and budget. Fake-transport contract tests are
  blocking for migration; live qualification is an opt-in, bounded,
  synthetic-only successor and is not a migration completion blocker.
  Rationale: tests must not fabricate provider evidence or expose/cost an
  account implicitly.

# Findings / deviations

- At task planning time the authoritative overview still listed production HTTP
  SSE and Worker polling among next gates although completed successor tasks
  implemented those seams. The 2026-08-19 documentation promotion corrected
  that stale status. This task owns live-provider integration and qualification,
  not a second SSE or Worker-polling implementation.
- Predecessor commit `94c1412` updates Worker readiness and workload identity
  behavior in files this task may later touch. This plan treats that commit as
  the baseline and does not reopen its completed remediation scope.
- The current port split permits one or multiple provider phases as approved by
  `SESS-DEC-19`. Control and content requests now carry frozen profile, opaque
  binding, minimized context, and provider-attempt identity so a process restart
  can reconstruct work without adapter-local cache.
- Worker-global `Sessions:ModelDeployment:*` values are no longer accepted as
  execution authority. `DurableInvocationWorkSettings` no longer carries a host
  provider id or binding callback; resolution uses the frozen Session binding
  plus installed registry and credential catalog.
- Implementer self-review (not independent reviewer): remaining unused
  host-binding settings were removed; profile `MaxProviderRequestAttempts`
  fails closed before an extra provider call; stream origin/outage errors fail
  closed without fabricating content.
- This task cannot claim full `REQ-RSC-32`/`AC-RSC-9` implementation because
  hosted configuration resolution and Session start remain out of scope. It
  owns runtime consumption/revalidation and must retain start-time enforcement
  as an explicit downstream gap.
- ADR-010's `GATE-STACK-PROVIDERS` names fake, Direct OpenAI, synthetic
  OpenRouter, and vLLM contract evidence. This task implements and qualifies the
  Direct OpenAI subset only and must not mark the complete cross-provider gate
  satisfied.
- Independent review of `bee700e` (2026-08-19) treated the adapter boundary as
  sound but Phase A incomplete: minimized context was not sent, requested-model
  aliases were executed, lease-renewal throws failed open, provenance keyed
  invocation attempts rather than provider requests, origin digest omitted
  path, and adapter contract version was not enforced. This remediation slice
  addresses those findings in place.
- Accepted participant-message bodies now flow through
  `AcceptParticipantMessageCommand.ExactUtf8Text`, domain transcript items,
  `0028` persistence, assembler, and provider-safe serialization. Integration
  reload proves the real command path, not a hand-built context.
- `0027` now disables the append-only UPDATE trigger only for the migration
  backfill, then re-enables it. A populated-`0026` upgrade test covers existing
  provider-attempt rows. Changing this already-shipped one-time script will
  fail Grate checksums on databases that applied the previous `0027` text;
  empty upgrades that never had rows are the expected local/CI case.
- Claim-authority cancellation is treated as `RetryLater` even when the adapter
  returns `Cancelled`. Direct OpenAI fake-transport plus heartbeat-throw covers
  the live adapter path. Content failures now emit `ModelContentFailed` with
  provenance. `MaxProviderRequestAttempts` counts distinct reserved
  `provider_request_id` values (`started` facts) rather than finished rows.
- Additive `0029` appends `fact_kind` (`started` | `finished`) without rewriting
  `0027` or `0028`. Budget admission writes `started` before network I/O;
  completion writes `finished`. Crash after HTTP and before `finished` still
  consumes the budget.
- Independent review of `353a242` found `CountAsync` then `WriteAsync` was not
  atomic with current claim ownership. `TryReserveAsync` now locks the
  durable-work row (`claimed`, fencing `claim_lease_until`, unexpired), counts
  distinct `provider_request_id` values, inserts `started`, and renews the
  claim in one transaction. A stale worker after reclaim receives
  `RetryLater` without HTTP. Remaining gap: a stall *after* a successful
  reservation and *before* HTTP can still send that already-budgeted request
  until heartbeat notices lease loss; that does not admit a second reservation.
  That residual is accepted for MVP: a request cannot be newly reserved after
  lease loss or after current authority is revoked, but an already-admitted
  request may still dispatch if the process is suspended until the renewed
  lease expires.
- Independent review of `eb432f2` found reservation renewed the claim without
  current authorization. `IProviderRequestAdmissionPort.TryReserveAsync` now
  revalidates workload/delegation in the same transaction as the claim lock,
  budget check, `started` insert, and lease renewal. Early
  `TryAuthorizeModelDisclosureAsync` remains fail-fast only. The executable
  processor requires the admission port; production Worker registers
  `PostgresModelProviderAttemptProvenanceWriter` with the commit kernel and
  workload identity. Postgres
  `Revoked_delegation_cannot_reserve_a_provider_request_after_disclosure`
  covers revoke-after-disclosure.
- Independent review of `4373f70` found no remaining P0/P1. PostgreSQL
  admission now requires `TrustedRuntimeActor`, `ICommitAuthorizationKernel`,
  and `IAuthenticatedWorkloadContextSource` with no skip-auth constructor.
  `Revoked_principal_binding_cannot_reserve_a_provider_request_after_disclosure`
  covers OAuth principal-binding revoke at the same reservation commit.
- Independent review of `4a6e314` (2026-08-19): no P0/P1. Do not derive a
  further Phase A remediation commit from that review. Remaining P2 is now
  closed: `TryReserveAsync` compares the supplied invocation id to
  `claimedWork.AgentInvocationId`, returns `LostClaim` on mismatch, and
  counts/inserts only the claimed id. The processor also reserves with the
  claimed id for both control and content.
- P0 participant-message admission requires non-empty exact UTF-8 text.
  `AcceptParticipantMessageCommand.ExactUtf8Text` is required; missing or blank
  text fails closed with `trigger_admission.missing_participant_content`.
- **`0027` checksum/history:** the `e17b546` rewrite of `0027` was never applied
  outside disposable local and CI databases. This MVP slice has no persistent
  operator, staging, or production database that applied the `dc727f2` `0027`
  text. No Grate checksum repair procedure is required; do not edit `0027` or
  `0028` again.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Deterministic migration / live-qualification delivery split | passed | Product, architecture, requirements, operations, contributor status, and this task now permit deterministic migration completion without an exact live profile while preserving default-off composition and exact-profile qualification as a mandatory successor before enablement. `python3 scripts/check_docs.py` and `git diff --check` pass on 2026-08-21. |
| OpenAI-compatible documentation/change-set review | passed with corrections | ADR/product/requirements/operations diff checked against current adapter, Worker composition, profile digest, PostgreSQL provenance, and endpoint guard on 2026-08-20. Corrected stale ADR amendment status, clarified operator-installed versus Organization self-service scope, required digest-bound base-path/destination configuration, and removed the DNS/rebinding security overclaim. `python3 scripts/check_docs.py`, JSON parse, and `git diff --check` pass. |
| Governing source and current-seam inventory | complete | Product foundation, RSC/Session/Auth requirements, ADR-008/010/012/016, Session runtime, backend module guide, current ports/composition, and predecessor task state reviewed 2026-08-18 |
| Predecessor remediation protected | complete | Worker identity/readiness remediation landed separately as `94c1412`; this planning edit is restricted to the new task file |
| Plan readiness review | complete | Backend/architecture/security consistency pass on 2026-08-19 added frozen per-Session provider authority, restart-safe phases, retry ownership, secret hardening, egress/SSRF, qualification scope, and threat-model gates |
| Current-source .NET baseline | passed | After required PostgreSQL admission auth: `dotnet test --solution FlexAgent.slnx` **1034 passed**, 0 failed (2026-08-19). Prior `4373f70` count was 1033 |
| Focused provider adapter tests | passed | `FlexAgent.Sessions.OpenAi.Tests` **14 passed** including fake-HTTP crash-after-request reservation (no second HTTP) and lease-renewal `RetryLater` |
| Invocation-id reservation binding (`4a6e314` P2) | passed | In-memory writer tests 4/4. Postgres `Mismatched_invocation_cannot_reserve_a_provider_request` passed on 2026-08-20 |
| Credential/profile isolation tests | passed | `FrozenModelDeploymentResolverTests` plus processor frozen-authority tests; secret symlink/size in WorkloadIdentity tests; no-fallback matrix for missing/revoked/wrong-org/provider mismatch |
| Lease/auth/lifecycle concurrency tests | passed | Claim-lease heartbeat; overlapping reclaim; Postgres delegation revoke and principal-binding revoke at reservation (no started fact, no HTTP, lease not extended) |
| PostgreSQL migration/provenance/recovery tests | passed | Additive `0029` unchanged; crash-recovery class **17 passed** on 2026-08-20 including mismatched-invocation reservation; claim class **15 passed**; earlier full Postgres integration was included in the 1034 solution run |
| Architecture/module dependency tests | passed | Architecture **33 passed**; official SDK isolated to `FlexAgent.Sessions.OpenAi` with negative control |
| Sessions domain/application tests | passed | `FlexAgent.Sessions.Tests` **448 passed** including revoked-authorization reservation |
| Exact OpenAI-compatible profile qualification | deferred successor | No exact profile is selected. Deterministic migration may complete, but the adapter remains default-off and no compatible endpoint is qualified or enabled until a later bounded live run passes. OpenRouter remains a distinct evidence track |
| Locked regression, supply chain, OCI, docs, whitespace | partial | `python3 scripts/check_docs.py` passed; `git diff --check` passed. OCI image rebuild/SBOM/grype not re-run in this session |
| Independent backend/architecture/security review | approved for deterministic Phase A | Independent review of `4a6e314`: no P0/P1; `4373f70` P2s closed; leftover invocation-id coupling closed on 2026-08-20 by claimed-work binding. GitHub connector still has no combined status checks for that SHA. Phase B remains the completion gate |

# Blockers

- Deterministic migration work has no external blocker. Exact-profile live
  qualification is a deferred successor requiring an owner-selected
  OpenAI-compatible profile, approved synthetic-use credential delivered
  through the mounted-file secret boundary, applicable provider data-policy
  and destination-policy evidence, reachable infrastructure, and permission to
  incur bounded calls/cost. Those missing inputs block enablement and the
  successor qualification task, not migration completion.

# Completion

- [x] Planned work is reconciled with actual changes and the final predecessor baseline
- [ ] Only `openai_compatible` / `sessions.openai_compatible.v1` can enable the generic adapter; legacy Direct OpenAI identities remain historical and fail closed
- [ ] The adapter project/namespace/class, Worker composition/readiness, solution/OCI inputs, tests, locks, and qualification harness use vendor-neutral naming while any retained SDK stays isolated
- [ ] Exact base path and destination-policy identity are bound through a required adapter-configuration digest
- [ ] Public and explicitly allowed private endpoints pass DNS/rebinding-safe destination tests without redirects, unapproved proxies, TLS bypass, or client-supplied authority
- [x] Frozen profile, deployment-default, and Organization BYOK resolution fail closed with no cross-scope or silent fallback
- [x] Real Session provider/model/endpoint/capability and opaque credential-binding authority comes from the frozen trusted Session binding, not mutable Worker-global settings
- [x] Structured control, participant-visible streaming, usage/provenance, timeout, cancellation, retry, and failure normalization pass contract tests
- [x] Long provider calls preserve claim lease, current authority, lifecycle/cutoff, idempotency, and durable-before-display invariants
- [x] Required execution/manifest provenance is append-only, reconstructable, and free of raw sensitive provider material
- [-] Exact-profile live qualification is deferred to a successor and remains a mandatory enablement gate; deterministic migration evidence is not represented as qualification, and distinct OpenRouter evidence is not substituted
- [ ] Applicable focused, integration, concurrency, recovery, architecture, locked regression, supply-chain, OCI, documentation, and whitespace checks pass
- [x] Governing specifications and implementation-status tables are rechecked and remain truthful
- [x] Full start-time immutable-model enforcement and the synthetic OpenRouter/vLLM portions of `GATE-STACK-PROVIDERS` remain explicitly recorded unless separately implemented and verified
- [x] Independent backend, architecture, and security/privacy findings for the deterministic Phase A admission/execution slice are resolved at `4a6e314`; leftover invocation-id coupling is closed by claimed-work binding
- [x] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
