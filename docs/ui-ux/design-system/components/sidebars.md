# Sidebars (gangway, rails, bulkhead)

Persistent area navigation is a **gangway** (`--gangway-w` 232px default,
248px in the administrator shell, `--gangway-w-collapsed` 76px) or a leading
**bulkhead** (`--bulkhead-w` 280px; 420px wide) at drawer widths. Nav lists
use uppercase mono, teal hover, Bright Text plus teal tick when current.
Navigation is never amber.

Participant Assignment and Session left rails are **instrument bulkheads**,
not inset plates: 260px on Assignment Station, 232px on Examination Console.
On desktop they flush to the hull; at ≤1080px (assignment) or ≤1180px
(session) they stack as an instrument band. Rail `--instrument-bulkhead-fill`
is `transparent` so the hull shows through; the examiner plate remains inset.

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
  Desktop shells must not use a min-height taller than `100dvh` while page
  overflow is hidden.

## Breadcrumbs

`BreadcrumbNav` is an in-bay trail (`nav` / `aria-label="Breadcrumb"`), not a
second gangway. Home plus slash-separated **reachable destinations**. Ancestor
crumbs are `.text-link` (phosphor color, no underline). The current crumb uses
`aria-current="page"` and is not a link. Nested-record `BackKey` belongs in
`OperateHead` beside the copy cluster (trailing at desktop; own leading row at compact widths),
not beside this trail. Production maps crumbs in `web/src/components/shell/`
from canonical routes, not from every URL segment: locator ids and collection
wrappers without a page (`Activity`, `Cohorts`, `Cohort`) are not crumbs.
Activity and cohort context stays in the URL and in-page chrome (`IA-MVP-2`).
The design lab and shared library import the presentational primitive. Gallery:
`breadcrumbs`.

## Index rail

`IndexRail` is the reference-shell catalog (`ReferenceLayout`): `.deck-rail` >
`.nav-rail` plus `SectionedNavigation`. Gallery section `nav-rail` shows the
shared `.nav-rail` / `.nav-link` grammar (teal tick + Bright Text when
current), including a nested `OperateHead` + `BackKey` specimen. That grammar
is not production area navigation; production area nav is gangway/bulkhead.
Current destination is never amber.
