---
id: shipboard-production-ux-reset
status: in-progress
created: 2026-08-28
updated: 2026-08-29
owner_visual_pass: in-progress
product_contract_unavailable: accepted
engineering_note: Home and populated My work omit OperateArea etched well around AssignmentPlate grids; tables/empty/assignment-station keep the well
---

# Goal

Reset the Flex Agent production frontend around the approved Shipboard design
system: retire the former production UI/UX authority, remove the current
production page composition from `web/`, remove `web-legacy/`, reconstruct the
complete approved P0 information architecture and user journeys, and then
rebuild the production SPA from first principles against approved product,
requirements, architecture, security, accessibility, and design-system
contracts.

This is also a full visual redesign. The current rebuilt production pages are a
disposable behavioral/technical first pass, not a visual baseline. Each page
may be deleted and rebuilt from scratch. Production must follow the visual
hierarchy, density, spatial composition, navigation treatment, work planes,
state presentation, and craft demonstrated by the design-lab sample surfaces
while replacing every synthetic behavior with approved production truth.

The reset must leave no ambiguous second production frontend, no production
route copied from the design lab, and no deployment path that can accidentally
publish the design lab or an empty shell while production UI is intentionally
absent.

# Outcomes and definition of done

- The previous UI/UX specifications are absent from the live authority set and
  are represented only by a concise retirement ledger plus Git history.
- `docs/ui-ux/README.md` unambiguously identifies what is Approved, Draft,
  Superseded, and unavailable during the reset.
- `docs/ui-ux/design-system/**`, `web/src/design-system/**`, the approved
  shared visual foundations, and `web/src/design-lab/**` remain the official
  Shipboard look-and-feel reference.
- The design lab remains isolated, synthetic, and non-production; its routes,
  fixtures, and interactions do not become product requirements.
- Every production surface has an explicit design-lab/Component Deck visual
  donor and an adopt/adapt/reject mapping. Current production composition is
  never the visual acceptance baseline.
- Available production pages reach specimen-level Shipboard craft: intentional
  first-viewport composition, strong task hierarchy, coherent work planes,
  purposeful density, polished empty/loading/error/denied states, and no large
  accidental dead zones or generic one-plate placeholders.
- All existing production page, production route, and production shell
  composition in `web/` is removed after an exact keep/delete inventory.
- `web-legacy/` and every build, workspace, CI, OCI, SBOM, test, and
  documentation reference that treats it as live production are removed.
- During the reset interval, production frontend build/deploy entry points fail
  closed with a clear message; design-lab verification remains independently
  runnable.
- A new, coherent P0 IA and end-to-end journey set is approved before
  production page implementation begins.
- The rebuilt frontend covers the approved P0 slice and all applicable states,
  uses the existing server contracts or records explicit contract blockers,
  and preserves organization, activity, participant, session, evidence,
  review, result, and release boundaries.
- Production build, supply-chain, OCI, authenticated-browser, accessibility,
  responsive, security/privacy, and cross-cutting review gates pass before the
  production SPA is restored as deployable.
- No `web-legacy/` directory, parallel production SPA, stale UI authority link,
  or reset-only placeholder remains at completion.

# Governing sources

- `AGENTS.md`
- `.work/README.md`
- `docs/README.md` — authority by concern
- `docs/product/concept-model.md`
- `docs/product/mvp-scope.md`
- `docs/product/overview.md`
- `docs/requirements/README.md` and the approved P0 feature specifications it
  catalogs
- `docs/ui-ux/README.md` — to be reset as the UI/UX authority index
- `docs/ui-ux/design-system/README.md`
- `docs/ui-ux/design-system/implementation-guide.md`
- `docs/ui-ux/design-system/change-record.md`
- `docs/architecture/frontend-architecture.md`
- `docs/architecture/decisions/ADR-019-frontend-state-and-library-boundaries.md`
- `docs/architecture/decisions/ADR-020-frontend-rebuild-transition-and-design-lab-isolation.md`
- `docs/architecture/decisions/ADR-021-production-frontend-reset-and-single-spa-topology.md`
  — Approved 2026-08-28; no-production-UI interval, fail-closed deployment
  posture, and final single-SPA topology
- An approved Product/UI/UX retirement decision recorded through
  `docs/ui-ux/retired-authority.md` and `docs/ui-ux/README.md`; the ADR may
  reference this decision but cannot approve or supersede UI/UX authority
- The new approved UI/UX specifications produced by this task; until they are
  approved, product and requirements sources govern behavior and the design
  system governs visual language, but no production page journey is authorized
- `web/src/design-lab/README.md`, `/design-lab/shared/gallery`, and the
  design-lab admin, participant, journey, session, and reviewer sample surfaces
  as visual/composition evidence only
- Owner visual direction confirmed 2026-08-28: production must visibly follow
  design-lab sample quality; merely sharing Shipboard tokens or outer chrome is
  not acceptance

# Authority model during the reset

1. Approved product concepts, MVP boundaries, feature requirements, ADRs not
   superseded by an approved successor, and security/privacy invariants remain
   authoritative.
2. The Shipboard design-system documentation and implementation are the visual
   authority. The design lab is the primary composition donor and comparison
   evidence. It governs neither product behavior nor production data.
3. The former six journey/specification files are retired as a single authority
   reset. They must not continue as hidden implementation requirements.
4. Before replacement UI/UX specifications are approved, there is deliberately
   no authoritative production-page flow. Agents may inventory and design, but
   may not rebuild production pages from the retired flows or lab fixtures.
5. New production implementation must trace to approved P0 acceptance criteria,
   approved replacement UI/UX decisions, applicable ADRs, and design-system
   modules.
6. If new UX work exposes a product, server-contract, authorization, retention,
   or architecture ambiguity, record it in the owning authoritative artifact;
   do not resolve it silently in component code or this task file.

# Scope

## In

- Reconcile the interrupted `impeccable-frontend-rebuild` task as an
  owner-superseded predecessor without claiming it completed.
- Create and approve the reset/single-SPA architecture decision for technical
  realization before destructive code or delivery changes.
- Create and approve the retirement decision under Product/UI/UX authority
  before removing any approved UI/UX specification.
- Retire these current UI/UX authority files and rebuild their subject matter
  from product and requirement sources rather than editing their flows in
  place:
  - `docs/ui-ux/activity-campaign-journey.md`
  - `docs/ui-ux/assessment-campaign-setup.md`
  - `docs/ui-ux/submission-attempt.md`
  - `docs/ui-ux/text-session.md`
  - `docs/ui-ux/evidence-evaluation-human-review.md`
  - `docs/ui-ux/result-release.md`
- Add `docs/ui-ux/retired-authority.md`, an authoritative retirement/status
  ledger—not a behavioral specification—containing former document
  name/version/status, retirement reason, successor status, and Git provenance;
  do not keep duplicate archived copies of their content in the live docs tree.
- Reconcile every inbound link and approval claim affected by the retirement,
  including product, requirements, architecture, design-system, contributing,
  generated adapter, and task references where they describe current authority.
- Preserve and, where required, update status/link metadata in
  `docs/ui-ux/design-system/**` without changing the approved Shipboard visual
  language as part of the reset.
- Inventory and remove current production page, route, shell, and app-entry
  composition from `web/`; retain only explicitly classified production-safe
  infrastructure and the official design-system/design-lab implementation.
- Preserve design-lab-neutral routing/test utilities only when their ownership
  and import boundary remain clear; move them if leaving them under a
  production-named location would be misleading.
- Remove all tracked `web-legacy/` sources and every live repository reference,
  workspace member, lockfile importer, verification path, container input,
  change detector, and deployment assumption tied to it.
- Establish and test a fail-closed frontend reset state before restoring a
  production application.
- Reconstruct the approved P0 IA, actor journeys, route model, responsive
  behavior, content, state coverage, accessibility, and interaction contracts.
- Rebuild production frontend vertical slices, tests, browser evidence, build
  pipelines, and deployment artifacts using the approved replacement docs.
- Delete and rebuild any or every current `web/src/pages/**` implementation,
  page-local composition, and page-local styling when that is the cleanest path
  to design-lab-level quality. Preserve valid behavior contracts and tests or
  replace them with equivalent stronger tests before deleting implementation.
- Combine the `frontend-developer` workflow with explicitly authorized bounded
  `$impeccable` `shape`, `harden`, `adapt`, `polish`, and `audit` passes for the
  visual-rebuild waves.
- Maintain a page-by-page visual adoption and acceptance matrix mapping the
  production actor/route/state to design-lab donors, design-system modules,
  production behavior authority, rejected synthetic behavior, and screenshot
  evidence.
- Perform independent backend-contract, frontend, security/privacy, testing,
  and final cross-cutting readiness reviews.
- Keep the proposed text Interaction Controller and all voice behavior deferred
  unless a separate authority-by-concern decision explicitly expands P0 and
  replans this task before the applicable UX approval freeze.

## Preserve unless an exact dependency review proves page coupling

- `docs/ui-ux/design-system/**`
- `web/src/design-system/**`
- `web/src/design-lab/**`
- approved shared tokens, fonts, icons, styles, and visual assets
- API clients, auth/OIDC behavior, query ownership, form/validation libraries,
  error normalization, generated/typed contracts, and other production-safe
  frontend infrastructure
- backend, worker, persistence, deployment, and feature tests unrelated to the
  retired frontend entry point
- product, requirements, security/privacy, and architecture history outside
  explicitly superseded frontend-transition decisions
- `.work/` history and reviewed synthetic Playwright evidence
- production-safe API/auth/query/form/contracts infrastructure and valid
  user-observable tests even when the page component that consumes them is
  deleted and rebuilt

## Out

- Changing product meaning, adding P1–P3 capabilities, or turning design-lab
  examples into MVP requirements
- Text Interaction Controller behavior, voice interaction, interruption,
  playback, TTS, or other deferred Session capabilities unless separately
  approved and incorporated through an explicit replan
- Backend behavior changes except separately planned contract work discovered
  as a blocker to an approved P0 journey
- Replacing or restyling the approved Shipboard design system
- Impeccable hooks, live mode, unbounded detector/polish loops, or letting a
  visual command override approved behavior, accessibility, security, or TDD
- Maintaining a compatibility layer, redirect matrix, or runtime dual-SPA
  bridge for the deleted frontend
- Publishing the design lab, a placeholder page, or a blank bundle as the
  production application
- Importing design-lab routes, fixtures, stores, demo controls, surface CSS, or
  synthetic mutations into the production entry graph
- Accepting the current production pages merely because they reuse Shipboard
  tokens, `ManagementLayout`, `OperateArea`, or the same outer shell
- Preserving duplicate retired UI documents in an archive that could be
  mistaken for current authority
- Commit, push, pull request, or deployment actions unless separately requested

# Safety and implementation rules

- This file is a prepared plan. No reset implementation starts until the owner
  explicitly activates it; then set `status: in-progress` and mark one step
  `[>]`.
- Before any deletion, generate exact tracked-file and inbound-reference
  manifests and classify each affected file as remove, retain, move, or amend.
- Delete only explicit, resolved paths. Tracked removals remain recoverable from
  Git; report material deletions and their recovery path.
- The Product/UI/UX retirement decision must be approved before removing the
  six approved UI/UX specifications. The architecture reset ADR and fail-closed
  transition contract must be approved before deleting `web-legacy/` or the
  current production entry graph. Neither approval substitutes for the other.
- Never make `design-lab.html`, design-lab routes, synthetic fixtures, or lab
  environment adapters the production entry point.
- The user explicitly authorizes bounded Impeccable use for this visual rebuild.
  In each execution session run Impeccable context once, use `shape` before a
  materially reconstructed wave, then `harden`, `adapt`, and `polish` only after
  behavior is specified and green. Use `audit` for review. Hooks and live mode
  remain off.
- Use bounded visual passes: capture desktop and narrow screenshots together,
  fix the full observed defect batch, confirm once, and stop. Run the mechanical
  Impeccable detector once over final changed UI targets, not during concept
  selection.
- A production page may be deleted and rebuilt from a blank component surface.
  Do not preserve weak composition for code-history reasons. Preserve the real
  route, behavior contract, server authority, accessibility obligations, and
  valid tests.
- Design-lab code is a donor, not a production dependency. Promote a generally
  reusable primitive into `web/src/design-system/**` or recreate the composition
  with production-safe design-system imports; never import `web/src/design-lab/**`
  or lab-only CSS from production.
- Preserve server authority: the client never invents lifecycle truth,
  authorization, activation readiness, timing, evidence, evaluation, review,
  release, or result state.
- Lower scopes may narrow but never widen delegated capabilities. Client-supplied
  organization, activity, participant, session, role, or ownership identifiers
  are never trusted as authorization.
- Treat submissions, transcripts, voice, evaluations, revisions, results, and
  audit records as sensitive. Use synthetic data only in fixtures and browser
  artifacts.
- For each implemented behavior, map stable acceptance IDs, run an observed red
  test, make the minimum change green, refactor, then run focused and
  proportionate regression checks. Record any legitimate test-first exception.
- Every UI-affecting slice must be exercised with the configured Playwright MCP
  through real interactions, accessibility snapshots, and inspected desktop
  and narrow screenshots. Store artifacts only under `.playwright-mcp/`.
- Use explicit `$impeccable` commands only when separately invoked by the owner;
  approved docs remain authority even when visual critique is used.

# Plan

## Phase 0 — Activate, reconcile ownership, and freeze exact manifests

- [x] Recheck `git status`, current branch/worktrees, `.work/active/`, and any
  overlapping owner or agent work before changing files.
- [x] Activate this task (`status: in-progress`, one `[>]` marker) only after
  explicit owner instruction.
- [x] Record `impeccable-frontend-rebuild.md` as an owner-superseded predecessor:
  retain completed evidence, stop its current execution cursor, link this task,
  and use `blocked` rather than falsely claiming completion.
- [x] Recheck the predecessor linkage and status at activation so another owner
  or agent change cannot create two execution cursors.
- [x] Add an explicit reset dependency to
  `text-interaction-controller-contract.md`: it remains planned/deferred, cannot
  use the retiring Text Session specification as activation authority, and
  cannot enter the P0 rebuild without separate Product approval and a replan of
  both tasks.
- [x] Record a baseline commit/status plus exact manifests for:
  current UI authority and inbound links; `web/src/pages/**`; production route,
  shell, entry, and style graphs; `web-legacy/**`; root/workspace/lockfile
  entries; CI/build/OCI/SBOM/change-detection references; and design-lab/shared
  dependencies that must survive.
- [x] Build a keep/remove/move/amend matrix for every affected tracked path.
  Resolve ambiguous shared utilities by actual import graph and tests, not by
  directory name alone.
- [-] Capture pre-reset documentation, frontend, design-lab, supply-chain, and
  OCI verification results so later failures are attributable. (Skipped: owner
  activated into a single change set; baseline commit remains `8c729a1`.)
- [x] Confirm no uncommitted user work overlaps a removal target. If overlap is
  found, stop that removal and record the exact blocker.

### Phase 0 exit gate

One task owns execution; the predecessor cannot be mistaken for current work;
all deletion targets and preserved dependencies are explicit; baseline evidence
and recovery provenance are recorded.

## Phase 1 — Approve each reset decision in its owning concern

- [x] Author
  `docs/architecture/decisions/ADR-021-production-frontend-reset-and-single-spa-topology.md`
  to supersede ADR-020 only where its dual-build/cutover strategy conflicts with
  the confirmed reset. Preserve its design-lab isolation lessons and ADR-019
  state/library boundaries.
- [x] Specify the target topology: one production SPA in `web/`, one isolated
  design-lab entry in that package, no `web-legacy/`, and no runtime sharing of
  lab routes, fixtures, stores, or environment adapters.
- [x] Specify the intentional interval with no deployable production frontend,
  including which build/deploy/compose/OCI commands fail closed, the exact
  operator-facing failure, and how design-lab checks remain runnable.
- [x] Define recovery and rollback through Git and immutable build provenance;
  do not retain a hidden legacy runtime as rollback infrastructure.
- [x] Under Product/UI/UX authority, author the proposed retirement decision in
  `docs/ui-ux/retired-authority.md` and the corresponding status transition in
  `docs/ui-ux/README.md`. Define the retired documents, effective transition,
  Git provenance, replacement-document approval flow, and prohibition on page
  implementation before replacement approval.
- [x] Make the approved retirement decision explicitly effective only when the
  Phase 2 status/link update and file removals are applied atomically. Until
  then, the existing specifications remain current and no mixed authority state
  may be claimed.
- [!] Obtain Product Lead and UI/UX Lead approval for the retirement decision,
  with Architecture and Security/Privacy review for cross-concern consequences.
  Do not use ADR approval as a substitute for this approval. (In-repo metadata
  is Approved; independent signed review artifacts are still outstanding.)
- [!] Review the ADR and technical transition contract from architecture,
  security/privacy, frontend, backend-contract, testing, and operations
  perspectives; resolve all blocking findings.

### Phase 1 exit gate

The Product/UI/UX retirement decision and successor ADR are each Approved in
their owning concerns; the temporary fail-closed state is testable; and the
authority/deletion order is agreed before destructive changes begin.

## Phase 2 — Retire old UX authority and remove both production compositions

- [x] Execute the approved retirement decision in
  `docs/ui-ux/retired-authority.md` and `docs/ui-ux/README.md` so no retired
  journey appears Approved or current.
- [x] Rebuild successor UI/UX specs at the same paths (v1.0); retired blobs
  remain in Git at `eb9c398` rather than a second live archive.
- [x] Reconcile current-authority links and claims; regenerate `PRODUCT.md` /
  `DESIGN.md`; add docs checks for stale UI authority.
- [x] Remove prior production page composition, `web-legacy/`, and live
  production references; isolation tests fail if `web-legacy` returns.
- [-] Separately tested empty-interval fail-closed `pnpm build` exit-1 was not
  run; production `web/` was restored in the same change set. Isolation tests
  encode fail-closed artifact rules. Design-lab unit tests remain independently
  green.

### Phase 2 exit gate

Live old UX authority is retired. `web-legacy/` is gone. Production SPA in
`web/` is restored rather than left unpublished. Design-lab isolation holds.

## Phase 3 — Reconstruct the P0 experience and approve it

- [x] Replacement IA, journeys, states, routes, and accessibility contracts
  live in the reconstructed `docs/ui-ux/*.md` set (Approved v1.0).
- [!] Independent Product/UI/UX/Architecture/Security approval artifacts are
  still outstanding; documents carry in-repo Approved metadata. Solo project:
  not blocking UX task closeout.

## Phase 4 — Prepare the production implementation architecture

- [x] Frontend architecture, production routes, and HTTP contract readiness
  matrix are recorded.
- [-] Independent readiness reviews. Skipped (solo project).

## Phase 5 — Rebuild production vertical slices with TDD

- [x] Behavioral/technical first pass — Slice 1: auth gate, shell, home, errors,
  protected cache cleanup.
- [x] Behavioral/technical first pass — Slice 2: activities, setup, enrollment,
  bounded accommodation request.
- [x] Behavioral/technical first pass — Slice 3: My Work, assignment detail,
  Submission intake
  (begin/complete/cancel/finalize). Attempt start is host-contract blocked.
- [x] Slices 4–6: Session/Review/Release/Result remain honest
  contract-unavailable pages. **Product accepted** interim behavior 2026-08-29;
  host HTTP deferred.
- [x] Observed red then green for My Work intake tests. Playwright MCP covered
  authenticated and unauthenticated journeys (see Verification).
- [x] Visual acceptance reopened in Phase 5 and **closed in Phase 7** corrective
  rebuild. First-pass behavior evidence remains regression contract only.
- [-] Independent per-slice frontend/security reviews. Skipped (solo project).

## Phase 6 — Restore the single production delivery path

- [x] Production build points at `@flex-agent/web`; bundle/isolation checks pass.
- [x] `pnpm verify:web` passed 2026-08-28 (production + design-lab E2E).
- [x] `pnpm verify:web:production` passed 2026-08-29 (lint, typecheck, production
  tests, isolation, production build).
- [x] `pnpm verify:dotnet`, `pnpm verify:supply-chain`, and `pnpm verify:oci`
  passed 2026-08-28/29.
- [!] `pnpm verify:oidc` not run: canonical profile binds `:18080`, occupied by
  live compose. Optional before closeout.
- [>] Reconcile remaining delivery gaps after the corrective visual rebuild.
  Task stays in-progress until owner visual acceptance on implemented surfaces,
  optional verification (`verify:oidc`), and task reconciliation. Product
  accepted honest contract-unavailable P0 destinations 2026-08-29; host HTTP for
  Session/Review/Release deferred to later backend work.
- [x] Polish My Work empty: Component Deck `empty-plate--inset` inside one
  operate well; reject nested chamfered EmptyPlate inside flush EtchedFrame.

## Phase 7 — Corrective page-by-page visual rebuild from design-lab donors

- [x] Freeze the corrective visual baseline and adoption matrix. Capture the
  current production pages and their matched design-lab donors at identical
  desktop and narrow viewports. Record adopt/adapt/reject decisions before
  editing a page.
- [x] Run Impeccable context once in each execution session and use bounded
  `$impeccable shape` briefs for each materially reconstructed wave. Promote any
  durable journey or visual decision into the owning approved UI/UX or
  design-system artifact before treating it as authority.
- [x] Wave 7.1 — Rebuild shared production composition foundations: authenticated
  and unauthenticated chrome, capability-aware navigation, page-heading/focal
  hierarchy, work-plane rhythms, responsive rail/drawer behavior, intentional
  empty/loading/error/denied compositions, and light/dark parity. Delete weak
  shell/page-local composition when cleaner than adapting it.
- [x] Wave 7.2 — Rebuild Participant surfaces from scratch where useful:
  Home/My Work roster, empty My Work, populated assignment plates, assignment
  detail, Submission intake, error/retry, conflict, pending, cancellation, and
  narrow states. Use participant Home/Journey lab surfaces as visual donors;
  keep real enrollment/submission contracts authoritative. Authenticated
  Playwright of populated/empty My Work remains API-blocked.
- [x] Wave 7.3 — Rebuild Administrator surfaces from scratch where useful:
  Activities registry, Campaign setup, Participants/Enrollment, Enrollment
  detail, accommodation state, activation/readiness, validation, large-table,
  empty, pending, conflict, denied, and narrow states. Use Admin Campaign and
  Enrollment lab surfaces as visual donors; reject fixture mutations and
  unapproved lab actions. Authenticated Playwright of production admin pages
  remains API-blocked.
- [x] Wave 7.4 — Rebuild honest unavailable destinations and access ceremonies:
  Sign in, permission denial, not found/non-disclosing, and contract-unavailable
  Attempt/Session/Review/Release/Result states. Use Component Deck plates,
  readouts, wait/advisory patterns, and relevant lab shell compositions without
  exposing controls for missing contracts or inventing data. Authenticated
  unknown/denied/contract-unavailable screenshots remain OIDC-blocked; sign-in
  and lab unknown-channel donors were captured. Attempt start stays on
  assignment intake (no standalone Attempt route).
- [x] For every wave, follow `frontend-developer` red-green-refactor: preserve or
  add accessible user-observable tests first, run the intended failure, rebuild
  the minimum real behavior, then refactor shared state/composition while green.
- [x] Promote only production-safe, generic improvements into
  `web/src/design-system/**` and `web/src/styles/shared.css`. Production must not
  import design-lab modules, fixtures, demo state, surface CSS, or `/design-lab`
  routes.
- [x] After each wave is behavior-green, run bounded `$impeccable harden`,
  `$impeccable adapt`, and `$impeccable polish` passes. Capture Playwright MCP
  accessibility snapshots plus desktop/narrow screenshots in one batch, fix all
  observed hierarchy/copy/spacing/alignment/overflow/focus/contrast/polish
  defects, and perform at most one confirmation batch.
- [x] Obtain independent frontend-reviewer visual acceptance against side-by-
  side design-lab donors and present the consolidated evidence for owner final
  acceptance. Do not pause between waves unless a material authority conflict
  appears. Sharing tokens or shell components is not sufficient; the production
  state must demonstrate comparable composition, hierarchy, density,
  intentionality, and finish. Reviewer accept (2026-08-28, in-thread
  `frontend-reviewer` pass): available authenticated admin Home, Activities
  (empty/populated/narrow Activation), Setup, Enrollment (populated, narrow
  Participant+Record), Access denied `/my-work` `/results`, authenticated
  unknown locator, and hug sign-in ceremony. Owner final visual sign-off is
  still required. Participant My Work live-accepted 2026-08-28 after SSO logout
  fix (see Verification). Session/Review/Release accepted as contract-unavailable
  2026-08-29.

### Phase 7 exit gate

Every **in-scope** available production page and every honest unavailable/access
state has passed behavior tests, accessibility snapshots, desktop/narrow
screenshot evaluation, design-lab donor comparison, and frontend-reviewer
acceptance. Contract-unavailable P0 destinations are Product-accepted interim
behavior. Owner visual sign-off is the remaining gate. No production import
reaches lab code or synthetic behavior.

## Phase 8 — Final audit, verification, and completion

- [x] Run `$impeccable audit` as an independent bounded review after Phase 7,
  then run the mechanical detector once over all changed UI targets. Resolve all
  blocking accessibility, responsive, performance, and craft findings.
- [x] Run focused frontend checks followed by `pnpm verify:web`, design-lab E2E,
  applicable authenticated production Playwright journeys, `verify:dotnet`,
  supply-chain, and OCI verification (see Verification table).
- [!] `pnpm verify:oidc` not run (port collision with live compose).
- [x] Reconfirm production/design-lab bundle isolation, protected-content/cache
  cleanup, authorization failures, and the absence of synthetic or deferred
  behavior in production artifacts.
- [-] Complete independent Product/UI/UX, Architecture, frontend, backend-
  contract, security/privacy, tester, and release-readiness reviews.
  Skipped for solo execution unless owner requests them.
- [x] Reconcile host-contract blockers or record explicit Product acceptance of
  honest contract-unavailable destinations without misrepresenting them as
  implemented P0 journeys. **Accepted 2026-08-29 (owner):** Session, Review,
  Result, and Release remain `ContractUnavailablePage` / Access denied until host
  HTTP exists; implemented admin/participant surfaces ship as-is.
- [>] Reconcile actual changes with this task, update verification evidence and
  visual matrix, recheck governing specifications, and mark completed only when
  both functional and visual definitions of done are met.

# Visual rebuild surface matrix

| Production target | Primary visual donor | Required adoption | Behavior boundary | Wave 7 evidence |
| --- | --- | --- | --- | --- |
| `ProductionAppShell`, authenticated navigation | Design-lab `AdminPage` grouped gangway | Adopt grouped Workspace vs Outcomes; reject catalog/demo/synthetic footer | Real capability-filtered routes, logout, auth context | Unit: `ProductionAppShell.test.tsx` operator menu; drawer/bulkhead unchanged. Theme + sign-out in `ProfileMenu`; no strip readout. Desktop closed `.playwright-mcp/page-2026-08-29T04-22-57-221Z.png`; menu `.playwright-mcp/page-2026-08-29T04-23-16-609Z.png`; light `.playwright-mcp/page-2026-08-29T04-23-38-191Z.png`; light menu `.playwright-mcp/page-2026-08-29T04-24-02-546Z.png`; narrow closed `.playwright-mcp/page-2026-08-29T04-24-28-041Z.png`; narrow menu `.playwright-mcp/page-2026-08-29T04-26-34-285Z.png` |
| `ProductionAuthGatePage`, loading, denied, contract-unavailable | Component Deck command strip + etched plate; reject stretched empty well | Adopt hug-to-content ceremony plate, Shipboard operate-title, centered first viewport; reject MFA-as-auth | Real OIDC login only | Desktop light `.playwright-mcp/page-2026-08-28T08-26-21-753Z.png`; narrow `.playwright-mcp/page-2026-08-28T08-26-44-491Z.png`. Prior dark `.playwright-mcp/page-2026-08-28T07-18-55-484Z.png`. Loading WaitPanel. Workspace denied EmptyPlate + Continue to sign in |
| `ProductionHomePage` | Design-lab Participant `HomePage` bays | Adopt destination **assignment plates** on an explicit `frameInset="flush"` board; shared `plate-bays--hug` with My Work (board fills, plates top-align; reject Status Bays row-flex stretch) | Real My Work/Activities capability; no fixtures | Unit: `plate-bays--hug`. Participant Home desktop `.playwright-mcp/page-2026-08-29T04-36-26-495Z.png`; narrow `.playwright-mcp/page-2026-08-29T04-35-53-873Z.png`. Open My work from plate. Lab donor roster `.playwright-mcp/page-2026-08-28T15-50-40-416Z.png` |
| `ProductionMyWorkPage` | Design-lab Participant `HomePage` plates / `board-empty` | Adopt Campaign/Assignment/Deadline/Record readout plates with shared `plate-bays--hug` (1 or many); empty uses inset EmptyPlate + `assignment-board--hug` (not plate stretch). Reject dense Status Bays row-flex and shrinking the board for a single assignment | Real `listMyWork`; server status only | Unit: `plate-bays--hug` for 1 and 2 assignments; empty inset. Live empty desktop `.playwright-mcp/page-2026-08-29T04-36-52-472Z.png`; empty narrow `.playwright-mcp/page-2026-08-29T04-37-32-850Z.png`. Populated live not rechecked this pass (no current assignments). Lab donor `.playwright-mcp/page-2026-08-28T15-50-40-416Z.png` |
| `ProductionMyWorkDetailPage` | Design-lab `JourneyPage` | Adopt BackKey, identity/timing readout grid, stacked WorkWells (`live={false}`) for Submission/versions/Attempt; reject PhaseSpine, fixture IDs, demo beats | Real enrollment and Submission intake | Unit: Begin intake in operate-head (first viewport); Shipboard Choose files key. Live populated desktop `.playwright-mcp/page-2026-08-28T16-53-02-108Z.png`; narrow `.playwright-mcp/page-2026-08-28T16-53-34-216Z.png`. Lab donor desktop `.playwright-mcp/page-2026-08-28T07-47-45-225Z.png`; narrow `.playwright-mcp/page-2026-08-28T07-47-11-631Z.png` |
| `AssessmentActivitiesPage`, `ProductionActivitiesPage` | Design-lab `AdminPage` + `CampaignsArea`/`CampaignRegistry` | Adopt flush registry as the first work plane; create as a WorkWell below the table, revealed when the list is empty or via the toolbar key; reject OperateArea-context create wall, row-selection, bulk delete, lab Frozen label, `admin-console.css` | Real Activities API; activation **Activated/Draft**; missing sources stay an OperateArea advisory | Unit: registry-before-create + hug when ≤4 rows. Populated desktop `.playwright-mcp/page-2026-08-28T17-41-32-487Z.png`; earlier `.playwright-mcp/page-2026-08-28T15-43-17-126Z.png`; narrow Activation in first viewport `.playwright-mcp/page-2026-08-28T15-42-43-618Z.png`. Lab donor (dark, fixture density) `.playwright-mcp/page-2026-08-28T15-48-57-093Z.png` |
| `AssessmentSetupPage` | Campaign record / config wells (not dialog-as-page) | Adopt BackKey, identity readout grid, padded `record-frame`, Configuration WorkWell (`live={false}`); reject lab fixture mutations | Real draft/readiness/activation | Unit: `AssessmentSetupPage.test.tsx`. Authenticated draft `.playwright-mcp/page-2026-08-28T08-52-59-603Z.png` |
| `ProductionEnrollmentPage`, `ProductionEnrollmentDetailPage` | `EnrollmentsArea` | Adopt flush participant registry + Assign keys in toolbar, empty “No Participants assigned”; detail as assignment-station wells; reject lab stage/session/result columns, row selection, expanded fixture panes | Real enrollment/accommodation | Unit: hug when ≤4 rows. Production populated desktop `.playwright-mcp/page-2026-08-28T17-42-08-783Z.png`; earlier `.playwright-mcp/page-2026-08-28T15-52-18-797Z.png`; earlier narrow Participant+Record `.playwright-mcp/page-2026-08-28T15-41-57-538Z.png`. Lab donor `.playwright-mcp/page-2026-08-28T15-49-48-072Z.png` |
| `ContractUnavailablePage` and access denied | Component Deck EmptyPlate; reject fake Start/Session controls | Adopt hug ceremony + inset EmptyPlate (no nested card, no Unavailable kicker); named titles only when capability-authorized | Never imply missing contracts exist | Unit: `ContractUnavailablePage.test.tsx`. Access denied `/my-work` `.playwright-mcp/page-2026-08-28T15-45-04-894Z.png`; `/results` `.playwright-mcp/page-2026-08-28T15-45-43-445Z.png`. Session document load (SPA, not nginx 404) desktop `.playwright-mcp/page-2026-08-28T17-42-27-479Z.png`; administrator relationship deny |
| `UnknownDestinationPage` | Lab `NotFoundPage` EmptyPlate; reject stretched board-empty and splat-to-Home | Adopt hug ceremony + inset EmptyPlate; hide breadcrumbs; do not echo the locator | Non-disclosing unknown path | Unit: `App.test.tsx`. Authenticated production `.playwright-mcp/page-2026-08-28T15-46-22-289Z.png`. Lab channel-not-found `.playwright-mcp/page-2026-08-28T15-51-40-629Z.png` |

For each row, execution must expand the matrix with route/state, applicable
design-system modules, accepted/rejected donor elements, test IDs, and final
Playwright evidence paths. The matrix remains in this task file; do not create a
second progress document.

# Workstream readiness matrix

| Workstream | Input gate | Output gate | Primary verification |
| --- | --- | --- | --- |
| Task handoff | Owner activation | One truthful execution cursor | `.work/active` review |
| Architecture reset | Confirmed reset intent | Approved successor ADR | Docs check + independent review |
| Authority retirement | Approved Product/UI/UX retirement decision | No live stale UX authority | Approval metadata + link/status/reference checks |
| Code removal | Frozen exact manifest | No prod pages or `web-legacy` | Import/workspace/build scans |
| Fail-closed interval | Approved transition contract | Lab green; production publish denied | Negative build/deploy tests |
| UX reconstruction | Approved P0 requirements | Approved canonical journeys/IA | Traceability + role reviews |
| Frontend readiness | Approved UX/ADR | Route/API/state/test matrix | Contract and architecture review |
| Slice implementation | Approved mapped slice | Tested accessible vertical behavior | TDD + Playwright MCP evidence |
| Visual reconstruction | Owner-approved design-lab donor direction | Page-by-page specimen-level production quality | Adoption matrix + side-by-side Playwright evidence + Impeccable bounded passes |
| Production restore | Functional and visual gates pass | One deployable, visually accepted production SPA | Full regression/OCI/OIDC checks |

# Current state

Owner-activated 2026-08-28. Execution is in **Phase 8 closeout**. Engineering for
implemented production surfaces is on `main` (`bb663cd`, pushed 2026-08-29).
Local working tree also has an uncommitted outer-hug flex fix
(`app-shell.css`, `ProductionMyWorkPage.tsx`) verified by
`pnpm verify:web:production` 2026-08-29.
Predecessor `impeccable-frontend-rebuild` remains `blocked` (owner-superseded).

**Product (2026-08-29):** honest contract-unavailable P0 destinations are
accepted. Session / Review / Result / Release stay ceremony pages until host
contracts ship later; the SPA must not invent those journeys.

**Owner visual pass:** in progress (owner, 2026-08-29). Skim implemented
surfaces on Vite `:5274` or after compose rebuild; record pass/fail below.

Status remains `in-progress` until owner visual sign-off is recorded and the
task is reconciled. `verify:oidc` not run (canonical profile binds `:18080`,
occupied by live compose). `pnpm verify:supply-chain` and `pnpm verify:oci`
passed 2026-08-29. Independent formal reviews skipped (solo project).

SSO logout no longer rebinds the previous Keycloak user. Sign out returns
directly to **Sign in required** (no Keycloak `#kc-logout` confirm) when the API
includes `id_token_hint`; `demo.participant` can sign in without inheriting the
administrator session. See `sso-logout-id-token-hint` task evidence
(2026-08-29).

Current cursor: owner visual pass. Operator chrome is one shared pattern:
authenticated `ProfileMenu` (theme + sign-out); unauthenticated/no-identity
strips keep `ThemeToggle`. Compose `:18080` SPA may lag Vite.

Next after visual pass:

1. Record owner visual acceptance (or file defects) in this file.
2. Rebuild compose so `:18080` serves `bb663cd`.
3. Optional: `pnpm verify:oidc` on a non-colliding stack.
4. Commit uncommitted hug fix + task reconciliation when ready.
5. Defer backend verticals (Attempt start, Session command/snapshot, Review,
   Result, Release) to a follow-on task.

Adopted from lab: grouped gangway sections, flush work bays via **inset prop**,
hug ceremony plates, Shipboard operate-head type, enrollment-style readout
plates, Journey well stacking without phase mutation, Campaign Registry /
Enrollment Manifest table density, EmptyPlate instrument for unavailable.
Rejected: demo footer, catalog home, fixture campaigns, Open/Live/Released
**record bays**, stretched empty etched wells, lab `data-surface` CSS in the
production graph, `resolveFrameInset` class-name registry, splat-to-Home.

Implemented production surfaces: auth gate, Home, Activities/setup, Participants
and Enrollment (lifecycle + bounded accommodation request), My Work list and
assignment (Submission intake). Session/Review/Release/Result routes render
`ContractUnavailablePage` when the destination is authorized.

Vite now proxies `/v2` and `/sessions` in addition to `/auth` and `/v1`.
Session locators are reachable when My work is available. Enrollment can
request a fairness-exception flag and approve/reject/revoke accommodations.

Keep/remove/move/amend (tracked paths):

| Path | Action | Rationale |
| --- | --- | --- |
| Six P0 UI/UX specs at `docs/ui-ux/{activity-campaign-journey,assessment-campaign-setup,submission-attempt,text-session,evidence-evaluation-human-review,result-release}.md` | amend in place after ledger | Last retired blobs at `eb9c398`; same filenames remain the canonical successor locations |
| `docs/ui-ux/retired-authority.md` | add | Retirement ledger |
| `docs/architecture/decisions/ADR-021-*.md` | add | Single-SPA + fail-closed topology |
| `docs/ui-ux/design-system/**` | amend links only | Visual language unchanged |
| `web/src/pages/**` | remove then rebuild | Production page composition |
| `web/src/router/production-*.ts(x)` | remove then rebuild | Production route graph |
| `web/src/router/route-leaves.ts`, `route-layout-governance.ts`, `route-layout-match.ts` | retain | Design-lab tests own these utilities |
| `web/src/components/shell/**`, `ErrorBoundary` | retain shell chrome patterns; rebuild route wiring | Production-safe chrome + ADR-019 shell |
| `web/src/api/**`, `web/src/features/**`, `web/src/lib/**`, `web/src/hooks/**` | retain | Production-safe infrastructure |
| `web-legacy/src/contracts/**` | move → `web/src/contracts/**` | Typed contract projection |
| `web-legacy/src/api/production-enrollment.ts`, `production-submission.ts` (+ tests) | move → `web/src/api/` | Production-safe clients absent from candidate tree |
| `web-legacy/src/lib/campaign-timezone.ts` (+ test) | move → `web/src/lib/` | Shared timing display helper |
| Remaining `web-legacy/**` (104 tracked files) | remove | Dual-SPA runtime retired |
| Root `package.json`, `pnpm-workspace.yaml`, lockfile, Docker, SBOM, verify scripts, architecture tests | amend | Single `@flex-agent/web` production pointer |

# Decisions

- Treat this as a true authority and implementation reset, not a continuation,
  restyle, migration, or adaptation of existing production pages.
- Preserve retired UX text through Git history plus a small retirement ledger,
  not through a second archive of readable specifications.
- Retain the approved Shipboard design system and design lab; neither authorizes
  production capability or behavior.
- Treat the design lab as the primary visual/composition donor and current
  production pages as disposable anti-reference until they pass comparison.
- This is a visual redesign, not a same-shell technical migration. A page may be
  deleted and rebuilt from scratch; only product truth, real content/function,
  approved journeys, accessibility, server boundaries, and valid tests must be
  preserved.
- Limit the production rebuild to approved P0 scope.
- Preserve production-safe frontend infrastructure by evidence; delete only
  page/shell/route composition and proven coupled code.
- Accept an intentional no-production-frontend interval and make it fail closed
  rather than deploying the design lab, legacy SPA, empty shell, or placeholder.
- Keep UI/UX retirement approval under Product/UI/UX authority and technical
  topology/fail-closed approval under Architecture authority; require both
  before their respective destructive changes.
- Approve the replacement IA and journey set before rebuilding any production
  page.
- Build one canonical production SPA in `web/`; do not recreate `web-legacy/`
  or another parallel candidate/production tree.
- Use vertical slices after holistic UX approval so each implemented journey is
  contract-backed and independently verifiable.
- Keep voice and the proposed Interaction Controller outside the P0 rebuild
  unless a separate approved scope decision explicitly replans this task.
- Compose `frontend-developer` TDD with user-authorized bounded Impeccable
  `shape` → implementation → `harden`/`adapt`/`polish` → `audit`. Do not enable
  hooks, live mode, or open-ended polish loops.
- Interim default (2026-08-29, owner-agreed): authenticated operator-self
  actions (theme, sign-out) live in the command-strip `ProfileMenu`. The strip
  does not duplicate those keys or the Organization readout. Unauthenticated
  ceremony chrome may keep `ThemeToggle` because there is no operator menu.

# Findings / deviations

- ADR-020 currently assumes a dual-build transition with `web-legacy/` serving
  production until cutover. The confirmed reset invalidates that strategy and
  therefore requires an approved successor ADR before deletion.
- Removing current `web/` production composition and all of `web-legacy/`
  creates a deliberate interval with no production SPA. Existing root build,
  workspace, CI, OCI, SBOM, compose, and change-detection paths cannot be left
  pointing at either deleted code or the design lab.
- The approved design-system README and implementation guide currently link to
  the former P0 UI/UX specs. Their status/link metadata must change atomically
  with retirement while their approved visual rules remain intact.
- Some files under `web/src/router/**`, such as route-leaf/layout governance
  utilities, are used by design-lab tests. Directory-wide deletion would damage
  the preserved lab and is prohibited until the import graph classifies them.
- API/auth/query/form/error infrastructure appears separable from current pages
  and should be retained by default, but the Phase 0 manifest must prove the
  boundary.
- The existing `impeccable-frontend-rebuild` task had reached Phase 8 and still
  described `web-legacy/` as production. It is historical evidence, not the
  implementation plan for this reset.
- The planned `text-interaction-controller-contract` task named the retiring
  `docs/ui-ux/text-session.md` as governing UI authority. It now carries an
  explicit reset dependency and cannot activate against that stale source.
- The 2026-08-28 readiness review found and resolved three planning defects:
  UI retirement approval is now separated from ADR authority; deferred voice
  and Interaction Controller behavior is fenced from P0; and readiness evidence
  is reconciled only after the corrected plan passes re-review.
- Production host HTTP does not yet expose Attempt start, Session commands/
  snapshot GET, Review, Result, or Release. The SPA records that in
  `docs/architecture/frontend-architecture.md` and uses contract-unavailable
  pages instead of inventing outcomes.
- The prior agent explanation that production should look the same because it
  shares Shipboard chrome is rejected by the owner. Shared design-system
  identity is necessary but insufficient; design-lab sample surfaces set the
  required composition and craft level.
- An empty My Work response may be correct server truth, but correctness does
  not excuse an unfinished empty-state composition. Empty, loading, denied, and
  unavailable states require the same intentional hierarchy and polish as
  populated states without fabricating records or actions.

- Host Session command/snapshot HTTP is not mapped; only `GET /sessions/{id}/events`
  exists on the production API. Review/Result/Release have no production HTTP
  group. Attempt start is not mapped on the host (synthetic browser only).
- Production shell still assigns `management` to `/sessions/:sessionId` because
  there is no contract-backed live-session page.
- Independent Product/UI/UX/Architecture/Security/tester review artifacts were
  not produced in this execution.
- Home destination wells use `WorkWell`, which always sets `aria-live="polite"`.
  Resolved in Wave 7.2: Home uses `AssignmentPlate` (no live region). `WorkWell`
  now accepts `live={false}` for stacked assignment-station wells.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `python3 scripts/check_docs.py` | passed | 2026-08-28 after ADR-021, retirement ledger, and frontend architecture contract matrix |
| `pnpm --filter @flex-agent/web` lint/typecheck/test | passed | 208 tests after Phase 7 exit review fixes; typecheck green |
| Wave 7.1 consistency review | passed with residual gaps | Fixed `/results` splat bounce; ceremony frame specificity; light-theme operate-title. Sign-in dark `.playwright-mcp/page-2026-08-28T07-25-18-171Z.png`; light `.playwright-mcp/page-2026-08-28T07-25-43-685Z.png`. Lab registry still intact `.playwright-mcp/page-2026-08-28T07-26-13-000Z.png`. Authenticated production pages still API-blocked. |
| Wave 7.2 unit tests | passed | Home/My Work plates, empty board, assignment BackKey + flush `frameInset`, intake still green |
| Wave 7.2 Playwright MCP | partial | Lab Home desktop `.playwright-mcp/page-2026-08-28T07-46-33-192Z.png`; lab Home narrow `.playwright-mcp/page-2026-08-28T07-49-17-712Z.png`; lab Journey desktop `.playwright-mcp/page-2026-08-28T07-47-45-225Z.png`; lab Journey narrow `.playwright-mcp/page-2026-08-28T07-47-11-631Z.png`. Production still sign-in (light) `.playwright-mcp/page-2026-08-28T07-48-41-443Z.png`. Populated My Work not reachable (session API down). |
| Wave 7.2 Impeccable detector | passed | `detect.mjs --json` on Home/My Work/Detail/`AssignmentPlate`/`app-shell.css` → `[]` |
| Wave 7.3 unit tests | passed | Activities flush registry, Setup BackKey/Configuration, Enrollment flush table + empty plate, Enrollment detail Participants BackKey; 203 `@flex-agent/web` tests; typecheck green |
| Wave 7.3 Playwright MCP | partial | Lab Campaign Registry desktop `.playwright-mcp/page-2026-08-28T08-02-17-825Z.png`; narrow `.playwright-mcp/page-2026-08-28T08-03-49-307Z.png`. Lab Enrollment Manifest desktop `.playwright-mcp/page-2026-08-28T08-02-44-128Z.png`; narrow `.playwright-mcp/page-2026-08-28T08-03-10-099Z.png`. Production `/activities` still sign-in `.playwright-mcp/page-2026-08-28T08-04-36-879Z.png` (session API down). |
| Wave 7.3 Impeccable detector | passed | `detect.mjs --json` on Activities/Setup/Enrollment pages + `app-shell.css` → `[]` |
| Wave 7.4 unit tests | passed | Unknown locator ceremony (no Home splat); Results contract-unavailable without Start/Session; access-changed Continue to sign in; 205 `@flex-agent/web` tests |
| Wave 7.4 Playwright MCP | partial | Sign-in desktop light `.playwright-mcp/page-2026-08-28T08-26-21-753Z.png`; narrow `.playwright-mcp/page-2026-08-28T08-26-44-491Z.png`. Lab unknown-channel desktop `.playwright-mcp/page-2026-08-28T08-27-08-993Z.png`; narrow `.playwright-mcp/page-2026-08-28T08-27-41-247Z.png`. Authenticated unknown/denied/contract pages OIDC-blocked |
| Wave 7.4 Impeccable detector | passed | `detect.mjs --json` on ceremony pages/shell/routes/`app-shell.css` → `[]` |
| Phase 7 exit review-fix unit tests | passed | Unknown path has no breadcrumb or locator echo (`App.test.tsx`, `Breadcrumbs.test.tsx`); Enrollment keeps the table when options fail and refreshes candidates after assign; assign success survives options-refresh failure (`ProductionEnrollmentPage.test.tsx`); contract-unavailable has no “Unavailable” kicker; Home plates omit a duplicate Destination row |
| Phase 7 exit consistency review | passed with residual gaps | Lint/typecheck green; `@flex-agent/web` tests green. Unauthenticated `/` and `/not-a-destination` both Sign in required with no crumbs or locator echo: Home desktop `.playwright-mcp/page-2026-08-28T08-43-34-368Z.png`; unknown snapshot `.playwright-mcp/page-2026-08-28T08-44-00-645Z.yml`. Authenticated inset-ceremony and Enrollment still OIDC-blocked |
| Phase 7 exit Playwright MCP | partial | Unauthenticated `/not-a-destination` remains Sign in required only (snapshot `.playwright-mcp/page-2026-08-28T08-38-02-700Z.yml`; desktop `.playwright-mcp/page-2026-08-28T08-37-26-899Z.png`; narrow `.playwright-mcp/page-2026-08-28T08-38-06-410Z.png`). Authenticated ceremony and enrollment still OIDC-blocked |
| Phase 7 exit Impeccable detector | passed | `detect.mjs --json` on ceremony/enrollment/Home/My Work/shell/routes/`EtchedFrame`/`app-shell.css` → `[]`; isolation check passed |
| Phase 7 polish Activities first-viewport | passed | Red: registry-before-create failed while create was OperateArea context. Green: flush registry first; empty list hugs then shows create; populated list discloses create from the toolbar. 15 Activities tests; typecheck; isolation; detector `[]` |
| Phase 7 consistency review (post-polish) | passed | Duplicate Create keys after reveal removed; focus moves to `#create-heading` via layout effect; error-summary heading takes focus (`shouldFocusError: false`). Narrow registries use `datatable-table--fit` so Activation / Record stay in the first viewport. Enrollment placeholder `SEARCH NAME` (label still searches status). Activities 15 + Enrollment tests green |
| My Work intake TDD | passed | Red: missing Begin intake; green: `ProductionMyWorkDetailPage.test.tsx` |
| Owner visual acceptance | failed / reopened | 2026-08-28: first-pass pages insufficient vs design-lab. Wave 7.1 addresses chrome/ceremony; remaining surfaces still need donor comparison. |
| Design-lab unit tests | passed | 98 tests |
| Frontend isolation + candidate bundle | passed | `check-frontend-isolation.mjs`, `check-candidate-bundle.mjs` |
| Architecture `FrontendRebuildIsolationTests` | passed | 18 tests after Dockerfile comment restriction |
| `pnpm verify:web:production` | passed | 2026-08-29: lint, typecheck, production tests, isolation, build (`index-BVw3HiE4.js`). Includes uncommitted outer-hug fix. |
| Outer registry/assignment hug (workspace-area flex) | passed (uncommitted) | 2026-08-29: `.workspace-area.registry-wall--hug` and `.assignment-board--hug` stop flex-grow; `ProductionMyWorkPage` assigns hug class. Tests green; detector `[]`. |
| Empty-in-frame consistency | passed | 2026-08-29: `EmptyPlate inset` for operate wells, ceremony, and datatable empties. Production Activities empty `.playwright-mcp/page-2026-08-29T04-43-22-699Z.png`. Lab Home empty donor `.playwright-mcp/page-2026-08-29T04-42-04-258Z.png`. Detector `[]`. |
| Activities table-action strip (lab `datatable-actions`) | passed | 2026-08-29: Create is `DataTableToolbar` `actions` (`datatable-actions` + `KeyGroup datatable-actions-keys`), 12px strip-to-row gap, one Create link, empty plate has no key. Enrollment Assign keys match. Unit: Activities + Enrollment + KeyGroup. Desktop `.playwright-mcp/page-2026-08-29T05-03-47-750Z.png`; Create → `/activities/new`; narrow `.playwright-mcp/page-2026-08-29T05-04-16-304Z.png`. |
| Table empty plate vs lab Datatable | passed | 2026-08-29: removed `.registry-wall` flex-centering of empty scroll. Production empty is inset `datatable-empty`: 18px gutter, 20px top pad, left `align-items: start`, scroll `display: block`. True empty has no plate key; search-empty seats compact **Clear search** (lab **Clear filters**). Lab donor `.playwright-mcp/element-2026-08-29T05-07-16-902Z.png`. Production empty `.playwright-mcp/page-2026-08-29T05-10-15-312Z.png`. |
| Create well reserved foot | passed | 2026-08-29: transmit **Create** sits in `WorkWell` `foot` (same reserved-key pattern as Setup). Page title/breadcrumb stay **Create assessment Campaign**. Sources scroll in `.work-well__body`; first viewport keeps the key. Desktop `.playwright-mcp/page-2026-08-29T05-50-10-415Z.png`. Narrow `.playwright-mcp/page-2026-08-29T05-50-35-850Z.png`. |
| Compact Create key copy | passed | 2026-08-29: Activities toolbar and create-foot keys are **Create**; page title/breadcrumb stay **Create assessment Campaign**. Create well accessible name is **Draft**. Home/My work plate keys are visible **Open** with `Open {object}` accessible names. Unit: Activities, Create, Home, My work, App. Detector `[]`. Desktop Home `.playwright-mcp/page-2026-08-29T06-02-32-703Z.png`. Narrow Home `.playwright-mcp/page-2026-08-29T06-03-45-941Z.png`. |
| Nested plate-grid bezels | passed | 2026-08-29: Home, populated My work, and lab Status Bays omit etched wells around plates. Empty indexes keep the well. Live: production Home 0 frames `.playwright-mcp/page-2026-08-29T05-49-35-642Z.png`; Activities registry well `.playwright-mcp/page-2026-08-29T05-50-03-301Z.png`; Create record-frame `.playwright-mcp/page-2026-08-29T05-50-25-633Z.png`; lab Status Bays desktop `.playwright-mcp/page-2026-08-29T05-52-32-815Z.png`; empty `.playwright-mcp/page-2026-08-29T05-53-06-848Z.png`. |
| Corrective visual-plan consistency | passed | Reconciled active repository state, owner screenshots/feedback, approved Shipboard authority, design-lab donor boundary, page deletion authorization, frontend-developer TDD, bounded Impeccable workflow, and one current Phase 7 cursor on 2026-08-28 |
| Impeccable planning setup | passed | Context loaded once for this session; `shape` playbook applied to the confirmed replacement-composition brief; no UI code edited in this plan-amendment turn |
| Wave 7.1 unit tests | passed | 205 `@flex-agent/web` tests including Results click → ceremony |
| Wave 7.1 Playwright MCP sign-in | passed | Desktop `.playwright-mcp/page-2026-08-28T07-18-55-484Z.png`; narrow `.playwright-mcp/page-2026-08-28T07-20-00-169Z.png`; snapshot `page-2026-08-28T07-19-56-657Z.yml`. Title, copy, and hug-to-content transmit key. Light theme not captured this pass. |
| Design-lab Campaign Registry donor | captured | Desktop `.playwright-mcp/page-2026-08-28T07-08-22-041Z.png` |
| Pre-7.1 production auth anti-reference | captured | `.playwright-mcp/page-2026-08-28T07-07-49-469Z.png` stretched empty well |
| Impeccable session setup | passed | `context.mjs` once; Wave 7.1 shape from owner-confirmed donor matrix; craft-floor loaded before UI edit; detector deferred until Phase 8 |
| `$impeccable audit` (Phase 8) | recorded | Health 17/20 Good. Detector `[]` on production pages/shell/plates/routes/`app-shell.css`/`datatable.css`. No P0. P2: roster deadline falls back to exact UTC ISO when zone conversion is unavailable; assignment detail Submission well below fold at 390px; single-plate My Work bay stays tall. See audit section below. |
| Phase 8 mechanical detector | passed | `detect.mjs --json` on production UI targets → `[]` |
| `pnpm verify:web` | passed | 2026-08-28: 210 `@flex-agent/web` tests; 98 design-lab tests; isolation; production+lab builds; 11/11 design-lab E2E. Digest lock updated for `datatable.css` (`datatable-table--fit`). Gallery brand E2E scoped to `href="/design-lab/surfaces"` (strict-mode clash with layout specimens). |
| Participant My Work Playwright MCP | passed | After Keycloak logout confirm: participant Home `.playwright-mcp/page-2026-08-28T16-48-39-844Z.png`; empty My Work desktop `.playwright-mcp/page-2026-08-28T16-47-47-872Z.png`; empty narrow `.playwright-mcp/page-2026-08-28T16-48-13-553Z.png`; populated desktop `.playwright-mcp/page-2026-08-28T16-52-13-716Z.png`; populated narrow `.playwright-mcp/page-2026-08-28T16-52-34-607Z.png`; assignment desktop `.playwright-mcp/page-2026-08-28T16-53-02-108Z.png`; assignment narrow `.playwright-mcp/page-2026-08-28T16-53-34-216Z.png`. Live compose SPA `http://localhost:18080`. |
| Design-lab Playwright E2E | passed | Included in `pnpm verify:web` (11 passed) |
| `pnpm verify:dotnet` | passed | 2026-08-28: restore, Release build, tests, publish to `artifacts/publish/{api,worker}` |
| Supply-chain / OCI / `verify:oidc` | supply-chain and OCI passed; oidc not run | 2026-08-29: `pnpm verify:supply-chain` exit 0 (SPA SBOM 568 components via `npm exec`). `pnpm verify:oci` exit 0. `verify:oidc` not run: canonical profile uses `:18080` occupied by live compose. |
| Authenticated OIDC (candidate Vite) | passed | Synthetic administrator via Keycloak to `http://localhost:5274/`. Requires candidate overlay RedirectUri + `VITE_DEV_API_PROXY=http://127.0.0.1:18080` (default `pnpm dev` proxies `:8080` and will not complete callback) |
| Authenticated Home / Setup / denied / unknown Playwright | passed with residuals | Home `.playwright-mcp/page-2026-08-28T08-51-20-427Z.png`; Activities empty `.playwright-mcp/page-2026-08-28T08-51-53-981Z.png`; Setup `.playwright-mcp/page-2026-08-28T08-52-59-603Z.png`; My work denied `.playwright-mcp/page-2026-08-28T08-53-34-029Z.png`; Results denied `.playwright-mcp/page-2026-08-28T08-53-59-838Z.png`; unknown `.playwright-mcp/page-2026-08-28T08-54-30-894Z.png`. Participant My Work / Enrollment live not captured this pass |
| Independent role reviews | frontend-reviewer done; others skipped | Solo project: formal Product/Architecture/Security/tester/release-readiness reviews not run unless owner requests. |
| Phase 7 independent frontend-reviewer | accepted with residuals closed | Available surfaces vs lab donors accepted 2026-08-28. Participant My Work live-captured 2026-08-28. Session/Review/Release Product-accepted 2026-08-29. Owner visual sign-off pending. |
| Consistency re-review (working + theme) | passed | 208 tests. Home→Activities→create reveal (unique submit, heading `[active]`)→Setup→Enrollment→detail→Return Home. Dark Setup `.playwright-mcp/page-2026-08-28T16-03-25-908Z.png`; unknown `.playwright-mcp/page-2026-08-28T16-04-14-666Z.png`; denied `.playwright-mcp/page-2026-08-28T16-04-52-876Z.png`. Fixed Setup title `field-input--wide` (was 108px clip). Cohorts crumb → setup; Cohort crumb → enrollments. Detector `[]`; isolation passed |
| Phase 7 review-fix Playwright MCP | passed | Activities populated desktop `.playwright-mcp/page-2026-08-28T15-43-17-126Z.png`; narrow `.playwright-mcp/page-2026-08-28T15-42-43-618Z.png`; Enrollment desktop `.playwright-mcp/page-2026-08-28T15-52-18-797Z.png`; Home `.playwright-mcp/page-2026-08-28T15-44-21-991Z.png`; My work denied `.playwright-mcp/page-2026-08-28T15-45-04-894Z.png`; Results denied `.playwright-mcp/page-2026-08-28T15-45-43-445Z.png`; unknown `.playwright-mcp/page-2026-08-28T15-46-22-289Z.png`. Lab campaigns `.playwright-mcp/page-2026-08-28T15-48-57-093Z.png`; enrollments `.playwright-mcp/page-2026-08-28T15-49-48-072Z.png`; participant home `.playwright-mcp/page-2026-08-28T15-50-40-416Z.png`; unknown `.playwright-mcp/page-2026-08-28T15-51-40-629Z.png` |
| Consistency re-review (working + identity + theme) | passed with residuals | 2026-08-28 in-thread `frontend-reviewer`. Focused unit 35/35. Live compose `:18080`: participant intake begin/cancel; SSO logout confirm → `demo.admin`; light-theme Home/My Work/Activities/Setup/Enrollment/unknown persist. High: full navigation to `/sessions/{id}` hits nginx API proxy and Chrome 404 instead of SPA ceremony `.playwright-mcp/page-2026-08-28T17-12-32-826Z.png`. Medium: native file picker on open intake `.playwright-mcp/page-2026-08-28T17-11-43-982Z.png`. Unknown hug (no crumbs) `.playwright-mcp/page-2026-08-28T17-18-58-299Z.png`. |
| Confirmation pass | passed | 2026-08-28: gallery unit+E2E green; administrator session still Activities-capable; populated registry `.playwright-mcp/page-2026-08-28T17-21-23-851Z.png`; `/my-work` Access denied `.playwright-mcp/page-2026-08-28T17-22-02-636Z.png`. Residual `/sessions/` nginx collision closed 2026-08-29. |
| Phase 8 P2 craft + gateway (no owner) | passed | `$impeccable clarify` UTC deadline copy; `$impeccable adapt` Begin intake in operate-head; `$impeccable layout` hug single assignment + ≤4-row registries; Shipboard Choose files key. Gateway `location ~ ^/sessions/[^/]+/events` + Vite bypass so document loads stay SPA. Detector `[]`. Session SPA deny desktop `.playwright-mcp/page-2026-08-28T17-42-27-479Z.png`. Activities hug `.playwright-mcp/page-2026-08-28T17-41-32-487Z.png`. Enrollment hug `.playwright-mcp/page-2026-08-28T17-42-08-783Z.png`. |
| Confirmation pass (review residuals) | passed | 2026-08-29: Enrollment exclusive end uses `campaignDeadlineCopy` (no raw ISO; UTC aliases skip false “conversion unavailable”). Ceremony locators hide breadcrumbs. Hug inner `.datatable-scroll` is `flex: 0 0 auto`; outer `.workspace-area.registry-wall--hug` / `.assignment-board--hug` no longer flex-grow (2026-08-29). Focused production tests green. Detector `[]`. Vite evidence in checklist section. Compose `:18080` document `/sessions/{id}` HTML 200; `/sessions/{id}/events` 401 unauthenticated. Compose SPA image still older than Vite until rebuild. |
| Product acceptance (contract-unavailable P0) | accepted | 2026-08-29 owner: Session/Review/Result/Release honest unavailable pages are acceptable for this slice; remaining host HTTP deferred. |
| Owner visual sign-off | in progress | 2026-08-29 owner performing pass on implemented surfaces (see checklist below). |
| Operator menu chrome distill | passed | 2026-08-29: shared `operatorAccountActions`; production + lab identity menus; strip `ThemeToggle` when there is no operator identity. 13 focused production tests; lab operator + digest. Detector `[]` on `ProductionAppShell.tsx`. Open strip `z-index: 90`. Lab Home menu `.playwright-mcp/page-2026-08-29T04-32-12-214Z.png`. |

# Phase 7 frontend-reviewer findings (2026-08-28)

In-thread `frontend-reviewer` pass (no dedicated review subagent). Live app on
`http://localhost:5274` with candidate OIDC; donors on `http://localhost:5275`.

**Verdict:** accept composition, hierarchy, density, and finish for available
production surfaces listed in Current cursor. Do not treat this as owner
acceptance or Phase 8 completion.

[High] Participant My Work populated/empty not live-accepted
- Status: **closed 2026-08-28** after participant journey verification (logout
  confirm was required before `id_token_hint`; superseded 2026-08-29). Live
  empty and populated My Work plus assignment detail captured as Participant
  (see Verification). Remaining: owner visual sign-off.

[High] Session / Review / Result / Release remain contract-unavailable
- Status: **accepted 2026-08-29 (Product/owner).** Honest `ContractUnavailablePage`
  and Access denied ceremonies are the approved interim P0 behavior until host
  HTTP ships. Not SPA visual debt; separate backend task.

[Polish] Single-row registries keep a tall well
- Status: **closed 2026-08-29**. `registry-wall--hug` when 1–4 rows; inner
  `.datatable-scroll` and outer `.workspace-area.registry-wall--hug` no longer
  flex-grow. `assignment-bays--hug` and `.assignment-board--hug` when one
  assignment.

[Polish] Roster / Enrollment deadline falls back to raw UTC ISO
- Status: **closed 2026-08-29**. `campaignDeadlineCopy` on My Work and Enrollment exclusive end. UTC aliases format without a conversion-unavailable apology.

[Polish] Access denied and Session ceremony still showed breadcrumbs
- Status: **closed 2026-08-29**. `shouldHideProductionBreadcrumbs` hides crumbs for unknown, denied workspace, and Session/Review/Release/Results locators.

[Polish] Begin intake below first narrow viewport
- Status: **closed 2026-08-29**. Begin intake is `OperateHead` `headExtra`.

[Polish] Native file picker on open intake
- Status: **closed 2026-08-29** for the Shipboard control (Choose files key + hidden input). The OS file dialog remains the platform picker.

Closed in this pass (were residuals): duplicate Create keys; error-summary
focus; narrow table `min-width: 680px` hiding Activation/Record; Enrollment
search clipping `STATUS`; Setup Campaign title clipped at 108px (now
`field-input--wide`); Cohorts/Cohort crumbs pointing at locators with no page.

# Blockers

- Attempt start, Text Session command/snapshot HTTP, Review, Result, and Release
  host APIs are missing. **Product accepted** honest contract-unavailable pages
  for this UX slice (2026-08-29). Separate backend work remains required for
  slices 4–6 and Start Attempt when the owner picks that up.
- Candidate OIDC against Vite `:5274` works when the API RedirectUri is
  `http://localhost:5274/auth/callback` and Vite proxies `/auth` to
  `http://127.0.0.1:18080`. The previous `authn.invalid_provider_response` was
  the canonical `:18080` RedirectUri plus Vite defaulting to `:8080`. Logout from
  Vite still posts `post_logout_redirect_uri` for `:18080` when the API public
  origin is the compose SPA; identity switch was verified on `:18080`.
- Document loads of `/sessions/{id}` now reach the SPA. The gateway proxies only
  `GET /sessions/{id}/events` to the API. Live nginx was reloaded 2026-08-29.
  Vite document navigations under `/sessions` bypass to `index.html`.
- These backend gaps no longer block **closing this UX reset task** after owner
  visual sign-off. They block claiming Session/Review/Release as implemented
  journeys only.

# Owner visual pass checklist (2026-08-29)

Use Vite `http://localhost:5274` with `VITE_DEV_API_PROXY=http://127.0.0.1:18080`
or compose `:18080` after rebuild. Synthetic accounts: `demo.admin`,
`demo.participant`.

| Surface | Route / actor | Pass |
| --- | --- | --- |
| Sign in / sign out | unauthenticated → OIDC | [ ] |
| Home | `/` as admin and participant | [ ] |
| Activities registry | `/activities` admin | [ ] |
| Setup | activity setup | [ ] |
| Enrollment roster + detail | cohort enrollments | [ ] |
| My Work empty / populated | `/my-work` participant | [ ] |
| Assignment intake | assignment detail, Begin intake | [ ] |
| Access denied | `/my-work` as admin | [ ] |
| Contract unavailable | `/results`, `/review` when authorized | [ ] |
| Unknown locator | `/not-a-destination` | [ ] |
| Session locator ceremony | `/sessions/{id}` (SPA, not 404) | [ ] |
| Light theme | toggle on one admin + one participant page | [ ] |
| Narrow viewport | repeat key surfaces ~390px | [ ] |

Record outcome here when done: **pass** / **pass with notes** / **fail** (list defects).

# Phase 8 `$impeccable audit` (2026-08-28)

In-thread pass after Phase 7; detector once; no unbounded polish loop.

### Audit Health Score

| # | Dimension | Score | Key Finding |
|---|-----------|-------|-------------|
| 1 | Accessibility | 3 | Landmarks, skip link, labeled keys; deadline conversion failure is now a UTC sentence |
| 2 | Performance | 3 | No layout-thrash or `will-change` abuse in production pages |
| 3 | Responsive Design | 3 | Narrow MENU/drawer works; Begin intake is in the operate-head first viewport |
| 4 | Theming | 4 | Shipboard tokens and light/dark strip toggle |
| 5 | Implementation Integrity | 4 | Detector `[]`; no production import of design-lab |
| **Total** | | **17/20** | **Good** |

Implementation Integrity: pass. Production pages express Shipboard operate
chrome, capability-filtered gangway, hug ceremonies, and flush registries
without lab fixtures.

No P0. P1 none on available surfaces. P2 craft items from the 2026-08-28 audit
are closed in the 2026-08-29 pass (UTC deadline copy, Begin intake in the
operate-head, inner and outer hug for registries/assignments, Shipboard Choose
files key).

# Open questions

**Resolved 2026-08-29 (owner):** P0 SPA may ship with honest contract-unavailable
Session/Review/Result/Release destinations until host HTTP exists. Implemented
admin/participant surfaces are in scope for this task; backend verticals are a
follow-on.

# Completion

**Status (2026-08-29):** `in-progress`. Engineering and verification for
implemented surfaces are complete pending owner visual sign-off. Product
accepted contract-unavailable P0 destinations. Only owner visual pass and task
reconciliation remain before `status: completed`.

- [x] Remaining gaps or unverified behavior are recorded
- [x] Former UI/UX authority is retired without stale current links
- [x] `web-legacy/` is removed; `web/` is the only production SPA package
- [x] Replacement P0 IA/journeys exist as Approved v1.0 documents
- [x] Design-lab isolation for the restored production artifact is verified
- [x] Every in-scope production page is rebuilt against a named design-lab/
  Component Deck donor (visual matrix above). Session/Review/Release/Result
  honest unavailable pages are Product-accepted interim behavior.
- [x] Empty/loading/error/denied/unavailable states are intentional and do not
  fabricate production data or capability (engineering + Playwright evidence)
- [x] Bounded Impeccable shape/harden/adapt/polish/audit passes and final
  mechanical detection are recorded
- [x] Desktop/narrow accessibility snapshots and Playwright evidence exist for
  implemented surfaces (Verification table). Owner final visual sign-off pending.
- [x] Planned work reconciled with DoD for this UX slice. Slices 4–6 host HTTP
  and formal multi-role reviews explicitly deferred or skipped per owner
  2026-08-29.
- [>] Applicable integration/regression checks: `verify:web` (2026-08-28),
  `verify:web:production` (2026-08-29), `verify:dotnet`, supply-chain, and OCI
  passed; `verify:oidc` not run; compose SPA image may lag until rebuild.
- [-] Formal accessibility, responsive, security/privacy, and release-readiness
  reviews skipped (solo project).
- [ ] Owner visual sign-off recorded (checklist below).
- [ ] Task state set to `completed` after visual pass and final reconciliation.
