import { campaignCreateSchema, emptyCampaignCreateValues } from "./campaignCreateSchema";

describe("campaign create schema", () => {
  it("does not trim title values and rejects only an empty title", () => {
    const sources = Object.fromEntries(
      Object.keys(emptyCampaignCreateValues.sources).map((category) => [category, `${category}-id:v1`]),
    );
    expect(campaignCreateSchema.safeParse({
      title: "",
      sources,
    }).success).toBe(false);
    const spaced = campaignCreateSchema.safeParse({
      title: "   ",
      sources,
    });
    expect(spaced.success).toBe(true);
  });

  it("rejects a title longer than 200 characters", () => {
    const result = campaignCreateSchema.safeParse({
      title: "a".repeat(201),
      sources: Object.fromEntries(
        Object.keys(emptyCampaignCreateValues.sources).map((category) => [category, `${category}-id:v1`]),
      ),
    });
    expect(result.success).toBe(false);
  });

  it("accepts the existing untrimmed title and exact source identities", () => {
    const sources = Object.fromEntries(
      Object.keys(emptyCampaignCreateValues.sources).map((category) => [category, `${category}-id:v1`]),
    );
    const result = campaignCreateSchema.safeParse({
      title: "  Local campaign  ",
      sources,
    });
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.title).toBe("  Local campaign  ");
    }
  });
});
