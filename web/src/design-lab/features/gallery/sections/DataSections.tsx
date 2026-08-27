import { Fragment, useRef, useState } from "react";
import { EMPTY_SELECTION, isSelected, matchingQueryKey, resolveSelectedIds, toggleRow, type TableSelection } from "../../../../design-system/patterns/tableSelection";
import {
  ActivationMark,
  ChevronGlyph,
  CommandMenu,
  DataTablePagination,
  DataTableShell,
  DataTableToolbar,
  DisclosureMenu,
  EmptyPlate,
  EtchedFrame,
  HeaderSelectionControl,
  Key,
  ReadoutGrid,
  ReadoutGridField,
  ReadoutGridRow,
  ReadoutList,
  RowActionMenu,
  SelectMark,
  SortableHeader,
  StateIndicator,
  TableSelectionBand,
  ToolbarReadout,
  ToolbarSearch,
  recordResultMark,
  useDatatableDetailGutter,
  useTableController,
  type SortDirection,
  type TableAction,
} from "../../../components";
import { GallerySection, Spec } from "./GallerySection";

type Stage = "BRIEFING" | "SUBMISSION" | "EXAMINATION" | "REVIEW" | "RELEASED";
type Row = {
  id: string;
  campaign: string;
  stage: Stage;
  result: string;
  deadline: string;
};
type SortKey = "id" | "campaign" | "stage" | "deadline" | "result";

const stages: Stage[] = ["BRIEFING", "SUBMISSION", "EXAMINATION", "REVIEW", "RELEASED"];
const rows: Row[] = Array.from({ length: 100 }, (_, index) => {
  const stage = stages[(index * 7 + 3) % stages.length];
  return {
    id: `P-${3114 + index}`,
    campaign: `CMP-${String(42 + (index % 5)).padStart(4, "0")}`,
    stage,
    result: stage === "RELEASED" ? "COMPLETE" : stage === "EXAMINATION" ? ["READY", "IN PROGRESS", "LIVE"][index % 3] : "PENDING",
    deadline: `2026-08-${String(28 + Math.floor(index / 48)).padStart(2, "0")}  ${String(9 + Math.floor((index % 48) / 4)).padStart(2, "0")}:${String((index * 18) % 60).padStart(2, "0")}`,
  };
});

function DatatableDetailRow({
  colSpan,
  onOpen,
  onOutside,
}: {
  colSpan: number;
  onOpen: () => void;
  onOutside: () => void;
}) {
  return (
    <tr className="datatable-detail">
      <td colSpan={colSpan}>
        <div className="datatable-detail-cut is-revealing">
          <div className="datatable-detail-plate">
            <div className="datatable-detail-body">
              <dl className="datatable-detail-readouts">
                <div className="datatable-detail-field">
                  <dt>Attempt</dt>
                  <dd>1 OF 2</dd>
                </div>
                <div className="datatable-detail-field">
                  <dt>Session duration</dt>
                  <dd>—</dd>
                </div>
                <div className="datatable-detail-field">
                  <dt>Submission</dt>
                  <dd>V2 PRESERVED</dd>
                </div>
                <div className="datatable-detail-field">
                  <dt>Evidence</dt>
                  <dd>12 ITEMS</dd>
                </div>
              </dl>
              <div className="datatable-detail-keys">
                <Key size="compact" onClick={onOpen}>View record</Key>
                <Key size="compact" onClick={onOutside}>Transcript</Key>
              </div>
            </div>
          </div>
        </div>
      </td>
    </tr>
  );
}

function DatatableSpecimen({
  announce,
}: {
  announce: (notice: { label: string; copy: string; attention?: boolean }) => void;
}) {
  const [stage, setStage] = useState<string>("all");
  const [search, setSearch] = useState("");
  const [sorts, setSorts] = useState<Array<{ key: SortKey; dir: SortDirection }>>([{ key: "deadline", dir: "asc" }]);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(8);
  const [selection, setSelection] = useState<TableSelection>(EMPTY_SELECTION);
  const [moreOpen, setMoreOpen] = useState(false);
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const [expanded, setExpanded] = useState<string | null>(null);
  const tableRef = useRef<HTMLTableElement>(null);
  const tbodyRef = useRef<HTMLTableSectionElement>(null);

  const query = search.trim().toUpperCase();
  const slice = useTableController({
    rows,
    match: (row) => (stage === "all" || row.stage === stage) && (!query || row.id.includes(query)),
    sorts,
    page,
    pageSize,
    getSortValue: (row, key) => row[key],
  });
  const filtered = slice.visibleRows;
  const visible = slice.pageRows;
  const pageCount = slice.pageCount;
  const safePage = slice.page;
  const start = slice.startIdx;

  useDatatableDetailGutter({
    tbodyRef,
    tableRef,
    expandedId: expanded,
    dependency: visible,
  });

  const matchingIds = filtered.map((row) => row.id);
  const pageIds = visible.map((row) => row.id);
  const selectedIds = resolveSelectedIds(selection, matchingIds);
  const selectedRows = filtered.filter((row) => selectedIds.includes(row.id));
  const queryKey = matchingQueryKey({ search, stage });

  const deleteAction: TableAction<Row> = {
    id: "delete",
    label: "Delete",
    kind: "destructive",
    placement: "overflow",
    eligibility: (records) => records.every((row) => row.result === "COMPLETE")
      ? { allowed: true }
      : { allowed: false, reason: "Incomplete enrollments are retained in this specimen" },
    run: () => ({ ok: true }),
  };
  const rowActions: TableAction<Row>[] = [
    {
      id: "view",
      label: "View record",
      kind: "standard",
      placement: "overflow",
      surfaces: ["row"],
      eligibility: () => ({ allowed: true }),
      run: () => ({ ok: true }),
    },
    {
      id: "transcript",
      label: "Transcript",
      kind: "standard",
      placement: "overflow",
      surfaces: ["row"],
      eligibility: () => ({ allowed: true }),
      run: () => ({ ok: true }),
    },
    { ...deleteAction, surfaces: ["row", "bulk"] },
  ];
  const menuEntries = [{ type: "action" as const, action: deleteAction }];

  const sort = (key: SortKey) => {
    setSorts((current) => {
      const index = current.findIndex((item) => item.key === key);
      if (index < 0) return [...current, { key, dir: "asc" }];
      if (current[index].dir === "asc") return current.map((item, i) => i === index ? { ...item, dir: "desc" as const } : item);
      const next = current.filter((_, i) => i !== index);
      return next.length ? next : [{ key: "deadline", dir: "asc" }];
    });
  };

  const resetForQuery = () => {
    setPage(0);
    setSelection(EMPTY_SELECTION);
    setExpanded(null);
    setOpenMenuId(null);
  };

  return (
    <EtchedFrame className="datatable-demo datatable-frame">
      <DataTableShell
        toolbar={
          <DataTableToolbar
            ariaLabel="Table controls"
            actions={
              <div className="datatable-actions" id="dtActionsStrip" aria-label="Table actions">
                <div className="datatable-actions-keys">
                  <Key id="create" size="compact" onClick={() => announce({ label: "Create", copy: "Gallery-only create action demonstrated." })}>Create</Key>
                  <Key id="dtBulkKey" size="compact" disabled={!selectedRows.length} disabledReason="Select one or more enrollments." onClick={() => announce({ label: "Export", copy: `${selectedRows.length} enrollments exported.` })}>Export</Key>
                  <Key id="dtDownloadKey" size="compact" disabled={!selectedRows.length || selectedRows.some((row) => row.result !== "COMPLETE")} disabledReason={!selectedRows.length ? "Select one or more enrollments." : "Selected enrollments must be complete."}>Download</Key>
                  <CommandMenu
                    open={moreOpen}
                    onOpenChange={setMoreOpen}
                    triggerLabel="More actions"
                    triggerCaption="More"
                    triggerId="dtMoreKey"
                    records={selectedRows}
                    entries={menuEntries}
                    onChoose={() => announce({ label: "Delete", copy: "Delete ceremony demonstrated." })}
                    triggerDisabled={!selectedRows.length}
                    triggerDisabledReason="Select one or more enrollments."
                  />
                </div>
              </div>
            }
            leading={
              <DisclosureMenu
                label="Filter"
                value={stage === "all" ? "All stages" : stage}
                selectedId={stage}
                ariaLabel="Stage filter"
                options={[{ id: "all", label: "All stages" }, ...stages.map((value) => ({ id: value, label: value }))]}
                onSelect={(value) => { setStage(value); resetForQuery(); }}
              />
            }
            readout={<ToolbarReadout label="Showing" value={`${filtered.length} enrollment${filtered.length === 1 ? "" : "s"}`} valueId="dtCountValue" />}
            search={<ToolbarSearch id="dtSearch" label="Search participant ID" placeholder="Search ID" value={search} onChange={(event) => { setSearch(event.target.value); resetForQuery(); }} />}
            selection={<TableSelectionBand selection={selection} pageIds={pageIds} matchingIds={matchingIds} noun="enrollments" headerSelectId="dtSelectAll" onClear={() => setSelection(EMPTY_SELECTION)} />}
          />
        }
        scrollProps={{ id: "dtScroll", tabIndex: 0, "aria-label": "Enrollment rows, scrollable" }}
        table={
          <table ref={tableRef} className="datatable-table" id="dtTable" aria-describedby="dtCountValue">
            <thead>
              <tr>
                <th scope="col" className="col-select">
                  <HeaderSelectionControl id="dtSelectAll" selection={selection} pageIds={pageIds} matchingIds={matchingIds} queryKey={queryKey} noun="enrollments" onTransition={setSelection} />
                </th>
                <SortableHeader sortKey="id" label="Participant ID" sorts={sorts} onSort={sort} />
                <SortableHeader sortKey="campaign" label="Campaign" sorts={sorts} onSort={sort} />
                <SortableHeader sortKey="stage" label="Stage" sorts={sorts} onSort={sort} />
                <th scope="col" className="col-state">Session state</th>
                <SortableHeader sortKey="deadline" label="Deadline" sorts={sorts} onSort={sort} />
                <SortableHeader sortKey="result" label="Result" sorts={sorts} onSort={sort} />
                <th scope="col"><span className="visually-hidden">Actions</span></th>
              </tr>
            </thead>
            <tbody id="dtBody" ref={tbodyRef}>
              {visible.map((row) => (
                <Fragment key={row.id}>
                  <tr className={`datatable-row${isSelected(selection, row.id) ? " is-selected" : ""}${expanded === row.id ? " is-expanded" : ""}`}>
                    <td className="cell-select"><SelectMark checked={isSelected(selection, row.id)} label={`Select ${row.id}`} onChange={(checked) => setSelection((current: TableSelection) => toggleRow(current, row.id, checked))} /></td>
                    <td className="cell-id"><div className="datatable-id-cell"><button className={`icon-button command-menu-trigger command-menu-trigger--icon${expanded === row.id ? " is-open" : ""}`} type="button" aria-label={`${expanded === row.id ? "Collapse" : "Expand"} enrollment ${row.id}`} aria-expanded={expanded === row.id} onClick={() => setExpanded(expanded === row.id ? null : row.id)}><ChevronGlyph /></button><button className="datatable-id" type="button" onClick={() => announce({ label: "Record", copy: "View is outside this specimen's scope." })}>{row.id}</button></div></td>
                    <td className="cell-content">{row.campaign}</td>
                    <td className="cell-content">{row.stage}</td>
                    <td className="cell-state">{recordResultMark(row.result)}</td>
                    <td className="cell-content">{row.deadline}</td>
                    <td className="cell-result cell-content">{row.result}</td>
                    <td className="col-action">
                      <RowActionMenu
                        open={openMenuId === row.id}
                        onOpenChange={(open) => setOpenMenuId(open ? row.id : null)}
                        label={`Actions for ${row.id}`}
                        records={[row]}
                        actions={rowActions}
                        onChoose={(action) => {
                          if (action.id === "view") {
                            announce({ label: "Record", copy: "View is outside this specimen's scope." });
                            return;
                          }
                          if (action.id === "transcript") {
                            announce({ label: "Record", copy: "Transcript is outside this specimen's scope." });
                            return;
                          }
                          announce({ label: "Delete", copy: "Delete ceremony demonstrated." });
                        }}
                      />
                    </td>
                  </tr>
                  {expanded === row.id ? (
                    <DatatableDetailRow
                      colSpan={8}
                      onOpen={() => announce({ label: "Record", copy: "View is outside this specimen's scope." })}
                      onOutside={() => announce({ label: "Record", copy: "Transcript is outside this specimen's scope." })}
                    />
                  ) : null}
                </Fragment>
              ))}
            </tbody>
          </table>
        }
        empty={filtered.length === 0 ? <EmptyPlate className="datatable-empty" label="No matching enrollments" note="Clear the stage filter or search field to restore the manifest."><Key size="compact" onClick={() => { setStage("all"); setSearch(""); resetForQuery(); }}>Clear filters</Key></EmptyPlate> : undefined}
        footer={<DataTablePagination total={filtered.length} startIndex={start} visibleCount={visible.length} page={safePage} pageCount={pageCount} pageSize={pageSize} pageSizeOptions={[8, 16, 32]} onPageSizeChange={(size) => { setPageSize(size); setPage(0); }} onPageChange={setPage} onPrevious={() => setPage(Math.max(0, safePage - 1))} onNext={() => setPage(Math.min(pageCount - 1, safePage + 1))} />}
      />
    </EtchedFrame>
  );
}

export function DataSections({
  announce,
}: {
  announce: (notice: { label: string; copy: string; attention?: boolean }) => void;
}) {
  return (
    <>
      <GallerySection id="marks" title="Instrument marks" note="State is a mark changing, never a colored blob. Every glyph is authored geometry in the two color voices.">
        <div className="spec-row">
          {([
            { key: "rest", variant: "rest", solid: false, tag: ".state-node · rest" },
            { key: "live", variant: "live", solid: false, tag: "--live" },
            { key: "live-solid", variant: "live", solid: true, tag: "--live-solid" },
            { key: "sealed", variant: "sealed", solid: false, tag: "--sealed" },
            { key: "sealed-solid", variant: "sealed", solid: true, tag: "--sealed-solid" },
            { key: "dim", variant: "dim", solid: false, tag: "--dim" },
          ] as const).map((specimen) => (
            <Spec key={specimen.key} tag={specimen.tag}>
              <StateIndicator variant={specimen.variant} solid={specimen.solid} />
            </Spec>
          ))}
        </div>
      </GallerySection>
      <GallerySection id="select-mark" title="Select mark" note="Teal selection marks for row and header checkboxes. Four header states — none, partial, page, and matching — use explicit modifiers; do not rely on :checked alone to distinguish page from matching.">
        <div className="spec-row">{["", " select-mark--partial is-indeterminate", " select-mark--page", " select-mark--matching"].map((suffix, index) => <Spec key={suffix || "none"} tag={[".select-mark · none", ".select-mark--partial", ".select-mark--page", ".select-mark--matching"][index]}><span className={`select-mark${suffix}`} aria-hidden="true" /></Spec>)}</div>
      </GallerySection>
      <GallerySection id="readout" title="Readout rows" note="The rail's reading grammar: dim microlabel over mono value, hairline dividers between rows.">
        <div className="readout-demo"><ReadoutList rows={[{ term: "Session ID", value: "FXA-7C19-2A07" }, { term: "Participant ID", value: "CND-8842-19" }, { term: "Protocol", value: "V7.3.1" }]} /><span className="spec-tag">.readout-stack &gt; .readout · dt/dd</span></div>
      </GallerySection>
      <GallerySection id="readout-grid" title="Readout grid" note="Aligned instrument data for records and configuration plates. Every row uses the same named column count; fields span tracks by meaning, so hairline divisions remain continuous.">
        <Spec wide tag="ReadoutGrid · ReadoutGridRow · ReadoutGridField · columns / span">
          <ReadoutGrid label="Campaign record specimen">
            <ReadoutGridRow label="Campaign summary"><ReadoutGridField term="Campaign" span={3}>CMP-0044 / Access Review</ReadoutGridField><ReadoutGridField term="Enrollments">38</ReadoutGridField><ReadoutGridField term="Activation" span={2}><ActivationMark frozen={false} className="readout-grid-state" /></ReadoutGridField></ReadoutGridRow>
            <ReadoutGridRow label="Campaign configuration"><ReadoutGridField term="Harness">GOVERNED-AUDIT-01</ReadoutGridField><ReadoutGridField term="Agent identity">EXAMINER-STRUCT</ReadoutGridField><ReadoutGridField term="Session limit">60:00</ReadoutGridField><ReadoutGridField term="Time warning">10:00</ReadoutGridField><ReadoutGridField term="Max attempts">1</ReadoutGridField><ReadoutGridField term="Cooldown">48H</ReadoutGridField></ReadoutGridRow>
          </ReadoutGrid>
        </Spec>
      </GallerySection>
      <GallerySection id="datatable" title="Datatable" note="The canonical manifest grammar: one shared 18px inline gutter across toolbar, table, expanded detail, and pagination; a persistent action bar; compact selection band; multi-column sort; teal row selection; expandable row detail; and pagination controls.">
        <Spec wide tag=".datatable-frame + .datatable · shared 18px gutter · full-bleed detail · page then all-matching selection"><DatatableSpecimen announce={announce} /></Spec>
      </GallerySection>
    </>
  );
}
