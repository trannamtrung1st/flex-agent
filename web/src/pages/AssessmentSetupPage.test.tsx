import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AssessmentSetupPage, type AssessmentSetupView } from "./AssessmentSetupPage";

const readyView: AssessmentSetupView = {
  activity_id: "act-1",
  title: "P0 Assessment",
  revision_number: 1,
  memory_mode: "disabled",
  has_activated_cohort: false,
  permitted_actions: ["save_draft", "check_readiness", "activate_cohort"],
  overall_severity: "ready",
  issues: [],
};

function renderSetup(view: AssessmentSetupView = readyView) {
  const loadSetup = () => Promise.resolve(view);
  const saveDraft = (_activityId: string, title: string) =>
    Promise.resolve({ ...view, title, revision_number: view.revision_number + 1 });
  const checkReadiness = () => Promise.resolve({ ...view, overall_severity: "ready" });
  const activateCohort = () => Promise.resolve({
    ...view,
    has_activated_cohort: true,
    permitted_actions: [],
    baseline_digest: "a".repeat(64),
  });

  return render(
    <MemoryRouter initialEntries={["/activities/act-1/setup"]}>
      <Routes>
        <Route
          path="/activities/:activityId/setup"
          element={
            <AssessmentSetupPage
              loadSetup={loadSetup}
              saveDraft={saveDraft}
              checkReadiness={checkReadiness}
              activateCohort={activateCohort}
            />
          }
        />
      </Routes>
    </MemoryRouter>,
  );
}

describe("AssessmentSetupPage", () => {
  it("renders the setup heading and default memory copy", async () => {
    renderSetup();
    expect(await screen.findByRole("heading", { name: "Setup and readiness" })).toBeInTheDocument();
    expect(screen.getByText(/approved reads disabled/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Assign Participants" })).not.toBeInTheDocument();
  });

  it("requires confirmation before activation", async () => {
    renderSetup();
    await screen.findByRole("heading", { name: "Setup and readiness" });
    fireEvent.click(screen.getByRole("button", { name: "Activate cohort" }));
    expect(await screen.findByRole("heading", { name: "Activate this empty cohort?" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Confirm activation" }));
    expect(await screen.findByText(/Cohort activated/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Assign Participants" })).not.toBeInTheDocument();
  });
});
