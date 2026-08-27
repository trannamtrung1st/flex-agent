# Sidebars (gangway, rails, bulkhead)

Persistent area navigation is a **gangway** (`--gangway-w` 232px default,
248px in the administrator shell, `--gangway-w-collapsed` 76px) or a leading
**bulkhead** (`--bulkhead-w` 280px; 420px wide) at drawer widths. Nav lists
use uppercase mono, teal hover, Bright Text plus teal tick when current.
Navigation is never amber.

Participant Assignment and Session left rails are **instrument bulkheads**,
not inset plates: 260px on Assignment Station, 232px on Examination Console.
On desktop they flush to the hull; at ≤1080px (assignment) or ≤1180px
(session) they stack as an instrument band. The examiner plate remains inset.

- Destinations exist only when traced to approved scope and a server-provided
  permission (`PC-09`, `PC-10`).
- Collapsed codes use trailing tooltips; full names remain available to
  assistive technology.
- Role home returns to the current actor’s operational home from the Activity
  IA, not prototype `/participant-*` paths.
- Campaign context sits in-page (Campaign Context instrument), not as silent
  substitution in the rail (`PC-06`).
- Rail internals that overflow a short desktop scroll inside the rail; do not
  clip instruments, and do not pin the stacked narrow band to the viewport.
