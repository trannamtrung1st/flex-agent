# OpenRouter synthetic-development Phase 20 evidence

This Gemma/Nano probe is a historical retired candidate. The current live pin
is GPT-OSS/Darkbloom.

| Field | Value |
| --- | --- |
| UTC date | 2026-08-20 |
| Adapter contract | `sessions.openrouter.v1` |
| Qualification scope | `synthetic_development` |
| Primary model | `google/gemma-4-26b-a4b-it:free` |
| Primary provider slug | `darkbloom` |
| Primary expected identity | `Darkbloom` |
| Primary profile digest | `48a2e696b6d0970ea58d9a5a040ccc4ff25c4e6d089447aa2dbe66c21f5d7ad9` |
| Primary adapter-configuration digest | `e442124b72a4a9d71ec3f5c39f64ce7d3a661de3e9211ef0641bc297ec631e52` |
| Backup model | `nvidia/nemotron-nano-9b-v2:free` |
| Backup provider slug | `nvidia` |
| Backup expected identity | `Nvidia` |
| Backup profile digest | `222f34dcffe90fc728ba02645872714ae7671cab2ee334af3736b295e34fa8fb` |
| Backup adapter-configuration digest | `77754995939f05366000e0f90022e998cdc85d18b3f675b8d64307595b0361ac` |
| Retention-accepted budget after this run | 11/12 originally; 15/18 after the owner 1.5× raise and retry wave |
| Historical strict-policy budget | 5/18 (consumed unchanged; ceiling raised) |
| Historical Lightning pin | preserved; not reused |
| Acceptance label | not applied |

This file contains only sanitized operator facts. It does not contain keys, key
fragments, account identifiers, prompts, model text, request or response bodies,
or authorization headers.

## Result

The Phase 20 live matrix is **incomplete**. Both reserved structured-control
probes reached HTTP 200 with cache absent and adapter-validated model identity,
then failed Flex Agent Decision admission as `malformed_control`. The runner
stopped before reserving participant-visible content in both cases.

Do not label this run:

```text
qualified_for: synthetic_development
```

Catalog advertisement of `structured_outputs` / `response_format` is not live
qualification under the 256-token Decision envelope. Do not relax schema,
enable fallback, or switch to a paid model.

## Sanitized request matrix

| Slot | Phase | HTTP | Class | Cache | Usage | Outcome |
| --- | --- | --- | --- | --- | --- | --- |
| 10/12 | Gemma/Darkbloom control | 200 | `ok` | absent | 2322 in / 256 out | `malformed_control`; content not reserved |
| 11/12 | Nemotron Nano/Nvidia control | 200 | `ok` | absent | none recorded | `malformed_control`; content not reserved |
| 14/18 | Gemma/Darkbloom owner-retry control | 404 | `request_rejected` | absent | none | `provider_unavailable`; content not reserved |
| 15/18 | Nemotron Nano/Nvidia owner-retry control | 200 | `ok` | absent | none recorded | `provider_unavailable`; content not reserved |

Returned model identity matched the exact pinned route on HTTP 200 probes. No
`X-OpenRouter-Cache-Status: HIT` was observed.

## Observed and not observed

Observed:

- distinct Phase 20 gates and runners separate from historical Lightning
- owner-only Gemma and Nano operator files load-verified through real loaders
- control-before-content stop after a non-admitted Decision
- HTTP 200 identity and cache-denial on both control probes
- Gemma control used the full 256-token output bound

Not observed in this live run:

- admitted live structured Decision
- participant-visible streaming on either Phase 20 pair
- timeout, caller cancellation, application retry, cutoff, or reconnect
- Worker plus PostgreSQL durable admission/provenance against the live network
- hosted Participant Text Session chat

## Enablement

Passing or partial live evidence does not enable a runtime, authorize Production
or Staging, or close the OpenAI-compatible endpoint qualification track
(formerly Direct OpenAI Phase B). Hosted Participant chat remains a separate
delivery gap. A further live slot requires a new owner-approved phase and
candidate; do not reuse Gemma or Nano control requests.
