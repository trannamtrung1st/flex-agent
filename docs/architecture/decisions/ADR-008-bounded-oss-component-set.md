# ADR-008: Bounded OSS component set and provider/deployment defaults

## Status

Approved — 2026-08-06; amended 2026-08-08 and 2026-08-19

Component-family and provider-boundary selection is approved. A profile is not
production-certified until the applicable compatibility, security, recovery,
license, and supply-chain gates in this ADR pass with recorded evidence. Model
qualification applies to a concrete provider deployment profile and never makes
one model, provider, or runtime part of Flex Agent's product identity.

## Decision metadata

| Field | Value |
| --- | --- |
| **Owner** | Architecture Lead |
| **Decision owners** | Architecture Lead, Operations owner |
| **Approvers** | Product Lead, Architecture Lead, Operations owner, Security/Privacy reviewer |
| **Consulted perspectives** | Business analysis, architecture, security/privacy, documentation |
| **Last amended** | 2026-08-19 |
| **Amendment reference** | Product Lead approval on 2026-08-19 of the bounded OpenRouter synthetic-development profile; preserves the 2026-08-08 model-neutral provider-profile decision |
| **Resolves** | `Q-ARCH-14` and `Q-ARCH-15` in the [MVP architecture](../mvp-architecture.md#approved-decision-disposition); `Q-OSS-1` and `Q-OSS-2` in this ADR |
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
- Make the application, Agent, and Harness—not a bundled model—the durable
  product value, while allowing model/provider changes without domain changes.

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
| `OSS-DEC-12` | Use a provider-neutral `ModelProvider` contract with a deterministic fake, approved native provider adapters, and an OpenAI-compatible adapter for approved external or self-hosted endpoints. Direct OpenAI is the first implementation adapter; vLLM is an optional reference self-hosted runtime, not an exclusive runtime or model selection. | Provider and model changes remain outside domain policy. Dynamic/free routing is never used for a frozen real assessment. Additional native or protocol-compatible adapters may be added after contract, security, privacy, and supply-chain review. |
| `OSS-DEC-13` | Require pinned artifacts, immutable digests, SBOMs, vulnerability scanning, provenance where available, and controlled updates. | A product family/version in this ADR is not permission to use a floating image tag. |
| `OSS-DEC-14` | Support operator-provisioned deployment profiles and Organization-scoped BYOK profiles for installed adapters in the MVP. Preserve the same profile boundary for a later optional Organization-owned model endpoint, enabled only after its additional gates pass. | Raw keys are never stored in product records or entered by Participants. Adapter, model, and credential selection—and endpoint selection when enabled—are frozen and audited by reference; missing, revoked, mismatched, or cross-Organization bindings fail closed without silent fallback. |
| `OSS-DEC-15` | Do not select a normative model family, model artifact, quantization, or hardware envelope. Qualify concrete provider deployment profiles independently against the model-provider gate, and permit multiple qualified profiles to coexist. | Certification belongs to the exact adapter/provider/endpoint/model/version-or-fingerprint/capability/credential-policy combination. Replacing a model or adding an Organization model does not change domain contracts, but the new profile must pass its applicable gates before real use. |
| `OSS-DEC-16` | Keep `grafana/otel-lgtm` operator-pulled and optional for local development and CI; do not bundle or redistribute it in the MVP distribution. | LGTM is development infrastructure, not product runtime or the production monitoring stack. This resolves `Q-OSS-2` for the MVP without making a legal conclusion about future redistribution. |
| `OSS-DEC-17` | Permit real OpenRouter calls for local synthetic development under the approved [OpenRouter synthetic-development profile](../../operations/provider-profiles/openrouter-synthetic-development.md). `openrouter/free` may be used only for capability discovery and smoke testing; repeatable interactive Session testing must pin a concrete `:free` model and one permitted provider slug. The 2026-08-20 development amendment permits provider/OpenRouter retention and training only for intentional synthetic content behind explicit owner acceptance. | Natural local chat may exercise the real provider path with synthetic, non-sensitive content, but neither a random free-router result nor a pinned free model becomes production-qualified. The relaxed development data policy is not authorization for real Participant/customer data or Production/Staging. Direct OpenAI qualification remains separate, runtime enablement remains explicit, and every missing identity, synthetic-data-policy acceptance, credential, routing, or budget control fails closed. |

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
| Reference self-hosted model runtime | vLLM `0.23.x` candidate line | Apache-2.0 | Optional GPU-backed endpoint behind the OpenAI-compatible adapter. It is neither exclusive nor a model default; exact runtime and model artifacts belong to independently qualified deployment profiles. |

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
| Local exploratory development | Distinct OpenRouter adapter using its OpenAI-compatible Chat Completions surface; `openrouter/free` or a specific `:free` model may be used under the approved synthetic-development profile | Synthetic data only. Dynamic/free routing cannot create a frozen real assessment manifest, and OpenRouter must not be substituted into the Direct OpenAI adapter profile. |
| Deployment-managed external provider | An installed native or protocol-compatible adapter with an operator-managed endpoint/deployment and credential binding | No model is the product default. The exact profile must pass quality, structured-output, latency, privacy, identity, and capacity gates before real use. |
| Optional Organization-owned model extension | An installed approved adapter with an Organization-scoped endpoint/deployment, model reference, capability profile, and BYOK binding | Not an MVP acceptance dependency. Before enablement, the endpoint is operator-approved and server-resolved; Organization configuration cannot introduce arbitrary runtime code, unvalidated URLs, cross-Organization credentials, or silent fallback. |
| Self-hosted | An installed protocol-compatible or native adapter pointed to an operator-approved runtime such as vLLM | Each concrete runtime/model artifact profile pins source, revision, digest, quantization where applicable, hardware envelope, and license evidence before real use. No particular model family is required. |

OpenRouter's stable MVP integration surface is the OpenAI-compatible Chat
Completions contract. Its Responses API is beta and is not the portability
baseline. OpenRouter automatic fallbacks, latest aliases, and free-model routing
are disabled for real assessment Sessions. If OpenRouter is later approved for
real data, the configuration must pin the model and allowed provider route,
disable fallbacks, require supported parameters and approved data policy, and
record returned routing metadata.

For approved local synthetic development, `openrouter/free` is a discovery
router rather than a frozen model. It may select a different eligible model for
each request, so its returned model and selected provider must be recorded and it
must not be used as the resolved model identity of a repeatable Session. This is
especially important when one Agent Invocation performs separate structured-
control and participant-visible-content requests: both phases must use one
concrete pinned `:free` model for coherent Session testing. Capability
discovery may use the random router; interactive local chat must move to the
concrete model before it is represented as a repeatable Session test.

The synthetic profile must use Chat Completions with strict JSON Schema where
structured control is required, require support for every sent parameter,
disable fallbacks, explicitly allow data-collecting routes without enforcing
request-level ZDR under the approved synthetic-only risk acceptance, restrict
repeatable requests to one permitted provider, expose and validate router
metadata, disable response caching, and fail when no eligible route or
attestable identity exists. Only intentional synthetic, non-sensitive content
may cross the boundary. Real Participant/customer data and Production/Staging
remain prohibited. The key remains an operator-mounted file outside the
repository, and committed evidence contains sanitized identity, capability,
usage, latency, cost, and outcome facts only. Exact operating bounds and the
default-off progression are governed by the
[provider-profile runbook](../../operations/provider-profiles/openrouter-synthetic-development.md).

Every external or Organization-provided endpoint is a separate trust boundary.
Real participant data requires an approved provider data/retention policy, data
minimization, an approved `SecretSource` binding, and no unnecessary
provider-side state. A mutable model alias alone does not satisfy `REQ-RSC-32`;
the response must supply an immutable version or an architecture-approved
equivalent fingerprint.

### Credential modes and BYOK

The MVP supports two credential modes for an approved provider profile:

- `deployment_default`: an operator provisions one deployment-scoped secret;
  an Organization may use it only when its approved provider policy explicitly
  selects that binding.
- `organization_byok`: an operator provisions a separate Organization-scoped
  secret and binds it to that Organization and provider adapter. The same
  profile shape may later identify an approved Organization-owned
  endpoint/deployment and model when that extension is enabled.
  Product records store only opaque binding identifiers and permitted provider,
  endpoint, model, and capability metadata.

Both modes use the mounted-file `SecretSource` boundary. Raw API keys are never
accepted through browser/API payloads, stored in the database, configuration
snapshot, execution manifest, audit, logs, telemetry, exports, or support
artifacts. Administrator-visible state may show provider, credential mode,
binding status, owner scope, and rotation/revocation status, but never secret
material.

Adapter, endpoint/deployment, model, capability, and credential selection are
resolved from trusted Organization policy, frozen into the Session by
non-secret reference, and revalidated before model work. Activity and Session
scopes may narrow an allowed model or capability but cannot select another
endpoint or credential owner. A missing, revoked, cross-Organization,
unapproved-endpoint, or mismatched binding fails closed before Session start or
provider invocation. The runtime must not silently fall back between
Organization BYOK, the deployment default, OpenRouter, or another provider
because that would change payer, privacy, residency, quota, capabilities, and
reconstruction semantics.

Provider extensibility is configuration plus reviewed adapter code, not an
untrusted in-process plugin mechanism. The architecture preserves a later path
for an Organization to select its own model through an adapter installed and
allowlisted by the operator; that path is not an MVP acceptance dependency.
Arbitrary endpoint URLs from request/session input and Organization-uploaded
executable adapter code are prohibited. A self-service plugin installation
surface requires a separate threat model, signing/provenance policy, isolation
boundary, feature specification, and ADR.

Direct OpenAI is the first implemented external adapter, not the preferred or
core model. The same BYOK contract may support additional native or compatible
adapters after approval; none becomes an MVP domain dependency. OpenRouter
credentials and OpenRouter BYOK pass-through remain
synthetic-development-only in the MVP.

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

- Qualify each proposed provider deployment profile independently. Record its
  adapter and contract versions, provider and endpoint/deployment references,
  exact model identity and immutable version/fingerprint, capability profile,
  credential mode, applicable source/artifact/license identity, and measured
  latency, throughput, capacity, cost, and failure results. For self-hosted
  profiles, also record artifact digest, quantization tool/version/settings when
  applicable, hardware, driver/runtime versions, and memory use.
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
  or endpoint mismatch, unapproved/loopback/link-local/metadata endpoint denial,
  quota/rate-limit attribution, and denial of every silent fallback.
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
| Gateway | Ocelot or YARP | At ADR-008 approval the application language was not selected. ADR-010 later selected .NET, but these remain rejected because the gateway must stay a simple replaceable ingress rather than couple transport policy to application code. |
| Recovery | Bundled pgBackRest or restic | Defer. Use component-native facilities through operator configuration management; revisit only when recovery evidence demonstrates a gap. |
| PostgreSQL HA | Bundled Patroni and distributed configuration store | Defer until a multi-host failover/fencing spike and an actual packaged-HA requirement exist. |
| Model testing | Cursor SDK adapter | Reject for the MVP because its agent/tool/filesystem harness is broader than the required bounded text-provider contract. |
| Model aggregation | OpenRouter as the real-pilot default | Reject. Retain it for synthetic local exploration; dynamic routing and the additional data boundary complicate frozen-session reconstruction. |
| Orchestration | Kubernetes baseline | Reject for MVP. Preserve the worker/scheduler seam and use `kind` later for adapter conformance only. |

## Remaining open questions

None. Concrete provider deployment profiles remain subject to evidence gates,
but profile qualification is delivery work rather than an architecture question.

## Approved question disposition

| Prior ID | Approved disposition | Consequence |
| --- | --- | --- |
| `Q-OSS-1` | Do not designate one self-hosted or external model as the Flex Agent default. Certify concrete provider deployment profiles independently; support deployment-managed and Organization-BYOK profiles in the MVP and preserve a separately gated Organization-model extension seam. | The application and Harness remain the core value. Model changes do not require a product or domain redesign, while every real-data profile still requires immutable identity, capability, quality, privacy, security, license, capacity, and operational evidence. |
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

- Every deployment or Organization model profile requires its own qualification
  evidence; passing one profile does not certify another.
- Organization-provided endpoints increase endpoint-validation, credential
  isolation, data-policy, quota, and operability responsibilities.
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
- [OpenRouter API and free-model limits](https://openrouter.ai/docs/faq)
- [OpenRouter free-model router](https://openrouter.ai/docs/guides/routing/routers/free-router)
- [OpenRouter provider routing](https://openrouter.ai/docs/guides/routing/provider-selection)
- [OpenRouter structured outputs](https://openrouter.ai/docs/guides/features/structured-outputs)
- [OpenRouter router metadata](https://openrouter.ai/docs/guides/features/router-metadata)
- [OpenRouter zero-data-retention routing](https://openrouter.ai/docs/guides/features/zdr)
- [OpenRouter data collection](https://openrouter.ai/docs/guides/privacy/data-collection)
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
- [Submission and Attempts](../../requirements/features/submission-attempts.md)
