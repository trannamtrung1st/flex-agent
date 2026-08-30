import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../../api/production-api";
import { FlexQueryProvider } from "../../api/query-client";
import { ProductionAppShell } from "./ProductionAppShell";

function json(status: number, body: unknown) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  });
}

async function openOperatorMenu() {
  fireEvent.click(await screen.findByRole("button", { name: /operator menu/i }));
}

describe("ProductionAppShell operator disclosure", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    window.localStorage.removeItem("flex-agent-theme");
    delete document.documentElement.dataset.theme;
  });

  it("keeps theme and sign-out in the operator menu instead of the command strip", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return json(200, { authenticated: true, csrf_token: "csrf" });
      }
      if (url.includes("/v1/assessment/shell")) {
        return json(200, {
          schema_version: "v1",
          actor_id: "actor-1",
          organization_id: "org-1",
          relationship: "administrator",
          display_name: "Demo Administrator",
          navigation: [{ destination_id: "home", is_available: true }],
          permitted_actions: [],
        });
      }
      return json(404, {});
    }));

    render(
      <FlexQueryProvider>
        <ProductionApiProvider>
          <MemoryRouter>
            <Routes>
              <Route element={<ProductionAppShell />}>
                <Route path="/" element={<p>Home</p>} />
              </Route>
            </Routes>
          </MemoryRouter>
        </ProductionApiProvider>
      </FlexQueryProvider>,
    );

    expect(await screen.findByRole("button", { name: /operator menu, administrator demo administrator/i })).toBeInTheDocument();
    expect(document.querySelector(".strip-brand")).not.toHaveClass("strip-brand--origin");
    expect(screen.getByText("Demo Administrator")).toBeInTheDocument();
    expect(document.querySelector(".strip-profile-key .strip-profile-capacity")).toBeNull();
    expect(screen.queryByText("ORG")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /light theme|dark theme/i })).not.toBeInTheDocument();
    expect(document.querySelector(".toast-dock")).toHaveAttribute("aria-live", "polite");
    expect(document.querySelector(".toast-dock")).toHaveAttribute("data-placement", "bottom-center");
    expect(screen.queryByRole("button", { name: "Sign out" })).not.toBeInTheDocument();
    expect(screen.queryByRole("menuitem")).not.toBeInTheDocument();

    await openOperatorMenu();
    expect(screen.getByRole("menuitem", { name: "Switch to light theme" })).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: "Sign out" })).toBeInTheDocument();
    const menu = screen.getByRole("menu");
    expect(menu.querySelector(".strip-profile-role")).toHaveTextContent("Administrator");
    expect(menu).toHaveTextContent("Demo Administrator");
    expect(menu).not.toHaveTextContent("Organization");
  });

  it("does not wrap the assignment locator in the management gangway", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return json(200, { authenticated: true, csrf_token: "csrf" });
      }
      if (url.includes("/v1/assessment/shell")) {
        return json(200, {
          schema_version: "v1",
          actor_id: "actor-1",
          organization_id: "org-1",
          relationship: "participant",
          navigation: [{ destination_id: "my-work", is_available: true }],
          permitted_actions: [],
        });
      }
      return json(404, {});
    }));

    render(
      <FlexQueryProvider>
        <ProductionApiProvider>
          <MemoryRouter initialEntries={["/my-work/enr-1"]}>
            <Routes>
              <Route element={<ProductionAppShell />}>
                <Route path="/my-work/:enrollmentId" element={<p>Assignment stub</p>} />
              </Route>
            </Routes>
          </MemoryRouter>
        </ProductionApiProvider>
      </FlexQueryProvider>,
    );

    expect(await screen.findByText("Assignment stub")).toBeInTheDocument();
    expect(screen.queryByRole("navigation", { name: "Primary navigation" })).not.toBeInTheDocument();
    expect(document.querySelector('[data-layout="management"]')).toBeNull();
  });
});
