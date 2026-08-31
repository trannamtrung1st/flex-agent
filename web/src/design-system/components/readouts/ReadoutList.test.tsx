import { render, screen } from "@testing-library/react";
import { ReadoutList } from "./ReadoutList";

describe("ReadoutList", () => {
  it("maps row emphasis to horizon readout modifiers", () => {
    render(
      <ReadoutList
        tone="horizon"
        rows={[
          { term: "Purpose", value: "Create drafts.", emphasis: "title" },
          { term: "Record", value: "Released", emphasis: "inline" },
        ]}
      />,
    );

    expect(screen.getByText("Create drafts.").closest(".readout")).toHaveClass(
      "readout--horizon",
      "readout--title",
    );
    expect(screen.getByText("Released").closest(".readout")).toHaveClass(
      "readout--horizon",
      "readout--record",
    );
  });
});
