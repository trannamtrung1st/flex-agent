# Proposal: Multi-Channel Agent Output for Voice + Rich Message Experiences

## Status

Proposed

## Purpose

Define a structured agent-output model that allows a single agent turn to produce coordinated outputs across multiple interaction channels, especially:

- natural, interruptible voice
- persistent rich chat content
- internal or control-oriented actions that are not shown directly to the participant

This proposal is intended to guide implementation planning. It describes the desired product behavior and architectural direction, but does not prescribe a final schema or code structure.

---

## Background

The product supports both text and natural, interruptible voice interaction.

These two interaction modes serve different purposes:

- **Voice** is best for natural conversation, pacing, clarification, follow-up questions, and maintaining a fluid human-like interaction.
- **Chat/UI content** is better for detailed explanations, structured information, code, tables, diagrams, evidence, references, generated artifacts, and information the participant may want to inspect later.

Treating every agent response as a single text message creates unnecessary limitations.

Likewise, treating voice as merely text that is converted to speech makes the voice experience too verbose and forces the participant to listen to content that is better presented visually.

The agent should instead be able to decide how each response should be presented.

---

## Core Proposal

A single agent turn should be able to produce:

1. a high-level **decision or action**
2. zero or more **presentation outputs**
3. optional **internal/control outputs**

The key principle is:

> An agent invocation produces a structured decision that may contain coordinated outputs and actions, rather than merely returning a text message.

A participant-facing conversational turn may be created from that decision, but not every agent invocation must become a conversational turn.

Conceptually:

```text
Agent Invocation
│
└── Agent Decision
    │
    ├── presentation outputs
    │   ├── voice
    │   │   └── conversational spoken content
    │   └── message
    │       └── persistent rich UI content
    │
    └── actions / control intent
        └── wait, stay silent, request runtime action, state transition, etc.
```

Not every category or output must exist for every invocation.

The exact schema should be determined during implementation planning. The proposal intentionally distinguishes the semantic decision returned by the agent from the runtime effects that execute it.

---

## Terminology

To avoid conflating model execution with human conversation:

- **Agent invocation**: one execution of the agent caused by a structured input/event.
- **Agent decision**: the structured semantic result of that invocation.
- **Output**: content produced by the decision for a presentation or structured-data channel.
- **Requested action**: an action the agent asks the harness/runtime to consider executing.
- **Conversational turn/response**: the participant-facing interaction that may result from one or more outputs.

The implementation may choose different final names, but it should preserve these conceptual distinctions.

---

## Voice and Message Are Different Representations

`voice` and `message` should not be treated as duplicate versions of the same content.

They serve different interaction goals.

### Voice

Voice should be optimized for:

- natural conversation
- brevity
- pacing
- turn-taking
- interruption
- references to visual content
- conversational explanation
- follow-up questions
- directing participant attention

Example:

```text
"I found one important issue with the retry behavior.
I've put the cases in the chat — the authentication case is the one I'd change first."
```

### Message

Message content should be optimized for:

- persistence
- precision
- scanability
- detailed explanations
- tables
- code
- diagrams
- citations
- evidence
- references
- generated artifacts
- interactive or rich UI elements

Example:

```markdown
### Retry behavior

`retry()` currently retries all failures, including non-recoverable errors.

| Error | Retry? |
|---|---|
| Timeout | Yes |
| 503 | Yes |
| Invalid input | No |
| Authentication failure | No |
```

The voice may refer to the message without reading it aloud.

---

## Supported Output Combinations

The system should support at least the following semantic combinations.

### Voice only

Useful for lightweight conversational turns.

Example:

```text
"Yes, exactly. What made you choose that approach?"
```

No persistent rich message is required.

---

### Message only

Useful when the agent wants to add persistent information without speaking.

Examples:

- generated rubric
- code snippet
- diagram
- comparison table
- evidence block
- evaluation details
- reference material

---

### Voice + message

This is a primary target behavior.

Example:

Voice:

```text
"I've compared the two approaches below.
The biggest difference is how they handle session state."
```

Message:

```markdown
| Area | Approach A | Approach B |
|---|---|---|
| Session state | Local | Centralized |
| Recovery | Limited | Event-backed |
| Auditability | Medium | High |
```

The two outputs should be treated as coordinated parts of the same logical agent decision and, when participant-facing, the same conversational response.

---

### No participant-facing output

The agent may decide not to speak or show a participant-facing message.

Possible reasons include:

- stay silent
- wait for more participant input
- request a workflow or runtime action
- request a state transition
- invoke or request a tool operation where the harness/runtime permits it
- react to an Interaction Controller event without addressing the participant

The structured output model should not assume that every agent invocation results in participant-visible content, nor that every requested action is executed directly by the agent. Runtime and harness policy remain authoritative for execution.

---

## Integration with Structured Agent Input

This proposal complements the broader structured-input model.

Agent input should not be limited to user messages.

Future inputs may include events from the Interaction Controller or other runtime systems, for example:

```text
participant has been silent for 8 seconds
participant interrupted agent playback
participant resumed speaking
voice playback completed
time remaining changed
workflow stage changed
tool result arrived
```

The agent may then decide whether to:

- speak
- display a rich message
- do both
- remain silent
- perform another action

This means both agent input and agent output should be modeled around semantic events and decisions rather than a simple request/response text API.

---

## Interaction Controller Responsibilities

The Interaction Controller should continue to own real-time conversational mechanics such as:

- speaker turns
- silence detection
- timing
- interruption
- floor handoff
- playback state
- partial voice playback
- what spoken content was actually heard

The agent should express **voice presentation intent**, while the Interaction Controller owns real-time delivery behavior.

Conceptually:

```text
agent voice output
      ↓
Interaction Controller
      ↓
TTS / audio generation
      ↓
audio playback
```

The agent should not need to model provider-specific audio details.

---

## Message Delivery Responsibilities

Persistent message content should have delivery semantics separate from voice, even when both outputs belong to the same logical agent decision.

Conceptually:

```text
agent message output
      ↓
session/event processing
      ↓
conversation UI
      ↓
rich rendering
```

The message may later support:

- Markdown
- code blocks
- tables
- citations
- diagrams
- attachments
- generated artifacts
- interactive components
- structured evaluation content

The protocol should avoid assuming that a "message" is always a plain text chat bubble.

---

## Interruption Semantics

Voice and message have different delivery semantics and should be tracked independently.

Example agent turn:

Voice:

```text
"Take a look at the diagram I added.
The request enters here, then the interaction controller..."
```

Message:

```text
[diagram]
```

If the participant interrupts after hearing:

```text
"Take a look at the diagram I added."
```

then:

- the message may already have been presented and remains available
- only the played portion of the voice counts as heard
- the unplayed voice content must not be treated as delivered to the participant
- the next agent turn should be able to reason from actual delivery state

Conceptually:

```text
message -> presented / persisted

voice -> streamed / partially consumed / interruptible
```

This must remain consistent with the existing principle:

> Only played agent voice content is treated as heard.

---

## Shared-Workspace Interaction Model

The desired experience is closer to an AI participant using both conversation and a shared display.

Example:

Agent voice:

```text
"There's an interesting pattern in your answer. Let me show you."
```

A diagram appears in the conversation UI.

Agent voice continues:

```text
"These first two components are strongly coupled.
That third component is actually independent."
```

Participant interrupts:

```text
"Why do you think the third one is independent?"
```

The agent answers naturally while the visual remains available as shared context.

This should feel like talking with an AI that can also place supporting material into a shared workspace, rather than a voice chatbot that simply reads chat messages aloud.

---

## Semantic Presentation Intent vs Transport

The agent output contract should describe semantic presentation intent, not transport implementation.

Prefer concepts such as:

```text
voice
message
```

rather than:

```text
text
audio
```

Reason:

`audio` is a delivery format, while `voice` represents the agent's intent to say something conversationally.

For example:

```text
voice
  ↓
Interaction Controller
  ↓
TTS provider
  ↓
audio stream
```

The TTS provider may change without affecting the agent contract.

Similarly, `message` can evolve from Markdown into richer UI without requiring the agent protocol to be redesigned around frontend transport formats.

---

## Agent Decision as the Primary Abstraction

The protocol should not assume that an agent invocation equals one chat message.

A more general conceptual model is:

```text
Agent Invocation
      ↓
Agent Decision
      ↓
Zero or more coordinated outputs/actions
```

Possible high-level decisions may eventually include concepts such as:

```text
respond
stay_silent
wait
invoke_tool
update_state
request_control_action
proactively_speak
```

These are examples only.

The implementation-planning phase should determine whether these concepts belong in:

- one decision enum
- an action list
- typed output variants
- another structured representation

The proposal intentionally does not lock in the final schema.

---

## Coordination Between Voice and Message

When both voice and message are produced in one turn, the runtime should preserve their relationship.

Useful properties may include:

- common turn identifier
- output identifiers
- ordering
- presentation timing
- references between outputs
- delivery state
- playback state
- interruption state

For example, voice may semantically refer to a specific message or artifact:

```text
"I've highlighted the relevant section below."
```

The implementation should make it possible to know what "below" refers to, especially for event tracking, replay, auditing, and future multimodal UI behavior.

Whether explicit references are required in the initial MVP should be decided during planning.

---

## Persistence and Auditability

The session event log should preserve enough information to reconstruct what happened.

For a multi-channel agent decision, the system may need to record:

- agent decision
- generated voice content
- generated message content
- internal/control actions
- when each output was emitted
- whether the message was presented
- voice playback progress
- interruptions
- what voice content was actually heard
- relevant tool executions
- state changes

This is important for:

- reproducibility
- evaluation
- session review
- debugging
- evidence tracking
- future agent learning
- harness improvement

The implementation should remain consistent with the existing authoritative session-state and event-log architecture.

---

## Assessment-Specific Benefits

For the assessment MVP, this model enables the examiner agent to behave more naturally.

Examples include:

### Asking a question verbally while showing reference material

Voice:

```text
"Take a look at these two implementations.
Which one would you choose and why?"
```

Message:

```text
[implementation A]

[implementation B]
```

### Giving concise verbal guidance while showing detailed evidence

Voice:

```text
"Your reasoning is mostly correct, but there's one assumption I'd challenge."
```

Message:

```markdown
### Relevant evidence

- Candidate stated X
- Submission shows Y
- These conflict under condition Z
```

### Producing evaluator-facing information without reading it aloud

Voice:

```text
"That's enough for this section. We'll move to the next topic."
```

Separate non-participant-facing output:

```text
[structured evidence/evaluation data for authorized reviewer or runtime use]
```

This illustrates that presentation channel and visibility are separate concerns: participant `message` output should not be overloaded to represent reviewer-only or internal data.

The same foundation should remain reusable for interviews, coaching, project reviews, support, and other future campaign types.

---

## Separation of Generated Content and Visibility

Not every agent output should automatically become participant-visible.

The implementation should distinguish at least two independent dimensions:

1. **What kind of output/action is this?**
   - voice presentation
   - persistent message/rich content
   - structured evaluation/evidence
   - control/runtime request
   - other typed output

2. **Who, if anyone, may see it?**
   - participant
   - reviewer/admin
   - runtime only
   - another explicitly authorized audience

This is especially important for evaluation and assessment use cases where the agent may produce:

- evaluator notes
- evidence
- scores
- confidence
- concise decision rationale or review metadata
- follow-up strategy
- workflow decisions

without exposing all of it to the participant.

The system should not depend on storing or exposing hidden chain-of-thought. If rationale is needed for audit or review, it should be an explicitly designed, concise structured explanation or evidence-backed summary.

The exact visibility/authorization model should be designed carefully during implementation planning and enforced by the runtime, not trusted solely to model-generated labels.

---

## Backward Compatibility

The implementation plan should consider how existing text-only behavior maps into the new model.

A likely compatibility path is:

```text
existing assistant text response
        ↓
message output
```

Voice-only and voice+message behavior can then be introduced without requiring all existing flows to change immediately.

The planning agent should verify the actual current repository architecture before deciding the migration strategy.

---

## Non-Goals

This proposal does not attempt to define:

- the final TypeScript schema
- exact database tables
- exact event names
- exact API endpoints
- TTS provider implementation
- frontend component design
- unrestricted full-duplex voice
- visual workflow builders
- multi-agent collaboration
- a generic multimodal protocol for every future media type

Those decisions should follow repository review and implementation planning.

---

## Design Principles

Implementation should preserve the following principles.

### 1. Agent output is semantic

The agent expresses what it intends to communicate or request.

The harness and runtime validate, authorize, execute, persist, and transport those intents.

### 2. Voice is conversational

Voice should not simply read persistent content aloud.

### 3. Message content is persistent and rich

Detailed information belongs in the UI when that improves comprehension.

### 4. Outputs are coordinated

Voice and message from the same turn should remain logically connected.

### 5. Delivery state matters

Generated content and delivered/heard content are not the same thing.

### 6. Interruption remains first-class

Only played spoken content counts as heard.

### 7. Presentation channel and visibility are separate

Not all generated outputs are participant-visible, and reviewer/runtime-only information should not be modeled as ordinary participant messages.

### 8. The model must remain reusable

The design should support assessment first without coupling the protocol tightly to assessment-specific behavior.

---

## Questions for Implementation Planning

The coding agent should review the repository and propose answers to at least the following.

1. Where is the current agent request/response contract defined?

2. Where should the new structured agent input/output types live?

3. Should one agent decision contain:
   - optional `voice`
   - optional participant-facing `message`
   - typed non-presentation outputs
   - one high-level decision/disposition
   - a list of requested actions
   - or another structure?

4. How should voice and message outputs be represented in the session event log?

5. How should delivery state be represented?

6. How should partial voice playback and interruptions reference the original agent turn/output?

7. Should message presentation timing be controlled by the agent, Interaction Controller, session runtime, or UI?

8. How should a voice output reference a message, artifact, or visual when necessary, without relying only on fragile positional language such as "below"?

9. Which outputs should be stored as session state versus append-only events?

10. How should current text-only agent responses map into the new structure?

11. What changes are required in:
    - agent runtime
    - session runtime
    - event model
    - Interaction Controller
    - TTS pipeline
    - frontend
    - persistence
    - tests

12. What is the smallest MVP-compatible implementation that does not over-engineer future channels?

13. What future extension points should be deliberately preserved?

14. Which current abstractions would become misleading or redundant after this change?

15. How should visibility and authorization be represented independently from presentation channel?

16. Which agent-requested actions require harness/runtime validation before execution?

17. What terminology should represent the logical grouping: invocation, decision, response, conversational turn, and emitted events?

---

## Requested Planning Task

Review the current repository before proposing implementation.

Produce an implementation plan that:

1. identifies the existing request/response, session-event, voice, and message flows
2. maps this proposal onto the current architecture
3. identifies files/modules/types that should change
4. proposes the structured agent-output model
5. proposes any corresponding structured agent-input changes needed now
6. distinguishes presentation channel from visibility/authorization
7. defines event and delivery semantics
8. explains Interaction Controller responsibilities
9. explains harness, session-runtime, frontend, and TTS responsibilities
10. preserves interruption and "actually heard" semantics
11. includes backward-compatibility/migration strategy
12. separates MVP work from future extensions
13. identifies risks and unresolved design decisions
14. proposes test coverage
15. breaks implementation into ordered, reviewable phases

Do not start implementation until the plan has been reviewed.

Prefer the smallest coherent architecture that supports:

```text
voice only
message only
voice + message
no participant-facing output
```

while remaining extensible to future agent decisions and output channels.

---

## Acceptance Criteria for the Planned Design

The resulting implementation design should make all of the following possible.

### Scenario A — conversational voice only

The agent asks a short follow-up question by voice without creating unnecessary persistent rich content.

### Scenario B — persistent message only

The agent places detailed information in the conversation UI without speaking.

### Scenario C — coordinated voice + message

The agent speaks naturally while presenting supporting rich content in the UI.

### Scenario D — interruption

The participant interrupts voice playback. The system records only the played portion as heard while leaving already-presented message content intact.

### Scenario E — silent/internal action

The agent can make a decision or update state without producing participant-visible output.

### Scenario F — non-user runtime input

The agent can eventually receive an Interaction Controller event, such as participant silence, and decide whether to speak, display content, act, or remain silent.

### Scenario G — audit reconstruction

A reviewer can reconstruct:

- what the agent generated
- what was shown
- what was spoken
- what was actually heard
- what actions occurred
- which outputs belonged to the same agent turn

---

## Summary

The desired direction is:

> A single agent turn may produce multiple coordinated outputs for different interaction channels.

Voice is a transient-in-presentation, conversational, interruptible channel whose generated and played state can still be recorded for auditability.

Message content is a persistent, rich-information channel.

Other structured outputs and requested actions may exist without being exposed to the participant. Visibility is orthogonal to presentation channel and must be enforced by runtime authorization.

The agent protocol should express semantic intent rather than transport details, while the harness, Interaction Controller, session runtime, UI, and TTS pipeline validate and handle execution and delivery according to their responsibilities.

This model should provide a strong foundation for combining natural voice interaction with a rich shared conversational workspace without forcing one channel to behave like the other.
