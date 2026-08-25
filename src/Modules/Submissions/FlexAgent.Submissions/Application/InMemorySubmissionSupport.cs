using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

internal static class InMemorySubmissionIdentity
{
    internal static readonly Dictionary<(Guid OrganizationId, Guid EnrollmentId), Guid> ByEnrollment = new();
}

public sealed class InMemoryIntakeStore : IIntakeStore
{
    private readonly Dictionary<(Guid OrganizationId, Guid IntakeId), SubmissionIntakeRecord> _intakes = new();

    public Task<SubmissionIntakeRecord?> FindIntakeAsync(
        Guid organizationId,
        Guid enrollmentId,
        Guid intakeId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_intakes.TryGetValue((organizationId, intakeId), out var intake)
            && intake.Scope.EnrollmentId == enrollmentId
            ? intake
            : null);

    public Task<SubmissionIntakeRecord?> FindActiveIntakeAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var intake = _intakes.Values
            .Where(item => item.Scope.OrganizationId == organizationId
                && item.Scope.EnrollmentId == enrollmentId
                && !IntakeStateMachine.IsTerminal(item.Status))
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefault();
        return Task.FromResult(intake);
    }

    public Task InsertIntakeAsync(
        SubmissionIntakeRecord intake,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        _intakes[(intake.Scope.OrganizationId, intake.IntakeId)] = intake;
        InMemorySubmissionIdentity.ByEnrollment[(intake.Scope.OrganizationId, intake.Scope.EnrollmentId)] = intake.SubmissionId;
        return Task.CompletedTask;
    }

    public Task UpdateIntakeAsync(
        SubmissionIntakeRecord intake,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        _intakes[(intake.Scope.OrganizationId, intake.IntakeId)] = intake;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SubmissionIntakeRecord>> ListIncompleteCreatedBeforeAsync(
        DateTimeOffset cutoffUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var items = _intakes.Values
            .Where(intake => !IntakeStateMachine.IsTerminal(intake.Status) && intake.CreatedAtUtc <= cutoffUtc)
            .OrderBy(intake => intake.CreatedAtUtc)
            .Take(limit)
            .ToArray();
        return Task.FromResult<IReadOnlyList<SubmissionIntakeRecord>>(items);
    }

    public Task<IReadOnlyList<SubmissionIntakeRecord>> ListRejectedUpdatedBeforeAsync(
        DateTimeOffset cutoffUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var items = _intakes.Values
            .Where(intake => intake.Status is IntakeStates.Cancelled or IntakeStates.Rejected or IntakeStates.Failed
                && intake.UpdatedAtUtc <= cutoffUtc)
            .OrderBy(intake => intake.UpdatedAtUtc)
            .Take(limit)
            .ToArray();
        return Task.FromResult<IReadOnlyList<SubmissionIntakeRecord>>(items);
    }
}

public sealed class InMemorySubmissionVersionStore : ISubmissionVersionStore
{
    private readonly Dictionary<(Guid OrganizationId, Guid VersionId), AcceptedSubmissionVersion> _versions = new();

    public Task<Guid?> FindSubmissionIdByEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(InMemorySubmissionIdentity.ByEnrollment.TryGetValue((organizationId, enrollmentId), out var submissionId)
            ? submissionId
            : (Guid?)null);

    public Task<IReadOnlyList<AcceptedVersionSummary>> ListVersionsAsync(
        Guid organizationId,
        Guid submissionId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default)
    {
        var items = _versions.Values
            .Where(version => version.Scope.OrganizationId == organizationId && version.SubmissionId == submissionId)
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => new AcceptedVersionSummary(
                version.VersionId,
                version.VersionNumber,
                version.AcceptedAtUtc,
                version.Items.Count))
            .ToArray();
        return Task.FromResult<IReadOnlyList<AcceptedVersionSummary>>(items);
    }

    public Task<AcceptedSubmissionVersion?> FindVersionAsync(
        Guid organizationId,
        Guid versionId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_versions.TryGetValue((organizationId, versionId), out var version) ? version : null);

    public Task<SubmissionVersionAllocation> AllocateNextVersionAsync(
        Guid organizationId,
        Guid submissionId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var latest = _versions.Values
            .Where(version => version.Scope.OrganizationId == organizationId && version.SubmissionId == submissionId)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();
        var nextNumber = (latest?.VersionNumber ?? 0) + 1;
        return Task.FromResult(new SubmissionVersionAllocation(
            nextNumber,
            latest?.VersionId));
    }

    public Task InsertAcceptedVersionAsync(
        AcceptedSubmissionVersion version,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        _versions[(version.Scope.OrganizationId, version.VersionId)] = version;
        InMemorySubmissionIdentity.ByEnrollment[(version.Scope.OrganizationId, version.Scope.EnrollmentId)] = version.SubmissionId;
        return Task.CompletedTask;
    }

    public Task<bool> HasAcceptedArtifactKeyAsync(
        Guid organizationId,
        string artifactObjectKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_versions.Values.Any(version =>
            version.Scope.OrganizationId == organizationId
            && version.Items.Any(item => string.Equals(item.ArtifactObjectKey, artifactObjectKey, StringComparison.Ordinal))));

    public Task<IReadOnlyList<AcceptedArtifactCleanupCandidate>> ListAcceptedArtifactCandidatesAsync(
        int limit,
        AcceptedArtifactCleanupCursor? after = null,
        CancellationToken cancellationToken = default)
    {
        var items = _versions.Values
            .SelectMany(version => version.Items.Select(item => new AcceptedArtifactCleanupCandidate(
                version.Scope.OrganizationId,
                version.Scope.ActivityId,
                version.Scope.EnrollmentId,
                version.VersionId,
                item.ItemId,
                version.AcceptedAtUtc,
                item.ArtifactObjectKey,
                item.ArtifactVersionId)))
            .OrderBy(candidate => candidate.AcceptedAtUtc)
            .ThenBy(candidate => candidate.VersionId)
            .ThenBy(candidate => candidate.ItemId)
            .Where(candidate => after is null || IsAfter(candidate, after))
            .Take(limit)
            .ToArray();
        return Task.FromResult<IReadOnlyList<AcceptedArtifactCleanupCandidate>>(items);
    }

    private static bool IsAfter(AcceptedArtifactCleanupCandidate candidate, AcceptedArtifactCleanupCursor after)
    {
        var acceptedCompare = candidate.AcceptedAtUtc.CompareTo(after.AcceptedAtUtc);
        if (acceptedCompare != 0)
        {
            return acceptedCompare > 0;
        }

        var versionCompare = candidate.VersionId.CompareTo(after.VersionId);
        if (versionCompare != 0)
        {
            return versionCompare > 0;
        }

        return candidate.ItemId.CompareTo(after.ItemId) > 0;
    }
}

public sealed class InMemoryArtifactStore : IArtifactStore
{
    private readonly Dictionary<(Guid OrganizationId, string Key), (byte[] Content, StoredArtifactReference Reference)> _objects = new();

    public Task<ArtifactPutResult> PutAsync(ArtifactPutRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.ObjectKey.BelongsToOrganization(request.OrganizationId))
        {
            return Task.FromResult(new ArtifactPutResult(false, null, ArtifactOutcomeCodes.ScopeMismatch));
        }

        var key = (request.OrganizationId, request.ObjectKey.Value);
        if (request.ConditionalCreate && _objects.ContainsKey(key))
        {
            return Task.FromResult(new ArtifactPutResult(false, null, ArtifactOutcomeCodes.AlreadyExists));
        }

        var digest = ArtifactDigest.FromHex(MaterialContentValidator.Sha256Hex(request.Content.Span));
        var reference = new StoredArtifactReference(
            request.ObjectKey,
            new ArtifactVersionId(Guid.CreateVersion7().ToString("D")),
            digest,
            request.Content.Length);
        _objects[key] = (request.Content.ToArray(), reference);
        return Task.FromResult(new ArtifactPutResult(true, reference, ArtifactOutcomeCodes.Stored));
    }

    public Task<ArtifactGetResult> GetExactVersionAsync(ArtifactGetRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Reference.ObjectKey.BelongsToOrganization(request.OrganizationId))
        {
            return Task.FromResult(new ArtifactGetResult(false, ReadOnlyMemory<byte>.Empty, ArtifactOutcomeCodes.ScopeMismatch));
        }

        if (!_objects.TryGetValue((request.OrganizationId, request.Reference.ObjectKey.Value), out var stored)
            || stored.Reference.VersionId.Value != request.Reference.VersionId.Value)
        {
            return Task.FromResult(new ArtifactGetResult(false, ReadOnlyMemory<byte>.Empty, ArtifactOutcomeCodes.NotFound));
        }

        if (!string.Equals(stored.Reference.Digest.Sha256Hex, request.Reference.Digest.Sha256Hex, StringComparison.Ordinal))
        {
            return Task.FromResult(new ArtifactGetResult(false, ReadOnlyMemory<byte>.Empty, ArtifactOutcomeCodes.DigestMismatch));
        }

        return Task.FromResult(new ArtifactGetResult(true, stored.Content, ArtifactOutcomeCodes.Stored));
    }

    public Task<ArtifactPresignResult> IssueUploadCapabilityAsync(
        ArtifactPresignRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ArtifactPresignResult(false, null, null, ArtifactOutcomeCodes.StorageUnavailable));

    public Task<ArtifactPresignResult> IssueDownloadCapabilityAsync(
        ArtifactPresignRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ArtifactPresignResult(false, null, null, ArtifactOutcomeCodes.StorageUnavailable));

    public Task<bool> DeleteAsync(Guid organizationId, StoredArtifactReference reference, CancellationToken cancellationToken = default)
    {
        if (!reference.ObjectKey.BelongsToOrganization(organizationId))
        {
            return Task.FromResult(false);
        }

        var key = (organizationId, reference.ObjectKey.Value);
        if (!_objects.TryGetValue(key, out var stored))
        {
            return Task.FromResult(false);
        }

        if (!string.IsNullOrWhiteSpace(reference.VersionId.Value)
            && stored.Reference.VersionId.Value != reference.VersionId.Value)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_objects.Remove(key));
    }
}

public sealed class InMemorySubmissionWorkStore : ISubmissionWorkStore
{
    private readonly List<SubmissionWorkItem> _items = [];

    public Task EnqueueAsync(SubmissionWorkItem work, IEnrollmentTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (_items.Any(item =>
            item.OrganizationId == work.OrganizationId
            && item.WorkKind == work.WorkKind
            && item.IntakeId == work.IntakeId
            && item.ArtifactObjectKey == work.ArtifactObjectKey
            && item.Status is SubmissionWorkStates.Pending or SubmissionWorkStates.Leased))
        {
            return Task.CompletedTask;
        }

        _items.Add(work);
        return Task.CompletedTask;
    }

    public Task<SubmissionWorkItem?> ClaimNextAsync(DateTimeOffset nowUtc, TimeSpan lease, CancellationToken cancellationToken = default)
    {
        var next = _items.FirstOrDefault(item =>
            item.Status == SubmissionWorkStates.Pending && item.AvailableAtUtc <= nowUtc
            || item.Status == SubmissionWorkStates.Leased && item.LeaseUntilUtc is DateTimeOffset until && until < nowUtc);
        if (next is null)
        {
            return Task.FromResult<SubmissionWorkItem?>(null);
        }

        var claimed = next with
        {
            Status = SubmissionWorkStates.Leased,
            AttemptCount = next.AttemptCount + 1,
            LeaseUntilUtc = nowUtc.Add(lease),
        };
        _items[_items.IndexOf(next)] = claimed;
        return Task.FromResult<SubmissionWorkItem?>(claimed);
    }

    public Task CompleteAsync(Guid organizationId, Guid workId, CancellationToken cancellationToken = default)
    {
        var index = _items.FindIndex(item => item.OrganizationId == organizationId && item.WorkId == workId);
        if (index >= 0)
        {
            _items[index] = _items[index] with { Status = SubmissionWorkStates.Completed };
        }

        return Task.CompletedTask;
    }

    public Task FailAsync(Guid organizationId, Guid workId, DateTimeOffset retryAtUtc, CancellationToken cancellationToken = default)
    {
        var index = _items.FindIndex(item => item.OrganizationId == organizationId && item.WorkId == workId);
        if (index >= 0)
        {
            _items[index] = _items[index] with
            {
                Status = SubmissionWorkStates.Pending,
                AvailableAtUtc = retryAtUtc,
                LeaseUntilUtc = null,
            };
        }

        return Task.CompletedTask;
    }

    public Task MarkTerminalFailureAsync(
        Guid organizationId,
        Guid workId,
        string failureReason,
        CancellationToken cancellationToken = default)
    {
        var index = _items.FindIndex(item => item.OrganizationId == organizationId && item.WorkId == workId);
        if (index >= 0)
        {
            _items[index] = _items[index] with
            {
                Status = SubmissionWorkStates.Failed,
                LeaseUntilUtc = null,
                FailureReason = failureReason,
            };
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<SubmissionWorkItem> Items => _items;
}

public sealed class InMemoryLifecycleHoldStore : ISubmissionLifecycleHoldStore
{
    private readonly HashSet<(Guid OrganizationId, string ArtifactObjectKey)> _holds = [];

    public Task<bool> IsHeldAsync(Guid organizationId, string artifactObjectKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(_holds.Contains((organizationId, artifactObjectKey)));

    public Task InsertHoldAsync(Guid organizationId, Guid holdId, string artifactObjectKey, CancellationToken cancellationToken = default)
    {
        _holds.Add((organizationId, artifactObjectKey));
        return Task.CompletedTask;
    }
}

public sealed class InMemoryArtifactDispositionStore : IArtifactDispositionStore
{
    public List<(Guid OrganizationId, string WorkKind, string ArtifactObjectKey)> Records { get; } = [];

    public Task RecordAsync(
        Guid organizationId,
        Guid dispositionId,
        string workKind,
        string artifactObjectKey,
        DateTimeOffset disposedAtUtc,
        CancellationToken cancellationToken = default)
    {
        Records.Add((organizationId, workKind, artifactObjectKey));
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(
        Guid organizationId,
        string artifactObjectKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Records.Any(record =>
            record.OrganizationId == organizationId
            && record.ArtifactObjectKey == artifactObjectKey));
}

public sealed class InMemoryProtectedArtifactCapabilityStore : IProtectedArtifactCapabilityStore
{
    private readonly Dictionary<(Guid OrganizationId, Guid CapabilityId), ProtectedArtifactCapability> _capabilities = [];

    public Task<ProtectedArtifactCapability> IssueAsync(
        ProtectedArtifactCapability capability,
        CancellationToken cancellationToken = default)
    {
        _capabilities[(capability.OrganizationId, capability.CapabilityId)] = capability;
        return Task.FromResult(capability);
    }

    public Task<ProtectedArtifactCapability?> FindAsync(
        Guid organizationId,
        Guid capabilityId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_capabilities.TryGetValue((organizationId, capabilityId), out var capability) ? capability : null);

    public Task MarkRedeemedAsync(
        Guid organizationId,
        Guid capabilityId,
        DateTimeOffset redeemedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (_capabilities.TryGetValue((organizationId, capabilityId), out var capability))
        {
            _capabilities[(organizationId, capabilityId)] = capability with { RedeemedAtUtc = redeemedAtUtc };
        }

        return Task.CompletedTask;
    }
}

public sealed class InMemoryAcceptedCleanupScanStore : IAcceptedCleanupScanStore
{
    private readonly object _gate = new();
    private AcceptedArtifactCleanupCursor? _cursor;
    private long _generation;

    public Task<AcceptedCleanupScanSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(new AcceptedCleanupScanSnapshot(_cursor, _generation));
        }
    }

    public Task<bool> TryAdvanceAsync(
        long expectedGeneration,
        AcceptedArtifactCleanupCursor? cursor,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_generation != expectedGeneration)
            {
                return Task.FromResult(false);
            }

            _cursor = cursor;
            _generation++;
            return Task.FromResult(true);
        }
    }
}
