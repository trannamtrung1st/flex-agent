import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { ActivityDetailProjectionV1, PermittedActionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { ErrorSummary } from "../components/ui/ErrorSummary";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { createIdempotencyKey, mapActionToCommand } from "../utils/commands";

export function ActivityDetailPage() {
  const { activityId } = useParams<{ activityId: string }>();
  const navigate = useNavigate();
  const { fetchJson, executeCommand } = useBrowserApi();
  const [detail, setDetail] = useState<ActivityDetailProjectionV1 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);

  const loadDetail = useCallback(async () => {
    if (!activityId) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const projection = await fetchJson<ActivityDetailProjectionV1>(
        `/browser/activities/${activityId}`,
      );
      setDetail(projection);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load activity");
    } finally {
      setLoading(false);
    }
  }, [activityId, fetchJson]);

  useEffect(() => {
    void loadDetail();
  }, [loadDetail]);

  const runAction = async (action: PermittedActionV1) => {
    if (!detail) {
      return;
    }

    if (action.action_id === "assign_participants") {
      void navigate(`/activities/${detail.activity_id}/enrollment`);
      return;
    }

    const commandType = mapActionToCommand(action.action_id);
    if (!commandType) {
      return;
    }

    setPending(true);
    setActionError(null);

    try {
      const result = await executeCommand({
        command_id: action.action_id,
        idempotency_key: createIdempotencyKey(),
        command_type: commandType,
        resource_id: detail.activity_id,
        expected_version: detail.expected_version,
      });

      if (result.outcome !== "succeeded") {
        setActionError(result.safe_message ?? "Action could not be completed.");
      } else {
        await loadDetail();
      }
    } catch (err: unknown) {
      setActionError(err instanceof Error ? err.message : "Action failed");
    } finally {
      setPending(false);
    }
  };

  if (loading) {
    return <ProtectedLoading label="Loading activity detail…" />;
  }

  if (error || !detail) {
    return <Alert variant="danger" title="Could not load activity">{error ?? "Activity not found"}</Alert>;
  }

  return (
    <div>
      <header className="page-header">
        <h1>{detail.title}</h1>
        <p>
          {detail.form} · {detail.type} · <Badge variant="info">{detail.lifecycle_state}</Badge>
        </p>
        {detail.baseline_summary ? <p>{detail.baseline_summary}</p> : null}
      </header>

      {actionError ? <ErrorSummary errors={[actionError]} /> : null}

      <section className="page-section" aria-labelledby="readiness-heading">
        <h2 id="readiness-heading">Readiness</h2>
        <div className="readiness-grid">
          {detail.readiness_categories.map((category) => (
            <div key={category.category_id} className="readiness-row">
              <span>{category.label}</span>
              <Badge variant={category.is_blocking ? "warning" : "success"}>{category.status}</Badge>
            </div>
          ))}
        </div>
      </section>

      {detail.permitted_actions.length > 0 ? (
        <div className="action-row" role="group" aria-label="Activity actions">
          {detail.permitted_actions.map((action) => (
            <Button
              key={action.action_id}
              variant={action.is_destructive ? "danger" : "primary"}
              onClick={() => void runAction(action)}
              disabled={pending}
            >
              {action.label}
            </Button>
          ))}
        </div>
      ) : null}

      <p className="page-section">
        <Link to="/activities">Back to activities</Link>
      </p>
    </div>
  );
}
