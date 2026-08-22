import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import { createProductionEnrollmentClient, type AssignmentSummaryV1 } from "../api/production-enrollment";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";

export function ProductionMyWorkDetailPage() {
  const { enrollmentId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const [assignment, setAssignment] = useState<AssignmentSummaryV1 | null>(null);
  const [unavailable, setUnavailable] = useState(false);

  useEffect(() => {
    let cancelled = false;
    client.getMyWork(enrollmentId)
      .then((result) => {
        if (!cancelled) {
          setAssignment(result.assignment);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setUnavailable(true);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [client, enrollmentId]);

  if (unavailable) {
    return (
      <StatusPanel title="Assignment unavailable" variant="danger">
        <p>This assignment is not available. Return to My work or contact the provided support route.</p>
        <p><Link to="/my-work">Return to My work</Link></p>
      </StatusPanel>
    );
  }

  if (assignment === null) {
    return <ProtectedLoading label="Loading assignment…" />;
  }

  return (
    <div>
      <header className="page-header">
        <h1>{assignment.activity_title ?? "Assignment"}</h1>
        <p>Current state: {assignment.status}.</p>
      </header>
      {assignment.summary_available ? (
        <section className="page-section">
          <h2>Task and timing</h2>
          <p>{assignment.task_title}</p>
          <p>Timezone {assignment.time_zone_id}. Deadline {assignment.deadline_utc}.</p>
        </section>
      ) : (
        <p>The assignment is visible, but the Task summary is currently unavailable.</p>
      )}
      {assignment.status === "suspended" ? (
        <p>This assignment is suspended. New submission or attempt actions are not available.</p>
      ) : null}
      <p><Link to="/my-work">Return to My work</Link></p>
    </div>
  );
}
