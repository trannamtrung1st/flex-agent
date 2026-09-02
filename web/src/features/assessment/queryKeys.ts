import { canonicalizeActivityListQuery, type NumberedActivityListQuery } from "../../api/production-assessment";

export const assessmentKeys = {
  all: ["assessment"] as const,
  v1: ["assessment", "v1"] as const,
  activitiesRoot: () => ["assessment", "v1", "activities", "list"] as const,
  activities: (query?: NumberedActivityListQuery) =>
    query
      ? (["assessment", "v1", "activities", "list", canonicalizeActivityListQuery(query)] as const)
      : (["assessment", "v1", "activities", "list"] as const),
  sourceOptions: () => ["assessment", "v1", "activities", "source-options"] as const,
  activity: (activityId: string) => ["assessment", "v1", "activities", "detail", activityId] as const,
};
