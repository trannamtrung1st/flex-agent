import { useState } from "react";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { BULKHEAD_INERT_SELECTOR, Bulkhead } from "./Bulkhead";
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
  it("inerts hull chrome and generic layout hosts, not reviewer surface classes", () => {
    expect(BULKHEAD_INERT_SELECTOR).toContain(".command-strip");
    expect(BULKHEAD_INERT_SELECTOR).toContain(".console-foot");
    expect(BULKHEAD_INERT_SELECTOR).toContain(".layout-management__shell");
    expect(BULKHEAD_INERT_SELECTOR).toContain(".composition-split");
    expect(BULKHEAD_INERT_SELECTOR).not.toMatch(/queue-view|record-view|record-grid/);
  });

  it("moves keyboard focus to Close, not the full-screen scrim", async () => {
    render(<Harness />);

    fireEvent.click(screen.getByRole("button", { name: "Open drawer" }));

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Close" })).toHaveFocus();
    });
    expect(screen.getByRole("button", { name: "Close Administrator" })).not.toHaveFocus();
  });

  it("reserves a stable scrollbar gutter and token inset on the overlay body", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const css = readFileSync(join(here, "../../../styles/components/navigation.css"), "utf8");
    const body = css.match(/\.bulkhead-body \{[^}]+\}/)?.[0] ?? "";
    expect(body).toMatch(/scrollbar-gutter:\s*stable/);
    expect(body).toMatch(/scrollbar-width:\s*auto/);
    expect(body).toMatch(/padding:\s*var\(--plate-foot-pad-block\)\s+var\(--frame-inset-inline\)/);
    expect(body).not.toMatch(/14px 20px 18px/);
  });
});
