using Dapper;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Domain;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionValidationEffectConcurrencyTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Terminal_effect_blocks_concurrent_validation_append()
    {
        var seeded = await SeedAcceptedValidationAsync();
        await using var holdingConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await using var appendingConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await holdingConnection.OpenAsync(CancellationToken);
        await appendingConnection.OpenAsync(CancellationToken);

        await using var holdingTransaction = await holdingConnection.BeginTransactionAsync(CancellationToken);
        var lockedInvocation = await holdingConnection.ExecuteScalarAsync<string>(
            new CommandDefinition(
                """
                SELECT agent_invocation_id
                FROM session_invocations
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                  AND agent_invocation_id = @InvocationId
                FOR UPDATE;
                """,
                seeded,
                holdingTransaction,
                cancellationToken: CancellationToken));
        Assert.Equal(seeded.InvocationId, lockedInvocation);

        await holdingConnection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE session_decision_validations
                SET effect_outcome = 'applied',
                    applied_turn_id = 'turn.agent.inv-1',
                    applied_response_slot_id = 'slot.agent.inv-1',
                    effect_commit_session_version = 2,
                    effect_commit_session_sequence = 3
                WHERE session_id = @SessionId
                  AND agent_invocation_id = @InvocationId
                  AND revision_ordinal = 1;
                """,
                seeded,
                holdingTransaction,
                cancellationToken: CancellationToken));

        var appendTask = Task.Run(async () =>
        {
            await appendingConnection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_decision_validations (
                        organization_id, activity_id, participant_id, attempt_id, session_id,
                        agent_invocation_id, revision_ordinal,
                        validated_against_session_version, validated_against_session_sequence,
                        validation_commit_session_version, validation_commit_session_sequence,
                        validation_outcome, effect_outcome, timer_validation_outcome)
                    VALUES (
                        @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                        @InvocationId, 2,
                        1, 2,
                        2, 3,
                        'rejected', 'not_attempted', 'not_present');
                    """,
                    seeded,
                    cancellationToken: CancellationToken));
        }, CancellationToken);

        await Task.Delay(TimeSpan.FromMilliseconds(200), CancellationToken);
        Assert.False(appendTask.IsCompleted);

        await holdingTransaction.CommitAsync(CancellationToken);
        var appendException = await Assert.ThrowsAsync<PostgresException>(
            () => appendTask.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken));
        Assert.Contains("terminal effect", appendException.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Concurrent_decision_and_execution_outcome_commit_exactly_one()
    {
        var seeded = await SeedInvocationOnlyAsync();
        await using var decisionConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await using var outcomeConnection = new NpgsqlConnection(Fixture.ConnectionString);
        await decisionConnection.OpenAsync(CancellationToken);
        await outcomeConnection.OpenAsync(CancellationToken);

        var decisionTask = Task.Run(async () =>
        {
            await decisionConnection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_decisions (
                        organization_id, activity_id, participant_id, attempt_id, session_id,
                        agent_invocation_id, decision_id, decision_type, produced_at,
                        payload_digest, decision_payload_digest_version,
                        committed_session_version, committed_session_sequence)
                    VALUES (
                        @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                        @InvocationId, @DecisionId, 'no_action', TIMESTAMPTZ '2026-08-13T00:00:00Z',
                        @Digest, @DigestVersion, 1, 2);
                    """,
                    new
                    {
                        seeded.OrganizationId,
                        seeded.ActivityId,
                        seeded.ParticipantId,
                        seeded.AttemptId,
                        seeded.SessionId,
                        seeded.InvocationId,
                        seeded.DecisionId,
                        Digest = new string('b', 64),
                        DigestVersion = DecisionPayloadDigest.FormatVersionV1,
                    },
                    cancellationToken: CancellationToken));
        }, CancellationToken);

        var outcomeTask = Task.Run(async () =>
        {
            await outcomeConnection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_execution_outcomes (
                        organization_id, activity_id, participant_id, attempt_id, session_id,
                        agent_invocation_id, execution_outcome_id, outcome_category, reason_category,
                        committed_session_version, committed_session_sequence)
                    VALUES (
                        @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                        @InvocationId, 'out-1', 'provider_unavailable', 'timeout',
                        1, 2);
                    """,
                    seeded,
                    cancellationToken: CancellationToken));
        }, CancellationToken);

        var results = await Task.WhenAll(
            ObserveAsync(decisionTask),
            ObserveAsync(outcomeTask));

        Assert.Single(results, result => result is null);
        var failure = Assert.Single(results.OfType<PostgresException>());
        Assert.True(
            failure.MessageText.Contains("Decision", StringComparison.OrdinalIgnoreCase)
            || failure.MessageText.Contains("ExecutionOutcome", StringComparison.OrdinalIgnoreCase));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var decisionCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(*)
                FROM session_decisions
                WHERE session_id = @SessionId
                  AND agent_invocation_id = @InvocationId;
                """,
                seeded,
                cancellationToken: CancellationToken));
        var outcomeCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(*)
                FROM session_execution_outcomes
                WHERE session_id = @SessionId
                  AND agent_invocation_id = @InvocationId;
                """,
                seeded,
                cancellationToken: CancellationToken));
        Assert.Equal(1, decisionCount + outcomeCount);
    }

    private static async Task<Exception?> ObserveAsync(Task task)
    {
        try
        {
            await task;
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private async Task<SeededRuntime> SeedInvocationOnlyAsync()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var runtime = new SeededRuntime(
            organization.OrganizationId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "inv-1",
            "dec-1");

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_runtimes (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    configuration_id, configuration_digest, manifest_id, lifecycle_state)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    'cfg-1', @Digest, 'man-1', 'active');
                INSERT INTO session_invocations (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, trigger_family, trigger_type, trigger_id, purpose,
                    idempotency_key, policy_digest, admitted_session_sequence, status)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, 'participant_input', 'participant_message', 'trig-1',
                    'participant_turn.respond', 'idem-1', @Digest, 1, 'admitted');
                """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    runtime.InvocationId,
                    Digest = new string('b', 64),
                },
                cancellationToken: CancellationToken));

        return runtime;
    }

    private async Task<SeededRuntime> SeedAcceptedValidationAsync()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var runtime = new SeededRuntime(
            organization.OrganizationId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "inv-1",
            "dec-1");

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_runtimes (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    configuration_id, configuration_digest, manifest_id, lifecycle_state)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    'cfg-1', @Digest, 'man-1', 'active');
                INSERT INTO session_invocations (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, trigger_family, trigger_type, trigger_id, purpose,
                    idempotency_key, policy_digest, admitted_session_sequence, status)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, 'participant_input', 'participant_message', 'trig-1',
                    'participant_turn.respond', 'idem-1', @Digest, 1, 'admitted');
                INSERT INTO session_decisions (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, decision_id, decision_type, produced_at,
                    payload_digest, decision_payload_digest_version,
                    committed_session_version, committed_session_sequence)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, @DecisionId, 'no_action', TIMESTAMPTZ '2026-08-13T00:00:00Z',
                    @Digest, @DigestVersion, 1, 2);
                INSERT INTO session_decision_validations (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    agent_invocation_id, revision_ordinal,
                    validated_against_session_version, validated_against_session_sequence,
                    validation_commit_session_version, validation_commit_session_sequence,
                    validation_outcome, effect_outcome, timer_validation_outcome)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @InvocationId, 1, 0, 1, 1, 2, 'accepted', 'not_attempted', 'not_present');
                """,
                new
                {
                    runtime.OrganizationId,
                    runtime.ActivityId,
                    runtime.ParticipantId,
                    runtime.AttemptId,
                    runtime.SessionId,
                    runtime.InvocationId,
                    runtime.DecisionId,
                    Digest = new string('b', 64),
                    DigestVersion = DecisionPayloadDigest.FormatVersionV1,
                },
                cancellationToken: CancellationToken));

        return runtime;
    }

    private sealed record SeededRuntime(
        Guid OrganizationId,
        Guid ActivityId,
        Guid ParticipantId,
        Guid AttemptId,
        Guid SessionId,
        string InvocationId,
        string DecisionId);
}
