using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Postgres.Outbox;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using FlexAgent.Submissions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class AttemptTerminalMappingPersistenceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Abort_maps_the_attempt_and_keeps_entitlement_consumed()
    {
        var ready = await PrepareMappedAttemptAsync();
        var result = await ready.Lifecycle.ChangeAsync(
            new ChangeSessionLifecycleCommand(
                ready.Actor,
                ready.Binding.Ownership,
                ready.SessionVersion,
                SessionLifecycleTransitions.Abort,
                Guid.NewGuid(),
                "integration.test"),
            ready.Binding,
            CancellationToken);

        Assert.True(result.Succeeded, result.OutcomeCode);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var attempt = await connection.QuerySingleAsync<(string Status, bool Consumed, int Ordinal)>(
            """
            SELECT status, consumed, ordinal
            FROM submissions_attempts
            WHERE organization_id = @OrganizationId AND attempt_id = @AttemptId
            """,
            new { ready.Binding.Ownership.OrganizationId, ready.Binding.Ownership.AttemptId });
        Assert.Equal(AttemptStates.Aborted, attempt.Status);
        Assert.True(attempt.Consumed);
        Assert.Equal(1, attempt.Ordinal);
    }

    [Fact]
    public async Task Audit_failure_during_abort_rolls_back_attempt_mapping()
    {
        var ready = await PrepareMappedAttemptAsync(new FaultInjectingAuditEventWriter());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ready.Lifecycle.ChangeAsync(
                new ChangeSessionLifecycleCommand(
                    ready.Actor,
                    ready.Binding.Ownership,
                    ready.SessionVersion,
                    SessionLifecycleTransitions.Abort,
                    Guid.NewGuid(),
                    "integration.test"),
                ready.Binding,
                CancellationToken));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var attempt = await connection.QuerySingleAsync<(string Status, bool Consumed)>(
            """
            SELECT status, consumed
            FROM submissions_attempts
            WHERE organization_id = @OrganizationId AND attempt_id = @AttemptId
            """,
            new { ready.Binding.Ownership.OrganizationId, ready.Binding.Ownership.AttemptId });
        var terminals = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM session_terminal_records
            WHERE organization_id = @OrganizationId AND session_id = @SessionId
            """,
            new { ready.Binding.Ownership.OrganizationId, ready.Binding.Ownership.SessionId });
        Assert.Equal(AttemptStates.Active, attempt.Status);
        Assert.True(attempt.Consumed);
        Assert.Equal(0, terminals);
    }

    private async Task<ReadyMappedAttempt> PrepareMappedAttemptAsync(IAuditEventWriter? auditEventWriter = null)
    {
        var harness = await SubmissionIntakeTestSeed.CreateAsync(Fixture, CancellationToken);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var enrollment = await connection.QuerySingleAsync<(
            Guid BaselineId, Guid TaskSourceId, Guid TaskVersionId, string TaskDigest)>(
            """
            SELECT baseline_id, task_source_id, task_version_id, task_content_digest
            FROM submissions_enrollments
            WHERE organization_id = @OrganizationId AND enrollment_id = @EnrollmentId
            """,
            new { harness.OrganizationId, harness.EnrollmentId });
        var digest = new string('a', 64);
        var attemptId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var configurationId = Guid.CreateVersion7();
        var manifestId = Guid.CreateVersion7();
        await connection.ExecuteAsync(
            """
            INSERT INTO submissions_attempts (
                organization_id, attempt_id, activity_id, cohort_id, baseline_id, enrollment_id,
                participant_actor_id, task_source_id, ordinal, entitlement_source, retry_entitlement_id,
                status, consumed, requested_at, started_at, terminal_at, terminal_reason_category,
                session_id, resolved_configuration_id, initial_manifest_id, configuration_digest, manifest_digest)
            VALUES (
                @OrganizationId, @AttemptId, @ActivityId, @CohortId, @BaselineId, @EnrollmentId,
                @ParticipantId, @TaskSourceId, 1, 'baseline', NULL,
                'active', TRUE, CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP(), NULL, NULL,
                @SessionId, @ConfigurationId, @ManifestId, @Digest, @Digest)
            """,
            new
            {
                harness.OrganizationId,
                AttemptId = attemptId,
                harness.ActivityId,
                harness.CohortId,
                enrollment.BaselineId,
                harness.EnrollmentId,
                ParticipantId = harness.ParticipantId,
                enrollment.TaskSourceId,
                SessionId = sessionId,
                ConfigurationId = configurationId,
                ManifestId = manifestId,
                Digest = digest,
            });
        var binding = SessionPersistenceFixtures.CreateBinding(
            harness.OrganizationId,
            cooldownSeconds: 0,
            activityId: harness.ActivityId,
            configurationDigest: digest,
            attemptId: attemptId,
            sessionId: sessionId,
            participantId: harness.ParticipantId);
        var store = new PostgresAttemptStore(Fixture.Services.ConnectionAccessor);
        var repository = new PostgresSessionRuntimeRepository(
            new SubmissionsSessionAttemptTerminalSink(new AttemptTerminalMappingPort(store)));
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            var session = SessionRuntime.CreateActive(
                binding,
                new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
            await SessionPersistenceFixtures.InsertActiveAsync(repository, 
                binding.Ownership,
                session,
                SessionPersistenceFixtures.Actor(harness.ParticipantId),
                scope.Transaction,
                CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        return new ReadyMappedAttempt(
            binding,
            SessionPersistenceFixtures.Actor(harness.ParticipantId),
            0,
            new PostgresSessionLifecycleCoordinator(
                Fixture.Services.ConnectionAccessor,
                repository,
                new ChangeSessionLifecycleHandler(),
                auditEventWriter));
    }

    private sealed record ReadyMappedAttempt(
        TrustedSessionBinding Binding,
        TrustedRuntimeActor Actor,
        long SessionVersion,
        PostgresSessionLifecycleCoordinator Lifecycle);

    private sealed class FaultInjectingAuditEventWriter : IAuditEventWriter
    {
        public Task InsertAsync(
            AuditEventWriteModel auditEvent,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Injected audit failure.");
    }
}
