using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class FrozenModelDeploymentResolverTests
{
    [Fact]
    public void Two_organizations_keep_distinct_frozen_bindings_when_host_settings_are_shared()
    {
        var orgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var orgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var profileA = SessionRuntimeTestFixtures.CreateInstalledProfile("provider.org-a");
        var profileB = InstalledModelDeploymentProfile.Create(
            "synthetic.fake.org-b",
            "1",
            ModelDeploymentAdapterKinds.DeterministicFake,
            "sessions.fake.v1",
            new Uri("https://api.openai.com/"),
            "synthetic.model.pinned",
            "synthetic.model.pinned.2026-01-01",
            "p0.text.structured-control",
            ModelDeploymentCredentialModes.OrganizationByok,
            256,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
            2,
            "provider.org-b");
        var frozenA = new FrozenModelDeploymentBinding(
            profileA.ProfileId,
            profileA.ProfileVersion,
            profileA.ProfileDigest,
            profileA.ProviderId,
            ModelDeploymentCredentialModes.OrganizationByok,
            "bind.org-a",
            "bind.v1");
        var frozenB = new FrozenModelDeploymentBinding(
            profileB.ProfileId,
            profileB.ProfileVersion,
            profileB.ProfileDigest,
            profileB.ProviderId,
            ModelDeploymentCredentialModes.OrganizationByok,
            "bind.org-b",
            "bind.v1");
        var profiles = new InMemoryInstalledModelDeploymentProfileRegistry(profileA, profileB);
        var catalog = new InMemoryModelDeploymentCredentialCatalog(
            SessionRuntimeTestFixtures.CreateCatalogRecord(orgA, "provider.org-a", bindingReference: "bind.org-a"),
            SessionRuntimeTestFixtures.CreateCatalogRecord(orgB, "provider.org-b", bindingReference: "bind.org-b"));

        var resolvedA = FrozenModelDeploymentResolver.Resolve(
            SessionRuntimeTestFixtures.CreateBinding(
                ownership: SessionRuntimeTestFixtures.CreateOwnership() with { OrganizationId = orgA },
                frozenModelDeployment: frozenA),
            profiles,
            catalog);
        var resolvedB = FrozenModelDeploymentResolver.Resolve(
            SessionRuntimeTestFixtures.CreateBinding(
                ownership: SessionRuntimeTestFixtures.CreateOwnership() with { OrganizationId = orgB },
                frozenModelDeployment: frozenB),
            profiles,
            catalog);

        Assert.True(resolvedA.Succeeded);
        Assert.True(resolvedB.Succeeded);
        Assert.Equal("provider.org-a", resolvedA.Binding!.ProviderId);
        Assert.Equal("provider.org-b", resolvedB.Binding!.ProviderId);
        Assert.Equal("bind.org-a", resolvedA.Binding.BindingReference);
        Assert.Equal("bind.org-b", resolvedB.Binding.BindingReference);
    }

    [Fact]
    public void Missing_or_mismatched_frozen_profile_does_not_fall_back()
    {
        var profiles = new InMemoryInstalledModelDeploymentProfileRegistry(
            SessionRuntimeTestFixtures.CreateInstalledProfile());
        var catalog = new InMemoryModelDeploymentCredentialCatalog(
            SessionRuntimeTestFixtures.CreateCatalogRecord(
                SessionRuntimeTestFixtures.CreateOwnership().OrganizationId));

        var missing = FrozenModelDeploymentResolver.Resolve(
            SessionRuntimeTestFixtures.CreateBinding(includeFrozenDeployment: false),
            profiles,
            catalog);
        var wrongDigest = FrozenModelDeploymentResolver.Resolve(
            SessionRuntimeTestFixtures.CreateBinding(
                frozenModelDeployment: SessionRuntimeTestFixtures.CreateFrozenDeployment() with
                {
                    ProfileDigest = new string('a', 64),
                }),
            profiles,
            catalog);

        Assert.False(missing.Succeeded);
        Assert.Equal(FrozenModelDeploymentOutcomeCodes.FrozenBindingMissing, missing.OutcomeCode);
        Assert.False(wrongDigest.Succeeded);
        Assert.Equal(FrozenModelDeploymentOutcomeCodes.ProfileMissing, wrongDigest.OutcomeCode);
    }

    [Fact]
    public void Revoked_wrong_organization_and_provider_mismatch_do_not_fall_back()
    {
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var frozen = SessionRuntimeTestFixtures.CreateFrozenDeployment();
        var profiles = new InMemoryInstalledModelDeploymentProfileRegistry(
            SessionRuntimeTestFixtures.CreateInstalledProfile());
        var binding = SessionRuntimeTestFixtures.CreateBinding(frozenModelDeployment: frozen);

        var revoked = FrozenModelDeploymentResolver.Resolve(
            binding,
            profiles,
            new InMemoryModelDeploymentCredentialCatalog(
                SessionRuntimeTestFixtures.CreateCatalogRecord(ownership.OrganizationId, revoked: true)));
        var wrongOrg = FrozenModelDeploymentResolver.Resolve(
            binding,
            profiles,
            new InMemoryModelDeploymentCredentialCatalog(
                SessionRuntimeTestFixtures.CreateCatalogRecord(Guid.Parse("99999999-9999-9999-9999-999999999999"))));
        var wrongProvider = FrozenModelDeploymentResolver.Resolve(
            binding,
            profiles,
            new InMemoryModelDeploymentCredentialCatalog(
                SessionRuntimeTestFixtures.CreateCatalogRecord(ownership.OrganizationId, providerId: "other.provider")));

        Assert.Equal(FrozenModelDeploymentOutcomeCodes.CredentialRevoked, revoked.OutcomeCode);
        Assert.Equal(FrozenModelDeploymentOutcomeCodes.CredentialWrongOrganization, wrongOrg.OutcomeCode);
        Assert.Equal(FrozenModelDeploymentOutcomeCodes.CredentialProviderMismatch, wrongProvider.OutcomeCode);
    }

    [Fact]
    public void Deployment_default_does_not_require_organization_owner()
    {
        var profile = SessionRuntimeTestFixtures.CreateInstalledProfile(
            credentialMode: ModelDeploymentCredentialModes.DeploymentDefault);
        var frozen = SessionRuntimeTestFixtures.CreateFrozenDeployment(
            credentialMode: ModelDeploymentCredentialModes.DeploymentDefault,
            bindingReference: "bind.default.0001");
        var resolution = FrozenModelDeploymentResolver.Resolve(
            SessionRuntimeTestFixtures.CreateBinding(frozenModelDeployment: frozen),
            new InMemoryInstalledModelDeploymentProfileRegistry(profile),
            new InMemoryModelDeploymentCredentialCatalog(
                new ModelDeploymentCredentialCatalogRecord(
                    "bind.default.0001",
                    "bind.v1",
                    Guid.Empty,
                    "synthetic.provider",
                    ModelDeploymentCredentialModes.DeploymentDefault,
                    false,
                    "deployment-default-openai")));

        Assert.True(resolution.Succeeded);
        Assert.Equal(ModelDeploymentCredentialBindingSource.DeploymentDefault, resolution.Binding!.Source);
    }
}

public sealed class FrozenProviderAuthorityProcessorTests
{
    [Fact]
    public async Task Worker_host_settings_cannot_replace_a_frozen_session_provider_or_binding()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.opening.frozen",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new RecordingModelExecutionPort();
        adapter.Inner.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                DecisionDispositions.NoAction,
                [],
                [],
                NoActionReasonCategories.IntentionalSilence,
                "adec.frozen.00000001"));
        var processor = CreateProcessor(adapter, session, CreateStore(session.Ownership, invocationId));

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Decided, result.Outcome);
        var request = Assert.Single(adapter.ExecutionRequests);
        Assert.Equal("synthetic.provider", request.ProviderId);
        Assert.Equal("bind.opaque.0001", request.CredentialBindingReference);
        Assert.Equal("bind.v1", request.CredentialBindingVersion);
        Assert.NotNull(request.FrozenDeployment);
        Assert.Equal(session.Binding.FrozenModelDeployment!.ProfileDigest, request.ProfileDigest);
        Assert.Equal("synthetic.model.pinned", request.RequestedModel);
        Assert.NotEqual("host.mutated.provider", request.ProviderId);
        Assert.NotEqual("bind.host.ignored", request.CredentialBindingReference);
    }

    [Fact]
    public async Task Restarted_content_phase_reconstructs_frozen_binding_without_process_local_adapter_state()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = SessionRuntimeTestFixtures.AdmitParticipant(session,
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.participant.1",
            "idem.p.restart",
            SessionRuntimeTestFixtures.T0);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var controlAdapter = new RecordingModelExecutionPort { CancelStream = true };
        controlAdapter.Inner.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                outputs: [SessionRuntimeTestFixtures.MessageOutput(turnId: null, responseSlotId: null)],
                decisionId: "adec.restart.control"));
        var store = CreateStore(session.Ownership, invocationId);
        var controlProcessor = CreateProcessor(controlAdapter, session, store);
        var control = await controlProcessor.TryProcessNextAsync(CancellationToken.None);
        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, control.Outcome);

        var restartedAdapter = new RecordingModelExecutionPort();
        restartedAdapter.Inner.EnqueueContent(
            new ModelContentTextDelta("Hi"),
            new ModelContentCompleted());
        store.ResetCompleted();
        var restarted = CreateProcessor(restartedAdapter, session, store);
        var published = await restarted.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Published, published.Outcome);
        Assert.Empty(restartedAdapter.ExecutionRequests);
        var stream = Assert.Single(restartedAdapter.StreamRequests);
        Assert.Equal(session.Binding.FrozenModelDeployment, stream.FrozenDeployment);
        Assert.Equal("synthetic.provider", stream.ProviderId);
        Assert.Equal("bind.opaque.0001", stream.CredentialBindingReference);
        Assert.NotNull(stream.Context);
        Assert.False(string.IsNullOrWhiteSpace(stream.ProviderAttemptId));
        Assert.True(stream.AttemptOrdinal >= 1);
    }

    [Fact]
    public async Task Blocking_control_call_renews_the_claim_lease()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.opening.lease",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new BlockingControlPort();
        adapter.Inner.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                DecisionDispositions.NoAction,
                [],
                [],
                NoActionReasonCategories.IntentionalSilence,
                "adec.lease.00000001"));
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
                InstalledProfiles: new InMemoryInstalledModelDeploymentProfileRegistry(
                    SessionRuntimeTestFixtures.CreateInstalledProfile()),
                CredentialCatalog: new InMemoryModelDeploymentCredentialCatalog(
                    SessionRuntimeTestFixtures.CreateCatalogRecord(session.Ownership.OrganizationId)),
                ClaimLease: TimeSpan.FromSeconds(30),
                ClaimLeaseRenewalPeriod: TimeSpan.FromMilliseconds(20)),
            PassThroughAgentResponsePublicationPersistPort.Succeed);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.Decided, result.Outcome);
        Assert.True(store.RenewCount >= 1);
    }

    [Fact]
    public async Task Lease_renewal_exception_cancels_the_in_flight_provider_call()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.opening.lease.throw",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new BlockingControlPort();
        adapter.Inner.EnqueueEnvelope(
            SessionRuntimeTestFixtures.Envelope(
                invocationId,
                DecisionDispositions.NoAction,
                [],
                [],
                NoActionReasonCategories.IntentionalSilence,
                "adec.lease.throw0001"));
        var store = new RestartStore(session.Ownership, invocationId) { ThrowOnRenew = true };
        var processor = new DurableInvocationWorkProcessor(
            store,
            new MemoryGateway(session),
            adapter,
            new CompleteInvocationHandler(),
            new DurableInvocationWorkSettings(
                SessionRuntimeTestFixtures.CreateActor(),
                "worker.session_runtime",
                65_536,
                InstalledProfiles: new InMemoryInstalledModelDeploymentProfileRegistry(
                    SessionRuntimeTestFixtures.CreateInstalledProfile()),
                CredentialCatalog: new InMemoryModelDeploymentCredentialCatalog(
                    SessionRuntimeTestFixtures.CreateCatalogRecord(session.Ownership.OrganizationId)),
                ClaimLease: TimeSpan.FromSeconds(30),
                ClaimLeaseRenewalPeriod: TimeSpan.FromMilliseconds(20)),
            PassThroughAgentResponsePublicationPersistPort.Succeed);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, result.Outcome);
        Assert.True(store.RenewCount >= 1);
    }

    [Fact]
    public async Task Lease_renewal_exception_retries_when_the_adapter_returns_cancelled()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.opening.lease.swallow",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        var adapter = new SwallowingCancellationControlPort();
        var store = new RestartStore(session.Ownership, invocationId) { ThrowOnRenew = true };
        var processor = new DurableInvocationWorkProcessor(
            store,
            new MemoryGateway(session),
            adapter,
            new CompleteInvocationHandler(),
            new DurableInvocationWorkSettings(
                SessionRuntimeTestFixtures.CreateActor(),
                "worker.session_runtime",
                65_536,
                InstalledProfiles: new InMemoryInstalledModelDeploymentProfileRegistry(
                    SessionRuntimeTestFixtures.CreateInstalledProfile()),
                CredentialCatalog: new InMemoryModelDeploymentCredentialCatalog(
                    SessionRuntimeTestFixtures.CreateCatalogRecord(session.Ownership.OrganizationId)),
                ClaimLease: TimeSpan.FromSeconds(30),
                ClaimLeaseRenewalPeriod: TimeSpan.FromMilliseconds(20)),
            PassThroughAgentResponsePublicationPersistPort.Succeed);

        var result = await processor.TryProcessNextAsync(CancellationToken.None);

        Assert.Equal(DurableInvocationWorkOutcomes.RetryLater, result.Outcome);
        Assert.True(store.RenewCount >= 1);
        Assert.False(session.Invocations[0].IsTerminal);
    }

    private static DurableInvocationWorkProcessor CreateProcessor(
        IModelExecutionPort adapter,
        SessionRuntime session,
        IDurableInvocationWorkStore store) =>
        new(
            store,
            new MemoryGateway(session),
            adapter,
            new CompleteInvocationHandler(),
            new DurableInvocationWorkSettings(
                SessionRuntimeTestFixtures.CreateActor(),
                "worker.session_runtime",
                65_536,
                InstalledProfiles: new InMemoryInstalledModelDeploymentProfileRegistry(
                    SessionRuntimeTestFixtures.CreateInstalledProfile()),
                CredentialCatalog: new InMemoryModelDeploymentCredentialCatalog(
                    SessionRuntimeTestFixtures.CreateCatalogRecord(session.Ownership.OrganizationId))),
            PassThroughAgentResponsePublicationPersistPort.Succeed);

    private static RestartStore CreateStore(SessionOwnership ownership, string invocationId) =>
        new(ownership, invocationId);

    private sealed class RecordingModelExecutionPort : IModelExecutionPort
    {
        public DeterministicFakeModelExecutionAdapter Inner { get; } = new();

        public List<ModelExecutionAttemptRequest> ExecutionRequests { get; } = [];

        public List<ModelContentStreamRequest> StreamRequests { get; } = [];

        public bool CancelStream { get; set; }

        public Task<ModelExecutionAttemptResult> ExecuteAsync(
            ModelExecutionAttemptRequest request,
            CancellationToken cancellationToken)
        {
            ExecutionRequests.Add(request);
            return Inner.ExecuteAsync(request, cancellationToken);
        }

        public IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
            ModelContentStreamRequest request,
            CancellationToken cancellationToken)
        {
            StreamRequests.Add(request);
            if (CancelStream)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return Inner.StreamParticipantVisibleContentAsync(request, cancellationToken);
        }
    }

    private sealed class SwallowingCancellationControlPort : IModelExecutionPort
    {
        public async Task<ModelExecutionAttemptResult> ExecuteAsync(
            ModelExecutionAttemptRequest request,
            CancellationToken cancellationToken)
        {
            var delayUntil = DateTime.UtcNow.AddMilliseconds(400);
            while (DateTime.UtcNow < delayUntil && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(10, CancellationToken.None);
            }

            return new ModelExecutionFailed(ExecutionAttemptOutcomeCategories.Cancelled);
        }

        public IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
            ModelContentStreamRequest request,
            CancellationToken cancellationToken) =>
            AsyncEnumerable.Empty<ModelContentEvent>();
    }

    private sealed class BlockingControlPort : IModelExecutionPort
    {
        public DeterministicFakeModelExecutionAdapter Inner { get; } = new();

        public async Task<ModelExecutionAttemptResult> ExecuteAsync(
            ModelExecutionAttemptRequest request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(80, cancellationToken);
            return await Inner.ExecuteAsync(request, cancellationToken);
        }

        public IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
            ModelContentStreamRequest request,
            CancellationToken cancellationToken) =>
            Inner.StreamParticipantVisibleContentAsync(request, cancellationToken);
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

        public void ResetCompleted() => _item.State = DurableSessionWorkStates.Pending;

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

        public int RenewCount { get; private set; }

        public bool ThrowOnRenew { get; init; }

        public Task<DateTimeOffset?> TryRenewClaimLeaseAsync(
            DurableInvocationWorkItem work,
            TimeSpan lease,
            CancellationToken cancellationToken)
        {
            RenewCount++;
            if (ThrowOnRenew)
            {
                throw new InvalidOperationException("Injected lease renewal failure.");
            }

            var until = DateTimeOffset.UtcNow.Add(lease);
            work.ClaimLeaseUntil = until;
            return Task.FromResult<DateTimeOffset?>(until);
        }
    }
}
