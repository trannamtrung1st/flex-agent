# Architecture documentation

Technical architecture, system boundaries, and integration design for Flex Agent.

## Status

**No approved documents yet.** Architecture work is deferred until foundational requirements specs establish bounded behavior and quality attributes.

## Purpose

This area will govern how the system is structured: boundaries, data ownership, runtime flows, deployment topology, quality attributes, and integration contracts.

## Entry criteria

Begin architecture documentation when:

- At least one P0 feature specification is in `Draft` or `Approved` status
- Cross-cutting quality attributes (isolation, audit, reproducibility, security) are stated in requirements
- Material technical options require an explicit decision record

## Expected document types

| Type | Description |
| --- | --- |
| Architecture overview | System context, major components, and trust boundaries |
| Data design | Entities, ownership, isolation, retention, and event model |
| Integration design | External services, APIs, queues, and provider contracts |
| Runtime flow | Request/session lifecycle, real-time voice path, tool orchestration |
| Deployment view | Environments, topology, secrets, and operational concerns |
| Quality attributes | Performance, reliability, security, and observability targets |

## Relationship to requirements and decisions

Architecture documents explain how approved requirements are realized. Irreversible or cross-cutting choices are recorded as [architecture decisions](decisions/README.md) (ADRs).

## Core invariants (from product foundation)

Architecture must preserve:

- Organization, campaign, participant, and session isolation
- Exact configuration snapshots per session (agent, harness, campaign, memory, tools, policies)
- Append-only events and immutable snapshots for audit-relevant history
- Distinction between generated, sent, played, interrupted, cancelled, and heard-likely voice content
- Evaluations and human revisions linked to stable evidence; original outputs preserved
- No uncontrolled memory learning, harness self-modification, or result release
- UTC for internal timestamps; explicit authorization at every sensitive boundary

## Related documents

- [Documentation home](../README.md)
- [Requirements](../requirements/README.md)
- [Architecture decisions](decisions/README.md)
- [Product overview](../overview-idea.md)
