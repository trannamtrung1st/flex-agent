using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationSessionEvidenceSourceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Session_evidence_source_loads_authoritative_handoff_and_scoped_transcript()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        var source = CreateSource();

        var handoffId = prepared.Request.FrozenInput.HandoffId;
        var bundle = await source.LoadAsync(
            prepared.Request.FrozenInput.Ownership.OrganizationId,
            prepared.Request.FrozenInput.Ownership.SessionId,
            handoffId,
            CancellationToken);

        Assert.NotNull(bundle);
        Assert.Equal(handoffId, bundle!.Handoff.HandoffId);
        Assert.Equal(42, bundle.Handoff.CutoffSequence);
        Assert.NotNull(bundle.TranscriptItemsAtOrBeforeCutoff);
    }

    [Fact]
    public async Task Participant_transcript_before_cutoff_is_materialized_with_admitted_sequence()
    {
        var prepared = await CreatePreparedAsync();
        var ownership = prepared.Request.FrozenInput.Ownership;
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        await InsertParticipantTranscriptAsync(
            connection,
            ownership,
            messageId: "msg.eval.before",
            turnId: "turn.eval.before",
            invocationId: "ainv.eval.before",
            admittedSequence: 10,
            text: "before cutoff",
            invocationStatus: AgentInvocationStatuses.Admitted);

        var bundle = await LoadBundleAsync(prepared);

        Assert.Contains(
            bundle!.TranscriptItemsAtOrBeforeCutoff,
            item => item.MessageId == "msg.eval.before" && item.PublishedSequence == 10);
    }

    [Fact]
    public async Task Participant_transcript_after_cutoff_is_not_materialized()
    {
        var prepared = await CreatePreparedAsync();
        var ownership = prepared.Request.FrozenInput.Ownership;
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        await InsertParticipantTranscriptAsync(
            connection,
            ownership,
            messageId: "msg.eval.after",
            turnId: "turn.eval.after",
            invocationId: "ainv.eval.after",
            admittedSequence: 43,
            text: "after cutoff",
            invocationStatus: AgentInvocationStatuses.Admitted);

        var bundle = await LoadBundleAsync(prepared);

        Assert.DoesNotContain(
            bundle!.TranscriptItemsAtOrBeforeCutoff,
            item => item.MessageId == "msg.eval.after");
    }

    [Fact]
    public async Task Participant_transcript_without_admitted_sequence_is_not_materialized()
    {
        var prepared = await CreatePreparedAsync();
        var ownership = prepared.Request.FrozenInput.Ownership;
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO session_visible_transcript_items (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                message_id, author_type, turn_id, protected_ref, content_digest, exact_utf8_text)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'msg.eval.orphan', 'participant', NULL, 'rev.orphan',
                @Digest, 'orphan without sequence');
            """,
            new
            {
                ownership.OrganizationId,
                ownership.ActivityId,
                ownership.ParticipantId,
                ownership.AttemptId,
                ownership.SessionId,
                Digest = Digest("orphan without sequence"),
            });

        var bundle = await LoadBundleAsync(prepared);

        Assert.DoesNotContain(
            bundle!.TranscriptItemsAtOrBeforeCutoff,
            item => item.MessageId == "msg.eval.orphan");
    }

    [Fact]
    public async Task Cancelled_participant_transcript_is_not_materialized()
    {
        var prepared = await CreatePreparedAsync();
        var ownership = prepared.Request.FrozenInput.Ownership;
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        await InsertParticipantTranscriptAsync(
            connection,
            ownership,
            messageId: "msg.eval.cancelled",
            turnId: "turn.eval.cancelled",
            invocationId: "ainv.eval.cancelled",
            admittedSequence: 10,
            text: "cancelled participant",
            invocationStatus: AgentInvocationStatuses.Cancelled);

        var bundle = await LoadBundleAsync(prepared);

        Assert.DoesNotContain(
            bundle!.TranscriptItemsAtOrBeforeCutoff,
            item => item.MessageId == "msg.eval.cancelled");
    }

    [Fact]
    public async Task Transcript_outside_frozen_handoff_cutoff_is_not_materialized()
    {
        var prepared = await CreatePreparedAsync();
        var ownership = prepared.Request.FrozenInput.Ownership;
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            """
            UPDATE session_runtimes
            SET cutoff_sequence = 50
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            ownership);
        await InsertParticipantTranscriptAsync(
            connection,
            ownership,
            messageId: "msg.eval.stale-runtime",
            turnId: "turn.eval.stale-runtime",
            invocationId: "ainv.eval.stale-runtime",
            admittedSequence: 10,
            text: "runtime cutoff drift",
            invocationStatus: AgentInvocationStatuses.Admitted);

        var bundle = await LoadBundleAsync(prepared);

        Assert.DoesNotContain(
            bundle!.TranscriptItemsAtOrBeforeCutoff,
            item => item.MessageId == "msg.eval.stale-runtime");
    }

    private async Task<EvaluationPersistenceTestSeed.PreparedEvaluation> CreatePreparedAsync() =>
        await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);

    private PostgresEvaluationSessionEvidenceSource CreateSource() =>
        new(
            Fixture.Services.ConnectionAccessor,
            new PostgresEvaluationHandoffSource(Fixture.Services.ConnectionAccessor));

    private async Task<EvaluationSessionEvidenceBundle?> LoadBundleAsync(
        EvaluationPersistenceTestSeed.PreparedEvaluation prepared)
    {
        var input = prepared.Request.FrozenInput;
        return await CreateSource().LoadAsync(
            input.Ownership.OrganizationId,
            input.Ownership.SessionId,
            input.HandoffId,
            CancellationToken);
    }

    private static async Task InsertParticipantTranscriptAsync(
        NpgsqlConnection connection,
        EvaluationOwnership ownership,
        string messageId,
        string turnId,
        string invocationId,
        long admittedSequence,
        string text,
        string invocationStatus)
    {
        var digest = Digest(text);
        await connection.ExecuteAsync(
            """
            INSERT INTO session_turns (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                turn_id, kind, state, trigger_invocation_id, response_slot_id,
                response_slot_state, created_session_sequence)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @TurnId, 'participant', 'accepted', @InvocationId, @ResponseSlotId,
                'open', @AdmittedSequence);

            INSERT INTO session_invocations (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, trigger_family, trigger_type, trigger_id, purpose,
                turn_id, response_slot_id, idempotency_key, policy_digest,
                admitted_session_sequence, status)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @InvocationId, 'participant_input', 'participant_message', @TriggerId,
                'participant_turn.respond', @TurnId, @ResponseSlotId, @IdempotencyKey,
                @PolicyDigest, @AdmittedSequence, @Status);

            INSERT INTO session_visible_transcript_items (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                message_id, author_type, turn_id, protected_ref, content_digest, exact_utf8_text)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @MessageId, 'participant', @TurnId, @ProtectedRef, @Digest, @Text);
            """,
            new
            {
                ownership.OrganizationId,
                ownership.ActivityId,
                ownership.ParticipantId,
                ownership.AttemptId,
                ownership.SessionId,
                MessageId = messageId,
                TurnId = turnId,
                InvocationId = invocationId,
                TriggerId = $"{turnId}.trigger",
                ResponseSlotId = $"{turnId}.slot",
                IdempotencyKey = $"{messageId}.idem",
                PolicyDigest = new string('p', 64),
                AdmittedSequence = admittedSequence,
                ProtectedRef = $"rev.{messageId}",
                Digest = digest,
                Text = text,
                Status = invocationStatus,
            });
    }

    private static string Digest(string text) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
