import { useMemo } from "react";
import { useProductionApi } from "../api/production-api";
import { createProductionAssessmentClient } from "../api/production-assessment";
import { AssessmentActivitiesPage } from "./AssessmentActivitiesPage";

export function ProductionActivitiesPage() {
  const { fetchJson, shell } = useProductionApi();
  const client = useMemo(() => createProductionAssessmentClient(fetchJson), [fetchJson]);

  return (
    <AssessmentActivitiesPage
      organizationId={shell?.organization_id}
      loadActivities={client.listActivities}
      loadSourceOptions={client.listSourceOptions}
    />
  );
}
