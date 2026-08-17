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
});
