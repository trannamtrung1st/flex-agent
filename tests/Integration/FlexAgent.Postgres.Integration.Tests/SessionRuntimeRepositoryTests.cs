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
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
    public async Task Admit_enqueues_pending_invocation_execute_work()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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

        var admitted = await coordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                new TrustedRuntimeActor(organization.ActorId, "synthetic.test_actor"),
                binding.Ownership,
                ExpectedSessionVersion: 0,
                SessionPersistenceFixtures.OpeningTrigger("trig.opening.work"),
                "idem.opening.work",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var work = await connection.QuerySingleAsync<(string WorkType, string BusinessKey, string State)>(
            new CommandDefinition(
                """
                SELECT work_type, business_key, state
                FROM session_durable_work
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                  AND work_type = @WorkType;
                """,
                new
                {
                    binding.Ownership.OrganizationId,
                    binding.Ownership.SessionId,
                    WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                },
                cancellationToken: CancellationToken));

        Assert.Equal(admitted.Invocation!.AgentInvocationId, work.BusinessKey);
        Assert.Equal(DurableSessionWorkStates.Pending, work.State);
        Assert.StartsWith("ainv.", admitted.Invocation.AgentInvocationId, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admit_rejects_stale_session_version()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
    public async Task Staged_validation_then_effect_reloads_per_item_effect_facts()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
                SessionPersistenceFixtures.OpeningTrigger("trig.opening.staged"),
                "idem.opening.staged",
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
                new EnvelopeRecommendation(
                    "adec.staged.0001",
                    invocationId,
                    clock,
                    DecisionDispositions.Respond,
                    [
                        new OutputRecommendation(
                            AgentOutputKinds.Message,
                            "out.message.primary",
                            "agent_opening"),
                        new OutputRecommendation(
                            AgentOutputKinds.Voice,
                            "out.voice.primary"),
                    ],
                    []),
                clock);
            Assert.True(recorded.Succeeded, recorded.OutcomeCode);
            var validated = loaded.ValidateDecision(invocationId, clock);
            Assert.Equal(DecisionValidationOutcomes.Accepted, validated.ValidationOutcome);
            Assert.Equal(DecisionEffectOutcomes.NotAttempted, loaded.Invocations[0].ValidationEffect!.EffectOutcome);
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

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            var expectedVersion = loaded!.SessionVersion;
            var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            var applied = loaded.ApplyDecisionEffect(admitted.Invocation.AgentInvocationId, clock);
            Assert.Equal(DecisionEffectOutcomes.Applied, applied.EffectOutcome);
            Assert.True(
                await repository.TrySaveCompletionAsync(
                    binding.Ownership,
                    expectedVersion,
                    loaded,
                    loaded.Invocations[0],
                    scope.Transaction,
                    CancellationToken));
            await scope.CommitAsync(CancellationToken);
        }

        await using var loadScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        var reloaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);

        Assert.NotNull(reloaded);
        var effect = reloaded!.Invocations[0].ValidationEffect!;
        Assert.Equal(DecisionEffectOutcomes.Applied, effect.EffectOutcome);
        var message = Assert.Single(effect.OutputValidations, item => item.Kind == AgentOutputKinds.Message);
        var voice = Assert.Single(effect.OutputValidations, item => item.Kind == AgentOutputKinds.Voice);
        Assert.Equal(DecisionEffectOutcomes.Applied, message.EffectOutcome);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, voice.EffectOutcome);
    }

    [Fact]
    public async Task Invocation_list_and_count_do_not_leak_across_ownership()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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

    [Fact]
    public async Task Later_turn_admission_does_not_restamp_an_unchanged_historical_turn()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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

        var first = await acceptCoordinator.AcceptAsync(
            new AcceptParticipantMessageCommand(
                actor,
                binding.Ownership,
                0,
                "msg.z",
                "turn.z",
                "slot.z",
                "trig.z",
                "idem.z",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(first.Succeeded, first.OutcomeCode);
        var completed = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                first.SessionVersion!.Value,
                first.Invocation!.AgentInvocationId,
                new NoActionRecommendation(
                    Guid.NewGuid().ToString("N"),
                    first.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    NoActionReasonCategories.IntentionalSilence,
                    null),
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        await using (var versionScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var afterComplete = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                versionScope.Transaction,
                CancellationToken);
            await versionScope.CommitAsync(CancellationToken);
            Assert.NotNull(afterComplete);
            var originalStamp = await ReadTurnCommittedAtAsync(binding.Ownership, "turn.z");
            await Task.Delay(TimeSpan.FromMilliseconds(20), CancellationToken);

            var second = await acceptCoordinator.AcceptAsync(
                new AcceptParticipantMessageCommand(
                    actor,
                    binding.Ownership,
                    afterComplete!.SessionVersion,
                    "msg.a",
                    "turn.a",
                    "slot.a",
                    "trig.a",
                    "idem.a",
                    Guid.NewGuid(),
                    "integration.test"),
                binding,
                CancellationToken);
            Assert.True(second.Succeeded, second.OutcomeCode);

            var laterStamp = await ReadTurnCommittedAtAsync(binding.Ownership, "turn.z");
            Assert.Equal(originalStamp, laterStamp);
        }
    }

    [Fact]
    public async Task Turns_reload_in_creation_order_even_when_ids_sort_lexicographically_differently()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = new PostgresSessionRuntimeRepository();
        var actor = SessionPersistenceFixtures.Actor(organization.ActorId);
        var acceptCoordinator = new PostgresAcceptParticipantMessageCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AcceptParticipantMessageHandler());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var first = await acceptCoordinator.AcceptAsync(
            new AcceptParticipantMessageCommand(
                actor,
                binding.Ownership,
                0,
                "msg.z",
                "turn.z",
                "slot.z",
                "trig.z",
                "idem.z",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(first.Succeeded, first.OutcomeCode);
        var second = await acceptCoordinator.AcceptAsync(
            new AcceptParticipantMessageCommand(
                actor,
                binding.Ownership,
                first.SessionVersion!.Value,
                "msg.a",
                "turn.a",
                "slot.a",
                "trig.a",
                "idem.a",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(second.Succeeded, second.OutcomeCode);

        await using var loadScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        var loaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(["turn.z", "turn.a"], loaded!.Turns.Select(turn => turn.TurnId));
        Assert.Equal(1, loaded.Turns[0].CreatedSessionSequence);
        Assert.Equal(2, loaded.Turns[1].CreatedSessionSequence);
    }

    [Fact]
    public async Task Opening_emit_then_participant_reply_persists_without_created_sequence_collision()
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
        var acceptCoordinator = new PostgresAcceptParticipantMessageCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AcceptParticipantMessageHandler());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var opening = await admitCoordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                actor,
                binding.Ownership,
                ExpectedSessionVersion: 0,
                SessionPersistenceFixtures.OpeningTrigger("trig.opening.order"),
                "idem.opening.order",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(opening.Succeeded, opening.OutcomeCode);

        var completed = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                opening.SessionVersion!.Value,
                opening.Invocation!.AgentInvocationId,
                new EmitMessageRecommendation(
                    Guid.NewGuid().ToString("N"),
                    opening.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    "agent_opening",
                    null,
                    null,
                    null),
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        await using (var versionScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var afterOpening = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                versionScope.Transaction,
                CancellationToken);
            await versionScope.CommitAsync(CancellationToken);
            Assert.NotNull(afterOpening);

            var participant = await acceptCoordinator.AcceptAsync(
                new AcceptParticipantMessageCommand(
                    actor,
                    binding.Ownership,
                    afterOpening!.SessionVersion,
                    "msg.a",
                    "turn.a",
                    "slot.a",
                    "trig.a",
                    "idem.a",
                    Guid.NewGuid(),
                    "integration.test"),
                binding,
                CancellationToken);
            Assert.True(participant.Succeeded, participant.OutcomeCode);
        }

        await using var loadScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        var loaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Turns.Count);
        Assert.Equal(TurnKinds.AgentOpening, loaded.Turns[0].Kind);
        Assert.Equal("turn.a", loaded.Turns[1].TurnId);
        Assert.Equal(2, loaded.Turns[0].CreatedSessionSequence);
        Assert.Equal(3, loaded.Turns[1].CreatedSessionSequence);
    }

    [Fact]
    public async Task Envelope_decision_round_trips_outputs_and_per_item_validation()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
                "idem.p.envelope",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var envelope = new EnvelopeRecommendation(
            "adec.roundtrip.0001",
            admitted.Invocation!.AgentInvocationId,
            new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
            DecisionDispositions.Respond,
            [
                new OutputRecommendation(
                    AgentOutputKinds.Message,
                    "out.message.primary",
                    "participant_reply",
                    "turn.1",
                    "slot.1"),
                new OutputRecommendation(
                    AgentOutputKinds.Voice,
                    "out.voice.primary",
                    PayloadRef: new ProtectedContentRef(
                        "prot.voice.roundtrip.0001",
                        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")),
            ],
            [
                new RequestedActionRecommendation(
                    AgentRequestedActionKinds.NextTimerRequest,
                    "act.timer.primary",
                    "PT5M",
                    "1"),
            ]);

        var completed = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                admitted.SessionVersion!.Value,
                admitted.Invocation.AgentInvocationId,
                envelope,
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        await using var loadScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        var loaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);

        Assert.NotNull(loaded);
        var loadedInvocation = Assert.Single(loaded!.Invocations);
        var loadedEnvelope = Assert.IsType<EnvelopeRecommendation>(loadedInvocation.Decision!.Recommendation);
        Assert.Equal(2, loadedEnvelope.Outputs.Count);
        Assert.Equal(AgentOutputKinds.Voice, loadedEnvelope.Outputs[1].Kind);
        Assert.Equal(
            "prot.voice.roundtrip.0001",
            loadedEnvelope.Outputs[1].PayloadRef!.ProtectedRef);
        Assert.Equal("PT5M", Assert.Single(loadedEnvelope.RequestedActions).RelativeDelay);

        var message = Assert.Single(
            loadedInvocation.ValidationEffect!.OutputValidations,
            item => item.Kind == AgentOutputKinds.Message);
        var voice = Assert.Single(
            loadedInvocation.ValidationEffect.OutputValidations,
            item => item.Kind == AgentOutputKinds.Voice);
        var timerAction = Assert.Single(loadedInvocation.ValidationEffect.RequestedActionValidations);
        Assert.Equal(DecisionValidationOutcomes.Accepted, message.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.Applied, message.EffectOutcome);
        Assert.StartsWith("aout.", message.AgentOutputId);
        Assert.Equal(DecisionValidationOutcomes.Rejected, voice.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, voice.EffectOutcome);
        Assert.Equal(DecisionEffectOutcomes.Applied, timerAction.EffectOutcome);
        Assert.Equal(completed.Decision!.PayloadDigest, loadedInvocation.Decision!.PayloadDigest);
        Assert.Equal(
            completed.Decision.PayloadDigest,
            DecisionRecommendationDigestComputer.Compute(loadedEnvelope));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var envelopeVersion = await connection.ExecuteScalarAsync<string>(
            """
            SELECT envelope_schema_version
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
        Assert.Equal("v2", envelopeVersion);
    }

    [Fact]
    public async Task Rejected_empty_respond_still_persists_an_accepted_next_timer_effect()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
                "idem.p.empty-respond-timer",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var envelope = new EnvelopeRecommendation(
            "adec.empty-respond.0001",
            admitted.Invocation!.AgentInvocationId,
            new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
            DecisionDispositions.Respond,
            [],
            [
                new RequestedActionRecommendation(
                    AgentRequestedActionKinds.NextTimerRequest,
                    "act.timer.primary",
                    "PT2M",
                    "1"),
            ]);

        var completed = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                admitted.SessionVersion!.Value,
                admitted.Invocation.AgentInvocationId,
                envelope,
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);
        Assert.Equal(DecisionValidationOutcomes.Rejected, completed.ValidationEffect!.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, completed.ValidationEffect.EffectOutcome);
        Assert.Equal(
            DecisionEffectOutcomes.Applied,
            Assert.Single(completed.ValidationEffect.RequestedActionValidations).EffectOutcome);

        await using var loadScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        var loaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            loadScope.Transaction,
            CancellationToken);
        Assert.NotNull(loaded);
        var clock = await repository.ReadAuthoritativeUtcAsync(loadScope.Transaction, CancellationToken);
        var revisionId = loaded!.CurrentTimerLane!.ScheduleRevisionId;
        var duplicate = loaded.ApplyDecisionEffect(admitted.Invocation.AgentInvocationId, clock);
        await loadScope.CommitAsync(CancellationToken);

        var effect = loaded.Invocations[0].ValidationEffect!;
        Assert.Equal(DecisionValidationOutcomes.Rejected, effect.ValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, effect.EffectOutcome);
        Assert.Equal(TimerValidationOutcomes.Accepted, effect.TimerValidationOutcome);
        Assert.Equal(DecisionEffectOutcomes.Applied, Assert.Single(effect.RequestedActionValidations).EffectOutcome);
        Assert.Equal(ResponseSlotStates.Open, loaded.Turns[0].ResponseSlot.State);
        Assert.Equal(1, loaded.PendingTimerCount);
        Assert.Equal(TimerLaneStates.Superseded, loaded.TimerSchedules[0].LaneState);
        Assert.Equal("PT2M", loaded.CurrentTimerLane.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.AgentRecommendation, loaded.CurrentTimerLane.RequestedByCategory);
        Assert.Equal(completed.Decision!.DecisionId, loaded.CurrentTimerLane.DrivingDecisionId);
        Assert.True(duplicate.Succeeded, duplicate.OutcomeCode);
        Assert.Equal(DecisionEffectOutcomes.NotAttempted, duplicate.EffectOutcome);
        Assert.Equal(revisionId, loaded.CurrentTimerLane.ScheduleRevisionId);
        Assert.Equal(2, loaded.CurrentTimerLane.ScheduleRevision);
    }

    [Fact]
    public async Task Complete_after_rejected_validate_reload_applies_the_accepted_timer_once()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
                "idem.p.staged-empty-timer",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var envelope = new EnvelopeRecommendation(
            "adec.staged-empty-timer.0001",
            admitted.Invocation!.AgentInvocationId,
            new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
            DecisionDispositions.Respond,
            [],
            [
                new RequestedActionRecommendation(
                    AgentRequestedActionKinds.NextTimerRequest,
                    "act.timer.primary",
                    "PT2M",
                    "1"),
            ]);

        long validatedVersion;
        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            var recorded = loaded!.RecordDecision(
                admitted.Invocation.AgentInvocationId,
                envelope,
                clock);
            Assert.True(recorded.Succeeded, recorded.OutcomeCode);
            var validated = loaded.ValidateDecision(admitted.Invocation.AgentInvocationId, clock);
            Assert.Equal(DecisionValidationOutcomes.Rejected, validated.ValidationOutcome);
            Assert.Equal(TimerValidationOutcomes.Accepted, validated.TimerValidationOutcome);
            Assert.Equal(AgentInvocationStatuses.DecisionRecorded, loaded.Invocations[0].Status);
            Assert.True(
                await repository.TrySaveCompletionAsync(
                    binding.Ownership,
                    admitted.SessionVersion!.Value,
                    loaded,
                    loaded.Invocations[0],
                    scope.Transaction,
                    CancellationToken));
            validatedVersion = loaded.SessionVersion;
            await scope.CommitAsync(CancellationToken);
        }

        var completed = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                validatedVersion,
                admitted.Invocation.AgentInvocationId,
                envelope,
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);
        Assert.Equal(
            DecisionEffectOutcomes.Applied,
            Assert.Single(completed.ValidationEffect!.RequestedActionValidations).EffectOutcome);

        await using var loadScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        var reloaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);

        Assert.NotNull(reloaded);
        Assert.Equal(AgentInvocationStatuses.Decided, reloaded!.Invocations[0].Status);
        Assert.Equal(
            DecisionEffectOutcomes.Applied,
            Assert.Single(reloaded.Invocations[0].ValidationEffect!.RequestedActionValidations).EffectOutcome);
        Assert.Equal("PT2M", reloaded.CurrentTimerLane!.RelativeDelay);
        Assert.Equal(TimerRequestedByCategories.AgentRecommendation, reloaded.CurrentTimerLane.RequestedByCategory);
        Assert.Equal(1, reloaded.PendingTimerCount);
    }

    [Fact]
    public async Task Envelope_message_fragments_persist_and_hydrate_linked_to_decision_output()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
                "idem.p.fragments",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var envelope = new EnvelopeRecommendation(
            "adec.fragments.0001",
            admitted.Invocation!.AgentInvocationId,
            new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
            DecisionDispositions.Respond,
            [
                new OutputRecommendation(
                    AgentOutputKinds.Message,
                    "out.message.primary",
                    "participant_reply",
                    "turn.1",
                    "slot.1"),
                new OutputRecommendation(AgentOutputKinds.Voice, "out.voice.primary"),
            ],
            []);

        var completed = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                admitted.SessionVersion!.Value,
                admitted.Invocation.AgentInvocationId,
                envelope,
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);
        var outputId = completed.ValidationEffect!.OutputValidations
            .Single(item => item.ValidationOutcome == DecisionValidationOutcomes.Accepted)
            .AgentOutputId;
        Assert.StartsWith("aout.", outputId);

        long expectedVersion;
        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            expectedVersion = loaded!.SessionVersion;
            var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            var invocationId = admitted.Invocation.AgentInvocationId;
            Assert.True(loaded!.CommitAgentResponseFragment(
                new AgentResponseFragmentCommit(invocationId, 1, "Hel", "agen.fragments.1"),
                clock).Succeeded);
            Assert.True(loaded.CommitAgentResponseFragment(
                new AgentResponseFragmentCommit(invocationId, 2, "lo", "agen.fragments.1"),
                clock.AddMilliseconds(1)).Succeeded);
            Assert.True(loaded.CompleteAgentResponseMessage(invocationId, clock.AddMilliseconds(2)).Succeeded);
            Assert.True(
                await repository.TrySaveAgentResponsePublicationAsync(
                    binding.Ownership,
                    expectedVersion,
                    loaded,
                    scope.Transaction,
                    CancellationToken));
            await scope.CommitAsync(CancellationToken);
            expectedVersion = loaded.SessionVersion;
        }

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            var message = Assert.Single(loaded!.AgentMessages);
            Assert.Equal(outputId, message.MessageId);
            Assert.Equal(completed.Decision!.DecisionId, message.DrivingDecisionId);
            Assert.Equal(admitted.Invocation.AgentInvocationId, message.DrivingInvocationId);
            Assert.Equal("agen.fragments.1", message.GenerationAttemptId);
            Assert.Equal("Hello", message.AssembleExactText());
            Assert.Equal(AgentMessageCompletionStates.Complete, message.CompletionState);
            Assert.Equal(ProtectedContentRef.DigestUtf8("Hello"), message.AssembledContentDigest);
            Assert.Equal(2, message.Fragments.Count);
            Assert.Contains(
                loaded.VisibleTranscript,
                item => item.AuthorType == TranscriptAuthorTypes.Agent && item.MessageId == outputId);
            Assert.Equal(TurnStates.Complete, loaded.Turns.Single(turn => turn.TurnId == "turn.1").State);

            var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            var retry = loaded.CommitAgentResponseFragment(
                new AgentResponseFragmentCommit(admitted.Invocation.AgentInvocationId, 1, "Hel", "agen.fragments.1"),
                clock);
            Assert.Equal(FragmentCommitOutcomeCodes.Reconciled, retry.OutcomeCode);
            Assert.True(
                await repository.TrySaveAgentResponsePublicationAsync(
                    binding.Ownership,
                    expectedVersion,
                    loaded,
                    scope.Transaction,
                    CancellationToken));
            await scope.CommitAsync(CancellationToken);
        }

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var acceptedOutput = await connection.ExecuteScalarAsync<string>(
            """
            SELECT accepted_agent_output_id
            FROM session_messages
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND message_id = @MessageId;
            """,
            new
            {
                binding.Ownership.OrganizationId,
                binding.Ownership.SessionId,
                MessageId = outputId,
            });
        Assert.Equal(outputId, acceptedOutput);

        var fragmentCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM session_message_fragments
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND message_id = @MessageId;
            """,
            new
            {
                binding.Ownership.OrganizationId,
                binding.Ownership.SessionId,
                MessageId = outputId,
            });
        Assert.Equal(2, fragmentCount);
    }

    [Fact]
    public async Task Later_fragment_persist_does_not_replay_already_stored_fragments()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
                "msg.p.incremental",
                "turn.incremental",
                "slot.incremental",
                "trig.participant.incremental",
                "idem.p.incremental",
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
                new EnvelopeRecommendation(
                    "adec.incremental.0001",
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    DecisionDispositions.Respond,
                    [
                        new OutputRecommendation(
                            AgentOutputKinds.Message,
                            "out.message.primary",
                            "participant_reply",
                            "turn.incremental",
                            "slot.incremental"),
                    ],
                    []),
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        long expectedVersion;
        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            expectedVersion = loaded!.SessionVersion;
            var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            Assert.True(loaded.CommitAgentResponseFragment(
                new AgentResponseFragmentCommit(admitted.Invocation.AgentInvocationId, 1, "Hel", "agen.incremental.1"),
                clock).Succeeded);
            Assert.True(
                await repository.TrySaveAgentResponsePublicationAsync(
                    binding.Ownership,
                    expectedVersion,
                    loaded,
                    scope.Transaction,
                    CancellationToken));
            await scope.CommitAsync(CancellationToken);
            expectedVersion = loaded.SessionVersion;
        }

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            Assert.True(loaded!.CommitAgentResponseFragment(
                new AgentResponseFragmentCommit(admitted.Invocation.AgentInvocationId, 2, "lo", "agen.incremental.1"),
                clock).Succeeded);
            PostgresSessionRuntimeRepository.FragmentInsertAttempts = 0;
            PostgresSessionRuntimeRepository.PublicationMessagesTouched = 0;
            PostgresSessionRuntimeRepository.TranscriptInsertAttempts = 0;
            PostgresSessionRuntimeRepository.TurnUpsertAttempts = 0;
            Assert.True(
                await repository.TrySaveAgentResponsePublicationAsync(
                    binding.Ownership,
                    expectedVersion,
                    loaded,
                    scope.Transaction,
                    CancellationToken));
            await scope.CommitAsync(CancellationToken);
            Assert.Equal(1, PostgresSessionRuntimeRepository.FragmentInsertAttempts);
            Assert.Equal(1, PostgresSessionRuntimeRepository.PublicationMessagesTouched);
            Assert.Equal(0, PostgresSessionRuntimeRepository.TranscriptInsertAttempts);
            Assert.Equal(0, PostgresSessionRuntimeRepository.TurnUpsertAttempts);
            Assert.Equal("Hello", Assert.Single(loaded.AgentMessages).AssembleExactText());
        }
    }

    [Fact]
    public async Task Rolled_back_publication_save_discards_the_aggregate_and_retries_from_postgres()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
                "msg.p.rollback",
                "turn.rollback",
                "slot.rollback",
                "trig.participant.rollback",
                "idem.p.rollback",
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
                new EnvelopeRecommendation(
                    "adec.rollback.0001",
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    DecisionDispositions.Respond,
                    [
                        new OutputRecommendation(
                            AgentOutputKinds.Message,
                            "out.message.primary",
                            "participant_reply",
                            "turn.rollback",
                            "slot.rollback"),
                    ],
                    []),
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        long versionBeforeSave;
        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            versionBeforeSave = loaded!.SessionVersion;
            var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            Assert.True(loaded.CommitAgentResponseFragment(
                new AgentResponseFragmentCommit(admitted.Invocation.AgentInvocationId, 1, "Hel", "agen.rollback.1"),
                clock).Succeeded);
            Assert.True(
                await repository.TrySaveAgentResponsePublicationAsync(
                    binding.Ownership,
                    versionBeforeSave,
                    loaded,
                    scope.Transaction,
                    CancellationToken));
            await scope.RollbackAsync(CancellationToken);
        }

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var reloaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(reloaded);
            Assert.Equal(versionBeforeSave, reloaded!.SessionVersion);
            Assert.Empty(reloaded.AgentMessages);
            var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            Assert.True(reloaded.CommitAgentResponseFragment(
                new AgentResponseFragmentCommit(admitted.Invocation.AgentInvocationId, 1, "Hel", "agen.rollback.1"),
                clock).Succeeded);
            Assert.True(
                await repository.TrySaveAgentResponsePublicationAsync(
                    binding.Ownership,
                    versionBeforeSave,
                    reloaded,
                    scope.Transaction,
                    CancellationToken));
            await scope.CommitAsync(CancellationToken);
        }

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var committed = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(committed);
            Assert.True(committed!.SessionVersion > versionBeforeSave);
            Assert.Equal("Hel", Assert.Single(committed.AgentMessages).AssembleExactText());
        }
    }

    [Fact]
    public async Task Legacy_emit_message_fragments_persist_without_accepted_output_fk()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
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
                "msg.p.legacy",
                "turn.legacy",
                "slot.legacy",
                "trig.participant.legacy",
                "idem.p.legacy.fragments",
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
                new EmitMessageRecommendation(
                    Guid.NewGuid().ToString("N"),
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    "participant_reply",
                    "turn.legacy",
                    "slot.legacy",
                    null),
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        string messageId;
        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            var expectedVersion = loaded!.SessionVersion;
            var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            Assert.True(loaded.CommitAgentResponseFragment(
                new AgentResponseFragmentCommit(admitted.Invocation.AgentInvocationId, 1, "Hi", "agen.legacy.1"),
                clock).Succeeded);
            messageId = Assert.Single(loaded.AgentMessages).MessageId;
            Assert.True(
                await repository.TrySaveAgentResponsePublicationAsync(
                    binding.Ownership,
                    expectedVersion,
                    loaded,
                    scope.Transaction,
                    CancellationToken));
            await scope.CommitAsync(CancellationToken);
        }

        await using var loadScope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken);
        var reloaded = await repository.LoadForUpdateAsync(
            binding.Ownership,
            binding,
            loadScope.Transaction,
            CancellationToken);
        await loadScope.CommitAsync(CancellationToken);

        var message = Assert.Single(reloaded!.AgentMessages);
        Assert.Equal(messageId, message.MessageId);
        Assert.StartsWith("aout.", message.MessageId);
        Assert.Equal(completed.Decision!.DecisionId, message.DrivingDecisionId);
        Assert.Equal("Hi", message.AssembleExactText());
        Assert.Equal(AgentMessageCompletionStates.Open, message.CompletionState);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var acceptedOutput = await connection.ExecuteScalarAsync<string?>(
            """
            SELECT accepted_agent_output_id
            FROM session_messages
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND message_id = @MessageId;
            """,
            new
            {
                binding.Ownership.OrganizationId,
                binding.Ownership.SessionId,
                MessageId = messageId,
            });
        Assert.Null(acceptedOutput);
    }

    private async Task<DateTime> ReadTurnCommittedAtAsync(SessionOwnership ownership, string turnId)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        return await connection.ExecuteScalarAsync<DateTime>(
            new CommandDefinition(
                """
                SELECT last_committed_at
                FROM session_turns
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                  AND turn_id = @TurnId;
                """,
                new
                {
                    ownership.OrganizationId,
                    ownership.SessionId,
                    TurnId = turnId,
                },
                cancellationToken: CancellationToken));
    }
}
