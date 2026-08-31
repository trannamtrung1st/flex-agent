import { render } from "@testing-library/react";
import { recordResultMark } from "./recordResultMark";

describe("recordResultMark", () => {
  it("maps enrollment result vocabulary to shared state marks", () => {
    const { container: live } = render(recordResultMark("LIVE"));
    expect(live.querySelector(".state-ring")).toBeTruthy();

    const { container: progress } = render(recordResultMark("IN PROGRESS"));
    expect(progress.querySelector(".state-node--live-solid")).toBeTruthy();

    const { container: rest } = render(recordResultMark("QUEUED"));
    expect(rest.querySelector(".state-node")).toBeTruthy();
    expect(rest.querySelector(".state-ring")).toBeNull();
  });
});
