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

        var permittedEvidenceDecision = TryValidatePermittedEvidence(context);
        if (!permittedEvidenceDecision.Succeeded)
        {
            return EvaluationDecision<EvaluationModelRequestV1>.Fail(
                permittedEvidenceDecision.OutcomeCode,
                permittedEvidenceDecision.Field);
        }

        IReadOnlyList<EvaluationModelDeterministicFactV1>? deterministicFacts = null;
        if (authorizedCriterion.EvaluatorMode == EvaluatorModes.AgentAssisted)
        {
            var deterministicFactsDecision = TryBuildVerifiedDeterministicFacts(
                context,
                verifiedDeterministicFacts);
            if (!deterministicFactsDecision.Succeeded || deterministicFactsDecision.Value is null)
            {
                return EvaluationDecision<EvaluationModelRequestV1>.Fail(
                    deterministicFactsDecision.OutcomeCode,
                    deterministicFactsDecision.Field);
            }

            deterministicFacts = deterministicFactsDecision.Value;
        }
        else if (context.DeterministicFactProtectedRefs is { Count: > 0 }
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
                deterministicFacts,
                authorizedCriterion.AgentIo.MaxContextUnicodeScalars));
    }

    private static EvaluationDecision<IReadOnlyList<EvaluationModelDeterministicFactV1>> TryBuildVerifiedDeterministicFacts(
        EvaluationModelExecutionContext context,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts)
    {
        if (context.DeterministicInvocationId is null
            || string.IsNullOrWhiteSpace(context.DeterministicInvocationStableId)
            || verifiedDeterministicFacts is null
            || verifiedDeterministicFacts.Count == 0
            || context.DeterministicFactProtectedRefs is null
            || context.DeterministicFactProtectedRefs.Count == 0)
        {
            return EvaluationDecision<IReadOnlyList<EvaluationModelDeterministicFactV1>>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "deterministic_facts");
        }

        if (context.DeterministicFactProtectedRefs.Count != verifiedDeterministicFacts.Count)
        {
            return EvaluationDecision<IReadOnlyList<EvaluationModelDeterministicFactV1>>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "deterministic_facts");
        }

        var facts = new List<EvaluationModelDeterministicFactV1>(verifiedDeterministicFacts.Count);
        foreach (var (sourceId, verifiedFact) in verifiedDeterministicFacts.OrderBy(
                     pair => pair.Key,
                     StringComparer.Ordinal))
        {
            if (!context.DeterministicFactProtectedRefs.TryGetValue(sourceId, out var protectedRef))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationModelDeterministicFactV1>>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "deterministic_facts");
            }

            if (!string.Equals(verifiedFact.SourceId, sourceId, StringComparison.Ordinal)
                || !string.Equals(protectedRef.ContentDigest, verifiedFact.ContentDigest, StringComparison.Ordinal))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationModelDeterministicFactV1>>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "deterministic_facts");
            }

            facts.Add(new EvaluationModelDeterministicFactV1(
                verifiedFact.SourceId,
                verifiedFact.ContentDigest,
                protectedRef));
        }

        foreach (var sourceId in context.DeterministicFactProtectedRefs.Keys)
        {
            if (!verifiedDeterministicFacts.ContainsKey(sourceId))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationModelDeterministicFactV1>>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "deterministic_facts");
            }
        }

        return EvaluationDecision<IReadOnlyList<EvaluationModelDeterministicFactV1>>.Ok(facts);
    }

    private static EvaluationDecision<object?> TryValidatePermittedEvidence(EvaluationModelExecutionContext context)
    {
        if (context.PermittedEvidence.Count == 0
            || context.PermittedEvidenceIdBindings.Count == 0)
        {
            return EvaluationDecision<object?>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "permitted_evidence");
        }

        if (context.PermittedEvidence.Count
            != context.PermittedEvidence
                .Select(item => item.EvidenceId)
                .Distinct(StringComparer.Ordinal)
                .Count())
        {
            return EvaluationDecision<object?>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "permitted_evidence");
        }

        var permittedEvidenceIds = context.PermittedEvidence
            .Select(item => item.EvidenceId)
            .ToHashSet(StringComparer.Ordinal);
        if (permittedEvidenceIds.Count != context.PermittedEvidenceIdBindings.Count)
        {
            return EvaluationDecision<object?>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "permitted_evidence");
        }

        foreach (var evidenceId in permittedEvidenceIds)
        {
            if (!context.PermittedEvidenceIdBindings.TryGetValue(evidenceId, out var evidenceGuid)
                || evidenceGuid == Guid.Empty)
            {
                return EvaluationDecision<object?>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "permitted_evidence");
            }
        }

        foreach (var evidenceId in context.PermittedEvidenceIdBindings.Keys)
        {
            if (!permittedEvidenceIds.Contains(evidenceId))
            {
                return EvaluationDecision<object?>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "permitted_evidence");
            }
        }

        return EvaluationDecision<object?>.Ok(null);
    }
}
