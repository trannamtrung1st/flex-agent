import { useState, type CSSProperties } from "react";
import {
  ActionMenuGlyph,
  Advisory,
  Alert,
  EmptyPlate,
  ErrorSummary,
  IconButton,
  Key,
  SETUP_RESOLVED_NOTE,
  StageBars,
  ToastDock,
  TooltipHost,
  WaitPanel,
  WaitPlate,
  type ToastNotice,
} from "../../../components";
import { GallerySection, Spec } from "./GallerySection";

export function FeedbackSections({
  toasts,
  pushToast,
}: {
  toasts: ToastNotice[];
  pushToast: (notice: Omit<ToastNotice, "id" | "leaving">) => void;
}) {
  const [waiting, setWaiting] = useState(false);

  return (
    <>
      <GallerySection id="toast" title="Toast" note="Instrument slips. ToastDock / ToastHost take placement (bottom-center, bottom-start, bottom-end, top-center, top-start, top-end) plus optional offsetInline and offsetBlock. Production defaults to top-center for now. The dock paints above hull chrome (z-index 75) at the default inset and may cover the strip. Compact widths stretch the dock to viewport inline edges; bottom placements keep offsetBlock above a fixed foot.">
        <div className="spec-row">
          <Spec tag=".toast"><Key id="toastSystemKey" onClick={() => pushToast({ label: "Link", copy: "Submission v2 preserved. Earlier versions remain on record." })}>Fire system slip</Key></Spec>
          <Spec tag=".toast--attention"><Key id="toastAttentionKey" onClick={() => pushToast({ label: "Time warning", copy: "10 minutes remain in this session.", attention: true })}>Fire attention slip</Key></Spec>
        </div>
      </GallerySection>

      <GallerySection id="tooltip" title="Tooltip" note="Shared TooltipHost wraps interactive controls so plaques receive hover and focus-visible even when the inner control is disabled. Move onto the plaque to select and copy its text. Disabled reasons also use persistent aria-describedby text.">
        <div className="spec-row">
          <Spec tag="enabled · hover / :focus-visible"><TooltipHost tip="Frozen at cohort activation"><Key ariaLabel="Harness snapshot">Harness snapshot</Key></TooltipHost></Spec>
          <Spec tag="disabled · aria-describedby + host hover"><Key disabled ariaLabel="Configure campaign" disabledReason="Configuration frozen at activation">Configure campaign</Key></Spec>
          <Spec tag="icon-button · TooltipHost"><IconButton label="More actions" tooltip="More actions"><ActionMenuGlyph /></IconButton></Spec>
        </div>
      </GallerySection>

      <GallerySection id="advisory" title="Advisory" note="A standing notice strip bounded by hairlines. Same two voices as the toast. The leading mark stays with the label row when copy wraps.">
        <Spec wide tag=".advisory"><Advisory label="Record" copy="Configuration frozen at activation. Every participant sits the same examination." /></Spec>
        <Spec wide tag=".advisory--attention"><Advisory attention label="Time warning" copy="10 minutes remain in this session. Unsent replies are not part of the record." /></Spec>
        <Spec tag=".advisory · multiline">
          <div style={{ maxWidth: "280px" }}>
            <Advisory label="Record" copy="Configuration frozen at activation. Every participant sits the same examination." />
          </div>
        </Spec>
        <Spec tag=".advisory--attention · multiline">
          <div style={{ maxWidth: "280px" }}>
            <Advisory attention label="Time warning" copy="10 minutes remain in this session. Unsent replies are not part of the record." />
          </div>
        </Spec>
      </GallerySection>

      <GallerySection id="alert" title="Alert" note="Workspace banner combining an advisory strip with optional body copy. Danger uses alert semantics and the attention voice. Frozen form clusters use the Note (info) strip for shared provenance, not a floating field-hint. The Draft saved specimen is the info skin only — production Save draft receipts use toast, not this banner.">
        <Spec wide tag=".workspace-alert · danger"><Alert variant="danger" title="Request could not be completed">The server could not complete this request. Try again.</Alert></Spec>
        <Spec wide tag=".workspace-alert · status"><Alert variant="info" title="Draft saved">Campaign configuration is stored locally until you check readiness.</Alert></Spec>
        <Spec wide tag=".workspace-alert · info · form provenance"><Alert variant="info" title={SETUP_RESOLVED_NOTE} /></Spec>
      </GallerySection>

      <GallerySection id="error-summary" title="Error summary" note="Named validation summary before fields. Each item may link to the invalid control.">
        <Spec wide tag=".error-summary"><ErrorSummary title="Correct the following" errors={[{ message: "Enter a Campaign title", href: "#campaign-title" }, "Select a source for each required category"]} /></Spec>
      </GallerySection>

      <GallerySection id="empty" title="Empty state" note="The empty state is still an instrument, never bare text.">
        <Spec wide center tag=".empty-plate · label · note"><EmptyPlate label="No assigned sessions" note="Nothing is waiting on you. When an administrator assigns an assessment it will rack into the Open bay." /></Spec>
        <Spec wide tag=".empty-plate--inset + --separated · dashed horizon after seated content"><EmptyPlate inset className="empty-plate--separated" label="No cohort records loaded" note="Frozen configuration at cohort activation is the fairness baseline. This plate will not invent cohort membership or accommodations." /></Spec>
      </GallerySection>

      <GallerySection id="wait" title="Wait & progress" note="Loading is an instrument, never a spinner. Teal is the system voice; amber stays on the current stage only. Skeleton lines are dashed absence — the record that has not arrived. Reduced motion holds the geometry still.">
        <div className="spec-row">
          <Spec tag=".wait-mark"><span className="wait-mark" aria-hidden="true" /></Spec>
          <Spec tag=".wait-mark--lg"><span className="wait-mark wait-mark--lg" aria-hidden="true" /></Spec>
          <Spec tag=".wait-copy · 1.4s pulse"><p className="wait-copy">Composing next question</p></Spec>
        </div>
        <Spec wide tag=".scan-track · .scan-fill · --scan · .scan-readout"><div className="scan-demo"><div className="scan-track" role="progressbar" aria-valuemin={0} aria-valuemax={100} aria-valuenow={62} aria-label="Export progress"><span className="scan-fill" style={{ "--scan": 0.62 } as CSSProperties} /></div><span className="scan-readout">62%</span></div></Spec>
        <Spec wide tag=".stage-bars · .is-done teal · .is-now amber (attention)"><div className="stage-demo"><p className="stage-line">Stage — Examination <span className="stage-count">3 of 5</span></p><StageBars stage={3} total={5} /></div></Spec>
        <Spec wide tag=".wait-plate · wait-mark · scan-track"><WaitPlate label="Retrieving manifest" note="The registry is still arriving. This plate will not invent campaign rows." /></Spec>
        <Spec wide tag=".skel-stack · .skel-line · --skel-w"><div className="skel-stack" aria-hidden="true"><span className="skel-line" /><span className="skel-line" style={{ "--skel-w": "78%" } as CSSProperties} /><span className="skel-line" style={{ "--skel-w": "54%" } as CSSProperties} /></div></Spec>
        <div className="spec-row">
          <Spec tag="click to occupy the key for 2.2s"><Key id="waitDemoKey" waiting={waiting} disabled={waiting} onClick={() => { setWaiting(true); window.setTimeout(() => { setWaiting(false); pushToast({ label: "Manifest", copy: "Enrollments seated. Table is ready." }); }, 2200); }}>{waiting ? "Retrieving" : "Retrieve manifest"}</Key></Spec>
          <Spec tag=".key--transmit.is-waiting · teal occupation"><Key variant="transmit" waiting disabled>Transmit</Key></Spec>
          <Spec tag=".key--open.is-waiting · teal occupation"><Key variant="open" waiting disabled>Open session</Key></Spec>
        </div>
      </GallerySection>

      <GallerySection id="wait-panel" title="Wait panel" note="Inline protected-loading status for occupied keys and toolbars: wait-mark plus polite live region text. Page-level wait lives under Shells → Management loading (#layout-management-loading): CeremonyWait inside CeremonyArea in management chrome, not a bare Spec frame.">
        <Spec tag=".loading-panel · role=status"><WaitPanel label="Loading activities…" /></Spec>
      </GallerySection>
      <ToastDock toasts={toasts} offsetInline="234px" />
    </>
  );
}
