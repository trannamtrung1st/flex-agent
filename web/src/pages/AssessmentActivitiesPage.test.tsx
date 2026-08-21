import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { REQUIRED_SOURCE_CATEGORIES, type ProductionSourceOption } from "../api/production-assessment";
import { AssessmentActivitiesPage } from "./AssessmentActivitiesPage";

function source(category: string, version = "v1"): ProductionSourceOption {
  return {
    category,
    source_id: `${category}-id`,
    version_id: version,
    content_digest: "a".repeat(64),
    source_kind: category,
    production_eligible: true,
  };
}

function renderActivities(options?: {
  activities?: Array<{ activity_id: string; title: string; revision_number: number; has_activated_cohort: boolean }>;
  permittedActions?: string[];
  sources?: ProductionSourceOption[];
  createError?: Error;
}) {
  const created: Array<{ title: string; sources: Record<string, { source_id: string }> }> = [];
  render(
    <MemoryRouter>
      <AssessmentActivitiesPage
        organizationId="org-1"
        loadActivities={() => Promise.resolve({
          activities: options?.activities ?? [],
          permitted_actions: options?.permittedActions ?? ["create_assessment"],
        })}
        loadSourceOptions={() => Promise.resolve({ sources: options?.sources ?? REQUIRED_SOURCE_CATEGORIES.map((category) => source(category)) })}
        createActivity={(title, sources) => {
          if (options?.createError) {
            return Promise.reject(options.createError);
          }

          created.push({ title, sources: sources as Record<string, { source_id: string }> });
          return Promise.resolve("act-new");
        }}
        onCreated={() => undefined}
      />
    </MemoryRouter>,
  );
  return created;
}

describe("AssessmentActivitiesPage", () => {
  it("shows an empty activity list and exact source selectors", async () => {
    renderActivities();
    expect(await screen.findByText("No activities are available.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Create assessment Campaign" })).toBeInTheDocument();
    expect(screen.getByLabelText("agent")).toBeInTheDocument();
    expect(screen.getAllByRole("option", { name: "agent · v1 · available" }).length).toBeGreaterThan(0);
  });

  it("explains a missing required source category", async () => {
    renderActivities({
      sources: REQUIRED_SOURCE_CATEGORIES.filter((category) => category !== "agent").map((category) => source(category)),
    });
    expect(await screen.findByText(/No permitted agent revisions are available/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Create assessment Campaign" })).not.toBeInTheDocument();
  });

  it("removes protected create controls when access is lost", async () => {
    renderActivities({ createError: new Error("Your access changed") });
    await screen.findByRole("heading", { name: "Create assessment Campaign" });
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Local campaign" } });
    fireEvent.click(screen.getByRole("button", { name: "Create assessment Campaign" }));
    expect(await screen.findByRole("heading", { name: "Your access changed" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Campaign title")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("agent")).not.toBeInTheDocument();
  });

  it("preserves the title after a create failure", async () => {
    renderActivities({ createError: new Error("denied") });
    await screen.findByRole("heading", { name: "Create assessment Campaign" });
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Local campaign" } });
    fireEvent.click(screen.getByRole("button", { name: "Create assessment Campaign" }));
    expect(await screen.findByText("The Campaign could not be created.")).toBeInTheDocument();
    expect(screen.getByLabelText("Campaign title")).toHaveValue("Local campaign");
  });

  it("omits create when the server does not permit it", async () => {
    renderActivities({
      permittedActions: [],
      activities: [{ activity_id: "act-1", title: "Existing", revision_number: 1, has_activated_cohort: false }],
    });
    expect(await screen.findByRole("link", { name: /Existing/ })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Create assessment Campaign" })).not.toBeInTheDocument();
  });
});
