using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Worker;

internal static class WorkerDurableWorkSampling
{
    public static void AddDurableWorkSampling(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<ISessionRuntimeTelemetry>(_ => new SessionRuntimeTelemetry());
        var connectionString = configuration.GetConnectionString("Sessions");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IDurableInvocationWorkStore>(UnknownDurableInvocationWorkStore.Instance);
        }
        else
        {
            services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
            services.AddSingleton<PostgresConnectionAccessor>();
            services.AddSingleton<IDurableInvocationWorkStore, PostgresDurableInvocationWorkStore>();
        }

        services.AddSingleton<IDurableWorkBacklogSampler>(sp =>
            new DurableWorkBacklogSampler(
                sp.GetRequiredService<IDurableInvocationWorkStore>(),
                sp.GetRequiredService<ISessionRuntimeTelemetry>()));
    }
}
