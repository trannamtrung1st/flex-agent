import { fireEvent, render } from "@testing-library/react";
import { NativeDialog } from "./NativeDialog";

function cancelDialog(dialog: HTMLDialogElement) {
  fireEvent(dialog, new Event("cancel", { bubbles: true, cancelable: true }));
}

describe("NativeDialog", () => {
  it("closes on cancel when no nested overlay is expanded", () => {
    const onClose = vi.fn();
    render(
      <NativeDialog open onClose={onClose} className="dialog" labelledBy="title">
        <h2 id="title">Confirm</h2>
        <button type="button">Ok</button>
      </NativeDialog>,
    );
    const dialog = document.querySelector("dialog");
    expect(dialog).not.toBeNull();
    cancelDialog(dialog!);
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("keeps the dialog open when Escape dismisses an expanded nested picker", () => {
    const onClose = vi.fn();
    render(
      <NativeDialog open onClose={onClose} className="dialog" labelledBy="title">
        <h2 id="title">Confirm</h2>
        <button type="button" aria-expanded="true" aria-haspopup="dialog">
          Requested value
        </button>
      </NativeDialog>,
    );
    const dialog = document.querySelector("dialog");
    expect(dialog).not.toBeNull();
    fireEvent.keyDown(dialog!, { key: "Escape" });
    cancelDialog(dialog!);
    expect(onClose).not.toHaveBeenCalled();
  });

  it("closes on the next Escape after a nested picker collapses without cancel", () => {
    const onClose = vi.fn();
    render(
      <NativeDialog open onClose={onClose} className="dialog" labelledBy="title">
        <h2 id="title">Confirm</h2>
        <button type="button" aria-expanded="true" aria-haspopup="dialog">
          Requested value
        </button>
      </NativeDialog>,
    );
    const dialog = document.querySelector("dialog");
    const trigger = dialog!.querySelector("button");
    expect(dialog).not.toBeNull();
    fireEvent.keyDown(dialog!, { key: "Escape" });
    trigger?.setAttribute("aria-expanded", "false");
    fireEvent.keyUp(dialog!, { key: "Escape" });
    cancelDialog(dialog!);
    expect(onClose).toHaveBeenCalledTimes(1);
  });
});
