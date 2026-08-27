import { SESSION_FOLLOWUPS, SESSION_OPENING } from "../../data/fixtures/session";
import type { TranscriptTurn } from "../../data/types";

export type TurnView = TranscriptTurn & { thinking?: boolean; arriving?: boolean; active?: boolean };

export type SessionModel = {
  briefing: boolean;
  remaining: number;
  warned: boolean;
  complete: boolean;
  stage: number;
  turns: TurnView[];
  thinking: boolean;
  busy: boolean;
  followUpIdx: number;
  feed: { t: string; text: string; amber?: boolean }[];
  composer: string;
  confirm: boolean;
  clock: number;
  dismissed: boolean;
};

export const WARN_AT = 40 * 60;

export type SessionAction =
  | { type: "tick" }
  | { type: "begin" }
  | { type: "compose"; value: string }
  | { type: "transmit" }
  | { type: "agent-start" }
  | { type: "agent-done" }
  | { type: "open-confirm"; open: boolean }
  | { type: "complete" }
  | { type: "clear-warn" };

function stamp(clock: number) {
  return new Date(clock).toTimeString().slice(0, 5);
}

export function initialSessionModel(stateParam: string | null): SessionModel {
  const briefing = !stateParam || stateParam === "briefing";
  return {
    briefing,
    remaining: stateParam === "warned" ? WARN_AT + 2 : 41 * 60 + 17,
    warned: false,
    complete: stateParam === "complete",
    stage: stateParam === "complete" ? 5 : 3,
    turns: SESSION_OPENING.map((t, i) => ({ ...t, active: i === SESSION_OPENING.length - 1 })),
    thinking: false,
    busy: false,
    followUpIdx: 0,
    feed: [
      { t: "10:19", text: "TURN 05 AWAITING PARTICIPANT" },
      { t: "10:19", text: "INVOCATION 0009 — DECISION PUBLISHED" },
      { t: "10:19", text: "SESSION 07 RESUMED — LINK NOMINAL" },
    ],
    composer: "",
    confirm: false,
    clock: new Date().setHours(10, 19, 27, 0),
    dismissed: !briefing,
  };
}

export function sessionReducer(state: SessionModel, action: SessionAction): SessionModel {
  switch (action.type) {
    case "tick":
      if (state.complete || state.briefing) return state;
      {
        const remaining = Math.max(0, state.remaining - 1);
        const warned = state.warned || remaining <= WARN_AT;
        return { ...state, remaining, warned };
      }
    case "begin":
      return {
        ...state,
        briefing: false,
        dismissed: true,
        feed: [{ t: stamp(state.clock), text: "RULES ACKNOWLEDGED — EXAMINATION LIVE", amber: true }, ...state.feed].slice(0, 5),
      };
    case "compose":
      return { ...state, composer: action.value };
    case "transmit": {
      if (state.busy || state.complete || !state.composer.trim()) return state;
      const clock = state.clock + 45 * 1000;
      const turns: TurnView[] = [
        ...state.turns.map((t) => ({ ...t, active: false })),
        {
          speaker: "participant",
          time: new Date(clock).toTimeString().slice(0, 8),
          text: state.composer.trim(),
        },
      ];
      return {
        ...state,
        clock,
        turns,
        composer: "",
        feed: [{ t: new Date(clock).toTimeString().slice(0, 5), text: `REPLY RECORDED — TURN ${String(turns.filter((t) => !t.thinking).length).padStart(2, "0")}` }, ...state.feed].slice(0, 5),
      };
    }
    case "agent-start":
      return {
        ...state,
        busy: true,
        thinking: true,
        turns: [...state.turns, { speaker: "agent", text: "Examiner is considering…", thinking: true }],
      };
    case "agent-done": {
      const next = SESSION_FOLLOWUPS[Math.min(state.followUpIdx, SESSION_FOLLOWUPS.length - 1)];
      const followUpIdx = state.followUpIdx + 1;
      let stage = state.stage;
      const clock = state.clock + 45 * 1000;
      if (next.advanceStage && stage < 5) stage += 1;
      const turns = state.turns
        .filter((t) => !t.thinking)
        .concat({
          speaker: "agent",
          time: new Date(clock).toTimeString().slice(0, 8),
          text: next.text,
          active: true,
          arriving: true,
        });
      return {
        ...state,
        busy: false,
        thinking: false,
        followUpIdx,
        stage,
        clock,
        turns,
        feed: [
          { t: new Date(clock).toTimeString().slice(0, 5), text: `INVOCATION ${String(9 + followUpIdx).padStart(4, "0")} — DECISION PUBLISHED` },
          ...(next.advanceStage ? [{ t: new Date(clock).toTimeString().slice(0, 5), text: `STAGE ADVANCED — ${stage} OF 5` }] : []),
          ...state.feed,
        ].slice(0, 5),
      };
    }
    case "open-confirm":
      return { ...state, confirm: action.open };
    case "complete":
      return {
        ...state,
        complete: true,
        confirm: false,
        busy: false,
        thinking: false,
        stage: 5,
        turns: state.turns.map((t) => ({ ...t, active: false, thinking: false })),
        feed: [{ t: stamp(state.clock), text: "SESSION SEALED — AWAITING HUMAN REVIEW", amber: true }, ...state.feed].slice(0, 5),
      };
    case "clear-warn":
      return { ...state, warned: state.remaining <= WARN_AT ? state.warned : false };
    default:
      return state;
  }
}
