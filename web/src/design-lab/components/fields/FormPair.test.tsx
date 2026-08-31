import { render } from "@testing-library/react";
import { FormPair, FormPairField } from "./FormPair";

describe("FormPair", () => {
  it("emits the paired field row grammar", () => {
    const { container } = render(
      <FormPair>
        <span>One</span>
        <span>Two</span>
      </FormPair>,
    );
    expect(container.firstElementChild).toHaveClass("form-row", "form-row--pair");
  });

  it("owns field-pair on each nested field", () => {
    const { container } = render(
      <FormPair>
        <FormPairField id="limit" label="Session limit">
          {(control) => <input {...control} />}
        </FormPairField>
      </FormPair>,
    );
    const field = container.querySelector("#limit")?.parentElement;
    expect(field).toHaveClass("field-pair");
    expect(field).not.toHaveClass("form-row");
  });
});
