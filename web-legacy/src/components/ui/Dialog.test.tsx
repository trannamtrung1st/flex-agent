import { useState } from "react";
import { fireEvent, render, screen, within } from "@testing-library/react";
import { Dialog } from "./Dialog";

describe("Dialog", () => {
  it("keeps Tab and Shift+Tab within the modal", () => {
    render(
      <>
        <button type="button">Outside</button>
        <Dialog
          open
          title="Complete this Session?"
          confirmLabel="Complete Session"
          cancelLabel="Continue Session"
          onConfirm={() => undefined}
          onCancel={() => undefined}
        >
          <p>After completion begins, you cannot send more messages.</p>
        </Dialog>
      </>,
    );

    const dialog = screen.getByRole("dialog");
    const cancel = within(dialog).getByRole("button", { name: /continue session/i });
    const confirm = within(dialog).getByRole("button", { name: /^complete session$/i });
    const outside = screen.getByRole("button", { name: /outside/i });

    confirm.focus();
    fireEvent.keyDown(document, { key: "Tab" });
    expect(document.activeElement).toBe(cancel);

    fireEvent.keyDown(document, { key: "Tab", shiftKey: true });
    expect(document.activeElement).toBe(confirm);
    expect(document.activeElement).not.toBe(outside);
  });

  it("clears inert before restoring the trigger's focus", () => {
    function Harness() {
      const [open, setOpen] = useState(false);
      return (
        <>
          <button type="button" onClick={() => { setOpen(true); }}>Complete Session</button>
          <Dialog
            open={open}
            title="Complete this Session?"
            confirmLabel="Complete Session"
            cancelLabel="Continue Session"
            initialFocus="title"
            onConfirm={() => undefined}
            onCancel={() => { setOpen(false); }}
          >
            <p>After completion begins, you cannot send more messages.</p>
          </Dialog>
        </>
      );
    }

    render(<Harness />);
    const trigger = screen.getByRole("button", { name: /^complete session$/i });
    trigger.focus();
    fireEvent.click(trigger);

    const dialog = screen.getByRole("dialog");
    const originalFocus = Function.prototype.call.bind(
      Object.getOwnPropertyDescriptor(HTMLElement.prototype, "focus")?.value as (
        this: HTMLElement,
        options?: FocusOptions,
      ) => void,
    ) as (element: HTMLElement, options?: FocusOptions) => void;
    const restorations: boolean[] = [];
    const spy = vi.spyOn(HTMLElement.prototype, "focus").mockImplementation(function (
      this: HTMLElement,
      options?: FocusOptions,
    ) {
      if (this === trigger) {
        restorations.push(this.closest("[inert]") !== null);
      }
      originalFocus(this, options);
    });

    fireEvent.click(within(dialog).getByRole("button", { name: /continue session/i }));
    spy.mockRestore();

    expect(restorations).toEqual([false]);
    expect(document.activeElement).toBe(trigger);
  });
});
