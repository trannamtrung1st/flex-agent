namespace FlexAgent.Sessions.Domain;

public sealed partial class SessionRuntime
{
    private readonly List<ManifestRuntimeRecord> _manifestRuntimeRecords = [];

    public IReadOnlyList<ManifestRuntimeRecord> ManifestRuntimeRecords => _manifestRuntimeRecords;

    public SessionTerminalRecord? TerminalRecord { get; private set; }

    public EvaluationHandoff? EvaluationHandoff { get; private set; }

    public bool VerifyTerminalSeal()
    {
        if (TerminalRecord is null
            || string.Equals(
                TerminalRecord.ProcedureId,
                ManifestSealProcedures.LegacyUnsealed,
                StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(TerminalRecord.SealDigest)
            || (string.Equals(
                    TerminalRecord.ProcedureId,
                    ManifestSealProcedures.ManifestJcsSha256V2,
                    StringComparison.Ordinal)
                && TerminalRecord.CutoffSequence is null))
        {
            return false;
        }

        return ManifestTerminalSealComputer.Verify(
            BuildSealDocument(TerminalRecord),
            TerminalRecord.SealDigest!);
    }

    internal IEnumerable<ManifestRuntimeRecord> PendingManifestRecords =>
        _manifestRuntimeRecords.Where(record => record.PendingInsert);

    internal bool TerminalRecordPendingInsert { get; private set; }

    internal bool EvaluationHandoffPendingInsert { get; private set; }

    internal void ReplaceManifestRecordForVerification(int index, ManifestRuntimeRecord record)
    {
        _manifestRuntimeRecords[index] = record;
    }

    internal void ReplaceTerminalCutoffForVerification(long cutoffSequence)
    {
        if (TerminalRecord is null)
        {
            throw new InvalidOperationException("A terminal record is required to replace the cutoff.");
        }

        TerminalRecord = TerminalRecord with { CutoffSequence = cutoffSequence };
    }

    internal void MarkTerminalArtifactsPersisted()
    {
        foreach (var record in _manifestRuntimeRecords)
        {
            record.MarkPersisted();
        }

        TerminalRecordPendingInsert = false;
        EvaluationHandoffPendingInsert = false;
    }

    private void AppendManifestRecord(
        string recordType,
        string protectedRef,
        string digestSeed,
        DateTimeOffset occurredAt)
    {
        if (_manifestRuntimeRecords.Any(record =>
                string.Equals(record.RecordType, recordType, StringComparison.Ordinal)
                && string.Equals(record.PayloadRef.ProtectedRef, protectedRef, StringComparison.Ordinal)))
        {
            return;
        }

        _manifestRuntimeRecords.Add(
            new ManifestRuntimeRecord(
                _manifestRuntimeRecords.Count + 1,
                recordType,
                ManifestRuntimeActors.SessionsRuntime,
                occurredAt,
                new ProtectedContentRef(protectedRef, ProtectedContentRef.DigestUtf8(digestSeed)),
                SessionSequence));
    }

    private void AppendInvocationAdmission(AgentInvocation invocation, DateTimeOffset occurredAt) =>
        AppendManifestRecord(
            ManifestRuntimeRecordTypes.ModelInvocationV1,
            invocation.AgentInvocationId,
            $"{invocation.Trigger.TriggerType}:{invocation.Trigger.Purpose}:admitted",
            occurredAt);

    private void AppendInvocationOutcome(AgentInvocation invocation, DateTimeOffset occurredAt)
    {
        var outcome = invocation.Decision is not null
            ? $"decided:{invocation.Decision.DecisionType}"
            : $"failed:{invocation.ExecutionOutcome?.OutcomeCategory ?? "unknown"}";
        AppendManifestRecord(
            ManifestRuntimeRecordTypes.ModelInvocationV1,
            $"{invocation.AgentInvocationId}.outcome",
            outcome,
            occurredAt);
    }

    private void AppendTranscript(string messageId, string authorType, DateTimeOffset occurredAt) =>
        AppendManifestRecord(
            ManifestRuntimeRecordTypes.TranscriptAppendV1,
            messageId,
            $"{authorType}:{messageId}",
            occurredAt);

    private void AppendTimerEvent(TimerScheduleRevision revision, string qualifier, DateTimeOffset occurredAt) =>
        AppendManifestRecord(
            ManifestRuntimeRecordTypes.TimerEventV1,
            qualifier.Length == 0 ? revision.ScheduleRevisionId : $"{revision.ScheduleRevisionId}.{qualifier}",
            $"{revision.RequestedByCategory}:{revision.LaneState}:{qualifier}",
            occurredAt);

    private void CommitTerminal(
        SessionLifecycleState terminalState,
        string reasonCategory,
        string attemptMapping,
        DateTimeOffset authoritativeUtc)
    {
        if (TerminalRecord is not null)
        {
            return;
        }

        if (CutoffSequence is null)
        {
            throw new InvalidOperationException("Terminal seal requires a cutoff sequence.");
        }

        var sealDigest = ManifestTerminalSealComputer.ComputeDigest(
            BuildSealDocument(
                ManifestSealProcedures.ManifestJcsSha256V2,
                "v2",
                terminalState,
                reasonCategory,
                CutoffSequence));
        var terminalRecordId = Guid.NewGuid();
        LifecycleState = terminalState;
        TerminalRecord = new SessionTerminalRecord(
            terminalRecordId,
            terminalState,
            reasonCategory,
            attemptMapping,
            CutoffSequence,
            ManifestSealProcedures.ManifestJcsSha256V2,
            sealDigest);
        EvaluationHandoff = new EvaluationHandoff(
            $"eho.{terminalRecordId.ToString("N").ToLowerInvariant()}",
            terminalState == SessionLifecycleState.Completed
                ? EvaluationHandoffEligibilities.Eligible
                : EvaluationHandoffEligibilities.Ineligible,
            terminalState,
            CutoffSequence,
            Binding.ConfigurationId,
            Binding.ConfigurationDigest,
            Binding.ManifestId,
            sealDigest);
        TerminalRecordPendingInsert = true;
        EvaluationHandoffPendingInsert = true;
        Touch(authoritativeUtc);
    }

    private ManifestSealDocument BuildSealDocument(SessionTerminalRecord terminal) =>
        BuildSealDocument(
            terminal.ProcedureId,
            string.Equals(terminal.ProcedureId, ManifestSealProcedures.ManifestJcsSha256V2, StringComparison.Ordinal)
                ? "v2"
                : "v1",
            terminal.LifecycleState,
            terminal.ReasonCategory ?? string.Empty,
            terminal.CutoffSequence);

    private ManifestSealDocument BuildSealDocument(
        string procedureId,
        string schemaVersion,
        SessionLifecycleState terminalState,
        string reasonCategory,
        long? cutoffSequence) =>
        new(
            procedureId,
            schemaVersion,
            "rfc8785",
            "v1",
            Binding.ConfigurationId,
            Binding.ConfigurationDigest,
            _manifestRuntimeRecords
                .Select(record => new ManifestSealRuntimeRecord(
                    record.ManifestSequence,
                    record.RecordType,
                    record.PayloadRef.ContentDigest))
                .ToArray(),
            ToSealLifecycle(terminalState),
            reasonCategory,
            SessionOwnershipStableIds.Organization(Ownership.OrganizationId),
            SessionOwnershipStableIds.Activity(Ownership.ActivityId),
            SessionOwnershipStableIds.Participant(Ownership.ParticipantId),
            SessionOwnershipStableIds.Attempt(Ownership.AttemptId),
            SessionOwnershipStableIds.Session(Ownership.SessionId),
            cutoffSequence);

    private static string ToSealLifecycle(SessionLifecycleState state) => state switch
    {
        SessionLifecycleState.Completed => "completed",
        SessionLifecycleState.Terminated => "terminated",
        SessionLifecycleState.Aborted => "aborted",
        _ => throw new InvalidOperationException("Seal documents cover only terminal Session states."),
    };
}
