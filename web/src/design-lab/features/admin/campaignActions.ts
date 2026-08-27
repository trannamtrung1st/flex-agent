import type { Campaign } from "../../data/types";
import type { ActionResult, TableAction } from "../../components";
import {
  campaignsConfigJson,
  campaignsToCsv,
  configFilename,
  summaryFilename,
  triggerDownload,
} from "./campaignArtifacts";
import { pad } from "../../../lib/format";

function wait(ms: number) {
  return new Promise((resolve) => window.setTimeout(resolve, ms));
}

export function createCampaignActions(handlers: {
  configure: (campaign: Campaign) => void;
  deleteCampaigns: (campaigns: Campaign[]) => void;
}): TableAction<Campaign>[] {
  return [
    {
      id: "configure",
      label: "Configure campaign",
      kind: "standard",
      placement: "overflow",
      surfaces: ["row"],
      eligibility: (records) => {
        const frozen = records.filter((campaign) => campaign.frozen);
        if (frozen.length) return { allowed: false, reason: "Configuration frozen at activation" };
        return { allowed: true };
      },
      run: (records) => {
        const campaign = records[0];
        if (!campaign) return { ok: false, message: "That campaign is no longer in the registry." };
        handlers.configure(campaign);
        return { ok: true, label: "Campaign", message: `Opened configuration for ${campaign.id} / ${campaign.name}.` };
      },
    },
    {
      id: "export",
      label: "Export summary",
      compactLabel: "Export",
      tooltip: "Export summary",
      kind: "standard",
      placement: "primary",
      eligibility: () => ({
        allowed: false,
        reason: "Design-lab reference only. Production export requires a server permission contract.",
      }),
      run: (records) => {
        if (!records.length) return { ok: false, message: "Nothing remaining in the current selection." };
        triggerDownload(new Blob([campaignsToCsv(records)], { type: "text/csv" }), summaryFilename(records.length));
        return {
          ok: true,
          label: "Export",
          message: `CSV summary of ${pad(records.length)} campaign${records.length === 1 ? "" : "s"} downloaded.`,
        };
      },
    },
    {
      id: "download",
      label: "Download configuration",
      compactLabel: "Download",
      tooltip: "Download configuration",
      kind: "standard",
      placement: "primary",
      eligibility: () => ({
        allowed: false,
        reason: "Design-lab reference only. Production configuration download requires a server permission contract.",
      }),
      run: (records) => {
        if (!records.length) return { ok: false, message: "Nothing remaining in the current selection." };
        triggerDownload(
          new Blob([campaignsConfigJson(records)], { type: "application/json" }),
          configFilename(records),
        );
        return {
          ok: true,
          label: "Download",
          message: `Configuration snapshot of ${pad(records.length)} frozen campaign${records.length === 1 ? "" : "s"} downloaded.`,
        };
      },
    },
    {
      id: "delete",
      label: "Delete",
      kind: "destructive",
      placement: "overflow",
      eligibility: () => ({
        allowed: false,
        reason: "Design-lab reference only. Production does not delete auditable campaign history from this menu.",
      }),
      confirmation: (records) => {
        const n = records.length;
        return {
          title: n === 1 ? "Delete campaign" : "Delete campaigns",
          body:
            n === 1
              ? `Remove ${records[0].id} / ${records[0].name} from this prototype registry. This is synthetic demonstration behavior, not a production retention policy, and cannot be undone here.`
              : `Remove ${pad(n)} draft campaigns from this prototype registry. This is synthetic demonstration behavior, not a production retention policy, and cannot be undone here.`,
          commitLabel: n === 1 ? "Delete campaign" : `Delete ${pad(n)} campaigns`,
        };
      },
      run: async (records): Promise<ActionResult> => {
        if (!records.length) return { ok: false, message: "Those campaigns are no longer in the registry. Selection is preserved." };
        await wait(400);
        handlers.deleteCampaigns(records);
        return {
          ok: true,
          label: "Delete",
          message: `${pad(records.length)} draft campaign${records.length === 1 ? "" : "s"} removed from the prototype registry.`,
        };
      },
    },
  ];
}
