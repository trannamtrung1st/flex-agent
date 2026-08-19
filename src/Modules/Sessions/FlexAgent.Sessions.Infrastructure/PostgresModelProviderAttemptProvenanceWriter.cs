using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresModelProviderAttemptProvenanceWriter(PostgresConnectionAccessor connectionAccessor)
    : IModelProviderAttemptProvenanceWriter
{
    private const string InsertSql = """
        INSERT INTO session_invocation_provider_attempts (
            organization_id, activity_id, participant_id, attempt_id, session_id,
            agent_invocation_id, attempt_ordinal, provider_request_id, phase, provider_request_ordinal,
            adapter_kind, adapter_contract_version,
            profile_id, profile_version, profile_digest, requested_model, resolved_model_version,
            outcome_category, input_token_count, output_token_count, provider_request_ref,
            started_at, completed_at)
        VALUES (
            @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
            @AgentInvocationId, @AttemptOrdinal, @ProviderRequestId, @Phase, @ProviderRequestOrdinal,
            @AdapterKind, @AdapterContractVersion,
            @ProfileId, @ProfileVersion, @ProfileDigest, @RequestedModel, @ResolvedModelVersion,
            @OutcomeCategory, @InputTokenCount, @OutputTokenCount, @ProviderRequestRef,
            @StartedAt, @CompletedAt)
        ON CONFLICT (organization_id, session_id, provider_request_id) DO NOTHING;
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
        var providerRequestId = string.IsNullOrWhiteSpace(provenance.ProviderRequestId)
            ? provenance.ProviderRequestRef ?? $"prat.{Guid.NewGuid():N}"
            : provenance.ProviderRequestId;
        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                InsertSql,
                new
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
                    ProviderRequestOrdinal = ProviderRequestOrdinal(
                        invocationAttemptOrdinal,
                        provenance.Phase),
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
                },
                cancellationToken: cancellationToken));
    }

    private static int ProviderRequestOrdinal(int invocationAttemptOrdinal, string? phase)
    {
        var attempt = Math.Max(1, invocationAttemptOrdinal);
        return string.Equals(phase, ModelProviderRequestPhases.Content, StringComparison.Ordinal)
            ? (attempt * 2)
            : (attempt * 2) - 1;
    }
}
