---
id: impeccable-frontend-rebuild
status: in-progress
created: 2026-08-27
updated: 2026-08-27
---

# Goal

Replace the current Flex Agent frontend and design system with one approved,
production-ready next-generation React SPA based on the validated Shipboard
Terminal prototypes, while integrating the complete Impeccable skill as a
controlled repository capability. Preserve approved product behavior,
authorization boundaries, backend contracts, and historical evidence during
the migration; cut production over once; then remove `web-legacy` and the raw
prototype snapshot so the maintained end state contains one production
frontend, one design system, and one authoritative documentation set.

# Outcomes and definition of done

- `.agents/skills/impeccable/` contains the complete reviewed Impeccable
  package at a recorded version and provenance, with implicit invocation and
  edit/stop hooks disabled by default.
- The repository UI/UX, frontend-development, review, and testing skills say
  when to invoke Impeccable and make clear that approved Flex Agent documents
  remain authoritative.
- `docs/ui-ux/design-system/` is updated in place to approved v1.0 and is the
  sole design-system source of truth. No parallel “prototype design system” or
  v0.1 runtime remains.
- Root `PRODUCT.md` and `DESIGN.md` are non-authoritative, deterministic
  Impeccable context projections of approved repository documents, and CI can
  detect projection drift.
- The experiment is preserved temporarily as a provenance-recorded,
  content-controlled snapshot under `.work/resources/`; its visual patterns,
  components, and surfaces receive separate visual (`adopt`/`adapt`/`reject`)
  and behavior (`repo-backed`/`design-lab-only`/`remove`) dispositions.
- The current `web/` is renamed to `web-legacy/` only for transitional
  behavior, contract, and regression evidence. It is not treated as product or
  business authority.
- A new `web/` reproduces every production-backed route and behavior frozen in
  the Phase 0 parity baseline using the v1.0 design system, ADR-019 frontend
  state boundaries, existing typed/domain API clients, and specification-driven
  TDD. Approved but not-yet-production capabilities remain design-lab/future
  references and are not implemented by this migration.
- Prototype-only pages are available from a separately built design lab under
  `/prototypes/*`; they use synthetic data and cannot enter the production
  router, production bundle, authentication flow, API traffic, OCI image, or
  production E2E suite.
- Every migrated production surface has requirement/UI-spec traceability,
  focused automated coverage, accessibility evidence, and desktop/narrow
  Playwright screenshots evaluated for visual quality.
- The production build, OCI image, SBOM, CI, developer commands, and
  documentation point to the new `web/`.
- `web-legacy/`, transitional compatibility commands, and the raw prototype
  snapshot are deleted after cutover gates pass. Git history and retained
  provenance/change records provide recovery and historical context.
- A repository-wide stale-reference search, focused and regression tests,
  browser E2E, docs validation, supply-chain checks, and OCI verification pass.

# Governing sources

## Repository and workflow authority

- `AGENTS.md`
- `.agents/skills/implementation-workflow/SKILL.md`
- `.agents/skills/business-analyst/SKILL.md`
- `.agents/skills/architect/SKILL.md`
- `.agents/skills/ui-ux-designer/SKILL.md`
- `.agents/skills/documentation-author/SKILL.md`
- `.agents/skills/frontend-developer/SKILL.md`
- `.agents/skills/frontend-reviewer/SKILL.md`
- `.agents/skills/security-privacy-reviewer/SKILL.md`
- `.agents/skills/tester/SKILL.md`
- `.work/README.md`
- `docs/README.md#authority-by-concern`

## Product, requirements, and UI/UX authority

- `docs/product/concept-model.md` — Approved v0.5
- `docs/product/mvp-scope.md` — Approved v0.4
- `docs/product/overview.md` — Approved v0.4
- `docs/requirements/README.md` and all seven approved P0 feature
  specifications
- `docs/ui-ux/activity-campaign-journey.md`
- `docs/ui-ux/assessment-campaign-setup.md`
- `docs/ui-ux/submission-attempt.md`
- `docs/ui-ux/text-session.md`
- `docs/ui-ux/evidence-evaluation-human-review.md`
- `docs/ui-ux/result-release.md`
- `docs/ui-ux/design-system/README.md` and the modules selected through
  `docs/ui-ux/design-system/implementation-guide.md`

## Technical authority

- `docs/architecture/mvp-architecture.md`, especially `AR-DEC-12`
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
- `docs/architecture/decisions/ADR-019-frontend-state-and-library-boundaries.md`
- `docs/architecture/frontend-architecture.md`
- `docs/contributing/workspace.md`
- `build/toolchain.json`, root `package.json`, `pnpm-workspace.yaml`, and
  `pnpm-lock.yaml`

## Non-authoritative design and implementation evidence

- Experiment repository:
  `/Users/trungtran/MyPlace/Personal/Projects/ui-design-systems`
- Planning-review experiment commit:
  `c52eeda3d8aa117bd7abd49f4ab0ab567953fe96` (then had dirty `gallery.css`,
  `reviewer-console.css`, and unrelated `TODO.md`)
- Execution freeze: clean HEAD
  `f724b68b11c2a147e59864f5789b260baaa50641`. Content-addressed snapshot:
  `.work/resources/impeccable-prototype-snapshot/` (`MANIFEST.json`). `TODO.md`
  excluded. No external commit or tag is required.
- Prototype context: `PRODUCT.md`, `DESIGN.md`, `.impeccable/design.json`,
  selected `.impeccable/surfaces/`, and `prototypes/`
- Prototype surfaces: Participant Home, Participant Journey, Participant
  Session, Administrator Console, Reviewer Console, Surfaces Index, and Shared
  Component Gallery.
- Impeccable source in the experiment:
  `.agents/skills/impeccable/`, version `4.1.1`, 153 tracked files at planning
  time.
- Official Codex skill structure and invocation behavior:
  `https://developers.openai.com/codex/build-skills`
- Official Impeccable upstream and license authority:
  `https://github.com/pbakaus/impeccable` (`Apache-2.0`, with `NOTICE.md`)

# Authority model for the migration

Apply sources by concern in this order:

1. Approved product documents define meaning, canonical vocabulary, scope,
   actors, and invariants.
2. Approved feature specifications define observable behavior, permission,
   transitions, and acceptance criteria.
3. Approved feature-specific UI/UX specifications define journeys, states,
   content, accessibility, and responsive behavior.
4. The approved design-system v1.0 defines shared visual language, visual
   tokens, reusable component presentation, and non-business visual patterns.
5. Approved ADRs define technical realization and browser/server ownership.
6. Production backend contracts and typed/domain clients define current wire
   integration.
7. `web-legacy/` supplies implemented-behavior and regression evidence only.
8. The prototypes are the user-approved superseding input only for visual
   identity, styling, visual tokens, component appearance, non-semantic layout,
   responsive visual composition, and non-semantic motion until those decisions
   are promoted into v1.0 docs. They do not define product scope, information
   architecture, journeys, actions, content meaning, permissions, server
   authority, state transitions, or business lifecycle.

The approved prototypes are the superseding direction for visual identity,
styling, visual token values, typography aesthetics, component appearance,
surface geometry, visual density, non-semantic layout, responsive visual
composition, decorative glyphs, and non-semantic motion. When they conflict
with the repository's current design-system v0.1 in those concerns, adopt the
prototype decision by default and update the design-system documentation in
place. Do not pause, ask for confirmation, preserve both variants, or dilute
the new design to accommodate v0.1. Git history and the v1.0 supersession record
preserve the old system.

Approved repository documents always win for product meaning, canonical
vocabulary, P0 scope, information architecture, routes, user journeys, action
availability and consequence, business copy, roles, permissions, lifecycle and
state transitions, server authority, data disclosure, audit history, security,
privacy, accessibility, responsive reading/action order, and feature-specific
behavior. If a prototype visual cannot satisfy one of those contracts without
change, preserve the visual intent as far as compatible and change the sample
behavior or presentation detail. The implementation rule is **repo behavior in
the prototype visual shell**, never a direct port of prototype business logic.
Reconcile ordinary differences autonomously.
Escalate to the repository owner only when a conflict is business-critical,
high risk, or important enough to change the delivery decision, including:

- product meaning, approved scope, actor authority, fairness, Result/Release,
  or another core workflow invariant;
- authentication, authorization, Organization/activity/participant/Session
  isolation, privacy, protected-content disclosure, or sensitive-data handling;
- an accessibility or safety requirement that cannot be satisfied while
  preserving the prototype direction;
- irreversible data loss, audit-history loss, destructive behavior, or a
  materially different user commitment;
- unresolved copyright/license/provenance, supply-chain, deployment, or runtime
  risk that could prevent lawful or safe release; or
- a cross-cutting prototype gap that materially affects several approved P0
  journeys, cost, maintainability, or release readiness.

For conflicts outside those thresholds, apply the appropriate authority by
concern: use the approved prototype for purely visual choices and the approved
repository for behavior, business flow, semantic content, and constraints.
Apply the safest spec-compatible implementation default and record only
consequential decisions in this task or the owning authoritative document. Do
not interrupt the user with routine token, typography, color, spacing, radius,
visual component, icon, non-semantic layout, copy-style, or motion differences.

# Pre-resolved prototype conflict register

These resolutions are implementation requirements, not questions to reopen in
later phases. The raw snapshot preserves the source as historical evidence;
the design lab and production app use the corrected repo-approved semantics.

| ID | Prototype conflict | Governing repository authority | Required resolution |
| --- | --- | --- | --- |
| `PC-01` | Reviewer Console combines **Approve & Release** and even permits an escalated case to enter Release confirmation. | `review-result-release.md` (`REQ-REV-19`, `REQ-REV-22`, `REQ-REV-23`, `REQ-REL-6`–`REQ-REL-9`); Human Review and Result/Release UI specs | Keep the console geometry and ceremony styling, but separate Review decision from Release. Approval creates a Result-ready handoff only. Reject/Escalate require bounded reasons and never create a releasable Result. Show Release only in the distinct authorized Release flow, with current permission and separation-of-duties checks. |
| `PC-02` | Prototype score/rationale edits mutate local criteria and treat **Save adjustment** as a completed Human revision without the full evidence, reason, visibility, preview, validation, or immutable-submit contract. | `REQ-REV-12`, `REQ-REV-14`, `REQ-REV-16`; Human Review UI spec | Retain the marginalia/editor visual pattern, but implement structured changed-field comparison, exact Evidence references, bounded reason, internal-note versus participant-feedback separation, preview, server validation, immutable submission, stale handling, and a separate later Review decision. |
| `PC-03` | Participant Home/Journey expose **Pending release**, **Evaluation under human review**, reviewer activity, and Release progress before publication. | `REQ-REL-12`; `review-result-release.md` participant flow; Result/Release UI spec | Keep the plate/bay visuals, but show only the neutral repo-approved **Result not available** state and safe next action before Release. Do not reveal Evaluation, score, reviewer state, review timing, or Release workflow through labels, counts, routes, caches, or notifications. |
| `PC-04` | Prototype Result surfaces use synthetic release records and may imply a fixed participant score/criterion payload. | `REQ-REL-11`–`REQ-REL-15`; Result/Release UI spec | Released views render only server-projected fields allowed by the frozen policy, authoritative Release time, correction status, and permitted support/history. Never infer participant-visible fields from the prototype, raw Evaluation, confidence, internal rationale, or reviewer notes. |
| `PC-05` | Campaign configuration submits directly to **Activate**, freezes local state immediately, and omits explicit draft saving, readiness, warnings/blockers, revision binding, revalidation, and uncertain-response reconciliation. | `assessment-campaign-setup.md` (`UI-ACT-DEC-2`, `UI-ACT-DEC-3`, `UI-ACT-DEC-6`) and Campaign requirements | Preserve the Campaign Registry/record and activation-ceremony visuals. Production remains one setup/readiness workspace with **Save draft**, **Check readiness**, exact saved revision, blocker/warning handling, explicit activation confirmation, server reauthorization/revalidation, pending/reconciling states, and authoritative success. Browser `frozen` state is never authority. |
| `PC-06` | An invalid requested Campaign identifier silently falls back to the first known Campaign, which can put an operator in the wrong context. | Campaign setup deep-link and non-disclosing-unavailable rules; authorization/isolation requirements | Never substitute an explicitly invalid or inaccessible Campaign. Show a non-disclosing unavailable state or require explicit selection. A remembered context may be offered only when authorized and visibly confirmed; mutations remain unavailable until authoritative context is loaded. Keep the Campaign-context instrument styling. |
| `PC-07` | Participant Journey buttons locally advance Briefing → Submission → Examination and use **Mark Submission Complete**, bypassing accepted-version and Attempt-readiness boundaries. | `submission-attempt.md` (`UI-SUBM-DEC-3`–`UI-SUBM-DEC-6`) | The current migration implements only the production-backed Assignment/Submission portion. In the design lab and any future production task, the approved flow uses explicit **Submit version**, authoritative Attempt readiness, and **Start Attempt** confirmation; the phase rail may remain visual navigation/progress but cannot mutate lifecycle, unlock a phase, or establish entitlement. |
| `PC-08` | Participant Session uses synthetic local timers, scripted/random Agent turns, local completion, simplified recording consent, and demo leave/resume behavior. | Text Session UI spec, Session requirements, ADR-019 runtime boundaries | Preserve the Session console, transcript ledger, chronology, Agent presence, composer, and ceremony styling. Drive time, acknowledgments, command status, message ordering, pause/resume, reconnect, no-action, completion, and authorization loss from approved server/runtime contracts. Remove P0 voice/listening implications and never ship the simulator in production. |
| `PC-09` | Prototype navigation and action menus include sample or unapproved management, CSV/config export, and deletion behavior. Deletion also risks contradicting immutable/auditable history. | Approved MVP scope and feature specs; audit, authorization, privacy, and export constraints | Visual nav/table/action-menu specimens are adoptable. Production destinations/actions exist only when traced to approved scope and a server-provided permission/action contract. Unapproved export, download, deletion, Users & Access, Policies, Audit management, or other future controls remain labeled design-lab references and disabled/absent from production. |
| `PC-10` | Prototype route topology and labels such as `/participant-*`, `/admin-console/*`, **Candidate**, **Docket**, and **Marginalia** can replace approved IA or canonical business language. | Product concept model, approved journey/UI specs, route inventory | Production routes, navigation hierarchy, page titles, actor names, control labels, and business copy follow the repo. Decorative instrument language may remain only as a secondary visual detail when comprehension is unaffected. Prototype route names remain design-lab-only. |
| `PC-11` | Prototype dates such as `12 SEP 18:00` omit the named governing timezone; custom date/time controls may not expose the approved semantics. | `UI-SUBM-DEC-6`, `UI-SUBM-DEC-12`; Campaign setup time rules | Always lead with authoritative server time in the named Campaign timezone, with exact UTC fallback and unchanged zone identifier when formatting fails. Browser-local time is supplementary. Keep a custom visual control only after keyboard, screen-reader, validation, and timezone behavior pass. |
| `PC-12` | Dark-only styling, very small/high-tracking labels, amber-only validation, custom listboxes, clipped surfaces, and motion can conflict with accessibility behavior. | Approved UI/UX accessibility requirements and design-system conformance | Accessibility is repo authority, not a discretionary prototype style. Preserve visual identity while adjusting size, contrast, spacing, focus, semantics, motion, reflow, and control implementation as needed for WCAG 2.2 AA, 400% zoom, forced colors, reduced motion, keyboard, and screen-reader use. No state relies on color alone. |
| `PC-13` | Prototype's blanket drawn-glyph/no-library rule conflicts with ADR-019's allowed named Lucide imports. | ADR-019 | Use Lucide for ordinary controls and custom instrument glyphs only for approved brand, state, or domain marks. Match prototype visual weight through the v1.0 icon contract; do not duplicate a general icon library. |
| `PC-14` | Prototype fixtures and local mutations make synthetic data look authoritative and could leak into production bundles or API paths. | ADR-019, frontend architecture, authorization/isolation specs | Keep synthetic fixtures and state controls exclusively in the separately built design lab. Production uses typed/domain clients and authoritative server state. Enforce entry-graph, route, bundle, OCI, and E2E isolation tests. |

# Confirmed scope

## In

- Controlled vendoring and role integration of the complete Impeccable skill.
- Temporary, sanitized import of the prototype experiment as reference.
- In-place design-system and related UI/UX documentation replacement.
- A new frontend-transition ADR and aligned frontend/workspace documentation.
- Transitional rename from `web/` to `web-legacy/` and creation of a new
  `web/`.
- New frontend application structure, design-system implementation,
  components, utilities, test harness, and separately built design lab.
- Migration of every useful prototype surface into the design lab.
- Migration of every route and behavior present in the frozen production API-
  mode baseline into the new SPA, with traceability to the applicable approved
  portion of its governing specification.
- Migration or replacement of valuable legacy tests and contract fixtures.
- CI, workspace, lockfile, build, SBOM, OCI, developer-doc, and E2E updates.
- Independent frontend, UI/UX, security/privacy, and release-readiness review.
- One-way production cutover followed by complete legacy and temporary-source
  removal.

## Out

- New product capabilities, actors, permissions, backend policies, or release
  scope.
- Voice, tools, Dynamic memory, general Agent/Harness authoring, shared
  sessions, or any other capability deferred by the approved MVP.
- Replacing React/Vite, native `fetch`, TanStack Query, React Hook Form, Zod,
  or ADR-019 state ownership without a separately approved architecture change.
- Tailwind, CSS-in-JS, Axios, Zustand, persisted query caches, or a browser
  domain-authority layer.
- Making the raw prototype project a permanent workspace package or runtime
  dependency.
- Shipping synthetic prototype data, prototype routes, or Impeccable runtime
  artifacts to production.
- Long-lived parallel frontends, themes, component libraries, design-system
  versions, or production fallback paths.
- Production implementation of approved but not-yet-production Session,
  Attempt-start, Evidence/Evaluation/Review, Result/Release, or other future
  capabilities; those begin as separate tasks against the new foundation.

# Decided defaults

- **Complete Impeccable package:** vendor the full skill directory, including
  `SKILL.md`, references, scripts, agent definitions, and `agents/openai.yaml`.
  Do not copy a partial subset that can drift internally.
- **Controlled activation:** set
  `policy.allow_implicit_invocation: false` in `agents/openai.yaml`; use
  explicit Impeccable commands from applicable repository role workflows.
  Keep post-edit and stop hooks, automatic update/staleness network checks, and
  live mode off by default. Hook or live-mode enablement requires a later
  bounded proposal after the new frontend is stable; it is not an exit
  criterion here. Updates use a new pinned-source review, never self-update.
- **Authority adapters:** root `PRODUCT.md` and `DESIGN.md` exist to give
  Impeccable concise context. They link to and are deterministically generated
  from canonical docs; they never become product or design authority.
- **New visual system:** Shipboard Terminal is the v1.0 identity and
  supersedes Deep-Space Operational Futurism v0.1. The approved prototypes win
  every ordinary purely visual design conflict. Repo-approved product,
  feature, journey, semantic-content, accessibility, security/privacy, and
  architecture contracts always govern their concerns. Retain a v0.1 concept
  only when the prototypes already preserve it or a governing repo contract
  requires it. No v0.1 runtime or dual-token layer remains.
- **Theme:** v1.0 is dark-only by default, matching the approved prototypes.
  It must still satisfy WCAG 2.2 AA, forced-colors/high-contrast behavior,
  reduced motion, reflow/zoom, and non-color state communication. Remove the
  legacy light-theme toggle unless a narrower approved specification requires
  it; none currently does.
- **Typography:** use version-pinned, locally bundled Michroma and Sometype Mono
  only after license and supply-chain review. System fallbacks remain active
  until the font gate passes. Supersede the v0.1 Geist/Space Grotesk/IBM Plex
  Mono decision explicitly.
- **Icons:** preserve ADR-019: direct named Lucide imports are allowed for
  ordinary controls. Authored instrument glyphs are reserved for brand,
  identity, state, and domain-object marks where the design system defines
  them. Emoji, icon fonts, and decorative icon proliferation remain excluded.
- **Styling:** semantic CSS tokens and component/surface styles remain the
  implementation model. Raw visual values stay in token/artwork definitions or
  documented exceptions, not scattered page code.
- **Prototype snapshot:** do not modify the external repository. Capture the
  reviewed files at their current working-tree content into a content-addressed
  snapshot, recording base commit, branch, `git status`, included dirty paths,
  exclusions, and per-file SHA-256 hashes. Include the two modified prototype
  CSS files; exclude unrelated `TODO.md`, dependencies, builds, caches,
  secrets/local config, live-question state, browser logs/traces, and bulk
  generated review images.
- **Prototype disposition:** the prototypes are user-approved superseding
  visual-design input and the default visual choice over design-system v0.1;
  Phase 3 promotes durable visual rules into authoritative v1.0 docs before
  production use. Classify every prototype item on two independent axes:
  visual disposition (`adopt`, `adapt`, or `reject`) and behavior disposition
  (`repo-backed`, `design-lab-only`, or `remove`). A surface can therefore be
  visually adopted while all of its local business logic is removed. Use
  `adapt` only to meet approved behavior, accessibility, architecture, or code
  quality without losing the visual identity.
- **Design lab:** prototype pages use a separate HTML/Vite entry and build.
  Production has no `/prototypes/*` route and no import path from its entry
  graph into design-lab modules.
- **Transition deployment:** the existing production artifact continues to
  build from `web-legacy/` until the final cutover gate. The incomplete new
  `web/` is never deployed as production by directory-name accident.
- **Toolchain:** new frontend dependencies use repository-pinned exact
  versions and the existing lockfile/supply-chain process. Do not copy the
  prototype's pnpm 11 or caret dependency ranges.
- **Performance budget:** Phase 0 records deterministic compressed production
  JS/CSS and local-font sizes plus representative route request counts. At
  cutover, initial production JS+CSS may not exceed the matching legacy
  baseline by more than 15 percent, locally bundled WOFF2 font assets may not
  exceed 250 KiB total, no authenticated surface may add a cross-origin font or
  asset request, and avoidable serial request waterfalls are not accepted.
  Exceeding a budget requires evidence and an explicit recorded architecture/
  UI/UX exception before cutover.
- **Foundation sequencing:** implement primitives first, then prove them with
  the component gallery and one representative real journey before widening
  abstraction. Do not build speculative components for deferred capabilities.
- **Production parity boundary:** production scope is the production API-mode
  route, API, and behavior inventory frozen in Phase 0. The current default
  baseline is `/`, `/activities`, `/activities/:activityId` (redirect),
  `/activities/:activityId/setup`, Cohort Participant/Enrollment routes,
  `/my-work`, and `/my-work/:enrollmentId`, including production-backed
  Submission intake reachable within My Work. The synthetic router is evidence
  and design-lab input, not production parity.
- **Backend behavior:** retain current approved server contracts. Do not add a
  missing backend capability to make a prototype page production-ready. A
  narrowly scoped defect in an existing parity contract may be fixed under the
  backend/security workflow; otherwise record the gap and place the capability
  in a separate successor task rather than substituting synthetic production
  state or widening this migration.
- **Cutover:** perform one production cutover after parity and readiness gates.
  Do not maintain route-level production toggles or a permanent legacy
  fallback.
- **Removal:** after cutover verification, delete `web-legacy/`, temporary
  compatibility scripts, and the raw prototype snapshot. Retain the task file,
  authoritative design-system change history, source provenance, and Git
  history.

# Target architecture

## Transitional workspace

```text
.agents/skills/impeccable/       # full reviewed skill; explicit invocation
.work/resources/
  impeccable-prototype-snapshot/ # temporary sanitized source snapshot
docs/ui-ux/design-system/        # sole authoritative design system
PRODUCT.md                       # generated non-authoritative skill adapter
DESIGN.md                        # generated non-authoritative skill adapter
web-legacy/                      # production source until cutover; then deleted
web/                             # new SPA and separately built design lab
contracts/                       # unchanged canonical/browser-safe contracts
```

## Final workspace

```text
.agents/skills/impeccable/       # controlled repository capability
docs/ui-ux/design-system/        # approved v1.0 authority
PRODUCT.md                       # checked generated adapter
DESIGN.md                        # checked generated adapter
web/                             # only maintained production frontend
  src/
    app/                         # composition, providers, router, auth shell
    api/                         # React-free typed/domain transport ownership
    components/                  # app composition components
    design-system/
      foundations/              # semantic tokens, fonts, reset, motion
      components/               # reusable UI primitives
      patterns/                 # approved product interaction patterns
    features/                    # capability-owned queries, forms, pages
    lib/                         # narrow browser-safe utilities
    test/                        # shared unit/component test harness
    design-lab/                  # separate entry graph; synthetic only
  e2e/                           # production critical-journey browser tests
  e2e-design-lab/                # isolated design-lab checks
contracts/
```

The exact folder split may be simplified when a directory would otherwise be
empty. Domain behavior remains server-owned; frontend folders organize
presentation, state coordination, and integration, not new business layers.

# Execution rules

- Work one numbered phase at a time. Keep this file's status, one `[>]` marker,
  current state, decisions, findings, blockers, and evidence current.
- Begin each behavior slice by identifying exact `REQ-*`, `AC-*`, UI decision,
  and design-system modules. Add and run the smallest meaningful failing test;
  implement; refactor while green.
- Do not copy a prototype component directly into production. First classify
  its semantics, accessibility, state ownership, dependencies, and design-
  system role; then port or rewrite it into the approved new structure.
- Preserve authorization, Organization/activity/participant/Session isolation,
  protected-content loading, cache clearing, auth epoch, stale-response,
  idempotency, and realtime Session constraints from approved sources.
- Never use browser cache keys, route params, prototype state, or legacy UI
  assumptions as authorization evidence.
- Use synthetic identities and data in design-lab and browser artifacts. Keep
  credentials, secrets, participant content, logs, traces, and accessibility
  dumps untracked. Store permitted screenshot evidence only in
  `.playwright-mcp/`.
- For each UI-affecting phase, use the project Playwright MCP to reach changed
  states through interactions, inspect accessibility structure, capture
  desktop and narrow screenshots, evaluate them, fix findings, and repeat.
- Invoke Impeccable in bounded passes appropriate to the work. Its output is a
  proposal/evaluation layer; repository specifications, role guidance, TDD,
  security review, and Playwright evidence remain mandatory.
- Review and test migration behavior continuously. The final phase reconciles
  accumulated evidence; it is not the first time docs, tests, accessibility,
  or security are considered.
- Do not commit, tag, push, or modify the external experiment repository
  without separate explicit authority. This plan does not require an external
  write: provenance comes from the recorded base commit, dirty-path inventory,
  captured source content, and cryptographic manifest.

# Plan

## Phase 0 — Establish reproducible baselines and migration control

- [x] Confirm the Flex Agent target worktree state and record unrelated user
  changes that must be preserved.
- [x] Recheck `.work/active/`, current branches/worktrees, and repository-owner
  coordination for any genuinely in-progress task touching `web/`, frontend
  contracts, CI, or deployment paths. Establish a migration freeze before the
  Phase 5 rename; completed retained task files are evidence, not active work.
- [x] Recheck the experiment base commit and status, then freeze the selected
  current working-tree files by content rather than modifying the source repo.
  Record base commit, branch, timestamp, remote, full status, included dirty
  paths, exclusions, and SHA-256 manifest. Default to include the modified
  `gallery.css` and `reviewer-console.css` and exclude unrelated `TODO.md`.
- [x] Inventory the experiment's tracked Impeccable files, prototype source,
  component families, routes, tests, fonts/assets, and generated/local
  artifacts. Produce explicit include/exclude rules before copying.
- [x] Inventory every current `web/` production and synthetic route, API
  client, provider, query/form owner, reusable component, styling entry,
  unit/component test, E2E scenario, and known UI state.
- [x] Freeze the production parity baseline from the production API-mode router
  and reachable server-backed behavior. Record synthetic-router routes in a
  separate design-lab/reference inventory. Do not promote a synthetic route to
  production merely because a page or test exists in `web-legacy/`.
- [x] Create the migration traceability table in this task file. For every
  production surface record: actor, route, governing feature spec and ACs,
  UI/UX spec and decisions, applicable design-system modules, backend client/
  endpoint, legacy source/tests, prototype source, migration wave, and status.
- [x] Capture pre-migration verification: docs check, frontend lint/typecheck/
  unit/build, and current bundle artifacts/sizes. Record exact results under
  `# Verification`.
- [x] Capture current E2E where runnable, supply-chain check, and OCI SPA
  build. Synthetic-harness Playwright E2E 6/6, `verify-supply-chain.sh`, SPA
  OCI build/probe, and `verify-oci.sh` recorded. Authenticated Keycloak-browser
  E2E remains a later production-profile check, not required to close this
  Phase 0 item.
- [x] Capture synthetic desktop and narrow screenshots plus accessibility
  snapshots for all prototype surfaces. Use them only as comparison evidence,
  never as acceptance authority. Authenticated current-production visual
  states were not captured (synthetic-mode 502 workspace panel only). This is
  **sufficient to start Phase 3**; a complete production-parity screenshot
  baseline is deferred to the production-parity/cutover phase.
- [x] Classify current non-P0/synthetic routes (`Agents`, `Harnesses`,
  `Governance`, and similar) as design-lab-only, deferred, or removable. Default
  all capabilities lacking approved P0 specs to design-lab/reference only.

### Phase 0 exit gate

- Target changes are understood; the selected prototype content is an immutable
  hash-manifested snapshot tied to its base revision and recorded dirty state;
  all inputs have provenance; production parity is separated from synthetic
  reference behavior; the route/behavior/AC inventory is complete; prototype
  visual comparison screenshots are recorded. Authenticated current-web
  production screenshots remain deferred and are not required to start Phase 3.

## Phase 1 — Integrate Impeccable as a governed repository capability

- [x] Verify the Impeccable source package's upstream provenance, version,
  copyright/license terms, included executable scripts, dependency/runtime
  expectations, and modification policy. Do not vendor it if redistribution or
  execution rights cannot be established.
- [x] Resolve the experiment bundle's `4.1.1` contents to an exact official
  upstream commit/tag by file manifest and hashes. Import upstream `LICENSE`
  and `NOTICE.md`, record every local difference, and reject any unexplained
  executable drift. The experiment copy's missing license files are not
  sufficient provenance on their own.
- [x] Do not run the network bundle installer or automatic `npx impeccable
  install/update` path during integration. Acquire the reviewed source from an
  immutable official Git revision, verify its commit and file hashes, then
  vendor the approved provider build. Reassess this default only after the
  upstream bundle-integrity issue
  (`https://github.com/pbakaus/impeccable/issues/479`) is demonstrably fixed in
  the pinned version.
- [x] Review the full `4.1.1` package for filesystem writes, network use,
  subprocess execution, browser injection, generated artifacts, hooks, and
  agent delegation. Record any disabled or locally patched behavior and why.
- [x] Vendor the complete package to `.agents/skills/impeccable/` from the
  pinned, hash-verified source. Add a provenance/notice record with upstream/source
  revision, package version, copied files, license evidence, and local changes.
- [x] Set `policy.allow_implicit_invocation: false` in
  `agents/openai.yaml`. Confirm
  skill discovery and explicit `$impeccable` routing work without enabling
  hooks or adding standalone command pins globally.
- [x] Do not copy the experiment `.codex/hooks.json`. Configure Impeccable with
  repository-specific ignored/generated paths and synthetic-artifact rules,
  set shared `stalenessCheck: false`, and leave automatic post-edit/stop
  detection and live mode off.
- [x] Add explicit `.gitignore` and artifact-retention rules for Impeccable:
  ignore local config, hook state, live sessions/previews/annotations/caches,
  screenshots, logs, critique records, and manual-edit transactions; keep only
  reviewed shared config and generated authority adapters/sidecar. Add a check
  that rejects runtime artifacts, high-signal secret patterns, and
  non-synthetic email identifiers from tracked Impeccable paths. Repository
  policy still requires synthetic-only participant content and reviewed
  artifacts; Gitleaks covers general secret scanning; workflow/review covers
  transcripts, submissions, and evaluations.
- [x] Update `AGENTS.md` role routing and the repository skills for UI/UX
  design, frontend development, frontend review, testing, and documentation so
  they compose Impeccable explicitly at suitable stages without delegating
  product scope, security, accessibility, or final approval to it.
- [x] Define bounded command use:
  `shape`/`critique` for UI/UX design, `extract`/`document` for design-system
  adoption, `harden`/`adapt`/`polish` for implementation, and `audit` for review.
  Do not run open-ended polish loops.
- [x] Add deterministic generation/check tooling for root `PRODUCT.md` and
  `DESIGN.md`. The generated files must contain authority warnings, canonical
  links, approved version/status, relevant design tokens/rules, and a content
  fingerprint. Until design-system v1.0, `DESIGN.md` is a minimal
  authority-safe adapter (canonical links, status, Shipboard migration
  guidance) and does **not** project v0.1 tokens. Complete v1.0 design
  token/rule projection happens in Phase 3. CI check mode must fail on drift
  without rewriting files.
- [x] Add focused tests for context generation and CI drift-check of the
  adapters without changing canonical docs.
- [x] Validate Impeccable `context.mjs` / doctor against the adapters in a
  bounded pass that does not rewrite canonical `docs/`. Ran `python3
  scripts/impeccable_context.py check`, `node …/context.mjs --target .`, and
  `node …/doctor.mjs --json --target .` without `--fix` or `init`/`document`.
  Doctor `route`/`mention` findings are expected adapter-schema gaps until
  Phase 3 v1.0 projections; they must not rewrite approved docs.

### Phase 1 exit gate

- Provenance/license review passes; full skill integration is reproducible;
  implicit invocation and hooks are off; applicable role skills define bounded
  composition; context adapters are generated and drift-checked.

## Phase 2 — Import and classify the prototype reference

- [x] Copy only the Phase 0 selected, hash-manifested source content into
  `.work/resources/impeccable-prototype-snapshot/` using a repeatable import
  script or explicit file manifest. Source files may reflect the recorded
  working-tree content; being tracked at the base commit is not required for an
  approved included dirty path.
- [x] Include the prototype `PRODUCT.md`, `DESIGN.md`, relevant
  `.impeccable/design.json` and surface briefs, `prototypes/src`, tests, configs,
  lockfile/package metadata, and only the small set of reviewed screenshots
  needed to communicate approved surfaces.
- [x] Exclude `.git`, `node_modules`, build output, caches, local configs,
  question/live state, credentials, console logs, traces, uninspected browser
  artifacts, and bulk generated critique/audit material.
- [x] Add a snapshot README and cryptographic manifest recording source remote,
  base commit, branch, source status, included dirty paths, import time,
  included/excluded paths, file hashes, license-review status, and the statement that the
  snapshot is non-authoritative and temporary.
- [x] Run secret scan on the imported snapshot before it is tracked. Font and
  npm license review for Michroma/Sometype Mono and prototype lockfile packages
  is deferred to the Phase 3 font/supply-chain gate. The selected snapshot has
  no standalone `.mjs`/`.sh` skill or install scripts; `prototypes/eslint.config.js`
  is config only.
- [x] Build a two-axis adoption matrix for every prototype token, font, glyph,
  component, pattern, route, interaction, fixture, mutation, and test. Record a
  visual disposition (`adopt`, `adapt`, `reject`) separately from behavior
  disposition (`repo-backed`, `design-lab-only`, or `remove`), the owning repo
  requirement/UI-spec IDs, and applicable `PC-*` resolution. Default in-scope
  visuals to `adopt` and prototype behavior to `repo-backed` only when it is
  explicitly traceable; otherwise keep it design-lab-only or remove it.
- [x] Explicitly reject prototype behavior that invents P0 authority, leaks
  protected content, couples UI state to business truth, conflicts with
  ADR-019, or enables deferred capabilities. See `# Prototype behavior
  rejections` and matrix rows marked `remove`.

### Phase 2 exit gate

- A clean, reviewable, source-identifiable snapshot exists; nothing in it is
  executable or authoritative by accident; the two-axis adoption matrix covers
  every useful design and code family, and every conflict maps to an existing
  `PC-*` resolution or a newly recorded entry.

# Prototype two-axis adoption matrix

Source: `.work/resources/impeccable-prototype-snapshot/` (HEAD `f724b68`).
Visual axis: `adopt` / `adapt` / `reject`. Behavior axis: `repo-backed` /
`design-lab-only` / `remove`. Production never imports snapshot modules.
Font/npm license remains a Phase 3 gate; visual `adopt` for fonts is
conditional on that gate.

## Tokens (`prototypes/src/styles/tokens.css`)

| Item | Visual | Behavior | Authority | Notes |
| --- | --- | --- | --- | --- |
| Ground / text / hairline / amber / teal palette | adopt | design-lab-only until v1.0 tokens | DS v1.0 (Phase 3); `PC-12` | Shipboard identity; map to semantic tokens, not raw page values |
| `--font-plaque` Michroma + Arial Narrow | adopt | design-lab-only until license pin | Typography; `PC-12` | System fallbacks required until font gate |
| `--font-mono` Sometype Mono + ui-monospace | adopt | design-lab-only until license pin | Typography; `PC-12` | Same |
| `--notch: 10px` zero-radius geometry | adopt | design-lab-only until v1.0 radius | Radius; `PC-12` | Adapt clip/overflow for 400% zoom |
| `--ease-out` and sheen/depth/inset fills | adopt | design-lab-only | Motion; `PC-12` | Honor `prefers-reduced-motion` |
| Gangway/bulkhead/readout/select/key rhythm tokens | adopt | design-lab-only until primitives | Layout/density | Production uses same visual values via v1.0 |

## Fonts and packages

| Item | Visual | Behavior | Authority | Notes |
| --- | --- | --- | --- | --- |
| `@fontsource/michroma`, `@fontsource/sometype-mono` | adopt | remove from production until pinned exact versions + license | Supply chain; decided typography default | Do not copy caret ranges or pnpm 11 |
| Prototype `package.json` / lockfile / Vite 8.2 / Playwright ^1.62 | reject | remove | ADR-010; toolchain pins | Rebuild on repo Node/pnpm/React/Vite/TS |
| `@hookform/resolvers`, RHF, Zod, React Router in prototype | reject as copied deps | repo-backed via existing ADR-019 stack | ADR-019 | Keep library choices; restyle controls |

## Glyphs

| Item | Visual | Behavior | Authority | Notes |
| --- | --- | --- | --- | --- |
| `BrandMark` wordmark, `OperatorGlyph`, `TransmitChevron` | adopt | design-lab-only then shared primitives | Identity; `PC-13` | Brand/state marks may stay custom |
| `ActionMenuGlyph`, `ChevronGlyph`, `DateGlyph`, `TimeGlyph` | adapt | repo-backed via Lucide for ordinary controls | ADR-019; `PC-13` | Match weight; do not duplicate a general icon set |
| Phase complete/locked SVG, `ActivationMark`, `RecordSeal`, `StateRing` | adopt | design-lab-only then v1.0 state marks | Status; `PC-13` | Domain/state marks |
| Inline rail Home chevron SVG | adapt | repo-backed | ADR-019 | Prefer named Lucide |

## Components and stylesheets

| Item | Visual | Behavior | Authority | Notes |
| --- | --- | --- | --- | --- |
| `base.css` reset, selection, scrollbars, `:focus-visible`, `[hidden]`, reduced-motion floor | adapt | repo-backed | Accessibility; `PC-12` | Keep Shipboard look; satisfy WCAG/forced-colors |
| Keys (`Key`, `IconButton`, `BackKey`, `TooltipHost`) + `keys.css` | adopt | repo-backed | Buttons | Map variants to approved actions; `.key--release` label is visual only |
| Chrome (`CommandStrip`, `OperateHead`, `ProfileMenu`, `ConsoleFoot`, brand) + `chrome.css` | adopt | adapt | Activity IA; `PC-10` | Production nav/copy/routes follow repo |
| Navigation (`Gangway`, `AreaGroupList`, `IndexRail`, `SectionedNavigation`) | adopt | adapt | IA; `PC-09`, `PC-10` | Destinations only when permitted |
| Plates (`OperateArea`, `EtchedFrame`, `EmptyPlate`, `DemoPlate`) | adopt | `DemoPlate` design-lab-only | Cards/empty | Demo fixture chrome never in production |
| State (`StateIndicator`, `StateReadout`, `StageBars`, `AcknowledgmentGate`) | adopt | adapt | Status; Session/Submission specs | Server owns lifecycle |
| Readouts (`ReadoutList`, `ReadoutGrid`) | adopt | repo-backed | Technical metadata | Timezone display `PC-11` |
| Fields (`FormField`, `FieldInput`, `FieldTextarea`, `ControlLine`, `Breaker`, `RadioGroup`) | adopt | repo-backed | Inputs; ADR-019 RHF | Keep RHF/Zod ownership |
| Select family + `searchable.css` | adapt | repo-backed | Inputs; `PC-12` | Keyboard/SR/listbox contracts win |
| `DropdownMenu` + `menus.css` | adopt | adapt | Dropdown | Unapproved actions stay lab-only |
| `DateTimePicker` + `temporal.css` / `temporalLogic` | adapt | repo-backed | `PC-11`; `UI-SUBM-DEC-6/12` | Named Campaign TZ + UTC fallback |
| Overlays (`DialogPlate`, `NativeDialog`, `CeremonyDialog`, `Bulkhead`, `SignOutCeremony`, toasts) | adopt | adapt | Modals; auth logout | Ceremony styling; server logout |
| Datatable shell/toolbar/pagination/sticky rails | adopt | repo-backed | Tables | Selection logic is reusable UI, not business truth |
| `TableActions` / `EnrollmentTable` | adopt | adapt | Tables; `PC-09` | Export/delete/download actions `remove` in production |
| `demo.css` | reject in production | design-lab-only | `PC-14` | Fixture controls |

## Surfaces, routes, and patterns

| Item | Visual | Behavior | Authority | Notes |
| --- | --- | --- | --- | --- |
| `/` → `/surfaces`, Surfaces Index | adopt | design-lab-only | `PC-10`, `PC-14` | Not a production route |
| `/participant-home` Home bays | adopt | adapt; production `/` and `/my-work` | `PC-03`, `PC-10` | Neutral unpublished Result |
| `/participant-journey` Assignment Station / `PhaseSpine` | adopt | adapt; production My Work | `PC-07`, `PC-03` | Rail is progress/nav only |
| `/participant-session` Examination Console | adopt | design-lab-only this migration | `PC-08` | No production Session route in parity freeze |
| `/admin-console/campaigns` registry + activation ceremony | adopt | adapt; production Activities/setup | `PC-05`, `PC-06` | Save draft / Check readiness / server activate |
| `/admin-console/enrollments` | adopt | adapt; production Enrollment routes | Enrollment ACs | |
| `/admin-console/cohorts`, `sessions` sample areas | adopt chrome | design-lab-only | `PC-09` | No production-backed pages in freeze |
| `/admin-console/users-access`, `policies`, `audit-log` | adopt as specimens | design-lab-only | `PC-09`; MVP scope | Disabled/absent in production |
| `/reviewer-console` | adopt geometry | design-lab-only this migration | `PC-01`, `PC-02`, `PC-04` | Separate Review vs Release |
| `/shared/gallery` Component Deck | adopt | design-lab-only | Foundation sequencing | Gallery-first primitives |
| Prototype 404 | adopt | design-lab-only | | Production uses app 404/unavailable |
| Sign-out ceremony | adopt | repo-backed logout | Auth | Visual only; server session end |

## Interactions, fixtures, and mutations

| Item | Visual | Behavior | Authority | Notes |
| --- | --- | --- | --- | --- |
| `useDemoParam` / `useStateParam` / `DemoPlate` | n/a | design-lab-only | `PC-14` | Never in production bundle |
| Fixtures `home.ts`, `journey.ts`, `session.ts`, `reviewer.ts`, `surfaces.ts`, admin sample data | n/a | design-lab-only | `PC-14` | Synthetic only |
| `HOME_BAYS` **Pending Release** / reviewer-progress copy | adapt labels | remove disclosure | `PC-03` | **Result not available** |
| Local Campaign `Activate` freeze (`CampaignConfigDialog`, `CampaignsArea`) | ceremony visual adopt | remove as authority | `PC-05` | |
| `operationalCampaignId` invalid-id → first campaign | n/a | remove | `PC-06` | Unavailable / explicit select |
| Journey local phase advance / **Mark Submission Complete** | n/a | remove | `PC-07` | Submit version / Start Attempt later |
| `sessionReducer` local timer, scripted Agent, local complete | console visual adopt | remove in production | `PC-08` | Design-lab simulator only |
| Reviewer `localStorage` score/rationale + **Approve & Release** | editor visual adopt | remove | `PC-01`, `PC-02`, `PC-14` | |
| Campaign CSV/JSON download, delete, Users/Policies/Audit actions | menu visual adopt | remove in production | `PC-09` | |
| `formatDeadline` / `12 SEP 18:00` without named TZ | n/a | adapt | `PC-11` | |
| `CND-` / Candidate / Docket / Marginalia / prototype paths | decorative only | remove as IA | `PC-10` | |
| Prototype account Profile/Preferences disabled stubs | n/a | design-lab-only | MVP scope | |

## Tests

| Item | Visual | Behavior | Authority | Notes |
| --- | --- | --- | --- | --- |
| Primitive tests (keys, select, menu, table, datetime, navigation, datatable) | n/a | adapt into new component tests | TDD | Re-express against v1.0 + a11y names |
| `adminNav.test.ts` fallback-to-first-campaign | n/a | remove that assertion; invert | `PC-06` | |
| `CampaignConfigDialog.test.tsx` activate-as-save | n/a | remove; replace with draft/readiness | `PC-05` | |
| `sessionReducer.test.ts`, `reviewerStorage.test.ts` | n/a | design-lab-only | `PC-08`, `PC-02` | Do not port as production truth |
| `campaignArtifacts.test.ts`, export/delete table tests | n/a | design-lab-only or remove | `PC-09` | |
| `useDemoParam.test.tsx`, `surfaces.test.ts`, gallery tests | n/a | design-lab-only | `PC-14` | |
| Prototype `e2e/surfaces.spec.ts` | n/a | design-lab-only | Isolation | Never production E2E |
| `adminNav.test.ts` invalid-id → first Campaign (`CMP-NOPE` → `CMP-0042`) | n/a | remove that assertion; invert | `PC-06` | Confirmed in snapshot |
| `chrome.test.tsx`, `shell.test.tsx`, `campaignSchema.test.ts`, `EnrollmentsArea.test.tsx`, `CampaignRegistry.test.tsx` | n/a | adapt or design-lab-only | TDD; `PC-05`/`PC-09` | Port only UI-contract assertions |

## Remaining families (not separate visual identities)

These are covered by the families above; listed so Phase 2 is file-complete without a per-file table.

| Item | Visual | Behavior | Authority | Notes |
| --- | --- | --- | --- | --- |
| Snapshot `PRODUCT.md` / `DESIGN.md` / `.impeccable/*` | reject as authority | remove | docs authority | Historical evidence only; root adapters are generated from `docs/` |
| `campaignSchema.ts` client Zod | n/a | adapt; server remains authority | Campaign setup; ADR-019 | Keep field-level UX checks; activation/readiness stay server-side (`PC-05`) |
| `CampaignContext` / `ReadoutBand` / `CampaignRegistry` | adopt | adapt | `PC-05`, `PC-06` | Context instrument styling; no silent Campaign substitution |
| `campaigns.ts` and other admin fixtures | n/a | design-lab-only | `PC-14` | |
| `lib/cx`, `breakpoints`, `useAnnouncer`, `useMediaQuery`, `useSurface`, `format.ts` helpers | n/a | adapt into new `web/src/lib` | ADR-019; `PC-11`/`PC-12` | Rebuild; do not copy prototype package |
| Gallery internals (`GalleryDeck`, `PanelTabs`, section files, scroll spy) | adopt | design-lab-only | Foundation sequencing | |
| Reviewer `RecordPanels` | adopt geometry | design-lab-only; strip combined release | `PC-01`, `PC-02`, `PC-04` | |
| `router.tsx` / page modules / surface CSS | adopt look | design-lab routes; production routes from Phase 0 table | `PC-10` | |
| `eslint.config.js` in snapshot | n/a | remove | Toolchain | No standalone `.mjs`/`.sh` skill scripts in the selected snapshot |

Hashed snapshot content is 215 files in `MANIFEST.json`. On-disk extras are `MANIFEST.json` and snapshot `README.md` only.

# Prototype behavior rejections

These are implementation constraints for later phases. Snapshot files stay as
historical evidence; design-lab ports must not reintroduce the rejected
behavior as if it were product truth.

| ID | Rejected prototype behavior | Maps to | Production / lab rule |
| --- | --- | --- | --- |
| `BR-01` | Combined **Approve & Release**, including escalated cases entering Release confirmation | `PC-01` | Separate Review decision from Release; no releasable Result from Reject/Escalate |
| `BR-02` | Local criterion mutation and **Save adjustment** as a completed Human revision | `PC-02` | Immutable server revision; structured evidence/reason/preview |
| `BR-03` | Participant **Pending release**, Evaluation-under-review, reviewer activity, Release progress before publication | `PC-03` | Neutral **Result not available** only |
| `BR-04` | Inferring participant Result fields from synthetic/raw Evaluation, confidence, or reviewer notes | `PC-04` | Server-projected released fields only |
| `BR-05` | Direct local **Activate**, immediate freeze, missing draft/readiness/revalidation | `PC-05` | Server activation; browser `frozen` is not authority |
| `BR-06` | Invalid Campaign id silently replaced by the first known Campaign | `PC-06` | Non-disclosing unavailable; no silent substitution |
| `BR-07` | Local Briefing → Submission → Examination advance and **Mark Submission Complete** | `PC-07` | Phase rail cannot mutate lifecycle or entitlement |
| `BR-08` | Client timer, scripted/random Agent turns, local completion, demo leave/resume as Session truth | `PC-08` | Design-lab simulator only; never production |
| `BR-09` | Unapproved export/download/delete, Users & Access, Policies, Audit management | `PC-09` | Absent or disabled outside design lab |
| `BR-10` | Prototype routes/labels replacing approved IA (`/participant-*`, Candidate, Docket, Marginalia) | `PC-10` | Lab-only paths; production copy/routes from repo |
| `BR-11` | Dateless/local-only timestamps as governing time | `PC-11` | Named Campaign timezone + UTC fallback |
| `BR-12` | Color-only state, undersized labels, inaccessible custom widgets, motion that ignores reduced-motion | `PC-12` | Accessibility is not optional styling |
| `BR-13` | Blanket no-icon-library rule | `PC-13` | Lucide for ordinary controls |
| `BR-14` | Fixtures, `localStorage`, demo query params, or local reducers as authoritative data or production imports | `PC-14` | Isolated design-lab entry graph only |

No new product `PC-*` IDs: every rejection maps to `PC-01`–`PC-14`.

## Phase 3 — Replace the authoritative design system and synchronize UI/UX docs

- [ ] Compare design-system v0.1, all approved P0 interaction specs, the
  two-axis adoption matrix, and `PC-01`–`PC-14` in one exception audit.
  Automatically resolve purely visual design-system conflicts in favor of the
  approved prototypes and behavior/flow/semantic conflicts in favor of the
  approved repo. Record only high-risk, cross-cutting important, or newly
  discovered boundary cases.
- [ ] Author design-system v1.0 in place under
  `docs/ui-ux/design-system/`. Mark the replacement `In review` during editing,
  preserve v0.1 history in Git and a supersession/change record, and make v1.0
  authoritative only after the required Product/UI/UX approval gate. Cite this
  task's repository-owner approval of Shipboard Terminal and the concern-
  specific authority boundary; do not request the same decision again unless
  the authored contract crosses an escalation threshold.
- [ ] Replace the visual direction with Shipboard Terminal and document its
  restrained operational use: near-black blue-green ground, smoked-glass
  planes, hairline structure, clipped/notched geometry, teal context/live
  signals, amber commitment/attention, emitters-only glow, and no floating
  card-stack or generic chat-bubble language.
- [ ] Update foundations comprehensively: semantic colors, typography/font
  delivery, spacing, layout, density, borders/clipped frames, zero-radius
  policy and allowed circular exceptions, depth, motion/reduced motion,
  interaction states, status grammar, breakpoints/container queries,
  accessibility, forced colors, and technical metadata. Prototype visuals may
  change how a state looks, but repo-approved state meaning, order, and
  accessibility behavior remain unchanged.
- [ ] Update component visual contracts to match the adopted prototype families:
  keys/buttons, fields and validation, selection/listbox/menu controls,
  navigation/chrome, plates/readouts, data tables/pagination, dialogs/overlays/
  toasts, status/empty/loading, tooltips, and approved glyph use. Preserve or
  improve repo-approved semantics, keyboard behavior, focus management,
  announcements, destructive consequences, and state ownership; a prototype
  component is not behavior authority.
- [ ] Update visual pattern guidance for Participant assignment/journey, text
  Session, administrator Campaign/Enrollment work, reviewer Evidence/Evaluation
  work, Result/Release, protected content, timelines, and Agent presence. Link
  each pattern to its governing feature UI/UX specification; do not restate or
  alter the journey, action, disclosure, or lifecycle contract in the design
  system. Keep later-release modules explicitly non-authorizing.
- [ ] Reconcile prototype copy and interaction semantics with canonical Flex
  Agent vocabulary. Remove fictional labels wherever they obscure ordinary
  actions or canonical concepts; retain visual metaphors as design language,
  not product nouns.
- [ ] Update `docs/ui-ux/design-system/implementation-guide.md` with module
  selection recipes, implementation mapping, complete UI-state expectations,
  and component-gallery verification for each P0 surface.
- [ ] Update the UI/UX hub and each affected approved P0 interaction spec where
  v1.0 changes shared visual presentation or component usage. Do not use the
  prototype to change approved hierarchy, navigation, journey order, business
  copy, responsive reading/action order, product behavior, or AC meaning.
- [ ] Add a non-normative design provenance/change document under the design-
  system directory recording prototype source revision, adopted concepts,
  deliberate deviations, v0.1 supersession, and future removal of the raw
  snapshot.
- [ ] Regenerate `PRODUCT.md` and `DESIGN.md`; run documentation validation and
  context-drift checks.
- [ ] Complete independent product-scope, UI/UX, accessibility, architecture,
  and security/privacy review. Reviewers must not re-litigate routine visual
  differences already approved by the prototypes; they focus on faithful
  adoption and the escalation thresholds above. Resolve all blocking/high
  findings, record the task approval reference and independent review signoffs,
  and approve v1.0 before production component implementation depends on it.

### Phase 3 exit gate

- Approved design-system v1.0 and synchronized P0 UI/UX documents form one
  coherent authority set; context adapters match them; no unresolved conflict
  or unauthorized capability remains.

## Phase 4 — Approve the frontend transition architecture

- [ ] Add `ADR-020` (or the next available ADR number at execution time) for
  the frontend rebuild, covering transitional directories, build/deployment
  ownership, design-lab isolation, dependency/version policy, API/state reuse,
  cutover criteria, rollback before cutover, and mandatory legacy removal after
  acceptance. Treat the transition structure approved in this task as delegated
  architecture direction; independent architecture/security review confirms
  the detailed ADR without reopening it unless a new high-risk conflict appears.
- [ ] State that the rename is transitional only: `web-legacy/` remains the
  production SPA until cutover; new `web/` is the candidate; production never
  serves both or selects between them at runtime.
- [ ] Preserve ADR-019 Query/form/icon/transport decisions and explicitly keep
  realtime Session outside ordinary Query cache semantics. Supersede only path
  and styling details that v1.0 or the transition changes.
- [ ] Define a compile-time import boundary: production entry modules may
  import app/design-system/features, while design-lab entry modules may also
  import prototype fixtures. Production code may never import from
  `src/design-lab`, `.work/resources`, or legacy source.
- [ ] Define package identities and commands for transition:
  `@flex-agent/web` for new, `@flex-agent/web-legacy` for old;
  `verify:web:new`, `verify:web:legacy`, combined `verify:web`, isolated
  design-lab commands, and an explicit production-build selector owned by CI/
  Docker rather than ambiguous package naming.
- [ ] Define cutover rollback as repository/deployment revision rollback only.
  Do not create a maintained code fallback, route switch, or compatibility
  bridge that survives the task.
- [ ] Update `docs/architecture/frontend-architecture.md`, ADR-010 path/layout
  material, `docs/contributing/workspace.md`, and affected README sections after
  the ADR is approved.

### Phase 4 exit gate

- The transition and end-state architecture are approved; production ownership
  and design-lab isolation are unambiguous; no directory rename begins under an
  undocumented deployment assumption.

## Phase 5 — Create the safe dual-build transition

- [ ] Run the full target baseline again immediately before the rename.
- [ ] Rename `web/` to `web-legacy/` with history-preserving Git operations.
  Change its package name to `@flex-agent/web-legacy`; do not refactor its
  behavior during the rename.
- [ ] Create the new `web/` with repository-pinned Node/pnpm/React/Vite/
  TypeScript, lint, Vitest, Testing Library, Playwright, Query, RHF, Zod, and
  Lucide versions. Add only license-reviewed font packages after Phase 3.
- [ ] Update `pnpm-workspace.yaml` and the lockfile to include both packages
  during transition without duplicating dependency versions unnecessarily.
- [ ] Update root scripts so developers and CI can verify new, legacy, both,
  and design-lab builds explicitly. Combined verification must remain green
  throughout migration.
- [ ] Update `.github/workflows/implementation.yml` and
  `architecture-certification.yml`, implementation-path detection, frontend
  boundary checks, E2E serving, SBOM generation, and supply-chain tooling for
  both paths during transition.
- [ ] Keep `deploy/docker/spa.Dockerfile` and the production E2E server pointed
  explicitly at `web-legacy/`. Add a non-production candidate build path for
  `web/`; never infer production source from which package happens to be named
  `@flex-agent/web`.
- [ ] Add architecture tests that reject production imports from
  `web-legacy/`, design-lab code, or `.work/resources`; reject legacy imports
  into new production code as well.
- [ ] Verify rename parity: legacy lint/typecheck/unit/build/E2E output and SPA
  image behavior match the pre-rename baseline; new scaffold lint/typecheck/
  unit/build passes independently.

### Phase 5 exit gate

- Both packages build deterministically; production still serves unchanged
  legacy behavior; new work cannot accidentally couple to legacy or prototype
  source; CI and supply-chain coverage see both trees.

## Phase 6 — Build the new design-system implementation and frontend foundation

- [ ] Establish the new app composition root, production/design-lab entry
  separation, router boundary, test harness, strict TypeScript/lint baseline,
  error boundary, and environment/config validation.
- [ ] Port the browser-safe typed contracts and native-fetch/domain-client
  architecture with tests. Preserve CSRF, opaque application-session handling,
  credentials mode, generation guards, typed errors/outcomes, cancellation,
  and deny-by-default response projection.
- [ ] Implement one in-memory QueryClient per app/test tree, protected cache
  purge, authorization-context epoch, protected-subtree remount, and stale-401/
  stale-response behavior exactly as ADR-019 requires. Prove these before page
  migration.
- [ ] Implement the v1.0 token layer, bundled fonts/fallbacks, reset/base styles,
  responsive/container-query foundations, reduced-motion, forced-colors, and
  safe-content rules. Add automated token completeness and raw-value boundary
  checks.
- [ ] Build shared primitives in bounded waves, adding a failing test and a
  design-lab specimen for every behaviorally meaningful variant:
  1. typography, spacing, color, focus, icons/glyphs, and state marks;
  2. keys/buttons and button groups;
  3. fields, text areas, checkbox/radio/toggle, error summary, and form layout;
  4. plates, readouts, alerts, badges, empty/loading/error/status states;
  5. navigation, command strip, gangway/bulkhead, breadcrumbs, and role shell;
  6. dialogs, menus, listboxes, searchable single/multi-select, toast/status;
  7. data table, row actions, expansion, sorting/filter display, and pagination;
  8. approved Agent-presence, conversation, Evidence, Evaluation, and Result
     primitives when their first real surface requires them.
- [ ] Keep abstractions evidence-based. A primitive enters production only
  after at least two justified consumers or a design-system contract requires
  it; otherwise leave it local to the first feature until reuse is proven.
- [ ] Create a component gallery in the isolated design lab covering default,
  hover, focus, active, selected, disabled, read-only, loading, validation,
  error, long-content, empty, destructive, and responsive states.
- [ ] Complete keyboard, accessible-name/description, focus-return, live-region,
  contrast, reduced-motion, 400-percent zoom/reflow, narrow, and desktop checks
  for each foundation wave.
- [ ] Run bounded Impeccable extraction/hardening/audit passes against approved
  v1.0, then independent frontend review. Impeccable findings cannot waive
  design-system, accessibility, security, or test failures.

### Phase 6 exit gate

- The new foundation is production-quality, documented by live specimens,
  structurally isolated, accessible, visually approved, and proven by focused
  tests without speculative or deferred product behavior.

## Phase 7 — Migrate prototype surfaces into the isolated design lab

- [ ] Add a separate design-lab HTML/Vite entry, router, build output, dev
  command, preview command, tests, and Playwright config. Its route root is
  `/prototypes/*`; its bundle is not an input to the production SPA image.
- [ ] Port/refactor the Surfaces Index and Shared Component Gallery first so
  they become navigation and verification infrastructure for the remaining
  reference surfaces.
- [ ] Port the Participant Home, Participant Journey, Participant Session,
  Administrator Console, and Reviewer Console as synthetic design-lab pages.
  Keep their approved visuals while refactoring them onto the new component
  and token contracts and replacing conflicting sample flows with the
  repo-approved semantics in `PC-01`–`PC-14`.
- [ ] Treat the immutable raw snapshot as the only exact historical copy. The
  runnable design lab is a current design reference: its routes, actions,
  labels, disclosure states, and synthetic transitions must model the approved
  repo flow even when that differs from the original sample behavior.
- [ ] Preserve useful out-of-current-scope prototype concepts only as clearly
  labeled future/reference states. Do not connect them to production APIs,
  production auth, real storage, service workers, analytics, or external calls.
- [ ] Replace copied surface-specific primitives with shared components only
  where the Phase 2 adoption matrix and Phase 6 evidence support the change.
  Record intentional visual deviations from the source prototype.
- [ ] Add route and bundle-boundary tests proving production returns no
  prototype route and contains no prototype fixtures/labels/modules.
- [ ] Run design-lab unit/component/E2E tests and Playwright accessibility plus
  desktop/narrow visual comparison for every ported surface. Resolve functional
  and high/polish findings needed to keep the design reference credible.

### Phase 7 exit gate

- Every retained prototype surface is available in the isolated design lab,
  visually reconciled with v1.0, and absent from production entry graphs and
  artifacts.

## Phase 8 — Migrate the frozen production-parity frontend in vertical waves

For every wave: populate the task traceability rows; read the exact governing
spec/modules; inspect legacy behavior and tests; inspect the corresponding
prototype surface; record its separate visual and behavior dispositions plus
applicable `PC-*` resolutions; write/run failing tests; implement repo behavior
inside the approved visual system; refactor; run focused and affected
regression tests; exercise all applicable states in the live app; capture and
evaluate desktop and narrow screenshots plus accessibility snapshots; run
frontend and security/privacy review; resolve blockers/high findings; then mark
the wave complete. Migrate only behavior present in the Phase 0 production
API-mode baseline. A synthetic page, approved future specification, prototype
screen, or reusable client module does not by itself authorize a production
route.

### Wave 8.1 — Authentication shell, protected routing, role home, and Activities

- [ ] Migrate app bootstrap, production API mode, login/callback/logout gates,
  protected-state teardown, role/capability navigation, Home, Activities list,
  Activity redirect, and Campaign-create entry. Retain a synthetic mode only in
  the isolated design lab; production entry code does not switch to it.
- [ ] Preserve fresh permission observation before dependent reads, exact
  Activities invalidation, non-optimistic audited mutations, and safe access-
  loss handling from ADR-019.
- [ ] Prove loading/unauthenticated/denied/error/ready/context-replacement/
  logout states, keyboard shell navigation, and narrow gangway/bulkhead behavior.

### Wave 8.2 — Assessment setup and Enrollment administration

- [ ] Migrate Campaign draft/readiness/activation, exact source selection,
  immutable baseline presentation, Enrollment lists/details, accommodation and
  fairness-exception interaction, and administrative recovery states.
- [ ] Preserve server authority for activation, eligibility, timing, limits,
  reason/approval requirements, expected revisions, idempotency, and audit.
- [ ] Prove draft/invalid/pending/active/stale/conflict/denied/empty/large-table/
  narrow states and correct date/time/timezone presentation.

### Wave 8.3 — Participant My Work and production-backed Submission intake

- [ ] Migrate assignment discovery/detail, instructions, submission intake and
  version presentation, cancellation/reconciliation, exact authorized preview/
  download, acknowledgment, and recoverable failure behavior already reachable
  through production My Work.
- [ ] Preserve accepted Submission versions, accommodation-derived effective
  timing, safe input on recoverable failure, and protected-content non-
  disclosure. Do not add Attempt readiness, Start Attempt, Session creation, or
  another downstream action unless it is present in the frozen production
  baseline; the current reviewed baseline stops before those capabilities.
- [ ] Prove no-assignment/loading/denied/versioned-submission/upload-validation/
  pending/cancelling/reconciling/duplicate/conflict/unavailable/narrow states.

### Wave 8.4 — Synthetic/future route disposition and valuable-evidence migration

- [ ] Explicitly classify the legacy synthetic routes for Participant Session,
  Review work, Release work, Results, Governance, Agents, Harnesses, and other
  non-production destinations as design-lab/reference, deferred successor work,
  or removal. The current default is design-lab/reference for visually useful
  approved P0 surfaces and removal/reference-only for unauthorized future
  management actions.
- [ ] Preserve valuable pure view-model, formatting, accessibility, and runtime
  test evidence only when it can live in a non-production design-lab module or
  a production-neutral utility without implying that the capability is
  deployed. Do not copy synthetic API authority, fixtures, local state
  transitions, or destination guards into the production entry graph.
- [ ] Keep Session realtime outside Query and retain its approved technical
  lessons in ADR/docs/tests or design-lab specimens, but defer production
  Session routing and server integration to a separately approved successor
  task unless Phase 0 proves it is already production-backed.

### Wave 8.5 — Route, behavior, and test parity reconciliation

- [ ] Reconcile the Phase 0 inventories. Every production API-mode route must
  map to an equivalent new production route. Every synthetic route must map to
  a design-lab/deferred disposition or approved removal; it must not silently
  become production.
- [ ] Reconcile API calls, commands, SSE flows, query keys, forms, safe-content
  rendering, auth transitions, and error states; eliminate silent behavior gaps.
- [ ] Port valuable legacy tests to behavior-focused new tests. Do not preserve
  brittle markup/style assertions or tests for rejected/deferred behavior.
- [ ] Run full new-web unit/component and production E2E regression. Confirm no
  production import or runtime dependency on `web-legacy/` or the raw snapshot.

### Phase 8 exit gate

- The complete frozen production API-mode parity baseline exists in new `web/`;
  every legacy production and synthetic surface has an explicit disposition;
  no approved-but-unimplemented capability was promoted by the migration;
  acceptance traceability and reviewed browser evidence are complete; and no
  blocking/high finding remains.

## Phase 9 — Production cutover

- [ ] Freeze feature work and reconcile this plan, the route/AC matrix, docs,
  code, tests, and review findings. Record all intentionally deferred items.
- [ ] Run the new production build in the authenticated local/CI deployment
  profile with real server contracts and synthetic non-sensitive data.
- [ ] Execute end-to-end role journeys:
  Administrator configures/activates and enrolls; Participant discovers My
  Work and receives/cancels/reconciles/previews/downloads accepted Submission
  versions according to the frozen production baseline. Run the isolated design
  lab journeys separately; do not count them as production E2E.
- [ ] Run wrong-role, wrong-Organization/activity/participant/Enrollment,
  guessed-ID, stale-permission, logout/context-replacement, duplicate-command,
  lost-response, upload/protected-content, and protected-loading negative tests
  applicable to the frozen production baseline. Session, Review, and Release
  negatives belong to successor production tasks while those routes remain
  design-lab-only.
- [ ] Complete final UI/UX and accessibility review across desktop, narrow,
  400-percent zoom/reflow, keyboard-only, reduced-motion, forced-colors, focus,
  dialog, loading, empty, error, pending, destructive, and terminal states.
- [ ] Compare production bundle size, startup, route loading, large-table/
  long-content rendering applicable to the parity baseline, network request
  counts, and avoidable waterfall results
  to the Phase 0 baseline and the decided performance budget. Resolve budget
  failures or record the required approved exception with evidence.
- [ ] Switch root production commands, production E2E server, CI artifact,
  `deploy/docker/spa.Dockerfile`, OCI verification, SPA SBOM/license inventory,
  and developer documentation from `web-legacy/` to `web/` in one reviewed
  change set.
- [ ] Build and inspect the final SPA OCI image. Prove it contains the new web,
  excludes `web-legacy`, prototype/design-lab code, source maps not intended for
  release, and sensitive/generated artifacts.
- [ ] Perform a post-switch smoke/E2E run against the production-shaped image.
  If a cutover gate fails, revert the deployment/repository revision; do not add
  a new dual-runtime fallback.

### Phase 9 exit gate

- The new `web/` is the only production build source and passes all functional,
  security/privacy, accessibility, visual, supply-chain, and OCI cutover gates.

## Phase 10 — Retire legacy and temporary migration material

- [ ] Confirm all valuable legacy tests, fixtures, API behavior evidence, and
  developer guidance have been migrated or deliberately rejected with a
  recorded reason.
- [ ] Delete `web-legacy/` completely. This deletion is explicitly part of the
  approved target state; Git history is the recovery mechanism.
- [ ] Remove legacy package filters, dual-verification commands, CI jobs,
  Docker/SBOM branches, E2E serving paths, migration flags, compatibility
  aliases, and transition-only architecture text.
- [ ] Remove the raw prototype snapshot after verifying all adopted material is
  present in v1.0 docs, new components, design-lab surfaces, tests, and the
  retained provenance/change record.
- [ ] Keep only small, intentional design-lab fixtures/assets. Remove duplicate
  prototype component trees, stale visual artifacts, generated Impeccable
  caches, and unused dependencies.
- [ ] Run repository-wide searches for `web-legacy`, obsolete package names,
  old `web/` migration paths, v0.1 token/component names, experiment absolute
  paths, direct prototype imports, prototype production routes, and superseded
  design-system/version language. Resolve every live runtime, build, test, and
  authoritative-doc stale reference. Retained task/provenance history may keep
  explicitly labeled historical paths and versions and must be excluded from
  the zero-live-reference assertion rather than rewritten inaccurately.
- [ ] Re-run docs/context drift checks, new-web verification, E2E, supply-chain,
  and OCI verification after deletion.

### Phase 10 exit gate

- The repository contains one frontend and one design system; no build, test,
  documentation, dependency, or runtime path relies on legacy or raw prototype
  material.

## Phase 11 — Independent completion review and handoff

- [ ] Conduct independent frontend review of behavior, state coverage,
  accessibility, responsiveness, performance, maintainability, and visual
  polish using live Playwright evidence.
- [ ] Conduct independent security/privacy review of auth lifecycle, protected
  cache/state purge, object/function authorization assumptions, content
  rendering, logs/artifacts, design-lab separation, and negative isolation
  tests.
- [ ] Conduct risk-based acceptance/regression testing with an AC-to-result
  ledger for every in-scope criterion and explicit PASS/FAIL/BLOCKED status.
- [ ] Resolve all blocker/high findings and rerun affected focused plus
  regression checks. Record accepted lower-severity findings with owner and
  follow-up scope; do not call the migration complete with unresolved material
  accessibility, security, behavior, or visual-quality defects.
- [ ] Recheck all governing product, requirements, UI/UX, design-system, and ADR
  sources against the implemented end state.
- [ ] Reconcile planned work with actual files and verification; update this
  task to `completed`, retain it for external review, and start future features
  only as separate tasks against the new foundation.

# Migration traceability

Populate and maintain this table during Phase 0; split rows by independently
verifiable state group when necessary.

| Actor / surface | Production route(s) | Governing AC/UI decisions | Backend/API owner | Legacy evidence | Prototype evidence | Wave | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Shared auth and role shell | `/` (production gate: loading / idle login / denied / signing-out / ready shell); OIDC login/callback/logout via production API | `auth-resource-isolation.md`; Activity IA; ADR-019 `FE-DEC-1`–`12` | `production-api.tsx` `GET /v1/assessment/shell`; `production-logout.ts` | `web/src/api`, `ProductionAppShell`, `production-routes.test.tsx` | command strip, gangway, sign-out ceremony | 8.1 | inventoried |
| Administrator Activities/Campaign create | `/activities` | `assessment-setup.md` `AC-ACT-*`; `UI-ACT-DEC-*` | `production-assessment.ts` `GET/POST /v1/assessment/activities`; `GET /v1/assessment/source-options`; Query keys in `features/assessment` | `ProductionActivitiesPage`, `AssessmentActivitiesPage` + tests | Administrator Console Campaigns/registry | 8.1 | inventoried |
| Administrator setup/activation | `/activities/:activityId` → `setup`; `/activities/:activityId/setup` | Assessment setup ACs/`UI-ACT-DEC-2/3/6`; `PC-05`, `PC-06`, `PC-11` | `GET /v1/assessment/activities/:id`; PATCH; readiness; activate + idempotent activation reconcile | `ProductionAssessmentSetupRoute`, `AssessmentSetupPage` + tests | Administrator Console activation ceremony | 8.2 | inventoried |
| Administrator Enrollment list/assignment | `/activities/:activityId/cohorts/:cohortId/participants` | Enrollment portions of Submission/Attempt and auth ACs | `production-enrollment.ts` participant-options, enrollments list/create | `ProductionEnrollmentPage` + tests | Administrator Console Enrollments area | 8.2 | inventoried |
| Administrator Enrollment detail/accommodation | `/activities/:activityId/cohorts/:cohortId/enrollments/:enrollmentId` | Enrollment/accommodation/fairness ACs; `PC-11` | enrollment detail; timing v2; accommodation decide/revoke | `ProductionEnrollmentDetailPage` + tests | Administrator Console enrollment record | 8.2 | inventoried |
| Participant assignment and Submission intake | `/my-work`, `/my-work/:enrollmentId` | Implemented Assignment/Submission portions; `PC-03`, `PC-07` | `GET /v1/assessment/my-work`; assignment; timing v2; submission intake/versions/preview/download/cancel/finalize | `ProductionMyWorkPage`, `ProductionMyWorkDetailPage` + tests | Participant Home/Journey (visual); Journey local phase mutation is `remove` | 8.3 | inventoried |
| Approved P0 synthetic references | synthetic only: `/sessions/:sessionId`, `/review-work`, `/review-work/:caseId`, `/release-work`, `/release-work/:releaseId`, `/results`, `/results/:resultId` | Session, Evaluation/Review, Result/Release specs; `PC-01`–`PC-04`, `PC-08` | none in production parity | `SessionPage`, review/release/result pages + `sessionRuntimeView` tests | Participant Session, Reviewer Console, Result states | design lab / 8.4 | inventoried — design-lab/reference |
| Other synthetic/future examples | `/agents`, `/harnesses`, `/governance`; synthetic `/activities/:activityId`, `/activities/:activityId/enrollment` | later-release specs if approved; `PC-09`, `PC-10` | none | `AgentsPage`, `HarnessesPage`, `GovernancePage`, `ActivityDetailPage`, `EnrollmentPage` | Admin sample areas Users/Policies/Audit; Surfaces Index/Gallery | design lab / 8.4 | inventoried — design-lab/reference or remove unapproved actions |

# Review gates

| Gate | Required evidence | Approvers/review perspectives |
| --- | --- | --- |
| G0 Source readiness | Content-addressed prototype snapshot tied to base commit and recorded dirty state, provenance, license/secret review, production-versus-synthetic parity inventory, baseline verification | Task approval reference, architecture, security/privacy |
| G1 Documentation authority | Task approval reference, independent signoffs, approved design-system v1.0, synced visual references in P0 UI specs, passing docs/context checks | Product, UI/UX, architecture |
| G2 Transition safety | Task-delegated direction recorded in approved ADR, dual-build parity, production still legacy, import boundaries | Architecture, frontend, security/privacy |
| G3 Foundation quality | Component tests/gallery, accessibility, desktop/narrow screenshots, visual review | UI/UX, frontend reviewer, tester |
| G4 Journey wave | AC traceability, red/green evidence, focused/regression tests, browser states, security negatives | Frontend reviewer, security/privacy reviewer, tester |
| G5 Cutover | Full frozen-parity production E2E, isolated design-lab E2E, performance/bundle comparison, supply chain/SBOM/OCI, production-shaped smoke | Architecture, frontend, security/privacy, tester |
| G6 Retirement | Legacy/snapshot deletion, zero stale refs, all checks rerun, final independent review | Task approval reference and all applicable independent reviewers |

# Verification strategy

## Required command families

Exact commands may gain new/new-legacy/design-lab variants during the task;
record the executed form and result here.

```bash
python3 scripts/check_docs.py
pnpm install --frozen-lockfile
bash build/scripts/verify-web.sh
pnpm --filter @flex-agent/web test:e2e
bash build/scripts/authenticated-browser-profile.sh validate
bash build/scripts/verify-supply-chain.sh
bash build/scripts/verify-oci.sh
```

Add and run focused commands for:

- Impeccable context projection generation/check and skill smoke tests.
- Prototype snapshot manifest/hash verification and secret/license scanning.
- New/legacy/design-lab lint, typecheck, unit, build, and import-boundary tests.
- Production route exclusion from design-lab/prototype modules and fixtures.
- Production API-mode browser coverage through the authenticated-browser
  profile with synthetic accounts/data; the existing synthetic-journey E2E is
  migrated to the design-lab suite and is never reported as production parity.
- Contract/type compatibility and typed client tests.
- Query/auth epoch/cache/stale-response negative tests.
- Session design-lab state-model tests. Approved realtime/SSE/reconnect/
  ordering/reconciliation coverage remains a successor production task and is
  not cutover evidence for this migration.
- Bundle composition/size and OCI content inspection.
- Repository stale-reference searches after cutover and deletion.

## Browser evidence minimum

- Desktop and narrow viewports for every changed surface.
- Accessibility snapshots for landmarks, names, headings, relationships,
  dialogs, live regions, and keyboard paths.
- Focus-visible, loading, empty, populated, validation, error/retry, pending,
  disabled/read-only, permission-denied, destructive confirmation, success,
  terminal, and responsive states where applicable.
- Session-specific reconnect, partial-stream, no-action, pause, time warning,
  expiry, completion, and authorization-loss states in the design lab; repeat
  as production evidence only in a successor task that adds a production route.
- Reviewer/Release protected-content and stale/reconciliation states in the
  design lab, without treating synthetic evidence as production acceptance.
- 400-percent zoom/reflow, reduced motion, and forced-colors/high-contrast smoke.

# Risks and mitigations

| Risk | Mitigation / gate |
| --- | --- |
| Prototype visual behavior becomes accidental product scope | Concern-specific authority boundary, two-axis adoption matrix, `PC-01`–`PC-14`, P0 traceability, product/UI review before approval |
| Dirty experiment snapshot is irreproducible or requires unauthorized external writes | Do not change the source repo; record base commit/status/included dirty paths and create a content-addressed snapshot with SHA-256 manifest |
| Synthetic legacy pages become accidental production scope | Freeze production API-mode parity separately from the synthetic router; migrate synthetic pages only to the design lab; require a separate successor task for new production capability |
| Impeccable package has unclear rights or unsafe automation | Provenance/license/script review; no vendoring if rights fail; hooks and implicit activation off |
| Impeccable installer/update bundle is not integrity-pinned | Do not use the network bundle installer; resolve an immutable official Git commit, verify manifests/hashes, retain Apache-2.0 `LICENSE` and `NOTICE.md`, and review local drift |
| Generated `PRODUCT.md`/`DESIGN.md` drift from canonical docs | Deterministic generator, fingerprints, CI check-only validation, authority warnings |
| Two frontends become permanent | ADR-defined deadline/gates, one cutover, no runtime switch, mandatory Phase 10 deletion |
| Directory rename accidentally deploys incomplete new SPA | Explicit legacy production selector until G5; candidate build named separately |
| Prototype/design-lab code leaks into production | Separate entry graph/build, architecture/import tests, bundle and OCI inspection |
| New components diverge from approved prototypes | v1.0 contracts, gallery-first specimens, bounded Impeccable pass, screenshot review |
| Prototype dependencies weaken pinned supply chain | Rebuild on repository versions, exact pins, license/SBOM review, no copied package ranges |
| Rebuild weakens authentication or isolation | Preserve ADR-019 providers/epoch/purge boundaries; negative auth/isolation review each wave |
| Session design-lab migration implies production readiness or weakens future runtime constraints | Keep it design-lab-only, preserve approved realtime/runtime constraints in docs and production-neutral evidence, and require a separate successor task for production routing, SSE, and reconciliation verification |
| Tests are postponed until the end | TDD and browser/review gate per component and journey wave |
| Dark-only visual system harms accessibility | WCAG/forced-colors/non-color semantics/reduced-motion/reflow requirements and live verification |
| Component-first work over-abstracts future capabilities | Two-consumer or explicit-contract rule; later-release patterns stay docs/design-lab only |
| Raw reference material remains indefinitely | Phase 10 deletion gate after provenance and adopted outcomes are durable |

# Current state

Phases 0–2 review-gate cleanup is **approved**. Next work is **Phase 3
design-system v1.0**. Phases 0–2 remain sufficient to start that work; they
are not a complete authenticated production-parity visual baseline. Prototype
comparison screenshots (committed, mapped below), synthetic-harness E2E, SPA
OCI, and supply-chain/OCI checks are recorded. Impeccable adapters remain
authority-safe until v1.0 token projection in Phase 3. Production `web/` has
not been renamed.

# Decisions

- Execute the entire migration under this one retained task file; do not create
  parallel plan/progress/state files for the same work.
- Adopt the complete Impeccable skill as a controlled explicit layer rather
  than using only its generated outputs.
- Treat prototypes as the user-approved replacement design and future
  design-lab material. They win routine purely visual conflicts with
  design-system v0.1; approved repo requirements, features, journeys, semantic
  content, accessibility, and business flow win all other conflicts. Promote
  durable visual rules into authoritative v1.0 docs before production use.
- Resolve routine design conflicts without asking the user. Surface only
  business-critical, high-risk, or cross-cutting important changes under the
  explicit escalation thresholds in `# Authority model for the migration`.
- Replace the current design system in place and preserve its history through
  versioning, supersession notes, provenance, and Git—not through parallel
  runtime systems.
- Use one transitional rename and one final cutover; remove legacy completely
  when parity and release gates pass.
- Keep tests, documentation synchronization, review, accessibility, security,
  and browser evidence continuous throughout the plan.
- Freeze the rebuild to existing production API-mode parity. Approved but
  currently synthetic or backend-incomplete capability remains in the design
  lab and moves to a separate successor implementation task.
- Cursor/Codex skill catalogs must stay aligned. Role-skill Impeccable notes
  were mirrored into `.cursor/skills/`. The complete Impeccable package lives
  once under `.agents/skills/impeccable/`; `.cursor/skills/impeccable` is a
  symlink to that tree.
- Treat this task's user-approved Shipboard visual direction, concern-specific
  authority rule, complete controlled Impeccable integration, transition
  architecture, and mandatory legacy retirement as delegated execution
  decisions. Independent Product/UI/UX/architecture review verifies faithful
  transcription; ask again only if execution discovers a new escalation-
  threshold conflict or would materially change those decisions.
- Phase 0–2 review-gate cleanup is approved after `d3b0372` plus the 15-test
  count correction. Phase 3 proceeds without reopening the adoption matrix,
  prototype freeze, Impeccable provenance, or frontend migration architecture.

# Findings / deviations

- Planning inspection found path coupling to `web/` in root/workspace scripts,
  CI, boundary checks, E2E serving, SPA SBOM generation, OCI Dockerfile,
  architecture documentation, and contributor documentation. Phase 5 and
  Phase 9 explicitly cover these surfaces.
- The experiment uses pnpm `11.21.0` and newer/caret dependency ranges while
  Flex Agent pins pnpm `9.6.0`, Node `22.18.0`, React `19.2.8`, Vite `8.1.5`,
  and TypeScript `5.9.3`. The new app follows Flex Agent's locked toolchain.
- The prototype is dark-only and uses Michroma/Sometype Mono, zero-radius
  notched geometry, and a stricter drawn-glyph rule. The decided v1.0 defaults
  reconcile these with accessibility, supply-chain requirements, and ADR-019's
  Lucide boundary.
- A focused behavior audit found fourteen pre-resolved conflict families,
  recorded as `PC-01`–`PC-14`. The material cases are combined Review/Release,
  premature participant workflow disclosure, direct local Campaign activation,
  invalid Campaign auto-selection, client-authored Participant/Session
  lifecycle, unapproved management/export/delete actions, route/terminology
  drift, timezone gaps, and accessibility constraints. No case requires a new
  business decision: each is resolved by retaining the visual direction and
  implementing the existing repo-approved contract.
- The current `web/` has two distinct route graphs. Production API mode exposes
  Home, Activities/setup, Cohort Participant/Enrollment, and My Work; the
  alternate browser adapter exposes Session, Review, Release, Results,
  Governance, Agents, and Harnesses with synthetic/development behavior. The
  readiness review corrected the plan so only the former is production parity
  and the latter is design-lab/future evidence.
- The experiment contains extensive generated `.playwright-mcp` and
  `.impeccable` material. Only explicitly reviewed, minimal evidence is eligible
  for the temporary snapshot.
- The experiment's installed Impeccable skill declares `4.1.1`, but no
  `LICENSE`, `NOTICE`, or `COPYING` file was found in the experiment root or
  skill directory. The official upstream is Apache-2.0 and carries both
  `LICENSE` and `NOTICE.md`; Phase 1 therefore resolves the local files to an
  immutable upstream revision and imports the notices before vendoring.
- The upstream network installer/update path has a reported open integrity and
  temporary-path issue. This plan avoids that path entirely and uses a pinned,
  hash-verified official Git source with automatic hooks disabled.
- No matching active implementation task existed when this plan was created.
- Execution-time experiment HEAD is `f724b68b11c2a147e59864f5789b260baaa50641`
  (clean), two commits after planning-review `c52eeda3`. Those commits strip
  design-process archive/shims and land the previously dirty gallery/reviewer
  CSS. Snapshot used current HEAD content; no external write.
- Other `.work/active/` files are `completed` except
  `text-interaction-controller-contract.md` (`planned`, explicit product-lead
  gate, no `web/` implementation). Migration freeze for `web/` is in effect
  until Phase 5 rename.
- Pre-migration `bash build/scripts/verify-web.sh` failed on one existing
  unit test: `AssessmentSetupPage` access-loss focus assertion. Lint 0 errors /
  11 warnings; typecheck passed; production build produced
  `index-DAqGTSFM.js` 546.88 kB (gzip 160.45 kB) and
  `index-52J12w0Z.css` 19.16 kB (gzip 4.48 kB).
- Phase 0 completion run (2026-08-27): `pnpm --filter @flex-agent/web
  test:e2e` 6/6 passed (synthetic harness, NGINX `:5173` + API `:18080`);
  `docker build -f deploy/docker/spa.Dockerfile -t flex-agent-oci-spa:local`
  produced JS/CSS matching the Vite baseline; SPA probe served as `nginx`
  with 0 source maps; `bash build/scripts/verify-supply-chain.sh` and
  `bash build/scripts/verify-oci.sh` exited 0. Grype reports High findings
  in the pinned nginx/alpine SPA base (`tiff`, `libcrypto3`/`libssl3`);
  the scripts still completed because those scans are informational in
  `scan-oci-image-sboms.sh`. Authenticated Keycloak-browser E2E was not
  started. `pnpm licenses` printed npm peer-missing noise and still wrote
  `artifacts/supply-chain/npm-licenses.json`.
- Checkpoint review found Cursor role skills lagged `.agents/skills/` and
  Phase 0/1 checklists over-claimed E2E/OCI and Impeccable doctor. Catalogs
  were aligned via symlink and role-skill mirrors; the deferred items were
  completed in this pass.
- Impeccable doctor against root adapters (`--json --target .`, no `--fix`):
  `product-schema-legacy` (route: do not `init`) and `design-md-coverage`
  (mention: do not `document` until v1.0). `context.mjs --target .` resolved
  `PRODUCT.md` / `DESIGN.md`. `scripts/impeccable_context.py check` passed.
  Canonical `docs/` were not rewritten.
- Authenticated production-profile screenshots were not captured: the
  authenticated-browser compose stack was not started. Current-web comparison
  evidence is the synthetic-mode workspace 502/unavailable panel. Prototype
  surfaces were captured at 1440×900 and 390×844 under `.playwright-mcp/`.
- Consistency re-check (2026-08-27): adapter check passed; Impeccable unit
  tests later grew to 15 and all passed on the post-`d3b0372` rerun; doctor
  findings unchanged (`product-schema-legacy`,
  `design-md-coverage`); `.cursor/skills/impeccable` still symlinks to
  `.agents/skills/impeccable`; hashed snapshot is 215 files, plus snapshot
  `README.md`/`MANIFEST.json` on disk. The Phase 2 matrix is family-level;
  remaining modules are listed in the remaining-families table. Current E2E
  still exercises synthetic Session/Release, which the plan already excludes
  from production parity. Playwright can leave `flex-agent-e2e-spa` bound to
  `:5173`; remove it before a later E2E run.
- External review of `6622c96`/`73bedd0`/`c66152a` required gate/evidence
  corrections before treating Phases 0–2 as a closed reviewed baseline:
  Apache-2.0 notice on `agents/openai.yaml`; `.impeccable` runtime ignore plus
  tracked-path/secret/participant-email checks; Phase 0 screenshot item scoped
  as prototype comparison plus deferred authenticated current-web capture;
  `license_review` on the snapshot manifest; DESIGN.md token projection
  explicitly deferred to Phase 3; committed PNG evidence map.
- Follow-up to `d1a95f5`: ignore and reject upstream `.impeccable/critique/`
  (`CRITIQUE_DIR`), keep plural `critiques/` as a leftover alias, classify
  `.impeccable-live.json` / `.impeccable-live/` as runtime, and align the
  Phase 1 checkbox with email/secret tripwires plus Gitleaks/review policy
  rather than claiming generic participant-content detection.
- External review of `d3b0372` approved Phase 0–2 review-gate cleanup (critique
  path, live-state guard, claim/enforcement match). One leftover: this file
  still said 12 adapter unit tests after three new tests landed. Corrected to
  15 after a rerun of
  `python3 -m unittest discover -s scripts -p 'test_impeccable_context.py'`
  (15 OK). Non-blocking later hardening: drop the substring fallback in
  `_is_snapshot_impeccable()` and keep only canonical `relative_to()`
  containment. Proceed to Phase 3; do not reopen the adoption matrix,
  prototype freeze, Impeccable provenance, or frontend migration architecture.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Required workflow and role skills read | passed | implementation-workflow, business analyst, architect, UI/UX, documentation, frontend, review, security/privacy, tester guidance inspected during planning |
| Governing product docs read | passed | Concept model v0.5, MVP scope v0.4, Product overview v0.4 |
| UI/UX authority and design-system status read | passed | UI/UX hub, design-system v0.1 README, implementation guide |
| Frontend architecture/toolchain inspected | passed | ADR-010, ADR-019, frontend architecture, workspace docs, package/workspace/toolchain files |
| Codex skill manifest policy checked | passed | official `Build skills` documentation supports optional `agents/openai.yaml` and `policy.allow_implicit_invocation: false` while preserving explicit `$skill` use |
| Target worktree planning status | passed | clean at planning inspection before adding this task file |
| Prototype source planning status | passed | freeze at clean `f724b68`; planning-review base `c52eeda3`; see snapshot MANIFEST |
| Production scope audit | passed | production API-mode router separated from synthetic router; cutover scope narrowed to current production-backed parity |
| Plan consistency/readiness audit | passed | phases 0–11 are ordered with exit gates; `PC-01`–`PC-14` are complete; source capture needs no external write; synthetic/future capability is excluded from production parity; approval delegation and legacy retirement are explicit |
| Authenticated production-profile definition | passed | `bash build/scripts/authenticated-browser-profile.sh validate` |
| Plan markdown/content validation | passed | `python3 scripts/check_docs.py` |
| Implementation verification | in progress | Phase 0–2 review gates closed and approved; Phase 3 design-system v1.0 next |
| Experiment freeze | passed | HEAD `f724b68` clean; planning base `c52eeda3`; 215 hashed files in `.work/resources/impeccable-prototype-snapshot/MANIFEST.json` |
| Impeccable adapter unit tests | passed | `python3 -m unittest discover -s scripts -p 'test_impeccable_context.py'` 15 tests (rerun 2026-08-27 after `d3b0372` review) |
| `verify-web.sh` | failed (pre-existing) | 181 pass / 1 fail `AssessmentSetupPage` access-loss focus; lint 11 warnings 0 errors; typecheck pass |
| Production SPA build / bundle | passed | Vite `web/dist`: JS 546884 B / gzip 160.45 kB; CSS 19163 B / gzip 4.48 kB |
| Prototype desktop/narrow screenshots | passed | 14 committed synthetic prototype PNGs; see evidence map below. Accessibility snapshots remain untracked. |
| Current-web comparison screenshots | partial — deferred for parity | Two committed 502 workspace panels (desktop/narrow). Authenticated production states not captured; not required to start Phase 3. |
| Synthetic-harness web E2E | passed | `pnpm --filter @flex-agent/web test:e2e` 6/6. Current-web baseline, including synthetic Session/Release; not the frozen production API-mode route set. Re-run required stopping leftover `flex-agent-e2e-spa` on `:5173`. |
| SPA OCI build + HTTP probe | passed | `flex-agent-oci-spa:local`; `/` 200; user `nginx`; 0 `*.map`; assets match Vite JS/CSS hashes |
| `verify-supply-chain.sh` | passed | exit 0; SPA SBOM 566 components; artifacts under `artifacts/supply-chain/` |
| `verify-oci.sh` | passed | API/worker/SPA health, non-root users, no SPA source maps, graceful stop |
| Impeccable context check | passed | `python3 scripts/impeccable_context.py check`; `node .agents/skills/impeccable/scripts/context.mjs --target .` |
| Impeccable doctor (report-only) | passed with expected adapter findings | `doctor.mjs --json --target .`: `product-schema-legacy` (route), `design-md-coverage` (mention); `--fix` not run |
| Two-axis adoption matrix | passed | `# Prototype two-axis adoption matrix` and `# Prototype behavior rejections` (`BR-01`–`BR-14` → `PC-01`–`PC-14`) |
| Authenticated Keycloak-browser E2E | not run | compose profile not started; not required to close Phase 0 once synthetic E2E ran |
| Snapshot secret scan | passed | no credential/private-key matches; `license_review` in `MANIFEST.json`; font/npm license deferred to Phase 3 |
| Impeccable tracked-path guard | passed | ignore + check cover `.impeccable/critique/` (upstream `CRITIQUE_DIR`) and `.impeccable-live*`; tripwire is secrets + non-synthetic emails, not a PII detector. Snapshot shots remain exempt. Gitleaks remains the repo-wide secret scan. |
| Codex/Cursor Impeccable catalog | passed | five role skills mirrored; `.cursor/skills/impeccable` symlink to `.agents/skills/impeccable` |

## Playwright screenshot evidence map

Auto-named MCP files under `.playwright-mcp/`. Purpose is comparison evidence,
not visual acceptance. Authenticated current-web production states remain
deferred.

| File | Source | Surface / route | State | Viewport | Actor |
| --- | --- | --- | --- | --- | --- |
| `page-2026-08-27T01-15-17-315Z.png` | prototype | `/surfaces` Channel Index | populated index | 1440×900 | none (catalog) |
| `page-2026-08-27T01-15-38-270Z.png` | prototype | `/participant-home` Status Bays | roster populated (`PC-03` disclosure present in prototype) | 1440×900 | Participant |
| `page-2026-08-27T01-16-10-248Z.png` | prototype | `/participant-journey` Assignment Station | first arrival / briefing | 1440×900 | Participant |
| `page-2026-08-27T01-16-35-989Z.png` | prototype | `/participant-session` Examination Console | briefing/consent overlay | 1440×900 | Participant |
| `page-2026-08-27T01-16-58-948Z.png` | prototype | `/admin-console/enrollments` | draft campaign, 120-row manifest | 1440×900 | Administrator |
| `page-2026-08-27T01-17-27-245Z.png` | prototype | `/reviewer-console` Review Docket | mixed docket (`PC-01` combined release still in prototype) | 1440×900 | Reviewer |
| `page-2026-08-27T01-17-47-544Z.png` | prototype | `/shared/gallery` Component Deck | foundations / colors | 1440×900 | none (catalog) |
| `page-2026-08-27T01-18-19-705Z.png` | prototype | `/shared/gallery` Component Deck | foundations index | 390×844 | none (catalog) |
| `page-2026-08-27T01-18-40-499Z.png` | prototype | `/participant-home` Status Bays | roster populated | 390×844 | Participant |
| `page-2026-08-27T01-19-00-396Z.png` | prototype | `/surfaces` Channel Index | populated index | 390×844 | none (catalog) |
| `page-2026-08-27T01-19-26-528Z.png` | prototype | `/participant-session` Examination Console | briefing/consent overlay | 390×844 | Participant |
| `page-2026-08-27T01-19-49-465Z.png` | prototype | `/reviewer-console` Review Docket | mixed docket | 390×844 | Reviewer |
| `page-2026-08-27T01-20-12-358Z.png` | prototype | `/admin-console/enrollments` | draft campaign manifest | 390×844 | Administrator |
| `page-2026-08-27T01-20-31-307Z.png` | prototype | `/participant-journey` Assignment Station | first arrival / briefing | 390×844 | Participant |
| `page-2026-08-27T01-21-20-383Z.png` | current `web/` | workspace load | synthetic mode without API: 502 | 1440×900 | unauthenticated |
| `page-2026-08-27T01-21-37-543Z.png` | current `web/` | workspace load | synthetic mode without API: 502 | 390×844 | unauthenticated |

# Blockers

None at planning time. Content-addressed prototype capture and Impeccable
license/provenance verification are planned entry gates, not unresolved product
questions and require no external-repository write. If either verification gate
fails during execution, mark the affected phase blocked and do not copy or
execute the unverified material.

# Open questions

None. Defaults have been selected for all currently known non-business-critical
choices. The approved prototypes automatically win ordinary purely visual
conflicts with design-system v0.1; approved repo sources automatically win
product, feature, journey, semantic, accessibility, security/privacy, and
business-flow conflicts. `PC-01`–`PC-14` are decided implementation constraints,
not open questions. Escalate only a genuinely new case under the business-
critical, high-risk, or cross-cutting important thresholds defined in
`# Authority model for the migration`; otherwise decide, document when
consequential, and continue.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [ ] Governing specifications were rechecked
- [ ] Design-system v1.0 and affected UI/UX docs are approved and synchronized
- [ ] Impeccable is provenance-reviewed, explicitly controlled, and drift-checked
- [ ] Production and design-lab entry graphs are verified isolated
- [ ] Full frozen production-parity role journeys and applicable negative authorization E2E pass
- [ ] Playwright accessibility and desktop/narrow visual evidence is reviewed
- [ ] Supply-chain, SBOM, OCI, bundle, and artifact-content checks pass
- [ ] `web-legacy/`, raw prototype snapshot, and transition-only paths are removed
- [ ] Repository-wide stale-reference searches are clean
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Independent frontend, security/privacy, and tester reviews are complete
- [ ] Task state is safe and complete for external review
