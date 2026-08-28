import { render, screen } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { DESIGN_LAB_BASENAME, designLabRoutes } from "./app/router";

function renderLab(url: string) {
  const router = createMemoryRouter(designLabRoutes, {
    basename: DESIGN_LAB_BASENAME,
    initialEntries: [url],
  });
  return render(<RouterProvider router={router} />);
}

describe("SurfacesPage", () => {
  it("renders the copied channel index with catalog destinations", () => {
    renderLab(`${DESIGN_LAB_BASENAME}/surfaces`);
    expect(screen.getByRole("heading", { name: "Prototype Surfaces" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Channel index" })).toHaveAttribute(
      "href",
      `${DESIGN_LAB_BASENAME}/surfaces`,
    );
    expect(screen.getByRole("link", { name: "Open Status Bays" })).toHaveAttribute(
      "href",
      `${DESIGN_LAB_BASENAME}/participant-home`,
    );
    expect(document.querySelector(".channel-copy")).toHaveClass("composition-stack");
    expect(document.querySelector(".channel-roster")).toHaveClass("composition-stack");
    expect(screen.getByRole("link", { name: "Open Assignment Station" })).toHaveAttribute(
      "href",
      `${DESIGN_LAB_BASENAME}/participant-journey`,
    );
    expect(screen.getByRole("link", { name: "Open Examination Console" })).toHaveAttribute(
      "href",
      `${DESIGN_LAB_BASENAME}/participant-session`,
    );
    expect(screen.getByRole("link", { name: "Open Administration" })).toHaveAttribute(
      "href",
      `${DESIGN_LAB_BASENAME}/admin-console`,
    );
    expect(screen.getByRole("link", { name: "Open Review Console" })).toHaveAttribute(
      "href",
      `${DESIGN_LAB_BASENAME}/reviewer-console`,
    );
    expect(screen.getByRole("link", { name: "Open Component Deck" })).toHaveAttribute(
      "href",
      `${DESIGN_LAB_BASENAME}/shared/gallery`,
    );
  });
});

describe("design lab routes", () => {
  it("opens the Assignment Station well on composition stacks", () => {
    renderLab("/design-lab/participant-journey");
    expect(screen.getByRole("heading", { name: "Assignment briefing" })).toBeInTheDocument();
    expect(document.querySelector("article.work-well")).toHaveClass("composition-stack");
    expect(document.querySelector(".work-well__head")).toHaveClass("composition-stack");
  });

  it("keeps the published-result seal spacing off the work-well head gap", () => {
    renderLab("/design-lab/participant-journey?demo=result-released");
    const seal = document.querySelector(".work-well__seal");
    expect(seal?.closest(".work-well__head")).toHaveAttribute("data-flow-gap", "none");
    expect(screen.getByRole("heading", { name: "Result released" }).closest(".composition-stack")).toHaveAttribute(
      "data-flow-gap",
      "2",
    );
  });

  it("opens Status Bays from the catalog path", () => {
    renderLab("/design-lab/participant-home");
    expect(screen.getByRole("heading", { name: "Assigned work" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Assigned work by record state" })).toBeInTheDocument();
    expect(document.querySelector(".bay")).toHaveClass("composition-stack");
    expect(document.querySelector("#main-content")?.querySelector(".composition-inset")).toBeNull();
    expect(document.querySelector(".board-frame.frame-cut")).toHaveClass("frame-cut--flush");
  });

  it("centers the participant-home empty plate inside the board frame", () => {
    renderLab("/design-lab/participant-home?demo=empty");
    const emptyPlate = screen.getByText("No assigned work").closest(".empty-plate");
    expect(emptyPlate).toBeTruthy();
    expect(emptyPlate).not.toHaveClass("empty-plate--inset");
    expect(emptyPlate?.closest(".board-empty")).toBeTruthy();
  });

  it("opens the Review Console queue on OperateArea", () => {
    renderLab("/design-lab/reviewer-console");
    expect(screen.getByRole("heading", { name: "Review queue" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Review queue" })).toBeInTheDocument();
    expect(document.querySelector("#main-content")?.querySelector(".composition-inset")).toBeNull();
    expect(document.querySelector(".datatable-frame.frame-cut")).toHaveClass("frame-cut--flush");
  });

  it("opens the Component Deck from the catalog path", () => {
    renderLab("/design-lab/shared/gallery");
    expect(screen.getByRole("heading", { name: "Shared component deck" })).toBeInTheDocument();
    const frame = document.querySelector(".frame-demo.frame-cut");
    expect(frame).toBeTruthy();
    expect(frame?.querySelector(".frame-tick--top")).toBeTruthy();
    expect(frame?.querySelector(".frame-tick--bottom")).toBeTruthy();
  });

  it("shows a non-disclosing unknown-channel state", () => {
    renderLab("/design-lab/does-not-exist");
    expect(screen.getByText("Channel not found")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Return to channel index" })).toBeInTheDocument();
  });
});
