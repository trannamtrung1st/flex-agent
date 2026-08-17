namespace FlexAgent.Sessions.Domain;

public static class ManifestRuntimeRecordTypes
{
    public const string ModelInvocationV1 = "model.invocation.v1";
    public const string TranscriptAppendV1 = "transcript.append.v1";
    public const string TimerEventV1 = "timer.event.v1";
}

public static class ManifestSealProcedures
{
    public const string ManifestJcsSha256V1 = "manifest-jcs-sha256-v1";
    public const string ManifestJcsSha256V2 = "manifest-jcs-sha256-v2";
    public const string LegacyUnsealed = "legacy-unsealed";
}

public static class ManifestRuntimeActors
{
    public const string SessionsRuntime = "sessions.runtime";
}

public static class TerminalReasonCategories
{
    public const string ParticipantCompleted = "participant_completed";
    public const string AuthorizedTermination = "authorized_termination";
    public const string UnrecoverableFailure = "unrecoverable_failure";
}

public static class AttemptTerminalMappings
{
    public const string Completed = "completed";
    public const string Aborted = "aborted";
}

public static class EvaluationHandoffEligibilities
{
    public const string Eligible = "eligible";
    public const string Ineligible = "ineligible";
}

public sealed class ManifestRuntimeRecord
{
    internal ManifestRuntimeRecord(
        long manifestSequence,
        string recordType,
        string serviceActor,
        DateTimeOffset occurredAt,
        ProtectedContentRef payloadRef,
        long sessionSequence)
    {
        ManifestSequence = manifestSequence;
        RecordType = recordType;
        ServiceActor = serviceActor;
        OccurredAt = occurredAt;
        PayloadRef = payloadRef;
        SessionSequence = sessionSequence;
        PendingInsert = true;
    }

    public static ManifestRuntimeRecord Rehydrate(
        long manifestSequence,
        string recordType,
        string serviceActor,
        DateTimeOffset occurredAt,
        ProtectedContentRef payloadRef,
        long sessionSequence)
    {
        return new ManifestRuntimeRecord(
            manifestSequence,
            recordType,
            serviceActor,
            occurredAt,
            payloadRef,
            sessionSequence)
        {
            PendingInsert = false,
        };
    }

    public long ManifestSequence { get; }

    public string RecordType { get; }

    public string ServiceActor { get; }

    public DateTimeOffset OccurredAt { get; }

    public ProtectedContentRef PayloadRef { get; }

    public long SessionSequence { get; }

    internal bool PendingInsert { get; private set; }

    internal void MarkPersisted() => PendingInsert = false;
}

public sealed record SessionTerminalRecord(
    Guid TerminalRecordId,
    SessionLifecycleState LifecycleState,
    string? ReasonCategory,
    string? AttemptMapping,
    long? CutoffSequence,
    string ProcedureId,
    string? SealDigest);

public sealed record EvaluationHandoff(
    string HandoffId,
    Guid TerminalRecordId,
    string ProcedureId,
    string Eligibility,
    SessionLifecycleState TerminalState,
    long? CutoffSequence,
    string ConfigurationId,
    string ConfigurationDigest,
    string ManifestId,
    string SealDigest);

public sealed record ManifestSealRuntimeRecord(long Sequence, string RecordType, string PayloadDigest);

public sealed record ManifestSealDocument(
    string ProcedureId,
    string SchemaVersion,
    string CanonicalizationVersion,
    string ManifestSchemaVersion,
    string ConfigurationId,
    string ConfigurationDigest,
    IReadOnlyList<ManifestSealRuntimeRecord> RuntimeRecords,
    string TerminalState,
    string TerminalReason,
    string OrganizationId,
    string ActivityId,
    string ParticipantId,
    string AttemptId,
    string SessionId,
    long? CutoffSequence = null);
