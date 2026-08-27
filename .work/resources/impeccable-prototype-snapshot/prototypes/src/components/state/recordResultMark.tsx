import { StateIndicator, StateRing } from "./StateIndicator";

export function recordResultMark(result: string) {
  if (result === "LIVE" || result === "COMPLETE") return <StateRing />;
  if (result === "IN PROGRESS") return <StateIndicator variant="live" solid />;
  return <StateIndicator />;
}
