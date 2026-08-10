import { useEffect, useState } from "react";
import { useBrowserApi } from "../api/browser-api";
import type { PlannedTierProjectionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";

export function HarnessesPage() {
  const { fetchJson } = useBrowserApi();
  const [data, setData] = useState<PlannedTierProjectionV1 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    void fetchJson<PlannedTierProjectionV1>("/browser/planned-tier/harnesses")
      .then((projection) => {
        if (active) {
          setData(projection);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load harnesses module");
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [fetchJson]);

  if (loading) {
    return <ProtectedLoading label="Loading harnesses module…" />;
  }

  if (error) {
    return <Alert variant="danger" title="Could not load harnesses">{error}</Alert>;
  }

  return (
    <div>
      <header className="page-header">
        <h1>Harnesses</h1>
        <p>
          <Badge variant="tier">P1</Badge>
        </p>
      </header>

      <Alert variant="info" title="Planned capability">
        {data?.message}
      </Alert>
    </div>
  );
}
