using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Infrastructure;

public sealed class PostgresHostedSessionFrozenTimingSource(
    PostgresConnectionAccessor connections,
    IActivatedCohortPort cohorts,
    IEnrollmentStore enrollments,
    IAccommodationStore accommodations,
    IAccommodationPolicyPort policies) : IHostedSessionFrozenTimingSource
{
    public async Task<HostedFrozenTimingPolicy> LoadAsync(
        Guid organizationId,
        Guid sessionId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<AttemptTimingRow>(
            new CommandDefinition(
                """
                SELECT attempt.enrollment_id AS EnrollmentId,
                       attempt.activity_id AS ActivityId,
                       attempt.cohort_id AS CohortId,
                       baseline.document::text AS BaselineDocument
                FROM submissions_attempts AS attempt
                LEFT JOIN assessment_activation_baselines AS baseline
                    ON baseline.organization_id = attempt.organization_id
                   AND baseline.baseline_id = attempt.baseline_id
                WHERE attempt.organization_id = @OrganizationId
                  AND attempt.session_id = @SessionId
                """,
                new
                {
                    OrganizationId = organizationId,
                    SessionId = sessionId,
                },
                cancellationToken: cancellationToken));
        if (row is null)
        {
            return HostedFrozenTimingPolicy.UnavailablePolicy;
        }

        var enrollment = await enrollments.FindAsync(
            organizationId,
            row.EnrollmentId,
            null,
            cancellationToken);
        var binding = await cohorts.FindActivatedAsync(
            organizationId,
            row.ActivityId,
            row.CohortId,
            cancellationToken);
        var applyEffective = false;
        int? effectiveDuration = null;
        if (enrollment is not null && binding is not null)
        {
            var baseline = TimingMapper.BaselineFrom(binding);
            var policy = await policies.ResolveCurrentAsync(
                organizationId,
                baseline,
                asOfUtc,
                null,
                cancellationToken);
            var records = await accommodations.ListForEnrollmentAsync(
                organizationId,
                enrollment.EnrollmentId,
                null,
                cancellationToken);
            var effective = EffectiveTimingEvaluator.Evaluate(
                baseline,
                enrollment.Status,
                policy,
                records,
                asOfUtc);
            applyEffective = effective.IsAuthoritativeEligibility;
            effectiveDuration = effective.EffectivePerAttemptDurationSeconds;
        }

        return HostedSessionFrozenTiming.Compose(
            row.BaselineDocument,
            effectiveDuration,
            applyEffective);
    }

    private sealed class AttemptTimingRow
    {
        public Guid EnrollmentId { get; init; }

        public Guid ActivityId { get; init; }

        public Guid CohortId { get; init; }

        public string? BaselineDocument { get; init; }
    }
}
