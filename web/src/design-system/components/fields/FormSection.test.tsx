import { render, screen } from "@testing-library/react";
import { FormSection } from "./FormSection";

describe("FormSection", () => {
  it("is a labelled fieldset with the form-section recipe", () => {
    render(
      <FormSection legend="Agent and Harness">
        <button type="button">Agent</button>
      </FormSection>,
    );

    const section = screen.getByRole("group", { name: "Agent and Harness" });
    expect(section.tagName).toBe("FIELDSET");
    expect(section).toHaveClass("form-section");
    expect(section.querySelector(":scope > legend")).toHaveTextContent("Agent and Harness");
    expect(screen.getByRole("button", { name: "Agent" })).toBeInTheDocument();
  });
});
