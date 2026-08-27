import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { ResultsProjectionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Card, CardBody, CardHeader, CardTitle } from "../components/ui/Card";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";

export function ResultsPage() {
  const { fetchJson } = useBrowserApi();
  const [data, setData] = useState<ResultsProjectionV1 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    void fetchJson<ResultsProjectionV1>("/browser/results")
      .then((projection) => {
        if (active) {
          setData(projection);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load results");
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [fetchJson]);

  if (loading) {
    return <ProtectedLoading label="Loading results…" />;
  }

  if (error) {
    return <Alert variant="danger" title="Could not load results">{error}</Alert>;
  }

  return (
    <div>
      <header className="page-header">
        <h1>Results</h1>
        <p>Participant-visible outcomes for your assignments.</p>
      </header>

      <section className="page-section" aria-labelledby="results-list-heading">
        <h2 id="results-list-heading">Result list</h2>

        <ul className="stack" aria-label="Results">
          {data?.results.map((result) => (
            <li key={result.result_id}>
              <Card interactive>
                <Link className="work-item-link" to={result.route ?? `/results/${result.result_id}`}>
                  <CardHeader>
                    <CardTitle>{result.activity_title}</CardTitle>
                  </CardHeader>
                  <CardBody>
                    <Badge
                      variant={
                        result.status_label === "Released"
                          ? "success"
                          : result.status_label === "Not yet available"
                            ? "default"
                            : "info"
                      }
                    >
                      {result.status_label}
                    </Badge>
                  </CardBody>
                </Link>
              </Card>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
