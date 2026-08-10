import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useBrowserApi } from "../api/browser-api";
import type { EnrollmentProjectionV1, PermittedActionV1 } from "../api/browser-contracts";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Card, CardBody, CardHeader, CardTitle } from "../components/ui/Card";
import { DataTable } from "../components/ui/DataTable";
import { ErrorSummary } from "../components/ui/ErrorSummary";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { createIdempotencyKey, mapActionToCommand } from "../utils/commands";

export function EnrollmentPage() {
  const { activityId } = useParams<{ activityId: string }>();
  const { fetchJson, executeCommand } = useBrowserApi();
  const [enrollment, setEnrollment] = useState<EnrollmentProjectionV1 | null>(null);
  const [selectedParticipant, setSelectedParticipant] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState(false);

  const loadEnrollment = useCallback(async () => {
    if (!activityId) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const projection = await fetchJson<EnrollmentProjectionV1>(
        `/browser/activities/${activityId}/enrollment`,
      );
      setEnrollment(projection);
      if (projection.permitted_participants.length > 0) {
        setSelectedParticipant(projection.permitted_participants[0].participant_id);
      }
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load enrollment");
    } finally {
      setLoading(false);
    }
  }, [activityId, fetchJson]);

  useEffect(() => {
    void loadEnrollment();
  }, [loadEnrollment]);

  const runAction = async (action: PermittedActionV1) => {
    if (!enrollment) {
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
        resource_id: enrollment.activity_id,
        payload: selectedParticipant ? { participant_id: selectedParticipant } : undefined,
      });

      if (result.outcome !== "succeeded") {
        setActionError(result.safe_message ?? "Assignment could not be completed.");
      } else {
        await loadEnrollment();
      }
    } catch (err: unknown) {
      setActionError(err instanceof Error ? err.message : "Assignment failed");
    } finally {
      setPending(false);
    }
  };

  if (loading) {
    return <ProtectedLoading label="Loading enrollment…" />;
  }

  if (error || !enrollment) {
    return <Alert variant="danger" title="Could not load enrollment">{error ?? "Enrollment not found"}</Alert>;
  }

  return (
    <div>
      <header className="page-header">
        <h1>Enrollment</h1>
        <p>Assign participants to the activated cohort.</p>
      </header>

      {actionError ? <ErrorSummary errors={[actionError]} /> : null}

      <section className="page-section" aria-labelledby="enrollments-heading">
        <h2 id="enrollments-heading">Current enrollments</h2>
        <DataTable
          caption="Enrollment records"
          rows={enrollment.enrollments}
          getRowKey={(row) => row.enrollment_id}
          emptyMessage="No participants enrolled yet."
          columns={[
            { id: "participant", header: "Participant", cell: (row) => row.participant_label },
            { id: "status", header: "Status", cell: (row) => <Badge variant="success">{row.status_label}</Badge> },
          ]}
        />
      </section>

      {enrollment.permitted_participants.length > 0 ? (
        <Card className="page-section">
          <CardHeader>
            <CardTitle>Assign participant</CardTitle>
          </CardHeader>
          <CardBody>
            <div className="field">
              <label className="field-label" htmlFor="participant-select">Participant</label>
              <select
                id="participant-select"
                className="select"
                value={selectedParticipant}
                onChange={(event) => { setSelectedParticipant(event.target.value); }}
              >
                {enrollment.permitted_participants.map((participant) => (
                  <option key={participant.participant_id} value={participant.participant_id}>
                    {participant.display_label}
                  </option>
                ))}
              </select>
            </div>
          </CardBody>
        </Card>
      ) : null}

      {enrollment.permitted_actions.length > 0 ? (
        <div className="action-row" role="group" aria-label="Enrollment actions">
          {enrollment.permitted_actions.map((action) => (
            <Button
              key={action.action_id}
              onClick={() => void runAction(action)}
              disabled={pending}
            >
              {action.label}
            </Button>
          ))}
        </div>
      ) : null}

      <p className="page-section">
        <Link to={`/activities/${enrollment.activity_id}`}>Back to activity</Link>
      </p>
    </div>
  );
}
