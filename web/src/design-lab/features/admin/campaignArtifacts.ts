import type { Campaign } from "../../data/types";
import { formatDeadline, pad } from "../../../lib/format";

export function campaignsToCsv(campaigns: Campaign[]) {
  const header = ["id", "name", "activation", "enrollments", "cohort_deadline", "updated"];
  const lines = campaigns.map((campaign) => {
    const deadline = campaign.rows.reduce<Date | null>((latest, row) => {
      if (!latest || row.deadline.getTime() > latest.getTime()) return row.deadline;
      return latest;
    }, null);
    return [
      csvCell(campaign.id),
      csvCell(campaign.name),
      csvCell(campaign.frozen ? "frozen" : "draft"),
      csvCell(String(campaign.rows.length)),
      csvCell(deadline ? formatDeadline(deadline) : ""),
      csvCell(formatDeadline(campaign.updatedAt)),
    ].join(",");
  });
  return [header.join(","), ...lines].join("\n") + "\n";
}

export function campaignsConfigJson(campaigns: Campaign[]) {
  return JSON.stringify(
    campaigns.map((campaign) => ({
      id: campaign.id,
      name: campaign.name,
      frozen: campaign.frozen,
      config: campaign.config,
    })),
    null,
    2,
  );
}

export function summaryFilename(count: number, stamped = new Date()) {
  return `campaign-summary-${stamp(stamped)}${count === 1 ? "" : `-${pad(count)}`}.csv`;
}

export function configFilename(campaigns: Campaign[], stamped = new Date()) {
  if (campaigns.length === 1) return `campaign-config-${campaigns[0].id}.json`;
  return `campaign-configs-${pad(campaigns.length)}-${stamp(stamped)}.json`;
}

export function triggerDownload(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  link.rel = "noopener";
  document.body.append(link);
  link.click();
  link.remove();
  window.setTimeout(() => URL.revokeObjectURL(url), 0);
}

function stamp(date: Date) {
  return `${date.getFullYear()}${pad(date.getMonth() + 1)}${pad(date.getDate())}`;
}

function csvCell(value: string) {
  if (/[",\n]/.test(value)) return `"${value.replaceAll('"', '""')}"`;
  return value;
}
