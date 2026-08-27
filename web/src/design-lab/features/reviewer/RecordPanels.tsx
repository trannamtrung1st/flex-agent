import { forwardRef } from "react";
import { FieldInput, FieldTextarea, FormField, ReadoutList } from "../../components";
import type { ReviewSession } from "../../data/types";

export function statusLabel(status: ReviewSession["reviewStatus"]) {
  switch (status) {
    case "awaiting":
      return "Awaiting review";
    case "adjusted":
      return "Adjusted";
    case "approved":
      return "Approved";
    case "rejected":
      return "Rejected";
    case "escalated":
      return "Escalated";
    case "released":
      return "Released";
    default:
      return status;
  }
}

export function ManifestPanel({ session }: { session: ReviewSession }) {
  return (
    <>
      <ReadoutList
        className="rail-readout"
        rowClassName=""
        rows={[
          { term: "Participant", value: session.candidate },
          { term: "Campaign", value: session.campaign },
          { term: "Rubric", value: session.rubric },
          { term: "Agent Revision", value: session.agentRevision },
          { term: "Harness Snapshot", value: session.harnessSnapshot },
          { term: "Review State", value: statusLabel(session.reviewStatus) },
        ]}
      />
      <section className="submission-block" aria-label="Preserved submissions">
        <h2 className="rail-section-label">Submissions</h2>
        <ul className="submission-list">
          {session.submissions.map((sub) => (
            <li className="submission-item" key={sub.version}>
              <svg
                className={`doc-glyph ${sub.preserved ? "doc-glyph--current" : "doc-glyph--dim"}`}
                viewBox="0 0 13 15"
                aria-hidden="true"
              >
                <path d="M2 1h7l2 2v11H2z" fill="none" stroke="currentColor" strokeWidth="1.1" />
                <path d="M9 1v3h3" fill="none" stroke="currentColor" strokeWidth="1.1" />
              </svg>
              <div>
                <strong>{sub.version} preserved</strong>
                {sub.label}
              </div>
            </li>
          ))}
        </ul>
      </section>
    </>
  );
}

type MarginaliaStackProps = {
  session: ReviewSession;
  activeCriterionId: string | null;
  onSelectCriterion: (id: string) => void;
  showLabel?: boolean;
  adjustMode?: boolean;
};

export const MarginaliaStack = forwardRef<HTMLDivElement, MarginaliaStackProps>(function MarginaliaStack(
  { session, activeCriterionId, onSelectCriterion, showLabel = true, adjustMode = false },
  ref,
) {
  return (
    <>
      {showLabel ? <h2 className="marginalia-label">Criterion Marginalia</h2> : null}
      <div className="marginalia-stack" ref={ref}>
        {session.criteria.map((c) => {
          const isInteractive = !adjustMode;
          return (
          <article
            key={c.id}
            className={`marginalia-plate pane pane--dim${activeCriterionId === c.id ? " is-active" : ""}${adjustMode ? " is-editing" : ""}`}
            data-criterion={c.id}
            tabIndex={isInteractive ? 0 : undefined}
            role={isInteractive ? "button" : undefined}
            aria-label={isInteractive ? `${c.label} evaluation` : undefined}
            onClick={isInteractive ? () => onSelectCriterion(c.id) : undefined}
            onKeyDown={
              isInteractive
                ? (e) => {
                    if (e.key === "Enter" || e.key === " ") {
                      e.preventDefault();
                      onSelectCriterion(c.id);
                    }
                  }
                : undefined
            }
          >
            <div className="marginalia-head">
              <span className="marginalia-criterion">{c.label}</span>
              <span className="marginalia-score">
                {c.score}/{c.max}
              </span>
            </div>
            <p className="marginalia-link">{c.cites.map((i) => `Linked to ${i}`).join(" · ")}</p>
            <p className="marginalia-rationale">{c.rationale}</p>
            <p className={`marginalia-confidence${c.confidence < 0.7 ? " is-low" : ""}`}>
              Confidence {c.confidence.toFixed(2)}
            </p>
            {c.uncertainty ? (
              <p className="marginalia-confidence is-low">Uncertainty — {c.uncertainty}</p>
            ) : null}
            {c.original ? (
              <dl className="marginalia-original">
                <dt>Agent original</dt>
                <dd>
                  Score {c.original.score}/{c.max} — {c.original.rationale}
                </dd>
              </dl>
            ) : null}
            <div className={`marginalia-adjust field-group${adjustMode ? " is-open" : ""}`}>
              <FormField id={`score-${c.id}`} label="Adjusted score" className="field-stack">
                {(controlProps) => (
                  <FieldInput
                    {...controlProps}
                    width="narrow"
                    type="text"
                    inputMode="numeric"
                    defaultValue={c.score}
                    data-field="score"
                  />
                )}
              </FormField>
              <FormField id={`rationale-${c.id}`} label="Adjusted rationale" className="field-stack">
                {(controlProps) => (
                  <FieldTextarea
                    {...controlProps}
                    rows={3}
                    defaultValue={c.rationale}
                    data-field="rationale"
                  />
                )}
              </FormField>
            </div>
          </article>
          );
        })}
      </div>
    </>
  );
});
