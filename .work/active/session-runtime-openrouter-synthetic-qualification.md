---
id: session-runtime-openrouter-synthetic-qualification
status: in-progress
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

# Requirement and acceptance traceability

| Governing behavior | Implementation surface | Planned verification |
| --- | --- | --- |
| `REQ-RSC-46`, `AC-RSC-25` — trusted frozen profile and opaque credential binding; fail closed on mismatch | Common installed-profile digest, OpenRouter profile registry, credential catalog, mounted-file secret source, Worker composition | Frozen-binding positive/negative tests; wrong Organization/provider/mode/version/digest; revoked/missing credential; no raw secret in records, errors, telemetry, or artifacts |
| `REQ-SESS-16`–`REQ-SESS-18`, `REQ-SESS-55`–`REQ-SESS-60`, `AC-SESS-5`–`AC-SESS-7`, `AC-SESS-32` — incremental streaming, durable-before-display publication, retry, cancellation, and cutoff | OpenRouter SSE parser plus existing publication coordinator and API/SSE read path | Pre-fragment and post-fragment failure; cancellation; content timeout; truncated/malformed stream; Unicode; durable fragment order; reconnect/replay; no hidden adapter retry, batching, or answer restart |
| `REQ-SESS-61`–`REQ-SESS-70`, `AC-SESS-33`–`AC-SESS-37` — provider-neutral Invocation, exactly one typed Decision or explicit execution failure, idempotency, bounds, and minimized provenance | `IModelExecutionPort`, OpenRouter control request, existing durable Invocation processor/admission/provenance ports, canonical Decision admission | Valid canonical fixtures; malformed/refusal/empty/oversized output; invocation-ID mismatch; no-action versus failure; duplicate/late/cutoff behavior; no response healing |
| `REQ-SESS-71`–`REQ-SESS-77`, `AC-SESS-38`–`AC-SESS-41` — optional next-timer recommendation remains typed but independently governed | Self-contained provider schema plus existing Decision/timer validation; no adapter scheduling authority | Canonical timer fixtures remain schema-valid; invalid timer schema fails; accepted/rejected timer effects remain independent from the message/no-action outcome |
| `REQ-SESS-78`–`REQ-SESS-85`, `AC-SESS-42`–`AC-SESS-48` — versioned output envelope, known typed-item representability, independent item validation, and reconstructable provenance | Self-contained provider schema, canonical Decision reader, existing output/action policy validation, provider-attempt provenance | Schema parity with canonical v2 fixtures; known `voice` and deferred action kinds remain schema-valid but P0-denied; unknown kinds fail schema; model/provider/attempt/usage facts; cache/mismatch rejection |
| `OSS-DEC-12`, `OSS-DEC-14`, `OSS-DEC-15`, `OSS-DEC-17` | Separate provider adapter, fixed dependency boundary, default-off synthetic profile | Architecture dependency tests; package/lock review; qualification-scope and environment gates |
| `STACK-DEC-11`, `STACK-DEC-18`, `GATE-STACK-PROVIDERS` | `FlexAgent.Sessions.OpenRouter`, Worker composition, solution/OCI graph | Project-reference and Dockerfile-copy tests; locked restore/build/publish; sanitized live evidence |
| Approved OpenRouter profile routing/privacy controls | Adapter request builder, fixed transport, discovery harness, host preflight | Exact request/header/body assertions; one provider; one router attempt; metadata required; cache denied; privacy attestation required; request/cost ceilings |
| `docs/ui-ux/text-session.md` `AC-SESS-5`–`AC-SESS-7`, `AC-SESS-31`–`AC-SESS-48` | Existing Participant Text Session only if a supported real runtime path is proven | Playwright accessibility snapshots and desktop/narrow screenshots through real interactions; otherwise record the exact integration blocker and do not claim end-to-end chat |

# Implementation decisions

- Add `openrouter` as a distinct `ModelDeploymentAdapterKinds` value with
  adapter contract `sessions.openrouter.v1`. Do not route it through
  `direct_openai`, inherit Direct OpenAI SDK behavior, or change the existing
  Direct OpenAI contract version.
- Create `src/Modules/Sessions/FlexAgent.Sessions.OpenRouter` and
  `tests/Sessions/FlexAgent.Sessions.OpenRouter.Tests`. Use `HttpClient` and
  strict `System.Text.Json` parsing so the adapter owns the exact OpenRouter
  request, SSE, header, and metadata contract without introducing another SDK.
- Keep `ApprovedHttpsOrigin` origin-only. The OpenRouter adapter constructs the
  one fixed `/api/v1/chat/completions` path internally and rejects redirects,
  alternate hosts, ports, schemes, paths, queries, fragments, and private or
  link-local destinations. Do not relax the common SSRF guard.
- Extend the common installed profile with an optional opaque
  adapter-configuration digest. When absent, its digest input must remain
  byte-for-byte compatible with existing Direct OpenAI profiles. A typed
  OpenRouter configuration registry, keyed by common profile
  ID/version/digest, owns the concrete provider slug, expected returned
  provider identity, fixed request-policy values, and its canonical digest.
  Resolution succeeds only when that digest equals the common profile's
  adapter-configuration digest; provider-specific fields do not enter Session
  records or browser contracts.
- Move adapter-neutral installed-profile and credential-file loading out of
  the OpenAI namespace into `FlexAgent.Sessions.Infrastructure`. The Worker may
  consume that shared loader; neither provider adapter may reference the other.
- Treat the provider request schema as an adapter-owned transport projection,
  not a new public Flex Agent contract. Inline the external references from the
  canonical v2 Decision schema without narrowing its known type set, and keep
  the existing canonical Decision parser/validator as final authority. Known
  `voice`, timer, and deferred-action shapes remain schema-representable so the
  existing frozen P0 policy can reject them independently; their presence in
  the schema does not enable those capabilities. Contract tests must prove the
  projection has the same valid/invalid result as every canonical v2 fixture.
- Keep discovery outside `IModelExecutionPort` and outside Worker runtime
  composition. `openrouter/free` may select sanitized candidate facts for an
  operator, but only an explicitly created profile containing one concrete
  `:free` model and one provider can execute a Session.
- Permit OpenRouter composition only in Development or Testing with exact
  qualification scope `synthetic_development`, an explicit privacy preflight,
  and the existing `Qualified` opt-in. Production and Staging remain fail
  closed even if a profile and key are present.
- Enforce existing Flex Agent request admission before every network call.
  The adapter performs no internal retry; each OpenRouter response must report
  router attempt `1`. The existing application layer owns the maximum two
  Flex Agent attempts permitted by the installed profile.
- Do not change the PostgreSQL schema unless implementation proves a durable
  contract gap. The frozen binding already stores the trusted profile digest;
  adapter configuration is revalidated through installed operator state.
- Do not add or alter Participant UI merely to produce a demo. First prove a
  supported real Session creation/message/SSE path. If none exists, stop at the
  integration gate and request a separately bounded developer-runtime surface
  rather than wiring the synthetic `/browser` path to live provider traffic.

# Plan

## Phase 0 — baseline, mapping, and safety gates

- [x] Record the starting commit, clean/dirty worktree state, migration head,
      solution membership, package locks, Worker publish graph, and relevant
      OCI COPY inputs in this task file.
- [x] Run the current focused baseline before behavior changes:
      `FlexAgent.Sessions.Tests`, `FlexAgent.Sessions.OpenAi.Tests`,
      `FlexAgent.Runtime.Tests`, and `FlexAgent.Architecture.Tests`.
- [x] Freeze a test trace matrix mapping each row above to one or more named
      tests and identify which checks require PostgreSQL, network access,
      Docker, or Playwright.
- [x] Confirm the live harness is opt-in, test-discoverable while disabled, and
      incapable of reading the key until all deterministic gates pass.
- [x] Confirm no real Participant or other sensitive data is needed; define a
      small synthetic prompt set and sanitized evidence fields before any live
      request.

Exit: baseline evidence is recorded, Direct OpenAI is green, live execution is
still off, and every governing criterion has an implementation and test owner.

## Phase 1 — frozen profile and project boundary, red then green

- [x] Red: add domain/profile tests proving an OpenRouter profile cannot be
      represented as `direct_openai`, cannot use `openrouter/free` or an alias
      for a repeatable Session, and cannot omit its adapter-policy digest.
- [x] Red: capture a known Direct OpenAI profile digest and prove the proposed
      common profile extension would change it if backward compatibility is
      implemented incorrectly.
- [x] Green: add the `openrouter` adapter kind and optional adapter-policy
      digest using an additive factory overload or equivalent version-safe
      construction; retain the old Direct OpenAI digest algorithm when the
      optional value is absent.
- [x] Green: create the OpenRouter source/test projects, add them to
      `FlexAgent.slnx`, Worker and architecture references, locked dependency
      graphs, and `deploy/docker/worker.Dockerfile` COPY inputs.
- [x] Green: introduce a typed OpenRouter installed configuration that accepts
      only the approved origin, fixed endpoint path, concrete `:free` model,
      one provider slug, one expected provider identity, 256 output tokens,
      30/60-second limits, and no more than two application attempts.
- [x] Make installed-profile, credential-catalog, and OpenRouter-configuration
      loading bounded and fail closed: reject oversized/non-array files,
      duplicate identities, unknown required-policy values, invalid digests,
      and incomplete records without partially installing a configuration.
- [x] Refactor the generic installed-profile and credential catalog loaders
      from `FlexAgent.Sessions.OpenAi` into the shared Sessions implementation
      surface, preserving Direct OpenAI parsing and behavior with regression
      tests.
- [x] Extend architecture tests so provider adapters depend only inward,
      provider-specific types do not cross domain/host/browser boundaries, and
      the official OpenAI SDK remains isolated to the Direct OpenAI assembly.
- [x] Verify the current migration head is unchanged; if a schema change proves
      necessary, stop and update the plan and governing contract before adding
      a migration.

Exit: both adapters compile in separate boundaries, a concrete OpenRouter
profile can be frozen without changing existing Direct OpenAI digests, and no
network code is enabled.

## Phase 2 — control request and strict Decision contract, red then green

- [x] Red: assert the exact POST destination and headers, including bearer
      authorization, metadata enabled, response cache disabled, JSON content,
      and the absence of optional plugins or unapproved headers.
- [x] Red: assert the nested `provider` object contains exactly one allowlisted
      provider, `allow_fallbacks:false`, `require_parameters:true`,
      `data_collection:"deny"`, and `zdr:true`.
- [x] Red: assert the request pins the concrete model and max-token bound and
      rejects discovery aliases, changed models, provider drift, unknown
      credential modes, and policy/profile digest mismatch before network I/O.
- [x] Build a reviewed, self-contained strict JSON Schema transport projection
      of `agent-decision.v2.schema.json`; inline external references while
      preserving every canonical known output/action shape and closed-object
      constraint.
- [x] Test the projection against canonical valid and invalid Decision fixtures,
      then validate the returned envelope again with the existing Flex Agent
      parser. Reject prose wrapping, repairable JSON, multiple objects,
      refusal-only, empty, malformed, unknown, and oversized responses.
- [x] Prove a canonical typed `voice` output, next-timer request, and known
      deferred action can cross the schema boundary but remain subject to the
      existing independent P0 capability/policy denial. Unknown kinds must fail
      schema admission rather than being coerced or healed.
- [x] Green: implement the smallest non-streaming control request and response
      parser with no automatic redirect, SDK retry, response healing, or raw
      body logging.
- [x] Validate required terminal response facts: exact requested/returned
      model, exactly one expected provider identity, router attempt `1`, no
      cache-hit indication, and usable usage metadata. Ignore additive unknown
      metadata only after all required known fields pass.
- [x] Keep generation lookup diagnostic-only: it may enrich a sanitized
      failure investigation but cannot replace missing metadata on the actual
      successful response.

Exit: fake HTTP proves the exact strict control contract and all routing,
privacy, identity, cache, and metadata contradictions fail closed.

## Phase 3 — streaming content and failure semantics, red then green

- [x] Red: cover valid SSE comments, multiple chunks, Unicode boundaries,
      terminal metadata, usage, and `[DONE]` ordering with non-overlapping
      fragments.
- [x] Red: cover HTTP failure, rate limit, outage, malformed event, invalid
      JSON, empty stream, truncated stream, missing terminal metadata, duplicate
      terminal metadata, cache hit, provider/model/attempt mismatch, excessive
      content, cancellation, and control/content timeout.
- [x] Red: distinguish failure before the first visible fragment from failure
      after one or more fragments. Prove the adapter never fabricates a finish,
      silently restarts an answer, or overlaps a replacement attempt.
- [x] Green: implement bounded incremental SSE parsing and content publication
      through the existing execution port; buffer only what is needed for
      protocol validation and enforce cancellation promptly.
- [x] Prove there is no hidden transport retry. Application retry remains
      subject to durable request reservation, lease/claim authority, the
      installed profile's maximum attempts, and the existing cutoff rules.
- [x] Map failures to stable non-sensitive reason categories; ensure exception,
      telemetry, and test output cannot contain the bearer token, prompt,
      response body, or raw OpenRouter error payload.

Exit: the fake streaming suite covers normal, retryable, terminal, timeout,
cancellation, and partial-publication cases without violating Session semantics.

## Phase 4 — credential hardening and egress controls, red then green

- [x] Red: extend mounted-file tests for group/other-readable or writable root
      directories and key files on Unix-like systems, plus traversal, symlink,
      reparse point, directory-as-file, non-regular file, missing, empty, and
      oversized values.
- [x] Green: add an OpenRouter-selected strict mounted-file policy or decorator
      that requires owner-only root-directory and key-file permissions on
      Unix-like systems before reading the secret. Preserve Direct OpenAI's
      existing default behavior unless a separate governing decision approves
      a cross-provider tightening. On platforms without Unix mode support,
      retain all portable checks and fail the OpenRouter live preflight until a
      reviewed platform-specific secure-source contract exists.
- [x] Add an explicit operator privacy attestation/preflight covering both
      private input/output logging and OpenRouter use of inputs/outputs. Missing
      or false attestation keeps readiness and model execution fail closed.
- [x] Prove only the exact approved HTTPS host and fixed path are reachable;
      reject redirects, alternate hosts/ports/schemes/paths, and literal
      private/link-local IP destinations without following them. Do not claim
      application-level DNS pinning unless it is explicitly implemented and
      tested.
- [x] Add log/telemetry/artifact assertions using canary secrets and canary
      prompts so leakage tests fail if authorization, raw content, or provider
      bodies appear.

Exit: deterministic tests prove secret, privacy, and egress controls fail closed
before live composition.

## Phase 5 — Worker composition and readiness, red then green

- [x] Red: cover unsupported adapter, unqualified profile, wrong environment,
      missing `synthetic_development` scope, missing privacy preflight, missing
      files, mixed adapter profiles, policy-digest mismatch, wrong provider,
      revoked credential, and insecure secret modes.
- [x] Use explicit host settings for the additional gates:
      `Sessions:ModelExecution:QualificationScope=synthetic_development`,
      `Sessions:ModelExecution:PrivacyPreflightConfirmed=true`, and an
      OpenRouter configuration-file path. Missing, misspelled, or conflicting
      values fail closed; no compiled default may opt into provider traffic.
- [x] Green: compose the OpenRouter adapter only when all exact gates pass in
      Development or Testing. Keep Direct OpenAI composition unchanged and
      prevent a single Worker configuration from mixing adapter kinds.
- [x] Extend `WorkerRuntimeCapabilities` or its equivalent so readiness reports
      adapter plus qualification scope without implying production approval.
      No secret name, key material, prompt, response, or participant fact may
      enter the capability response.
- [x] Prove Production and Staging remain fail closed even if `Qualified=true`,
      files exist, and an OpenRouter profile is otherwise valid.
- [x] Preserve deterministic fake execution for existing tests and preserve the
      idle/fail-closed behavior when Invocation processing or persistence is
      disabled.

Exit: host configuration cannot accidentally convert synthetic evidence into a
production-capable provider path.

## Phase 6 — durable integration without external network

- [x] Use a fake OpenRouter HTTP handler behind the real adapter and exercise
      the durable Invocation processor, request-admission reservation,
      provenance writer, lease/claim behavior, and publication coordinator.
- [x] Prove control and content use the same frozen concrete model, provider,
      profile digest, credential binding, and adapter contract.
- [x] Prove every network request has a prior durable `started` reservation;
      crash-after-request does not produce an unbounded duplicate; retry and
      cutoff facts are persisted correctly.
- [-] Prove generated fragments become displayable only after the governing
      durable publication step and reconnect/replay does not duplicate or
      re-order them.
      Covered by existing Sessions publication tests; not re-run as an
      OpenRouter-specific publication suite.
- [-] Run focused PostgreSQL concurrency/fault/provenance suites and verify the
      migration head remains unchanged.
      Migration files unchanged at `0029`; PostgreSQL Testcontainers suite was
      not executed in this session.

Exit: deterministic integration covers the real Flex Agent execution pipeline
through persistence and SSE publication seams, with only the external network
substituted.

## Phase 7 — known hosted Participant-path blocker and decision gate

- [x] Reconfirm the current approved UI status before integration work. As of
      this readiness review, the Participant surface uses the non-authoritative
      synthetic browser adapter and production HTTP SSE/OIDC application-session
      wiring remains a documented delivery gap.
- [x] Inventory and exercise any current Session creation, Participant message,
      Invocation scheduling, API authorization, SSE subscription, and SPA data
      path using deterministic execution; do not infer a hosted path from the
      existence of server-side persistence or Runtime SSE tests.
- [-] If the existing Participant Text Session can reach the real runtime,
      record the exact startup/configuration/seed procedure and add a repeatable
      synthetic end-to-end test before any live provider call.
- [x] If the current `/browser` surface remains synthetic or Session bootstrap
      is unavailable, record the exact missing seam and stop this phase. Do not
      claim the adapter-only harness as interactive Flex Agent chat and do not
      add a new UI or bypass authorization under this task without an approved
      scope update.

Exit: either a supported deterministic real chat path is proven, or the task
has an explicit bounded blocker and follow-up decision; ambiguity is not hidden.

## Phase 8 — bounded live discovery

- [!] Owner gate: confirm the two OpenRouter account privacy controls are
      disabled and confirm the test key's spend/expiry boundary. Do not inspect,
      print, copy, or commit the key.
- [ ] Recheck mounted secret metadata, live opt-in, concurrency `1`, maximum 12
      total inference requests, maximum two Flex Agent attempts per operation,
      256 output tokens, 30/60-second timeouts, and USD 2 stop threshold.
- [ ] Run deterministic suites immediately before enabling the live harness.
- [ ] Make one discovery-only `openrouter/free` request through the isolated
      discovery client as the approved bounded discovery/smoke run. Retain only
      sanitized candidate model/provider facts; retain no raw prompt, response,
      authorization, headers, or account data.
- [ ] Select one eligible concrete `:free` model and provider, then generate an
      operator-managed pinned profile/config outside source control. Recompute
      and verify both common and adapter-policy digests before Session use.
- [ ] If no candidate satisfies strict schema, streaming, privacy, metadata,
      cache-denial, and provider-pinning requirements, stop without fallback
      and record the bounded failure.

Exit: a single concrete model/provider is pinned for the live matrix, or live
qualification stops safely with sanitized evidence.

## Phase 9 — bounded live qualification and natural chat

- [ ] Run the approved control and streaming contract matrix sequentially using
      only synthetic prompts. Verify returned model, provider, router attempt,
      cache status, usage, timeout, and cancellation evidence on each relevant
      request.
- [ ] Use one shared qualification budget counter across discovery and pinned
      requests in Phases 8–9; process restarts or failed assertions must not
      silently reset the 12-inference-request ceiling for the run.
- [ ] Exercise both provider phases through the real Worker and PostgreSQL path;
      verify durable request admission and provider-attempt provenance without
      persisting the raw key or unapproved raw provider payloads.
- [ ] If Phase 7 proved a supported interactive path, conduct a short natural
      Participant Text Session through the real API/Worker/PostgreSQL/SSE path.
      Use Playwright accessibility snapshots and screenshots at desktop and a
      narrow viewport, with synthetic accounts/content only and artifacts only
      under `.playwright-mcp/`.
- [ ] Inspect loading, streaming, completion, timeout/error, retry/cutoff, and
      reconnect behavior that the live run actually reaches. Do not claim
      states that were not observed.
- [ ] Stop immediately on privacy uncertainty, identity/provider drift, cache
      evidence, missing metadata, fallback, spend/request ceiling, secret
      leakage, or unexpected sensitive content.
- [ ] Retain a sanitized summary labeled
      `qualified_for: synthetic_development`; passing does not enable a runtime
      by default or satisfy Direct OpenAI Phase B.

Exit: the live provider contract passes within bounds and, only if Phase 7
passed, natural chat is evidenced through the real Flex Agent path.

## Phase 10 — full verification, review, and reconciliation

- [x] Run the new adapter suite plus focused Sessions, Runtime, Architecture,
      and Direct OpenAI regressions.
      OpenRouter 17, Sessions 455, OpenAI 14, Runtime 129, Architecture 35 on
      2026-08-20.
- [-] Run focused PostgreSQL concurrency/fault/provenance suites.
      Not executed in this task; migration head remains `0029`.
- [x] Run locked restore. Worker Dockerfile COPY includes OpenRouter and is
      covered by architecture tests.
- [-] Run full solution `dotnet publish` and Worker OCI image build.
      Not executed in this task.
- [x] Review tracked and untracked changes for secrets, live payloads, browser
      state, generated logs, credentials, or account metadata before staging
      anything.
- [ ] Obtain independent backend, architecture, and security/privacy review;
      resolve findings or record accepted residual risks and owner decisions.
- [x] Recheck every governing requirement/acceptance ID and update authoritative
      status documents only where implemented evidence actually changes status.
- [x] Reconcile this file with actual changes, commands, results, deviations,
      remaining gaps, and the next safe action. Retain the completed task file
      for external review.

Exit: deterministic and permitted live evidence is complete, status claims are
traceable, and no requirement is marked complete on the strength of a demo.

## Phase 11 — review remediation (body timeout, cache, SSE bounds)

- [x] Cover the entire control and content provider operation with a linked
      timeout CTS (headers plus body/SSE), and distinguish caller cancellation
      from timeout.
- [x] Reject OpenRouter response-cache `HIT` via `X-OpenRouter-Cache-Status`;
      accept provider `prompt_tokens_details.cached_tokens` under ZDR.
- [x] Bound SSE events and visible content; require
      `content* -> exactly-one-terminal-metadata -> [DONE]`; reject duplicate
      metadata, EOF-before-DONE, and unexpected post-terminal payloads.
      Bound the control envelope before Decision admission.
- [x] Reconcile Phase 10 checklist markers with evidence actually collected.

## Phase 12 — review remediation (exact envelope bound, fixed timeouts, strict UTF-8)

- [x] Allow control envelopes of exactly `MaxControlEnvelopeUtf8Bytes` and reject
      one extra byte via an EOF overflow probe.
- [x] Keep installed OpenRouter timeouts fixed at 30/60 seconds; inject shorter
      timeouts only through a test-only adapter seam.
- [x] Decode SSE payloads with a throwing UTF-8 decoder; fail closed on invalid
      bytes before and after the first visible fragment.
- [x] Cover streaming stall-after-headers timeout and caller cancel, and keep
      the verification table aligned with those tests.

# Current state

Deterministic Phases 0–6 are implemented: distinct `openrouter` adapter
(`sessions.openrouter.v1`), backward-compatible optional adapter-configuration
digest, shared installed-profile/catalog loaders, fake-transport control and
SSE contracts, Unix owner-only secret source, Worker synthetic-development
opt-in, and durable request-reservation coverage through the existing
Invocation processor. Discovery fake-HTTP coverage records the selected
`:free` endpoint rather than `available[0]`. Direct OpenAI behavior and digest
`11fd39ad22fa975ad3db30a257405b33d8760d13d0ef7592f31e8cac6281ff2f` are preserved.

Phase 7 is blocked: Participant Text Session remains on the synthetic browser
adapter; production HTTP SSE and OIDC application-session wiring are still
documented delivery gaps. Do not claim interactive Flex Agent chat.

Phases 8–9 remain owner-gated: live OpenRouter calls are opt-in
(`FLEXAGENT_LIVE_OPENROUTER_QUALIFICATION=1` plus privacy preflight) and were
not executed. No key was read.

Migration head is unchanged (`0029`). Independent review of `964cfc5` found
missing body/stream timeouts, prompt-cache vs response-cache confusion,
unbounded SSE parsing, and overstated Phase 10 checkboxes. Phase 11 remediates
those findings with fake-transport evidence (OpenRouter 21). Phase 12 then
fixed the exact envelope bound, kept installed timeouts at 30/60 with a
test-only timeout seam, rejected invalid SSE UTF-8, and added streaming
stall/cancel coverage (OpenRouter 26). Do not mark this task complete: live
qualification and hosted Participant chat remain gated.

# Decisions

- A distinct adapter is required; do not repurpose `direct_openai` or loosen
  its endpoint/model checks.
- `openrouter/free` is discovery/smoke only. Repeatable Session testing pins
  one concrete `:free` model and provider for both provider phases.
- Privacy, identity, capability, credential, and budget controls fail closed.
- Passing evidence remains synthetic-development-only.
- Phase 7 interactive chat is out of this task until a hosted Participant
  Session/API/SSE path exists; the adapter-only harness is not Flex Agent chat.
- Installed OpenRouter control/content timeouts remain fixed at 30/60 seconds.
  Shorter timeouts exist only as an internal test seam on the adapter and are
  not reconstructed from operator configuration files.

# Findings / deviations

- `ApprovedHttpsOrigin` remains origin-only. The OpenRouter adapter constructs
  the fixed `/api/v1/chat/completions` path and rejects other destinations.
- Optional adapter-configuration digest is excluded from the legacy digest
  source when absent; known Direct OpenAI digest regression holds.
- Installed-profile and credential-catalog loaders now live in
  `FlexAgent.Sessions.Infrastructure`; Direct OpenAI no longer owns them.
- OpenRouter sends a self-contained v2 Decision schema projection; canonical
  admission remains final authority. Voice/timer/deferred shapes stay
  schema-valid and P0-denied independently.
- OpenRouter secrets use a Unix owner-only decorator; Direct OpenAI keeps the
  portable mounted-file source.
- Worker OpenRouter composition requires Development/Testing,
  `QualificationScope=synthetic_development`, privacy preflight, and does not
  mix adapter kinds. Production/Staging stay fail closed.
- Participant `/browser` remains synthetic. Hosted natural chat is blocked.
- Live qualification was not run; the opt-in sentinel remains disabled.
- Discovery records the selected endpoint, not `available[0]`, rejects a
  returned `openrouter/free` alias, and does not use the default HTTPS
  transport unless live opt-in and privacy preflight are both set.
- Control and content operations use a linked timeout CTS for headers and
  body/SSE, mapping caller cancel separately from provider timeout. Installed
  profiles stay at 30/60 seconds; tests inject shorter timeouts on the adapter.
- Response-cache denial uses `X-OpenRouter-Cache-Status: HIT`. Provider prompt
  `cached_tokens` is accepted. Streaming requires terminal metadata then
  `[DONE]`, with SSE-event and visible-content byte ceilings.
- Control envelopes of exactly 262,144 UTF-8 bytes are admitted; one extra byte
  fails. SSE payloads use a throwing UTF-8 decoder.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Plan traceability/readiness review | passed | Corrected Session requirement ranges; canonical v2 representability preserved; code/profile/Worker/OCI/UI seams rechecked on 2026-08-20 |
| Locked baseline restore | passed | `dotnet restore FlexAgent.slnx --locked-mode`; all solution projects restored with committed locks on 2026-08-20 |
| Focused pre-implementation baseline | passed | Sessions 448/448; Direct OpenAI 14/14; Runtime 126/126; Architecture 33/33 on .NET SDK 10.0.100, macOS arm64 |
| Architecture/operations approval | passed | ADR-008 `OSS-DEC-17`, ADR-010 `STACK-DEC-18`, and approved profile dated 2026-08-19 |
| Fake-transport provider contracts | passed | OpenRouter 26/26 on 2026-08-20: headers, provider object, metadata/attempt/identity/usage, response-cache HIT vs prompt cached_tokens, control and streaming stall-after-headers timeout vs caller cancel, exact envelope limit and limit+1, strict SSE UTF-8, terminal-then-DONE, discovery selected-endpoint, schema parity |
| Profile/credential/host isolation | passed | Digest regression; Infrastructure loaders; Unix owner-only secret tests; Worker Testing compose + Production fail-closed |
| Live synthetic qualification | blocked | Opt-in sentinel remains off; owner privacy/spend preflight not confirmed; no key read |
| Interactive local Text Session | blocked | Phase 7: synthetic browser adapter; production HTTP SSE/OIDC still a documented gap (`docs/ui-ux/text-session.md`) |
| Locked regression/supply chain/OCI/docs | partial | OpenRouter 26, Sessions 455, OpenAI 14, Runtime 129, Architecture 35 after Phase 12. Worker OCI COPY includes OpenRouter (architecture tests). Docker image build and `dotnet publish` not run |
| Independent review | pending | Review of `964cfc5` findings remediated in Phase 11; external backend/architecture/security review still required |

# Risks, interim defaults, and owner gates

| Topic | Risk | Interim default / gate |
| --- | --- | --- |
| Free model/provider availability | Candidate identity and capability can change without notice | Discover at qualification time, pin one concrete model/provider, fail closed on drift, and never fall back silently |
| Direct OpenAI digest compatibility | A common profile extension could invalidate installed profiles and frozen bindings | Optional adapter-policy digest is excluded from the legacy digest source when absent; preserve with a known-value regression test |
| Provider schema compatibility | Canonical schema references are not resolvable by an external provider | Generate a self-contained strict adapter projection and keep canonical validation as final authority |
| Additive OpenRouter metadata | New harmless fields could break an overly rigid parser; missing critical fields could be ignored by an overly loose parser | Require and validate all known security/identity fields, ignore unknown additive fields, reject contradictions |
| Account privacy state | Account-level toggles may not be queryable through the inference contract | Require explicit owner attestation immediately before live access; absent confirmation means no network call |
| Key spend/expiry | A general key may exceed the approved live-test risk boundary | Prefer a USD 2 / short-lived key; otherwise the harness still enforces its own 12-request/USD 2 stop and the owner explicitly accepts the broader credential scope |
| Unix permission portability | Owner-only mode checks do not map directly to every platform | Enforce on the current Unix-like target; fail live preflight elsewhere until a reviewed secure-source contract exists |
| Real browser/runtime path | Synthetic SPA behavior could be mistaken for real provider chat | Phase 7 must prove the route; if absent, record a blocker and seek a separately bounded scope update—no silent UI bypass |
| Live evidence sensitivity | Logs/screenshots could expose credentials, prompts, account data, or raw provider bodies | Synthetic prompts only, canary leakage tests first, sanitized summaries only, no raw live artifacts in Git |

# Blockers

Phases 0–6 have no blocker. Phase 7 is blocked on hosted Participant
Session/API/OIDC/SSE wiring. Phases 8–9 are blocked until the owner confirms
both OpenRouter privacy toggles and the accepted key spend/expiry boundary.
Do not enable live traffic from this session.

# First executable slice

Start with Phases 0–1 only: capture the green baseline, add failing profile and
architecture tests, then introduce the separate project boundary and
backward-compatible adapter-policy digest. Do not construct `HttpClient`, read
the mounted key, or make a live request in this slice. The first review point is
the Direct OpenAI digest regression plus the dependency/OCI graph. The readiness
baseline above is green; rerun it at implementation start if `HEAD`, locks, or
the worktree changes.

# Development handoff

## Start here

1. Change front matter to `status: in-progress`, mark the first Phase 0 item
   `[>]`, and keep this file current after each red/green boundary.
2. Capture a locked baseline before editing behavior:

   ```sh
   dotnet restore FlexAgent.slnx --locked-mode
   dotnet test --project tests/Sessions/FlexAgent.Sessions.Tests/FlexAgent.Sessions.Tests.csproj --no-restore
   dotnet test --project tests/Sessions/FlexAgent.Sessions.OpenAi.Tests/FlexAgent.Sessions.OpenAi.Tests.csproj --no-restore
   dotnet test --project tests/Runtime/FlexAgent.Runtime.Tests/FlexAgent.Runtime.Tests.csproj --no-restore
   dotnet test --project tests/Architecture/FlexAgent.Architecture.Tests/FlexAgent.Architecture.Tests.csproj --no-restore
   ```

3. Perform Phase 1 in strict TDD order. The first failing tests should cover
   the OpenRouter adapter kind, required adapter-configuration digest, rejection
   of `openrouter/free` as frozen identity, and the known Direct OpenAI digest.
4. Stop for the first review after Phase 1. Do not start provider transport or
   read the mounted key until the profile/digest, dependency, lock, and Worker
   OCI input-closure checks are green.

## Initial change map

| Concern | Primary implementation/test surfaces |
| --- | --- |
| Common frozen profile compatibility | `src/Modules/Sessions/FlexAgent.Sessions/Domain/FrozenModelDeployment.cs`; `tests/Sessions/FlexAgent.Sessions.Tests` |
| Shared installed-profile/catalog loading | move `InstalledProfileFiles.cs` behavior into `src/Modules/Sessions/FlexAgent.Sessions.Infrastructure`; add bounded parsing tests; preserve Direct OpenAI behavior |
| OpenRouter adapter/profile | new `src/Modules/Sessions/FlexAgent.Sessions.OpenRouter` and `tests/Sessions/FlexAgent.Sessions.OpenRouter.Tests` projects |
| Canonical strict output | `contracts/schemas/v2/session/agent-decision.v2.schema.json`, its primitive references and fixture catalog, plus an adapter-owned self-contained transport projection with parity tests |
| Worker opt-in/readiness | `src/Hosts/FlexAgent.Worker/WorkerDurableWorkSampling.cs`; `tests/Runtime/FlexAgent.Runtime.Tests/HostRuntimeTests.cs` |
| Architecture and packaging | `FlexAgent.slnx`, central/locked package graph, `tests/Architecture/FlexAgent.Architecture.Tests/ProviderAdapterBoundaryTests.cs`, `HostOciDockerfileTests.cs`, and `deploy/docker/worker.Dockerfile` |
| Durable integration | Sessions/PostgreSQL provider-request admission, provenance, publication, replay, and fault tests; migration head should remain `0029` |
| Live evidence | opt-in OpenRouter test/harness and, only after a real hosted path exists, Playwright evidence under `.playwright-mcp/`; sanitized qualification summary under the approved operations directory |

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [ ] Task state is safe and complete for external review
