using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public static class EvaluationModelRequestComposer
{
    public static EvaluationDecision<EvaluationModelRequestV1> TryCompose(
        EvaluationProcedureCriterionV1 authorizedCriterion,
        EvaluationModelExecutionContext context,
        IReadOnlyList<EvaluationModelPermittedEvidenceV1> authoritativePermittedEvidence,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts,
        IReadOnlyDictionary<string, VerifiedDeterministicOutputMaterial>? storeBackedDeterministicFacts)
    {
        ArgumentNullException.ThrowIfNull(authorizedCriterion);
        ArgumentNullException.ThrowIfNull(context);

        if (authorizedCriterion.AgentIo is null)
        {
            return EvaluationDecision<EvaluationModelRequestV1>.Fail(
                EvaluationFailureCodes.InvalidProcedure,
                "agent_io");
        }

        var permittedEvidenceDecision = TryValidatePermittedEvidence(
            context,
            authoritativePermittedEvidence);
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
                verifiedDeterministicFacts,
                storeBackedDeterministicFacts);
            if (!deterministicFactsDecision.Succeeded || deterministicFactsDecision.Value is null)
            {
                return EvaluationDecision<EvaluationModelRequestV1>.Fail(
                    deterministicFactsDecision.OutcomeCode,
                    deterministicFactsDecision.Field);
            }

            deterministicFacts = deterministicFactsDecision.Value;
        }
        else if (storeBackedDeterministicFacts is { Count: > 0 }
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
                authoritativePermittedEvidence,
                deterministicFacts,
                authorizedCriterion.AgentIo.MaxContextUnicodeScalars));
    }

    private static EvaluationDecision<IReadOnlyList<EvaluationModelDeterministicFactV1>> TryBuildVerifiedDeterministicFacts(
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? verifiedDeterministicFacts,
        IReadOnlyDictionary<string, VerifiedDeterministicOutputMaterial>? storeBackedDeterministicFacts)
    {
        if (verifiedDeterministicFacts is null
            || verifiedDeterministicFacts.Count == 0
            || storeBackedDeterministicFacts is null
            || storeBackedDeterministicFacts.Count == 0
            || storeBackedDeterministicFacts.Count != verifiedDeterministicFacts.Count)
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
            if (!storeBackedDeterministicFacts.TryGetValue(sourceId, out var material))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationModelDeterministicFactV1>>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "deterministic_facts");
            }

            if (!string.Equals(verifiedFact.SourceId, material.Projection.SourceId, StringComparison.Ordinal)
                || !string.Equals(verifiedFact.ContentDigest, material.Projection.ContentDigest, StringComparison.Ordinal)
                || !string.Equals(
                    verifiedFact.ContentDigest,
                    material.ProtectedRef.ContentDigest,
                    StringComparison.Ordinal))
            {
                return EvaluationDecision<IReadOnlyList<EvaluationModelDeterministicFactV1>>.Fail(
                    EvaluationFailureCodes.InvalidJudgment,
                    "deterministic_facts");
            }

            facts.Add(new EvaluationModelDeterministicFactV1(
                material.Projection.SourceId,
                material.Projection.ContentDigest,
                material.ProtectedRef));
        }

        foreach (var sourceId in storeBackedDeterministicFacts.Keys)
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

    private static EvaluationDecision<object?> TryValidatePermittedEvidence(
        EvaluationModelExecutionContext context,
        IReadOnlyList<EvaluationModelPermittedEvidenceV1> authoritativePermittedEvidence)
    {
        if (authoritativePermittedEvidence.Count == 0
            || context.PermittedEvidenceIdBindings.Count == 0)
        {
            return EvaluationDecision<object?>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "permitted_evidence");
        }

        if (authoritativePermittedEvidence.Count
            != authoritativePermittedEvidence
                .Select(item => item.EvidenceId)
                .Distinct(StringComparer.Ordinal)
                .Count())
        {
            return EvaluationDecision<object?>.Fail(
                EvaluationFailureCodes.InvalidJudgment,
                "permitted_evidence");
        }

        var permittedEvidenceIds = authoritativePermittedEvidence
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
