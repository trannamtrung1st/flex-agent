# ADR-012: Structured Agent Invocation and Decision runtime boundary

## Status

Approved

## Decision metadata

| Field | Value |
| --- | --- |
| **Owner** | Architecture Lead |
| **Required approvers** | Product Lead, Architecture Lead, Security/Privacy reviewer |
| **Consulted perspectives** | Business analysis, architecture, UI/UX, security/privacy, documentation |
| **Approved date** | 2026-08-11 |
| **Governs** | Provider-neutral Agent Invocation, trusted triggers, Agent Decisions, validation/effect boundary, intentional no-action, ordering, retry, provenance, and streaming coexistence |
| **Upstream sources** | Approved [concept model v0.2](../../product/concept-model.md), [MVP scope v0.2](../../product/mvp-scope.md), [resolved Session configuration v0.2](../../requirements/features/resolved-session-configuration.md), and [text Session lifecycle v0.3](../../requirements/features/session-text-lifecycle.md) |
| **Preserves** | [ADR-001](ADR-001-resolved-configuration-representation-and-integrity.md), [ADR-002](ADR-002-authorization-enforcement-and-delegation.md), [ADR-003](ADR-003-authorization-audit-persistence.md), [ADR-005](ADR-005-atomic-attempt-start-and-submission-binding.md), [ADR-009](ADR-009-mvp-session-evaluation-review-contracts.md), and [ADR-011](ADR-011-participant-visible-agent-response-streaming.md) |
| **Extended by** | [ADR-013](ADR-013-agent-requested-next-timer-replacement.md) permits one optional independently validated next-timer recommendation without making the Agent scheduler or trigger authority. [ADR-014](ADR-014-agent-output-envelope-and-p0-compatibility.md) specializes the P0 Decision as a compatibility profile of a typed output/action envelope without enabling voice or additional actions |

This ADR and its approved upstream product and requirement revisions govern
structured Agent Invocation/Decision implementation as specialized by
[ADR-014](ADR-014-agent-output-envelope-and-p0-compatibility.md) for the P0
Decision-output envelope.

## Context

The approved P0 Session path is centered on an accepted Participant message, a
response slot, model generation, and participant-visible Agent content. The
broader platform also needs governed Agent reasoning after workflow events,
future interaction signals, timers, tool results, and system events. Treating
all reasoning as a chat request/response would conflate:

- why the Agent ran;
- what semantic behavior the Agent recommended;
- lower-level provider requests and retries;
- conversational Turns and response slots;
- participant-visible content; and
- authoritative workflow or Session effects.

The boundary must support successful intentional silence and future
Agent-initiated behavior without making the Agent a self-scheduling authority.
It must also coexist with ADR-011: every participant-visible text delta remains
durable before display and cannot be buffered into one large structured JSON
response or exposed as incomplete control syntax.

## Decision drivers

- Preserve Session and workflow authority and treat model output as untrusted.
- Keep domain semantics independent of providers and provider-native tool-call,
  JSON-mode, finish-reason, or streaming formats.
- Distinguish intentional no-action from provider failure, cancellation,
  rejection, and pending work.
- Preserve one authoritative order, idempotency, cutoff, replay, and exact
  Participant exposure.
- Freeze behaviorally material trigger and decision policy for assessment
  fairness.
- Bound trigger duplication, chained invocations, tool loops, playback loops,
  and resource consumption.
- Reconstruct material Agent-driven effects while minimizing protected data and
  never requiring hidden chain-of-thought.
- Introduce the foundation without enabling deferred voice, tools,
  silence-driven/arbitrary proactivity, or richer workflow configuration in P0.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Keep message → model → response as the only Agent contract | Smallest immediate P0 change | Makes future triggers, no-action, tools, and workflow proposals invent competing contracts |
| Use provider request/response or provider-native tool calls as the domain model | Fast for one adapter | Leaks provider semantics into authority, versioning, retry, and portability |
| Treat every raw signal as a durable Agent invocation | Simple event counting | Excessive volume, retention, privacy exposure, and false implication that every signal required reasoning |
| Use one structured object containing decision and complete message content | Simple final parsing | Forces complete-response buffering or unsafe partial JSON exposure and conflicts with ADR-011 |
| Introduce provider-neutral Invocation, Decision, validation, and effect boundaries | Stable semantics, explicit authority, future reuse, streaming compatibility | Adds identities, state transitions, schemas, provenance, validation, and reconciliation work |

## Decision

### Use a provider-neutral semantic boundary

Adopt the logical flow:

```text
trusted event, signal, or Participant input
  -> runtime eligibility and policy check
  -> Agent Invocation
  -> bounded Agent execution attempts
  -> one Agent Decision, or an invocation execution outcome
  -> decision validation
  -> authorized effect, rejected/suppressed request, or explicit no domain effect
```

An **Agent Invocation** is one admitted semantic decision opportunity. An
**Agent Decision** is one structured recommendation produced by a successful
Invocation and then independently validated by the runtime. Neither a provider
response nor a Decision is authoritative platform state.

The initial logical decision vocabulary is:

- `emit_message` — recommend participant-visible communication;
- `no_action` — intentionally recommend no participant-visible, workflow, tool,
  or other domain effect;
- `request_tool` — recommend a future permitted tool operation;
- `propose_transition` — recommend a workflow transition; and
- `escalate` — recommend governed human or system attention.

These names establish semantic primitives, not a final wire enum. Versioned
schemas may add compatible detail or later decision types only through the
normal product, requirements, and architecture process. P0 permits only the
subset frozen for approved text Session behavior.

ADR-013 later permits one optional `next_timer_request` control on a successful
Decision. It is independently validated and does not change the exactly-one
Decision rule or make model output a trusted timer trigger.

### Admit only trusted typed triggers

The runtime owns Invocation admission. Each admitted Invocation contains or
references:

- stable `agent_invocation_id` allocated in the authoritative admission commit;
- Invocation contract version and purpose;
- typed, versioned trigger family/type;
- stable trigger identity and provenance when available;
- trusted Session/configuration ownership;
- minimized authorized context or protected references;
- permitted decision capabilities and validation-policy version;
- idempotency and authoritative ordering facts; and
- execution status plus attempt/Decision references.

Conceptual trigger families are `participant_input`, `interaction_signal`,
`workflow_event`, `timer_event`, `tool_result`, and `system_event`. Model-authored
content cannot establish a trigger or trusted trigger facts. Unknown,
unsupported, stale, duplicate-conflicting, or prohibited triggers fail closed
before external model work.

Raw controller, timer, playback, speech, or operational signals do not become
Agent Invocations merely because they occur. The runtime first applies frozen
eligibility, deduplication, cooldown, lifecycle, authorization, and budget
policy. Only an admitted decision opportunity receives an Invocation identity.

### Persist admitted Invocations, not every raw signal

Every admitted Invocation is a durable, minimized Session record because it may
cause provider disclosure, consume bounded resources, produce a Decision, or
affect a response opportunity. The record uses stable protected references or
reconstructable facts instead of copying complete prompts, transcripts,
Submissions, knowledge, credentials, or controller telemetry.

Signals rejected before admission need only the audit or operational record
required by their governing policy. High-frequency raw interaction telemetry is
not automatically copied into the Session event log or manifest.

### Separate Invocation, execution attempt, provider request, Decision, and effect

Cardinality is:

- one Invocation represents one semantic decision opportunity;
- one Invocation has one or more bounded execution attempts only when retry
  policy permits;
- one execution attempt may use one or more lower-level model/provider requests
  inside the provider-neutral orchestration boundary;
- one successful Invocation produces exactly one Agent Decision;
- infrastructure/provider failure produces no fabricated Decision;
- one Decision may be rejected or accepted for effect execution;
- `emit_message` may drive one response/publication path;
- `no_action` drives no Agent Message; and
- a completed future tool result normally creates a new Invocation.

The existing `generation_attempt_id` remains the identity of an Agent-message
publication attempt for a response slot. It references the driving Invocation
when applicable. A provider-neutral Invocation execution-attempt record may
span or precede message generation; it is not required to be the same identity
as a provider request or `generation_attempt_id`. Exact physical co-location is
deferred to the schema design, but the logical identities and references must
remain distinguishable.

An Invocation is not contained universally by a Turn. Participant-message P0
Invocations reference the existing Turn and response slot. Non-conversational
Invocations may have no Turn. A permitted `emit_message` from a non-Participant
trigger creates or claims an Agent-initiated Turn/publication path through the
Session runtime.

### Validate Decisions independently and trace effects separately

Before any effect, the runtime reauthorizes the service and revalidates:

- complete trusted Organization/Activity/Participant/Attempt/Session scope;
- Session state, authoritative order, pause/revocation, and terminal cutoff;
- frozen Agent, Harness, workflow, trigger, decision, and capability policy;
- decision type and decision-specific schema;
- payload bounds, protected-content rules, and target ownership;
- chain/loop/resource budgets; and
- effect-specific idempotency.

The authoritative history distinguishes:

```text
Agent recommended X
validation accepted, rejected, or suppressed X
authoritative effect Y occurred, failed, or did not occur
```

A rejected Decision is not a provider failure. An accepted Decision whose
effect later fails is not an Invocation execution failure. Errors expose only
bounded safe categories.

An explicit no-domain-effect outcome does not mean that the runtime performs no
write. It still records the successful Decision, validation, lifecycle outcome,
and required audit/manifest/outbox bookkeeping; it creates none of the external
or domain effects the Agent could otherwise recommend.

### Make intentional no-action explicit

`no_action` is a successful Agent Decision. It must never be inferred only from
the absence of an Agent Message or substituted for timeout, invalid output,
cancellation, pre-execution rejection, or a late result.

For a Participant Turn with an existing response slot, the Decision commit
atomically or equivalently:

- records the Decision and accepted validation outcome;
- terminalizes the response slot and Turn as intentional no-action;
- records required manifest/audit/outbox provenance; and
- publishes no Agent Message or response fragment.

Reconnect returns that terminal state and does not restart work merely because
the response slot has no Agent Message. The Participant UI clears working state,
shows no error, and exposes a neutral status only when frozen workflow policy
requires it.

### Keep decision control separate from streamed content

At the Flex Agent contract level, structured decision/control semantics and
participant-visible content are separate channels even when one provider
interaction produces both.

For `emit_message`:

1. the decision discriminator and required bounded control fields become valid;
2. the runtime validates that communication is currently permitted and claims
   the applicable response/publication path;
3. the provider adapter supplies ordered non-overlapping content deltas through
   a provider-neutral interface; and
4. every displayed delta follows ADR-011 validation, first-fragment publisher
   claim, durable commit, SSE/polling delivery, replay, completion/incomplete
   outcome, cutoff, and backpressure rules.

The adapter/orchestrator may use one provider interaction, multiple phases, or
another qualified mechanism. It must not require complete-message buffering,
expose partial structured JSON, allow message content before a valid accepted
communication decision, or make provider-native control events authoritative.

### Preserve ordering, idempotency, retry, and cutoff

Invocation admission allocates authoritative Session order when the Invocation
is behaviorally material. Decision and effect records receive their own
authoritative order when they affect Session behavior, reconstruction,
transcript publication, or audit. Raw low-level signals do not receive a
Session sequence solely because they occurred.

The idempotency boundary combines trusted Session scope, trigger identity and
version, Invocation purpose, and frozen policy. Equivalent duplicates reconcile
to the same Invocation. Mismatched reuse fails without mutation.

Retries occur within the same Invocation until a Decision is durably recorded
or the bounded Invocation outcome is terminal. Once a Decision is recorded,
delivery/reconnect does not re-run reasoning. Effect execution may retry only
through its own idempotent contract. A pause, revocation, terminal intent, or
cutoff that wins before Decision/effect commit prevents the late result from
causing an effect; the execution attempt records a bounded late/cancelled
outcome without fabricating a Decision.

### Freeze fairness- and behavior-relevant policy

The resolved configuration and cohort baseline include or immutably reference,
when behaviorally material:

- Invocation and Decision contract/schema versions;
- permitted trigger types and decision capabilities;
- trigger-specific eligibility and Agent-initiated communication policy;
- intentional no-action policy;
- decision validators;
- attempt, retry, chain, cooldown, and loop bounds; and
- stage-specific restrictions.

Lower scopes may narrow but not widen these values. Mutable runtime settings
cannot enable a new trigger or Decision path for an active cohort or Session.
The P0 profile keeps voice/Interaction Controller triggers, Participant Session
tools, silence-driven behavior, arbitrary or parallel timer lanes, and richer
workflow-trigger configuration disabled. ADR-013 permits one narrow exception:
an explicitly enabled system timer lane whose next event may be replaced by an
independently validated bounded Agent recommendation.

### Bound chaining and self-generated loops

Frozen Harness/workflow policy supplies positive bounds for Invocation
attempts, total/chained Invocations, tool iterations, trigger-specific
eligibility/cooldown, duplicate suppression, per-Turn and per-Session budgets,
and cancellation at pause or terminal state. Self-generated message/playback,
tool-result, or workflow events cannot recursively create unbounded
Invocations. Exact numeric defaults require separately approved policy and are
not selected by this ADR.

### Preserve the future Interaction Controller boundary

The future Interaction Controller owns speech activity, silence measurement,
floor state, interruption detection, playback state, partial-transcript
mechanics, and generated/sent/played/interrupted/cancelled facts. It may supply
trusted minimized interaction facts only after runtime eligibility checks. The
Agent interprets those facts semantically; it does not author them.

Voice continuation context must use authoritative playback-confirmed and
interruption facts, never the full intended/generated message as a proxy for
what the Participant received. This seam does not enable voice in P0.

### Minimize provenance and preserve category boundaries

Participant-visible transcript, Agent Invocation records, Agent Decision
records, raw interaction events, tool records, workflow events, Evidence,
Evaluation, audit, and operational telemetry remain separate categories. A
Decision is not automatically transcript content or Evaluation Evidence.

For a material Agent-driven effect, authorized reconstruction can determine:

- the trusted trigger and applicable resolved configuration;
- minimized context/provenance used;
- execution attempts and provider/model identity where required;
- the successful Decision or bounded failure outcome;
- validation result;
- authoritative domain effect or explicit no-domain-effect outcome; and
- exact Participant-visible content under ADR-011.

No record requires, stores, or exposes hidden chain-of-thought. Logs, metrics,
traces, generic events, and broker payloads use bounded categories and protected
references rather than raw sensitive content.

## Security and privacy consequences

Required controls and verification include:

| Threat or privacy harm | Required control | Minimum verification |
| --- | --- | --- |
| Model-authored fake trigger or authority | Trigger provenance only from trusted runtime state/adapters; independent Decision validation | Prompt/context attempts to forge trigger, scope, timing, tool approval, workflow authority, or Release |
| Indirect prompt injection through Participant, Submission, knowledge, or retrieved content | Label untrusted context, keep it outside control/provenance channels, minimize retrieval, and independently validate every Decision/effect | Attempts to override policy, exfiltrate protected context, fabricate trusted facts, or cross Session/resource scope |
| Cross-Session or cross-Participant context contamination | Trusted parent-chain resolution, minimized context builder, scoped protected references | Wrong Organization/Activity/Participant/Attempt/Session retrieval and substituted-reference tests |
| Duplicate/replayed/stale trigger | Scoped idempotency, trigger identity/version, expected Session order, cutoff checks | Equivalent duplicate, mismatched reuse, stale signal, late callback, and multiple-worker races |
| Tool/workflow escalation | Frozen allowlists and independent effect-specific authorization/validation | Prohibited tool, transition, memory, evaluation, and Release Decisions cause no effect |
| Signal or Agent loop storm | Positive chain/cooldown/budget bounds and self-event suppression | Tool-result, playback, no-action, and Agent-message feedback-loop tests |
| Malformed structured output | Versioned schema, bounded parsing, fail-closed classification, no partial control exposure | Unknown type/version, oversized/deep payload, malformed discriminator, split-stream control tests |
| Protected content duplication | Stable references, data minimization, bounded telemetry, lifecycle policy | Log/event/manifest/export leakage tests and lifecycle verification |
| Revocation or cutoff during reasoning | Reauthorization at Decision/effect commit and authoritative order race | Pause, revocation, completion, expiry, termination, and abort at each boundary |

## Consequences

- The Agent domain contract becomes broader than chat while remaining governed
  and provider-neutral.
- The runtime requires new logical Invocation, Decision, validation, effect, and
  explicit no-action state; machine-readable schemas and persistence are later
  implementation work.
- Admitted Invocations add durable write and provenance volume, offset by not
  persisting every raw signal and by referencing protected source material.
- Existing Turn/response-slot/generation/message/fragment identities remain;
  `agent_invocation_id` is linked rather than inserted mechanically into their
  containment hierarchy.
- ADR-011 remains unchanged and authoritative for all participant-visible text.
- Current P0 scope does not gain voice, tools, silence-driven/arbitrary timers,
  parallel timer lanes, or richer configurable workflows. ADR-013 separately
  permits one frozen-policy next-timer replacement lane.
- Future voice, tool, and workflow specifications must consume this boundary
  instead of defining competing Agent request/response semantics.
- Implementation must follow specification-driven TDD and satisfy the
  verification gates below.

## Verification required before implementation acceptance

- Invocation/Decision schema compatibility and provider-neutral adapter
  conformance fixtures.
- Trusted-trigger, unknown/prohibited trigger, fake-trigger, and context-
  isolation negative tests.
- Exactly-one Decision, explicit no-action, failure classification, decision
  rejection, and effect-failure tests.
- Duplicate trigger, attempt retry, process death, lost response, stale signal,
  and idempotent effect tests.
- Pause, revocation, and terminal-cutoff races before Decision, before first
  fragment, during streaming, and before non-message effects.
- One- and multi-interaction provider profiles proving decision/control and
  content-stream separation without complete-message buffering or partial
  structured-control exposure.
- Loop/storm, rate/resource exhaustion, protected-content leakage, prompt
  injection, tool/workflow escalation, and wrong-scope tests.
- Exact ADR-011 fragment replay and transcript reconstruction linked to the
  driving Invocation/Decision.
- P0 configuration tests proving every deferred trigger/effect family remains
  disabled.

## Related

- Product: [Concept model](../../product/concept-model.md) and
  [MVP scope](../../product/mvp-scope.md)
- Requirements: [Resolved Session configuration](../../requirements/features/resolved-session-configuration.md)
  and [Text Session lifecycle](../../requirements/features/session-text-lifecycle.md)
- UI/UX: [Text Session interaction specification](../../ui-ux/text-session.md)
- Architecture: [MVP architecture](../mvp-architecture.md) and
  [Text Session runtime contract](../session-runtime-contract.md)
- Streaming: [ADR-011](ADR-011-participant-visible-agent-response-streaming.md)
- Timer replacement: [ADR-013](ADR-013-agent-requested-next-timer-replacement.md)
- Output envelope: [ADR-014](ADR-014-agent-output-envelope-and-p0-compatibility.md)
