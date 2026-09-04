---
id: hosted-text-session
status: in-progress
created: 2026-09-02
updated: 2026-09-04
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
- [>] Realtime red/green: extend the authorized hosted Session event
  projection and `/v1/sessions/{sessionId}/events` mapping for committed
  UI-relevant deltas missing from the snapshot contract.
  - [x] Hosted cursor/version/message/work/fragment/complete terminal core
    (accepted 2026-09-04): `session_version` replay, distinct `stream_cursor`
    for SSE `Last-Event-ID`, issued-cursor validation, version-before-sequence
    snapshot merge.
  - [ ] Lifecycle pause/resume SSE.
  - [ ] Timing/warning SSE.
  - [ ] Access/reconcile SSE.
  - [ ] Multi-device/offline verification matrix.
  Compatibility `/sessions/{id}/events` still uses `session_sequence`.
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
- [x] Remaining-time red/green: project remaining seconds from the frozen
  Session timing document captured at Attempt start. Seat Design Lab chrono
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
- [x] Live and historical transcript polish: close the unparsed
  `live-session.css` token wrapper so conversation chrome and the ledger
  scrollport load; reuse channel plates on the management transcript
  record (`framed={false}` so `.operate-scroll` is the wheel target).
  Do not retire this task.
- [x] Review of `115e9c4` (historical; keep `in-progress`): persist immutable
  Session timing at Attempt start; freeze configured warning keys from
  Activity timing when present (`PROP-6`, no invented thresholds); enforce
  cutoff before message admission; system-owned expiry terminalization plus
  Worker due-scan; deterministic command correlation; fix Web lint without
  suppressions; remove stale 45-minute decision text.
- [x] Distinct backend, frontend, security/privacy, and QA reviews at HEAD
  `05ce7e4` (2026-09-04). **Do not retire.** High findings remain; task stays
  `in-progress` for external review. `docs/current-state.md` Active work
  updated; session-text-lifecycle rows stay Partial. Keep this file until
  durable-truth promotion and retirement.

2026-09-03 review of `920596e` (**reject retirement — keep `in-progress`**):
Implementation **`33763594004`** and Documentation **`33763594071`** green.
**Probe infrastructure kept:** `ComposeStackHostedExpiryProbeTests` +
`probe-compose-hosted-expiry-sweep.sh`. **P1 — probe did not prove running
Worker loop:** test called `ExpireDueAsync` in-process; CI skips probe
(`FLEXAGENT_COMPOSE_PROBE` unset). **P1 — premature retirement:** deleted
task without completion checklist, specialist reviews, or recorded live Worker
proof. Task file restored from `d6cdc9a`; `current-state` corrected.

2026-09-03 corrective pass after `920596e` review: probe now waits for the
**running Worker** loop (no in-test `ExpireDueAsync`); seeds demo-org Attempt
via `ComposeProbeSubmissionSeed` so Worker terminal mapping succeeds;
`PostgresHostedSessionExpirySweep.TryExpireAsync` read/write scope split +
distinct Complete correlation. Local `probe-compose-hosted-expiry-sweep.sh`
green — fairness 5/5; worker-loop probe 1/1 (~3s). Evidence: Session
`01a067ae-8d47-79cb-b93e-0e0b53d41a6a` → `completed`, terminal
`time_expiry`, Attempt `completed`. **Still open:** QA matrix; distinct
specialist reviews; completion checklist; then promote + retire.

2026-09-03 review of `92b43fb` + `183d13a` (**accept sequence — keep
`in-progress`**): no blocker; P2 cleanup before task closure — (1)
`docs/current-state.md` Worker-proof wording updated to “corrected in
`92b43fb`”; (2) probe script now waits for Worker container health, not
only `session-endpoint:ok`; (3) Compose probe reuses stable actor/enrollment
and logs append-only footprint in `finally`; (4) request-triggered expiry
now uses distinct `.complete` correlation via
`HostedSessionCommandCorrelation.ExpiryCommandId`.

## Present implementation (authoritative)

- **Landed:** authenticated snapshot/command/events host; versioned hosted SSE;
  production `/sessions/:sessionId`, `/operations`, `/transcript`; frozen
  timing (`0069`); `IHostedSessionExpirySweep` on Worker loop;
  `LiveSessionLayout` / `StageBars`; probe script + env-gated compose test
  (`920596e` infrastructure; **`92b43fb`** running-Worker loop proof).
- **CI closed:** core timing **`888eb91`** / **`33743544924`**; fixture
  **`b24f67c`** / **`33754337758`**; probe infra **`920596e`** /
  **`33763594004`** (probe skipped in CI).
- **Worker-loop proof (local, not task closure):** **`92b43fb`** —
  `probe-compose-hosted-expiry-sweep.sh` green 2026-09-03 confirm pass;
  Session `01a067b2-…532c58` → `completed` / `time_expiry`, Attempt
  `completed` (~3s). CI does not run env-gated probe.
- **Still open (tracked):** all High specialist-review findings listed under
  Specialist review findings; warning emission/history; multi-tab/device;
  offline/reconnect; terminate/abort live; forced-colors/400%; durable
  current-state promotion after those close; then `completed` + retire.
  Specialist reviews themselves are recorded (2026-09-04).

# Current state

2026-09-04 **review of `588d8a2` follow-up (keep `in-progress`; do not
retire):** accept specialist-review record; correct state accounting — restore
Realtime to `[>]` with nested accepted core vs residual SSE/QA sub-items; roll
up **all seven** High findings by reference (not a shortened five-item list);
rename `# Completion` to **External-review handoff checklist**. No
implementation rollback.

2026-09-04 **specialist reviews at HEAD `05ce7e4` (keep `in-progress`; do not
retire):** distinct backend, frontend, security/privacy, and QA reviews ran
against code, focused tests, governing specs, and live candidate `:5274`
(Compose `:18080` healthy; RedirectUri restored to canonical after). Host,
routes, Worker, frozen timing, and hosted SSE cursor/version mechanics remain
accepted. **Not product-complete:** High remaining defects listed under
Specialist review findings. No active Compose Session remained (two
`completed` runtimes; assignment entitlement exhausted), so live send, pause,
resume, terminate, warning, and reconnect were not re-exercised in the
browser; those stay residual plus the High findings. Task file kept for
external review.

2026-09-04 **confirm pass:** re-read pause empty payload + `ReasonCode` null
except terminate; HTTP-edge `HasCurrentPermissionAsync` with no commit AuthZ;
`lastError` reducer-only; `composerClosed` ignores connection; Confirm
Submission / Submit Session; historical `includeTranscript` without terminal
gate. Stack `session-endpoint:ok`, RedirectUri canonical `:18080`. Findings
unchanged. Do not retire.

2026-09-03 transcript polish (candidate `:5274`, then RedirectUri restored
to canonical `:18080`): Vite could not parse `live-session.css` (unclosed
`html:has` token wrapper), so conversation chrome never loaded and the
ledger had no overflow owner. Closing that block plus themed
`scrollbar-gutter` on `.ledger-frame` restored the live scrollport
(desktop: main `.ledger-frame` overflow auto, scrollHeight > clientHeight).
Historical `/transcript` now uses the same channel plates in an unframed
record well; `.operate-scroll` is the wheel target. Lab console-feed was
removed from the production rail. Detector on the changed page/ledger
returned no hits. Hosted-text-session stays `in-progress`.

Expiry boundary: authoritative remaining `0` closes send/submit, moves
stage to 2 of 2, shows Checking then **Time ended. Session completed**
(not a Result). Reconcile on that boundary seals Active/Paused via
`time_expiry` (`PROP-5` Completed). Live `:5274` Session `01a06512-…ae7d4c`
became `completed`; composer closed; Return to assignment present.
RedirectUri restored to canonical `:18080`. Reload of an expiry-completed
Session still uses the generic complete plate until `terminal_reason` is
projected. Worker due-scan of remaining `0` is implemented as
`IHostedSessionExpirySweep` on the Worker loop (service actor /
`session.hosted.expiry`). Live rebuild and CI evidence are still open.

Same-Session follow-up send no longer surfaces
"This Session record was updated" on the happy path. Transmit stays held
while Agent work is queued/working and while the post-accept snapshot
refetch is in flight. A single `trigger.admission.stale.version` conflict
refetches then retries the same message on the same Session locator.

2026-09-04 **Review of `c654649` — hosted cursor/replay accepted (keep `in-progress`)**:
external review accepted the unpaged-agent → hosted-paging fix, multi-page
regressions, `MaxEncodableSessionSequence`, and deterministic lease-renewal
cancellation. **Core `session_sequence` / `session_version` / `stream_cursor`
mechanics are now sound** — no further cursor/replay correction pass. Remaining
work shifts to lifecycle pause/resume SSE, warnings/timing/access/reconcile,
multi-tab/device offline QA, live `:5274` verification, accessibility/extreme
layout, and specialist reviews. **CI:** run **`33826694891`** green through dotnet/web/oidc/oci-oidc-smoke; supply-chain failed on
transient `pnpm audit` registry timeout (not a dependency finding). `run-pnpm-audit.sh`
retry landed in **`698e33c`**; run **`33828971210`** fully green (supply-chain included).

2026-09-04 **Review of `e3c641b` — hosted multi-page replay (keep `in-progress`)**:
hosted projection now builds from unpaged agent events before hosted paging, so
`agent.complete` survives beyond the compatibility page size; regression covers
multi-page replay and a second in-flight message. `MaxEncodableSessionSequence`
documents arithmetic cursor bounds. `FrozenProviderAuthorityProcessor` lease
renewal test waits on cancellation instead of a fixed delay. **Evidence:**
Sessions 545 (including `Hosted_replay_*` multi-page regressions);
`FrozenProviderAuthorityProcessorTests.Lease_renewal_exception_cancels_the_in_flight_provider_call`;
Contract 195; `session-view` vitest 23. Full Implementation CI re-run still
open.

2026-09-04 **Review of `3c2f664` — hosted stream cursor + version-first merge
(keep `in-progress`)**: (1) hosted SSE `id` / replay `Last-Event-ID` is now
`stream_cursor = session_sequence * 10 + slot`, so queued + accepted at the
same Session sequence are both applied; (2) historically issued working
cursors stay valid after the first fragment; (3) snapshot merge prefers
higher `session_version` before sequence (`working` V8/S15 → `failed` V9/S15);
(4) missing item occurrence metadata stays null instead of
`LastCommittedAt`. Compatibility `/sessions/{id}/events` still uses
`session_sequence`. Lifecycle pause/resume/warning/access SSE and the
multi-device/offline matrix remain open.

2026-09-03 **P1 turn-close gating + transcript timestamps (keep `in-progress`)**:
Agent transcript items from hosted SSE now carry `occurred_at` on
fragment/complete so turn times render without waiting for a later send.
Stale post-send snapshot refetches no longer rewind resolved Agent
`activity` when sequence/version lag SSE. Send and **Submit Session** stay
blocked until the Agent turn closes (queued/working activity or a streaming
Agent item) and post-accept reconciliation finishes; composer text stays
editable per `UI-SESS-DEC-2`. Stale refetches no longer rewind Agent item
status from `complete` to `streaming`. **Evidence:** session-view vitest 16;
`ProductionTextSessionPage` vitest 16.

2026-09-03 **Review of `288a8ee` + `3257aa4` — P1/P2 correction pass (keep
`in-progress`)**: (1) snapshot transcript rows now derive `occurred_at` and
sequence from durable manifest/fragment data — participant from
`transcript.append.v1`, agent from first fragment `CommittedAt`; regression
`Snapshot_preserves_transcript_occurred_at_after_later_session_mutation`.
(2) hosted `agent.complete` carries authoritative `item_status`
(complete/incomplete/cancelled); frontend reducer and merge prefer snapshot
terminal truth over erroneous SSE `complete`. (3) post-accept `checking` clears
only when `sessionPostSendReconciled` (snapshot shows queued/working/failed/
no_action, agent turn open, or participant item at/after accepted sequence);
refetch error → `uncertain`. (4) `IsIssuedStreamCursor` accepts only sequences
actually emitted by hosted projection replay. **Evidence:** Sessions 538;
session-view vitest 21+; `ProductionTextSessionPage` vitest 37. Lifecycle
pause/resume/warning/access SSE, multi-tab/offline matrix, and specialist
reviews remain open.

2026-09-03 **P1 repeated-turn 409 (confirmed + fixed; keep `in-progress`)**:
hosted SSE replay used `AuthorizedSessionEventProjector`, which left
`SessionVersion = 0` on Agent fragment/completion events. The browser
reducer uses `Math.max(event.session_version, snapshot.session_version)`,
so SSE could not advance the known Session version after Agent work; the
next send reused a stale `expected_session_version` and hit
`409 trigger.admission.stale.version`. **Fix:** pass
`UseHostedProjection: true` on the `/v1/sessions/{id}/events` subscribe
path so replay uses `HostedSessionEventProjector`, stamping authoritative
`session_version` and `work_state`. Follow-up pass extends hosted replay for
message accepted, agent queued/working/no-action/failure, hosted cursor
validation, SSE reconnect snapshot refetch, and regression tests. Lifecycle
pause/resume/warning/access SSE remains open. Legacy `/sessions/{id}/events`
compatibility route unchanged. Frontend merge preserves SSE transcript items
ahead of a refetched snapshot sequence. **Evidence:** Sessions 535; Runtime
338; session-view vitest 12; `ProductionTextSessionPage` vitest 15.

Live candidate `:5274` Attempt 2 Session `01a0654c-…ef4851` (pre-fix):
first send accepted; immediate second send 409 then 200 retry; transcript
kept both turns; examiner returned to awaiting. Re-verify on candidate
after deploy.

Independent review follow-up is in place; task stays `in-progress`. Hosted
timing reconstruction now distinguishes `unbounded` (timer disabled), timed
effective duration (cohort baseline plus `EffectiveTimingEvaluator` at
Session start), and unavailable (missing/corrupt provenance, or a timed
Session with no frozen warning schedule). The 45-minute synthetic budget is
no longer the default for missing, unbounded, or invalid data. Canonical
command locators accept committed UUIDs without rewriting the body.
Restored Agent transcript text no longer typewrites from empty on first
paint. Pause-interval persistence is unchanged.

2026-09-03 review of `115e9c4` (keep `in-progress`): persist
`session_frozen_timing` at Attempt start so later accommodation revoke or
supersession cannot rewrite the committed budget. Activity `TimingRules`
may author warning thresholds; `ActivationBaselineDocument` copies only
authored keys (`PROP-6`). Hosted commands reject admission after remaining
`0` (`session.cutoff.passed`). Expiry terminalization uses the Worker
service actor and `session.hosted.expiry`; the Participant reconcile
observes. Worker due-scan expires due timed Sessions without an open
console. Command correlation is deterministic for non-GUID `stable_id`
values. Web lint suppressions for transcript reveal and completing effects
were removed.

Consistency review 2026-09-03 (keep `in-progress`): live Compose
`:18080` is healthy (`session-endpoint:ok`, RedirectUri canonical) and
candidate `:5274` is signed in as Demo Participant. The running API/Worker
images do **not** contain `session_frozen_timing` / expiry-sweep symbols;
Postgres has `session_pause_intervals` but no `session_frozen_timing`
(0069 not applied). All four `session_runtimes` are `completed`. The
Anti-Harassment assignment is Attempt-exhausted (2 consumed). Source
cutoff now rejects only live `active`/`paused` remaining `0`, so a
`completing` seal is not blocked by the projected remaining `0`. Worker
due-scan now prefers timed documents that already have a warning
schedule. Domain cutoff + frozen-timing + correlation tests: 16 passed.
Confirm pass: Sessions timing/cutoff 25 passed; Assessment draft/baseline
19 passed; API and Worker Debug builds succeeded; Web eslint on the
touched Session files is clean; vitest reveal + Session page + ledger
14+ tests passed. Source cutoff only trips live `active`/`paused`
remaining `0`. Do not claim live expiry until API/Worker are rebuilt
and 0069 is applied.

Still not done: live rebuilt API/Worker + migration `0069` / server-owned
expiry verification (CI OIDC smoke applied `0069` and built Worker OCI but does
not exercise `IHostedSessionExpirySweep`; do not claim live expiry until local
Compose API/Worker are rebuilt and expiry is proven — see evidence below);
warning emission + reconstructable warning history; multi-tab/device behavior;
offline/reconnect; live terminate/abort; forced-colors; 400% zoom/reflow;
distinct backend/frontend/security-privacy/QA reviews; durable-truth/current-
state promotion. **Core hosted-session timing implementation and full
Implementation CI are closed** (see 2026-09-03 review below). Do not retire
this file until the remaining QA/behavior matrix and specialist reviews
complete.

2026-09-03 review of `b24f67c` (revert premature retirement — keep
`in-progress`): Implementation **`33754337758`** and Documentation **`33754337878`**
green on fixture-only commit. **P2 closed:** `DefaultHardEndAtUtc` → `2099-12-31`
in `SessionPersistenceFixtures.cs`. **Process P1:** commit deleted this task file
and promoted `docs/current-state.md` with live Compose expiry/rebuild claims
without reconciling completion checklist, specialist reviews, or recorded live
`IHostedSessionExpirySweep` proof — conflicts with `.work/README.md` lifecycle
(Reconcile → Promote → Review → mark completed → Retire). Task file restored
from `d366171`; current-state corrected. Live rebuilt API/Worker + migration
`0069` / server-owned expiry verification **still not done** in tracked evidence.
Do not retire until QA/behavior matrix and distinct reviews complete.

2026-09-03 review of `fb71087` (**approve** — keep `in-progress`): corrective
commit closes process P1 from `b24f67c`. Documentation **`33756971896`** and
Implementation **`33756971882`** green (docs/task-only; implementation jobs
skipped correctly). Full code-bearing gate remains **`33754337758`** on
`b24f67c`. Task file, `in-progress` status, unchecked completion, pending
specialist reviews, and live expiry gap restored; unsupported current-state
claims removed. **P2 housekeeping:** reconcile stale historical notes below
against present-state summary. Proceed with remaining closure work; do not
re-open core timing unless a concrete failure appears.

## Present implementation (historical 2026-09-03; superseded above)

- **Landed:** authenticated snapshot/command/events host; versioned hosted SSE;
  production `/sessions/:sessionId` live-session (Participant),
  `/operations`, `/transcript`; `/v1/sessions` gateway route;
  authenticated-browser Worker (Development `deterministic_fake` only);
  frozen timing at Attempt start (`0069`); `IHostedSessionExpirySweep` on Worker
  loop; `LiveSessionLayout` / `StageBars` production donors.
- **CI closed:** core timing + fixes on **`888eb91`** / **`33743544924`**; fixture
  hard-end on **`b24f67c`** / **`33754337758`** (`DefaultHardEndAtUtc` →
  `2099-12-31`).
- **Still open at that date:** later superseded by the 2026-09-04 specialist
  review snapshot in Present implementation (authoritative) above.

2026-09-03 review of `ed47065` (keep `in-progress`): approve docs/task-state
update recording timing + CI closure. Corrected: restored live API/Worker +
`0069` expiry verification in remaining summary; fixed stale HEAD reference in
verification table. Implementation **`33748755541`** on `ed47065` correctly
skipped implementation jobs; Documentation CI green.

2026-09-03 review — **core timing + Implementation CI closed** (keep
`in-progress`): Implementation **`33743544924`** on code-bearing **`888eb91`**
full green — changes, dotnet, web, oidc, supply-chain (NuGet vuln, pnpm audit,
licenses, SBOM + grype, Gitleaks clean), OCI/OIDC smoke. Commit chain:
`0895025` lint cleanup; `972d742` Activities `ProductionSourceOptionsResponse`
+ Postgres fixture default frozen timing; `888eb91` fix recursive
`SessionPersistenceFixtures.InsertActiveAsync`; `5ed60c7` task-state only.
No new P0/P1 timing regression. Proceed with remaining hosted-session QA/
behavior closure; do not iterate core timing unless a concrete failure appears.
~~**P2 deferred:** `DefaultHardEndAtUtc` uses `2026-12-31`~~ **Closed in
`b24f67c`:** `2099-12-31` (was deferred at this review). Fixture auto-seed keeps
unavailable-timing tests on
`repository.InsertActiveAsync` or `seedDefaultFrozenTiming: false`
(`HostedSessionTimingFairnessTests` unchanged).

2026-09-03 review of `cd90e5d` (keep `in-progress`): approved core timing
boundary — mandatory `hard_end_at_utc`, coordinator re-validation, private
capture result construction, exception-safe structural JSON validation, and
malicious-capture regression. P2 only: duplicate warning last-one-wins and
offset-less hard-end parsing hardening deferred. Core timing correctness
closed pending full Implementation CI green; task remains open for warning
emission/history, multi-tab/offline, terminate/abort-live, accessibility
matrix, and specialist reviews. Web CI lint fix (`AssessmentCampaignCreatePage`
test unused param) required to unblock Implementation web job on run
`33734154401`. CI fix `0895025` follow-up: align Activities page source-options
type with `ProductionSourceOptionsResponse`; seed default frozen timing in
Postgres integration session fixtures so admission no longer returns
`timing_unavailable` without a row. Follow-up: bulk replace accidentally made
`SessionPersistenceFixtures.InsertActiveAsync` recurse into itself (CI #504 dotnet
StackOverflow in ~1m); fixed to call `repository.InsertActiveAsync`.

2026-09-03 review of `4004584` (keep `in-progress`): require non-null
`hard_end_at_utc` for authoritative `timed` and `unbounded` capture documents;
re-validate capture document at `AttemptStartCoordinator` trust boundary;
harden structural JSON validation (root/warning object shape, no
`InvalidOperationException` escape); private successful
`FrozenAttemptTimingCaptureResult` construction with internal test bypass;
coordinator regression proves invalid capture never reaches
`CommitActiveAsync`. Verification 2026-09-03: Submissions
`FrozenAttemptTimingDocumentsTests` + `AttemptStartCoordinatorTests` 29 passed;
Runtime demo seed 3 passed. Full Implementation CI not re-run in this pass.
Core hosted timing correctness considered closed pending CI; warning emission,
multi-tab/offline, terminate/abort-live, accessibility matrix, and specialist
reviews remain open.

2026-09-03 review of `4ce0308` (keep `in-progress`): positive frozen timing
capture validation via typed `FrozenAttemptTimingCaptureResult`; gate
`development.synthetic_timed.v1` to development host environment; production
create path fails closed until timing is authored; dev synthetic capture uses
preset warning constants.

2026-09-03 review of `1126162` (keep `in-progress`): fail closed on
`timing.unavailable` for send/resume/permitted actions and Attempt start;
exclude unavailable rows from Worker hard-end expiry candidates; wire explicit
`development.synthetic_timed.v1` preset through Activity create (API + web);
allowlist Gitleaks false positives in session command envelope validator tests.

2026-09-03 review of `7de6983` (keep `in-progress`): fixed expiry SQL pause
sign (`budget + paused`), enforce `hard_end_at_utc` for unbounded Sessions in
projection/admission/sweep, wire warning thresholds through Activity HTTP +
demo/fixture seeds, and add resumed-active expiry starvation regression.
Verification 2026-09-03: Sessions 525 passed (includes unbounded hard-end
domain test); HostedSessionTimingFairnessTests 3 passed (cutoff, paused
starvation, resumed-active starvation); Runtime 336 passed (includes updated
demo baseline digest); AssessmentConfiguration 96 passed; Postgres integration
384 passed. Full Implementation CI not re-run end-to-end in this pass.

Hosted Session transport, actor projections, production SPA routes, Worker
composition, bounded hosted telemetry, and fail-closed Worker identity are
implemented. Production HTTP locators remain Attempt-committed UUIDs.

2026-09-03 remaining-work close: Worker required an explicit
`Sessions:WorkerServiceActorId` plus a seeded issuer with
`service_delegation.issue`. Additive seed issues Invocation-execute
delegations for existing demo-org runtimes. Fail-closed execution completes
work without Agent text; snapshot maps `execution_failed` to `failed` so the
Participant can send again. Administrator pause/resume confirmed on
`:5274`. Participant remaining time projects only frozen
`unbounded` / `timed` / `unavailable` timing. Agent timer lane stays
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
  **Time remaining** is a separate hosted snapshot projection. Timing is
  `unbounded`, `timed`, or `unavailable`. Missing, corrupt, or timed-without-
  a-frozen-warning-schedule provenance is `unavailable`. Do not invent a
  45-minute Session budget.

# Findings / deviations

- 2026-09-03 independent review: hosted snapshot timing used a hard-coded
  45-minute budget and charged paused wall-clock after resume; client merge
  restored protected text over authoritative `unavailable`; terminate
  `reason_code` was discarded (`_ = terminateReasonCode`); HTTP
  `TryReadEnvelope` was not equivalent to the closed command schema. Web CI
  failed on `set-state-in-effect` in Session chrono, transcript reveal, and
  completing-seal. **Remediated** in subsequent slices; see Present
  implementation above.
- **Historical (pre-land activation):** the missing boundary was transport and
  composition, not product meaning — command variants, runtime state, Postgres
  coordinators, Worker processing, and SSE replay existed while the production
  snapshot/command host and SPA were absent. **Present:** host, hosted events,
  live-session SPA, gateway route, and compose Worker are landed.
- **Historical:** legacy production SSE projected only Agent fragments/completion.
  **Present:** hosted events plus snapshot carry lifecycle, timing, and terminal
  deltas required by the approved Session UI (legacy stream regression-protected).
- **Historical:** Session route used management shell unavailable ceremony.
  **Present:** approved `live-session` family on `/sessions/:sessionId`.
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
| Full Implementation CI on hosted replay (`c654649` / `698e33c`) | complete | 2026-09-04: **`33826694891`** (`c654649`) — dotnet/web/oidc/oci-oidc-smoke green; supply-chain failed on transient `pnpm audit` timeout. **`698e33c`** adds registry retry; run **`33828971210`** fully green. |
| Full Implementation CI on timing + CI-fix chain (`888eb91`) | complete | 2026-09-03: run **`33743544924`** green — dotnet, web, oidc, supply-chain (Gitleaks clean), oci-oidc-smoke (applied `0069`; Worker OCI built; expiry sweep not exercised). Follow-up docs-only runs **`33743559578`** (`5ed60c7`) and **`33748755541`** (`ed47065`) skipped implementation jobs correctly. |
| Full Implementation CI on fixture hard-end (`b24f67c`) | complete | 2026-09-03: run **`33754337758`** green — changes, dotnet, web, oidc, supply-chain, oci-oidc-smoke (migration `0069` applied; Worker OCI built; expiry sweep not exercised). Documentation **`33754337878`** green. Does not satisfy live Compose expiry proof gap. |
| Corrective retirement revert (`fb71087`) | complete | 2026-09-03: **approve** — restores task + accurate gaps. Documentation **`33756971896`**; Implementation **`33756971882`** (changes only; code jobs skipped). Full code gate unchanged (`b24f67c` / **`33754337758`**). |
| Existing contract/runtime/host/frontend seam inventory | complete (present state) | **Landed:** command envelope + schema; snapshot/command host; versioned hosted SSE; production Session pages (`/sessions/:sessionId`, operations, transcript); `/v1/sessions` gateway; authenticated-browser Worker. Legacy unversioned SSE retained for regression. Design Lab donor adapted into production `LiveSessionLayout`. |
| Review of `4ce0308` positive capture + env-gated preset | complete | Submissions timing tests; Runtime; web; superseded by `cd90e5d`/`888eb91` chain + full CI above. |
| Review of `1126162` fail-closed + preset + Gitleaks fixes | confirmed prior pass | Sessions 531; HostedSessionTimingFairnessTests 5; gitleaks clean; Assessment HTTP negatives 24 passed. |
| Review of `7de6983` P1 fixes (expiry pause sign, unbounded hard end, Activity warning contract) | confirmed for prior pass | 2026-09-03: Sessions 525; HostedSessionTimingFairnessTests 3; Runtime 336; AssessmentConfiguration 96; Postgres integration 384 — all passed. Demo baseline digest `1406e373…`. Activity HTTP create/read now carries explicit warning fields; seeds/fixtures author `900/300`. Unbounded + `HardEndAtUtc` projects hard-end boundary with warnings disabled. Expiry/warning-emission/multi-tab/offline/terminate-live/forced-colors/400% still open. |
| Review-driven P1/P2 correctness (timing reconstruction, accommodations at start, warning fail-closed, UUID locator, transcript seed) | confirmed for prior pass | 2026-09-03 confirm: Sessions frozen-timing 8 after camelCase baseline parse; focused Sessions 48; web session 28. Live API rebuilt no-reseed. Participant Session `01a0654c-…ef4851` snapshot `timing.policy=unavailable`, remaining/budget null — cohort duration is 3600s with no frozen warning keys (`REQ-SESS-24`). Persist shape uses `fairnessDomains`/`domainKey`/`effectiveValue`. Expiry/warning-emission/multi-tab/offline/terminate-live/forced-colors/400% still open. |
| Governing product/requirements/UI/architecture inventory | complete | Approved Text Session requirements v0.5, UI specification v1.0, runtime contract v0.5, design system v1.1, and current implementation seams rechecked 2026-09-03 |
| Predecessor dependency and scope boundary | complete | Treated complete per implementation request; locator/Continue/notice/terminal sink consumed |
| Requirement/AC-to-surface and risk mapping | complete for planning | Requirement-to-surface table in this task covers Participant, administrator, assigned Reviewer/historical actors, backend, frontend, security/privacy, operations, and QA boundaries |
| `python3 scripts/check_docs.py` | complete | Passed 2026-09-03 after the state refresh; feature-catalog and documentation validation succeeded |
| `git diff --check` | complete | Passed 2026-09-03 with no whitespace errors; direct trailing-whitespace scan of this tracked task file also passed |
| Cross-cutting plan consistency/readiness review | complete | Third pass on 2026-09-03 applied backend, frontend, and security/privacy review perspectives to the current branch; refreshed landed predecessor seams and added the missing authenticated-browser Worker composition while preserving prior gateway, compatibility, actor-projection, state-ownership, UI-donor, and provider gates |
| Implementation tests and live-browser evidence | complete for this slice | 2026-09-03: hosted projection 7 passed; hosted HTTP+telemetry 7 passed; profile 22 passed. `verify:web` 638+206+11; `verify:dotnet` 1840/3 skipped; supply-chain and oci exit 0. Playwright: fail-closed `work: failed` then send recovered; guessed Session unavailable; admin pause→paused→resume; admin transcript route has no items/controls because administrator projection omits transcript. `verify:oidc` not run on this stack |
| Live-console CompactId, seated name, Agent status plaque | complete for this polish | 2026-09-03: `ProductionTextSessionPage` + keys + SessionMarks 55 passed. Candidate `:5274` (Demo Participant, compact `01a064de…9adad9`) and Design Lab `:5275` (`FXA…2A07`, Jordan Blake). Narrow 1-line status clamp opened a wrapping value plaque for the full examiner sentence. API RedirectUri restored to canonical after overlay |
| Design-system / Lab donor consistency | complete for this pass | 2026-09-03: Lab `LiveSessionLayout` wraps the production shell; `StageBars` owned by `components/work`; Lab complete plate copy and examiner sealed line match production. `check_docs.py` passed after `DESIGN.md` regenerate. Lab 5-bar theater and disabled composer remain demo-only. Desktop complete plate 560px on `:5275` and `:5274` |
| Implementation CI on probe infra (`920596e`) | complete | 2026-09-03: **`33763594004`** green; probe test skipped in dotnet job (`FLEXAGENT_COMPOSE_PROBE` unset). Does not prove running Worker loop. |
| Running-Worker Compose expiry loop (`92b43fb`) | complete (local env-gated) | 2026-09-03 confirm: `probe-compose-hosted-expiry-sweep.sh` end-to-end green — migrate, API/Worker recreate, nginx restart, fairness 5/5, worker-loop probe 1/1 (~3s). Session `01a067b2-b631-7832-9383-04f252532c58` → `completed`, terminal `time_expiry`, Attempt `completed`. Not exercised in CI. |
| 2026-09-03 confirm pass (post-`92b43fb`) | complete | Stack `session-endpoint:ok`; `probe-compose-hosted-expiry-sweep.sh` green (fairness 5/5, worker-loop 1/1); `ProductionTextSessionPage` vitest 12/12; `check_docs.py` passed. Re-run ~21:40 UTC+7 same result. Task remains `in-progress` (QA matrix + specialist reviews open). |
| 2026-09-03 confirm pass (P2 cleanup) | complete | `probe-compose-hosted-expiry-sweep.sh` green after api/worker health + session-endpoint readiness fix (fairness 5/5, worker-loop 1/1); correlation tests 3/3; vitest 12/12; `check_docs.py` passed. Task remains `in-progress`. |
| Hosted SSE authoritative `session_version` + repeated-turn regression | complete | 2026-09-03: `HostedSessionEventProjector` wired on `/v1/sessions/{id}/events`; regression tests for version, repeated turn, refresh-while-working, stale retry. Sessions/Runtime/web vitest green locally. Live `:5274` re-verify still open. |
| Hosted SSE committed-delta expansion (message/work/no-action/failure) | partial | 2026-09-04: distinct `stream_cursor` for hosted Last-Event-ID; same-sequence dual events; stable historical working cursor; version-before-sequence activity merge; honest null timestamp fallback. Evidence: Sessions 542; Contract 195; session-view + page vitest 39; eslint clean; `check_docs.py` passed. Lifecycle pause/resume/warning/access SSE and full offline/multi-tab matrix still open. Task remains `in-progress`. |
| Distinct specialist reviews at HEAD `05ce7e4` | complete (reject retirement) | 2026-09-04: backend + frontend + security/privacy + QA. Focused tests: HostedSession domain 60, application 9, Runtime HTTP/telemetry 8; session-view + ProductionTextSessionPage vitest 39. Live candidate `:5274` then RedirectUri restored canonical: participant terminal live + historical transcript; participant operations without pause/terminate; guessed-id non-disclosing unavailable; administrator operations without transcript; administrator transcript empty; administrator live route honest “not loaded”. No active Session remained — live send/pause/resume/terminate/reconnect not re-run. Screenshots local MCP, not committed. Confirm pass 2026-09-04: High findings re-read in source; stack canonical `session-endpoint:ok`. |

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

# External-review handoff checklist

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review

2026-09-04: this checklist is **external-review handoff only**, not
product-complete or retirement. Status stays `in-progress`. Implementation CI
already green on the hosted-replay chain (`33828971210`). Live send/pause/
resume/terminate/warning/reconnect and other QA matrix items were not re-run.
Specialist reviews found seven High remaining defects; keep this file.

Independent review of `593f554` / `417adf1` / `d662cab` timing defects and
later `cd90e5d`/`888eb91` chain are addressed; Implementation CI
**`33743544924`** is green. Do not mark the **task** `completed` or retire
until the High findings below and residual QA matrix are closed.

Gaps for review (not blockers for this host/UI slice):

- ~~**P2:** `SessionPersistenceFixtures.DefaultHardEndAtUtc` is `2026-12-31`~~
  **Closed in `b24f67c`:** `DefaultHardEndAtUtc` → `2099-12-31` (CI **`33754337758`**).
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
  pause/terminate; they do not inherit control. Reconfirmed 2026-09-04 on
  candidate `:5274`.
- Design-system and Lab donors reconciled 2026-09-03: production
  `LiveSessionLayout` is the shell; Lab wraps it; `StageBars` live in
  `components/work`; Lab complete plate copy matches production (no Result
  availability). Independent specialist review of this consistency pass is
  in the same conversation.

# Specialist review findings (2026-09-04, HEAD `05ce7e4`)

Consolidated across backend, frontend, security/privacy, and QA. Highest
justified severity kept. Specs rechecked: `REQ-SESS-7`, `REQ-SESS-27`,
`AC-SESS-12`, `AC-SESS-24`–`26`, `AC-SESS-30`, `UI-SESS-DEC-8`,
`UI-SESS-DEC-10`, `UI-SESS-DEC-11`, `REQ-AUTH-13`/`18`.

[High] Pause command and audit omit required bounded reason
- Location: `contracts/schemas/v1/session/command-envelope.v1.schema.json` (`pause_command` empty payload); `PostgresHostedSessionCoordinators.cs`; `ProductionSessionOperationsPage.tsx`
- Perspective: backend | frontend | security/privacy
- Spec/invariant: `REQ-SESS-27`, `AC-SESS-12`, `UI-SESS-DEC-11`
- Evidence: pause wire allows only empty `payload`; coordinator passes `ReasonCode: null`; ops UI has no reason selection.
- Impact: Fairness/audit cannot reconstruct why a Session was paused.
- Recommendation: Add bounded `reason_code` to pause wire + domain + audit + confirmation UI; reject pause without it. Interim default: treat as defect until a labeled `PROP-*` says otherwise.

[High] Hosted commands authorize once before commit, not at the write
- Location: `PostgresHostedSessionCoordinators.cs` (`HasCurrentPermissionAsync` then `AcceptAsync` / `ChangeAsync`); accept/lifecycle coordinators do not call commit AuthZ
- Perspective: backend | security/privacy
- Spec/invariant: `REQ-SESS-7`, `AC-SESS-12`, `REQ-AUTH-18`
- Evidence: permission check is HTTP-edge only; Invocation/timer paths re-check at commit elsewhere.
- Impact: Revocation between admission and commit can still mutate Session state.
- Recommendation: Re-evaluate the exact action + ownership chain inside the same transaction as `LoadForUpdate`.

[High] Reconnect/offline does not disable send or completion
- Location: `ProductionTextSessionPage.tsx` (`composerClosed` ignores `view.connection`; `"offline"` never assigned)
- Perspective: frontend | QA
- Spec/invariant: `UI-SESS-DEC-8`, `AC-SESS-25`
- Evidence: `EventSource.onerror` sets `reconnecting` only; Transmit/Submit stay usable. Not live-probed (no active Session).
- Impact: Participants can mutate while command state is uncertain.
- Recommendation: Gate composer + completion on `connection !== "connected"`; keep draft; show reconnect copy; reconcile before re-enabling.

[High] Command/admission errors never reach the UI
- Location: `session-view.ts` `lastError`; never read by `ProductionTextSessionPage.tsx`
- Perspective: frontend
- Spec/invariant: `UI-SESS-DEC-5`, `AC-SESS-25`
- Evidence: `rg lastError` hits only the reducer. Uncertain shows a bare Reconcile control.
- Impact: Failed or conflicted sends look like a silent stall.
- Recommendation: Surface `lastError` in a status/alert region tied to the composer.

[High] Completion ceremony copy violates required terms
- Location: `ProductionTextSessionPage.tsx` dialog; `SessionChrono.tsx` **Submit Session**
- Perspective: frontend
- Spec/invariant: `UI-SESS-DEC-10`
- Evidence: Dialog title **Confirm Submission**; primary **Submit Session**; cancel **Remain in Session**. Spec requires **Complete this Session?** / **Complete Session** / **Continue Session**.
- Impact: Completion reads like grading submission.
- Recommendation: Align labels to `UI-SESS-DEC-10`, or formally amend the decision if Shipboard “Submit” is intentional. Interim default: DEC-10 still governs completion.

[High] Historical snapshot can include live transcript before terminal
- Location: `HostedSessionSnapshotProjector.Project` (`includeTranscript` for `historical` regardless of lifecycle)
- Perspective: security/privacy | backend
- Spec/invariant: `AC-SESS-30`; UI assigned-Reviewer table (terminal transcript)
- Evidence: `recovery_category=unavailable` when non-terminal, but transcript/timing still projected. No assigned-Reviewer synthetic identity to live-prove.
- Impact: Premature disclosure if a reviewer assignment exists on an active Session.
- Recommendation: Omit transcript and exam timing for `historical` unless lifecycle is terminal.

[High] Org-wide grants plus dual-role command family
- Location: `PostgresAuthorizationKernel` human grants; `PostgresHostedSessionCommandCoordinator.SubmitAsync`; seed org-wide `session.pause` / `session.terminate`
- Perspective: security/privacy
- Spec/invariant: `REQ-AUTH-13`, `AC-SESS-13`; `docs/current-state.md` already Gap for `REQ-AUTH-9`/`10`/`13`
- Evidence: Kernel grant check is `(organization_id, actor_id, granted_action)`; command maps pause/terminate from grants even when subject resolved as Participant. Seed is synthetic-dev.
- Impact: Cross-activity admin control within a tenant; dual-role pause while appearing as Participant.
- Recommendation: Bind Session control to activity/ownership chain; require projection-compatible command families. Interim default: org-wide seed grants are synthetic-dev only; activity-scoped AuthZ is required before real multi-activity hosted enablement.

[Medium] Lifecycle `idempotency_key` accepted then ignored; admission rejects still advertise Active `permitted_actions`; pause does not dispose in-flight turns; no hosted rate limit; SSE MFA strength not applied to snapshot/commands; no `ProductionSessionOperationsPage` / transcript page tests; assignment Attempt history has no path back to a completed Session; examiner status truncates; terminal terminated/aborted not distinguished from **Session Complete**.

Working controls observed: 404 non-disclosure on guessed ID; administrator projection omits transcript; participant operations has no pause/terminate; antiforgery on commands; gateway exact `/v1/sessions`; provider fail-closed; hosted telemetry without transcript/Session UUIDs; Design Lab not imported into production.

Open questions (interim defaults):
1. Pause without `reason_code` — defect until `PROP-*`. Rationale: `REQ-SESS-27` is approved.
2. Shipboard “Submit Session” vs `UI-SESS-DEC-10` — DEC-10 still authoritative.
3. Reviewer live transcript — terminal-only for `historical`.
4. Dual-role admin commands while Participant — deny; command family must match projection.
5. Client auto-`session.complete.v1` while `completing` — prefer server-owned seal.

QA not re-run live (no active Session / exhausted entitlement): send, pause, resume, terminate, warning, reconnect/offline, multi-tab, forced-colors, 400% zoom.

2026-09-03 confirm pass (post-`92b43fb`): `probe-compose-hosted-expiry-sweep.sh`
green; Session `01a067b2-…532c58` worker-loop expiry proof; fairness 5/5;
vitest 12/12; `check_docs.py` passed; stack `session-endpoint:ok`. Task stays
`in-progress`.

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

# External review disposition (2026-09-04, HEAD `f80c111`)

**CHANGES REQUIRED — DO NOT RETIRE.** Reviewer treated `f80c111` as
accounting-only after specialist review at `05ce7e4`; seven High findings
remained open with no code-bearing fix commits.

# Closure pass (2026-09-04)

Addresses the seven High findings from specialist review at `05ce7e4`
(recorded in `588d8a2` / external review `f80c111`).

| High finding | Fix summary |
| --- | --- |
| Pause `reason_code` | `pause_payload.reason_code` on wire; validator/coordinator/lifecycle handler require reason; ops UI bounded reason select; audit `ReasonCode` on pause |
| Commit-time AuthZ | `SessionHostedCommandCommitAuthorization` reauthorizes in accept/lifecycle coordinators before commit |
| Reconnect gates send/complete | `connection !== "connected"` closes composer + Complete Session; reconnect alert copy per `UI-SESS-DEC-8` |
| `lastError` surfaced | Danger alert when `view.lastError` set |
| Completion copy | `Complete this Session?` / `Complete Session` / `Continue Session` per `UI-SESS-DEC-10` |
| Historical transcript gate | `historical` projection omits transcript/timing unless lifecycle terminal |
| Dual-role / projection AuthZ | `HostedSessionCommandAdmission` requires projection-compatible relationship + `permitted_actions` |

**Partial on org-wide activity scope:** kernel grants remain org-scoped
(synthetic-dev seed); command admission now denies admin commands when subject
relationship is Participant and requires server-derived `permitted_actions`.
Full activity-scoped human grants remain a documented gap (`REQ-AUTH-13`).

# External review disposition (2026-09-04, HEAD `13cc2c5`)

**CHANGES REQUIRED — DO NOT RETIRE.** Reviewer accepted direction on six of
seven High findings but blocked sign-off on: (1) OpenAPI pause-contract parity
(`SessionPauseCommandV1.payload.reason_code` missing from
`contracts/projections/openapi.v3.1.yaml`; web CI red); (2) `REQ-AUTH-13`
cross-Activity human grant scope still open — org-wide `session.pause` could
control another Activity's Session; (3) reconnect while completion dialog
open left a silently no-op primary button.

# Second closure pass (2026-09-04)

| Reviewer item | Fix summary |
| --- | --- |
| OpenAPI pause parity | `SessionPauseCommandV1` pause payload requires `reason_code` with min/max length |
| `REQ-AUTH-13` human Session admin | `SessionHumanGrantScopeValidation` in kernel: administrative session actions require activity stewardship (`assessment_activity_revisions.actor_id`) or explicit `session_actor_relationships` administrator row; ownership chain validated against `session_runtimes`; administrator subject resolution joins activity revision steward |
| Reconnect + completion dialog | `setConfirmComplete(false)` on EventSource `onerror`; vitest regression |
| Cross-Activity negative coverage | `SessionHumanGrantScopeTests` integration: steward A denied pause/terminate on Activity B session; steward permitted on own Activity |

**Still open:** Realtime `[>]` lifecycle pause/resume, timing/warning, access/
reconcile SSE; live QA matrix (no active Session); full Implementation CI on
final code-bearing SHA; durable promotion to `docs/current-state.md`.

**Focused verification (this pass):**

- `pnpm verify:web` green (OpenAPI parity + lint + vitest + build)
- `FlexAgent.Sessions.Tests` 552 passed
- `SessionHumanGrantScopeTests` 2 passed (Postgres integration)
- `ProductionTextSessionPage` vitest 18 passed (includes reconnect + completion-dialog edge)

**Prior pass verification (`13cc2c5`):**

- `FlexAgent.Sessions.Tests` 552 passed
- `FlexAgent.Contract.Tests` 195 passed
- `FlexAgent.Runtime.Tests` hosted-session filter 8 passed
- `web` vitest `ProductionTextSessionPage` + `session-view` 40 passed
