import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { FormField } from "./FormField";

describe("FormField", () => {
  it("uses row layout by default", () => {
    const { container } = render(
      <FormField id="demo" label="Callsign">
        {(controlProps) => <input {...controlProps} />}
      </FormField>,
    );
    expect(container.firstElementChild).toHaveClass("form-row");
  });

  it("applies stack layout for narrow plates", () => {
    const { container } = render(
      <FormField id="demo" label="Bounded reason" layout="stack">
        {(controlProps) => <textarea {...controlProps} />}
      </FormField>,
    );
    expect(container.firstElementChild).toHaveClass("field-stack");
  });

  it("merges stack layout with gallery modifier classes", () => {
    const { container } = render(
      <FormField id="demo" label="Adjusted rationale" layout="stack" className="form-demo-stack">
        {(controlProps) => <textarea {...controlProps} />}
      </FormField>,
    );
    expect(container.firstElementChild).toHaveClass("field-stack", "form-demo-stack");
  });

  it("associates validation copy with the control", () => {
    render(
      <FormField id="demo" label="Bounded reason" layout="stack" error="Enter a bounded reason.">
        {(controlProps) => <textarea {...controlProps} />}
      </FormField>,
    );
    const field = screen.getByLabelText("Bounded reason");
    expect(field).toHaveAttribute("aria-invalid", "true");
    expect(field).toHaveAttribute("aria-describedby", "demoError");
    expect(screen.getByText("Enter a bounded reason.")).toHaveAttribute("id", "demoError");
  });
});
