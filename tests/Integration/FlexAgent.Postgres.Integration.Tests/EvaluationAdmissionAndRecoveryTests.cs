using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationAdmissionAndRecoveryTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Admission_atomically_creates_request_work_audit_and_outbox()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);

        var result = await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(EvaluationPersistenceOutcomeCodes.Admitted, result.OutcomeCode);
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        var counts = await connection.QuerySingleAsync<(int Requests, int Work, int Audit, int Outbox)>(
            """
            SELECT
                (SELECT COUNT(*) FROM evaluation_requests WHERE request_id = @RequestId)::int,
                (SELECT COUNT(*) FROM evaluation_durable_work WHERE request_id = @RequestId)::int,
                (SELECT COUNT(*) FROM audit_events
                    WHERE resource_id = @RequestId AND action = 'evaluation.request.admit')::int,
                (SELECT COUNT(*) FROM outbox_items
                    WHERE aggregate_id = @RequestId AND event_type = 'evaluation.request.admitted.v1')::int;
            """,
            new { RequestId = prepared.Request.RequestId });
        Assert.Equal((1, 1, 1, 1), counts);
    }

    [Fact]
    public async Task Delivery_and_reconciliation_load_the_authoritative_handoff_into_a_durable_inbox()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        var source = new PostgresEvaluationHandoffSource(Fixture.Services.ConnectionAccessor);
        var inbox = new PostgresEvaluationHandoffInbox(Fixture.Services.ConnectionAccessor);
        var handler = new EvaluationHandoffDeliveryHandler(source, inbox);
        var input = prepared.Request.FrozenInput;

        Assert.True(await handler.HandleAsync(
            Guid.CreateVersion7(),
            input.Ownership.OrganizationId,
            input.Ownership.SessionId,
            CancellationToken));
        Assert.True(await handler.HandleAsync(
            Guid.CreateVersion7(),
            input.Ownership.OrganizationId,
            input.Ownership.SessionId,
            CancellationToken));
        var cursor = await handler.ReconcileAsync(null, 100, CancellationToken);

        Assert.NotNull(cursor);
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM evaluation_handoff_inbox
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND handoff_id = @HandoffId;
            """,
            new
            {
                input.Ownership.OrganizationId,
                input.Ownership.SessionId,
                input.HandoffId,
            }));
    }

    [Fact]
    public async Task Equivalent_retry_reconciles_and_conflicting_input_is_rejected()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        var command = prepared.Command();
        var admitted = await prepared.Admission.AdmitAsync(command, CancellationToken);

        var retry = await prepared.Admission.AdmitAsync(command, CancellationToken);
        var conflictingRequest = prepared.Request with
        {
            FrozenInput = prepared.Request.FrozenInput with
            {
                ManifestDigest = new string('8', 64),
            },
        };
        var conflict = await prepared.Admission.AdmitAsync(
            command with { Request = conflictingRequest },
            CancellationToken);
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                "UPDATE service_delegations SET revoked_at = clock_timestamp() WHERE delegation_id = @DelegationId;",
                new { prepared.DelegationId });
        }
        var revokedRetry = await prepared.Admission.AdmitAsync(command, CancellationToken);

        Assert.True(admitted.Succeeded);
        Assert.True(retry.Succeeded);
        Assert.Equal(EvaluationPersistenceOutcomeCodes.Reconciled, retry.OutcomeCode);
        Assert.Equal(admitted.RequestId, retry.RequestId);
        Assert.False(conflict.Succeeded);
        Assert.Equal(EvaluationPersistenceOutcomeCodes.IdempotencyConflict, conflict.OutcomeCode);
        Assert.False(revokedRetry.Succeeded);
        Assert.Equal(EvaluationPersistenceOutcomeCodes.Denied, revokedRetry.OutcomeCode);
    }

    [Fact]
    public async Task Cross_scope_request_insert_and_frozen_identity_mutation_fail_closed()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);

        var crossScope = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            INSERT INTO evaluation_requests
            SELECT
                organization_id, @NewRequestId, activity_id, @ForeignParticipantId, attempt_id,
                session_id, handoff_id, terminal_record_id, handoff_eligibility, handoff_terminal_state,
                cutoff_sequence, request_kind, @NewDigest, @NewIdempotencyKey,
                delegation_id, state, predecessor_evaluation_id, replacement_reason,
                rubric_source_id, rubric_source_version_id, rubric_content_digest,
                submission_source_id, submission_version_id, submission_content_digest,
                configuration_id, configuration_record_id, configuration_digest,
                manifest_id, manifest_record_id, manifest_digest,
                manifest_seal_procedure_id, terminal_seal_digest,
                model_profile_id, model_profile_version, model_profile_digest, provider_id,
                credential_mode, credential_binding_reference, credential_binding_version,
                evaluator_registry_version, lifecycle_policy_ref, @CorrelationId,
                created_at, completed_at, failure_category
            FROM evaluation_requests
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;
            """,
            new
            {
                NewRequestId = Guid.CreateVersion7(),
                ForeignParticipantId = Guid.CreateVersion7(),
                NewDigest = new string('9', 64),
                NewIdempotencyKey = $"idem.eval.{Guid.CreateVersion7():N}",
                CorrelationId = Guid.CreateVersion7(),
                OrganizationId = prepared.Request.FrozenInput.Ownership.OrganizationId,
                prepared.Request.RequestId,
            }));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, crossScope.SqlState);

        var mutation = await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync(
            """
            UPDATE evaluation_requests
            SET participant_id = @ForeignParticipantId
            WHERE organization_id = @OrganizationId AND request_id = @RequestId;
            """,
            new
            {
                ForeignParticipantId = Guid.CreateVersion7(),
                OrganizationId = prepared.Request.FrozenInput.Ownership.OrganizationId,
                prepared.Request.RequestId,
            }));
        Assert.Contains("immutable", mutation.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Audit_outage_rolls_back_request_and_work()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        var admission = new PostgresEvaluationAdmissionStore(
            Fixture.Services.ConnectionAccessor,
            prepared.Authority,
            new FaultInjectingAuditEventWriter());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => admission.AdmitAsync(prepared.Command(), CancellationToken));

        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM evaluation_requests WHERE request_id = @RequestId;",
            new { prepared.Request.RequestId }));
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM evaluation_durable_work WHERE request_id = @RequestId;",
            new { prepared.Request.RequestId }));
    }

    [Fact]
    public async Task Admission_requires_a_current_exact_evaluation_delegation()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                "UPDATE service_delegations SET revoked_at = clock_timestamp() WHERE delegation_id = @DelegationId;",
                new { prepared.DelegationId });
        }

        var result = await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationPersistenceOutcomeCodes.Denied, result.OutcomeCode);
    }

    [Fact]
    public async Task Concurrent_claims_have_one_winner_and_an_expired_lease_is_recovered()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);

        var claims = await Task.WhenAll(
            prepared.Work.TryClaimAsync(
                prepared.WorkerActorId,
                TimeSpan.FromSeconds(30),
                perOrganizationConcurrency: 1,
                CancellationToken),
            prepared.Work.TryClaimAsync(
                prepared.WorkerActorId,
                TimeSpan.FromSeconds(30),
                perOrganizationConcurrency: 1,
                CancellationToken));
        var first = Assert.Single(claims, item => item is not null)!;

        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                """
                UPDATE evaluation_durable_work
                SET claim_lease_until = clock_timestamp() - interval '1 second'
                WHERE organization_id = @OrganizationId AND work_id = @WorkId;
                """,
                new
                {
                    first.Ownership.OrganizationId,
                    first.WorkId,
                });
        }

        Assert.Null(await prepared.Work.TryRenewAsync(
            first,
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            CancellationToken));
        Assert.False(await prepared.Work.ReleaseForRetryAsync(
            first,
            prepared.WorkerActorId,
            "worker.stale_claim",
            CancellationToken));
        Assert.False(await prepared.Work.MarkCompletedAsync(
            first,
            prepared.WorkerActorId,
            CancellationToken));

        var recovered = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);

        Assert.NotNull(recovered);
        Assert.Equal(first.WorkId, recovered!.WorkId);
        Assert.Equal(2, recovered.AttemptCount);
        await using var verification = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        var attempts = (await verification.QueryAsync<(int Ordinal, string State)>(
            """
            SELECT attempt_ordinal, state
            FROM evaluation_invocation_attempts
            WHERE organization_id = @OrganizationId AND request_id = @RequestId
            ORDER BY attempt_ordinal;
            """,
            new
            {
                first.Ownership.OrganizationId,
                first.RequestId,
            })).AsList();
        Assert.Equal([(1, "failed_retryable"), (2, "running")], attempts);
    }

    [Fact]
    public async Task Retry_release_uses_positive_backoff_and_stale_claim_cannot_complete()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);

        Assert.False(await prepared.Work.MarkCompletedAsync(
            claimed!,
            prepared.WorkerActorId,
            CancellationToken));
        Assert.True(await prepared.Work.ReleaseForRetryAsync(
            claimed,
            prepared.WorkerActorId,
            "provider.timeout",
            CancellationToken));
        Assert.False(await prepared.Work.MarkCompletedAsync(
            claimed,
            prepared.WorkerActorId,
            CancellationToken));
        Assert.Null(await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken));
    }

    [Fact]
    public async Task Revoked_delegation_blocks_renew_retry_and_completion()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                "UPDATE service_delegations SET revoked_at = clock_timestamp() WHERE delegation_id = @DelegationId;",
                new { prepared.DelegationId });
        }

        Assert.Null(await prepared.Work.TryRenewAsync(
            claimed!,
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            CancellationToken));
        Assert.False(await prepared.Work.ReleaseForRetryAsync(
            claimed,
            prepared.WorkerActorId,
            "provider.timeout",
            CancellationToken));
        Assert.False(await prepared.Work.MarkCompletedAsync(
            claimed,
            prepared.WorkerActorId,
            CancellationToken));
    }

    [Fact]
    public async Task Retry_at_attempt_limit_requires_exhaustion_transition()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                """
                UPDATE evaluation_durable_work
                SET max_attempts = attempt_count
                WHERE organization_id = @OrganizationId AND work_id = @WorkId;
                """,
                new
                {
                    claimed!.Ownership.OrganizationId,
                    claimed.WorkId,
                });
        }

        Assert.False(await prepared.Work.ReleaseForRetryAsync(
            claimed,
            prepared.WorkerActorId,
            "provider.timeout",
            CancellationToken));
        Assert.True(await prepared.Work.MarkExhaustedAsync(
            claimed,
            prepared.WorkerActorId,
            "provider.exhausted",
            CancellationToken));
    }

    [Fact]
    public async Task Lost_completion_acknowledgement_is_reconciled_without_reexecution()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);
        var evaluationId = Guid.CreateVersion7();
        var evidenceSetId = Guid.CreateVersion7();
        await using (var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken))
        await using (var transaction = await connection.BeginTransactionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO evaluation_evidence_sets (
                    organization_id, evaluation_id, evidence_set_id, request_id,
                    invocation_attempt_id, seal_schema, seal_digest, sealed_at, sealed_by_service)
                VALUES (
                    @OrganizationId, @EvaluationId, @EvidenceSetId, @RequestId,
                    @InvocationAttemptId, 'evidence-set-jcs-sha256-v1', @Digest,
                    clock_timestamp(), 'evaluation.synthetic');

                INSERT INTO evaluations (
                    organization_id, evaluation_id, request_id, activity_id, participant_id,
                    attempt_id, session_id, evidence_set_id, procedure_source_id,
                    procedure_source_version_id, procedure_digest, aggregate_status,
                    creation_service_id, completed_at, predecessor_evaluation_id)
                SELECT
                    organization_id, @EvaluationId, request_id, activity_id, participant_id,
                    attempt_id, session_id, @EvidenceSetId, rubric_source_id,
                    rubric_source_version_id, rubric_content_digest, 'complete',
                    'evaluation.synthetic', clock_timestamp(), predecessor_evaluation_id
                FROM evaluation_requests
                WHERE organization_id = @OrganizationId AND request_id = @RequestId;

                UPDATE evaluation_requests
                SET state = 'completed', completed_at = clock_timestamp()
                WHERE organization_id = @OrganizationId AND request_id = @RequestId;

                UPDATE evaluation_durable_work
                SET claim_lease_until = clock_timestamp() - interval '1 second'
                WHERE organization_id = @OrganizationId AND work_id = @WorkId;
                """,
                new
                {
                    claimed!.Ownership.OrganizationId,
                    EvaluationId = evaluationId,
                    EvidenceSetId = evidenceSetId,
                    claimed.RequestId,
                    claimed.InvocationAttemptId,
                    Digest = new string('7', 64),
                    claimed.WorkId,
                },
                transaction);
            await transaction.CommitAsync(CancellationToken);
        }

        var next = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);

        Assert.Null(next);
        await using var verification = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        var state = await verification.QuerySingleAsync<(string WorkState, string AttemptState)>(
            """
            SELECT work.state, attempt.state
            FROM evaluation_durable_work AS work
            INNER JOIN evaluation_invocation_attempts AS attempt
              ON attempt.organization_id = work.organization_id
             AND attempt.request_id = work.request_id
            WHERE work.organization_id = @OrganizationId AND work.work_id = @WorkId;
            """,
            new
            {
                claimed.Ownership.OrganizationId,
                claimed.WorkId,
            });
        Assert.Equal(("completed", "completed"), state);
    }

    private sealed class FaultInjectingAuditEventWriter : IAuditEventWriter
    {
        public Task InsertAsync(
            AuditEventWriteModel auditEvent,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Injected audit outage.");
    }
}
