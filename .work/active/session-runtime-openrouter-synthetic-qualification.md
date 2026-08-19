---
id: session-runtime-openrouter-synthetic-qualification
status: planned
created: 2026-08-19
updated: 2026-08-20
---

# Goal

Implement and qualify the approved OpenRouter synthetic-development path behind
the provider-neutral Sessions execution port so a developer can make real,
bounded OpenRouter calls and conduct natural local text chat with synthetic,
non-sensitive content. Preserve Direct OpenAI behavior, frozen Session
authority, durable-before-display publication, Worker authorization, and
fail-closed provider controls.

# Governing sources

- `AGENTS.md`, `.work/README.md`, and the applicable implementation, backend,
  architecture, security/privacy, testing, and review skills
- `docs/operations/provider-profiles/openrouter-synthetic-development.md` —
  approved outcome, routing/privacy/credential/budget controls, evidence, and
  enablement limits
- `docs/architecture/decisions/ADR-008-bounded-oss-component-set.md` —
  `OSS-DEC-12`, `OSS-DEC-14`, `OSS-DEC-15`, and `OSS-DEC-17`
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
  — `STACK-DEC-11`, `STACK-DEC-18`, module ownership, supply chain, and
  `GATE-STACK-PROVIDERS`
- `docs/requirements/features/resolved-session-configuration.md` and
  `docs/requirements/features/session-text-lifecycle.md` — frozen authority,
  provider-neutral execution, Decision, streaming, provenance, cancellation,
  timeout, failure, durable publication, and recovery behavior
- `.work/active/session-runtime-live-provider-qualification.md` — implemented
  Direct OpenAI seams and explicitly separate Phase B evidence

# Scope

## In

- Inventory current provider-neutral contracts, Direct OpenAI adapter, host
  composition, installed profiles, credential catalog, tests, and additive
  versioning needs.
- Add a distinct versioned OpenRouter adapter kind and Sessions-owned adapter;
  preserve the Direct OpenAI adapter and its evidence.
- Model the fixed `https://openrouter.ai/api/v1` base path without weakening
  SSRF protections or accepting arbitrary runtime URLs.
- Map strict JSON Schema, streaming, no-fallback, required-parameter,
  data-collection-denial, ZDR, concrete-model, one-provider allowlist, router
  metadata, and response-cache-denial controls.
- Record and validate returned model, selected provider, and attempt count;
  reject missing metadata, cache hits, mismatch, fallback, or drift for a
  pinned development Session.
- Prove `openrouter/free` discovery cannot become frozen Session identity or
  mix models across control and visible-content phases.
- Reuse mounted-file secrets with OpenRouter-specific binding and no raw-secret
  persistence, telemetry, error, or artifact exposure; reject symlinks,
  non-regular files, and group/other directory or file permissions on Unix-like
  systems.
- Require an operator preflight confirming OpenRouter private input/output
  logging and use of inputs/outputs are disabled before opt-in live execution.
- Build default-off fake-transport contracts and an opt-in live harness with
  the approved request, concurrency, timeout, token, attempt, and cost bounds.
- Exercise real interactive local text chat with a concrete `:free` model after
  deterministic and discovery evidence pass.
- Retain sanitized evidence labeled only
  `qualified_for: synthetic_development`.

## Out

- Real Participant, customer, Submission, transcript, Evidence, Evaluation,
  Result, memory, or other sensitive data.
- Production qualification, automatic enablement, OpenRouter as a product
  default, or closing Direct OpenAI Phase B.
- Dynamic/free/latest routing in repeatable Sessions, silent fallback,
  multiple models in one Invocation, or relaxed privacy controls.
- Responses API beta, tools, web search, reasoning-trace capture, voice,
  Interaction Controller, or another MVP capability.
- Commits, pushes, deployments, releases, or production traffic unless
  separately requested.

# Plan

- [ ] Reconcile requirement IDs, adapter/host/profile seams, package locks,
      migration head, and threat model.
- [ ] Red — prove Direct OpenAI kind/origin-only behavior cannot safely
      represent OpenRouter `/api/v1`, routing policy, or random identity.
- [ ] Green — add the minimum versioned OpenRouter adapter/profile and
      path-aware allowlisted transport without changing domain policy.
- [ ] Red/green — cover strict structured output, streaming, selected-provider
      metadata, additive unknown metadata, diagnostic generation lookup, cache
      denial, privacy, fallback denial, timeout, cancellation, retries,
      usage/provenance, malformed responses, rate limits, outage, and errors.
- [ ] Red/green — cover mounted key directory/file type, symlink, permissions,
      privacy preflight, host composition, readiness, default-off, budget, and
      secret leakage.
- [ ] Run bounded discovery, select a concrete eligible `:free` model and
      provider, and execute the approved live synthetic qualification matrix.
- [ ] Exercise natural local chat through the real API, Worker, PostgreSQL,
      SSE, and Participant Text Session using synthetic content.
- [ ] Run focused, integration, architecture, locked regression, supply-chain,
      OCI, documentation, whitespace, and applicable Playwright verification.
- [ ] Obtain independent backend, architecture, and security/privacy review,
      resolve findings, reconcile status docs, and retain sanitized evidence.

# Current state

Planned. Architecture and operations approval is complete. No OpenRouter code,
profile schema, host composition, fake-transport suite, live harness, or
qualification evidence exists. Deterministic implementation can begin without
a key; the opt-in live stage requires the mounted key outside the repository.

# Decisions

- A distinct adapter is required; do not repurpose `direct_openai` or loosen
  its endpoint/model checks.
- `openrouter/free` is discovery/smoke only. Repeatable Session testing pins
  one concrete `:free` model and provider for both provider phases.
- Privacy, identity, capability, credential, and budget controls fail closed.
- Passing evidence remains synthetic-development-only.

# Findings / deviations

- Current `ApprovedHttpsOrigin` rejects a non-root path, while OpenRouter uses
  `/api/v1`; safe path handling needs an explicit adapter/profile contract.
- Current adapter kinds have no OpenRouter kind and the live-qualification test
  is only a Direct OpenAI opt-out sentinel.
- One Invocation may issue separate control and content requests, so random
  routing can create incoherent identity unless a model is selected first.
- The mounted-file source currently rejects traversal, symlinks, directories,
  empty/oversized values, and oversized decoded secrets, but does not enforce
  owner-only Unix directory/file modes.
- OpenRouter routing metadata is opt-in and absent on response-cache hits;
  repeatable evidence therefore requires the metadata header, explicit cache
  denial, one provider allowlist, and a fail-closed metadata check.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Architecture/operations approval | passed | ADR-008 `OSS-DEC-17`, ADR-010 `STACK-DEC-18`, and approved profile dated 2026-08-19 |
| Fake-transport provider contracts | pending | |
| Profile/credential/host isolation | pending | |
| Live synthetic qualification | pending | Requires mounted key; sanitized evidence only |
| Interactive local Text Session | pending | Synthetic, non-sensitive content only |
| Locked regression/supply chain/OCI/docs | pending | |
| Independent review | pending | |

# Blockers

No blocker for deterministic implementation. Live discovery, qualification,
and interactive chat require the owner-provided OpenRouter key through the
mounted-file secret boundary.

# Completion

- [ ] Planned work is reconciled with actual changes
- [ ] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [ ] Governing specifications were rechecked
- [ ] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
