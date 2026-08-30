import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import type { AssessmentSetupView } from "../api/production-assessment";
import { CAMPAIGN_TITLE_PLACEHOLDER, SETUP_RESOLVED_NOTE } from "../design-system/components/fields/fieldFormat";
import { ToastHost } from "../design-system";
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
    ...render(
      <ToastHost>
        <RouterProvider router={router} />
      </ToastHost>,
    ),
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
    expect(within(tracks).getByText("Cohort")).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "Campaign title" })).toHaveClass("field-input--wide");
    expect(screen.getByRole("textbox", { name: "Campaign title" })).toHaveAttribute(
      "placeholder",
      CAMPAIGN_TITLE_PLACEHOLDER,
    );
    expect(screen.getByRole("group", { name: "Task and Submission requirements" })).toHaveClass("form-section");
    expect(screen.getByRole("group", { name: "Agent and Harness" })).toHaveClass("form-section");
    expect(screen.getByRole("group", { name: "Assessment behavior" })).toHaveClass("form-section");
    expect(screen.getByRole("group", { name: "Timing and Attempts" })).toHaveClass("form-section");
    expect(screen.getByRole("group", { name: "Memory and capabilities" })).toHaveClass("form-section");
    expect(screen.getByRole("group", { name: "Review and Release requirements" })).toHaveClass("form-section");
    expect(screen.getByRole("group", { name: "Cohort" })).toHaveClass("form-section");
    expect(region.querySelector(".create-ceremony__scroll")?.querySelector(".form-divider")).toBeNull();
    expect(
      screen.getByRole("group", { name: "Task and Submission requirements" }).nextElementSibling,
    ).toBe(screen.getByRole("group", { name: "Agent and Harness" }));
    expect(screen.getByRole("group", { name: "Task and Submission requirements" }).parentElement).toHaveAttribute(
      "data-flow-gap",
      "6",
    );
    const memory = screen.getByRole("textbox", { name: "Memory" });
    expect(memory).toHaveClass("is-frozen");
    expect(memory).toHaveValue("Stable — new long-term learning disabled");
    expect(memory).not.toHaveAccessibleDescription(SETUP_RESOLVED_NOTE);
    const resolved = screen.getByText(SETUP_RESOLVED_NOTE);
    expect(resolved).toHaveClass("advisory-copy");
    expect(resolved.closest(".workspace-alert")).toBeTruthy();
    expect(resolved.closest(".field-hint")).toBeNull();
    const titleField = screen.getByRole("textbox", { name: "Campaign title" });
    expect(resolved.compareDocumentPosition(titleField) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(screen.getAllByText(SETUP_RESOLVED_NOTE)).toHaveLength(1);
    expect(screen.getByText("Note")).toBeInTheDocument();
    expect(document.querySelector(".setup-ceremony__memory")).toBeNull();
    expect(screen.getByRole("textbox", { name: "Agent" })).toHaveValue("Not bound");
    expect(screen.getByRole("textbox", { name: "Agent" })).toHaveClass("is-frozen");
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

  it("etches bound Task and Agent revisions on frozen fields", async () => {
    renderSetup(view({
      task_title: "Shoreline brief",
      sources: [{
        category: "agent",
        source_id: "examiner-core",
        version_id: "v2",
        content_digest: "b".repeat(64),
      }],
    }));

    expect(await screen.findByRole("textbox", { name: "Task" })).toHaveValue("Shoreline brief");
    expect(screen.getByRole("textbox", { name: "Task" })).toHaveClass("is-frozen");
    expect(screen.getByRole("textbox", { name: "Agent" })).toHaveValue("examiner-core · v2");
  });

  it("marks local unsaved when the title differs from the saved revision", async () => {
    renderSetup(view({ permitted_actions: ["save_draft", "check_readiness"] }));

    const field = await screen.findByRole("textbox", { name: "Campaign title" });
    fireEvent.change(field, { target: { value: "Campaign B" } });

    expect(screen.getByText("Unsaved")).toBeInTheDocument();
    expect(field).toHaveAccessibleDescription("Unsaved changes");
    expect(screen.getByText("Save this draft, then check readiness.")).toBeInTheDocument();
  });

  it("toasts when a draft save succeeds", async () => {
    const saveDraft = vi.fn().mockResolvedValue(view({ title: "Campaign B", revision_number: 2 }));
    renderSetup(view({ permitted_actions: ["save_draft", "check_readiness"] }), { saveDraft });

    const field = await screen.findByRole("textbox", { name: "Campaign title" });
    fireEvent.change(field, { target: { value: "Campaign B" } });
    fireEvent.click(screen.getByRole("button", { name: "Save draft" }));

    expect(await screen.findByText("This revision is saved.")).toBeInTheDocument();
    expect(screen.getByText("This revision is saved.").closest(".toast")).toHaveAttribute("role", "status");
    expect(saveDraft).toHaveBeenCalledWith("act-1", "Campaign B", 1);
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
    expect(screen.getByRole("heading", { name: "Readiness blocked" })).toBeInTheDocument();
    const timezone = screen.getByRole("textbox", { name: "Timezone" });
    expect(screen.getByRole("link", { name: "Set a valid session window." })).toHaveAttribute(
      "href",
      `#${timezone.getAttribute("id")}`,
    );
    expect(screen.getByText("Blocked")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Activate cohort" })).not.toBeInTheDocument();
  });

  it("moves focus to Readiness blocked after Check readiness returns blockers", async () => {
    const checkReadiness = vi.fn().mockResolvedValue(view({
      permitted_actions: ["save_draft", "check_readiness"],
      issues: [{
        category: "timing",
        severity: "blocker",
        reason_code: "window",
        recovery_hint: "Set a valid session window.",
      }],
    }));
    renderSetup(view({ permitted_actions: ["save_draft", "check_readiness"] }), { checkReadiness });

    fireEvent.click(await screen.findByRole("button", { name: "Check readiness" }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Readiness blocked" })).toHaveFocus();
    });
    expect(screen.getAllByRole("alert")).toHaveLength(1);
  });

  it("keeps one summary when a save error lands on an already blocked revision", async () => {
    const saveDraft = vi.fn().mockRejectedValue(new Error("save failed"));
    renderSetup(view({
      permitted_actions: ["save_draft", "check_readiness"],
      issues: [{
        category: "timing",
        severity: "blocker",
        reason_code: "window",
        recovery_hint: "Set a valid session window.",
      }],
    }), { saveDraft });

    fireEvent.click(await screen.findByRole("button", { name: "Save draft" }));

    await waitFor(() => {
      expect(screen.getByText("This draft could not be saved. Reconcile before retrying.")).toBeInTheDocument();
    });
    expect(screen.getByRole("heading", { name: "Readiness blocked" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Correct the following" })).not.toBeInTheDocument();
    expect(screen.getAllByRole("alert")).toHaveLength(1);
    expect(screen.getByRole("heading", { name: "Readiness blocked" })).toHaveFocus();
  });

  it("moves focus to Correct the following after a save failure", async () => {
    const saveDraft = vi.fn().mockRejectedValue(new Error("save failed"));
    renderSetup(view({ permitted_actions: ["save_draft", "check_readiness"] }), { saveDraft });

    fireEvent.click(await screen.findByRole("button", { name: "Save draft" }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Correct the following" })).toHaveFocus();
    });
    expect(screen.getAllByRole("alert")).toHaveLength(1);
  });

  it("arms Activate cohort only after a current ready result and confirms before calling activate", async () => {
    const activateCohort = vi.fn();
    renderSetup(view({ issues: [] }), { activateCohort });

    const activate = await screen.findByRole("button", { name: "Activate cohort" });
    expect(activate).toHaveClass("key--activate", "key--large");
    fireEvent.click(activate);

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
    const assign = screen.getByRole("link", { name: "Assign Participants" });
    expect(assign).toHaveAttribute("href", "/activities/act-1/cohorts/coh-1/enrollments");
    expect(assign).toHaveClass("key--open", "key--large");
    expect(screen.getByRole("link", { name: "Assign Participants" }).closest(".create-ceremony__scroll")).toBeNull();
    expect(screen.queryByRole("button", { name: "Save draft" })).not.toBeInTheDocument();
    expect(screen.getByText("Cohort activated")).toBeInTheDocument();
    expect(screen.getByText(SETUP_RESOLVED_NOTE)).toBeInTheDocument();
    expect(screen.getByText(SETUP_RESOLVED_NOTE).closest(".workspace-alert-body")).toBeTruthy();
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
