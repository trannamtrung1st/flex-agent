# Current state

Derived, **non-normative** status index for Flex Agent. Reviewed **2026-09-03**.

This file owns **classification only**. It must not restate or override product
meaning, `REQ-*`/`AC-*` contracts, UI/UX journeys, architecture documents, or
runbooks. If it disagrees with an owning source, the owner wins and this index
is stale.

| Classification | Meaning | Follow |
| --- | --- | --- |
| Intended | Approved current product/requirements/UI/UX/architecture meaning | Owning spec |
| Implemented | Behavior present in code and verified tests | Code/tests |
| Temporary legacy | Shipped or harness behavior that must not widen intended scope | Code + intended spec |
| Approved planned | Active `.work/active/` work that is genuinely in progress | Task file |
| Default-off | Implemented path that must not run until an operator gate enables it | Operations + config |
| Gap | Intended in P0 (or named) and not implemented | Owning spec; do not invent behavior |

Status: **Approved** as a derived index. Current governance and catalogs apply
the snapshot-first model. Git owns prior document versions and deleted
placeholder scaffolds.

## How to read P0 implementation rows

The product leaf removed volatile Implementation/Status columns from seven P0
Traceability tables. Those **Status** values are materialized below as derived
links only. Full Implementation cells remain in Git at
`4994076862e088bbc1ea25436ab2a6b95dfdb704`. They are **not** requirements.

Live modules and tests in the inventory below supersede a stale matrix cell
when they disagree (for example citations of `web-legacy`).

| Owning spec | Requirement/AC group (from Git snapshot) | Derived status |
| --- | --- | --- |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-1`, `REQ-AUTH-2`, `AC-AUTH-1`, `AC-AUTH-13` | Partial |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-3`–`REQ-AUTH-6`, `AC-AUTH-4`, `AC-AUTH-5`, `AC-AUTH-10` | Gap |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-7`, `REQ-AUTH-8`, `REQ-AUTH-24`, `AC-AUTH-2`, `AC-AUTH-3`, `AC-AUTH-19` | Gap |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-9`, `REQ-AUTH-10`, `AC-AUTH-5`, `AC-AUTH-6` | Gap |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-11`, `REQ-AUTH-15`, `AC-AUTH-16` | Partial |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-12`–`REQ-AUTH-14`, `AC-AUTH-7`, `AC-AUTH-8` | Gap |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-16`, `REQ-AUTH-17`, `AC-AUTH-9` | Gap |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-18`–`REQ-AUTH-20`, `AC-AUTH-11`, `AC-AUTH-12`, `AC-AUTH-17` | Gap |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-21`, `REQ-AUTH-22`, `AC-AUTH-3`, `AC-AUTH-13` | Gap |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-23`, `AC-AUTH-18` | Gap |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-25`, `AC-AUTH-4`, `AC-AUTH-7`, `AC-AUTH-8`, `AC-AUTH-16`, `AC-AUTH-19` | Gap |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-26`–`REQ-AUTH-29`, `REQ-AUTH-31`, `REQ-AUTH-33`, `AC-AUTH-14`, `AC-AUTH-15`, `AC-AUTH-22`, `AC-AUTH-24` | Gap |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-32`, `AC-AUTH-23` | Gap |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | `REQ-AUTH-30`, `AC-AUTH-21` | Partial |
| [`auth-resource-isolation.md`](requirements/features/auth-resource-isolation.md) | UX and accessibility requirements, `AC-AUTH-20` | Gap |
| [`resolved-session-configuration.md`](requirements/features/resolved-session-configuration.md) | `REQ-RSC-1`–`REQ-RSC-8`, `AC-RSC-1`–`AC-RSC-5` | Partial |
| [`resolved-session-configuration.md`](requirements/features/resolved-session-configuration.md) | `REQ-RSC-9`–`REQ-RSC-14`, `AC-RSC-6`, `AC-RSC-8`, `AC-RSC-24` | Gap |
| [`resolved-session-configuration.md`](requirements/features/resolved-session-configuration.md) | `REQ-RSC-15`–`REQ-RSC-22`, `AC-RSC-3`, `AC-RSC-7`, `AC-RSC-11`, `AC-RSC-12` | Gap |
| [`resolved-session-configuration.md`](requirements/features/resolved-session-configuration.md) | `REQ-RSC-23`–`REQ-RSC-28`, `AC-RSC-10`, `AC-RSC-11`, `AC-RSC-13`, `AC-RSC-16` | Gap |
| [`resolved-session-configuration.md`](requirements/features/resolved-session-configuration.md) | `REQ-RSC-29`–`REQ-RSC-37`, `AC-RSC-9`, `AC-RSC-14`, `AC-RSC-15`, `AC-RSC-17`, `AC-RSC-23` | Partial |
| [`resolved-session-configuration.md`](requirements/features/resolved-session-configuration.md) | `REQ-RSC-46`, `AC-RSC-25` | Partial |
| [`resolved-session-configuration.md`](requirements/features/resolved-session-configuration.md) | `REQ-RSC-47`–`REQ-RSC-55`, `AC-RSC-26`–`AC-RSC-28` | Partial |
| [`resolved-session-configuration.md`](requirements/features/resolved-session-configuration.md) | `REQ-RSC-38`–`REQ-RSC-45`, `AC-RSC-18`–`AC-RSC-22` | Gap |
| [`resolved-session-configuration.md`](requirements/features/resolved-session-configuration.md) | Quality and observability requirements, `AC-RSC-22` | Gap |
| [`assessment-setup.md`](requirements/features/assessment-setup.md) | `REQ-ACT-1`–`REQ-ACT-8`, `AC-ACT-1`–`AC-ACT-5`, `AC-ACT-18` | Implemented |
| [`assessment-setup.md`](requirements/features/assessment-setup.md) | `REQ-ACT-9`–`REQ-ACT-13`, `AC-ACT-4`, `AC-ACT-6` | Partial |
| [`assessment-setup.md`](requirements/features/assessment-setup.md) | `REQ-ACT-14`–`REQ-ACT-24`, `REQ-ACT-41`, `REQ-ACT-42`, `AC-ACT-7`, `AC-ACT-8`, `AC-ACT-13`–`AC-ACT-17`, `AC-ACT-25` | Partial |
| [`assessment-setup.md`](requirements/features/assessment-setup.md) | `REQ-ACT-25`, `REQ-ACT-31`–`REQ-ACT-34`, `AC-ACT-12`, `AC-ACT-19`, `AC-ACT-23` | Partial |
| [`assessment-setup.md`](requirements/features/assessment-setup.md) | `REQ-ACT-26`–`REQ-ACT-30`, `AC-ACT-9`–`AC-ACT-11`, `AC-ACT-26` | Partial |
| [`assessment-setup.md`](requirements/features/assessment-setup.md) | `REQ-ACT-35`–`REQ-ACT-40`, `AC-ACT-17`, `AC-ACT-20`, `AC-ACT-21` | Partial |
| [`assessment-setup.md`](requirements/features/assessment-setup.md) | UX/accessibility requirements, `AC-ACT-22` | Partial |
| [`assessment-setup.md`](requirements/features/assessment-setup.md) | Performance/reliability requirements, `AC-ACT-15`–`AC-ACT-18`, `AC-ACT-27`, `PROP-4` | Partial |
| [`assessment-setup.md`](requirements/features/assessment-setup.md) | Security/privacy requirements, `AC-ACT-3`, `AC-ACT-5`, `AC-ACT-21`, `AC-ACT-23`–`AC-ACT-25` | Partial |
| [`submission-attempts.md`](requirements/features/submission-attempts.md) | `REQ-SUBM-1`–`REQ-SUBM-8`, `REQ-SUBM-43`, `AC-SUBM-1`–`AC-SUBM-4`, `AC-SUBM-29`, `PROP-6` | Partial |
| [`submission-attempts.md`](requirements/features/submission-attempts.md) | `REQ-SUBM-9`–`REQ-SUBM-14`, `REQ-SUBM-50`–`REQ-SUBM-56`, `AC-SUBM-11`, `AC-SUBM-12`, `AC-SUBM-22`, `AC-SUBM-33`–`AC-SUBM-39`, `PROP-3`, `PROP-9`–`PROP-15` | Partial |
| [`submission-attempts.md`](requirements/features/submission-attempts.md) | `REQ-SUBM-15`–`REQ-SUBM-23`, `AC-SUBM-5`–`AC-SUBM-10`, `PROP-1` | Partial |
| [`submission-attempts.md`](requirements/features/submission-attempts.md) | `REQ-SUBM-24`–`REQ-SUBM-35`, `REQ-SUBM-44`–`REQ-SUBM-48`, `AC-SUBM-13`–`AC-SUBM-18`, `AC-SUBM-30`–`AC-SUBM-32`, `PROP-2`, `PROP-4` | Partial |
| [`submission-attempts.md`](requirements/features/submission-attempts.md) | `REQ-SUBM-36`–`REQ-SUBM-42`, `REQ-SUBM-49`, `AC-SUBM-19`–`AC-SUBM-21`, `AC-SUBM-24`–`AC-SUBM-26`, `PROP-7` | Partial |
| [`submission-attempts.md`](requirements/features/submission-attempts.md) | UX/accessibility requirements, `AC-SUBM-23` | Partial |
| [`submission-attempts.md`](requirements/features/submission-attempts.md) | Performance/reliability requirements, `REQ-SUBM-57`–`REQ-SUBM-58`, `AC-SUBM-9`, `AC-SUBM-17`, `AC-SUBM-27`, `AC-SUBM-40`–`AC-SUBM-41`, `PROP-5`, `PROP-8` | Partial |
| [`submission-attempts.md`](requirements/features/submission-attempts.md) | Security/privacy requirements, `AC-SUBM-14`, `AC-SUBM-18`–`AC-SUBM-21`, `AC-SUBM-26`, `AC-SUBM-28` | Partial |
| [`session-text-lifecycle.md`](requirements/features/session-text-lifecycle.md) | `REQ-SESS-1`–`REQ-SESS-7`, `AC-SESS-1`, `AC-SESS-2` | Partial |
| [`session-text-lifecycle.md`](requirements/features/session-text-lifecycle.md) | `REQ-SESS-8`–`REQ-SESS-19`, `REQ-SESS-51`–`REQ-SESS-60`, `AC-SESS-3`–`AC-SESS-8`, `AC-SESS-31`, `AC-SESS-32`, `PROP-7` | Partial |
| [`session-text-lifecycle.md`](requirements/features/session-text-lifecycle.md) | `REQ-SESS-61`–`REQ-SESS-85`, `AC-SESS-33`–`AC-SESS-48` | Partial |
| [`session-text-lifecycle.md`](requirements/features/session-text-lifecycle.md) | `REQ-SESS-20`–`REQ-SESS-30`, `AC-SESS-9`–`AC-SESS-14`, `PROP-2`, `PROP-3`, `PROP-6` | Partial |
| [`session-text-lifecycle.md`](requirements/features/session-text-lifecycle.md) | `REQ-SESS-31`–`REQ-SESS-41`, `AC-SESS-15`–`AC-SESS-20`, `PROP-5` | Partial |
| [`session-text-lifecycle.md`](requirements/features/session-text-lifecycle.md) | `REQ-SESS-42`–`REQ-SESS-50`, `AC-SESS-21`–`AC-SESS-23`, `AC-SESS-28`–`AC-SESS-30`, `PROP-1`, `PROP-8` | Partial |
| [`session-text-lifecycle.md`](requirements/features/session-text-lifecycle.md) | UX/accessibility requirements, `AC-SESS-24`–`AC-SESS-26`, `AC-SESS-31`, `AC-SESS-32` | Partial |
| [`session-text-lifecycle.md`](requirements/features/session-text-lifecycle.md) | Performance/reliability requirements, `AC-SESS-6`, `AC-SESS-9`–`AC-SESS-11`, `AC-SESS-27`, `PROP-4` | Partial |
| [`session-text-lifecycle.md`](requirements/features/session-text-lifecycle.md) | Security/privacy requirements, `AC-SESS-8`, `AC-SESS-13`, `AC-SESS-14`, `AC-SESS-23`, `AC-SESS-26`, `AC-SESS-29` | Partial |
| [`evidence-evaluation.md`](requirements/features/evidence-evaluation.md) | `REQ-EVAL-1`–`REQ-EVAL-7`, `AC-EVAL-1`–`AC-EVAL-5` | Gap |
| [`evidence-evaluation.md`](requirements/features/evidence-evaluation.md) | `REQ-EVAL-8`–`REQ-EVAL-17`, `AC-EVAL-5`–`AC-EVAL-8`, `PROP-2` | Gap |
| [`evidence-evaluation.md`](requirements/features/evidence-evaluation.md) | `REQ-EVAL-18`–`REQ-EVAL-28`, `AC-EVAL-9`–`AC-EVAL-14`, `AC-EVAL-20`, `PROP-1`, `PROP-5` | Gap |
| [`evidence-evaluation.md`](requirements/features/evidence-evaluation.md) | `REQ-EVAL-47`–`REQ-EVAL-53`, `AC-EVAL-32`–`AC-EVAL-38`, `PROP-8` | Gap |
| [`evidence-evaluation.md`](requirements/features/evidence-evaluation.md) | `REQ-EVAL-29`–`REQ-EVAL-36`, `AC-EVAL-15`–`AC-EVAL-19`, `AC-EVAL-27`, `PROP-4` | Gap |
| [`evidence-evaluation.md`](requirements/features/evidence-evaluation.md) | `REQ-EVAL-37`–`REQ-EVAL-46`, `AC-EVAL-21`–`AC-EVAL-27`, `AC-EVAL-30` | Gap |
| [`evidence-evaluation.md`](requirements/features/evidence-evaluation.md) | UX requirements, `AC-EVAL-22`, `AC-EVAL-28` | Gap |
| [`evidence-evaluation.md`](requirements/features/evidence-evaluation.md) | Performance requirements, `AC-EVAL-29`, `PROP-6` | Gap |
| [`evidence-evaluation.md`](requirements/features/evidence-evaluation.md) | `AC-EVAL-31`, `AC-EVAL-36` | Gap |
| [`evidence-evaluation.md`](requirements/features/evidence-evaluation.md) | Downstream boundary, `REQ-EVAL-28`, `AC-EVAL-20`, `PROP-3`, `PROP-4` | Gap |
| [`review-result-release.md`](requirements/features/review-result-release.md) | `REQ-REV-1`–`REQ-REV-8`, `AC-REV-1`–`AC-REV-4`, `PROP-1` | Gap |
| [`review-result-release.md`](requirements/features/review-result-release.md) | `REQ-REV-9`–`REQ-REV-18`, `AC-REV-5`–`AC-REV-9`, `PROP-2` | Gap |
| [`review-result-release.md`](requirements/features/review-result-release.md) | `REQ-REV-19`–`REQ-REV-26`, `AC-REV-6`, `AC-REV-10`–`AC-REV-15` | Gap |
| [`review-result-release.md`](requirements/features/review-result-release.md) | `REQ-REL-1`–`REQ-REL-5`, `AC-REL-1`, `AC-REL-2`, `PROP-3` | Gap |
| [`review-result-release.md`](requirements/features/review-result-release.md) | `REQ-REL-6`–`REQ-REL-13`, `AC-REL-3`–`AC-REL-12`, `PROP-4`, `PROP-5` | Gap |
| [`review-result-release.md`](requirements/features/review-result-release.md) | `REQ-REL-14`–`REQ-REL-16`, `AC-REL-13`–`AC-REL-15`, `PROP-6` | Gap |
| [`review-result-release.md`](requirements/features/review-result-release.md) | `REQ-REV-27`–`REQ-REV-35`, `AC-REV-12`, `AC-REV-15`, `AC-REV-16`, `AC-REV-18`–`AC-REV-20`, `PROP-10` | Gap |
| [`review-result-release.md`](requirements/features/review-result-release.md) | UX requirements, `AC-REL-10`–`AC-REL-12`, `PROP-9` | Gap |
| [`review-result-release.md`](requirements/features/review-result-release.md) | Performance requirements, `AC-REV-17`, `PROP-8` | Gap |
| [`review-result-release.md`](requirements/features/review-result-release.md) | Upstream/downstream end-to-end boundary | Gap |

## Live inventory (2026-09-01)

Counts and paths are evidence pointers, not contracts.

### Backend modules

`src/Modules/`: `AssessmentConfiguration`, `Configuration`, `IdentityAccess`,
`Sessions`, `Submissions`, `SyntheticBrowser`. There is no Evaluation, Review,
or Release host module in this tree.

Related hosts and infrastructure: `src/Hosts/`, `src/Infrastructure/`,
`src/BuildingBlocks/`. Session OpenAI-compatible adapter:
`src/Modules/Sessions/FlexAgent.Sessions.OpenAiCompatible` (compatibility
fixture; default-off).

### Persistence

`database/migrations/up/`: 63 SQL files through `0062` plus additive `0056a`.
Applied migrations are immutable; labels inside them are provenance only.

### Production UI routes and pages

Router: `web/src/router/production-routes.tsx`. Locators present: `/`,
`/activities`, `/activities/new`, `/activities/:activityId/setup`, cohort
Enrollment paths, `/my-work`, `/my-work/:enrollmentId`, `/sessions/:sessionId`,
`/review`, `/review/:reviewId`, `/release`, `/release/:resultId`, `/results`,
`/results/:resultId`. Presence of a locator is not proof the host contract is
complete (`IA-MVP-3`).

Pages: `ProductionAuthGatePage`, `ProductionHomePage`,
`ProductionActivitiesPage`, `ProductionCampaignCreatePage`,
`ProductionAssessmentSetupRoute`, `ProductionEnrollmentPage`,
`ProductionEnrollmentDetailPage`, `ProductionMyWorkPage`,
`ProductionMyWorkDetailPage`, `ProductionTextSessionPage`. Review/Release/Results
locators may still be unavailable shells.

Design Lab: isolated under `web/src/design-lab/` and `/design-lab/*`. Not
product authority.

### Tests and contracts

- `tests/Architecture`, `tests/AssessmentConfiguration`, `tests/Browser`
  (`FlexAgent.Oidc.Playwright`), `tests/CanonicalJson`, `tests/Contract`,
  `tests/Integration` (Artifact, Keycloak, Postgres), `tests/Runtime`,
  `tests/Sessions`, `tests/Submissions`
- `contracts/tests/` OpenAPI/JCS checks
- Root gates: `dotnet test`, `pnpm verify:web`, `bash build/scripts/verify-oci.sh`,
  `python3 scripts/check_docs.py`, `pnpm verify:oidc`

### Operations gates

See [operations](operations/README.md). Model adapter **default-off**. Keycloak
OIDC is local/CI qualification, not Production enablement. OpenRouter
synthetic pin is not production qualification.

## Cross-cutting classifications

| Area | Classification | Owner / evidence |
| --- | --- | --- |
| Product meaning and MVP boundary | Intended | [Concept model](product/concept-model.md), [MVP scope](product/mvp-scope.md), [overview](product/overview.md) |
| P0 `REQ-*`/`AC-*` | Intended | Seven P0 specs under [features](requirements/features/README.md) |
| P1–P3 deferred capability names | Deferred (not requirements) | Named in [MVP scope](product/mvp-scope.md); no placeholder spec files |
| Application UX architecture | Intended (Approved v1.0 journeys) | [UI/UX](ui-ux/README.md); [flows](ui-ux/flows/activity-campaign-journey.md) |
| Design System v1.1 | Intended visual contract | [Design system](ui-ux/design-system/README.md) |
| Architecture and code contracts | Intended | [Architecture](architecture/README.md) |
| OIDC application session, scoped API, Worker identity | Partial implemented | `IdentityAccess`; Keycloak integration tests; remaining AUTH matrix rows are gaps |
| Assessment Campaign draft/setup UI | Partial implemented | `AssessmentConfiguration`; production setup pages |
| Activities server-numbered paging and capability-aware table selection | Partial implemented | `REQ-ACT-43`–`REQ-ACT-46`, `UI-ACT-DEC-7`, `DS-DEC-12`–`DS-DEC-13`; numbered Activities list and page/matching table selection are in code and tests |
| Enrollment assignment / My work | Partial implemented | `Submissions`; production enrollment and My work pages |
| Submission intake / Attempt start | Partial implemented | Development atomic start, readiness, durable exact acknowledgments (`current_outcome` vs bindable), history, reconciliation, and **Continue Attempt** locator are complete and reviewed on `ec84274` ([Implementation 33703247493](https://github.com/trannamtrung1st/flex-agent/actions/runs/33703247493)); Production/Staging remain fail-closed; beyond-baseline retry grant (`REQ-SUBM-21`) remains a gap |
| Hosted Session start/command/snapshot; e2e production Session | Partial implemented | Authenticated host snapshot/command/events and production `/sessions/:sessionId` live-session, `/operations`, `/transcript` landed; frozen timing at Attempt start (`0069`); Worker `IHostedSessionExpirySweep` on authenticated-browser Compose (Development `deterministic_fake` only). Production/Staging Worker stay fail-closed. Design-system `LiveSessionLayout` and work `StageBars` are production donors. Core timing + Implementation CI closed (`888eb91` / `33743544924`; `b24f67c` / `33754337758`; `920596e` / `33763594004`). **Running-Worker expiry-loop proof:** corrected locally 2026-09-03 after `920596e` review (`probe-compose-hosted-expiry-sweep.sh` green; Session `01a067ae-…d41a6a` → `completed` / `time_expiry`); env-gated probe skipped in CI — see `.work/active/hosted-text-session.md`. Remaining: QA matrix, specialist reviews, task completion |
| Evaluation, Human review, Result, Release hosts | Gap | Intended in P0; no host modules |
| Agent/Harness library authoring | Not implemented | Named deferred P1 scope; not MVP requirements |
| Voice, tools, Dynamic memory, shared Sessions | Deferred | Placeholders are not requirements |
| Interaction Controller | Deferred | Product docs; planned (not activated) [text-interaction-controller-contract.md](../.work/active/text-interaction-controller-contract.md) |
| Home `/` redirect to `/my-work` when My work exists | Temporary legacy vs `IA-MVP-1` target | Production router; intended Home feed still future |
| Dual-build `web-legacy` | Historical only | Removed; do not restore. Owner: [frontend architecture](architecture/frontend-architecture.md) |
| Compose SPA vs `web/` source lag | Temporary legacy | Prefer Vite `:5274` for source UI evidence |
| Model execution adapter | Default-off | [Provider profiles](operations/provider-profiles/README.md) |
| OpenRouter synthetic pin | Default-off / qualified synthetic only | [OpenRouter profile](operations/provider-profiles/openrouter-synthetic-development.md) |
| Other `.work/active/*` | Mixed planned/completed review state | Interaction Controller is planned; completed pagination plans are retired from `.work/active` |

## Active work

`.work/active/hosted-text-session.md` is `in-progress` (premature retirement in
`920596e` reverted; running-Worker probe corrected locally; QA matrix and
specialist reviews still open). The Attempt-start predecessor is retired from
`.work/active` (recover from Git).
`.work/active/text-interaction-controller-contract.md` is `planned` and not
activated. Completed Participants cursor-pager, server-numbered pagination,
reset, harness-correction, and Attempt-start records are retired from
`.work/active`; recover them from Git.
