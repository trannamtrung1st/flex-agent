using System.Net;
using System.Text;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.OpenAiCompatible;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.OpenAiCompatible.Tests;

public sealed class OpenAiCompatibleProviderRequestReservationTests
{
    [Fact]
    public async Task Crash_after_http_before_finished_fact_does_not_send_another_provider_request()
    {
        var configuration = OpenAiCompatibleInstalledConfiguration.Create(
            "openai-compatible.unqualified.test",
            "1",
            new Uri("https://models.organization.example/"),
            "synthetic.model.pinned",
            "synthetic.model.pinned.2026-01-01",
            ModelDeploymentCredentialModes.OrganizationByok,
            "openai.compatible.test",
            "/v1",
            OpenAiCompatibleDestinationPolicy.PublicOnly,
            256,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5),
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
            "idem.opening.openai.reserve",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var envelope =
            "{\"schema_version\":\"v2\",\"agent_decision_id\":\"adec.reserve01\",\"agent_invocation_id\":\""
            + invocationId
            + "\",\"produced_at\":\"2026-08-14T00:00:00Z\",\"disposition\":\"no_action\",\"outputs\":[],\"requested_actions\":[],\"no_action\":{\"reason_category\":\"intentional_silence\"}}";
        var handler = new CountingCompatibleHandler(envelope);
        var adapter = new OpenAiCompatibleModelExecutionAdapter(
            new InMemoryInstalledModelDeploymentProfileRegistry(profile),
            new InMemoryModelDeploymentCredentialCatalog(
                SessionRuntimeTestFixtures.CreateCatalogRecord(
                    session.Ownership.OrganizationId,
                    providerId: "openai.compatible.test")),
            new StaticSecretSource("sk-test-not-for-production"),
            new InMemoryOpenAiCompatibleInstalledConfigurationRegistry(configuration),
            handler,
            new PublicTestResolver());
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
                        providerId: "openai.compatible.test"))),
            PassThroughAgentResponsePublicationPersistPort.Succeed,
            writer);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.TryProcessNextAsync(CancellationToken.None));
        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(1, await writer.CountAsync(session.Ownership, invocationId, CancellationToken.None));
        Assert.Contains(writer.Facts, fact => fact.Provenance.FactKind == ModelProviderRequestFacts.Started);
        Assert.DoesNotContain(writer.Facts, fact => fact.Provenance.FactKind == ModelProviderRequestFacts.Finished);

        writer.ThrowOnFinished = false;
        var retried = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(1, handler.RequestCount);
        Assert.NotEqual(DurableInvocationWorkOutcomes.Decided, retried.Outcome);
        Assert.Null(session.Invocations[0].Decision);
    }

    private sealed class CountingCompatibleHandler(string content) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            var encoded = System.Text.Json.JsonSerializer.Serialize(content);
            var json =
                "{\"id\":\"chatcmpl-test\",\"object\":\"chat.completion\",\"created\":0,\"model\":\"synthetic.model.pinned.2026-01-01\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":"
                + encoded
                + "},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":9,\"completion_tokens\":4,\"total_tokens\":13}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class PublicTestResolver : IEndpointAddressResolver
    {
        public Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IPAddress>>([IPAddress.Parse("203.0.113.10")]);
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
