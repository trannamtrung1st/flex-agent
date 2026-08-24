using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public interface IFrozenSubmissionRequirementPort
{
    Task<NormalizedMaterialPolicy?> ResolveFrozenAsync(
        Guid organizationId,
        Guid taskSourceId,
        Guid taskVersionId,
        string taskContentDigest,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);
}

public interface IMaterialPolicyPort
{
    Task<NormalizedMaterialPolicy?> ResolveCurrentAsync(
        Guid organizationId,
        PolicySourceRef frozenOrganizationPolicyRef,
        DateTimeOffset nowUtc,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);
}

public sealed record BeginIntakeCommand(
    EnrollmentActorContext Actor,
    Guid EnrollmentId,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record CompleteIntakeItemCommand(
    EnrollmentActorContext Actor,
    Guid EnrollmentId,
    Guid IntakeId,
    Guid ItemId,
    string Category,
    string? Filename,
    string? DeclaredMimeType,
    long ExpectedByteCount,
    string ContentDigest,
    long ExpectedRevision,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record CancelIntakeCommand(
    EnrollmentActorContext Actor,
    Guid EnrollmentId,
    Guid IntakeId,
    long ExpectedRevision,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record FinalizeIntakeCommand(
    EnrollmentActorContext Actor,
    Guid EnrollmentId,
    Guid IntakeId,
    long ExpectedRevision,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record IntakeMutationOutcome(
    bool Succeeded,
    string OutcomeCode,
    Guid? IntakeId,
    Guid? SubmissionId,
    string? Status,
    long? Revision,
    Guid? VersionId,
    int? VersionNumber,
    IReadOnlyList<string> PermittedActions);

public sealed record SubmissionIntakeProjection(
    Guid IntakeId,
    Guid SubmissionId,
    string Status,
    long Revision,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompleteReceiptAtUtc,
    IReadOnlyList<SubmissionIntakeItemProjection> Items,
    NormalizedMaterialPolicy? EffectivePolicy,
    IReadOnlyList<string> PermittedActions);

public sealed record SubmissionIntakeItemProjection(
    Guid ItemId,
    string Category,
    string? Filename,
    long ByteCount,
    string? ReceiptState);

public sealed record AcceptedVersionSummary(
    Guid VersionId,
    int VersionNumber,
    DateTimeOffset AcceptedAtUtc,
    int ItemCount);

public sealed record AcceptedVersionDetail(
    AcceptedVersionSummary Summary,
    IReadOnlyList<AcceptedVersionItemProjection> Items,
    IReadOnlyList<string> PermittedActions);

public sealed record AcceptedVersionItemProjection(
    Guid ItemId,
    string Category,
    string? Filename,
    long ByteCount,
    bool PreviewAuthorized,
    bool DownloadAuthorized);

public sealed record MyWorkSubmissionProjection(
    Guid EnrollmentId,
    string EnrollmentStatus,
    bool IntakeAvailable,
    string? UnavailableReason,
    NormalizedMaterialPolicy? Requirements,
    SubmissionIntakeProjection? ActiveIntake,
    IReadOnlyList<AcceptedVersionSummary> VersionHistory,
    IReadOnlyList<string> PermittedActions);

public interface IIntakeCoordinator
{
    Task<IntakeMutationOutcome> BeginAsync(BeginIntakeCommand command, CancellationToken cancellationToken = default);

    Task<IntakeMutationOutcome> CompleteItemAsync(CompleteIntakeItemCommand command, CancellationToken cancellationToken = default);

    Task<IntakeMutationOutcome> CancelAsync(CancelIntakeCommand command, CancellationToken cancellationToken = default);

    Task<IntakeMutationOutcome> FinalizeAsync(FinalizeIntakeCommand command, CancellationToken cancellationToken = default);
}

public interface ISubmissionQueryService
{
    Task<QueryResult<MyWorkSubmissionProjection>> GetMyWorkSubmissionAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        CancellationToken cancellationToken = default);

    Task<QueryResult<AcceptedVersionDetail>> GetAcceptedVersionAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        Guid versionId,
        CancellationToken cancellationToken = default);
}

public sealed record QueryResult<T>(bool Found, T? Value, string? OutcomeCode);

public interface IIntakeStore
{
    Task<SubmissionIntakeRecord?> FindIntakeAsync(
        Guid organizationId,
        Guid intakeId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);

    Task<SubmissionIntakeRecord?> FindActiveIntakeAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);

    Task InsertIntakeAsync(
        SubmissionIntakeRecord intake,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task UpdateIntakeAsync(
        SubmissionIntakeRecord intake,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);
}

public interface ISubmissionVersionStore
{
    Task<Guid?> FindSubmissionIdByEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcceptedVersionSummary>> ListVersionsAsync(
        Guid organizationId,
        Guid submissionId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);

    Task<AcceptedSubmissionVersion?> FindVersionAsync(
        Guid organizationId,
        Guid versionId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);

    Task<int> AllocateVersionNumberAsync(
        Guid organizationId,
        Guid submissionId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task InsertAcceptedVersionAsync(
        AcceptedSubmissionVersion version,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);
}

public interface IExactAcceptedVersionReader
{
    Task<AcceptedSubmissionVersion?> GetExactAsync(
        SubmissionParentScope scope,
        Guid versionId,
        object commitTransaction,
        CancellationToken cancellationToken = default);
}
