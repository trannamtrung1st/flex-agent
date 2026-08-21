import { useMemo } from "react";
import { useProductionApi } from "../api/production-api";
import { createProductionAssessmentClient } from "../api/production-assessment";
import { AssessmentSetupPage } from "./AssessmentSetupPage";

export function ProductionAssessmentSetupRoute() {
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionAssessmentClient(fetchJson), [fetchJson]);

  return (
    <AssessmentSetupPage
      loadSetup={client.loadSetup}
      saveDraft={client.saveDraft}
      checkReadiness={client.checkReadiness}
      activateCohort={(activityId, view) => client.activateCohort(activityId, view)}
    />
  );
}
