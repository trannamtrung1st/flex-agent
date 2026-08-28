import { expect, test } from "@playwright/test";

test("opens the channel catalog at /design-lab/surfaces", async ({ page }) => {
  await page.goto("/design-lab/surfaces");
  await expect(page.getByRole("heading", { name: "Prototype Surfaces" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Open Status Bays" })).toHaveAttribute(
    "href",
    "/design-lab/participant-home",
  );
});

test("opens a nested route and keeps it after refresh", async ({ page }) => {
  await page.goto("/design-lab/participant-home");
  await expect(page.getByRole("heading", { name: "Assigned work" })).toBeVisible();
  await page.reload();
  await expect(page.getByRole("heading", { name: "Assigned work" })).toBeVisible();
});

test("assignment and session left rails meet the desktop hull", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });

  await page.goto("/design-lab/participant-journey");
  const assignmentRail = page.getByRole("complementary", { name: "Assignment phases" });
  await expect(assignmentRail).toBeVisible();
  const assignmentBox = await assignmentRail.boundingBox();
  expect(assignmentBox).toBeTruthy();
  expect(assignmentBox!.y).toBeLessThanOrEqual(1);
  expect(assignmentBox!.height).toBeGreaterThanOrEqual(898);

  await page.goto("/design-lab/participant-session?state=live");
  const sessionRail = page.getByRole("complementary", { name: "Session instruments" });
  await expect(sessionRail).toBeVisible();
  const sessionBox = await sessionRail.boundingBox();
  expect(sessionBox).toBeTruthy();
  expect(sessionBox!.y).toBeLessThanOrEqual(1);
  expect(sessionBox!.height).toBeGreaterThanOrEqual(898);
});

function expectBoxInViewport(
  box: { x: number; y: number; width: number; height: number } | null,
  viewport: { width: number; height: number },
) {
  expect(box).toBeTruthy();
  expect(box!.y).toBeGreaterThanOrEqual(-1);
  expect(box!.y + box!.height).toBeLessThanOrEqual(viewport.height + 1);
}

function expectIntersectsViewport(
  box: { x: number; y: number; width: number; height: number } | null,
  viewport: { width: number; height: number },
) {
  expect(box).toBeTruthy();
  expect(box!.y + box!.height).toBeGreaterThan(0);
  expect(box!.y).toBeLessThan(viewport.height);
}

async function expectReachableAfterScroll(
  locator: import("@playwright/test").Locator,
  viewport: { width: number; height: number },
  mode: "fit" | "intersect" = "fit",
) {
  await locator.scrollIntoViewIfNeeded();
  await expect(locator).toBeVisible();
  const box = await locator.boundingBox();
  if (mode === "intersect") {
    expectIntersectsViewport(box, viewport);
    return;
  }
  expectBoxInViewport(box, viewport);
}

test("short desktop viewports keep shells inside the window without forcing scroller height", async ({ page }) => {
  const shortDesktop = { width: 1440, height: 500 };
  await page.setViewportSize(shortDesktop);

  await page.goto("/design-lab/participant-journey?demo=examination-ready");
  const assignmentShell = page.locator('[data-layout="guided-task"]');
  const assignmentShellBox = await assignmentShell.boundingBox();
  expect(assignmentShellBox).toBeTruthy();
  expect(assignmentShellBox!.height).toBeLessThanOrEqual(shortDesktop.height + 1);
  expect(assignmentShellBox!.height).toBeGreaterThanOrEqual(shortDesktop.height - 2);

  const assignmentScroller = page.locator(".phase-rail-scroll");
  await expect(assignmentScroller).toBeVisible();
  const assignmentOverflow = await assignmentScroller.evaluate(
    (el) => el.scrollHeight - el.clientHeight,
  );
  expect(assignmentOverflow).toBeGreaterThan(40);
  await assignmentScroller.evaluate((el) => {
    el.scrollTop = el.scrollHeight;
  });
  expectBoxInViewport(
    await page.locator(".phase-rail .protocol-value").boundingBox(),
    shortDesktop,
  );
  expectBoxInViewport(
    await page.getByRole("button", { name: "Start Attempt" }).boundingBox(),
    shortDesktop,
  );

  await page.goto("/design-lab/participant-session?state=live");
  const sessionShell = page.locator('[data-layout="live-session"]');
  const sessionShellBox = await sessionShell.boundingBox();
  expect(sessionShellBox).toBeTruthy();
  expect(sessionShellBox!.height).toBeLessThanOrEqual(shortDesktop.height + 1);
  expect(sessionShellBox!.height).toBeGreaterThanOrEqual(shortDesktop.height - 2);

  const sessionScroller = page.locator(".rail-scroll");
  await expect(sessionScroller).toBeVisible();
  const sessionOverflow = await sessionScroller.evaluate(
    (el) => el.scrollHeight - el.clientHeight,
  );
  expect(sessionOverflow).toBeGreaterThan(40);
  await sessionScroller.evaluate((el) => {
    el.scrollTop = el.scrollHeight;
  });
  expectBoxInViewport(
    await page.locator(".rail .protocol-value").boundingBox(),
    shortDesktop,
  );
  expectBoxInViewport(
    await page.getByRole("button", { name: "Transmit" }).boundingBox(),
    shortDesktop,
  );

  const chrono = page.locator(".chrono");
  await chrono.evaluate((el) => {
    el.scrollTop = el.scrollHeight;
  });
  expectBoxInViewport(
    await page.getByRole("button", { name: "Submit Session" }).boundingBox(),
    shortDesktop,
  );
});

test("narrow session viewport reflows with page scroll so transcript and composer stay reachable", async ({ page }) => {
  const narrowZoom = { width: 320, height: 256 };
  await page.setViewportSize(narrowZoom);
  await page.goto("/design-lab/participant-session?state=live");

  const scrollMetrics = await page.evaluate(() => {
    const consoleEl = document.querySelector('[data-layout="live-session"]')!;
    const consoleBox = consoleEl.getBoundingClientRect();
    return {
      bodyOverflow: getComputedStyle(document.body).overflow,
      consoleTallerThanViewport: consoleBox.height > window.innerHeight,
      pageScrollable: document.documentElement.scrollHeight > window.innerHeight,
    };
  });
  expect(scrollMetrics.bodyOverflow).toBe("auto");
  expect(scrollMetrics.consoleTallerThanViewport).toBe(true);
  expect(scrollMetrics.pageScrollable).toBe(true);

  await expectReachableAfterScroll(
    page.locator(".turn").last(),
    narrowZoom,
    "intersect",
  );
  await expectReachableAfterScroll(
    page.getByRole("textbox", { name: /compose reply/i }),
    narrowZoom,
  );
  await expectReachableAfterScroll(
    page.getByRole("button", { name: "Transmit" }),
    narrowZoom,
  );
});

test("narrow session completion consequence is reachable after page scroll", async ({ page }) => {
  const narrowZoom = { width: 320, height: 256 };
  await page.setViewportSize(narrowZoom);
  await page.goto("/design-lab/participant-session?state=complete");

  await expectReachableAfterScroll(
    page.getByRole("heading", { name: "Session Complete" }),
    narrowZoom,
  );
  await expectReachableAfterScroll(
    page.getByRole("link", { name: /return to assignment/i }),
    narrowZoom,
  );
});

test("session mid-width short viewport keeps transmit reachable", async ({ page }) => {
  const midShort = { width: 1000, height: 500 };
  await page.setViewportSize(midShort);
  await page.goto("/design-lab/participant-session?state=live");

  const sessionShellBox = await page.locator('[data-layout="live-session"]').boundingBox();
  expect(sessionShellBox).toBeTruthy();
  expect(sessionShellBox!.height).toBeLessThanOrEqual(midShort.height + 1);
  expectBoxInViewport(
    await page.getByRole("button", { name: "Transmit" }).boundingBox(),
    midShort,
  );
  expectBoxInViewport(
    await page.getByRole("button", { name: "Leave session" }).first().boundingBox(),
    midShort,
  );
});

test("assignment left-rail brand stays seated while phases scroll", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 720 });
  await page.goto("/design-lab/participant-journey");

  const rail = page.getByRole("complementary", { name: "Assignment phases" });
  const brand = rail.locator(".rail-brand");
  const scroller = rail.locator(".phase-rail-scroll");
  await expect(brand).toBeVisible();
  await expect(scroller).toBeVisible();

  await scroller.evaluate((el) => {
    el.style.height = "220px";
  });
  const overflow = await scroller.evaluate((el) => el.scrollHeight - el.clientHeight);
  expect(overflow).toBeGreaterThan(40);

  const brandBefore = await brand.boundingBox();
  expect(brandBefore).toBeTruthy();
  await scroller.evaluate((el) => {
    el.scrollTop = el.scrollHeight;
  });
  const brandAfter = await brand.boundingBox();
  expect(brandAfter).toBeTruthy();
  expect(Math.abs(brandAfter!.y - brandBefore!.y)).toBeLessThanOrEqual(1);
  const scrolled = await scroller.evaluate((el) => el.scrollTop);
  expect(scrolled).toBeGreaterThan(20);
});

test("session left-rail brand stays seated while instruments scroll", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 720 });
  await page.goto("/design-lab/participant-session?state=live");

  const rail = page.getByRole("complementary", { name: "Session instruments" });
  const brand = rail.locator(".rail-brand");
  const scroller = rail.locator(".rail-scroll");
  await expect(brand).toBeVisible();
  await expect(scroller).toBeVisible();
  await expect(brand.locator(".rail-nav")).toBeVisible();
  await expect(scroller.locator(".rail-nav")).toHaveCount(0);

  await scroller.evaluate((el) => {
    el.style.height = "180px";
  });
  const overflow = await scroller.evaluate((el) => el.scrollHeight - el.clientHeight);
  expect(overflow).toBeGreaterThan(20);

  const brandBefore = await brand.boundingBox();
  expect(brandBefore).toBeTruthy();
  await scroller.evaluate((el) => {
    el.scrollTop = el.scrollHeight;
  });
  const brandAfter = await brand.boundingBox();
  expect(brandAfter).toBeTruthy();
  expect(Math.abs(brandAfter!.y - brandBefore!.y)).toBeLessThanOrEqual(1);
  const scrolled = await scroller.evaluate((el) => el.scrollTop);
  expect(scrolled).toBeGreaterThan(20);
});

test("assignment and session rail scrollports sit on the rail hairline", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 720 });

  async function expectScrollerFlush(railName: string, scrollerClass: string, contentSelector: string) {
    const rail = page.getByRole("complementary", { name: railName });
    const scroller = rail.locator(scrollerClass);
    await expect(scroller).toBeVisible();
    const railBox = await rail.boundingBox();
    const scrollBox = await scroller.boundingBox();
    expect(railBox).toBeTruthy();
    expect(scrollBox).toBeTruthy();
    const railRight = railBox!.x + railBox!.width;
    const scrollRight = scrollBox!.x + scrollBox!.width;
    expect(railRight - scrollRight).toBeGreaterThanOrEqual(0);
    expect(railRight - scrollRight).toBeLessThanOrEqual(2);

    const sample = rail.locator(contentSelector).first();
    await expect(sample).toBeVisible();
    const sampleBox = await sample.boundingBox();
    expect(sampleBox).toBeTruthy();
    expect(railRight - (sampleBox!.x + sampleBox!.width)).toBeGreaterThanOrEqual(14);
  }

  await page.goto("/design-lab/participant-journey");
  await expectScrollerFlush("Assignment phases", ".phase-rail-scroll", ".phase-node");

  await page.goto("/design-lab/participant-session?state=live");
  await expectScrollerFlush("Session instruments", ".rail-scroll", ".feed-log");
});

test("gallery brand returns to the catalog", async ({ page }) => {
  await page.goto("/design-lab/shared/gallery");
  await expect(page.getByRole("heading", { name: "Shared component deck" })).toBeVisible();
  await page.locator('a.brand-home-link[href="/design-lab/surfaces"]').first().click();
  await expect(page).toHaveURL(/\/design-lab\/surfaces$/);
  await expect(page.getByRole("heading", { name: "Prototype Surfaces" })).toBeVisible();
});
