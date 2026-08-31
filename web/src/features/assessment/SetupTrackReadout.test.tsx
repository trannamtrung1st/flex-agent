import { render, screen } from "@testing-library/react";
import { SetupTrackReadout } from "./SetupTrackReadout";

describe("SetupTrackReadout", () => {
  it("highlights the active setup track", () => {
    render(<SetupTrackReadout variant="dim" label="Not checked" now />);

    expect(screen.getByText("Not checked").closest(".state-cell")).toHaveClass("setup-track-now");
  });

  it("leaves inactive tracks without the now mark", () => {
    render(<SetupTrackReadout variant="rest" label="Seated" />);

    expect(screen.getByText("Seated").closest(".state-cell")).not.toHaveClass("setup-track-now");
  });
});
