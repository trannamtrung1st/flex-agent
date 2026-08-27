import { MemoryRouter } from "react-router";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { CommandStrip, IconButton, Key, RailBrand, StripBrand, TooltipHost } from "../src/components";

describe("CommandStrip navigation", () => {
  it("marks only the matching hash link active on the same route", () => {
    render(
      <MemoryRouter initialEntries={["/shared/gallery#strip"]}>
        <CommandStrip
          nav={[
            { to: "/shared/gallery#strip", label: "Home" },
            { to: "/shared/gallery#tabs", label: "Tabs" },
          ]}
        />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: "Home" })).toHaveClass("is-current");
    expect(screen.getByRole("link", { name: "Tabs" })).not.toHaveClass("is-current");
  });

  it("keeps route-only nav behavior unchanged", () => {
    render(
      <MemoryRouter initialEntries={["/participant-home"]}>
        <CommandStrip nav={[{ to: "/participant-home", label: "Home" }]} />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: "Home" })).toHaveClass("is-current");
  });

  it("honors explicit current and inactive specimen tokens", () => {
    render(
      <MemoryRouter initialEntries={["/shared/gallery#tabs"]}>
        <CommandStrip
          nav={[
            { to: "/shared/gallery#strip", label: "Home", current: true },
            { to: "/shared/gallery#strip", label: "Assignments", inactive: true },
          ]}
        />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: "Home" })).toHaveClass("is-current");
    expect(screen.queryByRole("link", { name: "Assignments" })).not.toBeInTheDocument();
    expect(screen.getByText("Assignments")).toHaveClass("strip-token");
    expect(screen.getByText("Assignments")).not.toHaveClass("is-current");
  });

  it("matches administrator home links with campaign search params", () => {
    render(
      <MemoryRouter initialEntries={["/admin-console/enrollments?campaign=CMP-0043"]}>
        <CommandStrip
          nav={[
            {
              to: { pathname: "/admin-console/enrollments", search: "?campaign=CMP-0043" },
              label: "Home",
            },
          ]}
        />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: "Home" })).toHaveClass("is-current");
  });
});

describe("brand navigation", () => {
  it("links strip branding to the channel index", () => {
    render(
      <MemoryRouter>
        <StripBrand suffix="Admin" />
      </MemoryRouter>,
    );
    const link = screen.getByRole("link", { name: /channel index/i });
    expect(link).toHaveAttribute("href", "/surfaces");
    expect(link).toHaveTextContent("Flex Agent");
  });

  it("links rail branding to the channel index", () => {
    render(
      <MemoryRouter>
        <RailBrand suffix="Assignment Station" />
      </MemoryRouter>,
    );
    const link = screen.getByRole("link", { name: /channel index/i });
    expect(link).toHaveAttribute("href", "/surfaces");
  });
});

describe("TooltipHost", () => {
  it("renders children without a host when no tip is provided", () => {
    const { container } = render(
      <TooltipHost>
        <button type="button">Action</button>
      </TooltipHost>,
    );

    expect(container.querySelector(".tip-host")).toBeNull();
    expect(screen.getByRole("button", { name: "Action" })).toBeInTheDocument();
  });

  it("forwards a custom className on the host wrapper", () => {
    render(
      <TooltipHost tip="More actions" className="custom-host">
        <button type="button">More</button>
      </TooltipHost>,
    );

    expect(screen.getByRole("button", { name: "More" }).parentElement).toHaveClass("tip-host", "custom-host");
  });

  it("shows a portaled plaque on hover", async () => {
    const user = userEvent.setup();
    render(
      <TooltipHost tip="Export summary">
        <button type="button">Export</button>
      </TooltipHost>,
    );

    const button = screen.getByRole("button", { name: "Export" });
    expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();

    await user.hover(button);
    expect(screen.getByRole("tooltip")).toHaveTextContent("Export summary");

    await user.unhover(button);
    expect(screen.queryByRole("tooltip")).not.toBeInTheDocument();
  });

  it("shows a portaled plaque when the child receives focus", async () => {
    const user = userEvent.setup();
    render(
      <TooltipHost tip="Export summary">
        <button type="button">Export</button>
      </TooltipHost>,
    );

    await user.tab();
    expect(screen.getByRole("button", { name: "Export" })).toHaveFocus();
    expect(screen.getByRole("tooltip")).toHaveTextContent("Export summary");
  });
});

describe("Key tooltip integration", () => {
  it("uses TooltipHost for supplementary tooltips", async () => {
    const user = userEvent.setup();
    render(
      <Key size="compact" tooltip="Export summary">
        Export
      </Key>,
    );

    const button = screen.getByRole("button", { name: "Export" });
    expect(button.parentElement).toHaveClass("tip-host");
    await user.hover(button);
    expect(screen.getByRole("tooltip")).toHaveTextContent("Export summary");
  });

  it("exposes disabled reasons through aria-describedby and the tooltip host", async () => {
    const user = userEvent.setup();
    render(
      <Key size="compact" disabled disabledReason="Select one or more campaigns.">
        Export
      </Key>,
    );

    const button = screen.getByRole("button", { name: /Export/i });
    const reason = screen.getByText("Select one or more campaigns.");
    expect(button).toHaveAttribute("aria-describedby", reason.id);
    await user.hover(button);
    expect(screen.getByRole("tooltip")).toHaveTextContent("Select one or more campaigns.");
  });
});

describe("IconButton tooltip integration", () => {
  it("wraps the trigger with TooltipHost when a tooltip is provided", async () => {
    const user = userEvent.setup();
    render(
      <IconButton label="More actions" tooltip="More actions">
        <span aria-hidden="true">…</span>
      </IconButton>,
    );

    const button = screen.getByRole("button", { name: "More actions" });
    expect(button.parentElement).toHaveClass("tip-host");
    await user.tab();
    expect(button).toHaveFocus();
    expect(screen.getByRole("tooltip")).toHaveTextContent("More actions");
  });
});
