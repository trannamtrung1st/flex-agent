import type { TableSelection } from "../../design-system/patterns/tableSelection";

export type { TableSelection };

export type SortKey = "id" | "campaign" | "stage" | "deadline" | "result";

export type SortSpec = { key: SortKey; dir: "asc" | "desc" };

export type EnrollmentRow = {
  id: string;
  campaign: string;
  stage: string;
  result: string;
  deadline: Date;
  attempt: string;
  duration: string;
  submission: string;
  evidence: string;
};

export type DataTableState = {
  stageFilter: string | null;
  search: string;
  sorts: SortSpec[];
  page: number;
  pageSize: number;
  selection: TableSelection;
  expandedId: string | null;
};

export type HomeEnrollment = {
  bay: "open" | "live" | "pending" | "released";
  campaign: string;
  title: string;
  deadline: string;
  deadlineUtc?: string;
  phase: string;
  record: string;
  mark?: "live" | "seal";
  key?: { kind: "open" | "quiet"; label: string; to: string };
};

export type TranscriptTurn = {
  index?: string;
  speaker: "agent" | "participant";
  time?: string;
  text: string;
};

export type ReviewCriterion = {
  id: string;
  label: string;
  max: number;
  score: number;
  rationale: string;
  confidence: number;
  uncertainty?: string;
  cites: string[];
  original?: { score: number; rationale: string };
};

export type ReviewSession = {
  id: string;
  candidate: string;
  campaign: string;
  assignment: string;
  received: string;
  receivedSort: number;
  sessionLabel: string;
  rubric: string;
  agentRevision: string;
  harnessSnapshot: string;
  submissions: { version: string; label: string; preserved: boolean }[];
  turns: TranscriptTurn[];
  criteria: ReviewCriterion[];
  reviewStatus: "awaiting" | "adjusted" | "approved" | "rejected" | "escalated" | "released";
  hot?: boolean;
};

export type CampaignConfiguration = {
  harness: string;
  agent: string;
  sessionLimit: string;
  timeWarning: string;
  maxAttempts: string;
  cooldown: string;
};

export type Campaign = {
  id: string;
  name: string;
  frozen: boolean;
  config: CampaignConfiguration;
  rows: EnrollmentRow[];
  updatedAt: Date;
};

export type CampaignActivationFilter = "all" | "draft" | "frozen";

export type CampaignRegistrySortKey = "campaign" | "activation" | "enrollments" | "deadline" | "updated";

export type CampaignRegistrySort = { key: CampaignRegistrySortKey; dir: "asc" | "desc" };

export type CampaignRegistryState = {
  search: string;
  activationFilter: CampaignActivationFilter;
  sorts: CampaignRegistrySort[];
  page: number;
  pageSize: number;
  selection: TableSelection;
};

export type CampaignRegistryRow = {
  id: string;
  name: string;
  frozen: boolean;
  enrollments: number;
  deadline: Date | null;
  updatedAt: Date;
};
