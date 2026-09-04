using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionHumanGrantScopeTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Activity_steward_cannot_pause_or_terminate_another_activity_session_with_org_wide_grant()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var otherStewardId = await Fixture.SeedWorkerActorAsync();
        var stewardAId = seeded.ActorId;
        var activityAId = await SeedAssessmentActivityAsync(seeded.OrganizationId, stewardAId);
        var activityBId = await SeedAssessmentActivityAsync(seeded.OrganizationId, otherStewardId);
        var sessionB = await SeedActiveSessionAsync(seeded.OrganizationId, activityBId);

        await Fixture.GrantOrganizationActionAsync(
            seeded.OrganizationId,
            stewardAId,
            AuthorizationActions.PauseSession);
        await Fixture.GrantOrganizationActionAsync(
            seeded.OrganizationId,
            stewardAId,
            AuthorizationActions.TerminateSession);

        var kernel = new PostgresAuthorizationKernel(Fixture.Services.ConnectionAccessor);
        var stewardA = new TrustedActor(stewardAId, "synthetic.test_actor");
        var organization = new OrganizationScope(seeded.OrganizationId);
        var resource = new ResourceScope(organization, AuthorizationResourceTypes.Session, sessionB.SessionId);

        var pause = await kernel.AuthorizeAsync(
            new AuthorizationRequest(
                stewardA,
                organization,
                AuthorizationActions.PauseSession,
                resource,
                "integration.test",
                Guid.NewGuid(),
                null,
                activityBId,
                sessionB.ParticipantId,
                sessionB.AttemptId),
            CancellationToken);

        var terminate = await kernel.AuthorizeAsync(
            new AuthorizationRequest(
                stewardA,
                organization,
                AuthorizationActions.TerminateSession,
                resource,
                "integration.test",
                Guid.NewGuid(),
                null,
                activityBId,
                sessionB.ParticipantId,
                sessionB.AttemptId),
            CancellationToken);

        Assert.False(pause.IsPermitted);
        Assert.Equal(AuthorizationReasonCodes.ScopeMismatch, pause.ReasonCode);
        Assert.False(terminate.IsPermitted);
        Assert.Equal(AuthorizationReasonCodes.ScopeMismatch, terminate.ReasonCode);
    }

    [Fact]
    public async Task Activity_steward_can_pause_their_own_activity_session_with_org_wide_grant()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var stewardId = seeded.ActorId;
        var activityId = await SeedAssessmentActivityAsync(seeded.OrganizationId, stewardId);
        var session = await SeedActiveSessionAsync(seeded.OrganizationId, activityId);

        await Fixture.GrantOrganizationActionAsync(
            seeded.OrganizationId,
            stewardId,
            AuthorizationActions.PauseSession);

        var kernel = new PostgresAuthorizationKernel(Fixture.Services.ConnectionAccessor);
        var decision = await kernel.AuthorizeAsync(
            new AuthorizationRequest(
                new TrustedActor(stewardId, "synthetic.test_actor"),
                new OrganizationScope(seeded.OrganizationId),
                AuthorizationActions.PauseSession,
                new ResourceScope(
                    new OrganizationScope(seeded.OrganizationId),
                    AuthorizationResourceTypes.Session,
                    session.SessionId),
                "integration.test",
                Guid.NewGuid(),
                null,
                activityId,
                session.ParticipantId,
                session.AttemptId),
            CancellationToken);

        Assert.True(decision.IsPermitted);
    }

    private async Task<Guid> SeedAssessmentActivityAsync(Guid organizationId, Guid stewardActorId)
    {
        var activityId = Guid.NewGuid();
        var revisionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO assessment_activities (
                organization_id,
                activity_id,
                form,
                configured_type,
                current_revision_id,
                current_revision_number,
                has_activated_cohort,
                created_at,
                updated_at)
            VALUES (
                @OrganizationId,
                @ActivityId,
                'campaign',
                'assessment',
                @RevisionId,
                1,
                false,
                @Now,
                @Now);

            INSERT INTO assessment_activity_revisions (
                organization_id,
                activity_id,
                revision_id,
                revision_number,
                title,
                content,
                created_at,
                actor_id,
                actor_type,
                change_category,
                saved_at)
            VALUES (
                @OrganizationId,
                @ActivityId,
                @RevisionId,
                1,
                'Scope test activity',
                '{}'::jsonb,
                @Now,
                @StewardActorId,
                'synthetic.test_actor',
                'created',
                @Now);
            """,
            new
            {
                OrganizationId = organizationId,
                ActivityId = activityId,
                RevisionId = revisionId,
                StewardActorId = stewardActorId,
                Now = now,
            });

        return activityId;
    }

    private async Task<SessionOwnership> SeedActiveSessionAsync(Guid organizationId, Guid activityId)
    {
        var participantActorId = Guid.NewGuid();
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                "INSERT INTO actors (id, created_at) VALUES (@ActorId, clock_timestamp());",
                new { ActorId = participantActorId });
        }

        var binding = SessionPersistenceFixtures.CreateBinding(
            organizationId,
            activityId: activityId,
            participantId: participantActorId);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var participantActor = SessionPersistenceFixtures.Actor(participantActorId);
        var session = SessionRuntime.CreateActive(
            binding,
            new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        await SessionPersistenceFixtures.InsertActiveAsync(
            repository,
            binding.Ownership,
            session,
            participantActor,
            scope.Transaction,
            CancellationToken);
        await scope.CommitAsync(CancellationToken);

        return binding.Ownership;
    }
}
