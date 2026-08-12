---
id: csharp-contract-union-parity
status: completed
created: 2026-08-12
updated: 2026-08-12
---

# Goal

Close remaining canonical contract parity gaps before Sessions implementation.

# Follow-up (review of e8e4c2c)

- [x] Interface-typed JSON serialization preserves branch fields via union converters
- [x] Shared `SessionRuntimeContractJson.WireSerializerOptions`
- [x] Bounded enums: `NoActionReasonCategoryV1`, `RejectionReasonCategoryV1`, `SuppressionReasonCategoryV1`, `TimerValidationOutcomeV1`
- [x] Fix false-positive negative test (`attempt_id` typo)
- [x] Interface serialization regression tests for all runtime unions

# Verification

- Contract tests: 118/118
- Full dotnet: 220/220
