using System.Text.Json;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record EvidenceLocatorVerificationEntry(
    string EvidenceId,
    JsonElement Locator);

public sealed record EvidenceLocatorCompletionRequest(
    Guid EvaluationId,
    string HandoffId,
    IReadOnlyList<EvidenceLocatorVerificationEntry> Entries,
    bool PermitWholeItemFallback);

public sealed record EvidenceLocatorCompletionResult(
    IReadOnlyList<VerifiedEvidenceLocator> VerifiedLocators,
    IReadOnlyList<SealedEvidenceItemReference> SealedItems);

public interface IEvidenceLocatorCompletionService
{
    Task<EvaluationDecision<EvidenceLocatorCompletionResult>> TryVerifyAsync(
        Guid organizationId,
        Guid sessionId,
        EvidenceLocatorCompletionRequest request,
        CancellationToken cancellationToken);
}

public static class EvidenceLocatorCompletionVerifier
{
    public static EvaluationDecision<EvidenceLocatorCompletionResult> TryVerify(
        Guid organizationId,
        Guid sessionId,
        EvidenceLocatorCompletionRequest request,
        EvaluationSessionEvidenceBundle sessionEvidence,
        EvaluationSubmissionEvidenceBundle? submissionEvidence)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(sessionEvidence);

        var binding = TryBindTrustedOwnership(
            organizationId,
            sessionId,
            request,
            sessionEvidence,
            out var trustedOwnership);
        if (binding is not null)
        {
            return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(
                binding.OutcomeCode,
                binding.Field);
        }

        if (request.Entries.Count is < 1 or > 128)
        {
            return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(EvaluationFailureCodes.InvalidField);
        }

        if (request.Entries.Any(entry => !EvaluationIdentity.IsStableId(entry.EvidenceId)))
        {
            return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(EvaluationFailureCodes.InvalidField);
        }

        if (request.Entries
                .GroupBy(entry => entry.EvidenceId, StringComparer.Ordinal)
                .Any(group => group.Count() > 1))
        {
            return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(EvaluationFailureCodes.DuplicateIdentity);
        }

        var context = EvidenceLocatorVerificationContextBuilder.Build(
            trustedOwnership!,
            sessionEvidence,
            submissionEvidence,
            permitWholeItemFallback: request.PermitWholeItemFallback);

        var verified = new List<VerifiedEvidenceLocator>(request.Entries.Count);
        var sealedItems = new List<SealedEvidenceItemReference>(request.Entries.Count);
        foreach (var entry in request.Entries)
        {
            var result = EvidenceLocatorVerifier.TryVerify(entry.Locator, context);
            if (!result.Succeeded || result.Value is null)
            {
                return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(
                    result.OutcomeCode,
                    result.Field);
            }

            verified.Add(result.Value);
            sealedItems.Add(
                new SealedEvidenceItemReference(
                    entry.EvidenceId,
                    result.Value.SourceType,
                    result.Value.SourceRefDigest,
                    result.Value.LocationDigest,
                    result.Value.VerificationState));
        }

        return EvaluationDecision<EvidenceLocatorCompletionResult>.Ok(
            new EvidenceLocatorCompletionResult(verified, sealedItems));
    }

    internal static EvaluationDecision<EvaluationStableOwnershipReference>? TryBindTrustedOwnership(
        Guid organizationId,
        Guid sessionId,
        EvidenceLocatorCompletionRequest request,
        EvaluationSessionEvidenceBundle sessionEvidence,
        out EvaluationStableOwnershipReference? trustedOwnership)
    {
        trustedOwnership = null;
        var handoff = sessionEvidence.Handoff;
        var ownership = handoff.Ownership;

        if (!string.Equals(request.HandoffId, handoff.HandoffId, StringComparison.Ordinal))
        {
            return EvaluationDecision<EvaluationStableOwnershipReference>.Fail(
                EvaluationFailureCodes.IncompleteOwnership,
                "handoff_id");
        }

        if (request.EvaluationId == Guid.Empty
            || organizationId != ownership.OrganizationId
            || sessionId != ownership.SessionId)
        {
            return EvaluationDecision<EvaluationStableOwnershipReference>.Fail(
                EvaluationFailureCodes.IncompleteOwnership);
        }

        trustedOwnership = EvaluationStableOwnershipReferenceFactory.From(
            ownership,
            request.EvaluationId);
        return null;
    }
}

public sealed class EvidenceLocatorCompletionService(
    IEvaluationSessionEvidenceSource sessionEvidence,
    IEvaluationSubmissionEvidenceSource submissionEvidence) : IEvidenceLocatorCompletionService
{
    public async Task<EvaluationDecision<EvidenceLocatorCompletionResult>> TryVerifyAsync(
        Guid organizationId,
        Guid sessionId,
        EvidenceLocatorCompletionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sessionBundle = await sessionEvidence.LoadAsync(
            organizationId,
            sessionId,
            request.HandoffId,
            cancellationToken);
        if (sessionBundle is null)
        {
            return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(
                EvaluationFailureCodes.ProtectedContent);
        }

        var ownership = sessionBundle.Handoff.Ownership;
        var submissionBundle = await submissionEvidence.LoadBoundItemsAsync(
            ownership.OrganizationId,
            ownership.ActivityId,
            ownership.ParticipantId,
            ownership.AttemptId,
            ownership.SessionId,
            cancellationToken);

        return EvidenceLocatorCompletionVerifier.TryVerify(
            organizationId,
            sessionId,
            request,
            sessionBundle,
            submissionBundle);
    }
}
