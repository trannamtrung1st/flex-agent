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
    expect(screen.getByRole("link", { name: "Home" })).toHaveClass("text-link");
    expect(screen.getByRole("link", { name: "Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.getByRole("link", { name: "Activities" })).toHaveClass("text-link");
    expect(screen.getByText("Setup and readiness")).toHaveAttribute("aria-current", "page");
    expect(screen.queryByRole("link", { name: "Setup and readiness" })).not.toBeInTheDocument();
    const trail = screen.getByRole("navigation", { name: "Breadcrumb" });
    expect(trail.textContent).toBe("Home/Activities/Setup and readiness");
  });

  it("renders non-link ancestors when href is omitted", () => {
    render(
      <MemoryRouter>
        <BreadcrumbNav
          items={[
            { label: "Activities", href: "/activities" },
            { label: "Activity" },
            { label: "Setup and readiness", current: true },
          ]}
        />
      </MemoryRouter>,
    );

    expect(screen.getByText("Activity")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Activity" })).not.toBeInTheDocument();
  });
});
