import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { BackKey } from "./keys";
import {
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
} from "./overlays";
import { Breaker, FieldInput, FieldTextarea, FormField, RadioGroup } from "./fields";
import { ReadoutList } from "./readouts";
import { StateIndicator, StateReadout } from "./state";
import { ActivationMark } from "./state/ActivationMark";

describe("StateIndicator", () => {
  it("renders decorative visual variants and consumer-owned labels", () => {
    const { container } = render(
      <StateReadout variant="sealed" solid label="Frozen" className="state-cell" />,
    );

    expect(container.querySelector(".state-node--sealed-solid")).toHaveAttribute("aria-hidden", "true");
    expect(screen.getByText("Frozen")).toBeVisible();
  });

  it("keeps the rest variant on the base visual class", () => {
    const { container } = render(<StateIndicator />);
    expect(container.firstChild).toHaveClass("state-node");
    expect(container.firstChild).not.toHaveClass("state-node--dim");
  });
});

describe("ActivationMark", () => {
  it("uses the full sentence in context and compact labels in tables", () => {
    const { rerender } = render(<ActivationMark frozen={false} />);
    expect(screen.getByText("Draft — not activated")).toBeVisible();
    rerender(<ActivationMark frozen compact />);
    expect(screen.getByText("Frozen")).toBeVisible();
  });
});

describe("BackKey", () => {
  it("uses the back key class, chevron geometry, and a visible label", () => {
    const { container } = render(<BackKey label="Campaigns" />);
    expect(screen.getByRole("button", { name: "Campaigns" })).toHaveClass("key--back");
    expect(container.querySelector("svg")).toHaveAttribute("aria-hidden", "true");
  });
});

describe("FormField", () => {
  it("wires its label and validation description without owning state", () => {
    render(
      <FormField id="score" label="Adjusted score" hint="Cohort-scoped." error="Enter a score">
        {(controlProps) => <input {...controlProps} />}
      </FormField>,
    );

    const input = screen.getByRole("textbox", { name: "Adjusted score" });
    expect(input).toHaveAttribute("id", "score");
    expect(input).toHaveAttribute("aria-invalid", "true");
    expect(input).toHaveAttribute("aria-describedby", "scoreHint scoreError");
    expect(screen.getByText("Cohort-scoped.")).toHaveAttribute("id", "scoreHint");
    expect(screen.getByText("Enter a score")).toHaveAttribute("id", "scoreError");
  });
});

describe("FieldInput", () => {
  it("owns invalid and frozen classes from FormField props", () => {
    const { rerender } = render(
      <FormField id="limit" label="Session limit" error="Enter a value">
        {(controlProps) => <FieldInput {...controlProps} />}
      </FormField>,
    );
    expect(screen.getByRole("textbox", { name: "Session limit" })).toHaveClass("field-input", "is-invalid");

    rerender(
      <FormField id="limit" label="Session limit">
        {(controlProps) => <FieldInput {...controlProps} frozen defaultValue="60:00" />}
      </FormField>,
    );
    const frozen = screen.getByRole("textbox", { name: "Session limit" });
    expect(frozen).toHaveClass("field-input", "is-frozen");
    expect(frozen).toHaveAttribute("readOnly");
  });
});

describe("FieldTextarea", () => {
  it("locks resize by default and marks the vertical grow variant", () => {
    const { rerender } = render(
      <FormField id="rationale" label="Adjusted rationale">
        {(controlProps) => <FieldTextarea {...controlProps} />}
      </FormField>,
    );
    const locked = screen.getByRole("textbox", { name: "Adjusted rationale" });
    expect(locked).toHaveClass("field-textarea");
    expect(locked).not.toHaveClass("field-textarea--resize-y");

    rerender(
      <FormField id="rationale" label="Adjusted rationale">
        {(controlProps) => <FieldTextarea {...controlProps} resize="vertical" />}
      </FormField>,
    );
    expect(screen.getByRole("textbox", { name: "Adjusted rationale" })).toHaveClass("field-textarea--resize-y");
  });
});

describe("RadioGroup and Breaker", () => {
  it("keeps one selected radio and exposes a switch", () => {
    render(
      <>
        <RadioGroup
          legend="Agent identity"
          name="agent"
          value="EXAMINER-CORE"
          onChange={() => undefined}
          options={[
            { value: "EXAMINER-CORE", label: "Examiner-Core" },
            { value: "EXAMINER-OPS", label: "Examiner-Ops" },
          ]}
        />
        <Breaker checked onChange={() => undefined}>Time warnings</Breaker>
      </>,
    );

    const radios = screen.getAllByRole("radio");
    expect(radios).toHaveLength(2);
    expect(radios[0]).toBeChecked();
    expect(radios[1]).not.toBeChecked();
    expect(radios[0]).toHaveAttribute("name", "agent");
    expect(screen.getByRole("switch", { name: "Time warnings" })).toBeChecked();
  });
});

describe("DialogPlate", () => {
  it("composes typed narrow head, body, and footer regions", () => {
    const { container } = render(
      <DialogPlate width="narrow">
        <DialogPlateHead title="Confirm" titleId="confirmTitle" />
        <DialogPlateBody>Body copy</DialogPlateBody>
        <DialogPlateFooter>Actions</DialogPlateFooter>
      </DialogPlate>,
    );

    expect(container.firstChild).toHaveClass("dialog-plate", "dialog-plate--narrow");
    expect(screen.getByRole("heading", { name: "Confirm" })).toHaveAttribute("id", "confirmTitle");
    expect(container.querySelector(".dialog-body")).toHaveTextContent("Body copy");
    expect(container.querySelector(".dialog-foot")).toHaveTextContent("Actions");
  });
});

describe("ReadoutList", () => {
  it("renders compact semantic description rows", () => {
    render(
      <ReadoutList
        label="Session instruments"
        rows={[
          { term: "Session ID", value: "FXA-7C19-2A07" },
          { term: "Link", value: "Nominal" },
        ]}
      />,
    );

    const list = screen.getByText("Session ID").closest("dl");
    expect(list).toHaveClass("readout-stack");
    expect(within(list as HTMLElement).getAllByRole("term")).toHaveLength(2);
    expect(within(list as HTMLElement).getByText("Nominal")).toBeVisible();
  });
});
