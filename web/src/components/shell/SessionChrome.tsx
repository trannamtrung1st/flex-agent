import type { ReactNode } from "react";
import { Key, LayoutAssignment, ManagementLayout, OperateArea, WaitPanel, EmptyPlate } from "../../design-system";
import { cx } from "../../lib/cx";
import { ThemeToggle } from "./ThemeToggle";
import { useProductionApi } from "../../api/production-api";

const statusStrip = {
  homeTo: "/",
  homeLabel: "Home",
  origin: true,
  identLeading: <ThemeToggle />,
} as const;

export function CeremonyEmpty({
  note,
  children,
  alert,
}: {
  note: string;
  children?: ReactNode;
  alert?: boolean;
}) {
  return (
    <EmptyPlate className="ceremony-empty empty-plate--inset" note={note} noteRole={alert ? "alert" : undefined}>
      {children}
    </EmptyPlate>
  );
}

export function CeremonyArea({
  title,
  description,
  label,
  danger,
  children,
}: {
  title: string;
  description?: string;
  label: string;
  danger?: boolean;
  children?: ReactNode;
}) {
  return (
    <OperateArea
      className={cx("workspace-area", "work-plane", "work-plane--ceremony", danger && "workspace-area--danger")}
      frameClassName="ceremony-frame"
      label={label}
      title={title}
      description={description}
    >
      {children}
    </OperateArea>
  );
}

export function UnauthenticatedChrome({ children }: { children: ReactNode }) {
  return (
    <LayoutAssignment id="management">
      <ManagementLayout contain={false} commandStrip={{ ...statusStrip }}>
        {children}
      </ManagementLayout>
    </LayoutAssignment>
  );
}

export function SessionLoadingScreen() {
  return (
    <UnauthenticatedChrome>
      <CeremonyArea
        label="Establishing session"
        title="Establishing session"
        description="Confirming the production application session for this organization."
      >
        <WaitPanel label="Establishing session context…" />
      </CeremonyArea>
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
      <CeremonyArea label={title} title={title} danger={variant === "danger"}>
        {children}
      </CeremonyArea>
    </UnauthenticatedChrome>
  );
}

export function AccessChangedScreen() {
  const { login } = useProductionApi();
  return (
    <SessionStatusScreen title="Your access changed" variant="danger">
      <CeremonyEmpty note="This destination is not available for the current authorized relationship.">
        <Key variant="transmit" onClick={() => { login(); }}>
          Continue to sign in
        </Key>
      </CeremonyEmpty>
    </SessionStatusScreen>
  );
}

export function SignOutRetryKey({ onRetry }: { onRetry: () => void }) {
  return (
    <Key variant="quiet" onClick={onRetry}>
      Try again
    </Key>
  );
}
