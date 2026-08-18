---
id: text-interaction-controller-contract
status: planned
created: 2026-08-15
updated: 2026-08-18
activation_gate: explicit-product-lead-prioritization
---

# Goal

After the structured Agent runtime sync completes, decide whether and how to
promote a narrow text Interaction Controller into the assessment MVP, then
produce a reviewed, authority-by-concern product, requirements, UI/UX,
architecture, security/privacy, and implementation agreement.

The proposed controller coordinates accepted Participant input during an
active Agent response and the existing bounded Session timer signal. It permits
the resolved Agent to make semantic interaction recommendations while the
Session runtime remains authoritative and only one Agent response may publish
to the Participant at a time.

This task is a contract and planning task. It does not implement the controller.

# Governing sources

- `AGENTS.md` and `docs/README.md` — authority by concern, product invariants,
  open-question defaults, security/privacy, specification-driven work, and
  tracked-plan rules
- [`../resources/text-interaction-controller-proposal.md`](../resources/text-interaction-controller-proposal.md)
  — non-authoritative source proposal to evaluate, revise, approve, or reject
- [`structured-agent-runtime-sync.md`](structured-agent-runtime-sync.md) —
  prerequisite implementation and evidence; its current target must not move
  because this planned task exists
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — canonical Session, Agent Invocation/Decision,
  timer, Interaction Controller, assessment fairness, and release-tier meaning
- `docs/requirements/features/session-text-lifecycle.md` and
  `docs/requirements/features/resolved-session-configuration.md` — current P0
  trigger, streaming, cancellation, concurrent-pending-turn, timer, frozen
  policy, Decision, and deferred-capability behavior
- `docs/ui-ux/text-session.md` and the approved design-system status,
  implementation guide, and applicable interaction modules — current
  Participant message, Agent streaming, pending, no-action, timer-triggered,
  interruption-adjacent, reconnect, and accessibility behavior
- `docs/architecture/session-runtime-contract.md` and ADR-011 through ADR-014 —
  durable streaming, Invocation/Decision, one-lane timer replacement, output
  envelope, ordering, cutoff, and future Interaction Controller seams
- `docs/requirements/mvp-operational-defaults.md` — applicable latency,
  durability, availability, observability, and bounded-runtime defaults
- Completed runtime-sync verification, implementation traceability, review
  findings, and actual code/persistence evidence available when this task begins

# Dependency and activation gate

- This task remains `planned` and blocked while
  `structured-agent-runtime-sync` is not completed.
- Do not edit authoritative product, requirements, UI/UX, or architecture
  documents for this proposal before the dependency clears unless the Product
  Lead explicitly changes the active work priority and runtime-sync target.
- When the dependency clears, first reconcile this plan and source proposal
  against the implemented runtime. Do not assume the proposed lanes, actions,
  schemas, persistence, or UI states match the completed seams.
- Starting this contract task does not authorize implementation. Create a
  separate implementation task only after the governing decisions are approved.

# Scope

## In

- Inventory the completed structured runtime's actual trigger admission,
  Invocation concurrency, provider cancellation, Decision/action validation,
  response-slot publication, Session ordering, timer, durable-work,
  persistence, reconnect, UI, and operational seams.
- Decide the promoted MVP user outcome and measurable value for mid-response
  Participant input and interval-driven proactive Agent outreach.
- Decide canonical ownership among Agent, Harness, Activity, Session runtime,
  Interaction Controller, provider adapter, and Participant UI without creating
  another Session, independent Agent identity, or uncontrolled authority.
- Specify whether one purpose-bound interaction-decision Invocation may overlap
  one response Invocation within a Session and enforce exactly one
  Participant-visible Agent publisher.
- Define trusted trigger types, admission, minimization, deduplication,
  coalescing, idempotency, retry, cooldown, timeout, chain, interruption,
  proactive-outreach, and Session budgets.
- Define the minimum controller recommendation vocabulary and independent
  runtime validation/effect boundary, including continue, interrupt, queued
  response-slot eligibility, control-purpose no-action, the existing direct
  timer-triggered message/no-action path, and timer replacement semantics.
- Preserve one immutable Participant Message, one stable Turn, and one response
  slot for each accepted input. Define controller work as a separate
  purpose-bound Invocation that cannot create or re-admit a duplicate Turn.
- Define expected-state and stale-decision handling against authoritative
  Session sequence, lifecycle, active output, timer revision, pause,
  revocation, cutoff, completion, and terminal state.
- Decide text preemption outcome compatibility. Interim guidance is to preserve
  existing `Cancelled`/`Incomplete` Agent-message outcomes with a bounded
  preemption reason rather than reuse voice-specific `Interrupted` semantics.
- Resolve transcript-item order versus fragment-event order when Participant
  input is accepted while an earlier Agent message continues to grow.
- Define Participant-visible pending, checking, continuing, stopping,
  cancelled/incomplete, proactive, failure, reconnect, and recovery behavior
  with accessible names, announcements, focus, and narrow-viewport behavior.
- Define frozen policy resolution and cohort-fairness behavior. Lower scopes
  may narrow but never widen controller capabilities or budgets.
- Define durable state ownership, retention, Evidence/audit boundaries,
  protected references, reconstruction, metrics, alerts, and sensitive-content
  minimization.
- Compare amendment of the existing P0 Session specifications against a new
  owning feature specification. Use the smallest authority structure that
  remains coherent; do not add an eighth P0 specification by inertia.
- Compare extending ADR-012/ADR-013/ADR-014 against a new superseding or
  complementary Interaction Controller ADR, preserving historical decisions
  and applied migrations.
- Publish only explicitly approved decisions in the appropriate authoritative
  artifacts with versions, supersession links, stable requirement/acceptance
  IDs, traceability, and implementation-status honesty.
- Run distinct product/requirements, UI/UX, architecture, backend feasibility,
  frontend feasibility, security/privacy, and QA/testability reviews.
- Produce a separate specification-driven implementation task with exact
  requirement-to-code-and-test mapping after approval.

## Out

- Controller implementation, migrations, schemas, provider calls, worker
  handlers, API/SSE changes, frontend behavior, deployment, or release.
- Changes to the active `structured-agent-runtime-sync` acceptance target while
  this task remains blocked.
- Voice, speech detection, floor management, playback, TTS, or voice
  interruption behavior.
- Multiple visible Agent response lanes, multiple Participants in one Session,
  arbitrary or parallel timers, tools, Dynamic memory, general workflow-event
  orchestration, or an autonomous second Agent.
- Continuous model observation, token-by-token controller evaluation, or
  access to hidden reasoning or unpublished provider output.
- Provider-specific public contracts or a universal controller model/SKU
  choice before qualification evidence requires one.
- Commits, pushes, pull requests, deployments, or releases unless separately
  requested.

# Plan

- [x] Wait for `structured-agent-runtime-sync` completion and its required
  implementation, verification, and independent-review evidence.
- [ ] Reconcile the source proposal with the completed runtime seams,
  traceability matrix, residual risks, and any superseding product decisions.
- [ ] Resolve product scope, actor outcomes, release tier, measurable success,
  non-goals, and proposed defaults with explicit Product Lead decisions.
- [ ] Author observable requirements and acceptance criteria for trigger
  admission, concurrency, ordering, interruption, proactive outreach, failure,
  recovery, fairness, audit, and isolation.
- [ ] Author the Text Session interaction states and accessibility behavior,
  loading only the applicable approved design-system modules.
- [ ] Produce the architecture decision for controller ownership, logical
  lanes, purpose-bound Invocations, expected-state effects, one visible
  publisher, data ownership, contracts, persistence, recovery, and operations.
- [ ] Threat-model the new signal, model, cancellation, content-disclosure,
  isolation, fairness, resource-loop, and audit boundaries and add negative
  verification requirements.
- [ ] Run independent cross-concern reviews, resolve blocking findings, and
  validate document authority, links, IDs, versions, and traceability.
- [ ] Create a separately tracked implementation task mapping each approved
  requirement and acceptance criterion to code, migration, tests, runtime
  evidence, and UI verification.

# Current state

Planned and awaiting explicit Product Lead prioritization. The structured
runtime dependency and its successor host slices are completed, including the
structured Invocation/Decision, response-streaming, production HTTP SSE,
Session binding, and one-lane timer foundation. Interaction Controller triggers
remain disabled and deferred by the approved MVP scope.

If the Product Lead activates this contract task, the first substantive step is
an evidence-based seam inventory against the completed runtime and remaining
production gates; do not begin by copying the proposal into authoritative
documents or by treating the cleared technical dependency as product approval.

# Proposed decisions to evaluate

- One Session and one frozen resolved Agent revision may have one response
  Invocation and one interaction-decision Invocation overlap under positive
  bounds.
- Only one response lane may publish an Agent message to the Participant.
- The controller is event-driven. Mid-response Participant input may require a
  control-purpose Invocation; an idle timer preserves the existing direct
  timer-triggered Agent Invocation and Agent-initiated Turn path.
- The controller detects and normalizes permitted signals, the Agent recommends
  semantic interaction behavior, and the runtime owns trusted-trigger and
  Invocation admission, scheduling, authorization, ordering, validation,
  cancellation, and effects.
- Every control effect uses runtime-bound expected Session/output/timer state
  and fails safely when stale; model-authored identifiers cannot establish the
  target or authority.
- Controller failure preserves accepted Participant input and does not infer a
  destructive interruption.
- An accepted Participant message retains exactly one stable Turn and response
  slot. Controller resolution changes only its response eligibility and never
  creates or re-admits another Turn.
- A control-purpose Invocation cannot publish directly. This does not prevent
  an idle timer-triggered Invocation from using the existing single response
  lane to publish an Agent-initiated message.
- Text preemption preserves the existing `Cancelled`/`Incomplete` completion
  vocabulary with a bounded reason unless an approved specification justifies
  a new text-specific state; it does not reuse voice playback interruption.

These are Proposed only and require the authority-by-concern process above.

# Findings / deviations

- The active runtime already includes a bounded next-timer foundation and
  timer-triggered Agent work. The proposed controller should extend that lane
  rather than create a second scheduler.
- The active runtime task explicitly lists Interaction Controller behavior as
  out of scope and requires it to remain disabled. Authoritative promotion
  before completion would invalidate the stable implementation target.
- Durable incremental Agent fragments can interleave in Session event order
  with later Participant input while remaining part of an earlier stable Agent
  message. The contract must make transcript-item order and event order
  explicit rather than assume they are identical.
- Proposal consistency review corrected trusted-admission ownership, stable
  Participant Message/Turn identity, distinct-message handling, runtime-bound
  expected state, timer fast-path preservation, control-purpose `no_action`,
  and the publication-versus-human-perception boundary.

# Open questions

Interim defaults below are working guidance only.

- **Same or separate controller model?** Interim default: same frozen Agent
  revision and resolved provider/model profile with purpose-specific
  instructions and schema recorded in the resolved Session configuration.
  Rationale: preserves cohort consistency until a separate controller
  deployment is qualified and governed.
- **Controller failure fallback?** Interim default: allow a valid response to
  finish and retain the Participant input for the next permitted Turn.
  Rationale: avoids destructive cancellation from an unavailable classifier.
- **Timer signal during an active response or controller work?** Interim
  default: coalesce at most one pending signal under expiry/cooldown bounds and
  admit no parallel Agent work. When idle, preserve the existing direct
  timer-triggered Invocation and Agent-initiated Turn path. Rationale: prevents
  backlog and feedback loops without adding a second model call to ordinary
  proactive outreach.
- **Direct control-purpose messages?** Interim default: prohibited. The
  control-purpose Invocation may change only runtime-bound response
  eligibility. An idle timer-triggered Invocation may publish through the
  existing response lane. Rationale: enforces one visible publisher while
  preserving approved timer behavior.
- **Explicit Participant stop-and-send?** Interim default: preserve the runtime
  capability as a candidate recovery/control path, but defer its UI prominence
  and assessment consequence to approved interaction design.
- **Existing spec amendment or new P0 spec?** Interim default: prefer amending
  the existing Text Session and resolved-configuration specifications if clear
  ownership and traceability remain achievable. Rationale: the behavior is a
  Session interaction extension, but a dedicated spec remains available if
  amendment would obscure boundaries.
- **Text preemption completion state?** Interim default: use `Cancelled` with a
  bounded Participant-input-preemption reason when cancellation is proven and
  `Incomplete` otherwise; do not reuse voice-specific `Interrupted`. Rationale:
  preserves current streaming semantics without importing playback meaning.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Dependency status and completion evidence | complete | `.work/active/structured-agent-runtime-sync.md` and its Worker, production HTTP SSE, subject-binding, and timer-activation successors are completed as of 2026-08-18 |
| Proposal remains non-authoritative and does not enable behavior | complete | Status and dependency statements in `.work/resources/text-interaction-controller-proposal.md` and this task |
| Proposal cross-concern consistency review | complete | Checked against `REQ-SESS-10`, `REQ-SESS-61`–`REQ-SESS-70`, `REQ-SESS-84`, `SESS-DEC-14`–`SESS-DEC-23`, ADR-012, ADR-013, ADR-014, and Text Session/design-system interaction boundaries; corrections applied 2026-08-15 |
| Governing document inventory | pending | Recheck after dependency completion |
| Requirement/AC/decision traceability | pending | Produce during contract authoring |
| Documentation links, IDs, versions, and validation | pending | Run after authoritative edits |
| Independent cross-concern reviews | pending | Required before approval/completion |

# Blockers

- Product Lead approval is required to promote the Interaction Controller from
  deferred behavior into the MVP.

# Completion

- [ ] Planned work is reconciled with the completed runtime and actual changes
- [ ] Product scope and proposed defaults are explicitly decided
- [ ] Requirements and acceptance criteria are approved and traceable
- [ ] Applicable UI/UX specification is approved
- [ ] Architecture decision and security/privacy review are approved
- [ ] Documentation validation and cross-concern reviews pass
- [ ] A separate implementation task is created with verification mapping
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
