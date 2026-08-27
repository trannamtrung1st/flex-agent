import type { HomeEnrollment } from "../types";

export const HOME_BAYS = [
  { id: "open", label: "Open" },
  { id: "live", label: "Live" },
  { id: "pending", label: "Result not available" },
  { id: "released", label: "Released" },
] as const;

export const HOME_ENROLLMENTS: HomeEnrollment[] = [
  {
    bay: "open",
    campaign: "Systems Design Q3",
    title: "Real-time Inventory & Order Management at Scale",
    deadline: "12 SEP 18:00",
    deadlineUtc: "2026-09-12T23:00:00Z",
    phase: "Briefing",
    record: "Open",
    key: { kind: "open", label: "Open", to: "/participant-journey" },
  },
  {
    bay: "live",
    campaign: "Architecture Critique",
    title: "Distributed Lock Service",
    deadline: "—",
    phase: "Examination",
    record: "Live",
    mark: "live",
    key: { kind: "quiet", label: "Return", to: "/participant-session" },
  },
  {
    bay: "pending",
    campaign: "Network Failure Case",
    title: "Failover Strategy Analysis",
    deadline: "—",
    phase: "Result",
    record: "Result not available",
    key: { kind: "quiet", label: "Return", to: "/participant-home" },
  },
  {
    bay: "released",
    campaign: "Cache Coherence Lab",
    title: "MESI Protocol Walkthrough",
    deadline: "—",
    phase: "Result",
    record: "Released",
    mark: "seal",
    key: { kind: "quiet", label: "View", to: "/participant-journey?demo=result-released" },
  },
];

export const HOME_CROWDED: HomeEnrollment[] = [
  HOME_ENROLLMENTS[0],
  {
    bay: "open",
    campaign: "API Design Review",
    title: "Rate Limiter Contract & Error Semantics",
    deadline: "19 SEP 12:00",
    deadlineUtc: "2026-09-19T17:00:00Z",
    phase: "Briefing",
    record: "Open",
  },
  {
    bay: "open",
    campaign: "Storage Deep Dive",
    title: "Write-Ahead Log Recovery Guarantees",
    deadline: "26 SEP 18:00",
    deadlineUtc: "2026-09-26T23:00:00Z",
    phase: "Briefing",
    record: "Open",
  },
  HOME_ENROLLMENTS[1],
  HOME_ENROLLMENTS[2],
  {
    bay: "pending",
    campaign: "Consensus Protocols",
    title: "Raft Leader Election Edge Cases",
    deadline: "—",
    phase: "Result",
    record: "Result not available",
  },
  HOME_ENROLLMENTS[3],
  {
    bay: "released",
    campaign: "Queueing Theory Lab",
    title: "Backpressure Strategy Evaluation",
    deadline: "—",
    phase: "Result",
    record: "Released",
    mark: "seal",
    key: { kind: "quiet", label: "View", to: "/participant-journey?demo=result-released" },
  },
];

export const HOME_DEMO = {
  populated: HOME_ENROLLMENTS,
  crowded: HOME_CROWDED,
  single: HOME_ENROLLMENTS.filter((e) => e.bay === "open"),
  empty: [] as HomeEnrollment[],
};

export const HOME_DEMO_KEYS = ["populated", "crowded", "single", "empty"] as const;
