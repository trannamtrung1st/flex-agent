# Browser contract projections

TypeScript mappings for the canonical JSON Schema catalog in `contracts/`.
The catalog manifest (`contracts/catalog.manifest.json`) declares this tree as
`projections.typescript_root`.

## Files

| File | Role |
| --- | --- |
| `v1.ts` | Browser-safe v1 wire types (enrollment, my-work, session command envelopes) |
| `v2.ts` | Browser-safe v2 wire types (timing, accommodations, submission intake) |
| `internal-runtime.v1.ts` | Protected Session runtime types for catalog parity only |
| `internal-runtime.v2.ts` | Protected Session runtime v2 parity only |

## Import rules

- Production `web/src/api/` clients and pages import **browser-safe** types from
  `v1.ts` and `v2.ts` only.
- Do **not** import `internal-runtime.*` from participant-facing SPA modules,
  features, or API clients. Those files exist so TypeScript stays aligned with
  protected JSON Schema and server-side DTOs without leaking shapes into UI.
- Do not duplicate wire types in `api/` or `lib/`. Add or extend mappings here
  when the canonical schema changes, then consume them from typed clients.

C# equivalents live in `src/BuildingBlocks/FlexAgent.Contracts/`.
