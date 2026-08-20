using System.Diagnostics.Metrics;
using System.Net;
using FlexAgent.Api;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Sessions.OpenAiCompatible;
using FlexAgent.Sessions.OpenRouter;
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
        Assert.IsType<PostgresModelProviderAttemptProvenanceWriter>(
            factory.Services.GetRequiredService<IProviderRequestAdmissionPort>());
        Assert.IsType<PostgresDurableInvocationWorkStore>(
            factory.Services.GetRequiredService<IDurableInvocationWorkStore>());
        Assert.IsType<PostgresPublishAgentResponseCoordinator>(
            factory.Services.GetRequiredService<IAgentResponsePublicationPersistPort>());
        Assert.IsType<FailClosedModelExecutionPort>(
            factory.Services.GetRequiredService<IModelExecutionPort>());
        Assert.True(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().DurableWorkClaimingEnabled);
        Assert.Equal("fail_closed", factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().ModelExecutionAdapter);
        Assert.False(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().ModelExecutionQualified);
    }

    [Fact]
    public void Worker_keeps_fail_closed_execution_when_direct_openai_is_requested_without_a_qualified_profile()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
            builder.UseSetting("Sessions:ModelExecution:Adapter", "direct_openai");
            builder.UseSetting("Sessions:ModelExecution:Qualified", "true");
        });

        Assert.IsType<FailClosedModelExecutionPort>(
            factory.Services.GetRequiredService<IModelExecutionPort>());
        var capabilities = factory.Services.GetRequiredService<WorkerRuntimeCapabilities>();
        Assert.Equal("direct_openai", capabilities.ModelExecutionAdapter);
        Assert.False(capabilities.ModelExecutionQualified);
    }

    [Fact]
    public void Worker_keeps_fail_closed_when_direct_openai_files_are_present()
    {
        using var artifacts = OpenAiCompatibleWorkerArtifacts.CreateExample();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
            builder.UseSetting("Sessions:ModelExecution:Adapter", "direct_openai");
            builder.UseSetting("Sessions:ModelExecution:Qualified", "true");
            builder.UseSetting("Sessions:ModelExecution:InstalledProfilesPath", artifacts.ProfilesPath);
            builder.UseSetting("Sessions:ModelExecution:CredentialCatalogPath", artifacts.CatalogPath);
            builder.UseSetting("Sessions:ModelExecution:SecretDirectory", artifacts.SecretDirectory);
            builder.UseSetting("Sessions:ModelExecution:OpenAiCompatibleConfigurationsPath", artifacts.ConfigurationsPath);
            builder.UseSetting("Sessions:ModelExecution:QualificationRecordPath", artifacts.QualificationRecordPath);
        });

        Assert.IsType<FailClosedModelExecutionPort>(
            factory.Services.GetRequiredService<IModelExecutionPort>());
        var capabilities = factory.Services.GetRequiredService<WorkerRuntimeCapabilities>();
        Assert.Equal("direct_openai", capabilities.ModelExecutionAdapter);
        Assert.False(capabilities.ModelExecutionQualified);
    }

    [Fact]
    public void Worker_keeps_fail_closed_for_committed_openai_compatible_example_artifacts()
    {
        using var artifacts = OpenAiCompatibleWorkerArtifacts.CreateExample();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
            builder.UseSetting("Sessions:ModelExecution:Adapter", "openai_compatible");
            builder.UseSetting("Sessions:ModelExecution:Qualified", "true");
            builder.UseSetting("Sessions:ModelExecution:InstalledProfilesPath", artifacts.ProfilesPath);
            builder.UseSetting("Sessions:ModelExecution:CredentialCatalogPath", artifacts.CatalogPath);
            builder.UseSetting("Sessions:ModelExecution:SecretDirectory", artifacts.SecretDirectory);
            builder.UseSetting("Sessions:ModelExecution:OpenAiCompatibleConfigurationsPath", artifacts.ConfigurationsPath);
            builder.UseSetting("Sessions:ModelExecution:QualificationRecordPath", artifacts.QualificationRecordPath);
        });

        Assert.IsType<FailClosedModelExecutionPort>(
            factory.Services.GetRequiredService<IModelExecutionPort>());
        var capabilities = factory.Services.GetRequiredService<WorkerRuntimeCapabilities>();
        Assert.Equal("openai_compatible", capabilities.ModelExecutionAdapter);
        Assert.False(capabilities.ModelExecutionQualified);
    }

    [Fact]
    public void Worker_composes_openai_compatible_only_when_exact_profile_qualification_record_matches()
    {
        using var artifacts = OpenAiCompatibleWorkerArtifacts.CreateEnableable();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
            builder.UseSetting("Sessions:ModelExecution:Adapter", "openai_compatible");
            builder.UseSetting("Sessions:ModelExecution:Qualified", "true");
            builder.UseSetting("Sessions:ModelExecution:InstalledProfilesPath", artifacts.ProfilesPath);
            builder.UseSetting("Sessions:ModelExecution:CredentialCatalogPath", artifacts.CatalogPath);
            builder.UseSetting("Sessions:ModelExecution:SecretDirectory", artifacts.SecretDirectory);
            builder.UseSetting("Sessions:ModelExecution:OpenAiCompatibleConfigurationsPath", artifacts.ConfigurationsPath);
            builder.UseSetting("Sessions:ModelExecution:QualificationRecordPath", artifacts.QualificationRecordPath);
        });

        Assert.IsType<OpenAiCompatibleModelExecutionAdapter>(
            factory.Services.GetRequiredService<IModelExecutionPort>());
        var capabilities = factory.Services.GetRequiredService<WorkerRuntimeCapabilities>();
        Assert.Equal("openai_compatible", capabilities.ModelExecutionAdapter);
        Assert.True(capabilities.ModelExecutionQualified);
    }

    [Fact]
    public void Worker_keeps_openai_compatible_fail_closed_in_production_even_when_enableable_files_exist()
    {
        using var artifacts = OpenAiCompatibleWorkerArtifacts.CreateEnableable();
        var oauthSecrets = Directory.CreateTempSubdirectory("flexagent-oai-oauth-");
        File.WriteAllText(Path.Combine(oauthSecrets.FullName, "client-secret"), "unused-secret");
        try
        {
            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting(
                    "ConnectionStrings:Sessions",
                    "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
                builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
                builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
                builder.UseSetting("Sessions:ModelExecution:Adapter", "openai_compatible");
                builder.UseSetting("Sessions:ModelExecution:Qualified", "true");
                builder.UseSetting("Sessions:ModelExecution:InstalledProfilesPath", artifacts.ProfilesPath);
                builder.UseSetting("Sessions:ModelExecution:CredentialCatalogPath", artifacts.CatalogPath);
                builder.UseSetting("Sessions:ModelExecution:SecretDirectory", artifacts.SecretDirectory);
                builder.UseSetting("Sessions:ModelExecution:OpenAiCompatibleConfigurationsPath", artifacts.ConfigurationsPath);
                builder.UseSetting("Sessions:ModelExecution:QualificationRecordPath", artifacts.QualificationRecordPath);
                ApplyOauthWorkloadIdentity(builder, oauthSecrets.FullName);
            });

            Assert.IsType<FailClosedModelExecutionPort>(
                factory.Services.GetRequiredService<IModelExecutionPort>());
            var capabilities = factory.Services.GetRequiredService<WorkerRuntimeCapabilities>();
            Assert.Equal("openai_compatible", capabilities.ModelExecutionAdapter);
            Assert.False(capabilities.ModelExecutionQualified);
        }
        finally
        {
            oauthSecrets.Delete(recursive: true);
        }
    }

    [Fact]
    public void Worker_keeps_fail_closed_execution_when_openrouter_has_only_the_retired_privacy_preflight()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
            builder.UseSetting("Sessions:ModelExecution:Adapter", "openrouter");
            builder.UseSetting("Sessions:ModelExecution:Qualified", "true");
            builder.UseSetting("Sessions:ModelExecution:QualificationScope", "synthetic_development");
            builder.UseSetting("Sessions:ModelExecution:PrivacyPreflightConfirmed", "true");
        });

        Assert.IsType<FailClosedModelExecutionPort>(
            factory.Services.GetRequiredService<IModelExecutionPort>());
        var capabilities = factory.Services.GetRequiredService<WorkerRuntimeCapabilities>();
        Assert.Equal("openrouter", capabilities.ModelExecutionAdapter);
        Assert.False(capabilities.ModelExecutionQualified);
        Assert.Equal("synthetic_development", capabilities.ModelExecutionQualificationScope);
        Assert.DoesNotContain("sk-", capabilities.ModelExecutionAdapter, StringComparison.Ordinal);
    }

    [Fact]
    public void Worker_keeps_openrouter_fail_closed_in_production_even_when_files_exist()
    {
        using var artifacts = OpenRouterWorkerArtifacts.Create();
        var oauthSecrets = Directory.CreateTempSubdirectory("flexagent-openrouter-oauth-");
        File.WriteAllText(Path.Combine(oauthSecrets.FullName, "client-secret"), "unused-secret");
        try
        {
            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting(
                    "ConnectionStrings:Sessions",
                    "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
                builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
                builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
                builder.UseSetting("Sessions:ModelExecution:Adapter", "openrouter");
                builder.UseSetting("Sessions:ModelExecution:Qualified", "true");
                builder.UseSetting("Sessions:ModelExecution:QualificationScope", "synthetic_development");
                builder.UseSetting("Sessions:ModelExecution:SyntheticDataPolicyAccepted", "true");
                builder.UseSetting("Sessions:ModelExecution:InstalledProfilesPath", artifacts.ProfilesPath);
                builder.UseSetting("Sessions:ModelExecution:CredentialCatalogPath", artifacts.CatalogPath);
                builder.UseSetting("Sessions:ModelExecution:OpenRouterConfigurationsPath", artifacts.ConfigurationsPath);
                builder.UseSetting("Sessions:ModelExecution:SecretDirectory", artifacts.SecretDirectory);
                ApplyOauthWorkloadIdentity(builder, oauthSecrets.FullName);
            });

            Assert.IsType<FailClosedModelExecutionPort>(
                factory.Services.GetRequiredService<IModelExecutionPort>());
            Assert.False(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().ModelExecutionQualified);
        }
        finally
        {
            oauthSecrets.Delete(recursive: true);
        }
    }

    [Fact]
    public void Worker_composes_openrouter_only_when_synthetic_gates_pass()
    {
        using var artifacts = OpenRouterWorkerArtifacts.Create();
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting(
                "ConnectionStrings:Sessions",
                "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
            builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
            builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
            builder.UseSetting("Sessions:ModelExecution:Adapter", "openrouter");
            builder.UseSetting("Sessions:ModelExecution:Qualified", "true");
            builder.UseSetting("Sessions:ModelExecution:QualificationScope", "synthetic_development");
            builder.UseSetting("Sessions:ModelExecution:SyntheticDataPolicyAccepted", "true");
            builder.UseSetting("Sessions:ModelExecution:InstalledProfilesPath", artifacts.ProfilesPath);
            builder.UseSetting("Sessions:ModelExecution:CredentialCatalogPath", artifacts.CatalogPath);
            builder.UseSetting("Sessions:ModelExecution:OpenRouterConfigurationsPath", artifacts.ConfigurationsPath);
            builder.UseSetting("Sessions:ModelExecution:SecretDirectory", artifacts.SecretDirectory);
        });

        Assert.IsType<OpenRouterModelExecutionAdapter>(
            factory.Services.GetRequiredService<IModelExecutionPort>());
        var capabilities = factory.Services.GetRequiredService<WorkerRuntimeCapabilities>();
        Assert.Equal("openrouter", capabilities.ModelExecutionAdapter);
        Assert.True(capabilities.ModelExecutionQualified);
        Assert.Equal("synthetic_development", capabilities.ModelExecutionQualificationScope);
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
    public void Worker_refuses_protected_invocation_processing_without_workload_identity(
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
        Assert.Contains("workload identity", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("flexagent", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Worker_refuses_timer_polling_without_workload_identity(
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
        Assert.Contains("workload identity", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("flexagent", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Worker_refuses_synthetic_workload_identity_outside_development_and_testing(
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
            builder.UseSetting("WorkloadIdentity:Profile", "synthetic.configured_actor");
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            factory.Services.GetRequiredService<IDurableTimerFireProcessor>());
        Assert.Contains("synthetic.configured_actor", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("flexagent", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Production", "WorkloadIdentity:TokenEndpoint", "http://issuer.example/token")]
    [InlineData("Staging", "WorkloadIdentity:JwksUri", "http://issuer.example/certs")]
    public void Worker_refuses_plaintext_oauth_issuer_uris_outside_development_and_testing(
        string environmentName,
        string settingName,
        string plaintextUri)
    {
        var secrets = Directory.CreateTempSubdirectory("flexagent-worker-secrets");
        File.WriteAllText(Path.Combine(secrets.FullName, "client-secret"), "unused-secret");
        try
        {
            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environmentName);
                builder.UseSetting(
                    "ConnectionStrings:Sessions",
                    "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
                builder.UseSetting("Sessions:TimerPolling:Enabled", "true");
                builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
                ApplyOauthWorkloadIdentity(builder, secrets.FullName);
                builder.UseSetting(settingName, plaintextUri);
            });

            var exception = Assert.Throws<InvalidOperationException>(() =>
                factory.Services.GetRequiredService<IDurableTimerFireProcessor>());
            Assert.Contains("https", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(settingName, exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("unused-secret", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            secrets.Delete(true);
        }
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Worker_composes_timer_polling_when_oauth_workload_identity_is_configured(
        string environmentName)
    {
        var secrets = Directory.CreateTempSubdirectory("flexagent-worker-secrets");
        File.WriteAllText(Path.Combine(secrets.FullName, "client-secret"), "unused-secret");
        try
        {
            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environmentName);
                builder.UseSetting(
                    "ConnectionStrings:Sessions",
                    "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
                builder.UseSetting("Sessions:TimerPolling:Enabled", "true");
                builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
                ApplyOauthWorkloadIdentity(builder, secrets.FullName);
            });

            Assert.IsType<DurableTimerFireProcessor>(
                factory.Services.GetRequiredService<IDurableTimerFireProcessor>());
            Assert.True(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().TimerPollingEnabled);
            Assert.False(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().DurableWorkClaimingEnabled);
        }
        finally
        {
            secrets.Delete(true);
        }
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Worker_composes_invocation_processing_independently_of_timer_polling(
        string environmentName)
    {
        var secrets = Directory.CreateTempSubdirectory("flexagent-worker-secrets");
        File.WriteAllText(Path.Combine(secrets.FullName, "client-secret"), "unused-secret");
        try
        {
            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environmentName);
                builder.UseSetting(
                    "ConnectionStrings:Sessions",
                    "Host=localhost;Database=flexagent;Username=flexagent;Password=unused");
                builder.UseSetting("Sessions:InvocationProcessing:Enabled", "true");
                builder.UseSetting("Sessions:WorkerServiceActorId", TestWorkerServiceActorId.ToString("D"));
                ApplyOauthWorkloadIdentity(builder, secrets.FullName);
            });

            Assert.IsType<DurableInvocationWorkProcessor>(
                factory.Services.GetRequiredService<IDurableInvocationWorkProcessor>());
            Assert.True(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().DurableWorkClaimingEnabled);
            Assert.False(factory.Services.GetRequiredService<WorkerRuntimeCapabilities>().TimerPollingEnabled);
        }
        finally
        {
            secrets.Delete(true);
        }
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

    private sealed class OpenAiCompatibleWorkerArtifacts : IDisposable
    {
        private OpenAiCompatibleWorkerArtifacts(
            string root,
            string profilesPath,
            string catalogPath,
            string configurationsPath,
            string qualificationRecordPath,
            string secretDirectory)
        {
            Root = root;
            ProfilesPath = profilesPath;
            CatalogPath = catalogPath;
            ConfigurationsPath = configurationsPath;
            QualificationRecordPath = qualificationRecordPath;
            SecretDirectory = secretDirectory;
        }

        public string Root { get; }
        public string ProfilesPath { get; }
        public string CatalogPath { get; }
        public string ConfigurationsPath { get; }
        public string QualificationRecordPath { get; }
        public string SecretDirectory { get; }

        public static OpenAiCompatibleWorkerArtifacts CreateExample() =>
            Create(
                OpenAiCompatibleInstalledConfiguration.Create(
                    "openai-compatible.example.do-not-enable",
                    "1",
                    new Uri("https://models.organization.example/"),
                    "replace-with-operator-selected-model",
                    "replace-with-immutable-version-or-fingerprint",
                    ModelDeploymentCredentialModes.OrganizationByok,
                    "replace-with-actual-provider-or-runtime-id",
                    "/v1"),
                OpenAiCompatibleQualificationRecords.DoNotEnable);

        public static OpenAiCompatibleWorkerArtifacts CreateEnableable() =>
            Create(
                OpenAiCompatibleInstalledConfiguration.Create(
                    "openai-compatible.worker-gate.test",
                    "1",
                    new Uri("https://models.organization.example/"),
                    "synthetic.model.pinned",
                    "synthetic.model.pinned.2026-01-01",
                    ModelDeploymentCredentialModes.OrganizationByok,
                    "openai.compatible.test",
                    "/v1"),
                OpenAiCompatibleQualificationRecords.ExactProfile);

        private static OpenAiCompatibleWorkerArtifacts Create(
            OpenAiCompatibleInstalledConfiguration configuration,
            string qualifiedFor)
        {
            var root = Directory.CreateTempSubdirectory("flex-agent-oai-worker-").FullName;
            var profile = configuration.Profile;
            var profilesPath = Path.Combine(root, "profiles.json");
            var catalogPath = Path.Combine(root, "catalog.json");
            var configurationsPath = Path.Combine(root, "openai-compatible.json");
            var qualificationRecordPath = Path.Combine(root, "qualification.json");
            var secretDirectory = Path.Combine(root, "secrets");
            Directory.CreateDirectory(secretDirectory);
            File.WriteAllText(profilesPath, $$"""
                [
                  {
                    "profileId": "{{profile.ProfileId}}",
                    "profileVersion": "{{profile.ProfileVersion}}",
                    "adapterKind": "{{profile.AdapterKind}}",
                    "adapterContractVersion": "{{profile.AdapterContractVersion}}",
                    "approvedHttpsOrigin": "https://models.organization.example/",
                    "requestedModel": "{{profile.RequestedModel}}",
                    "resolvedModelVersion": "{{profile.ResolvedModelVersion}}",
                    "capabilityProfileId": "{{profile.CapabilityProfileId}}",
                    "credentialMode": "{{profile.CredentialMode}}",
                    "maxOutputTokens": {{profile.MaxOutputTokens}},
                    "controlTimeoutMilliseconds": {{(int)profile.ControlTimeout.TotalMilliseconds}},
                    "contentTimeoutMilliseconds": {{(int)profile.ContentTimeout.TotalMilliseconds}},
                    "maxProviderRequestAttempts": {{profile.MaxProviderRequestAttempts}},
                    "providerId": "{{profile.ProviderId}}",
                    "adapterConfigurationDigest": "{{profile.AdapterConfigurationDigest}}"
                  }
                ]
                """);
            File.WriteAllText(catalogPath, """
                [
                  {
                    "bindingReference": "bind.opaque.0001",
                    "bindingVersion": "bind.v1",
                    "ownerOrganizationId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    "providerId": "openai.compatible.test",
                    "credentialMode": "organization_byok",
                    "revoked": false,
                    "secretName": "org-a-openai"
                  }
                ]
                """);
            File.WriteAllText(configurationsPath, $$"""
                [
                  {
                    "profileId": "{{profile.ProfileId}}",
                    "profileVersion": "{{profile.ProfileVersion}}",
                    "profileDigest": "{{profile.ProfileDigest}}",
                    "adapterConfigurationDigest": "{{profile.AdapterConfigurationDigest}}",
                    "apiBasePath": "/v1",
                    "destinationPolicy": "public_only"
                  }
                ]
                """);
            File.WriteAllText(qualificationRecordPath, $$"""
                {
                  "adapterKind": "openai_compatible",
                  "adapterContractVersion": "sessions.openai_compatible.v1",
                  "profileId": "{{profile.ProfileId}}",
                  "profileVersion": "{{profile.ProfileVersion}}",
                  "profileDigest": "{{profile.ProfileDigest}}",
                  "adapterConfigurationDigest": "{{profile.AdapterConfigurationDigest}}",
                  "qualifiedFor": "{{qualifiedFor}}"
                }
                """);
            File.WriteAllText(Path.Combine(secretDirectory, "org-a-openai"), "sk-test-not-for-production");
            return new OpenAiCompatibleWorkerArtifacts(
                root,
                profilesPath,
                catalogPath,
                configurationsPath,
                qualificationRecordPath,
                secretDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class OpenRouterWorkerArtifacts : IDisposable
    {
        private OpenRouterWorkerArtifacts(
            string root,
            string profilesPath,
            string catalogPath,
            string configurationsPath,
            string secretDirectory)
        {
            Root = root;
            ProfilesPath = profilesPath;
            CatalogPath = catalogPath;
            ConfigurationsPath = configurationsPath;
            SecretDirectory = secretDirectory;
        }

        public string Root { get; }
        public string ProfilesPath { get; }
        public string CatalogPath { get; }
        public string ConfigurationsPath { get; }
        public string SecretDirectory { get; }

        public static OpenRouterWorkerArtifacts Create()
        {
            var root = Directory.CreateTempSubdirectory("flex-agent-or-worker-").FullName;
            var configuration = OpenRouterInstalledConfiguration.Create(
                "openrouter.synthetic.example",
                "1",
                "meta-llama/llama-3.1-8b-instruct:free",
                "meta-llama/llama-3.1-8b-instruct:free",
                "Together",
                "Together",
                ModelDeploymentCredentialModes.OrganizationByok,
                "openrouter.synthetic");
            var profile = configuration.Profile;
            var profilesPath = Path.Combine(root, "profiles.json");
            var catalogPath = Path.Combine(root, "catalog.json");
            var configurationsPath = Path.Combine(root, "openrouter.json");
            var secretDirectory = Path.Combine(root, "secrets");
            Directory.CreateDirectory(secretDirectory);
            File.WriteAllText(profilesPath, $$"""
                [
                  {
                    "profileId": "{{profile.ProfileId}}",
                    "profileVersion": "{{profile.ProfileVersion}}",
                    "adapterKind": "{{profile.AdapterKind}}",
                    "adapterContractVersion": "{{profile.AdapterContractVersion}}",
                    "approvedHttpsOrigin": "https://openrouter.ai/",
                    "requestedModel": "{{profile.RequestedModel}}",
                    "resolvedModelVersion": "{{profile.ResolvedModelVersion}}",
                    "capabilityProfileId": "{{profile.CapabilityProfileId}}",
                    "credentialMode": "{{profile.CredentialMode}}",
                    "maxOutputTokens": {{profile.MaxOutputTokens}},
                    "controlTimeoutMilliseconds": {{(int)profile.ControlTimeout.TotalMilliseconds}},
                    "contentTimeoutMilliseconds": {{(int)profile.ContentTimeout.TotalMilliseconds}},
                    "maxProviderRequestAttempts": {{profile.MaxProviderRequestAttempts}},
                    "providerId": "{{profile.ProviderId}}",
                    "adapterConfigurationDigest": "{{profile.AdapterConfigurationDigest}}"
                  }
                ]
                """);
            File.WriteAllText(catalogPath, """
                [
                  {
                    "bindingReference": "bind.opaque.0001",
                    "bindingVersion": "bind.v1",
                    "ownerOrganizationId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    "providerId": "openrouter.synthetic",
                    "credentialMode": "organization_byok",
                    "revoked": false,
                    "secretName": "openrouter-api-key"
                  }
                ]
                """);
            File.WriteAllText(configurationsPath, $$"""
                [
                  {
                    "profileId": "{{profile.ProfileId}}",
                    "profileVersion": "{{profile.ProfileVersion}}",
                    "profileDigest": "{{profile.ProfileDigest}}",
                    "adapterConfigurationDigest": "{{profile.AdapterConfigurationDigest}}",
                    "providerSlug": "Together",
                    "expectedReturnedProviderIdentity": "Together"
                  }
                ]
                """);
            var keyPath = Path.Combine(secretDirectory, "openrouter-api-key");
            File.WriteAllText(keyPath, "sk-or-canary-not-for-live");
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                File.SetUnixFileMode(secretDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            return new OpenRouterWorkerArtifacts(root, profilesPath, catalogPath, configurationsPath, secretDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private static void ApplyOauthWorkloadIdentity(IWebHostBuilder builder, string secretDirectory)
    {
        builder.UseSetting("WorkloadIdentity:Profile", "oauth_client_credentials_jwt");
        builder.UseSetting("WorkloadIdentity:Issuer", "https://issuer.example/realms/flex-agent");
        builder.UseSetting("WorkloadIdentity:Audience", "flex-agent-worker");
        builder.UseSetting("WorkloadIdentity:Subject", "worker-client");
        builder.UseSetting("WorkloadIdentity:ClientId", "worker-client");
        builder.UseSetting("WorkloadIdentity:TokenEndpoint", "https://issuer.example/realms/flex-agent/protocol/openid-connect/token");
        builder.UseSetting("WorkloadIdentity:JwksUri", "https://issuer.example/realms/flex-agent/protocol/openid-connect/certs");
        builder.UseSetting("WorkloadIdentity:SecretDirectory", secretDirectory);
        builder.UseSetting("WorkloadIdentity:ClientSecretName", "client-secret");
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
