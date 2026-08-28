import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AssessmentSetupPage } from "./AssessmentSetupPage";

describe("AssessmentSetupPage", () => {
  it("loads setup and offers Save draft when permitted", async () => {
    const loadSetup = vi.fn().mockResolvedValue({
      activity_id: "act-1",
      title: "Campaign A",
      revision_number: 1,
      memory_mode: "stable",
      has_activated_cohort: false,
      permitted_actions: ["save_draft", "check_readiness", "activate_cohort"],
    });

    render(
      <MemoryRouter initialEntries={["/activities/act-1/setup"]}>
        <Routes>
          <Route
            path="/activities/:activityId/setup"
            element={(
              <AssessmentSetupPage
                loadSetup={loadSetup}
                saveDraft={vi.fn()}
                checkReadiness={vi.fn()}
                activateCohort={vi.fn()}
              />
            )}
          />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByRole("button", { name: "Save draft" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Setup and readiness" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.getByRole("heading", { name: "Configuration" })).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Campaign title" })).toHaveClass("field-input--wide");
    expect(screen.getByRole("button", { name: "Check readiness" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Activate cohort" })).toBeInTheDocument();
  });
});
