import { render, screen } from "@testing-library/react";
import { Grid, Stack } from "../layout";
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

  it("stacks sibling clusters in a composition stack without a form-divider", () => {
    render(
      <Stack gap="6">
        <FormSection legend="Agent and Harness">
          <button type="button">Agent</button>
        </FormSection>
        <FormSection legend="Source set">
          <button type="button">Task</button>
        </FormSection>
      </Stack>,
    );

    const first = screen.getByRole("group", { name: "Agent and Harness" });
    const second = screen.getByRole("group", { name: "Source set" });
    expect(first.parentElement).toHaveClass("composition-stack");
    expect(first.parentElement).toHaveAttribute("data-flow-gap", "6");
    expect(first.nextElementSibling).toBe(second);
    expect(first.parentElement?.querySelector(".form-divider")).toBeNull();
  });

  it("places side-by-side clusters in a composition grid without a form-divider", () => {
    render(
      <Grid gap="6" minItemWidth="panel">
        <FormSection legend="Timing and attempts">
          <button type="button">Limit</button>
        </FormSection>
        <FormSection legend="Window">
          <button type="button">Opened</button>
        </FormSection>
      </Grid>,
    );

    const first = screen.getByRole("group", { name: "Timing and attempts" });
    const second = screen.getByRole("group", { name: "Window" });
    expect(first.parentElement).toHaveClass("composition-grid");
    expect(first.parentElement).toHaveAttribute("data-flow-gap", "6");
    expect(first.nextElementSibling).toBe(second);
    expect(first.parentElement?.querySelector(".form-divider")).toBeNull();
  });
});
