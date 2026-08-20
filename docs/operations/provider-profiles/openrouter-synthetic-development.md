# OpenRouter synthetic-development profile

## Status and authority

| Field | Value |
| --- | --- |
| **Status** | Approved |
| **Owner** | Product Lead |
| **Approvers** | Product Lead, Architecture Lead |
| **Effective date** | 2026-08-19 |
| **Last reviewed** | 2026-08-20 |
| **Decision reference** | Product Lead decisions on 2026-08-20 to (1) permit provider/OpenRouter retention and training for synthetic-only solo development, (2) approve and execute the bounded Gemma/Darkbloom Phase 20 candidate with a fail-closed result, and (3) approve and live-verify `openai/gpt-oss-20b:free` / Darkbloom as the separately budgeted Phase 21 candidate with a fail-closed result, preserving all production, real-data, and enablement gates |
| **Consulted perspectives** | Business analysis, architecture, security/privacy, documentation |
| **Governs** | Local synthetic OpenRouter calls, capability discovery, pinned free-model Session testing, credential placement, data-policy/routing controls, bounded qualification evidence, and enablement limits |
| **Related decisions** | [ADR-008 `OSS-DEC-17`](../../architecture/decisions/ADR-008-bounded-oss-component-set.md#approved-decisions) and [ADR-010 `STACK-DEC-18`](../../architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md#decision) |

This profile governs development and operations behavior. It does not change the
assessment MVP, approve OpenRouter for real Participant data, qualify a
production provider profile, or close the OpenAI-compatible endpoint
qualification track (formerly Direct OpenAI Phase B).

The 2026-08-20 development amendment accepts that OpenRouter and selected model
providers may retain synthetic prompts/responses and may use them for training
or product improvement. This risk acceptance is limited to intentional
synthetic local development. It is not consent or authorization for real
Participant, customer, Submission, transcript, Evidence, Evaluation, Result,
Release, memory, credential, private-source, or production data.

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

## Approved next live qualification candidate

| Field | Approved value or rule |
| --- | --- |
| Decision status | Approved candidate selection executed; live structured control failed closed; not qualified |
| Primary model | `google/gemma-4-26b-a4b-it:free` |
| Provider slug | `darkbloom` |
| Expected returned provider identity | `Darkbloom` |
| Excluded sibling endpoint | `google-ai-studio`; its catalog endpoint does not advertise `structured_outputs` |
| Backup candidate | `nvidia/nemotron-nano-9b-v2:free` / `nvidia` / `Nvidia` |
| Catalog verification | Rechecked 2026-08-20 against the exact free-route endpoint catalogs; primary fields still match the approved pair |
| Backup availability limit | OpenRouter currently reports an expiration date of 2026-08-24; recheck immediately before any backup pin |
| Qualification budget state | Retention-accepted counter is `11/12` after Phase 20; the historical strict-policy counter remains `5/12` and must not be reused |
| First live action | Recorded: reserved Gemma/Darkbloom control at consumed `9` (slot 10) and Nano/Nvidia backup control at consumed `10` (slot 11). Both failed Decision admission. Slot `12` is unused |

The Product Lead approves the primary pair above as the next explicit
operator-managed pin for bounded synthetic-development qualification. Public
catalog fields advertise both `structured_outputs` and `response_format` for
the Darkbloom endpoint, and reasoning is not mandatory or default-on. Those
facts make it the preferred first probe under the 256-output-token bound, but
they are not live capability evidence and do not qualify or enable the route.

The qualification sequence must:

1. preserve the historical Lightning operator pin and Phase 9 evidence;
2. implement and deterministically verify the distinct hardened Phase 20 gate
   and runner described below;
3. create a separately identified Gemma/Darkbloom operator profile and
   configuration outside Git, then recompute and load-verify both digests;
4. run the immediate endpoint, credential, budget, and deterministic preflight,
   then reserve exactly one control request as
   the first live action;
5. run participant-visible streaming on the same frozen pair only after strict
   control passes, and require visible, non-truncated content below the
   256-token ceiling; and
6. retain sanitized evidence and apply no qualification label unless both live
   phases and every existing acceptance gate pass.

If the primary control probe fails, the backup is a new deliberate operator
pin after the failure is recorded. It is not an automatic router fallback and
does not permit changing `allow_fallbacks:false`. Starting from `9/12`, a
primary control failure followed by backup control and content would consume
the three remaining slots and leave no retry or transient-failure contingency.
Because the backup is cataloged to expire on 2026-08-24, an absent, expired, or
changed backup route must fail closed before reservation and return to owner
selection; it does not authorize an unreviewed replacement.

### Implementation-readiness gates

The Phase 20 repository blockers are closed:

- Distinct `gemma-darkbloom-matrix` and `nemotron-nano-backup-matrix` phases
  exist. Historical discovery and Lightning `pinned-matrix` remain retired at
  their recorded counts and were not reused.
- The Phase 20 runner reserves participant-visible content only after an
  admitted structured Agent Decision. Both live control failures stopped before
  a second reservation.
- Deterministic tests prove exact phase/count admission, stale or wrong phase
  refusal, exact operator identity and digest checks, and zero content
  reservation after unsuccessful control.

Those gates do not qualify the route. Live Phase 20 control failed Decision
admission on both approved candidates.

## Approved Phase 21 GPT-OSS candidate

| Field | Approved value or rule |
| --- | --- |
| Decision status | Candidate approved and live-verified fail-closed; deterministic gates remain; operator pin exists outside Git; acceptance label not applied |
| Model | `openai/gpt-oss-20b:free` |
| Provider slug | `darkbloom` |
| Expected returned provider identity | `Darkbloom` |
| Selection basis | OpenAI documents GPT-OSS 20B as supporting structured outputs and configurable reasoning; the 2026-08-20 OpenRouter catalog advertised `structured_outputs` and `response_format` for the exact free Darkbloom route |
| Reasoning request | `reasoning.effort: "low"` and `reasoning.exclude: true`; reasoning remains counted in output usage and must not be persisted or exposed |
| Phase 21 output ceiling | 1,024 total output tokens per request, including reasoning; this is a candidate-specific bound and does not rewrite historical 256-token evidence |
| Visible-content acceptance | The content phase must still finish below 256 output tokens and contain visible, non-truncated Participant-facing text |
| Qualification budget | New Phase 21 ledger, maximum 4 inference requests, concurrency 1, maximum USD 2; now 1/4 after the fail-closed control probe |
| Historical budget handling | Preserve the retention-accepted 11/12 and strict-policy 5/12 ledgers; do not spend or relabel historical slot 12 |
| Verification state | Live control on 2026-08-20 returned HTTP 200, cache absent, validated model identity, and `malformed_control`. Content was not reserved. Phase 21 budget is 1/4. Catalog still listed the free Darkbloom structured-output route immediately before the request. Qualification label not applied |

GPT-OSS 20B is selected because its native structured-output contract is a
closer fit for `agent-decision.v2` than repeating the failed Gemma, Nemotron
Nano, or Lightning probes. Candidate selection is not a reliability claim.
OpenRouter route behavior remains independently variable, and catalog support
does not prove that the exact free provider can return an admissible Decision.

Before any live request, Phase 21 must:

1. recheck that the exact free Darkbloom endpoint is present, zero-priced, and
   advertises every sent parameter, including strict structured output and the
   selected reasoning controls;
2. add deterministic request-shape, parser, budget, phase, identity, and
   fail-closed tests before changing the live runner. Those deterministic
   gates now exist; they do not authorize a pin or live request;
3. create a distinct owner-only GPT-OSS operator profile outside Git, compute
   and load-verify its profile and adapter-configuration digests, and bind the
   runner to those exact values rather than environment-selected identity;
4. keep the historical Phase 9 and Phase 20 pins, ledgers, runners, and evidence
   immutable and independently inspectable;
5. obtain immediate owner confirmation of the synthetic-only data-policy,
   credential spend/expiry boundary, new 0/4 ledger, and exact candidate; and
6. reserve participant-visible content only after the same frozen pair returns
   one canonically admitted structured Agent Decision. A malformed, truncated,
   unsupported, cached, fallback, identity-mismatched, or policy-drifted result
   stops without content publication or qualification.

The four-request ceiling provides one control request, one content request only
after control passes, and at most one bounded transient retry per operation
under the existing two-attempt rule. A schema, capability, identity, cache,
policy, or malformed-output failure is not transient and must not be retried.
Unused requests confer no permission to test another model or provider.

## Data boundary

Allowed content:

- natural messages typed by a developer for testing;
- fictional Participants, Tasks, Submissions, rubrics, and assessment cases;
- generated or intentionally synthetic documents and conversation history;
- non-sensitive failure, cancellation, retry, reconnect, and recovery cases.

Allowed synthetic content is treated as externally retained and potentially
reused for training. Developers must not put material into this profile unless
they are willing for it to leave the project boundary under those terms.

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
| Adapter kind | Distinct `openrouter` adapter; never `openai_compatible` (and never the legacy `direct_openai`) |
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
| Development data policy | `data_collection: "allow"`; do not enforce request-level ZDR. Provider/OpenRouter retention and training are accepted only for synthetic content |
| Router evidence | `X-OpenRouter-Metadata: enabled`; selected model/provider and attempt count are validated, while unknown additive metadata fields are ignored |
| Response cache | `X-OpenRouter-Cache: false`; reject `X-OpenRouter-Cache-Status: HIT`. Missing router metadata also fails. Provider `prompt_tokens_details.cached_tokens` is not an OpenRouter response-cache hit and remains admissible under this retention-accepted synthetic profile. |
| Returned identity | Response model and selected provider identity are recorded and checked against the active development profile. Discovery rejects overlong or control-character-bearing identity values before they can enter sanitized evidence or pinned configuration |

Every request must include the equivalent of this provider object; the pinned
repeatable profile replaces the placeholder with its one reviewed provider
slug:

```json
{
  "provider": {
    "only": ["<permitted-provider-slug>"],
    "allow_fallbacks": false,
    "require_parameters": true,
    "data_collection": "allow",
    "zdr": false
  }
}
```

The discovery request may omit `provider.only` because its purpose is to find
an eligible model and provider. It must retain the other provider controls,
including the explicit synthetic-development data policy, must enable router
metadata, and must disable response caching. Structured
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

If no free route satisfies the capability and routing controls, the call must
fail. The developer must not silently remove strict structured output, enable
fallback, or substitute a paid/different model to obtain a successful response.
Changing the synthetic-development data policy again requires another explicit
Product Lead decision and a new profile digest.

## Credential handling

- Use a dedicated OpenRouter qualification key when practical.
- Before a live run, the owner must explicitly attest that all disclosed
  content is synthetic and accept the current account-level logging,
  retention, training, and OpenRouter use-of-input/output settings. Missing
  acceptance means no network call. The acceptance sentinel is development-only
  and cannot authorize real or production data.
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
| Maximum inference requests in one qualification run | Historical run: 12; separately approved Phase 21 GPT-OSS run: 4 |
| Maximum concurrent requests | 1 |
| Maximum Flex Agent inference attempts per operation | 2; every OpenRouter request must report router attempt `1` |
| Maximum output tokens per request | Historical and default OpenRouter profile: 256; Phase 21 GPT-OSS candidate: 1,024 total including reasoning, while visible-content acceptance remains below 256 |
| Structured-control timeout | 30 seconds |
| Participant-content timeout | 60 seconds |
| Maximum total provider spend | USD 2 |
| Load or sustained-capacity testing | Prohibited without separate approval |

Cancellation, timeout, retry, malformed-output, unavailable-route, and
rate-limit cases may consume the same 12-inference-request budget. Bounded
generation-metadata lookups used only to attest a completed inference do not
consume an inference slot, but they share the same concurrency and timeout
controls. The harness stops on budget exhaustion, unexpected identity,
data-policy or routing-policy drift, secret failure, or any prohibited fallback.

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
denial, explicit synthetic-data-policy acceptance, credential isolation,
budget enforcement, and
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
  hit, unexpected returned identity, unsupported parameters, missing synthetic
  data-policy acceptance, and budget exhaustion fail before further disclosure
  or publication.

## Implementation status

Approved design. Deterministic adapter, profile, Worker opt-in, fake-transport,
and architecture evidence exist behind `sessions.openrouter.v1`. A pinned live
Phase 9 matrix on 2026-08-20 proved streaming identity, cache-denial, metadata,
and usage for `nvidia/nemotron-3.5-lightning:free` / `Nvidia`, and proved the
same route rejects strict structured-control with HTTP 404. The run is not
labeled `qualified_for: synthetic_development`. Phase 20 implemented the
route-specific gates and runners, wrote separate owner-only Gemma and Nano
operator pins, and reserved two structured-control probes on 2026-08-20. Both
returned HTTP 200 with cache absent and validated model identity, then failed
Decision admission as `malformed_control` without reserving content. Sanitized
evidence is
[synthetic-development-phase20-2026-08-20.md](qualified/openrouter/synthetic-development-phase20-2026-08-20.md).
The run is not labeled `qualified_for: synthetic_development`. Hosted
Participant Text Session chat remains gated on the synthetic browser adapter.
Passing fake-transport, catalog, or partial live evidence does not enable
production or participant-data use.

Phase 21 implemented a distinct `gpt-oss-darkbloom-matrix` gate, a separate
`openrouter_qualification_budget.phase21.v1` ledger, and an explicit opt-in
runner bound to `openai/gpt-oss-20b:free` / `darkbloom` / `Darkbloom` with
`max_tokens:1024` and `reasoning:{effort:"low",exclude:true}`. Historical
256-token profiles and the 5/12 and 11/12 ledgers remain unchanged. The
2026-08-20 live control reserved slot 1/4, returned HTTP 200 with cache absent
and validated model identity, then failed Decision admission as
`malformed_control`. Content was not reserved. Sanitized evidence is
[synthetic-development-phase21-2026-08-20.md](qualified/openrouter/synthetic-development-phase21-2026-08-20.md).
The run is not labeled `qualified_for: synthetic_development`. Do not repeat
this control request. Catalog advertisement is still not live qualification.

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
- [Gemma free-route endpoint catalog](https://openrouter.ai/api/v1/models/google/gemma-4-26b-a4b-it-20260403%3Afree/endpoints)
- [Nemotron Nano free-route endpoint catalog](https://openrouter.ai/api/v1/models/nvidia/nemotron-nano-9b-v2%3Afree/endpoints)
- [OpenAI GPT-OSS 20B model documentation](https://developers.openai.com/api/docs/models/gpt-oss-20b)
- [OpenRouter GPT-OSS 20B free model](https://openrouter.ai/openai/gpt-oss-20b:free)
- [OpenRouter reasoning-token controls](https://openrouter.ai/docs/guides/best-practices/reasoning-tokens)
