using Dapper;
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
        var repository = SessionPersistenceFixtures.RuntimeRepository();
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
        await store.RevokeCurrentAsync(Current(binding, actor, SessionEventSubscriptionRelationships.Participant, 6), CancellationToken);
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
        var repository = SessionPersistenceFixtures.RuntimeRepository();
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
        await store.RevokeCurrentAsync(Current(binding, actor, SessionEventSubscriptionRelationships.Participant, 6), CancellationToken);
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

    [Fact]
    public async Task Stale_revoke_after_newer_assignment_does_not_revoke()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var actor = SessionPersistenceFixtures.Actor(organization.ActorId);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
        var store = new PostgresSessionActorRelationshipStore(Fixture.Services.ConnectionAccessor);

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(
                binding.Ownership,
                session,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
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
        await store.SetCurrentAsync(
            new SessionActorRelationship(
                binding.Ownership,
                actor.ActorId,
                actor.ActorType,
                SessionEventSubscriptionRelationships.Reviewer,
                7),
            CancellationToken);
        Assert.False(
            await store.RevokeCurrentAsync(Current(binding, actor, SessionEventSubscriptionRelationships.Participant, 6), CancellationToken));

        var subject = await store.ResolveCurrentAsync(actor, binding.Ownership.SessionId, CancellationToken);
        Assert.NotNull(subject);
        Assert.Equal(SessionEventSubscriptionRelationships.Reviewer, subject!.Relationship);

        Assert.True(
            await store.RevokeCurrentAsync(Current(binding, actor, SessionEventSubscriptionRelationships.Participant, 8), CancellationToken));
        Assert.Null(await store.ResolveCurrentAsync(actor, binding.Ownership.SessionId, CancellationToken));
    }

    [Fact]
    public async Task Newer_revoke_advances_tombstone_so_delayed_lower_set_does_not_restore_access()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var actor = SessionPersistenceFixtures.Actor(organization.ActorId);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
        var store = new PostgresSessionActorRelationshipStore(Fixture.Services.ConnectionAccessor);

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(
                binding.Ownership,
                session,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
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
        await store.RevokeCurrentAsync(Current(binding, actor, SessionEventSubscriptionRelationships.Participant, 6), CancellationToken);
        Assert.True(
            await store.RevokeCurrentAsync(Current(binding, actor, SessionEventSubscriptionRelationships.Participant, 8), CancellationToken));
        await store.SetCurrentAsync(
            new SessionActorRelationship(
                binding.Ownership,
                actor.ActorId,
                actor.ActorType,
                SessionEventSubscriptionRelationships.Reviewer,
                7),
            CancellationToken);

        Assert.Null(await store.ResolveCurrentAsync(actor, binding.Ownership.SessionId, CancellationToken));
    }

    [Fact]
    public async Task Revoke_before_first_set_keeps_delayed_lower_assignment_from_creating_access()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
        var store = new PostgresSessionActorRelationshipStore(Fixture.Services.ConnectionAccessor);
        var reviewerId = Guid.NewGuid();
        var reviewer = SessionPersistenceFixtures.Actor(reviewerId);

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(
                binding.Ownership,
                session,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                "INSERT INTO actors (id, created_at) VALUES (@ActorId, NOW() AT TIME ZONE 'UTC');",
                new { ActorId = reviewerId });
        }

        Assert.True(
            await store.RevokeCurrentAsync(
                Current(binding, reviewer, SessionEventSubscriptionRelationships.Reviewer, 8),
                CancellationToken));
        await store.SetCurrentAsync(
            new SessionActorRelationship(
                binding.Ownership,
                reviewer.ActorId,
                reviewer.ActorType,
                SessionEventSubscriptionRelationships.Reviewer,
                7),
            CancellationToken);

        Assert.Null(await store.ResolveCurrentAsync(reviewer, binding.Ownership.SessionId, CancellationToken));
    }

    private static SessionActorRelationship Current(
        TrustedSessionBinding binding,
        TrustedRuntimeActor actor,
        string relationship,
        long version) =>
        new(binding.Ownership, actor.ActorId, actor.ActorType, relationship, version);
}
