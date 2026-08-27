# Border Radius & Panel Geometry

The system is fabricated and notched rather than soft or bubbly.

| Token | Value | Usage |
| --- | ---: | --- |
| none | 0 | every authored rectangle, key, field, plate, dialog, and table |
| notch | 10px | standard clip-path cut (`--notch`) |
| notch-lg | 12–18px | examiner plates, ceremony frames, angled hot-key leading edges |
| full | 9999px | node terminals (5–7px), Agent Core, radio marks, scrollbar thumbs |

## Notched Geometry

Notched corners are the default Shipboard signature, not an optional flourish.

- Standard cut: 10px on selected corners (often top-leading and/or
  bottom-trailing).
- Eight-cut chamfer: about 18px outer / 17px inner on board frames.
- **Clipped-border:** when a chamfered frame must show a bezel on the diagonal,
  use two clipped layers (1px-padded hairline outer cut, inner pane re-cut 1px
  inside). A plain `border` plus `clip-path` leaves chamfers unstroked.
- Implementation may use clip-path, masks, or equivalent as long as focus
  outlines and content are not clipped at 400% zoom (`PC-12`).

## Rules

- Do not apply CSS `border-radius` on authored rectangles.
- Do not make badges, keys, or chips into pills.
- Nested containers stay square/notched; they do not regain v0.1 4–8px radii.
- The document icon is a square tile (see [icon shapes](../components/icon-shapes.md#document-icon-favicon)).
- Circular geometry is only for the exceptions in the table above.
- Technical/log/timeline rows use hairline dividers, not rounded chips.
