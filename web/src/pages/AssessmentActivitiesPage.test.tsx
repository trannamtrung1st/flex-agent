import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import {
  DEFAULT_ACTIVITY_LIST_QUERY,
  type NumberedActivityListQuery,
  REQUIRED_SOURCE_CATEGORIES,
  type ProductionActivityList,
  type ProductionActivitySummary,
  type ProductionSourceOption,
  type ProductionSourceOptionsResponse,
} from "../api/production-assessment";
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

function activityRow(overrides: {
  activity_id: string;
  title: string;
  revision_number?: number;
  has_activated_cohort?: boolean;
  updated_at?: string;
}): ProductionActivitySummary {
  return {
    revision_number: 1,
    has_activated_cohort: false,
    updated_at: "2026-08-01T00:00:00Z",
    ...overrides,
  };
}

function sortValue(row: ProductionActivitySummary, field: NumberedActivityListQuery["sort"][number]["field"]) {
  switch (field) {
    case "activation":
      return row.has_activated_cohort ? 1 : 0;
    case "revision":
      return row.revision_number;
    case "updated":
      return row.updated_at;
    default:
      return row.title.toLowerCase();
  }
}

function pageFromServer(
  activities: ProductionActivitySummary[],
  query: NumberedActivityListQuery,
  permittedActions: string[],
): ProductionActivityList {
  const q = query.q.trim().toLowerCase();
  const matched = activities.filter((row) =>
    !q || row.title.toLowerCase().includes(q) || row.activity_id.toLowerCase().includes(q));
  const sorted = [...matched].sort((left, right) => {
    for (const spec of query.sort) {
      const lv = sortValue(left, spec.field);
      const rv = sortValue(right, spec.field);
      if (lv < rv) return spec.direction === "asc" ? -1 : 1;
      if (lv > rv) return spec.direction === "asc" ? 1 : -1;
    }
    return left.activity_id.localeCompare(right.activity_id);
  });
  const total = sorted.length;
  const start = (query.page - 1) * query.pageSize;
  return {
    activities: sorted.slice(start, start + query.pageSize),
    permitted_actions: permittedActions,
    pagination: {
      mode: "numbered",
      page: query.page,
      page_size: query.pageSize,
      total_items: total,
      total_pages: total === 0 ? 0 : Math.ceil(total / query.pageSize),
    },
  };
}

function renderActivities(options?: {
  activities?: ProductionActivitySummary[];
  permittedActions?: string[];
  sources?: ProductionSourceOption[];
  loadActivities?: (query: NumberedActivityListQuery, signal?: AbortSignal) => Promise<ProductionActivityList>;
  loadSourceOptions?: (signal?: AbortSignal) => Promise<ProductionSourceOptionsResponse>;
  queryClient?: ReturnType<typeof createFlexQueryClient>;
}) {
  const sourceCalls: AbortSignal[] = [];
  const queryClient = options?.queryClient ?? createFlexQueryClient();
  const loadSourceOptions = options?.loadSourceOptions ?? ((signal?: AbortSignal) => {
    if (signal) {
      sourceCalls.push(signal);
    }

    return Promise.resolve({
      environment: "development" as const,
      sources: options?.sources ?? REQUIRED_SOURCE_CATEGORIES.map((category) => source(category)),
    });
  });
  render(
    <FlexQueryProvider client={queryClient}>
      <MemoryRouter>
        <AssessmentActivitiesPage
          organizationId="org-1"
          loadActivities={options?.loadActivities ?? ((query) => Promise.resolve(pageFromServer(
            options?.activities ?? [],
            query,
            options?.permittedActions ?? ["create_assessment"],
          )))}
          loadSourceOptions={loadSourceOptions}
        />
      </MemoryRouter>
    </FlexQueryProvider>,
  );
  return { sourceCalls, queryClient };
}

describe("AssessmentActivitiesPage", () => {
  it("shows a ceremony wait plate while activities load", () => {
    renderActivities({ loadActivities: () => new Promise(() => {}) });
    const status = screen.getByRole("status");
    expect(status).toHaveClass("wait-plate", "wait-plate--inset", "ceremony-wait");
    expect(screen.getByText("Loading activities…")).toBeVisible();
    expect(status.querySelector(".scan-track.is-waiting")).toBeTruthy();
    expect(status.closest(".work-plane--ceremony")).toBeTruthy();
  });

  it("shows an empty registry with a table-action create destination and no empty-plate create key", async () => {
    renderActivities();
    expect(await screen.findByText("No activities are available.")).toBeInTheDocument();
    const create = screen.getByRole("link", { name: "Create" });
    expect(create).toHaveAttribute("href", "/activities/new");
    expect(create.closest(".datatable-actions")).not.toBeNull();
    expect(screen.queryByRole("heading", { name: "Create assessment Campaign" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Campaign title")).not.toBeInTheDocument();
    expect(document.querySelector(".datatable-empty")).toHaveClass("datatable-empty", "empty-plate--inset");
    expect(document.querySelector(".datatable-empty")?.querySelector(".key")).toBeNull();
    expect(document.querySelector(".registry-wall--empty")).toBeNull();
    expect(document.querySelector(".registry-assign-keys")).toBeNull();
  });

  it("seats a Clear search key in the empty plate when no campaigns match the query", async () => {
    renderActivities({
      activities: [activityRow({ activity_id: "act-1", title: "Existing" })],
    });
    await screen.findByRole("link", { name: /Existing/ });
    fireEvent.change(screen.getByRole("searchbox", { name: "Search campaign title or ID" }), {
      target: { value: "zzz-no-match" },
    });
    expect(await screen.findByText("No matching activities")).toBeInTheDocument();
    expect(screen.getByText("Nothing matches the current search. Clear the search to restore the registry.")).toBeInTheDocument();
    const clear = screen.getByRole("button", { name: "Clear search" });
    expect(clear.closest(".datatable-empty")).not.toBeNull();
    expect(clear.closest(".work-plane")).toHaveClass("registry-wall--hug");
    fireEvent.click(clear);
    expect(await screen.findByRole("link", { name: /Existing/ })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Clear search" })).not.toBeInTheDocument();
  });

  it("keeps the empty registry as the only work plane", async () => {
    renderActivities();
    const empty = await screen.findByText("No activities are available.");
    expect(empty.closest(".frame-cut")).toHaveClass("datatable-frame", "frame-cut--flush");
    expect(empty.closest(".work-plane")).toHaveClass("registry-wall", "registry-wall--hug");
  });

  it("fills the registry bay when more than four activities are loaded", async () => {
    renderActivities({
      activities: [1, 2, 3, 4, 5].map((n) => activityRow({
        activity_id: `act-${n}`,
        title: `Campaign ${n}`,
      })),
    });
    const campaign = await screen.findByRole("link", { name: /Campaign 1/ });
    expect(screen.getByRole("searchbox", { name: "Search campaign title or ID" })).toHaveClass("seg-search");
    expect(campaign.closest(".work-plane")).toHaveClass("registry-wall");
    expect(campaign.closest(".work-plane")).not.toHaveClass("registry-wall--hug");
    fireEvent.change(screen.getByRole("searchbox", { name: "Search campaign title or ID" }), {
      target: { value: "zzz-no-match" },
    });
    expect(await screen.findByText("No matching activities")).toBeInTheDocument();
    expect(document.querySelector(".work-plane")).toHaveClass("registry-wall--hug");
  });

  it("hugs a short last page from visible rows rather than matching total", async () => {
    renderActivities({
      permittedActions: [],
      loadActivities: () => Promise.resolve({
        activities: [
          activityRow({ activity_id: "act-17", title: "Later A" }),
          activityRow({ activity_id: "act-18", title: "Later B" }),
        ],
        permitted_actions: [],
        pagination: { mode: "numbered", page: 2, page_size: 16, total_items: 18, total_pages: 2 },
      }),
    });
    const campaign = await screen.findByRole("link", { name: /Later A/ });
    expect(campaign.closest(".work-plane")).toHaveClass("registry-wall--hug");
    expect(screen.getByRole("contentinfo")).toHaveTextContent("17–18 OF 18");
  });

  it("explains a missing required source category and withholds create", async () => {
    renderActivities({
      sources: REQUIRED_SOURCE_CATEGORIES.filter((category) => category !== "agent").map((category) => source(category)),
    });
    expect(await screen.findByText(/No permitted Agent revisions are available/i)).toHaveClass("advisory-copy");
    expect(screen.queryByRole("link", { name: "Create" })).not.toBeInTheDocument();
  });

  it("omits create when the server does not permit it", async () => {
    const loadSourceOptions = vi.fn(() => Promise.resolve({
      environment: "development" as const,
      sources: REQUIRED_SOURCE_CATEGORIES.map((category) => source(category)),
    }));
    renderActivities({
      permittedActions: [],
      activities: [activityRow({ activity_id: "act-1", title: "Existing" })],
      loadSourceOptions,
    });
    expect(await screen.findByRole("link", { name: /Existing/ })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Existing/ })).toHaveAttribute("href", "/activities/act-1/setup");
    expect(screen.getByRole("table", { name: "Activities" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Existing/ }).closest(".frame-cut")).toHaveClass(
      "datatable-frame",
      "frame-cut--flush",
    );
    expect(screen.queryByRole("link", { name: "Create" })).not.toBeInTheDocument();
    expect(loadSourceOptions).not.toHaveBeenCalled();
  });

  it("filters campaigns by activity id", async () => {
    renderActivities({
      activities: [
        activityRow({ activity_id: "act-alpha", title: "Alpha Campaign" }),
        activityRow({ activity_id: "act-beta", title: "Beta Campaign", revision_number: 2, has_activated_cohort: true }),
      ],
    });
    await screen.findByRole("link", { name: /Alpha Campaign/ });
    fireEvent.change(screen.getByRole("searchbox", { name: "Search campaign title or ID" }), {
      target: { value: "act-beta" },
    });
    await waitFor(() => {
      expect(screen.queryByRole("link", { name: /Alpha Campaign/ })).not.toBeInTheDocument();
    });
    expect(screen.getByRole("link", { name: /Beta Campaign/ })).toBeInTheDocument();
  });

  it("sorts campaigns by activation state", async () => {
    renderActivities({
      activities: [
        activityRow({ activity_id: "act-draft", title: "Draft Campaign" }),
        activityRow({ activity_id: "act-live", title: "Live Campaign", revision_number: 3, has_activated_cohort: true }),
      ],
    });
    await screen.findByRole("link", { name: /Draft Campaign/ });
    fireEvent.click(screen.getByRole("button", { name: "Activation" }));
    const rows = screen.getAllByRole("row").slice(1);
    expect(rows[0]).toHaveTextContent("Draft Campaign");
    expect(rows[1]).toHaveTextContent("Live Campaign");
  });

  it("keeps a populated registry above setup links and offers a create destination in the toolbar", async () => {
    renderActivities({
      activities: [activityRow({ activity_id: "act-1", title: "Existing" })],
    });
    const campaign = await screen.findByRole("link", { name: /Existing/ });
    expect(campaign).toHaveClass("datatable-id");
    expect(screen.getByRole("table", { name: "Activities" })).toHaveClass("datatable-table");
    expect(screen.getByRole("columnheader", { name: "Updated" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Rev" })).toBeInTheDocument();
    expect(campaign.closest("tr")?.querySelector("time")).toBeInTheDocument();
    expect(campaign.closest("tr")?.children.item(3)).toHaveTextContent("1");
    expect(campaign.closest(".work-plane")).toHaveClass("registry-wall--hug");
    expect(screen.queryByRole("heading", { name: "Create assessment Campaign" })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Create" })).toHaveAttribute("href", "/activities/new");
    expect(screen.getByRole("link", { name: "Create" }).closest(".datatable-actions")).not.toBeNull();
  });

  it("shows the shared absence mark when a campaign has no updated instant", async () => {
    renderActivities({
      activities: [activityRow({ activity_id: "act-1", title: "Existing", updated_at: undefined })],
    });
    const campaign = await screen.findByRole("link", { name: /Existing/ });
    const row = campaign.closest("tr");
    expect(row).toHaveTextContent("—");
    expect(row).not.toHaveTextContent(/undefined/i);
    expect(row?.querySelector("time")).toBeNull();
    expect(screen.getByText("Not recorded")).toHaveClass("visually-hidden");
  });

  it("keeps the authorized list when source options fail independently", async () => {
    renderActivities({
      activities: [activityRow({ activity_id: "act-1", title: "Existing" })],
      loadSourceOptions: () => Promise.reject(new Error("source unavailable")),
    });
    expect(await screen.findByRole("link", { name: /Existing/ })).toBeInTheDocument();
    expect(await screen.findByText(/No permitted Organization policy revisions are available/i)).toHaveClass("advisory-copy");
  });

  it("does not request source options from cached create permission", async () => {
    const queryClient = createFlexQueryClient();
    queryClient.setQueryData(assessmentKeys.activities(DEFAULT_ACTIVITY_LIST_QUERY), {
      activities: [activityRow({ activity_id: "act-1", title: "Cached" })],
      permitted_actions: ["create_assessment"],
      pagination: { mode: "numbered", page: 1, page_size: 16, total_items: 1, total_pages: 1 },
    });
    const loadSourceOptions = vi.fn(() => Promise.resolve({
      environment: "development" as const,
      sources: REQUIRED_SOURCE_CATEGORIES.map((category) => source(category)),
    }));
    let releaseList: ((value: {
      activities: Array<ReturnType<typeof activityRow>>;
      permitted_actions: string[];
    }) => void) | undefined;
    const listPromise = new Promise<{
      activities: Array<ReturnType<typeof activityRow>>;
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
      activities: [activityRow({ activity_id: "act-1", title: "Cached" })],
      permitted_actions: [],
    });
    expect(await screen.findByRole("link", { name: /Cached/ })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Create" })).not.toBeInTheDocument();
    expect(loadSourceOptions).not.toHaveBeenCalled();
  });

  it("passes Query cancellation through the activities loader", async () => {
    const signals: AbortSignal[] = [];
    renderActivities({
      loadActivities: (_query, signal) => {
        if (signal) {
          signals.push(signal);
        }

        return Promise.resolve({
          activities: [],
          permitted_actions: [],
          pagination: { mode: "numbered", page: 1, page_size: 16, total_items: 0, total_pages: 0 },
        });
      },
    });
    await screen.findByText("No activities are available.");
    expect(signals.length).toBeGreaterThan(0);
  });

  it("keeps the last authorized page busy while the next page is pending", async () => {
    let release: (() => void) | undefined;
    const loadActivities = vi.fn((query: NumberedActivityListQuery) => {
      if (query.page === 1) {
        return Promise.resolve({
          activities: [activityRow({ activity_id: "act-1", title: "Existing" })],
          permitted_actions: [],
          pagination: { mode: "numbered" as const, page: 1, page_size: 16, total_items: 17, total_pages: 2 },
        });
      }
      return new Promise<ProductionActivityList>((resolve) => {
        release = () => resolve({
          activities: [activityRow({ activity_id: "act-17", title: "Later" })],
          permitted_actions: [],
          pagination: { mode: "numbered", page: 2, page_size: 16, total_items: 17, total_pages: 2 },
        });
      });
    });
    renderActivities({ loadActivities, permittedActions: [] });
    expect(await screen.findByRole("link", { name: /Existing/ })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Next" }));
    expect(await screen.findByRole("link", { name: /Existing/ })).toBeInTheDocument();
    const footer = screen.getByRole("contentinfo");
    expect(footer).toHaveAttribute("aria-busy", "true");
    expect(footer).toHaveTextContent("01–01 OF 17");
    expect(screen.getByRole("button", { name: /page\s*01/i })).toBeDisabled();
    release?.();
    expect(await screen.findByRole("link", { name: /Later/ })).toBeInTheDocument();
  });

  it("retries the exact failed query", async () => {
    const loadActivities = vi.fn((query: NumberedActivityListQuery) => {
      if (loadActivities.mock.calls.length === 1) {
        return Promise.reject(new Error("temporary list failure"));
      }
      return Promise.resolve(pageFromServer(
        [activityRow({ activity_id: "act-1", title: "Existing" })],
        query,
        [],
      ));
    });
    renderActivities({ loadActivities, permittedActions: [] });
    expect(await screen.findByRole("button", { name: "Retry" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Retry" }));
    expect(await screen.findByRole("link", { name: /Existing/ })).toBeInTheDocument();
    expect(loadActivities.mock.calls[0]?.[0]).toEqual(loadActivities.mock.calls[1]?.[0]);
  });

  it("recovers an out-of-range page using the returned totals", async () => {
    const loadActivities = vi.fn((query: NumberedActivityListQuery) => {
      if (query.page === 2) {
        return Promise.resolve({
          activities: [],
          permitted_actions: [],
          pagination: { mode: "numbered" as const, page: 2, page_size: 16, total_items: 1, total_pages: 1 },
        });
      }
      return Promise.resolve({
        activities: [activityRow({ activity_id: "act-1", title: "Existing" })],
        permitted_actions: [],
        pagination: { mode: "numbered" as const, page: 1, page_size: 16, total_items: 17, total_pages: 2 },
      });
    });
    renderActivities({ loadActivities, permittedActions: [] });
    expect(await screen.findByRole("link", { name: /Existing/ })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      expect(loadActivities.mock.calls.some((call) => call[0].page === 2)).toBe(true);
      expect(loadActivities.mock.calls.at(-1)?.[0].page).toBe(1);
    });
    expect(screen.getByRole("link", { name: /Existing/ })).toBeInTheDocument();
  });
});
