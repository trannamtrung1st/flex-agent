---
id: hosted-text-session
status: planned
created: 2026-09-02
updated: 2026-09-02
---

# Goal

Expose the existing governed text Session runtime through the authenticated
production host and production SPA so a Participant can enter the one Session
committed by Attempt start, load authoritative state and transcript, send text,
receive durable incremental Agent output, recover from connection or command
uncertainty, complete deliberately, and inspect an authorized terminal
transcript. Supply the separately authorized administrator pause, resume, and
terminate surface and the assigned-Reviewer read-only terminal transcript
entry required by the approved P0 Session contract.

This task realizes existing approved behavior. It does not define a second
Session model, promote the synthetic browser harness, or enable a deferred
capability.

# Governing sources

- `AGENTS.md`, `docs/README.md`, `.work/README.md`, and the
  `implementation-workflow` skill — authority, tracked execution state,
  specification-driven TDD, review, and retirement rules
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — canonical Session meaning, the P0 text-only
  assessment slice, outcome-chain separation, and deferred capabilities
- `docs/requirements/features/session-text-lifecycle.md` — owning observable
  behavior (`REQ-SESS-1`–`REQ-SESS-85`, `AC-SESS-1`–`AC-SESS-48`)
- `docs/requirements/features/auth-resource-isolation.md` — authentication,
  action/resource authorization, complete ownership-chain validation,
  Participant and Session isolation, revocation, and negative coverage
  (`REQ-AUTH-4`, `REQ-AUTH-6`, `REQ-AUTH-7`, `REQ-AUTH-8`, `REQ-AUTH-13`,
  `REQ-AUTH-15`, `REQ-AUTH-24`–`REQ-AUTH-26`, `REQ-AUTH-30`–`REQ-AUTH-32`,
  `AC-AUTH-3`, `AC-AUTH-6`, `AC-AUTH-19`–`AC-AUTH-24`)
- `docs/requirements/features/resolved-session-configuration.md` — frozen
  trusted binding, provenance, disabled capabilities, credential isolation,
  and historical reconstruction (`REQ-RSC-39`, `REQ-RSC-41`–`REQ-RSC-43`,
  `REQ-RSC-46`–`REQ-RSC-55`, `AC-RSC-12`–`AC-RSC-17`, `AC-RSC-20`,
  `AC-RSC-21`, `AC-RSC-23`–`AC-RSC-28`)
- `docs/requirements/features/submission-attempts.md` and
  `docs/ui-ux/flows/submission-attempt.md` — the committed Attempt/Session
  handoff, exact Submission binding, consumed entitlement, and Attempt history
- `docs/ui-ux/flows/text-session.md` — approved Participant and administrator
  journeys, interaction states, content, accessibility, responsive behavior,
  and `UI-SESS-DEC-1`–`UI-SESS-DEC-15`
- `docs/architecture/session-runtime-contract.md` — canonical Session command,
  ordering, timing, streaming, SSE/reconnect, terminalization, recovery, and
  quality contracts
- `docs/architecture/backend-module-architecture.md`,
  `docs/architecture/mvp-architecture.md`, and
  `docs/architecture/frontend-architecture.md` — module ownership, production
  host topology, request/response plus SSE, browser state ownership, and the
  current contract-unavailable route
- `docs/requirements/mvp-operational-defaults.md`,
  `docs/operations/provider-profiles/keycloak-oidc-contract.md`, and
  `docs/operations/provider-profiles/` — application-session/revocation bounds,
  authenticated gateway routing, protected-data lifecycle, provider
  qualification, credential isolation, and default-off production execution
- `docs/ui-ux/design-system/README.md` and
  `docs/ui-ux/design-system/implementation-guide.md` — Approved v1.1 authority,
  production-donor rules, and the `live-session` layout family
- Applicable design-system modules selected by the implementation guide:
  accessibility, colors, typography, layout, density, interaction states,
  motion, status, keys, inputs, alerts, error summary, modals, conversation,
  timeline, Agent presence, Session controls, empty/loading, and protected
  content
- `docs/contributing/development-harness.md` — canonical/candidate attach,
  synthetic OIDC sign-in, and Playwright MCP evidence rules
- `.work/active/p0-attempt-session-start.md` — predecessor boundary and exact
  committed Session locator; non-authoritative implementation state only

Before new production UI, classify the Participant route as the approved
`live-session` family and the administrator-operations and historical-
transcript routes as `management` nested records. Promote and adapt the
existing Component Deck/Design Lab live-session specimen for the Participant;
clone an accepted production record page plus the Component Deck management-
record specimen for the other two routes. Do not import Design Lab fixtures,
reducers, routes, or styles into production. No `$impeccable shape` work is
authorized by this plan because approved families and donors already exist.

# Dependency and activation gate

- Keep this task `planned` while `p0-attempt-session-start` is `in-progress`.
- Activate only after Attempt start and terminal Attempt mapping have completed
  their required backend, frontend, security/privacy, QA, authenticated-browser,
  and durable-state review; the predecessor must hand off one authorized,
  committed `active_session_id` without claiming a live client contract.
- At activation, refresh this inventory against the final predecessor code and
  remove or revise assumptions that no longer match the implemented seams.
- Production model execution remains fail-closed/default-off unless the exact
  frozen provider/deployment profile and credential binding have qualified
  evidence. Local/CI synthetic execution must not weaken that gate.
- Provider qualification does not block starting this implementation: use the
  deterministic provider and the approved synthetic-development profile only
  within their data boundaries. It does block any Production or real-
  Participant enablement claim until a separately owner-approved exact profile
  is qualified and explicitly enabled.
- `text-interaction-controller-contract` remains planned and unactivated. This
  task must not implement Interaction Controller signals, text preemption, or
  another concurrent Agent publication lane.

# Scope

## In

- Versioned, participant-safe Session snapshot, command-result, and hosted
  state-event/SSE contracts aligned across canonical JSON Schema, fixtures,
  OpenAPI, C#, and TypeScript.
- Authenticated gateway routing for the chosen Session HTTP namespace, with
  exact-path proxying, public-route negatives, streaming behavior preserved,
  and no broad `/v1` exposure.
- Authenticated production HTTP endpoints for authoritative Session snapshot,
  Participant message/send reconciliation, Participant completion, authorized
  administrator pause/resume/terminate, and safe command outcome recovery.
- Reuse of the existing `SessionRuntime`, trusted binding, Postgres
  repositories/coordinators, command handlers, required audit/outbox boundary,
  durable Invocation worker, timer lane, and production SSE endpoint.
- Server-derived permitted actions and actor-specific projections. Participant
  snapshot content includes only authorized identity, lifecycle/time facts,
  exact bound Submission summary, participant-visible transcript/activity,
  command reconciliation facts, and safe recovery categories. Administrator
  operations remain a separate minimized projection; raw transcript requires
  its distinct sensitive-content capability. An assigned Reviewer or other
  currently authorized terminal-history actor receives a separate read-only
  terminal projection that validates the active assignment/capability and
  exposes no Evaluation, Review decision, Result, or Release state.
- Add the production hosted event projection required for the approved UI state
  tracks: lifecycle, authoritative time/warnings, Participant message
  admission, Agent work/no-action, durable Agent fragments/completion, terminal
  cutoff, and access/reconciliation signals. Snapshot remains the baseline;
  SSE is a committed-delta transport, not a second source of truth. Preserve
  the current unversioned SSE route and closed v1 wire behavior as a
  compatibility path rather than extending it incompatibly.
- Production Participant Text Session route using the approved `live-session`
  family: entry/restore, Agent identity, authoritative time, exact Submission
  summary, ordered transcript, local composer, pending/reconciling send,
  durable streaming, work/no-action/failure states, reconnect/offline,
  pause/permission change, completion confirmation/finalizing, and terminal
  transcript.
- A separately authorized administrator Session-operations route for
  bounded operational state and deliberate pause/resume/terminate, without
  automatically loading transcript or Submission content.
- A separately authorized, read-only terminal-transcript entry for an assigned
  Reviewer and other policy-permitted historical actors, without implementing
  the Review-case workflow.
- Additive persistence changes only when current durable state cannot support
  an approved snapshot, command, idempotency, timing, or projection invariant.
- Bounded observability for HTTP commands, snapshot/reconnect latency,
  projection lag/gaps, SSE revalidation, command reconciliation, Session
  terminalization, and work backlog without raw Participant content or
  unrestricted identifiers.
- Full requirement-to-test traceability, focused red-green-refactor evidence,
  Postgres concurrency/fault tests, production-host negative tests, authenticated
  end-to-end coverage, and Playwright MCP accessibility/visual evaluation.

## Out

- Attempt eligibility, acknowledgment, entitlement, start transaction, or
  retry-entitlement creation owned by the predecessor and Submission/Attempt
  specifications.
- A new Session aggregate, a client-authored Session state machine, direct
  writes to Session-owned tables from the API, or a second transcript/event
  ordering authority.
- General Agent/Harness authoring, model/provider selection UI, production
  provider enablement without qualification, or credential entry/display.
- Evidence selection, Evaluation creation, Human revision, Review decision,
  Review-case queue/composition, Result construction, Release, appeal, or any
  score/pass/fail presentation. The assigned-Reviewer terminal-transcript entry
  is only the Session-owned read boundary already required by the Text Session
  specification.
- Voice, playback, tools, external retrieval, Dynamic memory, learning,
  shared/multi-Participant Sessions, arbitrary timers, Interaction Controller
  behavior, or richer Decision/output kinds.
- Offline message queues, automatic resend after an uncertain response,
  automatic external-link fetching, accepted-message editing/deletion, or
  reopening a terminal Session.
- Copying Design Lab fixtures, simulation controls, reducer state, route code,
  or lab-only component/style imports into the production entry graph.
- Commits, pushes, pull requests, deployment, or release unless separately
  requested.

# Current implementation inventory

- Session domain, persistence, lifecycle, durable Invocation work, timer,
  fragment publication/sealing, terminal manifest/handoff, and replay
  coordinators already exist under `src/Modules/Sessions/` with substantial
  unit and PostgreSQL coverage.
- Canonical `Session*CommandV1` C# records and
  `contracts/schemas/v1/session/command-envelope.v1.schema.json` already define
  the six approved command variants. They are not mapped to a production
  command endpoint.
- The production API exposes only `GET /sessions/{sessionId}/events`. Its
  current authorized projection covers Agent response fragments and completion,
  not the full Participant snapshot/state feed required by the approved UI.
- The canonical `SessionStateEventEnvelopeV1` and deployed
  `SseSessionEventV1` are distinct closed schemas with different event/payload
  coverage. Neither is sufficient for the full hosted UI state feed, and new
  variants must not be inserted into the existing closed v1 contract without a
  compatibility decision and proof.
- The authenticated Compose gateway proxies `/v1/assessment`,
  `/v2/assessment`, and the existing Session SSE path, but not
  `/v1/sessions`. Without an explicit narrow gateway/configuration update, the
  planned snapshot and command paths would fall through to the SPA.
- The production route `/sessions/:sessionId` intentionally renders a
  contract-unavailable management ceremony. Frontend architecture likewise
  records snapshot and command HTTP as missing.
- `ProductionAppShell` currently rejects a `live-session` assignment even
  though the family ID is production-approved; the route manifest, shell
  switch, production layout implementation, and denial/loading behavior must
  move together.
- `LiveSessionLayout`, the Session journey composition, and synthetic state
  examples exist in Design Lab as approved visual/composition donors only.
  Production has no Session page, API client, query/reducer, or authenticated
  browser journey.
- Worker composition already supports durable Invocation processing and timer
  polling behind workload-identity and exact provider-qualification gates.
  Production execution must continue to fail closed when those gates are not
  satisfied.
- No canonical participant-safe Session snapshot schema or production command
  outcome schema is present in the contract catalog. Those transport contracts
  are the first implementation boundary, not a missing product specification.

# Requirement-to-surface map

| Requirement / acceptance target | Implementation surfaces | Required evidence |
| --- | --- | --- |
| `REQ-SESS-1`, `REQ-SESS-5`–`REQ-SESS-7`, `AC-SESS-2`; applicable `REQ-AUTH-4`, `REQ-AUTH-7`, `REQ-AUTH-8`, `REQ-AUTH-13`, `REQ-AUTH-24`, `REQ-AUTH-30` | Actor-specific snapshot query, trusted Session binding/ownership loader, production identity adapter, current relationship and commit-time authorization | Signed-out, wrong role, guessed ID, wrong Organization/Activity/cohort/Participant/Attempt/Session, stale/revoked relationship, malformed locator, and non-disclosing denial tests |
| `REQ-SESS-8`–`REQ-SESS-19`, `REQ-SESS-51`–`REQ-SESS-70`, `REQ-SESS-78`–`REQ-SESS-85`; `AC-SESS-3`–`AC-SESS-8`, `AC-SESS-31`–`AC-SESS-37`, `AC-SESS-42`–`AC-SESS-48` | Command HTTP adapter, existing message-admission/trigger/Invocation/Decision/fragment coordinators, worker, snapshot and event projectors, transcript assembler, frontend command/reconciliation reducer | Red contract tests; equivalent/mismatched idempotency, concurrent tabs, one visible publisher, no-action, rejection, timeout before/after visibility, duplicate/gap/digest/cutoff, unsafe content, disabled output/action, restart and replay tests |
| `REQ-SESS-71`–`REQ-SESS-77`, `AC-SESS-38`–`AC-SESS-41`; `AC-RSC-27` | Existing one-lane timer scheduler/worker and minimized state/event projection | Enabled/disabled frozen policy, one pending revision, pause/resume, cutoff, duplicate fire, restart, and no internal timer/request leakage to Participant UI |
| `REQ-SESS-20`–`REQ-SESS-30`, `AC-SESS-9`–`AC-SESS-14`; `REQ-OPS-12`–`REQ-OPS-14` | Authoritative snapshot time projection, lifecycle coordinator, warning/timer events, SSE/poll/reconcile path, application-session revalidation, frontend last-confirmed timer model | Disconnect/reconnect, stale/future cursor, missed warning, exact expiry boundary, client-clock tampering, pause interval, multiple device, application-session expiry, and revocation within 60 seconds |
| `REQ-SESS-31`–`REQ-SESS-41`, `AC-SESS-15`–`AC-SESS-20`, `AC-SESS-28`; `AC-RSC-17`, `AC-RSC-23`; `AC-AUTH-22` | Participant complete and administrator lifecycle command adapters, Session/Attempt terminal transaction, ordered manifest append/seal, audit/outbox, snapshot/terminal projection, completion dialogs | Duplicate/lost/stale complete, pause/message/expiry/terminate races, concurrent manifest append, audit/seal/Attempt-map rollback, late callback, immutable cutoff, no restored entitlement, and terminal UI evidence |
| `REQ-SESS-42`–`REQ-SESS-50`, `AC-SESS-21`–`AC-SESS-23`, `AC-SESS-30`; `REQ-RSC-39`, `REQ-RSC-41`–`REQ-RSC-43`, `REQ-RSC-46`–`REQ-RSC-55`; `AC-RSC-12`–`AC-RSC-17`, `AC-RSC-20`, `AC-RSC-21`, `AC-RSC-23`–`AC-RSC-28`; `REQ-AUTH-6`, `REQ-AUTH-32`, `AC-AUTH-6`, `AC-AUTH-23`, `AC-AUTH-24` | Participant, administrator, assigned-Reviewer, service, and audit actor-specific history/snapshot projections; exact frozen binding and Submission summary; protected content references; terminal transcript read; provider/credential gate | Changed source/Submission invariance, unavailable content, current/revoked historical relationship, wrong/missing review assignment, sensitive-value redaction, disabled voice/tool/memory/shared Session, audit-content non-duplication, and credential failure tests |
| `AC-SESS-24`–`AC-SESS-26`; `AC-AUTH-20`; `UI-SESS-DEC-1`–`UI-SESS-DEC-15` | Production `live-session` Participant layout; separate `management` Session-operations and terminal-record layouts; semantic transcript/activity regions, composer, status/time, modal completion, responsive styles | Component tests plus authenticated Playwright accessibility snapshots and desktop/narrow screenshots for loading, active, pending, streaming, no-action, retry, offline/reconnect, paused, warning, finalizing, terminal, assigned-review read, denial, focus, reduced-motion, forced-colors, and 400% reflow states |
| `AC-SESS-27`, `AC-SESS-29`, `AC-SESS-32`; `REQ-AUTH-15`, `REQ-AUTH-25`, `REQ-AUTH-30`, `REQ-AUTH-31`; `AC-AUTH-21`, `AC-AUTH-22` | Request/SSE/worker telemetry, bounded snapshot/replay, gateway and cache/query isolation, negative suite and operations gates | Measured 2-second p95 admission/reconnect objectives under approved exclusions, bounded transcript/event paging, backlog/backpressure, projection-isolation, public-route and wrong-scope matrices, log/metric sensitivity inspection, and full repository gates |

# Plan

- [ ] After the dependency gate clears, refresh the implementation inventory and
  reconcile the predecessor handoff, Session/Attempt terminal mapping, API
  identity composition, migrations, worker gates, route layout, and current
  test coverage against this plan. Stop and update the owning specification
  only if a participant-visible, security-sensitive, or architecture-changing
  ambiguity is discovered.
- [ ] Contract red: add failing catalog, canonical-schema, fixture, OpenAPI, C#
  mapping, and TypeScript parity tests for an actor-specific Session snapshot,
  stable command outcome, and complete hosted UI state-event/SSE envelope.
  Preserve the approved command envelope and existing closed v1 event wires;
  reject unknown versions/variants/fields, route/body Session mismatch, unsafe
  identifiers, invalid int64 wire values, and protected internal fields.
- [ ] Contract green: implement the smallest versioned transport contracts. Use
  the existing `/v1` API namespace for snapshot and command HTTP so SPA document
  navigation at `/sessions/:sessionId` cannot collide with an API GET. Add the
  hosted stream at `/v1/sessions/{sessionId}/events` under the same actor-safe
  versioned contract, while keeping `/sessions/{sessionId}/events` compatible
  for current consumers. Add gateway handling for the exact `/v1/sessions`
  base and slash-delimited descendants, including SSE no-buffer behavior,
  authenticated-browser configuration/profile updates, and public-route,
  near-prefix, and proxy tests; do not expose all of `/v1`. Treat these paths
  as a reversible transport decision, not product meaning.
- [ ] Backend query red/green: create a Sessions-owned participant-safe snapshot
  projector and query coordinator over the trusted binding and canonical
  runtime. Derive permitted actions, lifecycle/time facts, transcript/activity,
  exact Submission summaries, and recovery categories on the server; paginate
  bounded older history without reordering or fabricating a cutoff. Add a
  separate minimized administrator projection and an assigned-Reviewer/
  historical-actor terminal projection behind separately gated transcript
  read.
- [ ] Backend command red/green: map the approved message, reconcile, complete,
  pause, resume, and terminate envelopes through thin production HTTP adapters
  to the existing Sessions application/infrastructure coordinators. Use opaque
  application-session identity, action/resource/relationship authorization at
  admission and commit, antiforgery for browser mutations, body/rate limits,
  `no-store`, safe status categories, expected Session version, scoped
  idempotency, required audit/outbox, and non-disclosing `404` denial.
- [ ] Realtime red/green: implement the authorized hosted Session event
  projection and `/v1/sessions/{sessionId}/events` mapping only for committed
  UI-relevant deltas missing from the snapshot contract. Preserve cursor
  validation, bounded replay/paging, duplicate suppression, gap reconciliation,
  application-session/relationship revalidation, and terminal cutoff. Keep the
  current unversioned SSE route and v1 projection regression-green. Never stream
  hidden prompts, raw Decision envelopes, provider diagnostics, internal timer
  requests, or another actor's content.
- [ ] Worker/runtime integration: verify end to end that an accepted Participant
  message admits one trusted trigger/Invocation, qualified worker processing
  publishes only durable fragments, intentional no-action resolves explicitly,
  and provider/audit/persistence failure preserves accepted input and an honest
  recovery state. Keep tools, voice, Dynamic memory, richer outputs, and
  unqualified production providers disabled.
- [ ] Persistence and concurrency: add only required additive migration(s),
  constraints, indexes, and projections discovered by the contract tests.
  Prove transaction participation, immutable transcript/terminal history,
  Session-local ordering, one visible response publisher, message/command
  idempotency, timer/lifecycle races, Attempt mapping, process-loss recovery,
  and upgrade/rollback safety with PostgreSQL tests and fault injection.
- [ ] Frontend shell red/green: promote/adapt the approved `LiveSessionLayout`
  from the Component Deck/Design Lab donor into production-safe design-system
  code, add its production route-layout assignment, and replace the
  contract-unavailable Session route. Import no Design Lab code or fixtures;
  preserve semantic order and the approved desktop/narrow family behavior.
  Keep `/sessions/:sessionId` as the Participant `live-session` route. Add
  `/sessions/:sessionId/operations` and `/sessions/:sessionId/transcript` as
  separate `management` nested-record routes cloned from an accepted production
  record page plus the Component Deck management-record specimen; neither route
  may inherit Participant live controls.
- [ ] Frontend state red/green: add typed production Session API/SSE clients,
  TanStack Query snapshot ownership, and an explicit reducer for committed
  deltas and ephemeral local states. Implement the six independent state
  tracks, local draft, pending/checking admission, authoritative transcript,
  durable streaming/no-action/failure, last-confirmed time, reconnect/offline,
  pause/access change, deliberate completion/finalizing, terminal transcript,
  and safe same-actor draft retention. Never infer success, resend blindly, or
  treat client time/cache/SSE receipt as authority. Query owns snapshot fetch,
  cancellation, isolation, and reconciliation transport only; each successful
  authoritative snapshot replaces the reducer baseline, and the reducer alone
  owns the materialized live view plus ordered deltas and ephemeral UI state.
  Do not mirror an evolving Session projection into both Query cache and reducer.
- [ ] Administrator operations red/green: implement the separate minimized
  operational view and deliberate pause/resume/terminate confirmations with
  bounded reasons, uncertain-outcome reconciliation, current permitted actions,
  and no automatic transcript/Submission load. Verify Participant and Reviewer
  roles cannot inherit control or sensitive-content access.
- [ ] Terminal history red/green: implement the read-only historical entry for
  the Participant on the terminal live route and for an assigned Reviewer or
  other explicitly permitted actor on the separate transcript route. Require
  current relationship, assignment/capability, workflow visibility, and
  lifecycle policy on every load; render unavailable ordered items honestly
  and expose no live controls, Evaluation, Review decision, Result, or Release.
- [ ] Operations and observability: add bounded metrics, logs, traces, health,
  and alertable failure categories for command admission/commit, snapshot and
  replay latency, SSE gaps/revocation, lifecycle/terminalization, durable work,
  fragment publication, and reconciliation. Inspect outputs and browser state
  for raw transcript, draft, Submission content, prompts, credentials,
  unrestricted identifiers, and authorization internals.
- [ ] Run focused contract, domain, application, architecture, runtime,
  PostgreSQL, Worker, web component, and browser tests, then `pnpm verify:web`,
  `pnpm verify:dotnet`, `pnpm verify:supply-chain`, `pnpm verify:oci`,
  `python3 scripts/check_docs.py`, and `pnpm verify:oidc` when the documented
  environment is available. Run `pnpm verify:oidc` only in its isolated
  documented harness, never against a user-owned healthy stack selected for
  candidate UI review. Record exact commands, counts, exit status, and blockers;
  do not convert unavailable integration infrastructure into a pass.
- [ ] Attach to a healthy candidate UI/API profile without reseeding or
  replacing an existing stack. Through real synthetic OIDC interactions verify
  committed handoff, authorized restore/deep link, send and lost-response
  reconciliation, streaming and incomplete output, no-action, timer-triggered
  output, warnings/expiry, reconnect/offline, multi-tab update, pause/resume,
  completion, termination/abort, terminal transcript, administrator controls,
  current-access denial, and protected-content boundaries. Capture accessibility
  snapshots and desktop/narrow, keyboard-focus, dialog/error, light/dark,
  reduced-motion, forced-colors, and 400% reflow screenshots under
  `.playwright-mcp/` and evaluate them before completion.
- [ ] Request distinct backend, frontend, security/privacy, and QA reviews.
  Resolve every blocking finding, rerun affected evidence, reconcile actual
  behavior with all governing sources, update `docs/current-state.md` and any
  current architecture/contract owner only to the demonstrated boundary, mark
  this task completed for review, then retire it after durable truth is
  promoted and review is complete.

# Current state

Planned and dependency-gated. Product meaning, observable Session behavior,
interaction design, and runtime architecture are already approved; no new
feature specification is required before implementation.

The first work after activation is a final seam inventory followed by
contract-first red tests for the missing participant-safe snapshot and command
outcome. Do not begin by copying the synthetic Session route or Design Lab
reducer into production.

# Decisions

- Reuse the existing Sessions domain and persistence owners. The production
  host authenticates, validates, maps, and composes; it does not become a
  second Session policy or state owner.
- Use request/response for snapshot and command outcomes plus the existing SSE
  connection for committed deltas. Every reconnect starts by reconciling
  authoritative state; neither SSE delivery nor browser cache is acceptance.
- Use `/v1/sessions/{sessionId}` for the snapshot and
  `/v1/sessions/{sessionId}/commands` for command submission, and
  `/v1/sessions/{sessionId}/events` for the complete hosted UI event stream as
  the interim transport realization. This avoids the existing SPA document
  route and leaves `/sessions/{sessionId}/events` as a compatible existing v1
  path. The gateway must proxy only the exact base and slash-delimited
  descendants, with SSE buffering disabled on the hosted event path. Contract
  tests must bind route and envelope locators to the same authoritative Session.
- Use `/sessions/:sessionId` for the Participant live/terminal route,
  `/sessions/:sessionId/operations` for administrator control, and
  `/sessions/:sessionId/transcript` for separately authorized read-only
  historical access. The latter two use the production `management`
  nested-record family; route separation prevents role-dependent outer-shell
  selection and accidental live-control inheritance.
- Return actor-specific projections and server-derived permitted actions. Do
  not ship a universal snapshot containing fields hidden by the client.
- Keep Participant Session and administrator operations as separate
  compositions and authorization actions. Transcript access is not implied by
  operational control.
- Promote/adapt the existing approved live-session layout family before using
  it in production. Design Lab supplies visual structure only, never runtime
  state, copy authority, fixtures, or production imports.
- Keep browser state ownership singular: Query coordinates authoritative
  snapshot requests and protected-cache lifecycle; a snapshot replaces the
  live reducer baseline; only the reducer materializes subsequent committed
  deltas and local transient state.
- Keep production provider execution fail-closed/default-off until exact-profile
  qualification, credential binding, and workload delegation are satisfied.
  Hosted HTTP/UI availability must not silently enable model execution.
- Do not depend on unapproved `PROP-9` timer guidance for public behavior. Use
  the already approved one-lane timer semantics and implemented frozen bounds;
  expose no internal scheduling state to the Participant.

# Findings / deviations

- The missing boundary is transport and composition, not product meaning:
  command variants, runtime state, Postgres coordinators, Worker processing,
  and SSE replay exist, while the production snapshot/command host and SPA are
  absent.
- Current production SSE projects only Agent fragments and Agent completion.
  It cannot by itself restore lifecycle, timing, Participant messages,
  no-action, warning, or terminal state required by the approved Session UI.
- The current production Session route is intentionally assigned to the
  management shell and displays an unavailable ceremony. Activation must
  establish the approved `live-session` production family before the route is
  considered implemented.
- The Design Lab Session reducer and fixtures are simulation evidence only.
  Production state must be derived from the canonical snapshot, stable command
  outcomes, and authorized committed events.
- Exact production provider qualification remains independent of host/UI
  implementation. A runnable synthetic local/CI journey is not evidence that
  Production may process Participant content through a provider.
- The initial route decision assumed the authenticated gateway already proxied
  `/v1/sessions`; it does not. The corrected plan includes the narrow gateway,
  profile, and negative-test update rather than allowing those requests to fall
  through to the SPA or broadening the whole `/v1` prefix.
- The initial scope omitted the assigned-Reviewer terminal-transcript entry
  required by the approved Text Session interaction specification and
  `AC-SESS-30`. The corrected plan adds only the Session-owned read boundary,
  not Evaluation or Review workflow behavior.
- Participant live Session, administrator operations, and assigned historical
  transcript cannot safely share one role-dependent outer route because route
  layout selection is path-owned. The corrected plan assigns distinct paths
  and approved donors.
- The initial frontend step could be read as dual Query/reducer ownership of
  live Session state. The corrected plan makes snapshot replacement and live
  reducer authority explicit.
- The initial realtime step assumed the limited existing SSE v1 contract could
  carry every hosted UI delta while remaining compatible. The corrected plan
  adds a versioned hosted event path/contract and retains the current
  unversioned stream as a regression-protected compatibility surface.
- Administrator operations and participant-visible terminal history are
  approved P0 behavior and are included here; omitting them would leave
  `AC-SESS-12`, `AC-SESS-13`, `AC-SESS-15`–`AC-SESS-20`, and `AC-SESS-30`
  knowingly incomplete.

# Open questions

No material product, UX, or runtime-architecture question is currently open.
The approved specifications and current architecture owners define the
observable behavior and trust boundaries. Route names, response DTO factoring,
pagination mechanics, and internal projector types are reversible
implementation details within those constraints.

If activation discovers a missing participant-visible state, a change to
authorization or retention, a new durable owner, a widened capability, or an
incompatible transport requirement, stop implementation and record the
question with an interim default and rationale in the owning authoritative
artifact; add a `Proposed`/`PROP-*` item when consequential.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Governing product/requirements/UI/architecture inventory | complete | Approved Text Session requirements v0.5, UI specification v1.0, runtime contract v0.5, design system v1.1, and current implementation seams reviewed 2026-09-02 |
| Predecessor dependency and scope boundary | complete for planning | `p0-attempt-session-start` explicitly hands off the committed Session locator and keeps hosted snapshot/commands unavailable; implementation activation remains gated |
| Existing contract/runtime/host/frontend seam inventory | complete for planning | Command envelope C#/schema present; production SSE only; snapshot/command host and production Session page absent; Design Lab donor identified |
| Requirement/AC-to-surface and risk mapping | complete for planning | Requirement-to-surface table in this task covers Participant, administrator, assigned Reviewer/historical actors, backend, frontend, security/privacy, operations, and QA boundaries |
| `python3 scripts/check_docs.py` | complete | Passed 2026-09-02; feature-catalog and documentation validation succeeded |
| `git diff --check` | complete | Passed 2026-09-02; direct trailing-whitespace scan of this new untracked task file also passed |
| Cross-cutting plan consistency/readiness review | complete | Second pass on 2026-09-02 applied backend, frontend, and security/privacy review perspectives to scope, authority, predecessor ownership, requirement coverage, gateway topology, event-contract compatibility, actor-specific historical access, frontend state ownership, UI route/donor rules, provider gates, and verification ordering; five readiness gaps were corrected, while distinct implementation reviews remain required during execution |
| Implementation tests and live-browser evidence | pending | Dependency-gated; execute only after activation |

# Blockers

- Activation is blocked until `p0-attempt-session-start` completes and its
  required review verifies committed Session handoff, exact binding,
  production-safe source/model gates, and mutation-coupled Session/Attempt
  terminalization. This does not block planning or plan review.
- Production or real-Participant model enablement remains separately blocked by
  exact-profile qualification and explicit owner approval. This does not block
  contract, host, SPA, deterministic-provider, or synthetic-development
  implementation, but it does block a Production rollout or real-data claim.

# Readiness review

The task is ready to activate once the dependency gate clears if the final seam
inventory confirms the facts above. No new feature specification or Product
Lead decision is required. Implementation has:

- approved product scope and explicit deferred/non-goal boundaries;
- stable requirement, acceptance, UX-decision, architecture, and operations
  owners;
- an identified predecessor output and activation gate;
- a contract-first incremental order with backend/frontend ownership;
- explicit transaction, ordering, idempotency, authorization, isolation,
  audit, provider, and recovery boundaries;
- a production UI donor and approved layout family without a design gap;
- negative, concurrency, fault, accessibility, responsive, and end-to-end
  verification targets; and
- distinct implementation and independent-review stages.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [ ] Governing specifications were rechecked
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
