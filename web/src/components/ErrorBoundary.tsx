import { Component, type ErrorInfo, type ReactNode } from "react";
import { Key, LayoutAssignment, ManagementLayout, OperateArea } from "../design-system";

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
          <ManagementLayout commandStrip={{ homeTo: "/", homeLabel: "Home", origin: true }}>
            <OperateArea
              className="workspace-area workspace-area--danger"
              label="Something went wrong"
              title="Something went wrong"
              description="Reload the page to continue. Work already stored on the server is unchanged."
            >
              <Key onClick={() => window.location.reload()}>Reload</Key>
            </OperateArea>
          </ManagementLayout>
        </LayoutAssignment>
      );
    }

    return this.props.children;
  }
}
