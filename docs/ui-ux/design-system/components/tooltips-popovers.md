# Tooltips and popovers

Attribute-driven plaques (`data-tip`) seat 12px from the trigger on a 1px
connector. Trailing placement for collapsed gangway codes.

Implementation: wrap interactive controls in `TooltipHost` so plaques receive
hover and focus-visible even when the inner control is disabled. Disabled
reasons also use persistent `aria-describedby` text (`disabledReason` on
`Key`). Gallery: `tooltip`.

- Text: uppercase mono about 0.6rem, `fg-muted`.
- Show on hover and focus-visible; 160ms opacity.
- Tooltips never hold the only name of a control, the only error, or
  destructive consequence.
- Popovers that contain actions are menus or dialogs, not tooltips.
