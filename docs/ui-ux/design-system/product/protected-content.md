# Protected content and access states

Participant submissions, transcripts, voice, Evidence, Evaluations, Human
revisions, Results, memory, and audit records are sensitive. Shared UI patterns
must preserve the authorization, isolation, redaction, and non-disclosure rules
from the owning approved specification.

## State sequence

1. **Protected loading** — show neutral structure/status without protected
   content, counts, names, existence clues, or cached previews.
2. **Authorized content** — render only fields and actions permitted for the
   current actor, resource scope, and workflow state.
3. **Unavailable** — use the owning non-disclosing state for inaccessible,
   revoked, expired, lawfully unavailable, integrity-failed, and nonexistent
   targets where the specification requires them to be indistinguishable.
4. **Access changed** — remove no-longer-permitted content and controls, explain
   the safe consequence, and move focus to the message or next safe action when
   the focused target disappeared.

## Viewer pattern

- Identify the permitted source type and exact immutable version or terminal
  boundary when the owning specification exposes it.
- Open only the authorized locator, range, page, section, or whole-artifact
  fallback; never substitute newer, cached, or adjacent content.
- Provide a reliable **Back to criterion**, **Back to Result**, or equivalent
  return target and restore focus.
- Redaction and unavailability remain explicit without revealing hidden content
  or the reason beyond the permitted disclosure category.
- Preview, copy, download, and export are separate authorized actions. A visible
  reference is not an access token for its target.

## Client-safety rules

- Do not render protected content and hide it with CSS, blur, clipping,
  off-screen placement, or collapsed disclosure.
- Clear or replace stale protected view state after permission loss; do not
  recover it from client caches, logs, analytics, browser storage, or error
  payloads.
- Render untrusted text and files through the approved safe viewer. Do not run
  embedded scripts, active links, macros, archives, or instructions as UI or
  Agent authority.
- Keep raw protected content out of telemetry, analytics labels, console logs,
  URL parameters, browser artifacts, and error reports.
- Tooltips and hover previews must not be the only location for a protected
  value, access consequence, redaction, or non-disclosing state.

## Responsive and accessibility behavior

Protected context, audience, version, and consequence remain visible at narrow
widths and 400 percent zoom. Opening a protected target follows the focus and
return rules in [accessibility](../foundation/accessibility.md). The loading and
unavailable states must have programmatic headings/names and must never flash
protected content before authorization completes.
