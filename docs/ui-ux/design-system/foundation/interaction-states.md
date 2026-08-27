# Global Interactive States

These are default behaviors for every interactive component. Component modules
may refine them but must not omit the requirements in
[accessibility](accessibility.md).

## Hover

Prefer `fg-brand` on quiet keys, a slightly brighter hairline, or
`surface-hover` on rows. Never make essential information hover-only. Do not
trigger glow simply because the pointer passes over resting chrome.

## Focus Visible

- Use a 1px `border-focus` (phosphor teal) outline with 3px offset on keys and
  equivalent controls.
- Composite fields may warm the whole slot bezel to teal at about 0.6 alpha
  instead of outlining the inner input; the visible focus must remain.
- A faint `emission-focus` halo may extend 8–12px in dark mode.
- A short one-time directional highlight is allowed when focus enters a large
  composite; reduced-motion users receive a static equivalent.
- Never remove focus indication without an equivalent visible replacement.
- Forced-colors mode must preserve a visible focus overlay.

## Active / Pressed

Use Teal Glow or Amber Glow fill according to the control voice, stronger edge
contrast, or a 1px inset. Avoid bounce, large transforms, or glow bursts.

## Selected / Current

- Use `surface-selected` plus a **teal tick**, node, or underline bar.
- Selected state must not depend on color alone.
- Use `aria-selected`, `aria-pressed`, `aria-current`, or native checked
  semantics as appropriate.
- Navigation is never amber.

## Disabled

Use reduced opacity (about 0.4) and/or `fg-disabled`. Disabled controls do not
emit, pulse, or respond to hover/active. Prefer native `disabled` where
possible. Explanations of why an action is unavailable remain normally
readable (`PC-09` unapproved actions stay absent or disabled with a reason).

## Occupied / waiting

`.is-waiting` on a control that is busy: `aria-busy`, pointer-events none,
opacity 1 (occupied, not the disabled fade). Quiet keys stay teal. Hot keys
drop amber for teal occupation and seat a wait-mark. Reduced motion holds the
mark still.

## Readonly / frozen

Readonly is not disabled. Frozen configuration etches the committed value on
the glass (`readOnly` / `disabled` presentation with bezels withdrawn) while
keeping `fg-strong` readable. Browser `frozen` styling is never activation
authority (`PC-05`).

## Loading

Preserve dimensions and label context, prevent duplicate activation, and show
an accessible wait instrument. Unexplained pulsing dots and spinners are not
allowed.
