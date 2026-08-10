import { Outlet } from "react-router-dom";
import { useBrowserApi } from "../../api/browser-api";
import { ProtectedLoading } from "../ui/ProtectedLoading";
import { StatusPanel } from "../ui/StatusPanel";
import { Breadcrumbs } from "./Breadcrumbs";
import { Navigation } from "./Navigation";
import { ThemeToggle } from "./ThemeToggle";

export function AppShell() {
  const { actor, apiState, errorMessage } = useBrowserApi();

  if (apiState === "loading") {
    return <ProtectedLoading label="Establishing session context…" />;
  }

  if (apiState === "denied") {
    return (
      <div className="shell-content" style={{ padding: "2rem 1.25rem" }}>
        <StatusPanel title="Access denied" variant="danger">
          <p>{errorMessage ?? "You do not have access to this content."}</p>
        </StatusPanel>
      </div>
    );
  }

  if (apiState === "error") {
    return (
      <div className="shell-content" style={{ padding: "2rem 1.25rem" }}>
        <StatusPanel title="Unable to load workspace" variant="danger">
          <p>{errorMessage ?? "An unexpected error occurred. Try refreshing the page."}</p>
        </StatusPanel>
      </div>
    );
  }

  return (
    <div className="shell">
      <a href="#main-content" className="skip-link">Skip to main content</a>

      <header className="shell-header" role="banner">
        <div className="shell-brand">
          <span className="shell-org">{actor?.organization_name ?? "Flex Agent"}</span>
          <span className="shell-title">Activity workspace</span>
        </div>
        <div className="shell-header-actions">
          <ThemeToggle />
        </div>
      </header>

      <div className="shell-nav-mobile">
        <Navigation layout="mobile" />
      </div>

      <div className="shell-body">
        <aside className="shell-nav-rail" aria-label="Workspace navigation">
          <Navigation layout="rail" />
        </aside>

        <main id="main-content" className="shell-main">
          <div className="shell-content">
            <Breadcrumbs />
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
