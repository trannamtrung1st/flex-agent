using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public sealed record EvidenceItem(
    Guid EvidenceId,
    string SourceType,
    ExactSourceIdentity Source,
    EvaluationOwnership Ownership,
    Guid EvaluationId,
    string Precision)
{
    public static EvaluationDecision<EvidenceItem> TryCreate(
        Guid evidenceId,
        string sourceType,
        ExactSourceIdentity source,
        EvaluationOwnership ownership,
        Guid evaluationId,
        string precision)
    {
        if (evidenceId == Guid.Empty
            || evaluationId == Guid.Empty
            || precision is not ("exact_range" or "whole_item_fallback")
            || sourceType is not (
                "submission.direct_text"
                or "submission.text_attachment"
                or "session.transcript_item"
                or "session.work_trace"
                or "configuration.fact"
                or "manifest.fact"
                or "deterministic.fact"))
        {
            return EvaluationDecision<EvidenceItem>.Fail(EvaluationFailureCodes.InvalidField);
        }

        return EvaluationDecision<EvidenceItem>.Ok(
            new EvidenceItem(evidenceId, sourceType, source, ownership, evaluationId, precision));
    }
}

public sealed record EvidenceSet(
    Guid EvidenceSetId,
    Guid EvaluationId,
    IReadOnlyList<Guid> EvidenceIds,
    string Digest)
{
    public static EvaluationDecision<EvidenceSet> TryCreate(
        Guid evidenceSetId,
        Guid evaluationId,
        EvaluationOwnership ownership,
        IReadOnlyList<EvidenceItem> items,
        string digest)
    {
        if (evidenceSetId == Guid.Empty
            || evaluationId == Guid.Empty
            || items.Count is < 1 or > 64
            || !EvaluationIdentity.IsSha256Hex(digest)
            || items.Any(item => item.EvaluationId != evaluationId))
        {
            return EvaluationDecision<EvidenceSet>.Fail(EvaluationFailureCodes.InvalidField);
        }

        if (items.Any(item => item.Ownership != ownership))
        {
            return EvaluationDecision<EvidenceSet>.Fail(EvaluationFailureCodes.IncompleteOwnership);
        }

        if (items.Select(item => item.EvidenceId).Distinct().Count() != items.Count)
        {
            return EvaluationDecision<EvidenceSet>.Fail(EvaluationFailureCodes.DuplicateIdentity);
        }

        return EvaluationDecision<EvidenceSet>.Ok(
            new EvidenceSet(
                evidenceSetId,
                evaluationId,
                items.Select(item => item.EvidenceId).ToArray(),
                digest));
    }
}

public sealed record CompletedEvaluation(
    Guid EvaluationId,
    Guid RequestId,
    EvaluationOwnership Ownership,
    ExactSourceIdentity ProcedureRef,
    FrozenInputIdentity FrozenInput,
    Guid EvidenceSetId,
    string EvidenceSetDigest,
    IReadOnlyList<string> CriterionIds,
    string AggregateStatus,
    DateTimeOffset CompletedAtUtc,
    string CreationServiceId,
    Guid? PredecessorEvaluationId)
{
    public static EvaluationDecision<CompletedEvaluation> TryCreate(
        Guid evaluationId,
        EvaluationRequest request,
        EvaluationProcedureV1 procedure,
        EvidenceSet evidenceSet,
        IReadOnlyList<EvidenceItem> evidenceItems,
        IReadOnlyList<CriterionJudgment> judgments,
        DateTimeOffset completedAtUtc,
        string creationServiceId)
    {
        var requiredIds = procedure.Criteria.Select(item => item.CriterionId).ToArray();
        if (evaluationId == Guid.Empty
            || request.State != EvaluationRequestStates.Completing
            || evidenceSet.EvaluationId != evaluationId
            || evidenceItems.Count != evidenceSet.EvidenceIds.Count
            || evidenceItems.Any(item => item.EvaluationId != evaluationId
                || !evidenceSet.EvidenceIds.Contains(item.EvidenceId))
            || judgments.Count != requiredIds.Length
            || !EvaluationIdentity.IsUtc(completedAtUtc)
            || !EvaluationIdentity.IsStableId(creationServiceId)
            || judgments.Any(item => item.EvaluationId != evaluationId))
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(EvaluationFailureCodes.IncompleteCriteria);
        }

        if (evidenceItems.Any(item => item.Ownership != request.FrozenInput.Ownership))
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(EvaluationFailureCodes.IncompleteOwnership);
        }

        if (judgments.Select(item => item.CriterionId).Distinct(StringComparer.Ordinal).Count() != judgments.Count)
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(EvaluationFailureCodes.DuplicateIdentity);
        }

        if (requiredIds.Any(id => judgments.All(item => !string.Equals(item.CriterionId, id, StringComparison.Ordinal))))
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(EvaluationFailureCodes.IncompleteCriteria);
        }

        var itemsById = evidenceItems.ToDictionary(item => item.EvidenceId);
        var cited = judgments.SelectMany(item => item.EvidenceIds).ToHashSet();
        if (judgments.SelectMany(item => item.EvidenceIds).Any(id => !itemsById.ContainsKey(id))
            || evidenceSet.EvidenceIds.Any(id => !cited.Contains(id)))
        {
            return EvaluationDecision<CompletedEvaluation>.Fail(EvaluationFailureCodes.CitationIntegrity);
        }

        foreach (var judgment in judgments)
        {
            var criterion = procedure.Criteria.First(item =>
                string.Equals(item.CriterionId, judgment.CriterionId, StringComparison.Ordinal));
            if (judgment.EvidenceIds.Any(id =>
                !criterion.EvidenceRequirements.PermittedSourceTypes.Contains(
                    itemsById[id].SourceType,
                    StringComparer.Ordinal)))
            {
                return EvaluationDecision<CompletedEvaluation>.Fail(EvaluationFailureCodes.CitationIntegrity);
            }
        }

        return EvaluationDecision<CompletedEvaluation>.Ok(
            new CompletedEvaluation(
                evaluationId,
                request.RequestId,
                request.FrozenInput.Ownership,
                request.FrozenInput.Rubric,
                request.FrozenInput,
                evidenceSet.EvidenceSetId,
                evidenceSet.Digest,
                requiredIds,
                EvaluationAggregator.Aggregate(procedure, judgments),
                completedAtUtc,
                creationServiceId,
                request.PredecessorEvaluationId));
    }
}

public static class EvaluationAggregator
{
    public static string Aggregate(EvaluationProcedureV1 procedure, IReadOnlyList<CriterionJudgment> judgments)
    {
        var considered = judgments.Where(item =>
            item.Status != CriterionStatuses.NotApplicable
            || procedure.NotApplicableBehavior != "exclude_from_aggregation").ToArray();

        if (considered.Any(item => item.Status == CriterionStatuses.Conflict))
        {
            return EvaluationAggregateStatuses.ConflictReviewRequired;
        }

        if (considered.Any(item => item.Status == CriterionStatuses.InsufficientEvidence)
            && procedure.InsufficiencyBehavior == "complete_insufficient_and_block_aggregation")
        {
            return EvaluationAggregateStatuses.InsufficientEvidence;
        }

        if (considered.Length == 0)
        {
            return EvaluationAggregateStatuses.NotApplicableExcluded;
        }

        if (considered.Any(item => item.Status == CriterionStatuses.NotSatisfied))
        {
            return EvaluationAggregateStatuses.RequirementsNotSatisfied;
        }

        return considered.All(item =>
            item.Status is CriterionStatuses.Satisfied or CriterionStatuses.NotApplicable)
            ? EvaluationAggregateStatuses.Complete
            : EvaluationAggregateStatuses.InsufficientEvidence;
    }
}

public sealed record EvaluationLineage(
    Guid LineageId,
    Guid PredecessorEvaluationId,
    Guid SuccessorRequestId,
    Guid SuccessorEvaluationId,
    string Reason,
    string ActorType,
    string ActorId,
    DateTimeOffset OccurredAtUtc)
{
    public static EvaluationDecision<EvaluationLineage> TryCreate(
        Guid lineageId,
        Guid predecessorEvaluationId,
        Guid successorRequestId,
        Guid successorEvaluationId,
        string reason,
        string actorType,
        string actorId,
        DateTimeOffset occurredAtUtc)
    {
        if (lineageId == Guid.Empty
            || predecessorEvaluationId == Guid.Empty
            || successorRequestId == Guid.Empty
            || successorEvaluationId == Guid.Empty
            || predecessorEvaluationId == successorEvaluationId
            || !EvaluationIdentity.IsStableId(reason)
            || actorType is not (
                EvaluationActorTypes.Human
                or EvaluationActorTypes.Service
                or EvaluationActorTypes.System)
            || !EvaluationIdentity.IsStableId(actorId)
            || !EvaluationIdentity.IsUtc(occurredAtUtc))
        {
            return EvaluationDecision<EvaluationLineage>.Fail(EvaluationFailureCodes.InvalidField);
        }

        return EvaluationDecision<EvaluationLineage>.Ok(
            new EvaluationLineage(
                lineageId,
                predecessorEvaluationId,
                successorRequestId,
                successorEvaluationId,
                reason,
                actorType,
                actorId,
                occurredAtUtc));
    }
}

public sealed record EvaluationAnnotation(
    Guid AnnotationId,
    Guid EvaluationId,
    string Kind,
    string Disposition,
    string Reason,
    string ActorType,
    string ActorId,
    DateTimeOffset OccurredAtUtc,
    Guid? EvidenceId)
{
    public static EvaluationDecision<EvaluationAnnotation> TryCreate(
        Guid annotationId,
        Guid evaluationId,
        string kind,
        string disposition,
        string reason,
        string actorType,
        string actorId,
        DateTimeOffset occurredAtUtc,
        Guid? evidenceId = null)
    {
        if (annotationId == Guid.Empty
            || evaluationId == Guid.Empty
            || kind is not (
                EvaluationAnnotationKinds.SourceIntegrityChanged
                or EvaluationAnnotationKinds.SourceLawfullyUnavailable
                or EvaluationAnnotationKinds.LowerPrecisionRecorded
                or EvaluationAnnotationKinds.LifecycleHold
                or EvaluationAnnotationKinds.LifecycleExpiry)
            || disposition is not (
                EvaluationDispositions.AttentionRequired
                or EvaluationDispositions.LawfullyUnavailable
                or EvaluationDispositions.LowerPrecision
                or EvaluationDispositions.Held
                or EvaluationDispositions.ExpiredMinimumProvenance)
            || !EvaluationIdentity.IsStableId(reason)
            || actorType is not (
                EvaluationActorTypes.Human
                or EvaluationActorTypes.Service
                or EvaluationActorTypes.System)
            || !EvaluationIdentity.IsStableId(actorId)
            || !EvaluationIdentity.IsUtc(occurredAtUtc)
            || evidenceId == Guid.Empty)
        {
            return EvaluationDecision<EvaluationAnnotation>.Fail(EvaluationFailureCodes.InvalidField);
        }

        return EvaluationDecision<EvaluationAnnotation>.Ok(
            new EvaluationAnnotation(
                annotationId,
                evaluationId,
                kind,
                disposition,
                reason,
                actorType,
                actorId,
                occurredAtUtc,
                evidenceId));
    }
}
