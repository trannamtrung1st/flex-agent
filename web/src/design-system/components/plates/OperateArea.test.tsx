import { render, screen } from "@testing-library/react";
import { OperateHead } from "../chrome/OperateHead";
import { OperateArea, OperateAreaHost } from "./OperateArea";

describe("OperateArea", () => {
  it("is a labeled region, not a second main landmark", () => {
    render(
      <main id="main-content">
        <OperateArea label="Campaign registry" title="Campaign registry">
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
        label="Campaign registry"
        title="Campaign registry"
        frame="datatable"
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

  it("keeps only the bottom center tick on non-flush record frames", () => {
    render(
      <OperateArea label="Record" title="Record" frame="record">
        <p>Readouts</p>
      </OperateArea>,
    );

    const frame = screen.getByRole("region", { name: "Record" }).querySelector(".record-frame");
    expect(frame).not.toHaveClass("frame-cut--flush");
    expect(frame?.querySelector(".frame-tick--top")).toBeNull();
    expect(frame?.querySelector(".frame-tick--bottom")).toBeTruthy();
  });

  it("applies flush inset when frameInset is flush", () => {
    render(
      <OperateArea label="Record" title="Record" frame="record" frameInset="flush">
        <p>Readout</p>
      </OperateArea>,
    );

    expect(screen.getByRole("region", { name: "Record" }).querySelector(".record-frame")).toHaveClass("frame-cut--flush");
  });

  it("can omit the etched frame for plate grids, split ledgers, and stacked nested records", () => {
    render(
      <OperateArea label="Evaluation record" title="Record" framed={false}>
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

  it("lets a domain wrapper supply a plaque OperateHead", () => {
    render(
      <OperateAreaHost
        label="Evaluation record"
        framed={false}
        gap="none"
        head={
          <OperateHead
            arrangement="plaque"
            title="Examination Transcript"
            description="Session 07"
            back={<button type="button">Queue</button>}
          />
        }
      >
        <p>Transcript column</p>
      </OperateAreaHost>,
    );

    const head = screen.getByRole("heading", { name: "Examination Transcript" }).closest(".operate-head");
    expect(head).toHaveClass("operate-head--plaque");
    expect(screen.getByRole("button", { name: "Queue" }).closest(".operate-head")).toBe(head);
    expect(screen.getByRole("region", { name: "Evaluation record" })).toHaveAttribute("data-flow-gap", "none");
  });

  it("can omit the operate head when the page supplies chrome another way", () => {
    render(
      <OperateArea label="Evaluation record" framed={false} headed={false}>
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
      <OperateArea label="Campaign registry" title="Campaign registry">
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
        bay="ceremony"
        composition="hug"
        label="This destination is not available"
        title="This destination is not available"
        frame="ceremony"
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
        composition="hug"
        hugMeasure="md"
        label="Sign in required"
        title="Sign in required"
        frame="ceremony"
      >
        <p>Sign in through the organization identity provider.</p>
      </OperateArea>,
    );

    expect(screen.getByRole("region", { name: "Sign in required" }).querySelector(":scope > .operate-column--hug")).toHaveAttribute(
      "data-hug-measure",
      "md",
    );
  });

  it("owns the workspace host classes when className is omitted", () => {
    render(
      <OperateArea label="Home" title="Home">
        <p>Destinations</p>
      </OperateArea>,
    );

    expect(screen.getByRole("region", { name: "Home" })).toHaveClass("workspace-area", "work-plane", "composition-stack");
  });

  it("selects typed frame variants and default flush inset for registry tables", () => {
    render(
      <OperateArea label="Activities" title="Activities" bay="registry" frame="registry">
        <p>Rows</p>
      </OperateArea>,
    );

    const frame = screen.getByRole("region", { name: "Activities" }).querySelector(".registry-frame");
    expect(frame).toHaveClass("datatable-frame", "frame-cut--flush");
  });

  it("selects registry hug and additive danger from typed props", () => {
    const { rerender } = render(
      <OperateArea label="Participants" title="Participants" bay="registry" hug="registry">
        <p>Rows</p>
      </OperateArea>,
    );
    expect(screen.getByRole("region", { name: "Participants" })).toHaveClass("registry-wall", "registry-wall--hug");

    rerender(
      <OperateArea label="Denied" title="Denied" bay="ceremony" danger>
        <p>Note</p>
      </OperateArea>,
    );
    expect(screen.getByRole("region", { name: "Denied" })).toHaveClass(
      "work-plane--ceremony",
      "workspace-area--danger",
    );

    rerender(
      <OperateAreaHost
        label="Record"
        title="Record"
        hostClassName="workspace-area extra-host"
        className="is-released"
      >
        <p>Ledger</p>
      </OperateAreaHost>,
    );
    expect(screen.getByRole("region", { name: "Record" })).toHaveClass("workspace-area", "extra-host", "is-released");
    expect(screen.getByRole("region", { name: "Record" })).not.toHaveClass("work-plane");
  });

  it("ignores hug that does not match the selected bay", () => {
    render(
      <OperateArea label="Home" title="Home" hug="registry">
        <p>Destinations</p>
      </OperateArea>,
    );

    expect(screen.getByRole("region", { name: "Home" })).not.toHaveClass("registry-wall--hug");
    expect(screen.getByRole("region", { name: "Home" })).not.toHaveClass("assignment-board--hug");
  });

  it("lets domain wrappers replace the host bundle without workspace-area or registry hug", () => {
    render(
      <OperateAreaHost label="Host" title="Host" hostClassName="replacement-host" hug="registry">
        <p>Rows</p>
      </OperateAreaHost>,
    );

    const region = screen.getByRole("region", { name: "Host" });
    expect(region).toHaveClass("replacement-host");
    expect(region).not.toHaveClass("workspace-area");
    expect(region).not.toHaveClass("registry-wall--hug");
  });
});
