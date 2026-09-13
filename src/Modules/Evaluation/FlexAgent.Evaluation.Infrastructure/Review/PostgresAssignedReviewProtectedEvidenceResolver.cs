using System.Text.Json;
using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Application.Review;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure.Review;

public sealed class PostgresAssignedReviewProtectedEvidenceResolver(
    PostgresConnectionAccessor connections,
    IEvaluationSessionEvidenceSource sessionEvidence,
    IEvaluationSubmissionEvidenceSource submissionEvidence,
    IProtectedDeterministicOutputStore deterministicOutputStore) : IAssignedReviewProtectedEvidenceResolver
{
    public async Task<EvaluationDecision<AssignedReviewProtectedEvidenceResolution>> TryResolveAsync(
        AssignedReviewProtectedEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.LocatorCanonicalJson))
        {
            return EvaluationDecision<AssignedReviewProtectedEvidenceResolution>.Fail(ReviewFailureCodes.Unavailable);
        }

        using var locatorDocument = JsonDocument.Parse(request.LocatorCanonicalJson);
        var canonicalLocator = locatorDocument.RootElement;
        var digest = EvidenceLocatorDigestComputer.TryComputeLocatorDigest(canonicalLocator);
        if (!digest.Succeeded
            || !string.Equals(digest.Value, request.LocatorDigest, StringComparison.Ordinal))
        {
            return EvaluationDecision<AssignedReviewProtectedEvidenceResolution>.Fail(ReviewFailureCodes.Denied);
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var handoffId = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                """
                SELECT handoff_id
                FROM evaluation_requests
                WHERE organization_id = @OrganizationId
                  AND request_id = @RequestId;
                """,
                new { request.OrganizationId, request.RequestId },
                cancellationToken: cancellationToken));
        if (string.IsNullOrWhiteSpace(handoffId))
        {
            return EvaluationDecision<AssignedReviewProtectedEvidenceResolution>.Fail(ReviewFailureCodes.Denied);
        }

        var sessionBundle = await sessionEvidence.LoadAsync(
            request.OrganizationId,
            request.SessionId,
            handoffId,
            cancellationToken);
        if (sessionBundle is null)
        {
            return EvaluationDecision<AssignedReviewProtectedEvidenceResolution>.Fail(ReviewFailureCodes.Denied);
        }

        var ownership = sessionBundle.Handoff.Ownership;
        if (ownership.OrganizationId != request.OrganizationId
            || ownership.ActivityId != request.ActivityId
            || ownership.ParticipantId != request.ParticipantId
            || ownership.AttemptId != request.AttemptId
            || ownership.SessionId != request.SessionId)
        {
            return EvaluationDecision<AssignedReviewProtectedEvidenceResolution>.Fail(ReviewFailureCodes.Denied);
        }

        var submissionBundle = await submissionEvidence.LoadBoundItemsAsync(
            ownership.OrganizationId,
            ownership.ActivityId,
            ownership.ParticipantId,
            ownership.AttemptId,
            ownership.SessionId,
            cancellationToken);

        var deterministicFacts = await TryLoadDeterministicFactsAsync(
            connection,
            request,
            canonicalLocator,
            cancellationToken);
        if (!deterministicFacts.Succeeded)
        {
            return EvaluationDecision<AssignedReviewProtectedEvidenceResolution>.Fail(
                deterministicFacts.OutcomeCode,
                deterministicFacts.Field);
        }

        var trustedOwnership = EvaluationStableOwnershipReferenceFactory.From(ownership, request.EvaluationId);
        var context = EvidenceLocatorVerificationContextBuilder.Build(
            trustedOwnership,
            sessionBundle,
            submissionBundle,
            deterministicFacts: deterministicFacts.Value,
            permitWholeItemFallback: false);

        var verified = EvidenceLocatorVerifier.TryVerify(canonicalLocator, context);
        if (!verified.Succeeded || verified.Value is null)
        {
            return EvaluationDecision<AssignedReviewProtectedEvidenceResolution>.Fail(
                verified.OutcomeCode == EvaluationFailureCodes.ProtectedContent
                || verified.OutcomeCode == EvaluationFailureCodes.CitationIntegrity
                    ? ReviewFailureCodes.Denied
                    : ReviewFailureCodes.Unavailable,
                verified.Field);
        }

        var mappedLocator = EvidenceLocatorContractMapper.TryMapV1(canonicalLocator);
        if (!mappedLocator.Succeeded || mappedLocator.Value is null)
        {
            return EvaluationDecision<AssignedReviewProtectedEvidenceResolution>.Fail(
                mappedLocator.OutcomeCode,
                mappedLocator.Field);
        }

        var availability = AssignedReviewProjectionMapper.MapEvidenceAvailability(request.IntegrityState);
        string? displayText = null;
        string? unavailabilityNotice = null;
        if (string.Equals(availability, "available", StringComparison.Ordinal)
            || string.Equals(availability, "lower_precision", StringComparison.Ordinal))
        {
            var display = AssignedReviewEvidenceDisplayExtractor.TryExtractDisplayText(canonicalLocator, context);
            if (display.Succeeded)
            {
                displayText = display.Value;
            }
            else
            {
                availability = "unavailable";
                unavailabilityNotice = "Protected evidence material could not be materialized for display.";
            }
        }
        else
        {
            unavailabilityNotice = "Protected evidence material is not available through this API surface.";
        }

        return EvaluationDecision<AssignedReviewProtectedEvidenceResolution>.Ok(
            new AssignedReviewProtectedEvidenceResolution(
                mappedLocator.Value,
                availability,
                displayText,
                unavailabilityNotice));
    }

    private async Task<EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>> TryLoadDeterministicFactsAsync(
        Npgsql.NpgsqlConnection connection,
        AssignedReviewProtectedEvidenceRequest request,
        JsonElement canonicalLocator,
        CancellationToken cancellationToken)
    {
        if (!DeterministicFactContextLoader.TryGetDeterministicFactRequirement(
                canonicalLocator,
                out var sourceId,
                out var attemptId,
                out var digest))
        {
            return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Ok(
                new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal));
        }

        var criterion = await connection.QuerySingleOrDefaultAsync<CriterionBindingRow>(
            new CommandDefinition(
                """
                SELECT judgment.criterion_id, judgment.criterion_version
                FROM evaluation_criterion_judgment_evidence_refs AS evidence_ref
                INNER JOIN evaluation_criterion_judgments AS judgment
                  ON judgment.organization_id = evidence_ref.organization_id
                 AND judgment.evaluation_id = evidence_ref.evaluation_id
                 AND judgment.judgment_id = evidence_ref.judgment_id
                WHERE evidence_ref.organization_id = @OrganizationId
                  AND evidence_ref.evaluation_id = @EvaluationId
                  AND evidence_ref.evidence_id = @EvidenceId
                LIMIT 1;
                """,
                new
                {
                    request.OrganizationId,
                    request.EvaluationId,
                    request.EvidenceId,
                },
                cancellationToken: cancellationToken));
        if (criterion is null)
        {
            return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Fail(
                ReviewFailureCodes.Denied);
        }

        var projection = await deterministicOutputStore.TryLoadProjectionAsync(
            request.OrganizationId,
            request.RequestId,
            attemptId,
            digest,
            criterion.criterion_id,
            criterion.criterion_version,
            cancellationToken);
        if (projection is null || !string.Equals(projection.SourceId, sourceId, StringComparison.Ordinal))
        {
            return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Fail(
                ReviewFailureCodes.Denied);
        }

        return EvaluationDecision<IReadOnlyDictionary<string, EvaluationSafeFactProjection>>.Ok(
            new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal)
            {
                [sourceId] = projection,
            });
    }

    private sealed record CriterionBindingRow(string criterion_id, string criterion_version);
}
