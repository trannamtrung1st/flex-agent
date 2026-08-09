# Status Grammar

Status is a shared product language. The same state must look and read consistently across lists, details, timelines, and dialogs.

## Status Anatomy

A status representation may include:

1. icon or dot
2. text label
3. semantic color
4. optional supporting timestamp/detail

Text label is mandatory when status meaning affects user decisions.

## Common State Families

### Lifecycle

- Draft
- Ready
- Active
- Paused
- Completed
- Archived

### Execution

- Waiting
- Running
- Streaming
- Succeeded
- Failed
- Cancelled

### Review

- Not reviewed
- In review
- Review required
- Approved
- Rejected

### Agent memory mode

- Dynamic — the Agent may learn from approved sources subject to memory
  policies and administrative controls
- Stable — no new long-term learning from the current interaction; configured
  identity, knowledge, and approved existing memory remain in use

Do **not** present `Disabled` as a third memory mode. When clarification is
needed, show `Stable` with supporting text such as `New long-term learning
disabled`. Stable mode must not imply that configured knowledge or previously
approved memory was deleted or made unavailable; display the independent
approved-memory read policy when it affects the current context.

When resolution rules permit Agent, Harness, Activity/Campaign, or Session
configuration to set or narrow memory policy, display the **effective mode** and
the exact permitted source as secondary metadata rather than inventing another
status. For the assessment MVP, also expose whether approved-memory reads are
disabled or pinned to one immutable Memory snapshot.

### Harness assignment and editor state

- Current — an Agent or Activity/Campaign assignment resolves the current
  approved Harness revision when a new Session is instantiated
- Pinned — an Agent or Activity/Campaign assignment is pinned to a specific
  immutable Harness snapshot
- Modified — the Harness editor has unapplied/unsaved changes

`Harness snapshot` is an object/version type, not a status. `Restored` is a
history event, not a persistent status. `Current` and `Pinned` describe
configuration/assignment policy; they are **not execution states for an
already-started Session**. Once a Session is instantiated, show the exact
resolved Harness snapshot/version recorded for that Session, plus the source
assignment when useful for audit.

### Connectivity

- Connected
- Reconnecting
- Disconnected

Connectivity is separate from Agent Core/voice state. Do not use `Offline` as a substitute for an agent-presence state. `Reconnecting` normally uses warning/attention semantics; `Disconnected` uses danger only when it blocks required interaction, otherwise use neutral lifecycle styling plus explicit text.

### Session

- Waiting
- Live
- Paused
- Completed
- Terminated
- Expired

## Visual Mapping

- neutral: draft, waiting, archived
- brand: active, selected, current
- live/cyan: listening, streaming, speaking, live
- success: succeeded, approved, completed when success semantics apply
- warning: review required, needs attention, reconnecting, or paused when pause has a consequence requiring attention
- danger: failed, rejected, terminated, disconnected when blocking, destructive/error states

Do not automatically make every completed state green if completion is merely lifecycle information rather than success.
