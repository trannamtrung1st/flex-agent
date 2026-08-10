import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { HomeProjectionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Card, CardBody, CardHeader, CardTitle } from "../components/ui/Card";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { SafeContent } from "../components/ui/SafeContent";

export function HomePage() {
  const { fetchJson } = useBrowserApi();
  const [home, setHome] = useState<HomeProjectionV1 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;

    void fetchJson<HomeProjectionV1>("/browser/home")
      .then((data) => {
        if (active) {
          setHome(data);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load home");
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [fetchJson]);

  if (loading) {
    return <ProtectedLoading label="Loading your work queue…" />;
  }

  if (error) {
    return <Alert variant="danger" title="Could not load home">{error}</Alert>;
  }

  return (
    <div>
      <header className="page-header">
        <h1>Home</h1>
        <SafeContent>
          <p>{home?.greeting}</p>
        </SafeContent>
      </header>

      <section className="page-section" aria-labelledby="work-items-heading">
        <h2 id="work-items-heading">Your work</h2>

        {home?.work_items.length === 0 ? (
          <p className="empty-state">No work items require attention right now.</p>
        ) : (
          <ul className="stack" aria-label="Work items">
            {home?.work_items.map((item) => (
              <li key={item.item_id}>
                <Card interactive>
                  {item.route ? (
                    <Link className="work-item-link" to={item.route}>
                      <CardHeader>
                        <CardTitle>{item.title}</CardTitle>
                      </CardHeader>
                      <CardBody>
                        <div className="stack">
                          <Badge variant="brand">{item.status_label}</Badge>
                          {item.next_action_label ? (
                            <span>{item.next_action_label}</span>
                          ) : null}
                        </div>
                      </CardBody>
                    </Link>
                  ) : (
                    <>
                      <CardHeader>
                        <CardTitle>{item.title}</CardTitle>
                      </CardHeader>
                      <CardBody>
                        <Badge variant="brand">{item.status_label}</Badge>
                      </CardBody>
                    </>
                  )}
                </Card>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
