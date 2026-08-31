import { fireEvent, render, screen } from "@testing-library/react";
import { DatePicker } from "../temporal/DateTimePicker";
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

  it("seats in-flow children in a stage so portaled overlays are not flex items of the dialog", () => {
    render(
      <NativeDialog open onClose={() => undefined} className="dialog" labelledBy="title">
        <h2 id="title">Confirm</h2>
        <button type="button">Ok</button>
      </NativeDialog>,
    );
    const dialog = document.querySelector("dialog");
    const stage = dialog?.querySelector(":scope > .dialog-stage");
    expect(stage).toBeTruthy();
    expect(stage).toContainElement(screen.getByRole("heading", { name: "Confirm" }));
    expect(dialog?.querySelector(":scope > h2")).toBeNull();
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

  it("dismisses a nested picker on dialog-body scroll without closing the dialog", () => {
    const onClose = vi.fn();
    render(
      <NativeDialog open onClose={onClose} className="dialog" labelledBy="title">
        <h2 id="title">Confirm</h2>
        <div data-testid="dialog-body" style={{ overflow: "auto", height: 40 }}>
          <DatePicker labelId="title" value="" onChange={() => undefined} now="2026-08-26" />
          <div style={{ height: 200 }}>pad</div>
        </div>
      </NativeDialog>,
    );
    fireEvent.click(screen.getByRole("button", { name: /select date/i }));
    expect(screen.getByRole("dialog", { name: "Choose date" })).toBeInTheDocument();
    fireEvent.scroll(screen.getByTestId("dialog-body"));
    expect(screen.queryByRole("dialog", { name: "Choose date" })).not.toBeInTheDocument();
    expect(onClose).not.toHaveBeenCalled();
  });
});
