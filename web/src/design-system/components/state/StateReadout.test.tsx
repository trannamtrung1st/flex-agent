import { render, screen } from "@testing-library/react";
import { StateReadout } from "./StateIndicator";

describe("StateReadout", () => {
  it("keeps the state-cell root for additive domain marks", () => {
    render(<StateReadout variant="dim" label="Not checked" className="setup-track-now" />);

    expect(screen.getByText("Not checked").closest(".state-cell")).toHaveClass("state-cell", "setup-track-now");
  });
});
