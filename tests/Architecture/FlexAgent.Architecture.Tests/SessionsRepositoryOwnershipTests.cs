using System.Reflection;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;

namespace FlexAgent.Architecture.Tests;

public sealed class SessionsRepositoryOwnershipTests
{
    private static readonly Type RepositoryType = typeof(PostgresSessionRuntimeRepository);

    [Fact]
    public void Protected_session_repository_methods_require_complete_ownership()
    {
        var scopedMethods = new[]
        {
            "InsertActiveAsync",
            "LoadForUpdateAsync",
            "TrySaveAdmissionAsync",
            "TrySaveCompletionAsync",
            "TrySaveAgentResponsePublicationAsync",
            "CountInvocationsAsync",
            "ListInvocationIdsAsync",
            "LoadSnapshotAsync",
        };

        foreach (var methodName in scopedMethods)
        {
            var method = RepositoryType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(method);
            Assert.True(
                HasCompleteSessionOwnership(method!),
                $"{methodName} must accept Organization, Activity, Participant, Attempt, and Session ownership.");
        }

        Assert.Null(RepositoryType.GetMethod("GetById", BindingFlags.Instance | BindingFlags.Public));
        Assert.Null(RepositoryType.GetMethod("GetBySessionId", BindingFlags.Instance | BindingFlags.Public));
        Assert.Null(RepositoryType.GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void Admit_coordinator_requires_command_ownership_and_does_not_accept_client_clocks()
    {
        AssertCoordinatorRejectsClientClocks(typeof(PostgresAdmitTrustedTriggerCoordinator), "AdmitAsync");
    }

    [Fact]
    public void Participant_completion_and_publication_coordinators_require_command_ownership_and_do_not_accept_client_clocks()
    {
        AssertCoordinatorRejectsClientClocks(typeof(PostgresAcceptParticipantMessageCoordinator), "AcceptAsync");
        AssertCoordinatorRejectsClientClocks(typeof(PostgresCompleteInvocationCoordinator), "CompleteAsync");
        AssertCoordinatorRejectsClientClocks(typeof(PostgresPublishAgentResponseCoordinator), "PublishFragmentAsync");
        AssertCoordinatorRejectsClientClocks(typeof(PostgresPublishAgentResponseCoordinator), "SealAsync");
        AssertCoordinatorRejectsClientClocks(typeof(PostgresReplayAuthorizedSessionEventsCoordinator), "ReplayAsync");
    }

    private static void AssertCoordinatorRejectsClientClocks(Type coordinatorType, string methodName)
    {
        var method = coordinatorType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Contains(parameters, parameter => parameter.Name == "command");
        Assert.DoesNotContain(
            parameters,
            parameter => parameter.Name is "utcNow" or "authoritativeUtc" or "timestamp" or "clock");
    }

    private static bool HasCompleteSessionOwnership(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Any(parameter => parameter.ParameterType == typeof(SessionOwnership)))
        {
            return true;
        }

        var names = parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal);
        return names.Contains("organizationId")
            && names.Contains("activityId")
            && names.Contains("participantId")
            && names.Contains("attemptId")
            && names.Contains("sessionId");
    }
}
