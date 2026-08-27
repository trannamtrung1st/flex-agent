import type { TranscriptTurn } from "../types";

export const SESSION_OPENING: TranscriptTurn[] = [
  { speaker: "agent", time: "10:12:31", text: "I’ve reviewed your submitted case study, “Real-time Inventory & Order Management at Scale.” Before we dive deeper, is there any context you’d like to highlight?" },
  { speaker: "participant", time: "10:14:02", text: "Yes. The system supports flash sales with highly variable write loads. We prioritized availability and partition tolerance over strong consistency to keep tail latencies predictable." },
  { speaker: "agent", time: "10:15:18", text: "Thanks. In section 3 you justify eventual consistency for the order ledger. What trade-offs did you consider compared to strong or causal consistency?" },
  { speaker: "participant", time: "10:17:43", text: "We considered latency, throughput, availability, and operational complexity. Strong consistency would have increased cross-region latency and reduced availability during network partitions. Causal consistency still didn’t meet our p99 latency SLO under peak load." },
  { speaker: "agent", time: "10:19:27", text: "In section 3 of your submitted case study, you chose eventual consistency for the order ledger. Walk me through how you would detect and reconcile a conflicting write." },
];

export const SESSION_FOLLOWUPS = [
  { text: "Understood. Suppose two regions accept conflicting decrements for the last unit of stock during a partition. What does the customer in each region see before reconciliation completes?", advanceStage: false },
  { text: "Thank you. Last one for this stage: if you could add one safeguard to this design before the next flash sale, what would it be — and what trade-off does it introduce?", advanceStage: true },
  { text: "Stage four — reflection. Looking back at your submission as a whole, which decision are you least confident about, and why?", advanceStage: false },
  { text: "Noted, and recorded. Is there anything you would like to add or clarify before we move on?", advanceStage: false },
];
