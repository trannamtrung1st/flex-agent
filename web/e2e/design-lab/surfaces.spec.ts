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
  await expectScrollerFlush("Session instruments", ".rail-scroll", ".rail-back");
});

test("gallery Index returns to the catalog", async ({ page }) => {
  await page.goto("/design-lab/shared/gallery");
  await expect(page.getByRole("heading", { name: "Shared component deck" })).toBeVisible();
  await page.getByRole("link", { name: "Index", exact: true }).click();
  await expect(page).toHaveURL(/\/design-lab\/surfaces$/);
  await expect(page.getByRole("heading", { name: "Prototype Surfaces" })).toBeVisible();
});
