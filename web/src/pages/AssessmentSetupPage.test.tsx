import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import type { AssessmentSetupView } from "../api/production-assessment";
import { CAMPAIGN_TITLE_PLACEHOLDER } from "../design-system/components/fields/fieldFormat";
import { AssessmentSetupPage, type AssessmentSetupPageProps } from "./AssessmentSetupPage";

function view(overrides: Partial<AssessmentSetupView> = {}): AssessmentSetupView {
  return {
    activity_id: "act-1",
    title: "Campaign A",
    revision_number: 1,
    memory_mode: "stable",
    has_activated_cohort: false,
    permitted_actions: ["save_draft", "check_readiness", "activate_cohort"],
    ...overrides,
  };
}

function renderSetup(
  next: AssessmentSetupView,
  {
    saveDraft = vi.fn<AssessmentSetupPageProps["saveDraft"]>(),
    checkReadiness = vi.fn<AssessmentSetupPageProps["checkReadiness"]>(),
    activateCohort = vi.fn<AssessmentSetupPageProps["activateCohort"]>(),
    initialEntry = "/activities/act-1/setup",
  }: {
    saveDraft?: AssessmentSetupPageProps["saveDraft"];
    checkReadiness?: AssessmentSetupPageProps["checkReadiness"];
    activateCohort?: AssessmentSetupPageProps["activateCohort"];
    initialEntry?: string;
  } = {},
) {
  const router = createMemoryRouter(
    [
      {
        path: "/activities/:activityId/setup",
        element: (
          <AssessmentSetupPage
            loadSetup={vi.fn().mockResolvedValue(next)}
            saveDraft={saveDraft}
            checkReadiness={checkReadiness}
            activateCohort={activateCohort}
          />
        ),
      },
      {
        path: "/activities",
        element: <h1>Activities list</h1>,
      },
    ],
    { initialEntries: [initialEntry] },
  );

  return {
    router,
    ...render(<RouterProvider router={router} />),
  };
}

describe("AssessmentSetupPage", () => {
  it("loads a ceremony-on-record station with next action, tracks, and permitted keys", async () => {
    renderSetup(view());

    expect(await screen.findByRole("button", { name: "Save draft" })).toBeInTheDocument();
    const region = screen.getByRole("region", { name: "Setup and readiness" });
    expect(region).toHaveClass("record-plane", "record-plane--setup");
    expect(region.querySelector(".setup-ceremony")).toBeTruthy();
    expect(region.querySelector(".create-ceremony__scroll")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Save draft" }).closest(".create-ceremony__scroll")).toBeNull();
    expect(screen.getByRole("heading", { name: "Setup and readiness" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.getByText("Check readiness on revision 1, then activate this cohort.")).toBeInTheDocument();
    const tracks = screen.getByLabelText("Setup tracks");
    expect(region.querySelector(".frame-cut")).toContainElement(tracks);
    expect(tracks.closest(".setup-ceremony")).toBeTruthy();
    expect(tracks.closest(".create-ceremony__scroll")).toBeNull();
    expect(region.querySelector(":scope > .readout-grid")).toBeNull();
    expect(region.querySelector(".readout-grid")).toBeTruthy();
    expect(screen.getByText("Local")).toBeInTheDocument();
    expect(screen.getByText("Draft")).toBeInTheDocument();
    expect(screen.getByText("Readiness")).toBeInTheDocument();
    expect(screen.getByText("Cohort")).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Campaign title" })).toHaveClass("field-input--wide");
    expect(screen.getByRole("textbox", { name: "Campaign title" })).toHaveAttribute(
      "placeholder",
      CAMPAIGN_TITLE_PLACEHOLDER,
    );
    expect(screen.getByText("Saved as revision 1")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Check readiness" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Activate cohort" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Configuration" })).not.toBeInTheDocument();
    expect(screen.queryByRole("group", { name: "Campaign identity" })).not.toBeInTheDocument();
  });

  it("omits unarmed keys instead of leaving them disabled", async () => {
    renderSetup(view({ permitted_actions: ["save_draft"] }));

    expect(await screen.findByRole("button", { name: "Save draft" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Check readiness" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Activate cohort" })).not.toBeInTheDocument();
  });

  it("marks local unsaved when the title differs from the saved revision", async () => {
    renderSetup(view({ permitted_actions: ["save_draft", "check_readiness"] }));

    const field = await screen.findByRole("textbox", { name: "Campaign title" });
    fireEvent.change(field, { target: { value: "Campaign B" } });

    expect(screen.getByText("Unsaved")).toBeInTheDocument();
    expect(field).toHaveAccessibleDescription("Unsaved changes");
    expect(screen.getByText("Save this draft, then check readiness.")).toBeInTheDocument();
  });

  it("lists readiness blockers and withholds Activate cohort", async () => {
    renderSetup(view({
      permitted_actions: ["save_draft", "check_readiness"],
      issues: [{
        category: "timing",
        severity: "blocker",
        reason_code: "window",
        recovery_hint: "Set a valid session window.",
      }],
    }));

    expect(await screen.findByText("Set a valid session window.")).toBeInTheDocument();
    expect(screen.getByText("Blocked")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Activate cohort" })).not.toBeInTheDocument();
  });

  it("arms Activate cohort only after a current ready result and confirms before calling activate", async () => {
    const activateCohort = vi.fn();
    renderSetup(view({ issues: [] }), { activateCohort });

    fireEvent.click(await screen.findByRole("button", { name: "Activate cohort" }));

    expect(screen.getByRole("dialog", { name: "Activate this cohort?" })).toBeInTheDocument();
    expect(activateCohort).not.toHaveBeenCalled();
  });

  it("presents an activated baseline and the Participants handoff", async () => {
    renderSetup(view({
      has_activated_cohort: true,
      permitted_actions: [],
      cohort_id: "coh-1",
      baseline_digest: "digest-a",
      verification_status: "verified",
    }));

    expect(await screen.findByRole("heading", { name: "Activated cohort" })).toBeInTheDocument();
    expect(screen.getByText("Ready")).toBeInTheDocument();
    expect(screen.getByText("Activated")).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Campaign title" })).toHaveClass("is-frozen");
    expect(screen.getByRole("link", { name: "Assign Participants" })).toHaveAttribute(
      "href",
      "/activities/act-1/cohorts/coh-1/enrollments",
    );
    expect(screen.getByRole("link", { name: "Assign Participants" }).closest(".create-ceremony__scroll")).toBeNull();
    expect(screen.queryByRole("button", { name: "Save draft" })).not.toBeInTheDocument();
  });

  it("warns before leaving with unsaved changes and can stay on page", async () => {
    renderSetup(view({ permitted_actions: ["save_draft", "check_readiness"] }));

    const field = await screen.findByRole("textbox", { name: "Campaign title" });
    fireEvent.change(field, { target: { value: "Campaign B" } });
    fireEvent.click(screen.getByRole("link", { name: "Activities" }));

    const dialog = await screen.findByRole("dialog", { name: "Unsaved changes" });
    expect(dialog).toHaveTextContent(
      "Your latest changes have not been saved. Save them before leaving this page, or leave and discard them.",
    );
    expect(screen.getByRole("button", { name: "Save draft and leave" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Stay on page" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Leave without saving" })).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Stay on page" }));

    await waitFor(() => {
      expect(screen.queryByRole("dialog", { name: "Unsaved changes" })).not.toBeInTheDocument();
    });
    expect(screen.getByRole("heading", { name: "Setup and readiness" })).toBeInTheDocument();
    expect(field).toHaveValue("Campaign B");
  });

  it("discards local changes and leaves when confirmed", async () => {
    const { router } = renderSetup(view({ permitted_actions: ["save_draft", "check_readiness"] }));

    const field = await screen.findByRole("textbox", { name: "Campaign title" });
    fireEvent.change(field, { target: { value: "Campaign B" } });
    fireEvent.click(screen.getByRole("link", { name: "Activities" }));

    fireEvent.click(await screen.findByRole("button", { name: "Leave without saving" }));

    expect(await screen.findByRole("heading", { name: "Activities list" })).toBeInTheDocument();
    expect(router.state.location.pathname).toBe("/activities");
  });

  it("saves before leaving when Save draft and leave succeeds", async () => {
    const saveDraft = vi.fn().mockResolvedValue(view({ title: "Campaign B", revision_number: 2 }));
    const { router } = renderSetup(view({ permitted_actions: ["save_draft", "check_readiness"] }), { saveDraft });

    const field = await screen.findByRole("textbox", { name: "Campaign title" });
    fireEvent.change(field, { target: { value: "Campaign B" } });
    fireEvent.click(screen.getByRole("link", { name: "Activities" }));

    fireEvent.click(await screen.findByRole("button", { name: "Save draft and leave" }));

    await waitFor(() => {
      expect(saveDraft).toHaveBeenCalledWith("act-1", "Campaign B", 1);
    });
    expect(await screen.findByRole("heading", { name: "Activities list" })).toBeInTheDocument();
    expect(router.state.location.pathname).toBe("/activities");
  });

  it("keeps the administrator on setup when save before leave fails", async () => {
    const saveDraft = vi.fn().mockRejectedValue(new Error("save failed"));
    renderSetup(view({ permitted_actions: ["save_draft", "check_readiness"] }), { saveDraft });

    const field = await screen.findByRole("textbox", { name: "Campaign title" });
    fireEvent.change(field, { target: { value: "Campaign B" } });
    fireEvent.click(screen.getByRole("link", { name: "Activities" }));
    fireEvent.click(await screen.findByRole("button", { name: "Save draft and leave" }));

    await waitFor(() => {
      expect(saveDraft).toHaveBeenCalled();
    });
    await waitFor(() => {
      expect(screen.queryByRole("dialog", { name: "Unsaved changes" })).not.toBeInTheDocument();
    });
    expect(screen.getByRole("heading", { name: "Setup and readiness" })).toBeInTheDocument();
    expect(screen.getByText("This draft could not be saved. Reconcile before retrying.")).toBeInTheDocument();
    expect(field).toHaveValue("Campaign B");
  });
});
