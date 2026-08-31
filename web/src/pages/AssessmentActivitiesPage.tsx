import { useCallback, useState } from "react";
import {
  isAssessmentAccessLoss,
  REQUIRED_SOURCE_CATEGORIES,
  type ProductionActivityList,
  type ProductionActivitySummary,
  type ProductionSourceOption,
} from "../api/production-assessment";
import { sourceCategoryLabel } from "../features/assessment/campaignCreatePresentation";
import {
  CeremonyArea,
  CeremonyUnavailable,
  CeremonyWait,
  DataTablePagination,
  DataTableShell,
  DataTableToolbar,
  DatatableActions,
  DatatableCell,
  DatatableEmpty,
  DatatableId,
  DatatableRow,
  DatatableStateReadout,
  DatatableTable,
  InstantReadout,
  Key,
  OperateArea,
  registryTableHug,
  SortableHeader,
  ToolbarReadout,
  ToolbarSearch,
  useTableController,
} from "../design-system";
import { SEARCH_TITLE_OR_ID_PLACEHOLDER } from "../content/fieldCopy";
import {
  useAssessmentActivitiesQuery,
  useAssessmentSourceOptionsQuery,
} from "../features/assessment/queries";

export interface AssessmentActivitiesPageProps {
  organizationId?: string;
  loadActivities: (signal?: AbortSignal) => Promise<ProductionActivityList>;
  loadSourceOptions: (signal?: AbortSignal) => Promise<{ sources: ProductionSourceOption[] }>;
}

type ActivitySortKey = "title" | "activation" | "revision" | "updated";

export function AssessmentActivitiesPage({
  loadActivities,
  loadSourceOptions,
}: AssessmentActivitiesPageProps) {
  const activitiesQuery = useAssessmentActivitiesQuery(loadActivities);
  const canCreate = activitiesQuery.isFetchedAfterMount
    && activitiesQuery.isSuccess
    && activitiesQuery.data.permitted_actions.includes("create_assessment");
  const sourcesQuery = useAssessmentSourceOptionsQuery(loadSourceOptions, canCreate);
  const sources = sourcesQuery.data?.sources ?? [];

  const loading = !activitiesQuery.isFetchedAfterMount || (canCreate && !sourcesQuery.isFetched);
  const accessChanged = isAssessmentAccessLoss(activitiesQuery.error)
    || isAssessmentAccessLoss(sourcesQuery.error);
  const loadError = activitiesQuery.error instanceof Error && !isAssessmentAccessLoss(activitiesQuery.error)
    ? activitiesQuery.error.message
    : null;

  if (loading) {
    return (
      <CeremonyArea
        label="Activities"
        title="Activities"
        description="Create and resume Assessment Campaign drafts."
      >
        <CeremonyWait label="Loading activities…" />
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

  if (loadError && !activitiesQuery.data) {
    return (
      <CeremonyUnavailable
        title="Activities"
        description="Create and resume Assessment Campaign drafts."
        note={loadError}
        danger
      />
    );
  }

  const data = activitiesQuery.data;
  const missingCategory = REQUIRED_SOURCE_CATEGORIES.find((category) => !sources.some((source) => source.category === category));
  const rows = data?.activities ?? [];
  const offerCreate = canCreate && !missingCategory;

  return (
    <ActivityRegistry
      rows={rows}
      offerCreate={offerCreate}
      advisory={canCreate && missingCategory ? {
        label: "Sources",
        copy: `No permitted ${sourceCategoryLabel(missingCategory)} revisions are available. A ready source set is required before a draft can be created.`,
      } : undefined}
    />
  );
}

function ActivityRegistry({
  rows,
  offerCreate,
  advisory,
}: {
  rows: readonly ProductionActivitySummary[];
  offerCreate: boolean;
  advisory?: { label: string; copy: string };
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
    switch (key) {
      case "activation":
        return row.has_activated_cohort ? 1 : 0;
      case "revision":
        return String(row.revision_number).padStart(8, "0");
      case "updated":
        return row.updated_at;
      default:
        return row.title.toLowerCase();
    }
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

  const createAction = offerCreate ? (
    <DatatableActions>
      <Key variant="quiet" size="compact" to="/activities/new">
        Create
      </Key>
    </DatatableActions>
  ) : undefined;

  return (
    <OperateArea
      bay="registry"
      hug={registryTableHug(slice.total)}
      frame="registry"
      label="Activities"
      title="Activities"
      description="Create and resume Assessment Campaign drafts."
      advisory={advisory}
    >
      <DataTableShell
      toolbar={
        <DataTableToolbar
          ariaLabel="Activities registry controls"
          actions={createAction}
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
              placeholder={SEARCH_TITLE_OR_ID_PLACEHOLDER}
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
        <DatatableTable caption="Activities" hidden={slice.total === 0}>
          <thead>
            <tr>
              <SortableHeader sortKey="title" sorts={sorts} onSort={handleSort} label="Campaign" colMin="id" />
              <SortableHeader sortKey="activation" sorts={sorts} onSort={handleSort} label="Activation" colMin="state" />
              <SortableHeader sortKey="updated" sorts={sorts} onSort={handleSort} label="Updated" colMin="instant" />
              <SortableHeader sortKey="revision" sorts={sorts} onSort={handleSort} label="Rev" colMin="rev" />
            </tr>
          </thead>
          <tbody>
            {slice.pageRows.map((row) => {
              return (
                <DatatableRow key={row.activity_id}>
                  <DatatableCell kind="id" colMin="id">
                    <DatatableId to={`/activities/${row.activity_id}/setup`}>
                      {row.title}
                    </DatatableId>
                  </DatatableCell>
                  <DatatableCell kind="state" colMin="state">
                    <DatatableStateReadout
                      variant={row.has_activated_cohort ? "sealed" : "rest"}
                      solid={row.has_activated_cohort}
                      label={row.has_activated_cohort ? "Activated" : "Draft"}
                    />
                  </DatatableCell>
                  <DatatableCell kind="content" colMin="instant">
                    <InstantReadout value={row.updated_at} />
                  </DatatableCell>
                  <DatatableCell kind="content" colMin="rev">{row.revision_number}</DatatableCell>
                </DatatableRow>
              );
            })}
          </tbody>
        </DatatableTable>
      }
      empty={
        slice.total === 0 ? (
          <DatatableEmpty
            inset
            label={query ? "No matching activities" : "No activities"}
            note={query
              ? "Nothing matches the current search. Clear the search to restore the registry."
              : "No activities are available."}
          >
            {query ? (
              <Key
                size="compact"
                onClick={() => {
                  setSearch("");
                  setPage(0);
                }}
              >
                Clear search
              </Key>
            ) : null}
          </DatatableEmpty>
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
    </OperateArea>
  );
}
