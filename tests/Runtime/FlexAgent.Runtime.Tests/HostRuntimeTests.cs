using System.Net;
using FlexAgent.Worker;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ApiProgram = FlexAgent.Api.Program;
using WorkerProgram = FlexAgent.Worker.Program;

namespace FlexAgent.Runtime.Tests;

public sealed class ApiRuntimeTests : IClassFixture<WebApplicationFactory<ApiProgram>>
{
    private readonly WebApplicationFactory<ApiProgram> _factory;

    public ApiRuntimeTests(WebApplicationFactory<ApiProgram> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task Api_reports_live_and_ready_health_endpoints()
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var live = await client.GetAsync("/health/live", cancellationToken);
        var ready = await client.GetAsync("/health/ready", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task Api_host_stops_cleanly_on_disposal()
    {
        await using var factory = _factory.WithWebHostBuilder(_ => { });
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live", cancellationToken)).StatusCode);

        await factory.DisposeAsync();
    }

    [Fact]
    public async Task Api_root_returns_development_smoke_payload()
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = await client.GetAsync("/", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("development-smoke", body, StringComparison.Ordinal);
    }
}

public sealed class WorkerRuntimeTests : IClassFixture<WebApplicationFactory<WorkerProgram>>
{
    private readonly WebApplicationFactory<WorkerProgram> _factory;

    public WorkerRuntimeTests(WebApplicationFactory<WorkerProgram> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task Worker_reports_live_and_ready_health_endpoints()
    {
        var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var live = await client.GetAsync("/health/live", cancellationToken);
        var ready = await client.GetAsync("/health/ready", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task Worker_stops_accepting_work_claims_after_shutdown_begins()
    {
        await using var factory = _factory.WithWebHostBuilder(_ => { });
        var gate = factory.Services.GetRequiredService<WorkClaimGate>();

        Assert.True(gate.TryClaimWork());

        await factory.DisposeAsync();

        Assert.False(gate.TryClaimWork());
    }

    [Fact]
    public async Task Worker_ready_endpoint_becomes_unhealthy_when_gate_stops_accepting_work()
    {
        var client = _factory.CreateClient();
        var gate = _factory.Services.GetRequiredService<WorkClaimGate>();
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready", cancellationToken)).StatusCode);

        gate.StopAcceptingWork();

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            (await client.GetAsync("/health/ready", cancellationToken)).StatusCode);
    }
}
