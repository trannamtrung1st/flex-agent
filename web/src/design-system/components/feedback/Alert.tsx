import type { ReactNode } from "react";
import { Advisory } from "../chrome/OperateHead";
import { Stack } from "../layout/Stack";

export function Alert({
  variant = "info",
  title,
  children,
}: {
  variant?: "info" | "success" | "warning" | "danger";
  title?: string;
  children?: ReactNode;
}) {
  return (
    <Stack role={variant === "danger" ? "alert" : "status"} className="workspace-alert" gap="none">
      {title ? (
        <Advisory
          label={variant === "danger" ? "Error" : "Note"}
          copy={title}
          attention={variant === "danger"}
          live={false}
        />
      ) : null}
      {children ? <div className="workspace-alert-body">{children}</div> : null}
    </Stack>
  );
}
