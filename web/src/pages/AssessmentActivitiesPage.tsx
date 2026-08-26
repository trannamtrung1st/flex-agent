import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useId, useMemo, useRef } from "react";
import { useForm } from "react-hook-form";
import { Link } from "react-router-dom";
import {
  isAssessmentAccessLoss,
  REQUIRED_SOURCE_CATEGORIES,
  resolveSelectedSources,
  sourceOptionIdentity,
  sourceOptionLabel,
  type ProductionActivityList,
  type ProductionSourceOption,
  type ProductionSourceRef,
} from "../api/production-assessment";
import { Alert } from "../components/ui/Alert";
import { Badge } from "../components/ui/Badge";
import { Button } from "../components/ui/Button";
import { Card, CardBody, CardHeader, CardTitle } from "../components/ui/Card";
import { ErrorSummary, type ErrorSummaryItem } from "../components/ui/ErrorSummary";
import { ProtectedLoading } from "../components/ui/ProtectedLoading";
import { StatusPanel } from "../components/ui/StatusPanel";
import {
  campaignCreateSchema,
  emptyCampaignCreateValues,
  type CampaignCreateValues,
} from "../features/assessment/campaignCreateSchema";
import {
  useAssessmentActivitiesQuery,
  useAssessmentSourceOptionsQuery,
  useCreateAssessmentActivityMutation,
} from "../features/assessment/queries";
import { assessmentKeys } from "../features/assessment/queryKeys";

export interface AssessmentActivitiesPageProps {
  organizationId?: string;
  loadActivities: (signal?: AbortSignal) => Promise<ProductionActivityList>;
  loadSourceOptions: (signal?: AbortSignal) => Promise<{ sources: ProductionSourceOption[] }>;
  createActivity: (title: string, sources: Partial<Record<string, ProductionSourceRef>>) => Promise<string>;
  onCreated: (activityId: string) => void;
}

export function AssessmentActivitiesPage({
  organizationId,
  loadActivities,
  loadSourceOptions,
  createActivity,
  onCreated,
}: AssessmentActivitiesPageProps) {
  const titleId = useId();
  const summaryId = `${titleId}-summary`;
  const queryClient = useQueryClient();
  const sourcesInitialized = useRef(false);
  const activitiesQuery = useAssessmentActivitiesQuery(loadActivities);
  const canCreate = activitiesQuery.data?.permitted_actions.includes("create_assessment") ?? false;
  const sourcesQuery = useAssessmentSourceOptionsQuery(loadSourceOptions, activitiesQuery.isSuccess && canCreate);
  const createMutation = useCreateAssessmentActivityMutation(createActivity, onCreated);
  const sources = useMemo(() => sourcesQuery.data?.sources ?? [], [sourcesQuery.data?.sources]);
  const form = useForm<CampaignCreateValues>({
    resolver: zodResolver(campaignCreateSchema),
    defaultValues: emptyCampaignCreateValues,
  });
  // RHF watch is the supported subscription for keeping stale selected identities visible.
  const sourceValues = form.watch("sources");

  useEffect(() => {
    if (!sourcesQuery.isSuccess || sourcesInitialized.current) {
      return;
    }

    for (const category of REQUIRED_SOURCE_CATEGORIES) {
      const first = sources.find((source) => source.category === category);
      form.setValue(`sources.${category}`, first ? sourceOptionIdentity(first) : "", {
        shouldDirty: false,
        shouldTouch: false,
        shouldValidate: false,
      });
    }

    sourcesInitialized.current = true;
  }, [form, sources, sourcesQuery.isSuccess]);

  const loading = !activitiesQuery.isFetched || (canCreate && !sourcesQuery.isFetched);
  const accessChanged = isAssessmentAccessLoss(activitiesQuery.error)
    || isAssessmentAccessLoss(sourcesQuery.error)
    || isAssessmentAccessLoss(createMutation.error);
  const loadError = activitiesQuery.error instanceof Error && !isAssessmentAccessLoss(activitiesQuery.error)
    ? activitiesQuery.error.message
    : null;
  const createError = createMutation.error && !isAssessmentAccessLoss(createMutation.error)
    ? "The Campaign could not be created."
    : null;

  useEffect(() => {
    if (!createError) {
      return;
    }

    document.getElementById(summaryId)?.focus();
  }, [createError, summaryId]);

  if (loading) {
    return <ProtectedLoading label="Loading activities…" />;
  }

  if (accessChanged) {
    return (
      <StatusPanel title="Your access changed" variant="danger">
        <p>Protected setup values were removed. Return to Home or sign in again.</p>
      </StatusPanel>
    );
  }

  if (loadError && !activitiesQuery.data) {
    return <Alert variant="danger" title="Could not load activities">{loadError}</Alert>;
  }

  const data = activitiesQuery.data;
  const missingCategory = REQUIRED_SOURCE_CATEGORIES.find((category) => !sources.some((source) => source.category === category));
  const fieldErrors = form.formState.errors;
  const summaryErrors: ErrorSummaryItem[] = [
    ...(fieldErrors.title?.message
      ? [{ message: fieldErrors.title.message, href: `#${titleId}` }]
      : []),
    ...REQUIRED_SOURCE_CATEGORIES.flatMap((category) => {
      const message = fieldErrors.sources?.[category]?.message;
      return message ? [{ message, href: `#${titleId}-${category}` }] : [];
    }),
    ...(fieldErrors.root?.message ? [{ message: fieldErrors.root.message }] : []),
    ...(createError ? [createError] : []),
  ];

  return (
    <div>
      <header className="page-header">
        <h1>Activities</h1>
        <p>Create and resume Assessment Campaign drafts{organizationId ? ` for organization ${organizationId}` : ""}.</p>
      </header>

      {canCreate ? (
        <section className="page-section" aria-labelledby="create-heading">
          <h2 id="create-heading">Create assessment Campaign</h2>
          {missingCategory ? (
            <Alert variant="info" title={`No permitted ${missingCategory.replaceAll("_", " ")} revisions are available`}>
              A ready source set is required before a draft can be created.
            </Alert>
          ) : (
            <form
              className="stack"
              onSubmit={(event) => {
                void form.handleSubmit((values) => {
                if (createMutation.isPending) {
                  return;
                }

                const latestSources = queryClient.getQueryData<{ sources: ProductionSourceOption[] }>(
                  assessmentKeys.sourceOptions(),
                )?.sources ?? sources;
                const chosen = resolveSelectedSources(latestSources, values.sources, REQUIRED_SOURCE_CATEGORIES);
                if (Object.keys(chosen).length !== REQUIRED_SOURCE_CATEGORIES.length) {
                  form.setError("root", {
                    type: "manual",
                    message: "Selected sources are no longer available. Choose current options.",
                  });
                  requestAnimationFrame(() => {
                    document.getElementById(summaryId)?.focus();
                  });
                  return;
                }

                createMutation.mutate({ title: values.title, sources: chosen });
              }, () => {
                requestAnimationFrame(() => {
                  document.getElementById(summaryId)?.focus();
                });
              })(event);
              }}
            >
              {summaryErrors.length > 0 ? (
                <ErrorSummary title="Correct the following" headingId={summaryId} errors={summaryErrors} />
              ) : null}
              <div className="field">
                <label htmlFor={titleId}>Campaign title</label>
                <input
                  id={titleId}
                  maxLength={200}
                  aria-invalid={Boolean(fieldErrors.title)}
                  aria-describedby={fieldErrors.title ? `${titleId}-error` : undefined}
                  {...form.register("title")}
                />
                {fieldErrors.title?.message ? (
                  <p id={`${titleId}-error`} className="field-error">{fieldErrors.title.message}</p>
                ) : null}
              </div>
              {REQUIRED_SOURCE_CATEGORIES.map((category) => {
                const options = sources.filter((source) => source.category === category);
                const fieldId = `${titleId}-${category}`;
                const errorId = `${fieldId}-error`;
                const message = fieldErrors.sources?.[category]?.message;
                const field = form.register(`sources.${category}`);
                const selectedValue = sourceValues[category];
                const hasSelectedOption = options.some((option) => sourceOptionIdentity(option) === selectedValue);
                return (
                  <div key={category} className="field">
                    <label className="field-label" htmlFor={fieldId}>{category.replaceAll("_", " ")}</label>
                    <select
                      id={fieldId}
                      aria-invalid={Boolean(message)}
                      aria-describedby={message ? errorId : undefined}
                      {...field}
                      value={selectedValue}
                    >
                      {options.length === 0 && !selectedValue ? <option value="">Unavailable</option> : null}
                      {selectedValue && !hasSelectedOption ? <option value={selectedValue}>No longer available</option> : null}
                      {options.map((option) => (
                        <option key={sourceOptionIdentity(option)} value={sourceOptionIdentity(option)}>
                          {sourceOptionLabel(option)}
                        </option>
                      ))}
                    </select>
                    {message ? <p id={errorId} className="field-error">{message}</p> : null}
                  </div>
                );
              })}
              <Button type="submit" disabled={createMutation.isPending || Boolean(missingCategory)}>
                {createMutation.isPending ? "Creating…" : "Create assessment Campaign"}
              </Button>
            </form>
          )}
        </section>
      ) : null}

      <section className="page-section" aria-labelledby="activities-list-heading">
        <h2 id="activities-list-heading">Activity list</h2>
        {data?.activities.length === 0 ? (
          <p className="empty-state">No activities are available.</p>
        ) : (
          <ul className="stack" aria-label="Activities">
            {data?.activities.map((activity) => (
              <li key={activity.activity_id}>
                <Card interactive>
                  <Link className="work-item-link" to={`/activities/${activity.activity_id}/setup`}>
                    <CardHeader>
                      <CardTitle>{activity.title}</CardTitle>
                    </CardHeader>
                    <CardBody>
                      <Badge variant={activity.has_activated_cohort ? "info" : "default"}>
                        {activity.has_activated_cohort ? "Activated" : "Draft"}
                      </Badge>
                    </CardBody>
                  </Link>
                </Card>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
