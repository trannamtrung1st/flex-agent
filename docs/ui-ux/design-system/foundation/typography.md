# Typography

## Font roles

- **Placard / identity:** `--font-display`; approved preferred face **Michroma**,
  followed by `"Arial Narrow", sans-serif`.
- **UI / body / data / technical:** `--font-mono`; approved preferred face
  **Sometype Mono**, weights 400/500/600, followed by `ui-monospace, monospace`.
- There is no third application face. Do not use Geist Sans, Space Grotesk, or
  IBM Plex Mono as v1.0 identity fonts.

`Q-DS-1` / `DS-PROP-2` in the [design-system root](../README.md#q-ds-1-font-delivery-and-dependency-approval)
require self-hosted, version-pinned files after license and delivery-artifact
review. Use the approved fallbacks until those implementation checks pass; do
not make third-party font requests from authenticated or Participant surfaces.

## Core Rules

- **Two voices.** Michroma names (brand, plate titles, key captions, bay heads).
  Sometype Mono speaks (transcript, readouts, inputs, navigation tokens, errors).
- Placard and microlabel strings are uppercase with wide tracking. Sentence-case
  body copy uses Sometype Mono and is not forced to uppercase.
- Do not use a third family for headings.
- Long-form content and transcripts prioritize readability: body 15px / 1.5–1.55,
  measure about 68–78ch, `fg-default` / `fg-strong`.
- Use tabular numerals for timers, timestamps, scores, token counts, calendar
  days, and aligned numeric columns (`font-variant-numeric: tabular-nums`).
- Interactive chrome must not drop below 0.75rem (12px) for visible control
  labels. Supporting field errors/hints must not drop below 0.68rem.
  Decorative microlabels may be smaller only when they are not the sole name of
  a control and still meet contrast (`PC-12`).
- Semantic `h1`–`h6` follow document order regardless of visual token.

## Heading Scale

| Role | Desktop | Narrow | Weight | Line height | Face |
| --- | ---: | ---: | ---: | ---: | --- |
| Display (timer digits only) | 2.9rem | 1.75–2.2rem | 600 | 1 | Mono |
| H1 / wall head | 0.78–1.15rem | 0.72–0.9rem | 400 | 1.2 | Placard |
| H2 / plate title | 0.72rem | 0.68rem | 400 | 1.25 | Placard |
| H3 | 0.68rem | 0.68rem | 400 | 1.3 | Placard or mono |
| Section label | 0.62–0.68rem | 0.62rem | 400–500 | 1.4 | Mono microlabel |

## Body Scale

| Role | Size | Line height | Usage |
| --- | ---: | ---: | --- |
| Reading large | 16–18px | 1.55–1.65 | long Agent/Participant narratives, review rationale |
| Body | 15px | 1.5–1.55 | standard application content and transcript |
| Compact body | 0.88–0.95rem | 1.5 | plates, tables, settings |
| Small | 0.75rem | 1.45 | interactive chrome, helper text |
| Micro | 0.62–0.68rem | 1.4 | readout keys, timestamps; not sole control names |

## Technical Typography

Use `--font-mono` for session, Agent, Campaign, harness, timestamp, tool,
token-count, JSON, and identifier data. Default technical size: 0.72–0.82rem
with tabular numerals. Unlike v0.1, mono is also the body face; do not
introduce a sans body to “fix” that. Improve readability with size, measure,
contrast, and line height instead.

## Links

- Inline links: same font and size as surrounding text, `fg-brand`, underline
  on hover or persistent underline in prose.
- Navigation links: no underline by default; current state uses a teal tick or
  underline bar plus `aria-current`. Never amber.
- External links may include a small Lucide external-link icon.
