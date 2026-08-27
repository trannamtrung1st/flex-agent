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

test("gallery Index returns to the catalog", async ({ page }) => {
  await page.goto("/design-lab/shared/gallery");
  await expect(page.getByRole("heading", { name: "Shared component deck" })).toBeVisible();
  await page.getByRole("link", { name: "Index", exact: true }).click();
  await expect(page).toHaveURL(/\/design-lab\/surfaces$/);
  await expect(page.getByRole("heading", { name: "Prototype Surfaces" })).toBeVisible();
});
