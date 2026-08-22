import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { ProductionApiProvider } from "../api/production-api";
import { ProductionMyWorkPage } from "./ProductionMyWorkPage";

describe("ProductionMyWorkPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("shows empty and active assignment states", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve({ authenticated: true, csrf_token: "csrf" }) });
      }
      if (url.includes("/v1/assessment/shell")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            actor_id: "part",
            organization_id: "org",
            relationship: "",
            navigation: [{ destination_id: "my-work", is_available: true }],
            permitted_actions: ["assessment.assignment.discover"],
          }),
        });
      }
      if (url.includes("/v1/assessment/my-work")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            items: [{
              enrollment_id: "enr-1",
              status: "active",
              visibility: "current",
              activity_title: "Campaign",
              summary_available: true,
              permitted_actions: ["open_assignment"],
            }],
            has_more: false,
          }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/my-work"]}>
          <Routes>
            <Route path="/my-work" element={<ProductionMyWorkPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>,
    );

    expect(await screen.findByRole("heading", { name: "My work" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Open assignment" })).toBeInTheDocument();
    expect(screen.queryByText("Start Attempt")).not.toBeInTheDocument();
  });

  it("shows empty and suspended assignments without start-attempt actions", async () => {
    let calls = 0;
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve({ authenticated: true, csrf_token: "csrf" }) });
      }
      if (url.includes("/v1/assessment/shell")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            actor_id: "part",
            organization_id: "org",
            relationship: "",
            navigation: [{ destination_id: "my-work", is_available: true }],
            permitted_actions: ["assessment.assignment.discover"],
          }),
        });
      }
      if (url.includes("/v1/assessment/my-work")) {
        calls += 1;
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            items: calls === 1
              ? []
              : [{
                enrollment_id: "enr-2",
                status: "suspended",
                visibility: "restricted",
                activity_title: "Campaign",
                summary_available: true,
                permitted_actions: ["return_to_my_work"],
              }],
            has_more: false,
          }),
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    const first = render(
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/my-work"]}>
          <Routes>
            <Route path="/my-work" element={<ProductionMyWorkPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>,
    );
    expect(await screen.findByText("You have no current assignments.")).toBeInTheDocument();
    first.unmount();

    render(
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/my-work"]}>
          <Routes>
            <Route path="/my-work" element={<ProductionMyWorkPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>,
    );
    expect(await screen.findByText(/suspended/)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Return to Home" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Open assignment" })).not.toBeInTheDocument();
    expect(screen.queryByText("Start Attempt")).not.toBeInTheDocument();
  });

  it("explains a rate-limited list as a recoverable wait", async () => {
    vi.stubGlobal("fetch", vi.fn((input: RequestInfo | URL) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.includes("/auth/session")) {
        return Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve({ authenticated: true, csrf_token: "csrf" }) });
      }
      if (url.includes("/v1/assessment/shell")) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({
            schema_version: "v1",
            actor_id: "part",
            organization_id: "org",
            relationship: "",
            navigation: [{ destination_id: "my-work", is_available: true }],
            permitted_actions: ["assessment.assignment.discover"],
          }),
        });
      }
      if (url.includes("/v1/assessment/my-work")) {
        return Promise.resolve({
          ok: false,
          status: 429,
          json: () => Promise.resolve({ error: "enrollment.rate_limited" }),
          clone() {
            return this;
          },
        });
      }
      return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) });
    }));

    render(
      <ProductionApiProvider>
        <MemoryRouter initialEntries={["/my-work"]}>
          <Routes>
            <Route path="/my-work" element={<ProductionMyWorkPage />} />
          </Routes>
        </MemoryRouter>
      </ProductionApiProvider>,
    );

    expect(await screen.findByRole("heading", { name: "Too many requests" })).toBeInTheDocument();
    expect(screen.getByText("Too many requests. Wait a moment, then try again.")).toBeInTheDocument();
    expect(screen.queryByText("My work is not available.")).not.toBeInTheDocument();
  });
});
