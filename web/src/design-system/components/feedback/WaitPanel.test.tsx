import { render, screen } from "@testing-library/react";
import { WaitPanel } from "./WaitPanel";

describe("WaitPanel", () => {
  it("announces loading with visible label", () => {
    render(<WaitPanel label="Loading activities…" />);
    const status = screen.getByRole("status");
    expect(status).toHaveAttribute("aria-busy", "true");
    expect(status).toHaveAttribute("aria-live", "polite");
    expect(screen.getByText("Loading activities…")).toBeVisible();
  });

  it("hides the label visually when announceOnly is set", () => {
    render(<WaitPanel label="Establishing session context…" announceOnly />);
    const label = screen.getByText("Establishing session context…");
    expect(label).toHaveClass("visually-hidden");
  });
});
