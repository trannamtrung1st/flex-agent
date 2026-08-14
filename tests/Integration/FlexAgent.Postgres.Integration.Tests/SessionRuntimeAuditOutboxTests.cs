using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionRuntimeAuditOutboxTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Audit_failure_rolls_back_admission_and_outbox()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var correlationId = Guid.NewGuid();
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler(),
            new FaultInjectingAuditEventWriter(),
            new PostgresOutboxItemWriter());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.AdmitAsync(
                new AdmitTrustedTriggerCommand(
                    SessionPersistenceFixtures.Actor(organization.ActorId),
                    binding.Ownership,
                    0,
                    SessionPersistenceFixtures.OpeningTrigger("trig.opening.audit"),
                    "idem.opening.audit",
                    correlationId,
                    "integration.test"),
                binding,
                CancellationToken));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var invocationCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_invocations
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        var auditCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM audit_events WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        var outboxCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        Assert.Equal(0, invocationCount);
        Assert.Equal(0, auditCount);
        Assert.Equal(0, outboxCount);
    }

    [Fact]
    public async Task Outbox_failure_rolls_back_admission_and_audit()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var correlationId = Guid.NewGuid();
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler(),
            new PostgresAuditEventWriter(),
            new FaultInjectingOutboxItemWriter());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.AdmitAsync(
                new AdmitTrustedTriggerCommand(
                    SessionPersistenceFixtures.Actor(organization.ActorId),
                    binding.Ownership,
                    0,
                    SessionPersistenceFixtures.OpeningTrigger("trig.opening.outbox"),
                    "idem.opening.outbox",
                    correlationId,
                    "integration.test"),
                binding,
                CancellationToken));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var invocationCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_invocations
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        var auditCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM audit_events WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        var outboxCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        Assert.Equal(0, invocationCount);
        Assert.Equal(0, auditCount);
        Assert.Equal(0, outboxCount);
    }

    [Fact]
    public async Task Audit_failure_rolls_back_completion_decision_effect_and_outbox()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = new PostgresSessionRuntimeRepository();
        var actor = SessionPersistenceFixtures.Actor(organization.ActorId);
        var acceptCoordinator = new PostgresAcceptParticipantMessageCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AcceptParticipantMessageHandler());
        var completeCorrelationId = Guid.NewGuid();
        var completeCoordinator = new PostgresCompleteInvocationCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new CompleteInvocationHandler(),
            new FaultInjectingAuditEventWriter(),
            new PostgresOutboxItemWriter());
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
                0,
                "msg.p.1",
                "turn.1",
                "slot.1",
                "trig.p.1",
                "idem.p.1",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            completeCoordinator.CompleteAsync(
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
                    completeCorrelationId,
                    "integration.test"),
                binding,
                CancellationToken));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var decisionCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_decisions
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        var validationCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_decision_validations
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        var slotState = await connection.ExecuteScalarAsync<string>(
            """
            SELECT response_slot_state
            FROM session_turns
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND turn_id = 'turn.1';
            """,
            binding.Ownership);
        var invocationStatus = await connection.ExecuteScalarAsync<string>(
            """
            SELECT status
            FROM session_invocations
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND agent_invocation_id = @InvocationId;
            """,
            new
            {
                binding.Ownership.OrganizationId,
                binding.Ownership.SessionId,
                InvocationId = admitted.Invocation!.AgentInvocationId,
            });
        var attemptCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_invocation_attempts
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        var sessionVersion = await connection.ExecuteScalarAsync<long>(
            """
            SELECT session_version
            FROM session_runtimes
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        var auditCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM audit_events WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = completeCorrelationId });
        var outboxCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = completeCorrelationId });

        Assert.Equal(0, decisionCount);
        Assert.Equal(0, validationCount);
        Assert.Equal(0, attemptCount);
        Assert.Equal(ResponseSlotStates.Open, slotState);
        Assert.Equal(AgentInvocationStatuses.Admitted, invocationStatus);
        Assert.Equal(admitted.SessionVersion, sessionVersion);
        Assert.Equal(0, auditCount);
        Assert.Equal(0, outboxCount);
    }

    [Fact]
    public async Task Fragment_persist_writes_outbox_wakeup_without_transcript_text()
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
        var correlationId = Guid.NewGuid();
        var publishCoordinator = new PostgresPublishAgentResponseCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new PublishAgentResponseFragmentHandler());
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
                "msg.p.outbox",
                "turn.outbox",
                "slot.outbox",
                "trig.participant.outbox",
                "idem.p.outbox",
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
                    "adec.outbox.0001",
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    DecisionDispositions.Respond,
                    [
                        new OutputRecommendation(
                            AgentOutputKinds.Message,
                            "out.message.primary",
                            "participant_reply",
                            "turn.outbox",
                            "slot.outbox"),
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
            await scope.CommitAsync(CancellationToken);
        }

        var published = await publishCoordinator.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                actor,
                binding.Ownership,
                expectedVersion,
                admitted.Invocation.AgentInvocationId,
                1,
                "Hel",
                "agen.outbox.1",
                correlationId,
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(published.Succeeded, published.OutcomeCode);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var fragmentCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_message_fragments
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        var eventType = await connection.ExecuteScalarAsync<string>(
            "SELECT event_type FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        var payloadDigest = await connection.ExecuteScalarAsync<string>(
            "SELECT payload_digest FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        var auditAction = await connection.ExecuteScalarAsync<string>(
            "SELECT action FROM audit_events WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });

        Assert.Equal(1, fragmentCount);
        Assert.Equal(SessionRuntimeOutboxEventTypes.AgentFragmentCommitted, eventType);
        Assert.Equal(SessionRuntimeAuditActions.PublishAgentResponseFragment, auditAction);
        var wakeupDigest = ProtectedContentRef.DigestForReference(
            SessionRuntimePublicationOutbox.FragmentWakeupSeed(
                published.Message!.MessageId,
                1,
                published.Fragment!.ContentDigest));
        Assert.Equal(wakeupDigest, payloadDigest);
        Assert.NotEqual(ProtectedContentRef.DigestUtf8("Hel"), payloadDigest);

        long versionAfterFirst;
        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            versionAfterFirst = loaded!.SessionVersion;
            await scope.CommitAsync(CancellationToken);
        }

        var second = await publishCoordinator.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                actor,
                binding.Ownership,
                versionAfterFirst,
                admitted.Invocation.AgentInvocationId,
                2,
                "lo",
                "agen.outbox.1",
                correlationId,
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(second.Succeeded, second.OutcomeCode);

        var outboxCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        Assert.Equal(2, outboxCount);
        Assert.DoesNotContain(
            await connection.QueryAsync<string>(
                "SELECT payload_digest FROM outbox_items WHERE correlation_id = @CorrelationId;",
                new { CorrelationId = correlationId }),
            digest => digest == ProtectedContentRef.DigestUtf8("lo")
                || digest == ProtectedContentRef.DigestUtf8("Hello"));
    }

    [Fact]
    public async Task Audit_failure_rolls_back_fragment_and_outbox()
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
        var correlationId = Guid.NewGuid();
        var publishCoordinator = new PostgresPublishAgentResponseCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new PublishAgentResponseFragmentHandler(),
            new FaultInjectingAuditEventWriter(),
            new PostgresOutboxItemWriter());
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
                "msg.p.outbox.fail",
                "turn.outbox.fail",
                "slot.outbox.fail",
                "trig.participant.outbox.fail",
                "idem.p.outbox.fail",
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
                    "adec.outbox.fail.0001",
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    DecisionDispositions.Respond,
                    [
                        new OutputRecommendation(
                            AgentOutputKinds.Message,
                            "out.message.primary",
                            "participant_reply",
                            "turn.outbox.fail",
                            "slot.outbox.fail"),
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
            await scope.CommitAsync(CancellationToken);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publishCoordinator.PublishFragmentAsync(
                new PublishAgentResponseFragmentCommand(
                    actor,
                    binding.Ownership,
                    expectedVersion,
                    admitted.Invocation.AgentInvocationId,
                    1,
                    "Hel",
                    "agen.outbox.fail.1",
                    correlationId,
                    "integration.test"),
                binding,
                CancellationToken));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var fragmentCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_message_fragments
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        var auditCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM audit_events WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        var outboxCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        Assert.Equal(0, fragmentCount);
        Assert.Equal(0, auditCount);
        Assert.Equal(0, outboxCount);
    }

    [Fact]
    public async Task Outbox_failure_rolls_back_fragment_and_audit()
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
        var correlationId = Guid.NewGuid();
        var publishCoordinator = new PostgresPublishAgentResponseCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new PublishAgentResponseFragmentHandler(),
            new PostgresAuditEventWriter(),
            new FaultInjectingOutboxItemWriter());
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
                "msg.p.outbox.writer",
                "turn.outbox.writer",
                "slot.outbox.writer",
                "trig.participant.outbox.writer",
                "idem.p.outbox.writer",
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
                    "adec.outbox.writer.0001",
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    DecisionDispositions.Respond,
                    [
                        new OutputRecommendation(
                            AgentOutputKinds.Message,
                            "out.message.primary",
                            "participant_reply",
                            "turn.outbox.writer",
                            "slot.outbox.writer"),
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
            await scope.CommitAsync(CancellationToken);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publishCoordinator.PublishFragmentAsync(
                new PublishAgentResponseFragmentCommand(
                    actor,
                    binding.Ownership,
                    expectedVersion,
                    admitted.Invocation.AgentInvocationId,
                    1,
                    "Hel",
                    "agen.outbox.writer.1",
                    correlationId,
                    "integration.test"),
                binding,
                CancellationToken));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var fragmentCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_message_fragments
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        var auditCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM audit_events WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        var outboxCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        Assert.Equal(0, fragmentCount);
        Assert.Equal(0, auditCount);
        Assert.Equal(0, outboxCount);
    }

    [Fact]
    public async Task Duplicate_fragment_reconciles_without_a_second_outbox_item()
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
        var correlationId = Guid.NewGuid();
        var publishCoordinator = new PostgresPublishAgentResponseCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new PublishAgentResponseFragmentHandler());
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
                "msg.p.outbox.dup",
                "turn.outbox.dup",
                "slot.outbox.dup",
                "trig.participant.outbox.dup",
                "idem.p.outbox.dup",
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
                    "adec.outbox.dup.0001",
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    DecisionDispositions.Respond,
                    [
                        new OutputRecommendation(
                            AgentOutputKinds.Message,
                            "out.message.primary",
                            "participant_reply",
                            "turn.outbox.dup",
                            "slot.outbox.dup"),
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
            await scope.CommitAsync(CancellationToken);
        }

        var first = await publishCoordinator.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                actor,
                binding.Ownership,
                expectedVersion,
                admitted.Invocation.AgentInvocationId,
                1,
                "Hel",
                "agen.outbox.dup.1",
                correlationId,
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(first.Succeeded, first.OutcomeCode);

        long versionAfterFirst;
        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            versionAfterFirst = loaded!.SessionVersion;
            await scope.CommitAsync(CancellationToken);
        }

        var duplicate = await publishCoordinator.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                actor,
                binding.Ownership,
                versionAfterFirst,
                admitted.Invocation.AgentInvocationId,
                1,
                "Hel",
                "agen.outbox.dup.1",
                correlationId,
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(duplicate.Succeeded, duplicate.OutcomeCode);
        Assert.Equal(FragmentCommitOutcomeCodes.Reconciled, duplicate.OutcomeCode);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var fragmentCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_message_fragments
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        var outboxCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*)::int FROM outbox_items WHERE correlation_id = @CorrelationId;",
            new { CorrelationId = correlationId });
        var sessionVersion = await connection.ExecuteScalarAsync<long>(
            """
            SELECT session_version
            FROM session_runtimes
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        Assert.Equal(1, fragmentCount);
        Assert.Equal(1, outboxCount);
        Assert.Equal(versionAfterFirst, sessionVersion);
    }

    private sealed class FaultInjectingAuditEventWriter : IAuditEventWriter
    {
        public Task InsertAsync(
            AuditEventWriteModel auditEvent,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Injected audit failure.");
    }

    private sealed class FaultInjectingOutboxItemWriter : IOutboxItemWriter
    {
        public Task InsertAsync(
            OutboxItemWriteModel outboxItem,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Injected outbox failure.");
    }
}
