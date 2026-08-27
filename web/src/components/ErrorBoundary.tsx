import { Component, type ErrorInfo, type ReactNode } from "react";

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
    console.error("Candidate workspace error", error, info);
  }

  public render(): ReactNode {
    if (this.state.hasError) {
      return (
        <main className="candidate-workspace">
          <p className="candidate-workspace__kicker">Workspace</p>
          <h1>Something went wrong</h1>
          <p>Reload the page. The production SPA is unchanged until cutover.</p>
        </main>
      );
    }

    return this.props.children;
  }
}
