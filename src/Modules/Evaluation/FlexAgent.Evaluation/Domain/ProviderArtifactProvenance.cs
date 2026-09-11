using System.Security.Cryptography;
using System.Text;
using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public static class ProviderArtifactOutcomes
{
    public const string Succeeded = "succeeded";

    public const string Failed = "failed";

    public const string TimedOut = "timed_out";

    public const string InvalidOutput = "invalid_output";

    public static string FromExecutionOutcomeCategory(string outcomeCategory) =>
        outcomeCategory switch
        {
            "succeeded" => Succeeded,
            "provider_timeout" => TimedOut,
            "schema_invalid" => InvalidOutput,
            _ => Failed,
        };
}

public static class ProviderArtifactIdentity
{
    public static string ComputeRequestContentDigest(
        Guid requestId,
        Guid invocationAttemptId,
        string criterionId,
        string criterionVersion,
        FrozenModelIdentity modelIdentity,
        string instructionVersion,
        IReadOnlyList<EvaluationModelPermittedEvidenceV1> permittedEvidence)
    {
        var evidenceBinding = string.Join(
            ',',
            permittedEvidence
                .OrderBy(item => item.EvidenceId, StringComparer.Ordinal)
                .Select(item => $"{item.EvidenceId}:{item.SourceType}:{item.ContentDigest}"));
        var payload = Encoding.UTF8.GetBytes(
            string.Join(
                '|',
                requestId.ToString("N"),
                invocationAttemptId.ToString("N"),
                criterionId,
                criterionVersion,
                modelIdentity.ProfileId,
                modelIdentity.ProfileVersion,
                modelIdentity.ProfileDigest,
                modelIdentity.ProviderId,
                modelIdentity.CredentialMode,
                modelIdentity.CredentialBindingReference,
                modelIdentity.CredentialBindingVersion,
                instructionVersion,
                evidenceBinding));
        return Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
    }

    public static Guid ComputeArtifactId(
        Guid requestId,
        Guid invocationAttemptId,
        string criterionId,
        string criterionVersion,
        string requestContentDigest)
    {
        var payload = Encoding.UTF8.GetBytes(
            string.Join(
                '|',
                requestId.ToString("N"),
                invocationAttemptId.ToString("N"),
                criterionId,
                criterionVersion,
                requestContentDigest));
        var hash = SHA256.HashData(payload);
        return new Guid(hash.AsSpan(0, 16));
    }
}

public sealed record ProviderArtifactProvenanceSnapshot(
    Guid InvocationAttemptId,
    string CriterionId,
    string CriterionVersion,
    string ModelProfileId,
    string ModelProfileVersion,
    string ModelProfileDigest,
    string CredentialBindingReference,
    string ProtectedRequestRef,
    string? ProtectedResponseRef,
    string Outcome,
    string? FailureCategory);

public static class ProviderArtifactProvenance
{
    public static string ProtectedRequestRef(string requestContentDigest) =>
        $"prot.eval.model-req.{requestContentDigest}";

    public static string? ProtectedResponseRef(string? responseContentDigest) =>
        responseContentDigest is null
            ? null
            : $"prot.eval.model-res.{responseContentDigest}";

    public static ProviderArtifactProvenanceSnapshot FromAppendCommand(
        ProviderArtifactAppendCommand command) =>
        new(
            command.InvocationAttemptId,
            command.CriterionId,
            command.CriterionVersion,
            command.ModelIdentity.ProfileId,
            command.ModelIdentity.ProfileVersion,
            command.ModelIdentity.ProfileDigest,
            command.ModelIdentity.CredentialBindingReference,
            command.ProtectedRequestRef,
            command.ProtectedResponseRef,
            command.Outcome,
            command.FailureCategory);

    public static bool IsEquivalentRetry(
        ProviderArtifactProvenanceSnapshot existing,
        ProviderArtifactProvenanceSnapshot candidate) =>
        existing.InvocationAttemptId == candidate.InvocationAttemptId
        && string.Equals(existing.CriterionId, candidate.CriterionId, StringComparison.Ordinal)
        && string.Equals(existing.CriterionVersion, candidate.CriterionVersion, StringComparison.Ordinal)
        && string.Equals(existing.ModelProfileId, candidate.ModelProfileId, StringComparison.Ordinal)
        && string.Equals(existing.ModelProfileVersion, candidate.ModelProfileVersion, StringComparison.Ordinal)
        && string.Equals(existing.ModelProfileDigest, candidate.ModelProfileDigest, StringComparison.Ordinal)
        && string.Equals(
            existing.CredentialBindingReference,
            candidate.CredentialBindingReference,
            StringComparison.Ordinal)
        && string.Equals(existing.ProtectedRequestRef, candidate.ProtectedRequestRef, StringComparison.Ordinal)
        && string.Equals(existing.ProtectedResponseRef, candidate.ProtectedResponseRef, StringComparison.Ordinal)
        && string.Equals(existing.Outcome, candidate.Outcome, StringComparison.Ordinal)
        && string.Equals(existing.FailureCategory, candidate.FailureCategory, StringComparison.Ordinal);
}

public sealed record ProviderArtifactAppendCommand(
    EvaluationOwnership Ownership,
    Guid RequestId,
    Guid InvocationAttemptId,
    Guid ProviderArtifactId,
    string CriterionId,
    string CriterionVersion,
    FrozenModelIdentity ModelIdentity,
    string ProtectedRequestRef,
    string? ProtectedResponseRef,
    string Outcome,
    string? FailureCategory);
