using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using FlexAgent.Api;
using FlexAgent.SyntheticBrowser;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API process is running."), tags: ["live", "ready"]);

builder.Services.AddSyntheticBrowser(builder.Configuration);
builder.Services.AddProductionSessionEvents(builder.Configuration, builder.Environment);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "flex-agent-api",
    environment = app.Environment.EnvironmentName,
    status = "development-smoke",
}));

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.MapSyntheticBrowserEndpoints();
app.MapProductionSessionEventEndpoints();

app.Run();
