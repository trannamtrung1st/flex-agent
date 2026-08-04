# Product overview

## Status

**Draft.** Vision, positioning, and principles. Canonical concepts, scope boundaries, and requirements live in linked product and requirements documents.

## Product vision

Flex Agent is a **multi-session conversational AI platform** for creating, configuring, operating, and improving reusable AI agents across structured activities involving multiple participants.

Administrators define an agent's identity, responsibilities, knowledge, behavior, tools, memory configuration, and evaluation approach, then deploy that agent into controlled activities such as assessments, interviews, coaching sessions, project reviews, requirements discovery, customer support, onboarding, and other guided conversational processes.

The platform provides chat-like and voice-enabled interaction with text conversation, streaming speech-to-text and text-to-speech, natural interruptible voice, participant submissions, concurrent isolated sessions, time and attempt controls, structured workflows, adaptive follow-up questions, tools, configurable memory, evidence collection, structured evaluations, human-review workflows, harness snapshots, and comprehensive audit tracking.

## Product positioning

The architecture supports a flexible conversational AI agent platform. The first product experience is positioned as:

> **An AI assessment and examination platform built on a reusable, memory-controlled conversational-agent foundation.**

The broader platform allows organizations to create agents that operate consistently or improve over time, deploy them into structured activities, conduct isolated text or interruptible voice sessions, use tools, follow controlled workflows, collect evidence, and produce auditable outcomes.

## Core product principles

The product separates identity, operating environment, activity configuration, and individual interaction. See [Concept model](concept-model.md) for canonical definitions and relationships.

> **Agent** — who the AI is, what it knows, what it can do, how it behaves, and how it reasons about outcomes.

> **Harness** — how the agent operates within a controlled process: workflows, policies, tools, rubrics, validation rules, memory controls, and execution procedures.

> **Campaign** — a structured activity: participants, tasks, limits, rules, and selected agent and harness configuration.

> **Session** — one isolated interaction between the configured agent and a participant or explicitly configured participant group.

An agent should not need to be recreated for every assessment, interview, or support process. A campaign is one deployment mechanism within the platform, not the limit of the underlying agent model.

## Product statement

A platform for creating reusable conversational AI agents with configurable identities, roles, knowledge, tools, memory modes, evaluation behavior, and operating boundaries, then running those agents through governed harnesses in structured, multi-session activities involving individual participants or participant groups.

The platform supports text and natural interruptible voice interaction, participant submissions, adaptive follow-up questions, tool use, controlled workflows, evidence collection, structured evaluations, human review, memory management, harness improvement, snapshots, backup, restoration, and complete auditability.

## Related documents

| Need | Authoritative source |
| --- | --- |
| Product documentation hub and boundaries | [Product documentation](README.md) |
| Domain concepts, relationships, lifecycles, invariants | [Concept model](concept-model.md) |
| MVP boundaries, non-goals, deferred capabilities | [MVP scope](mvp-scope.md) |
| Approved requirements and acceptance criteria | [Requirements](../requirements/README.md) |
| Journeys, interaction specs, design system | [UI/UX](../ui-ux/README.md) |
| System boundaries, data, runtime, ADRs | [Architecture](../architecture/README.md) |
| Repository source-of-truth order | [Documentation home](../README.md#source-of-truth-order) |
