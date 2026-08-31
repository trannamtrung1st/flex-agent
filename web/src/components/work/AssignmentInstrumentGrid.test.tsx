import { render, screen } from "@testing-library/react";
import { ReadoutGridField, ReadoutGridRow } from "../../design-system";
import { AssignmentInstrumentGrid } from "./AssignmentInstrumentGrid";

describe("AssignmentInstrumentGrid", () => {
  it("owns the assignment instrument band on the readout grid", () => {
    render(
      <AssignmentInstrumentGrid label="Setup tracks" columns={4}>
        <ReadoutGridRow label="Local through cohort">
          <ReadoutGridField term="Local">Seated</ReadoutGridField>
        </ReadoutGridRow>
      </AssignmentInstrumentGrid>,
    );

    expect(screen.getByLabelText("Setup tracks")).toHaveClass("readout-grid", "assignment-instruments");
  });

  it("keeps additive className after the instrument band", () => {
    render(
      <AssignmentInstrumentGrid label="Tracks" className="is-frozen">
        <ReadoutGridRow label="Row">
          <ReadoutGridField term="Local">Seated</ReadoutGridField>
        </ReadoutGridRow>
      </AssignmentInstrumentGrid>,
    );

    expect(screen.getByLabelText("Tracks")).toHaveClass("assignment-instruments", "is-frozen");
  });
});
