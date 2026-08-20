using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Sessions.OpenRouter;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.OpenRouter.Tests;

internal static class OpenRouterLivePhase20QualificationRunner
{
    private const string SecretName = "openrouter-api-key";

    public static async Task RunAsync(
        ITestOutputHelper output,
        string requiredPhase,
        OpenRouterLivePinnedRouteExpectation expected,
        string syntheticControlText,
        string syntheticContentText)
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
        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                requiredPhase,
                alreadyConsumed,
                out var denial),
            $"Sanitized Phase 20 refused before reserve: {denial} consumed={alreadyConsumed} phase={requiredPhase}.");

        var profiles = InstalledModelDeploymentProfileFile.Load(profilesPath);
        var configurations = OpenRouterInstalledConfigurationFile.Load(configurationsPath, profiles);
        var configuration = Assert.Single(configurations);
        Assert.True(
            OpenRouterLivePinnedRouteAcceptance.TryAccept(configuration, expected, out var routeDenial),
            $"Sanitized operator route refused: {routeDenial}.");
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
            budget.TryReserveExpected(alreadyConsumed, out var controlSlot),
            "The persistent qualification budget is unavailable, corrupt, busy, stale, or exhausted.");
        var control = await adapter.ExecuteAsync(
            ControlRequest(ownership, frozen, configuration.Profile, "ainv.or20ctl001", syntheticControlText),
            TestContext.Current.CancellationToken);
        WriteSanitized(output, "control", controlSlot, control, observer);
        AssertNoSensitiveLeak(control.ToString());
        AssertPinnedProvenance(control.Provenance, configuration.Profile, expected.Model, ModelProviderRequestPhases.Control);
        if (!OpenRouterLiveMatrixQualification.TryAuthorizeContentAfterControl(control, out var contentDenial))
        {
            Assert.Fail(
                $"Sanitized control not admitted; content was not reserved: denial={contentDenial} outcome={OutcomeName(control)} reason={Reason(control)} http={observer.StatusCode?.ToString() ?? "none"} class={observer.StatusClassification} cache={observer.CacheClassification} slot={controlSlot}/{OpenRouterLiveQualification.MaxInferenceRequests}.");
        }

        Assert.True(
            budget.TryReserveExpected(controlSlot, out var contentSlot),
            "The persistent qualification budget is unavailable, corrupt, busy, stale, or exhausted.");
        var events = new List<ModelContentEvent>();
        await foreach (var item in adapter.StreamParticipantVisibleContentAsync(
            StreamRequest(ownership, frozen, "ainv.or20cnt001", syntheticContentText),
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
            AssertPinnedProvenance(completed.Provenance, configuration.Profile, expected.Model, ModelProviderRequestPhases.Content);
        }

        if (!OpenRouterLiveMatrixQualification.TryQualify(
                control,
                events,
                configuration.Profile.MaxOutputTokens,
                out var qualificationDenial))
        {
            Assert.Fail(
                $"Sanitized matrix incomplete: denial={qualificationDenial} control={OutcomeName(control)} reason={Reason(control)} content_completed={completed is not null} content_failed={failed?.ReasonCategory ?? "none"} deltas={deltas.Length} visible_utf8={deltas.Sum(delta => System.Text.Encoding.UTF8.GetByteCount(delta.ExactUtf8Text))} tokens_out={completed?.Provenance?.OutputTokenCount?.ToString() ?? "none"} content_http={observer.StatusCode?.ToString() ?? "none"} content_class={observer.StatusClassification} content_cache={observer.CacheClassification}.");
        }

        output.WriteLine(
            "sanitized_matrix qualified_for=synthetic_development model={0} provider={1} control_slot={2} content_slot={3}",
            expected.Model,
            expected.ProviderIdentity,
            controlSlot,
            contentSlot);
    }

    private static void WriteSanitized(
        ITestOutputHelper output,
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
        string expectedModel,
        string phase)
    {
        Assert.NotNull(provenance);
        Assert.Equal(ModelDeploymentAdapterKinds.OpenRouter, provenance.AdapterKind);
        Assert.Equal(OpenRouterAdapterContracts.AdapterContractVersion, provenance.AdapterContractVersion);
        Assert.Equal(profile.ProfileId, provenance.ProfileId);
        Assert.Equal(profile.ProfileDigest, provenance.ProfileDigest);
        Assert.Equal(expectedModel, provenance.RequestedModel);
        Assert.Equal(expectedModel, provenance.ResolvedModelVersion);
        Assert.Equal(phase, provenance.Phase);
        if (string.Equals(provenance.OutcomeCategory, ExecutionAttemptOutcomeCategories.DecisionProduced, StringComparison.Ordinal)
            || string.Equals(provenance.OutcomeCategory, ExecutionAttemptOutcomeCategories.ContentProduced, StringComparison.Ordinal))
        {
            Assert.True(provenance.InputTokenCount is > 0);
            Assert.True(provenance.OutputTokenCount is >= 0);
        }
    }

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
            "msg.p.phase20",
            TranscriptAuthorTypes.Participant,
            "turn.phase20",
            new ProtectedContentRef("msg:msg.p.phase20", new string('d', 64)),
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
            "prat.phase20.control",
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
            "msg.p.phase20c",
            TranscriptAuthorTypes.Participant,
            "turn.phase20c",
            new ProtectedContentRef("msg:msg.p.phase20c", new string('e', 64)),
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
            "agen.phase20.1",
            frozen,
            context,
            1,
            "prat.phase20.content",
            frozen.ProviderId,
            frozen.CredentialBindingReference,
            frozen.CredentialBindingVersion);
    }
}
