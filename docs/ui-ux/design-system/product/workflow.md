# Structured Workflow & Stages

Workflows define ordered session stages, transitions, and structured activity progression. The MVP uses explicit list/step/detail patterns; **do not invent a free-form node-canvas workflow builder**.

The assessment MVP may display its resolved stage/progress behavior, but general
workflow configuration is a later capability unless an approved feature
specification enables it.

## Participant / Live Session View

- show the current stage clearly; a phase rail is visual progress/navigation
  only and must not mutate lifecycle or entitlement (`PC-07`)
- show overall progress only when the workflow configuration makes that progress meaningful
- keep future stage details hidden or subdued when revealing them would be inappropriate for the activity
- transitions should feel stable and preserve conversation/session context

## Admin / Configuration View

Use workspace density with an ordered stage list plus detail editor/inspector. A stage row may include:

- stage name
- concise purpose/type
- order/position
- key limits or transition condition summary
- validation/problem state
- drag/reorder handle only when reordering is permitted, with keyboard-accessible alternative controls

## Transition Representation

Represent transition rules as readable structured rows or condition summaries. Avoid decorative graph edges or node diagrams unless a future product capability explicitly introduces a visual workflow builder.

## History

Actual stage changes in a session are timeline/audit events and follow `timeline.md`. The configured workflow and the executed session history must not be visually conflated.
