---
id: multi-channel-agent-output-contract-adoption
status: completed
created: 2026-08-14
updated: 2026-08-14
unblocks: structured-agent-runtime-sync
---

# Goal

Turn `.work/resources/multi-channel-agent-output-proposal.md` into a reviewed,
authority-by-concern product, requirements, UI/UX, architecture, security, and
migration agreement before further provider, worker, or Agent-message runtime
implementation.

The outcome must approve the smallest coherent text-P0 semantic Agent-output
foundation and preserve a complete Proposed P2 extension path for voice only,
message only, coordinated voice plus message, and no participant-facing output
without making model output authoritative. It must identify the exact P0
compatibility changes—if any—that belong in
`.work/active/structured-agent-runtime-sync.md`, while leaving voice behavior,
Interaction Controller, TTS, rich-content UI, and P2 delivery for their approved
authoring and implementation stages.

# Governing sources

- `AGENTS.md` and `docs/README.md` — authority by concern, product invariants,
  open-question defaults, specification-driven delivery, security/privacy,
  and tracked-work rules
- `.work/resources/multi-channel-agent-output-proposal.md` — proposed behavior, design
  principles, seventeen planning questions, requested planning deliverables,
  scenarios A-G, and non-goals; Proposed only until promoted and approved in
  the governing authoritative documents
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — canonical Agent Invocation/Decision/Turn/Message
  meaning, voice product contract, assessment-first scope, and deferred voice
  release boundary
- `docs/requirements/features/session-text-lifecycle.md` and
  `docs/requirements/features/resolved-session-configuration.md` — approved P0
  text runtime, frozen capabilities, Decision/no-action/message, ordering,
  isolation, and next-timer behavior
- `docs/requirements/features/voice-interaction-interruption.md` — P2 voice
  placeholder that currently has no approved requirements or acceptance
  criteria
- `docs/requirements/features/evidence-evaluation.md` — Evidence/Evaluation
  authority and the current prohibition on enabling voice or Participant tools
  in P0
- `docs/ui-ux/text-session.md`, `docs/ui-ux/design-system/README.md`, and
  `docs/ui-ux/design-system/implementation-guide.md` — approved P0 Participant
  behavior, design-system authority, and the rule that later voice patterns do
  not enable voice without an approved owning specification
- `docs/ui-ux/design-system/product/conversation.md`, `agent-presence.md`,
  `timeline.md`, `protected-content.md`, `evidence.md`, `evaluation.md`, and
  `voice.md` — shared patterns to evaluate after release scope and owning
  interaction behavior are approved
- `docs/architecture/decisions/ADR-011-participant-visible-agent-response-streaming.md`,
  `ADR-012-structured-agent-invocation-and-decision-boundary.md`,
  `ADR-013-agent-requested-next-timer-replacement.md`, and
  `ADR-014-agent-output-envelope-and-p0-compatibility.md` — durable text
  streaming, one structured Decision envelope, validation/effect, intentional
  no-action, timer-control, P0 output profile, and future Interaction Controller
  boundaries
- `docs/architecture/session-runtime-contract.md`,
  `evaluation-execution-contract.md`, and `review-result-release-contract.md`
  — authoritative Session ordering/recovery, Evidence/Evaluation separation,
  and protected review/release boundaries
- `docs/architecture/mvp-architecture.md` and ADR-001-ADR-010 as applicable —
  modular-monolith ownership, scoped persistence, immutable history, provider
  neutrality, canonical contracts, and additive migration rules
- Current implementation evidence: `contracts/schemas/v1/session/agent-decision.v1.schema.json`,
  `src/BuildingBlocks/FlexAgent.Contracts/Session/SessionRuntimeContractsV1.cs`,
  `src/Modules/Sessions/`, `database/migrations/up/0005_session_runtime_schema.sql`
  through `0008_session_turn_created_sequence.sql`, and their focused tests
- `.work/active/structured-agent-runtime-sync.md` and
  `.work/active/structured-agent-runtime-traceability.md` — consumer of
  the approved P0 compatibility result

# Scope

## In

- Inventory the actual current input, Invocation, Decision, validation/effect,
  Agent Message, fragment/SSE, Session event, persistence, synthetic UI, future
  voice, and Interaction Controller seams. Answer every repository-location and
  impact question in the proposal with concrete files/modules/types.
- Build proposal traceability covering every section, all seventeen planning
  questions, every requested planning deliverable, scenarios A-G, all eight
  design principles, supported output combinations, and stated non-goals.
- Clarify and seek approval for canonical product semantics: Invocation,
  Decision, requested action, typed output, participant-facing conversational
  response, Turn, Agent Message, logical grouping, and authoritative effect.
  Preserve the distinction between model recommendation and runtime authority.
- Decide whether one Decision has a disposition, typed outputs, requested
  actions, controls, or another bounded combination. Resolve cardinality,
  ordering, validation independence, partial acceptance, effect atomicity,
  idempotency, retry, recovery, cutoff, and no-participant-output semantics.
- Preserve voice and message as different semantic presentations rather than
  duplicated text/audio encodings. Resolve only the P0-compatible semantic
  foundation now; retain voice-only and voice-plus-message behavior as explicit
  Proposed P2 inputs until the repository's P2 authoring gate is satisfied.
- Define channel-independent output identity and coordination: stable logical
  response/Turn association, runtime-owned output identifiers, deterministic
  order, cross-output/artifact references, presentation dependencies, and
  reconstruction of which outputs came from the same Decision.
- Separate presentation kind from visibility/authorization. Define permitted
  audiences and server-side audience derivation/validation without trusting a
  model-generated label, client scope, or content. Keep Participant message,
  reviewer/admin material, runtime-only control, Evidence, Evaluation, concise
  audit rationale, and hidden chain-of-thought as distinct categories.
- Define the message semantic boundary so persistent content can evolve from
  safe text/Markdown to typed code, tables, citations, diagrams, attachments,
  artifacts, or interactive components without treating arbitrary HTML or
  model-authored executable content as trusted UI.
- Capture the future voice semantic boundary as Proposed P2 input:
  conversational brevity, TTS-independent intent, Interaction Controller
  ownership of floor/silence/interruption/playback, generated/sent/played/
  interrupted/cancelled/playback-confirmed distinctions, and continuation from
  only authoritative playback-confirmed content.
- Define the P0 presentation-timing responsibility boundary and capture future
  Agent intent, Harness/workflow, Session runtime, Interaction Controller, TTS,
  and frontend coordination as Proposed P2 input. Include sequencing,
  dependency, independent channel progress, interruption, cancellation,
  reconnect/replay, pause/cutoff, and partial-channel failure or degradation.
- Define current structured-input consequences only where required for coherent
  P0 output, and capture Interaction Controller/workflow/tool-result extensions
  as Proposed later-tier inputs. Preserve trusted admission, minimization,
  deduplication, cooldown/budgets, feedback-loop suppression, and the fact that
  raw signals are not automatically Invocations.
- Define authoritative state versus append-only events for Decision, output
  generation, message publication/presentation acknowledgement, voice delivery/
  playback progress, interruption, requested-action validation/effect, and
  reconstruction. Distinguish persistence, server publication, client receipt/
  render acknowledgement, and human perception; assign one owning domain plus
  one scope/authorization boundary to every durable state without dual writes.
- Specify audit and review reconstruction sufficient to determine what was
  generated, persisted/shown, sent for speech, playback-confirmed, interrupted/
  cancelled, acted upon, and grouped together—without recording or exposing
  hidden chain-of-thought or copying sensitive content into generic audit,
  telemetry, or manifest records.
- Threat-model Organization/Activity/Participant/Attempt/Session isolation,
  prompt-injected audience or action requests, reviewer/internal content
  disclosure, output-reference substitution, unsafe rich rendering, artifact/
  citation access, untrusted links/external fetches, voice/transcript and TTS-
  provider disclosure, forged/missing/out-of-order presentation or playback
  acknowledgements, replay/races, resource storms, provider/TTS failure,
  retention/export/backup, and observability cardinality.
- Define measurable quality and UX behavior for latency, incremental delivery,
  backpressure, fairness/cutoff, recovery, offline/reconnect, accessibility,
  captions/transcripts and non-audio alternatives that distinguish generated
  from playback-confirmed content, focus/announcements, reduced motion,
  400-percent reflow, desktop/narrow shared-workspace layouts, and protected-
  content loss of access.
- Compare compatible v1 profile, additive v2 contract, and deferred-migration
  options. Preserve historical reconstruction; never silently redefine
  `agent-decision.v1` or rewrite applied/frozen migrations. Specify dual-read/
  write, projection, backfill, rollout, and rollback only to the extent the
  chosen option requires them.
- Separate release tiers explicitly:
  - current text P0 compatibility subset required before the parent runtime
    task resumes;
  - P2 voice requirements and UI/UX authoring only after the MVP slice works
    end to end, unless the Product Lead explicitly changes that approved order;
  - later multi-channel/voice implementation and Interaction Controller/TTS
    only after those P2 requirements and interactions are approved;
  - future typed rich content, tools/actions, reviewer outputs, and other
    channels only where approved;
  - explicit non-goals such as unrestricted full duplex, visual workflow
    builders, multi-agent collaboration, or a universal multimodal protocol.
- Publish only the P0-compatible meaning, behavior, and technical realization
  needed to clear the current gate in the appropriate authoritative product,
  existing P0 requirement, and ADR artifacts, with explicit status/version/
  supersession links. Do not create an eighth P0 feature specification. Keep
  later voice and shared-workspace requirements/interactions Proposed in this
  planning record until the approved P2 authoring sequence permits promotion.
- Produce an exact impact map for agent runtime, Session runtime, events,
  persistence, contracts/OpenAPI/C#/TypeScript, Interaction Controller, TTS,
  frontend, tests, deployment/operations, existing abstractions, and migration.
- After approval, amend the parent runtime plan and traceability matrix with
  only the approved P0 compatibility work, tests, and additive migrations;
  retain a complete P2 requirements/UI authoring handoff for later. Create a
  separately tracked voice/multi-channel implementation task only after the P2
  owning requirements, interaction specification, and ADR are approved.
- Run independent product/requirements, UI/UX, architecture, security/privacy,
  backend feasibility, frontend feasibility, and QA/testability reviews. Resolve
  blocking findings and verify documentation links, IDs, status, and internal
  consistency before clearing the runtime gate.

## Out

- Implementing or enabling voice, playback, silence triggers, Interaction
  Controller, TTS, rich rendering, reviewer UI, tools, workflow transitions, or
  other deferred capabilities.
- Implementing the provider port, worker, Session effects, events, database,
  frontend, or production adapters; approved P0 consequences are returned to
  the parent runtime task for specification-driven TDD.
- Treating design-system voice modules, proposal examples, or an architecture
  extension point as release approval.
- Letting model-authored audience, scope, output references, presentation
  timing, tool/control requests, or visibility become authoritative without
  Harness/runtime validation and current authorization.
- Treating reviewer notes, scores, confidence, follow-up strategy, structured
  Evidence, or Evaluation as an ordinary Participant message or as hidden
  chain-of-thought. Existing Evidence/Evaluation/review authority remains
  separate unless an approved specification changes it.
- Final provider-specific audio formats, codec, TTS product, database table
  names, API endpoints, frontend components, or TypeScript implementation
  shapes before the semantic and architectural decisions require them.
- Rewriting frozen/applied migration scripts or silently changing historical
  v1 wire meanings.
- Unrestricted full-duplex voice, generic multimedia support, visual workflow
  builders, multi-agent collaboration, deployments, commits, pushes, pull
  requests, or releases.

# Design coverage matrix

| Proposal outcome | Required disposition/evidence |
| --- | --- |
| A — voice only | Deferred P2 requirement/UI target plus an architecture extension path; no P0 voice enablement or unnecessary persistent rich message |
| B — message only | P0-compatible persistent, replayable message output without required speech |
| C — voice + message | Deferred P2 target for shared Decision coordination, independent output identity/order/delivery, explicit references, and coordinated UX |
| D — interruption | Deferred P2 target: message remains available; voice generated/sent/played/interrupted/cancelled/playback-confirmed state is reconstructable; only playback-confirmed content enters continuity |
| E — silent/internal action | P0-compatible distinction among no Participant output, accepted control, explicit primary no-effect, rejection, suppression, and execution/effect failure |
| F — non-user runtime input | Current approved trigger/timer subset remains bounded; Interaction Controller inputs and broader choices are deferred P2 inputs with trusted admission and loop/budget requirements |
| G — audit reconstruction | P0 reconstruction for current message/control behavior plus a deferred P2 target for shown, spoken, playback-confirmed, interrupted/cancelled, acted-upon, and coordinated outputs without hidden reasoning leakage |

# Plan

- [x] Inventory current repository contracts and flows.
- [x] Reconcile product meaning and release scope.
- [x] Determine the smallest authoritative P0 requirements change.
- [x] Produce a Proposed later-release UI/UX impact outline.
- [x] Compare architecture options and record ADR-014.
- [x] Produce the security/privacy threat model.
- [x] Produce the current-to-target impact and compatibility matrix.
- [x] Define ordered reviewable implementation phases without starting runtime implementation.
- [x] Run product/requirements, UI/UX, architecture, security/privacy, backend, frontend, and QA review passes.
- [x] Amend the parent runtime plan and traceability matrix; clear the dependency.
- [x] Run documentation validation and proposal-to-authority traceability audit.
- [x] Resolve post-commit review: independent output/action validation (P1),
  AC-SESS-43 execution-versus-Decision split (P2), and stale Text Session
  version catalog references (P3).

# Current state

Post-commit review of `2018d764` is addressed in authoritative artifacts. P1
independent item validation, P2 execution-versus-Decision split, and P3 Text
Session v0.5 catalog references are in the specs/ADR/runtime contract. Parent
runtime task `structured-agent-runtime-sync` is unblocked at the model-execution
port with those semantics. No provider, worker, voice, TTS, or rich-content
implementation was started here.

# Current-state inventory

Trusted input -> Invocation -> bounded execution attempts -> exactly one
Decision or execution outcome -> independent validation -> effect.

| Seam | Current location | P0 consequence |
| --- | --- | --- |
| Decision contract | `contracts/schemas/v1/session/agent-decision.v1.schema.json`; `IAgentDecisionV1` in `SessionRuntimeContractsV1.cs` | Keep immutable; dual-read as envelope profile; add successor schema in parent task |
| Frozen policy | `P0TextSessionRuntimeCapabilityPolicy`, `FrozenRuntimePolicyResolver` | Freeze output/action kinds (`REQ-RSC-54`/`55`) |
| Domain Decision | `AgentDecisionRecord`, `SessionRuntime` | Envelope + runtime-owned output ids before provider port |
| Persistence | `database/migrations/up/0005`–`0008` | No rewrite; additive only if successor storage needs it |
| Message stream | ADR-011 fragments/SSE; synthetic `SessionPage` | P0 `message` output effect only |
| Timer control | ADR-013 `next_timer_request` | Sole P0 requested action |
| Voice / IC / TTS | Product voice contract; ADR-012 seam; P2 placeholder spec | Remain disabled |

# Proposal question answers

1. Current contract: `agent-decision.v1` plus Invocation/attempt/outcome schemas and C# unions.
2. Successor types live with canonical Session contracts (`contracts/schemas/v1/session/`), C# DTOs, and Sessions domain; not provider SDKs.
3. One Decision envelope: disposition + typed outputs + typed requested actions (`PROP-MCO-1`, approved).
4. P0 message events remain ADR-011 fragments/completion linked to Decision and output id. Voice events are P2.
5. P0 delivery = generated/accepted/persisted/published/complete or incomplete. Client render ack not required. Voice playback states are P2.
6. P2: playback-confirmed ranges reference `agent_output_id` and Decision. P0 has no voice playback.
7. P0 message timing is Session runtime + ADR-011. Agent does not own wall-clock presentation. IC owns later voice timing.
8. Runtime-owned ids plus optional same-Decision local refs; no positional “below” authority.
9. Session runtime owns Decision/output/message publication state; Evaluation owns Evaluation; events are append-only projections.
10. v1 `emit_message` -> `respond` + one message; v1 `no_action` -> explicit `no_action` + zero outputs.
11. Parent runtime: successor schema, provider port, worker, message effect, additive persistence, tests, synthetic UI. IC/TTS/frontend voice later.
12. Smallest MVP: envelope + P0 profile + v1 dual-read + ADR-011 message path.
13. Extension points: typed outputs, typed requested actions, derived audience, IC seam. Not a generic media protocol.
14. Misleading if kept as forever-shape: v1 `decision_type` as the only Decision; treating SSE as voice; treating Evaluation as a hidden message.
15. Kind vs visibility independent; audience derived server-side (`REQ-SESS-82`).
16. P0: next-timer request. Later tools/transitions/escalation need owning specs.
17. Invocation, Decision, Output, requested action, optional Turn, Agent Message, events. No extra P0 group identity.

# Impact and compatibility matrix

| Surface | Change now | Later |
| --- | --- | --- |
| `agent-decision.v1` | Dual-read mapping only | Unchanged historical evidence |
| Successor envelope schema/C#/TS | Required in parent runtime task before provider/worker | — |
| Applied migrations 0005–0008 | Frozen | Additive successor tables/columns only if needed |
| `EmitMessageAgentDecisionV1` / `NoActionAgentDecisionV1` | Compatibility readers | New envelope writer |
| `P0TextSessionRuntimeCapabilityPolicy` | Add output-kind denials | Voice remain denied |
| Provider port / worker | Consume envelope | No voice adapter |
| Frontend | `UI-SESS-DEC-15` hide internals | P2 shared-workspace/voice UI |
| Misleading abstraction | Do not treat `decision_type` as future cardinality | Do not use message stream as playback |

Migration choice: explicit successor (v2 or distinct schema id) plus dual-read of v1. No silent v1 union widening. Rollback is version selection; reconstruction of v1 remains mandatory.

# Implementation phases (runtime task, not this task)

1. Successor envelope schema/fixtures/C#/TS parity and v1 dual-read tests.
2. Domain validation of P0 profile, output ids, independent per-item
   output/action validation, and partial rejection (`AC-SESS-48`).
3. Additive persistence if envelope cannot be stored in current Decision payload without meaning change.
4. Provider port + fake adapter using envelope.
5. Worker Decision/effect including message output -> ADR-011.
6. Synthetic/UI projections honoring `UI-SESS-DEC-15`.
7. Security/isolation/cardinality/v1 reconstruction regression.

Later P2 authoring, then later implementation, remain separate ordered work.

# Proposed P2 authoring handoff

Retain until the MVP slice works end to end. Do not populate
`docs/requirements/features/voice-interaction-interruption.md` or a new UI spec
now.

Scenarios A–G, permissions, failure/recovery, audit, security, accessibility,
performance, compatibility, and rollout inputs:

- A: voice-only conversational output; no required persistent rich message.
- B: already approved as P0 message-only.
- C: coordinated voice+message with independent ids/order/delivery and explicit refs.
- D: interrupt voice; keep authorized message; only playback-confirmed speech enters continuity.
- E: silent/internal requested actions remain distinct from `no_action`.
- F: IC signals need trusted admission; not automatic Invocations.
- G: reconstruct generated, shown, spoken, heard, acted-upon, grouped outputs without hidden CoT.

Proposed UI/UX outline (not approved): voice-only, message-only, combined, and
no-output journeys; shared-workspace grouping; pending/partial/failure/
interruption/reconnect/permission/terminal; captions that distinguish generated
from playback-confirmed; non-audio alternatives; reduced motion; touch;
400-percent reflow; desktop/narrow layouts; protected reviewer/runtime content
never in Participant UI. Author the owning UI spec only after P2 requirements
are approved. Design-system `voice.md` does not enable the capability.

# Independent reviews (2026-08-14)

| Perspective | Result | Notes |
| --- | --- | --- |
| Product/BA | pass | Envelope meaning in concept model v0.4; P0 not widened; no eighth feature |
| UI/UX | pass | `UI-SESS-DEC-15` hides internals; P2 outline stays Proposed |
| Architecture | pass | ADR-014 extends 012/013; preserves 011; v1 immutable; independent item validation is P0 |
| Security/privacy | pass | Threat table in ADR-014; audience/id fail-closed; Evidence not a message |
| Backend feasibility | pass | Dual-read + successor schema is implementable without rewriting 0005–0008 |
| Frontend feasibility | pass | No new Participant states required in P0 |
| QA/testability | pass | AC-SESS-42–48 and AC-RSC-28 are Given/When/Then; AC-SESS-48 covers mixed valid message + prohibited voice |

Blocking contradiction found and resolved during authoring: empty outputs must
not equal `no_action`. Post-commit contradiction resolved: P0 profile excess is
per-item rejection, not Decision-wide atomic rejection. No remaining P0
blocker.

# Decisions

- `PROP-MCO-1`–`6`, `13`–`16` are approved for P0 in product/requirements/ADR-014.
- Independent output/action validation with partial rejection is the P0 rule
  (`REQ-SESS-78`/`79`, `SESS-DEC-30`/`35`, `AC-SESS-48`). P0 does not impose
  Decision-wide output atomicity.
- Schema-invalid/incomplete provider output is an Invocation execution outcome
  (`REQ-SESS-63`). Schema-valid `respond` with zero valid outputs is a Decision
  whose communication/output validation fails (`AC-SESS-43`).
- `PROP-MCO-7`–`12` remain Proposed P2/later inputs except where they restate
  already-approved P0 rules (trusted admission, ADR-011 message path, runtime
  timing authority, no dual writes).
- This documentation task does not implement runtime code.
- Parent runtime task is unblocked for envelope-aware provider/worker work.

# Findings / deviations

- Approved cardinality fix: envelope instead of v1 discriminator as the future
  shape, with v1 kept reconstructable.
- Evaluator-facing proposal examples remain out of generic message outputs.
- P2 authoring sequence unchanged.
- Post-commit review: closed the `REQ-SESS-79` Decision-atomic contradiction in
  favor of independent item validation; split schema-invalid execution from
  Decision rejection in `AC-SESS-43`; corrected stale Text Session v0.4 catalog
  references.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Plan-only consistency review | passed | 2026-08-14 plan remediations retained |
| Proposal section/question/scenario traceability | passed | Question answers, design-coverage matrix, principles 1–8 mapped to ADR-014/product/P2 handoff |
| Current repository flow and impact inventory | passed | Inventory and impact matrix in this file |
| Product/requirements/UI/architecture authority | passed | Concept/MVP/overview v0.4; RSC v0.4; Session v0.5; UI v0.5; runtime v0.5; architecture v0.10; ADR-014 |
| Security/privacy threat model | passed | ADR-014 security table plus AC-SESS-44/46/47/48 |
| Compatibility/migration review | passed | v1 immutable; successor + dual-read; migrations 0005–0008 untouched |
| P0 versus later-release boundary | passed | No voice/tools/reviewer outputs enabled |
| Independent cross-functional reviews | passed | Review table above |
| Documentation, links, IDs, diff hygiene | passed | `python3 scripts/check_docs.py` passed; `git diff --check` clean |
| Post-commit review P1–P3 (2026-08-14) | passed | Independent item validation in REQ-SESS-78/79, ADR-014 layers, SESS-DEC-30/35, AC-SESS-43/44/45/48; Text Session catalogs v0.5; `python3 scripts/check_docs.py` passed; `git diff --check` clean |

# Blockers

None. P2 voice requirements and UI authoring remain gated until the MVP slice
works end to end.

# Completion

- [x] Every proposal section, question, requested deliverable, scenario,
      principle, and non-goal is mapped to an authoritative decision or an
      explicit deferred item
- [x] Product meaning and release scope are approved without silently widening
      text-only P0
- [x] Required P0 observable behavior change is approved in existing owning
      specifications with stable IDs; no eighth P0 feature
- [x] Proposed P2 voice requirements and UI/UX authoring inputs cover A–G
      without a false approval claim
- [x] P0 architecture, ownership, authorization, delivery, failure,
      compatibility, and migration decisions are approved; voice mechanics remain
      Proposed for P2
- [x] Hidden reasoning, Evidence/Evaluation/review, audience, and sensitive-data
      boundaries are explicit
- [x] Current v1 history remains reconstructable and frozen migrations remain
      unchanged
- [x] Exact P0 compatibility amendment is incorporated into the parent runtime
      plan and traceability matrix
- [x] Later voice/multi-channel work is retained as separate ordered future work
- [x] Documentation validation evidence recorded after `check_docs.py`
- [x] Remaining open questions and deferred capabilities are recorded honestly
- [x] Task state is safe and complete for external review
