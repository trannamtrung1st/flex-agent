import { describe, expect, it } from "vitest";
import { campaignDeadlineCopy, formatCampaignInstant } from "./campaign-timezone";

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
    expect(formatted.utcDisplay).toEqual(expect.stringMatching(/2026/));
    expect(formatted.utcDisplay).toEqual(expect.stringMatching(/UTC|GMT/i));
    expect(campaignDeadlineCopy(formatted)).toMatch(/conversion unavailable/i);
    expect(campaignDeadlineCopy(formatted)).not.toContain("2026-09-30T17:00:00Z");
  });

  it("formats UTC deadlines without a conversion-unavailable apology", () => {
    const formatted = formatCampaignInstant("2026-09-01T12:00:00Z", "UTC");
    expect(formatted.conversionAvailable).toBe(true);
    expect(formatted.localDisplay).toEqual(expect.stringMatching(/2026/));
    expect(campaignDeadlineCopy(formatted)).not.toMatch(/conversion unavailable/i);
    expect(campaignDeadlineCopy(formatted)).not.toContain("2026-09-01T12:00:00Z");
  });

  it("converts a named campaign zone without falling back to UTC", () => {
    const formatted = formatCampaignInstant("2026-09-12T23:00:00Z", "America/Chicago");
    expect(formatted.conversionAvailable).toBe(true);
    expect(formatted.localDisplay).toMatch(/12 Sept 2026/i);
    expect(formatted.localDisplay).toMatch(/18:00/);
    expect(campaignDeadlineCopy(formatted)).not.toMatch(/conversion unavailable/i);
    expect(campaignDeadlineCopy(formatted)).not.toContain("2026-09-12T23:00:00Z");
  });

  it("uses the shared absence mark when no readable UTC instant exists", () => {
    expect(campaignDeadlineCopy(formatCampaignInstant("", "UTC"))).toBe("—");
    expect(campaignDeadlineCopy(formatCampaignInstant("not-a-time", "UTC"))).toBe("—");
    expect(campaignDeadlineCopy(formatCampaignInstant("not-a-time", "UTC"))).not.toMatch(/undefined/i);
  });
});
