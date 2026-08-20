# OpenRouter synthetic-development Phase 27 evidence

This file records the 2026-08-21 owner-authorized live rerun. It is **not** a
passing matrix.

| Field | Value |
| --- | --- |
| UTC date | 2026-08-20T17:28:22Z |
| Adapter contract | `sessions.openrouter.v1` |
| Request-policy version | `openrouter.request-policy.v2` |
| Qualification scope | `synthetic_development` |
| Model | `openai/gpt-oss-20b:free` |
| Provider identity | `Darkbloom` |
| Profile digest | `9b4c641463dd33ab3c8900f00089eebb08e8abb9d1700592ba23292d0e7ce611` |
| Adapter-configuration digest | `a291c5a83dade637fe78fd68b0cb964ecbb704b48723ecad348eaa8a53810e7a` |
| Source revision | `4c3e9d4b96353156e4669f748b41636fa33ce6c5` |
| Control slot | 9 |
| Content slot | not reserved |
| Phase 21 budget after this run | 9/10 |
| Historical strict-policy budget | 5/24 (unchanged) |
| Retention-accepted budget | 21/24 (unchanged) |
| Acceptance label | not applied |

This file contains only sanitized operator facts. It does not contain keys, key
fragments, account identifiers, prompts, model text, request or response bodies,
reasoning traces, or authorization headers.

## Result

The owner-authorized GPT-OSS/Darkbloom control request **failed closed** before
content reservation.

| Observation | Value |
| --- | --- |
| Control HTTP | 429 |
| Control class | `rate_limited` |
| Control cache | absent |
| Control finish reason | none |
| Control tokens | none |
| Qualification outcome | `denied` |
| Denial | `control_not_admitted` |

The live runner wrote this machine record to the operator evidence path. The
`content_http` / `content_class` / `content_cache` fields in that JSON repeat
the control observer because content was not reserved; they are not a second
provider call.

```json
{"schema_version":"openrouter.sanitized-qualification.v1","request_policy_version":"openrouter.request-policy.v2","adapter_contract_version":"sessions.openrouter.v1","qualification_scope":"synthetic_development","model":"openai/gpt-oss-20b:free","provider_identity":"Darkbloom","profile_digest":"9b4c641463dd33ab3c8900f00089eebb08e8abb9d1700592ba23292d0e7ce611","adapter_configuration_digest":"a291c5a83dade637fe78fd68b0cb964ecbb704b48723ecad348eaa8a53810e7a","control_http":429,"control_class":"rate_limited","control_cache":"absent","content_http":429,"content_class":"rate_limited","content_cache":"absent","qualification_outcome":"denied","denial_reason":"control_not_admitted","recorded_at_utc":"2026-08-20T17:28:22Z","control_slot":9,"source_revision":"4c3e9d4b96353156e4669f748b41636fa33ce6c5"}
```

Do not label this run:

```text
qualified_for: synthetic_development
```

Do not immediately retry this control request. One remaining Phase 21 slot
(10/10) is not enough for a later control-plus-content pair. A later retry
needed a new owner-approved ceiling of at least 12 and a cooldown after the
429. The later retry is
[synthetic-development-phase28-2026-08-21.md](synthetic-development-phase28-2026-08-21.md).

## Enablement

This failed-closed adapter-harness evidence does not turn on a runtime,
authorize Production or Staging, or close hosted Participant chat.
