import { render, screen } from "@testing-library/react";
import { WaitPlate } from "./WaitPlate";

describe("WaitPlate", () => {
  it("announces loading as a wait-plate with a scan track", () => {
    render(<WaitPlate label="Loading activities…" />);
    const status = screen.getByRole("status");
    expect(status).toHaveAttribute("aria-busy", "true");
    expect(status).toHaveAttribute("aria-live", "polite");
    expect(status).toHaveClass("wait-plate");
    expect(screen.getByText("Loading activities…")).toBeVisible();
    expect(status.querySelector(".scan-track.is-waiting")).toBeTruthy();
    expect(status.querySelector(".wait-mark")).toBeTruthy();
  });

  it("uses inset anatomy inside an etched ceremony well", () => {
    render(<WaitPlate inset label="Establishing session context…" note="Confirming the production application session." />);
    const status = screen.getByRole("status");
    expect(status).toHaveClass("wait-plate", "wait-plate--inset");
    expect(screen.getByText("Establishing session context…")).toBeVisible();
    expect(screen.getByText("Confirming the production application session.")).toBeVisible();
  });
});
