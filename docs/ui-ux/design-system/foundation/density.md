# Interface Density

Flex Agent has two first-class density modes. Do not use one global density everywhere.

## Interaction Mode

Use for participant-facing sessions, live conversations, voice interaction, review narratives, focused completion flows, and any screen where sustained reading/conversation is primary.

### Interaction characteristics

- body/reading text: 15–18px
- generous vertical rhythm
- touch-friendly primary controls
- fewer visible secondary actions
- maximum reading width: ~760px
- metadata visually subordinate
- strong conversational focus
- minimal chrome around current task

### Interaction spacing

- row/item vertical padding: 14–20px
- conversation event spacing: 20–32px
- toolbar gaps: 8–12px
- major section gap: 32–48px

## Workspace Mode

Use for admin configuration, agent setup, harness editing, activity/campaign setup, sessions list, audit logs, evidence review, evaluation grids, tool configuration, and operational dashboards.

### Workspace characteristics

- body text: 13–15px
- dense but not cramped controls
- more information visible simultaneously
- hairline dividers
- compact tabs and toolbars
- mono metadata used frequently
- split panes and inspectors are encouraged

### Workspace spacing

- row vertical padding: 8–12px
- table cells: 8–12px vertical
- form groups: 16–24px
- panel padding: 16–24px
- major section gap: 24–32px

## Rules

- A page may contain both modes, but each region must have a clear dominant purpose.
- Do not shrink participant conversation text to match dense admin screens.
- Do not inflate audit tables to interaction-mode spacing.
- Density changes spacing and control sizing, not core color or typography hierarchy.
