# Memory Mode & Stored Memories

Memory UI must clearly separate **mode/configuration** from **stored memory records**. Stable mode disables new long-term learning; it does not imply that previously approved memory has been erased. Present mode as readout text plus a teal or dim node, not a decorative badge.

Stored-memory management and Dynamic memory are later-release patterns. They do
not enable those capabilities in the assessment MVP. The approved assessment
UI may show only the effective Stable mode and its disabled-or-pinned immutable
approved-memory read policy.

## Memory Mode Summary

Show:

- effective mode: `Dynamic` or `Stable`
- source of that mode when resolution rules permit it: Agent, Harness,
  Activity/Campaign, or Session configuration
- approved-memory read policy when decision-relevant
- concise consequence text, such as whether approved sources may contribute to
  long-term memory under the owning policy

Do not use a third `Disabled` mode.

## Stored Memory List

Use workspace-density operational rows or a table. Each record should surface, when available:

- concise memory content/summary
- provenance/source
- scope
- created/updated time
- relevant approval/audit metadata
- available management actions

Memory content uses readable sans typography. IDs, timestamps, and provenance identifiers may use mono.

## Management Actions

- inspect/edit: neutral actions
- approve when the product exposes an approval action: normal primary/secondary hierarchy according to context
- archive: neutral lifecycle action unless configured otherwise
- delete: danger semantics with explicit consequence confirmation

## Rules

- Never visually imply that switching to Stable deletes existing memory.
- Never merge Participant memory across Sessions/Campaigns in the UI unless an
  approved policy and specification explicitly permit the scope.
- Show provenance and scope clearly enough for an authorized Administrator or
  Reviewer to understand why a memory is available.
- Memory changes that affect auditability should appear in the relevant history/event surface.
