using Dapper;
using FlexAgent.Configuration;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Postgres.Integration.Tests.Support;

/// <summary>
/// Seeds a consumed active Attempt plus Enrollment in the Compose demo org so the
/// running Worker can map Session terminalization through
/// <see cref="Submissions.Infrastructure.SubmissionsSessionAttemptTerminalSink"/>.
/// </summary>
internal static class ComposeProbeSubmissionSeed
{
    internal static readonly Guid DemoOrganizationId =
        Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");

    private static readonly Guid DemoActivityId =
        Guid.Parse("a1000000-0000-4000-8000-000000000025");

    private static readonly Guid DemoCohortId =
        Guid.Parse("c1000000-0000-4000-8000-000000000025");

    private static readonly Guid DemoBaselineId =
        Guid.Parse("d1000000-0000-4000-8000-000000000025");

    private static readonly Guid DemoTaskSourceId =
        Guid.Parse("22222222-2222-2222-2222-222222222209");

    private static readonly Guid DemoTaskVersionId =
        Guid.Parse("33333333-3333-3333-3333-333333333309");

    private static readonly Guid DemoLifecyclePolicyId =
        Guid.Parse("11111111-1111-4111-8111-111111111118");

    private static readonly Guid DemoAssignedByActorId =
        Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");

    private const string DemoTaskDigest = "jjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjjj";

    internal sealed record ProbeAttemptContext(
        TrustedSessionBinding Binding,
        Guid ParticipantActorId,
        Guid EnrollmentId,
        Guid AttemptId);

    internal static async Task<ProbeAttemptContext> SeedDueSessionAsync(
        ConfigurationServiceCollection.ServiceBundle services,
        CancellationToken cancellationToken)
    {
        var participantActorId = Guid.CreateVersion7();
        var enrollmentId = Guid.CreateVersion7();
        var attemptId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var resolvedConfigurationId = Guid.CreateVersion7();
        var initialManifestId = Guid.CreateVersion7();
        var binding = SessionPersistenceFixtures.CreateBinding(
            DemoOrganizationId,
            cooldownSeconds: 0,
            activityId: DemoActivityId,
            attemptId: attemptId,
            sessionId: sessionId,
            participantId: participantActorId);
        var digest = binding.ConfigurationDigest;
        var now = DateTimeOffset.UtcNow;

        await using var connection = await services.ConnectionAccessor.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO actors (id, created_at) VALUES (@ActorId, @CreatedAt);

            INSERT INTO submissions_enrollments (
                organization_id, enrollment_id, activity_id, cohort_id, baseline_id,
                task_source_id, task_version_id, task_content_digest, lifecycle_policy_id,
                lifecycle_policy_version, participant_actor_id, status, revision,
                assigned_by_actor_id, assigned_at, updated_at)
            VALUES (
                @OrganizationId, @EnrollmentId, @ActivityId, @CohortId, @BaselineId,
                @TaskSourceId, @TaskVersionId, @TaskDigest, @LifecyclePolicyId,
                1, @ParticipantActorId, 'active', 1,
                @AssignedByActorId, @Now, @Now);

            INSERT INTO submissions_attempts (
                organization_id, attempt_id, activity_id, cohort_id, baseline_id, enrollment_id,
                participant_actor_id, task_source_id, ordinal, entitlement_source, retry_entitlement_id,
                status, consumed, requested_at, started_at, terminal_at, terminal_reason_category,
                session_id, resolved_configuration_id, initial_manifest_id, configuration_digest, manifest_digest)
            VALUES (
                @OrganizationId, @AttemptId, @ActivityId, @CohortId, @BaselineId, @EnrollmentId,
                @ParticipantActorId, @TaskSourceId, 1, 'baseline', NULL,
                'active', TRUE, @Now, @Now, NULL, NULL,
                @SessionId, @ResolvedConfigurationId, @InitialManifestId, @Digest, @Digest);
            """,
            new
            {
                OrganizationId = DemoOrganizationId,
                ActorId = participantActorId,
                CreatedAt = now,
                EnrollmentId = enrollmentId,
                ActivityId = DemoActivityId,
                CohortId = DemoCohortId,
                BaselineId = DemoBaselineId,
                TaskSourceId = DemoTaskSourceId,
                TaskVersionId = DemoTaskVersionId,
                TaskDigest = DemoTaskDigest,
                LifecyclePolicyId = DemoLifecyclePolicyId,
                ParticipantActorId = participantActorId,
                AssignedByActorId = DemoAssignedByActorId,
                AttemptId = attemptId,
                SessionId = sessionId,
                ResolvedConfigurationId = resolvedConfigurationId,
                InitialManifestId = initialManifestId,
                Digest = digest,
                Now = now,
            });

        return new ProbeAttemptContext(binding, participantActorId, enrollmentId, attemptId);
    }
}
