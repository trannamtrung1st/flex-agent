import type { ReactNode } from "react";

type AlertVariant = "info" | "success" | "warning" | "danger";

interface AlertProps {
  variant?: AlertVariant;
  title?: string;
  children: ReactNode;
  className?: string;
}

export function Alert({ variant = "info", title, children, className = "" }: AlertProps) {
  const classes = ["alert", `alert-${variant}`, className].filter(Boolean).join(" ");

  return (
    <div className={classes} role={variant === "danger" ? "alert" : "status"}>
      {title ? <p className="alert-title">{title}</p> : null}
      <div className="alert-body">{children}</div>
    </div>
  );
}
