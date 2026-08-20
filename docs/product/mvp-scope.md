# MVP scope

First product experience, platform direction, explicit non-goals, and deferred capabilities for Flex Agent.

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Approved v0.4 |
| **Owner** | Product Lead |
| **Approvers** | Product Lead, Architecture Lead |
| **Version** | 0.4 |
| **Effective date** | 2026-08-14 |
| **Last reviewed** | 2026-08-20 |
| **Approval reference** | v0.4 P0-compatible Agent-output envelope approved 2026-08-14; 2026-08-19 provider/host sequencing review and 2026-08-20 vendor-neutral OpenAI-compatible endpoint decision preserved product scope; supersedes v0.3 |
| **Related decisions** | Approved [Concept model v0.5](concept-model.md), [ADR-012](../architecture/decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md), [ADR-013](../architecture/decisions/ADR-013-agent-requested-next-timer-replacement.md), and [ADR-014](../architecture/decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md) |

Version 0.4 is **approved** and supersedes v0.3. It preserves the seven-step MVP
slice, text-only examination, and optional bounded next-timer replacement. It
records the P0-compatible Agent Decision output envelope without enabling voice
or additional presentation channels. Approved feature specifications govern
observable behavior. It remains compatible with approved Concept model v0.5:
`PROP-AGENT-1` permits person-like personas for existing Agent revisions but
does not add general Agent authoring, photographic human representation, voice,
or another MVP capability.

## First product experience

The first MVP focuses on **AI-assisted assessment and examination** built on a reusable, memory-controlled conversational-agent foundation.

**Positioning:**

> An AI assessment and examination platform built on a reusable, memory-controlled conversational-agent foundation.

The assessment use case is the initial product experience, not a limitation of the platform model. The same agent, harness, activity, session, workflow, memory, and evaluation concepts can support interviews, coaching, reviews, requirements discovery, onboarding, customer support, and other structured conversational activities.

## MVP validation slice

The MVP is one **executable vertical slice** — a complete participant-to-result assessment flow. It is narrower than the full platform vision.

> **Configure assessment → assign participant → upload submission → text examination → evidence-backed evaluation → human review → release result**

### In scope for MVP

| Capability | MVP scope |
| --- | --- |
| Participants | One participant per session; cohorts for administration with individual isolated sessions |
| Memory | Stable memory only; cohort assessment disables approved-memory reads or pins a memory snapshot at activation |
| Agent and harness | Select existing agent and harness; supply assessment-required parameters only — no general management UI |
| Activity setup | Assessment creation, task definition, and participant assignment |
| Submissions | Upload and versioned preservation of participant work |
| Examination | Text conversation |
| Evaluation | Evidence-backed structured evaluation with criterion-level rationale and evidence references |
| Human review | Inspection, adjustment, and audited result release |
| Audit | Resolved execution manifest, configuration baseline, and event history |
| Fairness | Configuration frozen at cohort activation; see [Assessment fairness](concept-model.md#assessment-fairness-constraints) |

The MVP establishes the provider-neutral
[Agent Invocation and Agent Decision](concept-model.md#agent-invocation-invocation-trigger-and-agent-decision)
contract as reusable foundation. Current P0 text execution uses trusted
participant-input, already-permitted workflow triggers, and—only when frozen
Session policy enables it—one system timer cadence. A successful Agent Decision
is an envelope that may recommend zero or one Participant message output and,
optionally, a bounded relative delay that replaces the next event on that timer
lane. The runtime validates outputs and requested actions independently; the
Agent does not wake itself, choose audience, or author output identity. This
narrow capability does not enable silence-driven triggers, arbitrary or parallel
timers, voice interaction, voice outputs, richer message kinds, Participant
Session tools, reviewer-facing presentation outputs, or richer configurable
workflow behavior.

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
- Direct, embedded, and API-triggered activities (campaign remains the MVP activity form)

## MVP executable workflow

The slice above decomposes into seven bounded outcomes. Each becomes a P0 feature specification before implementation.

| Step | Outcome | P0 spec |
| --- | --- | --- |
| 1 | Configure assessment activity with frozen cohort configuration | [`assessment-setup.md`](../requirements/features/assessment-setup.md) |
| 2 | Assign participant and permit controlled attempts | [`submission-attempts.md`](../requirements/features/submission-attempts.md) |
| 3 | Upload and preserve submission material | [`submission-attempts.md`](../requirements/features/submission-attempts.md) |
| 4 | Conduct text examination in an isolated session | [`session-text-lifecycle.md`](../requirements/features/session-text-lifecycle.md) |
| 5 | Produce evidence-backed structured evaluation | [`evidence-evaluation.md`](../requirements/features/evidence-evaluation.md) |
| 6 | Review, adjust, and approve outcomes | [`review-result-release.md`](../requirements/features/review-result-release.md) |
| 7 | Release result to participant with audit record | [`review-result-release.md`](../requirements/features/review-result-release.md) |

Cross-cutting specs required before or alongside the workflow:

- [`auth-resource-isolation.md`](../requirements/features/auth-resource-isolation.md)
- [`resolved-session-configuration.md`](../requirements/features/resolved-session-configuration.md)

Deferred until the MVP slice works end to end — see [Requirements feature catalog](../requirements/README.md#feature-catalog-overview) (P1–P3).

## Platform capabilities by release tier

These are **product-level capability themes**, not approved requirements until captured in specs.

### MVP capability themes

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
| Dynamic memory mode | Administrative enablement with policy controls |

### Later-release capability themes

| Theme | Product meaning |
| --- | --- |
| Memory candidates | Propose, review, and approve reusable learned artifacts |
| Harness improvement | Proposals, review, and controlled rollout |
| Shared sessions | Multi-participant real-time interaction |
| Platform breadth | Direct, embedded, and API activities; advanced analytics |

## Actor capability themes

High-level capability themes inform future specs but are **not** approved requirements until captured with `REQ-*` and `AC-*` IDs.

### Administrative capabilities (MVP)

Administrators can:

- Select agents and harnesses (or pre-provisioned assessment defaults) for assessment activities
- Create assessment activities (campaigns); activate cohorts with frozen configuration
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

Deferred participant and reviewer capabilities: voice and tools align with [Next release](#next-release-explicitly-deferred-from-mvp); memory candidate proposals align with [Later release](#later-release). Reusable agent and harness library authoring is P1 in the [Requirements feature catalog](../requirements/README.md#p1-foundation-expansion).

## Next step: P0 realization

All seven P0 feature specifications, the detailed Session, Evaluation, and
Review/Release architecture contracts, and the bounded component/provider
defaults are approved — see
[P0 authoring order](../requirements/README.md#p0-authoring-order),
[ADR-008](../architecture/decisions/ADR-008-bounded-oss-component-set.md), and
[ADR-009](../architecture/decisions/ADR-009-mvp-session-evaluation-review-contracts.md).
The P0 Activity journey and all five P0 surface interaction specifications,
including [Result and Release](../ui-ux/result-release.md), are also approved.
Continue remaining production gates after the Sessions runtime slice defined by
[ADR-010](../architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md#traceability-and-downstream-work).
Structured Agent Invocation/Decision, next-timer, and P0-compatible output
envelope behavior exists in Sessions, contracts, PostgreSQL, and the synthetic
Participant path. Production HTTP SSE and Worker polling are implemented host
successors; the human OIDC application-session foundation is implemented and
independently reviewed while Docker-backed Keycloak/`0033` evidence remains
open. Exact-profile provider qualification, hosted Session start/configuration, and other production gates remain against
approved Concept model v0.5, MVP scope v0.4, and current approved feature
specifications,
[ADR-012](../architecture/decisions/ADR-012-structured-agent-invocation-and-decision-boundary.md),
[ADR-013](../architecture/decisions/ADR-013-agent-requested-next-timer-replacement.md),
and
[ADR-014](../architecture/decisions/ADR-014-agent-output-envelope-and-p0-compatibility.md).
In parallel, complete ADR-008's applicable compatibility and
provider-credential evidence. The approved
[OpenRouter synthetic-development profile](../operations/provider-profiles/openrouter-synthetic-development.md)
may use real free-model calls for non-sensitive local chat but cannot qualify a
real assessment provider or replace the OpenAI-compatible endpoint
qualification track (formerly Direct OpenAI Phase B). Qualify at least one
concrete provider deployment profile for each claimed execution profile without
making its model a product dependency. Canonical Session runtime schemas and fixtures exist;
HTTP runtime validation and live-provider contract suites remain open. An
affected integration must pass its gates before acceptance or real use; the
production pilot must pass the broader evidence gates in
[MVP architecture implementation readiness](../architecture/mvp-architecture.md#implementation-readiness).
Apply the approved [design system](../ui-ux/design-system/README.md) and
interaction specifications throughout specification-driven implementation and
end-to-end verification while preserving the approved product scope and feature
boundaries.

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

- Interruptible voice
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

These capabilities may be introduced later without changing the core separation between agents, harnesses, activities, sessions, memory controls, resolved session state, evidence, and audit history.

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
