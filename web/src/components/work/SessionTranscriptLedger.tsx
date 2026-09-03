import type { ReactNode } from "react";
import type { SessionSnapshotTranscriptItemV1 } from "../../contracts/v1";
import { transcriptItemCopy } from "../../features/session/useTranscriptReveal";

export function SessionTranscriptLedger({
  items,
  label,
  copyFor,
  turnState,
  children,
}: {
  items: SessionSnapshotTranscriptItemV1[];
  label: string;
  copyFor?: (item: SessionSnapshotTranscriptItemV1) => string;
  turnState?: (item: SessionSnapshotTranscriptItemV1, index: number) => {
    active?: boolean;
    arriving?: boolean;
  };
  children?: ReactNode;
}) {
  return (
    <ol className="ledger" aria-label={label}>
      {items.map((item, index) => {
        const copy = copyFor?.(item) ?? transcriptItemCopy(item);
        const state = turnState?.(item, index);
        const arriving = Boolean(state?.arriving);
        const active = Boolean(state?.active);
        return (
          <li
            key={item.item_id}
            className={`turn turn--${item.author}${active ? " is-active" : ""}${arriving ? " is-arriving" : ""}`}
          >
            <div className="turn-body-wrap">
              <span className="turn-index turn-index--card-edge" aria-hidden="true">
                {String(index + 1).padStart(2, "0")}
              </span>
              <p className="turn-speaker">{item.author === "agent" ? "Agent" : "Participant"}</p>
              <p className="turn-text">{copy}</p>
              {item.occurred_at ? <p className="turn-time">{item.occurred_at}</p> : null}
            </div>
          </li>
        );
      })}
      {children}
    </ol>
  );
}
