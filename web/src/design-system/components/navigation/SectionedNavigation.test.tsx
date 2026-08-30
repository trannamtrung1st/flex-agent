import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { Gangway } from "./Gangway";
import { hashNavigationStrategy } from "./navigationStrategies";
import { SectionedNavigation } from "./SectionedNavigation";

const indexGroups = [
  {
    id: "foundations",
    label: "Foundations",
    items: [
      { id: "color", label: "Color" },
      { id: "type", label: "Type voices" },
    ],
  },
  {
    id: "navigation",
    label: "Navigation",
    items: [{ id: "strip", label: "Command strip" }],
  },
] as const;

const gangwayGroups = [
  {
    id: "ops",
    label: "Assessment operations",
    items: [
      { to: "/campaigns", label: "Campaigns", abbr: "CAM" },
      { to: "/enrollments", label: "Enrollments", abbr: "ENR", current: true },
    ],
  },
  {
    id: "gov",
    label: "Governance",
    collapsible: false,
    items: [{ to: "/audit", label: "Audit Log", abbr: "AUD" }],
  },
] as const;

describe("SectionedNavigation collapsible groups", () => {
  it("lets index groups collapse independently on desktop", () => {
    render(
      <nav>
        <SectionedNavigation
          groups={indexGroups}
          strategy={hashNavigationStrategy("color")}
          variant="index"
        />
      </nav>,
    );

    const foundations = screen.getByText("Foundations").closest("summary");
    expect(foundations).not.toBeNull();
    expect(foundations).toHaveClass("gangway-section-label");
    expect(foundations!.closest("details")).toHaveAttribute("open");

    fireEvent.click(foundations!);

    expect(foundations!.closest("details")).not.toHaveAttribute("open");
    expect(screen.getByRole("link", { name: "Command strip" }).closest("details")).toHaveAttribute("open");
  });

  it("reopens a collapsed index group when the current item moves into it", () => {
    const { rerender } = render(
      <nav>
        <SectionedNavigation
          groups={indexGroups}
          strategy={hashNavigationStrategy("color")}
          variant="index"
        />
      </nav>,
    );

    fireEvent.click(screen.getByText("Navigation").closest("summary")!);
    expect(screen.getByText("Navigation").closest("details")).not.toHaveAttribute("open");

    rerender(
      <nav>
        <SectionedNavigation
          groups={indexGroups}
          strategy={hashNavigationStrategy("strip")}
          variant="index"
        />
      </nav>,
    );

    expect(screen.getByText("Navigation").closest("details")).toHaveAttribute("open");
  });

  it("keeps gangway groups static unless collapsibleGroups is enabled", () => {
    render(
      <MemoryRouter>
        <Gangway
          title="Administrator"
          groups={gangwayGroups}
          collapsed={false}
          onCollapsedChange={() => undefined}
        />
      </MemoryRouter>,
    );

    expect(screen.getByText("Assessment operations").closest("summary")).toBeNull();
    expect(screen.getByRole("link", { name: "Campaigns" })).toBeInTheDocument();
  });

  it("collapses opted-in gangway groups and honors a per-group opt-out", () => {
    render(
      <MemoryRouter>
        <Gangway
          title="Administrator"
          groups={gangwayGroups}
          collapsed={false}
          onCollapsedChange={() => undefined}
          collapsibleGroups
        />
      </MemoryRouter>,
    );

    const ops = screen.getByText("Assessment operations").closest("summary");
    expect(ops).not.toBeNull();
    expect(screen.getByText("Governance").closest("summary")).toBeNull();

    fireEvent.click(ops!);
    expect(ops!.closest("details")).not.toHaveAttribute("open");
    expect(screen.getByText("Governance").closest("section")).not.toBeNull();
  });

  it("keeps gangway destinations visible when the rail is width-collapsed", () => {
    function Harness({ collapsed }: { collapsed: boolean }) {
      return (
        <MemoryRouter>
          <Gangway
            title="Administrator"
            groups={gangwayGroups}
            collapsed={collapsed}
            onCollapsedChange={() => undefined}
            collapsibleGroups
          />
        </MemoryRouter>
      );
    }

    const { rerender } = render(<Harness collapsed={false} />);

    const ops = screen.getByText("Assessment operations").closest("summary");
    fireEvent.click(ops!);
    expect(ops!.closest("details")).not.toHaveAttribute("open");

    rerender(<Harness collapsed />);

    expect(ops!.closest("details")).toHaveAttribute("open");
  });
});
