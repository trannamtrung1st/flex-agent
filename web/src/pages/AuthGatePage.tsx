import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { exchangeScenarioGrant } from "../api/browser-client";
import { useBrowserApi } from "../api/browser-api";
import { Alert } from "../components/ui/Alert";
import { Button } from "../components/ui/Button";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";

export function AuthGatePage() {
  const { refresh, apiState } = useBrowserApi();
  const [searchParams, setSearchParams] = useSearchParams();
  const [grantInput, setGrantInput] = useState(searchParams.get("grant") ?? "");
  const [error, setError] = useState<string | null>(null);
  const [exchanging, setExchanging] = useState(false);
  const attemptedUrlGrant = useRef(false);

  const exchangeGrant = async (token: string) => {
    if (!token.trim()) {
      setError("Grant token is required.");
      return;
    }

    setExchanging(true);
    setError(null);

    try {
      await exchangeScenarioGrant(token.trim());
      setSearchParams({});
      await refresh();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Grant exchange failed");
    } finally {
      setExchanging(false);
    }
  };

  useEffect(() => {
    const grantFromUrl = searchParams.get("grant");
    if (grantFromUrl && apiState === "idle" && !attemptedUrlGrant.current) {
      attemptedUrlGrant.current = true;
      void exchangeGrant(grantFromUrl);
    }
  }, [searchParams, apiState]);

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
          For automated and local verification, exchange a one-time scenario grant from the
          synthetic API test harness.
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
            placeholder="Paste grant token or use ?grant= in the URL"
            aria-describedby="grant-hint"
          />
          <p id="grant-hint" style={{ color: "var(--fg-muted)", fontSize: "0.875rem" }}>
            Grants are single-use and expire quickly. No role switcher is provided in product UI.
          </p>
        </div>

        <Button onClick={() => void exchangeGrant(grantInput)} disabled={exchanging}>
          Exchange grant
        </Button>
      </section>
    </div>
  );
}
