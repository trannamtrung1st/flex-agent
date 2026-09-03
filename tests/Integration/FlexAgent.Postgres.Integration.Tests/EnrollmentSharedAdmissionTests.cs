using System.Diagnostics;
using Dapper;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using FlexAgent.Submissions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EnrollmentSharedAdmissionTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Two_port_instances_share_one_actor_organization_surface_budget()
    {
        await WaitUntilAwayFromAdmissionWindowBoundaryAsync();
        var organizationId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var first = CreatePort();
        var second = CreatePort();

        var permitted = 0;
        var exhausted = 0;
        await Task.WhenAll(Enumerable.Range(0, 40).Select(async index =>
        {
            var port = index % 2 == 0 ? first : second;
            var result = await port.AcquireAsync(
                organizationId,
                actorId,
                EnrollmentRequestSurfaces.Mutation,
                CancellationToken);
            if (result.Decision == EnrollmentSharedAdmissionDecision.Permitted)
            {
                Interlocked.Increment(ref permitted);
            }
            else if (result.Decision == EnrollmentSharedAdmissionDecision.Exhausted)
            {
                Interlocked.Increment(ref exhausted);
            }
        }));

        Assert.Equal(EnrollmentRequestLimitDefaults.MutationPermitLimit, permitted);
        Assert.Equal(20, exhausted);
        Assert.Equal(
            EnrollmentSharedAdmissionDecision.Exhausted,
            (await first.AcquireAsync(
                organizationId,
                actorId,
                EnrollmentRequestSurfaces.Mutation,
                CancellationToken)).Decision);
        Assert.Equal(
            EnrollmentSharedAdmissionDecision.Permitted,
            (await first.AcquireAsync(
                organizationId,
                actorId,
                EnrollmentRequestSurfaces.Read,
                CancellationToken)).Decision);
        Assert.Equal(
            EnrollmentSharedAdmissionDecision.Permitted,
            (await second.AcquireAsync(
                Guid.CreateVersion7(),
                actorId,
                EnrollmentRequestSurfaces.Mutation,
                CancellationToken)).Decision);
    }

    [Fact]
    public async Task Restarted_port_keeps_the_consumed_window()
    {
        var organizationId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var original = CreatePort();
        for (var i = 0; i < EnrollmentRequestLimitDefaults.MutationPermitLimit; i++)
        {
            Assert.Equal(
                EnrollmentSharedAdmissionDecision.Permitted,
                (await original.AcquireAsync(
                    organizationId,
                    actorId,
                    EnrollmentRequestSurfaces.Mutation,
                    CancellationToken)).Decision);
        }

        var restarted = CreatePort();
        var denied = await restarted.AcquireAsync(
            organizationId,
            actorId,
            EnrollmentRequestSurfaces.Mutation,
            CancellationToken);
        Assert.Equal(EnrollmentSharedAdmissionDecision.Exhausted, denied.Decision);
        Assert.True(denied.RetryAfterSeconds >= 1);
    }

    [Fact]
    public async Task Database_clock_window_and_cleanup_use_postgres_utc()
    {
        var organizationId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO submissions_enrollment_request_counters (
                organization_id, actor_id, surface, window_start, window_seconds, policy_revision, permit_count)
            VALUES (
                @OrganizationId,
                @ActorId,
                'mutation',
                TIMESTAMPTZ 'epoch' + (floor(extract(epoch FROM clock_timestamp()) / 10) * 10) * INTERVAL '1 second',
                10,
                1,
                20)
            ON CONFLICT (organization_id, actor_id, surface, window_start)
            DO UPDATE SET permit_count = 20;
            INSERT INTO submissions_enrollment_request_counters (
                organization_id, actor_id, surface, window_start, window_seconds, policy_revision, permit_count)
            VALUES (
                @OrganizationId,
                @ActorId,
                'read',
                clock_timestamp() - INTERVAL '1 hour',
                10,
                1,
                3);
            """,
            new { OrganizationId = organizationId, ActorId = actorId });

        var port = CreatePort();
        var exhausted = await port.AcquireAsync(
            organizationId,
            actorId,
            EnrollmentRequestSurfaces.Mutation,
            CancellationToken);
        var read = await port.AcquireAsync(
            organizationId,
            actorId,
            EnrollmentRequestSurfaces.Read,
            CancellationToken);

        Assert.Equal(EnrollmentSharedAdmissionDecision.Exhausted, exhausted.Decision);
        Assert.Equal(EnrollmentSharedAdmissionDecision.Permitted, read.Decision);
        var stale = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM submissions_enrollment_request_counters
            WHERE organization_id = @OrganizationId
              AND actor_id = @ActorId
              AND window_start < clock_timestamp() - INTERVAL '30 minutes'
            """,
            new { OrganizationId = organizationId, ActorId = actorId });
        Assert.Equal(0, stale);
    }

    [Fact]
    public async Task Policy_mismatch_and_timeout_fail_closed_without_local_fallback()
    {
        var organizationId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var mismatched = CreatePort(EnrollmentSharedAdmissionSettings.FromDefaults() with { PolicyRevision = 2 });
        Assert.False(await mismatched.PolicyMatchesAsync(CancellationToken));
        Assert.Equal(
            EnrollmentSharedAdmissionDecision.Unavailable,
            (await mismatched.AcquireAsync(
                organizationId,
                actorId,
                EnrollmentRequestSurfaces.Read,
                CancellationToken)).Decision);

        await using var locker = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await using var lockTransaction = await locker.BeginTransactionAsync(CancellationToken);
        await locker.ExecuteAsync(
            new CommandDefinition(
                "LOCK TABLE submissions_enrollment_request_counters IN ACCESS EXCLUSIVE MODE",
                transaction: lockTransaction,
                cancellationToken: CancellationToken));
        var timedOut = CreatePort(
            EnrollmentSharedAdmissionSettings.FromDefaults() with { Timeout = TimeSpan.FromMilliseconds(100) });
        var watch = Stopwatch.StartNew();
        var unavailable = await timedOut.AcquireAsync(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            EnrollmentRequestSurfaces.Read,
            CancellationToken);
        watch.Stop();
        await lockTransaction.RollbackAsync(CancellationToken);

        Assert.Equal(EnrollmentSharedAdmissionDecision.Unavailable, unavailable.Decision);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2));
        Assert.Equal(
            EnrollmentSharedAdmissionDecision.Permitted,
            (await CreatePort().AcquireAsync(
                organizationId,
                actorId,
                EnrollmentRequestSurfaces.Read,
                CancellationToken)).Decision);
    }

    [Fact]
    public async Task Policy_updates_may_only_tighten_and_must_raise_the_revision()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(CancellationToken);
        var tightened = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE submissions_enrollment_request_policies
                SET policy_revision = policy_revision + 1,
                    mutation_permit_limit = 10,
                    activated_at = clock_timestamp()
                WHERE singleton_key = 1;
                """,
                transaction: transaction,
                cancellationToken: CancellationToken));
        Assert.Equal(1, tightened);
        var loosened = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE submissions_enrollment_request_policies
                SET policy_revision = policy_revision + 1,
                    mutation_permit_limit = 20,
                    activated_at = clock_timestamp()
                WHERE singleton_key = 1;
                """,
                transaction: transaction,
                cancellationToken: CancellationToken)));
        Assert.Contains("only tighten", loosened.MessageText, StringComparison.OrdinalIgnoreCase);
        await transaction.RollbackAsync(CancellationToken);
    }

    [Fact]
    public async Task Policy_cannot_change_window_seconds_because_that_would_reset_the_shared_budget()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(CancellationToken);
        var organizationId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var port = CreatePort();
        for (var i = 0; i < EnrollmentRequestLimitDefaults.MutationPermitLimit; i++)
        {
            Assert.Equal(
                EnrollmentSharedAdmissionDecision.Permitted,
                (await port.AcquireAsync(
                    organizationId,
                    actorId,
                    EnrollmentRequestSurfaces.Mutation,
                    CancellationToken)).Decision);
        }

        var lengthened = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE submissions_enrollment_request_policies
                SET policy_revision = policy_revision + 1,
                    window_seconds = 20,
                    activated_at = clock_timestamp()
                WHERE singleton_key = 1;
                """,
                transaction: transaction,
                cancellationToken: CancellationToken)));
        Assert.Contains("window", lengthened.MessageText, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            EnrollmentSharedAdmissionDecision.Exhausted,
            (await port.AcquireAsync(
                organizationId,
                actorId,
                EnrollmentRequestSurfaces.Mutation,
                CancellationToken)).Decision);
        await transaction.RollbackAsync(CancellationToken);
    }

    [Fact]
    public async Task Cleanup_uses_indexed_expires_at_and_does_not_remove_live_windows()
    {
        var liveOrganizationId = Guid.CreateVersion7();
        var expiredOrganizationId = Guid.CreateVersion7();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var windowStart = await connection.ExecuteScalarAsync<DateTimeOffset>(
            """
            SELECT to_timestamp(
                (floor(extract(epoch FROM clock_timestamp()) / 10))::bigint * 10);
            """);
        for (var i = 0; i < 80; i++)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO submissions_enrollment_request_counters (
                    organization_id, actor_id, surface, window_start, window_seconds, policy_revision, permit_count)
                VALUES (
                    @OrganizationId,
                    @ActorId,
                    'read',
                    @WindowStart,
                    10,
                    1,
                    1);
                """,
                new
                {
                    OrganizationId = liveOrganizationId,
                    ActorId = Guid.CreateVersion7(),
                    WindowStart = windowStart,
                });
        }

        await connection.ExecuteAsync(
            """
            INSERT INTO submissions_enrollment_request_counters (
                organization_id, actor_id, surface, window_start, window_seconds, policy_revision, permit_count)
            VALUES (
                @OrganizationId,
                @ActorId,
                'read',
                clock_timestamp() - INTERVAL '1 hour',
                10,
                1,
                4);
            """,
            new { OrganizationId = expiredOrganizationId, ActorId = Guid.CreateVersion7() });

        var indexes = (await connection.QueryAsync<string>(
            """
            SELECT indexname
            FROM pg_indexes
            WHERE tablename = 'submissions_enrollment_request_counters';
            """)).ToArray();
        Assert.Contains(indexes, name => name.Contains("expires_at", StringComparison.Ordinal));
        Assert.DoesNotContain("ix_submissions_enrollment_request_counters_cleanup", indexes);

        var result = await CreatePort().AcquireAsync(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            EnrollmentRequestSurfaces.Read,
            CancellationToken);
        Assert.Equal(EnrollmentSharedAdmissionDecision.Permitted, result.Decision);

        var expiredRemaining = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM submissions_enrollment_request_counters
            WHERE organization_id = @OrganizationId;
            """,
            new { OrganizationId = expiredOrganizationId });
        var liveRemaining = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM submissions_enrollment_request_counters
            WHERE organization_id = @OrganizationId;
            """,
            new { OrganizationId = liveOrganizationId });
        Assert.Equal(0, expiredRemaining);
        Assert.Equal(80, liveRemaining);
    }

    [Fact]
    public async Task Representative_shared_admission_stays_inside_the_mutation_latency_objective()
    {
        var organizationId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var port = CreatePort();
        var samples = new TimeSpan[EnrollmentRequestLimitDefaults.MutationPermitLimit];
        for (var i = 0; i < samples.Length; i++)
        {
            var watch = Stopwatch.StartNew();
            var result = await port.AcquireAsync(
                organizationId,
                actorId,
                EnrollmentRequestSurfaces.Mutation,
                CancellationToken);
            watch.Stop();
            samples[i] = watch.Elapsed;
            Assert.Equal(EnrollmentSharedAdmissionDecision.Permitted, result.Decision);
        }

        Assert.True(
            EnrollmentLatencyObjectives.Percentile(samples, 95) < EnrollmentLatencyObjectives.MutationP95);
    }

    private async Task WaitUntilAwayFromAdmissionWindowBoundaryAsync()
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var epoch = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                "SELECT FLOOR(EXTRACT(EPOCH FROM CLOCK_TIMESTAMP() AT TIME ZONE 'utc'))::bigint",
                cancellationToken: CancellationToken));
        var remaining = EnrollmentRequestLimitDefaults.WindowSeconds
            - (int)(epoch % EnrollmentRequestLimitDefaults.WindowSeconds);
        if (remaining < 3)
        {
            await Task.Delay(TimeSpan.FromSeconds(remaining) + TimeSpan.FromMilliseconds(150), CancellationToken);
        }
    }

    private PostgresEnrollmentSharedAdmissionPort CreatePort(EnrollmentSharedAdmissionSettings? settings = null) =>
        new(Fixture.Services.ConnectionAccessor, settings ?? EnrollmentSharedAdmissionSettings.FromDefaults());
}
