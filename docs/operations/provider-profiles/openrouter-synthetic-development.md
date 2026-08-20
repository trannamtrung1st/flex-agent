# OpenRouter synthetic-development profile

## Status and authority

| Field | Value |
| --- | --- |
| **Status** | Approved |
| **Owner** | Product Lead |
| **Approvers** | Product Lead, Architecture Lead |
| **Effective date** | 2026-08-19 |
| **Last reviewed** | 2026-08-20 |
| **Consulted perspectives** | Business analysis, architecture, security/privacy, documentation |
| **Governs** | Local synthetic OpenRouter calls, capability discovery, pinned free-model Session testing, credential placement, privacy/routing controls, bounded qualification evidence, and enablement limits |
| **Related decisions** | [ADR-008 `OSS-DEC-17`](../../architecture/decisions/ADR-008-bounded-oss-component-set.md#approved-decisions) and [ADR-010 `STACK-DEC-18`](../../architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md#decision) |

This profile governs development and operations behavior. It does not change the
assessment MVP, approve OpenRouter for real Participant data, qualify a
production provider profile, or close Direct OpenAI Phase B.

## Approved outcome

A developer may exercise the real OpenRouter network path and conduct natural,
interactive local text chat through Flex Agent using synthetic, non-sensitive
content. The response is real provider output rather than a deterministic demo
fixture. Deterministic fake providers remain mandatory for repeatable automated
tests.

The progression is:

1. Run fake-transport adapter contracts without a credential or network.
2. Use `openrouter/free` for bounded capability discovery and one smoke run.
3. Record the concrete returned model and selected provider identity from
   OpenRouter router metadata without retaining raw prompts or outputs.
4. Select one eligible concrete `:free` model, one permitted provider slug,
   and the corresponding selected-provider identity returned by metadata.
5. Freeze that concrete development profile for interactive local Session
   testing so structured-control and visible-content phases use the same model
   and provider.
6. Retain sanitized evidence and keep the profile labeled
   `synthetic_development`; do not enable it for production or real data.

`openrouter/free` randomly selects from its current eligible pool. It is not a
resolved model version, a deterministic test oracle, or acceptable frozen
identity for a real assessment Session.

## Data boundary

Allowed content:

- natural messages typed by a developer for testing;
- fictional Participants, Tasks, Submissions, rubrics, and assessment cases;
- generated or intentionally synthetic documents and conversation history;
- non-sensitive failure, cancellation, retry, reconnect, and recovery cases.

Prohibited content:

- real Participant or customer identity or contact data;
- real Submissions, transcripts, recordings, Evidence, Evaluations, reviewer
  notes, Results, Releases, or memory;
- credentials, tokens, secrets, production configuration, or private source
  material not explicitly cleared for provider disclosure;
- any content whose retention, training, residency, or secondary-use policy has
  not been approved for this external boundary.

The distinction is about data and qualification, not whether the model call is
real. Interactive synthetic chat is permitted; production or sensitive use is
not.

## Adapter and request contract

The approved adapter target is:

| Field | Approved value or rule |
| --- | --- |
| Adapter kind | Distinct `openrouter` adapter; never `direct_openai` |
| Contract | Versioned Sessions-owned adapter behind the provider-neutral execution port |
| API surface | OpenAI-compatible Chat Completions |
| Base URL | `https://openrouter.ai/api/v1` |
| Discovery model | `openrouter/free` |
| Repeatable Session model | One concrete eligible `:free` model selected after discovery |
| Structured control | Strict JSON Schema; unknown or invalid output fails validation |
| Streaming | Required for participant-visible content compatibility evidence |
| Fallback | Disabled; no alternate model, provider, payer, or endpoint substitution |
| Parameter support | Required for every parameter sent by Flex Agent |
| Provider pin | One provider slug in `provider.only` for repeatable Session requests |
| Router evidence | `X-OpenRouter-Metadata: enabled`; selected model/provider and attempt count are validated, while unknown additive metadata fields are ignored |
| Response cache | `X-OpenRouter-Cache: false`; reject `X-OpenRouter-Cache-Status: HIT`. Missing router metadata also fails. Provider `prompt_tokens_details.cached_tokens` is not a response-cache hit and is allowed under ZDR. |
| Returned identity | Response model and selected provider identity are recorded and checked against the active development profile |

Every request must include the equivalent of this provider object; the pinned
repeatable profile replaces the placeholder with its one reviewed provider
slug:

```json
{
  "provider": {
    "only": ["<permitted-provider-slug>"],
    "allow_fallbacks": false,
    "require_parameters": true,
    "data_collection": "deny",
    "zdr": true
  }
}
```

The discovery request may omit `provider.only` because its purpose is to find
an eligible model and provider. It must retain the other provider controls,
must enable router metadata, and must disable response caching. Structured
control requests additionally send `response_format.type=json_schema`,
`json_schema.strict=true`, a closed schema, and no response-healing plugin.
Visible-content requests stream; router metadata arrives in the terminal chunk.

The adapter must decode router metadata permissively for additive fields but
must require exactly one selected endpoint on a successful qualification
response, an attempt count of one, the expected returned model, and the
reviewed selected-provider identity corresponding to the pinned slug. If
synchronous metadata is absent after a request reached the router, the harness
may retrieve the generation record using its generation ID for diagnosis. That
lookup does not replace missing successful-response attempt evidence and cannot
turn the request into a passing qualification result.

If no free route satisfies the capability and privacy controls, the call must
fail. The developer must not silently remove strict structured output, permit
data collection, disable ZDR, enable fallback, or substitute a paid/different
model to obtain a successful response.

## Credential handling

- Use a dedicated OpenRouter qualification key when practical.
- Before a live run, the owner must verify in OpenRouter settings that private
  input/output logging and OpenRouter use of inputs/outputs are both disabled.
  Per-request provider filtering does not replace this account-level check.
- Store it as a regular mounted file outside the repository; do not pass it
  through browser/API input, source, JSON profiles, `.env`, command arguments,
  `.work/`, logs, telemetry, test fixtures, screenshots, or committed evidence.
- The portable local convention is
  `${XDG_CONFIG_HOME:-$HOME/.config}/flex-agent/secrets/openrouter-api-key`.
  The containing directory must be owner-only and the file must be
  owner-readable/writable only (`0700` directory, `0600` file on Unix-like
  systems). The local harness must reject a symlink, non-regular file, or
  group/other permission bits instead of merely warning.
- Configure the credential catalog with only the opaque binding, provider,
  owner scope, version, mode, and secret filename. Never store the raw key in
  the catalog.
- Revoke or rotate a temporary key after qualification when it is no longer
  needed.

The approved key policy is a USD 2 spending cap and a seven-day expiration when
the provider account supports those controls. A broader existing key may be
used only after the owner accepts its wider exposure; the raw value still must
use the mounted-file boundary.

## Execution bounds

| Bound | Approved value |
| --- | --- |
| Maximum inference requests in one qualification run | 12 |
| Maximum concurrent requests | 1 |
| Maximum Flex Agent inference attempts per operation | 2; every OpenRouter request must report router attempt `1` |
| Maximum output tokens per request | 256 |
| Structured-control timeout | 30 seconds |
| Participant-content timeout | 60 seconds |
| Maximum total provider spend | USD 2 |
| Load or sustained-capacity testing | Prohibited without separate approval |

Cancellation, timeout, retry, malformed-output, unavailable-route, and
rate-limit cases may consume the same 12-inference-request budget. Bounded
generation-metadata lookups used only to attest a completed inference do not
consume an inference slot, but they share the same concurrency and timeout
controls. The harness stops on budget exhaustion, unexpected identity,
privacy/routing-policy drift, secret failure, or any prohibited fallback.

## Evidence and acceptance

Sanitized evidence belongs under
`docs/operations/provider-profiles/qualified/openrouter/` only after a real run.
It may record:

- adapter contract and profile versions;
- discovery router plus returned concrete model and selected provider identity;
- pinned concrete `:free` model, permitted provider slug, and expected returned
  provider identity;
- capability and pass/fail results;
- aggregate request, token, latency, and cost facts;
- bounded failure categories and sanitized evidence digests;
- UTC qualification time and reviewer/approval state.

It must not contain keys, key fragments, account/workspace IDs, raw prompts,
raw responses, request/response bodies, sensitive headers, Participant data, or
hidden reasoning.

Acceptance requires executable evidence for streaming, strict structured
output without response healing, cancellation, timeout, bounded retry, failure
normalization, usage/provenance, identity matching, router metadata, cache
denial, privacy filters, credential isolation, budget enforcement, and
fail-closed no-fallback behavior. A successful result may be labeled only:

```text
qualified_for: synthetic_development
```

It must not be labeled `qualified_for: production` or
`qualified_for: participant_data`. Passing evidence does not automatically set
runtime qualification or enable traffic; enablement remains a separate explicit
owner decision.

## Failure and recovery

- A random-router result may inform selection but does not update a frozen
  Session profile automatically.
- A free model disappearing or becoming rate-limited makes the development
  profile unavailable; it does not authorize fallback.
- Structured-control and visible-content phases must not use different models
  or providers in one repeatable Session test.
- Provider or adapter failure preserves accepted Participant input under the
  governing Session specification and must not fabricate an Agent Decision or
  visible message.
- Revoked/missing credentials, route drift, missing router metadata, a cache
  hit, unexpected returned identity, unsupported parameters, privacy-filter
  failure, and budget exhaustion fail before further disclosure or publication.

## Implementation status

Approved design. Deterministic adapter, profile, Worker opt-in, fake-transport,
and architecture evidence exist behind `sessions.openrouter.v1`. Live OpenRouter
network qualification and hosted Participant Text Session chat remain gated:
live runs require owner privacy/spend preflight, and the Participant UI still
uses the synthetic browser adapter. Passing fake-transport evidence does not
enable production or participant-data use.

## References

- [Provider profile index](README.md)
- [ADR-008: bounded OSS component set](../../architecture/decisions/ADR-008-bounded-oss-component-set.md)
- [ADR-010: .NET implementation stack](../../architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md)
- [Resolved Session configuration](../../requirements/features/resolved-session-configuration.md)
- [Text Session lifecycle](../../requirements/features/session-text-lifecycle.md)
- [OpenRouter free-router documentation](https://openrouter.ai/docs/guides/routing/routers/free-router)
- [OpenRouter provider routing](https://openrouter.ai/docs/guides/routing/provider-selection)
- [OpenRouter structured outputs](https://openrouter.ai/docs/guides/features/structured-outputs)
- [OpenRouter zero-data-retention routing](https://openrouter.ai/docs/guides/features/zdr)
- [OpenRouter router metadata](https://openrouter.ai/docs/guides/features/router-metadata)
- [OpenRouter response caching](https://openrouter.ai/docs/guides/features/response-caching)
- [OpenRouter data collection](https://openrouter.ai/docs/guides/privacy/data-collection)
- [OpenRouter generation metadata](https://openrouter.ai/docs/api/api-reference/generations/get-generation)
