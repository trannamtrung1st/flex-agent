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
  it("opens Status Bays from the catalog path", () => {
    renderLab("/design-lab/participant-home");
    expect(screen.getByRole("heading", { name: "Assigned work" })).toBeInTheDocument();
  });

  it("opens the Component Deck from the catalog path", () => {
    renderLab("/design-lab/shared/gallery");
    expect(screen.getByRole("heading", { name: "Shared component deck" })).toBeInTheDocument();
  });

  it("shows a non-disclosing unknown-channel state", () => {
    renderLab("/design-lab/does-not-exist");
    expect(screen.getByText("Channel not found")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Return to channel index" })).toBeInTheDocument();
  });
});
