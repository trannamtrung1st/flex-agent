# OpenRouter synthetic-development Phase 21 evidence

| Field | Value |
| --- | --- |
| UTC date | 2026-08-20 |
| Adapter contract | `sessions.openrouter.v1` |
| Qualification scope | `synthetic_development` |
| Model | `openai/gpt-oss-20b:free` |
| Provider slug | `darkbloom` |
| Expected identity | `Darkbloom` |
| Profile digest | `fb1fb631fc25dcc05c07b19345c00986f4120d34e751b3a922d1df7bc3d04b48` |
| Adapter-configuration digest | `7559112b33caad06504136309a0216e7bdf7643391a8bb7b4084245c517092fd` |
| Request policy | `max_tokens:4096`; `reasoning.effort:low`; `reasoning.exclude:true`; 120-second control and content timeouts |
| Control prompt | one valid v2 `no_action` example in the system text |
| Phase 21 budget after this run | 8/8 |
| Historical strict-policy budget | 5/24 (consumed unchanged) |
| Retention-accepted budget | 21/24 (unchanged by this run) |
| Acceptance label | Historical `qualified_for: synthetic_development` under the pre-Phase-24 predicate. Not sufficient to close the slice |

This file contains only sanitized operator facts. It does not contain keys, key
fragments, account identifiers, prompts, model text, request or response bodies,
reasoning traces, or authorization headers.

## Result

The Phase 21 live matrix **passed** on the owner-requested GPT-OSS retry after
the control example prompt and 4,096-token pin. The explicit runner reserved
control at 7/8, reserved content only after an admitted Decision, then
`TryQualify` succeeded. The runner printed
`qualified_for=synthetic_development` for
`openai/gpt-oss-20b:free` / `Darkbloom`.

```text
qualified_for: synthetic_development
```

This historical label applied only to the pinned synthetic-development
GPT-OSS/Darkbloom route behind the Sessions OpenRouter adapter. It does not
enable Production or Staging, authorize real or private data, or close hosted
Participant chat.

Phase 24 (2026-08-21) later required an explicit parsed `finish_reason` of
`stop`, 256-token visible-content acceptance headroom against a 4,096-token
GPT-OSS request ceiling, and `openrouter.request-policy.v2` in the adapter
digest. This file does not contain those facts. HTTP/usage lines for slots 7
and 8 were not retained. Do not invent them. Do not treat the 6/8 → 8/8
budget increment as proof of the later success properties. The Phase 24/25
predicate later passed on
[synthetic-development-phase28-2026-08-21.md](synthetic-development-phase28-2026-08-21.md).

## Sanitized request matrix

| Slot | Phase | Outcome |
| --- | --- | --- |
| 1–6/8 | earlier GPT-OSS control probes | fail-closed (`malformed_control`, timeout, or 429); content not reserved |
| 7/8 | GPT-OSS/Darkbloom control after example prompt | admitted `ModelExecutionStructuredControl`; content reserved |
| 8/8 | GPT-OSS/Darkbloom content | completed with at least one visible delta and output tokens below 4096 |

Per-request HTTP/usage lines from the passing run were not retained in the
agent terminal capture. The passing xUnit result and the 6/8 → 8/8 ledger
increment are the recorded evidence that both slots were consumed by a
completed matrix.

## Observed and not observed

Observed:

- catalog still listed free Darkbloom `structured_outputs` and `response_format` immediately before the retry
- owner-only GPT-OSS pin load-verified through the live runner
- admitted live structured Decision
- participant-visible streaming that qualified under the current acceptance bound

Not observed in this live run:

- Worker plus PostgreSQL durable admission/provenance against the live network
- hosted Participant Text Session chat
- Production or Staging enablement

## Enablement

This passing adapter-harness evidence does not turn on a runtime, authorize
Production or Staging, or close the OpenAI-compatible endpoint qualification
track (formerly Direct OpenAI Phase B). Hosted Participant chat remains a
separate delivery gap.
