import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import { createProductionEnrollmentClient, type AssignmentSummaryV1 } from "../api/production-enrollment";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";

export function ProductionMyWorkPage() {
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const [items, setItems] = useState<AssignmentSummaryV1[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    client.listMyWork()
      .then((page) => {
        if (!cancelled) {
          setItems(page.items);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setError("My work is not available.");
        }
      });
    return () => {
      cancelled = true;
    };
  }, [client]);

  if (error) {
    return (
      <StatusPanel title="My work unavailable" variant="danger">
        <p>{error}</p>
      </StatusPanel>
    );
  }

  if (items === null) {
    return <ProtectedLoading label="Loading My work…" />;
  }

  return (
    <div>
      <header className="page-header">
        <h1>My work</h1>
        <p>Current Assignments for the signed-in Participant. Submission intake and Attempt start are not available yet.</p>
      </header>
      {items.length === 0 ? (
        <p>You have no current assignments.</p>
      ) : (
        <ul>
          {items.map((item) => (
            <li key={item.enrollment_id}>
              <p>{item.activity_title ?? "Assignment"} · {item.status}</p>
              {item.permitted_actions.includes("open_assignment") ? (
                <Link to={`/my-work/${item.enrollment_id}`}>Open assignment</Link>
              ) : (
                <Link to="/">Return to Home</Link>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
