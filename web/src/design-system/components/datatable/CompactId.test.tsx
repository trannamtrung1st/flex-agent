import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { act, fireEvent, render, screen } from "@testing-library/react";
import { NativeDialog } from "../overlays/NativeDialog";
import { CompactId } from "./CompactId";

const UUID = "a1000000-0000-4000-8000-000000000007";
const overlaysCss = resolve(dirname(fileURLToPath(import.meta.url)), "../../../styles/components/overlays.css");

describe("CompactId", () => {
  it("center-truncates a registry identifier and opens a value plaque with the exact full id", () => {
    render(<CompactId value={UUID} />);
    expect(screen.getByText("a1000000…000007")).toBeInTheDocument();
    fireEvent.mouseEnter(screen.getByText("a1000000…000007").closest(".tip-host")!);
    const plaque = screen.getByRole("tooltip");
    expect(plaque).toHaveTextContent(UUID);
    expect(plaque).toHaveClass("tip-plaque--value");
  });

  it("keeps the full identifier in the accessible cell text when compacted", () => {
    render(<CompactId value={UUID} />);
    expect(screen.getByText(UUID)).toHaveClass("visually-hidden");
    expect(screen.getByText("a1000000…000007")).toHaveAttribute("aria-hidden", "true");
  });

  it("does not open a plaque when the identifier already fits", () => {
    render(<CompactId value="solo" />);
    fireEvent.mouseEnter(screen.getByText("solo").closest(".tip-host")!);
    expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
    expect(screen.queryByText("solo", { selector: ".visually-hidden" })).not.toBeInTheDocument();
  });

  it("accepts an explicit display that still reveals the original value", () => {
    render(<CompactId value="GOVERNED-AUDIT-01" display="GOV…01" />);
    expect(screen.getByText("GOV…01")).toBeInTheDocument();
    fireEvent.mouseEnter(screen.getByText("GOV…01").closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toHaveTextContent("GOVERNED-AUDIT-01");
  });

  it("keeps value plaques from forcing uppercase", () => {
    const css = readFileSync(overlaysCss, "utf8");
    expect(css).toMatch(/\.tip-plaque--value\s*\{[^}]*text-transform:\s*none/);
  });

  it("opens the value plaque on keyboard focus-visible when tabbable", async () => {
    render(<CompactId value={UUID} tabbable />);
    const host = screen.getByText("a1000000…000007").closest<HTMLElement>(".compact-id")!;
    const matches = host.matches.bind(host);
    host.matches = (selector: string) => selector === ":focus-visible" || matches(selector);
    await act(async () => {
      host.focus();
      fireEvent.focusIn(host);
      await new Promise((resolve) => requestAnimationFrame(resolve));
    });
    expect(screen.getByRole("tooltip")).toHaveTextContent(UUID);
  });

  it("does not add a tab stop in dense registry tables by default", () => {
    render(<CompactId value={UUID} />);
    expect(screen.getByText("a1000000…000007").closest(".compact-id")).not.toHaveAttribute("tabindex");
  });

  it("opens the value plaque on press so a narrow table cell does not depend on hover", () => {
    render(<CompactId value={UUID} />);
    fireEvent.pointerDown(screen.getByText("a1000000…000007").closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toHaveTextContent(UUID);
  });

  it("centers the value plaque on the compact glyphs, not the stretched table host", () => {
    render(<CompactId value={UUID} />);
    const host = screen.getByText("a1000000…000007").closest(".tip-host") as HTMLElement;
    const glyphs = screen.getByText("a1000000…000007").closest(".compact-id") as HTMLElement;
    host.getBoundingClientRect = () => ({
      x: 0, y: 80, top: 80, left: 0, right: 400, bottom: 112, width: 400, height: 32, toJSON() { return {}; },
    });
    glyphs.getBoundingClientRect = () => ({
      x: 16, y: 88, top: 88, left: 16, right: 136, bottom: 104, width: 120, height: 16, toJSON() { return {}; },
    });
    fireEvent.mouseEnter(host);
    const plaque = screen.getByRole("tooltip");
    plaque.getBoundingClientRect = () => ({
      x: 0, y: 0, top: 0, left: 0, right: 120, bottom: 32, width: 120, height: 32, toJSON() { return {}; },
    });
    act(() => {
      window.dispatchEvent(new Event("resize"));
    });
    expect(Number.parseFloat(plaque.style.left)).toBe(16);
  });

  it("gives the compact-id host the full table-cell hit area without inflating row height", () => {
    const css = readFileSync(
      resolve(dirname(fileURLToPath(import.meta.url)), "../../../styles/components/datatable.css"),
      "utf8",
    );
    expect(css).toMatch(
      /\.datatable-table tbody td\.cell-content:has\(\.compact-id\)\s*\{[^}]*padding-top:\s*0/,
    );
    expect(css).toMatch(
      /\.datatable-table tbody td\.cell-content:has\(\.compact-id\)\s*\{[^}]*padding-bottom:\s*0/,
    );
    expect(css).toMatch(
      /\.datatable-table tbody td\.cell-content:has\(\.compact-id\)\s*>\s*\.tip-host\s*\{[^}]*width:\s*100%/,
    );
    expect(css).toMatch(
      /\.datatable-table tbody td\.cell-content:has\(\.compact-id\)\s*>\s*\.tip-host\s*\{[^}]*min-height:\s*var\(--datatable-row-height\)/,
    );
  });

  it("seats the value plaque inside an open modal dialog", () => {
    render(
      <NativeDialog open onClose={() => undefined} className="dialog" labelledBy="title">
        <h2 id="title">Assign Participant</h2>
        <CompactId tabbable value={UUID} />
      </NativeDialog>,
    );
    fireEvent.mouseEnter(screen.getByText("a1000000…000007").closest(".tip-host")!);
    const plaque = screen.getByRole("tooltip");
    expect(plaque).toHaveTextContent(UUID);
    expect(plaque.parentElement).toBe(document.querySelector("dialog"));
  });

  it("hides the value plaque on external scroll", () => {
    render(<CompactId value={UUID} />);
    fireEvent.mouseEnter(screen.getByText("a1000000…000007").closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toBeInTheDocument();
    fireEvent.scroll(window);
    expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
  });
});
