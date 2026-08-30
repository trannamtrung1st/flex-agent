import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { act, fireEvent, render, screen } from "@testing-library/react";
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
});
