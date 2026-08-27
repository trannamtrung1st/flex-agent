import { REVIEWER_DEMOS, REVIEWER_STORAGE_KEY } from "../../data/fixtures/reviewer";
import type { ReviewSession } from "../../data/types";

export function cloneSessions(list: ReviewSession[]): ReviewSession[] {
  return list.map((s) => ({
    ...s,
    criteria: s.criteria.map((c) => ({ ...c, original: c.original ? { ...c.original } : undefined })),
    submissions: s.submissions.map((x) => ({ ...x })),
    turns: s.turns.map((t) => ({ ...t })),
  }));
}

export function loadReviewerState(demoKey: string): ReviewSession[] {
  const base = cloneSessions(REVIEWER_DEMOS[demoKey as keyof typeof REVIEWER_DEMOS] ?? REVIEWER_DEMOS.default);
  try {
    const raw = localStorage.getItem(REVIEWER_STORAGE_KEY);
    if (!raw) return base;
    const saved = JSON.parse(raw) as {
      demoKey?: string;
      sessions?: Record<string, { reviewStatus?: ReviewSession["reviewStatus"]; criteria?: Record<string, { score?: number; rationale?: string; original?: { score: number; rationale: string } }> }>;
    };
    if (!saved || typeof saved !== "object" || saved.demoKey !== demoKey) return base;
    if (!saved.sessions || typeof saved.sessions !== "object") return base;
    return base.map((s) => {
      const patch = saved.sessions?.[s.id];
      if (!patch) return s;
      return {
        ...s,
        reviewStatus: patch.reviewStatus ?? s.reviewStatus,
        criteria: s.criteria.map((c) => {
          const cp = patch.criteria?.[c.id];
          if (!cp) return c;
          return { ...c, score: cp.score ?? c.score, rationale: cp.rationale ?? c.rationale, original: cp.original ?? c.original };
        }),
      };
    });
  } catch {
    return base;
  }
}

export function persistReviewerState(demoKey: string, sessions: ReviewSession[]) {
  const payload = {
    demoKey,
    sessions: Object.fromEntries(
      sessions.map((s) => [
        s.id,
        {
          reviewStatus: s.reviewStatus,
          criteria: Object.fromEntries(s.criteria.map((c) => [c.id, { score: c.score, rationale: c.rationale, original: c.original }])),
        },
      ]),
    ),
  };
  localStorage.setItem(REVIEWER_STORAGE_KEY, JSON.stringify(payload));
}
