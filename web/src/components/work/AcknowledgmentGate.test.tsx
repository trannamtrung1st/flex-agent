import { fireEvent, render, screen } from "@testing-library/react";
import { vi } from "vitest";
import { AcknowledgmentGate } from "./AcknowledgmentGate";

describe("AcknowledgmentGate", () => {
  it("defaults to the bordered briefing plate presentation", () => {
    render(
      <AcknowledgmentGate id="ack" checked={false} onChange={() => undefined}>
        Acknowledge
      </AcknowledgmentGate>,
    );

    expect(screen.getByLabelText("Acknowledge").closest("label")).toHaveClass("control-line", "briefing-ack");
  });

  it("supports compact inline presentation for dialog bodies", () => {
    render(
      <AcknowledgmentGate id="ack" presentation="inline" checked={false} onChange={() => undefined}>
        Acknowledge
      </AcknowledgmentGate>,
    );

    const label = screen.getByLabelText("Acknowledge").closest("label");
    expect(label).toHaveClass("control-line");
    expect(label).not.toHaveClass("briefing-ack");
  });

  it("forwards checkbox changes", () => {
    const onChange = vi.fn();
    render(
      <AcknowledgmentGate id="ack" checked={false} onChange={onChange}>
        Acknowledge
      </AcknowledgmentGate>,
    );

    fireEvent.click(screen.getByLabelText("Acknowledge"));
    expect(onChange).toHaveBeenCalledWith(true);
  });
});
