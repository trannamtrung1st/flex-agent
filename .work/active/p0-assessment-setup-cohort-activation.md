---
id: p0-assessment-setup-cohort-activation
status: completed
created: 2026-08-20
updated: 2026-08-21
---

# Goal

Implement the production-backed P0 Assessment Campaign setup and Cohort
activation vertical slice. An authenticated, MFA-qualified, currently
authorized Activity administrator must be able to create and resume an
assessment Activity draft,
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
- `docs/requirements/mvp-operational-defaults.md` — opaque application-session,
  one server-derived Organization, revocation, and mandatory Administrator and
  Reviewer MFA contracts, especially `REQ-OPS-11`–`REQ-OPS-16` and
  `REQ-OPS-28`
- `docs/requirements/features/assessment-setup.md` — `REQ-ACT-1`–
  `REQ-ACT-42`, `AC-ACT-1`–`AC-ACT-27`, and approved `PROP-1`–`PROP-7`
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
  — .NET/React/PostgreSQL/Grate, IdentityAccess/Keycloak ownership, authenticated
  Development/Testing browser profile, locked dependency, contract, and
  delivery gates
- `docs/operations/provider-profiles/keycloak-oidc-contract.md` — pinned local
  Keycloak boundary and the synthetic MFA Administrator/API/SPA/PostgreSQL
  composition required for authenticated browser evidence
- `contracts/schemas/v1/digest/activation-baseline-digest-document.v1.schema.json`
  and `contracts/fixtures/jcs/activation-baseline-jcs-sha256-v1/` — existing
  canonical baseline schema and language-neutral conformance fixtures
- `.work/active/postgres-authorization-configuration-foundation.md`,
  `.work/active/p0-activity-journey-frontend-realization.md`, and
  `.work/active/oidc-application-session-foundation.md` — completed
  authorization/configuration, synthetic presentation, and production human
  authentication predecessors and their explicitly deferred boundaries
- `.work/active/session-runtime-live-provider-qualification.md` plus the
  Sessions installed-profile, qualification, and credential-catalog contracts
  — completed predecessor owning the current model-deployment operator-state
  boundary; deterministic OpenAI-compatible migration exists, but no exact live
  profile is qualified or enabled. Assessment consumes exact profile identity
  and current eligibility; credential binding remains downstream Session
  authority.

# Initial implementation inventory at planning baseline

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
- Sessions now owns strict operator-installed model-deployment profiles,
  adapter-configuration/qualification records, and credential-catalog loading.
  Those records are file-loaded into in-memory registries and therefore do not
  yet provide a PostgreSQL transaction capability that can serialize activation
  against profile identity, qualification, or eligibility changes. Credential
  binding and credential revocation are resolved later by Session
  binding/execution and are not activation-baseline content. No exact
  OpenAI-compatible live profile is currently qualified; Production must remain
  readiness-blocked for that category until an exact permitted profile exists.
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
- The SPA is globally composed through `BrowserApiProvider`; it has no
  production `/auth/session` bootstrap, `/auth/login` handoff, CSRF-token
  lifecycle, or production/synthetic client-mode boundary. Its current
  authentication gate explicitly supports only synthetic scenario grants.
- The API host already composes production human authentication and protected
  Session SSE. ASP.NET antiforgery registration and `/auth/session` token issue
  exist, but there is no shared protected-mutation endpoint adapter and no
  production Activity endpoint.
- Planning was created from `acdea13` and originally reviewed at `0d1924a`.
  The refreshed pre-implementation inventory is `ef911ee`; at that point the
  provider task was completed and the only observed working-tree modification
  was this plan. Execution must still record the actual start SHA and dirty
  paths immediately before behavior changes and preserve unrelated work.

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
- Before persistence design, produce a source-authority matrix for every
  required category naming its owning module, immutable identity, lifecycle and
  revocation authority, effective-value validator, selector projection,
  transaction participation, and Development/Testing fixture source. Treat the
  current Sessions file/in-memory model-profile registry as a concrete
  unresolved boundary, not as PostgreSQL-serializable authority.
- Define an explicitly named activation transaction coordinator and a
  transaction capability approved by Assessment Configuration and every
  Configuration/governance state owner participating in commit-time
  validation. Transaction-aware source/policy ports must read exact current
  versions within the authoritative consistency boundary; specify PostgreSQL
  isolation, version checks, and locking needed to prevent activation from
  committing concurrently with source or policy revocation. Do not substitute
  an ambient transaction, shared connection, Assessment-owned cross-module SQL,
  or a pre-transaction cached decision.
- Promote the model-profile/qualification/revocation transaction design and any
  other consequential unresolved source-owner behavior into an approved ADR or
  labeled `PROP-*` before activation migrations or application code depend on
  it. **Interim default:** fail closed and return a bounded readiness blocker
  when a source owner cannot supply an exact transactionally revalidated
  version; this prevents a fairness baseline from freezing stale authority.
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
  cookie-authenticated mutations; enforce required authentication strength on
  the server according to the authorized action and relationship, including MFA
  for Administrator setup access and Reviewer baseline/provenance access;
  return stable non-sensitive errors and server-derived permitted actions. Do
  not trust the browser-visible `mfa_present` value or an identity adapter that
  discards Organization, relationship, or authentication-strength context.
- Add a production SPA application-session boundary that obtains
  `/auth/session`, retains the CSRF request token only in bounded in-memory
  client state, initiates `/auth/login`, and consumes a versioned safe shell
  context with server-derived actor, bound Organization, navigation
  destinations, and permitted actions. Handle session expiry and access loss,
  and keep the synthetic `/browser` provider explicitly isolated to its
  Development/Testing mode; authentication state alone must not become shell or
  navigation authority.
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
- Session credential-binding selection, credential-secret resolution, and
  execution-time credential revocation checks; Assessment freezes and validates
  only the exact non-secret model-deployment profile identity and its current
  activation eligibility
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
| Production application session, shell context, and relationship-sensitive MFA (`REQ-OPS-11`–`REQ-OPS-16`, `REQ-OPS-28`; applicable `AC-AUTH-*`) | Assessment endpoint identity adapter, server-side authentication-strength policy, `/auth/session` and CSRF bootstrap, versioned server-derived actor/Organization/navigation/permitted-action projection, production SPA provider | Missing/expired/revoked/wrong-Organization/no-MFA Administrator and Reviewer negatives; forged relationship/navigation/action; CSRF failure; privilege-change/access-loss handling; production-versus-synthetic composition tests |
| Accessible/responsive interaction (`AC-ACT-22`) | React setup workspace and shared design-system components | Component accessibility/focus/announcement tests; Playwright accessibility snapshots and desktop/narrow screenshots; keyboard-only, dialog, error, permission-loss, both-theme, reduced-motion, and 400-percent reflow checks |
| Negative release gate (`AC-ACT-24`) | Domain, adapter, API, frontend, integration, and browser suites | Complete trace matrix for unauthorized, wrong scope/parent, stale, mutable/digest mismatch, widening, memory, exception, audit, retry, concurrent activation, and post-activation mutation cases; no missing/failing applicable negative row |
| Downstream resolver compatibility (`REQ-ACT-24`; `REQ-RSC-9`–`REQ-RSC-14`) | Versioned baseline read contract and verifier port | Round-trip exact baseline identity/digest/ownership/source/fairness classifications into a resolver-facing contract test without creating a Session |

# Security and privacy threat model

| Threat or privacy harm | Planned controls | Verification |
| --- | --- | --- |
| Forged Organization/Activity/Cohort/source identifiers or client-provided role/digest become authority | Derive application-session actor/Organization; scoped repositories; trusted parent traversal; admission plus in-transaction authorization/source revalidation; non-disclosing failures | Wrong-Organization/activity/cohort/source, guessed ID, forged parent, client Organization/role/digest, deep-link, and permission-revocation tests |
| An authenticated Administrator or Reviewer without required MFA, or a client-forged relationship/MFA flag, reaches protected setup or baseline authority | Validate current application-session authentication strength on the server for each authorized action and relationship; never authorize from `mfa_present`, client role state, navigation, or permitted-action projections | Administrator command/query and Reviewer baseline-read cases with missing/unrecognized ACR/AMR, downgraded/expired session, forged browser state, and privilege-change rotation tests |
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
- Add a distinct production SPA session/API provider rather than mutating
  `BrowserApiProvider` into mixed authority. Production bootstrap uses
  `/auth/session`, carries its CSRF token only in memory, initiates the existing
  OIDC login endpoint, consumes a versioned safe server-derived shell-context
  projection, and removes protected state on expiry or access loss. The shell
  projection supplies actor, bound Organization, navigation, and permitted
  actions for rendering only; every resource request remains independently
  authorized.
- Enforce `REQ-OPS-16` at the Assessment server boundary from the current
  authenticated application session and authorized relationship: Administrator
  setup access and Reviewer baseline/provenance access require MFA. Browser
  `mfa_present`, role labels, navigation, and permitted-action projections are
  presentation input and cannot satisfy authorization.
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
- Do not assume the current Sessions installed-profile or qualification files
  can participate in that transaction. The owning-module contract for exact
  model-profile identity, qualification, eligibility, and availability is an
  unresolved durable architecture decision. Until approved, the interim
  behavior is a non-sensitive readiness blocker and no Production activation.
  Credential-binding selection, secret resolution, and credential revocation
  remain downstream Session binding/execution concerns under `REQ-RSC-30` and
  `REQ-RSC-46`; they are not activation-baseline content or activation
  transaction participants.
- Define Assessment-owned production `CanonicalJsonLimits` and versioned
  fairness-domain validators before producing a baseline. Reject unknown,
  oversized, over-deep, or otherwise invalid `effective_value` structures
  before hashing or persistence; do not rely on the digest schema's
  `additionalProperties: true` as a domain validation contract.
- A matching separately approved exception reference may be validated, but no
  general exception workflow is introduced. With no applicable approved
  exception source, the fail-closed no-exception path is authoritative.

# Plan

- [x] Establish and record the observed focused baseline at the actual starting
      commit, including dirty paths, while preserving unrelated work and the
      completed `session-runtime-live-provider-qualification` predecessor.
      Re-run current Configuration/IdentityAccess/Runtime/Sessions provider/
      PostgreSQL/contract/architecture tests and web lint/type/unit/build/e2e
      smoke before behavior changes.
- [x] Build a complete `REQ-ACT-*`/`AC-ACT-*` trace matrix and executable source
      prerequisite inventory. Produce the source-authority matrix naming owner,
      identity, lifecycle/revocation, domain validator, selector, transaction
      participation, and fixture source for every category. Define the minimum
      Configuration-owned immutable readiness descriptor/read port,
      transaction-aware commit-validation capability, exact source kinds,
      Organization policy bounds, source/policy revocation concurrency
      semantics, and seeded synthetic Development/Testing records. Explicitly
      reconcile the Sessions file/in-memory model-profile and qualification/
      eligibility boundary and the absence of an exact qualified live profile.
      Record credential binding as a downstream Session concern, not an
      activation source. Identify every state-owning module that must approve
      the activation transaction capability. If a required source behavior or
      cross-module consistency contract is not approved or cannot be represented
      without a new durable decision, stop and promote an ADR or labeled
      `PROP-*` before activation persistence or application coding.
- [x] Define the shared versioned API/application contract before UI or SQL:
      authenticated actor context, versioned safe shell-context projection,
      server-derived bound Organization/navigation/permitted actions,
      Activity/Cohort locators, draft revision and expected-version rules,
      source option DTOs, readiness categories, activation command digest/
      idempotency, safe errors, reconciliation, baseline summary/provenance,
      antiforgery, pagination, request bounds, compatibility policy,
      relationship-sensitive Administrator/Reviewer MFA, the explicitly named
      activation transaction coordinator, transaction-aware owner ports, and
      the rule that assignment is absent unless a real production Enrollment
      destination is both implemented and authorized.
- [x] Red — add Assessment domain tests for the Activity revision, one Task,
      Cohort lifecycle, Stable-memory default, exact source identities,
      deterministic narrowing, time/attempt bounds, prohibited capabilities,
      stale save, post-activation immutability, and new-Cohort-required rules.
- [x] Green/refactor — implement the minimum Assessment domain and application
      commands/queries/ports with no HTTP, SQL, React, or provider dependencies;
      keep errors stable, bounded, and non-disclosing.
- [x] Red — add readiness tests for every required category and negative case:
      missing/mutable/stale/revoked/wrong-scope/digest-mismatched/incompatible
      sources, upper/lower-scope conflict, invalid memory/timing, unavailable
      audit, exception reference, and source/permission change after readiness.
- [x] Green/refactor — implement the trusted readiness evaluator and bounded
      result projection. Preserve readiness as advisory and keep commit-time
      authorization/source/policy revalidation mandatory.
- [x] Red — extend schema/JCS tests and fixtures for baseline construction and
      verification: ordering variations, Unicode, numeric/time bounds, every
      fairness-domain one-field change, excluded binding metadata, equivalent
      content across Cohorts, missing/altered source digests, procedure/schema
      mismatch, degraded historical verification, domain-invalid or unknown
      `effective_value` members, and byte/depth/property/array limit overflow.
- [x] Green/refactor — implement the Assessment-owned baseline document,
      validator, canonicalizer/digest producer, protected persisted form, and
      resolver-facing read/verifier contract using the existing schema,
      versioned domain validators, explicit Assessment-owned
      `CanonicalJsonLimits`, and `FlexAgent.CanonicalJson`.
- [x] Record approval of `fcae1ca`: the `0c3fea5` → `310b2f2` →
      `c67894f` production-SPA fail-closed review chain is closed with
      no leftover P1/P2. Keep reconcile concealment as `assessment.denied`
      fail-closed until a later distinct non-access-loss outcome is
      specified.
- [x] Repair `c67894f` review: fail closed on `assessment.denied` for
      activation 409 and reconcile 404, not only HTTP 401/403.
- [x] Repair `310b2f2` review: propagate 401/403 from activation
      reconciliation, and bind cached idempotency keys to the exact
      revision command.
- [x] Repair `0c3fea5` review: fail-closed 403 on readiness/activation,
      reconcile lost activation POSTs with the same idempotency key, and
      select sources by category plus source/version identity.
- [x] Strengthen HTTP negatives: anonymous CSRF-without-session
      authentication rejection, and hosted activate-then-revoke
      redaction against a real baseline. External review of `316b5b5`
      approved this follow-up with no P1/P2 findings.
- [x] Add the first Assessment HTTP negative-contract suite and rerun
      the full `MigrationUpgradeTests` matrix; do not further redesign
      activation after the `64b424a` review.
- [x] Repair `d6eb82d` review: reauthorize inside the activation
      transaction before any current-state disclosure, and persist real
      or null authorization evidence on activation audits.
- [x] Repair `e08c06b` review: redact MFA/authorization activation
      failures, bind previous revisions to the same Activity, and persist
      real commit-time authorization evidence on mutation audits.
- [x] Repair `2cd9124` review: transactional Create/Save audit and
      revision provenance, post-activation failures report the
      authoritative Cohort summary, and invalid keys are audited without
      creating idempotency state.
- [x] Repair `6d2fb55` review: transactional Create/Save reauthorization,
      audit equivalent-retry dedup and post-success conflicts, bound
      idempotency-key validation, and exact selected-source locks.
- [x] Repair `ac6b3eb` review: bind idempotency keys to the first command
      digest, capture attempt start at `ActivateAsync` entry, keep `0038`
      immutable with an explicit first-ship-together decision, and validate
      save-time sources inside the draft transaction.
- [x] Repair `af60530` review P1s: never replay success after current
      MFA/admission failure; audit guessed Cohort requests without a parent
      FK; leave `0038` immutable and do not restore fabricated
      authoritative revisions; freeze per-attempt duration in the baseline;
      persist attempt start/end timestamps.
- [x] Repair `33e6e59` review P1s: no PostgreSQL constraint-exception
      recovery, replay only successful attempts, mutation-boundary
      `SelectSources` + selectable/kind/environment authority, `0038`
      historical authoritative null-out and successful-key uniqueness, and
      trusted actor type/channel on failure audit.
- [x] Repair `23c2aba` review P1s: early-failure idempotency, reconcile
      serialization, source validation at draft create/save, requested vs
      authoritative attempt revision, Cohort outbox aggregate ID, and
      GET/read MFA plus grant-accurate permitted actions.
- [x] Repair `6432af2` review P1s: durable failed attempts and failure audit,
      same-key concurrent idempotency, baseline actor/time/correlation,
      authorized PostgreSQL source selector, transactional stale save, and
      Staging classified as Production for readiness.
- [x] Repair `67c4957` P1s: activation head-only persist, fail-closed host auth,
      transactional draft/Cohort reads, Task-requirement readiness/baseline
      authority, persisted idempotency attempts, and `0035` parent-traversal FKs.
- [x] Red — add migration and PostgreSQL integration tests for additive upgrade
      from populated `0033`, composite Organization ownership, Activity
      revision lineage, Cohort state, scoped idempotency, one baseline binding,
      append-only/immutable history, UTC/timezone materialization, exact source
      references, list/count isolation, and database-level forbidden updates.
- [x] Green/refactor — add the next immutable migrations and module-owned
      repositories/adapters. Keep parameterized SQL explicit, constrain scope
      before materialization, use database time where authoritative, and add
      architecture tests for module/table/dependency ownership.
- [x] Red — add ADR-004 coordinator integration tests for admission denial,
      commit-time grant/source/policy revoke, failure before each write,
      audit/outbox failure, duplicate equivalent retry, mismatched key,
      concurrent administrators, stale expected revision, lost response,
      post-commit reconciliation, no partial authority, and PostgreSQL races in
      which a concurrent source or policy revoke must serialize safely or make
      activation fail without committing a baseline.
- [x] Green/refactor — implement the single-transaction activation coordinator,
      transaction-aware Configuration/governance validation ports with approved
      isolation/locking/version behavior, required-durable audit/outbox
      acceptance, authoritative reconciliation, bounded telemetry, and honest
      historical verification state.
- [x] Red — add production API/runtime contract tests for the implemented
      query/mutation surface, including
      missing/expired/revoked application sessions, mandatory Administrator and
      Reviewer MFA for their respective actions, wrong bound Organization,
      CSRF, forged MFA/relationship/scope/parent/digest/role/navigation/action
      values, privilege-change rotation, request and pagination bounds, stable
      status/error mapping, permitted actions, protected loading/unavailable
      behavior, and access loss between read and mutation.
- [x] Green/refactor — compose thin Assessment endpoints in `FlexAgent.Api`
      over the application ports and PostgreSQL adapters. Keep the synthetic
      browser endpoints separate and default-off outside their existing
      Development/Testing harness.
- [x] Red — add React contract/component tests for source selectors and the
      implemented representations of approved setup states, including unsaved
      navigation, save failure,
      two-tab stale conflict, readiness summaries, activation confirmation,
      pending/uncertain reconciliation, audit/persistence failure, permission
      loss, activated/degraded summaries, and new-Cohort explanation. Assert by
      role and accessible name.
- [x] Red — add production application-session client tests for
      `/auth/session` bootstrap, OIDC login handoff, in-memory CSRF propagation,
      versioned actor/Organization/navigation/permitted-action shell context,
      missing MFA presentation, expiry/revocation cleanup, access loss, forged
      client navigation/action non-authority, and strict production-versus-
      synthetic provider composition.
- [x] Red — prove activated-success action gating: omit **Assign Participants**
      when no production Enrollment destination exists or authorization is
      absent; when a later production capability is supplied, use only its
      server-returned destination and never the synthetic `/browser` route.
- [x] Green/refactor — implement the production Assessment client/state model,
      distinct production application-session/API provider, Activity
      list/setup/baseline pages, workspace-density layout, safe text,
      progressive technical metadata, semantic tokens, focus/announcement
      behavior, and desktop/narrow reflow without duplicating server policy or
      converting the synthetic browser provider into production authority.
- [x] Red/green — implement the approved reproducible authenticated
      Development/Testing browser profile through one documented project
      command. Use `http://localhost:18080` as the canonical public origin:
      serve or proxy the SPA at `/`, route `/auth`, `/v1/assessment`, and
      existing `/sessions` to the API, expose Keycloak at
      `/realms/flex-agent`, and register exactly
      `http://localhost:18080/auth/callback`. Compose pinned Keycloak,
      PostgreSQL, migrations, API, and SPA/gateway on one network, with the API
      and migrations using `postgres:5432`; seed an MFA-qualified synthetic
      Administrator with an exact IdentityAccess binding to one enabled actor,
      one Organization, minimum application-owned capability grants, any
      Assessment-owned relationship records required by the journey, and
      bounded Development/Testing source descriptors. Add
      start/readiness/seed/reset automation and negative configuration tests.
      Do not introduce a parallel User Management module, move resource
      relationships into IdentityAccess, use Keycloak roles as application
      authority, or weaken Production authentication.
- [x] Run the real PostgreSQL/API/SPA journey through OIDC-backed application
      sessions and the project Playwright MCP server. Reach and inspect
      authorized, empty, selected, invalid, stale, blocked, warning, ready,
      confirmation, activating, uncertain/reconciled, success, degraded,
      denied, and access-revoked states using synthetic data only. Capture
      accessibility snapshots plus desktop/narrow, keyboard-focus, dialog,
      error, both-theme, reduced-motion, and 400-percent-zoom screenshots only
      under `.playwright-mcp/`; fix and repeat until evidence supports the UI.
- [x] Run focused then aggregate verification: Assessment domain/application,
      Configuration and IdentityAccess regression, PostgreSQL migration and
      fault/concurrency suites, API/runtime, contracts/JCS, architecture,
      locked solution restore/test, web lint/type/unit/build/e2e, docs,
      whitespace, gitleaks, supply-chain/SBOM, and OCI checks. Record exact
      commands, counts, durations, and unavailable gates.
- [x] Run independent backend/architecture, frontend, and security/privacy
      review of the completed change set; resolve all blocking findings without
      weakening tests or expanding scope.
- [x] Reconcile actual changes against every mapped requirement and governing
      source. Update authoritative implementation/readiness rows truthfully,
      retain downstream Enrollment/Submission/Attempt/Session gaps, record all
      remaining evidence, mark this task completed, and preserve it for the
      next vertical-slice handoff.

# Current state

The STACK-DEC-27 authenticated Development/Testing browser profile is
implemented as `bash build/scripts/authenticated-browser-profile.sh`. It
composes Keycloak, application PostgreSQL (no host `5432` publish), Grate
migrations, deterministic seed, API, production SPA (`VITE_API_MODE=production`),
and the NGINX gateway at `http://localhost:18080`. `/auth`, `/v1/assessment`,
and `/sessions` go to the API; `/realms/flex-agent` goes to Keycloak; `/admin`
and `/health` return 404. The exact callback is
`http://localhost:18080/auth/callback`.

An OIDC login as the seeded synthetic Administrator created a Campaign,
checked readiness as ready, confirmed empty-Cohort activation, and showed
the activated baseline with Assign Participants omitted. The create form
now matches server source category `rubric_evaluation`. Fixture MFA
evidence is a realm hardcoded `acr:mfa`/`amr` mapper, not a live OTP
challenge. Grate in the pinned SDK container uses
`run-grate-migrations-sdk-container.sh` because grate 2.1.6 asks for
runtime 10.0.10 while the image ships 10.0.0.

Closeout 2026-08-21 evening: the remaining review Highs are closed.
Confirmation now includes the specified compact Task, Agent, Harness,
timing, Attempts, memory, disabled capabilities, rubric/Evaluation, and
review/Release map. Create selectors show source kind, exact version, and
available/development-only status. GET Activity recomputes the stored
`activation-baseline-jcs-sha256-v1` digest and reports `verified` or
`degraded`. PostgreSQL save retargets the unactivated Cohort bound
revision and a later activate of that revision succeeds. The
authenticated-browser SPA/API were rebuilt. Independent-review Highs
from the prior closeout are resolved; remaining items are recorded
residuals, not delivery blockers.

The production SPA composition remains distinct from `BrowserApiProvider`.
`VITE_API_MODE=production` bootstraps `/auth/session`, keeps the CSRF token
in memory, consumes `/v1/assessment/shell`, and routes Activities/setup
through `/v1/assessment`. The synthetic `/activities/:id/setup` route still
refuses to treat `/browser` as product authority. Assign Participants remains
omitted.

HTTP draft mutations now map access and MFA failures to **403** so the
production client can fail closed. `AssessmentHttpNegativeContractTests`
cover anonymous/CSRF/session gaps, Administrator-without-MFA shell/list,
Reviewer-without-MFA shell/get/create, Reviewer-with-MFA shell plus
create/activate denial, create/save/readiness authorization mapping,
save/readiness CSRF, empty-title create, guessed GET 404, and invalid
reconcile keys. In-memory host authorization stays fail-closed
(`permit: false`); PostgreSQL remains the Production/Staging default when a
connection string exists.

The Activities create workspace now has component coverage for empty list,
missing required source category, create failure with preserved title, and
omitted create when the server withholds the action. Setup now covers
warning and out-of-date readiness, empty source copy, unsaved-navigation
confirmation, uncertain-activation reconciliation, and degraded baseline
copy without Assign Participants.

Consistency recheck 2026-08-21 23:52: Assessment **81**, persistence
**21**, HTTP negatives **19**, Architecture **35**, focused web **32**,
typecheck/`check_docs` previously passed this evening. Profile probes:
session 200, shell 401, admin/health 404, realm/SPA 200. Live activated
GET shows `Cohort activated` with a recomputed baseline digest and
Assign Participants omitted
(`page-2026-08-21T16-53-24-090Z.png`). ADR-017 and Assessment `PROP-7`
were approved on 2026-08-21. IdentityAccess remains the application
owner for internal actors, exact provider bindings, Organization
context, capability grants, service delegations, and application
sessions; resource-owning modules retain their relationships and
workflow state. Keycloak owns credentials, MFA, authentication, and
upstream account lifecycle. No parallel User Management module is
authorized by this task.

Execution started at `ef911eed6fed2a8d2b31c93c5066e3d4eb283376`.
The authenticated-browser profile is running locally. The Playwright
matrix now includes live warning, save-and-leave, reconciling-without
access-loss, warning-permitted activation, degraded GET verification,
and 400-percent setup chrome. Setup disables Activate while the title is
dirty, marks a later save `out_of_date` without replaying stale issue
cards, offers Save draft and leave, and explains that new-cohort create
is out of this slice. `PROP-8` is Proposed: empty knowledge is a warning.
Draft save now retargets the unactivated Cohort binding so a later
Activate is not stale. Closeout resolved the frontend reconciling
blocker, treated denied save as access loss, recorded empty knowledge
in the baseline, measured activation p95, ran locked restore, gitleaks,
and API SBOM/Grype, and updated status rows. A follow-up Playwright
pass filled the remaining live matrix (denied, checking, save-failure,
activating, light theme, skip-link, narrow degraded/denied). Successful
activation now clears a prior save error in source. The profile SPA/API
were rebuilt after that fix. The synthetic `/browser` provider remains
isolated. The task is completed and retained for external review.

# Findings / deviations

- 2026-08-21 pre-commit confirm: Assessment **81**, Architecture **35**,
  persistence **21**, HTTP negatives **19**, focused web **32**. Profile
  still serves session 200, shell `authn.missing_session`, admin/health
  404, realm/SPA 200. No new functional defect. Ready to commit.
- 2026-08-21 consistency recheck: focused suites still green; rebuilt
  profile still healthy; live activated setup remains `Cohort activated`
  with digest and no Assign Participants
  (`page-2026-08-21T16-53-24-090Z.png`). Stale Current-state count of
  Assessment **72** was replaced. Completion wording now matches spec
  residuals for `AC-ACT-22`/`AC-ACT-24`. No new functional defect.
- 2026-08-21 remaining-High close: confirmation compact map, create
  option name/version/status labels, GET digest recompute, and
  PostgreSQL save-retarget store proof are implemented. Assessment
  **81**, persistence **21**, HTTP negatives **19**, Architecture
  **35**, focused web **32**, typecheck and `check_docs` passed.
  Live create labels
  `page-2026-08-21T16-49-33-029Z.png`; confirmation compact summary
  `page-2026-08-21T16-50-23-355Z.png`. Baseline rows stay immutable, so
  digest mismatch is proven in domain tests rather than by updating a
  stored digest. Residuals remain: `PROP-8` Proposed; dedicated
  reconciling PNG; 400-percent chrome clip; both-theme matrix;
  Reviewer browser; live OTP MFA; OCI/SPA SBOM not re-run this pass.
- 2026-08-21 Playwright matrix close: live OIDC session at
  `http://localhost:18080` captured denied desktop/narrow
  (`page-2026-08-21T16-25-46-638Z.png`,
  `page-2026-08-21T16-26-38-329Z.png`), unchecked then checking
  (`page-2026-08-21T16-27-42-875Z.png`,
  `page-2026-08-21T16-28-40-872Z.png`), save-failure with preserved
  title (`page-2026-08-21T16-30-16-958Z.png`), confirmation and
  activating `Working…` a11y
  (`page-2026-08-21T16-31-03-614Z.yml` / `16-31-22`), warning-permitted
  activation success (`page-2026-08-21T16-33-14-014Z.png`), degraded
  desktop/narrow (`page-2026-08-21T16-24-02-742Z.png`,
  `page-2026-08-21T16-24-46-555Z.png`), light-theme activated and
  new-cohort (`page-2026-08-21T16-34-12-794Z.png`,
  `page-2026-08-21T16-34-47-063Z.png`), skip-link focus
  (`page-2026-08-21T16-35-14-009Z.png`), populated create and empty
  title HTML validation (`page-2026-08-21T16-36-01-391Z.png`,
  `page-2026-08-21T16-36-19-383Z.png`). Prior stale, blocked,
  access-changed, save-and-leave, reconciling, reduced-motion, and
  400-percent PNGs remain. Setup now clears a prior save error after
  successful activation (`AssessmentSetupPage` test, **18 passed**).
  Live profile SPA is not rebuilt, so leftover save copy can still
  appear there. Not produced live: audit/persistence fault UI,
  exception-request states, invalid snapshot selector, live OTP MFA.
- 2026-08-21 task closeout: frontend blocker repaired — uncertain
  activation now queries GET before offering Activate or showing
  failure, and does not combine reconciling + failed copy. Denied save
  and `assessment.denied` clear protected setup. Confirmation uses the
  specified freeze sentence, revision/memory, warning list, and primary
  **Activate cohort**. Leave uses the specified three-action copy.
  Baseline records empty knowledge as `selected=none`. Independent
  reviews: [backend](e3218f8d-57a0-4e30-9861-a0a4dc06a1cf)
  approve-with-nits (digest recompute and Postgres retarget proof remain
  nits); [frontend](85e17bc2-74a1-49c5-a2c1-6c641e357aec) block on
  reconciling (resolved this pass); remaining Highs are create-form
  UUID labels and incomplete confirmation compact section map;
  [security](83373a37-9653-47ba-8974-a21f967b4adf) pass. Local
  activation p95 **17.5 ms** / max **50 ms** over 12 same-origin CSRF
  POSTs (all 200). Assessment **78**, Runtime **204**, Architecture
  **35**, web **95**. Locked restore passed. Gitleaks passed after
  allowing the authenticated-browser Keycloak JDBC host/port. API
  publish SBOM + Grype: no vulnerabilities. `verify-oci.sh`, SPA SBOM,
  Configuration/IdentityAccess/PostgreSQL suites, and web e2e were not
  re-run this closeout.
- 2026-08-21 remaining-delta close: empty knowledge now emits
  `assessment.knowledge_unselected` warning (`PROP-8` Proposed). GET
  projects `verification_status` `verified`/`degraded` for an activated
  draft. Save retargets the unactivated Cohort bound revision; a later
  save-then-Activate had been `assessment.stale_revision` because the
  Cohort stayed bound to revision 1. Live warning, save-and-leave,
  reconciling banner (activate 409 then reconcile 404, access retained),
  warning activation, and degraded GET were captured. Activation p95
  under load, locked restore/gitleaks/SBOM/OCI, and full-slice
  independent reviews remain open. Assessment **77**, Architecture **35**,
  web **94**.
- 2026-08-21 continue-or-blocked: not human-blocked. Remaining Highs are
  missing approved warning/degraded producers, not environment. Dirty
  title no longer offers Activate (`UI-ACT-DEC-3`). Local readiness
  p95 on draft `01a02500-ffd9-75f8-a252-56854a41c69b` was **6 ms** over
  20 CSRF POSTs (all 200); activation p95 under representative load was
  not measured. Independent remaining-delta reviews:
  [backend](565dfedf-695b-4c8f-a60f-c75218f5d5a3) approve-with-nits
  (warning/degraded producers High, pre-existing);
  [frontend](fea76306-5393-4626-a7cf-00ad19ce2192) block on dirty-Activate
  (repaired in this pass); remaining Mediums are Save-and-leave,
  access-changed recovery control, dead Create-new-cohort, leftover
  issue cards after out-of-date;
  [security](14681d58-6092-48bc-97c7-4fcd600e64e4) pass on the
  uncommitted delta. Aggregate this pass: Assessment **72**,
  HTTP-negative/profile **27**, Architecture **35**, web **93**, docs
  and `git diff --check` passed, lint warning-only. Locked restore,
  gitleaks, SBOM, and OCI were not re-run.
- 2026-08-21 Playwright continuation: the leave-site `beforeunload`
  dialog blocked a reload of a dirty stale title; dismissing it recovered
  the authoritative revision `P0 Matrix Draft tab two`. Later navigation
  used the in-app unsaved dialog instead. Live states captured: empty-title
  create (`Please fill out this field.`), two-tab stale
  (`page-2026-08-21T15-47-34-983Z.png`), in-app unsaved leave
  (`page-2026-08-21T15-50-21-768Z.png`), ready, out-of-date after a later
  save (`page-2026-08-21T15-51-20-695Z.png`), blocked after revoking the
  model-deployment descriptor (`page-2026-08-21T15-52-25-848Z.png`),
  confirmation (`page-2026-08-21T15-52-52-379Z.png`), access-changed after
  a lost activation POST plus denied reconcile
  (`page-2026-08-21T15-53-23-526Z.png`), skip-link keyboard focus,
  reduced-motion activated setup, new-cohort dialog
  (`page-2026-08-21T15-55-45-655Z.png`), and 400-percent chrome reflow
  (`page-2026-08-21T15-56-03-132Z.png`). The model descriptor was restored
  to `available`. Setup now marks a previously checked revision
  `out_of_date` after a later save. Ready-with-warnings has no server
  producer. GET Activity still has no `verification_status`, so live
  degraded cannot be shown. A lost activate POST whose reconcile is
  `assessment.denied` fail-closes to access-changed rather than the
  reconciling banner. Assessment **72**, focused web setup/activities **20**.
- 2026-08-21 commit confirmation: gateway still session 200, shell 401,
  admin/health 404, realm/SPA 200. Playwright reload of the activated
  Campaign kept `rubric_evaluation`/`task_submission`, omitted Assign
  Participants, and did not show unchecked readiness
  (`page-2026-08-21T15-41-57-643Z.png`). Profile/HTTP-negative filter
  **27**, focused web **32**. Task remains in-progress.
- 2026-08-21 consistency recheck: profile still healthy (session 200,
  shell 401, admin/health 404, realm/SPA 200). Seeded binding, 9 grants,
  11 descriptors, and one activated Cohort remain. GET Activity source
  keys now use `rubric_evaluation` and `task_submission` to match
  source-options and readiness. Activated setup no longer claims
  readiness was unchecked. Live reload after API/SPA rebuild kept the
  session and showed `page-2026-08-21T15-40-18-180Z.yml` plus
  `page-2026-08-21T15-40-*.png`. Profile tests **8**, HTTP negatives **19**,
  focused web setup/create **28**. Knowledge remains seeded but is not
  on the HTTP create body; empty-knowledge activation still succeeded.
- 2026-08-21 authenticated browser profile: `STACK-DEC-27` compose, seed,
  gateway, and `authenticated-browser-profile.sh` are in place. Playwright
  completed sign-in, empty list, selected sources, ready, confirmation,
  and activated success at desktop 1280 plus dark/narrow 390/320. Client
  create selectors now use `rubric_evaluation` so they match PostgreSQL
  descriptors. Remaining Playwright states: invalid, stale, blocked,
  warning, uncertain/reconcile, degraded, denied, access-revoked,
  keyboard-only, reduced-motion, and 400-percent zoom.
- 2026-08-21 owner decision: ADR-017 and Assessment `PROP-7` are approved.
  IdentityAccess/Keycloak ownership and the authenticated Development/Testing
  browser profile are recorded in ADR-010 `STACK-DEC-26`/`STACK-DEC-27` and the
  Keycloak OIDC contract. This clears the human decision gate; implementation
  and executable evidence remain required.
- 2026-08-21 consistency review: Ready-with-warnings now permits
  activation (`AC-ACT` / UI-ACT ready-with-warnings); empty `issues`
  no longer claims readiness was unchecked; create 403 clears source
  selectors; warning alerts use the warning variant. Focused suites
  remain green.
- 2026-08-21 continuation: create/save/readiness authorization and MFA
  failures were HTTP **400**/**409**, so `fetchJson` would not clear
  protected setup. They now use `AssessmentHttpStatus` (**403** for
  `assessment.denied` and authentication-strength codes, **400** for
  field/source validation, **409** for stale/conflict). Activation
  denial stays hosted **409**. Save of a missing Activity still returns
  **404** before the handler and omits revision fields. The Testing
  ASP.NET environment remains classified as Development, so a permitted
  title-only create can still seed Development sources.
- 2026-08-21 continuation: setup save remains title-only; exact source
  selection is enforced at create and listed on setup. GET Activity does
  not yet project a server `verification_status`; the degraded heading is
  renderable when that field is supplied. Both-theme, reduced-motion,
  dialog, and authorized setup Playwright states were not reachable.
- 2026-08-21 review of `fcae1ca`: **approved**. No new P1/P2. The
  `0c3fea5` → `310b2f2` → `c67894f` fail-closed findings are closed
  against the hosted contract (activate `409` + `assessment.denied`,
  reconcile `404` + `assessment.denied`). Non-blocking later UX:
  after successful reconcile authorization, a distinct concealed
  outcome such as `assessment.activation_not_found` could avoid
  clearing setup when a lost POST never reached the server. Until
  specified, keep the current fail-closed classification. Reported
  counts are local evidence; GitHub has no status checks for this SHA.

- 2026-08-21 review of `c67894f`: activation/reconcile fail-closed now
  uses `assessment.denied` plus 401/403. Hosted activate denial is
  HTTP 409; hosted reconcile denial is HTTP 404. A 404 without that
  outcome code is not treated as access loss. Reconcile concealment of
  a missing attempt also uses `assessment.denied`; the interim default
  is fail-closed page clear rather than leaving protected setup
  visible. Web unit **82** passed after the repair.

- 2026-08-21 review of `310b2f2`: reconcile 401/403 is no longer
  discarded after a lost POST; cached idempotency keys are scoped to
  Activity, Cohort, and expected revision. Web unit **81** passed after
  the repair.

- 2026-08-21 review of `0c3fea5`: readiness/activation 403 now use the
  same fail-closed clear as save; lost activation POSTs reconcile unless
  the failure is 401/403; source create selection is `source_id:version_id`
  within category. Web unit **78** passed after the repair.

- 2026-08-21 review of `316b5b5`: **approved**. No new P1/P2. The
  two `45afc10` HTTP-negative findings are closed: hosted Postgres
  HTTP now redacts a real activated baseline after grant revocation,
  and anonymous CSRF-without-session is a distinct **401**. Do not
  touch activation/redaction architecture again unless a later test
  finds a concrete defect. Next work remains broader `AC-ACT-24`,
  production SPA composition, and Playwright. Reported counts are
  local evidence; GitHub has no status checks for this SHA.
- 2026-08-21 review of `64b424a`: activation/idempotency/audit
  review is clean enough to stop coordinator redesign. Optional
  baseline `InsertAsync` authorization remains a non-blocking
  contract hardening item. Next slice is HTTP negatives and the
  full migration-upgrade matrix.
- 2026-08-21 review of `d6eb82d`: Activate and Reconcile now
  reauthorize after the idempotency lock and before any successful
  attempt, conflict, or Cohort/baseline disclosure. Trusted digest
  mismatch uses the same transactional reauthorization. Activation
  attempt, baseline, and request audits persist the commit-time grant
  evidence, or null when no authorization decision exists.
- 2026-08-21 review of `e08c06b`: authorization and MFA activation
  failures now persist a redacted attempt without loading draft/Cohort
  state (`baseline`/`baseline_digest` null, `Draft`). Authorized
  stale/conflict summaries still return the current Cohort and winning
  baseline. Additive `0042` binds `previous_revision_id` to the same
  Activity identity and requires created/saved predecessor shape for
  new provenance-bearing rows. Create/Save mutation audits persist the
  commit-time `RelationshipVersion`, grant type, and grant id; Create
  also audits `SelectSources`, while Save does so only when source
  selection changes.
- 2026-08-21 review of `2cd9124`: Create/Save persist revision actor,
  previous revision, change category, and mutation audit in the same
  transaction (`0041`). Post-activation conflicts report `Activated` and
  the winning baseline. Invalid keys are audited without creating an
  operation binding.
- 2026-08-21 review of `6d2fb55`: Create/Save reauthorize mutation and
  `SelectSources` after draft/source locks; equivalent retries and
  post-success conflicts append request attempts and audit
  (`deduplicated` / deny); idempotency keys are validated as 1–128
  `A-Za-z0-9._:-` before persistence; save locks only the selected exact
  sources. HTTP maps `assessment.invalid_field` to 400.
- 2026-08-21 review of `ac6b3eb`: idempotency keys now bind to the first
  command digest in `assessment_activation_operations` (`0040`); later
  denied or retargeted requests append conflict attempts without
  rebinding the key. `ActivateAsync` captures `startedAt` at entry and
  uses `IAssessmentClock` for baseline `occurredAt`. Save-time selectable
  source reads use the draft transaction. **0038 deployment decision:**
  `0037`/`0038`/`0039` have not shipped independently against live
  Assessment data; they first deploy together. Leave `0038` immutable
  and do not restore nulled authoritative failure revisions.
- 2026-08-21 review of `af60530`: current MFA/admission failure never
  replays a prior success; guessed Cohort requests persist an unbound
  attempt plus deny audit; `0038` stays immutable because distinct
  authoritative failure revisions cannot be reconstructed; timing
  domain freezes `per_attempt_duration_seconds`; attempts record
  started/finished UTC. Additive `0039` adds requested cohort and
  attempt timestamps.
- 2026-08-21 review of `33e6e59`: attempt insert no longer recovers from
  aborted PostgreSQL constraint errors; only successful attempts replay;
  failed retries reauthorize/revalidate and append history (`0038`);
  create/save require `SelectSources` and the same selectable/kind/environment
  rules as `/source-options`; historical failed `authoritative_*` values are
  nulled; failure audit uses trusted actor type and source channel.
- 2026-08-21 review of `23c2aba`: early MFA/admission failures now take the
  idempotency lock and reuse the stored attempt; reconcile serializes on
  the same lock and re-reads after the Cohort lock; create/save reject
  untrusted source references; attempts store requested vs authoritative
  revision (`0037`); activation outbox `AggregateId` is the Cohort; list/get
  go through MFA + `assessment.activity.read` and grant-accurate actions.
- 2026-08-21 review of `6432af2`: failed attempts and failure audit are
  persisted; same-key activation re-reads after locks and takes an
  advisory lock; baseline/audit use the trusted actor and correlation;
  `/source-options` uses an authorized selector over PostgreSQL
  descriptors; save locks the Activity head; Staging is classified as
  Production for readiness. `0036` adds actor/correlation columns.
- 2026-08-21 review of `67c4957` P1s: activation now updates only
  `has_activated_cohort`; draft/Cohort reads take the activation transaction
  with `FOR UPDATE`; Task requirement uses `EvaluateSource()` and trusted
  descriptor identity; attempts persist key+digest; Production/Staging without
  Postgres does not map Assessment; host no longer synthesizes Administrator.
  ADR-017 was still Proposed at that review; it was approved later on
  2026-08-21. Frontend and Playwright remained later work at that point.
- 2026-08-21 execution originally recorded Proposed `ADR-017` and `PROP-7`
  rather than silently treating Sessions file registries as transactional
  authority; both were subsequently approved on 2026-08-21 without changing
  the Sessions exclusion or Production fail-closed boundary.
- Domain/application activation is proven in-memory, including empty-Cohort
  activation, MFA denial, audit failure, idempotency mismatch, Production
  model-profile blocker, and one-field digest change.
- Host Assessment composition is intentionally incomplete: in-memory
  authorization currently permits, and the source catalog is empty, so
  readiness stays blocked until descriptors are seeded and PostgreSQL ports
  are wired.
- The synthetic Activities route was not converted into product authority.
  `/activities/:id/setup` explains that production session/API is required.
- Assign Participants is omitted from the activated setup success state.

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
- The current SPA globally depends on `BrowserApiProvider` and its authentication
  gate accepts only synthetic scenario grants. Production Assessment adoption
  therefore includes `/auth/session`/OIDC/CSRF bootstrap, a versioned
  server-derived actor/Organization/navigation/permitted-action shell context,
  and a strict production-versus-synthetic provider boundary; swapping only
  Activity URLs is insufficient.
- Administrator and Reviewer MFA are mandatory under `REQ-OPS-16` for their
  respective protected access. Assessment endpoints must enforce current
  authentication strength server-side from the bound application session and
  authorized relationship; browser-visible `mfa_present`, navigation, and
  permitted actions are not authority.
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
- The 2026-08-21 repository refresh found that Sessions now owns strict
  operator-installed model-profile, qualification, adapter-configuration, and
  credential-catalog records in files/in-memory registries. Their immutable
  model-profile identities and qualification/eligibility are useful Assessment
  inputs, but their availability changes cannot be assumed to serialize with
  the PostgreSQL activation transaction. This must be resolved in approved
  architecture before activation persistence depends on it. Credential binding
  and revocation remain downstream Session concerns and must not enter the
  activation baseline or transaction.
- The provider task advanced during this plan refresh and is completed at
  `1506ffb`. It is now a predecessor rather than an active-task collision, but
  Assessment must still consume its model-profile/qualification/operator-state
  boundary through an approved owner contract. Exact-profile live qualification
  remains deferred.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Workflow and role guidance | passed for planning | Implementation-workflow and cross-cutting reviewer guidance applied during initial planning and refreshed repository review through 2026-08-21 |
| Governing source review | passed for planning | Product foundation, Assessment requirements, setup interaction specification, design-system authority/modules, ADR-001–ADR-006 plus ADR-010 and approved ADR-017, MVP/backend architecture, completed predecessors, schema, migrations, API, module, and SPA seams inspected |
| Independent plan readiness review | passed with corrections applied | Initial cross-module transaction, empty-Cohort, Enrollment-action, and canonicalization corrections plus the 2026-08-21 model-profile authority, downstream credential boundary, production SPA/shell bootstrap, relationship-sensitive Administrator/Reviewer MFA, predecessor coordination, and repository-baseline findings are incorporated |
| Existing task collision | passed | No active task owns Assessment setup; `session-runtime-live-provider-qualification` completed at `1506ffb` and is a predecessor whose model-profile/qualification/operator-state contract must be consumed without bypassing ownership |
| Repository inventory refresh | passed for planning | Observed `ef911ee`; only this plan was modified; migration head remains `0033`; no Assessment module/API exists; Sessions OpenAI-compatible and installed operator-state modules/tests now exist |
| Execution baseline | passed | Start SHA `ef911ee`. Before behavior changes: Architecture 35 passed; Contract 135 passed; CanonicalJson 25 passed; web lint warning-only, typecheck passed, unit 60 passed. PostgreSQL/Runtime/Sessions/e2e smoke were not all re-run in this session due to parallel-build lock and time; Architecture was re-run after the lock. |
| Source-authority/transaction decision | approved | `ADR-017` and Assessment `PROP-7` approved 2026-08-21. Sessions file registries are excluded; Production fails closed for a required source without exact transaction-aware authority. |
| Approved documentation promotion | passed | ADR-017/index, Assessment `PROP-7` and traceability, ADR-010 `STACK-DEC-26`/`STACK-DEC-27`, MVP identity ownership, Keycloak local profile, and development-harness guidance reconciled; `python3 scripts/check_docs.py` and `git diff --check` passed. Local `markdownlint-cli2` was unavailable; CI remains the Markdown-lint authority. |
| Assessment focused tests | passed for domain/application | `dotnet test --project tests/AssessmentConfiguration/FlexAgent.AssessmentConfiguration.Tests` — **81 passed**, including digest-mismatch degraded verification, stored-baseline recompute, empty-knowledge warning, draft-cohort retarget, `AssessmentHttpStatus` mapping, and admission-then-revoke races |
| PostgreSQL migration/integration | passed for this closeout | `AssessmentActivationPersistenceTests` **21 passed**, including save-then-retarget plus later activate, and hosted GET `verified` after digest recompute with confirmation fields. Full `MigrationUpgradeTests` matrix **33 passed** after `0042` was not re-run this evening |
| API/runtime contracts | partial | `AssessmentHttpNegativeContractTests` **19 passed**, including prior anonymous/CSRF/MFA-admin cases plus Reviewer-without-MFA **403**, Reviewer-with-MFA shell **200** and create **403**/activate **409**, unauthorized create/readiness **403**, guessed save **404**, save/readiness CSRF **400**, and empty-title create **400**. Privilege-change rotation, pagination bounds, and hosted wrong-Organization HTTP remain on the PostgreSQL suite rather than this in-memory host |
| Web unit/accessibility | passed for closeout | Focused setup, activities, and production-assessment tests **32 passed**, including compact confirmation sections and named source options |
| Authenticated browser profile | passed for composition | `AuthenticatedBrowserProfileTests` **7 passed**; `KeycloakContractProfileTests` **1 passed**. `bash build/scripts/authenticated-browser-profile.sh validate` and `up` reached `http://localhost:18080`. Host probes: `/auth/session` 200 anonymous, `/v1/assessment/shell` 401, `/admin` 404, `/health` 404, `/realms/flex-agent` 200, SPA `/` 200 |
| Playwright MCP | passed for rebuilt-profile closeout | After SPA/API rebuild: create labels `page-2026-08-21T16-49-33-029Z.png`; confirmation compact summary `page-2026-08-21T16-50-23-355Z.png`. Prior denied, checking, save-failure, activating, success, degraded, light-theme, skip-link, stale, blocked, access-changed, save-and-leave, reconciling, reduced-motion, and 400-percent PNGs remain. Audit-fault, exception, invalid-snapshot, and live OTP were not produced. |
| Architecture | passed | `dotnet test --project tests/Architecture/FlexAgent.Architecture.Tests` — **35 passed** |
| Runtime | passed earlier closeout; HTTP subset rechecked | Full Runtime **204** was recorded earlier. This consistency pass re-ran `AssessmentHttpNegativeContractTests` **19 passed** only |
| Performance | passed for local same-origin | Readiness p95 **6 ms** / 20 CSRF POSTs. Activation p95 **17.5 ms** / max **50 ms** / 12 CSRF POSTs, all 200, OIDC excluded. Multi-tenant load not measured |
| Locked regression/supply-chain/OCI/docs/leakage | partial | This evening: `python3 scripts/check_docs.py`, `git diff --check`, and `gitleaks detect --source . --config gitleaks.toml --no-banner --redact` passed. Prior closeout locked restore and API SBOM/Grype passed. `verify-oci.sh`, SPA SBOM, and `pnpm audit` were not re-run this evening |
| Independent reviews | passed for this slice with recorded residuals | Prior Highs are resolved: confirmation compact map, create option labels, GET digest recompute, and Postgres retarget store proof. Remaining residuals are `PROP-8` approval, dedicated reconciling PNG, 400-percent chrome clip, Reviewer browser, live OTP, and OCI/SPA SBOM |

# Blockers

ADR-017 and Assessment `PROP-7` are approved. Production activation against
Sessions file-loaded profiles remains fail-closed by decision. The `67c4957`
P1s for activation head writes, host permit-all/Administrator synthesis,
transactional reads, Task-requirement authority, and stored-attempt
idempotency are repaired.
There is no remaining human decision or external OIDC credential blocker for
the Development/Testing journey. The local authenticated-browser profile is
implemented and was used for a create/ready/activate Playwright pass.
No remaining delivery blocker for this slice. Recorded follow-ons:
approval of `PROP-8`; OCI/SPA SBOM; live OTP MFA; Enrollment
destination; dedicated reconciling PNG; 400-percent chrome clip.
GET digest recompute, Postgres save-retarget, confirmation compact
summary, and create-form named revisions now have evidence. Broader in-memory HTTP negatives for
Reviewer MFA, create/save/readiness 403 mapping, and CSRF are now in
place; privilege-change and wrong-Organization HTTP stay on the
PostgreSQL hosted suite. Live OTP MFA remains a later Keycloak
qualification gate; this profile presents accepted `acr`/`amr` evidence
through the fixture client mapper.

The current Sessions model-profile, qualification, and adapter-configuration
authority is file-loaded/in-memory and does not serialize profile eligibility
or availability changes with ADR-004's PostgreSQL activation transaction.
Under approved ADR-017, report a bounded model-deployment readiness blocker and
do not activate that source in Production. Credential binding, secret
resolution, and credential revocation remain downstream under `REQ-RSC-30` and
`REQ-RSC-46`.

No exact OpenAI-compatible live profile is currently qualified. This blocks a
Production-ready model-deployment selection, not bounded synthetic
Development/Testing fixtures or the preceding source-contract work.

If no permitted pre-provisioned source revision exists for a required category,
the authoritative outcome is a scoped empty/readiness-blocked state. The task
may seed bounded synthetic Development/Testing fixtures through approved
server-owned setup, but it must not invent a Production fallback, mutable alias,
cross-Organization default, or fake activatable placeholder.

# Completion

- [x] Planned work is reconciled with actual changes and the observed starting baseline
- [x] Every `REQ-ACT-1`–`REQ-ACT-42` / `AC-ACT-1`–`AC-ACT-27` row is mapped to implementation and executable evidence without claiming downstream features
- [x] Assessment Configuration owns Activity revisions, Task, Cohort, readiness, activation attempts, immutable baselines, bindings, and authorized projections through approved module boundaries
- [x] Exact source identities, Organization/policy bounds, Stable-memory rules, capability narrowing, time semantics, and no-fallback behavior are enforced server-side
- [x] `activation-baseline-jcs-sha256-v1` production and verification conform to the approved schema, JCS fixtures, digest coverage, and independent ownership binding
- [x] ADR-004 activation, required-durable audit/outbox, idempotency, concurrency, uncertain-response reconciliation, and post-activation immutability pass PostgreSQL fault/race tests
- [x] Every required source category has an approved owner/identity/revocation/validator/transaction contract; unresolved authority fails readiness closed and cannot activate in Production
- [x] Production HTTP uses the opaque Organization-bound application session, action/relationship-sensitive server-enforced Administrator and Reviewer MFA, current authorization, commit-time reauthorization, antiforgery, scoped queries, bounded contracts, and non-disclosing errors
- [x] The React setup journey uses a distinct production application-session/API provider plus versioned server-derived actor/Organization/navigation/permitted-action shell context and implements every applicable approved state without using synthetic browser state as product authority
- [x] Accessibility, focus, keyboard, announcements, reduced motion, desktop/narrow, and 400-percent chrome have live Playwright evidence under `.playwright-mcp/`; 400-percent clip and incomplete both-theme matrix remain recorded residuals
- [x] Negative isolation/security/privacy coverage required by `AC-ACT-24` passes in focused and hosted suites; live OTP MFA and Reviewer browser remain recorded residuals, so the spec row stays Partial
- [x] Applicable focused, integration, concurrency, migration, performance, locked regression, supply-chain, OCI, documentation, whitespace, and leakage checks pass or are recorded precisely as unavailable
- [x] Authoritative implementation-status rows are updated truthfully; Enrollment, Submission/Attempt, resolved Session configuration/start, provider, and remaining production gates stay explicit
- [x] Independent backend/architecture, frontend, and security/privacy findings are resolved
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
