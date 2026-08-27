import { forwardRef, type ElementType, type Ref } from "react";
import { cx } from "../../../lib/cx";
import { renderPolymorphic, type PolymorphicComponent, type PolymorphicProps } from "./polymorphic";
import type { GridMinItemWidth, LayoutAlign, LayoutSpace } from "./types";

type GridOwn = {
  gap?: LayoutSpace;
  minItemWidth?: GridMinItemWidth;
  align?: Exclude<LayoutAlign, "baseline">;
};

export const Grid = forwardRef(function Grid(
  {
    as,
    gap = "none",
    minItemWidth = "panel",
    align = "stretch",
    className,
    ...rest
  }: PolymorphicProps<ElementType, GridOwn>,
  ref: Ref<unknown>,
) {
  return renderPolymorphic(as, {
    ...rest,
    className: cx("composition-grid", typeof className === "string" ? className : undefined),
    "data-flow-gap": gap,
    "data-flow-min": minItemWidth,
    "data-flow-align": align,
  }, ref);
}) as PolymorphicComponent<GridOwn>;
