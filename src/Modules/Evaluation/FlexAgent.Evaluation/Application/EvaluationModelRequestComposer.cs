using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationModelRequestComposer
{
    public static EvaluationDecision<EvaluationModelRequestV1> TryCompose(
        EvaluationProcedureCriterionV1 authorizedCriterion,
        EvaluationModelExecutionContext context,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts)
    {
        ArgumentNullException.ThrowIfNull(authorizedCriterion);
        ArgumentNullException.ThrowIfNull(context);

        if (authorizedCriterion.AgentIo is null)
        {
            return EvaluationDecision<EvaluationModelRequestV1>.Fail(
                EvaluationFailureCodes.InvalidProcedure,
                "agent_io");
        }

        if (context.PermittedEvidence.Count == 0
            || context.PermittedEvidenceIdBindings.Count == 0)
        {
            return EvaluationDecision<EvaluationModelRequestV1>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "permitted_evidence");
        }

        if (authorizedCriterion.EvaluatorMode == EvaluatorModes.AgentAssisted)
        {
            if (context.DeterministicFacts is null
                || context.DeterministicFacts.Count == 0
                || context.DeterministicInvocationId is null
                || string.IsNullOrWhiteSpace(context.DeterministicInvocationStableId)
                || verifiedDeterministicFacts is null
                || verifiedDeterministicFacts.Count == 0)
            {
                return EvaluationDecision<EvaluationModelRequestV1>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "deterministic_facts");
            }

            foreach (var (sourceId, verifiedFact) in verifiedDeterministicFacts)
            {
                var boundFact = context.DeterministicFacts.FirstOrDefault(item =>
                    string.Equals(item.SourceId, sourceId, StringComparison.Ordinal));
                if (boundFact is null
                    || !string.Equals(boundFact.ContentDigest, verifiedFact.ContentDigest, StringComparison.Ordinal))
                {
                    return EvaluationDecision<EvaluationModelRequestV1>.Fail(
                        EvaluationFailureCodes.InvalidJudgment,
                        "deterministic_facts");
                }
            }
        }
        else if (context.DeterministicFacts is { Count: > 0 }
                 || context.DeterministicInvocationId is not null
                 || !string.IsNullOrWhiteSpace(context.DeterministicInvocationStableId))
        {
            return EvaluationDecision<EvaluationModelRequestV1>.Fail(
                EvaluationFailureCodes.DeterministicConflict,
                "deterministic_facts");
        }

        return EvaluationDecision<EvaluationModelRequestV1>.Ok(
            new EvaluationModelRequestV1(
                "v1",
                context.RequestStableId,
                context.InvocationAttemptStableId,
                authorizedCriterion.CriterionId,
                authorizedCriterion.CriterionVersion,
                authorizedCriterion.EvaluatorMode,
                context.Ownership,
                authorizedCriterion.AgentIo.InputSchemaId,
                authorizedCriterion.AgentIo.OutputSchemaId,
                context.InstructionVersion,
                context.ModelIdentity.ProfileId,
                context.ModelIdentity.ProfileVersion,
                context.ModelIdentity.ProfileDigest,
                context.ModelIdentity.CredentialBindingReference,
                context.PermittedEvidence,
                context.DeterministicFacts,
                authorizedCriterion.AgentIo.MaxContextUnicodeScalars));
    }
}
