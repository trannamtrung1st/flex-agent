import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { BreadcrumbNav } from "./BreadcrumbNav";

describe("BreadcrumbNav", () => {
  it("renders home and linked trail items", () => {
    render(
      <MemoryRouter>
        <BreadcrumbNav
          items={[
            { label: "Activities", href: "/activities" },
            { label: "Setup and readiness", current: true },
          ]}
        />
      </MemoryRouter>,
    );

    expect(screen.getByRole("navigation", { name: "Breadcrumb" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Home" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: "Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.getByText("Setup and readiness")).toHaveAttribute("aria-current", "page");
    expect(screen.queryByRole("link", { name: "Setup and readiness" })).not.toBeInTheDocument();
  });
});
