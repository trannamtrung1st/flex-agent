# Session Controls, Timing & Progress

Structured activities may have timers, deadlines, attempts, stages, pause/end controls, or other session limits. These controls must be explicit and calm rather than gamified.

## Timer / Deadline

- use tabular Display-role digits for the Participant countdown; amber is the
  attention voice for time
- approaching configured threshold: amber intensification plus text; do not
  switch the timer to red
- expired: danger only when expiration blocks or fails an action; otherwise use
  neutral lifecycle wording
- never communicate urgency through color or animation alone
- stage bars: completed teal, current amber, remaining dim hairline
- client timers are never authority (`PC-08`)

## Stage / Progress

- show current stage name and, when known, position such as `2 of 2`
- bar count must match the frozen Session workflow stage count, not a demo
  theater. P0 hosted text Session is Examination then Complete (`2` bars).
  Design Lab Session and the Deck wait specimen may show a 5-bar demonstration;
  that count is not product authority
- use a compact step/linear progress treatment following `workflow.md`; avoid decorative achievement/gamification patterns
- completed stages use structural check/state indication; do not assume green if completion is not success

## Attempts

Show remaining/used attempts as explicit text when the user needs the information to make a decision. Avoid hiding attempt limits in tooltips.

## Pause / End / Terminate

- pause/resume uses neutral or warning semantics depending on consequence
- end/submit uses the normal primary action when it is the intended completion path
- terminate/discard uses danger semantics when destructive
- confirmation copy must state whether progress, responses, or eligibility will be affected

## Rules

- Timing shown to participants and reviewers must derive from the authoritative session state, not independent client-only counters.
- When connectivity/state is uncertain, show that uncertainty rather than presenting a misleading precise timer/state.
- In administrator/reviewer multi-session workflows, keep the active session/participant identity persistently clear before destructive or evaluative actions; do not rely on browser/tab position alone.
