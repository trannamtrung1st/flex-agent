using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionRuntimeProductionSubscribeTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Authorized_subscribe_replays_postgres_fragments_and_stops_after_grant_revocation()
    {
        var ready = await PrepareReadyToPublishAsync("sub");
        Assert.True((await ready.Publisher.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                ready.Actor,
                ready.Binding.Ownership,
                ready.SessionVersion,
                ready.InvocationId,
                1,
                "secret-a",
                "agen.sub.1",
                Guid.NewGuid(),
                "integration.test"),
            ready.Binding,
            CancellationToken)).Succeeded);

        await GrantSubscribeAsync(ready.OrganizationId, ready.Actor.ActorId);
        var bindings = new MemoryTrustedSessionBindingSource();
        bindings.Register(ready.Binding);
        var access = new KernelSubscribeAccess(
            new PostgresAuthorizationKernel(Fixture.Services.ConnectionAccessor));
        var handler = new SubscribeAuthorizedSessionEventsHandler(
            bindings,
            access,
            new PostgresReplayAuthorizedSessionEventsCoordinator(
                Fixture.Services.ConnectionAccessor,
                ready.Repository,
                new ReplayAuthorizedSessionEventsHandler()));
        var command = new SubscribeAuthorizedSessionEventsCommand(
            ready.Actor,
            ready.Binding.Ownership.OrganizationId,
            ready.Binding.Ownership.ParticipantId,
            SessionEventSubscriptionRelationships.Participant,
            ready.Binding.Ownership.SessionId,
            null);

        var authorized = await handler.AuthorizeAsync(command, CancellationToken);
        var replayed = await handler.ReplayAsync(command, CancellationToken);

        Assert.True(authorized.IsPermitted);
        Assert.True(replayed.Succeeded, replayed.OutcomeCode);
        Assert.Contains(replayed.Events, evt => evt.TextDelta == "secret-a");

        await Fixture.Services.GrantRepository.RevokeAsync(
            ready.OrganizationId,
            ready.Actor.ActorId,
            AuthorizationActions.SubscribeSessionEvents,
            CancellationToken);

        var afterRevoke = await handler.AuthorizeAsync(command, CancellationToken);
        Assert.False(afterRevoke.IsPermitted);
    }

    [Fact]
    public async Task Subscribe_does_not_project_another_session_or_accept_a_stolen_cursor_as_identity()
    {
        var first = await PrepareReadyToPublishAsync("own-a");
        Assert.True((await first.Publisher.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                first.Actor,
                first.Binding.Ownership,
                first.SessionVersion,
                first.InvocationId,
                1,
                "secret-a",
                "agen.own.a",
                Guid.NewGuid(),
                "integration.test"),
            first.Binding,
            CancellationToken)).Succeeded);

        var second = await PrepareReadyToPublishAsync("own-b");
        Assert.True((await second.Publisher.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                second.Actor,
                second.Binding.Ownership,
                second.SessionVersion,
                second.InvocationId,
                1,
                "secret-b",
                "agen.own.b",
                Guid.NewGuid(),
                "integration.test"),
            second.Binding,
            CancellationToken)).Succeeded);

        await GrantSubscribeAsync(first.OrganizationId, first.Actor.ActorId);
        var bindings = new MemoryTrustedSessionBindingSource();
        bindings.Register(first.Binding);
        bindings.Register(second.Binding);
        var handler = new SubscribeAuthorizedSessionEventsHandler(
            bindings,
            new KernelSubscribeAccess(
                new PostgresAuthorizationKernel(Fixture.Services.ConnectionAccessor)),
            new PostgresReplayAuthorizedSessionEventsCoordinator(
                Fixture.Services.ConnectionAccessor,
                first.Repository,
                new ReplayAuthorizedSessionEventsHandler()));

        var guessed = await handler.AuthorizeAsync(
            new SubscribeAuthorizedSessionEventsCommand(
                first.Actor,
                first.Binding.Ownership.OrganizationId,
                first.Binding.Ownership.ParticipantId,
                SessionEventSubscriptionRelationships.Participant,
                second.Binding.Ownership.SessionId,
                "4"),
            CancellationToken);
        var scoped = await handler.ReplayAsync(
            new SubscribeAuthorizedSessionEventsCommand(
                first.Actor,
                first.Binding.Ownership.OrganizationId,
                first.Binding.Ownership.ParticipantId,
                SessionEventSubscriptionRelationships.Participant,
                first.Binding.Ownership.SessionId,
                second.Binding.Ownership.SessionId.ToString("D")),
            CancellationToken);

        Assert.False(guessed.IsPermitted);
        Assert.Equal(SessionEventReplayOutcomeCodes.Reconcile, scoped.OutcomeCode);
        Assert.Empty(scoped.Events);
        Assert.DoesNotContain(scoped.Events, evt => evt.TextDelta == "secret-b");
        Assert.DoesNotContain(scoped.Events, evt => evt.TextDelta == "secret-a");
    }

    private async Task GrantSubscribeAsync(Guid organizationId, Guid actorId)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO actor_organization_grants (
                organization_id, actor_id, relationship_version, granted_action, created_at)
            VALUES (
                @OrganizationId, @ActorId, 1, @GrantedAction, NOW() AT TIME ZONE 'UTC');
            """,
            new
            {
                OrganizationId = organizationId,
                ActorId = actorId,
                GrantedAction = AuthorizationActions.SubscribeSessionEvents,
            });
    }

    private async Task<ReadyPublication> PrepareReadyToPublishAsync(string key)
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = new PostgresSessionRuntimeRepository();
        var actor = SessionPersistenceFixtures.Actor(organization.ActorId);
        var acceptCoordinator = new PostgresAcceptParticipantMessageCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AcceptParticipantMessageHandler());
        var completeCoordinator = new PostgresCompleteInvocationCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new CompleteInvocationHandler());
        var publisher = new PostgresPublishAgentResponseCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new PublishAgentResponseFragmentHandler());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var admitted = await acceptCoordinator.AcceptAsync(
            new AcceptParticipantMessageCommand(
                actor,
                binding.Ownership,
                ExpectedSessionVersion: 0,
                $"msg.p.{key}",
                $"turn.{key}",
                $"slot.{key}",
                $"trig.participant.{key}",
                $"idem.p.{key}",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var completed = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                admitted.SessionVersion!.Value,
                admitted.Invocation!.AgentInvocationId,
                new EnvelopeRecommendation(
                    $"adec.{key}.0001",
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    DecisionDispositions.Respond,
                    [
                        new OutputRecommendation(
                            AgentOutputKinds.Message,
                            "out.message.primary",
                            "participant_reply",
                            $"turn.{key}",
                            $"slot.{key}"),
                    ],
                    []),
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        long sessionVersion;
        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            sessionVersion = loaded!.SessionVersion;
            await scope.CommitAsync(CancellationToken);
        }

        return new ReadyPublication(
            organization.OrganizationId,
            binding,
            actor,
            admitted.Invocation.AgentInvocationId,
            sessionVersion,
            repository,
            publisher);
    }

    private sealed record ReadyPublication(
        Guid OrganizationId,
        TrustedSessionBinding Binding,
        TrustedRuntimeActor Actor,
        string InvocationId,
        long SessionVersion,
        PostgresSessionRuntimeRepository Repository,
        PostgresPublishAgentResponseCoordinator Publisher);

    private sealed class KernelSubscribeAccess(IAuthorizationKernel authorizationKernel) : ISessionEventSubscriptionAccess
    {
        public async Task<bool> HasCurrentSubscribePermissionAsync(
            TrustedRuntimeActor actor,
            Guid organizationId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            var organization = new OrganizationScope(organizationId);
            var decision = await authorizationKernel.AuthorizeAsync(
                new AuthorizationRequest(
                    new TrustedActor(actor.ActorId, actor.ActorType),
                    organization,
                    AuthorizationActions.SubscribeSessionEvents,
                    new ResourceScope(organization, AuthorizationResourceTypes.Session, sessionId),
                    "http.session_events",
                    Guid.NewGuid()),
                cancellationToken);
            return decision.IsPermitted;
        }
    }
}
