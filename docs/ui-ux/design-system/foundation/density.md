# Interface Density

Flex Agent has two first-class density modes. Do not use one global density
everywhere. Density changes spacing and control sizing, not token meaning.

## Interaction Mode

Use for Participant sessions, live conversations, review narratives, focused
completion flows, and any screen where sustained reading is primary.

### Interaction characteristics

- body/reading text: 15–18px
- generous vertical rhythm
- touch-friendly primary controls (prefer 44px targets)
- fewer visible secondary actions
- maximum reading width: ~68–78ch
- metadata visually subordinate
- strong conversational focus
- minimal chrome around the current task

### Interaction spacing

- row/item vertical padding: 14–20px
- conversation event spacing: 20–32px
- toolbar gaps: 8–12px
- major section gap: 32–48px
- ceremony plate padding: 30–46px

## Workspace Mode

Use for Campaign setup, Enrollment administration, review queues, Result
queues, and other operational dashboards authorized by the current release.

### Workspace characteristics

- body text: 13–15px (about 0.72–0.88rem in tables)
- dense but not cramped controls
- hairline dividers and compact toolbars
- mono metadata used frequently
- split panes, gangway, and inspectors are encouraged
- compact keys may use 30px visual height if the hit target remains at least
  24px (prefer 44px for destructive, timed, and primary Participant actions)

### Workspace spacing

- row vertical padding: 8–12px
- table cells: 7–12px vertical
- form groups: 16–24px
- panel padding: 16–24px
- major section gap: 24–32px
- readout row padding: 9–12px

## Rules

- A page may contain both modes, but each region must have a clear dominant purpose.
- Do not shrink Participant conversation text to match dense admin screens.
- Do not inflate audit tables to interaction-mode spacing.
- Prototype “instrument-panel density” is the workspace look; it must not
  violate type floors in [typography](typography.md) or target sizes in
  [accessibility](accessibility.md).
