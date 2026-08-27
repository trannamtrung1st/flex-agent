---
version: 1
slug: "prototypes-participant-home"
primary_target: "prototypes/src/routes/HomePage.tsx"
related_targets: ["prototypes/src/routes/JourneyPage.tsx","prototypes/src/routes/SessionPage.tsx"]
---

# Surface brief — Participant Home (assignment roster + management shell)

## Scope and mode

Operate. Post-login Participant Home: an Enrollment roster inside the management shell (top command strip), at `/participant-home`. Related targets: `/participant-journey` (assignment station), `/participant-session` (live examination).

## Shell topology (locked 2026-08-25)

Management chrome only. The top token strip (Flex Agent · Home · candidate · sign out) belongs to Home and future sibling list pages. The assignment station and examination console remain their own full-viewport shells; the station gains a quiet Home return, the exam stays immersive with no global nav. Menu stays honest: Home plus identity/sign-out — no invented Account, Settings, or Results archive. Released enrollments stay on the roster and open into the assignment Result well.

## Audience, job, constraints

A candidate signing in to see assigned work. Job: read every Enrollment's campaign, title, deadline, phase, and Record at a glance, and open exactly one. Under evaluation stress; fairness legibility outranks expression. WCAG 2.2 AA. Synthetic content only, honestly labeled. Not a kanban: bays are Record states the runtime owns, never draggable workflow.

## Chosen direction

Status Bays (seed cd5abd06, comp-led). Approved comp: `.impeccable/mocks/decision/home-status-bays.webp` (approval in `home-status-bays.webp.json`). One shared etched instrument frame divided into four labeled Record bays — OPEN · LIVE · PENDING RELEASE · RELEASED — each holding notched enrollment plates. One amber OPEN key on the single plate needing action; Live carries an amber node dot, Released a teal seal ring.

## Memorable moment

The bay board reads as one instrument: the frame's etched bezel and bay hairlines are a single fixture, and exactly one amber key on the whole screen tells the candidate where to go.

## Sampled color record (from approved comp pixels; incumbent tokens produce these through glow)

- Page ground: `#030e15`–`#041016` (darker end of incumbent `--ground-deep`); top strip `#031118`.
- Plate fill: `#08151b`; Released plate slightly lifted `#0c2026`.
- System teal in comp renders hot (`#57dada` brand, `#8cf7f8` bay headers/labels) — incumbent `--teal #3cc0bf` plus phosphor text-shadow reaches this; do not introduce a new teal token.
- Amber key text renders `#fdcc2a`-hot — incumbent `--amber #e2a33c`/`--amber-bright #edc890` plus glow; keep the ration rule.
- Value text `#ffffff` bright (incumbent `--text-bright`).

## Implementation fidelity inventory

| Ingredient | Medium |
| --- | --- |
| Top command strip: brand placard, HOME token with teal underline, candidate ID, SIGN OUT key | semantic HTML/CSS |
| Etched outer instrument frame with notched corners and top/bottom double-edge marks | CSS clip-path + hairlines |
| Four bay columns with Michroma headers, separated by 1px vertical hairlines | CSS grid + borders |
| Enrollment plates: notched glass, label-over-value readout stacks with hairline dividers | semantic HTML/CSS (dl stacks) |
| Amber OPEN key (single hot key) | HTML/CSS, full state set |
| Live amber node dot, Released teal seal ring | CSS node dot; authored SVG ring + check |
| Empty-bay and empty-Home states | HTML/CSS (required states) |
| Quiet Home return on assignment station rail | HTML/CSS edit to journey prototype |
| Browser surfaces: caret, selection, scrollbar, focus rings | CSS themed from palette |
| Type: Michroma placards, Sometype Mono data | webfont (Google Fonts, as siblings) |

No raster regions: every comp region is drawn interface geometry.

## States and ranges

- Default demo: four enrollments across the four bays (comp content).
- Empty bay: honest dim label ("No enrollments in this bay"), not blank space.
- Empty Home: zero enrollments — centered instrument plate stating no assigned work.
- Demo state selector (as sibling prototypes): populated / crowded / single-open / empty. Crowded shows multiple campaigns sharing a Record state (three Open, two Pending, two Released); the amber ration holds — one OPEN key on the whole board, on the plate with the nearest deadline.
- Plate counts per bay: realistic 0–3; the board is not built for dozens.

## Navigation topology

- OPEN plate's amber key → assignment station (`/participant-journey`).
- LIVE plate quiet RETURN key → live exam (`/participant-session`).
- Pending plate: no key (nothing actionable). Released plate: quiet VIEW key → station result phase.
- Station rail gains quiet "Home" back-link; exam untouched (immersive).

## Unresolved decisions

- Mobile bay order (stacked vertically OPEN-first) confirmed at build, not re-asked.
- Whether sibling list pages (results archive) ever exist — deferred; menu stays Home-only.
