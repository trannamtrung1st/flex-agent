# Layout primitives (inner composition)

Reusable flow, grid, width, and padding inside a named application-shell slot.
These primitives do not assemble command strips, gangways, bulkheads, or page
landmarks. Outer structure remains the closed set in
[layouts](layouts.md).

Implementation: `web/src/design-system/components/layout/`.
CSS: `web/src/styles/components/layout-primitives.css`.
Gallery specimens: Component Deck sections `composition-*` (visual showcase only; API lives in this module).

## When to use which layer

| Need | Use |
| --- | --- |
| Page chrome, landmarks, skip link, gangway/rail | One closed shell family |
| Vertical rhythm between siblings | `Stack` |
| Horizontal groups that may wrap | `Inline` |
| Equal-width tiles that reflow by available space | `Grid` |
| Readable or form column width | `Container` |
| Tokenized logical padding | `Inset` |
| Named start/main/end tracks that stay columns | `SplitBay` |
| Status Bays or live-session hull columns | Domain / shell CSS, not `Grid` |

Feature pages import primitives from the production design-system barrel. They
do not import `design-lab` or assemble shells from chrome primitives.

## Public API

The flow primitives (`Stack`, `Inline`, `Grid`, `Container`, `Inset`) are
polymorphic: `as` (default `div`), forwarded ref, standard attributes for the
chosen element, `className`, and `children`. They add no landmark role, do not
reorder children, and do not inject `data-layout` or `.layout-*` classes.
`SplitBay` is a named-slot primitive on a `div` root (`start`, `main`,
`end`, optional `head` / `foot` / `toolbar` / `overlay`) and is also not a
layout family. Optional `head` / `foot` span the main and end tracks when a
composition needs a plaque or bar over the ledger only. The reviewer split
ledger does not use those slots: `OperateHead` `arrangement="plaque"` is the
full-width `record-head`, `SplitBay` is the three work columns, and the
decision bar is a sibling of the bay.

Shared tokens:

- `LayoutSpace`: `none` \| `1` \| `2` \| `2.5` \| `3` \| `4` \| `5` \| `5.5` \| `6` \| `6.5` \| `8` \| `10` \| `12` \| `16` \| `20` \| `24` (the [spacing ladder](../foundation/layout.md))
- `LayoutAlign`: `start` \| `center` \| `end` \| `stretch` \| `baseline`
- `LayoutJustify`: `start` \| `center` \| `end` \| `between`

| Component | Props | Defaults | Class |
| --- | --- | --- | --- |
| `Stack` | `gap`, `align` | `gap="none"`, `align="stretch"` | `.composition-stack` |
| `Inline` | `gap`, `align`, `justify`, `wrap` | `gap="none"`, `align="center"`, `justify="start"`, `wrap={true}` | `.composition-inline` |
| `Grid` | `gap`, `minItemWidth`, `align` | `gap="none"`, `minItemWidth="panel"`, `align="stretch"` | `.composition-grid` |
| `Container` | `size`, `align` | `size="content"`, `align="start"` | `.composition-container` |
| `Inset` | `space`, `inline`, `block` | all `none`; axis props override `space` | `.composition-inset` |
| `SplitBay` | `start`, `end`, `head`, `foot`, `overlay`, `toolbar`, `drawer` | `drawer={false}` | `.composition-split` |

Labeled sibling-key clusters use `KeyGroup`, which renders `Inline` (`gap="2.5"`, wrap, `role="group"`) plus `.key-group`. Height comes from `Key` `size`, not the cluster. Do not stretch grouped keys to a shared min-height.

`Grid.minItemWidth`: `compact` \| `control` \| `panel` \| `wide`. Columns use
`repeat(auto-fit, minmax(min(100%, var(--grid-min-*)), 1fr))`.

`Container.size`: `prose` (68ch) \| `form` (52rem) \| `content` (shell max) \| `full`.

Application shells wrap the main slot in `Inset` with `inline="5.5"` and
`block="4"` (`contain`, `composition-inset--shell-main`; see
[layouts](layouts.md) and `shellInset.ts`). `Inset` also accepts independent
`inline` and `block` props; axis values override `space`. Flush bays read the
same `--shell-main-inset-inline` / `--shell-main-inset-block` tokens through
`.workspace-area` or `.shell-main-pad`. That is shell padding, not a
`Container` width cap.

Spacing belongs to the parent via `gap` or `Inset`. Do not add spacer nodes or
arbitrary child margins to fake rhythm.

## Semantic examples

```tsx
<Stack as="section" gap="6" aria-labelledby="create-heading">
  <h2 id="create-heading">Create assessment Campaign</h2>
  <Container size="form">
    <Stack as="form" gap="5">{/* fields */}</Stack>
  </Container>
</Stack>

<Inline as="ul" gap="3" wrap>
  <li>Compact key</li>
</Inline>

<OperateArea
  className="workspace-area record-view"
  label="Evaluation record"
  title="Examination Transcript"
  description="Session 07"
  framed={false}
  headArrangement="plaque"
  back={<BackKey label="Queue" onClick={onQueue} />}
>
  <SplitBay start={<aside>Manifest</aside>} end={<aside>Criteria</aside>}>
    <div>Transcript</div>
  </SplitBay>
  <footer className="decision-bar">{/* note + keys */}</footer>
</OperateArea>
```

## Accessibility and reflow

- Preserve source order as reading and focus order. Do not reorder for visual
  effect.
- Intrinsic wrap and auto-fit reflow; 400% zoom must not clip focus or actions.
  Wrapping `Inline` children keep their content size and move to the next line;
  only `wrap={false}` shrinks children onto one row.
- Logical padding (`padding-block` / `padding-inline`) for `Inset`.
- Forced colors and print inherit hull hairlines from shared sheets; primitives
  carry no decorative color.

## Anti-patterns

- Arbitrary pixel `gap`/`padding` props or an `sx` bag
- A universal `Box` that recreates one-off CSS
- Wrapping every group in a smoked-glass plate
- Using primitives to infer authorization, routes, or product capability
- A second layout root (`data-layout`) inside a page
