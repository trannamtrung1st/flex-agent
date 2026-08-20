# OpenRouter synthetic-development Phase 28 evidence

This file records the 2026-08-21 owner-authorized live retry after the Phase 27
429. It is the passing matrix under the Phase 24/25 predicate.

| Field | Value |
| --- | --- |
| UTC date | 2026-08-20T17:34:04Z |
| Adapter contract | `sessions.openrouter.v1` |
| Request-policy version | `openrouter.request-policy.v2` |
| Qualification scope | `synthetic_development` |
| Model | `openai/gpt-oss-20b:free` |
| Provider identity | `Darkbloom` |
| Profile digest | `9b4c641463dd33ab3c8900f00089eebb08e8abb9d1700592ba23292d0e7ce611` |
| Adapter-configuration digest | `a291c5a83dade637fe78fd68b0cb964ecbb704b48723ecad348eaa8a53810e7a` |
| Source revision | `4c3e9d4b96353156e4669f748b41636fa33ce6c5` |
| Control slot | 10 |
| Content slot | 11 |
| Phase 21 budget after this run | 11/12 |
| Historical strict-policy budget | 5/24 (unchanged) |
| Retention-accepted budget | 21/24 (unchanged) |
| Acceptance label | `qualified_for: synthetic_development` |

This file contains only sanitized operator facts. It does not contain keys, key
fragments, account identifiers, prompts, model text, request or response bodies,
reasoning traces, or authorization headers.

## Result

The owner-authorized GPT-OSS/Darkbloom matrix **passed**. Control reserved at
slot 10/12, content reserved only after an admitted Decision at slot 11/12,
and `TryQualify` succeeded. Both phases reported `finish_reason` exactly
`stop`. Content output tokens were 20, which is below the 256-token acceptance
bound.

```text
qualified_for: synthetic_development
```

This label applies only to the pinned synthetic-development GPT-OSS/Darkbloom
route behind the Sessions OpenRouter adapter harness. It does not enable
Production or Staging, authorize real or private data, or close hosted
Participant chat.

The live runner wrote this machine record to the operator evidence path:

```json
{"schema_version":"openrouter.sanitized-qualification.v1","request_policy_version":"openrouter.request-policy.v2","adapter_contract_version":"sessions.openrouter.v1","qualification_scope":"synthetic_development","model":"openai/gpt-oss-20b:free","provider_identity":"Darkbloom","profile_digest":"9b4c641463dd33ab3c8900f00089eebb08e8abb9d1700592ba23292d0e7ce611","adapter_configuration_digest":"a291c5a83dade637fe78fd68b0cb964ecbb704b48723ecad348eaa8a53810e7a","control_http":200,"control_class":"ok","control_cache":"absent","control_finish_reason":"stop","control_tokens_in":303,"control_tokens_out":96,"content_http":200,"content_class":"ok","content_cache":"absent","content_finish_reason":"stop","content_tokens_in":186,"content_tokens_out":20,"qualification_outcome":"qualified_for=synthetic_development","recorded_at_utc":"2026-08-20T17:34:04Z","control_slot":10,"content_slot":11,"source_revision":"4c3e9d4b96353156e4669f748b41636fa33ce6c5"}
```

## Observed and not observed

Observed:

- control HTTP 200, class `ok`, cache absent, finish reason `stop`, usage 303/96
- content HTTP 200, class `ok`, cache absent, finish reason `stop`, usage 186/20
- matching GPT-OSS profile and adapter-configuration digests
- historical 5/24 and retention-accepted 21/24 unchanged

Not observed in this live run:

- Worker plus PostgreSQL durable admission/provenance against the live network
- hosted Participant Text Session chat
- Production or Staging enablement

## Enablement

This passing adapter-harness evidence does not turn on a runtime, authorize
Production or Staging, or close the OpenAI-compatible endpoint qualification
track (formerly Direct OpenAI Phase B). Hosted Participant chat remains a
separate delivery gap.
