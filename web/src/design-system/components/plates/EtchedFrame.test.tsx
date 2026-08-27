import { render } from "@testing-library/react";
import { EtchedFrame, PlateFoot, resolveFrameInset, resolveFrameTicks } from "./EtchedFrame";

describe("resolveFrameTicks", () => {
  it("defaults operational frames to bottom tick only", () => {
    expect(resolveFrameTicks()).toBe("bottom");
    expect(resolveFrameTicks("datatable-frame campaigns-registry-frame")).toBe("bottom");
    expect(resolveFrameTicks("index-frame")).toBe("bottom");
    expect(resolveFrameTicks("campaigns-frame sample-frame")).toBe("bottom");
  });

  it("keeps both ticks for the etched-frame gallery specimen", () => {
    expect(resolveFrameTicks("frame-demo")).toBe("both");
  });
});

describe("resolveFrameInset", () => {
  it("defaults ceremonial plates to padded inset", () => {
    expect(resolveFrameInset()).toBe("default");
    expect(resolveFrameInset("campaigns-frame sample-frame")).toBe("default");
    expect(resolveFrameInset("index-frame")).toBe("default");
    expect(resolveFrameInset("frame-demo")).toBe("default");
  });

  it("resolves full-bleed work bays to flush inset", () => {
    expect(resolveFrameInset("board-frame")).toBe("flush");
    expect(resolveFrameInset("datatable-frame")).toBe("flush");
    expect(resolveFrameInset("datatable-frame campaigns-registry-frame")).toBe("flush");
    expect(resolveFrameInset("datatable-frame wall-frame")).toBe("flush");
  });
});

describe("EtchedFrame", () => {
  it("applies flush inset for board frames by default", () => {
    const { container } = render(
      <EtchedFrame className="board-frame">
        <p>Bays</p>
      </EtchedFrame>,
    );

    expect(container.querySelector(".frame-cut")).toHaveClass("frame-cut--flush");
  });

  it("keeps ceremonial frames on default inset", () => {
    const { container } = render(
      <EtchedFrame className="campaigns-frame">
        <p>Readout</p>
      </EtchedFrame>,
    );

    expect(container.querySelector(".frame-cut")).not.toHaveClass("frame-cut--flush");
  });

  it("applies flush inset for datatable frames by default", () => {
    const { container } = render(
      <EtchedFrame className="datatable-frame">
        <p>Rows</p>
      </EtchedFrame>,
    );

    expect(container.querySelector(".frame-cut")).toHaveClass("frame-cut--flush");
  });

  it("can override auto-resolved flush inset", () => {
    const { container: flushOverride } = render(
      <EtchedFrame className="campaigns-frame" inset="flush">
        <p>Flush readout</p>
      </EtchedFrame>,
    );
    expect(flushOverride.querySelector(".frame-cut")).toHaveClass("frame-cut--flush");

    const { container: defaultOverride } = render(
      <EtchedFrame className="board-frame" inset="default">
        <p>Bays</p>
      </EtchedFrame>,
    );
    expect(defaultOverride.querySelector(".frame-cut")).not.toHaveClass("frame-cut--flush");
  });

  it("applies the flush inset modifier when inset is flush", () => {
    const { container } = render(
      <EtchedFrame inset="flush">
        <p>Flush payload</p>
      </EtchedFrame>,
    );

    expect(container.querySelector(".frame-cut")).toHaveClass("frame-cut--flush");
  });

  it("omits the top center tick on datatable frames by default", () => {
    const { container } = render(
      <EtchedFrame className="datatable-frame">
        <p>Rows</p>
      </EtchedFrame>,
    );

    expect(container.querySelector(".frame-tick--top")).toBeNull();
    expect(container.querySelector(".frame-tick--bottom")).toBeTruthy();
  });

  it("omits the top center tick on index frames by default", () => {
    const { container } = render(
      <EtchedFrame className="index-frame">
        <p>Channels</p>
      </EtchedFrame>,
    );

    expect(container.querySelector(".frame-tick--top")).toBeNull();
    expect(container.querySelector(".frame-tick--bottom")).toBeTruthy();
  });

  it("keeps both center ticks for the frame gallery specimen", () => {
    const { container } = render(
      <EtchedFrame className="frame-demo">
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

    expect(container.querySelector("footer.plate-foot")).toHaveTextContent("Configure");
  });

  it("clips half-beads on the inner pane and scrolls payload separately", () => {
    const { container } = render(
      <EtchedFrame className="frame-demo">
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
