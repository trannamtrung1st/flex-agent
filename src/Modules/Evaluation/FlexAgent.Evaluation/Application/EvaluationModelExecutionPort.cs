using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationModelExecutionOutcomeCategories
{
    public const string Succeeded = "succeeded";
    public const string ProviderTimeout = "provider_timeout";
    public const string ProviderUnavailable = "provider_unavailable";
    public const string SchemaInvalid = "schema_invalid";
    public const string AuthorizationDenied = "authorization_denied";
}

public sealed record EvaluationModelAttemptRequest(
    EvaluationOwnership Ownership,
    Guid RequestId,
    Guid InvocationAttemptId,
    string CriterionId,
    string CriterionVersion,
    string EvaluatorMode,
    string InputSchemaId,
    string OutputSchemaId,
    FrozenModelIdentity ModelIdentity,
    int AttemptOrdinal,
    int MaxResponseUtf8Bytes,
    string? SyntheticScenario = null);

public abstract record EvaluationModelAttemptResult;

public sealed record EvaluationModelAttemptSucceeded(CriterionJudgmentDraft JudgmentDraft)
    : EvaluationModelAttemptResult;

public sealed record EvaluationModelAttemptFailed(string OutcomeCategory) : EvaluationModelAttemptResult;

public interface IEvaluationModelExecutionPort
{
    Task<EvaluationModelAttemptResult> ExecuteAsync(
        EvaluationModelAttemptRequest request,
        CancellationToken cancellationToken);
}

public sealed class FailClosedEvaluationModelExecutionPort : IEvaluationModelExecutionPort
{
    public Task<EvaluationModelAttemptResult> ExecuteAsync(
        EvaluationModelAttemptRequest request,
        CancellationToken cancellationToken)
    {
        _ = request;
        _ = cancellationToken;
        return Task.FromResult<EvaluationModelAttemptResult>(
            new EvaluationModelAttemptFailed(EvaluationModelExecutionOutcomeCategories.AuthorizationDenied));
    }
}

public sealed class SyntheticEvaluationModelExecutionAdapter : IEvaluationModelExecutionPort
{
    public Task<EvaluationModelAttemptResult> ExecuteAsync(
        EvaluationModelAttemptRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult<EvaluationModelAttemptResult>(
                new EvaluationModelAttemptFailed(EvaluationModelExecutionOutcomeCategories.ProviderUnavailable));
        }

        return Task.FromResult<EvaluationModelAttemptResult>(
            request.SyntheticScenario switch
            {
                "timeout" => new EvaluationModelAttemptFailed(
                    EvaluationModelExecutionOutcomeCategories.ProviderTimeout),
                "provider_unavailable" => new EvaluationModelAttemptFailed(
                    EvaluationModelExecutionOutcomeCategories.ProviderUnavailable),
                "schema_invalid" => new EvaluationModelAttemptFailed(
                    EvaluationModelExecutionOutcomeCategories.SchemaInvalid),
                "insufficient" => new EvaluationModelAttemptSucceeded(
                    CreateDraft(request, CriterionStatuses.InsufficientEvidence, "pass", null)),
                "conflict" => new EvaluationModelAttemptSucceeded(
                    CreateDraft(request, CriterionStatuses.Conflict, "fail", null)),
                _ => new EvaluationModelAttemptSucceeded(
                    CreateDraft(
                        request,
                        CriterionStatuses.Satisfied,
                        request.EvaluatorMode == EvaluatorModes.AgentJudgment ? 3 : "pass",
                        request.EvaluatorMode == EvaluatorModes.AgentAssisted
                            ? "Structure verified against the rubric."
                            : "Quality matches the rubric.")),
            });
    }

    private static CriterionJudgmentDraft CreateDraft(
        EvaluationModelAttemptRequest request,
        string status,
        object? score,
        string? provisionalFeedback)
    {
        return new CriterionJudgmentDraft(
            Guid.CreateVersion7(),
            request.RequestId,
            request.CriterionId,
            request.CriterionVersion,
            request.EvaluatorMode,
            status,
            "high",
            ["ambiguous_language"],
            "Synthetic evaluation model response.",
            [Guid.CreateVersion7()],
            score,
            provisionalFeedback,
            request.EvaluatorMode == EvaluatorModes.AgentAssisted
                ? Guid.CreateVersion7()
                : null);
    }
}

public sealed class EvaluationModelExecutionService
{
    public async Task<EvaluationDecision<CriterionJudgmentDraft>> TryExecuteAsync(
        EvaluationProcedureV1 procedure,
        EvaluationModelInvocationContext context,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts,
        FrozenModelIdentity modelIdentity,
        EvaluationModelAttemptRequest attemptRequest,
        IEvaluationModelExecutionPort executionPort,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(modelIdentity);
        ArgumentNullException.ThrowIfNull(attemptRequest);
        ArgumentNullException.ThrowIfNull(executionPort);

        var orchestration = AgentEvaluatorOrchestrationValidator.TryValidateInvocation(
            procedure,
            context,
            verifiedDeterministicFacts);
        if (!orchestration.Succeeded || orchestration.Value is null)
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                orchestration.OutcomeCode,
                orchestration.Field);
        }

        var criterion = orchestration.Value;

        var attempt = attemptRequest with
        {
            EvaluatorMode = criterion.EvaluatorMode,
            InputSchemaId = criterion.AgentIo!.InputSchemaId,
            OutputSchemaId = criterion.AgentIo.OutputSchemaId,
            ModelIdentity = modelIdentity,
        };

        var result = await executionPort.ExecuteAsync(attempt, cancellationToken);
        if (result is EvaluationModelAttemptFailed failed)
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                failed.OutcomeCategory switch
                {
                    EvaluationModelExecutionOutcomeCategories.SchemaInvalid
                        => EvaluationFailureCodes.InvalidJudgment,
                    EvaluationModelExecutionOutcomeCategories.ProviderTimeout
                        => EvaluationFailureCodes.InvalidJudgment,
                    EvaluationModelExecutionOutcomeCategories.AuthorizationDenied
                        => EvaluationFailureCodes.ProcessingDisabled,
                    _ => EvaluationFailureCodes.InvalidJudgment,
                },
                "model_execution");
        }

        if (result is not EvaluationModelAttemptSucceeded succeeded)
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(EvaluationFailureCodes.InvalidJudgment);
        }

        return EvaluationModelResponseValidator.TryValidate(
            procedure,
            succeeded.JudgmentDraft,
            verifiedDeterministicFacts);
    }
}
