# Inputs

Fields are bezeled glass slots: 1px hairline, `surface-inset` fill, Sometype
Mono, Bright Text, zero radius. React Hook Form and Zod remain the client
form owners (ADR-019). Server validation still wins.

## Core Specs

- Padding: 10px 14px
- Type: 0.78–0.82rem, tabular numerals where values are numeric or temporal
- Placeholder: `fg-subtle`, `opacity: 1`. Placeholders are format examples
  (for example `60:00`), never a substitute for the persistent label. Empty
  editable text and textarea slots must supply one when an example exists.
- Focus: whole slot bezel warms to teal at about 0.6 alpha; caret may be amber
- Invalid: amber bezel, Amber Glow, error line in Amber Bright led by a
  warning-triangle mark. Helper text stays `fg-subtle` and never uses the
  triangle.
- Frozen: etch the value on the glass; withdraw chevrons and slot padding;
  not a plate skin (`PC-05`). The value sits `--field-label-gap` (`--space-2-5`)
  under the persistent label. Tighter than `--form-group-gap`; looser than a
  readout `dt`/`dd` pair.

## Form section

Titled field clusters use `FormSection` (`fieldset.form-section` plus `legend`).
The legend is an H2 / plate title (0.72rem placard, `--text-bright`), not a
field microlabel. Grouping chrome lives on the **legend only**: a 2px
`--hairline` rule under the title words (not the bay width), then `--form-group-gap` to the fields.
Do not pad, rail, or top-border the fieldset — that interval lands between
the legend and the fields. Sibling sections in a `Stack` use bay gap
(`gap="6"`). Do not wrap clusters in plates, and do not insert `.form-divider`
between `FormSection`s. Side-by-side clusters stay on `Grid` gap only. Do not
give the legend a 4px local margin or `--label` microlabel type.

Cloneable page and dialog compositions live on Component Deck `form-recipes`
(OperateArea, ErrorSummary, stacked fields, FormSection + Grid, pair rows,
ReadoutGrid identity with frozen FormFields beside live inputs, DialogPlate,
PlateFoot). The seated DialogPlate recipe hugs the catalog document; it is
not a mini overlay scroller. `form` remains the parts catalog. The Dialog
section still opens live `<dialog>` specimens.

## Number fields

Numeric values use `FieldNumber`, not a bare `type="number"` slot. Native
inner/outer spin buttons stay hidden; the slot draws stacked chevron keys on
the trailing edge so increment/decrement match the bezel, not OS chrome.

- Keyboard: type digits; Arrow Up / Arrow Down on the input still step
- Pointer: Increase / Decrease keys; they are not in the tab order
- Frozen: etch the value and withdraw the stepper (`PC-05`)
- Invalid: amber on the whole slot, same as text
- Width tokens apply to the shell (narrow, standard, wide/full)

Do not restyle a text field with `inputMode="numeric"` to stand in for a
steppable number.

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
and wheel values use teal inset bezels, not amber. `DateTimePicker` popovers
use the same `placeFloating` path as selects. Gallery: `datetime`.

## File intake

`FieldFile` is the shared Shipboard control for seating local files before a
feature-specific submit or intake action.

- Modes: `single` (replace the seated file) and `multiple` (append unique files)
- Always expose a keyboard-operable **Choose file** / **Choose files** key that
  opens a native `input[type=file]`. Drag-and-drop may supplement it and is
  never the only method.
- Empty bay: 1px dashed `border-default` on `surface-inset` fill (absence)
- Drag-active or seated: solid bezel; drag-active warms to teal
- Invalid: amber bezel and Amber Glow, same as text slots
- Selected rows: document glyph, full filename (truncate with `title`),
  type/size microlabel, **Remove**
- Do not imply that local selection equals an accepted Submission version

Width follows the structured content measure. Pair with `FormField`
`layout="stack"` and `labelAssociatesControl={false}` so the group is named by
the field label.

## Rules

- Associate label, hint, and error with the control. Keep the label visible;
  put the example in `placeholder`. Field hints are sentence-case helpers
  (`fg-subtle` / `--label-dim`, 0.68rem): instruction tied to one control, not
  microlabels and not cluster provenance. Shared frozen-cluster provenance
  uses the workspace **Note** (`Alert` `variant="info"`; see
  [alerts](alerts.md)). Stacked labels (`FormField` `layout="stack"`,
  `.field-stack`) use `--field-label-gap` (control rung); do not add local
  margin between the microlabel and the slot. Titled clusters use `FormSection`
  (`--form-group-gap`), not a second field-label margin. The group mark is a
  2px `--hairline` underline under the legend words. Do not pad the fieldset or insert `.form-divider`.
- Pair rows stack at ≤720px without letting a long error drop the neighbor.
- Width tokens: narrow, standard, wide/full.
- Use `FieldNumber` when the value is a steppable number; keep text (including
  mm:ss and other formatted marks) on `FieldInput`.
