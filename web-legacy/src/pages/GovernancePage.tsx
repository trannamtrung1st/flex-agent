import { useEffect, useState } from "react";
import { useBrowserApi } from "../api/browser-api";
import type { GovernanceProjectionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { DataTable } from "../components/ui/DataTable";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";

export function GovernancePage() {
  const { fetchJson } = useBrowserApi();
  const [data, setData] = useState<GovernanceProjectionV1 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    void fetchJson<GovernanceProjectionV1>("/browser/governance")
      .then((projection) => {
        if (active) {
          setData(projection);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load governance");
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [fetchJson]);

  if (loading) {
    return <ProtectedLoading label="Loading governance history…" />;
  }

  if (error) {
    return <Alert variant="danger" title="Could not load governance">{error}</Alert>;
  }

  return (
    <div>
      <header className="page-header">
        <h1>Governance</h1>
        <p>Authorized audit and provenance history for this organization.</p>
      </header>

      {data?.is_partial ? (
        <Alert variant="warning" title="Partial P0 coverage" className="page-section">
          Governance history in P0 exposes only separately authorized partial paths. Full
          provenance surfaces are planned for later releases.
        </Alert>
      ) : null}

      <section className="page-section" aria-labelledby="audit-heading">
        <h2 id="audit-heading">Audit entries</h2>
        <DataTable
          caption="Governance audit entries"
          rows={data?.entries ?? []}
          getRowKey={(row) => row.entry_id}
          emptyMessage="No audit entries are available."
          columns={[
            { id: "action", header: "Action", cell: (row) => row.action },
            { id: "actor", header: "Actor", cell: (row) => row.actor_label },
            { id: "time", header: "Occurred", cell: (row) => row.occurred_at },
            {
              id: "outcome",
              header: "Outcome",
              cell: (row) => (
                <Badge variant={row.outcome === "succeeded" ? "success" : "danger"}>
                  {row.outcome}
                </Badge>
              ),
            },
          ]}
        />
      </section>
    </div>
  );
}
