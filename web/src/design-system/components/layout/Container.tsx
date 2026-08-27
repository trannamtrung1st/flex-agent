import { forwardRef, type ElementType, type Ref } from "react";
import { cx } from "../../../lib/cx";
import { renderPolymorphic, type PolymorphicComponent, type PolymorphicProps } from "./polymorphic";
import type { ContainerSize } from "./types";

type ContainerOwn = {
  size?: ContainerSize;
  align?: "start" | "center";
};

export const Container = forwardRef(function Container(
  { as, size = "content", align = "start", className, ...rest }: PolymorphicProps<ElementType, ContainerOwn>,
  ref: Ref<unknown>,
) {
  return renderPolymorphic(as, {
    ...rest,
    className: cx("composition-container", typeof className === "string" ? className : undefined),
    "data-flow-size": size,
    "data-flow-align": align,
  }, ref);
}) as PolymorphicComponent<ContainerOwn>;
