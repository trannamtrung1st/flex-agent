---
id: participant-session-narrow-reflow
status: completed
created: 2026-08-27
updated: 2026-08-27
---

# Goal

Let narrow Text Session (≤760px, including 400% zoom short viewports) reflow with page scroll so transcript, composer, Transmit, and completion consequence stay reachable per `docs/ui-ux/text-session.md`.

# Governing sources

- `docs/ui-ux/text-session.md` (`AC-SESS-24`–`AC-SESS-26`; narrow/400% zoom reachability)
- `docs/ui-ux/design-system/foundation/layout.md`
- Review of `44daff5` (P2 narrow clip; P3 Submit Session E2E gap)

# Scope

## In

- Narrow `.console` shell (`≤760px`): page scroll like stacked Assignment
- E2E: short narrow viewport (320×256) + Submit Session scroll at 1440×500
- Unit CSS assertion for narrow session overflow contract

## Out

- Rail redesign, examiner-plate density, production Phase 8 migration

# Plan

- [x] Red: unit + E2E for narrow page scroll and Submit Session reachability
- [x] Green: narrow session `body { overflow: auto }`, `.console { height: auto }`
- [x] Update stylesheet digest and layout docs
- [x] Playwright MCP 320×256 narrow evidence
- [x] Focused design-lab unit + e2e

# Current state

Narrow session (`≤760px`) mirrors stacked Assignment: page scroll enabled, `.console` grows with content (`height: auto; min-height: 100dvh`), grid rows are `auto auto auto` so transcript/composer are not squeezed into a fractional row.

# Decisions

- Mirror stacked Assignment at `bp.session` rather than invent a new escape hatch.

# Findings / deviations

- None.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Design-lab unit | pass | `pnpm test:design-lab` — 35 tests |
| Design-lab e2e | pass | `pnpm test:e2e:design-lab -- e2e/design-lab/surfaces.spec.ts` — 11 passed |
| Impeccable adapters | pass | `python3 scripts/impeccable_context.py generate` then `check` |
| Playwright MCP 320×256 live | pass | body overflow auto; console ~2298px; page scrollable; Transmit reachable after scroll. `.playwright-mcp/page-2026-08-27T10-19-17-125Z.png` + scrolled transmit capture |

# Blockers

None.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [x] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
