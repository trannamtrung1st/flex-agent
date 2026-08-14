using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Worker;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WorkClaimGate>();
RegisterDurableInvocationProcessing(builder.Services, builder.Configuration);
builder.Services.AddHostedService<WorkerBackgroundService>();
builder.Services.AddHealthChecks()
    .AddCheck<WorkerReadinessCheck>("worker", tags: ["ready"])
    .AddCheck("self", () => HealthCheckResult.Healthy("Worker process is running."), tags: ["live"]);

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.Run();

static void RegisterDurableInvocationProcessing(IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("Sessions");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        services.AddSingleton<IDurableInvocationWorkProcessor, IdleDurableInvocationWorkProcessor>();
        return;
    }

    var dataSource = new PostgresDataSourceFactory().Create(connectionString);
    var accessor = new PostgresConnectionAccessor(dataSource);
    var repository = new PostgresSessionRuntimeRepository();
    var bindingSource = new MemoryTrustedSessionBindingSource();
    var settings = CreateDurableInvocationWorkSettings(configuration);
    services.AddSingleton(dataSource);
    services.AddSingleton(accessor);
    services.AddSingleton(repository);
    services.AddSingleton<ITrustedSessionBindingSource>(bindingSource);
    services.AddSingleton<IDurableInvocationWorkStore, PostgresDurableInvocationWorkStore>();
    services.AddSingleton<IInvocationWorkSessionGateway, PostgresInvocationWorkSessionGateway>();
    services.AddSingleton<IModelExecutionPort, DeterministicFakeModelExecutionAdapter>();
    services.AddSingleton<ICompleteInvocationHandler, CompleteInvocationHandler>();
    services.AddSingleton(settings);
    services.AddSingleton<IDurableInvocationWorkProcessor, DurableInvocationWorkProcessor>();
}

static DurableInvocationWorkSettings CreateDurableInvocationWorkSettings(IConfiguration configuration)
{
    var actorIdValue = configuration["Sessions:WorkerActorId"];
    var actorId = Guid.TryParse(actorIdValue, out var parsed) ? parsed : Guid.Empty;
    var providerId = configuration["Sessions:ProviderId"] ?? "synthetic.provider";
    var bindingReference = configuration["Sessions:CredentialBindingReference"];
    var bindingVersion = configuration["Sessions:CredentialBindingVersion"];
    return new DurableInvocationWorkSettings(
        new TrustedRuntimeActor(actorId, configuration["Sessions:WorkerActorType"] ?? "worker.session_runtime"),
        providerId,
        "worker.session_runtime",
        65_536,
        ownership => new ModelDeploymentCredentialBindingRequest(
            ownership.OrganizationId,
            providerId,
            bindingReference,
            bindingVersion,
            null,
            null,
            false,
            false,
            false));
}
