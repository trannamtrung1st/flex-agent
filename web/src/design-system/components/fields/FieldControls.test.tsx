import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { FieldInput, FieldTextarea } from "./FieldControls";

const fieldsCss = readFileSync(join(dirname(fileURLToPath(import.meta.url)), "../../../styles/components/fields.css"), "utf8");

describe("FieldInput", () => {
  it("renders the example placeholder on an empty slot", () => {
    render(<FieldInput aria-label="Callsign" placeholder="BERTH-04" />);
    expect(screen.getByRole("textbox", { name: "Callsign" })).toHaveAttribute("placeholder", "BERTH-04");
  });

  it("preserves authored case on the default text slot", () => {
    render(<FieldInput aria-label="Campaign title" placeholder="Enter a campaign title" defaultValue="Is it okay?" />);
    const slot = screen.getByRole("textbox", { name: "Campaign title" });
    expect(slot).not.toHaveClass("field-input--uppercase");
    expect(fieldsCss).toMatch(/\.field-input \{[^}]*text-transform:\s*none/);
    expect(fieldsCss).not.toMatch(/\.field-input \{[^}]*text-transform:\s*uppercase/);
  });

  it("opts into uppercase token casing", () => {
    render(<FieldInput aria-label="Callsign" placeholder="BERTH-04" casing="uppercase" />);
    expect(screen.getByRole("textbox", { name: "Callsign" })).toHaveClass("field-input--uppercase");
    expect(fieldsCss).toMatch(/\.field-input--uppercase \{[^}]*text-transform:\s*uppercase/);
  });
});

describe("FieldTextarea", () => {
  it("renders the example placeholder on an empty slot", () => {
    render(<FieldTextarea aria-label="Direct text" placeholder="Write or paste the submission text" />);
    expect(screen.getByRole("textbox", { name: "Direct text" })).toHaveAttribute(
      "placeholder",
      "Write or paste the submission text",
    );
  });
});
