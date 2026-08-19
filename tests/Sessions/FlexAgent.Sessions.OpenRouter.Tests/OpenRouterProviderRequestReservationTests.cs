using System.Net;
using System.Text;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.OpenRouter;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterProviderRequestReservationTests
{
    [Fact]
    public async Task Crash_after_http_before_finished_fact_does_not_send_another_provider_request()
    {
        var configuration = OpenRouterInstalledConfiguration.Create(
            "openrouter.synthetic.example",
            "1",
            "meta-llama/llama-3.1-8b-instruct:free",
            "meta-llama/llama-3.1-8b-instruct:free",
            "Together",
            "Together",
            ModelDeploymentCredentialModes.OrganizationByok,
            "openrouter.synthetic",
            1);
        var profile = configuration.Profile;
        var frozen = new FrozenModelDeploymentBinding(
            profile.ProfileId,
            profile.ProfileVersion,
            profile.ProfileDigest,
            profile.ProviderId,
            ModelDeploymentCredentialModes.OrganizationByok,
            "bind.opaque.0001",
            "bind.v1");
        var session = SessionRuntime.CreateActive(
            SessionRuntimeTestFixtures.CreateBinding(frozenModelDeployment: frozen),
            SessionRuntimeTestFixtures.T0);
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.opening.openrouter.reserve",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var envelope =
            "{\"schema_version\":\"v2\",\"agent_decision_id\":\"adec.reserve01\",\"agent_invocation_id\":\""
            + invocationId
            + "\",\"produced_at\":\"2026-08-14T00:00:00Z\",\"disposition\":\"no_action\",\"outputs\":[],\"requested_actions\":[],\"no_action\":{\"reason_category\":\"intentional_silence\"}}";
        var handler = new CountingHandler(envelope, profile.ResolvedModelVersion);
        var adapter = new OpenRouterModelExecutionAdapter(
            new InMemoryInstalledModelDeploymentProfileRegistry(profile),
            new InMemoryModelDeploymentCredentialCatalog(
                SessionRuntimeTestFixtures.CreateCatalogRecord(
                    session.Ownership.OrganizationId,
                    providerId: "openrouter.synthetic")),
            new StaticSecretSource("sk-or-canary-secret-do-not-leak"),
            new InMemoryOpenRouterInstalledConfigurationRegistry(configuration),
            handler,
            privacyPreflightConfirmed: true);
        var writer = new InMemoryModelProviderAttemptProvenanceWriter { ThrowOnFinished = true };
        var store = new RestartStore(session.Ownership, invocationId);
        var processor = new DurableInvocationWorkProcessor(
            store,
            new MemoryGateway(session),
            adapter,
            new CompleteInvocationHandler(),
            new DurableInvocationWorkSettings(
                SessionRuntimeTestFixtures.CreateActor(),
                "worker.session_runtime",
                65_536,
                InstalledProfiles: new InMemoryInstalledModelDeploymentProfileRegistry(profile),
                CredentialCatalog: new InMemoryModelDeploymentCredentialCatalog(
                    SessionRuntimeTestFixtures.CreateCatalogRecord(
                        session.Ownership.OrganizationId,
                        providerId: "openrouter.synthetic"))),
            PassThroughAgentResponsePublicationPersistPort.Succeed,
            writer);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.TryProcessNextAsync(CancellationToken.None));
        Assert.Equal(1, handler.RequestCount);
        Assert.Contains(writer.Facts, fact => fact.Provenance.FactKind == ModelProviderRequestFacts.Started);
        Assert.DoesNotContain(writer.Facts, fact => fact.Provenance.FactKind == ModelProviderRequestFacts.Finished);

        writer.ThrowOnFinished = false;
        var retried = await processor.TryProcessNextAsync(CancellationToken.None);
        Assert.Equal(1, handler.RequestCount);
        Assert.NotEqual(DurableInvocationWorkOutcomes.Decided, retried.Outcome);
    }

    private sealed class CountingHandler(string content, string model) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var encoded = System.Text.Json.JsonSerializer.Serialize(content);
            var json = "{\"id\":\"gen-test\",\"model\":" + System.Text.Json.JsonSerializer.Serialize(model)
                + ",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":" + encoded
                + "},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":9,\"completion_tokens\":4,\"prompt_tokens_details\":{\"cached_tokens\":0}},\"openrouter_metadata\":{\"attempt\":1,\"endpoints\":{\"available\":[{\"provider\":\"Together\",\"model\":"
                + System.Text.Json.JsonSerializer.Serialize(model)
                + ",\"selected\":true}]}}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class StaticSecretSource(string value) : IProviderCredentialSecretSource
    {
        public Task<ProviderSecret?> TryReadAsync(string secretName, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProviderSecret?>(new ProviderSecret(value));
    }

    private sealed class MemoryGateway(SessionRuntime session) : IInvocationWorkSessionGateway
    {
        public Task<LoadedInvocationWorkSession?> LoadAsync(
            SessionOwnership ownership,
            CancellationToken cancellationToken) =>
            Task.FromResult<LoadedInvocationWorkSession?>(
                ownership == session.Ownership
                    ? new LoadedInvocationWorkSession(session, session.Binding, session.SessionVersion)
                    : null);

        public Task<DateTimeOffset> ReadAuthoritativeUtcAsync(CancellationToken cancellationToken) =>
            Task.FromResult(SessionRuntimeTestFixtures.T0.AddSeconds(2));

        public Task<bool> TrySaveCompletionAsync(
            SessionOwnership ownership,
            long expectedSessionVersion,
            SessionRuntime sessionRuntime,
            AgentInvocation invocation,
            Guid correlationId,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> TryAuthorizeModelDisclosureAsync(
            SessionOwnership ownership,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class RestartStore : IDurableInvocationWorkStore
    {
        private DurableInvocationWorkItem _item;

        public RestartStore(SessionOwnership ownership, string agentInvocationId)
        {
            _item = new DurableInvocationWorkItem(
                Guid.NewGuid(),
                ownership,
                agentInvocationId,
                DurableSessionWorkStates.Pending);
        }

        public Task<DurableInvocationWorkItem?> TryClaimExecuteInvocationAsync(
            TimeSpan lease,
            CancellationToken cancellationToken)
        {
            if (_item.State == DurableSessionWorkStates.Completed)
            {
                return Task.FromResult<DurableInvocationWorkItem?>(null);
            }

            _item.State = DurableSessionWorkStates.Claimed;
            _item.ClaimLeaseUntil = DateTimeOffset.UtcNow.Add(lease);
            return Task.FromResult<DurableInvocationWorkItem?>(_item);
        }

        public Task ReleaseToPendingAsync(DurableInvocationWorkItem work, CancellationToken cancellationToken)
        {
            _item.State = DurableSessionWorkStates.Pending;
            return Task.CompletedTask;
        }

        public Task MarkCompletedAsync(DurableInvocationWorkItem work, CancellationToken cancellationToken)
        {
            _item.State = DurableSessionWorkStates.Completed;
            return Task.CompletedTask;
        }
    }
}
