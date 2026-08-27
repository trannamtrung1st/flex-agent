import type { Campaign, CampaignConfiguration, EnrollmentRow } from "../types";

const STAGES = ["BRIEFING", "SUBMISSION", "EXAMINATION", "REVIEW", "RELEASED"];

const DEFAULT_CONFIG: CampaignConfiguration = {
  harness: "GOVERNED-EXAM-01",
  agent: "EXAMINER-CORE",
  sessionLimit: "60:00",
  timeWarning: "10:00",
  maxAttempts: "2",
  cooldown: "24H",
};

function campaignConfig(overrides: Partial<CampaignConfiguration> = {}): CampaignConfiguration {
  return { ...DEFAULT_CONFIG, ...overrides };
}

const PINNED: [string, string][] = [
  ["BRIEFING", "PENDING"], ["SUBMISSION", "PENDING"], ["EXAMINATION", "IN PROGRESS"],
  ["REVIEW", "PENDING"], ["BRIEFING", "PENDING"], ["EXAMINATION", "READY"],
  ["SUBMISSION", "PENDING"], ["EXAMINATION", "LIVE"], ["SUBMISSION", "PENDING"],
  ["BRIEFING", "PENDING"], ["REVIEW", "READY"], ["BRIEFING", "PENDING"],
  ["SUBMISSION", "PENDING"], ["EXAMINATION", "IN PROGRESS"], ["BRIEFING", "PENDING"],
  ["REVIEW", "READY"], ["SUBMISSION", "PENDING"], ["BRIEFING", "PENDING"],
  ["EXAMINATION", "LIVE"], ["BRIEFING", "PENDING"], ["SUBMISSION", "PENDING"],
  ["REVIEW", "READY"], ["BRIEFING", "PENDING"], ["SUBMISSION", "PENDING"],
  ["REVIEW", "READY"], ["BRIEFING", "PENDING"], ["SUBMISSION", "PENDING"],
  ["RELEASED", "COMPLETE"], ["BRIEFING", "PENDING"], ["BRIEFING", "PENDING"],
];

function makeCohort(
  campaignId: string,
  count: number,
  startNum: number,
  start = new Date("2026-08-28T09:00:00"),
): EnrollmentRow[] {
  const rows: EnrollmentRow[] = [];
  for (let i = 0; i < count; i++) {
    const [stage, result] = i < PINNED.length
      ? PINNED[i]
      : (() => {
          const stage = STAGES[(i * 7 + 3) % STAGES.length];
          const result =
            stage === "RELEASED" ? "COMPLETE" :
            stage === "EXAMINATION" ? ["READY", "IN PROGRESS", "LIVE"][i % 3] :
            stage === "REVIEW" ? (i % 2 ? "READY" : "PENDING") : "PENDING";
          return [stage, result] as [string, string];
        })();
    const deadline = new Date(start.getTime() + i * 15 * 60000);
    rows.push({
      id: `P-${startNum + i}`,
      campaign: campaignId,
      stage,
      result,
      deadline,
      attempt: "1 OF 2",
      duration: result === "LIVE" ? "42:11" : result === "IN PROGRESS" ? "17:36" : "—",
      submission: stage === "BRIEFING" ? "NONE" : `V${(i % 3) + 1} PRESERVED`,
      evidence:
        stage === "RELEASED" || stage === "REVIEW"
          ? `${(i % 9) + 8} ITEMS`
          : stage === "EXAMINATION"
            ? `${(i % 7) + 4} ITEMS`
            : "—",
    });
  }
  return rows;
}

const EXTRA: {
  id: string;
  name: string;
  frozen: boolean;
  count: number;
  startNum: number;
  start: string;
  updatedAt: string;
  config?: Partial<CampaignConfiguration>;
}[] = [
  { id: "CMP-0045", name: "Fleet Readiness", frozen: true, count: 22, startNum: 6101, start: "2026-07-14T08:00:00", updatedAt: "2026-07-14T11:40:00", config: { harness: "GOVERNED-EXAM-01", sessionLimit: "50:00" } },
  { id: "CMP-0046", name: "Signal Discipline", frozen: false, count: 18, startNum: 6201, start: "2026-09-02T09:00:00", updatedAt: "2026-08-21T16:05:00" },
  { id: "CMP-0047", name: "Dockside Protocol", frozen: false, count: 14, startNum: 6301, start: "2026-09-08T10:30:00", updatedAt: "2026-08-22T09:12:00", config: { maxAttempts: "3", cooldown: "12H" } },
  { id: "CMP-0048", name: "Cargo Integrity", frozen: true, count: 26, startNum: 6401, start: "2026-06-30T08:15:00", updatedAt: "2026-06-30T18:00:00", config: { harness: "GOVERNED-AUDIT-01", agent: "EXAMINER-STRUCT" } },
  { id: "CMP-0049", name: "Watch Rotation", frozen: false, count: 12, startNum: 6501, start: "2026-09-12T07:00:00", updatedAt: "2026-08-23T13:44:00" },
  { id: "CMP-0050", name: "Nav Recertification", frozen: true, count: 20, startNum: 6601, start: "2026-05-18T09:00:00", updatedAt: "2026-05-19T08:20:00", config: { sessionLimit: "75:00", timeWarning: "15:00" } },
  { id: "CMP-0051", name: "Comms Drill", frozen: false, count: 16, startNum: 6701, start: "2026-09-16T11:00:00", updatedAt: "2026-08-24T10:08:00" },
  { id: "CMP-0052", name: "Hull Survey", frozen: false, count: 10, startNum: 6801, start: "2026-09-20T08:45:00", updatedAt: "2026-08-19T17:30:00", config: { harness: "GOVERNED-EXAM-02" } },
  { id: "CMP-0053", name: "Reactor Walkthrough", frozen: true, count: 24, startNum: 6901, start: "2026-04-22T09:00:00", updatedAt: "2026-04-22T20:10:00", config: { agent: "EXAMINER-OPS", maxAttempts: "1" } },
  { id: "CMP-0054", name: "Berth Assignment", frozen: false, count: 8, startNum: 7001, start: "2026-09-24T12:00:00", updatedAt: "2026-08-25T08:55:00" },
  { id: "CMP-0055", name: "Evacuation Brief", frozen: false, count: 15, startNum: 7101, start: "2026-09-04T14:00:00", updatedAt: "2026-08-18T12:22:00", config: { timeWarning: "05:00", sessionLimit: "30:00" } },
  { id: "CMP-0056", name: "Quarterly Audit", frozen: true, count: 28, startNum: 7201, start: "2026-03-11T09:00:00", updatedAt: "2026-03-12T07:45:00", config: { harness: "GOVERNED-AUDIT-01" } },
  { id: "CMP-0057", name: "Sensor Calibration", frozen: false, count: 11, startNum: 7301, start: "2026-09-28T09:30:00", updatedAt: "2026-08-20T15:18:00" },
  { id: "CMP-0058", name: "Airlock Procedure", frozen: false, count: 13, startNum: 7401, start: "2026-10-02T08:00:00", updatedAt: "2026-08-17T11:02:00", config: { cooldown: "48H" } },
  { id: "CMP-0059", name: "Command Ethics", frozen: true, count: 19, startNum: 7501, start: "2026-02-08T10:00:00", updatedAt: "2026-02-09T09:00:00", config: { agent: "EXAMINER-CORE", maxAttempts: "1" } },
  { id: "CMP-0060", name: "Damage Control", frozen: false, count: 17, startNum: 7601, start: "2026-10-06T07:30:00", updatedAt: "2026-08-16T19:40:00" },
  { id: "CMP-0061", name: "Pilot Recert", frozen: false, count: 9, startNum: 7701, start: "2026-10-10T09:00:00", updatedAt: "2026-08-15T14:27:00", config: { sessionLimit: "90:00", timeWarning: "12:00" } },
];

export function createCampaigns(): Campaign[] {
  const primary = makeCohort("CMP-0042", 120, 3114);
  Object.assign(primary[7], { submission: "V3 PRESERVED", evidence: "14 ITEMS" });
  return [
    { id: "CMP-0042", name: "Structural Audit Q3", frozen: false, config: campaignConfig(), rows: primary, updatedAt: new Date("2026-08-24T18:12:00") },
    {
      id: "CMP-0043",
      name: "Ops Integrity",
      frozen: false,
      config: campaignConfig({ harness: "GOVERNED-EXAM-02", agent: "EXAMINER-OPS", sessionLimit: "45:00", timeWarning: "08:00" }),
      rows: makeCohort("CMP-0043", 64, 4201, new Date("2026-08-30T09:00:00")),
      updatedAt: new Date("2026-08-23T11:06:00"),
    },
    {
      id: "CMP-0044",
      name: "Access Review",
      frozen: false,
      config: campaignConfig({ harness: "GOVERNED-AUDIT-01", agent: "EXAMINER-STRUCT", maxAttempts: "1", cooldown: "48H" }),
      rows: makeCohort("CMP-0044", 38, 5307, new Date("2026-09-01T08:00:00")),
      updatedAt: new Date("2026-08-22T07:33:00"),
    },
    ...EXTRA.map((spec) => ({
      id: spec.id,
      name: spec.name,
      frozen: spec.frozen,
      config: campaignConfig(spec.config),
      rows: makeCohort(spec.id, spec.count, spec.startNum, new Date(spec.start)),
      updatedAt: new Date(spec.updatedAt),
    })),
  ];
}

export const ADMIN_STAGES = STAGES;

export function createGalleryRows(): EnrollmentRow[] {
  const campaigns = ["CMP-0042", "CMP-0043", "CMP-0044", "CMP-0045", "CMP-0046"];
  const cohortStart = new Date("2026-08-28T09:00:00");
  const rows: EnrollmentRow[] = [];
  for (let i = 0; i < 100; i++) {
    const stage = STAGES[(i * 7 + 3) % STAGES.length];
    const result =
      stage === "RELEASED" ? "COMPLETE" :
      stage === "EXAMINATION" ? ["READY", "IN PROGRESS", "LIVE"][i % 3] :
      stage === "REVIEW" ? (i % 2 ? "READY" : "PENDING") : "PENDING";
    rows.push({
      id: `P-${3114 + i}`,
      campaign: campaigns[i % campaigns.length],
      stage,
      result,
      deadline: new Date(cohortStart.getTime() + i * 18 * 60000),
      attempt: `${(i % 2) + 1} OF 2`,
      duration:
        result === "LIVE" ? `${String((i % 50) + 10).padStart(2, "0")}:${String(i % 60).padStart(2, "0")}` :
        result === "IN PROGRESS" ? `${String((i % 40) + 5).padStart(2, "0")}:${String(i % 60).padStart(2, "0")}` : "—",
      submission: stage === "BRIEFING" ? "NONE" : `V${(i % 3) + 1} PRESERVED`,
      evidence:
        stage === "RELEASED" || stage === "REVIEW"
          ? `${(i % 9) + 8} ITEMS`
          : stage === "EXAMINATION"
            ? `${(i % 7) + 4} ITEMS`
            : "—",
    });
  }
  return rows;
}
