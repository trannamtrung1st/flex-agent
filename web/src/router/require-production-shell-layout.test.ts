import { requireProductionShellLayout } from "./require-production-shell-layout";

describe("requireProductionShellLayout", () => {
  it("allows mapped management routes and unmapped redirect targets", () => {
    expect(requireProductionShellLayout("management", "/")).toBe("management");
    expect(requireProductionShellLayout(undefined, "/unknown")).toBe("management");
  });

  it("allows guided-task on the assignment locator", () => {
    expect(requireProductionShellLayout("guided-task", "/my-work/enr-1")).toBe("guided-task");
  });

  it("rejects a future live-session production assignment until the shell implements it", () => {
    expect(() => requireProductionShellLayout("live-session", "/sessions/sess-1")).toThrow(/live-session/);
  });
});
