using System.Globalization;

namespace FlexAgent.Sessions.Domain;

public static class HostedSessionProjectionKinds
{
    public const string Participant = "participant";
    public const string Administrator = "administrator";
    public const string Historical = "historical";
}

public static class HostedSessionPermittedActions
{
    public const string SendMessage = "send_message";
    public const string CompleteSession = "complete_session";
    public const string Reconcile = "reconcile";
    public const string PauseSession = "pause_session";
    public const string ResumeSession = "resume_session";
    public const string TerminateSession = "terminate_session";
    public const string ViewTranscript = "view_transcript";
    public const string ReturnToMyWork = "return_to_my_work";
}

public static class HostedSessionEventTypes
{
    public const string LifecycleChanged = "session.hosted.lifecycle.changed.v1";
    public const string TimingUpdated = "session.hosted.timing.updated.v1";
    public const string WarningIssued = "session.hosted.warning.issued.v1";
    public const string MessageAccepted = "session.hosted.message.accepted.v1";
    public const string AgentWork = "session.hosted.agent.work.v1";
    public const string AgentNoAction = "session.hosted.agent.no_action.v1";
    public const string AgentFragment = "session.hosted.agent.fragment.v1";
    public const string AgentComplete = "session.hosted.agent.complete.v1";
    public const string Terminal = "session.hosted.terminal.v1";
    public const string AccessChanged = "session.hosted.access.changed.v1";
    public const string ReconcileRequired = "session.hosted.reconcile.required.v1";
}

public sealed record HostedSessionEventProjectionOptions(
    DateTimeOffset? SessionStartedAt = null,
    HostedFrozenTimingPolicy? TimingPolicy = null,
    DateTimeOffset? AuthoritativeUtc = null,
    IReadOnlyList<HostedSessionWarningOccurrence>? WarningOccurrences = null);

public sealed record HostedSessionWarningOccurrence(
    string WarningThresholdId,
    string WarningCode,
    int RemainingSecondsThreshold,
    DateTimeOffset DueAt,
    DateTimeOffset CommittedAt,
    long SessionSequence,
    int RemainingSecondsAtCommit,
    string DeliveryStatus);

public sealed record HostedTranscriptItem(
    string ItemId,
    string Author,
    string Status,
    string SequenceStart,
    string SequenceEnd,
    string? Content,
    string? OccurredAt,
    string? TurnId);

public sealed record HostedSessionSnapshot(
    string ProjectionKind,
    Guid SessionId,
    string LifecycleState,
    long SessionVersion,
    long SessionSequence,
    DateTimeOffset AuthoritativeObservedAt,
    IReadOnlyList<string> PermittedActions,
    string RecoveryCategory,
    long? CutoffSequence,
    string? AgentDisplayName,
    int BoundSubmissionCount,
    IReadOnlyList<HostedTranscriptItem> Transcript,
    bool OlderAvailable,
    string ActivityWorkState,
    string? ActivityTurnId,
    string TimingPolicy = "disabled",
    int? RemainingSeconds = null,
    string? WarningCode = "none",
    string? PauseStartedAt = null,
    int? TimingBudgetSeconds = null,
    string LastConfirmedStreamCursor = "0");

public static class SessionPermittedActionsProjector
{
    public static IReadOnlyList<string> Project(
        string projectionKind,
        SessionLifecycleState lifecycle,
        int? remainingSeconds = null,
        string? timingPolicy = null)
    {
        return projectionKind switch
        {
            HostedSessionProjectionKinds.Participant => Participant(lifecycle, remainingSeconds, timingPolicy),
            HostedSessionProjectionKinds.Administrator => Administrator(lifecycle, timingPolicy),
            HostedSessionProjectionKinds.Historical => Historical(lifecycle),
            _ => [],
        };
    }

    private static IReadOnlyList<string> Participant(
        SessionLifecycleState lifecycle,
        int? remainingSeconds,
        string? timingPolicy)
    {
        if (HostedSessionTimingAuthority.IsUnavailable(timingPolicy))
        {
            return lifecycle switch
            {
                SessionLifecycleState.Active or SessionLifecycleState.Paused or SessionLifecycleState.Completing =>
                [
                    HostedSessionPermittedActions.Reconcile,
                    HostedSessionPermittedActions.ReturnToMyWork,
                ],
                _ =>
                [
                    HostedSessionPermittedActions.ViewTranscript,
                    HostedSessionPermittedActions.ReturnToMyWork,
                ],
            };
        }

        if (lifecycle == SessionLifecycleState.Active && remainingSeconds == 0)
        {
            return
            [
                HostedSessionPermittedActions.Reconcile,
                HostedSessionPermittedActions.ReturnToMyWork,
            ];
        }

        return lifecycle switch
        {
            SessionLifecycleState.Active =>
            [
                HostedSessionPermittedActions.SendMessage,
                HostedSessionPermittedActions.CompleteSession,
                HostedSessionPermittedActions.Reconcile,
                HostedSessionPermittedActions.ReturnToMyWork,
            ],
            SessionLifecycleState.Paused or SessionLifecycleState.Ready =>
            [
                HostedSessionPermittedActions.Reconcile,
                HostedSessionPermittedActions.ReturnToMyWork,
            ],
            SessionLifecycleState.Completing =>
            [
                HostedSessionPermittedActions.CompleteSession,
                HostedSessionPermittedActions.Reconcile,
                HostedSessionPermittedActions.ReturnToMyWork,
            ],
            _ =>
            [
                HostedSessionPermittedActions.ViewTranscript,
                HostedSessionPermittedActions.ReturnToMyWork,
            ],
        };
    }

    private static IReadOnlyList<string> Administrator(SessionLifecycleState lifecycle, string? timingPolicy) =>
        HostedSessionTimingAuthority.IsUnavailable(timingPolicy)
            ? lifecycle switch
            {
                SessionLifecycleState.Active or SessionLifecycleState.Paused or SessionLifecycleState.Completing =>
                    [HostedSessionPermittedActions.TerminateSession],
                _ => [],
            }
            : lifecycle switch
        {
            SessionLifecycleState.Active =>
            [
                HostedSessionPermittedActions.PauseSession,
                HostedSessionPermittedActions.TerminateSession,
            ],
            SessionLifecycleState.Paused =>
            [
                HostedSessionPermittedActions.ResumeSession,
                HostedSessionPermittedActions.TerminateSession,
            ],
            SessionLifecycleState.Completing => [HostedSessionPermittedActions.TerminateSession],
            _ => [],
        };

    private static IReadOnlyList<string> Historical(SessionLifecycleState lifecycle) =>
        IsTerminal(lifecycle)
            ? [HostedSessionPermittedActions.ViewTranscript, HostedSessionPermittedActions.ReturnToMyWork]
            : [];

    public static bool IsTerminal(SessionLifecycleState lifecycle) =>
        lifecycle is SessionLifecycleState.Completed
            or SessionLifecycleState.Terminated
            or SessionLifecycleState.Aborted;
}

public static class HostedSessionSnapshotProjector
{
    public static HostedSessionSnapshot Project(
        SessionRuntime session,
        string projectionKind,
        DateTimeOffset authoritativeUtc,
        DateTimeOffset? startedAt = null,
        HostedFrozenTimingPolicy? timingPolicy = null,
        IReadOnlyList<HostedSessionWarningOccurrence>? warningOccurrences = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        var includeTranscript = projectionKind switch
        {
            HostedSessionProjectionKinds.Participant => true,
            HostedSessionProjectionKinds.Historical =>
                SessionPermittedActionsProjector.IsTerminal(session.LifecycleState),
            _ => false,
        };
        var transcript = includeTranscript ? ProjectTranscript(session) : [];
        var recovery = projectionKind == HostedSessionProjectionKinds.Historical
            && !SessionPermittedActionsProjector.IsTerminal(session.LifecycleState)
            ? "unavailable"
            : "none";
        var lastTurn = session.Turns.LastOrDefault();
        var lastInvocation = lastTurn is null
            ? null
            : session.Invocations.LastOrDefault(invocation =>
                string.Equals(invocation.Trigger.TurnId, lastTurn.TurnId, StringComparison.Ordinal));
        var workState = lastInvocation?.Status == AgentInvocationStatuses.ExecutionFailed
            ? "failed"
            : lastTurn is null
            ? "idle"
            : lastTurn.ResponseSlot.State == ResponseSlotStates.IntentionalNoAction
            ? "no_action"
            : lastTurn.State == TurnStates.Complete
            ? "idle"
            : lastTurn.ResponseSlot.State switch
            {
                ResponseSlotStates.Cancelled => "failed",
                ResponseSlotStates.ClaimedForPublication => "working",
                _ => lastTurn.State switch
                {
                    TurnStates.WorkQueued => "queued",
                    TurnStates.Accepted => "working",
                    TurnStates.Cancelled => "failed",
                    _ => "idle",
                },
            };

        var timing = HostedSessionTiming.Project(
            session.LifecycleState,
            startedAt ?? session.LastCommittedAt,
            session.LastCommittedAt,
            authoritativeUtc,
            timingPolicy ?? HostedFrozenTimingPolicy.UnavailablePolicy,
            session.AccumulatedPausedSeconds,
            session.OpenPauseStartedAt);
        return new HostedSessionSnapshot(
            projectionKind,
            session.Ownership.SessionId,
            MapLifecycle(session.LifecycleState),
            session.SessionVersion,
            session.SessionSequence,
            authoritativeUtc,
            SessionPermittedActionsProjector.Project(
                projectionKind,
                session.LifecycleState,
                timing.RemainingSeconds,
                timing.Policy),
            recovery,
            session.CutoffSequence,
            includeTranscript ? "Assessment Agent" : null,
            includeTranscript ? session.Binding.PermittedSubmissionRefs.Count : 0,
            transcript,
            false,
            projectionKind == HostedSessionProjectionKinds.Historical ? "idle" : workState,
            lastTurn is null || projectionKind == HostedSessionProjectionKinds.Historical
                ? null
                : ToStableId(lastTurn.TurnId, "turn"),
            includeTranscript ? timing.Policy : "disabled",
            includeTranscript ? timing.RemainingSeconds : null,
            includeTranscript ? timing.WarningCode : "none",
            includeTranscript ? timing.PauseStartedAt : null,
            includeTranscript ? timing.BudgetSeconds : null,
            HostedSessionEventProjector.CurrentProjectedStreamCursor(
                session,
                new HostedSessionEventProjectionOptions(
                    startedAt,
                    timingPolicy,
                    authoritativeUtc,
                    warningOccurrences)));
    }

    public static string MapLifecycle(SessionLifecycleState state) =>
        state switch
        {
            SessionLifecycleState.Ready => "ready",
            SessionLifecycleState.Active => "active",
            SessionLifecycleState.Paused => "paused",
            SessionLifecycleState.Completing => "completing",
            SessionLifecycleState.Completed => "completed",
            SessionLifecycleState.Terminated => "terminated",
            SessionLifecycleState.Aborted => "aborted",
            _ => "active",
        };

    private static IReadOnlyList<HostedTranscriptItem> ProjectTranscript(SessionRuntime session)
    {
        var items = new List<HostedTranscriptItem>();
        var coveredAgentMessageIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in session.VisibleTranscript)
        {
            if (item.AuthorType == TranscriptAuthorTypes.Participant)
            {
                var append = FindTranscriptAppendRecord(session, item.MessageId);
                var sequence = append?.SessionSequence ?? 1;
                items.Add(new HostedTranscriptItem(
                    ToStableId(item.MessageId, "msg"),
                    "participant",
                    item.ExactUtf8Text is null ? "unavailable" : "accepted",
                    sequence.ToString(CultureInfo.InvariantCulture),
                    sequence.ToString(CultureInfo.InvariantCulture),
                    item.ExactUtf8Text,
                    append is null ? null : FormatUtc(append.OccurredAt),
                    string.IsNullOrWhiteSpace(item.TurnId) ? null : ToStableId(item.TurnId, "turn")));
                continue;
            }

            var message = FindAgentMessage(session, item.MessageId);
            if (message is not null)
            {
                coveredAgentMessageIds.Add(message.MessageId);
                items.Add(ProjectAgentTranscriptItem(message, ToStableId(item.MessageId, "msg")));
                continue;
            }

            var content = item.ExactUtf8Text;
            items.Add(new HostedTranscriptItem(
                ToStableId(item.MessageId, "msg"),
                "agent",
                content is null ? "unavailable" : "accepted",
                "1",
                "1",
                content,
                null,
                string.IsNullOrWhiteSpace(item.TurnId) ? null : ToStableId(item.TurnId, "turn")));
        }

        foreach (var message in session.AgentMessages)
        {
            if (coveredAgentMessageIds.Contains(message.MessageId))
            {
                continue;
            }

            items.Add(ProjectAgentTranscriptItem(message, ToStableId(message.MessageId, "amsg")));
        }

        return items;
    }

    private static ManifestRuntimeRecord? FindTranscriptAppendRecord(SessionRuntime session, string messageId) =>
        session.ManifestRuntimeRecords.FirstOrDefault(record =>
            string.Equals(record.RecordType, ManifestRuntimeRecordTypes.TranscriptAppendV1, StringComparison.Ordinal)
            && string.Equals(record.PayloadRef.ProtectedRef, messageId, StringComparison.Ordinal));

    private static AgentResponseMessage? FindAgentMessage(SessionRuntime session, string messageId) =>
        session.AgentMessages.FirstOrDefault(message =>
            string.Equals(message.MessageId, messageId, StringComparison.Ordinal));

    private static HostedTranscriptItem ProjectAgentTranscriptItem(AgentResponseMessage message, string itemId)
    {
        var assembled = message.AssembleExactText();
        var start = message.Fragments.Count == 0
            ? Math.Max(1, message.SealedSessionSequence ?? 1)
            : message.Fragments[0].SessionSequence;
        var end = message.SealedSessionSequence ?? message.Fragments.LastOrDefault()?.SessionSequence ?? start;
        var occurredAtSource = message.Fragments.Count > 0
            ? message.Fragments[0].CommittedAt
            : message.SealedAt;

        return new HostedTranscriptItem(
            itemId,
            "agent",
            MapAgentTranscriptStatus(message),
            Math.Max(1, start).ToString(CultureInfo.InvariantCulture),
            Math.Max(1, end).ToString(CultureInfo.InvariantCulture),
            string.IsNullOrEmpty(assembled) ? null : assembled,
            occurredAtSource is null ? null : FormatUtc(occurredAtSource.Value),
            string.IsNullOrWhiteSpace(message.TurnId) ? null : ToStableId(message.TurnId, "turn"));
    }

    private static string MapAgentTranscriptStatus(AgentResponseMessage message) =>
        message.CompletionState switch
        {
            AgentMessageCompletionStates.Complete => "complete",
            AgentMessageCompletionStates.Incomplete => "incomplete",
            AgentMessageCompletionStates.Cancelled => "cancelled",
            _ => message.Fragments.Count > 0 ? "streaming" : "streaming",
        };

    public static string ToStableId(string value, string prefix)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && value.Length >= 8
            && value[0] is >= 'a' and <= 'z')
        {
            return value;
        }

        var compact = new string((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        if (compact.Length < 8)
        {
            compact = (compact + "synthetic").PadRight(8, '0');
        }

        return $"{prefix}.{compact[..Math.Min(32, compact.Length)]}";
    }

    public static string FormatUtc(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
}

public static class HostedSessionEventProjector
{
    public static AuthorizedSessionEventReplayResult Project(
        SessionRuntime session,
        long afterCursor,
        HostedSessionEventProjectionOptions? projectionOptions = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!TryBuildHostedEvents(session, afterCursor, projectionOptions, out var events, out var baselineOutcome))
        {
            return new AuthorizedSessionEventReplayResult(false, baselineOutcome, []);
        }

        var pageSize = AuthorizedSessionEventProjector.ReplayPageSize(session);
        var hasMore = events.Count > pageSize;
        if (hasMore)
        {
            events = events.Take(pageSize).ToList();
        }

        return baselineOutcome == SessionEventReplayOutcomeCodes.Succeeded
            ? new AuthorizedSessionEventReplayResult(true, SessionEventReplayOutcomeCodes.Succeeded, events, hasMore)
            : new AuthorizedSessionEventReplayResult(false, baselineOutcome, []);
    }

    public static string CurrentProjectedStreamCursor(
        SessionRuntime session,
        HostedSessionEventProjectionOptions? projectionOptions = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!TryBuildHostedEvents(session, afterCursor: 0, projectionOptions, out var events, out _))
        {
            return "0";
        }

        return events.Count == 0
            ? "0"
            : events.Max(evt => HostedStreamCursors.Parse(evt.StreamCursor)).ToString(CultureInfo.InvariantCulture);
    }

    public static bool IsIssuedStreamCursor(
        SessionRuntime session,
        long cursor,
        HostedSessionEventProjectionOptions? projectionOptions = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        return EnumerateIssuedStreamCursors(session, projectionOptions).Contains(cursor);
    }

    private static bool TryBuildHostedEvents(
        SessionRuntime session,
        long afterCursor,
        HostedSessionEventProjectionOptions? projectionOptions,
        out List<AuthorizedSessionProjectionEvent> events,
        out string baselineOutcome)
    {
        events = [];
        baselineOutcome = SessionEventReplayOutcomeCodes.Succeeded;
        if (!AuthorizedSessionEventProjector.TryBuildAgentEvents(session, afterSequence: 0, out var agentEvents, out baselineOutcome))
        {
            events = [];
            return false;
        }

        var sessionId = session.Ownership.SessionId.ToString("D");
        AppendCommittedHostedEvents(session, sessionId, events, projectionOptions);
        AppendWarningOccurrences(session, sessionId, events, projectionOptions);

        foreach (var evt in agentEvents)
        {
            var hostedType = evt.EventType switch
            {
                AuthorizedSessionEventTypes.AgentFragment => HostedSessionEventTypes.AgentFragment,
                AuthorizedSessionEventTypes.AgentComplete => HostedSessionEventTypes.AgentComplete,
                _ => null,
            };
            if (hostedType is null)
            {
                continue;
            }

            var sequence = long.Parse(evt.SessionSequence, CultureInfo.InvariantCulture);
            var slot = hostedType == HostedSessionEventTypes.AgentComplete
                ? HostedStreamCursors.SlotComplete
                : HostedStreamCursors.SlotFragment;
            events.Add(evt with
            {
                EventType = hostedType,
                SessionVersion = session.SessionVersion,
                WorkState = hostedType == HostedSessionEventTypes.AgentComplete
                    ? "idle"
                    : hostedType == HostedSessionEventTypes.AgentFragment
                    ? "working"
                    : evt.WorkState,
                StreamCursor = HostedStreamCursors.Wire(sequence, slot),
            });
        }

        if (session.CutoffSequence is { } cutoff
            && SessionPermittedActionsProjector.IsTerminal(session.LifecycleState))
        {
            events.Add(new AuthorizedSessionProjectionEvent(
                HostedSessionEventTypes.Terminal,
                sessionId,
                cutoff.ToString(CultureInfo.InvariantCulture),
                HostedSessionSnapshotProjector.FormatUtc(session.LastCommittedAt),
                "Session reached a terminal cutoff.",
                CutoffSequence: cutoff.ToString(CultureInfo.InvariantCulture),
                LifecycleState: HostedSessionSnapshotProjector.MapLifecycle(session.LifecycleState),
                SessionVersion: session.SessionVersion,
                StreamCursor: HostedStreamCursors.Wire(cutoff, HostedStreamCursors.SlotTerminal)));
        }

        events.Sort(static (left, right) =>
            HostedStreamCursors.Parse(left.StreamCursor)
                .CompareTo(HostedStreamCursors.Parse(right.StreamCursor)));
        events = events
            .Where(evt => HostedStreamCursors.Parse(evt.StreamCursor) > afterCursor)
            .ToList();

        return true;
    }

    private static IReadOnlySet<long> EnumerateIssuedStreamCursors(
        SessionRuntime session,
        HostedSessionEventProjectionOptions? projectionOptions)
    {
        var issued = new HashSet<long>();
        foreach (var record in session.ManifestRuntimeRecords)
        {
            if (string.Equals(record.RecordType, ManifestRuntimeRecordTypes.TranscriptAppendV1, StringComparison.Ordinal))
            {
                issued.Add(HostedStreamCursors.Encode(record.SessionSequence, HostedStreamCursors.SlotAccepted));
                continue;
            }

            if (string.Equals(record.RecordType, ManifestRuntimeRecordTypes.ModelInvocationV1, StringComparison.Ordinal)
                && !record.PayloadRef.ProtectedRef.EndsWith(".outcome", StringComparison.Ordinal))
            {
                issued.Add(HostedStreamCursors.Encode(record.SessionSequence, HostedStreamCursors.SlotQueued));
            }
        }

        foreach (var invocation in session.Invocations)
        {
            if (!IsParticipantInvocation(invocation.Trigger))
            {
                continue;
            }

            if (string.Equals(invocation.Status, AgentInvocationStatuses.ExecutionFailed, StringComparison.Ordinal))
            {
                issued.Add(HostedStreamCursors.Encode(invocation.SessionSequence, HostedStreamCursors.SlotFailed));
            }

            if (invocation.ValidationEffect?.EffectCommitSessionSequence is not { } effectSequence)
            {
                continue;
            }

            if (IsIntentionalNoAction(invocation))
            {
                issued.Add(HostedStreamCursors.Encode(effectSequence, HostedStreamCursors.SlotNoAction));
                continue;
            }

            if (invocation.ValidationEffect.EffectOutcome == DecisionEffectOutcomes.Applied)
            {
                issued.Add(HostedStreamCursors.Encode(effectSequence, HostedStreamCursors.SlotWorking));
            }
        }

        foreach (var message in session.AgentMessages)
        {
            foreach (var fragment in message.Fragments)
            {
                issued.Add(HostedStreamCursors.Encode(fragment.SessionSequence, HostedStreamCursors.SlotFragment));
            }

            if (message.IsTerminal && message.SealedSessionSequence is { } sealSequence)
            {
                issued.Add(HostedStreamCursors.Encode(sealSequence, HostedStreamCursors.SlotComplete));
            }
        }

        if (session.CutoffSequence is { } cutoff
            && SessionPermittedActionsProjector.IsTerminal(session.LifecycleState))
        {
            issued.Add(HostedStreamCursors.Encode(cutoff, HostedStreamCursors.SlotTerminal));
        }

        if (session.LifecycleState == SessionLifecycleState.Completing && session.CutoffSequence is { } completingCutoff)
        {
            issued.Add(HostedStreamCursors.Encode(
                ResolveHostedSessionSequence(session, completingCutoff),
                HostedStreamCursors.SlotLifecycle));
        }

        foreach (var record in session.ManifestRuntimeRecords)
        {
            if (!string.Equals(record.RecordType, ManifestRuntimeRecordTypes.TimerEventV1, StringComparison.Ordinal)
                || !TryGetTimerLifecycleQualifier(record.PayloadRef.ProtectedRef, out _))
            {
                continue;
            }

            issued.Add(HostedStreamCursors.Encode(ResolveHostedSessionSequence(session, record.SessionSequence), HostedStreamCursors.SlotLifecycle));
            issued.Add(HostedStreamCursors.Encode(ResolveHostedSessionSequence(session, record.SessionSequence), HostedStreamCursors.SlotTiming));
        }

        foreach (var warning in projectionOptions?.WarningOccurrences ?? [])
        {
            issued.Add(HostedStreamCursors.Encode(warning.SessionSequence, HostedStreamCursors.SlotTiming));
        }

        return issued;
    }

    private static void AppendWarningOccurrences(
        SessionRuntime session,
        string sessionId,
        List<AuthorizedSessionProjectionEvent> events,
        HostedSessionEventProjectionOptions? projectionOptions)
    {
        foreach (var warning in (projectionOptions?.WarningOccurrences ?? [])
                     .GroupBy(item => item.WarningThresholdId, StringComparer.Ordinal)
                     .Select(group => group.OrderBy(item => item.SessionSequence).First()))
        {
            events.Add(new AuthorizedSessionProjectionEvent(
                HostedSessionEventTypes.WarningIssued,
                sessionId,
                warning.SessionSequence.ToString(CultureInfo.InvariantCulture),
                HostedSessionSnapshotProjector.FormatUtc(warning.CommittedAt),
                "Session time warning issued.",
                RemainingSeconds: warning.RemainingSecondsAtCommit,
                WarningCode: warning.WarningCode,
                SessionVersion: session.SessionVersion,
                StreamCursor: HostedStreamCursors.Wire(
                    warning.SessionSequence,
                    HostedStreamCursors.SlotTiming)));
        }
    }

    private static void AppendCommittedHostedEvents(
        SessionRuntime session,
        string sessionId,
        List<AuthorizedSessionProjectionEvent> events,
        HostedSessionEventProjectionOptions? projectionOptions)
    {
        var emittedLifecycle = new HashSet<string>(StringComparer.Ordinal);
        var simulation = new HostedLifecycleSimulation(SessionLifecycleState.Active);
        var previousWarningCode = "none";

        foreach (var record in session.ManifestRuntimeRecords.OrderBy(record => record.ManifestSequence))
        {
            if (string.Equals(record.RecordType, ManifestRuntimeRecordTypes.TranscriptAppendV1, StringComparison.Ordinal))
            {
                AppendParticipantMessageAccepted(session, sessionId, record, events);
                continue;
            }

            if (string.Equals(record.RecordType, ManifestRuntimeRecordTypes.ModelInvocationV1, StringComparison.Ordinal)
                && !record.PayloadRef.ProtectedRef.EndsWith(".outcome", StringComparison.Ordinal))
            {
                AppendQueuedWorkFromAdmission(session, sessionId, record, events);
                continue;
            }

            if (string.Equals(record.RecordType, ManifestRuntimeRecordTypes.TimerEventV1, StringComparison.Ordinal)
                && TryGetTimerLifecycleQualifier(record.PayloadRef.ProtectedRef, out var qualifier))
            {
                AppendTimerLifecycleAndTiming(
                    session,
                    sessionId,
                    record,
                    qualifier,
                    events,
                    emittedLifecycle,
                    simulation,
                    projectionOptions,
                    ref previousWarningCode);
            }
        }

        foreach (var invocation in session.Invocations)
        {
            if (!IsParticipantInvocation(invocation.Trigger))
            {
                continue;
            }

            var stableTurnId = invocation.Trigger.TurnId is null
                ? null
                : HostedSessionSnapshotProjector.ToStableId(invocation.Trigger.TurnId, "turn");

            if (string.Equals(invocation.Status, AgentInvocationStatuses.ExecutionFailed, StringComparison.Ordinal))
            {
                events.Add(CreateAgentWorkEvent(
                    session,
                    sessionId,
                    invocation.SessionSequence,
                    HostedStreamCursors.SlotFailed,
                    "failed",
                    stableTurnId,
                    "execution_failure",
                    "Agent work did not finish."));
                continue;
            }

            if (invocation.ValidationEffect?.EffectCommitSessionSequence is not { } effectSequence)
            {
                continue;
            }

            if (IsIntentionalNoAction(invocation))
            {
                events.Add(new AuthorizedSessionProjectionEvent(
                    HostedSessionEventTypes.AgentNoAction,
                    sessionId,
                    effectSequence.ToString(CultureInfo.InvariantCulture),
                    HostedSessionSnapshotProjector.FormatUtc(session.LastCommittedAt),
                    "Agent recorded no further output for this turn.",
                    TurnId: stableTurnId,
                    WorkState: "no_action",
                    ResolutionCategory: "no_action",
                    SessionVersion: session.SessionVersion,
                    StreamCursor: HostedStreamCursors.Wire(effectSequence, HostedStreamCursors.SlotNoAction)));
                continue;
            }

            if (invocation.ValidationEffect.EffectOutcome == DecisionEffectOutcomes.Applied
                && !HasPublishedAgentMessage(session, invocation.AgentInvocationId))
            {
                events.Add(CreateAgentWorkEvent(
                    session,
                    sessionId,
                    effectSequence,
                    HostedStreamCursors.SlotWorking,
                    "working",
                    stableTurnId,
                    "message_stream",
                    "Agent response in progress."));
            }
        }

        if (session.LifecycleState == SessionLifecycleState.Completing && session.CutoffSequence is { } cutoffSequence)
        {
            AppendLifecycleEvent(
                session,
                sessionId,
                ResolveHostedSessionSequence(session, cutoffSequence),
                session.LastCommittedAt,
                HostedSessionSnapshotProjector.MapLifecycle(SessionLifecycleState.Completing),
                "Session is completing.",
                events,
                emittedLifecycle,
                simulation,
                projectionOptions,
                ref previousWarningCode);
        }
    }

    private sealed class HostedLifecycleSimulation
    {
        public HostedLifecycleSimulation(SessionLifecycleState lifecycleState)
        {
            LifecycleState = lifecycleState;
        }

        public SessionLifecycleState LifecycleState { get; private set; }

        public int AccumulatedPausedSeconds { get; private set; }

        public DateTimeOffset? OpenPauseStartedAt { get; private set; }

        public void Pause(DateTimeOffset authoritativeUtc)
        {
            OpenPauseStartedAt = authoritativeUtc;
            LifecycleState = SessionLifecycleState.Paused;
        }

        public void Resume(DateTimeOffset authoritativeUtc)
        {
            if (OpenPauseStartedAt is { } started)
            {
                AccumulatedPausedSeconds += Math.Max(
                    0,
                    (int)(authoritativeUtc - started).TotalSeconds);
            }

            OpenPauseStartedAt = null;
            LifecycleState = SessionLifecycleState.Active;
        }

        public void Completing() => LifecycleState = SessionLifecycleState.Completing;
    }

    private static long ResolveHostedSessionSequence(ManifestRuntimeRecord record, SessionRuntime session) =>
        record.SessionSequence >= 1
            ? record.SessionSequence
            : Math.Max(1, session.SessionVersion);

    private static long ResolveHostedSessionSequence(SessionRuntime session, long sessionSequence) =>
        sessionSequence >= 1
            ? sessionSequence
            : Math.Max(1, session.SessionVersion);

    private static bool TryGetTimerLifecycleQualifier(string protectedRef, out string qualifier)
    {
        var separator = protectedRef.LastIndexOf('.');
        if (separator < 0 || separator == protectedRef.Length - 1)
        {
            qualifier = string.Empty;
            return false;
        }

        qualifier = protectedRef[(separator + 1)..];
        return qualifier is "paused" or "resumed";
    }

    private static void AppendTimerLifecycleAndTiming(
        SessionRuntime session,
        string sessionId,
        ManifestRuntimeRecord record,
        string qualifier,
        List<AuthorizedSessionProjectionEvent> events,
        HashSet<string> emittedLifecycle,
        HostedLifecycleSimulation simulation,
        HostedSessionEventProjectionOptions? projectionOptions,
        ref string previousWarningCode)
    {
        var lifecycleState = qualifier switch
        {
            "paused" => HostedSessionSnapshotProjector.MapLifecycle(SessionLifecycleState.Paused),
            "resumed" => HostedSessionSnapshotProjector.MapLifecycle(SessionLifecycleState.Active),
            _ => null,
        };
        if (lifecycleState is null)
        {
            return;
        }

        if (qualifier == "paused")
        {
            simulation.Pause(record.OccurredAt);
        }
        else
        {
            simulation.Resume(record.OccurredAt);
        }

        AppendLifecycleEvent(
            session,
            sessionId,
            ResolveHostedSessionSequence(record, session),
            record.OccurredAt,
            lifecycleState,
            qualifier == "paused"
                ? "Session paused."
                : "Session resumed.",
            events,
            emittedLifecycle,
            simulation,
            projectionOptions,
            ref previousWarningCode);
    }

    private static void AppendLifecycleEvent(
        SessionRuntime session,
        string sessionId,
        long sessionSequence,
        DateTimeOffset occurredAt,
        string lifecycleState,
        string summary,
        List<AuthorizedSessionProjectionEvent> events,
        HashSet<string> emittedLifecycle,
        HostedLifecycleSimulation simulation,
        HostedSessionEventProjectionOptions? projectionOptions,
        ref string previousWarningCode)
    {
        var lifecycleKey = $"{sessionSequence}:{lifecycleState}";
        if (!emittedLifecycle.Add(lifecycleKey))
        {
            return;
        }

        if (lifecycleState == HostedSessionSnapshotProjector.MapLifecycle(SessionLifecycleState.Completing))
        {
            simulation.Completing();
        }

        events.Add(new AuthorizedSessionProjectionEvent(
            HostedSessionEventTypes.LifecycleChanged,
            sessionId,
            sessionSequence.ToString(CultureInfo.InvariantCulture),
            HostedSessionSnapshotProjector.FormatUtc(occurredAt),
            summary,
            LifecycleState: lifecycleState,
            SessionVersion: session.SessionVersion,
            StreamCursor: HostedStreamCursors.Wire(sessionSequence, HostedStreamCursors.SlotLifecycle)));

        AppendTimingProjectionIfConfigured(
            session,
            sessionId,
            sessionSequence,
            occurredAt,
            simulation,
            projectionOptions,
            events,
            ref previousWarningCode);
    }

    private static void AppendTimingProjectionIfConfigured(
        SessionRuntime session,
        string sessionId,
        long sessionSequence,
        DateTimeOffset occurredAt,
        HostedLifecycleSimulation simulation,
        HostedSessionEventProjectionOptions? projectionOptions,
        List<AuthorizedSessionProjectionEvent> events,
        ref string previousWarningCode)
    {
        if (projectionOptions?.TimingPolicy is not { } timingPolicy)
        {
            return;
        }

        var startedAt = projectionOptions.SessionStartedAt ?? session.LastCommittedAt;
        var authoritativeUtc = projectionOptions.AuthoritativeUtc ?? occurredAt;
        var timing = HostedSessionTiming.Project(
            simulation.LifecycleState,
            startedAt,
            occurredAt,
            authoritativeUtc,
            timingPolicy,
            simulation.AccumulatedPausedSeconds,
            simulation.OpenPauseStartedAt);

        events.Add(new AuthorizedSessionProjectionEvent(
            HostedSessionEventTypes.TimingUpdated,
            sessionId,
            sessionSequence.ToString(CultureInfo.InvariantCulture),
            HostedSessionSnapshotProjector.FormatUtc(occurredAt),
            "Session timing updated.",
            LifecycleState: HostedSessionSnapshotProjector.MapLifecycle(simulation.LifecycleState),
            RemainingSeconds: timing.RemainingSeconds,
            WarningCode: timing.WarningCode,
            SessionVersion: session.SessionVersion,
            StreamCursor: HostedStreamCursors.Wire(sessionSequence, HostedStreamCursors.SlotTiming)));

        previousWarningCode = timing.WarningCode ?? "none";
    }

    private static void AppendParticipantMessageAccepted(
        SessionRuntime session,
        string sessionId,
        ManifestRuntimeRecord record,
        List<AuthorizedSessionProjectionEvent> events)
    {
        var messageId = record.PayloadRef.ProtectedRef;
        var transcriptItem = session.VisibleTranscript.FirstOrDefault(item =>
            string.Equals(item.MessageId, messageId, StringComparison.Ordinal));
        if (transcriptItem is null
            || !string.Equals(transcriptItem.AuthorType, TranscriptAuthorTypes.Participant, StringComparison.Ordinal))
        {
            return;
        }

        events.Add(new AuthorizedSessionProjectionEvent(
            HostedSessionEventTypes.MessageAccepted,
            sessionId,
            record.SessionSequence.ToString(CultureInfo.InvariantCulture),
            HostedSessionSnapshotProjector.FormatUtc(record.OccurredAt),
            "Participant message accepted.",
            TurnId: string.IsNullOrWhiteSpace(transcriptItem.TurnId)
                ? null
                : HostedSessionSnapshotProjector.ToStableId(transcriptItem.TurnId, "turn"),
            MessageId: HostedSessionSnapshotProjector.ToStableId(messageId, "msg"),
            SessionVersion: session.SessionVersion,
            StreamCursor: HostedStreamCursors.Wire(record.SessionSequence, HostedStreamCursors.SlotAccepted)));
    }

    private static void AppendQueuedWorkFromAdmission(
        SessionRuntime session,
        string sessionId,
        ManifestRuntimeRecord record,
        List<AuthorizedSessionProjectionEvent> events)
    {
        var invocationId = record.PayloadRef.ProtectedRef;
        var invocation = session.Invocations.FirstOrDefault(candidate =>
            string.Equals(candidate.AgentInvocationId, invocationId, StringComparison.Ordinal));
        if (invocation is null || !IsParticipantInvocation(invocation.Trigger))
        {
            return;
        }

        var stableTurnId = invocation.Trigger.TurnId is null
            ? null
            : HostedSessionSnapshotProjector.ToStableId(invocation.Trigger.TurnId, "turn");
        events.Add(CreateAgentWorkEvent(
            session,
            sessionId,
            record.SessionSequence,
            HostedStreamCursors.SlotQueued,
            "queued",
            stableTurnId,
            null,
            "Agent work queued."));
    }

    private static AuthorizedSessionProjectionEvent CreateAgentWorkEvent(
        SessionRuntime session,
        string sessionId,
        long sessionSequence,
        int slot,
        string workState,
        string? turnId,
        string? resolutionCategory,
        string summary) =>
        new(
            HostedSessionEventTypes.AgentWork,
            sessionId,
            sessionSequence.ToString(CultureInfo.InvariantCulture),
            HostedSessionSnapshotProjector.FormatUtc(session.LastCommittedAt),
            summary,
            TurnId: turnId,
            WorkState: workState,
            ResolutionCategory: resolutionCategory,
            SessionVersion: session.SessionVersion,
            StreamCursor: HostedStreamCursors.Wire(sessionSequence, slot));

    private static bool IsParticipantInvocation(TrustedTrigger trigger) =>
        string.Equals(trigger.TriggerFamily, RuntimeTriggerIdentifiers.ParticipantInputFamily, StringComparison.Ordinal)
        && string.Equals(trigger.TriggerType, RuntimeTriggerIdentifiers.ParticipantMessageType, StringComparison.Ordinal);

    private static bool IsIntentionalNoAction(AgentInvocation invocation) =>
        invocation.Decision is not null
        && string.Equals(invocation.Decision.DecisionType, RuntimeDecisionTypes.NoAction, StringComparison.Ordinal)
        && invocation.ValidationEffect is { EffectOutcome: DecisionEffectOutcomes.NoDomainEffect };

    private static bool HasPublishedAgentMessage(SessionRuntime session, string invocationId) =>
        session.AgentMessages.Any(message =>
            string.Equals(message.DrivingInvocationId, invocationId, StringComparison.Ordinal)
            && message.Fragments.Count > 0);
}
