import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { FieldInput, FieldTextarea } from "./FieldControls";
import { CALLSIGN_PLACEHOLDER, DIRECT_TEXT_PLACEHOLDER } from "./fieldFormat";

describe("FieldInput", () => {
  it("renders the example placeholder on an empty slot", () => {
    render(<FieldInput aria-label="Callsign" placeholder={CALLSIGN_PLACEHOLDER} />);
    expect(screen.getByRole("textbox", { name: "Callsign" })).toHaveAttribute(
      "placeholder",
      CALLSIGN_PLACEHOLDER,
    );
  });
});

describe("FieldTextarea", () => {
  it("renders the example placeholder on an empty slot", () => {
    render(<FieldTextarea aria-label="Direct text" placeholder={DIRECT_TEXT_PLACEHOLDER} />);
    expect(screen.getByRole("textbox", { name: "Direct text" })).toHaveAttribute(
      "placeholder",
      DIRECT_TEXT_PLACEHOLDER,
    );
  });
});
