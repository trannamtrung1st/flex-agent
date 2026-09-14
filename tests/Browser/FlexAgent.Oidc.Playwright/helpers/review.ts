import type { Page } from "@playwright/test";
import { expect } from "@playwright/test";
import { signInThroughKeycloak, syntheticUsers } from "./oidc";

export const demoReview = {
  caseId: "f2100000-0000-4000-8000-00000000000e",
  criterionId: "crit.judgment.quality",
  evidenceId: "f2100000-0000-4000-8000-000000000012",
  taskTitle: "Hazard identification response",
  criterionLabel: "crit judgment quality",
} as const;

/** 1280 CSS px desktop at 400% browser zoom ≈ 320 CSS px reflow width. */
export const equivalent400PercentZoomViewport = { width: 320, height: 844 } as const;

export const compactCriterionViewport = { width: 390, height: 844 } as const;

export async function signInAsReviewer(page: Page): Promise<void> {
  await signInThroughKeycloak(
    page,
    syntheticUsers.reviewer.username,
    syntheticUsers.reviewer.password,
  );
  await expect(page.getByRole("heading", { name: "Home" })).toBeVisible({ timeout: 30_000 });
}

export async function openDemoCriterionInspector(page: Page): Promise<void> {
  await page.goto("/review");
  await expect(page.getByRole("heading", { name: "Review work" })).toBeVisible({ timeout: 30_000 });
  await page.getByRole("link", { name: new RegExp(`${demoReview.taskTitle}.*Open review`, "i") }).click();
  await expect(page.getByRole("navigation", { name: "Criteria" })).toBeVisible();
  await page.getByRole("link", { name: demoReview.criterionLabel }).click();
  await expect(page.getByLabel("Criterion judgment")).toBeVisible();
}

export async function assertNoUnintendedHorizontalOverflow(page: Page): Promise<void> {
  const overflow = await page.evaluate(() => {
    const doc = document.documentElement;
    const body = document.body;
    const guided = document.querySelector('[data-layout="guided-task"]') as HTMLElement | null;
    const main = document.querySelector(".layout-guided__main") as HTMLElement | null;
    const delta = (el: HTMLElement) => el.scrollWidth - el.clientWidth;
    return {
      document: delta(doc),
      body: delta(body),
      guided: guided ? delta(guided) : 0,
      main: main ? delta(main) : 0,
    };
  });
  expect(overflow.document, "page horizontal overflow").toBeLessThanOrEqual(1);
  expect(overflow.body, "body horizontal overflow").toBeLessThanOrEqual(1);
  expect(overflow.guided, "guided-task horizontal overflow").toBeLessThanOrEqual(1);
  expect(overflow.main, "guided main horizontal overflow").toBeLessThanOrEqual(1);
}

export async function assertReducedMotionCriterionNav(page: Page, label: string): Promise<void> {
  const transition = await page.getByRole("link", { name: label }).evaluate((el) => {
    const style = getComputedStyle(el);
    return {
      duration: style.transitionDuration,
      property: style.transitionProperty,
    };
  });
  const durations = transition.duration.split(",").map((part) => part.trim());
  expect(
    transition.property === "none"
    || durations.every((part) => part === "0s" || part === "0ms"),
  ).toBe(true);
}

export async function assertForcedColorsCurrentCriterionNav(page: Page, label: string): Promise<void> {
  const outline = await page.getByRole("link", { name: label }).evaluate((el) => {
    const style = getComputedStyle(el);
    return {
      width: style.outlineWidth,
      style: style.outlineStyle,
      offset: style.outlineOffset,
    };
  });
  expect(outline.style).not.toBe("none");
  expect(parseFloat(outline.width)).toBeGreaterThan(0);
  expect(parseFloat(outline.offset)).toBeGreaterThan(0);
}
