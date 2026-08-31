# Alerts, advisories, and toasts

Implementation: `web/src/design-system/components/feedback/` (`Alert`) and
`web/src/design-system/components/chrome/OperateHead.tsx` (`Advisory`),
`overlays/ToastDock.tsx`. Gallery: `advisory`, `alert`, `toast`.

## Advisory

Standing full-width hairline strip (`Advisory`). Default: teal tick + teal
label. Attention: amber hairlines + warning triangle. `role="status"` unless a
parent live region owns the announcement (`live={false}`). Stays until the
condition clears. The leading mark stays with the label row when copy wraps.

Place standing notices with `OperateArea` `advisory` or as a sibling under the
operate head. Do not restyle a toast to stand in for a persistent condition.
Frozen-cluster provenance on a ceremony form uses in-form `Alert` (below), not
this operate-head slot.

## Alert

Workspace banner (`Alert`, `.workspace-alert`) combining an advisory strip with
optional body copy. The TypeScript `variant` union includes `info`, `success`,
`warning`, and `danger`, but only **danger** is a distinct skin today:

- `info` / `success` / `warning`: `role="status"`; advisory label **Note**; teal
  system voice. Do not invent a green success banner or amber warning banner on
  this primitive.
- `danger`: `role="alert"`; advisory label **Error**; attention voice
- Title is the advisory copy; children render in `.workspace-alert-body`

Place an in-form Note at the top of the ceremony scroll (after an error
summary, before fields) when several frozen controls share one provenance
line. Do not use OperateArea `advisory` for that copy — that strip sits with
the operate head, above tracks. Do not use a floating field-hint for the
same message. When a success Note is already present (for example **Cohort
activated**), put the provenance sentence in that Alert’s body instead of
stacking a second teal Note.

Success must not imply Result Release or Session completion. Danger is for
failed or blocking outcomes, not field validation (validation uses amber and
[error summary](error-summary.md)). Amber field warnings stay on the field /
error summary, not on `variant="warning"`.

## Toast

Instrument slip (`ToastDock` / `ToastHost`). Placement is a component prop
(`placement`: `bottom-center` | `bottom-start` | `bottom-end` |
`top-center` | `top-start` | `top-end`). The production default is
**top-center** for now. Optional `offsetInline` and `offsetBlock` (CSS lengths) add clearance
for a gangway, instrument rail, or hull/action foot; they set
`--toast-dock-offset-inline` and `--toast-dock-offset-block` on the dock.
On center placements, `offsetInline` insets the centering strip from the
inline-start (work bay after a gangway), not a corner. Management and
guided-task layouts supply inline offsets when the host does not. The dock
paints above hull chrome (`command-strip` / Deck `page-strip`, `z-index` 70)
at `z-index` 75 and below bulkhead (`80`). Top placements use the default
inset only and may cover hull chrome — receipts are short-lived. Compact
(≤720px) stretches the dock to the viewport inline edges. Bottom placements also keep `offsetBlock` so the slip sits
**above** a fixed foot, not over it.

Notched hairline, 320ms reveal (cut under reduced motion). System voice teal;
attention voice amber. Default linger is 4200ms (`useToasts`); leave fade is
240ms (0 under reduced motion). `role="status"` on each slip; the dock is a
polite live region. Do not toast protected content or authorization internals.

## Rules

- Decision-relevant errors that block a task also appear inline or in an error
  summary, not only as a toast.
- Transient action receipts (assign, save, lifecycle, accepted Submission
  version) use toast when the action is on the page. Receipts for actions
  inside an open modal sit in that ceremony’s pinned foot — the page toast
  dock cannot paint above the dialog top layer. Standing page conditions (missing sources, remaining
  registry pages) use OperateArea `advisory`. Blocking failures and still-true
  work outcomes (activated cohort, in-well capability notes) use `Alert`.
- Production management and guided-task shells, and the design-lab Admin
  console, mount `ToastHost`. Component Deck specimens may own a local
  `useToasts` + `ToastDock` so the gallery can fire slips without wrapping
  the whole lab.
- Occupied wait uses [WaitPanel / wait instruments](../product/empty-loading.md),
  not an alert.
- Shared frozen-cluster provenance is one in-form **Note** (`Alert` info), not
  a field-hint and not OperateArea `advisory`. Gallery: `alert` (primitive)
  and `layout-management-setup` (Setup composition). Readiness blockers use
  [error summary](error-summary.md) (**Readiness blocked**), not a second teal
  Note.
