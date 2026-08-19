using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresModelProviderAttemptProvenanceWriter(
    PostgresConnectionAccessor connectionAccessor,
    TrustedRuntimeActor serviceActor,
    ICommitAuthorizationKernel authorizationKernel,
    IAuthenticatedWorkloadContextSource workloadIdentity)
    : IProviderRequestAdmissionPort
{
    private const string InsertSql = """
        INSERT INTO session_invocation_provider_attempts (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            agent_invocation_id, attempt_ordinal, provider_request_id, phase, provider_request_ordinal,
            adapter_kind, adapter_contract_version,
            profile_id, profile_version, profile_digest, requested_model, resolved_model_version,
            outcome_category, input_token_count, output_token_count, provider_request_ref,
            started_at, completed_at, fact_kind)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @AttemptOrdinal, @ProviderRequestId, @Phase, @ProviderRequestOrdinal,
            @AdapterKind, @AdapterContractVersion,
            @ProfileId, @ProfileVersion, @ProfileDigest, @RequestedModel, @ResolvedModelVersion,
            @OutcomeCategory, @InputTokenCount, @OutputTokenCount, @ProviderRequestRef,
            @StartedAt, @CompletedAt, @FactKind)
        ON CONFLICT (organization_id, session_id, provider_request_id, fact_kind) DO NOTHING;
        """;

    private const string LockOwnedClaimSql = """
        SELECT claim_lease_until
        FROM session_durable_work
        WHERE organization_id = @OrganizationId
          AND session_id = @SessionId
          AND work_id = @WorkId
          AND state = @Claimed
          AND claim_lease_until IS NOT DISTINCT FROM @ClaimLeaseUntil
          AND claim_lease_until > clock_timestamp()
        FOR UPDATE;
        """;

    private const string CountDistinctRequestsSql = """
        SELECT COUNT(DISTINCT provider_request_id)
        FROM session_invocation_provider_attempts
        WHERE organization_id = @OrganizationId
          AND session_id = @SessionId
          AND agent_invocation_id = @AgentInvocationId;
        """;

    private const string RenewOwnedClaimSql = """
        UPDATE session_durable_work
        SET
            claim_lease_until = clock_timestamp() + (@LeaseSeconds * INTERVAL '1 second')
        WHERE organization_id = @OrganizationId
          AND session_id = @SessionId
          AND work_id = @WorkId
          AND state = @Claimed
          AND claim_lease_until IS NOT DISTINCT FROM @ClaimLeaseUntil
        RETURNING claim_lease_until;
        """;

    public async Task WriteAsync(
        SessionOwnership ownership,
        string agentInvocationId,
        int invocationAttemptOrdinal,
        ModelProviderAttemptProvenance provenance,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentInvocationId);
        ArgumentNullException.ThrowIfNull(provenance);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertSql,
                InsertArgs(ownership, agentInvocationId, invocationAttemptOrdinal, provenance),
                cancellationToken: cancellationToken));
    }

    public async Task<int> CountAsync(
        SessionOwnership ownership,
        string agentInvocationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentInvocationId);
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                CountDistinctRequestsSql,
                new
                {
                    ownership.OrganizationId,
                    ownership.SessionId,
                    AgentInvocationId = agentInvocationId,
                },
                cancellationToken: cancellationToken));
    }

    public async Task<ProviderRequestReservationResult> TryReserveAsync(
        DurableInvocationWorkItem claimedWork,
        string agentInvocationId,
        int invocationAttemptOrdinal,
        int maxProviderRequestAttempts,
        ModelProviderAttemptProvenance started,
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claimedWork);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentInvocationId);
        ArgumentNullException.ThrowIfNull(started);
        if (maxProviderRequestAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxProviderRequestAttempts));
        }

        if (lease <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lease));
        }

        var ownership = claimedWork.Ownership;
        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var ownedLease = await scope.Connection.QuerySingleOrDefaultAsync<DateTime?>(
                new CommandDefinition(
                    LockOwnedClaimSql,
                    new
                    {
                        ownership.OrganizationId,
                        ownership.SessionId,
                        claimedWork.WorkId,
                        Claimed = DurableSessionWorkStates.Claimed,
                        ClaimLeaseUntil = claimedWork.ClaimLeaseUntil?.UtcDateTime,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (ownedLease is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return ProviderRequestReservationResult.LostClaim;
            }

            if (!await TryAuthorizeCurrentAsync(scope, ownership, cancellationToken))
            {
                await scope.RollbackAsync(CancellationToken.None);
                return ProviderRequestReservationResult.LostClaim;
            }

            var used = await scope.Connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    CountDistinctRequestsSql,
                    new
                    {
                        ownership.OrganizationId,
                        ownership.SessionId,
                        AgentInvocationId = agentInvocationId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            if (used >= maxProviderRequestAttempts)
            {
                var held = await RenewOwnedClaimAsync(scope, claimedWork, lease, cancellationToken);
                await scope.CommitAsync(cancellationToken);
                return ProviderRequestReservationResult.BudgetExhausted(held);
            }

            await scope.Connection.ExecuteAsync(
                new CommandDefinition(
                    InsertSql,
                    InsertArgs(
                        ownership,
                        agentInvocationId,
                        invocationAttemptOrdinal,
                        started with { FactKind = ModelProviderRequestFacts.Started }),
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            var renewed = await RenewOwnedClaimAsync(scope, claimedWork, lease, cancellationToken);
            await scope.CommitAsync(cancellationToken);
            return ProviderRequestReservationResult.Succeeded(renewed);
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<bool> TryAuthorizeCurrentAsync(
        PostgresTransactionScope scope,
        SessionOwnership ownership,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceActor);
        ArgumentNullException.ThrowIfNull(authorizationKernel);
        ArgumentNullException.ThrowIfNull(workloadIdentity);
        var commitDecision = await SessionInvocationExecuteCommitAuthorization.ReauthorizeAsync(
            authorizationKernel,
            serviceActor,
            ownership,
            Guid.NewGuid(),
            "worker.session_runtime",
            scope.Transaction,
            cancellationToken,
            workloadIdentity);
        return commitDecision.IsPermitted;
    }

    private static async Task<DateTimeOffset?> RenewOwnedClaimAsync(
        PostgresTransactionScope scope,
        DurableInvocationWorkItem claimedWork,
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        var renewed = await scope.Connection.QuerySingleOrDefaultAsync<DateTime?>(
            new CommandDefinition(
                RenewOwnedClaimSql,
                new
                {
                    claimedWork.Ownership.OrganizationId,
                    claimedWork.Ownership.SessionId,
                    claimedWork.WorkId,
                    Claimed = DurableSessionWorkStates.Claimed,
                    ClaimLeaseUntil = claimedWork.ClaimLeaseUntil?.UtcDateTime,
                    LeaseSeconds = lease.TotalSeconds,
                },
                scope.Transaction,
                cancellationToken: cancellationToken));
        return ToUtc(renewed);
    }

    private static object InsertArgs(
        SessionOwnership ownership,
        string agentInvocationId,
        int invocationAttemptOrdinal,
        ModelProviderAttemptProvenance provenance)
    {
        var providerRequestId = string.IsNullOrWhiteSpace(provenance.ProviderRequestId)
            ? provenance.ProviderRequestRef ?? $"prat.{Guid.NewGuid():N}"
            : provenance.ProviderRequestId;
        return new
        {
            ownership.OrganizationId,
            ownership.ActivityId,
            ownership.ParticipantId,
            ownership.AttemptId,
            ownership.SessionId,
            AgentInvocationId = agentInvocationId,
            AttemptOrdinal = invocationAttemptOrdinal,
            ProviderRequestId = providerRequestId,
            Phase = string.IsNullOrWhiteSpace(provenance.Phase)
                ? ModelProviderRequestPhases.Control
                : provenance.Phase,
            ProviderRequestOrdinal = ProviderRequestOrdinal(invocationAttemptOrdinal, provenance.Phase),
            provenance.AdapterKind,
            provenance.AdapterContractVersion,
            provenance.ProfileId,
            provenance.ProfileVersion,
            provenance.ProfileDigest,
            provenance.RequestedModel,
            provenance.ResolvedModelVersion,
            provenance.OutcomeCategory,
            provenance.InputTokenCount,
            provenance.OutputTokenCount,
            provenance.ProviderRequestRef,
            provenance.StartedAt,
            provenance.CompletedAt,
            FactKind = string.IsNullOrWhiteSpace(provenance.FactKind)
                ? ModelProviderRequestFacts.Finished
                : provenance.FactKind,
        };
    }

    private static DateTimeOffset? ToUtc(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        var utc = value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private static int ProviderRequestOrdinal(int invocationAttemptOrdinal, string? phase)
    {
        var attempt = Math.Max(1, invocationAttemptOrdinal);
        return string.Equals(phase, ModelProviderRequestPhases.Content, StringComparison.Ordinal)
            ? (attempt * 2)
            : (attempt * 2) - 1;
    }
}
