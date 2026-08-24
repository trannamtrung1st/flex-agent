namespace FlexAgent.Submissions.Domain;

public sealed record SubmissionParentScope(
    Guid OrganizationId,
    Guid ActivityId,
    Guid CohortId,
    Guid BaselineId,
    Guid EnrollmentId,
    Guid ParticipantActorId,
    Guid TaskSourceId,
    Guid TaskVersionId,
    string TaskContentDigest);

public sealed record IntakeItem(
    Guid ItemId,
    string Category,
    string? Filename,
    string? DeclaredMimeType,
    long ByteCount,
    string ContentDigest,
    string? ArtifactObjectKey,
    string? ArtifactVersionId,
    DateTimeOffset? ReceivedAtUtc);

public sealed record SubmissionIntakeRecord(
    Guid IntakeId,
    Guid SubmissionId,
    SubmissionParentScope Scope,
    string Status,
    long Revision,
    string PolicyDigest,
    Guid FrozenRequirementSourceId,
    Guid FrozenRequirementVersionId,
    string FrozenRequirementDigest,
    Guid OrganizationPolicySourceId,
    Guid OrganizationPolicyVersionId,
    string OrganizationPolicyDigest,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompleteReceiptAtUtc,
    IReadOnlyList<IntakeItem> Items);

public sealed record AcceptedVersionItem(
    Guid ItemId,
    string Category,
    string? Filename,
    long ByteCount,
    string ContentDigest,
    string ArtifactObjectKey,
    string ArtifactVersionId);

public sealed record AcceptedSubmissionVersion(
    Guid SubmissionId,
    Guid VersionId,
    int VersionNumber,
    SubmissionParentScope Scope,
    string PolicyDigest,
    Guid? PredecessorVersionId,
    DateTimeOffset AcceptedAtUtc,
    IReadOnlyList<AcceptedVersionItem> Items);

public static class IntakeStateMachine
{
    private static readonly HashSet<string> TerminalStates =
    [
        IntakeStates.Cancelled,
        IntakeStates.Rejected,
        IntakeStates.Failed,
        IntakeStates.Accepted,
    ];

    public static bool IsTerminal(string status) => TerminalStates.Contains(status);

    public static bool CanTransition(string from, string to) => (from, to) switch
    {
        (IntakeStates.Receiving, IntakeStates.Received) => true,
        (IntakeStates.Receiving, IntakeStates.Cancelling) => true,
        (IntakeStates.Received, IntakeStates.Validating) => true,
        (IntakeStates.Received, IntakeStates.Cancelling) => true,
        (IntakeStates.Validating, IntakeStates.Accepted) => true,
        (IntakeStates.Validating, IntakeStates.Rejected) => true,
        (IntakeStates.Validating, IntakeStates.Failed) => true,
        (IntakeStates.Validating, IntakeStates.Cancelling) => true,
        (IntakeStates.Cancelling, IntakeStates.Cancelled) => true,
        (IntakeStates.Cancelling, IntakeStates.Reconciling) => true,
        (_, IntakeStates.Reconciling) when !IsTerminal(from) => true,
        _ => false,
    };

    public static bool ReceiptBeforeCutoff(DateTimeOffset? completeReceiptAtUtc, DateTimeOffset cutoffUtc) =>
        completeReceiptAtUtc is DateTimeOffset receipt && receipt <= cutoffUtc;
}
