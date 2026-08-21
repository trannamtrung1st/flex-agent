import { fireEvent, render, screen } from "@testing-library/react";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { AssessmentSetupPage, type AssessmentSetupView } from "./AssessmentSetupPage";

const readyView: AssessmentSetupView = {
  activity_id: "act-1",
  title: "P0 Assessment",
  revision_id: "rev-1",
  revision_number: 1,
  memory_mode: "disabled",
  has_activated_cohort: false,
  permitted_actions: ["save_draft", "check_readiness", "activate_cohort"],
  cohort_id: "cohort-1",
  overall_severity: "ready",
  issues: [],
  sources: [{ category: "agent", source_id: "s1", version_id: "v1", content_digest: "b".repeat(64) }],
};

function renderSetup(
  view: AssessmentSetupView = readyView,
  options?: {
    loadError?: Error;
    saveError?: Error;
    checkError?: Error;
    activateError?: Error;
  },
) {
  const loadSetup = () => (options?.loadError ? Promise.reject(options.loadError) : Promise.resolve(view));
  const saveDraft = (_activityId: string, title: string) =>
    options?.saveError
      ? Promise.reject(options.saveError)
      : Promise.resolve({ ...view, title, revision_number: view.revision_number + 1 });
  const checkReadiness = () =>
    options?.checkError ? Promise.reject(options.checkError) : Promise.resolve({ ...view, overall_severity: "ready" });
  const activateCohort = () =>
    options?.activateError
      ? Promise.reject(options.activateError)
      : Promise.resolve({
          ...view,
          has_activated_cohort: true,
          permitted_actions: [],
          baseline_digest: "a".repeat(64),
        });

  const router = createMemoryRouter(
    [
      {
        path: "/activities/:activityId/setup",
        element: (
          <AssessmentSetupPage
            loadSetup={loadSetup}
            saveDraft={saveDraft}
            checkReadiness={checkReadiness}
            activateCohort={activateCohort}
          />
        ),
      },
    ],
    { initialEntries: ["/activities/act-1/setup"] },
  );

  return render(<RouterProvider router={router} />);
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
    expect(await screen.findByRole("heading", { name: "Cohort activated" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Assign Participants" })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Change assessment configuration" }));
    expect(await screen.findByRole("heading", { name: "Create a new cohort to make this change" })).toBeInTheDocument();
  });

  it("shows a blocked readiness heading", async () => {
    renderSetup({
      ...readyView,
      overall_severity: "blocked",
      issues: [{ category: "model_deployment", severity: "blocked", reason_code: "unavailable", recovery_hint: "Select a permitted profile." }],
    });
    expect(await screen.findByRole("heading", { name: "Readiness blocked" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Activate cohort" })).toBeDisabled();
  });

  it("preserves the title and explains a stale draft", async () => {
    renderSetup(readyView, { saveError: new Error("This draft changed") });
    await screen.findByRole("heading", { name: "Setup and readiness" });
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Local title" } });
    fireEvent.click(screen.getByRole("button", { name: "Save draft" }));
    expect(await screen.findByText("This draft changed")).toBeInTheDocument();
    expect(screen.getByLabelText("Campaign title")).toHaveValue("Local title");
  });

  it("removes protected setup when access is lost", async () => {
    renderSetup(readyView, { loadError: new Error("Your access changed") });
    expect(await screen.findByRole("heading", { name: "Your access changed" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Campaign title")).not.toBeInTheDocument();
  });

  it("removes protected setup when a save is forbidden", async () => {
    renderSetup(readyView, { saveError: new Error("Your access changed") });
    await screen.findByRole("heading", { name: "Setup and readiness" });
    fireEvent.click(screen.getByRole("button", { name: "Save draft" }));
    expect(await screen.findByRole("heading", { name: "Your access changed" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Campaign title")).not.toBeInTheDocument();
    expect(screen.queryByRole("list", { name: "Selected source revisions" })).not.toBeInTheDocument();
  });

  it("removes protected setup when readiness is forbidden after load", async () => {
    renderSetup(readyView, { checkError: new Error("Your access changed") });
    await screen.findByRole("heading", { name: "Setup and readiness" });
    expect(screen.getByRole("list", { name: "Selected source revisions" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Check readiness" }));
    expect(await screen.findByRole("heading", { name: "Your access changed" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Campaign title")).not.toBeInTheDocument();
    expect(screen.queryByRole("list", { name: "Selected source revisions" })).not.toBeInTheDocument();
  });

  it("removes protected setup when activation is forbidden after load", async () => {
    renderSetup(readyView, { activateError: new Error("Your access changed") });
    await screen.findByRole("heading", { name: "Setup and readiness" });
    fireEvent.click(screen.getByRole("button", { name: "Activate cohort" }));
    fireEvent.click(screen.getByRole("button", { name: "Confirm activation" }));
    expect(await screen.findByRole("heading", { name: "Your access changed" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Campaign title")).not.toBeInTheDocument();
    expect(screen.queryByRole("list", { name: "Selected source revisions" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Activate this empty cohort?" })).not.toBeInTheDocument();
  });
});
