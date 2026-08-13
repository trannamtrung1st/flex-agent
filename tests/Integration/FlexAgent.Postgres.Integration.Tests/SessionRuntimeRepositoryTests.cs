using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionRuntimeRepositoryTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Load_requires_the_complete_ownership_tuple()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        await using var loadScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        var loaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            loadScope.Transaction,
            CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(binding.Ownership, loaded!.Ownership);

        var wrongOrganization = binding.Ownership with { OrganizationId = Guid.NewGuid() };
        Assert.Null(
            await repository.LoadForUpdateAsync(
                wrongOrganization,
                binding with { Ownership = wrongOrganization },
                loadScope.Transaction,
                CancellationToken));

        var wrongActivity = binding.Ownership with { ActivityId = Guid.NewGuid() };
        Assert.Null(
            await repository.LoadForUpdateAsync(
                wrongActivity,
                binding with { Ownership = wrongActivity },
                loadScope.Transaction,
                CancellationToken));

        var guessedSession = binding.Ownership with { SessionId = Guid.NewGuid() };
        Assert.Null(
            await repository.LoadForUpdateAsync(
                guessedSession,
                binding with { Ownership = guessedSession },
                loadScope.Transaction,
                CancellationToken));

        var wrongParticipant = binding.Ownership with { ParticipantId = Guid.NewGuid() };
        Assert.Null(
            await repository.LoadForUpdateAsync(
                wrongParticipant,
                binding with { Ownership = wrongParticipant },
                loadScope.Transaction,
                CancellationToken));

        var wrongAttempt = binding.Ownership with { AttemptId = Guid.NewGuid() };
        Assert.Null(
            await repository.LoadForUpdateAsync(
                wrongAttempt,
                binding with { Ownership = wrongAttempt },
                loadScope.Transaction,
                CancellationToken));
        await loadScope.CommitAsync(CancellationToken);
    }

    [Fact]
    public async Task Admit_opening_trigger_persists_and_reconciles_without_duplicate_insert()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var command = new AdmitTrustedTriggerCommand(
            new TrustedRuntimeActor(organization.ActorId, "synthetic.test_actor"),
            binding.Ownership,
            ExpectedSessionVersion: 0,
            SessionPersistenceFixtures.OpeningTrigger(),
            "idem.opening.1",
            Guid.NewGuid(),
            "integration.test");

        var first = await coordinator.AdmitAsync(command, binding, CancellationToken);
        Assert.True(first.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.Succeeded, first.OutcomeCode);
        Assert.NotNull(first.Invocation);

        var retry = await coordinator.AdmitAsync(
            command with { ExpectedSessionVersion = first.SessionVersion!.Value },
            binding,
            CancellationToken);
        Assert.True(retry.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.Reconciled, retry.OutcomeCode);
        Assert.Equal(first.Invocation!.AgentInvocationId, retry.Invocation!.AgentInvocationId);

        await using var loadScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        var loaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(1, loaded!.SessionSequence);
        Assert.Equal(1, loaded.SessionVersion);
        Assert.Equal(first.Invocation.AgentInvocationId, Assert.Single(loaded.Invocations).AgentInvocationId);
    }

    [Fact]
    public async Task Admit_rejects_stale_session_version()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var command = new AdmitTrustedTriggerCommand(
            new TrustedRuntimeActor(organization.ActorId, "synthetic.test_actor"),
            binding.Ownership,
            ExpectedSessionVersion: 3,
            SessionPersistenceFixtures.OpeningTrigger(),
            "idem.opening.stale",
            Guid.NewGuid(),
            "integration.test");

        var result = await coordinator.AdmitAsync(command, binding, CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.StaleVersion, result.OutcomeCode);
    }

    [Fact]
    public async Task Admit_rejects_command_binding_ownership_mismatch()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            new PostgresSessionRuntimeRepository(),
            new AdmitTrustedTriggerHandler());

        var command = new AdmitTrustedTriggerCommand(
            new TrustedRuntimeActor(organization.ActorId, "synthetic.test_actor"),
            binding.Ownership with { ParticipantId = Guid.NewGuid() },
            ExpectedSessionVersion: 0,
            SessionPersistenceFixtures.OpeningTrigger(),
            "idem.opening.mismatch",
            Guid.NewGuid(),
            "integration.test");

        var result = await coordinator.AdmitAsync(command, binding, CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.OwnershipMismatch, result.OutcomeCode);
    }

    [Fact]
    public async Task Cooldown_rehydration_uses_immutable_admitted_at()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 1);
        var repository = new PostgresSessionRuntimeRepository();
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var first = await coordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                new TrustedRuntimeActor(organization.ActorId, "synthetic.test_actor"),
                binding.Ownership,
                ExpectedSessionVersion: 0,
                SessionPersistenceFixtures.OpeningTrigger("trig.opening.cooldown-1"),
                "idem.opening.cooldown-1",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(first.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.Succeeded, first.OutcomeCode);

        await Task.Delay(TimeSpan.FromMilliseconds(1100), CancellationToken);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE session_invocations
                SET status = 'executing'
                WHERE organization_id = @OrganizationId
                  AND activity_id = @ActivityId
                  AND participant_id = @ParticipantId
                  AND attempt_id = @AttemptId
                  AND session_id = @SessionId
                  AND agent_invocation_id = @InvocationId;
                """,
                new
                {
                    binding.Ownership.OrganizationId,
                    binding.Ownership.ActivityId,
                    binding.Ownership.ParticipantId,
                    binding.Ownership.AttemptId,
                    binding.Ownership.SessionId,
                    InvocationId = first.Invocation!.AgentInvocationId,
                },
                cancellationToken: CancellationToken));

        var second = await coordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                new TrustedRuntimeActor(organization.ActorId, "synthetic.test_actor"),
                binding.Ownership,
                ExpectedSessionVersion: first.SessionVersion!.Value,
                SessionPersistenceFixtures.OpeningTrigger("trig.opening.cooldown-2"),
                "idem.opening.cooldown-2",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);

        Assert.Equal(TriggerAdmissionOutcomeCodes.Succeeded, second.OutcomeCode);
        Assert.True(second.Succeeded);
    }
}
