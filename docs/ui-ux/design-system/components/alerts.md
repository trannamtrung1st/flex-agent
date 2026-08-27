# Alerts, advisories, and toasts

## Advisory

Standing full-width hairline strip. Default: teal tick + teal label. Attention:
amber hairlines + warning triangle. `role="status"`. Stays until the condition
clears.

## Toast

Bottom-right instrument slip (full width at ≤720px). Notched hairline, 320ms
reveal (cut under reduced motion). System voice teal; attention voice amber.
Auto-dismiss about 4s; `role="status"` / polite live region. Do not toast
protected content or authorization internals.

## Rules

- Decision-relevant errors that block a task also appear inline or in an error
  summary, not only as a toast.
- Success toasts must not imply Result Release or Session completion.
