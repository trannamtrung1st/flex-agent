import { Fragment, useRef, type ReactNode } from "react";
import { DisclosureMenu } from "../../../design-system/components/select";
import { Key } from "../../../design-system/components/keys";
import { recordResultMark } from "../state/recordResultMark";
import {
  DataTablePagination,
  DataTableShell,
  DataTableToolbar,
  DatatableCell,
  DatatableEmpty,
  DatatableId,
  DatatableRow,
  DatatableTable,
  SelectHeader,
  SortableHeader,
  StaticHeader,
  ToolbarReadout,
  ToolbarSearch,
  useTableController,
} from "../../../design-system/components/datatable";
import {
  DatatableDetailBody,
  DatatableDetailField,
  DatatableDetailKeys,
  DatatableDetailReadouts,
  DatatableDetailRow,
  DatatableExpandButton,
  DatatableIdCell,
  useDatatableDetailGutter,
} from "../datatable";
import { InstantReadout } from "../../../design-system/components/temporal";
import { SEARCH_ID_PLACEHOLDER } from "../../../content/fieldCopy";
import { pad } from "../../../lib/format";
import type { DataTableState, EnrollmentRow, SortKey } from "../../data/types";
import { enrollmentMatches, enrollmentQueryKey, enrollmentSortValue, matchingEnrollmentIds, sortAndFilter } from "./tableLogic";
import { EMPTY_SELECTION, isSelected, isSelectionEmpty, normalizeSelection, toggleRow } from "../../../design-system/patterns/tableSelection";
import { SelectMark, TableSelectionBand } from "../../../design-system/patterns/TableActions";

const SORT_LABELS: Record<SortKey, string> = {
  id: "ID",
  campaign: "Campaign",
  stage: "Stage",
  deadline: "Deadline",
  result: "Result",
};

const ENROLLMENT_COL_MIN: Record<SortKey, "id" | "label" | "stage" | "instant" | "result"> = {
  id: "id",
  campaign: "label",
  stage: "stage",
  deadline: "instant",
  result: "result",
};

function enrollmentDetailId(rowId: string) {
  return `enrollment-detail-${rowId}`;
}

function rowClass(row: EnrollmentRow) {
  return `${row.result === "LIVE" || row.result === "IN PROGRESS" ? " is-live" : ""}${
    row.result === "COMPLETE" ? " is-complete" : ""
  }`;
}

export function DataTable({
  rows,
  state,
  setState,
  announce,
  stages,
  singleSort = false,
  emptyAction,
  onOpenRecord,
}: {
  rows: EnrollmentRow[];
  state: DataTableState;
  setState: (patch: Partial<DataTableState> | ((prev: DataTableState) => DataTableState)) => void;
  announce: (msg: string) => void;
  stages: string[];
  singleSort?: boolean;
  emptyAction?: ReactNode;
  onOpenRecord?: (row: EnrollmentRow) => void;
}) {
  const slice = useTableController({
    rows,
    match: (row) => enrollmentMatches(row, state),
    sorts: state.sorts,
    page: state.page,
    pageSize: state.pageSize,
    getSortValue: enrollmentSortValue,
  });
  const tableRef = useRef<HTMLTableElement>(null);
  const tbodyRef = useRef<HTMLTableSectionElement>(null);
  const headerSelectRef = useRef<HTMLInputElement>(null);

  const queryKey = enrollmentQueryKey(state);
  const matchingIds = matchingEnrollmentIds(rows, state);
  const selection = normalizeSelection(state.selection, matchingIds, queryKey);

  const patch = (next: Partial<DataTableState>) =>
    setState((prev) => ({ ...prev, ...next }));

  const openRecord = (row: EnrollmentRow) => {
    if (onOpenRecord) onOpenRecord(row);
    else announce("Action is outside this prototype's scope.");
  };

  const handleSort = (key: SortKey) => {
    setState((prev) => {
      const idx = prev.sorts.findIndex((s) => s.key === key);
      let sorts = prev.sorts.slice();
      if (!singleSort) {
        if (idx === -1) sorts.push({ key, dir: "asc" });
        else if (sorts[idx].dir === "asc") sorts[idx] = { key, dir: "desc" };
        else sorts.splice(idx, 1);
      } else if (idx === 0 && sorts.length === 1) {
        sorts = sorts[0].dir === "asc" ? [{ key, dir: "desc" }] : [];
      } else {
        sorts = [{ key, dir: "asc" }];
      }
      if (!sorts.length) sorts = [{ key: "deadline", dir: "asc" }];
      return { ...prev, sorts, page: 0, expandedId: null };
    });
    announce("Sorted.");
  };

  useDatatableDetailGutter({
    tbodyRef,
    tableRef,
    expandedId: state.expandedId,
    dependency: slice.pageRows,
  });

  const pageIds = slice.pageRows.map((r) => r.id);

  const filterLabel = state.stageFilter ?? "All stages";

  const clearSelectionIfQueryChanged = (nextStage: string | null, nextSearch: string) => {
    const nextKey = enrollmentQueryKey({ ...state, stageFilter: nextStage, search: nextSearch });
    if (nextKey === queryKey) return state.selection;
    return EMPTY_SELECTION;
  };

  return (
    <DataTableShell
      toolbar={
        <DataTableToolbar
          ariaLabel="Manifest controls"
          leading={
            <FilterMenu
              value={filterLabel}
              stages={stages}
              selected={state.stageFilter}
              onSelect={(stage) => {
                const hadSelection = !isSelectionEmpty(selection, matchingIds);
                const nextSelection = clearSelectionIfQueryChanged(stage, state.search);
                patch({ stageFilter: stage, page: 0, expandedId: null, selection: nextSelection });
                announce(
                  `${stage ? `Filtered to stage ${stage}.` : "Stage filter cleared."}${hadSelection && nextSelection === EMPTY_SELECTION ? " Selection cleared because the enrollment set changed." : ""}`,
                );
              }}
            />
          }
          readout={
            <ToolbarReadout
              label="Showing"
              value={`${slice.total} enrollment${slice.total === 1 ? "" : "s"}`}
              valueId="countValue"
            />
          }
          search={
            <ToolbarSearch
              id="searchInput"
              label="Search participant ID"
              placeholder={SEARCH_ID_PLACEHOLDER}
              value={state.search}
              onChange={(event) => {
                const { value } = event.target;
                const search = value.trim().toUpperCase();
                const hadSelection = !isSelectionEmpty(selection, matchingIds);
                const nextSelection = clearSelectionIfQueryChanged(state.stageFilter, value);
                patch({ search: value, page: 0, expandedId: null, selection: nextSelection });
                if (search && sortAndFilter(rows, { ...state, search: value }).length === 0) {
                  announce(`No enrollments match ${search}.`);
                } else if (hadSelection && nextSelection === EMPTY_SELECTION) {
                  announce("Selection cleared because the enrollment set changed.");
                }
              }}
            />
          }
          selection={
            <TableSelectionBand
              selection={selection}
              pageIds={pageIds}
              matchingIds={matchingIds}
              noun="enrollments"
              headerSelectId="enrollmentSelectAll"
              onClear={() => {
                patch({ selection: EMPTY_SELECTION });
                announce("Selection cleared.");
                headerSelectRef.current?.focus();
              }}
            />
          }
        />
      }
      scrollProps={{ tabIndex: 0, "aria-label": "Enrollment rows, scrollable" }}
      table={
        <DatatableTable
          ref={tableRef}
          caption="Enrollments for the selected campaign"
          hidden={slice.total === 0}
          aria-busy="false"
        >
          <thead>
            <tr>
              <SelectHeader
                ref={headerSelectRef}
                id="enrollmentSelectAll"
                selection={selection}
                pageIds={pageIds}
                matchingIds={matchingIds}
                queryKey={queryKey}
                noun="enrollments"
                onTransition={(next) => {
                  patch({ selection: next });
                  announce("Selection updated.");
                }}
              />
              {(["id", "campaign", "stage"] as SortKey[]).map((key) => (
                <SortableHeader
                  key={key}
                  sortKey={key}
                  sorts={state.sorts}
                  onSort={handleSort}
                  label={SORT_LABELS[key] === "ID" ? "Participant ID" : SORT_LABELS[key]}
                  colMin={ENROLLMENT_COL_MIN[key]}
                />
              ))}
              <StaticHeader label="Session state" colMin="state" />
              <SortableHeader sortKey="deadline" sorts={state.sorts} onSort={handleSort} label="Deadline" colMin="instant" />
              <SortableHeader sortKey="result" sorts={state.sorts} onSort={handleSort} label="Result" colMin="result" />
            </tr>
          </thead>
          <tbody ref={tbodyRef}>
            {slice.pageRows.map((row) => {
              const expanded = row.id === state.expandedId;
              const detailId = enrollmentDetailId(row.id);
              return (
                <Fragment key={row.id}>
                  <PlainRow
                    key={row.id}
                    row={row}
                    expanded={expanded}
                    detailId={detailId}
                    selected={isSelected(selection, row.id)}
                    onSelect={(checked) => {
                      patch({ selection: toggleRow(selection, row.id, checked) });
                    }}
                    onOpen={() => openRecord(row)}
                    onToggleExpand={() => {
                      if (expanded) {
                        patch({ expandedId: null });
                        announce(`Enrollment ${row.id} collapsed.`);
                      } else {
                        patch({ expandedId: row.id });
                        announce(`Enrollment ${row.id} expanded for inspection.`);
                      }
                    }}
                  />
                  {expanded ? (
                    <DetailRow
                      row={row}
                      detailId={detailId}
                      onOpen={() => openRecord(row)}
                      onOutside={() => announce("Action is outside this prototype's scope.")}
                    />
                  ) : null}
                </Fragment>
              );
            })}
          </tbody>
        </DatatableTable>
      }
      empty={
        slice.total === 0 ? (
          <DatatableEmpty
            id="manifestEmpty"
            inset
            label="No matching enrollments"
            note="Nothing in this campaign matches the current filter or search. Clear the search or set the stage filter back to all stages."
          >
            {emptyAction}
          </DatatableEmpty>
        ) : undefined
      }
      footer={
        <DataTablePagination
          total={slice.total}
          startIndex={slice.startIdx}
          visibleCount={slice.pageRows.length}
          page={state.page}
          pageCount={slice.pageCount}
          pageSize={state.pageSize}
          pageSizeOptions={[8, 16, 32, 64]}
          onPageSizeChange={(size) => {
            patch({ pageSize: size, page: 0, expandedId: null });
            announce(`Showing ${pad(size)} rows per page.`);
          }}
          onPageChange={(page) => {
            patch({ page, expandedId: null });
            announce(`Page ${pad(page + 1)} of ${pad(slice.pageCount)}.`);
          }}
          onPrevious={() => {
            patch({ page: state.page - 1, expandedId: null });
            announce("Previous page.");
          }}
          onNext={() => {
            patch({ page: state.page + 1, expandedId: null });
            announce("Next page.");
          }}
        />
      }
    />
  );
}

function FilterMenu({
  value,
  stages,
  selected,
  onSelect,
}: {
  value: string;
  stages: string[];
  selected: string | null;
  onSelect: (stage: string | null) => void;
}) {
  const options = [{ id: "", label: "All stages" }, ...stages.map((s) => ({ id: s, label: s }))];
  return (
    <DisclosureMenu
      keyId="filterKey"
      menuId="filterMenu"
      valueId="filterValue"
      label="Filter:"
      value={value}
      selectedId={selected ?? ""}
      ariaLabel="Filter by stage"
      options={options}
      onSelect={(id) => onSelect(id === "" ? null : id)}
    />
  );
}

function PlainRow({
  row,
  expanded,
  detailId,
  selected,
  onSelect,
  onOpen,
  onToggleExpand,
}: {
  row: EnrollmentRow;
  expanded: boolean;
  detailId: string;
  selected: boolean;
  onSelect: (checked: boolean) => void;
  onOpen: () => void;
  onToggleExpand: () => void;
}) {
  return (
    <DatatableRow
      selected={selected}
      expanded={expanded}
      className={rowClass(row)}
    >
      <DatatableCell kind="select">
        <SelectMark
          checked={selected}
          label={`Select ${row.id}`}
          onChange={onSelect}
        />
      </DatatableCell>
      <DatatableCell kind="id" colMin="id">
        <DatatableIdCell
          expand={(
            <DatatableExpandButton
              expanded={expanded}
              controls={detailId}
              label={expanded ? `Collapse enrollment ${row.id}` : `Expand enrollment ${row.id}`}
              onClick={onToggleExpand}
            />
          )}
        >
          <DatatableId onClick={onOpen}>
            {row.id}
          </DatatableId>
        </DatatableIdCell>
      </DatatableCell>
      <DatatableCell kind="content" colMin="label">{row.campaign}</DatatableCell>
      <DatatableCell kind="content" colMin="stage">{row.stage}</DatatableCell>
      <DatatableCell kind="state" colMin="state">{recordResultMark(row.result)}</DatatableCell>
      <DatatableCell kind="content" colMin="instant">
        <InstantReadout value={row.deadline} />
      </DatatableCell>
      <DatatableCell kind="content" colMin="result">{row.result}</DatatableCell>
    </DatatableRow>
  );
}

function DetailRow({
  row,
  detailId,
  onOpen,
  onOutside,
}: {
  row: EnrollmentRow;
  detailId: string;
  onOpen: () => void;
  onOutside: () => void;
}) {
  return (
    <DatatableDetailRow colSpan={7} id={detailId} plateClassName={rowClass(row)}>
      <DatatableDetailBody>
        <DatatableDetailReadouts>
          <DatatableDetailField term="Attempt">{row.attempt}</DatatableDetailField>
          <DatatableDetailField term="Session duration">{row.duration}</DatatableDetailField>
          <DatatableDetailField term="Submission">{row.submission}</DatatableDetailField>
          <DatatableDetailField term="Evidence">{row.evidence}</DatatableDetailField>
        </DatatableDetailReadouts>
        <DatatableDetailKeys>
          <Key size="compact" onClick={onOpen}>
            View record
          </Key>
          <Key size="compact" onClick={onOutside}>
            Transcript
          </Key>
        </DatatableDetailKeys>
      </DatatableDetailBody>
    </DatatableDetailRow>
  );
}
