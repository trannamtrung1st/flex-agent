import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { PermittedActionV1, ReviewCaseDetailProjectionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { ErrorSummary } from "../components/ui/ErrorSummary";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { SafeContent } from "../components/ui/SafeContent";
import { createIdempotencyKey, mapActionToCommand } from "../utils/commands";

export function ReviewCasePage() {
  const { caseId } = useParams<{ caseId: string }>();
  const { fetchJson, executeCommand } = useBrowserApi();
  const [detail, setDetail] = useState<ReviewCaseDetailProjectionV1 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);

  const loadDetail = useCallback(async () => {
    if (!caseId) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const projection = await fetchJson<ReviewCaseDetailProjectionV1>(
        `/browser/review-work/${caseId}`,
      );
      setDetail(projection);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load review case");
    } finally {
      setLoading(false);
    }
  }, [caseId, fetchJson]);

  useEffect(() => {
    void loadDetail();
  }, [loadDetail]);

  const runAction = async (action: PermittedActionV1) => {
    if (!detail) {
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
        resource_id: detail.case_id,
        expected_version: detail.expected_version,
      });

      if (result.outcome !== "succeeded") {
        setActionError(result.safe_message ?? "Decision could not be recorded.");
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
    return <ProtectedLoading label="Loading review case…" />;
  }

  if (error || !detail) {
    return <Alert variant="danger" title="Could not load review case">{error ?? "Case not found"}</Alert>;
  }

  return (
    <div>
      <header className="page-header">
        <h1>Review case</h1>
        <p>
          <Badge variant="warning">{detail.status_label}</Badge>
          · {detail.candidate_lineage}
        </p>
      </header>

      {actionError ? <ErrorSummary errors={[actionError]} /> : null}

      <section className="page-section" aria-labelledby="criteria-heading">
        <h2 id="criteria-heading">Criteria and evidence</h2>
        <div className="stack">
          {detail.criteria.map((criterion) => (
            <div key={criterion.criterion_id} className="criteria-block">
              <strong>{criterion.label}</strong>
              <Badge variant={criterion.outcome === "Met" ? "success" : "warning"}>
                {criterion.outcome}
              </Badge>
              <ul className="evidence-list">
                {criterion.evidence.map((item) => (
                  <li key={item.evidence_id}>
                    <SafeContent>
                      <span>{item.label}</span> — {item.locator_summary}
                      {item.content_preview ? <p>{item.content_preview}</p> : null}
                    </SafeContent>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </section>

      {detail.permitted_actions.length > 0 ? (
        <div className="action-row" role="group" aria-label="Review decisions">
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
        <Link to="/review-work">Back to review work</Link>
      </p>
    </div>
  );
}
