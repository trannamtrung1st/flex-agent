using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FlexAgent.Api;

internal static class HumanAuthenticationComposition
{
    public static void AddHumanAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var options = BindOptions(configuration, environment);
        services.AddSingleton(options);
        services.AddSingleton(TimeProvider.System);
        services.AddAntiforgery(antiforgery =>
        {
            antiforgery.HeaderName = HumanAuthenticationHostOptions.AntiforgeryHeaderName;
            antiforgery.Cookie.Name = HumanAuthenticationHostOptions.AntiforgeryCookieName;
            antiforgery.Cookie.HttpOnly = true;
            antiforgery.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            antiforgery.Cookie.SameSite = SameSiteMode.Lax;
        });

        if (options.TrustedProxies.Count > 0)
        {
            services.Configure<ForwardedHeadersOptions>(forwarded =>
            {
                forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                forwarded.KnownProxies.Clear();
                forwarded.KnownIPNetworks.Clear();
                foreach (var proxy in options.TrustedProxies)
                {
                    if (System.Net.IPAddress.TryParse(proxy, out var address))
                    {
                        forwarded.KnownProxies.Add(address);
                    }
                }
            });
        }

        var secretDirectory = configuration["HumanAuthentication:SecretDirectory"];
        services.AddSingleton<ISecretSource>(
            string.IsNullOrWhiteSpace(secretDirectory)
                ? new MissingSecretSource()
                : new MountedFileSecretSource(secretDirectory));

        var connectionString = HumanAuthenticationPersistencePolicy.ResolveConnectionString(configuration);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            if (services.All(descriptor => descriptor.ServiceType != typeof(NpgsqlDataSource)))
            {
                services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
                services.AddSingleton<PostgresConnectionAccessor>();
            }
            services.AddSingleton<IDatabaseClock, PostgresDatabaseClock>();
            services.AddSingleton<IHumanIdentityBindingStore, PostgresHumanIdentityBindingStore>();
            services.AddSingleton<IApplicationSessionStore, PostgresApplicationSessionStore>();
            services.AddSingleton<IOidcLoginTransactionStore, PostgresOidcLoginTransactionStore>();
            services.AddSingleton<ILogoutTokenReplayStore, PostgresLogoutTokenReplayStore>();
            services.AddSingleton<IAuthenticationSecurityEventWriter, PostgresAuthenticationSecurityEventWriter>();
            services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
                new ConfigureNamedOptions<KeyManagementOptions>(
                    Options.DefaultName,
                    keyOptions =>
                    {
                        keyOptions.XmlRepository = new PostgresDataProtectionXmlRepository(
                            sp.GetRequiredService<PostgresConnectionAccessor>(),
                            sp.GetRequiredService<ISymmetricPayloadProtector>());
                    }));
            services.AddDataProtection().SetApplicationName("flex-agent-api");
        }
        else
        {
            services.AddSingleton<IDatabaseClock>(_ => new SystemDatabaseClock(TimeProvider.System));
            services.AddSingleton<MemoryHumanIdentityBindingStore>();
            services.AddSingleton<IHumanIdentityBindingStore>(sp => sp.GetRequiredService<MemoryHumanIdentityBindingStore>());
            services.AddSingleton<MemoryApplicationSessionStore>();
            services.AddSingleton<IApplicationSessionStore>(sp => sp.GetRequiredService<MemoryApplicationSessionStore>());
            services.AddSingleton<IOidcLoginTransactionStore, MemoryOidcLoginTransactionStore>();
            services.AddSingleton<ILogoutTokenReplayStore, MemoryLogoutTokenReplayStore>();
            services.AddSingleton<IAuthenticationSecurityEventWriter, MemoryAuthenticationSecurityEventWriter>();
            services.AddDataProtection().SetApplicationName("flex-agent-api");
        }

        services.AddSingleton<ILookupDigestCalculator>(sp =>
        {
            var secrets = sp.GetRequiredService<ISecretSource>();
            var key = secrets.TryReadAsync("application-session-lookup-key").GetAwaiter().GetResult();
            var bytes = string.IsNullOrWhiteSpace(key)
                ? RequireOrFallbackSecret(environment, options.IsComplete, "application-session-lookup-key")
                : System.Text.Encoding.UTF8.GetBytes(key);
            if (bytes.Length < 32)
            {
                bytes = System.Security.Cryptography.SHA256.HashData(bytes);
            }

            return new HmacLookupDigestCalculator(bytes);
        });
        services.AddSingleton<ISymmetricPayloadProtector>(sp =>
        {
            var secrets = sp.GetRequiredService<ISecretSource>();
            var key = secrets.TryReadAsync("oidc-transaction-key").GetAwaiter().GetResult();
            var bytes = string.IsNullOrWhiteSpace(key)
                ? RequireOrFallbackSecret(environment, options.IsComplete, "oidc-transaction-key")
                : System.Text.Encoding.UTF8.GetBytes(key);
            if (bytes.Length != 32)
            {
                bytes = System.Security.Cryptography.SHA256.HashData(bytes);
            }

            return new AesGcmPayloadProtector(bytes);
        });
        services.AddSingleton<IHumanAuthenticationCoordinator>(sp =>
            new HumanAuthenticationCoordinator(
                sp.GetRequiredService<IHumanIdentityBindingStore>(),
                sp.GetRequiredService<IApplicationSessionStore>(),
                sp.GetRequiredService<IAuthenticationSecurityEventWriter>(),
                sp.GetRequiredService<ILookupDigestCalculator>(),
                sp.GetRequiredService<IDatabaseClock>(),
                options.SessionOptions));
        services.AddHttpClient("oidc-jwks", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
            client.MaxResponseContentBufferSize = 65_536;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
        });
        services.AddSingleton<IJwksKeySource>(sp =>
            new CachedJwksKeySource(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("oidc-jwks"),
                sp.GetRequiredService<TimeProvider>(),
                TimeSpan.FromMinutes(5)));
        services.AddHttpClient<IOidcAuthorizationClient, HttpOidcAuthorizationClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
            client.MaxResponseContentBufferSize = 65_536;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
        });
        services.AddSingleton<ISessionEventIdentityAdapter>(sp =>
        {
            var coordinator = sp.GetRequiredService<IHumanAuthenticationCoordinator>();
            var application = new ApplicationSessionEventIdentityAdapter(coordinator);
            if (SessionEventTestIdentity.IsEnabled(environment, configuration))
            {
                return new CompositeSessionEventIdentityAdapter(
                    application,
                    new DevelopmentHarnessSessionEventIdentityAdapter(configuration));
            }

            return options.Enabled ? application : DisabledSessionEventIdentityAdapter.Instance;
        });
    }

    public static HumanAuthenticationHostOptions BindOptions(IConfiguration configuration, IHostEnvironment environment)
    {
        var acceptedAcr = configuration.GetSection("HumanAuthentication:AcceptedAcr").Get<string[]>() ?? [];
        var acceptedAmr = configuration.GetSection("HumanAuthentication:AcceptedAmr").Get<string[]>() ?? [];
        var trustedProxies = configuration.GetSection("HumanAuthentication:TrustedProxies").Get<string[]>() ?? [];
        var requireHttps = !(environment.IsDevelopment() || environment.IsEnvironment("Testing"));
        var parsedSkew = TimeSpan.FromSeconds(int.TryParse(configuration["HumanAuthentication:ClockSkewSeconds"], out var skew) ? skew : 60);
        var parsedIdle = TimeSpan.FromMinutes(int.TryParse(configuration["HumanAuthentication:InactivityMinutes"], out var idle) ? idle : 30);
        var parsedAbsolute = TimeSpan.FromHours(int.TryParse(configuration["HumanAuthentication:AbsoluteLifetimeHours"], out var abs) ? abs : 12);
        return new HumanAuthenticationHostOptions
        {
            Enabled = string.Equals(configuration["HumanAuthentication:Enabled"], "true", StringComparison.OrdinalIgnoreCase),
            Issuer = configuration["HumanAuthentication:Issuer"] ?? string.Empty,
            ClientId = configuration["HumanAuthentication:ClientId"] ?? string.Empty,
            AuthorizationEndpoint = configuration["HumanAuthentication:AuthorizationEndpoint"] ?? string.Empty,
            TokenEndpoint = configuration["HumanAuthentication:TokenEndpoint"] ?? string.Empty,
            JwksUri = configuration["HumanAuthentication:JwksUri"] ?? string.Empty,
            EndSessionEndpoint = configuration["HumanAuthentication:EndSessionEndpoint"],
            RedirectUri = configuration["HumanAuthentication:RedirectUri"] ?? string.Empty,
            LifecycleBridgeKey = configuration["HumanAuthentication:LifecycleBridgeKey"],
            AcceptedAcr = acceptedAcr.ToHashSet(StringComparer.Ordinal),
            AcceptedAmr = acceptedAmr.ToHashSet(StringComparer.Ordinal),
            ClockSkew = OidcValidationProfile.MaximumClockSkew < parsedSkew ? OidcValidationProfile.MaximumClockSkew : parsedSkew,
            Inactivity = ApplicationSessionPolicy.BoundInactivity(parsedIdle),
            AbsoluteLifetime = ApplicationSessionPolicy.BoundAbsoluteLifetime(parsedAbsolute),
            TrustedProxies = trustedProxies,
            RequireHttpsEndpoints = requireHttps,
        };
    }

    private static byte[] RequireOrFallbackSecret(IHostEnvironment environment, bool enabled, string name)
    {
        if (enabled && (environment.IsProduction() || environment.IsEnvironment("Staging")))
        {
            throw new InvalidOperationException($"Required secret '{name}' is not configured.");
        }

        return System.Security.Cryptography.SHA256.HashData("flex-agent-test-only"u8.ToArray());
    }
}

public static class HumanAuthenticationPersistencePolicy
{
    public static string? ResolveConnectionString(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var identity = configuration.GetConnectionString("Identity");
        var sessions = configuration.GetConnectionString("Sessions");
        if (!string.IsNullOrWhiteSpace(identity)
            && !string.IsNullOrWhiteSpace(sessions)
            && !string.Equals(identity, sessions, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Identity differs from ConnectionStrings:Sessions. Split Identity and Sessions databases are not supported; authentication would otherwise bind to the Sessions datasource.");
        }

        return string.IsNullOrWhiteSpace(identity) ? sessions : identity;
    }
}

internal sealed class MissingSecretSource : ISecretSource
{
    public Task<string?> TryReadAsync(string secretName, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}

public sealed class ApplicationSessionEventIdentityAdapter(IHumanAuthenticationCoordinator coordinator)
    : ISessionEventIdentityAdapter
{
    public async Task<TrustedRuntimeActor?> TryAuthenticateAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default,
        bool advanceActivity = true)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.Cookies.TryGetValue(HumanAuthenticationHostOptions.CookieName, out var credential)
            || string.IsNullOrWhiteSpace(credential))
        {
            return null;
        }

        var session = await coordinator.AuthenticateAsync(credential, advanceActivity, cancellationToken)
            .ConfigureAwait(false);
        if (session is null)
        {
            return null;
        }

        request.HttpContext.Items[nameof(AuthenticatedApplicationSession)] = session;
        return new TrustedRuntimeActor(session.ActorId, HumanInteractiveActorTypes.Interactive);
    }
}

public sealed class CompositeSessionEventIdentityAdapter(
    ApplicationSessionEventIdentityAdapter application,
    DevelopmentHarnessSessionEventIdentityAdapter harness) : ISessionEventIdentityAdapter
{
    public async Task<TrustedRuntimeActor?> TryAuthenticateAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default,
        bool advanceActivity = true)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Cookies.ContainsKey(HumanAuthenticationHostOptions.CookieName))
        {
            return await application.TryAuthenticateAsync(request, cancellationToken, advanceActivity)
                .ConfigureAwait(false);
        }

        return await harness.TryAuthenticateAsync(request, cancellationToken, advanceActivity)
            .ConfigureAwait(false);
    }
}
