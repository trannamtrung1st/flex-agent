import { Link } from "react-router-dom";
import { useProductionApi } from "../api/production-api";

export function ProductionHomePage() {
  const { shell } = useProductionApi();
  const activitiesAvailable = shell?.navigation.some((item) => item.destination_id === "activities" && item.is_available);

  return (
    <div>
      <header className="page-header">
        <h1>Home</h1>
        <p>Assessment Campaign setup uses the production application session for this organization.</p>
      </header>
      {activitiesAvailable ? (
        <p>
          <Link to="/activities">Open Activities</Link>
        </p>
      ) : (
        <p>Activities are not available for the current authorized relationship.</p>
      )}
    </div>
  );
}
