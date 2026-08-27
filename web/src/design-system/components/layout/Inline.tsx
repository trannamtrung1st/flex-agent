import { forwardRef, type ElementType, type Ref } from "react";
import { cx } from "../../../lib/cx";
import { renderPolymorphic, type PolymorphicComponent, type PolymorphicProps } from "./polymorphic";
import type { LayoutAlign, LayoutJustify, LayoutSpace } from "./types";

type InlineOwn = {
  gap?: LayoutSpace;
  align?: LayoutAlign;
  justify?: LayoutJustify;
  wrap?: boolean;
};

export const Inline = forwardRef(function Inline(
  {
    as,
    gap = "none",
    align = "center",
    justify = "start",
    wrap = true,
    className,
    ...rest
  }: PolymorphicProps<ElementType, InlineOwn>,
  ref: Ref<unknown>,
) {
  return renderPolymorphic(as, {
    ...rest,
    className: cx("composition-inline", typeof className === "string" ? className : undefined),
    "data-flow-gap": gap,
    "data-flow-align": align,
    "data-flow-justify": justify,
    "data-flow-wrap": wrap ? "true" : "false",
  }, ref);
}) as PolymorphicComponent<InlineOwn>;
