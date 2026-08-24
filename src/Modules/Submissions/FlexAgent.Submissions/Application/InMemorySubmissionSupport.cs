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
        Guid intakeId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_intakes.TryGetValue((organizationId, intakeId), out var intake) ? intake : null);

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

    public Task<int> AllocateVersionNumberAsync(
        Guid organizationId,
        Guid submissionId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var next = _versions.Values
            .Where(version => version.Scope.OrganizationId == organizationId && version.SubmissionId == submissionId)
            .Select(version => version.VersionNumber)
            .DefaultIfEmpty(0)
            .Max() + 1;
        return Task.FromResult(next);
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
}
