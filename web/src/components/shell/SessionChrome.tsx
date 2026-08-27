import type { ReactNode } from "react";
import { Key, LayoutAssignment, ManagementLayout, OperateArea, WaitPanel } from "../../design-system";
import { ThemeToggle } from "./ThemeToggle";

const statusStrip = {
  homeTo: "/",
  homeLabel: "Home",
  origin: true,
  identLeading: <ThemeToggle />,
} as const;

export function UnauthenticatedChrome({ children }: { children: ReactNode }) {
  return (
    <LayoutAssignment id="management">
      <ManagementLayout commandStrip={{ ...statusStrip }}>
        {children}
      </ManagementLayout>
    </LayoutAssignment>
  );
}

export function SessionLoadingScreen() {
  return (
    <UnauthenticatedChrome>
      <OperateArea
        className="workspace-area"
        label="Establishing session"
        title="Establishing session"
        description="Confirming the production application session for this organization."
      >
        <WaitPanel label="Establishing session context…" />
      </OperateArea>
    </UnauthenticatedChrome>
  );
}

export function SessionStatusScreen({
  title,
  children,
  variant,
}: {
  title: string;
  children: ReactNode;
  variant?: "default" | "danger";
}) {
  return (
    <UnauthenticatedChrome>
      <OperateArea
        className={variant === "danger" ? "workspace-area workspace-area--danger" : "workspace-area"}
        label={title}
        title={title}
      >
        {children}
      </OperateArea>
    </UnauthenticatedChrome>
  );
}

export function SignOutRetryKey({ onRetry }: { onRetry: () => void }) {
  return (
    <Key variant="quiet" onClick={onRetry}>
      Try again
    </Key>
  );
}
