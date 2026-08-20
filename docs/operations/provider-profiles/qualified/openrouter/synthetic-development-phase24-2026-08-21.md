# OpenRouter synthetic-development Phase 24 evidence

This file records the 2026-08-21 contract correction. It is **not** a new live
matrix pass.

| Field | Value |
| --- | --- |
| UTC date | 2026-08-21 |
| Adapter contract | `sessions.openrouter.v1` |
| Request-policy version | `openrouter.request-policy.v2` |
| Qualification scope | `synthetic_development` |
| Current pin | `openai/gpt-oss-20b:free` / `darkbloom` / `Darkbloom` |
| GPT-OSS adapter digest | `a291c5a83dade637fe78fd68b0cb964ecbb704b48723ecad348eaa8a53810e7a` |
| GPT-OSS profile digest | `9b4c641463dd33ab3c8900f00089eebb08e8abb9d1700592ba23292d0e7ce611` |
| Default request policy | `max_tokens:256`; 30/60-second timeouts; no `reasoning` object |
| GPT-OSS request policy | `max_tokens:4096`; `reasoning.effort:low`; `reasoning.exclude:true`; 120-second timeouts |
| Visible-content acceptance | `finish_reason` exactly `stop` and content output tokens below 256 |
| Phase 21 budget | 8/8 (unchanged; no live spend) |
| Historical strict-policy budget | 5/24 (unchanged) |
| Retention-accepted budget | 21/24 (unchanged) |
| Acceptance label | not re-applied |

This file contains only sanitized operator facts. It does not contain keys, key
fragments, account identifiers, prompts, model text, request or response bodies,
reasoning traces, or authorization headers.

## Result

Deterministic contracts now:

- parse `finish_reason` as a required terminal fact;
- reject control when the finish reason is not `stop`;
- deny `TryQualify` for any non-`stop` finish reason, including `length`
  below the request ceiling;
- isolate the 4,096-token ceiling to the GPT-OSS policy;
- bind `openrouter.request-policy.v2` into the adapter-configuration digest;
- generate the control example with the current invocation ID;
- emit an `openrouter.sanitized-qualification.v1` JSON record from the live
  runner when an owner-authorized run occurs.

No live Phase 21 retry was authorized. Do not invent HTTP status, cache class,
token counts, or finish reasons for the historical 2026-08-20 slots 7 and 8.

Do not label this corrective slice:

```text
qualified_for: synthetic_development
```

Operator GPT-OSS pin files outside Git must be regenerated against the new
digests before any future live run.

## Closing condition

Close the OpenRouter adapter qualification slice only after an owner-approved
new budget ceiling produces a machine-written sanitized record with
`finish_reason: stop` on both phases and content tokens below 256.
