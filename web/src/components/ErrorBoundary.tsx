import { Component, type ErrorInfo, type ReactNode } from "react";
import { CeremonyUnavailable, LayoutAssignment, ManagementLayout } from "../design-system";
import { ThemeToggle } from "./shell/ThemeToggle";

interface ErrorBoundaryProps {
  children: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  public state: ErrorBoundaryState = { hasError: false };

  public static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true };
  }

  public componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error("Workspace error", error, info);
  }

  public render(): ReactNode {
    if (this.state.hasError) {
      return (
        <LayoutAssignment id="management">
          <ManagementLayout commandStrip={{ homeTo: "/", homeLabel: "Home", identLeading: <ThemeToggle /> }}>
            <CeremonyUnavailable
              danger
              title="Something went wrong"
              note="Reload the page to continue. Work already stored on the server is unchanged."
              recovery={{ label: "Reload", onClick: () => window.location.reload() }}
            />
          </ManagementLayout>
        </LayoutAssignment>
      );
    }

    return this.props.children;
  }
}
