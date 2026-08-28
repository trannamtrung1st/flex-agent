import { render, screen } from "@testing-library/react";
import { OperateArea } from "./OperateArea";

describe("OperateArea", () => {
  it("is a labeled region, not a second main landmark", () => {
    render(
      <main id="main-content">
        <OperateArea className="workspace-area" label="Campaign registry" title="Campaign registry">
          <p>Records</p>
        </OperateArea>
      </main>,
    );

    expect(screen.getAllByRole("main")).toHaveLength(1);
    const region = screen.getByRole("region", { name: "Campaign registry" });
    expect(region).toHaveClass("workspace-area", "composition-stack");
    expect(region).toHaveTextContent("Records");
    const frame = region.querySelector(".frame-cut");
    const pane = region.querySelector(".frame-in");
    const scroll = region.querySelector(".frame-scroll");
    expect(pane?.querySelector(":scope > .frame-node--tr")).toBeTruthy();
    expect(pane?.querySelector(":scope > .frame-tick--bottom")).toBeTruthy();
    expect(frame?.querySelector(":scope > .frame-node")).toBeNull();
    expect(frame?.querySelector(":scope > .frame-tick")).toBeNull();
    expect(frame?.querySelector(".frame-tick--top")).toBeNull();
    expect(scroll).toHaveTextContent("Records");
    expect(scroll?.querySelector(".frame-node")).toBeNull();
  });

  it("keeps only the bottom center tick on non-gallery etched frames", () => {
    render(
      <OperateArea
        className="workspace-area"
        label="Campaign registry"
        title="Campaign registry"
        frameClassName="datatable-frame"
        frameInset="flush"
      >
        <p>Rows</p>
      </OperateArea>,
    );

    const region = screen.getByRole("region", { name: "Campaign registry" });
    const frame = region.querySelector(".datatable-frame");
    expect(frame).toHaveClass("frame-cut--flush");
    expect(frame?.querySelector(".frame-tick--top")).toBeNull();
    expect(frame?.querySelector(".frame-tick--bottom")).toBeTruthy();
  });

  it("keeps only the bottom center tick on campaign detail frames", () => {
    render(
      <OperateArea className="workspace-area" label="Campaign record" title="Campaign record" frameClassName="campaigns-frame">
        <p>Readouts</p>
      </OperateArea>,
    );

    const frame = screen.getByRole("region", { name: "Campaign record" }).querySelector(".campaigns-frame");
    expect(frame).not.toHaveClass("frame-cut--flush");
    expect(frame?.querySelector(".frame-tick--top")).toBeNull();
    expect(frame?.querySelector(".frame-tick--bottom")).toBeTruthy();
  });

  it("applies flush inset when frameInset is flush", () => {
    render(
      <OperateArea
        className="workspace-area"
        label="Campaign record"
        title="Campaign record"
        frameClassName="campaigns-frame"
        frameInset="flush"
      >
        <p>Readout</p>
      </OperateArea>,
    );

    expect(screen.getByRole("region", { name: "Campaign record" }).querySelector(".campaigns-frame")).toHaveClass(
      "frame-cut--flush",
    );
  });

  it("can omit the etched frame for multi-column work bays", () => {
    render(
      <OperateArea className="workspace-area" label="Evaluation record" title="Record" framed={false}>
        <p>Transcript column</p>
      </OperateArea>,
    );

    expect(screen.getByRole("region", { name: "Evaluation record" }).querySelector(".frame-cut")).toBeNull();
    expect(screen.getByText("Transcript column")).toBeInTheDocument();
  });

  it("can arrange the operate head as a ledger plaque", () => {
    render(
      <OperateArea
        className="workspace-area"
        label="Evaluation record"
        title="Examination Transcript"
        description="Session 07"
        framed={false}
        headArrangement="plaque"
        back={<button type="button">Queue</button>}
      >
        <p>Transcript column</p>
      </OperateArea>,
    );

    const head = screen.getByRole("heading", { name: "Examination Transcript" }).closest(".operate-head");
    expect(head).toHaveClass("operate-head--plaque");
    expect(screen.getByRole("button", { name: "Queue" }).closest(".operate-head")).toBe(head);
  });

  it("can omit the operate head when the page supplies chrome another way", () => {
    render(
      <OperateArea className="workspace-area" label="Evaluation record" framed={false} headed={false}>
        <p>Ledger body</p>
      </OperateArea>,
    );

    expect(screen.queryByRole("heading")).toBeNull();
    expect(screen.getByRole("region", { name: "Evaluation record" }).querySelector(".operate-head")).toBeNull();
  });
});
