# Dropdown

## Trigger

- Follow `buttons.md` or `inputs.md` depending on whether the trigger is an action or value selector.
- Open state uses `surface-selected` or an explicit chevron/state change.
- Focus-visible follows `interaction-states.md`.
- Chevron: 16px; rotate only when the control semantics benefit from it.

## Menu Container

- background: surface-elevated
- border: 1px solid border-default
- radius: md
- shadow: shadow-sm
- padding: 4–6px
- typical min width: 180px
- max height: bounded with vertical scrolling when needed

## Menu Item

- height: 32–36px
- padding: 8px
- radius: sm
- font: 13–14px
- icon: 16px
- hover: surface-hover
- active: surface-selected
- destructive: fg-danger; hover danger-soft
- disabled: fg-disabled; no hover/active activation
- keyboard-highlighted/active-descendant item: same visible emphasis as hover, with focus semantics preserved on the owning control

Dividers use 1px border-subtle with 4–6px vertical separation.
