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
import { Alert, ErrorSummary, WaitPanel, type ErrorSummaryItem, Container, EmptyPlate, Inline, Key, OperateArea, Stack, StateReadout } from "../design-system";
import { FieldInput } from "../design-system/components/fields/FieldControls";
import { FormField } from "../design-system/components/fields/FormField";
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
  const canCreate = activitiesQuery.isFetchedAfterMount
    && activitiesQuery.isSuccess
    && activitiesQuery.data.permitted_actions.includes("create_assessment");
  const sourcesQuery = useAssessmentSourceOptionsQuery(loadSourceOptions, canCreate);
  const createMutation = useCreateAssessmentActivityMutation(createActivity, onCreated);
  const sources = useMemo(() => sourcesQuery.data?.sources ?? [], [sourcesQuery.data?.sources]);
  const form = useForm<CampaignCreateValues>({
    resolver: zodResolver(campaignCreateSchema),
    defaultValues: emptyCampaignCreateValues,
  });
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

  const loading = !activitiesQuery.isFetchedAfterMount || (canCreate && !sourcesQuery.isFetched);
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
    return (
      <OperateArea
        className="workspace-area"
        label="Activities"
        title="Activities"
        description="Create and resume Assessment Campaign drafts."
      >
        <WaitPanel label="Loading activities…" />
      </OperateArea>
    );
  }

  if (accessChanged) {
    return (
      <OperateArea
        className="workspace-area workspace-area--danger"
        label="Your access changed"
        title="Your access changed"
        description="Protected setup values were removed. Return to Home or sign in again."
      >
        <Key variant="open" to="/">Return to Home</Key>
      </OperateArea>
    );
  }

  if (loadError && !activitiesQuery.data) {
    return (
      <OperateArea
        className="workspace-area workspace-area--danger"
        label="Activities"
        title="Activities"
        description="Create and resume Assessment Campaign drafts."
      >
        <Alert variant="danger" title="Could not load activities">{loadError}</Alert>
      </OperateArea>
    );
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
    <OperateArea
      className="workspace-area"
      label="Activities"
      title="Activities"
      description="Create and resume Assessment Campaign drafts."
    >
      <Stack gap="none">
      {canCreate ? (
        <Stack as="section" className="workspace-section" gap="4" aria-labelledby="create-heading">
          <h2 id="create-heading">Create assessment Campaign</h2>
          {missingCategory ? (
            <Alert variant="info" title={`No permitted ${missingCategory.replaceAll("_", " ")} revisions are available`}>
              A ready source set is required before a draft can be created.
            </Alert>
          ) : (
            <Container size="form">
            <Stack
              as="form"
              gap="5"
              className="workspace-form"
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
              <FormField
                id={titleId}
                layout="stack"
                label="Campaign title"
                error={fieldErrors.title?.message}
              >
                {(control) => (
                  <FieldInput
                    {...control}
                    maxLength={200}
                    width="wide"
                    {...form.register("title")}
                  />
                )}
              </FormField>
              <Stack as="fieldset" gap="5" className="workspace-source-set">
                <legend>Sources</legend>
                {REQUIRED_SOURCE_CATEGORIES.map((category) => {
                  const options = sources.filter((source) => source.category === category);
                  const fieldId = `${titleId}-${category}`;
                  const message = fieldErrors.sources?.[category]?.message;
                  const field = form.register(`sources.${category}`);
                  const selectedValue = sourceValues[category];
                  const hasSelectedOption = options.some((option) => sourceOptionIdentity(option) === selectedValue);
                  return (
                    <FormField
                      key={category}
                      id={fieldId}
                      layout="stack"
                      label={category.replaceAll("_", " ")}
                      error={message}
                    >
                      {(control) => (
                        <select
                          className={message ? "field-input field-input--wide is-invalid" : "field-input field-input--wide"}
                          {...control}
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
                      )}
                    </FormField>
                  );
                })}
              </Stack>
              <Key type="submit" variant="transmit" disabled={createMutation.isPending || Boolean(missingCategory)} waiting={createMutation.isPending}>
                {createMutation.isPending ? "Creating…" : "Create assessment Campaign"}
              </Key>
            </Stack>
            </Container>
          )}
        </Stack>
      ) : null}

      <Stack as="section" className="workspace-section" gap="4" aria-labelledby="activities-list-heading">
        <h2 id="activities-list-heading">Activity list</h2>
        {data?.activities.length === 0 ? (
          <EmptyPlate
            className="empty-plate--inset"
            label="No activities"
            note="No activities are available."
          />
        ) : (
          <Stack as="ul" gap="none" className="activity-list" aria-label="Activities">
            {data?.activities.map((activity) => (
              <li key={activity.activity_id}>
                <Inline
                  as={Link}
                  className="activity-link"
                  to={`/activities/${activity.activity_id}/setup`}
                  justify="between"
                  wrap={false}
                  gap="4"
                >
                  <span>{activity.title}</span>
                  <StateReadout
                    variant={activity.has_activated_cohort ? "sealed" : "rest"}
                    solid={activity.has_activated_cohort}
                    label={activity.has_activated_cohort ? "Activated" : "Draft"}
                    className="state-cell"
                    labelClassName="state-label"
                  />
                </Inline>
              </li>
            ))}
          </Stack>
        )}
      </Stack>
      </Stack>
    </OperateArea>
  );
}
