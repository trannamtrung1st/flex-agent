import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { ReviewWorkProjectionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Card, CardBody, CardHeader, CardTitle } from "../components/ui/Card";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";

export function ReviewWorkPage() {
  const { fetchJson } = useBrowserApi();
  const [data, setData] = useState<ReviewWorkProjectionV1 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    void fetchJson<ReviewWorkProjectionV1>("/browser/review-work")
      .then((projection) => {
        if (active) {
          setData(projection);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load review work");
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [fetchJson]);

  if (loading) {
    return <ProtectedLoading label="Loading review queue…" />;
  }

  if (error) {
    return <Alert variant="danger" title="Could not load review work">{error}</Alert>;
  }

  return (
    <div>
      <header className="page-header">
        <h1>Review work</h1>
        <p>Cases awaiting human review decisions.</p>
      </header>

      <section className="page-section" aria-labelledby="review-cases-heading">
        <h2 id="review-cases-heading">Review cases</h2>

        {data?.cases.length === 0 ? (
          <p className="empty-state">No review cases are assigned.</p>
        ) : (
          <ul className="stack" aria-label="Review cases">
            {data?.cases.map((caseItem) => (
              <li key={caseItem.case_id}>
                <Card interactive>
                  <Link className="work-item-link" to={caseItem.route ?? `/review-work/${caseItem.case_id}`}>
                    <CardHeader>
                      <CardTitle>{caseItem.title}</CardTitle>
                    </CardHeader>
                    <CardBody>
                      <Badge variant="warning">{caseItem.status_label}</Badge>
                    </CardBody>
                  </Link>
                </Card>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
