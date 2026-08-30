import { productionOperatorIdentity, productionOperatorRole } from "./production-operator";

describe("productionOperatorIdentity", () => {
  it("uses the seated display name and keeps role separate", () => {
    expect(productionOperatorIdentity("participant", ["my-work"], "Demo Participant")).toEqual({
      shortId: "Demo Participant",
      fullId: "Demo Participant",
      role: "Participant",
      home: "/my-work",
    });
  });

  it("falls back to role when the shell has no display name", () => {
    expect(productionOperatorIdentity("administrator", ["activities"], "  ")).toEqual({
      shortId: "Administrator",
      fullId: "Administrator",
      role: "Administrator",
      home: "/",
    });
  });

  it("maps reviewer relationships", () => {
    expect(productionOperatorRole("reviewer", [])).toBe("Reviewer");
  });

  it("uses My work as operational home when that destination is available", () => {
    expect(productionOperatorIdentity("participant", ["my-work"]).home).toBe("/my-work");
    expect(productionOperatorIdentity("administrator", ["activities", "home"]).home).toBe("/");
  });
});
