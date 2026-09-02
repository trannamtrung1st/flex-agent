using System.Diagnostics;
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

public sealed class SessionRuntimeObservabilityTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Admission_p95_stays_inside_the_two_second_platform_objective()
    {
        var samples = new List<TimeSpan>(20);
        for (var index = 0; index < 20; index++)
        {
            var organization = await Fixture.SeedOrganizationAsync($"-p95-{index}");
            var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
            var repository = SessionPersistenceFixtures.RuntimeRepository();
            var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
                Fixture.Services.ConnectionAccessor,
                repository,
                new AdmitTrustedTriggerHandler());
            var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
            await using (var scope = await PostgresTransactionScope.BeginAsync(
                Fixture.Services.ConnectionAccessor,
                CancellationToken))
            {
                await repository.InsertActiveAsync(binding.Ownership, session, SessionPersistenceFixtures.Actor(organization.ActorId), scope.Transaction, CancellationToken);
                await scope.CommitAsync(CancellationToken);
            }

            var started = Stopwatch.GetTimestamp();
            var admitted = await coordinator.AdmitAsync(
                new AdmitTrustedTriggerCommand(
                    SessionPersistenceFixtures.Actor(organization.ActorId),
                    binding.Ownership,
                    0,
                    SessionPersistenceFixtures.OpeningTrigger($"trig.opening.p95.{index}"),
                    $"idem.opening.p95.{index}",
                    Guid.NewGuid(),
                    "integration.test"),
                binding,
                CancellationToken);
            samples.Add(Stopwatch.GetElapsedTime(started));
            Assert.True(admitted.Succeeded, admitted.OutcomeCode);
        }

        var p95 = SessionRuntimeLatencyObjectives.Percentile(samples, 95);
        Assert.True(
            p95 <= SessionRuntimeLatencyObjectives.AdmissionOrReconnectP95,
            $"admission p95 {p95.TotalMilliseconds:F1}ms exceeded 2000ms");
    }

    [Fact]
    public async Task Replay_p95_stays_inside_the_two_second_platform_objective()
    {
        var samples = new List<TimeSpan>(20);
        var prepared = new List<(TrustedSessionBinding Binding, ReplayAuthorizedSessionEventsCommand Command)>();
        for (var index = 0; index < 20; index++)
        {
            var organization = await Fixture.SeedOrganizationAsync($"-replay-{index}");
            var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
            var repository = SessionPersistenceFixtures.RuntimeRepository();
            var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
            await using (var scope = await PostgresTransactionScope.BeginAsync(
                Fixture.Services.ConnectionAccessor,
                CancellationToken))
            {
                await repository.InsertActiveAsync(binding.Ownership, session, SessionPersistenceFixtures.Actor(organization.ActorId), scope.Transaction, CancellationToken);
                await scope.CommitAsync(CancellationToken);
            }

            prepared.Add((
                binding,
                new ReplayAuthorizedSessionEventsCommand(
                    SessionPersistenceFixtures.Actor(organization.ActorId),
                    binding.Ownership,
                    null)));
        }

        var coordinator = new PostgresReplayAuthorizedSessionEventsCoordinator(
            Fixture.Services.ConnectionAccessor,
            SessionPersistenceFixtures.RuntimeRepository(),
            new ReplayAuthorizedSessionEventsHandler());
        foreach (var item in prepared)
        {
            var started = Stopwatch.GetTimestamp();
            var replayed = await coordinator.ReplayAsync(item.Command, item.Binding, CancellationToken);
            samples.Add(Stopwatch.GetElapsedTime(started));
            Assert.True(replayed.Succeeded, replayed.OutcomeCode);
        }

        var p95 = SessionRuntimeLatencyObjectives.Percentile(samples, 95);
        Assert.True(
            p95 <= SessionRuntimeLatencyObjectives.AdmissionOrReconnectP95,
            $"replay p95 {p95.TotalMilliseconds:F1}ms exceeded 2000ms");
    }

    [Fact]
    public async Task Audit_fault_emits_a_bounded_failed_signal_without_protected_identifiers()
    {
        var organization = await Fixture.SeedOrganizationAsync("-obs-audit");
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler(telemetry),
            new FaultInjectingAuditEventWriter(),
            new PostgresOutboxItemWriter(),
            telemetry);
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, SessionPersistenceFixtures.Actor(organization.ActorId), scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.AdmitAsync(
                new AdmitTrustedTriggerCommand(
                    SessionPersistenceFixtures.Actor(organization.ActorId),
                    binding.Ownership,
                    0,
                    SessionPersistenceFixtures.OpeningTrigger("trig.opening.obs.audit"),
                    "idem.opening.obs.audit",
                    Guid.NewGuid(),
                    "integration.test"),
                binding,
                CancellationToken));

        var fault = Assert.Single(
            sink.Counters,
            item => item.Instrument == SessionRuntimeTelemetryInstruments.Fault);
        Assert.Equal(SessionRuntimeTelemetryValues.Audit, fault.Labels[SessionRuntimeTelemetryLabelKeys.FaultKind]);
        Assert.Equal(SessionRuntimeTelemetryValues.Failed, fault.Labels[SessionRuntimeTelemetryLabelKeys.Outcome]);
        Assert.DoesNotContain(sink.AllLabelValues(), value => Guid.TryParse(value, out _));
        Assert.DoesNotContain(
            sink.AllLabelValues(),
            value => value.Contains(binding.Ownership.SessionId.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Successful_audit_after_a_fault_records_succeeded_without_protected_identifiers()
    {
        var organization = await Fixture.SeedOrganizationAsync("-obs-recover");
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var sink = new CapturingSessionRuntimeTelemetrySink();
        var telemetry = new SessionRuntimeTelemetry(sink);
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, SessionPersistenceFixtures.Actor(organization.ActorId), scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var command = new AdmitTrustedTriggerCommand(
            SessionPersistenceFixtures.Actor(organization.ActorId),
            binding.Ownership,
            0,
            SessionPersistenceFixtures.OpeningTrigger("trig.opening.obs.recover"),
            "idem.opening.obs.recover",
            Guid.NewGuid(),
            "integration.test");
        var failing = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler(telemetry),
            new FaultInjectingAuditEventWriter(),
            new PostgresOutboxItemWriter(),
            telemetry);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            failing.AdmitAsync(command, binding, CancellationToken));

        var recovering = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler(telemetry),
            telemetry: telemetry);
        var admitted = await recovering.AdmitAsync(command, binding, CancellationToken);

        Assert.True(admitted.Succeeded, admitted.OutcomeCode);
        Assert.Contains(
            sink.Counters,
            item => item.Instrument == SessionRuntimeTelemetryInstruments.Fault
                && item.Labels[SessionRuntimeTelemetryLabelKeys.Outcome] == SessionRuntimeTelemetryValues.Failed);
        Assert.Contains(
            sink.Counters,
            item => item.Instrument == SessionRuntimeTelemetryInstruments.Fault
                && item.Labels[SessionRuntimeTelemetryLabelKeys.Outcome] == SessionRuntimeTelemetryValues.Succeeded);
        Assert.DoesNotContain(
            sink.Counters,
            item => item.Labels.GetValueOrDefault(SessionRuntimeTelemetryLabelKeys.Outcome)
                == SessionRuntimeTelemetryValues.Recovered);
        Assert.DoesNotContain(sink.AllLabelValues(), value => Guid.TryParse(value, out _));
    }

    [Fact]
    public async Task Throwing_telemetry_sink_does_not_roll_back_a_successful_admission()
    {
        var organization = await Fixture.SeedOrganizationAsync("-obs-throw-sink");
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var telemetry = new SessionRuntimeTelemetry(new ThrowingSessionRuntimeTelemetrySink());
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler(telemetry),
            telemetry: telemetry);
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, SessionPersistenceFixtures.Actor(organization.ActorId), scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var admitted = await coordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                SessionPersistenceFixtures.Actor(organization.ActorId),
                binding.Ownership,
                0,
                SessionPersistenceFixtures.OpeningTrigger("trig.opening.obs.throw"),
                "idem.opening.obs.throw",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);

        Assert.True(admitted.Succeeded, admitted.OutcomeCode);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var invocationCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM session_invocations
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            new
            {
                binding.Ownership.OrganizationId,
                binding.Ownership.SessionId,
            });
        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public async Task Throwing_telemetry_sink_preserves_the_original_audit_failure()
    {
        var organization = await Fixture.SeedOrganizationAsync("-obs-throw-audit");
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var telemetry = new SessionRuntimeTelemetry(new ThrowingSessionRuntimeTelemetrySink());
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AdmitTrustedTriggerHandler(telemetry),
            new FaultInjectingAuditEventWriter(),
            new PostgresOutboxItemWriter(),
            telemetry);
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, SessionPersistenceFixtures.Actor(organization.ActorId), scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.AdmitAsync(
                new AdmitTrustedTriggerCommand(
                    SessionPersistenceFixtures.Actor(organization.ActorId),
                    binding.Ownership,
                    0,
                    SessionPersistenceFixtures.OpeningTrigger("trig.opening.obs.throw.audit"),
                    "idem.opening.obs.throw.audit",
                    Guid.NewGuid(),
                    "integration.test"),
                binding,
                CancellationToken));

        Assert.Equal("Injected audit failure.", error.Message);
    }

    private sealed class ThrowingSessionRuntimeTelemetrySink : ISessionRuntimeTelemetrySink
    {
        public void Write(SessionRuntimeTelemetryPoint point) =>
            throw new InvalidOperationException("telemetry sink failed");
    }

    private sealed class FaultInjectingAuditEventWriter : IAuditEventWriter
    {
        public Task InsertAsync(
            AuditEventWriteModel auditEvent,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Injected audit failure.");
    }
}
