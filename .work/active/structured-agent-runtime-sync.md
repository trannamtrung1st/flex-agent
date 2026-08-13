---
id: structured-agent-runtime-sync
status: in_progress
created: 2026-08-11
updated: 2026-08-13
---

# Goal

Bring every currently executable Flex Agent surface into conformance with the
approved structured Agent Invocation/Decision and one-lane next-timer contracts
before unrelated product work starts. Deliver a production-shaped, durable
Session runtime slice plus matching canonical contracts, synthetic-browser
behavior, Participant UI states, and repeatable evidence for trusted trigger
admission, exactly-one successful Decision or execution outcome, explicit
`no_action`, durable `emit_message`, and optional bounded timer replacement.

Completion is an implementation gate, not a contract-only milestone: DTOs,
schemas, or synthetic UI changes alone do not satisfy this task. The task is
complete only when all in-scope requirements are mapped to working code and
tests, the existing implementation surfaces no longer contradict the approved
documents, and independent backend, frontend, architecture, security/privacy,
and test reviews have no unresolved blocking findings.

# Governing sources

- `AGENTS.md` — authority by concern, isolation and audit invariants,
  specification-driven TDD, UI verification, security/privacy defaults, and
  tracked implementation workflow
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — Session meaning, frozen configuration, P0 scope,
  and provider-independent validation strategy
- `docs/requirements/features/resolved-session-configuration.md`
  - `REQ-RSC-46`, `AC-RSC-25`: trusted provider/credential binding and
    fail-closed no-fallback behavior required before any model work
  - `REQ-RSC-47`–`REQ-RSC-53`, `AC-RSC-26`, and `AC-RSC-27`: frozen
    Invocation/Decision policy, one-lane timer policy, disabled P0 capabilities,
    cohort consistency, and minimized manifest provenance
- `docs/requirements/features/session-text-lifecycle.md`
  - prerequisite Session invariants consumed by this slice: authoritative
    Participant message/Turn/response-slot identity, Session order and
    idempotency, active-time pause/cutoff, reconnect, terminalization,
    authorized history, and Evaluation-handoff eligibility. Implementing the
    new runtime must not bypass or falsely claim completion of the wider
    lifecycle requirements.
  - `REQ-SESS-55`–`REQ-SESS-60`, `AC-SESS-32`: durable incremental message
    publication required by `emit_message`
  - `REQ-SESS-61`–`REQ-SESS-70`, `AC-SESS-33`–`AC-SESS-37`: trusted
    triggers, Invocation identity, attempts/outcomes, exactly-one Decision,
    validation/effect separation, no-action, ordering, and bounded loops
  - `REQ-SESS-71`–`REQ-SESS-77`, `AC-SESS-38`–`AC-SESS-41`: optional
    Agent timer recommendation, independent validation, replacement semantics,
    active-time delay, firing, default cadence, and provenance
- `docs/ui-ux/text-session.md` — approved Participant-facing behavior,
  especially `UI-SESS-DEC-13` and `UI-SESS-DEC-14`
- `docs/ui-ux/design-system/README.md` and
  `docs/ui-ux/design-system/implementation-guide.md`, including the applicable
  accessibility, colors, typography, layout, density, interaction-states,
  motion, status, conversation, timeline, agent-presence, session-controls,
  empty/loading, and protected-content modules
- `docs/architecture/mvp-architecture.md` — modular-monolith ownership,
  PostgreSQL authority, provider adapters, durable work, audit/outbox,
  browser/API authority, and isolation boundaries
- `docs/architecture/session-runtime-contract.md` — `SESS-DEC-14`–
  `SESS-DEC-28` and the corresponding verification matrix
- `docs/architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md`
  — immutable configuration and manifest integrity
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`
  and `ADR-003-authorization-audit-persistence.md` — trusted scope,
  commit-time authorization, atomic audit, and outbox rules
- `docs/architecture/decisions/ADR-004-assessment-activation-baseline-and-atomicity.md`
  and `ADR-005-atomic-attempt-start-and-submission-binding.md` — cohort and
  Attempt/Session frozen-input boundaries
- `docs/architecture/decisions/ADR-009-mvp-session-evaluation-review-contracts.md`
  — Session event/evidence ownership and terminalization
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
  — .NET/PostgreSQL/React stack, module boundaries, contract-first delivery,
  and executable gates
- `docs/architecture/decisions/ADR-011-participant-visible-agent-response-streaming.md`
  — durable fragment identity, ordering, publication, replay, and completion
- `docs/architecture/decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md`
  and `ADR-013-agent-requested-next-timer-replacement.md` — approved decisions
  being implemented
- `docs/requirements/mvp-operational-defaults.md` — applicable latency,
  revocation, durability, availability, observability, and load-test gates;
  exact timer cadence values remain resolved policy rather than universal
  operational defaults
- `.work/active/postgres-authorization-configuration-foundation.md` and
  `.work/active/p0-activity-journey-frontend-realization.md` — completed
  persistence and synthetic-browser baselines this task must extend without
  overstating their production coverage

# Current repository baseline

- `main` is clean and synchronized with `origin/main` at `e8bb83a` at planning
  time; all earlier `.work/active/*.md` task records are completed.
- Canonical v1 Session contracts currently cover representative commands,
  state events, and SSE only. They do not model trusted triggers, Invocation,
  execution attempts/outcomes, Decision, validation/effect, no-action, timer
  recommendations, schedule revisions, or firing provenance.
- `session.agent.complete.v1` is emitted by the synthetic adapter but is absent
  from the current SSE schema enum. This existing drift must be corrected as
  part of contract parity, not preserved as an implicit exception.
- The synthetic browser adapter creates an Agent fragment/complete stream when
  an active Session SSE endpoint is read. It does not require a durable trusted
  trigger or model Invocation and cannot demonstrate no-action, Decision
  rejection, scheduling replacement, pause/resume timing, or restart recovery.
- `SessionPage.tsx` renders the synthetic stream but has no authoritative
  intentional-no-action resolution or timer-triggered Agent-work scenario. Raw
  timer and Decision data is already absent from Participant copy and must
  remain absent.
- The worker is an idle heartbeat loop. There is no durable Session work claim,
  provider-neutral model execution port, Invocation handler, Decision effect
  processor, or timer scheduler.
- PostgreSQL currently owns authorization/configuration-source, audit, and
  outbox foundations only. There are no Session, message fragment, Invocation,
  Decision, timer-lane, or runtime-work tables.
- There is no production Sessions module or complete resolved-Session
  configuration service. This task therefore includes the minimum production-
  shaped Session/configuration prerequisites needed to implement the approved
  contracts; it must not promote the synthetic adapter into domain authority.

# Scope

## In

- Maintain one traceability matrix from every in-scope requirement, acceptance
  criterion, UI decision, and architecture decision to code, migration,
  focused test, integration/runtime test, and observable UI evidence.
- Add canonical, versioned, provider-neutral JSON Schemas, fixtures, catalog
  entries, C# DTOs, TypeScript/browser mappings, safe OpenAPI projections, and
  compatibility/parity tests for the new runtime boundary. Internal protected
  records and browser-safe projections remain distinct.
- Add a `Sessions` module with framework-independent domain/application rules
  for trusted trigger admission, Invocation identity, bounded attempts,
  successful Decision versus execution outcome, independent Decision
  validation, idempotency/order, response-slot terminalization, and explicit
  no-domain-effect outcomes.
- Resolve and freeze the minimum behaviorally material Session runtime policy:
  allowed trigger and Decision types, deferred-capability denials, attempt and
  chain bounds, timer-lane enablement, default/minimum/maximum delay, cooldown,
  replacement/Invocation/Session budgets, schema/policy versions, and protected
  provenance references. Lower scopes may narrow but never widen it.
- Resolve the provider/model deployment and opaque Organization-scoped or
  deployment-default credential binding from trusted configuration before
  external model work. Missing, revoked, wrong-Organization, or provider-
  mismatched bindings fail closed without fallback; credentials never enter
  Session records, contracts, logs, metrics, fixtures, or browser artifacts.
- Add append-only, Organization/Activity/Participant/Attempt/Session-scoped
  PostgreSQL persistence for Session runtime state, ordered events, Participant
  and Agent-initiated Turns, response slots, Participant/Agent Messages, trusted
  trigger admission, Invocation, execution attempts/outcomes, Agent Decision,
  decision and schedule validation/effect, Agent Message fragments/completion,
  timer lane and schedule revisions, reconciliation records, lower-level
  provider-request provenance where required, durable work, manifest runtime
  append/seal references, audit, and outbox correlation.
- Enforce exact-once domain effects through transaction boundaries,
  idempotency keys, expected versions, authoritative Session sequence,
  composite scope constraints, immutable history, and bounded work claims.
- Add an application-owned model-execution port and deterministic fake adapter
  proving provider-neutral structured control followed by optional participant-
  visible content streaming. Provider requests/retries remain lower-level than
  the stable Invocation identity.
- Implement worker processing for durable Invocation work, bounded execution
  attempts, cancellation/late-result handling, Decision validation/effect, and
  ADR-011 fragment persistence/publication/replay. A failed execution records an
  outcome and never fabricates a Decision.
- Execute provider work only through a least-privilege, versioned service
  delegation that is reauthorized at admission, provider disclosure, Decision/
  effect commit, fragment commit, replay, and timer firing as applicable. Apply
  approved provider/egress allowlists, request/response limits, timeouts, and
  cancellation without letting an adapter select scope, payer, or authority.
- Classify boundaries exactly: malformed or incomplete structured output that
  cannot establish one valid Decision is an Invocation execution outcome, a
  well-formed prohibited Decision is a Decision rejection, and an accepted
  Decision whose effect fails is an effect failure. None may be rewritten as
  `no_action` or as another category for retry convenience.
- Implement the optional one-lane timer scheduler: default arm on `Active`,
  independently accepted/rejected/omitted recommendation, pending-event
  replacement or fired-event sole successor, expected revision, active Session
  time, pause/resume, due reauthorization, one trusted firing, default
  resumption, cutoff/terminal cancellation, restart recovery, and loop budgets.
  All scheduling calculations use database-authoritative UTC and Session order,
  never client, host-local, or provider time; named timezone facts remain
  available where a governing wall-clock boundary must be explained.
- Persist every admitted Invocation, but do not promote every rejected raw
  signal, worker poll, provider callback, or operational observation into the
  Session event stream. Retain only the bounded audit/operational outcome its
  governing policy requires.
- Extend the current synthetic API/scenario adapter to exercise the same
  observable contract deterministically for emit-message, no-action,
  rejected/failed Decision, accepted/rejected/omitted replacement, default
  cadence, duplicate/concurrent schedule change, pause/resume, and cutoff.
  Synthetic state remains test/development-only and explicitly non-authoritative.
- Update the current Text Session client to consume authoritative Agent
  queued/working/resolved projections, render no false Agent Message for
  no-action, show no false Participant Message for timer triggers, announce the
  resolution once without moving focus, and keep timer requests/revisions,
  rejection reasons, raw Decisions, and hidden reasoning out of Participant UI.
  A neutral persistent resolution appears only when frozen workflow policy
  requires it; Agent-initiated opening, closing, and timer messages use an
  authoritative Agent-initiated Turn and never invent Participant input.
- Complete an internal production-shaped end-to-end proof from an ADR-005-style
  committed readiness/binding fixture through Session activation, Participant
  and permitted Agent-initiated Invocations, pause/cutoff/terminalization,
  manifest runtime append and terminal seal, and eligible Evaluation handoff.
  The proof uses trusted test actors and immutable fixture sources; it does not
  claim the deferred production Campaign, authentication, or Evaluation UI.
- Add bounded operational instrumentation for admission/rejection, execution
  and effect outcomes, no-action, duplicate/stale/late work, fragment latency
  and integrity, backlog/claims, timer acceptance/rejection/drift/fire/cancel/
  expiry, budget exhaustion, cutoff attempts, and manifest/audit failures.
  Verify label cardinality and protected-data exclusion, applicable p95 targets,
  backpressure, recovery alerts, and Organization/Activity fairness.
- Apply the existing lifecycle, retention, backup/restore, lawful-unavailability,
  and export authorization rules to new protected records. Generic audit,
  telemetry, browser, and export surfaces must not gain raw prompts, Decisions,
  content, credentials, unrestricted identifiers, or cross-scope existence
  signals merely because the records now exist.
- Add observed red-green-refactor tests for every behavior change; focused
  domain/contract tests, real PostgreSQL integration/concurrency/restart tests,
  API/runtime tests, frontend component/state tests, and Playwright MCP
  accessibility/visual verification at desktop and narrow viewports.
- Reconcile implementation-status tables in authoritative docs only after the
  corresponding evidence exists. Change `Gap`/`implementation TBD` claims to a
  precise implemented or partial state; never use documentation edits to hide
  an unimplemented behavior.
- Run distinct implementation and review passes. Resolve every blocking
  architecture, backend, frontend, security/privacy, and QA finding, then rerun
  the affected focused and aggregate checks before marking the task completed.
- Treat this task as a repository work freeze: no unrelated feature or
  foundation item starts until this task is completed or the user explicitly
  changes the priority/scope.

## Out

- Voice, playback/interruption, silence triggers, Participant Session tools,
  general workflow triggers, Dynamic memory, shared Sessions, arbitrary or
  parallel timers, or Agent/Harness self-modification. Tests must prove these
  remain disabled in P0.
- A provider-specific public contract, production OpenAI/Azure deployment, live
  model credentials/calls, provider qualification, or `GATE-STACK-PROVIDERS`
  certification. The deterministic adapter validates the application boundary;
  each live provider remains a later qualified adapter.
- Production OIDC/Keycloak login, MFA, opaque application-session persistence,
  or complete public Participant/admin authorization delivery. Internal and
  integration entry points use trusted test actors; no browser-controlled
  identity or scope is introduced.
- The complete production Activity, Enrollment, Submission, Attempt,
  Evaluation, Review, Result, or Release implementation outside the minimum
  immutable references and Session prerequisites required by this runtime
  slice.
- A universal product timer duration chosen in code. Production timer values
  must come from the approved frozen policy; deterministic test/synthetic
  profiles may use explicit fixture values that make no product-default claim.
- Participant controls for viewing or editing timer timing, raw Invocations,
  Decisions, validation data, or runtime provenance.
- Commits, pushes, pull requests, deployments, or releases unless separately
  requested.

# Acceptance and verification mapping

| Obligation | Implementation surface | Planned verification |
| --- | --- | --- |
| Frozen policy and provenance (`REQ-RSC-47`–`REQ-RSC-53`, `AC-RSC-26`, `AC-RSC-27`) | Configuration resolver, resolved Session configuration/manifest references, Sessions policy value object, activation/session binding | Exact version/digest/reference reconstruction; lower-scope narrowing; drift rejection; disabled-capability matrix; timer enabled/disabled and bounded-policy tests |
| Provider/model authority (`REQ-RSC-46`, `AC-RSC-25`) | Trusted deployment and opaque credential-binding resolver, provider request context, worker preflight | Missing/revoked/wrong-Organization/provider-mismatched binding; no fallback payer/provider; credential absence from storage, DTOs, logs, telemetry, errors, fixtures, and artifacts |
| Trusted admission and identity (`REQ-SESS-61`, `REQ-SESS-62`, `REQ-SESS-67`, `REQ-SESS-68`, `SESS-DEC-14`, `SESS-DEC-15`, `SESS-DEC-20`) | Trusted trigger adapter, admission command, scoped repository, Session sequence/idempotency | Accepted Participant/opening/closing/timer trigger; unknown/fake/prohibited trigger; forged scope; duplicate/mismatch; stale/lifecycle/cutoff/budget; cross-Organization/Activity/Participant/Session isolation |
| Invocation execution (`REQ-SESS-63`, `REQ-SESS-69`, `SESS-DEC-16`, `SESS-DEC-21`) | Durable work row, claim/lease, Invocation handler, provider port/adapter, attempt/outcome and provider-request provenance | Exactly one Decision on success; timeout/unavailable/malformed/incomplete structured output with no fabricated Decision; bounded retry; cancellation/late result; crash at each commit boundary; lost response/lease; duplicate claim; attempt/chain/cooldown/Session budget exhaustion |
| Decision authority/effect (`REQ-SESS-64`–`REQ-SESS-67`, `REQ-SESS-70`, `SESS-DEC-17`–`SESS-DEC-19`, `SESS-DEC-23`) | Decision schema/parser, validator, response-slot/Agent-initiated outcome state machine, effect transaction, audit/outbox | Accepted versus schema-valid policy/payload/capability rejection; schema-invalid/parse-bound failure remains an Invocation outcome; Participant and non-Turn no-action terminalization; emit-message slot claim; accepted-effect failure distinct from execution failure; atomic failure injection; current-policy recheck; immutable recommendation/validation/effect history |
| Durable message streaming (`REQ-SESS-55`–`REQ-SESS-60`, `AC-SESS-32`, ADR-011) | Message/fragment/completion persistence, outbox/SSE projection and replay | Fragment order, duplicate/gap/mismatch, commit-before-publish, disconnect/replay, completion digest/length, partial failure, late/extra delta, multiple nodes, terminal transcript reconstruction |
| One-lane scheduling (`REQ-SESS-71`–`REQ-SESS-77`, `AC-SESS-38`–`AC-SESS-41`, `SESS-DEC-24`–`SESS-DEC-28`) | Timer request parser/validator, timer-lane aggregate, schedule revision rows, durable due-work scheduler | Default first arm; accepted/rejected/omitted request; primary-Decision independence; replace pending versus sole successor; expected-revision conflict; duplicate/concurrent response; one pending event/firing; restart; active-time pause/resume; cutoff/revocation/terminal cancellation; loop budgets |
| Participant presentation (`UI-SESS-DEC-13`, `UI-SESS-DEC-14`) | Browser-safe Session projection/SSE, synthetic scenarios, `SessionPage` working/resolved states | No synthetic Agent Message for no-action; no synthetic Participant Message for timer; one accessible announcement; no focus movement; real timer-triggered work/message only; raw control/timing data absent from DOM, storage, URL, logs, and screenshots |
| Runtime foundation and handoff (ADR-005, ADR-009, runtime-contract implementation gate) | Trusted readiness/Session fixture, lifecycle/order boundary, manifest appender/sealer, Evaluation-handoff eligibility projection | End-to-end readiness → Active → Participant and Agent-initiated work → pause/cutoff/terminal → sealed manifest → eligible handoff; manifest/audit/seal fault injection leaves honest recoverable state |
| Security/privacy and operations | Composite scope constraints, server-derived context, minimized records/logs/metrics, authorization/audit, bounded worker/scheduler, lifecycle/export controls | Guessed-ID and cross-scope query/cache/event/work/replay matrix; prompt/content cannot establish authority/timing; service authorization and revocation; current authorization for SSE/replay and the approved 60-second access-narrowing target; retention/export/backup and sensitive-data/log snapshots; timer storm/backpressure; database-time/clock-skew/restart fault injection; bounded metric labels; append-only history |
| Performance and observability | Metrics/traces/alerts, claim fairness, load and failure harnesses | Applicable admission/reconnect p95 objective; time-to-first durable fragment and commit-to-display latency; Organization/Activity fair claiming; backlog, scheduler drift, restart, provider slowness, and post-cutoff alerts without sensitive/high-cardinality labels |
| Repository and documentation consistency | Schema catalog, C#/TS/OpenAPI parity, module/architecture rules, docs status tables | Contract fixture/parity suites; architecture dependency tests; `check_docs.py`; no stale `implementation TBD` or false implementation claim; reviewer traceability audit |

# Plan

- [x] Inventory the approved deltas and current executable baseline. Confirm the
  clean `e8bb83a` starting point, identify every affected contract/module/host/
  migration/test/UI surface, and record the existing synthetic SSE-schema drift
  and missing production Session-runtime foundations in this task.
- [x] Build the executable traceability and threat-model matrix before feature
  code. For each mapped requirement, specify the authoritative aggregate,
  persisted record, transaction boundary, actor/service authorization,
  idempotency/order key, browser-safe projection, positive test, negative test,
  failure/restart case, and evidence location. Include protected assets,
  actors/service identities, entry points, provider/database/SSE boundaries,
  STRIDE and privacy misuse cases, lifecycle/retention/export treatment,
  preventive/detective/recovery controls, and residual risks. Promote any newly
  discovered product/architecture ambiguity before continuing.
- [x] Define and test the canonical contract tranche first. Add red schema,
  catalog, fixture, OpenAPI, C#, TypeScript, and compatibility tests for trusted
  triggers, Invocation, attempt/outcome, Decision, validation/effect,
  response-slot resolution, participant-visible work/message events, and timer
  request/schedule/firing records; then implement the minimum versioned
  contracts until parity is green. Keep protected internal envelopes separate
  from Participant-safe DTOs and fix the existing SSE event-type drift. Select
  and document one interoperable duration encoding, reject unsafe numeric/
  overflow representations, require zero-or-one timer request, and cover
  unknown versions/types and additive compatibility across JSON Schema, .NET,
  TypeScript, and OpenAPI. Preserve existing v1 meanings and fixtures; use the
  repository compatibility policy to choose a compatible addition or new
  version rather than silently redefining a shipped event.
- [x] Establish the new Sessions ownership boundary together with its first
  governed behavior: an immutable P0 runtime-capability policy kernel. This
  bounded tranche implements only the approved allow/deny boundary from
  `REQ-RSC-49`, `AC-RSC-24`, `AC-RSC-26`, `REQ-SESS-62`, `REQ-SESS-64`, and
  `REQ-SESS-69`. Full source-layer resolution/freezing, positive numeric bounds,
  Session lifecycle behavior, persistence, provider execution, scheduling,
  HTTP endpoints, worker handling, and UI behavior remain in later steps.
  - [x] Red — add the production and focused-test project shells at
    `src/Modules/Sessions/FlexAgent.Sessions/FlexAgent.Sessions.csproj` and
    `tests/Sessions/FlexAgent.Sessions.Tests/FlexAgent.Sessions.Tests.csproj`,
    include both in `FlexAgent.slnx`, generate their tracked lock files, and add
    focused policy tests before the production policy type exists. Run the
    focused project and record the expected missing-policy-type compile failure,
    not a missing-project/restore failure. The tests must prove
    that the P0 kernel permits only the approved text-Session trigger/Decision
    subset, treats the single system timer lane as optional, and explicitly
    denies voice/Interaction Controller, silence, tool-result/Participant-tool,
    arbitrary/parallel-timer, richer configurable-workflow, Dynamic-memory
    write/learning, and model-authorized Evaluation/Result/Release effects.
    Unknown or deferred capability identifiers must fail closed without adding
    speculative executable domain behavior for future features.
    Do not select timer durations, retry counts, or other unresolved numeric
    policy defaults in this tranche.
  - [x] Green — implement only the minimum strongly typed immutable domain
    policy needed to pass the red tests. Keep canonical deferred Decision
    branches representable at the wire boundary so current policy can reject
    them explicitly. Do not add placeholder markers, persistence/provider
    adapters, endpoints, worker handlers, or empty service registration.
  - [x] Guard 1 — add non-vacuous architecture rules for the real policy types:
    Sessions Domain must not depend on Application or Infrastructure; Sessions
    Domain/Application must not depend on ASP.NET Core, Npgsql, Dapper, shared
    PostgreSQL infrastructure, provider/telemetry SDKs, host assemblies,
    browser DTOs, or another feature module's `Infrastructure` namespace.
    Application-owned ports may depend on Domain, but no inverse dependency is
    permitted.
  - [x] Guard 2 — prove the architecture rules themselves detect violations by
    using bounded test-only negative-control fixtures for dependency direction,
    forbidden infrastructure/provider references, and browser/host authority.
    Assert the controls fail on those fixtures and pass on the production
    Sessions assembly; do not call a passing absence guard an observed red TDD
    phase.
  - [x] Guard 3 — preserve composition and contract boundaries: Sessions must
    not reference API or Worker; hosts may compose Sessions later but must not
    define reusable Session policy; exported canonical contracts retain no
    concrete provider-SDK types. This tranche does not ban the later governed
    terminal Evaluation-handoff seam or ordinary Session lifecycle transitions;
    it bans only model-authored or otherwise unapproved capability authority.
  - [x] Defer honestly — assign command-signature tests for server-derived
    ownership and rejection of browser/HTTP authority to the first real Session
    application-command tranche below. Assign concrete cross-module table-write,
    protected-repository scoping, and database-authoritative UTC/order checks to
    the PostgreSQL/repository tranches, where repositories, SQL, transaction
    coordinators, and time/order inputs make them non-vacuous. Do not claim
    those behaviors from this policy-only tranche.
  - [x] Refactor — centralize architecture-test assembly loading, forbidden
    dependency prefixes, negative-control helpers, and non-vacuity assertions
    without weakening existing IdentityAccess, Configuration, Contracts,
    CanonicalJson, API, or Worker guards. Keep each failure actionable and name
    the violated owner or dependency direction. Keep the one-assembly module
    shape approved by `STACK-DEC-17`; split it only after executable evidence
    shows namespace-level enforcement is insufficient.
  - [x] Verify — record red and green results with
    `dotnet test --project tests/Sessions/FlexAgent.Sessions.Tests/FlexAgent.Sessions.Tests.csproj -c Release`;
    run the architecture suite with
    `dotnet test --project tests/Architecture/FlexAgent.Architecture.Tests/FlexAgent.Architecture.Tests.csproj -c Release`;
    then run `bash build/scripts/verify-dotnet.sh`, `git diff --check`, and
    `python3 scripts/check_docs.py`. Record exact counts/results and recheck
    `GATE-STACK-MODULES`, `AR-DEC-1`, `AR-DEC-7`, `AR-DEC-12`, `AR-DEC-23`,
    `AR-DEC-24`, and `STACK-DEC-17` before marking this tranche complete.
- [x] Implement full frozen runtime-policy resolution using TDD, building on
  the P0 capability kernel above (domain-layer subset complete; Session/manifest
  bind commit deferred to persistence tranche). Resolve immutable Invocation/Decision/timer
  policy and protected provenance from approved source versions, enforce
  required positive bounds and disabled capabilities, prove
  lower-scope non-widening and cohort stability, and bind the resolved snapshot
  to the Session/manifest. Resolve model deployment and opaque credential-
  binding identity from trusted scope, fail closed without fallback when it is
  missing/revoked/mismatched, and keep credential material out of the module.
  Fail closed when required production policy values are absent; keep explicit
  fixture timing values test-only.
  - [x] Red — add focused tests for `Iso8601PositiveDuration`,
    `FrozenRuntimePolicyResolver`, and `ModelDeploymentCredentialBindingResolver`
    before production types exist; record compile failures for missing domain
    types (2026-08-12).
  - [x] Green — implement immutable `FrozenTextSessionRuntimePolicy`,
    `TimerLanePolicy`, `InvocationBounds`, source-layer merge with
    non-widening enforcement, stable `policy_digest` via `rsc-jcs-sha256-v1`
    canonical JSON, and opaque credential-binding resolution without fallback
    (`REQ-RSC-46`–`53`, `AC-RSC-25`–`27` at domain layer).
  - [x] Verify — `FlexAgent.Sessions.Tests` 101/101; architecture suite 21/21;
    aggregate .NET verification 332/332; `git diff --check` and
    `python3 scripts/check_docs.py` passed (2026-08-13). Session/manifest bind
    commit, PostgreSQL persistence, and application-command ownership tests
    remain in the next tranche.
- [x] Implement the framework-independent Sessions domain/application tranche
  with red-green-refactor tests: lifecycle eligibility, trusted trigger
  admission, stable Invocation identity, semantic decision opportunity versus
  provider attempts, exactly-one successful Decision or execution outcome,
  schema-invalid execution outcome versus well-formed Decision rejection versus
  accepted-effect failure, independent validation, Participant response-slot
  and non-Turn no-action terminalization, opening/closing/timer Agent-initiated
  Turn creation, emit-message slot claim, idempotency/order, stale/late handling,
  and positive loop/budget bounds. Build Invocation context only from the exact
  trusted Session binding, frozen configuration, permitted Submission/knowledge/
  memory-read references, and authoritative visible transcript; prove unrelated
  and model-authored control facts cannot enter the context/provenance channel.
  With the first application command, add signature/negative tests proving
  ownership comes from trusted application context rather than browser/HTTP
  DTOs and that client-supplied timestamps or sequence values cannot choose
  authoritative order.
  - [x] Red — add focused domain and application tests for `SessionRuntime`,
    `InvocationContextAssembler`, and `AdmitTrustedTriggerCommand` before
    production types exist. Record compile failures for missing domain/
    application types (2026-08-13).
  - [x] Green — implement the in-memory Session aggregate, decision/effect
    classification, trusted context assembly, and `AdmitTrustedTriggerHandler`
    that loads ownership from application context and authoritative UTC outside
    the command.
  - [x] Verify — `FlexAgent.Sessions.Tests` 156/156; architecture suite 21/21;
    aggregate .NET verification 387/387; `git diff --check` and
    `python3 scripts/check_docs.py` passed (2026-08-13). Durable PostgreSQL
    persistence, Session/manifest bind commit, and timer-lane schedule rows
    remain the next tranche. Consistency review (2026-08-13) added regression
    coverage and remediations; `FlexAgent.Sessions.Tests` 160/160.
- [x] Fix Sessions-domain retry/idempotency and clock invariants before
  PostgreSQL schema design (`REQ-SESS-11`, `REQ-SESS-62`, `REQ-SESS-67`,
  `REQ-SESS-68`, `AC-SESS-36`, `SESS-DEC-8`, `SESS-DEC-20`). Equivalent command
  retries must reconcile after a version bump; effect application must be
  duplicate-safe; participant admission must fingerprint the full bound tuple;
  every mutation must guard UTC (and reject clocks older than `LastCommittedAt`);
  invocation context must emit `memory_read_ref` when permitted memory refs exist.
  Do not design invocation/effect unique constraints until these semantics are
  explicit in the aggregate.
- [x] Make the Decision pipeline crash/recovery contract explicit before
  PostgreSQL (`REQ-SESS-64`, `REQ-SESS-67`, `SESS-DEC-17`, `SESS-DEC-18`,
  `SESS-DEC-20`). `CompleteInvocation` must resume `decision_recorded →
  validated → effect_applied|effect_failed|rejected` instead of treating
  `RecordDecision` as terminal. Version every durable stage mutation. Record
  that PostgreSQL commit time/order comes from the transaction, not worker
  `UtcNow`.
- [x] Harden Decision validation idempotency and payload identity before
  PostgreSQL (`SESS-DEC-8`, `SESS-DEC-17`, `SESS-DEC-20`). Accepted validation
  retries at unchanged Session state must reconcile without mutation; lifecycle
  change before effect appends a new validation revision and preserves history;
  same Decision IDs with a different canonical payload digest conflict.
- [x] Design the minimum additive PostgreSQL schema and run migration tests red
  before implementation. Add immutable/scoped Session runtime, event,
  Invocation, attempt/outcome, Decision, validation/effect, response-slot,
  message/fragment, timer-lane/revision, lower-level provider provenance where
  required, pause intervals/active-time facts, terminal intent/record,
  reconciliation, durable-work, and manifest runtime-reference records with
  composite ownership, database-authoritative UTC/order semantics, expected
  versions,
  constraints enforcing at most one current pending/claimed lane event and one
  effect, append-only protection, and audit/outbox correlation. Prove empty/
  repeat/upgrade/changed-script/transactional/concurrent migration safety. Add
  executable ownership guards for module-owned migration/table prefixes and
  prove Sessions code cannot write another module's tables directly.
  Persistence design constraints from approved in-memory review (`cd50439`):
  store `validated_against_session_version/sequence` separately from
  `validation_commit_session_version/sequence` (P2); store
  `decision_payload_digest_version` alongside `payload_digest` (P3, current
  format `v1`). Do not redesign the Decision pipeline.
  Red: architecture ownership test failed for missing `0005_session_runtime_schema.sql`
  (2026-08-13). Green: migration `0005` plus schema/constraint tests;
  `FlexAgent.Postgres.Integration.Tests` 43/43; architecture suite 24/24.
- [x] Patch Session runtime persistence invariants before repositories
  (`SESS-DEC-17`, `SESS-DEC-18`, participant isolation). Allow recorded
  Decisions that validation later rejects; bind invocation descendants to the
  full ownership+invocation tuple; make terminal effects one-way on the latest
  accepted revision; persist Decision/outcome commit sequence for hydration.
  Red: 5 schema tests failed against `0005` (request_tool CHECK, descendant
  ownership, stale/rejected/terminal effect updates, missing commit sequence).
  Green: additive `0006_harden_session_runtime_invariants.sql`; Postgres 48/48.
- [x] Harden 0006 empty-upgrade, validation/effect serialization, and
  mandatory commit sequences before repositories. Assert Session runtime
  tables are empty before 0006 repair (0005 was pre-production; do not
  fabricate history). Lock the invocation row for validation append and
  effect terminalization. Require positive commit sequences and
  effect-commit state that does not precede validation-commit state.
  Red: populated `0005` upgrade and missing Decision sequence failed until
  empty-table assertion and NOT NULL commit sequences landed. Green: empty
  `0005→0006` upgrade; populated `0005` fails closed; two-connection
  validation/effect lock; Postgres 52/53 with known concurrent-Grate flake
  passing on retry; architecture 24/24; Sessions 174/174.
- [x] Enforce Decision XOR ExecutionOutcome and validation→Decision FK
  before repositories (`SESS-DEC-16`). Reject the opposite artifact after
  the invocation lock; prove exactly one concurrent winner; bind
  `session_decision_validations` to the Decision row.
  Red: same-invocation Decision+outcome, validation without Decision, and
  concurrent Decision-vs-outcome allowed both rows (2026-08-13). Green:
  sequential XOR; validation FK 23503; two-connection exactly-one winner;
  schema/upgrade/concurrency 26/26; architecture 24/24; Sessions 174/174.
  External review approved `d7c2daf` and froze the Session runtime schema.
- [>] Implement scoped PostgreSQL repositories and transaction coordinators
  against real PostgreSQL 18 tests. Cover wrong Organization/Activity/
  Participant/Attempt/Session, forged ownership, guessed IDs, list/count leaks,
  duplicate/mismatched idempotency, commit-time revocation, concurrent
  admission/effect/replacement, injected audit/outbox failure, and immutable
  history. Cover lifecycle disposition, authorized reconstruction, generic-
  export exclusion, backup/restore reconstruction, and lawful unavailability.
  No general repository or unscoped lookup is permitted. Add non-vacuous
  reflection/signature tests requiring trusted Organization plus complete
  Activity/Participant/Attempt/Session ownership on every protected repository
  entry point, and reject client/host time or order as mutation authority.
- [ ] Add the model-execution port and deterministic fake provider through
  observed red-green-refactor. Prove structured control/content phase
  separation, bounded provider requests within one Invocation, cancellation,
  transient/permanent failure classification, malformed/incomplete/oversized
  control, cumulative-snapshot versus non-overlapping-delta normalization,
  duplicate/late completion, one- and multi-interaction profiles, no message
  content before accepted communication control, no partial JSON exposure, and
  absence of hidden reasoning in records or logs. Do not add a live provider
  or credential. Preflight the trusted deployment and opaque credential-binding
  identity, service delegation, egress allowlist, timeout, and payload limits;
  prove no missing/mismatched binding falls back to another provider or payer.
- [ ] Replace the worker heartbeat-only behavior with bounded durable-runtime
  processing while retaining health/readiness behavior. Claim Invocation work,
  reauthorize, execute attempts, record one Decision or execution outcome,
  validate current policy, apply an idempotent effect, renew/expire claims, and
  recover after process loss. Inject crashes after claim, provider return,
  Decision commit, effect/schedule commit, fragment commit, and before
  acknowledgement; prove lost-response reconciliation, backpressure, retry,
  shutdown, database-time/host-clock skew, and multi-worker contention.
- [ ] Implement ADR-011 as the `emit_message` effect seam required by this
  task: durable Agent Message/fragment/completion records, commit-before-publish,
  exact order and integrity checks, safe SSE projection, replay/gap recovery,
  finest-provider-granularity publication without application-added batching,
  cumulative-provider-event normalization, bounded rolling validation and
  backpressure, completion/terminalization, and no post-terminal content. Reuse
  one path for Participant, opening/closing, and timer-triggered Agent work.
- [ ] Implement the one-lane scheduler with model-based/domain tests first,
  then PostgreSQL/worker integration: default arm on `Active`, independent
  recommendation validation, replacement/sole-successor atomicity, expected
  revision and Session order, active-time pause/resume, due reauthorization,
  one trusted trigger/Invocation, default resumption, restart recovery,
  terminal/cutoff cancellation, and positive cooldown/replacement/Invocation/
  Session budgets. Prove there can never be two pending events or two effects
  from one due revision. Cover zero/negative/minimum/maximum/over-maximum delay,
  malformed complete Decision control, structurally valid policy rejection,
  accepted replacements coexisting with both `emit_message` and `no_action`,
  default successor after every timer-Invocation terminal outcome unless its
  successful Decision installed an accepted replacement,
  long-running timer Invocation, concurrent non-timer Decision replacement,
  claimed-work lease loss, scheduling-commit lost response, Session entry to
  `Active`, repeated pause/resume, and every terminal race. Preserve requested
  delay and accepted/rejected/superseded validation provenance separately from
  schedule states `Pending`, `Claimed`, `Fired`, `Cancelled`, `Superseded`, and
  `Expired`.
- [ ] Extend the non-production synthetic adapter to mirror the browser-safe
  semantics without becoming runtime authority. Add deterministic scenarios for
  Participant reply, Agent opening/closing, Participant and timer no-action,
  rejected Decision, accepted-effect failure, execution failure, default timer,
  accepted/rejected/omitted replacement, duplicate/concurrent revision,
  timer-triggered visible work, pause/resume, reconnect, and cutoff. Require a
  trusted synthetic trigger instead of generating work merely because SSE was
  read.
- [ ] Update the Text Session UI under the approved design-system modules using
  component/state TDD. Consume authoritative queued/working/resolved states,
  stop work honestly on no-action, announce once without focus movement, render
  no false transcript item, show persistent neutral resolution only when the
  authoritative workflow projection requires it, reuse Agent presence for
  opening/closing/timer-triggered work, and keep schedule/control/provenance
  details absent. Present a policy-rejected Decision only through the workflow's
  safe bounded failure or neutral suppressed state, never as provider failure or
  no-action. Cover loading, empty, populated, reconnect/replay, error/retry,
  permission loss, pause, terminal, bounded long-transcript rendering, reduced
  motion, long content, and narrow layout.
- [ ] Exercise the changed journey through real API/SSE interactions with
  Playwright MCP. Inspect accessibility snapshots and screenshots at desktop
  and narrow viewports; cover keyboard/focus, no-action, timer-triggered message
  and no-action, disconnect/replay, pause/resume, error, permission loss,
  terminal state, both themes, reduced motion, and 400-percent reflow. Iterate
  until functional, accessibility, privacy, and visual evidence is clean; store
  artifacts only under `.playwright-mcp/`.
- [ ] Complete the production-shaped internal end-to-end proof from committed
  readiness and immutable Session binding through `Active`, trusted Participant
  and Agent-initiated triggers, no-action/message effects, timer replacement,
  pause/cutoff/terminalization, manifest runtime append and terminal seal, and
  eligible Evaluation handoff. Inject audit/manifest/seal failures and prove an
  honest recoverable state with no post-cutoff publication or false handoff.
- [ ] Add and verify bounded observability and performance coverage: admission/
  rejection and effect categories, no-action, duplicate/stale/late work,
  fragment latency/integrity, backlog/claims, timer drift/outcomes/budgets,
  cutoff attempts, audit/manifest faults, alert recovery, and Organization/
  Activity fair claiming. Exercise applicable p95 objectives and load/
  backpressure profiles; prove labels and telemetry contain no sensitive or
  unbounded identifiers/content.
- [ ] Integrate focused and aggregate verification: contract/catalog/OpenAPI
  parity, architecture tests, Sessions domain tests, PostgreSQL 18 migration/
  isolation/concurrency/fault tests, API/worker runtime tests, locked web
  lint/type/unit/build/e2e, OCI/runtime checks, supply-chain/SBOM/license/secret
  checks, backup/restore and authorized-reconstruction exercises, and
  documentation validation. Add proportionate blocking CI coverage and record
  exact commands, counts, results, environments, and artifact paths here.
- [ ] Reconcile code against every mapped requirement and governing document.
  Update authoritative implementation-status/traceability tables only where
  evidence supports the new state; record any remaining unrelated production
  gates precisely and ensure no technical, product, or UI/UX document now
  contradicts implemented behavior.
- [ ] Run distinct architecture, backend, frontend, security/privacy, and QA
  review passes over the final change set. Resolve all blocking findings,
  rerun affected focused and full verification, confirm no unauthorized scope
  growth, and retain review evidence in this task without secrets or sensitive
  data.
- [ ] Complete the work-freeze gate: reconcile planned versus actual changes,
  confirm every in-scope row is implemented and verified rather than merely
  documented or simulated, mark only truthful partial/deferred production
  gates, update this task to `completed`, and obtain user confirmation before
  moving to an unrelated new item.

# Current state

Planning and baseline inventory are complete. The canonical contract tranche
and follow-up C# discriminated-union/wire-enum hardening are approved and frozen
at `8f0c046` (`d05db26` implementation review reference): 118 contract tests and
220 full .NET tests were recorded green by the retained parity task. The Sessions
P0 runtime-capability policy kernel tranche is **approved and frozen** at
`c3827d7` (remediation over `68ce3bd`): `P0TextSessionRuntimeCapabilityPolicy`
with focused allow/deny tests (39/39), Sessions architecture ownership guards
with dependency-direction negative controls (architecture suite 21/21), and
aggregate .NET verification at 270/270. External review recorded no blocking
findings; operational timer enablement and numeric bounds remain explicitly
deferred to frozen runtime-policy resolution.
The frozen runtime-policy domain tranche is **complete**. The Sessions
domain/application tranche is **approved** (`cd50439`) for retry/idempotency,
clock invariants, a resumable Decision pipeline, append-only validation
revisions, and canonical Decision payload identity. The PostgreSQL **schema**
tranche is **approved and frozen** at `d7c2daf`. Additive `0006` is the
pre-repository invariant repair. Do not add speculative schema constraints.
Admission persistence is **approved** at `5430f4e`. The P3 concurrency test
now waits on `pg_locks` until T2 is blocked by T1 before advancing
`last_committed_at`. Remaining repository coverage: participant no-action,
Decision/outcome XOR persist, validation hydrate, worker races, audit/outbox,
list/count leaks. Session/manifest bind commit, worker processing, synthetic
adapter scenarios, UI states, and end-to-end proof remain pending.

# Decisions

- Use one tracked task for the full synchronization effort and deliver it in
  ordered contract, domain, persistence, worker/scheduler, adapter/UI, and
  review tranches. This keeps one end-to-end completion gate while allowing
  each tranche to remain small enough for observed TDD and independent review.
- Start Sessions as one module assembly when the first governed behavior—the
  immutable P0 runtime-capability policy kernel—is implemented, matching
  ADR-010's minimum-useful-project rule. Do not introduce public assembly/
  namespace markers or empty composition abstractions solely for architecture
  tests. API and Worker wiring waits for a real application handler.
- Treat the focused policy tests, compile-time ownership guards, and later
  resolver/domain/persistence tests as complementary gates. This tranche proves
  the narrow P0 capability allow/deny kernel; the following frozen-policy and
  domain tranches must still prove source resolution, non-widening, fail-closed
  behavior, lifecycle validation, and effect enforcement. Test-only negative
  controls validate guard mechanics; passing absence checks alone do not claim
  behavioral acceptance.
- No new ADR is planned for this tranche. ADR-010, ADR-012, ADR-013, and the
  approved MVP architecture already decide module direction, provider
  neutrality, client/host authority limits, and deferred capability boundaries.
  Any implementation pressure that contradicts those decisions stops this
  tranche for architecture review rather than being recorded only in `.work/`.
- Do not treat the existing synthetic browser journey as proof of production
  Session behavior. It remains a presentation/e2e adapter; the Sessions module,
  PostgreSQL records, and worker own runtime authority.
- Add only the minimum Session/configuration prerequisites required to make the
  approved boundary executable. Unrelated production Campaign/Submission/
  Evaluation/Release and authentication work stays deferred and must not be
  pulled into this task accidentally.
- Keep a successful Decision and an infrastructure execution outcome mutually
  exclusive. Persist recommendation, validation, authoritative effect, and
  explicit no-domain-effect result separately; never reinterpret absence of a
  message as no-action.
- Treat output that cannot establish one schema-valid Decision—including a
  malformed complete `next_timer_request`—as an Invocation execution outcome.
  Only a schema-valid Decision can be independently accepted or
  rejected; failure after acceptance belongs to the effect, not the Invocation.
- Treat timer scheduling as one optional control on a successful Decision and
  validate it independently. An accepted request replaces the single pending
  event or becomes the fired timer Invocation's sole successor; it never
  appends a parallel timer.
- Use active Session time for relative delay. The authoritative scheduling-
  effect commit starts the delay, pause suspends remaining duration, and
  non-Active/revoked/completing/terminal state prevents firing or rearming.
- Require timer values and budgets in frozen production policy. Tests and
  synthetic scenarios declare their values explicitly; fixture numbers are not
  promoted into product defaults.
- Keep raw Invocation/Decision/timer records protected. Participant-visible
  projections expose only honest Agent working/resolved/message state required
  by the UI contract, never hidden reasoning or internal schedule details.
- Retry/idempotency semantics that PostgreSQL unique constraints must preserve:
  reconcile identical trigger identity + idempotency key + bound Turn/slot
  (and Participant message id) without requiring a matching expected version;
  conflict when any bound identity differs; treat `Applied`/`NoDomainEffect`/
  `EffectFailed` as terminal effect outcomes; derive Agent-initiated Turn/slot
  ids from the Invocation id. Authoritative clocks must be UTC and not precede
  `LastCommittedAt`.
- Decision recovery model (chosen over one opaque atomic-only API): durable
  stages are `decision_recorded` → validated → effect applied/failed or
  rejected-with-no-effect. `RecordDecision` stops re-reasoning but is not
  `CompleteInvocation`-terminal. `CompleteInvocation` resumes remaining stages
  and reconciles an identical completed Decision. `StoreValidation` and
  `FailEffect` increment Session version/`LastCommittedAt`. Happy-path
  `CompleteInvocation` remains one call (SESS-DEC-18 atomically-or-equivalently).
- PostgreSQL must take commit timestamps and Session order from the transaction
  (`clock_timestamp()` / `now()`), not from worker host `UtcNow`. The in-memory
  monotonic UTC guard is a stand-in until that persistence design lands.
- Validation is append-only and bound to the Session version/sequence it
  observed. An identical retry at that same authoritative state reconciles
  without mutation (SESS-DEC-20). If version/lifecycle changed before effect,
  a new validation revision is recorded against current state (SESS-DEC-17)
  and prior rows remain inspectable. Decision equivalence is `DecisionId +
  InvocationId + canonical payload digest`; same IDs with a different digest
  are `IdentityMismatch`. PostgreSQL stores `validated_against_session_version`
  / `sequence` separately from `validation_commit_session_version` / `sequence`
  plus `validation_committed_at`. Effect application may update only effect
  columns on the latest revision (`effect_commit_*`). Canonical digest format
  is persisted as `decision_payload_digest_version = v1`.
- `0005` is pre-production (no repositories, no real Session rows). `0006`
  refuses populated Session runtime tables rather than fabricating commit
  sequences. Empty `0005` databases upgrade; a database with any `session_*`
  row fails with a clear exception. Rewriting `0006` in place changes its
  Grate checksum: a database that already applied the previous `0006` must be
  recreated. Validation INSERT and effect UPDATE lock the invocation row
  `FOR UPDATE` before rechecking invariants. Decision/outcome commit sequences
  are required, positive, and must advance `last_session_sequence` atomically.
  After the same invocation lock, Decision and ExecutionOutcome are mutually
  exclusive (`SESS-DEC-16`). `session_decision_validations` FK the Decision
  ownership+invocation unique key. Repository retries should lock/read the
  existing Invocation artifact first: a duplicate INSERT with the original
  sequence can fail `last_session_sequence must advance` before `ON CONFLICT`.
- Sessions persistence lives in `FlexAgent.Sessions.Infrastructure` so the
  Sessions domain assembly stays Npgsql-free. Admission retries load the
  session head `FOR UPDATE`, then sample `clock_timestamp()`, then admit and
  persist so the snapshot and the clock describe the same serialized point.
  Cooldown rehydration uses immutable `session_invocations.admitted_at`
  (stamped once on INSERT); `last_committed_at` remains the last mutation.

# Findings / deviations

- Post-review contract fixes (2026-08-11): corrected ISO 8601 duration wire and
  semantic bounds; discriminated `AgentDecisionV1` branches; added
  `AgentInvocationExecutionOutcomeV1`; replaced dual ownership with scope-free
  `TrustedTriggerProvenanceV1` on Invocation; honest synthetic completion
  digests; separated protected runtime TypeScript to `internal-runtime.v1.ts`.
- Third review contract hardening (2026-08-11): discriminated Invocation
  lifecycle/terminal references; execution-attempt success/failure branches;
  execution-outcome category/reason pairings (removed `admission_rejected`);
  `DecisionValidationEffectV1` accepted/rejected/suppressed branches with
  `suppression_reason_category`; C#/TypeScript discriminated unions mirroring
  canonical `oneOf` (separate records/interfaces per branch).
- Existing implementation is intentionally synthetic and lacks a production
  Session module. Achieving full conformance for the approved change therefore
  requires a new durable vertical slice, not just editing current DTOs or
  `SessionPage.tsx`.
- Synthetic SSE previously emitted `session.agent.complete.v1` outside the SSE
  schema enum; that drift is corrected. Synthetic Agent work is still initiated
  by reading SSE rather than a trusted admitted trigger — a remaining adapter gap.
- Complete production OIDC, live provider qualification, and the surrounding
  Campaign/Attempt product journeys remain separate gates. Their absence must
  continue to be reported accurately, but it does not justify leaving the new
  Session domain/persistence/worker contracts synthetic-only.
- Second plan audit (2026-08-11) found and closed missing plan coverage for
  scoped provider/credential no-fallback behavior, malformed-output versus
  Decision/effect classification, non-Turn no-action and opening/closing
  triggers, database-authoritative time, manifest append/seal and Evaluation
  handoff, lifecycle/export controls, crash-boundary recovery, and bounded
  operational/performance evidence.
- Readiness review (2026-08-12) replaced the marker-only Sessions scaffold with
  a real P0 capability-policy behavior, limited the observed red claim to the
  missing policy type, added test-only negative controls for architecture-rule
  mechanics, preserved the governed Evaluation-handoff seam, and moved
  repository/cross-table/database-time assertions to the persistence tranche
  where they can be non-vacuous.
- Consistency review (2026-08-12): aligned deferred-capability coverage with
  `AC-RSC-24` by adding `shared_session`; added positive `text_interaction`
  and unknown-decision fail-closed tests; tightened Guard 1 with a Domain-
  scoped dependency rule alongside the assembly-wide rule; standardized policy
  tests on shared trigger/decision identifier constants. Agent opening/closing
  trigger types (`workflow_event.agent_opening/closing`) remain kernel-level
  identifiers pending canonical fixture addition in the frozen-policy tranche.
- External review remediation (2026-08-12): replaced vacuous Application/
  Infrastructure namespace-absence guard with Domain → Application/Infrastructure
  dependency-direction rules plus bounded negative controls; renamed P0 policy
  APIs from operational `Permitted`/`TimerLaneAvailability` semantics to
  `SupportedByP0`/`SupportsOptionalTimerLane`/`IsTimerTriggerSupportedByP0`,
  separating P0 capability ceiling from frozen timer enablement under
  `REQ-RSC-51`–`53`.
- External review approval (2026-08-12, `c3827d7`): P1 Domain→Application/
  Infrastructure dependency-direction guards and bounded negative controls
  accepted; P2 `SupportedByP0`/`SupportsOptionalTimerLane`/`IsTimerTriggerSupportedByP0`
  separation accepted; no blocking findings. Optional follow-up: negative
  controls could invoke the exact production `ShouldNot().HaveDependencyOn(...)`
  rule for stronger symmetry (non-blocking).
- Frozen runtime-policy domain tranche (2026-08-12): added
  `FrozenTextSessionRuntimePolicy`, `FrozenRuntimePolicyResolver`,
  `ModelDeploymentCredentialBindingResolver`, and `Iso8601PositiveDuration` in
  `FlexAgent.Sessions.Domain`. Lower-scope narrowing, baseline digest drift,
  P0-ceiling enforcement, stable policy digest, and credential no-fallback are
  covered at the pure-domain layer; Session/manifest bind and persistence are
  explicitly deferred to the next tranche.
- Review remediation (2026-08-12): deployment-default credential path now rejects
  provider mismatch (`REQ-RSC-46`); timer-lane permitted decision types are
  validated against the P0 kernel; `policy_digest` payload includes
  `domain_key: agent_invocation_policy`; added tests for min-delay widening,
  timer-lane deferred decisions, digest format, and deployment-default provider
  mismatch.
- Second review remediation (2026-08-12): frozen policy snapshots now copy
  collections via `ImmutableArray`; resolver verifies canonical baseline-content
  digest; P0 kernel owns required explicitly-disabled capability set; supported
  contract versions, active-time clock basis, and decision validation schema
  bindings are enforced; unknown scope kinds fail closed; duration parsing is
  overflow-safe.
- Third review remediation (2026-08-12): `REQ-RSC-52` timer narrowing now covers
  permitted stages/decisions and timer budgets; partial Organization or
  deployment-default credential bindings fail closed with `BindingIncomplete`
  instead of silently falling back.
- Fourth review remediation (2026-08-13): agent-initiated communication and
  no-action policy flags are required during baseline validation, merged-value
  validation, and baseline-content digest computation; missing values fail
  closed with `InvalidPolicyValues` instead of coercing to `false`. Shorter
  timer `DefaultDelay` is treated as widening (increased cadence) per
  `REQ-RSC-52`; lengthening within inherited min/max remains permitted.
- Sessions domain/application tranche (2026-08-13): in-memory `SessionRuntime`
  admits trusted Participant/opening/closing/timer triggers, classifies
  malformed control as an Invocation execution outcome, records well-formed
  prohibited Decisions as independent rejections, and treats accepted-effect
  slot-claim failure as distinct from execution failure. `no_action`
  terminalizes a Participant slot without an Agent Message; Agent-initiated
  `emit_message` creates an opening/closing/timer Turn. Invocation context
  accepts only the trusted binding plus visible transcript refs.
  `AdmitTrustedTriggerCommand` carries actor and complete ownership, omits
  client timestamps/sequences, and requires server-loaded `SessionRuntime` plus
  authoritative UTC. Timer *schedule revision rows* remain the later scheduler
  tranche; this tranche only independently validates an optional
  `next_timer_request`.
- Consistency review (2026-08-13): failed participant admission no longer
  leaves an orphaned Turn; Agent-initiated `emit_message` allocates runtime
  Turn/slot IDs and ignores model-supplied identities; Decision
  `InvocationId` must match the target Invocation; offered transcript facts
  must already exist on the Session; pause rejection uses `state_ineligible`
  rather than `cutoff_exceeded`; protected message refs use a SHA-256 digest of
  the reference identity instead of a constant placeholder.
- Retry/idempotency review (2026-08-13): identical `AdmitTrustedTriggerCommand`
  retries reconcile after the version bump; `ApplyDecisionEffect` reconciles
  terminal effect outcomes and uses deterministic Agent-initiated Turn/slot
  ids; Participant admission fingerprints message/turn/slot/trigger/idempotency
  and conflicts without creating an orphan Turn; completion/lifecycle mutations
  reject non-UTC and clocks older than `LastCommittedAt`; `memory_read_ref` is
  emitted when permitted memory refs are bound.
- Decision pipeline recovery (2026-08-13): `RecordDecision` sets
  `decision_recorded` (not terminal). `CompleteInvocation` resumes validation
  and effect, and an identical completed Decision reconciles instead of
  `AlreadyTerminal`. Validation and effect-failure mutations now version the
  Session. PostgreSQL clock/order authority is recorded as a persistence
  constraint, not implemented in this in-memory tranche.
- Validation/payload hardening (2026-08-13): accepted `NotAttempted`
  validation retries at unchanged Session version/sequence do not mutate;
  lifecycle change before effect appends a rejected revision and keeps the
  accepted row; Decision payload digest is required for equivalent retry.
  External review approved `cd50439` and directed PostgreSQL to distinguish
  observed vs commit versions and to version the digest format.
- PostgreSQL schema (2026-08-13): `0005_session_runtime_schema.sql` adds
  `session_*` tables only. Child rows FK the full ownership tuple to
  `uq_session_runtimes_ownership`. Activity/Participant/Attempt identifiers
  still have no module parent FKs until those tables exist. Message/fragment
  bytes are not stored in PostgreSQL; fragments require `session_sequence`
  plus optional turn/slot/generation-attempt identity. Dedicated
  `session_reconciliations` and generation-attempt provenance tables remain
  deferred.   Hydrate in-memory `ValidatedAtSessionVersion` from
  `validation_commit_*`. Mutable Session tables reject DELETE.
- PostgreSQL invariant patch (2026-08-13): additive `0006` (does not edit
  `0005`). Drops the P0 `decision_type` CHECK so `request_tool` and other
  recorded-then-rejected Decisions persist; invocation descendants FK the
  full ownership+invocation unique key; terminal effects are one-way on the
  latest accepted revision; Decision/outcome commit sequence and invocation
  `last_session_sequence` exist for hydration. Empty-upgrade fail-closed
  (no fabricated sequences); invocation `FOR UPDATE` serializes validation
  append vs effect terminalization; commit sequences are mandatory and
  positive, and effect commit state cannot precede validation commit state.
  Decision XOR ExecutionOutcome is enforced after the invocation lock;
  validations FK the Decision row rather than only the Invocation.
- Repository review remediation (2026-08-13, `126efbd`): admission now locks
  the session row before sampling `clock_timestamp()`, so a waiter cannot
  evaluate T1's new `last_committed_at` with a pre-wait clock (`StaleClock`).
  Additive `0007` introduces immutable `session_invocations.admitted_at`
  (INSERT-only stamp; preserved on UPDATE). Cooldown rehydration uses
  `MAX(admitted_at)` per trigger family. Frozen `0005`/`0006` were not edited.
  External review **approved** `5430f4e` with no blocking findings (P3:
  wait on `pg_locks` rather than a fixed delay before T1's timestamp bump).

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Baseline/current branch inspection | passed | Work began from clean `main...origin/main` at `e8bb83a`; reviewed current implementation baseline is synchronized `main...origin/main` at `8f0c046`, with only this tracked task-plan update pending |
| Approved requirement/ADR/UI ID discovery | passed | `rg` across `docs/` confirms `REQ-RSC-47`–`53`, `REQ-SESS-61`–`77`, `SESS-DEC-14`–`28`, and `UI-SESS-DEC-13`–`14` are authoritative and currently marked implementation TBD/Gap where applicable |
| Current contract/module/migration/test/UI inventory | passed | Repository inventory confirms representative Session contracts plus synthetic adapter/UI, with no production Sessions module, runtime persistence, provider port, or scheduler |
| Cross-cutting plan review | passed | Backend, frontend, security/privacy, and tester checklists applied to requirements, ADR-011–ADR-013, runtime contract, UI decisions, and current implementation boundary; omissions recorded above were incorporated into scope, mapping, steps, and completion gates |
| Plan formatting and documentation validation | passed | `git diff --no-index --check /dev/null .work/active/structured-agent-runtime-sync.md` produced no whitespace diagnostics (exit `1` denotes the expected new-file difference); `python3 scripts/check_docs.py` passed |
| Executable traceability/threat-model review | passed | `.work/active/structured-agent-runtime-traceability.md` — requirement matrix, STRIDE controls, module ownership, duration encoding |
| Contract/catalog/C#/TypeScript/OpenAPI compatibility | passed for frozen contract tranche | Retained `csharp-contract-union-parity` evidence: contract tests 118/118 and full .NET 220/220; earlier web verification passed; runtime-policy and domain behavior remain outside this completed contract evidence |
| Sessions capability-policy red/green and ownership guards | passed; approved | Red: `dotnet build tests/Sessions/FlexAgent.Sessions.Tests/...` failed with 14 compile errors for missing `FlexAgent.Sessions.Domain` policy types (2026-08-12). Green after review remediation at `c3827d7`: `FlexAgent.Sessions.Tests` 39/39; `FlexAgent.Architecture.Tests` 21/21 including Domain→Application/Infrastructure dependency rules with bounded negative controls; aggregate verification 270/270; `git diff --check` and `python3 scripts/check_docs.py` passed. External review: approve, no blocking findings (2026-08-12). Tranche frozen at `c3827d7`. |
| Frozen runtime-policy domain resolution (`REQ-RSC-46`–`53`) | passed; domain layer | Red: compile failures for missing resolver/policy types (2026-08-12). Green after fourth review remediation: required communication/no-action policy fail-closed (no `null`→`false` coercion), baseline digest requires explicit flags, shorter `DefaultDelay` widening rejected; `FlexAgent.Sessions.Tests` 101/101; aggregate .NET 332/332 (2026-08-13). Session/manifest bind commit and PostgreSQL persistence remain next tranche. |
| Sessions domain/application focused tests | passed; in-memory aggregate | Red: compile failures for missing `SessionRuntime`, `AdmitTrustedTriggerCommand`, and related domain types (2026-08-13). Green: admission, exactly-one Decision vs execution outcome, no-action/emit-message effects, context isolation, and command-signature tests. Consistency review remediations (orphaned-turn rollback, model-supplied turn IDs ignored, invocation identity match, transcript-fact allowlist): `FlexAgent.Sessions.Tests` 160/160; architecture suite 21/21 (2026-08-13). Persistence, worker, and scheduler remain next. |
| Sessions retry/idempotency/clock remediations | passed; in-memory aggregate | Red (2026-08-13): focused tests failed for stale-version retry, duplicate effect, mismatched participant tuple, non-UTC/stale completion clock, missing `memory_read_ref`, and revalidation resetting a terminal effect. Green: `FlexAgent.Sessions.Tests` 168/168; architecture suite 21/21; `git diff --check` clean. PostgreSQL schema remains next and must encode these reconcile/conflict/effect identities. |
| Decision pipeline crash/recovery contract | passed; in-memory aggregate | Red: resume-after-`RecordDecision` and identical-`CompleteInvocation` retry failed (`decision_recorded` vs `decided`, `AlreadyTerminal`). Green: `FlexAgent.Sessions.Tests` 171/171; architecture suite 21/21; `git diff --check` clean (2026-08-13). Confirmation pass also rejects execution-failure completion once a Decision is recorded. |
| Decision validation idempotency and payload digest | passed; in-memory aggregate | Red: unchanged-state `ValidateDecision` retried bumped version; lifecycle-change overwrite left one validation row; same Decision IDs with `EmitMessage` payload reconciled as NoAction. Green: `FlexAgent.Sessions.Tests` 174/174; architecture suite 21/21; `git diff --check` clean (2026-08-13). |
| PostgreSQL Session runtime schema (`0005`) | passed; schema/migration | Red: `SessionsPersistenceOwnershipTests` failed with missing `*_session_runtime*.sql`. Green: `FlexAgent.Postgres.Integration.Tests` 43/43 including 10 schema constraint tests (ownership FK, delete reject, fragment `session_sequence`); `FlexAgent.Architecture.Tests` 24/24; `FlexAgent.Sessions.Tests` 174/174; `git diff --check` and `python3 scripts/check_docs.py` passed (2026-08-13). Repositories remain next. |
| PostgreSQL Session runtime invariant patch (`0006`) | passed; schema/migration | Includes Decision XOR ExecutionOutcome (`SESS-DEC-16`) and validation→Decision FK. Red: both child rows and validation-without-Decision succeeded. Green: sequential XOR; concurrent exactly-one winner; schema/upgrade/concurrency 26/26; architecture 24/24; Sessions 174/174 (2026-08-13). Repositories remain next. |
| PostgreSQL 18 repository isolation/concurrency/fault tests | in progress; admission slice **approved** `5430f4e` | External review: approve, no blocking production findings. P3 hardening: wait until `pg_locks` shows T2 blocked by T1 before advancing `last_committed_at`. Remaining: participant no-action pipeline, Decision/outcome XOR persist, validation observed-vs-commit hydrate, concurrent worker races, audit/outbox, list/count leak tests. |
| API/worker/provider/scheduler runtime tests | pending | |
| Provider credential/no-fallback and manifest append/seal/handoff tests | pending | |
| Web lint/type/unit/build/e2e | pending | |
| Playwright accessibility/responsive/visual evaluation | pending | |
| Aggregate `.NET`, web, OCI, supply-chain, secret, and docs verification | pending | |
| Performance, observability, lifecycle/export, backup/restore verification | pending | |
| Architecture/backend/frontend/security/privacy/QA review | pending | |
| Final specification and repository consistency audit | pending | |

# Blockers

None at planning time. Exact production timer durations are intentionally
policy inputs, not an unresolved implementation blocker.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] Every in-scope requirement/acceptance/decision maps to implemented code
      and repeatable evidence
- [ ] No contract-only or synthetic-only gap remains for the approved runtime
      boundary
- [ ] Applicable focused tests pass
- [ ] Applicable PostgreSQL concurrency, restart, isolation, and fault tests pass
- [ ] Scoped provider/credential no-fallback behavior and credential non-
      disclosure are verified
- [ ] Manifest runtime append/seal, terminal recovery, and eligible Evaluation
      handoff are verified end to end
- [ ] Applicable integration/regression and full repository checks pass
- [ ] Applicable latency/backpressure, bounded observability, lifecycle/export,
      and backup/restore checks pass
- [ ] Playwright accessibility and visual verification passes for affected UI
      states
- [ ] Governing product, requirements, technical, architecture, and UI/UX
      specifications were rechecked and implementation status is truthful
- [ ] Independent review findings are resolved and reverified
- [ ] Remaining unrelated production gates or unverified behavior are recorded
- [ ] Task state is safe and complete for external review and retained tracking
- [ ] No unrelated new item starts before this completion gate or explicit user
      reprioritization
