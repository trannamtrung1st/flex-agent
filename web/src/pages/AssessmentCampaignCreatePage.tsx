import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useId, useMemo, useRef } from "react";
import { Controller, useForm, type Control, type UseFormWatch } from "react-hook-form";
import {
  isAssessmentAccessLoss,
  REQUIRED_SOURCE_CATEGORIES,
  resolveSelectedSources,
  sourceOptionIdentity,
  type ProductionActivityList,
  type ProductionSourceOption,
  type ProductionSourceRef,
} from "../api/production-assessment";
import { CeremonyArea, CeremonyUnavailable, CeremonyWait } from "../components/shell/SessionChrome";
import {
  BackKey,
  CAMPAIGN_TITLE_PLACEHOLDER,
  DropdownSelect,
  ErrorSummary,
  FieldInput,
  FormField,
  FormSection,
  Grid,
  Key,
  OperateArea,
  PlateFoot,
  Stack,
  StateReadout,
  type ErrorSummaryItem,
} from "../design-system";
import {
  createSourceEligibilityMode,
  inheritedSourceCategories,
  INTENT_SOURCE_CATEGORIES,
  LISTED_REVISIONS_DEVELOPMENT_NOTE,
  sourceCategoryLabel,
  sourceEligibilityLabel,
  sourceSelectOptionLabel,
} from "../features/assessment/campaignCreatePresentation";
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

export interface AssessmentCampaignCreatePageProps {
  loadActivities: (signal?: AbortSignal) => Promise<ProductionActivityList>;
  loadSourceOptions: (signal?: AbortSignal) => Promise<{ sources: ProductionSourceOption[] }>;
  createActivity: (title: string, sources: Partial<Record<string, ProductionSourceRef>>) => Promise<string>;
  onCreated: (activityId: string) => void;
}

function SourceField({
  category,
  titleId,
  intent,
  sources,
  control,
  watch,
  error,
  markDevelopment,
}: {
  category: (typeof REQUIRED_SOURCE_CATEGORIES)[number];
  titleId: string;
  intent?: boolean;
  sources: ProductionSourceOption[];
  control: Control<CampaignCreateValues>;
  watch: UseFormWatch<CampaignCreateValues>;
  error?: string;
  markDevelopment: boolean;
}) {
  const options = sources.filter((source) => source.category === category);
  const fieldId = `${titleId}-${category}`;
  const selectedValue = watch(`sources.${category}`);
  const selected = options.find((option) => sourceOptionIdentity(option) === selectedValue);
  const hasSelectedOption = Boolean(selected);
  const slack = Boolean(selectedValue && !hasSelectedOption);
  const showDevelopment = markDevelopment && selected && !selected.production_eligible;
  const dropdownOptions = [
    ...(options.length === 0 && !selectedValue ? [{ value: "", label: "Unavailable" }] : []),
    ...(slack ? [{ value: selectedValue, label: "No longer available" }] : []),
    ...options.map((option) => ({
      value: sourceOptionIdentity(option),
      label: sourceSelectOptionLabel(option.source_kind, option.version_id, intent ? "full" : "revision"),
    })),
  ];

  return (
    <Stack gap="2.5">
      <FormField
        id={fieldId}
        layout="stack"
        label={sourceCategoryLabel(category)}
        labelAssociatesControl={false}
        error={error}
        hint={slack ? "No longer available" : undefined}
      >
        {(fieldControl, { labelId }) => (
          <Controller
            control={control}
            name={`sources.${category}`}
            render={({ field }) => (
              <DropdownSelect
                id={fieldControl.id}
                labelId={labelId}
                describedBy={fieldControl["aria-describedby"]}
                value={field.value}
                options={dropdownOptions}
                onChange={field.onChange}
              />
            )}
          />
        )}
      </FormField>
      {showDevelopment ? (
        <StateReadout
          variant="rest"
          label={sourceEligibilityLabel(false)}
        />
      ) : null}
    </Stack>
  );
}

export function AssessmentCampaignCreatePage({
  loadActivities,
  loadSourceOptions,
  createActivity,
  onCreated,
}: AssessmentCampaignCreatePageProps) {
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
    shouldFocusError: false,
  });

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
  const createError = createMutation.error && !isAssessmentAccessLoss(createMutation.error)
    ? "The Campaign could not be created."
    : null;

  useEffect(() => {
    if (!createError) {
      return;
    }

    document.getElementById(summaryId)?.focus();
  }, [createError, summaryId]);

  useEffect(() => {
    if (form.formState.submitCount < 1) {
      return;
    }

    const fieldErrors = form.formState.errors;
    if (!fieldErrors.title && !fieldErrors.sources && !fieldErrors.root) {
      return;
    }

    document.getElementById(summaryId)?.focus();
  }, [form.formState.submitCount, form.formState.errors, summaryId]);

  if (loading) {
    return (
      <CeremonyArea
        label="Create assessment Campaign"
        title="Create assessment Campaign"
        description="Activity form: Campaign. Configured type: Assessment."
      >
        <CeremonyWait label="Loading create…" />
      </CeremonyArea>
    );
  }

  if (accessChanged) {
    return (
      <CeremonyUnavailable
        title="Your access changed"
        description="Protected setup values were removed. Return to Home or sign in again."
        note="Protected setup values were removed. Return to Home or sign in again."
        danger
        recovery={{ label: "Return to Home", to: "/" }}
      />
    );
  }

  if (!canCreate) {
    return (
      <CeremonyUnavailable
        title="Create is not available"
        description="This authorized relationship cannot start a new assessment Campaign."
        note="Create is not available for the current authorized relationship."
        danger
        recovery={{ label: "Return to Activities", to: "/activities" }}
      />
    );
  }

  const missingCategory = REQUIRED_SOURCE_CATEGORIES.find((category) => !sources.some((source) => source.category === category));
  const selectedSources = form.watch("sources");
  const eligibilityMode = createSourceEligibilityMode(
    REQUIRED_SOURCE_CATEGORIES.map((category) =>
      sources.find((source) => sourceOptionIdentity(source) === selectedSources[category]),
    ),
  );
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
      className="workspace-area work-plane record-plane record-plane--setup"
      frameClassName="record-frame"
      label="Create assessment Campaign"
      title="Create assessment Campaign"
      description="Activity form: Campaign. Configured type: Assessment."
      back={<BackKey to="/activities" label="Activities" />}
      empty={missingCategory ? {
        label: `No permitted ${sourceCategoryLabel(missingCategory)} revisions are available`,
        note: "A ready source set is required before a draft can be created.",
      } : undefined}
    >
      {missingCategory ? null : (
        <Stack
          as="form"
          gap="none"
          className="workspace-form setup-ceremony"
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
          <Stack gap="6" className="create-ceremony__scroll">
          {summaryErrors.length > 0 ? (
            <ErrorSummary title="Correct the following" headingId={summaryId} errors={summaryErrors} />
          ) : null}
          <FormField
            id={titleId}
            layout="stack"
            label="Campaign title"
            error={fieldErrors.title?.message}
          >
            {(fieldControl) => (
              <FieldInput
                {...fieldControl}
                {...form.register("title")}
                maxLength={200}
                width="wide"
                placeholder={CAMPAIGN_TITLE_PLACEHOLDER}
              />
            )}
          </FormField>
          {eligibilityMode === "plate" ? (
            <StateReadout
              variant="rest"
              label={LISTED_REVISIONS_DEVELOPMENT_NOTE}
            />
          ) : null}
          <FormSection legend="Agent and Harness">
            <Grid gap="4" minItemWidth="control">
              {INTENT_SOURCE_CATEGORIES.map((category) => (
                <SourceField
                  key={category}
                  category={category}
                  titleId={titleId}
                  intent
                  sources={sources}
                  control={form.control}
                  watch={form.watch}
                  error={fieldErrors.sources?.[category]?.message}
                  markDevelopment={eligibilityMode === "berth"}
                />
              ))}
            </Grid>
          </FormSection>
          <FormSection legend="Source set">
            <Grid gap="4" minItemWidth="compact">
              {inheritedSourceCategories().map((category) => (
                <SourceField
                  key={category}
                  category={category}
                  titleId={titleId}
                  sources={sources}
                  control={form.control}
                  watch={form.watch}
                  error={fieldErrors.sources?.[category]?.message}
                  markDevelopment={eligibilityMode === "berth"}
                />
              ))}
            </Grid>
          </FormSection>
          </Stack>
          <PlateFoot className="setup-ceremony__foot" arrangement="end">
            <Key type="submit" variant="transmit" size="large" disabled={createMutation.isPending} waiting={createMutation.isPending}>
              {createMutation.isPending ? "Creating…" : "Create"}
            </Key>
          </PlateFoot>
        </Stack>
      )}
    </OperateArea>
  );
}
