import { expect, test } from "@playwright/test";
import {
  assertForcedColorsCurrentCriterionNav,
  assertNoUnintendedHorizontalOverflow,
  assertReducedMotionCriterionNav,
  compactCriterionViewport,
  demoReview,
  demoReviewQueued,
  demoReviewRunning,
  equivalent400PercentZoomViewport,
  openDemoCriterionInspector,
  openDemoReviewCase,
  RUNNING_CRITERION_UNAVAILABLE,
  signInAsReviewer,
} from "../helpers/review";

test.describe.configure({ mode: "serial" });

test("REVIEW-E2E-01 assigned Review work registry [REVIEW-E2E-01]", async ({ page }) => {
  await page.goto("/");
  await signInAsReviewer(page);
  await page.goto("/review");
  await expect(page.getByRole("heading", { name: "Review work" })).toBeVisible();
  await expect(page.getByText("Internal Evaluation · Not a released Result")).toBeVisible();
  await expect(page.locator("#reviewWorkCountValue")).toHaveText("3 assigned cases");
  await expect(page.getByRole("link", { name: /Open review/i })).toHaveCount(1);
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
  await page.setViewportSize(compactCriterionViewport);
  await page.goto("/");
  await signInAsReviewer(page);
  await openDemoCriterionInspector(page);
  await assertNoUnintendedHorizontalOverflow(page);
  const openEvidence = page.getByRole("link", { name: "Open Evidence" });
  await openEvidence.scrollIntoViewIfNeeded();
  await expect(openEvidence).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Criteria" })).toBeVisible();
  await expect(page.getByLabel("Criterion judgment")).toBeVisible();
});

test("REVIEW-E2E-04 reduced motion and forced colors [REVIEW-E2E-04]", async ({ page }) => {
  await page.goto("/");
  await signInAsReviewer(page);
  await page.emulateMedia({ reducedMotion: "reduce", forcedColors: "active" });
  await openDemoCriterionInspector(page);
  const currentLink = page.getByRole("link", { name: demoReview.criterionLabel });
  await expect(currentLink).toHaveAttribute("aria-current", "page");
  await assertReducedMotionCriterionNav(page, demoReview.criterionLabel);
  await assertForcedColorsCurrentCriterionNav(page, demoReview.criterionLabel);
  await assertNoUnintendedHorizontalOverflow(page);
});

test("REVIEW-E2E-05 400% zoom equivalent reflow [REVIEW-E2E-05]", async ({ page }) => {
  await page.setViewportSize(equivalent400PercentZoomViewport);
  await page.goto("/");
  await signInAsReviewer(page);
  await openDemoCriterionInspector(page);
  await assertNoUnintendedHorizontalOverflow(page);

  const openEvidence = page.getByRole("link", { name: "Open Evidence" });
  const criteriaNav = page.getByRole("navigation", { name: "Criteria" });
  const judgment = page.getByLabel("Criterion judgment");

  await openEvidence.scrollIntoViewIfNeeded();
  await expect(openEvidence).toBeVisible();
  await expect(criteriaNav).toBeVisible();
  await expect(judgment).toBeVisible();
  await expect(page.getByText("Internal Evaluation · Not a released Result")).toBeVisible();

  const screenshot = await page.screenshot({ fullPage: true });
  await test.info().attach("review-criterion-400pct-equivalent-reflow", {
    body: screenshot,
    contentType: "image/png",
  });

  await page.evaluate(() => {
    document.documentElement.style.fontSize = "400%";
  });
  await assertNoUnintendedHorizontalOverflow(page);
  await expect(openEvidence).toBeVisible();
});

test("REVIEW-E2E-06 queued and running registry rows [REVIEW-E2E-06]", async ({ page }) => {
  await page.goto("/");
  await signInAsReviewer(page);
  await page.goto("/review");
  await expect(page.getByRole("heading", { name: "Review work" })).toBeVisible();
  await expect(page.locator("#reviewWorkCountValue")).toHaveText("3 assigned cases");
  await expect(page.getByText("Queued", { exact: true })).toBeVisible();
  await expect(page.getByText("Running", { exact: true })).toBeVisible();
  await expect(page.getByText("Evaluation completed", { exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: /Open review/i })).toHaveCount(1);
});

test("REVIEW-E2E-07 processing well hides Criteria navigation [REVIEW-E2E-07]", async ({ page }) => {
  await page.goto("/");
  await signInAsReviewer(page);
  await openDemoReviewCase(page, demoReviewQueued.caseId);
  await expect(page.getByText(RUNNING_CRITERION_UNAVAILABLE)).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Criteria" })).toHaveCount(0);
  await openDemoReviewCase(page, demoReviewRunning.caseId);
  await expect(page.getByText(RUNNING_CRITERION_UNAVAILABLE)).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Criteria" })).toHaveCount(0);
});
