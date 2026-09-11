using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public interface IEvaluationProviderArtifactStore
{
    Task<EvaluationDecision<Guid>> TryAppendAsync(
        ProviderArtifactAppendCommand command,
        CancellationToken cancellationToken);
}

public static class EvaluationProviderArtifactPersistence
{
    public static EvaluationDecision<ProviderArtifactAppendCommand> TryBuildAppendCommand(
        EvaluationModelExecutionContext context,
        EvaluationProcedureCriterionV1 authorizedCriterion,
        EvaluationModelRequestV1 request,
        EvaluationModelAttemptResult result)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(authorizedCriterion);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(result);

        var requestContentDigest = ProviderArtifactIdentity.ComputeRequestContentDigest(
            context.RequestId,
            context.InvocationAttemptId,
            authorizedCriterion.CriterionId,
            authorizedCriterion.CriterionVersion,
            context.ModelIdentity,
            context.InstructionVersion,
            request.PermittedEvidence);
        var protectedRequestRef = ProviderArtifactProvenance.ProtectedRequestRef(requestContentDigest);
        var providerArtifactId = ProviderArtifactIdentity.ComputeArtifactId(
            context.RequestId,
            context.InvocationAttemptId,
            authorizedCriterion.CriterionId,
            authorizedCriterion.CriterionVersion,
            requestContentDigest);

        string? protectedResponseRef = null;
        string outcomeCategory;
        string? failureCategory = null;

        switch (result)
        {
            case EvaluationModelAttemptSucceeded succeeded:
                outcomeCategory = EvaluationModelExecutionOutcomeCategories.Succeeded;
                protectedResponseRef = ProviderArtifactProvenance.ProtectedResponseRef(
                    succeeded.Response.ResponseRef.ContentDigest);
                break;
            case EvaluationModelAttemptFailed failed:
                outcomeCategory = failed.OutcomeCategory;
                failureCategory = failed.OutcomeCategory;
                break;
            default:
                return EvaluationDecision<ProviderArtifactAppendCommand>.Fail(
                    EvaluationFailureCodes.InvalidField,
                    "model_execution");
        }

        return EvaluationDecision<ProviderArtifactAppendCommand>.Ok(
            new ProviderArtifactAppendCommand(
                context.OwnershipScope,
                context.RequestId,
                context.InvocationAttemptId,
                providerArtifactId,
                authorizedCriterion.CriterionId,
                authorizedCriterion.CriterionVersion,
                context.ModelIdentity,
                protectedRequestRef,
                protectedResponseRef,
                ProviderArtifactOutcomes.FromExecutionOutcomeCategory(outcomeCategory),
                failureCategory));
    }

    public static async Task<EvaluationDecision<Guid>> TryPersistAsync(
        EvaluationModelExecutionContext context,
        EvaluationProcedureCriterionV1 authorizedCriterion,
        EvaluationModelRequestV1 request,
        EvaluationModelAttemptResult result,
        IEvaluationProviderArtifactStore store,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        var commandDecision = TryBuildAppendCommand(context, authorizedCriterion, request, result);
        if (!commandDecision.Succeeded || commandDecision.Value is null)
        {
            return EvaluationDecision<Guid>.Fail(commandDecision.OutcomeCode, commandDecision.Field);
        }

        return await store.TryAppendAsync(commandDecision.Value, cancellationToken);
    }
}
