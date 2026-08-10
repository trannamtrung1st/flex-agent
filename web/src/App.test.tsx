import { render, screen } from "@testing-library/react";
import { App } from "./App";
import type {
  ActorContextV1,
  HomeProjectionV1,
  NavigationProjectionV1,
} from "./api/browser-contracts";

const actorContext: ActorContextV1 = {
  schema_version: "v1",
  actor_id: "actor.synthetic.admin",
  display_name: "Synthetic Administrator",
  organization_id: "org.synthetic.demo",
  organization_name: "Synthetic Demo Organization",
  capabilities: ["activity_admin", "governance", "session_control"],
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
    {
      destination_id: "activities",
      label: "Activities",
      route: "/activities",
      tier: "p0",
      is_available: true,
    },
    {
      destination_id: "governance",
      label: "Governance",
      route: "/governance",
      tier: "p0",
      is_available: true,
    },
  ],
};

const homeProjection: HomeProjectionV1 = {
  schema_version: "v1",
  greeting: "Welcome, Synthetic Administrator",
  work_items: [],
  permitted_actions: [],
};

function mockAuthenticatedFetch() {
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

describe("App shell", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", mockAuthenticatedFetch());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("renders navigation landmarks and the home heading", async () => {
    render(<App />);

    expect(await screen.findByRole("navigation", { name: /primary navigation/i })).toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: /mobile navigation/i })).toBeInTheDocument();
    expect(screen.getByRole("banner")).toBeInTheDocument();
    expect(screen.getByRole("main")).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: /^home$/i })).toBeInTheDocument();
  });
});
