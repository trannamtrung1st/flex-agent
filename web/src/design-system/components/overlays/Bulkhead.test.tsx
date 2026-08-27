import { useState } from "react";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { Bulkhead } from "./Bulkhead";
import { Key } from "../keys/Key";

function Harness() {
  const [open, setOpen] = useState(false);
  return (
    <>
      <Key onClick={() => setOpen(true)}>Open drawer</Key>
      <Bulkhead open={open} onClose={() => setOpen(false)} title="Administrator" titleId="bulkhead-title">
        <a href="/">Home</a>
      </Bulkhead>
    </>
  );
}

describe("Bulkhead", () => {
  it("moves keyboard focus to Close, not the full-screen scrim", async () => {
    render(<Harness />);

    fireEvent.click(screen.getByRole("button", { name: "Open drawer" }));

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Close" })).toHaveFocus();
    });
    expect(screen.getByRole("button", { name: "Close Administrator" })).not.toHaveFocus();
  });
});
