---
id: evidence-evaluation
status: in-progress
created: 2026-09-07
updated: 2026-09-09T07:20:00+07:00
activation_gate: explicit-implementation-start-after-plan-review
phase3_review: approved-13fd2f3-4e2fb53
phase3_ci_review: approved-3c0c1c3-4a2a86a
phase3_fault_matrix: approved-909c624
phase3_status: complete
phase4_status: complete-work-trace-deferred
phase4_foundation: approved-437401b-2461466
phase4_slice: approved-0514a70-235f7ed
phase4_slice2: approved-9e286f9-1932276
phase4_slice3: approved-4a6a152-88b293f
phase4_slice4: approved-c97715e-7d16935
phase4_gate: approved-2026-09-09-verify-dotnet
---

# Goal

Implement the approved P0 Evidence and Evaluation capability from the existing
immutable terminal Session handoff through one internal, immutable, criterion-
complete Evaluation and an assigned Reviewer's accessible read-only inspection
of its exact Evidence and provenance.

The implementation must preserve the product outcome chain:

```text
Completed Session -> Evidence -> Evaluation -> Review handoff
                                      |
                                      +-> no Human revision
                                      +-> no Review decision
                                      +-> no Result
                                      +-> no Release or Participant visibility
```

It must support the frozen `deterministic`, `agent_assisted`, and
`agent_judgment` criterion modes; enforce exact Submission, transcript,
configuration, manifest, rubric, evaluator, and model bindings; fail closed on
invalid or unavailable authority; preserve retries and replacements as
immutable lineage; and keep Production/Staging model execution default-off
until an exact provider profile is qualified and enabled.

The user explicitly activated this plan on 2026-09-07. Implementation is
`in-progress`. Evaluation processing and Production/Staging model lanes stay
fail-closed until the five-spec upstream gate is evidenced.

# Governing sources

- `AGENTS.md`, `docs/README.md`, `.work/README.md`, and the
  `implementation-workflow` skill — authority by concern, specification-driven
  TDD, task-state hygiene, review, and retirement rules
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — canonical Evidence/Evaluation meaning, P0 order,
  mandatory human oversight, and separation from Human revision, Review
  decision, Result, and Release
- `docs/requirements/features/evidence-evaluation.md` — owning observable
  behavior (`REQ-EVAL-1`–`REQ-EVAL-53`, `AC-EVAL-1`–`AC-EVAL-38`, approved
  `PROP-1`–`PROP-8`)
- `docs/requirements/features/auth-resource-isolation.md` — service and human
  authentication, delegated scope, complete parent-chain authorization,
  assignment/content permission, non-disclosure, revocation, and negative
  isolation requirements
- `docs/requirements/features/resolved-session-configuration.md` — exact frozen
  rubric/evaluation procedure, model/runtime, configuration, manifest,
  provenance, reconstruction, and non-secret credential binding
- `docs/requirements/features/assessment-setup.md` — activated baseline,
  versioned source selection, fairness consistency, and immutable rubric/
  evaluation-procedure source reference
- `docs/requirements/features/submission-attempts.md` — exact accepted
  Submission-version binding and immutable Attempt ownership chain
- `docs/requirements/features/session-text-lifecycle.md` — terminal Session
  record, manifest seal, accepted/published transcript boundary, immutable
  cutoff, and Evaluation handoff
- `docs/requirements/features/review-result-release.md` — downstream Review
  ownership and the prohibition on Evaluation creating a decision, Result, or
  Release
- `docs/requirements/mvp-operational-defaults.md` — required-durable behavior,
  application-session and workload revocation, protected-data lifecycle,
  deletion/hold rules, and content-minimized operations
- `docs/architecture/evaluation-execution-contract.md` — approved locator,
  Evidence-set seal, request/invocation, evaluator, completion, replacement,
  authorization, worker, and Review-handoff realization (`EVAL-DEC-1`–
  `EVAL-DEC-8`)
- `docs/architecture/review-result-release-contract.md` — Review-case,
  assignment, exact candidate, and protected-inspection ownership; Evaluation
  must integrate through a narrow handoff, must never select by mutable alias or
  arrival order, and must never switch a replacement candidate or release
- `docs/architecture/session-runtime-contract.md` — upstream terminal seal,
  transcript/provenance, and handoff contracts
- `docs/architecture/backend-module-architecture.md`,
  `docs/architecture/mvp-architecture.md`, and
  `docs/architecture/frontend-architecture.md` — modular-monolith ownership,
  ports, durable work/outbox, provider-neutral execution, restricted evaluator
  boundary, production host topology, and browser state/query rules
- `docs/ui-ux/flows/evidence-evaluation-human-review.md` — approved Review work,
  Evaluation-processing, criterion, Evidence, provenance, integrity,
  accessibility, responsive, and exact-language contract; Human revision and
  decision portions remain downstream and out of scope here
- `docs/ui-ux/design-system/README.md` and
  `docs/ui-ux/design-system/implementation-guide.md` — Approved v1.1 authority,
  production donor rules, route-family assignments, and Design Lab isolation
- Applicable design-system modules selected through the implementation guide:
  accessibility, colors, typography, layout, density, interaction states,
  motion, status, keys, lists, tables, tabs, modals, error summary, content,
  alerts, layout primitives, cards, Evidence, Evaluation, technical metadata,
  timeline, protected content, and empty/loading
- `docs/contributing/development-harness.md` — canonical/candidate origin
  attachment, synthetic OIDC sign-in, and mandatory Playwright MCP evidence
- Git `15ae379` and predecessor Git `12bfc65` — retired hosted-text-session
  plan/status and the implemented terminal handoff, production Session host,
  Worker, transcript, multi-context, and offline/reconnect seams

Before new production UI, classify `/review` as the approved `management`
family and `/review/:reviewId` as the approved `guided-task` assigned-record
family. Clone an accepted production management page for the list, adapt
`ProductionMyWorkDetailPage`/`AssignmentStationLayout` for the assigned-record
shell, and use the accepted Reviewer Design Lab journey only as a composition
reference for criterion/Evidence content. Promote or recreate any required
production-safe feature component outside `web/src/design-lab/` first; never
import Design Lab fixtures, routes, reducers, storage, or styles into the
production entry graph. No `$impeccable shape` work is authorized because the
approved layout families and donors already exist.

# Dependency and activation gate

- `hosted-text-session` is complete, independently reviewed, and retired at
  `15ae379`; `main` and `origin/main` both point to that commit as of this plan.
  The predecessor gate is therefore clear.
- At activation, re-run the current-state and seam inventory. If upstream code
  changed after `15ae379`, reconcile this plan before writing the first test.
- Begin implementation only after the user explicitly activates this plan.
- Implementation work may start, but rollout and a full-feature completion
  claim remain gated by the approved Evaluation rollout rule: authorization/
  isolation, resolved configuration, assessment setup, Submission/Attempt, and
  Session lifecycle must each have their required upstream P0 behavior
  implemented and integration-green. `docs/current-state.md` currently marks
  those owning specifications Partial. At activation, build an exact upstream
  dependency checklist from the five specifications; do not silently absorb
  unrelated backlog, weaken the rollout rule, or enable Evaluation while a
  required dependency remains a gap.
- Production/Staging Evaluation work and model disclosure remain fail-closed
  until workload identity, exact deployment/profile, credential binding, data
  boundary, and provider qualification are enabled together. Development and
  Testing may use bounded deterministic/synthetic adapters only.
- Real-provider qualification is not a blocker for domain, persistence,
  contract, deterministic-runner, synthetic integration, API, or UI work. It
  is a blocker for claiming Production enablement or real-Participant
  readiness.
- `text-interaction-controller-contract` remains planned and unactivated.
  Evaluation must not add controller signals, tools, memory writes, voice, or a
  new Session publication lane.

# Scope

## In

- Canonical versioned contracts for the frozen evaluation procedure,
  Evaluation request/status/work, deterministic invocation, criterion judgment,
  immutable Evaluation, annotation, replacement lineage, Review handoff, and
  assigned-reviewer read projections. Publish each shape only to the consumers
  that require it: internal work/provider/protected-artifact contracts do not
  enter browser TypeScript or public OpenAPI; browser-visible projections do.
- The existing `evidence-locator.v1` and
  `evidence-set-jcs-sha256-v1` contracts completed as executable source-native
  validation and canonical sealing behavior, without incompatible edits to the
  closed v1 shapes.
- Forward-only protected persistence of canonical configuration-source payloads
  for new rubric/evaluation-procedure versions. Existing immutable source
  versions without payload bytes remain historical and honestly
  evaluation-ineligible; do not synthesize or rewrite their content.
- Strengthening Attempt-start resolution so a new evaluable Session freezes an
  exact rubric/evaluation-procedure source and all Evaluation-required
  provenance. Preserve the distinction between the handoff's terminal
  manifest-seal `procedure_id` and the rubric payload's evaluation-procedure
  identity.
- A new Evaluation module with domain/application and PostgreSQL infrastructure
  projects, plus focused tests and architecture dependency rules.
- Trusted owner ports for the Session handoff/terminal/transcript/manifest,
  resolved configuration and exact rubric payload, Attempt and exact accepted
  Submission versions/items, protected artifact bytes, authorization,
  audit/outbox, clock, model provider, deterministic evaluator, and downstream
  Review handoff.
- Automatic admission from only an eligible `Completed` Session handoff;
  stable handoff-plus-frozen-input idempotency; bounded durable work; claim
  leases; exact-input retries; recovery after lost responses/restarts; and one
  authoritative completion under concurrency.
- Source-native Evidence location and independent verification for direct text,
  `.txt`/`.md` accepted items, accepted/published transcript material at or
  before cutoff, safe configuration/manifest facts, and deterministic facts.
- A restricted allowlisted deterministic evaluator registry/runner with exact
  immutable identity, input/output schemas, canonical input digest, positive
  CPU/time/memory/output bounds, no Participant code, no unrestricted paths or
  scripts, and no network egress by default.
- Agent-assisted and Agent-judgment execution through an Evaluation-owned,
  provider-neutral port with independently validated structured output. Agent
  text is untrusted and cannot change criterion mode, scope, rubric, Evidence,
  deterministic facts, aggregation, capabilities, memory, or Release.
- Immutable Evidence items/set seal, criterion judgments, Evaluation,
  invocation/attempt provenance, replacement lineage, and append-only integrity
  or lawful-unavailability annotations.
- One primary-store completion transaction covering the Evaluation artifact,
  criterion judgments, Evidence/set seal, evaluator/model provenance,
  manifest append/reference, non-releasing Review handoff, required audit, and
  immutable outbox acceptance.
- A minimal Review-owned interoperability foundation sufficient to accept the
  non-releasing Evaluation handoff, create one explicit initial candidate
  selection when the case has no candidate and exactly one eligible Evaluation,
  and expose active-assignment checks to the Evaluation read path. The initial
  selection is an immutable Review-owned event with exact Evaluation identity,
  service authorization, bounded reason, UTC order, and durable audit; a
  replacement never switches it. Development/Testing may seed assignments, but
  there is no user-facing assignment, replacement-candidate, revision, or
  decision mutation in this task.
- Authenticated APIs for bounded authorized Evaluation status, assigned Review
  work/read, criterion detail, independently reauthorized Evidence source
  opening, operational retry/reconciliation, and authorized replacement and
  annotation commands. No public or Participant Evaluation endpoint.
- Production `/review` read-only Evaluation-work status and
  `/review/:reviewId` assigned-record inspection covering awaiting, queued,
  running, retryable failure, review-required, completed, insufficient,
  lower-precision, integrity-changed, and lawfully unavailable states. The
  surface must say **Internal Evaluation · Not a released Result** and expose no
  Human revision, Review decision, Result, or Release action.
- Bounded telemetry for admission/status/completion latency, queue depth/age,
  leases, retries, evaluator/provider outcomes, citation failures, conflicts,
  audit failures, replacements, annotations, and Review-required outcomes,
  without raw protected content or unrestricted identifiers.
- Default lifecycle resolution for Evidence/Evaluation and work/idempotency
  records, legal-hold dependency protection, honest unavailable-source
  representation, and no memory/learning/calibration/harness-reuse path.
- Specification-driven red-green-refactor execution, full requirement-to-test
  traceability, focused and PostgreSQL fault/concurrency tests, production-host
  negatives, authenticated end-to-end coverage, performance evidence, and
  Playwright MCP accessibility/visual evaluation.

## Out

- Creating, editing, approving, or publishing rubrics, evaluation procedures,
  evaluator packages, models, activities, cohort baselines, Submissions,
  Session content, or configuration aliases. This task stores and consumes
  exact versions; it does not add authoring UX.
- Review-work claiming, assignment/reassignment/relinquishment UI or commands,
  replacement-candidate retain/change resolution, Human revision,
  participant-facing preview, Review decision, Result construction, Release,
  notification, correction, appeal, or Participant outcome visibility. Those
  remain the next P0 item under `review-result-release.md`.
- Any Evaluation-owned or arrival-order candidate selection. The narrow Review
  adapter may record the one exact initial candidate only when the case has no
  candidate and exactly one eligible Evaluation. Replacement lineage always
  requires a later explicit Review-owned resolution and never switches the
  candidate automatically.
- A general Evaluation/Evidence repository, global search, arbitrary content
  export, public links, or using an Evaluation URL/ID as authorization.
- Participant access to Evaluation status, Evidence, criteria, scores,
  rationale, confidence, uncertainty, provisional feedback, model/evaluator
  provenance, or internal identifiers.
- Fine-grained source ranges that cannot be independently verified. Whole-
  artifact Evidence is allowed only with its lower precision represented
  honestly.
- PDF, Office, image, audio, video, voice/playback, participant-session tool
  results, external evaluator services, participant code, unrestricted scripts,
  or general sandbox execution. P0 source material remains direct text and
  accepted `.txt`/`.md` text attachments.
- Production provider enablement or credential management UI. Synthetic and
  local adapters do not qualify Production.
- Dynamic memory, cross-participant learning, calibration/training datasets,
  analytics training, unrelated-activity reuse, harness improvement, or Agent
  self-modification.
- Commits, pushes, pull requests, deployments, or releases unless separately
  requested.

# Current implementation inventory

- The predecessor is retired. `SessionRuntime` creates one append-only
  `session_evaluation_handoffs` row for every terminal Session and marks only a
  sealed `Completed` Session eligible. The row is foreign-key bound to the
  exact terminal record, lifecycle, cutoff, configuration, manifest, and seal;
  Session completion writes required audit/outbox in the same transaction.
- `session_evaluation_handoffs.procedure_id` is the terminal manifest-seal
  procedure (`manifest-jcs-sha256-v2`), not the rubric/evaluation procedure.
  The Evaluation implementation must not conflate those identities.
- `session_resolved_configurations.canonical_json` includes all activated
  baseline source references that reached Attempt start, including the rubric
  when present, and includes the exact model profile plus non-secret credential
  binding. The current resolver's required-source list does not require
  `rubric_evaluation`, so an Evaluation-capable start must tighten that
  prerequisite and regression-protect it.
- `configuration_source_versions` stores procedure/schema/digest metadata but
  not the canonical source bytes accepted by registration. Assessment
  readiness descriptors expose only bounded `effective_values`. There is no
  trustworthy frozen criterion list, evaluator mode, binding, scoring rule, or
  failure behavior for Evaluation to execute. A protected immutable payload
  store and a versioned procedure parser are the first implementation boundary.
- Historical Development source references use synthetic placeholder digests.
  They must not be backfilled with invented rubric content. Add a new exact
  source version with a real canonical digest for evaluable Development/
  Testing cohorts; old Sessions remain honestly unavailable/review-required.
- The contract catalog already contains `evidence-locator.v1`, C# and
  TypeScript locator records, `evidence-set-jcs-sha256-v1`, and a JCS fixture.
  It has no complete Evaluation request/status/artifact/criterion/replacement/
  annotation/Review-handoff contract family.
- Submissions own immutable accepted-version metadata and protected object
  bytes. `IExactAcceptedVersionReader` is transaction-scoped and returns exact
  item metadata, while the human preview service is actor/enrollment oriented.
  Evaluation needs a separate delegated-service owner port for exact bound
  items and content; it must not query Submission tables or object storage
  directly.
- Sessions own accepted Participant messages, published complete/incomplete
  Agent messages/fragments, visible notices, work-trace events, terminal cutoff,
  configuration/manifest records, and authorization. There is no Evaluation-
  specific exact-source reader yet; add a narrow Session-owned port instead of
  cross-module table reads.
- There is no Evaluation, Review, Result, or Release module in `src/Modules/`,
  and no Evaluation test project. PostgreSQL migrations currently end at
  `0070`; allocate the next available additive number at activation (currently
  `0071`) rather than reserving a stale number in advance.
- The Worker currently samples only Session invocation/timer/expiry lanes in a
  single loop. Evaluation work must be a separately bounded processor with
  fair polling, independent readiness/capability reporting, and no starvation
  or implicit reuse of Session response-publication semantics.
- Session's OpenAI-compatible adapter is Session-contract specific. Evaluation
  must define its own provider-neutral port and adapter contract. A genuinely
  protocol-level client may be extracted only if it meets the building-block
  stability rule and does not leak Session or Evaluation domain semantics.
- IdentityAccess has a generic `reviewer` relationship, but no active Review
  case assignment. A role label or Session relationship is not sufficient
  authorization for Evaluation/Evidence reads.
- `/review` and `/review/:reviewId` are honest contract-unavailable production
  shells. The accepted Reviewer journey and fixtures exist only in Design Lab;
  production has no query client, state model, list, criterion, or Evidence
  viewer.
- `docs/current-state.md` correctly classifies all Evidence/Evaluation rows and
  the Evaluation/Review/Result/Release hosts as gaps. Do not change that status
  until implemented behavior and review evidence justify a narrower derived
  classification.

# Requirement-to-surface map

| Requirement / acceptance target | Implementation surfaces | Required evidence |
| --- | --- | --- |
| `REQ-EVAL-1`–`REQ-EVAL-7`; `AC-EVAL-1`–`AC-EVAL-5` | Session handoff owner port; exact resolved-configuration/rubric, Attempt, Submission, transcript, and manifest loaders; input digester; request admission/idempotency; service delegation and commit reauthorization | Eligible Completed admission; Terminated/Aborted/unsealed/incomplete rejection before disclosure; wrong ownership/source/digest/cutoff negatives; equivalent and conflicting idempotency; later-alias invariance |
| `REQ-EVAL-8`–`REQ-EVAL-17`; `AC-EVAL-5`–`AC-EVAL-8`, `AC-EVAL-22`, `AC-EVAL-27` | Source-native locator types/verifiers; protected Submission/Session/configuration/manifest/deterministic readers; Evidence aggregate and JCS seal; append-only annotation/disposition | Exact version/range/UTF-8 byte and transcript-cutoff tests; forged/off-by-one/wrong-source/post-cutoff/unpublished-content negatives; whole-item precision; independent read reauthorization; later unavailable/integrity annotation |
| `REQ-EVAL-18`–`REQ-EVAL-28`; `AC-EVAL-9`–`AC-EVAL-14`, `AC-EVAL-20` | Frozen-procedure parser; criterion aggregate; schema/range/policy/citation validators; deterministic aggregation; immutable Evaluation artifact; non-releasing Review handoff | Exactly one judgment per exact criterion; insufficiency/not-applicable/aggregation matrix; concise rationale without hidden reasoning; confidence/uncertainty; provisional-label validation; database/API assertions that no revision/decision/Result/Release/notification exists |
| `REQ-EVAL-29`–`REQ-EVAL-36`; `AC-EVAL-15`–`AC-EVAL-19`, `AC-EVAL-27` | Durable request/work/attempt state; leases/backoff/timeout; provider-attempt records; atomic completion; unique constraints; replacement authorization/lineage; annotations and current disposition | Exact-input retry and restart; partial/schema-invalid/provider/audit failure; lost response; duplicate/equivalent/conflicting completion races; post-completion retry reconciliation; authorized replacement without overwrite; history reconstruction |
| `REQ-EVAL-37`–`REQ-EVAL-46`; `AC-EVAL-21`–`AC-EVAL-27`, `AC-EVAL-30`, `AC-EVAL-31` | Action/resource constants; service delegation; active Review assignment/content-scope port; scoped repositories; query/API authorization; protected references; audit/outbox; lifecycle/hold; no-learning eligibility boundary | Signed-out/wrong-role/wrong Organization/Activity/Participant/Attempt/Session/assignment/delegation/action negatives for list/count/direct/read/evidence/retry/replacement/annotation; revocation races; prompt injection; operational payload scan; retention/hold/deletion order; no eligible reuse records; reconstruction |
| `REQ-EVAL-47`–`REQ-EVAL-53`; `AC-EVAL-32`–`AC-EVAL-38` | Versioned procedure/evaluator bindings; allowlisted registry; restricted deterministic runner; canonical input/output validation; Agent-assisted fact channel; conflict validator; provider-neutral Agent adapter | Frozen one-mode-per-criterion; deterministic objective/aggregation; exact evaluator provenance; fact-to-Agent handoff; conflict cannot overwrite; timeout/memory/output/path/argument/schema/dependency failures; no egress/no code/no Session tool capability/no silent fallback |
| Narrow downstream interoperability from `REQ-REV-1`, `REQ-REV-3`–`REQ-REV-6`, `REQ-REV-8`, `REQ-REV-27`–`REQ-REV-31`; `AC-REV-1`, `AC-REV-2`, `AC-REV-4`, plus only the no-implicit-switch boundary of `AC-REV-3` | Review-owned handoff/case, one exact initial-candidate event, immutable assignment event/current pointer, scoped list/read port, replacement-available stale marker, authorization and audit | No-Evaluation unresolved case; exact initial selection without `latest`; no replacement auto-switch; wrong/revoked assignment and list/count isolation; selection/audit failure rollback; no claim/reassign/revision/decision/Result/Release behavior. `AC-REV-3` replacement resolution and the Review feature remain explicitly incomplete |
| UX requirements; `AC-EVAL-22`, `AC-EVAL-28`; approved `PROP-UI-REV-1`–`PROP-UI-REV-6` as applicable | Read-only production Review work list and guided assigned-record detail; exact criterion navigation; subordinate Evidence source route/view; six-track-compatible state model with only Evaluation/assignment/integrity capabilities active | Component/route/query tests plus authenticated Playwright accessibility snapshots and desktop/narrow screenshots for loading, empty, queued/running, failure/retry, review-required, completed, insufficient/not-applicable, lower precision, source denied/unavailable/integrity-changed, assignment loss, exact return focus, 400% reflow, reduced motion, and forced colors |
| Performance/reliability; `AC-EVAL-18`, `AC-EVAL-25`, `AC-EVAL-29`; `PROP-6` | Indexed admission/status queries; bounded queue/lease/worker; atomic primary-store completion; telemetry and representative load harness | 95% status within 2 seconds and 95% completion within 120 seconds under declared representative limits excluding provider-wide outage; separate queue/provider/retry/review-required metrics; audit outage rollback; concurrency/throughput/lease recovery |

# Planned implementation sequence

## Phase 0 — Activate and freeze the implementation baseline

- [x] Set `status: in-progress` and refresh `updated` only after explicit user
  activation.
- [x] Re-run `git status`, `git log`, `.work/active/`, contract catalog,
  migrations, host composition, Worker composition, route manifest, and current
  UI donors against the then-current commit.
- [x] Confirm no other active task owns Evaluation, Review handoff, rubric
  payload persistence, or the same production routes. Resolve overlap before
  editing shared surfaces.
- [x] Materialize the five-spec upstream rollout dependency checklist, map each
  required upstream behavior to current code/test evidence, and mark Evaluation
  processing disabled wherever the dependency is absent. A partial upstream
  classification is not automatically a blocker if every Evaluation-required
  behavior is proven, but any missing required behavior blocks rollout and a
  complete-feature claim unless the owning approved source changes.
- [>] Copy every row in the requirement-to-surface map into concrete red tests
  or a named manual/performance check. Do not mark a phase green without the
  corresponding `REQ-*`/`AC-*` evidence.
- [x] Record the exact Development/Testing evaluation procedure fixture,
  evaluator registry entries, service actor/delegation, Review assignment seed,
  and provider profile used for verification. Keep credentials and protected
  content out of this file and logs.

## Phase 1 — Close frozen-input and canonical-contract prerequisites

- [x] Red: add contract/catalog tests demonstrating that the current catalog
  lacks a complete, versioned evaluation-procedure and Evaluation transport/
  artifact family while preserving compatibility of existing v1 Evidence
  locator/seal contracts.
- [x] Define `evaluation-procedure.v1` as a bounded canonical document: exact
  procedure identity/version, ordered exact criterion IDs/versions, one frozen
  evaluator mode each, permitted status/score/decision/confidence fields,
  Evidence requirements, deterministic evaluator bindings, Agent input/output
  schemas, aggregation, insufficiency/not-applicable/conflict behavior,
  retry/time/resource bounds, and Review/replacement policy references. Do not
  invent a universal score, threshold, confidence scale, or aggregation rule.
- [x] Add canonical schemas/fixtures and C# contracts for internal request/work,
  deterministic invocation, protected provider artifacts, criterion judgment,
  immutable Evaluation, annotation, replacement, and Review handoff. Add only
  the authenticated reviewer status/read projections to OpenAPI and TypeScript.
  Prove internal fields cannot enter browser bundles or public responses. Use
  closed discriminated unions and positive size/count/range bounds.
- [x] Add negative fixtures for mutable aliases, duplicate/missing criteria,
  multiple/no evaluator modes, unknown evaluators, non-positive bounds,
  invalid statuses/ranges, raw content in operational envelopes, incompatible
  schema versions, and prohibited executable/network fields.
- [x] Red: prove Configuration registration currently discards the canonical
  bytes and that an evaluable Attempt can currently omit a required rubric.
- [x] Add an additive immutable protected payload table/owner repository keyed
  by exact Organization/source/version/digest and persist verified canonical
  bytes transactionally with new Configuration source versions. Keep public
  metadata reads content-free and add lifecycle/hold hooks.
- [x] Add a typed rubric/evaluation-procedure parser and protected owner port.
  Register a new real-digest Development/Testing rubric source version; never
  rewrite or fabricate payloads for historical placeholder-digest versions.
- [x] Extend source registration and Assessment readiness/activation so the
  exact procedure schema, criterion/mode set, Evidence policy, evaluator
  registry entries, immutable evaluator/config/dependency identities, positive
  limits, failure/conflict rules, Evaluation model binding, and lifecycle
  policy are verified before cohort activation. An unknown, unavailable,
  unqualified, mutable, or widening binding blocks activation; the Worker does
  not become the first place these errors are discovered.
- [x] Tighten new evaluable Attempt resolution to require exact
  `rubric_evaluation` and applicable review/evidence/model bindings from the
  activated baseline. Verify that the resolved configuration/manifest retains
  the exact rubric reference/digest, Evaluation model profile/deployment/model
  identity or immutable fingerprint, adapter contract, non-secret credential
  binding, and all procedure-permitted generation parameters. Fail closed if
  the existing Session model binding cannot prove the Evaluation binding rather
  than silently reusing it by profile name.
- [x] Name the handoff field in new contracts as
  `manifest_seal_procedure_id`; bind the evaluation procedure separately from
  resolved configuration. Preserve the deployed database column and closed
  Session wire contracts unless a versioned additive projection is required.
- [x] Green/refactor: run contract, Configuration, Assessment, Attempt-start,
  RSC/manifest, migration-upgrade, JCS, and architecture tests.

## Phase 2 — Establish Evaluation domain and module boundaries

- [x] Create `src/Modules/Evaluation/FlexAgent.Evaluation` for domain and
  application behavior and
  `src/Modules/Evaluation/FlexAgent.Evaluation.Infrastructure` for PostgreSQL,
  protected-source adapters, and host composition. Add
  `tests/Evaluation/FlexAgent.Evaluation.Tests` and solution references.
- [x] Red: add architecture tests preventing Evaluation from referencing host,
  concrete owner-module infrastructure, Design Lab, Review implementation
  internals, or provider-specific adapters. Permit only documented contracts
  and narrow owner ports.
- [x] Model explicit value objects/aggregates for trusted ownership, frozen
  input identity, request kind, request/invocation/attempt states, evaluator
  mode, criterion judgment, Evidence item/set, Evaluation, lineage, annotation,
  and disposition. Reject empty IDs, non-UTC times, unbounded text, unsupported
  schemas/modes/statuses, duplicate criteria/Evidence IDs, and mutable aliases.
- [x] Implement the evaluation-procedure resolver and independent validators
  for criterion completeness, configured fields/ranges, insufficiency,
  not-applicable, confidence/uncertainty, rationale/provisional content,
  aggregation, citation integrity, deterministic conflicts, and protected-
  content boundaries.
- [x] Red/green/refactor domain tests for all valid modes and the complete
  invalid matrix before persistence or provider wiring.

## Phase 3 — Add persistence, admission, durable work, and recovery

- [x] Allocate the next available additive migrations at activation (currently
  `0072+`) for Evaluation requests,
  idempotency/input identities, invocation attempts, deterministic attempts,
  durable work/leases, protected provider artifacts/references, Evidence items,
  Evidence sets/seals, criterion judgments, Evaluations, lineage,
  annotations/dispositions, manifest references, required audit/outbox, and
  minimal Review-handoff interoperability records.
- [x] Use complete Organization/Activity/Participant/Attempt/Session parent
  columns and composite keys/foreign keys where they prevent cross-scope
  binding. Add unique constraints for initial request input identity,
  request-attempt ordinal, one authoritative completion, Evidence ownership,
  exact criterion set, and replacement predecessor lineage.
- [x] Protect completed artifacts, Evidence sets/items, criterion judgments,
  terminal attempts, and lineage against ordinary update/delete. Isolate
  mutable current disposition and claim state from immutable history, and
  provide only the approved, auditable lifecycle-disposition mechanism needed
  for expiry/hold processing; normal repositories and service actors cannot
  bypass immutability, while lifecycle policy is not made impossible by an
  unconditional database trigger.
- [x] Red: PostgreSQL tests for cross-tenant/Activity/Participant/Attempt/
  Session foreign-key attacks, invalid terminal state/cutoff/seal/configuration,
  duplicate admission, conflicting idempotency, completion races, mutation,
  deletion, audit outage, lost response, expired lease, and restart recovery.
- [x] Version the Session-owned `session.evaluation_handoff.recorded` delivery
  contract and consume it through a durable Evaluation inbox. Treat the event
  only as a wake-up/locator: load the authoritative handoff through a narrow
  Session owner port, then admit idempotently. Add a bounded cursor-based
  reconciliation scan so a missed/delayed delivery cannot strand an eligible
  handoff. Accept only exact eligible `Completed` handoffs and verify the
  terminal/manifest seal before protected input materialization.
- [x] Compute a canonical frozen-input digest over the handoff, ownership,
  terminal/cutoff/seal, resolved configuration/manifest, exact rubric/
  procedure, Submission binding, evaluator registry, model binding, and
  lifecycle policy. Use it with the handoff identity for admission idempotency.
- [x] Admit the request, work row, safe status, required audit/outbox, and
  idempotent response in one primary-store transaction. Ineligible handoffs
  create no model/evaluator disclosure and no completed Evaluation.
- [x] Implement bounded claim/renew/release/retry/exhaustion semantics with
  authoritative database time, positive backoff/timeout/attempt bounds,
  per-Organization concurrency/backlog limits and fair claim partitioning, and
  stable non-content failure categories. One Organization, activity,
  Participant, oversized artifact, or provider failure must not starve another.
- [x] Green/refactor focused Evaluation and PostgreSQL migration/integration
  tests.

## Phase 4 — Materialize exact sources and validate Evidence locators

- [x] Add Session-owned read ports for the exact terminal handoff, terminal
  record/seal, resolved configuration/manifest, accepted/published transcript
  material at or before cutoff, and safe configuration/manifest facts. Reads
  must scope before materialization and return stable protected references plus
  digests, not unrestricted tables.
  `IEvaluationSessionEvidenceSource` + `PostgresEvaluationSessionEvidenceSource`
  load the authoritative handoff, cutoff-scoped transcript items, and safe
  configuration/manifest fact projections bound to handoff digests. Participant
  and agent transcript materialization is complete through slice 3–4. Session
  work-trace owner port/materialization remains deferred until durable Session
  persistence exists (see Findings).
- [x] Add Submission-owned delegated-service ports for the exact bound accepted
  versions/items and protected object bytes. Reauthorize ownership/status/
  binding before each disclosure; reject failed, quarantined, rejected, later,
  unbound, or mutable material.
  `IEvaluationSubmissionEvidenceSource` +
  `PostgresEvaluationSubmissionEvidenceSource` require completed Attempt scope
  and exact artifact bytes via `IArtifactStore`; integration coverage green for
  bound-item load, wrong-session rejection, and missing-artifact negatives.
- [x] Normalize P0 text sources to source-native units without losing exact
  bytes: Unicode-scalar/line and UTF-8 byte ranges for direct text/text files;
  stable transcript message/fragment/update/notice IDs and offsets at/before
  cutoff; allowlisted JSON Pointers for safe configuration/manifest and
  deterministic facts.
  `EvidenceTextSourceNormalizer` implements `line-split.v1` and UTF-8 byte-range
  excerpt digest checks; Session owner port reconstructs agent transcript bytes
  from durable fragments via `EvidenceAgentTranscriptAssembler`; work-trace
  materialization remains deferred (schema allowlist only; no owner table yet).
- [x] Red: locator tests for wrong source/version/digest/criterion/Evaluation,
  cross-scope material, invalid UTF-8 boundaries, off-by-one ranges, forged
  quotes, post-cutoff messages/fragments, unpublished/failed generations,
  local drafts, hidden prompts, unsafe config fields, missing objects, and
  later aliases.
  Domain matrix covers ownership cross-scope, wrong digest, post-cutoff
  transcript, post-cutoff agent fragment `session_sequence`, byte-range UTF-8
  boundary, line range beyond content, configuration JSON Pointer outside safe
  projection, missing submission material, forged excerpt digest, wrong source
  version / unpublished transcript material, unpublished open agent messages,
  cancelled agent messages, and lower-precision fallback; work-trace owner-port
  materialization remains deferred (schema allowlist only; no durable table yet).
- [x] Implement independent locator verification after parser/model output and
  again at completion. Store only protected references and locator metadata in
  Evaluation records; retrieve exact content from the owning source on an
  authorized Evidence open.
  `EvidenceLocatorVerifier` + digest computer verify structure, ownership,
  source material, location bounds, and adapter version;
  `EvidenceLocatorCompletionVerifier` + `EvidenceLocatorCompletionService` wire
  completion-time batch verification and `SealedEvidenceItemReference` output;
  trusted ownership is derived from authoritative handoff plus Evaluation ID,
  and safe-fact projection verifies canonical bytes against frozen digests;
  `EvidenceLocatorMetadataProjector` + `PostgresEvaluationEvidenceLocatorStore`
  persist protected locator metadata scoped to the admitted request.
- [x] Implement honest whole-artifact fallback only when the source cannot
  verify a finer location, carrying explicit lower precision. Never infer a
  range from model text.
  Verifier records `lower_precision` when `PermitWholeItemFallback` is set and
  finer range verification fails; `EvidenceLocatorProcedurePolicy` resolves
  fallback per criterion at completion (caller no longer supplies a bypass).
  Corrective `1932276`: fallback seals/persists effective `whole_item` location
  digest and verified precision rather than the failed exact-range JSON.
  Approved 2026-09-08: 0 Blocker / 0 High / 0 Medium; previous fallback
  integrity High closed.
  Review follow-up on `437401b`: Session owner port now requires authoritative
  participant admission sequences (no `COALESCE(..., 0)`), and byte-range
  normalization rejects non-boundary UTF-8 slices.
- [x] Canonicalize and seal ordered Evidence items with
  `evidence-set-jcs-sha256-v1`; verify the existing fixture and add ordering,
  duplicate, drift, and tamper fixtures.
  `EvidenceSetSealComputer` + fixture/tamper/duplicate/reorder/drift tests green.
- [x] Green/refactor owner-port, artifact, locator, seal, cross-scope, and
  later-source-invariance tests.
  Slice 2 (`9e286f9` + `1932276`) approved 2026-09-08: procedure gating,
  locator-metadata persistence, effective fallback sealing, seal reorder/drift,
  mutable-alias/work-trace negatives, and idempotent locator-store integration.
  Slice 3 (`4a6a152` + `88b293f`) approved 2026-09-09: agent fragment
  reconstruction in Session owner port with cutoff-bound `session_sequence`
  filtering (query + assembler), published `complete`/`incomplete` admission,
  assembler/domain/integration negatives, and extended locator matrix items from
  slice 3. External review on corrective chain: 0 Blocker / 0 High / 0 Medium;
  previous fragment-cutoff High and incomplete-publication Medium closed.
  Slice 4 (`c97715e` + `7d16935`) approved 2026-09-09: locator negative matrix
  (forged excerpt, wrong version, unpublished material, cancelled agent) plus
  work-trace source-type isolation via separate `WorkTraceItemsBySourceId`
  (empty from owner port until durable persistence exists). External review on
  corrective chain: 0 Blocker / 0 High / 0 Medium; work-trace masquerade High
  closed. Phase 4 gate closed 2026-09-09: proportionate full-suite regression
  green (`verify-dotnet.sh` 2201 passed / 4 skipped; `scripts/check_docs.py`
  passed). Session work-trace owner port/materialization remains an accepted
  deferred gap until Session durable persistence lands; fail-closed empty
  collection is interim default and does not block Phase 5 entry.

## Phase 5 — Implement the restricted deterministic evaluator lane

- [ ] Define an Evaluation-owned evaluator registry port whose entries bind
  exact evaluator ID/version or verified digest, supported operation,
  input/output schemas, canonicalization/configuration/dependency digests,
  positive resource bounds, failure/conflict behavior, and qualification
  state.
- [ ] Start with a minimal built-in allowlist sufficient for the approved
  synthetic procedure: exact comparison/schema validation, bounded permitted
  calculation, citation validation, and rubric aggregation as actually
  declared. Do not add a general expression language or execute procedure/
  Participant strings.
- [ ] Red: reject unknown/mutable evaluator identifiers, changed dependency or
  config digests, path traversal, shell/script/code fields, environment or
  secret access, network attempts, oversized/deep inputs, timeout, memory/CPU/
  output exhaustion, invalid output, wrong criterion/scope, and silent mode
  fallback.
- [ ] Run evaluators through a restricted adapter with no network egress by
  default and explicit positive bounds. If in-process built-ins cannot provide
  enforceable isolation for a permitted operation, use a separately bounded
  worker process/container contract before enabling that operation; do not
  claim sandboxing from application checks alone.
- [ ] If an evaluator requires temporary files, allocate a per-invocation
  private directory with bounded size, safe filenames, no inherited secrets,
  no symlink/path escape, and guaranteed cleanup on success, failure, timeout,
  cancellation, and process restart. Prefer memory-only canonical inputs for
  built-in evaluators.
- [ ] Persist every deterministic invocation's exact canonical input digest/
  references, evaluator/config/dependency identity, limits, UTC timing,
  outcome, bounded failure, output digest/protected reference, and audit/
  manifest correlation.
- [ ] Treat successful output as protected Evidence. It is not policy or
  infallible truth, and Agent output cannot overwrite it.
- [ ] Green/refactor evaluator registry, isolation, provenance, failure,
  aggregation, no-egress, and no-Session-tool-capability tests.

## Phase 6 — Implement Agent-assisted and Agent-judgment execution

- [ ] Define a provider-neutral Evaluation model port and strict request/
  response schemas. Include only fixed trusted policy/rubric fields, exact
  permitted Evidence/protected facts, bounded context, and non-secret frozen
  model provenance.
- [ ] Keep Session generation and Evaluation judgment contracts separate.
  Extract only genuinely generic protocol/credential plumbing if dependency
  and architecture tests prove the abstraction is stable; otherwise add an
  Evaluation-specific adapter project.
- [ ] Implement `deterministic` with no Agent call, `agent_assisted` only after
  required deterministic facts verify, and `agent_judgment` only for exact
  procedure-permitted criteria. Never substitute modes at runtime.
- [ ] Add a bounded deterministic/synthetic Development/Testing model adapter
  that can exercise valid, insufficient, malformed, conflicting, timeout, and
  provider-failure paths without real data or network access.
- [ ] Reuse workload credential source patterns without exposing secrets.
  Production/Staging adapter selection must fail closed unless exact
  qualification and workload identity are simultaneously valid.
- [ ] Red: prompt-injection and confused-deputy suites across Submission text,
  filenames, transcript, Agent messages, metadata, knowledge, deterministic
  output, and model response. Assert no scope/rubric/mode/tool/memory/Release
  change and no unapproved source disclosure.
- [ ] Independently validate every model response for schema, exact criterion
  set, configured types/ranges, aggregation, citation resolution, protected-
  content policy, deterministic conflicts, rationale, confidence/uncertainty,
  and provisional feedback before completion.
- [ ] Persist protected provider request/response references and bounded
  attempt outcomes, never full model output in queue, log, metric, audit, or
  error payloads.
- [ ] Green/refactor model adapter, disclosure minimization, retry, invalid
  response, conflict, insufficiency, and provider-profile gate tests.

## Phase 7 — Commit immutable Evaluation and Review handoff

- [ ] Red: prove partial Evidence/criteria/provider artifacts cannot become a
  completed Evaluation, audit/outbox failure rolls back authority, equivalent
  completion races reconcile, and conflicting completions raise integrity
  failure without overwrite.
- [ ] Implement one infrastructure/composition-owned completion transaction
  that reauthorizes
  service scope and exact inputs; re-verifies all criteria, Evidence locators,
  deterministic outcomes, aggregation, seals, lifecycle policy, and lineage;
  inserts immutable artifacts/provenance; appends the Evaluation manifest
  reference; accepts required audit/outbox; and invokes the Review-owned
  non-releasing handoff port. Evaluation application/domain ports must not
  expose `NpgsqlTransaction`, Dapper, SQL, or Review implementation types; the
  Review infrastructure adapter owns Review writes inside the shared primary-
  store transaction.
- [ ] Create or refresh only an `evaluation_available` Review handoff/case
  state. When the case has no candidate and exactly one eligible Evaluation,
  the Review-owned adapter records one immutable exact initial-candidate event
  with a bounded reason and durable audit. It must not assign a human; use
  arrival order or `latest`; switch a replacement; create a revision or
  decision; construct a Result; emit a Participant notification; or change
  Participant visibility.
- [ ] Implement equivalent post-completion retry as read/reconcile of the same
  authority. Implement replacement as a separately authorized request with one
  exact predecessor, bounded approved reason, actor/service, time, input
  identity, and disposition; never mutate the original.
- [ ] Publish a replacement-available event to Review so its current exact
  candidate is marked stale/attention-required when applicable. Preserve that
  selected candidate and block any future decision eligibility; do not switch
  Review candidate authority in Evaluation or this task's UI.
- [ ] Implement append-only annotations/current disposition for later source
  integrity or lawful availability findings while retaining the historical
  completion state and original locator.
- [ ] Assert at database, domain, API, and end-to-end levels that completion and
  replacement create no Human revision, Review decision, Result, Release,
  notification, memory, learning, calibration, or harness record.
- [ ] Green/refactor completion, rollback, concurrency, replacement, annotation,
  lineage, manifest/audit reconstruction, and prohibited-side-effect tests.

## Phase 8 — Host APIs and active-assignment authorization

- [ ] Define Evaluation service actions separately for admit/claim/execute/
  complete/retry/replace/annotate/status/read/read-Evidence and human actions
  for assigned Review list/read/open-Evidence. Bind every action to complete
  resource scope and current workflow state.
- [ ] Add a minimal Review-owned case/handoff/initial-candidate/assignment-read
  port. A generic
  `reviewer` relationship is never sufficient: assigned reads require an active
  exact case assignment and content capability, with current reauthorization
  for the case and again for each Evidence source.
- [ ] Use Development/Testing seed data or test-only trusted setup to create an
  assignment for browser verification. Do not add production assignment
  policy, an automatic assignee, or a user-facing assignment command in this
  task.
- [ ] Add bounded authenticated endpoints for Review work list/status, exact
  case Evaluation/criterion reads, and subordinate Evidence opens. Scope list
  and count before materialization; use indistinguishable non-disclosing
  denial/not-found behavior where governed. Enforce server-owned pagination,
  maximum page/criterion/Evidence context sizes, request/rate limits, and
  bounded surrounding-source expansion so enumeration or repeated locator
  opens cannot become a disclosure or resource-exhaustion path.
- [ ] Add separately authorized operational endpoints/handlers for exact
  retry/reconciliation, replacement, and annotation only when the actor has the
  required action, complete resource chain, reason, expected version, and
  current authentication strength. Do not expose arbitrary export.
- [ ] Red: API tests for signed-out, Participant, unassigned Reviewer,
  wrong/revoked/expired assignment, wrong Organization/Activity/Participant/
  Attempt/Session, guessed IDs, missing content capability, stale workflow,
  malformed schema, conflicting idempotency/expected version, and revocation
  between admission/read/commit.
- [ ] Ensure response projections never contain raw hidden prompts, credentials,
  provider request/response bodies, unrestricted config/rubric internals,
  unrelated source content, or Participant-visible outcome fields.
- [ ] Add exact narrow gateway proxy rules for only the new API namespace and
  negative tests that broader `/v1` paths remain unavailable.
- [ ] Green/refactor Runtime/API, IdentityAccess, gateway, and PostgreSQL
  authorization/isolation tests.

## Phase 9 — Production reviewer inspection UI

- [ ] Red: route/layout tests must first show `/review` and
  `/review/:reviewId` are unavailable and not assigned to the required
  management/guided-task families.
- [ ] Assign `/review` to `management` and `/review/:reviewId` to
  `guided-task`; preserve Production shell ownership, destination guards, and
  deep-link authorization.
- [ ] Build a typed Evaluation/Review query client with TanStack Query. Keys
  must include current Organization/actor context and exact Review case/
  Evaluation identity; purge protected caches on sign-out/context/assignment
  loss, cancel in-flight reads, and render no prior protected data while access
  is unresolved. Do not persist protected Evaluation/Evidence data in
  `localStorage`, `sessionStorage`, IndexedDB, service-worker caches, URLs,
  analytics, or browser logs. Do not use optimistic updates or client-owned
  authority.
- [ ] Implement a read-only Review work list for authorized assigned cases with
  bounded status counts/filters and honest empty/loading/error/denial states.
  Do not add claim, reassign, candidate, revision, decision, or Release
  controls.
- [ ] Implement the assigned-record hierarchy: case header, urgent status,
  exact selected Evaluation/provenance summary, criterion navigation, active
  criterion, Evidence references, and history/annotation. Show named timezone
  for authoritative times. Keep disabled/ineligible
  downstream stages semantically absent or explicitly unavailable rather than
  visually active.
- [ ] Use exact labels and state copy from the approved UI specification,
  including **Internal Evaluation · Not a released Result**,
  **Evaluation running. Criterion judgments are not available until
  completion.**, **Open Evidence**, and **Back to criterion**.
- [ ] Present modes as **Rule-based**, **Agent-assisted**, and **Agent
  judgment**, with canonical values in technical provenance. Keep confidence,
  uncertainty, insufficiency, lower precision, integrity, and provisional
  feedback explicit and never rely on color alone.
- [ ] Open Evidence deliberately in a subordinate view/route, reauthorize on
  the server, show exact source/version/location/verification context, render
  content inertly, never auto-fetch external links, and restore focus to the
  originating Evidence reference on **Back to criterion**.
- [ ] Red/green component tests for all applicable states, semantic headings/
  landmarks, list/detail keyboard navigation, focus restoration, live-region
  announcements without score disclosure, unsafe markup, cache purge,
  assignment loss, responsive stacking, reduced motion, and forced colors.
- [ ] Attach to an existing healthy candidate origin (`:5274` plus healthy
  Compose `:18080`) or start only the documented origin if down. Match OIDC
  redirect, sign in with synthetic Reviewer data, and use real API interactions.
- [ ] Use Playwright MCP accessibility snapshots and mandatory screenshots at
  desktop, narrow viewport, and 400% zoom for loading, empty, queued/running,
  retryable failure, review-required, completed, insufficient/not-applicable,
  lower-precision Evidence, denied/unavailable/integrity-changed source,
  assignment loss, keyboard focus/return, reduced motion, and forced colors.
  Keep artifacts under `.playwright-mcp/` and out of tracked records unless
  deliberately committed after inspection.
- [ ] Return a switched healthy API to canonical redirect with
  `pnpm compose:api:canonical`; do not reseed or tear down the user's healthy
  stack.

## Phase 10 — Worker composition, operations, lifecycle, and performance

- [ ] Add Evaluation as a distinct bounded Worker lane with fair scheduling
  beside Session invocation/timer/expiry work. Report lane-specific enabled,
  fail-closed, identity, adapter qualification, backlog, and readiness states.
- [ ] Prevent Evaluation claim or protected disclosure while workload identity
  is unavailable, expiring, revoked, out of delegated scope, or unqualified.
  Reauthorize at claim, source read/model disclosure, lease renewal as needed,
  and completion.
- [ ] Add bounded telemetry with allowlisted labels for status response,
  queue/claim/lease, time-to-completion, attempts, evaluator/provider outcomes,
  citation failures, conflict/review-required, audit rollback, replacement,
  annotation, and deletion. Add tests that raw content and unrestricted IDs are
  absent.
- [ ] Resolve lifecycle policy before creating Evaluation records. Apply the
  approved Activity-closure 365-day default to Evidence/Evaluation lineage,
  the 90-day defaults to terminal work/idempotency as applicable, and legal-
  hold/dependency-safe disposition without breaking audit or historical
  explanation. Test ordinary mutation denial separately from authorized
  lifecycle expiry, hold, backup-expiry disclosure, and minimum-provenance
  preservation.
- [ ] Use the repository's platform-managed encryption in transit and at rest
  for protected payloads, database/object storage, temporary evaluator data,
  and backups. Do not introduce application-designed cryptography; verify
  deployment/profile enforcement and secret exclusion instead.
- [ ] Add representative bounded load fixtures for deterministic-only,
  Agent-assisted, and Agent-judgment work. Measure 95% status within 2 seconds
  and 95% completion within 120 seconds, excluding a declared provider-wide
  outage as specified; record queue/provider/retry/review-required outcomes
  separately.
- [ ] Add recovery probes for Worker restart, expired lease, provider timeout,
  storage/audit outage, duplicate completion, and Review-handoff reconciliation.
- [ ] Update Compose Development/Testing seed and Worker settings only for the
  bounded synthetic profile. Keep Production/Staging Evaluation/model lanes
  default-off and fail closed.

## Phase 11 — Reconcile, review, and promote durable truth

- [ ] Run focused suites after each red/green slice, then the full applicable
  regression gates: `dotnet test`, `pnpm verify:web`, contract/JCS checks,
  PostgreSQL integration/migration-upgrade tests, gateway/OCI checks,
  authenticated browser/OIDC checks when the environment is available, and
  `python3 scripts/check_docs.py`.
- [ ] Recheck every governing specification and the requirement-to-surface map.
  Record any requirement not implemented or verified; do not convert a partial
  feature into a completion claim.
- [ ] Perform distinct backend, frontend, security/privacy, and QA review
  handoffs. Reviewers must inspect immutable lineage, authorization/isolation,
  source disclosure, evaluator isolation, provider trust, audit/lifecycle,
  responsive/accessibility evidence, and the no-Result/no-Release invariant.
- [ ] Resolve all blocking findings, rerun affected tests and browser states,
  and record durable evidence references in this file without raw sensitive
  output or untracked screenshot paths.
- [ ] Update `docs/current-state.md` only to the precise implemented/partial
  classification supported by code/tests and completed review. Update approved
  architecture or operations documents only if implementation discovers a
  durable contract change; do not treat this task file as authority.
- [ ] Reconcile planned work with actual changes, mark `status: completed` only
  when the entire scoped outcome and review are complete, then retire this file
  after durable truth is promoted. Git preserves the implementation history.

# Implementation decisions

- Evaluation is a new bounded module because it owns request/invocation,
  evaluator execution, Evidence, immutable Evaluation, replacement, annotation,
  and completion behavior. It does not become a subdomain of Sessions or
  Review.
- Existing Session handoffs are consumed as immutable upstream facts. Their
  `procedure_id` means manifest-seal procedure; the evaluation procedure is
  resolved from the exact rubric source frozen in the resolved configuration.
- Canonical rubric bytes are persisted only for new source versions. Historical
  digest-only versions are never backfilled with guessed content and cannot
  produce a completed Evaluation.
- Initial Evaluation request identity is the exact eligible handoff plus a
  canonical frozen-input digest. Worker attempts are retry history under that
  request; replacement is a new request bound to exactly one predecessor and
  authorized reason.
- All external model and deterministic work occurs outside the primary
  transaction. Admission and completion are short authoritative transactions;
  completion revalidates exact inputs and required authority.
- Evidence content stays in the owning protected source. Evaluation stores
  exact source/version/digest/locator metadata and retrieves content through
  separately authorized owner ports.
- Deterministic evaluator output is protected Evidence, not policy. The Agent
  may interpret it only in `agent_assisted` mode and can never replace or
  recompute an exact fact or aggregation result.
- The first evaluator allowlist is deliberately small and built-in. No generic
  expression language, participant code, external evaluator, unrestricted
  script, or network-capable runner is introduced.
- Evaluation uses a separate Worker lane and provider-neutral contract. It may
  share stable protocol plumbing with Sessions only after architecture tests
  prove the dependency does not couple domain semantics.
- Evaluation frozen model identity is owned by Evaluation and copies the exact
  profile id/version/digest plus credential binding from the Session RSC. It
  does not reference Sessions types or resolve a mutable profile name.
- Evaluation completion calls a narrow Review-owned transaction adapter to
  publish `evaluation_available`; it does not write Review tables directly.
  The Review adapter may record the exact initial candidate only for a case
  with no candidate and one eligible Evaluation. Replacement never switches
  the candidate, and the adapter never assigns a reviewer.
- Assigned-reviewer UI is read-only in this task. Development/Testing may seed
  an active assignment for verification, while production assignment behavior
  remains owned by the later Human Review/Result Release item.
- `/review` is the management index; `/review/:reviewId` is the guided assigned
  record. Design Lab supplies visual/composition evidence only and is never a
  production dependency.
- Evaluation status uses bounded request/response polling through TanStack
  Query unless measured requirements justify another approved transport. No
  new SSE authority is introduced by default.
- Participant non-disclosure is enforced by absence of Participant routes and
  by server authorization, not by hidden controls or client filtering.
- Session handoff outbox delivery is a wake-up, not authority. A durable inbox,
  authoritative owner-port reload, idempotent admission, and bounded
  reconciliation scan jointly prevent duplicate or stranded work.
- Ordinary mutation and lifecycle disposition are separate authorities.
  Append-only records reject normal update/delete while an approved,
  auditable, hold-aware lifecycle path can expire or minimize data without
  fabricating history.

# Current state

Activated on 2026-09-07 at `15ae379` (`main` / `origin/main`). Phase 1 frozen-
input prerequisites are implemented and approved on `b728d71`. Phase 2 added
the Evaluation module, architecture boundaries, domain validators, and
fail-closed admission. Phase 3 core persistence/admission/recovery is approved
on `13fd2f3` with hardening follow-up on `4e2fb53` and fault-matrix closure on
`0074`/`0075`. Developer review on `909c624` approved Phase 3 complete
(0 Blocker / 0 High / 0 Medium).

- Catalog family is present (procedure/request/work/artifact/review-read).
  Internal work/provider/protected-artifact contracts stay out of OpenAPI/TS.
- Additive `0071_configuration_source_payloads` persists canonical UTF-8 with
  new source versions. Payload rows are bound to the exact version
  `(organization_id, configuration_source_id, id)`. Public metadata list/count
  stays content-free. Ordinary UPDATE/DELETE is denied; inactive holds cascade
  on authorized `dispose_configuration_source_payload`, while active holds
  still block.
- Assessment descriptor reads set payload-presence from the join and load
  canonical bytes only for `rubric_evaluation`.
- `EvaluationProcedureDocumentParser` plus
  `IProtectedConfigurationSourcePayloadReader` own typed procedure load.
  Owner-port reads are organization- and source-scoped.
- Development/Testing rubric version `33333333-…3316` has real digest
  `8f1d3f5fc630bab60b48a29bfa915f159cd4b2136839ccc705ae8737b40327e2`. Historical
  placeholder `…3306` / `g…` remains without payload and is not the available
  descriptor.
- Assessment readiness blocks missing rubric payload
  (`assessment.unavailable_source`) and invalid procedure
  (`assessment.invalid_procedure`). Attempt/RSC require `rubric_evaluation`.
- Consistency review on 2026-09-07 fixed payload FK binding, inactive-hold
  dispose, rubric-only byte load, owner-port isolation tests, and the missing
  JCS `fixture.json` for the synthetic evaluation-procedure bytes.
- Phase 1 approved on `b728d71`. Evaluation processing remains fail-closed.
- Phase 2 added `FlexAgent.Evaluation` and `FlexAgent.Evaluation.Infrastructure`
  with architecture tests. Domain aggregates validate ownership, exact frozen
  input, procedure, judgments, Evidence, completion, lineage, and annotations.
  `FrozenModelIdentity` requires exact profile id/version/digest plus
  credential binding; names and `latest`/`current` aliases fail as
  `evaluation.unqualified_model`.
- Infrastructure remains host-fail-closed (`ProcessingEnabled = false`;
  `DisabledEvaluationAdmission` returns `evaluation.processing_disabled`). No
  host or provider adapter is wired. `0072` now owns Evaluation requests,
  attempts, durable work, protected artifact references, Evidence/completion
  records, lineage, annotations/dispositions, manifest references, and the
  minimal Review handoff.
- Phase 2 review on `f412926` required two Mediums before `0072`: Evidence
  ownership equality and a distinct `requirements_not_satisfied` aggregate.
  Those are now encoded in domain validators, `evaluation.v1`, and negative
  parent-chain tests.
- Session emits `session.evaluation_handoff.recorded.v1`; the delivery handler
  reloads through the Session-owned port, stores a duplicate-safe durable inbox,
  and supports a bounded tuple-cursor reconciliation scan.
- Admission computes `evaluation-frozen-input-jcs-sha256-v1`, then commits the
  request, work, audit, and outbox atomically. Equivalent retries reconcile;
  conflicting idempotency inputs and ineligible/cross-scope bindings fail
  closed. Durable work has positive bounds, Organization backlog locking,
  Organization-aware fair claims, leases, renewal, retry, exhaustion, and
  expired-lease recovery.
- Next: Phase 5 — restricted deterministic evaluator lane (registry port,
  built-in allowlist, qualification gates, and bounded execution). Phase 4 is
  complete with work-trace owner port/materialization explicitly deferred until
  Session durable persistence exists; interim fail-closed via empty
  `WorkTraceItemsBySourceId` is approved. Phase 4 gate closed 2026-09-09:
  `verify-dotnet.sh` 2201 passed / 4 skipped; `scripts/check_docs.py` passed.
  Phase 4 slice 3 approved 2026-09-09 through `4a6a152` + `88b293f` (review:
  0 Blocker / 0 High / 0 Medium). Phase 4 slice 4 approved 2026-09-09 through
  `c97715e` + `7d16935` (review: 0 Blocker / 0 High / 0 Medium; work-trace
  masquerade High closed). Phase 3 is complete through `909c624`.
  Do not resolve evaluator/model identity by profile name. Do not weaken the
  fail-closed physical lifecycle-disposal boundary to finish faster.
- Phase 4 foundation (`437401b` + `2461466`) approved 2026-09-08: 0 Blocker /
  0 High / 0 Medium on corrective commit. Session owner port cutoff-scopes
  participant material via authoritative `admitted_session_sequence`; UTF-8
  byte ranges require scalar boundaries and strict decode. Integration negatives
  cover post-cutoff, orphan, cancelled, and handoff/runtime drift cases.
  Verification: `FlexAgent.Evaluation.Tests` 80 passed; architecture 65 passed;
  `verify-dotnet.sh` green. Hosted CI not independently observed. Phase 4 slice 2
  approved 2026-09-08 through `9e286f9` + `1932276`: 0 Blocker / 0 High /
  0 Medium; previous fallback integrity High closed. Focused verification:
  `FlexAgent.Evaluation.Tests` 108; evaluation Postgres integration 11;
  architecture 65; `verify-dotnet.sh` 2181 passed / 4 skipped; `check_docs.py`
  passed. Hosted CI not independently observed.

The only other active task is `text-interaction-controller-contract`
(`planned`, not activated).

## Upstream rollout checklist (Evaluation-required behaviors)

| Upstream spec | Evaluation-required behavior | Evidence | Gate |
| --- | --- | --- | --- |
| `auth-resource-isolation` | Service identity, delegated scope, commit reauthorization, human OIDC | IdentityAccess kernel, grants, OIDC; generic `reviewer` relationship only; no active Review assignment | Processing disabled until assigned-read port exists |
| `resolved-session-configuration` | Exact frozen sources, model profile, non-secret credential binding, manifest | RSC freeze requires `rubric_evaluation`; canonical JSON keeps exact model profile id/version/digest and credential binding | Attempt start fail-closed without rubric; Evaluation binding still uses the frozen Session model identity rather than a name lookup |
| `assessment-setup` | Activated baseline with exact rubric/procedure source | New evaluable rubric version has canonical payload; readiness parses procedure and allowlisted evaluators before activation | Historical placeholder-digest versions stay evaluation-ineligible |
| `submission-attempts` | Exact accepted Submission version/item binding | Immutable accepted-version metadata and object bytes exist; no Evaluation owner port | Add delegated-service port; do not query tables directly |
| `session-text-lifecycle` | Terminal Completed handoff, cutoff, transcript, seal | `session_evaluation_handoffs` append-only FK-bound handoff exists | Consume as wake-up plus owner-port reload |

## Development/Testing verification identities

- Evaluation procedure fixture id: `evalproc.p0.text.synthetic`
- Built-in evaluators: `eval.builtin.bounded-calc`, `eval.builtin.schema-validate`,
  `eval.builtin.exact-compare`, `eval.builtin.citation-validate`,
  `eval.builtin.rubric-aggregate`
- Service actor: Evaluation service principal with delegated Session/Evaluation scope (no human credential)
- Review assignment: Development/Testing seed only; no production assignment command
- Model profile: existing Development `deterministic_fake` / synthetic adapter; Production/Staging remain fail-closed

# Findings / deviations

- The predecessor gate cleared before this plan was created: hosted text
  Session is retired at `15ae379`, rather than remaining active as the earlier
  repository snapshot suggested.
- The upstream terminal handoff is stronger than a loose event: it is
  append-only and foreign-key bound to the exact terminal record, lifecycle,
  cutoff, configuration, manifest, and seal. Evaluation should consume this
  seam rather than reconstruct eligibility from Session projections.
- The handoff's field named `procedure_id` is specifically a manifest-seal
  procedure. Treating it as the rubric/evaluation procedure would be an
  integrity bug; the plan makes the two identities explicit.
- Configuration registration verifies canonical bytes and now persists them in
  `configuration_source_payloads` for new versions. Historical digest-only
  versions remain without payload and cannot produce a completed Evaluation.
- New resolved Session configurations require `rubric_evaluation`. An
  Evaluation-capable start fails closed when that source is absent.
- Existing generic Reviewer relationship support is not an active Review
  assignment. The Evaluation read path needs a narrow Review-owned assignment
  check and must not reuse a role label as authority.
- The approved reviewer interaction specification covers the entire later
  Human Review journey. This task implements only its Evaluation-processing,
  criterion, Evidence, provenance, integrity, assignment-loss, accessibility,
  and responsive subset; decision/revision/Release behavior remains absent.
- No product, requirements, UI/UX, or architecture question blocks the core
  implementation. The detailed source procedure schema, database layout,
  endpoint names, polling interval, and built-in evaluator code are reversible
  implementation choices constrained by the approved contracts above.
- The Evaluation rollout section requires all five upstream P0 dependencies to
  be implemented, while `docs/current-state.md` currently classifies each as
  Partial. This does not prevent red/green implementation work, but it does
  prevent enabling Evaluation or claiming the whole feature complete until the
  exact dependency gate is evidenced.
- Phase 2 created `FlexAgent.Evaluation.Infrastructure` before Npgsql exists
  because architecture tests and fail-closed admission need a composition
  target. The assembly stays package-free and returns
  `evaluation.processing_disabled` rather than a no-op placeholder.
- `evaluation.v1` aggregate_status now includes `requirements_not_satisfied`
  so `all_required_satisfied` can record an explicit criterion failure without
  collapsing it into `insufficient_evidence`. Conflict and insufficiency keep
  precedence.
- Evidence set creation requires an expected `EvaluationOwnership`; mixed
  parent-chain items and completion against a different frozen-request
  ownership fail as `evaluation.incomplete_ownership`.
- Phase 3 integration exposed a Phase 2 persistence mismatch: the Session-owned
  handoff identity is a contract `stable_id`/database `TEXT`, not a UUID.
  `FrozenInputIdentity` now preserves that exact string and separately carries
  terminal-record id, terminal state/cutoff/seal, configuration id/digest, and
  manifest id/digest. This prevents treating the terminal seal digest as the
  resolved manifest digest.
- Phase 3 lifecycle disposal uses `dispose_evaluation_provider_artifact` on
  migration `0074`, hardened by `0075`. Disposal locks the owning Evaluation row
  for the transaction, hold INSERT/UPDATE serializes on the same lock, requires
  a current `evaluation.lifecycle.dispose` delegation plus matching audit, and
  is executable only by `flexagent_lifecycle_executor` (revoked from
  `flexagent` / `flexagent_application`). Active legal holds block disposition;
  inactive holds do not. Integration covers hold/disposal races, role denial,
  and delegation revocation negatives.
- Combined Phase 3 review on 2026-09-08 approved `13fd2f3` (core
  persistence/admission/durable recovery) and `4e2fb53` (security-review
  hardening) together: 0 Blocker / 0 High / 0 new Medium; no corrective
  commit required. GitHub combined-status endpoint returned no status records
  for either SHA, so CI green was not independently verified from that endpoint.
- CI/confirmation review on 2026-09-08 approved `3c0c1c3` (lockfile restore and
  migration-upgrade tail through `0073`) and `4a2a86a` (confirmation evidence):
  0 Blocker / 0 High / 0 Medium; no corrective commit required. Hosted GitHub
  Actions for `4a2a86a` was not independently observed; repository-recorded
  local/CI-equivalent verification is internally consistent.
- Phase 4 closes with Session work-trace owner port/materialization explicitly
  deferred: Evaluation allowlists `session.work_trace`, isolates verification
  through separate `WorkTraceItemsBySourceId`, and leaves the owner builder
  collection empty fail-closed until Session module durable work-trace
  persistence exists per `REQ-SESS-52`. External review accepted this interim
  default; it is a recorded gap, not a Phase 4 blocker, and does not prevent
  Phase 5 entry.

# Readiness review

Second cross-cutting review completed on 2026-09-07 using the backend,
frontend, and security/privacy reviewer perspectives. The review corrected the
following plan defects before implementation:

- Added omitted approved decision `EVAL-DEC-8` and made exact initial versus
  replacement candidate behavior consistent across Evaluation and Review.
- Separated internal work/provider/protected-artifact contracts from public
  OpenAPI and browser TypeScript projections.
- Replaced the ambiguous handoff `owner port or outbox` choice with a durable
  inbox, versioned wake-up event, authoritative Session-owner reload,
  idempotent admission, and reconciliation scan.
- Reconciled append-only history with approved lifecycle expiry by separating
  ordinary mutation denial from an auditable, hold-aware disposition path.
- Removed advance reservation of migration `0071`; implementation takes the
  next available number at activation.
- Added pre-activation evaluator/model/lifecycle validation, exact Evaluation
  model-binding proof, per-Organization backpressure/fairness, restricted
  temporary-file handling, browser-cache/content controls, bounded query and
  source-expansion limits, named-timezone display, and platform-managed
  encryption verification.
- Made the five-upstream-spec rollout gate explicit. Core implementation can
  begin, but enablement and a complete-feature claim cannot bypass current
  upstream gaps.

No unresolved plan-level Blocker or High finding remains. Frontend visual
quality is not reviewable yet because this change is a plan only; live
accessibility snapshots and screenshots remain mandatory during Phase 9.

# Open questions

None at planning time. If implementation uncovers a material ambiguity in
product meaning, observable behavior, UI interaction, security/privacy, or
architecture, stop that affected step and record an explicit question with an
interim default and rationale in the owning authority before proceeding.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Predecessor completion/retirement | complete | `15ae379` retires `hosted-text-session`; implementation/review target `12bfc65` |
| Governing product, requirements, architecture, UI/UX, design-system, operations, and harness inventory | complete | Reconciled on 2026-09-07 against the files listed under Governing sources |
| Current implementation seam inventory | complete | Inspected Session handoff/terminal/migration code, RSC/manifest construction, Configuration registration/source storage, Submission exact-version ports, Worker/API composition, contracts, routes, and Design Lab donors at `15ae379` |
| Requirement/AC-to-surface map | complete | All `REQ-EVAL-1`–`REQ-EVAL-53` and `AC-EVAL-1`–`AC-EVAL-38` grouped above with implementation and evidence targets |
| Plan documentation validation | complete | `python3 scripts/check_docs.py` passed on 2026-09-07; `git diff --no-index --check /dev/null .work/active/evidence-evaluation.md` reported no whitespace diagnostics (exit `1` only because the new file differs from `/dev/null`) |
| Second cross-cutting readiness review | complete | Backend ownership/concurrency/contracts, frontend route/state/accessibility/security, and security/privacy trust boundaries reviewed on 2026-09-07; corrections recorded under Readiness review |
| Focused red-green-refactor evidence | Phase 2 review-fix complete | Ownership/aggregation red (11 failing cases: mixed parent-chain Evidence accepted; `not_satisfied` → `insufficient_evidence`) then green |
| Contract/JCS and architecture tests | Phase 2 review-fix complete | `FlexAgent.Evaluation.Tests` 51 passed; `EvaluationContractCatalogTests` 4; `ContractCatalogTests` valid/invalid fixtures 185; `python3 scripts/check_docs.py` passed |
| Phase 3 frozen-input and persistence red/green | approved core | `13fd2f3`: `0072`, frozen-input digest, admission/inbox/reconciliation, durable work claim/retry/recovery. Focused: Evaluation domain 54; schema 3; admission/recovery 11; architecture 65; Grate 13 |
| Phase 3 security-review schema follow-up | approved hardening | `4e2fb53`: additive `0073` composite annotation/audit provenance; expired final-attempt exhaustion on claim scan. Focused schema/provenance/admission 18 passed |
| Phase 3 combined review (`13fd2f3` + `4e2fb53`) | approved | 2026-09-08: 0 Blocker / 0 High / 0 new Medium; no corrective commit required |
| Phase 3 fault matrix (`0074` + `0075`) | approved | Developer review on `909c624`: 0 Blocker / 0 High / 0 Medium. Hold/disposal serialization, lifecycle-executor boundary, delegation proof, role/delegation/race negatives. Confirmation: Evaluation integration 39 passed; `verify-dotnet.sh` / `verify-web.sh` / docs green. Hosted CI not independently observed |
| Phase 3 CI restore (`3c0c1c3`) | approved | Refreshed NuGet lock files for Sessions→Evaluation dependency; extended migration upgrade tail through `0073` without weakening assertions. Review 2026-09-08: 0 Blocker/High/Medium |
| Phase 3 migration and architecture regression | approved | 2026-09-08: `verify-dotnet.sh` 2095 passed / 4 skipped; `verify-web.sh` green; `check_docs.py` passed; architecture 65; Postgres integration including migration upgrade 415. Recorded on `4a2a86a`; hosted CI not independently observed |
| Phase 4 slice (`0514a70` + `235f7ed` + `895a30a`) | approved | Combined developer review 2026-09-08 on corrective slice: 0 Blocker / 0 High / 0 Medium; no further corrective commit required. `0514a70` review: 1 High + 1 Medium; `235f7ed` closes ownership binding and canonical-digest verification; `895a30a` records evidence. Focused: `FlexAgent.Evaluation.Tests` 94; session/submission Postgres integration 10; architecture 65; `check_docs.py` passed. Hosted CI not independently observed |
| Phase 4 slice 2 (`9e286f9` + `1932276` + `72ad277` + `2b70676` + `eb4be24`) | approved | Review 2026-09-08 on corrective chain: 0 Blocker / 0 High / 0 Medium; previous fallback integrity High closed. `9e286f9`: procedure gating, locator persistence, seal reorder/drift, mutable-alias/work-trace negatives. Review found 1 High — fallback sealed failed exact-range digests. `1932276`: effective `whole_item` location for seal/persistence via `EvidenceLocatorVerifiedProjection`. Evidence commits `72ad277`, `2b70676`, `eb4be24` record corrective/confirmation state without premature approval. Focused: `FlexAgent.Evaluation.Tests` 108; evaluation Postgres locator store 1; architecture 65; `verify-dotnet.sh` 2181 passed / 4 skipped; `check_docs.py` passed. Hosted CI not independently observed |
| Phase 4 slice 3 (`4a6a152` + `88b293f` + `42c5d7f` + `bbb8b1d`) | approved | External review 2026-09-09 on corrective chain: 0 Blocker / 0 High / 0 Medium; fragment-cutoff High and incomplete-publication Medium closed. Focused: `FlexAgent.Evaluation.Tests` 116; `EvaluationSessionEvidenceSourceTests` 13; architecture 65. Hosted CI not independently observed |
| Phase 4 slice 4 (`c97715e` + `7d16935` + `53611d8`) | approved | External review 2026-09-09 on corrective chain: 0 Blocker / 0 High / 0 Medium; work-trace masquerade High closed. `c97715e`: forged excerpt, wrong version, unpublished material, cancelled agent negatives. Review found 1 High — shared transcript dictionary allowed work-trace masquerade. `7d16935`: separate `WorkTraceItemsBySourceId`; owner builder leaves empty; masquerade negative; dedicated-collection positive when populated. `53611d8` records confirmation evidence. Focused: `FlexAgent.Evaluation.Tests` 121; `EvaluationSessionEvidenceSourceTests` 14; architecture 65; `check_docs.py` passed. Work-trace owner port still deferred. Hosted CI not independently observed |
| Phase 4 gate (proportionate regression) | complete | 2026-09-09: `build/scripts/verify-dotnet.sh` 2201 passed / 4 skipped; `python3 scripts/check_docs.py` passed. Work-trace owner port/materialization remains deferred (accepted gap per Findings). Hosted CI not independently observed |
| Phase 4 foundation (`437401b` + `2461466`) | approved | Developer review 2026-09-08: 0 Blocker / 0 High / 0 Medium on corrective commit. Owner ports, locator verifier, seal computer, cutoff-scoped Session transcript, UTF-8 boundary checks. `FlexAgent.Evaluation.Tests` 80; architecture 65; `verify-dotnet.sh` green. Hosted CI not independently observed |
| API/gateway negative and authenticated integration tests | pending | Populate during implementation |
| Frontend component/accessibility/responsive tests | pending | Populate during implementation |
| Playwright MCP accessibility snapshots and desktop/narrow/400% screenshots | pending | Required during UI implementation; keep local artifacts under `.playwright-mcp/` unless deliberately committed |
| Performance objectives (`PROP-6`) | pending | Representative status/completion measurements required before completion |
| Full regression and OIDC/Compose gates | pending | Run proportionately when implementation reaches integration readiness |
| Independent backend/frontend/security/privacy/QA review | pending | Required after implementation and before completion/retirement |

# Blockers

None for beginning implementation after explicit user activation.

Rollout and a full-feature completion claim remain blocked until the five
upstream P0 dependency surfaces pass the approved gate described above.
Real-provider Production enablement remains separately gated and is not
required to begin or to verify the bounded Development/Testing slice.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] The five upstream P0 rollout dependencies are implemented and integration-green for every Evaluation-required behavior; otherwise Evaluation remains disabled and this task is not claimed fully complete
- [ ] Every `REQ-EVAL-1`–`REQ-EVAL-53` and `AC-EVAL-1`–`AC-EVAL-38` target is implemented and evidenced, or an explicit remaining gap prevents a completion claim
- [ ] Existing handoff, frozen-input, Submission, transcript, manifest, and owner-module boundaries remain regression-green
- [ ] Deterministic, Agent-assisted, and Agent-judgment modes pass positive, failure, injection, conflict, isolation, and provenance tests
- [ ] Evaluation completion/replacement/annotation history is immutable, reconstructable, audit-gated, and creates no Review decision, Result, Release, notification, or learning record
- [ ] Applicable focused tests pass
- [ ] Applicable integration, migration-upgrade, concurrency, fault, security, accessibility, responsive, performance, and regression checks pass
- [ ] Required Playwright MCP screenshot evaluation is complete for all changed UI states
- [ ] Governing specifications are rechecked and durable discoveries are promoted to the correct owner
- [ ] Remaining gaps or unverified behavior are recorded honestly
- [ ] Distinct backend, frontend, security/privacy, and QA reviews have no unresolved blocker
- [ ] `docs/current-state.md` reflects only evidence-supported implementation status
- [ ] Task state is safe and complete for external review and retirement
