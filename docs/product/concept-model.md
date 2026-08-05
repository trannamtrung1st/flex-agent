# Concept model

Canonical product concepts, relationships, lifecycles, and invariants for Flex Agent.

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Accepted baseline v0.1 |
| **Owner** | Product |
| **Approvers** | TBD — formal sign-off pending |
| **Version** | 0.1 |
| **Effective date** | 2026-08-05 |
| **Last reviewed** | 2026-08-05 |
| **Related decisions** | None recorded yet |

**Accepted baseline** means requirements authors may depend on this model for feature specification. Normative system behavior is still governed by approved feature specifications. Changes that alter domain meaning require a new version or superseding product decision.

## Purpose

Flex Agent separates **who the AI is**, **how it operates**, **what activity is running**, and **each isolated interaction**. These concepts are related but intentionally distinct.

## Canonical vocabulary

| Term | Definition |
| --- | --- |
| **Organization** | Tenant boundary for isolation, policy, authorization, and data ownership |
| **Agent** | Reusable AI identity: knowledge defaults, capabilities, communication behavior, and evaluation approach |
| **Agent revision** | Versioned, inspectable state of an agent at a point in time |
| **Harness** | Governed operating instructions: workflow, policy constraints, allowed capability subset, and evaluation procedure |
| **Harness revision** | Versioned, inspectable state of a harness at a point in time |
| **Harness snapshot** | Immutable capture of harness revision content or immutable references with content hashes |
| **Activity** | Generic execution context that binds agent, harness, tasks, participants, and rules for a class of work |
| **Campaign** | Managed multi-participant activity form with shared deadlines, cohorts, and comparable outcomes |
| **Task** | Work expected from a participant within an activity |
| **Enrollment / participation** | A participant's authorized relationship to an activity |
| **Attempt** | A controlled execution attempt; in the MVP, typically maps to one session |
| **Session** | One isolated execution between a resolved configuration and a participant or authorized role |
| **Participant** | Person taking part in a session under activity or authorization rules |
| **Reviewer** | Authorized human who inspects evidence, evaluations, and outcomes |
| **Submission** | Versioned participant-provided material linked to an activity or session |
| **Knowledge source** | Curated reference content used by an agent or harness |
| **Memory candidate** | Proposed reusable learned information awaiting approval |
| **Approved memory** | Retained information available under a defined scope and policy |
| **Harness change proposal** | Suggested controlled-process change to a harness |
| **Calibration example / dataset** | Reviewed evaluation reference material |
| **Session working context** | Temporary, nonpersistent context for the current interaction |
| **Evidence** | Material linked to evaluation and audit (submissions, transcripts, tool results, playback records) |
| **Evaluation** | Internal structured, evidence-backed judgment of session outcomes |
| **Human revision** | Authorized human adjustment to an evaluation, preserving the original output |
| **Result** | Participant-facing outcome released after review |
| **Release** | Audited transition from internal evaluation to visible result |
| **Resolved session configuration** | Frozen effective configuration used by a session |
| **Resolved execution manifest** | Complete record of versions, policies, and references needed to reconstruct and explain a session |

## Core concepts

### Organization

An **organization** (tenant) is the top-level isolation and policy boundary. Organization policy sets non-bypassable limits across all agents, harnesses, activities, and sessions within that organization.

### Agent

A **reusable AI identity** configured and managed by an administrator.

The agent represents the durable conceptual entity that participants interact with. It defines role, capabilities, knowledge defaults, behavioral expectations, and decision-making approach **independently of any single activity or session**.

An agent may define:

- Name and identity
- Role and responsibilities
- Persona and communication style
- Behavioral instructions
- Domain knowledge and reference material defaults
- Available skills and tools (capability declarations)
- Tool-use principles and default procedures
- Interaction and questioning approach
- Safety and operational boundaries
- Escalation behavior
- Evaluation or decision-making behavior defaults
- Evidence requirements and output format defaults
- Confidence and uncertainty behavior
- Human-review requirement defaults
- Default memory mode and memory eligibility controls

The same agent may be used across many activities while retaining a recognizable role and consistent operating identity.

### Harness

A **governed operating environment** that surrounds an agent with instructions, procedures, policies, workflows, validation behavior, memory controls, and evaluation mechanisms for a particular class of activities.

The harness may contain:

- System and role-specific instructions
- Persona guidance and knowledge source bindings
- Allowed skills, tool definitions, and tool permissions (subset of agent capabilities)
- Workflows, session stages, and transition rules
- Rubrics, evaluation procedures, and evidence requirements
- Policies, safety constraints, and validation rules
- Output schemas and completion requirements
- Escalation and human-review rules
- Conversation protocols and voice/interruption policies
- Time-management behavior, memory controls, and learning policies

The agent defines the reusable AI identity. The harness defines the controlled operating environment in which that agent performs a structured activity. They are related but not interchangeable.

### Activity

An **activity** is the generic execution context above individual sessions. It binds an agent, a harness, tasks, participants, limits, rules, and session requirements for a class of work.

Activities may take different deployment forms:

| Form | Meaning |
| --- | --- |
| **Campaign** | Managed multi-participant activity with shared deadlines, cohorts, and comparable outcomes |
| **Direct activity** | Individually initiated session without a full campaign wrapper |
| **Embedded activity** | Activity embedded in another product or workflow |
| **API activity** | Activity triggered programmatically |

A campaign is one activity deployment mechanism, not the limit of the platform model.

An activity may configure:

- Activity type, description, and selected agent/harness
- Current harness revision or pinned harness snapshot
- Memory mode and override rules within permitted bounds
- Tasks, participant population, and cohorts
- Start/end dates, deadlines, duration, and attempt limits
- Submission requirements and activity-specific instructions
- Rubric, scoring, conversation, and follow-up rules
- Tool permissions, voice and interruption policies
- Completion, escalation, human-review, and result-release rules
- Data-retention and consent requirements

Activity configuration extends or constrains the selected agent and harness without redefining the agent's underlying identity. A **campaign** is the managed multi-participant activity form used in the assessment MVP. Activity-specific settings must not silently modify the reusable agent or its base harness. See [Effective configuration resolution](#effective-configuration-resolution).

### Session

One **isolated interaction** between a resolved configuration and:

- One participant
- An authorized reviewer
- Another permitted session role

**Shared multi-participant sessions** (multiple participants in one real-time session) are deferred beyond the assessment MVP. For the MVP, "group" means an administrative **cohort** whose members each receive individual isolated sessions. See [Group and cohort semantics](#group-and-cohort-semantics).

The session is the concrete execution unit in which conversation, tools, workflow, submissions, evidence, and outcomes are recorded.

Each session may contain identifiers, participant and cohort membership, agent and activity references, resolved harness state and snapshot reference, memory mode and policy, submitted work and attachments, text and voice transcripts, workflow state, timing and attempt data, tool executions, interaction events, evidence, evaluation data, human revisions, release status, and audit history.

Many sessions may run concurrently under the same activity or agent. Conversational state, participant information, attachments, tools, working context, evidence, and outcomes must remain isolated unless an activity explicitly permits controlled group interaction in a future release.

Every session resolves against an activity definition. Deployment form (campaign, direct, embedded, or API) affects administration and enrollment, not session isolation rules.

### Participant

A person taking part in a session under activity enrollment or authorization rules.

### Enrollment / participation

An **enrollment** records a participant's authorized relationship to an activity, including permitted attempts, deadlines, cohort membership, and release visibility rules.

### Attempt

An **attempt** is a controlled execution try within enrollment limits. In the MVP, an attempt typically maps to one session. Attempt limits, timing, and outcome linkage are recorded for audit.

### Task

A **task** defines work expected from a participant: instructions, required submissions, time expectations, and completion criteria within an activity.

### Submission

A **submission** is versioned participant-provided material (text, files, or other permitted artifacts) linked to a task, activity, or session. Submissions are preserved for evaluation and audit; later versions do not silently replace earlier ones.

### Reviewer

An authorized human who inspects evidence, evaluations, session configuration, and outcomes; may approve, adjust, or release results when permitted.

### Knowledge, memory, and learning artifacts

These concepts must not be conflated.

| Concept | Meaning |
| --- | --- |
| **Knowledge source** | Curated reference content configured for an agent or harness |
| **Memory candidate** | Proposed reusable learned information awaiting approval |
| **Approved memory** | Retained information available under a defined scope and policy |
| **Harness change proposal** | Suggested controlled-process change to a harness |
| **Calibration example / dataset** | Reviewed evaluation reference material |
| **Session working context** | Temporary, nonpersistent context for the current interaction |

**Memory policy dimensions** (configured independently; Dynamic/Stable are convenient presets):

| Dimension | Meaning |
| --- | --- |
| Read permission | Who and what may retrieve memory |
| Proposal/write permission | Who may propose or write memory |
| Approval requirement | Whether administrative approval is required |
| Reuse scope | Organization, activity, participant, or session boundaries |
| Retention period | How long memory is retained |
| Source eligibility | Which interaction sources may produce memory |

Two primary memory mode presets exist at product level:

| Mode | Meaning |
| --- | --- |
| **Dynamic** | Agent may learn from approved sources subject to memory policies and administrative controls |
| **Stable** | No new long-term learning from the current interaction; configured identity, knowledge, and approved existing memory remain in use |

Memory mode may be configured at agent, harness, activity, or session level within resolution rules. Each session must record the exact memory mode and policy used.

Participant information must not be reused across unrelated participants, sessions, activities, or organizations unless explicitly permitted.

### Evidence

Material linked to evaluation and audit: submissions, conversation references, tool results, interruption and playback records, and other administrator-configured artifacts.

Evidence references should point to stable locations within submitted or recorded material whenever possible.

### Evaluation, human revision, result, and release

These form a distinct chain; they must not be treated as the same object.

```text
Evidence → Evaluation → Human revision → Released result
```

| Concept | Meaning |
| --- | --- |
| **Evaluation** | Internal structured, evidence-backed judgment organized by rubric criterion or another configured decision framework |
| **Human revision** | Authorized human adjustment that preserves the original agent-generated evaluation |
| **Result** | Participant-facing outcome approved for visibility |
| **Release** | Audited transition that makes a result visible to the permitted audience |

Evaluations maintain an auditable connection between the configured rubric, participant submission, conversation transcript, tool results, collected evidence, criterion-level rationale and evidence references, scores or decisions, and provisional feedback. The system should support uncertainty rather than forcing false precision.

The product requires **inspectable justification**, not storage or exposure of hidden model chain-of-thought.

## Effective configuration resolution

Agent, harness, activity, and session layers overlap in several areas (tools, knowledge, evaluation behavior, evidence requirements, output formats, human-review rules, memory behavior). Without explicit resolution rules, different authors will assign the same responsibility to different concepts.

### Responsibility by layer

| Layer | Owns |
| --- | --- |
| **Organization policy** | Non-bypassable limits across all lower scopes |
| **Agent** | Reusable identity, capability declarations, knowledge defaults, communication behavior, evaluation defaults |
| **Harness** | Workflow and policy constraints; allowed capability subset; evaluation procedure; harness snapshots |
| **Activity** | Activity-specific parameters within the harness's permitted schema; participant population; deadlines; cohort rules |
| **Session** | Frozen resolved configuration; does not define new policy |

### Governing rule

> Lower scopes may narrow permissions or supply permitted parameters, but may not widen capabilities beyond an explicitly delegated upper-scope boundary.

### Conflict resolution

When two layers conflict at configuration time:

1. **Reject configuration** when the conflict cannot be resolved within permitted bounds.
2. **Use the most restrictive value** when both values are valid but incompatible, unless an authorized override is recorded.
3. **Require an authorized override** when a less restrictive value is intentionally needed; the override must be audited with actor, reason, and timestamp.

At session start, the system resolves and freezes the effective configuration. Later changes to agent, harness, or activity definitions do not alter the meaning of an in-progress or completed session.

## Group and cohort semantics

| Term | MVP meaning | Deferred |
| --- | --- | --- |
| **Cohort** | Administrative grouping whose members each receive individual isolated sessions with comparable configuration | — |
| **Shared session** | — | Multiple participants in one real-time session with shared transcript and attribution |

The assessment MVP supports cohorts for fair comparison and administration. Shared multi-participant sessions introduce attribution, consent, interruption, scoring, privacy, and transcript-ownership complexity and are deferred.

## Assessment fairness constraints

Assessment is the first product experience. Flexible platform behaviors can undermine fairness unless explicitly constrained.

For assessment activities, the default policy is:

- **Stable memory** during the active assessment period
- **No cross-participant learning** from assessment interactions
- **Frozen configuration** at activity or cohort activation: resolved agent revision, harness revision or snapshot, model, knowledge sources, tools, workflow, and evaluation configuration
- **Material changes create a new activity version or cohort** rather than silently affecting in-flight participants
- **Adaptive follow-up** constrained by a versioned fairness policy
- **Exceptions** explicitly authorized and audited

Harness snapshots must pin immutable content or immutable references with content hashes. A snapshot that references a mutable knowledge base or tool configuration without a hash is not sufficient for historical reconstruction.

## Concept relationships

```mermaid
flowchart TB
  subgraph orgScope [Organization scope]
    OrgPolicy[Organization policy]
    Agent[Agent]
    AgentRevision[Agent revision]
    Harness[Harness]
    HarnessRevision[Harness revision]
    HarnessSnapshot[Harness snapshot]
  end

  Activity[Activity]
  Campaign[Campaign]
  Task[Task]
  Enrollment[Enrollment]
  Attempt[Attempt]
  Session[Session]
  Submission[Submission]
  Participant[Participant]
  Reviewer[Reviewer]
  Evidence[Evidence]
  Evaluation[Evaluation]
  HumanRevision[Human revision]
  Result[Result]
  Release[Release]
  ApprovedMemory[Approved memory]

  OrgPolicy -->|"constrains"| Agent
  OrgPolicy -->|"constrains"| Harness
  OrgPolicy -->|"constrains"| Activity
  Agent -->|"versioned as"| AgentRevision
  Harness -->|"versioned as"| HarnessRevision
  HarnessRevision -->|"captured in"| HarnessSnapshot
  Agent -->|"selected by"| Activity
  Harness -->|"selected by"| Activity
  HarnessSnapshot -->|"may pin"| Activity
  Activity -->|"may deploy as"| Campaign
  Activity -->|"defines"| Task
  Activity -->|"enrolls"| Enrollment
  Enrollment -->|"authorizes"| Participant
  Enrollment -->|"permits"| Attempt
  Attempt -->|"executes as"| Session
  Activity -->|"may coordinate"| Session
  AgentRevision -->|"executes in"| Session
  HarnessRevision -->|"governs"| Session
  HarnessSnapshot -->|"may pin"| Session
  Participant -->|"participates in"| Session
  Participant -->|"submits"| Submission
  Submission -->|"linked to"| Session
  Reviewer -->|"reviews"| Session
  Session -->|"produces"| Evidence
  Session -->|"produces"| Evaluation
  Evaluation -->|"may receive"| HumanRevision
  HumanRevision -->|"may lead to"| Release
  Release -->|"publishes"| Result
  Agent -->|"may retain"| ApprovedMemory
  Harness -->|"controls"| ApprovedMemory
  Activity -->|"may constrain"| ApprovedMemory
  Session -->|"records"| ApprovedMemory
```

**Ownership summary:**

| Concept | Owns |
| --- | --- |
| Organization | Tenant isolation, organization-wide policy, authorization boundaries |
| Agent | Reusable identity, capability declarations, knowledge defaults, default memory mode |
| Harness | Operating procedures, workflows, allowed tools, policies, rubrics, snapshots |
| Activity | Activity rules, tasks, participants, limits, selected agent/harness configuration |
| Session | Isolated execution, events, evidence, evaluation, and exact resolved configuration used |

## Resolved execution manifest

Exact LLM outputs may not be reproducible because models, providers, external tools, and nondeterministic generation can change. The product reliably promises:

- **Configuration reconstructability** — what was configured and permitted
- **Evidence traceability** — what material supported judgments
- **Historical explainability** — why outcomes were produced or revised
- **Versioned behavior** — which revisions were in effect
- **Auditable revisions** — who changed what and when
- **Event replay** where technically possible from recorded events

Every session must record a **resolved execution manifest** containing at minimum:

- Agent revision
- Harness revision or snapshot identifier
- Activity revision (and campaign reference when applicable)
- Model provider, model identifier, and deployment version
- Knowledge-source versions or content hashes
- Tool definitions and versions
- Policies and workflow version
- Evaluation rubric version
- Memory read/write policy
- Relevant generation parameters
- Tool inputs and recorded results

This manifest, together with the event log and evidence references, allows administrators and reviewers to determine exactly why the agent behaved in a particular way. Later changes to agent, harness, activity, tools, policies, or memory do not alter the historical meaning of a completed session.

## Workflow model

Text and voice are **interaction surfaces**. A configurable workflow determines how the activity progresses and what the agent, participant, tools, and reviewers may do at each stage.

The workflow may determine current session stage, permitted actions, required information, submission requirements, question and tool permissions, evidence recording, evaluation timing, pause and completion rules, output requirements, result release, and whether memory updates may be proposed.

Workflows are defined primarily by the harness and may be extended or constrained by activity configuration. Different activity types may share the same agent, harness, session, voice, evidence, memory, and audit foundations while using different workflows.

## Harness mutability and snapshots

The harness is **mutable** and may improve over time through controlled, auditable changes — not uncontrolled self-modification.

Important harness states can be saved as immutable **harness snapshots** capturing instructions, workflows, tools, knowledge references (with hashes), rubrics, policies, memory controls, and related metadata at a point in time. Snapshots support audit, comparison, backup, restoration, and controlled rollout.

An activity may use the current approved harness, a pinned snapshot, a testing version, or a restored historical configuration. Each session must record the exact harness state used in its resolved execution manifest.

## Session state and events

The system maintains one canonical session state and event log used by workflow execution, tool orchestration, evaluation, human review, audit, and result generation.

Events may include session lifecycle, authentication, instructions, submissions, speech and playback, interruptions, tool execution, evidence recording, workflow transitions, timing, evaluation, human review, result release, and memory or harness improvement proposals.

Audit-relevant history cannot be silently overwritten; previous versions remain inspectable. Recorded times have unambiguous ordering and timezone interpretation for audit and fairness analysis.

## Voice interaction model (product level)

The platform supports **natural, interruptible streaming conversation** rather than rigid push-to-talk or unrestricted simultaneous full-duplex voice.

The platform must distinguish:

| Category | Meaning |
| --- | --- |
| **Generated** | Content produced by the agent |
| **Sent** | Content sent for playback |
| **Played** | Content actually played to the participant |
| **Cancelled** | Content cancelled before playback |
| **Interrupted** | Content interrupted during playback |
| **Playback-confirmed portion** | Content the participant was likely exposed to, based on an auditable technical proxy (for example, client playback acknowledgement and final playback offset) |

Only the **playback-confirmed portion** should be treated as conversation input for continuity, transcript accuracy, evaluation fairness, and audit. Playback does not prove that a person heard content; the product records the technical proxy explicitly.

Real-time voice floor management, speech timing, and playback control are technical realization concerns. The product contract is: voice is interruptible; playback progress is tracked; agent continuation uses only the playback-confirmed portion; interruption and cancellation are auditable. Authoritative implementation choices belong in architecture documentation and ADRs.

## Product invariants

These invariants apply across concepts and must be preserved in requirements, UI/UX, architecture, and implementation:

- Enforce organization, activity-scope, participant, and session isolation
- Record a resolved execution manifest and resolved session configuration for every session
- Audit-relevant history cannot be silently overwritten; previous versions remain inspectable
- Distinguish generated, sent, played, interrupted, cancelled, and playback-confirmed voice content
- Link evaluations and human revisions to stable evidence; preserve original outputs
- Distinguish evaluation, human revision, result, and release
- Never allow uncontrolled memory learning, harness self-modification, or result release
- Recorded times have unambiguous ordering and timezone interpretation
- Explicit authorization at every sensitive boundary
- Participant data must not be reused for agent learning unless explicitly permitted
- Lower configuration scopes may narrow but not widen capabilities beyond delegated upper-scope boundaries

## Related documents

- [Product documentation hub](README.md)
- [Product overview](overview.md)
- [MVP scope](mvp-scope.md)
- [Requirements](../requirements/README.md)
- [Architecture](../architecture/README.md)
