# Provider deployment profiles

This directory holds **non-secret**, operator-reviewable examples for installed
model-provider profiles. It does not select a product-default model.

- Runtime composition stays default-off (`Sessions:ModelExecution:Adapter=fail_closed`).
- A real Session uses the frozen trusted binding, not Worker-global
  `Sessions:ModelDeployment:*` values, for provider, endpoint, model, and
  credential-binding identity.
- Direct OpenAI fake-transport contract tests are the deterministic evidence for
  the adapter. Live qualification against one exact owner-selected profile is
  opt-in (`FLEXAGENT_LIVE_OPENAI_QUALIFICATION=1`) and remains a completion
  blocker until that profile, mounted credential, and data-policy determination
  are supplied.
- The approved [OpenRouter synthetic-development profile](openrouter-synthetic-development.md)
  has a distinct `sessions.openrouter.v1` adapter and Worker synthetic-development
  opt-in. Fake-transport evidence exists. A 2026-08-20 pinned live matrix proved
  streaming on one `:free` pair and recorded a structured-control 404; it is not
  labeled `qualified_for: synthetic_development`. Hosted Participant chat remains
  gated. `openrouter/free` is discovery/smoke only;
  repeatable Session testing pins one concrete `:free` model and one permitted
  provider slug. This evidence does not qualify production or close Direct OpenAI
  Phase B.
- The [Keycloak OIDC contract profile](keycloak-oidc-contract.md) pins
  Keycloak `26.7.0` for local/CI human-authentication qualification. It is not
  a Production or real-Participant enablement.
- Do not commit API keys, prompts, participant data, or raw provider payloads
  here.

Example shape: `direct-openai.profile.example.json`. Copy it outside the
repository, replace placeholders, and point Worker
`Sessions:ModelExecution:InstalledProfilesPath` and
`Sessions:ModelExecution:CredentialCatalogPath` at the operator-managed files.
Set `Sessions:ModelExecution:Qualified=true` only after the exact profile has
passed the Direct OpenAI subset of `GATE-STACK-PROVIDERS`.

Do not point the Direct OpenAI adapter at OpenRouter: the adapters have different
kinds, base-path, routing, privacy, identity, and evidence contracts. OpenRouter
operator examples are `openrouter-synthetic.profile.example.json` and
`openrouter-synthetic.configuration.example.json`; they are not enablement.
