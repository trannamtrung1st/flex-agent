import { z } from "zod";
import { MM_SS_PATTERN, MM_SS_WARNING_PLACEHOLDER, mmSsError } from "../../components";

export const campaignSchema = z
  .object({
    harness: z.string(),
    agent: z.string(),
    sessionLimit: z.string().regex(MM_SS_PATTERN, mmSsError("Session limit")),
    timeWarning: z.string().regex(MM_SS_PATTERN, mmSsError("Time warning", MM_SS_WARNING_PLACEHOLDER)),
    maxAttempts: z.string().regex(/^\d{1,2}$/, "Max attempts must be a whole number of at least 1."),
    cooldown: z.string(),
  })
  .superRefine((value, ctx) => {
    const toSec = (v: string) => {
      const [m, s] = v.split(":").map(Number);
      return m * 60 + s;
    };
    if (MM_SS_PATTERN.test(value.sessionLimit) && MM_SS_PATTERN.test(value.timeWarning)) {
      if (toSec(value.timeWarning) >= toSec(value.sessionLimit)) {
        ctx.addIssue({
          code: "custom",
          path: ["timeWarning"],
          message: `Time warning must land before the session limit. Set it below ${value.sessionLimit}.`,
        });
      }
    }
    if (!/^\d{1,2}$/.test(value.maxAttempts) || Number(value.maxAttempts) < 1) {
      ctx.addIssue({ code: "custom", path: ["maxAttempts"], message: "Max attempts must be a whole number of at least 1." });
    }
  });

export type CampaignForm = z.infer<typeof campaignSchema>;
