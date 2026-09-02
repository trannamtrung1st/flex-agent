import { useCallback, useEffect, useId, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import { createProductionAssessmentClient } from "../api/production-assessment";
import {
  createEnrollmentIdempotencyKey,
  createProductionEnrollmentClient,
  enrollmentFailureCopy,
  enrollmentOutcomeCopy,
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
  matchingQueryKey,
  OperateArea,
  registryTableHug,
  resolveSelectedIds,
  SelectHeader,
  SelectMark,
  StaticHeader,
  ToolbarReadout,
  ToolbarSearch,
  toggleRow,
  usePushToast,
  type TableSelection,
} from "../design-system";
import { SEARCH_NAME_OR_ID_PLACEHOLDER } from "../content/fieldCopy";

const PAGE_SIZE_OPTIONS = [16, 32] as const;
const DEFAULT_PAGE_SIZE = 16;

export function ProductionEnrollmentPage() {
  const { activityId = "", cohortId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const assessment = useMemo(() => createProductionAssessmentClient(fetchJson), [fetchJson]);
  const [enrollments, setEnrollments] = useState<EnrollmentSummaryV1[] | null>(null);
  const [enrollmentHasMore, setEnrollmentHasMore] = useState(false);
  const [enrollmentNextCursor, setEnrollmentNextCursor] = useState<string | null>(null);
  const [enrollmentStack, setEnrollmentStack] = useState<string[]>([]);
  const [enrollmentPageSize, setEnrollmentPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [enrollmentWaiting, setEnrollmentWaiting] = useState(false);
  const [candidates, setCandidates] = useState<EnrollmentCandidateV1[]>([]);
  const [candidateHasMore, setCandidateHasMore] = useState(false);
  const [candidateNextCursor, setCandidateNextCursor] = useState<string | null>(null);
  const [candidateStack, setCandidateStack] = useState<string[]>([]);
  const [candidatePageSize, setCandidatePageSize] = useState(DEFAULT_PAGE_SIZE);
  const [candidateWaiting, setCandidateWaiting] = useState(false);
  const [assignSearch, setAssignSearch] = useState("");
  const [campaignTitle, setCampaignTitle] = useState<string | undefined>();
  const [taskTitle, setTaskTitle] = useState<string | undefined>();
  const [error, setError] = useState<string | null>(null);
  const [candidateError, setCandidateError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const pushToast = usePushToast();

  const applyEnrollmentPage = useCallback((
    page: { items: EnrollmentSummaryV1[]; next_cursor?: string | null; has_more: boolean },
    stack: string[],
  ) => {
    setEnrollments(page.items);
    setEnrollmentNextCursor(page.next_cursor ?? null);
    setEnrollmentHasMore(page.has_more);
    setEnrollmentStack(stack);
    setError(null);
  }, []);

  const applyCandidatePage = useCallback((
    page: { items: EnrollmentCandidateV1[]; next_cursor?: string | null; has_more: boolean },
    stack: string[],
  ) => {
    setCandidates(page.items);
    setCandidateNextCursor(page.next_cursor ?? null);
    setCandidateHasMore(page.has_more);
    setCandidateStack(stack);
    setCandidateError(null);
  }, []);

  const loadEnrollments = useCallback((cursor: string | null, limit: number, stack: string[]) => {
    setEnrollmentWaiting(true);
    return client.listEnrollments(activityId, cohortId, cursor, limit)
      .then((page) => {
        applyEnrollmentPage(page, stack);
      })
      .catch((caught: unknown) => {
        setError(enrollmentFailureCopy(caught, "Participants are not available."));
      })
      .finally(() => setEnrollmentWaiting(false));
  }, [activityId, applyEnrollmentPage, client, cohortId]);

  const loadCandidates = useCallback((cursor: string | null, limit: number, stack: string[], q: string) => {
    setCandidateWaiting(true);
    return client.listCandidates(activityId, cohortId, cursor, limit, q)
      .then((page) => {
        applyCandidatePage(page, stack);
      })
      .catch((caught: unknown) => {
        setCandidateError(enrollmentFailureCopy(caught, "Assignable Participants are not available."));
        setCandidates([]);
        setCandidateHasMore(false);
        setCandidateNextCursor(null);
        setCandidateStack([]);
      })
      .finally(() => setCandidateWaiting(false));
  }, [activityId, applyCandidatePage, client, cohortId]);

  useEffect(() => {
    const signal = { cancelled: false };
    void client.listEnrollments(activityId, cohortId, null, enrollmentPageSize)
      .then((page) => {
        if (!signal.cancelled) applyEnrollmentPage(page, []);
      })
      .catch((caught: unknown) => {
        if (!signal.cancelled) {
          setError(enrollmentFailureCopy(caught, "Participants are not available."));
        }
      });
    return () => {
      signal.cancelled = true;
    };
  }, [activityId, applyEnrollmentPage, client, cohortId, enrollmentPageSize]);

  useEffect(() => {
    const signal = { cancelled: false };
    void client.listCandidates(activityId, cohortId, null, candidatePageSize, assignSearch)
      .then((page) => {
        if (!signal.cancelled) applyCandidatePage(page, []);
      })
      .catch((caught: unknown) => {
        if (!signal.cancelled) {
          setCandidateError(enrollmentFailureCopy(caught, "Assignable Participants are not available."));
        }
      });
    return () => {
      signal.cancelled = true;
    };
  }, [activityId, applyCandidatePage, assignSearch, candidatePageSize, client, cohortId]);

  useEffect(() => {
    const signal = { cancelled: false };
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
  }, [activityId, assessment]);

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
          client.listEnrollments(activityId, cohortId, null, enrollmentPageSize),
          client.listCandidates(activityId, cohortId, null, candidatePageSize, assignSearch),
        ]).then(([enrollmentRefresh, candidateRefresh]) => {
          if (enrollmentRefresh.status === "fulfilled") {
            applyEnrollmentPage(enrollmentRefresh.value, []);
          } else {
            setError(enrollmentFailureCopy(enrollmentRefresh.reason, "The assigned list could not be refreshed."));
          }
          if (candidateRefresh.status === "fulfilled") {
            applyCandidatePage(candidateRefresh.value, []);
          } else {
            setCandidates([]);
            setCandidateHasMore(false);
            setCandidateNextCursor(null);
            setCandidateStack([]);
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
      enrollmentWaiting={enrollmentWaiting}
      enrollmentPageIndex={enrollmentStack.length}
      enrollmentPageSize={enrollmentPageSize}
      onEnrollmentPrevious={() => {
        const stack = enrollmentStack.slice(0, -1);
        void loadEnrollments(stack.at(-1) ?? null, enrollmentPageSize, stack);
      }}
      onEnrollmentNext={() => {
        if (!enrollmentNextCursor) return;
        void loadEnrollments(enrollmentNextCursor, enrollmentPageSize, [...enrollmentStack, enrollmentNextCursor]);
      }}
      onEnrollmentPageSize={(next) => setEnrollmentPageSize(next)}
      candidateHasMore={candidateHasMore}
      candidateWaiting={candidateWaiting}
      candidatePageIndex={candidateStack.length}
      candidatePageSize={candidatePageSize}
      assignSearch={assignSearch}
      onAssignSearch={setAssignSearch}
      onCandidatePrevious={() => {
        const stack = candidateStack.slice(0, -1);
        void loadCandidates(stack.at(-1) ?? null, candidatePageSize, stack, assignSearch);
      }}
      onCandidateNext={() => {
        if (!candidateNextCursor) return;
        void loadCandidates(candidateNextCursor, candidatePageSize, [...candidateStack, candidateNextCursor], assignSearch);
      }}
      onCandidatePageSize={(next) => setCandidatePageSize(next)}
      onAssign={assign}
      description={enrollmentAssignmentDescription(campaignTitle, taskTitle)}
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
  enrollmentWaiting,
  enrollmentPageIndex,
  enrollmentPageSize,
  onEnrollmentPrevious,
  onEnrollmentNext,
  onEnrollmentPageSize,
  candidateHasMore,
  candidateWaiting,
  candidatePageIndex,
  candidatePageSize,
  assignSearch,
  onAssignSearch,
  onCandidatePrevious,
  onCandidateNext,
  onCandidatePageSize,
  onAssign,
  description,
  error,
  candidateError,
}: {
  activityId: string;
  cohortId: string;
  rows: readonly EnrollmentSummaryV1[];
  candidates: readonly EnrollmentCandidateV1[];
  pending: boolean;
  enrollmentHasMore: boolean;
  enrollmentWaiting: boolean;
  enrollmentPageIndex: number;
  enrollmentPageSize: number;
  onEnrollmentPrevious: () => void;
  onEnrollmentNext: () => void;
  onEnrollmentPageSize: (pageSize: number) => void;
  candidateHasMore: boolean;
  candidateWaiting: boolean;
  candidatePageIndex: number;
  candidatePageSize: number;
  assignSearch: string;
  onAssignSearch: (value: string) => void;
  onCandidatePrevious: () => void;
  onCandidateNext: () => void;
  onCandidatePageSize: (pageSize: number) => void;
  onAssign: (candidate: EnrollmentCandidateV1) => Promise<boolean>;
  description: string;
  error: string | null;
  candidateError: string | null;
}) {
  const [assignOpen, setAssignOpen] = useState(false);
  const [selection, setSelection] = useState<TableSelection>(EMPTY_SELECTION);
  const assignTitleId = useId();
  const assignSelectId = useId();
  const assignQuery = assignSearch.trim();
  const candidateIds = useMemo(() => candidates.map((candidate) => candidate.actor_id), [candidates]);
  const assignQueryKey = matchingQueryKey({ assign: "candidates", q: assignQuery });
  const selectedIds = resolveSelectedIds(selection, candidateIds);
  const selectedCandidate = selectedIds.length === 1
    ? candidates.find((candidate) => candidate.actor_id === selectedIds[0])
    : undefined;

  function closeAssignDialog() {
    setAssignOpen(false);
    setSelection(EMPTY_SELECTION);
    onAssignSearch("");
  }

  const assignAction = candidateError ? undefined : (
    <DatatableActions>
      <Key variant="quiet" size="compact" onClick={() => setAssignOpen(true)}>
        Assign
      </Key>
    </DatatableActions>
  );

  return (
    <OperateArea
      bay="registry"
      hug={registryTableHug(rows.length)}
      frame="registry"
      label="Participants"
      title="Participants"
      description={description}
      back={<BackKey to={`/activities/${activityId}/setup`} label="Setup" />}
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
              label="This page"
              value={`${rows.length} participant${rows.length === 1 ? "" : "s"}`}
              valueId="enrollmentCountValue"
            />
          }
        />
      }
      scrollProps={{ tabIndex: 0, "aria-label": "Participant rows, scrollable" }}
      table={
        <DatatableTable caption="Participants" hidden={rows.length === 0}>
          <thead>
            <tr>
              <StaticHeader label="Participant" colMin="id" />
              <StaticHeader label="Enrollment" colMin="compactId" />
              <StaticHeader label="Record" colMin="state" />
              <StaticHeader label="Assigned" colMin="instant" />
              <StaticHeader label="Updated" colMin="instant" />
              <StaticHeader label="Rev" colMin="rev" />
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => {
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
        rows.length === 0 ? (
          <DatatableEmpty
            inset
            label="No Participants assigned"
            note="Assign an eligible Participant to this cohort."
          />
        ) : null
      }
      footer={
        <DataTablePagination
          paging="cursor"
          visibleCount={rows.length}
          pageIndex={enrollmentPageIndex}
          pageSize={enrollmentPageSize}
          pageSizeOptions={PAGE_SIZE_OPTIONS}
          hasMore={enrollmentHasMore}
          waiting={enrollmentWaiting}
          onPageSizeChange={onEnrollmentPageSize}
          onPrevious={onEnrollmentPrevious}
          onNext={onEnrollmentNext}
        />
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
                    label="This page"
                    value={`${candidates.length} participant${candidates.length === 1 ? "" : "s"}`}
                    valueId="assignCountValue"
                  />
                }
                search={
                  <ToolbarSearch
                    id="assignSearchInput"
                    label="Search participant or actor"
                    placeholder={SEARCH_NAME_OR_ID_PLACEHOLDER}
                    value={assignSearch}
                    onChange={(event) => onAssignSearch(event.target.value)}
                  />
                }
              />
            }
            scrollProps={{ "aria-label": "Assignable Participant rows, scrollable" }}
            table={
              <DatatableTable caption="Assignable Participants" hidden={candidates.length === 0}>
                <thead>
                  <tr>
                    <SelectHeader
                      id={assignSelectId}
                      selection={selection}
                      pageIds={candidateIds}
                      matchingIds={candidateIds}
                      queryKey={assignQueryKey}
                      noun="participants"
                      onTransition={setSelection}
                    />
                    <StaticHeader label="Participant" colMin="id" />
                    <StaticHeader label="Actor" colMin="compactId" />
                  </tr>
                </thead>
                <tbody>
                  {candidates.map((candidate) => (
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
              candidates.length === 0 ? (
                <DatatableEmpty
                  inset
                  label={assignQuery ? "No matching Participants" : "No assignable Participants"}
                  note={assignQuery
                    ? "Nothing matches the current search. Clear the search to restore the list."
                    : "No assignable Participants are on this page."}
                >
                  {assignQuery ? (
                    <Key size="compact" onClick={() => onAssignSearch("")}>
                      Clear search
                    </Key>
                  ) : null}
                </DatatableEmpty>
              ) : null
            }
            footer={
              <DataTablePagination
                paging="cursor"
                visibleCount={candidates.length}
                pageIndex={candidatePageIndex}
                pageSize={candidatePageSize}
                pageSizeOptions={PAGE_SIZE_OPTIONS}
                hasMore={candidateHasMore}
                waiting={candidateWaiting}
                onPageSizeChange={onCandidatePageSize}
                onPrevious={onCandidatePrevious}
                onNext={onCandidateNext}
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
