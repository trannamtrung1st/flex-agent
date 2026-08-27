import { useState } from "react";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AreaGroupList, Gangway, IndexRail, type GangwayGroup } from ".";

afterEach(() => {
  vi.unstubAllGlobals();
});

const routeGroups: readonly GangwayGroup[] = [
  {
    label: "Assessment operations",
    items: [
      {
        to: "/admin-console/enrollments?campaign=CMP-0043",
        label: "Enrollments",
        abbr: "ENR",
        current: true,
      },
      {
        to: "/admin-console/sessions?campaign=CMP-0043",
        label: "Sessions",
        abbr: "SES",
      },
    ],
  },
];

describe("route section rendering", () => {
  it("preserves route destinations, labels, abbreviations, and page markers", () => {
    const { container } = render(
      <MemoryRouter>
        <AreaGroupList groups={routeGroups} variant="gangway" />
      </MemoryRouter>,
    );

    const current = screen.getByRole("link", { name: "Enrollments" });
    expect(current).toHaveAttribute("href", "/admin-console/enrollments?campaign=CMP-0043");
    expect(current).toHaveAttribute("aria-current", "page");
    expect(current).toHaveClass("gangway-link", "tip-trailing", "is-current");
    expect(current).toHaveAttribute("data-tip", "Enrollments");
    expect(container.querySelector(".gangway-abbr")).toHaveTextContent("ENR");
    expect(container.querySelector(".gangway-section-node")).toBeVisible();
    expect(container.querySelector(".gangway-section-label-text")).toHaveTextContent("Assessment operations");
    expect(screen.getByRole("link", { name: "Sessions" })).not.toHaveAttribute("aria-current");
  });

  it("renders the same groups as rail links for the bulkhead", () => {
    render(
      <MemoryRouter>
        <nav aria-label="Administrator areas">
          <AreaGroupList groups={routeGroups} variant="rail" />
        </nav>
      </MemoryRouter>,
    );

    const navigation = screen.getByRole("navigation", { name: "Administrator areas" });
    expect(within(navigation).getByRole("link", { name: "Enrollments" })).toHaveClass("nav-link", "is-current");
    expect(within(navigation).getAllByRole("link")).toHaveLength(2);
  });
});

describe("IndexRail", () => {
  it("renders open detail groups with hash destinations and location markers", () => {
    const { container } = render(
      <IndexRail
        activeId="keys"
        groups={[
          {
            id: "foundations",
            label: "Foundations",
            items: [
              { id: "colors", label: "Colors" },
              { id: "keys", label: "Keys" },
            ],
          },
        ]}
      />,
    );

    expect(screen.getByRole("navigation", { name: "Component index" })).toHaveClass("deck-rail");
    expect(container.querySelector("details.nav-rail-section")).toHaveAttribute("open");
    expect(screen.getByText("Foundations").closest(".gangway-section-label-text")).toBeVisible();
    expect(container.querySelector(".gangway-section-node")).toBeVisible();
    expect(screen.getByRole("link", { name: "Keys" })).toHaveAttribute("href", "#keys");
    expect(screen.getByRole("link", { name: "Keys" })).toHaveAttribute("aria-current", "location");
    expect(screen.getByRole("link", { name: "Colors" })).not.toHaveAttribute("aria-current");
  });

  it("keeps the newly selected compact group open", async () => {
    vi.stubGlobal("matchMedia", vi.fn().mockReturnValue({ matches: true }));
    const user = userEvent.setup();
    const { container } = render(
      <IndexRail
        groups={[
          {
            id: "foundations",
            label: "Foundations",
            items: [{ id: "colors", label: "Colors" }],
          },
          {
            id: "navigation",
            label: "Navigation",
            items: [{ id: "gangway", label: "Gangway" }],
          },
        ]}
      />,
    );
    const groups = container.querySelectorAll("details.nav-rail-section");

    expect(groups[0]).toHaveAttribute("open");
    expect(groups[1]).not.toHaveAttribute("open");

    await user.click(screen.getByText("Navigation").closest("summary")!);

    expect(groups[0]).not.toHaveAttribute("open");
    expect(groups[1]).toHaveAttribute("open");
  });
});

describe("Gangway", () => {
  it("reports controlled collapse changes and preserves optional footer content", async () => {
    const user = userEvent.setup();

    function Harness() {
      const [collapsed, setCollapsed] = useState(false);
      return (
        <MemoryRouter>
          <Gangway
            title="Administrator"
            groups={routeGroups}
            collapsed={collapsed}
            onCollapsedChange={setCollapsed}
            ariaLabel="Administrator areas"
            footer={<span>Operator ADM-7X92-19</span>}
          />
        </MemoryRouter>
      );
    }

    render(<Harness />);
    const gangway = screen.getByRole("navigation", { name: "Administrator areas" });
    const toggle = screen.getByRole("button", { name: "Collapse menu" });
    expect(toggle).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByText("Operator ADM-7X92-19")).toBeVisible();

    await user.click(toggle);
    expect(gangway).toHaveClass("is-collapsed");
    expect(screen.getByRole("button", { name: "Expand menu" })).toHaveAttribute("aria-expanded", "false");
  });

  it("uses a domain-neutral default navigation label", () => {
    render(
      <MemoryRouter>
        <Gangway
          title="Operations"
          groups={routeGroups}
          collapsed={false}
          onCollapsedChange={() => undefined}
        />
      </MemoryRouter>,
    );

    expect(screen.getByRole("navigation", { name: "Areas" })).toBeVisible();
  });
});
