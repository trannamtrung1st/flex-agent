# Modals, bulkheads, and ceremonies

Use native `<dialog>` where possible. Scrim 82% ground for blocking; lighter
scrim when the incumbent surface must stay readable.

## Dialog plate

  10px eight-cut, smoked glass, hairline head/body/foot. Widths: 412 / 520 /
680px (`min` with `100vw - 32px`). On a live overlay (`<dialog>`), tall
form and confirm bodies scroll inside; head and foot stay seated. A filling
table in that body (Assign picker) still scrolls `.dialog-body` or
`.ceremony-body`. Nested `.datatable-scroll` clips vertical overflow and
sets `overscroll-behavior-y: auto` so it cannot trap the wheel; horizontal
overflow stays on the table. The table toolbar is sticky so search stays
seated. Component Deck in-flow
recipes (Form recipes Record accommodation) are documentation: they must not
open a nested catalog scrollport. Gallery CSS clips those bodies; shared
overlay CSS stays overlay-owned. Titles may lead with the amber warning
triangle when the action is consequential. Head, body, and foot use equal
`--plate-foot-pad-block` on both block edges (inline `--frame-inset-inline`)
so title and keys sit in the band, not against the hairline. Overlay
`.dialog-body` and overlay bulkhead bodies reserve `scrollbar-gutter: stable`
with `scrollbar-width: auto` so a filling table’s bezels stay clear of the
thumb — the same gutter as in-page `.create-ceremony__scroll` and
`.operate-scroll`. Campaign configuration fill-grid plates leave `.ceremony-plate` unpadded.
Wide campaign configuration (`.dialog-plate--wide.ceremony-plate`) sets
`--dialog-w: 840px` (`min` with `100vw - 48px`) so four timing instruments
fit on one row; other `--wide` dialogs stay at 680px. Timing and attempts uses
`.ceremony-config-grid` (`repeat(4, minmax(0, 1fr))`); at ≤720px it reflows
to two columns. Apply it only through lab `CampaignCeremonyConfigGrid`
inside a lab `CampaignCeremonyBody` (campaign configuration); do
not pass `className="ceremony-config-grid"` on generic `Grid`, and do
not add ceremony presentation props to generic `DialogPlate`. Head, body, and foot
each use `--space-6` (not the compact 16px well remap,
and not a second `--space-3` inline-end track) so content sits on one inset
while the overlay `thin` thumb sits on the body’s inline-end, against the
cut. The foot uses `--space-6` outer padding on every edge. A conditional
receipt is the first child of `.ceremony-foot-actions` and shares its
`--space-3` stack gap to the keys; without a receipt, actions begin at the
same outer top inset. The hairline above the foot is full-bleed. Seated
Component Deck in-flow recipes neutralize that gutter (`auto`) because they
are not overlay scrollports. In-flow plate content sits in `.dialog-stage`.
The open `<dialog>` is `display: block` so portaled plaques and menus are not
flex or grid items of the plate. Dialogs stay
centered; they do not flip like a tooltip. Nested select, menu, and datetime
overlays inside a live dialog still use `placeFloating` / `useOverlayDismiss`:
scroll inside the picker stays open; scroll of `.dialog-body` dismisses the
picker and leaves the dialog open. In-dialog save and readiness *success*
receipts stay inside the pinned ceremony foot (`role="status"`), before its
actions. Blocked readiness uses an in-body
[`ErrorSummary`](error-summary.md) titled **Readiness blocked**,
not a second foot note. The page toast dock cannot
paint above the dialog top layer. The standing helper
sentence stays in the body as a sibling of the form stack (not a bay-gap
child). Its `margin-block-start` is `--space-6`, matching
`.ceremony-body` `padding-block-end` and `.ceremony-foot`
`padding-block-start`. Ceremony helpers use the full body width (`max-width:
none`); do not cap them at prose `ch` widths inside fill-grid plates. The
form stack grows (`flex: 1 1 auto`) so leftover
body height when the plate hits `max-height` sits above the helper. The foot’s
outer inset remains `--space-6`; receipt copy shares the action stack’s
`--space-3` gap to the keys.

`DialogPlateFooter` uses the shared [`PlateFoot`](cards.md#plate-foot) rail
(`arrangement` default `end`; `split` for Cancel + commit). Campaign
configuration fill feet use lab `CampaignCeremonyFooter` (`.ceremony-foot`)
and do not use the plate-foot cluster. `CeremonyDialog` remains the generic
overlay shell. Campaign fill-grid overlays wrap it with lab
`CampaignCeremonyDialog` (`.ceremony` and `.ceremony-cut`).

## Bulkhead

Leading or trailing smoked-glass drawer. Escape, scrim, and Close dismiss;
focus returns to the trigger. Reduced motion disables the 320ms transform.
While open, the drawer inerts hull chrome and generic layout hosts
(`.command-strip`, `.console-foot`, `.layout-management__shell`,
`.composition-split`). Do not add one-surface hosts such as `.queue-view`.

## Rules

- Initial focus, containment, and restore follow the governing UI spec.
- Logout, leave-session, activation, Review decision, and Release confirmation
  are distinct ceremonies. Do not combine Review and Release (`PC-01`).
- Browser dialog state is not server authority (`PC-05`, `PC-08`).
