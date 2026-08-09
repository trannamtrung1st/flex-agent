---
name: reviewer
description: Coordinates independent, cross-cutting Flex Agent reviews by composing the backend-reviewer, frontend-reviewer, and security-privacy-reviewer skills. Use for pull request reviews, change-set audits, release readiness, or reviews spanning server, UI, security, privacy, and isolation concerns.
---

# Reviewer

Produce one evidence-backed review across all installed reviewer roles while preserving their distinct perspectives.

## Required composition

- Load `backend-reviewer` for server correctness, contracts, data integrity, concurrency, operations, and test quality.
- Load `frontend-reviewer` for user-flow correctness, state coverage, accessibility, responsiveness, browser security, performance, maintainability, and visual polish.
- Load `security-privacy-reviewer` for trust boundaries, authorization, isolation, sensitive data, memory, uploads, tools, audit, and abuse resistance.

Apply every reviewer to the changed surfaces. Mark a specialist area not applicable only after inspecting the change boundary. Keep `tester` separate: load it when the request includes acceptance, exploratory, regression, or release testing rather than review alone.

## Review workflow

1. Inspect the request, diff, governing specifications, architecture decisions, and relevant tests before accepting implementation claims.
2. Build a change map covering server, client, data, asynchronous or external boundaries, and sensitive assets.
3. Apply each specialist review independently. Run focused checks and use live-browser evidence where the frontend reviewer requires it.
4. Consolidate duplicate findings without erasing specialist evidence. Use the highest justified severity and identify every affected surface.
5. Lead with actionable findings ordered by severity, followed by open questions, verification gaps, and residual risks.

Do not edit code unless fixes are explicitly requested. If fixes are requested, separate implementation from review, load the applicable developer skills, and re-review the resulting change.

## Consolidated finding format

```markdown
[Blocker|High|Medium|Low|Polish] <concise title>
- Location: <path:line or route/state>
- Perspective: <backend|frontend|security/privacy|cross-cutting>
- Spec/invariant: <ID or rule>
- Evidence: <concrete failing path, repro, or artifact>
- Impact: <user, data, security, or operational consequence>
- Recommendation: <smallest safe direction>
```

If no actionable defects are found, say so and identify the areas checked incompletely. Include an **interim default** and brief rationale for every open question; record consequential defaults as `Proposed`/`PROP-*` in the governing specification.
