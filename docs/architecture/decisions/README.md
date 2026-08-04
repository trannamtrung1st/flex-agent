# Architecture decision records

Architecture Decision Records (ADRs) for Flex Agent.

## Status

**No approved documents yet.**

## Purpose

ADRs capture significant technical choices: context, drivers, options considered, decision, consequences, status, and supersession. They prevent silent drift and preserve rationale for future reviewers.

## When to write an ADR

Create an ADR when a decision:

- Is difficult or expensive to reverse
- Affects multiple components or teams
- Establishes a pattern others must follow
- Resolves a material open question from requirements or architecture review

## Naming convention

```
docs/architecture/decisions/ADR-<nnn>-<short-title>.md
```

Example: `ADR-001-session-event-store.md`

## ADR template (proposed)

```markdown
# ADR-<nnn>: <Title>

## Status

Proposed | Approved | Deprecated | Superseded by ADR-<mmm>

## Context

What forces or constraints led to this decision?

## Decision drivers

- ...

## Options considered

| Option | Pros | Cons |
| --- | --- | --- |
| ... | ... | ... |

## Decision

What was chosen and why?

## Consequences

Positive, negative, and neutral outcomes.

## Related

- Requirements: ...
- Supersedes: ...
```

## Supersession

When a decision is replaced, set the old ADR status to `Superseded by ADR-<mmm>` and link forward. Do not delete historical ADRs.

## Related documents

- [Architecture documentation](../README.md)
- [Documentation home](../../README.md)
- [Requirements](../../requirements/README.md)
