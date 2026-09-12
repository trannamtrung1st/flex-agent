using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record EvaluationModelExecutionContext(
    SessionOwnershipRefV1 Ownership,
    EvaluationOwnership OwnershipScope,
    Guid RequestId,
    string RequestStableId,
    Guid InvocationAttemptId,
    string InvocationAttemptStableId,
    Guid EvaluationId,
    FrozenModelIdentity ModelIdentity,
    string InstructionVersion,
    IReadOnlyList<EvaluationModelPermittedEvidenceV1> PermittedEvidence,
    IReadOnlyDictionary<string, Guid> PermittedEvidenceIdBindings,
    IReadOnlyList<VerifiedPermittedEvidenceMaterial> VerifiedPermittedEvidence,
    Guid? DeterministicInvocationId,
    string? DeterministicInvocationStableId,
    string? SyntheticScenario = null);
