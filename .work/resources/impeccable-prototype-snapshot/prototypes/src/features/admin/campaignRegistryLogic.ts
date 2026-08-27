import { pageRows, sortAndFilterRows } from "../../components/datatable/tableController";
import type {
  Campaign,
  CampaignRegistryRow,
  CampaignRegistrySortKey,
  CampaignRegistryState,
} from "../../data/types";

export function toRegistryRow(campaign: Campaign): CampaignRegistryRow {
  const deadline = campaign.rows.reduce<Date | null>((latest, row) => {
    if (!latest || row.deadline.getTime() > latest.getTime()) return row.deadline;
    return latest;
  }, null);
  return {
    id: campaign.id,
    name: campaign.name,
    frozen: campaign.frozen,
    enrollments: campaign.rows.length,
    deadline,
    updatedAt: campaign.updatedAt,
  };
}

export function campaignQueryKey(state: Pick<CampaignRegistryState, "search" | "activationFilter">) {
  return `activation:${state.activationFilter}|search:${state.search.trim().toUpperCase()}`;
}

export function matchingCampaignIds(rows: CampaignRegistryRow[], state: CampaignRegistryState) {
  return sortAndFilterCampaigns(rows, state).map((row) => row.id);
}

export function campaignMatches(row: CampaignRegistryRow, state: Pick<CampaignRegistryState, "search" | "activationFilter">) {
  const search = state.search.trim().toUpperCase();
  if (state.activationFilter === "draft" && row.frozen) return false;
  if (state.activationFilter === "frozen" && !row.frozen) return false;
  if (search && !`${row.id} ${row.name}`.toUpperCase().includes(search)) return false;
  return true;
}

export function campaignSortValue(row: CampaignRegistryRow, key: CampaignRegistrySortKey): string | number {
  switch (key) {
    case "campaign":
      return `${row.id} ${row.name}`.toUpperCase();
    case "activation":
      return row.frozen ? 1 : 0;
    case "enrollments":
      return row.enrollments;
    case "deadline":
      return row.deadline?.getTime() ?? 0;
    case "updated":
      return row.updatedAt.getTime();
  }
}

export function sortAndFilterCampaigns(rows: CampaignRegistryRow[], state: CampaignRegistryState) {
  return sortAndFilterRows(rows, {
    match: (row) => campaignMatches(row, state),
    sorts: state.sorts,
    getSortValue: campaignSortValue,
  });
}

export function pageCampaigns(list: CampaignRegistryRow[], state: CampaignRegistryState) {
  return pageRows(list, state.page, state.pageSize);
}
