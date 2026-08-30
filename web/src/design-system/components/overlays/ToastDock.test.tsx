import { fireEvent, render, screen } from "@testing-library/react";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { ToastDock, ToastHost, usePushToast } from "./ToastDock";

function FireReceipt() {
  const pushToast = usePushToast();
  return (
    <button type="button" onClick={() => pushToast({ label: "Enrollment", copy: "Casey Candidate is assigned and active." })}>
      Fire
    </button>
  );
}

describe("ToastHost", () => {
  it("docks a polite receipt slip for a fired notice", () => {
    render(
      <ToastHost>
        <FireReceipt />
      </ToastHost>,
    );

    expect(document.querySelector(".toast-dock")).toHaveAttribute("aria-live", "polite");
    expect(document.querySelector(".toast-dock")).toHaveAttribute("data-placement", "bottom-center");
    fireEvent.click(screen.getByRole("button", { name: "Fire" }));
    const copy = screen.getByText("Casey Candidate is assigned and active.");
    expect(copy.closest(".toast")).toHaveAttribute("role", "status");
    expect(copy.closest(".toast")?.querySelector(".toast-label")).toHaveTextContent("Enrollment");
  });

  it("docks at bottom-center by default and accepts a placement override", () => {
    const { rerender } = render(<ToastDock toasts={[]} />);
    const dock = document.querySelector(".toast-dock");
    expect(dock).toHaveAttribute("data-placement", "bottom-center");

    rerender(
      <ToastDock
        toasts={[]}
        placement="bottom-end"
        offsetInline="248px"
        offsetBlock="88px"
      />,
    );
    const overridden = document.querySelector(".toast-dock");
    expect(overridden).toHaveAttribute("data-placement", "bottom-end");
    expect((overridden as HTMLElement).style.getPropertyValue("--toast-dock-offset-inline")).toBe("248px");
    expect((overridden as HTMLElement).style.getPropertyValue("--toast-dock-offset-block")).toBe("88px");
  });

  it("forwards placement from ToastHost onto the dock", () => {
    render(
      <ToastHost placement="top-end">
        <span />
      </ToastHost>,
    );
    expect(document.querySelector(".toast-dock")).toHaveAttribute("data-placement", "top-end");
  });
});

describe("toast-dock CSS", () => {
  it("keys geometry off data-placement instead of a hard trailing corner", () => {
    const css = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../../../styles/components/overlays.css"),
      "utf8",
    );
    expect(css).toMatch(/\.toast-dock\[data-placement="bottom-center"\]/);
    expect(css).toMatch(/\.toast-dock\[data-placement="bottom-start"\]/);
    expect(css).toMatch(/\.toast-dock\[data-placement="bottom-end"\]/);
    expect(css).not.toMatch(/\.toast-dock \{\s*position: fixed;\s*right: 22px;/);
  });
});
