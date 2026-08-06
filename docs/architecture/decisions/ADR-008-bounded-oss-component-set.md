# ADR-008: Bounded OSS component set and provider/deployment defaults

## Status

Approved — 2026-08-06

Component-family and provider-boundary selection is approved. A profile is not
production-certified until the applicable compatibility, security, recovery,
license, and supply-chain gates in this ADR pass with recorded evidence. The
self-hosted model profile remains uncertified until `Q-OSS-1` approves the exact
Mistral Small 3.1 artifact, quantization, and measured hardware envelope.

## Decision metadata

| Field | Value |
| --- | --- |
| **Owner** | Architecture Lead |
| **Decision owners** | Architecture Lead, Operations owner |
| **Approvers** | Product Lead, Architecture Lead, Operations owner, Security/Privacy reviewer |
| **Consulted perspectives** | Business analysis, architecture, security/privacy, documentation |
| **Resolves** | `Q-ARCH-14` and `Q-ARCH-15` in the [MVP architecture](../mvp-architecture.md#approved-decision-disposition) |
| **Governs** | Reference infrastructure products, provider/deployment defaults, supported version lines, profile placement, compatibility evidence, and replacement policy |

This ADR does not change product meaning. Approved feature specifications and
the [MVP operational defaults](../../requirements/mvp-operational-defaults.md)
continue to govern observable behavior. This ADR selects implementations and
operator boundaries for the portable contracts established by
[ADR-007](ADR-007-oss-first-self-hostable-deployment.md).

## Context

ADR-007 requires one maintainable, self-hosted reference path rather than an
unbounded compatibility promise. The MVP architecture left identity,
relational storage, artifact storage, artifact safety, telemetry, secrets,
gateway, model-provider, recovery, high-availability, and local-orchestration
choices open.

The first implementation must remain small enough to deliver the executable
MVP while preserving explicit seams for stronger production facilities. Product
names, model catalogs, and patch releases change more quickly than application
contracts. This ADR therefore selects component families and responsibility
boundaries; each released profile pins exact artifacts and digests in a
separate machine-readable lock manifest.

## Confirmed constraints

- One relational primary owns authoritative product state, durable work, audit,
  and atomic boundaries.
- Artifact storage is private and never authorizes access by possession of an
  object key or storage credential.
- The identity provider authenticates people; Flex Agent owns Organization,
  relationship, resource, workflow, and Release authorization.
- An attachment may be accepted only after every validation step required by
  its frozen material-category policy succeeds. A required scanner being
  unavailable, stale, timed out, or inconclusive never causes acceptance.
- Telemetry excludes credentials and raw protected content.
- Secrets do not enter images, source control, logs, telemetry, or generated
  test artifacts.
- The gateway provides transport controls, not domain authorization.
- Model providers are external sensitive-data boundaries and never become
  workflow or evaluation authorities.
- Provider credentials resolve only from trusted deployment or Organization
  secret bindings. Participant, Session, Activity, or browser input can never
  supply or widen a credential binding, and credential failure never triggers
  an unapproved fallback to another payer or provider.
- Kubernetes, Redis, an external broker, and a custom infrastructure service
  remain unnecessary for the MVP until evidence justifies them.
- A profile cannot claim high availability, RPO, RTO, or recovery readiness
  without measured evidence.

## Decision drivers

- Small, reversible MVP footprint with explicit adapter boundaries.
- Maintained upstreams with public security-reporting and release histories.
- OSI-approved licenses for bundled OSS components and a reviewable transitive
  inventory.
- Official OCI artifacts or a reproducible, signed build path.
- Non-interactive configuration, health, upgrade, diagnostics, backup, and
  restore paths.
- Linux `amd64` and `arm64` support where the capability permits it.
- Failure behavior that preserves isolation, audit, integrity, and Release
  authorization.
- Provider and operator choice without leaking vendor semantics into domain
  contracts or durable exports.

## Approved decisions

| ID | Approved decision | Consequence |
| --- | --- | --- |
| `OSS-DEC-1` | Use Keycloak `26.7.x` as the reference human OIDC provider. | One issuer is configured per MVP deployment. Provider roles do not directly authorize Flex Agent resources. |
| `OSS-DEC-2` | Use PostgreSQL `18.x` as the authoritative relational store. | Flex Agent and Keycloak use separate databases and least-privilege roles; they share no application schema. |
| `OSS-DEC-3` | Use SeaweedFS `4.x` as the conditional S3-compatible artifact-store default, with Ceph RGW as the first fallback. | SeaweedFS remains selected only while the artifact contract and recovery gates pass. |
| `OSS-DEC-4` | Make malware scanning a configurable `ArtifactSafetyScanner` adapter and do not require an external scanner product for the text/Markdown-only MVP policy. | `disabled_by_approved_policy` and `required` are distinct states. Required scanning fails closed. |
| `OSS-DEC-5` | Use OpenTelemetry Collector Contrib `0.158.x` as the telemetry transport boundary. | Application components export OTLP and do not depend on a telemetry backend. |
| `OSS-DEC-6` | Offer `grafana/otel-lgtm` `0.29.x` (initially `0.29.2`) only as an optional local/CI profile. | Production telemetry is operator-owned and OTLP-compatible; LGTM and ELK are not product runtime dependencies. |
| `OSS-DEC-7` | Use a vendor-neutral mounted-file `SecretSource`; do not require OpenBao for the MVP. | Docker deployments use generated synthetic files locally and operator-mounted protected files outside local development. OpenBao and hardened Kubernetes Secret projections are later adapters. |
| `OSS-DEC-8` | Use NGINX stable `1.30.x` (initially `1.30.4`) as the reference gateway. | NGINX owns TLS and transport policy only. The gateway contract remains replaceable. |
| `OSS-DEC-9` | Use Docker Engine and the Compose Specification through Docker Compose `5.x` for local development, CI, and a single-host evaluation pilot. | The evaluation pilot is non-production and synthetic-data-only. Kubernetes is deferred; a later `kind` profile may test adapters but is not a production topology. |
| `OSS-DEC-10` | Make backup execution an operator/configuration-management responsibility using component-native facilities. | Flex Agent does not ship a backup manager, scheduler, UI, pgBackRest, or restic as an MVP baseline. |
| `OSS-DEC-11` | Do not package PostgreSQL HA for the MVP. | A synthetic evaluation pilot may be explicitly non-HA. A production pilot must satisfy ADR-006's resilience/recovery baseline or record an explicitly approved weaker target and risk acceptance. Patroni remains a later evidence-driven option. |
| `OSS-DEC-12` | Use a provider-neutral model contract with a fake provider, an OpenAI-compatible chat adapter, direct OpenAI as the first enabled external provider, OpenRouter for synthetic local exploration, and vLLM as the approved self-hosted runtime family. | Dynamic/free routing is never used for a frozen real assessment. Anthropic remains a later native adapter; Cursor SDK is not selected. |
| `OSS-DEC-13` | Require pinned artifacts, immutable digests, SBOMs, vulnerability scanning, provenance where available, and controlled updates. | A product family/version in this ADR is not permission to use a floating image tag. |
| `OSS-DEC-14` | Support an operator-provisioned deployment credential and optional Organization-scoped BYOK through opaque `SecretSource` bindings for an approved provider adapter. | Raw keys are never stored in product records or entered by Participants. Credential selection is frozen and audited by reference; missing/revoked BYOK fails closed without silent fallback. |
| `OSS-DEC-15` | Use `mistralai/Mistral-Small-3.1-24B-Instruct-2503` as the first self-hosted benchmark candidate behind vLLM. | This approves a benchmark target, not production model weights or a certified self-hosted profile. Each benchmark pins the upstream revision and derived artifact digest and measures the selected quantization and hardware. |
| `OSS-DEC-16` | Keep `grafana/otel-lgtm` operator-pulled and optional for local development and CI; do not bundle or redistribute it in the MVP distribution. | LGTM is development infrastructure, not product runtime or the production monitoring stack. This resolves `Q-OSS-2` for the MVP without making a legal conclusion about future redistribution. |

## Selected OSS components

| Capability | Selected family and initial line | License | Placement and boundary |
| --- | --- | --- | --- |
| Human identity and OIDC | Keycloak `26.7.x` (initially `26.7.0`) | Apache-2.0 | All profiles. Stable `(issuer, subject)` mapping; restricted administration surface. |
| Authoritative relational storage | PostgreSQL `18.x` (initially `18.4`) | PostgreSQL License | All profiles. Current supported patch in the tested line. |
| Private S3-compatible artifact storage | SeaweedFS `4.x` (candidate `4.29`) | Apache-2.0 | All profiles, conditional on exact-version, integrity, lifecycle, and restore gates. Ceph RGW is the first fallback. |
| Telemetry transport | OpenTelemetry Collector Contrib `0.158.x` | Apache-2.0 | Internal OTLP receiver with bounded queues/retries and an approved attribute allowlist. |
| Local/CI telemetry backend | `grafana/otel-lgtm` `0.29.x` (initially `0.29.2`) | AGPL-3.0 and Apache-2.0 components | Operator-pulled optional development convenience only; not bundled, redistributed, a production topology, or a mandatory product dependency. |
| Public gateway and TLS | NGINX stable `1.30.x` (initially `1.30.4`) | BSD-2-Clause | Public SPA, API, OIDC callback, and SSE routes only; infrastructure administration paths remain private. |
| Local orchestration | Compose Specification with Docker Compose `5.x` | Apache-2.0 | One non-interactive project command wraps validation, start, readiness, seed, stop, and reset. Docker Desktop is not required. |
| Self-hosted model runtime | vLLM `0.23.x` candidate line | Apache-2.0 | Optional GPU-backed provider behind the OpenAI-compatible contract. Runtime selection alone approves no model artifact; `OSS-DEC-15` separately selects a benchmark candidate without production certification. |
| Self-hosted model benchmark | `mistralai/Mistral-Small-3.1-24B-Instruct-2503` | Apache-2.0 | First text-only benchmark candidate. Start with an exact pinned revision and a quantized single-24-GB-NVIDIA-GPU profile; no production or quality certification is implied. |

## Artifact-safety policy and adapter

The MVP enables direct text plus validated UTF-8 `.txt` and `.md`
attachments. Its mandatory intake pipeline covers configured count and size,
declared and detected type, strict UTF-8, content structure, integrity, archive
and active-content denial, safe preview behavior, and immutable association.

The `ArtifactSafetyScanner` port returns a bounded outcome such as `clean`,
`rejected`, `inconclusive`, or `unavailable`, plus permitted engine/policy
provenance. The frozen material-category policy resolves one of two modes:

- `disabled_by_approved_policy`: permitted for the initial narrowly constrained
  text/Markdown categories. No record may imply that an external scanner ran.
- `required`: acceptance waits for a `clean` result. Missing, stale, timed-out,
  unavailable, or inconclusive scanning fails closed.

ClamAV is a compatible future adapter candidate, not a bundled or required MVP
component. Enabling another material category requires an approved complete
parser/malware policy and negative tests before accepting that category.

## Secrets boundary

Application code consumes read-only files through a `SecretSource` port and has
no OpenBao, Kubernetes, or host-secret-manager SDK dependency.

- Local/CI uses generated synthetic secret files excluded from source control,
  images, logs, telemetry, support bundles, and test artifacts.
- A Docker evaluation or production pilot uses operator-mounted protected files with documented
  ownership, permissions, injection, rotation, revocation, and cleanup.
- A later Kubernetes profile may use projected Kubernetes Secret volumes only
  with encryption at rest, least-privilege RBAC, namespace/workload isolation,
  backup protection, and a tested rotation/reload procedure.
- OpenBao may be added when central audit, dynamic credentials, rotation, or a
  non-Kubernetes production environment justifies its operational ceremony.

## Gateway contract

NGINX terminates TLS and enforces only approved transport behavior:

- public-route allowlist and private infrastructure administration paths;
- request/body/connection limits, positive timeouts, and SSE proxy behavior;
- certificate handling and renewal procedure;
- replacement of untrusted forwarding and correlation headers at the boundary;
- health routing without exposing sensitive diagnostics.

Authentication and every Organization, Activity, Participant, Session,
Evaluation, Result, and Release authorization decision remain in Flex Agent.
Kong may be reconsidered when centralized API-consumer, plugin, quota, or
multi-service policy needs justify it. Ocelot and YARP are not selected because
they would prematurely couple the gateway to .NET application code.

## Recovery responsibility

Backup orchestration is not an application feature or bundled MVP service.

Flex Agent owns:

- the authoritative-data inventory and recovery order;
- exportable schemas, migrations, version compatibility, and stable object
  identities/digests;
- a documented maintenance/quiesce procedure for the simple MVP recovery path;
- component-native backup/restore guidance and post-restore lineage,
  isolation, digest, and seal verification;
- truthful profile limitations and measured evidence before any RPO/RTO claim.

The operator or external configuration-management system owns:

- scheduling, destinations, credentials, encryption/key custody, retention,
  monitoring, off-host/off-site transfer, and restore-drill scheduling;
- PostgreSQL-native logical backup/restore and, when required by approved
  recovery targets, physical backup plus WAL-based point-in-time recovery;
- artifact-store-native replication, snapshot, or export plus inventory/digest
  verification;
- Keycloak-supported configuration export where needed, while also protecting
  its PostgreSQL database.

The first simple evaluation-pilot procedure may stop or quiesce writes, back up
PostgreSQL and the artifact store through their native facilities, preserve
required identity configuration, then verify an isolated restore. A production
pilot must complete the stronger automated and point-in-time recovery evidence
required by ADR-006; an explicitly approved weaker target must be published as
such rather than represented as the production baseline.

## Model-provider strategy

The application-owned `ModelProvider` contract covers bounded text input,
streaming output, structured-output capability, cancellation, timeout, usage,
normalized failure, resolved model/provider identity, and protected request
correlation. It exposes no arbitrary model tools in the text-only MVP.

Approved profiles are:

| Profile | Provider configuration | Constraint |
| --- | --- | --- |
| Automated tests | Deterministic fake/in-memory provider | Required for repeatable tests; no remote free model is a deterministic test oracle. |
| Local exploratory development | OpenAI-compatible adapter pointed to OpenRouter; `openrouter/free` or a specific `:free` model may be used | Synthetic data only. Dynamic/free routing cannot create a frozen real assessment manifest. |
| Provider-backed evaluation and production-pilot candidate | Direct OpenAI through an approved deployment or Organization BYOK binding; `gpt-5.6-terra` is the initial balanced candidate | Evaluation uses synthetic data. A production pilot additionally requires assessment-quality, structured-output, latency, privacy, immutable-version/fingerprint, and production-profile gates. |
| Future second external provider | Native Anthropic adapter | Deferred until the MVP vertical slice works; used to prove portability rather than block it. |
| Self-hosted | OpenAI-compatible adapter pointed to vLLM with Mistral Small 3.1 24B Instruct as the first benchmark candidate | Requires an exact pinned revision, approved quantization and derived artifact digest, measured hardware envelope, license verification, and passing quality results before production certification. |

OpenRouter's stable MVP integration surface is the OpenAI-compatible Chat
Completions contract. Its Responses API is beta and is not the portability
baseline. OpenRouter automatic fallbacks, latest aliases, and free-model routing
are disabled for real assessment Sessions. If OpenRouter is later approved for
real data, the configuration must pin the model and allowed provider endpoint,
disable fallbacks, require supported parameters and approved data policy, and
record returned routing metadata.

Direct OpenAI remains a separate external trust boundary. Real participant data
requires an approved provider data/retention policy, data minimization, an
approved `SecretSource` binding, and no unnecessary provider-side state. A
mutable model alias alone does not satisfy `REQ-RSC-32`; the response must
supply an immutable version or an architecture-approved equivalent fingerprint.

### Credential modes and BYOK

The MVP supports two credential modes for an approved external provider:

- `deployment_default`: an operator provisions one deployment-scoped secret;
  an Organization may use it only when its approved provider policy explicitly
  selects that binding.
- `organization_byok`: an operator provisions a separate Organization-scoped
  secret and binds it to that Organization and provider adapter. Product records
  store only an opaque secret-binding identifier and permitted provider metadata.

Both modes use the mounted-file `SecretSource` boundary. Raw API keys are never
accepted through browser/API payloads, stored in the database, configuration
snapshot, execution manifest, audit, logs, telemetry, exports, or support
artifacts. Administrator-visible state may show provider, credential mode,
binding status, owner scope, and rotation/revocation status, but never secret
material.

Provider and credential selection is resolved from trusted Organization policy,
frozen into the Session by non-secret reference, and revalidated before model
work. Activity and Session scopes may narrow an allowed model but cannot select
another credential owner. A missing, revoked, cross-Organization, or mismatched
binding fails closed before Session start or provider invocation. The runtime
must not silently fall back between Organization BYOK, the deployment default,
OpenRouter, or another provider because that would change payer, privacy,
residency, quota, and reconstruction semantics.

Direct OpenAI is the first enabled real-data external adapter. The same BYOK
contract may support Anthropic after its native adapter is approved; this does
not make Anthropic an MVP delivery dependency. OpenRouter credentials and
OpenRouter BYOK pass-through remain synthetic-development-only in the MVP.

Cursor SDK is not selected. Its agent harness, filesystem/tool permissions,
public-beta stability, and usage model add complexity not required for bounded
text generation.

### Pilot terminology

- An **evaluation pilot** is a synthetic-data-only, non-production exercise of
  the reference deployment. It may be single-host and non-HA, and it makes no
  production durability, RPO, RTO, privacy, or availability claim.
- A **production pilot** processes real participant data or supports operational
  use. It must satisfy ADR-006's resilience and recovery baseline plus this
  ADR's security, provider/privacy, credential-isolation, upgrade, restore, and
  release gates, unless an authorized owner explicitly approves and publishes a
  weaker target and its risk acceptance.

Calling an environment a pilot does not reduce its controls. The data and use
classification determines which profile and evidence gates apply.

## Profile composition

| Profile | Included defaults | Explicit limitation |
| --- | --- | --- |
| Local development and CI | NGINX, Keycloak, PostgreSQL, SeaweedFS, OpenTelemetry Collector, optional `otel-lgtm`, synthetic mounted secrets, Docker Compose, disabled-by-approved-policy external scanning, fake provider for automation, and optional OpenRouter free models for manual synthetic testing | Disposable and single-host. No real participant data, production security, durability, HA, RPO, or RTO claim. |
| On-premises single-host evaluation pilot | Same application contracts and reference components; operator-mounted secrets; operator-owned OTLP backend; component-native backup procedure; explicitly selected scanner mode; fake provider or direct OpenAI | Synthetic data only; explicitly non-production and non-HA, with no durability, RPO, or RTO claim. |
| Production-pilot candidate | Redundant stateless application and gateway instances; operator-supplied PostgreSQL HA and replicated artifact storage; operator secret and telemetry facilities; external recovery copies; approved model provider and credential binding | Orchestrator-neutral and uncertified until security, provider/privacy, credential isolation, upgrade, backup/restore, load, failover, and recovery gates pass. |
| Optional managed deployment | Compatible managed identity, PostgreSQL, S3, secret, OTLP, and model services behind the same contracts | Every substitute needs contract tests and must preserve data meaning, authorization, export, and recovery portability. |

## Required evidence gates

### Identity and relational gates

- Exercise Authorization Code with PKCE and server-side exchange, issuer and
  audience validation, administrator/reviewer MFA, logout, revocation, key
  rotation, clock skew, account disablement, and provider outage.
- Keep Keycloak administration, health, and metrics on restricted routes.
- Run migrations, isolation, uniqueness, append-only, idempotency, durable-work,
  outbox-ordering, and wrong-Organization tests on PostgreSQL 18.
- Prove the native backup/restore path, checksum verification, major-version
  rehearsal, and restoration of audit and outcome lineage.

### Artifact and safety gates

- Contract-test multipart upload, conditional create, exact-version read,
  integrity metadata, versioning, lifecycle behavior, expired-capability denial,
  credential rotation, and cross-Organization object substitution.
- Restore database metadata and artifact data together, then verify every
  accepted object against its recorded version identity and digest.
- Reject SeaweedFS and reopen Ceph RGW if immutability, version identity,
  lifecycle, or restore behavior fails.
- Test type confusion, misleading extensions, invalid UTF-8, truncated and
  oversized input, disallowed active/binary/archive content, parser limits, and
  every configured scanner state. A required scanner failure never accepts.

### Telemetry, secret, and gateway gates

- Enforce an attribute allowlist and leakage tests for Submissions, prompts,
  transcripts, Evidence, Evaluations, reviewer notes, Results, tokens, secrets,
  emails, and high-cardinality Participant identifiers.
- Test collector backpressure and sink outage so telemetry cannot replace audit
  or block an otherwise committed protected mutation.
- Verify mounted-secret permissions, injection, rotation/reload, revocation,
  cleanup, and absence from arguments, environment dumps, logs, telemetry,
  crashes, and support bundles.
- Test TLS, renewal, limits, timeouts, SSE, forwarded-header spoofing, public
  route allowlists, and denial of infrastructure administration paths.

### Model-provider gate

- Benchmark the exact pinned Mistral Small 3.1 24B Instruct revision first,
  starting with a quantized single-24-GB-NVIDIA-GPU profile. Record the source
  revision, derived artifact digest, quantization tool/version/settings, GPU,
  driver/runtime versions, memory use, throughput, latency, and failure results.
- Verify streaming assembly, cancellation, timeout, bounded retry, structured
  output, usage capture, error normalization, and provider outage behavior.
- Record provider, requested and resolved model identifiers, immutable
  version/fingerprint, adapter version, relevant generation parameters, and
  protected request correlation in the execution manifest.
- Test that provider output, prompt injection, and provider-side instructions
  cannot authorize workflow transitions, tools, cross-scope access, Evaluation
  completion, Review decisions, or Release.
- Prove the configured provider's data policy before real participant data and
  reject dynamic/free routing for frozen assessment Sessions.
- Test deployment-default and Organization-BYOK selection, wrong-Organization
  substitution, missing/revoked/rotated bindings, concurrent rotation, provider
  mismatch, quota/rate-limit attribution, and denial of every silent fallback.
- Verify that raw credentials never enter product persistence, manifests, audit,
  logs, telemetry, exports, browser state, errors, or test artifacts.

## Version and supply-chain policy

Every released profile must include one machine-readable component lock with:

- component name, upstream source, license identifier, exact semantic version,
  OCI registry, immutable digest, architecture, and retrieval date;
- signature or provenance result where upstream provides it;
- generated SBOM and vulnerability scan result;
- configuration-schema and Flex Agent adapter-contract versions;
- approved exception owner, reason, expiry, and compensating control for any
  unresolved vulnerability or license finding.

Floating tags, including `latest`, are prohibited in a released profile. Patch
updates require focused contract, upgrade, and restore tests. A meaning-changing
minor/major update, license change, archived upstream, or loss of security
maintenance requires architecture and security/privacy review. Critical
exploitable vulnerabilities block release unless a time-bounded approved
exception records exposure, controls, owner, and remediation date.

## Options considered

| Capability | Alternative | Disposition |
| --- | --- | --- |
| Identity | Dex, Authentik, or ZITADEL | Keep as future adapter candidates; one reference identity product bounds the MVP security and migration surface. |
| Object storage | Ceph RGW | First fallback if SeaweedFS fails; not the first default because of higher minimum operator burden. |
| Malware scanning | Bundled ClamAV service | Deferred. Keep as an `ArtifactSafetyScanner` adapter candidate; the constrained text/Markdown MVP uses approved deterministic validation. |
| Secrets | Mandatory OpenBao | Deferred. Mounted-file `SecretSource` avoids secret-service coupling and unseal/recovery ceremony in the MVP. |
| Gateway | Kong | Defer until centralized API-consumer, plugin, quota, analytics, or multi-service policy needs justify its operational surface. |
| Gateway | Caddy | Viable, but NGINX was selected as the simpler familiar reference requested by the decision owner. |
| Gateway | Ocelot or YARP | Reject for now because the application language is not selected and either choice would couple the gateway to .NET. |
| Recovery | Bundled pgBackRest or restic | Defer. Use component-native facilities through operator configuration management; revisit only when recovery evidence demonstrates a gap. |
| PostgreSQL HA | Bundled Patroni and distributed configuration store | Defer until a multi-host failover/fencing spike and an actual packaged-HA requirement exist. |
| Model testing | Cursor SDK adapter | Reject for the MVP because its agent/tool/filesystem harness is broader than the required bounded text-provider contract. |
| Model aggregation | OpenRouter as the real-pilot default | Reject. Retain it for synthetic local exploration; dynamic routing and the additional data boundary complicate frozen-session reconstruction. |
| Orchestration | Kubernetes baseline | Reject for MVP. Preserve the worker/scheduler seam and use `kind` later for adapter conformance only. |

## Remaining open question and interim default

| ID | Question and owner | Interim default | Rationale and approval impact |
| --- | --- | --- | --- |
| `Q-OSS-1` | Architecture, AI Operations, Product, and license owners: Which exact Mistral Small 3.1 revision, quantization, derived artifact, and hardware envelope certify the vLLM self-hosted path? | Benchmark `mistralai/Mistral-Small-3.1-24B-Instruct-2503` first using an immutable upstream revision and derived artifact digest, starting with a quantized single-24-GB-NVIDIA-GPU profile. Continue using direct OpenAI for provider-backed evaluation or a production-pilot candidate until license, assessment quality, structured output, privacy, latency, capacity, and artifact-identity gates pass. | Candidate approval narrows the spike but does not approve production weights, quantization, hardware capacity, or quality. No self-hosted production-model claim may be made until this question is resolved. |

## Approved question disposition

| Prior ID | Approved disposition | Consequence |
| --- | --- | --- |
| `Q-OSS-2` | Keep LGTM as an optional operator-pulled local/CI image and do not bundle or redistribute it in the MVP distribution. | The licensing question does not block MVP implementation or production architecture. Production remains backend-neutral through OTLP. Any future redistribution requires a fresh license review and architecture/distribution decision. |

## Consequences

### Positive

- The executable MVP architecture has one bounded, reproducible default set without a
  mandatory cluster, secret server, scanner service, backup platform, or
  production observability stack.
- Provider-neutral boundaries are exercised by direct, aggregated, and
  self-hosted-compatible configurations without becoming domain authority.
- Operator responsibilities are explicit and do not block the feature slice.
- Conditional selections and profile limits prevent development conveniences
  from being represented as production readiness.

### Costs and risks

- The first production-pilot candidate depends on an external model provider until a model
  artifact and hardware envelope pass the vLLM gate.
- OpenRouter free models and aliases are useful but non-deterministic and are
  restricted to synthetic exploratory use.
- Deferring a malware engine is acceptable only while enabled categories remain
  narrowly constrained and the policy state is explicit.
- Mounted-file secrets and external backup automation reduce bundled components
  but increase the importance of operator runbooks and verification.
- SeaweedFS remains conditional; failed recovery evidence may force Ceph RGW.

## Upstream evidence reviewed

- [Keycloak release notes and supported configurations](https://www.keycloak.org/docs/latest/release_notes/)
- [PostgreSQL versioning policy](https://www.postgresql.org/support/versioning/)
- [PostgreSQL backup and restore](https://www.postgresql.org/docs/current/backup.html)
- [SeaweedFS source, license, and S3 capabilities](https://github.com/seaweedfs/seaweedfs)
- [OpenTelemetry Collector releases](https://github.com/open-telemetry/opentelemetry-collector-releases/releases)
- [Grafana Docker OpenTelemetry LGTM scope](https://grafana.com/docs/opentelemetry/docker-lgtm/)
- [Grafana OpenTelemetry LGTM image tags](https://hub.docker.com/r/grafana/otel-lgtm/tags)
- [NGINX stable releases](https://nginx.org/en/download.html)
- [Compose Specification](https://www.compose-spec.io/)
- [Docker Compose releases](https://github.com/docker/compose/releases)
- [Kubernetes Secret good practices](https://kubernetes.io/docs/concepts/security/secrets-good-practices/)
- [vLLM documentation](https://docs.vllm.ai/en/latest/)
- [vLLM supported models](https://docs.vllm.ai/en/latest/models/supported_models/)
- [Mistral Small 3.1 24B Instruct model card](https://huggingface.co/mistralai/Mistral-Small-3.1-24B-Instruct-2503)
- [OpenRouter API and free-model limits](https://openrouter.ai/docs/faq)
- [OpenRouter Responses API beta](https://openrouter.ai/docs/api/reference/responses/overview)
- [OpenAI model guidance](https://developers.openai.com/api/docs/guides/latest-model)
- [OpenAI API data controls](https://developers.openai.com/api/docs/guides/your-data)

## Related

- [MVP architecture](../mvp-architecture.md)
- [ADR-006: MVP architecture baseline and evolution](ADR-006-mvp-architecture-baseline-and-evolution.md)
- [ADR-007: OSS-first self-hostable deployment](ADR-007-oss-first-self-hostable-deployment.md)
- [MVP operational defaults](../../requirements/mvp-operational-defaults.md)
- [Authorization and resource isolation](../../requirements/features/auth-resource-isolation.md)
- [Resolved Session configuration](../../requirements/features/resolved-session-configuration.md)
- [Resolved Session configuration](../../requirements/features/resolved-session-configuration.md)
- [Submission and Attempts](../../requirements/features/submission-attempts.md)
