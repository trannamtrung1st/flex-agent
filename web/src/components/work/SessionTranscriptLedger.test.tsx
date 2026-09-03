import { render, screen } from "@testing-library/react";
import { SessionTranscriptLedger } from "./SessionTranscriptLedger";

it("renders conversation channel plates for historical items", () => {
  render(
    <SessionTranscriptLedger
      label="Historical transcript"
      items={[
        {
          item_id: "msg.participant1",
          author: "participant",
          status: "accepted",
          content: "Hello examiner",
          sequence_start: "1",
          sequence_end: "1",
        },
        {
          item_id: "msg.agent1",
          author: "agent",
          status: "complete",
          content: "Acknowledged.",
          sequence_start: "2",
          sequence_end: "2",
        },
      ]}
    />,
  );

  const ledger = screen.getByLabelText("Historical transcript");
  expect(ledger).toHaveClass("ledger");
  expect(ledger.querySelector(".turn.turn--participant")).toHaveTextContent("Participant");
  expect(ledger.querySelector(".turn.turn--agent")).toHaveTextContent("Agent");
  expect(screen.getByText("Hello examiner")).toBeVisible();
});
