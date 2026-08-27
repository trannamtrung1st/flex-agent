# Inputs

Fields are bezeled glass slots: 1px hairline, `surface-inset` fill, Sometype
Mono, Bright Text, zero radius. React Hook Form and Zod remain the client
form owners (ADR-019). Server validation still wins.

## Core Specs

- Padding: 10px 14px
- Type: 0.78–0.82rem, tabular numerals where values are numeric or temporal
- Placeholder: `fg-subtle`
- Focus: whole slot bezel warms to teal at about 0.6 alpha; caret may be amber
- Invalid: amber bezel, Amber Glow, error line in Amber Bright led by a
  warning-triangle mark. Helper text stays `fg-subtle` and never uses the
  triangle.
- Frozen: etch the value on the glass; withdraw chevrons; not a plate skin
  (`PC-05`)

## Composer

Session composer is a notched slot with the commit key sharing the trailing
edge. Focus the slot, not a naked textarea outline. Production time and send
status are runtime-owned (`PC-08`).

## Temporal values

Do not use native `date` / `time` / `datetime-local` as the only control if a
custom picker is shipped. Custom pickers must satisfy keyboard, screen-reader,
validation, and **named Campaign timezone with UTC fallback** (`PC-11`,
`UI-SUBM-DEC-6`, `UI-SUBM-DEC-12`). Browser-local time is supplementary.

Closed marks use `YYYY-MM-DD` and 24h `HH:MM` (optional seconds). Selected day
and wheel values use teal inset bezels, not amber.

## Rules

- Associate label, hint, and error with the control.
- Pair rows stack at ≤720px without letting a long error drop the neighbor.
- Width tokens: narrow, standard, wide/full.
