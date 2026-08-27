import type { GangwayGroup } from "../../components";

export const ADMIN_AREA_LABELS: Record<string, string> = {
  campaigns: "Campaigns",
  cohorts: "Cohorts",
  enrollments: "Enrollments",
  sessions: "Sessions",
  "users-access": "Users & Access",
  policies: "Policies",
  "audit-log": "Audit Log",
};

const CAMPAIGN_DEFAULTING = new Set(["cohorts", "enrollments", "sessions"]);

export function areaSlug(pathname: string) {
  return pathname.split("/").filter(Boolean).at(-1) ?? "";
}

export function adminAreaLabel(pathname: string) {
  return ADMIN_AREA_LABELS[areaSlug(pathname)] ?? "Administrator";
}

export function defaultsCampaignSelection(pathname: string) {
  return CAMPAIGN_DEFAULTING.has(areaSlug(pathname));
}

export function operationalCampaignId(
  pathname: string,
  requestedId: string | null,
  knownIds: string[],
  rememberedId: string | null,
) {
  if (requestedId && knownIds.includes(requestedId)) return requestedId;
  if (!defaultsCampaignSelection(pathname)) return undefined;
  if (requestedId) return knownIds[0];
  if (rememberedId && knownIds.includes(rememberedId)) return rememberedId;
  return knownIds[0];
}

export function assessmentCampaignQuery(
  knownIds: string[],
  matchedId: string | undefined,
  rememberedId: string | null,
  pathname: string,
) {
  const id =
    (matchedId && knownIds.includes(matchedId) ? matchedId : undefined) ??
    (rememberedId && knownIds.includes(rememberedId) ? rememberedId : undefined) ??
    (defaultsCampaignSelection(pathname) ? knownIds[0] : undefined);
  return id ? `?campaign=${encodeURIComponent(id)}` : "";
}

export function adminNavGroups({
  pathname,
  campaignQuery,
}: {
  pathname: string;
  campaignQuery: string;
}): GangwayGroup[] {
  const slug = areaSlug(pathname);
  const toOperational = (area: string) => `/admin-console/${area}${campaignQuery}`;

  return [
    {
      label: "Assessment operations",
      items: [
        {
          to: "/admin-console/campaigns",
          label: "Campaigns",
          abbr: "CAM",
          current: slug === "campaigns",
        },
        { to: toOperational("cohorts"), label: "Cohorts", abbr: "COH", current: slug === "cohorts" },
        { to: toOperational("enrollments"), label: "Enrollments", abbr: "ENR", current: slug === "enrollments" },
        { to: toOperational("sessions"), label: "Sessions", abbr: "SES", current: slug === "sessions" },
      ],
    },
    {
      label: "Organization control",
      items: [
        { to: "/admin-console/users-access", label: "Users & Access", abbr: "ACC", current: slug === "users-access" },
        { to: "/admin-console/policies", label: "Policies", abbr: "POL", current: slug === "policies" },
      ],
    },
    {
      label: "Governance",
      items: [
        { to: "/admin-console/audit-log", label: "Audit Log", abbr: "AUD", current: slug === "audit-log" },
      ],
    },
  ];
}
