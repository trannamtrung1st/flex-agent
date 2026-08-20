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
  opt-in. The only current live pin is `openai/gpt-oss-20b:free` /
  `darkbloom` / `Darkbloom`. Phase 28 labels that pin
  `qualified_for: synthetic_development` under
  `finish_reason: stop`, 256-token acceptance headroom, and
  `openrouter.request-policy.v2`. Earlier Lightning, Gemma, Nano, and GLM
  probes are historical fail-closed records and are not executable live
  phases. Hosted Participant chat remains gated.
  `openrouter/free` is discovery/smoke only. This evidence does not qualify
  production or close the OpenAI-compatible endpoint qualification track
  (formerly Direct OpenAI Phase B).
- The [Keycloak OIDC contract profile](keycloak-oidc-contract.md) pins
  Keycloak `26.7.0` for local/CI human-authentication qualification. It is not
  a Production or real-Participant enablement.
- Do not commit API keys, prompts, participant data, or raw provider payloads
  here.

Target shape: `openai-compatible.profile.example.json`. It documents the
approved future identifiers and is intentionally marked non-enableable because
the runtime migration has not been implemented. Do not point Worker at this
example or set `Sessions:ModelExecution:Qualified=true` yet. The deterministic
runtime migration may complete with fake-transport, isolation, destination,
failure, and qualification-harness tests even when no exact live profile is
available. That completion does not qualify or enable the adapter. Afterwards,
copy the target shape outside the repository, replace placeholders, and run a
separate bounded qualification for the exact provider/operator, endpoint,
model/version-or-fingerprint, capability profile, credential mode, and data
policy against `GATE-STACK-PROVIDERS` before real use.

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
