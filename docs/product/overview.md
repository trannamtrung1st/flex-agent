# Product overview

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Approved v0.4 |
| **Owner** | Product Lead |
| **Approvers** | Product Lead, Architecture Lead |
| **Version** | 0.4 |
| **Effective date** | 2026-08-14 |
| **Last reviewed** | 2026-08-21 |
| **Approval reference** | v0.4 P0-compatible Agent-output envelope approved 2026-08-14; 2026-08-19 provider sequencing review preserved product meaning and synthetic-only OpenRouter scope; 2026-08-20 delivery review approved OIDC/application-session foundation as a separate predecessor to hosted Participant Session start; 2026-08-20 vendor-neutral OpenAI-compatible endpoint decision preserved model-provider replaceability and product scope; 2026-08-21 delivery sequencing review separated deterministic adapter migration from exact-profile live qualification without changing enablement gates; supersedes v0.3 |

Vision, positioning, and principles. Canonical concepts, scope boundaries, and requirements live in linked product and requirements documents.

Version 0.4 is approved and supersedes v0.3. Structured Agent
Invocation/Decision, bounded next-timer replacement, and the P0-compatible
Decision-output envelope may proceed against the approved product,
requirements, UI/UX, and architecture package, subject to its contract and
verification gates. It remains compatible with approved Concept model v0.5,
whose person-like persona and honest Agent identity decision does not expand the
MVP. Voice remains a later release.

## What to do next

Approved Concept model v0.5, product overview and MVP scope v0.4, all seven P0
feature specifications, the
[MVP operational defaults](../requirements/mvp-operational-defaults.md), and the
OSS-first self-hostable
[MVP architecture](../architecture/mvp-architecture.md) and the detailed
[Session](../architecture/session-runtime-contract.md),
[Evaluation](../architecture/evaluation-execution-contract.md), and
[Review/Release](../architecture/review-result-release-contract.md) contracts are
approved. The P0 Activity journey and all five P0 surface interaction
specifications, including [Result and Release](../ui-ux/result-release.md), are
also approved. Production HTTP SSE and Worker polling are implemented successor
slices. The human OIDC application-session foundation is implemented and
independently reviewed, including Docker-backed PostgreSQL/`0033`, Keycloak
`26.7.0` back-channel logout, and NGINX restricted-route probes. The remaining
browser PKCE/MFA/key-rotation/clock-skew/multi-instance live matrix stays open
and full `AC-OPS-4` stays Partial. Continue the
remaining production gates: exact-profile live provider qualification, hosted
Session start/configuration, and the ADR-010 evidence listed by
[ADR-010](../architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md#traceability-and-downstream-work).
The OIDC/application-session foundation does not create a product Session. Hosted Participant Session start
is a separate successor and must not be exposed until the approved
Activity/Cohort activation, Enrollment, Submission/Attempt entitlement,
acknowledgment, resolved configuration, execution manifest, exact
Submission-version binding, and ADR-005 atomic-start prerequisites exist.
In parallel, complete the applicable compatibility evidence required by
[ADR-008](../architecture/decisions/ADR-008-bounded-oss-component-set.md),
including qualification of each enabled provider deployment profile and
provider-credential/Organization-endpoint isolation. The approved
[OpenRouter synthetic-development profile](../operations/provider-profiles/openrouter-synthetic-development.md)
may exercise real free-model calls and natural local chat with synthetic,
non-sensitive content, but it does not qualify production, accept Participant
data, or close the separately blocked OpenAI-compatible endpoint qualification
track (formerly Direct OpenAI Phase B). The deterministic migration from the
legacy Direct OpenAI implementation to the vendor-neutral adapter may complete
without a selected live profile, but the adapter must remain default-off until
one exact compatible profile passes its separate live qualification gate. An
affected
integration must not be accepted or enabled for real use until its gates pass,
and the production pilot remains blocked on the evidence listed in
[MVP architecture implementation readiness](../architecture/mvp-architecture.md#implementation-readiness).
Apply the approved shared [design system](../ui-ux/design-system/README.md) with
the approved interaction specifications and implement and verify the
[MVP executable workflow](mvp-scope.md#mvp-executable-workflow) with
specification-driven TDD.

## Product vision

Flex Agent is a **multi-session conversational AI platform** for creating, configuring, operating, and improving reusable AI agents across structured activities involving multiple participants.

Administrators define an agent's identity, responsibilities, knowledge, behavior, tools, memory configuration, and evaluation approach, then deploy that agent into controlled activities such as assessments, interviews, coaching sessions, project reviews, requirements discovery, customer support, onboarding, and other guided conversational processes.

The platform provides chat-like and voice-enabled interaction, participant submissions, concurrent isolated sessions, time and attempt controls, structured workflows, adaptive follow-up questions, tools, configurable memory, evidence collection, structured evaluations, human-review workflows, harness snapshots, and comprehensive audit tracking.

## Product positioning

The architecture supports a flexible conversational AI agent platform. The first product experience is positioned as:

> **An AI assessment and examination platform built on a reusable, memory-controlled conversational-agent foundation.**

The broader platform allows organizations to create agents that operate consistently or improve over time, deploy them into structured activities, conduct isolated text or voice sessions, use tools, follow controlled workflows, collect evidence, and produce auditable outcomes.

## Core product principles

The product separates identity, operating environment, activity configuration, and individual interaction. See [Concept model](concept-model.md) for canonical definitions and relationships.

- **Agent** — who the AI is, what it knows, what it can do, how it behaves, and how it reasons about outcomes.
- **Harness** — how the agent operates within a controlled process: workflows, policies, allowed tools, rubrics, validation rules, memory controls, and execution procedures.
- **Activity** — a structured execution context: tasks, participants, limits, rules, and selected agent and harness configuration. A campaign is one managed multi-participant form.
- **Session** — one isolated interaction between a resolved configuration and a participant or authorized role.

An agent should not need to be recreated for every assessment, interview, or support process. A campaign is one activity deployment mechanism within the platform, not the limit of the underlying agent model.

Agents operate through structured decision opportunities, not only chatbot
request/response exchanges. A trusted platform trigger and authorized resolved
context form an [Agent Invocation](concept-model.md#agent-invocation-invocation-trigger-and-agent-decision);
the Agent produces an Agent Decision envelope that may recommend zero or one
Participant message output, intentional no action, and—when frozen policy
permits—one bounded next-timer requested action. Harness, workflow,
authorization, and runtime validation remain authoritative for any effect.
Agent-initiated behavior therefore comes from governed events or signals rather
than uncontrolled self-waking execution.

When frozen Session policy enables one system timer cadence, a successful Agent
Decision may optionally recommend a bounded delay for the next timer event. The
runtime independently validates the recommendation and replaces the one pending
next event; it does not add parallel timers or delegate scheduling authority to
the Agent.

Model providers and individual models are replaceable execution dependencies,
not Flex Agent's core product value. The durable value is the application and
its governed Agent, Harness, Activity, Session, Evidence, Evaluation, review,
and Release behavior. The architecture preserves a path for an Organization to
use an allowed provider/model profile without redefining those concepts;
qualification, credential isolation, endpoint approval, and frozen execution
provenance remain mandatory, and self-service model-plugin installation is not
an MVP requirement.

OpenAI compatibility is a replaceable execution protocol, not a provider or
model commitment. Organizations may use an approved Organization-hosted,
on-premises, managed, or external compatible endpoint without changing product
meaning. OpenAI-hosted service is not preferred or assumed; if selected, it is
qualified through the same exact-profile boundary as any other compatible
endpoint.

## Product validation

### Initial customer profile

Organizations that run structured assessments or examinations where human reviewers currently spend significant time evaluating submissions and conducting follow-up questioning — especially where consistency, auditability, and fairness across participants matter.

### Actors

| Actor | Role |
| --- | --- |
| **Buyer** | Decision-maker evaluating whether AI-assisted assessment reduces reviewer burden while maintaining quality and auditability |
| **Administrator** | Configures agents, harnesses, activities, participants, and policies |
| **Reviewer** | Inspects evidence, evaluations, and session configuration; approves or adjusts outcomes; releases results |
| **Participant** | Completes assigned tasks, submits work, and participates in examination sessions |

### Problem being replaced

Manual or inconsistent assessment workflows where reviewers must repeatedly inspect submissions, conduct follow-up questioning, apply rubrics, and document decisions — often without a complete, inspectable record of what configuration and evidence produced each outcome.

### Value proposition

Reduce reviewer time per evaluation while preserving human oversight, evidence traceability, configuration reconstructability, and fairness across participants in the same assessment cohort.

### Riskiest assumptions

- Reviewers will trust AI recommendations when evidence references and rationale are inspectable
- Stable-memory, frozen-configuration assessment is sufficient for initial validation
- Text examination delivers enough signal before voice is required
- Organizations will adopt the agent/harness/activity model rather than expecting a single configurable chatbot

### MVP success metrics (candidates)

| Metric | Why it matters |
| --- | --- |
| Reviewer time saved per evaluation | Core efficiency hypothesis |
| Reviewer agreement with AI recommendations | Quality and trust signal |
| Human override rate | Calibration and over-reliance indicator |
| Evidence-reference coverage | Inspectability and audit quality |
| Participant completion rate | Experience viability |
| Session failure/recovery rate | Operational reliability |
| Result appeal or correction rate | Outcome quality |
| Evaluation latency and cost | Operational feasibility |
| Outcome variance across equivalent configurations | Fairness and outcome consistency across frozen configurations |

### When not to use Flex Agent

Organizations should consider alternatives when they need:

- Fully autonomous, unaudited result release without human review
- Uncontrolled agent learning across participants or sessions
- Real-time shared multi-participant sessions without attribution and privacy design
- Exact reproduction of LLM outputs rather than configuration reconstructability and evidence traceability
- A generic chatbot without structured workflows, rubrics, or assessment fairness controls

## Product statement

A platform for creating reusable conversational AI agents with configurable identities, roles, knowledge, tools, memory modes, evaluation behavior, and operating boundaries, then running those agents through governed harnesses in structured, multi-session activities.

The platform supports text and voice interaction, participant submissions, adaptive follow-up questions, tool use, controlled workflows, evidence collection, structured evaluations, human review, memory management, harness versioning, and complete auditability.

## Related documents

| Need | Authoritative source |
| --- | --- |
| Product documentation hub and boundaries | [Product documentation](README.md) |
| Domain concepts, relationships, lifecycles, invariants | [Concept model](concept-model.md) |
| MVP boundaries, non-goals, deferred capabilities | [MVP scope](mvp-scope.md) |
| Approved requirements and acceptance criteria | [Requirements](../requirements/README.md) |
| Journeys, interaction specs, design system | [UI/UX](../ui-ux/README.md) |
| System boundaries, data, runtime, ADRs | [Architecture](../architecture/README.md) |
| Authority by concern | [Documentation home](../README.md#authority-by-concern) |
