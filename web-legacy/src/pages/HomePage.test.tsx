import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { BrowserApiProvider } from "../api/browser-api";
import { FlexQueryProvider } from "../api/query-client";
import type {
  ActorContextV1,
  HomeProjectionV1,
  NavigationProjectionV1,
} from "../api/browser-contracts";
import { HomePage } from "./HomePage";

const actorContext: ActorContextV1 = {
  schema_version: "v1",
  actor_id: "actor.synthetic.admin",
  display_name: "Synthetic Administrator",
  organization_id: "org.synthetic.demo",
  organization_name: "Synthetic Demo Organization",
  capabilities: ["activity_admin"],
  actor_stage: "administrator",
  is_synthetic: true,
};

const navigation: NavigationProjectionV1 = {
  schema_version: "v1",
  destinations: [
    {
      destination_id: "home",
      label: "Home",
      route: "/",
      tier: "p0",
      is_available: true,
    },
  ],
};

const homeProjection: HomeProjectionV1 = {
  schema_version: "v1",
  greeting: "Welcome, Synthetic Administrator",
  work_items: [
    {
      item_id: "hw-1",
      title: "Assessment Campaign draft",
      status_label: "Draft · Not activated",
      priority_band: "campaign_administration",
      route: "/activities/act.synthetic.campaign-001",
      next_action_label: "Continue setup",
    },
  ],
  permitted_actions: [],
};

function mockFetch() {
  return vi.fn((input: RequestInfo | URL) => {
    const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;

    if (url.includes("/browser/actor-context")) {
      return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(actorContext) });
    }

    if (url.includes("/browser/navigation")) {
      return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(navigation) });
    }

    if (url.includes("/browser/home")) {
      return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(homeProjection) });
    }

    return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
  });
}

describe("HomePage", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", mockFetch());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("renders work items with actionable links", async () => {
    render(
      <MemoryRouter>
        <FlexQueryProvider>
      <BrowserApiProvider>
          <HomePage />
        </BrowserApiProvider>
    </FlexQueryProvider>
      </MemoryRouter>,
    );

    expect(await screen.findByRole("heading", { name: /^home$/i })).toBeInTheDocument();
    expect(screen.getByText(/welcome, synthetic administrator/i)).toBeInTheDocument();

    const workLink = await screen.findByRole("link", { name: /assessment campaign draft/i });
    expect(workLink).toHaveAttribute("href", "/activities/act.synthetic.campaign-001");
    expect(screen.getByText(/continue setup/i)).toBeInTheDocument();
  });
});
