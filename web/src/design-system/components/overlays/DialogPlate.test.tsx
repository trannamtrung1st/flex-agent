import { render, screen } from "@testing-library/react";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { DialogPlateFooter } from "./DialogPlate";

describe("DialogPlateFooter", () => {
  it("uses the shared plate foot with trailing arrangement", () => {
    render(
      <DialogPlateFooter>
        <button type="button">Close</button>
      </DialogPlateFooter>,
    );
    const foot = screen.getByRole("contentinfo");
    expect(foot).toHaveClass("plate-foot", "dialog-foot", "composition-inline");
    expect(foot).toHaveAttribute("data-arrangement", "end");
  });

  it("leaves ceremony fill feet outside the plate-foot rail", () => {
    render(
      <DialogPlateFooter className="ceremony-foot">
        <button type="button">Activate</button>
      </DialogPlateFooter>,
    );
    const foot = screen.getByRole("contentinfo");
    expect(foot).toHaveClass("ceremony-foot");
    expect(foot).not.toHaveClass("plate-foot");
    expect(foot).not.toHaveAttribute("data-arrangement");
  });

  it("splits secondary and primary keys across the rail", () => {
    render(
      <DialogPlateFooter
        arrangement="split"
        secondary={<button type="button">Cancel</button>}
        primary={<button type="button">Save</button>}
      />,
    );
    const foot = screen.getByRole("contentinfo");
    expect(foot).toHaveAttribute("data-arrangement", "split");
    expect(foot).toHaveAttribute("data-flow-justify", "between");
    expect(screen.getByRole("button", { name: "Cancel" }).closest(".plate-foot-slot--secondary")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Save" }).closest(".plate-foot-slot--primary")).toBeTruthy();
  });

  it("does not force dialog key groups to trailing independently of arrangement", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const overlaysCss = readFileSync(join(here, "../../../styles/components/overlays.css"), "utf8");
    expect(overlaysCss).not.toMatch(/\.dialog-foot > \.key-group \{[^}]*justify-content:\s*flex-end/);
  });
});
