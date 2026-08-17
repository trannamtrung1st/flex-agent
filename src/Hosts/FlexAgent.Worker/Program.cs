using FlexAgent.Worker;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WorkClaimGate>();
builder.Services.AddDurableWorkSampling(builder.Configuration);
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
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        var description = report.Entries
            .Select(entry => entry.Value.Description)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
        await context.Response.WriteAsync(description ?? report.Status.ToString());
    },
});

app.Run();
