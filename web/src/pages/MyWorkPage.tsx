import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { AssignmentProjectionV1, PermittedActionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Card, CardBody, CardHeader, CardTitle } from "../components/ui/Card";
import { ErrorSummary } from "../components/ui/ErrorSummary";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { SafeContent } from "../components/ui/SafeContent";
import { createIdempotencyKey, mapActionToCommand } from "../utils/commands";

const SYNTHETIC_SESSION_ID = "sess.synthetic.001";

export function MyWorkPage() {
  const navigate = useNavigate();
  const { fetchJson, executeCommand } = useBrowserApi();
  const [assignment, setAssignment] = useState<AssignmentProjectionV1 | null>(null);
  const [submissionText, setSubmissionText] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);

  const loadAssignment = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const projection = await fetchJson<AssignmentProjectionV1>("/browser/my-work");
      setAssignment(projection);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load assignment");
    } finally {
      setLoading(false);
    }
  }, [fetchJson]);

  useEffect(() => {
    void loadAssignment();
  }, [loadAssignment]);

  const runAction = async (action: PermittedActionV1) => {
    if (!assignment) {
      return;
    }

    if (action.action_id === "open_session") {
      void navigate(`/sessions/${SYNTHETIC_SESSION_ID}`);
      return;
    }

    const commandType = mapActionToCommand(action.action_id);
    if (!commandType) {
      return;
    }

    setPending(true);
    setActionError(null);

    try {
      const payload =
        action.action_id === "submit_text"
          ? { submission_text: submissionText }
          : undefined;

      const result = await executeCommand({
        command_id: action.action_id,
        idempotency_key: createIdempotencyKey(),
        command_type: commandType,
        resource_id: assignment.enrollment_id,
        payload,
      });

      if (result.outcome !== "succeeded") {
        setActionError(result.safe_message ?? "Action could not be completed.");
      } else {
        await loadAssignment();
      }
    } catch (err: unknown) {
      setActionError(err instanceof Error ? err.message : "Action failed");
    } finally {
      setPending(false);
    }
  };

  if (loading) {
    return <ProtectedLoading label="Loading your assignment…" />;
  }

  if (error || !assignment) {
    return <Alert variant="danger" title="Could not load assignment">{error ?? "Assignment not found"}</Alert>;
  }

  const canSubmit = assignment.permitted_actions.some((action) => action.action_id === "submit_text");

  return (
    <div>
      <header className="page-header">
        <h1>My work</h1>
        <SafeContent>
          <p>{assignment.activity_title}</p>
        </SafeContent>
      </header>

      {actionError ? <ErrorSummary errors={[actionError]} /> : null}

      <Card>
        <CardHeader>
          <CardTitle>Assignment</CardTitle>
        </CardHeader>
        <CardBody className="stack">
          <p>{assignment.task_summary}</p>
          <div className="stack">
            <Badge variant="info">Attempt: {assignment.attempt_status}</Badge>
            {assignment.deadline ? <span>Deadline: {assignment.deadline}</span> : null}
            <span>Timezone: {assignment.timezone}</span>
          </div>
        </CardBody>
      </Card>

      {canSubmit ? (
        <div className="page-section field">
          <label className="field-label" htmlFor="submission-text">Submission text</label>
          <textarea
            id="submission-text"
            className="textarea"
            value={submissionText}
            onChange={(event) => { setSubmissionText(event.target.value); }}
            placeholder="Enter .txt or .md content for your submission"
            aria-describedby="submission-hint"
          />
          <p id="submission-hint" className="fg-muted" style={{ color: "var(--fg-muted)", fontSize: "0.875rem" }}>
            Provide permitted text material for evaluation.
          </p>
        </div>
      ) : null}

      {assignment.submission_versions.length > 0 ? (
        <section className="page-section" aria-labelledby="versions-heading">
          <h2 id="versions-heading">Submission versions</h2>
          <ul className="stack">
            {assignment.submission_versions.map((version) => (
              <li key={version.version_id}>
                <Card>
                  <CardBody>
                    <strong>{version.label}</strong> — <Badge variant="success">{version.status_label}</Badge>
                    {version.content_preview ? <p>{version.content_preview}</p> : null}
                  </CardBody>
                </Card>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {assignment.permitted_actions.length > 0 ? (
        <div className="action-row" role="group" aria-label="Assignment actions">
          {assignment.permitted_actions.map((action) => (
            <Button
              key={action.action_id}
              onClick={() => void runAction(action)}
              disabled={pending || (action.action_id === "submit_text" && !submissionText.trim())}
            >
              {action.label}
            </Button>
          ))}
        </div>
      ) : null}

      {assignment.attempt_status === "Active" ? (
        <p className="page-section">
          <Link to={`/sessions/${SYNTHETIC_SESSION_ID}`}>Open session</Link>
        </p>
      ) : null}
    </div>
  );
}
