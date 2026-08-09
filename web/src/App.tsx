import { useEffect, useState } from "react";

type Theme = "light" | "dark";

function resolveInitialTheme(): Theme {
  if (typeof window === "undefined") {
    return "dark";
  }

  const stored = window.localStorage.getItem("flex-agent-theme");
  if (stored === "light" || stored === "dark") {
    return stored;
  }

  return window.matchMedia("(prefers-color-scheme: dark)").matches
    ? "dark"
    : "light";
}

export function App() {
  const [theme, setTheme] = useState<Theme>(resolveInitialTheme);

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    window.localStorage.setItem("flex-agent-theme", theme);
  }, [theme]);

  return (
    <div className="app-shell">
      <header className="app-header">
        <p className="eyebrow">Development smoke surface</p>
        <h1>Flex Agent workspace scaffold</h1>
        <p className="lede">
          This page verifies the React/Vite shell, semantic design tokens, and
          accessibility structure. It is not a product capability.
        </p>
      </header>

      <main className="app-main">
        <section className="status-panel" aria-labelledby="status-heading" aria-live="polite">
          <h2 id="status-heading">Runtime status</h2>
          <dl className="status-grid">
            <div>
              <dt>Surface</dt>
              <dd>SPA development smoke</dd>
            </div>
            <div>
              <dt>Theme</dt>
              <dd>{theme}</dd>
            </div>
            <div>
              <dt>Authority</dt>
              <dd>Browser presentation only</dd>
            </div>
          </dl>
        </section>

        <div className="actions">
          <button
            type="button"
            className="button-primary"
            onClick={() => {
              setTheme((current) => (current === "dark" ? "light" : "dark"));
            }}
          >
            Switch to {theme === "dark" ? "light" : "dark"} theme
          </button>
        </div>
      </main>

      <footer className="app-footer">
        <p>Flex Agent — provider-independent foundation scaffold</p>
      </footer>
    </div>
  );
}
