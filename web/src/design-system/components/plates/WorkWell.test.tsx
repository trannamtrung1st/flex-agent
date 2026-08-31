import { render, screen } from "@testing-library/react";
import { PlateStatusMark, WorkWell, WorkWellHead, WorkWellHint, WorkWellSection } from "./WorkWell";

describe("WorkWell", () => {
  it("renders a body and optional pinned foot", () => {
    render(
      <WorkWell head={<WorkWellHead title="Assignment briefing" ident="ENR-7C19-8842" />} foot={<PlateStatusMark>Briefing acknowledged and recorded.</PlateStatusMark>}>
        <WorkWellSection>
          <p>Body copy</p>
        </WorkWellSection>
      </WorkWell>,
    );

    expect(screen.getByRole("article")).toHaveClass("work-well", "composition-stack");
    expect(screen.getByRole("heading", { name: "Assignment briefing" })).toBeInTheDocument();
    expect(document.querySelector(".work-well__head")).toHaveAttribute("data-flow-gap", "none");
    expect(document.querySelector(".work-well__copy")).toHaveAttribute("data-flow-gap", "2.5");
    expect(document.querySelector(".work-well")).toHaveAttribute("data-seat", "pane");
    expect(document.querySelector(".work-well__head")).toHaveAttribute("data-mark", "span");
    expect(document.querySelector(".work-well__head")).toHaveAttribute("data-title-role", "task");
    expect(screen.getByRole("heading", { name: "Assignment briefing" })).toHaveAttribute("data-title-role", "task");
    expect(document.querySelector(".work-well")).toHaveAttribute("data-inset", "frame");
    expect(document.querySelector(".work-well__body")).toHaveTextContent("Body copy");
    const foot = document.querySelector("footer.plate-foot.work-well__foot");
    expect(foot).toBeInTheDocument();
    expect(foot).toHaveAttribute("data-arrangement", "start");
    expect(foot).toHaveAttribute("data-flow-justify", "start");
    expect(foot).not.toHaveClass("plate-foot--start");
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

  it("seats stacked records as flush title-marked wells without per-head mark props", () => {
    render(
      <WorkWell
        seat="stack"
        live={false}
        label="Enrollment actions"
        head={<WorkWellHead title="Enrollment actions" ident="Lifecycle stays on the server." />}
      >
        <p>Body</p>
      </WorkWell>,
    );

    const well = document.querySelector(".work-well");
    const head = document.querySelector(".work-well__head");
    expect(well).toHaveAttribute("data-seat", "stack");
    expect(well).toHaveAttribute("data-inset", "flush");
    expect(head).toHaveAttribute("data-mark", "title");
    expect(head).toHaveAttribute("data-title-role", "plate");
    expect(screen.getByRole("heading", { name: "Enrollment actions" })).toHaveAttribute("data-title-role", "plate");
    expect(document.querySelector(".work-well__copy")).toHaveAttribute("data-flow-align", "start");
    expect(head?.querySelector(".work-well__copy")).toHaveTextContent("Enrollment actions");
    expect(head?.querySelector(".work-well__copy")).toHaveTextContent("Lifecycle stays on the server.");
  });

  it("keeps an explicit head mark when it disagrees with seat", () => {
    render(
      <WorkWell seat="stack" head={<WorkWellHead mark="span" title="Assignment briefing" />}>
        <p>Body</p>
      </WorkWell>,
    );

    expect(document.querySelector(".work-well")).toHaveAttribute("data-inset", "flush");
    expect(document.querySelector(".work-well__head")).toHaveAttribute("data-mark", "span");
  });

  it("keeps an explicit title role when it disagrees with seat", () => {
    render(
      <WorkWell seat="stack" head={<WorkWellHead titleRole="task" title="Assignment briefing" />}>
        <p>Body</p>
      </WorkWell>,
    );

    expect(screen.getByRole("heading", { name: "Assignment briefing" })).toHaveAttribute("data-title-role", "task");
    expect(document.querySelector(".work-well__head")).toHaveAttribute("data-title-role", "task");
  });

  it("renders a released seal ahead of title copy", () => {
    render(
      <WorkWell head={<WorkWellHead seal={<span className="work-well__seal" />} title="Result released" ident="Synthetic specimen" />}>
        <p>Body</p>
      </WorkWell>,
    );

    expect(document.querySelector(".work-well__seal")).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Result released" })).toHaveClass("work-well__title");
    expect(screen.getByText("Synthetic specimen")).toHaveClass("work-well__ident");
    expect(document.querySelector(".work-well__copy")).toHaveAttribute("data-flow-gap", "2");
  });

  it("renders well hints in the body grammar", () => {
    render(
      <WorkWell head={<WorkWellHead title="Submission" />}>
        <WorkWellHint>Attempt readiness is server-authoritative.</WorkWellHint>
      </WorkWell>,
    );

    expect(screen.getByText("Attempt readiness is server-authoritative.")).toHaveClass("work-well__hint");
  });

  it("applies titleRole to custom head titles through the header", () => {
    render(
      <WorkWell
        seat="pane"
        head={
          <WorkWellHead gap="none">
            <h2 className="work-well__title">Result released</h2>
          </WorkWellHead>
        }
      >
        <p>Body</p>
      </WorkWell>,
    );

    expect(document.querySelector(".work-well__head")).toHaveAttribute("data-title-role", "task");
    expect(screen.getByRole("heading", { name: "Result released" })).not.toHaveAttribute("data-title-role");
  });
});
