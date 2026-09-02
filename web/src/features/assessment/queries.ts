import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  canonicalizeActivityListQuery,
  DEFAULT_ACTIVITY_LIST_QUERY,
  type NumberedActivityListQuery,
  type ProductionActivityList,
  type ProductionSourceOption,
  type ProductionSourceRef,
} from "../../api/production-assessment";
import { assessmentKeys } from "./queryKeys";

export function useAssessmentActivitiesQuery(
  listActivities: (query: NumberedActivityListQuery, signal?: AbortSignal) => Promise<ProductionActivityList>,
  query: NumberedActivityListQuery = DEFAULT_ACTIVITY_LIST_QUERY,
) {
  const canonical = canonicalizeActivityListQuery(query);
  return useQuery({
    queryKey: assessmentKeys.activities(canonical),
    queryFn: ({ signal }) => listActivities(canonical, signal),
    placeholderData: (previous) => previous,
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
  queryKey: assessmentKeys.activitiesRoot(),
  exact: false,
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
