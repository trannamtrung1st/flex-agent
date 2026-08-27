import { useId, type ReactNode } from "react";
import { cx } from "../../lib/cx";

export function StatusPanel({
  title,
  children,
  variant = "default",
}: {
  title: string;
  children: ReactNode;
  variant?: "default" | "danger";
}) {
  const titleId = useId();
  return (
    <section className={cx("status-panel", variant === "danger" && "status-panel--danger")} aria-labelledby={titleId}>
      <h2 id={titleId} className="operate-title">
        {title}
      </h2>
      <div className="status-panel-body">{children}</div>
    </section>
  );
}
