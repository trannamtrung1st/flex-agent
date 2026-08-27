import type { ReactNode } from "react";

interface StatusPanelProps {
  title: string;
  children: ReactNode;
  variant?: "default" | "danger";
}

export function StatusPanel({ title, children, variant = "default" }: StatusPanelProps) {
  const classes = ["status-panel", variant === "danger" ? "alert-danger" : ""].filter(Boolean).join(" ");

  return (
    <section className={classes} aria-labelledby="status-panel-title">
      <h2 id="status-panel-title" className="status-panel-title">{title}</h2>
      <div className="status-panel-body">{children}</div>
    </section>
  );
}
