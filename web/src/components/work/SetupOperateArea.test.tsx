import { render, screen } from "@testing-library/react";
import { SetupOperateArea } from "./SetupOperateArea";

describe("SetupOperateArea", () => {
  it("owns the setup record-plane on the record bay with a record frame", () => {
    render(
      <SetupOperateArea label="Setup and readiness" title="Setup and readiness">
        <p>Form</p>
      </SetupOperateArea>,
    );

    const region = screen.getByRole("region", { name: "Setup and readiness" });
    expect(region).toHaveClass("workspace-area", "work-plane", "record-plane", "record-plane--setup");
    expect(region.querySelector(".record-frame")).toBeTruthy();
  });
});
