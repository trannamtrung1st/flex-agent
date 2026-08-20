# OpenRouter synthetic-development Phase 22 evidence

This GLM/Decart probe is a historical retired candidate. The current live pin
is GPT-OSS/Darkbloom.

| Field | Value |
| --- | --- |
| UTC date | 2026-08-20 |
| Adapter contract | `sessions.openrouter.v1` |
| Qualification scope | `synthetic_development` |
| Model | `z-ai/glm-5.2:free` |
| Provider slug | `decart` |
| Expected identity | `Decart` |
| Profile digest | `2f2f4cb341dbde452fc612812564262705db82d82c6628436e5d2dcf623f982a` |
| Adapter-configuration digest | `fe31287607a3728fd42b8f562dff085ddbc9b16d0e902f2b09692933d7bc85c3` |
| Request policy | default `max_tokens:4096`; no `reasoning` object |
| Catalog before pin | one `Decart` / `decart/fp4` endpoint; prompt/completion `0`; `structured_outputs`, `response_format`, `max_tokens` |
| Retention-accepted budget after this run | 21/24 |
| Historical strict-policy budget | 5/24 (consumed unchanged) |
| Phase 21 budget | 6/8 (unchanged) |
| Acceptance label | not applied |

This file contains only sanitized operator facts. It does not contain keys, key
fragments, account identifiers, prompts, model text, request or response bodies,
or authorization headers.

## Result

The Phase 22 live matrix is **incomplete**. Both reserved structured-control
probes returned HTTP 429 / `rate_limited` with cache absent and no usage.
Content was not reserved. The pin loaded and the returned model identity was
the pinned GLM route.

Do not label this run:

```text
qualified_for: synthetic_development
```

Do not immediately retry this control request.

## Sanitized request matrix

| Slot | Phase | HTTP | Class | Cache | Usage | Outcome |
| --- | --- | --- | --- | --- | --- | --- |
| 20/24 | GLM/Decart control | 429 | `rate_limited` | absent | none recorded | `provider_unavailable`; content not reserved |
| 21/24 | GLM/Decart control after pause | 429 | `rate_limited` | absent | none recorded | `provider_unavailable`; content not reserved |

## Structured-output wire check

Control still sends OpenRouter `response_format` as
`type=json_schema`, `json_schema.name=agent_decision_v2`, `strict=true`, plus
the self-contained schema object; `provider.require_parameters=true`; no
`plugins` / Response Healing. Catalog advertisement is not live qualification.
