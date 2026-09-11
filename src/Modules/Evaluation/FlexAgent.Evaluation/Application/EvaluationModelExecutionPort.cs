using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
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

public abstract record EvaluationModelAttemptResult;

public sealed record EvaluationModelAttemptSucceeded(EvaluationModelResponseV1 Response)
    : EvaluationModelAttemptResult;

public sealed record EvaluationModelAttemptFailed(string OutcomeCategory) : EvaluationModelAttemptResult;

public interface IEvaluationModelExecutionPort
{
    Task<EvaluationModelAttemptResult> ExecuteAsync(
        EvaluationModelRequestV1 request,
        EvaluationModelExecutionContext context,
        CancellationToken cancellationToken);
}

public sealed class FailClosedEvaluationModelExecutionPort : IEvaluationModelExecutionPort
{
    public Task<EvaluationModelAttemptResult> ExecuteAsync(
        EvaluationModelRequestV1 request,
        EvaluationModelExecutionContext context,
        CancellationToken cancellationToken)
    {
        _ = request;
        _ = context;
        _ = cancellationToken;
        return Task.FromResult<EvaluationModelAttemptResult>(
            new EvaluationModelAttemptFailed(EvaluationModelExecutionOutcomeCategories.AuthorizationDenied));
    }
}

public sealed class SyntheticEvaluationModelExecutionAdapter : IEvaluationModelExecutionPort
{
    public Task<EvaluationModelAttemptResult> ExecuteAsync(
        EvaluationModelRequestV1 request,
        EvaluationModelExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult<EvaluationModelAttemptResult>(
                new EvaluationModelAttemptFailed(EvaluationModelExecutionOutcomeCategories.ProviderUnavailable));
        }

        return Task.FromResult<EvaluationModelAttemptResult>(
            context.SyntheticScenario switch
            {
                "timeout" => new EvaluationModelAttemptFailed(
                    EvaluationModelExecutionOutcomeCategories.ProviderTimeout),
                "provider_unavailable" => new EvaluationModelAttemptFailed(
                    EvaluationModelExecutionOutcomeCategories.ProviderUnavailable),
                "schema_invalid" => new EvaluationModelAttemptFailed(
                    EvaluationModelExecutionOutcomeCategories.SchemaInvalid),
                "wrong_criterion" => new EvaluationModelAttemptSucceeded(
                    CreateResponse(
                        request,
                        context,
                        "crit.judgment.quality",
                        "crit.judgment.quality.v1",
                        EvaluatorModes.AgentJudgment,
                        request.OutputSchemaId,
                        CriterionStatuses.Satisfied,
                        3,
                        null)),
                "insufficient" => new EvaluationModelAttemptSucceeded(
                    CreateResponse(
                        request,
                        context,
                        request.CriterionId,
                        request.CriterionVersion,
                        request.EvaluatorMode,
                        request.OutputSchemaId,
                        CriterionStatuses.InsufficientEvidence,
                        "pass",
                        null)),
                "conflict" => new EvaluationModelAttemptSucceeded(
                    CreateResponse(
                        request,
                        context,
                        request.CriterionId,
                        request.CriterionVersion,
                        request.EvaluatorMode,
                        request.OutputSchemaId,
                        CriterionStatuses.Conflict,
                        "fail",
                        null)),
                _ => new EvaluationModelAttemptSucceeded(
                    CreateResponse(
                        request,
                        context,
                        request.CriterionId,
                        request.CriterionVersion,
                        request.EvaluatorMode,
                        request.OutputSchemaId,
                        CriterionStatuses.Satisfied,
                        request.EvaluatorMode == EvaluatorModes.AgentJudgment ? 3 : "pass",
                        request.EvaluatorMode == EvaluatorModes.AgentAssisted
                            ? "Structure verified against the rubric."
                            : "Quality matches the rubric.")),
            });
    }

    private static EvaluationModelResponseV1 CreateResponse(
        EvaluationModelRequestV1 request,
        EvaluationModelExecutionContext context,
        string criterionId,
        string criterionVersion,
        string evaluatorMode,
        string outputSchemaId,
        string status,
        object? score,
        string? provisionalFeedback)
    {
        return new EvaluationModelResponseV1(
            "v1",
            outputSchemaId,
            criterionId,
            criterionVersion,
            evaluatorMode,
            status,
            "high",
            ["ambiguous_language"],
            "Synthetic evaluation model response.",
            request.PermittedEvidence.Select(item => item.EvidenceId).ToArray(),
            new ProtectedPayloadRefV1("prot.eval.res.synthetic", new string('d', 64)),
            score,
            provisionalFeedback,
            context.DeterministicInvocationStableId);
    }
}

public sealed class EvaluationModelExecutionService
{
    public async Task<EvaluationDecision<CriterionJudgmentDraft>> TryExecuteAsync(
        EvaluationProcedureV1 procedure,
        EvaluationModelInvocationContext context,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts,
        EvaluationModelExecutionContext executionContext,
        IEvaluationModelExecutionPort executionPort,
        IEvaluationRequestAuthorityStore requestAuthorityStore,
        IProtectedDeterministicOutputStore? deterministicOutputStore,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(executionPort);
        ArgumentNullException.ThrowIfNull(requestAuthorityStore);

        if (executionContext.EvaluationId == Guid.Empty)
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "evaluation_id");
        }

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

        var authorizedCriterion = orchestration.Value;

        var boundContext = EvaluationModelExecutionAuthorityVerifier.TryValidateBoundContext(
            executionContext,
            authorizedCriterion.EvaluatorMode);
        if (!boundContext.Succeeded)
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                boundContext.OutcomeCode,
                boundContext.Field);
        }

        var reloadedAuthority = await EvaluationModelExecutionAuthorityVerifier.TryReloadAuthorityAsync(
            executionContext,
            requestAuthorityStore,
            cancellationToken);
        if (!reloadedAuthority.Succeeded)
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                reloadedAuthority.OutcomeCode,
                reloadedAuthority.Field);
        }

        IReadOnlyDictionary<string, VerifiedDeterministicOutputMaterial>? storeBackedDeterministicFacts = null;
        if (authorizedCriterion.EvaluatorMode == EvaluatorModes.AgentAssisted)
        {
            if (deterministicOutputStore is null
                || executionContext.DeterministicInvocationId is null)
            {
                return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "deterministic_facts");
            }

            var authority = await EvaluationModelDeterministicFactAuthorityLoader.TryLoadAsync(
                executionContext.OwnershipScope,
                executionContext.RequestId,
                executionContext.DeterministicInvocationId.Value,
                authorizedCriterion.CriterionId,
                authorizedCriterion.CriterionVersion,
                verifiedDeterministicFacts!,
                deterministicOutputStore,
                cancellationToken);
            if (!authority.Succeeded || authority.Value is null)
            {
                return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                    authority.OutcomeCode,
                    authority.Field);
            }

            storeBackedDeterministicFacts = authority.Value;
        }

        var requestDecision = EvaluationModelRequestComposer.TryCompose(
            authorizedCriterion,
            executionContext,
            verifiedDeterministicFacts,
            storeBackedDeterministicFacts);
        if (!requestDecision.Succeeded || requestDecision.Value is null)
        {
            return EvaluationDecision<CriterionJudgmentDraft>.Fail(
                requestDecision.OutcomeCode,
                requestDecision.Field);
        }

        var result = await executionPort.ExecuteAsync(
            requestDecision.Value,
            executionContext,
            cancellationToken);
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

        var expected = new EvaluationModelExpectedInvocation(
            authorizedCriterion,
            executionContext.EvaluationId,
            executionContext.DeterministicInvocationId,
            executionContext.DeterministicInvocationStableId,
            requestDecision.Value.PermittedEvidence
                .Select(item => item.EvidenceId)
                .ToHashSet(StringComparer.Ordinal),
            executionContext.PermittedEvidenceIdBindings);

        return EvaluationModelResponseValidator.TryValidate(
            procedure,
            expected,
            succeeded.Response,
            verifiedDeterministicFacts);
    }
}
