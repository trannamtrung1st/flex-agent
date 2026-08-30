# Technical Metadata

Technical metadata should be easy to inspect without visually overpowering human-readable content.

## Typography

- font: `--font-mono`
- size: 0.72–0.82rem (12–13px)
- tabular numerals where applicable
- color: fg-muted by default
- primary technical value: fg-default or fg-strong

## Common fields

Expose only metadata authorized for the current actor and surface. A value being
useful for audit does not make it Participant-visible or safe for URLs,
analytics, logs, screenshots, or copy actions.

- agent ID
- session ID
- activity/campaign ID
- harness snapshot/version
- model identifier
- tool execution ID
- timestamps
- token counts
- duration/latency
- retry/attempt count
- effective memory mode and override source when relevant
- exact harness snapshot/version used by a session
- effective tool/policy/configuration version identifiers when exposed for audit

## Copy Behavior

Machine identifiers should offer copy affordance on hover/focus when useful. Copy success feedback should be brief and accessible.

## Truncation

Long identifiers may be center- or end-truncated visually, but full value must be
available through copy or tooltip/detail view. Registry tables use `CompactId`
for that: center-truncated display, value plaque on hover, and the full
identifier in the accessible name. The value plaque stays open long enough to
move onto it and select or copy the exact string. Dense registry tables omit
per-cell tab stops; pass `tabbable` on standalone surfaces when focus-visible
plaque is needed. Do not use a native `title` attribute for this pattern.
