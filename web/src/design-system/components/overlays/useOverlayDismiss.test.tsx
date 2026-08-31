import { act, fireEvent, render, screen } from "@testing-library/react";
import { useRef, useState } from "react";
import { useOverlayDismiss } from "./useOverlayDismiss";

function Harness({
  onDismiss,
  pointer = true,
  focus = true,
  scroll = true,
}: {
  onDismiss: () => void;
  pointer?: boolean;
  focus?: boolean;
  scroll?: boolean;
}) {
  const triggerRef = useRef<HTMLButtonElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(true);
  useOverlayDismiss(open, [triggerRef, panelRef], () => {
    onDismiss();
    setOpen(false);
  }, { pointer, focus, scroll });
  return (
    <div>
      <div data-testid="scroller" style={{ overflow: "auto", height: 40 }}>
        <button ref={triggerRef} type="button">
          Trigger
        </button>
        <div style={{ height: 200 }}>pad</div>
      </div>
      {open ? (
        <div ref={panelRef} data-testid="panel">
          <div data-testid="inner-scroll" style={{ overflow: "auto", height: 40 }}>
            <div style={{ height: 200 }}>inner</div>
          </div>
          <button type="button">Inside</button>
        </div>
      ) : null}
      <button type="button">Outside</button>
    </div>
  );
}

describe("useOverlayDismiss", () => {
  it("closes on pointer outside the trigger and panel composite", () => {
    const onDismiss = vi.fn();
    render(<Harness onDismiss={onDismiss} />);
    fireEvent.pointerDown(screen.getByRole("button", { name: "Outside" }));
    expect(onDismiss).toHaveBeenCalledTimes(1);
    expect(screen.queryByTestId("panel")).not.toBeInTheDocument();
  });

  it("ignores pointer inside the panel", () => {
    const onDismiss = vi.fn();
    render(<Harness onDismiss={onDismiss} />);
    fireEvent.pointerDown(screen.getByRole("button", { name: "Inside" }));
    expect(onDismiss).not.toHaveBeenCalled();
    expect(screen.getByTestId("panel")).toBeInTheDocument();
  });

  it("closes when focus leaves the complete composite", () => {
    const onDismiss = vi.fn();
    render(<Harness onDismiss={onDismiss} />);
    screen.getByRole("button", { name: "Inside" }).focus();
    act(() => {
      screen.getByRole("button", { name: "Outside" }).focus();
    });
    expect(onDismiss).toHaveBeenCalledTimes(1);
    expect(screen.queryByTestId("panel")).not.toBeInTheDocument();
  });

  it("does not close when focus moves from the trigger into the panel", () => {
    const onDismiss = vi.fn();
    render(<Harness onDismiss={onDismiss} />);
    screen.getByRole("button", { name: "Trigger" }).focus();
    screen.getByRole("button", { name: "Inside" }).focus();
    expect(onDismiss).not.toHaveBeenCalled();
    expect(screen.getByTestId("panel")).toBeInTheDocument();
  });

  it("closes on ancestor or window scroll outside the panel", () => {
    const onDismiss = vi.fn();
    render(<Harness onDismiss={onDismiss} />);
    fireEvent.scroll(screen.getByTestId("scroller"));
    expect(onDismiss).toHaveBeenCalledTimes(1);
    expect(screen.queryByTestId("panel")).not.toBeInTheDocument();
  });

  it("ignores scroll that originates inside the overlay", () => {
    const onDismiss = vi.fn();
    render(<Harness onDismiss={onDismiss} />);
    fireEvent.scroll(screen.getByTestId("inner-scroll"));
    expect(onDismiss).not.toHaveBeenCalled();
    expect(screen.getByTestId("panel")).toBeInTheDocument();
  });

  it("does not lock ordinary page scrolling", () => {
    render(<Harness onDismiss={() => undefined} />);
    expect(document.documentElement.style.overflow).not.toBe("hidden");
    expect(document.body.style.overflow).not.toBe("hidden");
  });

  it("does not move document focus when dismissing from outside pointer", () => {
    const onDismiss = vi.fn();
    render(<Harness onDismiss={onDismiss} />);
    const outside = screen.getByRole("button", { name: "Outside" });
    outside.focus();
    fireEvent.pointerDown(outside);
    expect(document.activeElement).toBe(outside);
  });
});
