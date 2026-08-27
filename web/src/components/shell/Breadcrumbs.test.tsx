import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { Breadcrumbs } from "./Breadcrumbs";

describe("Breadcrumbs", () => {
  it("does not show raw Enrollment or Assignment locators", () => {
    render(
      <MemoryRouter initialEntries={["/my-work/0198f0a4-7c2e-7e3a-b111-0c4d5e6f7081"]}>
        <Breadcrumbs />
      </MemoryRouter>,
    );

    expect(screen.getByRole("navigation", { name: "Breadcrumb" })).toBeInTheDocument();
    expect(screen.getByText("Assignment")).toBeInTheDocument();
    expect(screen.queryByText(/0198f0a4-7c2e-7e3a-b111-0c4d5e6f7081/i)).not.toBeInTheDocument();
  });

  it("does not show raw Activity, Cohort, or Enrollment locators", () => {
    render(
      <MemoryRouter initialEntries={[
        "/activities/11111111-1111-4111-8111-111111111111/cohorts/22222222-2222-4222-8222-222222222222/enrollments/33333333-3333-4333-8333-333333333333",
      ]}
      >
        <Breadcrumbs />
      </MemoryRouter>,
    );

    expect(screen.getByText("Activity")).toBeInTheDocument();
    expect(screen.getByText("Cohort")).toBeInTheDocument();
    expect(screen.getAllByText("Enrollment").length).toBeGreaterThan(0);
    expect(screen.queryByText(/11111111-1111-4111-8111-111111111111/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/22222222-2222-4222-8222-222222222222/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/33333333-3333-4333-8333-333333333333/i)).not.toBeInTheDocument();
  });

  it("does not show opaque activity locators that are not UUIDs", () => {
    render(
      <MemoryRouter initialEntries={["/activities/act-1/setup"]}>
        <Breadcrumbs />
      </MemoryRouter>,
    );

    expect(screen.getByText("Activity")).toBeInTheDocument();
    expect(screen.getByText("Setup and readiness")).toBeInTheDocument();
    expect(screen.queryByText("act-1")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Activity" })).toHaveAttribute("href", "/activities");
  });
});
