# Product overview

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Approved v0.1 |
| **Owner** | Product Lead |
| **Approvers** | Product Lead, Architecture Lead |
| **Version** | 0.1 |
| **Effective date** | 2026-08-05 |
| **Last reviewed** | 2026-08-05 |
| **Approval reference** | Baseline v0.1 approved on `main` (commits `fff26df`, `d8291ef`, 2026-08-05) |

Vision, positioning, and principles. Canonical concepts, scope boundaries, and requirements live in linked product and requirements documents.

## What to do next

Product baseline v0.1 is approved. The [feature catalog](../requirements/README.md#feature-catalog-overview) has 19 specs. [`auth-resource-isolation.md`](../requirements/features/auth-resource-isolation.md) is `Approved`; remaining P0 specs are still placeholders. Continue at [P0 authoring order](../requirements/README.md#p0-authoring-order) for the [MVP executable workflow](mvp-scope.md#mvp-executable-workflow).

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

> **Agent** — who the AI is, what it knows, what it can do, how it behaves, and how it reasons about outcomes.

> **Harness** — how the agent operates within a controlled process: workflows, policies, allowed tools, rubrics, validation rules, memory controls, and execution procedures.

> **Activity** — a structured execution context: tasks, participants, limits, rules, and selected agent and harness configuration. A campaign is one managed multi-participant form.

> **Session** — one isolated interaction between a resolved configuration and a participant or authorized role.

An agent should not need to be recreated for every assessment, interview, or support process. A campaign is one activity deployment mechanism within the platform, not the limit of the underlying agent model.

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
