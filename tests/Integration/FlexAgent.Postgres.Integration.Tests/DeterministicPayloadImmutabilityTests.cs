using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.Postgres.Integration.Tests.Support;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class DeterministicPayloadImmutabilityTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Direct_delete_with_allow_payload_disposition_session_flag_is_denied()
    {
        var context = await DeterministicPayloadTestSupport.ExecuteAndPersistAsync(Fixture, CancellationToken);
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);

        await connection.ExecuteAsync("SET flex_agent.allow_payload_disposition = 'true';");

        var delete = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            DELETE FROM evaluation_deterministic_payloads
            WHERE organization_id = @OrganizationId
              AND deterministic_attempt_id = @DeterministicAttemptId;
            """,
            new
            {
                context.Claimed.Ownership.OrganizationId,
                DeterministicAttemptId = context.First.Value!.DeterministicAttemptId,
            }));
        Assert.Contains("immutable", delete.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Payload_insert_with_mismatched_request_id_fails_foreign_key()
    {
        var context = await DeterministicPayloadTestSupport.ExecuteAttemptOnlyAsync(Fixture, CancellationToken);
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);

        var attempt = await connection.QuerySingleAsync<AttemptRow>(
            """
            SELECT organization_id, deterministic_attempt_id, request_id, protected_output_ref, output_content_digest
            FROM evaluation_deterministic_attempts
            WHERE organization_id = @OrganizationId
              AND deterministic_attempt_id = @DeterministicAttemptId;
            """,
            new
            {
                context.Claimed.Ownership.OrganizationId,
                DeterministicAttemptId = context.First.Value!.DeterministicAttemptId,
            });

        var insert = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO evaluation_deterministic_payloads (
                organization_id, deterministic_attempt_id, request_id,
                protected_ref, content_digest, output_utf8, created_at)
            VALUES (
                @OrganizationId, @DeterministicAttemptId, @WrongRequestId,
                @ProtectedRef, @ContentDigest, @OutputUtf8, clock_timestamp());
            """,
            new
            {
                OrganizationId = attempt.organization_id,
                DeterministicAttemptId = attempt.deterministic_attempt_id,
                WrongRequestId = Guid.CreateVersion7(),
                ProtectedRef = attempt.protected_output_ref,
                ContentDigest = attempt.output_content_digest,
                OutputUtf8 = context.First.Value.OutputUtf8!.Value.ToArray(),
            }));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, insert.SqlState);
    }

    [Fact]
    public async Task Persist_with_protected_output_ref_mismatch_fails_without_row()
    {
        var context = await DeterministicPayloadTestSupport.ExecuteAttemptOnlyAsync(Fixture, CancellationToken);
        var outputStore = new PostgresProtectedDeterministicOutputStore(Fixture.Services.ConnectionAccessor);
        var result = context.First.Value!;

        var persist = await outputStore.TryPersistAsync(
            new ProtectedDeterministicOutputPersistCommand(
                context.Claimed.Ownership,
                context.Claimed.RequestId,
                result.DeterministicAttemptId,
                DeterministicInvocationProvenance.ProtectedOutputRef(new string('b', 64))!,
                result.OutputContentDigest!,
                result.OutputUtf8!.Value),
            CancellationToken);

        Assert.False(persist.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, persist.OutcomeCode);
    }

    [Fact]
    public async Task Load_returns_null_when_request_scope_does_not_match_payload()
    {
        var context = await DeterministicPayloadTestSupport.ExecuteAndPersistAsync(Fixture, CancellationToken);
        var outputStore = new PostgresProtectedDeterministicOutputStore(Fixture.Services.ConnectionAccessor);

        var projection = await outputStore.TryLoadProjectionAsync(
            context.Claimed.Ownership.OrganizationId,
            Guid.CreateVersion7(),
            context.First.Value!.DeterministicAttemptId,
            context.First.Value.OutputContentDigest!,
            "crit.objective.word-count",
            "crit.objective.word-count.v1",
            CancellationToken);

        Assert.Null(projection);
    }

    [Fact]
    public async Task Load_returns_null_when_payload_protected_ref_does_not_match_attempt()
    {
        var context = await DeterministicPayloadTestSupport.ExecuteAttemptOnlyAsync(Fixture, CancellationToken);
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);

        var attempt = await connection.QuerySingleAsync<AttemptRow>(
            """
            SELECT organization_id, deterministic_attempt_id, request_id, protected_output_ref, output_content_digest
            FROM evaluation_deterministic_attempts
            WHERE organization_id = @OrganizationId
              AND deterministic_attempt_id = @DeterministicAttemptId;
            """,
            new
            {
                context.Claimed.Ownership.OrganizationId,
                DeterministicAttemptId = context.First.Value!.DeterministicAttemptId,
            });

        await connection.ExecuteAsync(
            """
            INSERT INTO evaluation_deterministic_payloads (
                organization_id, deterministic_attempt_id, request_id,
                protected_ref, content_digest, output_utf8, created_at)
            VALUES (
                @OrganizationId, @DeterministicAttemptId, @RequestId,
                @MismatchedProtectedRef, @ContentDigest, @OutputUtf8, clock_timestamp());
            """,
            new
            {
                OrganizationId = attempt.organization_id,
                DeterministicAttemptId = attempt.deterministic_attempt_id,
                RequestId = attempt.request_id,
                MismatchedProtectedRef = DeterministicInvocationProvenance.ProtectedOutputRef(new string('c', 64)),
                ContentDigest = attempt.output_content_digest,
                OutputUtf8 = context.First.Value.OutputUtf8!.Value.ToArray(),
            });

        var outputStore = new PostgresProtectedDeterministicOutputStore(Fixture.Services.ConnectionAccessor);
        var projection = await outputStore.TryLoadProjectionAsync(
            context.Claimed.Ownership.OrganizationId,
            context.Claimed.RequestId,
            context.First.Value.DeterministicAttemptId,
            context.First.Value.OutputContentDigest!,
            "crit.objective.word-count",
            "crit.objective.word-count.v1",
            CancellationToken);

        Assert.Null(projection);
    }

    [Fact]
    public async Task Persist_exact_retry_succeeds_with_single_payload_row()
    {
        var context = await DeterministicPayloadTestSupport.ExecuteAndPersistAsync(Fixture, CancellationToken);
        var outputStore = new PostgresProtectedDeterministicOutputStore(Fixture.Services.ConnectionAccessor);
        var result = context.First.Value!;

        var retry = await outputStore.TryPersistAsync(
            new ProtectedDeterministicOutputPersistCommand(
                context.Claimed.Ownership,
                context.Claimed.RequestId,
                result.DeterministicAttemptId,
                result.ProtectedOutputRef!,
                result.OutputContentDigest!,
                result.OutputUtf8!.Value),
            CancellationToken);

        Assert.True(retry.Succeeded, retry.OutcomeCode);

        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        var count = await connection.QuerySingleAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_deterministic_payloads
            WHERE organization_id = @OrganizationId
              AND deterministic_attempt_id = @DeterministicAttemptId;
            """,
            new
            {
                context.Claimed.Ownership.OrganizationId,
                DeterministicAttemptId = result.DeterministicAttemptId,
            });
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Persist_retry_with_wrong_request_id_fails_deterministic_conflict()
    {
        var context = await DeterministicPayloadTestSupport.ExecuteAndPersistAsync(Fixture, CancellationToken);
        var outputStore = new PostgresProtectedDeterministicOutputStore(Fixture.Services.ConnectionAccessor);
        var result = context.First.Value!;

        var retry = await outputStore.TryPersistAsync(
            new ProtectedDeterministicOutputPersistCommand(
                context.Claimed.Ownership,
                Guid.CreateVersion7(),
                result.DeterministicAttemptId,
                result.ProtectedOutputRef!,
                result.OutputContentDigest!,
                result.OutputUtf8!.Value),
            CancellationToken);

        Assert.False(retry.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, retry.OutcomeCode);
    }

    [Fact]
    public async Task Persist_retry_with_wrong_activity_id_fails_deterministic_conflict()
    {
        var context = await DeterministicPayloadTestSupport.ExecuteAndPersistAsync(Fixture, CancellationToken);
        var outputStore = new PostgresProtectedDeterministicOutputStore(Fixture.Services.ConnectionAccessor);
        var result = context.First.Value!;
        var wrongOwnership = context.Claimed.Ownership with { ActivityId = Guid.CreateVersion7() };

        var retry = await outputStore.TryPersistAsync(
            new ProtectedDeterministicOutputPersistCommand(
                wrongOwnership,
                context.Claimed.RequestId,
                result.DeterministicAttemptId,
                result.ProtectedOutputRef!,
                result.OutputContentDigest!,
                result.OutputUtf8!.Value),
            CancellationToken);

        Assert.False(retry.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, retry.OutcomeCode);
    }

    private sealed record AttemptRow(
        Guid organization_id,
        Guid deterministic_attempt_id,
        Guid request_id,
        string protected_output_ref,
        string output_content_digest);
}
