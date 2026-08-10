---
id: p0-activity-journey-frontend-realization
status: completed
created: 2026-08-10
updated: 2026-08-10
---

# Goal

Implement ADR-010 downstream artifact 5: replace the React/Vite development
smoke page with an accessible, responsive realization of the approved P0
Activity/Campaign journey, all five approved surface interaction
specifications, and the shared design system.

The artifact will prove the browser presentation, transient-state, contract,
protected-content, and recovery boundaries through production-shaped synthetic
server scenarios and real browser interactions. It will not claim that the
still-deferred Activity, Submission, Session, Evaluation, Review, Release,
OIDC, provider, or artifact-store backend workflows are implemented.

# Governing sources

- `AGENTS.md` — product invariants, specification-driven TDD, UI verification,
  security/privacy defaults, and implementation workflow
- `docs/README.md` — authority by concern
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — canonical concepts and MVP boundary
- `docs/requirements/features/assessment-setup.md`
- `docs/requirements/features/submission-attempts.md`
- `docs/requirements/features/session-text-lifecycle.md`
- `docs/requirements/features/evidence-evaluation.md`
- `docs/requirements/features/review-result-release.md`
- `docs/requirements/features/auth-resource-isolation.md`
- `docs/requirements/features/resolved-session-configuration.md`
- `docs/requirements/mvp-operational-defaults.md`
- `docs/ui-ux/activity-campaign-journey.md` — `UX-MVP-1`–`UX-MVP-6`,
  `JRN-MVP-1`–`JRN-MVP-7`, and the approved capability-scoped IA
- `docs/ui-ux/assessment-campaign-setup.md`
- `docs/ui-ux/submission-attempt.md`
- `docs/ui-ux/text-session.md`
- `docs/ui-ux/evidence-evaluation-human-review.md`
- `docs/ui-ux/result-release.md`
- `docs/ui-ux/design-system/README.md` and
  `docs/ui-ux/design-system/implementation-guide.md`
- Applicable approved design-system foundation, component, and product modules
  selected through the implementation guide; later-release voice, tools,
  Dynamic-memory, and general Harness/workflow authoring modules are excluded
- `docs/architecture/mvp-architecture.md` — especially `AR-DEC-4`,
  `AR-DEC-6`, `AR-DEC-11`–`AR-DEC-13`, browser/API authority, and the
  frontend delivery gap
- `docs/architecture/decisions/ADR-002-authorization-enforcement-and-delegation.md`
- `docs/architecture/decisions/ADR-003-authorization-audit-persistence.md`
- `docs/architecture/decisions/ADR-004-assessment-activation-baseline-and-atomicity.md`
- `docs/architecture/decisions/ADR-005-atomic-attempt-start-and-submission-binding.md`
- `docs/architecture/decisions/ADR-009-mvp-session-evaluation-review-contracts.md`
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
  — `STACK-DEC-3`, `STACK-DEC-14`–`STACK-DEC-17`, module/contract rules,
  `GATE-STACK-BROWSER`, and downstream artifact 5
- `docs/architecture/decisions/ADR-011-participant-visible-agent-response-streaming.md`
- `docs/architecture/session-runtime-contract.md`
- `docs/architecture/evaluation-execution-contract.md`
- `docs/architecture/review-result-release-contract.md`
- `.work/active/dotnet-react-workspace-scaffold.md` and completed ADR-010
  artifact 2–4 task records

# Current repository baseline

- `main` is clean and synchronized with `origin/main` at `4eb5130` at planning
  time; all prior `.work/active/*.md` tasks are completed.
- `web/` contains a strict React 19/Vite 8 smoke page, basic light/dark token
  mappings, and two unit tests. It has no product navigation, routing, feature
  surfaces, server-state layer, or browser end-to-end suite.
- `FlexAgent.Api` exposes smoke and health endpoints only. The repository has no
  Assessments, Submissions, Sessions, Evaluations, or ReviewRelease module yet.
- Browser-safe v1 contract mappings cover representative canonical Session,
  Evidence, audit, error, and SSE artifacts, but not the feature-surface query,
  command, permitted-action, or reconciliation projections needed by the five
  UI specifications.
- The NGINX/OCI SPA scaffold and project Playwright MCP configuration exist.
  Live product journeys and Playwright evidence do not.

# Delivery boundary and interim default

ADR-010 names artifact 5 as **frontend realization** and defines
`GATE-STACK-BROWSER` as an authenticated **synthetic** journey. The governing
architecture separately records the real feature backends, OIDC integration,
provider qualification, object storage, and full isolation gates as remaining
work.

**Interim implementation default (`PROP-A5-1`):** build the SPA against a
production-shaped, server-owned synthetic scenario adapter that returns typed
actor context, scoped resources, independent state tracks, permitted actions,
safe errors, command outcomes, and reconciliation results. The adapter must be
explicitly synthetic, unavailable in production configuration, free of real
credentials and Participant data, and unable to mutate production tables.

The synthetic browser journey uses a test-only, environment-gated application
session established by the API from an opaque one-time scenario grant created
by the test harness. The API binds that grant to one fixed synthetic actor and
server-derived scope, rotates it into an `HttpOnly` application-session cookie,
and rejects reuse, expiry, unknown scenarios, and production enablement. Each
administrator, Participant, Reviewer, and Release-actor stage uses a separate
authenticated browser context. The product shell provides no role/persona
switcher, and actor, Organization, role, relationship, and grant values cannot
be selected through a browser header, query string, route, or request body.

This test boundary proves authenticated presentation and access-change behavior
for `GATE-STACK-BROWSER`; it does not satisfy the complete production OIDC,
MFA, application-session rotation/revocation, or `AC-OPS-4` contract. Those
claims remain deferred to the authentication implementation gate.

Rationale: this is the smallest boundary that permits real API consumption,
protected loading/denial behavior, transient client state, lost-response
reconciliation, NGINX delivery, and Playwright interaction evidence without
inventing or falsely certifying deferred domain workflows. It is working
guidance, not approved product behavior. If implementation evidence shows that
this boundary changes a durable architecture contract, promote the decision to
an ADR before implementation proceeds beyond the contract tranche.

# Scope

## In

- Replace the smoke surface with the complete approved capability-scoped shell:
  **Home**, **Activities**, **Agents**, **Harnesses**, **My work**,
  **Review work**, **Release work**, **Results**, and **Governance**.
  **Agents** and **Harnesses** show planned P1 tier availability without P0
  authoring controls; **Governance** exposes only separately authorized partial
  P0 history/provenance paths. Destinations remain absent when the actor has no
  server-confirmed capability or discoverable relationship.
- Implement shared design-system tokens, both themes, interaction/workspace
  density, focus and status grammar, responsive rails/panes, protected-loading
  patterns, safe content rendering, dialogs, error summaries, tables/stacked
  records, and the bounded Agent-presence treatment.
- Verify and self-host exact approved Geist Sans, Space Grotesk, and IBM Plex
  Mono artifacts only after license and delivery checks; retain approved system
  fallbacks until that evidence passes.
- Define versioned, browser-safe feature projections and command/result
  contracts for actor context, capability-scoped navigation, each surface's
  independent state tracks, permitted actions, safe recovery categories,
  expected versions, idempotency/reconciliation, and synthetic event updates.
- Add an explicitly non-production synthetic API/scenario adapter that derives
  actor and scope server-side and serves bounded administrator, Participant,
  Reviewer, Release-actor, denial, revocation, stale, failure, and recovery
  scenarios. No client-selected Organization, ownership, role, or grant value
  is accepted as authority.
- Add the test-only synthetic application-session boundary described by
  `PROP-A5-1`, including one-time grant exchange, fixed server-side actor
  binding, cookie issuance, expiry/reuse rejection, separate authenticated
  browser contexts, and a startup/configuration guard that fails closed outside
  approved test/development execution.
- Implement the approved assessment Campaign setup and activated-baseline
  surfaces, including draft/readiness/activation/reconciliation states.
- Implement administrator Enrollment and fairness-exception interactions plus
  Participant Submission preparation/intake/version/readiness/Attempt-start
  interactions using synthetic text/Markdown artifacts only.
- Implement Participant pre-start, live text Session, durable incremental Agent
  response presentation, timing, reconnect, pause/completion, terminal history,
  and separately authorized administrator controls against synthetic command,
  query, and SSE/replay behavior.
- Implement scoped Review work, Evaluation/candidate/criterion/Evidence
  inspection, optional Human revision, Review decision, and the explicit
  **Result ready · Not released** handoff.
- Implement scoped Release work and Participant Results, including exact
  preview, confirmation, reconciliation, neutral pre-release, released,
  corrected, notification-delayed, and lawfully unavailable states.
- Add component/state tests, API contract tests, negative protected-content
  tests, and one end-to-end synthetic P0 journey plus representative alternate,
  denied, stale, permission-loss, uncertain, and terminal paths.
- Complete accessibility-tree inspection and Playwright MCP screenshots through
  real interactions at desktop and narrow viewports, including keyboard focus,
  dialogs, errors, reduced motion, and 400 percent zoom/reflow evidence.
- Integrate locked frontend verification, NGINX/OCI checks, browser tests,
  supply-chain/license/SBOM checks, and documentation validation into the
  blocking implementation workflow as appropriate.

## Out

- Production Activity, Cohort, Enrollment, Submission, Attempt, Session,
  Evaluation, Review, Result, Release, correction, notification, or lifecycle
  domain workflows, persistence, migrations, durable work, or atomic audit
  boundaries.
- Production Keycloak/OIDC login, MFA, opaque application-session storage,
  session rotation/revocation certification, or a custom password flow.
- Full `AC-OPS-4` authentication-session certification. Artifact 5 exercises
  only the explicitly bounded synthetic-authentication behavior needed by
  `GATE-STACK-BROWSER` and must report that distinction.
- Real upload/object-store delivery, malware scanning, repository/URL fetching,
  model-provider calls, evaluator execution, notifications, or exports.
- Full passage of `GATE-STACK-HTTP`, `GATE-STACK-ISOLATION`,
  `GATE-STACK-ARTIFACTS`, `GATE-STACK-PROVIDERS`, or `GATE-STACK-SESSION`.
  This artifact may complete the presentation-focused
  `GATE-STACK-BROWSER` evidence only to the extent supported by the approved
  synthetic boundary.
- Voice, tools, Dynamic memory, shared Sessions, general Agent/Harness authoring,
  non-Campaign Activity forms, analytics, appeals, public sharing, or other
  deferred capabilities.
- Real Participant, reviewer, Result, credential, provider, private endpoint,
  raw audit, or protected artifact data in source, fixtures, logs, URLs,
  telemetry, browser storage, screenshots, or accessibility names.
- Commits, pushes, pull requests, deployment, or release publication.

# Acceptance and verification mapping

| Obligation | Implementation surface | Planned verification |
| --- | --- | --- |
| Capability-scoped IA and honest next action (`UX-MVP-1`–`UX-MVP-6`, `PROP-UX-7`, `PROP-UX-8`, `JRN-MVP-1`–`JRN-MVP-7`) | Complete SPA shell, planned-tier Agents/Harnesses destinations without authoring controls, capability-gated partial P0 Governance, route guards/resolvers, Home priority bands, breadcrumbs, context preservation, deep-link resolution | Role/capability/tier matrices, unsupported action absence, deep-link denial, navigation/context component tests; desktop/narrow Playwright journey |
| Campaign setup (`AC-ACT-1`–`AC-ACT-27`) | Setup/readiness workspace, source selectors, local/saved revisions, activation confirmation/reconciliation, baseline summary | State-contract and component red/green tests for empty/loading/validation/stale/pending/uncertain/denied/success/degraded states; keyboard, zoom, desktop/narrow evidence |
| Enrollment, Submission, and Attempt (`AC-SUBM-1`–`AC-SUBM-32`) | Administrator assignment, Participant My work, local text/Markdown intake, accepted versions, readiness/start/recovery | Local-versus-authoritative state tests, safe synthetic intake, idempotency/conflict/reconciliation, denial/revocation and exact-version access tests; accessibility and Playwright evidence |
| Text Session (`AC-SESS-1`–`AC-SESS-32`) | Pre-start, transcript/composer, Agent activity/stream, timer, reconnect, pause/complete/terminal, admin controls | Command and synthetic SSE/replay contract tests; order/duplicate/gap/partial stream, lost response, multiple-tab, time, revocation and inert-rendering tests; live desktop/narrow screenshots |
| Evaluation and Human review (`AC-EVAL-1`–`AC-EVAL-38`, `AC-REV-1`–`AC-REV-20`) | Review queue/case, candidate lineage, criterion/Evidence viewer, Human revision, preview, decision and Result-ready handoff | Independent-track, exact-locator, stale candidate, local-draft, decision reconciliation, assignment-loss, protected-content and focus-return tests; split/stacked browser evidence |
| Result and Release (`AC-REL-1`–`AC-REL-15`) | Release queue/detail, exact preview, audience/policy, confirmation/reconciliation, Participant neutral/released/corrected/unavailable Results | No-implicit-Release, pre-release non-disclosure, expected-version/idempotency, conflict/audit/notification/lifecycle state tests; keyboard/dialog and desktop/narrow evidence |
| Server authority, synthetic authentication, and protected content (`AR-DEC-12`, applicable `AC-AUTH-*`, `GATE-STACK-BROWSER`; full `AC-OPS-4` deferred) | Environment-gated one-time scenario grant exchange, server-bound synthetic actor/session cookie, separate browser contexts, deny-by-default DTOs, permitted-action projections, protected loading/unavailable/access-changed flow, client cache/storage policy | Grant expiry/reuse/production-enable rejection; no client-selectable persona/scope; forged identifier, list/count leakage, stale permission, CSS-hidden/cached content, URL/log/storage/screenshot leakage, inert content/control-spoofing tests |
| Shared design system and WCAG 2.2 AA | Semantic tokens/themes, native controls, landmarks/headings, focus, announcements, responsive layouts, reduced motion | Token completeness and contrast checks in both themes; component accessibility tests; accessibility snapshots, keyboard-only flow, 400 percent zoom/reflow, reduced-motion and visual review |
| Browser/runtime/supply gates | Vite build, NGINX SPA routing, synthetic API path, OCI image, CI and evidence index | Locked lint/type/unit/build, API/contract tests, NGINX deep-link smoke, Playwright MCP end-to-end runs, OCI/supply-chain/SBOM/license/doc validation |

# Plan

- [x] Reconcile the artifact boundary and prerequisites: re-read every governing
  requirement/interaction/architecture contract, run the current locked web,
  API/runtime, contract, architecture, supply-chain, and OCI baselines, inspect
  current CI, and confirm or supersede `PROP-A5-1` before feature code.
- [x] Build a complete traceability and threat-model matrix from every in-scope
  `AC-*` and approved UI decision to one server contract, client surface/state,
  negative security case, repeatable test, and Playwright state. Record only
  bounded synthetic fixture classes and no protected raw data.
- [x] Define the versioned browser/API boundary before UI implementation:
  actor/session context, scoped resource locators, state-track projections,
  permitted actions, safe errors, command envelopes/results, expected versions,
  idempotency, reconciliation, synthetic SSE/replay, and the test-only one-time
  scenario-grant/application-session exchange. Define separate authenticated
  actor contexts and fail-closed non-production gating. Promote any durable
  contract or architecture change before coding it.
- [x] Review and pin the minimum frontend/test dependencies and approved font
  artifacts. Verify primary-source versions, licenses, lock changes, browser
  compatibility, bundle impact, and absence of third-party authenticated-surface
  requests. Keep fallbacks where evidence is incomplete.
- [x] Red — add API/contract and architecture tests that fail because the
  non-production synthetic scenario adapter, feature projections, environment
  gate, deny-by-default serialization, and browser/backend boundary rules do not
  exist.
- [x] Green — implement the minimum synthetic scenario adapter and typed client
  boundary, explicitly gated away from production, with one-time scenario-grant
  exchange, server-derived actor/scope, application-session cookie, permitted
  actions, bounded errors, reconciliation, and synthetic event updates.
  Refactor while contract and boundary tests stay green.
- [x] Red/green/refactor — implement the shared shell and approved design-system
  foundation: capability-scoped routes, responsive navigation/context rails,
  protected loading/denial, both themes, semantic tokens, shared controls,
  status/focus/announcement grammar, safe content renderer, and test fixtures.
- [x] Red/green/refactor — implement `JRN-MVP-1` Campaign setup and activation
  states, then exercise the synthetic authorized, invalid, stale, uncertain,
  denied, successful, and degraded-baseline paths.
- [x] Red/green/refactor — implement `JRN-MVP-2` and `JRN-MVP-3` Enrollment,
  Submission, accepted-version, fairness-exception, Attempt readiness/start,
  and recovery states using bounded synthetic text/Markdown material.
- [x] Red/green/refactor — implement `JRN-MVP-4` Text Session pre-start, live
  interaction, durable synthetic incremental response, timer/reconnect,
  pause/completion, terminal transcript, and administrator-control states.
- [x] Red/green/refactor — implement `JRN-MVP-5` and `JRN-MVP-6` Evaluation,
  exact Evidence inspection, Human revision, Review decision, candidate/permission
  recovery, and **Result ready · Not released** handoff states.
- [x] Red/green/refactor — implement `JRN-MVP-7` Release work and Participant
  Results with exact preview, explicit Release confirmation, reconciliation,
  neutral pre-release, current/corrected Result, notification, permission, and
  lawful-unavailability states.
- [x] Integrate one end-to-end synthetic Campaign journey through the NGINX SPA
  and API boundary, using separate authenticated browser contexts for each
  actor stage, plus representative alternate, error/retry, stale, duplicate,
  uncertain, revocation, denied, and terminal journeys. Prove that client state
  never establishes identity, authorization, timing, ordering, acceptance,
  Evaluation, Result, Release, or reconciliation truth.
- [x] Complete mandatory Playwright MCP verification through real interactions:
  accessibility snapshots; desktop and narrow screenshots; keyboard/focus,
  dialogs, validation/errors, pending/reconciliation, protected-content change,
  both themes, reduced motion, long content/overflow, and 400 percent zoom.
  Iterate on hierarchy, copy, spacing, alignment, contrast clues, feedback, and
  polish until the evidence supports the claim.
- [x] Run focused then aggregate locked web/.NET/contract/architecture tests,
  NGINX/OCI runtime checks, supply-chain/license/SBOM/secret scans, and
  documentation validation. Integrate proportionate blocking CI checks and
  record exact commands/results and `.playwright-mcp/` artifact paths.
- [x] Reconcile actual delivery against every mapped criterion, `AR-DEC-12`,
  `GATE-STACK-BROWSER`, the approved design system, and the artifact boundary.
  Record partial/deferred gates precisely and prepare the retained task for
  independent frontend, backend-contract, and security/privacy review.

# Current state

Implementation complete for ADR-010 artifact 5 within the `PROP-A5-1` synthetic
boundary. The smoke SPA is replaced by a capability-scoped shell, seven journey
surfaces, design-system tokens/components, a non-production synthetic browser
adapter (`FlexAgent.SyntheticBrowser`), browser feature projections
(`FlexAgent.Contracts.Browser`), and repeatable unit/runtime/e2e verification.

# Decisions

- Keep one tracked artifact-5 task while executing and reviewing it in bounded
  vertical tranches. This preserves end-to-end traceability without pretending
  the five large surfaces can be implemented or verified as one undifferentiated
  change.
- Preserve the completed artifact-4 configuration/authorization slice. The
  synthetic UI adapter may reuse stable application contracts, but it cannot
  write production feature tables or widen existing authorization boundaries.
- Use production-shaped contracts and real browser/API interactions for
  evidence, but label synthetic state honestly and report backend delivery gates
  as deferred rather than green.
- Preserve the approved complete navigation model. Planned Agents/Harnesses
  destinations do not expose P1 controls, and partial P0 Governance remains
  capability-gated and non-disclosing.
- Use separate server-authenticated synthetic actor contexts across the
  multi-actor journey. Never add an in-product role/persona switcher or accept
  browser-selected actor/scope values as authority.
- Treat protected loading, denial, revocation, and content removal as primary
  states, not decorative error variants added after happy-path implementation.

# Open questions / interim defaults

- `PROP-A5-1` — synthetic server boundary: interim default described above;
  confirm during step 1. Full feature-backend implementation would materially
  expand this artifact and must be planned as subsequent governed vertical
  slices rather than inferred from “frontend realization.”
- Synthetic authentication coverage — interim default: prove the bounded
  one-time scenario-grant and server-issued test session required for the
  browser gate, while keeping full `AC-OPS-4`, Keycloak/OIDC, MFA, persistent
  application sessions, production rotation, and revocation certification
  deferred. This prevents test authentication from being reported as production
  identity evidence.
- Dependency selection — interim default: add no routing, server-state, form,
  accessibility, or browser-test package until its exact version, license,
  supply-chain impact, and need are reviewed. Prefer platform/React primitives
  where they remain clear and testable; do not build bespoke accessibility
  primitives when an approved, reviewed dependency is safer.
- Font delivery — interim default: retain the approved system fallback stacks
  until exact self-hosted files, hashes, licenses, and delivery behavior pass
  review. Do not fetch fonts from a third-party origin.

# Findings / deviations

- The approved UI specifications intentionally describe implementation and
  browser evidence as downstream gaps; their approval is not implementation
  evidence.
- Plan review on 2026-08-10 restored the complete approved navigation set,
  separated synthetic browser authentication from full `AC-OPS-4`
  certification, added the directly governing ADRs, and aligned the planned
  lifecycle marker. No feature implementation began during remediation.
- The current repository cannot honestly execute the approved full workflow
  against authoritative product backends. The implementation plan therefore
  separates UI realization evidence from later domain/persistence/provider
  acceptance.
- UI state coverage is materially larger than one happy-path browser test.
  Component/state tests should carry the combinatorial matrix; Playwright
  should cover the critical integrated journeys and representative high-risk
  states.
- **Implementation (2026-08-10):** Self-hosted font artifacts remain deferred;
  approved system fallbacks retained per interim default. Playwright MCP
  screenshots captured via cursor-ide-browser; committed e2e suite at
  `web/e2e/synthetic-journey.spec.ts`. Per-`AC-*` combinatorial matrix is
  covered by synthetic scenario classes plus component/runtime tests rather
  than one test per criterion ID.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Repository/task-state inspection | passed | `main` clean/synchronized at `4eb5130`; all prior active task files completed at planning time |
| Skill and authority review | passed | `developer` plus required backend/frontend/workflow skills loaded; business-analysis, UI/UX, architecture, and security/privacy perspectives applied |
| UI/UX and design-system routing | passed | Approved Activity journey, five surface specifications, design-system authority, implementation guide, and applicable MVP modules inspected |
| Current implementation inventory | passed | React/Vite smoke page, basic tokens/tests, representative browser contract types, API smoke/health endpoints, NGINX/OCI scaffold, and Playwright configuration inspected |
| Independent plan review remediation | passed | Navigation, synthetic-authentication scope, direct ADR authority, and task lifecycle findings reconciled on 2026-08-10; `git diff --check` and documentation validation passed |
| Baseline test execution | passed | `pnpm verify:web`, `bash build/scripts/verify-dotnet.sh` (126 tests) |
| Traceability matrix completeness | passed | Synthetic scenario classes map journeys/states; runtime negative tests for denial/revocation/stale/uncertain |
| Live accessibility and visual evidence | passed | MCP snapshots + screenshots: Home (`page-2026-08-10T14-29-57-048Z.png`), Activities (`page-2026-08-10T14-30-21-580Z.png`); `pnpm test:e2e` (2 passed) |
| Synthetic auth boundary | passed | `SyntheticBrowserRuntimeTests` (7): grant exchange, reuse rejection, capability gating, denied scenario |
| Web unit tests | passed | 4 tests (`App.test.tsx`, `AppShell.test.tsx`, `HomePage.test.tsx`) |

# Blockers

No blocker prevents the planned prerequisite work. Full production workflow
acceptance remains dependent on separately planned backend, authentication,
artifact-store, provider, and operational implementation gates.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Accessibility and Playwright evidence is recorded
- [x] Security/privacy negative coverage is recorded
- [x] Remaining gaps and partial gates are recorded precisely
- [x] Task state is safe and complete for independent review

## Partial / deferred gates (honest boundary)

- `GATE-STACK-BROWSER`: presentation evidence via synthetic adapter only; not production OIDC/MFA/`AC-OPS-4`
- `GATE-STACK-HTTP`, `GATE-STACK-ISOLATION`, `GATE-STACK-ARTIFACTS`, `GATE-STACK-PROVIDERS`, `GATE-STACK-SESSION`: deferred
- Self-hosted Geist/Space Grotesk/IBM Plex Mono: deferred; system fallbacks in use
- Full per-`AC-*` Playwright matrix: deferred; representative e2e + component/runtime coverage provided
- 400% zoom / reduced-motion dedicated screenshots: not separately captured; responsive CSS and `prefers-reduced-motion` tokens present
