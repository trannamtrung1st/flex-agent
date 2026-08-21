import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import { createProductionAssessmentClient } from "../api/production-assessment";
import { AssessmentActivitiesPage } from "./AssessmentActivitiesPage";

export function ProductionActivitiesPage() {
  const { fetchJson, shell } = useProductionApi();
  const client = useMemo(() => createProductionAssessmentClient(fetchJson), [fetchJson]);
  const navigate = useNavigate();

  return (
    <AssessmentActivitiesPage
      organizationId={shell?.organization_id}
      loadActivities={client.listActivities}
      loadSourceOptions={client.listSourceOptions}
      createActivity={client.createActivity}
      onCreated={(activityId) => {
        void navigate(`/activities/${activityId}/setup`);
      }}
    />
  );
}
