---
id: repository-baseline-reset
status: completed
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
| Twelve P1-P3 placeholder feature files | Absorb useful scope then delete | Gate A §5.1 is the sentence-level inventory; preserve only those unique sentences and related-decision constraints in `mvp-scope.md` / deferred product-architecture boundaries; create a new spec only when the capability is actually prioritized |
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
| `docs/operations/provider-profiles/qualified/openrouter/synthetic-development-phase*.md` (seven named files in Gate A §5.2) | Classify at Gate A; consolidate only narrative history | Gate A working class: retain-until-Phase-3/5 recheck for each named file. Preserve any evidence still needed to operate, qualify, reproduce, secure, or verify the current profile. Delete only phase narrative duplicated by retained operational or machine-verifiable evidence |
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

- [x] Classify each important file or directory as: current normative
  authority; current implementation guidance; current operational or
  compatibility evidence; implemented-status evidence; active or explicitly
  approved planned work; temporary legacy implementation; historical
  narrative; or generated/derived material.
- [x] For mixed files, identify the exact current knowledge/evidence to migrate
  and the narrative or duplication eligible for removal.
- [x] Build inbound-reference, stable-ID, validation, generated-source, and
  immutable/checksum dependency maps.
- [x] Record conflicts as open questions with decision owner, impact, interim
  default, and rationale; do not silently select one source.

### Phase 1 exit gate

Every important artifact has one classification, every mixed source has an
extraction target, and no deletion candidate lacks a verified disposition.

## Phase 2 - Design the target authority and migration model

- [x] Reconcile the target tree in this plan against the post-review baseline;
  define each source's concern, owner, lifecycle, status, upstream authority,
  and review boundary.
- [x] Finalize the consolidation map, governance changes, deletion candidates,
  knowledge/evidence migration requirements, current implementation gaps, and
  validation changes.
- [x] Define `docs/current-state.md` as a derived status/evidence index. Its
  intended and planned entries link to their owning canonical specification or
  active task without duplicating normative behavior or planned scope; its
  implemented entries link to code and verified tests.
- [x] Produce the exact no-deletion rewrite sequence and rollback/recovery
  procedure.

### Delegated Gate A - Target authority approval

- [x] Request independent TDP focused_output review of this Gate A package
  (item-b83e84eb7ea0). The producer does not approve. Copy the persisted
  reviewer respond into the Gate A record before any canonical rewrite.

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

Satisfied 2026-09-01: independent persist `review-focused-output-01` is
`approved` (verification `verified`) and copied into **Approval Gate A
record**. The producer did not author that respond.

## Phase 3 - Consolidate current truth without deleting sources

- [x] Rewrite product sources in current tense without changing meaning or
  converting examples/future ideas into requirements.
- [x] Reconcile approved requirements, preserve stable `REQ-*`/`AC-*`, move
  volatile implementation claims to `docs/current-state.md`, and extract any
  unique scope from placeholder specifications.
- [x] Rewrite current architecture and focused contracts. Extract ADR-001
  through ADR-021 one by one into current constraints or mark them historical
  only, but leave every ADR in place during this phase.
- [x] Rewrite `docs/ui-ux/README.md` as application-level UX architecture and
  prepare approved representative journeys under `docs/ui-ux/flows/`, keeping
  distinct flow owners and all existing sources until replacements pass review.
- [x] Reconcile the approved Design System and Design Lab references without
  allowing lab fixtures or legacy production behavior to authorize product
  behavior.
- [x] Create the non-normative `docs/current-state.md` index from actual
  modules, endpoints, migrations, schemas, routes, builds, tests, current work,
  and operational gates. Link intended and planned classifications to their
  owners rather than restating their contracts.
- [x] Consolidate current operations/runbooks/profiles while preserving all
  qualification, compatibility, security, and verification evidence still
  required for safe operation or reproducibility.
- [x] Review the replacement sources by product/requirements, architecture,
  UI/UX, security/privacy, operations, and documentation concern while the old
  sources remain recoverable in the same tree.

### Phase 3 exit gate

Replacement sources are review-complete and ready for atomic approval/cutover,
but remain `Draft` or `In review` and non-authoritative under current governance.
No historical source has been deleted.

## Phase 4 - Cut over governance and validation

- [x] Atomically approve the replacement canonical sources and update
  `AGENTS.md`, Cursor rules, both role/workflow skill trees, contributor
  guidance, `.work/README.md`, and templates to the Gate-A-approved authority
  and retention model.
- [x] Preserve role routing, stable requirements, open-question controls,
  specification-driven TDD, security/privacy, independent review, Playwright,
  auditability, and verification requirements.
- [x] Encode the pre-build UI pattern-adoption rule in applicable UI/UX,
  frontend, reviewer, tester, implementation-workflow, contributor, and task
  guidance: classify first; clone and adapt the matching accepted production
  page plus Component Deck specimen; use a Lab journey only for an approved
  family without a production donor; use explicit `$impeccable shape` only for
  a documented gap; approve and establish reusable additions before production
  use.
- [x] Update indexes, `scripts/check_docs.py`, Impeccable adapters/generator
  tests, docs CI/lint scope, toolchain metadata, and path-sensitive
  architecture/build tests.
- [x] Verify Codex/Cursor rule and skill parity and confirm the new validators
  reject stale authority, duplicate IDs, broken links, misleading status, and
  completed/obsolete work on the active surface.

### Phase 4 exit gate

Canonical sources and all repository governance/automation consistently apply
the approved model. The new model is now effective; historical sources remain
present only until the deletion manifest is verified.

## Phase 5 - Remove historical and obsolete surfaces

- [x] Recheck each deletion candidate against its extraction target and
  surviving operational/security/compatibility evidence immediately before
  removal.
- [x] Remove superseded ADR narrative, UI retirement/change narrative,
  placeholder scaffolds, obsolete/completed/blocked work, abandoned or absorbed
  proposals, stale instructions, and qualification narrative only where the
  classification proves no required evidence is lost.
- [x] Preserve applied migrations, compatibility fixtures/readers, current
  operational qualification evidence, security verification, and runtime audit
  requirements even when they contain historical identities or versions.
- [x] Reconfirm `text-interaction-controller-contract.md` as explicitly
  approved planned work or remove it after deferred scope is preserved.
- [x] Produce the exact final deletion manifest with target/reason/evidence
  columns and run stale-reference/authority scans.

### Phase 5 exit gate

Only current authority/guidance/evidence, generated projections, active work,
and explicitly approved planned work remain. This still-active reset task is
included in that rule. Every deletion is recoverable from Git and justified by
the reviewed manifest.

## Phase 6 - Validate and reconcile

- [x] Run the complete validation matrix below and record exact results.
- [x] Reconcile the final repository against approved product intent, actual
  code/tests, the post-review UX baseline, surviving invariants, Gate A, and
  this task.
- [x] Obtain independent product/requirements, architecture, UI/UX,
  security/privacy, operations, documentation, tester, and repository-process
  review; resolve every blocking finding.
- [x] Prepare the final current-state matrix, deletion manifest, surviving
  invariant/evidence list, known gaps, verification evidence, and any
  not-applicable rationale for owner review.

### Delegated Gate B - Consolidated baseline acceptance

- [x] Request independent TDP focused_output review of this Gate B package
  (item-c478fbc81dbb). The producer does not approve. Copy the persisted
  reviewer respond into the Gate B record before marking the reset complete.

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

Satisfied 2026-09-01: independent persist `review-focused-output-02` is
`approved` and copied into **Approval Gate B record**. The producer did not
author that respond. No required/blocking findings were reported. Two optional
minor findings were owner-actioned `accept_as_is` after workspace commit
`a344427`. Phase 7 then marked this task `completed` and left it on
`.work/active/`.

## Phase 7 - Complete and clean up the reset task

- [x] After Gate B, mark this task `completed`, reconcile final evidence, and
  leave it present through accepted external review as the explicit
  transitional cleanup exception.
- [ ] In a subsequent bounded cleanup change, remove this task under the newly
  approved retention policy. If commits were requested for execution, use a
  subsequent cleanup commit; otherwise leave commit control to the owner.
- [x] Do not create a reset archive, completion diary, or replacement history
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

Phase 3 product/requirements (2026-09-01): same three commands passed after
`python3 scripts/impeccable_context.py generate` regenerated `PRODUCT.md`.
No visual verification claimed (source-only documentation).

Phase 3 architecture (2026-09-01): `python3 scripts/check_docs.py` exit 0
`Documentation validation passed.` ADR files and `decisions/README.md`
unchanged. No adapter regeneration (DESIGN/PRODUCT inputs unchanged).

Phase 3 UI/UX (2026-09-01): `python3 scripts/impeccable_context.py generate`
regenerated `DESIGN.md` after README/DS/implementation-guide input changes.
`python3 scripts/check_docs.py` exit 0 (`Documentation validation passed.`).
`python3 scripts/impeccable_context.py check` exit 0 (`Impeccable context
adapters are current.`). Adapter unit tests: `Ran 15 tests in 0.008s` `OK`.
Originals, `retired-authority.md`, and `design-system/change-record.md`
remain. Six `docs/ui-ux/flows/` copies are In review. No visual verification
claimed (source-only documentation).

Phase 3 operations (2026-09-01): created `docs/current-state.md` (70 derived
P0 status rows linking Git `4994076`; live module/route/migration/test
inventory; no REQ/AC rewrite). README.md and docs/README.md route to that
file. Operations ADR-007/008 pointers in `docs/operations/`. Seven OpenRouter
phase files retained. In-tree review recorded in current-state. Validators
re-run on this leaf: `python3 scripts/impeccable_context.py generate` wrote
PRODUCT.md (docs/README.md is a PRODUCT source). `python3 scripts/check_docs.py`
exit 0. `python3 scripts/impeccable_context.py check` exit 0. Adapter tests
`Ran 15 tests in 0.007s` `OK`. No deletions.

Phase 4 cutover (2026-09-01): snapshot-first governance effective. Current
catalog is seven P0 specs; ADR directory and UI retirement ledger not required
by validators. Replacement sources Approved. `build/toolchain.json` governing
points at mvp-architecture and workspace.md. `python3 scripts/check_docs.py`
exit 0. `python3 scripts/impeccable_context.py check` exit 0. Script tests
`Ran 18 tests` `OK` including `test_check_docs.py`. No historical files
deleted.

## Approval Gate A record

Copied from independent persist `review-focused-output-01` (producer did not
author the respond). Gate A authorizes migration only; current repository
governance remains binding until Phase 4.

| Field | Value |
| --- | --- |
| Status | approved (copied from persist; not producer approval) |
| Approved by | Independent TDP reviewer under owner 2026-09-01 delegation (not the producer). Persist `reviewer_binding`: role `reviewer`, kind `reviewer`, provider `cursor`, `session_instance_id` `tdp-session-7a472fad0d97`, `provider_session_id` `005bbd38-d4a9-44ad-97e7-a75daace0e64`, generation 1, state `bound` |
| Approved at | persist `review-focused-output-01` revision 11; `lifecycle_status`/`status` `approved`; `verification_result.decision` `verified`; `verification_result.stage` `finding_verification`; `review_incomplete` null |
| Focused_output loop id | `review-focused-output-01` |
| Scope | `kind`/`type` `focused_output`; `item_ids` includes `item-48819dbb36d9`; `finding_set_id` `review-focused-output-01-fs-02`; `target_revision` 4; `target_digest` `c53a08e9c1aeda99ee9ea797fa6843fdd2161da8e43ea96bcf4d700f24a05cce` |
| Reviewed Git reference | evidence-revision checkpoint `ceb2b5565e31744c2daef6bdd2a09945a24a7cea` (`output-phase2-gate-a-inventories-rev02`); package commit `a60e6c6fcd018aebc4d44c1ea65c94d202a7409d`; freeze parent `cda98826844480770bab7603506cc241638a15f4` |
| Decision and scope | Independent reviewer closed the loop as `approved` after verifying resolved blocking findings against the Gate A package in this task (target authority model, consolidation map, deletion-candidate inventories §5.1–§5.3, immutable/runtime-audit exceptions, honest gaps, rewrite sequence, rollback). Canonical sources, governance, and deletion candidates were unchanged by the review. |
| Required changes or conditions | No remaining unresolved required findings. `finding-001`/`002`/`003` (`family-001`) `resolved`; `finding-004` (`family-002`) `invalid`. No new direct side effects. Open questions below keep their recorded interim defaults; Gate A working classification for named OpenRouter phase files is retain-until-Phase-3/5 recheck. Phase 4 remains the authority cutover. |

### Gate A persist copy (not producer-authored)

- Loop: `review-focused-output-01`, revision 11, `revise_at` `major` (initial
  respond requested changes; verification then closed the loop).
- Initial independent respond (producer did not author):
  `temp/tdp-inputs/runs/run-20260831T172348-9b56ac/agent-requests/review-respond-focused-output-r3-a01.json`
  against `target_revision` 3 /
  `5d5a8ea6d41d43acd69f07b7305da2289f184c05d75646d33e6c3bab23af6059`.
- Closing independent verification respond (producer did not author):
  `temp/tdp-inputs/runs/run-20260831T172348-9b56ac/agent-requests/review-respond-finding_verification-focused-output-r4-a01.json`
  against `target_revision` 4 /
  `c53a08e9c1aeda99ee9ea797fa6843fdd2161da8e43ea96bcf4d700f24a05cce`.
- Persist file:
  `temp/tdp-inputs/runs/run-20260831T172348-9b56ac/reviews/review-focused-output-01.json`
  (`status`/`lifecycle_status` `approved`, `verification_result.decision`
  `verified`).
- Closing verification summary (quoted from persist): producer challenge to
  `finding-004` accepted as invalid; evidence revision 4 (commit `ceb2b55`,
  `output-phase2-gate-a-inventories-rev02`) resolves `family-001`; no new
  direct side effects; canonical sources unchanged.

### Gate A package (producer evidence; independently approved for migration)

Reviewed against live freeze parent `cda9882` and classification commit
`f7d490afabbd74bb029a13405ea5823ac4c3ccd8`. This package does not change
repository authority. Current `AGENTS.md`, skills, rules, catalogs, and
validators remain binding until Phase 4.

#### 1. Target authority model

After Gate A + Phases 3–4, intended truth, implemented truth, planned work,
and derived status are separate:

| Layer | Owner | Lifecycle | Status at cutover | Review boundary |
| --- | --- | --- | --- | --- |
| Intended product meaning | `docs/product/{README,overview,concept-model,mvp-scope}.md` | Direct update | Approved current-tense product docs | Product Lead |
| Observable behavior | Seven P0 specs + `docs/requirements/README.md` + `mvp-operational-defaults.md` | Direct update; stable `REQ-*`/`AC-*` | Approved specs; catalogs list only current specs after Phase 4 | Product / BA |
| Technical realization | `docs/architecture/` excluding `decisions/` after Phase 5 | Direct update; focused contracts when ownership is distinct | Current architecture + code-contracts | Architecture |
| Application UX | `docs/ui-ux/README.md` | Direct update | Application IA/shell/archetypes | UI/UX |
| Representative journeys | `docs/ui-ux/flows/*` (six distinct owners; filenames per interim default) | Direct update | Approved v1.0 journeys, not merged | UI/UX per flow |
| Shared presentation | `docs/ui-ux/design-system/**` Approved v1.0 | Direct update of current rules | Do not reopen baseline without contradiction/a11y/security/missing pattern | UI/UX |
| Lab evidence | `web/src/design-lab/**` | Isolated | Evidence only; never authorizes routes/permissions/lifecycle/release | Architecture + UI/UX isolation |
| Derived status | `docs/current-state.md` | Regenerated from owners + inventory | **Non-normative**; stale if it conflicts with an owner | Documentation; not a spec |
| Implemented truth | `src/**`, `web/src/**` (production), `tests/**`, `contracts/**`, applied migrations | Code/TDD | Never promoted to intended truth by shipping | Engineering |
| Planned work | `.work/active/` in-progress + explicitly reconfirmed planned | Snapshot-first after Phase 4 | This reset remains until post-review cleanup | Implementation workflow |
| History | Git | Immutable commits | Not live authority | n/a |

`docs/current-state.md` must only link: intended → spec; planned → active task;
implemented → code/test/module/endpoint/migration/route. It must not restate
REQ/AC or architecture contracts. Conflict means the index is wrong.

Replacement sources in Phase 3 stay `Draft` or `In review` and
**non-authoritative** until Phase 4 cutover. Gate A authorizes that migration
target only.

#### 2. Reconciled target tree

The **Target baseline structure** in this task is the Gate A file tree, with
these reconciliations against the 2026-08-31 UI baseline and Phase 1:

- Keep `docs/architecture/mvp-architecture.md` name unless the reviewer
  requires a clearer current-state filename.
- Keep six journey documents as separate files under `docs/ui-ux/flows/`.
  **Interim default filenames:** `activity-campaign-journey.md`,
  `assessment-campaign-setup.md`, `submission-attempt.md`, `text-session.md`,
  `evidence-evaluation-human-review.md`, `result-release.md` (same basenames as
  today). Decision owner: Product/UI/UX Lead at Gate A.
- Design System modules stay in place; `change-record.md` and
  `retired-authority.md` leave the live tree only in Phase 5.
- `docs/architecture/decisions/` stays until Phase 5 after extraction.
- Placeholder feature files stay until Phase 5 after Phase 4 catalog cutover.
- Do not add `docs/current-state.md` or README links to it until the Phase 3
  operations leaf.

#### 3. ADR-001–021 extraction owner matrix

Phase 3 architecture leaf applies **architecture** and **code-contract** rows
only. **Operations** rows wait for the Phase 3 ops leaf. **Contribution** and
**verification** rows wait for Phase 4. **Historical only** is not applied as
a live constraint.

| ADR | Still-valid constraint summary | Extraction owner | Target (Phase) |
| --- | --- | --- | --- |
| 001 | Resolved configuration representation, digest, integrity | code-contract | `session-runtime-contract.md` (P3 arch) |
| 002 | Authorization decision contract, enforcement, delegation, freshness | architecture | `backend-module-architecture.md` + `mvp-architecture.md` (P3 arch) |
| 003 | Authorization audit ownership, append-only, durability | architecture | `backend-module-architecture.md` (P3 arch) |
| 004 | Assessment activation baseline, atomicity, idempotency | architecture | `mvp-architecture.md` (P3 arch) |
| 005 | Atomic Attempt start, submission-version binding, entitlement | code-contract | `session-runtime-contract.md` (P3 arch) |
| 006 | MVP app/deploy baseline, SPA/API, OIDC, persistence/recovery | architecture | `mvp-architecture.md` (P3 arch) |
| 007 | OSS-first self-hostable reference deploy, OCI | architecture + operations | `mvp-architecture.md` (P3 arch); deploy/runbook pointers (P3 ops) |
| 008 | Bounded OSS set, OpenAI-compatible + OpenRouter synthetic profile, Compose | operations + verification | current provider profiles (P3 ops); `build/toolchain.json` governing (P4) |
| 009 | Session / evaluation / review-result-release contract split | code-contract | the three `docs/architecture/*-contract.md` files (P3 arch) |
| 010 | .NET/React workspace, JCS, grate, test stack, openai_compatible adapter | contribution + verification | `docs/contributing/workspace.md`, toolchain (P4); stack facts also in `mvp-architecture.md` (P3 arch) |
| 011 | Durable-before-display streaming, replay, cutoff, backpressure | code-contract | `session-runtime-contract.md` (P3 arch) |
| 012 | Trusted invocation, structured decision, no-action, provenance | code-contract | `session-runtime-contract.md` (P3 arch) |
| 013 | Agent next-timer replacement bounds | code-contract | `session-runtime-contract.md` (P3 arch) |
| 014 | Decision envelope, P0 message-only compatibility | code-contract | `session-runtime-contract.md` (P3 arch) |
| 015 | Worker timer-lane delegation, reauthorization | architecture + code-contract | `mvp-architecture.md` + session contract (P3 arch) |
| 016 | Worker workload identity, invocation delegation | architecture + code-contract | same (P3 arch) |
| 017 | Assessment source descriptors, activation coordinator, fail-closed sources | architecture | `mvp-architecture.md` / backend module (P3 arch) |
| 018 | Enrollment request-limit vs PostgreSQL admission | architecture | `backend-module-architecture.md` (P3 arch) |
| 019 | Frontend state/library boundaries (Query, RHF/Zod, no Zustand/Tailwind/Axios) | architecture | `frontend-architecture.md` (P3 arch) |
| 020 | Dual-build `web-legacy` topology | historical only | Do not restore dual-build; lab isolation restated by 021 |
| 021 | Single production SPA in `web/`, isolated design-lab, no `web-legacy/` runtime | architecture | `frontend-architecture.md` (P3 arch) |

Phase 3 architecture (2026-09-01): architecture and code-contract rows
applied. **Applied this leaf:** ADR-001, 005, 009, 011–014 → session
contract; 002, 003, 018, 017 (module) → backend-module + mvp-architecture;
004, 006, 007 (arch), 010 stack facts, 015–016 (arch), 017 (activation) →
mvp-architecture; 019, 021 → frontend-architecture; 020 historical only.
**Not applied (matrix only):** 010 contribution/verification, remaining
verification (`build/toolchain.json` governing stays Phase 4).
Phase 3 operations (2026-09-01): **007 operations pointers** and **008
operations** applied under `docs/operations/` (README + provider-profile
index/OpenRouter retention). ADR files unchanged.
Phase 4 cutover (2026-09-01): **010 contribution/verification** applied to
workspace guidance, `build/toolchain.json` governing, validators, adapters,
and skills/rules. Remaining verification is the live `check_docs.py` /
impeccable / CI lint set.

#### 4. Governance and validator changes (Phase 4 only)

Effective only in the atomic cutover commit:

- Snapshot-first: completed/cancelled/blocked/superseded tasks leave
  `.work/active/` after durable promotion; Git remains history. Preserve TDD,
  independent review, security/privacy, Playwright, isolation, runtime-audit,
  role routing, open-question controls.
- Pre-build UI pattern-adoption sequence encoded in UI/frontend/reviewer/tester
  /workflow/contributor guidance.
- `scripts/check_docs.py`: drop 19-file/tier pins, ADR-021 existence pin, and
  retirement-ledger catalog pin; require the Gate-A-approved current catalogs,
  unique IDs, links, and reject stale historical-authority patterns.
- Specification catalogs and architecture ADR indexes: list current sources
  only; ADR directory not required after cutover.
- `docs/ui-ux/README.md`: drop retirement-ledger pin and keep Approved v1.0
  identification only as still accurate for journeys/DS, or equivalent current
  catalog wording approved at cutover.
- `scripts/impeccable_context.py` source lists follow rewritten product/UX
  inputs; regenerate adapters.
- `build/toolchain.json` `governing` points at current architecture/workspace
  docs, not ADR-008/010.
- Safe comment/schema ADR-as-authority edits; **not** applied migrations.
- Codex/Cursor skill and rule copies stay semantically mirrored.
- CI lint scope may include `.work` README/template/active plans.

#### 5. Deletion candidates and migration requirements

Execute only in Phase 5 after recheck. Already-deleted `cda9882` paths are
**not** Gate A deletions. Named inventories below are the Gate A freeze
baseline for Phase 3 extract and Phase 5 completeness; bulk deletion by
status or glob is prohibited.

| Candidate | Knowledge/evidence to migrate first | Surviving evidence |
| --- | --- | --- |
| 12 placeholder feature files (named in §5.1) | Unique sentences and related-decision constraints → `mvp-scope.md` and product/architecture deferred boundaries; explicit **none** where the body is TBD-only | Git |
| `docs/architecture/decisions/**` | Matrix in §3; inbound ADR mentions rewritten | Git; immutable ADR tokens remain in migrations |
| `docs/ui-ux/retired-authority.md` | None as live behavior; Git has retired versions | Git `eb9c398` cited today |
| `docs/ui-ux/design-system/change-record.md` | Live constraints already in DS modules/README | Git |
| Original journey files after `flows/` replacements are link-complete | Traceability IDs stay in replacements | Git |
| 52 completed `.work/active` tasks + blocked `impeccable-frontend-rebuild.md` (named in §5.3) | Recheck each named path for residual durable constraint → owning spec/code; **none** if already promoted | Git |
| `.work/resources/*.md` | Merge controller proposal into planned task or mvp-scope | Git |
| Seven OpenRouter `synthetic-development-phase*.md` files (named in §5.2) | File-level retain-until-Phase-3/5 recheck; delete only narrative proven redundant after current profile + machine evidence exist | **OQ:** Operations Lead; interim default retain-all |
| `text-interaction-controller-contract.md` | **OQ:** Product Lead Phase 5; interim default delete if not reconfirmed, keep deferred boundary in product docs | Git |
| `TODO.md` | Not a spec; optional hygiene, not required deletion | Git |

##### 5.1 Placeholder unique-scope extraction (Phase 3 must absorb; files stay until Phase 5)

Paths are under `docs/requirements/features/`. Unique sentences are those
that are not `TBD during authoring.` Catalog/source links already owned by
`docs/requirements/features/README.md` and product docs are not restated as
new requirements. Confirm-in-product means Phase 3 must keep the meaning in
`mvp-scope.md` / concept-model deferred boundaries even if it already exists
there; do not treat the placeholder body as empty.

| Path | Unique sentences to absorb | Related-decision constraints | Migration target |
| --- | --- | --- | --- |
| `agent-library-configuration.md` | Implementation remains deferred to P1. Authoring must carry approved `PROP-AGENT-1` into testable persona authoring, revision, preview, validation, honest Agent attribution, real-person impersonation prevention, and later human-likeness extension boundaries. The P0 assessment flow may select existing pre-provisioned Agent revisions but does not gain general Agent authoring. | Approved `PROP-AGENT-1` person-like persona and honest Agent identity boundary | Confirm/absorb in `mvp-scope.md` P1/deferred Agent-library boundary and concept-model `PROP-AGENT-1` / pre-provisioned selection (already present; must not drop) |
| `harness-library-configuration.md` | **none** (TBD-only body) | **none** | Deferred P1 harness-library name already in mvp-scope/catalog; no extra sentences |
| `voice-interaction-interruption.md` | **none** (TBD-only body) | ADR-012 defines the interaction-signal/Decision seam but **does not approve** interruptible voice | Confirm in `mvp-scope.md` next-release voice non-goal; architecture: ADR-012 does not authorize this feature |
| `tool-execution-permissions.md` | **none** (TBD-only body) | ADR-012 defines `request_tool` as a non-authoritative recommendation and tool-result chaining as a new Invocation, but **does not approve** tool execution | Confirm in `mvp-scope.md` next-release tools non-goal; architecture/code-contract: `request_tool` remains non-authoritative |
| `workflow-stage-configuration.md` | **none** (TBD-only body) | ADR-012 defines trusted workflow triggers and non-authoritative transition proposals, but **does not approve** configurable workflow stages | Confirm deferred configurable stages in `mvp-scope.md`; architecture: ADR-012 does not authorize this feature |
| `harness-snapshots-comparison-restoration.md` | **none** (TBD-only body) | **none** | Deferred next-release name already in mvp-scope/catalog |
| `memory-governance-dynamic-mode.md` | **none** (TBD-only body) | **none** | Deferred next-release dynamic memory name already in mvp-scope/catalog |
| `memory-candidates-learning-approval.md` | **none** (TBD-only body) | **none** | Deferred later-release memory-candidates name already in mvp-scope/catalog |
| `harness-improvement-proposals.md` | **none** (TBD-only body) | **none** | Deferred later-release harness-improvement name already in mvp-scope/catalog |
| `shared-multi-participant-sessions.md` | **none** (TBD-only body) | **none** | Deferred later-release shared-sessions name already in mvp-scope/catalog |
| `calibration-analytics.md` | **none** (TBD-only body) | **none** | Deferred later-release calibration/analytics name already in mvp-scope/catalog |
| `activity-deployment-forms.md` | **none** (TBD-only body) | **none** | Deferred later-release alternative activity forms name already in mvp-scope/catalog |

##### 5.2 OpenRouter qualified phase files (Gate A working classification)

Directory: `docs/operations/provider-profiles/qualified/openrouter/`.
Delegated working classification for every file: **retain until Phase 3/5
recheck** (not narrative-only). Operations OQ may later mark a file
narrative-only only after the current profile plus machine-verifiable
evidence are confirmed to duplicate it.

| Path | Gate A disposition |
| --- | --- |
| `synthetic-development-phase9-2026-08-20.md` | retain-until-Phase-3/5 recheck |
| `synthetic-development-phase20-2026-08-20.md` | retain-until-Phase-3/5 recheck |
| `synthetic-development-phase21-2026-08-20.md` | retain-until-Phase-3/5 recheck |
| `synthetic-development-phase22-2026-08-20.md` | retain-until-Phase-3/5 recheck |
| `synthetic-development-phase24-2026-08-21.md` | retain-until-Phase-3/5 recheck |
| `synthetic-development-phase27-2026-08-21.md` | retain-until-Phase-3/5 recheck |
| `synthetic-development-phase28-2026-08-21.md` | retain-until-Phase-3/5 recheck |

##### 5.3 Freeze-complete completed and blocked task deletion-candidate set

Not concurrent cursors. Not this reset. Not the planned controller task.
Phase 5 must recheck this named set (plus Git recovery), not a status glob.
Already-deleted 24 tasks from `cda9882` remain in the Phase 0 freeze list
and are **not** this Gate A set.

**Blocked (1):** `.work/active/impeccable-frontend-rebuild.md`

**Completed (52), freeze 2026-09-01, all under `.work/active/`:**
`admin-activities-create-route.md`,
`assign-dialog-datatable-paging.md`,
`assignment-station-guided-task.md`,
`breadcrumb-destination-trail.md`,
`canonical-contract-jcs-foundation.md`,
`canonical-contract-package.md`,
`ceremony-unavailable-auth-commit.md`,
`collapsible-nav-groups.md`,
`component-owned-class-grammar.md`,
`create-assessment-campaign-commission.md`,
`demo-seed-accounts.md`,
`dotnet-react-workspace-scaffold.md`,
`enrollment-assign-decisions.md`,
`enrollment-assign-selector.md`,
`form-section-sibling-grouping.md`,
`frontend-build-docker.md`,
`frontend-state-form-library-foundation.md`,
`item-list-load-more.md`,
`loopback-provider-logout.md`,
`multi-channel-agent-output-contract-adoption.md`,
`oidc-application-session-foundation.md`,
`oidc-integration-harness-normalization.md`,
`p0-activity-journey-frontend-realization.md`,
`p0-assessment-setup-cohort-activation.md`,
`p0-enrollment-assignment-discovery.md`,
`p0-enrollment-shared-admission-review-fixes.md`,
`p0-enrollment-shared-request-quota.md`,
`p0-participant-timing-accommodations.md`,
`p0-submission-intake-immutable-versioning.md`,
`participant-home-assignment-plates.md`,
`participants-assign-dialog.md`,
`postgres-authorization-configuration-foundation.md`,
`seated-operator-identity.md`,
`seed-numbered-demo-accounts.md`,
`session-runtime-live-provider-qualification.md`,
`session-runtime-openrouter-synthetic-qualification.md`,
`session-runtime-production-http-sse.md`,
`session-runtime-subject-binding-rehydration.md`,
`session-runtime-worker-binding-timer-activation.md`,
`session-runtime-worker-host-wiring.md`,
`session-runtime-worker-identity-invocation-delegation.md`,
`setup-readiness-ceremony-record.md`,
`setup-readiness-form-sections.md`,
`setup-readiness-summary-harden.md`,
`shipboard-production-ux-reset.md`,
`signin-fail-closed-recovery.md`,
`sso-logout-id-token-hint.md`,
`static-header-assign-table.md`,
`structured-agent-runtime-sync.md`,
`structured-agent-runtime-traceability.md`,
`unify-participant-home-my-work.md`,
`workspace-toast-advisory.md`.

#### 6. Immutable, compatibility, security, and runtime-audit exceptions

Must remain; not historical clutter:

- Organization, activity, participant, session isolation; deny-by-default authz
- Frozen session configuration and execution manifests
- Reconstructable events, transcripts, output, interruption/cancel distinctions
- Evidence/evaluation/revision/result/release distinct objects
- Memory/learning permission; no uncontrolled learning or harness self-mod
- Applied `database/migrations/up/**` (through `0062` + `0056a`)
- OpenAI-compatible example profiles, modules, tests
- Current qualification/runbook/security verification needed to operate
- Checksummed toolchain hashes (change `governing` pointers only)

ADR labels inside those immutable bytes stay non-authoritative provenance.

#### 7. Known gaps and concurrent work (honest)

Do not implement these in this reset. Index them in `docs/current-state.md`.

| Area | Classification |
| --- | --- |
| Identity/authorization matrices beyond implemented OIDC/scoped API/Worker identity | Partial implemented; remaining matrices are gaps |
| Agent/Harness library authoring | Not implemented |
| Attempt start | Gap |
| Hosted session start/command/snapshot APIs; e2e production Session | Gap / default-off |
| Evaluation, human review, Result, Release host modules | Intended in P0 specs; **implementation gap** |
| Voice, dynamic memory, tools, shared sessions | Deferred; placeholders are not requirements |
| Production UI | Implemented Shipboard slices at `cda9882`; remaining product journeys may still be incomplete vs P0 UX — record from inventory in Phase 3, do not treat readiness-era dirty tree as current |
| Interaction Controller | Deferred/planned only if Phase 5 reconfirms |
| Concurrent cursors | Only this reset is `in-progress`; 52 completed + 1 blocked + 1 planned as Phase 1 |

P0 implementation matrices that cite `web-legacy` or stale migration numbers
are **implemented-status drift**, not permission to restore `web-legacy/`.
Interim default: code/tests own implemented truth; REQ/AC remain intended
truth.

#### 8. No-deletion rewrite sequence

Order after Gate A persist is copied into this task:

1. Phase 3 product + requirements + mvp-operational-defaults + README entry
   points **without** current-state route (`item-3c9cd9e9a2c1`); regenerate
   PRODUCT.md if those inputs change.
2. Phase 3 architecture + code-contracts + record remaining matrix rows
   (`item-a8dbad2a8346`); ADRs stay on disk; catalogs/pins unchanged.
3. Phase 3 UI README + `docs/ui-ux/flows/` preparations + DS/Lab reconcile
   (`item-87f71e2b96ef`); keep retirement pin and Approved v1.0; regenerate
   DESIGN.md if inputs change.
4. Phase 3 operations ADR rows + create `docs/current-state.md` + add README
   current-state route + in-tree specialist notes (`item-2e7864079029`).
5. Phase 4 atomic cutover (`item-7190be5d0f02`).
6. Phase 5 verified deletions (`item-d8ff5629ae40`).

Steps 2 and 3 may proceed after step 1; step 4 depends on both 2 and 3.

#### 9. Rollback and recovery

| Stage | Recovery |
| --- | --- |
| Before Phase 3 | `git checkout` / reset only if owner requests; freeze parent `cda9882`; Phase 0 `f1c2d73`; Phase 1 `f7d490a`; package `a60e6c6`; inventories `ceb2b55`; this Gate A copy-back commit |
| During Phase 3 | Revert the leaf commit; old sources still present; validators still old |
| After Phase 4 before Phase 5 | Revert the cutover commit to restore old governance; replacement docs remain but become non-effective if validators revert with it — revert cutover as one commit |
| After Phase 5 | Restore deleted paths from Git by path from the Phase 5 parent; do not reconstruct from memory |
| Migrations/fixtures | Never revert by rewriting bytes to strip labels; restore whole files from Git if damaged |
| Do not | `push --force`, hook bypass, or treat Git history as sufficient for operational qualification evidence that was deleted without surviving owner |

## Classification and deletion manifest

Phase 1 inventory against freeze `f1c2d738a7edb4c68e735364a5196706946ded53`
(parent `cda9882`). Rows are path groups where classification is uniform;
named files are listed when disposition or mixed extraction differs. No
canonical source is rewritten in this phase. A deletion disposition is not
Gate-A execution permission; it is the recorded candidate or already-accepted
history class.

Classification vocabulary: **normative** (current intended authority),
**guidance** (implementation/process), **ops-evidence** (operate/qualify/secure),
**implemented-status** (code/tests as implemented truth), **active/planned
work**, **temporary-legacy**, **historical**, **generated**, **immutable**.
**Mixed** rows must not be treated as purely historical.

| Path | Classification | Owning source or migration target | Evidence retained | Disposition | Verification |
| --- | --- | --- | --- | --- | --- |
| `README.md` | mixed: normative navigation + historical phase/maturity | Keep current routes/validation commands; move volatile maturity to derived `docs/current-state.md` (Phase 3/ops leaf owns that file) | Git | keep-rewrite; no current-state link until that file exists | Phase 3 product leaf; `check_docs.py` |
| `docs/README.md` | mixed: authority-by-concern + maturity/history + ADR/retirement catalogs | Keep authority map; drop historical catalogs after Phase 4 validator cutover | Git; inbound links | keep-rewrite; validator-pin until Phase 4 | Phase 3 then Phase 4 catalogs |
| `docs/product/README.md` | mixed: concern boundary + version/ADR next-actions | Current product routes in this file | Git | keep-rewrite | Phase 3 |
| `docs/product/overview.md` | mixed: current vision + status/version narrative | Product meaning owner | Git; PRODUCT.md input | keep-rewrite current tense; do not convert examples to requirements | Phase 3; regenerate PRODUCT.md |
| `docs/product/concept-model.md` | mixed: vocabulary/invariants + evolution narrative | Concept-model owner | Git; PRODUCT.md input | keep-rewrite; preserve runtime-audit invariants | Phase 3 |
| `docs/product/mvp-scope.md` | mixed: P0/next/later + absorb unique placeholder scope | MVP scope owner | Git; PRODUCT.md input | keep-rewrite; absorb placeholder unique scope here | Phase 3 |
| `docs/requirements/README.md` | mixed: catalog process + frozen 19-file/tier pins | Observable-behavior catalog; `scripts/check_docs.py` | Git | keep membership/order/tier counts until Phase 4 | Phase 3 content; Phase 4 catalog cutover |
| `docs/requirements/features/README.md` | mixed: same catalog pin | Same | Git | same as requirements README | same |
| `docs/requirements/mvp-operational-defaults.md` | mixed: current ops limits + ADR-linked history | Operational defaults owner | Git | keep-rewrite; relink to architecture not ADR chains | Phase 3 |
| Seven P0 `docs/requirements/features/{auth-resource-isolation,resolved-session-configuration,assessment-setup,submission-attempts,session-text-lifecycle,evidence-evaluation,review-result-release}.md` | mixed: unique `REQ-*`/`AC-*` + volatile implementation matrices + approval history | Each file remains the REQ/AC owner; matrices queued to derived current-state index | 618 unique ID definitions (see stable-ID map) | keep-rewrite; **do not** treat matrices or shipped UI as new requirements | Phase 3; integrity scan Phase 6 |
| Twelve P1–P3 placeholders (agent-library, harness-library, voice-interaction-interruption, tool-execution-permissions, workflow-stage-configuration, harness-snapshots-comparison-restoration, memory-governance-dynamic-mode, memory-candidates-learning-approval, harness-improvement-proposals, shared-multi-participant-sessions, calibration-analytics, activity-deployment-forms) | mixed: empty/future scaffold + unique deferred-scope sentences inventoried in Gate A §5.1 | Unique scope → `mvp-scope.md`; files stay until Phase 5 after Phase 4 catalog cutover | Git; validator currently requires all 12; §5.1 extraction table | absorb-then-delete candidate; **not** approved specs | Phase 3 extract per §5.1; Phase 5 delete after Gate A |
| `docs/current-state.md` | absent | Create as derived non-normative index only | n/a | create in Phase 3 ops leaf; not a behavior owner | Phase 3 |
| `docs/architecture/README.md` | mixed: index + ADR routing | Current architecture navigation | Git | keep-rewrite | Phase 3 architecture leaf |
| `docs/architecture/mvp-architecture.md` | mixed: current baseline + ADR/evolution | System architecture owner; absorb ADR-001–018 still-valid constraints | Git | keep-rewrite; do not rename unless Gate A | Phase 3 |
| `docs/architecture/backend-module-architecture.md` | mixed: module/API/auth conventions + history | Backend architecture owner | Git | keep-rewrite | Phase 3 |
| `docs/architecture/frontend-architecture.md` | mixed: SPA/state/DS/Lab isolation + history | Frontend architecture owner; absorb ADR-019–021 still-valid constraints | Git | keep-rewrite | Phase 3 |
| `docs/architecture/{session-runtime,evaluation-execution,review-result-release}-contract.md` | mixed: current runtime contracts + superseded decision prose | Code-contract owners under `docs/architecture/` excluding `decisions/` | Git; tests citing contract IDs | keep-rewrite; preserve stable contract identifiers | Phase 3 architecture leaf |
| `docs/architecture/decisions/README.md` and `ADR-001`–`ADR-021` (21 ADRs) | mixed: still-valid constraints + historical alternatives/supersession | Extraction matrix in this task (Phase 2/3); files remain until Phase 5 | Git; 39 inbound files outside `decisions/` (see inbound map); `check_docs.py` pins ADR-021 | absorb-then-delete candidate; **validator-pin until Phase 4** | Phase 2 matrix; Phase 3 extract architecture-owned rows; Phase 4 drop pins; Phase 5 delete after recheck |
| `docs/ui-ux/README.md` | mixed: UX authority + Approved v1.0 P0 pin + retirement-ledger catalog pin | Application UX architecture owner | Git; DESIGN.md input; `check_docs.py` | keep-rewrite architecture; **keep catalog pins until Phase 4** | Phase 3 UI leaf |
| `docs/ui-ux/{activity-campaign-journey,assessment-campaign-setup,submission-attempt,text-session,evidence-evaluation-human-review,result-release}.md` | mixed: distinct approved journeys + duplicated app-wide architecture | Distinct flow owners; prepare `docs/ui-ux/flows/` copies in Phase 3; do not merge owners | Git | keep-rewrite/move under flows; originals stay until replacements link-complete | Phase 3 UI leaf; Gate A flow-filename interim default |
| `docs/ui-ux/retired-authority.md` | historical UI retirement ledger | Git owns retired versions; `check_docs.py` + UI README pin | Git; inbound from `docs/README.md`, UI README, activity-campaign-journey, ADR-021 | delete candidate after Phase 4 pin removal | Phase 5 |
| `docs/ui-ux/design-system/README.md` | mixed: Approved v1.0 current rules + supersession/change narrative | Design System owner | Git; DESIGN.md input | keep-rewrite current-state portions; do not reopen v1.0 | Phase 3 |
| `docs/ui-ux/design-system/implementation-guide.md` and modules under `foundation/`, `components/`, `product/` (52 markdown files in design-system tree) | normative shared presentation | Design System modules; later-capability modules must not authorize deferred product scope | Git; DESIGN.md subset of modules | keep/reconcile links; no product-scope widening | Phase 3; isolation tests |
| `docs/ui-ux/design-system/change-record.md` | historical DS change narrative | Absorb live constraints into module metadata; Git owns evolution | Git; inbound from DS README | delete candidate after absorption | Phase 5 |
| `docs/operations/provider-profiles/README.md`, `openrouter-synthetic-development.md`, `keycloak-oidc-contract.md`, and `*.example.json` profiles | ops-evidence / current profiles | Operations owner; apply operations-owned ADR rows in Phase 3 ops leaf | Git; qualification tests | keep-rewrite current safe config and evidence pointers | Phase 3 ops; Gate A OpenRouter retention OQ |
| `docs/operations/provider-profiles/qualified/openrouter/synthetic-development-phase*.md` (7 named files in Gate A §5.2) | mixed: phase narrative + possibly still-needed qualification evidence | Current profile + machine-verifiable evidence; delete only redundant narrative | Git | retain-until-Phase-3/5 recheck per named file; not bulk-delete | Phase 3/5 |
| `docs/contributing/development-harness.md`, `docs/contributing/workspace.md` | guidance | Contributor process; apply contribution ADR rows at Phase 4 | Git | keep; snapshot-first at cutover | Phase 4 |
| `AGENTS.md`, `.cursor/rules/*.mdc`, `.agents/skills/**/SKILL.md`, `.cursor/skills/**/SKILL.md` | mixed: live governance + ADR/history retention rules | Remain binding through Gate A and Phase 3; cutover Phase 4; keep Codex/Cursor parity | Git | keep; rewrite only at Phase 4 atomic cutover | Phase 4 parity check |
| `.work/README.md`, `.work/templates/implementation-plan.md` | guidance | Implementation workflow; retention policy changes at Phase 4 | Git | keep | Phase 4 |
| `.work/active/repository-baseline-reset.md` | active work | This reset cursor | this file | retain through whole-output review | Phase 7; no in-graph delete |
| `.work/active/text-interaction-controller-contract.md` | planned work (unconfirmed priority) | Product Lead Phase 5 reconfirm or defer into product scope | Git; duplicate `.work/resources/text-interaction-controller-proposal.md` | retain-until-Phase-5 decision | Phase 5 OQ |
| `.work/active/impeccable-frontend-rebuild.md` | historical blocked/superseded | Durable Impeccable/UI governance already in skills and DS | Git | delete candidate after extraction check; named in Gate A §5.3 | Phase 5 |
| `.work/active/*.md` other 52 `completed` tasks (named in Gate A §5.3) | historical completed work on active surface | Promote any remaining durable truth into docs/code then remove | Git; freeze path list in §5.3 | delete candidates after per-path extraction; not concurrent cursors; not bulk-by-status | Phase 5 work hygiene |
| `.work/resources/multi-channel-agent-output-proposal.md` | historical consumed proposal | Approved result in product/requirements/architecture | Git | delete candidate after confirm | Phase 5 |
| `.work/resources/text-interaction-controller-proposal.md` | mixed proposal duplicate of planned task | Merge useful content into planned task or mvp-scope | Git | delete after merge | Phase 5 |
| `TODO.md` | non-canonical scratch | Open ideas are not requirements; consolidation item restates this reset | Git | do not treat as spec; optional later hygiene | not a Phase 5 deletion unless Gate A names it |
| `PRODUCT.md`, `DESIGN.md` | generated | `scripts/impeccable_context.py` PRODUCT_SOURCES / DESIGN_SOURCES | adapters + unit tests | regenerate when inputs change; never authority | freeze already green; each rewrite leaf |
| `scripts/check_docs.py`, `scripts/impeccable_context.py`, `scripts/test_impeccable_context.py` | guidance / generated-source validators | Frozen until Phase 4 cutover | tests | keep; replace catalog/ADR/retirement pins at Phase 4 | Phase 4 |
| `.github/workflows/{docs,implementation,architecture-certification}.yml` | ops-evidence / CI | Path-sensitive checks | Git | keep; lint scope may expand at Phase 4 | Phase 4/6 |
| `build/toolchain.json` | mixed: pin facts + ADR-008/010 as `governing` | Current architecture/workspace after cutover | checksums in file | amend governing pointers at Phase 4; do not churn pins | Phase 4 |
| `src/**`, `web/src/**` excluding design-lab, `tests/**`, `contracts/**` | implemented-status | Code and verified tests own implemented truth | tests, migrations, contracts | retain; classify gaps in current-state later | Phase 3 current-state inventory |
| `web/src/design-system/**` | implemented-status of Approved DS | DS contract | tests | retain | Phase 3/6 isolation |
| `web/src/design-lab/**` | ops-evidence / isolated composition; **not** product authority | Lab README + architecture isolation rules | isolation tests | retain as evidence | Phase 3 |
| `database/migrations/up/**` (63 SQL including `0056a`, through `0062`) | immutable | Applied schema history | grate/compatibility tests | **retain-immutable**; ADR tokens non-authoritative provenance | never rewrite for labels |
| Compatibility fixtures/readers (`docs/operations/provider-profiles/*.example.json`, `src/Modules/Sessions/FlexAgent.Sessions.OpenAiCompatible/**`, matching tests) | immutable / ops-evidence | Compatibility and qualification | tests | **retain-immutable** labels; keep current evidence usable | Phase 6 evidence audit |
| `deploy/**`, `build/**` except toolchain governing field, `package.json`, `pnpm-lock.yaml`, `FlexAgent.slnx`, `Directory.*.props`, `global.json`, `nuget.config`, `gitleaks.toml` | ops-evidence / current delivery | Operations/build | CI | retain | proportionate Phase 6 |
| `.playwright-mcp/**` remaining tracked screenshots if any | ops-evidence ephemeral | Git | inspect at freeze: deletions already accepted | do not treat as product authority | n/a |
| Deleted since `ea97a88`: 231 `.playwright-mcp/**` PNGs | historical ephemeral artifacts | Git at `cda9882` | Git blobs | **already-deleted-accepted** (Shipboard commit on `origin/main`); not Gate A | Phase 0 freeze |
| Deleted since `ea97a88`: 24 completed `.work/active/*.md` listed in Phase 0 freeze | historical completed work | Git at `cda9882^` | Git | **already-deleted-accepted**; record so Gate A does not re-approve | Phase 0 freeze |
| Deleted since `ea97a88`: `web/src/components/content/SafeContent.tsx`, `web/src/design-system/components/select/useDismissOnOutsidePointer.ts`, `web/src/design-system/components/state/AcknowledgmentGate.tsx`, `web/src/hooks/useTheme.ts` | implemented-status removed in Shipboard typed wrappers | Replacements in `cda9882` tree | Git + surviving production pages | **already-deleted-accepted** refactor; not unexplained | Phase 0 freeze; do not restore |

### Phase 1 dependency maps

#### Inbound-reference map

Still-binding mechanical pins (must survive until Phase 4 cutover):

- `scripts/check_docs.py`: all 19 feature files, both requirement catalogs
  (membership, order, tier counts 7/2/5/5), `docs/ui-ux/retired-authority.md`
  exists, `docs/ui-ux/README.md` contains `retired-authority.md` and
  `Approved v1.0`, ADR-021 file exists.
- `scripts/impeccable_context.py` `PRODUCT_SOURCES`: product overview,
  concept-model, mvp-scope, `docs/README.md`.
- `scripts/impeccable_context.py` `DESIGN_SOURCES`: DS README,
  implementation-guide, `docs/ui-ux/README.md`, foundation
  colors/typography/layout/radius/shadows/motion/borders, components
  sidebars/layouts/layout-primitives/cards/buttons/alerts/lists/tables/inputs,
  product empty-loading.
- `build/toolchain.json` `governing`: ADR-008, ADR-010.

Human inbound (must migrate before ADR/retirement deletion): **39** Markdown
harness/docs files outside `docs/architecture/decisions/` mention `ADR-00x`
(counts: ADR-002 18, ADR-008 14, ADR-001/003/006/009/012 12–13, others lower;
ADR-021 6). `retired-authority` inbound:
`docs/README.md`, `docs/ui-ux/README.md`,
`docs/ui-ux/activity-campaign-journey.md`, ADR-021.
`change-record` inbound: DS README. No live `docs/current-state.md` link yet.

#### Stable-ID map

Requirement definition pattern `` `REQ|AC-…` — `` in P0 specs (unique counts):
auth-resource-isolation 57, resolved-session-configuration 83, assessment-setup
69, submission-attempts 99, session-text-lifecycle 133, evidence-evaluation 91,
review-result-release 86. Placeholders must not mint colliding IDs. Runtime
contract documents keep their own stable headings/IDs used by code/tests;
Phase 3 must not drop identifiers tests depend on.

#### Validation map

Pre-reset: `python3 scripts/check_docs.py`,
`python3 scripts/impeccable_context.py check`,
`python3 -m unittest discover -s scripts -p 'test_impeccable_context.py'`.
Later: docs CI `.github/workflows/docs.yml`, architecture certification,
implementation workflow, isolation tests, `pnpm verify:*` per Phase 6 matrix.
Historical-authority and path scans are Phase 6, not Phase 1 rewrites.

#### Generated-source map

| Output | Inputs | Frozen generator |
| --- | --- | --- |
| `PRODUCT.md` | PRODUCT_SOURCES | `scripts/impeccable_context.py` |
| `DESIGN.md` | DESIGN_SOURCES | same |

Do not hand-edit adapters. Regenerating is required when a leaf changes those
inputs.

#### Immutable / checksum map

Allowlist (do not rewrite to strip ADR names):
`database/migrations/up/**`; OpenAI-compatible example JSON under
`docs/operations/provider-profiles/`; corresponding
`FlexAgent.Sessions.OpenAiCompatible` modules and tests; other checksummed
supply-chain hashes in `build/toolchain.json` (version pins stay; only
`governing` doc pointers change at Phase 4).

### Phase 1 conflicts (not silently resolved)

Existing open questions in **Decisions and interim defaults** remain: UI flow
filenames (Product/UI/UX Lead, Gate A), Interaction Controller plan priority
(Product Lead, Phase 5), OpenRouter evidence retention (Operations, Gate A),
immutable ADR labels (interim default retain in checksum-sensitive files).
Additional Phase 1 observation: P0 implementation matrices and
`docs/README.md` migration counts disagree with live code (migrations through
`0062`+`0056a`; `web-legacy` still cited in at least one matrix). **Interim
default:** treat code/tests as implemented truth and keep REQ/AC text as
intended truth; queue matrix claims for the derived index rather than picking
one prose source. Owner: documentation/product at Gate A if a matrix sentence
is actually a hidden requirement.

## Approval Gate B record

Copied from independent persist `review-focused-output-02` (producer did not
author the respond). This record is not producer approval. The reset is not
marked complete by this copy.

| Field | Value |
| --- | --- |
| Status | approved (copied from persist; not producer approval) |
| Approved by | Independent TDP reviewer under owner 2026-09-01 delegation (not the producer). Persist `reviewer_binding`: role `reviewer`, kind `reviewer`, provider `cursor`, `session_instance_id` `tdp-session-3f54bb0f8390`, `provider_session_id` `0d3d1b77-5b4d-426a-9754-7940a29c2e83`, generation 1, state `bound` |
| Approved at | persist `review-focused-output-02` revision 7; `lifecycle_status`/`status` `approved`; `review_incomplete` null; no `verification_result` (no required findings; optional findings closed by owner `accept_as_is` without a finding_verification cycle) |
| Focused_output loop id | `review-focused-output-02` |
| Scope | `kind`/`type` `focused_output`; `item_ids` includes `item-0d7215912a29`; `finding_set_id` `review-focused-output-02-fs-02`; `target_revision` 12; `target_digest` `9c69c7231718f4847a87a302d32bc0140205d99eb461e3b4b878160119f22960` |
| Reviewed Git reference | optional-finding workspace checkpoint `a344427f3303448d73237c2156abb7987817547c` (`Keep MD025 on product docs and record the real verify:dotnet skip rationale.`); Phase 6 package commit `b18f013`; freeze parent `cda98826844480770bab7603506cc241638a15f4` |
| Baseline and deletion-manifest decision | Independent reviewer closed the loop as `approved` against the Phase 6 Gate B package in this task (canonical baseline map, Phase 5 deletion manifest, surviving operational/security/compatibility/runtime-audit evidence, non-normative current-state index, work surface of this reset only, validation matrix). No further canonical rewrite or deletion is authorized by this leaf. |
| Validation and independent-review summary | Independent re-run in the reviewer respond confirmed `check_docs.py`, `impeccable_context.py check`, 18 script tests, 21 frontend-isolation-lib tests, `check-frontend-isolation.mjs`, and contracts 8/8. No required/blocking findings. Optional `finding-001` (verify:dotnet skip rationale) and `finding-002` (MD025 scope) are minor; producer `accept_as_is` after `a344427`. In-tree specialist notes remain non-approval. |
| Required follow-up or cleanup | Owner will review the final result. Phase 7 marked this task `completed` and left it on `.work/active/`. Bounded task-file cleanup is subsequent follow-up after mandatory whole-output review. |

### Gate B persist copy (not producer-authored)

- Loop: `review-focused-output-02`, revision 7, `revise_at` `major`.
- Independent discovery respond (producer did not author):
  `temp/tdp-inputs/runs/run-20260831T172348-9b56ac/agent-requests/review-respond-focused-output-r12-a01.json`
  against `target_revision` 12 /
  `9c69c7231718f4847a87a302d32bc0140205d99eb461e3b4b878160119f22960`.
- Persist file:
  `temp/tdp-inputs/runs/run-20260831T172348-9b56ac/reviews/review-focused-output-02.json`
  (`status`/`lifecycle_status` `approved`).
- Optional findings: `finding-001`/`finding-002` (`review-focused-output-02-fs-02`),
  both `minor`, owner-actioned `accept_as_is` at artifact revision 12 after
  workspace commit `a344427`. No remaining unresolved required findings.

## Phase 5 execution record

Recorded 2026-09-01 on `item-d8ff5629ae40`. Recover every path from Git at the
parent of the Phase 5 checkpoint commit. This reset task was not deleted.

### Product Lead decision (Interaction Controller)

No Product Lead reconfirmation occurred in this run. Gate A interim default
applied: deferred Interaction Controller scope stays in product and UI docs;
`.work/active/text-interaction-controller-contract.md` and
`.work/resources/text-interaction-controller-proposal.md` were deleted.

### Recheck dispositions applied

| Path or set | Target | Reason | Retained evidence | Verification |
| --- | --- | --- | --- | --- |
| 12 P1–P3 placeholder specs under `docs/requirements/features/` (Gate A §5.1) | Unique sentences already in `mvp-scope.md`; catalogs name deferred files without links | Not approved requirements | Git; MVP scope deferred bullets including `PROP-AGENT-1` | `check_docs.py` P0-only; no unlisted specs |
| `docs/architecture/decisions/**` (ADR-001–021 + README) | Current architecture/ops/workspace owners after inbound rewrite | Historical alternatives; live constraints already extracted | Git; ADR identity tokens remain in applied migrations listed below | `check_docs.py` links; no live `architecture/decisions` tree |
| `docs/ui-ux/retired-authority.md` | Git `eb9c398` retired versions | Ledger is not current UI authority | Git | UI README has no `retired-authority.md`; `check_docs.py` |
| `docs/ui-ux/design-system/change-record.md` | DS module metadata + Git | Provenance only | Git | DS README structure without the file |
| Six original `docs/ui-ux/{activity-campaign-journey,assessment-campaign-setup,submission-attempt,text-session,evidence-evaluation-human-review,result-release}.md` | Distinct owners under `docs/ui-ux/flows/` | Duplicates of current catalog | Git | Journey links retargeted; `check_docs.py` |
| 52 completed `.work/active` tasks (Gate A §5.3 names) | Durable truth already in specs/code | Historical completed work on the active surface | Git | `.work/active/` is this reset only |
| `.work/active/impeccable-frontend-rebuild.md` | Skills + DS v1.0 | Blocked superseded predecessor | Git | Named file gone |
| `.work/resources/multi-channel-agent-output-proposal.md` | Approved product/requirements/architecture | Absorbed proposal | Git | File gone |
| `TODO.md` | Not a spec | Personal scratch; interaction-controller line is not planned work | Git | File gone |
| Seven OpenRouter `synthetic-development-phase*.md` (Gate A §5.2) | **Retain all seven** | Current qualification evidence; OQ interim default retain-all | Live files + current profile | Files still present |
| Applied `database/migrations/up/**` | **Retain** | Immutable compatibility | Those files | Not edited |
| OpenAI-compatible fixtures/readers | **Retain** | Wire compatibility | contracts/examples | Not edited |

### Immutable ADR labels (not rewritten)

Allowlisted as non-authoritative provenance inside applied migrations (grep
`ADR-0` in `database/migrations/up/`): `0001`, `0002`, `0003`, `0004`,
`0005`, `0022`, `0023`, `0025`. No remaining `per ADR-` / `governed by ADR-`
tokens in `src/` or `contracts/` after Phase 4 comment/schema edits.

### Validators this leaf

- `python3 scripts/check_docs.py` — passed
- `python3 scripts/impeccable_context.py generate` then `check` — passed
- `python3 -m unittest discover -s scripts -p 'test_*.py'` — 18 OK
- Playwright visual verification — not applicable (no UI behavior change)

## Phase 6 execution record and Gate B package

Recorded 2026-09-01 on `item-0d7215912a29` against Git parent `c2f1e16` plus
this checkpoint. **Not independent Gate B approval.** Producer in-tree notes
must not close blocking findings.

Gate B focused_output optional findings (`review-focused-output-02-fs-02`):
workspace commit `a344427` applied the recommended skip-rationale and MD025
scope changes; producer then `accept_as_is` on both minors. Independent persist
`review-focused-output-02` later closed `approved`. This package text is not a
second producer approval.

### Validation matrix (exact commands)

| Check | Result | Evidence |
| --- | --- | --- |
| `python3 scripts/check_docs.py` | passed | exit 0, `Documentation validation passed.` P0 seven files present; no ADR directory; empty placeholder allowlist |
| `python3 scripts/impeccable_context.py generate` then `check` | passed | adapters regenerated after DS `cards.md` lint wording; `Impeccable context adapters are current.` |
| Historical-authority / path / terminology | passed (docs) | `check_docs.py` stale-authority empty allowlist; no `retired-authority.md`; no `binding until phase 4` / `all 19 feature` in `docs/` |
| Requirements integrity | passed | Seven P0 specs; unique `REQ-*`/`AC-*`; deferred names in MVP scope only |
| Markdown lint (CI globs) | passed after Phase 6 fixes; MD025 re-scoped in Gate B optional-finding fix | Default `.markdownlint-cli2.yaml` keeps MD025 enabled; `.work/templates/.markdownlint-cli2.yaml` sets `MD025: false` for the multi-H1 template contract. Re-run 2026-09-01: `npx markdownlint-cli2@0.17.2` on CI globs — `markdownlint-cli2` v0.17.2, 135 files, 0 errors. Phase 6 also fixed MD012 in `auth-resource-isolation.md`, MD004 parse in `cards.md`, MD032/MD047 in impeccable skills |
| Architecture extraction (in-tree) | recorded | Live owners exist (`mvp-architecture.md`, session/evaluation/review contracts, `frontend-architecture.md`, `backend-module-architecture.md`, `docs/operations/README.md`, `docs/contributing/workspace.md`). `docs/architecture/decisions/` absent. Independent persist `review-focused-output-02` approved with no required findings |
| Operational/security/compatibility | recorded | Seven OpenRouter `synthetic-development-phase*.md` present; `database/migrations/up` has 63 SQL files (untouched); contracts tests 8/8 pass; no `per ADR-` in `src/` or `contracts/` |
| Current-state | recorded | `docs/current-state.md` remains non-normative; Interaction Controller deferred; only this reset is active work |
| Work hygiene | passed | `.work/active/` contains only `repository-baseline-reset.md` |
| Skill/rule parity | passed | `.cursor/skills` and `.agents/skills` directory sets match; every `SKILL.md` byte-identical including impeccable |
| UI pattern-adoption governance | passed (guidance present) | `AGENTS.md`, `.cursor/rules/00` and `06`, `implementation-workflow` skill, `frontend-developer` skill, `docs/ui-ux/README.md` require classify-then-clone. No new production UI in this reset |
| Focused script tests | passed | `python3 -m unittest discover -s scripts -p 'test_*.py'` 18 OK; `node --test build/scripts/frontend-isolation-lib.test.mjs` 21 pass |
| Frontend isolation check | passed | `node build/scripts/check-frontend-isolation.mjs` — Frontend isolation check passed |
| `pnpm --dir contracts test` | passed | 8 tests OK |
| `pnpm verify:web` / `pnpm build` | not run (proportionate) | Phase 5/6 changed docs, skills, scripts, `.work`, markdownlint config — not `web/` source. Isolation checks cover the frontend invariant this reset governs |
| `pnpm verify:dotnet` | not run (proportionate split; not a full-suite pass) | Reset-wide `src/` and `contracts/` edits are non-behavioral: Phase 4 `3f85078` dropped a `per ADR-002` comment in `SubscribeAuthorizedSessionEventsCommand.cs` and three `contracts/schemas/v1/digest` schema description strings. No behavioral `src/` or migration change. Phase 6 itself did not edit `src/`. Do not treat this skip as `verify:dotnet` passing |
| `pnpm verify:oidc` / Playwright MCP | not applicable | No routed UI, profile, or authenticated-browser contract change. Do not claim visual verification |

### Reconciliation (in-tree)

Approved product intent remains in concept-model / MVP scope / seven P0 specs.
Architecture and flows are current owners. Design System v1.0 Shipboard remains
the visual baseline. Gate A inventories were executed in Phase 5; surviving
evidence matches the Phase 5 deletion manifest. Known P0 implementation gaps
stay in `docs/current-state.md` and must not be treated as permission to skip
controls.

Non-blocking in-tree observation: `auth-resource-isolation.md` Traceability
still names a `web-legacy` gateway journey in a Playwright/manual cell. That
is implemented-status drift already classified in Gate A / current-state; it
is not a restore instruction.

### In-tree specialist-check notes (not Gate B)

| Concern | Note |
| --- | --- |
| Product / requirements | P0 catalog and deferred-scope absorption look intact; unique IDs unchanged |
| Architecture | Current docs own technical realization; ADR files gone; matrix still needs independent review |
| UI/UX | Flows are the journey catalog; DS v1.0 unchanged except lint wording in `cards.md` |
| Security / privacy | Isolation/deny-by-default language remains in intended specs; no authz code edited |
| Operations | OpenRouter seven phase files retained; default-off unchanged |
| Documentation | Snapshot-first catalogs; adapters regenerated |
| Tester / process | Focused validators recorded; full web/dotnet/OIDC suites intentionally not claimed |
| Repository process | Skill trees parity OK; reset task retained; no second diary created |

### Canonical baseline map (for Gate B)

| Concern | Current owner |
| --- | --- |
| Product meaning | `docs/product/concept-model.md`, `mvp-scope.md`, `overview.md` |
| Observable behavior | Seven P0 files under `docs/requirements/features/` |
| User interaction | `docs/ui-ux/README.md` + `docs/ui-ux/flows/*` + DS v1.0 |
| Technical realization | `docs/architecture/*` excluding deleted `decisions/` |
| Implemented behavior | Code and tests |
| Status index | `docs/current-state.md` (non-normative) |
| Active work | `.work/active/repository-baseline-reset.md` only |
| History | Git |

Deletion manifest: Phase 5 execution record in this task. Surviving
invariants: product runtime auditability, applied migrations, compatibility
fixtures, OpenRouter qualification files, this reset task.

Work surface: this reset is now `completed` and still the only `.work/active`
task; no planned Interaction Controller task.

Independent Gate B persist `review-focused-output-02` is **approved** and
copied above. This Phase 6 package remains producer evidence; it is not a
second approval.

## Phase 7 execution record

Recorded 2026-09-01 on `item-948817441989` after Gate B persist copy
`de09ecafbe3a73b04a1eb39ab153d7e51cc65799`. This leaf does not delete this file
and does not add a reset archive.

### Governing sources rechecked

| Source | Recheck |
| --- | --- |
| `docs/product/overview.md` | Approved current-tense product meaning; vision examples not converted to requirements |
| `docs/product/concept-model.md` | Approved vocabulary and invariants; `PROP-AGENT-1` remains Proposed-bounded |
| `docs/product/mvp-scope.md` | Approved MVP/deferred/non-goal boundaries; Interaction Controller remains deferred |
| `docs/README.md` | Authority-by-concern plus derived current-state route |
| `docs/current-state.md` | Non-normative index; intended vs implemented vs gap/default-off remain distinct |
| Gate B persist | `review-focused-output-02` rev 7 `approved`; copied in Approval Gate B record |

Whole-output review `review-whole-output-01` required findings `sf-001` through
`sf-005` rewrote the residual Phase 3 “not the Phase 4 authority cutover”
sentences on approved product/requirements owners and regenerated `PRODUCT.md`.
Those files now state they are current snapshot-first intended-truth owners;
prior wording remains recoverable in Git. `web-legacy` remains a derived
status-cell citation in Git-era P0 matrices, superseded by live inventory in
`docs/current-state.md`.

### Planned versus actual

| Planned leaf | Actual checkpoint |
| --- | --- |
| Phase 0 freeze | `f1c2d73` |
| Phase 1 classify | `f7d490a` |
| Phase 2 Gate A package | `a60e6c6`; inventories `ceb2b55` |
| Gate A copy-back | `4994076` |
| Phase 3 product/requirements | `6e39d94` |
| Phase 3 architecture | `422efde` |
| Phase 3 UI/UX | `b5bfab6` |
| Phase 3 ops/current-state | `4882193` |
| Phase 4 cutover | `3f85078` |
| Phase 5 deletions | `c2f1e16` |
| Phase 6 Gate B package | `b18f013`; optional-finding fix `a344427` |
| Gate B copy-back | `de09eca` |
| Phase 7 complete task | this checkpoint; file remains `.work/active/repository-baseline-reset.md` |

Durable truth already lives in approved product, requirements, architecture,
UI/UX, operations, `docs/current-state.md`, code/tests, and Git. This completed
task remains the explicit transitional exception until post-review cleanup.
The derived index row for this reset was updated to `completed` (retained);
no second plan or archive was created.

### Whole-output required-finding wording fix

Recorded 2026-09-01 for `review-whole-output-01` required findings `sf-001`
through `sf-005` (optional `sf-006` included because it is the same task-file
cursor sentence). Product meaning, REQ/AC identifiers, and MVP bounds are
unchanged.

Verification:

| Command | Result |
| --- | --- |
| `python3 scripts/impeccable_context.py generate` | Wrote `PRODUCT.md` and `DESIGN.md`; `DESIGN.md` unchanged |
| `python3 scripts/impeccable_context.py check` | Impeccable context adapters are current |
| `python3 scripts/check_docs.py` | Documentation validation passed |

Workspace search after the rewrite found no remaining “not the Phase 4
authority cutover” claim in canonical product/requirements sources or
`PRODUCT.md`.

# Current state

Phase 0 freeze is recorded on 2026-09-01 against live Git, not the 2026-08-31
readiness snapshot. See **Phase 0 freeze record**.

Freeze facts (Phase 0 record; not current cursor status): activation/freeze
parent branch `main`, `cda98826844480770bab7603506cc241638a15f4`, equal to
`origin/main`. The only dirty path before the Phase 0 checkpoint commit was
this task file. Concurrent execution cursors were completed,
blocked-superseded, or planned; at freeze this reset was the only
`in-progress` cursor. Deletions since `ea97a88` (259 paths, all in `cda9882`)
are separately accepted Shipboard work with Git recovery, not an uncommitted
deletion set.

Design System v1.0 and the 2026-08-31 Shipboard owner visual pass remain the
approved UI baseline. Pre-reset `check_docs.py`, `impeccable_context.py check`,
and 15 adapter unit tests passed on this freeze.

Current state: this retained reset task is `completed` at
`.work/active/repository-baseline-reset.md`, not in-progress. Bounded file
removal is subsequent follow-up after mandatory whole-output review. Do not
delete this task in this graph. Gate B persist `review-focused-output-02` is
copied; producer did not approve.

### Queued P0 implementation-matrix claims (not requirements)

Removed from seven P0 Traceability tables on the product leaf. Full
Implementation cells remain in Git at
`4994076862e088bbc1ea25436ab2a6b95dfdb704`. Phase 3 operations materialized
those Status values as derived links in `docs/current-state.md` (70 rows),
not by rewriting P0 requirements.

| Spec | Rows queued | Prior Status values |
| --- | --- | --- |
| `auth-resource-isolation.md` | 15 | Partial (3), Gap (12) |
| `resolved-session-configuration.md` | 9 | Partial (3), Gap (6) |
| `assessment-setup.md` | 9 | Implemented (1), Partial (8) |
| `submission-attempts.md` | 8 | Partial (7), Gap (1) |
| `session-text-lifecycle.md` | 9 | Partial (8), Gap (1) |
| `evidence-evaluation.md` | 10 | Gap (10) |
| `review-result-release.md` | 10 | Gap (10) |

Do not treat those Status values as intended product meaning.

# Decisions and interim defaults

- Use one task file for the reset; do not create phase plans, migration diaries,
  or a documentation-history archive.
- Preserve stable requirement/acceptance and runtime contract IDs when code and
  tests depend on them. Snapshot-first removes historical prose, not useful
  identifiers or compatibility contracts.
- Prefer direct current-state statements with rationale where rationale is a
  live correctness constraint.
- **Open question - final UI/UX flow filenames. Decision owner:** Product/UI/UX
  Lead. Gate A persist `review-focused-output-01` approved the package that
  records this working default; it did not invent new filenames. **Interim
  default:** place each approved representative journey under
  `docs/ui-ux/flows/` while keeping distinct documents for distinct owners,
  stable traceability, or review boundaries; move shared application
  architecture into `docs/ui-ux/README.md`. Rationale: minimizes overlap
  without forcing an arbitrary file-count target.
- **Open question - Interaction Controller plan priority. Decision (Phase 5):**
  No Product Lead reconfirmation in this run. Applied Gate A interim default:
  deferred scope remains in `docs/product/mvp-scope.md`,
  `docs/product/concept-model.md`, and `docs/ui-ux/README.md`; the planned
  task and proposal resource were removed. The controller is not implemented.
- **Open question - current OpenRouter evidence retention. Decision owner:**
  Operations/Architecture Lead. Gate A persist accepted §5.2 named-file
  working classification: retain-until-Phase-3/5 recheck for each of the
  seven `synthetic-development-phase*.md` files. **Interim default:** retain
  every current machine-verifiable or human-readable record needed to
  reproduce, qualify, secure, or audit the active profile; remove only
  narrative history proven redundant after the canonical current profile and
  evidence index are complete. Rationale: Git history alone is insufficient
  for evidence needed by present operational gates.
- **Open question - immutable ADR labels. Interim default:** allow ADR tokens
  only inside checksum-sensitive applied migrations/fixtures and exact wire
  compatibility examples, explicitly non-authoritative. Rationale: rewriting
  immutable artifacts creates more risk than the label.

# Validation

| Check | Status | Evidence required after execution |
| --- | --- | --- |
| Pre-reset documentation baseline: `python3 scripts/check_docs.py` | passed Phase 0 freeze | 2026-09-01 freeze: exit 0, `Documentation validation passed.` |
| New documentation validator and link/fragment scan | passed Phase 6 | Re-run 2026-09-01: `python3 scripts/check_docs.py` exit 0 after lint-driven adapter regenerate |
| Generated adapter consistency | passed Phase 6 | `impeccable_context.py generate` then `check` exit 0 |
| Historical-authority scan | passed Phase 6 | `check_docs.py` stale-authority empty allowlist; no live ADR catalog/retirement/change-record files |
| Path and terminology scan | passed Phase 6 (docs) | `check_docs.py` link scan exit 0; immutable ADR tokens remain in migrations listed in Phase 5 |
| Requirements integrity | passed Phase 6 | Seven P0 specs; unique `REQ-*`/`AC-*`; no placeholder files |
| Work hygiene | passed Phase 6 | `.work/active/` contains only this reset task |
| Focused script tests | passed Phase 6 | 18 Python script tests OK; 21 frontend-isolation-lib tests OK |
| Markdown lint | passed Phase 6; MD025 re-scoped after Gate B optional finding | Re-run after scoped config: CI globs 135 files, 0 errors (`markdownlint-cli2` v0.17.2). MD025 remains enabled in the default config; only `.work/templates/.markdownlint-cli2.yaml` disables it for the multi-H1 template contract |
| Architecture extraction audit | recorded Phase 6; Gate B persist approved | Live owners present; ADR directory absent; independent persist `review-focused-output-02` approved with no required findings |
| Operational/security/compatibility evidence audit | recorded Phase 6; Gate B persist approved | Seven OpenRouter phase files; 63 applied up-migrations; contracts 8/8; isolation check passed |
| Current-state audit | recorded Phase 6; Gate B persist approved | Index still non-normative; gaps/default-off/deferred controller honest |
| Skill/rule parity | passed Phase 6 | Cursor/Agents skill dirs and SKILL.md bytes match |
| UI pattern-adoption governance | passed Phase 6 | Classify-then-clone language in AGENTS, rules 00/06, implementation-workflow, frontend-developer, UI README |
| Frontend verification | proportionate passed | Isolation lib tests + `check-frontend-isolation.mjs` passed; full `pnpm verify:web` not run (no `web/` source change) |
| .NET/architecture/contract verification | proportionate | `pnpm --dir contracts test` 8/8; `pnpm verify:dotnet` omitted as a proportionate split because the only reset-wide `src/`/`contracts/` edits are non-behavioral comment/description token removals in `3f85078`, not because `src/` was unchanged. Not a full-suite pass |
| Build and delivery checks | proportionate not run | No `web/` build graph change; isolation check is the delivery invariant for this reset |
| Authenticated browser verification | not applicable | No routed UI / OIDC / profile contract change; Playwright screenshots not claimed |
| Delegated Gate A | passed 2026-09-01 | Copied persist `review-focused-output-01` rev 11: `approved` / verification `verified`; scope `item-48819dbb36d9`; loop id `review-focused-output-01`; reviewer `tdp-session-7a472fad0d97`; reviewed Git `ceb2b5565e31744c2daef6bdd2a09945a24a7cea`. Producer did not author the respond. |
| Independent cross-concern review | passed 2026-09-01 via Gate B persist | Persist `review-focused-output-02` approved covering `item-0d7215912a29`; no required/blocking findings; two optional minors `accept_as_is` after `a344427`. In-tree notes remain non-approval. |
| Delegated Gate B | passed 2026-09-01 | Copied persist `review-focused-output-02` rev 7: `approved`; scope `item-0d7215912a29`; loop id `review-focused-output-02`; reviewer `tdp-session-3f54bb0f8390`; reviewed Git `a344427f3303448d73237c2156abb7987817547c`. Producer did not author the respond. |

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
- Cleared Phase 1: classification/deletion manifest and dependency maps recorded
  in this task; mixed sources have extraction targets; already-deleted `cda9882`
  paths have accepted dispositions.
- Cleared Phase 2: Gate A package recorded (target model, extraction matrix,
  deletion migration requirements, immutable exceptions, gaps, rewrite
  sequence, rollback). 2026-09-01 review revision added Gate A §5.1–§5.3
  named inventories.
- Cleared delegated Gate A: independent focused_output persist
  `review-focused-output-01` is `approved` (verification `verified`); copied
  into this task. Producer did not author the respond. Blocking findings
  resolved or invalid. Canonical sources unchanged by the copy-back.
- Cleared delegated Gate B: independent focused_output persist
  `review-focused-output-02` is `approved`; copied into this task. Producer did
  not author the respond. No required/blocking findings. Optional minors
  `accept_as_is` after `a344427`. Canonical sources unchanged by the copy-back.
- Cleared Phase 7 completion: task `status` is `completed`; planned work
  reconciled to checkpoint commits; governing product sources rechecked;
  Gate B persist remains copied. File left at
  `.work/active/repository-baseline-reset.md`. No reset archive created.
- Current execution blocker: none. Next leaf records that bounded cleanup of
  this completed task is subsequent follow-up (`item-881e9eb175f1`), not an
  in-graph deletion.

# Completion

- [x] One coherent current product baseline exists without historical
  reconstruction.
- [x] Canonical architecture, API/data/runtime boundaries, and operational
  constraints are clear without ADR lookup.
- [x] Canonical application UX, Design System, and Design Lab/reference-flow
  authority is clear and approved.
- [x] Feature delivery follows UX architecture and approved patterns before
  production vertical slices.
- [x] Every new or meaningfully changed page/component is classified before
  implementation; existing Design System modules, accepted production-page
  compositions, and Component Deck specimens are cloned and adapted, with Lab
  journeys used only for families without production donors, while genuine
  gaps follow the bounded Impeccable proposal, approval, and shared-pattern
  establishment path.
- [x] Important rationale survives as current constraints/invariants; runtime
  product auditability and immutable compatibility artifacts are preserved.
- [x] Historical decision, retirement, change-record, phase-diary, placeholder,
  and supersession maintenance is removed from normal development.
- [x] Only active and genuinely planned work remains, except this reset task's
  explicit Gate-B-to-cleanup transition; completed/cancelled/blocked/
  superseded plans and duplicate resources are removed.
- [x] `docs/current-state.md` honestly distinguishes intended, implemented,
  temporary legacy, approved planned, default-off, and gap behavior.
- [x] All deleted-path references, stale terminology, and contradictory
  authority claims are resolved.
- [x] Documentation, formatting, tests, builds, architecture/isolation checks,
  and applicable delivery validation pass with recorded evidence.
- [x] Independent cross-concern review is complete and blocking findings are
  resolved.
- [x] Planned work is reconciled with actual changes and governing product
  sources are rechecked.
- [x] This task's durable truth is promoted, Gate B accepts the reset, and the
  subsequent bounded cleanup change is recorded. Actual removal is follow-up
  cleanup, not a condition for marking this reset completed; Git retains the
  history.
