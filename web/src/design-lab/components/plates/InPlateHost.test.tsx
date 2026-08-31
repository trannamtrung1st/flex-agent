import { render, screen } from "@testing-library/react";
import { InPlateHost } from "./InPlateHost";

describe("InPlateHost", () => {
  it("owns the etched-frame inset host class", () => {
    render(<InPlateHost>Readout body</InPlateHost>);
    expect(screen.getByText("Readout body").closest(".in-plate-host")).toHaveClass("plate-bleed");
  });
});
