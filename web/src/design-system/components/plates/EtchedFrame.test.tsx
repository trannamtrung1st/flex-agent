import { render } from "@testing-library/react";
import { EtchedFrame, PlateFoot } from "./EtchedFrame";

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

    expect(container.querySelector("footer.plate-foot")).toHaveTextContent("Configure");
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
