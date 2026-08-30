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
import { CeremonyArea, CeremonyUnavailable, CeremonyWait } from "../components/shell/SessionChrome";
import { enrollmentRecordVariant, enrollmentStatusCopy } from "../lib/enrollment-presentation";
import { cx } from "../lib/cx";
import {
  Alert,
  BackKey,
  CompactId,
  DataTablePagination,
  DataTableShell,
  DataTableToolbar,
  datatableColMin,
  EmptyPlate,
  InstantReadout,
  Key,
  KeyGroup,
  OperateArea,
  SortableHeader,
  StateReadout,
  ToolbarReadout,
  ToolbarSearch,
  SEARCH_NAME_OR_ID_PLACEHOLDER,
  useTableController,
} from "../design-system";

type EnrollmentSortKey = "participant" | "enrollment" | "status" | "assigned" | "updated" | "revision";

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
    <OperateArea
      className={cx(
        "workspace-area",
        "work-plane",
        "registry-wall",
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

  const assignAction = candidates.length > 0 ? (
    <div className="datatable-actions" aria-label="Table actions">
      <KeyGroup className="datatable-actions-keys" justify="end">
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
      </KeyGroup>
    </div>
  ) : undefined;

  return (
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
        <table className="datatable-table" hidden={slice.total === 0}>
          <caption className="visually-hidden">Participants</caption>
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
                <tr key={row.enrollment_id} className="datatable-row">
                  <td className="cell-id" {...datatableColMin("id")}>
                    <Link
                      className="datatable-id"
                      to={`/activities/${activityId}/cohorts/${cohortId}/enrollments/${row.enrollment_id}`}
                    >
                      {row.display_label}
                    </Link>
                  </td>
                  <td className="cell-content" {...datatableColMin("compactId")}>
                    <CompactId value={row.enrollment_id} />
                  </td>
                  <td className="cell-state" {...datatableColMin("state")}>
                    <StateReadout
                      variant={record.variant}
                      solid={record.solid}
                      label={enrollmentStatusCopy(row.status)}
                      className="state-cell"
                      labelClassName="state-label"
                    />
                  </td>
                  <td className="cell-content" {...datatableColMin("instant")}>
                    <InstantReadout value={row.assigned_at} />
                  </td>
                  <td className="cell-content" {...datatableColMin("instant")}>
                    <InstantReadout value={row.updated_at} />
                  </td>
                  <td className="cell-content" {...datatableColMin("rev")}>{row.revision}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      }
      empty={
        slice.total === 0 ? (
          <EmptyPlate
            className="datatable-empty"
            inset
            label={query ? "No matching enrollments" : "No Participants assigned"}
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
          </EmptyPlate>
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
