import { ABSENT_INSTANT_MARK } from "./format";

export interface FormattedCampaignInstant {
  exactUtc: string;
  zoneLabel: string;
  localDisplay: string | null;
  utcDisplay: string | null;
  conversionAvailable: boolean;
}

function isUtcAlias(timeZoneId: string): boolean {
  const normalized = timeZoneId.trim().toUpperCase();
  return normalized === "UTC" || normalized === "GMT" || normalized === "ETC/UTC" || normalized === "ETC/GMT";
}

function zonesAgree(requested: string, resolved: string): boolean {
  if (requested === resolved) {
    return true;
  }

  return isUtcAlias(requested) && isUtcAlias(resolved);
}

function utcDisplayOf(exactUtc: string): string | null {
  const date = new Date(exactUtc);
  if (Number.isNaN(date.getTime())) {
    return null;
  }

  try {
    return new Intl.DateTimeFormat("en-GB", {
      timeZone: "UTC",
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      timeZoneName: "short",
    }).format(date);
  } catch {
    return null;
  }
}

export function formatCampaignInstant(utcInstant: string, timeZoneId: string): FormattedCampaignInstant {
  const raw = typeof utcInstant === "string" ? utcInstant.trim() : "";
  if (!raw) {
    return { exactUtc: "", zoneLabel: timeZoneId, localDisplay: null, utcDisplay: null, conversionAvailable: false };
  }
  const exactUtc = raw.endsWith("Z") ? raw : `${raw}Z`;
  const zoneLabel = timeZoneId;
  const utcDisplay = utcDisplayOf(exactUtc);
  if (isUtcAlias(timeZoneId)) {
    if (!utcDisplay) {
      return { exactUtc, zoneLabel, localDisplay: null, utcDisplay, conversionAvailable: false };
    }
    return { exactUtc, zoneLabel, localDisplay: utcDisplay, utcDisplay, conversionAvailable: true };
  }
  try {
    const date = new Date(exactUtc);
    if (Number.isNaN(date.getTime())) {
      return { exactUtc, zoneLabel, localDisplay: null, utcDisplay, conversionAvailable: false };
    }

    // dateStyle/timeStyle cannot be combined with timeZoneName in ICU; the zone is
    // already named by the campaign record, so local clock copy is enough here.
    const formatter = new Intl.DateTimeFormat("en-GB", {
      timeZone: timeZoneId,
      dateStyle: "medium",
      timeStyle: "short",
    });
    const resolvedZone = formatter.resolvedOptions().timeZone;
    if (!zonesAgree(timeZoneId, resolvedZone)) {
      return { exactUtc, zoneLabel, localDisplay: null, utcDisplay, conversionAvailable: false };
    }

    return { exactUtc, zoneLabel, localDisplay: formatter.format(date), utcDisplay, conversionAvailable: true };
  } catch {
    return { exactUtc, zoneLabel, localDisplay: null, utcDisplay, conversionAvailable: false };
  }
}

export function campaignDeadlineCopy(formatted: FormattedCampaignInstant): string {
  if (formatted.conversionAvailable && formatted.localDisplay) {
    return formatted.localDisplay;
  }
  if (formatted.utcDisplay) {
    return `${formatted.utcDisplay} (${formatted.zoneLabel} conversion unavailable)`;
  }
  return ABSENT_INSTANT_MARK;
}
