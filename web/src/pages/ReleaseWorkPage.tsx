import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { ReleaseWorkProjectionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Card, CardBody, CardHeader, CardTitle } from "../components/ui/Card";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";

export function ReleaseWorkPage() {
  const { fetchJson } = useBrowserApi();
  const [data, setData] = useState<ReleaseWorkProjectionV1 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    void fetchJson<ReleaseWorkProjectionV1>("/browser/release-work")
      .then((projection) => {
        if (active) {
          setData(projection);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load release work");
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [fetchJson]);

  if (loading) {
    return <ProtectedLoading label="Loading release queue…" />;
  }

  if (error) {
    return <Alert variant="danger" title="Could not load release work">{error}</Alert>;
  }

  return (
    <div>
      <header className="page-header">
        <h1>Release work</h1>
        <p>Approved results awaiting release confirmation.</p>
      </header>

      <section className="page-section" aria-labelledby="release-items-heading">
        <h2 id="release-items-heading">Release items</h2>

        {data?.items.length === 0 ? (
          <p className="empty-state">No results are ready for release.</p>
        ) : (
          <ul className="stack" aria-label="Release items">
            {data?.items.map((item) => (
              <li key={item.release_id}>
                <Card interactive>
                  <Link className="work-item-link" to={item.route ?? `/release-work/${item.release_id}`}>
                    <CardHeader>
                      <CardTitle>{item.title}</CardTitle>
                    </CardHeader>
                    <CardBody>
                      <Badge variant="warning">{item.status_label}</Badge>
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
