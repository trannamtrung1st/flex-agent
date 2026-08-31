import { render, screen } from "@testing-library/react";
import { ReadoutGrid, ReadoutGridField, ReadoutGridRow } from "./ReadoutGrid";

describe("ReadoutGrid", () => {
  it("emits the column track without a domain band", () => {
    render(
      <ReadoutGrid label="Setup tracks" columns={4}>
        <ReadoutGridRow label="Local through cohort">
          <ReadoutGridField term="Local">Seated</ReadoutGridField>
        </ReadoutGridRow>
      </ReadoutGrid>,
    );

    const grid = screen.getByLabelText("Setup tracks");
    expect(grid).toHaveClass("readout-grid", "readout-grid--columns-4");
    expect(grid).not.toHaveClass("assignment-instruments");
  });

  it("keeps additive className", () => {
    render(
      <ReadoutGrid label="Tracks" className="is-frozen">
        <ReadoutGridRow label="Row">
          <ReadoutGridField term="Local">Seated</ReadoutGridField>
        </ReadoutGridRow>
      </ReadoutGrid>,
    );

    expect(screen.getByLabelText("Tracks")).toHaveClass("readout-grid", "is-frozen");
  });
});
