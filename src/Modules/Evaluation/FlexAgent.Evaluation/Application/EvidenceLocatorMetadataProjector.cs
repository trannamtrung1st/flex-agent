using System.Text.Json;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record EvaluationEvidenceLocatorRecord(
    Guid EvidenceId,
    string SourceType,
    Guid SourceId,
    Guid SourceVersionId,
    string SourceContentDigest,
    string LocatorSchema,
    string LocatorDigest,
    string Precision,
    string IntegrityState);

public interface IEvaluationEvidenceLocatorStore
{
    Task<EvaluationDecision<IReadOnlyList<Guid>>> TryPersistAsync(
        Guid organizationId,
        Guid evaluationId,
        Guid requestId,
        EvaluationOwnership ownership,
        IReadOnlyList<EvaluationEvidenceLocatorRecord> records,
        string createdByService,
        CancellationToken cancellationToken);
}

public static class EvidenceLocatorMetadataProjector
{
    public static EvaluationDecision<EvaluationEvidenceLocatorRecord> TryCreate(
        Guid evidenceId,
        JsonElement locator,
        VerifiedEvidenceLocator verified,
        EvaluationHandoffSnapshot handoff,
        EvaluationSubmissionEvidenceBundle? submissionEvidence)
    {
        if (evidenceId == Guid.Empty
            || !TryGetRequiredString(locator, "locator_schema", out var locatorSchema)
            || !locator.TryGetProperty("source_ref", out var sourceRef)
            || !TryGetRequiredString(sourceRef, "source_id", out var sourceId)
            || !TryGetRequiredString(sourceRef, "source_version", out var sourceVersion))
        {
            return EvaluationDecision<EvaluationEvidenceLocatorRecord>.Fail(EvaluationFailureCodes.InvalidField);
        }

        var source = TryResolveProtectedSource(
            verified.SourceType,
            sourceId,
            sourceVersion,
            verified.ResolvedSourceDigest,
            handoff,
            submissionEvidence);
        if (source is null)
        {
            return EvaluationDecision<EvaluationEvidenceLocatorRecord>.Fail(
                EvaluationFailureCodes.ProtectedContent,
                "source_ref");
        }

        return EvaluationDecision<EvaluationEvidenceLocatorRecord>.Ok(
            new EvaluationEvidenceLocatorRecord(
                evidenceId,
                verified.SourceType,
                source.Value.SourceId,
                source.Value.SourceVersionId,
                verified.ResolvedSourceDigest,
                locatorSchema,
                verified.VerifiedLocatorDigest,
                verified.VerifiedPrecision,
                verified.VerificationState));
    }

    private static (Guid SourceId, Guid SourceVersionId)? TryResolveProtectedSource(
        string sourceType,
        string sourceId,
        string sourceVersion,
        string contentDigest,
        EvaluationHandoffSnapshot handoff,
        EvaluationSubmissionEvidenceBundle? submissionEvidence)
    {
        return sourceType switch
        {
            "submission.direct_text" or "submission.text_attachment"
                when submissionEvidence is not null
                     && submissionEvidence.BoundItems.FirstOrDefault(item =>
                         string.Equals(item.SourceId, sourceId, StringComparison.Ordinal)
                         && string.Equals(item.SourceVersion, sourceVersion, StringComparison.Ordinal)
                         && string.Equals(item.ContentDigest, contentDigest, StringComparison.Ordinal))
                     is { } submission =>
                (submission.ItemRecordId, submission.AcceptedVersionId),
            "session.transcript_item" =>
                (
                    EvaluationDeterministicGuid.CreateVersion5(EvaluationSourceNamespaces.TranscriptItem, sourceId),
                    EvaluationDeterministicGuid.CreateVersion5(EvaluationSourceNamespaces.TranscriptVersion, sourceVersion)),
            "session.work_trace" =>
                (
                    EvaluationDeterministicGuid.CreateVersion5(EvaluationSourceNamespaces.WorkTraceItem, sourceId),
                    EvaluationDeterministicGuid.CreateVersion5(EvaluationSourceNamespaces.WorkTraceVersion, sourceVersion)),
            "configuration.fact"
                when EvaluationEvidenceSourceIdentity.TryParseConfigurationFactSourceId(sourceId, out var configurationId)
                     && configurationId == handoff.ConfigurationId
                     && string.Equals(
                         sourceVersion,
                         EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(handoff.ConfigurationDigest),
                         StringComparison.Ordinal) =>
                (
                    configurationId,
                    EvaluationDeterministicGuid.CreateVersion5(
                        EvaluationSourceNamespaces.ConfigurationVersion,
                        handoff.ConfigurationDigest)),
            "manifest.fact"
                when EvaluationEvidenceSourceIdentity.TryParseManifestFactSourceId(sourceId, out var manifestId)
                     && manifestId == handoff.ManifestId
                     && string.Equals(
                         sourceVersion,
                         EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(handoff.ManifestDigest),
                         StringComparison.Ordinal) =>
                (
                    manifestId,
                    EvaluationDeterministicGuid.CreateVersion5(
                        EvaluationSourceNamespaces.ManifestVersion,
                        handoff.ManifestDigest)),
            _ => null,
        };
    }

    private static bool TryGetRequiredString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }
}
