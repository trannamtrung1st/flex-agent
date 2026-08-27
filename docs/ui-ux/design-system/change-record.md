# Design-system v1.0 change record

Non-normative provenance. This file does not govern product behavior, journeys,
or visual contracts. The [design-system README](README.md) and module files do.

## Source

| Field | Value |
| --- | --- |
| Visual source | Shipboard Terminal prototypes |
| Snapshot | Retired in Phase 7.5; formerly `.work/resources/impeccable-prototype-snapshot/` (Git history) |
| Experiment HEAD | `f724b68b11c2a147e59864f5789b260baaa50641` |
| Planning-review commit | `c52eeda3d8aa117bd7abd49f4ab0ab567953fe96` |
| Hashed files | 215 in `MANIFEST.json` |
| Rebuild task | `.work/active/impeccable-frontend-rebuild.md` |

The raw snapshot was temporary historical evidence and was deleted in Phase 7.5
of the rebuild, after adopted visual outcomes were durable in Git, this change
record, approved v1.0 modules, and the verified design lab. Recovery is Git
history plus this provenance record. Do not treat the deleted tree, the
external experiment checkout, or `/prototypes` as a live implementation
dependency.

## Supersession

Approved design-system v0.1 **Deep-Space Operational Futurism** is superseded
for shared visual identity, token values, typography aesthetics, component
appearance, non-semantic layout, and non-semantic motion. Git history retains
v0.1. Approved P0 interaction specifications are not superseded.

## Light-theme contrast

Light-theme phosphor teal for text/focus is `#146261` (at least 4.5:1 on
`#E8F0F2` canvas). Dark-theme `#3CC0BF` on hull ground exceeds 8:1. Implementation
must still verify every token pairing in both themes.

## Font license review

| Face | Package | License | Notes |
| --- | --- | --- | --- |
| Michroma | `@fontsource/michroma@5.3.0` | SIL OFL 1.1 | Copyright 2011 The Michroma Project Authors; imported from `web/src/styles/shared.css` |
| Sometype Mono | `@fontsource/sometype-mono@5.3.0` | SIL OFL 1.1 | Confirm OFL notice remains in the pinned package |

Exact versions are pinned in `web/package.json`. Self-host only. Include OFL
notices with the SPA license inventory.

## Implementation mapping (2026-08-27)

Recorded so agents do not treat root `DESIGN.md` or unused `--fa-*` names as
the token source:

- CSS primitives: `web/src/styles/tokens.css` (`--ground`, `--notch`,
  `--gangway-w`, `--ease-out`, …).
- Semantic aliases: `web/src/styles/semantic-aliases.css`.
- Light remaps: `web/src/styles/adaptations.css`.
- Participant instrument rails are desktop hull bulkheads (assignment 260px,
  session 232px) with stacked instrument bands at ≤1080px / ≤1180px. Desktop
  shells are `100dvh` with no 620px floor so short viewports scroll inside the
  rail rather than clipping past a hidden body.

## Adopted visual concepts

Smoked-glass plates, hairline bezels, notched zero-radius geometry, phosphor
teal / rationed amber, Michroma placards, Sometype Mono data, gangway/bulkhead,
command strip, keys, readout grid, wait instruments, clipped-border frames,
emitters-only glow, the square hull-ground document icon (favicon), and the
Component Deck as the design-lab catalog.

Candidate production CSS loads `web/src/styles/shared.css` (tokens, base, and
production-safe component families). Lab-only demo and surface sheets load
only through `web/src/styles/design-lab.css`.

## Deliberate deviations from the prototype

Every `PC-01`–`PC-14` / `BR-01`–`BR-14` item in the rebuild task. Material
examples:

- Review and Release stay separate (`PC-01`).
- Human revision is an immutable server submit (`PC-02`).
- Unpublished Results stay at **Result not available** (`PC-03`).
- Campaign activation stays draft / readiness / server activate (`PC-05`).
- Invalid Campaign identifiers never silently substitute (`PC-06`).
- Lucide for ordinary controls (`PC-13`).
- Accessible type floors, contrast, focus, forced colors, and reduced motion
  override undersized prototype microlabels and color-only state (`PC-12`).
- Semantic success and danger tokens exist for outcomes even though the
  prototype forbade red and green as brand voices.
- An accessible light theme maps the same semantic roles.
- Production routes, copy, and permissions follow the repository (`PC-09`,
  `PC-10`).

## Exception audit (Phase 3)

Purely visual v0.1 versus prototype conflicts adopt Shipboard. Behavior, flow,
semantic, accessibility, security, and IA conflicts adopt the repository. No
new escalation-threshold product question was found.

| ID | Conflict | Resolution |
| --- | --- | --- |
| `DS-X1` | Deep-Space identity vs Shipboard Terminal | Shipboard (owner-approved visual direction) |
| `DS-X2` | Prototype dark-only vs required light theme | Dark-first identity; light operational theme with mapped tokens |
| `DS-X3` | Electric blue/cyan vs teal/amber | Teal = system/context/live; amber = attention/commitment |
| `DS-X4` | Prototype no red/green vs outcome semantics | Keep success/danger tokens; never as brand; always pair with text/marks |
| `DS-X5` | Observation Glass vs smoked-glass planes | Smoked glass is the plate language; no blur under reading content |
| `DS-X6` | Prototype no icon library vs ADR-019 Lucide | `DS-DEC-10` / `PC-13` |
| `DS-X7` | Prototype microlabel sizes vs WCAG | Raise interactive/label floors; 400% zoom/reflow required |
| `DS-X8` | Prototype amber-only validation vs danger | Amber for field validation; danger for failed/destructive outcomes |
| `DS-X9` | Geist/Space Grotesk/IBM Plex vs Michroma/Sometype | `DS-PROP-2` |
| `DS-X10` | Soft radii vs zero-radius notches | `DS-DEC-9` |
