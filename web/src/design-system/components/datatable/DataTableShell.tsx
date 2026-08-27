import { useRef, type HTMLAttributes, type ReactNode, type Ref } from "react";
import { useDatatableStickyRail } from "./useDatatableStickyRail";

type TableSlot = {
  table: ReactNode;
  body?: never;
};

type BodySlot = {
  table?: never;
  body: ReactNode;
};

export type DataTableShellProps = (TableSlot | BodySlot) & {
  variant?: "complete" | "bodyOnly";
  className?: string;
  toolbar?: ReactNode;
  empty?: ReactNode;
  footer?: ReactNode;
  scrollClassName?: string;
  scrollProps?: Omit<HTMLAttributes<HTMLDivElement>, "className" | "children">;
};

function assignRef<T>(ref: Ref<T> | undefined, value: T | null) {
  if (!ref) return;
  if (typeof ref === "function") ref(value);
  else ref.current = value;
}

export function DataTableShell({
  variant = "complete",
  className,
  toolbar,
  table,
  body,
  empty,
  footer,
  scrollClassName,
  scrollProps,
}: DataTableShellProps) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const { ref: scrollPropsRef, ...restScrollProps } = (scrollProps ?? {}) as typeof scrollProps & {
    ref?: Ref<HTMLDivElement>;
  };
  useDatatableStickyRail(scrollRef);

  const rootClassName = [
    "datatable",
    variant === "bodyOnly" ? "datatable--body-only" : null,
    className,
  ]
    .filter(Boolean)
    .join(" ");
  const resolvedScrollClassName = ["datatable-scroll", scrollClassName].filter(Boolean).join(" ");

  return (
    <div className={rootClassName}>
      {toolbar}
      <div
        {...restScrollProps}
        ref={(node) => {
          scrollRef.current = node;
          assignRef(scrollPropsRef, node);
        }}
        className={resolvedScrollClassName}
      >
        {table ?? body}
        {empty}
      </div>
      {footer}
    </div>
  );
}
