# ADR-007: OSS-first self-hostable deployment

## Status

Approved — 2026-08-06

## Context

Flex Agent must be easy to deploy, test, automate, and operate without tying
its core workflow to one cloud provider. Future customers may require a
deployment wholly inside an Organization-controlled environment. Contributors
and coding Agents also need a deterministic setup that does not depend on
interactive cloud consoles or private managed services.

The MVP architecture already separates the SPA, API, worker, transactional
store, artifact store, OIDC provider, model provider, and optional cache. This
decision establishes the portability rule for those boundaries without
prematurely selecting individual products.

OSS-first does not mean implementing commodity infrastructure inside Flex
Agent. It means preferring maintained open-source components and open contracts,
and ensuring that every required platform capability has a supported
self-hosted path.

## Decision drivers

- Support on-premises Organization and business deployments.
- Avoid mandatory cloud accounts, proprietary control planes, and provider
  lock-in.
- Make local development, automated testing, and coding-Agent operation
  reproducible and non-interactive.
- Preserve portable data, backups, configuration, and deployment artifacts.
- Keep cloud-managed services available as optional operational choices.
- Avoid adding Kubernetes, Redis, a broker, or custom infrastructure before a
  demonstrated need.

## Options considered

| Option | Advantages | Disadvantages |
| --- | --- | --- |
| Cloud-managed-first reference architecture | Low initial operations burden for one provider | Conflicts with on-premises deployment, makes testing depend on external services, and creates migration and lock-in risk |
| Support cloud and on-premises through unrelated stacks | Allows each environment to optimize independently | Doubles integration, upgrade, testing, and failure-mode surfaces and encourages behavioral drift |
| OSS-first, self-hostable reference architecture with optional managed adapters | One portable baseline, local reproducibility, and deployment choice | Flex Agent must own packaging, upgrade, backup, diagnostics, and compatibility guidance |
| Build databases, identity, storage, scheduling, and observability as application modules | Maximum direct control | Creates unsafe custom infrastructure and an unsustainable maintenance burden |

## Decision

Flex Agent adopts an **OSS-first, self-hostable, open-standard deployment
baseline**.

[ADR-008](ADR-008-bounded-oss-component-set.md) approves the current component
families and model-neutral provider boundaries. That selection establishes an
implementable self-hosted architecture without making a model part of the
product identity. Each claimed provider deployment profile—external,
Organization-provided, or self-hosted—requires its own immutable identity,
quality, privacy, security, capacity, license, and operational evidence.

1. The complete MVP workflow must be deployable without a mandatory public
   cloud account or proprietary managed service.
2. Core application runtimes are distributed as portable OCI container images.
   Runtime state must not depend on a particular node or container filesystem.
3. Required infrastructure capabilities must have a maintained self-hosted
   implementation path. Managed cloud services may implement the same contracts
   but remain optional.
4. Integration boundaries use broadly implemented standards and portable
   contracts where suitable: OIDC for human identity, HTTP/SSE for application
   traffic, SQL/relational semantics for the authoritative store,
   S3-compatible object operations for protected artifacts, OCI for runtime
   packaging, and OpenTelemetry-compatible telemetry export.
5. Provider-specific SDKs, identities, resource names, and control-plane
   concepts remain behind infrastructure adapters. They must not enter domain
   policy, durable workflow meaning, authorization, or exported product data.
6. Deployment configuration must be externalized, machine-readable,
   non-interactive, and validatable. Secrets are injected through the selected
   deployment's protected secret facility and are never committed to images,
   repositories, logs, or generated test artifacts.
7. The reference development and test profile must provide deterministic
   bootstrap, health/readiness checks, migrations, synthetic seed data, and
   documented reset/recovery procedures suitable for both people and coding
   Agents. No required setup step may exist only in a graphical console.
8. The production on-premises profile must document installation, upgrades,
   schema migration, rollback or forward recovery, backup/restore,
   observability, TLS, identity integration, capacity, and component
   compatibility. High-availability claims require measured deployment-specific
   evidence.
9. Each distribution must identify component versions and licenses and produce
   a software bill of materials. Component selection must consider maintenance,
   security response, license compatibility, upgrade path, and data portability,
   not OSS status alone.
10. Data export and backup procedures must use documented formats and preserve
    Organization scope, immutable identities, integrity metadata, and lineage.
    A deployment must be recoverable without access to a former provider's
    proprietary control plane.
11. Kubernetes, Redis, and an external broker remain optional. They may be added
    through the approved worker, cache, and work-delivery seams when evidence or
    an approved feature justifies them.
12. A dedicated on-premises deployment for one Organization does not bypass
    application authorization, audit, Activity, Participant, or Session
    isolation invariants.

## Initial deployment profiles

[ADR-008](ADR-008-bounded-oss-component-set.md) selects the current bounded
component families, provider/credential boundaries, and version policy. The
supported portable shape remains fixed here:

| Profile | Required property |
| --- | --- |
| Local development and automated test | One documented, non-interactive command starts the application and self-hosted dependencies from pinned artifacts; deterministic health checks and synthetic data verify readiness |
| On-premises evaluation pilot | Uses the same contracts in a synthetic-data-only, explicitly non-production single-host profile; Kubernetes and HA are not required |
| Production-pilot candidate | Uses the same application images and contracts with Organization-operated identity, relational storage, object storage, telemetry, secrets, TLS, and backup facilities; Kubernetes is not required, but ADR-006 resilience and recovery gates still apply |
| Optional cloud deployment | May replace infrastructure adapters with managed equivalents without changing domain behavior, protected data meaning, or public application contracts |

ADR-008 selects Docker Compose for local/CI and the evaluation pilot. Exact
artifacts remain subject to its compatibility, security, recovery, license, and
supply-chain evidence gates; no selection makes Kubernetes an MVP prerequisite.

## Consequences

### Positive

- Local, CI, on-premises, and optional cloud deployments exercise the same
  application boundaries.
- Organizations retain infrastructure and provider choice.
- Coding Agents can bootstrap and diagnose environments through documented
  commands and machine-readable state.
- Future Kubernetes scheduling and isolated Agent work can reuse OCI images and
  durable work contracts.

### Costs and risks

- The project must maintain installation, compatibility, migration, backup,
  observability, and upgrade documentation rather than delegating all operating
  concerns to one provider.
- Supporting arbitrary products is not feasible. Flex Agent will certify a
  bounded compatibility matrix and treat other compatible implementations as
  unverified until tested.
- Self-hosting transfers patching, capacity, identity, backup, malware scanning,
  model serving, and incident-response duties to the operator.
- Air-gapped installation is not implied by on-premises support. It requires a
  separate approved requirement for offline artifact mirroring, updates,
  licensing, model availability, and vulnerability intelligence.

## Verification

- Run the P0 workflow and negative isolation suite using only the reference
  self-hosted profile.
- Build and inspect pinned OCI images and the component/license SBOM.
- Recreate a clean environment through documented non-interactive commands.
- Restore database and artifact backups without a cloud-provider control plane.
- Run adapter contract tests against every certified identity, relational,
  artifact, model, and telemetry implementation.
- Verify that provider names and infrastructure identifiers do not appear in
  domain contracts, authorization decisions, or portable exports.

## Related

- [MVP architecture](../mvp-architecture.md)
- [ADR-006: MVP architecture baseline and evolution](ADR-006-mvp-architecture-baseline-and-evolution.md)
- [ADR-008: Bounded OSS component set and provider/deployment defaults](ADR-008-bounded-oss-component-set.md)
- [Authorization and resource isolation](../../requirements/features/auth-resource-isolation.md)
- [Approved MVP operational defaults](../../requirements/mvp-operational-defaults.md)
- [MVP scope](../../product/mvp-scope.md)
