using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionActorRelationshipStoreTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Stale_set_current_after_revoke_does_not_restore_access()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var actor = SessionPersistenceFixtures.Actor(organization.ActorId);
        var repository = new PostgresSessionRuntimeRepository();
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
        var store = new PostgresSessionActorRelationshipStore(Fixture.Services.ConnectionAccessor);

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, SessionPersistenceFixtures.Actor(organization.ActorId), scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        await store.SetCurrentAsync(
            new SessionActorRelationship(
                binding.Ownership,
                actor.ActorId,
                actor.ActorType,
                SessionEventSubscriptionRelationships.Participant,
                5),
            CancellationToken);
        await store.RevokeCurrentAsync(actor.ActorId, binding.Ownership.SessionId, CancellationToken);
        await store.SetCurrentAsync(
            new SessionActorRelationship(
                binding.Ownership,
                actor.ActorId,
                actor.ActorType,
                SessionEventSubscriptionRelationships.Reviewer,
                2),
            CancellationToken);

        Assert.Null(await store.ResolveCurrentAsync(actor, binding.Ownership.SessionId, CancellationToken));
    }

    [Fact]
    public async Task Newer_set_current_after_revoke_reassigns_access()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var actor = SessionPersistenceFixtures.Actor(organization.ActorId);
        var repository = new PostgresSessionRuntimeRepository();
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
        var store = new PostgresSessionActorRelationshipStore(Fixture.Services.ConnectionAccessor);

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, SessionPersistenceFixtures.Actor(organization.ActorId), scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        await store.SetCurrentAsync(
            new SessionActorRelationship(
                binding.Ownership,
                actor.ActorId,
                actor.ActorType,
                SessionEventSubscriptionRelationships.Participant,
                5),
            CancellationToken);
        await store.RevokeCurrentAsync(actor.ActorId, binding.Ownership.SessionId, CancellationToken);
        await store.SetCurrentAsync(
            new SessionActorRelationship(
                binding.Ownership,
                actor.ActorId,
                actor.ActorType,
                SessionEventSubscriptionRelationships.Reviewer,
                7),
            CancellationToken);

        var subject = await store.ResolveCurrentAsync(actor, binding.Ownership.SessionId, CancellationToken);
        Assert.NotNull(subject);
        Assert.Equal(SessionEventSubscriptionRelationships.Reviewer, subject!.Relationship);
    }
}
