import { forwardRef, type ElementType, type Ref } from "react";
import { cx } from "../../../lib/cx";
import { renderPolymorphic, type PolymorphicComponent, type PolymorphicProps } from "./polymorphic";
import type { LayoutAlign, LayoutSpace } from "./types";

type StackOwn = {
  gap?: LayoutSpace;
  align?: LayoutAlign;
};

export const Stack = forwardRef(function Stack(
  { as, gap = "none", align = "stretch", className, ...rest }: PolymorphicProps<ElementType, StackOwn>,
  ref: Ref<unknown>,
) {
  return renderPolymorphic(as, {
    ...rest,
    className: cx("composition-stack", typeof className === "string" ? className : undefined),
    "data-flow-gap": gap,
    "data-flow-align": align,
  }, ref);
}) as PolymorphicComponent<StackOwn>;
