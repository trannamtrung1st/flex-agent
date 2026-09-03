import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { ProductionApiError } from "../api/production-api";
import {
  REQUIRED_SOURCE_CATEGORIES,
  type NumberedActivityListQuery,
  type ProductionActivityList,
  type ProductionSourceOption,
} from "../api/production-assessment";
import { FlexQueryProvider, createFlexQueryClient } from "../api/query-client";
import { CAMPAIGN_TITLE_PLACEHOLDER } from "../content/fieldCopy";
import { assessmentKeys } from "../features/assessment/queryKeys";
import { AssessmentCampaignCreatePage } from "./AssessmentCampaignCreatePage";

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

function renderCreate(options?: {
  permittedActions?: string[];
  sources?: ProductionSourceOption[];
  createError?: Error;
  loadActivities?: (query: NumberedActivityListQuery, signal?: AbortSignal) => Promise<ProductionActivityList>;
  loadSourceOptions?: (signal?: AbortSignal) => Promise<{ environment: "development" | "production"; sources: ProductionSourceOption[] }>;
  onCreated?: (activityId: string) => void;
  queryClient?: ReturnType<typeof createFlexQueryClient>;
}) {
  const created: Array<{ title: string; sources: Record<string, { source_id: string }> }> = [];
  const queryClient = options?.queryClient ?? createFlexQueryClient();
  const loadSourceOptions = options?.loadSourceOptions ?? (() =>
    Promise.resolve({
      environment: "development" as const,
      sources: options?.sources ?? REQUIRED_SOURCE_CATEGORIES.map((category) => source(category)),
    }));
  render(
    <FlexQueryProvider client={queryClient}>
      <MemoryRouter>
        <AssessmentCampaignCreatePage
          loadActivities={options?.loadActivities ?? (() => Promise.resolve({
            activities: [],
            permitted_actions: options?.permittedActions ?? ["create_assessment"],
          } satisfies ProductionActivityList))}
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
  return { created, queryClient };
}

describe("AssessmentCampaignCreatePage", () => {
  it("shows exact source selectors on a dedicated create surface", async () => {
    renderCreate();
    expect(await screen.findByLabelText("Campaign title")).toHaveAttribute(
      "placeholder",
      CAMPAIGN_TITLE_PLACEHOLDER,
    );
    expect(screen.getByLabelText("Campaign title")).not.toHaveClass("field-input--uppercase");
    const region = screen.getByRole("region", { name: "Create assessment Campaign" });
    expect(region).toHaveClass("record-plane", "record-plane--setup");
    expect(region.querySelector(".frame-cut")).toContainElement(screen.getByLabelText("Campaign title"));
    expect(screen.getByRole("heading", { name: "Create assessment Campaign" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.getByRole("button", { name: "Create" })).toHaveClass("key", "key--transmit", "key--large");
    expect(screen.getByRole("button", { name: "Create" }).closest(".plate-foot")).toHaveAttribute("data-arrangement", "end");
    expect(screen.getByRole("button", { name: "Create" }).closest(".create-ceremony__scroll")).toBeNull();
    expect(screen.getByRole("button", { name: /Agent/ })).toBeInTheDocument();
    expect(screen.getByRole("group", { name: "Agent and Harness" })).toHaveClass("form-section");
    expect(screen.getByRole("group", { name: "Source set" })).toHaveClass("form-section");
    expect(screen.getByRole("group", { name: "Agent and Harness" }).nextElementSibling).toBe(
      screen.getByRole("group", { name: "Source set" }),
    );
    expect(screen.getByRole("group", { name: "Agent and Harness" }).parentElement).toHaveAttribute(
      "data-flow-gap",
      "6",
    );
    expect(region.querySelector(".create-ceremony__scroll")?.querySelector(".form-divider")).toBeNull();
    expect(screen.getByRole("button", { name: /Harness/ })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Organization policy/ })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Agent/ }).compareDocumentPosition(screen.getByRole("button", { name: /Organization policy/ }))
      & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(screen.getByRole("button", { name: /agent · v1/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Organization policy/ })).toHaveTextContent("v1");
  });

  it("uses one plate note when every selected revision is development-only", async () => {
    renderCreate({
      sources: REQUIRED_SOURCE_CATEGORIES.map((category) => ({
        ...source(category),
        production_eligible: false,
      })),
    });
    const plateNote = await screen.findByText("Listed revisions are development only.");
    expect(plateNote.closest(".state-cell")).toBeTruthy();
    expect(plateNote.closest(".create-eligibility-note")).toBeNull();
    expect(screen.queryByText("development")).not.toBeInTheDocument();
    expect(screen.queryByText("available")).not.toBeInTheDocument();
  });

  it("marks only mixed development berths", async () => {
    renderCreate({
      sources: REQUIRED_SOURCE_CATEGORIES.map((category) => ({
        ...source(category),
        production_eligible: category !== "agent",
      })),
    });
    expect(await screen.findByLabelText("Agent")).toBeInTheDocument();
    expect(screen.queryByText("Listed revisions are development only.")).not.toBeInTheDocument();
    const developmentMarks = await screen.findAllByText("development");
    expect(developmentMarks).toHaveLength(1);
    const developmentMark = developmentMarks[0];
    expect(developmentMark.closest(".state-cell")).toBeTruthy();
    expect(developmentMark.closest(".field-hint")).toBeNull();
    expect(screen.queryByText("available")).not.toBeInTheDocument();
  });

  it("explains a missing required source category", async () => {
    renderCreate({
      sources: REQUIRED_SOURCE_CATEGORIES.filter((category) => category !== "agent").map((category) => source(category)),
    });
    const emptyLabel = await screen.findByText(/No permitted Agent revisions are available/i);
    const region = screen.getByRole("region", { name: "Create assessment Campaign" });
    expect(region).toHaveClass("record-plane", "record-plane--setup");
    expect(region.querySelector(".frame-cut")).toContainElement(emptyLabel);
    expect(emptyLabel.closest(".empty-plate")).toHaveClass("empty-plate--inset");
    expect(screen.getByText("A ready source set is required before a draft can be created.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.queryByRole("button", { name: "Create" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Campaign title")).not.toBeInTheDocument();
  });

  it("removes protected create controls when access is lost", async () => {
    renderCreate({ createError: new ProductionApiError(403, "Your access changed") });
    await screen.findByLabelText("Campaign title");
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Local campaign" } });
    fireEvent.click(screen.getByRole("button", { name: "Create" }));
    expect(await screen.findByRole("heading", { name: "Your access changed" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Campaign title")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Agent")).not.toBeInTheDocument();
  });

  it("preserves the title after a create failure", async () => {
    renderCreate({ createError: new Error("denied") });
    await screen.findByLabelText("Campaign title");
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Local campaign" } });
    fireEvent.click(screen.getByRole("button", { name: "Create" }));
    expect(await screen.findByText("The Campaign could not be created.")).toBeInTheDocument();
    expect(screen.getByLabelText("Campaign title")).toHaveValue("Local campaign");
  });

  it("blocks create in production environments until timing is authored", async () => {
    renderCreate({
      loadSourceOptions: () => Promise.resolve({
        environment: "production",
        sources: REQUIRED_SOURCE_CATEGORIES.map((category) => source(category)),
      }),
    });

    expect(await screen.findByRole("heading", { name: "Create timing is not configured" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Campaign title")).not.toBeInTheDocument();
  });

  it("omits create when the server does not permit it", async () => {
    const loadSourceOptions = vi.fn(() => Promise.resolve({
      environment: "development" as const,
      sources: REQUIRED_SOURCE_CATEGORIES.map((category) => source(category)),
    }));
    renderCreate({
      permittedActions: [],
      loadSourceOptions,
    });
    expect(await screen.findByRole("heading", { name: "Create is not available" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Return to Activities" })).toHaveAttribute("href", "/activities");
    expect(screen.queryByLabelText("Campaign title")).not.toBeInTheDocument();
    expect(loadSourceOptions).not.toHaveBeenCalled();
  });

  it("prevents duplicate create submission while pending", async () => {
    let resolveCreate: ((value: string) => void) | undefined;
    const createActivity = vi.fn(() => new Promise<string>((resolve) => {
      resolveCreate = resolve;
    }));
    render(
      <FlexQueryProvider>
        <MemoryRouter>
          <AssessmentCampaignCreatePage
            loadActivities={() => Promise.resolve({
              activities: [],
              permitted_actions: ["create_assessment"],
            } satisfies ProductionActivityList)}
            loadSourceOptions={() => Promise.resolve({
              environment: "development" as const,
              sources: REQUIRED_SOURCE_CATEGORIES.map((category) => source(category)),
            })}
            createActivity={createActivity}
            onCreated={() => undefined}
          />
        </MemoryRouter>
      </FlexQueryProvider>,
    );
    await screen.findByRole("button", { name: "Create" });
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Local campaign" } });
    fireEvent.click(screen.getByRole("button", { name: "Create" }));
    expect(await screen.findByRole("button", { name: "Creating…" })).toBeDisabled();
    fireEvent.click(screen.getByRole("button", { name: "Creating…" }));
    expect(createActivity).toHaveBeenCalledTimes(1);
    resolveCreate?.("act-new");
  });

  it("navigates immediately after authoritative create and invalidates the list key", async () => {
    const onCreated = vi.fn();
    const queryClient = createFlexQueryClient();
    const invalidateSpy = vi.spyOn(queryClient, "invalidateQueries");
    renderCreate({ onCreated, queryClient });
    await screen.findByRole("button", { name: "Create" });
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Local campaign" } });
    fireEvent.click(screen.getByRole("button", { name: "Create" }));
    await waitFor(() => {
      expect(onCreated).toHaveBeenCalledWith("act-new");
    });
    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: assessmentKeys.activitiesRoot(),
      exact: false,
      refetchType: "none",
    });
    expect(queryClient.isFetching({ queryKey: assessmentKeys.activitiesRoot() })).toBe(0);
  });

  it("does not submit a client-invalid title and focuses the linked error summary", async () => {
    const { created } = renderCreate();
    await screen.findByRole("button", { name: "Create" });
    fireEvent.click(screen.getByRole("button", { name: "Create" }));
    expect(await screen.findByRole("link", { name: "Enter a Campaign title" })).toHaveAttribute("href", expect.stringMatching(/^#/));
    await waitFor(() => {
      expect(document.activeElement).toHaveTextContent("Correct the following");
      expect(document.activeElement?.id).toBe(screen.getByRole("heading", { name: "Correct the following" }).id);
    });
    expect(created).toHaveLength(0);
  });

  it("rejects a stale source identity before calling create", async () => {
    const queryClient = createFlexQueryClient();
    const { created } = renderCreate({ queryClient });
    await screen.findByRole("button", { name: "Create" });
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Local campaign" } });
    queryClient.setQueryData(assessmentKeys.sourceOptions(), {
      environment: "development",
      sources: REQUIRED_SOURCE_CATEGORIES.map((category) => source(category, "v2")),
    });
    fireEvent.click(screen.getByRole("button", { name: "Create" }));
    expect(await screen.findByText("Selected sources are no longer available. Choose current options.")).toBeInTheDocument();
    expect(screen.getByLabelText("Campaign title")).toHaveValue("Local campaign");
    expect(created).toHaveLength(0);
  });

  it("does not reset a touched title when source options refetch", async () => {
    const queryClient = createFlexQueryClient();
    renderCreate({ queryClient });
    await screen.findByLabelText("Campaign title");
    fireEvent.change(screen.getByLabelText("Campaign title"), { target: { value: "Kept title" } });
    queryClient.setQueryData(assessmentKeys.sourceOptions(), {
      environment: "development",
      sources: REQUIRED_SOURCE_CATEGORIES.map((category) => source(category, "v9")),
    });
    await waitFor(() => {
      expect(screen.getByLabelText("Campaign title")).toHaveValue("Kept title");
    });
    expect(screen.getByRole("button", { name: /Agent/ })).toHaveTextContent("No longer available");
  });
});
