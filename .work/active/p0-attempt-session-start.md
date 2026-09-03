---
id: p0-attempt-session-start
status: in-progress
created: 2026-09-02
updated: 2026-09-03
---

# Goal

Implement the P0 participant Attempt-start boundary from the existing My Work
assignment: show authoritative readiness, durably record any exact required
acknowledgments, and process one idempotent start command that revalidates those
records and atomically consumes one entitlement, activates one Attempt, binds
the exact accepted Submission version set, freezes one resolved Session
configuration and initial manifest, creates one active Session, and records
required audit.

The participant must be able to reconcile a lost or uncertain response into
either **Attempt did not start** with unchanged entitlement or **Attempt in
progress** with the one committed Session locator. No interaction, timer,
provider, Evidence, or Evaluation side effect may become observable before the
atomic commit.

This is the enabling start slice. The successor hosted-text-session task owns
the live conversation route, snapshot/reconnect UI, participant-message HTTP
command, provider execution, streaming transcript, and completion journey. This
task must not represent that successor behavior as production-ready.

# Governing sources

- `AGENTS.md`, `.agents/skills/implementation-workflow/SKILL.md`, and
  `.work/README.md` — repository invariants, tracked execution state,
  specification-driven TDD, review separation, and completion rules
- `docs/README.md` — authority by concern
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — Attempt, Session, Submission, assessment MVP,
  and non-negotiable product meaning
- `docs/requirements/features/submission-attempts.md` — `REQ-SUBM-15`–
  `REQ-SUBM-23`, `REQ-SUBM-31`–`REQ-SUBM-32`, `REQ-SUBM-36`–`REQ-SUBM-42`,
  `REQ-SUBM-46`–`REQ-SUBM-47`, and `AC-SUBM-4`–`AC-SUBM-10`
- `docs/requirements/features/resolved-session-configuration.md` —
  `REQ-RSC-1`–`REQ-RSC-32`, `REQ-RSC-39`, `REQ-RSC-41`–`REQ-RSC-43`,
  `REQ-RSC-46`–`REQ-RSC-49`, `REQ-RSC-51`–`REQ-RSC-55`, `AC-RSC-1`–
  `AC-RSC-14`, `AC-RSC-16`, `AC-RSC-20`–`AC-RSC-22`, and
  `AC-RSC-24`–`AC-RSC-28`
- `docs/requirements/features/session-text-lifecycle.md` — `REQ-SESS-1`–
  `REQ-SESS-7`, `REQ-SESS-44`, `AC-SESS-1`, and `AC-SESS-2`; execution after
  committed start is a successor concern
- `docs/requirements/features/auth-resource-isolation.md` and
  `docs/requirements/mvp-operational-defaults.md` — current authorization,
  isolation, audit, idempotency, availability, and bounded observability
- `docs/ui-ux/flows/submission-attempt.md` — readiness, exact acknowledgment,
  confirmation, starting, reconciliation, active-conflict, and abort behavior
- `docs/architecture/mvp-architecture.md` — Participation/Submission Attempt
  ownership, Session-resolution ownership, the explicitly approved shared
  atomic-start coordinator, data ownership, and transaction rules
- `docs/architecture/backend-module-architecture.md` — application ports,
  module collaboration, trusted scope, commit-time authorization, and
  PostgreSQL negative verification
- `docs/architecture/session-runtime-contract.md` — committed Session
  readiness, trusted binding, frozen policy, actor relationship, manifest, and
  no-precommit-execution boundary
- `docs/ui-ux/design-system/README.md` and
  `docs/ui-ux/design-system/implementation-guide.md` — design authority and
  donor/routing rules
- Applicable design-system modules: `foundation/accessibility.md`,
  `foundation/layout.md`, `foundation/interaction-states.md`,
  `foundation/status.md`, `components/buttons.md`, `components/modals.md`,
  `components/alerts.md`, `components/error-summary.md`,
  `product/attachments.md`, `product/empty-loading.md`,
  `product/protected-content.md`, and `product/session-controls.md`

Before new production UI, retain `ProductionMyWorkDetailPage` and its guided-task
shell as the accepted production donor. Adapt the approved Start Attempt
journey specimen only for the missing readiness/confirmation states; do not
copy its fixture data or promote the Design Lab Session page as production
behavior.

# Scope

## In

- A durable Attempt aggregate and append-only Attempt/start-operation history,
  including trusted ordinal derivation, baseline/retry entitlement source,
  `Active` and terminal mappings, consumed status, timestamps, and stable
  Session/configuration/manifest bindings.
- A durable start-operation record keyed by organization, participant,
  enrollment, action, idempotency key, and canonical command digest so
  duplicate, concurrent, lost, and uncertain responses reconcile without a
  second consumption and mismatched key reuse fails closed. A bounded claim
  lease and stale-claim takeover/recovery rule must prevent a process crash from
  leaving the Enrollment permanently stuck in **Starting**.
- An authoritative readiness query derived from current identity, application
  session, organization, participant relationship, active Enrollment, cohort
  availability, effective timing/accommodation, Attempt history and entitlement,
  accepted Submission readiness, required Agent-reading compatibility, required
  acknowledgment versions, configuration-source readiness, and active conflict.
- Participant-visible safe readiness outcomes and recovery actions for
  eligible, too early, expired, exhausted, missing accepted material, required
  material not Agent-readable, active conflict, dependency/configuration
  unavailable, and permission/Enrollment change.
- Versioned participant-visible instruction/notice descriptors, a separately
  authorized and idempotent acknowledgment mutation, append-only exact-version
  acknowledgment records, and start-time Attempt bindings sufficient for
  `REQ-SESS-2`–`REQ-SESS-4`. Deployment or activity policy supplies the
  wording; this task must not hard-code legal or consent text. A checked browser
  control remains local intent until the acknowledgment mutation commits.
- An immutable participant-notice projection registered with the exact text
  workflow/policy source version and covered by that source's declared content
  digest. The activated baseline already freezes that exact source identity;
  Configuration owns the immutable notice/protected-content projection, while
  Session execution owns the acknowledgment lifecycle and Attempt/Session
  binding. Existing source versions without a verifiable projection are
  unsupported for a required-notice start and fail closed.
- Deterministic P0 resolution from the activated cohort baseline and immutable
  source versions into one canonical resolved-configuration document and
  digest, one initial manifest with required immutable provenance, one frozen
  runtime-policy snapshot, one frozen model-deployment identity, and exact
  permitted Submission references.
- Fail-closed validation for unsupported/missing source families, mutable
  aliases, digest drift, capability widening, unqualified model identity,
  dynamic memory writes, voice, tools, shared Sessions, and direct deployment.
- One explicitly named application-level Attempt-start coordinator and narrow
  module-owned ports. Submissions owns Attempt/entitlement/exact Submission
  binding; Session resolution/execution owns resolved configuration, manifest,
  frozen runtime state, Session, and Session actor relationship.
- One PostgreSQL transaction for commit-time reauthorization/revalidation and
  all start-success mutations: Attempt activation/consumption, exact version
  binding, binding of current affirmative acknowledgment records to the exact
  Attempt, resolved configuration, initial manifest, active Session,
  participant relationship, audit/outbox, and successful operation outcome.
- Schema constraints and immutability guards for organization/activity/cohort/
  enrollment/participant scope, ordinal uniqueness, one nonterminal Session per
  Attempt, one active Attempt conflict per Enrollment where required, exact
  Submission binding, configuration/manifest binding, append-only history, and
  idempotency.
- Repair the exact accepted-version port so a commit transaction is required
  and actually used; the coordinator must never make an atomicity claim through
  the current transaction-ignoring adapter.
- Authenticated, antiforgery-protected production HTTP contracts for readiness
  and idempotent start/reconciliation. The server derives owner, ordinal,
  entitlement, exact versions, configuration, manifest, and Session identity;
  client-supplied identifiers are assertions to verify, never authority.
- C#/OpenAPI/TypeScript contract parity, strict request validation, bounded
  participant-safe reason categories, `Cache-Control: no-store`, consistent
  correlation, and no raw protected or provider/configuration details.
- My Work UI for readiness, exact notice/acknowledgment presentation, the
  accessible Start Attempt confirmation, occupied starting state, response
  reconciliation, precommit failure, active conflict, post-start abort/history,
  and **Continue Attempt** locator preservation.
- Bounded telemetry for starts requested/committed/blocked/reconciled,
  duplicate/conflict outcomes, latency, precommit failure category, source
  drift, audit failure, and invariant breaches without raw content or
  unrestricted identifiers.
- Synthetic seed/profile data sufficient to exercise eligible and blocked
  start paths without enabling Production deployments lacking an explicitly
  qualified P0 model/profile and required policy sources.
- Documentation/current-state reconciliation after implementation, including
  precise honesty that atomic start is complete while the hosted live Session
  journey remains separate if that successor has not landed.

## Out

- Participant message admission over production HTTP, transcript snapshot,
  provider invocation, incremental publication, reconnect UI, pause/resume,
  completion, timer-warning UI, or the full production Text Session console.
- Evaluation, Evidence extraction, Review, Result, or Release implementation.
- Reviewer/administrator resolved-configuration inspection or export and
  historical reconstruction jobs from `REQ-RSC-38`, `REQ-RSC-40`, and
  `REQ-RSC-44`–`REQ-RSC-45`; this task preserves the data needed by those
  separately bounded consumers and does not claim their completion.
- Administrator retry-entitlement request/approval UI beyond consuming and
  displaying already-authorized entitlement facts; that existing requirement
  remains a separately bounded workflow unless already available.
- Retry-entitlement creation, independent approval, and persistence required by
  `REQ-SUBM-21`. No such production records currently exist. This task defines
  the start-side read port and safely handles an empty result, but does not
  claim that separately authorized beyond-baseline retries can yet be granted.
- New configuration precedence semantics, new capability families, mutable
  `current`/`latest` resolution, or fallback behavior not approved by the
  governing specifications.
- Voice, tools, Dynamic memory, shared Sessions, arbitrary timers, direct
  deployment, Interaction Controller behavior, or participant data reuse for
  learning.
- Product-wide consent wording, retention durations, deletion schedules,
  malware-provider selection, cryptography, compliance claims, or model/vendor
  selection.
- Replacing the guided-task My Work shell or promoting the Design Lab Session
  fixture as production authority.
- Commits, pushes, pull requests, deployments, or releases unless separately
  requested.

# Requirement-to-surface map

| Requirement / acceptance target | Implementation surfaces | Required evidence |
| --- | --- | --- |
| `REQ-SUBM-15`–`REQ-SUBM-17`, `AC-SUBM-4`–`AC-SUBM-6` | Attempt domain, readiness service, Enrollment/effective-timing and accepted-version ports, API response, My Work readiness | Domain decision tables; current-auth, timing, lifecycle, entitlement, capability, and active-conflict tests; eligible/blocked UI states |
| `REQ-SESS-2`–`REQ-SESS-4`, `AC-SESS-1` | Exact notice descriptors, separate acknowledgment mutation/records, start-time Attempt binding and revalidation, durable audit, dialog controls | Exact-version, stale/declined/withdrawn/cross-scope, duplicate, lost-response, audit-failure, keyboard, focus, and announcement tests |
| `REQ-SUBM-18`–`REQ-SUBM-20`, `REQ-SUBM-22`–`REQ-SUBM-23`, `AC-SUBM-7`–`AC-SUBM-10` except retry-entitlement creation | Start operation, Attempt store, atomic coordinator, constraints, reconciliation, Attempt history projection | Same-key/different-key concurrency, process-loss/fault injection, one-consumption, no-reset, abort/history tests |
| `REQ-SUBM-31`–`REQ-SUBM-32`, `REQ-SUBM-46`–`REQ-SUBM-47` | Exact accepted-version transaction reader, immutable Attempt-version rows, permitted Session refs | Wrong parent/actor/version, required/optional capability, transaction participation, immutable-version tests |
| `REQ-RSC-1`–`REQ-RSC-32`, `AC-RSC-1`–`AC-RSC-14` | Resolver, canonical configuration, source readers/revalidators, initial manifest, frozen policy/model identity, Session-start writer | Ownership, determinism, canonical digest, precedence, drift, mutable alias, model identity, source poisoning, manifest completeness, rollback tests |
| `REQ-RSC-39`, `REQ-RSC-41`–`REQ-RSC-43`, `AC-RSC-20`–`AC-RSC-22` | Participant-safe readiness/failure projection, required audit and protected references, authoritative UTC order | Wrong-role/scope, sensitive-value redaction, audit acceptance, UTC/order, accessible-failure tests |
| `REQ-RSC-46`–`REQ-RSC-49`, `REQ-RSC-51`–`REQ-RSC-55`, `AC-RSC-24`–`AC-RSC-28` | Frozen credential-binding reference, Invocation/Decision/output policy, P0 disabled capabilities, optional one-lane timer policy | Wrong-Organization/revoked credential binding, exact policy/schema identity, no-widening, disabled capability, timer enabled/disabled tests |
| `REQ-SESS-1`, `REQ-SESS-5`–`REQ-SESS-7`, `AC-SESS-2` | Existing `SessionRuntime.CreateActive`, transactional persistence, participant relationship, commit-time authorization, worker visibility boundary | No-precommit Session/timer/provider visibility, wrong-scope binding, authorization-race, post-commit trusted-binding tests |
| `REQ-SUBM-20`, `REQ-SUBM-22`, `REQ-SESS-44`, `AC-SUBM-10` except retry-entitlement creation, `AC-RSC-16` | Session terminal command transaction, Session-owned Attempt-mapping record, Submissions-owned terminal projection port, immutable Attempt history | Complete/terminate/abort mapping, required-audit failure, duplicate terminal command, race, consumed-entitlement and immutable-binding tests |
| `REQ-SUBM-36`–`REQ-SUBM-42`, `REQ-SESS-44` | Required audit/outbox, append-only operation/Attempt/acknowledgment history, retention-policy identity, bounded telemetry | Audit failure rolls back success; mutation/immutability guards; log/metric sensitivity inspection |
| Approved submission-attempt UX | `ProductionMyWorkDetailPage`, production submission API/contracts, shared `AcknowledgmentGate`, `DialogPlate`, `Key`, `ErrorSummary`, `WaitPanel`, status/attempt components | Web unit tests plus real authenticated Playwright accessibility snapshots and desktop/narrow screenshots |

# Plan

- [x] Activate this task only when implementation begins: set `status` to
  `in-progress`, refresh the current-state snapshot, and recheck all governing
  documents and IDs above for drift.
- [x] Write the smallest failing domain tests for Attempt ordinal/entitlement,
  eligibility, active conflict, consumption, terminal history, and equivalent
  versus conflicting idempotency. Add the Attempt aggregate, start operation,
  bounded outcome codes, and in-memory ports without persistence concerns.
- [x] Define the public application contracts before adapters: readiness
  projection, acknowledgment command/result, start command/result, trusted
  actor/scope, operation claim/lease/reconciliation, Attempt history,
  configuration candidate, terminal Attempt mapping, and module-owned
  transaction-aware ports. Add contract-boundary tests that prohibit
  infrastructure types in application/domain signatures.
- [x] Add failing tests for exact notice/acknowledgment behavior. Extend the
  Configuration source-version registration with a typed participant-notice
  projection for the exact text workflow/policy source. Persist stable notice
  identity/type plus a protected content reference, version, digest, required
  outcome, and provenance; couple projection persistence to source-version
  registration and verify it against the same canonical source bytes/digest.
  Keep the existing activation-baseline v1 shape and semantics unchanged: its
  exact workflow/policy source reference freezes the notice set. Existing
  source versions without a verifiable projection fail closed when a notice is
  required. Implement the separate idempotent acknowledgment mutation,
  append-only exact-version/outcome records, Attempt binding at start, and
  stale/declined/withdrawn/cross-scope rejection. Do not invent notice content
  or assume a browser checkbox is authoritative.
- [x] Add failing resolver tests covering determinism, source ordering,
  canonical digest, precedence/narrowing, P0 disabled capabilities, Stable
  memory, required versus optional Submission material, model identity,
  immutable source references, source drift, and manifest completeness.
  Implement the narrow P0 resolver by composing the activated baseline,
  Configuration source versions, existing frozen-runtime-policy resolver, and
  frozen model-deployment types.
- [x] Add the next immutable migration (currently `0063`; refresh at task
  activation and never reuse a number) for Attempts, start operations, exact
  Attempt/Submission bindings, acknowledgment decisions and Attempt bindings,
  resolved configurations, and initial manifests. Add composite-scope foreign
  keys, partial/unique indexes, claim lease/expiry constraints, append-only
  guards, digest checks, authoritative timestamps/order, and safe
  upgrade/empty-state behavior.
- [x] Add failing PostgreSQL integration tests for the schema and each success
  participant. Fix `PostgresExactAcceptedVersionReader` and its API composition
  adapter to use `PostgresCommitTransaction.Required`, then prove an exact
  accepted version cannot be read or substituted outside the coordinator's
  transaction/scope.
- [x] Implement the explicit shared atomic-start coordinator. Prepare pure
  deterministic resolution outside the transaction where safe; inside one
  bounded transaction lock the start operation and Enrollment/Attempt scope,
  reauthorize the live application session and participant relationship,
  revalidate timing/accommodations/entitlement/accepted versions and every
  separately committed current affirmative acknowledgment, revalidate source
  digests/model eligibility, bind those acknowledgment records to the new
  Attempt, persist all start-success records through owner ports, accept
  required audit, and commit. Perform no external provider/network call inside
  the transaction.
- [x] Add crash/fault/concurrency proofs at every material boundary: before and
  after operation claim, each owned persistence write, required audit, and
  commit-response loss. Prove precommit failure leaves no consumed entitlement
  or exposed Session, success is all-or-nothing, same-key retry returns the same
  identifiers, mismatched key reuse conflicts, an authorized retry can reclaim
  a stale operation lease without creating another outcome, different-key
  races create no competitor, and a postcommit abort never restores the
  original entitlement.
- [x] Integrate Session terminalization with a narrow Submissions-owned
  Attempt-terminal writer in the existing primary-store transaction. Prove
  `Completed` maps to consumed `Completed`, termination/integrity abort maps to
  consumed `Aborted`, required audit failure rolls the terminal mutation back,
  duplicates reconcile, and no terminal path renumbers, deletes, rebinds, or
  restores entitlement to the Attempt.
- [x] Add readiness, acknowledgment, and start/reconcile HTTP contracts to the production API
  using the established authenticated Enrollment endpoint policy,
  antiforgery/request-shape validation, request limits, correlation, safe
  status mapping, and `no-store`. Add runtime tests for signed-out, wrong role,
  guessed enrollment, cross-organization/participant/activity/cohort/version,
  revoked application session, stale/declined/withdrawn acknowledgment,
  stale readiness, malformed body, duplicate, lost response, mismatched key
  reuse, conflict, unavailable dependency, and audit failure.
- [x] Add canonical fixtures/schema where required and update C#, OpenAPI, and
  `web/src/contracts/v2.ts` together. Prove invalid and unknown discriminators,
  missing schema versions, unsafe identifiers, and mismatched idempotency reuse
  fail closed.
- [x] Extend `web/src/api/production-submission.ts` and
  `ProductionMyWorkDetailPage` through test-first state/reducer work. Render the
  server readiness projection, explicit Attempt count/consequence, exact
  Submission version summary, required acknowledgment controls, accessible
  confirmation dialog, acknowledgment-saving and saved/failed states, occupied
  start, reconciliation, unchanged-entitlement failure, active conflict,
  abort/history, and **Continue Attempt**. The confirm flow must first receive
  durable acknowledgment success, then issue start; it must not send start
  after an acknowledgment failure or uncertain outcome. Never decrement
  entitlement or infer active state locally.
- [x] Preserve the committed Session locator in API and My Work state, and add
  a focused contract test that it is scoped to the participant's one committed
  Attempt. Do not remove the production Session route's honest unavailable
  state or claim live handoff completion until the hosted-text-session successor
  supplies its snapshot/command contract.
- [x] Add bounded metrics/logging and invariant checks. Verify logs, traces,
  metrics, responses, browser storage, and test artifacts contain no notice
  content, Submission content, raw configuration, provider credentials,
  unrestricted identifiers, or authorization internals.
- [x] Run focused domain, application, contract, architecture, runtime, web,
  and PostgreSQL tests, then full repository gates. Record exact commands,
  counts, exit status, and any environment blocker below; do not convert an
  unavailable integration environment into a pass.
- [>] Attach to the documented healthy candidate origin (`:5274` with canonical
  API `:18080`) and use the synthetic Participant account. Live seed verifies
  active conflict, history, and **Continue Attempt** at desktop and narrow
  widths. Eligible confirm/cancel, acknowledgment save, uncertain
  acknowledgment, starting, reconcile, and retry-ordinal dialog remain
  unit-tested only until a seed without an active Attempt is available.
- [x] Request distinct backend, frontend, security/privacy, and QA reviews.
  Resolve every blocking finding in this task, rerun affected checks, and record
  reviewer evidence without asking reviewers to edit the implementation.
  Backend/security High Development synthetic fallback and frontend Blocker 409
  occupied-start were fixed earlier. Confirmation facts (`UI-SUBM-DEC-4`) and
  active-Attempt landing on **Continue Attempt** remain. This cleanup pass
  keeps acknowledgment save/saved/failed, uncertain-ack recovery, retry
  ordinal copy, and Attempt history in scope until review accepts them.
- [x] Reconcile this plan with actual changes and governing specs; update
  `docs/current-state.md` only to the demonstrated boundary, create/activate the
  separately tracked hosted-text-session successor if authorized, then remove
  this task file after durable truth and required review evidence are complete.

# Current state

Backend review of `1e8b25a` is approved for Attempt-start correctness. Follow-up
`c966ddb` landed the explicit `AttemptMutationDisposition` client contract and
path-scoped Gitleaks allowlists; that review approved the commit with a P2 to
narrow the 64-hex digest exemption.

`753728b` tightened Gitleaks to `^[aA]{64}$` and unblocked Implementation
[33657055497](https://github.com/trannamtrung1st/flex-agent/actions/runs/33657055497) (all jobs green). Review of that commit requested
changes: the first `fast-uri` override (`>=3.1.6`) resolved to `4.1.4`, outside
AJV `^3.0.1`, and CycloneDX logged `npm error invalid: fast-uri@4.1.4` (masked by
`--ignore-npm-errors`).

`61ff0f9` scopes the override to `ajv>fast-uri: 3.1.6`. Review approved that
commit: patched v3, inside AJV `^3.0.1`, no leftover `4.1.4`/`3.1.5`, no
completion overclaim. Optional further scope (`ajv@8.17.1>fast-uri`) is
deferred; not requested as another commit.

Implementation [33658231713](https://github.com/trannamtrung1st/flex-agent/actions/runs/33658231713)
completed successfully (`changes`, `dotnet`, `web`, `oidc`, `supply-chain`,
`oci-oidc-smoke`). Supply-chain `Generate SBOM` succeeded. Job logs are not
readable without GitHub auth, so the remote CycloneDX line cannot be grepped
here; local SPA SBOM generation on this tree has no `invalid: fast-uri`.

Review of `38738f6` kept this task open. The follow-up pass (uncommitted until
requested) separates local checkbox intent from durably recorded
acknowledgments, persists acknowledgments before occupying start, reuses
acknowledgment idempotency keys across uncertain `/acknowledgments` outcomes,
uses retry copy `Attempt N · Authorized retry (baseline limit M)` with a
realistic `next_ordinal: 3` fixture, and renders `attempt.history`.

Confirmation pass: Start dialog includes an honest Agent-inspection line
(readiness has no per-item inspection facts). Narrow guided-task bay clearance
is `7.5rem` so history can sit above the fixed foot. Live candidate `:5274`
still lands seeded Q3 on **Attempt in progress** with history **Attempt 1
active. Consumed.** Confirmation/retry/uncertain-ack remain unit-covered.
Review of `602dadb` requested one more pass: restore honest Session-locator
copy (Continue does not claim a hosted Session) and project
`current_outcome` on readiness notices so recorded acknowledgments survive
reload. Acknowledgment idempotency keys are now `notice_id:source_version_id`.
Review of `2bc7a1c` required skipping acknowledgment POSTs when readiness
already reports an exact affirmed `current_outcome`, projecting the latest
exact-version outcome (including declined/withdrawn) separately from start-time
`CurrentBindable`, and not treating a shared-admission window-boundary flake as
an Attempt-start regression. Do not retire until that follow-up is reviewed
and Implementation is green.

Successor/operational only: hosted live Session, Production qualified model
selection, Worker in Compose.

# Decisions

- Submissions owns the Attempt aggregate, entitlement history, exact accepted
  Submission binding, readiness orchestration, and start command because those
  are Participation/Submission responsibilities in the approved architecture.
- Session resolution/execution owns canonical resolved configurations, initial
  and append-only manifests, frozen runtime policy/model binding, Session state,
  timers, and Session actor relationships. Submissions receives only narrow
  application ports and never writes Session-owned tables directly.
- The host is composition only. Reusable eligibility/resolution/start policy
  remains in module application/domain code; the approved cross-module atomic
  boundary is explicit and architecture-tested.
- Cross-module collaboration uses consumer-owned neutral application ports and
  owner-side or composition adapters. Neither core module references another
  module's infrastructure, neither adapter writes another owner's tables, and
  start plus terminal mapping must not introduce a project-reference cycle.
- The client uses distinct opaque idempotency keys and canonical command
  digests for acknowledgment and start mutations. It first records a deliberate
  outcome against the exact displayed notice version. The start command does
  not turn checkbox state into authority: the server reloads and revalidates
  the durable current acknowledgment set, then binds those records to the exact
  Attempt created by the successful transaction.
- A durable start-operation claim exists before the success transaction and
  does not consume entitlement. It is created only after authenticated,
  authorized preflight and has bounded ownership/expiry so an authorized retry
  can recover an abandoned claim. The success transaction marks that operation
  committed with its immutable outcome; bounded post-rollback failure recording
  supports reconciliation without fabricating an Attempt or Session.
- Existing frozen activation-baseline records are immutable. Participant-visible
  notice support uses an immutable projection of their already-bound exact text
  workflow/policy source version, covered by the source content digest. The
  activation-baseline v1 document is not reinterpreted or rewritten; an older
  source version without a verifiable required-notice projection fails closed.
- Pure/canonical resolution may be prepared outside the transaction, but every
  source and decision-relevant fact is revalidated under the commit boundary.
  External provider calls, credential exchange, and model execution never run
  inside it.
- The existing runtime enters `Active` at the successful commit; its timer
  state may be prepared in memory but is not authoritative or worker-visible
  before commit.
- Production deployment remains fail-closed unless required versioned policy
  sources and a qualified frozen P0 model/deployment identity are available.
  Synthetic local/CI fixtures do not enable or weaken Production gates.
- The start slice ends at an authoritative, recoverable Session locator on My
  Work. Full live Session navigation remains unclaimed until the separately
  governed hosted-text-session contract is implemented.
- Session terminalization remains the source of the terminal outcome, while
  Submissions remains the authoritative owner of Attempt status. The existing
  Session terminal transaction must call a narrow Submissions port so terminal
  record, Attempt mapping, manifest seal/handoff, Attempt state, and required
  audit cannot diverge.

# Findings / deviations

- No approved product or UX ambiguity was found for the start semantics; the
  submission-attempt and Session requirements already define the participant
  outcomes and have no open start questions.
- Attempt ownership, durable acknowledgments, atomic Development start, exact
  accepted-version reads under the commit transaction, notice projection sets
  (`0067`), and Session-to-Attempt terminal mapping now exist. Confirmation
  facts, active-Attempt landing, local-versus-recorded acknowledgment copy,
  uncertain-ack recovery without start occupation, retry ordinal wording, and
  Attempt history rendering are implemented in source and focused web tests.
  Remaining High items are successor/operational (hosted live Session,
  Production qualified model, Worker in Compose). Residual in-scope UX: live
  (non-unit) ack/uncertain/retry dialog evidence on a seed that is not already
  active-conflict. Per-item Agent-inspection remains absent from the readiness
  contract; the dialog states that honestly. Continue Attempt names the
  committed locator and says live Session interaction is not available yet.
- Existing Session persistence is reachable through a supplied PostgreSQL
  transaction. `PostgresSessionRuntimeRepository` requires
  `ISessionAttemptTerminalSink`; Session integration tests pass
  `IgnoringSessionAttemptTerminalSink`.
- PostgreSQL Development start loads frozen baseline sources and revalidated
  `configuration_source_versions` rows under the same commit transaction.
  Digest drift fails closed. In-memory Testing without a PostgreSQL
  transaction still uses matching synthetic lists.
- Exact-version adapters now require the commit transaction
  (`PostgresCommitTransaction.Required`). Keep regression coverage; do not
  restore a transaction-ignoring reader.
- `ActivationBaselineDocument` captures immutable source references and major
  fairness domains but not explicit participant-visible instruction/notice
  descriptors. Required acknowledgment UI cannot use fixture copy as the
  authoritative source.
- `configuration_source_versions` retains source identity and digest but not
  the canonical bytes passed at registration. Notice resolution therefore
  needs a typed immutable projection produced and digest-verified at source
  registration; start must not reconstruct policy from unavailable bytes or
  an unversioned runtime setting.
- The initial plan incorrectly allowed acknowledgment records to be created as
  part of the successful start transaction. The approved journey requires a
  deliberate durable acknowledgment followed by start-time revalidation; the
  corrected plan uses separate idempotent mutation and exact Attempt binding.
- The initial plan did not state how an abandoned `Starting` operation recovers
  or how Session terminalization updates the Submissions-owned Attempt. The
  corrected plan adds bounded claim leases/takeover and mutation-coupled
  terminal mapping.
- The participant-facing handoff required by the approved UX and traced to
  `AC-SUBM-7` remains partially unmet by this enabling slice until the
  hosted-text-session successor replaces the current production unavailable
  route. This residual must remain explicit in `docs/current-state.md` and
  release claims.
- `REQ-SUBM-21` has no production retry-entitlement record or grant/approval
  path today. This task can enforce baseline allowance and consume an empty
  start-side retry source, but beyond-baseline retry authorization remains a
  separately tracked gap and must not be implied by the UI.

# Verification

Planning is documentation/working-state work, so a red/green test phase is not
meaningful yet. Implementation must execute the red/green evidence described in
the plan.

| Check | Status | Evidence |
| --- | --- | --- |
| Focused Submissions domain/application tests | passed | AttemptStartCoordinatorTests 14/14 including declined `current_outcome` vs start-time `CurrentBindable` |
| PostgreSQL migration/integration/fault tests | passed | 2026-09-02: AttemptStartPersistenceTests 7 (includes registered digest drift). Notice list count mismatch fail-closed. Parser digest-before-empty. Mapping persistence 2. Full Postgres.Integration in verify-dotnet. |
| Runtime HTTP negative-contract tests | passed | Included in `CI=true bash build/scripts/verify-dotnet.sh` |
| Web unit/component tests | passed | `ProductionMyWorkDetailPage.test.tsx` 14/14 including skip of acknowledgment POST when `current_outcome` is already affirmed |
| `pnpm verify:web` | passed | `bash build/scripts/verify-web.sh` exit 0 |
| Proportionate `.NET` solution/build/test gates | passed | `CI=true bash build/scripts/verify-dotnet.sh` exit 0; 1812 succeeded, 3 skipped. Worker Dockerfile COPY for AssessmentConfiguration + Submissions. Artifact lock NU1004 fixed. |
| `pnpm compose:status` and authenticated Playwright MCP | passed (partial) | Candidate `:5274` + `demo.participant` on seeded Q3: **Attempt in progress**, history **Attempt 1 active. Consumed.** above **Continue Attempt** at desktop 1280 and scrolled narrow 390. Start dialog / retry / uncertain-ack not live (active conflict). |
| Independent backend/frontend/security/QA review | recorded | `1e8b25a` approved for backend correctness; `c966ddb` P2 hex allowlist addressed; `61ff0f9` approved (AJV-compatible `fast-uri` 3.1.6). Remaining items are product-completion gaps |
| Implementation `supply-chain` | green on `61ff0f9` | [33658231713](https://github.com/trannamtrung1st/flex-agent/actions/runs/33658231713) conclusion success; `pnpm audit` and `Generate SBOM` succeeded. Local CycloneDX has no `invalid: fast-uri`. Remote log text not downloadable without GitHub auth. |

# Blockers

None for Development start on a seeded Compose stack. Confirmation-dialog
facts are implemented. Do not treat hosted live Session or Production
qualified model identity as done. The versioned notice projection and
transactional exact-version reader are implemented prerequisites, not
remaining blockers.

# Readiness review

Reviewed on 2026-09-02 against the approved requirements, current architecture,
current code seams, backend/frontend/security reviewer criteria, and the
implementation workflow. The task is ready to activate with no unresolved
product decision. Implementation must preserve these entry gates:

- acknowledge first, then revalidate and bind at start;
- recover abandoned start-operation claims without weakening idempotency;
- preserve the activation-baseline v1 shape and reject unverifiable legacy
  notice sources instead of reinterpreting frozen records;
- resolve notices only from the digest-covered immutable projection of the
  exact frozen workflow/policy source version;
- keep Attempt terminal state mutation-coupled to Session terminalization; and
- keep the hosted live Session journey explicitly unclaimed.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
