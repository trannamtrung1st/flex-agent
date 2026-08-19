using System.Reflection;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class CompleteInvocationCommandTests
{
    [Fact]
    public void Command_requires_trusted_actor_and_complete_session_ownership_without_client_clocks()
    {
        var ctor = typeof(CompleteInvocationCommand).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var parameters = ctor.GetParameters();

        Assert.Contains(parameters, parameter => parameter.Name == "Actor" && parameter.ParameterType == typeof(TrustedRuntimeActor));
        Assert.Contains(parameters, parameter => parameter.Name == "Ownership" && parameter.ParameterType == typeof(SessionOwnership));
        Assert.Contains(parameters, parameter => parameter.Name == "ExpectedSessionVersion" && parameter.ParameterType == typeof(long));
        Assert.Contains(parameters, parameter => parameter.Name == "AgentInvocationId" && parameter.ParameterType == typeof(string));
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType == typeof(DateTime));
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType == typeof(DateTimeOffset));
        Assert.DoesNotContain(parameters, parameter => parameter.Name is "utcNow" or "authoritativeUtc" or "timestamp" or "clock");
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Namespace?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Namespace?.StartsWith("FlexAgent.Contracts", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Handler_requires_server_loaded_session_and_authoritative_utc_outside_the_command()
    {
        var method = typeof(ICompleteInvocationHandler).GetMethod(nameof(ICompleteInvocationHandler.Handle));
        Assert.NotNull(method);
        var parameters = method!.GetParameters();

        Assert.Contains(parameters, parameter => parameter.ParameterType == typeof(CompleteInvocationCommand));
        Assert.Contains(parameters, parameter => parameter.Name == "session" && parameter.ParameterType == typeof(SessionRuntime));
        Assert.Contains(parameters, parameter => parameter.Name == "authoritativeUtc" && parameter.ParameterType == typeof(DateTimeOffset));
    }

    [Fact]
    public void Handler_rejects_stale_expected_version_before_recording_a_decision()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var command = new CompleteInvocationCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            ExpectedSessionVersion: session.SessionVersion + 3,
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            null,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "application.test");

        var result = new CompleteInvocationHandler().Handle(command, session, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.False(result.Succeeded);
        Assert.Equal(InvocationCompletionOutcomeCodes.StaleVersion, result.OutcomeCode);
        Assert.Null(session.Invocations[0].Decision);
        Assert.Equal(ResponseSlotStates.Open, session.Turns[0].ResponseSlot.State);
    }

    [Fact]
    public void Handler_rejects_missing_actor_as_denied()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var command = new CompleteInvocationCommand(
            new TrustedRuntimeActor(Guid.Empty, "synthetic.test_actor"),
            session.Ownership,
            session.SessionVersion,
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            null,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "application.test");

        var result = new CompleteInvocationHandler().Handle(command, session, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.False(result.Succeeded);
        Assert.Equal(InvocationCompletionOutcomeCodes.Denied, result.OutcomeCode);
        Assert.Null(session.Invocations[0].Decision);
    }

    [Fact]
    public void Handler_rejects_command_ownership_that_does_not_match_the_loaded_session()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var command = new CompleteInvocationCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership with { OrganizationId = Guid.Parse("00000000-0000-0000-0000-000000000099") },
            session.SessionVersion,
            invocationId,
            SessionRuntimeTestFixtures.NoAction(invocationId),
            null,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "application.test");

        var result = new CompleteInvocationHandler().Handle(command, session, SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.False(result.Succeeded);
        Assert.Equal(InvocationCompletionOutcomeCodes.OwnershipMismatch, result.OutcomeCode);
        Assert.Null(session.Invocations[0].Decision);
    }
}
