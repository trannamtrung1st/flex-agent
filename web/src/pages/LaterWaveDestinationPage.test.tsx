import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { LaterWaveDestinationPage } from "./LaterWaveDestinationPage";

describe("LaterWaveDestinationPage", () => {
  it("does not repeat the page title inside the empty plate", () => {
    render(
      <MemoryRouter>
        <LaterWaveDestinationPage
          title="Setup and readiness"
          note="Campaign setup is not connected in this candidate build. The Campaign remains on the server."
        />
      </MemoryRouter>,
    );

    expect(screen.getAllByRole("heading", { name: "Setup and readiness" })).toHaveLength(1);
    expect(screen.getByText("Not connected yet")).toBeInTheDocument();
    expect(screen.queryByText(/Wave 8/)).not.toBeInTheDocument();
  });
});
