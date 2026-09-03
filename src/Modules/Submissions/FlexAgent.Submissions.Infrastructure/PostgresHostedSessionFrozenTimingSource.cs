using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using Npgsql;

namespace FlexAgent.Submissions.Infrastructure;

public sealed class PostgresHostedSessionFrozenTimingSource(
    PostgresConnectionAccessor connections) : IHostedSessionFrozenTimingSource, IFrozenAttemptTimingCapture
{
    public async Task<HostedFrozenTimingPolicy> LoadAsync(
        Guid organizationId,
        Guid sessionId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        _ = asOfUtc;
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var document = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                """
                SELECT document::text
                FROM session_frozen_timing
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                """,
                new
                {
                    OrganizationId = organizationId,
                    SessionId = sessionId,
                },
                cancellationToken: cancellationToken));
        return HostedSessionFrozenTiming.FromDocumentJson(document);
    }

    public async Task<FrozenAttemptTimingCaptureResult> CaptureAsync(
        EffectiveTiming effectiveTiming,
        ActivatedCohortBinding binding,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effectiveTiming);
        ArgumentNullException.ThrowIfNull(binding);
        if (commitTransaction is not NpgsqlTransaction transaction)
        {
            throw new ArgumentException("commit.transaction.required", nameof(commitTransaction));
        }

        var baselineDocument = await transaction.Connection!.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                """
                SELECT document::text
                FROM assessment_activation_baselines
                WHERE organization_id = @OrganizationId
                  AND baseline_id = @BaselineId
                """,
                new
                {
                    binding.OrganizationId,
                    binding.BaselineId,
                },
                transaction,
                cancellationToken: cancellationToken));

        return FrozenAttemptTimingCaptureResult.FromDocument(
            HostedSessionFrozenTiming.ToDocumentJson(
                HostedSessionFrozenTiming.ComposeFromEffective(
                    baselineDocument,
                    effectiveTiming.EffectivePerAttemptDurationSeconds,
                    effectiveTiming.IsAuthoritativeEligibility,
                    effectiveTiming.EffectiveAttemptStartExclusiveEndUtc,
                    effectiveTiming.EffectiveSubmissionExclusiveEndUtc)));
    }
}
