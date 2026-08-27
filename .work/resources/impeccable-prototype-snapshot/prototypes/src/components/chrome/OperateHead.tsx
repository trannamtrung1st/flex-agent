import type { ReactNode } from "react";
import { cx } from "../../lib/cx";

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
}: {
  title: string;
  description?: string;
  back?: ReactNode;
  className?: string;
  titleTabIndex?: number;
}) {
  return (
    <div className={cx("operate-head", className)}>
      {back}
      <h1 className="operate-title" tabIndex={titleTabIndex}>
        {title}
      </h1>
      {description ? <p className="page-desc">{description}</p> : null}
    </div>
  );
}

export function Advisory({
  label,
  copy,
  attention,
  className,
}: {
  label: string;
  copy: string;
  attention?: boolean;
  className?: string;
}) {
  return (
    <div className={cx("advisory", attention && "advisory--attention", className)} role="status">
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
