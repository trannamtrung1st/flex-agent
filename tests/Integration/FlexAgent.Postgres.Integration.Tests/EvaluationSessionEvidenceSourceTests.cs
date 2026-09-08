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
    public async Task Session_evidence_source_materializes_safe_configuration_and_manifest_facts()
    {
        var prepared = await CreatePreparedAsync();
        var bundle = await LoadBundleAsync(prepared);

        Assert.NotNull(bundle);
        Assert.NotNull(bundle!.ConfigurationFact);
        Assert.NotNull(bundle.ManifestFact);
        Assert.Equal(
            EvaluationEvidenceSourceIdentity.ConfigurationFactSourceId(prepared.Request.FrozenInput.ConfigurationId),
            bundle.ConfigurationFact!.SourceId);
        Assert.Equal(
            EvaluationEvidenceSourceIdentity.ManifestFactSourceId(prepared.Request.FrozenInput.ManifestId),
            bundle.ManifestFact!.SourceId);
        Assert.Equal(
            EvaluationEvidenceSourceIdentity.DigestBoundSourceVersion(prepared.Request.FrozenInput.ConfigurationDigest),
            bundle.ConfigurationFact.SourceVersion);
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
    public async Task Agent_transcript_is_reconstructed_from_durable_fragments()
    {
        var prepared = await CreatePreparedAsync();
        var ownership = prepared.Request.FrozenInput.Ownership;
        const string messageId = "msg.eval.agent.fragments";
        const string first = "Thank you ";
        const string second = "for participating.";
        var assembled = first + second;
        var digest = Digest(assembled);
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        await InsertAgentTranscriptWithFragmentsAsync(
            connection,
            ownership,
            messageId,
            sealedSequence: 20,
            fragments: [(1, first), (2, second)],
            contentDigest: digest);

        var bundle = await LoadBundleAsync(prepared);

        Assert.Contains(
            bundle!.TranscriptItemsAtOrBeforeCutoff,
            item => item.MessageId == messageId
                    && item.PublishedSequence == 20
                    && assembled == System.Text.Encoding.UTF8.GetString(item.ExactUtf8.Span));
    }

    [Fact]
    public async Task Agent_transcript_with_fragment_gap_is_not_materialized()
    {
        var prepared = await CreatePreparedAsync();
        var ownership = prepared.Request.FrozenInput.Ownership;
        const string messageId = "msg.eval.agent.gap";
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        await InsertAgentTranscriptWithFragmentsAsync(
            connection,
            ownership,
            messageId,
            sealedSequence: 20,
            fragments: [(2, "missing-first-fragment")],
            contentDigest: Digest("missing-first-fragment"));

        var bundle = await LoadBundleAsync(prepared);

        Assert.DoesNotContain(
            bundle!.TranscriptItemsAtOrBeforeCutoff,
            item => item.MessageId == messageId);
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

    private static async Task InsertAgentTranscriptWithFragmentsAsync(
        NpgsqlConnection connection,
        EvaluationOwnership ownership,
        string messageId,
        long sealedSequence,
        IReadOnlyList<(int Ordinal, string Text)> fragments,
        string contentDigest)
    {
        var turnId = $"turn.{messageId}";
        var responseSlotId = $"slot.{messageId}";
        var generationAttemptId = $"agen.{messageId}";
        var invocationId = $"ainv.{messageId}";
        var decisionId = $"dec.{messageId}";
        var assembled = string.Concat(fragments.OrderBy(fragment => fragment.Ordinal).Select(fragment => fragment.Text));

        await connection.ExecuteAsync(
            """
            INSERT INTO session_turns (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                turn_id, kind, state, trigger_invocation_id, response_slot_id,
                response_slot_state, created_session_sequence)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @TurnId, 'agent_opening', 'accepted', @InvocationId, @ResponseSlotId,
                'claimed_for_publication', @SealedSequence);

            INSERT INTO session_invocations (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, trigger_family, trigger_type, trigger_id, purpose,
                turn_id, response_slot_id, idempotency_key, policy_digest,
                admitted_session_sequence, status)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @InvocationId, 'workflow_event', 'workflow_event.agent_opening', @TriggerId,
                'agent_opening', @TurnId, @ResponseSlotId, @IdempotencyKey, @PolicyDigest,
                @AdmittedSequence, 'decided');

            INSERT INTO session_decisions (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, decision_id, decision_type, produced_at,
                payload_digest, decision_payload_digest_version,
                committed_session_version, committed_session_sequence)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @InvocationId, @DecisionId, 'no_action', TIMESTAMPTZ '2026-08-13T00:00:00Z',
                @PayloadDigest, @DigestVersion,
                1, @CommittedSessionSequence);

            INSERT INTO session_decision_validations (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, revision_ordinal,
                validated_against_session_version, validated_against_session_sequence,
                validation_commit_session_version, validation_commit_session_sequence,
                validation_outcome, effect_outcome, timer_validation_outcome)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @InvocationId, 1,
                0, 1,
                1, 2,
                'accepted', 'not_attempted', 'not_present');

            INSERT INTO session_decision_output_validations (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                agent_invocation_id, revision_ordinal, item_ordinal, local_ref, kind,
                validation_outcome, rejection_reason_category, agent_output_id, effect_outcome)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @InvocationId, 1, 0, 'out.message.primary', 'message',
                'accepted', NULL, @MessageId, 'not_attempted');

            INSERT INTO session_messages (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                message_id, author_type, turn_id, protected_ref, content_digest,
                completion_state, generation_attempt_id, driving_invocation_id, driving_decision_id,
                accepted_agent_output_id, assembled_content_digest, response_slot_id,
                sealed_session_sequence, sealed_at)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @MessageId, 'agent', @TurnId, @ProtectedRef, @ContentDigest,
                'complete', @GenerationAttemptId, @InvocationId, @DecisionId,
                @MessageId, @ContentDigest, @ResponseSlotId,
                @SealedSequence, clock_timestamp());

            INSERT INTO session_visible_transcript_items (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                message_id, author_type, turn_id, protected_ref, content_digest, exact_utf8_text)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @MessageId, 'agent', @TurnId, @ProtectedRef, @ContentDigest, @AssembledText);
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
                ResponseSlotId = responseSlotId,
                GenerationAttemptId = generationAttemptId,
                InvocationId = invocationId,
                DecisionId = decisionId,
                TriggerId = $"{messageId}.trigger",
                IdempotencyKey = $"{messageId}.idem",
                PolicyDigest = new string('p', 64),
                AdmittedSequence = sealedSequence - 1,
                PayloadDigest = new string('d', 64),
                DigestVersion = DecisionPayloadDigest.FormatVersionV1,
                CommittedSessionSequence = sealedSequence,
                ProtectedRef = $"rev.{messageId}",
                ContentDigest = contentDigest,
                SealedSequence = sealedSequence,
                AssembledText = assembled,
            });

        foreach (var fragment in fragments)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO session_message_fragments (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    message_id, fragment_ordinal, session_sequence, turn_id, response_slot_id,
                    generation_attempt_id, protected_ref, content_digest, exact_utf8_text,
                    driving_invocation_id, driving_decision_id)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @MessageId, @FragmentOrdinal, @SessionSequence, @TurnId, @ResponseSlotId,
                    @GenerationAttemptId, @ProtectedRef, @ContentDigest, @ExactUtf8Text,
                    @InvocationId, @DecisionId);
                """,
                new
                {
                    ownership.OrganizationId,
                    ownership.ActivityId,
                    ownership.ParticipantId,
                    ownership.AttemptId,
                    ownership.SessionId,
                    MessageId = messageId,
                    FragmentOrdinal = fragment.Ordinal,
                    SessionSequence = sealedSequence + fragment.Ordinal,
                    TurnId = turnId,
                    ResponseSlotId = responseSlotId,
                    GenerationAttemptId = generationAttemptId,
                    ProtectedRef = $"frag:{messageId}:{fragment.Ordinal}",
                    ContentDigest = Digest(fragment.Text),
                    ExactUtf8Text = fragment.Text,
                    InvocationId = invocationId,
                    DecisionId = decisionId,
                });
        }
    }

    private static string Digest(string text) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
