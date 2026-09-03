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

describe("Key", () => {
  it("adds the danger modifier when destructive is set on a quiet key", () => {
    render(<Key variant="quiet" destructive>Delete</Key>);
    expect(screen.getByRole("button", { name: "Delete" })).toHaveClass("key--quiet", "key--danger");
  });

  it("styles destructive transmit commit keys with danger stroke", () => {
    render(<Key variant="transmit" size="large" destructive>Revoke</Key>);
    expect(screen.getByRole("button", { name: "Revoke" })).toHaveClass("key--transmit", "key--danger", "key--large");
  });
});

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

  it("treats line-clamp overflow as block truncation", () => {
    const el = document.createElement("span");
    Object.defineProperty(el, "scrollWidth", { configurable: true, value: 200 });
    Object.defineProperty(el, "clientWidth", { configurable: true, value: 200 });
    Object.defineProperty(el, "scrollHeight", { configurable: true, value: 48 });
    Object.defineProperty(el, "clientHeight", { configurable: true, value: 22 });
    expect(isTruncated(el)).toBe(false);
    expect(isTruncated(el, "block")).toBe(true);
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

  it("plaques a block-clamped status that still fits horizontally", () => {
    const line = document.createElement("span");
    Object.defineProperty(line, "scrollWidth", { configurable: true, value: 220 });
    Object.defineProperty(line, "clientWidth", { configurable: true, value: 220 });
    Object.defineProperty(line, "scrollHeight", { configurable: true, value: 64 });
    Object.defineProperty(line, "clientHeight", { configurable: true, value: 22 });
    const truncationRef = { current: line };

    render(
      <TooltipHost tip="Considering your reply…" tipOnlyWhenTruncated truncationAxis="block" truncationRef={truncationRef}>
        <span>Considering your reply…</span>
      </TooltipHost>,
    );
    fireEvent.mouseEnter(screen.getByText("Considering your reply…").closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toHaveTextContent("Considering your reply…");
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

  it("seats a label plaque inside an open modal dialog", async () => {
    const { NativeDialog } = await import("../overlays/NativeDialog");
    render(
      <NativeDialog open onClose={() => undefined} className="dialog" labelledBy="title">
        <h2 id="title">Confirm</h2>
        <TooltipHost tip="Select all visible participants.">
          <button type="button">Select page</button>
        </TooltipHost>
      </NativeDialog>,
    );
    fireEvent.mouseEnter(screen.getByRole("button", { name: "Select page" }).closest(".tip-host")!);
    const plaque = screen.getByRole("tooltip");
    expect(plaque).toHaveTextContent("Select all visible participants.");
    expect(plaque.parentElement).toBe(document.querySelector("dialog"));
  });

  it("does not stretch a dialog-portaled plaque to the plate height", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const overlaysCss = readFileSync(join(here, "../../../styles/components/overlays.css"), "utf8");
    const openDialog = overlaysCss.match(/\.dialog\[open\] \{[^}]+\}/)?.[0] ?? "";
    const plaque = overlaysCss.match(/\.tip-plaque \{[^}]+\}/)?.[0] ?? "";
    const floating = overlaysCss.match(/\.floating-overlay \{[^}]+\}/)?.[0] ?? "";
    expect(openDialog).toMatch(/display:\s*block/);
    expect(openDialog).not.toMatch(/display:\s*flex/);
    expect(overlaysCss).toMatch(/\.dialog-stage \{/);
    expect(plaque).toMatch(/height:\s*max-content/);
    expect(floating).toMatch(/height:\s*max-content/);
  });

  it("does not light a disabled activate key on hover", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const keysCss = readFileSync(join(here, "../../../styles/components/keys.css"), "utf8");
    expect(keysCss).toMatch(/\.key--activate:hover:not\(:disabled\)/);
    expect(keysCss).toMatch(/\.key--activate:hover:not\(:disabled\)::before/);
    expect(keysCss).not.toMatch(/\.key--activate:hover, \.key--activate:focus-visible \{/);
  });

  it("keeps plaque chrome to the hairline so umbra does not smear dialog feet", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const overlaysCss = readFileSync(join(here, "../../../styles/components/overlays.css"), "utf8");
    const plaque = overlaysCss.match(/\.tip-plaque \{[^}]+\}/)?.[0] ?? "";
    expect(plaque).toMatch(/box-shadow:\s*none/);
    expect(plaque).not.toMatch(/10px 28px/);
  });

  it("hugs plaque copy instead of stretching to the trigger width", () => {
    render(
      <TooltipHost tip="Frozen at cohort activation">
        <button type="button">Harness snapshot</button>
      </TooltipHost>,
    );
    const host = screen.getByRole("button").closest(".tip-host") as HTMLElement;
    host.getBoundingClientRect = () => ({
      x: 40, y: 80, top: 80, left: 40, right: 280, bottom: 112, width: 240, height: 32, toJSON() { return {}; },
    });
    fireEvent.mouseEnter(host);
    act(() => {
      window.dispatchEvent(new Event("resize"));
    });
    const plaque = screen.getByRole("tooltip");
    expect(plaque.style.minWidth).not.toBe("240px");
    expect(plaque.style.width).toBe("");
  });

  it("shifts a value plaque inward from the right viewport edge", () => {
    Object.defineProperty(window, "innerWidth", { configurable: true, value: 400 });
    Object.defineProperty(window, "innerHeight", { configurable: true, value: 400 });
    render(
      <TooltipHost tip="a1000000-0000-4000-8000-000000000007" tone="value">
        <button type="button">id</button>
      </TooltipHost>,
    );
    const host = screen.getByRole("button").closest(".tip-host") as HTMLElement;
    host.getBoundingClientRect = () => ({
      x: 360, y: 80, top: 80, left: 360, right: 392, bottom: 104, width: 32, height: 24, toJSON() { return {}; },
    });
    fireEvent.mouseEnter(host);
    const plaque = screen.getByRole("tooltip");
    plaque.getBoundingClientRect = () => ({
      x: 0, y: 0, top: 0, left: 0, right: 240, bottom: 32, width: 240, height: 32, toJSON() { return {}; },
    });
    act(() => {
      window.dispatchEvent(new Event("resize"));
    });
    const left = Number.parseFloat(plaque.style.left);
    expect(left + 240).toBeLessThanOrEqual(400);
    expect(left).toBeGreaterThanOrEqual(0);
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

  it("hides immediately on external scroll without waiting for linger", () => {
    render(
      <TooltipHost tip="Frozen at cohort activation">
        <button type="button">Harness snapshot</button>
      </TooltipHost>,
    );
    fireEvent.mouseEnter(screen.getByRole("button").closest(".tip-host")!);
    expect(screen.getByRole("tooltip")).toBeInTheDocument();
    fireEvent.scroll(window);
    expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
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

describe("ceremony size", () => {
  it("lets large hot keys keep notch inset while using ceremony block padding and 44px floor", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const keysCss = readFileSync(join(here, "../../../styles/components/keys.css"), "utf8");
    const large = keysCss.match(/\.key--large \{[^}]+\}/)?.[0] ?? "";
    expect(large).toMatch(/min-height:\s*44px/);
    const largeHot = keysCss.match(
      /\.key--transmit\.key--large,\s*\.key--open\.key--large,\s*\.key--inspect\.key--large,\s*\.key--release\.key--large,\s*\.key--begin\.key--large,\s*\.key--activate\.key--large \{[^}]+\}/,
    )?.[0] ?? "";
    expect(largeHot).toMatch(/padding-block:\s*var\(--key-large-padding-block\)/);
    expect(largeHot).toMatch(/font-size:\s*var\(--key-large-font-size\)/);
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
