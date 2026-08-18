using System.Diagnostics.Metrics;
using System.Net;
using FlexAgent.Api;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Worker;
using Microsoft.AspNetCore.Hosting;
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

    [Fact]
    public void Api_defaults_to_unhosted_session_event_subscription()
    {
        var handler = _factory.Services.GetRequiredService<ISubscribeAuthorizedSessionEventsHandler>();

        Assert.IsType<UnhostedSubscribeAuthorizedSessionEventsHandler>(handler);
    }

    [Fact]
    public void Api_registers_postgres_replay_and_kernel_when_a_sessions_connection_string_is_set()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
        });

        Assert.IsType<SubscribeAuthorizedSessionEventsHandler>(
            factory.Services.GetRequiredService<ISubscribeAuthorizedSessionEventsHandler>());
        Assert.IsType<PostgresReplayAuthorizedSessionEventsCoordinator>(
            factory.Services.GetRequiredService<IReplayAuthorizedSessionEventsCoordinator>());
        Assert.IsType<PostgresTrustedSessionBindingSource>(
            factory.Services.GetRequiredService<ITrustedSessionBindingSource>());
        Assert.IsType<PostgresSessionActorRelationshipStore>(
            factory.Services.GetRequiredService<ISessionEventSubjectSource>());
        Assert.IsType<FlexAgent.IdentityAccess.Infrastructure.PostgresAuthorizationKernel>(
            factory.Services.GetRequiredService<FlexAgent.IdentityAccess.Application.IAuthorizationKernel>());
        Assert.IsType<DisabledSessionEventIdentityAdapter>(
            factory.Services.GetRequiredService<ISessionEventIdentityAdapter>());
    }

    [Fact]
    public async Task Api_ready_is_unhealthy_when_sessions_store_is_configured_but_unavailable()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=127.0.0.1;Port=1;Database=flexagent;Username=flexagent;Password=unused;Timeout=1;Command Timeout=1");
        });
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var ready = await client.GetAsync("/health/ready", cancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
    }
}

public sealed class WorkerRuntimeTests : IClassFixture<WebApplicationFactory<WorkerProgram>>
{
    private static readonly Guid TestWorkerServiceActorId = Guid.Parse("11111111-2222-3333-4444-555555555555");
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
        var readyBody = await ready.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("Worker loop is running. Durable work claiming is not enabled. Timer polling is not enabled.", readyBody, StringComparison.Ordinal);
        Assert.DoesNotContain("accepting work claims", readyBody, StringComparison.Ordinal);
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

    [Fact]
    public void Worker_defaults_to_idle_processor_when_no_sessions_connection_string()
    {
        var processor = _factory.Services.GetRequiredService<IDurableInvocationWorkProcessor>();

        Assert.IsType<IdleDurableInvocationWorkProcessor>(processor);
        Assert.IsType<IdleDurableTimerFireProcessor>(
            _factory.Services.GetRequiredService<IDurableTimerFireProcessor>());
        Assert.IsType<UnknownDurableInvocationWorkStore>(
            _factory.Services.GetRequiredService<IDurableInvocationWorkStore>());
        Assert.IsType<DurableWorkBacklogSampler>(
            _factory.Services.GetRequiredService<IDurableWorkBacklogSampler>());
        Assert.IsType<MeterSessionRuntimeTelemetrySink>(
            _factory.Services.GetRequiredService<ISessionRuntimeTelemetrySink>());
    }

    [Fact]
    public void Worker_publishes_session_runtime_gauges_to_a_meter()
    {
        var observed = new List<double>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == MeterSessionRuntimeTelemetrySink.MeterName
                && instrument.Name == SessionRuntimeTelemetryInstruments.WorkBacklog)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((_, value, _, _) =>
        {
            lock (observed)
            {
                observed.Add(value);
            }
        });
        listener.Start();

        var telemetry = _factory.Services.GetRequiredService<ISessionRuntimeTelemetry>();
        telemetry.RecordGauge(
            SessionRuntimeTelemetryInstruments.WorkBacklog,
            42,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [SessionRuntimeTelemetryLabelKeys.WorkType] = DurableSessionWorkTypes.ExecuteInvocation,
                [SessionRuntimeTelemetryLabelKeys.BacklogBucket] = "n21_to_100",
                [SessionRuntimeTelemetryLabelKeys.PartitionBucket] = "n1",
            });

        lock (observed)
        {
            Assert.Contains(42d, observed);
        }
    }

    [Fact]
    public async Task Worker_samples_backlog_independently_of_claim_polling()
    {
        var sampler = new CountingDurableWorkBacklogSampler();
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IDurableWorkBacklogSampler>(sampler);
            });
        });
        var gate = factory.Services.GetRequiredService<WorkClaimGate>();
        gate.StopAcceptingWork();
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live", cancellationToken)).StatusCode);
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

        Assert.False(gate.TryClaimWork());
        Assert.True(sampler.Calls > 0);
    }

    [Fact]
    public void Worker_keeps_invocation_processing_idle_when_only_a_sessions_connection_string_is_set()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
        });

        var processor = factory.Services.GetRequiredService<IDurableInvocationWorkProcessor>();
        var store = factory.Services.GetRequiredService<IDurableInvocationWorkStore>();
        var timer = factory.Services.GetRequiredService<IDurableTimerFireProcessor>();
        var bindingSource = factory.Services.GetRequiredService<ITrustedSessionBindingSource>();
        var capabilities = factory.Services.GetRequiredService<WorkerRuntimeCapabilities>();

        Assert.IsType<IdleDurableInvocationWorkProcessor>(processor);
        Assert.IsType<UnknownDurableInvocationWorkStore>(store);
        Assert.IsType<IdleDurableTimerFireProcessor>(timer);
        Assert.IsType<PostgresTrustedSessionBindingSource>(bindingSource);
        Assert.Null(factory.Services.GetService<IAgentResponsePublicationPersistPort>());
        Assert.Null(factory.Services.GetService<IModelExecutionPort>());
        Assert.False(capabilities.DurableWorkClaimingEnabled);
        Assert.False(capabilities.TimerPollingEnabled);
    }

    [Fact]
    public void Worker_registers_live_processor_only_when_invocation_processing_is_explicitly_enabled()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
        });

        Assert.IsType<DurableInvocationWorkProcessor>(
            factory.Services.GetRequiredService<IDurableInvocationWorkProcessor>());
        Assert.IsType<PostgresDurableInvocationWorkStore>(
            factory.Services.GetRequiredService<IDurableInvocationWorkStore>());
        Assert.IsType<PostgresPublishAgentResponseCoordinator>(
            factory.Services.GetRequiredService<IAgentResponsePublicationPersistPort>());
        Assert.IsType<FailClosedModelExecutionPort>(
            factory.Services.GetRequiredService<IModelExecutionPort>());
        Assert.True(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().DurableWorkClaimingEnabled);
    }

    [Fact]
    public void Worker_registers_live_processor_when_invocation_processing_is_enabled_in_testing()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
        });

        Assert.IsType<DurableInvocationWorkProcessor>(
            factory.Services.GetRequiredService<IDurableInvocationWorkProcessor>());
        Assert.True(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().DurableWorkClaimingEnabled);
    }

    [Fact]
    public async Task Worker_ready_copy_names_claiming_as_disabled_without_the_explicit_capability()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
        });
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var ready = await client.GetAsync("/health/ready", cancellationToken);
        var readyBody = await ready.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Contains("Worker loop is running. Durable work claiming is not enabled. Timer polling is not enabled.", readyBody, StringComparison.Ordinal);
        Assert.DoesNotContain("accepting work claims", readyBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Worker_ready_copy_names_claiming_when_invocation_processing_is_explicitly_enabled()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
        });
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var ready = await client.GetAsync("/health/ready", cancellationToken);
        var readyBody = await ready.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Contains("Worker loop is running and durable work claiming is enabled. Timer polling is not enabled.", readyBody, StringComparison.Ordinal);
        Assert.DoesNotContain("accepting work claims", readyBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Worker_registers_timer_processor_only_when_timer_polling_is_explicitly_enabled()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:TimerPolling:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
        });

        Assert.IsType<DurableTimerFireProcessor>(
            factory.Services.GetRequiredService<IDurableTimerFireProcessor>());
        Assert.IsType<PostgresFireDueTimerCoordinator>(
            factory.Services.GetRequiredService<IDueTimerFirePort>());
        Assert.IsType<PostgresTrustedSessionBindingSource>(
            factory.Services.GetRequiredService<ITrustedSessionBindingSource>());
        Assert.IsType<IdleDurableInvocationWorkProcessor>(
            factory.Services.GetRequiredService<IDurableInvocationWorkProcessor>());
        Assert.IsType<UnknownDurableInvocationWorkStore>(
            factory.Services.GetRequiredService<IDurableInvocationWorkStore>());
        Assert.Null(factory.Services.GetService<IModelExecutionPort>());
        Assert.IsType<FlexAgent.IdentityAccess.Infrastructure.PostgresAuthorizationKernel>(
            factory.Services.GetRequiredService<FlexAgent.IdentityAccess.Application.IAuthorizationKernel>());
        Assert.True(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().TimerPollingEnabled);
        Assert.False(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().DurableWorkClaimingEnabled);
    }

    [Fact]
    public void Worker_refuses_to_start_timer_polling_without_an_explicit_worker_service_actor_id()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:TimerPolling:Enabled", "true");
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            factory.Services.GetRequiredService<IDurableTimerFireProcessor>());
        Assert.Contains("Sessions:WorkerServiceActorId", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("11111111", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Worker_refuses_to_start_when_invocation_processing_is_enabled_outside_development_and_testing(
        string environmentName)
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environmentName);
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            factory.Services.GetRequiredService<IDurableInvocationWorkProcessor>());
        Assert.Contains("Sessions:InvocationProcessing:Enabled", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Development", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("flexagent", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Worker_refuses_to_start_when_timer_polling_is_enabled_outside_development_and_testing(
        string environmentName)
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environmentName);
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:TimerPolling:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            factory.Services.GetRequiredService<IDurableTimerFireProcessor>());
        Assert.Contains("Sessions:TimerPolling:Enabled", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Development", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("flexagent", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Worker_registers_timer_processor_when_timer_polling_is_enabled_in_testing()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:TimerPolling:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
        });

        Assert.IsType<DurableTimerFireProcessor>(
            factory.Services.GetRequiredService<IDurableTimerFireProcessor>());
        Assert.True(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().TimerPollingEnabled);
    }

    [Fact]
    public void Worker_keeps_invocation_processing_idle_in_production_when_only_a_sessions_connection_string_is_set()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
        });

        Assert.IsType<IdleDurableInvocationWorkProcessor>(
            factory.Services.GetRequiredService<IDurableInvocationWorkProcessor>());
        Assert.IsType<UnknownDurableInvocationWorkStore>(
            factory.Services.GetRequiredService<IDurableInvocationWorkStore>());
        Assert.IsType<IdleDurableTimerFireProcessor>(
            factory.Services.GetRequiredService<IDurableTimerFireProcessor>());
        Assert.IsType<PostgresTrustedSessionBindingSource>(
            factory.Services.GetRequiredService<ITrustedSessionBindingSource>());
        Assert.False(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().DurableWorkClaimingEnabled);
        Assert.False(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().TimerPollingEnabled);
    }

    [Fact]
    public async Task Worker_ready_copy_names_timer_polling_when_the_capability_is_enabled()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:TimerPolling:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
        });
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var ready = await client.GetAsync("/health/ready", cancellationToken);
        var readyBody = await ready.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Contains("Durable work claiming is not enabled", readyBody, StringComparison.Ordinal);
        Assert.Contains("Timer polling is enabled.", readyBody, StringComparison.Ordinal);
        Assert.DoesNotContain("accepting work claims", readyBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Worker_loop_invokes_the_durable_invocation_processor_while_the_claim_gate_allows()
    {
        var processor = new CountingDurableInvocationWorkProcessor();
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IDurableInvocationWorkProcessor>(processor);
            });
        });
        _ = factory.Services.GetRequiredService<WorkClaimGate>();
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live", cancellationToken)).StatusCode);
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

        Assert.True(processor.Calls > 0);
    }

    [Fact]
    public async Task Worker_stays_live_when_durable_invocation_processing_throws()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IDurableInvocationWorkProcessor, ThrowingDurableInvocationWorkProcessor>();
            });
        });
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live", cancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready", cancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Worker_stays_live_when_backlog_sampling_throws()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IDurableWorkBacklogSampler, ThrowingDurableWorkBacklogSampler>();
            });
        });
        var client = factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live", cancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready", cancellationToken)).StatusCode);
    }

    private sealed class ThrowingDurableInvocationWorkProcessor : IDurableInvocationWorkProcessor
    {
        public Task<DurableInvocationWorkProcessResult> TryProcessNextAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("synthetic processing fault");
    }

    private sealed class CountingDurableInvocationWorkProcessor : IDurableInvocationWorkProcessor
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public Task<DurableInvocationWorkProcessResult> TryProcessNextAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult(DurableInvocationWorkProcessResult.Idle);
        }
    }

    private sealed class CountingDurableWorkBacklogSampler : IDurableWorkBacklogSampler
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public Task SampleIfDueAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingDurableWorkBacklogSampler : IDurableWorkBacklogSampler
    {
        public Task SampleIfDueAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("synthetic sampling fault");
    }
}
