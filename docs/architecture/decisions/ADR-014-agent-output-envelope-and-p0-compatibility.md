# ADR-014: Agent Decision output envelope and P0 compatibility profile

## Status

Approved

## Decision metadata

| Field | Value |
| --- | --- |
| **Owner** | Architecture Lead |
| **Required approvers** | Product Lead, Architecture Lead, Security/Privacy reviewer |
| **Consulted perspectives** | Business analysis, architecture, UI/UX, security/privacy, documentation |
| **Approved date** | 2026-08-14 |
| **Governs** | Provider-neutral Agent Decision envelope, typed outputs, requested actions, output identity, presentation-versus-visibility, P0 compatibility profile, historical v1 reconstruction, and the deferred multi-channel extension path |
| **Upstream sources** | Approved [concept model v0.4](../../product/concept-model.md), [MVP scope v0.4](../../product/mvp-scope.md), [resolved Session configuration v0.4](../../requirements/features/resolved-session-configuration.md), and [text Session lifecycle v0.5](../../requirements/features/session-text-lifecycle.md) |
| **Extends** | [ADR-012](ADR-012-structured-agent-invocation-and-decision-boundary.md) and [ADR-013](ADR-013-agent-requested-next-timer-replacement.md) |
| **Preserves** | [ADR-001](ADR-001-resolved-configuration-representation-and-integrity.md), [ADR-002](ADR-002-authorization-enforcement-and-delegation.md), [ADR-003](ADR-003-authorization-audit-persistence.md), [ADR-011](ADR-011-participant-visible-agent-response-streaming.md), and applied migration immutability |

This ADR approves the smallest P0-compatible semantic and technical foundation
needed by structured Agent runtime implementation. It does not approve voice,
Interaction Controller, TTS, rich-content UI, reviewer presentation outputs,
tools, or workflow-transition effects.

## Context

Approved ADR-012 models one successful Invocation as exactly one Agent Decision
with a primary `decision_type` (`emit_message`, `no_action`, or a deferred
action). ADR-013 adds one optional independently validated `next_timer_request`.
Canonical v1 schemas, C# DTOs, frozen runtime policy, and applied PostgreSQL
migrations already encode that discriminator union.

A later multi-channel experience needs one Decision to recommend coordinated
typed outputs and requested actions without making model output authoritative.
Hardening the provider port, worker, and Agent-message stream against the v1
discriminator as the only future shape would freeze the wrong cardinality.

The current v1 contract remains historical evidence. This ADR therefore
introduces an explicit envelope and a restricted P0 profile, rather than
silently widening `agent-decision.v1`.

## Decision drivers

- Preserve exactly one successful Decision per Invocation and explicit
  `no_action`.
- Permit later coordinated voice and message without enabling them in P0.
- Keep presentation kind independent from authorized visibility.
- Keep Evidence, Evaluation, review, Result, and Release outside generic
  presentation outputs.
- Allocate output identity, Session order, and effective audience in the
  runtime, never from model-authored labels.
- Keep ADR-011 as the only P0 participant-visible text publication seam.
- Reconstruct historical v1 Decisions without rewriting applied migrations.
- Prefer a small typed protocol over a generic multimodal or arbitrary-action
  contract.

## Options considered

| Option | Benefits | Costs and risks |
| --- | --- | --- |
| Keep v1 primary `decision_type` as the only Decision shape | Smallest immediate code change | Forces later voice/message coordination into competing identities and conflates presentation with disposition |
| Silently add output arrays onto `agent-decision.v1` | Avoids a new schema version | Changes frozen wire meaning, digests, tests, and historical reconstruction |
| One Decision per output | Simple per-channel records | Breaks the approved exactly-one Decision rule and loses coordination |
| Treat voice as TTS of the same message | Smallest voice implementation | Violates the voice product contract and forces verbose speech |
| Introduce a versioned Decision envelope with a P0 compatibility profile | Preserves v1 history, coordinates later channels, and keeps P0 small | Adds mapping, dual-read, and validation work |
| Design a generic arbitrary-media protocol now | Maximum future flexibility | Over-engineers P0 and weakens fail-closed authorization |

## Decision

### Use one versioned Decision envelope

One successful Invocation still produces exactly one Agent Decision. The
Decision is an envelope containing:

1. an explicit **disposition** so empty collections are never inferred as
   `no_action`;
2. zero or more **typed output recommendations** in runtime-owned order; and
3. zero or more **typed requested actions**, independently validated.

The Agent recommends. Harness, workflow, authorization, and Session runtime
validate and effect. Model-authored content cannot establish identity, order,
audience, timing, capability, or authority.

Logical P0 dispositions remain `respond` (participant-visible communication is
intended) and `no_action` (intentional primary no domain effect). Deferred
dispositions such as tool, transition, or escalation remain representable only
as later typed requested actions under an owning approved requirement. They
stay disabled in P0.

### Restrict P0 to a compatibility profile

The approved P0 text profile is:

- zero or one Participant `message` output;
- zero `voice` outputs;
- no reviewer, administrator, or runtime-only presentation outputs;
- at most one independently validated `next_timer_request` under ADR-013;
- no other requested actions.

Historical v1 `emit_message` maps to disposition `respond` plus one `message`
output. Historical v1 `no_action` maps to disposition `no_action` plus zero
presentation outputs. An optional v1 `next_timer_request` remains a requested
action, not a presentation output and not a synonym for `no_action`.

Absence of a Participant message is not `no_action`. The runtime must
distinguish:

- no presentation output;
- explicit primary `no_action`;
- accepted requested action or control;
- rejection or suppression;
- execution or effect failure;
- Invocation execution failure with no Decision.

### Keep v1 reconstructable and introduce an explicit successor

Do not mutate `agent-decision.v1` meaning or rewrite applied migrations
`0005`–`0008`. Treat v1 as the historical and currently persisted P0
discriminator profile.

The runtime task must introduce an explicit successor contract (v2 Decision
envelope or an additive versioned profile with a distinct schema id) before
provider and worker seams consume Decision shape. Dual-read is required:

- persisted v1 rows remain readable as the mapped P0 profile;
- new writes use the approved successor once that schema exists;
- canonical digests and fixtures for v1 remain valid historical evidence.

Rollback is schema/version selection plus additive migration reversal only
where an additive object exists. Historical v1 reconstruction must remain
possible after either path.

### Allocate output identity and resolve references in the runtime

The runtime allocates every authoritative `agent_output_id` and Session order
at acceptance. The Agent may supply only bounded semantic local references
inside one Decision. The runtime resolves and validates those references before
effect.

Default coordination root is `agent_decision_id`. Turn association remains
optional: Participant-message Invocations keep the existing Turn and response
slot; non-conversational Invocations need not create a Turn. Do not introduce a
separate response-group identity in P0.

Cross-output references are optional in P0 because at most one message output
exists. The successor schema must reserve typed, validated reference slots so
later voice and artifacts can name a sibling output without positional language
such as “below”.

### Separate presentation kind from visibility

Presentation kind (`message`, later `voice`, later typed rich message) is
independent from authorized visibility. Effective audience is derived from
trusted Harness, workflow, and runtime context and only where an owning
approved requirement permits that audience.

P0 effective audience for a permitted `message` output is the Session
Participant. Model-authored `audience`, scope, or visibility labels are
ignored as authority and fail closed when they request a prohibited audience.

Evidence, Evaluation, scores, reviewer notes, concise audit explanations,
workflow decisions, and hidden chain-of-thought are not generic presentation
outputs. They remain in their owning domains unless an approved specification
defines a typed, authorized effect.

### Keep channel delivery independent

Each accepted output has its own delivery lifecycle. P0 uses ADR-011 for the
single `message` output: generated recommendation, accepted validation,
durable fragment publication, server publication, client receipt, completion
or incomplete/cancelled outcome, and cutoff.

Do not equate persistence or SSE publication with proof that a human perceived
the content. A client render acknowledgement is not required for P0; add it
only if a later approved requirement needs that technical proxy, and then
authenticate, scope, order, and deduplicate it.

Voice generated/sent/played/interrupted/cancelled/playback-confirmed states,
Interaction Controller timing, and TTS transport remain Proposed P2 mechanics.
This ADR preserves the seam: the Agent may later recommend voice presentation
intent; the Interaction Controller owns floor, silence, interruption, and
playback; TTS is a replaceable transport; only playback-confirmed content
enters continuity. The seam does not enable voice.

### Preserve current structured-input bounds

P0 continues to admit only the already-approved trusted trigger subset.
Raw Interaction Controller or operational signals do not become Invocations.
Trusted admission, minimization, deduplication, cooldown, budgets, and
feedback-loop suppression remain required.

### Assign one owner per durable fact

| Fact | Authoritative owner | Event/projection role |
| --- | --- | --- |
| Admitted Invocation, Decision envelope, output recommendations, requested-action recommendations | Session runtime | Append-only Session events and minimized manifest references |
| Message publication, fragments, completion | Session runtime under ADR-011 | SSE/replay projections |
| Next-timer schedule | Session runtime under ADR-013 | Schedule revision events |
| Evaluation and Evidence | Evaluation domain | Protected locators, not copied Decision payloads |
| Review, Result, Release | Review/Release domain | Separate from Participant transcript |
| Future voice playback facts | Interaction Controller after P2 approval | Playback-confirmed continuity input |
| Audit | Authorization/audit adapters | Bounded categories and protected references |

Do not dual-write raw sensitive content into audit, telemetry, generic events,
or the execution manifest.

## ADR-011, ADR-012, and ADR-013 relationship

| Clause | Disposition |
| --- | --- |
| ADR-012 exactly-one Decision, trusted triggers, independent validation, explicit `no_action`, ordering, retry, cutoff, provenance minimization, Interaction Controller seam | Remain |
| ADR-012 logical vocabulary `emit_message` / `no_action` as P0 primary behaviors | Specialized: they are the P0 profile mapping of the envelope, not the exclusive future Decision shape |
| ADR-012 `emit_message` may drive one response/publication path | Remains the P0 `message` output effect |
| ADR-012 deferred `request_tool`, `propose_transition`, `escalate` | Remain disabled P0 requested-action extension points, not current effects |
| ADR-013 optional independently validated next-timer replacement | Remains the only P0 requested action |
| ADR-011 durable-before-display fragments, replay, cutoff, backpressure | Remain the only P0 participant-visible text publication contract |
| ADR-011 as the definition of voice playback | Does not; message streaming is not voice delivery |

## Security and privacy consequences

| Threat or privacy harm | Required control | Minimum verification |
| --- | --- | --- |
| Prompt-injected audience, output id, or action request | Runtime-owned IDs, derived audience, independent validation, fail-closed prohibited kinds | Model labels requesting reviewer/admin/runtime-only audience, guessed output ids, or disabled actions cause no effect |
| Reviewer/internal content as a Participant message | Typed owning domains; P0 profile rejects non-Participant presentation | Attempted Evidence/Evaluation/score/reviewer-note outputs are rejected and absent from transcript |
| Output-reference substitution | Resolve local refs inside the same Decision and current Session scope | Cross-Decision, cross-Session, and swapped-artifact references fail |
| Unsafe rich rendering | P0 message remains untrusted text under existing Session rendering rules | Markup/script/link/fetch injection tests already required by `REQ-SESS-19` and `AC-SESS-26` |
| Forged presentation or playback acknowledgements | No P0 client delivery authority; later acks must be authenticated, scoped, ordered, and labeled technical proxies | Client-supplied “shown” or “heard” facts cannot mutate authoritative state in P0 |
| Resource storms from multi-output generation | P0 cardinality bounds; later per-output budgets | More than one message, any voice, or extra actions are rejected |
| Observability cardinality and leakage | Bounded labels; protected references; no raw Decision payloads in logs | Metric/log/export leakage tests |
| Retention/export/backup | Existing lifecycle policy applies to new envelope records | Cross-scope export denial; no silent v1 rewrite |

## Consequences

- Provider, worker, and message-stream implementation must consume the envelope
  and P0 profile rather than treating v1 `decision_type` as the forever shape.
- Existing v1 schemas and applied migrations stay immutable historical
  evidence.
- Participant-visible P0 behavior remains message streaming or explicit
  no-action; no voice or rich-content UI is enabled.
- Later P2 authoring may add voice-only, message-only, coordinated
  voice-plus-message, and silent-action journeys against this envelope without
  inventing a second Agent contract.
- Implementation must follow specification-driven TDD in the parent runtime
  task; this ADR does not itself change production adapters.

## Verification required before implementation acceptance

- Successor schema compatibility fixtures plus lossless v1 dual-read mapping.
- P0 cardinality tests: zero or one message, zero voice, timer independence.
- Empty outputs without explicit `no_action` fail closed.
- Model-authored audience, output id, or prohibited action tests.
- Independent output versus requested-action validation and partial rejection.
- ADR-011 fragment tests still linked to the driving Decision and output id.
- Deferred-capability matrix proving voice, tools, reviewer outputs, and
  Interaction Controller triggers remain disabled.
- Historical v1 reconstruction after additive successor migration.

## Related

- Product: [Concept model](../../product/concept-model.md) and
  [MVP scope](../../product/mvp-scope.md)
- Requirements: [Resolved Session configuration](../../requirements/features/resolved-session-configuration.md)
  and [Text Session lifecycle](../../requirements/features/session-text-lifecycle.md)
- UI/UX: [Text Session interaction specification](../../ui-ux/text-session.md)
- Architecture: [MVP architecture](../mvp-architecture.md) and
  [Text Session runtime contract](../session-runtime-contract.md)
- Streaming: [ADR-011](ADR-011-participant-visible-agent-response-streaming.md)
- Invocation/Decision: [ADR-012](ADR-012-structured-agent-invocation-and-decision-boundary.md)
- Timer replacement: [ADR-013](ADR-013-agent-requested-next-timer-replacement.md)
