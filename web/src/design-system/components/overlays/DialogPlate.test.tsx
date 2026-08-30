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

  it("seats dialog head, body, and foot on equal plate-foot block pad", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const overlaysCss = readFileSync(join(here, "../../../styles/components/overlays.css"), "utf8");
    const head = overlaysCss.match(/\.dialog-head \{[^}]+\}/)?.[0] ?? "";
    const body = overlaysCss.match(/\.dialog-body \{[^}]+\}/)?.[0] ?? "";
    const foot = overlaysCss.match(/\.dialog-foot \{[^}]+\}/)?.[0] ?? "";
    const pad = /padding:\s*var\(--plate-foot-pad-block\)\s+var\(--frame-inset-inline\)/;
    expect(head).toMatch(pad);
    expect(body).toMatch(pad);
    expect(foot).toMatch(pad);
    expect(head).not.toMatch(/22px 24px 14px/);
    expect(body).not.toMatch(/18px 24px 20px/);
    expect(foot).not.toMatch(/14px 24px 20px/);
  });

  it("does not force dialog key groups to trailing independently of arrangement", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const overlaysCss = readFileSync(join(here, "../../../styles/components/overlays.css"), "utf8");
    expect(overlaysCss).not.toMatch(/\.dialog-foot > \.key-group \{[^}]*justify-content:\s*flex-end/);
    expect(overlaysCss).not.toMatch(/\.dialog-foot \{[^}]*border-(?:top|block-start):/);
    const ceremonyCss = readFileSync(join(here, "../../../styles/surfaces/admin-console.css"), "utf8");
    expect(ceremonyCss).toMatch(
      /\.ceremony-foot \{[^}]*padding-block-start:\s*var\(--plate-foot-pad-block\)/,
    );
    expect(ceremonyCss).toMatch(
      /\.ceremony-foot \{[^}]*border-block-start:\s*1px solid var\(--hairline-dim\)/,
    );
  });
});
