# Modals, bulkheads, and ceremonies

Use native `<dialog>` where possible. Scrim 82% ground for blocking; lighter
scrim when the incumbent surface must stay readable.

## Dialog plate

10px eight-cut, smoked glass, hairline head/body/foot. Widths: 412 / 520 /
680px. Tall bodies scroll inside; head and foot stay seated. Titles may lead
with the amber warning triangle when the action is consequential.

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
