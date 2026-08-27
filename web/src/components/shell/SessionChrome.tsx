import type { ReactNode } from "react";
import { CommandStrip, Key, OperateArea } from "../../design-system";
import { ProtectedLoading } from "../ui/ProtectedLoading";
import { ThemeToggle } from "./ThemeToggle";

export function UnauthenticatedChrome({ children }: { children: ReactNode }) {
  return (
    <div className="workspace-root">
      <a href="#main-content" className="skip-link">Skip to main content</a>
      <CommandStrip homeTo="/" homeLabel="Home" origin identLeading={<ThemeToggle />} />
      <div id="main-content" className="workspace-main">
        {children}
      </div>
    </div>
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
        <ProtectedLoading label="Establishing session context…" />
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
