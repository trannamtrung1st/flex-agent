using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionWorkerSnapshotIsolationTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Worker_snapshot_does_not_mix_a_later_committed_decision_into_an_earlier_head()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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

        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var admitted = await admitCoordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                actor,
                binding.Ownership,
                0,
                SessionPersistenceFixtures.OpeningTrigger("trig.snapshot.rr"),
                "idem.snapshot.rr",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);
        var expectedVersion = admitted.SessionVersion!.Value;
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var bindingSource = new MemoryTrustedSessionBindingSource();
        bindingSource.Register(binding);
        var settings = new DurableInvocationWorkSettings(
            actor,
            "synthetic.provider",
            "worker.session_runtime",
            65_536,
            ownership => new ModelDeploymentCredentialBindingRequest(
                ownership.OrganizationId,
                "synthetic.provider",
                "bind.opaque.0001",
                "bind.v1",
                null,
                null,
                false,
                false,
                false));
        var gateway = new PostgresInvocationWorkSessionGateway(
            Fixture.Services.ConnectionAccessor,
            repository,
            bindingSource,
            settings);
        string? isolation = null;
        PostgresSessionRuntimeRepository.AfterHeadLoadedAsync = async transaction =>
        {
            isolation = await transaction.Connection!.ExecuteScalarAsync<string>(
                new CommandDefinition(
                    "SHOW transaction_isolation;",
                    transaction: transaction,
                    cancellationToken: CancellationToken));
            var completed = await completeCoordinator.CompleteAsync(
                new CompleteInvocationCommand(
                    actor,
                    binding.Ownership,
                    expectedVersion,
                    invocationId,
                    new NoActionRecommendation(
                        "adec.snapshot.rr000001",
                        invocationId,
                        new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                        NoActionReasonCategories.IntentionalSilence,
                        null),
                    null,
                    Guid.NewGuid(),
                    "integration.test"),
                binding,
                CancellationToken);
            Assert.True(completed.Succeeded, completed.OutcomeCode);
        };

        try
        {
            var loaded = await gateway.LoadAsync(binding.Ownership, CancellationToken);

            Assert.Equal("repeatable read", isolation);
            Assert.NotNull(loaded);
            Assert.Equal(expectedVersion, loaded!.ObservedSessionVersion);
            var invocation = Assert.Single(loaded.Session.Invocations);
            Assert.Null(invocation.Decision);
            Assert.Equal(expectedVersion, loaded.Session.SessionVersion);
        }
        finally
        {
            PostgresSessionRuntimeRepository.AfterHeadLoadedAsync = null;
        }
    }
}
