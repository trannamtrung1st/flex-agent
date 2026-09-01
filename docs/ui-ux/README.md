# UI/UX documentation

Application-level user experience architecture for Flex Agent: information
architecture, shell, navigation, page archetypes, shared interaction states,
accessibility and responsive baselines, and the catalog of journey owners.

Product meaning and scope live under [product documentation](../product/README.md).
UI/UX documents implement and extend approved requirements; they do not override
acceptance criteria or redefine canonical concepts.

## Status

**Replacement P0 journey and interaction specifications are Approved v1.0.**
This README is **Approved** application UX architecture. Current journey
owners are the distinct files under [flows](flows/activity-campaign-journey.md).
Originals at `docs/ui-ux/*.md` may remain on disk until Phase 5; they are not
the current catalog.

The [design system](design-system/README.md) remains **Approved v1.0 Shipboard
Terminal** visual authority. New production UI clones a matching existing
production page and Component Deck specimen. The design lab is isolated
composition evidence and is not a production journey specification.

## Pre-build UI pattern adoption

Classify the surface first against approved UX and
[current state](../current-state.md). Clone and adapt a matching accepted
production page and Component Deck specimen. Use a Design Lab journey only when
the approved layout family has no production donor. Invoke explicit
`$impeccable shape` only for a documented gap. Establish any reusable addition
in the design system before production use.

Voice interaction, interruption, playback, TTS, and the proposed text
Interaction Controller are **unavailable in P0** until a separate product
decision expands scope.

## Purpose

This area governs how users experience the product: journeys, information
architecture, interaction states, accessibility, responsive behavior, content,
and visual design. This README owns **application-level** UX architecture.
Each representative journey keeps a **distinct document owner** under
`docs/ui-ux/flows/` (prepared) and at the Approved original path until
replacements are link-complete.

## Authority during and after the reset

| Status | Meaning |
| --- | --- |
| Approved | Current UI/UX authority for the named concern |
| Draft | Not authoritative for implementation |
| In review | Replacement or prepared source; not cutover authority |
| Superseded | Replaced inside this area; retained only if still in the live tree |
| Retired | Former authority; recover full text from Git |

Technical topology (single SPA, fail-closed publication, design-lab isolation)
is governed by [frontend architecture](../architecture/frontend-architecture.md),
not by this index.

### What is not UI/UX authority

- Design Lab screens, Component Deck specimens, and lab journeys are
  composition evidence. They do not authorize routes, actor permissions,
  lifecycle, Evaluation, Review, Result, or Release.
- Current production pages and `web-legacy` (removed) compositions are
  implemented or historical UI. They do not widen MVP scope or invent a
  journey the approved specifications do not own.
- Design-system later-capability modules (Agent library, Harness authoring,
  voice, tools, Dynamic memory) do not authorize deferred product behavior.

## Entry criteria

Begin UI/UX documentation when:

- At least one P0 feature specification is in `Draft` or `Approved` status
- Actors, permissions, and primary journeys are defined in requirements
- [Concept model](../product/concept-model.md) actors and session concepts are understood
- Open questions that materially affect UI/UX are resolved, or each carries an **interim default** (and `Proposed`/`PROP-*` when consequential)

## Expected document types

| Type | Description |
| --- | --- |
| Application UX architecture | This README: shell, IA, archetypes, shared states, catalog |
| User journey | End-to-end flow for an actor through a bounded outcome (distinct file) |
| Interaction specification | States, transitions, feedback, errors, and edge cases for a surface |
| Content guide | Voice, tone, labels, messages, and empty/error copy |
| Accessibility guide | Keyboard, focus, screen reader, contrast, and accommodation patterns |
| Design system | Components, tokens, layout, and responsive breakpoints |
| Retirement ledger | Provenance only; not behavioral authority |

## Relationship to requirements

UI/UX documents implement and extend approved requirements; they do not override
acceptance criteria. Link interaction specs to `AC-*` IDs from feature
specifications.

## Application shell

Flex Agent uses **one authenticated application shell**. Destinations are the
union of the actor's current server-confirmed capabilities and resource
relationships. Hiding a destination is usability guidance, not authorization.

- Persistent area navigation is the **gangway / bulkhead** (collapsible track or
  leading drawer). It is never amber.
- There is no role-impersonation switch. An account with more than one job may
  see more than one destination; every action remains independently authorized.
- MVP does not provide a general Organization switcher. The application session
  enters exactly one server-derived Organization context (`PROP-UX-6`).
- Layout families for production locators are `management`, `guided-task`, and
  `live-session`. A missing host contract must not fake a live-session or review
  station.

## Object hierarchy

Durable platform term: **Activity**. P0 implements one form (**Campaign**) and
one use case (**assessment Campaign**). Direct, embedded, and API-triggered
Activity forms are deferred.

```text
Activity
├── Campaign (managed multi-participant Activity form)
│   └── Assessment Campaign (P0 use case)
├── Direct Activity (deferred)
├── Embedded Activity (deferred)
└── API-triggered Activity (deferred)
```

Administrative navigation is organized around Activities, cohorts, Enrollments,
and capability-scoped review/release summaries. Participant navigation is
organized around **My work**, the assignment, Submission, Attempt, text Session,
and **Results** after permitted Release visibility. Review work and Release work
remain distinct destinations even when the same person holds both capabilities.

**Agents** and **Harnesses** may appear as planned modules. P0 must not expose
incomplete authoring controls. P0 Campaigns select existing Agent/Harness
revisions or snapshots.

## Navigation model

| Destination | Visible to | Purpose | Delivery tier |
| --- | --- | --- | --- |
| **Home** | Every authenticated actor | Current work and the most important safe next action | P0 (`/` locator; interim redirect to `/my-work` when My work is available) |
| **Activities** | Activity administration | Create and inspect Activities; P0 exposes assessment Campaigns only | P0 |
| **Agents** | Agent-library capability | Reusable Agent identities | P1; no P0 authoring controls |
| **Harnesses** | Harness-library capability | Reusable operating environments | P1; no P0 authoring controls |
| **My work** | Participant with visible Enrollment | Own assigned work | P0 |
| **Review work** | Active Review assignment or permitted review-work management | Assigned case work without general repository browsing | P0 |
| **Release work** | Explicit Release authority | Release exact approved Results | P0 |
| **Results** | Participant with an authorized Activity relationship | Neutral pre-release or own released Result | P0 |
| **Governance** | Separately delegated audit/history or policy access | Minimized reconstructable histories | Partial P0 |

Home prioritization (`IA-MVP-1`), context preservation (`IA-MVP-2`), deep-link
authorization (`IA-MVP-3`), and narrow-viewport behavior (`IA-MVP-4`) remain as
specified in the activity journey document. A path is a locator, not proof of
access.

Canonical P0 locators (not compatibility redirects): `/`, `/activities`,
`/activities/new`, `/activities/:activityId/setup`, cohort Enrollment paths,
`/my-work`, `/my-work/:enrollmentId`, `/sessions/:sessionId`, `/review`,
`/review/:reviewId`, `/release`, `/release/:resultId`, `/results`,
`/results/:resultId`. Exact actor, layout family, and owning journey IDs live
in the activity journey specification.

## Page archetypes

| Archetype | Typical family | Use |
| --- | --- | --- |
| Gate | `management` | Unauthenticated or fail-closed sign-in |
| Destination catalog / Home | `management` | Available work plates; omit unavailable destinations |
| Registry | `management` | Activities, Enrollments, My work, Review work, Release work, Results |
| Setup / create | `management` | Assessment draft, readiness, activation |
| Guided task | `guided-task` | Assignment detail, Review case, Result preview/confirmation |
| Live Session | `live-session` | Isolated text Session after committed start |
| History / provenance | `management` | Separately authorized history |

Shared presentation of these archetypes is the [design system](design-system/README.md).
Journey-specific states, copy meaning, and permissions stay in the owning flow.

## Shared experience principles

| ID | Rule |
| --- | --- |
| `UX-MVP-1` | Show the current server-confirmed status, the next permitted action, and its consequence — or why no action is permitted |
| `UX-MVP-2` | Transient browser states are not committed workflow outcomes; authority is server-confirmed |
| `UX-MVP-3` | Keep Evidence, Evaluation, Human revision, Review decision, Result, and Release distinct |
| `UX-MVP-4` | Scope lists, counts, search, notifications, errors, breadcrumbs, and deep links; denials must not confirm inaccessible existence |
| `UX-MVP-5` | Preserve recoverable local work when safe; distinguish it from server-saved or accepted work |
| `UX-MVP-6` | Status must be perceivable without one sensory channel; core actions must not depend on hover, drag, animation, sound, or pointer precision |

## Shared states

Every primary surface must cover applicable: initial/empty, loading, local
draft, pending command, success, validation error, authorization denied/lost,
sign-in fail-closed, dependency failure, conflict/stale, offline/reconnecting,
and terminal. Success names the exact committed outcome and must not imply a
later stage such as Evaluation or Release.

## Accessibility and responsive baseline

WCAG 2.2 AA is the contractual target. Across the application:

- skip path and landmarks to navigation, title/status, and main task;
- page title, current object, lifecycle state, and primary action are
  programmatically determinable;
- material status changes use an appropriate live announcement;
- validation, conflict, and permission changes move or offer focus to recovery;
- destructive or immutable transitions require deliberate confirmation without
  preselected consent;
- 400 percent zoom and narrow layouts reflow without loss of content, control,
  status, or sequence (`IA-MVP-4`);
- reduced motion preserves meaning.

Feature-level accessibility criteria remain in the owning specifications.

## Content and terminology

Use **Activities** as the durable platform label, **Campaign** as the managed
multi-participant form, and **assessment Campaign** as the P0 use case. Preserve
canonical concept-model capitalization. Action labels name committed intent.
Do not use **complete**, **approved**, **published**, **available**, or
**released** without the owning object.

## Document catalog

| Document | Type | Status | Governs |
| --- | --- | --- | --- |
| [Activity journey](flows/activity-campaign-journey.md) | Distinct flow owner | Approved v1.0 | Platform IA and end-to-end P0 journey |
| [Assessment Campaign setup](flows/assessment-campaign-setup.md) | Distinct flow owner | Approved v1.0 | `JRN-MVP-1` setup/readiness/activation |
| [Submission and Attempt](flows/submission-attempt.md) | Distinct flow owner | Approved v1.0 | `JRN-MVP-2` and `JRN-MVP-3` |
| [Text Session](flows/text-session.md) | Distinct flow owner | Approved v1.0 | `JRN-MVP-4` |
| [Evidence, Evaluation, and Human Review](flows/evidence-evaluation-human-review.md) | Distinct flow owner | Approved v1.0 | `JRN-MVP-5` and `JRN-MVP-6` |
| [Result and Release](flows/result-release.md) | Distinct flow owner | Approved v1.0 | `JRN-MVP-7` |
| [Flex Agent design system](design-system/README.md) | Shared visual, interaction, accessibility, and product-pattern foundation | Approved v1.0 | Shipboard Terminal visual language; semantic tokens, foundations, reusable components, cross-surface product patterns. Does not authorize production capability |

Originals at `docs/ui-ux/{activity-campaign-journey,assessment-campaign-setup,submission-attempt,text-session,evidence-evaluation-human-review,result-release}.md` may remain until Phase 5. They are not the current catalog.

## Related documents

- [Documentation home](../README.md)
- [Product documentation](../product/README.md)
- [Concept model](../product/concept-model.md)
- [Requirements](../requirements/README.md)
- [Design-system implementation guide](design-system/implementation-guide.md)
