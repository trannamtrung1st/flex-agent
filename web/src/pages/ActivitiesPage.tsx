import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { ActivitiesListProjectionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Card, CardBody, CardHeader, CardTitle } from "../components/ui/Card";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";

export function ActivitiesPage() {
  const { fetchJson } = useBrowserApi();
  const [data, setData] = useState<ActivitiesListProjectionV1 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    void fetchJson<ActivitiesListProjectionV1>("/browser/activities")
      .then((projection) => {
        if (active) {
          setData(projection);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load activities");
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [fetchJson]);

  if (loading) {
    return <ProtectedLoading label="Loading activities…" />;
  }

  if (error) {
    return <Alert variant="danger" title="Could not load activities">{error}</Alert>;
  }

  return (
    <div>
      <header className="page-header">
        <h1>Activities</h1>
        <p>Manage Campaign and Activity lifecycle for your organization.</p>
      </header>

      <section className="page-section" aria-labelledby="activities-list-heading">
        <h2 id="activities-list-heading">Activity list</h2>

        {data?.activities.length === 0 ? (
          <p className="empty-state">No activities are available.</p>
        ) : (
          <ul className="stack" aria-label="Activities">
            {data?.activities.map((activity) => (
              <li key={activity.activity_id}>
                <Card interactive>
                  <Link className="work-item-link" to={activity.route ?? `/activities/${activity.activity_id}`}>
                    <CardHeader>
                      <CardTitle>{activity.title}</CardTitle>
                    </CardHeader>
                    <CardBody>
                      <div className="stack">
                        <Badge variant="info">{activity.form}</Badge>
                        <Badge variant="default">{activity.status_label}</Badge>
                      </div>
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
