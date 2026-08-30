import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import { createProductionAssessmentClient } from "../api/production-assessment";
import { AssessmentCampaignCreatePage } from "./AssessmentCampaignCreatePage";

export function ProductionCampaignCreatePage() {
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionAssessmentClient(fetchJson), [fetchJson]);
  const navigate = useNavigate();

  return (
    <AssessmentCampaignCreatePage
      loadActivities={client.listActivities}
      loadSourceOptions={client.listSourceOptions}
      createActivity={client.createActivity}
      onCreated={(activityId) => {
        void navigate(`/activities/${activityId}/setup`);
      }}
    />
  );
}
