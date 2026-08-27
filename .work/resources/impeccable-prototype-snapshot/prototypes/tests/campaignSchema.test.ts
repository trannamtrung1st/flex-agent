import { describe, expect, it } from "vitest";
import { campaignSchema } from "../src/features/admin/campaignSchema";

const valid = {
  harness: "GOVERNED-EXAM-01",
  agent: "EXAMINER-CORE",
  sessionLimit: "60:00",
  timeWarning: "10:00",
  maxAttempts: "2",
  cooldown: "24H",
};

describe("campaignSchema", () => {
  it("accepts a valid frozen-config payload", () => {
    expect(campaignSchema.safeParse(valid).success).toBe(true);
  });

  it("rejects a warning at or after the session limit", () => {
    const result = campaignSchema.safeParse({ ...valid, timeWarning: "60:00" });
    expect(result.success).toBe(false);
  });

  it("rejects zero attempts", () => {
    const result = campaignSchema.safeParse({ ...valid, maxAttempts: "0" });
    expect(result.success).toBe(false);
  });
});
