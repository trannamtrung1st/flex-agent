# Operations

Operator-facing profiles, qualification evidence, and reference-deploy
pointers for Flex Agent.

## Status

**Approved** as the operations owner. Existing provider-profile contracts remain
live operator evidence. This index does not change product meaning, feature
requirements, or architecture contracts.

Architecture still-valid constraints from ADR-007 and ADR-008 that belong to
operations live here as **pointers**. ADR files remain until Phase 5 and are
not the current operations catalog. ADR-010 contribution and verification
rows live in [workspace development](../contributing/workspace.md) and
[`build/toolchain.json`](../../build/toolchain.json).

## What this area does not authorize

- Product scope, MVP capabilities, or Result/Release policy
- Architecture topology beyond recording how operators run the selected
  reference path
- Enabling a model provider, Production, Staging, or real Participant data
- Deleting qualification phase records that still support the live pin

## ADR-007 — reference deploy pointers

The architecture owner remains
[MVP architecture — OSS-first](../architecture/mvp-architecture.md#oss-first-and-on-premises-portability)
and [ADR-007](../architecture/decisions/ADR-007-oss-first-self-hostable-deployment.md).
Operators use these current commands and origins:

| Need | Current pointer |
| --- | --- |
| Local/CI Compose lifecycle | [Workspace development](../contributing/workspace.md) `pnpm compose:validate`, `compose:up`, `compose:status`, `compose:down`, `compose:reset`, `compose:api:canonical`, `compose:api:candidate` |
| Attach without reseeding | [Development harness](../contributing/development-harness.md#attach-to-a-running-local-origin) |
| Canonical authenticated origin | `http://localhost:18080` (Compose nginx on `127.0.0.1`) |
| Candidate UI overlay | `http://localhost:5274` with matching `RedirectUri` |
| OCI image check | `bash build/scripts/verify-oci.sh` (root [README](../../README.md)) |
| Identity qualification | [Keycloak OIDC contract](provider-profiles/keycloak-oidc-contract.md) and `pnpm verify:oidc` |

The reference path is self-hosted and must not require a mandatory public-cloud
account. Kubernetes, Redis, and an external broker remain optional. A
production-pilot or high-availability claim still requires architecture
recovery gates and measured evidence; this directory does not certify them.

## ADR-008 — bounded profiles and default-off execution

Live operator facts stay in
[provider profiles](provider-profiles/README.md). Current rules this leaf
records without widening enablement:

- Model execution stays **default-off**
  (`Sessions:ModelExecution:Adapter=fail_closed`) until a separately qualified
  profile is enabled outside this reset.
- The approved generic adapter is `openai_compatible` /
  `sessions.openai_compatible.v1`. Example JSON under
  `docs/operations/provider-profiles/` is **not enablement**.
- OpenRouter has a distinct synthetic-development adapter and pin. It is not
  the generic OpenAI-compatible adapter and does not qualify production or
  real Participant data.
- Docker Compose is the local/CI and synthetic evaluation-pilot orchestrator
  selected by ADR-008. Exact image digests remain in Compose lock material.
- Each enabled live profile must still pass the applicable quality, privacy,
  security, identity, capacity, license, and operational gates (`GATE-STACK-PROVIDERS`
  and successors) before real use.

## Qualification evidence retention

Gate A classified the seven named OpenRouter phase files as
retain-until-Phase-3/5 recheck. This leaf **retains all seven**. They remain
human-readable qualification evidence for the current synthetic pin; they are
not a second product specification.

| File | Role |
| --- | --- |
| [phase9](provider-profiles/qualified/openrouter/synthetic-development-phase9-2026-08-20.md) | Historical probe record |
| [phase20](provider-profiles/qualified/openrouter/synthetic-development-phase20-2026-08-20.md) | Historical probe record |
| [phase21](provider-profiles/qualified/openrouter/synthetic-development-phase21-2026-08-20.md) | Historical verification ledger |
| [phase22](provider-profiles/qualified/openrouter/synthetic-development-phase22-2026-08-20.md) | Historical probe record |
| [phase24](provider-profiles/qualified/openrouter/synthetic-development-phase24-2026-08-21.md) | Corrective contract |
| [phase27](provider-profiles/qualified/openrouter/synthetic-development-phase27-2026-08-21.md) | Failed-closed retry |
| [phase28](provider-profiles/qualified/openrouter/synthetic-development-phase28-2026-08-21.md) | Current passing live pin evidence |

Canonical current pin and limits:
[OpenRouter synthetic-development profile](provider-profiles/openrouter-synthetic-development.md).

## Related documents

- [Derived current-state index](../current-state.md)
- [Documentation home](../README.md)
- [Architecture documentation](../architecture/README.md)
