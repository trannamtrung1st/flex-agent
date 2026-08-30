import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { Breadcrumbs } from "./Breadcrumbs";

function renderTrail(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Breadcrumbs />
    </MemoryRouter>,
  );
}

describe("Breadcrumbs", () => {
  it("does not show raw Enrollment or Assignment locators", () => {
    renderTrail("/my-work/0198f0a4-7c2e-7e3a-b111-0c4d5e6f7081");

    expect(screen.getByRole("navigation", { name: "Breadcrumb" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "My work" })).toHaveAttribute("href", "/my-work");
    expect(screen.getByText("Assignment")).toHaveAttribute("aria-current", "page");
    expect(screen.queryByText(/0198f0a4-7c2e-7e3a-b111-0c4d5e6f7081/i)).not.toBeInTheDocument();
  });

  it("maps the Participants roster to reachable destinations only", () => {
    renderTrail(
      "/activities/11111111-1111-4111-8111-111111111111/cohorts/22222222-2222-4222-8222-222222222222/enrollments",
    );

    expect(screen.getByRole("link", { name: "Home" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: "Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.getByRole("link", { name: "Setup and readiness" })).toHaveAttribute(
      "href",
      "/activities/11111111-1111-4111-8111-111111111111/setup",
    );
    expect(screen.getByText("Participants")).toHaveAttribute("aria-current", "page");
    expect(screen.queryByText("Activity")).not.toBeInTheDocument();
    expect(screen.queryByText("Cohorts")).not.toBeInTheDocument();
    expect(screen.queryByText("Cohort")).not.toBeInTheDocument();
    expect(screen.queryByText("Enrollment")).not.toBeInTheDocument();
    expect(screen.queryByText(/11111111-1111-4111-8111-111111111111/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/22222222-2222-4222-8222-222222222222/i)).not.toBeInTheDocument();
  });

  it("maps Enrollment detail through Participants, not locator segments", () => {
    renderTrail(
      "/activities/11111111-1111-4111-8111-111111111111/cohorts/22222222-2222-4222-8222-222222222222/enrollments/33333333-3333-4333-8333-333333333333",
    );

    expect(screen.getByRole("link", { name: "Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.getByRole("link", { name: "Setup and readiness" })).toHaveAttribute(
      "href",
      "/activities/11111111-1111-4111-8111-111111111111/setup",
    );
    expect(screen.getByRole("link", { name: "Participants" })).toHaveAttribute(
      "href",
      "/activities/11111111-1111-4111-8111-111111111111/cohorts/22222222-2222-4222-8222-222222222222/enrollments",
    );
    expect(screen.getByText("Enrollment")).toHaveAttribute("aria-current", "page");
    expect(screen.queryByText("Activity")).not.toBeInTheDocument();
    expect(screen.queryByText("Cohorts")).not.toBeInTheDocument();
    expect(screen.queryByText("Cohort")).not.toBeInTheDocument();
    expect(screen.queryByText(/33333333-3333-4333-8333-333333333333/i)).not.toBeInTheDocument();
  });

  it("marks Activities as current on the index", () => {
    renderTrail("/activities");

    expect(screen.getByRole("link", { name: "Home" })).toHaveAttribute("href", "/");
    expect(screen.getByText("Activities")).toHaveAttribute("aria-current", "page");
    expect(screen.queryByRole("link", { name: "Activities" })).not.toBeInTheDocument();
  });

  it("labels the Campaign create locator instead of echoing new", () => {
    renderTrail("/activities/new");

    expect(screen.getByRole("link", { name: "Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.getByText("Create assessment Campaign")).toHaveAttribute("aria-current", "page");
    expect(screen.queryByText("new")).not.toBeInTheDocument();
  });

  it("maps Setup to Activities without an Activity locator crumb", () => {
    renderTrail("/activities/act-1/setup");

    expect(screen.getByRole("link", { name: "Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.getByText("Setup and readiness")).toHaveAttribute("aria-current", "page");
    expect(screen.queryByText("Activity")).not.toBeInTheDocument();
    expect(screen.queryByText("act-1")).not.toBeInTheDocument();
  });

  it("maps a bare Activity locator to Setup, matching the setup redirect", () => {
    renderTrail("/activities/act-1");

    expect(screen.getByRole("link", { name: "Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.getByText("Setup and readiness")).toHaveAttribute("aria-current", "page");
    expect(screen.queryByText("act-1")).not.toBeInTheDocument();
  });

  it("does not echo an unknown locator as a breadcrumb", () => {
    renderTrail("/not-a-destination");

    expect(screen.queryByRole("navigation", { name: "Breadcrumb" })).not.toBeInTheDocument();
    expect(screen.queryByText("not-a-destination")).not.toBeInTheDocument();
  });

  it("does not render a Home-only trail for a prefix-known locator that is not a destination", () => {
    renderTrail("/activities/act-1/not-a-leaf");

    expect(screen.queryByRole("navigation", { name: "Breadcrumb" })).not.toBeInTheDocument();
    expect(screen.queryByText("not-a-leaf")).not.toBeInTheDocument();
  });
});
