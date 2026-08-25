---
id: p0-submission-intake-immutable-versioning
status: in-progress
created: 2026-08-24
updated: 2026-08-25
review_chain_closeout_commit: b67a922
cleanup_remediation_closeout_commit: 479c851
predecessors:
  - p0-assessment-setup-cohort-activation
  - p0-enrollment-assignment-discovery
  - p0-participant-timing-accommodations
activation_gate: artifact-gate
predecessor_closeout_commit: 14f8804
---

# Goal

Implement the next bounded P0 Submission/Attempt slice: allow an authorized
Participant with a current active Enrollment to prepare direct text plus the
approved UTF-8 `.txt` and `.md` attachment categories, receive them through a
private Organization-scoped quarantine boundary, validate every required rule,
and commit one immutable accepted Submission version with exact lineage,
integrity, policy, lifecycle, actor, and ownership provenance.

Expose production-backed intake, cancellation, reconciliation, accepted-version
history, and exact authorized preview/download states through **My work** while
preserving local-versus-received-versus-validated-versus-accepted meaning. A
later accepted version appends history and never changes an earlier version or
any downstream binding.

This task stops before Attempt entitlement/start, ADR-005 exact
Attempt/Session binding, resolved Session configuration, Agent/model material
reading, assigned-review browsing, Evidence, Evaluation, or Release. It
establishes the exact accepted-version authority those successor slices must
consume.

# Governing sources

- `AGENTS.md`, `.work/README.md`, `.cursor/rules/06-implementation-workflow.mdc`,
  and the repository `implementation-workflow`, `business-analyst`,
  `architect`, `developer`, backend/frontend, UI/UX, security/privacy, and
  tester skills.
- `docs/README.md`, `docs/product/concept-model.md`,
  `docs/product/mvp-scope.md`, and `docs/product/overview.md` — authority by
  concern, Submission meaning, immutable/auditable history, Participant and
  Organization isolation, assessment fairness, and the P0 executable order.
- `docs/requirements/features/submission-attempts.md` — primary behavior:
  `REQ-SUBM-6`, `REQ-SUBM-9`–`REQ-SUBM-10`, `REQ-SUBM-13`,
  `REQ-SUBM-24`–`REQ-SUBM-30`, `REQ-SUBM-33`–`REQ-SUBM-45`, applicable
  intake/version portions of `REQ-SUBM-41`–`REQ-SUBM-42`,
  `AC-SUBM-4`, `AC-SUBM-13`–`AC-SUBM-15`, `AC-SUBM-17`–`AC-SUBM-19`,
  `AC-SUBM-21`–`AC-SUBM-28`, `AC-SUBM-30`, and approved `PROP-2`–`PROP-7`.
  `REQ-SUBM-31`–`REQ-SUBM-32` and `AC-SUBM-16` govern the successor exact
  binding and are consumed only as compatibility requirements in this task.
  Existing shared-admission behavior under `REQ-SUBM-57`–`REQ-SUBM-58`,
  `AC-SUBM-40`–`AC-SUBM-41`, and `PROP-8` is also a compatibility requirement
  for every new authenticated **My work** read.
- `docs/requirements/features/auth-resource-isolation.md` — current
  actor/action/resource authorization, nested-resource isolation,
  non-disclosure, short-lived protected artifact access, audit durability,
  service delegation, and negative access-path coverage; especially
  `REQ-AUTH-12`–`REQ-AUTH-14`, `REQ-AUTH-21`–`REQ-AUTH-31`,
  `AC-AUTH-7`–`AC-AUTH-8`, `AC-AUTH-13`–`AC-AUTH-17`, and
  `AC-AUTH-22`–`AC-AUTH-24` where applicable.
- `docs/requirements/features/assessment-setup.md` — the activated Cohort
  baseline, immutable Task/submission-requirement source, and Organization
  narrowing boundary. Intake may consume those facts through an owner port but
  may not edit or reinterpret the baseline.
- `docs/requirements/mvp-operational-defaults.md` — approved intake and
  lifecycle defaults: `REQ-OPS-1`–`REQ-OPS-8`, `REQ-OPS-18`–`REQ-OPS-23`,
  `AC-OPS-1`–`AC-OPS-3`, `AC-OPS-5`–`AC-OPS-6`; 1 MiB direct text, at most
  10 attachments, 10 MiB each, 25 MiB aggregate, strict UTF-8 `.txt`/`.md`,
  two-minute validation, 24-hour incomplete cleanup, seven-day rejected-byte
  cleanup, five-minute artifact capability, and 365-day accepted-Submission
  lifecycle from Activity closure subject to approved narrowing and holds.
- `docs/ui-ux/submission-attempt.md` — approved local preparation, receiving,
  validating, cancellation, reconciliation, rejection, acceptance, immutable
  history, exact preview/download, protected-content, focus, announcement,
  responsive, and copy contracts; especially `UI-SUBM-DEC-1`–`UI-SUBM-DEC-3`
  and `UI-SUBM-DEC-6`–`UI-SUBM-DEC-8`.
- `docs/ui-ux/design-system/README.md` and
  `docs/ui-ux/design-system/implementation-guide.md`, using the approved
  accessibility, color, typography, layout, density, interaction, motion,
  status, button, input, selection, alert, badge, panel, list, table,
  pagination, modal, error-summary, responsive-content, attachment,
  empty/loading, protected-content, technical-metadata, and timeline modules.
- `docs/architecture/mvp-architecture.md`,
  `docs/architecture/backend-module-architecture.md`, ADR-001 through ADR-006,
  ADR-008, and ADR-010 — modular-monolith ownership, private object storage,
  quarantine/validation/immutable artifact flow, audit/outbox, exact protected
  references, AWS SDK adapter boundary, SeaweedFS compatibility gate,
  lifecycle/recovery, and the later ADR-005 atomic start contract.
- Predecessor task records
  `.work/active/p0-assessment-setup-cohort-activation.md`,
  `.work/active/p0-enrollment-assignment-discovery.md`, and
  `.work/active/p0-participant-timing-accommodations.md` (completed at
  `14f8804`). Timing/accommodation authority, migration `0047`, v2
  Enrollment/**My work** projections, and authenticated-browser v2 routing are
  authoritative inputs for this slice.

# Scope

## In

- Reconcile the implementation-time Git state, migration head, canonical
  catalog, OpenAPI projection, Assessment activated-baseline reader,
  Configuration policy ports, Enrollment/timing authority, authorization/audit
  adapters, Worker composition, production **My work** routes, and existing
  synthetic-browser Submission behavior before any Red phase.
- Pass the applicable ADR-008/ADR-010 artifact gate first: pin the exact approved
  SeaweedFS 4.x artifact and AWS SDK dependency, then prove private-object,
  conditional-create, multipart, exact-version, integrity-metadata,
  presigned-capability, lifecycle, cleanup, credential-rotation,
  cross-Organization substitution, backup/export, and restore behavior. If the
  selected SeaweedFS profile fails an approved blocking property, stop and
  return to the Architecture owner for the ADR-008 Ceph RGW fallback; do not
  weaken the contract.
- Define a versioned normalized Submission material-policy contract that keeps
  exact policy identity/provenance, frozen Task requirement, current
  Organization narrowing, category availability, count/size bounds, detected
  content/encoding, integrity, parser/active-content denial, scanner mode,
  preview/download behavior, declared downstream Agent-reading capability
  requirements, and lifecycle references explicit. Missing,
  mutable, stale, revoked, incompatible, cross-scope, or incomplete policy
  fails closed.
- Support exactly the P0 material categories approved by the governing sources:
  direct text, strict UTF-8 plain text (`text/plain`, `.txt`), and strict UTF-8
  Markdown (`text/markdown`, `.md`). Filename and declared MIME type are hints,
  never detection or authorization evidence. Links remain inert text; archives,
  executables, binary/active content, repository retrieval, code execution,
  and automatic network fetch remain disabled.
- Add a Submission-owned intake/version model with separate local/client state,
  durable intake identity, items, receiving/received/validating/cancelling/
  cancelled/rejected/failed/reconciling states, stable accepted Submission and
  version identities, monotonically ordered lineage, immutable accepted item
  metadata, exact actor/Organization/Activity/Cohort/Enrollment/Participant/
  Task/baseline bindings, and bounded failure categories.
- Treat complete server-observed payload receipt before the exclusive effective
  Submission cutoff as the timing fact. Selection, upload start, client
  percentage, validation completion, or request construction does not reserve
  the cutoff. Validation may finish after cutoff only for a payload completely
  received before cutoff and only while every current acceptance precondition
  still passes.
- Add Submissions-owned application ports and infrastructure adapters for the
  verified frozen Submission requirement, exact current Organization policy,
  current application session and authorization, versioned lifecycle policy,
  private artifact storage, safety scanner, authoritative clock/order,
  PostgreSQL transaction, audit/outbox, durable validation work, cleanup, and
  bounded telemetry.
- Implement the approved scanner-mode distinction. The initial constrained
  text policy may use `disabled_by_approved_policy` without recording or
  presenting a clean scan. `required` accepts only a current `clean` result;
  missing, stale, timed-out, unavailable, or inconclusive scanning fails
  closed. Do not bundle or select a malware vendor in this task.
- Add the next available additive PostgreSQL migration after rechecking the
  implementation-time head. Persist Submission, intake, item, upload receipt,
  validation fact, immutable accepted version/item, lineage, operation/
  idempotency, durable work, lifecycle, audit/outbox, and reconciliation state
  with database-enforced complete scope, uniqueness, append-only history, and
  immutable accepted records.
- Store direct-text and attachment payload bytes in the private artifact store,
  not ordinary relational rows, logs, audit events, idempotency records, or
  telemetry. Generate opaque storage identities server-side. Database metadata
  is the authority for intake/accepted visibility; object possession, object
  keys, ETags, version IDs, digests, or temporary access URLs never authorize a
  product action.
- Make intake creation, upload completion, cancellation, validation result,
  finalization, and reconciliation idempotent and concurrency-safe. Equivalent
  retries converge; mismatched key reuse, duplicate/concurrent finalization,
  cancellation/finalization races, delayed worker results, and lost responses
  never expose a partial or duplicate accepted version.
- Couple each accepted-version commit and its required immutable audit/outbox
  fact in one PostgreSQL transaction. Artifact promotion occurs outside the
  database transaction through conditional immutable object operations; an
  unreferenced promoted object is never visible and is removed through bounded
  reconciliation/cleanup. No external storage or scanner call remains inside
  an open database transaction.
- Add canonical Draft 2020-12 command, outcome, intake, material-policy,
  accepted-version, history, protected-artifact metadata, and bounded-error
  schemas plus valid/invalid fixtures. Update catalog, OpenAPI 3.1, reviewed C#
  DTOs, and strict TypeScript projections together; preserve existing strict v1
  Enrollment meaning and use the already-established production `/v2/assessment`
  surface for additive Submission behavior.
- Extend production Participant **My work** detail with server-supplied
  Submission requirements and limits, local direct-text/file preparation,
  explicit **Submit version** confirmation, receiving/validating/cancelling/
  reconciliation states, item-level safe errors, accepted confirmation,
  newest-first immutable version history, and exact authorized preview/download.
  The browser never calculates acceptance, timing, category availability,
  version number, integrity, or authority.
- Render accepted direct text and Markdown as inert untrusted text in a bounded
  protected-content viewer. Embedded content cannot imitate trusted controls,
  activate links, fetch resources, execute code, or supply authorization. Rich
  Markdown rendering is not required for P0 and requires separate sanitizer/
  renderer qualification before enablement.
- Preserve safe recoverable local input only for the same actor/context. Clear
  protected server content, temporary capabilities, pending callbacks, and
  prohibited actions on logout, application-session rotation/expiry,
  actor/Organization change, Enrollment visibility loss, authorization loss,
  stale responses, or route disposal. Never persist raw Submission content or
  temporary capabilities in browser storage, analytics, logs, or Playwright
  artifacts.
- Implement exact preview/download issuance and use-time authorization. A
  capability expires within five minutes, binds exact actor/action/Organization/
  Enrollment/version/item, cannot be substituted or reused, and produces the
  governing durable audit record before protected download disclosure.
- Implement lifecycle/reconciliation for incomplete temporary payloads,
  rejected/quarantined bytes, accepted versions, idempotency outcomes, audit
  metadata, and authorized holds using independently resolved versioned policy.
  Cleanup preserves minimum provenance and never silently rewrites an accepted
  version or binding.
- Add allowlisted telemetry for intake admission, bytes/count bands, validation
  duration/outcome, finalization latency, work backlog/lease/retry,
  reconciliation, cleanup, and artifact-adapter health. Exclude raw text,
  filenames unless strictly necessary and permitted, Participant attributes,
  identifiers, object keys, access URLs, digests, scanner/parser output,
  credentials, and high-cardinality protected labels.
- Update authoritative implementation-status/traceability rows only after
  repeatable implementation and review evidence exists, then run independent
  backend, frontend, security/privacy, and QA review.

## Out

- Attempt ordinal or entitlement calculation, retry entitlements, Attempt
  start/consumption, ADR-005 cross-module atomic start, Session creation or
  readiness, resolved Session configuration/manifest, or exact Attempt/Session
  Submission binding. This task exposes a narrow exact accepted-version reader
  for that successor but does not invoke it from start.
- Agent/model/parser/tool consumption of accepted material, resolved
  capability compatibility, required-Agent-reading readiness, in-Session later
  material bindings, or any claim that an Agent inspected an item. The UI must
  not show positive Agent-inspection status before the successor implements it.
- Assigned-review queue/workspace integration, Reviewer content disclosure,
  general administrator raw-content browsing, Evidence locators, Evaluation,
  Human revision, Review decision, Result, Release, or export.
- New material categories beyond direct text, `.txt`, and `.md`; archive
  extraction, office/PDF/image/audio/video processing, repository cloning,
  external URL retrieval, code execution, plagiarism/cheating detection, or
  automated proctoring.
- A general artifact manager, unrestricted bucket browser, mutable `latest` or
  `current` payload alias, public object access, long-lived download link, or
  client-selected object key/owner/version.
- Selecting or bundling ClamAV or another malware/scanner product. The
  configurable `ArtifactSafetyScanner` port and its fail-closed modes are in;
  a concrete required-scanner profile is a separately qualified adapter.
- Editing the Activity, activated Cohort, frozen Task/submission requirement,
  baseline, Organization policy, Enrollment/accommodation, timing rule, or
  lifecycle policy through the Submission surface.
- Dynamic memory, learning, calibration reuse, cross-Participant reuse, tools,
  voice, external notifications, a new network service, Redis, an external
  broker, Kubernetes, or another deployment topology.
- Production certification, real Participant data, backup scheduler, HA claim,
  or RPO/RTO claim. The local artifact profile and browser evidence use only
  synthetic non-sensitive data.
- Commits, pushes, pull requests, deployment, or release unless separately
  requested.

# Acceptance and traceability matrix

| Governing behavior | Implementation surfaces | Required evidence |
| --- | --- | --- |
| Only the owning Participant with a current active Enrollment may create or continue intake; current effective Submission timing and immutable activated parents are server-authoritative (`REQ-SUBM-6`, `REQ-SUBM-9`, `REQ-SUBM-13`, `REQ-SUBM-24`, `AC-SUBM-4`, `AC-SUBM-18`) | Submissions intake coordinator, Assessment frozen-requirement port, timing reader, IdentityAccess application-session/authorization port, scoped PostgreSQL repository | Before/at/after exclusive cutoff; suspended/revoked/closed Enrollment; stale session/grant; wrong Organization/activity/cohort/Enrollment/Participant/Task/baseline; permission loss before finalization; no accepted version or disclosure |
| Frozen requirement plus exact current Organization policy enables only complete, supported, non-widening material categories and bounds (`REQ-SUBM-24`–`REQ-SUBM-26`, `REQ-SUBM-44`–`REQ-SUBM-45`, `REQ-OPS-1`–`REQ-OPS-5`, `AC-SUBM-14`, `AC-SUBM-30`, `AC-OPS-1`–`AC-OPS-3`) | Versioned normalized material-policy contract, Configuration policy port, policy resolver, validation domain | Direct text at 1 MiB boundary; 10 items/10 MiB each/25 MiB aggregate; `.txt`/`.md`; declared/detected mismatch; invalid UTF-8; binary/active/archive/truncated/oversized input; inert URL; missing/stale/cross-scope policy; scanner disabled/required/clean/rejected/inconclusive/unavailable/timeout |
| Private artifact storage remains a replaceable adapter and SeaweedFS is used only after its conditional compatibility gate passes (ADR-008, ADR-010 `STACK-DEC-12`, `GATE-STACK-ARTIFACTS`) | Submissions-owned artifact port, AWS SDK adapter in `FlexAgent.Submissions.Infrastructure`, exact pinned local/CI profile, focused artifact integration project | Multipart, conditional create, path-style/custom endpoint, exact version read, integrity metadata, private-by-default, presigned issue/use/expiry, credential rotation, wrong-scope/object substitution, lifecycle, orphan cleanup, joint metadata/artifact backup-export and isolated restore verification |
| Receipt, validation, cancellation, failure, rejection, uncertainty, and acceptance remain distinct; only complete server receipt before cutoff may proceed (`REQ-SUBM-25`–`REQ-SUBM-29`, `AC-SUBM-13`–`AC-SUBM-14`, UI contract) | Intake aggregate/state machine, authoritative receipt facts, durable validation work, cancel/reconcile coordinators, bounded outcomes, Participant projections | Transfer loss/resume-or-restart, cutoff race, cancel during upload/validation, worker timeout/crash/redelivery, validation after pre-cutoff receipt, dependency outage, late/stale result, uncertain final response; no progress/validation false acceptance |
| One accepted immutable version appends ordered lineage; duplicate/concurrent finalization cannot create a partial or second version (`REQ-SUBM-27`–`REQ-SUBM-30`, `REQ-SUBM-33`, `AC-SUBM-13`, `AC-SUBM-15`, `AC-SUBM-17`) | Submission/version domain, PostgreSQL constraints/triggers, idempotency outcomes, finalization coordinator, history query | First/later version numbering, exact stable IDs, immutable update/delete rejection, prior version unchanged, equivalent retry, mismatched key, concurrent finalization, cancellation/finalization race, audit/outbox failure, promoted-object/database failure reconciliation |
| Every list/detail/item/preview/download path is exactly scoped and non-disclosing; temporary artifact access is short-lived and is never authority (`REQ-SUBM-34`, `REQ-SUBM-42`, `REQ-AUTH-12`–`REQ-AUTH-14`, `AC-SUBM-19`, `AC-SUBM-21`, `REQ-OPS-8`) | Scoped history/query service, protected viewer, capability issuer/redemption adapter, audit boundary, no-store HTTP mapping | Guessed IDs, forged parents, cursor/count leakage, object key/version substitution, capability actor/action/artifact mismatch, replay/expiry/revocation, cached preview loss, exact use-time authorization, required-audit outage |
| Every new authenticated **My work** read preserves the existing deployment-wide shared-admission boundary without bypass or duplicate permit consumption (`REQ-SUBM-57`–`REQ-SUBM-58`, `AC-SUBM-40`–`AC-SUBM-41`, `PROP-8`) | Existing Enrollment shared-admission adapter and route boundary, Submission history/detail/status/preview metadata queries, strict HTTP outcomes | Exactly one trusted `(Organization, actor, read)` permit before protected query work; aggregate two-replica exhaustion; independent actor/Organization/surface budgets; accurate positive `Retry-After`; `429` versus fail-closed `503`; no local fallback, protected query start, double consumption, or partition leakage |
| Lifecycle and cleanup preserve accepted lineage while minimizing incomplete/rejected payloads and indirect copies (`REQ-SUBM-29`–`REQ-SUBM-30`, `REQ-SUBM-39`, `REQ-OPS-6`–`REQ-OPS-7`, `REQ-OPS-18`–`REQ-OPS-23`) | Versioned lifecycle resolver, durable cleanup work, object-store deletion adapter, PostgreSQL eligibility/hold query, append-only disposition facts | 24-hour incomplete, seven-day rejected bytes, 365-day accepted version from Activity closure, 90-day idempotency, 730-day audit, legal hold, dependency order, orphan cleanup, restoration and lawful-unavailability provenance; no raw payload in diagnostics |
| Participant UI keeps local work separate from server authority and covers accessible recovery, immutable history, and protected-content loss (`AC-SUBM-13`–`AC-SUBM-15`, `AC-SUBM-18`–`AC-SUBM-19`, `AC-SUBM-21`, `AC-SUBM-23`, `AC-SUBM-28`, UI spec) | Production **My work** API/client/page, local preparation state, progress/status, confirmation dialog, error summary, version history, inert viewer, protected loading/unavailable states | Component tests plus authenticated real interactions for empty/editing/local error/receiving/validating/cancelling/cancelled/rejected/failed/reconciling/accepted/later-version/permission-loss states; keyboard, focus, announcements, reduced motion, both themes, desktop/narrow, 400% reflow screenshots |
| Submission material never creates policy, capability, learning, external retrieval, or operational-data leakage (`REQ-SUBM-35`, `REQ-SUBM-38`–`REQ-SUBM-40`, `REQ-SUBM-48`, `AC-SUBM-26`) | Inert validators/viewer, disabled consumer ports, telemetry allowlist, safe errors/logs, architecture/composition tests | Prompt/control imitation, embedded link, HTML/script/active Markdown, memory/calibration/tool/repository attempts, log/metric/trace/error/browser artifact leakage, disabled capability composition, supply-chain and secret scans |

This task closes only the intake, immutable accepted-version, Participant
history, and exact Participant preview/download implementation rows.
`AC-SUBM-4` remains Partial for Attempt start until its successor consumes the
same current Enrollment denial. `AC-SUBM-16`, `REQ-SUBM-31`–`REQ-SUBM-32`,
`REQ-SUBM-46`–`REQ-SUBM-49`, Attempt, Agent-reading, assigned-review, and
Session rows remain unimplemented or Partial as governed by their owners.

# Architecture and data plan

## Ownership and module boundaries

- `FlexAgent.Submissions` owns normalized material policy, Submission/intake/
  version identities and invariants, intake state decisions, validation outcome
  semantics, finalization/idempotency/reconciliation use cases, exact accepted-
  version queries, lifecycle intent, and browser-safe results.
- `FlexAgent.Submissions.Infrastructure` owns explicit PostgreSQL/Dapper/Npgsql
  persistence, the AWS SDK S3-compatible artifact adapter, SeaweedFS-specific
  configuration kept behind that adapter, authoritative transaction
  participation, audit/outbox adapter, durable validation/cleanup work store,
  and telemetry adapters. Core code must not reference AWS, S3, SeaweedFS,
  Npgsql, Dapper, ASP.NET, Testcontainers, or storage URLs.
- Assessment Configuration retains authority for the activated Cohort, Task,
  immutable baseline, exact frozen Submission-requirement source identity/
  digest, and verification status. Extend an Assessment-owned application port
  to return the normalized frozen requirement needed by intake. Submissions
  must not read Assessment tables or reconstruct policy from display summaries.
- Configuration retains authority for exact current applicable Organization
  material-policy versions and non-widening values. Add a narrow owner port and
  transaction-aware revalidation seam; production positive intake fails closed
  until it supplies a complete exact policy. Synthetic Development/Testing
  fixtures are never Production authority.
- IdentityAccess retains actors, application sessions, Organization context,
  grants, authentication strength, service identities, and delegations.
  Submissions derives no authorization from request bodies, browser action
  visibility, object-store credentials, filenames, or cached projections.
- The API is a thin authenticated transport/composition root. The Worker may
  host Submissions-owned durable validation, reconciliation, and cleanup work
  through bounded service identity/delegation; it must not reuse the
  Session-Invocation work contract or create unscoped aggregate authority.
- The private artifact store contains opaque bytes and immutable object
  versions only. It owns neither Submission acceptance, lifecycle state,
  version numbering, Participant visibility, authorization, nor Evidence
  meaning.
- Canonical schemas under `contracts/` remain wire authority. Browser and C#
  projections are strict mappings, not domain objects or authorization facts.
- No new business module, service, cache, broker, or ADR is expected. Escalate
  before code if the approved modular monolith, private artifact pattern,
  ADR-002/003/005/008/010, or named cross-owner ports cannot preserve the
  required boundary.

## Policy and category contract

- Resolve one exact frozen Task Submission requirement and one exact current
  Organization material policy for every authoritative intake decision. The
  effective contract is their most restrictive compatible intersection; lower
  scopes cannot enable a category or widen a limit.
- Version the normalized contract (initially a v1 contract) and carry exact
  source ID, version ID, digest, schema/contract identity, availability,
  effective time, category definitions, positive limits, detection/encoding,
  integrity, parser/active-content rules, scanner mode, preview/download,
  lifecycle, and compatibility facts.
- Direct text is a separate material item category with the 1 MiB UTF-8 bound.
  Plain text and Markdown attachments require strict UTF-8 and detected
  `text/plain` or `text/markdown` compatibility; `.txt`/`.md` remain hints.
- Do not infer positive policy from current Assessment display summaries or
  opaque development sources. An incomplete source makes the intake
  unavailable with a bounded safe reason.
- Scanner outcomes are bounded and provenance-minimized. When the mode is
  `disabled_by_approved_policy`, validation records the exact policy mode but
  no engine result. When `required`, only a current compatible `clean` result
  may pass.

## Intake and acceptance state

- Keep local browser preparation out of durable product state. Durable intake
  begins only after authenticated server admission and receives a stable
  intake ID, revision, correlation, idempotency context, trusted complete parent
  binding, policy references, lifecycle references, and positive limits.
- Model durable states explicitly: `receiving`, `received`, `validating`,
  `cancelling`, `cancelled`, `rejected`, `failed`, `reconciling`, and
  `accepted`. Do not collapse transfer receipt, validation, or acceptance into
  `submitted`, `uploaded`, `ready`, or `complete`.
- Each item receives a stable server-generated identity and opaque artifact
  reference. Receipt records authoritative database/object-store evidence,
  byte count, integrity digest, detected category inputs, exact object version,
  and receipt UTC time. Browser progress is advisory.
- Validation appends bounded per-item and aggregate facts. A required failure,
  timeout, cancellation, stale policy, or current authorization loss prevents
  acceptance; raw parser/scanner output is not a product or audit payload.
- Finalization reauthorizes the current application session/actor/action and
  complete Enrollment/Task/baseline chain, revalidates current policy and
  effective cutoff using authoritative time and recorded complete receipt,
  checks all exact immutable artifacts and validation facts, resolves lifecycle
  policy, conditionally promotes artifacts, and commits one accepted version,
  items, lineage, operation outcome, and immutable audit/outbox in one database
  transaction.
- Accepted version number is allocated by authoritative database order under a
  per-Submission uniqueness/locking boundary. Clients never choose Submission,
  version, item, owner, object key, receipt time, acceptance time, or current
  alias.
- Cancellation and finalization compete through expected revision and database
  constraints. A late worker/callback cannot move a terminal intake or accepted
  version. An uncertain response reconciles by scope/idempotency and does not
  offer duplicate finalization.

## Artifact flow and failure handling

- Use the ADR-010 AWS SDK adapter with a custom SeaweedFS endpoint and path-style
  addressing. Pin direct/transitive dependencies and the exact container
  artifact through repository lock/supply-chain conventions; do not use a
  floating tag or runtime download.
- Keep buckets/containers private and Organization-partitioned through trusted
  configuration. Generate object keys from server-side opaque identities; the
  partition is defense in depth and never replaces database authorization.
- Issue only short-lived intake-item capabilities after current authorization.
  Bind upload ID/part number, item, actor, Organization, action, allowed length,
  content constraints, and expiry where the storage protocol permits; verify
  the complete result rather than trusting capability presentation.
- Support conditional immutable create/promotion and exact object-version reads.
  Record a service-calculated digest and verify it on acceptance, delivery, and
  restore; do not treat S3 ETag semantics as a universal content digest.
- External upload, storage, and scanner work occurs without an open database
  transaction. Persist bounded intent first, perform external work, then commit
  results idempotently against current revision/delegation/authorization.
- Acceptance authority remains in PostgreSQL. A promoted object without a
  committed accepted-version reference is an orphan, is never previewable or
  downloadable, and is removed by bounded reconciliation. A committed version
  whose exact object cannot be verified becomes honestly unavailable/degraded;
  it is never silently replaced by a later version.
- Backup/export and restore evidence must pair database metadata with artifact
  inventory/version/digest verification. A database-only or object-only restore
  cannot close the artifact gate.

## Persistence

- Recheck migration head immediately before Red. Planning-time head is `0047`
  after the timing/accommodation complete-parent closeout; use the next
  available additive migration rather than assuming `0047`.
- Persist logical Submission identity (one current MVP Task requirement per
  Enrollment without using that uniqueness as future product scope), intake and
  revision, intake items, received artifact facts, append-only validation
  facts, accepted versions, accepted version items, lineage, idempotent
  operation outcomes, durable validation/reconciliation/cleanup work, policy/
  lifecycle references, and audit/outbox correlation.
- Every protected row carries the complete minimal scope necessary for
  database-enforced parentage: Organization, Activity, Cohort, Enrollment,
  Participant, Task, and baseline/source references as applicable. Use
  composite foreign keys/checks/unique constraints so a valid identifier from
  another scope cannot be substituted.
- Accepted version/item identity, ownership, version ordinal, accepted UTC
  time, artifact identity/version/digest, validation/policy provenance, and
  predecessor are immutable. Normal application paths expose no update/delete;
  database constraints/triggers reject mutation where appropriate.
- Failed/rejected/cancelled intake facts append or transition through expected
  revision without creating a version. Payload cleanup and lawful
  unavailability append disposition facts and preserve minimum provenance.
- Use PostgreSQL UTC time and explicit sequence/revision/order. Store
  idempotency scope, normalized trusted command digest, outcome reference, and
  terminal time without raw material.
- Required protected mutations and audit/outbox acceptance share the owning
  transaction. Durable work claims use positive lease/attempt/timeout bounds,
  current service identity/delegation revalidation, duplicate-safe commit, and
  Organization-fair claiming.

## HTTP and canonical contracts

- Add strict additive v2 Submission schemas and routes under the established
  authenticated `/v2/assessment` Participant **My work** boundary. Preserve
  strict v1 Enrollment and existing v2 timing semantics; do not add unknown
  fields to a closed v1 schema or reinterpret the synthetic browser contract.
- Preserve the existing PostgreSQL-backed shared-admission boundary for every
  new authenticated **My work** read. Acquire exactly one trusted read permit
  after application-session resolution and before protected query work; reuse
  the approved `429`/`Retry-After` and fail-closed `503` outcomes without a
  replica-local fallback or a second permit at a nested Submission handler.
- Keep intake admission, upload-capability issue/receipt, cancel, validation
  retry when permitted, finalization, reconciliation, history/detail, preview,
  and download distinct. A GET result or client-visible action never grants a
  mutation or artifact capability.
- Define bounded outcomes for invalid field/category/encoding/type/count/size,
  denied/unavailable, too early/late, policy missing/stale/incompatible,
  upload incomplete/conflict, scanner required/unavailable/inconclusive,
  validation rejected/timed out, stale revision, idempotency conflict,
  cancellation race, already accepted, audit unavailable, storage unavailable,
  rate limited, and uncertain/reconciling.
- Use `no-store` for protected metadata/capability responses, current CSRF and
  application-session protection for browser mutations, explicit request/body/
  query/path/multipart limits, strict unknown-field rejection, and safe
  non-disclosing errors.
- Temporary upload/download capabilities use dedicated minimal response
  contracts and remain only in memory for their immediate operation. Do not put
  access URLs, credentials, raw content, object keys, or unrestricted IDs into
  general Assignment projections, query parameters controlled by navigation,
  analytics, logs, or test artifacts.
- Expose a narrow internal exact accepted-version metadata port for the future
  ADR-005 coordinator. It requires trusted full scope and returns stable exact
  references plus policy/integrity provenance; it has no mutable `latest` or
  `current` lookup and does not expose raw bytes.

## Participant UI

- Extend `ProductionMyWorkDetailPage` and the production Enrollment client;
  keep the synthetic `/browser` Submission workflow explicitly synthetic and
  do not use it as evidence for production authorization, persistence, or
  artifact behavior.
- Use interaction density for Participant preparation and intake, with a clear
  Task/timing/requirement summary, direct-text input, native file picker plus
  optional equivalent drop zone, divider-first item list, explicit local state,
  one dominant **Submit version** action, and a consequence-confirmation dialog.
- Validate client-side only for early feedback. Preserve server-returned limits,
  actions, receipt/acceptance facts, version number, and current state as
  authoritative. Keep exact effective cutoff and named Campaign timezone
  visible throughout transfer and validation.
- Cover local empty/editing/selected/issue/unsent states independently from
  durable receiving/received/validating/cancelling/cancelled/rejected/failed/
  reconciling/accepted states. Throttle progress announcements and never
  announce bytes or timer ticks continuously.
- On submitted validation errors, focus a linked error-summary heading and
  retain safe same-actor input. File removal restores focus to the next item,
  previous item, or **Choose files**. Dialogs contain focus and restore it to
  the trigger/logical successor.
- Version history is newest first but shows stable `Version N`, exact accepted
  time in the named timezone, ordered lineage, authorized material summary, and
  only truthful status. Until downstream binding/capability work exists, do not
  display `Bound`, `Agent inspected`, Attempt readiness, or a start action.
- Exact preview/download uses a bounded viewer/delivery flow after fresh server
  authorization. Direct text and Markdown display as inert selectable text;
  links are non-activating content. Permission loss removes content/actions and
  moves focus to the safe unavailable message without flashing cached content.
- Verify both themes, keyboard-only operation, accessible names/roles/status,
  error association, focus recovery, reduced motion, 400 percent zoom/reflow,
  and desktop/narrow layouts through the real authenticated production profile.

# Threat and privacy checklist

- Cross-Organization/Participant/Enrollment/Task/version/object substitution
  across direct reads, lists/counts, commands, upload parts, callbacks,
  background work, preview/download, cleanup, caches, and restore.
- Malicious or misleading extensions/MIME, invalid UTF-8, binary/active HTML or
  Markdown, archive/decompression/parser bombs, truncated/multipart ambiguity,
  oversized/over-count input, digest mismatch, object-version drift, and
  scanner-state confusion.
- Presigned capability theft/replay, excessive lifetime, verb/part/object
  substitution, browser persistence, redirect/referrer leakage, credential
  rotation/revocation, and bucket-list/public-access exposure.
- Cutoff race, client-clock substitution, stale policy/grant/session,
  cancellation/finalization race, concurrent duplicate acceptance, delayed
  worker result, lost response, database/object partial failure, and orphaned
  artifact cleanup.
- Prompt/control injection through direct text or Markdown, unsafe viewer
  rendering, automatic link fetch, metadata spoofing, capability creation from
  Participant content, and unintended Agent/tool/memory/calibration reuse.
- Sensitive material, filenames, object keys, access URLs, digests, scanner/
  parser details, actor attributes, and high-cardinality IDs leaking through
  logs, metrics, traces, errors, browser titles/history/storage, screenshots,
  support artifacts, idempotency records, or audit payloads.
- Excessive retention, premature cleanup, legal-hold bypass, restore without
  lineage, accepted-object deletion, rejected-byte over-retention, and indirect
  copies in backups/telemetry/diagnostics.

# Plan

- [x] Activation readiness — `p0-participant-timing-accommodations` completed at
  `14f8804` with no blocking external-review findings; migration head `0047`,
  v2 Enrollment/timing contracts, and authenticated-browser `/v2/assessment`
  proxy preserved as authoritative inputs. Requirement-to-surface matrix frozen
  in this plan; no duplicate active task owns this slice.
- [x] Artifact Gate Red — add and run the smallest failing real SeaweedFS/AWS
  SDK contract tests for private conditional/multipart object creation, exact
  version/integrity reads, presigned issue/use/expiry, lifecycle/cleanup,
  credential rotation, wrong-scope substitution, and paired metadata/artifact
  restore.
- [x] Artifact Gate Green/refactor — pin the approved exact SeaweedFS artifact
  and AWS SDK dependencies, implement the Submissions-owned artifact port and
  adapter/profile, pass `GATE-STACK-ARTIFACTS` core contract suite, record
  license/SBOM/digest evidence, or stop for the approved Ceph RGW architecture
  fallback if a blocking contract property fails.
- [x] Policy/domain Red — run failing tests for frozen/current policy
  intersection, direct-text/`.txt`/`.md` categories, positive limits, strict
  UTF-8/detected type, scanner modes, authoritative receipt cutoff, intake
  lifecycle, terminal races, accepted version identity/lineage/immutability,
  and bounded outcomes.
- [x] Policy/domain Green/refactor — implement the minimum transport/storage-
  independent material policy, intake, validation decision, Submission/version,
  idempotency, reconciliation, and lifecycle domain/application behavior.
- [x] PostgreSQL Red — run failing migration-upgrade, complete-scope,
  parent-substitution, uniqueness, immutable accepted-row, append-only fact,
  version-allocation, idempotency, concurrent finalization, cancel/finalize,
  audit-failure, durable-work lease/redelivery, lifecycle/hold, and UTC/order
  tests against PostgreSQL 18.
- [x] PostgreSQL Green/refactor — add the next available migration,
  Submissions repositories, named transaction coordinators, Assessment/
  Configuration/IdentityAccess/lifecycle owner adapters, audit/outbox, durable
  work, reconciliation, cleanup, and allowlisted telemetry without cross-module
  SQL.
- [x] Intake integration Red — run failing end-to-end adapter tests for receive,
  complete-receipt, validation, conditional promotion, acceptance, duplicate/
  uncertain response, permission/policy loss, worker crash/redelivery,
  scanner/storage outage, rejection cleanup, and accepted-object verification.
- [x] Intake integration Green/refactor — compose API/Worker infrastructure and
  implement bounded validation/reconciliation/cleanup so external calls remain
  outside transactions and only PostgreSQL-committed acceptance becomes
  visible.
- [x] Contract/HTTP Red — run failing canonical schema/fixture/catalog,
  OpenAPI, C#/TypeScript parity, strict serialization, positive/negative HTTP,
  CSRF, no-store, body/query/path/multipart limit, safe-error, capability,
  cancellation, finalization, reconciliation, preview/download, shared-read-
  admission bypass/double-consumption/`429`/`503`, and leakage tests.
- [x] Contract/HTTP Green/refactor — implement thin authenticated v2 routes and
  strict coordinated projections without changing v1 Enrollment or synthetic
  browser meaning.
- [x] React Red — run failing production **My work** API/component tests for
  requirement summary, local preparation, file removal, Submit-version
  confirmation, receiving/validating/cancelling/cancelled/rejected/failed/
  reconciling/accepted states, later immutable versions, exact viewer/download,
  authorization loss, inaccessible content, focus, announcements, and
  responsive records.
- [x] React Green/refactor — implement the approved Participant interaction
  using shared design-system primitives, protected-state clearing, safe
  same-actor recovery, inert content, accessible focus/announcements, and
  desktop/narrow/400-percent behavior.
- [x] Close remaining consistency gaps: catalogued version-detail and preview
  contracts, per-item preview when a version has multiple items, reconciling
  retry, and accepted-payload cleanup eligibility after Activity closure.
  Focused plus proportionate Postgres/HTTP/contract evidence re-run 2026-08-25.
- [x] Run the authenticated production profile through real Participant intake
  and exact preview/download interactions with synthetic content; use
  Playwright MCP accessibility snapshots and desktop/narrow/400%/both-theme/
  focus/dialog/error/progress/permission-loss screenshots under
  `.playwright-mcp/` only. (Desktop, dark, 360px, 320px, dialog, error, accept,
  later version, preview, session download, rebuilt-compose Version 2 preview,
  CSS `zoom: 4`, live receiving with **Cancel intake**, cancelled recovery,
  Version 3 preview, and sign-out from preview captured. Live validating was
  not captured. 400% zoom is evidence, not a reflow pass.)
- [x] Run proportionate regression, docs, whitespace, secret-scan allowlist, and
  remaining authenticated Playwright receiving/cancel/sign-out evidence. Full
  solution/OCI/SBOM/locked restore, paired backup product, and live validating
  remain residual. Independent review of this continuation is next.
- [x] Record external review approval of `b67a922` — the `7dac50c` → `28c22f3`
  → `3bd256f` / `c60a280` → `b67a922` security/correctness remediation chain
  is closed with no remaining blocking findings; no further remediation commit
  is requested from that chain.
- [x] Remediate independent review of `7e7ffd8` accepted-payload cleanup:
  honor `DeleteAsync` success before disposition/complete; carry exact
  artifact version into cleanup work; keyset-paginate past ineligible rows;
  keep independently resolved versioned lifecycle policy an explicit partial
  gap (approved 365-day OPS default only).
- [x] Remediate `f0974fb` review: backfill exact artifact versions on already-
  queued cleanup work, fail closed when a version is absent, and persist a
  rotating accepted-cleanup scan cursor so held/open pages cannot starve later
  eligible artifacts.
- [x] Remediate `8b4aae1` review: preserve unbackfillable cleanup work as
  terminal `failed` with `exact_artifact_version_unavailable` instead of
  deleting the row; CAS scan-cursor advancement with a generation column so
  replica writes cannot move the cursor backwards.
- [x] Remediate `ba87718` review: restore shipped `0055` immutability, move
  terminal-failure/disposition uniqueness into additive `0057`, reconstruct
  recoverable unversioned-item provenance, and complete duplicate
  accepted-cleanup work after a peer disposition.
- [x] Remediate `3dbb93f` review: do not unique-index disposition audit facts;
  add a separate disposition acquisition guard; join reconstructed cleanup
  provenance to intake/accepted parents (legacy kind when unknown).
- [x] Remediate `0b2527a` review: restore shipped `3dbb93f` `0057` checksum;
  drop the disposition unique index in additive `0059`; give accepted-version
  reconstruction precedence (including accepted intake + item overlap) and
  recover `version_id`.
- [x] Remediate `c84e960` review: non-destructive upgrade for databases already
  sitting on duplicate disposition facts at `0056`, without editing immutable
  `0057`/`0058`/`0059` or rewriting history.
- [x] Record external review approval of `479c851` — the accepted-payload
  cleanup / disposition-upgrade remediation chain from `7e7ffd8` through
  `c84e960` is closed with no remaining blocking findings; no further
  remediation commit is requested from that chain.
- [x] Remediate `1150e21` P2: refresh authoritative intake after cancel
  conflict (`completeItem`/`finalize` wins) and treat cancel-success plus
  refresh failure as reconciling, with reverse-race vitest coverage.
- [x] Remediate `6cb48bb` P2: known successful cancel recovers as cancelled
  even when older accepted versions exist; hide Cancel while reconciling.
- [>] Re-run independent backend, frontend, security/privacy, and QA review for
  remaining planned slice work after this continuation; reconcile actual
  changes with this plan and the governing sources, update truthful
  implementation-status rows, and retain the completed task record.

# Current state

The slice remains **in progress**. Independent review of `6cb48bb` requested
one remaining P2: a known successful cancel recovered as `accepted` whenever
older versions existed, and **Cancel intake** stayed available during
reconciling against a stale revision.

Remediation: `after-cancel` refresh with no active intake sets `cancelled`
regardless of `version_history`. Cancel is hidden while reconciling; only
**Refresh assignment** is offered. Unknown cancel/finalize races remain a
separate conflict-refresh path.

Red: recovery with existing Version 1 still showed Cancel while reconciling.
Green: My work tests **12 passed**, including later-version cancel then
successful refresh showing cancelled and Version 1 only. Web typecheck passed.

Remediation: after a failed cancel, refresh authoritative **My work**
submission state (received intake for a meaningful retry, or accepted history
if finalize won). Successful cancel plus a failed projection refresh enters
`reconciling` with **Refresh assignment** and cancel-specific copy, not a
false “could not be cancelled” error. `retryRefresh` applies server state
instead of always assuming accept.

Red: three reverse-race vitest cases failed while stuck on cancelling. Green:
My work tests **11 passed**, including cancel-wins, `completeItem` wins,
`finalize` wins, and cancel-success/refresh-failure. Web typecheck passed.

Independent review of this remediations is next. Residual slice gaps are
unchanged (lifecycle policy, Worker cleanup, Activity closure, paired restore,
live validating Playwright, 400% reflow).

Authenticated Playwright on rebuilt SPA at `http://localhost:18080` (synthetic
participant, Timing Accommodation QA): receiving with **Cancel intake**,
cancelled recovery without a Version 4, Version 3 inert preview, and sign-out
from an open preview. One delayed item POST after cancel returned HTTP 409
(expected race). Live validating was not captured because finalize is short.

Proportionate verification 2026-08-25: Submissions **113**, architecture **41**,
contracts **173**, SubmissionPersistence **14**, HTTP negatives **9**, My work
vitest **8**, web typecheck, `check_docs`, `git diff --check`. Gitleaks on git
history still reports one predecessor-task finding in
`p0-participant-timing-accommodations.md`; Submission fixture/HTTP keys are
allowlisted. Full solution/OCI/SBOM/locked restore and paired DB+artifact
restore were not re-run.

Still open: independently resolved versioned lifecycle policy; Worker-hosted
cleanup; Configuration-backed Production/Staging org material policy;
Assessment Activity-closure persistence; paired restore product; live
validating Playwright; WCAG 400% reflow; in-page permission-loss (sign-out
navigates to Sign in required); independent backend/frontend/security/QA
review of this continuation.

Next: independent reviews of this continuation. Do not mark the task completed
until those reviews and recorded residual gaps are accepted.

- **Red:** catalog counts expected 31/37 representative/closure schemas;
  version-detail HTTP had no catalogued DTO; cleanup tests did not cover
  Activity-closure retention; vitest did not cover multi-item preview.
- **Green:** `AcceptedVersionDetailV2` and `ProtectedItemPreviewV2` are
  catalogued with OpenAPI `$ref`, C#, TypeScript, and fixtures. HTTP maps
  version detail through the catalogued DTO. **My work** previews a selected
  item when a version has multiple items and offers **Refresh assignment**
  after reconciling-after-accept. Domain cleanup deletes accepted bytes only
  when `IActivityClosurePort` returns a closure instant at least 365 days in
  the past and no legal hold is active. Migration `0053` allows
  `cleanup_accepted` work/disposition kinds. Production/Staging still use
  `UnavailableActivityClosurePort` because Assessment does not persist
  Activity closure.
- Authenticated Playwright on rebuilt `api`/`spa` at `http://localhost:18080`:
  Version 2 inert preview and Download exact item remain available. CSS
  `zoom: 4` captured; sticky chrome dominates the viewport. 360px captured.
  Synthetic participant only.

Observed tests (2026-08-25 `f0974fb` follow-up):

- `FlexAgent.Submissions.Tests` **111 passed** (delete-false, exact version,
  missing-version fail-closed, 20-disposed page, 20-held persisted scan cursor)
- `FlexAgent.Architecture.Tests` **41 passed**
- `SubmissionPersistenceTests` **14 passed**
- `Upgrade_from_0053_adds_durable_work_artifact_version_id` **passed**
- `Upgrade_from_0053_pending_cleanup_backfills_exact_artifact_version` **passed**
- `Upgrade_from_0001_backfills_idempotency_and_rejects_conflicting_retry` **passed**

Still open: independently resolved versioned accepted-payload lifecycle policy
(Organization narrowing); Worker-hosted cleanup; Configuration-backed
Production/Staging org material policy; Assessment Activity-closure persistence
so accepted cleanup can enqueue in Production; paired DB+artifact restore as a
joint backup product; transferable five-minute browser presigned URLs
(intentionally not issued); live cancel/receiving/validating and permission-loss
Playwright; WCAG 400% reflow polish; full regression/supply-chain/OCI;
independent backend/frontend/security/QA review of this remediation by a
reviewer who did not implement it.

Next: independent reviews of this continuation, then remaining lifecycle and
regression gates. Do not mark the task completed until those reviews and
recorded residual gaps are accepted.

# Decisions

- Create this separate task file because protected Submission intake and
  immutable versioning are a distinct multi-step slice from timing/
  accommodations and from future Attempt start. Keep one state file for the
  whole intake slice, including its required artifact qualification.
- Keep all new business behavior in the existing Submissions module. The
  already-split `FlexAgent.Submissions.Infrastructure` project is the approved
  adapter home because object storage, PostgreSQL, durable work, and security-
  sensitive isolation meet the repository project-splitting criteria.
- Treat ADR-008/ADR-010 artifact qualification as Phase 0, not optional
  closeout. Passing fast fake tests cannot substitute for real SeaweedFS
  evidence; failing a blocking property returns to Architecture and the
  approved Ceph RGW fallback path.
- Store direct text and attachment bytes through the same protected artifact
  abstraction. Relational state stores immutable metadata/provenance and remains
  the acceptance/visibility authority; this avoids raw Participant content in
  ordinary product rows and gives downstream consumers one exact-version model.
- Keep quarantine artifact identity, accepted artifact identity, and accepted
  Submission version distinct. External promotion cannot itself make a version
  visible; PostgreSQL acceptance is authoritative and orphan cleanup is
  explicit.
- Use exact stable accepted-version/item references and never expose a mutable
  `latest`/`current` consumer API. A newest-first Participant projection is a
  presentation order, not an authority alias.
- Preserve the existing synthetic browser Submission behavior for synthetic
  development until separately retired. Production code, tests, traceability,
  and Playwright evidence must use the authenticated v2/PostgreSQL/object-store
  path.
- Use an inert text viewer for direct text and Markdown in this slice. Do not
  add a rich Markdown renderer/sanitizer dependency merely for presentation;
  a later qualified implementation may enhance formatting without changing the
  immutable payload contract.
- Support the approved external-scanner modes but enable no concrete scanner by
  default. The initial strict text policy may explicitly disable external
  scanning; no UI or record may call that outcome `clean` or `malware-free`.
- Add no Attempt, Session, Agent-reading, Reviewer, Evidence, memory, learning,
  tool, repository, or general artifact-browsing behavior. The only downstream
  seam is a trusted exact accepted-version metadata port for a later ADR-005
  coordinator.
- No new ADR is expected. Any need for a different object-store family,
  distributed transaction, external broker, new material category, required
  scanner product, rich active-content rendering, or different acceptance
  authority returns to the owning architecture/product concern before code.

# Findings / deviations

- **Predecessor closeout:** `p0-participant-timing-accommodations` is
  `completed` at `14f8804`. External review found no blocking findings on
  Participant provenance, decide transport, revision bounds, or v2 gateway
  proxy. Migration `0047`, OpenAPI canonical refs, and administrator multi-dimension
  `current_accommodations[]` remain intact. This task is activated; Phase 0
  artifact-gate work is the next blocking step.
- **CI remediation (`67ac540`):** Implementation run **32732247658** on
  `14f8804` failed supply-chain Secret scan on accommodation fixture
  `idempotency_key` values (`acc-revoke-synthetic-0001`). Narrow gitleaks
  allowlist extended; local `gitleaks detect` reports no leaks. Await green
  GitHub corroboration on `67ac540` or later.
- **Artifact readiness gap (closed for core gate):** SeaweedFS/AWS SDK artifact
  port, pinned profile, and six negative scope-isolation contract tests exist.
  Lifecycle/cleanup/restore and paired database/artifact restore evidence remain
  open planned work.
- **Frozen requirement gap (narrowed):** Assessment now supplies frozen MVP
  material policy only when the activated Cohort Task identity/digest match and
  baseline verification is not degraded. It still does not persist a richer
  frozen category snapshot beyond approved OPS defaults bound to that Task.
- **Current policy gap (narrowed):** Configuration still has no stored
  material-policy source. Development/Testing use environment-eligible OPS
  defaults; Production/Staging remain fail-closed.
- **Migration/contracts gap (closed for canonical contracts):** persistence head
  is `0060` restoring facts parked by `0056a` around immutable `0057`, plus `0059`
  accepted-reconstruction precedence and disposition unique-index drop, `0058`
  disposition acquisition guards, `0057` as shipped at `3dbb93f`, `0056`
  replica-safe scan generation, `0055` exact-version backfill
  (shipped checksum), and
  `0048`–`0054` intake/
  version/hold/capability tables; v2 Submission command/outcome/My work/
  version-detail/preview schemas are catalogued. Recheck heads before further
  additive migrations.
- **Synthetic/production split:** `SyntheticBrowser` and the non-production
  `MyWorkPage` remain synthetic. Production `ProductionMyWorkDetailPage` now
  includes local preparation, Submit version, cancel, preview, and download;
  do not treat the synthetic browser contract as production evidence.
- **Lifecycle implementation gap (narrowed 2026-08-25, `ba87718` follow-up):** incomplete/rejected cleanup and disposition facts exist (API-hosted). Accepted-object cleanup requires successful exact-version `DeleteAsync` before disposition, fails closed when the exact version is absent, backfills already-queued `0053` work in shipped `0055`, reconstructs recoverable unbackfillable provenance in `0057`, and persists a rotating scan cursor with CAS `generation` (`0056`). Duplicate accepted-cleanup work after a peer completes is completed as a no-op when a disposition exists. Eligibility still uses the approved 365-day OPS default through `IAcceptedPayloadLifecyclePolicyPort` with `IndependentlyResolvedFromOwner=false`. Assessment still has no Activity-closure timestamp, so Production enqueue remains idle for that class. Worker-hosted cleanup and paired database/artifact restore evidence remain open. Cleanup remains API-hosted, so replica-safe cursor CAS remains required until a single Worker owner exists.
- **Consistency continuation (2026-08-25):** catalogued v2 version-detail and
  preview schemas; HTTP no longer serializes the application DTO; **My work**
  offers **Refresh assignment** after reconciling-after-accept; multi-item
  versions expose per-item preview when `item_count > 1`. Traceability rows in
  `docs/requirements/features/submission-attempts.md` updated.
- **Consistency review (2026-08-24):** Submission query/mutation previously used
  administrator-only `EnrollmentAuthenticationPolicy` and `submissions.*`
  admission actions that participants do not hold. Aligned with My work timing:
  `assessment.assignment.discover` admission on `assignment` resources;
  submission-specific actions remain audit labels. Fixed idempotency replay
  mapping (no longer puts `enrollmentId` into `version_id`). `unavailable_reason`
  now distinguishes `policy_unavailable` from `enrollment_not_active`.
  Postgres integration **327 passed**; architecture **41 passed**; web `tsc` and
  `ProductionMyWork` component tests pass.
- **Security review remediation (`28c22f3`):** External merge review on `7dac50c`
  flagged six blockers. Remediated at the time: production `AddSubmissionIntake()`
  registered `Unavailable*` ports when PostgreSQL was wired. That composition
  was later replaced (2026-08-25) by Assessment-verified frozen requirements and
  environment-scoped organization policy; Production/Staging still fail closed
  on current org policy. `FindIntakeAsync` and coordinator cancel/finalize require enrollment
  scope plus participant ownership; `BeginAsync` reuses stable
  `submissions_submissions` per enrollment; version allocation locks parent row
  then `MAX()+1`; finalize idempotency stores/replays `version_id`; stale
  revision maps to bounded `stale_revision` instead of 500. Added
  `IntakeCoordinatorTests` (cross-enrollment deny, stable submission reuse,
  finalize replay, stale revision). `FlexAgent.Submissions.Tests` **94 passed**
  on 2026-08-24 after remediation; build Release **0 warnings**.
- **Artifact gate follow-up:** `S3ArtifactStore` now validates
  `ArtifactObjectKey` organization scope on put/get/delete/upload-presign/
  download-presign (`scope_mismatch`). SeaweedFS contract suite expanded to six
  negative isolation tests (**6 passed**). Migration `0049` adds complete
  Submission parent-scope FKs and predecessor lineage constraints; finalize
  writes `predecessor_version_id` for version N>1. `SubmissionPersistenceTests`
  added for parent substitution rejection, stable submission reuse, and lineage
  persistence. Full Postgres integration **330 passed** after `0049`.
- **Parent-scope Task binding (`0050` / `b67a922`):** Review of `c60a280` found
  `0049` unique/FKs omitted `task_source_id`, `task_version_id`, and
  `task_content_digest`. Additive `0050` extends Enrollment unique
  `uq_submissions_enrollments_complete_binding`, replaces
  `submissions_submissions` enrollment FK with a full binding parent FK,
  expands intake enrollment parent to the same tuple, and rebuilds
  `uq_submissions_submissions_complete_scope` plus intake/accepted-version
  parent FKs through Task identity. Red: 9 persistence cases failed because
  substituted Task columns inserted. Green: those 9 plus the existing 3
  persistence cases and `Upgrade_from_0049_*` pass. Full Postgres **340
  passed / 1 failed** (`KeycloakBackChannelLogoutTests` 403, known flake).
- **Independent review approval (`b67a922`):** External review found no new
  blocking finding. Review sequence: `7dac50c` (six blockers) → `28c22f3`
  (original six fixed) → `3bd256f` / `c60a280` (S3 isolation, lineage,
  partial parent scope) → `b67a922` (Task-binding scope). All findings raised
  in that chain are resolved. GitHub exposes no commit status checks for this
  SHA. Remaining open items are planned slice work (concurrent finalize,
  contracts, production UI, Playwright, lifecycle), not unresolved review
  findings.
- **Review of `6cb48bb` (P2, remediating):** Known successful cancel plus
  later refresh no longer treats older `version_history` as this intake
  having been accepted. Cancel is hidden while reconciling.
- **Review of `1150e21` (P2, remediating):** Cancel conflict could leave
  cancelling while completeItem/finalize committed. Reverse-race tests plus
  authoritative GET after cancel failure, and reconciling when cancel succeeds
  but refresh fails. `c6d7f43` screenshot-only child has no logic impact.
- **Continuation 2026-08-25 (cancel during receiving):** Confirm dialog closed
  on submit start so receiving is not inert. Local `active_intake` after begin
  enables **Cancel intake** during receiving/validating. In-flight submit
  generation is invalidated so delayed `completeItem` cannot finalize after
  cancel. Red: vitest could not find **Cancel intake** during held item POST.
  Green: 8 My work tests passed. Live cancel Playwright captured.
- **Continuation 2026-08-25 (owner ports and recovery):** Assessment-owned
  frozen requirement now verifies the activated Task identity/digest rather
  than returning policy unconditionally. Organization material policy follows
  the accommodation pattern: Development/Testing receive approved OPS defaults;
  Production/Staging remain fail-closed until Configuration publishes a current
  versioned material-policy source. Exact download is a use-time authenticated
  GET, not a transferable five-minute capability. Rebuilt compose Playwright
  on 2026-08-25 covers prepare, confirm, accept, later version, preview, and
  session download.
- **Dapper accepted-history crash (closed 2026-08-25):** finalize succeeded then
  GET `/submission` returned 500 because `AcceptedVersionSummary` could not be
  materialized (`timestamptz` as `DateTime`, `COUNT(*)` as `Int64`). List/find
  now map through Dapper-safe row types. Persistence **14 passed**; live GET
  history then showed Version 1.
- **Independent review approval (`479c851`):** External review found no blocking
  finding on the cleanup/disposition-upgrade remediations. Sequence: `7e7ffd8`
  → `f0974fb` → `8b4aae1` → `ba87718` → `3dbb93f` → `0b2527a` → `c84e960` →
  `479c851` (`0056a` park / `0060` restore around immutable `0057`). GitHub
  exposes no commit status checks for this SHA. Remaining open items are
  planned slice work, not unresolved review findings.
- **Cleanup hosting (interim):** incomplete-artifact cleanup runs as an API
  `BackgroundService` (`SubmissionCleanupHostedService`) so the Worker OCI
  copy set does not expand in this slice. The Session invocation work
  contract is not reused. Promote to the Worker host when that Dockerfile
  surface is deliberately expanded.
- **Downstream compatibility:** Evidence-locator schemas already represent exact
  Submission locations, and ADR-005 requires exact version/item metadata. This
  task must produce stable compatible references without claiming that Attempt,
  Session, Evidence, Agent, or Reviewer consumers are implemented.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Governing product/requirements/UI/architecture sources reconciled | passed for planning | Approved sources agree on direct text plus UTF-8 `.txt`/`.md`, private quarantine, policy/scanner fail-closed behavior, immutable accepted versions, exact access, lifecycle, and downstream exact binding. No governing open question remains. |
| Existing task duplication check | passed | No `.work/active/` task owns production Submission intake and immutable versioning; timing/accommodation explicitly excludes it. |
| Repository seam inventory | passed for planning | Existing Submissions core/infrastructure, PostgreSQL Enrollment/timing, authenticated v2 Assessment API, production **My work**, Worker, audit/outbox, contract catalog, and test patterns are reusable. No production Submission/artifact implementation exists. |
| Predecessor closeout | passed — `14f8804` | `p0-participant-timing-accommodations` completed; external review approved with no blocking findings. Intake activated; migration head `0047` and v2 timing authority preserved. |
| SeaweedFS/AWS SDK artifact compatibility | passed — scope isolation plus exact-version delete | `FlexAgent.Artifact.Integration.Tests` **9 passed** against `chrislusf/seaweedfs:4.29`: conditional create, exact-version get, exact-version delete then GET-fail, presigned download, digest verification, and negative get/put/delete/upload-presign/download-presign scope checks (`scope_mismatch`). Paired restore as a joint backup product remains open. |
| Frozen/current material-policy authority | partial | Assessment verifies activated Task identity (`OwnerMaterialPolicyPortTests` **5 passed**). Testing/Development org policy is environment-eligible OPS defaults. Production/Staging org policy still returns `null` (`policy_unavailable`) until Configuration stores a current material-policy version. |
| Domain red/green | passed for intake receipt plus cleanup correctness | `FlexAgent.Submissions.Tests` **113 passed** on 2026-08-25, including missing-version terminal `failed`, 20-held persisted scan cursor, scan CAS, and two-replica accepted-cleanup duplicate no-op after peer disposition (red: duplicate returned `failed`; green: both `completed`, one delete, one disposition). |
| PostgreSQL migration/isolation/concurrency/audit | partial | Head `0060`. `0057` blob unchanged from `3dbb93f` (`3c2ab5b4…`). Red: historical two-fact `0056` upgrade failed creating `uq_submissions_artifact_dispositions_artifact` (`23505`). Green: same facts survive through `0060` (embedded + Grate tool); later duplicates still insert; reconstruction/backfill/`0001` list passed; persistence + scan CAS **15 passed**; empty Grate migrate passed. Recreate only databases that applied edited `0b2527a` `0057`. Do not mix `3dbb93f` cleanup binaries with a schema through `0059`. Full Postgres suite not re-run. |
| Canonical schema/OpenAPI/C#/TypeScript parity | passed for added v2 Submission contracts | Catalog **33** representative schemas; `FlexAgent.Contract.Tests` **173 passed**; OpenAPI `$ref` for My work, version detail, and preview. Node OpenAPI parity **8 passed**. |
| HTTP CSRF/admission/isolation | passed for added negatives | `SubmissionHttpNegativeContractTests` **9 passed**: begin/cancel/finalize CSRF, unauthenticated submission/version-detail/preview/download `no-store`, unauthenticated skip of shared admission, exhausted shared admission without protected query. |
| API/Worker integration | partial | v2 routes: query, begin, complete-item, cancel, finalize, version detail, item preview, item download. Artifact store: SeaweedFS when `ArtifactStorage` is configured. Cleanup loop is API-hosted, not Worker-hosted. Finalize scanner calls are outside the DB transaction. |
| React/accessibility | partial | `ProductionMyWorkDetailPage` implements local preparation, Submit-version dialog (closes on start), cancel during receiving/validating, cancel-conflict refresh, cancel-success/refresh-failure reconciling without stale Cancel, later-version cancel recovery as cancelled beside older history, intake status, inert preview, download, version history, permission-loss focus, reconciling-after-accept with Refresh assignment, and per-item preview when a version has multiple items. Focused vitest **12 passed**. Live validating Playwright remains. |
| Authenticated Playwright MCP | partial — rebuilt SPA 2026-08-25 | Receiving with **Cancel intake**: `.playwright-mcp/page-2026-08-25T05-49-57-764Z.png`. Cancelled without Version 4: `...T05-51-19-390Z.png`. Version 3 inert preview: `...T05-52-17-009Z.png`. Sign-out from preview: `...T05-52-54-390Z.png`. Delayed item POST after cancel was HTTP 409. Live validating not captured. |
| Regression/security/supply-chain/OCI/recovery/docs | partial | `python3 scripts/check_docs.py` passed. `git diff --check` clean. Submissions **113**, architecture **41**, contracts **173**, persistence **14**, HTTP negatives **9**. Gitleaks: Submission keys allowlisted; one historical predecessor-task finding remains. Locked restore, SBOM, OCI, paired restore not re-run. |
| Independent review — security/correctness remediation chain | passed — external review `b67a922` | No blocking findings. All issues raised from `7dac50c` through follow-up reviews resolved at `b67a922`, including S3 scope isolation, predecessor lineage, partial parent scope (`0049`), and Task-binding parent tuple (`0050`). `SubmissionPersistenceTests` **12 passed**; full PostgreSQL **340 passed / 1 failed** (Keycloak 403 flake). GitHub commit statuses not independently visible. Separate full-slice backend/frontend/QA review remains for unconsumed planned work. |
| Independent review — accepted-payload cleanup remediations | passed — external review `479c851` | No blocking findings. `c84e960` P1 closed by `0056a`/`0060` around unchanged `0057`. Persistence + scan CAS **15 passed**; historical duplicate path covered by embedded runner and Grate tool. Full PostgreSQL suite still not re-run (recorded partial). GitHub commit statuses not independently visible. |

# Planned verification command set

- Focused core: `dotnet test --project
  tests/Submissions/FlexAgent.Submissions.Tests/FlexAgent.Submissions.Tests.csproj
  -c Release` with observed Red filters before Green.
- Artifact adapter: create a focused integration project and run it against the
  exact pinned SeaweedFS profile for conditional/multipart/exact-version/
  integrity/presigned/lifecycle/rotation/substitution/restore evidence.
- PostgreSQL: focused Submission migration/repository/work/lifecycle filters,
  then the full
  `tests/Integration/FlexAgent.Postgres.Integration.Tests` Release project.
- Runtime/API/Worker: focused Submission HTTP and composition filters, then the
  full `tests/Runtime/FlexAgent.Runtime.Tests` Release project and applicable
  Worker host tests.
- Architecture/contracts: architecture and contract .NET projects plus
  `pnpm --filter @flex-agent/contracts test`, OpenAPI parity, and all new
  canonical fixtures.
- Web: focused production **My work** intake/viewer tests, then web lint,
  typecheck, test, and build.
- Browser: authenticated Compose profile extended with the exact private
  artifact service, then Playwright MCP through real Participant interactions;
  synthetic content and screenshots only under `.playwright-mcp/`.
- Recovery/security/performance: artifact/database paired export/restore,
  negative isolation/capability/leakage matrices, validation two-minute and
  metadata-finalization p95 evidence, resource/backpressure/cleanup tests.
- Regression: locked .NET/pnpm restore, full solution Release tests,
  `python3 scripts/check_docs.py`, `git diff --check`, supply-chain/license/
  SBOM/vulnerability/gitleaks, OCI, and operability verification.

# Blockers

No unresolved product, requirements, UI/UX, lifecycle-policy, or architecture
decision blocks planning. The timing/accommodation predecessor gate is closed at
`14f8804`.

The blocking gate for functional intake work is Phase 0 — execute and pass the
real SeaweedFS/artifact-safety compatibility suite (`GATE-STACK-ARTIFACTS`). If
a blocking immutability, version-identity, lifecycle, capability, or restore
property fails, stop and obtain the Architecture owner's ADR-008 fallback
decision before continuing.

Do not run overlapping Submissions migrations/contracts/UI rewrites against
another active slice.

Docker/object-store availability is required for artifact and PostgreSQL
evidence but is not a product decision. No external credential, paid provider,
real Participant data, production deployment, or selected malware vendor is
required; all preparation and verification use pinned local infrastructure and
synthetic non-sensitive content.

# Completion

- [x] Predecessor task is completed and this plan is reconciled with its final migration, contract, API, UI, and traceability state
- [ ] Exact SeaweedFS/AWS SDK profile passes ADR-008/ADR-010 artifact, safety, supply-chain, and paired-restore gates or an approved fallback supersedes it
- [ ] Frozen/current material-policy owner ports and fail-closed Production composition are verified
- [x] Domain Red/Green/Refactor evidence covers intake, validation, timing, immutable versions, lineage, idempotency, races, reconciliation, and lifecycle
- [x] PostgreSQL migration, complete-scope isolation, immutable/append-only constraints, concurrency, work, clock, audit, and lifecycle evidence passes
- [x] Canonical schemas, fixtures, catalog, OpenAPI, C#, TypeScript, HTTP, and browser projections remain strict, compatible, and traceable
- [ ] Participant local/receiving/validating/cancelling/rejected/failed/reconciling/accepted/history/viewer states pass component, accessibility, responsive, and authenticated Playwright verification
- [ ] Exact preview/download capabilities pass issue/use-time authorization, expiry, replay/substitution, revocation, required-audit, and non-disclosure tests
- [ ] Incomplete/rejected/orphan/accepted artifact lifecycle, legal hold, cleanup, and paired database/artifact restore evidence passes
- [ ] Telemetry, logs, errors, browser state, audit, support artifacts, and test evidence contain no raw Submission content, credentials, access URLs, object keys, scanner/parser details, or protected high-cardinality labels
- [ ] Focused, integration, performance, security, full regression, locked restore, supply-chain, OCI, docs, whitespace, and operability gates pass with exact recorded evidence
- [ ] Governing specifications and implementation-status rows remain truthful without claiming Attempt, exact binding, Session, Agent reading, Reviewer access, Evidence, Evaluation, or Release
- [x] Security/correctness independent review findings from the `7dac50c` →
  `b67a922` chain are resolved (approved `b67a922`)
- [x] Accepted-payload cleanup / disposition-upgrade independent review
  findings from `7e7ffd8` through `c84e960` are resolved (approved `479c851`)
- [ ] Independent backend, frontend, security/privacy, and QA review for
  remaining planned slice work is resolved or explicitly accepted by an
  authorized owner
- [ ] Remaining gaps and unverified behavior are recorded; task state is safe and complete for external review and retained
