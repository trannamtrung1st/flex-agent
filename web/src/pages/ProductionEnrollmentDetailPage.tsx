import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import { createProductionEnrollmentClient, type EnrollmentDetailV1 } from "../api/production-enrollment";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";

export function ProductionEnrollmentDetailPage() {
  const { activityId = "", cohortId = "", enrollmentId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const [detail, setDetail] = useState<EnrollmentDetailV1 | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    client.getEnrollment(activityId, cohortId, enrollmentId)
      .then((result) => {
        if (!cancelled) {
          setDetail(result);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setError("This Enrollment is not available.");
        }
      });
    return () => {
      cancelled = true;
    };
  }, [activityId, client, cohortId, enrollmentId]);

  if (error) {
    return (
      <StatusPanel title="Enrollment unavailable" variant="danger">
        <p>{error}</p>
        <p><Link to={`/activities/${activityId}/cohorts/${cohortId}/participants`}>Return to Assign Participants</Link></p>
      </StatusPanel>
    );
  }

  if (detail === null) {
    return <ProtectedLoading label="Loading Enrollment…" />;
  }

  return (
    <div>
      <header className="page-header">
        <h1>{detail.enrollment.display_label}</h1>
        <p>Status {detail.enrollment.status}. Revision {detail.enrollment.revision}.</p>
      </header>
      <section className="page-section">
        <h2>History</h2>
        <ol>
          {detail.history.map((item) => (
            <li key={item.sequence}>
              {item.prior_status} to {item.new_status} ({item.reason_code})
            </li>
          ))}
        </ol>
      </section>
      <p>
        <Link to={`/activities/${activityId}/cohorts/${cohortId}/participants`}>Return to Assign Participants</Link>
      </p>
    </div>
  );
}
