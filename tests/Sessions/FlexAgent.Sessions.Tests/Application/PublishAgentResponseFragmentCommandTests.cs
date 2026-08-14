using System.Reflection;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class PublishAgentResponseFragmentCommandTests
{
    [Fact]
    public void Command_requires_trusted_actor_and_complete_session_ownership_without_client_clocks()
    {
        var ctor = typeof(PublishAgentResponseFragmentCommand).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var parameters = ctor.GetParameters();

        Assert.Contains(parameters, parameter => parameter.Name == "Actor" && parameter.ParameterType == typeof(TrustedRuntimeActor));
        Assert.Contains(parameters, parameter => parameter.Name == "Ownership" && parameter.ParameterType == typeof(SessionOwnership));
        Assert.Contains(parameters, parameter => parameter.Name == "ExpectedSessionVersion" && parameter.ParameterType == typeof(long));
        Assert.Contains(parameters, parameter => parameter.Name == "AgentInvocationId" && parameter.ParameterType == typeof(string));
        Assert.Contains(parameters, parameter => parameter.Name == "FragmentOrdinal" && parameter.ParameterType == typeof(int));
        Assert.Contains(parameters, parameter => parameter.Name == "ExactUtf8Text" && parameter.ParameterType == typeof(string));
        Assert.Contains(parameters, parameter => parameter.Name == "GenerationAttemptId" && parameter.ParameterType == typeof(string));
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType == typeof(DateTime));
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType == typeof(DateTimeOffset));
        Assert.DoesNotContain(parameters, parameter => parameter.Name is "utcNow" or "authoritativeUtc" or "timestamp" or "clock");
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Namespace?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Namespace?.StartsWith("FlexAgent.Contracts", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Handler_requires_server_loaded_session_and_authoritative_utc_outside_the_command()
    {
        var method = typeof(IPublishAgentResponseFragmentHandler).GetMethod(nameof(IPublishAgentResponseFragmentHandler.Handle));
        Assert.NotNull(method);
        var parameters = method!.GetParameters();

        Assert.Contains(parameters, parameter => parameter.ParameterType == typeof(PublishAgentResponseFragmentCommand));
        Assert.Contains(parameters, parameter => parameter.Name == "session" && parameter.ParameterType == typeof(SessionRuntime));
        Assert.Contains(parameters, parameter => parameter.Name == "authoritativeUtc" && parameter.ParameterType == typeof(DateTimeOffset));
    }

    [Fact]
    public void Handler_rejects_stale_expected_version_before_committing_a_fragment()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        var command = new PublishAgentResponseFragmentCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            ExpectedSessionVersion: session.SessionVersion + 3,
            invocationId,
            1,
            "Hel",
            "agen.test.1",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "application.test");

        var result = new PublishAgentResponseFragmentHandler().Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.False(result.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.StaleVersion, result.OutcomeCode);
        Assert.Empty(session.AgentMessages);
    }

    [Fact]
    public void Handler_rejects_missing_actor_as_denied()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        var command = new PublishAgentResponseFragmentCommand(
            new TrustedRuntimeActor(Guid.Empty, "synthetic.test_actor"),
            session.Ownership,
            session.SessionVersion,
            invocationId,
            1,
            "Hel",
            "agen.test.1",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "application.test");

        var result = new PublishAgentResponseFragmentHandler().Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.False(result.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.Denied, result.OutcomeCode);
        Assert.Empty(session.AgentMessages);
    }

    [Fact]
    public void Handler_rejects_command_ownership_that_does_not_match_the_loaded_session()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        var command = new PublishAgentResponseFragmentCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership with { OrganizationId = Guid.Parse("00000000-0000-0000-0000-000000000099") },
            session.SessionVersion,
            invocationId,
            1,
            "Hel",
            "agen.test.1",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "application.test");

        var result = new PublishAgentResponseFragmentHandler().Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.False(result.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.OwnershipMismatch, result.OutcomeCode);
        Assert.Empty(session.AgentMessages);
    }

    [Fact]
    public void Handler_commits_the_fragment_when_actor_ownership_and_version_match()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        var command = new PublishAgentResponseFragmentCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            session.SessionVersion,
            invocationId,
            1,
            "Hel",
            "agen.test.1",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "application.test");

        var result = new PublishAgentResponseFragmentHandler().Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(3));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(FragmentCommitOutcomeCodes.Succeeded, result.OutcomeCode);
        Assert.Equal("Hel", Assert.Single(session.AgentMessages).AssembleExactText());
    }

    [Fact]
    public void Handler_reconciles_an_exact_retry_that_still_carries_the_original_expected_version()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        var originalVersion = session.SessionVersion;
        var command = new PublishAgentResponseFragmentCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            originalVersion,
            invocationId,
            1,
            "Hel",
            "agen.test.1",
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "application.test");
        Assert.True(
            new PublishAgentResponseFragmentHandler().Handle(
                command,
                session,
                SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);
        Assert.True(session.SessionVersion > originalVersion);

        var retry = new PublishAgentResponseFragmentHandler().Handle(
            command,
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.Equal(FragmentCommitOutcomeCodes.Reconciled, retry.OutcomeCode);
        Assert.Equal("Hel", Assert.Single(session.AgentMessages).AssembleExactText());
    }

    [Fact]
    public void Handler_reports_digest_mismatch_for_a_stale_retry_with_the_same_ordinal_and_different_text()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var invocationId = ClaimParticipantPublication(session);
        var originalVersion = session.SessionVersion;
        Assert.True(
            new PublishAgentResponseFragmentHandler().Handle(
                new PublishAgentResponseFragmentCommand(
                    SessionRuntimeTestFixtures.CreateActor(),
                    session.Ownership,
                    originalVersion,
                    invocationId,
                    1,
                    "Hel",
                    "agen.test.1",
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    "application.test"),
                session,
                SessionRuntimeTestFixtures.T0.AddSeconds(3)).Succeeded);

        var retry = new PublishAgentResponseFragmentHandler().Handle(
            new PublishAgentResponseFragmentCommand(
                SessionRuntimeTestFixtures.CreateActor(),
                session.Ownership,
                originalVersion,
                invocationId,
                1,
                "Hey",
                "agen.test.1",
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "application.test"),
            session,
            SessionRuntimeTestFixtures.T0.AddSeconds(4));

        Assert.False(retry.Succeeded);
        Assert.Equal(FragmentCommitOutcomeCodes.DigestMismatch, retry.OutcomeCode);
        Assert.NotEqual(FragmentCommitOutcomeCodes.StaleVersion, retry.OutcomeCode);
        Assert.Equal("Hel", Assert.Single(session.AgentMessages).AssembleExactText());
    }

    private static string ClaimParticipantPublication(SessionRuntime session)
    {
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var completed = session.CompleteInvocation(
            invocationId,
            SessionRuntimeTestFixtures.EmitMessage(invocationId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));
        Assert.True(completed.PublicationPathClaimed);
        return invocationId;
    }
}
