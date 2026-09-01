---
id: repository-baseline-reset
status: in-progress
created: 2026-08-29
updated: 2026-09-01
activation_gate: owner-activated-with-delegated-intermediate-review
---

# Goal

Consolidate Flex Agent into one coherent current-state repository baseline before
new feature delivery resumes:

> Specs own current truth. Git owns history.

The precise post-reset rule is:

> Canonical specifications own current intended truth. Code and verified tests
> describe implemented truth. Active work describes approved future change.
> Git owns repository history.

`docs/current-state.md` is a derived, non-normative status index across those
owners. It may summarize classifications and link to evidence, but it must not
restate or override intended behavior, implementation contracts, or planned
scope. A conflict means the index is stale and must be reconciled against the
owning source.

The completed reset must make the latest intended product, requirements,
architecture, UI/UX, design system, delivery process, implementation status,
and genuinely active/planned work discoverable without reconstructing
supersession chains, completed projects, migration diaries, or old production
UI behavior.

This is the implementation-ready plan for that reset. The owner activated direct
TDP execution on 2026-09-01 under the delegated intermediate-review rules below.
Do not proceed past any unmet Phase 0 prerequisite or delegated review checkpoint.

# Activation gates and prerequisites

1. `.work/active/shipboard-production-ux-reset.md` is completed with its
   verification, remaining host-contract disposition, owner visual sign-off,
   and an explicit owner-approved disposition for any skipped independent
   review recorded.
2. The owner's follow-up UI/UX review and polish is complete. The resulting
   Design System, application UX, representative reference flows, and Design
   Lab specimens have explicit approved-current status in their owning sources.
3. Every non-reset execution cursor is completed, paused, or superseded with
   its durable truth reconciled. This currently includes
   `create-assessment-campaign-commission.md`; normalize its nonstandard
   `in_progress` status before classification.
4. Concurrent task/artifact cleanup is completed or reverted outside this
   reset. Reconcile every deletion since readiness reference `ea97a88` from Git
   so already-removed sources are not silently omitted from classification or
   treated as Gate-A-approved deletion.
5. The exact post-review Git reference is committed and pushed or otherwise
   explicitly accepted by the owner. The index and worktree are clean, except
   for this task's intentional activation edit, and no untracked artifact or
   concurrent task overlaps a reset target.
6. `python3 scripts/check_docs.py`,
   `python3 scripts/impeccable_context.py check`, and the focused adapter tests
   pass before any reset rewrite. This separates inherited baseline failures
   from reset regressions.
7. The owner explicitly activates this task. Then change `status` to
   `in-progress` and mark exactly one plan step `[>]`.
8. New feature implementation remains paused until this reset completes. A
   material product, security/privacy, or architecture ambiguity found during
   execution is resolved in its owning canonical source before dependent
   deletion proceeds.

## Owner activation and delegated intermediate review

On 2026-09-01, the owner explicitly activated this task for a direct TDP run and
authorized execution to proceed without pausing for separate human approval at
Gate A or Gate B. The owner will review the final result.

This delegation changes the approval actor, not the substance of either gate.
Gate A and Gate B remain mandatory execution checkpoints. Their complete evidence
packages must be independently reviewed by the TDP reviewer, every blocking finding
must be resolved, and the reviewer decision and evidence must be recorded below
before dependent work proceeds. The producer may not approve its own output. Any
material product, requirements, architecture, UI/UX, security/privacy, operational,
or scope ambiguity that the approved sources and recorded interim defaults do not
resolve remains a blocker requiring owner input.

The post-review UI/UX baseline is an input to this reset, not work to recreate
inside it. If the review changes the file set proposed below, update this plan's
consolidation map before activation while preserving the target authority
model.

# Governance transition

Current repository instructions remain binding during planning, Approval Gate
A, and the no-deletion consolidation phase. Gate A authorizes the migration
target and work; it does not itself change authority. The replacement model
becomes effective only when Phase 4 atomically approves the replacement
canonical sources, updates all applicable repository instructions, rules,
skills, templates, indexes, and validation, and passes the Phase 4 exit gate.

Do not resolve a semantic conflict by choosing the newest document, the most
frequent statement, or currently implemented behavior. Product intent,
implemented behavior, and approved planned behavior are separate dimensions.
Every material ambiguity must identify its decision owner, impact, interim
default, and rationale; consequential defaults remain `Proposed` until
approved.

No source may be deleted before its current constraints, invariants,
operational requirements, compatibility obligations, security evidence, and
necessary verification evidence are migrated and checked. Git may retain
repository narrative history after that migration, but it is not a substitute
for evidence still required to operate, secure, reproduce, or verify the
system.

# Governing sources

- `AGENTS.md`, `.cursor/rules/06-implementation-workflow.mdc`,
  `.agents/skills/implementation-workflow/SKILL.md`, and `.work/README.md` —
  current planning discipline; this task proposes a reviewed change to
  completed-work retention, not an ad hoc exception before approval
- `docs/README.md` — current authority-by-concern model and documentation index
- `docs/product/{overview,concept-model,mvp-scope}.md` — approved product
  meaning, vocabulary, scope, and invariants
- `docs/requirements/README.md`, `docs/requirements/mvp-operational-defaults.md`,
  and the seven approved P0 specifications under
  `docs/requirements/features/` — observable behavior and acceptance IDs
- `docs/architecture/**` — current technical baseline, focused execution
  contracts, and the ADR history to absorb before removal
- the owner-approved post-review `docs/ui-ux/**`,
  `web/src/design-system/**`, and `web/src/design-lab/**` baseline — UX/design
  contract, implementation, and reference evidence at activation time
- `docs/contributing/**`, `.agents/skills/**`, `.cursor/skills/**`,
  `.cursor/rules/**`, `.codex/config.toml`, `.cursor/mcp.json`, `.work/**`,
  `scripts/check_docs.py`, `scripts/impeccable_context.py`, and validation/build
  scripts — repository governance and enforcement surfaces
- actual implementation under `src/**`, `database/migrations/**`,
  `contracts/**`, `web/**`, `deploy/**`, `build/**`, and `tests/**` — evidence
  for implemented, legacy, and gap classification

# Current-state findings

## Authority and documentation shape

- Product authority is split cleanly by intent among `overview.md`,
  `concept-model.md`, and `mvp-scope.md`, but their status/version narratives,
  next-action lists, and ADR references make them partially historical.
- Seven approved P0 feature specifications contain the strongest stable
  `REQ-*`/`AC-*` behavior. They also contain large implementation-status
  matrices that duplicate and drift from code, tests, `docs/README.md`, and
  completed work items.
- Twelve P1-P3 feature files are placeholder scaffolds, not approved
  specifications. `scripts/check_docs.py` nevertheless requires all 19 files,
  both catalogs, tier ordering, and fixed tier counts, so empty future
  scaffolding is mechanically treated as permanent repository structure.
- Architecture authority is reconstructed from `mvp-architecture.md`, five
  focused architecture contracts/guides, 21 ADRs, and supersession chains.
  Current constraints such as authorization, audit, atomicity, deployment,
  stack, streaming, Worker delegation, frontend state, and single-SPA
  isolation are repeated across these sources.
- UI/UX authority currently uses six large P0 journey/surface specifications,
  a modular Design System, a retirement ledger, a Design System change record,
  Design Lab documentation, current production composition, and frontend
  architecture decisions. Design System v1.0 remains approved and was reviewed
  2026-08-31, and the Shipboard owner visual pass was accepted that day. The
  accepted UI baseline is now a reset input, but its completed uncommitted
  refactor must be reconciled into the clean activation freeze.
- `docs/README.md` is both an index and a long maturity/history narrative. It
  currently reports migrations only through `0043`, while the repository has
  migrations through `0062` plus the additive `0056a` migration.
- At least one current requirements implementation matrix still cites a
  `web-legacy` gateway journey even though the implementation and architecture
  assert that `web-legacy/` no longer exists. This is direct evidence that
  implementation status needs one current owner and code-backed validation.
- `PRODUCT.md` and `DESIGN.md` are generated non-authoritative Impeccable
  adapters. They should remain generated projections, with their source lists
  updated after consolidation.

## Historical-maintenance mechanisms

- `docs/architecture/decisions/` retains ADR context, alternatives,
  amendments, and supersession links as normal-development authority.
- `docs/ui-ux/retired-authority.md` and
  `docs/ui-ux/design-system/change-record.md` retain UI migration and change
  history in the live authority tree.
- `docs/operations/provider-profiles/qualified/openrouter/` contains seven
  phase-by-phase qualification narratives; the current profile also carries
  historical budgets/candidates.
- Product, requirements, architecture, UI/UX, role skills, and contributor
  guidance repeatedly require supersession links, approval histories, ADRs,
  rejected alternatives, or preserved decision trails.
- `.work/README.md`, its template, `AGENTS.md`, Cursor rules, both copies of the
  implementation-workflow skill, and contributor guidance require completed
  task files to remain indefinitely unless maintainers later clean them up.

## Current work-planning surface

The `.work/active/` inventory changed during the 2026-08-31 readiness review:
it first contained 120 Markdown files, including 114 `completed`, then dropped
to 55 files while concurrent cleanup remained uncommitted. The latest pass
found 51 `completed`, no `in-progress`, one substantively complete but
nonstandard `in_progress`, one owner-superseded `blocked`, and two `planned`
files. This volatility is itself a no-go signal. Phase 0 must regenerate the
inventory and compare it with Git so staged or completed deletions remain in
the classification scope.

The two files under `.work/resources/` are proposal inputs. The multi-channel
proposal has already been consumed by completed work. The Interaction
Controller proposal duplicates much of the one genuinely planned contract
task.

## Current implementation compared with intended behavior

| Area | Intended current behavior | Implementation evidence | Honest baseline classification |
| --- | --- | --- | --- |
| Identity and authorization | Organization/resource-scoped, deny-by-default human and service access | OIDC application sessions, PostgreSQL identity state, scoped API paths, Worker workload identity, and negative tests exist | Partial; several full-resource and operational matrices remain gaps |
| Assessment Campaign | Draft/setup/readiness/activation with frozen sources | `AssessmentConfiguration`, API endpoints, migrations `0034`-`0042`, tests, and production pages exist | Implemented bounded slice; broader Agent/Harness authoring is not implemented |
| Enrollment and timing | Assignment, lifecycle, shared admission, accommodations | API endpoints, migrations `0043`-`0047`, tests, and production pages exist | Implemented bounded slice |
| Submission | Immutable intake/version lifecycle | API endpoints, migrations `0048`-`0060`/`0056a`, contracts, tests, and production pages exist | Implemented bounded intake slice; Attempt start is absent |
| Session runtime | Frozen configuration, ordered events, streaming, timer/Worker foundations, audit/evidence handoff | Sessions modules, contracts, migrations `0005`-`0029`, Worker, and production SSE GET exist | Runtime foundation is substantial; hosted start/command/snapshot APIs and end-to-end production Session remain gaps/default-off |
| Evaluation, review, Result, Release | Distinct auditable outcome chain | Requirements and architecture contracts exist; no corresponding production host modules/endpoints | Planned product behavior / implementation gap |
| Production UI | Approved post-review UX contract implemented as vertical slices | Current `main` provides the rebuilt SPA and Shipboard slices; additional page/library changes remain uncommitted during this readiness review | Partial and still moving; freeze accepted production composition before reset classification |
| Design System, production donors, and Design Lab | Approved shared visual contract, current production compositions, Component Deck specimens, and fallback lab journeys | `docs/ui-ux/design-system/**`, `web/src/design-system/**`, production pages, and isolated `web/src/design-lab/**` exist | Preserve the accepted baseline and donor hierarchy; fixtures remain evidence, not product behavior |
| Voice, Dynamic memory, tools, shared Sessions, general Agent/Harness libraries | Deferred release capabilities | Placeholder specs/design preparation only | Not implemented; do not imply otherwise |

## Important contradictions and risks

- The requested snapshot-first policy conflicts with current `AGENTS.md`,
  documentation-author, architect, workflow skills, `.work` rules, and docs
  validation. Change them atomically only after current knowledge has a new
  canonical owner.
- Deleting ADRs before absorbing their current constraints could lose
  authorization, audit, atomicity, idempotency, streaming, provider,
  deployment, Worker, or frontend-isolation requirements.
- Repository-history cleanup can be confused with runtime history. Applied
  migrations, immutable contract versions, event logs, transcripts, evidence,
  evaluations, revision lineage, audit records, and release history are product
  or compatibility data and must not be rewritten or removed.
- Applied SQL migrations and compatibility fixtures contain legacy ADR labels.
  Their bytes may be checksum-sensitive; treat those labels as non-authoritative
  provenance and do not rewrite immutable files merely to remove an ADR name.
- The remaining owner visual review and acceptance may still rename or
  reorganize the proposed target UI documents. Execution must use the approved
  post-review result, not this discovery snapshot or unaccepted production
  behavior.
- The 2026-08-31 worktree contains hundreds of staged/unstaged changes and
  untracked paths across governance, documentation, UI, backend, tests, and
  Playwright evidence. Starting the reset from that moving tree would make
  authorship, rollback, validation, and deletion safety ambiguous.
- Canonical UI edits temporarily left generated `DESIGN.md` out of sync during
  readiness review; the current working state later restored green docs and
  adapter checks. Re-run them at the freeze so later concurrent changes cannot
  reintroduce inherited drift.
- Concurrent cleanup removed or staged removal of task files and Playwright
  evidence before this reset was activated. Those changes may be valid current
  work, but Gate A cannot approve them retroactively; their scope, retained
  evidence, and recovery path must be reconciled before the reset freeze.
- A repository-wide prose rewrite can accidentally claim unimplemented
  behavior. Every implementation statement needs a code/test/contract check.

# Target baseline structure

Minimize overlapping authority, not file count. Preserve separate documents
when they have distinct concerns, owners, lifecycles, stable identifiers,
operational purposes, or review boundaries.

```text
docs/
├── README.md                    # Authority map and navigation only
├── current-state.md             # Derived status/evidence index; non-normative
├── product/                     # Product meaning, vocabulary, MVP scope
├── requirements/                # Observable behavior and stable REQ/AC IDs
├── ui-ux/
│   ├── README.md                # UX authority and application-level UX architecture
│   ├── flows/                   # Approved representative journeys
│   └── design-system/           # Shared visual and interaction contract
├── architecture/                # Current technical realization and contracts
├── operations/                  # Current profiles, runbooks, compatibility evidence
└── contributing/                # Current delivery and validation process

.work/
├── active/                      # Active and explicitly approved planned work only
├── resources/                   # Temporary non-authoritative task inputs
└── templates/
```

| Concern | Canonical current-state source after reset | Notes |
| --- | --- | --- |
| Repository entry | `README.md` and `docs/README.md` | Short routes and authority rules; no maturity diary or historical catalog |
| Cross-concern status | `docs/current-state.md` | Derived, non-normative intended/implemented/legacy/planned/default-off/gap index; summaries link to owning specs, code/tests, or active work and do not duplicate their contracts |
| Product truth | `docs/product/{README,overview,concept-model,mvp-scope}.md` | Keep distinct vision, vocabulary/model, and scope owners; rewrite in current tense |
| Observable behavior | `docs/requirements/README.md`, approved specifications, and current operational requirements | Preserve distinct feature owners and stable `REQ-*`/`AC-*`; do not retain empty files merely to reserve future scope |
| Architecture truth | Current documents under `docs/architecture/` | Preserve separate system, backend, frontend, runtime, data, API/event, deployment, and compatibility contracts wherever their ownership or review boundary is real |
| UX/design truth | `docs/ui-ux/README.md`, approved documents under `docs/ui-ux/flows/`, and `docs/ui-ux/design-system/**` | README owns application-level UX architecture; flow files own distinct representative journeys; Design System owns shared presentation and patterns |
| Reference UX evidence | Approved flow mappings plus isolated `web/src/design-lab/README.md` and routes | Design Lab is visual/compositional evidence and never independently authorizes product behavior, permissions, routes, or scope |
| Operations | Current `docs/operations/**`, runbooks, profiles, compatibility/qualification evidence, and machine-readable build/config sources | Preserve evidence still required to operate, qualify, secure, reproduce, or verify the current system |
| Delivery rules | `AGENTS.md`, mirrored rules/skills, `.work/README.md`, and the task template | Snapshot-first governance without weakening planning, TDD, review, security, Playwright, or verification |
| Active/planned work | `.work/active/*.md` | Only executing work and explicitly approved future change |

# Scope

## In

- Inventory, classify, rewrite, merge, move, or delete repository
  documentation and governance needed for one current snapshot.
- Compare intended behavior with code, contracts, migrations, tests, SPA
  routes, build/deploy configuration, and Design Lab isolation.
- Absorb current technical constraints from all 21 ADRs into canonical
  architecture, operations, contribution, code-contract, or verification
  sources before removing `docs/architecture/decisions/`.
- Consolidate the approved post-review UI/UX baseline, make the application UX
  and representative flows explicit, and remove retirement/change-history
  authority.
- Replace completed-work retention with active/planned-only work hygiene while
  preserving the implementation workflow's planning, TDD, verification,
  review, and handoff controls.
- Update docs validation, generated adapters, tests, hard-coded paths, build
  metadata, and indexes for the new structure.
- Record genuine implementation gaps honestly without implementing them.

## Out

- New product features, missing backend endpoints, migrations, production UI
  slices, or Interaction Controller implementation.
- Reopening the owner-approved Design System or UI baseline without a concrete
  contradiction, accessibility failure, security issue, or missing required
  interaction pattern.
- Editing immutable applied migrations or compatibility fixtures solely to
  remove historical identifiers.
- Deleting product runtime auditability, version compatibility, evidence,
  qualification gates, or security/isolation requirements.
- Commit, push, pull request, deployment, or release actions unless separately
  requested.
- Creating an archive, migration diary, supersession ledger, or second plan
  hierarchy to describe the reset.

# Consolidation map

## Entry points, product, requirements, and status

| Current path | Action | Knowledge to preserve / target |
| --- | --- | --- |
| `README.md` | Rewrite | Short current phase, canonical routes, and validation commands; remove links to maturity/next-work diaries |
| `docs/README.md` | Rewrite | Keep authority by concern and navigation; move volatile maturity into `docs/current-state.md`; remove ADR and retirement catalogs |
| `docs/product/README.md` | Rewrite | Keep concern boundary and product routes; replace version/approval history and ADR-dependent next actions with current guidance |
| `docs/product/overview.md` | Keep and rewrite | Current vision, positioning, validation strategy, principles, and feature-delivery sequence |
| `docs/product/concept-model.md` | Keep and rewrite | Canonical vocabulary, relationships, lifecycles, configuration precedence, memory modes, evaluation chain, runtime-audit invariants; remove evolution narrative only after current meaning is explicit |
| `docs/product/mvp-scope.md` | Keep and rewrite | Current P0/next/later boundaries and honest non-goals; absorb any unique future-capability meaning from placeholder specs before deleting them |
| `docs/requirements/README.md` and `docs/requirements/features/README.md` | Rewrite | Catalog only approved/current specs and the authoring process for a new prioritized spec; do not require empty future files |
| Seven approved P0 files under `docs/requirements/features/` | Keep and rewrite | Preserve stable requirements/AC IDs and current observable behavior; remove approval/version history, ADR links, and volatile implementation matrices after migration |
| `docs/requirements/mvp-operational-defaults.md` | Keep and rewrite | Current cross-cutting limits, retention/lifecycle, recovery, and operational constraints; link to current architecture, not ADR chains |
| Twelve P1-P3 placeholder feature files | Absorb useful scope then delete | Preserve only unique current product scope in `mvp-scope.md`; create a new spec only when the capability is actually prioritized |
| New `docs/current-state.md` | Create | Derived, non-normative matrix of intended-source links, implemented surfaces, temporary legacy behavior, approved-plan links, gaps, evidence paths, default-off gates, and current review date; it owns status classification only |
| `PRODUCT.md`, `DESIGN.md` | Regenerate, keep non-authoritative | Update `scripts/impeccable_context.py` and tests to project from the new canonical source set |

The twelve placeholder deletion candidates are:

- `agent-library-configuration.md`
- `harness-library-configuration.md`
- `voice-interaction-interruption.md`
- `tool-execution-permissions.md`
- `workflow-stage-configuration.md`
- `harness-snapshots-comparison-restoration.md`
- `memory-governance-dynamic-mode.md`
- `memory-candidates-learning-approval.md`
- `harness-improvement-proposals.md`
- `shared-multi-participant-sessions.md`
- `calibration-analytics.md`
- `activity-deployment-forms.md`

## Architecture and operations

| Current path | Action | Knowledge to preserve / target |
| --- | --- | --- |
| `docs/architecture/mvp-architecture.md` | Keep/rewrite or rename only if Gate A approves a clearer current-state name | Current context, boundaries, ownership, trust/data flows, deployment, quality attributes, implementation gaps, and live constraints from ADR-001 through ADR-018 |
| `docs/architecture/backend-module-architecture.md` | Keep/rewrite; rename only if its concern or audience changes | Current module/persistence/API/event/authorization boundaries and implementation conventions |
| `docs/architecture/frontend-architecture.md` | Keep/rewrite | Current single-SPA topology, state ownership, API boundary, design-system/Design Lab isolation, routing, responsive/accessibility implementation constraints; absorb ADR-019 through ADR-021 |
| `session-runtime-contract.md`, `evaluation-execution-contract.md`, `review-result-release-contract.md` | Keep/rewrite | Current detailed runtime contracts and invariants; remove superseded decisions and approval histories while keeping stable contract IDs needed by code/tests |
| `docs/architecture/decisions/ADR-001` through `ADR-021` and `decisions/README.md` | Absorb useful current constraints then delete directory | Use an ADR-to-target extraction checklist; no deletion until every still-valid constraint has a current owner and inbound references are migrated |
| `docs/architecture/README.md` | Rewrite | Route by current architecture concern; explain direct-update policy and when a separate focused contract is justified |
| `docs/operations/provider-profiles/**` | Rewrite current profiles | Keep current safe configuration, qualification status, privacy boundaries, exact gates, and machine-verifiable evidence pointers |
| `docs/operations/provider-profiles/qualified/openrouter/synthetic-development-phase*.md` | Classify at Gate A; consolidate only narrative history | Preserve any evidence still needed to operate, qualify, reproduce, secure, or verify the current profile. Delete only phase narrative duplicated by retained operational or machine-verifiable evidence |
| `build/toolchain.json` | Amend | Point `governing` to current architecture/workspace sources instead of ADR-008/ADR-010 |
| Source comments/schema descriptions naming ADRs | Amend where safe | Replace authority claims with the current contract/invariant name; do not churn identifiers required for compatibility |
| Applied `database/migrations/up/**` and historical migration fixtures | Keep immutable | ADR labels are tolerated non-authoritative provenance; checks must not require byte rewrites |

## UI/UX, Design System, and Design Lab

| Current path | Action | Knowledge to preserve / target |
| --- | --- | --- |
| `docs/ui-ux/README.md` | Rewrite | Current UX authority plus application-level information architecture, navigation, shell, object hierarchy, page archetypes, shared states, responsive/accessibility expectations, approved baseline status, flow catalog, and legacy-code non-authority rule |
| `docs/ui-ux/activity-campaign-journey.md` | Move/rewrite under `docs/ui-ux/flows/` if it remains a distinct approved journey | Remove application-wide duplication after it is owned by `README.md`; preserve current end-to-end Campaign continuity and traceability |
| `docs/ui-ux/assessment-campaign-setup.md` | Move/rewrite under `docs/ui-ux/flows/` | Preserve the distinct Campaign setup/readiness/activation journey and review boundary |
| `docs/ui-ux/submission-attempt.md` | Move/rewrite under `docs/ui-ux/flows/` | Preserve the distinct Enrollment/My Work/Submission/Attempt journey and stable interaction traceability |
| `docs/ui-ux/text-session.md` | Move/rewrite under `docs/ui-ux/flows/` | Preserve the distinct Text Session journey, interaction states, recovery, and accessibility contract |
| `docs/ui-ux/evidence-evaluation-human-review.md` | Move/rewrite under `docs/ui-ux/flows/` | Preserve the distinct examiner/reviewer journey and Evidence/Evaluation/Review separation |
| `docs/ui-ux/result-release.md` | Move/rewrite under `docs/ui-ux/flows/` | Preserve the distinct Result/Release journey and authorization boundary |
| `docs/ui-ux/retired-authority.md` | Delete | Git owns the retired versions and reset narrative; no current behavior should depend on the ledger |
| `docs/ui-ux/design-system/change-record.md` | Delete | Absorb current constraints into module contracts and current version metadata; Git owns visual evolution |
| `docs/ui-ux/design-system/README.md` | Rewrite current-state portions | Keep approved visual authority/boundaries, tokens, shared decisions as current rules, verification, and restraint; remove supersession/change narrative |
| `docs/ui-ux/design-system/implementation-guide.md` and modules | Keep/reconcile | Preserve modular component/foundation/product contracts; remove obsolete links and ensure later-capability modules cannot authorize deferred scope |
| `web/src/design-system/**` | Keep; validate against docs | Production-safe shared implementation of the approved visual contract |
| `web/src/design-lab/**` and `web/src/design-lab/README.md` | Keep as isolated evidence; reconcile | Catalog approved representative flows/specimens, label synthetic/future cases, and maintain import/build isolation |
| Current production pages/routes | Retain as implementation only | Classify against approved UX and `docs/current-state.md`; existing behavior never overrides the baseline merely because it ships |

Do not merge distinct flow documents merely to reduce the count. Consolidate
only duplicated application-level architecture, shared patterns, or repeated
normative statements with one clear owner. Do not delete or move a source until
its replacement is approved, link-complete, and requirement/state coverage has
been mechanically and independently checked.

## Harness, rules, skills, process, and validation

| Current path | Action |
| --- | --- |
| `AGENTS.md` and `.cursor/rules/00-project-foundation.mdc` | Replace ADR authority with approved current architecture; state snapshot-first policy and retain runtime audit invariants |
| `.agents/skills/architect/SKILL.md` and `.cursor/skills/architect/SKILL.md` | Make current architecture docs the normal decision surface; create a focused decision artifact only when explicitly required, not as historical bookkeeping |
| Both `documentation-author` skills | Directly update current canonical sources; remove mandatory preservation of rejected alternatives, supersession chains, and approval history |
| Both `business-analyst`, developer, reviewer, tester, and specialist skill copies | Replace ADR/history assumptions and stale paths while preserving stable IDs, TDD, security/privacy, review independence, and evidence standards |
| `AGENTS.md`, `.cursor/rules/06-implementation-workflow.mdc`, both implementation-workflow skills, `.work/README.md`, `.work/templates/implementation-plan.md`, and `docs/contributing/development-harness.md` | Retain one live plan, progress markers, verification, promotion, review handoff, and no-secret rules; change completion to promote durable truth then remove completed/cancelled/superseded plans from the active surface |
| `.agents/skills/**` and `.cursor/skills/**` | Keep semantically mirrored; validate no drift after changes |
| `scripts/check_docs.py` | Replace fixed 19-file/ADR/UI-retirement checks with current catalog/status/schema checks, internal links/fragments, duplicate IDs, prohibited historical-authority patterns, current-state evidence, and active-work status hygiene |
| `scripts/impeccable_context.py` and tests | Update canonical source manifest and adapter text for approved post-review UX structure |
| Architecture tests/build scripts/hard-coded paths | Update only where they treat deleted docs or old paths as required authority; retain real production/Design Lab isolation checks |
| `.github/workflows/docs.yml` | Keep docs check and Markdown lint; include `.work/README.md`, template, and active plans if lint scope is intentionally expanded |

New governance wording must preserve this distinction:

> Repository specifications describe current intended truth and are updated
> directly. Git is the default repository history. Product runtime records,
> immutable configuration, events, transcripts, evidence, evaluations,
> revisions, releases, audit records, migrations, and compatibility contracts
> remain governed data and are not repository documentation history.

# Work-plan cleanup

## Remove after durable extraction

Generate the cleanup manifest from every task present at the Phase 0 freeze and
every task deleted since readiness reference `ea97a88`; do not rely on the
obsolete original 26-file list or only on files that still exist. During the
2026-08-31 review, concurrent cleanup reduced completed files visible in
`.work/active/` from 114 to 50 and then 51 as paths continued changing.
Classify each surviving or removed file and
delete or accept its prior deletion only after its still-current constraint,
status, and necessary verification pointer has been reconciled into canonical
docs, code/tests, operational evidence, or `docs/current-state.md`. A completed
marker alone is not extraction evidence, and bulk deletion by status or glob
without a manifest is prohibited.

Also remove after reconciliation:

- `impeccable-frontend-rebuild.md` — blocked, owner-superseded predecessor;
  absorb any current Impeccable governance into skills and current UI docs
- `shipboard-production-ux-reset.md` — only after it completes and the owner's
  follow-up approved UI baseline has absorbed its durable truth
- `create-assessment-campaign-commission.md` — only after its pre-activation
  disposition is recorded and any durable Design System,
  production-composition, or verification truth is promoted
- `.work/resources/multi-channel-agent-output-proposal.md` — completed input
  whose approved result already belongs in product/requirements/architecture
- `.work/resources/text-interaction-controller-proposal.md` — merge still-useful
  proposal content into the planned task or owning product scope, then delete
  the duplicate resource

## Retain only if genuinely planned

- `text-interaction-controller-contract.md` remains only if the Product Lead
  reconfirms it as genuinely prioritized future work against the post-review
  product/UX baseline. Rewrite its ADR/history dependencies to current sources
  and absorb its proposal resource. If it is merely an idea without current
  priority, preserve the deferred capability boundary in product scope and
  delete the task.
- Any newly discovered implementation gap belongs first in
  `docs/current-state.md`. Create a `.work/active/` task only when the
  owner actually prioritizes it; do not turn every gap into a plan during the
  reset.

## This task's own retirement

This plan remains through execution, independent review, and Approval Gate B.
It is the one explicit transitional exception to active/planned-only work
between Gate B acceptance and its cleanup. After Gate B accepts the reset and
external review is complete, remove this task in a subsequent bounded cleanup
change so the accepted reset and its evidence can be reviewed before its
planning record leaves the current surface. If the owner requests commits for
execution, perform this as a subsequent cleanup commit; this plan itself does
not authorize commits. Git retains the record and no separate completion
archive or reset diary is created.

# UI/UX baseline and feature-delivery strategy

The post-reset guidance must encode this sequence:

> Design System -> UX architecture -> representative flows/wireframes ->
> approved UX baseline -> production vertical slices

The application-level UX contract must define information architecture,
capability-aware navigation, application shell, object hierarchy, page
archetypes, list/detail/create/edit patterns, workflow continuity, statuses,
loading/empty/error/permission/destructive states, progressive disclosure,
responsive behavior, accessibility, and protected-content behavior before a
feature invents a local pattern.

Representative approved flows must cover the important continuity paths rather
than every screen, including:

- Agent -> Harness -> Campaign -> Enrollment/Participant -> Submission/Attempt
  -> Session -> Evidence/Evaluation -> Review -> Result/Release;
- Campaign creation, configuration, readiness, activation, and participant
  management;
- Participant My Work, Submission, Session lifecycle, and recovery;
- examiner/reviewer evaluation and release;
- Agent/Harness/memory configuration and snapshot management only as clearly
  labeled future/reference flows until product scope and approved requirements
  authorize them.

Design Lab specimens provide reusable reference evidence. They do not invent
roles, permissions, lifecycle transitions, server state, or release scope.
Legacy production code and screenshots are implementation evidence or temporary
legacy behavior only; they never outrank canonical UX, the approved Design
System, or approved Design Lab reference flows.

## Pre-build UI pattern adoption

Every new or meaningfully changed page and component must complete a pattern
classification before production implementation begins:

1. Trace the surface to its approved product scope, requirements, actor,
   permissions, and UI/UX behavior. Then classify its page archetype or layout
   family, component and product patterns, density, and required interaction,
   responsive, accessibility, security, and lifecycle states through the
   Design System implementation guide and Design Lab catalog.
2. When an approved category fits, reuse its production-safe Design System
   implementation, clone the closest matching accepted production page
   composition, and pair it with the governing Component Deck specimen. Use an
   approved Design Lab journey as the composition donor only when that approved
   family is not yet production-backed. Adapt content, data, and
   feature-specific behavior without duplicating CSS, forking an
   almost-identical component, or treating prototype fixtures as product
   authority.
3. When no approved category fits, record the concrete gap and why composition
   of existing patterns is insufficient. Invoke explicit `$impeccable shape`
   for bounded exploration before implementation. Its output is a proposal,
   not authority, and must be reconciled with product, requirements, UI/UX,
   accessibility, security/privacy, architecture, and release scope.
4. Approve and establish a genuinely reusable new pattern in the Design
   System contract, production-safe component library, and Design Lab specimen
   catalog before a production page consumes it. Keep genuinely
   feature-specific behavior in its narrower approved UI/UX specification.
5. Record the classification, selected production donor, Component Deck
   specimen, modules, any fallback Lab donor or approved new-pattern decision,
   adaptations, applicable states, and verification evidence in the
   implementation task.

The governing pre-build sequence is:

> Classify -> clone and adapt the matching accepted production page plus
> Component Deck specimen -> use an approved Lab journey only when the family
> is not production-backed -> or shape a bounded new proposal with
> `$impeccable shape` -> approve and establish the reusable pattern -> build
> the production surface

Production pages, Component Deck specimens, and Design Lab journeys donate
composition and presentation only. They never authorize routes, data,
permissions, lifecycle transitions, release scope, or server behavior; those
remain owned by approved product, requirements, and UI/UX sources.

Future feature delivery must:

1. confirm the feature against current product scope and approved requirements;
2. classify every new or changed page/component and record its selected Design
   System modules, accepted production-page donor, Component Deck specimen,
   any fallback Design Lab donor, states, and adaptations;
3. reuse and adapt an approved category before proposing a new one;
4. when no category fits, shape a bounded proposal with explicit
   `$impeccable shape`, approve it, and establish any reusable addition in the
   Design System/library/Lab before production use;
5. map the result into the application IA and validate representative
   wireframe/Design Lab behavior;
6. directly update canonical specs when intended behavior changes;
7. implement one production vertical slice with specification-driven TDD;
8. validate architecture, security/privacy, isolation, Design System, UX,
   accessibility, browser states, contracts, and regression tests;
9. update `docs/current-state.md` and current specs directly; and
10. remove the completed task from the active surface only after durable truth
   and evidence are promoted and the applicable review/acceptance gate passes.

# Product runtime invariants that must survive

- Organization, Activity scope, Participant, and Session isolation are
  non-bypassable; lower scopes may narrow but never widen delegated capability.
- Every Session records frozen resolved configuration and a resolved execution
  manifest with stable identity/integrity.
- Session events, transcripts, exact published output, interruption/cancellation
  distinctions, timing/order, tool/model attempts, and recovery facts remain
  reconstructable.
- Evidence links to stable sources; Evaluations preserve original outputs;
  human revisions, review decisions, Results, and Releases remain distinct and
  traceable.
- Harness snapshots, memory modes, memory eligibility/provenance, and explicit
  learning permission remain governed; no uncontrolled learning or harness
  self-modification is introduced.
- Sensitive mutations are authorized, idempotent where retryable, and audited
  with actor, scope, reason, prior state, and unambiguous time.
- Participant data is not reused for Agent learning without explicit
  permission. Secrets and raw sensitive content stay out of source, logs,
  metrics, task files, and browser artifacts.
- Applied migrations, immutable versions, compatibility readers, and exact
  evidence used by running or reconstructable Sessions are not historical-doc
  clutter and remain intact.

# Plan

## Phase 0 - Activate, freeze, and resolve concurrent work

- [x] Record the 2026-09-01 owner activation, verify every remaining activation
  prerequisite, and retain `in-progress` status while execution continues.
- [x] Finish, pause, or supersede every concurrent execution cursor. Reconcile
  `shipboard-production-ux-reset.md` and
  `create-assessment-campaign-commission.md`; normalize every task status to
  the repository's hyphenated status vocabulary before inventory.
- [x] Reconcile the concurrent task/artifact cleanup against `ea97a88`,
  including paths already absent from the worktree. Either complete it as
  separately accepted work with retained-evidence proof or restore it; do not
  inherit an unexplained staged deletion set into the reset.
- [x] Restore a green pre-reset baseline for documentation and generated
  adapters, including regeneration or reconciliation of `DESIGN.md` from its
  accepted canonical inputs.
- [x] Record the inspected Git commit, branch, dirty working-tree paths, staged
  state, untracked paths, concurrent agents/tasks, generated artifacts, and
  exact approved post-review UI/UX baseline.
- [x] Require a clean index and worktree at the freeze, except for this task's
  intentional activation edit. If the owner explicitly accepts a different
  snapshot, record every included path and why it is safe before proceeding;
  an unbounded dirty tree is never an accepted baseline.
- [x] Do not treat uncommitted or in-progress changes as established baseline
  truth unless the owner explicitly accepts them into reset scope.
- [x] Decide and record whether each conflicting in-flight task will complete,
  pause, be superseded, or rebase before consolidation. Only one reset cursor
  may modify a classified target.
- [x] Freeze inventories for authorities, links, code boundaries, validation,
  generated material, work status, immutable/compatibility artifacts, and
  operational evidence.

### Phase 0 exit gate

The inspected source, clean or explicitly bounded accepted state, concurrent
work disposition, normalized task statuses, exact affected paths, immutable
exceptions, green baseline validators, and Git recovery path are known.

## Phase 1 - Classify every material artifact

- [>] Classify each important file or directory as: current normative
  authority; current implementation guidance; current operational or
  compatibility evidence; implemented-status evidence; active or explicitly
  approved planned work; temporary legacy implementation; historical
  narrative; or generated/derived material.
- [ ] For mixed files, identify the exact current knowledge/evidence to migrate
  and the narrative or duplication eligible for removal.
- [ ] Build inbound-reference, stable-ID, validation, generated-source, and
  immutable/checksum dependency maps.
- [ ] Record conflicts as open questions with decision owner, impact, interim
  default, and rationale; do not silently select one source.

### Phase 1 exit gate

Every important artifact has one classification, every mixed source has an
extraction target, and no deletion candidate lacks a verified disposition.

## Phase 2 - Design the target authority and migration model

- [ ] Reconcile the target tree in this plan against the post-review baseline;
  define each source's concern, owner, lifecycle, status, upstream authority,
  and review boundary.
- [ ] Finalize the consolidation map, governance changes, deletion candidates,
  knowledge/evidence migration requirements, current implementation gaps, and
  validation changes.
- [ ] Define `docs/current-state.md` as a derived status/evidence index. Its
  intended and planned entries link to their owning canonical specification or
  active task without duplicating normative behavior or planned scope; its
  implemented entries link to code and verified tests.
- [ ] Produce the exact no-deletion rewrite sequence and rollback/recovery
  procedure.

### Delegated Gate A - Target authority approval

Before any canonical rewrite, governance cutover, or deletion, obtain independent
TDP reviewer approval under the owner's 2026-09-01 delegation of:

- the target authority model and file structure;
- the concern/owner/lifecycle boundaries and consolidation map;
- governance, workflow, skill, template, index, and validator changes;
- deletion candidates and required knowledge/evidence migration;
- immutable, compatibility, operational, security, and runtime-audit evidence
  that must remain; and
- the representation of known implementation gaps and concurrent work.

If Gate A changes the model, update this task before execution continues. Gate
A authorizes migration only; current repository governance remains binding
until the Phase 4 atomic cutover passes.

## Phase 3 - Consolidate current truth without deleting sources

- [ ] Rewrite product sources in current tense without changing meaning or
  converting examples/future ideas into requirements.
- [ ] Reconcile approved requirements, preserve stable `REQ-*`/`AC-*`, move
  volatile implementation claims to `docs/current-state.md`, and extract any
  unique scope from placeholder specifications.
- [ ] Rewrite current architecture and focused contracts. Extract ADR-001
  through ADR-021 one by one into current constraints or mark them historical
  only, but leave every ADR in place during this phase.
- [ ] Rewrite `docs/ui-ux/README.md` as application-level UX architecture and
  prepare approved representative journeys under `docs/ui-ux/flows/`, keeping
  distinct flow owners and all existing sources until replacements pass review.
- [ ] Reconcile the approved Design System and Design Lab references without
  allowing lab fixtures or legacy production behavior to authorize product
  behavior.
- [ ] Create the non-normative `docs/current-state.md` index from actual
  modules, endpoints, migrations, schemas, routes, builds, tests, current work,
  and operational gates. Link intended and planned classifications to their
  owners rather than restating their contracts.
- [ ] Consolidate current operations/runbooks/profiles while preserving all
  qualification, compatibility, security, and verification evidence still
  required for safe operation or reproducibility.
- [ ] Review the replacement sources by product/requirements, architecture,
  UI/UX, security/privacy, operations, and documentation concern while the old
  sources remain recoverable in the same tree.

### Phase 3 exit gate

Replacement sources are review-complete and ready for atomic approval/cutover,
but remain `Draft` or `In review` and non-authoritative under current governance.
No historical source has been deleted.

## Phase 4 - Cut over governance and validation

- [ ] Atomically approve the replacement canonical sources and update
  `AGENTS.md`, Cursor rules, both role/workflow skill trees, contributor
  guidance, `.work/README.md`, and templates to the Gate-A-approved authority
  and retention model.
- [ ] Preserve role routing, stable requirements, open-question controls,
  specification-driven TDD, security/privacy, independent review, Playwright,
  auditability, and verification requirements.
- [ ] Encode the pre-build UI pattern-adoption rule in applicable UI/UX,
  frontend, reviewer, tester, implementation-workflow, contributor, and task
  guidance: classify first; clone and adapt the matching accepted production
  page plus Component Deck specimen; use a Lab journey only for an approved
  family without a production donor; use explicit `$impeccable shape` only for
  a documented gap; approve and establish reusable additions before production
  use.
- [ ] Update indexes, `scripts/check_docs.py`, Impeccable adapters/generator
  tests, docs CI/lint scope, toolchain metadata, and path-sensitive
  architecture/build tests.
- [ ] Verify Codex/Cursor rule and skill parity and confirm the new validators
  reject stale authority, duplicate IDs, broken links, misleading status, and
  completed/obsolete work on the active surface.

### Phase 4 exit gate

Canonical sources and all repository governance/automation consistently apply
the approved model. The new model is now effective; historical sources remain
present only until the deletion manifest is verified.

## Phase 5 - Remove historical and obsolete surfaces

- [ ] Recheck each deletion candidate against its extraction target and
  surviving operational/security/compatibility evidence immediately before
  removal.
- [ ] Remove superseded ADR narrative, UI retirement/change narrative,
  placeholder scaffolds, obsolete/completed/blocked work, abandoned or absorbed
  proposals, stale instructions, and qualification narrative only where the
  classification proves no required evidence is lost.
- [ ] Preserve applied migrations, compatibility fixtures/readers, current
  operational qualification evidence, security verification, and runtime audit
  requirements even when they contain historical identities or versions.
- [ ] Reconfirm `text-interaction-controller-contract.md` as explicitly
  approved planned work or remove it after deferred scope is preserved.
- [ ] Produce the exact final deletion manifest with target/reason/evidence
  columns and run stale-reference/authority scans.

### Phase 5 exit gate

Only current authority/guidance/evidence, generated projections, active work,
and explicitly approved planned work remain. This still-active reset task is
included in that rule. Every deletion is recoverable from Git and justified by
the reviewed manifest.

## Phase 6 - Validate and reconcile

- [ ] Run the complete validation matrix below and record exact results.
- [ ] Reconcile the final repository against approved product intent, actual
  code/tests, the post-review UX baseline, surviving invariants, Gate A, and
  this task.
- [ ] Obtain independent product/requirements, architecture, UI/UX,
  security/privacy, operations, documentation, tester, and repository-process
  review; resolve every blocking finding.
- [ ] Prepare the final current-state matrix, deletion manifest, surviving
  invariant/evidence list, known gaps, verification evidence, and any
  not-applicable rationale for owner review.

### Delegated Gate B - Consolidated baseline acceptance

Before declaring the reset complete, obtain independent TDP reviewer approval under
the owner's 2026-09-01 delegation of:

- the consolidated canonical baseline and authority map;
- the final deletion manifest and confirmation that required operational,
  compatibility, security, and runtime-audit evidence survived;
- `docs/current-state.md` and its honest gap/default-off representation;
- the final active/planned work surface;
- all validation and independent-review evidence; and
- the revised feature-delivery and task-retention behavior.

Do not mark the reset complete or remove this task before Gate B approval.

## Phase 7 - Complete and clean up the reset task

- [ ] After Gate B, mark this task `completed`, reconcile final evidence, and
  leave it present through accepted external review as the explicit
  transitional cleanup exception.
- [ ] In a subsequent bounded cleanup change, remove this task under the newly
  approved retention policy. If commits were requested for execution, use a
  subsequent cleanup commit; otherwise leave commit control to the owner.
- [ ] Do not create a reset archive, completion diary, or replacement history
  document.

# Execution records

Keep these records in this task during execution so approvals and deletion
decisions are reviewable without creating a second plan or migration diary.

## Phase 0 freeze record

Inspected 2026-09-01 during TDP production of `item-76f0df7773cc`. Live facts
replace the 2026-08-31 readiness snapshot.

| Field | Live freeze value |
| --- | --- |
| Branch | `main` tracking `origin/main` |
| Inspected commit (parent of this freeze checkpoint) | `cda98826844480770bab7603506cc241638a15f4` |
| `origin/main` | identical to inspected commit |
| Readiness comparison commit | `ea97a88bf30a99423f6460099104ef2ba3e161a7` |
| Dirty paths before this freeze checkpoint | only unstaged `M .work/active/repository-baseline-reset.md` (owner activation plus this freeze record) |
| Staged paths | none |
| Untracked paths | none |
| Bounded accepted dirty exception | this reset task file only, until the Phase 0 checkpoint commit lands it |
| Git recovery | restore `cda9882` to drop uncommitted freeze edits; after the Phase 0 checkpoint commit, recover that commit from `git log -- .work/active/repository-baseline-reset.md`. Do not `reset --hard` unless the owner requests it. |

### Concurrent-task disposition

`.work/active/` contains **55** Markdown files. Hyphenated statuses only; no
`in_progress`.

| Status | Count | Disposition |
| --- | --- | --- |
| `completed` | 52 | Historical completed work on the active surface; classify in Phase 1; not concurrent execution |
| `in-progress` | 1 | `.work/active/repository-baseline-reset.md` — the only allowed reset cursor |
| `planned` | 1 | `.work/active/text-interaction-controller-contract.md` — retain until Phase 5 Product Lead reconfirm; not an execution cursor |
| `blocked` | 1 | `.work/active/impeccable-frontend-rebuild.md` — owner-superseded predecessor (2026-08-28); not an active cursor |

Named activation cursors:

- `shipboard-production-ux-reset.md`: `completed`; `owner_visual_pass: accepted`;
  `updated: 2026-08-31`.
- `create-assessment-campaign-commission.md`: `completed`; hyphenated status;
  `updated: 2026-08-29`.
- `.work/resources/`: `multi-channel-agent-output-proposal.md` and
  `text-interaction-controller-proposal.md` remain as non-authoritative inputs.

### Deletions since `ea97a88`

`git log --diff-filter=D ea97a88..HEAD` reports **259** deletions, all in
`cda9882` (*Freeze Shipboard class grammar…*). Worktree versus `HEAD` has **no**
uncommitted deletions. `overlay-closed-bezel.md` is absent at both `ea97a88` and
`HEAD` (not in this deletion window).

These deletions are **separately accepted Shipboard work already on
`origin/main`**, not a Gate A deletion set and not restored:

| Class | Count | Retained-evidence proof |
| --- | --- | --- |
| `.playwright-mcp/**` inspection PNGs | 231 | Recoverable from Git at `cda9882`; ephemeral browser artifacts, not operational evidence |
| Completed `.work/active/*.md` cleanup | 24 | Recoverable from Git at `cda9882^` / `cda9882`; listed below |
| Production/DS helper source removals in the same Shipboard commit | 4 | Recoverable from Git; replacements live in the Shipboard typed OperateArea/work-wrapper commit |

Deleted task files (24): `activities-registry-polish.md`,
`assign-dialog-single-scroll.md`, `assign-table-pattern-consistency.md`,
`assignment-station-pattern-consistency.md`, `ceremony-foot-hairline-bleed.md`,
`create-campaign-ds-gaps.md`, `dialog-tooltip-top-layer.md`,
`etched-frame-clip-rule.md`, `gallery-document-scroll.md`,
`gallery-seated-dialog-scroll.md`, `harness-attach-running-origins.md`,
`home-my-work-consistency.md`, `home-plate-grid-promotion.md`,
`impeccable-document-docs-sync.md`, `in-plate-host-hairline.md`,
`nested-scroll-ownership.md`, `plate-foot-air-resolved-note.md`,
`plate-foot-hairline-composition.md`, `plate-foot-hairline.md`,
`setup-note-remaining-polish.md`, `setup-resolved-note-alert.md`,
`toast-dock-placement.md`, `version-list-generic-composition.md`,
`viewport-aware-overlays.md`.

Deleted implementation files (4): `web/src/components/content/SafeContent.tsx`,
`web/src/design-system/components/select/useDismissOnOutsidePointer.ts`,
`web/src/design-system/components/state/AcknowledgmentGate.tsx`,
`web/src/hooks/useTheme.ts`.

### Approved UI baseline at freeze

- Design System `docs/ui-ux/design-system/README.md`: **Approved v1.0**, last
  reviewed **2026-08-31**, Shipboard Terminal visual authority.
- `docs/ui-ux/README.md`: replacement P0 journey specs **Approved v1.0** after
  the Shipboard production UX reset; retirement ledger still catalog-pinned.
- Production composition freeze: commit `cda9882` on `main`.
- Owner visual pass: recorded on `shipboard-production-ux-reset.md`.

### Freeze inventories (counts, not classifications)

| Inventory | Count / note |
| --- | --- |
| `docs/**/*.md` | 129 |
| `docs/architecture/decisions/*.md` | 22 (ADR-001–021 plus decisions README) |
| `docs/requirements/features/*.md` | 20 (7 P0 + 12 placeholders + features README) |
| `docs/ui-ux/**/*.md` | 60 |
| `docs/operations/**/*.md` | 10 |
| `database/migrations/up/*.sql` | 63 (through `0062` plus additive `0056a`) |
| `.agents/skills/**/SKILL.md` | 15 |
| `.cursor/skills/**/SKILL.md` | 14 |
| Generated adapters | `PRODUCT.md`, `DESIGN.md` current per impeccable check |

### Immutable / checksum exceptions (do not rewrite)

- Applied SQL under `database/migrations/up/**` (including ADR-token provenance).
- Historical migration/compatibility fixtures and readers; OpenAI-compatible
  example profiles under `docs/operations/provider-profiles/` and matching
  tests/modules.
- Checksum-sensitive ADR labels remain non-authoritative provenance per the
  recorded interim default.

### Pre-reset validators (this freeze)

| Command | Result |
| --- | --- |
| `python3 scripts/check_docs.py` | exit 0; `Documentation validation passed.` |
| `python3 scripts/impeccable_context.py check` | exit 0; `Impeccable context adapters are current.` |
| `python3 -m unittest discover -s scripts -p 'test_impeccable_context.py' -v` | exit 0; `Ran 15 tests in 0.007s` `OK` |

## Approval Gate A record

| Field | Value |
| --- | --- |
| Status | pending delegated review |
| Approved by | Independent TDP reviewer under owner delegation |
| Approved at | pending |
| Reviewed Git reference | pending |
| Decision and scope | pending |
| Required changes or conditions | Owner delegated intermediate approval on 2026-09-01; all substantive Gate A evidence and blocking-finding requirements remain |

## Classification and deletion manifest

Populate this manifest during Phases 1 through 5. Add rows as needed and keep
mixed-source extraction targets explicit. A deletion disposition is not valid
until its replacement or retained evidence has been verified.

| Path | Classification | Owning source or migration target | Evidence retained | Disposition | Verification |
| --- | --- | --- | --- | --- | --- |
| pending | pending | pending | pending | pending | pending |

## Approval Gate B record

| Field | Value |
| --- | --- |
| Status | pending delegated review |
| Approved by | Independent TDP reviewer under owner delegation |
| Approved at | pending |
| Reviewed Git reference | pending |
| Baseline and deletion-manifest decision | pending |
| Validation and independent-review summary | pending |
| Required follow-up or cleanup | Owner delegated intermediate approval on 2026-09-01 and will review the final result |

# Current state

Phase 0 freeze is recorded on 2026-09-01 against live Git, not the 2026-08-31
readiness snapshot. See **Phase 0 freeze record**.

Activation/freeze parent: branch `main`,
`cda98826844480770bab7603506cc241638a15f4`, equal to `origin/main`. The only
dirty path before the Phase 0 checkpoint commit is this task file. Concurrent
execution cursors are completed, blocked-superseded, or planned; this reset is
the only `in-progress` cursor. Deletions since `ea97a88` (259 paths, all in
`cda9882`) are separately accepted Shipboard work with Git recovery, not an
uncommitted deletion set.

Design System v1.0 and the 2026-08-31 Shipboard owner visual pass remain the
approved UI baseline. Pre-reset `check_docs.py`, `impeccable_context.py check`,
and 15 adapter unit tests passed on this freeze.

Current action: Phase 1 classification in this task. No canonical rewrite,
governance cutover, or deletion until delegated Gate A focused_output review
is persisted and copied here.

# Decisions and interim defaults

- Use one task file for the reset; do not create phase plans, migration diaries,
  or a documentation-history archive.
- Preserve stable requirement/acceptance and runtime contract IDs when code and
  tests depend on them. Snapshot-first removes historical prose, not useful
  identifiers or compatibility contracts.
- Prefer direct current-state statements with rationale where rationale is a
  live correctness constraint.
- **Open question - final UI/UX flow filenames. Decision owner:** Product/UI/UX
  Lead at Gate A. **Interim default:** place each approved representative
  journey under `docs/ui-ux/flows/` while keeping distinct documents for
  distinct owners, stable traceability, or review boundaries; move shared
  application architecture into `docs/ui-ux/README.md`. Rationale: minimizes
  overlap without forcing an arbitrary file-count target before the owner's
  post-review baseline exists.
- **Open question - Interaction Controller plan priority. Interim default:**
  retain it as planned only if the Product Lead explicitly reconfirms it during
  Phase 5; otherwise preserve deferred scope in product docs and delete the
  task. Rationale: a proposal is not genuine planned work by age alone.
- **Open question - current OpenRouter evidence retention. Decision owner:**
  Operations/Architecture Lead at Gate A. **Interim default:** retain every
  current machine-verifiable or human-readable record needed to reproduce,
  qualify, secure, or audit the active profile; remove only narrative history
  proven redundant after the canonical current profile and evidence index are
  complete. Rationale: Git history alone is insufficient for evidence needed
  by present operational gates.
- **Open question - immutable ADR labels. Interim default:** allow ADR tokens
  only inside checksum-sensitive applied migrations/fixtures and exact wire
  compatibility examples, explicitly non-authoritative. Rationale: rewriting
  immutable artifacts creates more risk than the label.

# Validation

| Check | Status | Evidence required after execution |
| --- | --- | --- |
| Pre-reset documentation baseline: `python3 scripts/check_docs.py` | passed Phase 0 freeze | 2026-09-01 freeze: exit 0, `Documentation validation passed.` |
| New documentation validator and link/fragment scan | pending | `python3 scripts/check_docs.py` passes against the new catalog |
| Markdown lint | pending | CI-equivalent lint passes for `README.md`, `docs/**`, harness rules/skills, `.work/README.md`, template, and retained active plans |
| Generated adapter consistency | passed Phase 0 freeze | 2026-09-01: `python3 scripts/impeccable_context.py check` exit 0; `python3 -m unittest discover -s scripts -p 'test_impeccable_context.py' -v` 15 tests OK |
| Historical-authority scan | pending | No live docs/harness requirement for ADRs, supersession chains, retirement/change records, migration diaries, or completed-task retention; allowlisted immutable artifacts reported separately |
| Path and terminology scan | pending | No stale deleted doc path, `web-legacy` current claim, old UI authority, or obsolete terminology remains |
| Requirements integrity | pending | Seven P0 specs/cataloged current behavior, unique `REQ-*`/`AC-*`, no scope loss, no placeholder files required |
| Architecture extraction audit | pending | ADR-001..021 extraction matrix independently reviewed; every current constraint has one canonical target before deletion |
| Operational/security/compatibility evidence audit | pending | Every qualification, runbook, immutable migration/fixture, security verification, and runtime-audit artifact is classified; retained evidence remains directly usable after narrative cleanup |
| Current-state audit | pending | Module/endpoint/migration/schema/route/test inventory agrees with `docs/current-state.md`; intended, implemented, temporary legacy, approved planned, gap, and default-off behavior are explicit |
| Work hygiene | pending | `.work/active/` contains only in-progress or explicitly planned tasks; no completed/blocked/cancelled/superseded files or duplicate proposal resources |
| Skill/rule parity | pending | Codex/Cursor copies are semantically equivalent and snapshot-first language is consistent |
| UI pattern-adoption governance | pending | Applicable guidance requires a recorded pre-build classification; sampled UI tasks select approved Design System modules, a matching accepted production-page donor, and a Component Deck specimen, use a Lab journey only when the family lacks a production donor, or document, shape, approve, and establish a genuine new reusable pattern before production use |
| Focused script tests | pending | `python3 -m unittest discover -s scripts -p 'test_impeccable_context.py'`, docs-validator tests added by the reset, and frontend-isolation script tests pass |
| Frontend verification | pending | `pnpm verify:web` passes; Design Lab/production isolation and approved reference specimens remain intact |
| .NET/architecture/contract verification | pending | `pnpm verify:dotnet` and `pnpm --dir contracts test` (or current equivalents) pass |
| Build and delivery checks | pending | `pnpm build`, applicable supply-chain/OCI checks, and path-sensitive build metadata pass proportionate to changed files |
| Authenticated browser verification | pending | Run `pnpm verify:oidc` if reset changes any routed docs-derived UI, build/profile path, or authenticated-browser contract; otherwise record reviewed not-applicable rationale |
| Delegated Gate A | pending | Independent reviewer approval record covers the target authority model, governance migration, deletion candidates, evidence preservation, and gap representation before rewriting begins |
| Independent cross-concern review | pending | Product/requirements, architecture, UI/UX, security/privacy, operations, documentation, tester/process findings resolved |
| Delegated Gate B | pending | Independent reviewer acceptance covers the final baseline, deletion manifest, surviving evidence/invariants, current-state matrix, work surface, and verification results |

Execution may split expensive checks into focused and full gates, but may not
claim completion from searches or Markdown links alone. UI screenshots are
required only if execution changes rendered UI; source-only documentation
consolidation does not itself claim visual verification. The approved
post-review UI, production donor, Component Deck, and fallback Design Lab
artifacts must nevertheless remain green.

# Blockers

- Cleared prerequisite: `shipboard-production-ux-reset.md` is completed with
  owner visual sign-off; Design System v1.0 remains approved and current.
- Cleared prerequisite: documentation, generated adapter, and focused adapter
  unit checks pass in the current working state.
- Cleared prerequisite: `create-assessment-campaign-commission.md` has normalized
  `completed` metadata.
- Cleared prerequisite: the activation reference was clean and synchronized with
  `origin/main` before the intentional activation edit.
- Cleared prerequisite: the owner activated direct execution on 2026-09-01 and
  delegated intermediate Gate A and Gate B decisions to independent TDP review.
- Cleared Phase 0: live freeze recorded; deletions since `ea97a88` accepted via
  `cda9882` on `origin/main` with Git recovery; pre-reset validators green.
- Current execution blocker: none. Next leaf is Phase 1 classification. Do not
  rewrite canonical sources until delegated Gate A review is persisted.

# Completion

- [ ] One coherent current product baseline exists without historical
  reconstruction.
- [ ] Canonical architecture, API/data/runtime boundaries, and operational
  constraints are clear without ADR lookup.
- [ ] Canonical application UX, Design System, and Design Lab/reference-flow
  authority is clear and approved.
- [ ] Feature delivery follows UX architecture and approved patterns before
  production vertical slices.
- [ ] Every new or meaningfully changed page/component is classified before
  implementation; existing Design System modules, accepted production-page
  compositions, and Component Deck specimens are cloned and adapted, with Lab
  journeys used only for families without production donors, while genuine
  gaps follow the bounded Impeccable proposal, approval, and shared-pattern
  establishment path.
- [ ] Important rationale survives as current constraints/invariants; runtime
  product auditability and immutable compatibility artifacts are preserved.
- [ ] Historical decision, retirement, change-record, phase-diary, placeholder,
  and supersession maintenance is removed from normal development.
- [ ] Only active and genuinely planned work remains, except this reset task's
  explicit Gate-B-to-cleanup transition; completed/cancelled/blocked/
  superseded plans and duplicate resources are removed.
- [ ] `docs/current-state.md` honestly distinguishes intended, implemented,
  temporary legacy, approved planned, default-off, and gap behavior.
- [ ] All deleted-path references, stale terminology, and contradictory
  authority claims are resolved.
- [ ] Documentation, formatting, tests, builds, architecture/isolation checks,
  and applicable delivery validation pass with recorded evidence.
- [ ] Independent cross-concern review is complete and blocking findings are
  resolved.
- [ ] Planned work is reconciled with actual changes and governing product
  sources are rechecked.
- [ ] This task's durable truth is promoted, Gate B accepts the reset, and the
  subsequent bounded cleanup change is recorded. Actual removal is follow-up
  cleanup, not a condition for marking this reset completed; Git retains the
  history.
