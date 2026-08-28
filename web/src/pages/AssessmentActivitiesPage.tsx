import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useId, useMemo, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { Link } from "react-router-dom";
import {
  isAssessmentAccessLoss,
  REQUIRED_SOURCE_CATEGORIES,
  resolveSelectedSources,
  sourceOptionIdentity,
  sourceOptionLabel,
  type ProductionActivityList,
  type ProductionActivitySummary,
  type ProductionSourceOption,
  type ProductionSourceRef,
} from "../api/production-assessment";
import { CeremonyArea, CeremonyEmpty } from "../components/shell/SessionChrome";
import {
  Alert,
  Container,
  DataTablePagination,
  DataTableShell,
  DataTableToolbar,
  EmptyPlate,
  ErrorSummary,
  EtchedFrame,
  Key,
  OperateArea,
  SortableHeader,
  Stack,
  StateReadout,
  ToolbarReadout,
  ToolbarSearch,
  WaitPanel,
  WorkWell,
  WorkWellHead,
  type ErrorSummaryItem,
  useTableController,
} from "../design-system";
import { cx } from "../lib/cx";
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

type ActivitySortKey = "title" | "activation";

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
  const [createRevealed, setCreateRevealed] = useState(false);
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

  useEffect(() => {
    if (!createRevealed) {
      return;
    }

    document.getElementById("create-heading")?.focus();
  }, [createRevealed]);

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
        label="Activities"
        title="Activities"
        description="Create and resume Assessment Campaign drafts."
      >
        <WaitPanel label="Loading activities…" />
      </CeremonyArea>
    );
  }

  if (accessChanged) {
    return (
      <CeremonyArea
        label="Your access changed"
        title="Your access changed"
        description="Protected setup values were removed. Return to Home or sign in again."
        danger
      >
        <CeremonyEmpty note="Protected setup values were removed. Return to Home or sign in again.">
          <Key variant="open" to="/">Return to Home</Key>
        </CeremonyEmpty>
      </CeremonyArea>
    );
  }

  if (loadError && !activitiesQuery.data) {
    return (
      <CeremonyArea
        label="Activities"
        title="Activities"
        description="Create and resume Assessment Campaign drafts."
        danger
      >
        <CeremonyEmpty note={loadError} />
      </CeremonyArea>
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

  const createForm = canCreate && !missingCategory ? (
    <WorkWell
      live={false}
      className="registry-create"
      label="Create assessment Campaign"
      head={(
        <WorkWellHead>
          <h2 id="create-heading" className="work-well__title" tabIndex={-1}>Create assessment Campaign</h2>
          <p className="work-well__ident">Activity form: Campaign. Configured type: Assessment.</p>
        </WorkWellHead>
      )}
    >
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
            <Key type="submit" variant="transmit" disabled={createMutation.isPending} waiting={createMutation.isPending}>
              {createMutation.isPending ? "Creating…" : "Create assessment Campaign"}
            </Key>
          </Stack>
        </Container>
    </WorkWell>
  ) : null;

  const rows = data?.activities ?? [];
  const emptyRegistry = rows.length === 0;
  const showCreateForm = Boolean(createForm) && (emptyRegistry || createRevealed);

  return (
    <OperateArea
      className={cx("workspace-area", "work-plane", "registry-wall", emptyRegistry && "registry-wall--empty", !emptyRegistry && rows.length <= 4 && "registry-wall--hug")}
      framed={false}
      label="Activities"
      title="Activities"
      description="Create and resume Assessment Campaign drafts."
      context={canCreate && missingCategory ? (
        <Alert variant="info" title={`No permitted ${missingCategory.replaceAll("_", " ")} revisions are available`}>
          A ready source set is required before a draft can be created.
        </Alert>
      ) : null}
    >
      <EtchedFrame className="datatable-frame registry-frame" inset="flush">
        <ActivityRegistry
          rows={rows}
          canCreate={canCreate && !missingCategory}
          onRevealCreate={() => {
            setCreateRevealed(true);
          }}
          createRevealed={createRevealed}
        />
      </EtchedFrame>
      {showCreateForm ? createForm : null}
    </OperateArea>
  );
}

function ActivityRegistry({
  rows,
  canCreate,
  createRevealed,
  onRevealCreate,
}: {
  rows: readonly ProductionActivitySummary[];
  canCreate: boolean;
  createRevealed: boolean;
  onRevealCreate: () => void;
}) {
  const [search, setSearch] = useState("");
  const [sorts, setSorts] = useState<{ key: ActivitySortKey; dir: "asc" | "desc" }[]>([{ key: "title", dir: "asc" }]);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(16);
  const query = search.trim().toLowerCase();
  const match = useCallback(
    (row: ProductionActivitySummary) => {
      if (!query) return true;
      return row.title.toLowerCase().includes(query) || row.activity_id.toLowerCase().includes(query);
    },
    [query],
  );
  const getSortValue = useCallback((row: ProductionActivitySummary, key: ActivitySortKey) => {
    return key === "activation" ? (row.has_activated_cohort ? 1 : 0) : row.title.toLowerCase();
  }, []);
  const slice = useTableController({
    rows,
    match,
    sorts,
    page,
    pageSize,
    getSortValue,
  });

  const handleSort = (key: ActivitySortKey) => {
    setSorts((prev) => {
      const idx = prev.findIndex((spec) => spec.key === key);
      let next = prev.slice();
      if (idx === -1) next.push({ key, dir: "asc" });
      else if (next[idx].dir === "asc") next[idx] = { key, dir: "desc" };
      else next.splice(idx, 1);
      if (!next.length) next = [{ key: "title", dir: "asc" }];
      return next;
    });
    setPage(0);
  };

  return (
    <DataTableShell
      toolbar={
        <DataTableToolbar
          ariaLabel="Activities registry controls"
          actions={
            canCreate && rows.length > 0 && !createRevealed ? (
              <div className="registry-assign-keys">
                <Key variant="quiet" size="compact" ariaExpanded={false} onClick={onRevealCreate}>
                  Create assessment Campaign
                </Key>
              </div>
            ) : undefined
          }
          readout={
            <ToolbarReadout
              label="Showing"
              value={`${slice.total} campaign${slice.total === 1 ? "" : "s"}`}
              valueId="activityCountValue"
            />
          }
          search={
            <ToolbarSearch
              id="activitySearchInput"
              label="Search campaign title or ID"
              placeholder="SEARCH TITLE OR ID"
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(0);
              }}
            />
          }
        />
      }
      scrollProps={{ tabIndex: 0, "aria-label": "Campaign rows, scrollable" }}
      table={
        <table className="datatable-table datatable-table--fit" hidden={slice.total === 0}>
          <caption className="visually-hidden">Activities</caption>
          <thead>
            <tr>
              <SortableHeader sortKey="title" sorts={sorts} onSort={handleSort} label="Campaign" />
              <SortableHeader sortKey="activation" sorts={sorts} onSort={handleSort} label="Activation" />
            </tr>
          </thead>
          <tbody>
            {slice.pageRows.map((row) => (
              <tr key={row.activity_id} className="datatable-row">
                <td className="cell-id">
                  <Link className="datatable-id" to={`/activities/${row.activity_id}/setup`}>
                    {row.title}
                  </Link>
                </td>
                <td className="cell-content">
                  <StateReadout
                    variant={row.has_activated_cohort ? "sealed" : "rest"}
                    solid={row.has_activated_cohort}
                    label={row.has_activated_cohort ? "Activated" : "Draft"}
                    className="state-cell"
                    labelClassName="state-label"
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      }
      empty={
        slice.total === 0 ? (
          <EmptyPlate
            className="datatable-empty"
            label={query ? "No matching activities" : "No activities"}
            note={query
              ? "No loaded campaigns match this search."
              : canCreate
                ? "No activities are available. Create an assessment Campaign below."
                : "No activities are available."}
          />
        ) : null
      }
      footer={
        <DataTablePagination
          total={slice.total}
          startIndex={slice.startIdx}
          visibleCount={slice.pageRows.length}
          page={slice.page}
          pageCount={slice.pageCount}
          pageSize={pageSize}
          pageSizeOptions={[16, 32]}
          onPageSizeChange={(next) => {
            setPageSize(next);
            setPage(0);
          }}
          onPageChange={setPage}
          onPrevious={() => setPage((current) => Math.max(0, current - 1))}
          onNext={() => setPage((current) => current + 1)}
        />
      }
    />
  );
}
