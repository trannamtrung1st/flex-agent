import type { ReactNode } from "react";
import { cx } from "../../../lib/cx";
import { Inline } from "../layout/Inline";
import { Stack } from "../layout/Stack";

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
  const heading = (
    <h1 className="operate-title" tabIndex={titleTabIndex}>
      {title}
    </h1>
  );
  const descriptionNode = description ? <p className="page-desc">{description}</p> : null;
  const copy = plaque ? null : (
    <Stack className="operate-head-copy" gap="2.5" align="start">
      {heading}
      {descriptionNode}
    </Stack>
  );
  return (
    <Root
      className={cx("operate-head", plaque && "operate-head--plaque", className)}
      data-head-arrange={plaque ? "plaque" : undefined}
    >
      {plaque ? (
        <>
          {back}
          <div className="operate-head-cluster">
            {heading}
            {headExtra}
          </div>
          {descriptionNode}
        </>
      ) : back ? (
        <Inline className="operate-head-mast" gap="3" align="start" justify="between" wrap={false}>
          {copy}
          {back}
        </Inline>
      ) : (
        copy
      )}
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
