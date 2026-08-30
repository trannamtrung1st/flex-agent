---
id: toast-dock-placement
status: completed
created: 2026-08-30
updated: 2026-08-30
---

# Goal

Dock toasts at the work-bay **bottom-center** by default (clear of start and trailing plate-foot keys), and make placement configurable on `ToastDock` / `ToastHost`.

# Governing sources

- `docs/ui-ux/design-system/components/alerts.md`
- Owner decision: bottom-center of the work bay, raised above hull/action feet, so start and trailing keys stay clear.

# Scope

## In

- `placement` + optional `offsetInline` / `offsetBlock` on `ToastDock` and `ToastHost`
- CSS keyed off `data-placement`
- Production/lab hosts use `bottom-center`
- Spec + gallery notes

## Out

- Per-toast placement
- Moving the dock into `main` (still `position: fixed`)

# Plan

- [x] Red: placement defaults and overrides
- [x] Green: props + CSS
- [x] Docs + gallery
- [x] Focused tests + detector

# Current state

Completed. `ToastDock` / `ToastHost` take `placement`, `offsetInline`, and `offsetBlock`. Default is `bottom-center`.

# Decisions

- Default `placement="bottom-center"`. Corners remain available for specimens or shells that need them.
- Offsets are CSS lengths on the dock (`--toast-dock-offset-inline` / `--toast-dock-offset-block`).
- Nested layout specimens set `data-nested` so gallery gangways do not steal hull offsets.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Focused Vitest | passed | ToastDock, ProductionAppShell, layouts |
| detect.mjs | passed | `[]` |
