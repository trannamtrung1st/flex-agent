using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenRouter;

public sealed class OpenRouterModelExecutionAdapter(
    IInstalledModelDeploymentProfileRegistry profiles,
    IModelDeploymentCredentialCatalog catalog,
    IProviderCredentialSecretSource secrets,
    IOpenRouterInstalledConfigurationRegistry configurations,
    HttpMessageHandler? transport = null,
    bool syntheticDataPolicyAccepted = false) : IModelExecutionPort
{
    public const string AdapterContractVersion = OpenRouterAdapterContracts.AdapterContractVersion;

    internal TimeSpan? TestControlTimeout { get; init; }

    internal TimeSpan? TestContentTimeout { get; init; }

    public async Task<ModelExecutionAttemptResult> ExecuteAsync(
        ModelExecutionAttemptRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var startedAt = DateTimeOffset.UtcNow;
        if (!syntheticDataPolicyAccepted)
        {
            return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, null);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Fail(ExecutionAttemptOutcomeCategories.Cancelled, startedAt, request, null);
        }

        var resolved = TryResolve(request.FrozenDeployment, request.Ownership, out var failure);
        if (resolved is null)
        {
            return Fail(failure!, startedAt, request, null);
        }

        using var secret = await secrets.TryReadAsync(resolved.Value.SecretName, cancellationToken);
        if (secret is null)
        {
            return Fail(ExecutionFailureReasons.CredentialBindingFailed, startedAt, request, resolved.Value.Profile);
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TestControlTimeout ?? resolved.Value.Profile.ControlTimeout);
            var operation = timeoutCts.Token;
            using var lifetime = CreateClient();
            using var httpRequest = OpenRouterRequestFactory.CreateControl(
                resolved.Value.Profile,
                resolved.Value.Configuration,
                request.AgentInvocationId,
                request.Context,
                secret.Reveal());
            using var response = await lifetime.Client.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                operation);
            if (!response.IsSuccessStatusCode)
            {
                return Fail(MapStatus(response.StatusCode), startedAt, request, resolved.Value.Profile);
            }

            if (OpenRouterResponseParser.IsResponseCacheHit(response))
            {
                return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, resolved.Value.Profile);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(operation);
            await using var bounded = new BoundedReadStream(stream, OpenRouterAdapterContracts.MaxControlEnvelopeUtf8Bytes);
            using var document = await JsonDocument.ParseAsync(bounded, cancellationToken: operation);
            if (!OpenRouterResponseParser.TryReadTerminalFacts(
                    document.RootElement,
                    resolved.Value.Profile.ResolvedModelVersion,
                    resolved.Value.Configuration.ExpectedReturnedProviderIdentity,
                    out var facts,
                    out _)
                || facts is null)
            {
                return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, resolved.Value.Profile);
            }

            if (OpenRouterResponseParser.ContainsHiddenReasoning(document.RootElement))
            {
                return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, resolved.Value.Profile);
            }

            if (!OpenRouterAdapterContracts.IsApprovedNonTruncationFinishReason(facts.FinishReason))
            {
                return Fail(
                    ExecutionFailureReasons.MalformedControl,
                    startedAt,
                    request,
                    resolved.Value.Profile,
                    facts.InputTokens,
                    facts.OutputTokens,
                    facts.FinishReason);
            }

            if (!OpenRouterResponseParser.TryReadControlContent(document.RootElement, out var content)
                || string.IsNullOrEmpty(content))
            {
                return Fail(ExecutionFailureReasons.MalformedControl, startedAt, request, resolved.Value.Profile);
            }

            var utf8 = System.Text.Encoding.UTF8.GetBytes(content);
            var outcome = AdmitControl(request, utf8);
            return outcome with
            {
                Provenance = CreateProvenance(
                    resolved.Value.Profile,
                    Outcome(outcome),
                    facts.InputTokens,
                    facts.OutputTokens,
                    request.ProviderAttemptId,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    ModelProviderRequestPhases.Control,
                    facts.FinishReason),
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Fail(ExecutionAttemptOutcomeCategories.Cancelled, startedAt, request, resolved.Value.Profile);
        }
        catch (OperationCanceledException)
        {
            return Fail(ExecutionFailureReasons.ProviderTimeout, startedAt, request, resolved.Value.Profile);
        }
        catch (OpenRouterTransportLimitExceededException)
        {
            return Fail(ExecutionFailureReasons.MalformedControl, startedAt, request, resolved.Value.Profile);
        }
        catch (DecoderFallbackException)
        {
            return Fail(ExecutionFailureReasons.MalformedControl, startedAt, request, resolved.Value.Profile);
        }
        catch (HttpRequestException)
        {
            return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, resolved.Value.Profile);
        }
        catch (JsonException)
        {
            return Fail(ExecutionFailureReasons.MalformedControl, startedAt, request, resolved.Value.Profile);
        }
    }

    public async IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
        ModelContentStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!syntheticDataPolicyAccepted)
        {
            yield break;
        }

        var resolved = TryResolve(request.FrozenDeployment, request.Ownership, out _);
        if (resolved is null)
        {
            yield break;
        }

        using var secret = await secrets.TryReadAsync(resolved.Value.SecretName, cancellationToken);
        if (secret is null)
        {
            yield break;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TestContentTimeout ?? resolved.Value.Profile.ContentTimeout);
        var operation = timeoutCts.Token;
        var startedAt = DateTimeOffset.UtcNow;
        var publishedFragment = false;
        var lifetime = CreateClient();
        HttpResponseMessage? response = null;
        Stream? stream = null;
        string? startupFailure = null;
        try
        {
            using var httpRequest = OpenRouterRequestFactory.CreateContent(
                resolved.Value.Profile,
                resolved.Value.Configuration,
                request.AgentInvocationId,
                request.Context,
                secret.Reveal());
            response = await lifetime.Client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, operation);
            if (!response.IsSuccessStatusCode)
            {
                startupFailure = MapStatus(response.StatusCode);
            }
            else if (OpenRouterResponseParser.IsResponseCacheHit(response))
            {
                startupFailure = ExecutionFailureReasons.ProviderUnavailable;
            }
            else
            {
                stream = await response.Content.ReadAsStreamAsync(operation);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            startupFailure = ExecutionAttemptOutcomeCategories.Cancelled;
        }
        catch (OperationCanceledException)
        {
            startupFailure = ExecutionFailureReasons.ProviderTimeout;
        }
        catch (HttpRequestException)
        {
            startupFailure = ExecutionFailureReasons.ProviderUnavailable;
        }

        if (startupFailure is not null)
        {
            lifetime.Dispose();
            response?.Dispose();
            yield return ContentFailure(resolved.Value.Profile, startupFailure, request.ProviderAttemptId, startedAt);
            if (string.Equals(startupFailure, ExecutionAttemptOutcomeCategories.Cancelled, StringComparison.Ordinal))
            {
                throw new OperationCanceledException(cancellationToken);
            }

            yield break;
        }

        OpenRouterTerminalFacts? facts = null;
        var sawTerminal = false;
        var sawDone = false;
        var visibleUtf8Bytes = 0;
        await using var enumerator = OpenRouterSseParser.ReadDataPayloadsAsync(stream!, operation)
            .GetAsyncEnumerator(operation);
        while (true)
        {
            bool moved;
            string? failureReason = null;
            try
            {
                moved = await enumerator.MoveNextAsync();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                failureReason = ExecutionAttemptOutcomeCategories.Cancelled;
                moved = false;
            }
            catch (OperationCanceledException)
            {
                failureReason = ExecutionFailureReasons.ProviderTimeout;
                moved = false;
            }
            catch (OpenRouterTransportLimitExceededException)
            {
                failureReason = publishedFragment
                    ? ExecutionFailureReasons.ProviderUnavailable
                    : ExecutionFailureReasons.MalformedControl;
                moved = false;
            }
            catch (DecoderFallbackException)
            {
                failureReason = publishedFragment
                    ? ExecutionFailureReasons.ProviderUnavailable
                    : ExecutionFailureReasons.MalformedControl;
                moved = false;
            }
            catch (HttpRequestException)
            {
                failureReason = ExecutionFailureReasons.ProviderUnavailable;
                moved = false;
            }

            if (failureReason is not null)
            {
                yield return ContentFailure(resolved.Value.Profile, failureReason, request.ProviderAttemptId, startedAt);
                await enumerator.DisposeAsync();
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }

                response?.Dispose();
                lifetime.Dispose();
                if (string.Equals(failureReason, ExecutionAttemptOutcomeCategories.Cancelled, StringComparison.Ordinal))
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                yield break;
            }

            if (!moved)
            {
                break;
            }

            var payload = enumerator.Current;
            if (sawDone)
            {
                yield return ContentFailure(
                    resolved.Value.Profile,
                    ExecutionFailureReasons.ProviderUnavailable,
                    request.ProviderAttemptId,
                    startedAt);
                await enumerator.DisposeAsync();
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }

                response?.Dispose();
                lifetime.Dispose();
                yield break;
            }

            if (string.Equals(payload, "[DONE]", StringComparison.Ordinal))
            {
                if (!sawTerminal)
                {
                    yield return ContentFailure(
                        resolved.Value.Profile,
                        ExecutionFailureReasons.ProviderUnavailable,
                        request.ProviderAttemptId,
                        startedAt);
                    await enumerator.DisposeAsync();
                    if (stream is not null)
                    {
                        await stream.DisposeAsync();
                    }

                    response?.Dispose();
                    lifetime.Dispose();
                    yield break;
                }

                sawDone = true;
                continue;
            }

            if (sawTerminal)
            {
                yield return ContentFailure(
                    resolved.Value.Profile,
                    ExecutionFailureReasons.ProviderUnavailable,
                    request.ProviderAttemptId,
                    startedAt);
                await enumerator.DisposeAsync();
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }

                response?.Dispose();
                lifetime.Dispose();
                yield break;
            }

            JsonDocument? document = null;
            string? parseFailure = null;
            try
            {
                document = JsonDocument.Parse(payload);
            }
            catch (JsonException)
            {
                parseFailure = publishedFragment
                    ? ExecutionFailureReasons.ProviderUnavailable
                    : ExecutionFailureReasons.MalformedControl;
            }

            if (parseFailure is not null)
            {
                yield return ContentFailure(
                    resolved.Value.Profile,
                    parseFailure,
                    request.ProviderAttemptId,
                    startedAt);
                await enumerator.DisposeAsync();
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }

                response?.Dispose();
                lifetime.Dispose();
                yield break;
            }

            var parsed = document ?? throw new InvalidOperationException("OpenRouter SSE JSON parse produced no document.");
            using (parsed)
            {
                if (OpenRouterResponseParser.ContainsHiddenReasoning(document.RootElement))
                {
                    yield return ContentFailure(
                        resolved.Value.Profile,
                        ExecutionFailureReasons.ProviderUnavailable,
                        request.ProviderAttemptId,
                        startedAt);
                    await enumerator.DisposeAsync();
                    if (stream is not null)
                    {
                        await stream.DisposeAsync();
                    }

                    response?.Dispose();
                    lifetime.Dispose();
                    yield break;
                }

                var hasDelta = OpenRouterResponseParser.TryReadDelta(document.RootElement, out var delta, out var malformedString);
                if (malformedString)
                {
                    yield return ContentFailure(
                        resolved.Value.Profile,
                        publishedFragment
                            ? ExecutionFailureReasons.ProviderUnavailable
                            : ExecutionFailureReasons.MalformedControl,
                        request.ProviderAttemptId,
                        startedAt);
                    await enumerator.DisposeAsync();
                    if (stream is not null)
                    {
                        await stream.DisposeAsync();
                    }

                    response?.Dispose();
                    lifetime.Dispose();
                    yield break;
                }

                if (hasDelta && !string.IsNullOrEmpty(delta))
                {
                    visibleUtf8Bytes += System.Text.Encoding.UTF8.GetByteCount(delta);
                    if (visibleUtf8Bytes > OpenRouterAdapterContracts.MaxVisibleContentUtf8Bytes)
                    {
                        yield return ContentFailure(
                            resolved.Value.Profile,
                            publishedFragment
                                ? ExecutionFailureReasons.ProviderUnavailable
                                : ExecutionFailureReasons.MalformedControl,
                            request.ProviderAttemptId,
                            startedAt);
                        await enumerator.DisposeAsync();
                        if (stream is not null)
                        {
                            await stream.DisposeAsync();
                        }

                        response?.Dispose();
                        lifetime.Dispose();
                        yield break;
                    }

                    publishedFragment = true;
                    yield return new ModelContentTextDelta(delta);
                }

                if (document.RootElement.TryGetProperty("openrouter_metadata", out _))
                {
                    if (sawTerminal
                        || !OpenRouterResponseParser.TryReadTerminalFacts(
                            document.RootElement,
                            resolved.Value.Profile.ResolvedModelVersion,
                            resolved.Value.Configuration.ExpectedReturnedProviderIdentity,
                            out facts,
                            out _))
                    {
                        yield return ContentFailure(
                            resolved.Value.Profile,
                            ExecutionFailureReasons.ProviderUnavailable,
                            request.ProviderAttemptId,
                            startedAt);
                        await enumerator.DisposeAsync();
                        if (stream is not null)
                        {
                            await stream.DisposeAsync();
                        }

                        response?.Dispose();
                        lifetime.Dispose();
                        yield break;
                    }

                    sawTerminal = true;
                }
            }
        }

        await enumerator.DisposeAsync();
        if (stream is not null)
        {
            await stream.DisposeAsync();
        }

        response?.Dispose();
        lifetime.Dispose();
        if (!sawDone || facts is null)
        {
            yield return ContentFailure(
                resolved.Value.Profile,
                ExecutionFailureReasons.ProviderUnavailable,
                request.ProviderAttemptId,
                startedAt);
            yield break;
        }

        yield return new ModelContentCompleted
        {
            Provenance = CreateProvenance(
                resolved.Value.Profile,
                ExecutionAttemptOutcomeCategories.ContentProduced,
                facts.InputTokens,
                facts.OutputTokens,
                request.ProviderAttemptId,
                startedAt,
                DateTimeOffset.UtcNow,
                ModelProviderRequestPhases.Content,
                facts.FinishReason),
        };
    }

    private (InstalledModelDeploymentProfile Profile, OpenRouterInstalledConfiguration Configuration, string SecretName)? TryResolve(
        FrozenModelDeploymentBinding? frozen,
        SessionOwnership ownership,
        out string? failure)
    {
        failure = ExecutionFailureReasons.CredentialBindingFailed;
        if (frozen is null)
        {
            return null;
        }

        var resolution = FrozenModelDeploymentResolver.Resolve(ownership, frozen, profiles, catalog);
        if (!resolution.Succeeded || resolution.Profile is null || resolution.SecretName is null)
        {
            return null;
        }

        if (!string.Equals(resolution.Profile.AdapterKind, ModelDeploymentAdapterKinds.OpenRouter, StringComparison.Ordinal)
            || !string.Equals(resolution.Profile.AdapterContractVersion, AdapterContractVersion, StringComparison.Ordinal))
        {
            return null;
        }

        var configuration = configurations.TryGet(
            resolution.Profile.ProfileId,
            resolution.Profile.ProfileVersion,
            resolution.Profile.ProfileDigest);
        if (configuration is null
            || !string.Equals(configuration.AdapterConfigurationDigest, resolution.Profile.AdapterConfigurationDigest, StringComparison.Ordinal)
            || !string.Equals(configuration.Profile.RequestedModel, resolution.Profile.RequestedModel, StringComparison.Ordinal)
            || !string.Equals(configuration.Profile.ResolvedModelVersion, resolution.Profile.ResolvedModelVersion, StringComparison.Ordinal))
        {
            return null;
        }

        failure = null;
        return (resolution.Profile, configuration, resolution.SecretName);
    }

    private ClientLifetime CreateClient()
    {
        var inner = transport ?? new HttpClientHandler { AllowAutoRedirect = false };
        var bounded = new OpenRouterDestinationHandler(inner);
        var http = new HttpClient(bounded, disposeHandler: transport is null)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        return new ClientLifetime(http);
    }

    private static ModelExecutionAttemptResult AdmitControl(ModelExecutionAttemptRequest request, byte[] utf8Json)
    {
        if (utf8Json.Length > request.MaxControlUtf8Bytes)
        {
            return new ModelExecutionFailed(ExecutionFailureReasons.MalformedControl);
        }

        if (!ValidatedAgentDecisionEnvelope.TryAdmit(utf8Json, out var admitted, out var failureReasonCategory)
            || admitted is null)
        {
            return new ModelExecutionFailed(failureReasonCategory);
        }

        if (!string.Equals(admitted.Envelope.InvocationId, request.AgentInvocationId, StringComparison.Ordinal))
        {
            return new ModelExecutionFailed(ExecutionFailureReasons.MalformedControl);
        }

        return new ModelExecutionStructuredControl(admitted);
    }

    private static string MapStatus(HttpStatusCode status) =>
        (int)status switch
        {
            408 or 504 => ExecutionFailureReasons.ProviderTimeout,
            _ => ExecutionFailureReasons.ProviderUnavailable,
        };

    private static string Outcome(ModelExecutionAttemptResult result) =>
        result is ModelExecutionStructuredControl
            ? ExecutionAttemptOutcomeCategories.DecisionProduced
            : result is ModelExecutionFailed failed
                ? failed.ReasonCategory
                : ExecutionFailureReasons.MalformedControl;

    private static ModelExecutionFailed Fail(
        string reason,
        DateTimeOffset startedAt,
        ModelExecutionAttemptRequest request,
        InstalledModelDeploymentProfile? profile,
        int? inputTokens = null,
        int? outputTokens = null,
        string? terminalFinishReason = null) =>
        new ModelExecutionFailed(reason)
        {
            Provenance = profile is null
                ? null
                : CreateProvenance(
                    profile,
                    reason,
                    inputTokens,
                    outputTokens,
                    request.ProviderAttemptId,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    ModelProviderRequestPhases.Control,
                    terminalFinishReason),
        };

    private static ModelProviderAttemptProvenance CreateProvenance(
        InstalledModelDeploymentProfile profile,
        string outcome,
        int? inputTokens,
        int? outputTokens,
        string? providerAttemptId,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        string phase,
        string? terminalFinishReason = null) =>
        new(
            profile.AdapterKind,
            AdapterContractVersion,
            profile.ProfileId,
            profile.ProfileVersion,
            profile.ProfileDigest,
            profile.RequestedModel,
            profile.ResolvedModelVersion,
            outcome,
            inputTokens,
            outputTokens,
            string.IsNullOrWhiteSpace(providerAttemptId) ? null : $"pref.{providerAttemptId}",
            startedAt,
            completedAt,
            phase,
            providerAttemptId,
            ModelProviderRequestFacts.Finished,
            terminalFinishReason);

    private static ModelContentFailed ContentFailure(
        InstalledModelDeploymentProfile profile,
        string reason,
        string? providerAttemptId,
        DateTimeOffset startedAt) =>
        new(reason)
        {
            Provenance = CreateProvenance(
                profile,
                reason,
                null,
                null,
                providerAttemptId,
                startedAt,
                DateTimeOffset.UtcNow,
                ModelProviderRequestPhases.Content),
        };

    private sealed class ClientLifetime(HttpClient client) : IDisposable
    {
        public HttpClient Client { get; } = client;

        public void Dispose() => Client.Dispose();
    }
}

public static class OpenRouterLiveQualification
{
    public const string EnableEnvironmentVariable = "FLEXAGENT_LIVE_OPENROUTER_QUALIFICATION";
    public const string SyntheticDataPolicyEnvironmentVariable = "FLEXAGENT_OPENROUTER_SYNTHETIC_DATA_POLICY_ACCEPTED";
    public const string BudgetPathEnvironmentVariable = "FLEXAGENT_OPENROUTER_QUALIFICATION_BUDGET_PATH";
    public const string Phase21BudgetPathEnvironmentVariable = "FLEXAGENT_OPENROUTER_PHASE21_QUALIFICATION_BUDGET_PATH";
    public const string InstalledProfilesPathEnvironmentVariable = "FLEXAGENT_OPENROUTER_INSTALLED_PROFILES_PATH";
    public const string ConfigurationsPathEnvironmentVariable = "FLEXAGENT_OPENROUTER_CONFIGURATIONS_PATH";
    public const string ExpectedConsumedEnvironmentVariable = "FLEXAGENT_OPENROUTER_QUALIFICATION_EXPECTED_CONSUMED";
    public const string PhaseEnvironmentVariable = "FLEXAGENT_OPENROUTER_LIVE_PHASE";
    public const string DiscoveryPhase = "discovery";
    public const string GptOssDarkbloomPhase = "gpt-oss-darkbloom-matrix";
    public const int DiscoveryRetiredAtConsumed = 6;
    public const int GptOssDarkbloomStartsAtConsumed = 0;
    public const int MaxInferenceRequests = 24;
    public const int Phase21MaxInferenceRequests = 8;
    public const string GptOssDarkbloomProfileId = "openrouter.synthetic.local.gpt-oss-20b";
    public const string GptOssDarkbloomModel = "openai/gpt-oss-20b:free";
    public const string GptOssDarkbloomProviderSlug = "darkbloom";
    public const string GptOssDarkbloomProviderIdentity = "Darkbloom";
    public const string GptOssDarkbloomAdapterDigest = "a291c5a83dade637fe78fd68b0cb964ecbb704b48723ecad348eaa8a53810e7a";
    public const string GptOssDarkbloomProfileDigest = "9b4c641463dd33ab3c8900f00089eebb08e8abb9d1700592ba23292d0e7ce611";

    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable(EnableEnvironmentVariable), "1", StringComparison.Ordinal);

    public static bool SyntheticDataPolicyAccepted =>
        string.Equals(Environment.GetEnvironmentVariable(SyntheticDataPolicyEnvironmentVariable), "1", StringComparison.Ordinal);

    public static bool TryAuthorizeReservation(
        string requiredPhase,
        int currentConsumed,
        out string denialReason) =>
        TryAuthorizeReservation(
            requiredPhase,
            Environment.GetEnvironmentVariable(PhaseEnvironmentVariable),
            currentConsumed,
            Environment.GetEnvironmentVariable(ExpectedConsumedEnvironmentVariable),
            out denialReason);

    public static bool TryAuthorizeReservation(
        string requiredPhase,
        string? configuredPhase,
        int currentConsumed,
        string? expectedConsumedText,
        out string denialReason)
    {
        if (!string.Equals(configuredPhase, requiredPhase, StringComparison.Ordinal))
        {
            denialReason = "phase_mismatch";
            return false;
        }

        if (!int.TryParse(
                expectedConsumedText,
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out var expected)
            || expected != currentConsumed
            || expected < 0
            || expected > MaxInferenceRequests)
        {
            denialReason = "expected_consumed_mismatch";
            return false;
        }

        if (!string.Equals(requiredPhase, DiscoveryPhase, StringComparison.Ordinal)
            && !string.Equals(requiredPhase, GptOssDarkbloomPhase, StringComparison.Ordinal))
        {
            denialReason = "retired_candidate";
            return false;
        }

        if (string.Equals(requiredPhase, DiscoveryPhase, StringComparison.Ordinal)
            && currentConsumed >= DiscoveryRetiredAtConsumed)
        {
            denialReason = "discovery_retired";
            return false;
        }

        if (string.Equals(requiredPhase, GptOssDarkbloomPhase, StringComparison.Ordinal)
            && (currentConsumed < GptOssDarkbloomStartsAtConsumed
                || currentConsumed >= Phase21MaxInferenceRequests))
        {
            denialReason = "gpt_oss_darkbloom_requires_consumed_0_to_7";
            return false;
        }

        denialReason = string.Empty;
        return true;
    }
}

public sealed class OpenRouterDiscoveryClient(HttpMessageHandler? transport = null)
{
    public async Task<OpenRouterDiscoveryCandidate?> DiscoverAsync(
        string secret,
        CancellationToken cancellationToken) =>
        (await DiscoverOutcomeAsync(secret, cancellationToken)).Candidate;

    public async Task<OpenRouterDiscoveryOutcome> DiscoverOutcomeAsync(
        string secret,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        if (transport is null
            && (!OpenRouterLiveQualification.IsEnabled || !OpenRouterLiveQualification.SyntheticDataPolicyAccepted))
        {
            return OpenRouterDiscoveryOutcome.Failed(OpenRouterDiscoveryFailureReasons.PreflightDenied);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(OpenRouterAdapterContracts.ControlTimeout);
        var operation = timeoutCts.Token;
        var inner = transport ?? new HttpClientHandler { AllowAutoRedirect = false };
        using var bounded = new OpenRouterDestinationHandler(inner);
        using var client = new HttpClient(bounded, disposeHandler: transport is null)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        using var request = OpenRouterRequestFactory.CreateDiscovery(secret);
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, operation);
            var status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                return OpenRouterDiscoveryOutcome.Failed(StatusFailure(status), status);
            }

            if (OpenRouterResponseParser.IsResponseCacheHit(response))
            {
                return OpenRouterDiscoveryOutcome.Failed(
                    OpenRouterDiscoveryFailureReasons.ResponseCacheHit,
                    status);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(operation);
            await using var boundedBody = new BoundedReadStream(stream, OpenRouterAdapterContracts.MaxControlEnvelopeUtf8Bytes);
            using var document = await JsonDocument.ParseAsync(boundedBody, cancellationToken: operation);
            if (!OpenRouterResponseParser.TryReadJsonString(document.RootElement, "model", out var model)
                || string.IsNullOrWhiteSpace(model)
                || !IsSafeModelIdentity(model)
                || string.Equals(model, OpenRouterAdapterContracts.DiscoveryModel, StringComparison.Ordinal)
                || !model.EndsWith(":free", StringComparison.Ordinal))
            {
                return OpenRouterDiscoveryOutcome.Failed(
                    OpenRouterDiscoveryFailureReasons.ModelIdentity,
                    status);
            }

            if (!OpenRouterResponseParser.TryReadSelectedProvider(
                    document.RootElement,
                    out var selectedProvider)
                || string.IsNullOrWhiteSpace(selectedProvider))
            {
                return OpenRouterDiscoveryOutcome.Failed(
                    OpenRouterDiscoveryFailureReasons.MissingProviderMetadata,
                    status);
            }

            if (!IsSafeProviderIdentity(selectedProvider))
            {
                return OpenRouterDiscoveryOutcome.Failed(
                    OpenRouterDiscoveryFailureReasons.ProviderIdentity,
                    status);
            }

            if (!OpenRouterResponseParser.TryReadTerminalFacts(
                    document.RootElement,
                    model,
                    selectedProvider,
                    out var facts,
                    out _)
                || facts is null)
            {
                return OpenRouterDiscoveryOutcome.Failed(
                    OpenRouterDiscoveryFailureReasons.InvalidTerminalFacts,
                    status);
            }

            return OpenRouterDiscoveryOutcome.Succeeded(
                new OpenRouterDiscoveryCandidate(facts.ReturnedModel, facts.SelectedProvider),
                status);
        }
        catch (OperationCanceledException)
        {
            return OpenRouterDiscoveryOutcome.Failed(
                cancellationToken.IsCancellationRequested
                    ? OpenRouterDiscoveryFailureReasons.Cancelled
                    : OpenRouterDiscoveryFailureReasons.Timeout);
        }
        catch (HttpRequestException)
        {
            return OpenRouterDiscoveryOutcome.Failed(OpenRouterDiscoveryFailureReasons.Transport);
        }
        catch (JsonException)
        {
            return OpenRouterDiscoveryOutcome.Failed(OpenRouterDiscoveryFailureReasons.MalformedResponse);
        }
        catch (OpenRouterTransportLimitExceededException)
        {
            return OpenRouterDiscoveryOutcome.Failed(OpenRouterDiscoveryFailureReasons.ResponseTooLarge);
        }
    }

    private static string StatusFailure(int status) => status switch
    {
        401 => OpenRouterDiscoveryFailureReasons.Authentication,
        402 => OpenRouterDiscoveryFailureReasons.PaymentRequired,
        403 => OpenRouterDiscoveryFailureReasons.PolicyDenied,
        408 => OpenRouterDiscoveryFailureReasons.Timeout,
        429 => OpenRouterDiscoveryFailureReasons.RateLimited,
        >= 500 => OpenRouterDiscoveryFailureReasons.ProviderUnavailable,
        _ => OpenRouterDiscoveryFailureReasons.RequestRejected,
    };

    private static bool IsSafeModelIdentity(string value) =>
        value.Length <= 256
        && value.All(character =>
            char.IsAsciiLetterOrDigit(character)
            || character is '-' or '_' or '.' or ':' or '/');

    private static bool IsSafeProviderIdentity(string value) =>
        value.Length <= 256
        && value.All(character =>
            !char.IsControl(character)
            && char.GetUnicodeCategory(character) is not System.Globalization.UnicodeCategory.LineSeparator
                and not System.Globalization.UnicodeCategory.ParagraphSeparator);
}

public sealed record OpenRouterDiscoveryCandidate(string Model, string ProviderIdentity);

public sealed record OpenRouterDiscoveryOutcome(
    OpenRouterDiscoveryCandidate? Candidate,
    string? FailureReason,
    int? HttpStatusCode)
{
    public static OpenRouterDiscoveryOutcome Succeeded(
        OpenRouterDiscoveryCandidate candidate,
        int status) => new(candidate, null, status);

    public static OpenRouterDiscoveryOutcome Failed(
        string reason,
        int? status = null) => new(null, reason, status);
}

public static class OpenRouterDiscoveryFailureReasons
{
    public const string PreflightDenied = "preflight_denied";
    public const string Authentication = "authentication";
    public const string PaymentRequired = "payment_required";
    public const string PolicyDenied = "policy_denied";
    public const string RateLimited = "rate_limited";
    public const string ProviderUnavailable = "provider_unavailable";
    public const string RequestRejected = "request_rejected";
    public const string ResponseCacheHit = "response_cache_hit";
    public const string ModelIdentity = "model_identity";
    public const string ProviderIdentity = "provider_identity";
    public const string MissingProviderMetadata = "missing_provider_metadata";
    public const string InvalidTerminalFacts = "invalid_terminal_facts";
    public const string Timeout = "timeout";
    public const string Cancelled = "cancelled";
    public const string Transport = "transport";
    public const string MalformedResponse = "malformed_response";
    public const string ResponseTooLarge = "response_too_large";
}
