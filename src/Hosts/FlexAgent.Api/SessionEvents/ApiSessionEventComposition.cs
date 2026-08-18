using System.Collections.Concurrent;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

public sealed class MemoryTrustedInteractiveActorDirectory : ISessionEventSubjectSource
{
    private readonly ConcurrentDictionary<Guid, TrustedInteractiveActor> _actors = new();

    public void Register(TrustedInteractiveActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        _actors[actor.ActorId] = actor;
    }

    public Task<SessionEventSubject?> GetCurrentAsync(
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        if (!_actors.TryGetValue(actorId, out var actor))
        {
            return Task.FromResult<SessionEventSubject?>(null);
        }

        return Task.FromResult<SessionEventSubject?>(new SessionEventSubject(
            actor.ActorId,
            actor.ActorType,
            actor.OrganizationId,
            actor.ParticipantId,
            actor.Relationship));
    }
}

public interface ISessionEventIdentityAdapter
{
    Task<TrustedRuntimeActor?> TryAuthenticateAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class DisabledSessionEventIdentityAdapter : ISessionEventIdentityAdapter
{
    public static DisabledSessionEventIdentityAdapter Instance { get; } = new();

    public Task<TrustedRuntimeActor?> TryAuthenticateAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult<TrustedRuntimeActor?>(null);
    }
}

public sealed class DevelopmentHarnessSessionEventIdentityAdapter(IConfiguration configuration)
    : ISessionEventIdentityAdapter
{
    public Task<TrustedRuntimeActor?> TryAuthenticateAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var expected = configuration["SessionEvents:TestIdentity:HarnessApiKey"];
        var presented = request.Headers[SessionEventEndpointExtensions.TestHarnessKeyHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(expected)
            || !string.Equals(presented, expected, StringComparison.Ordinal))
        {
            return Task.FromResult<TrustedRuntimeActor?>(null);
        }

        var header = request.Headers[SessionEventEndpointExtensions.TestActorHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header) || !Guid.TryParse(header, out var actorId) || actorId == Guid.Empty)
        {
            return Task.FromResult<TrustedRuntimeActor?>(null);
        }

        return Task.FromResult<TrustedRuntimeActor?>(new TrustedRuntimeActor(actorId, "synthetic.test_actor"));
    }
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

public sealed class SessionsStoreReadinessCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            _ = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("Sessions store is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Sessions store is unreachable.", exception);
        }
    }
}

internal static class SessionEventTestIdentity
{
    public static bool IsEnabled(IHostEnvironment environment, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);
        if (!(environment.IsDevelopment() || environment.IsEnvironment("Testing")))
        {
            return false;
        }

        return string.Equals(
                   configuration["SessionEvents:TestIdentity:Enabled"],
                   "true",
                   StringComparison.OrdinalIgnoreCase)
               && !string.IsNullOrWhiteSpace(configuration["SessionEvents:TestIdentity:HarnessApiKey"]);
    }
}

internal static class ApiSessionEventComposition
{
    public static void AddProductionSessionEvents(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<MemoryTrustedInteractiveActorDirectory>();
        services.AddSingleton<ISessionEventSubjectSource>(sp =>
            sp.GetRequiredService<MemoryTrustedInteractiveActorDirectory>());
        services.AddSingleton(new SessionEventSubscriptionOptions());
        services.AddSingleton<ISessionEventIdentityAdapter>(sp =>
            SessionEventTestIdentity.IsEnabled(environment, configuration)
                ? new DevelopmentHarnessSessionEventIdentityAdapter(configuration)
                : DisabledSessionEventIdentityAdapter.Instance);

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
        services.AddSingleton<ITrustedSessionBindingSource>(_ => FailClosedTrustedSessionBindingSource.Instance);
        services.AddSingleton<IAuthorizationKernel, PostgresAuthorizationKernel>();
        services.AddSingleton<ISessionEventSubscriptionAccess, Adr002SessionEventSubscriptionAccess>();
        services.AddSingleton<ISubscribeAuthorizedSessionEventsHandler, SubscribeAuthorizedSessionEventsHandler>();
        services.AddHealthChecks()
            .AddCheck<SessionsStoreReadinessCheck>("sessions-store", tags: ["ready"]);
    }
}
