# Modals

## Backdrop

- full-screen fixed overlay
- background: overlay-scrim
- no decorative backdrop blur required

## Dialog

- background: surface-elevated
- border: 1px solid border-default
- radius: lg
- shadow: shadow-md
- width: content-dependent
- default max width: 520–640px

## Anatomy

### Header

- padding: 20–24px
- title: 18–20px, 600
- optional description: 13–14px, fg-muted

### Body

- padding: 0 20–24px 20–24px or 20–24px when separated
- form gaps: 16–20px

### Footer

- top border optional when body is long
- padding: 16–24px
- actions aligned according to reading direction

## Rules

- use native/accessibility dialog semantics and programmatically associate the
  title and relevant description/consequence
- move initial focus according to the governing flow; destructive confirmation
  commonly starts on the safe cancel action unless an approved specification
  says otherwise
- contain focus while open and restore it to the trigger or logical successor
  on close
- Escape performs the safe cancel/close action unless an approved workflow
  requires an additional discard confirmation
- confirmation dialogs state the concrete target, consequence, and affected
  audience or preserved work when relevant
- never preload unauthorized protected detail merely because the dialog is
  visually hidden before opening
