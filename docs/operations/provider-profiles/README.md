# Provider deployment profiles

This directory holds **non-secret**, operator-reviewable examples for installed
model-provider profiles. It does not select a product-default model.

- Runtime composition stays default-off (`Sessions:ModelExecution:Adapter=fail_closed`).
- A real Session uses the frozen trusted binding, not Worker-global
  `Sessions:ModelDeployment:*` values, for provider, endpoint, model, and
  credential-binding identity.
- The approved generic adapter is `openai_compatible` /
  `sessions.openai_compatible.v1` in `FlexAgent.Sessions.OpenAiCompatible`.
  Installed profiles require a versioned adapter-configuration digest that
  binds the exact API base path and destination-policy identity. Historical
  `direct_openai` / `sessions.openai.v1` identities remain inspectable and
  cannot enable execution.
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
  production or close the OpenAI-compatible endpoint qualification track.
- The [Keycloak OIDC contract profile](keycloak-oidc-contract.md) pins
  Keycloak `26.7.0` for local/CI human-authentication qualification. It is not
  a Production or real-Participant enablement.
- Do not commit API keys, prompts, participant data, or raw provider payloads
  here.

Target shape: `openai-compatible.profile.example.json`,
`openai-compatible.configuration.example.json`, and
`openai-compatible.qualification.example.json`. They document the approved
identifiers and are intentionally marked non-enableable
(`openai-compatible.example.do-not-enable`, `qualifiedFor: do_not_enable`).
Do not point Worker at these examples or treat their presence as
qualification. Deterministic fake-transport, isolation, destination-policy,
and harness-gate tests may pass without a selected live profile. That
completion does not qualify or enable the adapter. Afterwards, copy the
target shape outside the repository, replace placeholders, and run a
separate bounded qualification for the exact provider/operator, endpoint,
model/version-or-fingerprint, capability profile, credential mode,
destination policy, and data policy against `GATE-STACK-PROVIDERS` before
real use.

Organization-hosted private endpoints require the additional approved
destination-policy evidence for canonical origin/base path, DNS resolution and
rebinding, private/link-local/metadata destinations, redirects, TLS trust,
egress, endpoint ownership, credential isolation, and cross-Organization
denial. The adapter's public-only policy admits only globally-routable unicast and
denies IANA special-purpose and non-global ranges, including documentation,
TEST-NET, and benchmarking space. Private-allowlist CIDRs must be wholly
contained in RFC1918 or IPv6 unique-local space and are stored with
canonical network bits. That evaluator is the deterministic control; live
private-endpoint evidence remains a successor gate.

Do not point the generic OpenAI-compatible adapter at OpenRouter: the adapters
have different kinds, base-path, routing, privacy, identity, and evidence
contracts. OpenRouter
operator examples are `openrouter-synthetic.profile.example.json` and
`openrouter-synthetic.configuration.example.json`; they are not enablement.
