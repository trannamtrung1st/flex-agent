import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { fireEvent, render, screen } from "@testing-library/react";
import { vi } from "vitest";
import { SelectPanelFoot } from "./SelectPanelFoot";

const searchableCss = readFileSync(
  join(dirname(fileURLToPath(import.meta.url)), "../../../styles/components/searchable.css"),
  "utf8",
);

describe("SelectPanelFoot", () => {
  it("trails the done action when there is no leading control", () => {
    const { container } = render(<SelectPanelFoot doneLabel="Close" onDone={() => undefined} />);
    expect(container.firstChild).toHaveClass("multiselect-foot", "multiselect-foot--trailing");
    expect(screen.getByRole("button", { name: "Close" })).toBeInTheDocument();
  });

  it("spaces leading and done actions when a leading control is present", () => {
    const { container } = render(
      <SelectPanelFoot
        leading={<button type="button">Clear</button>}
        onDone={() => undefined}
      />,
    );
    expect(container.firstChild).toHaveClass("multiselect-foot");
    expect(container.firstChild).not.toHaveClass("multiselect-foot--trailing");
  });

  it("calls onDone when the trailing action is activated", () => {
    const onDone = vi.fn();
    render(<SelectPanelFoot doneLabel="Done" onDone={onDone} />);
    fireEvent.click(screen.getByRole("button", { name: "Done" }));
    expect(onDone).toHaveBeenCalledTimes(1);
  });

  it("keeps searchable panel foot separators on the list edge", () => {
    expect(searchableCss).toMatch(
      /\.multiselect-options,\s*\n\.searchable-select-options,\s*\n\.searchable-disclosure-options \{[^}]*border-bottom:\s*1px solid var\(--hairline-dim\)/,
    );
    expect(searchableCss).toMatch(
      /\.searchable-select-panel \.multiselect-foot,\s*\n\.searchable-disclosure-panel \.multiselect-foot \{[^}]*border-top:\s*none/,
    );
    expect(searchableCss).toMatch(/\.multiselect-foot \{[^}]*border-top:\s*1px solid var\(--hairline-dim\)/);
  });

  it("does not punch the outer overlay bezel when a select panel has a foot", () => {
    const overlays = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../../../styles/components/overlays.css"),
      "utf8",
    );
    expect(overlays).not.toMatch(/:has\(\.multiselect-foot, \.select-popover-foot\)/);
    expect(searchableCss).toMatch(/\.multiselect-foot \{[^}]*border-top:\s*1px solid var\(--hairline-dim\)/);
  });
});
