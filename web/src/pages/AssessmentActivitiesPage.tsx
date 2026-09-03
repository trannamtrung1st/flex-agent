import { useMemo, useState } from "react";
import {
  canonicalizeActivityListQuery,
  isAssessmentAccessLoss,
  REQUIRED_SOURCE_CATEGORIES,
  type ActivitySortField,
  type NumberedActivityListQuery,
  type ProductionActivityList,
  type ProductionActivitySummary,
  type ProductionSourceOptionsResponse,
} from "../api/production-assessment";
import { sourceCategoryLabel } from "../features/assessment/campaignCreatePresentation";
import {
  Alert,
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
} from "../design-system";
import { SEARCH_TITLE_OR_ID_PLACEHOLDER } from "../content/fieldCopy";
import {
  useAssessmentActivitiesQuery,
  useAssessmentSourceOptionsQuery,
} from "../features/assessment/queries";

export interface AssessmentActivitiesPageProps {
  organizationId?: string;
  loadActivities: (query: NumberedActivityListQuery, signal?: AbortSignal) => Promise<ProductionActivityList>;
  loadSourceOptions: (signal?: AbortSignal) => Promise<ProductionSourceOptionsResponse>;
}

type ActivitySortKey = ActivitySortField;

export function AssessmentActivitiesPage({
  loadActivities,
  loadSourceOptions,
}: AssessmentActivitiesPageProps) {
  const [search, setSearch] = useState("");
  const [sorts, setSorts] = useState<{ key: ActivitySortKey; dir: "asc" | "desc" }[]>([{ key: "title", dir: "asc" }]);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(16);
  const query = useMemo(
    () => canonicalizeActivityListQuery({
      paging: "numbered",
      page: page + 1,
      pageSize,
      q: search,
      sort: sorts.map((spec) => ({ field: spec.key, direction: spec.dir })),
    }),
    [page, pageSize, search, sorts],
  );
  const activitiesQuery = useAssessmentActivitiesQuery(loadActivities, query);
  const canCreate = activitiesQuery.isFetchedAfterMount
    && activitiesQuery.isSuccess
    && activitiesQuery.data.permitted_actions.includes("create_assessment");
  const sourcesQuery = useAssessmentSourceOptionsQuery(loadSourceOptions, canCreate);
  const sources = sourcesQuery.data?.sources ?? [];

  const waitingForSources = canCreate && !sourcesQuery.isFetched;
  const loading = (!activitiesQuery.data && !activitiesQuery.isError) || waitingForSources;
  const accessChanged = isAssessmentAccessLoss(activitiesQuery.error)
    || isAssessmentAccessLoss(sourcesQuery.error);
  const loadError = activitiesQuery.error instanceof Error && !isAssessmentAccessLoss(activitiesQuery.error)
    ? activitiesQuery.error.message
    : null;

  const pagination = activitiesQuery.data?.pagination;
  if (pagination && !activitiesQuery.isFetching) {
    if (pagination.total_pages === 0 && page !== 0) {
      setPage(0);
    } else if (pagination.total_pages > 0 && pagination.page > pagination.total_pages) {
      setPage(pagination.total_pages - 1);
    }
  }

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
        recovery={{ label: "Retry", onClick: () => { void activitiesQuery.refetch(); } }}
      />
    );
  }

  const data = activitiesQuery.data;
  const missingCategory = REQUIRED_SOURCE_CATEGORIES.find((category) => !sources.some((source) => source.category === category));
  const rows = data?.activities ?? [];
  const offerCreate = canCreate && !missingCategory;
  const paginationMeta = data?.pagination;
  const total = paginationMeta?.total_items ?? rows.length;
  const pageCount = paginationMeta?.total_pages ?? 0;
  const displayedPage = paginationMeta ? paginationMeta.page - 1 : page;
  const displayedPageSize = paginationMeta?.page_size ?? pageSize;
  const queryText = query.q;

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
      hug={registryTableHug(rows.length)}
      frame="registry"
      label="Activities"
      title="Activities"
      description="Create and resume Assessment Campaign drafts."
      advisory={canCreate && missingCategory ? {
        label: "Sources",
        copy: `No permitted ${sourceCategoryLabel(missingCategory)} revisions are available. A ready source set is required before a draft can be created.`,
      } : undefined}
      context={loadError ? (
        <Alert variant="danger" title="Could not refresh Activities">
          {loadError}
          <Key size="compact" onClick={() => void activitiesQuery.refetch()}>
            Retry
          </Key>
        </Alert>
      ) : undefined}
    >
      <DataTableShell
      toolbar={
        <DataTableToolbar
          ariaLabel="Activities registry controls"
          actions={createAction}
          readout={
            <ToolbarReadout
              label="Showing"
              value={`${total} campaign${total === 1 ? "" : "s"}`}
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
        <DatatableTable caption="Activities" hidden={total === 0}>
          <thead>
            <tr>
              <SortableHeader sortKey="title" sorts={sorts} onSort={handleSort} label="Campaign" colMin="id" />
              <SortableHeader sortKey="activation" sorts={sorts} onSort={handleSort} label="Activation" colMin="state" />
              <SortableHeader sortKey="updated" sorts={sorts} onSort={handleSort} label="Updated" colMin="instant" />
              <SortableHeader sortKey="revision" sorts={sorts} onSort={handleSort} label="Rev" colMin="rev" />
            </tr>
          </thead>
          <tbody>
            {rows.map((row: ProductionActivitySummary) => {
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
        total === 0 ? (
          <DatatableEmpty
            inset
            label={queryText ? "No matching activities" : "No activities"}
            note={queryText
              ? "Nothing matches the current search. Clear the search to restore the registry."
              : "No activities are available."}
          >
            {queryText ? (
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
          total={total}
          startIndex={total === 0 ? 0 : displayedPage * displayedPageSize}
          visibleCount={rows.length}
          page={displayedPage}
          pageCount={pageCount}
          pageSize={pageSize}
          pageSizeOptions={[16, 32]}
          waiting={activitiesQuery.isFetching}
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
