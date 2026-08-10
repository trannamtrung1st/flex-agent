import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { ResultDetailProjectionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { SafeContent } from "../components/ui/SafeContent";

export function ResultDetailPage() {
  const { resultId } = useParams<{ resultId: string }>();
  const { fetchJson } = useBrowserApi();
  const [detail, setDetail] = useState<ResultDetailProjectionV1 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!resultId) {
      return;
    }

    let active = true;

    void fetchJson<ResultDetailProjectionV1>(`/browser/results/${resultId}`)
      .then((projection) => {
        if (active) {
          setDetail(projection);
          setLoading(false);
        }
      })
      .catch((err: unknown) => {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load result");
          setLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [resultId, fetchJson]);

  if (loading) {
    return <ProtectedLoading label="Loading result…" />;
  }

  if (error || !detail) {
    return <Alert variant="danger" title="Could not load result">{error ?? "Result not found"}</Alert>;
  }

  const isReleased = detail.lifecycle_state === "released";

  return (
    <div>
      <header className="page-header">
        <h1>Result detail</h1>
        <p>
          <Badge variant={isReleased ? "success" : "default"}>{detail.status_label}</Badge>
        </p>
      </header>

      <section className="page-section" aria-labelledby="result-content-heading">
        <h2 id="result-content-heading">Content</h2>
        {isReleased && detail.content ? (
          <SafeContent>
            <p>{detail.content}</p>
          </SafeContent>
        ) : (
          <p className="empty-state">
            Result content is not yet available. You will see the released outcome here after
            authorized release.
          </p>
        )}
        {detail.correction_note ? <p>Correction note: {detail.correction_note}</p> : null}
      </section>

      <p className="page-section">
        <Link to="/results">Back to results</Link>
      </p>
    </div>
  );
}
