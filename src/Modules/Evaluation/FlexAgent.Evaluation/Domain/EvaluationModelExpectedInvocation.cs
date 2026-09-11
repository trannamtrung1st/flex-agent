using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public sealed record EvaluationModelExpectedInvocation(
    EvaluationProcedureCriterionV1 Criterion,
    Guid EvaluationId,
    Guid? DeterministicInvocationId,
    string? DeterministicInvocationStableId,
    IReadOnlyDictionary<string, Guid> PermittedEvidenceIdBindings);
