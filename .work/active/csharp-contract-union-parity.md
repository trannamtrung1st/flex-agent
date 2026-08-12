---
id: csharp-contract-union-parity
status: completed
created: 2026-08-12
updated: 2026-08-12
---

# Goal

Close remaining canonical contract parity gaps before Sessions implementation:

1. C# discriminated-union parity for runtime session contracts (P1)
2. Execution-outcome attempt-reference cardinality in JSON Schema (P2)
3. Negative discriminator / provenance tests

# Governing sources

- External review of `28e203601fa6e96fb004cae305ab3fca6361ae78`
- ADR-012 invocation / execution attempt model
- `contracts/schemas/v1/session/*.schema.json`
- `web/src/contracts/internal-runtime.v1.ts`

# Plan

- [x] Tighten execution-outcome schema attempt provenance
- [x] Finish C# discriminated unions and wire enums
- [x] Align TypeScript outcome attempt provenance
- [x] Add invalid fixtures and negative parity tests
- [x] Run contract and architecture test suites (219/219 dotnet)

# Verification

- Contract catalog fixture tests (valid + invalid)
- New C# discriminator parity tests
- Full `dotnet test` for contract projects
