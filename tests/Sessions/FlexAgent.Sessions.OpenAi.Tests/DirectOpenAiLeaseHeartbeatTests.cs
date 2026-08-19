using System.Net;
using System.Text;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.OpenAi.Tests;

public sealed class DirectOpenAiLeaseHeartbeatTests
{
    [Fact]
    public async Task Lease_renewal_exception_retries_through_the_direct_openai_adapter()
    {
        var profile = InstalledModelDeploymentProfile.Create(
            "direct-openai.unqualified.example",
            "1",
            ModelDeploymentAdapterKinds.DirectOpenAi,
            DirectOpenAiModelExecutionAdapter.AdapterContractVersion,
            new Uri("https://api.openai.com/"),
            "synthetic.model.pinned",
            "synthetic.model.pinned.2026-01-01",
            "p0.text.structured-control",
            ModelDeploymentCredentialModes.OrganizationByok,
            256,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5),
            2,
            "openai.direct");
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
        var adapter = new DirectOpenAiModelExecutionAdapter(
            new InMemoryInstalledModelDeploymentProfileRegistry(profile),
            new InMemoryModelDeploymentCredentialCatalog(
                SessionRuntimeTestFixtures.CreateCatalogRecord(
                    session.Ownership.OrganizationId,
                    providerId: "openai.direct")),
            new StaticSecretSource("sk-test-not-for-production"),
            new DelayedOpenAiHandler());
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
                        providerId: "openai.direct")),
                ClaimLease: TimeSpan.FromSeconds(30),
                ClaimLeaseRenewalPeriod: TimeSpan.FromMilliseconds(20)),
            PassThroughAgentResponsePublicationPersistPort.Succeed);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, result.Outcome);
        Assert.False(session.Invocations[0].IsTerminal);
    }

    private sealed class StaticSecretSource(string value) : IProviderCredentialSecretSource
    {
        public Task<ProviderSecret?> TryReadAsync(string secretName, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProviderSecret?>(new ProviderSecret(value));
    }

    private sealed class DelayedOpenAiHandler : HttpMessageHandler
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
