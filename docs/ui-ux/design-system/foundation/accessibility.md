# Accessibility foundation

The approved P0 Activity journey and surface specifications establish WCAG 2.2
AA as the contractual accessibility baseline. Shared components must preserve
that baseline; a visual treatment is defective when it makes an approved flow
less perceivable, operable, understandable, or robust. Shipboard styling does
not waive this contract (`PC-12`).

## Semantics and structure

- Use native elements and established ARIA patterns before custom controls.
- Preserve one logical heading hierarchy and named page regions/landmarks.
- Give every control a programmatic name; associate instructions, constraints,
  helper text, and validation messages with the relevant control.
- Use tables only for true row/column relationships and expose header scope.
- Dynamic content must preserve an understandable reading order independent of
  viewport layout.
- Custom listboxes, menus, and temporal widgets must meet the same keyboard and
  name contracts as native controls; visual adopt is not behavior authority.

## Keyboard and focus

- Every action must work without pointer, hover, drag, motion, sound, or touch.
- Keep focus order aligned with reading and task order.
- Use the visible focus treatment from
  [interaction states](interaction-states.md); never remove it without an
  equivalent.
- Do not move focus for ordinary incoming content, streaming updates, list
  refreshes, or background status changes.
- Move focus when the current task requires it: error summary, opened dialog,
  exact Evidence target or viewer heading, permission-loss explanation, or the
  next safe action after a focused control disappears.
- Restore focus to the invoking control or an explicit logical successor when a
  dialog, viewer, removed record, or subordinate route closes.

## Status and announcements

- Visible text communicates decision-relevant state; icon, color, shape,
  position, motion, or sound may reinforce but never replace it.
- Announce accepted actions, material state transitions, errors, warnings, and
  completion at a useful rate. Do not announce every streaming token, upload
  byte, timer tick, or decorative animation frame.
- Preserve user input on recoverable failure when the governing specification
  permits it and identify both the summary and field/item error.

## Reflow, zoom, and targets

- Support 400 percent browser zoom and narrow viewports without hiding the
  current state, consequence, recovery action, or protected-context warning.
- Avoid two-dimensional scrolling for ordinary text and controls. A true data
  table or other intrinsically two-dimensional region may scroll within a named
  container while surrounding actions remain reachable.
- The visual target for an interactive control must be at least 24 by 24 CSS
  pixels or meet the applicable WCAG spacing/exception. Prefer at least 44 by 44
  CSS pixels for touch-critical, destructive, timed, and primary Participant
  controls. Compact 30px keys must still meet the target requirement via padding
  or an invisible hit area.
- Clip-path notches, sticky rails, and command strips must not cover focused
  content, errors, dialogs, browser zoom controls, or the software keyboard.

## Contrast and themes

- Normal text must meet at least 4.5:1 contrast; large text at least 3:1.
- Essential control boundaries, state indicators, and focus indicators must
  meet at least 3:1 against adjacent colors.
- Disabled controls may use reduced contrast where the criterion permits, but
  any explanation of why an action is unavailable must remain normally
  readable.
- Verify token pairings in both light and dark themes and in every interactive
  state. Do not infer contrast from token names or visual inspection alone.
- Do not place dither, emission, gradients, or imagery behind dense reading,
  form, transcript, Evidence, or table content.
- Support `forced-colors`: keep borders, focus, and text keywords; do not rely
  on phosphor glow.

## Motion, audio, and media

- Respect `prefers-reduced-motion`; preserve meaning with static state text,
  structure, and restrained opacity or edge changes.
- Avoid flashes, rapid high-contrast sweeps, and motion unrelated to state.
- Voice and audio controls require visible state and an equivalent non-audio
  path where the owning feature specification requires one.
- Decorative Agent Core and trace imagery use empty alternative text or the
  equivalent hidden presentation. State text carries meaning.

## Verification

Combine automated accessibility checks with keyboard-only operation,
accessibility-tree inspection, focus/announcement tests, 400 percent zoom,
reduced motion, forced colors, and desktop/narrow screenshots. Automated checks
do not prove reading order, clear content, focus recovery, or visual polish.
