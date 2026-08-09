# Sidebars & Navigation Rails

## Global Navigation Rail

- width: 232–260px expanded; 56–68px compact
- background: canvas or surface-inset
- right border: 1px border-subtle
- no drop shadow
- optional 1px dim blue rail separating navigation from active work bay

## Navigation Item

- height: 36–40px
- padding: 8–10px
- radius: sm
- icon: 18px
- label: 13–14px, 500
- inactive: fg-muted/default
- hover: surface-hover + fg-strong
- active/current: surface-selected + fg-strong + 2–3px brand signal rail on leading edge; `aria-current` where applicable
- focus-visible: scanner focus treatment

## Section Label

11–12px, 600, fg-subtle. Uppercase is permitted with restrained tracking; mono may be used for short telemetry-like group labels but not long navigation copy.

## Agent Context

When navigation is scoped to an agent, a Micro/Compact Core may appear near the agent name. Keep the full animated Primary Core in the main interaction/header region, not duplicated throughout navigation.

## Rules

Active state must be visible without relying only on text color. Mobile uses drawer/sheet navigation. Avoid more than two persistent navigation columns. Do not make every navigation item emit light.
