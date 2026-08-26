import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { ProductionActivityList, ProductionSourceOption, ProductionSourceRef } from "../../api/production-assessment";
import { assessmentKeys } from "./queryKeys";

export function useAssessmentActivitiesQuery(
  listActivities: (signal?: AbortSignal) => Promise<ProductionActivityList>,
) {
  return useQuery({
    queryKey: assessmentKeys.activities(),
    queryFn: ({ signal }) => listActivities(signal),
  });
}

export function useAssessmentSourceOptionsQuery(
  loadSourceOptions: (signal?: AbortSignal) => Promise<{ sources: ProductionSourceOption[] }>,
  enabled: boolean,
) {
  return useQuery({
    queryKey: assessmentKeys.sourceOptions(),
    queryFn: ({ signal }) => loadSourceOptions(signal),
    enabled,
  });
}

export const activitiesListInvalidation = {
  queryKey: assessmentKeys.activities(),
  exact: true,
  refetchType: "none" as const,
};

export function useCreateAssessmentActivityMutation(
  createActivity: (title: string, sources: Partial<Record<string, ProductionSourceRef>>) => Promise<string>,
  onCreated: (activityId: string) => void,
) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      title,
      sources,
    }: {
      title: string;
      sources: Partial<Record<string, ProductionSourceRef>>;
    }) => createActivity(title, sources),
    onSuccess: (activityId) => {
      void queryClient.invalidateQueries(activitiesListInvalidation);
      onCreated(activityId);
    },
  });
}
