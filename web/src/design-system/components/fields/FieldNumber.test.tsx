import { fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import { FieldNumber } from "./FieldNumber";

function ControlledScore() {
  const [value, setValue] = useState("3");
  return (
    <label>
      Score
      <FieldNumber
        value={value}
        min={0}
        max={4}
        step={1}
        onChange={(event) => setValue(event.target.value)}
      />
    </label>
  );
}

describe("FieldNumber", () => {
  it("exposes a number input with increase and decrease keys", () => {
    render(
      <label>
        Score
        <FieldNumber defaultValue={3} min={0} max={4} />
      </label>,
    );

    const input = screen.getByRole("spinbutton", { name: "Score" });
    expect(input).toHaveAttribute("type", "number");
    expect(screen.getByRole("button", { name: "Increase" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Decrease" })).toBeInTheDocument();
  });

  it("steps the value with the authored keys and clamps at max", () => {
    render(<ControlledScore />);

    const input = screen.getByRole("spinbutton", { name: "Score" });
    fireEvent.click(screen.getByRole("button", { name: "Increase" }));
    expect(input).toHaveValue(4);
    fireEvent.click(screen.getByRole("button", { name: "Increase" }));
    expect(input).toHaveValue(4);
    fireEvent.click(screen.getByRole("button", { name: "Decrease" }));
    expect(input).toHaveValue(3);
  });

  it("steps an uncontrolled value and names stepper keys from stepperLabel", () => {
    render(
      <label>
        Score
        <FieldNumber defaultValue={2} min={0} max={4} stepperLabel="score" />
      </label>,
    );

    const input = screen.getByRole("spinbutton", { name: "Score" });
    fireEvent.click(screen.getByRole("button", { name: "Increase score" }));
    expect(input).toHaveValue(3);
    expect(screen.getByRole("button", { name: "Decrease score" })).toBeInTheDocument();
  });

  it("withdraws stepper keys when frozen", () => {
    render(
      <label>
        Score
        <FieldNumber defaultValue={3} frozen />
      </label>,
    );

    expect(screen.getByRole("spinbutton", { name: "Score" })).toHaveAttribute("readOnly");
    expect(screen.queryByRole("button", { name: "Increase" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Decrease" })).not.toBeInTheDocument();
  });
});
