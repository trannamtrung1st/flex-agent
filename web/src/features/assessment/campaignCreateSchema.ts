import { z } from "zod";
import { REQUIRED_SOURCE_CATEGORIES } from "../../api/production-assessment";
import { sourceCategoryLabel } from "./campaignCreatePresentation";

const sourceShape = Object.fromEntries(
  REQUIRED_SOURCE_CATEGORIES.map((category) => [
    category,
    z.string().min(1, `Select a source for ${sourceCategoryLabel(category)}`),
  ]),
) as Record<(typeof REQUIRED_SOURCE_CATEGORIES)[number], z.ZodString>;

export const campaignCreateSchema = z.object({
  title: z.string()
    .min(1, "Enter a Campaign title")
    .max(200, "Campaign title must be 200 characters or fewer"),
  sources: z.object(sourceShape),
});

export type CampaignCreateValues = z.infer<typeof campaignCreateSchema>;

export const emptyCampaignCreateValues: CampaignCreateValues = {
  title: "",
  sources: Object.fromEntries(REQUIRED_SOURCE_CATEGORIES.map((category) => [category, ""])) as CampaignCreateValues["sources"],
};
