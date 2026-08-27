import type { ReactNode } from "react";
import { Advisory } from "../../design-system";

export function Alert({
  variant = "info",
  title,
  children,
}: {
  variant?: "info" | "success" | "warning" | "danger";
  title?: string;
  children: ReactNode;
}) {
  return (
    <div role={variant === "danger" ? "alert" : "status"} className="workspace-alert">
      {title ? (
        <Advisory
          label={variant === "danger" ? "Error" : "Note"}
          copy={title}
          attention={variant === "danger"}
        />
      ) : null}
      <div className="workspace-alert-body">{children}</div>
    </div>
  );
}
