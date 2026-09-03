---
id: hosted-text-session
status: in-progress
created: 2026-09-02
updated: 2026-09-03
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
- The predecessor now supplies a scoped committed `active_session_id`, a
  **Continue Attempt** link to the honest unavailable Session route,
  digest-verified immutable notice projections, transactional exact-version
  reads, and mutation-coupled Session-to-Attempt terminal mapping. Treat those
  as implemented entry seams; do not reimplement them here.
- Activate only after the predecessor reconciles its remaining Participant UX,
  timing/source-integrity, full-regression/CI, and review evidence; reaches
  `status: completed`; and is safe for retirement under `.work/README.md`.
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
- Authenticated-browser Compose integration for the existing Worker with
  bounded workload identity/delegation, health/readiness, deterministic test
  execution, and optional approved synthetic-development execution. Do not
  place provider credentials in Compose YAML, images, logs, fixtures, or
  browser-visible configuration.
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
  family: entry/restore, Agent identity and core animation, authoritative
  remaining-time projection plus examiner chrono, exact Submission
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
  shared/multi-Participant Sessions, arbitrary extra timers, Interaction Controller
  behavior, or richer Decision/output kinds. The approved Participant remaining-
  time projection and chrono display are in scope. The internal Agent next-timer
  lane and unapproved `PROP-9` cooldown/duplicate-suppression stay out. Expiry
  as an idempotent hosted terminal command remains a gap.
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
- Attempt start now persists and returns the Participant's scoped committed
  Session locator, and production My Work links **Continue Attempt** to
  `/sessions/{sessionId}` while that destination remains honestly unavailable.
- Digest-verified notice projection sets and the narrow
  `ISessionAttemptTerminalSink`/Submissions terminal-mapping adapter are now
  committed prerequisites. The hosted task must consume and regression-protect
  those seams, not create competing notice or Attempt state owners.
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
- The authenticated-browser Compose profile does not currently start the
  Worker. API/UI hosting alone therefore cannot prove the accepted-message to
  durable-Agent-output journey until this task adds and verifies that service.
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

- [x] Independent review follow-up (keep `in-progress`; do not retire):
  reconstruct hosted timing as unbounded | timed | unavailable; apply
  `EffectiveTimingEvaluator` duration at Session start (`REQ-SESS-20`);
  fail closed when a timed Session has no frozen warning schedule
  (`REQ-SESS-24`, `PROP-6`); do not map missing/corrupt/`unbounded` onto
  45 minutes. Then permit UUID Session locators on the canonical command
  envelope and seed transcript reveal as already complete. Pause-interval
  persistence stays. Expiry/warning-emission/multi-tab/offline/terminate-live
  /forced-colors/400% evidence stays open.
- [x] Review-driven correctness pass (keep `in-progress`; do not retire):
  authoritative timing from frozen Attempt/Activity duration plus accumulated
  pause intervals and configured warnings (`REQ-SESS-20`–`24`, `PROP-2`,
  `PROP-6`); authoritative `unavailable` wins over local transcript;
  persist/audit `session.terminate.v1` `reason_code`; hosted HTTP commands
  validate against the closed canonical envelope; fix Web
  `react-hooks/set-state-in-effect` CI failures; add focused P1 regressions.
  Missing expiry/warning/multi-tab/offline/terminate-live/forced-colors/400%
  evidence stays open.
- [x] After the dependency gate clears, refresh the implementation inventory and
  reconcile the predecessor handoff, Session/Attempt terminal mapping, API
  identity composition, migrations, worker gates, route layout, and current
  test coverage against this plan. Stop and update the owning specification
  only if a participant-visible, security-sensitive, or architecture-changing
  ambiguity is discovered.
- [x] Contract red: add failing catalog, canonical-schema, fixture, OpenAPI, C#
  mapping, and TypeScript parity tests for an actor-specific Session snapshot,
  stable command outcome, and complete hosted UI state-event/SSE envelope.
  Preserve the approved command envelope and existing closed v1 event wires;
  reject unknown versions/variants/fields, route/body Session mismatch, unsafe
  identifiers, invalid int64 wire values, and protected internal fields.
- [x] Contract green: implement the smallest versioned transport contracts. Use
  the existing `/v1` API namespace for snapshot and command HTTP so SPA document
  navigation at `/sessions/:sessionId` cannot collide with an API GET. Add the
  hosted stream at `/v1/sessions/{sessionId}/events` under the same actor-safe
  versioned contract, while keeping `/sessions/{sessionId}/events` compatible
  for current consumers. Add gateway handling for the exact `/v1/sessions`
  base and slash-delimited descendants, including SSE no-buffer behavior,
  authenticated-browser configuration/profile updates, and public-route,
  near-prefix, and proxy tests; do not expose all of `/v1`. Treat these paths
  as a reversible transport decision, not product meaning.
- [x] Backend query red/green: create a Sessions-owned participant-safe snapshot
  projector and query coordinator over the trusted binding and canonical
  runtime. Derive permitted actions, lifecycle/time facts, transcript/activity,
  exact Submission summaries, and recovery categories on the server; paginate
  bounded older history without reordering or fabricating a cutoff. Add a
  separate minimized administrator projection and an assigned-Reviewer/
  historical-actor terminal projection behind separately gated transcript
  read.
- [x] Backend command red/green: map the approved message, reconcile, complete,
  pause, resume, and terminate envelopes through thin production HTTP adapters
  to the existing Sessions application/infrastructure coordinators. Use opaque
  application-session identity, action/resource/relationship authorization at
  admission and commit, antiforgery for browser mutations, body/rate limits,
  `no-store`, safe status categories, expected Session version, scoped
  idempotency, required audit/outbox, and non-  disclosing `404` denial.
- [x] Realtime red/green: implement the authorized hosted Session event
  projection and `/v1/sessions/{sessionId}/events` mapping only for committed
  UI-relevant deltas missing from the snapshot contract. Preserve cursor
  validation, bounded replay/paging, duplicate suppression, gap reconciliation,
  application-session/relationship revalidation, and terminal cutoff. Keep the
  current unversioned SSE route and v1 projection regression-green. Never stream
  hidden prompts, raw Decision envelopes, provider diagnostics, internal timer
  requests, or   another actor's content.
- [x] Worker/runtime integration: add the existing Worker to the authenticated-
  browser Compose profile with the documented image, migration dependency,
  database connectivity, bounded workload identity/delegation, health/readiness,
  and deterministic provider configuration. Verify end to end that an accepted
  Participant message admits one trusted trigger/Invocation, worker processing
  publishes only durable fragments, intentional no-action resolves explicitly,
  and provider/audit/persistence failure preserves accepted input and an honest
  recovery state. Exercise the approved synthetic-development profile only
  under its explicit opt-in and data boundary. Keep tools, voice, Dynamic
  memory, richer outputs, credentials in Compose, and unqualified Production
  providers disabled.
- [-] Persistence and concurrency: add only required additive migration(s),
  constraints, indexes, and projections discovered by the contract tests.
  Prove transaction participation, immutable transcript/terminal history,
  Session-local ordering, one visible response publisher, message/command
  idempotency, timer/lifecycle races, Attempt mapping, process-loss recovery,
  and upgrade/rollback safety with PostgreSQL tests and fault injection.
  Existing Session runtime migrations already persist participant
  relationships at start; no new migration was required. Admin operations
  resolve via current relationship or an org `session.operations.read` grant
  bound to a Session that exists in that organization.
- [x] Frontend shell red/green: promote/adapt the approved `LiveSessionLayout`
  from the Component Deck/Design Lab donor into production-safe design-system
  code, add its production route-layout assignment, and replace the
  contract-unavailable Session route. Import no Design Lab code or fixtures;
  preserve semantic order and the approved desktop/narrow family behavior.
  Keep `/sessions/:sessionId` as the Participant `live-session` route. Add
  `/sessions/:sessionId/operations` and `/sessions/:sessionId/transcript` as
  separate `management` nested-record routes cloned from an accepted production
  record page plus the Component Deck management-record specimen; neither route
  may inherit Participant live controls.
- [x] Remaining-time red/green: project `active_duration` remaining seconds
  from `session_runtimes.created_at` and the Proposed 45-minute synthetic
  development budget when Activity duration is absent. Seat Design Lab chrono
  digits/gauge and Submit Session on the examiner; keep Agent core animation;
  Enter sends immediately and Shift+Enter inserts a newline (Design Lab
  examination-console keys; Proposed override of `UI-SESS-DEC-4`). Seat
  CompactId for the Session locator, the seated operator display name on
  Participant, and a wider Agent status line with an overflow plaque
  (production and Design Lab). Do not enable the Agent timer
  lane or invent warning/expiry behavior.
- [x] Frontend state red/green: add typed production Session API/SSE clients,
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
- [x] Administrator operations red/green: implement the separate minimized
  operational view and deliberate pause/resume/terminate confirmations with
  bounded reasons, uncertain-outcome reconciliation, current permitted actions,
  and no automatic transcript/Submission load. Verify Participant and Reviewer
  roles cannot inherit control or sensitive-content access.
- [x] Terminal history red/green: implement the read-only historical entry for
  the Participant on the terminal live route and for an assigned Reviewer or
  other explicitly permitted actor on the separate transcript route. Require
  current relationship, assignment/capability, workflow visibility, and
  lifecycle policy on every load; render unavailable ordered items honestly
  and expose no live controls, Evaluation, Review decision, Result, or Release.
- [x] Operations and observability: hosted HTTP/SSE emit
  `session.hosted.snapshot`, `session.hosted.command`, and
  `session.hosted.subscribe` outcome+duration labels only (spaces in labels
  collapse to `unknown`). Existing Session runtime telemetry remains the
  work/fragment/backlog owner. Logs do not include transcript text or Session
  UUIDs.
- [x] Focused hosted tests plus `pnpm verify:web` (exit 0; production 638,
  design-lab 206, e2e 11), `pnpm verify:dotnet` (exit 0; 1840 succeeded, 3
  skipped), `pnpm verify:supply-chain` (exit 0), `pnpm verify:oci` (exit 0),
  `python3 scripts/check_docs.py` (passed), `git diff --check` (passed).
  `pnpm verify:oidc` skipped against this user-owned healthy stack.
- [x] Playwright on candidate `:5274` with healthy Compose `:18080`: Continue
  Attempt, send after fail-closed recovery, guessed-id denial, participant
  operations without pause/terminate, administrator pause/resume confirmations,
  administrator transcript route without live controls. Desktop and 390px
  screenshots captured locally. API RedirectUri restored to canonical after
  candidate admin sign-in.
- [x] Fix examiner work-state lingering after Agent complete and same-Session
  send conflicts caused by Worker version bumps the SPA never saw.
- [x] Same-Session follow-up send: hold Transmit while Agent work is
  queued/working or snapshot sync is in flight; on `trigger.admission.stale.version`
  refetch once and retry the same message on this Session (not a new Session).
- [x] Authoritative remaining `0`: close send/submit, show Checking then
  **Time ended. Session completed** (not a Result), stage 2 of 2. Reconcile
  seals Active/Paused Sessions via `begin_completing` + `complete` with
  `time_expiry` (`REQ-SESS-33`, `AC-SESS-16`, `PROP-5`, `UI-SESS` expiry).
- [ ] Request distinct backend, frontend, security/privacy, and QA reviews.
  `docs/current-state.md` is not updated in this slice; promote only after
  those reviews. Keep this file until review and durable-truth promotion.

# Current state

Expiry boundary: authoritative remaining `0` closes send/submit, moves
stage to 2 of 2, shows Checking then **Time ended. Session completed**
(not a Result). Reconcile on that boundary seals Active/Paused via
`time_expiry` (`PROP-5` Completed). Live `:5274` Session `01a06512-…ae7d4c`
became `completed`; composer closed; Return to assignment present.
RedirectUri restored to canonical `:18080`. Reload of an expiry-completed
Session still uses the generic complete plate until `terminal_reason` is
projected. Worker due-scan of remaining `0` without an open console is not
in this slice.

Same-Session follow-up send no longer surfaces
"This Session record was updated" on the happy path. Transmit stays held
while Agent work is queued/working and while the post-accept snapshot
refetch is in flight. A single `trigger.admission.stale.version` conflict
refetches then retries the same message on the same Session locator.

Live candidate `:5274` Attempt 2 Session `01a0654c-…ef4851`: first send
accepted; immediate second send 409 then 200 retry; transcript kept both
turns; examiner returned to awaiting. RedirectUri restored to canonical
`:18080`. Prior Session `01a06512-…ae7d4c` is time-budget exhausted and
rejects further sends (`trigger.admission.budget.exhausted`).

Independent review follow-up is in place; task stays `in-progress`. Hosted
timing reconstruction now distinguishes `unbounded` (timer disabled), timed
effective duration (cohort baseline plus `EffectiveTimingEvaluator` at
Session start), and unavailable (missing/corrupt provenance, or a timed
Session with no frozen warning schedule). The 45-minute synthetic budget is
no longer the default for missing, unbounded, or invalid data. Canonical
command locators accept committed UUIDs without rewriting the body.
Restored Agent transcript text no longer typewrites from empty on first
paint. Pause-interval persistence is unchanged.

Interim: accommodations are evaluated at `session_runtimes.created_at`, not
a dedicated frozen Attempt-timing row. A later revoke can drop a grant from
current rows even if it applied at start. Warning keys are consumed only
when present on the frozen timing domain; `ActivationBaselineDocument`
still does not write them, so real timed Sessions project `unavailable`
until a workflow/configuration producer exists. Do not invent thresholds.

Still not done: rebuild live API/Worker for 0068 + interval persist, full
Implementation CI, and the previously missing hosted-session states
(expiry, warning emission, multi-tab, offline reconnect, terminate/abort
live, forced-colors, 400% zoom). Do not retire this file.

Hosted Session transport, actor projections, production SPA routes, Worker
composition, bounded hosted telemetry, and fail-closed Worker identity are
implemented. Production HTTP locators remain Attempt-committed UUIDs.

2026-09-03 remaining-work close: Worker required an explicit
`Sessions:WorkerServiceActorId` plus a seeded issuer with
`service_delegation.issue`. Additive seed issues Invocation-execute
delegations for existing demo-org runtimes. Fail-closed execution completes
work without Agent text; snapshot maps `execution_failed` to `failed` so the
Participant can send again. Administrator pause/resume confirmed on
`:5274`. Participant remaining time now projects `active_duration` with the
Proposed 45-minute synthetic development budget. Agent timer lane stays
disabled. Production model enablement is not claimed.

2026-09-03 consistency review (no code edits): stack healthy (`session-endpoint:ok`,
Worker up, `:5274` 200, RedirectUri restored to canonical). Focused hosted
tests 7+7 passed. Participant live Session is `active`/`connected` with both
admitted messages and an open composer. Administrator on the live route is
not a working surface (empty transcript, composer closed, SSE reconnect
loop). See review findings in the implementation chat.

2026-09-03 confirm pass: stack still healthy; hosted HTTP/telemetry/profile 29
and projection 7 passed; Participant live Session remained `active`/`connected`
with both admitted messages after RedirectUri returned to canonical.

Interim transport default: production HTTP locators are the Attempt-committed
UUID; the existing command-envelope `stable_id` catalog remains unchanged for
synthetic fixtures. Hosted snapshot/event contracts use UUID `session_id`.
Command adapters require the path UUID and body `session_locator.session_id`
to be the same authoritative Session string.

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
  Authenticated-browser compose is Development-only and now registers the
  repeating synthetic fake that matches the frozen Attempt-start profile.
- Do not depend on unapproved `PROP-9` timer guidance for public behavior. Use
  the already approved one-lane timer semantics and implemented frozen bounds;
  expose no internal Agent-lane scheduling state to the Participant. Participant
  **Time remaining** is a separate hosted snapshot projection (`active_duration`
  remaining seconds). When Activity duration is absent, use the labeled
  Proposed 45-minute synthetic development budget.

# Findings / deviations

- 2026-09-03 independent review: hosted snapshot timing used a hard-coded
  45-minute budget and charged paused wall-clock after resume; client merge
  restored protected text over authoritative `unavailable`; terminate
  `reason_code` was discarded (`_ = terminateReasonCode`); HTTP
  `TryReadEnvelope` was not equivalent to the closed command schema. Web CI
  failed on `set-state-in-effect` in Session chrono, transcript reveal, and
  completing-seal. Task remains in-progress.
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
- The predecessor has now committed the active Session locator, digest-verified
  notice projection, and mutation-coupled Attempt terminal mapping seams. The
  refreshed plan treats them as dependencies to consume and protect rather
  than unresolved successor work.
- Authenticated-browser Compose currently runs PostgreSQL, Keycloak, migration,
  seed, artifact storage, API, SPA, and gateway services, but no Worker. The
  refreshed plan adds Worker composition because hosted message admission
  without durable Invocation processing cannot satisfy the end-to-end Session
  goal. Worker start also requires `Sessions:WorkerServiceActorId` and a
  seeded issuer that can issue `session.invocation.execute` delegations;
  P0 session start now issues that envelope when those actors are configured.
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
| Review-driven P1/P2 correctness (timing reconstruction, accommodations at start, warning fail-closed, UUID locator, transcript seed) | confirmed for this pass | 2026-09-03 confirm: Sessions frozen-timing 8 after camelCase baseline parse; focused Sessions 48; web session 28. Live API rebuilt no-reseed. Participant Session `01a0654c-…ef4851` snapshot `timing.policy=unavailable`, remaining/budget null — cohort duration is 3600s with no frozen warning keys (`REQ-SESS-24`). Persist shape uses `fairnessDomains`/`domainKey`/`effectiveValue`. Expiry/warning-emission/multi-tab/offline/terminate-live/forced-colors/400% still open. |
| Governing product/requirements/UI/architecture inventory | complete | Approved Text Session requirements v0.5, UI specification v1.0, runtime contract v0.5, design system v1.1, and current implementation seams rechecked 2026-09-03 |
| Predecessor dependency and scope boundary | complete | Treated complete per implementation request; locator/Continue/notice/terminal sink consumed |
| Existing contract/runtime/host/frontend seam inventory | complete for planning | 2026-09-03: command envelope C#/schema present; production SSE only; snapshot/command host, hosted event contract, production Session page, `/v1/sessions` gateway route, and authenticated-browser Worker service absent; Design Lab donor identified |
| Requirement/AC-to-surface and risk mapping | complete for planning | Requirement-to-surface table in this task covers Participant, administrator, assigned Reviewer/historical actors, backend, frontend, security/privacy, operations, and QA boundaries |
| `python3 scripts/check_docs.py` | complete | Passed 2026-09-03 after the state refresh; feature-catalog and documentation validation succeeded |
| `git diff --check` | complete | Passed 2026-09-03 with no whitespace errors; direct trailing-whitespace scan of this tracked task file also passed |
| Cross-cutting plan consistency/readiness review | complete | Third pass on 2026-09-03 applied backend, frontend, and security/privacy review perspectives to the current branch; refreshed landed predecessor seams and added the missing authenticated-browser Worker composition while preserving prior gateway, compatibility, actor-projection, state-ownership, UI-donor, and provider gates |
| Implementation tests and live-browser evidence | complete for this slice | 2026-09-03: hosted projection 7 passed; hosted HTTP+telemetry 7 passed; profile 22 passed. `verify:web` 638+206+11; `verify:dotnet` 1840/3 skipped; supply-chain and oci exit 0. Playwright: fail-closed `work: failed` then send recovered; guessed Session unavailable; admin pause→paused→resume; admin transcript route has no items/controls because administrator projection omits transcript. `verify:oidc` not run on this stack |
| Live-console CompactId, seated name, Agent status plaque | complete for this polish | 2026-09-03: `ProductionTextSessionPage` + keys + SessionMarks 55 passed. Candidate `:5274` (Demo Participant, compact `01a064de…9adad9`) and Design Lab `:5275` (`FXA…2A07`, Jordan Blake). Narrow 1-line status clamp opened a wrapping value plaque for the full examiner sentence. API RedirectUri restored to canonical after overlay |
| Design-system / Lab donor consistency | complete for this pass | 2026-09-03: Lab `LiveSessionLayout` wraps the production shell; `StageBars` owned by `components/work`; Lab complete plate copy and examiner sealed line match production. `check_docs.py` passed after `DESIGN.md` regenerate. Lab 5-bar theater and disabled composer remain demo-only. Desktop complete plate 560px on `:5275` and `:5274` |

# Blockers

- Predecessor activation gate cleared by implementation request; treat
  `p0-attempt-session-start` as completed for this successor. Remaining
  predecessor task-file status is ignored.
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

Independent review of `593f554` / `417adf1` / `d662cab` plus Implementation CI
`33708509001` found P1 timing, `unavailable` merge, and discarded terminate
`reason_code` defects; P2 HTTP/schema drift; and four Web lint failures. Do not
mark completed until those are green and the missing hosted-session states are
rechecked.

Gaps for review (not blockers for this host/UI slice):

- No assigned-Reviewer-only synthetic identity; `demo.admin` resolves as
  administrator and therefore sees an empty historical transcript list.
- Authenticated-browser Worker now uses Development `deterministic_fake` so
  compose Sessions can receive a repeating synthetic reply. Production Worker
  still fail-closes that adapter. This is not a qualified real-provider
  enablement. Live check on candidate `:5274`: after Worker-only recreate, a
  new send published the synthetic examiner follow-up. Turns processed before
  that recreate can still show `Content unavailable.`
- Chrono color stays `--amber` until the snapshot emits `warning_code:
  imminent`. Lab `is-warned` at 40:00 of a 60-minute demo is theater, not a
  production schedule. Desktop top inset matches Lab (rail/bay/examiner 18px);
  decorative traces are now absolutely overlaid so they cannot add a row.
- [x] 2026-09-03: after Agent complete, snapshot work_state is idle; hosted
  events carry current Session version and work_state; SPA keeps the higher
  version across refetch and applies command outcomes. Live `:5274` Session
  `01a06512-…ae7d4c` showed idle/awaiting after reload and accepted a new send
  (`Confirm same Session send.`) without a version-conflict notice.
- [x] Live console polish: snapshot now assembles Agent text from fragments;
  the SPA keeps richer local copy across refetch, typewrites durable Agent
  deltas, focuses the latest turn, uses two Examination/Complete bars, and
  seats a Lab-matched complete plate (focus Return). Verified on `:5274`: new
  send shows the synthetic reply (not unavailable); last turn `is-active`;
  complete plate 560px centered with `completeToAssignment` focused.
- Agent next-timer lane remains disabled (`PROP-9` unused). Participant
  remaining-time chrono is now in this slice. Expiry terminalization, warning
  thresholds, multi-tab, offline reconnect, terminate/abort, light/dark,
  forced-colors, and 400% zoom were not fully re-checked in this close-out.
- Participant can open `/operations` and see lifecycle/version without
  pause/terminate; they do not inherit control.
- Design-system and Lab donors reconciled 2026-09-03: production
  `LiveSessionLayout` is the shell; Lab wraps it; `StageBars` live in
  `components/work`; Lab complete plate copy matches production (no Result
  availability). Independent specialist review of this consistency pass is
  in the same conversation.

2026-09-03 confirm pass: stack still healthy; hosted HTTP/telemetry/profile 29
and projection 7 passed; Participant live Session remained `active`/`connected`
with both admitted messages after RedirectUri returned to canonical.

Implementation CI `oidc` static gate failed on 593f554: validator fixture
`valid_config()` omitted the required `worker` service.

2026-09-03 defect: `session.complete.v1` only entered `completing` and never
sealed `completed`, so the live console stayed composer-closed with no further
server transition. Host now runs begin_completing then complete (same pattern
as terminate). Production live-session chrome now uses the Design Lab
conversation donor: Agent core animation, examiner chrono, ledger turns, and
Transmit composer. Briefing and console-feed stay out. Detector on the changed
page/CSS returned no hits. Remaining-time chrono verified on candidate
`:5274` after API rebuild: desktop examiner shows Time remaining digits plus
gauge; narrow stacks examiner above transcript/composer. RedirectUri restored
to canonical `:18080`.
