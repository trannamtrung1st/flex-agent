# Color Tokens

## Color Philosophy

The signature Flex Agent environment is **near-black blue-green hull + phosphor
teal systems + rationed signal amber**.

Brand and semantic status colors are separate systems:

- Phosphor teal expresses **interaction, selection, focus, wait instruments,
  sealed/ready marks, current context, and genuine live Agent state**.
- Signal amber expresses **attention and commitment**: time, the active turn or
  stage, the single hot commit in a region, field validation, and destructive
  confirmation emphasis.
- Green means **success/healthy/approved** as an outcome, always with a text
  label and instrument mark. It is not a brand or navigation color.
- Red/danger means **failed, rejected when rejection is an outcome, blocking
  disconnect, Access denied, or destructive consequence**. Dark `danger` is
  **fault phosphor** (`#F05C58` / placard `#FF7468`): an ember lamp next to
  amber, not a pink consumer error. It is not a brand color and does not
  decorate validation (validation uses amber).
- Violet is not part of the Shipboard identity. Do not introduce it as a
  co-equal primary.

The dark theme is the canonical brand expression. The light theme is a
supported operational alternate and must preserve the same semantic hierarchy
and non-color state cues (`DS-DEC-1`, `PC-12`).

### Named rules

**Amber ration.** Amber appears only where attention is demanded. It never
decorates resting chrome, wait, or progress, and it does not appear twice in
one region for different reasons.

**Two control voices.** Amber marks commitment (acknowledgment, hot keys,
validation). Teal marks selection and system state (radios, breakers,
listbox ticks, row select, focus, wait). Do not swap the voices.

**Instrument marks.** State changes shift a node, hairline, digit brightness, or
glyph. Do not use filled pills or background blobs as the only status.

## Background Tokens

### Hull surfaces

Dark values match the recorded prototype tokens. Light values invert the hull
while keeping teal/amber roles.

| Token | Light | Dark |
| --- | --- | --- |
| canvas | #E8F0F2 | #07141B |
| surface-primary | #F4F8F9 | #041018 |
| surface-secondary | #DDE8EC | #0E1C24 |
| surface-tertiary | #D0DEE3 | #0A181F |
| surface-elevated | #F7FBFC | #041018 |
| surface-inset | #E3ECF0 | rgba(2, 9, 14, 0.55) |
| surface-selected | #D4EEEE | rgba(60, 192, 191, 0.14) |
| surface-hover | #DCECEE | rgba(2, 9, 14, 0.28) |
| surface-disabled | #E2E8EA | #090E12 |
| surface-inverse | #07141B | #E6EEF2 |
| overlay-scrim | rgba(4, 12, 17, 0.45) | rgba(2, 9, 14, 0.82) |

Ceremony overlays that must keep the incumbent surface readable may use the
lighter scrim in both themes. Blocking dialogs use the stronger scrim.

### Brand / Signal

| Token | Light | Dark |
| --- | --- | --- |
| brand-softer | #D7F1F1 | rgba(60, 192, 191, 0.14) |
| brand-soft | #B8E4E3 | rgba(60, 192, 191, 0.22) |
| brand-primary | #146261 | #3CC0BF |
| brand-strong | #125A59 | #5FD0CF |
| brand-signal | #146261 | #3CC0BF |
| brand-live-soft | #D7F1F1 | rgba(60, 192, 191, 0.14) |
| brand-live | #146261 | #3CC0BF |
| brand-live-strong | #0F5251 | #5FD0CF |
| attention-softer | #F8EEDC | rgba(226, 163, 60, 0.18) |
| attention | #9A6A12 | #E2A33C |
| attention-strong | #7A540E | #EDC890 |

`brand-primary` is phosphor teal, not electric blue. `brand-live` shares that
teal family; live meaning still requires a text/structure cue (`PC-12`).

### Semantic

| Token | Light | Dark |
| --- | --- | --- |
| success-soft | #EAF8F0 | #0B2518 |
| success | #1F7A49 | #53D28A |
| warning-soft | #F8EEDC | rgba(226, 163, 60, 0.18) |
| warning | #9A6A12 | #E2A33C |
| danger-soft | #FDECEF | #321015 |
| danger | #C43E4B | #F05C58 |
| danger-strong | #A9313D | #FF7468 |
| info-soft | #D7F1F1 | rgba(60, 192, 191, 0.14) |
| info | #146261 | #3CC0BF |

Warning tokens alias the amber attention family. Info tokens alias teal.

## Foreground Tokens

| Token | Light | Dark |
| --- | --- | --- |
| fg-strong | #041018 | #F8FCFE |
| fg-default | #1A2A32 | #E6EEF2 |
| fg-muted | #3D5A62 | #A8C4CA |
| fg-subtle | #4A6A72 | #88A8B0 |
| fg-disabled | #7A9096 | #5A7078 |
| fg-inverse | #F8FCFE | #041018 |
| fg-on-accent | #041018 | #041018 |
| fg-on-live | #041018 | #041018 |
| fg-brand | #125A59 | #3CC0BF |
| fg-live | #125A59 | #3CC0BF |
| fg-attention | #7A540E | #E2A33C |
| fg-success | #1A6D40 | #68DB98 |
| fg-warning | #7A540E | #EDC890 |
| fg-danger | #A9323E | #FF7468 |
| fg-info | #146261 | #5FD0CF |

## Border / Signal Tokens

| Token | Light | Dark |
| --- | --- | --- |
| border-subtle | rgba(70, 110, 116, 0.35) | rgba(110, 154, 156, 0.28) |
| border-default | rgba(70, 110, 116, 0.55) | rgba(110, 154, 156, 0.52) |
| border-hover | #146261 | #3CC0BF |
| border-strong | #4A6A72 | #A8C4CA |
| border-focus | #146261 | #3CC0BF |
| border-selected | #146261 | #3CC0BF |
| border-brand | #146261 | #3CC0BF |
| border-info | #146261 | #3CC0BF |
| border-success | #367B53 | #367B53 |
| border-warning | #9A6A12 | #E2A33C |
| border-danger | #C43E4B | #8B3844 |
| border-live | #146261 | #3CC0BF |
| border-signal-dim | rgba(70, 110, 116, 0.35) | rgba(110, 154, 156, 0.28) |

Hairline structure is always 1px in `border-default` or `border-subtle`.

Dark primitives and light remaps are the CSS custom properties in
`web/src/styles/tokens.css` and `web/src/styles/adaptations.css`. Primitive
names (`--ground`, `--teal`, `--amber`, `--danger`) remain the dark-theme
source values. Semantic aliases (`--canvas`, `--brand-primary`, `--fg-default`,
`--fg-danger`, and the rest) live in `semantic-aliases.css`. Do not reintroduce
a `--fa-*` prefix unless the token implementation is renamed as a whole. Light
`--teal-glow` follows `emission-focus` (`rgba(26, 122, 121, 0.2)`), not light
`brand-softer`. Denied ceremony titles use `--fg-danger` plus `--danger-glow`;
do not leave the resting teal placard halo on a danger title.

## Emission Tokens

Emission is phosphor glow on emitters, not a general shadow style.

| Token | Light | Dark | Intended use |
| --- | --- | --- | --- |
| emission-focus | rgba(26,122,121,.20) | rgba(60,192,191,.34) | keyboard focus |
| emission-selected | rgba(26,122,121,.12) | rgba(60,192,191,.14) | selected row/context |
| emission-live | rgba(26,122,121,.16) | rgba(60,192,191,.30) | genuine live Agent |
| emission-agent | rgba(26,122,121,.12) | rgba(60,192,191,.20) | Agent Core |
| emission-attention | rgba(154,106,18,.16) | rgba(226,163,60,.18) | timer, hot key, validation |
| emission-danger | rgba(169,50,62,.22) | rgba(240,92,88,.32) | denied / failed backlit placards |

Light mode relies primarily on borders and surface state; keep glow lower.

## Semantic Usage Rules

- Application canvas: `canvas`.
- Command strip, gangway, console foot, management drawer bar, and instrument
  bulkhead rails: no extra fill — the hull (`canvas` / `--ground` gradient on
  `body`) shows through. Sticky Component Deck chrome uses `surface-primary`
  (`--ground-deep`) only, matching `html` and the top of the hull. Overlay
  bulkhead *drawers* stay opaque so they occlude the page.
- Primary work surface / smoked plate fill: `surface-primary` over canvas with
  documented sheen/depth/inset stacks from [shadows](shadows.md).
- Selected context: `surface-selected` + `border-selected` + a tick, rail, or
  node. Never color alone.
- Main text: `fg-default`; titles and current values: `fg-strong`; instrument
  labels: `fg-muted`.
- Primary interaction and focus: `brand-primary` / `border-focus`. Light-theme
  teal uses `#146261` so normal-size brand text meets at least 4.5:1 on
  `canvas` (`PC-12`). Dark-theme `#3CC0BF` on hull ground exceeds 8:1.
- Live Agent/streaming: `brand-live` plus visible state text.
- Success/danger remain outcome colors and must not become decorative brand
  accents or replace labels.

## Prohibited

- No raw color values in component code; use tokens.
- No generic purple-pink gradient identity.
- No electric-blue v0.1 identity as the target look.
- No filled status pills as the only state cue.
- No amber on wait, progress, or resting navigation.
- No low-contrast teal-on-black for long-form content; verify 4.5:1.
- No uncontrolled multi-color glows.
- Gradients are allowed only as **subtle directional illumination** in plate
  sheen/depth and Agent Core; keep them low-opacity.
