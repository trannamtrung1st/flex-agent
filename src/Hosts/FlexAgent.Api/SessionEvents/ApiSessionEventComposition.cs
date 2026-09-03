using System.Collections.Concurrent;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Submissions.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FlexAgent.Api;

public sealed class SessionEventSubscriptionOptions
{
    public const string SectionName = "SessionEvents:Subscription";

    public TimeSpan AuthorizationRevalidationInterval { get; set; } = TimeSpan.FromSeconds(60);

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(15);
}

public sealed record TrustedInteractiveActor(
    Guid ActorId,
    string ActorType,
    Guid OrganizationId,
    Guid SessionId,
    Guid? ParticipantId,
    string Relationship);

public sealed class MemoryTrustedInteractiveActorDirectory : ISessionEventSubjectSource
{
    private readonly ConcurrentDictionary<(Guid ActorId, Guid SessionId), TrustedInteractiveActor> _actors = new();

    public void Register(TrustedInteractiveActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        _actors[(actor.ActorId, actor.SessionId)] = actor;
    }

    public Task<SessionEventSubject?> ResolveCurrentAsync(
        TrustedRuntimeActor actor,
        Guid untrustedSessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (!_actors.TryGetValue((actor.ActorId, untrustedSessionId), out var registered)
            || !string.Equals(registered.ActorType, actor.ActorType, StringComparison.Ordinal))
        {
            return Task.FromResult<SessionEventSubject?>(null);
        }

        return Task.FromResult<SessionEventSubject?>(new SessionEventSubject(
            registered.ActorId,
            registered.ActorType,
            registered.OrganizationId,
            registered.ParticipantId,
            registered.Relationship));
    }
}

public interface ISessionEventIdentityAdapter
{
    Task<TrustedRuntimeActor?> TryAuthenticateAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default,
        bool advanceActivity = true);
}

public sealed class DisabledSessionEventIdentityAdapter : ISessionEventIdentityAdapter
{
    public static DisabledSessionEventIdentityAdapter Instance { get; } = new();

    public Task<TrustedRuntimeActor?> TryAuthenticateAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default,
        bool advanceActivity = true)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult<TrustedRuntimeActor?>(null);
    }
}

public sealed class DevelopmentHarnessSessionEventIdentityAdapter(IOptions<SessionEventTestIdentityOptions> options)
    : ISessionEventIdentityAdapter
{
    public Task<TrustedRuntimeActor?> TryAuthenticateAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default,
        bool advanceActivity = true)
    {
        ArgumentNullException.ThrowIfNull(request);

        var identity = options.Value;
        var presented = request.Headers[SessionEventEndpointExtensions.TestHarnessKeyHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(identity.HarnessApiKey)
            || !string.Equals(presented, identity.HarnessApiKey, StringComparison.Ordinal))
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

public sealed class Adr002HostedSessionAccess(IAuthorizationKernel authorizationKernel) : IHostedSessionAccess
{
    public async Task<bool> HasCurrentPermissionAsync(
        TrustedRuntimeActor actor,
        Guid organizationId,
        Guid sessionId,
        string action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (actor.ActorId == Guid.Empty
            || organizationId == Guid.Empty
            || sessionId == Guid.Empty
            || string.IsNullOrWhiteSpace(action))
        {
            return false;
        }

        var organization = new OrganizationScope(organizationId);
        var decision = await authorizationKernel.AuthorizeAsync(
            new AuthorizationRequest(
                new TrustedActor(actor.ActorId, actor.ActorType),
                organization,
                action,
                new ResourceScope(organization, AuthorizationResourceTypes.Session, sessionId),
                "http.session_hosted",
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
    public static bool IsEnabled(IHostEnvironment environment, SessionEventTestIdentityOptions options)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);
        if (!(environment.IsDevelopment() || environment.IsEnvironment("Testing")))
        {
            return false;
        }

        return options.Enabled && !string.IsNullOrWhiteSpace(options.HarnessApiKey);
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

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<SessionEventSubscriptionOptions>>().Value);
        services.AddSingleton<IHostedSessionTelemetry, LoggingHostedSessionTelemetry>();

        var connectionString = configuration.GetConnectionString("Sessions");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<ISubscribeAuthorizedSessionEventsHandler>(
                UnhostedSubscribeAuthorizedSessionEventsHandler.Instance);
            services.AddSingleton<IHostedSessionSnapshotQuery>(UnhostedHostedSessionSnapshotQuery.Instance);
            services.AddSingleton<IHostedSessionCommandCoordinator>(UnhostedHostedSessionCommandCoordinator.Instance);
            return;
        }

        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<PostgresConnectionAccessor>();
        services.AddSingleton(provider =>
            new PostgresSessionRuntimeRepository(provider.GetRequiredService<ISessionAttemptTerminalSink>()));
        services.AddSingleton<IReplayAuthorizedSessionEventsHandler, ReplayAuthorizedSessionEventsHandler>();
        services.AddSingleton<IReplayAuthorizedSessionEventsCoordinator, PostgresReplayAuthorizedSessionEventsCoordinator>();
        services.AddSingleton<PostgresSessionActorRelationshipStore>();
        services.AddSingleton<ISessionActorRelationshipStore>(sp =>
            sp.GetRequiredService<PostgresSessionActorRelationshipStore>());
        services.AddSingleton<ISessionEventSubjectSource>(sp =>
            sp.GetRequiredService<PostgresSessionActorRelationshipStore>());
        services.AddSingleton<IHostedSessionSubjectSource, PostgresHostedSessionSubjectSource>();
        services.AddSingleton<ITrustedSessionBindingSource, PostgresTrustedSessionBindingSource>();
        services.AddSingleton<IAuthorizationKernel, PostgresAuthorizationKernel>();
        if (services.All(descriptor => descriptor.ServiceType != typeof(ICommitAuthorizationKernel)))
        {
            services.AddSingleton<ICommitAuthorizationKernel>(sp =>
                (ICommitAuthorizationKernel)sp.GetRequiredService<IAuthorizationKernel>());
        }
        services.AddSingleton<ISessionEventSubscriptionAccess, Adr002SessionEventSubscriptionAccess>();
        services.AddSingleton<ISubscribeAuthorizedSessionEventsHandler, SubscribeAuthorizedSessionEventsHandler>();
        services.AddSingleton<IAcceptParticipantMessageHandler, AcceptParticipantMessageHandler>();
        services.AddSingleton<IChangeSessionLifecycleHandler, ChangeSessionLifecycleHandler>();
        services.AddSingleton<PostgresAcceptParticipantMessageCoordinator>();
        services.AddSingleton<PostgresSessionLifecycleCoordinator>();
        services.AddSingleton<IHostedSessionAccess, Adr002HostedSessionAccess>();
        services.AddSingleton<IHostedSessionFrozenTimingSource, PostgresHostedSessionFrozenTimingSource>();
        services.AddSingleton<IHostedSessionSnapshotQuery, PostgresHostedSessionSnapshotQuery>();
        services.AddSingleton<IHostedSessionCommandCoordinator, PostgresHostedSessionCommandCoordinator>();
        services.AddHealthChecks()
            .AddCheck<SessionsStoreReadinessCheck>("sessions-store", tags: ["ready"]);
    }
}
