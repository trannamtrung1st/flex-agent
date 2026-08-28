import { isProductionDestinationOpen, shouldHideProductionBreadcrumbs } from "./production-navigation";

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

  it("keeps crumbs on available workspace locators", () => {
    expect(shouldHideProductionBreadcrumbs("/my-work", participantNav)).toBe(false);
    expect(shouldHideProductionBreadcrumbs("/activities/act-1/setup", administratorNav)).toBe(false);
  });

  it("treats Session as open when My work is available", () => {
    expect(isProductionDestinationOpen(participantNav, "sessions")).toBe(true);
    expect(isProductionDestinationOpen(administratorNav, "sessions")).toBe(false);
  });
});
