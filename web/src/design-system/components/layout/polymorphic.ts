import {
  createElement,
  type ComponentPropsWithRef,
  type ElementType,
  type ReactElement,
  type Ref,
} from "react";

type AsProp<E extends ElementType> = { as?: E };

type PropsToOmit<E extends ElementType, P> = keyof (AsProp<E> & P);

export type PolymorphicProps<E extends ElementType, P = object> = P &
  AsProp<E> &
  Omit<ComponentPropsWithRef<E>, PropsToOmit<E, P>>;

export type PolymorphicComponent<P, Default extends ElementType = "div"> = <
  E extends ElementType = Default,
>(
  props: PolymorphicProps<E, P>,
) => ReactElement | null;

export function renderPolymorphic(
  as: ElementType | undefined,
  props: Record<string, unknown>,
  ref: Ref<unknown>,
) {
  return createElement(as ?? "div", { ...props, ref });
}
