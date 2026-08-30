import { describe, expect, it } from "vitest";
import { formatViewerInstant } from "./format";

describe("formatViewerInstant", () => {
  const iso = "2026-08-25T19:42:00.000Z";

  it("formats a UTC instant in the viewer timezone with a short zone, not a repeated IANA name", () => {
    const formatted = formatViewerInstant(iso, "America/Chicago");
    expect(formatted.datetime).toBe("2026-08-25T19:42:00.000Z");
    expect(formatted.label).toMatch(/25 Aug/i);
    expect(formatted.label).toMatch(/14:42/);
    expect(formatted.label).toMatch(/CDT|GMT-5/);
    expect(formatted.label).not.toMatch(/America\/Chicago/);
    expect(formatted.title).toContain("America/Chicago");
    expect(formatted.title).toMatch(/2026-08-25T19:42:00/);
  });

  it("converts the same instant into another viewer timezone", () => {
    const formatted = formatViewerInstant(iso, "Asia/Bangkok");
    expect(formatted.label).toMatch(/26 Aug/i);
    expect(formatted.label).toMatch(/02:42/);
    expect(formatted.title).toContain("Asia/Bangkok");
    expect(formatted.label).not.toMatch(/Asia\/Bangkok/);
  });

  it("defaults to the runtime timezone when none is provided", () => {
    const resolved = Intl.DateTimeFormat().resolvedOptions().timeZone;
    const formatted = formatViewerInstant(iso);
    expect(formatted.title).toContain(resolved);
  });

  it("uses the shared absence mark when the instant is missing, not the word undefined", () => {
    for (const value of [undefined, null, "", "   "]) {
      const formatted = formatViewerInstant(value as never, "Asia/Saigon");
      expect(formatted.datetime).toBe("");
      expect(formatted.label).toBe("—");
      expect(formatted.title).toBe("Not recorded");
      expect(formatted.label).not.toMatch(/undefined/i);
      expect(formatted.title).not.toMatch(/undefined/i);
    }
  });

  it("uses the shared absence mark when the instant cannot be read", () => {
    const formatted = formatViewerInstant("not-a-time", "America/Chicago");
    expect(formatted.datetime).toBe("");
    expect(formatted.label).toBe("—");
    expect(formatted.title).toBe("Not recorded");
    expect(formatted.label).not.toMatch(/undefined/i);
    expect(formatted.label).not.toContain("America/Chicago");
  });
});
