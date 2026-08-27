import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";

export function Announcer({ message }: { message: string }) {
  return (
    <div className="visually-hidden" aria-live="polite">
      {message}
    </div>
  );
}

export function OperateHead({
  title,
  description,
  back,
  className,
  titleTabIndex,
  headExtra,
  arrangement = "stack",
}: {
  title: string;
  description?: string;
  back?: ReactNode;
  className?: string;
  titleTabIndex?: number;
  headExtra?: ReactNode;
  arrangement?: "stack" | "plaque";
}) {
  const plaque = arrangement === "plaque";
  const Root = plaque ? "header" : "div";
  return (
    <Root
      className={cx("operate-head", plaque && "operate-head--plaque", className)}
      data-head-arrange={plaque ? "plaque" : undefined}
    >
      {back}
      {plaque ? (
        <div className="operate-head-cluster">
          <h1 className="operate-title" tabIndex={titleTabIndex}>
            {title}
          </h1>
          {headExtra}
        </div>
      ) : (
        <h1 className="operate-title" tabIndex={titleTabIndex}>
          {title}
        </h1>
      )}
      {description ? <p className="page-desc">{description}</p> : null}
      {plaque ? null : headExtra}
    </Root>
  );
}

export function Advisory({
  label,
  copy,
  attention,
  className,
  live = true,
}: {
  label: string;
  copy: string;
  attention?: boolean;
  className?: string;
  /** When false, omit role so a parent live region owns the announcement. */
  live?: boolean;
}) {
  return (
    <div
      className={cx("advisory", attention && "advisory--attention", className)}
      role={live ? "status" : undefined}
    >
      <span className="advisory-label">{label}</span>
      <span className="advisory-copy">{copy}</span>
    </div>
  );
}

export function ConsoleFoot({
  note,
  children,
}: {
  note: string;
  children?: ReactNode;
}) {
  return (
    <footer className="console-foot">
      <p className="foot-note bar-note">{note}</p>
      {children}
    </footer>
  );
}
