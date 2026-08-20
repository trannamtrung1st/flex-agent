---
id: p0-assessment-setup-cohort-activation
status: planned
created: 2026-08-20
updated: 2026-08-20
---

# Goal

Implement the production-backed P0 Assessment Campaign setup and Cohort
activation vertical slice. An authenticated, currently authorized Activity
administrator must be able to create and resume an assessment Activity draft,
save expected revisions, select exact permitted source revisions, check
readiness, deliberately activate a ready Cohort including an empty Cohort,
reconcile an uncertain activation response, and inspect the immutable activated
baseline.

The implementation must make PostgreSQL-backed server state authoritative,
preserve Organization/Activity/Cohort isolation, use the distinct
`activation-baseline-jcs-sha256-v1` contract, and atomically commit the
activation attempt, immutable baseline, unique Cohort binding, `Activated`
transition, and required durable audit/outbox acceptance. The React surface
must adopt the approved Assessment Campaign setup interaction specification
through the production application-session/API boundary and must not treat the
completed synthetic `/browser` scenario adapter as product authority.

This slice guarantees activation of a ready Cohort, including an empty Cohort,
and preserves compatibility with later Enrollment without baseline mutation.
It ends at the activated Cohort and a conditional authorized assignment
handoff. It does not create or populate Enrollments, accept Submissions,
consume an Attempt, resolve a Session configuration, create a Session, or
enable model execution.

# Governing sources

- `AGENTS.md`, `.agents/skills/implementation-workflow/SKILL.md`,
  `.work/README.md`, and `.cursor/rules/06-implementation-workflow.mdc` —
  tracked implementation state, specification-driven TDD, verification, and
  completion rules
- `.agents/skills/developer/SKILL.md`, `backend-developer`,
  `frontend-developer`, `business-analyst`, `architect`, `ui-ux-designer`, and
  `security-privacy-reviewer` — full-stack, requirements, boundary, UX, and
  threat-model quality bars used to prepare and execute this plan
- `docs/README.md#authority-by-concern` — product, requirements, UI/UX,
  architecture, and implementation authority boundaries
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — canonical Organization, Activity, Campaign,
  Task, Cohort, fairness, Stable-memory, and MVP scope
- `docs/requirements/features/assessment-setup.md` — `REQ-ACT-1`–
  `REQ-ACT-42`, `AC-ACT-1`–`AC-ACT-27`, and approved `PROP-1`–`PROP-6`
- `docs/requirements/features/auth-resource-isolation.md` — applicable
  authentication, Organization/resource authorization, scoped-query,
  commit-time reauthorization, non-disclosure, and required-durable audit
  contracts, including `REQ-AUTH-31` and the `AC-AUTH-*` criteria cited by the
  Assessment setup specification
- `docs/requirements/features/resolved-session-configuration.md` — downstream
  `REQ-RSC-9`–`REQ-RSC-14` baseline-consumption contract; this slice produces
  a consumable baseline but does not resolve a Session
- `docs/requirements/features/submission-attempts.md` and
  `docs/architecture/decisions/ADR-005-atomic-attempt-start-and-submission-binding.md`
  — downstream Enrollment, Submission, Attempt, and Session-start boundary;
  explicitly outside this slice
- `docs/ui-ux/activity-campaign-journey.md` and
  `docs/ui-ux/assessment-campaign-setup.md` — approved Activity IA, setup
  hierarchy, states, copy, focus, recovery, accessibility, responsive behavior,
  and Playwright evidence matrix
- `docs/ui-ux/design-system/README.md` and
  `docs/ui-ux/design-system/implementation-guide.md` — approved shared UI
  authority and module-selection process
- Applicable design-system modules: accessibility, colors, typography, layout,
  density, interaction states, status; buttons, inputs, selection controls,
  error summary, alerts, badges, panels, lists, dropdowns, dialogs, tables,
  navigation rails, and content grids; empty/loading, protected content,
  technical metadata, workflow, Stable-memory summary, and Harness assignment
  presentation within approved Assessment scope
- `docs/architecture/decisions/ADR-001-resolved-configuration-representation-and-integrity.md`,
  `ADR-002-authorization-enforcement-and-delegation.md`,
  `ADR-003-authorization-audit-persistence.md`,
  `ADR-004-assessment-activation-baseline-and-atomicity.md`, and
  `ADR-006-mvp-architecture-baseline-and-evolution.md` — canonical integrity,
  authorization, audit, activation atomicity, and modular-monolith boundaries
- `docs/architecture/mvp-architecture.md` and
  `docs/architecture/backend-module-architecture.md` — Assessment
  Configuration ownership, transaction rules, module/adaptor placement,
  production API authority, isolation, quality attributes, and verification
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
  — .NET/React/PostgreSQL/Grate, locked dependency, contract, and delivery gates
- `contracts/schemas/v1/digest/activation-baseline-digest-document.v1.schema.json`
  and `contracts/fixtures/jcs/activation-baseline-jcs-sha256-v1/` — existing
  canonical baseline schema and language-neutral conformance fixtures
- `.work/active/postgres-authorization-configuration-foundation.md`,
  `.work/active/p0-activity-journey-frontend-realization.md`, and
  `.work/active/oidc-application-session-foundation.md` — completed
  authorization/configuration, synthetic presentation, and production human
  authentication predecessors and their explicitly deferred boundaries

# Current implementation inventory

- There is no production Assessment Configuration module, Activity/Cohort
  repository, readiness coordinator, activation coordinator, or production
  Assessment HTTP API.
- Migration head is `0033`. Migrations `0001`–`0004` provide Organization,
  actor grants, generic immutable configuration-source versions, append-only
  audit events, outbox items, and idempotency foundations. Applied migrations
  are immutable and must not be edited.
- `FlexAgent.Configuration` owns generic source/version registration and digest
  verification, but the current implementation exposes only a synthetic source
  kind and persists identity/digest metadata rather than the complete trusted
  compatibility/effective-value descriptors required by Assessment readiness.
- `FlexAgent.IdentityAccess` provides the application authorization kernel,
  commit-time PostgreSQL reauthorization, and the completed OIDC-backed opaque
  application session. Activity/Cohort actions and relationships are not yet
  represented.
- `FlexAgent.Postgres` provides transaction, UTC-time, append-only audit, and
  outbox building blocks that can participate in ADR-004's single primary-store
  transaction without becoming the owner of Assessment policy.
- The activation-baseline Draft 2020-12 schema and JCS fixtures already exist,
  but no Assessment-owned C# digest document, validator, baseline producer, or
  historical verifier uses them in a production workflow.
- The React SPA already contains Activities list/detail presentation, shared
  workspace components, responsive tokens, and representative synthetic
  Playwright journeys. Those pages currently call the non-production
  `/browser` adapter and do not implement the approved setup form, revision,
  source-selector, readiness, confirmation, reconciliation, or baseline-detail
  contracts against authoritative product state.
- The API host already composes production human authentication and protected
  Session SSE. It has no production Activity endpoints or reusable
  cookie-authenticated Assessment command antiforgery adapter.
- Planning was created from `acdea13`. The reviewed pre-implementation baseline
  is `0d1924a`; execution must record the then-current start SHA rather than
  assuming either value. Existing modifications to
  `.work/active/session-runtime-openrouter-synthetic-qualification.md`,
  `docs/README.md`, and the OpenRouter provider-profile documents are unrelated
  concurrent work and must be preserved.

# Scope

## In

- Introduce an Assessment Configuration module that owns Activity identity and
  revision lineage, the single MVP Task binding, Cohort identity/state,
  readiness results, activation attempts, immutable activation baselines,
  Cohort-to-baseline binding, and authorized baseline/history projections.
- Keep domain/application policy independent from HTTP, PostgreSQL, React, and
  canonicalization adapters. Split a module-owned infrastructure assembly when
  Npgsql/Dapper dependencies would otherwise cross the approved boundary.
- Extend the Configuration-owned source boundary only as needed to expose
  exact, immutable, Organization-scoped source identity, digest, source kind,
  lifecycle/availability, compatibility, capability, and effective-value
  metadata through a narrow application-facing port. Do not add Agent or
  Harness authoring.
- Define an explicitly named activation transaction coordinator and a
  transaction capability approved by Assessment Configuration and every
  Configuration/governance state owner participating in commit-time
  validation. Transaction-aware source/policy ports must read exact current
  versions within the authoritative consistency boundary; specify PostgreSQL
  isolation, version checks, and locking needed to prevent activation from
  committing concurrently with source or policy revocation. Do not substitute
  an ambient transaction, shared connection, Assessment-owned cross-module SQL,
  or a pre-transaction cached decision.
- Support the required pre-provisioned source families used by Assessment
  readiness: Organization policy/bounds, Agent revision, Harness revision,
  one Task and Submission-requirement revision, workflow/adaptive-follow-up
  policy, rubric/evaluation procedure, model deployment profile, knowledge
  references, Stable-memory no-read or one immutable snapshot, capability
  profile, and review/Release requirements.
- Add additive Grate migrations after `0033` with Organization-scoped composite
  keys and foreign keys, expected revision/optimistic concurrency, immutable
  Activity revision and baseline records, one-baseline-per-Cohort uniqueness,
  scoped activation idempotency and trusted command digest, append-only or
  state-constrained activation attempts, UTC timestamps/timezone identity, and
  database-enforced post-activation immutability.
- Implement draft create/resume/save, scoped list/search/count and exact source
  selectors, required field/time validation, current-policy narrowing,
  Stable-memory default no-read behavior, and stale-revision conflict recovery.
- Implement a trusted readiness evaluator that returns complete bounded
  category results without treating readiness as commit authority. Cover
  missing, mutable, stale, revoked, cross-Organization, incompatible,
  digest-mismatched, capability-widening, invalid memory, timing, audit, and
  exception-reference cases.
- Implement `activation-baseline-jcs-sha256-v1` using the approved schema,
  `FlexAgent.CanonicalJson`, SHA-256 lowercase encoding, sorted semantic sets,
  ordered behavioral arrays, schema/version/procedure identifiers, minimized
  protected references, and cross-language conformance fixtures. Assessment
  must own explicit positive canonicalization limits for UTF-8 bytes, nesting
  depth, object properties, and array elements, and must validate each
  fairness-domain `effective_value` against its domain contract before invoking
  canonicalization; the schema's open nested object and caller-supplied
  `CanonicalJsonLimits` are not production defaults.
- Implement ADR-004 activation with admission authorization and in-transaction
  reauthorization/source revalidation. Atomically accept one successful
  activation attempt, immutable baseline, unique Cohort binding, `Activated`
  transition, and required durable audit/outbox event, or none.
- Implement safe equivalent retry, mismatched idempotency conflict, stale
  expected revision, concurrent activation winner, pre-commit failure,
  post-commit/lost-response reconciliation, and honest degraded baseline
  verification without substitution.
- Add versioned production HTTP query/command contracts for Activity/Cohort
  lists, create/resume/save, source options, readiness, activation,
  reconciliation, baseline summary, technical provenance, and history. Derive
  actor and Organization from the application session; validate antiforgery for
  cookie-authenticated mutations; return stable non-sensitive errors and
  server-derived permitted actions.
- Replace the Activities production route's synthetic state with the real
  Assessment API while retaining the non-production synthetic adapter only for
  its explicitly bounded test/demo purpose. Keep server, URL, form, local, and
  ephemeral UI state distinct.
- Return **Assign Participants** only when a production Enrollment capability
  and separately authorized destination actually exist. Until that downstream
  feature is available, omit the permitted action and never route production
  users into the synthetic `/browser` Enrollment flow.
- Implement the approved workspace-density setup hierarchy and states:
  protected loading, initial/empty, draft/unsaved, saving/saved, validation,
  stale conflict, readiness checking/blocked/warning/ready/out-of-date,
  confirmation, activating, uncertain/reconciling, failure/retry, access
  changed/denied, activated summary, degraded verification, and
  new-Cohort-required explanation.
- Provide accessible names, landmarks/headings, error-summary links, deliberate
  confirmation, focus movement/restoration, status announcements, preserved
  safe input, timezone descriptions, keyboard-only operation, reduced motion,
  both themes, 400 percent zoom/reflow, and desktop/narrow behavior.
- Add bounded telemetry for draft/save conflicts, readiness and activation
  latency, activation outcomes/deduplication/reconciliation, isolation and
  integrity failures, memory choices, audit acceptance failure, verification
  status, and post-activation mutation attempts without protected content or
  unrestricted identifiers.
- Update authoritative implementation-status/traceability rows only after
  executable evidence supports them, without claiming Enrollment, Session
  start, full `AC-OPS-4`, provider qualification, or production-pilot readiness.

## Out

- Reusable Agent or Harness creation/editing, general workflow builders,
  rubric authoring, model-profile self-service, knowledge management, memory
  management, or P1/P2/P3 capability expansion
- Enrollment/participant assignment mutation, accommodations, Submission
  intake/uploads/artifact storage, Attempt entitlement or start, exact
  Submission binding, resolved Session configuration, execution manifest,
  hosted Session creation, or model execution
- Dynamic memory, tools, voice, shared Sessions, uncontrolled adaptive
  behavior, or any capability beyond the approved P0 Assessment profile
- A general exception-request/approval workflow. This slice validates an exact
  current separately approved exception reference when such a governing record
  exists and otherwise fails closed; it does not invent exception authority.
- Raw prompts, knowledge/rubric contents, secrets, provider tokens, Participant
  data, or protected payload copies in the baseline, API projections,
  telemetry, errors, browser storage, or Playwright artifacts
- Distributed transactions, a broker, Redis authority, a separate Assessment
  service, or external calls inside the activation transaction
- Commit, push, pull request, deployment, production enablement, data migration
  from an external system, or real customer/Participant data unless separately
  requested and authorized

# Acceptance and verification mapping

| Contract | Implementation surfaces | Required evidence |
| --- | --- | --- |
| Draft, exact sources, widening prevention, timing, optimistic concurrency (`REQ-ACT-1`–`REQ-ACT-8`; `AC-ACT-1`–`AC-ACT-5`, `AC-ACT-18`, `AC-ACT-19`, `AC-ACT-23`) | Assessment domain/application, Configuration source port, PostgreSQL revisions and scoped queries, Activity list/setup API and form | Domain red/green; PostgreSQL wrong-scope/list/count and stale-save races; API contract tests; empty/selected/unavailable/time/conflict component and browser states |
| Readiness and commit-time revalidation (`REQ-ACT-9`–`REQ-ACT-13`; `AC-ACT-4`, `AC-ACT-6`, `AC-ACT-27`) | Readiness evaluator, exact source/policy readers, bounded category projection, telemetry | Missing/stale/mutable/revoked/digest/compatibility matrix; source/permission change race; representative p95 load evidence; checking/blocked/warning/ready/out-of-date UI |
| Atomic activation and immutable baseline (`REQ-ACT-14`–`REQ-ACT-24`, `REQ-ACT-41`, `REQ-ACT-42`; `AC-ACT-7`, `AC-ACT-8`, `AC-ACT-13`–`AC-ACT-17`, `AC-ACT-25`) | Baseline schema/producer/verifier, activation coordinator, migration constraints, audit/outbox, idempotency/reconciliation API, confirmation UI | JCS/schema fixtures; one-field digest changes; in-transaction reauthorization; fault injection at each write/audit step; duplicate/mismatch/concurrency/lost-response tests; confirmation/pending/reconciliation/success/failure browser evidence |
| Cohort, Stable memory, and P0 capability profile (`REQ-ACT-25`–`REQ-ACT-34`; `AC-ACT-9`–`AC-ACT-12`, `AC-ACT-19`, `AC-ACT-26`) | Cohort state, timing bounds, memory/capability validator, effective-value summary | Empty-Cohort activation; default no-read; exact snapshot; cross-scope snapshot; Dynamic/tool/voice/shared-session rejection; timing-zone edge cases; summary UI |
| Audit, inspection, lifecycle, and non-disclosure (`REQ-ACT-35`–`REQ-ACT-40`; `AC-ACT-17`, `AC-ACT-20`, `AC-ACT-21`) | Required-durable audit/outbox transaction, baseline/history query authorization, degraded verification | Audit failure rollback/redaction; administrator/reviewer/Participant matrix; append-only/immutability tests; raw-content/log/error leakage scan; readable then technical summary and participant denial states |
| Accessible/responsive interaction (`AC-ACT-22`) | React setup workspace and shared design-system components | Component accessibility/focus/announcement tests; Playwright accessibility snapshots and desktop/narrow screenshots; keyboard-only, dialog, error, permission-loss, both-theme, reduced-motion, and 400-percent reflow checks |
| Negative release gate (`AC-ACT-24`) | Domain, adapter, API, frontend, integration, and browser suites | Complete trace matrix for unauthorized, wrong scope/parent, stale, mutable/digest mismatch, widening, memory, exception, audit, retry, concurrent activation, and post-activation mutation cases; no missing/failing applicable negative row |
| Downstream resolver compatibility (`REQ-ACT-24`; `REQ-RSC-9`–`REQ-RSC-14`) | Versioned baseline read contract and verifier port | Round-trip exact baseline identity/digest/ownership/source/fairness classifications into a resolver-facing contract test without creating a Session |

# Security and privacy threat model

| Threat or privacy harm | Planned controls | Verification |
| --- | --- | --- |
| Forged Organization/Activity/Cohort/source identifiers or client-provided role/digest become authority | Derive application-session actor/Organization; scoped repositories; trusted parent traversal; admission plus in-transaction authorization/source revalidation; non-disclosing failures | Wrong-Organization/activity/cohort/source, guessed ID, forged parent, client Organization/role/digest, deep-link, and permission-revocation tests |
| Lists, counts, selectors, caches, pagination, errors, or loading states reveal inaccessible resources | Scope before materialization/counting; bounded projections; protected loading; no stale protected cache; nonexistent/inaccessible equivalence | Cross-scope list/search/count/page metadata, cached flash, access-changed, API error, browser storage, and screenshot inspection |
| Mutable aliases, stale source state, digest substitution, or post-read races corrupt the fairness baseline | Exact immutable versions/digests; commit-time reload; canonical schema validation; independent ownership binding; no latest-version fallback | Mutable/stale/revoked/mismatched source matrix, changed-after-read race, one-field digest fixtures, cross-Cohort equivalent digest and substitution tests |
| Lower scope widens capability, memory, timing, attempts, or policy | Deterministic most-restrictive resolver; non-bypassable Organization bounds; Stable/no-read default; exact exception validation | Widening property tests; Dynamic/tool/voice/shared-session negatives; invalid/unauthorized/stale exception tests |
| Duplicate, concurrent, replayed, or uncertain activation creates multiple baselines or false success | Scoped idempotency key plus trusted command digest; expected revision; unique binding; atomic transaction; authoritative reconciliation | Equivalent retry, mismatched reuse, two-admin race, process/lost-response, pre/post-commit fault injection, and no blind retry tests |
| Audit failure yields activated state without required history | Baseline/state/audit-or-outbox append in one transaction; `required_durable` fail closed | Audit/outbox insertion failure at each boundary; assert no activated Cohort/baseline and bounded operational alert |
| Raw prompts, rubric/knowledge content, secrets, Participant data, or unrestricted identifiers escape through baseline, API, logs, telemetry, or artifacts | Minimized protected references/digests; allowlisted DTOs/telemetry labels; inert text rendering; synthetic browser evidence only | Serialization/log/metric/error/URL/storage/gitleaks scan; control-spoof/XSS labels; screenshot artifact inspection |
| Post-activation mutation rewrites fairness history | Immutable baseline rows/triggers or constrained commands; activated state has no material edit path; new Cohort/revision handoff | Repository/domain/API mutation denial, database update/delete attempts, UI absence of edit controls, new-Cohort explanation |
| Unbounded readiness/source enumeration or activation floods exhaust one tenant or reveal timing | Positive bounds, pagination, request/body limits, bounded validation categories, gateway/rate policy, p95 telemetry without protected labels | Oversized source set/body, pagination, repeated readiness/activation, cancellation/timeout, and Organization-isolation load checks |

# Decisions

- Use one tracked full-stack task because the production API contract and
  approved setup UI must be implemented and verified together. Keep backend,
  frontend, security, and review evidence distinct inside the task.
- Name the new logical module **Assessment Configuration** and let it own
  Activity revisions, Tasks, Cohorts, readiness, activation attempts,
  baselines, and bindings as assigned by the MVP architecture. Concrete
  Npgsql/Dapper adapters belong in a module-owned infrastructure boundary.
- Keep immutable source records owned by their Configuration/governance
  boundaries. Assessment consumes narrow versioned read contracts and stores
  only normalized effective values plus stable protected references/digests;
  it must not query another module's tables through Assessment-owned SQL.
- Reuse `FlexAgent.CanonicalJson` and the existing language-neutral baseline
  schema/fixtures. Do not reuse the resolved-configuration procedure ID or
  treat a matching digest as ownership or authorization.
- Use the primary PostgreSQL transaction for ADR-004. No remote call,
  projector, cache, or browser state participates in activation authority.
- Use the completed opaque application session and current antiforgery
  contract for production HTTP. The synthetic `/browser` actor/session remains
  non-production evidence and cannot be upgraded into product authority by
  reusing its in-memory state.
- Return server-derived permitted actions and stable reason categories. The
  React client renders authoritative state and recovery but never infers
  authorization, readiness, activation, digest validity, or idempotent outcome.
- Keep assignment as an authorized handoff only. The activated success state
  may offer **Assign Participants** only when the production server proves a
  separately authorized, implemented Enrollment destination. Otherwise it
  omits the action. This task does not implement the mutation, populate a
  Cohort, imply an Enrollment exists, or reuse the synthetic Enrollment route.
- Make the Assessment activation transaction coordinator explicit in the
  application contract. Configuration/governance source and policy owners must
  expose transaction-aware validation capabilities approved for the shared
  PostgreSQL consistency boundary, including exact-version/revocation checks
  and defined isolation/locking behavior. An independent read connection,
  ambient transaction, or cached readiness result cannot satisfy ADR-004.
- Define Assessment-owned production `CanonicalJsonLimits` and versioned
  fairness-domain validators before producing a baseline. Reject unknown,
  oversized, over-deep, or otherwise invalid `effective_value` structures
  before hashing or persistence; do not rely on the digest schema's
  `additionalProperties: true` as a domain validation contract.
- A matching separately approved exception reference may be validated, but no
  general exception workflow is introduced. With no applicable approved
  exception source, the fail-closed no-exception path is authoritative.

# Plan

- [ ] Establish and record the clean focused baseline at the actual starting
      commit while preserving unrelated OpenRouter task edits. Re-run current
      Configuration/IdentityAccess/Runtime/PostgreSQL/contract/architecture
      tests and web lint/type/unit/build/e2e smoke before behavior changes.
- [ ] Build a complete `REQ-ACT-*`/`AC-ACT-*` trace matrix and executable source
      prerequisite inventory. Define the minimum Configuration-owned immutable
      readiness descriptor/read port, transaction-aware commit-validation
      capability, exact source kinds, Organization policy bounds, source/policy
      revocation concurrency semantics, and seeded synthetic
      Development/Testing records. Identify every state-owning module that must
      approve the activation transaction capability. If a required source
      behavior or cross-module consistency contract is not approved or cannot
      be represented without a new durable decision, stop and promote a
      `Proposed` decision before coding.
- [ ] Define the shared versioned API/application contract before UI or SQL:
      authenticated actor context, Activity/Cohort locators, draft revision and
      expected-version rules, source option DTOs, readiness categories,
      activation command digest/idempotency, safe errors, reconciliation,
      baseline summary/provenance, permitted actions, antiforgery, pagination,
      request bounds, compatibility policy, the explicitly named activation
      transaction coordinator, transaction-aware owner ports, and the rule that
      assignment is absent unless a real production Enrollment destination is
      both implemented and authorized.
- [ ] Red — add Assessment domain tests for the Activity revision, one Task,
      Cohort lifecycle, Stable-memory default, exact source identities,
      deterministic narrowing, time/attempt bounds, prohibited capabilities,
      stale save, post-activation immutability, and new-Cohort-required rules.
- [ ] Green/refactor — implement the minimum Assessment domain and application
      commands/queries/ports with no HTTP, SQL, React, or provider dependencies;
      keep errors stable, bounded, and non-disclosing.
- [ ] Red — add readiness tests for every required category and negative case:
      missing/mutable/stale/revoked/wrong-scope/digest-mismatched/incompatible
      sources, upper/lower-scope conflict, invalid memory/timing, unavailable
      audit, exception reference, and source/permission change after readiness.
- [ ] Green/refactor — implement the trusted readiness evaluator and bounded
      result projection. Preserve readiness as advisory and keep commit-time
      authorization/source/policy revalidation mandatory.
- [ ] Red — extend schema/JCS tests and fixtures for baseline construction and
      verification: ordering variations, Unicode, numeric/time bounds, every
      fairness-domain one-field change, excluded binding metadata, equivalent
      content across Cohorts, missing/altered source digests, procedure/schema
      mismatch, degraded historical verification, domain-invalid or unknown
      `effective_value` members, and byte/depth/property/array limit overflow.
- [ ] Green/refactor — implement the Assessment-owned baseline document,
      validator, canonicalizer/digest producer, protected persisted form, and
      resolver-facing read/verifier contract using the existing schema,
      versioned domain validators, explicit Assessment-owned
      `CanonicalJsonLimits`, and `FlexAgent.CanonicalJson`.
- [ ] Red — add migration and PostgreSQL integration tests for additive upgrade
      from populated `0033`, composite Organization ownership, Activity
      revision lineage, Cohort state, scoped idempotency, one baseline binding,
      append-only/immutable history, UTC/timezone materialization, exact source
      references, list/count isolation, and database-level forbidden updates.
- [ ] Green/refactor — add the next immutable migrations and module-owned
      repositories/adapters. Keep parameterized SQL explicit, constrain scope
      before materialization, use database time where authoritative, and add
      architecture tests for module/table/dependency ownership.
- [ ] Red — add ADR-004 coordinator integration tests for admission denial,
      commit-time grant/source/policy revoke, failure before each write,
      audit/outbox failure, duplicate equivalent retry, mismatched key,
      concurrent administrators, stale expected revision, lost response,
      post-commit reconciliation, no partial authority, and PostgreSQL races in
      which a concurrent source or policy revoke must serialize safely or make
      activation fail without committing a baseline.
- [ ] Green/refactor — implement the single-transaction activation coordinator,
      transaction-aware Configuration/governance validation ports with approved
      isolation/locking/version behavior, required-durable audit/outbox
      acceptance, authoritative reconciliation, bounded telemetry, and honest
      historical verification state.
- [ ] Red — add production API/runtime contract tests for every query/mutation,
      application-session and MFA requirements where governed, CSRF, forged
      scope/parent/digest/role values, request and pagination bounds, stable
      status/error mapping, permitted actions, protected loading/unavailable
      behavior, and access loss between read and mutation.
- [ ] Green/refactor — compose thin Assessment endpoints in `FlexAgent.Api`
      over the application ports and PostgreSQL adapters. Keep the synthetic
      browser endpoints separate and default-off outside their existing
      Development/Testing harness.
- [ ] Red — add React contract/component tests for source selectors and every
      approved setup state, including unsaved navigation, save failure,
      two-tab stale conflict, readiness summaries, activation confirmation,
      pending/uncertain reconciliation, audit/persistence failure, permission
      loss, activated/degraded summaries, and new-Cohort explanation. Assert by
      role and accessible name.
- [ ] Red — prove activated-success action gating: omit **Assign Participants**
      when no production Enrollment destination exists or authorization is
      absent; when a later production capability is supplied, use only its
      server-returned destination and never the synthetic `/browser` route.
- [ ] Green/refactor — implement the production Assessment client/state model,
      Activity list/setup/baseline pages, workspace-density layout, safe text,
      progressive technical metadata, semantic tokens, focus/announcement
      behavior, and desktop/narrow reflow without duplicating server policy.
- [ ] Run the real PostgreSQL/API/SPA journey through OIDC-backed application
      sessions and the project Playwright MCP server. Reach and inspect
      authorized, empty, selected, invalid, stale, blocked, warning, ready,
      confirmation, activating, uncertain/reconciled, success, degraded,
      denied, and access-revoked states using synthetic data only. Capture
      accessibility snapshots plus desktop/narrow, keyboard-focus, dialog,
      error, both-theme, reduced-motion, and 400-percent-zoom screenshots only
      under `.playwright-mcp/`; fix and repeat until evidence supports the UI.
- [ ] Run focused then aggregate verification: Assessment domain/application,
      Configuration and IdentityAccess regression, PostgreSQL migration and
      fault/concurrency suites, API/runtime, contracts/JCS, architecture,
      locked solution restore/test, web lint/type/unit/build/e2e, docs,
      whitespace, gitleaks, supply-chain/SBOM, and OCI checks. Record exact
      commands, counts, durations, and unavailable gates.
- [ ] Run independent backend/architecture, frontend, and security/privacy
      review of the completed change set; resolve all blocking findings without
      weakening tests or expanding scope.
- [ ] Reconcile actual changes against every mapped requirement and governing
      source. Update authoritative implementation/readiness rows truthfully,
      retain downstream Enrollment/Submission/Attempt/Session gaps, record all
      remaining evidence, mark this task completed, and preserve it for the
      next vertical-slice handoff.

# Current state

Planning is ready to begin baseline execution and the source-contract and
transaction-capability inventory. No production Assessment behavior has been
implemented by this task. The first implementation step is to record the
observed baseline at the actual starting commit and prove the exact
pre-provisioned source metadata and approved cross-module transaction capability
needed by commit-time validation before adding a new module or migration.

The completed synthetic Activity UI is a presentation and browser-test
predecessor only. The completed OIDC/application-session and authorization
foundations are production predecessors. This task must connect new
authoritative Assessment state to those production boundaries without silently
turning the synthetic scenario model into product persistence.

# Findings / deviations

- The original next-item description could be read as a backend-only
  foundation. The approved Assessment setup specification includes an
  authoritative administrator interaction surface and `AC-ACT-22`; this plan
  therefore covers the complete production-backed vertical slice while keeping
  Enrollment and Session start outside scope.
- The existing generic configuration-version table proves immutable identity
  and digest registration but does not yet prove the complete trusted
  compatibility/effective-value metadata required by Assessment readiness.
  That prerequisite is an explicit first gate, not an assumption.
- The activation baseline schema and JCS fixtures already exist and must be
  reused. Implementation work is producer/verifier/domain/persistence/API/UI
  adoption, not invention of another baseline format.
- The current Activities UI uses an in-memory non-production `/browser`
  adapter. Its components and state examples are useful, but its server state,
  actor model, commands, and verification cannot satisfy production Assessment
  requirements.
- No product, requirements, UI/UX, or ADR open question is currently recorded
  for Assessment setup. Any consequential ambiguity discovered during source
  or contract mapping must receive an interim default and rationale and be
  promoted as `Proposed` before implementation depends on it.
- Readiness review found that a narrow source read port alone was insufficient
  to explain ADR-004 commit-time revalidation across module ownership. The plan
  now gates persistence work on an explicitly approved transaction-aware
  coordinator/port contract and concurrent revocation evidence.
- Readiness review removed the unsupported promise that this slice populates a
  Cohort. Empty-Cohort activation is required; populated membership and its
  mutation remain owned by the later Enrollment feature.
- Readiness review made the assignment action conditional on a real production
  Enrollment destination and prohibited handoff to the synthetic route.
- Readiness review made canonical JSON resource limits and domain-specific
  `effective_value` validation explicit because the shared schema and
  canonicalizer intentionally do not provide production defaults.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Workflow and role guidance | passed for planning | Requested implementation workflow plus developer, backend, frontend, business-analysis, architecture, UI/UX, and security/privacy guidance loaded on 2026-08-20 |
| Governing source review | passed for planning | Product foundation, Assessment requirements, setup interaction specification, design-system authority/modules, ADR-001–ADR-006, MVP/backend architecture, completed predecessors, schema, migrations, API, module, and SPA seams inspected |
| Independent plan readiness review | passed with corrections applied | Cross-module transaction capability, empty-Cohort scope, Enrollment action gating, canonicalization limits, and repository-baseline corrections incorporated on 2026-08-20 |
| Existing task collision | passed | No active task owns production P0 Assessment setup/Cohort activation; completed frontend task explicitly limits itself to the synthetic boundary |
| Repository baseline | pending | Record actual start SHA, clean/dirty paths, solution/migration/contract heads, and baseline commands before implementation |
| Assessment focused tests | pending | Domain/application test project does not yet exist |
| PostgreSQL migration/integration | pending | Additive post-`0033` migration, populated upgrade, isolation, concurrency, and ADR-004 fault injection not implemented |
| API/runtime contracts | pending | Production Assessment endpoints and application-session/CSRF authorization matrix not implemented |
| Web unit/accessibility | pending | Production Assessment client/setup states not implemented |
| Playwright MCP | pending | Must use real app interactions, accessibility snapshots, and desktop/narrow screenshots in `.playwright-mcp/` after the slice runs |
| Performance | pending | `AC-ACT-27` readiness and activation p95 evidence not observed |
| Locked regression/supply-chain/OCI/docs/leakage | pending | Run proportionately after implementation |
| Independent reviews | pending | Backend/architecture, frontend, and security/privacy review required before completion |

# Blockers

No approved product or architecture decision blocks baseline inventory and
contract design. Migration and production activation behavior are gated on
proving or adding both the minimum immutable Configuration/governance source
metadata contract and the explicitly approved cross-module transaction
capability described in the plan.

If no permitted pre-provisioned source revision exists for a required category,
the authoritative outcome is a scoped empty/readiness-blocked state. The task
may seed bounded synthetic Development/Testing fixtures through approved
server-owned setup, but it must not invent a Production fallback, mutable alias,
cross-Organization default, or fake activatable placeholder.

# Completion

- [ ] Planned work is reconciled with actual changes and the observed starting baseline
- [ ] Every `REQ-ACT-1`–`REQ-ACT-42` / `AC-ACT-1`–`AC-ACT-27` row is mapped to implementation and executable evidence without claiming downstream features
- [ ] Assessment Configuration owns Activity revisions, Task, Cohort, readiness, activation attempts, immutable baselines, bindings, and authorized projections through approved module boundaries
- [ ] Exact source identities, Organization/policy bounds, Stable-memory rules, capability narrowing, time semantics, and no-fallback behavior are enforced server-side
- [ ] `activation-baseline-jcs-sha256-v1` production and verification conform to the approved schema, JCS fixtures, digest coverage, and independent ownership binding
- [ ] ADR-004 activation, required-durable audit/outbox, idempotency, concurrency, uncertain-response reconciliation, and post-activation immutability pass PostgreSQL fault/race tests
- [ ] Production HTTP uses the opaque application session, current authorization, commit-time reauthorization, antiforgery, scoped queries, bounded contracts, and non-disclosing errors
- [ ] The React setup journey implements every applicable approved state without using synthetic browser state as product authority
- [ ] Accessibility, focus, keyboard, announcements, both themes, reduced motion, desktop/narrow, and 400-percent reflow have live Playwright evidence under `.playwright-mcp/`
- [ ] Negative isolation/security/privacy coverage required by `AC-ACT-24` is complete and passing
- [ ] Applicable focused, integration, concurrency, migration, performance, locked regression, supply-chain, OCI, documentation, whitespace, and leakage checks pass or are recorded precisely as unavailable
- [ ] Authoritative implementation-status rows are updated truthfully; Enrollment, Submission/Attempt, resolved Session configuration/start, provider, and remaining production gates stay explicit
- [ ] Independent backend/architecture, frontend, and security/privacy findings are resolved
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
