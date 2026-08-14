---
id: structured-agent-runtime-sync
status: in-progress
created: 2026-08-11
updated: 2026-08-14
---

# Goal

Bring every currently executable Flex Agent surface into conformance with the
approved structured Agent Invocation/Decision and one-lane next-timer contracts
before unrelated product work starts. Deliver a production-shaped, durable
Session runtime slice plus matching canonical contracts, synthetic-browser
behavior, Participant UI states, and repeatable evidence for trusted trigger
admission, exactly-one successful Decision envelope or execution outcome, explicit
`no_action`, durable P0 `message` output, historical v1 reconstruction, and
optional bounded timer replacement.

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
- `.work/resources/multi-channel-agent-output-proposal.md` and
  `.work/active/multi-channel-agent-output-contract-adoption.md` — user-
  prioritized proposal and the prerequisite cross-concern decision task that
  must resolve the future output/action shape before provider and worker seams
  are implemented; neither source enables voice in P0
- `docs/requirements/features/resolved-session-configuration.md`
  - `REQ-RSC-46`, `AC-RSC-25`: trusted provider/credential binding and
    fail-closed no-fallback behavior required before any model work
  - `REQ-RSC-47`–`REQ-RSC-53`, `AC-RSC-26`, and `AC-RSC-27`: frozen
    Invocation/Decision policy, one-lane timer policy, disabled P0 capabilities,
    cohort consistency, and minimized manifest provenance
  - `REQ-RSC-54`–`REQ-RSC-55`, `AC-RSC-28`: frozen P0 output and
    requested-action kinds plus historical v1 reconstruction identity
- `docs/requirements/features/session-text-lifecycle.md`
  - prerequisite Session invariants consumed by this slice: authoritative
    Participant message/Turn/response-slot identity, Session order and
    idempotency, active-time pause/cutoff, reconnect, terminalization,
    authorized history, and Evaluation-handoff eligibility. Implementing the
    new runtime must not bypass or falsely claim completion of the wider
    lifecycle requirements.
  - `REQ-SESS-55`–`REQ-SESS-60`, `AC-SESS-32`: durable incremental message
    publication required by the P0 `message` output (historical `emit_message`)
  - `REQ-SESS-61`–`REQ-SESS-70`, `AC-SESS-33`–`AC-SESS-37`: trusted
    triggers, Invocation identity, attempts/outcomes, exactly-one Decision,
    validation/effect separation, no-action, ordering, and bounded loops
  - `REQ-SESS-71`–`REQ-SESS-77`, `AC-SESS-38`–`AC-SESS-41`: optional
    Agent timer recommendation, independent validation, replacement semantics,
    active-time delay, firing, default cadence, and provenance
  - `REQ-SESS-78`–`REQ-SESS-85`, `AC-SESS-42`–`AC-SESS-48`: P0 Decision
    envelope, independent output/action validation and partial rejection,
    output cardinality, runtime-owned output identity, audience derivation,
    Evidence/Evaluation exclusion, and v1 reconstruction
- `docs/ui-ux/text-session.md` — approved Participant-facing behavior,
  especially `UI-SESS-DEC-13`, `UI-SESS-DEC-14`, and `UI-SESS-DEC-15`
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
- `docs/architecture/decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md`
  — approved P0 Decision envelope, output identity, visibility derivation, and
  historical v1 reconstruction; does not enable voice
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
- Honor the 2026-08-14 multi-channel adoption result: consume the approved
  ADR-014 P0 envelope before the model-execution port. Keep v1
  `agent-decision.v1` immutable and reconstructable; introduce the successor
  envelope schema/profile before provider and worker seams harden Decision
  shape. Voice, playback, Interaction Controller, rich-content rendering, and
  later-release audience experiences remain out of this implementation task.

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
| Frozen policy and provenance (`REQ-RSC-47`–`REQ-RSC-55`, `AC-RSC-26`–`AC-RSC-28`) | Configuration resolver, resolved Session configuration/manifest references, Sessions policy value object, activation/session binding | Exact version/digest/reference reconstruction; lower-scope narrowing; drift rejection; disabled-capability matrix including voice/extra outputs; timer enabled/disabled and bounded-policy tests |
| Provider/model authority (`REQ-RSC-46`, `AC-RSC-25`) | Trusted deployment and opaque credential-binding resolver, provider request context, worker preflight | Missing/revoked/wrong-Organization/provider-mismatched binding; no fallback payer/provider; credential absence from storage, DTOs, logs, telemetry, errors, fixtures, and artifacts |
| Trusted admission and identity (`REQ-SESS-61`, `REQ-SESS-62`, `REQ-SESS-67`, `REQ-SESS-68`, `SESS-DEC-14`, `SESS-DEC-15`, `SESS-DEC-20`) | Trusted trigger adapter, admission command, scoped repository, Session sequence/idempotency | Accepted Participant/opening/closing/timer trigger; unknown/fake/prohibited trigger; forged scope; duplicate/mismatch; stale/lifecycle/cutoff/budget; cross-Organization/Activity/Participant/Session isolation |
| Invocation execution (`REQ-SESS-63`, `REQ-SESS-69`, `SESS-DEC-16`, `SESS-DEC-21`) | Durable work row, claim/lease, Invocation handler, provider port/adapter, attempt/outcome and provider-request provenance | Exactly one Decision on success; timeout/unavailable/malformed/incomplete structured output with no fabricated Decision; bounded retry; cancellation/late result; crash at each commit boundary; lost response/lease; duplicate claim; attempt/chain/cooldown/Session budget exhaustion |
| Decision authority/effect (`REQ-SESS-64`–`REQ-SESS-67`, `REQ-SESS-70`, `REQ-SESS-78`–`REQ-SESS-85`, `AC-SESS-42`–`AC-SESS-48`, `SESS-DEC-17`–`SESS-DEC-19`, `SESS-DEC-23`, `SESS-DEC-29`–`SESS-DEC-35`) | Successor Decision envelope schema/parser, validator, response-slot/Agent-initiated outcome state machine, effect transaction, audit/outbox, v1 dual-read | Exactly-one envelope; empty collections not inferred as `no_action`; schema-invalid output is an execution outcome with no Decision; disposition is Decision-level; outputs and requested actions validate/effect independently (partial rejection; mixed valid message + prohibited voice); 0–1 accepted message / 0 accepted voice; runtime-owned output ids; model audience/id fail-closed; Evidence/Evaluation not a message; Participant and non-Turn no-action terminalization; message output slot claim; accepted-effect failure distinct from execution failure; independent timer-action validation; v1 `emit_message`/`no_action` reconstruction; atomic failure injection; current-policy recheck; immutable recommendation/validation/effect history |
| Durable message streaming (`REQ-SESS-55`–`REQ-SESS-60`, `AC-SESS-32`, ADR-011) | Message/fragment/completion persistence, outbox/SSE projection and replay | Fragment order, duplicate/gap/mismatch, commit-before-publish, disconnect/replay, completion digest/length, partial failure, late/extra delta, multiple nodes, terminal transcript reconstruction |
| One-lane scheduling (`REQ-SESS-71`–`REQ-SESS-77`, `AC-SESS-38`–`AC-SESS-41`, `SESS-DEC-24`–`SESS-DEC-28`) | Timer request parser/validator, timer-lane aggregate, schedule revision rows, durable due-work scheduler | Default first arm; accepted/rejected/omitted request; primary-Decision independence; replace pending versus sole successor; expected-revision conflict; duplicate/concurrent response; one pending event/firing; restart; active-time pause/resume; cutoff/revocation/terminal cancellation; loop budgets |
| Participant presentation (`UI-SESS-DEC-13`, `UI-SESS-DEC-14`, `UI-SESS-DEC-15`) | Browser-safe Session projection/SSE, synthetic scenarios, `SessionPage` working/resolved states | No synthetic Agent Message for no-action; no synthetic Participant Message for timer; envelope/output-id/audience/requested-action internals absent; one accessible announcement; no focus movement; real timer-triggered work/message only; raw control/timing data absent from DOM, storage, URL, logs, and screenshots |
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
- [x] Implement scoped PostgreSQL repositories and transaction coordinators
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
  External review **approved** `0a40324` (no remaining P0/P1/P2). Dirty-only
  Turn persist, conditional UPSERT, `created_session_sequence` order, mixed
  agent/participant sequence uniqueness, completion audit/outbox rollback, and
  execution-failure `AlreadyTerminal` ack semantics are in. Commit-time
  revocation, timer replacement races, and export/backup/restore/lawful
  unavailability wait on those later surfaces and do not block this slice.
  P3 nit: `0008` header still says "UTC-ordered"; the column is Session-
  sequence ordered. Left unchanged to avoid retouching an applied one-time
  Grate checksum; noted here only.
- [x] Complete and review
  `.work/active/multi-channel-agent-output-contract-adoption.md`, then amend
  this task and its traceability matrix with the approved P0-compatible output
  model before starting provider, worker, or Agent-message streaming code.
  Preserve frozen historical contracts and applied migration checksums; if an
  approved replacement contract or persistence shape is required, use explicit
  versioning and additive migration. Voice, playback, Interaction Controller,
  rich-content rendering, and later-release audience experiences remain out of
  this implementation task.
- [x] Add the model-execution port and deterministic fake provider through
  observed red-green-refactor. Consume the ADR-014 successor Decision envelope
  and P0 profile (0–1 accepted `message` output, 0 accepted `voice`, explicit
  disposition, runtime-owned output ids, independent output and requested-action
  validation with partial rejection). The successor envelope must parse typed
  `voice` as a schema-valid output that then fails frozen P0 profile validation;
  do not omit `voice` from the schema so parse rejects the Decision. Dual-read
  historical v1 `emit_message`/`no_action`. Prove structured control/content
  phase separation, bounded provider requests within one Invocation,
  cancellation, transient/permanent failure classification,
  malformed/incomplete/oversized control, empty-output inference rejection,
  schema-invalid execution outcome versus Decision rejection, mixed valid
  message plus prohibited voice, prohibited voice/audience/id item rejection
  without voiding valid siblings, cumulative-snapshot versus non-overlapping-delta
  normalization, duplicate/late completion, one- and multi-interaction
  profiles, no message content before accepted communication control, no
  partial JSON exposure, and absence of hidden reasoning in records or logs. Do
  not add a live provider or credential. Preflight the trusted deployment and
  opaque credential-binding identity, service delegation, egress allowlist,
  timeout, and payload limits; prove no missing/mismatched binding falls back to
  another provider or payer.
  - [x] Successor `agent-decision.v2` schema, catalog, fixtures, C# DTOs,
    TypeScript `internal-runtime.v2.ts`, and lossless v1 dual-read mapping.
    Typed `voice` is schema-valid; unknown kinds and hidden-reasoning properties
    fail envelope parse.
  - [x] Domain `EnvelopeRecommendation`, parser, historical mapper, and P0
    independent item validation (message accept + voice/extra/audience/id
    reject; empty `respond` is Decision rejection not `no_action`; `no_action`
    rejects every presentation output independently and never publishes;
    runtime allocates `aout.*` only for accepted messages). Envelope
    `payload_ref` and output `references`/`payload_ref` are retained and
    included in the recommendation digest. P0 validates local references before
    message cardinality: missing/ambiguous refs are `payload_invalid`, any
    named sibling is `policy_prohibited` because P0 cannot accept a second
    presentation output. The first runtime-ordered permitted message can still
    be accepted after an earlier invalid sibling. `aout.*` is allocated only
    for that accepted message.
  - [x] Application `IModelExecutionPort`, deterministic fake adapter, and
    credential-binding preflight that fails closed without fallback or a live
    provider.
  - [ ] Worker-owned claim, egress allowlist, attempt timeout, and
    cumulative-snapshot versus non-overlapping-delta content-phase remain for
    the worker and ADR-011 steps; do not treat this slice as streaming complete.
- [x] Close the two hard worker-claim gates before any worker consumes the
  model-execution port. Do not start worker claim, live provider wiring, or
  durable v2 Decision writes until both are green.
  - [x] Exact `agent-decision.v2` schema validation at the model-execution
    boundary (parser/port parity with every invalid v2 catalog fixture).
    Schema-invalid provider output must be an Invocation execution outcome and
    create no Agent Decision (`SESS-DEC-31` layer 1 / ADR-014).
  - [x] Additive PostgreSQL migration for the v2 envelope plus per-item
    output/action validation and effect/absence (`SESS-DEC-35`). Do not rewrite
    applied migrations `0005`–`0008`.
- [x] Remediate `d5b740c` P1s before worker claim. Do not start worker claim,
  live provider wiring, or ADR-011 until both are green.
  - [x] Make `agent-decision.v2` schema admission structurally unavoidable on
    `IModelExecutionPort`: structured control cannot be constructed from a
    typed envelope that skipped the canonical schema boundary (`SESS-DEC-31`).
    A `message` plus `payload_ref` must not become worker-consumable control.
  - [x] Persist per-item effect as append-only facts separate from item
    validation (`SESS-DEC-35`). Staged validate → persist → effect → persist
    → reload must reconstruct Decision `applied`, accepted message `applied`,
    and rejected voice `not_attempted`. Do not rewrite `0005`–`0009`.
- [x] Remediate `737cb69` P1 before worker claim. Do not start worker claim,
  live provider wiring, or ADR-011 until this integrity pass is green.
  Do not rewrite applied migration `0010`.
  - [x] Additive `0011`: bind output/action item-effect facts to the complete
    ownership tuple and only `accepted` validation items (`SESS-DEC-35`).
    Negative tests: wrong `activity_id` / `participant_id` / `attempt_id`;
    `rejected` item plus `applied` effect. Retain staged validate → persist →
    apply → persist → reload.
- [x] Replace the worker heartbeat-only behavior with bounded durable-runtime
  processing while retaining health/readiness behavior. Keep live provider
  wiring and ADR-011 streaming publication as subsequent separate steps.
  - [x] Application `DurableInvocationWorkProcessor`: claim → fake
    `IModelExecutionPort` → exactly one Decision or execution outcome →
    complete or reconcile redelivery. Schema-invalid control and missing
    credential binding fail closed with no Decision. Admitted Invocation ids
    are schema-stable `ainv.*`.
  - [x] Admission persists pending `invocation.execute` durable work. The
    worker loop invokes `IDurableInvocationWorkProcessor` while the claim gate
    allows; health/readiness remain. Production claim/lease against PostgreSQL
    is the next slice (host currently registers an idle processor when no
    claim store is wired). Post-claim release uses a bounded cleanup token so
    shutdown cannot leave work claimed because the worker token is already
    cancelled. Worker OCI COPY includes Sessions, CanonicalJson, and the
    embedded Decision schema files.
  - [x] PostgreSQL claim/lease infrastructure proof: `SKIP LOCKED`, database
    `clock_timestamp()` leases, expire/reclaim, release-to-pending, and CAS on
    the returned lease timestamp. Integration tests prove admit → fake port →
    one Decision → completed work using the insert-time trusted binding.
    Worker host stays idle until frozen-policy rehydration and an executable
    model port exist (`c2fa693` P1). Snapshot load uses PostgreSQL
    `REPEATABLE READ` so child-table reads cannot mix with a later Session
    version. Do not treat host `ConnectionStrings:Sessions` as an execution
    path.
  - [x] Inject crashes after claim, provider return, Decision commit,
    and before acknowledgement; prove lost-response reconciliation,
    retry, claimed-work recovery after a processing throw, database-time
    versus host-clock skew, multi-worker contention, and an unprocessable
    oldest item that does not monopolize later pending work.
    Effect/schedule and fragment-commit injection remain with ADR-011 and
    the scheduler steps.
- [ ] Implement ADR-011 as the P0 `message` output effect seam required by this
  runtime: durable Agent Message/fragment/completion records, commit-before-publish,
  exact order and integrity checks, safe SSE projection, replay/gap recovery,
  finest-provider-granularity publication without application-added batching,
  cumulative-provider-event normalization, bounded rolling validation and
  backpressure, completion/terminalization, and no post-terminal content. Link
  fragments to the driving Decision and runtime-owned output id. Do not treat
  the stream as voice playback. Reuse one path for Participant, opening/closing,
  and timer-triggered Agent work.
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

External review of `a6aba5e` (2026-08-14) **approved: 0 P0 / 0 P1. CI green.**
Push-triggered GitHub Actions on `main`: Documentation #123 (~19s) and
Implementation #92 (~6m 31s). The `d5b740c` / `737cb69` remediation chain is
closed. **Worker claim against the model-execution port is unblocked.**

Current work is complete for the crash/recovery tranche of durable worker
execution. Next remaining `[ ]` item is ADR-011 streaming publication. Do
**not** fold live provider wiring or frozen-policy rehydration into that
step unless the user reprioritizes. Voice stays disabled. Frozen `0005`–
`0011` stay frozen. Worker host stays idle until frozen-policy rehydration
and an executable model port exist.

- External review of `4a483a7` (2026-08-14) **approved: 0 P0 / 0 P1 / 0 P2 /
  1 P3 documentation cleanup.** Repeatable Read snapshot and idle host P1s
  from `c2fa693` are closed. Queue monopolization on retry is covered by the
  crash/recovery fault-injection tests.

- P1: Effect FKs now bind the complete Organization → Activity → Participant →
  Attempt → Session chain plus invocation/revision/item, and can reference only
  an `accepted` validation item (`validation_outcome` column defaults to
  `accepted` and is CHECK-constrained). Wrong ownership or `rejected + applied`
  is a foreign-key violation. Staged validate → persist → apply → persist →
  reload is retained.

- P1 #1: `ModelExecutionStructuredControl` now requires a
  `ValidatedAgentDecisionEnvelope` that can only be admitted from UTF-8 through
  the canonical schema reader. `EnqueueEnvelope` serializes then uses the same
  JSON admission path. A typed `message` plus `payload_ref` is
  `malformed_control`, not structured control.
- P1 #2: Additive `0010` stores terminal per-item effects as append-only facts.
  Validation child `effect_outcome` stays `not_attempted` on insert (0009
  dual-read if no effect row). Hydrate restores parent effect without remapping
  items. Duplicate Decision/outcome inserts use `WHERE NOT EXISTS` so a later
  effect persist does not re-fire the last-sequence bump trigger. Staged
  validate → persist → apply → persist → reload reconstructs Decision
  `applied`, accepted message `applied`, and rejected voice `not_attempted`.

External review of `39883c2` (2026-08-14) **approves the local-reference
remediation slice: 0 P0, 0 P1.** Push-triggered GitHub Actions on `main`
passed for that SHA: Documentation #120 (~12s) and Implementation #89
(~6m 30s). An empty PR-connector result means no PR-triggered runs were
visible through that tool, not that CI did not run.

The multi-channel output decision gate is **cleared**. The successor Decision
envelope (`agent-decision.v2`) and deterministic model-execution port exist:
dual-read of historical v1, P0 independent item validation, schema admission at
the port, additive `0009`/`0010` persistence, and a scripted fake adapter with
fail-closed credential preflight. Do not enable voice or rewrite applied
migrations `0005`–`0009`. Next executable work is worker durable Invocation
claim after re-review of this remediation pass. ADR-011 fragment publication
and the one-lane scheduler wait after worker is unblocked.

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
`last_committed_at`. The repository **completion/isolation/order** slice is
**approved** at `0a40324`: dirty-only Turns, conditional UPSERT, immutable
`created_session_sequence`, mixed agent-opening then participant uniqueness,
completion audit/outbox rollback, and execution-failure `AlreadyTerminal` as
a safe worker ack. `0008` backfill is best-effort; no production Session
upgrade path. The output-contract decision gate is cleared; the successor
envelope and deterministic fake port are in. Worker-claim gates (exact v2
schema validation and additive v2/per-item persistence) are in. Commit-time
revocation, timer replacement races, and export/backup/restore wait on later
surfaces.

# Decisions

- Exact `agent-decision.v2` validation at the model-execution boundary uses
  JsonSchema.Net Draft 2020-12 against the embedded canonical schema. The
  handwritten Domain parser remains a typed mapper after schema success and
  is not schema-complete. Schema-invalid provider JSON is an Invocation
  execution outcome (`malformed_control`) and creates no Agent Decision.
- Additive `0009` stores v2 `envelope_json` plus per-item output/action
  validation and effect/absence. Applied `0005`–`0008` are not rewritten.
  Historical v1 Decision rows keep flattened columns and null `envelope_json`.
- The output-contract decision is resolved by ADR-014: one Decision envelope
  with a P0 profile of zero or one accepted `message` output, explicit
  `no_action`, independent next-timer requested action, and reconstructable
  historical v1 `emit_message`/`no_action`. Outputs and requested actions are
  validated and effected independently (partial rejection); schema-invalid
  output is an Invocation execution outcome with no Decision. The successor
  schema must represent typed `voice` so P0 denial is profile/capability
  validation of that item (`AC-SESS-48`), not envelope parse. Provider, worker,
  and message-stream code must consume that envelope rather than treating v1
  `decision_type` as the future shape.
- P0 `voice` is a recognized envelope kind with local-reference metadata and
  an optional bounded opaque payload reference. Speech text, TTS, audio format,
  playback, and Interaction Controller timing remain P2 and must not be
  designed in this schema.
- Schema-valid `no_action` plus presentation outputs is not an execution
  outcome. ADR-014 layer 3 rejects each presentation output independently
  (`policy_prohibited`); the Decision remains an accepted `no_action` when
  policy permits. Effect never claims the publication path for that
  disposition. Empty `respond` remains Decision-level communication rejection.
- The domain envelope must retain every semantically kept v2 field used for
  recommendation identity, including envelope `payload_ref` and output
  `references`/`payload_ref`. Changing any of those fields must change
  `DecisionRecommendationDigestComputer` output.
- P0 validates output `references` independently before message cardinality
  (`SESS-DEC-30`, `SESS-DEC-32`, `REQ-SESS-81`). Unresolved/missing, self, or
  ambiguous local refs are `payload_invalid`. Any named same-Decision sibling
  is `policy_prohibited` in P0 because at most one presentation output can be
  accepted; useful cross-output resolution waits for the later multi-channel
  profile. A later valid message is still eligible after an earlier invalid
  sibling. Neither rejected item allocates `aout.*`.
- Keep this task's release boundary unchanged:
  text-only P0 remains the executable target, and tests must continue to reject
  voice, playback/Interaction Controller signals, unapproved audiences, tools,
  richer workflow effects, and other deferred capabilities. Architectural
  preparation is not capability enablement.
- Do not rewrite frozen or applied `0005`-`0008` migration scripts or silently
  redefine `agent-decision.v1`. Any approved evolution uses a compatible
  profile, an explicitly versioned successor, and additive migration with
  historical reconstruction tests.
- Use one tracked task for the full P0 runtime synchronization implementation
  and deliver it in ordered contract, domain, persistence, worker/scheduler,
  adapter/UI, and review tranches. The multi-channel adoption task is the
  completed P0 envelope decision boundary, not a split implementation record
  for this same runtime work. This keeps one P0
  implementation completion gate while allowing each tranche to remain small
  enough for observed TDD and independent review.
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
- Before the multi-channel proposal, no new ADR was planned for this runtime
  task: ADR-010, ADR-012, ADR-013, and the approved MVP architecture decide its
  module direction, provider neutrality, client/host authority limits, and
  deferred-capability boundaries. The prerequisite adoption task must now
  determine whether an extending/superseding ADR is required. Any resulting
  ADR must be approved before its P0 consequences enter this task; unresolved
  architecture pressure remains a blocker rather than a `.work/`-only choice.
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
- Persist only dirty Turns. UPSERT conflict updates run only when `state`,
  `response_slot_state`, or `claimed_by_invocation_id` actually change, so
  historical `last_committed_at` is not restamped. Additive `0008` stores
  immutable `created_session_sequence` (preserved on UPDATE) and loads
  `ORDER BY created_session_sequence`. Participant Turns stamp
  `NextAdmissionSequence()` (`SessionSequence + 1`, the sequence admission is
  about to claim). Agent-initiated Turns stamp `ClaimedSessionSequence()`
  after `RecordDecision` has already incremented. `0008` backfill from
  `last_committed_at` is best-effort for empty/test databases; it does not
  reconstruct pre-0008 creation order, and no production Session runtime rows
  are expected to exist before this script.
- Execution-failure completion after a terminal Invocation returns
  `AlreadyTerminal` with `Succeeded=false`. That is a **safe acknowledgement**
  for at-least-once workers (do not retry). Exact Decision retries instead
  reconcile as `Succeeded=true` when Decision identity and payload digest
  match. Worker claim/ack mapping must treat this code as terminal success
  from a delivery perspective, not as a retryable failure.
- Durable Invocation work recovery uses PostgreSQL `clock_timestamp()` leases,
  `SKIP LOCKED` claiming, CAS on the returned `claim_lease_until`, and
  `last_committed_at` restamp on every work-row update (including release to
  pending). A processing throw leaves work claimed until database-time reclaim.
  A committed Decision with a lost acknowledgement reconciles as
  `AlreadyTerminal` without a second provider call. An unprocessable oldest
  pending row is released with a newer `last_committed_at` so it cannot starve
  later work. Effect/schedule and fragment-commit injection wait on those
  surfaces.

# Findings / deviations

- External review of `38f1375` (2026-08-14): **request changes**, 1 P1.
  Post-claim `ReleaseToPendingAsync` must not use the already-cancelled worker
  token; use a bounded cleanup token. In-memory store now honors cancellation
  on release/complete so that path cannot be hidden. Lease recovery after a
  processing throw is covered by the crash/recovery tranche. Cancellation
  during load/execute still leaves the claim until database-time reclaim.
- External review of `a6aba5e` (2026-08-14): **approved**, 0 P0 / 0 P1. CI green
  (Documentation #123, Implementation #92). Worker claim unblocked. Live
  provider wiring and ADR-011 remain subsequent separate steps. Non-blocking
  P2 from `737cb69`: admitted `EnvelopeRecommendation` collections are
  `IReadOnlyList`, not deeply immutable.
- External review of `737cb69` (2026-08-14): **request changes**, 0 P0 / 1 P1.
  Push-triggered GitHub Actions on `main` were independently green
  (Documentation #122, Implementation #91). The `d5b740c` schema-admission and
  staged per-item persistence P1s are accepted. Remaining P1: `0010` effect FKs
  omitted Activity/Participant/Attempt and allowed `rejected + applied`.
  Remediation: additive `0011` (do not rewrite `0010`). Non-blocking P2:
  admitted `EnvelopeRecommendation` collections are `IReadOnlyList`, not deeply
  immutable.
- External review of `d5b740c` (2026-08-14): **request changes**, 0 P0 / 2 P1.
  Push-triggered GitHub Actions on `main` were independently green
  (Documentation #121, Implementation #90). Remediation this pass: schema
  admission is unavoidable on `IModelExecutionPort` via
  `ValidatedAgentDecisionEnvelope`; additive `0010` append-only item-effect
  facts plus `WHERE NOT EXISTS` Decision/outcome insert so staged effect
  persist can reconstruct per-item outcomes. Frozen `0005`–`0009` unchanged.
- Worker-claim gates (2026-08-14): exact `agent-decision.v2` schema validation
  at `AgentDecisionEnvelopeReader` (JsonSchema.Net Draft 2020-12, embedded
  canonical schema + primitives) plus additive `0009`. Domain parser remains a
  typed mapper and is not schema authority. Domain types do not reference
  `Json.Schema`. Per-item effect: accepted Participant `message` follows the
  Decision effect; rejected items and requested actions record explicit
  `not_attempted` until a later channel/scheduler seam exists. Duplicate
  output/action `local_ref` values are `payload_invalid` and persist by
  `item_ordinal`, not a UNIQUE `local_ref` constraint.
- External review of `39883c2` (2026-08-14): **approved** for the
  local-reference remediation slice (0 P0, 0 P1). Push-triggered GitHub
  Actions on `main`: Documentation #120 passed; Implementation #89 passed.
  Empty PR-connector results are not “no CI.”
- External review of `7e1bc83` (2026-08-14): 0 P0, 1 P1 — a reference-invalid
  first message blocked a later valid message because cardinality ran before
  refs. Fixed in `39883c2`: refs validate first; first permitted message is
  accepted (`SESS-DEC-30`).
- External review of `b83aaaa` (2026-08-14): improved, **not approved for
  worker integration**. Prior P1 #1/`no_action` publication and P1 #3 digest
  losslessness accepted. Remaining slice P1: local refs were retained but not
  resolved before effect — fixed in `7e1bc83` (`SESS-DEC-32`, `REQ-SESS-81`,
  missing `local_ref` and P0-rejected voice sibling). The two worker gates
  (exact v2 schema validation; additive v2/per-item persistence) remain. GitHub
  combined status was not independently corroborated for that SHA.
- External review of `bba3a20` (2026-08-14): **not approved**, 0 P0 / 4 P1.
  #1 `no_action` could accept and publish a Participant message — fixed this
  slice (`AC-SESS-42`/`SESS-DEC-18`/`SESS-DEC-30`). #3 parser/digest dropped
  envelope `payload_ref` and output `references`/`payload_ref` — fixed this
  slice. #2 handwritten parser is not schema-complete; #4 PostgreSQL flattens
  v2 and drops per-item validation on reload. #2 and #4 stay separate steps
  and **block worker claim**. GitHub combined status was not independently
  corroborated for that SHA.
- Multi-channel proposal impact (2026-08-14): the proposal aligns with the
  existing Invocation/Decision authority boundary, control/content separation,
  explicit non-message outcomes, future Interaction Controller ownership, and
  playback-confirmed continuity. It is not merely a voice adapter: coordinated
  output cardinality, output/action separation, independent visibility,
  per-output delivery state, cross-output references, rich-message evolution,
  and partial-channel failure may change the provider, effect, event, and
  persistence seams. The dedicated adoption task now owns those unapproved
  decisions and all proposal scenarios/questions before this task resumes.
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
- Repository completion/isolation slice (2026-08-13): participant admission
  persists Turn/transcript with the Invocation; `no_action` terminalizes the
  slot without an Agent transcript item. Completion persist writes Decision
  XOR ExecutionOutcome, attempts, validation revisions, and effect columns.
  Hydration binds in-memory `ValidatedAtSessionVersion` from
  `validation_commit_*` so unchanged-state validation retries do not mutate.
  Concurrent completions serialize on `LoadForUpdate`; the waiter reconciles
  or returns `AlreadyTerminal`. Admission and completion write audit+outbox
  in the same transaction; injected writer failures roll back runtime rows.
  `CountInvocationsAsync`/`ListInvocationIdsAsync` require the complete
  ownership tuple and return empty/zero for the wrong Participant or guessed
  Session. Commit-time revocation, timer replacement races, and
  export/backup/restore remain deferred until those surfaces exist.
- Consistency review (2026-08-13, completion/isolation slice): hydrate now
  restores the stored Decision `payload_digest` so sub-microsecond
  `produced_at` round-trips still reconcile (`SESS-DEC-20`). Missing actor
  and command/binding ownership failures use `invocation_completion.denied`
  / `ownership_mismatch` instead of `already_terminal` / `identity_mismatch`,
  matching admission. Added persist-then-retry and handler negative tests.
- Persistence-fidelity follow-up (2026-08-13, `6c89609` review): unconditional
  historical Turn UPSERTs restamped `last_committed_at`; load ordered by
  `turn_id` reversed non-lexicographic creation order. Fixed with dirty-only
  persist, conditional `DO UPDATE`, and `created_session_sequence`. Completion
  audit-fault rollback now covers Decision/validation/attempts/slot/head/
  outbox. `AlreadyTerminal` on execution-failure retry is documented as a
  safe worker ack rather than Decision-style payload reconciliation.
- Sequence collision review (2026-08-14, `8f374d6`): participant Turns stamped
  current `SessionSequence` collided with agent-opening Turns stamped after
  `RecordDecision`/`Touch()`. Participant Turns now use the admission sequence
  about to be claimed. `0008` backfill limitation from restamped
  `last_committed_at` is documented; no production Session upgrade path.
  External review **approved** `0a40324` with no remaining P0/P1/P2. P3:
  `0008` still says "UTC-ordered"; the column is Session-sequence ordered.
  Not edited in place (applied one-time Grate script).
- External review of `38a20a0` (2026-08-14): approved with no runtime blocker.
  P3 sensitive-data wording now covers every tracked `.work/` file, including
  `resources/`. P0 `voice` representation is kind + local refs + optional opaque
  payload; speech/TTS/playback remain P2.
- Model-execution port slice (2026-08-14): added `agent-decision.v2` with
  disposition/outputs/requested_actions; dual-read maps v1 `emit_message` to
  `respond`+one message and v1 `no_action` to explicit `no_action`+zero outputs.
  Domain validation accepts the first Participant message, rejects voice/extra
  messages/prohibited audience/model-authored output ids per item, and does not
  infer `no_action` from empty `respond`. The deterministic fake parses v2 JSON
  or returns scripted envelopes; missing/mismatched credential bindings fail
  closed as `credential_binding_failed` and never select another payer. No live
  provider. Content streaming remains later. Worker claim is blocked until
  schema-parity validation and additive v2 persistence land.
- Review remediation of `bba3a20` (2026-08-14): `no_action` plus a valid
  message now rejects the message (`policy_prohibited`), records accepted
  `no_action`, and does not claim publication. Parser retains envelope
  `payload_ref` and output `references`; digest covers those fields plus
  output `payload_ref`.
- Review remediation of `b83aaaa` (2026-08-14): same-Decision local references
  resolve after item validation. Missing/ambiguous refs reject the message as
  `payload_invalid`; refs to a P0-rejected voice sibling reject it as
  `policy_prohibited`. Neither allocates `aout.*` nor publishes.
- Review remediation of `7e1bc83` (2026-08-14): reference validation now runs
  before extra-message cardinality. A later valid message is accepted after a
  missing-ref or voice-ref sibling; exactly one `aout.*` is allocated.
  External review **approved** that slice at `39883c2` (0 P0, 0 P1) with
  push-triggered Documentation #120 and Implementation #89 green.
- Durable worker crash/recovery tranche (2026-08-14): fault-injection tests
  prove the existing PostgreSQL claim/lease path without a production behavior
  change. A throw after claim or provider return leaves work claimed until
  `clock_timestamp()` reclaim; a throw during Decision save records no
  Decision and retries; a throw after Decision commit reconciles without a
  second provider call; stale-lease CAS cannot complete a reclaimed row;
  host-clock remaining lease time does not block database-time reclaim;
  concurrent workers complete independent sessions; an older unprocessable
  `invocation.execute` row is released with a restamped `last_committed_at`
  and does not monopolize later pending work. Concurrent workers share
  trusted bindings and Invocation-keyed fake ports so a global `SKIP LOCKED`
  claim cannot fail closed on the sibling Session. Effect/schedule and fragment
  injection remain with ADR-011 and the scheduler. Companion traceability no
  longer lists per-item validation persistence as pending (`0010`/`0011`
  already landed).

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
| PostgreSQL 18 repository isolation/concurrency/fault tests | passed; **approved** `0a40324` | Dirty-only Turn persist; conditional UPSERT; `created_session_sequence` order; opening `emit_message` then participant reply unique (2, 3); completion audit-fault rollback; `AlreadyTerminal` documented as safe worker ack; `0008` backfill scoped as best-effort. `FlexAgent.Sessions.Tests` 183/183; architecture 27/27; Postgres 76/76 with known concurrent-empty Grate `pg_type` flake that passed on retry. Deferred until later surfaces: commit-time revocation, timer replacement races, export/backup/restore/lawful unavailability. |
| Multi-channel output decision gate | passed | ADR-014 plus product v0.4, RSC v0.4, Session v0.5, UI Session v0.5, runtime contract v0.5, and MVP architecture v0.10 approve the P0 envelope; this plan and the companion matrix were amended 2026-08-14. Voice remains out of scope. |
| Successor Decision envelope and dual-read (`SESS-DEC-31`, `REQ-SESS-80`, `AC-SESS-46`) | passed; contract+domain | Red: catalog/schema tests required `agent-decision.v2` before the schema existed. Green: contract tests 134/134 including v1 dual-read, schema-valid `voice`, opaque voice `payload_ref`, and rejected voice `communication_purpose`; Sessions 200/200 including mixed message+voice, extra message, empty `respond`, model-authored id/audience, hidden-reasoning parse, fake port, and no-fallback preflight; architecture 27/27; Postgres 75/76 with known concurrent-empty Grate `pg_type` flake that passed on retry; `git diff --check` and `python3 scripts/check_docs.py` passed (2026-08-14). |
| `bba3a20` P1 remediations (`SESS-DEC-18`, `SESS-DEC-30`, `SESS-DEC-35` identity) | passed; domain | Red: `No_action_with_a_valid_message_*` failed with effect `applied`; `Parser_retains_envelope_payload_ref_*` NRE; `Recommendation_digest_changes_*` equal digests (2026-08-14). Green: Sessions 203/203; architecture 27/27 (2026-08-14). Residual worker gates: exact v2 schema validation at the port; additive v2/per-item persistence. |
| `b83aaaa` local-ref resolution (`SESS-DEC-32`, `REQ-SESS-81`) | passed; domain | Red: missing `local_ref` and P0-rejected voice sibling still accepted the message (2026-08-14). Green: Sessions 205/205; architecture 27/27 (2026-08-14). Worker gates unchanged. |
| `7e1bc83` first-invalid/second-valid cardinality (`SESS-DEC-30`) | passed; **approved** `39883c2` | Red: first missing-ref or voice-ref message left communication `rejected` so a later valid message stayed extra (2026-08-14). Green: Sessions 207/207; architecture 27/27. External review: 0 P0, 0 P1. Push-triggered GitHub Actions on `main` for `39883c2`: Documentation #120 passed (~12s); Implementation #89 passed (~6m 30s). Worker gates unchanged: exact v2 schema validation; additive v2/per-item persistence. |
| Exact `agent-decision.v2` schema validation (`SESS-DEC-31` layer 1) | passed; application boundary | Red: `invalid-timer-duration.json` (`P1Y`) became `ModelExecutionStructuredControl` and `AgentDecisionEnvelopeReader` succeeded (2026-08-14). Green after consistency review: Sessions 221/221 including fixture-parity and duplicate-`local_ref` rejection; architecture 28/28 with Domain↛`Json.Schema` guard. Schema-invalid JSON is `malformed_control` with no envelope. |
| `d5b740c` P1 remediations (`SESS-DEC-31` admission, `SESS-DEC-35` item effects) | passed; application+schema+repository | Red: `Typed_message_payload_ref_cannot_become_structured_control` returned `ModelExecutionStructuredControl` (2026-08-14). Green confirmation pass: Sessions 223/223 including constructor-type guard and serialize→admit fake path; architecture 28/28; Postgres schema/repository/upgrade 46/46 including `0010`, staged validate→effect reload (Decision `applied`, message `applied`, rejected voice `not_attempted`), and `0009→0010` upgrade. Grate one-time script count 10; known concurrent-empty Grate `pg_type` flake unchanged. Worker claim remains blocked pending re-review. |
| `737cb69` P1 remediation (`SESS-DEC-35` effect ownership) | passed; **approved** `a6aba5e` | Red: `Item_effect_rows_must_match_the_validation_item_ownership_tuple` and `Item_effect_rows_cannot_reference_a_rejected_validation_item` inserted successfully under `0010` (2026-08-14). Green confirmation pass: those inserts are FK violations after `0011`; schema/repository/upgrade 49/49 including staged validate→effect reload and `0010→0011` upgrade; Grate empty/repeat 2/2 with one-time script count 11. Frozen `0005`–`0010` unchanged. External review of `a6aba5e`: 0 P0 / 0 P1. Push-triggered GitHub Actions: Documentation #123 passed (~19s); Implementation #92 passed (~6m 31s). Worker claim unblocked. |
| Durable worker claim/execution first slice (`SESS-DEC-16`–`18`) | passed; application+admit+host | Red: admitted Invocation ids were `Guid`-N (not `ainv.*`); worker loop only heartbeated; `RetryLater` left work claimed; shutdown cancellation persisted `execution_failed`. Green confirmation pass: Sessions 230/230 including processor no-action Decision, malformed_control execution outcome, credential fail-closed, terminal redelivery, claim release on retry, and shutdown cancellation without terminalizing; architecture 28/28; runtime 35/35 including processor invocation and live/ready after a processing throw; Postgres repository 16/16 including pending `invocation.execute` enqueue. Live provider and ADR-011 remain out of this step. PostgreSQL claim/lease wiring is next. |
| PostgreSQL claim/lease and worker composition (`SESS-DEC-16`, `SESS-DEC-21`) | passed after isolation fix; infrastructure+host | Red: `DurableInvocationWorkClaimTests` failed to compile (`PostgresDurableInvocationWorkStore` missing, 2026-08-14). Focused 6/6 passed in isolation; the full Postgres suite then failed those 6 because global `SKIP LOCKED` claimed leftover pending rows. Tests now lock other claimable work with `FOR UPDATE` while asserting the prepared Session. Confirmation pass (2026-08-14): claim 6/6; Sessions 230/230; architecture 30/30; runtime 36/36; `git diff --check` and `python3 scripts/check_docs.py` passed. Crash/fault injection remains next. Frozen-policy rehydration from configuration sources remains deferred. |
| `c2fa693` P1 remediations (Repeatable Read snapshot, idle host) | passed; **approved** `4a483a7` | Red: snapshot load `SHOW transaction_isolation` was `read committed`; Worker with `ConnectionStrings:Sessions` registered `DurableInvocationWorkProcessor` (2026-08-14). Green: worker snapshot uses `REPEATABLE READ` and does not mix a later Decision into the earlier head; Worker stays idle even when a Sessions connection string is set. Confirmation pass: snapshot+claim 7/7; Sessions 230/230; architecture 29/29; runtime 37/37; `git diff --check` and `python3 scripts/check_docs.py` passed. External review: 0 P0 / 0 P1 / 0 P2 / 1 P3 work-file current-work sentence (fixed). Queue monopolization on retry is covered by the crash/recovery tranche. |
| `38f1375` P1 cancelled-token release | passed; application | Red: `MemoryWorkStore.ReleaseToPendingAsync` honoring `ThrowIfCancellationRequested` made `Pre_cancelled_worker_releases_claimed_work_with_a_cleanup_token` throw `OperationCanceledException` (2026-08-14). Green: `ReleaseForRetryAsync` uses a bounded cleanup token (default 2s); store still honors cancellation; Sessions 230/230; architecture 28/28; runtime 35/35. Cancellation during load/execute still leaves the claim until lease recovery (covered by crash/recovery tests). |
| Durable worker crash/recovery fault injection (`SESS-DEC-16`, `SESS-DEC-21`) | passed; application+postgres | Confirmation pass (2026-08-14): Sessions 234/234 including crash-after-claim, crash-after-provider-return, lost acknowledgement, and unprocessable-oldest queue fairness; crash-recovery Postgres 8/8 including Decision-commit persist failure, database-time reclaim despite a still-valid host lease, stale-lease CAS, and two-worker contention; claim+snapshot 7/7; architecture 29/29; runtime 37/37; `git diff --check` and `python3 scripts/check_docs.py` passed. No production claim/lease behavior change was required. ADR-011 fragment and scheduler effect-commit injection remain later. |
| Worker OCI COPY of Sessions graph | passed; deploy | Red: `HostOciDockerfileTests` failed because `worker.Dockerfile` did not COPY Sessions, CanonicalJson, or embedded Decision schemas (2026-08-14). Green: architecture 29/29; local `docker build -f deploy/docker/worker.Dockerfile` restored/published Worker without skipping Sessions. |
| API/worker/provider/scheduler runtime tests | pending | |
| Provider credential/no-fallback and manifest append/seal/handoff tests | pending | |
| Web lint/type/unit/build/e2e | pending | |
| Playwright accessibility/responsive/visual evaluation | pending | |
| Aggregate `.NET`, web, OCI, supply-chain, secret, and docs verification | pending | |
| Performance, observability, lifecycle/export, backup/restore verification | pending | |
| Architecture/backend/frontend/security/privacy/QA review | pending | |
| Final specification and repository consistency audit | pending | |

# Blockers

None for the completed crash/recovery worker slice. Live provider wiring,
ADR-011 streaming publication, and frozen-policy rehydration from
configuration sources remain the next out-of-slice gates. Worker host stays
idle until those exist.

Exact production timer durations remain intentional policy inputs. Voice and
other deferred channels remain out of scope.

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
