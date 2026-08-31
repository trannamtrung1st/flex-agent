import { render } from "@testing-library/react";
import { FormDemoField } from "./FormDemoField";

describe("FormDemoField", () => {
  it("replaces form-row with the gallery demo host", () => {
    const { container } = render(
      <FormDemoField id="demo" label="Callsign">
        {(controlProps) => <input {...controlProps} />}
      </FormDemoField>,
    );

    expect(container.firstElementChild).toHaveClass("form-demo-row");
    expect(container.firstElementChild).not.toHaveClass("form-row");
  });

  it("adds the fit modifier without restoring form-row", () => {
    const { container } = render(
      <FormDemoField id="demo" label="Harness" fit>
        {(controlProps) => <input {...controlProps} />}
      </FormDemoField>,
    );

    expect(container.firstElementChild).toHaveClass("form-demo-row", "form-demo-row--fit");
    expect(container.firstElementChild).not.toHaveClass("form-row");
  });
});
