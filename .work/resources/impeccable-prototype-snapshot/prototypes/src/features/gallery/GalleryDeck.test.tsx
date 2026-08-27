import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { GalleryDeck } from "./GalleryDeck";
import { gallerySections } from "./gallerySections";

beforeAll(() => {
  Object.defineProperty(window, "matchMedia", {
    configurable: true,
    value: vi.fn().mockReturnValue({
      matches: false,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    }),
  });
  window.scrollTo = vi.fn();
});

afterEach(() => {
  window.history.replaceState(null, "", "/shared/gallery");
  vi.mocked(window.scrollTo).mockClear();
});

describe("GalleryDeck", () => {
  it("renders the typed 27-section catalog with shared component specimens", () => {
    const { container } = render(<MemoryRouter><GalleryDeck /></MemoryRouter>);
    const expectedIds = gallerySections.flatMap((group) => group.items.map((item) => item.id));
    expect(container.querySelectorAll(".deck-sec")).toHaveLength(expectedIds.length);
    expect([...container.querySelectorAll<HTMLElement>(".deck-sec")].map((section) => section.id)).toEqual(expectedIds);
    expect([...container.querySelectorAll<HTMLElement>(".deck-sec")].map((section) => Number(section.dataset.galleryOrder))).toEqual(
      expectedIds.map((_, index) => index),
    );
    expect(screen.getByRole("navigation", { name: "Component index" })).toBeVisible();
    expect(container.querySelector("header.command-strip.page-strip")).toBeVisible();
    expect(container.querySelector("[data-gangway]")).toBeVisible();
    expect(container.querySelector("#dtBody .datatable-row")).toBeVisible();
    expect(container.querySelector("#demoDropKey")).toBeVisible();
    expect(container.querySelector("#demoSearchKey")).toBeVisible();
    expect(container.querySelector("#demoContextSearchKey")).toBeVisible();
    expect(container.querySelector("#dialogOpenKey")).toBeVisible();
    expect(screen.getByRole("button", { name: "Campaigns" })).toHaveClass("key--back");
    expect(container.querySelector("#demoLimit")).toHaveAttribute("id", "demoLimit");
    expect(container.querySelector("#demoDate")).toBeVisible();
    expect(container.querySelector("#demoTime")).toBeVisible();
    expect(container.querySelector("#demoDateTime")).toBeVisible();
    const optionMenu = screen.getByRole("listbox", { name: "Option menu specimen" });
    expect(optionMenu).toBeVisible();
    expect(within(optionMenu).getByRole("option", { name: "Examination" })).toHaveAttribute("aria-selected", "true");
  });

  it("commits option-menu specimen rows from click and keyboard", async () => {
    const user = userEvent.setup();
    render(<MemoryRouter><GalleryDeck /></MemoryRouter>);
    const optionMenu = screen.getByRole("listbox", { name: "Option menu specimen" });

    await user.click(within(optionMenu).getByRole("option", { name: "Review" }));
    expect(within(optionMenu).getByRole("option", { name: "Review" })).toHaveAttribute("aria-selected", "true");
    expect(within(optionMenu).getByRole("option", { name: "Examination" })).toHaveAttribute("aria-selected", "false");

    within(optionMenu).getByRole("option", { name: "Review" }).focus();
    await user.keyboard("{ArrowDown}{Enter}");
    expect(within(optionMenu).getByRole("option", { name: "Released" })).toHaveAttribute("aria-selected", "true");
  });

  it("uses the campaign-registry row overflow icon in the datatable specimen", async () => {
    const user = userEvent.setup();
    const { container } = render(<MemoryRouter><GalleryDeck /></MemoryRouter>);
    const trigger = container.querySelector("#dtBody .col-action .icon-button.command-menu-trigger--icon");
    expect(trigger).toBeTruthy();
    expect(container.querySelector("#dtBody .col-action .key")).toBeNull();

    await user.click(screen.getByRole("button", { name: "Actions for P-3114" }));
    expect(screen.getByRole("menuitem", { name: "View record" })).toBeVisible();
    expect(screen.getByRole("menuitem", { name: "Transcript" })).toBeVisible();
  });

  it("initializes and updates the current marker from the hash", async () => {
    const user = userEvent.setup();
    window.history.replaceState(null, "", "/shared/gallery#dialog");
    render(<MemoryRouter><GalleryDeck /></MemoryRouter>);

    expect(screen.getByRole("link", { name: "Dialog" })).toHaveAttribute("aria-current", "location");
    await user.click(screen.getByRole("link", { name: "Colors" }));
    await waitFor(() => expect(window.location.hash).toBe("#colors"));
    expect(screen.getByRole("link", { name: "Colors" })).toHaveAttribute("aria-current", "location");
  });

  it("uses reduced-motion scrolling and shares toast state through React", async () => {
    const user = userEvent.setup();
    vi.mocked(window.matchMedia).mockImplementation((query) => ({
      matches: query.includes("prefers-reduced-motion"),
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }));
    render(<MemoryRouter><GalleryDeck /></MemoryRouter>);

    await user.click(screen.getByRole("link", { name: "Keys" }));
    expect(window.scrollTo).toHaveBeenLastCalledWith(expect.objectContaining({ behavior: "auto" }));

    await user.click(screen.getByRole("button", { name: "Create" }));
    expect(screen.getByText("Gallery-only create action demonstrated.").closest('[role="status"]')).toHaveClass("toast");
  });
});
