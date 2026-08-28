# Retired UI/UX authority ledger

## Document metadata

| Field | Value |
| --- | --- |
| **Status** | Approved |
| **Owner** | Product Lead |
| **Approvers** | Product Lead, UI/UX Lead |
| **Required reviewers** | Architecture Lead, Security/Privacy reviewer |
| **Version** | 1.0 |
| **Effective date** | 2026-08-28 |
| **Audience** | Product, design, engineering, security/privacy, QA |
| **Governs** | Retirement of former P0 journey/interaction specifications as current UI/UX authority; Git provenance; successor approval rule |

This document is a **status and provenance ledger**. It is not a behavioral
specification. It does not authorize production page implementation.

## Decision

The six former P0 UI/UX specifications listed below are **retired as current
authority** as of 2026-08-28. They must not be used as implementation
requirements, approval evidence, or hidden style guides. Full historical text
is recoverable from Git at the recorded commits. Duplicate archived copies are
not kept in the live docs tree.

Replacement documents occupy the same canonical paths after reconstruction
from approved product and requirements sources. Until a replacement file is
Approved at its current version, that path must not be treated as live journey
authority.

## Retired documents

| Former document | Last current version | Last current Git commit | Retirement reason | Successor |
| --- | --- | --- | --- | --- |
| `docs/ui-ux/activity-campaign-journey.md` | Approved v0.3 | `eb9c39877d391b126af9e28c950ea0df4318878f` | Shipboard production UX reset; reconstruct IA from product/requirements | Same path, successor Approved v1.0 |
| `docs/ui-ux/assessment-campaign-setup.md` | Approved | `eb9c39877d391b126af9e28c950ea0df4318878f` | Same | Same path, successor Approved v1.0 |
| `docs/ui-ux/submission-attempt.md` | Approved v0.2 | `eb9c39877d391b126af9e28c950ea0df4318878f` | Same | Same path, successor Approved v1.0 |
| `docs/ui-ux/text-session.md` | Approved v0.5 | `eb9c39877d391b126af9e28c950ea0df4318878f` | Same | Same path, successor Approved v1.0 |
| `docs/ui-ux/evidence-evaluation-human-review.md` | Approved | `eb9c39877d391b126af9e28c950ea0df4318878f` | Same | Same path, successor Approved v1.0 |
| `docs/ui-ux/result-release.md` | Approved | `eb9c39877d391b126af9e28c950ea0df4318878f` | Same | Same path, successor Approved v1.0 |

Baseline repository revision when this ledger was prepared:
`8c729a12f776977f6957173a748787493bbf0836`.

## What remains current

| Document | Status | Role |
| --- | --- | --- |
| [UI/UX index](README.md) | Approved | Catalog of current vs retired vs design-system authority |
| [Design system](design-system/README.md) | Approved v1.0 | Shipboard visual language only |
| Approved product and P0 feature specifications | Approved | Observable behavior |
| [ADR-021](../architecture/decisions/ADR-021-production-frontend-reset-and-single-spa-topology.md) | Approved | Single-SPA topology and fail-closed publication |

Design-lab surfaces remain synthetic composition evidence. They are not
successor UI/UX specifications.

## Replacement approval flow

1. Reconstruct IA and journeys from product, MVP scope, and approved P0
   feature specifications — not by editing retired flows in place as if they
   were still current.
2. Author one canonical set under `docs/ui-ux/` without `legacy`, `new`, or
   `v2` parallel trees.
3. Obtain Product Lead and UI/UX Lead approval, with Architecture and
   Security/Privacy review for cross-concern consequences.
4. Only then may production pages be implemented against the replacement set.

## Prohibitions

- Do not implement production pages from retired blobs, `web-legacy/` page
  composition, or design-lab fixtures.
- Do not treat ADR-021 as a substitute for this Product/UI/UX decision.
- Do not restore retired text as current by reverting only the six files
  without a new Product/UI/UX approval.
- Voice interaction, interruption, playback, TTS, and the proposed text
  Interaction Controller remain deferred unless a separate product decision
  expands P0 and replans implementation.

## Git recovery

```text
git show eb9c39877d391b126af9e28c950ea0df4318878f:docs/ui-ux/activity-campaign-journey.md
git show eb9c39877d391b126af9e28c950ea0df4318878f:docs/ui-ux/assessment-campaign-setup.md
git show eb9c39877d391b126af9e28c950ea0df4318878f:docs/ui-ux/submission-attempt.md
git show eb9c39877d391b126af9e28c950ea0df4318878f:docs/ui-ux/text-session.md
git show eb9c39877d391b126af9e28c950ea0df4318878f:docs/ui-ux/evidence-evaluation-human-review.md
git show eb9c39877d391b126af9e28c950ea0df4318878f:docs/ui-ux/result-release.md
```

## Related

- [UI/UX documentation](README.md)
- [ADR-021](../architecture/decisions/ADR-021-production-frontend-reset-and-single-spa-topology.md)
