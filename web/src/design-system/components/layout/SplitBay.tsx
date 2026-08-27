import { forwardRef, type HTMLAttributes, type ReactNode } from "react";
import { cx } from "../../../lib/cx";

export type SplitBayProps = HTMLAttributes<HTMLDivElement> & {
  start?: ReactNode;
  end?: ReactNode;
  head?: ReactNode;
  foot?: ReactNode;
  overlay?: ReactNode;
  toolbar?: ReactNode;
  drawer?: boolean;
  children: ReactNode;
};

export const SplitBay = forwardRef<HTMLDivElement, SplitBayProps>(function SplitBay(
  { start, end, head, foot, overlay, toolbar, drawer = false, className, children, ...rest },
  ref,
) {
  return (
    <div
      {...rest}
      ref={ref}
      className={cx("composition-split", className)}
      data-flow-split={drawer ? "drawer" : "bay"}
      data-flow-head={head ? "true" : undefined}
      data-flow-foot={foot ? "true" : undefined}
    >
      {overlay ? (
        <div className="composition-split__overlay" aria-hidden="true">
          {overlay}
        </div>
      ) : null}
      {head ? <div className="composition-split__head">{head}</div> : null}
      {toolbar ? <div className="composition-split__toolbar">{toolbar}</div> : null}
      {start ? <div className="composition-split__start">{start}</div> : null}
      <div className="composition-split__main">{children}</div>
      {end ? <div className="composition-split__end">{end}</div> : null}
      {foot ? <div className="composition-split__foot">{foot}</div> : null}
    </div>
  );
});
