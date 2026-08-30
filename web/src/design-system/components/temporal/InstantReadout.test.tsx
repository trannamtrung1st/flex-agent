import { render, screen } from "@testing-library/react";
import { InstantReadout } from "./InstantReadout";

describe("InstantReadout", () => {
  it("renders a time element for a readable UTC instant", () => {
    render(<InstantReadout value="2026-08-25T19:42:00.000Z" timeZone="America/Chicago" />);
    const time = screen.getByText(/25 Aug/i);
    expect(time.tagName).toBe("TIME");
    expect(time).toHaveAttribute("datetime", "2026-08-25T19:42:00.000Z");
    expect(time).toHaveAttribute("title");
    expect(time.getAttribute("title")).not.toMatch(/undefined/i);
  });

  it("renders the shared absence mark instead of the word undefined", () => {
    render(<InstantReadout value={undefined} timeZone="Asia/Saigon" />);
    expect(screen.getByText("Not recorded")).toHaveClass("visually-hidden");
    expect(screen.getByText("—")).toHaveAttribute("aria-hidden", "true");
    expect(screen.queryByText(/undefined/i)).not.toBeInTheDocument();
    expect(document.querySelector("time")).toBeNull();
  });

  it("accepts a Date the same way as an ISO string", () => {
    render(<InstantReadout value={new Date("2026-08-25T19:42:00.000Z")} timeZone="America/Chicago" />);
    const time = screen.getByText(/25 Aug/i);
    expect(time.tagName).toBe("TIME");
    expect(time).toHaveAttribute("datetime", "2026-08-25T19:42:00.000Z");
  });
});
