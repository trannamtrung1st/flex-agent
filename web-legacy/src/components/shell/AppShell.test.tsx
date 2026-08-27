import { render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { BrowserApiProvider } from "../../api/browser-api";
import { FlexQueryProvider } from "../../api/query-client";
import type { ActorContextV1, NavigationProjectionV1 } from "../../api/browser-contracts";
import { AppShell } from "./AppShell";

function buildNavigation(availableIds: string[]): NavigationProjectionV1 {
  const all = [
    { destination_id: "home", label: "Home", route: "/", tier: "p0" },
    { destination_id: "activities", label: "Activities", route: "/activities", tier: "p0" },
    { destination_id: "my-work", label: "My work", route: "/my-work", tier: "p0" },
    { destination_id: "review-work", label: "Review work", route: "/review-work", tier: "p0" },
    { destination_id: "agents", label: "Agents", route: "/agents", tier: "p1" },
  ];

  return {
    schema_version: "v1",
    destinations: all.map((item) => ({
      ...item,
      is_available: availableIds.includes(item.destination_id),
      unavailable_reason: availableIds.includes(item.destination_id) ? null : "Not authorized",
    })),
  };
}

const actorContext: ActorContextV1 = {
  schema_version: "v1",
  actor_id: "actor.synthetic.participant",
  display_name: "Synthetic Participant",
  organization_id: "org.synthetic.demo",
  organization_name: "Synthetic Demo Organization",
  capabilities: ["participant"],
  actor_stage: "participant",
  is_synthetic: true,
};

function renderShell(availableIds: string[]) {
  const navigation = buildNavigation(availableIds);

  vi.stubGlobal(
    "fetch",
    vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;

      if (url.includes("/browser/actor-context")) {
        return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(actorContext) });
      }

      if (url.includes("/browser/navigation")) {
        return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(navigation) });
      }

      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }),
  );

  render(
    <MemoryRouter initialEntries={["/"]}>
      <FlexQueryProvider>
      <BrowserApiProvider>
        <Routes>
          <Route element={<AppShell />}>
            <Route index element={<p>Workspace content</p>} />
          </Route>
        </Routes>
      </BrowserApiProvider>
    </FlexQueryProvider>
    </MemoryRouter>,
  );
}

describe("AppShell navigation gating", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("shows only server-available destinations for the actor", async () => {
    renderShell(["home", "my-work", "results"]);

    await waitFor(() => {
      const nav = screen.getByRole("navigation", { name: /primary navigation/i });
      expect(within(nav).getByRole("link", { name: /home/i })).toBeInTheDocument();
    });

    const nav = screen.getByRole("navigation", { name: /primary navigation/i });
    expect(within(nav).getByRole("link", { name: /my work/i })).toBeInTheDocument();
    expect(within(nav).queryByRole("link", { name: /^activities$/i })).not.toBeInTheDocument();
    expect(within(nav).queryByRole("link", { name: /review work/i })).not.toBeInTheDocument();
  });

  it("still shows planned P1 destinations when navigation includes them", async () => {
    renderShell(["home", "agents"]);

    await waitFor(() => {
      const nav = screen.getByRole("navigation", { name: /primary navigation/i });
      expect(within(nav).getByRole("link", { name: /agents/i })).toBeInTheDocument();
    });

    expect(screen.getAllByText("P1").length).toBeGreaterThan(0);
  });
});
