import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { ProductionApiError } from "../api/production-api";
import { REQUIRED_SOURCE_CATEGORIES, type ProductionSourceOption } from "../api/production-assessment";
import { FlexQueryProvider, createFlexQueryClient } from "../api/query-client";
import { assessmentKeys } from "../features/assessment/queryKeys";
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
  loadActivities?: (signal?: AbortSignal) => Promise<{
    activities: Array<{ activity_id: string; title: string; revision_number: number; has_activated_cohort: boolean }>;
    permitted_actions: string[];
  }>;
  loadSourceOptions?: (signal?: AbortSignal) => Promise<{ sources: ProductionSourceOption[] }>;
  onCreated?: (activityId: string) => void;
  queryClient?: ReturnType<typeof createFlexQueryClient>;
}) {
  const created: Array<{ title: string; sources: Record<string, { source_id: string }> }> = [];
  const sourceCalls: AbortSignal[] = [];
  const queryClient = options?.queryClient ?? createFlexQueryClient();
  const loadSourceOptions = options?.loadSourceOptions ?? ((signal?: AbortSignal) => {
    if (signal) {
      sourceCalls.push(signal);
    }

    return Promise.resolve({ sources: options?.sources ?? REQUIRED_SOURCE_CATEGORIES.map((category) => source(category)) });
  });
  render(
    <FlexQueryProvider client={queryClient}>
      <MemoryRouter>
        <AssessmentActivitiesPage
          organizationId="org-1"
          loadActivities={options?.loadActivities ?? (() => Promise.resolve({
            activities: options?.activities ?? [],
            permitted_actions: options?.permittedActions ?? ["create_assessment"],
          }))}
          loadSourceOptions={loadSourceOptions}
          createActivity={(title, sources) => {
            if (options?.createError) {
              return Promise.reject(options.createError);
            }

            created.push({ title, sources: sources as Record<string, { source_id: string }> });
            return Promise.resolve("act-new");
          }}
          onCreated={options?.onCreated ?? (() => undefined)}
        />
      </MemoryRouter>
    </FlexQueryProvider>,
  );
  return { created, sourceCalls, queryClient };
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
    renderActivities({ createError: new ProductionApiError(403, "Your access changed") });
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
    const loadSourceOptions = vi.fn(() => Promise.resolve({ sources: REQUIRED_SOURCE_CATEGORIES.map((category) => source(category)) }));
    renderActivities({
      permittedActions: [],
      activities: [{ activity_id: "act-1", title: "Existing", revision_number: 1, has_activated_cohort: false }],
      loadSourceOptions,
    });
    expect(await screen.findByRole("link", { name: /Existing/ })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Existing/ })).toHaveClass("composition-inline");
    expect(screen.getByRole("link", { name: /Existing/ })).toHaveAttribute("data-flow-wrap", "false");
    expect(screen.getByRole("heading", { name: "Activity list" }).closest(".workspace-section")?.parentElement).toHaveAttribute(
      "data-flow-gap",
      "none",
    );
    expect(screen.queryByRole("heading", { name: "Create assessment Campaign" })).not.toBeInTheDocument();
    expect(loadSourceOptions).not.toHaveBeenCalled();
  });

  it("keeps the authorized list when source options fail independently", async () => {
    renderActivities({
      activities: [{ activity_id: "act-1", title: "Existing", revision_number: 1, has_activated_cohort: false }],
      loadSourceOptions: () => Promise.reject(new Error("source unavailable")),
    });
    expect(await screen.findByRole("link", { name: /Existing/ })).toBeInTheDocument();
    expect(await screen.findByText(/No permitted organization policy revisions are available/i)).toBeInTheDocument();
  });

  it("prevents duplicate create submission while pending", async () => {
    let resolveCreate: ((value: string) => void) | undefined;
    const createActivity = vi.fn(() => new Promise<string>((resolve) => {
      resolveCreate = resolve;
    }));
    render(
      <FlexQueryProvider>
        <MemoryRouter>
          <AssessmentActivitiesPage
            organizationId="org-1"
            loadActivities={() => Promise.resolve({
              activities: [],
              permitted_actions: ["create_assessment"],
            })}
            loadSourceOptions={() => Promise.resolve({
              sources: REQUIRED_SOURCE_CATEGORIES.map((category) => source(category)),
            })}
            createActivity={createActivity}
            onCreated={() => undefined}
          />
        </MemoryRouter>
      </FlexQueryProvider>,
    );
    await screen.findByRole("button", { name: "Create assessment Campaign" });
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Local campaign" } });
    fireEvent.click(screen.getByRole("button", { name: "Create assessment Campaign" }));
    expect(await screen.findByRole("button", { name: "Creating…" })).toBeDisabled();
    fireEvent.click(screen.getByRole("button", { name: "Creating…" }));
    expect(createActivity).toHaveBeenCalledTimes(1);
    resolveCreate?.("act-new");
  });

  it("navigates immediately after authoritative create and invalidates the list key", async () => {
    const onCreated = vi.fn();
    const queryClient = createFlexQueryClient();
    const invalidateSpy = vi.spyOn(queryClient, "invalidateQueries");
    renderActivities({ onCreated, queryClient });
    await screen.findByRole("button", { name: "Create assessment Campaign" });
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Local campaign" } });
    fireEvent.click(screen.getByRole("button", { name: "Create assessment Campaign" }));
    await waitFor(() => {
      expect(onCreated).toHaveBeenCalledWith("act-new");
    });
    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: assessmentKeys.activities(),
      exact: true,
      refetchType: "none",
    });
    expect(queryClient.getQueryState(assessmentKeys.activities())?.fetchStatus).not.toBe("fetching");
  });

  it("does not request source options from cached create permission", async () => {
    const queryClient = createFlexQueryClient();
    queryClient.setQueryData(assessmentKeys.activities(), {
      activities: [{ activity_id: "act-1", title: "Cached", revision_number: 1, has_activated_cohort: false }],
      permitted_actions: ["create_assessment"],
    });
    const loadSourceOptions = vi.fn(() => Promise.resolve({
      sources: REQUIRED_SOURCE_CATEGORIES.map((category) => source(category)),
    }));
    let releaseList: ((value: {
      activities: Array<{ activity_id: string; title: string; revision_number: number; has_activated_cohort: boolean }>;
      permitted_actions: string[];
    }) => void) | undefined;
    const listPromise = new Promise<{
      activities: Array<{ activity_id: string; title: string; revision_number: number; has_activated_cohort: boolean }>;
      permitted_actions: string[];
    }>((resolve) => {
      releaseList = resolve;
    });

    renderActivities({
      queryClient,
      loadActivities: () => listPromise,
      loadSourceOptions,
    });

    await waitFor(() => {
      expect(loadSourceOptions).not.toHaveBeenCalled();
    });
    releaseList?.({
      activities: [{ activity_id: "act-1", title: "Cached", revision_number: 1, has_activated_cohort: false }],
      permitted_actions: [],
    });
    expect(await screen.findByRole("link", { name: /Cached/ })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Create assessment Campaign" })).not.toBeInTheDocument();
    expect(loadSourceOptions).not.toHaveBeenCalled();
  });

  it("does not submit a client-invalid title and focuses the linked error summary", async () => {
    const { created } = renderActivities();
    await screen.findByRole("button", { name: "Create assessment Campaign" });
    fireEvent.click(screen.getByRole("button", { name: "Create assessment Campaign" }));
    expect(await screen.findByRole("link", { name: "Enter a Campaign title" })).toHaveAttribute("href", expect.stringMatching(/^#/));
    await waitFor(() => {
      expect(document.activeElement).toHaveTextContent("Correct the following");
    });
    expect(created).toHaveLength(0);
  });

  it("rejects a stale source identity before calling create", async () => {
    const queryClient = createFlexQueryClient();
    const { created } = renderActivities({ queryClient });
    await screen.findByRole("button", { name: "Create assessment Campaign" });
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Local campaign" } });
    queryClient.setQueryData(assessmentKeys.sourceOptions(), {
      sources: REQUIRED_SOURCE_CATEGORIES.map((category) => source(category, "v2")),
    });
    fireEvent.click(screen.getByRole("button", { name: "Create assessment Campaign" }));
    expect(await screen.findByText("Selected sources are no longer available. Choose current options.")).toBeInTheDocument();
    expect(screen.getByLabelText("Campaign title")).toHaveValue("Local campaign");
    expect(created).toHaveLength(0);
  });

  it("does not reset a touched title when source options refetch", async () => {
    const queryClient = createFlexQueryClient();
    renderActivities({ queryClient });
    await screen.findByLabelText("Campaign title");
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Kept title" } });
    queryClient.setQueryData(assessmentKeys.sourceOptions(), {
      sources: REQUIRED_SOURCE_CATEGORIES.map((category) => source(category, "v9")),
    });
    await waitFor(() => {
      expect(screen.getByLabelText("Campaign title")).toHaveValue("Kept title");
    });
    expect(screen.getByLabelText("agent")).toHaveValue("agent-id:v1");
  });

  it("passes Query cancellation through the activities loader", async () => {
    const signals: AbortSignal[] = [];
    renderActivities({
      loadActivities: (signal) => {
        if (signal) {
          signals.push(signal);
        }

        return Promise.resolve({ activities: [], permitted_actions: [] });
      },
    });
    await screen.findByText("No activities are available.");
    expect(signals.length).toBeGreaterThan(0);
  });
});
