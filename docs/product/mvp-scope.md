# MVP scope

First product experience, platform direction, explicit non-goals, and deferred capabilities for Flex Agent.

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Accepted baseline v0.1 |
| **Owner** | Product |
| **Approvers** | TBD — formal sign-off pending |
| **Version** | 0.1 |
| **Effective date** | 2026-08-05 |
| **Last reviewed** | 2026-08-05 |
| **Related decisions** | [Concept model v0.1](concept-model.md) |

**Accepted baseline** means requirements authors may depend on this scope for feature specification. Approved feature specifications govern observable behavior.

## First product experience

The first MVP focuses on **AI-assisted assessment and examination** built on a reusable, memory-controlled conversational-agent foundation.

**Positioning:**

> An AI assessment and examination platform built on a reusable, memory-controlled conversational-agent foundation.

The assessment use case is the initial product experience, not a limitation of the platform model. The same agent, harness, activity, session, workflow, memory, and evaluation concepts can support interviews, coaching, reviews, requirements discovery, onboarding, customer support, and other structured conversational activities.

## MVP validation slice

The MVP is a **vertical product slice** that delivers a complete participant-to-result assessment experience. It is narrower than the full platform vision.

### In scope for MVP

| Capability | MVP scope |
| --- | --- |
| Participants | One participant per session; cohorts for administration with individual isolated sessions |
| Memory | Stable memory only during active assessment |
| Agent and harness | Minimal configuration sufficient to run an assessment; no advanced editors or libraries |
| Activity setup | Assessment creation, task definition, and participant assignment |
| Submissions | Upload and versioned preservation of participant work |
| Examination | Text conversation |
| Evaluation | Evidence-backed structured evaluation with criterion-level rationale and evidence references |
| Human review | Inspection, adjustment, and audited result release |
| Audit | Resolved execution manifest, configuration baseline, and event history |
| Fairness | Configuration frozen at cohort activation; see [Assessment fairness](concept-model.md#assessment-fairness-constraints) |

### Next release (explicitly deferred from MVP)

- Interruptible voice interaction
- Tool execution
- Richer workflow and harness configuration
- Harness snapshot comparison and restoration
- Dynamic memory mode

### Later release

- Agent learning and memory candidates
- Harness improvement proposals
- Shared multi-participant real-time sessions
- Advanced calibration and analytics
- Direct, embedded, and API-triggered activities beyond minimal assessment campaigns

**Trade-off note:** If interruptible voice is essential to product validation, retain voice in the MVP validation slice and defer dynamic memory and harness learning instead.

## MVP demonstration workflow (candidate)

The following end-to-end flow illustrates the **MVP validation slice**. It is a **candidate workflow**, not an approved requirement list.

1. Create a minimal examiner agent with identity, knowledge, and evaluation behavior defaults
2. Configure a minimal harness: workflow, rubric, policies, and stable memory controls
3. Create an assessment activity (campaign); select agent and harness; freeze configuration at cohort activation
4. Define task, instructions, submission requirements, deadlines, time limits, and attempt rules
5. Enroll participants in a cohort
6. Allow participants to submit work; validate and preserve submitted material
7. Create one isolated session per participant (one attempt per session in MVP)
8. Let the agent inspect permitted submission material
9. Conduct structured text examination with fairness-constrained adaptive follow-up when permitted
10. Collect evidence from submission and conversation
11. Produce structured evaluation; flag uncertainty and human-review cases
12. Allow reviewers to inspect evidence, transcript, evaluation, and resolved session configuration
13. Apply human revision when needed; approve and release results
14. Inspect audit history and resolved execution manifest for completed sessions

Each step must be decomposed into approved feature specifications with testable acceptance criteria before implementation commitments.

## Platform capabilities by release tier

These are **product-level capability themes**, not approved requirements until captured in specs.

### MVP validation slice

| Theme | Product meaning |
| --- | --- |
| Minimal agents and harnesses | Sufficient configuration to run one assessment pattern |
| Structured activities | Campaign-based assessment with cohort administration |
| Isolated sessions | One participant per session with strict data isolation |
| Text examination | Chat-like structured conversation |
| Submissions | Participant work preserved for evaluation |
| Evidence-backed evaluation | Structured outcomes linked to rubric, submission, and transcript |
| Human review and release | Authorized inspection, adjustment, and audited result visibility |
| Stable memory | No new long-term learning during active assessment |
| Audit and explainability | Resolved execution manifest, configuration baseline, and inspectable history |
| Assessment fairness | Frozen configuration at cohort activation |

### Next release

| Theme | Product meaning |
| --- | --- |
| Interruptible voice | Natural streaming conversation with playback-confirmed continuity |
| Tools | Permitted tool execution with audit records |
| Richer harness configuration | Snapshots, comparison, and restoration |
| Workflow depth | More configurable stages and transitions |

### Later release

| Theme | Product meaning |
| --- | --- |
| Dynamic memory | Controlled learning with administrative approval |
| Harness improvement | Proposals, review, and controlled rollout |
| Shared sessions | Multi-participant real-time interaction |
| Platform breadth | Direct, embedded, and API activities; advanced analytics |

## Actor capability themes

High-level capability themes inform future specs but are **not** approved requirements until captured with `REQ-*` and `AC-*` IDs.

### Administrative capabilities (MVP)

Administrators can:

- Create and manage agents with minimal identity, knowledge, and evaluation defaults
- Configure harnesses with workflow, rubric, policies, and stable memory controls
- Create assessment activities (campaigns); select agent and harness; activate cohorts with frozen configuration
- Define tasks, submission requirements, time limits, deadlines, and attempt rules
- Enroll participants and assign cohorts
- Monitor active sessions; pause or terminate sessions when authorized
- Inspect submissions, transcripts, evidence, evaluations, and resolved session configuration
- Manage human-review workflows; approve and release results
- Inspect audit history; export permitted records

### Participant capabilities (MVP)

Participants can:

- Access an assigned or authorized session
- Review activity instructions; acknowledge rules and consent requirements
- Submit required work; upload permitted attachments
- Start a timed session; communicate through text
- Respond to fairness-constrained adaptive follow-up questions
- View session status and remaining time; receive time warnings
- Complete or submit the session
- View completion confirmation; view results after release
- Request review or appeal when supported

### Reviewer capabilities (MVP)

Reviewers can:

- Access assigned sessions
- Inspect submissions, text transcripts, and collected evidence
- Review criterion-level evaluations with rationale and evidence references, confidence, and uncertainty
- Compare agent conclusions with evidence
- Adjust or comment on evaluations when authorized
- Approve or reject results; release results when authorized
- Review resolved execution manifest and audit history

Deferred participant and reviewer capabilities (voice, tools, harness comparison, memory proposals) belong to later release tiers.

## Candidate feature-spec catalog

See the [MVP feature-spec catalog](../requirements/README.md#mvp-feature-spec-catalog) in the requirements hub for prioritized candidate areas and authoring order. The catalog is reprioritized around the end-to-end assessment validation slice.

## Platform differentiation (product level)

The platform differentiates through:

- Reusable agent identities with governed memory modes
- Administratively controlled memory and learning artifacts
- Mutable but governed harnesses with versioned snapshots
- Structured workflows beyond generic chat
- Activity-scope isolation with cohort fairness controls
- Evidence-backed evaluations with inspectable rationale
- Human review with audited result release
- Configuration reconstructability and detailed audit history

## Explicit non-goals (deferred)

The MVP validation slice does **not** initially require:

- Interruptible voice (unless promoted via trade-off decision above)
- Tool execution
- Dynamic memory or cross-participant learning
- Harness snapshot comparison, restoration, or improvement proposals
- Shared multi-participant real-time sessions
- Unrestricted simultaneous full-duplex voice conversation
- Video conversation
- Automated human proctoring
- Multi-agent collaboration inside one session
- Multi-agent debate or delegation
- Visual workflow builders
- Public agent or tool marketplaces
- Advanced cheating detection
- Biometric identity verification
- Unrestricted cross-activity or cross-organization participant memory
- Fully autonomous agent self-modification
- Uncontrolled harness modification
- Automatic application of high-risk learning proposals
- Complex billing systems
- Advanced organization management
- Large-scale workforce scheduling
- Fully autonomous result release
- General-purpose collaborative workspaces
- Direct, embedded, and API-triggered activities (beyond campaign-based assessment)

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
