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

## Alert

Workspace banner (`Alert`, `.workspace-alert`) combining an advisory strip with
optional body copy. The TypeScript `variant` union includes `info`, `success`,
`warning`, and `danger`, but only **danger** is a distinct skin today:

- `info` / `success` / `warning`: `role="status"`; advisory label **Note**; teal
  system voice. Do not invent a green success banner or amber warning banner on
  this primitive.
- `danger`: `role="alert"`; advisory label **Error**; attention voice
- Title is the advisory copy; children render in `.workspace-alert-body`

Success must not imply Result Release or Session completion. Danger is for
failed or blocking outcomes, not field validation (validation uses amber and
[error summary](error-summary.md)). Amber field warnings stay on the field /
error summary, not on `variant="warning"`.

## Toast

Bottom-right instrument slip (`ToastDock`; full width at ≤720px). Notched
hairline, 320ms reveal (cut under reduced motion). System voice teal;
attention voice amber. Default linger is 4200ms (`useToasts`); leave fade is
240ms (0 under reduced motion). `role="status"` on each slip; the dock is a
polite live region. Do not toast protected content or authorization internals.

## Rules

- Decision-relevant errors that block a task also appear inline or in an error
  summary, not only as a toast.
- Occupied wait uses [WaitPanel / wait instruments](../product/empty-loading.md),
  not an alert.
