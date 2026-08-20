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
  explicit synthetic-development data-collection allowance, no request-level
  ZDR requirement, concrete-model, one-provider allowlist, router metadata,
  and response-cache-denial controls.
- Record and validate returned model, selected provider, and attempt count;
  reject missing metadata, cache hits, mismatch, fallback, or drift for a
  pinned development Session.
- Prove `openrouter/free` discovery cannot become frozen Session identity or
  mix models across control and visible-content phases.
- Reuse mounted-file secrets with OpenRouter-specific binding and no raw-secret
  persistence, telemetry, error, or artifact exposure; reject symlinks,
  non-regular files, and group/other directory or file permissions on Unix-like
  systems.
- Require explicit owner acceptance that every disclosed value is synthetic
  and may be retained or used for training before opt-in live execution.
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
  multiple models in one Invocation, or disclosure of any real/private data.
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
| Approved OpenRouter profile routing/data-policy controls | Adapter request builder, fixed transport, discovery harness, host acceptance gate | Exact request/header/body assertions; one provider; one router attempt; metadata required; cache denied; explicit synthetic-data-policy acceptance required; request/cost ceilings |
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
  qualification scope `synthetic_development`, explicit acceptance of the
  synthetic-only retention/training policy, and the existing `Qualified`
  opt-in. Production and Staging remain fail closed even if a profile and key
  are present. The retired privacy-preflight setting is not sufficient.
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
      `data_collection:"allow"`, and `zdr:false` under the synthetic-only
      development decision.
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
      Superseded on 2026-08-20 by Phase 14's explicit synthetic-data-policy
      acceptance gate; the old setting no longer enables execution.
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
      missing `synthetic_development` scope, missing data-policy acceptance, missing
      files, mixed adapter profiles, policy-digest mismatch, wrong provider,
      revoked credential, and insecure secret modes.
- [x] Use explicit host settings for the additional gates:
      `Sessions:ModelExecution:QualificationScope=synthetic_development`,
      `Sessions:ModelExecution:SyntheticDataPolicyAccepted=true`, and an
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

## Phase 8 — historical strict-policy bounded live discovery (superseded)

- [x] Owner gate: confirm the two OpenRouter account privacy controls are
      disabled and confirm the test key's spend/expiry boundary. Do not inspect,
      print, copy, or commit the key. Confirmed by the owner on 2026-08-20 for
      the original strict-policy run; Phase 14 governs the amended profile.
- [x] Recheck mounted secret metadata, live opt-in, concurrency `1`, maximum 12
      total inference requests, maximum two Flex Agent attempts per operation,
      256 output tokens, 30/60-second timeouts, and USD 2 stop threshold.
- [x] Run deterministic suites immediately before enabling the live harness.
      OpenRouter deterministic suite: 31 passed, 1 explicit live test excluded
      after persistent-budget and sanitized-classification coverage.
- [!] Make one discovery-only `openrouter/free` request through the isolated
      discovery client as the approved bounded discovery/smoke run. Retain only
      sanitized candidate model/provider facts; retain no raw prompt, response,
      authorization, headers, or account data. Slots 1–4/12 on 2026-08-20
      produced no candidate (`rate_limited` / 429 and `request_rejected` / 404).
      A later eligibility recheck plus deterministic gate (31/31, live excluded)
      authorized one new reserved request; slot 5/12 returned sanitized
      `request_rejected` / HTTP 404. No further retry followed.
- [!] Select one eligible concrete `:free` model and provider, then generate an
      operator-managed pinned profile/config outside source control. Recompute
      and verify both common and adapter-policy digests before Session use.
      Blocked because discovery produced no admissible candidate.
- [x] If no candidate satisfies strict schema, streaming, privacy, metadata,
      cache-denial, and provider-pinning requirements, stop without fallback
      and record the bounded failure. Stopped after 5/12 reserved requests;
      Phase 9 did not start.

Exit: a single concrete model/provider is pinned for the live matrix, or live
qualification stops safely with sanitized evidence.

## Phase 9 — bounded live qualification and natural chat

- [x] Run the approved control and streaming contract matrix sequentially using
      only synthetic prompts. Verify returned model, provider, router attempt,
      cache status, usage, timeout, and cancellation evidence on each relevant
      request.
      Control: slot 7 unclassified `provider_unavailable`; slot 8 HTTP 404 /
      `request_rejected`, cache absent. Content: slot 9 HTTP 200, cache absent,
      identity and usage accepted, 1 fragment, completed. Timeout, cancellation,
      and retry were not reached.
- [x] Use one shared qualification budget counter across discovery and pinned
      requests in Phases 8–9; process restarts or failed assertions must not
      silently reset the 12-inference-request ceiling for the run.
      Retention-accepted counter is 9/12 after slots 7–9. `TryRead` peeks without
      incrementing. Historical counter remains 5/12.
- [-] Exercise both provider phases through the real Worker and PostgreSQL path;
      verify durable request admission and provider-attempt provenance without
      persisting the raw key or unapproved raw provider payloads.
      Docker was unavailable, so the live Worker/PostgreSQL path was not run.
      Deterministic reservation coverage remains Phase 6.
- [-] If Phase 7 proved a supported interactive path, conduct a short natural
      Participant Text Session through the real API/Worker/PostgreSQL/SSE path.
      Use Playwright accessibility snapshots and screenshots at desktop and a
      narrow viewport, with synthetic accounts/content only and artifacts only
      under `.playwright-mcp/`.
      Phase 7 remains blocked; no hosted chat in this phase.
- [x] Inspect loading, streaming, completion, timeout/error, retry/cutoff, and
      reconnect behavior that the live run actually reaches. Do not claim
      states that were not observed.
      Observed: operator-file load, control 404, streaming, completion, usage.
      Not observed: timeout, cancel, retry, cutoff, reconnect, Worker/Postgres.
- [x] Stop immediately on synthetic-data-policy uncertainty, any real/private
      content, identity/provider drift, cache evidence, missing metadata,
      fallback, spend/request ceiling, or secret leakage.
      Slot 7 stopped before content because status was unclassified. Slot 8 404
      was treated as control-route rejection, not a hard identity/cache stop, so
      content still ran. No fallback or paid-model substitution.
- [x] Retain a sanitized summary; apply `qualified_for: synthetic_development`
      only if the live contract actually passed. Passing does not enable a
      runtime by default or satisfy Direct OpenAI Phase B.
      Summary retained at
      `docs/operations/provider-profiles/qualified/openrouter/synthetic-development-phase9-2026-08-20.md`.
      The acceptance label was **not** applied because structured control failed.

Exit: not met. The pinned live contract is incomplete (control 404, content
passed). Phase 7 chat remains blocked. Do not treat the Phase 9 checkboxes as
full qualification.

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
- [x] Obtain independent backend, architecture, and security/privacy review of
      the synthetic OpenRouter adapter remediation series through `7e2e438`.
      Reviewer found no remaining substantive correctness or architecture
      issues; nine prior findings are closed. GitHub currently reports no
      commit statuses for that SHA, so local OpenRouter 27/27 is recorded but
      not independently confirmed from CI metadata. Live qualification and
      hosted Participant chat remain gated and were not in this review scope.
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

## Phase 13 — escaped invalid surrogate fail-closed

- [x] Reject JSON strings that `GetString()` cannot materialize (lone
      `\uD800` escapes) as typed adapter failures, not unhandled exceptions.
      Cover control content, stream deltas, and model/provider identity.

## Phase 14 — synthetic retention/training development amendment

- [x] Record the Product Lead's 2026-08-20 decision allowing OpenRouter and
      selected providers to retain or train on intentional synthetic local
      development content only.
- [x] Preserve the prohibition on Production, Staging, and all real
      Participant/customer, Submission, transcript, Evidence, Evaluation,
      Result, Release, memory, credential, and private-source data.
- [x] Red: require `data_collection:"allow"`, `zdr:false`, a new explicit
      synthetic-data-policy acceptance name, and proof that the retired
      privacy-preflight setting alone remains fail closed.
- [x] Green: update request construction, immutable adapter digest, live
      discovery gate, and Worker composition without weakening routing,
      fallback, cache, credential, environment, or budget controls.
- [x] Regenerate example profile digests and run focused, architecture, and
      documentation verification.
- [x] Start a distinct persistent live qualification budget for the amended
      profile; preserve the prior strict-policy budget and its five consumed
      requests as historical evidence.

Exit: the amended synthetic profile is explicit and auditable, deterministic
tests pass, the old gate cannot authorize traffic, and any live run uses a new
persistent budget without erasing the prior history.

## Phase 15 — final consistency, security, and delivery review

- [x] Re-review backend correctness, external-provider trust boundaries,
      security/privacy, documentation authority, tests, and delivery state.
      Frontend review is not applicable because the diff contains no UI code or
      user-observable browser state.
- [x] Red/green: reject provider-controlled discovery model/provider identity
      values that could inject control characters into sanitized evidence;
      bound both identities before profile pinning.
- [x] Add Unix regression coverage proving dangling budget-state links and
      symlinked budget directories fail closed without creating a target.
- [x] Reconcile the stale Phase 1 handoff with the actual next live-discovery
      and pinning step.
- [x] Run the full solution. All 832 non-PostgreSQL tests passed; the 248
      PostgreSQL Testcontainers cases could not start because Docker is not
      running. No persistence or migration surface changed in this amendment.

Exit: no actionable backend, architecture, security/privacy, documentation, or
test defect remains in the changed surface; the Docker-dependent integration
gap and live-provider next step are explicitly recorded.

## Phase 16 — retention-accepted discovery and pin

- [x] Rerun the deterministic OpenRouter gate immediately before another live
      request. 34/34 on 2026-08-20 with the explicit live test excluded.
- [x] Make one reserved discovery against the distinct retention-accepted
      counter and capture sanitized identity through a temporary xUnit XML
      report plus live output. Slot 6/12 returned
      `nvidia/nemotron-3.5-lightning:free` / `Nvidia`. Historical 5/12 counter
      was not reused.
- [x] Pin that pair in operator-managed files outside source control using
      provider slug `nvidia` and expected returned identity `Nvidia`. Recompute
      and load-verify both digests through the real profile/configuration
      loaders. Files are owner-only under the local OpenRouter operator
      directory and are not in Git.
- [x] Begin the Phase 9 control/streaming matrix against the pinned pair using
      the same retention-accepted budget. Do not start hosted Participant chat.
      Matrix ran; control 404, content passed; label not applied.

Exit: one concrete `:free` model and provider are pinned with verified digests,
or the run stops with sanitized failure evidence.

## Phase 17 — live-harness review remediation

- [x] Fail closed before any reservation unless the configured live phase and
      expected consumed count match, discovery is retired after pin (consumed
      >= 6), and the pinned Nvidia matrix refuses at the recorded 9/12 state.
      Put both explicit live tests in one non-parallel collection.
- [x] Reset sanitized HTTP observations before each request so a thrown
      follow-up cannot inherit the previous status or cache class.
- [x] Require visible content (at least one delta and UTF-8 bytes), no content
      failure, and non-truncated output tokens before emitting
      `qualified_for=synthetic_development`.
      The recorded slot-9 content used 256/256 tokens, so that stream would not
      qualify even if control later succeeded.

## Phase 18 — atomic expected reservation

- [x] Add `TryReserveExpected` so the expected-count comparison and increment
      happen under the same exclusive budget lock. Wire live discovery and
      pinned control/content reservations to it, and cover two concurrent
      instances that both expect `6`.
- [x] Record independent review approval of `08c9304` on 2026-08-20: no
      remaining actionable correctness issue in the changed live-harness
      surface. Local OpenRouter 55/55 is documented evidence only; GitHub
      still reports no commit statuses for that SHA. Live qualification
      remains partial at 9/12 and is not labeled
      `qualified_for: synthetic_development`.

## Phase 19 — public catalog eligibility recheck (no live inference)

- [x] Recheck the public OpenRouter models and endpoints catalogs without
      using the mounted key or consuming a budget slot.
- [x] Compare advertised `structured_outputs` / `response_format` support
      against the pinned `nvidia/nemotron-3.5-lightning:free` / `Nvidia` pair
      and against current `:free` endpoints.
- [x] Confirm discovery still omits `response_format`, so `openrouter/free`
      plus `require_parameters:true` can still select a streaming-only free
      route that later 404s on strict control.

Exit: catalog evidence only. Do not treat advertised parameters as live
qualification. Do not spend remaining slots repeating the recorded lightning
control 404.

## Phase 19 catalog ranking and approved route decision

- [x] Rank advertised `:free` structured-output endpoints for the 256-token
      control bound, text-only request surface, no-fallback pin, and
      non-mandatory reasoning. Do not pin or spend a live slot in this step.

Product Lead decision approved on 2026-08-20: use
`google/gemma-4-26b-a4b-it:free` with provider slug `darkbloom` and expected
returned identity `Darkbloom`. The sibling `google-ai-studio` endpoint lacks
`structured_outputs`; do not pin it. Fallback candidate if that route 404s:
`nvidia/nemotron-nano-9b-v2:free` / `nvidia` / `Nvidia`. Do not prefer
`gpt-oss-20b`, `lfm-2.5`, `glm-5.2`, or `nemotron-3-super` first because
mandatory or default-on reasoning competes with the 256-token Decision
envelope.

## Phase 20 — approved Gemma/Darkbloom pin and gated live matrix

- [x] Record Product Lead approval of the Gemma/Darkbloom route selection in
      the authoritative synthetic-development operations profile. Approval is
      candidate selection only; it is not qualification or enablement.
- [x] Recheck repository, operator-state, credential-metadata, budget, and exact
      public endpoint readiness without reading the key or spending a slot.
      Primary remains catalog-eligible. Existing operator files still pin
      Lightning with owner-only modes; the retention-accepted counter is `9/12`.
      The backup catalog reports expiration on 2026-08-24.
- [x] Red: add deterministic tests for a distinct Gemma/Darkbloom phase that
      starts only at expected consumed count `9`, refuses stale/wrong phase and
      route identity, preserves the retired Lightning phase, and cannot reserve
      content after any result other than an admitted structured Agent Decision.
- [x] Green: implement the smallest route-specific Phase 20 gate and explicit
      live runner. Keep the historical Lightning runner and evidence unchanged;
      bind exact profile ID, model, provider slug/identity, digests, and a short
      synthetic content prompt. Do not make route identity environment-driven.
- [x] Add the conditional backup phase gate at exact consumed count `10`, with
      exact Nemotron/Nvidia identity and the same control-before-content rule.
      The backup remains unusable unless primary failure is sanitized and the
      exact endpoint is still free, present, unexpired, and parameter-compatible.
- [x] Run the focused OpenRouter suite and route/gate negative tests with both
      explicit live cases excluded. Recheck docs and diff hygiene. Do not enable
      live access while deterministic verification is incomplete.
- [x] Preserve the historical Lightning operator files. Create separately named
      owner-only Gemma/Darkbloom profile and configuration files outside Git,
      recompute both digests, verify them through the real loaders, and pin the
      exact digest constants in the explicit runner.
- [x] Immediately before live access, recheck exact endpoint capability and
      zero pricing, budget `9/12`, owner-only modes, key spend/expiry boundary,
      and explicit synthetic-data-policy acceptance. Any drift stops before
      reservation.
- [x] Run the explicit primary Phase 20 matrix with expected consumed `9`.
      Reserve slot 10 for control; reserve slot 11 for visible streaming only
      after admitted structured control. Require validated identity, metadata,
      cache denial, usage, visible content, and output below 256 tokens.
      Slot 10: HTTP 200, cache absent, identity accepted, usage 2322/256,
      `malformed_control`. Content was not reserved.
- [x] If primary control fails, record the sanitized failure before creating and
      load-verifying a separate backup operator pin. Run backup only at expected
      consumed `10`; slots 11–12 leave no contingency. An expired or changed
      backup returns to owner selection without spending another slot.
      Slot 11: HTTP 200, cache absent, identity accepted, usage none,
      `malformed_control`. Content was not reserved. Slot 12 unused.
- [x] Reconcile sanitized evidence and apply
      `qualified_for: synthetic_development` only if every existing gate passes.
      Label not applied. Evidence:
      `docs/operations/provider-profiles/qualified/openrouter/synthetic-development-phase20-2026-08-20.md`.

Exit: the same frozen model/provider pair passes strict control and visible
streaming with complete evidence, or the bounded run stops without fallback,
qualification, or enablement.

## Phase 21 — approved GPT-OSS/Darkbloom candidate; live fail-closed

- [x] Record the Product Lead candidate decision and bounded Phase 21 plan in
      the authoritative synthetic-development profile and provider index.
      Candidate: `openai/gpt-oss-20b:free` / `darkbloom` / `Darkbloom`.
      Approval is planning only and does not inspect a credential, write an
      operator pin, make a network request, qualify the route, or enable it.
- [x] Recheck the exact public model and endpoint catalogs immediately before
      implementation and again before live access. Require zero price, exact
      provider identity, strict structured-output parameters, and support for
      every sent reasoning and routing parameter. Catalog advertisement remains
      eligibility evidence only.
      Pre-implementation and immediate pre-live rechecks on 2026-08-20: one
      free Darkbloom endpoint, prompt/completion `0`, `structured_outputs`,
      `response_format`, `reasoning`, `reasoning_effort`, and `max_tokens`.
- [x] Red — add deterministic tests for a distinct
      `gpt-oss-darkbloom-matrix` phase, a new 0/4 Phase 21 budget ledger, exact
      profile/configuration identity, immutable historical 5/12 and 11/12
      ledgers, `max_tokens:1024`,
      `reasoning:{effort:"low",exclude:true}`, strict schema, no reasoning
      persistence/exposure, and refusal before reservation for stale phase,
      wrong count, unsupported parameters, fallback, or identity drift.
- [x] Green/refactor — implement the minimum candidate-specific request policy,
      installed configuration, budget gate, and explicit runner. Preserve the
      default and historical 256-token contract; Phase 21 alone may use 1,024
      total output tokens including reasoning. Keep visible-content acceptance
      below 256 output tokens and canonical Decision admission as final
      authority.
- [x] Run the focused deterministic OpenRouter suite, request-shape/schema
      parity checks, budget/concurrency/identity negatives, documentation
      validation, and diff hygiene with every live test excluded. Resolve
      failures before creating operator state or enabling network access.
- [x] Create a separately named owner-only GPT-OSS/Darkbloom profile and
      configuration outside Git; recompute and load-verify both digests. Do not
      modify or delete Lightning, Gemma, or Nano operator files or evidence.
      Files written `0600` as
      `synthetic-development-gpt-oss-darkbloom.profile.json` and
      `.configuration.json`. Historical Lightning, Gemma, Nano, and both 12-slot
      ledgers were checksum-unchanged.
- [x] Obtain immediate owner confirmation before live verification: synthetic
      content only, retention/training acceptance, exact candidate and digests,
      owner-only key metadata, spend/expiry boundary, 0/4 budget, concurrency
      1, USD 2 stop, 30/60-second timeouts, low/excluded reasoning, and the
      1,024-token candidate ceiling. Authorized by the owner on 2026-08-20 to
      perform the live Phase 21 verification.
- [x] Run one strict structured-control request at expected Phase 21 consumed
      count 0. Reserve participant-visible content only after an admitted
      Decision from the same frozen pair. Schema, capability, identity, cache,
      policy, or malformed-output failure is terminal for this candidate. At
      most one retry per operation is allowed only for an already classified
      transient failure under the existing two-attempt rule.
      Slot 1/4: HTTP 200, cache absent, model accepted, usage none,
      `malformed_control`. Content was not reserved. Not retried.
- [x] Reconcile sanitized Phase 21 evidence, focused and applicable integration
      checks, and independent review. Apply
      `qualified_for: synthetic_development` only if strict control, visible
      non-truncated content below 256 output tokens, identity, metadata, cache,
      usage, policy, credential, budget, cancellation/timeout/retry, and all
      existing acceptance gates pass.
      Label not applied. Evidence:
      `docs/operations/provider-profiles/qualified/openrouter/synthetic-development-phase21-2026-08-20.md`.

Exit: GPT-OSS/Darkbloom either passes the complete synthetic-development
contract with a separately reviewable evidence record, or Phase 21 stops
fail-closed without consuming unrelated budget, trying another route, applying
a qualification label, or enabling runtime traffic.

# Current state

On 2026-08-20 the Product Lead explicitly accepted OpenRouter/provider
retention and training risk for intentional synthetic solo-development content
so development can proceed before production hardening. This does not widen the
boundary to Production, Staging, or any real/private data. The amended profile
uses `data_collection:"allow"` and `zdr:false` behind a newly named explicit
acceptance gate; the retired privacy-preflight flag is intentionally
insufficient. The prior strict-policy qualification run remains historical at
5/12 consumed requests and will not be reset or reused for the amended profile.

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

Phase 9 ran against the pinned pair. Retention-accepted budget is 9/12.
Structured control is unavailable on this free route (HTTP 404 /
`request_rejected`). Streaming completed with adapter-validated identity, cache
absent, router metadata, and usage 188/256. The acceptance label was not
applied. Live Worker/PostgreSQL was skipped because Docker was unavailable.
Phase 17 closed the live-harness review: explicit runners now require a matching
phase and expected consumed count, refuse this 9/12 Nvidia matrix before
reserve, isolate per-request HTTP observations, and require visible
non-truncated content before printing `qualified_for`. Phase 18 makes the
expected count compare-and-increment atomic under the exclusive budget lock so
two processes that both observed `6` cannot both reserve. Independent review
of `08c9304` (2026-08-20) approved that harness surface.

Phase 19 public-catalog recheck on 2026-08-20 (no key, no budget slot):
OpenRouter now advertises several `:free` endpoints with both
`structured_outputs` and `response_format`. The previously pinned
`nvidia/nemotron-3.5-lightning:free` / `Nvidia` endpoint still advertises
neither, so repeating its control request cannot clear the blocker. Catalog
advertisement is not live qualification under `require_parameters`, no
fallback, metadata, cache denial, and the Flex Agent Decision schema. The
qualification task stays open for a new live pin of a catalog-eligible free
route and the hosted Participant path.

Phase 20 completed on 2026-08-20. Distinct `gemma-darkbloom-matrix` and
`nemotron-nano-backup-matrix` gates and runners are implemented. Historical
Lightning operator files remain. Separate owner-only Gemma and Nano pins were
load-verified. Retention-accepted budget is now 11/12. Slot 10 Gemma control
and slot 11 Nano control both returned HTTP 200, cache absent, validated
model identity, and `malformed_control`. Content was not reserved. The
acceptance label was not applied. Slot 12 is unused. Do not repeat either
control request.

Phase 21 completed its live control probe on 2026-08-20 for
`openai/gpt-oss-20b:free` / `darkbloom` / `Darkbloom`. The distinct 0/4 ledger
is now 1/4. Slot 1 returned HTTP 200, cache absent, validated model identity,
and `malformed_control` with no recorded usage. Content was not reserved.
Remaining Phase 21 slots must not repeat that control request. Historical 5/12
and 11/12 ledgers are unchanged. The acceptance label was not applied.
Evidence:
`docs/operations/provider-profiles/qualified/openrouter/synthetic-development-phase21-2026-08-20.md`.

## Historical strict-policy qualification run

The Phase 8–9 owner gate cleared on 2026-08-20. Live calls remain opt-in through
`FLEXAGENT_LIVE_OPENROUTER_QUALIFICATION=1` plus privacy preflight; key contents
were never printed or retained.

The owner privacy and short-lived-key gate cleared on 2026-08-20. Mounted-file
metadata passes the approved local convention (`0700` regular directory,
`0600` regular file); key contents were not printed. Pre-live inspection found
that the budget-path constant and 12-request bound have no executable runner or
persistent counter yet. Interim default: add and test that fail-closed runner
before the first network request rather than make an ad-hoc call that could
silently reset the shared Phase 8–9 budget.

The persistent counter and explicit live discovery runner were then added and
the deterministic suite passed (31 passed; live case explicitly excluded).
The first live discovery request consumed slot 1/12 and failed closed because
the response did not produce an admissible concrete `:free` model/provider
candidate. No raw provider body, header, generation ID, prompt, or key was
retained. Sanitized failure classification was added before the one permitted
diagnostic retry.

Sanitized classification tests passed, and the second request reported HTTP
429 / `rate_limited`. The run stopped with 2/12 slots consumed and ten slots
untouched. Phase 8 has no pinnable candidate; Phase 9 did not start. Resume only
after the OpenRouter free-model rate-limit window resets or the owner supplies
an independently approved eligible key/account boundary. Do not bypass the
profile with a paid model, fallback, or relaxed routing/privacy controls.

After an explicit owner request to try again, the deterministic gate passed
31/31 and one new bounded request reserved slot 3/12. OpenRouter returned
sanitized HTTP 404 / `request_rejected`; no raw error body was retained. Current
OpenRouter documentation permits 404 when no allowed provider is available for
the selected model, so this is consistent with (but does not prove) the strict
privacy/routing filters leaving no eligible free route. Discovery stopped with
nine slots untouched and Phase 9 remains blocked.

One final owner-authorized attempt reran the deterministic gate (31/31) and
reserved slot 4/12. It returned sanitized HTTP 429 / `rate_limited`. The run
stopped with eight slots untouched; immediate retries are exhausted and Phase 9
remains blocked.

The next owner-requested verification rechecked public free-route eligibility
without using the key: the catalog listed 17 `:free` models and at least one
zero-price ZDR text endpoint (`z-ai/glm-5.2:free` / Decart). That does not
prove a route under the combined discovery filters (`zdr`, `data_collection`
deny, `require_parameters`, and no fallbacks). Mounted-secret metadata still
matched `0700`/`0600`. Persistent budget was 4/12. Deterministic OpenRouter
tests passed 31/31 with the live case excluded. One reserved discovery request
then consumed slot 5/12 and returned sanitized HTTP 404 / `request_rejected`.
No raw body, header, prompt, or key was retained. Discovery stopped with seven
slots untouched; Phase 9 remains blocked.

Migration head is unchanged (`0029`). Independent review of `7e2e438`
(2026-08-20) approved the synthetic OpenRouter adapter remediation series and
found no remaining adapter-level defects. Nine prior findings (timeouts, cache
semantics, SSE bounds, checklist honesty, exact envelope limit, fixed 30/60
timeouts, strict UTF-8, streaming timeout evidence, escaped surrogates) are
closed. Do not mark this qualification task complete: live OpenRouter
qualification has no admissible discovery candidate and hosted Participant chat
remains gated. Next safe action is to treat combined privacy/routing filters as
the likely 404 cause, not a transient 429, and either wait for an independently
approved eligible free route or obtain a separately approved key/account
boundary; use a separately scoped hosted Participant-path task if natural chat
is required. Do not relax ZDR, data-collection denial, fallbacks, or switch to
a paid model.

## Retention-accepted qualification run

Phase 14 supersedes the strict-policy routing fields without erasing its
history. After an owner `continue`, the deterministic suite passed 34/34 and
one reserved discovery consumed slot 6/12 of the distinct retention-accepted
counter. Sanitized identity is
`nvidia/nemotron-3.5-lightning:free` / `Nvidia`. That pair is pinned outside
source control as `openrouter.synthetic.local.nemotron-3.5-lightning` with
provider slug `nvidia`, expected identity `Nvidia`, adapter digest
`77754995939f05366000e0f90022e998cdc85d18b3f675b8d64307595b0361ac`, and profile
digest `52b47fe8a81ec93aad637d3d81fee665ee9a8230762ecad3204ad6963ca038ac`. Real
loaders accepted both files. Historical budget remains 5/12. Phase 9 then
consumed slots 7–9 of the retention-accepted counter. Slot 8 proved control
HTTP 404 / `request_rejected`. Slot 9 proved streaming completion with usage
188/256, cache absent, and adapter-validated identity. Three retention-accepted
slots remain. The run is not labeled `qualified_for: synthetic_development`.

# Decisions

- A distinct adapter is required; do not repurpose `direct_openai` or loosen
  its endpoint/model checks.
- `openrouter/free` is discovery/smoke only. Repeatable Session testing pins
  one concrete `:free` model and provider for both provider phases.
- Privacy, identity, capability, credential, and budget controls fail closed.
- Passing evidence remains synthetic-development-only.
- For the amended synthetic-development profile only, OpenRouter/provider
  retention and training are accepted through an explicit gate. Production,
  Staging, and real/private data remain prohibited and require later hardening
  and a separate approval.
- Phase 7 interactive chat is out of this task until a hosted Participant
  Session/API/SSE path exists; the adapter-only harness is not Flex Agent chat.
- Installed OpenRouter control/content timeouts remain fixed at 30/60 seconds.
  Shorter timeouts exist only as an internal test seam on the adapter and are
  not reconstructed from operator configuration files.

# Findings / deviations

- Phase 20 implementation: distinct phases `gemma-darkbloom-matrix` (consumed
  9 only) and `nemotron-nano-backup-matrix` (consumed 10 only). Historical
  Lightning runner unchanged. Content reservation requires
  `ModelExecutionStructuredControl`.
- Phase 20 self-review: live Gemma and Nano control both failed closed before
  content. Gemma usage 2322/256 indicates the Decision JSON was truncated at
  the installed output bound. Nano recorded no usage tokens on the failed
  control. Catalog `structured_outputs` did not produce an admissible Decision.
- Phase 20 self-review: Nano adapter digest equals the Lightning adapter digest
  because both use `nvidia` / `Nvidia` and the same immutable request-policy
  source; profile digests differ. This is expected, not a pin mix-up.
- Exact public catalogs rechecked on 2026-08-20: Gemma free still exposes
  `darkbloom` / `Darkbloom` with `response_format` and `structured_outputs`;
  Google AI Studio still lacks `structured_outputs`. The Nemotron Nano backup
  is currently present but reports expiration on 2026-08-24.
- Phase 21 implementation: `OpenRouterRequestPolicy.Phase21GptOss` is the only
  path that may send 1,024 tokens and
  `reasoning:{effort:"low",exclude:true}`. It is bound to the GPT-OSS/Darkbloom
  identity. Default Create remains 256 tokens with no reasoning and keeps the
  Gemma adapter digest. Hidden `reasoning` / `reasoning_details` /
  `reasoning_content` fields fail closed as `provider_unavailable` and are not
  published. The Phase 21 budget format
  `openrouter_qualification_budget.phase21.v1` cannot read or write historical
  `v1` 12-slot files.
- Phase 21 self-review: a separately named owner-only GPT-OSS pin was written
  outside Git. The key was not printed. The explicit live runner uses
  `FLEXAGENT_OPENROUTER_PHASE21_QUALIFICATION_BUDGET_PATH` so it cannot increment
  the 11/12 ledger by accident. Visible-content qualification still uses the
  256-token acceptance bound, not the 1,024 request ceiling.
- Phase 21 live self-review: slot 1/4 failed closed before content
  (`malformed_control`, HTTP 200, cache absent, usage none). Historical 5/12
  and 11/12 checksums were unchanged after the pin and the live request. Do
  not retry this control. Catalog `structured_outputs` again did not produce
  an admissible Decision.

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
  `QualificationScope=synthetic_development`, explicit synthetic-data-policy
  acceptance, and does not mix adapter kinds. The retired privacy flag alone
  fails closed; Production/Staging stay fail closed.
- Participant `/browser` remains synthetic. Hosted natural chat is blocked.
- The historical strict-policy discovery stopped after 5/12 reserved requests.
  The distinct retention-accepted run is 11/12. Slot 6 captured sanitized
  `nvidia/nemotron-3.5-lightning:free` / `Nvidia` and that pair is now pinned
  outside Git. Slots 1–3 passed without recorded identity; slot 4 was
  `rate_limited`; slot 5 was `timeout`. Slot 7 was an unclassified control
  `provider_unavailable`. Slot 8 was control HTTP 404 / `request_rejected`.
  Slot 9 was content HTTP 200 with cache absent and usage 188/256.
- Discovery records the selected endpoint, not `available[0]`, rejects a
  returned `openrouter/free` alias, and does not use the default HTTPS
  transport unless live opt-in and synthetic-data-policy acceptance are both set.
- Fresh cross-cutting review rejected provider-controlled discovery model or
  provider identities containing log-breaking control characters and bounded
  both identity fields before sanitized evidence or profile pinning. Budget
  regressions also prove dangling state links and symlinked directories fail
  closed on the current supported Unix target.
- Control and content operations use a linked timeout CTS for headers and
  body/SSE, mapping caller cancel separately from provider timeout. Installed
  profiles stay at 30/60 seconds; tests inject shorter timeouts on the adapter.
- Response-cache denial uses `X-OpenRouter-Cache-Status: HIT`. Provider prompt
  `cached_tokens` is accepted. Streaming requires terminal metadata then
  `[DONE]`, with SSE-event and visible-content byte ceilings.
- Control envelopes of exactly 262,144 UTF-8 bytes are admitted; one extra byte
  fails. SSE payloads use a throwing UTF-8 decoder. Escaped invalid surrogates
  in provider JSON strings fail closed in the parser instead of escaping the
  adapter.
- Independent review of `7e2e438` (2026-08-20) approved the adapter
  remediation series. No further adapter iteration before the gated live
  qualification step. Local OpenRouter 27/27 was not independently confirmed
  from GitHub CI metadata.
- Phase 9 live matrix against the pinned Nvidia free route: structured control
  is HTTP 404 / `request_rejected`; streaming completed with identity, cache
  absent, and usage 188/256. The first reserved control (slot 7) lacked HTTP
  classification. Remaining slots must not repeat the same control request.
  Acceptance label not applied.
- Phase 17 live-harness review: reservation now requires an exact live phase
  and expected consumed count; discovery is retired after pin; the recorded
  9/12 pinned matrix refuses before reserve; HTTP observations reset per
  request; qualification requires visible non-truncated content.
- Phase 18: `TryReserveExpected` compare-and-increments under the exclusive
  budget lock. Live control uses `alreadyConsumed -> n+1` and content uses
  `n+1 -> n+2`. Two concurrent instances expecting `6` cannot both succeed.
- Independent review of `08c9304` (2026-08-20) approved the live-harness
  remediation series. No remaining actionable correctness issue. Local
  OpenRouter 55/55 was not independently confirmed from GitHub CI metadata.
- Phase 19 public catalog/endpoints recheck (2026-08-20, no inference): the
  pinned lightning route still lacks `structured_outputs` and
  `response_format`. Other `:free` endpoints now advertise both, including
  `openai/gpt-oss-20b:free` / Darkbloom, `z-ai/glm-5.2:free` / Decart,
  `google/gemma-4-26b-a4b-it:free` / Darkbloom, `liquid/lfm-2.5-2.6b:free` /
  Liquid, `nvidia/nemotron-nano-9b-v2:free` / Nvidia,
  `nvidia/nemotron-3-super-120b-a12b:free` / Nvidia, and
  `dots-studio/dots-3-note-preview:free` / AtlasCloud. `openrouter/free` lists
  those parameters at model level but has no concrete endpoints document.
  Discovery still does not send `response_format`.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Plan traceability/readiness review | passed | Corrected Session requirement ranges; canonical v2 representability preserved; code/profile/Worker/OCI/UI seams rechecked on 2026-08-20 |
| Locked baseline restore | passed | `dotnet restore FlexAgent.slnx --locked-mode`; all solution projects restored with committed locks on 2026-08-20 |
| Focused pre-implementation baseline | passed | Sessions 448/448; Direct OpenAI 14/14; Runtime 126/126; Architecture 33/33 on .NET SDK 10.0.100, macOS arm64 |
| Architecture/operations approval | passed | ADR-008 `OSS-DEC-17`, ADR-010 `STACK-DEC-18`, and approved profile dated 2026-08-19 |
| Fake-transport provider contracts | passed | OpenRouter deterministic suite 55/55 on 2026-08-20 after Phase 18 atomic `TryReserveExpected` plus Phase 17 live-harness remediation: reservation phase/expected-consumed gates, observer no-response isolation, and stricter matrix qualification. Prior coverage remains: persistent-budget `TryRead`, live HTTP/cache observer, symlink, sanitized-failure, unsafe-identity, headers, provider object, metadata/attempt/identity/usage, response-cache HIT vs prompt cached_tokens, control and streaming stall-after-headers timeout vs caller cancel, exact envelope limit and limit+1, strict SSE UTF-8, escaped invalid surrogates, terminal-then-DONE, discovery selected-endpoint, schema parity |
| Profile/credential/host isolation | passed | Digest regression; Infrastructure loaders; Unix owner-only secret tests; Worker Testing compose + Production fail-closed |
| Live synthetic qualification | failed closed | Retention-accepted budget remains 11/12. Phase 9 Lightning: slot 8 control HTTP 404; slot 9 content HTTP 200, usage 188/256. Phase 20: slot 10 Gemma and slot 11 Nano control HTTP 200 / `malformed_control`. Phase 21: slot 1/4 GPT-OSS control HTTP 200 / `malformed_control` / usage none / cache absent; content not reserved. Summaries include `synthetic-development-phase21-2026-08-20.md`. Acceptance label not applied |
| Interactive local Text Session | blocked | Phase 7: synthetic browser adapter; production HTTP SSE/OIDC still a documented gap (`docs/ui-ux/text-session.md`) |
| Locked regression/supply chain/OCI/docs | partial | Phase 18 re-verification: OpenRouter 55/55 deterministic with two explicit live tests excluded. Status reconciliation on 2026-08-20 corrected `docs/README.md` and `docs/contributing/workspace.md` to record the implemented adapter and partial live result; `python3 scripts/check_docs.py` and `git diff --check` passed, and the focused OpenRouter suite again passed 55/55 with two explicit live tests skipped. Docker remains unavailable, so PostgreSQL Testcontainers, Worker OCI image build, and `dotnet publish` were not run |
| Independent review (`7e2e438`) | passed | Adapter remediation series approved 2026-08-20: no remaining substantive correctness or architecture issues. Nine prior findings closed. Local OpenRouter 27/27 not independently verifiable from GitHub commit statuses. Live qualification and hosted Participant path remain gated |
| Independent review (`08c9304`) | passed | Live-harness series approved 2026-08-20: atomic expected reservation, phase/budget gates, request-scoped HTTP evidence, and fail-closed qualification predicate. No remaining actionable correctness issue. Local OpenRouter 55/55 not independently verifiable from GitHub commit statuses. Live result stays partial / 9 of 12 |
| Public free-route structured-output catalog (Phase 19) | passed (catalog only) | `GET /api/v1/models` plus `/endpoints` on 2026-08-20 without a key. 20 zero-price/free rows; 8 advertise `structured_outputs`. Pinned lightning endpoint still advertises neither parameter. No live slot consumed |
| Approved Phase 20 candidate decision | passed (documentation) | Authoritative profile, provider-profile index, documentation maturity summary, and active handoff updated on 2026-08-20. `python3 scripts/check_docs.py` and `git diff --check` passed. No operator pin was written and no live slot was consumed |
| Phase 20 exact endpoint and operator readiness review | passed | Rechecked immediately before reservation on 2026-08-20. Gemma/Darkbloom still advertises both structured-output parameters at zero price. Lightning files preserved. Gemma operator files written `0600`. Retention-accepted budget was `9/12` before the first Phase 20 reserve |
| Phase 20 live Gemma/Darkbloom control | failed closed | Slot 10/12: HTTP 200, cache absent, model accepted, usage 2322/256, `malformed_control`. Content not reserved |
| Phase 20 live Nano backup control | failed closed | Public catalog still listed Nvidia structured-output at zero price; endpoint tag was `nvidia/bf16` while the approved slug remained `nvidia`. Slot 11/12: HTTP 200, cache absent, model accepted, usage none, `malformed_control`. Content not reserved. Slot 12 unused |
| Focused OpenRouter readiness baseline | passed | Direct xUnit v3 runner after Phase 21 deterministic gates: 85 discovered, 80 passed, 5 explicit live tests not run, 0 failed |
| Approved Phase 21 GPT-OSS candidate plan | documented; live fail-closed | `openai/gpt-oss-20b:free` / `darkbloom` / `Darkbloom`; low/excluded reasoning; candidate-specific 1,024-token ceiling; below-256 visible-content acceptance; 1/4 ledger after one control probe. Operator pin exists outside Git. Qualification label not applied |
| Phase 21 catalog recheck before implementation | passed (catalog only) | `GET /api/v1/models/openai/gpt-oss-20b:free/endpoints` on 2026-08-20 without a key. One `darkbloom` / `Darkbloom` endpoint; prompt/completion `0`; advertised `structured_outputs`, `response_format`, `reasoning`, `reasoning_effort`, and `max_tokens`. No live slot consumed |
| Phase 21 deterministic gates | passed | OpenRouter 80/80 non-explicit immediately before live access on 2026-08-20. Five explicit live tests excluded. Historical 256-token contract unchanged |
| Phase 21 live GPT-OSS/Darkbloom control | failed closed | Immediate catalog recheck still listed zero-price Darkbloom structured-output/reasoning parameters. Slot 1/4: HTTP 200, cache absent, model accepted, usage none, `malformed_control`. Content not reserved. Historical 5/12 and 11/12 unchanged. Remaining 3/4 unused |

# Risks, interim defaults, and owner gates

| Topic | Risk | Interim default / gate |
| --- | --- | --- |
| Free model/provider availability | Candidate identity and capability can change without notice | Discover at qualification time, pin one concrete model/provider, fail closed on drift, and never fall back silently |
| Next structured-output free pin | Catalog-advertised routes can still fail the 256-token Decision envelope even when HTTP 200 | Recorded: Gemma used 256/256 output tokens then `malformed_control`; Nano also `malformed_control`. Do not repeat those probes. Phase 21 is the separately approved GPT-OSS/Darkbloom candidate and does not reuse historical slot 12. |
| GPT-OSS reasoning and route reliability | Reasoning consumes output budget even when excluded, and a free OpenRouter route may not enforce the advertised schema reliably | Phase 21 pins low/excluded reasoning, raises only its total ceiling to 1,024, retains canonical validation and a below-256 visible-content bound, and fails closed on malformed or unsupported control output |
| Direct OpenAI digest compatibility | A common profile extension could invalidate installed profiles and frozen bindings | Optional adapter-policy digest is excluded from the legacy digest source when absent; preserve with a known-value regression test |
| Provider schema compatibility | Canonical schema references are not resolvable by an external provider | Generate a self-contained strict adapter projection and keep canonical validation as final authority |
| Additive OpenRouter metadata | New harmless fields could break an overly rigid parser; missing critical fields could be ignored by an overly loose parser | Require and validate all known security/identity fields, ignore unknown additive fields, reject contradictions |
| Synthetic data-policy acceptance | Account/provider retention and training behavior may not be fully attestable through the inference response | Require explicit owner acceptance immediately before live access and prohibit all real/private or Production/Staging data; absent acceptance means no network call |
| Key spend/expiry | A general key may exceed the approved live-test risk boundary | Prefer a USD 2 / short-lived key; otherwise the harness still enforces its own 12-request/USD 2 stop and the owner explicitly accepts the broader credential scope |
| Unix permission portability | Owner-only mode checks do not map directly to every platform | Enforce on the current Unix-like target; fail live preflight elsewhere until a reviewed secure-source contract exists |
| Real browser/runtime path | Synthetic SPA behavior could be mistaken for real provider chat | Phase 7 must prove the route; if absent, record a blocker and seek a separately bounded scope update—no silent UI bypass |
| Live evidence sensitivity | Logs/screenshots could expose credentials, prompts, account data, or raw provider bodies | Synthetic prompts only, canary leakage tests first, sanitized summaries only, no raw live artifacts in Git |

# Blockers

Phases 0–6 have no blocker. Phase 7 is blocked on hosted Participant
Session/API/OIDC/SSE wiring. The amended owner data-policy gate for Phases 8–9
cleared on 2026-08-20. The old strict-policy budget remains historical at 5/12.
The distinct retention-accepted budget is 11/12. Historical Lightning files
remain; separate Gemma and Nano pins exist outside Git. Phase 9 Lightning
control was HTTP 404; Phase 20 Gemma and Nano control were HTTP 200
`malformed_control` with cache absent and no content reservation. Do not fall
back or switch to a paid model. Do not reuse the historical strict-policy 5/12
counter. Do not repeat recorded control requests. Historical slot 12 remains
unused. Phase 21 live control failed closed at 1/4 (`malformed_control`, HTTP
200, cache absent). Do not repeat that GPT-OSS control request. Full
`qualified_for: synthetic_development` remains unmet. Phase 7 hosted chat
remains blocked. Docker was unavailable for a live Worker/PostgreSQL path.

# Next executable slice

Phase 21 live control failed closed. Do not spend remaining Phase 21 slots,
historical slot 12, or repeat Lightning, Gemma, Nano, or GPT-OSS control
requests. A new owner-approved candidate or a separately scoped hosted
Participant path is required before further live OpenRouter work. Do not
relax the schema, enable fallback, or switch to a paid model.

# Development handoff

## Resume here

1. Retention-accepted budget is 11/12; historical budget stays 5/12. Historical
   Lightning operator files are preserved. Separate Gemma and Nano pins exist
   outside Git.
2. Discovery remains retired at consumed >= 6. Lightning `pinned-matrix` remains
   retired at 9. Gemma authorizes only at consumed 9. Nano backup authorizes
   only at consumed 10. The recorded 11/12 state refuses all current phases.
3. Phase 20 live control failed on both approved routes (`malformed_control`,
   HTTP 200, cache absent). Content was not reserved. The acceptance label was
   not applied.
4. Keep the task open: full live qualification is incomplete, hosted chat is
   blocked, and Docker was unavailable for a live Worker/PostgreSQL path.
5. Phase 21 live control failed: slot 1/4 HTTP 200 / `malformed_control` /
   cache absent / usage none. Content not reserved. Phase 21 budget is 1/4.
   Do not retry this candidate. The acceptance label was not applied. Keep
   the task open until a new approved candidate or a decision to stop
   synthetic-development qualification without the label.

## Implemented change map

| Concern | Primary implementation/test surfaces |
| --- | --- |
| Common frozen profile compatibility | `src/Modules/Sessions/FlexAgent.Sessions/Domain/FrozenModelDeployment.cs`; `tests/Sessions/FlexAgent.Sessions.Tests` |
| Shared installed-profile/catalog loading | move `InstalledProfileFiles.cs` behavior into `src/Modules/Sessions/FlexAgent.Sessions.Infrastructure`; add bounded parsing tests; preserve Direct OpenAI behavior |
| OpenRouter adapter/profile | new `src/Modules/Sessions/FlexAgent.Sessions.OpenRouter` and `tests/Sessions/FlexAgent.Sessions.OpenRouter.Tests` projects |
| Canonical strict output | `contracts/schemas/v2/session/agent-decision.v2.schema.json`, its primitive references and fixture catalog, plus an adapter-owned self-contained transport projection with parity tests |
| Worker opt-in/readiness | `src/Hosts/FlexAgent.Worker/WorkerDurableWorkSampling.cs`; `tests/Runtime/FlexAgent.Runtime.Tests/HostRuntimeTests.cs` |
| Architecture and packaging | `FlexAgent.slnx`, central/locked package graph, `tests/Architecture/FlexAgent.Architecture.Tests/ProviderAdapterBoundaryTests.cs`, `HostOciDockerfileTests.cs`, and `deploy/docker/worker.Dockerfile` |
| Durable integration | Sessions/PostgreSQL provider-request admission, provenance, publication, replay, and fault tests; migration head should remain `0029` |
| Live evidence | opt-in OpenRouter test/harness and, only after a real hosted path exists, Playwright evidence under `.playwright-mcp/`; sanitized qualification summaries under the approved operations directory |
| Phase 20 route gates | `OpenRouterLiveQualification` phase/count/digest constants; `OpenRouterLivePhase20QualificationTests` and runner; `OpenRouterLivePinnedRouteAcceptance` |
| Phase 21 route gates | `OpenRouterRequestPolicy.Phase21GptOss`; `OpenRouterQualificationBudget.CreatePhase21`; `OpenRouterLiveQualification` GPT-OSS constants; `OpenRouterLivePhase21QualificationTests` and runner; hidden-reasoning parser rejection |

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass
- [ ] Applicable integration/regression checks pass
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Adapter remediation series (`7e2e438`) and live-harness series
      (`08c9304`) are recorded as review-approved; the qualification task
      remains open: Phase 20 and Phase 21 live control both failed Decision
      admission, hosted chat is gated, and no route is labeled
      `qualified_for: synthetic_development`; historical slot 12 stays unused
