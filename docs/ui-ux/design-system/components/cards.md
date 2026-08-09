# Cards & Hull Panels

Cards are explicit object/group boundaries, not the default container for all content. Bounded operational surfaces should read more like **instrument panels** than floating SaaS cards.

## Core Specs

- background: surface-primary or surface-secondary according to plane
- border: 1px solid border-subtle by default; border-default for stronger grouping
- radius: md
- shadow: none by default
- padding: 16–24px depending on density

## Hull Panel Variant

Use for active agent/session summary, inspector groups, live controls, or telemetry clusters.

- precise 1px edge
- optional 2–3px signal rail on one edge for current context
- optional notch-sm/md on one corner
- optional very faint inner blue edge; no general outer glow

## Static Card

No hover effect. Use for summaries, evidence groups, and bounded dashboard objects.

## Interactive Card

- hover: surface-hover and/or border-hover
- selected/current: surface-selected + border-selected or signal rail
- focus-visible: scanner focus treatment
- cursor only if the whole surface is truly actionable

## Rules

Avoid nested cards when dividers/sections can express hierarchy. Do not notch every panel or add shadows to every card.
