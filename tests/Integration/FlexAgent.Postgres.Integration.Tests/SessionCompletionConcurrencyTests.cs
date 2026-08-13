using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionCompletionConcurrencyTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Concurrent_completion_serializes_to_one_decision_and_reconciles_the_waiter()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var actor = SessionPersistenceFixtures.Actor(organization.ActorId);
        var admitCoordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler());
        var completeCoordinator = new PostgresCompleteInvocationCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new CompleteInvocationHandler());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var admitted = await admitCoordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                actor,
                binding.Ownership,
                0,
                SessionPersistenceFixtures.OpeningTrigger("trig.opening.race"),
                "idem.opening.race",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var expectedVersion = admitted.SessionVersion!.Value;

        var first = completeCoordinator.CompleteAsync(
            CreateNoAction(actor, binding, expectedVersion, invocationId),
            binding,
            CancellationToken);
        var second = completeCoordinator.CompleteAsync(
            CreateNoAction(actor, binding, expectedVersion, invocationId),
            binding,
            CancellationToken);

        var results = await Task.WhenAll(first, second);
        Assert.Contains(results, result => result.OutcomeCode == InvocationCompletionOutcomeCodes.Decided && result.Succeeded);
        Assert.All(results, result => Assert.True(result.Succeeded || result.OutcomeCode == InvocationCompletionOutcomeCodes.AlreadyTerminal));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var decisionCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_decisions
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND agent_invocation_id = @InvocationId;
            """,
            new
            {
                binding.Ownership.OrganizationId,
                binding.Ownership.SessionId,
                InvocationId = invocationId,
            });
        var outcomeCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_execution_outcomes
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND agent_invocation_id = @InvocationId;
            """,
            new
            {
                binding.Ownership.OrganizationId,
                binding.Ownership.SessionId,
                InvocationId = invocationId,
            });
        Assert.Equal(1, decisionCount);
        Assert.Equal(0, outcomeCount);
    }

    private static CompleteInvocationCommand CreateNoAction(
        TrustedRuntimeActor actor,
        TrustedSessionBinding binding,
        long expectedVersion,
        string invocationId) =>
        new(
            actor,
            binding.Ownership,
            expectedVersion,
            invocationId,
            new NoActionRecommendation(
                Guid.NewGuid().ToString("N"),
                invocationId,
                new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                NoActionReasonCategories.IntentionalSilence,
                null),
            null,
            Guid.NewGuid(),
            "integration.test");
}
