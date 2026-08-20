using System.Net;
using System.Text;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.OpenAiCompatible;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.OpenAiCompatible.Tests;

public sealed class OpenAiCompatibleLeaseHeartbeatTests
{
    [Fact]
    public async Task Lease_renewal_exception_retries_through_the_openai_compatible_adapter()
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
            2);
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
            "idem.opening.openai.lease",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new OpenAiCompatibleModelExecutionAdapter(
            new InMemoryInstalledModelDeploymentProfileRegistry(profile),
            new InMemoryModelDeploymentCredentialCatalog(
                SessionRuntimeTestFixtures.CreateCatalogRecord(
                    session.Ownership.OrganizationId,
                    providerId: "openai.compatible.test")),
            new StaticSecretSource("sk-test-not-for-production"),
            new InMemoryOpenAiCompatibleInstalledConfigurationRegistry(configuration),
            new DelayedCompatibleHandler(),
            new PublicTestResolver());
        var store = new RenewThrowStore(session.Ownership, invocationId);
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
                        providerId: "openai.compatible.test")),
                ClaimLease: TimeSpan.FromSeconds(30),
                ClaimLeaseRenewalPeriod: TimeSpan.FromMilliseconds(20)),
            PassThroughAgentResponsePublicationPersistPort.Succeed,
            new InMemoryModelProviderAttemptProvenanceWriter());

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, result.Outcome);
        Assert.False(session.Invocations[0].IsTerminal);
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

    private sealed class DelayedCompatibleHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
        }
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

    private sealed class RenewThrowStore : IDurableInvocationWorkStore
    {
        private DurableInvocationWorkItem _item;

        public RenewThrowStore(SessionOwnership ownership, string agentInvocationId)
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

        public Task<DateTimeOffset?> TryRenewClaimLeaseAsync(
            DurableInvocationWorkItem work,
            TimeSpan lease,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Injected lease renewal failure.");
    }
}
