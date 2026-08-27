---
version: 1
slug: "prototypes-participant-journey"
primary_target: "prototypes/src/routes/JourneyPage.tsx"
related_targets: ["prototypes/src/routes/SessionPage.tsx"]
---

# Surface brief — Participant assignment journey landing

## Scope and mode

Operate. Assignment station and phase navigation shell for one Enrollment: briefing, submission placeholder, handoff to text examination, result placeholder. Route: `/participant-journey`. Related target: `/participant-session` (live examination).

## Audience, job, constraints

A candidate under evaluation stress arriving at one assigned assessment. They must understand the assignment, see their current phase on a persistent rail, and take exactly one allowed next step. Agent identity boundary preserved (assignment copy states AI Agent; no human misrepresentation). WCAG 2.2 AA: contrast, keyboard, reduced-motion. Synthetic content only, clearly labeled.

## Chosen direction

Shipboard Terminal extension (seed 8066189d) — assignment station, not a second examiner console. No Agent core or chrono hero. Phase rail with four nodes (Briefing → Submission → Examination → Result); current-phase well; one amber commit key. States as instrument marks (node warmth, record readout), not colored blobs.

## Memorable moment

Phase commit: acknowledging briefing or completing submission advances the rail — the well reveals with a single phosphor sweep (clip-path), and the current node warms to amber.

## Navigation topology

- Default demo state: first arrival (briefing current).
- Demo state selector cycles six beats for implementation reference (`?demo=` persisted).
- Working flow: ack briefing → mark submission complete → Enter Session → Return to Session while active → result pending → result released placeholder.
- Session prototype gains quiet **Assignment** back-link on its rail (no restyle).

## Placeholder boundaries

- **Submission:** versioned list with synthetic v1/v2; upload channel disabled with honest label.
- **Result pending:** awaiting Release; no scores.
- **Result released:** identity readout only; View Result disabled — full result surface deferred.

## Briefing split (resolved)

Assignment briefing and consent live on this landing. Examination protocol and session rules remain in the session pre-start briefing overlay. Production may merge later; prototype keeps both to model the full chain.

## Unresolved decisions

- Production merge of assignment vs session briefing (deferred).
- Shared token extraction between journey and session styles (deferred).
