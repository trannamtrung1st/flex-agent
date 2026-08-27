import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { DESIGN_LAB_BASENAME, designLabRoutes } from "./app/router";

function renderLab(url: string) {
  const router = createMemoryRouter(designLabRoutes, {
    basename: DESIGN_LAB_BASENAME,
    initialEntries: [url],
  });
  return render(<RouterProvider router={router} />);
}

describe("PC-03 participant disclosure", () => {
  it("shows Result not available instead of pending-release workflow copy on Home", () => {
    renderLab("/design-lab/participant-home");
    expect(screen.getByRole("heading", { name: "Result not available" })).toBeInTheDocument();
    expect(screen.queryByText(/Pending Release/i)).not.toBeInTheDocument();
    expect(screen.getAllByText("Result not available").length).toBeGreaterThan(1);
  });

  it("keeps unpublished Journey results at Result not available", () => {
    renderLab("/design-lab/participant-journey?demo=result-pending");
    expect(screen.getByRole("heading", { name: "Result not available" })).toBeInTheDocument();
    expect(screen.queryByText(/Pending release/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Evaluation under human review/i)).not.toBeInTheDocument();
    expect(screen.getByText(/Return to your activity/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Return" })).toBeInTheDocument();
  });
});

describe("PC-07 assignment lifecycle", () => {
  it("does not let Mark Submission Complete unlock examination", () => {
    renderLab("/design-lab/participant-journey?demo=submission");
    expect(screen.queryByRole("button", { name: /Mark Submission Complete/i })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: /Submit version/i }));
    expect(screen.getByRole("heading", { name: "Submission" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: /Examination ready/i })).not.toBeInTheDocument();
  });
});

describe("PC-01 and PC-02 reviewer decision", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("separates approval from release and blocks release on escalated records", async () => {
    vi.useFakeTimers();
    renderLab("/design-lab/reviewer-console");
    fireEvent.click(screen.getByRole("button", { name: "CND-8842-19" }));
    await act(async () => {
      await vi.advanceTimersByTimeAsync(700);
    });
    expect(screen.queryByRole("button", { name: /Approve & Release/i })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Approve" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Queue" })).toBeInTheDocument();
    expect(screen.getByText(/Release is a separate authorized flow/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Approve" }));
    expect(screen.getByText(/Result-ready handoff/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Release Result" })).not.toBeInTheDocument();
  });

  it("requires a bounded reason before reject and does not treat save adjustment as a submitted revision", async () => {
    vi.useFakeTimers();
    renderLab("/design-lab/reviewer-console");
    fireEvent.click(screen.getByRole("button", { name: "CND-8842-19" }));
    await act(async () => {
      await vi.advanceTimersByTimeAsync(700);
    });
    fireEvent.click(screen.getByRole("button", { name: "Reject" }));
    expect(screen.getByRole("dialog", { name: /Reject/i })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Confirm reject" }));
    expect(screen.getByText("A bounded reason is required.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
    fireEvent.click(screen.getByRole("button", { name: "Adjust" }));
    fireEvent.click(screen.getByRole("button", { name: "Save adjustment" }));
    expect(screen.getByText(/not submitted/i)).toBeInTheDocument();
  });
});

describe("PC-05 and PC-06 campaign setup", () => {
  it("does not activate from a single submit and offers draft plus readiness", async () => {
    renderLab("/design-lab/admin-console/campaigns?campaign=CMP-0044");
    fireEvent.click(screen.getByRole("button", { name: "Configure campaign" }));
    const dialog = screen.getByRole("dialog", { name: /Campaign Configuration/i });
    expect(within(dialog).getByRole("button", { name: "Save draft" })).toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "Check readiness" })).toBeInTheDocument();
    expect(within(dialog).queryByRole("button", { name: /^Activate$/ })).not.toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: /Confirm activation/ })).toBeDisabled();
    fireEvent.click(within(dialog).getByRole("button", { name: "Check readiness" }));
    await waitFor(() => {
      expect(within(dialog).getByRole("button", { name: /Confirm activation/ })).toBeEnabled();
    });
    fireEvent.click(within(dialog).getByRole("button", { name: /Confirm activation/ }));
    expect(within(dialog).getByRole("button", { name: "Activate campaign" })).toBeInTheDocument();
    expect(within(dialog).queryByRole("button", { name: "Save draft" })).not.toBeInTheDocument();
  });

  it("shows a non-disclosing unavailable state for an invalid campaign id", () => {
    renderLab("/design-lab/admin-console/campaigns?campaign=CMP-NOPE");
    expect(screen.getByText("Campaign not found")).toBeInTheDocument();
    expect(screen.queryByText(/CMP-NOPE/)).not.toBeInTheDocument();
    expect(screen.queryByText(/CMP-0042/)).not.toBeInTheDocument();
  });

  it("keeps enrollments and other operational areas unavailable without echoing an invalid id", () => {
    renderLab("/design-lab/admin-console/enrollments?campaign=CMP-NOPE");
    expect(screen.getByText("Campaign not available")).toBeInTheDocument();
    expect(screen.queryByText(/CMP-NOPE/)).not.toBeInTheDocument();
  });
});

describe("PC-08 session specimen", () => {
  it("labels the timer as synthetic and avoids listening copy", () => {
    renderLab("/design-lab/participant-session?state=live");
    expect(screen.getByText(/Synthetic demonstration timer/i)).toBeInTheDocument();
    expect(screen.queryByText(/I’m listening/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/I'm listening/i)).not.toBeInTheDocument();
  });
});

describe("PC-09 future references", () => {
  it("labels Users & Access as a future design-lab reference", () => {
    renderLab("/design-lab/admin-console/users-access");
    expect(screen.getByRole("heading", { name: "Users & Access" })).toBeInTheDocument();
    expect(screen.getByText(/Future \/ not in current MVP/i)).toBeInTheDocument();
  });
});

describe("PC-11 named timezone", () => {
  it("shows the Campaign timezone on Home deadlines", () => {
    renderLab("/design-lab/participant-home");
    expect(screen.getByText(/America\/Chicago/)).toBeInTheDocument();
  });
});

describe("gallery primitive specimens", () => {
  it("covers disabled, empty, and dialog states with accessible names", () => {
    renderLab("/design-lab/shared/gallery");
    expect(screen.getByRole("heading", { name: "Shared component deck" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Disabled" })).toBeDisabled();
    expect(screen.getByRole("button", { name: /Disabled with reason/ })).toBeDisabled();
    expect(screen.getByText("No assigned sessions")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Open confirm dialog" }));
    expect(screen.getByRole("dialog", { name: "Confirm Release" })).toBeInTheDocument();
  });
});
