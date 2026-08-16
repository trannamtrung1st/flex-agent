using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FlexAgent.Contracts.Browser;
using FlexAgent.Contracts.Transport;
using FlexAgent.SyntheticBrowser.Domain;

namespace FlexAgent.SyntheticBrowser.Application;

internal static class SyntheticSessionRuntimeAdapter
{
    internal const string ParticipantReplyText = "Thank you for your response. ";
    internal const string OpeningText = "Welcome. Let us begin. ";
    internal const string ClosingText = "This Session is now complete. ";
    internal const string TimerCheckInText = "Checking in on your progress. ";

    internal static void OnSessionActivated(string scenarioId, SyntheticScenarioState state)
    {
        if (scenarioId == SyntheticScenarioIds.SessionOpeningClosing)
        {
            EmitMessageStream(state, OpeningText, NextTurnId(state));
        }
    }

    internal static void OnParticipantMessage(string scenarioId, SyntheticScenarioState state)
    {
        if (state.CutoffReached || state.SessionLifecycle != "active")
        {
            return;
        }

        switch (ParticipantOutcome(scenarioId))
        {
            case RuntimeOutcome.MessageStream:
                EmitMessageStream(state, ParticipantReplyText, NextTurnId(state));
                break;
            case RuntimeOutcome.NoAction:
                EmitResolved(state, NextTurnId(state), "no_action", "Turn resolved without Agent reply.", showPersistentTurnStatus: true);
                break;
            case RuntimeOutcome.SuppressedFailure:
                EmitResolved(state, NextTurnId(state), "suppressed_failure", "This turn could not be completed.", showPersistentTurnStatus: false);
                break;
            case RuntimeOutcome.ExecutionFailure:
                EmitResolved(state, NextTurnId(state), "execution_failure", "The Agent could not finish this turn.", showPersistentTurnStatus: false);
                break;
        }
    }

    internal static void OnPause(SyntheticScenarioState state)
    {
        AppendEvent(state, "session.state.changed.v1", new SseSessionEventPayloadV1("Session paused."));
    }

    internal static void OnResume(SyntheticScenarioState state)
    {
        AppendEvent(state, "session.state.changed.v1", new SseSessionEventPayloadV1("Session resumed."));
    }

    internal static void OnComplete(string scenarioId, SyntheticScenarioState state)
    {
        if (state.CutoffReached)
        {
            return;
        }

        if (scenarioId == SyntheticScenarioIds.SessionOpeningClosing && state.SessionLifecycle is "active" or "paused")
        {
            EmitMessageStream(state, ClosingText, NextTurnId(state));
        }

        state.CutoffReached = true;
        AppendEvent(state, "session.terminal.v1", new SseSessionEventPayloadV1("Session completed."));
    }

    internal static void AdmitTimerFire(string scenarioId, SyntheticScenarioState state, string revisionId)
    {
        if (state.CutoffReached || state.SessionLifecycle != "active")
        {
            return;
        }

        if (!state.FiredTimerRevisions.Add(revisionId))
        {
            return;
        }

        switch (TimerOutcome(scenarioId))
        {
            case RuntimeOutcome.MessageStream:
                EmitMessageStream(state, TimerCheckInText, NextTurnId(state));
                break;
            case RuntimeOutcome.NoAction:
                EmitResolved(state, NextTurnId(state), "no_action", "Turn resolved without Agent reply.", showPersistentTurnStatus: false);
                break;
        }
    }

    private static RuntimeOutcome ParticipantOutcome(string scenarioId) => scenarioId switch
    {
        SyntheticScenarioIds.SessionParticipantNoAction => RuntimeOutcome.NoAction,
        SyntheticScenarioIds.SessionRejectedDecision => RuntimeOutcome.SuppressedFailure,
        SyntheticScenarioIds.SessionAcceptedEffectFailure => RuntimeOutcome.SuppressedFailure,
        SyntheticScenarioIds.SessionExecutionFailure => RuntimeOutcome.ExecutionFailure,
        SyntheticScenarioIds.SessionTimerNoAction => RuntimeOutcome.None,
        SyntheticScenarioIds.SessionTimerVisibleWork => RuntimeOutcome.None,
        SyntheticScenarioIds.SessionDefaultTimer => RuntimeOutcome.None,
        SyntheticScenarioIds.SessionDuplicateConcurrentRevision => RuntimeOutcome.None,
        _ => RuntimeOutcome.MessageStream,
    };

    private static RuntimeOutcome TimerOutcome(string scenarioId) => scenarioId switch
    {
        SyntheticScenarioIds.SessionTimerNoAction => RuntimeOutcome.NoAction,
        SyntheticScenarioIds.SessionTimerVisibleWork => RuntimeOutcome.MessageStream,
        SyntheticScenarioIds.SessionDefaultTimer => RuntimeOutcome.MessageStream,
        SyntheticScenarioIds.SessionDuplicateConcurrentRevision => RuntimeOutcome.MessageStream,
        SyntheticScenarioIds.SessionPauseResume => RuntimeOutcome.MessageStream,
        SyntheticScenarioIds.SessionTimerReplacementAccepted => RuntimeOutcome.MessageStream,
        SyntheticScenarioIds.SessionTimerReplacementRejected => RuntimeOutcome.MessageStream,
        SyntheticScenarioIds.SessionTimerReplacementOmitted => RuntimeOutcome.MessageStream,
        _ => RuntimeOutcome.None,
    };

    private static void EmitMessageStream(SyntheticScenarioState state, string text, string turnId)
    {
        state.AgentMessageOrdinal++;
        var messageId = $"msg.synthetic.agent.{state.AgentMessageOrdinal}";
        EmitWork(state, turnId, "queued", "The Agent is preparing a response.");
        EmitWork(state, turnId, "working", "The Agent is preparing a response.");
        AppendEvent(
            state,
            "session.agent.fragment.v1",
            new SseSessionEventPayloadV1("Agent response fragment.", 1, messageId, text, turnId));
        AppendEvent(
            state,
            "session.agent.complete.v1",
            new SseSessionEventPayloadV1(
                "Agent response complete.",
                null,
                messageId,
                null,
                turnId,
                null,
                "message_stream",
                null,
                ComputeSha256Hex(text),
                1));
        state.Transcript.Add(new SessionTranscriptItemV1(messageId, "agent", text, "confirmed", UtcNow()));
    }

    private static void EmitResolved(
        SyntheticScenarioState state,
        string turnId,
        string resolutionCategory,
        string resolvedSummary,
        bool showPersistentTurnStatus)
    {
        EmitWork(state, turnId, "queued", "The Agent is preparing a response.");
        EmitWork(state, turnId, "working", "The Agent is preparing a response.");
        AppendEvent(
            state,
            "session.agent.work.v1",
            new SseSessionEventPayloadV1(
                resolvedSummary,
                null,
                null,
                null,
                turnId,
                "resolved",
                resolutionCategory,
                showPersistentTurnStatus));
    }

    private static void EmitWork(SyntheticScenarioState state, string turnId, string workState, string summary) =>
        AppendEvent(
            state,
            "session.agent.work.v1",
            new SseSessionEventPayloadV1(summary, null, null, null, turnId, workState));

    private static void AppendEvent(SyntheticScenarioState state, string eventType, SseSessionEventPayloadV1 payload)
    {
        state.SessionSequence++;
        state.EmittedSseEvents.Add(new SseSessionEventV1(
            BrowserSchemaVersion.V1,
            eventType,
            SyntheticCommandAuthorization.SyntheticSessionId,
            state.SessionSequence.ToString(CultureInfo.InvariantCulture),
            UtcNow(),
            payload));
    }

    private static string NextTurnId(SyntheticScenarioState state)
    {
        state.TurnOrdinal++;
        return $"turn.synthetic.{state.TurnOrdinal}";
    }

    private static string UtcNow() =>
        DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    private static string ComputeSha256Hex(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private enum RuntimeOutcome
    {
        None,
        MessageStream,
        NoAction,
        SuppressedFailure,
        ExecutionFailure,
    }
}
