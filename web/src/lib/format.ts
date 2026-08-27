export const DESIGN_LAB_CAMPAIGN_TIME_ZONE = "America/Chicago";

export type ViewerInstantDisplay = {
  datetime: string;
  label: string;
  title: string;
};

export function resolveViewerTimeZone(timeZone?: string) {
  if (timeZone) return timeZone;
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
  } catch {
    return "UTC";
  }
}

function shortTimeZoneName(date: Date, timeZone: string) {
  const name = new Intl.DateTimeFormat("en-GB", {
    timeZone,
    timeZoneName: "short",
    hour: "2-digit",
  })
    .formatToParts(date)
    .find((part) => part.type === "timeZoneName")?.value;
  return name ?? timeZone;
}

function formatZonedClock(date: Date, timeZone: string) {
  return new Intl.DateTimeFormat("en-GB", {
    timeZone,
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(date);
}

export function formatViewerInstant(isoUtc: string, timeZone?: string): ViewerInstantDisplay {
  const zone = resolveViewerTimeZone(timeZone);
  const date = new Date(isoUtc);
  if (Number.isNaN(date.getTime())) {
    const fallback = `${isoUtc} (${zone} unavailable; UTC ${isoUtc})`;
    return { datetime: isoUtc, label: fallback, title: fallback };
  }
  const datetime = date.toISOString();
  try {
    const clock = formatZonedClock(date, zone);
    const shortZone = shortTimeZoneName(date, zone);
    return {
      datetime,
      label: `${clock} ${shortZone}`,
      title: `${clock} ${zone} · UTC ${datetime}`,
    };
  } catch {
    const fallback = `${datetime} (${zone} unavailable; UTC ${datetime})`;
    return { datetime, label: fallback, title: fallback };
  }
}

export function prefersReducedMotion() {
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}

export function formatNamedCampaignInstant(
  isoUtc: string,
  timeZone = DESIGN_LAB_CAMPAIGN_TIME_ZONE,
) {
  const date = new Date(isoUtc);
  const utc = Number.isNaN(date.getTime()) ? isoUtc : date.toISOString().replace(".000Z", "Z");
  if (Number.isNaN(date.getTime())) {
    return `${isoUtc} (${timeZone} unavailable; UTC ${isoUtc})`;
  }
  try {
    const zoned = new Intl.DateTimeFormat("en-GB", {
      timeZone,
      day: "2-digit",
      month: "short",
      hour: "2-digit",
      minute: "2-digit",
      hour12: false,
    }).format(date);
    return `${zoned} ${timeZone} (${utc})`;
  } catch {
    return `${utc} (${timeZone} unavailable; UTC ${utc})`;
  }
}

export function pad(n: number, width = 2) {
  return String(n).padStart(n > 99 && width < 3 ? 3 : width, "0");
}

export function formatDeadline(d: Date) {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}  ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function formatClock(seconds: number) {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const sec = seconds % 60;
  return [h, m, sec].map((n) => String(n).padStart(2, "0")).join(":");
}

export function polar(cx: number, cy: number, r: number, deg: number) {
  const rad = ((deg - 90) * Math.PI) / 180;
  return [cx + r * Math.cos(rad), cy + r * Math.sin(rad)] as const;
}

export function arcPath(cx: number, cy: number, r: number, startDeg: number, endDeg: number) {
  const [sx, sy] = polar(cx, cy, r, startDeg);
  const [ex, ey] = polar(cx, cy, r, endDeg);
  const large = endDeg - startDeg > 180 ? 1 : 0;
  return `M ${sx.toFixed(2)} ${sy.toFixed(2)} A ${r} ${r} 0 ${large} 1 ${ex.toFixed(2)} ${ey.toFixed(2)}`;
}
