# Typography

## Font roles

- **UI / Body:** `--font-sans`; approved preferred face Geist Sans, followed by
  the approved system sans-serif fallback stack.
- **Display / Brand:** `--font-display`; approved preferred face Space Grotesk,
  used sparingly for product/Agent identity, marketing, onboarding, and
  top-level display moments.
- **Technical / Mono:** `--font-mono`; approved preferred face IBM Plex Mono,
  followed by the approved system monospace fallback stack.

The resolved `Q-DS-1` and approved `DS-PROP-1` in the
[design-system root](../README.md#q-ds-1-font-delivery-and-dependency-approval)
require self-hosted, version-pinned files after license and delivery-artifact
review. Use the approved system fallbacks until those implementation checks
pass; do not make third-party font requests from authenticated or Participant
surfaces.

Do not use a futuristic display face for normal application headings.

## Core Rules

- Normal UI hierarchy uses the sans font.
- Long-form content and transcripts prioritize readability.
- Monospace is reserved for machine-readable or machine-originated information.
- Avoid uppercase for normal navigation and headings.
- Use tabular numerals for timers, timestamps, scores, token counts, and aligned numeric columns.
- Never use mono for a full conversation transcript.

## Heading Scale

| Role | Desktop | Mobile | Weight | Line height |
|---|---:|---:|---:|---:|
| Display | 40px | 32px | 600 | 1.1 |
| H1 | 30px | 26px | 600 | 1.2 |
| H2 | 24px | 22px | 600 | 1.25 |
| H3 | 20px | 18px | 600 | 1.3 |
| H4 | 16px | 16px | 600 | 1.35 |
| H5 | 14px | 14px | 600 | 1.4 |
| H6 | 13px | 13px | 600 | 1.4 |
| Section label | 13px | 13px | 600 | 1.4 |

Use semantic `h1`–`h6` in document order regardless of visual token chosen.

## Body Scale

| Role | Size | Line height | Usage |
|---|---:|---:|---|
| Reading large | 18px | 1.65 | participant answers, long agent responses, review narratives |
| Body | 15px | 1.6 | standard application content |
| Compact body | 14px | 1.5 | workspace panels, tables, settings |
| Small | 13px | 1.45 | helper text, secondary metadata |
| Micro | 12px | 1.4 | timestamps, compact labels, dense technical metadata |

## Technical Typography

Use `--font-mono` for:

- session IDs
- agent IDs
- campaign/workflow IDs
- harness versions and snapshot identifiers
- timestamps when shown as audit data
- tool names and execution metadata
- token counts
- JSON, code, expressions, schemas, logs
- model/configuration identifiers

Default technical size: 12–13px with 1.45 line height.

## Links

- Inline links: same font and size as surrounding text, `fg-brand`, underline on hover or persistent underline in prose.
- Navigation links: no underline by default; selected/active state must be structural, not color-only.
- External links may include a small external-link icon.
