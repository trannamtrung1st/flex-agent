# Error summary

Use an error summary when validation, authorization-safe input rejection, or a
multi-item failure can leave more than one correction or recovery target.

## Anatomy

- heading that identifies the failed action or form
- concise consequence or preservation statement
- list of actionable errors linked to the exact permitted field, item, or
  section
- retry, refresh, return, or support action when correction is not available in
  place

Use `danger-soft`, `fg-danger`, and `border-danger` only for confirmed error
semantics. A warning or stale-state conflict uses its owning semantic treatment.

## Behavior

- After a submitted action returns actionable errors, move focus to the summary
  heading and announce it once.
- Activating a summary item moves focus to and identifies the affected control
  or section. Do not focus an inaccessible or hidden target.
- Preserve recoverable input according to the governing interaction
  specification.
- Keep field/item messages associated with their controls; the summary does not
  replace inline identification.
- A retry remains pending until authoritative success or failure is reconciled;
  prevent duplicate activation where required.
- Do not expose internal policy, parser, scanner, storage, authorization, or
  inaccessible-resource details in error copy.

Use [alerts](alerts.md) for standalone notices that do not need linked
correction targets.
