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
so title and keys sit in the band, not against the hairline. Dialogs stay
centered; they do not flip like a tooltip.

`DialogPlateFooter` uses the shared [`PlateFoot`](cards.md#plate-foot) rail
(`arrangement` default `end`; `split` for Cancel + commit). Ceremony fill
feet keep `className="ceremony-foot"` and do not use the plate-foot cluster.

## Bulkhead

Leading or trailing smoked-glass drawer. Escape, scrim, and Close dismiss;
focus returns to the trigger. Reduced motion disables the 320ms transform.

## Rules

- Initial focus, containment, and restore follow the governing UI spec.
- Logout, leave-session, activation, Review decision, and Release confirmation
  are distinct ceremonies. Do not combine Review and Release (`PC-01`).
- Browser dialog state is not server authority (`PC-05`, `PC-08`).
