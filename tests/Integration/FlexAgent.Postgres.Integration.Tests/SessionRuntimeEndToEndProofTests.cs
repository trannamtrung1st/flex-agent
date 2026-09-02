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

public sealed class SessionRuntimeEndToEndProofTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Readiness_through_completed_seal_exposes_eligible_handoff_and_blocks_post_cutoff_work()
    {
        var ready = await PrepareActiveAsync();
        var actor = ready.Actor;
        var binding = ready.Binding;
        var admit = CreateAdmit(ready);
        var complete = CreateComplete(ready);
        var accept = CreateAccept(ready);
        var publish = CreatePublish(ready);
        var lifecycle = CreateLifecycle(ready);

        var opening = await admit.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                actor,
                binding.Ownership,
                0,
                SessionPersistenceFixtures.OpeningTrigger("trig.e2e.open"),
                "idem.e2e.open",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(opening.Succeeded, opening.OutcomeCode);

        var noAction = await complete.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                opening.SessionVersion!.Value,
                opening.Invocation!.AgentInvocationId,
                new NoActionRecommendation(
                    Guid.NewGuid().ToString("N"),
                    opening.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    NoActionReasonCategories.IntentionalSilence,
                    new NextTimerRecommendation("PT2M", "1")),
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(noAction.Succeeded, noAction.OutcomeCode);

        var participant = await accept.AcceptAsync(
            new AcceptParticipantMessageCommand(
                actor,
                binding.Ownership,
                await ReadVersionAsync(binding.Ownership),
                "msg.e2e.p",
                "turn.e2e.p",
                "slot.e2e.p",
                "trig.e2e.p",
                "idem.e2e.p",
                Guid.NewGuid(),
                "integration.test",
                "synthetic.participant.message"),
            binding,
            CancellationToken);
        Assert.True(participant.Succeeded, participant.OutcomeCode);

        var emit = await complete.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                participant.SessionVersion!.Value,
                participant.Invocation!.AgentInvocationId,
                new EmitMessageRecommendation(
                    Guid.NewGuid().ToString("N"),
                    participant.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 4, TimeSpan.Zero),
                    "participant_reply",
                    "turn.e2e.p",
                    "slot.e2e.p",
                    null),
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(emit.Succeeded, emit.OutcomeCode);

        var fragment = await publish.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                actor,
                binding.Ownership,
                await ReadVersionAsync(binding.Ownership),
                participant.Invocation.AgentInvocationId,
                1,
                "Hello",
                "agen.e2e.1",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(fragment.Succeeded, fragment.OutcomeCode);

        var sealedMessage = await publish.SealAsync(
            new SealAgentResponseCommand(
                actor,
                binding.Ownership,
                await ReadVersionAsync(binding.Ownership),
                participant.Invocation.AgentInvocationId,
                AgentMessageCompletionStates.Complete,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(sealedMessage.Succeeded, sealedMessage.OutcomeCode);

        var paused = await lifecycle.ChangeAsync(
            new ChangeSessionLifecycleCommand(
                actor,
                binding.Ownership,
                await ReadVersionAsync(binding.Ownership),
                SessionLifecycleTransitions.Pause,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(paused.Succeeded, paused.OutcomeCode);
        var resumed = await lifecycle.ChangeAsync(
            new ChangeSessionLifecycleCommand(
                actor,
                binding.Ownership,
                paused.SessionVersion,
                SessionLifecycleTransitions.Resume,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(resumed.Succeeded, resumed.OutcomeCode);

        var completing = await lifecycle.ChangeAsync(
            new ChangeSessionLifecycleCommand(
                actor,
                binding.Ownership,
                resumed.SessionVersion,
                SessionLifecycleTransitions.BeginCompleting,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completing.Succeeded, completing.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Completing, completing.LifecycleState);

        var completed = await lifecycle.ChangeAsync(
            new ChangeSessionLifecycleCommand(
                actor,
                binding.Ownership,
                completing.SessionVersion,
                SessionLifecycleTransitions.Complete,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Completed, completed.LifecycleState);

        SessionRuntime loaded;
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            loaded = (await ready.Repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken))!;
            await scope.CommitAsync(CancellationToken);
        }

        Assert.Contains(
            loaded.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.TimerEventV1);
        Assert.Contains(
            loaded.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.ModelInvocationV1
                && record.PayloadRef.ProtectedRef == opening.Invocation.AgentInvocationId);
        Assert.Contains(
            loaded.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.ModelInvocationV1
                && record.PayloadRef.ProtectedRef == $"{opening.Invocation.AgentInvocationId}.outcome");
        Assert.Contains(
            loaded.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.TranscriptAppendV1
                && record.PayloadRef.ProtectedRef == "msg.e2e.p");
        Assert.Contains(
            loaded.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.TimerEventV1
                && record.PayloadRef.ProtectedRef.EndsWith(".paused", StringComparison.Ordinal));
        Assert.Contains(
            loaded.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.TimerEventV1
                && record.PayloadRef.ProtectedRef.EndsWith(".resumed", StringComparison.Ordinal));
        Assert.True(loaded.VerifyTerminalSeal());
        Assert.Equal(EvaluationHandoffEligibilities.Eligible, loaded.EvaluationHandoff!.Eligibility);
        Assert.Equal(AttemptTerminalMappings.Completed, loaded.TerminalRecord!.AttemptMapping);

        var postCutoff = await admit.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                actor,
                binding.Ownership,
                loaded.SessionVersion,
                SessionPersistenceFixtures.OpeningTrigger("trig.e2e.late"),
                "idem.e2e.late",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.False(postCutoff.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.LifecycleIneligible, postCutoff.OutcomeCode);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var handoff = await connection.QuerySingleAsync<(
            string Eligibility,
            string TerminalState,
            string SealDigest,
            Guid TerminalRecordId,
            string ProcedureId,
            long? CutoffSequence)>(
            """
            SELECT eligibility, terminal_state, seal_digest, terminal_record_id, procedure_id, cutoff_sequence
            FROM session_evaluation_handoffs
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            binding.Ownership);
        Assert.Equal("eligible", handoff.Eligibility);
        Assert.Equal("completed", handoff.TerminalState);
        Assert.Equal(loaded.TerminalRecord.SealDigest, handoff.SealDigest);
        Assert.Equal(loaded.TerminalRecord.TerminalRecordId, handoff.TerminalRecordId);
        Assert.Equal(ManifestSealProcedures.ManifestJcsSha256V2, handoff.ProcedureId);
        Assert.Equal(loaded.TerminalRecord.CutoffSequence, handoff.CutoffSequence);
        Assert.Equal(
            1,
            await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)::int
                FROM session_manifest_refs
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                  AND ref_kind = 'configuration';
                """,
                binding.Ownership));
    }

    [Fact]
    public async Task Aborted_session_seals_but_does_not_create_an_eligible_handoff()
    {
        var ready = await PrepareActiveAsync();
        var aborted = await CreateLifecycle(ready).ChangeAsync(
            new ChangeSessionLifecycleCommand(
                ready.Actor,
                ready.Binding.Ownership,
                0,
                SessionLifecycleTransitions.Abort,
                Guid.NewGuid(),
                "integration.test"),
            ready.Binding,
            CancellationToken);
        Assert.True(aborted.Succeeded, aborted.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Aborted, aborted.LifecycleState);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var handoff = await connection.QuerySingleAsync<(string Eligibility, string TerminalState)>(
            """
            SELECT eligibility, terminal_state
            FROM session_evaluation_handoffs
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            ready.Binding.Ownership);
        Assert.Equal("ineligible", handoff.Eligibility);
        Assert.Equal("aborted", handoff.TerminalState);
    }

    [Fact]
    public async Task Terminated_session_seals_but_does_not_create_an_eligible_handoff()
    {
        var ready = await PrepareActiveAsync();
        var lifecycle = CreateLifecycle(ready);
        var completing = await lifecycle.ChangeAsync(
            new ChangeSessionLifecycleCommand(
                ready.Actor,
                ready.Binding.Ownership,
                0,
                SessionLifecycleTransitions.BeginCompleting,
                Guid.NewGuid(),
                "integration.test"),
            ready.Binding,
            CancellationToken);
        Assert.True(completing.Succeeded, completing.OutcomeCode);

        var terminated = await lifecycle.ChangeAsync(
            new ChangeSessionLifecycleCommand(
                ready.Actor,
                ready.Binding.Ownership,
                completing.SessionVersion,
                SessionLifecycleTransitions.Terminate,
                Guid.NewGuid(),
                "integration.test"),
            ready.Binding,
            CancellationToken);
        Assert.True(terminated.Succeeded, terminated.OutcomeCode);
        Assert.Equal(SessionLifecycleState.Terminated, terminated.LifecycleState);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var handoff = await connection.QuerySingleAsync<(string Eligibility, string TerminalState)>(
            """
            SELECT eligibility, terminal_state
            FROM session_evaluation_handoffs
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            ready.Binding.Ownership);
        Assert.Equal("ineligible", handoff.Eligibility);
        Assert.Equal("terminated", handoff.TerminalState);
        Assert.Equal(
            1,
            await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)::int
                FROM outbox_items
                WHERE organization_id = @OrganizationId
                  AND event_type = @EventType;
                """,
                new
                {
                    ready.Binding.Ownership.OrganizationId,
                    EventType = SessionRuntimeOutboxEventTypes.ManifestSealed,
                }));
    }

    [Fact]
    public async Task Audit_failure_during_terminal_seal_leaves_completing_without_handoff()
    {
        var ready = await PrepareActiveAsync();
        var lifecycle = CreateLifecycle(ready);
        var completing = await lifecycle.ChangeAsync(
            new ChangeSessionLifecycleCommand(
                ready.Actor,
                ready.Binding.Ownership,
                0,
                SessionLifecycleTransitions.BeginCompleting,
                Guid.NewGuid(),
                "integration.test"),
            ready.Binding,
            CancellationToken);
        Assert.True(completing.Succeeded, completing.OutcomeCode);

        var failing = CreateLifecycle(ready, new FaultInjectingAuditEventWriter());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            failing.ChangeAsync(
                new ChangeSessionLifecycleCommand(
                    ready.Actor,
                    ready.Binding.Ownership,
                    completing.SessionVersion,
                    SessionLifecycleTransitions.Complete,
                    Guid.NewGuid(),
                    "integration.test"),
                ready.Binding,
                CancellationToken));

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var lifecycleState = await connection.ExecuteScalarAsync<string>(
            """
            SELECT lifecycle_state
            FROM session_runtimes
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            ready.Binding.Ownership);
        var handoffCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_evaluation_handoffs
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            ready.Binding.Ownership);
        Assert.Equal("completing", lifecycleState);
        Assert.Equal(0, handoffCount);
    }

    [Fact]
    public async Task Eligible_handoff_cannot_be_recorded_for_a_terminated_session()
    {
        var ready = await PrepareActiveAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            connection.ExecuteAsync(
                """
                INSERT INTO session_evaluation_handoffs (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    handoff_id, terminal_record_id, procedure_id, eligibility, terminal_state,
                    cutoff_sequence, configuration_id, configuration_digest, manifest_id, seal_digest)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    'eho.forged000000000000000000000000', @TerminalRecordId,
                    'manifest-jcs-sha256-v2', 'eligible', 'terminated', 1,
                    @ConfigurationId, @ConfigurationDigest, @ManifestId, @SealDigest);
                """,
                new
                {
                    ready.Binding.Ownership.OrganizationId,
                    ready.Binding.Ownership.ActivityId,
                    ready.Binding.Ownership.ParticipantId,
                    ready.Binding.Ownership.AttemptId,
                    ready.Binding.Ownership.SessionId,
                    TerminalRecordId = Guid.NewGuid(),
                    ready.Binding.ConfigurationId,
                    ready.Binding.ConfigurationDigest,
                    ready.Binding.ManifestId,
                    SealDigest = new string('a', 64),
                }));
        Assert.Contains("eligible_completed", error.ConstraintName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task V2_terminal_seal_cannot_omit_cutoff_sequence()
    {
        var ready = await PrepareActiveAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            connection.ExecuteAsync(
                """
                INSERT INTO session_terminal_records (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    terminal_record_id, lifecycle_state, reason_category, attempt_mapping,
                    cutoff_sequence, procedure_id, seal_digest)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @TerminalRecordId, 'completed', 'participant_completed', 'completed',
                    NULL, 'manifest-jcs-sha256-v2', @SealDigest);
                """,
                new
                {
                    ready.Binding.Ownership.OrganizationId,
                    ready.Binding.Ownership.ActivityId,
                    ready.Binding.Ownership.ParticipantId,
                    ready.Binding.Ownership.AttemptId,
                    ready.Binding.Ownership.SessionId,
                    TerminalRecordId = Guid.NewGuid(),
                    SealDigest = new string('a', 64),
                }));
        Assert.Contains("chk_session_terminal_records_seal", error.ConstraintName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Eligible_completed_handoff_cannot_be_recorded_for_an_active_session_without_a_terminal_record()
    {
        var ready = await PrepareActiveAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            connection.ExecuteAsync(
                """
                INSERT INTO session_evaluation_handoffs (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    handoff_id, terminal_record_id, procedure_id, eligibility, terminal_state,
                    cutoff_sequence, configuration_id, configuration_digest, manifest_id, seal_digest)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    'eho.forged000000000000000000000000', @TerminalRecordId,
                    'manifest-jcs-sha256-v2', 'eligible', 'completed', 1,
                    @ConfigurationId, @ConfigurationDigest, @ManifestId, @SealDigest);
                """,
                new
                {
                    ready.Binding.Ownership.OrganizationId,
                    ready.Binding.Ownership.ActivityId,
                    ready.Binding.Ownership.ParticipantId,
                    ready.Binding.Ownership.AttemptId,
                    ready.Binding.Ownership.SessionId,
                    TerminalRecordId = Guid.NewGuid(),
                    ready.Binding.ConfigurationId,
                    ready.Binding.ConfigurationDigest,
                    ready.Binding.ManifestId,
                    SealDigest = new string('a', 64),
                }));
        Assert.True(
            error.ConstraintName is not null
            && (error.ConstraintName.Contains("handoffs_terminal", StringComparison.OrdinalIgnoreCase)
                || error.ConstraintName.Contains("runtime_lifecycle", StringComparison.OrdinalIgnoreCase)),
            error.ConstraintName);
    }

    [Fact]
    public async Task Eligible_handoff_cutoff_must_match_the_sealed_terminal_record()
    {
        var forged = await PrepareCompletedRuntimeWithTerminalAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertEligibleHandoffAsync(
                connection,
                forged,
                cutoffSequence: forged.CutoffSequence + 1,
                sealDigest: forged.SealDigest));
        Assert.Contains("handoffs_terminal", error.ConstraintName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Eligible_handoff_seal_digest_must_match_the_sealed_terminal_record()
    {
        var forged = await PrepareCompletedRuntimeWithTerminalAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertEligibleHandoffAsync(
                connection,
                forged,
                cutoffSequence: forged.CutoffSequence,
                sealDigest: new string('b', 64)));
        Assert.Contains("handoffs_terminal", error.ConstraintName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Eligible_handoff_configuration_id_must_match_the_session_binding()
    {
        var forged = await PrepareCompletedRuntimeWithTerminalAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertEligibleHandoffAsync(
                connection,
                forged,
                cutoffSequence: forged.CutoffSequence,
                sealDigest: forged.SealDigest,
                configurationId: "cfg.forged"));
        Assert.Contains("runtime_configuration", error.ConstraintName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Eligible_handoff_configuration_digest_must_match_the_session_binding()
    {
        var forged = await PrepareCompletedRuntimeWithTerminalAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertEligibleHandoffAsync(
                connection,
                forged,
                cutoffSequence: forged.CutoffSequence,
                sealDigest: forged.SealDigest,
                configurationDigest: new string('c', 64)));
        Assert.Contains("runtime_configuration", error.ConstraintName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Eligible_handoff_manifest_id_must_match_the_session_binding()
    {
        var forged = await PrepareCompletedRuntimeWithTerminalAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            InsertEligibleHandoffAsync(
                connection,
                forged,
                cutoffSequence: forged.CutoffSequence,
                sealDigest: forged.SealDigest,
                manifestId: "man.forged"));
        Assert.Contains("runtime_configuration", error.ConstraintName, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Prepared> PrepareActiveAsync()
    {
        var organization = await Fixture.SeedOrganizationAsync();
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

        return new Prepared(binding, SessionPersistenceFixtures.Actor(organization.ActorId), repository);
    }

    private async Task<ForgedCompletedTerminal> PrepareCompletedRuntimeWithTerminalAsync()
    {
        var ready = await PrepareActiveAsync();
        var terminalRecordId = Guid.NewGuid();
        const long cutoffSequence = 7;
        var sealDigest = new string('a', 64);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO session_terminal_records (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                terminal_record_id, lifecycle_state, reason_category, attempt_mapping,
                cutoff_sequence, procedure_id, seal_digest)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                @TerminalRecordId, 'completed', 'participant_completed', 'completed',
                @CutoffSequence, 'manifest-jcs-sha256-v2', @SealDigest);

            UPDATE session_runtimes
            SET lifecycle_state = 'completed'
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            new
            {
                ready.Binding.Ownership.OrganizationId,
                ready.Binding.Ownership.ActivityId,
                ready.Binding.Ownership.ParticipantId,
                ready.Binding.Ownership.AttemptId,
                ready.Binding.Ownership.SessionId,
                TerminalRecordId = terminalRecordId,
                CutoffSequence = cutoffSequence,
                SealDigest = sealDigest,
            });

        return new ForgedCompletedTerminal(ready.Binding, terminalRecordId, cutoffSequence, sealDigest);
    }

    private static Task InsertEligibleHandoffAsync(
        NpgsqlConnection connection,
        ForgedCompletedTerminal forged,
        long cutoffSequence,
        string sealDigest,
        string? configurationId = null,
        string? configurationDigest = null,
        string? manifestId = null) =>
        connection.ExecuteAsync(
            """
            INSERT INTO session_evaluation_handoffs (
                organization_id, activity_id, participant_id, attempt_id, session_id,
                handoff_id, terminal_record_id, procedure_id, eligibility, terminal_state,
                cutoff_sequence, configuration_id, configuration_digest, manifest_id, seal_digest)
            VALUES (
                @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                'eho.forged000000000000000000000000', @TerminalRecordId,
                'manifest-jcs-sha256-v2', 'eligible', 'completed', @CutoffSequence,
                @ConfigurationId, @ConfigurationDigest, @ManifestId, @SealDigest);
            """,
            new
            {
                forged.Binding.Ownership.OrganizationId,
                forged.Binding.Ownership.ActivityId,
                forged.Binding.Ownership.ParticipantId,
                forged.Binding.Ownership.AttemptId,
                forged.Binding.Ownership.SessionId,
                forged.TerminalRecordId,
                CutoffSequence = cutoffSequence,
                ConfigurationId = configurationId ?? forged.Binding.ConfigurationId,
                ConfigurationDigest = configurationDigest ?? forged.Binding.ConfigurationDigest,
                ManifestId = manifestId ?? forged.Binding.ManifestId,
                SealDigest = sealDigest,
            });

    private PostgresAdmitTrustedTriggerCoordinator CreateAdmit(Prepared ready) =>
        new(Fixture.Services.ConnectionAccessor, ready.Repository, new AdmitTrustedTriggerHandler());

    private PostgresCompleteInvocationCoordinator CreateComplete(Prepared ready) =>
        new(Fixture.Services.ConnectionAccessor, ready.Repository, new CompleteInvocationHandler());

    private PostgresAcceptParticipantMessageCoordinator CreateAccept(Prepared ready) =>
        new(Fixture.Services.ConnectionAccessor, ready.Repository, new AcceptParticipantMessageHandler());

    private PostgresPublishAgentResponseCoordinator CreatePublish(Prepared ready) =>
        new(Fixture.Services.ConnectionAccessor, ready.Repository, new PublishAgentResponseFragmentHandler());

    private PostgresSessionLifecycleCoordinator CreateLifecycle(
        Prepared ready,
        IAuditEventWriter? auditEventWriter = null) =>
        new(
            Fixture.Services.ConnectionAccessor,
            ready.Repository,
            new ChangeSessionLifecycleHandler(),
            auditEventWriter);

    private async Task<long> ReadVersionAsync(SessionOwnership ownership)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        return await connection.ExecuteScalarAsync<long>(
            """
            SELECT session_version
            FROM session_runtimes
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            ownership);
    }

    private sealed record ForgedCompletedTerminal(
        TrustedSessionBinding Binding,
        Guid TerminalRecordId,
        long CutoffSequence,
        string SealDigest);

    private sealed record Prepared(
        TrustedSessionBinding Binding,
        TrustedRuntimeActor Actor,
        PostgresSessionRuntimeRepository Repository);

    private sealed class FaultInjectingAuditEventWriter : IAuditEventWriter
    {
        public Task InsertAsync(
            AuditEventWriteModel auditEvent,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Injected audit failure.");
    }
}
