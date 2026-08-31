import { useCallback, useEffect, useId, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import { createProductionAssessmentClient } from "../api/production-assessment";
import {
  createEnrollmentIdempotencyKey,
  createProductionEnrollmentClient,
  enrollmentFailureCopy,
  enrollmentOutcomeCopy,
  PARTICIPANT_OPTIONS_PAGE_LIMIT,
  type EnrollmentCandidateV1,
  type EnrollmentSummaryV1,
} from "../api/production-enrollment";
import {
  enrollmentAssignedReceipt,
  enrollmentAssignmentDescription,
  enrollmentRecordVariant,
  enrollmentStatusCopy,
} from "../lib/enrollment-presentation";
import {
  Alert,
  BackKey,
  CeremonyArea,
  CeremonyDialog,
  CeremonyUnavailable,
  CeremonyWait,
  CompactId,
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
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  EMPTY_SELECTION,
  InstantReadout,
  isSelected,
  Key,
  KeyGroup,
  matchingQueryKey,
  OperateArea,
  registryTableHug,
  resolveSelectedIds,
  SelectHeader,
  SelectMark,
  SortableHeader,
  StaticHeader,
  ToolbarReadout,
  ToolbarSearch,
  toggleRow,
  useTableController,
  usePushToast,
  type TableSelection,
} from "../design-system";
import { SEARCH_NAME_OR_ID_PLACEHOLDER } from "../content/fieldCopy";

type EnrollmentSortKey = "participant" | "enrollment" | "status" | "assigned" | "updated" | "revision";

function appendById<T>(existing: readonly T[], incoming: readonly T[], id: (row: T) => string): T[] {
  const seen = new Set(existing.map(id));
  const extra = incoming.filter((row) => !seen.has(id(row)));
  return extra.length === 0 ? [...existing] : [...existing, ...extra];
}

export function ProductionEnrollmentPage() {
  const { activityId = "", cohortId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const assessment = useMemo(() => createProductionAssessmentClient(fetchJson), [fetchJson]);
  const [enrollments, setEnrollments] = useState<EnrollmentSummaryV1[] | null>(null);
  const [enrollmentCursor, setEnrollmentCursor] = useState<string | null>(null);
  const [enrollmentHasMore, setEnrollmentHasMore] = useState(false);
  const [candidates, setCandidates] = useState<EnrollmentCandidateV1[]>([]);
  const [candidateHasMore, setCandidateHasMore] = useState(false);
  const [campaignTitle, setCampaignTitle] = useState<string | undefined>();
  const [taskTitle, setTaskTitle] = useState<string | undefined>();
  const [error, setError] = useState<string | null>(null);
  const [candidateError, setCandidateError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [loadingMoreEnrollments, setLoadingMoreEnrollments] = useState(false);
  const pushToast = usePushToast();

  useEffect(() => {
    const signal = { cancelled: false };
    void client.listEnrollments(activityId, cohortId)
      .then((page) => {
        if (signal.cancelled) return;
        setEnrollments(page.items);
        setEnrollmentCursor(page.next_cursor ?? null);
        setEnrollmentHasMore(page.has_more);
        setError(null);
      })
      .catch((caught: unknown) => {
        if (!signal.cancelled) {
          setError(enrollmentFailureCopy(caught, "Participants are not available."));
        }
      });
    void client.listCandidates(activityId, cohortId, null, PARTICIPANT_OPTIONS_PAGE_LIMIT)
      .then((options) => {
        if (signal.cancelled) return;
        setCandidates(options.items);
        setCandidateHasMore(options.has_more);
        setCandidateError(null);
      })
      .catch((caught: unknown) => {
        if (!signal.cancelled) {
          setCandidateError(enrollmentFailureCopy(caught, "Assignable Participants are not available."));
        }
      });
    void assessment.loadSetup(activityId)
      .then((view) => {
        if (signal.cancelled) return;
        setCampaignTitle(view.title);
        setTaskTitle(view.task_title);
      })
      .catch(() => {
        /* Registry remains usable without Campaign copy. */
      });
    return () => {
      signal.cancelled = true;
    };
  }, [activityId, assessment, client, cohortId]);

  function loadMoreEnrollments() {
    if (!enrollmentHasMore || !enrollmentCursor || loadingMoreEnrollments) return;
    setLoadingMoreEnrollments(true);
    void client.listEnrollments(activityId, cohortId, enrollmentCursor)
      .then((page) => {
        setEnrollments((current) => appendById(current ?? [], page.items, (row) => row.enrollment_id));
        setEnrollmentCursor(page.next_cursor ?? null);
        setEnrollmentHasMore(page.has_more);
        setError(null);
      })
      .catch((caught: unknown) => {
        setError(enrollmentFailureCopy(caught, "The next Participants page could not be loaded."));
      })
      .finally(() => setLoadingMoreEnrollments(false));
  }

  function assign(candidate: EnrollmentCandidateV1): Promise<boolean> {
    setPending(true);
    return client.assign(activityId, cohortId, candidate.actor_id, createEnrollmentIdempotencyKey())
      .then((outcome) => {
        const alreadyAssigned = outcome.outcome_code === "enrollment.assignment.deduplicated";
        if (!outcome.succeeded && !alreadyAssigned) {
          setError(enrollmentOutcomeCopy(outcome.outcome_code, "Assignment did not complete."));
          return false;
        }
        setError(null);
        pushToast(alreadyAssigned
          ? { label: "Already assigned", copy: candidate.display_label }
          : enrollmentAssignedReceipt(candidate.display_label));
        return Promise.allSettled([
          client.listEnrollments(activityId, cohortId),
          client.listCandidates(activityId, cohortId, null, PARTICIPANT_OPTIONS_PAGE_LIMIT),
        ]).then(([enrollmentRefresh, candidateRefresh]) => {
          if (enrollmentRefresh.status === "fulfilled") {
            setEnrollments(enrollmentRefresh.value.items);
            setEnrollmentCursor(enrollmentRefresh.value.next_cursor ?? null);
            setEnrollmentHasMore(enrollmentRefresh.value.has_more);
          } else {
            setError(enrollmentFailureCopy(enrollmentRefresh.reason, "The assigned list could not be refreshed."));
          }
          if (candidateRefresh.status === "fulfilled") {
            setCandidates(candidateRefresh.value.items);
            setCandidateHasMore(candidateRefresh.value.has_more);
            setCandidateError(null);
          } else {
            setCandidates([]);
            setCandidateHasMore(false);
            setCandidateError(enrollmentFailureCopy(candidateRefresh.reason, "Assignable Participants are not available."));
          }
          return true;
        });
      })
      .catch((caught: unknown) => {
        setError(enrollmentFailureCopy(caught, "Assignment did not complete."));
        return false;
      })
      .finally(() => setPending(false));
  }

  if (error && enrollments === null) {
    return (
      <CeremonyUnavailable
        title="Participants unavailable"
        note={error}
        danger
        recovery={{ label: "Return to setup", to: `/activities/${activityId}/setup` }}
      />
    );
  }

  if (enrollments === null) {
    return (
      <CeremonyArea label="Participants" title="Participants">
        <CeremonyWait label="Loading Participants…" />
      </CeremonyArea>
    );
  }

  return (
    <EnrollmentRegistry
      activityId={activityId}
      cohortId={cohortId}
      rows={enrollments}
      candidates={candidates}
      pending={pending}
      enrollmentHasMore={enrollmentHasMore}
      loadingMoreEnrollments={loadingMoreEnrollments}
      onLoadMoreEnrollments={loadMoreEnrollments}
      candidateHasMore={candidateHasMore}
      onAssign={assign}
      description={enrollmentAssignmentDescription(campaignTitle, taskTitle)}
      advisory={enrollmentHasMore ? {
        label: "Registry",
        copy: "More Participants remain. Load more fetches the next server page. Search and sort apply to loaded rows.",
      } : undefined}
      error={error}
      candidateError={candidateError}
    />
  );
}

function EnrollmentRegistry({
  activityId,
  cohortId,
  rows,
  candidates,
  pending,
  enrollmentHasMore,
  loadingMoreEnrollments,
  onLoadMoreEnrollments,
  candidateHasMore,
  onAssign,
  description,
  advisory,
  error,
  candidateError,
}: {
  activityId: string;
  cohortId: string;
  rows: readonly EnrollmentSummaryV1[];
  candidates: readonly EnrollmentCandidateV1[];
  pending: boolean;
  enrollmentHasMore: boolean;
  loadingMoreEnrollments: boolean;
  onLoadMoreEnrollments: () => void;
  candidateHasMore: boolean;
  onAssign: (candidate: EnrollmentCandidateV1) => Promise<boolean>;
  description: string;
  advisory?: { label: string; copy: string };
  error: string | null;
  candidateError: string | null;
}) {
  const [search, setSearch] = useState("");
  const [sorts, setSorts] = useState<{ key: EnrollmentSortKey; dir: "asc" | "desc" }[]>([{ key: "participant", dir: "asc" }]);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(16);
  const [assignOpen, setAssignOpen] = useState(false);
  const [assignSearch, setAssignSearch] = useState("");
  const [assignPage, setAssignPage] = useState(0);
  const [assignPageSize, setAssignPageSize] = useState(16);
  const [selection, setSelection] = useState<TableSelection>(EMPTY_SELECTION);
  const assignTitleId = useId();
  const assignSelectId = useId();
  const query = search.trim().toLowerCase();
  const match = useCallback(
    (row: EnrollmentSummaryV1) => {
      if (!query) return true;
      return row.display_label.toLowerCase().includes(query)
        || row.status.toLowerCase().includes(query)
        || row.enrollment_id.toLowerCase().includes(query)
        || row.participant_actor_id.toLowerCase().includes(query);
    },
    [query],
  );
  const getSortValue = useCallback((row: EnrollmentSummaryV1, key: EnrollmentSortKey) => {
    switch (key) {
      case "enrollment":
        return row.enrollment_id.toLowerCase();
      case "status":
        return row.status.toLowerCase();
      case "assigned":
        return row.assigned_at;
      case "updated":
        return row.updated_at;
      case "revision":
        return String(row.revision).padStart(8, "0");
      default:
        return row.display_label.toLowerCase();
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

  const handleSort = (key: EnrollmentSortKey) => {
    setSorts((prev) => {
      const idx = prev.findIndex((spec) => spec.key === key);
      let next = prev.slice();
      if (idx === -1) next.push({ key, dir: "asc" });
      else if (next[idx].dir === "asc") next[idx] = { key, dir: "desc" };
      else next.splice(idx, 1);
      if (!next.length) next = [{ key: "participant", dir: "asc" }];
      return next;
    });
    setPage(0);
  };

  const assignQuery = assignSearch.trim().toLowerCase();
  const assignMatch = useCallback(
    (row: EnrollmentCandidateV1) => {
      if (!assignQuery) return true;
      return row.display_label.toLowerCase().includes(assignQuery)
        || row.actor_id.toLowerCase().includes(assignQuery);
    },
    [assignQuery],
  );
  const assignSlice = useTableController({
    rows: candidates,
    match: assignMatch,
    sorts: [{ key: "participant" as const, dir: "asc" as const }],
    page: assignPage,
    pageSize: assignPageSize,
    getSortValue: (row) => row.display_label.toLowerCase(),
  });
  const candidateIds = useMemo(() => candidates.map((candidate) => candidate.actor_id), [candidates]);
  const assignPageIds = useMemo(
    () => assignSlice.pageRows.map((candidate) => candidate.actor_id),
    [assignSlice.pageRows],
  );
  const assignMatchingIds = useMemo(
    () => assignSlice.visibleRows.map((candidate) => candidate.actor_id),
    [assignSlice.visibleRows],
  );
  const assignQueryKey = matchingQueryKey({ assign: "candidates" });
  const selectedIds = resolveSelectedIds(selection, candidateIds);
  const selectedCandidate = selectedIds.length === 1
    ? candidates.find((candidate) => candidate.actor_id === selectedIds[0])
    : undefined;

  function closeAssignDialog() {
    setAssignOpen(false);
    setSelection(EMPTY_SELECTION);
    setAssignSearch("");
    setAssignPage(0);
  }

  const assignAction = candidates.length > 0 || candidateHasMore ? (
    <DatatableActions>
      <Key variant="quiet" size="compact" onClick={() => setAssignOpen(true)}>
        Assign
      </Key>
    </DatatableActions>
  ) : undefined;

  return (
    <OperateArea
      bay="registry"
      hug={registryTableHug(slice.total)}
      frame="registry"
      label="Participants"
      title="Participants"
      description={description}
      back={<BackKey to={`/activities/${activityId}/setup`} label="Setup" />}
      advisory={advisory}
      context={error || candidateError ? (
        <>
          {error ? <Alert variant="danger" title="Could not update Participants">{error}</Alert> : null}
          {candidateError ? <Alert variant="danger" title="Assignable Participants unavailable">{candidateError}</Alert> : null}
        </>
      ) : undefined}
    >
    <DataTableShell
      toolbar={
        <DataTableToolbar
          ariaLabel="Participant registry controls"
          actions={assignAction}
          readout={
            <ToolbarReadout
              label="Showing"
              value={`${slice.total} participant${slice.total === 1 ? "" : "s"}`}
              valueId="enrollmentCountValue"
            />
          }
          search={
            <ToolbarSearch
              id="enrollmentSearchInput"
              label="Search participant, enrollment, or status"
              placeholder={SEARCH_NAME_OR_ID_PLACEHOLDER}
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(0);
              }}
            />
          }
        />
      }
      scrollProps={{ tabIndex: 0, "aria-label": "Participant rows, scrollable" }}
      table={
        <DatatableTable caption="Participants" hidden={slice.total === 0}>
          <thead>
            <tr>
              <SortableHeader sortKey="participant" sorts={sorts} onSort={handleSort} label="Participant" colMin="id" />
              <SortableHeader sortKey="enrollment" sorts={sorts} onSort={handleSort} label="Enrollment" colMin="compactId" />
              <SortableHeader sortKey="status" sorts={sorts} onSort={handleSort} label="Record" colMin="state" />
              <SortableHeader sortKey="assigned" sorts={sorts} onSort={handleSort} label="Assigned" colMin="instant" />
              <SortableHeader sortKey="updated" sorts={sorts} onSort={handleSort} label="Updated" colMin="instant" />
              <SortableHeader sortKey="revision" sorts={sorts} onSort={handleSort} label="Rev" colMin="rev" />
            </tr>
          </thead>
          <tbody>
            {slice.pageRows.map((row) => {
              const record = enrollmentRecordVariant(row.status);
              return (
                <DatatableRow key={row.enrollment_id}>
                  <DatatableCell kind="id" colMin="id">
                    <DatatableId
                      to={`/activities/${activityId}/cohorts/${cohortId}/enrollments/${row.enrollment_id}`}
                    >
                      {row.display_label}
                    </DatatableId>
                  </DatatableCell>
                  <DatatableCell kind="content" colMin="compactId">
                    <CompactId value={row.enrollment_id} />
                  </DatatableCell>
                  <DatatableCell kind="state" colMin="state">
                    <DatatableStateReadout
                      variant={record.variant}
                      solid={record.solid}
                      label={enrollmentStatusCopy(row.status)}
                    />
                  </DatatableCell>
                  <DatatableCell kind="content" colMin="instant">
                    <InstantReadout value={row.assigned_at} />
                  </DatatableCell>
                  <DatatableCell kind="content" colMin="instant">
                    <InstantReadout value={row.updated_at} />
                  </DatatableCell>
                  <DatatableCell kind="content" colMin="rev">{row.revision}</DatatableCell>
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
            label={query ? "No matching Participants" : "No Participants assigned"}
            note={query
              ? "Nothing matches the current search. Clear the search to restore the registry."
              : "Assign an eligible Participant to this cohort."}
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
        <>
          {enrollmentHasMore ? (
            <KeyGroup justify="start">
              <Key
                variant="quiet"
                size="compact"
                waiting={loadingMoreEnrollments}
                disabled={loadingMoreEnrollments}
                onClick={onLoadMoreEnrollments}
              >
                Load more Participants
              </Key>
            </KeyGroup>
          ) : null}
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
        </>
      }
    />
    <CeremonyDialog open={assignOpen} onClose={closeAssignDialog} labelledBy={assignTitleId}>
      <DialogPlate width="wide">
        <DialogPlateHead marker={false} title="Assign Participant" titleId={assignTitleId} />
        <DialogPlateBody>
          <DataTableShell
            toolbar={
              <DataTableToolbar
                ariaLabel="Assignable Participant controls"
                readout={
                  <ToolbarReadout
                    label="Showing"
                    value={`${assignSlice.total} participant${assignSlice.total === 1 ? "" : "s"}`}
                    valueId="assignCountValue"
                  />
                }
                search={
                  <ToolbarSearch
                    id="assignSearchInput"
                    label="Search participant or actor"
                    placeholder={SEARCH_NAME_OR_ID_PLACEHOLDER}
                    value={assignSearch}
                    onChange={(event) => {
                      setAssignSearch(event.target.value);
                      setAssignPage(0);
                    }}
                  />
                }
              />
            }
            scrollProps={{ "aria-label": "Assignable Participant rows, scrollable" }}
            table={
              <DatatableTable caption="Assignable Participants" hidden={assignSlice.total === 0}>
                <thead>
                  <tr>
                    <SelectHeader
                      id={assignSelectId}
                      selection={selection}
                      pageIds={assignPageIds}
                      matchingIds={assignMatchingIds}
                      queryKey={assignQueryKey}
                      noun="participants"
                      onTransition={setSelection}
                    />
                    <StaticHeader label="Participant" colMin="id" />
                    <StaticHeader label="Actor" colMin="compactId" />
                  </tr>
                </thead>
                <tbody>
                  {assignSlice.pageRows.map((candidate) => (
                    <DatatableRow
                      key={candidate.actor_id}
                      selected={isSelected(selection, candidate.actor_id)}
                    >
                      <DatatableCell kind="select">
                        <SelectMark
                          checked={isSelected(selection, candidate.actor_id)}
                          label={`Select ${candidate.display_label}`}
                          onChange={(checked) => {
                            setSelection((current) => toggleRow(current, candidate.actor_id, checked));
                          }}
                        />
                      </DatatableCell>
                      <DatatableCell kind="id" colMin="id">
                        <DatatableId
                          onClick={() => {
                            const next = !isSelected(selection, candidate.actor_id);
                            setSelection((current) => toggleRow(current, candidate.actor_id, next));
                          }}
                        >
                          {candidate.display_label}
                        </DatatableId>
                      </DatatableCell>
                      <DatatableCell kind="content" colMin="compactId">
                        <CompactId tabbable value={candidate.actor_id} />
                      </DatatableCell>
                    </DatatableRow>
                  ))}
                </tbody>
              </DatatableTable>
            }
            empty={
              assignSlice.total === 0 ? (
                <DatatableEmpty
                  inset
                  label={assignQuery ? "No matching Participants" : "No assignable Participants"}
                  note={assignQuery
                    ? "Nothing matches the current search. Clear the search to restore the list."
                    : "No assignable Participants are on this page."}
                >
                  {assignQuery ? (
                    <Key
                      size="compact"
                      onClick={() => {
                        setAssignSearch("");
                        setAssignPage(0);
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
                total={assignSlice.total}
                startIndex={assignSlice.startIdx}
                visibleCount={assignSlice.pageRows.length}
                page={assignSlice.page}
                pageCount={assignSlice.pageCount}
                pageSize={assignPageSize}
                pageSizeOptions={[16, 32]}
                onPageSizeChange={(next) => {
                  setAssignPageSize(next);
                  setAssignPage(0);
                }}
                onPageChange={setAssignPage}
                onPrevious={() => setAssignPage((current) => Math.max(0, current - 1))}
                onNext={() => setAssignPage((current) => current + 1)}
              />
            }
          />
        </DialogPlateBody>
        <DialogPlateFooter
          arrangement="split"
          secondary={
            <Key variant="quiet" disabled={pending} onClick={closeAssignDialog}>
              Cancel
            </Key>
          }
          primary={
            <Key
              variant="transmit"
              size="large"
              waiting={pending}
              disabled={pending || !selectedCandidate}
              onClick={() => {
                if (!selectedCandidate) return;
                void onAssign(selectedCandidate).then((ok) => {
                  if (ok) closeAssignDialog();
                });
              }}
            >
              {pending ? "Assigning Participant" : "Assign Participant"}
            </Key>
          }
        />
      </DialogPlate>
    </CeremonyDialog>
    </OperateArea>
  );
}
