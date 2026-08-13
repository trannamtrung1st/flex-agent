using System.Reflection;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class AdmitTrustedTriggerCommandTests
{
    [Fact]
    public void Command_requires_trusted_actor_and_complete_session_ownership()
    {
        var ctor = typeof(AdmitTrustedTriggerCommand).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var parameters = ctor.GetParameters();

        Assert.Contains(parameters, parameter => parameter.Name == "Actor" && parameter.ParameterType == typeof(TrustedRuntimeActor));
        Assert.Contains(parameters, parameter => parameter.Name == "Ownership" && parameter.ParameterType == typeof(SessionOwnership));
        Assert.Contains(parameters, parameter => parameter.Name == "ExpectedSessionVersion" && parameter.ParameterType == typeof(long));
        Assert.Contains(parameters, parameter => parameter.Name == "Trigger" && parameter.ParameterType == typeof(TrustedTrigger));
        Assert.Contains(parameters, parameter => parameter.Name == "IdempotencyKey" && parameter.ParameterType == typeof(string));
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType == typeof(DateTime));
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType == typeof(DateTimeOffset));
        Assert.DoesNotContain(parameters, parameter => parameter.Name is "SessionSequence" or "ClientLastSeenSequence" or "ClientTimestamp");
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Namespace?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Namespace?.StartsWith("FlexAgent.Contracts", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(parameters, parameter => parameter.ParameterType.Name == "HttpContext");
    }

    [Fact]
    public void Session_ownership_exposes_the_complete_trusted_resource_chain()
    {
        var properties = typeof(SessionOwnership).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Contains(properties, property => property.Name == "OrganizationId" && property.PropertyType == typeof(Guid));
        Assert.Contains(properties, property => property.Name == "ActivityId" && property.PropertyType == typeof(Guid));
        Assert.Contains(properties, property => property.Name == "ParticipantId" && property.PropertyType == typeof(Guid));
        Assert.Contains(properties, property => property.Name == "AttemptId" && property.PropertyType == typeof(Guid));
        Assert.Contains(properties, property => property.Name == "SessionId" && property.PropertyType == typeof(Guid));
    }

    [Fact]
    public void Handler_requires_server_loaded_session_and_authoritative_utc_outside_the_command()
    {
        var method = typeof(IAdmitTrustedTriggerHandler).GetMethod(nameof(IAdmitTrustedTriggerHandler.Handle));
        Assert.NotNull(method);
        var parameters = method!.GetParameters();

        Assert.Contains(parameters, parameter => parameter.ParameterType == typeof(AdmitTrustedTriggerCommand));
        Assert.Contains(parameters, parameter => parameter.Name == "session" && parameter.ParameterType == typeof(SessionRuntime));
        Assert.Contains(parameters, parameter => parameter.Name == "authoritativeUtc" && parameter.ParameterType == typeof(DateTimeOffset));
    }

    [Fact]
    public void Handler_rejects_command_ownership_that_does_not_match_the_loaded_session()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var forgedOwnership = session.Ownership with
        {
            OrganizationId = Guid.Parse("00000000-0000-0000-0000-000000000099"),
        };
        var command = new AdmitTrustedTriggerCommand(
            SessionRuntimeTestFixtures.CreateActor(),
            forgedOwnership,
            session.SessionVersion,
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "application.test");

        var result = new AdmitTrustedTriggerHandler().Handle(command, session, SessionRuntimeTestFixtures.T0);

        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.OwnershipMismatch, result.OutcomeCode);
        Assert.Empty(session.Invocations);
    }

    [Fact]
    public void Handler_rejects_stale_expected_version_without_mutating_session_order()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var command = CreateOpeningCommand(session, expectedVersion: session.SessionVersion + 4);
        var sequenceBefore = session.SessionSequence;

        var result = new AdmitTrustedTriggerHandler().Handle(command, session, SessionRuntimeTestFixtures.T0);

        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.StaleVersion, result.OutcomeCode);
        Assert.Equal(sequenceBefore, session.SessionSequence);
        Assert.Empty(session.Invocations);
    }

    [Fact]
    public void Handler_retries_of_the_same_command_reconcile_to_the_same_invocation_after_the_version_bump()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var command = CreateOpeningCommand(session, session.SessionVersion);
        var handler = new AdmitTrustedTriggerHandler();

        var first = handler.Handle(command, session, SessionRuntimeTestFixtures.T0);
        var retry = handler.Handle(command, session, SessionRuntimeTestFixtures.T0.AddSeconds(1));

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.Equal(TriggerAdmissionOutcomeCodes.Succeeded, first.OutcomeCode);
        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.Equal(TriggerAdmissionOutcomeCodes.Reconciled, retry.OutcomeCode);
        Assert.Equal(first.Invocation!.AgentInvocationId, retry.Invocation!.AgentInvocationId);
        Assert.Single(session.Invocations);
        Assert.Equal(TriggerAdmissionOutcomeCodes.StaleVersion, new AdmitTrustedTriggerHandler().Handle(
            CreateOpeningCommand(session, expectedVersion: 0) with
            {
                Trigger = SessionRuntimeTestFixtures.OpeningTrigger("trig.opening.other"),
                IdempotencyKey = "idem.open.other",
            },
            session,
            SessionRuntimeTestFixtures.T0.AddMinutes(1)).OutcomeCode);
    }

    [Fact]
    public void Handler_admits_from_trusted_application_context_using_authoritative_utc()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var command = CreateOpeningCommand(session, session.SessionVersion);

        var result = new AdmitTrustedTriggerHandler().Handle(command, session, SessionRuntimeTestFixtures.T0);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(TriggerAdmissionOutcomeCodes.Succeeded, result.OutcomeCode);
        Assert.NotNull(result.Invocation);
        Assert.Equal(1, result.SessionSequence);
        Assert.Equal(SessionRuntimeTestFixtures.T0, session.LastCommittedAt);
    }

    [Fact]
    public void Handler_ignores_client_produced_trigger_timing_by_omitting_it_from_the_command()
    {
        var properties = typeof(AdmitTrustedTriggerCommand).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.DoesNotContain(properties, property => property.Name.Contains("Timestamp", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, property => property.Name.Contains("Sequence", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, property => property.PropertyType == typeof(DateTimeOffset));
    }

    [Fact]
    public void Handler_rejects_missing_actor()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var command = new AdmitTrustedTriggerCommand(
            new TrustedRuntimeActor(Guid.Empty, "synthetic.test_actor"),
            session.Ownership,
            session.SessionVersion,
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "application.test");

        var result = new AdmitTrustedTriggerHandler().Handle(command, session, SessionRuntimeTestFixtures.T0);

        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.Denied, result.OutcomeCode);
        Assert.Empty(session.Invocations);
    }

    private static AdmitTrustedTriggerCommand CreateOpeningCommand(SessionRuntime session, long expectedVersion) =>
        new(
            SessionRuntimeTestFixtures.CreateActor(),
            session.Ownership,
            expectedVersion,
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "application.test");
}
