# Content and readout grid

Prose measure 68–78ch. Section headings may use teal uppercase mono with a
7×1px tick. Completion lines may prefix a teal check glyph.

## Readout grid

Aligned record data uses 2, 3, 4, or 6 equal tracks and semantic spans.
Each row remains a labeled `<dl>` so visual and AT order agree. A container
query collapses spans to divided rows below 46rem.

The grid is a **rule band**, not a plate. Do not wrap it in `EtchedFrame` on a
stacked nested record (Enrollment detail, Deck management-record). It sits
inside an etched well only when fused into that well: setup/create ceremony
with a docked `PlateFoot`, or a lab Campaign record whose readout shares the
clip with the foot. Open outer edges are intentional; closing them with
chamfer and ticks is a grouping box.

## Readout list / band

Teal microlabel over Bright Text values, hairline dividers. Times lead with
the named Campaign timezone (`PC-11`).

## Untrusted content

Sanitize and isolate untrusted rich content. Never present internal rubric or
authorization data to unauthorized clients.
