import type { ReactNode } from "react";
import { CeremonyArea, CeremonyEmpty, CeremonyUnavailable, CeremonyWait, Key, LayoutAssignment, ManagementLayout } from "../../design-system";
import { ThemeToggle } from "./ThemeToggle";
import { useProductionApi } from "../../api/production-api";

const statusStrip = {
  homeTo: "/",
  homeLabel: "Home",
  identLeading: <ThemeToggle />,
} as const;

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
        <CeremonyWait label="Establishing session context…" />
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
    <UnauthenticatedChrome>
      <CeremonyUnavailable
        title="Your access changed"
        danger
        note="This destination is not available for the current authorized relationship."
        recovery={{
          label: "Continue to sign in",
          variant: "transmit",
          onClick: () => { login(); },
        }}
      />
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

export function SigningOutScreen({
  errorMessage,
  onRetry,
}: {
  errorMessage?: string | null;
  onRetry: () => void;
}) {
  return (
    <SessionStatusScreen title="Signing out">
      {errorMessage ? (
        <CeremonyEmpty note={errorMessage} alert>
          <SignOutRetryKey onRetry={onRetry} />
        </CeremonyEmpty>
      ) : (
        <CeremonyWait label="Signing out…" />
      )}
    </SessionStatusScreen>
  );
}
