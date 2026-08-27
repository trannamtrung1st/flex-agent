# Evidence

Evidence surfaces must prioritize traceability, provenance, and inspectability
over decoration. Evaluation consumes Evidence but is specified separately in
the [Evaluation and review pattern](evaluation.md).

## Evidence Reference

Each reference may include:

- source/turn/submission identifier
- concise excerpt or summary
- criterion or evaluation linkage
- timestamp/page/section when available
- confidence or reviewer state when applicable

## Presentation

- border: 1px hairline; zero radius; optional 10px notch on the plate
- background: smoked-glass inset
- citation/source label: mono microlabel
- excerpt: 13–15px Sometype Mono body
- optional hairline tether to the cited turn on Review surfaces; tethers are
  decorative reinforcement of exact Evidence references, not authority

## Evidence Quality / Confidence

When Evidence itself carries a quality/confidence assessment, express it with
text (for example High / Medium / Low or a configured numeric value) and not
color alone. Do not conflate Evidence-quality confidence with Evaluation
confidence; [Evaluation and review](evaluation.md) owns the latter.

## Review Flags

- neutral/info: note
- warning: human review requested / insufficient evidence
- danger: blocking evidence problem or invalid reference

Do not use red merely for low confidence unless the configured product semantics define it as an error.
