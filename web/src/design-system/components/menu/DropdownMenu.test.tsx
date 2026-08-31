import { fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import { DropdownMenu, DropdownMenuItem } from "./DropdownMenu";

function MenuHarness() {
  const [open, setOpen] = useState(false);
  return (
    <>
      <DropdownMenu
        open={open}
        onOpenChange={setOpen}
        trigger={(bind) => (
          <button type="button" {...bind}>
            Account
          </button>
        )}
      >
        <DropdownMenuItem onSelect={() => setOpen(false)}>Sign out</DropdownMenuItem>
      </DropdownMenu>
      <button type="button">Elsewhere</button>
    </>
  );
}

describe("DropdownMenu dismissal", () => {
  it("closes on external scroll without moving the overlay", () => {
    render(<MenuHarness />);
    fireEvent.click(screen.getByRole("button", { name: "Account" }));
    expect(screen.getByRole("menu")).toBeInTheDocument();
    fireEvent.scroll(window);
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
  });

  it("does not steal focus on outside pointer dismissal", () => {
    render(<MenuHarness />);
    const trigger = screen.getByRole("button", { name: "Account" });
    fireEvent.click(trigger);
    const focus = vi.spyOn(trigger, "focus");
    focus.mockClear();
    fireEvent.pointerDown(screen.getByRole("button", { name: "Elsewhere" }));
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
    expect(focus).not.toHaveBeenCalled();
  });

  it("restores trigger focus on Escape", () => {
    render(<MenuHarness />);
    const trigger = screen.getByRole("button", { name: "Account" });
    fireEvent.click(trigger);
    fireEvent.keyDown(screen.getByRole("menu"), { key: "Escape" });
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
    expect(document.activeElement).toBe(trigger);
  });
});
