---
id: promote-openrouter-synthetic-development-profile
status: completed
created: 2026-08-19
updated: 2026-08-20
---

# Goal

Promote the Product Lead's approved OpenRouter synthetic-development direction
into the authoritative architecture and operations documentation without
changing the assessment MVP, weakening frozen Session configuration, or
representing a free/dynamic router as production-qualified.

# Governing sources

- `AGENTS.md`, `docs/README.md`, and `.work/README.md` — authority by concern,
  product invariants, documentation status, and tracked-work rules
- `docs/product/concept-model.md`, `docs/product/mvp-scope.md`, and
  `docs/product/overview.md` — provider-neutral product meaning, text-only MVP,
  and production-gate sequencing
- `docs/architecture/decisions/ADR-008-bounded-oss-component-set.md` — approved
  synthetic OpenRouter exploration, frozen-profile, credential, privacy, and
  qualification boundaries
- `docs/architecture/decisions/ADR-010-dotnet-implementation-stack-and-workspace.md`
  — provider-adapter ownership, OpenAI-compatible SDK boundary, and
  `GATE-STACK-PROVIDERS`
- `docs/requirements/features/resolved-session-configuration.md` and
  `docs/requirements/features/session-text-lifecycle.md` — frozen model
  identity, provider-neutral execution, publication, and provenance behavior
- Product Lead approval on 2026-08-19 of the bounded OpenRouter proposal:
  synthetic-only real calls and local interactive chat, `openrouter/free` for
  discovery/smoke testing, a concrete `:free` model and provider for repeatable
  Session testing, mounted-file credentials, strict privacy/routing controls,
  bounded cost, sanitized evidence, and no automatic production enablement

# Scope

## In

- Amend existing approved ADRs without erasing their original decisions.
- Publish an approved operator/developer profile for real OpenRouter calls with
  synthetic, non-sensitive content.
- Separate random free-router exploration from pinned free-model Session
  testing and from production qualification.
- Record credential, privacy, routing, cost, evidence, failure, and enablement
  controls.
- Correct stale roadmap/status text for already-completed production HTTP SSE
  and Worker polling work.
- Create a separate planned implementation task for the OpenRouter adapter and
  qualification harness.

## Out

- Adapter, host, schema, migration, test, UI, deployment, or provider calls.
- Real Participant, customer, Submission, transcript, Evidence, Evaluation,
  Result, or other sensitive data.
- Closing Direct OpenAI Phase B, qualifying OpenRouter for production, or
  changing the seven-step MVP workflow.
- Committing, pushing, or enabling runtime traffic.

# Plan

- [x] Recheck authority, existing provider decisions, implementation state, and
      active task boundaries.
- [x] Amend the approved architecture decisions and publish the bounded
      operations profile.
- [x] Reconcile documentation hubs, roadmap/status tables, and provider-gate
      wording.
- [x] Create the separately tracked OpenRouter implementation task.
- [x] Run documentation/link/whitespace validation and review the complete diff.
- [x] Reconcile this task and prepare the documentation handoff.
- [x] Re-verify the approved profile against current official OpenRouter
      contracts and reconcile any correctness or consistency findings.
- [x] Re-run documentation validation, inspect the resulting diff, and close
      the follow-up review.

# Current state

Promotion was completed on 2026-08-19 and the 2026-08-20 consistency review is
complete. ADR-008 and ADR-010 contain the approved amendments; the
provider-profile runbook governs safe real-call and interactive-chat use; hubs,
roadmap, maturity, and gate tables are reconciled; and a separate planned task
owns implementation. The follow-up corrected request nesting, provider
identity evidence, caching, privacy preflight, and mounted-file permission
requirements. No adapter or live-provider call was made.

# Decisions

- Preserve Direct OpenAI as the separately blocked production-qualification
  track; OpenRouter does not substitute for its evidence.
- Permit real OpenRouter calls and natural local chat only with synthetic,
  non-sensitive content.
- Use `openrouter/free` only for capability discovery and smoke testing; use a
  concrete `:free` model and one provider for repeatable two-phase Session
  execution.
- Keep runtime default-off and fail closed when routing, privacy, identity,
  credential, or budget controls cannot be enforced.

# Findings / deviations

- `docs/product/overview.md` and `docs/contributing/workspace.md` still describe
  production HTTP SSE and/or Worker polling as pending although their retained
  successor task records mark those slices completed.
- The current Direct OpenAI adapter accepts only the `direct_openai` adapter
  kind and an origin-only endpoint. OpenRouter requires a separate adapter
  contract and path-aware `/api/v1` handling; a profile-only substitution would
  be incorrect.
- No requirements or UI/UX specification changed: the profile is development
  infrastructure and preserves existing Text Session observable behavior.
- The original request example placed OpenRouter provider preferences at the
  top level. Official Chat Completions uses a nested `provider` object.
- Provider identity is not safe to infer from the returned model alone.
  Successful qualification now requires opt-in router metadata, one selected
  provider, attempt `1`, and explicit response-cache denial; generation lookup
  remains diagnostic only.
- OpenRouter input/output logging and use-of-inputs/outputs are independent
  account settings. The live preflight now requires both disabled in addition
  to per-request provider privacy controls.
- The existing mounted-file secret source does not enforce owner-only Unix
  modes, so permission enforcement and negative tests remain implementation
  work rather than completed evidence.

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| Governing product/requirements/ADR review | passed | Product foundation, ADR-008, ADR-010, provider operations guidance, current Direct OpenAI task, and implementation seams reviewed 2026-08-19 |
| Documentation validation | passed | `python3 scripts/check_docs.py` — documentation validation passed, 2026-08-19 |
| Whitespace validation | passed | `git diff --check` — no output, exit 0 |
| Diff and status review | passed | `git diff --stat`, targeted/full diff review, and `git status --short`; changes are documentation and tracked task state only |
| Official OpenRouter contract review | passed | Free router, provider routing, strict structured outputs, router metadata, response caching, ZDR, data collection, generation metadata, and API-key controls reviewed 2026-08-20 |
| Follow-up documentation validation | passed | `python3 scripts/check_docs.py` — documentation validation passed, 2026-08-20 |
| Follow-up whitespace validation | passed | `git diff --check` — no output, exit 0, 2026-08-20 |

# Blockers

None for documentation promotion. Implementation remains pending and live calls
remain blocked until the key is mounted outside the repository.

# Completion

- [x] Planned work is reconciled with actual changes
- [x] Applicable focused tests pass (`scripts/check_docs.py`)
- [-] Runtime integration/regression checks are not applicable to this docs-only promotion; no behavior changed
- [x] Governing specifications were rechecked
- [x] Remaining gaps or unverified behavior are recorded
- [x] Task state is safe and complete for external review
