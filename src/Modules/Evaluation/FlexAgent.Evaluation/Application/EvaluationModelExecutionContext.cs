using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record EvaluationModelExecutionContext(
    SessionOwnershipRefV1 Ownership,
    string RequestStableId,
    string InvocationAttemptStableId,
    Guid EvaluationId,
    FrozenModelIdentity ModelIdentity,
    string InstructionVersion,
    IReadOnlyList<EvaluationModelPermittedEvidenceV1> PermittedEvidence,
    IReadOnlyDictionary<string, Guid> PermittedEvidenceIdBindings,
    IReadOnlyDictionary<string, ProtectedPayloadRefV1>? DeterministicFactProtectedRefs,
    Guid? DeterministicInvocationId,
    string? DeterministicInvocationStableId,
    string? SyntheticScenario = null);
