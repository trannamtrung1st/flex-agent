import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useProductionApi } from "../api/production-api";
import {
  createEnrollmentIdempotencyKey,
  createProductionEnrollmentClient,
  enrollmentFailureCopy,
  enrollmentOutcomeCopy,
  type EnrollmentCandidateV1,
  type EnrollmentSummaryV1,
} from "../api/production-enrollment";
import { CeremonyArea, CeremonyEmpty } from "../components/shell/SessionChrome";
import { cx } from "../lib/cx";
import {
  Alert,
  BackKey,
  DataTablePagination,
  DataTableShell,
  DataTableToolbar,
  EmptyPlate,
  Key,
  OperateArea,
  SortableHeader,
  StateReadout,
  ToolbarReadout,
  ToolbarSearch,
  WaitPanel,
  useTableController,
} from "../design-system";

type EnrollmentSortKey = "participant" | "status";

export function ProductionEnrollmentPage() {
  const { activityId = "", cohortId = "" } = useParams();
  const { fetchJson } = useProductionApi();
  const client = useMemo(() => createProductionEnrollmentClient(fetchJson), [fetchJson]);
  const [enrollments, setEnrollments] = useState<EnrollmentSummaryV1[] | null>(null);
  const [candidates, setCandidates] = useState<EnrollmentCandidateV1[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [candidateError, setCandidateError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  useEffect(() => {
    const signal = { cancelled: false };
    void client.listEnrollments(activityId, cohortId)
      .then((page) => {
        if (signal.cancelled) return;
        setEnrollments(page.items);
        setError(null);
      })
      .catch((caught: unknown) => {
        if (!signal.cancelled) {
          setError(enrollmentFailureCopy(caught, "Participants are not available."));
        }
      });
    void client.listCandidates(activityId, cohortId)
      .then((options) => {
        if (signal.cancelled) return;
        setCandidates(options.items);
        setCandidateError(null);
      })
      .catch((caught: unknown) => {
        if (!signal.cancelled) {
          setCandidateError(enrollmentFailureCopy(caught, "Assignable Participants are not available."));
        }
      });
    return () => {
      signal.cancelled = true;
    };
  }, [activityId, client, cohortId]);

  function assign(candidate: EnrollmentCandidateV1) {
    setPending(true);
    void client.assign(activityId, cohortId, candidate.actor_id, createEnrollmentIdempotencyKey())
      .then((outcome) => {
        if (!outcome.succeeded) {
          setError(enrollmentOutcomeCopy(outcome.outcome_code, "Assignment did not complete."));
          return;
        }
        return Promise.allSettled([
          client.listEnrollments(activityId, cohortId),
          client.listCandidates(activityId, cohortId),
        ]).then(([enrollmentRefresh, candidateRefresh]) => {
          if (enrollmentRefresh.status === "fulfilled") {
            setEnrollments(enrollmentRefresh.value.items);
            setError(null);
          } else {
            setError(enrollmentFailureCopy(enrollmentRefresh.reason, "The assigned list could not be refreshed."));
          }
          if (candidateRefresh.status === "fulfilled") {
            setCandidates(candidateRefresh.value.items);
            setCandidateError(null);
          } else {
            setCandidates([]);
            setCandidateError(enrollmentFailureCopy(candidateRefresh.reason, "Assignable Participants are not available."));
          }
        });
      })
      .catch((caught: unknown) => {
        setError(enrollmentFailureCopy(caught, "Assignment did not complete."));
      })
      .finally(() => setPending(false));
  }

  if (error && enrollments === null) {
    return (
      <CeremonyArea label="Participants unavailable" title="Participants unavailable" danger>
        <CeremonyEmpty note={error}>
          <Key variant="open" to={`/activities/${activityId}/setup`}>Return to setup</Key>
        </CeremonyEmpty>
      </CeremonyArea>
    );
  }

  if (enrollments === null) {
    return (
      <CeremonyArea label="Participants" title="Participants">
        <WaitPanel label="Loading Participants…" />
      </CeremonyArea>
    );
  }

  return (
    <OperateArea
      className={cx(
        "workspace-area",
        "work-plane",
        "registry-wall",
        enrollments.length === 0 && "registry-wall--empty",
        enrollments.length > 0 && enrollments.length <= 4 && "registry-wall--hug",
      )}
      frameClassName="datatable-frame registry-frame"
      frameInset="flush"
      label="Participants"
      title="Participants"
      description="Assign a currently eligible Participant. Duplicate assignment and conflicts stay on the server."
      back={<BackKey to={`/activities/${activityId}/setup`} label="Setup" />}
      context={error || candidateError ? (
        <>
          {error ? <Alert variant="danger" title="Could not update Participants">{error}</Alert> : null}
          {candidateError ? <Alert variant="danger" title="Assignable Participants unavailable">{candidateError}</Alert> : null}
        </>
      ) : null}
    >
      <EnrollmentRegistry
        activityId={activityId}
        cohortId={cohortId}
        rows={enrollments}
        candidates={candidates}
        pending={pending}
        onAssign={assign}
      />
    </OperateArea>
  );
}

function EnrollmentRegistry({
  activityId,
  cohortId,
  rows,
  candidates,
  pending,
  onAssign,
}: {
  activityId: string;
  cohortId: string;
  rows: readonly EnrollmentSummaryV1[];
  candidates: readonly EnrollmentCandidateV1[];
  pending: boolean;
  onAssign: (candidate: EnrollmentCandidateV1) => void;
}) {
  const [search, setSearch] = useState("");
  const [sorts, setSorts] = useState<{ key: EnrollmentSortKey; dir: "asc" | "desc" }[]>([{ key: "participant", dir: "asc" }]);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(16);
  const query = search.trim().toLowerCase();
  const match = useCallback(
    (row: EnrollmentSummaryV1) => {
      if (!query) return true;
      return row.display_label.toLowerCase().includes(query) || row.status.toLowerCase().includes(query);
    },
    [query],
  );
  const getSortValue = useCallback((row: EnrollmentSummaryV1, key: EnrollmentSortKey) => {
    return key === "status" ? row.status.toLowerCase() : row.display_label.toLowerCase();
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

  return (
    <DataTableShell
      toolbar={
        <DataTableToolbar
          ariaLabel="Participant registry controls"
          actions={
            candidates.length > 0 ? (
              <div className="registry-assign-keys">
                {candidates.map((candidate) => (
                  <Key
                    key={candidate.actor_id}
                    variant="quiet"
                    size="compact"
                    disabled={pending}
                    onClick={() => onAssign(candidate)}
                  >
                    Assign {candidate.display_label}
                  </Key>
                ))}
              </div>
            ) : undefined
          }
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
              label="Search participant or status"
              placeholder="SEARCH NAME"
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
        <table className="datatable-table datatable-table--fit" hidden={slice.total === 0}>
          <caption className="visually-hidden">Participants</caption>
          <thead>
            <tr>
              <SortableHeader sortKey="participant" sorts={sorts} onSort={handleSort} label="Participant" />
              <SortableHeader sortKey="status" sorts={sorts} onSort={handleSort} label="Record" />
            </tr>
          </thead>
          <tbody>
            {slice.pageRows.map((row) => (
              <tr key={row.enrollment_id} className="datatable-row">
                <td className="cell-id">
                  <Link
                    className="datatable-id"
                    to={`/activities/${activityId}/cohorts/${cohortId}/enrollments/${row.enrollment_id}`}
                  >
                    {row.display_label}
                  </Link>
                </td>
                <td className="cell-content">
                  <StateReadout
                    variant="rest"
                    label={row.status}
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
            label="No Participants assigned"
            note={query ? "No loaded enrollments match this search." : "Assign an eligible Participant to this cohort."}
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
