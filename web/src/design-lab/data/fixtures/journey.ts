export const JOURNEY_PHASES = [
  { id: "briefing", label: "Briefing", short: "Rules & consent", statusPhase: "Briefing", statusRecord: "Open" },
  { id: "submission", label: "Submission", short: "Upload work", statusPhase: "Submission", statusRecord: "Open" },
  { id: "examination", label: "Examination", short: "Text session", statusPhase: "Examination", statusRecord: "Active" },
  { id: "result", label: "Result", short: "After publication", statusPhase: "Result", statusRecord: "Result not available" },
] as const;

export const JOURNEY_DEMOS = {
  briefing: { completed: [], current: "briefing", examination: "locked", result: "locked", record: "Open" },
  submission: { completed: ["briefing"], current: "submission", examination: "locked", result: "locked", record: "Open" },
  "examination-ready": {
    completed: ["briefing", "submission"],
    current: "examination",
    examination: "ready",
    result: "locked",
    record: "Ready",
  },
  "examination-active": {
    completed: ["briefing", "submission"],
    current: "examination",
    examination: "active",
    result: "locked",
    record: "In session",
  },
  "result-pending": {
    completed: ["briefing", "submission", "examination"],
    current: "result",
    examination: "complete",
    result: "pending",
    record: "Result not available",
  },
  "result-released": {
    completed: ["briefing", "submission", "examination"],
    current: "result",
    examination: "complete",
    result: "released",
    record: "Released",
  },
} as const;

export type JourneyDemo = keyof typeof JOURNEY_DEMOS;
export const JOURNEY_DEMO_KEYS = Object.keys(JOURNEY_DEMOS) as JourneyDemo[];
