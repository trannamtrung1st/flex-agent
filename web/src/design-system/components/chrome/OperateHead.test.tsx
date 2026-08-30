import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { BackKey } from "../keys/BackKey";
import { OperateHead } from "./OperateHead";

describe("OperateHead", () => {
  it("keeps the default stack order for index pages", () => {
    render(<OperateHead title="Review queue" description="Ranked by receipt time." />);
    const head = screen.getByRole("heading", { name: "Review queue" }).closest(".operate-head");
    expect(head).not.toHaveClass("operate-head--plaque");
    expect(head).not.toHaveAttribute("data-head-arrange", "plaque");
    expect(head?.querySelector(".operate-head-mast")).toBeNull();
    const copy = head?.querySelector(".operate-head-copy");
    expect(copy).toHaveClass("composition-stack");
    expect(copy).toHaveAttribute("data-flow-gap", "2.5");
    expect(copy).toContainElement(screen.getByRole("heading", { name: "Review queue" }));
    expect(copy).toContainElement(screen.getByText("Ranked by receipt time."));
  });

  it("trails the nested-record return key beside the copy cluster", () => {
    render(
      <OperateHead
        title="Campaign Record"
        description="Configuration, activation state, and enrollment context."
        back={<button type="button">Campaigns</button>}
      />,
    );
    const heading = screen.getByRole("heading", { name: "Campaign Record" });
    const description = screen.getByText("Configuration, activation state, and enrollment context.");
    const back = screen.getByRole("button", { name: "Campaigns" });
    const copy = heading.closest(".operate-head-copy");
    const mast = heading.closest(".operate-head-mast");
    expect(copy).toBeTruthy();
    expect(copy).toContainElement(description);
    expect(copy).not.toContainElement(back);
    expect(mast).toBeTruthy();
    expect(mast).toContainElement(copy as HTMLElement);
    expect(mast).toContainElement(back);
    expect(mast).toHaveClass("composition-inline");
    expect(mast).toHaveAttribute("data-flow-justify", "between");
    expect(mast).toHaveAttribute("data-flow-align", "start");
    expect(heading.compareDocumentPosition(back) & Node.DOCUMENT_POSITION_FOLLOWING).toBeGreaterThan(0);
  });

  it("keeps production link BackKey beside the copy cluster when TooltipHost wraps the key", () => {
    render(
      <MemoryRouter>
        <OperateHead
          title="Setup and readiness"
          back={<BackKey to="/activities" label="Activities" />}
        />
      </MemoryRouter>,
    );
    const heading = screen.getByRole("heading", { name: "Setup and readiness" });
    const back = screen.getByRole("link", { name: "Activities" });
    const mast = heading.closest(".operate-head-mast");
    expect(mast).toBeTruthy();
    expect(back.closest(".tip-host")).toBeTruthy();
    expect(mast).toContainElement(back.closest(".tip-host"));
    expect(heading.closest(".operate-head-copy")).not.toContainElement(back.closest(".tip-host"));
  });

  it("arranges a ledger plaque as back, centered title plus status, and trailing session", () => {
    render(
      <OperateHead
        arrangement="plaque"
        title="Examination Transcript — The Overlay Ledger"
        description="Session 07 · FXA-7C19-2A07"
        back={<button type="button">Queue</button>}
        headExtra={<span>Sealed</span>}
      />,
    );
    const head = screen.getByRole("heading", { name: "Examination Transcript — The Overlay Ledger" }).closest(".operate-head");
    expect(head?.tagName).toBe("HEADER");
    expect(head).toHaveClass("operate-head--plaque");
    expect(head).toHaveAttribute("data-head-arrange", "plaque");
    expect(screen.getByRole("button", { name: "Queue" }).closest(".operate-head")).toBe(head);
    expect(screen.getByRole("button", { name: "Queue" }).closest(".operate-head-mast")).toBeNull();
    expect(screen.getByText("Sealed").closest(".operate-head-cluster")).toBeTruthy();
  });
});
