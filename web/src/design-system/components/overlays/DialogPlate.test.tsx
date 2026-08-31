import { render, screen } from "@testing-library/react";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { DialogPlate, DialogPlateBody, DialogPlateFooter, DialogPlateHead } from "./DialogPlate";

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
    expect(body).not.toMatch(/padding-inline-end:\s*calc\(var\(--frame-inset-inline\) \+ var\(--space-3\)\)/);
    expect(head).not.toMatch(/22px 24px 14px/);
    expect(body).not.toMatch(/18px 24px 20px/);
    expect(foot).not.toMatch(/14px 24px 20px/);
  });

  it("lets overlay dialog-body scroll with symmetric inset and edge overlay thumb", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const overlaysCss = readFileSync(join(here, "../../../styles/components/overlays.css"), "utf8");
    const overlayBodies = overlaysCss.match(/:is\(\.dialog-body, \.ceremony-body\) \{[^}]+\}/)?.[0] ?? "";
    expect(overlayBodies).toMatch(/scrollbar-color:/);
    expect(overlayBodies).not.toMatch(/scrollbar-gutter:\s*stable/);
    const dialogBody = overlaysCss.match(/\.dialog-body \{[^}]+\}/)?.[0] ?? "";
    expect(dialogBody).toMatch(/overflow-y:\s*auto/);
    expect(dialogBody).toMatch(/scrollbar-gutter:\s*auto/);
    expect(dialogBody).toMatch(/scrollbar-width:\s*auto/);
    expect(dialogBody).not.toMatch(/padding-inline-end:/);
  });

  it("does not force dialog key groups to trailing independently of arrangement", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const overlaysCss = readFileSync(join(here, "../../../styles/components/overlays.css"), "utf8");
    expect(overlaysCss).not.toMatch(/\.dialog-foot > \.key-group \{[^}]*justify-content:\s*flex-end/);
    expect(overlaysCss).not.toMatch(/\.dialog-foot \{[^}]*border-(?:top|block-start):/);
  });

  it("keeps the generic plate free of campaign ceremony classes", () => {
    render(
      <DialogPlate width="wide">
        <DialogPlateHead title="Confirm" titleId="confirmTitle" />
        <DialogPlateBody>
          <p>Body</p>
        </DialogPlateBody>
        <DialogPlateFooter>
          <button type="button">Close</button>
        </DialogPlateFooter>
      </DialogPlate>,
    );
    expect(document.querySelector(".dialog-plate--wide")).toBeTruthy();
    expect(document.querySelector(".ceremony-plate")).toBeNull();
    expect(document.querySelector(".warn-triangle")).toBeTruthy();
    expect(document.querySelector(".dialog-head .dialog-title")).toHaveTextContent("Confirm");
    expect(document.querySelector(".ceremony-trace")).toBeNull();
  });
});
