namespace FlexAgent.Submissions.Application;

using FlexAgent.Submissions.Domain;

public sealed record ArtifactObjectKey(string Value)
{
    private const string Prefix = "org/";

    public static ArtifactObjectKey Create(Guid organizationId, Guid artifactId) =>
        new($"{Prefix}{organizationId:D}/{artifactId:D}");

    public Guid? ScopedOrganizationId
    {
        get
        {
            if (!Value.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return null;
            }

            var remainder = Value[Prefix.Length..];
            var separator = remainder.IndexOf('/');
            if (separator <= 0)
            {
                return null;
            }

            return Guid.TryParse(remainder[..separator], out var organizationId)
                ? organizationId
                : null;
        }
    }

    public bool BelongsToOrganization(Guid organizationId) =>
        ScopedOrganizationId == organizationId;
}

public sealed record ArtifactVersionId(string Value);

public sealed record ArtifactDigest(string Sha256Hex)
{
    public static ArtifactDigest FromHex(string hex) =>
        new(hex.ToLowerInvariant());
}

public sealed record StoredArtifactReference(
    ArtifactObjectKey ObjectKey,
    ArtifactVersionId VersionId,
    ArtifactDigest Digest,
    long ByteCount);

public sealed record ArtifactPutRequest(
    Guid OrganizationId,
    ArtifactObjectKey ObjectKey,
    ReadOnlyMemory<byte> Content,
    string ContentType,
    bool ConditionalCreate = true);

public sealed record ArtifactPutResult(
    bool Succeeded,
    StoredArtifactReference? Reference,
    string OutcomeCode);

public sealed record ArtifactGetRequest(
    Guid OrganizationId,
    StoredArtifactReference Reference);

public sealed record ArtifactGetResult(
    bool Succeeded,
    ReadOnlyMemory<byte> Content,
    string OutcomeCode);

public sealed record ArtifactPresignRequest(
    Guid OrganizationId,
    Guid ActorId,
    string Action,
    ArtifactObjectKey ObjectKey,
    TimeSpan Lifetime,
    long? MaxContentLength = null,
    string? ContentType = null);

public sealed record ArtifactPresignResult(
    bool Succeeded,
    Uri? PresignedUrl,
    DateTimeOffset? ExpiresAtUtc,
    string OutcomeCode);

public enum ArtifactScanOutcome
{
    Clean,
    Rejected,
    Inconclusive,
    Unavailable,
}

public static class ArtifactScanOutcomeMapper
{
    public static MaterialScanOutcome ToDomain(ArtifactScanOutcome outcome) => outcome switch
    {
        ArtifactScanOutcome.Clean => MaterialScanOutcome.Clean,
        ArtifactScanOutcome.Rejected => MaterialScanOutcome.Rejected,
        ArtifactScanOutcome.Unavailable => MaterialScanOutcome.Unavailable,
        _ => MaterialScanOutcome.Inconclusive,
    };
}

public sealed record ArtifactScanRequest(
    Guid OrganizationId,
    StoredArtifactReference Reference,
    string MaterialCategory);

public sealed record ArtifactScanResult(
    bool Succeeded,
    ArtifactScanOutcome Outcome,
    string OutcomeCode);

public interface IArtifactStore
{
    Task<ArtifactPutResult> PutAsync(ArtifactPutRequest request, CancellationToken cancellationToken = default);

    Task<ArtifactGetResult> GetExactVersionAsync(ArtifactGetRequest request, CancellationToken cancellationToken = default);

    Task<ArtifactPresignResult> IssueUploadCapabilityAsync(
        ArtifactPresignRequest request,
        CancellationToken cancellationToken = default);

    Task<ArtifactPresignResult> IssueDownloadCapabilityAsync(
        ArtifactPresignRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid organizationId, StoredArtifactReference reference, CancellationToken cancellationToken = default);
}

public interface IArtifactSafetyScanner
{
    Task<ArtifactScanResult> ScanAsync(ArtifactScanRequest request, CancellationToken cancellationToken = default);
}

public static class ArtifactOutcomeCodes
{
    public const string Stored = "stored";
    public const string AlreadyExists = "already_exists";
    public const string NotFound = "not_found";
    public const string VersionMismatch = "version_mismatch";
    public const string DigestMismatch = "digest_mismatch";
    public const string ScopeMismatch = "scope_mismatch";
    public const string StorageUnavailable = "storage_unavailable";
    public const string Presigned = "presigned";
    public const string ScanClean = "scan_clean";
    public const string ScanRejected = "scan_rejected";
    public const string ScanInconclusive = "scan_inconclusive";
    public const string ScanUnavailable = "scan_unavailable";
    public const string ScanDisabled = "scan_disabled";
}
