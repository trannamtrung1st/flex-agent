---
name: Flex Agent — Shipboard Terminal
description: A governed AI examination held on a working ship's console — smoked glass, hairline bezels, phosphor teal systems, rationed amber attention.
colors:
  ground: "#07141b"
  ground-deep: "#041018"
  ground-sheen: "#0e1c24"
  text: "#e6eef2"
  text-bright: "#f8fcfe"
  label: "#a8c4ca"
  label-dim: "#88a8b0"
  hairline: "rgba(110, 154, 156, 0.52)"
  hairline-dim: "rgba(110, 154, 156, 0.28)"
  phosphor-teal: "#3cc0bf"
  teal-glow: "rgba(60, 192, 191, 0.14)"
  signal-amber: "#e2a33c"
  amber-bright: "#edc890"
  amber-glow: "rgba(226, 163, 60, 0.18)"
typography:
  display:
    fontFamily: "Sometype Mono, ui-monospace, monospace"
    fontSize: "2.9rem"
    fontWeight: 600
    lineHeight: 1
    letterSpacing: "0.03em"
  headline:
    fontFamily: "Sometype Mono, ui-monospace, monospace"
    fontSize: "1.28rem"
    fontWeight: 400
    lineHeight: 1.5
  title:
    fontFamily: "Michroma, Arial Narrow, sans-serif"
    fontSize: "0.72rem"
    fontWeight: 400
    letterSpacing: "0.24em"
  body:
    fontFamily: "Sometype Mono, ui-monospace, monospace"
    fontSize: "15px"
    fontWeight: 400
    lineHeight: 1.55
  label:
    fontFamily: "Sometype Mono, ui-monospace, monospace"
    fontSize: "0.62rem"
    fontWeight: 400
    letterSpacing: "0.14em"
rounded:
  none: "0px"
spacing:
  xs: "8px"
  sm: "10px"
  md: "16px"
  lg: "22px"
  xl: "26px"
components:
  key-quiet:
    backgroundColor: "transparent"
    textColor: "{colors.label}"
    rounded: "{rounded.none}"
    padding: "10px 20px"
  key-compact:
    backgroundColor: "transparent"
    textColor: "{colors.label}"
    rounded: "{rounded.none}"
    padding: "6px 12px"
    height: "30px"
  key-quiet-hover:
    backgroundColor: "transparent"
    textColor: "{colors.phosphor-teal}"
  key-transmit:
    textColor: "{colors.signal-amber}"
    rounded: "{rounded.none}"
    padding: "10px 26px 10px 30px"
  key-transmit-hover:
    backgroundColor: "rgba(226, 163, 60, 0.16)"
    textColor: "{colors.amber-bright}"
  key-begin:
    textColor: "{colors.amber-bright}"
    rounded: "{rounded.none}"
    padding: "10px 26px 10px 30px"
  key-begin-hover:
    backgroundColor: "rgba(226, 163, 60, 0.48)"
    textColor: "{colors.amber-bright}"
  key-open:
    textColor: "{colors.signal-amber}"
    rounded: "{rounded.none}"
    padding: "10px 30px 10px 34px"
  key-open-hover:
    backgroundColor: "rgba(226, 163, 60, 0.16)"
    textColor: "{colors.amber-bright}"
  key-activate:
    textColor: "{colors.amber-bright}"
    rounded: "{rounded.none}"
    padding: "10px 30px 10px 34px"
  key-activate-hover:
    backgroundColor: "rgba(226, 163, 60, 0.28)"
    textColor: "{colors.amber-bright}"
  key-inspect:
    textColor: "{colors.signal-amber}"
    rounded: "{rounded.none}"
    padding: "10px 26px 10px 34px"
  key-inspect-hover:
    backgroundColor: "rgba(226, 163, 60, 0.16)"
    textColor: "{colors.amber-bright}"
  key-release:
    textColor: "{colors.signal-amber}"
    rounded: "{rounded.none}"
    padding: "10px 30px 10px 38px"
  key-release-hover:
    backgroundColor: "rgba(226, 163, 60, 0.16)"
    textColor: "{colors.amber-bright}"
  field-input:
    backgroundColor: "rgba(2, 9, 14, 0.55)"
    textColor: "{colors.text-bright}"
    rounded: "{rounded.none}"
    padding: "10px 14px"
  dropdown-key:
    backgroundColor: "rgba(2, 9, 14, 0.55)"
    textColor: "{colors.text-bright}"
    rounded: "{rounded.none}"
    padding: "10px 14px"
  composer:
    textColor: "{colors.text-bright}"
    rounded: "{rounded.none}"
    padding: "20px 22px"
  strip-profile-key:
    backgroundColor: "transparent"
    textColor: "{colors.label}"
    rounded: "{rounded.none}"
    padding: "0 18px"
  strip-profile-key-open:
    backgroundColor: "{colors.teal-glow}"
    textColor: "{colors.phosphor-teal}"
  gangway:
    textColor: "{colors.label}"
    rounded: "{rounded.none}"
    width: "232px"
  gangway-collapsed:
    width: "76px"
---

# Design System: Flex Agent — Shipboard Terminal

## Overview

**Creative North Star: "Shipboard Terminal"**

A timed examination held on a working ship's console: the candidate faces a governed AI examiner behind smoked glass, with time, stage, and record always visible on instruments. The lineage is hard sci-fi working-ship interfaces (The Expanse), not consumer chat. The system explicitly refuses the rounded dark AI-chat-bubble screen — there are no bubbles, no avatars-with-smiles, no pill buttons. Every surface is a flat smoked-glass panel framed by a teal hairline bezel with notched (clipped, never rounded) corners, sitting on a near-black blue-green ground.

The palette is severe and rationed: cool desaturated teals carry all structure and systems; one warm amber exists solely for attention (the timer, the active question, the commit keys, acknowledgment marks). Density is instrument-panel density — many small uppercase readouts with wide letter-spacing, hairline dividers, and a single large focal element per region. State is always conveyed by instrument marks (a node dot lighting up, a hairline changing color, digits brightening, a wait-mark scanning), never by colored blobs, badges, filled pills, or spinners.

**Key Characteristics:**
- Smoked-glass panels on a #07141b ground, framed by low-alpha teal hairlines with notched corners (10–18px cuts)
- Phosphor teal (#3cc0bf) for all system life; amber (#e2a33c) strictly rationed to attention
- Two type voices: Michroma placards for identity, Sometype Mono for every piece of data
- Hairline circuitry traces with circular node terminals as the signature ornament
- Calm, governed motion: 150–250ms state feedback, one authored phosphor-sweep moment, full reduced-motion support
- One shared CSS layer (`prototypes/src/styles/`: tokens, base, then family sheets under `components/`) consumed by all five surfaces; React families live under `prototypes/src/components/` and the Component Deck gallery is the living catalog
- Wait is a teal instrument family (wait-mark, scan-track, skel-stack, wait-plate), never a spinner; amber stays on the current stage bar only
- Navigation chrome: four shell modes — management (command strip + operator profile + role-home), guided-task (rail + compact identity), live-session (rail + guarded leave), reference (catalog Index only). Gangway (232px default, 248px in the Administrator shell, folding to a 76px channel-code rail) and bulkhead drawer are the shared stable-area navigation grammar; the Administrator Gangway is grouped (assessment / organization / governance). Campaign context stays inside relevant operational pages via one shared `CampaignContext` component on Enrollments, Cohorts, and Sessions

## Colors

A near-black blue-green ground carrying two cool structural teals and one hot rationed amber; nothing else.

### Primary
- **Phosphor Teal** (`phosphor-teal`, #3cc0bf): the voice of the system itself — the Agent core rings and dot, the examiner's name, focus outlines, hover states on quiet keys, the composer's focus bezel, list tick marks, the link signal bars, and the completion mark. Its soft counterpart **Teal Glow** (rgba(60,192,191,0.14)) backs active/pressed fills and the core's radial halo.

### Secondary
- **Signal Amber** (`signal-amber`, #e2a33c): attention only. The countdown digits and gauge fill, the active question's index node and speaker mark, the TRANSMIT/BEGIN keys, the checked acknowledgment mark, and the caret. **Amber Bright** (#edc890) is its intensified state (hover text, time-warning digits); **Amber Glow** (rgba(226,163,60,0.18)) is its halo (text-shadow, checked fills, hover box-shadow).

### Neutral
- **Ground** (#07141b): the base hull color; body background gradients run from **Ground Deep** (#041018) at the top to **Ground Sheen** (#0e1c24) at the bottom, and panel fills are Ground Deep at 0.15–0.6 alpha.
- **Console Text** (#e6eef2): default reading text. **Bright Text** (#f8fcfe) is reserved for the current question, composer input, and plate titles.
- **Instrument Label** (#a8c4ca) and **Dim Label** (#88a8b0): the uppercase microlabel voices — readout keys, timestamps, quiet keys at rest, decorative node borders.
- **Hairline** (rgba(110, 154, 156, 0.52)) and **Dim Hairline** (rgba(110, 154, 156, 0.28)): every bezel, divider, and trace. Structure is always drawn at 1px in one of these two alphas.

### Named Rules
**The Amber Ration Rule.** Amber appears only where the candidate's attention is demanded: time, the active turn, the current stage bar, and the commit action. It never decorates, never labels a resting element, never marks wait or progress, and never appears twice in one region for different reasons. Its rarity is the mechanism.

**The Instrument Mark Rule.** State changes are shown as instrument marks — a node dot's border shifting to amber, digits brightening, a hairline warming — never as colored pills, filled badges, or background blobs.

**The Two Control Voices Rule.** Amber marks commitment — acknowledgment marks, hot keys, validation errors, attention toasts and advisories. Teal marks selection and system state — radio marks, breakers, select-marks, option-menu ticks, calendar and time-wheel selected bezels, system toasts, focus bezels, wait instruments. Never swap the voices; a checkbox is not a radio, and a row-select mark is not an acknowledgment. A waiting hot key drops amber for teal occupation.

## Typography

**Placard Font:** Michroma (with Arial Narrow, sans-serif)
**Data/Body Font:** Sometype Mono (with ui-monospace, monospace), weights 400/500/600

**Character:** Michroma is the engraved placard riveted to the panel — always uppercase, tiny, and tracked extremely wide; it names things and never speaks. Sometype Mono is everything the console prints: transcript, readouts, digits, input. There is no third voice and no system UI font anywhere.

### Hierarchy
- **Display** (600, 2.9rem, lh 1, tabular-nums): the countdown digits only, with an amber-glow text-shadow. Shrinks to 2.2rem at ≤1180px and 1.75rem at ≤760px.
- **Headline** (inherit at 1.55 lh): the current active question in the ledger — same size as other turns, in Bright Text; hierarchy comes from the speaker mark and warmed border, not a type-size jump.
- **Title / Placard** (Michroma 400, 0.54–1.15rem, tracking 0.17–0.34em, uppercase): brand line, agent name, panel labels, key captions, plate titles. Larger tracking as the size grows.
- **Interactive chrome** (0.75rem, tracking 0.08–0.12em, min-height 44px): strip tokens, operator trigger and enabled menu items, role-home and task-exit keys, gangway/nav-rail links, drawer Menu controls.
- **Body** (400, 15px base / 0.88–0.95rem in panels, lh 1.5–1.55): transcript turns (max 78ch), briefing copy (max 68ch), composer input.
- **Label / Microlabel** (400–500, 0.5–0.68rem, tracking 0.14–0.32em, uppercase): readout keys, speakers, timestamps, feed entries, stage line, profile role (Michroma 0.52rem / 0.22em), rail designation (0.5rem / 0.32em), disabled-action notes. Supporting interactive labels may use 0.68rem; they never drop below that floor.

### Named Rules
**The Two Voices Rule.** Michroma placards name; Sometype Mono speaks. No text on any surface uses another family, and every Michroma or microlabel string is uppercase with wide tracking.

**The Tabular Time Rule.** Every changing number (timer, indices, gauge numerals, calendar days, HH/MM wheels, and optional SS) is set in Sometype Mono with `font-variant-numeric: tabular-nums` so digits never jitter.

## Layout

Five layout shells share the same token stack; each surface picks the shell that fits its task.

**Examination console** (`.console`): a fixed full-viewport CSS grid (`100dvh`, min 620px; `overflow: hidden` on `body`) with a **232px instrument rail** left, a **fluid session column** center, and a **320px examiner panel** right — `grid-template-areas: "rail session-main agent-panel"` with a 26px column gap and asymmetric padding (18px 20px 18px 0; the rail bleeds to the left edge). Regions are separated by hairline rules rather than boxed margins. The instrument rail is the densest left column: it scrolls as one track (`overflow-y: auto`, `overscroll-behavior: contain`); brand, readouts, nav keys, console feed, and protocol plate keep natural height and never flex-shrink. The feed log is not a nested scroller.

**Session column** (`.session-main`): a flex column holding the transcript ledger and composer footer. The ledger is the only vertical scroll region in the center (`overscroll-behavior: contain`, subtle top/bottom fade masks, `scroll-padding`, active-turn `scrollIntoView`). Below it: the bezeled composer slot with embedded TRANSMIT key, then the **Link Nominal** connection readout (label + equal-height teal signal bars, right-aligned).

**Examiner panel** (`.agent-panel`): a full-height smoked-glass plate with notched bezel containing the Agent core and reassurance line (top, hairline-separated) and the chrono instrument stack (timer digits, arc gauge, stage readout, Submit key) below. Time warning pulses an inset amber ring on the whole panel.

**Assignment station** (`.station`): a two-column grid (`100dvh`, min 620px) with a **260px phase rail** left and a **fluid main column** right — `grid-template-areas: "phase-rail main"` with a 28px gap and padding 18px 22px 18px 0. The phase rail scrolls the same way as the examination rail — brand, enrollment readout, phase spine, demo plate, and protocol keep natural height; the spine is not a nested scroller. The main column stacks an assignment header (title + status readout), a scrollable **well frame** for the current phase content, and an action row (note + keys). No examiner panel on this surface.

Density is instrumental on every shell: readout stacks use 9–10px vertical padding between hairline dividers; framed panels share `--panel-sheen` + `--panel-depth` + `--panel-inset`. Transcript turns are channel cards at 75% width (92% on mobile): agent left with amber wash, participant right with teal glass; a tabular index badge sits inside each card's top-right corner. There is no center spine in the transcript — only aligned channel cards.

**Examination breakpoints:** at ≤1180px the rail flattens into a horizontal band above the console (feed and telemetry hidden); session column and examiner panel share the row below. At ≤760px the stack is railband → compact examiner band → session column; the examiner stays visible (never hidden), the viewport stays `100dvh`, and the ledger scrolls in the remaining space.

**Assignment breakpoints:** at ≤1080px the station becomes single-column with the phase rail as a horizontal band (protocol plate hidden); phase list wraps into a row, the page scrolls naturally (station height auto, min 100dvh), and the content well keeps a 340px minimum height so the rail can never starve it. At ≤720px the assignment header stacks, status readouts gain top dividers, the well padding tightens, and the action row stacks vertically.

**Home status board** (`.command-strip` + `.board`): a full-viewport flex column (body `overflow: hidden`) — a slim command strip across the top, one etched board frame filling the remaining height (26px 30px 18px board padding), and a quiet footer row beneath. Inside the frame, four Record bays (OPEN / LIVE / PENDING RELEASE / RELEASED) split the width as equal grid columns (`repeat(4, minmax(0, 1fr))`) separated by dim hairline rules; each bay scrolls its own plate stack independently (16px plate gaps, `overscroll-behavior: contain`) so the board itself never scrolls.

**Home breakpoints:** at ≤1080px the page scrolls naturally (height auto, min 100dvh), the bays fold to a 2×2 grid with dim hairline rules on both axes, and the frame's edge nodes hide. At ≤720px bays stack single-column, the command strip regrids (brand + operator profile on the first row; nav drops to its own full-width hairline-topped row that scrolls horizontally — tokens no longer wrap — and the current-page underline shortens to a fixed 42px bar), the mode placard hides, and the footer stacks vertically.

**Administrator workspace** (`.command-strip` + `.admin-shell` + area content): a full-viewport flex column (body `overflow: hidden`) with the shared command-strip wordmark, ADMIN suffix, and operator profile above an `.admin-shell` flex row. Desktop seats a **Gangway** track at the surface override of 248px (`--gangway-w: 248px`), user-collapsible to the shared 76px channel-code rail, beside a fluid content column. The Gangway is grouped: Assessment operations (CAM Campaigns, COH Cohorts, ENR Enrollments, SES Sessions), Organization control (ACC Users & Access, POL Policies), and Governance (AUD Audit Log). Routes are `/admin-console/campaigns`, `/cohorts`, `/enrollments`, `/sessions`, `/users-access`, `/policies`, and `/audit-log`, with the index redirect preserving the query string into Enrollments. Gangway and Bulkhead contain stable area navigation only; CAM remains current for both Campaign Registry and Campaign Record and always returns to the bare registry. Campaigns and Enrollments remain the functional prototype areas; Cohorts, Sessions, Users & Access, Policies, and Audit Log are polished sample/empty surfaces (`.sample-wall`) — not Agents, Harnesses, integrations, billing, or policy authoring. Enrollments, Cohorts, and Sessions share one campaign context: a missing parameter uses the remembered seated campaign, then the first available campaign; an invalid parameter canonicalizes to the first available campaign. Their compact page-local **Campaign Context** instrument (`CampaignContext.tsx`) sits directly below the page head with a Campaign picker followed by Activation (`ActivationMark`: full sentence `Draft — not activated` / `Frozen at activation`). The Campaign Registry Activation column and filter use the same helper in `compact` (`Draft` / `Frozen`). Campaign Registry, Campaign Record, invalid records, organization, and governance omit that instrument. Campaigns does not auto-select — an invalid campaign address shows an unavailable-record empty plate rather than substituting another campaign. Operational Gangway links (COH, ENR, SES) preserve a seated campaign. Organization and governance links omit `campaign` so tenant work is never campaign-scoped. At ≤1080px the Gangway leaves layout and a compact drawer bar plus leading **Bulkhead** carry the same stable area destinations.

**Enrollments (Record Wall)** (`.wall`): the enrollment manifest instrument inside the shell — a glowing Michroma **wall head** (ENROLLMENT MANIFEST, 0.78rem at 0.28em with teal text glow), mono note, and shared page-local Campaign Context over one etched wall frame (18px 24px 16px area padding; frame carries the board's 18px/17px clipped-border cut, 46px edge ticks, and 5px right-edge nodes). Campaign selection lives in the shared Campaign Context, not the manifest toolbar. The shared `.datatable-frame` + `.datatable` composition fills the frame and uses the Component Deck's canonical 18px inline gutter across toolbar, scroll region, expanded detail, and pagination foot; surfaces do not override this geometry. Row expansion follows the HyperCard raise: inspect inline before any ceremony.

**Campaigns area** (`.campaigns-wall`): a glowing Michroma **campaigns head** over one etched frame. With no selection the frame is the **Campaign Registry** (`.campaigns-registry-frame`) — shared datatable grammar with activation filter, search, sortable heads (Campaign, Activation, Enrollments, Cohort deadline, Updated), pagination, teal row selection, and a trailing Actions command menu. A valid `?campaign=` parameter swaps the registry for the **Campaign Record** (`.campaigns-frame`, `width: fit-content`, `min-width: min(100%, 52rem)` — never ballooning to fill the viewport) — identity, enrollment count, activation state, and configuration values seated in the shared six-track **Readout Grid** (`ReadoutGrid` / `.readout-grid`). The summary row spans 3 / 1 / 2 tracks and the six configuration fields each occupy one, so both semantic rows share exact divider horizons; below 46rem of container width the grid becomes divided rows. Values retain Bright Text, tabular numerals, and the sealed-solid state node when frozen. A quiet Back-to-campaigns key sits in the head, and a quiet Configure key in the frame foot summons the ceremony plate. An invalid campaign address shows an unavailable-record empty plate rather than substituting another campaign. The single hot amber ACTIVATE key lives in that ceremony only.

**Sample destinations** (`.sample-wall`): Cohorts, Sessions, Users & Access, Policies, and Audit Log share the Campaign Record's etched frame (`.campaigns-frame.sample-frame`) but hug their seated content — they do not inherit the record's 52rem minimum, which would leave an empty hairline wing beside a short plate. Campaign-scoped Cohorts and Sessions place the same shared Campaign Context directly below their page head and outside the content frame. Organization and governance surfaces omit campaign context. Absence inside the frame uses the shared empty plate in inset form (`.empty-plate--inset`) with the state node seated beside the label, never a second clipped card. A standalone inset starts without a divider; `.empty-plate--separated` adds the dashed absence horizon only when the empty state follows seated content. Policies seats a two-track **Readout Grid** plus a frozen line instead of an empty plate. The standalone clipped empty plate remains the not-found / unavailable instrument.

**Configuration ceremony:** native `<dialog>` over a lighter scrim (`rgba(4, 12, 17, 0.45)`). Activation runs the phosphor-sweep freeze (`.is-sealing`) on whichever etched frame is active — the Enrollments wall frame or the Campaigns record frame.

**Administrator breakpoints:** at ≤1080px the page scrolls naturally (height auto, min 100dvh), the Gangway swaps to Bulkhead, frame edge nodes hide, and the Campaign record expands to full width. Its Readout Grid adapts by container rather than viewport, stacking as divided rows below 46rem. Both the registry and the enrollment manifest inherit the shared datatable's horizontal scroll with the canonical 680px table minimum. At ≤720px ceremony form pairs stack vertically, and the ceremony foot becomes a full-width vertical action stack. Campaign selection never returns as a horizontal tab track.

**Reviewer console** (`.queue-view` / `.record-view`): one full-viewport flex column (body `overflow: hidden`) whose shell holds two absolutely-stacked views that trade places with a 640ms clip-path unfold — the **Review Docket** (a title block over one clipped-border etched frame — the board frame's 18px/17px two-layer cut with 46px centered edge ticks — that hugs the docket's rows instead of filling the viewport) and the **evaluation record** (the Overlay Ledger). The record is a three-column grid — a **220px manifest rail** left (session readout stack plus preserved-submission list; the rail scrolls, children keep natural height), a **fluid sealed-transcript column** center, and a **280px marginalia rail** right — under a record head (Docket back key, title with sealed mark, session ID) and above a hairline-topped **decision bar**. An SVG tether layer spans the whole grid above the columns. A quiet console footer (synthetic-content note, demo-state select) closes the page.

**Reviewer breakpoints:** at ≤1180px the record columns tighten to 200px / fluid / 240px. At ≤960px the page scrolls naturally (height auto, min 100dvh) and the record stacks single-column — manifest rail band, transcript (min 320px), then marginalia — with the decision keys reflowing to a two-up grid and the release key spanning full width. At ≤720px the strip nav drops to its own hairline-topped row and the docket table collapses its header, each row becoming a two-column labeled grid card between hairline dividers.

**Component Deck** (Read surface, `/shared/gallery` in the Vite app): a sticky index rail beside a long specimen column — not an app shell. Body scrolls naturally (`height: auto`); the rail is a `.nav-rail` of five grouped disclosures (Foundations, Navigation, Data, Feedback, Overlays & input) tracking the current section with the listbox selected grammar (teal tick). Overlays & input catalogs Form controls, **Date & time** (`#datetime`), Search select, Search multiselect, Option menu, and Dialog. A catalog Index key returns to `/surfaces`; there is no live operator profile. At ≤900px groups collapse to an accordion (one open at a time) with wrapping links; at ≤720px the command strip regrids like the other surfaces.

### Named Rules
**The Shared Horizon Rule.** Sibling plates in a row share one horizon: readout rows are equal flex tracks (`flex: 1 1 0`) inside identically-sized plates, each row's divider is seated exactly 12px under its value with slack absorbed after the divider, and every plate reserves a uniform key foot (min-height 58px) whether or not a key is present. The horizon is board-wide, not per-bay: while every bay holds at most one plate, plates stretch to the full bay height; the moment any bay stacks two or more, the whole board goes dense (`.bays--dense`, toggled in render) and every plate drops to content height together — plates to `flex: 0 0 auto`, rows to `flex: none`, the key foot to content height (min-height 0, 14px top padding) with keyless feet removed entirely (`:empty` hidden) — so a lone plate never balloons beside crowded neighbors. Alignment and density come from the grammar, never from hand-tuned margins.

**The Gangway-or-Bulkhead Rule.** Persistent side navigation is a gangway track (`--gangway-w` defaults to 232px and may be widened by a shell; Administrator uses 248px), folding to `--gangway-w-collapsed` at 76px channel codes. At drawer widths the shell swaps it for a leading bulkhead rather than collapsing further. Navigation never speaks amber. Adopted on the Administrator console; catalogued on the Component Deck for all other specimens.

**The Role Home Rule.** Authenticated global navigation returns to the current role’s operational home — Participant `/participant-home`, Administrator `/admin-console/enrollments` (preserving `campaign`), Reviewer `/reviewer-console`. The prototype catalog at `/surfaces` is outside product navigation; `/` redirects there. Index appears only on reference and recovery surfaces.

**The Account Action Rule.** Operator menus consume typed actions. Unavailable destinations (Profile, Preferences in this prototype) are disabled buttons with a reason — never `href="#"`. Sign out on management and guided-task shells opens a confirmation that names prototype behavior, then returns to `/surfaces`. Live-session sign-out is mediated by **Leave session**; the examination shell does not expose the normal account menu.

**The Guarded Leave Rule.** Leave session opens a native confirmation, moves focus into the dialog, and restores it to the trigger on cancel or Escape. Confirm returns to the matching Assignment Station demo state without clearing the synthetic transcript. Copy states that replies are preserved here, that the prototype timer continues while the plate is open, and that production pause/resume is runtime-owned.

**Shell-mode matrix.** Management (Home, Admin, Reviewer): command strip, role-home token, operator menu. Guided task (Journey): rail Home + compact Participant disclosure; no management strip. Live session (Session): Assignment return, Participant ID, guarded Leave session; no profile menu and no Sign out. Reference (catalog, Component Deck, Not Found): catalog Index only, no operator.

## Elevation & Depth

No drop shadows lift panels off the ground; the system is flat smoked glass. Depth is conveyed three ways: layered translucent panel fills (Ground Deep at 0.15–0.6 alpha over the fixed body gradient), the shared `--panel-inset` stack (`inset 0 1px 0 rgba(246, 252, 254, 0.05)` top edge-light plus `inset 0 -18px 34px rgba(2, 9, 14, 0.5)` bottom vignette), and phosphor glows on light-emitting elements only (`drop-shadow`/`text-shadow` in teal or amber at 5–18px blur). Overlay menus that must seat over the strip (the operator-profile menu) add a hull-ground umbra (`0 14px 30px rgba(2, 9, 14, 0.55)`) so the plate reads against the chrome beneath — overlay seating, not a card lift.

### Named Rules
**The Glass-Not-Cards Rule.** Panels never lift off the ground with outer shadows. A surface is a pane of smoked glass: 1px hairline bezel, translucent dark fill, top edge-light, bottom vignette.

**The Emitters-Only Glow Rule.** Glow belongs exclusively to things that emit light in the fiction — the core dot, the timer digits, the gauge fill and needle, a hot TRANSMIT key, and backlit Michroma signage (the brand core and bay-head placards, which glow teal as lit signs). Structural chrome — hairlines, dividers, resting keys, readout text — never glows.

## Shapes

Zero border-radius on every authored element (the only rounded things are the tiny circular node terminals and scrollbar thumbs). Corners are notched instead: `clip-path` polygons cut corners at 10–18px (`--notch: 10px` is the standard cut; the chrono panel uses 18px/12px, the TRANSMIT key a 14px angled leading edge). Notch placement is deliberate — console and station panels clip their top-leading and/or bottom-trailing corner; the home board frame and enrollment plates chamfer all four corners into an eight-sided instrument plate.

Where a chamfered panel must show its bezel on the cuts, the frame is built with the **clipped-border technique**: an outer layer filled with Hairline and `padding: 1px`, clipped at N px, holding an inner pane whose polygon is re-cut 1px inside (18px outer / 17px inner on the board frame; 10px outer / 9px inner on enrollment plates). A plain `border` + `clip-path` leaves the chamfers unstroked — the two-layer cut is the evolution the Participant Home introduced so every edge, including the diagonal, carries a visible 1px stroke.

All structure is drawn at 1px: bezels, dividers, phase traces, and frame circuitry traces. The signature ornament is the hairline trace terminating in a 5px circular node (1px border, ground-colored fill) — used on fixed frame traces, phase-spine connectors, and list tick marks. Attention-side nodes warm to amber (e.g. the chrono trace terminus, the current phase dot).

The mark vocabulary extends into a small family of **drawn instrument glyphs** — authored geometry only, never an icon font or library. Stroke glyphs are inline SVG at ~1–1.1px stroke with square linecaps (13px check ring and lock for phase states, 13×15px document sheet for preserved submissions, 34–52px seal ring for sealed/released records, 14px operator glyph as a hairline head-and-shoulders schematic). Solid marks are clip-path polygons in a single color voice (10px teal checkmark prefix on completion lines, 11px amber warning triangle on the submit confirmation, 7×10px triangular speaker indicator on the active turn). State-bearing dots reuse the 5–7px bordered-circle node: dim at rest, amber border + Amber Glow fill when live, teal border + Teal Glow fill when sealed or ready.

### Named Rules
**The Clipped-Border Rule.** A notched panel that needs a stroked bezel is two clipped layers — a 1px-padded Hairline-filled outer cut and an inner pane re-cut 1px inside — never a `border` that vanishes at the chamfer. Use it whenever the frame itself is the instrument (board frames, enrollment plates); dividers and unframed panes stay single-layer.

**The Drawn Glyph Rule.** Every icon-like mark is authored geometry — a hairline SVG stroke or a clip-path solid in teal or amber. No icon fonts, icon libraries, emoji, or Unicode symbols standing in for icons; no pictographic icon beside every label. A glyph exists only where it carries state or object identity at a glance.

**The Dashed Absence Rule.** A dashed hairline marks what is not the current record: an unoccupied bay slot's empty note, the preserved Agent original seated beneath a human revision, skeleton lines standing in for a record still in transit. Solid hairlines frame what is; dashes frame what is absent, superseded, or not yet seated. Dashed strokes never frame a live surface.

## Components

The official prototypes are the React 19 + Vite app at `prototypes/`. The five product surfaces and the Component Deck consume one shared layer at `prototypes/src/styles/`: `tokens.css` (primitives), `base.css` (reset and themed browser surfaces), then family sheets in `components/` (`keys`, `chrome`, `navigation`, `plates`, `state`, `readouts`, `fields`, `menus`, `temporal`, `searchable`, `overlays`, `datatable`, `demo`), imported from `index.css`. Surface sheets are scoped with `html[data-surface="…"]` and tighten density only. React maps semantic props onto those classes from family folders (`keys/`, `chrome/`, `overlays/`, `plates/`, `state/`, `fields/`, `navigation/`, `datatable/`, `select/`, `menu/`, `temporal/`, `glyphs/`). Import the public barrel at `prototypes/src/components`. Open `/shared/gallery` — the Component Deck — to inspect every class, variant, and interactive state. New staples land there first when they define the standard control vocabulary before a surface needs them.

### Readout Grid
Aligned record and configuration data uses `ReadoutGrid`, `ReadoutGridRow`, and `ReadoutGridField` from `prototypes/src/components/readouts/ReadoutGrid.tsx`, with shared styling in `prototypes/src/styles/components/readouts.css`. The grid supports 2, 3, 4, or 6 equal tracks and semantic field spans from 1–6. Rows choose spans by meaning while inheriting the same track geometry, creating continuous divider horizons without pretending every value has equal weight. Each row remains its own labeled `<dl>`; the visual and assistive-technology reading orders therefore agree. A named container query collapses every span to one divided row below 46rem, allowing the component to adapt inside frames, drawers, and other hosts independently of viewport width. The canonical six-track Campaign Record specimen is catalogued in the Component Deck.

### Keys (buttons)
- **Character:** engraved console keys — uppercase Michroma captions, wide tracking (0.16–0.22em), 1px bezels, square corners, no fills at rest.
- **Quiet key:** transparent, Instrument Label text, hairline border, padding from `--key-padding-*` (10px 20px) and caption from `--key-font-size` / `--key-letter-spacing` (0.68rem / 0.16em) — the **standard** size. Session and journey tighten to 10px 18px locally. Hover/focus: text and border shift to Phosphor Teal (160ms). Active: Teal Glow fill. Disabled: 0.4 opacity. **Compact** (`.key--compact`, `size="compact"`) is the dense table-toolbar scale (min-height 30px, `--key-compact-*`: 6px 12px, 0.62rem / 0.14em). **Large** (`.key--large`) is ceremony emphasis (12px 24px, 0.75rem) without changing hot-key color. **Back** (`.key--back`, `variant="back"`) is the quiet key with leading-chevron padding (8px 16px 8px 12px). Hot-key captions sit at 0.75rem.
- **Transmit key (in-session commit):** Signal Amber text and border over a faint amber gradient fill (`0.08` → `0.02` alpha), leading edge angled by a 14px clip. Hover: fill to 0.16 alpha, text brightens to Amber Bright, 18px amber-glow shadow. Active: 0.26 alpha fill. Disabled: 0.38 opacity, fill and glow removed.
- **Begin key (journey commit / ceremony):** Amber Bright text and border over a stronger pre-lit gradient (`0.38` → `0.16` alpha) with a 20px amber-glow box-shadow at rest — the hotter variant for starting the examination. Hover: fill to 0.48 alpha, shadow to 26px. Active: 0.56 alpha fill. Disabled: retains 0.78 opacity with a dimmed gradient fill (not transparent). There is at most one hot key on screen.
- **Open key (home roster commit):** the Transmit key's roster form — Signal Amber text and border over the same faint gradient (`0.08` → `0.02` alpha), 14px angled leading edge, 10px 30px 10px 34px padding at a 0.66rem caption; identical hover/active ramp (0.16/0.26 alpha fills, 18px amber-glow shadow). Exactly one OPEN key is lit per board — the board-level expression of the Amber Ration Rule, held even under load: when several OPEN plates share a bay, only the nearest deadline carries the key and the rest stay keyless until promoted. Other plate actions (Resume, View) are quiet keys, and every Released plate may carry its own.
- **Inspect key (docket commit):** the Transmit form applied to opening the next evaluation — Signal Amber text and border over the faint gradient (`0.08` → `0.02` alpha), 14px angled leading edge, 10px 26px 10px 34px padding at a 0.64rem caption; the standard hover/active ramp (0.16 alpha fill, Amber Bright text, 18px amber-glow shadow). Exactly one INSPECT key is lit per docket — the oldest awaiting review carries it (its row takes a faint amber wash); every other row's action is a quiet Open or View key.
- **Release key (record commit):** the record's one hot key — same amber Transmit anatomy at 10px 30px 10px 38px padding and a 0.66rem caption, captioned APPROVE & RELEASE in the decision bar and RELEASE RESULT in the confirm dialog. Hover: 0.16 alpha fill, Amber Bright, 18px amber glow. Active: 0.26 alpha. Disabled (rejected or already released): 0.38 opacity with fill and glow removed.
- **Activate key (campaign commit / ceremony):** the hot key's double-stroke form — Amber Bright text on a 1px Signal Amber border with a second inset amber stroke (`inset 0 0 0 1px` at 0.45 alpha), an inset amber glow fill (16px at 0.22 alpha), a pre-lit gradient (`0.18` → `0.06` alpha), and a resting 14px outer amber glow; same 14px angled leading edge, 10px 30px 10px 34px padding at a 0.66rem caption. Hover: fill to 0.28 alpha, both inset strokes and the outer glow intensify (0.6 / 0.3 alpha, 22px). Active: 0.36 alpha fill. Disabled (post-activation, relabeled "Activated"): 0.38 opacity with fill and all glows removed.
- **Focus (all keys):** global 1px Phosphor Teal outline, 3px offset.
- **Waiting (occupied):** `.is-waiting` on a disabled key with `aria-busy`. Pointer-events none, cursor progress, opacity held at 1 — occupied, not the disabled fade. Quiet keys stay in the teal system voice. Hot keys drop amber: Phosphor Teal text and border over Teal Glow fill, outer glow removed. Seat a wait-mark in the key's 10px gap. Reduced motion holds the mark still.

### Pane (smoked-glass primitive)
The reusable surface every plate composes from: `.pane` carries a 1px Hairline bezel, the shared sheen over `--pane-fill` (defaults to `--panel-depth`), `--panel-inset`, and per-corner `--cut-tl / --cut-tr / --cut-br / --cut-bl` clip-path cuts. Modifiers: `.pane--dim` (dim bezel), `.pane--tl`, `.pane--br`, `.pane--notched` (leading top + trailing bottom cuts at `--notch`), `.pane--chamfer` (18px eight-cut). Consumers set geometry vars and layout locally — protocol plates, ledger frames, agent panels, briefing glass, and confirm plates all build on `.pane`.

### Panels / Plates (containers)
- **Corner style:** square with notched clips (10–18px cuts on select corners), usually via `.pane` or the clipped-border `.frame-cut` technique.
- **Background:** shared smoked-glass stack — a faint 115° sheen gradient over a depth gradient (`--panel-sheen`, `--panel-depth`), used consistently on chrono, ledger, composer, briefing, confirm, and complete plates.
- **Depth:** inset top edge-light and bottom vignette per the Glass-Not-Cards Rule (`--panel-inset`); never outer shadows.
- **Border:** 1px Hairline for framed panels; Dim Hairline for internal dividers.
- **Internal padding:** 16–24px for live panels, 30–46px for ceremony plates.
- **Operate area:** Administrator walls compose `OperateArea` (`plates/OperateArea.tsx`) — shared operate-head + etched frame recipe. Surfaces own wall class names; they do not restyle the frame cut.

### Composer (input)
- **Style:** a bezeled glass slot — 1px hairline, notched top-left corner, transparent textarea in Bright Text (0.95rem mono), 20px 22px padding, placeholder in Dim Label.
- **Focus:** the whole slot's bezel shifts to Phosphor Teal at 0.6 alpha (180ms); the textarea itself carries no outline. Caret is amber.
- **The TRANSMIT key sits inside the slot,** sharing its right edge (its own top/right/bottom borders removed).

### Checkbox (acknowledgment mark)
- **Style:** 17px square, 1px Instrument Label border, native input hidden. Checked: amber border, Amber Glow fill, amber clip-path checkmark. Focus: teal outline on the mark. Amber is the **commitment voice** — consent and ceremony acknowledgment only. Row selection uses the teal **select-mark**, never this control.

### Select mark (row selection)
- **Style:** same 17px square anatomy as the acknowledgment mark, but teal — checked: teal border, Teal Glow fill, teal clip-path checkmark; **partial:** centered dash (`.select-mark--partial`); **page:** filled inner square (`.select-mark--page`); **matching:** checkmark across the full matching set (`.select-mark--matching`). Focus: teal outline on the mark.
- **Character:** selection speaks teal, same voice as radio and option ticks. Lives in the datatable's select column (`.select-head` / `.select-cell`); never amber, never used for consent. Header selection escalates page → matching → clear through the shared four-state control; there is no separate toolbar escalation key.

### Radio mark (selection control)
- **Style:** 15px circular bezel (the only round control besides node dots and scrollbar thumbs), 1px Instrument Label border, native input hidden. Checked: teal border, Teal Glow fill, inner 7px teal dot with glow. Focus: teal outline on the mark.
- **Character:** selection speaks teal — the chosen option in a group, never the amber commitment voice of the acknowledgment mark.

### Breaker (toggle switch)
- **Style:** a square breaker, never a pill — 34×18px hairline track with a 12px square thumb that slides 16px when closed. Rest: dim thumb on Instrument Label border; checked: teal border, Teal Glow track fill, teal thumb with glow. Focus: teal outline on the breaker.
- **Character:** the on/off circuit for preferences and settings; same teal selection voice as radio and option ticks.

### Transcript Ledger (signature component)
The examination record: a bezeled scroll panel containing channel cards. Each turn is a single glass card (75% width, notched corners) with an uppercase microlabel speaker row, prose body, and timestamp; the index badge is a small tabular mono mark pinned to the card's top-right inside the bezel. Agent cards align left with an amber wash; participant cards align right with teal glass. The active question warms its border and speaker mark to amber (agent) or teal (participant), adds a 7×10px triangular speaker indicator, and promotes body text to Bright Text without enlarging type. Arriving turns reveal with a 640ms phosphor sweep (`clip-path` inset animation; agent sweeps from left, participant from right). A thinking placeholder row shows uppercase ellipsis text with a 1.4s opacity pulse.

### Agent Panel (signature component)
The examiner station: a 320px right-column smoked-glass plate with notched bezel framing the Agent core, reassurance copy, and chrono instrument stack as one vertical instrument. Agent post and chrono are separated by a dim hairline; stage readout and Submit key sit at the chrono foot. On tablet (≤1180px, 300px wide) the panel shares a row with the session column; on mobile (≤760px) it compresses to a compact band above the transcript — a 72px core post on top, then an inline row of timer digits (1.75rem), 56px gauge, and stage/Submit.

### Agent Core (signature component)
The examiner's presence: a 118px living phosphor orb — layered radial glows, a 54px shell, and a hairline ring, drifting on slow aura/core pulses (6.2s / 3.55s at idle). **Idle is phosphor teal; thinking shifts the entire orb to signal amber** (480ms color transition, faster pulse at 2.13s / 1.8s) while keeping the same size and silhouette. Reassurance copy below is clamped to two lines (max 24ch; one line on mobile) so the core never compresses when the message changes. The orb scales to 96px at ≤1180px and 72px at ≤760px.

### Chrono Gauge (signature component)
The timer instrument inside the examiner panel: amber digits (Display role) above a 100px SVG arc gauge — hairline track, 3.8px amber fill with drop-shadow glow, tick marks, mono numerals, amber needle. Stacked vertically with stage readout and Submit key in the panel foot. The time warning pulses an inset amber ring on the examiner panel (1.2s × 3) and brightens the digits; it never turns anything red.

### Stage Progress Bars (stage instrument)
Five 3px segment bars under the stage readout — now in the shared layer (`prototypes/src/styles/components/state.css`; the session chrono only adds side pad). One bar per examination stage: completed stages fill teal at 75% alpha, the current stage fills amber with a 6px Amber Glow shadow (the family's only amber), remaining stages stay Dim Hairline. Segments shift with a 200ms transition; on session complete all five settle teal. On mobile the strip runs full-width across the panel foot on its own row. Catalogued on the Component Deck with the wait family.

### Wait & Progress (signature instruments)
Loading is a Shipboard instrument, never a spinner. The family lives in the shared layer and is catalogued on the Component Deck. Teal is the system wait voice; amber stays on the current stage bar only. `prefers-reduced-motion` kills every loop and leaves the geometry seated (node fully lit, scan line centered, indeterminate fill parked at 32% and centered, skeleton wash held).

- **Wait-mark:** a 14px square hairline bezel (22px large) holding a pulsing teal node (inset 4px / 7px large, 8px teal glow, 1.4s opacity) and a 1px teal scan line sweeping the square (1.1s, 12px travel; 20px on large). Authored geometry, not a rotating ring.
- **Wait-copy:** uppercase mono at 0.88rem / 0.12em tracking in Instrument Label, 1.4s opacity pulse (0.35–1) — in-place status copy such as a composing line, not a spinner caption.
- **Scan-track:** a 3px Dim Hairline rail (max 280px; 220px inside a wait-plate). Determinate fill is Phosphor Teal at `width: calc(var(--scan, 0) * 100%)` with an 8px teal glow and 200ms width ease; pair with a teal tabular `.scan-readout`. Indeterminate (`.is-waiting`) locks the fill at 32% and shuttles it along the rail (1.6s alternate) so the segment never leaves the track. Never amber.
- **Skel-stack / skel-line:** a 10px-gapped stack of 8px dashed Dim Hairline lines (width via `--skel-w`, default 100%, max stack 420px). A teal-glow sweep crosses each line (1.4s). Dashed per the Dashed Absence Rule: the record that has not arrived, not a filled shimmer card.
- **Wait-plate:** the empty-state plate's anatomy applied to transit — 10px eight-cut hairline pane (max 520px, 44px 52px padding, sheen over 0.45-alpha dark wash, panel inset), large wait-mark, Michroma label (0.78rem / 0.3em), mono note (max 44ch), and an indeterminate scan-track. `role="status"` / `aria-busy` / `aria-live="polite"`.

**The Instrument Wait Rule.** Loading is drawn as a console instrument in the teal system voice — wait-mark, scan-track, skel-stack, wait-plate, occupied keys — never a spinner. Amber stays on the current stage bar only. Reduced motion holds the geometry still.

### Link Status (connection readout)
A right-aligned footer instrument below the composer: Michroma **Link Nominal** label (0.55rem, 0.2em tracking) beside six equal-height teal signal bars (1em tall, baseline-aligned). System-state voice only — no hex icon, no amber. Bars use `opacity: 0.8` lit / `0.22` off when animated.

### Phase Spine (signature component — assignment journey)
The journey navigation (`prototypes/src/features/journey/PhaseSpine.tsx`): a vertical list of phase nodes connected by 1px hairline traces. Each node is a full-width interactive row with a state marker, Michroma phase label, and mono short description. The marker is a drawn glyph per state: **complete** phases carry a 13px teal check-ring glyph (1.1px stroke, teal-glow drop-shadow); **current** phases keep the 7px circular dot warmed to amber with Amber Glow shadow and Amber Bright label; **locked** phases carry a 13px dim lock glyph and dim to label-dim. The viewing state adds a hairline border and faint amber wash. On tablet the spine wraps into a horizontal row with shortened traces.

### Well Frame (assignment content panel)
The current-phase content well: a bezeled scroll panel with a 14px bottom-trailing notch, shared smoked-glass stack, and 32–38px internal padding (max 78ch prose). Section headings are teal uppercase mono led by the same 7×1px teal tick that marks list items. Completion lines ("Briefing acknowledged and recorded") prefix a 10px teal clip-path checkmark; a released result opens its well head with a 34px teal seal ring (the session complete plate's mark, scaled down). Versioned submission rows carry a 13×15px hairline document glyph — teal with glow on the current preserved version, dim on superseded ones. Instrument plate labels lead with a 5px bordered node mark matching the trace terminals. Phase transitions can play a 640ms `clip-path` reveal (`well-reveal`). Acknowledgment marks reuse the shared checkbox pattern.

### Ceremony Plates (briefing, confirm, complete)
Full-screen or modal smoked-glass plates for pre-session briefing, submit confirmation, and session-complete states. Briefing uses a centered overlay scrim (hull-dark at 0.97–0.985 alpha) with a 680px max plate and 40–44px padding, and opens its head with a **ceremony trace**: a 76px hairline with a centered 5px amber node, echoing the frame traces. Confirm and release ceremonies use the shared `.dialog` root over the 82% scrim; titles lead with the 11px solid amber warning triangle. Complete is an in-ledger centered `.pane` plate with a teal SVG checkmark ring. All share the notched bezel, panel sheen/depth, and hairline dividers between head, body, and foot sections. The campaign-configuration ceremony is a `.dialog` over a lighter scrim (`rgba(4, 12, 17, 0.45)`): a 620px clipped-border plate (18px top / 12px bottom-trailing cuts, stroked per the Clipped-Border Rule, 34px 44px 30px padding) opening with the same amber-node ceremony trace under a Michroma title, its body a hairline-divided form of dropdown selectors and field inputs ending in a Cancel quiet key and the one Activate key.

### Status Readout (state node)
The assignment header's Phase/Record readout pairs each Record value with a 7px state node dot: amber border + Amber Glow fill when the session is live, teal border + Teal Glow fill when ready, sealed, or released, dim border at rest. The dot is the glanceable state; the mono text names it.

### Command Strip (home management chrome)
The full-width top chrome of the home, admin, reviewer, and Component Deck surfaces: one stretch-aligned row of hairline-divided segments over a dark vertical gradient (rgba(2,9,14) 0.6 → 0.25), closed by a bottom dim hairline (min-height 48px). Left: the brand placard — the canonical wordmark **FLEX AGENT** (never "Flex Agent Systems" or any other suffix) in Michroma 0.7rem at 0.22em tracking in Phosphor Teal with a layered teal text glow (14px + 30px), a backlit sign in the fiction. On rail shells (session, journey) the same wordmark stacks over a small mono designation line (0.5rem, 0.32em, Console Text at 0.82 opacity) naming the console — "Examination Console", "Assignment Station" — the rail's counterpart to the strip's mode placard. Center: role-home nav tokens (Michroma 0.75rem, 0.12em, uppercase) in Instrument Label at rest, teal on hover/focus; the current token brightens to Bright Text and seats a 2px teal underline bar on the strip's bottom hairline (inset 22px from the token edges, 10px Teal Glow shadow) — the only current-page marker. Authenticated surfaces never link to the prototype catalog from this slot. Right: the **ident cluster** — optional mode placard then the operator profile. The Component Deck seats a Component Deck mode placard beside the wordmark (`.strip-brand--origin`) and a catalog Index key plus `.strip-readout` — no operator profile on the page chrome; profile specimens live in the strip section below. At ≤720px the strip regrids: brand and profile share the first row, the mode placard hides, and nav drops to its own full-width hairline-topped row that scrolls horizontally. The station reciprocates with a quiet **rail home back-link** (0.75rem) stacked under the brand, then a full-width Participant ident plate — not a strip-profile cell jammed beside Home.

On the reviewer surface the strip carries a Review role-home token and operator profile.

On the administrator surface the command strip carries the same unframed `.brand-mark` as the other management shells, an ADMIN suffix, a Home role-home token that preserves a valid selected `campaign` query, and the operator profile. Campaign selection is not strip or side-nav chrome: the Campaigns area opens as a registry, and Enrollments selects the working campaign from the shared page-local Campaign Context. This keeps grouped Gangway destinations stable and separates where the operator is from what campaign they are working on.

### Strip Profile (operator disclosure)
One compact trigger in the ident cluster: a 14px drawn operator glyph (hairline head-and-shoulders schematic in currentColor — never a photo, never an avatar), a short tabular mono ID (0.75rem at 0.12em), and a chevron. Rest: Instrument Label. Hover/focus: Phosphor Teal. Open: teal text over Teal Glow fill, chevron flips 180°. The menu drops from the strip (min 236px, hairline bezel, near-opaque ground at 0.94 alpha over panel inset, plus the hull umbra so it reads over the chrome): a non-interactive head (Michroma role 0.52rem / 0.22em in Dim Label over the full tabular ID in Bright Text) then typed action rows — disabled Profile and Preferences with a 0.68rem reason line, and an enabled Sign out row closed behind a full Hairline. Keyboard navigation skips disabled items. On Assignment Station the same contract uses `.strip-profile--rail`: Home stays a quiet back-link; identity is a hairline ident plate on its own row (full rail width on desktop, a trailing cell on mobile) whose menu opens under that plate rather than overlapping Home. Live session uses Leave session instead. Sign out confirmation uses the shared `.dialog-plate--narrow` ceremony (warn triangle, head / body / foot), not the session-local `.confirm-plate`. The Component Deck catalogs the strip specimen. On Administrator mobile, the drawer-bar Menu key sits above the profile overlay (z-index 45) so the first click is never swallowed.

### Nav Rail (shared side navigation)
Vertical nav list in the listbox selected grammar: uppercase mono links (0.72rem / 0.08em) over Dim Hairline dividers, teal on hover, Bright Text plus the 7×1px teal tick on the current item. No popover plate — layout (width, sticky, border) stays on the consuming shell. Teal selection voice only — navigation is never amber. Optional traced spine (`.nav-list--traced`): 5px node terminals on 1px hairline connectors, current node warming teal with glow; locked items carry the 13px dim lock glyph. The Administrator Bulkhead uses this list for grouped area navigation; the Component Deck catalogs both standard and traced forms.

### Gangway (persistent collapsible side menu)
The in-layout counterpart to the bulkhead drawer: a full-height side-menu column the shell seats as a grid/flex track. Width rides `--gangway-w` (232px shared default; 248px in the Administrator shell); the toggle key folds it to `--gangway-w-collapsed` (76px) — a rail of engraved channel codes (`.gangway-abbr`, 0.6rem / 0.14em) whose full names speak through the trailing tooltip plaque (`data-tip` + `.tip-trailing`, silent while expanded). Selection keeps the teal tick voice. Head: Michroma title (0.58rem / 0.24em) plus a 30px hairline toggle with a chevron that points at the fold edge when expanded and back out when collapsed. The 240ms width transition is deliberate — the fold must reflow the shell's content track — and is disabled under reduced motion. Collapsed, the foot hides; a shell with a menu too long for its height scrolls the gangway body (head stays pinned; nav sections and links keep natural height) and drops the tips. React `Gangway` in `prototypes/src/components/navigation/Gangway.tsx` is the product-shell implementation (`navigation/index.ts` is the barrel). Items name stable product areas; narrower working context belongs inside the active page. The Component Deck renders the same React component and class contract. At drawer widths the shell swaps the gangway for `.bulkhead--leading` rather than collapsing further; the Bulkhead repeats the same area navigation.

### Bulkhead Drawer (off-canvas panel)
Smoked-glass off-canvas panel over an 82% ground scrim (`rgba(2, 9, 14, 0.82)`). `.bulkhead--leading` slides from the left (navigation); `.bulkhead--trailing` slides from the right (marginalia, readouts). Width rides `--bulkhead-w` — 280px default for navigation weight; `.bulkhead--wide` (420px) carries field work like forms and adjust ceremonies. The panel uses the shared glass stack with a 10px notched leading/trailing clip, hairline-divided head / body / foot (Michroma title 0.62rem / 0.22em). Escape, scrim click, and Close dismiss; focus returns to the trigger. Open/close is a 320ms transform (disabled under reduced motion). Pair with `.nav-rail` inside. Adopted on the Administrator console (leading area nav at ≤1080px) and Reviewer console (record rails on narrow viewports); catalogued on the Component Deck for specimens.

### Panel Tabs (in-page tab set)
Distinct from command-strip navigation: a hairline-underlined row of uppercase mono tokens (0.66rem / 0.14em) with the same 2px teal underline current marker and an opacity panel switch (200ms; none under reduced motion) — never filled tab backgrounds. Catalogued on the Component Deck. The Administrator campaign list is a registry table, not this tab set.

### Console Footer (quiet page foot)
Hairline-topped bar over a darkening gradient (10px 22px 12px padding) carrying a Dim Label tabular readout (`.console-foot-readout`, 0.62rem / 0.14em uppercase) and optional action cluster. Shared by home, admin, reviewer, and the Component Deck. At ≤720px the foot stacks vertically.

### Board Frame (home instrument frame)
The single etched frame holding the whole roster: an 18px/17px clipped-border cut (per the Clipped-Border Rule) around a smoked-glass pane carrying the shared sheen, depth, and inset stack. The frame is instrumented like the fixed traces: 46px etched double-edge ticks centered on its top and bottom edges (1px side rules over a ground-deep fill), and 5px circular nodes seated on its right edge at 15% and 85%. The board's one authored moment is a 640ms clip-path reveal of the frame on load. When the roster is empty the frame holds a centered **empty-state instrument plate**: a 10px-notched hairline pane (max 520px, 44px 52px padding) opening with a 7px dim node mark above a Michroma label (0.78rem, 0.3em) and a mono note (max 44ch) — the empty state is still an instrument, never bare text.

### Status Bay
A labeled Record-state column inside the board frame — one per state (OPEN, LIVE, PENDING RELEASE, RELEASED), divided by dim hairlines. The bay head is a glowing Michroma placard: 0.66rem at 0.3em tracking, centered, Phosphor Teal with a layered teal text glow (10px + 24px) — backlit signage naming the bay, seated 24px above the plate stack. Below it the plate stack scrolls independently: a stack that exceeds the bay's fold scrolls inside its own bay (`overflow-y: auto`, `overscroll-behavior: contain`) while the board frame stays fixed. A bay with no enrollments shows a dashed-hairline empty note (0.62rem uppercase Dim Label, centered, `border: 1px dashed`) — dashed per the Dashed Absence Rule, marking an unoccupied slot rather than a surface.

### Enrollment Plate (signature component — home roster)
One assignment as a physical plate racked into a bay: a 10px/9px clipped-border cut around a glass pane (sheen over a 0.4-alpha dark wash; released plates swap the wash for a faint 0.05-alpha teal tint). Inside, a `<dl>` readout follows the shared row grammar of the Shared Horizon Rule: each row is an equal flex track with a teal microlabel over its mono value (dt 0.56rem at 0.16em, 0.85 opacity; dd 0.82rem at lh 1.45) and a dim hairline divider drawn 12px under the value; the assignment title row promotes its value to Bright Text with balanced wrapping. The Record row seats an instrument mark beside its value: **live** sessions carry a 7px solid amber node — border and fill both amber with a 9px glow, the fully-lit form of the state node dot; **released** results carry a 22px teal seal ring, a double SVG circle (dashed outer ring, `stroke-dasharray: 2.4 2.2`, 1.2px stroke) around a 1.8px square-capped check with a teal-glow drop-shadow — the well frame's seal mark miniaturized for the roster. Every plate ends in the reserved key foot (min-height 58px, centered) holding one key or nothing. Plates obey the Shared Horizon Rule's density switch: on a one-per-bay board they stretch to the full bay height; once any bay stacks, every plate on the board renders at content height and keyless feet collapse.

### Toolbar Segments (manifest controls)
Hairline-bordered instrument segments (1px Dim Hairline) sharing edges — an adjacent segment drops its left border so a run reads as one bezeled strip. Forms: a **readout segment** (dim key label beside a tabular mono value, 0.66rem uppercase at 0.14em tracking, 9px 16px padding, `aria-live` for the count readout); a **dropdown segment** (a borderless `.seg-key` that warms to teal on hover and holds a Teal Glow fill while its listbox is open, with a drawn 10×6px chevron at 1.1px square-capped stroke, dropping a hairline `.seg-menu`); and a **search slot** (borderless mono search input, 170px, uppercase Bright Text with dim placeholder; the segment's own border warms to teal at 0.6 alpha on focus-within). Sort lives on the column heads, not in the toolbar. A flexible spacer separates reading segments (left) from acting segments (right). Enrollments reuses the dropdown segment inside the page-local Campaign Context rather than mixing campaign scope into manifest controls. Campaign configuration is summoned from the Campaigns record frame foot (quiet key), not from the enrollment toolbar.

### Manifest Table (signature component — record wall)
The dense datatable grammar (`.datatable-frame` + `.datatable` / `.datatable-table`): a mono table at 0.72rem / 0.06em tracking with tabular-nums throughout, set `border-collapse: separate` with `border-spacing: 0` — separate borders because collapsed borders do not travel with sticky headers. The Component Deck is the visual contract. One shared `--datatable-inline-gutter` defaults to 18px and aligns toolbar, table, expanded detail, and pagination across every surface; page selectors must not customize it. Height fits the current page's rows and caps at `--datatable-max-height`, after which the scroll region takes over. Column heads are sticky (opaque Ground Deep fill, no alpha — a translucent fill would ghost scrolled rows through the head — 1px Hairline underline) sortable keys — mono uppercase teal at 0.62rem / 0.16em, weight 500, resting at 0.85 opacity and lifting to 1 on hover. Sort lives on the heads: multi-column by default (each head click adds or cycles a key; rank numbers sit on the heads as `.col-key-rank` when more than one column is active). Pass `singleSort: true` to restrict sorting to one column at a time. The sort mark is a 7×5px clip-path triangle in currentColor, visible only on a sorted column (rotated 180° for ascending), with `aria-sort` carried on the `th`; sorted heads brighten to Bright Text. Static `.col-head` spans keep the same col-key voice without sort affordance, for manifests whose order is fixed (the reviewer docket). Body rows are 7px-padded nowrap cells over Dim Hairline dividers; hover paints the row with teal glass (0.045 alpha) and lights a 7×1px teal tick before the participant ID. Selected rows (`.is-selected`) deepen the teal wash to 0.08 alpha (0.1 on hover) and keep the tick lit. The SESSION STATE column speaks in instrument marks per the Instrument Mark Rule: the 7px hollow dim node at rest, the solid amber node (amber border and fill, 9px glow) for a live in-progress session, and the 13px teal check-ring glyph (1.1px stroke, teal-glow drop-shadow) once complete. The RESULT column keeps state in the two voices: Amber Bright text while in progress, teal for a released result, dim label otherwise.

The Deck specimen and Campaign Registry share a persistent **actions strip** (`.datatable-actions`) above the filter row. Table-level actions (gallery-only Create in the Deck) stay enabled at rest; primary bulk keys (Export, Download) and the compact **More** overflow trigger always render but disable at zero selection with “Select one or more {noun}.” Once rows are selected a compact **selection band** (`.datatable-selection-band`) appears directly under the filter row — mono uppercase readout plus an unframed **Clear** text action (`.clear-action`) — while the actions strip stays keys-only. Matching escalation lives in the header checkbox cycle (none → partial → page → matching → clear), not a separate toolbar key. Only the object identifier opens the canonical record; ordinary cells and row whitespace remain inert and text-selectable. The shared borderless `.icon-button` (22px visible over an invisible 44px target, reduced to the row's 34px target inside dense manifests) is the dense-row glyph control. Expansion uses a chevron (`ChevronGlyph`) with a row-specific Expand/Collapse name. Row overflow uses a horizontal-ellipsis glyph (`ActionMenuGlyph`), `More actions` tooltip, and the portalled `.command-menu` through `RowActionMenu` with flush `placement="fixed"` so the panel can leave the scrolling table. The Deck specimen and Campaign Registry use this same overflow control, never a compact `.key` in the action column, so baseline row height stays 35px. `useTableController` owns generic filter/sort/page slicing; consumers own query reset, selection, and domain actions. Selection is page-first, then all-matching via the header; search and filter clear it, sort and paging preserve it. Bulk actions are all-or-nothing — mixed eligibility disables the action with a reason rather than skipping records. Destructive actions open the shared confirm dialog with one amber commit key. The Enrollment Manifest keeps expanded-row inspection and does not yet promote bulk mutation keys. A **pagination foot** (`.datatable-foot`) closes both: a tabular range readout, a rows-per-page dropdown, a page selector, and prev/next step keys. Foot menus open upward so the pane's clip-path never clips the listbox. A filtered-empty manifest shows the shared empty-state instrument plate (10px-notched hairline pane, node mark, Michroma label, mono recovery note) inside the scroll region.

### Expanded Enrollment Object (signature component)
The record wall's one-object expansion: activating a row takes it out of the table flow and re-renders it inside a single clipped-border frame — an outer Hairline cut notched at the top-left and bottom-trailing corners (10px outer / 9px inner, per the Clipped-Border Rule) around a glass pane (sheen over a 0.45-alpha dark wash, panel inset). The frame bleeds by the full shared gutter on both sides (`margin-inline: calc(-1 * var(--datatable-scroll-gutter))`), so its background meets the etched table frame with no page-dependent sliver. A Dim Hairline divider separates the source row from the detail body (a matched 6px collar above and below the cut; 16px even padding inside the pane, left inset synced to the first column edge): a compact content-width **readout band** (`inline-flex`, never stretched across the frame) of teal-microlabel-over-Bright-Text cells divided by Dim Hairline rules — including a closing right divider — over a bottom hairline that ends with the band; below it, a foot of quiet keys (14px gap, 16px top padding) at the band's inset. The whole object is keyboard operable. Expansion reveals with a quiet 320ms opacity ease — no sweep; on mobile the band relaxes to full width (bottom hairline dropped) and cells wrap two-up with bottom dividers.

### Review Docket (reviewer queue)
The reviewer's manifest: a glowing Michroma title (REVIEW DOCKET, 0.78rem at 0.28em with teal text glow) and mono note over one clipped-border etched frame (18px/17px two-layer cut per the Clipped-Border Rule, 46px centered edge ticks top and bottom) that hugs its rows. Inside, the docket declares the shared footerless `.datatable--body-only` variant and consumes the datatable grammar (`.datatable` / `.datatable-table`): sticky heads on an opaque Ground Deep fill, but as static `.col-head` spans — the col-key voice (mono uppercase teal, 0.62rem at 0.16em, 0.85 opacity) without sort affordance, because receipt order is fixed. Body rows are the shared dense cells (7px padding, Dim Hairline dividers, teal-glass hover at 0.045 alpha); ordinary cells and row whitespace remain inert, while the candidate identifier and explicit Open/View key provide record access. Candidate IDs read in Bright Text tabular-nums; assignment titles wrap at a 28ch measure; mean confidence reads teal. The review-state column pairs the 7px state node with an uppercase state label — amber solid while work is demanded (awaiting, adjusted, escalated), teal solid once released, dim otherwise. The docket enforces the Amber Ration at queue level: the oldest awaiting session takes a faint amber row wash (0.05 alpha) and the one lit INSPECT key; all other rows act through quiet Open/View keys. An empty docket shows the shared empty-state instrument plate (DOCKET CLEAR). At ≤720px the docket refuses the datatable's horizontal-scroll adaptation (that suits 100-row walls): the header row hides and each row re-lays as a two-column labeled card — every cell restating its column as a teal microlabel via `data-label` — with the row wash painting the whole card.

### Overlay Ledger (signature component — reviewer record)
The evaluation record: the docket row unfolds (640ms clip-path, top-down; reduced-motion cuts straight) into a full-shell record where the human review rides over the sealed exam. The record head carries the Docket back key, the record title beside a **sealed mark** (solid 7px teal node + Michroma SEALED at 0.55rem/0.24em), and the tabular session ID. Below, the three-column grid: the **manifest rail** stacks hairline-divided readouts (candidate, campaign, rubric, agent revision, harness snapshot, review state) over the preserved-submission list (13×15px document glyphs — teal with glow on the preserved version, dim on superseded); the **sealed transcript** re-renders the examination ledger inside a notched hairline frame with top/bottom fade masks — turn cards at 88% width, agent left with amber wash, participant right with teal glass, index badges pinned top-right; the **marginalia rail** holds the criterion plates. When a criterion is active, its cited turns take the cited treatment (border warmed toward amber, inset amber stroke, speaker mark colored with the 7×10px triangular indicator, body promoted to Bright Text) while every uncited turn dims to 0.38 opacity — the record literally focuses on the evidence. Clicking the transcript clears the focus.

### Evidence Tethers (signature component)
The reviewer's one memorable moment: hairline evidence paths drawn in an SVG layer spanning the record grid, connecting each criterion plate to the transcript turns it cites. Each tether is a 1px cubic curve from the plate's left edge to the cited turn's right edge, terminated by 2.5px circular nodes at both ends — the circuitry-trace ornament made load-bearing. At rest tethers read teal at 0.45 opacity; activating a criterion lights its tethers amber at 0.85 with a 4px amber drop-shadow and scrolls the first cited turn to center. Tethers redraw live on ledger scroll, marginalia scroll, and resize, so the paths stay pinned to their evidence.

### Criterion Marginalia (signature component)
One criterion evaluation as a plate racked beside the transcript: a notched (10px) dim-hairline glass pane (sheen over a 0.42-alpha dark wash, panel inset) that is itself the tether control — hover warms the bezel toward teal, the active plate warms it toward amber over a faint teal wash. Inside: a Michroma criterion label opposite a large mono score (1.1rem, 600, tabular), a teal LINKED TO microlabel naming cited turns, the rationale in prose, and a mono confidence readout that turns Signal Amber below 0.70 (with an amber uncertainty line when the Agent flagged one). A human revision preserves the record: the **Agent original** (score and rationale) seats beneath a dashed top divider per the Dashed Absence Rule — superseded, never erased. Adjust mode reveals in-place score and rationale fields (dark slot fills, teal focus border) inside every plate; saving records the revision and re-renders the docket state to Adjusted.

### Decision Bar (reviewer commit chrome)
The record's foot: a hairline-topped bar over a darkening gradient holding a mono decision note (0.72rem, max 44ch, restating the governance stance for the current state) and an equal-width key grid — Adjust, Reject, Escalate as quiet keys beside the one APPROVE & RELEASE key (`repeat(3, minmax(0,1fr)) minmax(0,1.15fr)`, 42px minimum key height). Adjust toggles to Save adjustment with `aria-pressed`; Reject disables release; release opens the confirm ceremony. Once released, the commit keys withdraw entirely — only the quiet Docket path remains — and the note records that the result is sealed for audit inspection. On mobile the keys reflow two-up with the release key spanning full width.

### Dropdown Selector (configuration select)
The console's select control: a full-width bezeled key — 1px Hairline over a dark slot fill (ground at 0.55 alpha), mono uppercase Bright Text at 0.78rem / 0.1em, 10px 14px padding — holding the current value and a drawn 10×6px chevron (1.1px square-capped stroke, dim at rest). Hover deepens the slot fill; open or focus warms the bezel to teal at 0.6 alpha, and the chevron flips 180° and turns teal while the listbox is open. The menu drops from `.dropdown-menu` and uses the shared option-menu grammar below (0.74rem / 0.1em rows — `--select-option-size` / `--select-option-tracking`). Fully keyboard operable (arrows, Enter, Escape). Implemented in `prototypes/src/styles/components/menus.css` and `fields.css`; `DropdownSelect` (field default, `toolbar` for datatable segments) and `DisclosureMenu` consume it. Optional `clearable` selects keep value rows in the listbox and put an unframed **Clear** text action (`.clear-action`) in the foot.

### Option menu (shared listbox grammar)
The listbox rows behind every **single-select** menu: hairline row dividers, teal-glass hover/focus (Teal Glow fill), Bright Text on the selected option with the 7×1px teal tick prefix. Type is the field-menu scale (`--select-option-size` 0.74rem / `--select-option-tracking` 0.1em). Class `.option-menu`; positioning and width stay on the consumer (toolbar `.seg-menu`, `.dropdown-menu`, deck specimens).

Ground and sheen live on the consumer plate: `.popover-surface` carries the sheen/umbra stack. Nested `.select-popover > .option-menu` is a transparent inner list. Keyboard focus paints `.is-focused` / `:focus-visible` with the same teal-glass as hover. **Selected rest is the tick, not the glass.**

**Selected-mark families — all teal, never amber:**
- **Tick** — option-menu rows and nav-rail current.
- **Select-mark** — searchable multiselect (square + check); the option-menu tick is suppressed.
- **Inset bezel** — calendar day and time-wheel selected; wheels keep option-menu hairline + teal-glass hover and hide the tick.

### Command menu (action cousin)
`.command-menu` shares the popover surface, hairline rows, and teal-glass hover. It is not a listbox: no tick, no `.option-menu`. Rows are commands (View, Transcript, destructive labels stay Instrument Label). Catalogued on the datatable overflow, not as an option-menu specimen. `DropdownMenu` (`menu/`) is the shared `role="menu"` shell: connected select-like seam for toolbar More; flush `placement="fixed"` for row overflow.

### Searchable select & multiselect
Field and context plates (`select/`) commit on row pick and close; the foot key is Close. The committed option stays listed when the filter would hide it. Single-select rows keep the option-menu tick. **Searchable multiselect** (`.multiselect.select-shell--field`) reuses the same trigger and popover: combobox search, `aria-multiselectable`, teal `.select-mark` rows (tick suppressed), result and selection readouts, Clear, and Done. Pass `caseSensitive` for exact-case filtering; the default matches case-insensitively.

### Dialog (ceremony plate root)
Native `<dialog class="dialog">` over an 82% ground scrim (`rgba(2, 9, 14, 0.82)`); lighter scrims (`rgba(4, 12, 17, 0.45)` on the campaign ceremony) when the incumbent surface must stay readable beneath. The plate is `.dialog-plate` — a 10px eight-cut (`--notch` on all four corners), shared glass stack, hairline dividers between `.dialog-head`, `.dialog-body`, `.dialog-readout`, and `.dialog-foot`. Width via `--dialog-w` or a named size: `--narrow` (412px) for a single-question confirm, default 520px, `--wide` (680px) when the body carries a form. A body taller than the viewport scrolls inside the plate; head and foot stay seated. Titles lead with the 11px solid amber warning triangle; readout pairs are teal microlabel over Bright Text. Session confirm, admin ceremony, reviewer release, and the deck specimen all share this root; surface-specific padding and cut depth stay local.

### Toast (instrument slip)
Transient messages dock bottom-right in `.toast-dock` (fixed, 22px inset; stretches full width at ≤720px). Each `.toast` is a notched hairline slip (10px leading cut) on near-opaque ground with panel inset; arrivals sweep in with a 320ms clip-path reveal (disabled under reduced motion). The **system voice** leads with a 7×1px teal tick and teal `.toast-label`; `.toast--attention` warms the bezel toward amber and swaps the tick for the 9×8px amber warning triangle. Copy is mono 0.78rem; slips auto-dismiss (~4.2s) with a 240ms opacity leave. `role="status"` on each slip; dock is `aria-live="polite"`.

### Tooltip (instrument plaque)
Attribute-driven: `data-tip="…"` on an element without its own decorative pseudo-elements (wrap otherwise). On `:hover` and `:focus-visible`, a hairline-bordered plaque seats 12px above the trigger with a 1px vertical connector tick — no arrow blobs. `.tip-trailing` seats the plaque to the right of the trigger on a horizontal connector (for collapsed side rails and left-edge triggers where the standard plaque would leave the viewport). Plaque text is uppercase mono 0.6rem at 0.14em tracking in Instrument Label; 160ms opacity fade.

### Advisory (standing notice strip)
A full-width hairline-bounded strip for persistent notices: top and bottom 1px rules, flex row with a leading instrument mark, uppercase `.advisory-label`, and `.advisory-copy` in body mono. Default voice: 7×1px teal tick + teal label. `.advisory--attention` warms both hairlines toward amber and swaps the tick for the amber warning triangle with an amber label. `role="status"`; not a toast — it stays until the condition clears.

### Field Inputs & Amber Validation (configuration form)
Form inputs share the dropdown's slot anatomy: 1px Hairline bezel, dark fill (ground at 0.55 alpha), mono uppercase Bright Text (0.82rem, tabular-nums, 0.1em tracking), 10px 14px padding, dim placeholder; focus warms the bezel to teal at 0.6 alpha. Widths: 108px standard, 84px `.field-input--narrow`, 100% `.field-input--wide`. Textareas fill their stack (min-height 6.25rem) and stay locked in ceremony plates (`resize: none`); `.field-textarea--resize-y` / `--resize-both` are opt-in grow. React `FormField` wires the label, optional `.field-hint` (dim uppercase 0.68rem, no triangle), and `.field-error`; `FieldInput` / `FieldTextarea` own `is-invalid` and `is-frozen`. Validation speaks amber per the world's no-red doctrine: an invalid field's bezel turns Signal Amber with a 10px Amber Glow shadow, and an error line appears in Amber Bright (0.68rem) led by a small solid amber clip-path warning triangle (9×8px — the confirm dialog's 11px mark at field scale), naming the problem and the recovery in one sentence. The helper line is not an error — it stays Dim Label and never uses the triangle. Duration fields carry a standing `MM:SS · e.g. 60:00` hint and validate as the operator types. Pair rows (`.form-row--pair` + `.field-pair`) seat two fields on one horizon with a 48px pair gap. Each `.field-pair` is an inline grid sized to its label and slot; hint and error wrap under that grid only, so a long recovery line cannot inflate the pair and drop the neighbor. They stack at ≤720px. Warnings intensify amber; nothing ever turns red. Labels are the standard 0.62rem uppercase microlabel, held in a 148px column on desktop and stacking above their fields at ≤720px.

### Date, time, and datetime pickers
Temporal values use the field select-shell, never native `input type="date|time|datetime-local"`. `DatePicker`, `TimePicker`, and `DateTimePicker` live in `prototypes/src/components/temporal/` and re-export from the public `components` barrel. The trigger is a dropdown key sized to its mark (`max-content`, independent of the plate): a 14px authored calendar or chrono glyph (Instrument Label at rest, Phosphor Teal while open), uppercase mono closed mark, and chevron. Closed marks are `YYYY-MM-DD`, 24h `HH:MM` (or `HH:MM:SS` when `withSeconds` is set), or `YYYY-MM-DD HH:MM` with a space — not an ISO `T`. The plate is the shared popover surface (`role="dialog"`), sized as a dense instrument (date 17.5rem, time 10.25rem, datetime 26.25rem — capped, not stretched to the trigger): 32px day cells, 0.78rem tabular digits, 0.68rem month plaque and weekday/HH/MM heads. Month chevrons are the drawn stroke glyph in Bright Text (teal on hover). Date is a Monday-start grid of 4–6 weeks (a trailing week that is entirely out of month is dropped). Adjacent-month days stay Instrument Label; in-month days are Bright Text. The selected day is Teal Glow fill plus a 1px inset rectangular bezel at Phosphor Teal 0.6 alpha; today is a 22px circular hairline around the numeral (teal digit, no cell fill). When that day is also selected the rectangle wins and the numeral stays teal. The plate is a four-sided instrument: trigger keeps its bottom bezel while open, and `.datetime-popover` draws a matching top hairline. Time is two wheels on the option-menu row rhythm (hairline, teal-glass hover) in a 184px drum with 30px rows so the selected HH and MM share one horizon; the selected mark is the same teal inset bezel, with the 7×1px tick hidden. Pass `withSeconds` to add an SS wheel (`.datetime-clock--seconds`) and `HH:MM:SS` closed marks; session marks stay HH/MM. Datetime seats calendar and chrono side by side (`.datetime-popover--split`, ~16.25rem + 9.25rem) and stacks them at ≤720px. Day pick on date-only commits and closes; datetime and time stay open until Done, outside dismiss, or Escape. Clear empties the value. Invalid and frozen reuse the field voices — amber bezel + Amber Glow + error line, etch with glyph and chevron withdrawn. Catalogued on the Component Deck as **Date & time** (`/shared/gallery#datetime`).

### Frozen Configuration Readout (post-activation state)
Frozen is a **control** state, not a plate skin: `.field-input.is-frozen`, `.field-textarea.is-frozen`, and `.select-shell.is-frozen` etch the committed value — `readOnly` / `disabled`, bezels and fills transparent, left padding zero, chevrons and datetime glyphs hidden — so Bright Text sits directly on the glass. Ceremony still owns the teal `.frozen-line` ("Configuration frozen at activation": 0.68rem uppercase at 0.14em, led by the 10px teal clip-path checkmark) and demotes Activate to a disabled "Activated". The Campaign Record / registry activation column lights a sealed-solid teal node. The commit itself plays the administrator's one authored moment: a 640ms teal seal-sweep (`.is-sealing`) across whichever etched frame is active — the Enrollments wall frame or the Campaigns record frame (105° gradient translated edge to edge, removed under reduced motion). Disabled (inert chrome, still a control) is not frozen (readout etch).

## Do's and Don'ts

### Do:
- **Do** frame every surface with a 1px teal hairline (rgba(110, 154, 156) at 0.52 or 0.28 alpha) and cut selected corners with 10–18px clip-path notches.
- **Do** reserve amber (#e2a33c) exclusively for attention: time, the active turn, the current stage bar, the single commit action per screen.
- **Do** set every label in uppercase with 0.14–0.34em tracking — Michroma for placards, Sometype Mono for data — and use tabular-nums on all changing digits.
- **Do** keep state feedback at 150–250ms with `cubic-bezier(0.16, 1, 0.3, 1)`, and honor `prefers-reduced-motion` completely.
- **Do** terminate hairline traces and phase connectors with 5px circular nodes (1px border, ground fill); warm the node to amber to mark the active position.
- **Do** draw every glyph as authored geometry — ~1.1px SVG strokes (check ring, lock, document, seal ring, operator glyph, 14px calendar and chrono glyphs) or clip-path solids (checkmark, warning triangle) — in the two color voices only, and only where the mark carries state or object identity.
- **Do** build a chamfered frame that needs a stroked bezel as two clipped layers (1px-padded Hairline outer cut, inner pane re-cut 1px inside), and keep sibling plates on one horizon: equal flex row tracks, dividers seated 12px under values, a uniform reserved key foot (58px min) — dropped board-wide to content height the moment any bay stacks more than one plate.
- **Do** draw wait as Shipboard instruments: a 14px (22px large) hairline wait-mark with a pulsing teal node and scan line, a 3px teal scan-track (`--scan` 0–1, or `.is-waiting` 32% sweep), dashed skel-lines for records in transit, and the wait-plate (empty-plate anatomy). Occupy keys with `.is-waiting` — teal, opacity 1, wait-mark seated; hot keys drop amber while occupied.
- **Do** fold a persistent side menu as a gangway (232px shared default, 248px Administrator override → 76px channel-code rail, trailing tooltip plaques while collapsed); swap it for a leading bulkhead at drawer widths rather than collapsing further.
- **Do** disclose the operator through the strip profile — drawn 14px hairline glyph, short tabular ID, chevron — and put Sign out inside that menu as a real action (never `href="#"`), never as a sibling token on the strip.

### Don't:
- **Don't** use border-radius, chat bubbles, avatars, pill buttons, or filled colored badges — this world refuses the rounded dark AI-chat screen.
- **Don't** cast outer drop shadows or lift panels; depth is inset glass (top edge-light, bottom vignette) and phosphor glow on emitters only.
- **Don't** introduce a third color voice: no reds, greens, purples, or status-color systems. Warnings intensify amber; success and system states speak teal.
- **Don't** use any font besides Michroma and Sometype Mono, and never set placard text in mixed case.
- **Don't** show state as a colored blob or background fill; shift an instrument mark (border warmth, emitter color, digit brightness) instead.
- **Don't** import icon fonts, icon libraries, emoji, or Unicode symbols as icons, and don't seat a pictographic icon beside every label — glyphs are rationed like amber.
- **Don't** show loading as a circular spinner, bouncing dots, or a filled shimmer card; don't keep amber on a hot key while it waits.
- **Don't** put an amber check on row selection, or a photographic avatar / pill identity chip in the command strip — selection is the teal select-mark; identity is the drawn operator glyph.
- **Don't** collapse a gangway past the 76px channel-code rail, or use a hamburger to hide persistent navigation; at drawer widths the shell swaps to a leading bulkhead.
- **Don't** use native `date`, `time`, or `datetime-local` inputs — temporal values use DatePicker, TimePicker, and DateTimePicker on the field select-shell.
