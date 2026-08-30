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

  it("can omit the etched frame for plate grids, split ledgers, and stacked nested records", () => {
    render(
      <OperateArea className="workspace-area" label="Evaluation record" title="Record" framed={false}>
        <p>Transcript column</p>
      </OperateArea>,
    );

    const region = screen.getByRole("region", { name: "Evaluation record" });
    expect(region.querySelector(".frame-cut")).toBeNull();
    const scroll = region.querySelector(":scope > .operate-scroll");
    expect(scroll).toHaveTextContent("Transcript column");
    expect(region.querySelector(":scope > .operate-head")).toBeTruthy();
    expect(scroll?.querySelector(".operate-head")).toBeNull();
    expect(scroll).toHaveClass("operate-scroll");
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
    expect(screen.getByRole("region", { name: "Evaluation record" })).toHaveAttribute("data-flow-gap", "none");
  });

  it("can omit the operate head when the page supplies chrome another way", () => {
    render(
      <OperateArea className="workspace-area" label="Evaluation record" framed={false} headed={false}>
        <p>Ledger body</p>
      </OperateArea>,
    );

    expect(screen.queryByRole("heading")).toBeNull();
    const region = screen.getByRole("region", { name: "Evaluation record" });
    expect(region.querySelector(".operate-head")).toBeNull();
    expect(region.querySelector(":scope > .operate-scroll")).toHaveTextContent("Ledger body");
  });

  it("keeps OperateHead outside the work-body scroller on framed fill pages", () => {
    render(
      <OperateArea
        className="workspace-area"
        label="Setup"
        title="Setup"
        context={<p>Tracks</p>}
      >
        <p>Form</p>
      </OperateArea>,
    );

    const region = screen.getByRole("region", { name: "Setup" });
    const scroll = region.querySelector(":scope > .operate-scroll");
    expect(region.querySelector(":scope > .operate-head")).toBeTruthy();
    expect(scroll).not.toHaveTextContent("Tracks");
    expect(region).toHaveTextContent("Tracks");
    expect(scroll).toHaveTextContent("Form");
    expect(scroll?.querySelector(".operate-head")).toBeNull();
    expect(region.querySelector(":scope > p")).toHaveTextContent("Tracks");
  });

  it("keeps fill composition as a direct head and frame stack", () => {
    render(
      <OperateArea className="workspace-area" label="Campaign registry" title="Campaign registry">
        <p>Records</p>
      </OperateArea>,
    );

    const region = screen.getByRole("region", { name: "Campaign registry" });
    expect(region.querySelector(":scope > .operate-head")).toBeTruthy();
    expect(region.querySelector(":scope > .operate-scroll > .frame-cut")).toBeTruthy();
    expect(region.querySelector(":scope > .frame-cut")).toBeNull();
    expect(region.querySelector(".operate-column--hug")).toBeNull();
    expect(region).toHaveAttribute("data-flow-gap", "6");
  });

  it("wraps a hug column so the title and etched frame share one measure", () => {
    render(
      <OperateArea
        className="workspace-area work-plane work-plane--ceremony"
        composition="hug"
        label="This destination is not available"
        title="This destination is not available"
        frameClassName="ceremony-frame"
      >
        <p>The current authorized relationship cannot use this locator.</p>
      </OperateArea>,
    );

    const region = screen.getByRole("region", { name: "This destination is not available" });
    const column = region.querySelector(":scope > .operate-column--hug");
    expect(column).toBeTruthy();
    expect(column).toHaveAttribute("data-hug-measure", "auto");
    expect(column?.querySelector(":scope > .operate-head")).toBeTruthy();
    expect(column?.querySelector(":scope > .ceremony-frame")).toBeTruthy();
    expect(region.querySelector(":scope > .operate-head")).toBeNull();
    expect(region).toHaveAttribute("data-flow-gap", "none");
  });

  it("can pin a hug column to a named dialog measure", () => {
    render(
      <OperateArea
        className="workspace-area"
        composition="hug"
        hugMeasure="md"
        label="Sign in required"
        title="Sign in required"
        frameClassName="ceremony-frame"
      >
        <p>Sign in through the organization identity provider.</p>
      </OperateArea>,
    );

    expect(screen.getByRole("region", { name: "Sign in required" }).querySelector(":scope > .operate-column--hug")).toHaveAttribute(
      "data-hug-measure",
      "md",
    );
  });
});
