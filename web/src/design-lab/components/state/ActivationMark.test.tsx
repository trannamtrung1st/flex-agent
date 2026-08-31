import { render, screen } from "@testing-library/react";
import { ActivationMark } from "./ActivationMark";

describe("ActivationMark", () => {
  it("uses grid placement styling in readout grids", () => {
    render(<ActivationMark frozen={false} placement="grid" />);
    expect(screen.getByText("Draft — not activated").closest(".state-cell")).toHaveClass("readout-grid-state");
  });
});
