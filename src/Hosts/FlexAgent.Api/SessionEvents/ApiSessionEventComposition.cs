using System.Collections.Concurrent;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Api;

public sealed class SessionEventSubscriptionOptions
{
    public TimeSpan AuthorizationRevalidationInterval { get; init; } = TimeSpan.FromSeconds(60);

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(15);
}

public sealed record TrustedInteractiveActor(
    Guid ActorId,
    string ActorType,
    Guid OrganizationId,
    Guid? ParticipantId,
    string Relationship);

public interface ITrustedInteractiveActorDirectory
{
    bool TryGet(Guid actorId, out TrustedInteractiveActor actor);
}

public sealed class MemoryTrustedInteractiveActorDirectory : ITrustedInteractiveActorDirectory
{
    private readonly ConcurrentDictionary<Guid, TrustedInteractiveActor> _actors = new();

    public void Register(TrustedInteractiveActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        _actors[actor.ActorId] = actor;
    }

    public bool TryGet(Guid actorId, out TrustedInteractiveActor actor) =>
        _actors.TryGetValue(actorId, out actor!);
}

public sealed class Adr002SessionEventSubscriptionAccess(IAuthorizationKernel authorizationKernel)
    : ISessionEventSubscriptionAccess
{
    public async Task<bool> HasCurrentSubscribePermissionAsync(
        TrustedRuntimeActor actor,
        Guid organizationId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (actor.ActorId == Guid.Empty || organizationId == Guid.Empty || sessionId == Guid.Empty)
        {
            return false;
        }

        var organization = new OrganizationScope(organizationId);
        var decision = await authorizationKernel.AuthorizeAsync(
            new AuthorizationRequest(
                new TrustedActor(actor.ActorId, actor.ActorType),
                organization,
                AuthorizationActions.SubscribeSessionEvents,
                new ResourceScope(organization, AuthorizationResourceTypes.Session, sessionId),
                "http.session_events",
                Guid.NewGuid()),
            cancellationToken).ConfigureAwait(false);
        return decision.IsPermitted;
    }
}

internal static class ApiSessionEventComposition
{
    public static void AddProductionSessionEvents(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<MemoryTrustedInteractiveActorDirectory>();
        services.AddSingleton<ITrustedInteractiveActorDirectory>(sp =>
            sp.GetRequiredService<MemoryTrustedInteractiveActorDirectory>());
        services.AddSingleton(new SessionEventSubscriptionOptions());

        var connectionString = configuration.GetConnectionString("Sessions");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<ISubscribeAuthorizedSessionEventsHandler>(
                UnhostedSubscribeAuthorizedSessionEventsHandler.Instance);
            return;
        }

        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<PostgresConnectionAccessor>();
        services.AddSingleton<PostgresSessionRuntimeRepository>();
        services.AddSingleton<IReplayAuthorizedSessionEventsHandler, ReplayAuthorizedSessionEventsHandler>();
        services.AddSingleton<IReplayAuthorizedSessionEventsCoordinator, PostgresReplayAuthorizedSessionEventsCoordinator>();
        services.AddSingleton<MemoryTrustedSessionBindingSource>();
        services.AddSingleton<ITrustedSessionBindingSource>(sp =>
            sp.GetRequiredService<MemoryTrustedSessionBindingSource>());
        services.AddSingleton<IAuthorizationKernel, PostgresAuthorizationKernel>();
        services.AddSingleton<ISessionEventSubscriptionAccess, Adr002SessionEventSubscriptionAccess>();
        services.AddSingleton<ISubscribeAuthorizedSessionEventsHandler, SubscribeAuthorizedSessionEventsHandler>();
    }
}
