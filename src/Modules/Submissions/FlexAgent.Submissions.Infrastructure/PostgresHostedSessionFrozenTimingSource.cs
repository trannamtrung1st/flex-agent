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
    IAccommodationPolicyPort policies) : IHostedSessionFrozenTimingSource, IFrozenAttemptTimingCapture
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

    public async Task<string> CaptureAsync(
        Guid organizationId,
        Guid enrollmentId,
        Guid activityId,
        Guid cohortId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var baselineDocument = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                """
                SELECT baseline.document::text
                FROM assessment_activation_baselines AS baseline
                INNER JOIN assessment_cohorts AS cohort
                    ON cohort.organization_id = baseline.organization_id
                   AND cohort.baseline_id = baseline.baseline_id
                WHERE baseline.organization_id = @OrganizationId
                  AND cohort.activity_id = @ActivityId
                  AND cohort.cohort_id = @CohortId
                """,
                new
                {
                    OrganizationId = organizationId,
                    ActivityId = activityId,
                    CohortId = cohortId,
                },
                cancellationToken: cancellationToken));

        var enrollment = await enrollments.FindAsync(
            organizationId,
            enrollmentId,
            null,
            cancellationToken);
        var binding = await cohorts.FindActivatedAsync(
            organizationId,
            activityId,
            cohortId,
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

        return HostedSessionFrozenTiming.ToDocumentJson(
            HostedSessionFrozenTiming.Compose(
                baselineDocument,
                effectiveDuration,
                applyEffective));
    }
}
