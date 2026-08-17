using System.Diagnostics;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed record PublishAgentResponseFragmentCommand(
    TrustedRuntimeActor Actor,
    SessionOwnership Ownership,
    long ExpectedSessionVersion,
    string AgentInvocationId,
    int FragmentOrdinal,
    string ExactUtf8Text,
    string GenerationAttemptId,
    Guid CorrelationId,
    string SourceChannel);

public interface IPublishAgentResponseFragmentHandler
{
    AgentResponseFragmentCommitResult Handle(
        PublishAgentResponseFragmentCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc);
}

public sealed class PublishAgentResponseFragmentHandler(ISessionRuntimeTelemetry? telemetry = null)
    : IPublishAgentResponseFragmentHandler
{
    private readonly ISessionRuntimeTelemetry _telemetry = telemetry ?? NoopSessionRuntimeTelemetry.Instance;

    public AgentResponseFragmentCommitResult Handle(
        PublishAgentResponseFragmentCommand command,
        SessionRuntime session,
        DateTimeOffset authoritativeUtc)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        var started = Stopwatch.GetTimestamp();

        AgentResponseFragmentCommitResult result;
        if (command.Actor.ActorId == Guid.Empty || string.IsNullOrWhiteSpace(command.Actor.ActorType))
        {
            result = new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.Denied);
        }
        else if (command.Ownership != session.Ownership)
        {
            result = new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.OwnershipMismatch);
        }
        else
        {
            var existing = session.AgentMessages.FirstOrDefault(message =>
                string.Equals(message.DrivingInvocationId, command.AgentInvocationId, StringComparison.Ordinal));
            var ordinalAlreadyPresent = existing?.Fragments.Any(fragment =>
                fragment.FragmentOrdinal == command.FragmentOrdinal) == true;
            if (!ordinalAlreadyPresent && command.ExpectedSessionVersion != session.SessionVersion)
            {
                result = new AgentResponseFragmentCommitResult(false, FragmentCommitOutcomeCodes.StaleVersion);
            }
            else
            {
                result = session.CommitAgentResponseFragment(
                    new AgentResponseFragmentCommit(
                        command.AgentInvocationId,
                        command.FragmentOrdinal,
                        command.ExactUtf8Text,
                        command.GenerationAttemptId),
                    authoritativeUtc);
            }
        }

        var labels = SessionRuntimeTelemetryRecording.Labels(
            (SessionRuntimeTelemetryLabelKeys.Outcome, result.OutcomeCode),
            (
                SessionRuntimeTelemetryLabelKeys.FirstFragment,
                command.FragmentOrdinal == 1 ? SessionRuntimeTelemetryValues.Yes : SessionRuntimeTelemetryValues.No));
        _telemetry.RecordCounter(SessionRuntimeTelemetryInstruments.FragmentCommit, labels);
        _telemetry.RecordDuration(
            SessionRuntimeTelemetryInstruments.FragmentCommit,
            Stopwatch.GetElapsedTime(started),
            labels);
        return result;
    }
}

public static class SessionRuntimePublicationOutbox
{
    public static string FragmentWakeupSeed(string messageId, int fragmentOrdinal, string contentDigest) =>
        $"frag:{messageId}:{fragmentOrdinal}:{contentDigest}";

    public static string SealWakeupSeed(string messageId, string completionState, string? assembledContentDigest) =>
        $"seal:{messageId}:{completionState}:{assembledContentDigest}";
}
