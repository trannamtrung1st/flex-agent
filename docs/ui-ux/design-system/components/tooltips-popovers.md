# Tooltips and popovers

Attribute-driven plaques (`data-tip`) seat 12px from the trigger on a 1px
connector. Trailing placement for collapsed gangway codes.

Implementation: wrap interactive controls in `TooltipHost` so plaques receive
hover and focus-visible even when the inner control is disabled. Disabled
reasons also use persistent `aria-describedby` text (`disabledReason` on
`Key`). `tipOnlyWhenTruncated` plus `truncationRef` keeps a stable host and
opens the plaque only while the measured label is clipped (`EllipsisKey` /
`Key truncate`). Gallery: `tooltip`. Center-truncated identifiers use `CompactId`, which
wraps `TooltipHost` with `tone="value"`. Pass `tabbable` when focus-visible
should open the plaque. Gallery: `compact-id`.

- Label plaques: uppercase mono about 0.6rem, `fg-muted`.
- Value plaques (`tone="value"`): sentence case preserved, tighter tracking,
  so machine identifiers stay copy-readable. Plaque chrome is a dark overlay
  in both themes; identifier glyphs stay light on that overlay.
- Show on hover and focus-visible; 160ms opacity.
- `TooltipHost` plaques stay open across a short linger (240ms) so the
  pointer can enter the plaque. Plaque text is selectable and copyable.
  Do not hide while a text-selection drag that started on the plaque is
  held. Opening one plaque dismisses any other. CSS `data-tip` plaques
  remain inspect-only; `::after` content cannot be selected.
- Tooltips never hold the only name of a control, the only error, or
  destructive consequence.
- Popovers that contain actions are menus or dialogs, not tooltips. Do
  not put copy keys or other controls in a plaque.
