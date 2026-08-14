using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class SealAgentResponseCommandTests
{
    [Fact]
    public void Handler_rejects_stale_expected_version_before_sealing_an_open_message()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = PublishFirstFragment(session);
        var command = new SealAgentResponseCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            session.SessionVersion + 3,
            invocationId,
            AgentMessageCompletionStates.Complete,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "application.test");

        var result = new SealAgentResponseHandler().Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.False(result.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.StaleVersion, result.OutcomeCode);
        Assert.Equal(AgentMessageCompletionStates.Open, session.AgentMessages[0].CompletionState);
    }

    [Fact]
    public void Handler_reconciles_an_exact_seal_retry_that_still_carries_the_original_expected_version()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = PublishFirstFragment(session);
        var originalVersion = session.SessionVersion;
        var command = new SealAgentResponseCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            originalVersion,
            invocationId,
            AgentMessageCompletionStates.Complete,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "application.test");
        Assert.True(
            new SealAgentResponseHandler().Handle(
                command,
                session,
                SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);
        Assert.True(session.SessionVersion > originalVersion);

        var retry = new SealAgentResponseHandler().Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(5));

        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.Equal(FragmentCommitOutcomeCodes.Reconciled, retry.OutcomeCode);
        Assert.Equal(AgentMessageCompletionStates.Complete, session.AgentMessages[0].CompletionState);
    }

    [Fact]
    public void Handler_reconciles_an_exact_incomplete_seal_retry_that_still_carries_the_original_expected_version()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = PublishFirstFragment(session);
        var originalVersion = session.SessionVersion;
        var command = new SealAgentResponseCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            originalVersion,
            invocationId,
            AgentMessageCompletionStates.Incomplete,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "application.test");
        Assert.True(
            new SealAgentResponseHandler().Handle(
                command,
                session,
                SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);
        Assert.True(session.SessionVersion > originalVersion);

        var retry = new SealAgentResponseHandler().Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(5));

        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.Equal(FragmentCommitOutcomeCodes.Reconciled, retry.OutcomeCode);
        Assert.Equal(AgentMessageCompletionStates.Incomplete, session.AgentMessages[0].CompletionState);
    }

    [Fact]
    public void Handler_reports_already_terminal_not_stale_version_for_an_opposite_seal_at_the_original_expected_version()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = PublishFirstFragment(session);
        var originalVersion = session.SessionVersion;
        Assert.True(
            new SealAgentResponseHandler().Handle(
                new SealAgentResponseCommand(
                    SessionRuntimeTestFixtures.CreateActor(),
                    session.Ownership,
                    originalVersion,
                    invocationId,
                    AgentMessageCompletionStates.Complete,
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    "application.test"),
                session,
                SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);

        var retry = new SealAgentResponseHandler().Handle(
            new SealAgentResponseCommand(
                SessionRuntimeTestFixtures.CreateActor(),
                session.Ownership,
                originalVersion,
                invocationId,
                AgentMessageCompletionStates.Incomplete,
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "application.test"),
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(5));

        Assert.False(retry.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.AlreadyTerminal, retry.OutcomeCode);
        Assert.NotEqual(FragmentCommitOutcomeCodes.StaleVersion, retry.OutcomeCode);
        Assert.Equal(AgentMessageCompletionStates.Complete, session.AgentMessages[0].CompletionState);
    }

    private static string PublishFirstFragment(SessionRuntime session)
    {
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).PublicationPathClaimed);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.test.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        return invocationId;
    }
}
