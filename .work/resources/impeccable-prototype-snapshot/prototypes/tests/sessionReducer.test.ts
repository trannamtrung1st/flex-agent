import { describe, expect, it } from "vitest";
import { initialSessionModel, sessionReducer, WARN_AT } from "../src/features/session/sessionReducer";

describe("sessionReducer", () => {
  it("starts in briefing unless a live state param is supplied", () => {
    expect(initialSessionModel(null).briefing).toBe(true);
    expect(initialSessionModel("live").briefing).toBe(false);
    expect(initialSessionModel("complete").complete).toBe(true);
  });

  it("begins the examination after acknowledgment", () => {
    const started = sessionReducer(initialSessionModel("briefing"), { type: "begin" });
    expect(started.briefing).toBe(false);
    expect(started.dismissed).toBe(true);
    expect(started.feed[0].text).toMatch(/EXAMINATION LIVE/);
  });

  it("does not tick while briefing", () => {
    const before = initialSessionModel("briefing");
    const after = sessionReducer(before, { type: "tick" });
    expect(after.remaining).toBe(before.remaining);
  });

  it("warns when remaining time crosses the threshold", () => {
    const live = { ...initialSessionModel("live"), remaining: WARN_AT + 1 };
    const warned = sessionReducer(live, { type: "tick" });
    expect(warned.warned).toBe(true);
    expect(warned.remaining).toBe(WARN_AT);
  });

  it("records a participant turn on transmit and then an agent reply", () => {
    const live = sessionReducer(initialSessionModel("live"), { type: "compose", value: "Trade-offs first." });
    const sent = sessionReducer(live, { type: "transmit" });
    expect(sent.composer).toBe("");
    expect(sent.turns.at(-1)?.speaker).toBe("participant");
    const thinking = sessionReducer(sent, { type: "agent-start" });
    expect(thinking.thinking).toBe(true);
    const done = sessionReducer(thinking, { type: "agent-done" });
    expect(done.thinking).toBe(false);
    expect(done.turns.at(-1)?.speaker).toBe("agent");
  });

  it("seals the session on complete", () => {
    const done = sessionReducer(initialSessionModel("live"), { type: "complete" });
    expect(done.complete).toBe(true);
    expect(done.stage).toBe(5);
    expect(done.confirm).toBe(false);
  });
});
