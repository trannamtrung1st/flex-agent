namespace FlexAgent.Evaluation.Domain;

public static class EvaluationEvidenceSourceIdentity
{
    public const string LocatorAdapterVersion = "locator-adapter.v1";
    public const string LineSplitProcedureVersion = "line-split.v1";

    public static string SubmissionItemSourceId(Guid itemId) => $"item.{itemId:N}";

    public static string SubmissionVersionSourceVersion(int versionNumber) =>
        $"rev.{versionNumber:D4}";

    public static string TranscriptSourceVersion(string protectedRef) =>
        protectedRef.StartsWith("rev.", StringComparison.Ordinal)
            ? protectedRef
            : $"rev.{protectedRef}";

    public static string ConfigurationFactSourceId(Guid configurationId) =>
        $"cfg.{configurationId:N}";

    public static string ManifestFactSourceId(Guid manifestId) =>
        $"mfst.{manifestId:N}";

    public static string DigestBoundSourceVersion(string contentDigest) =>
        $"rev.{contentDigest}";
}

public sealed record EvidenceLocatorVerificationContext(
    EvaluationStableOwnershipReference TrustedOwnership,
    long TerminalCutoffSequence,
    IReadOnlyDictionary<string, EvaluationSessionTranscriptMaterial> TranscriptItemsByMessageId,
    IReadOnlyDictionary<string, EvaluationSubmissionMaterial> SubmissionItemsBySourceId,
    IReadOnlyDictionary<string, EvaluationSafeFactProjection> SafeConfigurationFactsBySourceId,
    IReadOnlyDictionary<string, EvaluationSafeFactProjection> SafeManifestFactsBySourceId,
    bool PermitWholeItemFallback);

public sealed record EvaluationSafeFactProjection(
    string SourceId,
    string SourceVersion,
    string ContentDigest,
    ReadOnlyMemory<byte> ProjectionUtf8);

public sealed record EvaluationStableOwnershipReference(
    string OrganizationId,
    string ActivityId,
    string ParticipantId,
    string AttemptId,
    string SessionId,
    string EvaluationId);

public sealed record EvaluationSessionTranscriptMaterial(
    string MessageId,
    string SourceVersion,
    long PublishedSequence,
    string ContentDigest,
    ReadOnlyMemory<byte> ExactUtf8);

public sealed record EvaluationSubmissionMaterial(
    string SourceId,
    string SourceVersion,
    string Category,
    string ContentDigest,
    ReadOnlyMemory<byte> ExactUtf8);

public sealed record VerifiedEvidenceLocator(
    string SourceType,
    string SourceRefDigest,
    string LocationDigest,
    string VerificationState,
    string ResolvedSourceDigest);
