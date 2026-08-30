import { compactRegistryId } from "./compactRegistryId";

describe("compactRegistryId", () => {
  it("shortens UUID-style registry ids", () => {
    expect(compactRegistryId("a1000000-0000-4000-8000-000000000025")).toBe("a1000000…000025");
    expect(compactRegistryId("e1000000-0000-4000-8000-000000000002")).toBe("e1000000…000002");
  });

  it("keeps short ids readable", () => {
    expect(compactRegistryId("act-1")).toBe("act…1");
    expect(compactRegistryId("solo")).toBe("solo");
  });
});
