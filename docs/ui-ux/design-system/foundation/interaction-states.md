# Global Interactive States

These are default behaviors for every interactive component. Component modules
may refine them but must not omit the requirements in
[accessibility](accessibility.md).

## Hover

Prefer `surface-hover`, a slightly brighter edge, or foreground emphasis. Never make essential information hover-only. Do not trigger glow simply because the pointer passes over an element.

## Focus Visible — Scanner State

- Use a 2px `border-focus` ring with 2px offset.
- In dark mode, a faint `emission-focus` halo may extend 8–12px beyond the ring.
- A short one-time directional highlight/sweep is allowed when focus enters a large composite control; reduced-motion users receive a static equivalent.
- Never remove focus indication without an equivalent visible replacement.

## Active / Pressed

Use `surface-tertiary`, stronger edge contrast, or subtle 1px inset/press feedback. Avoid bounce, large transforms, or glow bursts.

## Selected / Current

- Use `surface-selected` plus `border-selected` or a **signal rail**.
- A selected workspace region may use very faint `emission-selected`.
- Selected state must not depend on color alone; use marker position, icon/check, label, rail, or shape.
- Use `aria-selected`, `aria-pressed`, `aria-current`, or native checked semantics as appropriate.

## Disabled

Use `surface-disabled` and/or `fg-disabled`. Disabled controls do not emit, pulse, or respond to hover/active states. Prefer native `disabled` where possible.

## Readonly

Readonly is not disabled. Keep text readable with normal foreground contrast and a neutral/inset panel treatment. Values remain selectable/copyable when useful.

## Loading

Preserve dimensions and label context, prevent duplicate activation, and show an accessible progress cue. A bounded blue signal pulse is acceptable; unexplained pulsing dots are not.
