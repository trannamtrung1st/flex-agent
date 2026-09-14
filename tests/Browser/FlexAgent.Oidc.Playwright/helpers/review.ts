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
