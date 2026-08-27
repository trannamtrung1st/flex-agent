import { useState } from "react";
import { exchangeScenarioGrant } from "../api/browser-client";
import { useBrowserApi } from "../api/browser-api";
import { Alert } from "../components/ui/Alert";
import { Button } from "../components/ui/Button";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";

export function AuthGatePage() {
  const { refresh, apiState } = useBrowserApi();
  const [grantInput, setGrantInput] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [exchanging, setExchanging] = useState(false);

  const exchangeGrant = async (token: string) => {
    if (!token.trim()) {
      setError("Grant token is required.");
      return;
    }

    setExchanging(true);
    setError(null);

    try {
      await exchangeScenarioGrant(token.trim());
      setGrantInput("");
      await refresh();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Grant exchange failed");
    } finally {
      setExchanging(false);
    }
  };

  if (exchanging || apiState === "loading") {
    return <ProtectedLoading label="Establishing synthetic session…" />;
  }

  return (
    <div className="shell-content" style={{ padding: "2rem 1.25rem" }}>
      <StatusPanel title="Sign in required">
        <p>
          This workspace requires an authenticated application session. Production sign-in is not
          available in this synthetic P0 surface.
        </p>
      </StatusPanel>

      <section className="page-section" aria-labelledby="grant-heading">
        <h2 id="grant-heading">Test harness grant</h2>
        <p>
          For automated and local verification, paste a one-time scenario grant issued by the
          synthetic test harness. Grants are never accepted from URL parameters.
        </p>

        {error ? (
          <Alert variant="danger" title="Authentication failed" className="page-section">
            {error}
          </Alert>
        ) : null}

        <div className="field page-section">
          <label className="field-label" htmlFor="grant-token">Grant token</label>
          <input
            id="grant-token"
            className="input"
            type="text"
            value={grantInput}
            onChange={(event) => { setGrantInput(event.target.value); }}
            placeholder="Paste grant token from the test harness"
            aria-describedby="grant-hint"
            autoComplete="off"
          />
          <p id="grant-hint" style={{ color: "var(--fg-muted)", fontSize: "0.875rem" }}>
            Grants are single-use, expire quickly, and must be exchanged through this form.
          </p>
        </div>

        <Button onClick={() => void exchangeGrant(grantInput)} disabled={exchanging}>
          Exchange grant
        </Button>
      </section>
    </div>
  );
}
