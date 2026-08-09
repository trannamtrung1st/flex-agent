# Color Tokens

## Color Philosophy

The signature Flex Agent environment is **deep-space black + electric blue + live cyan**.

Brand and semantic status colors are separate systems:

- Electric blue expresses **interaction, selection, focus, agent computation, routing, and current context**.
- Cyan expresses **genuinely live states** such as listening, speaking, streaming audio, or an active signal.
- Green means **success/healthy/approved**.
- Amber means **warning/pending/review needed**.
- Red means **danger/error/destructive/failed**.
- Violet is a rare secondary spectral accent for memory/knowledge visualization or identity artwork; it is not a co-equal primary brand color.

The dark theme is the canonical brand expression. The light theme is a supported operational alternate and must preserve the same semantic hierarchy.

## Background Tokens

### Deep-Space Surfaces

| Token | Light | Dark |
| --- | --- | --- |
| canvas | #F4F8FC | #04070D |
| surface-primary | #FFFFFF | #080D16 |
| surface-secondary | #EEF4FA | #0C1320 |
| surface-tertiary | #E5EEF7 | #111B2A |
| surface-elevated | #FFFFFF | #0E1827 |
| surface-inset | #F0F5FA | #060A11 |
| surface-selected | #E7F3FF | #092744 |
| surface-hover | #EDF6FF | #0C1C2E |
| surface-disabled | #EDF1F5 | #090E16 |
| surface-inverse | #07111E | #F1F8FF |
| overlay-scrim | rgba(3, 10, 20, 0.42) | rgba(0, 2, 6, 0.72) |

### Brand / Signal

| Token | Light | Dark |
| --- | --- | --- |
| brand-softer | #EAF4FF | #06182B |
| brand-soft | #D5E9FF | #08233D |
| brand-primary | #086CEB | #1684FF |
| brand-strong | #0058C7 | #43A2FF |
| brand-signal | #007EEA | #2E9CFF |
| brand-violet | #6751D8 | #8E7CFF |
| brand-live-soft | #E4F9FF | #06212C |
| brand-live | #007F9E | #22C7F2 |
| brand-live-strong | #006983 | #59D9FA |

### Semantic

| Token | Light | Dark |
| --- | --- | --- |
| success-soft | #EAF8F0 | #0B2518 |
| success | #1F7A49 | #53D28A |
| warning-soft | #FFF4DF | #30220B |
| warning | #9B6210 | #F0B74F |
| danger-soft | #FDECEF | #321015 |
| danger | #C43E4B | #FF6675 |
| danger-strong | #A9313D | #FF8290 |
| info-soft | #EAF4FF | #071E37 |
| info | #2F6DAE | #65AEF4 |

## Foreground Tokens

| Token | Light | Dark |
| --- | --- | --- |
| fg-strong | #07111E | #F2F8FF |
| fg-default | #28384A | #C9D8E8 |
| fg-muted | #52677B | #8398AD |
| fg-subtle | #5A6C7E | #70869C |
| fg-disabled | #A0AAB5 | #48586A |
| fg-inverse | #FFFFFF | #07111E |
| fg-on-accent | #FFFFFF | #03131A |
| fg-on-live | #FFFFFF | #03131A |
| fg-brand | #0664D8 | #65B6FF |
| fg-live | #00728E | #62DDFB |
| fg-success | #1A6D40 | #68DB98 |
| fg-warning | #82520D | #F5C56D |
| fg-danger | #A9323E | #FF8290 |
| fg-info | #2B6097 | #81BFF8 |

## Border / Signal Tokens

| Token | Light | Dark |
| --- | --- | --- |
| border-subtle | #DCE7F1 | #152235 |
| border-default | #C8D8E7 | #20334C |
| border-hover | #6F93B6 | #315A82 |
| border-strong | #7794AE | #2E4E72 |
| border-focus | #087DF4 | #3BA7FF |
| border-selected | #0B74E5 | #208CFF |
| border-brand | #72AEE8 | #1D6EBA |
| border-info | #7EABD4 | #2C648F |
| border-success | #70B38B | #367B53 |
| border-warning | #C99A4A | #79561D |
| border-danger | #D97882 | #8B3844 |
| border-live | #54B6C8 | #1AA6C8 |
| border-signal-dim | #A9CAE8 | #123B60 |

## Emission Tokens

Emission is a controlled science-fiction effect, not a general shadow style.

| Token | Light | Dark | Intended use |
| --- | --- | --- | --- |
| emission-focus | rgba(8,125,244,.20) | rgba(59,167,255,.34) | keyboard focus / scanner edge |
| emission-selected | rgba(11,116,229,.12) | rgba(32,140,255,.22) | selected panel/row/context |
| emission-live | rgba(0,127,158,.16) | rgba(34,199,242,.30) | listening/speaking/live signal |
| emission-agent | rgba(8,108,235,.12) | rgba(22,132,255,.20) | Agent Core / identity field |

## Semantic Usage Rules

- Application canvas: `canvas`; the canonical dark value should read as near-black, not gray.
- Primary hull/work surface: `surface-primary`.
- Secondary/inset instrument region: `surface-secondary` or `surface-inset`.
- Selected context: `surface-selected` + `border-selected` + a non-color cue such as a rail, marker, check, or current-position indicator.
- Main text: `fg-default`; headings and primary values: `fg-strong`; telemetry/supporting metadata: `fg-muted`.
- Primary interaction and focus: `brand-primary` / `border-focus`.
- Live voice/streaming: `brand-live`; text on saturated cyan fills uses `fg-on-live`.
- Green/amber/red are reserved for semantic outcomes and must not become decorative brand accents.
- Blue emission may reinforce state, but must never replace a visible status label where state affects decisions.

## Prohibited

- No raw color values in component code; use tokens.
- No generic purple-pink gradient identity.
- No large bright-blue page fills in the application shell.
- No neon outlines around every component.
- No green primary navigation or CTA color.
- No low-contrast blue-on-black text for long-form content.
- No uncontrolled multi-color glows.
- Gradients are allowed only as **subtle directional illumination** in Agent Core, onboarding, or hero identity surfaces; keep them low-opacity and predominantly blue/cyan.
