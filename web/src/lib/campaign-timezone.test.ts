import { describe, expect, it } from "vitest";
import { formatCampaignInstant } from "./campaign-timezone";

describe("formatCampaignInstant", () => {
  it("keeps the exact UTC instant and named zone for a supported identifier", () => {
    const formatted = formatCampaignInstant("2026-09-30T17:00:00Z", "America/New_York");
    expect(formatted.exactUtc).toBe("2026-09-30T17:00:00Z");
    expect(formatted.zoneLabel).toBe("America/New_York");
    expect(formatted.exactUtc).toBe("2026-09-30T17:00:00Z");
    if (formatted.conversionAvailable) {
      expect(formatted.localDisplay).toEqual(expect.stringContaining("2026"));
    }
  });

  it("does not substitute the browser zone when the named zone is unsupported", () => {
    const formatted = formatCampaignInstant("2026-09-30T17:00:00Z", "Not/AZone");
    expect(formatted.exactUtc).toBe("2026-09-30T17:00:00Z");
    expect(formatted.zoneLabel).toBe("Not/AZone");
    expect(formatted.conversionAvailable).toBe(false);
    expect(formatted.localDisplay).toBeNull();
  });
});
