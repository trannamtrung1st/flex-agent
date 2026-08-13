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

    [Fact]
    public async Task Participant_no_action_persists_turn_resolution_without_an_agent_message()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var actor = SessionPersistenceFixtures.Actor(organization.ActorId);
        var acceptCoordinator = new PostgresAcceptParticipantMessageCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AcceptParticipantMessageHandler());
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

        var admitted = await acceptCoordinator.AcceptAsync(
            new AcceptParticipantMessageCommand(
                actor,
                binding.Ownership,
                ExpectedSessionVersion: 0,
                "msg.p.1",
                "turn.1",
                "slot.1",
                "trig.participant.1",
                "idem.p.1",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var completed = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                admitted.SessionVersion!.Value,
                admitted.Invocation!.AgentInvocationId,
                new NoActionRecommendation(
                    Guid.NewGuid().ToString("N"),
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    NoActionReasonCategories.IntentionalSilence,
                    null),
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, completed.ValidationEffect!.EffectOutcome);
        Assert.False(completed.AgentMessagePublished);

        await using var loadScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        var loaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);

        Assert.NotNull(loaded);
        var loadedInvocation = Assert.Single(loaded!.Invocations);
        Assert.Equal(ResponseSlotStates.IntentionalNoAction, Assert.Single(loaded.Turns).ResponseSlot.State);
        Assert.Equal(TurnStates.Complete, loaded.Turns[0].State);
        Assert.Equal(TranscriptAuthorTypes.Participant, Assert.Single(loaded.VisibleTranscript).AuthorType);
        Assert.Equal(DecisionValidationOutcomes.Accepted, loadedInvocation.ValidationEffect!.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NoDomainEffect, loadedInvocation.ValidationEffect.EffectOutcome);
        Assert.Equal(AgentInvocationStatuses.Decided, loadedInvocation.Status);
        Assert.Null(loadedInvocation.ExecutionOutcome);
    }

    [Fact]
    public async Task Decision_and_execution_outcome_remain_mutually_exclusive_after_persist()
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
                ExpectedSessionVersion: 0,
                SessionPersistenceFixtures.OpeningTrigger(),
                "idem.opening.xor",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var decided = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                admitted.SessionVersion!.Value,
                admitted.Invocation!.AgentInvocationId,
                new NoActionRecommendation(
                    Guid.NewGuid().ToString("N"),
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    NoActionReasonCategories.IntentionalSilence,
                    null),
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(decided.Succeeded, decided.OutcomeCode);
        Assert.NotNull(decided.Decision);

        var failed = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                decided.Invocation!.SessionSequence,
                admitted.Invocation.AgentInvocationId,
                null,
                new ExecutionFailureCompletion(ExecutionFailureReasons.MalformedControl),
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.False(failed.Succeeded);
        Assert.Equal(InvocationCompletionOutcomeCodes.AlreadyTerminal, failed.OutcomeCode);

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
                InvocationId = admitted.Invocation.AgentInvocationId,
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
                InvocationId = admitted.Invocation.AgentInvocationId,
            });
        Assert.Equal(1, decisionCount);
        Assert.Equal(0, outcomeCount);
    }

    [Fact]
    public async Task Complete_retry_after_reload_reconciles_the_same_decision_payload()
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
                ExpectedSessionVersion: 0,
                SessionPersistenceFixtures.OpeningTrigger("trig.opening.retry"),
                "idem.opening.retry",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var recommendation = new NoActionRecommendation(
            Guid.NewGuid().ToString("N"),
            admitted.Invocation!.AgentInvocationId,
            DateTimeOffset.Parse("2026-08-13T00:00:02.1234567+00:00"),
            NoActionReasonCategories.IntentionalSilence,
            null);
        var first = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                admitted.SessionVersion!.Value,
                admitted.Invocation.AgentInvocationId,
                recommendation,
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(first.Succeeded, first.OutcomeCode);

        var retry = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                first.Invocation!.SessionSequence,
                admitted.Invocation.AgentInvocationId,
                recommendation,
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);

        Assert.True(retry.Succeeded, retry.OutcomeCode);
        Assert.Equal(InvocationCompletionOutcomeCodes.Decided, retry.OutcomeCode);
        Assert.Equal(first.Decision!.DecisionId, retry.Decision!.DecisionId);
        Assert.Equal(first.Decision.PayloadDigest, retry.Decision.PayloadDigest);

        await using var loadScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        var loaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);
        Assert.Equal(first.Decision.PayloadDigest, loaded!.Invocations[0].Decision!.PayloadDigest);
        Assert.Single(loaded.Invocations[0].ValidationHistory);
    }

    [Fact]
    public async Task Validation_hydrate_uses_commit_state_so_unchanged_retry_does_not_mutate()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var admitCoordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var admitted = await admitCoordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                SessionPersistenceFixtures.Actor(organization.ActorId),
                binding.Ownership,
                ExpectedSessionVersion: 0,
                SessionPersistenceFixtures.OpeningTrigger("trig.opening.validate"),
                "idem.opening.validate",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            var invocationId = admitted.Invocation!.AgentInvocationId;
            var recorded = loaded!.RecordDecision(
                invocationId,
                new NoActionRecommendation(
                    Guid.NewGuid().ToString("N"),
                    invocationId,
                    clock,
                    NoActionReasonCategories.IntentionalSilence,
                    null),
                clock);
            Assert.True(recorded.Succeeded, recorded.OutcomeCode);
            var validated = loaded.ValidateDecision(invocationId, clock);
            Assert.Equal(DecisionValidationOutcomes.Accepted, validated.ValidationOutcome);
            Assert.True(
                await repository.TrySaveCompletionAsync(
                    binding.Ownership,
                    admitted.SessionVersion!.Value,
                    loaded,
                    loaded.Invocations[0],
                    scope.Transaction,
                    CancellationToken));
            await scope.CommitAsync(CancellationToken);
        }

        await using var retryScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        var reloaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            retryScope.Transaction,
            CancellationToken);
        Assert.NotNull(reloaded);
        var version = reloaded!.SessionVersion;
        var sequence = reloaded.SessionSequence;
        var retryClock = await repository.ReadAuthoritativeUtcAsync(retryScope.Transaction, CancellationToken);
        var retry = reloaded.ValidateDecision(admitted.Invocation!.AgentInvocationId, retryClock);
        await retryScope.CommitAsync(CancellationToken);

        Assert.Equal(DecisionValidationOutcomes.Accepted, retry.ValidationOutcome);
        Assert.Equal(version, reloaded.SessionVersion);
        Assert.Equal(sequence, reloaded.SessionSequence);
        var validation = Assert.Single(reloaded.Invocations[0].ValidationHistory);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, validation.EffectOutcome);
        Assert.Equal(reloaded.SessionVersion, validation.ValidatedAtSessionVersion);
    }

    [Fact]
    public async Task Invocation_list_and_count_do_not_leak_across_ownership()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var other = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler());
        var actor = SessionPersistenceFixtures.Actor(organization.ActorId);

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(
                binding.Ownership,
                SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero)),
                scope.Transaction,
                CancellationToken);
            await repository.InsertActiveAsync(
                other.Ownership,
                SessionRuntime.CreateActive(other, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero)),
                scope.Transaction,
                CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        Assert.True((await coordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                actor,
                binding.Ownership,
                0,
                SessionPersistenceFixtures.OpeningTrigger("trig.opening.list-a"),
                "idem.opening.list-a",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken)).Succeeded);

        await using var listScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        Assert.Equal(
            1,
            await repository.CountInvocationsAsync(binding.Ownership, listScope.Transaction, CancellationToken));
        Assert.Single(await repository.ListInvocationIdsAsync(binding.Ownership, listScope.Transaction, CancellationToken));
        Assert.Equal(
            0,
            await repository.CountInvocationsAsync(other.Ownership, listScope.Transaction, CancellationToken));
        Assert.Empty(await repository.ListInvocationIdsAsync(other.Ownership, listScope.Transaction, CancellationToken));

        var wrongParticipant = binding.Ownership with { ParticipantId = Guid.NewGuid() };
        Assert.Equal(
            0,
            await repository.CountInvocationsAsync(wrongParticipant, listScope.Transaction, CancellationToken));
        Assert.Empty(await repository.ListInvocationIdsAsync(wrongParticipant, listScope.Transaction, CancellationToken));

        var guessedSession = binding.Ownership with { SessionId = Guid.NewGuid() };
        Assert.Equal(
            0,
            await repository.CountInvocationsAsync(guessedSession, listScope.Transaction, CancellationToken));
        await listScope.CommitAsync(CancellationToken);
    }
}
