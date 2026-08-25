---
id: backend-maintainability-refactor
status: completed
created: 2026-08-26
updated: 2026-08-26
---

# Goal

Improve backend maintainability via typed options, endpoint decomposition, transport validators, and shared HTTP infrastructure without behavior changes or new libraries.

# Scope delivered

## Configuration (§1)

- `HumanAuthenticationHostOptions` via `AddOptions` + `HumanAuthenticationHostOptionsBinding` (includes `SecretDirectory`)
- `ArtifactStorage` / `S3ArtifactStoreOptions` with `ValidateOnStart` for production credentials
- `SessionEventSubscriptionOptions` + `SessionEventTestIdentityOptions`
- `EnrollmentCursorSigningOptions` for cursor key rotation metadata

## Transport validation (§2)

- Submission: `BeginIntake`, `CompleteIntakeItem`, `IntakeRevision` validators
- Enrollment: `EnrollmentAssign`, `EnrollmentLifecycle` validators
- Accommodation: `Grant`, `Decide`, `Revoke` validators
- Assessment: `AssessmentActivate` (revision shape only), `AssessmentReconcile` query validator

## Mappings (§3)

- `SubmissionResponseMapper`
- `EnrollmentTimingResponseMapper`

## Endpoint decomposition (§4)

- `Submission/` feature folder (composition, queries, mutations, registration)
- `Assessment/Composition/AssessmentServiceCollectionExtensions.cs`

## HTTP consolidation (§10)

- Assessment mutations delegate to `EnrollmentEndpointExtensions.ValidateMutationAsync` (shared CSRF path)

# Verification

| Check | Status | Evidence |
| --- | --- | --- |
| `dotnet build` | passed | 0 warnings, 0 errors |
| Runtime tests | passed | 265/265 |
| Architecture tests | passed | 41/41 |
| Submissions unit tests | passed | 114/114 |

# Intentionally unchanged

- Large persistence files (`PostgresSessionRuntimeRepository`, etc.)
- Worker `WorkloadIdentity` manual configuration (host project, separate scope)
- HumanAuthentication `ValidateOnStart` when enabled-but-incomplete (preserves runtime 503 fail-closed)
- Assessment endpoint handlers beyond composition split (~770 lines remain)
- Activate idempotency validation remains in domain (preserves outcome-shaped 400 responses)
