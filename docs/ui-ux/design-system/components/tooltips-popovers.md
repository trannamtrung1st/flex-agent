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
  in both themes; identifier glyphs stay light on that overlay. CompactId
  places the plaque against `.compact-id` even when the table host fills the
  identifier cell for hover and press.
- Show on hover and focus-visible; 160ms opacity. Compact registry identifiers
  also open on primary press so a clipped table cell remains reachable without
  a tab stop (see [tables](tables.md)).
- `TooltipHost` plaques stay open across a short linger (240ms) so the
  pointer can enter the plaque. Plaque text is selectable and copyable.
  Do not hide while a text-selection drag that started on the plaque is
  held.   Opening one plaque dismisses any other. CSS `data-tip` plaques
  remain inspect-only; `::after` content cannot be selected.
  When the host is inside a modal `<dialog>`, `overlayPortalRoot` portals the
  plaque into that dialog as a sibling of `.dialog-stage` (not into the plate,
  and not `#root`) so it paints in the same top layer instead of behind the
  scrim. Open dialogs are `display: block` so those siblings are not flex or
  grid items of the plate (that first measure painted a dark slab over foot
  keys). Plaques set `height: max-content` and `box-shadow: none` (hairline
  fill only). Outside a dialog,
  overlays portal into `#root` so the command strip / Deck header can stack
  above menus and select popovers. Identifier plaques keep a higher z-index
  so a copied value is not trapped under chrome. Command menus,
  listboxes, and select/datetime popovers use the same root.
  Portaled plaques, menus, and select popovers use shared `placeFloating`:
  flip to the opposite side only when that side fits the full panel; otherwise
  pin flush to the viewport edge (no inset). Covering the trigger is allowed.
  Select, command-menu, and datetime plates seat with `OVERLAY_PLATE_OFFSET`
  (`-1`) on the open axis; plaques keep this gap (host `offset: 10`; CSS
  `data-tip` 12px) and do not use that overlap. Place once on open; resize re-places. External page, window,
  visual-viewport, or ancestor scroll hides the overlay immediately. Pointer
  movement between host and plaque still uses the linger; do not hide for
  hover travel or while a plaque selection drag is held. Menus and selects
  cap height to the viewport and scroll inside only when the panel is taller
  than the viewport; that inner scroll does not dismiss. Do not lock ordinary
  page scroll. Plaques hug their
  copy (single line) and center on the trigger; do not lock plaque min-width to
  the control. The connector tick tracks the trigger. CSS `[data-tip]` plaques
  do not auto-flip. Native dialogs stay centered and size-constrained; they
  do not flip like a tooltip. New overlays clone `AnchoredOverlay`; do not
  invent a local `position: absolute` popover.
- Tooltips never hold the only name of a control, the only error, or
  destructive consequence.
- Popovers that contain actions are menus or dialogs, not tooltips. Do
  not put copy keys or other controls in a plaque.
