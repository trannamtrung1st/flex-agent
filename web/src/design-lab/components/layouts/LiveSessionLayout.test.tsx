import { type ReactElement } from "react";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { LayoutAssignment } from "../../../design-system/patterns/layouts/LayoutAssignment";
import { LiveSessionLayout } from "./LiveSessionLayout";

function wrap(ui: ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

describe("LiveSessionLayout", () => {
  it("keeps live-session rail brand outside the scroller", () => {
    wrap(
      <LayoutAssignment id="live-session">
        <LiveSessionLayout
          railLabel="Session instruments"
          brandSuffix="Examination Console"
          brandExtras={<span>Brand extra</span>}
          instruments={<p>Instrument body</p>}
          composer={<p>Composer slot</p>}
          examiner={<p>Examiner slot</p>}
        >
          <p>Transcript slot</p>
        </LiveSessionLayout>
      </LayoutAssignment>,
    );
    const rail = screen.getByRole("complementary", { name: "Session instruments" });
    const scroller = rail.querySelector(".rail-scroll");
    expect(scroller).toBeTruthy();
    expect(rail.textContent).toContain("Examination Console");
    expect(rail.textContent).toContain("Brand extra");
    expect(scroller?.textContent).toContain("Instrument body");
    expect(scroller?.textContent).not.toContain("Examination Console");
    expect(scroller?.textContent).not.toContain("Brand extra");
  });

  it("places live-session rail, transcript, composer, and examiner", () => {
    wrap(
      <LayoutAssignment id="live-session">
        <LiveSessionLayout
          railLabel="Session instruments"
          brandSuffix="Examination Console"
          instruments={<p>Feed</p>}
          composer={<p>Composer slot</p>}
          examiner={<p>Examiner slot</p>}
        >
          <p>Transcript slot</p>
        </LiveSessionLayout>
      </LayoutAssignment>,
    );
    expect(document.querySelector('[data-layout="live-session"]')).toBeTruthy();
    expect(screen.getByRole("complementary", { name: "Session instruments" })).toBeInTheDocument();
    expect(screen.getByRole("main", { name: "Examination transcript" })).toHaveTextContent("Transcript slot");
    expect(screen.getByText("Composer slot")).toBeInTheDocument();
    expect(screen.getByRole("complementary", { name: "Examiner station" })).toHaveTextContent("Examiner slot");
    expect(screen.getByRole("main", { name: "Examination transcript" }).querySelector(".composition-inset")).toBeNull();
    expect(screen.getByRole("link", { name: "Home" })).toHaveAttribute("href", "/surfaces");
  });

  it("omits the composer footer when no composer slot is provided", () => {
    wrap(
      <LayoutAssignment id="live-session">
        <LiveSessionLayout
          railLabel="Session instruments"
          brandSuffix="Examination Console"
          instruments={<p>Feed</p>}
          examiner={<p>Examiner slot</p>}
        >
          <p>Transcript slot</p>
        </LiveSessionLayout>
      </LayoutAssignment>,
    );
    expect(document.querySelector(".layout-session__composer")).toBeNull();
  });
});
