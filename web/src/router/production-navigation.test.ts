import { isProductionDestinationOpen, productionDestinationUnavailableCopy, shouldHideProductionBreadcrumbs, availableProductionDestinations, productionWorkspaceHome } from "./production-navigation";

describe("shouldHideProductionBreadcrumbs", () => {
  const participantNav = [
    { destination_id: "home", is_available: true },
    { destination_id: "my-work", is_available: true },
  ];
  const administratorNav = [
    { destination_id: "home", is_available: true },
    { destination_id: "activities", is_available: true },
  ];

  it("hides crumbs on Home, unknown locators, and honest ceremony destinations", () => {
    expect(shouldHideProductionBreadcrumbs("/", administratorNav)).toBe(true);
    expect(shouldHideProductionBreadcrumbs("/not-a-destination", administratorNav)).toBe(true);
    expect(shouldHideProductionBreadcrumbs("/sessions/sess-1", participantNav)).toBe(true);
    expect(shouldHideProductionBreadcrumbs("/review", [{ destination_id: "review", is_available: true }])).toBe(true);
    expect(shouldHideProductionBreadcrumbs("/results", administratorNav)).toBe(true);
  });

  it("hides crumbs when the workspace destination is denied", () => {
    expect(shouldHideProductionBreadcrumbs("/my-work", administratorNav)).toBe(true);
    expect(shouldHideProductionBreadcrumbs("/activities", participantNav)).toBe(true);
  });

  it("hides crumbs on available gangway indexes", () => {
    expect(shouldHideProductionBreadcrumbs("/activities", administratorNav)).toBe(true);
    expect(shouldHideProductionBreadcrumbs("/my-work", participantNav)).toBe(true);
  });

  it("keeps crumbs on nested workspace locators", () => {
    expect(shouldHideProductionBreadcrumbs("/my-work/enr-1", participantNav)).toBe(false);
    expect(shouldHideProductionBreadcrumbs("/activities/act-1/setup", administratorNav)).toBe(false);
  });

  it("treats Session locators as open for assignment, operations, and review actors", () => {
    expect(isProductionDestinationOpen(participantNav, "sessions")).toBe(true);
    expect(isProductionDestinationOpen(administratorNav, "sessions")).toBe(true);
    expect(isProductionDestinationOpen([{ destination_id: "review", is_available: true }], "sessions")).toBe(true);
    expect(isProductionDestinationOpen([{ destination_id: "home", is_available: true }], "sessions")).toBe(false);
  });
});

describe("availableProductionDestinations", () => {
  it("omits Home when My work is the assignment index", () => {
    expect(availableProductionDestinations([
      { destination_id: "home", is_available: true },
      { destination_id: "my-work", is_available: true },
      { destination_id: "results", is_available: true },
    ]).map((item) => item.id)).toEqual(["my-work", "results"]);
  });

  it("keeps Home when My work is not available", () => {
    expect(availableProductionDestinations([
      { destination_id: "home", is_available: true },
      { destination_id: "activities", is_available: true },
      { destination_id: "my-work", is_available: false },
    ]).map((item) => item.id)).toEqual(["home", "activities"]);
  });
});

describe("productionWorkspaceHome", () => {
  it("lands My work actors on the assignment index", () => {
    expect(productionWorkspaceHome([
      { destination_id: "home", is_available: true },
      { destination_id: "my-work", is_available: true },
    ])).toBe("/my-work");
  });

  it("keeps administrators on Home", () => {
    expect(productionWorkspaceHome([
      { destination_id: "home", is_available: true },
      { destination_id: "activities", is_available: true },
    ])).toBe("/");
  });
});

describe("productionDestinationUnavailableCopy", () => {
  it("uses consistent grammar for each guarded destination", () => {
    expect(productionDestinationUnavailableCopy("activities")).toBe(
      "Activities are not available for the current authorized relationship.",
    );
    expect(productionDestinationUnavailableCopy("my-work")).toBe(
      "My work is not available for the current authorized relationship.",
    );
    expect(productionDestinationUnavailableCopy("review")).toBe(
      "Review work is not available for the current authorized relationship.",
    );
    expect(productionDestinationUnavailableCopy("release")).toBe(
      "Release work is not available for the current authorized relationship.",
    );
    expect(productionDestinationUnavailableCopy("results")).toBe(
      "Results are not available for the current authorized relationship.",
    );
    expect(productionDestinationUnavailableCopy("sessions")).toBe(
      "Sessions are not available for the current authorized relationship.",
    );
  });
});
