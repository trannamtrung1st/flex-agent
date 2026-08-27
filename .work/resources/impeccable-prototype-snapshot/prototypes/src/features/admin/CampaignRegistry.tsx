import { useMemo, useRef, useState } from "react";
import {
  ActionConfirmDialog,
  ActivationMark,
  DataTablePagination,
  DataTableShell,
  DataTableToolbar,
  DisclosureMenu,
  EmptyPlate,
  HeaderSelectionControl,
  Key,
  RowActionMenu,
  SelectMark,
  SortableHeader,
  TableActionBar,
  TableSelectionBand,
  ToolbarReadout,
  ToolbarSearch,
  useTableController,
  type TableAction,
} from "../../components";
import {
  EMPTY_SELECTION,
  isSelected,
  normalizeSelection,
  toggleRow,
} from "../../components/tableSelection";
import { formatDeadline, pad } from "../../lib/format";
import type { Campaign } from "../../data/types";
import type { CampaignRegistrySortKey, CampaignRegistryState, CampaignRegistryRow } from "../../data/types";
import { campaignMatches, campaignQueryKey, campaignSortValue, matchingCampaignIds, sortAndFilterCampaigns } from "./campaignRegistryLogic";

const SORT_LABELS: Record<CampaignRegistrySortKey, string> = {
  campaign: "Campaign",
  activation: "Activation",
  enrollments: "Enrollments",
  deadline: "Cohort deadline",
  updated: "Updated",
};

const ACTIVATION_FILTER_OPTIONS = [
  { id: "all", label: "All campaigns" },
  { id: "draft", label: "Draft" },
  { id: "frozen", label: "Frozen" },
] as const;

export function CampaignRegistry({
  rows,
  campaigns,
  state,
  setState,
  announce,
  onOpen,
  actions,
  onChoose,
  busyActionId,
  confirm,
}: {
  rows: CampaignRegistryRow[];
  campaigns: Campaign[];
  state: CampaignRegistryState;
  setState: (patch: Partial<CampaignRegistryState> | ((prev: CampaignRegistryState) => CampaignRegistryState)) => void;
  announce: (message: string) => void;
  onOpen: (id: string) => void;
  actions: TableAction<Campaign>[];
  onChoose: (action: TableAction<Campaign>, records: Campaign[], trigger: HTMLElement) => void;
  busyActionId: string | null;
  confirm: {
    open: boolean;
    confirmation: { title: string; body: string; commitLabel: string } | null;
    error: string | null;
    waiting: boolean;
    onCancel: () => void;
    onConfirm: () => void;
  };
}) {
  const slice = useTableController({
    rows,
    match: (row) => campaignMatches(row, state),
    sorts: state.sorts,
    page: state.page,
    pageSize: state.pageSize,
    getSortValue: campaignSortValue,
  });
  const queryKey = campaignQueryKey(state);
  const matchingIds = matchingCampaignIds(rows, state);
  const selection = normalizeSelection(state.selection, matchingIds, queryKey);
  const campaignById = useMemo(() => new Map(campaigns.map((campaign) => [campaign.id, campaign])), [campaigns]);
  const selectedRecords = useMemo(
    () =>
      (selection.mode === "explicit" ? selection.ids : matchingIds.filter((id) => !selection.excludedIds.includes(id)))
        .map((id) => campaignById.get(id))
        .filter((campaign): campaign is Campaign => Boolean(campaign)),
    [campaignById, matchingIds, selection],
  );
  const pageIds = slice.pageRows.map((row) => row.id);
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const headerSelectRef = useRef<HTMLInputElement>(null);

  const closeMenus = () => setOpenMenuId(null);

  const patch = (next: Partial<CampaignRegistryState>) => setState((prev) => ({ ...prev, ...next }));

  const handleSort = (key: CampaignRegistrySortKey) => {
    closeMenus();
    setState((prev) => {
      const idx = prev.sorts.findIndex((s) => s.key === key);
      let sorts = prev.sorts.slice();
      if (idx === -1) sorts.push({ key, dir: "asc" });
      else if (sorts[idx].dir === "asc") sorts[idx] = { key, dir: "desc" };
      else sorts.splice(idx, 1);
      if (!sorts.length) sorts = [{ key: "campaign", dir: "asc" }];
      return { ...prev, sorts, page: 0 };
    });
    announce("Sorted.");
  };

  const filterLabel =
    ACTIVATION_FILTER_OPTIONS.find((option) => option.id === state.activationFilter)?.label ??
    ACTIVATION_FILTER_OPTIONS[0].label;

  return (
    <DataTableShell
      toolbar={
        <DataTableToolbar
          ariaLabel="Campaign registry controls"
          actions={
            <TableActionBar
              selection={selection}
              pageIds={pageIds}
              matchingIds={matchingIds}
              noun="campaigns"
              actions={actions}
              records={selectedRecords}
              busyActionId={busyActionId}
              onChoose={(action, trigger) => onChoose(action, selectedRecords, trigger)}
            />
          }
          leading={
            <DisclosureMenu
              keyId="campaignFilterKey"
              menuId="campaignFilterMenu"
              valueId="campaignFilterValue"
              label="Filter:"
              value={filterLabel}
              selectedId={state.activationFilter}
              ariaLabel="Filter by activation state"
              options={[...ACTIVATION_FILTER_OPTIONS]}
              onSelect={(id) => {
                const activationFilter = id === "draft" || id === "frozen" ? id : "all";
                const hadSelection = state.selection.mode === "matching" || (state.selection.mode === "explicit" && state.selection.ids.length > 0);
                patch({ activationFilter, page: 0, selection: hadSelection ? EMPTY_SELECTION : state.selection });
                closeMenus();
                announce(
                  `${
                    activationFilter === "all"
                      ? "Activation filter cleared."
                      : `Filtered to ${activationFilter} campaigns.`
                  }${hadSelection ? " Selection cleared because the campaign set changed." : ""}`,
                );
              }}
            />
          }
          readout={
            <ToolbarReadout
              label="Showing"
              value={`${slice.total} campaign${slice.total === 1 ? "" : "s"}`}
              valueId="campaignCountValue"
            />
          }
          search={
            <ToolbarSearch
              id="campaignSearchInput"
              label="Search campaign ID or name"
              placeholder="SEARCH ID OR NAME"
              value={state.search}
              onChange={(event) => {
                const { value } = event.target;
                const search = value.trim().toUpperCase();
                const hadSelection = state.selection.mode === "matching" || (state.selection.mode === "explicit" && state.selection.ids.length > 0);
                const next = { ...state, search: value, page: 0, selection: hadSelection ? EMPTY_SELECTION : state.selection };
                patch({ search: value, page: 0, selection: next.selection });
                closeMenus();
                if (search && sortAndFilterCampaigns(rows, next).length === 0) {
                  announce(`No campaigns match ${search}.`);
                } else if (hadSelection) {
                  announce("Selection cleared because the campaign set changed.");
                }
              }}
            />
          }
          selection={
            <TableSelectionBand
              selection={selection}
              pageIds={pageIds}
              matchingIds={matchingIds}
              noun="campaigns"
              headerSelectId="campaignSelectAll"
              onClear={() => {
                patch({ selection: EMPTY_SELECTION });
                setOpenMenuId(null);
                announce("Selection cleared.");
                headerSelectRef.current?.focus();
              }}
            />
          }
        />
      }
      scrollProps={{ tabIndex: 0, "aria-label": "Campaign rows, scrollable" }}
      table={
        <table className="datatable-table" hidden={slice.total === 0}>
          <caption className="visually-hidden">Campaign registry</caption>
          <thead>
            <tr>
              <th scope="col" className="col-select">
                <HeaderSelectionControl
                  ref={headerSelectRef}
                  id="campaignSelectAll"
                  selection={selection}
                  pageIds={pageIds}
                  matchingIds={matchingIds}
                  queryKey={queryKey}
                  noun="campaigns"
                  onTransition={(next) => {
                    patch({ selection: next });
                    announce("Selection updated.");
                  }}
                />
              </th>
              {(Object.keys(SORT_LABELS) as CampaignRegistrySortKey[]).map((key) => (
                <SortableHeader
                  key={key}
                  sortKey={key}
                  sorts={state.sorts}
                  onSort={handleSort}
                  label={SORT_LABELS[key]}
                />
              ))}
              <th scope="col" className="col-action"><span className="visually-hidden">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            {slice.pageRows.map((row) => {
              const selected = isSelected(selection, row.id);
              return (
                <tr
                  key={row.id}
                  className={`datatable-row${selected ? " is-selected" : ""}`}
                >
                  <td className="cell-select">
                    <SelectMark
                      checked={selected}
                      label={`Select ${row.id} / ${row.name}`}
                      onChange={(checked) => patch({ selection: toggleRow(selection, row.id, checked) })}
                    />
                  </td>
                  <td className="cell-id">
                    <button
                      type="button"
                      className="datatable-id"
                      onClick={() => onOpen(row.id)}
                    >
                      {row.id} / {row.name}
                    </button>
                  </td>
                  <td className="cell-content">
                    <ActivationMark frozen={row.frozen} compact className="state-cell" labelClassName="state-label" />
                  </td>
                  <td className="cell-content">{pad(row.enrollments, 2)}</td>
                  <td className="cell-content">{row.deadline ? formatDeadline(row.deadline) : "—"}</td>
                  <td className="cell-content">{formatDeadline(row.updatedAt)}</td>
                  <td className="col-action">
                    <RowActionMenu
                      open={openMenuId === row.id}
                      onOpenChange={(open) => setOpenMenuId(open ? row.id : null)}
                      label={`Actions for ${row.id} / ${row.name}`}
                      records={[campaignById.get(row.id)].filter((campaign): campaign is Campaign => Boolean(campaign))}
                      actions={actions}
                      busyActionId={busyActionId}
                      onChoose={(action, trigger) => {
                        const campaign = campaignById.get(row.id);
                        if (campaign) onChoose(action, [campaign], trigger);
                      }}
                    />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      }
      empty={
        slice.total === 0 ? (
          <EmptyPlate
            id="campaignRegistryEmpty"
            className="datatable-empty"
            label="No matching campaigns"
            note="Nothing matches the current filter or search. Clear the search or set the activation filter back to all campaigns."
          >
            <Key
              size="compact"
              onClick={() => {
                patch({ search: "", activationFilter: "all", page: 0, selection: EMPTY_SELECTION });
                announce("Filters cleared. Registry restored.");
              }}
            >
              Clear filters
            </Key>
          </EmptyPlate>
        ) : undefined
      }
      footer={
        <>
          <DataTablePagination
            total={slice.total}
            startIndex={slice.startIdx}
            visibleCount={slice.pageRows.length}
            page={state.page}
            pageCount={slice.pageCount}
            pageSize={state.pageSize}
            pageSizeOptions={[8, 16, 32]}
            onPageSizeChange={(pageSize) => {
              patch({ pageSize, page: 0 });
              closeMenus();
              announce(`Showing ${pad(pageSize)} rows per page.`);
            }}
            onPageChange={(page) => {
              patch({ page });
              closeMenus();
              announce(`Page ${pad(page + 1)} of ${pad(slice.pageCount)}.`);
            }}
            onPrevious={() => {
              patch({ page: state.page - 1 });
              closeMenus();
              announce("Previous page.");
            }}
            onNext={() => {
              patch({ page: state.page + 1 });
              closeMenus();
              announce("Next page.");
            }}
          />
          <ActionConfirmDialog
            open={confirm.open}
            confirmation={confirm.confirmation}
            error={confirm.error}
            waiting={confirm.waiting}
            onCancel={confirm.onCancel}
            onConfirm={confirm.onConfirm}
          />
        </>
      }
    />
  );
}
