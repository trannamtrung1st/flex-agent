import { requireProductionShellLayout } from "./require-production-shell-layout";

describe("requireProductionShellLayout", () => {
  it("allows mapped management routes and unmapped redirect targets", () => {
    expect(requireProductionShellLayout("management", "/")).toBe("management");
    expect(requireProductionShellLayout(undefined, "/unknown")).toBe("management");
  });

  it("rejects a future non-management production assignment until the shell implements it", () => {
    expect(() => requireProductionShellLayout("guided-task", "/my-work/enr-1")).toThrow(/guided-task/);
  });
});
