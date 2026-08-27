import { render, screen } from "@testing-library/react";
import { PlateStatusMark, WorkWell, WorkWellHead, WorkWellSection } from "./WorkWell";

describe("WorkWell", () => {
  it("renders a scroll body and optional pinned foot", () => {
    render(
      <WorkWell head={<WorkWellHead title="Assignment briefing" ident="ENR-7C19-8842" />} foot={<PlateStatusMark>Briefing acknowledged and recorded.</PlateStatusMark>}>
        <WorkWellSection>
          <p>Body copy</p>
        </WorkWellSection>
      </WorkWell>,
    );

    expect(screen.getByRole("article")).toHaveClass("work-well", "composition-stack");
    expect(screen.getByRole("heading", { name: "Assignment briefing" })).toBeInTheDocument();
    expect(document.querySelector(".work-well__body")).toHaveTextContent("Body copy");
    expect(document.querySelector("footer.plate-foot.work-well__foot")).toBeInTheDocument();
    expect(screen.getByRole("status")).toHaveTextContent("Briefing acknowledged and recorded.");
  });

  it("omits the foot landmark when no foot slot is provided", () => {
    render(
      <WorkWell head={<WorkWellHead title="Submission" />}>
        <p>Only body</p>
      </WorkWell>,
    );

    expect(document.querySelector(".work-well__foot")).toBeNull();
    expect(document.querySelector(".work-well__body")).toHaveTextContent("Only body");
  });
});
