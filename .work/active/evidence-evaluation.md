---
id: evidence-evaluation
status: in-progress
created: 2026-09-07
updated: 2026-09-12T19:30:00+07:00
phase6_slice2_schema_parse: approved-83309967-9bbb6c35
phase6_slice2_schema_parse_review: approved-9bbb6c35-0-blocker-0-high-0-medium-0-low
phase6_slice2_schema_parse_ci: approved-34689229774-34689229816
phase6_slice2_provider_artifacts: confirmed-16da6ef2
phase6_slice2_provider_artifacts_review: approved-d9c7b6a5-16da6ef2-591f1381-0-blocker-0-high-0-medium-0-low
phase6_slice2_provider_artifacts_ci: approved-34614235430-34614235435
phase6_slice2_evidence_source_bookkeeping: approved-f3105a7b-1afd1cbb
phase6_slice2_evidence_source_bookkeeping_review: approved-f3105a7b-1afd1cbb-0-blocker-0-high-0-medium
phase6_slice2_credential_failclosed: confirmed-fb0c79fb
phase6_slice2_evidence_source: approved-8773c4f9
phase6_slice2_evidence_source_review: approved-8773c4f9-0-blocker-0-high-0-medium
phase6_slice2_evidence_source_ci: approved-34574753938-34574753257
phase6_slice2_injection: approved-5c28e565-2db28254
phase6_slice2_injection_review: approved-2db28254-0-blocker-0-high-0-medium
phase6_slice2_injection_ci: approved-34573333205-34573333223
phase6_slice2: in-progress-store-authority-approved-2466f1be-b8b5efad
phase6_slice2_store_authority: approved-2466f1be-b8b5efad
phase6_slice2_store_authority_review: approved-b8b5efad-0-blocker-0-high-0-medium
phase6_slice2_store_authority_ci: approved-34566336207-34566336176
phase6_slice2_review: approved-b8b5efad-0-blocker-0-high-0-medium
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
phase4_gate: approved-f89c35c
phase5_status: complete
phase5_slice1: approved-2ede5e1-2f3c699
phase5_slice2: approved-a53a5f5-d9011f9-ce63dbe-d4255c4
phase5_slice3: approved-4c803fe-5579ce7-395a7af
phase5_ci_review: approved-03056d0-be1b1d5-76d3496-7ee5293
phase5_slice4: approved-4acfa4b-a179b085-0c63e403
phase5_slice4_materialization: approved-5ff61ee-2209477-4422772
phase5_slice4_completion_linkage: approved-428c835-b3b37b4
phase6_status: in-progress-slice2
phase6_slice1: approved-8e0a5fc0-7bc3ff4f-78190660
phase6_slice1_review: approved-78190660-0-blocker-0-high
phase6_slice1_ci: approved-34554419421-34554419477
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
  passed). External review 2026-09-09 on `f89c35c`: 0 Blocker / 0 High /
  0 Medium; Phase 5 entry authorized. Session work-trace owner port/
  materialization remains an accepted deferred gap until Session durable
  persistence lands; fail-closed empty collection is interim default and does
  not block Phase 5 entry.

## Phase 5 — Implement the restricted deterministic evaluator lane

- [x] Define an Evaluation-owned evaluator registry port whose entries bind
  exact evaluator ID/version or verified digest, supported operation,
  input/output schemas, canonicalization/configuration/dependency digests,
  positive resource bounds, failure/conflict behavior, and qualification
  state.
  `IEvaluatorRegistry` + `EvaluatorRegistryEntry`/`EvaluatorRegistrySnapshot`;
  `BuiltinEvaluatorRegistry` serves `evalreg.p0.v1` and contract alias
  `evalreg.builtin.v1` with five qualified built-ins aligned to
  `EvaluationProcedureDocumentParser.AllowlistedEvaluatorIds`.
- [x] Start with a minimal built-in allowlist sufficient for the approved
  synthetic procedure: exact comparison/schema validation, bounded permitted
  calculation, citation validation, and rubric aggregation as actually
  declared. Do not add a general expression language or execute procedure/
  Participant strings.
  Built-in entries cover `eval.builtin.exact-compare`, `schema-validate`,
  `citation-validate`, `bounded-calc`, and `rubric-aggregate` with digests from
  `BuiltinEvaluatorImplementationManifest` v2 source-closure binding
  (`BuiltinEvaluatorImplementationBinding`).
- [>] Red: reject unknown/mutable evaluator identifiers, changed dependency or
  config digests, path traversal, shell/script/code fields, environment or
  secret access, network attempts, oversized/deep inputs, timeout, memory/CPU/
  output exhaustion, invalid output, wrong criterion/scope, and silent mode
  fallback.
  Slice 1: `EvaluatorBindingValidator` + `EvaluatorRegistryTests` cover unknown
  evaluator, mutable aliases, digest/operation drift, prohibited network egress,
  non-positive memory/output bounds, malformed durations, and memory bound
  exhaustion; synthetic procedure deterministic bindings validate against
  registry. Shared `EvaluationPositiveDuration` aligns parser and binding
  validation. Review on `2ede5e1` found 1 Medium — binding validator did not
  independently enforce positive bounds; `2f3c699` closes it with fail-closed
  positive memory/output/duration validation. External review 2026-09-09 on
  corrective chain: 0 Blocker / 0 High / 0 Medium. Slice 2 adds runner negatives
  for shell/path/digest/output-bound exhaustion; timeout and environment/secret
  runner negatives (`DeterministicEvaluatorRunnerTests`: `environment`, `env`,
  `secret`, nested `secret`). Slice 3 adds orchestration gate plus admitted-request authority
  reconciliation: `DeterministicEvaluatorAuthorityVerifier`,
  `IEvaluationRequestAuthorityStore`, `PostgresEvaluationRequestAuthorityStore`,
  `PostgresProtectedEvaluationProcedureSource`; execution reloads frozen
  `ProcedureRef` from persisted request scope (not caller-supplied procedure),
  verifies digest/source identity, then applies criterion/mode/binding gate.
  External review on `4c803fe`: 0 Blocker / 1 High — caller-supplied procedure
  treated as authority; corrective `5579ce7` removes bare `EvaluationProcedureV1`
  parameter and adds substitution/ownership/digest negatives. External review on
  `5579ce7`: 0 Blocker / 1 High — protected procedure bytes not independently
  bound to frozen digest (metadata-only reconciliation). Corrective pass adds
  `ProtectedEvaluationProcedureContentDigest`: recompute
  `CanonicalJsonProcessor.CanonicalizeSha256Hex` on loaded `canonical_utf8`
  with evaluation-procedure JCS limits and fail closed on
  `procedure_content_digest` mismatch before parse/orchestration/registry/runner/
  store; negative where source id/version and digest metadata are unchanged but
  bytes contain a different valid procedure. External review 2026-09-10 on chain
  `4c803fe` + `5579ce7` + `395a7af`: **approved**, 0 Blocker / 0 High /
  0 Medium; caller-supplied procedure and byte→digest Highs closed. Slice closed
  with docs chain `1193ec6` (approval record) + `6d1abc6` (confirmation evidence);
  external review 2026-09-10 on full chain through `6d1abc6`: **approved**, 0
  Blocker / 0 High / 0 Medium. Post-slice-3 CI chain `03056d0` + `be1b1d5` +
  `76d3496` + `7ee5293` externally reviewed **approved** 2026-09-10: 0 Blocker /
  0 High / 0 Medium; hosted **Implementation** run `34447801929` and
  **Documentation** run `34447801925` green at `7ee5293` (all six Implementation
  jobs including `supply-chain` and `oci-oidc-smoke`).
- [>] Run evaluators through a restricted adapter with no network egress by
  default and explicit positive bounds. If in-process built-ins cannot provide
  enforceable isolation for a permitted operation, use a separately bounded
  worker process/container contract before enabling that operation; do not
  claim sandboxing from application checks alone.
  `RestrictedBuiltinDeterministicEvaluatorRunner` re-validate registry bindings,
  canonical input digests, forbidden shell/script/path fields, and output limits;
  built-in memory-only execution for bounded-calc, exact-compare, schema-validate,
  citation-validate, and rubric-aggregate. Corrective #2 (`d9011f9` review):
  `InProcessDeterministicExecutionContract` (`in-process-algorithmically-bounded.v1`)
  uses cooperative wall-clock deadline (min of elapsed/cpu binding seconds) checked
  during scalar loops; `memory_limit_bytes` is documented as canonical-input size
  only, not execution heap. No network egress or temp files in slice 2 built-ins.
  Does not claim process-level CPU/memory kill or hung-evaluator preemption.
- [>] If an evaluator requires temporary files, allocate a per-invocation
  private directory with bounded size, safe filenames, no inherited secrets,
  no symlink/path escape, and guaranteed cleanup on success, failure, timeout,
  cancellation, and process restart. Prefer memory-only canonical inputs for
  built-in evaluators.
  Built-in path remains memory-only; separate temp-directory contract deferred
  until a built-in requires it.
- [>] Persist every deterministic invocation's exact canonical input digest/
  references, evaluator/config/dependency identity, limits, UTC timing,
  outcome, bounded failure, output digest/protected reference, and audit/
  manifest correlation.
  `IDeterministicInvocationStore` + `PostgresDeterministicInvocationStore` append
  to `evaluation_deterministic_attempts` with ownership-scoped FK to invocation
  attempts and idempotent retry on stable attempt identity;
  `DeterministicEvaluatorExecutionService` orchestrates registry lookup, run,
  and persist.
- [>] Treat successful output as protected Evidence. It is not policy or
  infallible truth, and Agent output cannot overwrite it.
  Runner records protected input/output refs and output content digest on
  success. Slice 4 (`0077` + `IProtectedDeterministicOutputStore` +
  `PostgresProtectedDeterministicOutputStore`): on succeeded invocation,
  `DeterministicEvaluatorExecutionService` persists bounded `output_utf8` keyed by
  `deterministic_attempt_id`, `protected_output_ref`, and `output_content_digest`
  with ownership-scoped FK to `evaluation_deterministic_attempts`; idempotent
  retry reconciles byte-identical payloads; digest/ref mismatch fails
  `deterministic_conflict`. External review 2026-09-10 on `5ff61ee`: **not approved**
  (0 Blocker / 1 High / 1 Medium). High: `0077` delete guard allowed caller-settable
  `flex_agent.allow_payload_disposition` bypass — regresses Phase 3 lifecycle model.
  Medium: independent `request_id` FK permitted impossible payload/attempt provenance;
  `TryLoadProjectionAsync` did not independently reconcile request/ref/outcome.
  Corrective `0078`: unconditional UPDATE/DELETE rejection; composite FK
  `(organization_id, request_id, deterministic_attempt_id)`; hardened load join on
  `payload.request_id`, `protected_ref`, and `attempt.outcome = succeeded`;
  `DeterministicPayloadImmutabilityTests` regressions. External review 2026-09-10 on
  `2209477`: **not approved** 0 Blocker / 0 High / 1 Medium — original High/Medium
  closed; duplicate `TryPersistAsync` retry reconciled ref/digest/bytes only, not
  request/ownership provenance. Corrective `4422772`: existing-row load joins
  attempt/request and fails `deterministic_conflict` on request or ownership
  mismatch before ref/digest/bytes; retry negatives added. External review
  2026-09-10 on corrective chain `5ff61ee` → `2209477` → `4422772` (with additive
  `0078` from `2209477`): **approved**, 0 Blocker / 0 High / 0 Medium. Payload
  materialization/provenance corrective work closed. Completion-linkage chain
  `428c835` → `b3b37b4`: store-backed context, Evidence-row persistence, criterion
  provenance on protected load. External review 2026-09-10 on `428c835`: **not approved**
  0 Blocker / 1 High / 0 Medium (cross-criterion reuse). Corrective `b3b37b4`:
  `TryLoadProjectionAsync` + loader bind criterion id/version to parent attempt;
  cross-criterion negative with zero Evidence rows. External review 2026-09-10 on
  chain `428c835` → `b3b37b4`: **approved**, 0 Blocker / 0 High / 0 Medium. Focused:
  `DeterministicFactContextLoaderTests` 5; `DeterministicEvidenceCompletionTests` 3;
  `verify-dotnet.sh` 2327 passed / 4 skipped. Hosted CI green at `b3b37b4`
  (Documentation `34480110188`; Implementation `34480110154` all six jobs). Slice 4
  runner negatives + Worker evaluation-lane scaffold landed locally (forbidden
  `environment`/`env`/`secret` tests; idle `IEvaluationDurableWorkProcessor` lane
  with config + compile-time fail-closed gate). External review 2026-09-10 on
  `4acfa4b`: **approved**, 0 Blocker / 0 High / 0 Medium. Hosted CI at `4acfa4b`:
  Documentation `34495832537` green; Implementation `34495832645` **failed** on
  unrelated `EnrollmentSharedAdmissionTests.Database_clock_window_and_cleanup_use_postgres_utc`
  (10-second window boundary race). Corrective `a179b085`: await
  `WaitUntilAwayFromAdmissionWindowBoundaryAsync()` before current-window insert;
  external review 2026-09-10 on `a179b085` + `0c63e403`: **approved**, 0 Blocker /
  0 High / 0 Medium. Hosted at `a179b085`: Documentation `34503812220` green;
  Implementation `34503812163` green — all six jobs (`changes`, `dotnet`, `web`,
  `oidc`, `supply-chain`, `oci-oidc-smoke`). Local confirmation at closure:
  `verify-dotnet.sh` 2337 passed / 4 skipped. **Phase 5 slice 4 closed.**
  Worker processing remains disabled.
- [>] Green/refactor evaluator registry, isolation, provenance, failure,
  aggregation, no-egress, and no-Session-tool-capability tests.
  Slice 2: `DeterministicEvaluatorRunnerTests` (shell/path/digest/output-bound/
  expired-deadline/cooperative-scalar-check negatives);
  `DeterministicInvocationStoreTests` integration idempotency + 4 conflict paths;
  `BuiltinEvaluatorImplementationBindingTests` (full v2 source-closure artifact
  digests + dependency-artifact identity regression).
  External review 2026-09-10 on chain `a53a5f5` + `d9011f9` + `ce63dbe` +
  `d4255c4`: **approved**, 0 Blocker / 0 High / 0 Medium. Focused verification:
  Evaluation 180; contract 264; architecture 65; `DeterministicInvocationStoreTests` 5.
  Slice 3 authority/orchestration: `DeterministicEvaluatorAuthorityVerifierTests` 9;
  `ProtectedEvaluationProcedureContentDigestTests` 2;
  `DeterministicEvaluatorOrchestrationValidatorTests` 7;
  `DeterministicEvaluatorExecutionServiceTests` 4; Evaluation 203. External review
  2026-09-10 on chain `4c803fe` + `5579ce7` + `395a7af`: **approved**, 0 Blocker /
  0 High / 0 Medium. Slice 3 closed; docs chain `1193ec6` + `6d1abc6` externally
  reviewed **approved** 0 Blocker / 0 High / 0 Medium. CI chain through
  `7ee5293` externally reviewed **approved** 2026-09-10: hosted Implementation
  run `34447801929` + Documentation run `34447801925` green (supply-chain and
  oci-oidc-smoke included). Remaining Phase 5 before Phase 6: completion-time
  Evidence-item linkage from materialized deterministic facts, environment/secret
  runner negatives, and lane integration/Worker enablement (still disabled).
  Slice 4 in progress: materialization corrective chain `5ff61ee` + `0078` +
  `2209477` +   `4422772` externally reviewed **approved** 2026-09-10 (0 Blocker /
  0 High / 0 Medium); hosted CI green at `4422772` (Documentation run `34459660148`;
  Implementation run `34459660500` — changes, OIDC, web, dotnet, supply-chain,
  oci-oidc-smoke). Completion-linkage chain `428c835` → `b3b37b4` externally
  reviewed **approved** 2026-09-10 (0 Blocker / 0 High / 0 Medium); hosted CI
  green at `b3b37b4` (Documentation `34480110188`; Implementation `34480110154`
  all six jobs). `4acfa4b` runner-negative + Worker-lane scaffold externally
  reviewed **approved** 2026-09-10 (0 Blocker / 0 High / 0 Medium); hosted CI at
  `4acfa4b` Documentation green (`34495832537`), Implementation **failed**
  (`34495832645`, dotnet: enrollment admission window-boundary race — unrelated).
  Corrective chain `a179b085` + `0c63e403` externally reviewed **approved**
  2026-09-10 (0 Blocker / 0 High / 0 Medium). Hosted at `a179b085`:
  Documentation `34503812220` green; Implementation `34503812163` green — all
  six jobs. **Phase 5 slice 4 closed.** Worker disabled.

## Phase 6 — Implement Agent-assisted and Agent-judgment execution

- [x] Define a provider-neutral Evaluation model port and strict request/
  response schemas. Include only fixed trusted policy/rubric fields, exact
  permitted Evidence/protected facts, bounded context, and non-secret frozen
  model provenance.
  Slice 1 **closed and approved** through chain `8e0a5fc0` → `7bc3ff4f` →
  `78190660` (external review 2026-09-11: **0 Blocker / 0 High / 0 Medium**).
  Canonical `evaluation-model-request.v1` / `evaluation-model-response.v1`
  schemas, fixtures, catalog, C# DTOs; `IEvaluationModelExecutionPort` consumes/
  returns canonical envelopes via `EvaluationModelRequestComposer` and response
  validation/mapping; `FailClosedEvaluationModelExecutionPort`,
  `EvaluationModelExecutionService`. Corrective chain closed authority binding,
  bidirectional verified-fact/protected-ref set equality, and permitted Evidence
  list/binding consistency. Hosted CI green at `78190660`: Documentation
  `34554419421`; Implementation `34554419477` — all six jobs (`changes`, `dotnet`,
  `web`, `oidc`, `supply-chain`, `oci-oidc-smoke`). No Worker wiring; no real
  provider adapters.
- [x] Keep Session generation and Evaluation judgment contracts separate.
  Extract only genuinely generic protocol/credential plumbing if dependency
  and architecture tests prove the abstraction is stable; otherwise add an
  Evaluation-specific adapter project.
  Slice 1 closed: Evaluation-specific port in `FlexAgent.Evaluation`; no Sessions
  `IModelExecutionPort` reference. Generic protocol extraction deferred to later
  slice when architecture tests prove stability.
- [x] Implement `deterministic` with no Agent call, `agent_assisted` only after
  required deterministic facts verify, and `agent_judgment` only for exact
  procedure-permitted criteria. Never substitute modes at runtime.
  Slice 1 closed: `AgentEvaluatorOrchestrationValidator` gates deterministic
  rejection, assisted requires verified facts, judgment rejects non-empty facts.
- [x] Add a bounded deterministic/synthetic Development/Testing model adapter
  that can exercise valid, insufficient, malformed, conflicting, timeout, and
  provider-failure paths without real data or network access.
  Slice 1 closed: `SyntheticEvaluationModelExecutionAdapter` scenarios
  (`timeout`, `provider_unavailable`, `schema_invalid`, `insufficient`,
  `conflict`, default satisfied). Schema parse gate **in progress** (slice 2):
  `EvaluationModelResponseDocumentReader` + embedded schema authority on execution
  path; aggregation/citation matrix still open.
- [x] Reuse workload credential source patterns without exposing secrets.
  Production/Staging adapter selection must fail closed unless exact
  qualification and workload identity are simultaneously valid.
  **Confirmation pass 2026-09-11 at `fb0c79fb`:** `EvaluationModelExecutionCompositionComposer`
  mirrors Sessions Worker compose gates without IdentityAccess dependency:
  default/unknown adapters fail closed; Production/Staging reject
  `synthetic_development` even when qualified with OAuth workload; Development/Testing
  synthetic requires verified `synthetic.configured_actor` workload, matching frozen
  P0 profile, and non-revoked catalog binding admission (secret name only, no secret
  load). `EvaluationFixtures.Model()` aligned to
  `EvaluationSyntheticDevelopmentModelProfile` constants. Focused:
  `EvaluationModelExecutionCompositionTests` 11; Evaluation unit 285;
  `EvaluationBoundaryTests` 6; `verify-dotnet.sh` green (local). Worker/processing
  remain disabled; no real provider adapter wired. Awaiting external review and hosted
  CI.
- [>] Red: prompt-injection and confused-deputy suites across Submission text,
  filenames, transcript, Agent messages, metadata, knowledge, deterministic
  output, and model response. Assert no scope/rubric/mode/tool/memory/Release
  change and no unapproved source disclosure.
  **Approved 2026-09-11** through chain `5c28e565` → `2db28254`: external review
  **0 Blocker / 0 High / 0 Medium**; closes Medium from `5c28e565` (instruction
  substring blacklist). `EvaluationProhibitedModelOutputDisclosurePolicy` scopes
  disclosure sentinels only; positive regressions prove rationale/feedback may
  describe hostile instructions while trusted fields stay frozen; structural
  confused-deputy negatives unchanged. Focused:
  `EvaluationModelPromptInjectionAndConfusedDeputyTests` 8;
  `EvaluationModelExecutionServiceTests` 13; Evaluation unit 265; `verify-dotnet.sh`
  green (local). Hosted CI at `2db28254`: Documentation `34573333205` green;
  Implementation `34573333223` green — all six jobs. **Model-boundary approved.
  **Approved 2026-09-11** through chain `8773c4f9` → `f3105a7b` (+ reconciliation
  `1afd1cbb`): external review **0 Blocker / 0 High / 0 Medium** on increment;
  bookkeeping chain `f3105a7b` → `f329933b` → `1afd1cbb` also **0 Blocker / 0
  High / 0 Medium** — closes `09c8f8e1` stale-CI documentation-state Medium,
  records hosted CI `34574753938` / `34574753257`, reconciles stale `2db28254`
  CI to green, and preserves wider AC-EVAL-24 matrix `[>]`. `8773c4f9`
  `EvidenceSourcePromptInjectionAndConfusedDeputyTests` 9 — submission/transcript/
  configuration hostile text verifies as untrusted data; completion seals with
  authoritative handoff; structural confused-deputy negatives (ownership, source
  type, criterion). Evaluation unit 274; `verify-dotnet.sh` green (local). Hosted
  CI at `8773c4f9`: Documentation `34574753938` green; Implementation
  `34574753257` green — all six jobs. **Evidence-source increment closed.**
  Wider `[>]` AC-EVAL-24 matrix (filenames, metadata, Agent messages, knowledge,
  model output) remains open at Phase 6 gate. **Slice 2 remainder:** credential
  fail-closed external review/CI for `fb0c79fb`; fuller response validation.
- [>] Independently validate every model response for schema, exact criterion
  set, configured types/ranges, aggregation, citation resolution, protected-
  content policy, deterministic conflicts, rationale, confidence/uncertainty,
  and provisional feedback before completion.
  Slice 1 foundation **approved** at `78190660`: `EvaluationModelResponseValidator`
  validates against explicit expected invocation identity (criterion/mode/output
  schema/deterministic invocation), deterministic conflict (`"valid":false` fact +
  `satisfied`), trusted draft mapping, and `CriterionJudgmentValidator`. **Slice 2
  schema parse gate approved/closed** (`83309967` → `09ca54ab` → `4609f7be` →
  `9bbb6c35`; external review **0 Blocker / 0 High / 0 Medium / 0 Low**):
  provider wire bytes → JsonSchema gate → document/adapter `response_ref` binding
  → semantic validation → artifact persistence; `output_semantic_invalid` distinct
  from `schema_invalid`; adapter-side `response_ref` on binding failure.
  Authoritative implementation head `9bbb6c35`; hosted CI Implementation
  `34689229774` and Documentation `34689229816` — all six jobs green; docs-only
  reconciliation `ad2105b8` (Implementation `34689256500` skipped implementation
  jobs). Focused: Evaluation unit **324**; contract **274**; `verify-dotnet.sh`
  green (local). **Payload-digest/citation remainder (`87b72647`):** pushed
  2026-09-12; hosted CI Implementation `34692385856` and Documentation
  `34692385860` — all six jobs green. External review on `cec7d6c2` → `87b72647`:
  **0 Blocker / 0 High / 2 Medium / 1 Low** — increment **not approved**;
  schema parse gate (`… → 9bbb6c35`) stays **closed**. Medium findings: (1)
  digest must be explicit versioned wire-content procedure, not ambiguous JCS
  logical payload or non-convergent raw self-hash; (2) citation source type must
  reconcile execution-context claims against `VerifiedPermittedEvidenceMaterial`.
  **Corrective pass (`abe4e902`, pushed):** `ProtectedModelResponseWireBytesDigest`
  with `evaluation-model-response-wire-content-digest-sha256-v1` (SHA-256 over
  wire UTF-8 with `response_ref.content_digest` zero-filled); one-shot
  `EvaluationModelResponseDocumentBinder`; `EvaluationModelPermittedEvidenceAuthorityVerifier`
  + authoritative expected-invocation source types; matrix negatives including
  mutated-wire-content before ref/semantic checks and forged context source type
  before model execution. Focused: `ProtectedModelResponseWireBytesDigestTests` 4;
  `EvaluationModelPermittedEvidenceAuthorityVerifierTests` 2;
  `EvaluationModelResponseValidationMatrixTests` 3; Evaluation unit **335**;
  `verify-dotnet.sh` green (local). External review on `abe4e902` → `debe1251`:
  **0 Blocker / 0 High / 1 Medium / 0 Low** — citation-authority Medium closed;
  hosted CI Implementation `34700298235` + Documentation `34700298213` green at
  `abe4e902`; docs-only `debe1251` Documentation `34700304491` green. Remaining
  Medium: structural `response_ref.content_digest` byte location must not assume
  canonical lexical JSON. **Structural locator corrective (`86877105`, pushed):**
  `EvaluationModelResponseContentDigestWireLocator` (`Utf8JsonReader` span +
  zero-fill); total `TryVerify`/`TryCompute`; provider-format negatives
  (whitespace, reordered properties, missing/malformed digest). Confirmation pass
  2026-09-12: `ProtectedModelResponseWireBytesDigestTests` 9; Evaluation unit
  **340**; `verify-dotnet.sh` green (local). **Pending:** hosted Implementation
  CI on `86877105`, external re-review; do not close Phase 6 slice 2 or enable
  Worker.
  Credential fail-closed external review/CI for `fb0c79fb` still pending.
- [x] Obtain verified deterministic-fact protected refs from
  `IProtectedDeterministicOutputStore` before model compose (slice 1 carry-forward).
  **Approved 2026-09-11** through chain `2466f1be` → `b8b5efad` (+ confirmation
  `9ae78e36`): external review **0 Blocker / 0 High / 0 Medium**; closes High
  (invocation/ownership provenance) and Medium (work-plan drift) from `2466f1be`.
  Store-backed protected refs; admitted request authority reload; session ownership
  and stable request/invocation/deterministic-ID reconciliation; full ownership
  chain on Postgres verified-material load; negatives for forged ownership,
  claimed-invocation mismatch, and wrong same-org scope. Focused:
  `EvaluationModelExecutionAuthorityVerifierTests` 5;
  `EvaluationModelDeterministicFactAuthorityLoaderTests` 4;
  `EvaluationModelExecutionServiceTests` 10;
  `EvaluationModelRequestComposerTests` 7; Evaluation unit 253. Hosted CI green at
  `b8b5efad`: Documentation `34566336207`; Implementation `34566336176` — all
  six jobs. `EvaluationId` authoritative binding deferred to Phase 6/7 completion
  persistence (no admitted-request Evaluation ID at this boundary). **Slice 2
  remainder:** credential fail-closed external review/CI for `fb0c79fb`; fuller
  response validation.
  Model-boundary prompt-injection/
  confused-deputy suite **approved** (`5c28e565` → `2db28254`). Evidence-source
  **approved** (`8773c4f9` → `f3105a7b` → `1afd1cbb`; bookkeeping chain **0/0/0**).
- [x] Persist protected provider request/response references and bounded
  attempt outcomes, never full model output in queue, log, metric, audit, or
  error payloads.
  **Confirmation pass 2026-09-11 at `d9c7b6a5`:** `IEvaluationProviderArtifactStore`,
  `EvaluationProviderArtifactPersistence`, `ProviderArtifactProvenance`/`ProviderArtifactOutcomes`,
  `InMemoryEvaluationProviderArtifactStore`, `PostgresEvaluationProviderArtifactStore`;
  optional persistence wired into `EvaluationModelExecutionService` after port
  execution (success and failure). Protected refs only (`prot.eval.model-req.*`,
  `prot.eval.model-res.*`); DB outcomes map to `0072` check constraint
  (`succeeded`/`failed`/`timed_out`/`invalid_output`); bounded `failure_category`
  only. Focused: `ProviderArtifactProvenanceTests` 7;
  `EvaluationProviderArtifactPersistenceTests` 4; `EvaluationModelExecutionServiceTests`
  14 (+2 persistence with ref-prefix assertions); `EvaluationProviderArtifactStoreTests`
  3 (+1 concurrent `ON CONFLICT` reconciliation); Evaluation unit 298;
  `EvaluationBoundaryTests` 6; migration `0079` criterion provenance columns;
  `verify-dotnet.sh` green (local). External review 2026-09-11 on descendant head
  `42668dfd`: **0 Blocker / 0 High / 1 Medium / 1 Low** — Medium concurrent
  idempotent insert closed via `ON CONFLICT DO NOTHING` + reconciliation; Low
  criterion self-compare closed via persisted `criterion_id`/`criterion_version`
  and DB-backed reconciliation (`16da6ef2`). **Reconfirmation pass 2026-09-11 at
  `bd7bce9b`:** `EvaluationProviderArtifactStoreTests` 3/3; Postgres integration
  475/475; `verify-dotnet.sh` green (local). External review 2026-09-11 on full
  corrective chain (`16da6ef2` → `591f1381`): **0 Blocker / 0 High / 0 Medium /
  0 Low**; closes prior documentation-state Low on stale CI wording. Hosted CI
  green at corrective `591f1381`: Documentation `34614235435`; Implementation
  `34614235430` — all six jobs including `dotnet`, `web`, `oidc`, `oci-oidc-smoke`,
  and `supply-chain`. Docs-only `7b708062` is not authoritative implementation CI.
  **Provider artifact increment closed.** Worker/processing remain disabled; no payload
  blob table yet (refs-only at `0072`).
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
  `cb9db3a1cf6f96961cf8506ec3196edd2b9ce3d0dcf7cbfc4334ecaf624860de` (JCS
  canonical evaluation-procedure bytes; refreshed when evaluator implementation
  binding gained `Directory.Packages.props`). Historical placeholder `…3306` /
  `g…` remains without payload and is not the available descriptor.
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
- **Phase 6 slice 2 in progress.** Store/authority-binding increment **approved**
  (`2466f1be` → `b8b5efad`). Model-boundary prompt-injection/confused-deputy suite
  **approved** (`5c28e565` → `2db28254`): external review **0 Blocker / 0 High /
  0 Medium**; prior Medium closed. Evidence-source injection at locator/completion
  **approved** (`8773c4f9` → `f3105a7b` → `1afd1cbb`; bookkeeping chain **0/0/0**).
  Credential fail-closed adapter selection confirmed at `fb0c79fb` (awaiting external
  review/CI). Focused: `EvidenceSourcePromptInjectionAndConfusedDeputyTests` 9;
  unit **274**; `verify-dotnet.sh` green (local). Hosted CI at `8773c4f9`:
  Documentation `34574753938` green; Implementation `34574753257` green — all six
  jobs. Hosted CI at `2db28254`: Documentation `34573333205` green;
  Implementation `34573333223` green — all six jobs. **Credential fail-closed
  adapter selection confirmed at `fb0c79fb`:** `EvaluationModelExecutionCompositionComposer`,
  `EvaluationModelCredentialBindingAdmission`, `EvaluationSyntheticDevelopmentModelProfile`;
  fixtures aligned to profile constants. Focused: `EvaluationModelExecutionCompositionTests`
  11; Evaluation unit **285**; `EvaluationBoundaryTests` 6; `verify-dotnet.sh` green
  (local). **Protected provider artifact persistence confirmed at `d9c7b6a5`**, corrective
  review on `42668dfd`: **0 Blocker / 0 High / 1 Medium / 1 Low** — concurrent
  idempotent append hardened (`ON CONFLICT DO NOTHING` + reconciliation); criterion
  provenance persisted (`0079`) and reconciled from DB (`16da6ef2`). Focused:
  `ProviderArtifactProvenanceTests` 7; `EvaluationProviderArtifactPersistenceTests` 4;
  `EvaluationProviderArtifactStoreTests` 3; Evaluation unit **298**; `verify-dotnet.sh`
  green (local). Hosted CI green at corrective `591f1381`: Documentation
  `34614235435`; Implementation `34614235430` — all six jobs. **Provider artifact
  increment closed.** **Schema parse gate approved/closed** (`83309967` →
  `09ca54ab` → `4609f7be` → `9bbb6c35`): external review **0 Blocker / 0 High /
  0 Medium / 0 Low**; authoritative implementation head `9bbb6c35`; hosted CI
  Implementation `34689229774` and Documentation `34689229816` — all six jobs
  green. Docs-only reconciliation `ad2105b8` (Documentation `34689256524`;
  Implementation `34689256500` change-detector only — not implementation
  evidence). **Slice 2 not closed** — aggregation/citation/protected-response-
  bytes matrix; credential fail-closed external review/CI for `fb0c79fb`. Wider
  AC-EVAL-24 matrix item remains `[>]` at Phase 6 gate. Worker disabled. **Do
  not** close slice 2, enable Worker processing, or wire real provider adapters
  until remainder + review.
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
  Phase 5 entry. Before final feature completion or production readiness,
  `session.work_trace` Evidence must either be materialized from durable Session
  persistence per `REQ-SESS-52` or remain explicitly unavailable under approved
  product configuration.

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
| Phase 4 slice 4 (`c97715e` + `7d16935` + `53611d8` + `cce2302`) | approved | External review 2026-09-09 on corrective chain: 0 Blocker / 0 High / 0 Medium; work-trace masquerade High closed. `c97715e`: forged excerpt, wrong version, unpublished material, cancelled agent negatives. Review found 1 High — shared transcript dictionary allowed work-trace masquerade. `7d16935`: separate `WorkTraceItemsBySourceId`; owner builder leaves empty; masquerade negative; dedicated-collection positive when populated. `53611d8` records confirmation evidence; `cce2302` records approval. Focused: `FlexAgent.Evaluation.Tests` 121; `EvaluationSessionEvidenceSourceTests` 14; architecture 65; `check_docs.py` passed. Work-trace owner port still deferred. Hosted CI not independently observed |
| Phase 4 gate (`f89c35c`) | approved | External review 2026-09-09: 0 Blocker / 0 High / 0 Medium; Phase 5 entry authorized. `f89c35c` records gate closure with `phase4_status: complete-work-trace-deferred`, proportionate regression (`verify-dotnet.sh` 2201 passed / 4 skipped; `scripts/check_docs.py` passed), and accepted work-trace deferral. Hosted CI not independently observed |
| Phase 5 slice 1 (`2ede5e1` + `2f3c699`) | approved | External review 2026-09-09 on corrective chain: 0 Blocker / 0 High / 0 Medium; positive-bounds Medium closed. `2ede5e1`: registry port, built-in allowlist, binding validation. Review found 1 Medium — binding validator did not independently reject non-positive memory/output or malformed durations. `2f3c699`: shared `EvaluationPositiveDuration`; fail-closed positive bound checks on binding and registry durations. Focused: `FlexAgent.Evaluation.Tests` 149; contract 264; architecture 65. Hosted CI not independently observed. Runner and invocation persistence remain for slice 2 |
| Phase 5 slice 2 (`a53a5f5` + `d9011f9` + `ce63dbe` + `d4255c4` + `d76c0ac`) | approved | External review 2026-09-10: **0 Blocker / 0 High / 0 Medium**. Chain: `a53a5f5` runner + invocation persistence; `d9011f9` idempotency/provenance + `0076`; `ce63dbe` in-process algorithmically bounded contract + cooperative deadlines; `d4255c4` `eval.builtin.impl-manifest.v2` source-closure identity; `d76c0ac` records push evidence. Focused: Evaluation 180; contract 264; architecture 65; `DeterministicInvocationStoreTests` 5. Worker/processing remain disabled pending later Phase 5 integration. Hosted CI not independently observed |
| Phase 5 slice 3 authority (`4c803fe` + `5579ce7` + `395a7af` + `1193ec6` + `6d1abc6`) | approved | External review 2026-09-10 on full chain: **0 Blocker / 0 High / 0 Medium**. Implementation: `4c803fe` orchestration gate; `5579ce7` admitted-request reload + protected source; `395a7af` independent byte→digest verification before parse. Docs: `1193ec6` approval record; `6d1abc6` confirmation evidence and rubric digest reconciliation to `04bdd47d…`. Focused: Evaluation 203; Runtime 340; `verify-dotnet.sh` 2309 passed / 4 skipped. Slice closed; `phase5_status` remains in-progress. Worker/processing remain disabled |
| Phase 5 post-slice-3 CI chain (`03056d0` + `be1b1d5` + `76d3496` + `7ee5293`) | approved | External review 2026-09-10: **0 Blocker / 0 High / 0 Medium**. `03056d0` docs-only slice-3 chain record; `be1b1d5` reconciles demo seed fixtures/baseline digest and stabilizes accommodation idempotency expiry dates after digest refresh (`be1b1d5` hosted dotnet/web/oidc/oci-oidc-smoke green; supply-chain failed at SPA SBOM grype on `js-yaml@4.3.1`); `76d3496` local confirmation; `7ee5293` pnpm override `js-yaml@4.3.2` + SPA OCI `apk upgrade curl libcurl`. Hosted **Implementation** run `34447801929` and **Documentation** run `34447801925` green at `7ee5293`; all six Implementation jobs pass including `supply-chain` and `oci-oidc-smoke`. Phase 5 remains in-progress; Worker disabled |
| Phase 5 slice 4 materialization corrective chain (`5ff61ee` + `0078` + `2209477` + `4422772`) | approved | External review 2026-09-10 on full corrective chain: **0 Blocker / 0 High / 0 Medium**. `5ff61ee`: payload store + `deterministic.fact` locator resolution. Review: lifecycle bypass (High) + decoupled FK/load provenance (Medium). `2209477`/`0078`: unconditional immutability; composite FK; hardened load. Review: duplicate retry provenance (Medium). `4422772`: retry joins attempt/request; reconciles request + ownership before ref/digest/bytes. Focused: `DeterministicPayloadImmutabilityTests` 8; hosted CI green at `4422772` (Documentation `34459660148`; Implementation `34459660500` all six jobs). Worker disabled |
| Phase 5 slice 4 completion linkage (`428c835` + `b3b37b4`) | approved | External review 2026-09-10 on full chain: **0 Blocker / 0 High / 0 Medium**. `428c835`: `DeterministicFactContextLoader` + completion service store-backed context; persists `deterministic.fact` `evaluation_evidence_items`. Review: cross-criterion reuse High (load bound request/attempt/digest only). `b3b37b4`: criterion id/version on `TryLoadProjectionAsync` SQL + loader; `Completion_service_rejects_cross_criterion_deterministic_fact_reuse` negative (both criteria permit `deterministic.fact`). Focused: `DeterministicFactContextLoaderTests` 5; `DeterministicEvidenceCompletionTests` 3; `verify-dotnet.sh` 2327 passed / 4 skipped. Hosted CI green at `b3b37b4` (Documentation `34480110188`; Implementation `34480110154` all six jobs). Worker disabled |
| Phase 5 slice 4 runner negatives + Worker lane scaffold (`4acfa4b` + `a179b085` + `0c63e403`) | approved | External review 2026-09-10 on `4acfa4b`: **0 Blocker / 0 High / 0 Medium**. Forbidden `environment`/`env`/`secret` runner regression tests (including nested secret). Worker: `IEvaluationDurableWorkProcessor`, idle lane registration, background polling, readiness/capability reporting, config + compile-time fail-closed gate. Hosted at `4acfa4b`: Documentation `34495832537` green; Implementation `34495832645` **failed** on unrelated enrollment admission window-boundary race. Corrective `a179b085`: `WaitUntilAwayFromAdmissionWindowBoundaryAsync()` before current-window counter insert; `0c63e403` + `22c17489` record confirmation and external approval. External review on corrective chain: **0 Blocker / 0 High / 0 Medium**. Hosted at `a179b085`: Documentation `34503812220` green; Implementation `34503812163` green — all six jobs. Local: `verify-dotnet.sh` 2337 passed / 4 skipped. Phase 5 slice 4 closed. Worker disabled |
| Phase 6 slice 1 model execution foundation (`8e0a5fc0` + `7bc3ff4f` + `78190660`) | approved | External review 2026-09-11 on `78190660`: **0 Blocker / 0 High / 0 Medium**. Chain: `8e0a5fc0` canonical model request/response contracts + orchestration gates + synthetic adapter; `7bc3ff4f` closes criterion/provenance override and parallel port contract; `78190660` closes bidirectional verified-fact/protected-ref and permitted Evidence set reconciliation. Focused: `EvaluationModelRequestComposerTests` 7; `EvaluationModelExecutionServiceTests` 7; `EvaluationModelResponseValidatorTests` 5; `AgentEvaluatorOrchestrationValidatorTests` 5; Evaluation unit 242; contract 269; `verify-dotnet.sh` green. Hosted CI green at `78190660`: Documentation `34554419421`; Implementation `34554419477` — all six jobs including `supply-chain` and `oci-oidc-smoke`. Slice 2 carry-forward: obtain protected refs from deterministic-output store before real adapter dereference. Worker disabled |
| Phase 6 slice 2 store/authority binding (`2466f1be` + `b8b5efad` + `9ae78e36`) | approved | External review 2026-09-11 on `b8b5efad`: **0 Blocker / 0 High / 0 Medium**; closes `2466f1be` High (invocation/ownership provenance) + Medium (work-plan drift). Chain: `2466f1be` store-backed protected refs; `b8b5efad` admitted request reload, ownership/stable-ID reconciliation, deterministic invocation binding, Postgres full-chain verified-material load; `9ae78e36` work-plan confirmation. Focused: `EvaluationModelExecutionAuthorityVerifierTests` 5; `EvaluationModelDeterministicFactAuthorityLoaderTests` 4; `EvaluationModelExecutionServiceTests` 10; `EvaluationModelRequestComposerTests` 7; Evaluation unit 253. Hosted CI green at `b8b5efad`: Documentation `34566336207`; Implementation `34566336176` — all six jobs; `9ae78e36` Documentation `34566378273`; Implementation `34566378258`. Worker disabled |
| Phase 6 slice 2 model-boundary injection (`5c28e565` + `2db28254`) | approved | External review 2026-09-11 on `2db28254`: **0 Blocker / 0 High / 0 Medium**; closes `5c28e565` Medium (instruction substring blacklist). Chain: `5c28e565` structural confused-deputy suite; `2db28254` disclosure-only `EvaluationProhibitedModelOutputDisclosurePolicy`, describe-injection positives, service synthetic scenarios corrected. Focused: `EvaluationModelPromptInjectionAndConfusedDeputyTests` 8; `EvaluationModelExecutionServiceTests` 13; Evaluation unit 265; `verify-dotnet.sh` green (local). Hosted CI green at `2db28254`: Documentation `34573333205`; Implementation `34573333223` — all six jobs. Worker disabled |
| Phase 6 slice 2 credential fail-closed adapter selection (`fb0c79fb`) | pending | Confirmation pass 2026-09-11 at `fb0c79fb`: fail-closed compose gates + non-secret credential binding admission; Production/Staging fail closed; Development/Testing synthetic gated on workload identity + frozen profile + catalog binding; fixtures aligned to profile constants. Focused: `EvaluationModelExecutionCompositionTests` 11; Evaluation unit 285; `EvaluationBoundaryTests` 6; `verify-dotnet.sh` green (local). Awaiting external review and hosted CI. Worker disabled |
| Phase 6 slice 2 provider artifact persistence (`d9c7b6a5` + `16da6ef2` + `591f1381`) | approved | External review 2026-09-11 on full corrective chain: **0 Blocker / 0 High / 0 Medium / 0 Low**. `d9c7b6a5`: `IEvaluationProviderArtifactStore`, persistence helper, provenance/outcome mapping, in-memory + Postgres stores; optional wire-in to `EvaluationModelExecutionService` after port execution. Protected refs only; bounded failure categories; no raw model bodies. Review Medium: concurrent idempotent insert — closed in `16da6ef2` via `INSERT ... ON CONFLICT DO NOTHING` + provenance reconciliation + eight-way concurrent integration test. Review Low: criterion self-compare — closed in `16da6ef2` via migration `0079` and DB-backed reconciliation. Review documentation-state Low: stale post-corrective CI wording — closed at `591f1381` bookkeeping. Focused: `ProviderArtifactProvenanceTests` 7; `EvaluationProviderArtifactPersistenceTests` 4; `EvaluationModelExecutionServiceTests` 14; `EvaluationProviderArtifactStoreTests` 3; Evaluation unit 298; `verify-dotnet.sh` green (local). Hosted CI green at corrective `591f1381`: Documentation `34614235435`; Implementation `34614235430` — all six jobs including `dotnet`, `web`, `oidc`, `oci-oidc-smoke`, and `supply-chain`. Docs-only `7b708062` not authoritative implementation CI. **Provider artifact increment closed.** **Slice 2 not closed** — credential fail-closed external review/CI for `fb0c79fb`; fuller response validation remain. Worker disabled |
| Phase 6 slice 2 evidence-source injection (`8773c4f9` + `f3105a7b` + `1afd1cbb`) | approved | External review 2026-09-11 on `8773c4f9`: **0 Blocker / 0 High / 0 Medium**; bookkeeping chain `f3105a7b` → `f329933b` → `1afd1cbb` also **0 Blocker / 0 High / 0 Medium** — closes `09c8f8e1` stale-CI documentation-state Medium; `f3105a7b` records hosted CI and reconciles stale `2db28254` CI to green; `1afd1cbb` updates verification table to full approved chain. Chain: `8773c4f9` `EvidenceSourcePromptInjectionAndConfusedDeputyTests` 9; `09c8f8e1` confirmation; `f3105a7b` approval + CI reconciliation; `f329933b` timestamp-only pass-through; `1afd1cbb` table reconciliation. No production code change; hostile source text treated as data per `AC-EVAL-24`. Evaluation unit 274; `verify-dotnet.sh` green (local). Hosted CI green at `8773c4f9`: Documentation `34574753938`; Implementation `34574753257` — all six jobs. Wider AC-EVAL-24 matrix remains `[>]` at Phase 6 gate. **Evidence-source increment closed.** **Slice 2 not closed** — credential fail-closed external review/CI for `fb0c79fb`; fuller response validation remain. Worker disabled |
| Phase 6 slice 2 model response schema parse gate (`83309967` → `09ca54ab` → `4609f7be` → `9bbb6c35`) | approved | External review 2026-09-12 on full chain: **0 Blocker / 0 High / 0 Medium / 0 Low**. `83309967` embeds schema authority, JsonSchema gate, structural constraints, negative fixtures. `09ca54ab` closes wire-trust-boundary and artifact-ordering Mediums. `4609f7be` closes binding, semantic-category, and digest Lows. `9bbb6c35` closes mismatch-provenance Medium (adapter-side `response_ref` on binding failure). Authoritative implementation head `9bbb6c35`; hosted CI Implementation `34689229774` and Documentation `34689229816` — all six jobs including `dotnet`, `web`, `oidc`, `supply-chain`, `oci-oidc-smoke`. Docs-only `ad2105b8` (Documentation `34689256524`; Implementation `34689256500` skipped implementation jobs — not implementation evidence). Focused: Evaluation unit **324**; contract **274**; `verify-dotnet.sh` green (local). **Schema parse increment closed.** **Slice 2 not closed** — aggregation/citation/protected-response-bytes matrix; credential fail-closed review for `fb0c79fb`. Worker disabled |
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
