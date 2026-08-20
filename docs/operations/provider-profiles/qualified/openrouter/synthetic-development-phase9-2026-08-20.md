# OpenRouter synthetic-development Phase 9 evidence

This Lightning/Nvidia probe is a historical retired candidate. The current
live pin is GPT-OSS/Darkbloom.

| Field | Value |
| --- | --- |
| UTC date | 2026-08-20 |
| Adapter contract | `sessions.openrouter.v1` |
| Qualification scope | `synthetic_development` |
| Pinned model | `nvidia/nemotron-3.5-lightning:free` |
| Provider slug | `nvidia` |
| Expected returned provider identity | `Nvidia` |
| Profile digest | `52b47fe8a81ec93aad637d3d81fee665ee9a8230762ecad3204ad6963ca038ac` |
| Adapter-configuration digest | `77754995939f05366000e0f90022e998cdc85d18b3f675b8d64307595b0361ac` |
| Retention-accepted budget after this run | 9/12 originally; 15/18 after the owner 1.5× raise and retry wave |
| Historical strict-policy budget | 5/18 (consumed unchanged; ceiling raised) |
| Acceptance label | not applied |

This file contains only sanitized operator facts. It does not contain keys, key
fragments, account identifiers, prompts, model text, request or response bodies,
or authorization headers.

## Result

The pinned live matrix is **incomplete**. Visible-content streaming passed the
identity, cache-denial, router-metadata, attempt, and usage checks. Structured
control did not: the pinned free route rejected the strict JSON Schema request
with HTTP 404 / `request_rejected`. That is not authorization to relax schema,
enable fallback, or switch to a paid or different model.

Do not label this run:

```text
qualified_for: synthetic_development
```

## Sanitized request matrix

| Slot | Phase | HTTP | Class | Cache | Usage | Outcome |
| --- | --- | --- | --- | --- | --- | --- |
| 7/12 | control | unclassified | `provider_unavailable` | not captured | none | first reserved control; HTTP class was not captured, so content was not started |
| 8/12 | control | 404 | `request_rejected` | absent | none | fail-closed; not a hard identity/cache/rate-limit stop |
| 9/12 | content | 200 | `ok` | absent | 188 in / 256 out | completed; 1 non-overlapping fragment; 989 UTF-8 bytes of visible text (length only) |
| 12/18 | owner-retry control | 404 | `request_rejected` | absent | none | same structured-control rejection; Lightning runner still reserved content |
| 13/18 | owner-retry content | 200 | `ok` | absent | none recorded | `provider_unavailable`; 0 deltas |

Returned model and selected provider were validated by the adapter before
content completion. Router attempt remains required to be `1`. No
`X-OpenRouter-Cache-Status: HIT` was observed.

## Observed and not observed

Observed:

- operator profile/configuration load and digest match
- persistent retention-accepted budget reservation without reset
- control fail-closed on an unsupported structured-output route
- incremental streaming, terminal metadata, `[DONE]`, completion, and usage
- cache header absent rather than HIT

Not observed in this live run:

- admitted live structured Decision
- timeout, caller cancellation, application retry, cutoff, or reconnect
- Worker plus PostgreSQL durable admission/provenance against the live network
- hosted Participant Text Session chat

## Enablement

Passing or partial live evidence does not enable a runtime, authorize Production
or Staging, or close the OpenAI-compatible endpoint qualification track
(formerly Direct OpenAI Phase B). Hosted Participant chat remains a separate
delivery gap.
