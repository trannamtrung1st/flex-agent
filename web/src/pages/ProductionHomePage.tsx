import { Key, OperateArea } from "../design-system";
import { useProductionApi } from "../api/production-api";

export function ProductionHomePage() {
  const { shell } = useProductionApi();
  const activitiesAvailable = shell?.navigation.some((item) => item.destination_id === "activities" && item.is_available);
  const myWorkAvailable = shell?.navigation.some((item) => item.destination_id === "my-work" && item.is_available);

  return (
    <OperateArea
      className="workspace-area"
      label="Home"
      title="Home"
      description="Assessment Campaign setup and current Assignments use the production application session for this organization."
      empty={
        !activitiesAvailable && !myWorkAvailable
          ? {
              label: "No destinations available",
              note: "Activities and My work are not available for the current authorized relationship.",
            }
          : undefined
      }
    >
      {activitiesAvailable || myWorkAvailable ? (
        <div className="home-destinations">
          {activitiesAvailable ? (
            <Key variant="open" to="/activities">
              Open Activities
            </Key>
          ) : null}
          {myWorkAvailable ? (
            <Key variant="open" to="/my-work">
              Open My work
            </Key>
          ) : null}
          {!activitiesAvailable ? (
            <p className="home-unavailable">Activities are not available for the current authorized relationship.</p>
          ) : null}
          {!myWorkAvailable ? (
            <p className="home-unavailable">My work is not available for the current authorized relationship.</p>
          ) : null}
        </div>
      ) : null}
    </OperateArea>
  );
}
