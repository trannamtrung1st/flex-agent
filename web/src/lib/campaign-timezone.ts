export interface FormattedCampaignInstant {
  exactUtc: string;
  zoneLabel: string;
  localDisplay: string | null;
  conversionAvailable: boolean;
}

export function formatCampaignInstant(utcInstant: string, timeZoneId: string): FormattedCampaignInstant {
  const exactUtc = utcInstant.endsWith("Z") ? utcInstant : `${utcInstant}Z`;
  const zoneLabel = timeZoneId;
  try {
    const date = new Date(exactUtc);
    if (Number.isNaN(date.getTime())) {
      return { exactUtc, zoneLabel, localDisplay: null, conversionAvailable: false };
    }

    const formatter = new Intl.DateTimeFormat("en-GB", {
      timeZone: timeZoneId,
      dateStyle: "medium",
      timeStyle: "short",
      timeZoneName: "short",
    });
    const resolvedZone = formatter.resolvedOptions().timeZone;
    if (resolvedZone !== timeZoneId) {
      return { exactUtc, zoneLabel, localDisplay: null, conversionAvailable: false };
    }

    return { exactUtc, zoneLabel, localDisplay: formatter.format(date), conversionAvailable: true };
  } catch {
    return { exactUtc, zoneLabel, localDisplay: null, conversionAvailable: false };
  }
}
