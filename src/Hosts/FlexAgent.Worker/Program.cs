using FlexAgent.Worker;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WorkClaimGate>();
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
