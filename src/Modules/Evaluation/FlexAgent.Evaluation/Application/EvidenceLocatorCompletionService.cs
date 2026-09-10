using System.Text.Json;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record EvidenceLocatorVerificationEntry(
    Guid EvidenceId,
    string CriterionId,
    JsonElement Locator);

public sealed record EvidenceLocatorCompletionRequest(
    Guid EvaluationId,
    Guid RequestId,
    string HandoffId,
    IReadOnlyList<EvidenceLocatorVerificationEntry> Entries);

public sealed record EvidenceLocatorCompletionResult(
    IReadOnlyList<VerifiedEvidenceLocator> VerifiedLocators,
    IReadOnlyList<SealedEvidenceItemReference> SealedItems,
    IReadOnlyList<EvaluationEvidenceLocatorRecord> LocatorRecords);

public interface IEvidenceLocatorCompletionService
{
    Task<EvaluationDecision<EvidenceLocatorCompletionResult>> TryVerifyAsync(
        Guid organizationId,
        Guid sessionId,
        EvaluationProcedureV1 procedure,
        EvidenceLocatorCompletionRequest request,
        CancellationToken cancellationToken);

    Task<EvaluationDecision<EvidenceLocatorCompletionResult>> TryVerifyAndPersistAsync(
        Guid organizationId,
        Guid sessionId,
        EvaluationProcedureV1 procedure,
        EvidenceLocatorCompletionRequest request,
        string createdByService,
        CancellationToken cancellationToken);
}

public static class EvidenceLocatorCompletionVerifier
{
    public static EvaluationDecision<EvidenceLocatorCompletionResult> TryVerify(
        Guid organizationId,
        Guid sessionId,
        EvaluationProcedureV1 procedure,
        EvidenceLocatorCompletionRequest request,
        EvaluationSessionEvidenceBundle sessionEvidence,
        EvaluationSubmissionEvidenceBundle? submissionEvidence,
        IReadOnlyDictionary<string, EvaluationSafeFactProjection>? deterministicFacts = null)
    {
        ArgumentNullException.ThrowIfNull(procedure);
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

        if (request.RequestId == Guid.Empty
            || request.Entries.Count is < 1 or > 128)
        {
            return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(EvaluationFailureCodes.InvalidField);
        }

        if (request.Entries.Any(entry => entry.EvidenceId == Guid.Empty))
        {
            return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(EvaluationFailureCodes.InvalidField);
        }

        if (request.Entries
                .GroupBy(entry => entry.EvidenceId)
                .Any(group => group.Count() > 1))
        {
            return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(EvaluationFailureCodes.DuplicateIdentity);
        }

        var verified = new List<VerifiedEvidenceLocator>(request.Entries.Count);
        var sealedItems = new List<SealedEvidenceItemReference>(request.Entries.Count);
        var locatorRecords = new List<EvaluationEvidenceLocatorRecord>(request.Entries.Count);
        foreach (var entry in request.Entries)
        {
            var fallbackPolicy = EvidenceLocatorProcedurePolicy.TryResolveWholeItemFallbackPermitted(
                procedure,
                entry.CriterionId);
            if (!fallbackPolicy.Succeeded)
            {
                return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(
                    fallbackPolicy.OutcomeCode,
                    fallbackPolicy.Field);
            }

            var context = EvidenceLocatorVerificationContextBuilder.Build(
                trustedOwnership!,
                sessionEvidence,
                submissionEvidence,
                deterministicFacts: deterministicFacts,
                permitWholeItemFallback: fallbackPolicy.Value);

            var result = EvidenceLocatorVerifier.TryVerify(entry.Locator, context);
            if (!result.Succeeded || result.Value is null)
            {
                return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(
                    result.OutcomeCode,
                    result.Field);
            }

            var metadata = EvidenceLocatorMetadataProjector.TryCreate(
                entry.EvidenceId,
                entry.Locator,
                result.Value,
                sessionEvidence.Handoff,
                submissionEvidence);
            if (!metadata.Succeeded || metadata.Value is null)
            {
                return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(
                    metadata.OutcomeCode,
                    metadata.Field);
            }

            verified.Add(result.Value);
            sealedItems.Add(
                new SealedEvidenceItemReference(
                    EvaluationEvidenceSourceIdentity.StableEvidenceId(entry.EvidenceId),
                    result.Value.SourceType,
                    result.Value.SourceRefDigest,
                    result.Value.LocationDigest,
                    result.Value.VerificationState));
            locatorRecords.Add(metadata.Value);
        }

        return EvaluationDecision<EvidenceLocatorCompletionResult>.Ok(
            new EvidenceLocatorCompletionResult(verified, sealedItems, locatorRecords));
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
    IEvaluationSubmissionEvidenceSource submissionEvidence,
    IProtectedDeterministicOutputStore deterministicOutputStore,
    IEvaluationEvidenceLocatorStore locatorStore) : IEvidenceLocatorCompletionService
{
    public async Task<EvaluationDecision<EvidenceLocatorCompletionResult>> TryVerifyAsync(
        Guid organizationId,
        Guid sessionId,
        EvaluationProcedureV1 procedure,
        EvidenceLocatorCompletionRequest request,
        CancellationToken cancellationToken) =>
        await TryVerifyInternalAsync(
            organizationId,
            sessionId,
            procedure,
            request,
            persist: false,
            createdByService: null,
            cancellationToken);

    public async Task<EvaluationDecision<EvidenceLocatorCompletionResult>> TryVerifyAndPersistAsync(
        Guid organizationId,
        Guid sessionId,
        EvaluationProcedureV1 procedure,
        EvidenceLocatorCompletionRequest request,
        string createdByService,
        CancellationToken cancellationToken) =>
        await TryVerifyInternalAsync(
            organizationId,
            sessionId,
            procedure,
            request,
            persist: true,
            createdByService,
            cancellationToken);

    private async Task<EvaluationDecision<EvidenceLocatorCompletionResult>> TryVerifyInternalAsync(
        Guid organizationId,
        Guid sessionId,
        EvaluationProcedureV1 procedure,
        EvidenceLocatorCompletionRequest request,
        bool persist,
        string? createdByService,
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

        var deterministicFacts = await DeterministicFactContextLoader.TryLoadForCompletionAsync(
            organizationId,
            request.RequestId,
            request.Entries,
            deterministicOutputStore,
            cancellationToken);
        if (!deterministicFacts.Succeeded)
        {
            return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(
                deterministicFacts.OutcomeCode,
                deterministicFacts.Field);
        }

        var result = EvidenceLocatorCompletionVerifier.TryVerify(
            organizationId,
            sessionId,
            procedure,
            request,
            sessionBundle,
            submissionBundle,
            deterministicFacts.Value);
        if (!result.Succeeded || result.Value is null || !persist)
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(createdByService))
        {
            return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(EvaluationFailureCodes.InvalidField);
        }

        var persisted = await locatorStore.TryPersistAsync(
            organizationId,
            request.EvaluationId,
            request.RequestId,
            ownership,
            result.Value.LocatorRecords,
            createdByService,
            cancellationToken);
        if (!persisted.Succeeded)
        {
            return EvaluationDecision<EvidenceLocatorCompletionResult>.Fail(
                persisted.OutcomeCode,
                persisted.Field);
        }

        return result;
    }
}
