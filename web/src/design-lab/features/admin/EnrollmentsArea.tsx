import { useState } from "react";
import { DataTable, matchingEnrollmentIds } from "../../components/enrollment";
import { Key, registryTableHug } from "../../components";
import { EnrollmentWallOperateArea } from "../../components/operate";
import { EMPTY_SELECTION } from "../../../design-system/patterns/tableSelection";
import { ADMIN_STAGES } from "../../data/fixtures/campaigns";
import type { DataTableState } from "../../data/types";
import { useAdminContext } from "./adminContext";
import { CampaignContext } from "./CampaignContext";

const EMPTY_TABLE: DataTableState = {
  stageFilter: null,
  search: "",
  sorts: [{ key: "deadline", dir: "asc" }],
  page: 0,
  pageSize: 16,
  selection: EMPTY_SELECTION,
  expandedId: "P-3121",
};

export function EnrollmentsArea() {
  const { campaign, announce } = useAdminContext();
  const [table, setTable] = useState<DataTableState>(EMPTY_TABLE);
  const [tableCampaignId, setTableCampaignId] = useState(campaign?.id ?? null);

  if (campaign && campaign.id !== tableCampaignId) {
    setTableCampaignId(campaign.id);
    setTable({
      ...EMPTY_TABLE,
      expandedId: campaign.id === "CMP-0042" ? "P-3121" : null,
    });
  }

  const applyTable = (patch: Partial<DataTableState> | ((prev: DataTableState) => DataTableState)) => {
    setTable((prev) => (typeof patch === "function" ? patch(prev) : { ...prev, ...patch }));
  };

  if (!campaign) {
    return (
      <EnrollmentWallOperateArea
        variant="plain"
        label="Cohort enrollment manifest"
        title="Enrollment Manifest"
        description="Selecting a campaign for the enrollment records."
        empty={{
          label: "Campaign not available",
          note: "Select an authorized campaign before inspecting enrollments.",
        }}
        hug={registryTableHug(0)}
      />
    );
  }

  return (
    <EnrollmentWallOperateArea
      label="Cohort enrollment manifest"
      title="Enrollment Manifest"
      description="Cohort enrollment records for the selected Campaign."
      revealing
      hug={registryTableHug(matchingEnrollmentIds(campaign.rows, table).length)}
      context={<CampaignContext />}
    >
      <DataTable
          rows={campaign.rows}
          state={table}
          setState={applyTable}
          announce={announce}
          stages={ADMIN_STAGES}
          onOpenRecord={() => announce("Action is outside this prototype's scope.")}
          emptyAction={
            <Key
              size="compact"
              onClick={() => {
                applyTable({ stageFilter: null, search: "", page: 0, expandedId: null });
                announce("Filters cleared. Manifest restored.");
              }}
            >
              Clear filters
            </Key>
          }
        />
    </EnrollmentWallOperateArea>
  );
}
