# Timeline & Event Stream

The timeline unifies conversation, tool execution, interruption, workflow, evidence, and audit events.

## Event Types

- participant message
- agent message
- tool execution
- attachment/submission
- voice interruption
- workflow transition
- timer/deadline event
- evidence reference
- evaluation/review event
- system/audit event

## Standard Event Row

- optional 16–20px Lucide or state glyph
- event label: microlabel, `fg-muted`
- time: mono tabular, named timezone then UTC fallback (`PC-11`)
- body: 13–15px
- vertical padding: 8–16px
- left hairline rail optional for audit-oriented timelines; node terminals
  mark the current event when useful

## Tool Execution Event

Compact presentation:

- tool name
- status
- execution duration if useful
- collapsed input/output summary
- expandable structured detail

Do not dump full JSON into the primary conversation flow unless explicitly requested. Put raw data in an expandable technical panel.

## Interruption Event

Display as a lightweight timeline divider or event row, for example:

`Voice interrupted · 14:36:12`

The event must be clearly distinguishable from participant or agent speech.

## Audit Event

Use compact workspace density, mono metadata, and explicit actor/action semantics.
