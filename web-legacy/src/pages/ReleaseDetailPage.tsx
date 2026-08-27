import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { ReleaseDetailProjectionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Dialog } from "../components/ui/Dialog";
import { ErrorSummary } from "../components/ui/ErrorSummary";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { SafeContent } from "../components/ui/SafeContent";
import { createIdempotencyKey, mapActionToCommand } from "../utils/commands";

export function ReleaseDetailPage() {
  const { releaseId } = useParams<{ releaseId: string }>();
  const { fetchJson, executeCommand } = useBrowserApi();
  const [detail, setDetail] = useState<ReleaseDetailProjectionV1 | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);

  const loadDetail = useCallback(async () => {
    if (!releaseId) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const projection = await fetchJson<ReleaseDetailProjectionV1>(
        `/browser/release-work/${releaseId}`,
      );
      setDetail(projection);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load release detail");
    } finally {
      setLoading(false);
    }
  }, [releaseId, fetchJson]);

  useEffect(() => {
    void loadDetail();
  }, [loadDetail]);

  const confirmRelease = async () => {
    if (!detail) {
      return;
    }

    const action = detail.permitted_actions.find((item) => item.action_id === "release_result");
    if (!action) {
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
        resource_id: detail.release_id,
        expected_version: detail.expected_version,
      });

      if (result.outcome !== "succeeded") {
        setActionError(result.safe_message ?? "Release could not be completed.");
      } else {
        setDialogOpen(false);
        await loadDetail();
      }
    } catch (err: unknown) {
      setActionError(err instanceof Error ? err.message : "Release failed");
    } finally {
      setPending(false);
    }
  };

  if (loading) {
    return <ProtectedLoading label="Loading release detail…" />;
  }

  if (error || !detail) {
    return <Alert variant="danger" title="Could not load release">{error ?? "Release not found"}</Alert>;
  }

  const canRelease = detail.permitted_actions.some((action) => action.action_id === "release_result");

  return (
    <div>
      <header className="page-header">
        <h1>Release detail</h1>
        <p>
          <Badge variant={detail.lifecycle_state === "released" ? "success" : "warning"}>
            {detail.status_label}
          </Badge>
        </p>
      </header>

      {actionError ? <ErrorSummary errors={[actionError]} /> : null}

      <section className="page-section" aria-labelledby="preview-heading">
        <h2 id="preview-heading">Participant preview</h2>
        <SafeContent>
          <p>{detail.result_preview}</p>
        </SafeContent>
        <p>Audience: {detail.audience_policy}</p>
      </section>

      {canRelease ? (
        <div className="action-row">
          <Button onClick={() => { setDialogOpen(true); }} disabled={pending}>
            Release Result
          </Button>
        </div>
      ) : null}

      <Dialog
        open={dialogOpen}
        title="Confirm release"
        confirmLabel="Release Result"
        confirmVariant="primary"
        onConfirm={() => void confirmRelease()}
        onCancel={() => { setDialogOpen(false); }}
        isConfirming={pending}
      >
        <p>
          This action makes the Result visible to authorized Participants. It cannot be undone
          from this surface.
        </p>
      </Dialog>

      <p className="page-section">
        <Link to="/release-work">Back to release work</Link>
      </p>
    </div>
  );
}
