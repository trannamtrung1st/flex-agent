# Badges

## Core Specs

- font: 12px, 500–600
- height: 20–24px
- padding: 4–7px horizontal
- radius: xs by default
- border: optional 1px

## Variants

| Variant | Background | Foreground | Border | Typical use |
|---|---|---|---|---|
| Neutral | surface-secondary | fg-default | border-subtle | metadata/lifecycle |
| Brand | brand-softer | fg-brand | border-brand | current/selected context |
| Live | brand-live-soft | fg-live | border-live | listening/streaming/live |
| Success | success-soft | fg-success | border-success | succeeded/approved |
| Warning | warning-soft | fg-warning | border-warning | review/pause/attention |
| Danger | danger-soft | fg-danger | border-danger | failed/rejected/error |
| Info | info-soft | fg-info | border-info | informational state |

## Pill Variant

Use `full` radius for statuses, filters, participant groups, or compact categorical labels when pill shape improves scanning.

## Rules

- Lifecycle/status badge text must match `status.md`.
- Do not use saturated badge fills in dense tables; prefer soft fills or neutral text+dot.
- A badge is not a button unless it has explicit interactive styling and keyboard behavior.
