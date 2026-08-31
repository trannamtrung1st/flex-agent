import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
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

  it("does not treat form-demo-row as a layout replacement", () => {
    const { container } = render(
      <FormField id="demo" label="Callsign" className="form-demo-row">
        {(controlProps) => <input {...controlProps} />}
      </FormField>,
    );
    expect(container.firstElementChild).toHaveClass("form-row", "form-demo-row");
  });

  it("lets lab specimens replace the layout host", () => {
    const { container } = render(
      <FormField id="demo" label="Callsign" hostClassName="form-demo-row">
        {(controlProps) => <input {...controlProps} />}
      </FormField>,
    );
    expect(container.firstElementChild).toHaveClass("form-demo-row");
    expect(container.firstElementChild).not.toHaveClass("form-row");
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

  it("does not own pair layout on the generic field", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const source = readFileSync(join(here, "FormField.tsx"), "utf8");
    expect(source).not.toMatch(/"pair"/);
    expect(source).not.toContain("field-pair");
  });

  it("treats hints as sentence-case helpers, not microlabels", () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const fieldsCss = readFileSync(join(here, "../../../styles/components/fields.css"), "utf8");
    expect(fieldsCss).toMatch(/\.field-hint \{[^}]*text-transform:\s*none/);
    expect(fieldsCss).not.toMatch(/\.field-hint \{[^}]*text-transform:\s*uppercase/);
  });
});
