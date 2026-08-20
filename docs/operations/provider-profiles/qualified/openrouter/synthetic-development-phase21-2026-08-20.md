# OpenRouter synthetic-development Phase 21 evidence

| Field | Value |
| --- | --- |
| UTC date | 2026-08-20 |
| Adapter contract | `sessions.openrouter.v1` |
| Qualification scope | `synthetic_development` |
| Model | `openai/gpt-oss-20b:free` |
| Provider slug | `darkbloom` |
| Expected identity | `Darkbloom` |
| Profile digest | `64f98960972b425ed65e4db960836f59e4bebfd386f0076af295334f49a6ebf5` |
| Adapter-configuration digest | `d392ac50dafcfedd6810afec54016d0e8867f6a7401b61558016382c08b9e7bd` |
| Request policy | `max_tokens:1024`; `reasoning.effort:low`; `reasoning.exclude:true` |
| Phase 21 budget after this run | 1/4 |
| Historical strict-policy budget | 5/12 (unchanged) |
| Retention-accepted budget | 11/12 (unchanged) |
| Historical Lightning, Gemma, and Nano pins | preserved; not reused |
| Acceptance label | not applied |

This file contains only sanitized operator facts. It does not contain keys, key
fragments, account identifiers, prompts, model text, request or response bodies,
reasoning traces, or authorization headers.

## Result

The Phase 21 live matrix is **incomplete**. The reserved structured-control
probe reached HTTP 200 with cache absent and adapter-validated model identity,
then failed Flex Agent Decision admission as `malformed_control`. Usage tokens
were not recorded on the failed control. The runner stopped before reserving
participant-visible content.

Do not label this run:

```text
qualified_for: synthetic_development
```

Malformed control is terminal for this candidate. Do not retry the same
control request, spend the remaining 3/4 slots on another model, relax the
schema, enable fallback, or switch to a paid model.

## Sanitized request matrix

| Slot | Phase | HTTP | Class | Cache | Usage | Outcome |
| --- | --- | --- | --- | --- | --- | --- |
| 1/4 | GPT-OSS/Darkbloom control | 200 | `ok` | absent | none recorded | `malformed_control`; content not reserved |

Returned model identity matched the exact pinned route. No
`X-OpenRouter-Cache-Status: HIT` was observed. Slots 2–4/4 remain unused.

## Observed and not observed

Observed:

- distinct Phase 21 0/4 ledger separate from historical 5/12 and 11/12 files
- owner-only GPT-OSS operator files load-verified through the live runner
- control-before-content stop after a non-admitted Decision
- HTTP 200 identity and cache-denial on the control probe

Not observed in this live run:

- admitted live structured Decision
- participant-visible streaming
- timeout, caller cancellation, application retry, cutoff, or reconnect
- Worker plus PostgreSQL durable admission/provenance against the live network
- hosted Participant Text Session chat

## Enablement

Passing or partial live evidence does not enable a runtime, authorize Production
or Staging, or close the OpenAI-compatible endpoint qualification track
(formerly Direct OpenAI Phase B). Hosted Participant chat remains a separate
delivery gap. Do not reuse this GPT-OSS control request.
