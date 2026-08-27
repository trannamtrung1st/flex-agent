# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Stack

React 19 + Vite 8 + TypeScript 5.9 prototypes (this workspace). Routing is
React Router 7; styling is the existing semantic CSS token/component layer
(no Tailwind). Forms use React Hook Form + Zod where validation is non-trivial.
Authored instrument glyphs stay in the Shipboard Terminal language. Prototype
session timers and scripted agent turns are local and labeled synthetic.
Zustand is not used.

The production Flex Agent implementation lives elsewhere (a .NET workspace
per ADR-010). The official prototypes in this workspace are the React app
under `prototypes/` (Vite on `127.0.0.1:5173`). They exist so markup, tokens,
and interactions can be extracted into that frontend. Synthetic demonstration
data only.

## Users

Flex Agent serves four actors; **this workspace designs for the Participant
first** (confirmed 2026-08-25).

- **Participant** — a person completing an assigned assessment: reviews
  instructions, acknowledges rules/consent, uploads submission work, conducts
  a timed text examination with an AI agent, sees status and remaining time,
  and views results after release. Often under time pressure and evaluation
  stress; fairness and clarity matter more than novelty.
- **Administrator** — configures assessments (campaigns), cohorts, tasks,
  deadlines, attempt rules; monitors sessions. (Later design scope here.)
- **Reviewer** — inspects evidence, criterion-level evaluations, transcripts;
  adjusts, approves, and releases results. (Later design scope here.)
- **Buyer** — evaluates whether AI-assisted assessment reduces reviewer burden
  while preserving quality and auditability.

## Product Purpose

Flex Agent is a multi-session conversational AI platform. Its first product
experience is **AI-assisted assessment and examination**: configure an
assessment, assign participants, collect submissions, conduct an isolated
text examination between the participant and a governed AI agent, produce an
evidence-backed structured evaluation, pass it through human review, and
release an audited result. Success means reviewers save time per evaluation
while human oversight, evidence traceability, and fairness across a cohort
are preserved.

## Positioning

> An AI assessment and examination platform built on a reusable,
> memory-controlled conversational-agent foundation.

The durable value is governed behavior, not any model: reusable Agent
identities, governed Harnesses, frozen cohort configuration at activation,
evidence-backed evaluations with inspectable rationale, human review before
release, and complete configuration reconstructability. Model providers are
replaceable execution dependencies. A generic chatbot vendor cannot
truthfully copy the fairness, audit, and review chain.

## Operating Context

- MVP is one executable vertical slice: configure assessment → assign
  participant → upload submission → text examination → evidence-backed
  evaluation → human review → release result.
- The Participant journey: access an assigned session → review instructions
  and acknowledge rules/consent → submit required work (versioned uploads) →
  start a timed text session → answer adaptive, fairness-constrained
  follow-up questions → receive time warnings → complete/submit → see
  completion confirmation → view results after release.
- Sessions are strictly isolated: one participant per session; cohorts are
  administrative groupings, not shared rooms.
- Configuration is frozen at cohort activation; the participant experiences a
  stable, comparable examination.
- The Agent responds through governed decision opportunities (Agent
  Invocation → Agent Decision envelope → runtime-validated output). At most
  one participant-visible message per turn in P0; the runtime, not the model,
  owns identity, order, and audience.
- Canonical terminology from the concept model must be used as-is: Agent,
  Harness, Activity, Campaign, Cohort, Enrollment, Attempt, Session,
  Submission, Evidence, Evaluation, Review decision, Result, Release.

## Capabilities and Constraints

- **P0 is text-only.** No voice, no tools, no richer message kinds, no
  reviewer-facing presentation outputs. Voice is explicitly a later release;
  UI must not imply it exists.
- Timed sessions with remaining-time display and time warnings; optional
  bounded next-timer behavior exists but is runtime-owned.
- Submissions are versioned and preserved; later versions never silently
  replace earlier ones.
- Results are visible only after audited Release; participants may request
  review or appeal when supported.
- **Honest Agent identity boundary (`PROP-AGENT-1`):** the Agent may have a
  distinctive person-like persona, but presentation must remain honestly
  attributable to an Agent — never misrepresent it as a human, and no
  photographic human representation. Any UI persona treatment must keep
  Agent identity discernible wherever authorship could be ambiguous.
- This workspace produces prototypes, not the production frontend. The
  authoritative feature specs, interaction specs, and existing design system
  referenced by the docs live in the main project repository and are not
  present here; prototypes must not contradict approved product meaning.
- Undecided: which Participant surface to prototype first (session, journey
  shell, submission flow) — decided per work request, not here.

## Brand Commitments

- Product name: **Flex Agent**.
- **Binding visual constraint (user, 2026-08-25):** the UI/UX should feel
  futuristic and cinematic — "like a human interacting with a spaceship with
  an AI agent as core," in the manner of sci-fi films. Recorded as given;
  the visual world itself is established in design work, not here.
- The sci-fi treatment must coexist with the honest Agent identity boundary
  and assessment-grade clarity; it cannot present the Agent as human or
  sacrifice fairness-critical information (time, status, rules).

## Evidence on Hand

- Approved product docs in this workspace: `docs/product/overview.md` (v0.4),
  `docs/product/concept-model.md` (v0.5), `docs/product/mvp-scope.md` (v0.4),
  `docs/product/README.md`.
- Referenced but **not present here**: requirements specs, UI/UX interaction
  specs, shared design system, architecture ADRs (they live in the main
  project repo).
- No logos, imagery, testimonials, customers, benchmarks, or pricing exist.
  Do not fabricate any; prototypes use clearly synthetic assessment content.

## Product Principles

1. **Fairness is the experience.** Comparable treatment across a cohort —
   frozen configuration, clear rules, visible timing — outranks expressive
   flourish on any participant surface.
2. **Governed, not autonomous.** The UI reflects that humans review and
   release outcomes and the platform validates every Agent effect; never
   imply the AI acts on its own authority.
3. **Honest AI presence.** The Agent can have character, but the interface
   always makes clear the participant is talking to an Agent, not a person.
4. **Inspectable by design.** Evidence, rationale, status, and history are
   first-class UI material, not buried metadata.
5. **Prototype for extraction.** Every prototype is written so its structure,
   tokens, and interactions transfer cleanly to the production codebase.

## Accessibility & Inclusion

WCAG 2.2 AA is required (confirmed 2026-08-25). The cinematic sci-fi
direction must meet it: contrast, reduced-motion support, keyboard operation,
and legibility under time pressure are non-negotiable on assessment surfaces.
Accommodations are a product concept (Enrollment-scoped timing adjustments),
so the UI must tolerate per-participant timing differences without exposing
them to others.
