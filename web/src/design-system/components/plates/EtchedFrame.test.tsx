import { render, screen } from "@testing-library/react";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { EmptyPlate, EtchedFrame, PlateFoot } from "./EtchedFrame";

describe("EtchedFrame", () => {
  it("defaults to padded inset", () => {
    const { container } = render(
      <EtchedFrame className="campaigns-frame">
        <p>Readout</p>
      </EtchedFrame>,
    );

    expect(container.querySelector(".frame-cut")).not.toHaveClass("frame-cut--flush");
  });

  it("applies flush inset when inset is flush", () => {
    const { container } = render(
      <EtchedFrame className="board-frame" inset="flush">
        <p>Bays</p>
      </EtchedFrame>,
    );

    expect(container.querySelector(".frame-cut")).toHaveClass("frame-cut--flush");
  });

  it("keeps padded inset when inset is default", () => {
    const { container } = render(
      <EtchedFrame className="board-frame" inset="default">
        <p>Bays</p>
      </EtchedFrame>,
    );

    expect(container.querySelector(".frame-cut")).not.toHaveClass("frame-cut--flush");
  });

  it("does not infer flush from a former registry class name", () => {
    const { container } = render(
      <EtchedFrame className="destination-board">
        <p>Destinations</p>
      </EtchedFrame>,
    );

    expect(container.querySelector(".frame-cut")).toHaveClass("destination-board");
    expect(container.querySelector(".frame-cut")).not.toHaveClass("frame-cut--flush");
  });

  it("defaults to bottom tick only", () => {
    const { container } = render(
      <EtchedFrame className="datatable-frame" inset="flush">
        <p>Rows</p>
      </EtchedFrame>,
    );

    expect(container.querySelector(".frame-tick--top")).toBeNull();
    expect(container.querySelector(".frame-tick--bottom")).toBeTruthy();
  });

  it("renders both center ticks when ticks is both", () => {
    const { container } = render(
      <EtchedFrame className="frame-demo" ticks="both">
        <p>Specimen</p>
      </EtchedFrame>,
    );

    expect(container.querySelector(".frame-tick--top")).toBeTruthy();
    expect(container.querySelector(".frame-tick--bottom")).toBeTruthy();
  });

  it("renders a plate foot for etched-frame actions", () => {
    const { container } = render(
      <PlateFoot>
        <button type="button">Configure</button>
      </PlateFoot>,
    );

    const foot = container.querySelector("footer.plate-foot");
    expect(foot).toHaveTextContent("Configure");
    expect(foot).toHaveAttribute("data-arrangement", "end");
    expect(foot).toHaveAttribute("data-flow-justify", "end");
    expect(foot).toHaveAttribute("data-hairline", "true");
    expect(foot).toHaveClass("composition-inline");
  });

  it("lets hull chrome omit the in-plate hairline", () => {
    const { container } = render(
      <PlateFoot hairline={false}>
        <button type="button">Cancel intake</button>
      </PlateFoot>,
    );

    expect(container.querySelector("footer.plate-foot")).toHaveAttribute("data-hairline", "false");
  });

  it("maps start, center, and split arrangements onto Inline justify", () => {
    const { rerender, container } = render(
      <PlateFoot arrangement="start">
        <button type="button">Continue</button>
      </PlateFoot>,
    );
    expect(container.querySelector("footer.plate-foot")).toHaveAttribute("data-arrangement", "start");
    expect(container.querySelector("footer.plate-foot")).toHaveAttribute("data-flow-justify", "start");

    rerender(
      <PlateFoot arrangement="center">
        <button type="button">Sign in</button>
      </PlateFoot>,
    );
    expect(container.querySelector("footer.plate-foot")).toHaveAttribute("data-arrangement", "center");
    expect(container.querySelector("footer.plate-foot")).toHaveAttribute("data-flow-justify", "center");

    rerender(
      <PlateFoot arrangement="split" secondary={<button type="button">Cancel</button>} primary={<button type="button">Save</button>} />,
    );
    const split = container.querySelector("footer.plate-foot");
    expect(split).toHaveAttribute("data-arrangement", "split");
    expect(split).toHaveAttribute("data-flow-justify", "between");
    const slots = container.querySelectorAll(".plate-foot-slot");
    expect(slots[0]).toHaveClass("plate-foot-slot--secondary");
    expect(slots[0]).toHaveTextContent("Cancel");
    expect(slots[1]).toHaveClass("plate-foot-slot--primary");
    expect(slots[1]).toHaveTextContent("Save");
    const cancel = screen.getByRole("button", { name: "Cancel" });
    const save = screen.getByRole("button", { name: "Save" });
    expect(cancel.compareDocumentPosition(save) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it("keeps a split primary at the trailing slot when secondary is omitted", () => {
    const { container } = render(
      <PlateFoot arrangement="split" primary={<button type="button">Open</button>} />,
    );
    const foot = container.querySelector("footer.plate-foot");
    expect(foot?.querySelector(".plate-foot-slot--secondary")).toBeEmptyDOMElement();
    expect(foot?.querySelector(".plate-foot-slot--primary")).toHaveTextContent("Open");
  });

  it("does not keep a className hatch for leading feet", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const platesCss = readFileSync(join(here, "../../../styles/components/plates.css"), "utf8");
    expect(platesCss).not.toMatch(/plate-foot--start/);
    expect(platesCss).toMatch(
      /\.plate-foot\[data-arrangement="split"\] \.plate-foot-slot--primary \{[^}]*margin-inline-start:\s*auto/,
    );
    expect(platesCss).toMatch(
      /\.plate-foot \{[^}]*width:\s*100%;[^}]*border-block-start:\s*1px solid var\(--hairline-dim\)/,
    );
    expect(platesCss).toMatch(
      /\.plate-foot\[data-hairline="false"\] \{[^}]*border-block-start:\s*none/,
    );
    expect(platesCss).toMatch(
      /:has\(\+ \.plate-foot:not\(\[data-hairline="false"\]\)\)/,
    );
    expect(platesCss).toMatch(
      /\.assignment-plate\.frame-cut:not\(\.frame-cut--flush\) > \.frame-in \{[^}]*padding-inline:\s*0/,
    );
    expect(platesCss).toMatch(
      /\.assignment-plate \.assignment-plate-keys \{[^}]*padding-inline:\s*var\(--frame-inset-inline\)/,
    );
    expect(platesCss).toMatch(
      /\.frame-cut:not\(\.frame-cut--flush\) > \.frame-in:has\(\.setup-ceremony\),\s*\.frame-cut:not\(\.frame-cut--flush\) > \.frame-in:has\(\.in-plate-host\) \{[^}]*padding-inline:\s*0/,
    );
    expect(platesCss).toMatch(
      /\.setup-ceremony > :not\(\.plate-foot\),\s*\.in-plate-host > :not\(\.plate-foot\) \{[^}]*padding-inline:\s*var\(--frame-inset-inline\)/,
    );
    expect(platesCss).toMatch(
      /\.setup-ceremony > \.plate-foot,\s*\.in-plate-host > \.plate-foot \{[^}]*padding-inline:\s*var\(--frame-inset-inline\)/,
    );
    expect(platesCss).not.toMatch(/\.work-well > \.work-well__foot \{[^}]*border-(?:top|block-start):/);
    expect(platesCss).toMatch(
      /:has\(\+ \.plate-foot:not\(\[data-hairline="false"\]\)\):not\(\.dialog-body\):not\(\.work-well__body\):not\(\.create-ceremony__scroll\) \{[^}]*padding-block-end:\s*var\(--plate-foot-pad-block\)/,
    );
    expect(platesCss).toMatch(
      /\.assignment-plate \.assignment-plate-keys \{[^}]*padding-block-start:\s*var\(--plate-foot-pad-block\)/,
    );
    expect(platesCss).not.toMatch(/\.assignment-plate \.assignment-plate-keys \{[^}]*padding-block-start:\s*10px/);
  });

  it("clips half-beads on the inner pane and scrolls payload separately", () => {
    const { container } = render(
      <EtchedFrame className="frame-demo" ticks="both">
        <p>Specimen</p>
      </EtchedFrame>,
    );

    const pane = container.querySelector(".frame-in");
    const scroll = container.querySelector(".frame-scroll");
    expect(pane?.querySelector(":scope > .frame-node")).toBeTruthy();
    expect(pane?.querySelector(":scope > .frame-tick--bottom")).toBeTruthy();
    expect(container.querySelector(".frame-cut > .frame-tick")).toBeNull();
    expect(scroll).toHaveTextContent("Specimen");
    expect(scroll?.querySelector(".frame-node")).toBeNull();
    expect(scroll?.querySelector(".frame-tick")).toBeNull();
  });
});

describe("EmptyPlate", () => {
  it("marks in-frame absence as an inset well", () => {
    render(<EmptyPlate inset label="No current assignments" note="There is no assigned work." />);
    expect(screen.getByText("No current assignments").closest(".empty-plate")).toHaveClass("empty-plate--inset");
  });
});
