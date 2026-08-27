import { ActionMenuGlyph, EtchedFrame, IconButton, Key } from "../../../components";
import { GallerySection, Spec } from "./GallerySection";

const colors = [
  ["ground", "#07141b"], ["ground-deep", "#041018"], ["ground-sheen", "#0e1c24"],
  ["text", "#e6eef2"], ["text-bright", "#f8fcfe"], ["label", "#a8c4ca"],
  ["label-dim", "#88a8b0"], ["hairline", "rgba(110,154,156,0.52)"],
  ["hairline-dim", "rgba(110,154,156,0.28)"], ["teal", "#3cc0bf"],
  ["teal-glow", "rgba(60,192,191,0.14)"], ["amber", "#e2a33c"],
  ["amber-bright", "#edc890"], ["amber-glow", "rgba(226,163,60,0.18)"],
] as const;

export function FoundationsSections() {
  return (
    <>
      <GallerySection id="colors" title="Colors" note={<>Two structural teal voices, one rationed amber, nothing else. Tokens live in <span className="code">tokens.css</span>.</>}>
        <ul className="chip-row">
          {colors.map(([name, color]) => (
            <li className="chip" key={name}>
              <span className="chip-swatch" style={{ background: color }} />
              <span className="chip-name">{name}</span>
              <span className="chip-value">{color}</span>
            </li>
          ))}
        </ul>
      </GallerySection>

      <GallerySection id="type" title="Type voices" note="Michroma placards name; Sometype Mono speaks. Every changing number is tabular.">
        <div className="type-specimens">
          <Spec tag="placard · Michroma 400 · 0.24em tracking · uppercase"><span className="type-placard">Examination Console</span></Spec>
          <Spec tag="display · mono 600 · tabular-nums · amber glow"><span className="type-display">00:41:17</span></Spec>
          <Spec tag="body · mono 400 · 15px · lh 1.55 · max 78ch"><p className="type-body">The Examiner — an AI agent operating under a frozen configuration — will ask follow-up questions about your work.</p></Spec>
          <Spec tag="microlabel · mono 400 · 0.14em tracking · uppercase"><span className="type-microlabel">Session Duration</span></Spec>
        </div>
      </GallerySection>

      <GallerySection id="keys" title="Keys" note={<>Engraved console keys: square corners, hairline bezels, no fills at rest. Hot keys cut a 14px leading edge and are rationed to one per live surface. Use <span className="code">size="compact"</span> for dense table toolbars; standard is the default form scale; large is ceremony emphasis.</>}>
        <div className="spec-row">
          <Spec tag={'size="compact" · .key--compact'}><Key size="compact">Compact</Key></Spec>
          <Spec tag={'size="standard" · .key'}><Key>Standard</Key></Spec>
          <Spec tag={'size="large" · .key--large'}><Key size="large">Large</Key></Spec>
        </div>
        <div className="spec-row">
          <Spec tag=".key .key--quiet"><Key>Quiet key</Key></Spec>
          <Spec tag=".icon-button · TooltipHost"><IconButton label="More actions" tooltip="More actions"><ActionMenuGlyph /></IconButton></Spec>
          <Spec tag=":disabled"><Key disabled>Disabled</Key></Spec>
          <Spec tag="disabledReason · aria-describedby"><Key disabled disabledReason="Configuration frozen at activation">Disabled with reason</Key></Spec>
          <Spec tag=".is-waiting · wait-mark"><Key waiting disabled>Retrieving</Key></Spec>
        </div>
        <div className="spec-row">
          <Spec tag=".key--transmit · compact"><Key variant="transmit" size="compact">Transmit</Key></Spec>
          <Spec tag=".key--transmit · standard"><Key variant="transmit">Transmit</Key></Spec>
          <Spec tag=".key--transmit · large"><Key variant="transmit" size="large">Transmit</Key></Spec>
        </div>
        <div className="spec-row">
          <Spec tag=".key--open"><Key variant="open">Open session</Key></Spec>
          <Spec tag=".key--inspect"><Key variant="inspect">Inspect</Key></Spec>
          <Spec tag=".key--release"><Key variant="release">Approve &amp; release</Key></Spec>
          <Spec tag=".key--begin · pre-lit"><Key variant="begin">Begin examination</Key></Spec>
          <Spec tag=".key--activate · double stroke"><Key variant="activate">Activate</Key></Spec>
        </div>
      </GallerySection>

      <GallerySection id="pane" title="Pane" note={<>The world's one surface: hairline bezel, sheen over dark glass, inset edge-light and vignette. Cut any corner with <span className="code">--cut-tl / --cut-tr / --cut-br / --cut-bl</span>; set the glass with <span className="code">--pane-fill</span>.</>}>
        <div className="pane-grid">
          {[
            ["pane pane--tl pane-demo", "Top cut", ".pane .pane--tl"],
            ["pane pane--dim pane--br pane-demo", "Dim · trailing cut", ".pane--dim .pane--br"],
            ["pane pane--notched pane-demo", "Notched", ".pane--notched"],
            ["pane pane--chamfer pane-demo", "Chamfered", ".pane--chamfer · 18px eight-cut"],
          ].map(([className, label, tag]) => (
            <Spec key={label} tag={tag}><div className={className}><span className="pane-demo-label">{label}</span></div></Spec>
          ))}
        </div>
        <Spec tag=".protocol-plate · .pane--dim .pane--br">
          <div className="protocol-plate pane pane--dim pane--br">
            <span className="protocol-label">Protocol</span>
            <span className="protocol-value">V7.3.1</span>
          </div>
        </Spec>
      </GallerySection>

      <GallerySection id="frame" title="Etched frame" note={<>The Clipped-Border Rule: a 1px-padded hairline outer cut around an inner pane re-cut 1px inside, so every chamfer carries a visible stroke. Cut depth rides on <span className="code">--cut</span> (default 18px).</>}>
        <div className="frame-demo-wrap">
          <EtchedFrame className="frame-demo"><span className="frame-demo-label">.frame-cut &gt; .frame-in · ticks · nodes</span></EtchedFrame>
        </div>
      </GallerySection>
    </>
  );
}
