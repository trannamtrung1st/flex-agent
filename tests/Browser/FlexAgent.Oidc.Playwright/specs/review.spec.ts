import { expect, test } from "@playwright/test";
import { demoReview, openDemoCriterionInspector, signInAsReviewer } from "../helpers/review";

test.describe.configure({ mode: "serial" });

test("REVIEW-E2E-01 assigned Review work registry [REVIEW-E2E-01]", async ({ page }) => {
  await page.goto("/");
  await signInAsReviewer(page);
  await page.goto("/review");
  await expect(page.getByRole("heading", { name: "Review work" })).toBeVisible();
  await expect(page.getByText("Internal Evaluation · Not a released Result")).toBeVisible();
  await expect(page.getByRole("link", { name: /Open review/i })).toBeVisible();
  await expect(page.getByRole("button", { name: /approve/i })).toHaveCount(0);
  await expect(page.getByRole("link", { name: /release/i })).toHaveCount(0);
});

test("REVIEW-E2E-02 criterion Evidence inspect and focus restore [REVIEW-E2E-02]", async ({ page }) => {
  await page.goto("/");
  await signInAsReviewer(page);
  await openDemoCriterionInspector(page);
  const openEvidence = page.getByRole("link", { name: "Open Evidence" });
  await openEvidence.click();
  await expect(page.getByLabel("Evidence provenance")).toBeVisible();
  await expect(page.getByText("Internal Evaluation · Not a released Result")).toBeVisible();
  const backLinks = page.getByRole("link", { name: "Back to criterion" });
  await expect(backLinks).toHaveCount(2);
  await backLinks.first().click();
  await expect(openEvidence).toBeFocused();
});

test("REVIEW-E2E-03 narrow viewport criterion layout [REVIEW-E2E-03]", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");
  await signInAsReviewer(page);
  await openDemoCriterionInspector(page);
  await expect(page.getByRole("link", { name: "Open Evidence" })).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Criteria" })).toBeVisible();
});

test("REVIEW-E2E-04 reduced motion and forced colors [REVIEW-E2E-04]", async ({ page }) => {
  await page.goto("/");
  await signInAsReviewer(page);
  await page.emulateMedia({ reducedMotion: "reduce", forcedColors: "active" });
  await openDemoCriterionInspector(page);
  await expect(page.getByRole("link", { name: demoReview.criterionLabel })).toHaveAttribute("aria-current", "page");
});

test("REVIEW-E2E-05 400% zoom reflow [REVIEW-E2E-05]", async ({ page }) => {
  await page.goto("/");
  await signInAsReviewer(page);
  await page.goto(
    `/review/${demoReview.caseId}/criteria/${encodeURIComponent(demoReview.criterionId)}`,
  );
  await expect(page.getByLabel("Criterion judgment")).toBeVisible({ timeout: 30_000 });
  await page.evaluate(() => {
    document.documentElement.style.fontSize = "400%";
  });
  await expect(page.getByRole("link", { name: "Open Evidence" })).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Criteria" })).toBeVisible();
});
