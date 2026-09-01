import { Fragment, useCallback, useEffect, useRef, useState } from "react";
import { EMPTY_SELECTION, isSelected, matchingQueryKey, resolveSelectedIds, toggleRow, type TableSelection } from "../../../../design-system/patterns/tableSelection";
import {
  ActivationMark,
  CommandMenu,
  CompactId,
  DataTablePagination,
  DataTableShell,
  DataTableToolbar,
  DatatableActions,
  DatatableCell,
  DatatableDetailBody,
  DatatableDetailField,
  DatatableDetailKeys,
  DatatableDetailReadouts,
  DatatableDetailRow,
  DatatableEmpty,
  DatatableExpandButton,
  DatatableId,
  DatatableIdCell,
  DatatableRow,
  DatatableTable,
  DisclosureMenu,
  EtchedFrame,
  Inline,
  InstantReadout,
  ItemList,
  Key,
  ReadoutGrid,
  ReadoutGridField,
  ReadoutGridRow,
  ReadoutList,
  RowActionMenu,
  ActionHeader,
  SelectHeader,
  SelectMark,
  SortableHeader,
  StaticHeader,
  StateIndicator,
  TableSelectionBand,
  ToolbarReadout,
  ToolbarSearch,
  SEARCH_ID_PLACEHOLDER,
  recordResultMark,
  useDatatableDetailGutter,
  useTableController,
  type ItemListLoadMoreTrigger,
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
    deadline: new Date(
      Date.UTC(
        2026,
        7,
        28 + Math.floor(index / 48),
        9 + Math.floor((index % 48) / 4),
        (index * 18) % 60,
      ),
    ).toISOString(),
  };
});

const ITEM_LIST_TITLES = [
  "Access Review",
  "Policy Walkthrough",
  "Evidence Intake",
  "Control Mapping",
  "Scope Confirmation",
  "Interview Circuit",
  "Residual Review",
  "Release Check",
  "Field Observation",
  "Attestation Sweep",
  "Exception Ledger",
  "Closeout Briefing",
  "Vendor Recertification",
  "Access Recheck",
  "Boundary Walk",
  "Privilege Census",
  "Key Ceremony",
  "Retention Sweep",
  "Shadow Inventory",
  "Change Freeze Brief",
  "Incident Replay",
  "Control Sampling",
  "Segregation Check",
  "Owner Attestation",
  "Evidence Backfill",
  "Risk Recast",
  "Third-Party Review",
  "Model Card Audit",
  "Prompt Boundary Check",
  "Tool Allowlist Review",
  "Session Isolation Drill",
  "Release Gate Walk",
  "Rollback Rehearsal",
  "Watchdesk Handoff",
  "Archive Integrity Check",
  "Policy Diff Review",
  "Consent Ledger Sweep",
  "Memory Scope Review",
  "Evaluator Recalibration",
  "Outcome Release Brief",
] as const;

const ITEM_LIST_CAMPAIGNS = ITEM_LIST_TITLES.map((title, index) => ({
  id: `cmp-${String(42 + index).padStart(4, "0")}`,
  title,
}));

const ITEM_LIST_PAGE = 8;
export const ITEM_LIST_LOAD_DELAY_MS = 800;

function ItemListSpecimen({
  trigger = "button",
  label,
}: {
  trigger?: ItemListLoadMoreTrigger;
  label: string;
}) {
  const [visible, setVisible] = useState(ITEM_LIST_PAGE);
  const [waiting, setWaiting] = useState(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => () => {
    if (timerRef.current != null) clearTimeout(timerRef.current);
  }, []);

  const items = ITEM_LIST_CAMPAIGNS.slice(0, visible);
  const hasMore = visible < ITEM_LIST_CAMPAIGNS.length;

  const requestMore = useCallback(() => {
    if (timerRef.current != null) return;
    setWaiting(true);
    timerRef.current = setTimeout(() => {
      timerRef.current = null;
      setVisible((count) => Math.min(ITEM_LIST_CAMPAIGNS.length, count + ITEM_LIST_PAGE));
      setWaiting(false);
    }, ITEM_LIST_LOAD_DELAY_MS);
  }, []);

  return (
    <EtchedFrame className="item-list-demo" inset="flush">
      <ItemList
        items={items}
        itemKey={(item) => item.id}
        label={label}
        scroll
        renderItem={(item) => (
          <Inline gap="4" justify="between" wrap={false} align="center">
            <span>{item.title}</span>
            <Key size="compact" ariaLabel={`Open ${item.title}`}>Open</Key>
          </Inline>
        )}
        loadMore={hasMore || waiting ? {
          trigger,
          waiting,
          onLoadMore: requestMore,
          children: "Load more campaigns",
        } : null}
      />
    </EtchedFrame>
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
    <EtchedFrame className="datatable-demo datatable-frame" inset="flush">
      <DataTableShell
        toolbar={
          <DataTableToolbar
            ariaLabel="Table controls"
            actions={(
              <DatatableActions id="dtActionsStrip">
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
              </DatatableActions>
            )}
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
            search={<ToolbarSearch id="dtSearch" label="Search participant ID" placeholder={SEARCH_ID_PLACEHOLDER} value={search} onChange={(event) => { setSearch(event.target.value); resetForQuery(); }} />}
            selection={<TableSelectionBand selection={selection} pageIds={pageIds} matchingIds={matchingIds} noun="enrollments" headerSelectId="dtSelectAll" onClear={() => setSelection(EMPTY_SELECTION)} />}
          />
        }
        scrollProps={{ id: "dtScroll", tabIndex: 0, "aria-label": "Enrollment rows, scrollable" }}
        table={
          <DatatableTable ref={tableRef} id="dtTable" aria-describedby="dtCountValue">
            <thead>
              <tr>
                <SelectHeader id="dtSelectAll" selection={selection} pageIds={pageIds} matchingIds={matchingIds} queryKey={queryKey} noun="enrollments" onTransition={setSelection} />
                <SortableHeader sortKey="id" label="Participant ID" sorts={sorts} onSort={sort} colMin="id" />
                <SortableHeader sortKey="campaign" label="Campaign" sorts={sorts} onSort={sort} colMin="label" />
                <SortableHeader sortKey="stage" label="Stage" sorts={sorts} onSort={sort} colMin="stage" />
                <StaticHeader label="Session state" colMin="state" />
                <SortableHeader sortKey="deadline" label="Deadline" sorts={sorts} onSort={sort} colMin="instant" />
                <SortableHeader sortKey="result" label="Result" sorts={sorts} onSort={sort} colMin="result" />
                <ActionHeader />
              </tr>
            </thead>
            <tbody id="dtBody" ref={tbodyRef}>
              {visible.map((row) => (
                <Fragment key={row.id}>
                  <DatatableRow selected={isSelected(selection, row.id)} expanded={expanded === row.id}>
                    <DatatableCell kind="select">
                      <SelectMark checked={isSelected(selection, row.id)} label={`Select ${row.id}`} onChange={(checked) => setSelection((current: TableSelection) => toggleRow(current, row.id, checked))} />
                    </DatatableCell>
                    <DatatableCell kind="id" colMin="id">
                      <DatatableIdCell
                        expand={(
                          <DatatableExpandButton
                            expanded={expanded === row.id}
                            label={expanded === row.id ? `Collapse enrollment ${row.id}` : `Expand enrollment ${row.id}`}
                            onClick={() => setExpanded(expanded === row.id ? null : row.id)}
                          />
                        )}
                      >
                        <DatatableId onClick={() => announce({ label: "Record", copy: "View is outside this specimen's scope." })}>
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
                    <DatatableCell kind="action" colMin="action">
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
                    </DatatableCell>
                  </DatatableRow>
                  {expanded === row.id ? (
                    <DatatableDetailRow colSpan={8}>
                      <DatatableDetailBody>
                        <DatatableDetailReadouts>
                          <DatatableDetailField term="Attempt">1 OF 2</DatatableDetailField>
                          <DatatableDetailField term="Session duration">—</DatatableDetailField>
                          <DatatableDetailField term="Submission">V2 PRESERVED</DatatableDetailField>
                          <DatatableDetailField term="Evidence">12 ITEMS</DatatableDetailField>
                        </DatatableDetailReadouts>
                        <DatatableDetailKeys>
                          <Key size="compact" onClick={() => announce({ label: "Record", copy: "View is outside this specimen's scope." })}>View record</Key>
                          <Key size="compact" onClick={() => announce({ label: "Record", copy: "Transcript is outside this specimen's scope." })}>Transcript</Key>
                        </DatatableDetailKeys>
                      </DatatableDetailBody>
                    </DatatableDetailRow>
                  ) : null}
                </Fragment>
              ))}
            </tbody>
          </DatatableTable>
        }
        empty={filtered.length === 0 ? <DatatableEmpty inset label="No matching enrollments" note="Clear the stage filter or search field to restore the manifest."><Key size="compact" onClick={() => { setStage("all"); setSearch(""); resetForQuery(); }}>Clear filters</Key></DatatableEmpty> : undefined}
        footer={<DataTablePagination total={filtered.length} startIndex={start} visibleCount={visible.length} page={safePage} pageCount={pageCount} pageSize={pageSize} pageSizeOptions={[8, 16, 32]} onPageSizeChange={(size) => { setPageSize(size); setPage(0); }} onPageChange={setPage} onPrevious={() => setPage(Math.max(0, safePage - 1))} onNext={() => setPage(Math.min(pageCount - 1, safePage + 1))} />}
      />
    </EtchedFrame>
  );
}

const WIDE_ROWS = [
  {
    participant: "Alex Chen",
    campaign: "Access Review 2026",
    stage: "EXAMINATION",
    cohort: "Northbound Q3",
    channel: "Voice + text",
    locale: "en-AU",
    attempt: "01 / 02",
    session: "Live",
    received: "2026-08-28T02:18:00.000Z",
    deadline: "2026-08-29T14:00:00.000Z",
    result: "IN PROGRESS",
    confidence: "0.86",
    reviewer: "Morgan Ellis",
    rev: "3",
  },
  {
    participant: "Priya Nair",
    campaign: "Clinical Documentation",
    stage: "REVIEW",
    cohort: "Night watch",
    channel: "Text",
    locale: "en-GB",
    attempt: "01 / 01",
    session: "Sealed",
    received: "2026-08-27T19:42:00.000Z",
    deadline: "2026-08-30T09:00:00.000Z",
    result: "READY",
    confidence: "0.91",
    reviewer: "Sam Okonkwo",
    rev: "1",
  },
  {
    participant: "Jordan Blake",
    campaign: "Harassment Annual",
    stage: "BRIEFING",
    cohort: "Dockside A",
    channel: "Voice",
    locale: "en-US",
    attempt: "02 / 02",
    session: "Draft",
    received: "2026-08-26T11:05:00.000Z",
    deadline: "2026-09-01T16:30:00.000Z",
    result: "PENDING",
    confidence: "—",
    reviewer: "Unassigned",
    rev: "2",
  },
  {
    participant: "Riley Cho",
    campaign: "Field Hazard Brief",
    stage: "RELEASED",
    cohort: "Pacific rim",
    channel: "Voice + text",
    locale: "en-SG",
    attempt: "01 / 01",
    session: "Released",
    received: "2026-08-25T07:55:00.000Z",
    deadline: "2026-08-28T22:00:00.000Z",
    result: "COMPLETE",
    confidence: "0.74",
    reviewer: "Alex Chen",
    rev: "4",
  },
] as const;

function WideDatatableSpecimen() {
  return (
    <EtchedFrame className="datatable-demo datatable-frame" inset="flush">
      <DataTableShell
        toolbar={
          <DataTableToolbar
            ariaLabel="Wide registry controls"
            readout={<ToolbarReadout label="Showing" value="4 rows · 14 columns" valueId="wideDtCount" />}
          />
        }
        scrollProps={{ tabIndex: 0, role: "region", "aria-label": "Wide registry rows, scrollable" }}
        table={
          <DatatableTable caption="Wide registry">
            <thead>
              <tr>
                <StaticHeader label="Participant" colMin="id" />
                <StaticHeader label="Campaign" colMin="label" />
                <StaticHeader label="Stage" colMin="stage" />
                <StaticHeader label="Cohort" colMin="label" />
                <StaticHeader label="Channel" colMin="label" />
                <StaticHeader label="Locale" colMin="label" />
                <StaticHeader label="Attempt" colMin="count" />
                <StaticHeader label="Session" colMin="state" />
                <StaticHeader label="Received" colMin="instant" />
                <StaticHeader label="Deadline" colMin="instant" />
                <StaticHeader label="Result" colMin="result" />
                <StaticHeader label="Confidence" colMin="confidence" />
                <StaticHeader label="Reviewer" colMin="label" />
                <StaticHeader label="Rev" colMin="rev" />
              </tr>
            </thead>
            <tbody>
              {WIDE_ROWS.map((row) => (
                <DatatableRow key={row.participant}>
                  <DatatableCell kind="id" colMin="id">
                    <DatatableId>{row.participant}</DatatableId>
                  </DatatableCell>
                  <DatatableCell kind="content" colMin="label">{row.campaign}</DatatableCell>
                  <DatatableCell kind="content" colMin="stage">{row.stage}</DatatableCell>
                  <DatatableCell kind="content" colMin="label">{row.cohort}</DatatableCell>
                  <DatatableCell kind="content" colMin="label">{row.channel}</DatatableCell>
                  <DatatableCell kind="content" colMin="label">{row.locale}</DatatableCell>
                  <DatatableCell kind="content" colMin="count">{row.attempt}</DatatableCell>
                  <DatatableCell kind="content" colMin="state">{row.session}</DatatableCell>
                  <DatatableCell kind="content" colMin="instant">
                    <InstantReadout value={row.received} />
                  </DatatableCell>
                  <DatatableCell kind="content" colMin="instant">
                    <InstantReadout value={row.deadline} />
                  </DatatableCell>
                  <DatatableCell kind="content" colMin="result">{row.result}</DatatableCell>
                  <DatatableCell kind="content" colMin="confidence">{row.confidence}</DatatableCell>
                  <DatatableCell kind="content" colMin="label">{row.reviewer}</DatatableCell>
                  <DatatableCell kind="content" colMin="rev">{row.rev}</DatatableCell>
                </DatatableRow>
              ))}
            </tbody>
          </DatatableTable>
        }
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
      <GallerySection id="readout" title="Readout rows" note="Rail grammar is a dim microlabel over a mono value. Horizon rows (assignment and destination plates) use teal labels and Bright Text titles.">
        <div className="readout-demo"><ReadoutList rows={[{ term: "Session ID", value: "FXA-7C19-2A07" }, { term: "Participant ID", value: "CND-8842-19" }, { term: "Protocol", value: "V7.3.1" }]} /><span className="spec-tag">.readout-stack &gt; .readout · dt/dd</span></div>
        <div className="readout-demo"><ReadoutList tone="horizon" rows={[{ term: "Purpose", value: "Create and resume Assessment Campaign drafts.", emphasis: "title" }, { term: "Availability", value: "Available" }]} /><span className="spec-tag">tone=horizon · emphasis=title</span></div>
      </GallerySection>
      <GallerySection id="readout-grid" title="Readout grid" note="Aligned instrument data for records and configuration plates. Every row uses the same named column count; fields span tracks by meaning, so hairline divisions remain continuous.">
        <Spec wide tag="ReadoutGrid · ReadoutGridRow · ReadoutGridField · columns / span">
          <ReadoutGrid label="Campaign record specimen">
            <ReadoutGridRow label="Campaign summary"><ReadoutGridField term="Campaign" span={3}>CMP-0044 / Access Review</ReadoutGridField><ReadoutGridField term="Enrollments">38</ReadoutGridField><ReadoutGridField term="Activation" span={2}><ActivationMark frozen={false} placement="grid" /></ReadoutGridField></ReadoutGridRow>
            <ReadoutGridRow label="Campaign configuration"><ReadoutGridField term="Harness">GOVERNED-AUDIT-01</ReadoutGridField><ReadoutGridField term="Agent identity">EXAMINER-STRUCT</ReadoutGridField><ReadoutGridField term="Session limit">60:00</ReadoutGridField><ReadoutGridField term="Time warning">10:00</ReadoutGridField><ReadoutGridField term="Max attempts">1</ReadoutGridField><ReadoutGridField term="Cooldown">48H</ReadoutGridField></ReadoutGridRow>
          </ReadoutGrid>
        </Spec>
      </GallerySection>
      <GallerySection id="compact-id" title="Compact ID" note="Center-truncated registry identifiers keep the head and tail. Hover opens a copyable value plaque; pass tabbable for focus-visible plaque in standalone surfaces. Dense registry tables omit per-cell tab stops because assistive technology already hears the full value.">
        <div className="spec-row">
          <Spec tag="CompactId · truncated · value plaque">
            <CompactId tabbable value="a1000000-0000-4000-8000-000000000007" />
          </Spec>
          <Spec tag="CompactId · fits · no plaque">
            <CompactId value="solo" />
          </Spec>
          <Spec tag="CompactId · explicit display">
            <CompactId tabbable value="GOVERNED-AUDIT-01" display="GOV…01" />
          </Spec>
        </div>
      </GallerySection>
      <GallerySection
        id="item-list"
        title="Item list"
        note="Generic record rows with custom content from renderItem. Nested overflow is intentional. Load more has two triggers: a trailing key, or auto-request when the nested scrollport reaches its end. Deck demos delayed waiting so the occupied key and WaitPanel stay inspectable."
      >
        <Spec wide tag="trigger=button · Load more key inside named nested scroll">
          <ItemListSpecimen label="Campaigns" />
        </Spec>
        <Spec wide tag="trigger=end · sentinel at nested scroll end">
          <ItemListSpecimen trigger="end" label="End-paged campaigns" />
        </Spec>
      </GallerySection>
      <GallerySection id="datatable" title="Datatable" note="The canonical manifest grammar: one shared 18px inline gutter across toolbar, table, expanded detail, and pagination; a persistent action bar; compact selection band; multi-column sort; teal row selection; expandable row detail; and pagination controls.">
        <Spec wide tag=".datatable-frame + .datatable · shared 18px gutter · full-bleed detail · page then all-matching selection"><DatatableSpecimen announce={announce} /></Spec>
      </GallerySection>
      <GallerySection id="datatable-scroll" title="Datatable scroll" note="Named column floors (`data-col-min`) keep headers and typical values from crushing. When the floors no longer fit the scrollport, `.datatable-scroll` takes horizontal overflow — the etched frame does not.">
        <Spec wide tag="14 columns · data-col-min floors · named scroll region"><WideDatatableSpecimen /></Spec>
      </GallerySection>
    </>
  );
}
