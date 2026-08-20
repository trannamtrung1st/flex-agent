# Provider deployment profiles

This directory holds **non-secret**, operator-reviewable examples for installed
model-provider profiles. It does not select a product-default model.

- Runtime composition stays default-off (`Sessions:ModelExecution:Adapter=fail_closed`).
- A real Session uses the frozen trusted binding, not Worker-global
  `Sessions:ModelDeployment:*` values, for provider, endpoint, model, and
  credential-binding identity.
- The approved target is the vendor-neutral `openai_compatible` adapter contract
  for an exact operator-selected external, Organization-hosted, self-hosted, or
  managed endpoint. OpenAI-hosted service is not preferred or assumed. The
  current `direct_openai` adapter, `sessions.openai.v1` contract, environment
  variable, and `FlexAgent.Sessions.OpenAi` tests are legacy implementation
  surfaces; they remain default-off and do not qualify the approved target.
- The approved [OpenRouter synthetic-development profile](openrouter-synthetic-development.md)
  has a distinct `sessions.openrouter.v1` adapter and Worker synthetic-development
  opt-in. Fake-transport evidence exists. A 2026-08-20 pinned live matrix proved
  streaming on one `:free` pair and recorded a structured-control 404; it is not
  labeled `qualified_for: synthetic_development`. The Product Lead subsequently
  approved `google/gemma-4-26b-a4b-it:free` / `darkbloom` / `Darkbloom` as the
  next bounded live qualification candidate. Phase 20 implemented the distinct
  gate/runner, operator pins, and reserved control probes; both Gemma and the
  Nano backup failed structured Decision admission without reserving content,
  so the run is not labeled `qualified_for: synthetic_development`. The next
  approved candidate is `openai/gpt-oss-20b:free` / `darkbloom` / `Darkbloom`
  under a separately budgeted Phase 21 with low, excluded reasoning and a
  1,024-token candidate ceiling. Deterministic gates and an owner-only pin are
  implemented. The 2026-08-20 live control reserved slot 1/4 and failed Decision
  admission as `malformed_control` with HTTP 200, cache absent, and no content
  reservation. No qualification label or enablement follows. Do not repeat that
  control request. Hosted
  Participant chat remains gated. `openrouter/free` is discovery/smoke only;
  repeatable Session testing pins one concrete `:free` model and one permitted
  provider slug. This evidence does not qualify production or close the
  OpenAI-compatible endpoint qualification track (formerly Direct OpenAI Phase
  B).
- The [Keycloak OIDC contract profile](keycloak-oidc-contract.md) pins
  Keycloak `26.7.0` for local/CI human-authentication qualification. It is not
  a Production or real-Participant enablement.
- Do not commit API keys, prompts, participant data, or raw provider payloads
  here.

Target shape: `openai-compatible.profile.example.json`. It documents the
approved future identifiers and is intentionally marked non-enableable because
the runtime migration has not been implemented. Do not point Worker at this
example or set `Sessions:ModelExecution:Qualified=true` yet. After the
implementation migration, copy the target shape outside the repository,
replace placeholders, and qualify the exact provider/operator, endpoint,
model/version-or-fingerprint, capability profile, credential mode, and data
policy against `GATE-STACK-PROVIDERS`.

Organization-hosted private endpoints require the additional approved
destination-policy evidence for canonical origin/base path, DNS resolution and
rebinding, private/link-local/metadata destinations, redirects, TLS trust,
egress, endpoint ownership, credential isolation, and cross-Organization
denial. The legacy adapter's literal-IP and same-origin checks are retained as
partial controls, but they do not resolve DNS or prove rebinding resistance.
The legacy adapter therefore remains disabled for private endpoints and must
not be treated as satisfying this gate.

Do not point the generic OpenAI-compatible adapter at OpenRouter: the adapters
have different kinds, base-path, routing, privacy, identity, and evidence
contracts. OpenRouter
operator examples are `openrouter-synthetic.profile.example.json` and
`openrouter-synthetic.configuration.example.json`; they are not enablement.
