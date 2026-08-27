---
version: 1
slug: "prototypes-reviewer-console"
primary_target: "prototypes/src/routes/ReviewerPage.tsx"
related_targets: []
---

# Surface brief — Reviewer console (Overlay Ledger)

## Scope and mode

Operate. Authorized Reviewer console: review queue (manifest table) plus evaluation record (Overlay Ledger) at `/reviewer-console`. Completes the MVP slice after participant examination — inspect evidence-backed evaluations, adjust in place, approve/reject/escalate, release result.

## Shell topology

Reviewer command strip (Flex Agent · Review · Home link · reviewer ID · sign out). Queue unfolds into record on one page (clip-path transition). Participant Home remains separate actor surface.

## Audience, job, constraints

Authorized human under audit obligation. Job: read sealed transcript and submission evidence, weigh criterion evaluations with rationale and confidence against cited turns, adjust while preserving Agent original, then approve/reject/escalate and release. WCAG 2.2 AA. Synthetic content only. Canonical terminology. No editing transcript or evidence.

## Chosen direction

Overlay Ledger (seed 2880621d, comp-led). Approved comp: `.impeccable/mocks/decision/reviewer-overlay-ledger.webp`. Sealed transcript center; criterion marginalia right with hairline tethers to cited turns; manifest rail left; one amber APPROVE & RELEASE key. Queue is manifest-table grammar. Raises: Miura unfold (row deploys into dossier); HyperCard in-place revision (Agent original preserved beneath).

## Memorable moment

Evidence tethers: activating marginalia lights cited transcript turns and draws hairline paths — the evaluation rides over the exam as inspectable marginalia, not a detached scorecard.

## States and ranges

Queue: 0–8 rows; demo states default / busy / single / empty. Record: awaiting → adjusted (original preserved) → approved/rejected/escalated → released with confirm dialog. 3–5 criteria per session; 5–9 transcript turns; low-confidence criterion flagged.

## Navigation

Queue Inspect/Open → record unfold. Docket back → queue. Released rows: View only. Home link → participant-home.

## Unresolved decisions

- Reviewer strip vs shared Command Strip component literal (built as reviewer variant with Home link).
- Rejected session re-queue presentation (shown in table state column).
