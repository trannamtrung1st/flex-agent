import { useState } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { DropdownMenu, DropdownMenuItem } from "./DropdownMenu";

function Harness({ placement }: { placement?: "connected" | "fixed" }) {
  const [open, setOpen] = useState(false);
  return (
    <DropdownMenu
      open={open}
      onOpenChange={setOpen}
      labelledBy="menu-trigger"
      placement={placement}
      trigger={(bind) => (
        <button
          ref={bind.ref}
          id="menu-trigger"
          type="button"
          aria-haspopup={bind["aria-haspopup"]}
          aria-expanded={bind["aria-expanded"]}
          onClick={bind.onClick}
          onKeyDown={bind.onKeyDown}
        >
          More
        </button>
      )}
    >
      <DropdownMenuItem onSelect={() => undefined}>Delete</DropdownMenuItem>
    </DropdownMenu>
  );
}

describe("DropdownMenu", () => {
  it("attaches a connected panel under the trigger", async () => {
    const user = userEvent.setup();
    const { container } = render(<Harness />);
    await user.click(screen.getByRole("button", { name: "More" }));
    const panel = screen.getByRole("menu");
    expect(panel).toHaveClass("menu-popover", "select-popover");
    expect(panel).not.toHaveClass("menu-popover--fixed");
    expect(container.querySelector(".menu-shell")?.contains(panel)).toBe(true);
  });

  it("portals a flush fixed panel for clipping parents", async () => {
    const user = userEvent.setup();
    const { container } = render(<Harness placement="fixed" />);
    await user.click(screen.getByRole("button", { name: "More" }));
    const panel = screen.getByRole("menu");
    expect(panel).toHaveClass("menu-popover--fixed");
    expect(container.querySelector(".menu-shell")?.contains(panel)).toBe(false);
  });

  it("moves from the trigger into the first item with ArrowDown", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    const trigger = screen.getByRole("button", { name: "More" });
    trigger.focus();
    await user.keyboard("{ArrowDown}");
    expect(screen.getByRole("menuitem", { name: "Delete" })).toHaveFocus();
  });
});
