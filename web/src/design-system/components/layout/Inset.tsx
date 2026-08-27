import { forwardRef, type ElementType, type Ref } from "react";
import { cx } from "../../../lib/cx";
import { renderPolymorphic, type PolymorphicComponent, type PolymorphicProps } from "./polymorphic";
import type { LayoutSpace } from "./types";

type InsetOwn = {
  space?: LayoutSpace;
  inline?: LayoutSpace;
  block?: LayoutSpace;
};

export const Inset = forwardRef(function Inset(
  { as, space = "none", inline, block, className, ...rest }: PolymorphicProps<ElementType, InsetOwn>,
  ref: Ref<unknown>,
) {
  return renderPolymorphic(as, {
    ...rest,
    className: cx("composition-inset", typeof className === "string" ? className : undefined),
    "data-flow-space": space,
    "data-flow-inline": inline ?? space,
    "data-flow-block": block ?? space,
  }, ref);
}) as PolymorphicComponent<InsetOwn>;
