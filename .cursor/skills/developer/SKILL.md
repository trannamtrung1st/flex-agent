---
name: developer
description: Coordinates full-stack Flex Agent implementation by composing the backend-developer and frontend-developer skills. Use for features, defects, or refactors that cross server APIs, domain behavior, persistence, client state, and web UI boundaries.
---

# Developer

Coordinate one implementation across the backend and frontend without weakening either specialist's standards.

## Required composition

- Load `backend-developer` and follow its domain, API, persistence, authorization, integration, and test requirements.
- Load `frontend-developer` and follow its interaction, accessibility, responsive, client-state, test, and Playwright evidence requirements.
- Load `implementation-workflow` for substantive work and keep its shared task state current.
- Add `architect`, `business-analyst`, `ui-ux-designer`, or `security-privacy-reviewer` when the change reaches those roles' triggers.

Do not copy or dilute specialist instructions here. If a requirement conflicts across surfaces, identify the governing source and resolve the contract before implementation.

## Full-stack workflow

1. Read the governing specifications and map every in-scope acceptance criterion to backend, frontend, and boundary verification.
2. Define the shared contract: authenticated actor, request and response shapes, errors, state transitions, authorization, idempotency, timing, and real-time behavior where applicable.
3. Follow red-green-refactor on each changed surface. Run the smallest useful failing test before implementing its behavior; do not claim a red phase without observed failure.
4. Implement the minimum vertical slice, keeping domain rules on the server and rendering authoritative state clearly in the client.
5. Verify focused backend and frontend tests, then the integrated user journey and proportionate regression coverage.
6. For UI-affecting work, complete the frontend skill's accessibility snapshot and desktop/narrow screenshot evaluation in the live app.

## Coordination checks

- Keep server and client validation aligned without trusting client enforcement.
- Preserve stable error semantics and recovery behavior across the boundary.
- Cover pending, retry, duplicate, stale, unauthorized, and partial-failure paths.
- Keep tenant, activity, participant, and session scope authoritative on the server.
- Ensure client caches and real-time events cannot leak or display cross-scope data.
- Reconcile migrations, generated clients or types, fixtures, and contract tests when schemas change.

## Output

Report acceptance-criterion coverage, observed red and green evidence by surface, contract or migration changes, integrated journey evidence, Playwright artifacts when applicable, security considerations, and remaining verification gaps.
