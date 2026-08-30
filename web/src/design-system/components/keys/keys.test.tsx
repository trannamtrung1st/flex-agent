import { act, fireEvent, render, screen } from "@testing-library/react";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { afterEach, beforeEach, vi } from "vitest";
import { Key } from "./Key";
import { KeyGroup } from "./KeyGroup";
import { TOOLTIP_HIDE_DELAY_MS, TooltipHost } from "./TooltipHost";
import { isTruncated } from "./useTruncated";

function expectIconKeyLayout(button: HTMLElement) {
  const label = button.querySelector(".key-label");
  const icon = button.querySelector("svg");
  expect(label).toBeTruthy();
  expect(icon).toBeTruthy();
  expect(label?.parentElement).toBe(button);
  expect(icon?.parentElement).toBe(button);
  expect(label).not.toHaveStyle({ transform: "translateY(-1px)" });
  expect(button).not.toHaveStyle({ justifyContent: "center" });
}

describe("KeyGroup", () => {
  it("groups keys as an Inline cluster with a shared role", () => {
    render(
      <KeyGroup aria-label="Dialog actions">
        <Key>Cancel</Key>
        <Key size="compact">Save draft</Key>
      </KeyGroup>,
    );
    const group = screen.getByRole("group", { name: "Dialog actions" });
    expect(group).toHaveClass("key-group");
    expect(group).toHaveClass("composition-inline");
    expect(group).toHaveAttribute("data-flow-gap", "2.5");
    expect(group).toHaveAttribute("data-flow-align", "center");
    expect(group).toHaveAttribute("data-flow-wrap", "true");
    expect(group).toHaveAttribute("data-flow-justify", "start");
    expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Save draft" })).toHaveClass("key--compact");
  });

  it("does not force grouped keys to a cluster min-height", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const keysCss = readFileSync(join(here, "../../../styles/components/keys.css"), "utf8");
    expect(keysCss).not.toMatch(/key-group-min-height/);
    expect(keysCss).not.toMatch(/\.key-group\s*\{[^}]*align-items:\s*stretch/);
    const groupedKey = keysCss.match(
      /\.key-group > \.tip-host > \.key,\n\.key-group > \.tip-host > a\.key \{[\s\S]*?\n\}/,
    );
    expect(groupedKey?.[0]).toBeTruthy();
    expect(groupedKey?.[0]).not.toMatch(/min-height/);
    expect(groupedKey?.[0]).not.toMatch(/align-self:\s*stretch/);
  });

  it("can end-align keys for table action strips", () => {
    render(
      <KeyGroup aria-label="Table actions" justify="end">
        <Key size="compact">Create</Key>
      </KeyGroup>,
    );
    expect(screen.getByRole("group", { name: "Table actions" })).toHaveAttribute("data-flow-justify", "end");
  });
});

describe("truncate tooltip", () => {
  it("detects clipped labels", () => {
    const el = document.createElement("span");
    Object.defineProperty(el, "scrollWidth", { configurable: true, value: 200 });
    Object.defineProperty(el, "clientWidth", { configurable: true, value: 40 });
    expect(isTruncated(el)).toBe(true);

    Object.defineProperty(el, "scrollWidth", { configurable: true, value: 80 });
    Object.defineProperty(el, "clientWidth", { configurable: true, value: 400 });
    expect(isTruncated(el)).toBe(false);
  });

  it("does not treat line-box glyph overflow as truncation", () => {
    const el = document.createElement("span");
    Object.defineProperty(el, "scrollWidth", { configurable: true, value: 126 });
    Object.defineProperty(el, "clientWidth", { configurable: true, value: 126 });
    Object.defineProperty(el, "scrollHeight", { configurable: true, value: 13 });
    Object.defineProperty(el, "clientHeight", { configurable: true, value: 11 });
    expect(isTruncated(el)).toBe(false);
  });

  it("ignores one-pixel subpixel width overflow", () => {
    const el = document.createElement("span");
    Object.defineProperty(el, "scrollWidth", { configurable: true, value: 127 });
    Object.defineProperty(el, "clientWidth", { configurable: true, value: 126 });
    expect(isTruncated(el)).toBe(false);
  });

  it("omits a tooltip that repeats the visible caption", () => {
    render(<Key tooltip="Save">Save</Key>);
    fireEvent.mouseEnter(screen.getByRole("button", { name: "Save" }).closest(".tip-host")!);
    expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
  });

  it("keeps a tooltip that adds meaning beyond the caption", () => {
    render(<Key tooltip="Save the draft">Save</Key>);
    fireEvent.mouseEnter(screen.getByRole("button", { name: "Save" }).closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toHaveTextContent("Save the draft");
  });

  it("prefers a disabled reason over a distinct tooltip", () => {
    render(
      <Key tooltip="Export summary" disabled disabledReason="Select one or more campaigns.">
        Export
      </Key>,
    );
    fireEvent.mouseEnter(screen.getByRole("button").closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toHaveTextContent("Select one or more campaigns.");
  });

  it("prefers a disabled reason on icon keys over the action name", async () => {
    const { IconButton } = await import("./IconButton");
    render(
      <IconButton label="More actions" tooltip="More actions" disabled disabledReason="Select one or more campaigns.">
        <span aria-hidden="true" />
      </IconButton>,
    );
    fireEvent.mouseEnter(screen.getByRole("button", { name: "More actions" }).closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toHaveTextContent("Select one or more campaigns.");
  });

  it("does not surface truncate tooltips when the label fits", () => {
    render(
      <TooltipHost tip="Full label" tipOnlyWhenTruncated truncationRef={{ current: document.createElement("span") }}>
        <button type="button">Action</button>
      </TooltipHost>,
    );
    fireEvent.mouseEnter(screen.getByRole("button", { name: "Action" }).closest(".tip-host")!);
    expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
  });

  it("keeps a stable tip host so a later truncation measure can open the plaque", () => {
    const label = document.createElement("span");
    Object.defineProperty(label, "scrollWidth", { configurable: true, value: 200 });
    Object.defineProperty(label, "clientWidth", { configurable: true, value: 40 });
    const truncationRef = { current: label };

    const { rerender } = render(
      <TooltipHost tip="Confirm activation" tipOnlyWhenTruncated truncationRef={truncationRef}>
        <button type="button">Confirm activation</button>
      </TooltipHost>,
    );
    const host = screen.getByRole("button", { name: "Confirm activation" }).closest(".tip-host");
    rerender(
      <TooltipHost tip="Confirm activation" tipOnlyWhenTruncated truncationRef={truncationRef}>
        <button type="button">Confirm activation</button>
      </TooltipHost>,
    );
    expect(screen.getByRole("button", { name: "Confirm activation" }).closest(".tip-host")).toBe(host);

    fireEvent.mouseEnter(host!);
    expect(screen.getByRole("tooltip")).toHaveTextContent("Confirm activation");
  });

  it("still plaques a disabled reason when truncate is on and the caption fits", () => {
    render(
      <Key truncate disabled disabledReason="Check readiness before activation">
        Confirm activation
      </Key>,
    );
    fireEvent.mouseEnter(screen.getByRole("button").closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toHaveTextContent("Check readiness before activation");
  });
});

describe("TooltipHost focus-visible", () => {
  it("does not open on programmatic focus without focus-visible", async () => {
    render(
      <TooltipHost tip="Harness snapshot">
        <button type="button">Harness snapshot</button>
      </TooltipHost>,
    );
    const button = screen.getByRole("button", { name: "Harness snapshot" });
    await act(async () => {
      button.focus();
      fireEvent.focusIn(button);
      await new Promise((resolve) => requestAnimationFrame(resolve));
    });
    expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
  });

  it("opens on keyboard focus-visible", async () => {
    render(
      <TooltipHost tip="Harness snapshot">
        <button type="button">Harness snapshot</button>
      </TooltipHost>,
    );
    const button = screen.getByRole("button", { name: "Harness snapshot" });
    const matches = button.matches.bind(button);
    button.matches = (selector: string) => selector === ":focus-visible" || matches(selector);
    await act(async () => {
      button.focus();
      fireEvent.focusIn(button);
      await new Promise((resolve) => requestAnimationFrame(resolve));
    });
    expect(screen.getByRole("tooltip")).toHaveTextContent("Harness snapshot");
  });

  it("still opens on hover", () => {
    render(
      <TooltipHost tip="Harness snapshot">
        <button type="button">Harness snapshot</button>
      </TooltipHost>,
    );
    fireEvent.mouseEnter(screen.getByRole("button").closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toHaveTextContent("Harness snapshot");
  });
});

describe("TooltipHost linger and selection", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it("keeps the plaque open when the pointer moves from the host onto the plaque", () => {
    render(
      <TooltipHost tip="Frozen at cohort activation">
        <button type="button">Harness snapshot</button>
      </TooltipHost>,
    );
    const host = screen.getByRole("button").closest(".tip-host")!;
    fireEvent.mouseEnter(host);
    const plaque = screen.getByRole("tooltip");
    fireEvent.mouseLeave(host);
    fireEvent.mouseEnter(plaque);
    act(() => {
      vi.advanceTimersByTime(TOOLTIP_HIDE_DELAY_MS);
    });
    expect(screen.getByRole("tooltip")).toHaveTextContent("Frozen at cohort activation");
  });

  it("hides after the linger delay when the pointer leaves host and plaque", () => {
    render(
      <TooltipHost tip="Frozen at cohort activation">
        <button type="button">Harness snapshot</button>
      </TooltipHost>,
    );
    const host = screen.getByRole("button").closest(".tip-host")!;
    fireEvent.mouseEnter(host);
    fireEvent.mouseLeave(host);
    expect(screen.getByRole("tooltip")).toBeInTheDocument();
    act(() => {
      vi.advanceTimersByTime(TOOLTIP_HIDE_DELAY_MS - 1);
    });
    expect(screen.getByRole("tooltip")).toBeInTheDocument();
    act(() => {
      vi.advanceTimersByTime(1);
    });
    expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
  });

  it("does not hide while a pointer drag that started on the plaque is held", () => {
    render(
      <TooltipHost tip="a1000000-0000-4000-8000-000000000007" tone="value">
        <button type="button">a1000000…000007</button>
      </TooltipHost>,
    );
    const host = screen.getByRole("button").closest(".tip-host")!;
    fireEvent.mouseEnter(host);
    const plaque = screen.getByRole("tooltip");
    fireEvent.pointerDown(plaque);
    fireEvent.mouseLeave(plaque);
    act(() => {
      vi.advanceTimersByTime(TOOLTIP_HIDE_DELAY_MS);
    });
    expect(screen.getByRole("tooltip")).toBeInTheDocument();
    fireEvent.pointerUp(window);
    act(() => {
      vi.advanceTimersByTime(TOOLTIP_HIDE_DELAY_MS);
    });
    expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
  });

  it("lets the plaque receive pointer events and text selection", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const overlaysCss = readFileSync(join(here, "../../../styles/components/overlays.css"), "utf8");
    expect(overlaysCss).toMatch(/\.tip-plaque\s*\{[^}]*pointer-events:\s*auto/);
    expect(overlaysCss).toMatch(/\.tip-plaque\s*\{[^}]*user-select:\s*text/);
  });

  it("dismisses the previous plaque when another host opens", () => {
    render(
      <>
        <TooltipHost tip="First plaque">
          <button type="button">One</button>
        </TooltipHost>
        <TooltipHost tip="Second plaque">
          <button type="button">Two</button>
        </TooltipHost>
      </>,
    );
    fireEvent.mouseEnter(screen.getByRole("button", { name: "One" }).closest(".tip-host")!);
    fireEvent.mouseEnter(screen.getByRole("button", { name: "Two" }).closest(".tip-host")!);
    expect(screen.getAllByRole("tooltip")).toHaveLength(1);
    expect(screen.getByRole("tooltip")).toHaveTextContent("Second plaque");
  });
});

describe("icon keys", () => {
  it("lays out BackKey with sibling svg and key-label flex children", async () => {
    const { BackKey } = await import("./BackKey");
    render(<BackKey label="Campaigns" onClick={() => undefined} />);
    expectIconKeyLayout(screen.getByRole("button", { name: "Campaigns" }));
  });

  it("wraps plain text labels for truncation", () => {
    render(<Key truncate>Confirm activation after readiness</Key>);
    const button = screen.getByRole("button", { name: "Confirm activation after readiness" });
    expect(button.querySelector(".key-label")).toBeTruthy();
  });

  it("keeps grouped text keys leading-aligned when the key grows", () => {
    render(
      <div style={{ width: 200 }}>
        <KeyGroup aria-label="Dialog actions">
          <Key>Cancel</Key>
        </KeyGroup>
      </div>,
    );
    const button = screen.getByRole("button", { name: "Cancel" });
    expect(button).not.toHaveStyle({ justifyContent: "center" });
    expect(button.querySelector(".key-label")).toBeTruthy();
  });
});

describe("EllipsisKey", () => {
  it("renders a truncating key inside a width-constrained host", async () => {
    const { EllipsisKey } = await import("./EllipsisKey");
    render(
      <div style={{ width: 120 }}>
        <EllipsisKey id="ellipsisDemo">Confirm activation after readiness</EllipsisKey>
      </div>,
    );
    const button = screen.getByRole("button", { name: "Confirm activation after readiness" });
    expect(button).toHaveClass("key--truncate");
    expect(button.closest(".tip-host")).toHaveClass("tip-host");
    expect(button.querySelector(".key-label")).toBeTruthy();
  });
});

describe("occupied keys", () => {
  it.each([
    ["transmit", "Transmit"],
    ["open", "Open session"],
    ["inspect", "Inspect"],
    ["release", "Approve & release"],
    ["begin", "Begin examination"],
    ["activate", "Activate"],
  ] as const)("seats wait-mark and caption on an occupied %s key", (variant, label) => {
    render(<Key variant={variant} waiting disabled>{label}</Key>);
    const button = screen.getByRole("button", { name: label });
    expect(button).toHaveClass(`key--${variant}`, "is-waiting");
    expect(button).toHaveAttribute("aria-busy", "true");
    expect(button.querySelector(".wait-mark")).toBeTruthy();
    expect(button.querySelector(".key-label")).toHaveTextContent(label);
  });

  it("grounds occupied hot keys on --ground-deep instead of a flat teal fill", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const keysCss = readFileSync(join(here, "../../../styles/components/keys.css"), "utf8");
    expect(keysCss).toMatch(/\.key--transmit\.is-waiting[\s\S]*--key-face:[^;]*var\(--ground-deep\)/);
    expect(keysCss).not.toMatch(/\.key--transmit\.is-waiting[\s\S]*--key-face:\s*var\(--teal-glow\)/);
    expect(keysCss).toMatch(/\.key--open\.is-waiting[\s\S]*--key-face:[^;]*var\(--ground-deep\)/);
  });
});
