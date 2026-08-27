import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  Announcer,
  BackKey,
  Bulkhead,
  CeremonyDialog,
  CommandStrip,
  ConsoleFoot,
  DataTableShell,
  DemoPlate,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  EmptyPlate,
  EtchedFrame,
  Key,
  OperateHead,
  REVIEWER_HOME,
  REVIEWER_IDENTITY,
  SignOutCeremony,
  StateReadout,
  usePrototypeSignOut,
  type StateIndicatorVariant,
} from "../components";
import { REVIEWER_DEMO_KEYS } from "../data/fixtures/reviewer";
import type { ReviewSession } from "../data/types";
import { ManifestPanel, MarginaliaStack, statusLabel } from "../features/reviewer/RecordPanels";
import { loadReviewerState, persistReviewerState } from "../features/reviewer/storage";
import { useAnnouncer } from "../lib/useAnnouncer";
import { useDemoParam } from "../lib/useDemoParam";
import { maxWidthQuery } from "../lib/breakpoints";
import { useMediaQuery } from "../lib/useMediaQuery";
import { useSurface } from "../lib/useSurface";

function statusIndicatorVariant(status: ReviewSession["reviewStatus"]): StateIndicatorVariant {
  if (status === "released") return "sealed";
  if (status === "escalated" || status === "awaiting" || status === "adjusted") return "live";
  return "dim";
}

export function ReviewerPage() {
  const [demo, setDemo] = useDemoParam(REVIEWER_DEMO_KEYS, "default");
  const [view, setView] = useState<"queue" | "unfolding" | "record">("queue");
  useSurface("reviewer-console", `view-${view === "unfolding" ? "unfolding" : view}`);
  const [sessions, setSessions] = useState(() => loadReviewerState(demo));
  const [activeId, setActiveId] = useState<string | null>(null);
  const [adjustMode, setAdjustMode] = useState(false);
  const [activeCriterionId, setActiveCriterionId] = useState<string | null>(null);
  const [releaseOpen, setReleaseOpen] = useState(false);
  const [manifestOpen, setManifestOpen] = useState(false);
  const [marginaliaOpen, setMarginaliaOpen] = useState(false);
  const isDrawerLayout = useMediaQuery(maxWidthQuery("reviewerDrawer"));
  const [loadedDemo, setLoadedDemo] = useState(demo);
  if (demo !== loadedDemo) {
    setLoadedDemo(demo);
    setSessions(loadReviewerState(demo));
    setActiveId(null);
    setView("queue");
    setAdjustMode(false);
    setManifestOpen(false);
    setMarginaliaOpen(false);
  }
  const gridRef = useRef<HTMLDivElement>(null);
  const ledgerRef = useRef<HTMLOListElement>(null);
  const stackRef = useRef<HTMLDivElement>(null);
  const svgRef = useRef<SVGSVGElement>(null);
  const session = sessions.find((s) => s.id === activeId) ?? null;

  const { message, announce } = useAnnouncer();
  const { actions, signOutOpen, setSignOutOpen } = usePrototypeSignOut();
  const mean = (s: ReviewSession) => {
    const vals = s.criteria.map((c) => c.confidence);
    return vals.length ? vals.reduce((a, b) => a + b, 0) / vals.length : 0;
  };

  const rows = useMemo(() => [...sessions].sort((a, b) => a.receivedSort - b.receivedSort), [sessions]);
  const hotId = rows.find((s) => s.hot && s.reviewStatus === "awaiting")?.id ?? rows.find((s) => s.reviewStatus === "awaiting")?.id;

  const drawTethers = useCallback(() => {
    const svg = svgRef.current;
    const grid = gridRef.current;
    if (!svg || !grid || !session) return;
    if (isDrawerLayout && !marginaliaOpen) {
      svg.replaceChildren();
      return;
    }
    const gridBox = grid.getBoundingClientRect();
    if (gridBox.width < 1) return;
    svg.setAttribute("viewBox", `0 0 ${gridBox.width} ${gridBox.height}`);
    svg.replaceChildren();
    session.criteria.forEach((c) => {
      const plate = stackRef.current?.querySelector<HTMLElement>(`[data-criterion="${c.id}"]`);
      if (!plate) return;
      const plateBox = plate.getBoundingClientRect();
      const startX = plateBox.left - gridBox.left;
      const startY = plateBox.top - gridBox.top + plateBox.height * 0.35;
      c.cites.forEach((turnIndex) => {
        const turn = document.getElementById(`turn-${session.id}-${turnIndex}`);
        if (!turn) return;
        const turnBox = turn.getBoundingClientRect();
        const endX = turnBox.right - gridBox.left;
        const endY = turnBox.top - gridBox.top + turnBox.height * 0.45;
        const midX = (startX + endX) / 2;
        const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
        path.setAttribute("d", `M ${startX} ${startY} C ${midX} ${startY}, ${midX} ${endY}, ${endX} ${endY}`);
        path.setAttribute("class", `tether-line${activeCriterionId === c.id ? " is-active" : ""}`);
        path.dataset.criterion = c.id;
        svg.appendChild(path);
        for (const [cx, cy] of [
          [startX, startY],
          [endX, endY],
        ]) {
          const node = document.createElementNS("http://www.w3.org/2000/svg", "circle");
          node.setAttribute("cx", String(cx));
          node.setAttribute("cy", String(cy));
          node.setAttribute("r", "2.5");
          node.setAttribute("class", `tether-node${activeCriterionId === c.id ? " is-active" : ""}`);
          node.dataset.criterion = c.id;
          svg.appendChild(node);
        }
      });
    });
  }, [session, activeCriterionId, isDrawerLayout, marginaliaOpen]);

  useEffect(() => {
    if (isDrawerLayout) return;
    /* Drawers are mobile-only; reset them when the shell returns to split layout. */
    /* eslint-disable react-hooks/set-state-in-effect */
    setManifestOpen(false);
    setMarginaliaOpen(false);
    /* eslint-enable react-hooks/set-state-in-effect */
  }, [isDrawerLayout]);

  useEffect(() => {
    if (!marginaliaOpen || !isDrawerLayout) return;
    const t = window.setTimeout(() => drawTethers(), 340);
    return () => window.clearTimeout(t);
  }, [drawTethers, isDrawerLayout, marginaliaOpen]);

  useEffect(() => {
    drawTethers();
    const grid = gridRef.current;
    const ledger = document.getElementById("ledgerFrame");
    const scrollRail = stackRef.current?.closest(".marginalia-rail, .bulkhead-body");
    const ro = new ResizeObserver(() => drawTethers());
    if (grid) ro.observe(grid);
    ledger?.addEventListener("scroll", drawTethers);
    scrollRail?.addEventListener("scroll", drawTethers);
    window.addEventListener("resize", drawTethers);
    return () => {
      ro.disconnect();
      ledger?.removeEventListener("scroll", drawTethers);
      scrollRail?.removeEventListener("scroll", drawTethers);
      window.removeEventListener("resize", drawTethers);
    };
  }, [drawTethers, view, isDrawerLayout, marginaliaOpen]);

  const closeDrawers = () => {
    setManifestOpen(false);
    setMarginaliaOpen(false);
  };

  const openRecord = (id: string) => {
    setActiveId(id);
    setAdjustMode(false);
    setActiveCriterionId(null);
    closeDrawers();
    setView("unfolding");
    window.setTimeout(() => setView("record"), 640);
    const s = sessions.find((x) => x.id === id);
    if (s) announce(`Opened evaluation record for ${s.candidate}.`);
  };

  const updateSession = (id: string, patch: (s: ReviewSession) => ReviewSession) => {
    setSessions((list) => {
      const next = list.map((s) => (s.id === id ? patch(s) : s));
      persistReviewerState(demo, next);
      return next;
    });
  };

  const commitAdjustment = () => {
    if (!session) return;
    const stack = stackRef.current;
    let changed = false;
    const nextCriteria = session.criteria.map((c) => {
      const plate = stack?.querySelector(`[data-criterion="${c.id}"]`);
      const scoreInput = plate?.querySelector<HTMLInputElement>("[data-field='score']");
      const rationaleInput = plate?.querySelector<HTMLTextAreaElement>("[data-field='rationale']");
      const newScore = Number(scoreInput?.value);
      const newRationale = rationaleInput?.value.trim() ?? c.rationale;
      if (newScore === c.score && newRationale === c.rationale) return c;
      changed = true;
      return {
        ...c,
        original: c.original ?? { score: c.score, rationale: c.rationale },
        score: Math.min(c.max, Math.max(0, newScore)),
        rationale: newRationale,
      };
    });
    setAdjustMode(false);
    if (isDrawerLayout) setMarginaliaOpen(false);
    if (changed) {
      updateSession(session.id, (s) => ({ ...s, reviewStatus: "adjusted", criteria: nextCriteria }));
      announce("Human revision recorded. Agent original preserved beneath adjustments.");
    }
  };

  return (
    <>
      <CommandStrip
        nav={[{ to: REVIEWER_HOME, label: "Review" }]}
        profile={REVIEWER_IDENTITY}
        actions={actions}
      />
      <div className="shell">
        <section className="queue-view" aria-label="Review docket" hidden={view === "record"}>
          <OperateHead
            className="queue-head"
            title="Review Docket"
            description="Sessions awaiting human review — ranked by receipt time."
          />
          <div className="queue-frame">
            <EtchedFrame className="datatable-frame">
              <DataTableShell
                variant="bodyOnly"
                className="queue-datatable"
                table={
                  <table className="datatable-table manifest">
                    <caption className="visually-hidden">Sessions awaiting human review</caption>
                    <thead>
                      <tr>
                        <th scope="col"><span className="col-head">Candidate</span></th>
                        <th scope="col"><span className="col-head">Campaign</span></th>
                        <th scope="col"><span className="col-head">Assignment</span></th>
                        <th scope="col"><span className="col-head">Received</span></th>
                        <th scope="col"><span className="col-head">Confidence</span></th>
                        <th scope="col"><span className="col-head">State</span></th>
                        <th scope="col"><span className="visually-hidden">Action</span></th>
                      </tr>
                    </thead>
                    <tbody>
                      {rows.map((s) => {
                        const isHot = s.id === hotId && s.reviewStatus === "awaiting";
                        return (
                          <tr key={s.id} className={`datatable-row${isHot ? " is-hot" : ""}`}>
                            <td className="col-candidate cell-id" data-label="Candidate">
                              <button
                                type="button"
                                className="datatable-id"
                                onClick={() => openRecord(s.id)}
                              >
                                {s.candidate}
                              </button>
                            </td>
                            <td className="cell-content" data-label="Campaign">{s.campaign}</td>
                            <td className="col-assignment cell-content" data-label="Assignment">{s.assignment}</td>
                            <td className="col-received cell-content" data-label="Received">{s.received}</td>
                            <td className="col-confidence cell-content" data-label="Confidence">{mean(s).toFixed(2)}</td>
                            <td className="cell-content" data-label="State">
                              <StateReadout
                                variant={statusIndicatorVariant(s.reviewStatus)}
                                solid={s.reviewStatus === "released" || s.reviewStatus === "escalated" || s.reviewStatus === "awaiting" || s.reviewStatus === "adjusted"}
                                label={statusLabel(s.reviewStatus)}
                                className="state-cell"
                                labelClassName="state-label"
                              />
                            </td>
                            <td className="col-action">
                              <Key
                                variant={isHot && s.reviewStatus !== "released" ? "inspect" : "quiet"}
                                onClick={() => openRecord(s.id)}
                              >
                                {s.reviewStatus === "released" ? "View" : isHot ? "Inspect" : "Open"}
                              </Key>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                }
                empty={
                  rows.length === 0 ? (
                    <div className="queue-empty">
                      <EmptyPlate label="Docket clear" note="No sessions are queued for review in this demo state." />
                    </div>
                  ) : undefined
                }
              />
            </EtchedFrame>
          </div>
        </section>

        <section
          className={`record-view${session?.reviewStatus === "released" ? " is-released" : ""}${adjustMode ? " is-adjusting" : ""}`}
          aria-label="Evaluation record"
          hidden={view === "queue"}
        >
          <header className="record-head">
            <BackKey
              label="Docket"
              onClick={() => {
                setView("queue");
                setActiveId(null);
                setAdjustMode(false);
                closeDrawers();
              }}
            />
            <div className="record-head-center">
              <h1 className="record-title">Examination Transcript — The Overlay Ledger</h1>
              <StateReadout variant="sealed" solid label="Sealed" className="sealed-mark" />
            </div>
            <p className="record-session-id">{session?.sessionLabel}</p>
          </header>

          {session ? (
            <div className={`record-grid${isDrawerLayout ? " record-grid--drawer" : ""}`} ref={gridRef}>
              <svg className="tether-layer" ref={svgRef} aria-hidden="true" />
              {isDrawerLayout ? (
                <div className="record-drawer-bar" aria-label="Record panels">
                  <Key
                    pressed={manifestOpen}
                    ariaExpanded={manifestOpen}
                    ariaControls="recordManifestBulkhead"
                    onClick={() => {
                      if (manifestOpen) {
                        setManifestOpen(false);
                        return;
                      }
                      setMarginaliaOpen(false);
                      setManifestOpen(true);
                    }}
                  >
                    Manifest
                  </Key>
                  <Key
                    pressed={marginaliaOpen}
                    ariaExpanded={marginaliaOpen}
                    ariaControls="recordMarginaliaBulkhead"
                    onClick={() => {
                      if (marginaliaOpen) {
                        setMarginaliaOpen(false);
                        return;
                      }
                      setManifestOpen(false);
                      setMarginaliaOpen(true);
                    }}
                  >
                    Marginalia
                    {activeCriterionId ? (
                      <span className="record-drawer-mark" aria-hidden="true" />
                    ) : null}
                  </Key>
                </div>
              ) : (
                <aside className="manifest-rail" aria-label="Session manifest">
                  <ManifestPanel session={session} />
                </aside>
              )}
              <div className="transcript-col">
                <div className="ledger-frame pane pane--tl" id="ledgerFrame" onClick={() => setActiveCriterionId(null)}>
                  <ol className="ledger" ref={ledgerRef} aria-label="Sealed examination transcript">
                    {session.turns.map((t) => {
                      const cited = activeCriterionId
                        ? session.criteria.find((c) => c.id === activeCriterionId)?.cites.includes(t.index ?? "")
                        : false;
                      return (
                        <li
                          key={t.index}
                          className={`turn turn--${t.speaker}${cited ? " is-cited" : ""}${activeCriterionId && !cited ? " is-dimmed" : ""}`}
                          id={`turn-${session.id}-${t.index}`}
                          data-turn={t.index}
                        >
                          <div className="turn-body-wrap">
                            <span className="turn-index turn-index--card-edge" aria-hidden="true">{t.index}</span>
                            <div className="turn-speaker">
                              {t.speaker === "agent" ? "AGENT" : "PARTICIPANT"} <span className="turn-time">{t.time}</span>
                            </div>
                            <p className="turn-text">{t.text}</p>
                          </div>
                        </li>
                      );
                    })}
                  </ol>
                </div>
              </div>
              {!isDrawerLayout ? (
                <aside className="marginalia-rail" aria-label="Criterion evaluations">
                  <MarginaliaStack
                    ref={stackRef}
                    session={session}
                    activeCriterionId={activeCriterionId}
                    onSelectCriterion={setActiveCriterionId}
                    adjustMode={adjustMode}
                  />
                </aside>
              ) : null}
            </div>
          ) : null}

          <footer className="decision-bar">
            <p className="decision-note bar-note">
              {session?.reviewStatus === "released"
                ? "Result released. Record is sealed for audit inspection."
                : session?.reviewStatus === "escalated"
                  ? "Escalated for senior review. Adjust or reject before release."
                  : session?.reviewStatus === "rejected"
                    ? "Evaluation rejected. Participant result will not release from this decision."
                    : "Inspect rationale and evidence before release. Human revision preserves the Agent evaluation."}
            </p>
            <div className="decision-keys">
              <Key
                pressed={adjustMode}
                disabled={session?.reviewStatus === "released"}
                onClick={() => {
                  if (!session || session.reviewStatus === "released") return;
                  if (adjustMode) {
                    commitAdjustment();
                  } else {
                    setAdjustMode(true);
                    if (isDrawerLayout) {
                      setManifestOpen(false);
                      setMarginaliaOpen(true);
                    }
                    announce("Adjustment mode — edit scores and rationale. Agent originals are preserved on save.");
                  }
                }}
              >
                {adjustMode ? "Save adjustment" : "Adjust"}
              </Key>
              <Key disabled={session?.reviewStatus === "released"} onClick={() => session && updateSession(session.id, (s) => ({ ...s, reviewStatus: "rejected" }))}>
                Reject
              </Key>
              <Key disabled={session?.reviewStatus === "released"} onClick={() => session && updateSession(session.id, (s) => ({ ...s, reviewStatus: "escalated" }))}>
                Escalate
              </Key>
              <Key id="releaseBtn" variant="release" disabled={!session || session.reviewStatus === "released" || session.reviewStatus === "rejected"} onClick={() => setReleaseOpen(true)}>
                Approve &amp; Release
              </Key>
            </div>
          </footer>
        </section>
      </div>
      {session && isDrawerLayout ? (
        <>
          <Bulkhead
            id="recordManifestBulkhead"
            open={manifestOpen}
            onClose={() => setManifestOpen(false)}
            side="leading"
            title="Session manifest"
            titleId="recordManifestTitle"
          >
            <ManifestPanel session={session} />
          </Bulkhead>
          <Bulkhead
            id="recordMarginaliaBulkhead"
            open={marginaliaOpen}
            onClose={() => setMarginaliaOpen(false)}
            side="trailing"
            wide={adjustMode}
            title="Criterion Marginalia"
            titleId="recordMarginaliaTitle"
            footer={
              adjustMode ? (
                <>
                  <Key
                    onClick={() => {
                      setAdjustMode(false);
                      setMarginaliaOpen(false);
                    }}
                  >
                    Cancel
                  </Key>
                  <Key onClick={commitAdjustment}>Save adjustment</Key>
                </>
              ) : undefined
            }
          >
            <MarginaliaStack
              ref={stackRef}
              session={session}
              activeCriterionId={activeCriterionId}
              onSelectCriterion={setActiveCriterionId}
              showLabel={false}
              adjustMode={adjustMode}
            />
          </Bulkhead>
        </>
      ) : null}
      <ConsoleFoot note="Synthetic demonstration content — no real participant or evaluation data.">
        <DemoPlate
          id="demoState"
          value={demo}
          onChange={(v) => {
            setDemo(v as typeof demo);
            announce("Demo state loaded.");
          }}
          options={[
            { value: "default", label: "Mixed docket" },
            { value: "busy", label: "Busy docket" },
            { value: "single", label: "Single awaiting review" },
            { value: "empty", label: "Docket clear" },
          ]}
        />
      </ConsoleFoot>
      <CeremonyDialog open={releaseOpen} onClose={() => setReleaseOpen(false)} labelledBy="releaseTitle" id="releaseDialog" variant="release">
        <DialogPlate>
          <DialogPlateHead title="Confirm Release" titleId="releaseTitle" />
          <DialogPlateBody>
            <p>Release makes the Result visible to the participant after audited transition. This action is recorded with reviewer identity, timestamp, and evaluation revision.</p>
            <dl className="dialog-readout">
              <div>
                <dt>Candidate</dt>
                <dd>{session?.candidate}</dd>
              </div>
              <div>
                <dt>Review decision</dt>
                <dd>
                  {session?.reviewStatus === "adjusted"
                    ? "Approve with human revision"
                    : session?.reviewStatus === "rejected"
                      ? "Rejected — release blocked"
                      : session?.reviewStatus === "escalated"
                        ? "Escalated — confirm release"
                        : "Approve unchanged"}
                </dd>
              </div>
            </dl>
          </DialogPlateBody>
          <DialogPlateFooter>
            <Key id="releaseCancel" onClick={() => setReleaseOpen(false)}>Cancel</Key>
            <Key
              variant="release"
              disabled={session?.reviewStatus === "rejected"}
              onClick={() => {
                if (!session || session.reviewStatus === "rejected") return;
                updateSession(session.id, (s) => ({ ...s, reviewStatus: "released" }));
                setAdjustMode(false);
                setReleaseOpen(false);
                announce("Result released. Audited transition recorded.");
              }}
            >
              Release Result
            </Key>
          </DialogPlateFooter>
        </DialogPlate>
      </CeremonyDialog>
      <Announcer message={message} />
      <SignOutCeremony open={signOutOpen} onClose={() => setSignOutOpen(false)} />
    </>
  );
}
