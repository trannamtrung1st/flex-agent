# Radios, Checkboxes & Toggles

## Checkbox

- 16px visual control; interactive label/hit area should normally reach at least 32px in workspace mode and 40px in interaction mode
- sm/xs radius
- 1px border-strong
- checked: brand-primary background/border
- focus-visible: scanner focus treatment from `interaction-states.md`

## Radio

- 16px visual control with the same expanded interactive target guidance as checkbox
- full radius
- 1px border-strong
- checked: brand-primary ring/indicator
- focus-visible: scanner focus treatment from `interaction-states.md`

## Toggle

- track: ~32–36px × 18–20px; the full labeled control should provide a comfortably larger interactive target
- full radius
- unchecked: surface-tertiary / border-strong
- checked: brand-primary
- thumb: surface-primary

Disabled controls use `fg-disabled`/`surface-disabled`, retain labels, and have no hover/active response.

Use semantic status colors only if the toggle itself represents a semantic state and the text label makes that meaning explicit.
