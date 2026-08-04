# MVP scope

First product experience, platform direction, explicit non-goals, and deferred capabilities for Flex Agent.

## Status

**Draft.** Scope boundaries inform feature specification authoring; approved requirements live in feature specifications under [`requirements/features/`](../requirements/features/README.md).

## First product experience

The first MVP focuses on **AI-assisted assessment and examination** built on a reusable, memory-controlled conversational-agent foundation.

**Positioning:**

> An AI assessment and examination platform built on a reusable, memory-controlled conversational-agent foundation.

The assessment use case is the initial product experience, not a limitation of the platform model. The same agent, harness, session, workflow, memory, and evaluation concepts can support interviews, coaching, reviews, requirements discovery, onboarding, customer support, and other structured conversational activities.

## MVP demonstration workflow (candidate)

The following end-to-end flow illustrates how core concepts work together in the assessment MVP. It is a **candidate workflow**, not an approved requirement list.

1. Create an examiner agent with identity, knowledge, skills, tools, communication style, and evaluation behavior
2. Select stable or dynamic agent memory mode
3. Configure the harness: workflows, policies, rubrics, tools, validation rules, memory controls, and output requirements
4. Save an initial harness snapshot
5. Create an assessment campaign; select agent and harness; pin snapshot or use current approved harness
6. Define tasks, instructions, submission requirements, rubrics, deadlines, time limits, attempts, and interaction rules
7. Add or import participants
8. Allow participants to submit work; validate and preserve submitted material
9. Create an isolated session per participant
10. Let the agent inspect permitted submission material
11. Conduct structured text or interruptible voice examination with adaptive follow-up when permitted
12. Use approved tools where necessary
13. Collect evidence from submission, conversation, and tool results
14. Produce structured evaluation; flag uncertainty and human-review cases
15. Allow reviewers to inspect evidence, transcript, evaluation, and session configuration
16. Approve and release results
17. Use approved feedback to propose memory or harness improvements when permitted
18. Save, compare, or restore harness snapshots as the system evolves

Each step must be decomposed into approved feature specifications with testable acceptance criteria before implementation commitments.

## Platform capabilities the MVP should demonstrate

These are **product-level capability themes** for the first release. They are not approved requirements until captured in specs.

| Theme | Product meaning |
| --- | --- |
| Reusable agents | Stable identity across activities without recreating agents per use case |
| Governed harnesses | Controlled operating instructions with snapshots and restoration |
| Structured campaigns | Multi-participant activities with shared rules and comparable outcomes |
| Isolated sessions | Concurrent participants with strict data and experience isolation |
| Text and voice | Chat-like and natural interruptible streaming conversation |
| Submissions | Participant work and attachments preserved for evaluation |
| Adaptive follow-up | Questions within configured fairness and rubric boundaries |
| Tools | Permitted tool execution with audit records |
| Evidence-backed evaluation | Structured outcomes linked to rubric, submission, and transcript |
| Human review | Authorized inspection, adjustment, and release workflows |
| Memory governance | Dynamic and stable modes with administrative control |
| Audit and reproducibility | Explainable outcomes with configuration and event history |

## Actor capability themes (reference)

High-level capability themes inform future specs but are **not** approved requirements until captured with `REQ-*` and `AC-*` IDs.

### Administrative capabilities

Administrators can:

- Create and manage agents; configure identity, behavior, roles, knowledge, skills, and tools
- Configure safety, operational boundaries, and evaluation behavior
- Select default memory modes; enable or disable memory; inspect and manage stored memories
- Configure harnesses: workflows, procedures, policies, validation rules, rubrics, and output schemas
- Configure voice interaction, interruption behavior, and interaction-controller policies
- Save, compare, and restore harness snapshots; review harness change history
- Approve or reject harness improvement proposals
- Create campaigns; select current or pinned harness configurations
- Define tasks, submission requirements, time limits, deadlines, attempts, and participant instructions
- Add or import participants; configure participant groups
- Monitor active sessions; pause or terminate sessions when authorized
- Inspect submissions, transcripts, interaction events, tool executions, evidence, and evaluations
- Manage human-review workflows; approve and release results
- Review memory and harness proposals; inspect audit history; export permitted records

### Participant capabilities

Participants can:

- Access an assigned or authorized session
- Review activity instructions; acknowledge rules and consent requirements
- Submit required work; upload permitted attachments
- Start a timed session; communicate through text
- Participate in streaming voice conversation; hear streaming agent speech
- Interrupt the agent naturally; receive clarification
- Be redirected or interrupted according to configured policy
- Respond to adaptive follow-up questions
- View session status and remaining time; receive time warnings
- Pause when permitted; complete or submit the session
- View completion confirmation; view results after release
- View feedback when permitted; request review or appeal when supported

### Reviewer capabilities

Reviewers can:

- Access assigned sessions
- Inspect submissions, text and voice transcripts, interruption and playback events
- Review collected evidence, criterion-level evaluations, confidence, and uncertainty
- Compare agent conclusions with evidence
- Adjust or comment on evaluations when authorized
- Approve or reject results; request additional review
- Release results when authorized
- Propose memory updates and harness improvements
- Compare relevant harness states; review audit history

## Candidate feature-spec catalog

See the [MVP feature-spec catalog](../requirements/README.md#mvp-feature-spec-catalog) in the requirements hub for prioritized candidate areas and authoring order.

## Platform differentiation (product level)

The platform differentiates through:

- Reusable agent identities with stable and dynamic memory modes
- Administratively controlled memory
- Mutable but governed harnesses with snapshots and restoration
- Structured workflows beyond generic chat
- Natural interruptible voice with authoritative session state
- Participant isolation
- Evidence-backed evaluations and human-review support
- Reproducible outcomes and detailed audit history

## Explicit non-goals (deferred)

The MVP does **not** initially require:

- Unrestricted simultaneous full-duplex voice conversation
- Video conversation
- Automated human proctoring
- Multi-agent collaboration inside one session
- Multi-agent debate or delegation
- Visual workflow builders
- Public agent or tool marketplaces
- Advanced cheating detection
- Biometric identity verification
- Unrestricted cross-campaign or cross-organization participant memory
- Fully autonomous agent self-modification
- Uncontrolled harness modification
- Automatic application of high-risk learning proposals
- Complex billing systems
- Advanced organization management
- Large-scale workforce scheduling
- Fully autonomous result release
- General-purpose collaborative workspaces

These capabilities may be introduced later without changing the core separation between agents, harnesses, activities, sessions, memory controls, authoritative state, evidence, and audit history.

## Future platform direction (not MVP commitment)

The platform foundation should remain suitable for:

- Candidate interviews and employee coaching
- Speaking practice and project/design reviews
- Requirements gathering and customer onboarding
- Customer-support and compliance conversations
- Knowledge checks, guided investigations, and certification activities
- Individually initiated sessions, embedded support, or API-triggered activities

Future use cases are directional examples until promoted through approved specifications.

## Related documents

- [Product documentation hub](README.md)
- [Product overview](overview.md)
- [Concept model](concept-model.md)
- [Requirements catalog](../requirements/README.md)
