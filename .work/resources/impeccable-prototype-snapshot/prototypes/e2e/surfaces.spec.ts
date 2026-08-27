import { expect, test } from "@playwright/test";

const routes = [
  "/",
  "/surfaces",
  "/participant-home",
  "/participant-journey",
  "/participant-session",
  "/admin-console",
  "/reviewer-console",
  "/shared/gallery",
];

for (const route of routes) {
  test(`${route} loads and survives refresh`, async ({ page }) => {
    await page.goto(route);
    await expect(page.locator("body")).toBeVisible();
    await page.reload();
    await expect(page.locator("body")).toBeVisible();
  });
}

const brandIndexRoutes = [
  "/participant-home",
  "/participant-journey",
  "/participant-session?state=warned",
  "/admin-console/enrollments",
  "/reviewer-console",
  "/surfaces",
  "/unknown-route-test",
];

for (const route of brandIndexRoutes) {
  test(`brand on ${route} navigates to channel index`, async ({ page }) => {
    await page.goto(route);
    await page.getByRole("link", { name: "Channel index", exact: true }).click();
    await expect(page).toHaveURL(/\/surfaces$/);
  });
}

test("gallery strip brand navigates to channel index", async ({ page }) => {
  await page.goto("/shared/gallery");
  await page.locator("header.command-strip.page-strip .brand-home-link").click();
  await expect(page).toHaveURL(/\/surfaces$/);
});

test("session examiner wordmark is not a navigation link", async ({ page }) => {
  await page.goto("/participant-session?state=warned");
  await expect(page.locator(".rail-brand .brand-home-link")).toHaveCount(1);
  await expect(page.locator(".agent-name a")).toHaveCount(0);
  await expect(page.locator(".agent-name .brand-mark")).toHaveCount(1);
});

test("home to journey to session preserves navigation", async ({ page }) => {
  await page.goto("/participant-home");
  await page.getByRole("link", { name: "Open" }).click();
  await expect(page).toHaveURL(/participant-journey/);
  await page.locator("#demoState").click();
  await page.locator(".demo-plate [role='option'][data-value='examination-ready']").click();
  await page.getByRole("link", { name: /Enter Session/ }).click();
  await expect(page).toHaveURL(/participant-session/);
});

test("session briefing gates transmit until acknowledged", async ({ page }) => {
  await page.goto("/participant-session");
  await expect(page.getByRole("heading", { name: "Examination Briefing" })).toBeVisible();
  await expect(page.getByRole("button", { name: /Resume Examination/ })).toBeDisabled();
  await page.getByRole("checkbox").check();
  await page.getByRole("button", { name: /Resume Examination/ }).click();
  await expect(page.getByPlaceholder(/Compose reply/)).toBeVisible();
});

test("session warned state skips briefing", async ({ page }) => {
  await page.goto("/participant-session?state=warned");
  await expect(page.getByRole("timer")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Examination Briefing" })).toHaveCount(0);
});

test("admin table search and ceremony validation", async ({ page }) => {
  await page.goto("/admin-console/campaigns?campaign=CMP-0044");
  await expect(page.getByText("Draft — not activated")).toBeVisible();
  await page.getByRole("button", { name: "Configure campaign" }).click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.getByLabel("Time warning at").fill("99:00");
  await page.getByRole("button", { name: "Activate" }).click();
  await expect(page.getByText(/Time warning must land before/)).toBeVisible();
});

test("admin navigation and page context preserve campaign", async ({ page }) => {
  await page.goto("/admin-console/enrollments?campaign=CMP-0043");
  await expect(page.getByRole("navigation", { name: "Administrator areas" })).toBeVisible();
  const context = page.getByLabel("Campaign context");
  await expect(context).toContainText("CMP-0043 / Ops Integrity");
  await expect(context).toContainText("Draft — not activated");
  await expect(page.locator("main > .operate-head + .campaign-context")).toBeVisible();
  await page.getByRole("button", { name: "Collapse menu" }).click();
  await expect(page.locator(".gangway.is-collapsed")).toBeVisible();
  await expect(page.locator(".gangway").getByRole("link")).toHaveCount(7);
  await page.getByRole("button", { name: "Expand menu" }).click();
  await page.getByRole("link", { name: "Campaigns", exact: true }).click();
  await expect(page).toHaveURL(/\/admin-console\/campaigns$/);
  await expect(page.getByLabel("Campaign context")).toHaveCount(0);
  await page.getByRole("link", { name: "Cohorts" }).click();
  await expect(page).toHaveURL(/admin-console\/cohorts\?campaign=CMP-0043/);
  await expect(page.locator("main > .operate-head + .campaign-context")).toContainText("CMP-0043 / Ops Integrity");
  await expect(page.locator("#campaignKey")).toHaveCount(1);
  await page.getByRole("link", { name: "Sessions" }).click();
  await expect(page).toHaveURL(/admin-console\/sessions\?campaign=CMP-0043/);
  await expect(page.locator("main > .operate-head + .campaign-context")).toContainText("CMP-0043 / Ops Integrity");
  await page.getByRole("link", { name: "Users & Access" }).click();
  await expect(page).toHaveURL(/admin-console\/users-access$/);
  await expect(page).not.toHaveURL(/campaign=/);
  await expect(page.getByLabel("Campaign context")).toHaveCount(0);
  await page.getByRole("link", { name: "Policies" }).click();
  await expect(page).toHaveURL(/admin-console\/policies$/);
  await expect(page.getByLabel("Campaign context")).toHaveCount(0);
  await page.getByRole("link", { name: "Audit Log" }).click();
  await expect(page).toHaveURL(/admin-console\/audit-log$/);
  await page.getByRole("link", { name: "Enrollments" }).click();
  await expect(page).toHaveURL(/admin-console\/enrollments\?campaign=CMP-0043/);
  await expect(page.getByLabel("Campaign context")).toContainText("CMP-0043 / Ops Integrity");
});

test("admin navigation uses bulkhead on narrow viewports", async ({ page }) => {
  await page.setViewportSize({ width: 900, height: 900 });
  await page.goto("/admin-console/enrollments?campaign=CMP-0043");
  await expect(page.locator(".gangway")).toHaveCount(0);
  await page.getByRole("button", { name: "Menu", exact: true }).click();
  const drawer = page.getByRole("dialog", { name: /Administrator/i });
  await expect(drawer).toBeVisible();
  await expect(drawer.getByRole("link", { name: "Cohorts" })).toBeVisible();
  await expect(drawer.getByRole("link", { name: "Users & Access" })).toBeVisible();
  await expect(drawer.getByRole("link", { name: "Audit Log" })).toBeVisible();
  await expect(drawer.getByRole("link")).toHaveCount(7);
  await drawer.getByRole("link", { name: "Campaigns", exact: true }).click();
  await expect(page).toHaveURL(/\/admin-console\/campaigns$/);
  await expect(drawer).toHaveCount(0);
  await page.getByRole("button", { name: "Menu", exact: true }).click();
  await expect(drawer).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(drawer).toHaveCount(0);
});

test("session submit dialog opens from the live console", async ({ page }) => {
  await page.goto("/participant-session?state=live");
  await page.getByRole("button", { name: "Submit Session" }).click();
  await expect(page.locator("#confirmDialog")).toBeVisible();
  await page.locator("#confirmCancel").click();
  await expect(page.getByPlaceholder(/Compose reply/)).toBeVisible();
});

test("session composer sends on Enter and newline on Shift+Enter", async ({ page }) => {
  await page.goto("/participant-session?state=live");
  const composer = page.getByPlaceholder(/Compose reply/);
  await composer.fill("Line one");
  await composer.press("Shift+Enter");
  await composer.type("Line two");
  await expect(composer).toHaveValue("Line one\nLine two");
  await composer.press("Enter");
  await expect(composer).toHaveValue("");
  await expect(page.locator(".turn--participant .turn-text").last()).toHaveText("Line one\nLine two");
});

test("session composer ignores Enter on whitespace and while busy", async ({ page }) => {
  await page.goto("/participant-session?state=live");
  const composer = page.getByPlaceholder(/Compose reply/);
  const participantTurns = () => page.locator(".turn--participant");
  const initialCount = await participantTurns().count();

  await composer.fill("   ");
  await composer.press("Enter");
  await expect(participantTurns()).toHaveCount(initialCount);

  await composer.fill("Hold the line");
  await composer.press("Enter");
  await expect(participantTurns()).toHaveCount(initialCount + 1);
  await composer.fill("Second attempt");
  await composer.press("Enter");
  await expect(participantTurns()).toHaveCount(initialCount + 1);
  await expect(composer).toHaveValue("Second attempt");
});

test("admin campaigns keep independent configuration", async ({ page }) => {
  await page.goto("/admin-console/campaigns?campaign=CMP-0043");
  await expect(page.locator(".campaigns-wall")).toContainText("GOVERNED-EXAM-02");
  await expect(page.getByRole("link", { name: "Campaigns", exact: true })).toHaveAttribute("aria-current", "page");
  await expect(page.getByLabel("Campaign context")).toHaveCount(0);
  await page.getByRole("button", { name: "Campaigns" }).click();
  await page.getByRole("button", { name: "CMP-0042 / Structural Audit Q3", exact: true }).click();
  await expect(page.locator(".campaigns-wall")).toContainText("GOVERNED-EXAM-01");
  await page.getByRole("button", { name: "Campaigns" }).click();
  await page.getByRole("button", { name: "CMP-0043 / Ops Integrity", exact: true }).click();
  await expect(page.locator(".campaigns-wall")).toContainText("GOVERNED-EXAM-02");
});

test("admin campaign registry search filter and invalid deep link", async ({ page }) => {
  await page.goto("/admin-console/campaigns");
  await expect(page.getByRole("heading", { name: "Campaign Registry" })).toBeVisible();
  await expect(page.locator("#campaignCountValue")).toContainText("20 campaigns");
  await page.locator("#campaignSearchInput").fill("OPS");
  await expect(page.getByRole("button", { name: "CMP-0043 / Ops Integrity", exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: "CMP-0042 / Structural Audit Q3", exact: true })).toHaveCount(0);
  await page.locator("#campaignSearchInput").fill("");
  await page.locator("#campaignFilterKey").click();
  await page.getByRole("option", { name: "Frozen" }).click();
  await expect(page.locator("#campaignCountValue")).toContainText("6 campaign");
  await page.goto("/admin-console/campaigns?campaign=CMP-NOPE");
  await expect(page.getByText("Campaign not found")).toBeVisible();
  await expect(page.getByLabel("Campaign context")).toHaveCount(0);
  await page.getByRole("button", { name: "Back to campaigns" }).click();
  await expect(page).toHaveURL(/\/admin-console\/campaigns$/);
  await expect(page.getByRole("heading", { name: "Campaign Registry" })).toBeVisible();
});

test("admin enrollments campaign selector updates the url", async ({ page }) => {
  await page.goto("/admin-console/enrollments");
  await expect(page).toHaveURL(/campaign=CMP-0042/);
  await page.locator("#campaignKey").click();
  await page.getByPlaceholder("Filter campaigns").fill("0043");
  await page.getByRole("option", { name: /CMP-0043 \/ Ops Integrity/ }).click();
  await expect(page).toHaveURL(/admin-console\/enrollments\?campaign=CMP-0043/);
  await expect(page.getByLabel("Campaign context")).toContainText("CMP-0043 / Ops Integrity");
  await expect(page.locator(".datatable-toolbar")).not.toContainText("Campaign:");
});

test("admin campaign-scoped sample areas inherit the same campaign default", async ({ page }) => {
  await page.goto("/admin-console/cohorts");
  await expect(page).toHaveURL(/admin-console\/cohorts\?campaign=CMP-0042/);
  await expect(page.getByText("Campaign not selected")).toHaveCount(0);
  await expect(page.locator("main > .operate-head + .campaign-context")).toContainText("CMP-0042 / Structural Audit Q3");
  await expect(page.getByText("No cohort records loaded")).toBeVisible();
  await page.goto("/admin-console/sessions?campaign=CMP-NOPE");
  await expect(page).toHaveURL(/admin-console\/sessions\?campaign=CMP-0042/);
  await expect(page.locator("main > .operate-head + .campaign-context")).toContainText("CMP-0042 / Structural Audit Q3");
  await expect(page.getByText("No session telemetry loaded")).toBeVisible();
});

test("admin expansion, filter, and keyboard smoke", async ({ page }) => {
  await page.goto("/admin-console");
  await page.locator("#filterKey").click();
  await expect(page.locator("#filterMenu")).toBeVisible();
  await page.locator("#filterMenu li").nth(3).click();
  await page.getByRole("button", { name: "Expand enrollment P-3121" }).click();
  const detail = page.locator(".datatable-detail-cut");
  const scroll = page.locator(".datatable-scroll");
  await expect(detail).toBeVisible();
  const detailBox = await detail.boundingBox();
  const scrollBox = await scroll.boundingBox();
  expect(detailBox).not.toBeNull();
  expect(scrollBox).not.toBeNull();
  expect(Math.abs(detailBox!.x - scrollBox!.x)).toBeLessThanOrEqual(1);
  expect(Math.abs(detailBox!.x + detailBox!.width - (scrollBox!.x + scrollBox!.width))).toBeLessThanOrEqual(1);
  await page.keyboard.press("Tab");
});

test("all datatable surfaces share canonical geometry", async ({ page }) => {
  const geometry = async () =>
    page.locator(".datatable").first().evaluate((root) => {
      const round = (value: number) => Math.round(value * 100) / 100;
      const toolbar = root.querySelector<HTMLElement>(".datatable-toolbar");
      const scroll = root.querySelector<HTMLElement>(".datatable-scroll");
      const foot = root.querySelector<HTMLElement>(".datatable-foot");
      const th = root.querySelector<HTMLElement>("thead th");
      const rows = [...root.querySelectorAll<HTMLElement>("tbody tr.datatable-row")];
      const first = rows[0];
      const second = rows[1];
      const id0 = first?.querySelector<HTMLElement>(".datatable-id");
      const id1 = second?.querySelector<HTMLElement>(".datatable-id");
      if (!toolbar || !scroll || !foot || !th || !first || !second || !id0 || !id1) {
        throw new Error("Complete datatable chrome is required");
      }
      const before = getComputedStyle(scroll, "::before");
      const beforeHeight = parseFloat(before.height);
      const paintedRail =
        before.boxSizing === "border-box"
          ? beforeHeight
          : beforeHeight + parseFloat(before.borderBottomWidth || "0");
      const thBox = th.getBoundingClientRect();
      const firstBox = first.getBoundingClientRect();
      const secondBox = second.getBoundingClientRect();
      const overlap = Math.max(0, Math.min(thBox.bottom, firstBox.bottom) - Math.max(thBox.top, firstBox.top));
      return {
        gutter: getComputedStyle(root).getPropertyValue("--datatable-inline-gutter").trim(),
        toolbarInline: getComputedStyle(toolbar).paddingInlineStart,
        scrollInline: getComputedStyle(scroll).paddingInlineStart,
        footInline: getComputedStyle(foot).paddingInlineStart,
        headHeight: round(thBox.height),
        paintedRail: round(paintedRail),
        headOvershoot: round(paintedRail - thBox.height),
        firstRowOverlap: round(overlap),
        firstRowHeight: round(firstBox.height),
        secondRowHeight: round(secondBox.height),
        firstTextInset: round(id0.getBoundingClientRect().top - firstBox.top),
        secondTextInset: round(id1.getBoundingClientRect().top - secondBox.top),
      };
    });

  await page.goto("/shared/gallery");
  await expect(page.locator("#dtBody .datatable-row").first()).toBeVisible();
  const gallery = await geometry();

  await page.goto("/admin-console/enrollments?campaign=CMP-0042");
  await expect(page.locator(".datatable-table .datatable-row").first()).toBeVisible();
  const enrollments = await geometry();

  await page.goto("/admin-console/campaigns");
  await expect(page.locator(".datatable-table .datatable-row").first()).toBeVisible();
  const campaigns = await geometry();

  expect(gallery).toMatchObject({
    gutter: "18px",
    toolbarInline: "18px",
    scrollInline: "18px",
    footInline: "18px",
    headOvershoot: 0,
    firstRowOverlap: 0,
  });
  expect(gallery.headHeight).toBe(gallery.paintedRail);
  expect(gallery.firstTextInset).toBe(gallery.secondTextInset);
  expect(gallery.firstRowHeight).toBe(gallery.secondRowHeight);
  expect(enrollments).toEqual(gallery);
  expect(campaigns).toEqual(gallery);

  await page.goto("/reviewer-console");
  await expect(page.locator(".queue-datatable .datatable-row").first()).toBeVisible();
  await expect(page.locator(".queue-datatable")).toHaveClass(/datatable--body-only/);
  await expect(page.locator(".queue-datatable")).toHaveCSS("--datatable-inline-gutter", "18px");
  await expect(page.locator(".queue-datatable .datatable-scroll")).toHaveCSS("padding-left", "18px");

  await page.setViewportSize({ width: 390, height: 844 });
  for (const route of ["/shared/gallery", "/admin-console/enrollments?campaign=CMP-0042", "/admin-console/campaigns"]) {
    await page.goto(route);
    await expect(page.locator(".datatable-table").first()).toHaveCSS("min-width", "680px");
  }
});

test("datatable content cells do not open records", async ({ page }) => {
  await page.goto("/admin-console/enrollments?campaign=CMP-0042");
  await page.getByRole("cell", { name: "EXAMINATION" }).first().click();
  await expect(page.locator("body.view-record")).toHaveCount(0);
  await expect(page).toHaveURL(/admin-console\/enrollments/);

  await page.goto("/admin-console/campaigns");
  await page.getByRole("cell", { name: "Frozen" }).first().click();
  await expect(page).toHaveURL(/\/admin-console\/campaigns$/);
  await expect(page).not.toHaveURL(/campaign=/);

  await page.goto("/reviewer-console");
  await page.getByRole("cell", { name: "Real-time Inventory & Order Management at Scale" }).first().click();
  await expect(page.locator("body.view-record")).toHaveCount(0);
});

test("datatable identifiers open canonical records", async ({ page }) => {
  await page.goto("/admin-console/campaigns");
  await page.getByRole("button", { name: "CMP-0042 / Structural Audit Q3", exact: true }).click();
  await expect(page).toHaveURL(/campaign=CMP-0042/);

  await page.goto("/reviewer-console");
  await page.getByRole("button", { name: "CND-8842-19", exact: true }).click();
  await expect(page.locator("body.view-record")).toBeVisible();
  await expect(page.getByRole("heading", { name: /Overlay Ledger/ })).toBeVisible();
});

test("gallery specimens bind dropdown and dialog", async ({ page }) => {
  await page.goto("/shared/gallery");
  await page.locator("[data-gangway] .gangway-toggle").click();
  await expect(page.locator("[data-gangway].gangway.is-collapsed")).toBeVisible();
  await page.locator("[data-gangway] .gangway-toggle").click();
  await expect(page.locator("[data-gangway].gangway.is-collapsed")).toHaveCount(0);
  await page.locator("#demoDropKey").click();
  await expect(page.locator("#demoDropKey + .dropdown-menu")).toBeVisible();
  await page.keyboard.press("ArrowDown");
  await page.keyboard.press("Enter");
  await expect(page.locator("#demoDropValue")).toContainText("GOVERNED");
  await page.locator("#demoSearchKey").click();
  await page.locator("#demoSearchFilter").fill("ops-02");
  await page.locator("#demoSearchOpt5").click();
  await expect(page.locator("#demoSearchValue")).toContainText("GOVERNED-OPS-02");
  await page.locator("#demoContextSearchKey").focus();
  await page.keyboard.press("Enter");
  await expect(page.locator("#demoContextSearchFilter")).toBeFocused();
  await page.locator("#demoContextSearchFilter").fill("berth");
  await page.locator("#demoContextSearchOpt3").click();
  await expect(page.locator("#demoContextSearchValue")).toContainText("CMP-0054");
  await page.locator("#demoDate").click();
  const dateDialog = page.getByRole("dialog", { name: "Choose date" });
  await expect(dateDialog).toBeVisible();
  await dateDialog.getByRole("button", { name: "2026-09-10" }).click();
  await expect(page.locator("#demoDateValue")).toContainText("2026-09-10");
  await page.locator("#demoTime").click();
  const timeDialog = page.getByRole("dialog", { name: "Choose time" });
  await expect(timeDialog).toBeVisible();
  await timeDialog.getByRole("listbox", { name: "Hours" }).getByRole("option", { name: "14", exact: true }).click();
  await timeDialog.getByRole("listbox", { name: "Minutes" }).getByRole("option", { name: "30", exact: true }).click();
  await timeDialog.getByRole("button", { name: "Done" }).click();
  await expect(page.locator("#demoTimeValue")).toContainText("14:30");
  await page.locator("#dialogOpenKey").click();
  await expect(page.locator("#deckDialog")).toBeVisible();
  await page.keyboard.press("Escape");
});

test("reviewer edits persist after reload", async ({ page }) => {
  await page.goto("/reviewer-console");
  const openRecord = () => page.getByRole("row", { name: /CND-8842-19/ }).getByRole("button").last().click();
  await openRecord();
  await expect(page.locator("body.view-record")).toBeVisible({ timeout: 2000 });
  await expect(page.getByRole("heading", { name: /Overlay Ledger/ })).toBeVisible();
  await page.getByRole("button", { name: "Adjust" }).click();
  await page.locator(".marginalia-plate textarea").first().fill("Local synthetic revision.");
  await page.getByRole("button", { name: "Save adjustment" }).click();
  await page.reload();
  await openRecord();
  await expect(page.getByRole("heading", { name: /Overlay Ledger/ })).toBeVisible();
  await expect(page.locator(".marginalia-plate textarea").first()).toHaveValue("Local synthetic revision.");
});

test("unknown route uses the not-found plate", async ({ page }) => {
  await page.goto("/no-such-channel");
  await expect(page.getByText("Channel not found")).toBeVisible();
  await page.getByRole("link", { name: "Return to channel index" }).click();
  await expect(page).toHaveURL("/surfaces");
});

test("root redirects to the channel catalog", async ({ page }) => {
  await page.goto("/");
  await expect(page).toHaveURL("/surfaces");
  await expect(page.getByRole("heading", { name: "Prototype Surfaces" })).toBeVisible();
  await page.reload();
  await expect(page).toHaveURL("/surfaces");
  await expect(page.getByRole("heading", { name: "Prototype Surfaces" })).toBeVisible();
});

test("channel index lists surfaces and opens participant home", async ({ page }) => {
  await page.goto("/surfaces");
  await expect(page.getByRole("heading", { name: "Prototype Surfaces" })).toBeVisible();
  await expect(page.getByText("6 CHANNELS")).toBeVisible();
  await expect(page.getByRole("link", { name: "Open Status Bays" })).toBeVisible();
  await page.getByRole("link", { name: "Open Status Bays" }).click();
  await expect(page).toHaveURL(/participant-home/);
});

test("channel index opens every registered surface", async ({ page }) => {
  const channels = [
    { name: "Open Status Bays", path: /participant-home/ },
    { name: "Open Assignment Station", path: /participant-journey/ },
    { name: "Open Examination Console", path: /participant-session/ },
    { name: "Open Administration", path: /admin-console\/enrollments/ },
    { name: "Open Review Console", path: /reviewer-console/ },
    { name: "Open Component Deck", path: /shared\/gallery/ },
  ] as const;

  for (const channel of channels) {
    await page.goto("/surfaces");
    await page.getByRole("link", { name: channel.name }).click();
    await expect(page).toHaveURL(channel.path);
  }
});

test("gallery returns to channel index", async ({ page }) => {
  await page.goto("/shared/gallery");
  await page.getByRole("link", { name: "Index", exact: true }).click();
  await expect(page).toHaveURL("/surfaces");
});

test("session complete state and assignment link", async ({ page }) => {
  await page.goto("/participant-session?state=complete");
  await expect(page.getByRole("heading", { name: "Session Complete" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Back to assignment" })).toHaveAttribute("href", /demo=result-pending/);
  await page.getByRole("link", { name: "Return to Assignment" }).click();
  await expect(page).toHaveURL(/participant-journey\?demo=result-pending/);
  await expect(page.getByRole("heading", { name: "Awaiting release" })).toBeVisible();
});

test("session submit seals the record and returns to assignment", async ({ page }) => {
  await page.goto("/participant-session?state=live");
  await page.locator("#submitOpen").click();
  await page.locator("#confirmSubmit").click();
  await expect(page.getByRole("heading", { name: "Session Complete" })).toBeVisible();
  await expect(page.locator("#completeToAssignment")).toBeFocused();
  await expect(page.getByRole("link", { name: "Back to assignment" })).toHaveAttribute("href", /demo=result-pending/);
  await expect(page.locator("#completeToAssignment")).toHaveAttribute("href", /demo=result-pending/);
  await page.locator("#completeToAssignment").click();
  await expect(page).toHaveURL(/participant-journey\?demo=result-pending/);
  await expect(page.getByRole("heading", { name: "Awaiting release" })).toBeVisible();
});

test("reviewer record uses bulkhead drawers on mobile", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/reviewer-console");
  await page.getByRole("button", { name: "Inspect" }).click();
  await expect(page.locator("body.view-record")).toBeVisible({ timeout: 2000 });
  await expect(page.getByRole("heading", { name: /Overlay Ledger/ })).toBeVisible();
  await expect(page.locator(".record-drawer-bar")).toBeVisible();
  await expect(page.locator(".manifest-rail")).toHaveCount(0);

  await page.getByRole("button", { name: "Marginalia", exact: true }).click();
  await expect(page.getByRole("dialog", { name: /Criterion Marginalia/i })).toBeVisible();
  await expect(page.locator(".tether-line").first()).toBeVisible({ timeout: 5000 });

  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog", { name: /Criterion Marginalia/i })).toHaveCount(0);

  await page.getByRole("button", { name: "Manifest", exact: true }).click();
  const manifestDialog = page.getByRole("dialog", { name: /Session manifest/i });
  await expect(manifestDialog).toBeVisible();
  await manifestDialog.getByRole("button", { name: "Close", exact: true }).click();
  await expect(manifestDialog).toHaveCount(0);

  await page.getByRole("button", { name: "Adjust" }).click();
  await expect(page.locator("#recordMarginaliaBulkhead.bulkhead--wide")).toHaveClass(/is-open/);
  const marginaliaDialog = page.getByRole("dialog", { name: /Criterion Marginalia/i });
  await marginaliaDialog.getByRole("button", { name: "Save adjustment" }).click();
  await expect(marginaliaDialog).toHaveCount(0);
});

test("reviewer record draws tethers", async ({ page }) => {
  await page.goto("/reviewer-console");
  await page.getByRole("button", { name: "Inspect" }).click();
  await expect(page.getByRole("heading", { name: /Overlay Ledger/ })).toBeVisible();
  await expect(page.locator(".tether-line").first()).toBeVisible({ timeout: 5000 });
});

test("journey briefing gates continue until acknowledged", async ({ page }) => {
  await page.goto("/participant-journey");
  await expect(page.getByRole("button", { name: /Acknowledge/ })).toBeDisabled();
  await page.getByRole("checkbox").check();
  await page.getByRole("button", { name: /Acknowledge/ }).click();
  await expect(page).toHaveURL(/demo=submission/);
});

test("mobile viewport loads each canonical route", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  for (const route of routes) {
    await page.goto(route);
    await expect(page.locator("body")).toBeVisible();
  }
});

test("authenticated shells use role home instead of the catalog", async ({ page }) => {
  await page.goto("/participant-home");
  await expect(page.getByRole("link", { name: "Home" })).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Primary" }).getByRole("link", { name: "Index" })).toHaveCount(0);

  await page.goto("/reviewer-console");
  await expect(page.getByRole("link", { name: "Review" })).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Primary" }).getByRole("link", { name: "Index" })).toHaveCount(0);

  await page.goto("/admin-console/enrollments?campaign=CMP-0043");
  await page.getByRole("link", { name: "Home" }).click();
  await expect(page).toHaveURL(/admin-console\/enrollments\?campaign=CMP-0043/);
});

test("profile actions are disabled and sign out cancels with focus return", async ({ page }) => {
  await page.goto("/participant-home");
  const trigger = page.getByRole("button", { name: /operator menu/i });
  await trigger.click();
  await expect(page.getByRole("menuitem", { name: /Profile/ })).toBeDisabled();
  await expect(page.getByRole("menuitem", { name: /Preferences/ })).toBeDisabled();
  await page.getByRole("menuitem", { name: "Sign out" }).click();
  await expect(page.getByRole("heading", { name: "End prototype session" })).toBeVisible();
  await page.getByRole("button", { name: "Remain signed in" }).click();
  await expect(trigger).toBeFocused();
  await expect(page).toHaveURL(/participant-home/);
});

test("journey identity disclosure is keyboard operable", async ({ page }) => {
  await page.goto("/participant-journey");
  const trigger = page.getByRole("button", { name: /operator menu/i });
  await trigger.focus();
  await page.keyboard.press("ArrowDown");
  await expect(page.getByRole("menuitem", { name: "Sign out" })).toBeFocused();
  await page.keyboard.press("Escape");
  await expect(trigger).toBeFocused();
});

test("session leave ceremony cancel and confirm", async ({ page }) => {
  await page.goto("/participant-session?state=live");
  const leave = page.getByRole("button", { name: "Leave session" });
  await leave.click();
  await expect(page.getByRole("heading", { name: "Leave session" })).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(leave).toBeFocused();
  await leave.click();
  await page.getByRole("link", { name: "Leave to assignment" }).click();
  await expect(page).toHaveURL(/participant-journey\?demo=examination-active/);
});

test("gallery and campaign datatables keep persistent compact action bars", async ({ page }) => {
  await page.goto("/shared/gallery#datatable");
  const galleryStrip = page.locator("#dtActionsStrip");
  await expect(galleryStrip).toBeVisible();
  await expect(page.locator("#create")).toBeEnabled();
  await expect(page.locator("#dtBulkKey")).toBeDisabled();
  await expect(page.locator("#dtBulkKey")).toHaveText("Export");
  await expect(page.locator("#dtDownloadKey")).toHaveText("Download");
  await expect(page.locator("#dtMoreKey")).toHaveText("More");

  const header = page.locator("#dtSelectAll");
  await expect(header).toHaveAttribute("aria-label", /Select all visible enrollments/i);
  // Controlled header mark: native `.check()` races React's checked reset. Click the visible label instead.
  await page.locator("label.select-head").filter({ has: header }).click();
  await expect(header).toHaveAttribute("aria-label", /matching enrollments/i);
  await expect(page.locator("#dtBulkKey")).toBeEnabled();
  await page.getByRole("button", { name: "Clear" }).click();
  await expect(header).toHaveAttribute("aria-label", /Select all visible enrollments/i);

  await page.goto("/admin-console/campaigns");
  await expect(page.getByRole("button", { name: /Export summary/i })).toBeDisabled();
  await expect(page.getByText("Export", { exact: true })).toBeVisible();
  await expect(page.getByText(/Select all matching/i)).toHaveCount(0);
  await expect(page.locator("#campaignFilterMenu")).toHaveAttribute("role", "listbox");
});

test("admin profile menu does not block the mobile menu key", async ({ page }) => {
  await page.setViewportSize({ width: 900, height: 900 });
  await page.goto("/admin-console/enrollments");
  await page.getByRole("button", { name: /operator menu/i }).click();
  await expect(page.getByRole("menu", { name: "Operator menu" })).toBeVisible();
  await page.getByRole("button", { name: "Menu", exact: true }).click();
  await expect(page.getByRole("dialog", { name: /Administrator/i })).toBeVisible();
});

test("component deck grouped index still reaches sections", async ({ page }) => {
  await page.goto("/shared/gallery");
  await page.getByRole("link", { name: "Dialog" }).click();
  await expect(page).toHaveURL(/#dialog/);
  await expect(page.locator("#dialog")).toBeVisible();

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/shared/gallery");
  const foundations = page.getByRole("group", { name: "Foundations" }).or(page.locator("details.nav-rail-section").first());
  await expect(foundations).toBeVisible();
  await page.getByRole("link", { name: "Colors" }).click();
  await expect(page).toHaveURL(/#colors/);
});
