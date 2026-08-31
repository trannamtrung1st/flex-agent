import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
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
  it("seats lab journey actions in GuidedTaskFoot without an in-plate hairline", () => {
    renderLab("/design-lab/participant-journey?demo=briefing");
    const foot = document.querySelector(".layout-guided__actions");
    expect(foot?.tagName).toBe("FOOTER");
    expect(foot).toHaveAttribute("data-hairline", "false");
    expect(document.querySelector(".action-keys")).toBeNull();
  });

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
    const rejectDialog = screen.getByRole("dialog", { name: /Reject/i });
    expect(rejectDialog).toBeInTheDocument();
    expect(rejectDialog.querySelector(".dialog-plate--wide")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Confirm reject" }));
    expect(screen.getByText("Enter a bounded reason.")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Bounded reason"), { target: { value: "a a" } });
    fireEvent.click(screen.getByRole("button", { name: "Confirm reject" }));
    expect(screen.getByText("Enter at least 8 characters.")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Bounded reason"), { target: { value: "policy mismatch on criterion 2" } });
    expect(screen.queryByText("Enter at least 8 characters.")).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
    fireEvent.click(screen.getByRole("button", { name: "Adjust" }));
    fireEvent.click(screen.getByRole("button", { name: "Save adjustment" }));
    expect(screen.getByText(/not submitted/i)).toBeInTheDocument();
  });

  it("gives assignment a title floor and seats Action with StaticHeader", () => {
    renderLab("/design-lab/reviewer-console");
    const table = screen.getByRole("table", { name: "Sessions awaiting human review" });
    expect(within(table).getByRole("columnheader", { name: "Participant" })).toHaveAttribute("data-col-min", "compactId");
    const assignmentHead = within(table).getByRole("columnheader", { name: "Assignment" });
    expect(assignmentHead).toHaveAttribute("data-col-min", "title");
    expect(assignmentHead).toHaveClass("col-assignment");
    expect(within(table).getByRole("cell", { name: /Real-time Inventory/ })).toHaveAttribute("data-col-min", "title");
    const actionHead = within(table).getByRole("columnheader", { name: "Action" });
    expect(actionHead).toHaveAttribute("data-col-min", "action");
    expect(actionHead).toHaveClass("col-action");
    expect(actionHead.querySelector(".col-head")).toHaveTextContent("Action");
    expect(actionHead.querySelector(".visually-hidden")).toBeNull();
    expect(within(table).getByRole("button", { name: "Inspect" }).closest("td")).toHaveAttribute(
      "data-label",
      "Action",
    );
  });

  it("does not cap assignment at 28ch and gives leftover width to that column", () => {
    const css = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../styles/surfaces/reviewer-console.css"),
      "utf8",
    );
    expect(css).not.toMatch(/\.manifest \.col-assignment \{[^}]*max-width:\s*28ch/);
    expect(css).toMatch(/\.queue-datatable \.datatable-table thead th:not\(\.col-select\):first-child \{[^}]*width:\s*1%/);
    expect(css).toMatch(/\.queue-datatable \.datatable-table thead th\.col-assignment \{[^}]*width:\s*auto/);
    expect(css).toMatch(/\.queue-datatable \.datatable-table tbody td\.col-assignment \{[^}]*width:\s*auto/);
    expect(css).toMatch(/\.queue-datatable \.datatable-table tbody td\.cell-id \{[^}]*width:\s*1%/);
    expect(css).toMatch(/\.queue-datatable \.datatable-table tbody td\.cell-id \{[^}]*min-width:\s*var\(--datatable-col-min\)/);
    expect(css).not.toMatch(/@container queue-docket/);
    expect(css).not.toMatch(/\.manifest thead \{ display: none;/);
    expect(css).not.toMatch(/\.datatable-scroll::before \{ content: none \}/);
  });

  it("shows Received in the viewer timezone with compact zone disclosure", () => {
    renderLab("/design-lab/reviewer-console");
    const received = document.querySelector(".col-received time");
    expect(received).toHaveAttribute("datetime", "2026-08-25T19:42:00.000Z");
    expect(received?.textContent).toMatch(/\d{2}:\d{2}/);
    expect(received?.textContent).toMatch(/GMT|[A-Z]{2,5}/);
    expect(received?.textContent).not.toMatch(/America\/Chicago/);
    expect(received?.getAttribute("title")).toMatch(/UTC 2026-08-25T19:42:00/);
    expect(screen.queryAllByText(/America\/Chicago/).length).toBe(0);
  });

  it("ranks the busy queue by receipt instant newest first", () => {
    renderLab("/design-lab/reviewer-console?demo=busy");
    const datetimes = [...document.querySelectorAll(".col-received time")].map((el) => el.getAttribute("datetime"));
    expect(datetimes[0]).toBe("2026-08-25T19:42:00.000Z");
    expect(datetimes).toEqual([...datetimes].sort((a, b) => (b ?? "").localeCompare(a ?? "")));
  });

  it("hides the queue table chrome when the demo queue is empty", () => {
    renderLab("/design-lab/reviewer-console?demo=empty");
    const table = document.querySelector(".queue-datatable .datatable-table");
    expect(document.getElementById("queueEmpty")).toBeInTheDocument();
    expect(table).toHaveAttribute("hidden");
    expect(table).not.toBeVisible();
    expect(screen.getByRole("region", { name: "Review queue" })).toHaveClass("registry-wall--hug");
  });

  it("fills the review queue when more than four sessions are listed", () => {
    renderLab("/design-lab/reviewer-console?demo=busy");
    expect(screen.getByRole("region", { name: "Review queue" })).not.toHaveClass("registry-wall--hug");
  });

  it("keeps the evaluation record on a full-bleed split bay", async () => {
    vi.useFakeTimers();
    renderLab("/design-lab/reviewer-console");
    expect(document.querySelector("#main-content > .composition-inset")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "CND-8842-19" }));
    await act(async () => {
      await vi.advanceTimersByTimeAsync(700);
    });
    const split = document.querySelector(".composition-split");
    expect(split).toHaveAttribute("data-flow-split", "bay");
    expect(split).toHaveClass("record-grid");
    expect(screen.getByRole("complementary", { name: "Session manifest" })).toBeInTheDocument();
    expect(screen.getByRole("complementary", { name: "Criterion evaluations" })).toBeInTheDocument();
    expect(screen.getByRole("list", { name: "Sealed examination transcript" })).toBeInTheDocument();
    expect(document.querySelector(".record-view > .operate-head")).toHaveClass("operate-head--plaque");
    expect(document.querySelector(".record-view > .operate-scroll")).toBeTruthy();
    expect(document.querySelector(".record-view .sealed-mark")?.closest(".operate-head-cluster")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Queue" }).closest(".operate-head")).toBeTruthy();
    expect(document.querySelector(".composition-split__head")).toBeNull();
    expect(document.querySelector(".composition-split__foot")).toBeNull();
    expect(document.querySelector(".decision-bar .decision-note")).toBeTruthy();
    expect(document.querySelector(".decision-bar")?.closest(".composition-split")).toBeNull();
  });

  it("lets the drawer breakpoint override the key-group decision grid", () => {
    const css = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../styles/surfaces/reviewer-console.css"),
      "utf8",
    );
    const drawer = css.split("@media (max-width: 960px)")[1] ?? "";
    expect(drawer).toMatch(/\.decision-keys\.key-group/);
    expect(drawer).toMatch(/grid-template-columns:\s*1fr 1fr/);
    expect(drawer).toMatch(/\.decision-keys > \.tip-host:has\(\.key--release\)/);
  });
});

describe("PC-05 and PC-06 campaign setup", () => {
  it("does not activate from a single submit and offers draft plus readiness", async () => {
    renderLab("/design-lab/admin-console/campaigns?campaign=CMP-0044");
    fireEvent.click(screen.getByRole("button", { name: "Configure campaign" }));
    const dialog = screen.getByRole("dialog", { name: /Campaign Configuration/i });
    expect(dialog.querySelector(".dialog-plate--wide")).toBeTruthy();
    expect(dialog.querySelector(".ceremony-plate")).toBeTruthy();
    const ceremonyCss = readFileSync(
      join(dirname(fileURLToPath(import.meta.url)), "../styles/surfaces/admin-console.css"),
      "utf8",
    );
    expect(ceremonyCss).toMatch(/\.ceremony-plate\s*\{[^}]*width:\s*min\(var\(--dialog-w, 680px\)/);
    const compact = ceremonyCss.split("@media (max-width: 720px)")[1] ?? "";
    expect(compact).toMatch(/\.ceremony-foot-row\.key-group/);
    expect(compact).toMatch(/grid-template-columns:\s*1fr/);
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

  it("activates a draft campaign without the sealing sweep", async () => {
    renderLab("/design-lab/admin-console/campaigns?campaign=CMP-0044");
    fireEvent.click(screen.getByRole("button", { name: "Configure campaign" }));
    const dialog = screen.getByRole("dialog", { name: /Campaign Configuration/i });
    fireEvent.click(within(dialog).getByRole("button", { name: "Check readiness" }));
    await waitFor(() => {
      expect(within(dialog).getByRole("button", { name: /Confirm activation/ })).toBeEnabled();
    });
    fireEvent.click(within(dialog).getByRole("button", { name: /Confirm activation/ }));
    fireEvent.click(within(dialog).getByRole("button", { name: "Activate campaign" }));
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
    const main = screen.getByRole("main");
    expect(within(main).getByText("Frozen at activation")).toBeInTheDocument();
    const foot = main.querySelector(".plate-foot");
    expect(foot).toBeTruthy();
    expect(within(foot as HTMLElement).getByText("Configuration frozen at activation")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Configure campaign" })).not.toBeInTheDocument();
    expect(document.querySelector(".is-sealing")).toBeNull();
  });

  it("hides configure on frozen campaign records and keeps the frozen line in the plate foot", () => {
    renderLab("/design-lab/admin-console/campaigns?campaign=CMP-0045");
    expect(screen.queryByRole("button", { name: "Configure campaign" })).not.toBeInTheDocument();
    const foot = screen.getByRole("main").querySelector(".plate-foot");
    expect(foot).toBeTruthy();
    expect(foot?.parentElement).toHaveClass("in-plate-host");
    expect(within(foot as HTMLElement).getByText("Configuration frozen at activation")).toBeInTheDocument();
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
    expect(screen.getByRole("region", { name: "Cohort enrollment manifest" })).toHaveClass("registry-wall--hug");
  });

  it("hugs the campaign registry when search matches four or fewer campaigns", () => {
    renderLab("/design-lab/admin-console/campaigns");
    const region = screen.getByRole("region", { name: "Campaign registry" });
    expect(region).not.toHaveClass("registry-wall--hug");
    fireEvent.change(screen.getByRole("searchbox", { name: "Search campaign title or ID" }), {
      target: { value: "zzz-no-match" },
    });
    expect(screen.getByRole("region", { name: "Campaign registry" })).toHaveClass("registry-wall--hug");
  });

  it("hugs the enrollment manifest when search matches four or fewer rows", async () => {
    renderLab("/design-lab/admin-console/enrollments");
    const region = await screen.findByRole("region", { name: "Cohort enrollment manifest" });
    expect(region).not.toHaveClass("registry-wall--hug");
    fireEvent.change(screen.getByRole("searchbox", { name: "Search participant ID" }), {
      target: { value: "zzz-no-match" },
    });
    expect(await screen.findByRole("region", { name: "Cohort enrollment manifest" })).toHaveClass("registry-wall--hug");
  });
});

describe("administrator chrome", () => {
  it("does not duplicate Home in the command strip when area navigation is in the gangway", () => {
    renderLab("/design-lab/admin-console/enrollments");
    expect(screen.queryByRole("navigation", { name: "Primary" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Home" })).not.toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: "Administrator areas" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Enrollments" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Campaigns" })).toBeInTheDocument();
  });
});

describe("PC-08 session specimen", () => {
  it("labels the timer as synthetic and avoids listening copy", () => {
    renderLab("/design-lab/participant-session?state=live");
    expect(screen.getByText(/Synthetic demonstration timer/i)).toBeInTheDocument();
    expect(screen.queryByText(/I’m listening/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/I'm listening/i)).not.toBeInTheDocument();
  });

  it("keeps the examination console brand and rail actions outside the rail scroller", () => {
    const { container } = renderLab("/design-lab/participant-session?state=live");
    const rail = screen.getByRole("complementary", { name: "Session instruments" });
    const brand = rail.querySelector(".rail-brand");
    const scroller = rail.querySelector(".rail-scroll");
    expect(brand).toBeTruthy();
    expect(scroller).toBeTruthy();
    expect(brand!.parentElement).toBe(rail);
    expect(scroller!.contains(brand)).toBe(false);
    expect(brand!.querySelector(".rail-nav")).toBeTruthy();
    expect(scroller!.querySelector(".rail-nav")).toBeNull();
    expect(within(brand as HTMLElement).getByRole("link", { name: "Back to assignment" })).toBeInTheDocument();
    expect(within(brand as HTMLElement).getByRole("button", { name: "Leave session" })).toBeInTheDocument();
    expect(within(scroller as HTMLElement).getByRole("heading", { name: "Console Feed" })).toBeInTheDocument();
    expect(container.querySelector(".rail-scroll .protocol-plate")).toBeTruthy();
  });

  it("seats submit and leave ceremonies on the shared dialog plate with an accessible footer", () => {
    renderLab("/design-lab/participant-session?state=live");

    fireEvent.click(screen.getByRole("button", { name: "Submit Session" }));
    const submitDialog = screen.getByRole("dialog", { name: "Confirm Submission" });
    expect(submitDialog.querySelector(".dialog-plate")).toBeTruthy();
    expect(submitDialog.querySelector(".dialog-plate--wide")).toBeTruthy();
    expect(submitDialog.querySelector(".dialog-plate--narrow")).toBeNull();
    expect(submitDialog.querySelector(".dialog-foot")).toHaveAttribute("data-arrangement", "split");
    expect(within(submitDialog).getByRole("button", { name: "Remain in Session" })).toBeVisible();
    expect(within(submitDialog).getByRole("button", { name: "Submit Session" })).toBeVisible();

    fireEvent.click(within(submitDialog).getByRole("button", { name: "Remain in Session" }));
    expect(screen.queryByRole("dialog", { name: "Confirm Submission" })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Leave session" }));
    const leaveDialog = screen.getByRole("dialog", { name: "Leave session" });
    expect(leaveDialog.querySelector(".dialog-plate")).toBeTruthy();
    expect(leaveDialog.querySelector(".dialog-plate--wide")).toBeTruthy();
    expect(leaveDialog.querySelector(".dialog-foot")).toHaveAttribute("data-arrangement", "split");
    expect(within(leaveDialog).getByRole("button", { name: "Remain in session" })).toBeVisible();
    expect(within(leaveDialog).getByRole("link", { name: "Leave to assignment" })).toBeVisible();
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
  it("covers disabled, empty, and dialog states with accessible names", { timeout: 15_000 }, () => {
    renderLab("/design-lab/shared/gallery");
    expect(screen.getByRole("heading", { name: "Shared component deck" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Disabled" })).toBeDisabled();
    expect(screen.getByRole("button", { name: /Disabled with reason/ })).toBeDisabled();
    expect(screen.getByText("No assigned sessions")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Open confirm dialog" }));
    expect(screen.getByRole("dialog", { name: "Confirm Release" })).toBeInTheDocument();
  });
});
