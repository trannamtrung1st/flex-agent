---
version: 1
slug: "prototypes-participant-session"
primary_target: "prototypes/src/routes/SessionPage.tsx"
related_targets: []
---

# Surface brief — Participant text examination session

## Scope and mode

Operate. The live examination screen (flagship prototype), with key states built in: pre-start briefing/acknowledgment, live session, time warning, submit confirmation, sealed complete. Route: `/participant-session`.

## Audience, job, constraints

A candidate under evaluation stress completing a timed text examination with a governed AI Agent. Always legible: the Agent's current question, time remaining, session stage, rules/status. Agent must read calm and supportive, honestly non-human (PROP-AGENT-1). WCAG 2.2 AA: contrast, keyboard, reduced-motion. Prototype's success criterion: sell the spaceship-AI vision.

## Chosen direction

Shipboard Terminal (hard sci-fi working-ship console, The Expanse lineage), locked from seed 8066189d.
Approved comp: `.impeccable/mocks/decision/assigned.webp` (comp round choice "A — Console Ledger", approval recorded in `assigned.webp.json`).
Raises carried: one vertical session spine governs layout; numbered addressable exchange rail; amber reserved for attention only; world owns 100% of viewport; states as instrument marks, not colored blobs.

## Memorable moment

The transmit/response cycle: a reply transmits into the numbered ledger, the Agent core rings ripple while the Examiner considers, and the next question materializes with a single phosphor sweep. One authored motion; everything else is 150–250ms state feedback.

After seal, the complete plate carries a begin-key exit to the assignment (`result-pending`). Results stay locked until Release; the next place is the journey well, not a fabricated next examination.

## Sampled color record (from approved comp pixels; supersedes card chips)

- Page/panel ground: `#050e13` (page sheen ranges `#040d12`–`#0a141a`)
- Left rail ground (darker layer): `#02090e`
- Body text: `#cfd7dd`; brightest text/digits: `#f6fcfe`
- Dim labels (AGENT/PARTICIPANT, captions): `#77989f`
- Bezel hairlines: `#577f81` (rendered at low alpha)
- Attention amber: `#e2a13a` (active spine numeral highlight `#edc890`)
- System teal: `#3cc0bf`

## Comp design-system record

Component grammar: smoked-glass panels with 1px teal-tinted hairline bezels and notched/cut corners (45° clips), no border-radius cards, no shadows-as-elevation (elevation = hairline + subtle panel glow). Engraved caps labels, letter-spaced, tiny. Readouts as label-over-value stacks separated by hairlines. State marks are symbols (triangle marker for active turn, node dots on spine, segment bars for link). Amber appears only on: timer, active turn marker/numeral, TRANSMIT, attention states. Type ramp: tiny caps labels (~10–11px tracked wide) → body mono (~14–15px) → current question large (~22–26px) → timer digits (~40–48px tabular).

## Implementation fidelity inventory

| Ingredient | Medium |
| --- | --- |
| Page ground + vignette/panel glow | CSS gradients (light effects; world's material is phosphor glow, no physical texture in comp) |
| Left instrument rail: SESSION/CANDIDATE/SESSION IDs, CONSOLE FEED, SYS TEMP/CORE LOAD/MEMORY/UPLINK, PROTOCOL box | semantic HTML/CSS |
| Agent core concentric rings (top center) | authored SVG + CSS breathing animation |
| Reassurance line under core | HTML |
| Timer digits 00:41:17 (large amber tabular) | HTML + live JS countdown |
| Timer arc gauge with ticks (60/30 marks) | authored SVG + JS |
| STAGE — EXAMINATION 3 OF 5 readout | HTML |
| Transcript panel notched bezel | CSS clip-path + hairlines |
| Numbered spine 01–05, node markers, vertical rule, active amber numeral + triangle | HTML/CSS |
| Turn rows (label, body, timestamp); current question at display scale | HTML |
| Composer field with angled notch + amber TRANSMIT chevron key | HTML/CSS clip-path, fully interactive |
| LINK NOMINAL readout (equal-height teal signal bars + label) | authored SVG |
| Primary action (TRANSMIT key: amber outlined chevron-cut key) | HTML/CSS clip-path with full state set |
| Complete plate begin-key (Return to Assignment → journey result-pending) | Key + React Router |
| Browser surfaces: caret, selection, scrollbar, focus rings | CSS themed from palette |
| TYPE: placards/labels Michroma (Eurostile-extended class, wide); transcript/digits Sometype Mono (regular-width mono, tabular digits; Geist Mono was rejected by the design hook as overused) | webfont |

No raster-native regions: every comp region is drawn interface geometry (code territory). Asset-producer second opinion still requested.

## Unresolved decisions

- Whether the collapsed-history "focus mode" (comp C) becomes a toggle in a later iteration.
- Production font licensing/self-hosting (prototype loads Google Fonts).
