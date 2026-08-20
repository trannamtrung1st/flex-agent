using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Sessions.OpenRouter;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterLivePinnedQualificationTests(ITestOutputHelper output)
{
    private const string ExpectedProfileId = "openrouter.synthetic.local.nemotron-3.5-lightning";
    private const string ExpectedModel = "nvidia/nemotron-3.5-lightning:free";
    private const string ExpectedProviderSlug = "nvidia";
    private const string ExpectedProviderIdentity = "Nvidia";
    private const string ExpectedAdapterDigest = "77754995939f05366000e0f90022e998cdc85d18b3f675b8d64307595b0361ac";
    private const string ExpectedProfileDigest = "52b47fe8a81ec93aad637d3d81fee665ee9a8230762ecad3204ad6963ca038ac";
    private const string SecretName = "openrouter-api-key";
    private const string SyntheticControlText = "synthetic.openrouter.phase9.control";
    private const string SyntheticContentText = "synthetic.openrouter.phase9.content";

    [Fact]
    public void Pinned_matrix_remains_opt_in_and_does_not_touch_operator_state()
    {
        Assert.False(OpenRouterLiveQualification.IsEnabled);
        Assert.Equal(
            "FLEXAGENT_OPENROUTER_INSTALLED_PROFILES_PATH",
            OpenRouterLiveQualification.InstalledProfilesPathEnvironmentVariable);
        Assert.Equal(
            "FLEXAGENT_OPENROUTER_CONFIGURATIONS_PATH",
            OpenRouterLiveQualification.ConfigurationsPathEnvironmentVariable);
    }

    [Fact(Explicit = true, Timeout = 180_000)]
    public async Task Pinned_control_then_content_records_sanitized_identity_attempt_cache_and_usage()
    {
        Assert.True(
            OpenRouterLiveQualification.IsEnabled,
            $"Set {OpenRouterLiveQualification.EnableEnvironmentVariable}=1 only for an approved live run.");
        Assert.True(
            OpenRouterLiveQualification.SyntheticDataPolicyAccepted,
            $"Set {OpenRouterLiveQualification.SyntheticDataPolicyEnvironmentVariable}=1 only after confirming every disclosed value is synthetic and accepting retention/training risk.");

        var profilesPath = RequiredEnvironment(OpenRouterLiveQualification.InstalledProfilesPathEnvironmentVariable);
        var configurationsPath = RequiredEnvironment(OpenRouterLiveQualification.ConfigurationsPathEnvironmentVariable);
        var budgetPath = RequiredEnvironment(OpenRouterLiveQualification.BudgetPathEnvironmentVariable);
        var budget = new OpenRouterQualificationBudget(budgetPath);
        Assert.True(budget.TryRead(out var alreadyConsumed));
        output.WriteLine(
            "sanitized_budget before={0}/{1}",
            alreadyConsumed,
            OpenRouterLiveQualification.MaxInferenceRequests);

        var profiles = InstalledModelDeploymentProfileFile.Load(profilesPath);
        var configurations = OpenRouterInstalledConfigurationFile.Load(configurationsPath, profiles);
        var configuration = Assert.Single(configurations);
        Assert.Equal(ExpectedProfileId, configuration.Profile.ProfileId);
        Assert.Equal(ExpectedModel, configuration.Profile.RequestedModel);
        Assert.Equal(ExpectedModel, configuration.Profile.ResolvedModelVersion);
        Assert.Equal(ExpectedProviderSlug, configuration.ProviderSlug);
        Assert.Equal(ExpectedProviderIdentity, configuration.ExpectedReturnedProviderIdentity);
        Assert.Equal(ExpectedAdapterDigest, configuration.AdapterConfigurationDigest);
        Assert.Equal(ExpectedProfileDigest, configuration.Profile.ProfileDigest);
        Assert.Equal(OpenRouterAdapterContracts.MaxOutputTokens, configuration.Profile.MaxOutputTokens);
        Assert.Equal(OpenRouterAdapterContracts.ControlTimeout, configuration.Profile.ControlTimeout);
        Assert.Equal(OpenRouterAdapterContracts.ContentTimeout, configuration.Profile.ContentTimeout);
        Assert.Equal(OpenRouterAdapterContracts.MaxApplicationAttempts, configuration.Profile.MaxProviderRequestAttempts);

        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var catalog = new InMemoryModelDeploymentCredentialCatalog(
            SessionRuntimeTestFixtures.CreateCatalogRecord(
                ownership.OrganizationId,
                providerId: configuration.Profile.ProviderId,
                secretName: SecretName));
        var frozen = new FrozenModelDeploymentBinding(
            configuration.Profile.ProfileId,
            configuration.Profile.ProfileVersion,
            configuration.Profile.ProfileDigest,
            configuration.Profile.ProviderId,
            configuration.Profile.CredentialMode,
            "bind.opaque.0001",
            "bind.v1");
        var secretRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "flex-agent",
            "secrets");
        using var observer = new OpenRouterSanitizedLiveObserver(
            new HttpClientHandler { AllowAutoRedirect = false });
        var adapter = new OpenRouterModelExecutionAdapter(
            new InMemoryInstalledModelDeploymentProfileRegistry(profiles),
            catalog,
            new UnixOwnerOnlyMountedFileProviderSecretSource(secretRoot),
            new InMemoryOpenRouterInstalledConfigurationRegistry(configurations),
            observer,
            syntheticDataPolicyAccepted: true);

        Assert.True(
            budget.TryReserve(out var controlSlot),
            "The persistent qualification budget is unavailable, corrupt, busy, or exhausted.");
        var control = await adapter.ExecuteAsync(
            ControlRequest(ownership, frozen, configuration.Profile, "ainv.or9ctl001", SyntheticControlText),
            TestContext.Current.CancellationToken);
        WriteSanitized("control", controlSlot, control, observer);
        var controlHttp = observer.StatusCode?.ToString() ?? "none";
        var controlClass = observer.StatusClassification;
        var controlCache = observer.CacheClassification;
        AssertNoSensitiveLeak(control.ToString());
        AssertPinnedProvenance(control.Provenance, configuration.Profile, ModelProviderRequestPhases.Control);
        if (IsHardStop(control, observer))
        {
            Assert.Fail(
                $"Sanitized control hard-stop: outcome={OutcomeName(control)} reason={Reason(control)} http={controlHttp} class={controlClass} cache={controlCache} slot={controlSlot}/{OpenRouterLiveQualification.MaxInferenceRequests}.");
        }

        Assert.True(
            budget.TryReserve(out var contentSlot),
            "The persistent qualification budget is unavailable, corrupt, busy, or exhausted.");
        var events = new List<ModelContentEvent>();
        await foreach (var item in adapter.StreamParticipantVisibleContentAsync(
            StreamRequest(ownership, frozen, "ainv.or9cnt001", SyntheticContentText),
            TestContext.Current.CancellationToken))
        {
            events.Add(item);
            AssertNoSensitiveLeak(item.ToString());
        }

        var deltas = events.OfType<ModelContentTextDelta>().ToArray();
        var completed = events.OfType<ModelContentCompleted>().SingleOrDefault();
        var failed = events.OfType<ModelContentFailed>().SingleOrDefault();
        output.WriteLine(
            "sanitized_content request={0}/{1} deltas={2} visible_utf8={3} completed={4} failed={5} tokens_in={6} tokens_out={7} outcome={8} http={9} class={10} cache={11}",
            contentSlot,
            OpenRouterLiveQualification.MaxInferenceRequests,
            deltas.Length,
            deltas.Sum(delta => System.Text.Encoding.UTF8.GetByteCount(delta.ExactUtf8Text)),
            completed is not null,
            failed?.ReasonCategory ?? "none",
            completed?.Provenance?.InputTokenCount?.ToString() ?? "none",
            completed?.Provenance?.OutputTokenCount?.ToString() ?? "none",
            completed is not null ? ExecutionAttemptOutcomeCategories.ContentProduced : failed?.ReasonCategory ?? "none",
            observer.StatusCode?.ToString() ?? "none",
            observer.StatusClassification,
            observer.CacheClassification);
        if (completed is not null)
        {
            AssertPinnedProvenance(completed.Provenance, configuration.Profile, ModelProviderRequestPhases.Content);
        }

        if (failed is not null && IsHardStop(failed, observer))
        {
            Assert.Fail(
                $"Sanitized content hard-stop: reason={failed.ReasonCategory} http={observer.StatusCode?.ToString() ?? "none"} class={observer.StatusClassification} cache={observer.CacheClassification} slot={contentSlot}/{OpenRouterLiveQualification.MaxInferenceRequests}.");
        }

        if (control is not ModelExecutionStructuredControl || completed is null)
        {
            Assert.Fail(
                $"Sanitized matrix incomplete: control={OutcomeName(control)} reason={Reason(control)} control_http={controlHttp} control_class={controlClass} control_cache={controlCache} content_completed={completed is not null} content_failed={failed?.ReasonCategory ?? "none"} content_http={observer.StatusCode?.ToString() ?? "none"} content_class={observer.StatusClassification} content_cache={observer.CacheClassification}.");
        }

        output.WriteLine(
            "sanitized_matrix qualified_for=synthetic_development model={0} provider={1} control_slot={2} content_slot={3}",
            ExpectedModel,
            ExpectedProviderIdentity,
            controlSlot,
            contentSlot);
    }

    private void WriteSanitized(
        string phase,
        int slot,
        ModelExecutionAttemptResult result,
        OpenRouterSanitizedLiveObserver observer)
    {
        output.WriteLine(
            "sanitized_{0} request={1}/{2} outcome={3} reason={4} tokens_in={5} tokens_out={6} model={7} digest={8} http={9} class={10} cache={11}",
            phase,
            slot,
            OpenRouterLiveQualification.MaxInferenceRequests,
            OutcomeName(result),
            Reason(result) ?? "none",
            result.Provenance?.InputTokenCount?.ToString() ?? "none",
            result.Provenance?.OutputTokenCount?.ToString() ?? "none",
            result.Provenance?.ResolvedModelVersion ?? "none",
            result.Provenance?.ProfileDigest ?? "none",
            observer.StatusCode?.ToString() ?? "none",
            observer.StatusClassification,
            observer.CacheClassification);
    }

    private static void AssertPinnedProvenance(
        ModelProviderAttemptProvenance? provenance,
        InstalledModelDeploymentProfile profile,
        string phase)
    {
        Assert.NotNull(provenance);
        Assert.Equal(ModelDeploymentAdapterKinds.OpenRouter, provenance.AdapterKind);
        Assert.Equal(OpenRouterAdapterContracts.AdapterContractVersion, provenance.AdapterContractVersion);
        Assert.Equal(profile.ProfileId, provenance.ProfileId);
        Assert.Equal(profile.ProfileDigest, provenance.ProfileDigest);
        Assert.Equal(ExpectedModel, provenance.RequestedModel);
        Assert.Equal(ExpectedModel, provenance.ResolvedModelVersion);
        Assert.Equal(phase, provenance.Phase);
        if (string.Equals(provenance.OutcomeCategory, ExecutionAttemptOutcomeCategories.DecisionProduced, StringComparison.Ordinal)
            || string.Equals(provenance.OutcomeCategory, ExecutionAttemptOutcomeCategories.ContentProduced, StringComparison.Ordinal))
        {
            Assert.True(provenance.InputTokenCount is > 0);
            Assert.True(provenance.OutputTokenCount is >= 0);
        }
    }

    private static bool IsHardStop(ModelExecutionAttemptResult result, OpenRouterSanitizedLiveObserver observer) =>
        result is ModelExecutionFailed failed && IsHardStop(failed, observer);

    private static bool IsHardStop(ModelContentFailed failed, OpenRouterSanitizedLiveObserver observer) =>
        observer.IsHttpHardStop
        || string.Equals(failed.ReasonCategory, ExecutionFailureReasons.ProviderTimeout, StringComparison.Ordinal)
        || string.Equals(failed.ReasonCategory, ExecutionFailureReasons.CredentialBindingFailed, StringComparison.Ordinal)
        || string.Equals(failed.ReasonCategory, ExecutionAttemptOutcomeCategories.Cancelled, StringComparison.Ordinal)
        || (string.Equals(failed.ReasonCategory, ExecutionFailureReasons.ProviderUnavailable, StringComparison.Ordinal)
            && observer.StatusCode == 200);

    private static bool IsHardStop(ModelExecutionFailed failed, OpenRouterSanitizedLiveObserver observer) =>
        observer.IsHttpHardStop
        || string.Equals(failed.ReasonCategory, ExecutionFailureReasons.ProviderTimeout, StringComparison.Ordinal)
        || string.Equals(failed.ReasonCategory, ExecutionFailureReasons.CredentialBindingFailed, StringComparison.Ordinal)
        || string.Equals(failed.ReasonCategory, ExecutionAttemptOutcomeCategories.Cancelled, StringComparison.Ordinal)
        || (string.Equals(failed.ReasonCategory, ExecutionFailureReasons.ProviderUnavailable, StringComparison.Ordinal)
            && observer.StatusCode == 200);

    private static string OutcomeName(ModelExecutionAttemptResult result) =>
        result switch
        {
            ModelExecutionStructuredControl => "structured_control",
            ModelExecutionFailed => "failed",
            _ => result.GetType().Name,
        };

    private static string? Reason(ModelExecutionAttemptResult result) =>
        result is ModelExecutionFailed failed ? failed.ReasonCategory : result.Provenance?.OutcomeCategory;

    private static void AssertNoSensitiveLeak(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        Assert.DoesNotContain("Bearer ", value, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-or-", value, StringComparison.Ordinal);
    }

    private static string RequiredEnvironment(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        Assert.False(string.IsNullOrWhiteSpace(value), $"Set {name} to the operator-managed path.");
        return value;
    }

    private static ModelExecutionAttemptRequest ControlRequest(
        SessionOwnership ownership,
        FrozenModelDeploymentBinding frozen,
        InstalledModelDeploymentProfile profile,
        string invocationId,
        string participantText)
    {
        var transcript = new VisibleTranscriptItemRef(
            "msg.p.phase9",
            TranscriptAuthorTypes.Participant,
            "turn.phase9",
            new ProtectedContentRef("msg:msg.p.phase9", new string('d', 64)),
            participantText);
        var context = new InvocationContext(
            ownership,
            new string('a', 64),
            new string('b', 64),
            [],
            [],
            [],
            [transcript],
            [InvocationContextFactCategories.TranscriptItem]);
        return new ModelExecutionAttemptRequest(
            ownership,
            invocationId,
            frozen.ProviderId,
            frozen.CredentialBindingReference,
            frozen.CredentialBindingVersion,
            context,
            1,
            65_536,
            frozen,
            "prat.phase9.control",
            profile.RequestedModel,
            profile.ProfileDigest);
    }

    private static ModelContentStreamRequest StreamRequest(
        SessionOwnership ownership,
        FrozenModelDeploymentBinding frozen,
        string invocationId,
        string participantText)
    {
        var transcript = new VisibleTranscriptItemRef(
            "msg.p.phase9c",
            TranscriptAuthorTypes.Participant,
            "turn.phase9c",
            new ProtectedContentRef("msg:msg.p.phase9c", new string('e', 64)),
            participantText);
        var context = new InvocationContext(
            ownership,
            new string('a', 64),
            new string('b', 64),
            [],
            [],
            [],
            [transcript],
            [InvocationContextFactCategories.TranscriptItem]);
        return new ModelContentStreamRequest(
            ownership,
            invocationId,
            "agen.phase9.1",
            frozen,
            context,
            1,
            "prat.phase9.content",
            frozen.ProviderId,
            frozen.CredentialBindingReference,
            frozen.CredentialBindingVersion);
    }
}
